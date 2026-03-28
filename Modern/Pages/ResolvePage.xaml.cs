// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class ResolvePage : UserControl {
        public ResolvePage() {
            InitializeComponent();
            findBar.Attach(outputTextBox);
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
