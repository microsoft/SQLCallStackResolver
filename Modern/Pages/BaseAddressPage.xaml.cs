// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class BaseAddressPage : UserControl {
        public BaseAddressPage() {
            InitializeComponent();
        }

        private void LoadFromFile_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Load base addresses from file"
            };
            if (dlg.ShowDialog(Window.GetWindow(this)) == true) {
                var vm = DataContext as ResolverViewModel;
                if (vm != null) vm.BaseAddressesString = File.ReadAllText(dlg.FileName);
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
                    var vm = DataContext as ResolverViewModel;
                    if (vm != null) vm.BaseAddressesString = File.ReadAllText(files[0]);
                }
            }
        }
    }
}
