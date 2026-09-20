using System.Drawing;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Forms
{
    partial class ProfileViewForm
    {
        private void InitializeComponent()
        {
            Text = "My Profile";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false; MaximizeBox = false;
            BackColor = UiTheme.PageBg;
            ClientSize = new Size(360, 460);
        }
    }
}
