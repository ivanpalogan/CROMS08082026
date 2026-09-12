namespace CROMS.Forms
{
    partial class LoginForm
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlLogo = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlBadge = new System.Windows.Forms.Panel();
            this.lblHeadline = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblUserCap = new System.Windows.Forms.Label();
            this.pnlUserHost = new System.Windows.Forms.Panel();
            this.txtUser = new System.Windows.Forms.TextBox();
            this.lblPassCap = new System.Windows.Forms.Label();
            this.pnlPassHost = new System.Windows.Forms.Panel();
            this.txtPass = new System.Windows.Forms.TextBox();
            this.btnEye = new System.Windows.Forms.Button();
            this.pnlCapsIcon = new System.Windows.Forms.Panel();
            this.lblCaps = new System.Windows.Forms.Label();
            this.lblMsg = new System.Windows.Forms.Label();
            this.btnSignIn = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.pnlUserHost.SuspendLayout();
            this.pnlPassHost.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlLogo
            // 
            this.pnlLogo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.pnlLogo.Location = new System.Drawing.Point(27, 21);
            this.pnlLogo.Name = "pnlLogo";
            this.pnlLogo.Size = new System.Drawing.Size(24, 24);
            this.pnlLogo.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblTitle.Location = new System.Drawing.Point(57, 21);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(66, 21);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "CROMS";
            // 
            // pnlBadge
            // 
            this.pnlBadge.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.pnlBadge.Location = new System.Drawing.Point(208, 19);
            this.pnlBadge.Name = "pnlBadge";
            this.pnlBadge.Size = new System.Drawing.Size(141, 26);
            this.pnlBadge.TabIndex = 2;
            // 
            // lblHeadline
            // 
            this.lblHeadline.Font = new System.Drawing.Font("Segoe UI", 19F, System.Drawing.FontStyle.Bold);
            this.lblHeadline.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblHeadline.Location = new System.Drawing.Point(0, 48);
            this.lblHeadline.Name = "lblHeadline";
            this.lblHeadline.Size = new System.Drawing.Size(377, 35);
            this.lblHeadline.TabIndex = 3;
            this.lblHeadline.Text = "Sign in to CROMS";
            this.lblHeadline.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblHeadline.Click += new System.EventHandler(this.lblHeadline_Click);
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblSubtitle.Location = new System.Drawing.Point(0, 83);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(377, 25);
            this.lblSubtitle.TabIndex = 4;
            this.lblSubtitle.Text = "Local Civil Registry Office";
            this.lblSubtitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblSubtitle.Click += new System.EventHandler(this.lblSubtitle_Click);
            // 
            // lblUserCap
            // 
            this.lblUserCap.AutoSize = true;
            this.lblUserCap.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.lblUserCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblUserCap.Location = new System.Drawing.Point(24, 108);
            this.lblUserCap.Name = "lblUserCap";
            this.lblUserCap.Size = new System.Drawing.Size(60, 15);
            this.lblUserCap.TabIndex = 5;
            this.lblUserCap.Text = "Username";
            // 
            // pnlUserHost
            // 
            this.pnlUserHost.Controls.Add(this.txtUser);
            this.pnlUserHost.Location = new System.Drawing.Point(24, 124);
            this.pnlUserHost.Name = "pnlUserHost";
            this.pnlUserHost.Size = new System.Drawing.Size(322, 38);
            this.pnlUserHost.TabIndex = 6;
            // 
            // txtUser
            // 
            this.txtUser.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtUser.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.txtUser.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.txtUser.Location = new System.Drawing.Point(34, 9);
            this.txtUser.Name = "txtUser";
            this.txtUser.Size = new System.Drawing.Size(274, 22);
            this.txtUser.TabIndex = 0;
            // 
            // lblPassCap
            // 
            this.lblPassCap.AutoSize = true;
            this.lblPassCap.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.lblPassCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblPassCap.Location = new System.Drawing.Point(24, 171);
            this.lblPassCap.Name = "lblPassCap";
            this.lblPassCap.Size = new System.Drawing.Size(57, 15);
            this.lblPassCap.TabIndex = 7;
            this.lblPassCap.Text = "Password";
            // 
            // pnlPassHost
            // 
            this.pnlPassHost.Controls.Add(this.txtPass);
            this.pnlPassHost.Controls.Add(this.btnEye);
            this.pnlPassHost.Location = new System.Drawing.Point(24, 186);
            this.pnlPassHost.Name = "pnlPassHost";
            this.pnlPassHost.Size = new System.Drawing.Size(322, 38);
            this.pnlPassHost.TabIndex = 8;
            // 
            // txtPass
            // 
            this.txtPass.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtPass.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.txtPass.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.txtPass.Location = new System.Drawing.Point(34, 9);
            this.txtPass.Name = "txtPass";
            this.txtPass.Size = new System.Drawing.Size(240, 22);
            this.txtPass.TabIndex = 0;
            this.txtPass.UseSystemPasswordChar = true;
            // 
            // btnEye
            // 
            this.btnEye.BackColor = System.Drawing.Color.White;
            this.btnEye.FlatAppearance.BorderSize = 0;
            this.btnEye.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEye.Location = new System.Drawing.Point(279, 3);
            this.btnEye.Name = "btnEye";
            this.btnEye.Size = new System.Drawing.Size(31, 31);
            this.btnEye.TabIndex = 1;
            this.btnEye.TabStop = false;
            this.btnEye.Tag = "noskin";
            this.btnEye.UseVisualStyleBackColor = false;
            // 
            // pnlCapsIcon
            // 
            this.pnlCapsIcon.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.pnlCapsIcon.Location = new System.Drawing.Point(24, 233);
            this.pnlCapsIcon.Name = "pnlCapsIcon";
            this.pnlCapsIcon.Size = new System.Drawing.Size(12, 12);
            this.pnlCapsIcon.TabIndex = 9;
            this.pnlCapsIcon.Visible = false;
            // 
            // lblCaps
            // 
            this.lblCaps.AutoSize = true;
            this.lblCaps.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblCaps.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(83)))), ((int)(((byte)(9)))));
            this.lblCaps.Location = new System.Drawing.Point(40, 232);
            this.lblCaps.Name = "lblCaps";
            this.lblCaps.Size = new System.Drawing.Size(89, 15);
            this.lblCaps.TabIndex = 10;
            this.lblCaps.Text = "Caps Lock is on";
            this.lblCaps.Visible = false;
            // 
            // lblMsg
            // 
            this.lblMsg.AutoSize = true;
            this.lblMsg.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblMsg.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(50)))), ((int)(((byte)(63)))));
            this.lblMsg.Location = new System.Drawing.Point(24, 254);
            this.lblMsg.Name = "lblMsg";
            this.lblMsg.Size = new System.Drawing.Size(0, 15);
            this.lblMsg.TabIndex = 11;
            // 
            // btnSignIn
            // 
            this.btnSignIn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.btnSignIn.FlatAppearance.BorderSize = 0;
            this.btnSignIn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSignIn.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnSignIn.ForeColor = System.Drawing.Color.White;
            this.btnSignIn.Location = new System.Drawing.Point(27, 263);
            this.btnSignIn.Name = "btnSignIn";
            this.btnSignIn.Size = new System.Drawing.Size(322, 40);
            this.btnSignIn.TabIndex = 12;
            this.btnSignIn.Text = "Sign In";
            this.btnSignIn.UseVisualStyleBackColor = false;
            this.btnSignIn.Click += new System.EventHandler(this.btnSignIn_Click);
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.btnExit.FlatAppearance.BorderSize = 0;
            this.btnExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExit.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnExit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.btnExit.Location = new System.Drawing.Point(27, 309);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(322, 34);
            this.btnExit.TabIndex = 13;
            this.btnExit.TabStop = false;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // LoginForm
            // 
            this.AcceptButton = this.btnSignIn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(377, 364);
            this.Controls.Add(this.pnlLogo);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.pnlBadge);
            this.Controls.Add(this.lblHeadline);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblUserCap);
            this.Controls.Add(this.pnlUserHost);
            this.Controls.Add(this.lblPassCap);
            this.Controls.Add(this.pnlPassHost);
            this.Controls.Add(this.pnlCapsIcon);
            this.Controls.Add(this.lblCaps);
            this.Controls.Add(this.lblMsg);
            this.Controls.Add(this.btnSignIn);
            this.Controls.Add(this.btnExit);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CROMS — Sign In";
            this.pnlUserHost.ResumeLayout(false);
            this.pnlUserHost.PerformLayout();
            this.pnlPassHost.ResumeLayout(false);
            this.pnlPassHost.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlLogo;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel pnlBadge;
        private System.Windows.Forms.Label lblHeadline;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Label lblUserCap;
        private System.Windows.Forms.Panel pnlUserHost;
        private System.Windows.Forms.TextBox txtUser;
        private System.Windows.Forms.Label lblPassCap;
        private System.Windows.Forms.Panel pnlPassHost;
        private System.Windows.Forms.TextBox txtPass;
        private System.Windows.Forms.Button btnEye;
        private System.Windows.Forms.Panel pnlCapsIcon;
        private System.Windows.Forms.Label lblCaps;
        private System.Windows.Forms.Label lblMsg;
        private System.Windows.Forms.Button btnSignIn;
        private System.Windows.Forms.Button btnExit;
    }
}
