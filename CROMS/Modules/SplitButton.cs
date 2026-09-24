using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// One button carrying a main action plus a small menu of the actions that belong WITH
    /// it — the main label on the left, a divider, and a chevron on the right that drops the
    /// rest.
    /// <para/>
    /// It exists to stop related actions on one record spreading sideways across a toolbar.
    /// "Print Certificate" and "View Softcopy" are two things you do with the SAME saved
    /// record; as two equal-weight buttons they read as two unrelated choices and take twice
    /// the width. Folding the secondary one under the primary says which is the usual action
    /// without hiding the other.
    /// <para/>
    /// Tagged "noskin" so <see cref="UiTheme"/> leaves the painting alone — it owner-draws the
    /// same rounded chip UiTheme gives every other button, but has to split the surface in
    /// two, which UiTheme's painter has no concept of.
    /// </summary>
    public class SplitButton : Button
    {
        private const int ArrowZone = 30;   // width of the drop-down half
        private const int Radius = 8;       // matches UiTheme's button corner

        private bool _hover, _pressed, _overArrow, _menuOpen;

        public SplitButton()
        {
            Tag = "noskin";
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            BackColor = UiTheme.Chrome;
            ForeColor = UiTheme.Ink;
            Font = new Font("Segoe UI", UiTheme.MinTextPt, FontStyle.Bold);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer, true);
        }

        /// <summary>The menu the chevron opens. Its items are the secondary actions.</summary>
        public ContextMenuStrip Menu { get; set; }

        private Rectangle ArrowRect =>
            new Rectangle(Width - ArrowZone, 0, ArrowZone, Height);

        // ---- input ---------------------------------------------------------
        // The two halves must not both fire. Nothing is delegated to the base for a click
        // in the arrow zone, so ButtonBase never enters its pressed state and never raises
        // Click — which is what keeps "open the menu" from also printing a certificate.

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Menu != null && ArrowRect.Contains(e.Location))
            {
                ShowMenu();
                return;
            }
            _pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool wasPressed = _pressed;
            _pressed = false;
            Invalidate();
            if (wasPressed) base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool over = Menu != null && ArrowRect.Contains(e.Location);
            if (over != _overArrow) { _overArrow = over; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        { _hover = true; Invalidate(); base.OnMouseEnter(e); }

        protected override void OnMouseLeave(EventArgs e)
        { _hover = _pressed = _overArrow = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnEnabledChanged(EventArgs e)
        { Invalidate(); base.OnEnabledChanged(e); }

        /// <summary>Alt+Down opens the menu, the shortcut a drop-down is expected to have.</summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Alt | Keys.Down) && Focused && Menu != null)
            {
                ShowMenu();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public void ShowMenu()
        {
            if (Menu == null || !Enabled) return;
            _menuOpen = true;
            _pressed = false;
            Invalidate();
            Menu.Closed += MenuClosed;

            // Left-aligned under the button is the standard split-button default; flip to
            // right-aligned only when that would run the menu off the screen's right edge
            // (e.g. this button sitting near the edge of a maximized window).
            int menuWidth = Math.Max(Menu.PreferredSize.Width, Width);
            Point topLeft = PointToScreen(new Point(0, Height));
            Rectangle screen = Screen.FromControl(this).WorkingArea;
            int x = (topLeft.X + menuWidth > screen.Right) ? Width - menuWidth : 0;
            Menu.Show(this, new Point(x, Height));
        }

        private void MenuClosed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            Menu.Closed -= MenuClosed;
            _menuOpen = false;
            Invalidate();
        }

        // ---- paint ---------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Clearing to the first OPAQUE ancestor, not Parent.BackColor: a transparent
            // parent would otherwise paint a dark halo into the rounded corners. Same trap
            // UiTheme and the kiosk both had to fix.
            g.Clear(Backdrop());

            bool on = Enabled;
            Color fill = !on ? UiTheme.Mix(BackColor, UiTheme.PageBg, 0.55f)
                       : _pressed ? UiTheme.Mix(BackColor, UiTheme.Ink, 0.16f)
                       : (_hover || _menuOpen) ? UiTheme.Mix(BackColor, UiTheme.Ink, 0.07f)
                       : BackColor;
            Color ink = on ? ForeColor : UiTheme.Faint;

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = CardPanel.RoundedRect(r, Radius))
            using (var b = new SolidBrush(fill))
                g.FillPath(b, path);

            if (Menu == null)
            {
                TextRenderer.DrawText(g, Text, Font, r, ink,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                    | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                return;
            }

            // The arrow half only tints separately on hover: a permanently darker strip
            // reads as a disabled section rather than as a second target.
            if (on && _overArrow)
            {
                using (GraphicsPath clip = CardPanel.RoundedRect(r, Radius))
                using (var b = new SolidBrush(UiTheme.Mix(fill, UiTheme.Ink, 0.07f)))
                {
                    Region saved = g.Clip;
                    g.SetClip(clip, CombineMode.Intersect);
                    g.FillRectangle(b, ArrowRect);
                    g.Clip = saved;
                }
            }

            int split = Width - ArrowZone;
            using (var pen = new Pen(UiTheme.Mix(fill, UiTheme.Ink, on ? 0.18f : 0.08f)))
                g.DrawLine(pen, split, 6, split, Height - 7);

            TextRenderer.DrawText(g, Text, Font,
                new Rectangle(0, 0, split, Height), ink,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            DrawChevron(g, ArrowRect, ink);
        }

        private static void DrawChevron(Graphics g, Rectangle zone, Color ink)
        {
            float cx = zone.Left + zone.Width / 2f;
            float cy = zone.Top + zone.Height / 2f;
            using (var pen = new Pen(ink, 1.6f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                g.DrawLines(pen, new[]
                {
                    new PointF(cx - 3.5f, cy - 1.8f),
                    new PointF(cx,        cy + 2.0f),
                    new PointF(cx + 3.5f, cy - 1.8f),
                });
        }

        private Color Backdrop()
        {
            for (Control p = Parent; p != null; p = p.Parent)
                if (p.BackColor.A == 255) return p.BackColor;
            return UiTheme.PageBg;
        }
    }
}
