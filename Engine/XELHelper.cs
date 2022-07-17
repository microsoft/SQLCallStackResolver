// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    [SupportedOSPlatform("windows")]
    internal class XELHelper {
        /// Read a XEL file, consume all callstacks, optionally bucketize them, and in all cases, return the information as equivalent XML
        internal async static Task<Tuple<int, string>> ExtractFromXELAsync(StackResolver parent, string[] xelFiles, bool groupEvents, List<string> fieldsToGroupOn, CancellationTokenSource cts) {
            return await Task.Run(async () => {
                Contract.Requires(xelFiles != null);
                var callstackSlots = new ConcurrentDictionary<string, long>();
                var callstackRaw = new ConcurrentDictionary<string, string>();
                var xmlEquivalent = new StringBuilder();
                foreach (var xelFileName in xelFiles.Where(f => File.Exists(f))) {
                    var numEvents = 0;
                    parent.StatusMessage = $@"Reading {xelFileName}...";
                    var xeStream = new XEFileEventStreamer(xelFileName);
                    try {
                        await xeStream.ReadEventStream(evt => {
                            parent.PercentComplete = (int)((Interlocked.Increment(ref numEvents) % 1000.0) / 10.0);
                            var eventKey = string.Join(Environment.NewLine, evt.Actions.Union(evt.Fields).Join(fieldsToGroupOn, l => l.Key, r => r, (l, r) => new { val = l.Value.ToString() }).Select(v => v.val)).Trim();
                            if (!string.IsNullOrWhiteSpace(eventKey)) {
                                if (groupEvents) callstackSlots.AddOrUpdate(eventKey, 1, (k, v) => v + 1);
                                else callstackRaw.AddOrUpdate(string.Format(CultureInfo.CurrentCulture, "File: {0}, UTC: {1}, UUID: {2}", xelFileName, evt.Timestamp.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.CurrentCulture), evt.UUID), eventKey, (k, v) => v + $"{Environment.NewLine}{eventKey}");
                            }
                            return Task.CompletedTask;
                        },
                        cts.Token);
                    } catch (AggregateException e) {
                        if (e.InnerException is OperationCanceledException) {
                            parent.StatusMessage = StackResolver.OperationCanceled;
                            parent.PercentComplete = 0;
                            return new Tuple<int, string>(0, StackResolver.OperationCanceled);
                        } else throw;
                    } catch (OperationCanceledException) {
                        parent.StatusMessage = StackResolver.OperationCanceled;
                        parent.PercentComplete = 0;
                        return new Tuple<int, string>(0, StackResolver.OperationCanceled);
                    }
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
                } else {
                    xmlEquivalent.AppendLine("<Events>");
                    parent.globalCounter = 0;
                    var hasOverflow = false;
                    foreach (var item in callstackRaw.OrderBy(key => key.Key)) {
                        if (xmlEquivalent.Length < int.MaxValue * 0.90) {
                            xmlEquivalent.AppendFormat(CultureInfo.CurrentCulture, "<event key=\"{0}\"><action name='callstack'><value><![CDATA[{1}]]></value></action></event>", item.Key, item.Value);
                            xmlEquivalent.AppendLine();
                        } else hasOverflow = true;
                        parent.globalCounter++;
                        parent.PercentComplete = (int)((double)parent.globalCounter / callstackRaw.Count * 100.0);
                    }

                    if (hasOverflow) xmlEquivalent.AppendLine("<!-- WARNING: output was truncated due to size limits -->");
                    xmlEquivalent.AppendLine("</Events>");
                    finalEventCount = callstackRaw.Count;
                }

                parent.StatusMessage = $@"Finished processing {xelFiles.Length} XEL files";
                parent.PercentComplete = StackResolver.Operation100Percent;
                return new Tuple<int, string>(finalEventCount, xmlEquivalent.ToString());
            });
        }

        internal async static Task<Tuple<List<string>, List<string>>> GetDistinctXELActionsFieldsAsync(string[] xelFiles, int eventsToSampleFromEachFile, CancellationTokenSource cts) {
            return await Task.Run(async () => {
                Contract.Requires(xelFiles != null && eventsToSampleFromEachFile > 0);
                var allActions = new HashSet<string>();
                var allFields = new HashSet<string>();
                bool internalCancel = false;
                foreach (var file in xelFiles) {
                    var numEvents = 0;
                    var xeStream = new XEFileEventStreamer(file);
                    try {
                        await xeStream.ReadEventStream(evt => {
                                if (Interlocked.Increment(ref numEvents) > eventsToSampleFromEachFile) {
                                    internalCancel = true;
                                    cts.Cancel();
                                }
                                lock (allActions) evt.Actions.Select(action => allActions.Add(action.Key)).Count();
                                lock (allFields) evt.Fields.Select(field => allFields.Add(field.Key)).Count();
                                return Task.CompletedTask;
                            }, cts.Token);
                    } catch (AggregateException e) {
                        if (e.InnerException is OperationCanceledException) if (!internalCancel) return new Tuple<List<string>, List<string>>(new List<string>(), new List<string>());
                            else throw;
                    } catch (OperationCanceledException) { if (!internalCancel) return new Tuple<List<string>, List<string>>(new List<string>(), new List<string>()); }
                }

                return new Tuple<List<string>, List<string>>(allActions.OrderBy(k => k).ToList(), allFields.OrderBy(k => k).ToList());
            });
        }
    }
}
