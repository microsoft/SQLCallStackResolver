// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
using System.Windows.Threading;

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public class ResolverViewModel : BaseViewModel {
        internal readonly StackResolver _resolver = new StackResolver();
        private CancellationTokenSource _cts;
        private readonly Dispatcher _dispatcher;

        internal static readonly string SqlBuildInfoFileName = @"sqlbuildinfo.json";
        internal static readonly string LastUpdatedTimestampFileName = @"lastupdated.txt";
        internal static readonly string LastUpdatedTimestampFormat = "yyyy-MM-dd HH:mm";
        internal static readonly string LastUpdatedTimestampCulture = "en-US";
        internal static readonly string LatestReleaseTimestampFileName = @"latestrelease.txt";
        internal static readonly string LatestReleaseTimestampFormat = "yyyy-MM-dd HH:mm";
        internal static readonly string LatestReleaseTimestampCulture = "en-US";

        internal ResolverViewModel() {
            _dispatcher = Dispatcher.CurrentDispatcher;
            ResolveCommand = new RelayCommand(_ => Resolve(), _ => !IsProcessing && !string.IsNullOrWhiteSpace(PdbPaths));
            CancelCommand = new RelayCommand(_ => Cancel(), _ => IsProcessing);
            NextStepCommand = new RelayCommand(_ => CurrentStep++, _ => CurrentStep < TotalSteps - 1);
            PreviousStepCommand = new RelayCommand(_ => CurrentStep--, _ => CurrentStep > 0);
            PasteFromClipboardCommand = new RelayCommand(_ => PasteFromClipboard());
        }

        // -- Mode --
        private bool _isWizardMode = Properties.Settings.Default.UseWizardMode;
        public bool IsWizardMode {
            get => _isWizardMode;
            set { if (SetField(ref _isWizardMode, value, nameof(IsWizardMode))) { Properties.Settings.Default.UseWizardMode = value; Properties.Settings.Default.Save(); } }
        }

        // -- Wizard Navigation --
        public int TotalSteps => 4;
        private int _currentStep;
        public int CurrentStep {
            get => _currentStep;
            set { SetField(ref _currentStep, value, nameof(CurrentStep)); OnPropertyChanged(nameof(CanGoBack)); OnPropertyChanged(nameof(CanGoNext)); OnPropertyChanged(nameof(IsOnResolvePage)); }
        }
        public bool CanGoBack => _currentStep > 0;
        public bool CanGoNext => _currentStep < TotalSteps - 1;
        public bool IsOnResolvePage => _currentStep == TotalSteps - 1;

        // -- Input --
        private string _inputText = string.Empty;
        public string InputText { get => _inputText; set => SetField(ref _inputText, value, nameof(InputText)); }

        private string _outputText = string.Empty;
        public string OutputText { get => _outputText; set => SetField(ref _outputText, value, nameof(OutputText)); }

        // -- Input Options --
        private bool _framesOnSingleLine;
        public bool FramesOnSingleLine { get => _framesOnSingleLine; set => SetField(ref _framesOnSingleLine, value, nameof(FramesOnSingleLine)); }

        private bool _relookupSource;
        public bool RelookupSource { get => _relookupSource; set => SetField(ref _relookupSource, value, nameof(RelookupSource)); }

        private bool _groupXEvents = true;
        public bool GroupXEvents { get => _groupXEvents; set => SetField(ref _groupXEvents, value, nameof(GroupXEvents)); }

        private string _baseAddressesString = string.Empty;
        public string BaseAddressesString { get => _baseAddressesString; set => SetField(ref _baseAddressesString, value, nameof(BaseAddressesString)); }

        // -- Symbol Config --
        private string _pdbPaths = string.Empty;
        public string PdbPaths { get => _pdbPaths; set => SetField(ref _pdbPaths, value, nameof(PdbPaths)); }

        private bool _pdbRecurse = true;
        public bool PdbRecurse { get => _pdbRecurse; set => SetField(ref _pdbRecurse, value, nameof(PdbRecurse)); }

        private bool _cachePDB;
        public bool CachePDB { get => _cachePDB; set => SetField(ref _cachePDB, value, nameof(CachePDB)); }

        private string _binaryPaths = string.Empty;
        public string BinaryPaths { get => _binaryPaths; set => SetField(ref _binaryPaths, value, nameof(BinaryPaths)); }

        private bool _dllRecurse;
        public bool DllRecurse { get => _dllRecurse; set => SetField(ref _dllRecurse, value, nameof(DllRecurse)); }

        // -- Output Options --
        private bool _includeLineNumbers = true;
        public bool IncludeLineNumbers { get => _includeLineNumbers; set => SetField(ref _includeLineNumbers, value, nameof(IncludeLineNumbers)); }

        private bool _includeOffsets = true;
        public bool IncludeOffsets { get => _includeOffsets; set => SetField(ref _includeOffsets, value, nameof(IncludeOffsets)); }

        private bool _showInlineFrames = true;
        public bool ShowInlineFrames { get => _showInlineFrames; set => SetField(ref _showInlineFrames, value, nameof(ShowInlineFrames)); }

        private string _outputFilePath = string.Empty;
        public string OutputFilePath { get => _outputFilePath; set => SetField(ref _outputFilePath, value, nameof(OutputFilePath)); }

        // -- Status --
        private string _statusMessage = "Ready";
        public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value, nameof(StatusMessage)); }

        private int _progressPercent;
        public int ProgressPercent { get => _progressPercent; set => SetField(ref _progressPercent, value, nameof(ProgressPercent)); }

        private bool _isProcessing;
        public bool IsProcessing { get => _isProcessing; set { SetField(ref _isProcessing, value, nameof(IsProcessing)); OnPropertyChanged(nameof(IsNotProcessing)); } }
        public bool IsNotProcessing => !_isProcessing;

        // -- Commands --
        public ICommand ResolveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand NextStepCommand { get; }
        public ICommand PreviousStepCommand { get; }
        public ICommand PasteFromClipboardCommand { get; }

        // -- Validate inputs (returns error message or null if valid) --
        internal string ValidateInputs() {
            if (string.IsNullOrWhiteSpace(PdbPaths))
                return "Please specify path(s) to PDB files.";

            if (_resolver.IsInputVAOnly(InputText) && string.IsNullOrEmpty(BaseAddressesString))
                return "The input seems to be entirely virtual addresses, but no module base address information has been provided. Please provide base addresses or change the input.";

            var res = _resolver.ProcessBaseAddresses(BaseAddressesString);
            if (!res)
                return "Cannot interpret the module base address information. Make sure you have the output of:\nselect name, base_address from sys.dm_os_loaded_modules where name not like '%.rll'";

            bool isSingleLineInput = _resolver.IsInputSingleLine(InputText, ConfigurationManager.AppSettings["PatternsToTreatAsMultiline"]);
            if (isSingleLineInput && !FramesOnSingleLine)
                return "Your input appears to have all frames on a single line, but 'Frames on single line' is not checked. Please enable it or adjust your input.";

            if (!isSingleLineInput && FramesOnSingleLine)
                return "Your input appears to have multiple lines, but 'Frames on single line' is checked. Please uncheck it or adjust your input.";

            return null;
        }

        internal async void Resolve() {
            var validationError = ValidateInputs();
            if (validationError != null) {
                MessageBox.Show(validationError, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsProcessing = true;
            OutputText = string.Empty;
            StatusMessage = "Parsing input...";
            try {
                List<StackDetails> allStacks;
                using (_cts = new CancellationTokenSource()) {
                    var parseTask = _resolver.GetListofCallStacksAsync(InputText, FramesOnSingleLine, RelookupSource, _cts);
                    allStacks = await MonitorTaskAsync(parseTask);
                }

                if (allStacks != null && allStacks.Any()) {
                    using (_cts = new CancellationTokenSource()) {
                        var resolveTask = _resolver.ResolveCallstacksAsync(
                            allStacks, PdbPaths, PdbRecurse,
                            string.IsNullOrEmpty(BinaryPaths) ? null : BinaryPaths.Split(';').ToList(),
                            DllRecurse, IncludeLineNumbers, RelookupSource,
                            IncludeOffsets, ShowInlineFrames, CachePDB, OutputFilePath, _cts);
                        var result = await MonitorTaskAsync(resolveTask);
                        if (result != null) OutputText = result;
                    }
                }

                if (OutputText.Contains(StackResolver.WARNING_PREFIX)) {
                    MessageBox.Show(
                        "One or more potential issues exist in the output. This is sometimes due to mismatched symbols; please double-check symbol paths and re-run if needed.",
                        "Potential issues with the output", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                StatusMessage = "Resolution complete.";
            } catch (OperationCanceledException) {
                StatusMessage = StackResolver.OperationCanceled;
            } catch (AggregateException ae) when (ae.InnerExceptions.Any(ex => ex is OperationCanceledException)) {
                StatusMessage = StackResolver.OperationCanceled;
            } catch (Exception ex) {
                StatusMessage = $"Error: {ex.Message}";
            } finally {
                IsProcessing = false;
                ProgressPercent = 0;
            }
        }

        private async Task<T> MonitorTaskAsync<T>(Task<T> task) {
            while (!task.IsCompleted) {
                await Task.Delay(StackResolver.OperationWaitIntervalMilliseconds);
                StatusMessage = _resolver.StatusMessage ?? StatusMessage;
                ProgressPercent = Math.Min(StackResolver.Operation100Percent, _resolver.PercentComplete);
            }
            StatusMessage = _resolver.StatusMessage ?? StatusMessage;
            ProgressPercent = Math.Min(StackResolver.Operation100Percent, _resolver.PercentComplete);
            return await task;
        }

        private void Cancel() {
            _cts?.Cancel();
            StatusMessage = "Cancellation requested...";
        }

        private void PasteFromClipboard() {
            if (Properties.Settings.Default.promptForClipboardPaste) {
                var resProceed = MessageBox.Show("Proceeding will paste the contents of your clipboard and attempt to resolve them. Are you sure?",
                    "Proceed with paste from clipboard", MessageBoxButton.YesNo, MessageBoxImage.Information);
                var resRememberChoice = MessageBox.Show("Should we remember your choice for the future?",
                    "Save your choice?", MessageBoxButton.YesNo, MessageBoxImage.Question);

                Properties.Settings.Default.promptForClipboardPaste = (resRememberChoice == MessageBoxResult.No);
                Properties.Settings.Default.choiceForClipboardPaste = (resProceed == MessageBoxResult.Yes);
                Properties.Settings.Default.Save();
            }

            if (!Properties.Settings.Default.choiceForClipboardPaste) {
                StatusMessage = (Properties.Settings.Default.promptForClipboardPaste ? "You chose to not" : "You have chosen never to") + " paste clipboard contents. Nothing to do!";
                return;
            }

            InputText = Clipboard.GetText();
            Resolve();
        }

        internal async Task LoadXELFilesAsync(string[] fileNames, List<string> relevantFields) {
            if (!relevantFields.Any()) {
                StatusMessage = "No fields were selected for import from the XEL files. Nothing to do!";
                return;
            }
            IsProcessing = true;
            StatusMessage = "Loading from XEL files; please wait. This may take a while!";
            try {
                using (_cts = new CancellationTokenSource()) {
                    var xelTask = _resolver.ExtractFromXELAsync(fileNames, GroupXEvents, relevantFields, _cts);
                    var result = await MonitorTaskAsync(xelTask);
                    if (result != null && !_cts.IsCancellationRequested)
                        InputText = result.Item2;
                }
                StatusMessage = "Finished importing callstacks from XEL file(s)!";
            } catch (OperationCanceledException) {
                StatusMessage = StackResolver.OperationCanceled;
            } finally {
                IsProcessing = false;
                ProgressPercent = 0;
            }
        }

        internal async Task<Tuple<List<string>, List<string>>> GetDistinctXELFieldsAsync(string[] fileNames) {
            return await _resolver.GetDistinctXELFieldsAsync(fileNames, 1000);
        }

        internal async Task CheckForUpdatesAsync() {
            try {
                DateTime latestReleaseDateTimeServer = DateTime.MinValue;
                DateTime latestReleaseDateTimeLocal = DateTime.MinValue;
                var latestReleaseURLs = ConfigurationManager.AppSettings["LatestReleaseURLs"].Split(';');
                var t = latestReleaseURLs.Select(url => Utils.GetTextFromUrl(url)).ToArray();
                var taskRes = (await Task.WhenAll(t)).Where(s => !string.IsNullOrWhiteSpace(s));
                if (taskRes.Any()) latestReleaseDateTimeServer = DateTime.ParseExact(taskRes.First().Trim(), LatestReleaseTimestampFormat, new CultureInfo(LatestReleaseTimestampCulture));

                string latestReleaseDateStringLocal = "Unknown";
                if (File.Exists(LatestReleaseTimestampFileName)) {
                    using var sr = new StreamReader(LatestReleaseTimestampFileName);
                    latestReleaseDateStringLocal = (await sr.ReadToEndAsync()).Trim();
                    latestReleaseDateTimeLocal = DateTime.ParseExact(latestReleaseDateStringLocal, LatestReleaseTimestampFormat, new CultureInfo(LatestReleaseTimestampCulture));
                }

                if (latestReleaseDateTimeServer > latestReleaseDateTimeLocal) {
                    MessageBox.Show(
                        $"You are currently on release: {latestReleaseDateStringLocal} of SQLCallStackResolver. There is a newer release ({latestReleaseDateTimeServer.ToString(LatestReleaseTimestampFormat)}) available.\nYou should exit, then download the latest release from https://aka.ms/SQLStack/releases.",
                        "New release available.", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DateTime lastUpdDateTimeServer = DateTime.MinValue;
                DateTime lastUpdDateTimeLocal = DateTime.MinValue;
                var sqlBuildInfoUpdateURLs = ConfigurationManager.AppSettings["SQLBuildInfoUpdateURLs"].Split(';');
                var sqlBuildInfoURLs = ConfigurationManager.AppSettings["SQLBuildInfoURLs"].Split(';');
                t = sqlBuildInfoUpdateURLs.Select(url => Utils.GetTextFromUrl(url)).ToArray();
                taskRes = (await Task.WhenAll(t)).Where(s => !string.IsNullOrWhiteSpace(s));
                if (taskRes.Any()) lastUpdDateTimeServer = DateTime.ParseExact(taskRes.First().Trim(), LastUpdatedTimestampFormat, new CultureInfo(LastUpdatedTimestampCulture));

                if (File.Exists(LastUpdatedTimestampFileName)) {
                    using var sr = new StreamReader(LastUpdatedTimestampFileName);
                    var lastUpdLocal = (await sr.ReadToEndAsync()).Trim();
                    lastUpdDateTimeLocal = DateTime.ParseExact(lastUpdLocal, LastUpdatedTimestampFormat, new CultureInfo(LastUpdatedTimestampCulture));
                }

                if (lastUpdDateTimeServer > lastUpdDateTimeLocal) {
                    var res = MessageBox.Show("The SQLBuildInfo.json file was updated recently on GitHub. Do you wish to update your copy with the newer version?",
                        "SQL build info updated", MessageBoxButton.YesNo);
                    if (MessageBoxResult.Yes == res) {
                        StatusMessage = "Trying to update SQL build info from GitHub...";
                        t = sqlBuildInfoURLs.Select(jsonURL => Utils.GetTextFromUrl(jsonURL)).ToArray();
                        taskRes = (await Task.WhenAll(t)).Where(s => !string.IsNullOrWhiteSpace(s));
                        if (taskRes.Any()) {
                            using var writer = new StreamWriter(SqlBuildInfoFileName);
                            writer.Write(taskRes.First());
                            writer.Flush();
                            writer.Close();
                            using var wr = new StreamWriter(LastUpdatedTimestampFileName, false);
                            wr.Write(lastUpdDateTimeServer.ToString(LastUpdatedTimestampFormat, new CultureInfo(LastUpdatedTimestampCulture)));
                            wr.Flush();
                            wr.Close();
                            StatusMessage = "Successfully updated SQL build info!";
                        } else {
                            MessageBox.Show("Could not download the SQL build Info file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            } catch { /* best effort update check */ }
        }

        internal void AppendPdbPath(string path) {
            if (string.IsNullOrEmpty(path)) return;
            PdbPaths = string.IsNullOrEmpty(PdbPaths) ? path : PdbPaths + ";" + path;
        }

        internal void AppendBinaryPath(string path) {
            if (string.IsNullOrEmpty(path)) return;
            BinaryPaths = string.IsNullOrEmpty(BinaryPaths) ? path : BinaryPaths + ";" + path;
        }
    }
}
