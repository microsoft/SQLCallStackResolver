// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class ResolvePage : UserControl {
        public ResolvePage() {
            InitializeComponent();
        }

        private void CopyAll_Click(object sender, RoutedEventArgs e) {
            if (DataContext is ResolverViewModel vm && !string.IsNullOrEmpty(vm.OutputText)) {
                Clipboard.SetText(vm.OutputText);
                vm.StatusMessage = "Output copied to clipboard.";
            }
        }
    }
}
