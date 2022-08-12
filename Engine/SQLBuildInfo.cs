﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    public class SQLBuildInfo {
        [JsonInclude][JsonPropertyOrder(0)] public string ProductMajorVersion = "<<ProductMajorVersion>>";
        [JsonInclude][JsonPropertyOrder(1)] public string ProductLevel = "<<ProductLevel>>";
        [JsonInclude][JsonPropertyOrder(2)] public string Label = "<<BuildName>>";
        [JsonInclude][JsonPropertyOrder(3)] public string BuildNumber = "<<BuildNumber>>";
        [JsonInclude][JsonPropertyOrder(4)] public string KBInfo = "<<KBArticle>>";
        [JsonInclude][JsonPropertyOrder(5)] public List<Symbol> SymbolDetails;
        [JsonInclude][JsonPropertyOrder(6)] public string MachineType = "<<x64|x86>>";

        public override string ToString() {
            return string.Format(CultureInfo.CurrentCulture,
                $"{ProductMajorVersion} {ProductLevel} {Label} - {BuildNumber} - {MachineType} ({KBInfo})");
        }

        public static SortedDictionary<string, SQLBuildInfo> GetSqlBuildInfo(string jsonFile) {
            var allBuilds = new SortedDictionary<string, SQLBuildInfo>();
            using var fs = new FileStream(jsonFile, FileMode.Open, FileAccess.Read, FileShare.None);
            using var sr = new StreamReader(fs);
            var allLines = Regex.Split(sr.ReadToEnd(), @"{\s*""ProductMajorVersion""").Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => @"{""ProductMajorVersion""" + m.Replace('\r', ' ').Replace('\n', ' '));
            foreach (var line in allLines) {
                var currBuildInfo = JsonSerializer.Deserialize<SQLBuildInfo>(line);
                currBuildInfo.BuildNumber = currBuildInfo.BuildNumber.Trim();
                currBuildInfo.KBInfo = currBuildInfo.KBInfo.Trim();
                currBuildInfo.Label = currBuildInfo.Label.Trim();
                currBuildInfo.ProductLevel = currBuildInfo.ProductLevel.Trim();
                currBuildInfo.ProductMajorVersion = currBuildInfo.ProductMajorVersion.Trim();

                if (!allBuilds.ContainsKey(currBuildInfo.ToString())) allBuilds.Add(currBuildInfo.ToString(), currBuildInfo);
                else allBuilds[currBuildInfo.ToString()] = currBuildInfo;
            }
            return new SortedDictionary<string, SQLBuildInfo>(allBuilds);
        }

        public static void SaveSqlBuildInfo(List<SQLBuildInfo> allBuilds, string jsonFile) {
            if (allBuilds is null) return;
            using var fs = new FileStream(jsonFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            fs.SetLength(0); // initially, truncate the file
            using var wrtr = new StreamWriter(fs);
            allBuilds.ForEach(bld => wrtr.WriteLine(JsonSerializer.Serialize(bld)));
            fs.Flush();
        }
    }
}
