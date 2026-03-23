// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
using System.Windows.Media.Animation;

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class MainWindow : Window {
        private readonly ResolverViewModel _viewModel = new ResolverViewModel();

        public MainWindow() {
            InitializeComponent();
            DataContext = _viewModel;
            WizardRadio.IsChecked = _viewModel.IsWizardMode;
            ClassicRadio.IsChecked = !_viewModel.IsWizardMode;

            // Initialize theme from system setting
            bool isDark = DwmInterop.IsSystemDarkTheme();
            ApplyTheme(isDark);
            ThemeToggle.IsChecked = isDark;

            // Disable main content while splash is showing
            MainContent.IsEnabled = false;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            // Apply DWM dark mode title bar if needed
            DwmInterop.ApplyImmersiveDarkMode(this, ThemeToggle.IsChecked == true);

            // Populate splash version text
            if (File.Exists(ResolverViewModel.LatestReleaseTimestampFileName)) {
                using var sr = new StreamReader(ResolverViewModel.LatestReleaseTimestampFileName);
                var releaseDate = (await sr.ReadToEndAsync()).Trim();
                SplashVersion.Text = $"Release: {releaseDate}";
                Title += $" (release: {releaseDate})";
            }

            // The splash overlay is visible by default; user must click Accept
        }

        private async void SplashAccept_Click(object sender, RoutedEventArgs e) {
            // Fade out the splash overlay
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
            fadeOut.Completed += (s, _) => {
                SplashOverlay.Visibility = Visibility.Collapsed;
                MainContent.IsEnabled = true;
            };
            SplashOverlay.BeginAnimation(OpacityProperty, fadeOut);

            await _viewModel.CheckForUpdatesAsync();
        }

        /// <summary>Show a ModernWpf ContentDialog as a themed in-window dialog.</summary>
        internal static async Task ShowContentDialogAsync(string title, string message, string closeButtonText = "OK") {
            var dialog = new ModernWpf.Controls.ContentDialog {
                Title = title,
                Content = new TextBlock {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 450
                },
                CloseButtonText = closeButtonText
            };
            await dialog.ShowAsync();
        }

        /// <summary>Show a ModernWpf ContentDialog with Primary + Close buttons. Returns true if Primary was clicked.</summary>
        internal static async Task<bool> ShowConfirmDialogAsync(string title, string message, string primaryText = "Yes", string closeText = "No") {
            var dialog = new ModernWpf.Controls.ContentDialog {
                Title = title,
                Content = new TextBlock {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 450
                },
                PrimaryButtonText = primaryText,
                CloseButtonText = closeText
            };
            var result = await dialog.ShowAsync();
            return result == ModernWpf.Controls.ContentDialogResult.Primary;
        }

        private void ApplyTheme(bool isDark) {
            ModernWpf.ThemeManager.Current.ApplicationTheme = isDark
                ? ModernWpf.ApplicationTheme.Dark
                : ModernWpf.ApplicationTheme.Light;
            DwmInterop.ApplyImmersiveDarkMode(this, isDark);
            // Sun icon for light mode, Moon for dark mode
            ThemeIcon.Text = isDark ? "\uE706" : "\uE708";
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e) {
            bool isDark = ThemeToggle.IsChecked == true;
            ApplyTheme(isDark);
        }

        private void WizardRadio_Checked(object sender, RoutedEventArgs e) {
            if (_viewModel != null) _viewModel.IsWizardMode = true;
        }

        private void ClassicRadio_Checked(object sender, RoutedEventArgs e) {
            if (_viewModel != null) _viewModel.IsWizardMode = false;
        }
    }
}
