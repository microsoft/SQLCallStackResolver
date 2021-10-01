// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using Dia;
    using Microsoft.Diagnostics.Runtime.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Xml;

    public class StackResolver : IDisposable {
        /// This is used to store module name and start / end virtual address ranges
        /// Only populated if the user provides a tab-separated string corresponding to the output of the following SQL query:
        /// select name, base_address from sys.dm_os_loaded_modules where name not like '%.rll'
        public List<ModuleInfo> LoadedModules = new List<ModuleInfo>();
        /// A cache of already resolved addresses
        Dictionary<string, string> cachedSymbols = new Dictionary<string, string>();
        /// R/W lock to protect the above cached symbols dictionary
        ReaderWriterLockSlim rwLockCachedSymbols = new ReaderWriterLockSlim();
        DLLOrdinalHelper dllMapHelper = new DLLOrdinalHelper();
        /// Status message - populated during associated long-running operations
        public string StatusMessage;
        /// Internal counter used to implement progress reporting
        internal int globalCounter = 0;
        internal bool cancelRequested = false;
        /// Percent completed - populated during associated long-running operations
        public int PercentComplete;

        public void CancelRunningTasks() {
            this.cancelRequested = true;
        }

        /// Public method which to help import XEL files
        public Tuple<int, string> ExtractFromXEL(string[] xelFiles, bool bucketize) {
            return XELHelper.ExtractFromXEL(this, xelFiles, bucketize);
        }

        /// Convert virtual-address only type frames to their module+offset format
        private string[] PreProcessVAs(string[] callStackLines) {
            var rgxVAOnly = new Regex(@"^\s*0[xX](?<vaddress>[0-9a-fA-F]+)\s*$");
            string[] retval = new string[callStackLines.Length];

            int frameNum = 0;
            foreach (var currentFrame in callStackLines) {
                // let's see if this is an VA-only address
                var matchVA = rgxVAOnly.Match(currentFrame);
                if (matchVA.Success) {
                    ulong virtAddress = Convert.ToUInt64(matchVA.Groups["vaddress"].Value, 16);
                    if (TryObtainModuleOffset(virtAddress, out string moduleName, out uint offset)) {
                        // finalCallstack.AppendLine(ProcessFrameModuleOffset(moduleName, offset.ToString()));
                        retval[frameNum] = string.Format(CultureInfo.CurrentCulture, "{0}+0x{1:X}", moduleName, offset);
                    }
                    else {
                        retval[frameNum] = currentFrame.Trim();
                    }
                }
                else {
                    retval[frameNum] = currentFrame.Trim();
                }

                frameNum++;
            }
            return retval;
        }

        /// Runs through each of the frames in a call stack and looks up symbols for each
        private string ResolveSymbols(Dictionary<string, DiaUtil> _diautils, string[] callStackLines, bool includeSourceInfo, bool relookupSource, bool includeOffsets, bool showInlineFrames) {
            var finalCallstack = new StringBuilder();
            var rgxModuleName = new Regex(@"((?<framenum>\d+)\s+)*(?<module>\w+)(\.(dll|exe))*\s*\+\s*(0[xX])*(?<offset>[0-9a-fA-F]+)\s*");
            var rgxAlreadySymbolizedFrame = new Regex(@"((?<framenum>\d+)\s+)*(?<module>\w+)(\.(dll|exe))*!(?<symbolizedfunc>.+?)\s*\+\s*(0[xX])*(?<offset>[0-9a-fA-F]+)\s*");
            foreach (var iterFrame in callStackLines) {
                // hard-coded find-replace for XML markup - useful when importing from XML histograms
                var currentFrame = iterFrame.Replace("&lt;", "<").Replace("&gt;", ">");
                if (relookupSource && includeSourceInfo) {
                    // This is a rare case. Sometimes we get frames which are already resolved to their symbols but do not include source and line number information
                    // take for example     sqldk.dll!SpinlockBase::Sleep+0x2d0
                    // in these cases, we may want to 're-resolve' them to a symbol using DIA so that later
                    // we can embed source / line number information if that is available now (this is important for some
                    // Microsoft internal cases where customers send us stacks resolved with public PDBs but internally we
                    // have private PDBs so we want to now leverage the extra information provided in the private PDBs.)
                    var matchAlreadySymbolized = rgxAlreadySymbolizedFrame.Match(currentFrame);
                    if (matchAlreadySymbolized.Success && _diautils.ContainsKey(matchAlreadySymbolized.Groups["module"].Value)) {
                        var myDIAsession = _diautils[matchAlreadySymbolized.Groups["module"].Value]._IDiaSession;
                        myDIAsession.findChildrenEx(myDIAsession.globalScope, SymTagEnum.SymTagNull, matchAlreadySymbolized.Groups["symbolizedfunc"].Value, 0, out IDiaEnumSymbols matchedSyms);

                        if (matchedSyms.count > 0) {
                            for (uint tmpOrdinal = 0; tmpOrdinal < matchedSyms.count; tmpOrdinal++) {
                                IDiaSymbol tmpSym = matchedSyms.Item(tmpOrdinal);
                                var rva = tmpSym.relativeVirtualAddress;

                                string offsetString = matchAlreadySymbolized.Groups["offset"].Value;
                                int numberBase = offsetString.ToUpperInvariant().StartsWith("0X", StringComparison.CurrentCulture) ? 16 : 10;
                                uint offset = Convert.ToUInt32(offsetString, numberBase);
                                rva += offset;
                                myDIAsession.findLinesByRVA(rva, 0, out IDiaEnumLineNumbers enumLineNums);
                                string tmpsourceInfo = DiaUtil.GetSourceInfo(enumLineNums,
                                    _diautils[matchAlreadySymbolized.Groups["module"].Value].HasSourceInfo);

                                if (tmpOrdinal > 0) {
                                    finalCallstack.Append(" OR ");
                                }

                                finalCallstack.AppendFormat(CultureInfo.CurrentCulture, "{0}!{1}{2}\t{3}", matchAlreadySymbolized.Groups["module"].Value,
                                    matchAlreadySymbolized.Groups["symbolizedfunc"].Value, includeOffsets ? "+" + offsetString : string.Empty, tmpsourceInfo);
                                Marshal.ReleaseComObject(tmpSym);
                            }
                            Marshal.ReleaseComObject(matchedSyms);
                        }
                        else {
                            // in the rare case that the symbol does not exist, return frame as-is
                            finalCallstack.Append(currentFrame);
                        }
                        finalCallstack.AppendLine();
                        continue;
                    }
                }

                var match = rgxModuleName.Match(currentFrame);
                if (match.Success) {
                    var matchedModuleName = match.Groups["module"].Value;
                    int frameNum = string.IsNullOrWhiteSpace(match.Groups["framenum"].Value) ? int.MinValue : Convert.ToInt32(match.Groups["framenum"].Value, 16);
                    if (_diautils.ContainsKey(matchedModuleName)) {
                        string processedFrame = ProcessFrameModuleOffset(_diautils, frameNum, matchedModuleName, match.Groups["offset"].Value, includeSourceInfo, includeOffsets, showInlineFrames);
                        if (!string.IsNullOrEmpty(processedFrame)) {
                            // typically this is because we could not find the offset in any known function range
                            finalCallstack.AppendLine(processedFrame);
                        }
                        else {
                            finalCallstack.AppendLine(currentFrame);
                        }
                    }
                    else {
                        finalCallstack.AppendLine(currentFrame.Trim());
                    }
                }
                else {
                    finalCallstack.AppendLine(currentFrame.Trim());
                }
            }

            return finalCallstack.ToString();
        }

        /// This function will check if we have a module corresponding to the load address. Only used for pure virtual address format frames.
        private bool TryObtainModuleOffset(ulong virtAddress, out string moduleName, out uint offset) {
            var matchedModule = from mod in LoadedModules
                                where (mod.BaseAddress <= virtAddress && virtAddress <= mod.EndAddress)
                                select mod;

            // we must have exactly one match (else either there's no matching module or we've got flawed load address data
            if (matchedModule.Count() != 1) {
                moduleName = null;
                offset = 0;

                return false;
            }
            moduleName = matchedModule.First().ModuleName;
            // compute the offset / RVA now
            offset = (uint)(virtAddress - matchedModule.First().BaseAddress);
            return true;
        }

        /// This is the most important function in this whole utility! It uses DIA to lookup the symbol based on RVA offset
        /// It also looks up line number information if available and then formats all of this information for returning to caller
        private string ProcessFrameModuleOffset(Dictionary<string, DiaUtil> _diautils, int frameNum, string moduleName, string offset, bool includeSourceInfo, bool includeOffset, bool showInlineFrames) {
            bool useUndecorateLogic = false;

            // the offsets in the XE output are in hex, so we convert to base-10 accordingly
            var rva = Convert.ToUInt32(offset, 16);
            var symKey = moduleName + rva.ToString(CultureInfo.CurrentCulture);
            string result = null;
            this.rwLockCachedSymbols.EnterReadLock();
            if (this.cachedSymbols.ContainsKey(symKey)) {
                result = this.cachedSymbols[symKey];
            }
            this.rwLockCachedSymbols.ExitReadLock();

            if (!string.IsNullOrEmpty(result)) {
                // value was in cache
                return result;
            }

            // process the function name (symbol); initially we look for 'block' symbols, which have a parent function; typically this is seen in kernelbase.dll 
            // (not very important for XE callstacks but important if you have an assert or non-yielding stack in SQLDUMPnnnn.txt files...)
            _diautils[moduleName]._IDiaSession.findSymbolByRVAEx(rva, SymTagEnum.SymTagBlock, out IDiaSymbol mysym, out int displacement);
            if (mysym != null) {
                uint blockAddress = mysym.addressOffset;

                // if we did find a block symbol then we look for its parent till we find either a function or public symbol
                // an addition check is on the name of the symbol being non-null and non-empty
                while (!(mysym.symTag == (uint)SymTagEnum.SymTagFunction || mysym.symTag == (uint)Dia.SymTagEnum.SymTagPublicSymbol) && string.IsNullOrEmpty(mysym.name)) {
                    mysym = mysym.lexicalParent;
                }

                // Calculate offset into the function by assuming that the final lexical parent we found in the loop above
                // is the actual start of the function. Then the difference between (the original block start function start + displacement) 
                // and final lexical parent's start addresses is the final "displacement" / offset to be displayed
                displacement = (int)(blockAddress - mysym.addressOffset + displacement);
            }
            else {
                // we did not find a block symbol, so let's see if we get a Function symbol itself
                // generally this is going to return mysym as null for most users (because public PDBs do not tag the functions as Function
                // they instead are tagged as PublicSymbol)
                _diautils[moduleName]._IDiaSession.findSymbolByRVAEx(rva, SymTagEnum.SymTagFunction, out mysym, out displacement);
                if (mysym == null) {
                    useUndecorateLogic = true;

                    // based on previous remarks, look for public symbol near the offset / RVA
                    _diautils[moduleName]._IDiaSession.findSymbolByRVAEx(rva, SymTagEnum.SymTagPublicSymbol, out mysym, out displacement);
                }
            }

            if (mysym == null) {
                // if all attempts to locate a matching symbol have failed, return null
                return null;
            }

            // try to find if we have source and line number info and include it based on the param
            string sourceInfo = string.Empty;
            var pdbHasSourceInfo = _diautils[moduleName].HasSourceInfo;
            if (includeSourceInfo) {
                _diautils[moduleName]._IDiaSession.findLinesByRVA(rva, 0, out IDiaEnumLineNumbers enumLineNums);
                sourceInfo = DiaUtil.GetSourceInfo(enumLineNums, pdbHasSourceInfo);
            }
            var symbolizedFrame = DiaUtil.GetSymbolizedFrame(frameNum, moduleName, mysym, useUndecorateLogic, includeOffset, displacement);
            // Process inline functions, but only if private PDBs are in use
            string inlineFrameAndSourceInfo = string.Empty;
            if (showInlineFrames && pdbHasSourceInfo) {
                inlineFrameAndSourceInfo = DiaUtil.ProcessInlineFrames(moduleName, useUndecorateLogic, includeOffset, includeSourceInfo, rva, mysym, pdbHasSourceInfo);
            }
            result = (inlineFrameAndSourceInfo + symbolizedFrame + "\t" + sourceInfo).Trim();
            // make sure we cleanup COM allocations for the resolved sym
            Marshal.FinalReleaseComObject(mysym);
            this.rwLockCachedSymbols.EnterWriteLock();
            if (!this.cachedSymbols.ContainsKey(symKey)) {
                this.cachedSymbols.Add(symKey, result);
            }
            this.rwLockCachedSymbols.ExitWriteLock();

            return result;
        }

        /// This helper function parses the output of the sys.dm_os_loaded_modules query and constructs an internal map of each modules start and end virtual address
        public bool ProcessBaseAddresses(string baseAddressesString) {
            bool retVal = true;
            if (string.IsNullOrEmpty(baseAddressesString)) {
                // changed this to return true because this is not a true error condition
                return true;
            }
            LoadedModules.Clear();
            var rgxmoduleaddress = new Regex(@"^\s*(?<filepath>.+)(\t+| +)(?<baseaddress>(0x)*[0-9a-fA-F`]+)\s*$", RegexOptions.Multiline);
            var mcmodules = rgxmoduleaddress.Matches(baseAddressesString);
            if (mcmodules.Count == 0) {
                // it is likely that we have malformed input, cannot ignore this so return false.
                return false;
            }

            try {
                foreach (Match matchedmoduleinfo in mcmodules) {
                    LoadedModules.Add(new ModuleInfo() {
                        ModuleName = Path.GetFileNameWithoutExtension(matchedmoduleinfo.Groups["filepath"].Value),
                        BaseAddress = Convert.ToUInt64(matchedmoduleinfo.Groups["baseaddress"].Value.Replace("`", string.Empty), 16),
                        EndAddress = ulong.MaxValue // stub this with an 'infinite' end address; only the highest loaded module will end up with this value finally
                    });
                }
            } catch (FormatException) {
                // typically errors with non-numeric info passed to  Convert.ToUInt64
                retVal = false;
            } catch (ArgumentException) {
                // typically these are malformed paths passed to Path.GetFileNameWithoutExtension
                retVal = false;
            }

            // sort them by base address
            LoadedModules = (from mod in LoadedModules orderby mod.BaseAddress select mod).ToList();
            // loop through the list, computing their end address
            for (int moduleIndex = 1; moduleIndex < LoadedModules.Count; moduleIndex++) {
                // the previous modules end address will be current module's end address - 1 byte
                LoadedModules[moduleIndex - 1].EndAddress = LoadedModules[moduleIndex].BaseAddress - 1;
            }

            return retVal;
        }

        /// <summary>
        /// This is what the caller will invoke to resolve symbols
        /// </summary>
        /// <param name="inputCallstackText">the input call stack text or XML</param>
        /// <param name="symPath">PDB search paths; separated by semi-colons. The first path containing a 'matching' PDB will be used.</param>
        /// <param name="searchPDBsRecursively">search for PDBs recursively in each path specified</param>
        /// <param name="dllPaths">DLL search paths. this is optional unless the call stack has frames of the form dll!OrdinalNNN+offset</param>
        /// <param name="searchDLLRecursively">Search for DLLs recursively in each path specified. The first path containing a 'matching' DLL will be used.</param>
        /// <param name="framesOnSingleLine">Mostly set this to false except when frames are on the same line and separated by spaces.</param>
        /// <param name="includeSourceInfo">This is used to control whether source information is included (in the case that private PDBs are available)</param>
        /// <param name="relookupSource">Boolean used to control if we attempt to relookup source information</param>
        /// <param name="includeOffsets">Whether to output func offsets or not as part of output</param>
        /// <param name="showInlineFrames">Boolean, whether to resolve and show inline frames in the output</param>
        /// <param name="cachePDB">Boolean, whether to cache PDBs locally</param>
        /// <param name="outputFilePath">File path, used if output is directly written to a file</param>
        /// <returns></returns>
        public string ResolveCallstacks(string inputCallstackText, string symPath, bool searchPDBsRecursively, List<string> dllPaths,
            bool searchDLLRecursively, bool framesOnSingleLine, bool includeSourceInfo, bool relookupSource, bool includeOffsets,
            bool showInlineFrames, bool cachePDB, string outputFilePath) {
            // check if the user has provided a list of modules, each with comma-separated which can be structured fairly flexibly as long as they contain the following pieces of info
            // per row, in different fields: PDB file name (including .pdb extension), OR module file name (.dll or .exe extension); a GUID representing the matching PDB GUID
            // the very last field in the row should be an integer specifying the PDB "age" field. in such cases, the below function will return a non-zero list of Symbol objects which internally contain these parsed values for PDB name, GUID and age

            this.cancelRequested = false;
            this.cachedSymbols.Clear();

            // delete and recreate the cached PDB folder
            var symCacheFolder = Path.Combine(Path.GetTempPath(), "SymCache");
            if (Directory.Exists(symCacheFolder)) {
                new DirectoryInfo(symCacheFolder).GetFiles("*", SearchOption.AllDirectories).ToList().ForEach(file => file.Delete());
            }
            else {
                Directory.CreateDirectory(symCacheFolder);
            }

            var finalCallstack = new StringBuilder();
            var xmldoc = new XmlDocument() { XmlResolver = null };
            bool isXMLdoc = false;
            // we evaluate if the input is XML containing multiple stacks
            try {
                this.PercentComplete = 0;
                this.StatusMessage = "Inspecting input to determine processing plan...";
                using (var sreader = new StringReader(inputCallstackText)) {
                    using (var reader = XmlReader.Create(sreader, new XmlReaderSettings() { XmlResolver = null })) {
                        xmldoc.Load(reader);
                    }
                }

                isXMLdoc = true;
            } catch (XmlException) {
                // do nothing because this is not a XML doc
            }

            var listOfCallStacks = new List<StackWithCount>();
            if (!isXMLdoc) {
                this.StatusMessage = "Input being treated as a single callstack...";
                listOfCallStacks.Add(new StackWithCount() {
                    Callstack = inputCallstackText,
                    Count = 1
                });
            }
            else {
                this.StatusMessage = "Input is well formed XML, proceeding...";

                // since the input was XML containing multiple stacks, construct the list of stacks to process
                int stacknum = 0;
                var allstacknodes = xmldoc.SelectNodes("/HistogramTarget/Slot");

                // handle the case wherein we are dealing with a ring buffer output with individual events and not a histogram
                if (0 == allstacknodes.Count) {
                    allstacknodes = xmldoc.SelectNodes("//event[count(./action[contains(@name, 'callstack')]) > 0]");

                    if (allstacknodes.Count > 0) {
                        this.StatusMessage = "Preprocessing XEvent events...";
                        // process individual callstacks
                        foreach (XmlNode currstack in allstacknodes) {
                            if (this.cancelRequested) {
                                return "Operation cancelled.";
                            }

                            var callstackTextNode = currstack.SelectSingleNode("./action[contains(@name, 'callstack')][1]/value[1]");
                            var callstackText = callstackTextNode.InnerText;
                            // proceed to extract the surrounding XML markup
                            callstackTextNode.ParentNode.RemoveChild(callstackTextNode);
                            var eventXMLMarkup = currstack.OuterXml.Replace("\r", string.Empty).Replace("\n", string.Empty);
                            var candidatestack = string.Format(CultureInfo.CurrentCulture, "Event details: {0}:{2}{2}{1}", eventXMLMarkup, callstackText, Environment.NewLine);
                            listOfCallStacks.Add(new StackWithCount() {Callstack = candidatestack, Count = 1});
                            stacknum++;
                            this.PercentComplete = (int)((double)stacknum / allstacknodes.Count * 100.0);
                        }
                    }
                    else {
                        this.StatusMessage = "XML input was detected but it does not appear to be a known schema. Cannot proceed, sorry!";
                    }
                }
                else {
                    this.StatusMessage = "Preprocessing XEvent histogram slots...";

                    // process histograms
                    foreach (XmlNode currstack in allstacknodes) {
                        if (this.cancelRequested) {
                            return "Operation cancelled.";
                        }

                        var slotcount = int.Parse(currstack.Attributes["count"].Value, CultureInfo.CurrentCulture);
                        var candidatestack = string.Format(CultureInfo.CurrentCulture, "Slot_{0}\t[count:{1}]:{3}{3}{2}", stacknum, slotcount, currstack.SelectSingleNode("./value[1]").InnerText, Environment.NewLine);
                        listOfCallStacks.Add(new StackWithCount() {Callstack = candidatestack, Count = slotcount});
                        stacknum++;
                        this.PercentComplete = (int)((double)stacknum / allstacknodes.Count * 100.0);
                    }
                }
            }

            var syms = ModuleInfoHelper.ParseModuleInfo(inputCallstackText);
            if (syms.Count > 0) {
                // if the user has provided such a list of module info, proceed to actually use dbghelp.dll / symsrv.dll to download thos PDBs and get local paths for them
                var paths = SymSrvHelpers.GetFolderPathsForPDBs(this, symPath, syms.Values.ToList());
                // we then "inject" those local PDB paths as higher priority than any possible user provided paths
                symPath = string.Join(";", paths) + ";" + symPath;
            } else {
                // attempt to check if there are XML-formatted frames each with the related PDB attributes and if so replace those lines with the normalized versions
                (syms, listOfCallStacks) = ModuleInfoHelper.ParseModuleInfoXML(listOfCallStacks);
                if (syms.Count > 0) {
                    // if the user has provided such a list of module info, proceed to actually use dbghelp.dll / symsrv.dll to download thos PDBs and get local paths for them
                    var paths = SymSrvHelpers.GetFolderPathsForPDBs(this, symPath, syms.Values.ToList());
                    // we then "inject" those local PDB paths as higher priority than any possible user provided paths
                    symPath = string.Join(";", paths) + ";" + symPath;
                }
            }

            this.StatusMessage = "Resolving callstacks to symbols...";
            this.globalCounter = 0;

            // (re-)initialize the DLL Ordinal Map
            this.dllMapHelper.Initialize();

            // Create a pool of threads to process in parallel
            int numThreads = Math.Min(listOfCallStacks.Count, Environment.ProcessorCount);
            List<Thread> threads = new List<Thread>();
            for (int threadOrdinal = 0; threadOrdinal < numThreads; threadOrdinal++) {
                var tmpThread = new Thread(ProcessCallStack);
                threads.Add(tmpThread);
                tmpThread.Start(new ThreadParams() {dllPaths = dllPaths, framesOnSingleLine = framesOnSingleLine, includeOffsets = includeOffsets,includeSourceInfo = includeSourceInfo,
                    showInlineFrames = showInlineFrames, listOfCallStacks = listOfCallStacks, numThreads = numThreads, relookupSource = relookupSource,
                    searchDLLRecursively = searchDLLRecursively, searchPDBsRecursively = searchPDBsRecursively, symPath = symPath, threadOrdinal = threadOrdinal, cachePDB = cachePDB});
            }

            foreach (var tmpThread in threads) {
                tmpThread.Join();
            }

            if (this.cancelRequested) {
                return "Operation cancelled.";
            }

            this.StatusMessage = "Done with symbol resolution, finalizing output...";
            this.globalCounter = 0;

            // populate the output
            if (!string.IsNullOrEmpty(outputFilePath)) {
                this.StatusMessage = $@"Writing output to file {outputFilePath}";
                using (var outStream = new StreamWriter(outputFilePath, false)) {
                    foreach (var currstack in listOfCallStacks) {
                        if (this.cancelRequested) {
                            return "Operation cancelled.";
                        }

                        if (!string.IsNullOrEmpty(currstack.Resolvedstack)) {
                            outStream.WriteLine(currstack.Resolvedstack);
                        }
                        else {
                            if (!string.IsNullOrEmpty(currstack.Callstack.Trim())) {
                                outStream.WriteLine("WARNING: No output to show. This may indicate an internal error!");
                                break;
                            }
                        }

                        this.globalCounter++;
                        this.PercentComplete = (int)((double)this.globalCounter / listOfCallStacks.Count * 100.0);
                    }
                }
            }
            else {
                this.StatusMessage = "Consolidating output for screen display...";

                foreach (var currstack in listOfCallStacks) {
                    if (this.cancelRequested) {
                        return "Operation cancelled.";
                    }

                    if (!string.IsNullOrEmpty(currstack.Resolvedstack)) {
                        finalCallstack.AppendLine(currstack.Resolvedstack);
                    }
                    else {
                        if (!string.IsNullOrEmpty(currstack.Callstack)) {
                            finalCallstack = new StringBuilder("WARNING: No output to show. This may indicate an internal error!");
                            break;
                        }
                    }

                    this.globalCounter++;
                    this.PercentComplete = (int)((double)this.globalCounter / listOfCallStacks.Count * 100.0);
                }
            }

            // Unfortunately the below is necessary to ensure that the handles to the cached PDB files opened by DIA 
            // and later deleted at the next invocation of this function, are released deterministically
            // This is despite we correctly releasing those interface pointers using Marshal.FinalReleaseComObject
            // Thankfully we only need to resort to this if the caller wants to cache PDBs in the temp folder
            if (cachePDB) {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            this.StatusMessage = "Finished!";
            if (string.IsNullOrEmpty(outputFilePath)) {
                return finalCallstack.ToString();
            }
            else {
                return $@"Output has been saved to {outputFilePath}";
            }
        }

        /// Function executed by worker threads to process callstacks. Threads work on portions of the listOfCallStacks based on their thread ordinal.
        private void ProcessCallStack(Object obj) {
            SafeNativeMethods.EstablishActivationContext();
            var tp = (ThreadParams)obj;
            Dictionary<string, DiaUtil> _diautils = new Dictionary<string, DiaUtil>();

            for (int tmpStackIndex = 0; tmpStackIndex < tp.listOfCallStacks.Count; tmpStackIndex++) {
                if (this.cancelRequested) {
                    break;
                }

                if (tmpStackIndex % tp.numThreads != tp.threadOrdinal) {
                    continue;
                }

                var currstack = tp.listOfCallStacks[tmpStackIndex];
                // split the callstack into lines, and for each line try to resolve
                string ordinalresolvedstack;
                ordinalresolvedstack = this.dllMapHelper.LoadDllsIfApplicable(currstack.Callstack, tp.searchDLLRecursively, tp.dllPaths);
                // sometimes we see call stacks which are arranged horizontally (this typically is seen when copy-pasting directly
                // from the SSMS XEvent window (copying the callstack field without opening it in its own viewer)
                // in that case, space is a valid delimiter, and we need to support that as an option
                var delims = tp.framesOnSingleLine ? new char[3] { ' ', '\t', '\n' } : new char[1] { '\n' };
                var callStackLines = ordinalresolvedstack.Replace('\r', ' ').Split(delims, StringSplitOptions.RemoveEmptyEntries);

                // process any frames which are purely virtual address (in such cases, the caller should have specified base addresses)
                callStackLines = PreProcessVAs(callStackLines);

                // locate the PDBs and populate their DIA session helper classes
                if (DiaUtil.LocateandLoadPDBs(_diautils, tp.symPath, tp.searchPDBsRecursively, Preprocessors.EnumModuleNames(callStackLines), tp.cachePDB)) {
                    // resolve symbols by using DIA
                    currstack.Resolvedstack = ResolveSymbols(_diautils, callStackLines, tp.includeSourceInfo, tp.relookupSource, tp.includeOffsets, tp.showInlineFrames);
                }
                else {
                    currstack.Resolvedstack = string.Empty;
                    break;
                }

                var localCounter = Interlocked.Increment(ref this.globalCounter);
                this.PercentComplete = (int)((double)localCounter / tp.listOfCallStacks.Count * 100.0);
            }

            // cleanup any older COM objects
            if (_diautils != null) {
                foreach (var diautil in _diautils.Values) {
                    diautil.Dispose();
                }
            }

            SafeNativeMethods.DestroyActivationContext();
        }

        /// This method generates a PowerShell script to automate download of matched PDBs from the public symbol server.
        public static List<Symbol> GetSymbolDetailsForBinaries(List<string> dllPaths, bool recurse) {
            if (dllPaths == null || dllPaths.Count == 0) {
                return new List<Symbol>();
            }

            var symbolsFound = new List<Symbol>();
            var moduleNames = new string[] { "ntdll", "kernel32", "kernelbase", "ntoskrnl", "sqldk", "sqlmin", "sqllang", "sqltses", "sqlaccess", "qds", "hkruntime", "hkengine", "hkcompile", "sqlos", "sqlservr" };
            foreach (var currentModule in moduleNames) {
                string finalFilePath = null;

                foreach (var currPath in dllPaths) {
                    if (!Directory.Exists(currPath)) {
                        continue;
                    }

                    var foundFiles = from f in Directory.EnumerateFiles(currPath, currentModule + ".*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                                     where f.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase) || f.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase)
                                     select f;

                    if (foundFiles.Any()) {
                        finalFilePath = foundFiles.First();
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(finalFilePath)) {
                    using (var dllFileStream = new FileStream(finalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        using (var dllImage = new PEImage(dllFileStream, false)) {
                            var internalPDBName = dllImage.DefaultPdb.Path;
                            var pdbGuid = dllImage.DefaultPdb.Guid;
                            var pdbAge = dllImage.DefaultPdb.Revision;
                            var usablePDBName = Path.GetFileNameWithoutExtension(internalPDBName);
                            var newSymbol = new Symbol() {PDBName = usablePDBName, InternalPDBName = internalPDBName,
                                DownloadURL = string.Format(CultureInfo.CurrentCulture, @"https://msdl.microsoft.com/download/symbols/{0}.pdb/{1}/{0}.pdb",
                                    usablePDBName, pdbGuid.ToString("N", CultureInfo.CurrentCulture) + pdbAge.ToString(CultureInfo.CurrentCulture)), FileVersion = dllImage.GetFileVersionInfo().FileVersion};

                            newSymbol.DownloadVerified = Symbol.IsURLValid(new Uri(newSymbol.DownloadURL));

                            symbolsFound.Add(newSymbol);
                        }
                    }
                }
            }

            return symbolsFound;
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    rwLockCachedSymbols.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
