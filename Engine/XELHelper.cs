// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using Microsoft.SqlServer.XEvent.XELite;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal class XELHelper {
        /// Read a XEL file, consume all callstacks, optionally bucketize them, and in all cases, return the information as equivalent XML
        internal static Tuple<int, string> ExtractFromXEL(StackResolver parent, string[] xelFiles, bool bucketize) {
            Contract.Requires(xelFiles != null);
            parent.cancelRequested = false;
            var callstackSlots = new Dictionary<string, long>();
            var callstackRaw = new Dictionary<string, string>();
            var xmlEquivalent = new StringBuilder();

            // the below feels quite hacky. Unfortunately till such time that we have strong typing in XELite I believe this is unavoidable
            var relevantKeyNames = new string[] { "callstack", "call_stack", "stack_frames" };
            foreach (var xelFileName in xelFiles) {
                if (File.Exists(xelFileName)) {
                    parent.StatusMessage = $@"Reading {xelFileName}...";
                    var xeStream = new XEFileEventStreamer(xelFileName);
                    xeStream.ReadEventStream(
                        () => {
                            return Task.CompletedTask;
                        },
                        evt => {
                            var allStacks = (from actTmp in evt.Actions
                                             from keyName in relevantKeyNames
                                             where actTmp.Key.ToLower(CultureInfo.CurrentCulture).StartsWith(keyName)
                                             select actTmp.Value as string)
                                                .Union(
                                                from fldTmp in evt.Fields
                                                from keyName in relevantKeyNames
                                                where fldTmp.Key.ToLower(CultureInfo.CurrentCulture).StartsWith(keyName)
                                                select fldTmp.Value as string);

                            foreach (var callStackString in allStacks) {
                                if (string.IsNullOrEmpty(callStackString)) {
                                    continue;
                                }

                                if (bucketize) {
                                    lock (callstackSlots) {
                                        if (!callstackSlots.ContainsKey(callStackString)) {
                                            callstackSlots.Add(callStackString, 1);
                                        }
                                        else {
                                            callstackSlots[callStackString]++;
                                        }
                                    }
                                }
                                else {
                                    var evtId = string.Format(CultureInfo.CurrentCulture, "File: {0}, Timestamp: {1}, UUID: {2}:", xelFileName, evt.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.CurrentCulture), evt.UUID);
                                    lock (callstackRaw) {
                                        if (!callstackRaw.ContainsKey(evtId)) {
                                            callstackRaw.Add(evtId, callStackString);
                                        }
                                        else {
                                            callstackRaw[evtId] += $"{Environment.NewLine}{callStackString}";
                                        }
                                    }
                                }
                            }
                            return Task.CompletedTask;
                        },
                        CancellationToken.None).Wait();
                }
            }

            parent.StatusMessage = "Finished reading file(s), finalizing output...";
            int finalEventCount;
            if (bucketize) {
                xmlEquivalent.AppendLine("<HistogramTarget>");
                parent.globalCounter = 0;
                foreach (var item in callstackSlots.OrderByDescending(key => key.Value)) {
                    xmlEquivalent.AppendFormat(CultureInfo.CurrentCulture,
                        "<Slot count=\"{0}\"><value><![CDATA[{1}]]></value></Slot>",
                        item.Value,
                        item.Key);
                    xmlEquivalent.AppendLine();
                    parent.globalCounter++;
                    parent.PercentComplete = (int)((double)parent.globalCounter / callstackSlots.Count * 100.0);
                }

                xmlEquivalent.AppendLine("</HistogramTarget>");
                finalEventCount = callstackSlots.Count;
            }
            else {
                xmlEquivalent.AppendLine("<Events>");
                parent.globalCounter = 0;
                var hasOverflow = false;
                foreach (var item in callstackRaw.OrderBy(key => key.Key)) {
                    if (xmlEquivalent.Length < int.MaxValue * 0.90) {
                        xmlEquivalent.AppendFormat(CultureInfo.CurrentCulture, "<event key=\"{0}\"><action name='callstack'><value><![CDATA[{1}]]></value></action></event>", item.Key, item.Value);
                        xmlEquivalent.AppendLine();
                    }
                    else {
                        hasOverflow = true;
                    }
                    parent.globalCounter++;
                    parent.PercentComplete = (int)((double)parent.globalCounter / callstackRaw.Count * 100.0);
                }

                if (hasOverflow) xmlEquivalent.AppendLine("<!-- WARNING: output was truncated due to size limits -->");
                xmlEquivalent.AppendLine("</Events>");
                finalEventCount = callstackRaw.Count;
            }

            parent.StatusMessage = $@"Finished processing {xelFiles.Length} XEL files";
            return new Tuple<int, string>(finalEventCount, xmlEquivalent.ToString());
        }
    }
}
