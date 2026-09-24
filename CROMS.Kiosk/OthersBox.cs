using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// "Others, specify ______" — same helper as CROMS.Modules.OthersBox (the kiosk has no reference to CROMS.exe).
    /// <para/>
    /// A dropdown that offers "Others" and nothing else is a dead end: the encoder picks it,
    /// the record says "Others", and what the paper actually says is lost. These forms print
    /// the word "specify" next to the box for that reason, so the screen has to as well.
    /// <para/>
    /// WHAT GETS STORED, and why it is not just the typed text. The value is written as
    /// <c>Others - Traditional Birth Attendant</c>: the CATEGORY survives, so a PSA count of
    /// "attended by others" is still possible, and the DETAIL survives, so the certificate
    /// can be reprinted with what the paper says. Storing only the typed text would lose the
    /// category; storing only "Others" would lose the answer.
    /// <para/>
    /// The specify box is hidden — not merely blank — whenever the choice is anything else,
    /// for the same reason the marriage fields grey out on an unmarried birth: an empty box
    /// that does not apply reads as one somebody forgot.
    /// </summary>
    public static class OthersBox
    {
        /// <summary>The separator between the category and the specified detail.</summary>
        private const string Join = " - ";

        /// <summary>
        /// Show <paramref name="box"/> (and its caption, if any) only while
        /// <paramref name="combo"/> is on an "Others" choice, and clear it when it is not.
        /// Safe to call on a combo that has no Others entry — it simply never shows.
        /// </summary>
        public static void Bind(ComboBox combo, TextBox box, Control caption = null)
        {
            if (combo == null || box == null) return;
            EventHandler apply = (s, e) => Refresh(combo, box, caption);
            combo.SelectedIndexChanged += apply;
            combo.TextChanged += apply;
            Refresh(combo, box, caption);
        }

        private static void Refresh(ComboBox combo, TextBox box, Control caption)
        {
            bool others = IsOthers(combo.Text);
            if (!others && box.Text.Length > 0) box.Clear();
            box.Visible = others;
            if (caption != null) caption.Visible = others;
        }

        /// <summary>
        /// True for the several spellings a form and a master file use between them —
        /// "Others", "Other", "Others (specify)", "Other, specify".
        /// </summary>
        public static bool IsOthers(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   Regex.IsMatch(value.Trim(), @"^others?\b", RegexOptions.IgnoreCase);
        }

        /// <summary>The value to store: the choice, plus the specified detail when there is one.</summary>
        public static string Compose(ComboBox combo, TextBox box)
        {
            string choice = (combo == null ? "" : combo.Text ?? "").Trim();
            string detail = (box == null ? "" : box.Text ?? "").Trim();
            if (!IsOthers(choice) || detail.Length == 0) return choice;
            return choice + Join + detail;
        }

        /// <summary>
        /// Put a stored value back on screen, splitting "Others - detail" into the choice
        /// and the specify box.
        /// </summary>
        public static void Apply(ComboBox combo, TextBox box, Control caption, string stored,
                                 Action<ComboBox, string> select)
        {
            stored = (stored ?? "").Trim();
            string choice = stored, detail = "";

            if (IsOthers(stored))
            {
                int cut = stored.IndexOf(Join, StringComparison.Ordinal);
                if (cut > 0)
                {
                    choice = stored.Substring(0, cut).Trim();
                    detail = stored.Substring(cut + Join.Length).Trim();
                }
            }

            if (select != null) select(combo, choice);
            else if (combo != null) combo.Text = choice;

            if (box != null) box.Text = detail;
            Refresh(combo, box, caption);
        }

        // ================================================================ inline specify box
        // For dropdowns whose screen has no room set aside for a separate specify box: the
        // box appears IN THE COMBO'S OWN SPACE, beside it, when Others is chosen, and the combo
        // takes the full width back when anything else is. Nothing below it moves, so this can
        // be attached to any dropdown on any layout (TableLayoutPanel cell, docked, or placed).

        private sealed class InlineState
        {
            public ComboBox Combo;
            public TextBox Box;
            public Panel Host;
            public int MaxTotal;
            public bool Shown;   // tracked here: Control.Visible reads false while the form is unshown
        }

        private static readonly ConditionalWeakTable<ComboBox, InlineState> Inline =
            new ConditionalWeakTable<ComboBox, InlineState>();

        /// <summary>
        /// Give <paramref name="combo"/> an inline "Please specify" box that shows only while
        /// an Others choice is selected, and is cleared and hidden the moment it is not.
        /// <paramref name="maxTotal"/> is the column's width, so "Others - detail" always fits.
        /// Read the value back with <see cref="Value"/> and put a stored one with <see cref="SetValue"/>.
        /// </summary>
        public static TextBox AttachInline(ComboBox combo, int maxTotal = 0, bool borderless = false)
        {
            if (combo == null) return null;
            InlineState st;
            if (Inline.TryGetValue(combo, out st)) return st.Box;

            st = new InlineState
            {
                Combo = combo,
                MaxTotal = maxTotal,
                Box = new TextBox
                {
                    Visible = false,
                    Font = combo.Font,
                    BorderStyle = borderless ? BorderStyle.None : BorderStyle.FixedSingle
                }
            };
            Inline.Add(combo, st);
            SetCue(st.Box, "Please specify");

            if (combo.Parent != null) Wrap(st);
            else
            {
                EventHandler once = null;
                once = (s, e) =>
                {
                    if (combo.Parent == null || st.Host != null) return;
                    combo.ParentChanged -= once;
                    Wrap(st);
                };
                combo.ParentChanged += once;
            }

            EventHandler apply = (s, e) => RefreshInline(st);
            combo.SelectedIndexChanged += apply;
            combo.TextChanged += apply;
            combo.EnabledChanged += (s, e) => st.Box.Enabled = combo.Enabled;
            RefreshInline(st);
            return st.Box;
        }

        /// <summary>The value to store for a combo: "Others - detail" when specified, else its text.</summary>
        public static string Value(ComboBox combo)
        {
            if (combo == null) return "";
            InlineState st;
            if (!Inline.TryGetValue(combo, out st)) return (combo.Text ?? "").Trim();
            string v = Compose(combo, st.Box);
            if (st.MaxTotal > 0 && v.Length > st.MaxTotal) v = v.Substring(0, st.MaxTotal).TrimEnd();
            return v;
        }

        /// <summary>Put a stored value back, splitting "Others - detail" into the choice and the box.</summary>
        public static void SetValue(ComboBox combo, string stored)
        {
            if (combo == null) return;
            InlineState st;
            if (!Inline.TryGetValue(combo, out st)) { combo.Text = stored ?? ""; return; }
            Apply(combo, st.Box, null, stored, null);
            RefreshInline(st);
        }

        private static void Wrap(InlineState st)
        {
            ComboBox combo = st.Combo;
            Control parent = combo.Parent;
            var host = new Panel
            {
                BackColor = Color.Transparent,
                Margin = combo.Margin,
                Dock = combo.Dock,
                Anchor = combo.Anchor,
                TabIndex = combo.TabIndex,
                Bounds = combo.Bounds,
                MinimumSize = new Size(0, combo.Height)
            };
            st.Host = host;

            parent.SuspendLayout();
            var tlp = parent as TableLayoutPanel;
            if (tlp != null)
            {
                // Cell and span are lost with the control, so read them first.
                TableLayoutPanelCellPosition pos = tlp.GetPositionFromControl(combo);
                int cs = tlp.GetColumnSpan(combo), rs = tlp.GetRowSpan(combo);
                tlp.Controls.Remove(combo);
                tlp.Controls.Add(host, pos.Column, pos.Row);
                tlp.SetColumnSpan(host, cs);
                tlp.SetRowSpan(host, rs);
            }
            else
            {
                int index = parent.Controls.GetChildIndex(combo);
                parent.Controls.Remove(combo);
                parent.Controls.Add(host);
                parent.Controls.SetChildIndex(host, index);   // keeps dock order
            }

            combo.Dock = DockStyle.None;
            combo.Anchor = AnchorStyles.None;
            combo.Margin = Padding.Empty;
            combo.TabIndex = 0;
            st.Box.TabIndex = 1;
            host.Controls.Add(combo);
            host.Controls.Add(st.Box);
            host.Resize += (s, e) => LayoutInline(st);
            parent.ResumeLayout(true);
            LayoutInline(st);
        }

        private static void RefreshInline(InlineState st)
        {
            ComboBox combo = st.Combo;
            TextBox box = st.Box;
            bool others = IsOthers(combo.Text);
            if (!others && box.Text.Length > 0) box.Clear();
            if (st.MaxTotal > 0)
                box.MaxLength = Math.Max(1, st.MaxTotal - (combo.Text ?? "").Trim().Length - Join.Length);
            if (st.Shown == others) return;
            st.Shown = others;
            box.Visible = others;
            LayoutInline(st);
        }

        private static void LayoutInline(InlineState st)
        {
            if (st.Host == null) return;
            ComboBox combo = st.Combo;
            TextBox box = st.Box;
            int w = st.Host.ClientSize.Width, h = combo.Height;
            if (w <= 0) return;
            if (st.Shown)
            {
                int cw = Math.Max(90, (int)(w * 0.42));
                if (cw > w - 60) cw = Math.Max(40, w - 60);
                combo.SetBounds(0, 0, cw, h);
                int bx = cw + 6;
                box.SetBounds(bx, Math.Max(0, (h - box.Height) / 2), Math.Max(20, w - bx), box.Height);
            }
            else combo.SetBounds(0, 0, w, h);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private static void SetCue(TextBox box, string cue)
        {
            EventHandler set = (s, e) => SendMessage(box.Handle, 0x1501 /* EM_SETCUEBANNER */, (IntPtr)1, cue);
            if (box.IsHandleCreated) set(box, EventArgs.Empty);
            else box.HandleCreated += set;
        }
    }
}
