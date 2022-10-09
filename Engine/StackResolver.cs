// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    public class StackResolver : IDisposable {
        public const string OperationCanceled = "Operation cancelled.";
        public const int OperationWaitIntervalMilliseconds = 300;
        public const int Operation100Percent = 100;

        /// This is used to store module name and start / end virtual address ranges
        /// Only populated if the user provides a tab-separated string corresponding to the output of the following SQL query:
        /// select name, base_address from sys.dm_os_loaded_modules where name not like '%.rll'
        public List<ModuleInfo> LoadedModules = new();
        /// A cache of already resolved addresses
        private readonly Dictionary<string, string> cachedSymbols = new();
        /// R/W lock to protect the above cached symbols dictionary
        private readonly ReaderWriterLock rwLockCachedSymbols = new();
        private readonly DLLOrdinalHelper dllMapHelper = new();
        /// Status message - populated during associated long-running operations
        public string StatusMessage { get; set; }
        /// Percent completed - populated during associated long-running operations
        public int PercentComplete { get; set; }
        /// Internal counter used to implement progress reporting
        internal int globalCounter = 0;

        private static readonly RegexOptions rgxOptions = RegexOptions.ExplicitCapture | RegexOptions.Compiled;
        private static readonly Regex rgxModuleName = new(@"((?<framenum>[0-9a-fA-F]+)\s+)*(?<module>\w+)(\.(dll|exe))*\s*\+\s*(0[xX])*(?<offset>[0-9a-fA-F]+)\s*", rgxOptions);
        private static readonly Regex rgxVAOnly = new (@"^\s*0[xX](?<vaddress>[0-9a-fA-F]+)\s*$", rgxOptions);
        private static readonly Regex rgxAlreadySymbolizedFrame = new (@"((?<framenum>\d+)\s+)*(?<module>\w+)(\.(dll|exe))*!(?<symbolizedfunc>.+?)\s*\+\s*(0[xX])*(?<offset>[0-9a-fA-F]+)\s*", rgxOptions);
        private static readonly Regex rgxmoduleaddress = new (@"^\s*(?<filepath>.+)(\t+| +)(?<baseaddress>(0x)?[0-9a-fA-F`]+)\s*$", RegexOptions.Multiline);

        public Task<Tuple<List<string>, List<string>>> GetDistinctXELFieldsAsync(string[] xelFiles, int eventsToSample, CancellationTokenSource cts) {
            return XELHelper.GetDistinctXELActionsFieldsAsync(xelFiles, eventsToSample, cts);
        }

        /// Public method which to help import XEL files
        public async Task<Tuple<int, string>> ExtractFromXELAsync(string[] xelFiles, bool groupEvents, List<string> relevantFields, CancellationTokenSource cts) {
            return await XELHelper.ExtractFromXELAsync(this, xelFiles, groupEvents, relevantFields, cts);
        }

        /// Convert virtual-address only type frames to their module+offset format
        private string[] PreProcessVAs(string[] callStackLines, CancellationTokenSource cts) {
            string[] retval = new string[callStackLines.Length];
            int frameNum = 0;
            foreach (var currentFrame in callStackLines) {
                if (cts.IsCancellationRequested) return callStackLines;
                // let's see if this is an VA-only address
                var matchVA = rgxVAOnly.Match(currentFrame);
                if (matchVA.Success) {
                    ulong virtAddress = Convert.ToUInt64(matchVA.Groups["vaddress"].Value, 16);
                    retval[frameNum] = TryObtainModuleOffset(virtAddress, out string moduleName, out uint offset)
                        ? string.Format(CultureInfo.CurrentCulture, "{0}+0x{1:X}", moduleName, offset)
                        : currentFrame.Trim();
                }
                else retval[frameNum] = currentFrame.Trim();

                frameNum++;
            }
            return retval;
        }

        public bool IsInputSingleLine(string text, string patternsToTreatAsMultiline) {
            if (Regex.Match(text, patternsToTreatAsMultiline).Success) return false;
            text = System.Net.WebUtility.HtmlDecode(text);  // decode XML markup if present
            if (!(Regex.Match(text, "Histogram").Success || Regex.Match(text, @"\<frame", RegexOptions.IgnoreCase).Success) && !text.Replace("\r", string.Empty).Trim().Contains('\n')) return true; // not a histogram and does not have any newlines, so is single-line
            if (!Regex.Match(text, @"\<frame").Success) {   // input does not have "XML frames", so keep looking...
                if (Regex.Match(text, @"\<Slot.+\<\/Slot\>").Success) return true;  // the content within a given histogram slot is on a single line, so is single-line
                if (Regex.Match(text, @"0x.+0x.+").Success) return true;
            }
            return false;
        }

        /// Runs through each of the frames in a call stack and looks up symbols for each
        private string ResolveSymbols(Dictionary<string, DiaUtil> _diautils, Dictionary<string, string> moduleNamesMap, string[] callStackLines, string symPath, bool searchPDBsRecursively, bool cachePDB, bool includeSourceInfo, bool relookupSource, bool includeOffsets, bool showInlineFrames, List<string> modulesToIgnore, CancellationTokenSource cts) {
            var finalCallstack = new StringBuilder();
            int runningFrameNum = int.MinValue;
            foreach (var iterFrame in callStackLines) {
                if (cts.IsCancellationRequested) { StatusMessage = OperationCanceled; PercentComplete = 0; return OperationCanceled; }
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
                    if (matchAlreadySymbolized.Success) {
                        var matchedModuleName = matchAlreadySymbolized.Groups["module"].Value;
                        if (!_diautils.ContainsKey(matchedModuleName)) DiaUtil.LocateandLoadPDBs(_diautils, symPath, searchPDBsRecursively, new Dictionary<string, string>() { { matchedModuleName, matchedModuleName } }, cachePDB, modulesToIgnore);
                        if (_diautils.TryGetValue(matchedModuleName, out var existingEntry) && _diautils[matchedModuleName].HasSourceInfo) {
                            var myDIAsession = existingEntry._IDiaSession;
                            myDIAsession.findChildrenEx(myDIAsession.globalScope, SymTagEnum.SymTagNull, matchAlreadySymbolized.Groups["symbolizedfunc"].Value, 0, out IDiaEnumSymbols matchedSyms);

                            var foundMatch = false;
                            if (matchedSyms.count > 0) {
                                for (uint tmpOrdinal = 0; tmpOrdinal < matchedSyms.count; tmpOrdinal++) {
                                    IDiaSymbol tmpSym = matchedSyms.Item(tmpOrdinal);
                                    string offsetString = matchAlreadySymbolized.Groups["offset"].Value;
                                    int numberBase = offsetString.ToUpperInvariant().StartsWith("0X", StringComparison.CurrentCulture) ? 16 : 10;
                                    var currAddress = tmpSym.addressOffset + Convert.ToUInt32(offsetString, numberBase);

                                    myDIAsession.findLinesByRVA(tmpSym.relativeVirtualAddress, (uint)tmpSym.length, out IDiaEnumLineNumbers enumAllLineNums);
                                    if (enumAllLineNums.count > 0) {
                                        for (uint tmpOrdinalInner = 0; tmpOrdinalInner < enumAllLineNums.count; tmpOrdinalInner++) {
                                            // below, we search for a line of code whose address range covers the current address of interest, and if matched, we re-write the current line in the module+RVA format
                                            if (enumAllLineNums.Item(tmpOrdinalInner).addressOffset <= currAddress
                                                && currAddress < enumAllLineNums.Item(tmpOrdinalInner).addressOffset + enumAllLineNums.Item(tmpOrdinalInner).length) {
                                                currentFrame = $"{matchedModuleName}+{currAddress - enumAllLineNums.Item(tmpOrdinalInner).addressOffset + enumAllLineNums.Item(tmpOrdinalInner).relativeVirtualAddress:X}" 
                                                    + (foundMatch ? " -- WARNING: ambiguous symbol; relookup might be incorrect -- " : String.Empty);
                                                foundMatch = true;
                                            }
                                            Marshal.FinalReleaseComObject(enumAllLineNums.Item(tmpOrdinalInner));
                                        }
                                    }
                                    Marshal.FinalReleaseComObject(enumAllLineNums);
                                    Marshal.FinalReleaseComObject(tmpSym);
                                }
                                Marshal.FinalReleaseComObject(matchedSyms);
                            }
                        }
                    }
                }

                var match = rgxModuleName.Match(currentFrame);
                if (match.Success) {
                    var matchedModuleName = match.Groups["module"].Value;
                    // maybe we have a "not well-known" module, attempt to (best effort) find PDB for it.
                    if (!_diautils.ContainsKey(matchedModuleName)) DiaUtil.LocateandLoadPDBs(_diautils, symPath, searchPDBsRecursively, new Dictionary<string, string>() { { matchedModuleName, matchedModuleName } }, cachePDB, modulesToIgnore);
                    int frameNumFromInput = string.IsNullOrWhiteSpace(match.Groups["framenum"].Value) ? int.MinValue : Convert.ToInt32(match.Groups["framenum"].Value, 16);
                    if (frameNumFromInput != int.MinValue && runningFrameNum == int.MinValue) runningFrameNum = frameNumFromInput;
                    if (_diautils.ContainsKey(matchedModuleName)) {
                        string processedFrame = ProcessFrameModuleOffset(_diautils, moduleNamesMap, frameNumFromInput, ref runningFrameNum, matchedModuleName, match.Groups["offset"].Value, includeSourceInfo, includeOffsets, showInlineFrames);
                        if (!string.IsNullOrEmpty(processedFrame)) finalCallstack.AppendLine(processedFrame);   // typically this is because we could not find the offset in any known function range
                        else finalCallstack.AppendLine(currentFrame);
                    }
                    else finalCallstack.AppendLine(currentFrame.Trim());
                }
                else finalCallstack.AppendLine(currentFrame.Trim());
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
        private string ProcessFrameModuleOffset(Dictionary<string, DiaUtil> _diautils, Dictionary<string, string> moduleNamesMap, int frameNumFromInput, ref int frameNum, string moduleName, string offset, bool includeSourceInfo, bool includeOffset, bool showInlineFrames) {
            bool useUndecorateLogic = false;

            // the offsets in the XE output are in hex, so we convert to base-10 accordingly
            var rva = Convert.ToUInt32(offset, 16);
            var symKey = moduleName + rva.ToString(CultureInfo.CurrentCulture);
            this.rwLockCachedSymbols.AcquireReaderLock(-1);
            bool resWasCached = this.cachedSymbols.TryGetValue(symKey, out string result);
            this.rwLockCachedSymbols.ReleaseReaderLock();
            if (!resWasCached) {
                // process the function name (symbol); initially we look for 'block' symbols, which have a parent function; typically this is seen in kernelbase.dll 
                // (not very important for XE callstacks but important if you have an assert or non-yielding stack in SQLDUMPnnnn.txt files...)
                _diautils[moduleName]._IDiaSession.findSymbolByRVAEx(rva, SymTagEnum.SymTagBlock, out IDiaSymbol mysym, out int displacement);
                if (mysym != null) {
                    uint blockAddress = mysym.addressOffset;

                    // if we did find a block symbol then we look for its parent till we find either a function or public symbol
                    // an addition check is on the name of the symbol being non-null and non-empty
                    while (!(mysym.symTag == (uint)SymTagEnum.SymTagFunction || mysym.symTag == (uint)SymTagEnum.SymTagPublicSymbol) && string.IsNullOrEmpty(mysym.name)) {
                        mysym = mysym.lexicalParent;
                    }

                    // Calculate offset into the function by assuming that the final lexical parent we found in the loop above
                    // is the actual start of the function. Then the difference between (the original block start function start + displacement) 
                    // and final lexical parent's start addresses is the final "displacement" / offset to be displayed
                    displacement = (int)(blockAddress - mysym.addressOffset + displacement);
                } else {
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

                string sourceInfo = string.Empty;   // try to find if we have source and line number info and include it based on the param
                string inlineFrameAndSourceInfo = string.Empty; // Process inline functions, but only if private PDBs are in use
                var pdbHasSourceInfo = _diautils[moduleName].HasSourceInfo;
                if (includeSourceInfo) {
                    _diautils[moduleName]._IDiaSession.findLinesByRVA(rva, 0, out IDiaEnumLineNumbers enumLineNums);
                    sourceInfo = DiaUtil.GetSourceInfo(enumLineNums, pdbHasSourceInfo);
                    Marshal.FinalReleaseComObject(enumLineNums);
                }
                var originalModuleName = moduleNamesMap.ContainsKey(moduleName) ? moduleNamesMap[moduleName] : moduleName;
                if (showInlineFrames && pdbHasSourceInfo && !sourceInfo.Contains("-- WARNING:")) {
                    inlineFrameAndSourceInfo = DiaUtil.ProcessInlineFrames(originalModuleName, useUndecorateLogic, includeOffset, includeSourceInfo, rva, mysym, pdbHasSourceInfo);
                }

                var symbolizedFrame = DiaUtil.GetSymbolizedFrame(originalModuleName, mysym, useUndecorateLogic, includeOffset, displacement, false);

                // make sure we cleanup COM allocations for the resolved sym
                Marshal.FinalReleaseComObject(mysym);
                result = (inlineFrameAndSourceInfo + symbolizedFrame + "\t" + sourceInfo).Trim();
                if (!resWasCached) {    // we only need to add to cache if it was not already cached.
                    this.rwLockCachedSymbols.AcquireWriterLock(-1);
                    if (!this.cachedSymbols.ContainsKey(symKey)) {
                        this.cachedSymbols.Add(symKey, result);
                    }
                    this.rwLockCachedSymbols.ReleaseWriterLock();
                }
            }

            if (frameNum != int.MinValue) {
                if (frameNumFromInput == 0) frameNum = frameNumFromInput;
                var withFrameNums = new StringBuilder();
                var resultLines = result.Split('\n');
                foreach (var line in resultLines) {
                    withFrameNums.AppendLine($"{frameNum:x2} {line.Trim('\r')}");
                    frameNum++;
                }
                result = withFrameNums.ToString().Trim();
            }
            return result;
        }

        /// <summary>
        /// Parse the output of the sys.dm_os_loaded_modules query and constructs an internal map of each modules start and end virtual address
        /// </summary>
        public bool ProcessBaseAddresses(string baseAddressesString) {
            bool retVal = true;
            if (string.IsNullOrEmpty(baseAddressesString)) return true; // not a true error condition so we are okay
            LoadedModules.Clear();
            var mcmodules = rgxmoduleaddress.Matches(baseAddressesString);
            if (!mcmodules.Cast<Match>().Any()) return false; // it is likely that we have malformed input, cannot ignore this so return false.

            try {
                string[] validExtensions = { ".dll", ".exe" };
                mcmodules.Cast<Match>().Where(m => validExtensions.Contains(Path.GetExtension(m.Groups["filepath"].Value).Trim().ToLower())).ToList().ForEach(matchedmoduleinfo => LoadedModules.Add(new ModuleInfo() {
                    ModuleName = Path.GetFileNameWithoutExtension(matchedmoduleinfo.Groups["filepath"].Value),
                    BaseAddress = Convert.ToUInt64(matchedmoduleinfo.Groups["baseaddress"].Value.Replace("`", string.Empty), 16),
                    EndAddress = ulong.MaxValue // stub this with an 'infinite' end address; only the highest loaded module will end up with this value finally
                }));
            } catch (FormatException) {
                // typically errors with non-numeric info passed to Convert.ToUInt64
                retVal = false;
            } catch (OverflowException) {
                // absurdly large numeric info passed to Convert.ToUInt64
                retVal = false;
            } catch (ArgumentException) {
                // typically these are malformed paths passed to Path.GetFileNameWithoutExtension
                retVal = false;
            }
            if (!LoadedModules.Any()) return false; // no valid modules found

            // check for duplicate base addresses - this should normally never be possible unless there is wrong data input
            if (LoadedModules.Select(m => m.BaseAddress).GroupBy(m => m).Where(g => g.Count() > 1).Any()) return false;

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
        public async Task<string> ResolveCallstacksAsync(List<StackDetails> listOfCallStacks, string symPath, bool searchPDBsRecursively, List<string> dllPaths,
            bool searchDLLRecursively, bool includeSourceInfo, bool relookupSource, bool includeOffsets,
            bool showInlineFrames, bool cachePDB, string outputFilePath, CancellationTokenSource cts) {
            return await Task.Run(async () => {
                this.cachedSymbols.Clear();

                // delete and recreate the cached PDB folder
                var symCacheFolder = Path.Combine(Path.GetTempPath(), "SymCache");
                if (Directory.Exists(symCacheFolder)) {
                    new DirectoryInfo(symCacheFolder).GetFiles("*", SearchOption.AllDirectories).ToList().ForEach(file => file.Delete());
                } else Directory.CreateDirectory(symCacheFolder);

                this.StatusMessage = "Checking for embedded symbol information...";
                var syms = await ModuleInfoHelper.ParseModuleInfoAsync(listOfCallStacks, cts);
                if (cts.IsCancellationRequested) { StatusMessage = OperationCanceled; PercentComplete = 0; return OperationCanceled; }

                if (syms.Count() > 0) {
                    this.StatusMessage = "Downloading symbols as needed...";
                    // if the user has provided such a list of module info, proceed to actually use dbghelp.dll / symsrv.dll to download those PDBs and get local paths for them
                    var paths = SymSrvHelpers.GetFolderPathsForPDBs(this, symPath, syms.Values.ToList());
                    // we then "inject" those local PDB paths as higher priority than any possible user provided paths
                    symPath = string.Join(";", paths) + ";" + symPath;
                } else {
                    this.StatusMessage = "Looking for embedded XML-formatted frames and symbol information...";
                    // attempt to check if there are XML-formatted frames each with the related PDB attributes and if so replace those lines with the normalized versions
                    (syms, listOfCallStacks) = await ModuleInfoHelper.ParseModuleInfoXMLAsync(listOfCallStacks, cts);
                    if (syms == null) return "Unable to determine symbol information from XML frames - this may be caused by multiple PDB versions in the same input.";
                    if (cts.IsCancellationRequested) { StatusMessage = OperationCanceled; PercentComplete = 0; return OperationCanceled; }
                    if (syms.Count() > 0) {
                        // if the user has provided such a list of module info, proceed to actually use dbghelp.dll / symsrv.dll to download thos PDBs and get local paths for them
                        var paths = SymSrvHelpers.GetFolderPathsForPDBs(this, symPath, syms.Values.ToList());
                        // we then "inject" those local PDB paths as higher priority than any possible user provided paths
                        symPath = string.Join(";", paths) + ";" + symPath;
                    }
                }

                var moduleNamesMap = syms.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ModuleName, StringComparer.OrdinalIgnoreCase); //.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value.PDBName));

                this.StatusMessage = "Resolving callstacks to symbols...";
                this.globalCounter = 0;

                // (re-)initialize the DLL Ordinal Map
                this.dllMapHelper.Initialize();

                // Create a pool of threads to process in parallel
                this.StatusMessage = "Starting tasks to process frames...";
                int numThreads = Math.Min(listOfCallStacks.Count, Environment.ProcessorCount);
                List<Task> tasks = new();
                for (int taskOrdinal = 0; taskOrdinal < numThreads; taskOrdinal++) tasks.Add(ProcessCallStack(taskOrdinal, numThreads, listOfCallStacks, moduleNamesMap,
                    symPath, dllPaths, searchPDBsRecursively, searchDLLRecursively, includeSourceInfo, showInlineFrames,
                    relookupSource, includeOffsets, cachePDB, cts));

                this.StatusMessage = "Waiting for tasks to finish...";
                while (true) {
                    if (Task.WaitAll(tasks.ToArray(), OperationWaitIntervalMilliseconds)) break;
                }
                if (cts.IsCancellationRequested) { StatusMessage = OperationCanceled; PercentComplete = 0; return OperationCanceled; }
                this.StatusMessage = "Done with symbol resolution, finalizing output...";
                this.globalCounter = 0;

                var finalCallstack = new StringBuilder();
                // populate the output
                if (!string.IsNullOrEmpty(outputFilePath)) {
                    this.StatusMessage = $@"Writing output to file {outputFilePath}";
                    using var outStream = new StreamWriter(outputFilePath, false);
                    foreach (var currstack in listOfCallStacks) {
                        if (cts.IsCancellationRequested) { StatusMessage = OperationCanceled; PercentComplete = 0; return OperationCanceled; }
                        if (!string.IsNullOrEmpty(currstack.Resolvedstack)) outStream.WriteLine(currstack.Resolvedstack);
                        else if (!string.IsNullOrEmpty(currstack.Callstack.Trim())) {
                            outStream.WriteLine("WARNING: No output to show. This may indicate an internal error!");
                            break;
                        }

                        this.globalCounter++;
                        this.PercentComplete = (int)((double)this.globalCounter / listOfCallStacks.Count * 100.0);
                    }
                } else {
                    this.StatusMessage = "Consolidating output for screen display...";

                    foreach (var currstack in listOfCallStacks) {
                        if (cts.IsCancellationRequested) { StatusMessage = OperationCanceled; PercentComplete = 0; return OperationCanceled; }
                        if (!string.IsNullOrEmpty(currstack.Resolvedstack)) finalCallstack.Append(currstack.Resolvedstack);
                        else if (!string.IsNullOrEmpty(currstack.Callstack)) {
                            finalCallstack = new StringBuilder("WARNING: No output to show. This may indicate an internal error!");
                            break;
                        }

                        if (finalCallstack.Length > int.MaxValue * 0.1) {
                            this.StatusMessage = "WARNING: output is too large to display on screen. Use the option to output to file directly (instead of screen). Re-run after specifying file path!";
                            break;
                        }

                        this.globalCounter++;
                        this.PercentComplete = (int)((double)this.globalCounter / listOfCallStacks.Count * 100.0);
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                this.PercentComplete = StackResolver.Operation100Percent;
                this.StatusMessage = "Finished!";
                return string.IsNullOrEmpty(outputFilePath) ? finalCallstack.ToString() : $@"Output has been saved to {outputFilePath}";
            });
        }

        /// <summary>
        /// Gets a list of StackDetails objects based on the textual callstack input
        /// </summary>
        /// <param name="inputCallstackText"></param>
        /// <param name="framesOnSingleLine"></param>
        /// <param name="cts"></param>
        /// <returns>List of StackDetails objects</returns>
        public async Task<List<StackDetails>> GetListofCallStacksAsync(string inputCallstackText, bool framesOnSingleLine, CancellationTokenSource cts) {
            return await Task.Run(() => {
                this.StatusMessage = "Analyzing input...";
                if (Regex.IsMatch(inputCallstackText, @"<HistogramTarget(\s+|\>)") && inputCallstackText.Contains(@"</HistogramTarget>")) {
                    var numHistogramTargets = Regex.Matches(inputCallstackText, @"\<\/HistogramTarget\>").Count;
                    if (numHistogramTargets > 0) {
                        inputCallstackText = Regex.Replace(inputCallstackText, @"(?<prefix>.*?)(?<starttag>\<HistogramTarget)(?<trailing>.+?\<\/HistogramTarget\>)",
                            (Match m) => { return $"{m.Groups["starttag"].Value} annotation=\"{System.Net.WebUtility.HtmlEncode(m.Groups["prefix"].Value.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim())}\" {m.Groups["trailing"].Value}"; }
                            , RegexOptions.Singleline);
                        inputCallstackText = $"<Histograms>{inputCallstackText}</Histograms>";
                    }
                }

                bool isXMLdoc = false;
                // we evaluate if the input is XML containing multiple stacks
                try {
                    this.PercentComplete = 0;
                    this.StatusMessage = "Determining processing plan...";
                    using var sreader = new StringReader(inputCallstackText);
                    using var reader = XmlReader.Create(sreader, new XmlReaderSettings() { XmlResolver = null });
                    var validElementNames = new List<string>() { "HistogramTarget", "event" };
                    this.StatusMessage = "WARNING: XML input was detected but it does not appear to be a known schema!";
                    while (reader.Read()) {
                        if (XmlNodeType.Element == reader.NodeType && validElementNames.Contains(reader.Name)) {
                            this.StatusMessage = "Input seems to be relevant XML, attempting to process...";
                            isXMLdoc = true;    // assume with reasonable confidence that we have a valid XML doc
                            break;
                        }
                    }
                } catch (XmlException) { this.StatusMessage = "Input is not XML; being treated as a single callstack..."; }

                var allStacks = new List<StackDetails>();
                if (!isXMLdoc) {
                    allStacks.Add(new StackDetails(inputCallstackText, framesOnSingleLine));
                } else {
                    try {
                        int stacknum = 0;
                        using var sreader = new StringReader(inputCallstackText);
                        using var reader = XmlReader.Create(sreader, new XmlReaderSettings() { XmlResolver = null, });
                        string annotation = string.Empty;
                        string eventDetails = string.Empty;
                        string trailingText = string.Empty;
                        while (reader.Read()) {
                            if (cts.IsCancellationRequested) return null;
                            if (XmlNodeType.Text == reader.NodeType) trailingText = reader.Value.Trim();
                            if (XmlNodeType.Element == reader.NodeType) {
                                switch (reader.Name) {
                                    case "HistogramTarget": {   // Parent node for the XML from a histogram target
                                            annotation = reader.GetAttribute("annotation");
                                            if (!string.IsNullOrWhiteSpace(annotation)) { annotation = annotation.Trim(); }
                                            break;
                                        }
                                    case "Slot": {  // Child node for the XML from a histogram target
                                            var slotcount = int.Parse(reader.GetAttribute("count"), CultureInfo.CurrentCulture);
                                            string callstackText = string.Empty;
                                            if (reader.ReadToDescendant("value")) {
                                                reader.Read();
                                                if (XmlNodeType.Text == reader.NodeType || XmlNodeType.CDATA == reader.NodeType) callstackText = reader.Value;
                                            }
                                            if (string.IsNullOrEmpty(callstackText)) throw new XmlException();
                                            allStacks.Add(new StackDetails(callstackText, framesOnSingleLine, annotation, $"Slot_{stacknum}\t[count:{slotcount}]:"));
                                            stacknum++;
                                            break;
                                        }
                                    case "event": { // ring buffer output with individual events
                                            var sbTmp = new StringBuilder();
                                            for (int tmpOrdinal = 0; tmpOrdinal < reader.AttributeCount; tmpOrdinal++) {
                                                reader.MoveToAttribute(tmpOrdinal);
                                                sbTmp.AppendFormat($"{reader.Name}: {reader.Value}".Replace("\r", string.Empty).Replace("\n", string.Empty));
                                            }
                                            eventDetails = sbTmp.ToString();
                                            break;
                                        }
                                    case "action": { // actual action associated with the above ring buffer events
                                            if (!reader.GetAttribute("name").Contains("callstack")) throw new XmlException();
                                            if (!reader.ReadToDescendant("value")) throw new XmlException();
                                            reader.Read();
                                            if (!(XmlNodeType.Text == reader.NodeType || XmlNodeType.CDATA == reader.NodeType)) throw new XmlException();
                                            allStacks.Add(new StackDetails(reader.Value, framesOnSingleLine, string.Empty, $"Event {eventDetails}"));
                                            stacknum++;
                                            break;
                                        }
                                    default: break;
                                }
                            }
                            this.PercentComplete = (int)((double)stacknum % 100.0); // since we are streaming, we can only show pseudo-progress (repeatedly go from 0 to 100 and back).
                        }
                        if (!string.IsNullOrEmpty(trailingText)) allStacks.Last().UpdateAnnotation(trailingText);
                    } catch (XmlException) {
                        // our guesstimate that the input is XML, is not correct, so bail out and revert back to handling the callstack as text
                        this.StatusMessage = "XML-like input was found to be invalid, now being treated as a single callstack...";
                        allStacks.Clear();
                        allStacks.Add(new StackDetails(inputCallstackText, framesOnSingleLine));
                    }
                }
                this.PercentComplete = StackResolver.Operation100Percent;
                return allStacks;
            });
        }

        /// Function executed by worker threads to process callstacks. Threads work on portions of the listOfCallStacks based on their thread ordinal.
        private async Task ProcessCallStack(int threadOrdinal, int numThreads, List<StackDetails> listOfCallStacks, Dictionary<string, string> moduleNamesMap,
        string symPath, List<string> dllPaths, bool searchPDBsRecursively, bool searchDLLRecursively,
        bool includeSourceInfo, bool showInlineFrames, bool relookupSource, bool includeOffsets, bool cachePDB, CancellationTokenSource cts) {
            await Task.Run(() => {
                if (!SafeNativeMethods.EstablishActivationContext()) return;
                var _diautils = new Dictionary<string, DiaUtil>();
                var modulesToIgnore = new List<string>();

                for (int tmpStackIndex = 0; tmpStackIndex < listOfCallStacks.Count; tmpStackIndex++) {
                    if (cts.IsCancellationRequested) break;
                    if (tmpStackIndex % numThreads != threadOrdinal) continue;

                    var currstack = listOfCallStacks[tmpStackIndex];
                    var ordinalResolvedFrames = this.dllMapHelper.LoadDllsIfApplicable(currstack.CallstackFrames, searchDLLRecursively, dllPaths);
                    // process any frames which are purely virtual address (in such cases, the caller should have specified base addresses)
                    var callStackLines = this.LoadedModules.Any() ? PreProcessVAs(ordinalResolvedFrames, cts) : ordinalResolvedFrames;
                    if (cts.IsCancellationRequested) return;

                    // resolve symbols by using DIA
                    currstack.Resolvedstack = ResolveSymbols(_diautils, moduleNamesMap, callStackLines, symPath, searchPDBsRecursively, cachePDB,  includeSourceInfo, relookupSource, includeOffsets, showInlineFrames, modulesToIgnore, cts);
                    if (cts.IsCancellationRequested) return;

                    var localCounter = Interlocked.Increment(ref this.globalCounter);
                    this.PercentComplete = (int)((double)localCounter / listOfCallStacks.Count * 100.0);
                }

                // cleanup any older COM objects
                if (_diautils != null) {
                    foreach (var diautil in _diautils.Values) diautil.Dispose();
                    _diautils.Clear();
                }

                SafeNativeMethods.DestroyActivationContext();
            });
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) disposedValue = true;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
