// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class InputSourcePage : UserControl {
        private ResolverViewModel ViewModel => DataContext as ResolverViewModel;

        public InputSourcePage() {
            InitializeComponent();
        }

        private void DirectInputCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            ViewModel?.RaiseSubStepAction("ChooseDirectInput");
        }

        private void XELImportCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            ViewModel?.RaiseSubStepAction("ChooseXELImport");
        }

        private void Card_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) {
            if (sender is Border b) b.BorderBrush = System.Windows.Media.Brushes.DodgerBlue;
        }

        private void Card_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
            if (sender is Border b) b.BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCCCC"));
        }

        private void InputSource_DragOver(object sender, DragEventArgs e) {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void InputSource_Drop(object sender, DragEventArgs e) {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;

            if (files.All(f => Path.GetExtension(f).Equals(".xel", StringComparison.OrdinalIgnoreCase))) {
                if (ViewModel != null) {
                    ViewModel.PendingXELFileNames = files;
                    ViewModel.RaiseSubStepAction("ChooseXELImport");
                }
            } else {
                if (ViewModel != null) {
                    var sb = new StringBuilder();
                    foreach (var file in files) sb.AppendLine(File.ReadAllText(file));
                    ViewModel.InputText = sb.ToString();
                    ViewModel.RaiseSubStepAction("ChooseDirectInput");
                }
            }
        }
    }
}
