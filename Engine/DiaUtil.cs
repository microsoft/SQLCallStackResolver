// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    /// Wrapper class around DIA
    internal class DiaUtil {
        internal readonly IDiaDataSource _IDiaDataSource;
        internal readonly IDiaSession _IDiaSession;
        private bool disposedValue = false;
        internal readonly bool HasSourceInfo = false;
        private static readonly object _syncRoot = new();

        internal DiaUtil(string pdbName) {
            try {
                _IDiaDataSource = new DiaSource();
                _IDiaDataSource.loadDataFromPdb(pdbName);
                _IDiaDataSource.openSession(out _IDiaSession);
                this._IDiaSession.findChildrenEx(this._IDiaSession.globalScope, SymTagEnum.SymTagFunction, null, 0, out IDiaEnumSymbols matchedSyms);
                foreach (IDiaSymbol sym in matchedSyms) {
                    this._IDiaSession.findLinesByRVA(sym.relativeVirtualAddress, (uint)sym.length, out IDiaEnumLineNumbers enumLineNums);
                    Marshal.FinalReleaseComObject(sym);
                    if (enumLineNums.count > 0) { // this PDB has at least 1 function with source info, so end the search
                        HasSourceInfo = true;
                        break;
                    }
                    Marshal.FinalReleaseComObject(enumLineNums);
                }
                Marshal.FinalReleaseComObject(matchedSyms);
            } catch (COMException) {
                ReleaseDiaObjects();
                throw;
            }
        }

        public void Dispose() {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) ReleaseDiaObjects();
                disposedValue = true;
            }
        }

        private void ReleaseDiaObjects() {
            if (null != _IDiaSession) Marshal.FinalReleaseComObject(_IDiaSession);
            if (null != _IDiaDataSource) Marshal.FinalReleaseComObject(_IDiaDataSource);
        }

        /// This function builds up the PDB map, by searching for matched PDBs (based on name) and constructing the DIA session for each
        internal static bool LocateandLoadPDBs(string currentModule, string pdbFileName, Dictionary<string, DiaUtil> _diautils, string userSuppliedSymPath, string symSrvSymPath, bool recurse, bool cachePDB, List<string> modulesToIgnore, out string errorDetails) {
            var completeSymPath = $"{symSrvSymPath};{userSuppliedSymPath}";
            // loop through each module, trying to find matched PDB files
            if (!modulesToIgnore.Contains(currentModule) && !_diautils.ContainsKey(currentModule)) {
                // we only need to search for the PDB if it does not already exist in our map
                var cachedPDBFile = Path.Combine(Path.GetTempPath(), "SymCache", currentModule + ".pdb");
                lock (_syncRoot) {  // the lock is needed to ensure that we do not make multiple copies of PDBs when cachePDB is true
                    if (!File.Exists(cachedPDBFile)) {
                        IEnumerable<string> foundFiles = new List<string>();
                        foreach (var currPath in completeSymPath.Split(';').Where(p => Directory.Exists(p))) {
                            foundFiles = Directory.EnumerateFiles(currPath, currentModule + ".pdb", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                            if (!foundFiles.Any()) {
                                // repeat the search but with a more relaxed filter. this (somewhat hacky) consideration is required
                                // for modules like vcruntime140.dll where the PDB name is actually vcruntime140.amd64.pdb
                                foundFiles = Directory.EnumerateFiles(currPath, currentModule + ".*.pdb", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                                if (foundFiles.Any()) break;
                            } else break;

                            if (currPath.EndsWith(currentModule)) { // search for subfolder with PDB GUID as the name
                                foundFiles = Directory.EnumerateFiles(currPath, "*.pdb", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                                if (foundFiles.Any()) break;
                            }
                        }

                        // if needed, make a last attempt looking for the original module name - but only amongst user-supplied symbol path folder(s)
                        if (!foundFiles.Any()) foreach (var currPath in userSuppliedSymPath.Split(';').Where(p => Directory.Exists(p) && !p.EndsWith(currentModule))) {
                                foundFiles = Directory.EnumerateFiles(currPath, pdbFileName, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                            }

                        if (foundFiles?.Count() == 1) {  // we need to be sure there is only 1 file which matches
                            if (cachePDB) File.Copy(foundFiles.First(), cachedPDBFile);
                            else cachedPDBFile = foundFiles.First();
                        }
                    }
                }

                if (File.Exists(cachedPDBFile)) {
                    try {
                        _diautils.Add(currentModule, new DiaUtil(cachedPDBFile));
                    } catch (COMException) {
                        errorDetails = cachedPDBFile;
                        return false;
                    }
                } else if (!modulesToIgnore.Contains(currentModule)) modulesToIgnore.Add(currentModule);
            }
            errorDetails = string.Empty;
            return true;
        }

        /// Internal helper function to return the symbolized frame text (not including source info)
        internal static string GetSymbolizedFrame(string moduleName, IDiaSymbol mysym, bool useUndecorateLogic, bool includeOffset, int displacement, bool isInLinee) {
            string funcname2;
            if (!useUndecorateLogic) funcname2 = mysym.name;
            else {
                // refer https://msdn.microsoft.com/en-us/library/kszfk0fs.aspx
                // UNDNAME_NAME_ONLY == 0x1000: Gets only the name for primary declaration; returns just [scope::]name. Expands template params. 
                mysym.get_undecoratedNameEx(0x1000, out funcname2);
                // catch-all / fallback
                if (string.IsNullOrEmpty(funcname2)) funcname2 = mysym.name;
            }

            string offsetStr = string.Empty;
            if (includeOffset) offsetStr = string.Format(CultureInfo.CurrentCulture, "+{0}", displacement);
            var inlineePrefix = isInLinee ? "(Inline Function) " : string.Empty;

            return $"{inlineePrefix}{moduleName}!{funcname2}{offsetStr}";
        }

        /// Internal helper function to obtain source information for given symbol
        internal static string GetSourceInfo(IDiaEnumLineNumbers enumLineNums, bool pdbHasSourceInfo) {
            var sbOutput = new StringBuilder();

            // only if we found line number information should we append to output 
            if (enumLineNums.count > 0) {
                for (uint tmpOrdinal = 0; tmpOrdinal < enumLineNums.count; tmpOrdinal++) {
                    if (tmpOrdinal > 0) sbOutput.Append($" {StackResolver.WARNING_PREFIX} multiple matches -- ");
                    sbOutput.Append(string.Format(CultureInfo.CurrentCulture,
                        "({0}:{1})", enumLineNums.Item(tmpOrdinal).sourceFile.fileName,
                        enumLineNums.Item(tmpOrdinal).lineNumber));

                    Marshal.FinalReleaseComObject(enumLineNums.Item(tmpOrdinal).sourceFile);
                    Marshal.FinalReleaseComObject(enumLineNums.Item(tmpOrdinal));
                }
            }
            else if (pdbHasSourceInfo) sbOutput.Append($"{StackResolver.WARNING_PREFIX} unable to find source info --");
            Marshal.FinalReleaseComObject(enumLineNums);
            return sbOutput.ToString();
        }

        /// Internal helper function to find any inline frames at a given RVA
        internal static string ProcessInlineFrames(string moduleName, bool useUndecorateLogic, bool includeOffset, bool includeSourceInfo, uint rva, IDiaSymbol parentSym, bool pdbHasSourceInfo) {
            var sbInline = new StringBuilder();
            try {
                var inlineRVA = rva - 1;
                parentSym.findInlineFramesByRVA(inlineRVA, out IDiaEnumSymbols enumInlinees);
                int inlineeIndex = 0;
                foreach (IDiaSymbol inlineFrame in enumInlinees) {
                    var inlineeOffset = (int)(rva - inlineFrame.relativeVirtualAddress);
                    sbInline.Append(DiaUtil.GetSymbolizedFrame(moduleName, inlineFrame, useUndecorateLogic, includeOffset, inlineeOffset, true));
                    if (includeSourceInfo) {
                        inlineFrame.findInlineeLinesByRVA(inlineRVA, 0, out IDiaEnumLineNumbers enumLineNums);
                        sbInline.Append("\t");
                        sbInline.Append(DiaUtil.GetSourceInfo(enumLineNums, pdbHasSourceInfo));
                        Marshal.FinalReleaseComObject(enumLineNums);
                    }
                    inlineeIndex++;
                    Marshal.FinalReleaseComObject(inlineFrame);
                    sbInline.AppendLine();
                }
                Marshal.FinalReleaseComObject(enumInlinees);
            } catch (COMException) {
                sbInline.AppendLine($" {StackResolver.WARNING_PREFIX} Unable to process inline frames; maybe symbols are mismatched?");
            } catch (System.ArgumentException) {
                sbInline.AppendLine($" {StackResolver.WARNING_PREFIX} Unable to process inline frames; maybe symbols are mismatched?");
            }

            return sbInline.ToString();
        }
    }
}
