// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using Microsoft.SqlServer.XEvent.XELite;
    using System;
    using System.Collections.Concurrent;
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
        internal async static Task<Tuple<int, string>> ExtractFromXELAsync(StackResolver parent, string[] xelFiles, bool groupEvents, List<string> fieldsToGroupOn) {
            Contract.Requires(xelFiles != null);
            parent.cancelRequested = false;
            var callstackSlots = new ConcurrentDictionary<string, long>();
            var callstackRaw = new ConcurrentDictionary<string, string>();
            var xmlEquivalent = new StringBuilder();
            foreach (var xelFileName in xelFiles.Where(f => File.Exists(f))) {
                parent.StatusMessage = $@"Reading {xelFileName}...";
                var xeStream = new XEFileEventStreamer(xelFileName);
                await xeStream.ReadEventStream(
                    () => {
                        return Task.CompletedTask;
                    },
                    evt => {
                        var eventKey = string.Join(Environment.NewLine, evt.Actions.Union(evt.Fields).Join(fieldsToGroupOn, l => l.Key, r => r, (l, r) => new { val = l.Value.ToString() }).Select(v => v.val)).Trim();
                        if (!string.IsNullOrWhiteSpace(eventKey)) {
                            if (groupEvents) callstackSlots.AddOrUpdate(eventKey, 1, (k, v) => v + 1);
                            else callstackRaw.AddOrUpdate(string.Format(CultureInfo.CurrentCulture, "File: {0}, Timestamp: {1}, UUID: {2}:", xelFileName, evt.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.CurrentCulture), evt.UUID), eventKey, (k, v) => v + $"{Environment.NewLine}{eventKey}");
                        }
                        return Task.CompletedTask;
                    },
                    CancellationToken.None);
            }

            parent.StatusMessage = "Finished reading file(s), finalizing output...";
            int finalEventCount;
            if (groupEvents) {
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

        internal async static Task<Tuple<List<string>, List<string>>> GetDistinctXELActionsFieldsAsync(string[] xelFiles, int eventsToSampleFromEachFile) {
            Contract.Requires(xelFiles != null && eventsToSampleFromEachFile > 0);
            var allActions = new HashSet<string>();
            var allFields = new HashSet<string>();
            foreach (var file in xelFiles) {
                var numEvents = 0;
                using (var cts = new CancellationTokenSource()) {
                    var xeStream = new XEFileEventStreamer(file);
                    try {
                        await xeStream.ReadEventStream(
                            evt => {
                                if (Interlocked.Increment(ref numEvents) > eventsToSampleFromEachFile) cts.Cancel();
                                lock (allActions) evt.Actions.Select(action => allActions.Add(action.Key)).Count();
                                lock (allFields) evt.Fields.Select(field => allFields.Add(field.Key)).Count();
                                return Task.CompletedTask;
                            }, cts.Token);
                    } catch (OperationCanceledException) { /* there's really nothing to do here so it is empty */ }
                }
            }

            return new Tuple<List<string>, List<string>>(allActions.OrderBy(k => k).ToList(), allFields.OrderBy(k => k).ToList());
        }
    }
}
