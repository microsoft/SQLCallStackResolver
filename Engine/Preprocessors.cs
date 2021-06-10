// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    internal class Preprocessors {
        /// Find out distinct module names in a given stack. This is used to later load PDBs and optionally DLLs
        internal static List<string> EnumModuleNames(string[] callStack) {
            List<string> uniqueModuleNames = new List<string>();
            var reconstructedCallstack = new StringBuilder();
            foreach (var frame in callStack) {
                reconstructedCallstack.AppendLine(frame);
            }

            // using the ".dll!0x" to locate the module names
            var rgxModuleName = new Regex(@"(?<module>\w+)((\.(dll|exe))*(!(?<symbolizedfunc>.+))*)*\s*\+(0[xX])*");
            var matchedModuleNames = rgxModuleName.Matches(reconstructedCallstack.ToString());

            foreach (Match moduleMatch in matchedModuleNames) {
                var actualModuleName = moduleMatch.Groups["module"].Value;

                if (!uniqueModuleNames.Contains(actualModuleName)) {
                    uniqueModuleNames.Add(actualModuleName);
                }
            }

            return uniqueModuleNames;
        }
    }
}
