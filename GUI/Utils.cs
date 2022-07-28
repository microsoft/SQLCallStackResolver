// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
using System.Net.Http;

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    internal class Utils {
        internal static async Task<bool> DownloadFromUrl(string url, string outFilename, DownloadProgress progress, CancellationTokenSource cts) {
            if (string.IsNullOrEmpty(url)) return false;
            try {
                using var client = new HttpClient();
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var res = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                res.EnsureSuccessStatusCode();
                using var httpStream = await res.Content.ReadAsStreamAsync();
                double totalBytesRead = 0;
                var expectedTotalBytes = res.Content.Headers.ContentLength;
                if (httpStream is not null && expectedTotalBytes > 0) {
                    using var outFS = new FileStream(outFilename, FileMode.OpenOrCreate);
                    outFS.SetLength(0);
                    var buffer = new byte[4096];
                    while (true) {
                        if (cts.IsCancellationRequested) break;
                        var bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;
                        outFS.Write(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        progress.Percent = expectedTotalBytes > 0 ? (int)(totalBytesRead / expectedTotalBytes * 100.0) : 0;
                    }
                    await outFS.FlushAsync();
                }

                return true;
            } catch (IOException) { /* fall through to the return false */ } catch (HttpRequestException) { /* fall through to the return false */ } catch (NotSupportedException) { /* fall through to the return false */ }
            return false;
        }

        internal static async Task<string> GetTextFromUrl(string url) {
            if (string.IsNullOrEmpty(url)) return null;
            try {
                using var client = new HttpClient();
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var res = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                res.EnsureSuccessStatusCode();
                return await res.Content.ReadAsStringAsync();
            } catch (HttpRequestException) { /* this will fall through to the return false so it is okay to leave blank */ } catch (NotSupportedException) { /* this will fall through to the return false so it is okay to leave blank */ }
            return null;
        }
    }

    internal class DownloadProgress {
        internal int Percent;
    }
}
