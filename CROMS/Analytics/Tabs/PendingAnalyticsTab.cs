using System;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Analytics.Tabs
{
    /// <summary>
    /// Stands in for a domain tab that has not been built yet, and says so plainly.
    ///
    /// The module is being delivered tab by tab (Birth first, as the reference
    /// implementation), and an empty tab with no explanation reads as a broken screen.
    /// This one names what will go there, so the tab strip is honest about its own state.
    /// </summary>
    public class PendingAnalyticsTab : UserControl
    {
        public PendingAnalyticsTab(string domain, string[] plannedCharts)
        {
            Dock = DockStyle.Fill;
            BackColor = UiTheme.PageBg;
            Padding = new Padding(28, 26, 28, 20);

            var body = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = UiTheme.Muted,
                Text = BuildText(plannedCharts)
            };

            var head = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 56,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                Text = domain + " analytics — not built yet"
            };

            Controls.Add(body);
            Controls.Add(head);
        }

        private static string BuildText(string[] plannedCharts)
        {
            string s = "This tab is part of the same module and follows the Birth tab's pattern. " +
                       "It is left empty rather than half-filled so nothing on screen is mistaken " +
                       "for a real figure.\r\n\r\nPlanned here:\r\n";
            if (plannedCharts != null)
                foreach (string c in plannedCharts)
                    s += "     •  " + c + "\r\n";
            return s;
        }
    }
}
