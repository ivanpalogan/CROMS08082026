namespace CROMS.Forms
{
    partial class PetitionsForm
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
            this.titleLabel = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.grid = new System.Windows.Forms.DataGridView();
            this.pnlEdit = new System.Windows.Forms.Panel();
            this.lblSel = new System.Windows.Forms.Label();
            this.lblType = new System.Windows.Forms.Label();
            this.cboType = new System.Windows.Forms.ComboBox();
            this.lblRecordType = new System.Windows.Forms.Label();
            this.cboRecordType = new System.Windows.Forms.ComboBox();
            this.lblRecord = new System.Windows.Forms.Label();
            this.cboRecord = new System.Windows.Forms.ComboBox();
            this.lblFiled = new System.Windows.Forms.Label();
            this.dtpFiled = new System.Windows.Forms.DateTimePicker();
            this.lblStage = new System.Windows.Forms.Label();
            this.cboStage = new System.Windows.Forms.ComboBox();
            this.lblRemarks = new System.Windows.Forms.Label();
            this.txtRemarks = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnAdvance = new System.Windows.Forms.Button();
            this.btnNew = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.pnlEdit.SuspendLayout();
            this.SuspendLayout();
            //
            // titleLabel
            //
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.titleLabel.Location = new System.Drawing.Point(32, 28);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(200, 37);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Petitions & Case Tracking";
            this.titleLabel.UseMnemonic = false;
            //
            // lblSubtitle
            //
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblSubtitle.Location = new System.Drawing.Point(34, 70);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(360, 19);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Correction petitions plus track-only cases: legitimation, supplemental reports, legal " +
    "instruments, court orders";
            //
            // grid
            //
            this.grid.AllowUserToAddRows = false;
            this.grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid.Location = new System.Drawing.Point(34, 104);
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(520, 560);
            this.grid.TabIndex = 2;
            this.grid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.grid_CellClick);
            //
            // pnlEdit
            //
            this.pnlEdit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlEdit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlEdit.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlEdit.Controls.Add(this.lblSel);
            this.pnlEdit.Controls.Add(this.lblType);
            this.pnlEdit.Controls.Add(this.cboType);
            this.pnlEdit.Controls.Add(this.lblRecordType);
            this.pnlEdit.Controls.Add(this.cboRecordType);
            this.pnlEdit.Controls.Add(this.lblRecord);
            this.pnlEdit.Controls.Add(this.cboRecord);
            this.pnlEdit.Controls.Add(this.lblFiled);
            this.pnlEdit.Controls.Add(this.dtpFiled);
            this.pnlEdit.Controls.Add(this.lblStage);
            this.pnlEdit.Controls.Add(this.cboStage);
            this.pnlEdit.Controls.Add(this.lblRemarks);
            this.pnlEdit.Controls.Add(this.txtRemarks);
            this.pnlEdit.Controls.Add(this.btnSave);
            this.pnlEdit.Controls.Add(this.btnAdvance);
            this.pnlEdit.Controls.Add(this.btnNew);
            this.pnlEdit.Controls.Add(this.btnDelete);
            this.pnlEdit.Location = new System.Drawing.Point(570, 104);
            this.pnlEdit.Name = "pnlEdit";
            this.pnlEdit.Padding = new System.Windows.Forms.Padding(16);
            this.pnlEdit.Size = new System.Drawing.Size(296, 560);
            this.pnlEdit.TabIndex = 3;
            //
            // lblSel
            //
            this.lblSel.AutoSize = true;
            this.lblSel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblSel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblSel.Location = new System.Drawing.Point(16, 14);
            this.lblSel.Name = "lblSel";
            this.lblSel.Size = new System.Drawing.Size(103, 21);
            this.lblSel.TabIndex = 0;
            this.lblSel.Text = "New Petition";
            //
            // lblType
            //
            this.lblType.AutoSize = true;
            this.lblType.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblType.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblType.Location = new System.Drawing.Point(16, 54);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(78, 15);
            this.lblType.TabIndex = 1;
            this.lblType.Text = "Case Type";
            //
            // cboType
            //
            this.cboType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboType.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboType.FormattingEnabled = true;
            this.cboType.Items.AddRange(new object[] {
            "RA 9048 — Clerical Error / Change of First Name",
            "RA 10172 — Day / Month / Sex Correction",
            "RA 9858 — Legitimation (parents married after birth)",
            "Supplemental Report — up to 2 missing entries",
            "Legal Instrument — Acknowledgment / AUSF",
            "Court Order — annotate per final decision"});
            this.cboType.Location = new System.Drawing.Point(16, 74);
            this.cboType.Name = "cboType";
            this.cboType.Size = new System.Drawing.Size(260, 25);
            this.cboType.TabIndex = 2;
            this.cboType.SelectedIndexChanged += new System.EventHandler(this.cboType_SelectedIndexChanged);
            //
            // lblRecordType
            //
            this.lblRecordType.AutoSize = true;
            this.lblRecordType.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRecordType.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblRecordType.Location = new System.Drawing.Point(16, 118);
            this.lblRecordType.Name = "lblRecordType";
            this.lblRecordType.Size = new System.Drawing.Size(72, 15);
            this.lblRecordType.TabIndex = 3;
            this.lblRecordType.Text = "Record Type";
            //
            // cboRecordType
            //
            this.cboRecordType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboRecordType.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboRecordType.FormattingEnabled = true;
            this.cboRecordType.Items.AddRange(new object[] {
            "Birth",
            "Marriage",
            "Death"});
            this.cboRecordType.Location = new System.Drawing.Point(16, 138);
            this.cboRecordType.Name = "cboRecordType";
            this.cboRecordType.Size = new System.Drawing.Size(260, 25);
            this.cboRecordType.TabIndex = 4;
            this.cboRecordType.SelectedIndexChanged += new System.EventHandler(this.cboRecordType_SelectedIndexChanged);
            //
            // lblRecord
            //
            this.lblRecord.AutoSize = true;
            this.lblRecord.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRecord.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblRecord.Location = new System.Drawing.Point(16, 182);
            this.lblRecord.Name = "lblRecord";
            this.lblRecord.Size = new System.Drawing.Size(46, 15);
            this.lblRecord.TabIndex = 5;
            this.lblRecord.Text = "Record";
            //
            // cboRecord
            //
            this.cboRecord.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboRecord.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboRecord.FormattingEnabled = true;
            this.cboRecord.Location = new System.Drawing.Point(16, 202);
            this.cboRecord.Name = "cboRecord";
            this.cboRecord.Size = new System.Drawing.Size(260, 25);
            this.cboRecord.TabIndex = 6;
            //
            // lblFiled
            //
            this.lblFiled.AutoSize = true;
            this.lblFiled.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFiled.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblFiled.Location = new System.Drawing.Point(16, 246);
            this.lblFiled.Name = "lblFiled";
            this.lblFiled.Size = new System.Drawing.Size(60, 15);
            this.lblFiled.TabIndex = 7;
            this.lblFiled.Text = "Filed Date";
            //
            // dtpFiled
            //
            this.dtpFiled.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpFiled.Location = new System.Drawing.Point(16, 266);
            this.dtpFiled.Name = "dtpFiled";
            this.dtpFiled.Size = new System.Drawing.Size(260, 25);
            this.dtpFiled.TabIndex = 8;
            //
            // lblStage
            //
            this.lblStage.AutoSize = true;
            this.lblStage.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblStage.Location = new System.Drawing.Point(16, 310);
            this.lblStage.Name = "lblStage";
            this.lblStage.Size = new System.Drawing.Size(38, 15);
            this.lblStage.TabIndex = 9;
            this.lblStage.Text = "Stage";
            //
            // cboStage
            //
            this.cboStage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStage.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboStage.FormattingEnabled = true;
            this.cboStage.Items.AddRange(new object[] {
            "Filed",
            "Posted",
            "Decision",
            "PSA Endorsement"});
            this.cboStage.Location = new System.Drawing.Point(16, 330);
            this.cboStage.Name = "cboStage";
            this.cboStage.Size = new System.Drawing.Size(260, 25);
            this.cboStage.TabIndex = 10;
            //
            // lblRemarks
            //
            this.lblRemarks.AutoSize = true;
            this.lblRemarks.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRemarks.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblRemarks.Location = new System.Drawing.Point(16, 374);
            this.lblRemarks.Name = "lblRemarks";
            this.lblRemarks.Size = new System.Drawing.Size(56, 15);
            this.lblRemarks.TabIndex = 11;
            this.lblRemarks.Text = "Remarks";
            //
            // txtRemarks
            //
            this.txtRemarks.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtRemarks.Location = new System.Drawing.Point(16, 394);
            this.txtRemarks.Multiline = true;
            this.txtRemarks.Name = "txtRemarks";
            this.txtRemarks.Size = new System.Drawing.Size(260, 50);
            this.txtRemarks.TabIndex = 12;
            //
            // btnSave
            //
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(16, 458);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(116, 38);
            this.btnSave.TabIndex = 13;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // btnAdvance
            //
            this.btnAdvance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.btnAdvance.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdvance.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnAdvance.ForeColor = System.Drawing.Color.White;
            this.btnAdvance.Location = new System.Drawing.Point(138, 458);
            this.btnAdvance.Name = "btnAdvance";
            this.btnAdvance.Size = new System.Drawing.Size(116, 38);
            this.btnAdvance.TabIndex = 14;
            this.btnAdvance.Text = "Advance Stage ▸";
            this.btnAdvance.UseVisualStyleBackColor = false;
            this.btnAdvance.Click += new System.EventHandler(this.btnAdvance_Click);
            //
            // btnNew
            //
            this.btnNew.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.btnNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNew.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnNew.ForeColor = System.Drawing.Color.White;
            this.btnNew.Location = new System.Drawing.Point(16, 504);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(116, 38);
            this.btnNew.TabIndex = 15;
            this.btnNew.Text = "New";
            this.btnNew.UseVisualStyleBackColor = false;
            this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
            //
            // btnDelete
            //
            this.btnDelete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDelete.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnDelete.ForeColor = System.Drawing.Color.White;
            this.btnDelete.Location = new System.Drawing.Point(138, 504);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(116, 38);
            this.btnDelete.TabIndex = 16;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = false;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            //
            // PetitionsForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(900, 700);
            this.Controls.Add(this.grid);
            this.Controls.Add(this.pnlEdit);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.titleLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "PetitionsForm";
            this.Text = "Petitions & Case Tracking";
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.pnlEdit.ResumeLayout(false);
            this.pnlEdit.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.Panel pnlEdit;
        private System.Windows.Forms.Label lblSel;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.ComboBox cboType;
        private System.Windows.Forms.Label lblRecordType;
        private System.Windows.Forms.ComboBox cboRecordType;
        private System.Windows.Forms.Label lblRecord;
        private System.Windows.Forms.ComboBox cboRecord;
        private System.Windows.Forms.Label lblFiled;
        private System.Windows.Forms.DateTimePicker dtpFiled;
        private System.Windows.Forms.Label lblStage;
        private System.Windows.Forms.ComboBox cboStage;
        private System.Windows.Forms.Label lblRemarks;
        private System.Windows.Forms.TextBox txtRemarks;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnAdvance;
        private System.Windows.Forms.Button btnNew;
        private System.Windows.Forms.Button btnDelete;
    }
}
