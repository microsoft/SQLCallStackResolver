// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    public class StackResolver : IDisposable {
        /// This is used to store module name and start / end virtual address ranges
        /// Only populated if the user provides a tab-separated string corresponding to the output of the following SQL query:
        /// select name, base_address from sys.dm_os_loaded_modules where name not like '%.rll'
        public List<ModuleInfo> LoadedModules = new();
        /// A cache of already resolved addresses
        private readonly Dictionary<string, string> cachedSymbols = new();
        /// R/W lock to protect the above cached symbols dictionary
        private readonly ReaderWriterLockSlim rwLockCachedSymbols = new();
        private readonly DLLOrdinalHelper dllMapHelper = new();
        /// Status message - populated during associated long-running operations
        public string StatusMessage;
        /// Internal counter used to implement progress reporting
        internal int globalCounter = 0;
        /// Percent completed - populated during associated long-running operations
        public int PercentComplete;

        public Task<Tuple<List<string>, List<string>>> GetDistinctXELFieldsAsync(string[] xelFiles, int eventsToSample, CancellationTokenSource cts) {
            return XELHelper.GetDistinctXELActionsFieldsAsync(xelFiles, eventsToSample, cts);
        }

        /// Public method which to help import XEL files
        public async Task<Tuple<int, string>> ExtractFromXELAsync(string[] xelFiles, bool groupEvents, List<string> relevantFields, CancellationTokenSource cts) {
            return await XELHelper.ExtractFromXELAsync(this, xelFiles, groupEvents, relevantFields, cts);
        }

        /// Convert virtual-address only type frames to their module+offset format
        private string[] PreProcessVAs(string[] callStackLines, Regex rgxVAOnly, CancellationTokenSource cts) {
            if (!this.LoadedModules.Any()) return callStackLines;// only makes sense doing the rest of the work in this function if have loaded module information

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
            if (!Regex.Match(text, "Histogram").Success && !text.Replace("\r", string.Empty).Trim().Contains('\n')) return true;
            if (!Regex.Match(text, @"\<frame").Success) {
                if (Regex.Match(text, @"\<Slot.+\<\/Slot\>").Success) return true;
                if (Regex.Match(text, @"0x.+0x.+").Success) return true;
            }
            return false;
        }

        /// Runs through each of the frames in a call stack and looks up symbols for each
        private string ResolveSymbols(Dictionary<string, DiaUtil> _diautils, string[] callStackLines, bool includeSourceInfo, bool relookupSource, bool includeOffsets, bool showInlineFrames, Regex rgxAlreadySymbolizedFrame, Regex rgxModuleName, List<string> modulesToIgnore, ThreadParams tp) {
            var finalCallstack = new StringBuilder();
            int frameNum = int.MinValue;
            foreach (var iterFrame in callStackLines) {
                if (tp.cts.IsCancellationRequested) return "Operation cancelled.";
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
                        if (!_diautils.ContainsKey(matchedModuleName)) DiaUtil.LocateandLoadPDBs(_diautils, tp.symPath, tp.searchPDBsRecursively, new List<string>() { matchedModuleName }, tp.cachePDB, modulesToIgnore);
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
                    if (!_diautils.ContainsKey(matchedModuleName)) DiaUtil.LocateandLoadPDBs(_diautils, tp.symPath, tp.searchPDBsRecursively, new List<string>() { matchedModuleName }, tp.cachePDB, modulesToIgnore);
                    frameNum = string.IsNullOrWhiteSpace(match.Groups["framenum"].Value) ? int.MinValue : frameNum == int.MinValue ? Convert.ToInt32(match.Groups["framenum"].Value, 16) : frameNum;
                    if (_diautils.ContainsKey(matchedModuleName)) {
                        string processedFrame = ProcessFrameModuleOffset(_diautils, ref frameNum, matchedModuleName, match.Groups["offset"].Value, includeSourceInfo, includeOffsets, showInlineFrames);
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
        private string ProcessFrameModuleOffset(Dictionary<string, DiaUtil> _diautils, ref int frameNum, string moduleName, string offset, bool includeSourceInfo, bool includeOffset, bool showInlineFrames) {
            bool useUndecorateLogic = false;

            // the offsets in the XE output are in hex, so we convert to base-10 accordingly
            var rva = Convert.ToUInt32(offset, 16);
            var symKey = moduleName + rva.ToString(CultureInfo.CurrentCulture);
            this.rwLockCachedSymbols.EnterReadLock();
            bool resWasCached = this.cachedSymbols.TryGetValue(symKey, out string result);
            this.rwLockCachedSymbols.ExitReadLock();
            if (!resWasCached) {
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
                if (showInlineFrames && pdbHasSourceInfo && !sourceInfo.Contains("-- WARNING:")) {
                    inlineFrameAndSourceInfo = DiaUtil.ProcessInlineFrames(moduleName, useUndecorateLogic, includeOffset, includeSourceInfo, rva, mysym, pdbHasSourceInfo);
                }

                var symbolizedFrame = DiaUtil.GetSymbolizedFrame(moduleName, mysym, useUndecorateLogic, includeOffset, displacement, false);

                // make sure we cleanup COM allocations for the resolved sym
                Marshal.FinalReleaseComObject(mysym);
                result = (inlineFrameAndSourceInfo + symbolizedFrame + "\t" + sourceInfo).Trim();
                if (!resWasCached) {    // we only need to add to cache if it was not already cached.
                    this.rwLockCachedSymbols.EnterWriteLock();
                    if (!this.cachedSymbols.ContainsKey(symKey)) {
                        this.cachedSymbols.Add(symKey, result);
                    }
                    this.rwLockCachedSymbols.ExitWriteLock();
                }
            }

            if (frameNum != int.MinValue) {
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
            if (string.IsNullOrEmpty(baseAddressesString)) {
                // changed this to return true because this is not a true error condition
                return true;
            }
            LoadedModules.Clear();
            var rgxmoduleaddress = new Regex(@"^\s*(?<filepath>.+)(\t+| +)(?<baseaddress>(0x)?[0-9a-fA-F`]+)\s*$", RegexOptions.Multiline);
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
                // typically errors with non-numeric info passed to Convert.ToUInt64
                retVal = false;
            } catch (OverflowException) {
                // absurdly large numeric info passed to Convert.ToUInt64
                retVal = false;
            } catch (ArgumentException) {
                // typically these are malformed paths passed to Path.GetFileNameWithoutExtension
                retVal = false;
            }

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
            this.cachedSymbols.Clear();

            // delete and recreate the cached PDB folder
            var symCacheFolder = Path.Combine(Path.GetTempPath(), "SymCache");
            if (Directory.Exists(symCacheFolder)) {
                new DirectoryInfo(symCacheFolder).GetFiles("*", SearchOption.AllDirectories).ToList().ForEach(file => file.Delete());
            }
            else Directory.CreateDirectory(symCacheFolder);

            this.StatusMessage = "Checking for embedded symbol information...";
            var syms = await ModuleInfoHelper.ParseModuleInfoAsync(listOfCallStacks, cts);
            if (cts.IsCancellationRequested) return "Operation cancelled.";

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
                if (cts.IsCancellationRequested) return "Operation cancelled.";
                if (syms.Count() > 0) {
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
            this.StatusMessage = "Creating thread pool to process frames...";
            int numThreads = Math.Min(listOfCallStacks.Count, Environment.ProcessorCount);
            List<Thread> threads = new();
            for (int threadOrdinal = 0; threadOrdinal < numThreads; threadOrdinal++) {
                var tmpThread = new Thread(ProcessCallStack);
                threads.Add(tmpThread);
                tmpThread.Start(new ThreadParams() {dllPaths = dllPaths, includeOffsets = includeOffsets,includeSourceInfo = includeSourceInfo,
                    showInlineFrames = showInlineFrames, listOfCallStacks = listOfCallStacks, numThreads = numThreads, relookupSource = relookupSource, searchDLLRecursively = searchDLLRecursively,
                    searchPDBsRecursively = searchPDBsRecursively, symPath = symPath, threadOrdinal = threadOrdinal, cachePDB = cachePDB, cts = cts});
            }

            this.StatusMessage = "Waiting for threads to finish...";
            threads.ForEach(tmpThread => tmpThread.Join());
            if (cts.IsCancellationRequested) return "Operation cancelled.";
            this.StatusMessage = "Done with symbol resolution, finalizing output...";
            this.globalCounter = 0;

            var finalCallstack = new StringBuilder();
            // populate the output
            if (!string.IsNullOrEmpty(outputFilePath)) {
                this.StatusMessage = $@"Writing output to file {outputFilePath}";
                using var outStream = new StreamWriter(outputFilePath, false);
                foreach (var currstack in listOfCallStacks) {
                    if (cts.IsCancellationRequested) return "Operation cancelled.";
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
                    if (cts.IsCancellationRequested) return "Operation cancelled.";
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

            this.StatusMessage = "Finished!";
            return string.IsNullOrEmpty(outputFilePath) ? finalCallstack.ToString() : $@"Output has been saved to {outputFilePath}";
        }

        /// <summary>
        /// Gets a list of StackDetails objects based on the textual callstack input
        /// </summary>
        /// <param name="inputCallstackText"></param>
        /// <param name="framesOnSingleLine"></param>
        /// <param name="cts"></param>
        /// <returns>List of StackDetails objects</returns>
        public List<StackDetails> GetListofCallStacks(string inputCallstackText, bool framesOnSingleLine, CancellationTokenSource cts) {
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
                this.StatusMessage = "Inspecting input to determine processing plan...";
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

            return allStacks;
        }

        /// Function executed by worker threads to process callstacks. Threads work on portions of the listOfCallStacks based on their thread ordinal.
        private void ProcessCallStack(Object obj) {
            SafeNativeMethods.EstablishActivationContext();
            var tp = (ThreadParams)obj;
            var _diautils = new Dictionary<string, DiaUtil>();
            var rgxOptions = tp.listOfCallStacks.Count > 10 ? RegexOptions.Compiled : RegexOptions.None;
            var rgxModuleName = new Regex(@"((?<framenum>[0-9a-fA-F]+)\s+)*(?<module>\w+)(\.(dll|exe))*\s*\+\s*(0[xX])*(?<offset>[0-9a-fA-F]+)\s*", rgxOptions);
            var rgxVAOnly = new Regex(@"^\s*0[xX](?<vaddress>[0-9a-fA-F]+)\s*$", rgxOptions);
            var rgxAlreadySymbolizedFrame = new Regex(@"((?<framenum>\d+)\s+)*(?<module>\w+)(\.(dll|exe))*!(?<symbolizedfunc>.+?)\s*\+\s*(0[xX])*(?<offset>[0-9a-fA-F]+)\s*", rgxOptions);
            var modulesToIgnore = new List<string>();

            for (int tmpStackIndex = 0; tmpStackIndex < tp.listOfCallStacks.Count; tmpStackIndex++) {
                if (tp.cts.IsCancellationRequested) break;
                if (tmpStackIndex % tp.numThreads != tp.threadOrdinal) continue;

                var currstack = tp.listOfCallStacks[tmpStackIndex];
                var ordinalResolvedFrames = this.dllMapHelper.LoadDllsIfApplicable(currstack.CallstackFrames, tp.searchDLLRecursively, tp.dllPaths);
                // process any frames which are purely virtual address (in such cases, the caller should have specified base addresses)
                var callStackLines = PreProcessVAs(ordinalResolvedFrames, rgxVAOnly, tp.cts);
                if (tp.cts.IsCancellationRequested) return;

                // resolve symbols by using DIA
                currstack.Resolvedstack = ResolveSymbols(_diautils, callStackLines, tp.includeSourceInfo, tp.relookupSource, tp.includeOffsets, tp.showInlineFrames, rgxAlreadySymbolizedFrame, rgxModuleName, modulesToIgnore, tp);
                if (tp.cts.IsCancellationRequested) return;

                var localCounter = Interlocked.Increment(ref this.globalCounter);
                this.PercentComplete = (int)((double)localCounter / tp.listOfCallStacks.Count * 100.0);
            }

            // cleanup any older COM objects
            if (_diautils != null) {
                foreach (var diautil in _diautils.Values) {
                    diautil.Dispose();
                }

                _diautils.Clear();
            }

            SafeNativeMethods.DestroyActivationContext();
        }

        static readonly string[] wellKnownModuleNames = new string[] { "ntdll", "kernel32", "kernelbase", "ntoskrnl", "sqldk", "sqlmin", "sqllang", "sqltses", "sqlaccess", "qds", "hkruntime", "hkengine", "hkcompile", "sqlos", "sqlservr", "SqlServerSpatial", "SqlServerSpatial110", "SqlServerSpatial120", "SqlServerSpatial130", "SqlServerSpatial140", "SqlServerSpatial150" };

        /// <summary>
        /// This method generates a PowerShell script to automate download of matched PDBs from the public symbol server.
        /// </summary>
        public static List<Symbol> GetSymbolDetailsForBinaries(List<string> dllPaths, bool recurse, List<Symbol> existingSymbols = null) {
            if (dllPaths == null || dllPaths.Count == 0) return new List<Symbol>();

            var symbolsFound = new List<Symbol>();
            foreach (var currentModule in wellKnownModuleNames) {
                if (null != existingSymbols) {
                    var syms = existingSymbols.Where(s => string.Equals(s.PDBName, currentModule, StringComparison.InvariantCultureIgnoreCase));
                    if (syms.Any()) {
                        symbolsFound.Add(syms.First());
                        continue;
                    }
                }
                var search = dllPaths.Where(p => Directory.Exists(p)).SelectMany(currPath => Directory.EnumerateFiles(currPath, currentModule + ".*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    .Where(f => f.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase) || f.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase));
                if (search.Any()) {
                    using var dllFileStream = new FileStream(search.First(), FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var reader = new PEReader(dllFileStream);
                    var lastPdbInfo = PEHelper.ReadPdbs(reader).Last();
                    var internalPDBName = lastPdbInfo.Path;
                    var pdbGuid = lastPdbInfo.Guid;
                    var pdbAge = lastPdbInfo.Age;
                    var usablePDBName = Path.GetFileNameWithoutExtension(internalPDBName);
                    var fileVer = FileVersionInfo.GetVersionInfo(search.First()).FileVersion;
                    var newSymbol = new Symbol() {
                        PDBName = usablePDBName, InternalPDBName = internalPDBName,
                        DownloadURL = string.Format(CultureInfo.CurrentCulture, @"https://msdl.microsoft.com/download/symbols/{0}.pdb/{1}/{0}.pdb",
                            usablePDBName, pdbGuid.ToString("N", CultureInfo.CurrentCulture) + pdbAge.ToString(CultureInfo.CurrentCulture)), FileVersion = fileVer
                    };
                    newSymbol.DownloadVerified = Symbol.IsURLValid(new Uri(newSymbol.DownloadURL));
                    symbolsFound.Add(newSymbol);
                }
            }

            return symbolsFound;
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) rwLockCachedSymbols.Dispose();
                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
