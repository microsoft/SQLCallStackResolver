// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    /// <summary>
    /// Helper class which stores DLL export name and address (offset)
    /// </summary>
    public class ExportedSymbol {
        public string OrdinalName { get; set; }
        public ulong Address { get; set; }

        /// <summary>
        /// Helper function to load a DLL and then lookup exported functions.
        /// </summary>
        public static Dictionary<int, ExportedSymbol> GetExports(string DLLPath) {
            using var dllStream = new FileStream(DLLPath, FileMode.Open, FileAccess.Read);
            using var dllImage = new PEReader(dllStream);
            var dir = dllImage.PEHeaders.PEHeader.ExportTableDirectory;
            var offset = PEHelper.RvaToOffset(Convert.ToInt32(dir.RelativeVirtualAddress), dllImage.PEHeaders.SectionHeaders);
            using var mmf = MemoryMappedFile.CreateFromFile(dllStream, null, 0, MemoryMappedFileAccess.Read, null, HandleInheritability.None, false);
            using var mmfAccessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            mmfAccessor.Read(offset, out ImageExportDirectory exportDirectory);
            var count = exportDirectory.NumberOfFunctions;
            // this is the placeholder for the final mapping of ordinal # to address map
            Dictionary<int, ExportedSymbol> exports = new(count);
            var functionsOffset = PEHelper.RvaToOffset(exportDirectory.AddressOfFunctions, dllImage.PEHeaders.SectionHeaders);
            var ordinalBase = exportDirectory.Base;
            for (uint funcOrdinal = 0; funcOrdinal < count; funcOrdinal++) {
                // read function address
                var address = mmfAccessor.ReadUInt32(functionsOffset + funcOrdinal * 4);

                if (0 != address) {
                    exports.Add((int)(ordinalBase + funcOrdinal), new ExportedSymbol { OrdinalName = string.Format(CultureInfo.CurrentCulture, "Ordinal{0}", ordinalBase + funcOrdinal), Address = address });
                }
            }

            return exports;
        }
    }
}
