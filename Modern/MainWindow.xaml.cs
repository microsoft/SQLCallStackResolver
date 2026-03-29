// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class MainWindow : Window {
        private readonly ResolverViewModel _viewModel = new ResolverViewModel();
        private static readonly string CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public MainWindow() {
            InitializeComponent();
            DataContext = _viewModel;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            WizardRadio.IsChecked = _viewModel.IsWizardMode;
            ClassicRadio.IsChecked = !_viewModel.IsWizardMode;

            // Initialize theme from system setting
            bool isDark = DwmInterop.IsSystemDarkTheme();
            ApplyTheme(isDark);
            ThemeToggle.IsChecked = isDark;

            // Check if splash was already accepted for this version
            if (Properties.Settings.Default.AcceptedSplashVersion == CurrentVersion) {
                SplashOverlay.Visibility = Visibility.Collapsed;
                MainContent.IsEnabled = true;
            } else {
                MainContent.IsEnabled = false;
            }

            SplashIcon.Source = AppIcon;
            Loaded += MainWindow_Loaded;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(ResolverViewModel.StatusMessage))
                FlashStatusBar();
        }

        private void FlashStatusBar() {
            var accent = TryFindResource("SystemControlForegroundAccentBrush") as SolidColorBrush;
            var flashColor = accent?.Color ?? Colors.DodgerBlue;
            // Brief highlight: accent color fading to transparent over 600ms
            var anim = new ColorAnimation(
                Color.FromArgb(60, flashColor.R, flashColor.G, flashColor.B),
                Colors.Transparent,
                TimeSpan.FromMilliseconds(600)) {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            StatusFlashBrush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
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

            // If splash was already accepted for this version, check for updates directly
            if (Properties.Settings.Default.AcceptedSplashVersion == CurrentVersion) {
                await _viewModel.CheckForUpdatesAsync();
            }
        }

        private async void SplashAccept_Click(object sender, RoutedEventArgs e) {
            // Remember this version so splash won't show again until next upgrade
            Properties.Settings.Default.AcceptedSplashVersion = CurrentVersion;
            Properties.Settings.Default.Save();

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

        private async void HelpAbout_Click(object sender, RoutedEventArgs e) {
            string releaseInfo = "";
            if (File.Exists(ResolverViewModel.LatestReleaseTimestampFileName)) {
                using var sr = new StreamReader(ResolverViewModel.LatestReleaseTimestampFileName);
                releaseInfo = $"\nRelease: {(await sr.ReadToEndAsync()).Trim()}";
            }

            var dialog = new ModernWpf.Controls.ContentDialog {
                Title = "About SQLCallStackResolver",
                Content = new StackPanel {
                    Children = {
                        new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12),
                            Children = {
                                new Image { Source = AppIcon,
                                            Width = 48, Height = 48, Margin = new Thickness(0, 0, 12, 0) },
                                new StackPanel { VerticalAlignment = VerticalAlignment.Center,
                                    Children = {
                                        new TextBlock { Text = "SQLCallStackResolver", FontSize = 18, FontWeight = FontWeights.Bold },
                                        new TextBlock { Text = $"Version {CurrentVersion}{releaseInfo}", FontSize = 12, Opacity = 0.7 }
                                    }
                                }
                            }
                        },
                        new TextBlock { Text = "Copyright \u00A9 2025 Microsoft Corporation. All rights reserved.",
                                        TextWrapping = TextWrapping.Wrap, FontSize = 12, Margin = new Thickness(0, 0, 0, 8) },
                        new TextBlock { Text = "https://aka.ms/sqlstack", FontSize = 12, Opacity = 0.7 }
                    }
                },
                PrimaryButtonText = "View License",
                CloseButtonText = "OK"
            };

            var result = await dialog.ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary) {
                ShowSplashOverlay();
            }
        }

        private static System.Windows.Media.Imaging.BitmapFrame _appIcon;
        private static System.Windows.Media.Imaging.BitmapFrame AppIcon {
            get {
                if (_appIcon == null) {
                    var decoder = new System.Windows.Media.Imaging.IconBitmapDecoder(
                        new Uri("pack://application:,,,/app.ico"),
                        System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                    _appIcon = decoder.Frames.OrderByDescending(f => f.PixelWidth).First();
                }
                return _appIcon;
            }
        }

        internal void ShowSplashOverlay() {
            // Clear any previous fade-out animation so we can set Opacity directly
            SplashOverlay.BeginAnimation(OpacityProperty, null);
            SplashOverlay.Opacity = 1;
            SplashOverlay.Visibility = Visibility.Visible;
            MainContent.IsEnabled = false;
        }
    }
}
