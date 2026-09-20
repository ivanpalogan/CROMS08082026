using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    internal sealed partial class MarriageRecordForm
    {
        private readonly Panel _left = new Panel(), _right = new Panel();
        private readonly DataGridView _copies = new DataGridView();

        private void InitializeComponent()
        {
            Text = "Marriage Record";
            StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(1120, 780); MinimumSize = new Size(980, 640);
            BackColor = UiTheme.PageBg; ShowInTaskbar = false;
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16), BackColor = UiTheme.PageBg };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            _left.Dock = DockStyle.Fill; _left.AutoScroll = true; _left.Padding = new Padding(0, 0, 10, 0);
            _right.Dock = DockStyle.Fill; _right.AutoScroll = true; _right.Padding = new Padding(10, 0, 0, 0);
            grid.Controls.Add(_left, 0, 0); grid.Controls.Add(_right, 1, 0);
            var foot = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(16, 11, 16, 8), BackColor = UiTheme.Surface };
            foot.Controls.Add(Btn("View Form 97", (s, e) => { using (var f = new MarriageEntryForm(_id)) f.ShowDialog(this); Rebuild(); }));
            foot.Controls.Add(Btn("View License", (s, e) => OpenLicense()));
            foot.Controls.Add(Btn("View Scan", (s, e) => ViewScan()));
            foot.Controls.Add(Btn("Print / Preview", (s, e) => CertificateReport.ShowFor(DocKind.Marriage, _id, this)));
            foot.Controls.Add(Btn("Facts Certification (Form 3A)", (s, e) => { using (var f = new Form3ACertForm(_id)) f.ShowDialog(this); }));
            foot.Controls.Add(Btn("Certified Copy Workflow", (s, e) => CertifiedCopy()));
            foot.Controls.Add(Btn("View History", (s, e) => MUi.HistoryDialog(this, "Marriage", _id, "History - marriage record")));
            var close = MUi.Btn("Close", MUi.Kind.Ghost, 90); close.Click += (s, e) => Close(); foot.Controls.Add(close);
            Controls.Add(grid); Controls.Add(foot);
            Rebuild();
        }

        private static Button Btn(string text, EventHandler h)
        {
            var b = MUi.Btn(text, MUi.Kind.Secondary); b.Click += h; return b;
        }
    }
}
