// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    /// <summary>Describes a single wizard step (core or conditional sub-step).</summary>
    public class WizardStep : BaseViewModel {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Icon { get; set; }
        public bool IsConditional { get; set; }

        /// <summary>For conditional steps: the Id of the core step this follows.</summary>
        public string ParentStepId { get; set; }
    }

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

        // -- Core step definitions (always present) --
        internal static readonly WizardStep StepInputSource = new WizardStep { Id = "InputSource", Label = "Choose Input", Icon = "\uE8A5" };
        internal static readonly WizardStep StepSymbols = new WizardStep { Id = "Symbols", Label = "Configure Symbols", Icon = "\uE71C" };
        internal static readonly WizardStep StepOptions = new WizardStep { Id = "Options", Label = "Output Options", Icon = "\uE713" };
        internal static readonly WizardStep StepResolve = new WizardStep { Id = "Resolve", Label = "Resolve", Icon = "\uE768" };

        // -- Conditional sub-step definitions (inserted after InputSource based on user choice) --
        internal static readonly WizardStep StepInput = new WizardStep {
            Id = "Input", Label = "Provide Input", Icon = "\uE8B7",
            IsConditional = true, ParentStepId = "InputSource"
        };
        internal static readonly WizardStep StepFieldSelection = new WizardStep {
            Id = "FieldSelection", Label = "Import XEL", Icon = "\uE762",
            IsConditional = true, ParentStepId = "InputSource"
        };
        internal static readonly WizardStep StepBaseAddress = new WizardStep {
            Id = "BaseAddress", Label = "Base Addresses", Icon = "\uE81C",
            IsConditional = true, ParentStepId = "InputSource"
        };

        /// <summary>The ordered list of wizard steps, including any dynamically inserted sub-steps.</summary>
        public ObservableCollection<WizardStep> WizardSteps { get; } = new ObservableCollection<WizardStep>();

        /// <summary>Raised when a sub-step requests the WizardView to perform an action (e.g. load XEL fields).</summary>
        public event Action<string> SubStepAction;

        /// <summary>Invoke the SubStepAction event from outside the class (e.g. from pages).</summary>
        internal void RaiseSubStepAction(string action) => SubStepAction?.Invoke(action);

        internal ResolverViewModel() {
            _dispatcher = Dispatcher.CurrentDispatcher;

            WizardSteps.Add(StepInputSource);
            WizardSteps.Add(StepSymbols);
            WizardSteps.Add(StepResolve);

            ResolveCommand = new RelayCommand(_ => Resolve(), _ => !IsProcessing);
            CancelCommand = new RelayCommand(_ => Cancel(), _ => IsProcessing);
            NextStepCommand = new RelayCommand(_ => AdvanceStep(), _ => CurrentStep < WizardSteps.Count - 1);
            PreviousStepCommand = new RelayCommand(_ => RetreatStep(), _ => CurrentStep > 0);
            PasteFromClipboardCommand = new RelayCommand(_ => PasteFromClipboard());
            StartOverCommand = new RelayCommand(_ => StartOver(), _ => !IsProcessing);
        }

        // -- Mode --
        private bool _isWizardMode = Properties.Settings.Default.UseWizardMode;
        public bool IsWizardMode {
            get => _isWizardMode;
            set { if (SetField(ref _isWizardMode, value, nameof(IsWizardMode))) { Properties.Settings.Default.UseWizardMode = value; Properties.Settings.Default.Save(); } }
        }

        // -- Wizard Navigation --
        public int TotalSteps => WizardSteps.Count;
        private int _currentStep;
        public int CurrentStep {
            get => _currentStep;
            set { SetField(ref _currentStep, value, nameof(CurrentStep)); OnPropertyChanged(nameof(CanGoBack)); OnPropertyChanged(nameof(CanGoNext)); OnPropertyChanged(nameof(IsOnResolvePage)); OnPropertyChanged(nameof(IsOnStepBeforeResolve)); OnPropertyChanged(nameof(ShowNextButton)); OnPropertyChanged(nameof(CurrentStepId)); OnPropertyChanged(nameof(CanStartOver)); }
        }
        public bool CanGoBack => _currentStep > 0;
        public bool CanGoNext => _currentStep < WizardSteps.Count - 1;
        public bool IsOnResolvePage => _currentStep == WizardSteps.Count - 1;
        public bool IsOnStepBeforeResolve => _currentStep == WizardSteps.Count - 2;
        public bool ShowNextButton => _currentStep < WizardSteps.Count - 2 && CurrentStepId != "InputSource";
        public string CurrentStepId => _currentStep < WizardSteps.Count ? WizardSteps[_currentStep].Id : string.Empty;
        public bool CanStartOver => _currentStep > 0;

        // -- Pending XEL data (for field selection sub-step) --
        public string[] PendingXELFileNames { get; set; }
        public List<string> PendingXELActions { get; set; }
        public List<string> PendingXELFields { get; set; }

        // -- Input --
        private string _inputText = string.Empty;
        public string InputText {
            get => _inputText;
            set {
                if (SetField(ref _inputText, value, nameof(InputText)))
                    DetectSqlBuildVersion();
            }
        }

        // -- Detected SQL Build --
        private SQLBuildInfo _detectedBuildInfo;
        public SQLBuildInfo DetectedBuildInfo { get => _detectedBuildInfo; private set { SetField(ref _detectedBuildInfo, value, nameof(DetectedBuildInfo)); OnPropertyChanged(nameof(HasDetectedBuild)); OnPropertyChanged(nameof(DetectedBuildVersion)); OnPropertyChanged(nameof(DetectedBuildDetails)); } }
        public bool HasDetectedBuild => _detectedBuildInfo != null;
        public string DetectedBuildVersion => _detectedBuildInfo?.BuildNumber;
        public string DetectedBuildDetails => _detectedBuildInfo != null ? $"{_detectedBuildInfo.ProductMajorVersion} {_detectedBuildInfo.ProductLevel} - {_detectedBuildInfo.Label} ({_detectedBuildInfo.MachineType})" : null;

        // -- Detected XML frames with PDB GUID info --
        private bool _hasXmlFrameInput;
        public bool HasXmlFrameInput { get => _hasXmlFrameInput; private set => SetField(ref _hasXmlFrameInput, value, nameof(HasXmlFrameInput)); }
        private string _detectedPdbModules;
        public string DetectedPdbModules { get => _detectedPdbModules; private set => SetField(ref _detectedPdbModules, value, nameof(DetectedPdbModules)); }

        private void DetectSqlBuildVersion() {
            DetectedBuildInfo = null;
            HasXmlFrameInput = false;
            DetectedPdbModules = null;
            if (string.IsNullOrWhiteSpace(_inputText)) return;

            // HTML-decode first, as input may contain &lt;frame ... (same as MCP's HasEmbeddedSymbolInfo)
            var decoded = WebUtility.HtmlDecode(_inputText);

            // Check for XML frame format (same sniff test as engine's ModuleInfoHelper.ParseModuleInfoXMLAsync
            // and MCP's HasEmbeddedSymbolInfo)
            if (decoded.Contains("<frame")) {
                HasXmlFrameInput = true;
                // Extract distinct module names using XML parsing, same as engine does
                var uniqueModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var reader = new StringReader(decoded)) {
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains("<frame")) continue;
                        try {
                            using var sreader = new StringReader(line.Substring(line.IndexOf("<frame")));
                            using var xmlReader = XmlReader.Create(sreader, new XmlReaderSettings() { XmlResolver = null });
                            if (xmlReader.Read()) {
                                var moduleName = xmlReader.GetAttribute("module");
                                if (string.IsNullOrEmpty(moduleName)) moduleName = xmlReader.GetAttribute("name");
                                if (!string.IsNullOrEmpty(moduleName))
                                    uniqueModules.Add(Path.GetFileNameWithoutExtension(moduleName));
                            }
                        } catch { /* best-effort module extraction */ }
                    }
                }
                if (uniqueModules.Count > 0)
                    DetectedPdbModules = string.Join(", ", uniqueModules.OrderBy(s => s));
            }

            // Also try version-based detection
            if (!File.Exists(SqlBuildInfoFileName)) return;
            var match = Regex.Match(_inputText, @"\b(\d{2}\.\d+\.\d+\.\d+)\b");
            if (!match.Success) return;
            var versionStr = match.Groups[1].Value;
            try {
                var allBuilds = SQLBuildInfo.GetSqlBuildInfo(SqlBuildInfoFileName);
                var found = allBuilds.Values.FirstOrDefault(b => b.BuildNumber == versionStr && b.SymbolDetails?.Count > 0);
                if (found != null) DetectedBuildInfo = found;
            } catch { /* best effort */ }
        }

        private string _outputText = string.Empty;
        public string OutputText { get => _outputText; set { SetField(ref _outputText, value, nameof(OutputText)); OnPropertyChanged(nameof(HasOutput)); } }

        public bool HasOutput => !string.IsNullOrEmpty(_outputText);

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

        // -- Resolve button highlight (set after banner actions, cleared on resolve) --
        private bool _highlightResolve;
        public bool HighlightResolve { get => _highlightResolve; set => SetField(ref _highlightResolve, value, nameof(HighlightResolve)); }

        // -- Commands --
        public ICommand ResolveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand NextStepCommand { get; }
        public ICommand PreviousStepCommand { get; }
        public ICommand PasteFromClipboardCommand { get; }
        public ICommand StartOverCommand { get; }

        // -- Dynamic step management --
        internal bool HasSubStep(string stepId) => WizardSteps.Any(s => s.Id == stepId);

        internal void InsertSubStepAfter(string parentStepId, WizardStep subStep) {
            if (HasSubStep(subStep.Id)) return; // already present
            int parentIdx = -1;
            for (int i = 0; i < WizardSteps.Count; i++) {
                if (WizardSteps[i].Id == parentStepId) { parentIdx = i; break; }
            }
            if (parentIdx < 0) return;
            // Insert after parent and any existing sub-steps of the same parent
            int insertIdx = parentIdx + 1;
            while (insertIdx < WizardSteps.Count && WizardSteps[insertIdx].IsConditional && WizardSteps[insertIdx].ParentStepId == parentStepId)
                insertIdx++;
            WizardSteps.Insert(insertIdx, subStep);
            // Refresh navigation properties since TotalSteps changed
            OnPropertyChanged(nameof(TotalSteps));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(IsOnResolvePage));
            OnPropertyChanged(nameof(IsOnStepBeforeResolve));
            OnPropertyChanged(nameof(ShowNextButton));
        }

        internal void RemoveSubStep(string stepId) {
            var step = WizardSteps.FirstOrDefault(s => s.Id == stepId);
            if (step == null) return;
            WizardSteps.Remove(step);
            if (_currentStep >= WizardSteps.Count) _currentStep = WizardSteps.Count - 1;
            OnPropertyChanged(nameof(CurrentStep));
            OnPropertyChanged(nameof(CurrentStepId));
            OnPropertyChanged(nameof(TotalSteps));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(IsOnResolvePage));
            OnPropertyChanged(nameof(IsOnStepBeforeResolve));
            OnPropertyChanged(nameof(ShowNextButton));
        }

        /// <summary>Advance to next step with validation hooks.</summary>
        private void AdvanceStep() {
            // InputSource: cards handle navigation, Next is hidden
            if (CurrentStepId == "InputSource") return;

            // When leaving the Input step, check if base address sub-step is needed
            if (CurrentStepId == "Input" && !string.IsNullOrWhiteSpace(InputText)) {
                if (_resolver.IsInputVAOnly(InputText) && string.IsNullOrEmpty(BaseAddressesString)) {
                    if (!HasSubStep("BaseAddress"))
                        InsertSubStepAfter("InputSource", StepBaseAddress);
                } else {
                    RemoveSubStep("BaseAddress");
                }
            }

            // When leaving the FieldSelection step, trigger XEL import and let the handler manage navigation
            if (CurrentStepId == "FieldSelection") {
                SubStepAction?.Invoke("ImportXEL");
                return;
            }

            CurrentStep++;
        }

        /// <summary>Go back one step, cleaning up conditional sub-steps if appropriate.</summary>
        private void RetreatStep() {
            CurrentStep--;
        }

        /// <summary>Reset all inputs and navigate back to the first step.</summary>
        private void StartOver() {
            InputText = string.Empty;
            OutputText = string.Empty;
            BaseAddressesString = string.Empty;
            FramesOnSingleLine = false;
            RelookupSource = false;
            StatusMessage = "Ready";
            ProgressPercent = 0;
            PendingXELFileNames = null;
            PendingXELActions = null;
            PendingXELFields = null;

            // Remove any conditional sub-steps
            RemoveSubStep("BaseAddress");
            RemoveSubStep("FieldSelection");
            RemoveSubStep("Input");

            CurrentStep = 0;
        }

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
            HighlightResolve = false;
            var validationError = ValidateInputs();
            if (validationError != null) {
                await MainWindow.ShowContentDialogAsync("Validation Error", validationError);
                return;
            }

            IsProcessing = true;
            OutputText = string.Empty;
            StatusMessage = "Parsing input...";

            // Remember where we were so we can return on cancel
            int previousStep = CurrentStep;

            // Navigate to the Resolve/output page so the user sees progress and results
            int resolveIdx = WizardSteps.IndexOf(StepResolve);
            if (resolveIdx >= 0 && CurrentStep != resolveIdx) CurrentStep = resolveIdx;

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
                    await MainWindow.ShowContentDialogAsync(
                        "Potential issues with the output",
                        "One or more potential issues exist in the output. This is sometimes due to mismatched symbols; please double-check symbol paths and re-run if needed.");
                }
                StatusMessage = "Resolution complete.";
            } catch (OperationCanceledException) {
                StatusMessage = StackResolver.OperationCanceled;
                CurrentStep = previousStep;
            } catch (AggregateException ae) when (ae.InnerExceptions.Any(ex => ex is OperationCanceledException)) {
                StatusMessage = StackResolver.OperationCanceled;
                CurrentStep = previousStep;
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

        private async void PasteFromClipboard() {
            if (Properties.Settings.Default.promptForClipboardPaste) {
                var proceed = await MainWindow.ShowConfirmDialogAsync(
                    "Proceed with paste from clipboard",
                    "Proceeding will paste the contents of your clipboard and attempt to resolve them. Are you sure?",
                    "Yes", "No");
                var remember = await MainWindow.ShowConfirmDialogAsync(
                    "Save your choice?",
                    "Should we remember your choice for the future?",
                    "Yes", "No");

                Properties.Settings.Default.promptForClipboardPaste = !remember;
                Properties.Settings.Default.choiceForClipboardPaste = proceed;
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
                    await MainWindow.ShowContentDialogAsync(
                        "New release available",
                        $"You are currently on release: {latestReleaseDateStringLocal} of SQLCallStackResolver. There is a newer release ({latestReleaseDateTimeServer.ToString(LatestReleaseTimestampFormat)}) available.\nYou should exit, then download the latest release from https://aka.ms/SQLStack/releases.");
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
                    var res = await MainWindow.ShowConfirmDialogAsync(
                        "SQL build info updated",
                        "The SQLBuildInfo.json file was updated recently on GitHub. Do you wish to update your copy with the newer version?",
                        "Yes", "No");
                    if (res) {
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
                            await MainWindow.ShowContentDialogAsync("Error", "Could not download the SQL build Info file.");
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

        internal async Task DownloadSymbolsForDetectedBuildAsync() {
            if (_detectedBuildInfo == null || _detectedBuildInfo.SymbolDetails?.Count == 0) return;
            var bld = _detectedBuildInfo;
            var pdbFolder = ConfigurationManager.AppSettings["PDBDownloadFolder"] ?? @"c:\temp";
            var destFolder = $@"{pdbFolder}\{bld.BuildNumber}.{bld.MachineType}";
            Directory.CreateDirectory(destFolder);

            IsProcessing = true;
            var errors = new StringBuilder();
            var urls = bld.SymbolDetails.Select(s => s.DownloadURL).Where(u => !string.IsNullOrEmpty(u)).ToList();
            int completed = 0;

            using (_cts = new CancellationTokenSource()) {
                foreach (var url in urls) {
                    if (_cts.IsCancellationRequested) break;
                    var filename = Path.GetFileName(new Uri(url).LocalPath);
                    var localPath = Path.Combine(destFolder, filename);
                    if (File.Exists(localPath)) { completed++; continue; }

                    StatusMessage = $"Downloading {filename} ({completed + 1}/{urls.Count})...";
                    var prog = new DownloadProgress();
                    var dlTask = Task.Run(async () => {
                        var ok = await Utils.DownloadFromUrl(url, localPath, prog, _cts);
                        if (!ok) errors.AppendLine($"Failed: {filename}");
                    });
                    while (!dlTask.IsCompleted) {
                        await Task.Delay(StackResolver.OperationWaitIntervalMilliseconds);
                        ProgressPercent = prog.Percent;
                    }
                    completed++;
                }
            }

            IsProcessing = false;
            ProgressPercent = 0;

            if (errors.Length > 0) {
                StatusMessage = "Some symbol downloads failed.";
                await MainWindow.ShowContentDialogAsync("Download Errors", errors.ToString());
            } else if (_cts?.IsCancellationRequested == true) {
                StatusMessage = StackResolver.OperationCanceled;
            } else {
                AppendPdbPath(destFolder);
                StatusMessage = $"Symbols for {bld.BuildNumber} downloaded. Ready to resolve!";
                HighlightResolve = true;
            }
        }
    }
}
