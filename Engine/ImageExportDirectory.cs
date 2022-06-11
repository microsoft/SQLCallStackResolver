// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using System.Runtime.InteropServices;

    /// PE header's Image Export Directory
    [StructLayout(LayoutKind.Sequential)]
    #pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct ImageExportDirectory
        #pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public uint Characteristics, TimeDateStamp;
        public ushort MajorVersion, MinorVersion;
        public int Name, Base, NumberOfFunctions, NumberOfNames, AddressOfFunctions, AddressOfNames, AddressOfOrdinals;
    }
}
