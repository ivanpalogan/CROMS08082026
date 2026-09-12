namespace CROMS.Forms
{
    partial class WindowAssignmentForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblChooseWindow = new System.Windows.Forms.Label();
            this.pnlWindows = new System.Windows.Forms.Panel();
            this.lblSelectTxn = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.pnlPriority = new System.Windows.Forms.Panel();
            this.lblPriorityTitle = new System.Windows.Forms.Label();
            this.lblPriorityHint = new System.Windows.Forms.Label();
            this.tglPriority = new CROMS.Modules.ToggleSwitch();
            this.txnPanel = new System.Windows.Forms.Panel();
            this.lblWarn = new System.Windows.Forms.Label();
            this.lblMsg = new System.Windows.Forms.Label();
            this.footerDivider = new System.Windows.Forms.Panel();
            this.btnLogout = new System.Windows.Forms.Button();
            this.btnSkip = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.pnlPriority.SuspendLayout();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 19F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblTitle.Location = new System.Drawing.Point(28, 22);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Choose Your Window";
            //
            // lblSubtitle
            //
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblSubtitle.Location = new System.Drawing.Point(30, 62);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.TabIndex = 1;
            //
            // lblChooseWindow
            //
            this.lblChooseWindow.AutoSize = true;
            this.lblChooseWindow.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            this.lblChooseWindow.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblChooseWindow.Location = new System.Drawing.Point(28, 104);
            this.lblChooseWindow.Name = "lblChooseWindow";
            this.lblChooseWindow.TabIndex = 2;
            this.lblChooseWindow.Text = "Available Windows";
            //
            // pnlWindows   (rounded card painted in code-behind; rows built at runtime)
            //
            this.pnlWindows.AutoScroll = true;
            this.pnlWindows.BackColor = System.Drawing.Color.White;
            this.pnlWindows.Location = new System.Drawing.Point(28, 130);
            this.pnlWindows.Name = "pnlWindows";
            this.pnlWindows.Padding = new System.Windows.Forms.Padding(12, 12, 12, 12);
            this.pnlWindows.Size = new System.Drawing.Size(340, 408);
            this.pnlWindows.TabIndex = 3;
            //
            // lblSelectTxn
            //
            this.lblSelectTxn.AutoSize = true;
            this.lblSelectTxn.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            this.lblSelectTxn.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblSelectTxn.Location = new System.Drawing.Point(392, 104);
            this.lblSelectTxn.Name = "lblSelectTxn";
            this.lblSelectTxn.TabIndex = 4;
            this.lblSelectTxn.Text = "What This Window Handles";
            //
            // lblStatus
            //
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblStatus.Location = new System.Drawing.Point(632, 105);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(220, 20);
            this.lblStatus.TabIndex = 5;
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // pnlPriority   (rounded card painted in code-behind)
            //
            this.pnlPriority.BackColor = System.Drawing.Color.White;
            this.pnlPriority.Controls.Add(this.lblPriorityTitle);
            this.pnlPriority.Controls.Add(this.lblPriorityHint);
            this.pnlPriority.Controls.Add(this.tglPriority);
            this.pnlPriority.Location = new System.Drawing.Point(392, 130);
            this.pnlPriority.Name = "pnlPriority";
            this.pnlPriority.Size = new System.Drawing.Size(460, 74);
            this.pnlPriority.TabIndex = 6;
            //
            // lblPriorityTitle
            //
            this.lblPriorityTitle.AutoSize = true;
            this.lblPriorityTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblPriorityTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblPriorityTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblPriorityTitle.Location = new System.Drawing.Point(22, 15);
            this.lblPriorityTitle.Name = "lblPriorityTitle";
            this.lblPriorityTitle.TabIndex = 0;
            this.lblPriorityTitle.Text = "Priority Window";
            //
            // lblPriorityHint
            //
            this.lblPriorityHint.AutoSize = true;
            this.lblPriorityHint.BackColor = System.Drawing.Color.Transparent;
            this.lblPriorityHint.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPriorityHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblPriorityHint.Location = new System.Drawing.Point(24, 40);
            this.lblPriorityHint.Name = "lblPriorityHint";
            this.lblPriorityHint.TabIndex = 1;
            this.lblPriorityHint.Text = "Handles ALL transactions — the list below is ignored.";
            //
            // tglPriority
            //
            this.tglPriority.BackColor = System.Drawing.Color.Transparent;
            this.tglPriority.Location = new System.Drawing.Point(382, 22);
            this.tglPriority.Name = "tglPriority";
            this.tglPriority.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.tglPriority.Size = new System.Drawing.Size(56, 30);
            this.tglPriority.TabIndex = 2;
            this.tglPriority.CheckedChanged += new System.EventHandler(this.chkAll_CheckedChanged);
            //
            // txnPanel   (plain Panel: rows are owner-drawn selectable cards, built at runtime)
            //
            this.txnPanel.BackColor = System.Drawing.Color.White;
            this.txnPanel.Location = new System.Drawing.Point(392, 216);
            this.txnPanel.Name = "txnPanel";
            this.txnPanel.Padding = new System.Windows.Forms.Padding(14, 12, 14, 12);
            this.txnPanel.Size = new System.Drawing.Size(460, 322);
            this.txnPanel.TabIndex = 7;
            //
            // lblWarn
            //
            this.lblWarn.AutoSize = true;
            this.lblWarn.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblWarn.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(83)))), ((int)(((byte)(9)))));
            this.lblWarn.Location = new System.Drawing.Point(28, 550);
            this.lblWarn.Name = "lblWarn";
            this.lblWarn.TabIndex = 8;
            this.lblWarn.Visible = false;
            //
            // lblMsg
            //
            this.lblMsg.AutoSize = true;
            this.lblMsg.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblMsg.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(50)))), ((int)(((byte)(63)))));
            this.lblMsg.Location = new System.Drawing.Point(28, 572);
            this.lblMsg.Name = "lblMsg";
            this.lblMsg.TabIndex = 9;
            //
            // footerDivider
            //
            this.footerDivider.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.footerDivider.Location = new System.Drawing.Point(0, 600);
            this.footerDivider.Name = "footerDivider";
            this.footerDivider.Size = new System.Drawing.Size(880, 1);
            this.footerDivider.TabIndex = 10;
            //
            // btnLogout
            //
            this.btnLogout.BackColor = System.Drawing.Color.White;
            this.btnLogout.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.btnLogout.FlatAppearance.BorderSize = 1;
            this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogout.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnLogout.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(50)))), ((int)(((byte)(63)))));
            this.btnLogout.Location = new System.Drawing.Point(28, 618);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(120, 44);
            this.btnLogout.TabIndex = 13;
            this.btnLogout.Text = "Log out";
            this.btnLogout.UseVisualStyleBackColor = false;
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            //
            // btnSkip
            //
            this.btnSkip.BackColor = System.Drawing.Color.White;
            this.btnSkip.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.btnSkip.FlatAppearance.BorderSize = 1;
            this.btnSkip.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSkip.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnSkip.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.btnSkip.Location = new System.Drawing.Point(544, 618);
            this.btnSkip.Name = "btnSkip";
            this.btnSkip.Size = new System.Drawing.Size(136, 44);
            this.btnSkip.TabIndex = 11;
            this.btnSkip.Text = "Skip for now";
            this.btnSkip.UseVisualStyleBackColor = false;
            this.btnSkip.Click += new System.EventHandler(this.btnSkip_Click);
            //
            // btnStart
            //
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.btnStart.FlatAppearance.BorderSize = 0;
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.Location = new System.Drawing.Point(692, 618);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(160, 44);
            this.btnStart.TabIndex = 12;
            this.btnStart.Text = "Start Serving";
            this.btnStart.UseVisualStyleBackColor = false;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            //
            // WindowAssignmentForm
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(880, 682);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblChooseWindow);
            this.Controls.Add(this.pnlWindows);
            this.Controls.Add(this.lblSelectTxn);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.pnlPriority);
            this.Controls.Add(this.txnPanel);
            this.Controls.Add(this.lblWarn);
            this.Controls.Add(this.lblMsg);
            this.Controls.Add(this.footerDivider);
            this.Controls.Add(this.btnLogout);
            this.Controls.Add(this.btnSkip);
            this.Controls.Add(this.btnStart);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WindowAssignmentForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CROMS — Choose Your Window";
            this.pnlPriority.ResumeLayout(false);
            this.pnlPriority.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Label lblChooseWindow;
        private System.Windows.Forms.Panel pnlWindows;
        private System.Windows.Forms.Label lblSelectTxn;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Panel pnlPriority;
        private System.Windows.Forms.Label lblPriorityTitle;
        private System.Windows.Forms.Label lblPriorityHint;
        private CROMS.Modules.ToggleSwitch tglPriority;
        private System.Windows.Forms.Panel txnPanel;
        private System.Windows.Forms.Label lblWarn;
        private System.Windows.Forms.Label lblMsg;
        private System.Windows.Forms.Panel footerDivider;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.Button btnSkip;
        private System.Windows.Forms.Button btnStart;
    }
}
