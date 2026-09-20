using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Forms
{
    internal partial class ZoomPrintPreviewForm
    {
        private readonly PrintPreviewControl _view = new PrintPreviewControl();
        private readonly Label _zoomLabel = new Label();

        private void InitializeComponent(string caption, string note)
        {
            Text = caption;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1000, 820);
            MinimumSize = new Size(720, 520);
            BackColor = UiTheme.PageBg;
            ShowIcon = false;
            KeyPreview = true;

            _view.Dock = DockStyle.Fill;
            _view.Document = _doc;
            _view.AutoZoom = false;
            _view.UseAntiAlias = true;
            _view.BackColor = Color.FromArgb(226, 230, 236);

            Controls.Add(_view);
            Controls.Add(BuildBar());
            if (!string.IsNullOrEmpty(note))
                Controls.Add(new Label
                {
                    Text = note, Dock = DockStyle.Top, AutoSize = false, Height = 20 + 16 * Math.Min(6, note.Split('\n').Length),
                    BackColor = Color.FromArgb(254, 243, 226), ForeColor = Color.FromArgb(146, 64, 14),
                    Padding = new Padding(12, 8, 12, 6), Font = new Font("Segoe UI", 9F), UseMnemonic = false
                });

            _wheel = new CtrlWheelZoom(_view, dir => SetZoom(CtrlWheelZoom.Next(_zoom, dir)));
            Shown += (s, e) => FitPage();
        }

        private Control BuildBar()
        {
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(10, 7, 10, 5), BackColor = UiTheme.Surface, WrapContents = false };
            bar.Paint += (s, e) => { using (var p = new Pen(UiTheme.CardLine)) e.Graphics.DrawLine(p, 0, bar.Height - 1, bar.Width, bar.Height - 1); };
            Func<string, int, EventHandler, Button> btn = (text, w, click) =>
            {
                var b = new Button { Text = text, Width = w, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = UiTheme.PageBg, ForeColor = UiTheme.Ink, UseMnemonic = false, Margin = new Padding(0, 0, 6, 0) };
                b.FlatAppearance.BorderSize = 0;
                b.Click += click;
                bar.Controls.Add(b);
                return b;
            };
            Button print = btn("Print...", 90, (s, e) => PrintNow());
            print.BackColor = UiTheme.Accent; print.ForeColor = Color.White;
            bar.Controls.Add(new Label { Width = 14 });
            btn("−", 34, (s, e) => SetZoom(CtrlWheelZoom.Next(_zoom, -1)));
            _zoomLabel.AutoSize = false; _zoomLabel.Width = 58; _zoomLabel.Height = 32; _zoomLabel.TextAlign = ContentAlignment.MiddleCenter;
            _zoomLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold); _zoomLabel.ForeColor = UiTheme.Ink; _zoomLabel.Margin = new Padding(0, 0, 6, 0);
            bar.Controls.Add(_zoomLabel);
            btn("+", 34, (s, e) => SetZoom(CtrlWheelZoom.Next(_zoom, 1)));
            btn("Fit page", 80, (s, e) => FitPage());
            btn("Fit width", 80, (s, e) => FitWidth());
            btn("100%", 56, (s, e) => SetZoom(100));
            if (_editForm != null)
            {
                bar.Controls.Add(new Label { Width = 14 });
                btn("Edit Layout...", 118, (s, e) => EditLayout());
            }
            bar.Controls.Add(new Label { Text = "Ctrl + mouse wheel to zoom", AutoSize = true, ForeColor = UiTheme.Muted, Margin = new Padding(14, 9, 0, 0) });
            return bar;
        }
    }
}
