// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    internal class Utils {
        internal static string GetFileContentsFromUrl(string url) {
            if (string.IsNullOrEmpty(url)) return null;
            try {
                using var client = new HttpClient();
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                var res = client.Send(req); // TODO use Async
                if (res.StatusCode == HttpStatusCode.OK) {
                    using var strm = new StreamReader(res.Content.ReadAsStream());
                    return strm.ReadToEnd().Trim();
                }
            } catch (HttpRequestException) { /* this will fall through to the return false so it is okay to leave blank */ } catch (NotSupportedException) { /* this will fall through to the return false so it is okay to leave blank */ }
            return null;
        }
    }
}
