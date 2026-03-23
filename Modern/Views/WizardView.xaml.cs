// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
using System.Collections.Specialized;
using System.Windows.Media;

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class WizardView : UserControl {
        // Page registry: maps step IDs to their UserControl instances
        private readonly Dictionary<string, UserControl> _pageRegistry = new Dictionary<string, UserControl>();
        private readonly InputSourcePage _inputSourcePage = new InputSourcePage();
        private readonly InputPage _inputPage = new InputPage();
        private readonly FieldSelectionPage _fieldSelectionPage = new FieldSelectionPage();
        private readonly BaseAddressPage _baseAddressPage = new BaseAddressPage();
        private readonly SymbolConfigPage _symbolConfigPage = new SymbolConfigPage();
        private readonly OptionsPage _optionsPage = new OptionsPage();
        private readonly ResolvePage _resolvePage = new ResolvePage();

        public WizardView() {
            InitializeComponent();

            _pageRegistry["InputSource"] = _inputSourcePage;
            _pageRegistry["Input"] = _inputPage;
            _pageRegistry["BaseAddress"] = _baseAddressPage;
            _pageRegistry["FieldSelection"] = _fieldSelectionPage;
            _pageRegistry["Symbols"] = _symbolConfigPage;
            _pageRegistry["Options"] = _optionsPage;
            _pageRegistry["Resolve"] = _resolvePage;

            DataContextChanged += (s, e) => {
                foreach (var page in _pageRegistry.Values) page.DataContext = DataContext;
                if (DataContext is ResolverViewModel vm) {
                    StepsList.ItemsSource = vm.WizardSteps;
                    vm.WizardSteps.CollectionChanged += (_, __) =>
                        Dispatcher.BeginInvoke(new Action(UpdateSidebarHighlight), System.Windows.Threading.DispatcherPriority.Loaded);
                    vm.SubStepAction += OnSubStepAction;
                }
                UpdatePage();
            };
            Loaded += (s, e) => { UpdatePage(); };
        }

        private void RefreshSidebar() {
            var vm = DataContext as ResolverViewModel;
            if (vm == null) return;
            StepsList.ItemsSource = null;
            StepsList.ItemsSource = vm.WizardSteps;
            Dispatcher.BeginInvoke(new Action(UpdateSidebarHighlight), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void UpdateSidebarHighlight() {
            var vm = DataContext as ResolverViewModel;
            if (vm == null) return;

            for (int i = 0; i < StepsList.Items.Count; i++) {
                var container = StepsList.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container == null) continue;

                var panel = FindChild<StackPanel>(container);
                if (panel == null) continue;

                bool isCurrent = i == vm.CurrentStep;
                bool isPast = i < vm.CurrentStep;
                var brush = isCurrent ? Brushes.DodgerBlue : (isPast ? Brushes.Gray : Brushes.DarkGray);
                var weight = isCurrent ? FontWeights.Bold : FontWeights.Normal;

                foreach (var tb in panel.Children.OfType<TextBlock>()) {
                    tb.Foreground = brush;
                    tb.FontWeight = weight;
                }
            }
        }

        private void UpdatePage() {
            var vm = DataContext as ResolverViewModel;
            if (vm == null) return;
            var step = Math.Max(0, Math.Min(vm.CurrentStep, vm.WizardSteps.Count - 1));
            var stepDef = vm.WizardSteps[step];
            if (_pageRegistry.TryGetValue(stepDef.Id, out var page))
                PageContent.Content = page;

            Dispatcher.BeginInvoke(new Action(UpdateSidebarHighlight), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
            base.OnPropertyChanged(e);
            if (e.Property == DataContextProperty && DataContext is ResolverViewModel vm) {
                vm.PropertyChanged += (s, args) => {
                    if (args.PropertyName == nameof(ResolverViewModel.CurrentStep)) UpdatePage();
                };
            }
        }

        private void StepItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var vm = DataContext as ResolverViewModel;
            if (vm == null) return;

            if (sender is FrameworkElement fe) {
                var step = fe.DataContext as WizardStep;
                if (step != null) {
                    int idx = vm.WizardSteps.IndexOf(step);
                    if (idx >= 0) vm.CurrentStep = idx;
                }
            }
        }

        private async void OnSubStepAction(string action) {
            var vm = DataContext as ResolverViewModel;
            if (vm == null) return;

            if (action == "ChooseDirectInput") {
                // Switch to the direct text input path
                vm.RemoveSubStep("FieldSelection");
                vm.RemoveSubStep("BaseAddress");
                if (!vm.HasSubStep("Input"))
                    vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepInput);
                int idx = vm.WizardSteps.IndexOf(ResolverViewModel.StepInput);
                if (idx >= 0) vm.CurrentStep = idx;
                return;
            }

            if (action == "ChooseXELImport") {
                // Switch to the XEL import path
                vm.RemoveSubStep("Input");
                vm.RemoveSubStep("BaseAddress");
                if (!vm.HasSubStep("FieldSelection"))
                    vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepFieldSelection);
                int idx = vm.WizardSteps.IndexOf(ResolverViewModel.StepFieldSelection);
                if (idx >= 0) vm.CurrentStep = idx;

                // If files were pre-loaded (e.g. via drag-drop on InputSource), auto-analyze
                if (vm.PendingXELFileNames != null && vm.PendingXELFileNames.Length > 0) {
                    await _fieldSelectionPage.LoadXELFiles(vm.PendingXELFileNames);
                    vm.PendingXELFileNames = null;
                }
                return;
            }

            if (action == "ImportXEL") {
                var fileNames = _fieldSelectionPage.SelectedFileNames;
                if (fileNames == null || fileNames.Length == 0) {
                    vm.StatusMessage = "Please select XEL file(s) first.";
                    return;
                }

                var selectedFields = _fieldSelectionPage.GetSelectedFields();
                if (!selectedFields.Any()) {
                    vm.StatusMessage = "No fields selected. Please select at least one field.";
                    return;
                }

                await vm.LoadXELFilesAsync(fileNames, selectedFields);

                // After import, check if the imported data needs base addresses
                if (!string.IsNullOrWhiteSpace(vm.InputText)
                    && vm._resolver.IsInputVAOnly(vm.InputText)
                    && string.IsNullOrEmpty(vm.BaseAddressesString)) {
                    if (!vm.HasSubStep("BaseAddress"))
                        vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepBaseAddress);
                    int baIdx = vm.WizardSteps.IndexOf(ResolverViewModel.StepBaseAddress);
                    if (baIdx >= 0) {
                        vm.CurrentStep = baIdx;
                        UpdatePage();
                        return;
                    }
                }

                // Otherwise navigate to Symbols
                int symbolsIdx = vm.WizardSteps.IndexOf(ResolverViewModel.StepSymbols);
                if (symbolsIdx >= 0) {
                    vm.CurrentStep = symbolsIdx;
                    UpdatePage();
                }
            }
        }

        private static T FindChild<T>(DependencyObject parent) where T : DependencyObject {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++) {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var result = FindChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
