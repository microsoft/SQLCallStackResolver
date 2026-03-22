// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class InputPage : UserControl {
        private ResolverViewModel ViewModel => DataContext as ResolverViewModel;

        public InputPage() {
            InitializeComponent();
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
                if (fields.Item1.Count + fields.Item2.Count == 0) {
                    ViewModel.StatusMessage = "No fields found in XEL files.";
                    return;
                }
                var fieldDialog = new FieldSelectionDialog(fields.Item1, fields.Item2) { Owner = Window.GetWindow(this) };
                if (fieldDialog.ShowDialog() == true) {
                    await ViewModel.LoadXELFilesAsync(dlg.FileNames, fieldDialog.SelectedEventItems);
                }
            }
        }

        private void EnterBaseAddresses_Click(object sender, RoutedEventArgs e) {
            var dialog = new BaseAddressDialog(ViewModel.BaseAddressesString) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true) {
                ViewModel.BaseAddressesString = dialog.BaseAddressesString;
            }
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
                    if (fields.Item1.Count + fields.Item2.Count == 0) {
                        ViewModel.StatusMessage = "No fields found in XEL files.";
                        return;
                    }
                    var fieldDialog = new FieldSelectionDialog(fields.Item1, fields.Item2) { Owner = Window.GetWindow(this) };
                    if (fieldDialog.ShowDialog() == true) {
                        await ViewModel.LoadXELFilesAsync(files, fieldDialog.SelectedEventItems);
                    }
                } else {
                    var sb = new StringBuilder();
                    foreach (var file in files) sb.AppendLine(File.ReadAllText(file));
                    ViewModel.InputText = sb.ToString();
                }
            }
        }
    }
}
