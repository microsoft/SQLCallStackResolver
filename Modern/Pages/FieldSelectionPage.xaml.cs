// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class FieldSelectionPage : UserControl {
        private readonly List<FieldItem> _items = new List<FieldItem>();
        public string[] SelectedFileNames { get; private set; }
        private ResolverViewModel ViewModel => DataContext as ResolverViewModel;

        public FieldSelectionPage() {
            InitializeComponent();
        }

        private async void BrowseXEL_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog {
                Multiselect = true,
                CheckPathExists = true,
                CheckFileExists = true,
                Filter = "XEL files (*.xel)|*.xel|All files (*.*)|*.*",
                Title = "Select XEL file(s)"
            };
            if (dlg.ShowDialog(Window.GetWindow(this)) == true) {
                await LoadXELFiles(dlg.FileNames);
            }
        }

        internal async Task LoadXELFiles(string[] fileNames) {
            SelectedFileNames = fileNames;
            FileInfoText.Text = fileNames.Length == 1
                ? Path.GetFileName(fileNames[0])
                : $"{fileNames.Length} files selected";

            if (ViewModel == null) return;
            ViewModel.StatusMessage = "Analyzing XEL files for available fields...";
            var fields = await ViewModel.GetDistinctXELFieldsAsync(fileNames);
            if (fields.Item1.Count + fields.Item2.Count == 0) {
                ViewModel.StatusMessage = "No fields found in XEL files.";
                return;
            }
            LoadFields(fields.Item1, fields.Item2);
            ViewModel.StatusMessage = "Fields loaded. Select the relevant fields, then click Next.";
        }

        internal void LoadFields(List<string> actions, List<string> fields) {
            _items.Clear();
            var callstackPatterns = new[] { "callstack", "call_stack", "stack_frames" };
            foreach (var a in actions) {
                bool preCheck = callstackPatterns.Any(p => a.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
                _items.Add(new FieldItem { Category = "Action", Name = a, IsChecked = preCheck });
            }
            foreach (var f in fields) {
                bool preCheck = callstackPatterns.Any(p => f.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
                _items.Add(new FieldItem { Category = "Field", Name = f, IsChecked = preCheck });
            }
            FieldsListView.ItemsSource = null;
            FieldsListView.ItemsSource = _items;
        }

        internal List<string> GetSelectedFields() {
            return _items.Where(i => i.IsChecked).Select(i => i.Name).ToList();
        }

        private void AddItem_Click(object sender, RoutedEventArgs e) {
            _items.Add(new FieldItem { Category = "Field", Name = "new_field", IsChecked = true });
            FieldsListView.Items.Refresh();
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e) {
            var selected = FieldsListView.SelectedItems.Cast<FieldItem>().ToList();
            foreach (var item in selected) _items.Remove(item);
            FieldsListView.Items.Refresh();
        }

        private void Page_DragOver(object sender, DragEventArgs e) {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private async void Page_Drop(object sender, DragEventArgs e) {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;
            var xelFiles = files.Where(f => Path.GetExtension(f).Equals(".xel", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (xelFiles.Length > 0) await LoadXELFiles(xelFiles);
        }
    }
}
