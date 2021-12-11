// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using Newtonsoft.Json;
    using System;
    using System.Net;

    public class Symbol {
        public string PDBName;

        [JsonIgnore]
        public string InternalPDBName;

        [JsonIgnore]
        public string PDBGuid;

        [JsonIgnore]
        public int PDBAge;

        [JsonIgnore]
        public ulong CalculatedModuleBaseAddress;

        public string DownloadURL;

        public bool DownloadVerified;

        public string FileVersion;

        public static bool IsURLValid(Uri url) {
            try {
                var request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "HEAD";
                var response = request.GetResponse() as HttpWebResponse;
                response.Close();
            } catch (WebException) {
                return false;
            }

            return true;
        }
    }
}
