// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using System;
    using System.Net;
    using System.Runtime.Serialization;

    [DataContract] public class Symbol {
        [DataMember(Order = 0)] public string PDBName;
        [IgnoreDataMember] public string InternalPDBName;
        [IgnoreDataMember] public string PDBGuid;
        [IgnoreDataMember] public int PDBAge;
        [IgnoreDataMember] public ulong CalculatedModuleBaseAddress;
        [DataMember(Order = 1)] public string DownloadURL;
        [DataMember(Order = 2)] public bool DownloadVerified;
        [DataMember(Order = 3)] public string FileVersion;

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
