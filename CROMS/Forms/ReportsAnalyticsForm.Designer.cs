using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Analytics;
using CROMS.Analytics.Tabs;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    public partial class ReportsAnalyticsForm
    {
        private TabControl _tabs;
        private Panel _psaHost;

        private void InitializeComponent()
        {
            Text = "Reports & Analytics";
            BackColor = UiTheme.PageBg;
            Font = new Font("Segoe UI", 9F);

            var title = new Label
            {
                Text = "Reports and Analytics",
                AutoSize = false,
                Location = new Point(30, 22),
                Size = new Size(600, 34),
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                UseMnemonic = false
            };

            var subtitle = new Label
            {
                Text = "Registry and operations analytics, plus the monthly PSA submission report. Read-only.",
                AutoSize = false,
                Location = new Point(32, 58),
                Size = new Size(900, 20),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = UiTheme.Muted,
                UseMnemonic = false
            };

            var header = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = UiTheme.PageBg };
            header.Controls.Add(subtitle);
            header.Controls.Add(title);

            _tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F),
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(160, 34),
                DrawMode = TabDrawMode.OwnerDrawFixed,
                Padding = new Point(0, 0)
            };
            _tabs.DrawItem += DrawTab;
            _tabs.SelectedIndexChanged += (s, e) => LoadSelected();

            AddAnalyticsTab("Birth", new BirthAnalyticsTab());
            AddAnalyticsTab("Death", new DeathAnalyticsTab());
            AddAnalyticsTab("Marriage", new MarriageAnalyticsTab());
            AddAnalyticsTab("Queuing", new QueuingAnalyticsTab());
            AddAnalyticsTab("Certificates", new CertificateAnalyticsTab());

            AddCollectionsTab();
            AddPsaTab();

            Controls.Add(_tabs);
            Controls.Add(header);

            Load += (s, e) => LoadSelected();
        }
    }
}
