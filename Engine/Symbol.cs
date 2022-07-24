// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    [DataContract] public class Symbol {
        [DataMember(Order = 0)] public string PDBName;
        [IgnoreDataMember] public string InternalPDBName;
        [IgnoreDataMember] public string PDBGuid;
        [IgnoreDataMember] public int PDBAge;
        [IgnoreDataMember] public ulong CalculatedModuleBaseAddress;
        [DataMember(Order = 1)] public string DownloadURL;
        [DataMember(Order = 2)] public bool DownloadVerified;
        [DataMember(Order = 3)] public string FileVersion;

        public static async Task<bool> IsURLValid(Uri url) {
            try {
                using var client = new HttpClient();
                using var req = new HttpRequestMessage(HttpMethod.Head, url);
                var res = await client.SendAsync(req);
                if (null != res.EnsureSuccessStatusCode()) return true;
            } catch (HttpRequestException) { /* this will fall through to the return false so it is okay to leave blank */ } catch (ArgumentException) { /* this will fall through to the return false so it is okay to leave blank */ }
            return false;
        }
    }
}
