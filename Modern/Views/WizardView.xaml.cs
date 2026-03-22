// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class WizardView : UserControl {
        private readonly UserControl[] _pages;

        public WizardView() {
            InitializeComponent();
            _pages = new UserControl[] {
                new InputPage(),
                new SymbolConfigPage(),
                new OptionsPage(),
                new ResolvePage()
            };
            DataContextChanged += (s, e) => {
                foreach (var page in _pages) page.DataContext = DataContext;
                UpdatePage();
            };
            Loaded += (s, e) => UpdatePage();
        }

        private void UpdatePage() {
            var vm = DataContext as ResolverViewModel;
            if (vm == null) return;
            var step = Math.Max(0, Math.Min(vm.CurrentStep, _pages.Length - 1));
            PageContent.Content = _pages[step];
        }

        // Re-subscribe to property changes when DataContext is available
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
            base.OnPropertyChanged(e);
            if (e.Property == DataContextProperty && DataContext is ResolverViewModel vm) {
                vm.PropertyChanged += (s, args) => {
                    if (args.PropertyName == nameof(ResolverViewModel.CurrentStep)) UpdatePage();
                };
            }
        }

        private void StepLabel_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (sender is System.Windows.FrameworkElement fe && fe.Tag is string tagStr && int.TryParse(tagStr, out int step)) {
                var vm = DataContext as ResolverViewModel;
                if (vm != null) vm.CurrentStep = step;
            }
        }
    }
}
