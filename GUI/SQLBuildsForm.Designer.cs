// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    partial class SQLBuildsForm {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }
        #region Windows Form Designer generated code
        private void InitializeComponent() {
            this.buildFormStatusStrip = new System.Windows.Forms.StatusStrip();
            this.downloadStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.downloadProgress = new System.Windows.Forms.ToolStripProgressBar();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.findNext = new System.Windows.Forms.Button();
            this.searchText = new System.Windows.Forms.TextBox();
            this.searchLabel = new System.Windows.Forms.Label();
            this.treeviewSyms = new System.Windows.Forms.TreeView();
            this.checkPDBAvail = new System.Windows.Forms.Button();
            this.dnldButton = new System.Windows.Forms.Button();
            this.buildFormStatusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // buildFormStatusStrip
            // 
            this.buildFormStatusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.buildFormStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.downloadStatus,
            this.downloadProgress});
            this.buildFormStatusStrip.Location = new System.Drawing.Point(0, 807);
            this.buildFormStatusStrip.Name = "buildFormStatusStrip";
            this.buildFormStatusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 13, 0);
            this.buildFormStatusStrip.Size = new System.Drawing.Size(548, 30);
            this.buildFormStatusStrip.TabIndex = 2;
            this.buildFormStatusStrip.Text = "statusStrip1";
            // 
            // downloadStatus
            // 
            this.downloadStatus.Name = "downloadStatus";
            this.downloadStatus.Size = new System.Drawing.Size(0, 24);
            // 
            // downloadProgress
            // 
            this.downloadProgress.AccessibleDescription = "Progress of download";
            this.downloadProgress.AccessibleName = "downloadProgress";
            this.downloadProgress.Name = "downloadProgress";
            this.downloadProgress.Size = new System.Drawing.Size(100, 22);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.checkPDBAvail);
            this.splitContainer1.Panel2.Controls.Add(this.dnldButton);
            this.splitContainer1.Size = new System.Drawing.Size(548, 837);
            this.splitContainer1.SplitterDistance = 767;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 3;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.IsSplitterFixed = true;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.findNext);
            this.splitContainer2.Panel1.Controls.Add(this.searchText);
            this.splitContainer2.Panel1.Controls.Add(this.searchLabel);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.treeviewSyms);
            this.splitContainer2.Size = new System.Drawing.Size(548, 767);
            this.splitContainer2.SplitterDistance = 35;
            this.splitContainer2.SplitterWidth = 5;
            this.splitContainer2.TabIndex = 0;
            // 
            // findNext
            // 
            this.findNext.AccessibleDescription = "Find";
            this.findNext.AccessibleName = "findNext";
            this.findNext.Location = new System.Drawing.Point(468, 11);
            this.findNext.Name = "findNext";
            this.findNext.Size = new System.Drawing.Size(75, 29);
            this.findNext.TabIndex = 2;
            this.findNext.Text = "Find";
            this.findNext.UseVisualStyleBackColor = true;
            this.findNext.Click += new System.EventHandler(this.FindNext_Click);
            // 
            // searchText
            // 
            this.searchText.AccessibleDescription = "Search term goes here";
            this.searchText.AccessibleName = "searchText";
            this.searchText.Location = new System.Drawing.Point(165, 11);
            this.searchText.Name = "searchText";
            this.searchText.Size = new System.Drawing.Size(296, 27);
            this.searchText.TabIndex = 1;
            // 
            // searchLabel
            // 
            this.searchLabel.AutoSize = true;
            this.searchLabel.Location = new System.Drawing.Point(11, 15);
            this.searchLabel.Name = "searchLabel";
            this.searchLabel.Size = new System.Drawing.Size(156, 20);
            this.searchLabel.TabIndex = 0;
            this.searchLabel.Text = "SQL version / keyword";
            // 
            // treeviewSyms
            // 
            this.treeviewSyms.AccessibleDescription = "SQL Server versions and builds";
            this.treeviewSyms.AccessibleName = "treeviewSyms";
            this.treeviewSyms.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeviewSyms.Location = new System.Drawing.Point(0, 0);
            this.treeviewSyms.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.treeviewSyms.Name = "treeviewSyms";
            this.treeviewSyms.Size = new System.Drawing.Size(548, 727);
            this.treeviewSyms.TabIndex = 1;
            // 
            // checkPDBAvail
            // 
            this.checkPDBAvail.AccessibleDescription = "Check if public PDBs for this build are available";
            this.checkPDBAvail.AccessibleName = "checkPDBAvail";
            this.checkPDBAvail.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.checkPDBAvail.Location = new System.Drawing.Point(88, 1);
            this.checkPDBAvail.Name = "checkPDBAvail";
            this.checkPDBAvail.Size = new System.Drawing.Size(171, 40);
            this.checkPDBAvail.TabIndex = 3;
            this.checkPDBAvail.Text = "Check PDB availability";
            this.checkPDBAvail.UseVisualStyleBackColor = true;
            this.checkPDBAvail.Click += new System.EventHandler(this.CheckPDBAvail_Click);
            // 
            // dnldButton
            // 
            this.dnldButton.AccessibleDescription = "Download public PDBs";
            this.dnldButton.AccessibleName = "dnldButton";
            this.dnldButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dnldButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.dnldButton.Location = new System.Drawing.Point(265, 1);
            this.dnldButton.Name = "dnldButton";
            this.dnldButton.Size = new System.Drawing.Size(189, 40);
            this.dnldButton.TabIndex = 2;
            this.dnldButton.Text = "Download PDBs";
            this.dnldButton.UseVisualStyleBackColor = true;
            this.dnldButton.Click += new System.EventHandler(this.DownloadPDBs);
            // 
            // SQLBuildsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(548, 837);
            this.Controls.Add(this.buildFormStatusStrip);
            this.Controls.Add(this.splitContainer1);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SQLBuildsForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Select SQL build";
            this.Load += new System.EventHandler(this.Treeview_Load);
            this.buildFormStatusStrip.ResumeLayout(false);
            this.buildFormStatusStrip.PerformLayout();
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
        private System.Windows.Forms.StatusStrip buildFormStatusStrip;
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
