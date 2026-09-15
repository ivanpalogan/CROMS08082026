namespace CROMS.Forms
{
    partial class DeathRegistrationForm
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
            this.btnSave = new System.Windows.Forms.Button();
            this.btnPrint = new System.Windows.Forms.Button();
            this.grpDeceased = new System.Windows.Forms.GroupBox();
            this.lblFullName = new System.Windows.Forms.Label();
            this.txtFullName = new System.Windows.Forms.TextBox();
            this.lblSex = new System.Windows.Forms.Label();
            this.cboSex = new System.Windows.Forms.ComboBox();
            this.lblCivil = new System.Windows.Forms.Label();
            this.cboCivil = new System.Windows.Forms.ComboBox();
            this.lblAge = new System.Windows.Forms.Label();
            this.txtAge = new System.Windows.Forms.TextBox();
            this.lblCitizen = new System.Windows.Forms.Label();
            this.txtCitizen = new System.Windows.Forms.TextBox();
            this.lblDod = new System.Windows.Forms.Label();
            this.dtpDod = new System.Windows.Forms.DateTimePicker();
            this.lblTod = new System.Windows.Forms.Label();
            this.dtpTod = new System.Windows.Forms.DateTimePicker();
            this.lblPlace = new System.Windows.Forms.Label();
            this.txtPlace = new System.Windows.Forms.TextBox();
            this.lblReligion = new System.Windows.Forms.Label();
            this.txtReligion = new System.Windows.Forms.TextBox();
            this.lblBookVol = new System.Windows.Forms.Label();
            this.txtBookVol = new System.Windows.Forms.TextBox();
            this.lblBookPage = new System.Windows.Forms.Label();
            this.txtBookPage = new System.Windows.Forms.TextBox();
            this.grpCause = new System.Windows.Forms.GroupBox();
            this.lblImm = new System.Windows.Forms.Label();
            this.txtImm = new System.Windows.Forms.TextBox();
            this.lblAnt = new System.Windows.Forms.Label();
            this.txtAnt = new System.Windows.Forms.TextBox();
            this.lblUnd = new System.Windows.Forms.Label();
            this.txtUnd = new System.Windows.Forms.TextBox();
            this.lblCertifier = new System.Windows.Forms.Label();
            this.txtCertifier = new System.Windows.Forms.TextBox();
            this.lblLicense = new System.Windows.Forms.Label();
            this.txtLicense = new System.Windows.Forms.TextBox();
            this.lblDisposal = new System.Windows.Forms.Label();
            this.cboDisposal = new System.Windows.Forms.ComboBox();
            this.lblDispPlace = new System.Windows.Forms.Label();
            this.txtDispPlace = new System.Windows.Forms.TextBox();
            this.lblDispDate = new System.Windows.Forms.Label();
            this.dtpDispDate = new System.Windows.Forms.DateTimePicker();
            this.lblPermit = new System.Windows.Forms.Label();
            this.cboPermit = new System.Windows.Forms.ComboBox();
            this.lblRecent = new System.Windows.Forms.Label();
            this.dgvDeaths = new System.Windows.Forms.DataGridView();
            this.grpCert = new System.Windows.Forms.GroupBox();
            this.lblCInfName = new System.Windows.Forms.Label();
            this.txtCInfName = new System.Windows.Forms.TextBox();
            this.lblCInfRel = new System.Windows.Forms.Label();
            this.txtCInfRel = new System.Windows.Forms.TextBox();
            this.lblCInfRelOther = new System.Windows.Forms.Label();
            this.txtCInfRelOther = new System.Windows.Forms.TextBox();
            this.lblCInfDate = new System.Windows.Forms.Label();
            this.dtpCInfDate = new System.Windows.Forms.DateTimePicker();
            this.lblCInfAddr = new System.Windows.Forms.Label();
            this.txtCInfAddr = new System.Windows.Forms.TextBox();
            this.lblCPrepBy = new System.Windows.Forms.Label();
            this.txtCPrepBy = new System.Windows.Forms.TextBox();
            this.lblCPrepTitle = new System.Windows.Forms.Label();
            this.txtCPrepTitle = new System.Windows.Forms.TextBox();
            this.lblCPrepDate = new System.Windows.Forms.Label();
            this.dtpCPrepDate = new System.Windows.Forms.DateTimePicker();
            this.lblCRecvBy = new System.Windows.Forms.Label();
            this.txtCRecvBy = new System.Windows.Forms.TextBox();
            this.lblCRecvTitle = new System.Windows.Forms.Label();
            this.txtCRecvTitle = new System.Windows.Forms.TextBox();
            this.lblCRecvDate = new System.Windows.Forms.Label();
            this.dtpCRecvDate = new System.Windows.Forms.DateTimePicker();
            this.lblCRegBy = new System.Windows.Forms.Label();
            this.txtCRegBy = new System.Windows.Forms.TextBox();
            this.lblCRegTitle = new System.Windows.Forms.Label();
            this.txtCRegTitle = new System.Windows.Forms.TextBox();
            this.lblCRegDate = new System.Windows.Forms.Label();
            this.dtpCRegDate = new System.Windows.Forms.DateTimePicker();
            this.btnNew = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.grpCert.SuspendLayout();
            this.grpDeceased.SuspendLayout();
            this.grpCause.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDeaths)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblTitle.Location = new System.Drawing.Point(21, 14);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(259, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Death Registration";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblSubtitle.Location = new System.Drawing.Point(22, 49);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(288, 15);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "MUNICIPAL FORM 103  •  BURIAL & TRANSFER PERMIT";
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(1106, 19);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(137, 35);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "Register Death";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnPrint
            // 
            this.btnPrint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPrint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrint.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPrint.Location = new System.Drawing.Point(1250, 19);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(173, 35);
            this.btnPrint.TabIndex = 3;
            this.btnPrint.Text = "Print COD + Burial Permit";
            this.btnPrint.UseVisualStyleBackColor = true;
            this.btnPrint.Click += new System.EventHandler(this.btnPrint_Click);
            // 
            // grpDeceased
            // 
            this.grpDeceased.Controls.Add(this.lblFullName);
            this.grpDeceased.Controls.Add(this.txtFullName);
            this.grpDeceased.Controls.Add(this.lblSex);
            this.grpDeceased.Controls.Add(this.cboSex);
            this.grpDeceased.Controls.Add(this.lblCivil);
            this.grpDeceased.Controls.Add(this.cboCivil);
            this.grpDeceased.Controls.Add(this.lblAge);
            this.grpDeceased.Controls.Add(this.txtAge);
            this.grpDeceased.Controls.Add(this.lblCitizen);
            this.grpDeceased.Controls.Add(this.txtCitizen);
            this.grpDeceased.Controls.Add(this.lblDod);
            this.grpDeceased.Controls.Add(this.dtpDod);
            this.grpDeceased.Controls.Add(this.lblTod);
            this.grpDeceased.Controls.Add(this.dtpTod);
            this.grpDeceased.Controls.Add(this.lblPlace);
            this.grpDeceased.Controls.Add(this.txtPlace);
            this.grpDeceased.Controls.Add(this.lblReligion);
            this.grpDeceased.Controls.Add(this.txtReligion);
            this.grpDeceased.Controls.Add(this.lblBookVol);
            this.grpDeceased.Controls.Add(this.txtBookVol);
            this.grpDeceased.Controls.Add(this.lblBookPage);
            this.grpDeceased.Controls.Add(this.txtBookPage);
            this.grpDeceased.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.grpDeceased.Location = new System.Drawing.Point(21, 71);
            this.grpDeceased.Name = "grpDeceased";
            this.grpDeceased.Size = new System.Drawing.Size(686, 452);
            this.grpDeceased.TabIndex = 4;
            this.grpDeceased.TabStop = false;
            this.grpDeceased.Text = "Deceased Information (Form 103)";
            // 
            // lblFullName
            // 
            this.lblFullName.AutoSize = true;
            this.lblFullName.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblFullName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblFullName.Location = new System.Drawing.Point(17, 26);
            this.lblFullName.Name = "lblFullName";
            this.lblFullName.Size = new System.Drawing.Size(70, 13);
            this.lblFullName.TabIndex = 0;
            this.lblFullName.Text = "FULL NAME";
            // 
            // txtFullName
            // 
            this.txtFullName.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtFullName.Location = new System.Drawing.Point(17, 42);
            this.txtFullName.Name = "txtFullName";
            this.txtFullName.Size = new System.Drawing.Size(652, 25);
            this.txtFullName.TabIndex = 1;
            // 
            // lblSex
            // 
            this.lblSex.AutoSize = true;
            this.lblSex.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblSex.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblSex.Location = new System.Drawing.Point(17, 78);
            this.lblSex.Name = "lblSex";
            this.lblSex.Size = new System.Drawing.Size(26, 13);
            this.lblSex.TabIndex = 2;
            this.lblSex.Text = "SEX";
            // 
            // cboSex
            // 
            this.cboSex.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSex.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cboSex.Location = new System.Drawing.Point(17, 94);
            this.cboSex.Name = "cboSex";
            this.cboSex.Size = new System.Drawing.Size(309, 25);
            this.cboSex.TabIndex = 3;
            // 
            // lblCivil
            // 
            this.lblCivil.AutoSize = true;
            this.lblCivil.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblCivil.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblCivil.Location = new System.Drawing.Point(351, 78);
            this.lblCivil.Name = "lblCivil";
            this.lblCivil.Size = new System.Drawing.Size(74, 13);
            this.lblCivil.TabIndex = 4;
            this.lblCivil.Text = "CIVIL STATUS";
            // 
            // cboCivil
            // 
            this.cboCivil.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCivil.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cboCivil.Location = new System.Drawing.Point(351, 94);
            this.cboCivil.Name = "cboCivil";
            this.cboCivil.Size = new System.Drawing.Size(309, 25);
            this.cboCivil.TabIndex = 5;
            // 
            // lblAge
            // 
            this.lblAge.AutoSize = true;
            this.lblAge.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblAge.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblAge.Location = new System.Drawing.Point(17, 130);
            this.lblAge.Name = "lblAge";
            this.lblAge.Size = new System.Drawing.Size(83, 13);
            this.lblAge.TabIndex = 6;
            this.lblAge.Text = "AGE AT DEATH";
            // 
            // txtAge
            // 
            this.txtAge.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtAge.Location = new System.Drawing.Point(17, 146);
            this.txtAge.Name = "txtAge";
            this.txtAge.Size = new System.Drawing.Size(309, 25);
            this.txtAge.TabIndex = 7;
            // 
            // lblCitizen
            // 
            this.lblCitizen.AutoSize = true;
            this.lblCitizen.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblCitizen.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblCitizen.Location = new System.Drawing.Point(351, 130);
            this.lblCitizen.Name = "lblCitizen";
            this.lblCitizen.Size = new System.Drawing.Size(72, 13);
            this.lblCitizen.TabIndex = 8;
            this.lblCitizen.Text = "CITIZENSHIP";
            // 
            // txtCitizen
            // 
            this.txtCitizen.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtCitizen.Location = new System.Drawing.Point(351, 146);
            this.txtCitizen.Name = "txtCitizen";
            this.txtCitizen.Size = new System.Drawing.Size(309, 25);
            this.txtCitizen.TabIndex = 9;
            // 
            // lblDod
            // 
            this.lblDod.AutoSize = true;
            this.lblDod.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblDod.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblDod.Location = new System.Drawing.Point(17, 182);
            this.lblDod.Name = "lblDod";
            this.lblDod.Size = new System.Drawing.Size(89, 13);
            this.lblDod.TabIndex = 10;
            this.lblDod.Text = "DATE OF DEATH";
            // 
            // dtpDod
            // 
            this.dtpDod.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.dtpDod.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDod.Location = new System.Drawing.Point(17, 198);
            this.dtpDod.Name = "dtpDod";
            this.dtpDod.Size = new System.Drawing.Size(309, 25);
            this.dtpDod.TabIndex = 11;
            // 
            // lblTod
            // 
            this.lblTod.AutoSize = true;
            this.lblTod.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblTod.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblTod.Location = new System.Drawing.Point(351, 182);
            this.lblTod.Name = "lblTod";
            this.lblTod.Size = new System.Drawing.Size(88, 13);
            this.lblTod.TabIndex = 12;
            this.lblTod.Text = "TIME OF DEATH";
            // 
            // dtpTod
            // 
            this.dtpTod.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.dtpTod.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpTod.Location = new System.Drawing.Point(351, 198);
            this.dtpTod.Name = "dtpTod";
            this.dtpTod.ShowUpDown = true;
            this.dtpTod.Size = new System.Drawing.Size(309, 25);
            this.dtpTod.TabIndex = 13;
            // 
            // lblPlace
            // 
            this.lblPlace.AutoSize = true;
            this.lblPlace.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblPlace.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblPlace.Location = new System.Drawing.Point(17, 234);
            this.lblPlace.Name = "lblPlace";
            this.lblPlace.Size = new System.Drawing.Size(96, 13);
            this.lblPlace.TabIndex = 14;
            this.lblPlace.Text = "PLACE OF DEATH";
            // 
            // txtPlace
            // 
            this.txtPlace.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtPlace.Location = new System.Drawing.Point(17, 250);
            this.txtPlace.Name = "txtPlace";
            this.txtPlace.Size = new System.Drawing.Size(652, 25);
            this.txtPlace.TabIndex = 15;
            // 
            // lblReligion
            // 
            this.lblReligion.AutoSize = true;
            this.lblReligion.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblReligion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblReligion.Location = new System.Drawing.Point(17, 306);
            this.lblReligion.Name = "lblReligion";
            this.lblReligion.Size = new System.Drawing.Size(57, 13);
            this.lblReligion.TabIndex = 16;
            this.lblReligion.Text = "RELIGION";
            // 
            // txtReligion
            // 
            this.txtReligion.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtReligion.Location = new System.Drawing.Point(17, 322);
            this.txtReligion.Name = "txtReligion";
            this.txtReligion.Size = new System.Drawing.Size(652, 25);
            this.txtReligion.TabIndex = 17;
            //
            // lblBookVol
            //
            this.lblBookVol.AutoSize = true;
            this.lblBookVol.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblBookVol.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblBookVol.Location = new System.Drawing.Point(17, 364);
            this.lblBookVol.Name = "lblBookVol";
            this.lblBookVol.Size = new System.Drawing.Size(93, 13);
            this.lblBookVol.TabIndex = 18;
            this.lblBookVol.Text = "BOOK / VOLUME";
            //
            // txtBookVol
            //
            this.txtBookVol.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtBookVol.Location = new System.Drawing.Point(17, 380);
            this.txtBookVol.Name = "txtBookVol";
            this.txtBookVol.Size = new System.Drawing.Size(315, 25);
            this.txtBookVol.TabIndex = 19;
            //
            // lblBookPage
            //
            this.lblBookPage.AutoSize = true;
            this.lblBookPage.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblBookPage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblBookPage.Location = new System.Drawing.Point(351, 364);
            this.lblBookPage.Name = "lblBookPage";
            this.lblBookPage.Size = new System.Drawing.Size(69, 13);
            this.lblBookPage.TabIndex = 20;
            this.lblBookPage.Text = "BOOK PAGE";
            //
            // txtBookPage
            //
            this.txtBookPage.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtBookPage.Location = new System.Drawing.Point(351, 380);
            this.txtBookPage.Name = "txtBookPage";
            this.txtBookPage.Size = new System.Drawing.Size(315, 25);
            this.txtBookPage.TabIndex = 21;
            // 
            // grpCause
            // 
            this.grpCause.Controls.Add(this.lblImm);
            this.grpCause.Controls.Add(this.txtImm);
            this.grpCause.Controls.Add(this.lblAnt);
            this.grpCause.Controls.Add(this.txtAnt);
            this.grpCause.Controls.Add(this.lblUnd);
            this.grpCause.Controls.Add(this.txtUnd);
            this.grpCause.Controls.Add(this.lblCertifier);
            this.grpCause.Controls.Add(this.txtCertifier);
            this.grpCause.Controls.Add(this.lblLicense);
            this.grpCause.Controls.Add(this.txtLicense);
            this.grpCause.Controls.Add(this.lblDisposal);
            this.grpCause.Controls.Add(this.cboDisposal);
            this.grpCause.Controls.Add(this.lblDispPlace);
            this.grpCause.Controls.Add(this.txtDispPlace);
            this.grpCause.Controls.Add(this.lblDispDate);
            this.grpCause.Controls.Add(this.dtpDispDate);
            this.grpCause.Controls.Add(this.lblPermit);
            this.grpCause.Controls.Add(this.cboPermit);
            this.grpCause.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.grpCause.Location = new System.Drawing.Point(729, 71);
            this.grpCause.Name = "grpCause";
            this.grpCause.Size = new System.Drawing.Size(699, 407);
            this.grpCause.TabIndex = 5;
            this.grpCause.TabStop = false;
            this.grpCause.Text = "Cause of Death & Disposal";
            // 
            // lblImm
            // 
            this.lblImm.AutoSize = true;
            this.lblImm.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblImm.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblImm.Location = new System.Drawing.Point(17, 26);
            this.lblImm.Name = "lblImm";
            this.lblImm.Size = new System.Drawing.Size(106, 13);
            this.lblImm.TabIndex = 0;
            this.lblImm.Text = "IMMEDIATE CAUSE";
            // 
            // txtImm
            // 
            this.txtImm.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtImm.Location = new System.Drawing.Point(17, 42);
            this.txtImm.Name = "txtImm";
            this.txtImm.Size = new System.Drawing.Size(666, 25);
            this.txtImm.TabIndex = 1;
            // 
            // lblAnt
            // 
            this.lblAnt.AutoSize = true;
            this.lblAnt.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblAnt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblAnt.Location = new System.Drawing.Point(17, 78);
            this.lblAnt.Name = "lblAnt";
            this.lblAnt.Size = new System.Drawing.Size(116, 13);
            this.lblAnt.TabIndex = 2;
            this.lblAnt.Text = "ANTECEDENT CAUSE";
            // 
            // txtAnt
            // 
            this.txtAnt.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtAnt.Location = new System.Drawing.Point(17, 94);
            this.txtAnt.Name = "txtAnt";
            this.txtAnt.Size = new System.Drawing.Size(666, 25);
            this.txtAnt.TabIndex = 3;
            // 
            // lblUnd
            // 
            this.lblUnd.AutoSize = true;
            this.lblUnd.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblUnd.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblUnd.Location = new System.Drawing.Point(17, 130);
            this.lblUnd.Name = "lblUnd";
            this.lblUnd.Size = new System.Drawing.Size(115, 13);
            this.lblUnd.TabIndex = 4;
            this.lblUnd.Text = "UNDERLYING CAUSE";
            // 
            // txtUnd
            // 
            this.txtUnd.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtUnd.Location = new System.Drawing.Point(17, 146);
            this.txtUnd.Name = "txtUnd";
            this.txtUnd.Size = new System.Drawing.Size(666, 25);
            this.txtUnd.TabIndex = 5;
            // 
            // lblCertifier
            // 
            this.lblCertifier.AutoSize = true;
            this.lblCertifier.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblCertifier.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblCertifier.Location = new System.Drawing.Point(17, 182);
            this.lblCertifier.Name = "lblCertifier";
            this.lblCertifier.Size = new System.Drawing.Size(110, 13);
            this.lblCertifier.TabIndex = 6;
            this.lblCertifier.Text = "MEDICAL CERTIFIER";
            // 
            // txtCertifier
            // 
            this.txtCertifier.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtCertifier.Location = new System.Drawing.Point(17, 198);
            this.txtCertifier.Name = "txtCertifier";
            this.txtCertifier.Size = new System.Drawing.Size(326, 25);
            this.txtCertifier.TabIndex = 7;
            // 
            // lblLicense
            // 
            this.lblLicense.AutoSize = true;
            this.lblLicense.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblLicense.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblLicense.Location = new System.Drawing.Point(360, 182);
            this.lblLicense.Name = "lblLicense";
            this.lblLicense.Size = new System.Drawing.Size(73, 13);
            this.lblLicense.TabIndex = 8;
            this.lblLicense.Text = "LICENSE NO.";
            // 
            // txtLicense
            // 
            this.txtLicense.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtLicense.Location = new System.Drawing.Point(360, 198);
            this.txtLicense.Name = "txtLicense";
            this.txtLicense.Size = new System.Drawing.Size(323, 25);
            this.txtLicense.TabIndex = 9;
            // 
            // lblDisposal
            // 
            this.lblDisposal.AutoSize = true;
            this.lblDisposal.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblDisposal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblDisposal.Location = new System.Drawing.Point(17, 234);
            this.lblDisposal.Name = "lblDisposal";
            this.lblDisposal.Size = new System.Drawing.Size(109, 13);
            this.lblDisposal.TabIndex = 10;
            this.lblDisposal.Text = "DISPOSAL METHOD";
            // 
            // cboDisposal
            // 
            this.cboDisposal.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDisposal.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cboDisposal.Location = new System.Drawing.Point(17, 250);
            this.cboDisposal.Name = "cboDisposal";
            this.cboDisposal.Size = new System.Drawing.Size(326, 25);
            this.cboDisposal.TabIndex = 11;
            // 
            // lblDispPlace
            // 
            this.lblDispPlace.AutoSize = true;
            this.lblDispPlace.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblDispPlace.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblDispPlace.Location = new System.Drawing.Point(360, 234);
            this.lblDispPlace.Name = "lblDispPlace";
            this.lblDispPlace.Size = new System.Drawing.Size(113, 13);
            this.lblDispPlace.TabIndex = 12;
            this.lblDispPlace.Text = "PLACE OF DISPOSAL";
            // 
            // txtDispPlace
            // 
            this.txtDispPlace.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtDispPlace.Location = new System.Drawing.Point(360, 250);
            this.txtDispPlace.Name = "txtDispPlace";
            this.txtDispPlace.Size = new System.Drawing.Size(323, 25);
            this.txtDispPlace.TabIndex = 13;
            // 
            // lblDispDate
            // 
            this.lblDispDate.AutoSize = true;
            this.lblDispDate.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblDispDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblDispDate.Location = new System.Drawing.Point(17, 286);
            this.lblDispDate.Name = "lblDispDate";
            this.lblDispDate.Size = new System.Drawing.Size(106, 13);
            this.lblDispDate.TabIndex = 14;
            this.lblDispDate.Text = "DATE OF DISPOSAL";
            // 
            // dtpDispDate
            // 
            this.dtpDispDate.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.dtpDispDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDispDate.Location = new System.Drawing.Point(17, 302);
            this.dtpDispDate.Name = "dtpDispDate";
            this.dtpDispDate.ShowCheckBox = true;
            this.dtpDispDate.Size = new System.Drawing.Size(326, 25);
            this.dtpDispDate.TabIndex = 15;
            // 
            // lblPermit
            // 
            this.lblPermit.AutoSize = true;
            this.lblPermit.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblPermit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblPermit.Location = new System.Drawing.Point(360, 286);
            this.lblPermit.Name = "lblPermit";
            this.lblPermit.Size = new System.Drawing.Size(76, 13);
            this.lblPermit.TabIndex = 16;
            this.lblPermit.Text = "PERMIT TYPE";
            // 
            // cboPermit
            // 
            this.cboPermit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPermit.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cboPermit.Location = new System.Drawing.Point(360, 302);
            this.cboPermit.Name = "cboPermit";
            this.cboPermit.Size = new System.Drawing.Size(323, 25);
            this.cboPermit.TabIndex = 17;
            //
            // grpCert
            //
            this.grpCert.Controls.Add(this.lblCInfName);
            this.grpCert.Controls.Add(this.txtCInfName);
            this.grpCert.Controls.Add(this.lblCInfRel);
            this.grpCert.Controls.Add(this.txtCInfRel);
            this.grpCert.Controls.Add(this.lblCInfRelOther);
            this.grpCert.Controls.Add(this.txtCInfRelOther);
            this.grpCert.Controls.Add(this.lblCInfDate);
            this.grpCert.Controls.Add(this.dtpCInfDate);
            this.grpCert.Controls.Add(this.lblCInfAddr);
            this.grpCert.Controls.Add(this.txtCInfAddr);
            this.grpCert.Controls.Add(this.lblCPrepBy);
            this.grpCert.Controls.Add(this.txtCPrepBy);
            this.grpCert.Controls.Add(this.lblCPrepTitle);
            this.grpCert.Controls.Add(this.txtCPrepTitle);
            this.grpCert.Controls.Add(this.lblCPrepDate);
            this.grpCert.Controls.Add(this.dtpCPrepDate);
            this.grpCert.Controls.Add(this.lblCRecvBy);
            this.grpCert.Controls.Add(this.txtCRecvBy);
            this.grpCert.Controls.Add(this.lblCRecvTitle);
            this.grpCert.Controls.Add(this.txtCRecvTitle);
            this.grpCert.Controls.Add(this.lblCRecvDate);
            this.grpCert.Controls.Add(this.dtpCRecvDate);
            this.grpCert.Controls.Add(this.lblCRegBy);
            this.grpCert.Controls.Add(this.txtCRegBy);
            this.grpCert.Controls.Add(this.lblCRegTitle);
            this.grpCert.Controls.Add(this.txtCRegTitle);
            this.grpCert.Controls.Add(this.lblCRegDate);
            this.grpCert.Controls.Add(this.dtpCRegDate);
            this.grpCert.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.grpCert.Location = new System.Drawing.Point(21, 537);
            this.grpCert.Name = "grpCert";
            this.grpCert.Size = new System.Drawing.Size(1407, 347);
            this.grpCert.TabIndex = 4;
            this.grpCert.TabStop = false;
            this.grpCert.Text = "Certification (Items 26-29)";
            //
            // lblCInfName
            //
            this.lblCInfName.AutoSize = true;
            this.lblCInfName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCInfName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCInfName.Location = new System.Drawing.Point(17, 28);
            this.lblCInfName.Name = "lblCInfName";
            this.lblCInfName.Text = "Informant - Name in Print";
            //
            // txtCInfName
            //
            this.txtCInfName.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtCInfName.Location = new System.Drawing.Point(17, 48);
            this.txtCInfName.Name = "txtCInfName";
            this.txtCInfName.Size = new System.Drawing.Size(330, 25);
            //
            // lblCInfRel
            //
            this.lblCInfRel.AutoSize = true;
            this.lblCInfRel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCInfRel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCInfRel.Location = new System.Drawing.Point(361, 28);
            this.lblCInfRel.Name = "lblCInfRel";
            this.lblCInfRel.Text = "Relationship to the Deceased";
            //
            // txtCInfRel
            //
            this.txtCInfRel.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtCInfRel.Location = new System.Drawing.Point(361, 48);
            this.txtCInfRel.Name = "txtCInfRel";
            this.txtCInfRel.Size = new System.Drawing.Size(330, 25);
            //
            // lblCInfRelOther
            //
            this.lblCInfRelOther.AutoSize = true;
            this.lblCInfRelOther.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCInfRelOther.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCInfRelOther.Location = new System.Drawing.Point(705, 28);
            this.lblCInfRelOther.Name = "lblCInfRelOther";
            this.lblCInfRelOther.Text = "If Others, specify";
            //
            // txtCInfRelOther
            //
            this.txtCInfRelOther.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtCInfRelOther.Location = new System.Drawing.Point(705, 48);
            this.txtCInfRelOther.Name = "txtCInfRelOther";
            this.txtCInfRelOther.Size = new System.Drawing.Size(330, 25);
            //
            // lblCInfDate
            //
            this.lblCInfDate.AutoSize = true;
            this.lblCInfDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCInfDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCInfDate.Location = new System.Drawing.Point(1049, 28);
            this.lblCInfDate.Name = "lblCInfDate";
            this.lblCInfDate.Text = "Date Signed";
            //
            // dtpCInfDate
            //
            this.dtpCInfDate.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.dtpCInfDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpCInfDate.ShowCheckBox = true;
            this.dtpCInfDate.Checked = false;
            this.dtpCInfDate.Location = new System.Drawing.Point(1049, 48);
            this.dtpCInfDate.Name = "dtpCInfDate";
            this.dtpCInfDate.Size = new System.Drawing.Size(240, 25);
            //
            // lblCInfAddr
            //
            this.lblCInfAddr.AutoSize = true;
            this.lblCInfAddr.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCInfAddr.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCInfAddr.Location = new System.Drawing.Point(17, 92);
            this.lblCInfAddr.Name = "lblCInfAddr";
            this.lblCInfAddr.Text = "Informant - Address";
            //
            // txtCInfAddr
            //
            this.txtCInfAddr.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtCInfAddr.Location = new System.Drawing.Point(17, 112);
            this.txtCInfAddr.Name = "txtCInfAddr";
            this.txtCInfAddr.Size = new System.Drawing.Size(674, 25);
            //
            // lblCPrepBy
            //
            this.lblCPrepBy.AutoSize = true;
            this.lblCPrepBy.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCPrepBy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCPrepBy.Location = new System.Drawing.Point(17, 156);
            this.lblCPrepBy.Name = "lblCPrepBy";
            this.lblCPrepBy.Text = "Prepared By - Name in Print";
            //
            // txtCPrepBy
            //
            this.txtCPrepBy.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtCPrepBy.Location = new System.Drawing.Point(17, 176);
            this.txtCPrepBy.Name = "txtCPrepBy";
            this.txtCPrepBy.Size = new System.Drawing.Size(330, 25);
            //
            // lblCPrepTitle
            //
            this.lblCPrepTitle.AutoSize = true;
            this.lblCPrepTitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCPrepTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCPrepTitle.Location = new System.Drawing.Point(361, 156);
            this.lblCPrepTitle.Name = "lblCPrepTitle";
            this.lblCPrepTitle.Text = "Prepared By - Title or Position";
            //
            // txtCPrepTitle
            //
            this.txtCPrepTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtCPrepTitle.Location = new System.Drawing.Point(361, 176);
            this.txtCPrepTitle.Name = "txtCPrepTitle";
            this.txtCPrepTitle.Size = new System.Drawing.Size(330, 25);
            //
            // lblCPrepDate
            //
            this.lblCPrepDate.AutoSize = true;
            this.lblCPrepDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCPrepDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCPrepDate.Location = new System.Drawing.Point(705, 156);
            this.lblCPrepDate.Name = "lblCPrepDate";
            this.lblCPrepDate.Text = "Prepared By - Date";
            //
            // dtpCPrepDate
            //
            this.dtpCPrepDate.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.dtpCPrepDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpCPrepDate.ShowCheckBox = true;
            this.dtpCPrepDate.Checked = false;
            this.dtpCPrepDate.Location = new System.Drawing.Point(705, 176);
            this.dtpCPrepDate.Name = "dtpCPrepDate";
            this.dtpCPrepDate.Size = new System.Drawing.Size(240, 25);
            //
            // lblCRecvBy
            //
            this.lblCRecvBy.AutoSize = true;
            this.lblCRecvBy.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCRecvBy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCRecvBy.Location = new System.Drawing.Point(17, 220);
            this.lblCRecvBy.Name = "lblCRecvBy";
            this.lblCRecvBy.Text = "Received By - Name in Print";
            //
            // txtCRecvBy
            //
            this.txtCRecvBy.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtCRecvBy.Location = new System.Drawing.Point(17, 240);
            this.txtCRecvBy.Name = "txtCRecvBy";
            this.txtCRecvBy.Size = new System.Drawing.Size(330, 25);
            //
            // lblCRecvTitle
            //
            this.lblCRecvTitle.AutoSize = true;
            this.lblCRecvTitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCRecvTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCRecvTitle.Location = new System.Drawing.Point(361, 220);
            this.lblCRecvTitle.Name = "lblCRecvTitle";
            this.lblCRecvTitle.Text = "Received By - Title or Position";
            //
            // txtCRecvTitle
            //
            this.txtCRecvTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtCRecvTitle.Location = new System.Drawing.Point(361, 240);
            this.txtCRecvTitle.Name = "txtCRecvTitle";
            this.txtCRecvTitle.Size = new System.Drawing.Size(330, 25);
            //
            // lblCRecvDate
            //
            this.lblCRecvDate.AutoSize = true;
            this.lblCRecvDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCRecvDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCRecvDate.Location = new System.Drawing.Point(705, 220);
            this.lblCRecvDate.Name = "lblCRecvDate";
            this.lblCRecvDate.Text = "Received By - Date";
            //
            // dtpCRecvDate
            //
            this.dtpCRecvDate.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.dtpCRecvDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpCRecvDate.ShowCheckBox = true;
            this.dtpCRecvDate.Checked = false;
            this.dtpCRecvDate.Location = new System.Drawing.Point(705, 240);
            this.dtpCRecvDate.Name = "dtpCRecvDate";
            this.dtpCRecvDate.Size = new System.Drawing.Size(240, 25);
            //
            // lblCRegBy
            //
            this.lblCRegBy.AutoSize = true;
            this.lblCRegBy.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCRegBy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCRegBy.Location = new System.Drawing.Point(17, 284);
            this.lblCRegBy.Name = "lblCRegBy";
            this.lblCRegBy.Text = "Registered By - Name in Print";
            //
            // txtCRegBy
            //
            this.txtCRegBy.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtCRegBy.Location = new System.Drawing.Point(17, 304);
            this.txtCRegBy.Name = "txtCRegBy";
            this.txtCRegBy.Size = new System.Drawing.Size(330, 25);
            //
            // lblCRegTitle
            //
            this.lblCRegTitle.AutoSize = true;
            this.lblCRegTitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCRegTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCRegTitle.Location = new System.Drawing.Point(361, 284);
            this.lblCRegTitle.Name = "lblCRegTitle";
            this.lblCRegTitle.Text = "Registered By - Title or Position";
            //
            // txtCRegTitle
            //
            this.txtCRegTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtCRegTitle.Location = new System.Drawing.Point(361, 304);
            this.txtCRegTitle.Name = "txtCRegTitle";
            this.txtCRegTitle.Size = new System.Drawing.Size(330, 25);
            //
            // lblCRegDate
            //
            this.lblCRegDate.AutoSize = true;
            this.lblCRegDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCRegDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCRegDate.Location = new System.Drawing.Point(705, 284);
            this.lblCRegDate.Name = "lblCRegDate";
            this.lblCRegDate.Text = "Registered By - Date";
            //
            // dtpCRegDate
            //
            this.dtpCRegDate.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.dtpCRegDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpCRegDate.ShowCheckBox = true;
            this.dtpCRegDate.Checked = false;
            this.dtpCRegDate.Location = new System.Drawing.Point(705, 304);
            this.dtpCRegDate.Name = "dtpCRegDate";
            this.dtpCRegDate.Size = new System.Drawing.Size(240, 25);
            // 
            // lblRecent
            // 
            this.lblRecent.AutoSize = true;
            this.lblRecent.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblRecent.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblRecent.Location = new System.Drawing.Point(21, 900);
            this.lblRecent.Name = "lblRecent";
            this.lblRecent.Size = new System.Drawing.Size(205, 17);
            this.lblRecent.TabIndex = 6;
            this.lblRecent.Text = "RECENT DEATH REGISTRATIONS";
            // 
            // dgvDeaths
            // 
            this.dgvDeaths.AllowUserToAddRows = false;
            this.dgvDeaths.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvDeaths.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvDeaths.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvDeaths.BackgroundColor = System.Drawing.Color.White;
            this.dgvDeaths.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDeaths.Location = new System.Drawing.Point(5, 920);
            this.dgvDeaths.Name = "dgvDeaths";
            this.dgvDeaths.ReadOnly = true;
            this.dgvDeaths.RowHeadersVisible = false;
            this.dgvDeaths.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvDeaths.Size = new System.Drawing.Size(1407, 300);
            this.dgvDeaths.TabIndex = 7;
            //
            // btnNew
            //
            this.btnNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNew.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnNew.Location = new System.Drawing.Point(1101, 525);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(100, 30);
            this.btnNew.TabIndex = 8;
            this.btnNew.Text = "New";
            this.btnNew.UseVisualStyleBackColor = true;
            this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
            //
            // btnUpdate
            //
            this.btnUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUpdate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnUpdate.Location = new System.Drawing.Point(1207, 525);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(100, 30);
            this.btnUpdate.TabIndex = 9;
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            //
            // btnDelete
            //
            this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDelete.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnDelete.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.btnDelete.Location = new System.Drawing.Point(1313, 525);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(100, 30);
            this.btnDelete.TabIndex = 10;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            //
            // DeathRegistrationForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(1449, 1245);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnPrint);
            this.Controls.Add(this.grpDeceased);
            this.Controls.Add(this.grpCause);
            this.Controls.Add(this.lblRecent);
            this.Controls.Add(this.btnNew);
            this.Controls.Add(this.btnUpdate);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.grpCert);
            this.Controls.Add(this.dgvDeaths);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "DeathRegistrationForm";
            this.Text = "Death Registration";
            this.grpCert.ResumeLayout(false);
            this.grpCert.PerformLayout();
            this.grpDeceased.ResumeLayout(false);
            this.grpDeceased.PerformLayout();
            this.grpCause.ResumeLayout(false);
            this.grpCause.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDeaths)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnPrint;
        private System.Windows.Forms.GroupBox grpDeceased;
        private System.Windows.Forms.Label lblFullName;
        private System.Windows.Forms.TextBox txtFullName;
        private System.Windows.Forms.Label lblSex;
        private System.Windows.Forms.ComboBox cboSex;
        private System.Windows.Forms.Label lblCivil;
        private System.Windows.Forms.ComboBox cboCivil;
        private System.Windows.Forms.Label lblAge;
        private System.Windows.Forms.TextBox txtAge;
        private System.Windows.Forms.Label lblCitizen;
        private System.Windows.Forms.TextBox txtCitizen;
        private System.Windows.Forms.Label lblDod;
        private System.Windows.Forms.DateTimePicker dtpDod;
        private System.Windows.Forms.Label lblTod;
        private System.Windows.Forms.DateTimePicker dtpTod;
        private System.Windows.Forms.Label lblPlace;
        private System.Windows.Forms.TextBox txtPlace;
        private System.Windows.Forms.Label lblReligion;
        private System.Windows.Forms.TextBox txtReligion;
        private System.Windows.Forms.Label lblBookVol;
        private System.Windows.Forms.TextBox txtBookVol;
        private System.Windows.Forms.Label lblBookPage;
        private System.Windows.Forms.TextBox txtBookPage;
        private System.Windows.Forms.GroupBox grpCause;
        private System.Windows.Forms.Label lblImm;
        private System.Windows.Forms.TextBox txtImm;
        private System.Windows.Forms.Label lblAnt;
        private System.Windows.Forms.TextBox txtAnt;
        private System.Windows.Forms.Label lblUnd;
        private System.Windows.Forms.TextBox txtUnd;
        private System.Windows.Forms.Label lblCertifier;
        private System.Windows.Forms.TextBox txtCertifier;
        private System.Windows.Forms.Label lblLicense;
        private System.Windows.Forms.TextBox txtLicense;
        private System.Windows.Forms.Label lblDisposal;
        private System.Windows.Forms.ComboBox cboDisposal;
        private System.Windows.Forms.Label lblDispPlace;
        private System.Windows.Forms.TextBox txtDispPlace;
        private System.Windows.Forms.Label lblDispDate;
        private System.Windows.Forms.DateTimePicker dtpDispDate;
        private System.Windows.Forms.Label lblPermit;
        private System.Windows.Forms.ComboBox cboPermit;
        private System.Windows.Forms.Label lblRecent;
        private System.Windows.Forms.DataGridView dgvDeaths;
        private System.Windows.Forms.GroupBox grpCert;
        private System.Windows.Forms.Label lblCInfName;
        private System.Windows.Forms.TextBox txtCInfName;
        private System.Windows.Forms.Label lblCInfRel;
        private System.Windows.Forms.TextBox txtCInfRel;
        private System.Windows.Forms.Label lblCInfRelOther;
        private System.Windows.Forms.TextBox txtCInfRelOther;
        private System.Windows.Forms.Label lblCInfDate;
        private System.Windows.Forms.DateTimePicker dtpCInfDate;
        private System.Windows.Forms.Label lblCInfAddr;
        private System.Windows.Forms.TextBox txtCInfAddr;
        private System.Windows.Forms.Label lblCPrepBy;
        private System.Windows.Forms.TextBox txtCPrepBy;
        private System.Windows.Forms.Label lblCPrepTitle;
        private System.Windows.Forms.TextBox txtCPrepTitle;
        private System.Windows.Forms.Label lblCPrepDate;
        private System.Windows.Forms.DateTimePicker dtpCPrepDate;
        private System.Windows.Forms.Label lblCRecvBy;
        private System.Windows.Forms.TextBox txtCRecvBy;
        private System.Windows.Forms.Label lblCRecvTitle;
        private System.Windows.Forms.TextBox txtCRecvTitle;
        private System.Windows.Forms.Label lblCRecvDate;
        private System.Windows.Forms.DateTimePicker dtpCRecvDate;
        private System.Windows.Forms.Label lblCRegBy;
        private System.Windows.Forms.TextBox txtCRegBy;
        private System.Windows.Forms.Label lblCRegTitle;
        private System.Windows.Forms.TextBox txtCRegTitle;
        private System.Windows.Forms.Label lblCRegDate;
        private System.Windows.Forms.DateTimePicker dtpCRegDate;
        private System.Windows.Forms.Button btnNew;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
    }
}
