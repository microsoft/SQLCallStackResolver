// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    public partial class FindBar : UserControl {
        private TextBox _targetTextBox;
        private List<int> _matchPositions = new List<int>();
        private int _currentMatchIndex = -1;

        public FindBar() {
            InitializeComponent();
        }

        /// <summary>Attach this find bar to a target TextBox for searching.</summary>
        public void Attach(TextBox target) {
            _targetTextBox = target;
        }

        /// <summary>Show the find bar and focus the search box.</summary>
        public void Open() {
            Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() => {
                SearchBox.Focus();
                Keyboard.Focus(SearchBox);
                SearchBox.SelectAll();
            }));
        }

        /// <summary>Hide the find bar and clear state.</summary>
        public void Close() {
            Visibility = Visibility.Collapsed;
            _matchPositions.Clear();
            _currentMatchIndex = -1;
            MatchInfo.Text = "";
            SearchBox.Text = "";
        }

        private void Close_Click(object sender, RoutedEventArgs e) {
            Close();
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() => {
                if (_targetTextBox != null) {
                    _targetTextBox.Focus();
                    Keyboard.Focus(_targetTextBox);
                }
            }));
        }

        public bool IsOpen => Visibility == Visibility.Visible;

        private void FindMatches() {
            _matchPositions.Clear();
            _currentMatchIndex = -1;
            var searchText = SearchBox.Text;
            if (_targetTextBox == null || string.IsNullOrEmpty(searchText)) {
                MatchInfo.Text = "";
                return;
            }

            var content = _targetTextBox.Text ?? "";
            int pos = 0;
            while ((pos = content.IndexOf(searchText, pos, StringComparison.OrdinalIgnoreCase)) >= 0) {
                _matchPositions.Add(pos);
                pos += searchText.Length;
            }

            if (_matchPositions.Count > 0) {
                MatchInfo.Text = $"{_matchPositions.Count} matches";
            } else {
                MatchInfo.Text = "No matches";
            }
        }

        private void HighlightCurrent() {
            if (_targetTextBox == null || _currentMatchIndex < 0 || _currentMatchIndex >= _matchPositions.Count) return;
            var pos = _matchPositions[_currentMatchIndex];
            var len = SearchBox.Text.Length;

            // Focus the target briefly to make Select() work, then scroll and return focus
            _targetTextBox.Focus();
            _targetTextBox.Select(pos, len);
            var lineIndex = _targetTextBox.GetLineIndexFromCharacterIndex(pos);
            _targetTextBox.ScrollToLine(lineIndex);
            SearchBox.Focus();
            MatchInfo.Text = $"{_currentMatchIndex + 1} of {_matchPositions.Count}";
        }

        private void Next_Click(object sender, RoutedEventArgs e) => FindNext();
        private void Prev_Click(object sender, RoutedEventArgs e) => FindPrevious();

        public void FindNext() {
            if (_matchPositions.Count == 0) FindMatches();
            if (_matchPositions.Count == 0) return;
            _currentMatchIndex = (_currentMatchIndex + 1) % _matchPositions.Count;
            HighlightCurrent();
        }

        public void FindPrevious() {
            if (_matchPositions.Count == 0) FindMatches();
            if (_matchPositions.Count == 0) return;
            _currentMatchIndex = (_currentMatchIndex - 1 + _matchPositions.Count) % _matchPositions.Count;
            HighlightCurrent();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) {
            // Clear stale matches when search text changes; new matches built on Enter/nav
            _matchPositions.Clear();
            _currentMatchIndex = -1;
            MatchInfo.Text = "";
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter || e.Key == Key.F3) {
                if (_matchPositions.Count == 0) FindMatches();
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                    FindPrevious();
                else
                    FindNext();
                e.Handled = true;
            } else if (e.Key == Key.Escape) {
                Close();
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() => {
                    if (_targetTextBox != null) {
                        _targetTextBox.Focus();
                        Keyboard.Focus(_targetTextBox);
                    }
                }));
                e.Handled = true;
            }
        }
    }
}
