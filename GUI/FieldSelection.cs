// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    public partial class FieldSelection : Form {
        public List<string> AllActions = new();
        public List<string> AllFields = new();
        public List<string> SelectedEventItems = new();
        private int newItemOrdinal = 1;
        public FieldSelection() {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, EventArgs e) {
            this.SelectedEventItems = listViewActionsFields.Items.Cast<ListViewItem>().Where(f => f.Checked).Select(f => f.Text).ToList();
        }

        private void ListSelection_Load(object sender, EventArgs e) {
            var grpActions = listViewActionsFields.Groups.Add("Actions", "Actions");
            var grpFields = listViewActionsFields.Groups.Add("Fields", "Fields");
            listViewActionsFields.Items.AddRange(this.AllActions.Select(a => new ListViewItem() { Text = a, Group = grpActions, Checked = IsCallStackName(a) }).ToArray());
            listViewActionsFields.Items.AddRange(this.AllFields.Select(f => new ListViewItem() { Text = f, Group = grpFields, Checked = IsCallStackName(f) }).ToArray());
            listViewActionsFields.Items[0].Selected = true;
        }

        private bool IsCallStackName(string a) {
            return (new List<string>() { "callstack", "call_stack", "stack_frames" }).Where(v => a.StartsWith(v)).Any();
        }

        private void AddItemButton_Click(object sender, EventArgs e) {
            if (listViewActionsFields.SelectedItems.Count > 0) {
                var selectedGroup = listViewActionsFields.SelectedItems[0].Group;
                listViewActionsFields.SelectedItems.Clear();
                var newItem = listViewActionsFields.Items.Add(new ListViewItem() { Text = $"New item {newItemOrdinal}", Group = selectedGroup });
                newItemOrdinal++;
                newItem.Selected = true;
                listViewActionsFields.EnsureVisible(newItem.Index);
                newItem.BeginEdit();
            }
        }

        private void DelItemButton_Click(object sender, EventArgs e) {
            if (listViewActionsFields.SelectedItems.Count > 0) foreach (ListViewItem item in listViewActionsFields.SelectedItems) {
                    listViewActionsFields.Items.Remove(item);
                }
        }
    }
}
