using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Forms
{
    /// <summary>Designer-side of <see cref="MobileCaptureSetupForm"/> (static layout; text is set in code).</summary>
    public partial class MobileCaptureSetupForm
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblTitle;
        private Label lblHelp;
        private Label lblHost;
        private TextBox txtHost;
        private Label lblSuffix;
        private Label lblToken;
        private TextBox txtToken;
        private Label lblTokenState;
        private Label lblStatus;
        private ProgressBar progress;
        private Button btnTest;
        private Button btnSave;
        private Button btnClose;
        private Timer pollTimer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblTitle = new Label();
            this.lblHelp = new Label();
            this.lblHost = new Label();
            this.txtHost = new TextBox();
            this.lblSuffix = new Label();
            this.lblToken = new Label();
            this.txtToken = new TextBox();
            this.lblTokenState = new Label();
            this.lblStatus = new Label();
            this.progress = new ProgressBar();
            this.btnTest = new Button();
            this.btnSave = new Button();
            this.btnClose = new Button();
            this.pollTimer = new Timer(this.components);
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = false;
            this.lblTitle.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
            this.lblTitle.Location = new Point(24, 18);
            this.lblTitle.Size = new Size(492, 30);
            this.lblTitle.Text = "Mobile Capture Setup";
            this.lblTitle.UseMnemonic = false;
            //
            // lblHelp
            //
            this.lblHelp.AutoSize = false;
            this.lblHelp.Location = new Point(26, 54);
            this.lblHelp.Size = new Size(490, 84);
            this.lblHelp.UseMnemonic = false;
            //
            // lblHost
            //
            this.lblHost.AutoSize = true;
            this.lblHost.Location = new Point(26, 148);
            this.lblHost.Text = "DuckDNS name";
            this.lblHost.UseMnemonic = false;
            //
            // txtHost
            //
            this.txtHost.Font = new Font("Segoe UI", 11F);
            this.txtHost.Location = new Point(28, 170);
            this.txtHost.MaxLength = 80;
            this.txtHost.Size = new Size(300, 27);
            this.txtHost.TabIndex = 0;
            //
            // lblSuffix
            //
            this.lblSuffix.AutoSize = true;
            this.lblSuffix.Font = new Font("Segoe UI", 11F);
            this.lblSuffix.Location = new Point(332, 172);
            this.lblSuffix.Text = ".duckdns.org";
            this.lblSuffix.UseMnemonic = false;
            //
            // lblToken
            //
            this.lblToken.AutoSize = true;
            this.lblToken.Location = new Point(26, 210);
            this.lblToken.Text = "DuckDNS token";
            this.lblToken.UseMnemonic = false;
            //
            // txtToken
            //
            this.txtToken.Font = new Font("Segoe UI", 11F);
            this.txtToken.Location = new Point(28, 232);
            this.txtToken.MaxLength = 80;
            this.txtToken.Size = new Size(488, 27);
            this.txtToken.TabIndex = 1;
            this.txtToken.UseSystemPasswordChar = true;
            //
            // lblTokenState
            //
            this.lblTokenState.AutoSize = false;
            this.lblTokenState.Location = new Point(26, 262);
            this.lblTokenState.Size = new Size(490, 20);
            this.lblTokenState.UseMnemonic = false;
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = false;
            this.lblStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblStatus.Location = new Point(26, 292);
            this.lblStatus.Size = new Size(490, 72);
            this.lblStatus.UseMnemonic = false;
            //
            // progress
            //
            this.progress.Location = new Point(28, 368);
            this.progress.MarqueeAnimationSpeed = 30;
            this.progress.Size = new Size(488, 8);
            this.progress.Style = ProgressBarStyle.Marquee;
            this.progress.Visible = false;
            //
            // btnTest
            //
            this.btnTest.BackColor = Color.FromArgb(233, 236, 239);
            this.btnTest.FlatAppearance.BorderSize = 0;
            this.btnTest.FlatStyle = FlatStyle.Flat;
            this.btnTest.Location = new Point(28, 392);
            this.btnTest.Size = new Size(150, 40);
            this.btnTest.TabIndex = 2;
            this.btnTest.Text = "Test Connection";
            this.btnTest.UseVisualStyleBackColor = false;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            //
            // btnSave
            //
            this.btnSave.BackColor = Color.FromArgb(29, 78, 216);
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = FlatStyle.Flat;
            this.btnSave.ForeColor = Color.White;
            this.btnSave.Location = new Point(188, 392);
            this.btnSave.Size = new Size(210, 40);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save && Configure";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // btnClose
            //
            this.btnClose.BackColor = Color.FromArgb(233, 236, 239);
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.FlatStyle = FlatStyle.Flat;
            this.btnClose.Location = new Point(408, 392);
            this.btnClose.Size = new Size(108, 40);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // pollTimer
            //
            this.pollTimer.Interval = 1500;
            this.pollTimer.Tick += new System.EventHandler(this.pollTimer_Tick);
            //
            // MobileCaptureSetupForm
            //
            this.BackColor = Color.White;
            this.ClientSize = new Size(540, 450);
            this.Font = new Font("Segoe UI", 9.75F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Mobile Capture Setup";
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblHelp);
            this.Controls.Add(this.lblHost);
            this.Controls.Add(this.txtHost);
            this.Controls.Add(this.lblSuffix);
            this.Controls.Add(this.lblToken);
            this.Controls.Add(this.txtToken);
            this.Controls.Add(this.lblTokenState);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnClose);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
