using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// Gives a plain search <see cref="TextBox"/> the same look as the rest of the app: a rounded
    /// white field with a hairline border that turns accent-blue while typing, and a small
    /// magnifier. A WinForms TextBox cannot recolour or round its own border (FixedSingle is
    /// always the system's dark 1px line, which is what made every search box look like a leftover
    /// next to the cards), so the box is placed inside a painted host instead.
    ///
    /// <para>The TextBox itself is kept - same name, same events, same Text - so no screen's code
    /// changes. The host takes its place in the layout: same parent, same cell and span in a
    /// TableLayoutPanel, same position in a flow, same Dock / Anchor / Margin / Size.</para>
    /// </summary>
    public static class SearchHost
    {
        private const string Marker = "searchhost";

        /// <summary>Names the app uses for its search boxes; <see cref="UiTheme.Polish"/> wraps these.</summary>
        public static bool LooksLikeSearchBox(TextBox tb)
        {
            if (tb == null || tb.Multiline || tb.ReadOnly || string.IsNullOrEmpty(tb.Name)) return false;
            string n = tb.Name.TrimStart('_').ToLowerInvariant();
            return n.StartsWith("txtsearch") || n.StartsWith("search") || n.StartsWith("txtquery") || n.StartsWith("txtmonsearch");
        }

        public static bool IsWrapped(TextBox tb)
        {
            return tb != null && tb.Parent != null && tb.Parent.Tag is string && (string)tb.Parent.Tag == Marker;
        }

        /// <summary>Wraps <paramref name="tb"/> in a painted search field. Safe to call twice.</summary>
        public static void Wrap(TextBox tb)
        {
            Control parent = tb == null ? null : tb.Parent;
            if (parent == null || IsWrapped(tb) || parent is DataGridView) return;

            var host = new Field
            {
                Tag = Marker,
                Name = tb.Name + "_host",
                Size = tb.Size,
                Margin = tb.Margin,
                Anchor = tb.Anchor,
                Dock = tb.Dock,
                Location = tb.Location,
                MinimumSize = tb.MinimumSize,
                MaximumSize = tb.MaximumSize,
                TabStop = false
            };
            // Changing the border style and moving the box destroy and re-create its window, which drops
            // the cue banner (the greyed hint the screen set). Read it now, put it back once re-created.
            string cue = ReadCue(tb);
            // A TextBox sizes itself from its font, so an unstyled one is ~25px; keep the host at
            // least tall enough for the text plus a little air.
            tb.BorderStyle = BorderStyle.None;
            tb.Dock = DockStyle.None;
            int need = tb.PreferredHeight + 10;
            if (host.Dock == DockStyle.None && host.Height < need) host.Height = need;

            TableLayoutPanel tlp = parent as TableLayoutPanel;
            TableLayoutPanelCellPosition pos = default(TableLayoutPanelCellPosition);
            int colSpan = 1, rowSpan = 1;
            if (tlp != null)
            {
                pos = tlp.GetPositionFromControl(tb);
                colSpan = tlp.GetColumnSpan(tb);
                rowSpan = tlp.GetRowSpan(tb);
            }
            int index = parent.Controls.GetChildIndex(tb);
            int tabIndex = tb.TabIndex;

            parent.SuspendLayout();
            parent.Controls.Remove(tb);
            if (tlp != null)
            {
                tlp.Controls.Add(host, pos.Column, pos.Row);
                if (colSpan > 1) tlp.SetColumnSpan(host, colSpan);
                if (rowSpan > 1) tlp.SetRowSpan(host, rowSpan);
            }
            else
            {
                parent.Controls.Add(host);
                try { parent.Controls.SetChildIndex(host, index); } catch { }
            }
            host.TabIndex = tabIndex;
            host.Attach(tb);
            parent.ResumeLayout(true);
            if (!string.IsNullOrEmpty(cue)) SetCue(tb, cue);
        }

        /// <summary>Greyed hint shown inside an empty box (the Win32 cue banner), kept after the box is wrapped.</summary>
        public static void SetCue(TextBox tb, string cue)
        {
            if (tb == null) return;
            EventHandler apply = (s, e) => SendMessage(tb.Handle, 0x1501, (IntPtr)1, cue);
            if (tb.IsHandleCreated) apply(tb, EventArgs.Empty);
            tb.HandleCreated += apply;
        }

        private static string ReadCue(TextBox tb)
        {
            try
            {
                if (!tb.IsHandleCreated) return null;
                var sb = new System.Text.StringBuilder(256);
                SendMessageSb(tb.Handle, 0x1502, sb, sb.Capacity);   // EM_GETCUEBANNER
                return sb.Length == 0 ? null : sb.ToString();
            }
            catch { return null; }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, string l);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessage")]
        private static extern IntPtr SendMessageSb(IntPtr h, int msg, System.Text.StringBuilder w, int l);

        // ------------------------------------------------------------------ the painted host
        private sealed class Field : Panel
        {
            private TextBox _tb;
            private const int IconPad = 30, RightPad = 10, Radius = 8;

            public Field()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                BackColor = Color.Transparent;
            }

            public void Attach(TextBox tb)
            {
                _tb = tb;
                Controls.Add(tb);
                tb.BackColor = Color.White;
                Layout_();
                tb.Enter += (s, e) => Invalidate();
                tb.Leave += (s, e) => Invalidate();
                tb.TextChanged += (s, e) => Invalidate();
                SizeChanged += (s, e) => Layout_();
                Click += (s, e) => tb.Focus();
            }

            // The host's own Font is the form's; the box carries the real one.
            private void Layout_()
            {
                if (_tb == null) return;
                _tb.Left = IconPad;
                _tb.Width = Math.Max(10, Width - IconPad - RightPad);
                _tb.Top = Math.Max(0, (Height - _tb.Height) / 2);
                _tb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            }

            private Color Backdrop()
            {
                for (Control p = Parent; p != null; p = p.Parent)
                {
                    if (p is CardPanel) return ((CardPanel)p).CardColor;
                    if (p.BackColor.A == 255) return p.BackColor;
                }
                return UiTheme.PageBg;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(Backdrop());
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bool focus = _tb != null && _tb.Focused;
                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = Round(r, Radius))
                {
                    using (var fill = new SolidBrush(Color.White)) g.FillPath(fill, path);
                    using (var pen = new Pen(focus ? UiTheme.Accent : UiTheme.CardLine, focus ? 1.6f : 1f)) g.DrawPath(pen, path);
                }
                DrawMagnifier(g, new Rectangle(10, (Height - 14) / 2, 14, 14), focus ? UiTheme.Accent : UiTheme.Faint);
            }

            private static void DrawMagnifier(Graphics g, Rectangle r, Color c)
            {
                using (var pen = new Pen(c, 1.7f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawEllipse(pen, r.X, r.Y, r.Width - 5, r.Height - 5);
                    g.DrawLine(pen, r.X + r.Width - 5 + 1, r.Y + r.Height - 5 + 1, r.Right - 1, r.Bottom - 1);
                }
            }

            private static GraphicsPath Round(Rectangle r, int radius)
            {
                int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
                var p = new GraphicsPath();
                if (d <= 0) { p.AddRectangle(r); return p; }
                p.AddArc(r.X, r.Y, d, d, 180, 90);
                p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                p.CloseFigure();
                return p;
            }
        }
    }
}
