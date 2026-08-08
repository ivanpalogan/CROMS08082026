namespace CROMS.Forms
{
    partial class FeesPaymentsForm
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
            this.lblHeader = new System.Windows.Forms.Label();
            this._dgv = new System.Windows.Forms.DataGridView();
            this.pnlRight = new System.Windows.Forms.Panel();
            this._lblSel = new System.Windows.Forms.Label();
            this.grpSummary = new System.Windows.Forms.GroupBox();
            this.lblDocFeeCap = new System.Windows.Forms.Label();
            this._lblDocFee = new System.Windows.Forms.Label();
            this.lblAddCap = new System.Windows.Forms.Label();
            this._txtAdd = new System.Windows.Forms.TextBox();
            this.lblTotalCap = new System.Windows.Forms.Label();
            this._lblTotal = new System.Windows.Forms.Label();
            this.lblTenderedCap = new System.Windows.Forms.Label();
            this._txtTendered = new System.Windows.Forms.TextBox();
            this.lblChangeCap = new System.Windows.Forms.Label();
            this._lblChange = new System.Windows.Forms.Label();
            this.grpDetails = new System.Windows.Forms.GroupBox();
            this.lblMethodCap = new System.Windows.Forms.Label();
            this._cboMethod = new System.Windows.Forms.ComboBox();
            this.lblRefCap = new System.Windows.Forms.Label();
            this._txtRef = new System.Windows.Forms.TextBox();
            this.lblOrCap = new System.Windows.Forms.Label();
            this._txtOr = new System.Windows.Forms.TextBox();
            this._lblBy = new System.Windows.Forms.Label();
            this._lblWhen = new System.Windows.Forms.Label();
            this.lblRemarksCap = new System.Windows.Forms.Label();
            this._txtRemarks = new System.Windows.Forms.TextBox();
            this.btnPay = new System.Windows.Forms.Button();
            this.btnPrint = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this._dgv)).BeginInit();
            this.pnlRight.SuspendLayout();
            this.grpSummary.SuspendLayout();
            this.grpDetails.SuspendLayout();
            this.SuspendLayout();
            //
            // lblHeader
            //
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(20, 14, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1200, 58);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "Fees & Payments";
            this.lblHeader.UseMnemonic = false;
            //
            // _dgv
            //
            this._dgv.AllowUserToAddRows = false;
            this._dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._dgv.BackgroundColor = System.Drawing.Color.White;
            this._dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._dgv.Dock = System.Windows.Forms.DockStyle.Left;
            this._dgv.Location = new System.Drawing.Point(0, 58);
            this._dgv.Name = "_dgv";
            this._dgv.ReadOnly = true;
            this._dgv.RowHeadersVisible = false;
            this._dgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._dgv.Size = new System.Drawing.Size(540, 702);
            this._dgv.TabIndex = 1;
            this._dgv.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellClick);
            //
            // pnlRight
            //
            this.pnlRight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlRight.Controls.Add(this._lblSel);
            this.pnlRight.Controls.Add(this.grpSummary);
            this.pnlRight.Controls.Add(this.grpDetails);
            this.pnlRight.Controls.Add(this._lblBy);
            this.pnlRight.Controls.Add(this._lblWhen);
            this.pnlRight.Controls.Add(this.lblRemarksCap);
            this.pnlRight.Controls.Add(this._txtRemarks);
            this.pnlRight.Controls.Add(this.btnPay);
            this.pnlRight.Controls.Add(this.btnPrint);
            this.pnlRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRight.Location = new System.Drawing.Point(540, 58);
            this.pnlRight.Name = "pnlRight";
            this.pnlRight.Padding = new System.Windows.Forms.Padding(28);
            this.pnlRight.Size = new System.Drawing.Size(660, 702);
            this.pnlRight.TabIndex = 2;
            //
            // _lblSel
            //
            this._lblSel.AutoSize = true;
            this._lblSel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this._lblSel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this._lblSel.Location = new System.Drawing.Point(28, 22);
            this._lblSel.Name = "_lblSel";
            this._lblSel.Size = new System.Drawing.Size(210, 20);
            this._lblSel.TabIndex = 0;
            this._lblSel.Text = "Select a payment on the left  →";
            //
            // grpSummary
            //
            this.grpSummary.Controls.Add(this.lblDocFeeCap);
            this.grpSummary.Controls.Add(this._lblDocFee);
            this.grpSummary.Controls.Add(this.lblAddCap);
            this.grpSummary.Controls.Add(this._txtAdd);
            this.grpSummary.Controls.Add(this.lblTotalCap);
            this.grpSummary.Controls.Add(this._lblTotal);
            this.grpSummary.Controls.Add(this.lblTenderedCap);
            this.grpSummary.Controls.Add(this._txtTendered);
            this.grpSummary.Controls.Add(this.lblChangeCap);
            this.grpSummary.Controls.Add(this._lblChange);
            this.grpSummary.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.grpSummary.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.grpSummary.Location = new System.Drawing.Point(28, 58);
            this.grpSummary.Name = "grpSummary";
            this.grpSummary.Size = new System.Drawing.Size(290, 300);
            this.grpSummary.TabIndex = 1;
            this.grpSummary.TabStop = false;
            this.grpSummary.Text = "Payment Summary";
            //
            // lblDocFeeCap
            //
            this.lblDocFeeCap.AutoSize = true;
            this.lblDocFeeCap.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblDocFeeCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblDocFeeCap.Location = new System.Drawing.Point(18, 40);
            this.lblDocFeeCap.Name = "lblDocFeeCap";
            this.lblDocFeeCap.Size = new System.Drawing.Size(88, 19);
            this.lblDocFeeCap.TabIndex = 0;
            this.lblDocFeeCap.Text = "Document Fee";
            //
            // _lblDocFee
            //
            this._lblDocFee.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this._lblDocFee.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this._lblDocFee.Location = new System.Drawing.Point(150, 40);
            this._lblDocFee.Name = "_lblDocFee";
            this._lblDocFee.Size = new System.Drawing.Size(122, 22);
            this._lblDocFee.TabIndex = 1;
            this._lblDocFee.Text = "₱ 0.00";
            this._lblDocFee.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblAddCap
            //
            this.lblAddCap.AutoSize = true;
            this.lblAddCap.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblAddCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblAddCap.Location = new System.Drawing.Point(18, 82);
            this.lblAddCap.Name = "lblAddCap";
            this.lblAddCap.Size = new System.Drawing.Size(89, 19);
            this.lblAddCap.TabIndex = 2;
            this.lblAddCap.Text = "Additional Fee";
            //
            // _txtAdd
            //
            this._txtAdd.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._txtAdd.Location = new System.Drawing.Point(150, 79);
            this._txtAdd.Name = "_txtAdd";
            this._txtAdd.Size = new System.Drawing.Size(122, 25);
            this._txtAdd.TabIndex = 3;
            this._txtAdd.Text = "0.00";
            this._txtAdd.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // lblTotalCap
            //
            this.lblTotalCap.AutoSize = true;
            this.lblTotalCap.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblTotalCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblTotalCap.Location = new System.Drawing.Point(18, 128);
            this.lblTotalCap.Name = "lblTotalCap";
            this.lblTotalCap.Size = new System.Drawing.Size(96, 20);
            this.lblTotalCap.TabIndex = 4;
            this.lblTotalCap.Text = "Total Amount";
            //
            // _lblTotal
            //
            this._lblTotal.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this._lblTotal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this._lblTotal.Location = new System.Drawing.Point(120, 122);
            this._lblTotal.Name = "_lblTotal";
            this._lblTotal.Size = new System.Drawing.Size(152, 30);
            this._lblTotal.TabIndex = 5;
            this._lblTotal.Text = "₱ 0.00";
            this._lblTotal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblTenderedCap
            //
            this.lblTenderedCap.AutoSize = true;
            this.lblTenderedCap.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblTenderedCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblTenderedCap.Location = new System.Drawing.Point(18, 188);
            this.lblTenderedCap.Name = "lblTenderedCap";
            this.lblTenderedCap.Size = new System.Drawing.Size(114, 19);
            this.lblTenderedCap.TabIndex = 6;
            this.lblTenderedCap.Text = "Amount Tendered";
            //
            // _txtTendered
            //
            this._txtTendered.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._txtTendered.Location = new System.Drawing.Point(150, 185);
            this._txtTendered.Name = "_txtTendered";
            this._txtTendered.Size = new System.Drawing.Size(122, 25);
            this._txtTendered.TabIndex = 7;
            this._txtTendered.Text = "0.00";
            this._txtTendered.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // lblChangeCap
            //
            this.lblChangeCap.AutoSize = true;
            this.lblChangeCap.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblChangeCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblChangeCap.Location = new System.Drawing.Point(18, 236);
            this.lblChangeCap.Name = "lblChangeCap";
            this.lblChangeCap.Size = new System.Drawing.Size(56, 19);
            this.lblChangeCap.TabIndex = 8;
            this.lblChangeCap.Text = "Change";
            //
            // _lblChange
            //
            this._lblChange.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this._lblChange.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this._lblChange.Location = new System.Drawing.Point(120, 232);
            this._lblChange.Name = "_lblChange";
            this._lblChange.Size = new System.Drawing.Size(152, 28);
            this._lblChange.TabIndex = 9;
            this._lblChange.Text = "₱ 0.00";
            this._lblChange.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // grpDetails
            //
            this.grpDetails.Controls.Add(this.lblMethodCap);
            this.grpDetails.Controls.Add(this._cboMethod);
            this.grpDetails.Controls.Add(this.lblRefCap);
            this.grpDetails.Controls.Add(this._txtRef);
            this.grpDetails.Controls.Add(this.lblOrCap);
            this.grpDetails.Controls.Add(this._txtOr);
            this.grpDetails.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.grpDetails.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.grpDetails.Location = new System.Drawing.Point(334, 58);
            this.grpDetails.Name = "grpDetails";
            this.grpDetails.Size = new System.Drawing.Size(290, 300);
            this.grpDetails.TabIndex = 2;
            this.grpDetails.TabStop = false;
            this.grpDetails.Text = "Payment Details";
            //
            // lblMethodCap
            //
            this.lblMethodCap.AutoSize = true;
            this.lblMethodCap.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMethodCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblMethodCap.Location = new System.Drawing.Point(18, 32);
            this.lblMethodCap.Name = "lblMethodCap";
            this.lblMethodCap.Size = new System.Drawing.Size(97, 15);
            this.lblMethodCap.TabIndex = 0;
            this.lblMethodCap.Text = "Payment Method";
            //
            // _cboMethod
            //
            this._cboMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboMethod.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._cboMethod.Location = new System.Drawing.Point(18, 50);
            this._cboMethod.Name = "_cboMethod";
            this._cboMethod.Size = new System.Drawing.Size(254, 25);
            this._cboMethod.TabIndex = 1;
            this._cboMethod.SelectedIndexChanged += new System.EventHandler(this.method_Changed);
            //
            // lblRefCap
            //
            this.lblRefCap.AutoSize = true;
            this.lblRefCap.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRefCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblRefCap.Location = new System.Drawing.Point(18, 88);
            this.lblRefCap.Name = "lblRefCap";
            this.lblRefCap.Size = new System.Drawing.Size(150, 15);
            this.lblRefCap.TabIndex = 2;
            this.lblRefCap.Text = "Reference No. (GCash/Bank)";
            //
            // _txtRef
            //
            this._txtRef.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._txtRef.Location = new System.Drawing.Point(18, 106);
            this._txtRef.Name = "_txtRef";
            this._txtRef.Size = new System.Drawing.Size(254, 25);
            this._txtRef.TabIndex = 3;
            //
            // lblOrCap
            //
            this.lblOrCap.AutoSize = true;
            this.lblOrCap.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblOrCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblOrCap.Location = new System.Drawing.Point(18, 144);
            this.lblOrCap.Name = "lblOrCap";
            this.lblOrCap.Size = new System.Drawing.Size(112, 15);
            this.lblOrCap.TabIndex = 4;
            this.lblOrCap.Text = "Official Receipt No.";
            //
            // _txtOr
            //
            this._txtOr.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._txtOr.Location = new System.Drawing.Point(18, 162);
            this._txtOr.Name = "_txtOr";
            this._txtOr.Size = new System.Drawing.Size(254, 25);
            this._txtOr.TabIndex = 5;
            //
            // _lblBy
            //
            this._lblBy.AutoSize = true;
            this._lblBy.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this._lblBy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this._lblBy.Location = new System.Drawing.Point(28, 372);
            this._lblBy.Name = "_lblBy";
            this._lblBy.Size = new System.Drawing.Size(120, 17);
            this._lblBy.TabIndex = 3;
            this._lblBy.Text = "Processed By:  —";
            //
            // _lblWhen
            //
            this._lblWhen.AutoSize = true;
            this._lblWhen.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this._lblWhen.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this._lblWhen.Location = new System.Drawing.Point(28, 398);
            this._lblWhen.Name = "_lblWhen";
            this._lblWhen.Size = new System.Drawing.Size(140, 17);
            this._lblWhen.TabIndex = 4;
            this._lblWhen.Text = "Date & Time Paid:  —";
            //
            // lblRemarksCap
            //
            this.lblRemarksCap.AutoSize = true;
            this.lblRemarksCap.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblRemarksCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblRemarksCap.Location = new System.Drawing.Point(28, 432);
            this.lblRemarksCap.Name = "lblRemarksCap";
            this.lblRemarksCap.Size = new System.Drawing.Size(64, 17);
            this.lblRemarksCap.TabIndex = 5;
            this.lblRemarksCap.Text = "Remarks";
            //
            // _txtRemarks
            //
            this._txtRemarks.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._txtRemarks.Location = new System.Drawing.Point(28, 452);
            this._txtRemarks.Multiline = true;
            this._txtRemarks.Name = "_txtRemarks";
            this._txtRemarks.Size = new System.Drawing.Size(596, 60);
            this._txtRemarks.TabIndex = 6;
            //
            // btnPay
            //
            this.btnPay.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.btnPay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPay.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnPay.ForeColor = System.Drawing.Color.White;
            this.btnPay.Location = new System.Drawing.Point(28, 532);
            this.btnPay.Name = "btnPay";
            this.btnPay.Size = new System.Drawing.Size(360, 48);
            this.btnPay.TabIndex = 7;
            this.btnPay.Text = "Record Payment";
            this.btnPay.UseVisualStyleBackColor = false;
            this.btnPay.Click += new System.EventHandler(this.btnPay_Click);
            //
            // btnPrint
            //
            this.btnPrint.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.btnPrint.Enabled = false;
            this.btnPrint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrint.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnPrint.ForeColor = System.Drawing.Color.White;
            this.btnPrint.Location = new System.Drawing.Point(404, 532);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(220, 48);
            this.btnPrint.TabIndex = 8;
            this.btnPrint.Text = "Print Receipt";
            this.btnPrint.UseVisualStyleBackColor = false;
            this.btnPrint.Click += new System.EventHandler(this.btnPrint_Click);
            //
            // FeesPaymentsForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1200, 760);
            this.Controls.Add(this.pnlRight);
            this.Controls.Add(this._dgv);
            this.Controls.Add(this.lblHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FeesPaymentsForm";
            this.Text = "Fees and Payments";
            ((System.ComponentModel.ISupportInitialize)(this._dgv)).EndInit();
            this.pnlRight.ResumeLayout(false);
            this.pnlRight.PerformLayout();
            this.grpSummary.ResumeLayout(false);
            this.grpSummary.PerformLayout();
            this.grpDetails.ResumeLayout(false);
            this.grpDetails.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.DataGridView _dgv;
        private System.Windows.Forms.Panel pnlRight;
        private System.Windows.Forms.Label _lblSel;
        private System.Windows.Forms.GroupBox grpSummary;
        private System.Windows.Forms.Label lblDocFeeCap;
        private System.Windows.Forms.Label _lblDocFee;
        private System.Windows.Forms.Label lblAddCap;
        private System.Windows.Forms.TextBox _txtAdd;
        private System.Windows.Forms.Label lblTotalCap;
        private System.Windows.Forms.Label _lblTotal;
        private System.Windows.Forms.Label lblTenderedCap;
        private System.Windows.Forms.TextBox _txtTendered;
        private System.Windows.Forms.Label lblChangeCap;
        private System.Windows.Forms.Label _lblChange;
        private System.Windows.Forms.GroupBox grpDetails;
        private System.Windows.Forms.Label lblMethodCap;
        private System.Windows.Forms.ComboBox _cboMethod;
        private System.Windows.Forms.Label lblRefCap;
        private System.Windows.Forms.TextBox _txtRef;
        private System.Windows.Forms.Label lblOrCap;
        private System.Windows.Forms.TextBox _txtOr;
        private System.Windows.Forms.Label _lblBy;
        private System.Windows.Forms.Label _lblWhen;
        private System.Windows.Forms.Label lblRemarksCap;
        private System.Windows.Forms.TextBox _txtRemarks;
        private System.Windows.Forms.Button btnPay;
        private System.Windows.Forms.Button btnPrint;
    }
}
