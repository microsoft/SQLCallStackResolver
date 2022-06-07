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
                if (WebRequest.Create(url) is HttpWebRequest request) {
                    request.Method = "HEAD";
                    if (request.GetResponse() is HttpWebResponse response) response.Close();
                    return true;
                }
            } catch (WebException) { /* this will fall through to the return false so it is okay to leave blank */ }
            return false;
        }
    }
}
