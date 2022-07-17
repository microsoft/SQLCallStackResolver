// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    [SupportedOSPlatform("windows")]
    public partial class SQLBuildsForm : Form {
        public string pathToPDBs = string.Empty;
        public string lastDownloadedSymFolder = string.Empty;
        private Task _dnldTask = null;
        private CancellationTokenSource _cts;

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
                _cts.Cancel();
                return;
            }

            if (treeviewSyms.SelectedNode.Tag is SQLBuildInfo bld && bld.SymbolDetails.Count > 0) {
                var statusMsg = new StringBuilder();
                dnldButton.Text = "Cancel download";
                lastDownloadedSymFolder = $@"{pathToPDBs}\{bld.BuildNumber}.{bld.MachineType}";
                Directory.CreateDirectory(lastDownloadedSymFolder);
                var urls = bld.SymbolDetails.Select(s => s.DownloadURL);
                using (_cts = new CancellationTokenSource()) {
                    foreach (var (url, filename) in from url in urls where !string.IsNullOrEmpty(url) let uri = new Uri(url) let filename = Path.GetFileName(uri.LocalPath) select (url, filename)) {
                        if (_cts.IsCancellationRequested) break;
                        if (File.Exists($@"{lastDownloadedSymFolder}\{filename}")) continue;
                        downloadStatus.Text = filename;
                        var prog = new DownloadProgress();
                        _dnldTask = Task.Run(async () => {
                            var res = await Utils.DownloadFromUrl(url, $@"{lastDownloadedSymFolder}\{filename}", prog, _cts);
                            if (!res) statusMsg.AppendLine($"Failed to download {url}");
                        });

                        while (!_dnldTask.Wait(StackResolver.OperationWaitIntervalMilliseconds)) {
                            downloadProgress.ProgressBar.Value = prog.Percent;
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

                    if (!_cts.IsCancellationRequested) {
                        this.Close();
                    }
                }
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
