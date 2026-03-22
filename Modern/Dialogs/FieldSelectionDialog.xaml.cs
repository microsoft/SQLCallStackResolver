// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class FieldSelectionDialog : Window {
        public List<string> SelectedEventItems { get; private set; } = new List<string>();
        private readonly List<FieldItem> _items = new List<FieldItem>();

        public FieldSelectionDialog(List<string> actions, List<string> fields) {
            InitializeComponent();
            var callstackPatterns = new[] { "callstack", "call_stack", "stack_frames" };
            foreach (var a in actions) {
                bool preCheck = callstackPatterns.Any(p => a.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
                _items.Add(new FieldItem { Category = "Action", Name = a, IsChecked = preCheck });
            }
            foreach (var f in fields) {
                bool preCheck = callstackPatterns.Any(p => f.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
                _items.Add(new FieldItem { Category = "Field", Name = f, IsChecked = preCheck });
            }
            FieldsListView.ItemsSource = _items;
        }

        private void OK_Click(object sender, RoutedEventArgs e) {
            SelectedEventItems = _items.Where(i => i.IsChecked).Select(i => i.Name).ToList();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            SelectedEventItems = new List<string>();
            DialogResult = false;
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
    }

    internal class FieldItem : INotifyPropertyChanged {
        private bool _isChecked;
        public bool IsChecked { get => _isChecked; set { _isChecked = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked))); } }
        public string Category { get; set; }
        public string Name { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
