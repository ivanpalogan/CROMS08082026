using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Forms
{
    /// <summary>Designer-side of <see cref="ServerSetupForm"/> (static layout; text/prefill set in code).</summary>
    public partial class ServerSetupForm
    {
        private System.ComponentModel.IContainer components = null;

        private Label title;
        private Label help;
        private Label lblHost;
        private Label lblPort;
        private TextBox _txtHost;
        private TextBox _txtPort;
        private Label _lblStatus;
        private Button _btnScan;
        private Button _btnTest;
        private Button _btnSave;
        private Button _btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.title = new Label();
            this.help = new Label();
            this.lblHost = new Label();
            this.lblPort = new Label();
            this._txtHost = new TextBox();
            this._txtPort = new TextBox();
            this._lblStatus = new Label();
            this._btnScan = new Button();
            this._btnTest = new Button();
            this._btnSave = new Button();
            this._btnCancel = new Button();
            this.SuspendLayout();
            //
            // title
            //
            this.title.AutoSize = true;
            this.title.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
            this.title.ForeColor = Color.FromArgb(33, 37, 41);
            this.title.Location = new Point(20, 18);
            this.title.Text = "Connect to the CROMS database";
            //
            // help
            //
            this.help.ForeColor = Color.FromArgb(90, 90, 90);
            this.help.Location = new Point(22, 52);
            this.help.Size = new Size(396, 44);
            //
            // lblHost
            //
            this.lblHost.AutoSize = true;
            this.lblHost.Location = new Point(22, 104);
            this.lblHost.Text = "Server IP address";
            //
            // _txtHost
            //
            this._txtHost.Font = new Font("Segoe UI", 11F);
            this._txtHost.Location = new Point(24, 126);
            this._txtHost.Size = new Size(260, 28);
            //
            // lblPort
            //
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new Point(300, 104);
            this.lblPort.Text = "Port";
            //
            // _txtPort
            //
            this._txtPort.Font = new Font("Segoe UI", 11F);
            this._txtPort.Location = new Point(302, 126);
            this._txtPort.Size = new Size(114, 28);
            //
            // _lblStatus
            //
            this._lblStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this._lblStatus.Location = new Point(24, 166);
            this._lblStatus.Size = new Size(392, 40);
            //
            // _btnScan
            //
            this._btnScan.BackColor = Color.FromArgb(13, 110, 253);
            this._btnScan.FlatAppearance.BorderSize = 0;
            this._btnScan.FlatStyle = FlatStyle.Flat;
            this._btnScan.ForeColor = Color.White;
            this._btnScan.Location = new Point(24, 210);
            this._btnScan.Size = new Size(392, 40);
            this._btnScan.Text = "🔍  Find Server Automatically";
            this._btnScan.UseVisualStyleBackColor = false;
            this._btnScan.Click += new System.EventHandler(this.btnScan_Click);
            //
            // _btnTest
            //
            this._btnTest.BackColor = Color.FromArgb(233, 236, 239);
            this._btnTest.FlatAppearance.BorderSize = 0;
            this._btnTest.FlatStyle = FlatStyle.Flat;
            this._btnTest.Location = new Point(24, 286);
            this._btnTest.Size = new Size(150, 40);
            this._btnTest.Text = "Test Connection";
            this._btnTest.UseVisualStyleBackColor = false;
            this._btnTest.Click += new System.EventHandler(this.btnTest_Click);
            //
            // _btnSave
            //
            this._btnSave.BackColor = Color.FromArgb(25, 135, 84);
            this._btnSave.FlatAppearance.BorderSize = 0;
            this._btnSave.FlatStyle = FlatStyle.Flat;
            this._btnSave.ForeColor = Color.White;
            this._btnSave.Location = new Point(184, 286);
            this._btnSave.Size = new Size(150, 40);
            this._btnSave.Text = "Save && Continue";
            this._btnSave.UseVisualStyleBackColor = false;
            this._btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // _btnCancel
            //
            this._btnCancel.BackColor = Color.FromArgb(233, 236, 239);
            this._btnCancel.DialogResult = DialogResult.Cancel;
            this._btnCancel.FlatAppearance.BorderSize = 0;
            this._btnCancel.FlatStyle = FlatStyle.Flat;
            this._btnCancel.Location = new Point(344, 286);
            this._btnCancel.Size = new Size(72, 40);
            this._btnCancel.Text = "Exit";
            this._btnCancel.UseVisualStyleBackColor = false;
            //
            // ServerSetupForm
            //
            this.BackColor = Color.White;
            this.ClientSize = new Size(440, 350);
            this.Font = new Font("Segoe UI", 9.75F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Connect to CROMS Server";
            this.Controls.Add(this.title);
            this.Controls.Add(this.help);
            this.Controls.Add(this.lblHost);
            this.Controls.Add(this._txtHost);
            this.Controls.Add(this.lblPort);
            this.Controls.Add(this._txtPort);
            this.Controls.Add(this._lblStatus);
            this.Controls.Add(this._btnScan);
            this.Controls.Add(this._btnTest);
            this.Controls.Add(this._btnSave);
            this.Controls.Add(this._btnCancel);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
