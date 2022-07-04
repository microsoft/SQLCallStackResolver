﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    public partial class MainForm : Form {
        public MainForm() {
            MessageBox.Show("Copyright (c) 2022 Microsoft Corporation. All rights reserved.\r\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.", "SQLCallStackResolver - Legal Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            InitializeComponent();
        }

        private readonly StackResolver _resolver = new();
        private Task<string> backgroundTask;
        private CancellationTokenSource BackgroundCTS { get; set; }
        private CancellationToken backgroundCT;

        private string _baseAddressesString = null;
        internal static readonly string SqlBuildInfoFileName = @"sqlbuildinfo.json";
        internal static readonly string LastUpdatedTimestampFileName = @"lastupdated.txt";
        internal static readonly string LastUpdatedTimestampFormat = "yyyy-MM-dd HH:mm";
        internal static readonly string LastUpdatedTimestampCulture = "en-US";

        internal static readonly string LatestReleaseTimestampFileName = @"latestrelease.txt";
        internal static readonly string LatestReleaseTimestampFormat = "yyyy-MM-dd HH:mm";
        internal static readonly string LatestReleaseTimestampCulture = "en-US";

        private void ResolveCallstacks_Click(object sender, EventArgs e) {
            List<string> dllPaths = null;
            if (!string.IsNullOrEmpty(binaryPaths.Text)) dllPaths = binaryPaths.Text.Split(';').ToList();
            var res = this._resolver.ProcessBaseAddresses(this._baseAddressesString);
            if (!res) {
                MessageBox.Show(this, "Cannot interpret the module base address information. Make sure you just have the output of the following query (no column headers, no other columns) copied from SSMS using the Grid Results\r\n\r\nselect name, base_address from sys.dm_os_loaded_modules where name not like '%.rll'",
                            "Unable to load base address information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
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
                return;
            }

            this.backgroundTask = Task.Run(() => {
                return this._resolver.ResolveCallstacks(callStackInput.Text,
                    pdbPaths.Text,
                    pdbRecurse.Checked,
                    dllPaths,
                    DLLrecurse.Checked,
                    FramesOnSingleLine.Checked,
                    IncludeLineNumbers.Checked,
                    RelookupSource.Checked,
                    includeOffsets.Checked,
                    showInlineFrames.Checked,
                    cachePDB.Checked,
                    outputFilePath.Text
                    );
            });

            this.MonitorBackgroundTask(backgroundTask);

            finalOutput.Text = backgroundTask.Result;
            if (backgroundTask.Result.Contains("WARNING:")) {
                MessageBox.Show(this,
                    "One or more potential issues exist in the output. This is sometimes due to mismatched symbols, so please double-check symbol paths and re-run if needed.",
                    "Potential issues with the output", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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

        private void ShowStatus(string txt) {
            this.statusLabel.Text = txt;
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
                this.ShowStatus("Loading from XEL files; please wait. This may take a while!");
                this.backgroundTask = Task.Run(async () => {
                    return (await this._resolver.ExtractFromXEL(genericOpenFileDlg.FileNames, GroupXEvents.Checked, relevantXEFields)).Item2;
                });
                this.MonitorBackgroundTask(backgroundTask);
                callStackInput.Text = backgroundTask.Result;
                this.ShowStatus("Finished importing callstacks from XEL file(s)!");
            }
        }

        private void MonitorBackgroundTask(Task theTask) {
            using (BackgroundCTS = new CancellationTokenSource()) {
                backgroundCT = BackgroundCTS.Token;
                this.EnableCancelButton();
                while (!theTask.Wait(30)) {
                    if (backgroundCT.IsCancellationRequested) {
                        this._resolver.CancelRunningTasks();
                    }

                    this.ShowStatus(_resolver.StatusMessage);
                    this.progressBar.Value = _resolver.PercentComplete;
                    this.statusStrip1.Refresh();
                    Application.DoEvents();
                }

                // refresh it one last time to ensure that the last status message is displayed
                this.ShowStatus(_resolver.StatusMessage);
                this.progressBar.Value = _resolver.PercentComplete;
                this.statusStrip1.Refresh();
                Application.DoEvents();
                this.DisableCancelButton();
            }
        }

        private async void CallStackInput_DragDrop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false)) {
                e.Effect = DragDropEffects.All;

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length != 0) {
                    var allFilesContent = new StringBuilder();
                    if (files.Where(f => Path.GetExtension(f).ToLower(CultureInfo.CurrentCulture) == ".xel").Count() == files.Count()) {
                        // all files being dragged are XEL files, work on them.
                        this.ShowStatus("XEL file was dragged; please wait while we extract events from the file");
                        List<string> relevantXEFields = await GetUserSelectedXEFieldsAsync(files);
                        allFilesContent.AppendLine((await this._resolver.ExtractFromXEL(files, GroupXEvents.Checked, relevantXEFields)).Item2);
                        this.ShowStatus(string.Empty);
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
            using var sqlbuildsForm = new SQLBuildsForm {
                pathToPDBs = ConfigurationManager.AppSettings["PDBDownloadFolder"]
            };
            sqlbuildsForm.StartPosition = FormStartPosition.CenterParent;
            sqlbuildsForm.ShowDialog(this);
            this.pdbPaths.AppendText((pdbPaths.TextLength == 0 ? string.Empty : ";") + sqlbuildsForm.lastDownloadedSymFolder);
        }

        private void MainForm_Load(object sender, EventArgs e) {
            DateTime latestReleaseDateTimeServer;
            DateTime latestReleaseDateTimeLocal = DateTime.MinValue;
            var latestReleaseURLs = ConfigurationManager.AppSettings["LatestReleaseURLs"].Split(';');
            // get the timestamp contained within the first valid file within latestReleaseURLs
            foreach (var url in latestReleaseURLs) {
                string latestReleaseDateStringServer = Utils.GetFileContentsFromUrl(url);
                if (!string.IsNullOrWhiteSpace(latestReleaseDateStringServer)) {
                    latestReleaseDateTimeServer = DateTime.ParseExact(latestReleaseDateStringServer,
                        LatestReleaseTimestampFormat, new CultureInfo(LatestReleaseTimestampCulture));
                } else continue;

                string latestReleaseDateStringLocal = "Unknown";
                // get content of local latestrelease.txt (if it exists)
                if (File.Exists(LatestReleaseTimestampFileName)) {
                    using var strm = new StreamReader(LatestReleaseTimestampFileName);
                    latestReleaseDateStringLocal = strm.ReadToEnd().Trim();
                    this.Text += $" (release: {latestReleaseDateStringLocal})"; // update form title bar
                    latestReleaseDateTimeLocal = DateTime.ParseExact(latestReleaseDateStringLocal,
                        LatestReleaseTimestampFormat, new CultureInfo(LatestReleaseTimestampCulture));
                } else latestReleaseDateTimeLocal = DateTime.MinValue;

                if (latestReleaseDateTimeServer > latestReleaseDateTimeLocal) {
                    // if the server timestamp > local timestamp, prompt to download
                    MessageBox.Show(this,
                        $"You are currently on release: {latestReleaseDateStringLocal} of SQLCallStackResolver. There is a newer release ({latestReleaseDateStringServer}) available." + Environment.NewLine + "You should exit, then download the latest release from https://aka.ms/SQLStack/releases. Then, extract the files from the release ZIP, overwriting and updating your older copy.",
                        "New release available.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            DateTime lastUpdDateTimeServer = DateTime.MinValue;
            DateTime lastUpdDateTimeLocal = DateTime.MinValue;
            var sqlBuildInfoUpdateURLs = ConfigurationManager.AppSettings["SQLBuildInfoUpdateURLs"].Split(';');
            var sqlBuildInfoURLs = ConfigurationManager.AppSettings["SQLBuildInfoURLs"].Split(';');

            // get the timestamp contained within the first valid file within SQLBuildInfoURLs
            foreach (var lastUpd in sqlBuildInfoUpdateURLs.Select(b => Utils.GetFileContentsFromUrl(b))) {
                if (!string.IsNullOrWhiteSpace(lastUpd)) {
                    lastUpdDateTimeServer = DateTime.ParseExact(lastUpd,
                    LastUpdatedTimestampFormat, new CultureInfo(LastUpdatedTimestampCulture));
                } else continue;

                // get content of local lastupdated.txt (if it exists)
                if (File.Exists(LastUpdatedTimestampFileName)) {
                    using var strm = new StreamReader(LastUpdatedTimestampFileName);
                    var lastUpdLocal = strm.ReadToEnd().Trim();
                    lastUpdDateTimeLocal = DateTime.ParseExact(lastUpdLocal,
                        LastUpdatedTimestampFormat, new CultureInfo(LastUpdatedTimestampCulture));
                } else lastUpdDateTimeLocal = DateTime.MinValue;

                if (lastUpdDateTimeServer > lastUpdDateTimeLocal) {
                    // if the server timestamp > local timestamp, prompt to download
                    var res = MessageBox.Show(this,
                        "The SQLBuildInfo.json file was updated recently on GitHub. Do you wish to update your copy with the newer version?",
                        "SQL Build info updated", MessageBoxButtons.YesNo);

                    if (DialogResult.Yes == res) {
                        string jsonContent = null;
                        foreach (var jsonURL in sqlBuildInfoURLs) {
                            jsonContent = Utils.GetFileContentsFromUrl(jsonURL);
                            if (!string.IsNullOrWhiteSpace(jsonContent)) { // update local copy of build info file
                                using var writer = new StreamWriter(SqlBuildInfoFileName);
                                writer.Write(jsonContent);
                                writer.Flush();
                                writer.Close();
                                using var wr = new StreamWriter(LastUpdatedTimestampFileName, false);  // update local last updated timestamp
                                wr.Write(lastUpdDateTimeServer.ToString(
                                    LastUpdatedTimestampFormat,
                                    new CultureInfo(LastUpdatedTimestampCulture)));
                                break;
                            }
                        }
                        if (string.IsNullOrEmpty(jsonContent)) MessageBox.Show(this, "Could not download the SQL Build Info file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                break;
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
