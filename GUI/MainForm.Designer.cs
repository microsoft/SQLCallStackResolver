﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    partial class MainForm {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _resolver.Dispose();
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }
        #region Windows Form Designer generated code
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.genericOpenFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.callStackInput = new System.Windows.Forms.TextBox();
            this.finalOutput = new System.Windows.Forms.TextBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.DLLrecurse = new System.Windows.Forms.CheckBox();
            this.BinaryPathPicker = new System.Windows.Forms.Button();
            this.binaryPaths = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.GroupXEvents = new System.Windows.Forms.CheckBox();
            this.LoadXELButton = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.cachePDB = new System.Windows.Forms.CheckBox();
            this.selectSQLPDB = new System.Windows.Forms.Button();
            this.PDBPathPicker = new System.Windows.Forms.Button();
            this.pdbRecurse = new System.Windows.Forms.CheckBox();
            this.pdbPaths = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.showInlineFrames = new System.Windows.Forms.CheckBox();
            this.outputFilePathPicker = new System.Windows.Forms.Button();
            this.outputFilePath = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.FramesOnSingleLine = new System.Windows.Forms.CheckBox();
            this.IncludeLineNumbers = new System.Windows.Forms.CheckBox();
            this.EnterBaseAddresses = new System.Windows.Forms.Button();
            this.includeOffsets = new System.Windows.Forms.CheckBox();
            this.RelookupSource = new System.Windows.Forms.CheckBox();
            this.ResolveCallStackButton = new System.Windows.Forms.Button();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.cancelButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.formToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.genericSaveFileDlg = new System.Windows.Forms.SaveFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.splitContainer1);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.groupBox4);
            this.splitContainer2.Panel2.Controls.Add(this.groupBox3);
            this.splitContainer2.Panel2.Controls.Add(this.groupBox2);
            this.splitContainer2.Panel2.Controls.Add(this.groupBox1);
            this.splitContainer2.Panel2.Controls.Add(this.ResolveCallStackButton);
            this.splitContainer2.Size = new System.Drawing.Size(1305, 878);
            this.splitContainer2.SplitterDistance = 388;
            this.splitContainer2.SplitterWidth = 5;
            this.splitContainer2.TabIndex = 0;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.callStackInput);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.finalOutput);
            this.splitContainer1.Size = new System.Drawing.Size(1305, 388);
            this.splitContainer1.SplitterDistance = 430;
            this.splitContainer1.TabIndex = 0;
            // 
            // callStackInput
            // 
            this.callStackInput.AccessibleDescription = "Input goes here";
            this.callStackInput.AccessibleName = "callStackInput";
            this.callStackInput.AllowDrop = true;
            this.callStackInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.callStackInput.Location = new System.Drawing.Point(0, 0);
            this.callStackInput.MaxLength = 999999999;
            this.callStackInput.Multiline = true;
            this.callStackInput.Name = "callStackInput";
            this.callStackInput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.callStackInput.Size = new System.Drawing.Size(430, 388);
            this.callStackInput.TabIndex = 0;
            this.callStackInput.Text = resources.GetString("callStackInput.Text");
            this.callStackInput.WordWrap = false;
            this.callStackInput.DragDrop += new System.Windows.Forms.DragEventHandler(this.CallStackInput_DragDrop);
            this.callStackInput.DragEnter += new System.Windows.Forms.DragEventHandler(this.CallStackInput_DragOver);
            // 
            // finalOutput
            // 
            this.finalOutput.AccessibleDescription = "Output is displayed here";
            this.finalOutput.AccessibleName = "finalOutput";
            this.finalOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.finalOutput.Location = new System.Drawing.Point(0, 0);
            this.finalOutput.MaxLength = 999999999;
            this.finalOutput.Multiline = true;
            this.finalOutput.Name = "finalOutput";
            this.finalOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.finalOutput.Size = new System.Drawing.Size(871, 388);
            this.finalOutput.TabIndex = 0;
            this.finalOutput.WordWrap = false;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.DLLrecurse);
            this.groupBox4.Controls.Add(this.BinaryPathPicker);
            this.groupBox4.Controls.Add(this.binaryPaths);
            this.groupBox4.Controls.Add(this.label2);
            this.groupBox4.Location = new System.Drawing.Point(15, 365);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox4.Size = new System.Drawing.Size(1276, 66);
            this.groupBox4.TabIndex = 4;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "SPECIAL CASES";
            // 
            // DLLrecurse
            // 
            this.DLLrecurse.AccessibleDescription = "Search for binaries recursively";
            this.DLLrecurse.AccessibleName = "DLLrecurse";
            this.DLLrecurse.AutoSize = true;
            this.DLLrecurse.Location = new System.Drawing.Point(991, 25);
            this.DLLrecurse.Name = "DLLrecurse";
            this.DLLrecurse.Size = new System.Drawing.Size(264, 24);
            this.DLLrecurse.TabIndex = 3;
            this.DLLrecurse.Text = "Search for DLLs and EXE recursively";
            this.DLLrecurse.UseVisualStyleBackColor = true;
            // 
            // BinaryPathPicker
            // 
            this.BinaryPathPicker.Location = new System.Drawing.Point(952, 23);
            this.BinaryPathPicker.Name = "BinaryPathPicker";
            this.BinaryPathPicker.Size = new System.Drawing.Size(33, 29);
            this.BinaryPathPicker.TabIndex = 2;
            this.BinaryPathPicker.Text = "...";
            this.BinaryPathPicker.UseVisualStyleBackColor = true;
            this.BinaryPathPicker.Click += new System.EventHandler(this.BinaryPathPicker_Click);
            // 
            // binaryPaths
            // 
            this.binaryPaths.AccessibleDescription = "Path(s) to folders containing binaries";
            this.binaryPaths.AccessibleName = "binaryPaths";
            this.binaryPaths.Location = new System.Drawing.Point(616, 23);
            this.binaryPaths.Name = "binaryPaths";
            this.binaryPaths.Size = new System.Drawing.Size(330, 27);
            this.binaryPaths.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 26);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(604, 20);
            this.label2.TabIndex = 0;
            this.label2.Text = "Specify Path(s) to SQL Server binaries (only needed when frames have OrdinalNNN v" +
    "alues)";
            this.formToolTip.SetToolTip(this.label2, "Only need to do this if you are dealing with incomplete stacks collected by -T365" +
        "6 OR if you need to get PowerShell commands to download PDBs for a specific buil" +
        "d of SQL");
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.GroupXEvents);
            this.groupBox3.Controls.Add(this.LoadXELButton);
            this.groupBox3.Location = new System.Drawing.Point(15, 114);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(1276, 80);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "STEP 1: Either directly paste raw callstack(s) in textbox above, or import XEL fi" +
    "le(s)";
            // 
            // GroupXEvents
            // 
            this.GroupXEvents.AccessibleDescription = "Group similar XEvents into a histogram";
            this.GroupXEvents.AccessibleName = "GroupXEvents";
            this.GroupXEvents.AutoSize = true;
            this.GroupXEvents.Checked = true;
            this.GroupXEvents.CheckState = System.Windows.Forms.CheckState.Checked;
            this.GroupXEvents.Location = new System.Drawing.Point(332, 35);
            this.GroupXEvents.Name = "GroupXEvents";
            this.GroupXEvents.Size = new System.Drawing.Size(664, 24);
            this.GroupXEvents.TabIndex = 1;
            this.GroupXEvents.Text = "Group events from XEL files (generally leave this checked unless you need individ" +
    "ual event data)";
            this.GroupXEvents.UseVisualStyleBackColor = true;
            // 
            // LoadXELButton
            // 
            this.LoadXELButton.AccessibleDescription = "Load XEL file(s)";
            this.LoadXELButton.AccessibleName = "LoadXELButton";
            this.LoadXELButton.Location = new System.Drawing.Point(11, 23);
            this.LoadXELButton.Name = "LoadXELButton";
            this.LoadXELButton.Size = new System.Drawing.Size(304, 46);
            this.LoadXELButton.TabIndex = 0;
            this.LoadXELButton.Text = "Select XEL files and import callstacks";
            this.LoadXELButton.UseVisualStyleBackColor = true;
            this.LoadXELButton.Click += new System.EventHandler(this.LoadXELButton_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.cachePDB);
            this.groupBox2.Controls.Add(this.selectSQLPDB);
            this.groupBox2.Controls.Add(this.PDBPathPicker);
            this.groupBox2.Controls.Add(this.pdbRecurse);
            this.groupBox2.Controls.Add(this.pdbPaths);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Location = new System.Drawing.Point(15, 200);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(1276, 82);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "STEP 2: Either use preset symbol downloads or set custom PDB search paths";
            // 
            // cachePDB
            // 
            this.cachePDB.AccessibleDescription = "Cache PDBs in a temporary folder";
            this.cachePDB.AccessibleName = "cachePDB";
            this.cachePDB.AutoSize = true;
            this.cachePDB.Location = new System.Drawing.Point(1076, 34);
            this.cachePDB.Name = "cachePDB";
            this.cachePDB.Size = new System.Drawing.Size(109, 24);
            this.cachePDB.TabIndex = 5;
            this.cachePDB.Text = "Cache PDBs";
            this.formToolTip.SetToolTip(this.cachePDB, "This option will copy PDBs from the paths specified to the %TEMP%\\SymCache folder" +
        ". It is highly recommended to use this if you have a UNC path specified.");
            this.cachePDB.UseVisualStyleBackColor = true;
            // 
            // selectSQLPDB
            // 
            this.selectSQLPDB.AccessibleDescription = "Open the dialog to choose a SQL Server build whose symbols are needed";
            this.selectSQLPDB.AccessibleName = "selectSQLPDB";
            this.selectSQLPDB.Location = new System.Drawing.Point(9, 29);
            this.selectSQLPDB.Name = "selectSQLPDB";
            this.selectSQLPDB.Size = new System.Drawing.Size(377, 38);
            this.selectSQLPDB.TabIndex = 0;
            this.selectSQLPDB.Text = "Use public PDBs for a known SQL Server build";
            this.selectSQLPDB.UseVisualStyleBackColor = true;
            this.selectSQLPDB.Click += new System.EventHandler(this.SelectSQLPDB_Click);
            // 
            // PDBPathPicker
            // 
            this.PDBPathPicker.Location = new System.Drawing.Point(825, 34);
            this.PDBPathPicker.Name = "PDBPathPicker";
            this.PDBPathPicker.Size = new System.Drawing.Size(33, 29);
            this.PDBPathPicker.TabIndex = 3;
            this.PDBPathPicker.Text = "...";
            this.PDBPathPicker.UseVisualStyleBackColor = true;
            this.PDBPathPicker.Click += new System.EventHandler(this.PDBPathPicker_Click);
            // 
            // pdbRecurse
            // 
            this.pdbRecurse.AccessibleDescription = "Search for PDBs recursively";
            this.pdbRecurse.AccessibleName = "pdbRecurse";
            this.pdbRecurse.AutoSize = true;
            this.pdbRecurse.Checked = true;
            this.pdbRecurse.CheckState = System.Windows.Forms.CheckState.Checked;
            this.pdbRecurse.Location = new System.Drawing.Point(864, 34);
            this.pdbRecurse.Name = "pdbRecurse";
            this.pdbRecurse.Size = new System.Drawing.Size(209, 24);
            this.pdbRecurse.TabIndex = 4;
            this.pdbRecurse.Text = "Search for PDBs recursively";
            this.pdbRecurse.UseVisualStyleBackColor = true;
            // 
            // pdbPaths
            // 
            this.pdbPaths.AccessibleDescription = "Path to PDBs";
            this.pdbPaths.AccessibleName = "pdbPaths";
            this.pdbPaths.Location = new System.Drawing.Point(507, 32);
            this.pdbPaths.Name = "pdbPaths";
            this.pdbPaths.Size = new System.Drawing.Size(312, 27);
            this.pdbPaths.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(392, 37);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(109, 20);
            this.label1.TabIndex = 1;
            this.label1.Text = "Path(s) to PDBs";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.showInlineFrames);
            this.groupBox1.Controls.Add(this.outputFilePathPicker);
            this.groupBox1.Controls.Add(this.outputFilePath);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.FramesOnSingleLine);
            this.groupBox1.Controls.Add(this.IncludeLineNumbers);
            this.groupBox1.Controls.Add(this.EnterBaseAddresses);
            this.groupBox1.Controls.Add(this.includeOffsets);
            this.groupBox1.Controls.Add(this.RelookupSource);
            this.groupBox1.Location = new System.Drawing.Point(15, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1276, 105);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "STEP 0: Input and output options";
            // 
            // showInlineFrames
            // 
            this.showInlineFrames.AccessibleDescription = "Display inline frames in the output?";
            this.showInlineFrames.AccessibleName = "showInlineFrames";
            this.showInlineFrames.AutoSize = true;
            this.showInlineFrames.Checked = true;
            this.showInlineFrames.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showInlineFrames.Location = new System.Drawing.Point(1093, 63);
            this.showInlineFrames.Name = "showInlineFrames";
            this.showInlineFrames.Size = new System.Drawing.Size(178, 24);
            this.showInlineFrames.TabIndex = 8;
            this.showInlineFrames.Text = "OUTPUT: Inline frames";
            this.showInlineFrames.UseVisualStyleBackColor = true;
            // 
            // outputFilePathPicker
            // 
            this.outputFilePathPicker.Location = new System.Drawing.Point(1224, 28);
            this.outputFilePathPicker.Name = "outputFilePathPicker";
            this.outputFilePathPicker.Size = new System.Drawing.Size(33, 29);
            this.outputFilePathPicker.TabIndex = 5;
            this.outputFilePathPicker.Text = "...";
            this.outputFilePathPicker.UseVisualStyleBackColor = true;
            this.outputFilePathPicker.Click += new System.EventHandler(this.OutputFilePathPicker_Click);
            // 
            // outputFilePath
            // 
            this.outputFilePath.AccessibleDescription = "Output file path";
            this.outputFilePath.AccessibleName = "outputFilePath";
            this.outputFilePath.Location = new System.Drawing.Point(833, 26);
            this.outputFilePath.Name = "outputFilePath";
            this.outputFilePath.Size = new System.Drawing.Size(384, 27);
            this.outputFilePath.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(616, 31);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(217, 20);
            this.label3.TabIndex = 3;
            this.label3.Text = "OUTPUT: Redirect output to file";
            // 
            // FramesOnSingleLine
            // 
            this.FramesOnSingleLine.AccessibleName = "FramesOnSingleLine";
            this.FramesOnSingleLine.AutoSize = true;
            this.FramesOnSingleLine.Location = new System.Drawing.Point(9, 26);
            this.FramesOnSingleLine.Name = "FramesOnSingleLine";
            this.FramesOnSingleLine.Size = new System.Drawing.Size(310, 24);
            this.FramesOnSingleLine.TabIndex = 0;
            this.FramesOnSingleLine.Text = "INPUT: Callstack frames are in a single line";
            this.formToolTip.SetToolTip(this.FramesOnSingleLine, "Required if copy-pasting XE callstack from SSMS");
            this.FramesOnSingleLine.UseVisualStyleBackColor = true;
            // 
            // IncludeLineNumbers
            // 
            this.IncludeLineNumbers.AccessibleDescription = "Include source information in output?";
            this.IncludeLineNumbers.AccessibleName = "IncludeLineNumbers";
            this.IncludeLineNumbers.AutoSize = true;
            this.IncludeLineNumbers.Checked = true;
            this.IncludeLineNumbers.CheckState = System.Windows.Forms.CheckState.Checked;
            this.IncludeLineNumbers.Location = new System.Drawing.Point(805, 63);
            this.IncludeLineNumbers.Name = "IncludeLineNumbers";
            this.IncludeLineNumbers.Size = new System.Drawing.Size(270, 24);
            this.IncludeLineNumbers.TabIndex = 7;
            this.IncludeLineNumbers.Text = "OUTPUT: Source lines (private PDBs)";
            this.IncludeLineNumbers.UseVisualStyleBackColor = true;
            // 
            // EnterBaseAddresses
            // 
            this.EnterBaseAddresses.AccessibleName = "EnterBaseAddresses";
            this.EnterBaseAddresses.Location = new System.Drawing.Point(411, 26);
            this.EnterBaseAddresses.Name = "EnterBaseAddresses";
            this.EnterBaseAddresses.Size = new System.Drawing.Size(193, 58);
            this.EnterBaseAddresses.TabIndex = 2;
            this.EnterBaseAddresses.Text = "INPUT: Specify base addresses for modules";
            this.formToolTip.SetToolTip(this.EnterBaseAddresses, "Required for working with XEL files and hex address-only callstacks");
            this.EnterBaseAddresses.UseVisualStyleBackColor = true;
            this.EnterBaseAddresses.Click += new System.EventHandler(this.EnterBaseAddresses_Click);
            // 
            // includeOffsets
            // 
            this.includeOffsets.AccessibleDescription = "Whether to include offsets into functions in the output";
            this.includeOffsets.AccessibleName = "includeOffsets";
            this.includeOffsets.AutoSize = true;
            this.includeOffsets.Location = new System.Drawing.Point(616, 63);
            this.includeOffsets.Name = "includeOffsets";
            this.includeOffsets.Size = new System.Drawing.Size(171, 24);
            this.includeOffsets.TabIndex = 6;
            this.includeOffsets.Text = "OUTPUT: Func offsets";
            this.includeOffsets.UseVisualStyleBackColor = true;
            // 
            // RelookupSource
            // 
            this.RelookupSource.AccessibleName = "RelookupSource";
            this.RelookupSource.AutoSize = true;
            this.RelookupSource.Location = new System.Drawing.Point(9, 58);
            this.RelookupSource.Name = "RelookupSource";
            this.RelookupSource.Size = new System.Drawing.Size(402, 24);
            this.RelookupSource.TabIndex = 1;
            this.RelookupSource.Text = "INPUT: Re-lookup source (rare case, needs private PDBs)";
            this.RelookupSource.UseVisualStyleBackColor = true;
            // 
            // ResolveCallStackButton
            // 
            this.ResolveCallStackButton.AccessibleDescription = "Resolve call stacks";
            this.ResolveCallStackButton.AccessibleName = "ResolveCallStackButton";
            this.ResolveCallStackButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.ResolveCallStackButton.Location = new System.Drawing.Point(15, 288);
            this.ResolveCallStackButton.Name = "ResolveCallStackButton";
            this.ResolveCallStackButton.Size = new System.Drawing.Size(1276, 69);
            this.ResolveCallStackButton.TabIndex = 3;
            this.ResolveCallStackButton.Text = "STEP 3: Resolve callstacks!";
            this.ResolveCallStackButton.UseVisualStyleBackColor = true;
            this.ResolveCallStackButton.Click += new System.EventHandler(this.ResolveCallstacks_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel,
            this.progressBar,
            this.cancelButton});
            this.statusStrip.Location = new System.Drawing.Point(0, 836);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 19, 0);
            this.statusStrip.Size = new System.Drawing.Size(1305, 42);
            this.statusStrip.TabIndex = 0;
            this.statusStrip.Text = "statusStrip1";
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = false;
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(700, 36);
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // progressBar
            // 
            this.progressBar.AccessibleDescription = "Progress Bar";
            this.progressBar.AccessibleName = "taskProgress";
            this.progressBar.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.progressBar.AutoSize = false;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(100, 34);
            // 
            // cancelButton
            // 
            this.cancelButton.AccessibleDescription = "Cancel running operation";
            this.cancelButton.AccessibleName = "cancelButton";
            this.cancelButton.AutoSize = false;
            this.cancelButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.cancelButton.Enabled = false;
            this.cancelButton.Image = ((System.Drawing.Image)(resources.GetObject("cancelButton.Image")));
            this.cancelButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.ShowDropDownArrow = false;
            this.cancelButton.Size = new System.Drawing.Size(100, 40);
            this.cancelButton.Text = "Cancel";
            this.cancelButton.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.cancelButton.Visible = false;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1305, 878);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.splitContainer2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SQLCallstackResolver (http://aka.ms/sqlstack)";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion
        private System.Windows.Forms.OpenFileDialog genericOpenFileDlg;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TextBox callStackInput;
        private System.Windows.Forms.TextBox finalOutput;
        private System.Windows.Forms.CheckBox DLLrecurse;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox binaryPaths;
        private System.Windows.Forms.Button BinaryPathPicker;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.CheckBox FramesOnSingleLine;
        private System.Windows.Forms.CheckBox GroupXEvents;
        private System.Windows.Forms.Button LoadXELButton;
        private System.Windows.Forms.Button EnterBaseAddresses;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button selectSQLPDB;
        private System.Windows.Forms.Button PDBPathPicker;
        private System.Windows.Forms.CheckBox pdbRecurse;
        private System.Windows.Forms.TextBox pdbPaths;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox IncludeLineNumbers;
        private System.Windows.Forms.CheckBox includeOffsets;
        private System.Windows.Forms.CheckBox RelookupSource;
        private System.Windows.Forms.Button ResolveCallStackButton;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.CheckBox cachePDB;
        private System.Windows.Forms.ToolTip formToolTip;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button outputFilePathPicker;
        private System.Windows.Forms.TextBox outputFilePath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.SaveFileDialog genericSaveFileDlg;
        private System.Windows.Forms.ToolStripProgressBar progressBar;
        private System.Windows.Forms.ToolStripDropDownButton cancelButton;
        private System.Windows.Forms.CheckBox showInlineFrames;
    }
}
