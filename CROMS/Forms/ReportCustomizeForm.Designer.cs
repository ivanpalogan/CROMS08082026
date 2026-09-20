using System.Drawing;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Forms
{
    partial class ReportCustomizeForm
    {
        private void InitializeComponent()
        {
            Text = "Customize Report";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F);

            Controls.Add(new Label
            {
                Text = "Charts on the printed report",
                Location = new Point(20, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = UiTheme.Ink
            });
            Controls.Add(new Label
            {
                Text = "Choose which charts print as extra pages when you press " +
                       "\"Print Assessment Report\".\r\nThe monthly count table always prints.",
                Location = new Point(22, 44),
                Size = new Size(390, 34),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = UiTheme.Muted
            });
        }
    }
}
