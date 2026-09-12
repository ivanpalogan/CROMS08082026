using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Analytics
{
    /// <summary>
    /// What a widget shows INSTEAD of a chart when the data cannot support one — an unfilled
    /// source column, an empty range, or a query that failed.
    ///
    /// This is a deliberate feature, not a fallback nobody will see: the office's live
    /// database holds one marriage and three deaths, and several PSA columns
    /// (mother_age, weight_grams, attendant_type, disposal_method) are unfilled in almost
    /// every row. A bar chart over one record with a trend line through it would be a
    /// misleading screenshot; a panel that names the column and the count is an honest one,
    /// and it tells the office exactly what to start capturing.
    /// </summary>
    public class EmptyStatePanel : Panel
    {
        /// <summary>Why the panel is showing — changes only the glyph and tint, not the wording.</summary>
        public enum Kind
        {
            /// <summary>Source column is NULL/blank in every row in range.</summary>
            Unfilled,
            /// <summary>No rows at all in the selected range.</summary>
            NoRows,
            /// <summary>Rows exist but too few to draw the chart this widget renders.</summary>
            TooFew,
            /// <summary>The query itself failed (connection dropped, etc.).</summary>
            Error
        }

        private readonly Label _glyph;
        private readonly Label _message;
        private readonly Label _hint;
        private Kind _kind = Kind.NoRows;

        public EmptyStatePanel()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(252, 252, 254);

            _glyph = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 34,
                Text = "—",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = UiTheme.Faint,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _message = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 44,
                Text = "",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = UiTheme.Muted,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _hint = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 34,
                Text = "",
                Font = new Font("Segoe UI", 8.25F),
                ForeColor = UiTheme.Faint,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Docked top-to-bottom, so add in reverse of the order they should appear.
            Controls.Add(_hint);
            Controls.Add(_message);
            Controls.Add(_glyph);

            Paint += DrawFrame;
            Resize += (s, e) => Recentre();
        }

        /// <summary>
        /// Vertically centres the three docked labels inside whatever height the widget
        /// gives us, so the panel reads the same at full size and at Dashboard size.
        /// </summary>
        private void Recentre()
        {
            int block = _glyph.Height + _message.Height + _hint.Height;
            int pad = Math.Max(0, (Height - block) / 2);
            Padding = new Padding(8, pad, 8, 0);
        }

        private void DrawFrame(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(UiTheme.CardLine) { DashStyle = DashStyle.Dash })
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        /// <summary>Shows a message. <paramref name="hint"/> may be null.</summary>
        public void Show(Kind kind, string message, string hint)
        {
            _kind = kind;
            _message.Text = message ?? "";
            _hint.Text = hint ?? "";
            _hint.Visible = !string.IsNullOrEmpty(hint);

            switch (kind)
            {
                case Kind.Unfilled:
                    _glyph.Text = "▤";
                    _glyph.ForeColor = UiTheme.Faint;
                    break;
                case Kind.TooFew:
                    _glyph.Text = "▪";
                    _glyph.ForeColor = UiTheme.Faint;
                    break;
                case Kind.Error:
                    _glyph.Text = "!";
                    _glyph.ForeColor = UiTheme.Warning;
                    break;
                default:
                    _glyph.Text = "—";
                    _glyph.ForeColor = UiTheme.Faint;
                    break;
            }
            Recentre();
            Visible = true;
            BringToFront();
        }

        public Kind CurrentKind { get { return _kind; } }

        /// <summary>Compact placements hide the secondary hint line, which will not fit.</summary>
        public void SetCompact(bool compact)
        {
            _glyph.Height = compact ? 24 : 34;
            _message.Font = new Font("Segoe UI", compact ? 8.25F : 9.5F);
            _message.Height = compact ? 40 : 44;
            _hint.Visible = !compact && !string.IsNullOrEmpty(_hint.Text);
            Recentre();
        }
    }
}
