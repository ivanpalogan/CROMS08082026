namespace CROMS.Forms
{
    partial class MarriageRegistrationForm
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
            this.btnLicense = new System.Windows.Forms.Button();
            this.btnRegister = new System.Windows.Forms.Button();
            this.cardPosting = new System.Windows.Forms.Panel();
            this.lblPostingCap = new System.Windows.Forms.Label();
            this.lblPostingVal = new System.Windows.Forms.Label();
            this.lblPostingSub = new System.Windows.Forms.Label();
            this.cardIssued = new System.Windows.Forms.Panel();
            this.lblIssuedCap = new System.Windows.Forms.Label();
            this.lblIssuedVal = new System.Windows.Forms.Label();
            this.lblIssuedSub = new System.Windows.Forms.Label();
            this.cardExpiring = new System.Windows.Forms.Panel();
            this.lblExpiringCap = new System.Windows.Forms.Label();
            this.lblExpiringVal = new System.Windows.Forms.Label();
            this.lblExpiringSub = new System.Windows.Forms.Label();
            this.cardComs = new System.Windows.Forms.Panel();
            this.lblComsCap = new System.Windows.Forms.Label();
            this.lblComsVal = new System.Windows.Forms.Label();
            this.lblComsSub = new System.Windows.Forms.Label();
            this.lblLicenses = new System.Windows.Forms.Label();
            this.dgvLicenses = new System.Windows.Forms.DataGridView();
            this.lblRecords = new System.Windows.Forms.Label();
            this.dgvMarriages = new System.Windows.Forms.DataGridView();
            this.cardPosting.SuspendLayout();
            this.cardIssued.SuspendLayout();
            this.cardExpiring.SuspendLayout();
            this.cardComs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLicenses)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMarriages)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblTitle.Location = new System.Drawing.Point(21, 14);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(430, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Marriage Registration && License";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblSubtitle.Location = new System.Drawing.Point(22, 49);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(397, 15);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "MUNICIPAL FORM 90 (LICENSE)  •  FORM 97 (CERTIFICATE OF MARRIAGE)";
            // 
            // btnLicense
            // 
            this.btnLicense.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLicense.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLicense.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnLicense.Location = new System.Drawing.Point(1097, 19);
            this.btnLicense.Name = "btnLicense";
            this.btnLicense.Size = new System.Drawing.Size(171, 35);
            this.btnLicense.TabIndex = 2;
            this.btnLicense.Text = "+ License Application";
            this.btnLicense.UseVisualStyleBackColor = true;
            this.btnLicense.Click += new System.EventHandler(this.btnLicense_Click);
            // 
            // btnRegister
            // 
            this.btnRegister.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRegister.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.btnRegister.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRegister.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnRegister.ForeColor = System.Drawing.Color.White;
            this.btnRegister.Location = new System.Drawing.Point(1277, 19);
            this.btnRegister.Name = "btnRegister";
            this.btnRegister.Size = new System.Drawing.Size(146, 35);
            this.btnRegister.TabIndex = 3;
            this.btnRegister.Text = "+ Register Marriage";
            this.btnRegister.UseVisualStyleBackColor = false;
            this.btnRegister.Click += new System.EventHandler(this.btnRegister_Click);
            // 
            // cardPosting
            // 
            this.cardPosting.BackColor = System.Drawing.Color.White;
            this.cardPosting.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardPosting.Controls.Add(this.lblPostingCap);
            this.cardPosting.Controls.Add(this.lblPostingVal);
            this.cardPosting.Controls.Add(this.lblPostingSub);
            this.cardPosting.Location = new System.Drawing.Point(21, 76);
            this.cardPosting.Name = "cardPosting";
            this.cardPosting.Size = new System.Drawing.Size(340, 83);
            this.cardPosting.TabIndex = 4;
            // 
            // lblPostingCap
            // 
            this.lblPostingCap.AutoSize = true;
            this.lblPostingCap.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblPostingCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblPostingCap.Location = new System.Drawing.Point(12, 10);
            this.lblPostingCap.Name = "lblPostingCap";
            this.lblPostingCap.Size = new System.Drawing.Size(121, 13);
            this.lblPostingCap.TabIndex = 0;
            this.lblPostingCap.Text = "LICENSES IN POSTING";
            // 
            // lblPostingVal
            // 
            this.lblPostingVal.AutoSize = true;
            this.lblPostingVal.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblPostingVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblPostingVal.Location = new System.Drawing.Point(10, 26);
            this.lblPostingVal.Name = "lblPostingVal";
            this.lblPostingVal.Size = new System.Drawing.Size(35, 41);
            this.lblPostingVal.TabIndex = 1;
            this.lblPostingVal.Text = "0";
            // 
            // lblPostingSub
            // 
            this.lblPostingSub.AutoSize = true;
            this.lblPostingSub.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblPostingSub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(134)))), ((int)(((byte)(142)))), ((int)(((byte)(150)))));
            this.lblPostingSub.Location = new System.Drawing.Point(12, 70);
            this.lblPostingSub.Name = "lblPostingSub";
            this.lblPostingSub.Size = new System.Drawing.Size(78, 13);
            this.lblPostingSub.TabIndex = 2;
            this.lblPostingSub.Text = "10-day period";
            // 
            // cardIssued
            // 
            this.cardIssued.BackColor = System.Drawing.Color.White;
            this.cardIssued.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardIssued.Controls.Add(this.lblIssuedCap);
            this.cardIssued.Controls.Add(this.lblIssuedVal);
            this.cardIssued.Controls.Add(this.lblIssuedSub);
            this.cardIssued.Location = new System.Drawing.Point(372, 76);
            this.cardIssued.Name = "cardIssued";
            this.cardIssued.Size = new System.Drawing.Size(340, 83);
            this.cardIssued.TabIndex = 5;
            // 
            // lblIssuedCap
            // 
            this.lblIssuedCap.AutoSize = true;
            this.lblIssuedCap.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblIssuedCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblIssuedCap.Location = new System.Drawing.Point(12, 10);
            this.lblIssuedCap.Name = "lblIssuedCap";
            this.lblIssuedCap.Size = new System.Drawing.Size(149, 13);
            this.lblIssuedCap.TabIndex = 0;
            this.lblIssuedCap.Text = "LICENSES ISSUED (MONTH)";
            // 
            // lblIssuedVal
            // 
            this.lblIssuedVal.AutoSize = true;
            this.lblIssuedVal.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblIssuedVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblIssuedVal.Location = new System.Drawing.Point(10, 26);
            this.lblIssuedVal.Name = "lblIssuedVal";
            this.lblIssuedVal.Size = new System.Drawing.Size(35, 41);
            this.lblIssuedVal.TabIndex = 1;
            this.lblIssuedVal.Text = "0";
            // 
            // lblIssuedSub
            // 
            this.lblIssuedSub.AutoSize = true;
            this.lblIssuedSub.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblIssuedSub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(134)))), ((int)(((byte)(142)))), ((int)(((byte)(150)))));
            this.lblIssuedSub.Location = new System.Drawing.Point(12, 70);
            this.lblIssuedSub.Name = "lblIssuedSub";
            this.lblIssuedSub.Size = new System.Drawing.Size(79, 13);
            this.lblIssuedSub.TabIndex = 2;
            this.lblIssuedSub.Text = "Valid 120 days";
            // 
            // cardExpiring
            // 
            this.cardExpiring.BackColor = System.Drawing.Color.White;
            this.cardExpiring.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardExpiring.Controls.Add(this.lblExpiringCap);
            this.cardExpiring.Controls.Add(this.lblExpiringVal);
            this.cardExpiring.Controls.Add(this.lblExpiringSub);
            this.cardExpiring.Location = new System.Drawing.Point(723, 76);
            this.cardExpiring.Name = "cardExpiring";
            this.cardExpiring.Size = new System.Drawing.Size(340, 83);
            this.cardExpiring.TabIndex = 6;
            // 
            // lblExpiringCap
            // 
            this.lblExpiringCap.AutoSize = true;
            this.lblExpiringCap.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblExpiringCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblExpiringCap.Location = new System.Drawing.Point(12, 10);
            this.lblExpiringCap.Name = "lblExpiringCap";
            this.lblExpiringCap.Size = new System.Drawing.Size(143, 13);
            this.lblExpiringCap.TabIndex = 0;
            this.lblExpiringCap.Text = "LICENSES EXPIRING ≤ 30D";
            // 
            // lblExpiringVal
            // 
            this.lblExpiringVal.AutoSize = true;
            this.lblExpiringVal.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblExpiringVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblExpiringVal.Location = new System.Drawing.Point(10, 26);
            this.lblExpiringVal.Name = "lblExpiringVal";
            this.lblExpiringVal.Size = new System.Drawing.Size(35, 41);
            this.lblExpiringVal.TabIndex = 1;
            this.lblExpiringVal.Text = "0";
            // 
            // lblExpiringSub
            // 
            this.lblExpiringSub.AutoSize = true;
            this.lblExpiringSub.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblExpiringSub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(134)))), ((int)(((byte)(142)))), ((int)(((byte)(150)))));
            this.lblExpiringSub.Location = new System.Drawing.Point(12, 70);
            this.lblExpiringSub.Name = "lblExpiringSub";
            this.lblExpiringSub.Size = new System.Drawing.Size(102, 13);
            this.lblExpiringSub.TabIndex = 2;
            this.lblExpiringSub.Text = "Follow up couples";
            // 
            // cardComs
            // 
            this.cardComs.BackColor = System.Drawing.Color.White;
            this.cardComs.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardComs.Controls.Add(this.lblComsCap);
            this.cardComs.Controls.Add(this.lblComsVal);
            this.cardComs.Controls.Add(this.lblComsSub);
            this.cardComs.Location = new System.Drawing.Point(1075, 76);
            this.cardComs.Name = "cardComs";
            this.cardComs.Size = new System.Drawing.Size(348, 83);
            this.cardComs.TabIndex = 7;
            // 
            // lblComsCap
            // 
            this.lblComsCap.AutoSize = true;
            this.lblComsCap.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblComsCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblComsCap.Location = new System.Drawing.Point(12, 10);
            this.lblComsCap.Name = "lblComsCap";
            this.lblComsCap.Size = new System.Drawing.Size(157, 13);
            this.lblComsCap.TabIndex = 0;
            this.lblComsCap.Text = "COMs REGISTERED (MONTH)";
            // 
            // lblComsVal
            // 
            this.lblComsVal.AutoSize = true;
            this.lblComsVal.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblComsVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblComsVal.Location = new System.Drawing.Point(10, 26);
            this.lblComsVal.Name = "lblComsVal";
            this.lblComsVal.Size = new System.Drawing.Size(35, 41);
            this.lblComsVal.TabIndex = 1;
            this.lblComsVal.Text = "0";
            // 
            // lblComsSub
            // 
            this.lblComsSub.AutoSize = true;
            this.lblComsSub.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblComsSub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(134)))), ((int)(((byte)(142)))), ((int)(((byte)(150)))));
            this.lblComsSub.Location = new System.Drawing.Point(12, 70);
            this.lblComsSub.Name = "lblComsSub";
            this.lblComsSub.Size = new System.Drawing.Size(103, 13);
            this.lblComsSub.TabIndex = 2;
            this.lblComsSub.Text = "Solemnizer returns";
            // 
            // lblLicenses
            // 
            this.lblLicenses.AutoSize = true;
            this.lblLicenses.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblLicenses.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblLicenses.Location = new System.Drawing.Point(21, 173);
            this.lblLicenses.Name = "lblLicenses";
            this.lblLicenses.Size = new System.Drawing.Size(253, 17);
            this.lblLicenses.TabIndex = 8;
            this.lblLicenses.Text = "LICENSES — POSTING PERIOD TRACKER";
            // 
            // dgvLicenses
            // 
            this.dgvLicenses.AllowUserToAddRows = false;
            this.dgvLicenses.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvLicenses.BackgroundColor = System.Drawing.Color.White;
            this.dgvLicenses.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvLicenses.Location = new System.Drawing.Point(21, 192);
            this.dgvLicenses.Name = "dgvLicenses";
            this.dgvLicenses.ReadOnly = true;
            this.dgvLicenses.RowHeadersVisible = false;
            this.dgvLicenses.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvLicenses.Size = new System.Drawing.Size(1402, 165);
            this.dgvLicenses.TabIndex = 9;
            // 
            // lblRecords
            // 
            this.lblRecords.AutoSize = true;
            this.lblRecords.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblRecords.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblRecords.Location = new System.Drawing.Point(21, 371);
            this.lblRecords.Name = "lblRecords";
            this.lblRecords.Size = new System.Drawing.Size(205, 17);
            this.lblRecords.TabIndex = 10;
            this.lblRecords.Text = "MARRIAGE RECORDS (FORM 97)";
            // 
            // dgvMarriages
            // 
            this.dgvMarriages.AllowUserToAddRows = false;
            this.dgvMarriages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvMarriages.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvMarriages.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvMarriages.BackgroundColor = System.Drawing.Color.White;
            this.dgvMarriages.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvMarriages.Location = new System.Drawing.Point(21, 390);
            this.dgvMarriages.Name = "dgvMarriages";
            this.dgvMarriages.ReadOnly = true;
            this.dgvMarriages.RowHeadersVisible = false;
            this.dgvMarriages.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvMarriages.Size = new System.Drawing.Size(1402, 421);
            this.dgvMarriages.TabIndex = 11;
            // 
            // MarriageRegistrationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(1449, 837);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.btnLicense);
            this.Controls.Add(this.btnRegister);
            this.Controls.Add(this.cardPosting);
            this.Controls.Add(this.cardIssued);
            this.Controls.Add(this.cardExpiring);
            this.Controls.Add(this.cardComs);
            this.Controls.Add(this.lblLicenses);
            this.Controls.Add(this.dgvLicenses);
            this.Controls.Add(this.lblRecords);
            this.Controls.Add(this.dgvMarriages);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "MarriageRegistrationForm";
            this.Text = "Marriage Registration";
            this.cardPosting.ResumeLayout(false);
            this.cardPosting.PerformLayout();
            this.cardIssued.ResumeLayout(false);
            this.cardIssued.PerformLayout();
            this.cardExpiring.ResumeLayout(false);
            this.cardExpiring.PerformLayout();
            this.cardComs.ResumeLayout(false);
            this.cardComs.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLicenses)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMarriages)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Button btnLicense;
        private System.Windows.Forms.Button btnRegister;
        private System.Windows.Forms.Panel cardPosting;
        private System.Windows.Forms.Label lblPostingCap;
        private System.Windows.Forms.Label lblPostingVal;
        private System.Windows.Forms.Label lblPostingSub;
        private System.Windows.Forms.Panel cardIssued;
        private System.Windows.Forms.Label lblIssuedCap;
        private System.Windows.Forms.Label lblIssuedVal;
        private System.Windows.Forms.Label lblIssuedSub;
        private System.Windows.Forms.Panel cardExpiring;
        private System.Windows.Forms.Label lblExpiringCap;
        private System.Windows.Forms.Label lblExpiringVal;
        private System.Windows.Forms.Label lblExpiringSub;
        private System.Windows.Forms.Panel cardComs;
        private System.Windows.Forms.Label lblComsCap;
        private System.Windows.Forms.Label lblComsVal;
        private System.Windows.Forms.Label lblComsSub;
        private System.Windows.Forms.Label lblLicenses;
        private System.Windows.Forms.DataGridView dgvLicenses;
        private System.Windows.Forms.Label lblRecords;
        private System.Windows.Forms.DataGridView dgvMarriages;
    }
}
