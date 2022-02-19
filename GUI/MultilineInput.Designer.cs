// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    partial class MultilineInput {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        #region Windows Form Designer generated code
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MultilineInput));
            this.OkButton = new System.Windows.Forms.Button();
            this.FormCancelButton = new System.Windows.Forms.Button();
            this.InputAddresses = new System.Windows.Forms.TextBox();
            this.loadFromFile = new System.Windows.Forms.Button();
            this.fileDlg = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            this.OkButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OkButton.Location = new System.Drawing.Point(180, 320);
            this.OkButton.Margin = new System.Windows.Forms.Padding(2);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(47, 23);
            this.OkButton.TabIndex = 0;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
            this.FormCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.FormCancelButton.Location = new System.Drawing.Point(239, 320);
            this.FormCancelButton.Margin = new System.Windows.Forms.Padding(2);
            this.FormCancelButton.Name = "FormCancelButton";
            this.FormCancelButton.Size = new System.Drawing.Size(56, 23);
            this.FormCancelButton.TabIndex = 1;
            this.FormCancelButton.Text = "Cancel";
            this.FormCancelButton.UseVisualStyleBackColor = true;
            this.InputAddresses.AllowDrop = true;
            this.InputAddresses.Location = new System.Drawing.Point(0, 0);
            this.InputAddresses.Margin = new System.Windows.Forms.Padding(2);
            this.InputAddresses.MaxLength = 999999999;
            this.InputAddresses.Multiline = true;
            this.InputAddresses.Name = "InputAddresses";
            this.InputAddresses.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.InputAddresses.Size = new System.Drawing.Size(468, 313);
            this.InputAddresses.TabIndex = 2;
            this.InputAddresses.Text = resources.GetString("InputAddresses.Text");
            this.InputAddresses.WordWrap = false;
            this.InputAddresses.DragDrop += new System.Windows.Forms.DragEventHandler(this.InputAddresses_DragDrop);
            this.InputAddresses.DragOver += new System.Windows.Forms.DragEventHandler(this.InputAddresses_DragOver);
            this.InputAddresses.KeyDown += new System.Windows.Forms.KeyEventHandler(this.InputAddresses_KeyDown);
            this.loadFromFile.Location = new System.Drawing.Point(12, 320);
            this.loadFromFile.Name = "loadFromFile";
            this.loadFromFile.Size = new System.Drawing.Size(93, 23);
            this.loadFromFile.TabIndex = 3;
            this.loadFromFile.Text = "Load from file";
            this.loadFromFile.UseVisualStyleBackColor = true;
            this.loadFromFile.Click += new System.EventHandler(this.loadFromFile_Click);
            this.fileDlg.Filter = "Text files|*.txt";
            this.AcceptButton = this.OkButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(467, 349);
            this.Controls.Add(this.loadFromFile);
            this.Controls.Add(this.InputAddresses);
            this.Controls.Add(this.FormCancelButton);
            this.Controls.Add(this.OkButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MultilineInput";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "MultilineInput";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion
        private System.Windows.Forms.Button OkButton;
        private System.Windows.Forms.Button FormCancelButton;
        private System.Windows.Forms.TextBox InputAddresses;
        private System.Windows.Forms.Button loadFromFile;
        private System.Windows.Forms.OpenFileDialog fileDlg;
    }
}
