using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    internal partial class BreqsForm
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _search = new TextBox();
        private readonly CheckBox _showClosed = new CheckBox();
        private readonly Label _kRequested = new Label(), _kPaid = new Label(), _kAtPsa = new Label(), _kReady = new Label();
        private readonly Label _kRequestedSub = new Label(), _kPaidSub = new Label(), _kAtPsaSub = new Label(), _kReadySub = new Label();
        private readonly Panel _detail = new Panel();
        private readonly Timer _searchDelay = new Timer { Interval = 300 };

        private void InitializeComponent()
        {
            Text = "PSA Copies (BREQS)";
            BackColor = UiTheme.PageBg;
            ClientSize = new Size(1400, 900);
            AutoScrollMinSize = new Size(1100, 700);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(24, 18, 24, 18), BackColor = UiTheme.PageBg };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildKpis(), 0, 1);
            root.Controls.Add(BuildBody(), 0, 2);
            Controls.Add(root);

            _searchDelay.Tick += (s, e) => { _searchDelay.Stop(); LoadList(); };
            UiTheme.Polish(this);
            LoadList();
        }
    }
}
