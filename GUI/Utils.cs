// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using System;
    using System.IO;
    using System.Net;

    internal class Utils {
        internal static string GetFileContentsFromUrl(string url) {
            if (string.IsNullOrEmpty(url)) return null;
            var httpReq = (HttpWebRequest)WebRequest.Create(new Uri(url));

            try {
                HttpWebResponse httpResp = (HttpWebResponse)httpReq.GetResponse();
                if (httpResp != null) {
                    if (httpResp.StatusCode == HttpStatusCode.OK) {
                        using (var strm = new StreamReader(httpResp.GetResponseStream()))
                            return strm.ReadToEnd().Trim();
                    }
                }
            } catch (WebException) {
            }

            return null;
        }
    }
}
