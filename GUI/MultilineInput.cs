// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    public partial class MultilineInput : Form {
        public MultilineInput(string initialtext, bool showFilepicker) {
            InitializeComponent();
            if (!string.IsNullOrEmpty(initialtext)) this.InputAddresses.Text = initialtext;
            loadFromFile.Visible = showFilepicker;
        }

        public string Baseaddressesstring {
            get { return this.InputAddresses.Text; }
        }

        private void InputAddresses_KeyDown(object sender, KeyEventArgs e) {
            if (e.Control && e.KeyCode == Keys.A) InputAddresses.SelectAll();
        }

        private void InputAddresses_DragDrop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false)) {
                e.Effect = DragDropEffects.All;

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length != 0) {
                    var allFilesContent = new StringBuilder();
                    foreach (var currFile in files) { allFilesContent.AppendLine(File.ReadAllText(currFile)); }
                    InputAddresses.Text = allFilesContent.ToString();
                }
            }
        }

        private void InputAddresses_DragOver(object sender, DragEventArgs e) {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void LoadFromFile_Click(object sender, System.EventArgs e) {
            fileDlg.Multiselect = false;
            fileDlg.CheckPathExists = true;
            fileDlg.CheckFileExists = true;
            fileDlg.FileName = string.Empty;
            fileDlg.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            fileDlg.Title = "Select file";
            if (DialogResult.Cancel != fileDlg.ShowDialog(this)) InputAddresses.Text = File.ReadAllText(fileDlg.FileName);
        }
    }
}
