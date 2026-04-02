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

            var statusMsg = new StringBuilder();
            dnldButton.IsEnabled = false;
            processingOverlay.Visibility = Visibility.Visible;
            LastDownloadedSymFolder = $@"{PathToPDBs}\{bld.BuildNumber}.{bld.MachineType}";
            Directory.CreateDirectory(LastDownloadedSymFolder);
            var urls = bld.SymbolDetails.Select(s => s.DownloadURL);

            using (_cts = new CancellationTokenSource()) {
                foreach (var url in urls.Where(u => !string.IsNullOrEmpty(u))) {
                    if (_cts.IsCancellationRequested) break;
                    var uri = new Uri(url);
                    var filename = Path.GetFileName(uri.LocalPath);
                    if (File.Exists($@"{LastDownloadedSymFolder}\{filename}")) continue;

                    overlayStatus.Text = $"Downloading {filename}...";
                    var prog = new DownloadProgress();
                    _dnldTask = Task.Run(async () => {
                        var res = await Utils.DownloadFromUrl(url, $@"{LastDownloadedSymFolder}\{filename}", prog, _cts);
                        if (!res) statusMsg.AppendLine($"Failed to download {url}");
                    });

                    while (!_dnldTask.IsCompleted) {
                        await Task.Delay(StackResolver.OperationWaitIntervalMilliseconds);
                    }
                    _dnldTask = null;
                }

                dnldButton.IsEnabled = true;
                processingOverlay.Visibility = Visibility.Collapsed;
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
                downloadStatus.Text = $"Checking {url}...";
                if (!(await Symbol.IsURLValid(new Uri(url)))) failedUrls.Add(url);
            }
            downloadStatus.Text = failedUrls.Count > 0 ? string.Join(",", failedUrls) : "All PDBs for this build are available!";
        }

        private void CancelDownload_Click(object sender, RoutedEventArgs e) {
            _cts?.Cancel();
        }

        private void FindNext_Click(object sender, RoutedEventArgs e) => FindInTree(forward: true);
        private void FindPrev_Click(object sender, RoutedEventArgs e) => FindInTree(forward: false);

        private void Find_Executed(object sender, ExecutedRoutedEventArgs e) {
            findBarBorder.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() => {
                searchText.Focus();
                Keyboard.Focus(searchText);
                searchText.SelectAll();
            }));
        }

        private void FindClose_Click(object sender, RoutedEventArgs e) {
            findBarBorder.Visibility = Visibility.Collapsed;
            matchInfo.Text = "";
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter || e.Key == Key.F3) {
                FindInTree(forward: Keyboard.Modifiers != ModifierKeys.Shift);
                e.Handled = true;
            } else if (e.Key == Key.Escape) {
                FindClose_Click(sender, e);
                e.Handled = true;
            }
        }

        private List<TreeViewItem> _flatItems;
        private int _lastFoundIndex = -1;

        private void FindInTree(bool forward) {
            if (string.IsNullOrWhiteSpace(searchText.Text)) return;
            var term = searchText.Text.Trim().ToLower(CultureInfo.CurrentCulture);

            // Build flat list of all leaf items
            _flatItems = new List<TreeViewItem>();
            CollectLeafItems(treeviewSyms.Items, _flatItems);

            if (_flatItems.Count == 0) { matchInfo.Text = "No items"; return; }

            int start = forward ? _lastFoundIndex + 1 : _lastFoundIndex - 1;
            int count = _flatItems.Count;

            for (int i = 0; i < count; i++) {
                int idx = forward
                    ? (start + i + count) % count
                    : (start - i + count) % count;
                var item = _flatItems[idx];
                if (item.Tag is SQLBuildInfo bld && bld.ToString().ToLower(CultureInfo.CurrentCulture).Contains(term)) {
                    // Expand parent chain
                    var parent = item.Parent as TreeViewItem;
                    while (parent != null) { parent.IsExpanded = true; parent = parent.Parent as TreeViewItem; }
                    item.IsSelected = true;
                    item.BringIntoView();
                    _lastFoundIndex = idx;
                    matchInfo.Text = "Found";
                    downloadStatus.Text = "";
                    return;
                }
            }
            matchInfo.Text = "No matches";
        }

        private void CollectLeafItems(ItemCollection items, List<TreeViewItem> result) {
            foreach (TreeViewItem item in items) {
                if (item.Items.Count > 0)
                    CollectLeafItems(item.Items, result);
                else
                    result.Add(item);
            }
        }
    }
}
