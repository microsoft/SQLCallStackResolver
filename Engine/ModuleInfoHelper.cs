// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    public static class ModuleInfoHelper {
        private static Regex rgxPDBName = new Regex(@"^(?<pdb>.+)(\.pdb)$", RegexOptions.IgnoreCase);
        private static Regex rgxFileName = new Regex(@"^(?<module>.+)\.(dll|exe)$", RegexOptions.IgnoreCase);

        /// Given a set of rows each containing several comma-separated fields, return a set of resolved Symbol
        /// objects each of which have PDB GUID and age details.
        public static Dictionary<string, Symbol> ParseModuleInfo(string input) {
            var retval = new Dictionary<string, Symbol>();
            Contract.Requires(!string.IsNullOrEmpty(input));
            // split into multiple lines
            var lines = input.Split('\n');

            foreach (var line in lines) {
                Guid pdbGuid = Guid.Empty;
                string moduleName = null;
                string pdbName = null;

                // foreach line, split into comma-delimited fields
                var fields = line.Split(',');
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

                int pdbAge = int.MinValue;
                // assumption is that last field is pdbAge - TODO parameterize
                _ = int.TryParse(fields[fields.Length - 1], out pdbAge);

                if (string.IsNullOrEmpty(pdbName)) {
                    // fall back to module name as PDB name
                    pdbName = moduleName;
                }

                // check if we have all 3 details
                if (!string.IsNullOrEmpty(pdbName)
                    && pdbAge != int.MinValue
                    && pdbGuid != Guid.Empty) {
                    retval.Add(moduleName, new Symbol() { PDBName = pdbName + ".pdb", PDBAge = pdbAge, PDBGuid = pdbGuid.ToString("N") });
                }
            }

            return retval;
        }

        private static string matchEval(Match m) {
            return $"{m.Groups["prefix"]}{m.Groups["suffix"]}";
        }

        internal static (Dictionary<string, Symbol>, string) ParseModuleInfoXML(string input) {
            Contract.Requires(!string.IsNullOrEmpty(input));
            var outCallstack = new StringBuilder();
            // use a multi-line regex replace to reassemble XML fragments which were split across lines
            input = Regex.Replace(input, @"(?<prefix>\<frame[^\/\>]*?)(?<newline>(\r\n|\n))(?<suffix>.*?\/\>\s*?$)", new MatchEvaluator(matchEval), RegexOptions.Multiline);
            // next, replace any pre-post stuff from the XML frame lines
            input = Regex.Replace(input, @"(?<prefix>.*?)(?<retain>\<frame.+\/\>)(?<suffix>.*?)", "${retain}");

            // split into multiple lines
            var syms = new Dictionary<string, Symbol>();
            var lines = input.Split('\n');
            foreach (var line in lines) {
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                // foreach line, check if XML
                try {
                    using (var sreader = new StringReader(line)) {
                        using (var reader = XmlReader.Create(sreader, new XmlReaderSettings() { XmlResolver = null })) {
                            if (reader.Read()) {
                                // seems to be XML; process attributes only if all 3 are there
                                var pdbGuid = Guid.Parse(reader.GetAttribute("guid"));
                                var moduleName = reader.GetAttribute("module").ToLower(); // TODO check case-sensitivity
                                var pdbName = reader.GetAttribute("pdb").ToLower();
                                var pdbAge = int.Parse(reader.GetAttribute("age"));
                                if (!syms.ContainsKey(moduleName)) {
                                    syms.Add(moduleName, new Symbol() { PDBName = pdbName, PDBAge = pdbAge, PDBGuid = pdbGuid.ToString("N") });
                                }

                                // transform the XML into a simple module+offset notation
                                outCallstack.AppendFormat($"{moduleName}+{reader.GetAttribute("rva")}{Environment.NewLine}");
                            }
                        }
                    }
                } catch (Exception ex) {
                    if (ex is ArgumentNullException || ex is XmlException) {
                        // pass-through this line as it is not XML
                        outCallstack.AppendLine(line);
                    } else { throw; }
                }
            }

            return (syms, outCallstack.ToString());
        }
    }
}
