// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class BaseAddressDialog : Window {
        public string BaseAddressesString { get; private set; }

        public BaseAddressDialog(string currentValue) {
            InitializeComponent();
            InputAddresses.Text = currentValue ?? string.Empty;
        }

        private void OK_Click(object sender, RoutedEventArgs e) {
            BaseAddressesString = InputAddresses.Text;
            DialogResult = true;
        }

        private void LoadFromFile_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Load base addresses from file"
            };
            if (dlg.ShowDialog(this) == true) {
                InputAddresses.Text = File.ReadAllText(dlg.FileName);
            }
        }

        private void InputAddresses_DragOver(object sender, DragEventArgs e) {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void InputAddresses_Drop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0) {
                    InputAddresses.Text = File.ReadAllText(files[0]);
                }
            }
        }
    }
}
