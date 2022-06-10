// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text;

    [DataContract]
    public class SQLBuildInfo {
        [DataMember(Order=0)]
        public string ProductMajorVersion = "<<ProductMajorVersion>>";
        [DataMember(Order = 1)]
        public string ProductLevel = "<<ProductLevel>>";
        [DataMember(Order = 2)]
        public string Label = "<<BuildName>>";
        [DataMember(Order = 3)]
        public string BuildNumber = "<<BuildNumber>>";
        [DataMember(Order = 4)]
        public string KBInfo = "<<KBArticle>>";
        [DataMember(Order = 5)]
        public List<Symbol> SymbolDetails;
        [DataMember(Order = 6)]
        public string MachineType = "<<x64|x86>>";

        public override string ToString() {
            return string.Format(CultureInfo.CurrentCulture,
                $"{ProductMajorVersion} {ProductLevel} {Label} - {BuildNumber} - {MachineType} ({KBInfo})");
        }

        public static SortedDictionary<string, SQLBuildInfo> GetSqlBuildInfo(string jsonFile) {
            var allBuilds = new SortedDictionary<string, SQLBuildInfo>();

            using (var fs = new FileStream(jsonFile, FileMode.Open, FileAccess.Read, FileShare.None)) {
                using (var sr = new StreamReader(fs)) {
                    var allLines = sr.ReadToEnd().Split('\n');
                    var jsonSerializer = new DataContractJsonSerializer(typeof(SQLBuildInfo));
                    foreach (var line in allLines.Where(l => !string.IsNullOrEmpty(l))) {
                        using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(line))) {
                            var currBuildInfo = jsonSerializer.ReadObject(memStream) as SQLBuildInfo;
                            if (currBuildInfo != null) {
                                currBuildInfo.BuildNumber = currBuildInfo.BuildNumber.Trim();
                                currBuildInfo.KBInfo = currBuildInfo.KBInfo.Trim();
                                currBuildInfo.Label = currBuildInfo.Label.Trim();
                                currBuildInfo.ProductLevel = currBuildInfo.ProductLevel.Trim();
                                currBuildInfo.ProductMajorVersion = currBuildInfo.ProductMajorVersion.Trim();

                                if (!allBuilds.ContainsKey(currBuildInfo.ToString())) {
                                    allBuilds.Add(currBuildInfo.ToString(), currBuildInfo);
                                } else {
                                    allBuilds[currBuildInfo.ToString()] = currBuildInfo;
                                }
                            }
                        }
                    }
                }
            }
            return new SortedDictionary<string, SQLBuildInfo>(allBuilds);
        }

        public static void SaveSqlBuildInfo(List<SQLBuildInfo> allBuilds, string jsonFile) {
            if (allBuilds != null) {
                using (var fs = new FileStream(jsonFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) {
                    // initially, truncate the file
                    fs.SetLength(0);
                    var jsonSerializer = new DataContractJsonSerializer(typeof(SQLBuildInfo));
                    foreach (var bld in allBuilds) {
                        jsonSerializer.WriteObject(fs, bld);
                        fs.Write(new byte[] { 13, 10 }, 0, 2);   // \r\n after each JSON "record"
                    }
                    fs.Flush();
                    fs.Close();
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
