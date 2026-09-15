namespace CROMS.Forms
{
    partial class UsersAuditForm
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabUsers = new System.Windows.Forms.TabPage();
            this.gridUsers = new System.Windows.Forms.DataGridView();
            this.pnlEdit = new System.Windows.Forms.Panel();
            this.lblUsername = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.lblFullName = new System.Windows.Forms.Label();
            this.txtFullName = new System.Windows.Forms.TextBox();
            this.lblRole = new System.Windows.Forms.Label();
            this.cboRole = new System.Windows.Forms.ComboBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblConfirm = new System.Windows.Forms.Label();
            this.txtConfirm = new System.Windows.Forms.TextBox();
            this.chkActive = new System.Windows.Forms.CheckBox();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnNew = new System.Windows.Forms.Button();
            this.tabAudit = new System.Windows.Forms.TabPage();
            this.gridAudit = new System.Windows.Forms.DataGridView();
            this.tabBiodata = new System.Windows.Forms.TabPage();
            this.gridBiodata = new System.Windows.Forms.DataGridView();
            this.pnlBiodataEdit = new System.Windows.Forms.Panel();
            this.lblBioHint = new System.Windows.Forms.Label();
            this.lblBioUser = new System.Windows.Forms.Label();
            this.cboBioUser = new System.Windows.Forms.ComboBox();
            this.lblBioEmployeeNo = new System.Windows.Forms.Label();
            this.txtBioEmployeeNo = new System.Windows.Forms.TextBox();
            this.lblBioStatus = new System.Windows.Forms.Label();
            this.cboBioStatus = new System.Windows.Forms.ComboBox();
            this.lblBioPosition = new System.Windows.Forms.Label();
            this.txtBioPosition = new System.Windows.Forms.TextBox();
            this.lblBioDateHired = new System.Windows.Forms.Label();
            this.dtpBioDateHired = new System.Windows.Forms.DateTimePicker();
            this.chkBioDateHired = new System.Windows.Forms.CheckBox();
            this.lblBioBirthdate = new System.Windows.Forms.Label();
            this.dtpBioBirthdate = new System.Windows.Forms.DateTimePicker();
            this.chkBioBirthdate = new System.Windows.Forms.CheckBox();
            this.lblBioSex = new System.Windows.Forms.Label();
            this.cboBioSex = new System.Windows.Forms.ComboBox();
            this.lblBioCivilStatus = new System.Windows.Forms.Label();
            this.txtBioCivilStatus = new System.Windows.Forms.TextBox();
            this.lblBioAddress = new System.Windows.Forms.Label();
            this.txtBioAddress = new System.Windows.Forms.TextBox();
            this.lblBioContactNo = new System.Windows.Forms.Label();
            this.txtBioContactNo = new System.Windows.Forms.TextBox();
            this.lblBioEmergencyName = new System.Windows.Forms.Label();
            this.txtBioEmergencyName = new System.Windows.Forms.TextBox();
            this.lblBioEmergencyNo = new System.Windows.Forms.Label();
            this.txtBioEmergencyNo = new System.Windows.Forms.TextBox();
            this.lblBioUpdated = new System.Windows.Forms.Label();
            this.btnBioSave = new System.Windows.Forms.Button();
            this.btnBioNew = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabUsers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridUsers)).BeginInit();
            this.pnlEdit.SuspendLayout();
            this.tabAudit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridAudit)).BeginInit();
            this.tabBiodata.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridBiodata)).BeginInit();
            this.pnlBiodataEdit.SuspendLayout();
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
            this.titleLabel.Text = "Users and Audit Trail";
            this.titleLabel.UseMnemonic = false;
            //
            // tabControl
            //
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabUsers);
            this.tabControl.Controls.Add(this.tabAudit);
            this.tabControl.Controls.Add(this.tabBiodata);
            this.tabControl.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tabControl.Location = new System.Drawing.Point(32, 74);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(936, 528);
            this.tabControl.TabIndex = 1;
            //
            // tabUsers
            //
            this.tabUsers.BackColor = System.Drawing.Color.White;
            this.tabUsers.Controls.Add(this.gridUsers);
            this.tabUsers.Controls.Add(this.pnlEdit);
            this.tabUsers.Location = new System.Drawing.Point(4, 26);
            this.tabUsers.Name = "tabUsers";
            this.tabUsers.Padding = new System.Windows.Forms.Padding(3);
            this.tabUsers.Size = new System.Drawing.Size(928, 498);
            this.tabUsers.TabIndex = 0;
            this.tabUsers.Text = "Users";
            //
            // gridUsers
            //
            this.gridUsers.AllowUserToAddRows = false;
            this.gridUsers.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridUsers.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridUsers.BackgroundColor = System.Drawing.Color.White;
            this.gridUsers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridUsers.Location = new System.Drawing.Point(12, 12);
            this.gridUsers.Name = "gridUsers";
            this.gridUsers.ReadOnly = true;
            this.gridUsers.RowHeadersVisible = false;
            this.gridUsers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridUsers.Size = new System.Drawing.Size(596, 474);
            this.gridUsers.TabIndex = 0;
            this.gridUsers.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridUsers_CellClick);
            //
            // pnlEdit
            //
            this.pnlEdit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlEdit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlEdit.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlEdit.Controls.Add(this.lblUsername);
            this.pnlEdit.Controls.Add(this.txtUsername);
            this.pnlEdit.Controls.Add(this.lblFullName);
            this.pnlEdit.Controls.Add(this.txtFullName);
            this.pnlEdit.Controls.Add(this.lblRole);
            this.pnlEdit.Controls.Add(this.cboRole);
            this.pnlEdit.Controls.Add(this.lblPassword);
            this.pnlEdit.Controls.Add(this.txtPassword);
            this.pnlEdit.Controls.Add(this.lblConfirm);
            this.pnlEdit.Controls.Add(this.txtConfirm);
            this.pnlEdit.Controls.Add(this.chkActive);
            this.pnlEdit.Controls.Add(this.btnAdd);
            this.pnlEdit.Controls.Add(this.btnUpdate);
            this.pnlEdit.Controls.Add(this.btnNew);
            this.pnlEdit.Location = new System.Drawing.Point(624, 12);
            this.pnlEdit.Name = "pnlEdit";
            this.pnlEdit.Padding = new System.Windows.Forms.Padding(14);
            this.pnlEdit.Size = new System.Drawing.Size(292, 474);
            this.pnlEdit.TabIndex = 1;
            //
            // lblUsername
            //
            this.lblUsername.AutoSize = true;
            this.lblUsername.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblUsername.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblUsername.Location = new System.Drawing.Point(14, 14);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(58, 15);
            this.lblUsername.TabIndex = 0;
            this.lblUsername.Text = "Username";
            //
            // txtUsername
            //
            this.txtUsername.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtUsername.Location = new System.Drawing.Point(14, 34);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(250, 25);
            this.txtUsername.TabIndex = 1;
            //
            // lblFullName
            //
            this.lblFullName.AutoSize = true;
            this.lblFullName.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblFullName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblFullName.Location = new System.Drawing.Point(14, 76);
            this.lblFullName.Name = "lblFullName";
            this.lblFullName.Size = new System.Drawing.Size(60, 15);
            this.lblFullName.TabIndex = 2;
            this.lblFullName.Text = "Full Name";
            //
            // txtFullName
            //
            this.txtFullName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtFullName.Location = new System.Drawing.Point(14, 96);
            this.txtFullName.Name = "txtFullName";
            this.txtFullName.Size = new System.Drawing.Size(250, 25);
            this.txtFullName.TabIndex = 3;
            //
            // lblRole
            //
            this.lblRole.AutoSize = true;
            this.lblRole.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblRole.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblRole.Location = new System.Drawing.Point(14, 138);
            this.lblRole.Name = "lblRole";
            this.lblRole.Size = new System.Drawing.Size(30, 15);
            this.lblRole.TabIndex = 4;
            this.lblRole.Text = "Role";
            //
            // cboRole
            //
            this.cboRole.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboRole.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboRole.FormattingEnabled = true;
            this.cboRole.Items.AddRange(new object[] {
            "Admin",
            "Registrar",
            "Staff",
            "Cashier",
            "Releasing"});
            this.cboRole.Location = new System.Drawing.Point(14, 158);
            this.cboRole.Name = "cboRole";
            this.cboRole.Size = new System.Drawing.Size(250, 25);
            this.cboRole.TabIndex = 5;
            //
            // lblPassword
            //
            this.lblPassword.AutoSize = true;
            this.lblPassword.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblPassword.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblPassword.Location = new System.Drawing.Point(14, 200);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(220, 15);
            this.lblPassword.TabIndex = 6;
            this.lblPassword.Text = "Password (blank = keep on edit, min 8)";
            //
            // txtPassword
            //
            this.txtPassword.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtPassword.Location = new System.Drawing.Point(14, 220);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(250, 25);
            this.txtPassword.TabIndex = 7;
            this.txtPassword.UseSystemPasswordChar = true;
            //
            // lblConfirm
            //
            this.lblConfirm.AutoSize = true;
            this.lblConfirm.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblConfirm.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblConfirm.Location = new System.Drawing.Point(14, 262);
            this.lblConfirm.Name = "lblConfirm";
            this.lblConfirm.Size = new System.Drawing.Size(104, 15);
            this.lblConfirm.TabIndex = 8;
            this.lblConfirm.Text = "Confirm Password";
            //
            // txtConfirm
            //
            this.txtConfirm.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtConfirm.Location = new System.Drawing.Point(14, 282);
            this.txtConfirm.Name = "txtConfirm";
            this.txtConfirm.Size = new System.Drawing.Size(250, 25);
            this.txtConfirm.TabIndex = 9;
            this.txtConfirm.UseSystemPasswordChar = true;
            //
            // chkActive
            //
            this.chkActive.AutoSize = true;
            this.chkActive.Checked = true;
            this.chkActive.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkActive.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.chkActive.Location = new System.Drawing.Point(14, 324);
            this.chkActive.Name = "chkActive";
            this.chkActive.Size = new System.Drawing.Size(64, 23);
            this.chkActive.TabIndex = 10;
            this.chkActive.Text = "Active";
            this.chkActive.UseVisualStyleBackColor = true;
            //
            // btnAdd
            //
            this.btnAdd.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.btnAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdd.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnAdd.ForeColor = System.Drawing.Color.White;
            this.btnAdd.Location = new System.Drawing.Point(14, 364);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(122, 38);
            this.btnAdd.TabIndex = 11;
            this.btnAdd.Text = "Add User";
            this.btnAdd.UseVisualStyleBackColor = false;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            //
            // btnUpdate
            //
            this.btnUpdate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.btnUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUpdate.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnUpdate.ForeColor = System.Drawing.Color.White;
            this.btnUpdate.Location = new System.Drawing.Point(142, 364);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(122, 38);
            this.btnUpdate.TabIndex = 12;
            this.btnUpdate.Text = "Save Changes";
            this.btnUpdate.UseVisualStyleBackColor = false;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            //
            // btnNew
            //
            this.btnNew.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.btnNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNew.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnNew.ForeColor = System.Drawing.Color.White;
            this.btnNew.Location = new System.Drawing.Point(14, 410);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(122, 38);
            this.btnNew.TabIndex = 13;
            this.btnNew.Text = "New";
            this.btnNew.UseVisualStyleBackColor = false;
            this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
            //
            // tabAudit
            //
            this.tabAudit.BackColor = System.Drawing.Color.White;
            this.tabAudit.Controls.Add(this.gridAudit);
            this.tabAudit.Location = new System.Drawing.Point(4, 26);
            this.tabAudit.Name = "tabAudit";
            this.tabAudit.Padding = new System.Windows.Forms.Padding(3);
            this.tabAudit.Size = new System.Drawing.Size(928, 498);
            this.tabAudit.TabIndex = 1;
            this.tabAudit.Text = "Audit Trail";
            //
            // gridAudit
            //
            this.gridAudit.AllowUserToAddRows = false;
            this.gridAudit.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridAudit.BackgroundColor = System.Drawing.Color.White;
            this.gridAudit.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridAudit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridAudit.Location = new System.Drawing.Point(3, 3);
            this.gridAudit.Name = "gridAudit";
            this.gridAudit.ReadOnly = true;
            this.gridAudit.RowHeadersVisible = false;
            this.gridAudit.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridAudit.Size = new System.Drawing.Size(922, 492);
            this.gridAudit.TabIndex = 0;
            //
            // tabBiodata
            //
            this.tabBiodata.BackColor = System.Drawing.Color.White;
            this.tabBiodata.Controls.Add(this.gridBiodata);
            this.tabBiodata.Controls.Add(this.pnlBiodataEdit);
            this.tabBiodata.Location = new System.Drawing.Point(4, 26);
            this.tabBiodata.Name = "tabBiodata";
            this.tabBiodata.Padding = new System.Windows.Forms.Padding(3);
            this.tabBiodata.Size = new System.Drawing.Size(928, 498);
            this.tabBiodata.TabIndex = 2;
            this.tabBiodata.Text = "Staff Biodata";
            //
            // gridBiodata
            //
            this.gridBiodata.AllowUserToAddRows = false;
            this.gridBiodata.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridBiodata.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridBiodata.BackgroundColor = System.Drawing.Color.White;
            this.gridBiodata.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridBiodata.Location = new System.Drawing.Point(12, 12);
            this.gridBiodata.Name = "gridBiodata";
            this.gridBiodata.ReadOnly = true;
            this.gridBiodata.RowHeadersVisible = false;
            this.gridBiodata.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridBiodata.Size = new System.Drawing.Size(596, 474);
            this.gridBiodata.TabIndex = 0;
            this.gridBiodata.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridBiodata_CellClick);
            //
            // pnlBiodataEdit
            //
            this.pnlBiodataEdit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlBiodataEdit.AutoScroll = true;
            this.pnlBiodataEdit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlBiodataEdit.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlBiodataEdit.Location = new System.Drawing.Point(624, 12);
            this.pnlBiodataEdit.Name = "pnlBiodataEdit";
            this.pnlBiodataEdit.Padding = new System.Windows.Forms.Padding(14);
            this.pnlBiodataEdit.Size = new System.Drawing.Size(292, 474);
            this.pnlBiodataEdit.TabIndex = 1;
            this.pnlBiodataEdit.Controls.Add(this.lblBioHint);
            this.pnlBiodataEdit.Controls.Add(this.lblBioUser);
            this.pnlBiodataEdit.Controls.Add(this.cboBioUser);
            this.pnlBiodataEdit.Controls.Add(this.lblBioEmployeeNo);
            this.pnlBiodataEdit.Controls.Add(this.txtBioEmployeeNo);
            this.pnlBiodataEdit.Controls.Add(this.lblBioStatus);
            this.pnlBiodataEdit.Controls.Add(this.cboBioStatus);
            this.pnlBiodataEdit.Controls.Add(this.lblBioPosition);
            this.pnlBiodataEdit.Controls.Add(this.txtBioPosition);
            this.pnlBiodataEdit.Controls.Add(this.lblBioDateHired);
            this.pnlBiodataEdit.Controls.Add(this.dtpBioDateHired);
            this.pnlBiodataEdit.Controls.Add(this.chkBioDateHired);
            this.pnlBiodataEdit.Controls.Add(this.lblBioBirthdate);
            this.pnlBiodataEdit.Controls.Add(this.dtpBioBirthdate);
            this.pnlBiodataEdit.Controls.Add(this.chkBioBirthdate);
            this.pnlBiodataEdit.Controls.Add(this.lblBioSex);
            this.pnlBiodataEdit.Controls.Add(this.cboBioSex);
            this.pnlBiodataEdit.Controls.Add(this.lblBioCivilStatus);
            this.pnlBiodataEdit.Controls.Add(this.txtBioCivilStatus);
            this.pnlBiodataEdit.Controls.Add(this.lblBioAddress);
            this.pnlBiodataEdit.Controls.Add(this.txtBioAddress);
            this.pnlBiodataEdit.Controls.Add(this.lblBioContactNo);
            this.pnlBiodataEdit.Controls.Add(this.txtBioContactNo);
            this.pnlBiodataEdit.Controls.Add(this.lblBioEmergencyName);
            this.pnlBiodataEdit.Controls.Add(this.txtBioEmergencyName);
            this.pnlBiodataEdit.Controls.Add(this.lblBioEmergencyNo);
            this.pnlBiodataEdit.Controls.Add(this.txtBioEmergencyNo);
            this.pnlBiodataEdit.Controls.Add(this.lblBioUpdated);
            this.pnlBiodataEdit.Controls.Add(this.btnBioSave);
            this.pnlBiodataEdit.Controls.Add(this.btnBioNew);
            //
            // lblBioHint
            //
            this.lblBioHint.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblBioHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBioHint.Location = new System.Drawing.Point(14, 14);
            this.lblBioHint.Name = "lblBioHint";
            this.lblBioHint.Size = new System.Drawing.Size(250, 32);
            this.lblBioHint.TabIndex = 0;
            this.lblBioHint.Text = "Admin adds or edits any staff member\'s biodata here. Staff can only view their" +
    " own.";
            this.lblBioHint.UseMnemonic = false;
            //
            // lblBioUser
            //
            this.lblBioUser.AutoSize = true;
            this.lblBioUser.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBioUser.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBioUser.Location = new System.Drawing.Point(14, 54);
            this.lblBioUser.Name = "lblBioUser";
            this.lblBioUser.Size = new System.Drawing.Size(80, 15);
            this.lblBioUser.TabIndex = 1;
            this.lblBioUser.Text = "Staff Member";
            //
            // cboBioUser
            //
            this.cboBioUser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboBioUser.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboBioUser.FormattingEnabled = true;
            this.cboBioUser.Location = new System.Drawing.Point(14, 74);
            this.cboBioUser.Name = "cboBioUser";
            this.cboBioUser.Size = new System.Drawing.Size(250, 25);
            this.cboBioUser.TabIndex = 2;
            this.cboBioUser.SelectedIndexChanged += new System.EventHandler(this.cboBioUser_SelectedIndexChanged);
            //
            // lblBioEmployeeNo
            //
            this.lblBioEmployeeNo.AutoSize = true;
            this.lblBioEmployeeNo.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBioEmployeeNo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBioEmployeeNo.Location = new System.Drawing.Point(14, 116);
            this.lblBioEmployeeNo.Name = "lblBioEmployeeNo";
            this.lblBioEmployeeNo.Size = new System.Drawing.Size(90, 15);
            this.lblBioEmployeeNo.TabIndex = 3;
            this.lblBioEmployeeNo.Text = "Employee No.";
            //
            // txtBioEmployeeNo
            //
            this.txtBioEmployeeNo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtBioEmployeeNo.Location = new System.Drawing.Point(14, 136);
            this.txtBioEmployeeNo.Name = "txtBioEmployeeNo";
            this.txtBioEmployeeNo.Size = new System.Drawing.Size(250, 25);
            this.txtBioEmployeeNo.TabIndex = 4;
            //
            // lblBioStatus
            //
            this.lblBioStatus.AutoSize = true;
            this.lblBioStatus.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBioStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBioStatus.Location = new System.Drawing.Point(14, 178);
            this.lblBioStatus.Name = "lblBioStatus";
            this.lblBioStatus.Size = new System.Drawing.Size(112, 15);
            this.lblBioStatus.TabIndex = 5;
            this.lblBioStatus.Text = "Employment Status";
            //
            // cboBioStatus
            //
            this.cboBioStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboBioStatus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboBioStatus.FormattingEnabled = true;
            this.cboBioStatus.Items.AddRange(new object[] {
            "Permanent",
            "Casual",
            "Job Order",
            "Contractual",
            "Probationary"});
            this.cboBioStatus.Location = new System.Drawing.Point(14, 198);
            this.cboBioStatus.Name = "cboBioStatus";
            this.cboBioStatus.Size = new System.Drawing.Size(250, 25);
            this.cboBioStatus.TabIndex = 6;
            //
            // lblBioPosition
            //
            this.lblBioPosition.AutoSize = true;
            this.lblBioPosition.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBioPosition.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBioPosition.Location = new System.Drawing.Point(14, 240);
            this.lblBioPosition.Name = "lblBioPosition";
            this.lblBioPosition.Size = new System.Drawing.Size(80, 15);
            this.lblBioPosition.TabIndex = 7;
            this.lblBioPosition.Text = "Position/Title";
            //
            // txtBioPosition
            //
            this.txtBioPosition.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtBioPosition.Location = new System.Drawing.Point(14, 260);
            this.txtBioPosition.Name = "txtBioPosition";
            this.txtBioPosition.Size = new System.Drawing.Size(250, 25);
            this.txtBioPosition.TabIndex = 8;
            //
            // lblBioDateHired
            //
            this.lblBioDateHired.AutoSize = true;
            this.lblBioDateHired.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBioDateHired.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBioDateHired.Location = new System.Drawing.Point(14, 302);
            this.lblBioDateHired.Name = "lblBioDateHired";
            this.lblBioDateHired.Size = new System.Drawing.Size(70, 15);
            this.lblBioDateHired.TabIndex = 9;
            this.lblBioDateHired.Text = "Date Hired";
            //
            // dtpBioDateHired
            //
            this.dtpBioDateHired.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.dtpBioDateHired.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpBioDateHired.Location = new System.Drawing.Point(14, 322);
            this.dtpBioDateHired.Name = "dtpBioDateHired";
            this.dtpBioDateHired.Size = new System.Drawing.Size(160, 25);
            this.dtpBioDateHired.TabIndex = 10;
            //
            // chkBioDateHired
            //
            this.chkBioDateHired.AutoSize = true;
            this.chkBioDateHired.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.chkBioDateHired.Location = new System.Drawing.Point(180, 325);
            this.chkBioDateHired.Name = "chkBioDateHired";
            this.chkBioDateHired.Size = new System.Drawing.Size(52, 19);
            this.chkBioDateHired.TabIndex = 11;
            this.chkBioDateHired.Text = "Set";
            this.chkBioDateHired.UseVisualStyleBackColor = true;
            this.chkBioDateHired.CheckedChanged += new System.EventHandler(this.chkBioDateHired_CheckedChanged);
            //
            // lblBioBirthdate
            //
            this.lblBioBirthdate.AutoSize = true;
            this.lblBioBirthdate.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBioBirthdate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBioBirthdate.Location = new System.Drawing.Point(14, 364);
            this.lblBioBirthdate.Name = "lblBioBirthdate";
            this.lblBioBirthdate.Size = new System.Drawing.Size(60, 15);
            this.lblBioBirthdate.TabIndex = 12;
            this.lblBioBirthdate.Text = "Birthdate";
            //
            // dtpBioBirthdate
            //
            this.dtpBioBirthdate.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.dtpBioBirthdate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpBioBirthdate.Location = new System.Drawing.Point(14, 384);
            this.dtpBioBirthdate.Name = "dtpBioBirthdate";
            this.dtpBioBirthdate.Size = new System.Drawing.Size(160, 25);
            this.dtpBioBirthdate.TabIndex = 13;
            //
            // chkBioBirthdate
            //
            this.chkBioBirthdate.AutoSize = true;
            this.chkBioBirthdate.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.chkBioBirthdate.Location = new System.Drawing.Point(180, 387);
            this.chkBioBirthdate.Name = "chkBioBirthdate";
            this.chkBioBirthdate.Size = new System.Drawing.Size(52, 19);
            this.chkBioBirthdate.TabIndex = 14;
            this.chkBioBirthdate.Text = "Set";
            this.chkBioBirthdate.UseVisualStyleBackColor = true;
            this.chkBioBirthdate.CheckedChanged += new System.EventHandler(this.chkBioBirthdate_CheckedChanged);
            //
            // lblBioSex
            //
            this.lblBioSex.AutoSize = true;
            this.lblBioSex.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBioSex.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBioSex.Location = new System.Drawing.Point(14, 426);
            this.lblBioSex.Name = "lblBioSex";
            this.lblBioSex.Size = new System.Drawing.Size(28, 15);
            this.lblBioSex.TabIndex = 15;
            this.lblBioSex.Text = "Sex";
            //
            // cboBioSex
            //
            this.cboBioSex.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboBioSex.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboBioSex.FormattingEnabled = true;
            this.cboBioSex.Items.AddRange(new object[] {
            "Male",
            "Female"});
            this.cboBioSex.Location = new System.Drawing.Point(14, 446);
            this.cboBioSex.Name = "cboBioSex";
            this.cboBioSex.Size = new System.Drawing.Size(160, 25);
            this.cboBioSex.TabIndex = 16;
            //
            // lblBioCivilStatus
            //
            this.lblBioCivilStatus.AutoSize = true;
            this.lblBioCivilStatus.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBioCivilStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBioCivilStatus.Location = new System.Drawing.Point(14, 488);
            this.lblBioCivilStatus.Name = "lblBioCivilStatus";
            this.lblBioCivilStatus.Size = new System.Drawing.Size(70, 15);
            this.lblBioCivilStatus.TabIndex = 17;
            this.lblBioCivilStatus.Text = "Civil Status";
            //
            // txtBioCivilStatus
            //
            this.txtBioCivilStatus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtBioCivilStatus.Location = new System.Drawing.Point(14, 508);
            this.txtBioCivilStatus.Name = "txtBioCivilStatus";
            this.txtBioCivilStatus.Size = new System.Drawing.Size(250, 25);
            this.txtBioCivilStatus.TabIndex = 18;
            //
            // lblBioAddress
            //
            this.lblBioAddress.AutoSize = true;
            this.lblBioAddress.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBioAddress.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBioAddress.Location = new System.Drawing.Point(14, 550);
            this.lblBioAddress.Name = "lblBioAddress";
            this.lblBioAddress.Size = new System.Drawing.Size(52, 15);
            this.lblBioAddress.TabIndex = 19;
            this.lblBioAddress.Text = "Address";
            //
            // txtBioAddress
            //
            this.txtBioAddress.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtBioAddress.Location = new System.Drawing.Point(14, 570);
            this.txtBioAddress.Name = "txtBioAddress";
            this.txtBioAddress.Size = new System.Drawing.Size(250, 25);
            this.txtBioAddress.TabIndex = 20;
            //
            // lblBioContactNo
            //
            this.lblBioContactNo.AutoSize = true;
            this.lblBioContactNo.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBioContactNo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBioContactNo.Location = new System.Drawing.Point(14, 612);
            this.lblBioContactNo.Name = "lblBioContactNo";
            this.lblBioContactNo.Size = new System.Drawing.Size(80, 15);
            this.lblBioContactNo.TabIndex = 21;
            this.lblBioContactNo.Text = "Contact No.";
            //
            // txtBioContactNo
            //
            this.txtBioContactNo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtBioContactNo.Location = new System.Drawing.Point(14, 632);
            this.txtBioContactNo.Name = "txtBioContactNo";
            this.txtBioContactNo.Size = new System.Drawing.Size(250, 25);
            this.txtBioContactNo.TabIndex = 22;
            //
            // lblBioEmergencyName
            //
            this.lblBioEmergencyName.AutoSize = true;
            this.lblBioEmergencyName.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBioEmergencyName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBioEmergencyName.Location = new System.Drawing.Point(14, 674);
            this.lblBioEmergencyName.Name = "lblBioEmergencyName";
            this.lblBioEmergencyName.Size = new System.Drawing.Size(130, 15);
            this.lblBioEmergencyName.TabIndex = 23;
            this.lblBioEmergencyName.Text = "Emergency Contact Name";
            //
            // txtBioEmergencyName
            //
            this.txtBioEmergencyName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtBioEmergencyName.Location = new System.Drawing.Point(14, 694);
            this.txtBioEmergencyName.Name = "txtBioEmergencyName";
            this.txtBioEmergencyName.Size = new System.Drawing.Size(250, 25);
            this.txtBioEmergencyName.TabIndex = 24;
            //
            // lblBioEmergencyNo
            //
            this.lblBioEmergencyNo.AutoSize = true;
            this.lblBioEmergencyNo.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBioEmergencyNo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBioEmergencyNo.Location = new System.Drawing.Point(14, 736);
            this.lblBioEmergencyNo.Name = "lblBioEmergencyNo";
            this.lblBioEmergencyNo.Size = new System.Drawing.Size(150, 15);
            this.lblBioEmergencyNo.TabIndex = 25;
            this.lblBioEmergencyNo.Text = "Emergency Contact No.";
            //
            // txtBioEmergencyNo
            //
            this.txtBioEmergencyNo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtBioEmergencyNo.Location = new System.Drawing.Point(14, 756);
            this.txtBioEmergencyNo.Name = "txtBioEmergencyNo";
            this.txtBioEmergencyNo.Size = new System.Drawing.Size(250, 25);
            this.txtBioEmergencyNo.TabIndex = 26;
            //
            // lblBioUpdated
            //
            this.lblBioUpdated.AutoSize = true;
            this.lblBioUpdated.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblBioUpdated.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(158)))), ((int)(((byte)(169)))));
            this.lblBioUpdated.Location = new System.Drawing.Point(14, 798);
            this.lblBioUpdated.Name = "lblBioUpdated";
            this.lblBioUpdated.Size = new System.Drawing.Size(140, 15);
            this.lblBioUpdated.TabIndex = 27;
            this.lblBioUpdated.Text = "Pick a staff member above.";
            //
            // btnBioSave
            //
            this.btnBioSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.btnBioSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBioSave.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnBioSave.ForeColor = System.Drawing.Color.White;
            this.btnBioSave.Location = new System.Drawing.Point(14, 830);
            this.btnBioSave.Name = "btnBioSave";
            this.btnBioSave.Size = new System.Drawing.Size(122, 38);
            this.btnBioSave.TabIndex = 28;
            this.btnBioSave.Text = "Save";
            this.btnBioSave.UseVisualStyleBackColor = false;
            this.btnBioSave.Click += new System.EventHandler(this.btnBioSave_Click);
            //
            // btnBioNew
            //
            this.btnBioNew.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.btnBioNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBioNew.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnBioNew.ForeColor = System.Drawing.Color.White;
            this.btnBioNew.Location = new System.Drawing.Point(142, 830);
            this.btnBioNew.Name = "btnBioNew";
            this.btnBioNew.Size = new System.Drawing.Size(122, 38);
            this.btnBioNew.TabIndex = 29;
            this.btnBioNew.Text = "Clear";
            this.btnBioNew.UseVisualStyleBackColor = false;
            this.btnBioNew.Click += new System.EventHandler(this.btnBioNew_Click);
            //
            // UsersAuditForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1000, 620);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.titleLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "UsersAuditForm";
            this.Text = "Users and Audit Trail";
            this.tabControl.ResumeLayout(false);
            this.tabUsers.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridUsers)).EndInit();
            this.pnlEdit.ResumeLayout(false);
            this.pnlEdit.PerformLayout();
            this.tabAudit.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridAudit)).EndInit();
            this.tabBiodata.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridBiodata)).EndInit();
            this.pnlBiodataEdit.ResumeLayout(false);
            this.pnlBiodataEdit.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabUsers;
        private System.Windows.Forms.DataGridView gridUsers;
        private System.Windows.Forms.Panel pnlEdit;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblFullName;
        private System.Windows.Forms.TextBox txtFullName;
        private System.Windows.Forms.Label lblRole;
        private System.Windows.Forms.ComboBox cboRole;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblConfirm;
        private System.Windows.Forms.TextBox txtConfirm;
        private System.Windows.Forms.CheckBox chkActive;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnNew;
        private System.Windows.Forms.TabPage tabAudit;
        private System.Windows.Forms.DataGridView gridAudit;
        private System.Windows.Forms.TabPage tabBiodata;
        private System.Windows.Forms.DataGridView gridBiodata;
        private System.Windows.Forms.Panel pnlBiodataEdit;
        private System.Windows.Forms.Label lblBioHint;
        private System.Windows.Forms.Label lblBioUser;
        private System.Windows.Forms.ComboBox cboBioUser;
        private System.Windows.Forms.Label lblBioEmployeeNo;
        private System.Windows.Forms.TextBox txtBioEmployeeNo;
        private System.Windows.Forms.Label lblBioStatus;
        private System.Windows.Forms.ComboBox cboBioStatus;
        private System.Windows.Forms.Label lblBioPosition;
        private System.Windows.Forms.TextBox txtBioPosition;
        private System.Windows.Forms.Label lblBioDateHired;
        private System.Windows.Forms.DateTimePicker dtpBioDateHired;
        private System.Windows.Forms.CheckBox chkBioDateHired;
        private System.Windows.Forms.Label lblBioBirthdate;
        private System.Windows.Forms.DateTimePicker dtpBioBirthdate;
        private System.Windows.Forms.CheckBox chkBioBirthdate;
        private System.Windows.Forms.Label lblBioSex;
        private System.Windows.Forms.ComboBox cboBioSex;
        private System.Windows.Forms.Label lblBioCivilStatus;
        private System.Windows.Forms.TextBox txtBioCivilStatus;
        private System.Windows.Forms.Label lblBioAddress;
        private System.Windows.Forms.TextBox txtBioAddress;
        private System.Windows.Forms.Label lblBioContactNo;
        private System.Windows.Forms.TextBox txtBioContactNo;
        private System.Windows.Forms.Label lblBioEmergencyName;
        private System.Windows.Forms.TextBox txtBioEmergencyName;
        private System.Windows.Forms.Label lblBioEmergencyNo;
        private System.Windows.Forms.TextBox txtBioEmergencyNo;
        private System.Windows.Forms.Label lblBioUpdated;
        private System.Windows.Forms.Button btnBioSave;
        private System.Windows.Forms.Button btnBioNew;
    }
}
