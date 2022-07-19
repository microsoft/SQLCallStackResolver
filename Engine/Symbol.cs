// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    public class Symbol {
        [JsonInclude][JsonPropertyOrder(0)] public string PDBName;
        [JsonIgnore] public string InternalPDBName;
        [JsonIgnore] public string PDBGuid;
        [JsonIgnore] public int PDBAge;
        [JsonIgnore] public ulong CalculatedModuleBaseAddress;
        [JsonInclude][JsonPropertyOrder(1)] public string DownloadURL;
        [JsonInclude][JsonPropertyOrder(2)] public bool DownloadVerified;
        [JsonInclude][JsonPropertyOrder(3)] public string FileVersion;

        public static async Task<bool> IsURLValid(Uri url) {
            try {
                var client = new HttpClient();
                var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                if (null != res.EnsureSuccessStatusCode()) return true;
            } catch (HttpRequestException) { /* this will fall through to the return false so it is okay to leave blank */ } 
            catch (NotSupportedException) { /* this will fall through to the return false so it is okay to leave blank */ }
            return false;
        }
    }
}
