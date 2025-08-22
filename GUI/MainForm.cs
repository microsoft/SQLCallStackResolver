// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    public partial class MainForm : Form {
        public MainForm() {
            InitializeComponent();
        }

        private readonly StackResolver _resolver = new();
        private CancellationTokenSource BackgroundCTS { get; set; }

        private string _baseAddressesString = null;
        internal static readonly string SqlBuildInfoFileName = @"sqlbuildinfo.json";
        internal static readonly string LastUpdatedTimestampFileName = @"lastupdated.txt";
        internal static readonly string LastUpdatedTimestampFormat = "yyyy-MM-dd HH:mm";
        internal static readonly string LastUpdatedTimestampCulture = "en-US";

        internal static readonly string LatestReleaseTimestampFileName = @"latestrelease.txt";
        internal static readonly string LatestReleaseTimestampFormat = "yyyy-MM-dd HH:mm";
        internal static readonly string LatestReleaseTimestampCulture = "en-US";

        private void ResolveCallstacks_Click(object sender, EventArgs e) {
            if (!ValidateInputs()) return;
            ProcessCallstacks();
        }

        private void ResolveCallStackFromClipboardButton_Click(object sender, EventArgs e) {
            if (Properties.Settings.Default.promptForClipboardPaste) {
                var resProceed = MessageBox.Show(this, "Proceeding will paste the contents of your clipboard and attempt to resolve them. Are you sure?", "Proceed with paste from clipboard", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                var resRememberChoice = MessageBox.Show(this, "Should we remember your choice for the future?", "Save your choice?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                Properties.Settings.Default.promptForClipboardPaste = (resRememberChoice == DialogResult.No);
                Properties.Settings.Default.choiceForClipboardPaste = (resProceed == DialogResult.Yes);
                Properties.Settings.Default.Save();
            }

            if (!Properties.Settings.Default.choiceForClipboardPaste) {
                this.UpdateStatus((Properties.Settings.Default.promptForClipboardPaste ? "You chose to not" : "You have chosen never to") + " paste clipboard contents. Nothing to do!");
                return;
            }

            callStackInput.Clear();
            finalOutput.Clear();
            callStackInput.Text = Clipboard.GetText();
            if (!ValidateInputs()) return;
            ProcessCallstacks();
        }

        private void ProcessCallstacks() {
            List<StackDetails> allStacks = null;
            using (BackgroundCTS = new CancellationTokenSource()) {
                var allStacksTask = this._resolver.GetListofCallStacksAsync(callStackInput.Text, FramesOnSingleLine.Checked, RelookupSource.Checked, BackgroundCTS);
                this.MonitorBackgroundTask(allStacksTask);
                allStacks = allStacksTask.Result;
            }
            if (allStacks.Any()) using (BackgroundCTS = new CancellationTokenSource()) {
                    var resolverTask = this._resolver.ResolveCallstacksAsync(allStacks, pdbPaths.Text, pdbRecurse.Checked, string.IsNullOrEmpty(binaryPaths.Text) ? null : binaryPaths.Text.Split(';').ToList(),
                        DLLrecurse.Checked, IncludeLineNumbers.Checked, RelookupSource.Checked,
                        includeOffsets.Checked, showInlineFrames.Checked, cachePDB.Checked, outputFilePath.Text, BackgroundCTS);
                    this.MonitorBackgroundTask(resolverTask);
                    finalOutput.Text = resolverTask.Result;
                }

            if (finalOutput.Text.Contains(StackResolver.WARNING_PREFIX)) {
                MessageBox.Show(this,
                    "One or more potential issues exist in the output. This is sometimes due to mismatched symbols, so please double-check symbol paths and re-run if needed.",
                    "Potential issues with the output", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool ValidateInputs() {
            if (string.IsNullOrWhiteSpace(pdbPaths.Text)) {
                MessageBox.Show(this,
                    "Please specify path(s) to PDB files!",
                    "No symbol path(s) specified", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (this._resolver.IsInputVAOnly(callStackInput.Text) && string.IsNullOrEmpty(this._baseAddressesString) && DialogResult.No == MessageBox.Show(this,
                    "The input provided seems to be comprised entirely of virtual addresses only, but the required corresponding module information has not been provided. Do you still want to attempt to process this input?",
                    "Missing module base information", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) return false;

            if (!this._resolver.IsInputVAOnly(callStackInput.Text) && !string.IsNullOrEmpty(this._baseAddressesString)) {
                var dlgRes = MessageBox.Show(this,
                    "Module base address information has been provided, but the input format probably does not need that information. Do you still want to attempt to process this input?",
                    "Module base address information not needed", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (DialogResult.No == dlgRes) {
                    dlgRes = MessageBox.Show(this,
                    "Would you like to clear out the module base address info to avoid any confusion?",
                    "Clear module base address information", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (DialogResult.Yes == dlgRes) this._baseAddressesString = string.Empty;
                    else return false;
                }
                ;
            }

            var res = this._resolver.ProcessBaseAddresses(this._baseAddressesString);
            if (!res) {
                MessageBox.Show(this, "Cannot interpret the module base address information. Make sure you just have the output of the following query (no column headers, no other columns) copied from SSMS using the Grid Results\r\n\r\nselect name, base_address from sys.dm_os_loaded_modules where name not like '%.rll'",
                            "Unable to load base address information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            bool isSingleLineInput = this._resolver.IsInputSingleLine(callStackInput.Text, ConfigurationManager.AppSettings["PatternsToTreatAsMultiline"]);
            if (isSingleLineInput && !FramesOnSingleLine.Checked && DialogResult.Yes == MessageBox.Show(this,
                    "Maybe this is intentional, but your input seems to have all the frames on a single line, but the 'Callstack frames are in single line' checkbox is unchecked. " +
                    "This may cause problems resolving symbols. Would you like to enable this?", "Enable the 'frames on single line' option?", MessageBoxButtons.YesNo)) {
                FramesOnSingleLine.Checked = true;
                FramesOnSingleLine.Refresh();
                this.Refresh();
                Application.DoEvents();
            }

            if (!isSingleLineInput && FramesOnSingleLine.Checked && DialogResult.Yes == MessageBox.Show(this,
                    "Your input seems to have multiple lines, but the 'Callstack frames are in single line' checkbox is checked. " +
                    "This may cause problems resolving symbols. Would you like to uncheck this setting?", "Disable the 'frames on single line' option?", MessageBoxButtons.YesNo)) {
                FramesOnSingleLine.Checked = false;
                FramesOnSingleLine.Refresh();
                this.Refresh();
                Application.DoEvents();
            }

            if (!pdbPaths.Text.Contains(@"\\") && cachePDB.Checked && DialogResult.Yes == MessageBox.Show(this,
                    "Cache PDBs is only recommended when getting symbols from UNC paths. Would you like to disable this?",
                    "Disable symbol file cache?", MessageBoxButtons.YesNo)) {
                cachePDB.Checked = false;
                cachePDB.Refresh();
                this.Refresh();
                Application.DoEvents();
            }

            if (pdbPaths.Text.Contains(@"\\") && !cachePDB.Checked && DialogResult.Yes == MessageBox.Show(this,
                    "When getting symbols from UNC paths, SQLCallStackResolver can temporary cache a copy when resolving symbols. " +
                    "This may speed things up especially when the UNC path is over a WAN and when you have a number of callstacks to resolve. " +
                    "Would you like to enable this?",
                    "Enable symbol file cache?",
                    MessageBoxButtons.YesNo)) {
                cachePDB.Checked = true;
                cachePDB.Refresh();
                this.Refresh();
                Application.DoEvents();
            }

            if (string.IsNullOrEmpty(outputFilePath.Text) && callStackInput.Text.Length > 0.1 * int.MaxValue && DialogResult.Yes == MessageBox.Show(this,
                    "The input seems quite large; output might be truncated unless the option to output to a file is selected and a suitable file path specified. " +
                    "It is recommended that you first do that. Do you want to exit (select Yes in that case) or continue at your own risk (select No in that case)?",
                    "Input is large, risk of truncation or errors",
                    MessageBoxButtons.YesNo)) {
                return false;
            }
            return true;
        }

        private void DisableCancelButton() {
            this.cancelButton.Enabled = false;
            this.cancelButton.Visible = false;
        }

        private void EnableCancelButton() {
            this.cancelButton.Enabled = true;
            this.cancelButton.Visible = true;
        }

        private void EnterBaseAddresses_Click(object sender, EventArgs e) {
            using var baseAddressForm = new MultilineInput(this._baseAddressesString, true);
            baseAddressForm.StartPosition = FormStartPosition.CenterParent;
            if (DialogResult.OK == baseAddressForm.ShowDialog(this)) this._baseAddressesString = baseAddressForm.Baseaddressesstring;
        }
        private void CallStackInput_KeyDown(object sender, KeyEventArgs e) {
            if (e.Control && e.KeyCode == Keys.A) {
                callStackInput.SelectAll();
            }
        }

        private void FinalOutput_KeyDown(object sender, KeyEventArgs e) {
            if (e.Control && e.KeyCode == Keys.A) {
                finalOutput.SelectAll();
            }
        }

        private void UpdateOperationProgress() {
            this.statusLabel.Text = _resolver.StatusMessage;
            this.progressBar.Value = _resolver.PercentComplete;
            this.statusStrip.Refresh();
            Application.DoEvents();
        }

        private void UpdateStatus(string message) {
            this.statusLabel.Text = message;
            Application.DoEvents();
        }

        private async void LoadXELButton_Click(object sender, EventArgs e) {
            genericOpenFileDlg.Multiselect = true;
            genericOpenFileDlg.CheckPathExists = true;
            genericOpenFileDlg.CheckFileExists = true;
            genericOpenFileDlg.FileName = String.Empty;
            genericOpenFileDlg.Filter = "XEL files (*.xel)|*.xel|All files (*.*)|*.*";
            genericOpenFileDlg.Title = "Select XEL file";

            if (DialogResult.Cancel != genericOpenFileDlg.ShowDialog(this)) {
                List<string> relevantXEFields = await GetUserSelectedXEFieldsAsync(genericOpenFileDlg.FileNames);
                if (!relevantXEFields.Any()) {
                    this.UpdateStatus("No fields were selected for import from the XEL files. Nothing to do!");
                    return;
                }
                this.UpdateStatus("Loading from XEL files; please wait. This may take a while!");
                using (this.BackgroundCTS = new CancellationTokenSource()) {
                    var xelTask = this._resolver.ExtractFromXELAsync(genericOpenFileDlg.FileNames, GroupXEvents.Checked, relevantXEFields, this.BackgroundCTS);
                    this.MonitorBackgroundTask(xelTask);
                    callStackInput.Text = xelTask.Result.Item2;
                    if (BackgroundCTS.IsCancellationRequested) return;
                }
                this.UpdateStatus("Finished importing callstacks from XEL file(s)!");
            }
        }

        private void MonitorBackgroundTask(Task theTask) {
            this.EnableCancelButton();
            while (!theTask.Wait(50)) this.UpdateOperationProgress();
            this.UpdateOperationProgress();  // status update after task exits
            this.DisableCancelButton();
        }

        private async void CallStackInput_DragDrop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false)) {
                e.Effect = DragDropEffects.All;

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length != 0) {
                    var allFilesContent = new StringBuilder();
                    if (files.Where(f => Path.GetExtension(f).ToLower(CultureInfo.CurrentCulture) == ".xel").Count() == files.Count()) {
                        // all files being dragged are XEL files, work on them.
                        this.UpdateStatus("XEL file was dragged; please wait while we extract events from the file");
                        List<string> relevantXEFields = await GetUserSelectedXEFieldsAsync(files);
                        if (!relevantXEFields.Any()) {
                            this.UpdateStatus("No fields were selected for import from the XEL files. Nothing to do!");
                            return;
                        }
                        using (BackgroundCTS = new CancellationTokenSource()) {
                            var xelTask = this._resolver.ExtractFromXELAsync(files, GroupXEvents.Checked, relevantXEFields, BackgroundCTS);
                            this.MonitorBackgroundTask(xelTask);
                            allFilesContent.AppendLine(xelTask.Result.Item2);
                        }
                        this.UpdateStatus(string.Empty);
                    } else foreach (var currFile in files) { // handle the files as text input
                            allFilesContent.AppendLine(File.ReadAllText(currFile));
                        }

                    callStackInput.Text = allFilesContent.ToString();
                }
            }
        }

        private async Task<List<string>> GetUserSelectedXEFieldsAsync(string[] fileNames) {
            using var fieldsListDialog = new FieldSelection();
            fieldsListDialog.Text = "Select relevant XEvent fields";
            var xeEventItems = await this._resolver.GetDistinctXELFieldsAsync(fileNames, 1000);
            if (xeEventItems.Item1.Count + xeEventItems.Item2.Count == 0) return new List<string>();
            fieldsListDialog.AllActions = xeEventItems.Item1;
            fieldsListDialog.AllFields = xeEventItems.Item2;
            fieldsListDialog.StartPosition = FormStartPosition.CenterParent;
            fieldsListDialog.ShowDialog(this);
            return fieldsListDialog.SelectedEventItems;
        }
        private void CallStackInput_DragOver(object sender, DragEventArgs e) {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void PDBPathPicker_Click(object sender, EventArgs e) {
            genericOpenFileDlg.Multiselect = false;
            genericOpenFileDlg.CheckPathExists = false;
            genericOpenFileDlg.CheckFileExists = false;
            genericOpenFileDlg.FileName = "select folder only";
            genericOpenFileDlg.Filter = "All files (*.*)|*.*";
            genericOpenFileDlg.Title = "Select FOLDER path to your PDBs";
            if (DialogResult.Cancel != genericOpenFileDlg.ShowDialog(this)) pdbPaths.AppendText((pdbPaths.TextLength == 0 ? string.Empty : ";") + Path.GetDirectoryName(genericOpenFileDlg.FileName));
        }

        private void BinaryPathPicker_Click(object sender, EventArgs e) {
            genericOpenFileDlg.Multiselect = false;
            genericOpenFileDlg.CheckPathExists = false;
            genericOpenFileDlg.CheckFileExists = false;
            genericOpenFileDlg.FileName = "select folder only";
            genericOpenFileDlg.Filter = "All files (*.*)|*.*";
            genericOpenFileDlg.Title = "Select FOLDER path to the SQL binaries";
            if (DialogResult.Cancel != genericOpenFileDlg.ShowDialog(this)) binaryPaths.AppendText((binaryPaths.TextLength == 0 ? string.Empty : ";") + Path.GetDirectoryName(genericOpenFileDlg.FileName));
        }

        private void SelectSQLPDB_Click(object sender, EventArgs e) {
            if (!File.Exists(MainForm.SqlBuildInfoFileName)) {
                MessageBox.Show(this,
                    $"Could not find the SQL build info JSON file: {MainForm.SqlBuildInfoFileName}. You might need to manually obtain it from one of these locations: {ConfigurationManager.AppSettings["SQLBuildInfoURLs"]}",
                    "SQL build info missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using var sqlbuildsForm = new SQLBuildsForm {pathToPDBs = ConfigurationManager.AppSettings["PDBDownloadFolder"]};
            sqlbuildsForm.StartPosition = FormStartPosition.CenterParent;
            sqlbuildsForm.ShowDialog(this);
            this.pdbPaths.AppendText((pdbPaths.TextLength == 0 ? string.Empty : ";") + sqlbuildsForm.lastDownloadedSymFolder);
        }

        private async void MainForm_Load(object sender, EventArgs e) {
            MessageBox.Show(this, "Copyright (c) 2025 Microsoft Corporation. All rights reserved.\r\n\r\nTHIS SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.\r\n\r\nUSAGE OF THE MICROSOFT SYMBOL SERVER IS COVERED BY THE LICENSE TERMS PUBLISHED AT https://docs.microsoft.com/legal/windows-sdk/microsoft-symbol-server-license-terms.", "SQLCallStackResolver - Legal Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DateTime latestReleaseDateTimeServer = DateTime.MinValue;
            DateTime latestReleaseDateTimeLocal = DateTime.MinValue;
            var latestReleaseURLs = ConfigurationManager.AppSettings["LatestReleaseURLs"].Split(';');

            // get latest release timestamp from the web
            var t = latestReleaseURLs.Select(url => Utils.GetTextFromUrl(url)).ToArray();
            var taskRes = (await Task.WhenAll(t)).Where(s => !string.IsNullOrWhiteSpace(s));
            if (taskRes.Any()) latestReleaseDateTimeServer = DateTime.ParseExact(taskRes.First().Trim(), LatestReleaseTimestampFormat, new CultureInfo(LatestReleaseTimestampCulture));

            // get content of local latestrelease.txt (if it exists)
            string latestReleaseDateStringLocal = "Unknown";
            if (File.Exists(LatestReleaseTimestampFileName)) {
                using var sr = new StreamReader(LatestReleaseTimestampFileName);
                latestReleaseDateStringLocal = (await sr.ReadToEndAsync()).Trim();
                this.Text += $" (release: {latestReleaseDateStringLocal})"; // update form title bar
                latestReleaseDateTimeLocal = DateTime.ParseExact(latestReleaseDateStringLocal,
                    LatestReleaseTimestampFormat, new CultureInfo(LatestReleaseTimestampCulture));
            } else latestReleaseDateTimeLocal = DateTime.MinValue;

            if (latestReleaseDateTimeServer > latestReleaseDateTimeLocal) // the server timestamp > local timestamp, prompt to download
                MessageBox.Show(this,
                    $"You are currently on release: {latestReleaseDateStringLocal} of SQLCallStackResolver. There is a newer release ({latestReleaseDateTimeServer.ToString(LatestReleaseTimestampFormat)}) available." + Environment.NewLine + "You should exit, then download the latest release from https://aka.ms/SQLStack/releases. Then, extract the files from the release ZIP, overwriting and updating your older copy.",
                    "New release available.", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DateTime lastUpdDateTimeServer = DateTime.MinValue;
            DateTime lastUpdDateTimeLocal = DateTime.MinValue;
            var sqlBuildInfoUpdateURLs = ConfigurationManager.AppSettings["SQLBuildInfoUpdateURLs"].Split(';');
            var sqlBuildInfoURLs = ConfigurationManager.AppSettings["SQLBuildInfoURLs"].Split(';');

            // get the timestamp contained within the first valid file within SQLBuildInfoURLs
            t = sqlBuildInfoUpdateURLs.Select(url => Utils.GetTextFromUrl(url)).ToArray();
            taskRes = (await Task.WhenAll(t)).Where(s => !string.IsNullOrWhiteSpace(s));
            if (taskRes.Any()) lastUpdDateTimeServer = DateTime.ParseExact(taskRes.First().Trim(), LastUpdatedTimestampFormat, new CultureInfo(LastUpdatedTimestampCulture));

            // get content of local lastupdated.txt (if it exists)
            if (File.Exists(LastUpdatedTimestampFileName)) {
                using var sr = new StreamReader(LastUpdatedTimestampFileName);
                var lastUpdLocal = (await sr.ReadToEndAsync()).Trim();
                lastUpdDateTimeLocal = DateTime.ParseExact(lastUpdLocal,
                    LastUpdatedTimestampFormat, new CultureInfo(LastUpdatedTimestampCulture));
            } else lastUpdDateTimeLocal = DateTime.MinValue;

            if (lastUpdDateTimeServer > lastUpdDateTimeLocal) { // if the server timestamp > local timestamp, prompt to download
                var res = MessageBox.Show(this,
                    "The SQLBuildInfo.json file was updated recently on GitHub. Do you wish to update your copy with the newer version?",
                    "SQL build info updated", MessageBoxButtons.YesNo);

                if (DialogResult.Yes == res) {
                    t = sqlBuildInfoURLs.Select(jsonURL => Utils.GetTextFromUrl(jsonURL)).ToArray();
                    this.UpdateStatus("Trying to update SQL build info from GitHub...");
                    taskRes = (await Task.WhenAll(t)).Where(s => !string.IsNullOrWhiteSpace(s));
                    if (taskRes.Any()) { // update local copy of build info file
                        using var writer = new StreamWriter(SqlBuildInfoFileName);
                        writer.Write(taskRes.First());
                        writer.Flush();
                        writer.Close();
                        using var wr = new StreamWriter(LastUpdatedTimestampFileName, false);  // update local last updated timestamp
                        wr.Write(lastUpdDateTimeServer.ToString(LastUpdatedTimestampFormat, new CultureInfo(LastUpdatedTimestampCulture)));
                        this.UpdateStatus("Successfully updated SQL build info!");
                        wr.Flush();
                        wr.Close();
                    } else MessageBox.Show(this, "Could not download the SQL build Info file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OutputFilePathPicker_Click(object sender, EventArgs e) {
            genericSaveFileDlg.FileName = "resolvedstacks.txt";
            genericSaveFileDlg.Filter = "Text files (*.txt)|*.txt";
            genericSaveFileDlg.Title = "Save output as";
            if (DialogResult.Cancel != genericSaveFileDlg.ShowDialog(this)) outputFilePath.Text = genericSaveFileDlg.FileName;
        }

        private void CancelButton_Click(object sender, EventArgs e) {
            this.BackgroundCTS.Cancel();
        }
    }
}
