using System;
using System.Drawing;
using System.Windows.Forms;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Forms
{
    partial class CertificateViewerForm
    {
        private readonly CrystalReportViewer _viewer;
        private readonly Label _zoomLabel = new Label();

        private void InitializeComponent()
        {
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1000, 820);
            MinimumSize = new Size(720, 520);
            BackColor = UiTheme.PageBg;
            ShowIcon = false;
            KeyPreview = true;
        }

        private Control BuildZoomBar()
        {
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(10, 7, 10, 5),
                BackColor = UiTheme.Surface, WrapContents = false
            };
            bar.Paint += (s, e) => { using (var p = new Pen(UiTheme.CardLine)) e.Graphics.DrawLine(p, 0, 0, bar.Width, 0); };
            Func<string, int, EventHandler, Button> btn = (text, w, click) =>
            {
                var b = new Button { Text = text, Width = w, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = UiTheme.PageBg, ForeColor = UiTheme.Ink, UseMnemonic = false, Margin = new Padding(0, 0, 6, 0) };
                b.FlatAppearance.BorderSize = 0;
                b.Click += click;
                bar.Controls.Add(b);
                return b;
            };
            btn("−", 34, (s, e) => SetZoom(CtrlWheelZoom.Next(_zoom, -1)));
            _zoomLabel.AutoSize = false; _zoomLabel.Width = 58; _zoomLabel.Height = 30;
            _zoomLabel.TextAlign = ContentAlignment.MiddleCenter; _zoomLabel.ForeColor = UiTheme.Ink;
            _zoomLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold); _zoomLabel.Margin = new Padding(0, 0, 6, 0);
            bar.Controls.Add(_zoomLabel);
            btn("+", 34, (s, e) => SetZoom(CtrlWheelZoom.Next(_zoom, 1)));
            btn("Fit page", 80, (s, e) => FitPage());
            btn("Fit width", 80, (s, e) => FitWidth());
            btn("100%", 56, (s, e) => SetZoom(100));
            var hint = new Label
            {
                Text = "Ctrl + mouse wheel to zoom  ·  wheel to scroll  ·  Print and Export are on the toolbar above",
                AutoSize = true, ForeColor = UiTheme.Muted, Margin = new Padding(14, 8, 0, 0), UseMnemonic = false
            };
            bar.Controls.Add(hint);
            return bar;
        }

        private static Control BuildNote(string note)
        {
            return new Label
            {
                Text = note, Dock = DockStyle.Top, AutoSize = false, Height = 20 + 16 * Math.Min(6, note.Split('\n').Length),
                BackColor = Color.FromArgb(254, 243, 226), ForeColor = Color.FromArgb(146, 64, 14),
                Padding = new Padding(12, 8, 12, 6), Font = new Font("Segoe UI", 9F), UseMnemonic = false
            };
        }
    }
}
