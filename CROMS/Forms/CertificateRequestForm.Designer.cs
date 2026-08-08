namespace CROMS.Forms
{
    partial class CertificateRequestForm
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
            this.grpDetails = new System.Windows.Forms.GroupBox();
            this.lblClient = new System.Windows.Forms.Label();
            this.txtFirst = new System.Windows.Forms.TextBox();
            this.txtMiddle = new System.Windows.Forms.TextBox();
            this.txtLast = new System.Windows.Forms.TextBox();
            this.lblFirst = new System.Windows.Forms.Label();
            this.lblMiddle = new System.Windows.Forms.Label();
            this.lblLast = new System.Windows.Forms.Label();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.lblCertType = new System.Windows.Forms.Label();
            this.cboCertType = new System.Windows.Forms.ComboBox();
            this.lblRecordType = new System.Windows.Forms.Label();
            this.cboRecordType = new System.Windows.Forms.ComboBox();
            this.lblRecord = new System.Windows.Forms.Label();
            this.cboRecord = new System.Windows.Forms.ComboBox();
            this.lblCopies = new System.Windows.Forms.Label();
            this.txtCopies = new System.Windows.Forms.TextBox();
            this.lblPurpose = new System.Windows.Forms.Label();
            this.txtPurpose = new System.Windows.Forms.TextBox();
            this.btnCreate = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.lblRecent = new System.Windows.Forms.Label();
            this.dgvReq = new System.Windows.Forms.DataGridView();
            this.grpDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReq)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblTitle.Location = new System.Drawing.Point(21, 14);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(262, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Certificate Request";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblSubtitle.Location = new System.Drawing.Point(22, 49);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(392, 15);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "REQUEST A CERTIFIED TRUE COPY (CTC) OR A NEGATIVE CERTIFICATION";
            // 
            // grpDetails
            // 
            this.grpDetails.Controls.Add(this.lblClient);
            this.grpDetails.Controls.Add(this.txtFirst);
            this.grpDetails.Controls.Add(this.txtMiddle);
            this.grpDetails.Controls.Add(this.txtLast);
            this.grpDetails.Controls.Add(this.lblFirst);
            this.grpDetails.Controls.Add(this.lblMiddle);
            this.grpDetails.Controls.Add(this.lblLast);
            this.grpDetails.Controls.Add(this.btnRefresh);
            this.grpDetails.Controls.Add(this.lblCertType);
            this.grpDetails.Controls.Add(this.cboCertType);
            this.grpDetails.Controls.Add(this.lblRecordType);
            this.grpDetails.Controls.Add(this.cboRecordType);
            this.grpDetails.Controls.Add(this.lblRecord);
            this.grpDetails.Controls.Add(this.cboRecord);
            this.grpDetails.Controls.Add(this.lblCopies);
            this.grpDetails.Controls.Add(this.txtCopies);
            this.grpDetails.Controls.Add(this.lblPurpose);
            this.grpDetails.Controls.Add(this.txtPurpose);
            this.grpDetails.Controls.Add(this.btnCreate);
            this.grpDetails.Controls.Add(this.btnClear);
            this.grpDetails.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.grpDetails.Location = new System.Drawing.Point(21, 83);
            this.grpDetails.Name = "grpDetails";
            this.grpDetails.Size = new System.Drawing.Size(729, 312);
            this.grpDetails.TabIndex = 2;
            this.grpDetails.TabStop = false;
            this.grpDetails.Text = "Request Details";
            // 
            // lblClient
            // 
            this.lblClient.AutoSize = true;
            this.lblClient.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblClient.Location = new System.Drawing.Point(17, 37);
            this.lblClient.Name = "lblClient";
            this.lblClient.Size = new System.Drawing.Size(73, 15);
            this.lblClient.TabIndex = 0;
            this.lblClient.Text = "Client Name";
            //
            // lblFirst
            //
            this.lblFirst.AutoSize = true;
            this.lblFirst.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblFirst.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblFirst.Location = new System.Drawing.Point(146, 18);
            this.lblFirst.Name = "lblFirst";
            this.lblFirst.Size = new System.Drawing.Size(30, 13);
            this.lblFirst.TabIndex = 20;
            this.lblFirst.Text = "First";
            //
            // lblMiddle
            //
            this.lblMiddle.AutoSize = true;
            this.lblMiddle.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblMiddle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblMiddle.Location = new System.Drawing.Point(334, 18);
            this.lblMiddle.Name = "lblMiddle";
            this.lblMiddle.Size = new System.Drawing.Size(40, 13);
            this.lblMiddle.TabIndex = 21;
            this.lblMiddle.Text = "Middle";
            //
            // lblLast
            //
            this.lblLast.AutoSize = true;
            this.lblLast.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblLast.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblLast.Location = new System.Drawing.Point(522, 18);
            this.lblLast.Name = "lblLast";
            this.lblLast.Size = new System.Drawing.Size(28, 13);
            this.lblLast.TabIndex = 22;
            this.lblLast.Text = "Last";
            //
            // txtFirst
            //
            this.txtFirst.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtFirst.Location = new System.Drawing.Point(146, 35);
            this.txtFirst.Name = "txtFirst";
            this.txtFirst.Size = new System.Drawing.Size(178, 25);
            this.txtFirst.TabIndex = 1;
            //
            // txtMiddle
            //
            this.txtMiddle.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtMiddle.Location = new System.Drawing.Point(334, 35);
            this.txtMiddle.Name = "txtMiddle";
            this.txtMiddle.Size = new System.Drawing.Size(178, 25);
            this.txtMiddle.TabIndex = 2;
            //
            // txtLast
            //
            this.txtLast.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtLast.Location = new System.Drawing.Point(522, 35);
            this.txtLast.Name = "txtLast";
            this.txtLast.Size = new System.Drawing.Size(182, 25);
            this.txtLast.TabIndex = 3;
            // 
            // lblCertType
            // 
            this.lblCertType.AutoSize = true;
            this.lblCertType.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCertType.Location = new System.Drawing.Point(17, 72);
            this.lblCertType.Name = "lblCertType";
            this.lblCertType.Size = new System.Drawing.Size(100, 15);
            this.lblCertType.TabIndex = 2;
            this.lblCertType.Text = "Certification Type";
            // 
            // cboCertType
            // 
            this.cboCertType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCertType.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cboCertType.Location = new System.Drawing.Point(146, 69);
            this.cboCertType.Name = "cboCertType";
            this.cboCertType.Size = new System.Drawing.Size(223, 25);
            this.cboCertType.TabIndex = 4;
            // 
            // lblRecordType
            // 
            this.lblRecordType.AutoSize = true;
            this.lblRecordType.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRecordType.Location = new System.Drawing.Point(17, 107);
            this.lblRecordType.Name = "lblRecordType";
            this.lblRecordType.Size = new System.Drawing.Size(72, 15);
            this.lblRecordType.TabIndex = 4;
            this.lblRecordType.Text = "Record Type";
            // 
            // cboRecordType
            // 
            this.cboRecordType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboRecordType.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cboRecordType.Location = new System.Drawing.Point(146, 104);
            this.cboRecordType.Name = "cboRecordType";
            this.cboRecordType.Size = new System.Drawing.Size(223, 25);
            this.cboRecordType.TabIndex = 5;
            // 
            // lblRecord
            // 
            this.lblRecord.AutoSize = true;
            this.lblRecord.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRecord.Location = new System.Drawing.Point(17, 141);
            this.lblRecord.Name = "lblRecord";
            this.lblRecord.Size = new System.Drawing.Size(44, 15);
            this.lblRecord.TabIndex = 6;
            this.lblRecord.Text = "Record";
            // 
            // cboRecord
            // 
            this.cboRecord.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.cboRecord.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cboRecord.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cboRecord.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cboRecord.Location = new System.Drawing.Point(146, 139);
            this.cboRecord.Name = "cboRecord";
            this.cboRecord.Size = new System.Drawing.Size(558, 25);
            this.cboRecord.TabIndex = 7;
            // 
            // lblCopies
            // 
            this.lblCopies.AutoSize = true;
            this.lblCopies.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCopies.Location = new System.Drawing.Point(17, 176);
            this.lblCopies.Name = "lblCopies";
            this.lblCopies.Size = new System.Drawing.Size(43, 15);
            this.lblCopies.TabIndex = 8;
            this.lblCopies.Text = "Copies";
            // 
            // txtCopies
            // 
            this.txtCopies.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtCopies.Location = new System.Drawing.Point(146, 173);
            this.txtCopies.Name = "txtCopies";
            this.txtCopies.Size = new System.Drawing.Size(86, 25);
            this.txtCopies.TabIndex = 9;
            this.txtCopies.Text = "1";
            // 
            // lblPurpose
            // 
            this.lblPurpose.AutoSize = true;
            this.lblPurpose.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPurpose.Location = new System.Drawing.Point(17, 211);
            this.lblPurpose.Name = "lblPurpose";
            this.lblPurpose.Size = new System.Drawing.Size(50, 15);
            this.lblPurpose.TabIndex = 10;
            this.lblPurpose.Text = "Purpose";
            // 
            // txtPurpose
            // 
            this.txtPurpose.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtPurpose.Location = new System.Drawing.Point(146, 208);
            this.txtPurpose.Name = "txtPurpose";
            this.txtPurpose.Size = new System.Drawing.Size(558, 25);
            this.txtPurpose.TabIndex = 11;
            // 
            // btnCreate
            // 
            this.btnCreate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.btnCreate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCreate.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnCreate.ForeColor = System.Drawing.Color.White;
            this.btnCreate.Location = new System.Drawing.Point(146, 256);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(146, 35);
            this.btnCreate.TabIndex = 12;
            this.btnCreate.Text = "Create Request";
            this.btnCreate.UseVisualStyleBackColor = false;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            // 
            // btnClear
            // 
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnClear.Location = new System.Drawing.Point(300, 256);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(103, 35);
            this.btnClear.TabIndex = 13;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            //
            // btnRefresh
            //
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnRefresh.Location = new System.Drawing.Point(411, 256);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(103, 35);
            this.btnRefresh.TabIndex = 14;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            //
            // lblRecent
            // 
            this.lblRecent.AutoSize = true;
            this.lblRecent.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblRecent.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblRecent.Location = new System.Drawing.Point(21, 412);
            this.lblRecent.Name = "lblRecent";
            this.lblRecent.Size = new System.Drawing.Size(204, 17);
            this.lblRecent.TabIndex = 3;
            this.lblRecent.Text = "RECENT CERTIFICATE REQUESTS";
            // 
            // dgvReq
            // 
            this.dgvReq.AllowUserToAddRows = false;
            this.dgvReq.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvReq.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvReq.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvReq.BackgroundColor = System.Drawing.Color.White;
            this.dgvReq.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvReq.Location = new System.Drawing.Point(21, 435);
            this.dgvReq.Name = "dgvReq";
            this.dgvReq.ReadOnly = true;
            this.dgvReq.RowHeadersVisible = false;
            this.dgvReq.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvReq.Size = new System.Drawing.Size(1407, 381);
            this.dgvReq.TabIndex = 4;
            // 
            // CertificateRequestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(1449, 837);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.grpDetails);
            this.Controls.Add(this.lblRecent);
            this.Controls.Add(this.dgvReq);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "CertificateRequestForm";
            this.Text = "Certificate Request";
            this.grpDetails.ResumeLayout(false);
            this.grpDetails.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReq)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.GroupBox grpDetails;
        private System.Windows.Forms.Label lblClient;
        private System.Windows.Forms.TextBox txtFirst;
        private System.Windows.Forms.TextBox txtMiddle;
        private System.Windows.Forms.TextBox txtLast;
        private System.Windows.Forms.Label lblFirst;
        private System.Windows.Forms.Label lblMiddle;
        private System.Windows.Forms.Label lblLast;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Label lblCertType;
        private System.Windows.Forms.ComboBox cboCertType;
        private System.Windows.Forms.Label lblRecordType;
        private System.Windows.Forms.ComboBox cboRecordType;
        private System.Windows.Forms.Label lblRecord;
        private System.Windows.Forms.ComboBox cboRecord;
        private System.Windows.Forms.Label lblCopies;
        private System.Windows.Forms.TextBox txtCopies;
        private System.Windows.Forms.Label lblPurpose;
        private System.Windows.Forms.TextBox txtPurpose;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Label lblRecent;
        private System.Windows.Forms.DataGridView dgvReq;
    }
}
