// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    class DLLOrdinalHelper {
        /// This holds the mapping of the various DLL exports for a module and the address (offset) for each such export
        /// Only populated if the user provides the 'image path' to the DLLs
        Dictionary<string, Dictionary<int, ExportedSymbol>> _DLLOrdinalMap;

        internal void Initialize() {
            _DLLOrdinalMap = new Dictionary<string, Dictionary<int, ExportedSymbol>>();
        }

        /// This function loads DLLs from a specified path, so that we can then build the DLL export's ordinal / address map
        internal string[] LoadDllsIfApplicable(string[] callstackFrames, bool recurse, List<string> dllPaths) {
            if (dllPaths == null) {
                return callstackFrames;
            }

            var processedFrames = new string[callstackFrames.Length];
            for (var idx = 0; idx < callstackFrames.Length; idx++) {
                var callstack = callstackFrames[idx];
                // first we seek out distinct module names in this call stack
                // note that such frames will only be seen in the call stack when trace flag 3656 is enabled, but there were no PDBs in the BINN folder
                // sample frames are given below
                // sqldk.dll!Ordinal947+0x25f
                // sqldk.dll!Ordinal699 + 0x5f
                // sqlmin.dll!Ordinal1634 + 0x76c
                // More recent patterns which we choose not to support, because in these cases the module+offset is cleanly represented and it does symbolize nicely
                // 00007FF818405E70 Module(sqlmin+0000000001555E70) (Ordinal1877 + 00000000000004B0)
                // 00007FF81840226A Module(sqlmin+000000000155226A) (Ordinal1261 + 00000000000071EA)
                // 00007FF81555A663 Module(sqllang+0000000000C6A663) (Ordinal1203 + 0000000000005E33)
                // define a regex to identify such ordinal based frames
                var rgxOrdinalNotation = new Regex(@"(?<module>\w+)(\.dll)*!Ordinal(?<ordinal>[0-9]+)\s*\+\s*(0[xX])*");
                var matchednotations = rgxOrdinalNotation.Matches(callstack);
                var moduleNames = new List<string>();
                moduleNames.AddRange(from Match match in matchednotations let currmodule = match.Groups["module"].Value where !moduleNames.Contains(currmodule) select currmodule);

                // then we see if there is a matched DLL in any of the paths we have
                foreach (var currmodule in moduleNames) {
                    foreach (var currPath in dllPaths) {
                        var foundFiles = Directory.EnumerateFiles(currPath, currmodule + ".dll", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                        lock (_DLLOrdinalMap) {
                            if (!_DLLOrdinalMap.ContainsKey(currmodule) && foundFiles.Any()) {
                                _DLLOrdinalMap.Add(currmodule, ExportedSymbol.GetExports(foundFiles.First()));

                                break;
                            }
                        }
                    }
                }

                // finally do a pattern based replace the replace method calls a delegate (ReplaceOrdinalWithRealOffset) which figures
                // out the start address of the ordinal and then computes the actual offset
                var fullpattern = new Regex(@"(?<module>\w+)(\.dll)*!Ordinal(?<ordinal>[0-9]+)\s*\+\s*(0[xX])*(?<offset>[0-9a-fA-F]+)\s*");
                processedFrames[idx] = fullpattern.Replace(callstack, ReplaceOrdinalWithRealOffset);
            }

            return processedFrames;
        }

        /// This delegate is invoked by the Replace function and is used to compute the effective offset from module load address
        /// based on ordinal start address and original offset
        private string ReplaceOrdinalWithRealOffset(Match mtch) {
            var moduleName = mtch.Groups["module"].Value;
            if (!_DLLOrdinalMap.ContainsKey(moduleName)) {
                return mtch.Value;
            }

            uint offsetSpecified = Convert.ToUInt32(mtch.Groups["offset"].Value, 16);
            return string.Format(CultureInfo.CurrentCulture, "{0}.dll+0x{1:X}{2}", moduleName,
                _DLLOrdinalMap[moduleName][int.Parse(mtch.Groups["ordinal"].Value, CultureInfo.CurrentCulture)].Address + offsetSpecified, Environment.NewLine);
        }
    }
}
