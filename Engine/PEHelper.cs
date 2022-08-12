// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    internal class PEHelper {
        public static int RvaToOffset(int virtualAddress, ImmutableArray<SectionHeader> sections) {
            var section = sections.Where(s => s.VirtualAddress <= virtualAddress && virtualAddress < s.VirtualAddress + s.VirtualSize);
            return section.Any() ? section.First().PointerToRawData + virtualAddress - section.First().VirtualAddress : -1;
        }
    }
}
