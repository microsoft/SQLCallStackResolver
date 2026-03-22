// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class MainWindow : Window {
        private readonly ResolverViewModel _viewModel = new ResolverViewModel();

        public MainWindow() {
            InitializeComponent();
            DataContext = _viewModel;
            WizardRadio.IsChecked = _viewModel.IsWizardMode;
            ClassicRadio.IsChecked = !_viewModel.IsWizardMode;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            MessageBox.Show(this,
                "Copyright (c) 2025 Microsoft Corporation. All rights reserved.\r\n\r\n" +
                "THIS SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, " +
                "INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. " +
                "IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, " +
                "WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.\r\n\r\n" +
                "USAGE OF THE MICROSOFT SYMBOL SERVER IS COVERED BY THE LICENSE TERMS PUBLISHED AT https://docs.microsoft.com/legal/windows-sdk/microsoft-symbol-server-license-terms.",
                "SQLCallStackResolver - Legal Notice", MessageBoxButton.OK, MessageBoxImage.Information);

            // update title with release info
            if (File.Exists(ResolverViewModel.LatestReleaseTimestampFileName)) {
                using var sr = new StreamReader(ResolverViewModel.LatestReleaseTimestampFileName);
                var releaseDate = (await sr.ReadToEndAsync()).Trim();
                Title += $" (release: {releaseDate})";
            }

            await _viewModel.CheckForUpdatesAsync();
        }

        private void WizardRadio_Checked(object sender, RoutedEventArgs e) {
            if (_viewModel != null) _viewModel.IsWizardMode = true;
        }

        private void ClassicRadio_Checked(object sender, RoutedEventArgs e) {
            if (_viewModel != null) _viewModel.IsWizardMode = false;
        }
    }
}
