// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
using System.Globalization;

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    /// <summary>
    /// Helper class to store module name, start and end address
    /// </summary>
    public class ModuleInfo {
        public string ModuleName;
        public ulong BaseAddress;
        public ulong EndAddress;

        public override string ToString() {
            return string.Format(CultureInfo.CurrentCulture, "{0} from {1:X} to {2:X}", ModuleName, BaseAddress, EndAddress);
        }
    }
}
