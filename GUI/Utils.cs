// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
using System.Net.Http;

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    internal class Utils {
        internal static async Task<Tuple<Stream, long>> GetStreamFromUrl(string url) {
            if (string.IsNullOrEmpty(url)) return null;
            try {
                var client = new HttpClient();
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                var res = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                res.EnsureSuccessStatusCode();
                return new Tuple<Stream, long>(await res.Content.ReadAsStreamAsync(), (long)res.Content.Headers.ContentLength);
            } catch (HttpRequestException) { /* this will fall through to the return false so it is okay to leave blank */ } catch (NotSupportedException) { /* this will fall through to the return false so it is okay to leave blank */ }
            return null;
        }
    }
}
