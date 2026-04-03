// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class ClassicView : UserControl {
        private ResolverViewModel ViewModel => DataContext as ResolverViewModel;
        private string _lastResolveMode = Properties.Settings.Default.LastResolveMode ?? "resolve";

        public ClassicView() {
            InitializeComponent();
            findBar.Attach(outputTextBox);
            // On mouse wheel, focus the output TextBox so the highlight stays with the text
            outputTextBox.PreviewMouseWheel += (s, e) => {
                if (findBar.IsOpen) {
                    outputTextBox.Focus();
                    Keyboard.Focus(outputTextBox);
                }
            };
            // On keyboard scroll keys or Escape, close find bar
            outputTextBox.PreviewKeyDown += (s, e) => {
                if (findBar.IsOpen && (e.Key == Key.Escape || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.PageUp || e.Key == Key.PageDown || e.Key == Key.Home || e.Key == Key.End))
                    findBar.Close();
            };
            // Ctrl+F from the input textbox needs special handling because
            // WPF's TextBox intercepts ApplicationCommands.Find internally
            inputTextBox.PreviewKeyDown += (s, e) => {
                if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control) {
                    findBar.Open();
                    e.Handled = true;
                }
            };
            // Restore the split button face from the persisted setting
            Loaded += (s, e) => {
                if (_lastResolveMode == "paste") {
                    resolveMainIcon.Text = "\uE77F";
                    resolveMainText.Text = "Paste clipboard & resolve";
                }
            };
        }

        private void Find_Executed(object sender, ExecutedRoutedEventArgs e) => findBar.Open();

        private void UseSymbolServer_Click(object sender, RoutedEventArgs e) {
            ViewModel.HasXmlFrameInput = false;
            ViewModel.UpdatePdbPath(@"SRV*c:\temp\symcache*https://msdl.microsoft.com/download/symbols");
            ViewModel.StatusMessage = "Symbol server path added. Ready to resolve!";
            ViewModel.HighlightResolve = true;
        }

        private async void LoadXEL_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog {
                Multiselect = true,
                CheckPathExists = true,
                CheckFileExists = true,
                Filter = "XEL files (*.xel)|*.xel|All files (*.*)|*.*",
                Title = "Select XEL file(s)"
            };
            if (dlg.ShowDialog(Window.GetWindow(this)) == true) {
                var fields = await ViewModel.GetDistinctXELFieldsAsync(dlg.FileNames);
                if (fields.Item1.Count + fields.Item2.Count == 0) { ViewModel.StatusMessage = "No fields found in XEL files."; return; }
                var fieldDialog = new FieldSelectionDialog(fields.Item1, fields.Item2) { Owner = Window.GetWindow(this) };
                if (fieldDialog.ShowDialog() == true) {
                    await ViewModel.LoadXELFilesAsync(dlg.FileNames, fieldDialog.SelectedEventItems);
                }
            }
        }

        private void EnterBaseAddresses_Click(object sender, RoutedEventArgs e) {
            var dialog = new BaseAddressDialog(ViewModel.BaseAddressesString) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true) ViewModel.BaseAddressesString = dialog.BaseAddressesString;
        }

        private void BrowsePdbPath_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog {
                CheckPathExists = false, CheckFileExists = false, FileName = "select folder only",
                Filter = "All files (*.*)|*.*", Title = "Select FOLDER path to your PDBs"
            };
            if (dlg.ShowDialog(Window.GetWindow(this)) == true) ViewModel.UpdatePdbPath(Path.GetDirectoryName(dlg.FileName));
        }

        private void BrowseBinaryPath_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog {
                CheckPathExists = false, CheckFileExists = false, FileName = "select folder only",
                Filter = "All files (*.*)|*.*", Title = "Select FOLDER path to the SQL binaries"
            };
            if (dlg.ShowDialog(Window.GetWindow(this)) == true) ViewModel.AppendBinaryPath(Path.GetDirectoryName(dlg.FileName));
        }

        private void SelectSQLPDB_Click(object sender, RoutedEventArgs e) {
            // Dismiss the banner immediately — user has responded to the hint
            ViewModel.DetectedBuildInfo = null;
            if (!File.Exists(ResolverViewModel.SqlBuildInfoFileName)) {
                MessageBox.Show(Window.GetWindow(this),
                    $"Could not find the SQL build info JSON file: {ResolverViewModel.SqlBuildInfoFileName}. You might need to manually obtain it from: {ConfigurationManager.AppSettings["SQLBuildInfoURLs"]}",
                    "SQL build info missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var dialog = new SQLBuildsDialog { PathToPDBs = ConfigurationManager.AppSettings["PDBDownloadFolder"], Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true) ViewModel.UpdatePdbPath(dialog.LastDownloadedSymFolder);
        }

        private void BrowseOutputPath_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.SaveFileDialog { FileName = "resolvedstacks.txt", Filter = "Text files (*.txt)|*.txt", Title = "Save output as" };
            if (dlg.ShowDialog(Window.GetWindow(this)) == true) ViewModel.OutputFilePath = dlg.FileName;
        }

        private void CallStackInput_DragOver(object sender, DragEventArgs e) {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private async void CallStackInput_Drop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files == null || files.Length == 0) return;
                if (files.All(f => Path.GetExtension(f).Equals(".xel", StringComparison.OrdinalIgnoreCase))) {
                    ViewModel.StatusMessage = "XEL file(s) dragged; extracting events...";
                    var fields = await ViewModel.GetDistinctXELFieldsAsync(files);
                    if (fields.Item1.Count + fields.Item2.Count == 0) { ViewModel.StatusMessage = "No fields found in XEL files."; return; }
                    var fieldDialog = new FieldSelectionDialog(fields.Item1, fields.Item2) { Owner = Window.GetWindow(this) };
                    if (fieldDialog.ShowDialog() == true) await ViewModel.LoadXELFilesAsync(files, fieldDialog.SelectedEventItems);
                } else {
                    var sb = new StringBuilder();
                    foreach (var file in files) sb.AppendLine(File.ReadAllText(file));
                    ViewModel.InputText = sb.ToString();
                }
            }
        }

        private void ExecuteResolveAction(string mode) {
            if (mode == "paste") {
                ViewModel.PasteFromClipboardCommand.Execute(null);
            } else {
                ViewModel.ResolveCommand.Execute(null);
            }
        }

        private void UpdateResolveButton(string mode) {
            _lastResolveMode = mode;
            Properties.Settings.Default.LastResolveMode = mode;
            Properties.Settings.Default.Save();
            if (mode == "paste") {
                resolveMainIcon.Text = "\uE77F";
                resolveMainText.Text = "Paste clipboard & resolve";
            } else {
                resolveMainIcon.Text = "\uE768";
                resolveMainText.Text = "Resolve Callstacks!";
            }
        }

        private void ResolveMain_Click(object sender, RoutedEventArgs e) => ExecuteResolveAction(_lastResolveMode);

        private void ResolveDropdown_Click(object sender, RoutedEventArgs e) {
            if (sender is Button btn) {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void ResolveMenuItem_Click(object sender, RoutedEventArgs e) {
            if (sender is MenuItem item && item.Tag is string mode) {
                UpdateResolveButton(mode);
            }
        }
    }
}
