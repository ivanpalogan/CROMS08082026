using System;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Analytics
{
    /// <summary>
    /// One flat summary tile in the strip across the top of an analytics tab: a caption,
    /// a number, and a comparison line. Built in code (no .Designer.cs) because it is a
    /// small fixed layout reused a couple of dozen times.
    ///
    /// The card carries its OWN period, stated on the tile, because the tab's date-range
    /// control filters the CHARTS only — a card that said "This month" while the range
    /// combo said "This Year" would be read as a bug in a screenshot.
    /// </summary>
    public class SummaryCard : UserControl
    {
        public const int CardWidth = 236;
        public const int CardHeight = 104;

        private readonly Panel _stripe;
        private readonly Label _caption;
        private readonly Label _value;
        private readonly Label _compare;

        public SummaryCard(string caption, Color accent)
        {
            Size = new Size(CardWidth, CardHeight);
            Margin = new Padding(0, 0, 12, 12);
            BackColor = UiTheme.Surface;
            Padding = new Padding(0);

            _stripe = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = accent
            };

            _caption = new Label
            {
                Text = caption,
                AutoSize = false,
                Location = new Point(16, 12),
                Size = new Size(CardWidth - 26, 32),
                Font = new Font("Segoe UI", 8.75F, FontStyle.Bold),
                ForeColor = UiTheme.Muted
            };

            _value = new Label
            {
                Text = "—",
                AutoSize = false,
                Location = new Point(15, 42),
                Size = new Size(CardWidth - 26, 32),
                Font = new Font("Segoe UI", 19F, FontStyle.Bold),
                ForeColor = UiTheme.Ink
            };

            _compare = new Label
            {
                Text = "",
                AutoSize = false,
                Location = new Point(16, 76),
                Size = new Size(CardWidth - 26, 22),
                Font = new Font("Segoe UI", 8F),
                ForeColor = UiTheme.Faint
            };

            Controls.Add(_caption);
            Controls.Add(_value);
            Controls.Add(_compare);
            Controls.Add(_stripe);

            Paint += DrawBorder;
        }

        /// <summary>Thin 1px card outline, matching the rest of the app's card surfaces.</summary>
        private void DrawBorder(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(UiTheme.CardLine))
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        /// <summary>Sets the headline number and the comparison line beneath it.</summary>
        public void Set(string value, string comparison)
        {
            _value.Text = value;
            _compare.Text = comparison ?? "";
            _compare.ForeColor = UiTheme.Faint;
        }

        /// <summary>
        /// Sets a number whose comparison line should read as good/bad. Used sparingly —
        /// "unclaimed over 30 days" going up is bad, but a birth count going up is neither.
        /// </summary>
        public void Set(string value, string comparison, Color comparisonColor)
        {
            _value.Text = value;
            _compare.Text = comparison ?? "";
            _compare.ForeColor = comparisonColor;
        }

        /// <summary>
        /// The card equivalent of a widget's empty state: show the number that IS known and
        /// say plainly that there is nothing to compare it against, rather than printing a
        /// percentage derived from a single period.
        /// </summary>
        public void SetNoComparison(string value, string why)
        {
            _value.Text = value;
            _compare.Text = why ?? "";
            _compare.ForeColor = UiTheme.Faint;
        }

        /// <summary>Shown when the card's own query failed — never leaves a stale number on screen.</summary>
        public void SetUnavailable(string why)
        {
            _value.Text = "—";
            _compare.Text = why ?? "Unavailable";
            _compare.ForeColor = UiTheme.Warning;
        }
    }
}
