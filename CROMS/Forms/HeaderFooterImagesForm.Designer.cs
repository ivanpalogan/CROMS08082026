using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    public partial class HeaderFooterImagesForm
    {
        private readonly Label _lblStatus = new Label();

        private void InitializeComponent(IEnumerable<ImageFieldSpec> fields)
        {
            Text = _formTitle + " - Header & Footer Images";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UiTheme.PageBg;

            var title = new Label
            {
                Text = _formTitle,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                AutoSize = true,
                Location = new Point(20, 16)
            };
            var sub = new Label
            {
                Text = "These images print in the header and footer of this form only. " +
                       "A slot left empty here uses the office-wide default (Settings) if one exists, " +
                       "or simply prints blank - it never stops the certificate from printing.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = UiTheme.Muted,
                AutoSize = false,
                MaximumSize = new Size(660, 0),
                Location = new Point(20, 44)
            };
            Controls.Add(title);
            Controls.Add(sub);

            int y = 80;
            var list = new List<ImageFieldSpec>(fields);
            foreach (ImageFieldSpec spec in list)
            {
                AddRow(spec.Kind, spec.Caption, y);
                y += RowHeight;
            }

            _lblStatus.SetBounds(20, y + 6, 560, 34);
            _lblStatus.Font = new Font("Segoe UI", 9f);
            _lblStatus.ForeColor = UiTheme.Muted;
            Controls.Add(_lblStatus);

            var close = new Button
            {
                Text = "Close",
                Location = new Point(600, y),
                Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            close.Click += (s, e) => Close();
            Controls.Add(close);

            ClientSize = new Size(710, y + 60);

            foreach (Row row in _rows) LoadRow(row);
            UiTheme.Polish(this);
        }
    }
}
