// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    public partial class SQLBuildsForm : Form {
        public string pathToPDBs = string.Empty;
        public string lastDownloadedSymFolder = string.Empty;
        private Task _dnldTask = null;
        private bool _cancelRequested = false;

        public SQLBuildsForm() {
            InitializeComponent();
        }

        private void Treeview_Load(object sender, EventArgs e) {
            var allBuilds = SQLBuildInfo.GetSqlBuildInfo(MainForm.SqlBuildInfoFileName);
            var sqlMajorVersions = allBuilds.Values.Select(b => b.ProductMajorVersion).OrderByDescending(b => b).Distinct();
            treeviewSyms.Nodes.AddRange(sqlMajorVersions.Select(b => new TreeNode(b) { Name = b }).ToArray());
            foreach (var ver in sqlMajorVersions) {
                var prodLevels = allBuilds.Values.Where(b => b.ProductMajorVersion == ver).Select(b => b.ProductLevel).OrderByDescending(b => b).Distinct();
                treeviewSyms.Nodes[ver].Nodes.AddRange(prodLevels.Select(pl => new TreeNode(pl) { Name = pl }).ToArray());

                // finally within each product level get the individual builds
                foreach (var pl in prodLevels) {
                    var blds = allBuilds.Values.Where(b => b.ProductMajorVersion == ver && b.ProductLevel == pl && b.SymbolDetails.Count > 0).Distinct().OrderByDescending(b => b.BuildNumber);
                    treeviewSyms.Nodes[ver].Nodes[pl].Nodes.AddRange(blds.Select(bld => new TreeNode(bld.ToString()) { Name = bld.ToString(), Tag = bld }).ToArray());
                }
            }
        }

        private void DownloadPDBs(object sender, EventArgs e) {
            if (treeviewSyms.SelectedNode is null) return;
            if (_dnldTask != null) {
                _cancelRequested = true;
                return;
            }

            _cancelRequested = false;

            if (treeviewSyms.SelectedNode.Tag is SQLBuildInfo bld && bld.SymbolDetails.Count > 0) {
                var statusMsg = new StringBuilder();
                dnldButton.Text = "Cancel download";
                lastDownloadedSymFolder = $@"{pathToPDBs}\{bld.BuildNumber}.{bld.MachineType}";
                Directory.CreateDirectory(lastDownloadedSymFolder);
                var urls = bld.SymbolDetails.Select(s => s.DownloadURL);
                foreach (var (url, filename) in from url in urls where !string.IsNullOrEmpty(url) let uri = new Uri(url) let filename = Path.GetFileName(uri.LocalPath) select (url, filename)) {
                    if (_cancelRequested) break;
                    if (File.Exists($@"{lastDownloadedSymFolder}\{filename}")) continue;
                    int totalBytesRead = 0;
                    double expectedTotalBytes = 0;
                    downloadStatus.Text = filename;
                    _dnldTask = Task.Run(async () => {
                        try {
                            var httpStreamDetails = await Utils.GetStreamFromUrl(url);
                            if (null != httpStreamDetails) {
                                using var httpStream = httpStreamDetails.Item1;
                                expectedTotalBytes = httpStreamDetails.Item2;
                                if (httpStream is not null && expectedTotalBytes > 0) {
                                    using var outFS = new FileStream($@"{lastDownloadedSymFolder}\{filename}", FileMode.OpenOrCreate);
                                    outFS.SetLength(0);
                                    var buffer = new byte[4096];
                                    while (true) {
                                        if (_cancelRequested) break;
                                        var bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length);
                                        if (bytesRead == 0) break;
                                        outFS.Write(buffer, 0, bytesRead);
                                        totalBytesRead += bytesRead;
                                    }
                                    await outFS.FlushAsync();
                                }
                            } else statusMsg.AppendLine($"Failed to download {url}");
                        } catch (IOException) { statusMsg.AppendLine($"Failed to download {url}"); }
                    });

                    while (!_dnldTask.Wait(StackResolver.OperationWaitIntervalMilliseconds)) {
                        downloadProgress.ProgressBar.Value = expectedTotalBytes > 0 ? (int)(totalBytesRead / expectedTotalBytes * 100.0) : 0;
                        Application.DoEvents();
                    }
                    _dnldTask = null;
                }

                dnldButton.Text = "Download PDBs";
                downloadProgress.ProgressBar.Value = 0;
                downloadStatus.Text = String.Empty;

                if (statusMsg.Length > 0 && DialogResult.Yes == MessageBox.Show(this, "One or more files could not be downloaded. Press Yes to go back (and try again), or No to close. Error details:" + Environment.NewLine + Environment.NewLine + statusMsg, "Error(s) downloading PDB symbols", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) {
                    return;
                }

                if (!_cancelRequested) this.Close();
            }
        }

        private async void CheckPDBAvail_Click(object sender, EventArgs e) {
            if (treeviewSyms.SelectedNode is null) return;
            if (treeviewSyms.SelectedNode.Tag is SQLBuildInfo bld && bld.SymbolDetails.Count > 0) {
                List<string> failedUrls = new();
                var urls = bld.SymbolDetails.Select(s => s.DownloadURL);
                foreach (var url in urls) {
                    downloadStatus.Text = url;
                    if (!(await Symbol.IsURLValid(new Uri(url)))) failedUrls.Add(url);
                }

                if (failedUrls.Count > 0) MessageBox.Show(string.Join(",", failedUrls));
                else downloadStatus.Text = "All PDBs for this build are available!";
            }
        }

        private void FindNext_Click(object sender, EventArgs e) {
            var foundMatch = treeviewSyms.Nodes.Cast<TreeNode>().Where(n => CheckIfAnyNodesMatch(n)).Any();
            downloadStatus.Text = foundMatch ? "Found match!" : "No matches found.";
            if (!foundMatch) treeviewSyms.SelectedNode = null;
            treeviewSyms.Refresh();
            Application.DoEvents();
        }

        private bool CheckIfAnyNodesMatch(TreeNode node) {
            if (node.Tag is SQLBuildInfo bld && bld.ToString().ToLower(CultureInfo.CurrentCulture).Contains(searchText.Text.Trim().ToLower(CultureInfo.CurrentCulture))) {
                treeviewSyms.SelectedNode = node;
                treeviewSyms.Select();
                treeviewSyms.Refresh();
                Application.DoEvents();
                return true;
            }
            return node.Nodes.Cast<TreeNode>().Where(child => CheckIfAnyNodesMatch(child)).Any();
        }
    }
}
