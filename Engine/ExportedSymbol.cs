// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using Microsoft.Diagnostics.Runtime.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.MemoryMappedFiles;

    /// Helper class which stores DLL export name and address (offset)
    public class ExportedSymbol {
        public string Name { get; set; }
        public uint Address { get; set; }

        /// Helper function to load a DLL and then lookup exported functions. For this we use CLRMD and specifically the PEHeader class
        public static Dictionary<int, ExportedSymbol> GetExports(string DLLPath) {
            // this is the placeholder for the final mapping of ordinal # to address map
            Dictionary<int, ExportedSymbol> exports = null;
            using (var dllStream = new FileStream(DLLPath, FileMode.Open, FileAccess.Read)) {
                using (var dllImage = new PEImage(dllStream)) {
                    var dir = dllImage.PEHeader.ExportTableDirectory;
                    var offset = dllImage.RvaToOffset(Convert.ToInt32(dir.RelativeVirtualAddress));
                    using (var mmf = MemoryMappedFile.CreateFromFile(dllStream, null, 0, MemoryMappedFileAccess.Read, null, HandleInheritability.None, false)) {
                        using (var mmfAccessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read)) {
                            mmfAccessor.Read(offset, out ImageExportDirectory exportDirectory);
                            var count = exportDirectory.NumberOfFunctions;
                            exports = new Dictionary<int, ExportedSymbol>(count);
                            var namesOffset = exportDirectory.AddressOfNames != 0 ? dllImage.RvaToOffset(exportDirectory.AddressOfNames) : 0;
                            var ordinalOffset = exportDirectory.AddressOfOrdinals != 0 ? dllImage.RvaToOffset(exportDirectory.AddressOfOrdinals) : 0;
                            var functionsOffset = dllImage.RvaToOffset(exportDirectory.AddressOfFunctions);
                            var ordinalBase = (int)exportDirectory.Base;
                            for (uint funcOrdinal = 0; funcOrdinal < count; funcOrdinal++) {
                                // read function address
                                var address = mmfAccessor.ReadUInt32(functionsOffset + funcOrdinal * 4);

                                if (0 != address) {
                                    exports.Add((int)(ordinalBase + funcOrdinal), new ExportedSymbol { Name = string.Format(CultureInfo.CurrentCulture, "Ordinal{0}", ordinalBase + funcOrdinal), Address = address});
                                }
                            }
                        }
                    }
                }

                return exports;
            }
        }
    }
}