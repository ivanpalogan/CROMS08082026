using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;

namespace CROMS.Modules
{
    /// <summary>
    /// Google-style search for a ComboBox: type and a short list of matches drops under the box,
    /// narrowing on every keystroke. Matches anywhere in the word (so "cag" finds "Tuguegarao,
    /// Cagayan"), ignoring case and accents. Pick with the mouse or Up/Down + Enter.
    /// <para/>
    /// Replaces the native <c>AutoCompleteSource.ListItems</c> hook. That one hands the WHOLE item
    /// list to Windows, which cannot cope with a list the size of the PSGC barangay table
    /// ("Too many items in the combo box"). Here the list is filtered in our own code and only the
    /// first <see cref="MaxShown"/> matches are ever drawn, so list size no longer matters.
    /// <para/>
    /// The list searched is the combo's own items (the master-file "library" behind that field),
    /// and when a Learning Library category is registered through <see cref="UseLibrary"/> the
    /// library's own ranked values are offered too, so a value learned from an earlier record
    /// shows up even if the master file never had it.
    /// </summary>
    public static class SearchCombo
    {
        /// <summary>Most matches shown at once.</summary>
        private const int MaxShown = 10;

        private static readonly Dictionary<ComboBox, State> _states = new Dictionary<ComboBox, State>();

        /// <summary>Make a combo searchable. Safe to call more than once.</summary>
        public static void Attach(ComboBox cb)
        {
            if (cb == null || _states.ContainsKey(cb)) return;
            var st = new State(cb);
            _states[cb] = st;
            cb.Disposed += (s, e) => { st.Close(); st.Drop?.Dispose(); _states.Remove(cb); };
            st.Wire();
        }

        /// <summary>Also offer the Learning Library's values for <paramref name="category"/>.</summary>
        public static void UseLibrary(ComboBox cb, string category)
        {
            if (cb == null) return;
            Attach(cb);
            State st;
            if (_states.TryGetValue(cb, out st)) st.Category = category;
        }

        /// <summary>True when the combo was made searchable by <see cref="Attach"/>.</summary>
        public static bool IsAttached(ComboBox cb) { return cb != null && _states.ContainsKey(cb); }

        /// <summary>A list that never takes keyboard focus, so clicking a suggestion leaves the
        /// combo focused (otherwise the combo's Leave would close the popup before the click lands).</summary>
        private sealed class NoFocusListBox : ListBox
        {
            private const int WM_MOUSEACTIVATE = 0x0021;
            private const int MA_NOACTIVATE = 3;

