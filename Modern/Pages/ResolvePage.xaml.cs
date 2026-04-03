// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class ResolvePage : UserControl {
        public ResolvePage() {
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
        }

        private void CopyAll_Click(object sender, RoutedEventArgs e) {
            if (DataContext is ResolverViewModel vm && !string.IsNullOrEmpty(vm.OutputText)) {
                Clipboard.SetText(vm.OutputText);
                vm.StatusMessage = "Output copied to clipboard.";
            }
        }

        private void Find_Executed(object sender, ExecutedRoutedEventArgs e) => findBar.Open();
        private void Find_Click(object sender, RoutedEventArgs e) => findBar.Open();
    }
}
