// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    public static class SymSrvHelpers {
        /// Private method to locate the local path for a matching PDB. Implicitly handles symbol download if needed.
        private static string GetLocalSymbolFolderForModule(string pdbFilename, string pdbGuid, int pdbAge, string symPath) {
            const int MAX_PATH = 4096;
            StringBuilder outPath = new(MAX_PATH);
            var guid = Guid.Parse(pdbGuid);
            int rawsize = Marshal.SizeOf(guid);
            IntPtr buffer = Marshal.AllocHGlobal(rawsize);
            Marshal.StructureToPtr(guid, buffer, false);
            bool success = SafeNativeMethods.SymFindFileInPath((IntPtr)(-1), null, pdbFilename, buffer, pdbAge, 0, 8, outPath, IntPtr.Zero, IntPtr.Zero);
            if (!success)  return String.Empty;
            return outPath.ToString();
        }

        /// <summary>
        /// Public method to return local PDB file paths for specified symbols.
        /// </summary>
        public static List<string> GetFolderPathsForPDBs(StackResolver parent, string symPath, List<Symbol> syms) {
            var retval = new List<string>();
            Contract.Requires(null != syms);
            Contract.Requires(null != parent);

            if (!SafeNativeMethods.SymSetParentWindow(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle)) return retval;

            if (!SafeNativeMethods.SymInitialize((IntPtr)(-1), symPath, false)) return retval;
            int progress = 0;
            foreach (var sym in syms) {
                parent.StatusMessage = string.Format(CultureInfo.CurrentCulture, $"Finding local PDB path for {sym.PDBName}");
                var path = GetLocalSymbolFolderForModule(sym.PDBName, sym.PDBGuid, sym.PDBAge, symPath);
                if (!string.IsNullOrEmpty(path)) {
                    retval.Add(Path.GetDirectoryName(path));
                    parent.StatusMessage = string.Format(CultureInfo.CurrentCulture, $"Successfully found local PDB at {path}");
                }
                else parent.StatusMessage = string.Format(CultureInfo.CurrentCulture, $"Could not find local PDB for {sym.PDBName}");

                progress++;
                parent.PercentComplete = (int)((double)progress / syms.Count * 100.0);
            }
            SafeNativeMethods.SymCleanup((IntPtr)(-1));
            return retval;
        }
    }
}
