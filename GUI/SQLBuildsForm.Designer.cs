// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    partial class SQLBuildsForm {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        #region Windows Form Designer generated code
        private void InitializeComponent() {
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.downloadStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.downloadProgress = new System.Windows.Forms.ToolStripProgressBar();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.checkPDBAvail = new System.Windows.Forms.Button();
            this.dnldButton = new System.Windows.Forms.Button();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.treeviewSyms = new System.Windows.Forms.TreeView();
            this.searchLabel = new System.Windows.Forms.Label();
            this.searchText = new System.Windows.Forms.TextBox();
            this.findNext = new System.Windows.Forms.Button();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.downloadStatus,
            this.downloadProgress});
            this.statusStrip1.Location = new System.Drawing.Point(0, 645);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 13, 0);
            this.statusStrip1.Size = new System.Drawing.Size(548, 25);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            this.downloadStatus.Name = "downloadStatus";
            this.downloadStatus.Size = new System.Drawing.Size(0, 19);
            this.downloadProgress.Name = "downloadProgress";
            this.downloadProgress.Size = new System.Drawing.Size(100, 17);
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            this.splitContainer1.Panel2.Controls.Add(this.checkPDBAvail);
            this.splitContainer1.Panel2.Controls.Add(this.dnldButton);
            this.splitContainer1.Size = new System.Drawing.Size(548, 670);
            this.splitContainer1.SplitterDistance = 600;
            this.splitContainer1.TabIndex = 3;
            this.checkPDBAvail.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.checkPDBAvail.Location = new System.Drawing.Point(88, 7);
            this.checkPDBAvail.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkPDBAvail.Name = "checkPDBAvail";
            this.checkPDBAvail.Size = new System.Drawing.Size(171, 32);
            this.checkPDBAvail.TabIndex = 3;
            this.checkPDBAvail.Text = "Check PDB availability";
            this.checkPDBAvail.UseVisualStyleBackColor = true;
            this.checkPDBAvail.Click += new System.EventHandler(this.CheckPDBAvail_Click);
            this.dnldButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dnldButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.dnldButton.Location = new System.Drawing.Point(265, 7);
            this.dnldButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dnldButton.Name = "dnldButton";
            this.dnldButton.Size = new System.Drawing.Size(189, 32);
            this.dnldButton.TabIndex = 2;
            this.dnldButton.Text = "Download PDBs";
            this.dnldButton.UseVisualStyleBackColor = true;
            this.dnldButton.Click += new System.EventHandler(this.DownloadPDBs);
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.IsSplitterFixed = true;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitContainer2.Panel1.Controls.Add(this.findNext);
            this.splitContainer2.Panel1.Controls.Add(this.searchText);
            this.splitContainer2.Panel1.Controls.Add(this.searchLabel);
            this.splitContainer2.Panel2.Controls.Add(this.treeviewSyms);
            this.splitContainer2.Size = new System.Drawing.Size(548, 600);
            this.splitContainer2.SplitterDistance = 35;
            this.splitContainer2.TabIndex = 0;
            this.treeviewSyms.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeviewSyms.Location = new System.Drawing.Point(0, 0);
            this.treeviewSyms.Margin = new System.Windows.Forms.Padding(4);
            this.treeviewSyms.Name = "treeviewSyms";
            this.treeviewSyms.Size = new System.Drawing.Size(548, 561);
            this.treeviewSyms.TabIndex = 1;
            this.searchLabel.AutoSize = true;
            this.searchLabel.Location = new System.Drawing.Point(10, 12);
            this.searchLabel.Name = "searchLabel";
            this.searchLabel.Size = new System.Drawing.Size(150, 17);
            this.searchLabel.TabIndex = 0;
            this.searchLabel.Text = "SQL version / keyword";
            this.searchText.Location = new System.Drawing.Point(166, 9);
            this.searchText.Name = "searchText";
            this.searchText.Size = new System.Drawing.Size(296, 22);
            this.searchText.TabIndex = 1;
            this.findNext.Location = new System.Drawing.Point(468, 9);
            this.findNext.Name = "findNext";
            this.findNext.Size = new System.Drawing.Size(75, 23);
            this.findNext.TabIndex = 2;
            this.findNext.Text = "Find";
            this.findNext.UseVisualStyleBackColor = true;
            this.findNext.Click += new System.EventHandler(this.findNext_Click);
            this.AcceptButton = this.dnldButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(548, 670);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.splitContainer1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SQLBuildsForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Select SQL build";
            this.Load += new System.EventHandler(this.Treeview_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel downloadStatus;
        private System.Windows.Forms.ToolStripProgressBar downloadProgress;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button dnldButton;
        private System.Windows.Forms.Button checkPDBAvail;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TreeView treeviewSyms;
        private System.Windows.Forms.TextBox searchText;
        private System.Windows.Forms.Label searchLabel;
        private System.Windows.Forms.Button findNext;
    }
}
