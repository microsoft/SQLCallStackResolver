// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class SQLBuildInfo {
        public string ProductMajorVersion = "<<ProductMajorVersion>>";
        public string ProductLevel = "<<ProductLevel>>";
        public string Label = "<<BuildName>>";
        public string BuildNumber = "<<BuildNumber>>";
        public string KBInfo = "<<KBArticle>>";
        public List<Symbol> SymbolDetails;
        public string MachineType = "<<x64|x86>>";

        public override string ToString() {
            return string.Format(CultureInfo.CurrentCulture,
                $"{ProductMajorVersion} {ProductLevel} {Label} - {BuildNumber} - {MachineType} ({KBInfo})");
        }

        public static SortedDictionary<string, SQLBuildInfo> GetSqlBuildInfo(string jsonFile) {
            var allBuilds = new SortedDictionary<string, SQLBuildInfo>();

            using (var fs = new FileStream(jsonFile, FileMode.Open, FileAccess.Read, FileShare.None)) {
                using (var rdr = new StreamReader(fs)) {
                    using (var jsonRdr = new JsonTextReader(rdr)) {
                        jsonRdr.SupportMultipleContent = true;
                        var serializer = new JsonSerializer();
                        while (true) {
                            if (!jsonRdr.Read()) {
                                break;
                            }

                            var currBuildInfo = serializer.Deserialize<SQLBuildInfo>(jsonRdr);
                            currBuildInfo.BuildNumber = currBuildInfo.BuildNumber.Trim();
                            currBuildInfo.KBInfo = currBuildInfo.KBInfo.Trim();
                            currBuildInfo.Label = currBuildInfo.Label.Trim();
                            currBuildInfo.ProductLevel = currBuildInfo.ProductLevel.Trim();
                            currBuildInfo.ProductMajorVersion = currBuildInfo.ProductMajorVersion.Trim();

                            if (!allBuilds.ContainsKey(currBuildInfo.ToString())) {
                                allBuilds.Add(currBuildInfo.ToString(), currBuildInfo);
                            }
                            else {
                                allBuilds[currBuildInfo.ToString()] = currBuildInfo;
                            }
                        }

                        jsonRdr.Close();
                    }

                    rdr.Close();
                }
            }

            return new SortedDictionary<string, SQLBuildInfo>(allBuilds);
        }

        public static void SaveSqlBuildInfo(List<SQLBuildInfo> allBuilds, string jsonFile) {
            if (allBuilds != null) {
                using (var fs = new FileStream(jsonFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) {
                    // initially, truncate the file
                    fs.SetLength(0);

                    using (var wrtr = new StreamWriter(fs)) {
                        foreach (var bld in allBuilds) {
                            wrtr.WriteLine(JsonConvert.SerializeObject(bld));
                        }

                        wrtr.Flush();
                        wrtr.Close();
                    }
                }
            }
        }

        public static string GetDownloadScriptPowerShell(SQLBuildInfo bld, bool includeMarkdown) {
            Contract.Requires(bld != null);

            var symcmds = new StringBuilder();

            if (null != bld.SymbolDetails && bld.SymbolDetails.Where(s => s.DownloadVerified).Any()) {
                if (includeMarkdown) {
                    symcmds.AppendLine($"# {bld}");
                    symcmds.AppendLine("``` powershell");
                }
                symcmds.AppendLine($"# {bld}");
                symcmds.AppendLine($"$outputFolder = 'c:\\sqlsyms\\{bld.BuildNumber}\\{bld.MachineType}' # <<change this output folder if needed>>'");
                symcmds.AppendLine($"mkdir -f $outputFolder");
                foreach (var sym in bld.SymbolDetails.Where(s => s.DownloadVerified)) {
                    symcmds.AppendLine($"if (-not (Test-Path \"$outputFolder\\{sym.PDBName}.pdb\")) {{ Invoke-WebRequest -uri '{sym.DownloadURL}' -OutFile \"$outputFolder\\{sym.PDBName}.pdb\" }} # File version {sym.FileVersion}");
                }

                if (includeMarkdown) symcmds.AppendLine("```");
                symcmds.AppendLine();
            }

            return symcmds.ToString();
        }
    }
}
