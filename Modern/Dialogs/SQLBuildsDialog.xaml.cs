// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class SQLBuildsDialog : Window {
        public string PathToPDBs { get; set; } = string.Empty;
        public string LastDownloadedSymFolder { get; set; } = string.Empty;
        private Task _dnldTask = null;
        private CancellationTokenSource _cts;

        public SQLBuildsDialog() {
            InitializeComponent();
            Loaded += Treeview_Load;
        }

        private void Treeview_Load(object sender, RoutedEventArgs e) {
            try {
                var allBuilds = SQLBuildInfo.GetSqlBuildInfo(ResolverViewModel.SqlBuildInfoFileName);
                var sqlMajorVersions = allBuilds.Values.Select(b => b.ProductMajorVersion).OrderByDescending(b => b).Distinct();

                foreach (var ver in sqlMajorVersions) {
                    var verItem = new TreeViewItem { Header = ver, Tag = ver };
                    var prodLevels = allBuilds.Values.Where(b => b.ProductMajorVersion == ver).Select(b => b.ProductLevel).OrderByDescending(b => b).Distinct();

                    foreach (var pl in prodLevels) {
                        var plItem = new TreeViewItem { Header = pl, Tag = pl };
                        var blds = allBuilds.Values.Where(b => b.ProductMajorVersion == ver && b.ProductLevel == pl && b.SymbolDetails.Count > 0).Distinct().OrderByDescending(b => b.BuildNumber);
                        foreach (var bld in blds) {
                            plItem.Items.Add(new TreeViewItem { Header = bld.ToString(), Tag = bld });
                        }
                        verItem.Items.Add(plItem);
                    }
                    treeviewSyms.Items.Add(verItem);
                }
            } catch (Exception ex) {
                MessageBox.Show(this, $"Error loading SQL build info: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DownloadPDBs_Click(object sender, RoutedEventArgs e) {
            var selectedItem = treeviewSyms.SelectedItem as TreeViewItem;
            if (selectedItem?.Tag is not SQLBuildInfo bld || bld.SymbolDetails.Count <= 0) return;

            if (_dnldTask != null) {
                _cts?.Cancel();
                return;
            }

            var statusMsg = new StringBuilder();
            dnldButton.Content = "Cancel download";
            LastDownloadedSymFolder = $@"{PathToPDBs}\{bld.BuildNumber}.{bld.MachineType}";
            Directory.CreateDirectory(LastDownloadedSymFolder);
            var urls = bld.SymbolDetails.Select(s => s.DownloadURL);

            using (_cts = new CancellationTokenSource()) {
                foreach (var url in urls.Where(u => !string.IsNullOrEmpty(u))) {
                    if (_cts.IsCancellationRequested) break;
                    var uri = new Uri(url);
                    var filename = Path.GetFileName(uri.LocalPath);
                    if (File.Exists($@"{LastDownloadedSymFolder}\{filename}")) continue;

                    downloadStatus.Text = filename;
                    var prog = new DownloadProgress();
                    _dnldTask = Task.Run(async () => {
                        var res = await Utils.DownloadFromUrl(url, $@"{LastDownloadedSymFolder}\{filename}", prog, _cts);
                        if (!res) statusMsg.AppendLine($"Failed to download {url}");
                    });

                    while (!_dnldTask.IsCompleted) {
                        await Task.Delay(StackResolver.OperationWaitIntervalMilliseconds);
                        downloadProgress.Value = prog.Percent;
                    }
                    _dnldTask = null;
                }

                dnldButton.Content = "Download PDBs";
                downloadProgress.Value = 0;
                downloadStatus.Text = string.Empty;

                if (statusMsg.Length > 0) {
                    var result = MessageBox.Show(this,
                        "One or more files could not be downloaded. Press Yes to go back (and try again), or No to close. Error details:\n\n" + statusMsg,
                        "Error(s) downloading PDB symbols", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes) return;
                }

                if (!_cts.IsCancellationRequested) {
                    DialogResult = true;
                    Close();
                }
            }
        }

        private async void CheckPDBAvail_Click(object sender, RoutedEventArgs e) {
            var selectedItem = treeviewSyms.SelectedItem as TreeViewItem;
            if (selectedItem?.Tag is not SQLBuildInfo bld || bld.SymbolDetails.Count <= 0) return;

            List<string> failedUrls = new();
            var urls = bld.SymbolDetails.Select(s => s.DownloadURL);
            foreach (var url in urls) {
                downloadStatus.Text = url;
                if (!(await Symbol.IsURLValid(new Uri(url)))) failedUrls.Add(url);
            }
            downloadStatus.Text = failedUrls.Count > 0 ? string.Join(",", failedUrls) : "All PDBs for this build are available!";
        }

        private void FindNext_Click(object sender, RoutedEventArgs e) {
            if (string.IsNullOrWhiteSpace(searchText.Text)) return;
            var found = SearchTree(treeviewSyms.Items, searchText.Text.Trim().ToLower(CultureInfo.CurrentCulture));
            downloadStatus.Text = found ? "Found match!" : "No matches found.";
        }

        private bool SearchTree(ItemCollection items, string searchTerm) {
            foreach (TreeViewItem item in items) {
                if (item.Tag is SQLBuildInfo bld && bld.ToString().ToLower(CultureInfo.CurrentCulture).Contains(searchTerm)) {
                    item.IsSelected = true;
                    item.BringIntoView();
                    // expand parent chain
                    var parent = item.Parent as TreeViewItem;
                    while (parent != null) { parent.IsExpanded = true; parent = parent.Parent as TreeViewItem; }
                    return true;
                }
                if (item.Items.Count > 0 && SearchTree(item.Items, searchTerm)) return true;
            }
            return false;
        }
    }
}
