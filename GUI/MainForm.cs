// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;
    using System.Text;
    using System.IO;
    using System.Net;
    using System.Globalization;
    using System.Configuration;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using System.Threading;

    public partial class MainForm : Form {
        public MainForm() {
            InitializeComponent();
        }

        private StackResolver _resolver = new StackResolver();
        private Task<string> backgroundTask;
        private CancellationTokenSource backgroundCTS { get; set; }
        private CancellationToken backgroundCT;

        private string _baseAddressesString = null;
        internal static string SqlBuildInfoFileName = @"sqlbuildinfo.json";
        internal static string LastUpdatedTimestampFileName = @"lastupdated.txt";
        internal static string LastUpdatedTimestampFormat = "yyyy-MM-dd HH:mm";
        internal static string LastUpdatedTimestampCulture = "en-US";

        private void ResolveCallstacks_Click(object sender, EventArgs e) {
            List<string> dllPaths = null;
            if (!string.IsNullOrEmpty(binaryPaths.Text)) {
                dllPaths = binaryPaths.Text.Split(';').ToList();
            }

            var res = this._resolver.ProcessBaseAddresses(this._baseAddressesString);
            if (!res) {
                MessageBox.Show(
                            this,
                            "Cannot interpret the module base address information. Make sure you just have the output of the following query (no column headers, no other columns) copied from SSMS using the Grid Results\r\n\r\nselect name, base_address from sys.dm_os_loaded_modules where name not like '%.rll'",
                            "Unable to load base address information",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                return;
            }

            int numLinesOfInput = callStackInput.Text.Length - callStackInput.Text.Replace(Environment.NewLine, string.Empty).Length;

            if (1 == numLinesOfInput && !FramesOnSingleLine.Checked) {
                if (DialogResult.Yes == MessageBox.Show(this,
                    "Maybe this is intentional, but your input seems to have all the frames on a single line, but the 'Callstack frames are in single line' checkbox is unchecked. " +
                    "This may cause problems resolving symbols. Would you like to enable this?",
                    "Enable the 'frames on single line' option?",
                    MessageBoxButtons.YesNo)) {
                    FramesOnSingleLine.Checked = true;
                    FramesOnSingleLine.Refresh();
                    this.Refresh();
                    Application.DoEvents();
                }
            }

            if (numLinesOfInput > 1 && FramesOnSingleLine.Checked) {
                if (DialogResult.Yes == MessageBox.Show(this,
                    "Your input seems to have multiple lines, but the 'Callstack frames are in single line' checkbox is checked. " +
                    "This may cause problems resolving symbols. Would you like to uncheck this setting?",
                    "Disable the 'frames on single line' option?",
                    MessageBoxButtons.YesNo)) {
                    FramesOnSingleLine.Checked = false;
                    FramesOnSingleLine.Refresh();
                    this.Refresh();
                    Application.DoEvents();
                }
            }

            if (!pdbPaths.Text.Contains(@"\\") && cachePDB.Checked) {
                if (DialogResult.Yes == MessageBox.Show(this,
                    "Cache PDBs is only recommended when getting symbols from UNC paths. " +
                    "Would you like to disable this?",
                    "Disable symbol file cache?",
                    MessageBoxButtons.YesNo)) {
                    cachePDB.Checked = false;
                    cachePDB.Refresh();
                    this.Refresh();
                    Application.DoEvents();
                }
            }

            if (pdbPaths.Text.Contains(@"\\") && !cachePDB.Checked) {
                if (DialogResult.Yes == MessageBox.Show(this,
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
            }

            if (string.IsNullOrEmpty(outputFilePath.Text) && callStackInput.Text.Length > 0.1 * int.MaxValue) {
                if (DialogResult.Yes == MessageBox.Show(this,
                    "The input seems quite large; output might be truncated unless the option to output to a file is selected and a suitable file path specified. " +
                    "It is recommended that you first do that. Do you want to exit (select Yes in that case) or continue at your own risk (select No in that case)?",
                    "Input is large, risk of truncation or errors",
                    MessageBoxButtons.YesNo)) {
                    return;
                }
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
            using (var baseAddressForm = new MultilineInput(this._baseAddressesString, true)) {
                baseAddressForm.StartPosition = FormStartPosition.CenterParent;
                DialogResult res = baseAddressForm.ShowDialog(this);

                if (res == DialogResult.OK) {
                    this._baseAddressesString = baseAddressForm.baseaddressesstring;
                }
                else {
                    return;
                }
            }
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

        private void GetPDBDnldScript_Click(object sender, EventArgs e) {
            this.ShowStatus("Getting PDB download script... please wait. This may take a while!");

            var symDetails = StackResolver.GetSymbolDetailsForBinaries(binaryPaths.Text.Split(';').ToList(),
                DLLrecurse.Checked);

            if (0 == symDetails.Count) {
                return;
            }

            var fakeBuild = new SQLBuildInfo() {
                SymbolDetails = symDetails
            };

            var downloadCmds = SQLBuildInfo.GetDownloadScriptPowerShell(fakeBuild, false);

            this.ShowStatus(string.Empty);

            using (var outputCmds = new MultilineInput(downloadCmds.ToString(CultureInfo.CurrentCulture), false)) {
                outputCmds.StartPosition = FormStartPosition.CenterParent;
                outputCmds.ShowDialog(this);
            }
        }

        private void ShowStatus(string txt) {
            this.statusLabel.Text = txt;
            Application.DoEvents();
        }

        private void LoadXELButton_Click(object sender, EventArgs e) {
            genericOpenFileDlg.Multiselect = true;
            genericOpenFileDlg.CheckPathExists = true;
            genericOpenFileDlg.CheckFileExists = true;
            genericOpenFileDlg.FileName = String.Empty;
            genericOpenFileDlg.Filter = "XEL files (*.xel)|*.xel|All files (*.*)|*.*";
            genericOpenFileDlg.Title = "Select XEL file";

            var res = genericOpenFileDlg.ShowDialog(this);

            if (res != DialogResult.Cancel) {
                this.ShowStatus("Loading from XEL files; please wait. This may take a while!");

                this.backgroundTask = Task.Run(() => {
                    return this._resolver.ExtractFromXEL(genericOpenFileDlg.FileNames, BucketizeXEL.Checked).Item2;
                });

                this.MonitorBackgroundTask(backgroundTask);

                callStackInput.Text = backgroundTask.Result;

                this.ShowStatus("Finished importing callstacks from XEL file(s)!");
            }
        }

        private void MonitorBackgroundTask(Task backgroundTask) {
            using (backgroundCTS = new CancellationTokenSource()) {
                backgroundCT = backgroundCTS.Token;

                this.EnableCancelButton();

                while (!backgroundTask.Wait(30)) {
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

        private void CallStackInput_DragDrop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true) {
                e.Effect = DragDropEffects.All;

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length != 0) {
                    var allFilesContent = new StringBuilder();

                    // sample the first file selected and if it is XEL assume all the files are XEL
                    // if there is any other format in between, it will be rejected by the ExtractFromXEL code
                    if (Path.GetExtension(files[0]).ToLower(CultureInfo.CurrentCulture) == ".xel") {
                        this.ShowStatus("XEL file was dragged; please wait while we extract events from the file");

                        allFilesContent.AppendLine(this._resolver.ExtractFromXEL(files, BucketizeXEL.Checked).Item2);

                        this.ShowStatus(string.Empty);
                    }
                    else {
                        // handle the files as text input
                        foreach (var currFile in files) {
                            allFilesContent.AppendLine(File.ReadAllText(currFile));
                        }
                    }

                    callStackInput.Text = allFilesContent.ToString();
                }
            }
        }

        private void CallStackInput_DragOver(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void PDBPathPicker_Click(object sender, EventArgs e) {
            genericOpenFileDlg.Multiselect = false;
            genericOpenFileDlg.CheckPathExists = false;
            genericOpenFileDlg.CheckFileExists = false;
            genericOpenFileDlg.FileName = "select folder only";
            genericOpenFileDlg.Filter = "All files (*.*)|*.*";
            genericOpenFileDlg.Title = "Select FOLDER path to your PDBs";

            var res = genericOpenFileDlg.ShowDialog(this);

            if (res != DialogResult.Cancel) {
                pdbPaths.AppendText((pdbPaths.TextLength == 0 ? string.Empty : ";") + Path.GetDirectoryName(genericOpenFileDlg.FileName));
            }
        }

        private void BinaryPathPicker_Click(object sender, EventArgs e) {
            genericOpenFileDlg.Multiselect = false;
            genericOpenFileDlg.CheckPathExists = false;
            genericOpenFileDlg.CheckFileExists = false;
            genericOpenFileDlg.FileName = "select folder only";
            genericOpenFileDlg.Filter = "All files (*.*)|*.*";
            genericOpenFileDlg.Title = "Select FOLDER path to the SQL binaries";

            var res = genericOpenFileDlg.ShowDialog(this);

            if (res != DialogResult.Cancel) {
                binaryPaths.AppendText((binaryPaths.TextLength == 0 ? string.Empty : ";") + Path.GetDirectoryName(genericOpenFileDlg.FileName));
            }
        }

        private void SelectSQLPDB_Click(object sender, EventArgs e) {
            using (var sqlbuildsForm = new SQLBuildsForm {
                pathToPDBs = ConfigurationManager.AppSettings["PDBDownloadFolder"]
            }) {
                sqlbuildsForm.StartPosition = FormStartPosition.CenterParent;
                DialogResult res = sqlbuildsForm.ShowDialog(this);

                this.pdbPaths.AppendText((pdbPaths.TextLength == 0 ? string.Empty : ";") + sqlbuildsForm.lastDownloadedSymFolder);
            }
        }

        private void MainForm_Load(object sender, EventArgs e) {
            DateTime lastUpdDateTimeServer = DateTime.MinValue;
            DateTime lastUpdDateTimeLocal = DateTime.MinValue;

            var sqlBuildInfoUpdateURLs = ConfigurationManager.AppSettings["SQLBuildInfoUpdateURLs"].Split(';');
            var sqlBuildInfoURLs = ConfigurationManager.AppSettings["SQLBuildInfoURLs"].Split(';');

            // get the timestamp of the first valid file within SQLBuildInfoURLs
            foreach (var url in sqlBuildInfoUpdateURLs) {
                HttpWebResponse httpResp = null;
                var httpReq = (HttpWebRequest)WebRequest.Create(new Uri(url));

                try {
                    httpResp = (HttpWebResponse)httpReq.GetResponse();
                } catch (WebException) {
                }

                if (httpResp != null) {
                    if (httpResp.StatusCode == HttpStatusCode.OK) {
                        // get content of lastupdated.txt from upstream (GitHub) repo
                        using (var strm = new StreamReader(httpResp.GetResponseStream())) {
                            string lastUpd = strm.ReadToEnd().Trim();
                            lastUpdDateTimeServer = DateTime.ParseExact(lastUpd,
                                LastUpdatedTimestampFormat,
                                new CultureInfo(LastUpdatedTimestampCulture)
                                );
                        }

                        // get content of local lastupdated.txt (if it exists)
                        if (File.Exists(LastUpdatedTimestampFileName)) {
                            using (var strm = new StreamReader(LastUpdatedTimestampFileName)) {
                                string lastUpd = strm.ReadToEnd().Trim();
                                lastUpdDateTimeLocal = DateTime.ParseExact(lastUpd,
                                    LastUpdatedTimestampFormat,
                                    new CultureInfo(LastUpdatedTimestampCulture)
                                    );
                            }
                        } else {
                            lastUpdDateTimeLocal = DateTime.MinValue;
                        }

                        if (lastUpdDateTimeServer > lastUpdDateTimeLocal) {
                            // if the server timestamp > local timestamp, prompt to download
                            var res = MessageBox.Show(this,
                                "The SQLBuildInfo.json file was updated recently on GitHub. Do you wish to update your copy with the newer version?",
                                "SQL Build info updated",
                                MessageBoxButtons.YesNo);

                            if (DialogResult.Yes == res) {
                                foreach (var jsonURL in sqlBuildInfoURLs) {
                                    httpResp = null;
                                    httpReq = (HttpWebRequest)WebRequest.Create(new Uri(jsonURL));

                                    try {
                                        httpResp = (HttpWebResponse)httpReq.GetResponse();
                                    } catch (WebException) {
                                    }

                                    if (httpResp != null) {
                                        if (httpResp.StatusCode == HttpStatusCode.OK) {
                                            try {
                                                using (var strm = new StreamReader(httpResp.GetResponseStream())) {
                                                    var jsonContent = strm.ReadToEnd();

                                                    // update local copy of build info file
                                                    using (var writer = new StreamWriter(SqlBuildInfoFileName)) {
                                                        writer.Write(jsonContent);
                                                        writer.Flush();
                                                        writer.Close();
                                                    }

                                                    // update local last updated timestamp
                                                    using (var wr = new StreamWriter(LastUpdatedTimestampFileName, false)) {
                                                        wr.Write(lastUpdDateTimeServer.ToString(
                                                            LastUpdatedTimestampFormat,
                                                            new CultureInfo(LastUpdatedTimestampCulture)
                                                            ));
                                                    }
                                                }
                                            } catch (WebException) {
                                                MessageBox.Show(this,
                                                    "Could not download SQL Build Info file due to HTTP errors.",
                                                    "Error",
                                                    MessageBoxButtons.OK,
                                                    MessageBoxIcon.Error);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        break;
                    }
                }
            }
        }

        private void outputFilePathPicker_Click(object sender, EventArgs e) {
            genericSaveFileDlg.FileName = "resolvedstacks.txt";
            genericSaveFileDlg.Filter = "Text files (*.txt)|*.txt";
            genericSaveFileDlg.Title = "Save output as";

            var res = genericSaveFileDlg.ShowDialog(this);

            if (res != DialogResult.Cancel) {
                outputFilePath.Text = genericSaveFileDlg.FileName;
            }
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            this.backgroundCTS.Cancel();
        }
    }
}
