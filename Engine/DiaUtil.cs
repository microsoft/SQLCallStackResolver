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
        internal static bool LocateandLoadPDBs(Dictionary<string, DiaUtil> _diautils, string rootPaths, bool recurse, Dictionary<string, string> moduleNamesMap, bool cachePDB, List<string> modulesToIgnore) {
            var moduleNames = moduleNamesMap.Keys;
            // loop through each module, trying to find matched PDB files
            foreach (string currentModule in moduleNames.Where(m => !modulesToIgnore.Contains(m) && !_diautils.ContainsKey(m))) {
                // we only need to search for the PDB if it does not already exist in our map
                var cachedPDBFile = Path.Combine(Path.GetTempPath(), "SymCache", currentModule + ".pdb");
                lock (_syncRoot) {  // the lock is needed to ensure that we do not make multiple copies of PDBs when cachePDB is true
                    if (!File.Exists(cachedPDBFile)) {
                        foreach (var currPath in rootPaths.Split(';').Where(p => Directory.Exists(p))) {
                            var foundFiles = Directory.EnumerateFiles(currPath, currentModule + ".pdb", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                            if (!foundFiles.Any()) {
                                // repeat the search but with a more relaxed filter. this (somewhat hacky) consideration is required
                                // for modules like vcruntime140.dll where the PDB name is actually vcruntime140.amd64.pdb
                                foundFiles = Directory.EnumerateFiles(currPath, currentModule + ".*.pdb", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                            }

                            if (!foundFiles.Any() && currPath.EndsWith(currentModule)) { // search for subfolder with PDB GUID as the name
                                foundFiles = Directory.EnumerateFiles(currPath, "*.pdb", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                            }

                            if (foundFiles.Count() == 1) {  // we need to be sure there is only 1 file which matches
                                if (cachePDB) File.Copy(foundFiles.First(), cachedPDBFile);
                                else cachedPDBFile = foundFiles.First();
                                break;
                            }
                        }
                    }
                }

                if (File.Exists(cachedPDBFile)) {
                    try {
                        _diautils.Add(currentModule, new DiaUtil(cachedPDBFile));
                    } catch (COMException) {
                        return false;
                    }
                } else if (!modulesToIgnore.Contains(currentModule)) modulesToIgnore.Add(currentModule);
            }
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
            // replace any "unique" module names with their original values
            

            return $"{inlineePrefix}{moduleName}!{funcname2}{offsetStr}";
        }

        /// Internal helper function to obtain source information for given symbol
        internal static string GetSourceInfo(IDiaEnumLineNumbers enumLineNums, bool pdbHasSourceInfo) {
            var sbOutput = new StringBuilder();

            // only if we found line number information should we append to output 
            if (enumLineNums.count > 0) {
                for (uint tmpOrdinal = 0; tmpOrdinal < enumLineNums.count; tmpOrdinal++) {
                    if (tmpOrdinal > 0) sbOutput.Append(" -- WARNING: multiple matches -- ");
                    sbOutput.Append(string.Format(CultureInfo.CurrentCulture,
                        "({0}:{1})", enumLineNums.Item(tmpOrdinal).sourceFile.fileName,
                        enumLineNums.Item(tmpOrdinal).lineNumber));

                    Marshal.FinalReleaseComObject(enumLineNums.Item(tmpOrdinal).sourceFile);
                    Marshal.FinalReleaseComObject(enumLineNums.Item(tmpOrdinal));
                }
            }
            else if (pdbHasSourceInfo) sbOutput.Append("-- WARNING: unable to find source info --");
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
                sbInline.AppendLine(" -- WARNING: Unable to process inline frames; maybe symbols are mismatched?");
            } catch (System.ArgumentException) {
                sbInline.AppendLine(" -- WARNING: Unable to process inline frames; maybe symbols are mismatched?");
            }

            return sbInline.ToString();
        }
    }
}
