// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    partial class FieldSelection {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        #region Windows Form Designer generated code
        private void InitializeComponent() {
            System.Windows.Forms.ColumnHeader DataItem;
            this.OKButton = new System.Windows.Forms.Button();
            this.listViewActionsFields = new System.Windows.Forms.ListView();
            this.label2 = new System.Windows.Forms.Label();
            this.AddItemButton = new System.Windows.Forms.Button();
            this.DelItemButton = new System.Windows.Forms.Button();
            DataItem = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            DataItem.Width = 400;
            this.OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKButton.Location = new System.Drawing.Point(125, 338);
            this.OKButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(45, 30);
            this.OKButton.TabIndex = 1;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            this.listViewActionsFields.AutoArrange = false;
            this.listViewActionsFields.CheckBoxes = true;
            this.listViewActionsFields.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            DataItem});
            this.listViewActionsFields.FullRowSelect = true;
            this.listViewActionsFields.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listViewActionsFields.HideSelection = false;
            this.listViewActionsFields.LabelEdit = true;
            this.listViewActionsFields.Location = new System.Drawing.Point(9, 46);
            this.listViewActionsFields.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.listViewActionsFields.Name = "listViewActionsFields";
            this.listViewActionsFields.Size = new System.Drawing.Size(278, 288);
            this.listViewActionsFields.TabIndex = 0;
            this.listViewActionsFields.UseCompatibleStateImageBehavior = false;
            this.listViewActionsFields.View = System.Windows.Forms.View.Details;
            this.label2.Location = new System.Drawing.Point(10, 8);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(278, 35);
            this.label2.TabIndex = 3;
            this.label2.Text = "Select the XEvent actions / fields to import (by default, callstack action / fiel" +
    "d names are automatically selected):";
            this.AddItemButton.Location = new System.Drawing.Point(209, 338);
            this.AddItemButton.Name = "AddItemButton";
            this.AddItemButton.Size = new System.Drawing.Size(38, 23);
            this.AddItemButton.TabIndex = 2;
            this.AddItemButton.Text = "Add";
            this.AddItemButton.UseVisualStyleBackColor = true;
            this.AddItemButton.Click += new System.EventHandler(this.AddItemButton_Click);
            this.DelItemButton.Location = new System.Drawing.Point(249, 338);
            this.DelItemButton.Name = "DelItemButton";
            this.DelItemButton.Size = new System.Drawing.Size(38, 23);
            this.DelItemButton.TabIndex = 3;
            this.DelItemButton.Text = "Del";
            this.DelItemButton.UseVisualStyleBackColor = true;
            this.DelItemButton.Click += new System.EventHandler(this.DelItemButton_Click);
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(296, 379);
            this.Controls.Add(this.DelItemButton);
            this.Controls.Add(this.AddItemButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.listViewActionsFields);
            this.Controls.Add(this.OKButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FieldSelection";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Select items";
            this.Load += new System.EventHandler(this.ListSelection_Load);
            this.ResumeLayout(false);
        }
        #endregion
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.ListView listViewActionsFields;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button AddItemButton;
        private System.Windows.Forms.Button DelItemButton;
    }
}
