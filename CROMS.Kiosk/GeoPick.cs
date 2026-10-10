using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace CROMS.Kiosk
{
    /// <summary>
    /// The Philippine province / city-municipality lists, read from the same `provinces` and
    /// `municipalities` tables the staff app uses (loaded from the official PSGC by migration 29).
    /// Provinces are read once and kept; a province's municipalities are read the first time it is
    /// chosen. Never throws: when the database cannot be reached the lists come back empty and the
    /// caller falls back to plain typing, so a dropped connection cannot block a client's request.
    /// </summary>
    internal static class GeoData
    {
        private static List<string> _provinces;
        private static Dictionary<string, int> _provinceIds;
        private static readonly Dictionary<int, List<string>> _munis = new Dictionary<int, List<string>>();

        private static readonly object _provLock = new object();

        public static List<string> Provinces()
        {
            // Locked because Program warms this on a worker thread at start-up: a screen asking at
            // the same moment must wait for the full list, not read a half-filled one.
            lock (_provLock)
            {
                if (_provinces != null && _provinces.Count > 0) return _provinces;
                var list = new List<string>();
                var ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    DataTable t = Db.Pull("SELECT id, name FROM provinces ORDER BY name");
                    foreach (DataRow r in t.Rows)
                    {
                        string name = Convert.ToString(r["name"]);
                        list.Add(name);
                        ids[name] = Convert.ToInt32(r["id"]);
                    }
                }
                catch { /* offline: empty list, caller falls back to typing */ }
                _provinces = list;
                _provinceIds = ids;
                return _provinces;
            }
        }

        /// <summary>The id of an exactly-named province (case and accent insensitive), or 0.</summary>
        public static int ProvinceId(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;
            Provinces();
            string n = Normalize(name);
            foreach (var kv in _provinceIds) if (Normalize(kv.Key) == n) return kv.Value;
            return 0;
        }

        /// <summary>Only the cities and municipalities that belong to this province.</summary>
        public static List<string> Municipalities(int provinceId)
        {
            List<string> list;
            if (_munis.TryGetValue(provinceId, out list)) return list;
            list = new List<string>();
            try
            {
                DataTable t = Db.Pull("SELECT name FROM municipalities WHERE province_id = @p ORDER BY name",
                    new MySqlParameter("@p", provinceId));
                foreach (DataRow r in t.Rows) list.Add(Convert.ToString(r["name"]));
                _munis[provinceId] = list;   // only cache a real answer
            }
            catch { }
            return list;
        }

        /// <summary>Lower case with accents and punctuation folded away, so "penab" finds "Peñablanca".</summary>
        public static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string d = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            bool space = false;
            foreach (char c in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c)) { sb.Append(char.ToLowerInvariant(c)); space = false; }
                else if (!space && sb.Length > 0) { sb.Append(' '); space = true; }
            }
            return sb.ToString().Trim();
        }
    }

    /// <summary>
    /// Type-to-search for a ComboBox: as the client types, a short list of matches drops under the
    /// box (matching anywhere in the name, ignoring case and accents); tap or Enter to pick. Only
    /// the first few matches are ever drawn, so the size of the list does not matter. Picking sets
    /// the combo by index, so its SelectedIndexChanged still fires.
    /// </summary>
    internal static class SearchPick
    {
        private const int MaxShown = 8;
        private static readonly Dictionary<ComboBox, State> States = new Dictionary<ComboBox, State>();

        public static void Attach(ComboBox cb)
        {
            if (cb == null || States.ContainsKey(cb)) return;
            var st = new State(cb);
            States[cb] = st;
            cb.Disposed += (s, e) => { st.Close(); st.Drop?.Dispose(); States.Remove(cb); };
            st.Wire();
        }

        /// <summary>Call after the combo's items were replaced.</summary>
        public static void Refresh(ComboBox cb)
        {
            State st;
            if (cb != null && States.TryGetValue(cb, out st)) { st.Dirty = true; st.Close(); }
        }

        /// <summary>A list that never takes focus, so tapping a suggestion leaves the combo focused.</summary>
        private sealed class NoFocusListBox : ListBox
        {
            public NoFocusListBox() { SetStyle(ControlStyles.Selectable, false); }
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == 0x0021 /* WM_MOUSEACTIVATE */) { m.Result = (IntPtr)3 /* MA_NOACTIVATE */; return; }
                base.WndProc(ref m);
            }
        }

        private sealed class State
        {
            private readonly ComboBox _cb;
            public ToolStripDropDown Drop;
            public bool Dirty = true;
            private ListBox _list;
            private bool _suppress;
            private string[] _raw = new string[0];
            private string[] _norm = new string[0];

            public State(ComboBox cb) { _cb = cb; }

            public void Wire()
            {
                _cb.AutoCompleteMode = AutoCompleteMode.None;   // never the native list
                _cb.TextUpdate += (s, e) => OnTyped();
                _cb.KeyDown += OnKeyDown;
                _cb.Leave += (s, e) => { Close(); CommitTyped(); };
                _cb.DropDown += (s, e) => Close();               // the arrow shows the full list
                _cb.Resize += (s, e) => { if (Drop != null && Drop.Visible) Place(); };
            }

            private void OnTyped()
            {
                if (_suppress || !_cb.Enabled) return;
                string q = (_cb.Text ?? "").Trim();
                if (q.Length == 0) { Close(); return; }
                var hits = Search(q);
                if (hits.Count == 0) { Close(); return; }
                Show(hits);
            }

            private List<string> Search(string q)
            {
                if (Dirty || _raw.Length != _cb.Items.Count) Rebuild();
                string nq = GeoData.Normalize(q);
                var starts = new List<string>();
                var inside = new List<string>();
                for (int i = 0; i < _norm.Length; i++)
                {
                    int at = _norm[i].IndexOf(nq, StringComparison.Ordinal);
                    if (at == 0) starts.Add(_raw[i]);
                    else if (at > 0) inside.Add(_raw[i]);
                }
                return starts.Concat(inside).Take(MaxShown).ToList();
            }

            private void Rebuild()
            {
                var raw = new List<string>(_cb.Items.Count);
                foreach (object o in _cb.Items) raw.Add(_cb.GetItemText(o) ?? "");
                _raw = raw.ToArray();
                _norm = _raw.Select(GeoData.Normalize).ToArray();
                Dirty = false;
            }

            private void Show(List<string> hits)
            {
                EnsurePopup();
                _list.BeginUpdate();
                _list.Items.Clear();
                foreach (string h in hits) _list.Items.Add(h);
                _list.EndUpdate();
                _list.SelectedIndex = -1;
                _list.Height = hits.Count * _list.ItemHeight + 4;
                Place();
            }

            private void Place()
            {
                if (Drop == null || _cb.IsDisposed || !_cb.IsHandleCreated) return;
                _list.Width = Math.Max(_cb.Width, 200);
                Drop.Size = new Size(_list.Width + 2, _list.Height + 2);
                Drop.Show(_cb.PointToScreen(new Point(0, _cb.Height)));
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
                    ItemHeight = Math.Max(40, _cb.Font.Height + 14),   // finger-sized rows
                    BackColor = KioskCore.CardBg,
                    ForeColor = KioskCore.Ink,
                };
                _list.DrawItem += (s, e) =>
                {
                    if (e.Index < 0) return;
                    bool hot = (e.State & DrawItemState.Selected) != 0;
                    using (var b = new SolidBrush(hot ? KioskCore.CardSelBg : KioskCore.CardBg))
                        e.Graphics.FillRectangle(b, e.Bounds);
                    TextRenderer.DrawText(e.Graphics, Convert.ToString(_list.Items[e.Index]), _list.Font,
                        new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 14, e.Bounds.Height),
                        hot ? KioskCore.Accent : KioskCore.Ink,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
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
                    AutoClose = false,
                    Padding = new Padding(1),
                    BackColor = KioskCore.Line,
                    DropShadowEnabled = true,
                };
                host.Size = _list.Size;
                Drop.Items.Add(host);
            }

            public void Close() { if (Drop != null && Drop.Visible) Drop.Close(); }

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
                        if (_list.SelectedIndex >= 0) { Accept(Convert.ToString(_list.SelectedItem)); e.Handled = e.SuppressKeyPress = true; }
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
                    int idx = _cb.FindStringExact(value);
                    if (idx >= 0) { if (_cb.SelectedIndex != idx) _cb.SelectedIndex = idx; else _cb.Text = value; }
                    else _cb.Text = value;
                    _cb.SelectionStart = (_cb.Text ?? "").Length;
                }
                finally { _suppress = false; }
            }

            /// <summary>Typed text that equals a list entry (ignoring case/accents) becomes that
            /// selection on leaving the box, so the province -> city cascade fires even when the
            /// client never tapped a suggestion.</summary>
            private void CommitTyped()
            {
                string t = _cb.Text ?? "";
                if (t.Length == 0 || _cb.SelectedIndex >= 0 || !_cb.Enabled) return;
                string n = GeoData.Normalize(t);
                for (int i = 0; i < _cb.Items.Count; i++)
                {
                    if (GeoData.Normalize(_cb.GetItemText(_cb.Items[i])) != n) continue;
                    _suppress = true;
                    try { _cb.SelectedIndex = i; } finally { _suppress = false; }
                    return;
                }
            }
        }
    }
}
