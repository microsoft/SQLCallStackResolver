// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    public static class ModuleInfoHelper {
        private static readonly Regex rgxPDBName = new(@"^(?<pdb>.+)(\.pdb)$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
        private static readonly Regex rgxFileName = new(@"^(?<module>.+)\.(dll|exe)$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        /// <summary>
        /// Parse the input and return a set of resolved Symbol objects
        /// </summary>
        public async static Task<Dictionary<string, Symbol>> ParseModuleInfoAsync(List<StackDetails> listOfCallStacks, CancellationTokenSource cts) {
            var retval = new Dictionary<string, Symbol>();
            await Task.Run(() => Parallel.ForEach(listOfCallStacks.Where(c => c.Callstack.Contains(",")).Select(c => c.CallstackFrames), lines => {
                if (cts.IsCancellationRequested) return;
                Contract.Requires(lines.Length > 0);
                foreach (var line in lines) {
                    if (cts.IsCancellationRequested) return;
                    string moduleName = null, pdbName = null;

                    // foreach line, split into comma-delimited fields
                    var fields = line.Split(',');
                    // only attempt to process further if this line does look like it has delimited fields
                    if (fields.Length >= 3) {
                        Guid pdbGuid = Guid.Empty;
                        foreach (var field in fields.Select(f => f.Trim().TrimEnd('"').TrimStart('"'))) {
                            // for each field, attempt using regexes to detect file name and GUIDs
                            if (Guid.TryParse(field, out Guid tmpGuid)) pdbGuid = tmpGuid;
                            if (string.IsNullOrEmpty(moduleName)) {
                                var matchFilename = rgxFileName.Match(field);
                                if (matchFilename.Success) moduleName = matchFilename.Groups["module"].Value;
                            }

                            if (string.IsNullOrEmpty(pdbName)) {
                                var matchPDBName = rgxPDBName.Match(field);
                                if (matchPDBName.Success) pdbName = matchPDBName.Groups["pdb"].Value;
                            }
                        }

                        _ = int.TryParse(fields[fields.Length - 1], out int pdbAge);    // assumption is that last field is pdbAge
                        pdbName = string.IsNullOrEmpty(pdbName) ? moduleName : pdbName; // fall back to module name as PDB name

                        // check if we have all 3 details
                        if (!string.IsNullOrEmpty(pdbName) && pdbAge != int.MinValue && pdbGuid != Guid.Empty) {
                            lock (retval) {
                                if (!retval.ContainsKey(moduleName)) retval.Add(moduleName, new Symbol() { PDBName = pdbName + ".pdb", PDBAge = pdbAge, PDBGuid = pdbGuid.ToString("N") });
                            }
                        }
                    }
                }
            }));

            return cts.IsCancellationRequested ? new Dictionary<string, Symbol>() : retval;
        }

        public async static Task<(Dictionary<string, Symbol>, List<StackDetails>)> ParseModuleInfoXMLAsync(List<StackDetails> listOfCallStacks, CancellationTokenSource cts) {
            var syms = new Dictionary<string, Symbol>();
            bool anyTaskFailed = false;
            await Task.Run(() => Parallel.ForEach(listOfCallStacks, currItem => {
                var latestMappedModuleNames = new Dictionary<string, string>();
                if (cts.IsCancellationRequested) return;
                var outCallstack = new StringBuilder();
                // sniff test to allow for quick exit if input has no XML at all
                if (currItem.Callstack.Contains("<frame")) {
                    // use a multi-line regex replace to reassemble XML fragments which were split across lines
                    currItem.Callstack = Regex.Replace(currItem.Callstack, @"(?<prefix>\<frame[^\/\>]*?)(?<newline>(\r\n|\n))(?<suffix>.*?\/\>\s*?$)", @"${prefix}${suffix}", RegexOptions.Multiline);
                    if (cts.IsCancellationRequested) return;
                    // ensure that each <frame> element starts on a newline
                    currItem.Callstack = Regex.Replace(currItem.Callstack, @"/\>\s*\<frame", "/>\r\n<frame");
                    if (cts.IsCancellationRequested) return;
                    // next, replace any pre-post stuff from the XML frame lines
                    currItem.Callstack = Regex.Replace(currItem.Callstack, @"(?<prefix>.*?)(?<retain>\<frame.+\/\>)(?<suffix>.*?)", "${retain}");
                    if (cts.IsCancellationRequested) return;

                    foreach (var line in currItem.Callstack.Split('\n')) {
                        if (cts.IsCancellationRequested) return;
                        if (!string.IsNullOrWhiteSpace(line) && line.StartsWith("<frame")) { // only attempt further formal XML parsing if a simple text check works
                            try {
                                using var sreader = new StringReader(line);
                                using var reader = XmlReader.Create(sreader, new XmlReaderSettings() { XmlResolver = null });
                                if (reader.Read()) {
                                    // seems to be XML; process attributes only if all 3 are there
                                    var moduleNameAttributeVal = reader.GetAttribute("module");
                                    if (string.IsNullOrEmpty(moduleNameAttributeVal)) moduleNameAttributeVal = reader.GetAttribute("name");
                                    var moduleName = Path.GetFileNameWithoutExtension(moduleNameAttributeVal);
                                    var addressAttributeVal = reader.GetAttribute("address");
                                    ulong addressIfPresent = string.IsNullOrEmpty(addressAttributeVal) ? ulong.MinValue : Convert.ToUInt64(addressAttributeVal, 16);
                                    var rvaAttributeVal = reader.GetAttribute("rva");
                                    ulong rvaIfPresent = string.IsNullOrEmpty(rvaAttributeVal) ? ulong.MinValue : Convert.ToUInt64(rvaAttributeVal, 16);
                                    ulong calcBaseAddress = ulong.MinValue;
                                    if (rvaIfPresent != ulong.MinValue && addressIfPresent != ulong.MinValue) calcBaseAddress = addressIfPresent - rvaIfPresent;
                                    var pdbGuid = reader.GetAttribute("guid");
                                    var pdbAge = reader.GetAttribute("age");
                                    string uniqueModuleName;
                                    // TODO handle cases when the above are null
                                    if (pdbGuid != null && pdbAge != null) {
                                        uniqueModuleName = $"{pdbGuid.Replace("-", string.Empty).ToUpper()}{pdbAge}";
                                        if (latestMappedModuleNames.ContainsKey(moduleName)) latestMappedModuleNames[moduleName] = uniqueModuleName;
                                        else latestMappedModuleNames.Add(moduleName, uniqueModuleName);
                                    } else {
                                        if (!latestMappedModuleNames.TryGetValue(moduleName, out uniqueModuleName)) {
                                            anyTaskFailed = true;
                                            return;
                                        }
                                    }
                                    lock (syms) {
                                        if (syms.TryGetValue(uniqueModuleName, out var existingEntry)) {
                                            //if (Guid.Parse(reader.GetAttribute("guid")).ToString("N") != existingEntry.PDBGuid || int.Parse(reader.GetAttribute("age")) != existingEntry.PDBAge) {
                                            //    anyTaskFailed = true;
                                            //    return;
                                            //}
                                            if (ulong.MinValue == existingEntry.CalculatedModuleBaseAddress) existingEntry.CalculatedModuleBaseAddress = calcBaseAddress;
                                        } else syms.Add(uniqueModuleName, new Symbol() { PDBName = reader.GetAttribute("pdb").ToLower(), ModuleName = moduleName, PDBAge = int.Parse(pdbAge), PDBGuid = Guid.Parse(pdbGuid).ToString("N").ToUpper(), CalculatedModuleBaseAddress = calcBaseAddress });
                                    }
                                    string rvaAsIsOrDerived = null;
                                    if (ulong.MinValue != rvaIfPresent) rvaAsIsOrDerived = rvaAttributeVal;
                                    else if (ulong.MinValue != addressIfPresent && ulong.MinValue != syms[uniqueModuleName].CalculatedModuleBaseAddress)
                                        rvaAsIsOrDerived = "0x" + (addressIfPresent - syms[uniqueModuleName].CalculatedModuleBaseAddress).ToString("X");

                                    if (string.IsNullOrEmpty(rvaAsIsOrDerived)) { throw new NullReferenceException(); }

                                    var frameNumHex = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:x2}", int.Parse(reader.GetAttribute("id")));
                                    // transform the XML into a simple module+offset notation
                                    outCallstack.AppendFormat($"{frameNumHex} {uniqueModuleName}+{rvaAsIsOrDerived}{Environment.NewLine}");
                                    continue;
                                }
                            } catch (Exception ex) {
                                if (!(ex is ArgumentNullException || ex is NullReferenceException || ex is XmlException)) { throw; }
                            }
                        }

                        // pass-through this line as it is either non-XML, 0-length or whitespace-only
                        outCallstack.AppendLine(line);
                    }
                    currItem.Callstack = outCallstack.ToString();
                }
            }));

            return cts.IsCancellationRequested ? (new Dictionary<string, Symbol>(), new List<StackDetails>()) : (anyTaskFailed ? null : syms, listOfCallStacks);
        }
    }
}
