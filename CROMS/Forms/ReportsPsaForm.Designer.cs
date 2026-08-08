namespace CROMS.Forms
{
    partial class ReportsPsaForm
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
            this.lblMonth = new System.Windows.Forms.Label();
            this.cboMonth = new System.Windows.Forms.ComboBox();
            this.lblYear = new System.Windows.Forms.Label();
            this.cboYear = new System.Windows.Forms.ComboBox();
            this.btnExport = new System.Windows.Forms.Button();
            this.pnlBirthCard = new System.Windows.Forms.Panel();
            this.lblBirthVal = new System.Windows.Forms.Label();
            this.capBirth = new System.Windows.Forms.Label();
            this.stripeBirth = new System.Windows.Forms.Panel();
            this.pnlMarriageCard = new System.Windows.Forms.Panel();
            this.lblMarriageVal = new System.Windows.Forms.Label();
            this.capMarriage = new System.Windows.Forms.Label();
            this.stripeMarriage = new System.Windows.Forms.Panel();
            this.pnlDeathCard = new System.Windows.Forms.Panel();
            this.lblDeathVal = new System.Windows.Forms.Label();
            this.capDeath = new System.Windows.Forms.Label();
            this.stripeDeath = new System.Windows.Forms.Panel();
            this.pnlCollectCard = new System.Windows.Forms.Panel();
            this.lblCollectVal = new System.Windows.Forms.Label();
            this.capCollect = new System.Windows.Forms.Label();
            this.stripeCollect = new System.Windows.Forms.Panel();
            this.lblSplit = new System.Windows.Forms.Label();
            this.lblRoster = new System.Windows.Forms.Label();
            this.cboType = new System.Windows.Forms.ComboBox();
            this.grid = new System.Windows.Forms.DataGridView();
            this.pnlBirthCard.SuspendLayout();
            this.pnlMarriageCard.SuspendLayout();
            this.pnlDeathCard.SuspendLayout();
            this.pnlCollectCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            //
            // titleLabel
            //
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.titleLabel.Location = new System.Drawing.Point(32, 28);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(180, 37);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Reports and PSA";
            this.titleLabel.UseMnemonic = false;
            //
            // lblSubtitle
            //
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblSubtitle.Location = new System.Drawing.Point(34, 70);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(400, 19);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Monthly Report of Registered Vital Events (for PSA submission)";
            //
            // lblMonth
            //
            this.lblMonth.AutoSize = true;
            this.lblMonth.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMonth.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblMonth.Location = new System.Drawing.Point(34, 108);
            this.lblMonth.Name = "lblMonth";
            this.lblMonth.Size = new System.Drawing.Size(43, 15);
            this.lblMonth.TabIndex = 2;
            this.lblMonth.Text = "Month";
            //
            // cboMonth
            //
            this.cboMonth.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMonth.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboMonth.FormattingEnabled = true;
            this.cboMonth.Items.AddRange(new object[] {
            "January",
            "February",
            "March",
            "April",
            "May",
            "June",
            "July",
            "August",
            "September",
            "October",
            "November",
            "December"});
            this.cboMonth.Location = new System.Drawing.Point(34, 128);
            this.cboMonth.Name = "cboMonth";
            this.cboMonth.Size = new System.Drawing.Size(150, 25);
            this.cboMonth.TabIndex = 3;
            this.cboMonth.SelectedIndexChanged += new System.EventHandler(this.cboMonth_SelectedIndexChanged);
            //
            // lblYear
            //
            this.lblYear.AutoSize = true;
            this.lblYear.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblYear.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblYear.Location = new System.Drawing.Point(200, 108);
            this.lblYear.Name = "lblYear";
            this.lblYear.Size = new System.Drawing.Size(30, 15);
            this.lblYear.TabIndex = 4;
            this.lblYear.Text = "Year";
            //
            // cboYear
            //
            this.cboYear.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboYear.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboYear.FormattingEnabled = true;
            this.cboYear.Location = new System.Drawing.Point(200, 128);
            this.cboYear.Name = "cboYear";
            this.cboYear.Size = new System.Drawing.Size(100, 25);
            this.cboYear.TabIndex = 5;
            this.cboYear.SelectedIndexChanged += new System.EventHandler(this.cboYear_SelectedIndexChanged);
            //
            // btnExport
            //
            this.btnExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnExport.Location = new System.Drawing.Point(720, 126);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(146, 30);
            this.btnExport.TabIndex = 6;
            this.btnExport.Text = "Export CSV";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            //
            // pnlBirthCard
            //
            this.pnlBirthCard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlBirthCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlBirthCard.Controls.Add(this.stripeBirth);
            this.pnlBirthCard.Controls.Add(this.lblBirthVal);
            this.pnlBirthCard.Controls.Add(this.capBirth);
            this.pnlBirthCard.Location = new System.Drawing.Point(34, 172);
            this.pnlBirthCard.Name = "pnlBirthCard";
            this.pnlBirthCard.Size = new System.Drawing.Size(200, 96);
            this.pnlBirthCard.TabIndex = 7;
            //
            // lblBirthVal
            //
            this.lblBirthVal.AutoSize = true;
            this.lblBirthVal.Font = new System.Drawing.Font("Segoe UI", 26F, System.Drawing.FontStyle.Bold);
            this.lblBirthVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblBirthVal.Location = new System.Drawing.Point(18, 12);
            this.lblBirthVal.Name = "lblBirthVal";
            this.lblBirthVal.Size = new System.Drawing.Size(35, 47);
            this.lblBirthVal.TabIndex = 0;
            this.lblBirthVal.Text = "0";
            //
            // capBirth
            //
            this.capBirth.AutoSize = true;
            this.capBirth.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.capBirth.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.capBirth.Location = new System.Drawing.Point(18, 66);
            this.capBirth.Name = "capBirth";
            this.capBirth.Size = new System.Drawing.Size(97, 15);
            this.capBirth.TabIndex = 1;
            this.capBirth.Text = "Births Registered";
            //
            // stripeBirth
            //
            this.stripeBirth.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.stripeBirth.Location = new System.Drawing.Point(0, 0);
            this.stripeBirth.Name = "stripeBirth";
            this.stripeBirth.Size = new System.Drawing.Size(6, 96);
            this.stripeBirth.TabIndex = 2;
            //
            // pnlMarriageCard
            //
            this.pnlMarriageCard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlMarriageCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlMarriageCard.Controls.Add(this.stripeMarriage);
            this.pnlMarriageCard.Controls.Add(this.lblMarriageVal);
            this.pnlMarriageCard.Controls.Add(this.capMarriage);
            this.pnlMarriageCard.Location = new System.Drawing.Point(246, 172);
            this.pnlMarriageCard.Name = "pnlMarriageCard";
            this.pnlMarriageCard.Size = new System.Drawing.Size(200, 96);
            this.pnlMarriageCard.TabIndex = 8;
            //
            // lblMarriageVal
            //
            this.lblMarriageVal.AutoSize = true;
            this.lblMarriageVal.Font = new System.Drawing.Font("Segoe UI", 26F, System.Drawing.FontStyle.Bold);
            this.lblMarriageVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblMarriageVal.Location = new System.Drawing.Point(18, 12);
            this.lblMarriageVal.Name = "lblMarriageVal";
            this.lblMarriageVal.Size = new System.Drawing.Size(35, 47);
            this.lblMarriageVal.TabIndex = 0;
            this.lblMarriageVal.Text = "0";
            //
            // capMarriage
            //
            this.capMarriage.AutoSize = true;
            this.capMarriage.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.capMarriage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.capMarriage.Location = new System.Drawing.Point(18, 66);
            this.capMarriage.Name = "capMarriage";
            this.capMarriage.Size = new System.Drawing.Size(120, 15);
            this.capMarriage.TabIndex = 1;
            this.capMarriage.Text = "Marriages Registered";
            //
            // stripeMarriage
            //
            this.stripeMarriage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.stripeMarriage.Location = new System.Drawing.Point(0, 0);
            this.stripeMarriage.Name = "stripeMarriage";
            this.stripeMarriage.Size = new System.Drawing.Size(6, 96);
            this.stripeMarriage.TabIndex = 2;
            //
            // pnlDeathCard
            //
            this.pnlDeathCard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlDeathCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlDeathCard.Controls.Add(this.stripeDeath);
            this.pnlDeathCard.Controls.Add(this.lblDeathVal);
            this.pnlDeathCard.Controls.Add(this.capDeath);
            this.pnlDeathCard.Location = new System.Drawing.Point(458, 172);
            this.pnlDeathCard.Name = "pnlDeathCard";
            this.pnlDeathCard.Size = new System.Drawing.Size(200, 96);
            this.pnlDeathCard.TabIndex = 9;
            //
            // lblDeathVal
            //
            this.lblDeathVal.AutoSize = true;
            this.lblDeathVal.Font = new System.Drawing.Font("Segoe UI", 26F, System.Drawing.FontStyle.Bold);
            this.lblDeathVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblDeathVal.Location = new System.Drawing.Point(18, 12);
            this.lblDeathVal.Name = "lblDeathVal";
            this.lblDeathVal.Size = new System.Drawing.Size(35, 47);
            this.lblDeathVal.TabIndex = 0;
            this.lblDeathVal.Text = "0";
            //
            // capDeath
            //
            this.capDeath.AutoSize = true;
            this.capDeath.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.capDeath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.capDeath.Location = new System.Drawing.Point(18, 66);
            this.capDeath.Name = "capDeath";
            this.capDeath.Size = new System.Drawing.Size(103, 15);
            this.capDeath.TabIndex = 1;
            this.capDeath.Text = "Deaths Registered";
            //
            // stripeDeath
            //
            this.stripeDeath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.stripeDeath.Location = new System.Drawing.Point(0, 0);
            this.stripeDeath.Name = "stripeDeath";
            this.stripeDeath.Size = new System.Drawing.Size(6, 96);
            this.stripeDeath.TabIndex = 2;
            //
            // pnlCollectCard
            //
            this.pnlCollectCard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlCollectCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlCollectCard.Controls.Add(this.stripeCollect);
            this.pnlCollectCard.Controls.Add(this.lblCollectVal);
            this.pnlCollectCard.Controls.Add(this.capCollect);
            this.pnlCollectCard.Location = new System.Drawing.Point(670, 172);
            this.pnlCollectCard.Name = "pnlCollectCard";
            this.pnlCollectCard.Size = new System.Drawing.Size(200, 96);
            this.pnlCollectCard.TabIndex = 10;
            //
            // lblCollectVal
            //
            this.lblCollectVal.AutoSize = true;
            this.lblCollectVal.Font = new System.Drawing.Font("Segoe UI", 26F, System.Drawing.FontStyle.Bold);
            this.lblCollectVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblCollectVal.Location = new System.Drawing.Point(18, 12);
            this.lblCollectVal.Name = "lblCollectVal";
            this.lblCollectVal.Size = new System.Drawing.Size(35, 47);
            this.lblCollectVal.TabIndex = 0;
            this.lblCollectVal.Text = "0";
            //
            // capCollect
            //
            this.capCollect.AutoSize = true;
            this.capCollect.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.capCollect.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.capCollect.Location = new System.Drawing.Point(18, 66);
            this.capCollect.Name = "capCollect";
            this.capCollect.Size = new System.Drawing.Size(103, 15);
            this.capCollect.TabIndex = 1;
            this.capCollect.Text = "Collections (PHP)";
            //
            // stripeCollect
            //
            this.stripeCollect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(193)))), ((int)(((byte)(7)))));
            this.stripeCollect.Location = new System.Drawing.Point(0, 0);
            this.stripeCollect.Name = "stripeCollect";
            this.stripeCollect.Size = new System.Drawing.Size(6, 96);
            this.stripeCollect.TabIndex = 2;
            //
            // lblSplit
            //
            this.lblSplit.AutoSize = true;
            this.lblSplit.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblSplit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblSplit.Location = new System.Drawing.Point(34, 286);
            this.lblSplit.Name = "lblSplit";
            this.lblSplit.Size = new System.Drawing.Size(0, 17);
            this.lblSplit.TabIndex = 11;
            //
            // lblRoster
            //
            this.lblRoster.AutoSize = true;
            this.lblRoster.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblRoster.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblRoster.Location = new System.Drawing.Point(34, 318);
            this.lblRoster.Name = "lblRoster";
            this.lblRoster.Size = new System.Drawing.Size(90, 20);
            this.lblRoster.TabIndex = 12;
            this.lblRoster.Text = "Detail Roster";
            //
            // cboType
            //
            this.cboType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboType.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboType.FormattingEnabled = true;
            this.cboType.Items.AddRange(new object[] {
            "Births",
            "Marriages",
            "Deaths"});
            this.cboType.Location = new System.Drawing.Point(180, 315);
            this.cboType.Name = "cboType";
            this.cboType.Size = new System.Drawing.Size(160, 25);
            this.cboType.TabIndex = 13;
            this.cboType.SelectedIndexChanged += new System.EventHandler(this.cboType_SelectedIndexChanged);
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
            this.grid.Location = new System.Drawing.Point(34, 350);
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(832, 250);
            this.grid.TabIndex = 14;
            //
            // ReportsPsaForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(900, 620);
            this.Controls.Add(this.grid);
            this.Controls.Add(this.cboType);
            this.Controls.Add(this.lblRoster);
            this.Controls.Add(this.lblSplit);
            this.Controls.Add(this.pnlCollectCard);
            this.Controls.Add(this.pnlDeathCard);
            this.Controls.Add(this.pnlMarriageCard);
            this.Controls.Add(this.pnlBirthCard);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.cboYear);
            this.Controls.Add(this.lblYear);
            this.Controls.Add(this.cboMonth);
            this.Controls.Add(this.lblMonth);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.titleLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ReportsPsaForm";
            this.Text = "Reports and PSA";
            this.pnlBirthCard.ResumeLayout(false);
            this.pnlBirthCard.PerformLayout();
            this.pnlMarriageCard.ResumeLayout(false);
            this.pnlMarriageCard.PerformLayout();
            this.pnlDeathCard.ResumeLayout(false);
            this.pnlDeathCard.PerformLayout();
            this.pnlCollectCard.ResumeLayout(false);
            this.pnlCollectCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Label lblMonth;
        private System.Windows.Forms.ComboBox cboMonth;
        private System.Windows.Forms.Label lblYear;
        private System.Windows.Forms.ComboBox cboYear;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Panel pnlBirthCard;
        private System.Windows.Forms.Label lblBirthVal;
        private System.Windows.Forms.Label capBirth;
        private System.Windows.Forms.Panel stripeBirth;
        private System.Windows.Forms.Panel pnlMarriageCard;
        private System.Windows.Forms.Label lblMarriageVal;
        private System.Windows.Forms.Label capMarriage;
        private System.Windows.Forms.Panel stripeMarriage;
        private System.Windows.Forms.Panel pnlDeathCard;
        private System.Windows.Forms.Label lblDeathVal;
        private System.Windows.Forms.Label capDeath;
        private System.Windows.Forms.Panel stripeDeath;
        private System.Windows.Forms.Panel pnlCollectCard;
        private System.Windows.Forms.Label lblCollectVal;
        private System.Windows.Forms.Label capCollect;
        private System.Windows.Forms.Panel stripeCollect;
        private System.Windows.Forms.Label lblSplit;
        private System.Windows.Forms.Label lblRoster;
        private System.Windows.Forms.ComboBox cboType;
        private System.Windows.Forms.DataGridView grid;
    }
}
