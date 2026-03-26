// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class SymbolConfigPage : UserControl {
        private ResolverViewModel ViewModel => DataContext as ResolverViewModel;

        public SymbolConfigPage() {
            InitializeComponent();
        }

        private void BrowsePdbPath_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog {
                CheckPathExists = false,
                CheckFileExists = false,
                FileName = "select folder only",
                Filter = "All files (*.*)|*.*",
                Title = "Select FOLDER path to your PDBs"
            };
            if (dlg.ShowDialog(Window.GetWindow(this)) == true) {
                ViewModel.AppendPdbPath(Path.GetDirectoryName(dlg.FileName));
            }
        }

        private void BrowseBinaryPath_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog {
                CheckPathExists = false,
                CheckFileExists = false,
                FileName = "select folder only",
                Filter = "All files (*.*)|*.*",
                Title = "Select FOLDER path to the SQL binaries"
            };
            if (dlg.ShowDialog(Window.GetWindow(this)) == true) {
                ViewModel.AppendBinaryPath(Path.GetDirectoryName(dlg.FileName));
            }
        }

        private void SelectSQLPDB_Click(object sender, RoutedEventArgs e) {
            if (!File.Exists(ResolverViewModel.SqlBuildInfoFileName)) {
                MessageBox.Show(Window.GetWindow(this),
                    $"Could not find the SQL build info JSON file: {ResolverViewModel.SqlBuildInfoFileName}. You might need to manually obtain it from one of these locations: {ConfigurationManager.AppSettings["SQLBuildInfoURLs"]}",
                    "SQL build info missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var dialog = new SQLBuildsDialog {
                PathToPDBs = ConfigurationManager.AppSettings["PDBDownloadFolder"],
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true) {
                ViewModel.AppendPdbPath(dialog.LastDownloadedSymFolder);
            }
        }

        private void BrowseOutputPath_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.SaveFileDialog {
                FileName = "resolvedstacks.txt",
                Filter = "Text files (*.txt)|*.txt",
                Title = "Save output as"
            };
            if (dlg.ShowDialog(Window.GetWindow(this)) == true) {
                ViewModel.OutputFilePath = dlg.FileName;
            }
        }
    }
}
