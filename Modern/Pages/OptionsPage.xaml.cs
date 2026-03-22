// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class OptionsPage : UserControl {
        public OptionsPage() {
            InitializeComponent();
        }

        private void BrowseOutputPath_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.SaveFileDialog {
                FileName = "resolvedstacks.txt",
                Filter = "Text files (*.txt)|*.txt",
                Title = "Save output as"
            };
            if (dlg.ShowDialog(Window.GetWindow(this)) == true) {
                (DataContext as ResolverViewModel).OutputFilePath = dlg.FileName;
            }
        }
    }
}
