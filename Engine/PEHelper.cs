// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    internal class PdbInfo {
        internal string Path;
        internal Guid Guid;
        internal int Age;
    }

    internal class PEHelper {
        internal static List<PdbInfo> ReadPdbs(PEReader reader) {
            var debugDirectories = reader.ReadDebugDirectory();
            return new List<PdbInfo>(debugDirectories.Where(entry => entry.Type == DebugDirectoryEntryType.CodeView)
                .Select(entry => reader.ReadCodeViewDebugDirectoryData(entry))
                .Select(data => new PdbInfo() { Path = data.Path, Guid = data.Guid, Age = data.Age }));
        }

        public static int RvaToOffset(int virtualAddress, ImmutableArray<SectionHeader> sections) {
            var section = sections.Where(s => s.VirtualAddress <= virtualAddress && virtualAddress < s.VirtualAddress + s.VirtualSize);
            return section.Any() ? section.First().PointerToRawData + virtualAddress - section.First().VirtualAddress : -1;
        }
    }
}
