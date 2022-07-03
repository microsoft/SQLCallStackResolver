// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    /// PE header's Image Export Directory
    [StructLayout(LayoutKind.Sequential)]
    public struct ImageExportDirectory {
        public uint Characteristics, TimeDateStamp;
        public ushort MajorVersion, MinorVersion;
        public int Name, Base, NumberOfFunctions, NumberOfNames, AddressOfFunctions, AddressOfNames, AddressOfOrdinals;
    }
}
