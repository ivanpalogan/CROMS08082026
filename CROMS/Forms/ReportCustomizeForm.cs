using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Analytics;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Lets the operator choose which of a Reports &amp; Analytics tab's charts print as extra
    /// pages on its Assessment Report. Default is every chart ON (<see cref="ReportWidgetPrefs"/>)
    /// — this screen is for turning OFF the ones the office does not want on the printout, not
    /// for opting in to something that was previously hidden.
    /// </summary>
    public partial class ReportCustomizeForm : Form
    {
        private readonly string _formCode;
        private readonly List<CheckBox> _boxes = new List<CheckBox>();

        public ReportCustomizeForm(string formCode, IList<AnalyticsWidget> widgets)
        {
            _formCode = formCode;
            InitializeComponent();

            int y = 88;
            if (widgets != null)
            {
                foreach (AnalyticsWidget w in widgets)
                {
                    var box = new CheckBox
                    {
                        Text = w.Title,
                        Location = new Point(24, y),
                        Size = new Size(380, 24),
                        Checked = ReportWidgetPrefs.IsEnabled(formCode, w.Title),
                        Tag = w.Title,
                        Font = new Font("Segoe UI", 9.5F),
                        ForeColor = UiTheme.Ink
                    };
                    _boxes.Add(box);
                    Controls.Add(box);
                    y += 28;
                }
            }

            if (_boxes.Count == 0)
            {
                Controls.Add(new Label
                {
                    Text = "This tab has no charts to customize.",
                    Location = new Point(24, y),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                    ForeColor = UiTheme.Muted
                });
                y += 28;
            }

            ClientSize = new Size(432, y + 60);

            var btnOk = new Button
            {
                Text = "Save",
                Size = new Size(100, 32),
                Location = new Point(ClientSize.Width - 220, y + 14),
                BackColor = UiTheme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.Click += (s, e) =>
            {
                foreach (CheckBox box in _boxes)
                    ReportWidgetPrefs.SetEnabled(_formCode, (string)box.Tag, box.Checked);
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 32),
                Location = new Point(ClientSize.Width - 110, y + 14),
                BackColor = UiTheme.Surface,
                ForeColor = UiTheme.Ink,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            UiTheme.Polish(this);
        }
    }
}
