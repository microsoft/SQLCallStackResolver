// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Xml;

    public static class ModuleInfoHelper {
        private static Regex rgxPDBName = new Regex(@"^(?<pdb>.+)(\.pdb)$", RegexOptions.IgnoreCase);
        private static Regex rgxFileName = new Regex(@"^(?<module>.+)\.(dll|exe)$", RegexOptions.IgnoreCase);

        /// Given a set of rows each containing several comma-separated fields, return a set of resolved Symbol
        /// objects each of which have PDB GUID and age details.
        public static Dictionary<string, Symbol> ParseModuleInfo(List<StackWithCount> listOfCallStacks) {
            var retval = new Dictionary<string, Symbol>();
            Parallel.ForEach(listOfCallStacks.Select(c => c.Callstack), input => {
                Contract.Requires(!string.IsNullOrEmpty(input));
                // split into multiple lines
                var lines = input.Split('\n');

                foreach (var line in lines) {
                    Guid pdbGuid = Guid.Empty;
                    string moduleName = null;
                    string pdbName = null;

                    // foreach line, split into comma-delimited fields
                    var fields = line.Split(',');
                    // only attempt to process further if this line does look like it has delimited fields
                    if (fields.Length >= 3) {
                        foreach (var rawfield in fields) {
                            var field = rawfield.Trim().TrimEnd('"').TrimStart('"');
                            Guid tmpGuid = Guid.Empty;
                            // for each field, attempt using regexes to detect file name and GUIDs
                            if (Guid.TryParse(field, out tmpGuid)) {
                                pdbGuid = tmpGuid;
                            }

                            if (string.IsNullOrEmpty(moduleName)) {
                                var matchFilename = rgxFileName.Match(field);
                                if (matchFilename.Success) {
                                    moduleName = matchFilename.Groups["module"].Value;
                                }
                            }

                            if (string.IsNullOrEmpty(pdbName)) {
                                var matchPDBName = rgxPDBName.Match(field);
                                if (matchPDBName.Success) {
                                    pdbName = matchPDBName.Groups["pdb"].Value;
                                }
                            }
                        }

                        // assumption is that last field is pdbAge - TODO parameterize
                        _ = int.TryParse(fields[fields.Length - 1], out int pdbAge);

                        if (string.IsNullOrEmpty(pdbName)) {
                            // fall back to module name as PDB name
                            pdbName = moduleName;
                        }

                        // check if we have all 3 details
                        if (!string.IsNullOrEmpty(pdbName)
                            && pdbAge != int.MinValue
                            && pdbGuid != Guid.Empty) {
                            lock (retval) {
                                if (!retval.ContainsKey(moduleName)) {
                                    retval.Add(moduleName, new Symbol() { PDBName = pdbName + ".pdb", PDBAge = pdbAge, PDBGuid = pdbGuid.ToString("N") });
                                }
                            }
                        }
                    }
                }
            });

            return retval;
        }

        public static (Dictionary<string, Symbol>, List<StackWithCount>) ParseModuleInfoXML(List<StackWithCount> listOfCallStacks) {
            var syms = new Dictionary<string, Symbol>();

            Parallel.ForEach(listOfCallStacks, currItem => {
                var outCallstack = new StringBuilder();
                // sniff test to allow for quick exit if input has no XML at all
                if (currItem.Callstack.Contains("<frame")) {
                    // use a multi-line regex replace to reassemble XML fragments which were split across lines
                    currItem.Callstack = Regex.Replace(currItem.Callstack, @"(?<prefix>\<frame[^\/\>]*?)(?<newline>(\r\n|\n))(?<suffix>.*?\/\>\s*?$)", @"${prefix}${suffix}", RegexOptions.Multiline);
                    // ensure that each <frame> element starts on a newline
                    currItem.Callstack = Regex.Replace(currItem.Callstack, @"/\>\s*\<frame", "/>\r\n<frame");
                    // next, replace any pre-post stuff from the XML frame lines
                    currItem.Callstack = Regex.Replace(currItem.Callstack, @"(?<prefix>.*?)(?<retain>\<frame.+\/\>)(?<suffix>.*?)", "${retain}");

                    // split into multiple lines
                    var lines = currItem.Callstack.Split('\n');
                    bool readStatus = false;
                    foreach (var line in lines) {
                        if (!string.IsNullOrWhiteSpace(line)) {
                            // only attempt further formal XML parsing if a simple text check works
                            if (line.StartsWith("<frame")) {
                                try {
                                    using (var sreader = new StringReader(line)) {
                                        using (var reader = XmlReader.Create(sreader, new XmlReaderSettings() { XmlResolver = null })) {
                                            readStatus = reader.Read();
                                            if (readStatus) {
                                                // seems to be XML; process attributes only if all 3 are there
                                                var moduleNameAttributeVal = reader.GetAttribute("module");
                                                if (string.IsNullOrEmpty(moduleNameAttributeVal)){
                                                    moduleNameAttributeVal = reader.GetAttribute("name");
                                                }
                                                var moduleName = Path.GetFileNameWithoutExtension(moduleNameAttributeVal);
                                                var addressAttributeVal = reader.GetAttribute("address");
                                                ulong addressIfPresent = string.IsNullOrEmpty(addressAttributeVal) ? ulong.MinValue : Convert.ToUInt64(addressAttributeVal, 16);
                                                var rvaAttributeVal = reader.GetAttribute("rva");
                                                ulong rvaIfPresent = string.IsNullOrEmpty(rvaAttributeVal) ? ulong.MinValue : Convert.ToUInt64(rvaAttributeVal, 16);
                                                ulong calcBaseAddress = ulong.MinValue;
                                                if (rvaIfPresent != ulong.MinValue && addressIfPresent != ulong.MinValue) {
                                                    calcBaseAddress = addressIfPresent - rvaIfPresent;
                                                }
                                                lock (syms) {
                                                    if (!syms.ContainsKey(moduleName)) {
                                                        syms.Add(moduleName, new Symbol() { PDBName = reader.GetAttribute("pdb").ToLower(), PDBAge = int.Parse(reader.GetAttribute("age")), PDBGuid = Guid.Parse(reader.GetAttribute("guid")).ToString("N"), CalculatedModuleBaseAddress = calcBaseAddress });
                                                    } else {
                                                        if (ulong.MinValue == syms[moduleName].CalculatedModuleBaseAddress) {
                                                            syms[moduleName].CalculatedModuleBaseAddress = calcBaseAddress;
                                                        }
                                                    }
                                                }
                                                string rvaAsIsOrDerived = null;
                                                if (ulong.MinValue != rvaIfPresent) {
                                                    rvaAsIsOrDerived = rvaAttributeVal;
                                                } else if (ulong.MinValue != addressIfPresent && ulong.MinValue != syms[moduleName].CalculatedModuleBaseAddress) {
                                                    rvaAsIsOrDerived = "0x" + (addressIfPresent - syms[moduleName].CalculatedModuleBaseAddress).ToString("X");
                                                }

                                                if (string.IsNullOrEmpty(rvaAsIsOrDerived)) { throw new NullReferenceException(); }

                                                var frameNumHex = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:x2}", int.Parse(reader.GetAttribute("id")));
                                                // transform the XML into a simple module+offset notation
                                                outCallstack.AppendFormat($"{frameNumHex} {moduleName}+{rvaAsIsOrDerived}{Environment.NewLine}");
                                                continue;
                                            }
                                        }
                                    }
                                } catch (Exception ex) {
                                    if (ex is ArgumentNullException || ex is NullReferenceException || ex is XmlException) {
                                    } else { throw; }
                                }
                            }
                        }

                        // pass-through this line as it is either non-XML, 0-length or whitespace-only
                        outCallstack.AppendLine(line);
                    }
                    currItem.Callstack = outCallstack.ToString();
                }
            });

            return (syms, listOfCallStacks);
        }
    }
}