            public NoFocusListBox() { SetStyle(ControlStyles.Selectable, false); }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_MOUSEACTIVATE) { m.Result = (IntPtr)MA_NOACTIVATE; return; }
                base.WndProc(ref m);
            }
        }

        // ------------------------------------------------------------------
        private sealed class State
        {
            private readonly ComboBox _cb;
            public string Category;
            public ToolStripDropDown Drop;
            private ListBox _list;
            private bool _suppress;        // our own text writes must not reopen the popup
            private string[] _raw = new string[0];
            private string[] _norm = new string[0];
            private int _cachedCount = -1;

            public State(ComboBox cb) { _cb = cb; }

            public void Wire()
            {
                _cb.AutoCompleteMode = AutoCompleteMode.None;   // never the native list
                _cb.TextUpdate += (s, e) => OnTyped();
                _cb.KeyDown += OnKeyDown;
                _cb.Leave += (s, e) => { Close(); CommitTyped(); };
                _cb.DropDown += (s, e) => Close();               // the arrow shows the plain list
                _cb.Resize += (s, e) => { if (Drop != null && Drop.Visible) Place(); };
                _cb.DataSourceChanged += (s, e) => _cachedCount = -1;
                _cb.Enter += (s, e) => _cachedCount = -1;        // lists are refilled while unfocused
            }

            // ----- typing ----------------------------------------------------
            private void OnTyped()
            {
                if (_suppress) return;
                string q = (_cb.Text ?? "").Trim();
                if (q.Length == 0) { Close(); return; }
                var hits = Search(q);
                if (hits.Count == 0) { Close(); return; }
                Show(hits);
            }

            private List<string> Search(string q)
            {
                if (_cachedCount != _cb.Items.Count) Rebuild();
                string nq = LearningLibrary.Normalize(q);
                var starts = new List<string>();
                var inside = new List<string>();
                for (int i = 0; i < _norm.Length; i++)
                {
                    if (_raw[i].Length == 0) continue;
                    int at = _norm[i].IndexOf(nq, StringComparison.Ordinal);
                    if (at == 0) starts.Add(_raw[i]);
                    else if (at > 0) inside.Add(_raw[i]);
                    if (starts.Count >= MaxShown) break;
                }
                var hits = starts.Concat(inside).Take(MaxShown).ToList();

                // Library values the master list does not carry (only worth a query once the
                // operator has typed enough to be specific).
                if (!string.IsNullOrEmpty(Category) && hits.Count < MaxShown && nq.Length >= 2)
                {
                    foreach (string v in LearningLibrary.Suggest(Category, q, MaxShown))
                        if (hits.Count < MaxShown &&
                            !hits.Any(h => string.Equals(h, v, StringComparison.OrdinalIgnoreCase)))
                            hits.Add(v);
                }
                return hits;
            }

            private void Rebuild()
            {
                var raw = new List<string>(_cb.Items.Count);
                foreach (object o in _cb.Items) raw.Add(_cb.GetItemText(o) ?? "");
                _raw = raw.ToArray();
                _norm = _raw.Select(LearningLibrary.Normalize).ToArray();
                _cachedCount = _cb.Items.Count;
            }

            // ----- popup -----------------------------------------------------
            private void Show(List<string> hits)
            {
                EnsurePopup();
                _list.BeginUpdate();
                _list.Items.Clear();
                foreach (string h in hits) _list.Items.Add(h);
                _list.EndUpdate();
                _list.SelectedIndex = -1;
                _list.Height = Math.Min(hits.Count, MaxShown) * _list.ItemHeight + 4;
                Place();
            }

            private void Place()
            {
                if (Drop == null || _cb.IsDisposed || !_cb.IsHandleCreated) return;
                _list.Width = Math.Max(_cb.Width, 160);
                Drop.Size = new Size(_list.Width + 2, _list.Height + 2);
                Point at = _cb.PointToScreen(new Point(0, _cb.Height));
                Drop.Show(at);
            }

            private void EnsurePopup()
            {
                if (Drop != null) return;
                _list = new NoFocusListBox
                {
                    BorderStyle = BorderStyle.None,
                    IntegralHeight = false,
                    Font = _cb.Font,
                    TabStop = false,
                    DrawMode = DrawMode.OwnerDrawFixed,
                    ItemHeight = Math.Max(20, _cb.Font.Height + 6),
                    BackColor = UiTheme.Surface,
                    ForeColor = UiTheme.Ink
                };
                _list.DrawItem += (s, e) =>
                {
                    if (e.Index < 0) return;
                    bool hot = (e.State & DrawItemState.Selected) != 0;
                    using (var b = new SolidBrush(hot ? UiTheme.AccentTint : UiTheme.Surface))
                        e.Graphics.FillRectangle(b, e.Bounds);
                    TextRenderer.DrawText(e.Graphics, Convert.ToString(_list.Items[e.Index]), _list.Font,
                        new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height),
                        hot ? UiTheme.Accent : UiTheme.Ink,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis |
                        TextFormatFlags.NoPrefix);
                };
                _list.MouseMove += (s, e) =>
                {
                    int i = _list.IndexFromPoint(e.Location);
                    if (i >= 0 && i != _list.SelectedIndex) _list.SelectedIndex = i;
                };
                _list.MouseUp += (s, e) =>
                {
                    int i = _list.IndexFromPoint(e.Location);
                    if (i >= 0) Accept(Convert.ToString(_list.Items[i]));
                };

                var host = new ToolStripControlHost(_list) { Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false };
                Drop = new ToolStripDropDown
                {
                    AutoClose = false,          // we close it ourselves; AutoClose would steal focus handling
                    Padding = new Padding(1),
                    BackColor = UiTheme.CardLine,
                    DropShadowEnabled = true
                };
                host.Size = _list.Size;
                Drop.Items.Add(host);
            }

            public void Close()
            {
                if (Drop != null && Drop.Visible) Drop.Close();
            }

            // ----- choosing --------------------------------------------------
            private void OnKeyDown(object sender, KeyEventArgs e)
            {
                bool open = Drop != null && Drop.Visible && _list.Items.Count > 0;
                if (!open) return;
                switch (e.KeyCode)
                {
                    case Keys.Down:
                        _list.SelectedIndex = Math.Min(_list.SelectedIndex + 1, _list.Items.Count - 1);
                        e.Handled = e.SuppressKeyPress = true; break;
                    case Keys.Up:
                        _list.SelectedIndex = Math.Max(_list.SelectedIndex - 1, 0);
                        e.Handled = e.SuppressKeyPress = true; break;
                    case Keys.Enter:
                        if (_list.SelectedIndex >= 0)
                        {
                            Accept(Convert.ToString(_list.SelectedItem));
                            e.Handled = e.SuppressKeyPress = true;
                        }
                        else Close();
                        break;
                    case Keys.Escape:
                        Close(); e.Handled = e.SuppressKeyPress = true; break;
                }
            }

            private void Accept(string value)
            {
                Close();
                _suppress = true;
                try
                {
                    // Selecting by index (when the list has it) fires SelectedIndexChanged, which is
                    // what drives the province -> municipality -> barangay cascade.
                    int idx = _cb.FindStringExact(value);
                    if (idx >= 0) { if (_cb.SelectedIndex != idx) _cb.SelectedIndex = idx; else _cb.Text = value; }
                    else _cb.Text = value;
                    _cb.SelectionStart = (_cb.Text ?? "").Length;
                }
                finally { _suppress = false; }
            }

            /// <summary>Typed text that exactly equals a list entry becomes that selection on leaving
            /// the box, so a cascade fires even when the operator never opened the popup.</summary>
            private void CommitTyped()
            {
                string t = _cb.Text ?? "";
                if (t.Length == 0 || _cb.SelectedIndex >= 0) return;
                int i = _cb.FindStringExact(t);
                if (i < 0) return;
                _suppress = true;
                try { _cb.SelectedIndex = i; } finally { _suppress = false; }
            }
        }
    }
}
