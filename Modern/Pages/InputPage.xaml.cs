// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class InputPage : UserControl {
        private ResolverViewModel ViewModel => DataContext as ResolverViewModel;

        public InputPage() {
            InitializeComponent();
            DataContextChanged += (s, e) => {
                if (DataContext is ResolverViewModel vm) {
                    vm.PropertyChanged += (_, args) => {
                        if (args.PropertyName == nameof(ResolverViewModel.InputText)
                            || args.PropertyName == nameof(ResolverViewModel.FramesOnSingleLine))
                            CheckSingleLineMismatch();
                    };
                }
            };
        }

        internal void CheckSingleLineMismatch() {
            if (ViewModel == null || string.IsNullOrWhiteSpace(ViewModel.InputText)) {
                SingleLineWarningBanner.Visibility = Visibility.Collapsed;
                return;
            }

            bool isSingleLine = ViewModel._resolver.IsInputSingleLine(
                ViewModel.InputText, ConfigurationManager.AppSettings["PatternsToTreatAsMultiline"]);

            if (isSingleLine && !ViewModel.FramesOnSingleLine) {
                SingleLineWarningText.Text = "Your input appears to have all frames on a single line, but 'Frames on single line' is not checked.";
                SingleLineWarningBanner.Visibility = Visibility.Visible;
                AdvancedOptionsExpander.IsExpanded = true;
            } else if (!isSingleLine && ViewModel.FramesOnSingleLine) {
                SingleLineWarningText.Text = "Your input appears to have multiple lines, but 'Frames on single line' is checked.";
                SingleLineWarningBanner.Visibility = Visibility.Visible;
                AdvancedOptionsExpander.IsExpanded = true;
            } else {
                SingleLineWarningBanner.Visibility = Visibility.Collapsed;
            }
        }

        private void AutoFixSingleLine_Click(object sender, RoutedEventArgs e) {
            if (ViewModel == null) return;
            bool isSingleLine = ViewModel._resolver.IsInputSingleLine(
                ViewModel.InputText, ConfigurationManager.AppSettings["PatternsToTreatAsMultiline"]);
            ViewModel.FramesOnSingleLine = isSingleLine;
        }

        private void CallStackInput_DragOver(object sender, DragEventArgs e) {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void CallStackInput_Drop(object sender, DragEventArgs e) {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;

            if (files.All(f => Path.GetExtension(f).Equals(".xel", StringComparison.OrdinalIgnoreCase))) {
                // Switch to XEL import path
                if (ViewModel != null) {
                    ViewModel.PendingXELFileNames = files;
                    ViewModel.RaiseSubStepAction("ChooseXELImport");
                }
            } else {
                var sb = new StringBuilder();
                foreach (var file in files) sb.AppendLine(File.ReadAllText(file));
                ViewModel.InputText = sb.ToString();
            }
        }
    }
}
