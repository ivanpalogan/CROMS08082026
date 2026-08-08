namespace CROMS.Forms
{
    partial class ReleaseClaimForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.grpPending = new System.Windows.Forms.GroupBox();
            this.dgvPending = new System.Windows.Forms.DataGridView();
            this.grpClaim = new System.Windows.Forms.GroupBox();
            this.lblSelected = new System.Windows.Forms.Label();
            this.lblClaimant = new System.Windows.Forms.Label();
            this.txtClaimant = new System.Windows.Forms.TextBox();
            this.chkRep = new System.Windows.Forms.CheckBox();
            this.lblIdType = new System.Windows.Forms.Label();
            this.txtIdType = new System.Windows.Forms.ComboBox();
            this.lblIdNum = new System.Windows.Forms.Label();
            this.txtIdNum = new System.Windows.Forms.TextBox();
            this.lblUseCam = new System.Windows.Forms.Label();
            this.tglUseCam = new CROMS.Modules.ToggleSwitch();
            this.lblUseCamState = new System.Windows.Forms.Label();
            this.lblCam = new System.Windows.Forms.Label();
            this.pbCam = new System.Windows.Forms.PictureBox();
            this.lblPick = new System.Windows.Forms.Label();
            this.cboCamera = new System.Windows.Forms.ComboBox();
            this.btnStartCam = new System.Windows.Forms.Button();
            this.btnCapture = new System.Windows.Forms.Button();
            this.btnRetake = new System.Windows.Forms.Button();
            this.lblCamStatus = new System.Windows.Forms.Label();
            this.btnRelease = new System.Windows.Forms.Button();
            this.lblReleased = new System.Windows.Forms.Label();
            this.dgvReleased = new System.Windows.Forms.DataGridView();
            this.grpPending.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPending)).BeginInit();
            this.grpClaim.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbCam)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReleased)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblTitle.Location = new System.Drawing.Point(21, 14);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(224, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Release & Claim";
            this.lblTitle.UseMnemonic = false;
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblSubtitle.Location = new System.Drawing.Point(22, 49);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(354, 15);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "RELEASE DOCUMENTS  ·  CAPTURE THE CLAIMANT\'S PHOTO";
            // 
            // grpPending
            // 
            this.grpPending.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.grpPending.Controls.Add(this.dgvPending);
            this.grpPending.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.grpPending.Location = new System.Drawing.Point(21, 83);
            this.grpPending.Name = "grpPending";
            this.grpPending.Size = new System.Drawing.Size(514, 728);
            this.grpPending.TabIndex = 2;
            this.grpPending.TabStop = false;
            this.grpPending.Text = "Pending Releases  (For Release)";
            // 
            // dgvPending
            // 
            this.dgvPending.AllowUserToAddRows = false;
            this.dgvPending.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPending.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvPending.BackgroundColor = System.Drawing.Color.White;
            this.dgvPending.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPending.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPending.Location = new System.Drawing.Point(3, 21);
            this.dgvPending.Name = "dgvPending";
            this.dgvPending.ReadOnly = true;
            this.dgvPending.RowHeadersVisible = false;
            this.dgvPending.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPending.Size = new System.Drawing.Size(508, 704);
            this.dgvPending.TabIndex = 0;
            // 
            // grpClaim
            // 
            this.grpClaim.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpClaim.Controls.Add(this.lblSelected);
            this.grpClaim.Controls.Add(this.lblClaimant);
            this.grpClaim.Controls.Add(this.txtClaimant);
            this.grpClaim.Controls.Add(this.chkRep);
            this.grpClaim.Controls.Add(this.lblIdType);
            this.grpClaim.Controls.Add(this.txtIdType);
            this.grpClaim.Controls.Add(this.lblIdNum);
            this.grpClaim.Controls.Add(this.txtIdNum);
            this.grpClaim.Controls.Add(this.lblUseCam);
            this.grpClaim.Controls.Add(this.tglUseCam);
            this.grpClaim.Controls.Add(this.lblUseCamState);
            this.grpClaim.Controls.Add(this.lblCam);
            this.grpClaim.Controls.Add(this.pbCam);
            this.grpClaim.Controls.Add(this.lblPick);
            this.grpClaim.Controls.Add(this.cboCamera);
            this.grpClaim.Controls.Add(this.btnStartCam);
            this.grpClaim.Controls.Add(this.btnCapture);
            this.grpClaim.Controls.Add(this.btnRetake);
            this.grpClaim.Controls.Add(this.lblCamStatus);
            this.grpClaim.Controls.Add(this.btnRelease);
            this.grpClaim.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.grpClaim.Location = new System.Drawing.Point(549, 83);
            this.grpClaim.Name = "grpClaim";
            this.grpClaim.Size = new System.Drawing.Size(879, 510);
            this.grpClaim.TabIndex = 3;
            this.grpClaim.TabStop = false;
            this.grpClaim.Text = "Claim Details";
            // 
            // lblSelected
            // 
            this.lblSelected.AutoSize = true;
            this.lblSelected.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblSelected.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.lblSelected.Location = new System.Drawing.Point(17, 26);
            this.lblSelected.Name = "lblSelected";
            this.lblSelected.Size = new System.Drawing.Size(241, 17);
            this.lblSelected.TabIndex = 0;
            this.lblSelected.Text = "Select a pending release on the left →";
            // 
            // lblClaimant
            // 
            this.lblClaimant.AutoSize = true;
            this.lblClaimant.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblClaimant.Location = new System.Drawing.Point(17, 63);
            this.lblClaimant.Name = "lblClaimant";
            this.lblClaimant.Size = new System.Drawing.Size(90, 15);
            this.lblClaimant.TabIndex = 1;
            this.lblClaimant.Text = "Claimant Name";
            // 
            // txtClaimant
            // 
            this.txtClaimant.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtClaimant.Location = new System.Drawing.Point(146, 61);
            this.txtClaimant.Name = "txtClaimant";
            this.txtClaimant.Size = new System.Drawing.Size(343, 25);
            this.txtClaimant.TabIndex = 2;
            // 
            // chkRep
            // 
            this.chkRep.AutoSize = true;
            this.chkRep.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkRep.Location = new System.Drawing.Point(146, 95);
            this.chkRep.Name = "chkRep";
            this.chkRep.Size = new System.Drawing.Size(238, 19);
            this.chkRep.TabIndex = 3;
            this.chkRep.Text = "Claimed by an authorized representative";
            this.chkRep.UseVisualStyleBackColor = true;
            // 
            // lblIdType
            // 
            this.lblIdType.AutoSize = true;
            this.lblIdType.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblIdType.Location = new System.Drawing.Point(17, 133);
            this.lblIdType.Name = "lblIdType";
            this.lblIdType.Size = new System.Drawing.Size(72, 15);
            this.lblIdType.TabIndex = 4;
            this.lblIdType.Text = "Rep. ID Type";
            // 
            // txtIdType
            // 
            this.txtIdType.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtIdType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.txtIdType.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.txtIdType.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.txtIdType.MaxDropDownItems = 15;
            this.txtIdType.Location = new System.Drawing.Point(146, 130);
            this.txtIdType.Name = "txtIdType";
            this.txtIdType.Size = new System.Drawing.Size(223, 25);
            this.txtIdType.TabIndex = 5;
            // 
            // lblIdNum
            // 
            this.lblIdNum.AutoSize = true;
            this.lblIdNum.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblIdNum.Location = new System.Drawing.Point(386, 133);
            this.lblIdNum.Name = "lblIdNum";
            this.lblIdNum.Size = new System.Drawing.Size(91, 15);
            this.lblIdNum.TabIndex = 6;
            this.lblIdNum.Text = "Rep. ID Number";
            // 
            // txtIdNum
            // 
            this.txtIdNum.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtIdNum.Location = new System.Drawing.Point(497, 130);
            this.txtIdNum.Name = "txtIdNum";
            this.txtIdNum.Size = new System.Drawing.Size(223, 25);
            this.txtIdNum.TabIndex = 7;
            //
            // lblUseCam
            //
            this.lblUseCam.AutoSize = true;
            this.lblUseCam.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblUseCam.Location = new System.Drawing.Point(560, 26);
            this.lblUseCam.Name = "lblUseCam";
            this.lblUseCam.Size = new System.Drawing.Size(78, 15);
            this.lblUseCam.TabIndex = 17;
            this.lblUseCam.Text = "Use Camera?";
            //
            // tglUseCam
            //
            this.tglUseCam.Location = new System.Drawing.Point(660, 22);
            this.tglUseCam.Name = "tglUseCam";
            this.tglUseCam.Size = new System.Drawing.Size(52, 28);
            this.tglUseCam.TabIndex = 18;
            this.tglUseCam.CheckedChanged += new System.EventHandler(this.tglUseCam_CheckedChanged);
            //
            // lblUseCamState
            //
            this.lblUseCamState.AutoSize = true;
            this.lblUseCamState.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblUseCamState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblUseCamState.Location = new System.Drawing.Point(720, 27);
            this.lblUseCamState.Name = "lblUseCamState";
            this.lblUseCamState.Size = new System.Drawing.Size(26, 15);
            this.lblUseCamState.TabIndex = 19;
            this.lblUseCamState.Text = "Off";
            //
            // lblCam
            //
            this.lblCam.AutoSize = true;
            this.lblCam.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCam.Location = new System.Drawing.Point(17, 169);
            this.lblCam.Name = "lblCam";
            this.lblCam.Size = new System.Drawing.Size(320, 15);
            this.lblCam.TabIndex = 8;
            this.lblCam.Text = "Claimant Photo  (capture with the webcam — optional)";
            //
            // pbCam
            //
            this.pbCam.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(236)))), ((int)(((byte)(239)))));
            this.pbCam.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbCam.Location = new System.Drawing.Point(17, 190);
            this.pbCam.Name = "pbCam";
            this.pbCam.Size = new System.Drawing.Size(360, 250);
            this.pbCam.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbCam.TabIndex = 9;
            this.pbCam.TabStop = false;
            //
            // lblPick
            //
            this.lblPick.AutoSize = true;
            this.lblPick.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPick.Location = new System.Drawing.Point(392, 192);
            this.lblPick.Name = "lblPick";
            this.lblPick.Size = new System.Drawing.Size(52, 15);
            this.lblPick.TabIndex = 10;
            this.lblPick.Text = "Camera";
            //
            // cboCamera
            //
            this.cboCamera.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCamera.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cboCamera.Location = new System.Drawing.Point(395, 210);
            this.cboCamera.Name = "cboCamera";
            this.cboCamera.Size = new System.Drawing.Size(320, 25);
            this.cboCamera.TabIndex = 11;
            this.cboCamera.SelectedIndexChanged += new System.EventHandler(this.cboCamera_SelectedIndexChanged);
            //
            // btnStartCam
            //
            this.btnStartCam.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartCam.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnStartCam.Location = new System.Drawing.Point(395, 248);
            this.btnStartCam.Name = "btnStartCam";
            this.btnStartCam.Size = new System.Drawing.Size(150, 36);
            this.btnStartCam.TabIndex = 12;
            this.btnStartCam.Text = "Start Camera";
            this.btnStartCam.UseVisualStyleBackColor = true;
            this.btnStartCam.Click += new System.EventHandler(this.btnStartCam_Click);
            //
            // btnCapture
            //
            this.btnCapture.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.btnCapture.Enabled = false;
            this.btnCapture.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCapture.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnCapture.ForeColor = System.Drawing.Color.White;
            this.btnCapture.Location = new System.Drawing.Point(555, 248);
            this.btnCapture.Name = "btnCapture";
            this.btnCapture.Size = new System.Drawing.Size(150, 36);
            this.btnCapture.TabIndex = 13;
            this.btnCapture.Text = "Capture";
            this.btnCapture.UseVisualStyleBackColor = false;
            this.btnCapture.Click += new System.EventHandler(this.btnCapture_Click);
            //
            // btnRetake
            //
            this.btnRetake.Enabled = false;
            this.btnRetake.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRetake.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnRetake.Location = new System.Drawing.Point(395, 290);
            this.btnRetake.Name = "btnRetake";
            this.btnRetake.Size = new System.Drawing.Size(150, 36);
            this.btnRetake.TabIndex = 14;
            this.btnRetake.Text = "Retake";
            this.btnRetake.UseVisualStyleBackColor = true;
            this.btnRetake.Click += new System.EventHandler(this.btnRetake_Click);
            //
            // lblCamStatus
            //
            this.lblCamStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCamStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblCamStatus.Location = new System.Drawing.Point(392, 336);
            this.lblCamStatus.Name = "lblCamStatus";
            this.lblCamStatus.Size = new System.Drawing.Size(470, 40);
            this.lblCamStatus.TabIndex = 15;
            this.lblCamStatus.Text = "Camera idle. Click Start Camera to begin.";
            //
            // btnRelease
            //
            this.btnRelease.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.btnRelease.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRelease.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnRelease.ForeColor = System.Drawing.Color.White;
            this.btnRelease.Location = new System.Drawing.Point(17, 456);
            this.btnRelease.Name = "btnRelease";
            this.btnRelease.Size = new System.Drawing.Size(206, 40);
            this.btnRelease.TabIndex = 16;
            this.btnRelease.Text = "Release";
            this.btnRelease.UseVisualStyleBackColor = false;
            this.btnRelease.Click += new System.EventHandler(this.btnRelease_Click);
            // 
            // lblReleased
            // 
            this.lblReleased.AutoSize = true;
            this.lblReleased.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblReleased.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblReleased.Location = new System.Drawing.Point(549, 606);
            this.lblReleased.Name = "lblReleased";
            this.lblReleased.Size = new System.Drawing.Size(119, 17);
            this.lblReleased.TabIndex = 4;
            this.lblReleased.Text = "RECENT RELEASES";
            // 
            // dgvReleased
            // 
            this.dgvReleased.AllowUserToAddRows = false;
            this.dgvReleased.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvReleased.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvReleased.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvReleased.BackgroundColor = System.Drawing.Color.White;
            this.dgvReleased.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvReleased.Location = new System.Drawing.Point(549, 625);
            this.dgvReleased.Name = "dgvReleased";
            this.dgvReleased.ReadOnly = true;
            this.dgvReleased.RowHeadersVisible = false;
            this.dgvReleased.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvReleased.Size = new System.Drawing.Size(879, 186);
            this.dgvReleased.TabIndex = 5;
            // 
            // ReleaseClaimForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(1449, 837);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.grpPending);
            this.Controls.Add(this.grpClaim);
            this.Controls.Add(this.lblReleased);
            this.Controls.Add(this.dgvReleased);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ReleaseClaimForm";
            this.Text = "Release & Claim";
            this.grpPending.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPending)).EndInit();
            this.grpClaim.ResumeLayout(false);
            this.grpClaim.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbCam)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReleased)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.GroupBox grpPending;
        private System.Windows.Forms.DataGridView dgvPending;
        private System.Windows.Forms.GroupBox grpClaim;
        private System.Windows.Forms.Label lblSelected;
        private System.Windows.Forms.Label lblClaimant;
        private System.Windows.Forms.TextBox txtClaimant;
        private System.Windows.Forms.CheckBox chkRep;
        private System.Windows.Forms.Label lblIdType;
        private System.Windows.Forms.ComboBox txtIdType;
        private System.Windows.Forms.Label lblIdNum;
        private System.Windows.Forms.TextBox txtIdNum;
        private System.Windows.Forms.Label lblUseCam;
        private CROMS.Modules.ToggleSwitch tglUseCam;
        private System.Windows.Forms.Label lblUseCamState;
        private System.Windows.Forms.Label lblCam;
        private System.Windows.Forms.PictureBox pbCam;
        private System.Windows.Forms.Label lblPick;
        private System.Windows.Forms.ComboBox cboCamera;
        private System.Windows.Forms.Button btnStartCam;
        private System.Windows.Forms.Button btnCapture;
        private System.Windows.Forms.Button btnRetake;
        private System.Windows.Forms.Label lblCamStatus;
        private System.Windows.Forms.Button btnRelease;
        private System.Windows.Forms.Label lblReleased;
        private System.Windows.Forms.DataGridView dgvReleased;
    }
}
