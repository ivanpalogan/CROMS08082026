namespace CROMS.Forms
{
    partial class StaffBiodataForm
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
            this.titleLabel = new System.Windows.Forms.Label();
            this.subtitleLabel = new System.Windows.Forms.Label();
            this.pnlAccount = new System.Windows.Forms.Panel();
            this.lblFullNameCap = new System.Windows.Forms.Label();
            this.txtFullName = new System.Windows.Forms.TextBox();
            this.lblUsernameCap = new System.Windows.Forms.Label();
            this.lblUsernameValue = new System.Windows.Forms.Label();
            this.lblPwNote = new System.Windows.Forms.Label();
            this.lblCurPass = new System.Windows.Forms.Label();
            this.txtCurPass = new System.Windows.Forms.TextBox();
            this.lblNewPass = new System.Windows.Forms.Label();
            this.txtNewPass = new System.Windows.Forms.TextBox();
            this.lblConfirmPass = new System.Windows.Forms.Label();
            this.txtConfirmPass = new System.Windows.Forms.TextBox();
            this.lblWorkHeader = new System.Windows.Forms.Label();
            this.pnlForm = new System.Windows.Forms.Panel();
            this.lblEmployeeNo = new System.Windows.Forms.Label();
            this.txtEmployeeNo = new System.Windows.Forms.TextBox();
            this.lblEmploymentStatus = new System.Windows.Forms.Label();
            this.cboEmploymentStatus = new System.Windows.Forms.ComboBox();
            this.lblPosition = new System.Windows.Forms.Label();
            this.txtPosition = new System.Windows.Forms.TextBox();
            this.lblDateHired = new System.Windows.Forms.Label();
            this.dtpDateHired = new System.Windows.Forms.DateTimePicker();
            this.chkDateHired = new System.Windows.Forms.CheckBox();
            this.lblBirthdate = new System.Windows.Forms.Label();
            this.dtpBirthdate = new System.Windows.Forms.DateTimePicker();
            this.chkBirthdate = new System.Windows.Forms.CheckBox();
            this.lblSex = new System.Windows.Forms.Label();
            this.cboSex = new System.Windows.Forms.ComboBox();
            this.lblCivilStatus = new System.Windows.Forms.Label();
            this.txtCivilStatus = new System.Windows.Forms.TextBox();
            this.lblAddress = new System.Windows.Forms.Label();
            this.txtAddress = new System.Windows.Forms.TextBox();
            this.lblContactNo = new System.Windows.Forms.Label();
            this.txtContactNo = new System.Windows.Forms.TextBox();
            this.lblEmergencyName = new System.Windows.Forms.Label();
            this.txtEmergencyName = new System.Windows.Forms.TextBox();
            this.lblEmergencyNo = new System.Windows.Forms.Label();
            this.txtEmergencyNo = new System.Windows.Forms.TextBox();
            this.lblUpdated = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.pnlAccount.SuspendLayout();
            this.pnlForm.SuspendLayout();
            this.SuspendLayout();
            //
            // titleLabel
            //
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(27)))), ((int)(((byte)(36)))));
            this.titleLabel.Location = new System.Drawing.Point(24, 20);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(150, 30);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Edit Profile";
            this.titleLabel.UseMnemonic = false;
            //
            // subtitleLabel
            //
            this.subtitleLabel.AutoSize = true;
            this.subtitleLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.subtitleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.subtitleLabel.Location = new System.Drawing.Point(26, 54);
            this.subtitleLabel.Name = "subtitleLabel";
            this.subtitleLabel.Size = new System.Drawing.Size(400, 15);
            this.subtitleLabel.TabIndex = 1;
            this.subtitleLabel.Text = "Update your name or password below. Ask an admin to change your work details.";
            this.subtitleLabel.UseMnemonic = false;
            //
            // pnlAccount
            //
            this.pnlAccount.BackColor = System.Drawing.Color.White;
            this.pnlAccount.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlAccount.Location = new System.Drawing.Point(24, 82);
            this.pnlAccount.Name = "pnlAccount";
            this.pnlAccount.Size = new System.Drawing.Size(552, 222);
            this.pnlAccount.TabIndex = 2;
            this.pnlAccount.Controls.Add(this.lblFullNameCap);
            this.pnlAccount.Controls.Add(this.txtFullName);
            this.pnlAccount.Controls.Add(this.lblUsernameCap);
            this.pnlAccount.Controls.Add(this.lblUsernameValue);
            this.pnlAccount.Controls.Add(this.lblPwNote);
            this.pnlAccount.Controls.Add(this.lblCurPass);
            this.pnlAccount.Controls.Add(this.txtCurPass);
            this.pnlAccount.Controls.Add(this.lblNewPass);
            this.pnlAccount.Controls.Add(this.txtNewPass);
            this.pnlAccount.Controls.Add(this.lblConfirmPass);
            this.pnlAccount.Controls.Add(this.txtConfirmPass);
            //
            // lblFullNameCap
            //
            this.lblFullNameCap.AutoSize = true;
            this.lblFullNameCap.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblFullNameCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblFullNameCap.Location = new System.Drawing.Point(20, 16);
            this.lblFullNameCap.Name = "lblFullNameCap";
            this.lblFullNameCap.Size = new System.Drawing.Size(63, 15);
            this.lblFullNameCap.TabIndex = 0;
            this.lblFullNameCap.Text = "Full Name";
            //
            // txtFullName
            //
            this.txtFullName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtFullName.Location = new System.Drawing.Point(20, 36);
            this.txtFullName.Name = "txtFullName";
            this.txtFullName.Size = new System.Drawing.Size(250, 25);
            this.txtFullName.TabIndex = 1;
            //
            // lblUsernameCap
            //
            this.lblUsernameCap.AutoSize = true;
            this.lblUsernameCap.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblUsernameCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblUsernameCap.Location = new System.Drawing.Point(286, 16);
            this.lblUsernameCap.Name = "lblUsernameCap";
            this.lblUsernameCap.Size = new System.Drawing.Size(63, 15);
            this.lblUsernameCap.TabIndex = 2;
            this.lblUsernameCap.Text = "Username";
            //
            // lblUsernameValue
            //
            this.lblUsernameValue.AutoSize = false;
            this.lblUsernameValue.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblUsernameValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(27)))), ((int)(((byte)(36)))));
            this.lblUsernameValue.Location = new System.Drawing.Point(286, 36);
            this.lblUsernameValue.Name = "lblUsernameValue";
            this.lblUsernameValue.Size = new System.Drawing.Size(220, 25);
            this.lblUsernameValue.TabIndex = 3;
            this.lblUsernameValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblPwNote
            //
            this.lblPwNote.AutoSize = true;
            this.lblPwNote.Font = new System.Drawing.Font("Segoe UI", 8.75F, System.Drawing.FontStyle.Bold);
            this.lblPwNote.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblPwNote.Location = new System.Drawing.Point(20, 82);
            this.lblPwNote.Name = "lblPwNote";
            this.lblPwNote.Size = new System.Drawing.Size(350, 15);
            this.lblPwNote.TabIndex = 4;
            this.lblPwNote.Text = "Change Password (leave blank to keep your current password)";
            //
            // lblCurPass
            //
            this.lblCurPass.AutoSize = true;
            this.lblCurPass.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblCurPass.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCurPass.Location = new System.Drawing.Point(20, 106);
            this.lblCurPass.Name = "lblCurPass";
            this.lblCurPass.Size = new System.Drawing.Size(100, 15);
            this.lblCurPass.TabIndex = 5;
            this.lblCurPass.Text = "Current Password";
            //
            // txtCurPass
            //
            this.txtCurPass.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtCurPass.Location = new System.Drawing.Point(20, 126);
            this.txtCurPass.Name = "txtCurPass";
            this.txtCurPass.Size = new System.Drawing.Size(250, 25);
            this.txtCurPass.TabIndex = 6;
            this.txtCurPass.UseSystemPasswordChar = true;
            //
            // lblNewPass
            //
            this.lblNewPass.AutoSize = true;
            this.lblNewPass.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblNewPass.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblNewPass.Location = new System.Drawing.Point(20, 162);
            this.lblNewPass.Name = "lblNewPass";
            this.lblNewPass.Size = new System.Drawing.Size(90, 15);
            this.lblNewPass.TabIndex = 7;
            this.lblNewPass.Text = "New Password";
            //
            // txtNewPass
            //
            this.txtNewPass.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtNewPass.Location = new System.Drawing.Point(20, 182);
            this.txtNewPass.Name = "txtNewPass";
            this.txtNewPass.Size = new System.Drawing.Size(220, 25);
            this.txtNewPass.TabIndex = 8;
            this.txtNewPass.UseSystemPasswordChar = true;
            //
            // lblConfirmPass
            //
            this.lblConfirmPass.AutoSize = true;
            this.lblConfirmPass.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblConfirmPass.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblConfirmPass.Location = new System.Drawing.Point(286, 162);
            this.lblConfirmPass.Name = "lblConfirmPass";
            this.lblConfirmPass.Size = new System.Drawing.Size(140, 15);
            this.lblConfirmPass.TabIndex = 9;
            this.lblConfirmPass.Text = "Confirm New Password";
            //
            // txtConfirmPass
            //
            this.txtConfirmPass.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtConfirmPass.Location = new System.Drawing.Point(286, 182);
            this.txtConfirmPass.Name = "txtConfirmPass";
            this.txtConfirmPass.Size = new System.Drawing.Size(220, 25);
            this.txtConfirmPass.TabIndex = 10;
            this.txtConfirmPass.UseSystemPasswordChar = true;
            //
            // lblWorkHeader
            //
            this.lblWorkHeader.AutoSize = true;
            this.lblWorkHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblWorkHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(27)))), ((int)(((byte)(36)))));
            this.lblWorkHeader.Location = new System.Drawing.Point(24, 316);
            this.lblWorkHeader.Name = "lblWorkHeader";
            this.lblWorkHeader.Size = new System.Drawing.Size(150, 19);
            this.lblWorkHeader.TabIndex = 3;
            this.lblWorkHeader.Text = "Work Details (view only)";
            //
            // pnlForm
            //
            this.pnlForm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlForm.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlForm.Location = new System.Drawing.Point(24, 344);
            this.pnlForm.Name = "pnlForm";
            this.pnlForm.Padding = new System.Windows.Forms.Padding(20);
            this.pnlForm.Size = new System.Drawing.Size(552, 520);
            this.pnlForm.TabIndex = 4;
            this.pnlForm.Controls.Add(this.lblEmployeeNo);
            this.pnlForm.Controls.Add(this.txtEmployeeNo);
            this.pnlForm.Controls.Add(this.lblEmploymentStatus);
            this.pnlForm.Controls.Add(this.cboEmploymentStatus);
            this.pnlForm.Controls.Add(this.lblPosition);
            this.pnlForm.Controls.Add(this.txtPosition);
            this.pnlForm.Controls.Add(this.lblDateHired);
            this.pnlForm.Controls.Add(this.dtpDateHired);
            this.pnlForm.Controls.Add(this.chkDateHired);
            this.pnlForm.Controls.Add(this.lblBirthdate);
            this.pnlForm.Controls.Add(this.dtpBirthdate);
            this.pnlForm.Controls.Add(this.chkBirthdate);
            this.pnlForm.Controls.Add(this.lblSex);
            this.pnlForm.Controls.Add(this.cboSex);
            this.pnlForm.Controls.Add(this.lblCivilStatus);
            this.pnlForm.Controls.Add(this.txtCivilStatus);
            this.pnlForm.Controls.Add(this.lblAddress);
            this.pnlForm.Controls.Add(this.txtAddress);
            this.pnlForm.Controls.Add(this.lblContactNo);
            this.pnlForm.Controls.Add(this.txtContactNo);
            this.pnlForm.Controls.Add(this.lblEmergencyName);
            this.pnlForm.Controls.Add(this.txtEmergencyName);
            this.pnlForm.Controls.Add(this.lblEmergencyNo);
            this.pnlForm.Controls.Add(this.txtEmergencyNo);
            this.pnlForm.Controls.Add(this.lblUpdated);
            //
            // lblEmployeeNo
            //
            this.lblEmployeeNo.AutoSize = true;
            this.lblEmployeeNo.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblEmployeeNo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblEmployeeNo.Location = new System.Drawing.Point(24, 20);
            this.lblEmployeeNo.Name = "lblEmployeeNo";
            this.lblEmployeeNo.Size = new System.Drawing.Size(90, 15);
            this.lblEmployeeNo.TabIndex = 0;
            this.lblEmployeeNo.Text = "Employee No.";
            //
            // txtEmployeeNo
            //
            this.txtEmployeeNo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtEmployeeNo.Location = new System.Drawing.Point(24, 40);
            this.txtEmployeeNo.Name = "txtEmployeeNo";
            this.txtEmployeeNo.Size = new System.Drawing.Size(250, 25);
            this.txtEmployeeNo.TabIndex = 1;
            //
            // lblEmploymentStatus
            //
            this.lblEmploymentStatus.AutoSize = true;
            this.lblEmploymentStatus.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblEmploymentStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblEmploymentStatus.Location = new System.Drawing.Point(290, 20);
            this.lblEmploymentStatus.Name = "lblEmploymentStatus";
            this.lblEmploymentStatus.Size = new System.Drawing.Size(112, 15);
            this.lblEmploymentStatus.TabIndex = 2;
            this.lblEmploymentStatus.Text = "Employment Status";
            //
            // cboEmploymentStatus
            //
            this.cboEmploymentStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboEmploymentStatus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboEmploymentStatus.FormattingEnabled = true;
            this.cboEmploymentStatus.Items.AddRange(new object[] {
            "Permanent",
            "Casual",
            "Job Order",
            "Contractual",
            "Probationary"});
            this.cboEmploymentStatus.Location = new System.Drawing.Point(290, 40);
            this.cboEmploymentStatus.Name = "cboEmploymentStatus";
            this.cboEmploymentStatus.Size = new System.Drawing.Size(220, 25);
            this.cboEmploymentStatus.TabIndex = 3;
            //
            // lblPosition
            //
            this.lblPosition.AutoSize = true;
            this.lblPosition.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblPosition.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblPosition.Location = new System.Drawing.Point(24, 76);
            this.lblPosition.Name = "lblPosition";
            this.lblPosition.Size = new System.Drawing.Size(80, 15);
            this.lblPosition.TabIndex = 4;
            this.lblPosition.Text = "Position/Title";
            //
            // txtPosition
            //
            this.txtPosition.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtPosition.Location = new System.Drawing.Point(24, 96);
            this.txtPosition.Name = "txtPosition";
            this.txtPosition.Size = new System.Drawing.Size(486, 25);
            this.txtPosition.TabIndex = 5;
            //
            // lblDateHired
            //
            this.lblDateHired.AutoSize = true;
            this.lblDateHired.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblDateHired.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblDateHired.Location = new System.Drawing.Point(24, 132);
            this.lblDateHired.Name = "lblDateHired";
            this.lblDateHired.Size = new System.Drawing.Size(70, 15);
            this.lblDateHired.TabIndex = 6;
            this.lblDateHired.Text = "Date Hired";
            //
            // dtpDateHired
            //
            this.dtpDateHired.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.dtpDateHired.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDateHired.Location = new System.Drawing.Point(24, 152);
            this.dtpDateHired.Name = "dtpDateHired";
            this.dtpDateHired.Size = new System.Drawing.Size(160, 25);
            this.dtpDateHired.TabIndex = 7;
            //
            // chkDateHired
            //
            this.chkDateHired.AutoSize = true;
            this.chkDateHired.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.chkDateHired.Location = new System.Drawing.Point(190, 155);
            this.chkDateHired.Name = "chkDateHired";
            this.chkDateHired.Size = new System.Drawing.Size(52, 19);
            this.chkDateHired.TabIndex = 8;
            this.chkDateHired.Text = "Set";
            this.chkDateHired.UseVisualStyleBackColor = true;
            this.chkDateHired.CheckedChanged += new System.EventHandler(this.chkDateHired_CheckedChanged);
            //
            // lblBirthdate
            //
            this.lblBirthdate.AutoSize = true;
            this.lblBirthdate.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblBirthdate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBirthdate.Location = new System.Drawing.Point(290, 132);
            this.lblBirthdate.Name = "lblBirthdate";
            this.lblBirthdate.Size = new System.Drawing.Size(60, 15);
            this.lblBirthdate.TabIndex = 9;
            this.lblBirthdate.Text = "Birthdate";
            //
            // dtpBirthdate
            //
            this.dtpBirthdate.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.dtpBirthdate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpBirthdate.Location = new System.Drawing.Point(290, 152);
            this.dtpBirthdate.Name = "dtpBirthdate";
            this.dtpBirthdate.Size = new System.Drawing.Size(160, 25);
            this.dtpBirthdate.TabIndex = 10;
            //
            // chkBirthdate
            //
            this.chkBirthdate.AutoSize = true;
            this.chkBirthdate.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.chkBirthdate.Location = new System.Drawing.Point(456, 155);
            this.chkBirthdate.Name = "chkBirthdate";
            this.chkBirthdate.Size = new System.Drawing.Size(52, 19);
            this.chkBirthdate.TabIndex = 11;
            this.chkBirthdate.Text = "Set";
            this.chkBirthdate.UseVisualStyleBackColor = true;
            this.chkBirthdate.CheckedChanged += new System.EventHandler(this.chkBirthdate_CheckedChanged);
            //
            // lblSex
            //
            this.lblSex.AutoSize = true;
            this.lblSex.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblSex.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblSex.Location = new System.Drawing.Point(24, 188);
            this.lblSex.Name = "lblSex";
            this.lblSex.Size = new System.Drawing.Size(28, 15);
            this.lblSex.TabIndex = 12;
            this.lblSex.Text = "Sex";
            //
            // cboSex
            //
            this.cboSex.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSex.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboSex.FormattingEnabled = true;
            this.cboSex.Items.AddRange(new object[] {
            "Male",
            "Female"});
            this.cboSex.Location = new System.Drawing.Point(24, 208);
            this.cboSex.Name = "cboSex";
            this.cboSex.Size = new System.Drawing.Size(160, 25);
            this.cboSex.TabIndex = 13;
            //
            // lblCivilStatus
            //
            this.lblCivilStatus.AutoSize = true;
            this.lblCivilStatus.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblCivilStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCivilStatus.Location = new System.Drawing.Point(290, 188);
            this.lblCivilStatus.Name = "lblCivilStatus";
            this.lblCivilStatus.Size = new System.Drawing.Size(70, 15);
            this.lblCivilStatus.TabIndex = 14;
            this.lblCivilStatus.Text = "Civil Status";
            //
            // txtCivilStatus
            //
            this.txtCivilStatus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtCivilStatus.Location = new System.Drawing.Point(290, 208);
            this.txtCivilStatus.Name = "txtCivilStatus";
            this.txtCivilStatus.Size = new System.Drawing.Size(220, 25);
            this.txtCivilStatus.TabIndex = 15;
            //
            // lblAddress
            //
            this.lblAddress.AutoSize = true;
            this.lblAddress.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblAddress.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblAddress.Location = new System.Drawing.Point(24, 244);
            this.lblAddress.Name = "lblAddress";
            this.lblAddress.Size = new System.Drawing.Size(52, 15);
            this.lblAddress.TabIndex = 16;
            this.lblAddress.Text = "Address";
            //
            // txtAddress
            //
            this.txtAddress.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtAddress.Location = new System.Drawing.Point(24, 264);
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.Size = new System.Drawing.Size(486, 25);
            this.txtAddress.TabIndex = 17;
            //
            // lblContactNo
            //
            this.lblContactNo.AutoSize = true;
            this.lblContactNo.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblContactNo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblContactNo.Location = new System.Drawing.Point(24, 300);
            this.lblContactNo.Name = "lblContactNo";
            this.lblContactNo.Size = new System.Drawing.Size(80, 15);
            this.lblContactNo.TabIndex = 18;
            this.lblContactNo.Text = "Contact No.";
            //
            // txtContactNo
            //
            this.txtContactNo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtContactNo.Location = new System.Drawing.Point(24, 320);
            this.txtContactNo.Name = "txtContactNo";
            this.txtContactNo.Size = new System.Drawing.Size(220, 25);
            this.txtContactNo.TabIndex = 19;
            //
            // lblEmergencyName
            //
            this.lblEmergencyName.AutoSize = true;
            this.lblEmergencyName.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblEmergencyName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblEmergencyName.Location = new System.Drawing.Point(24, 356);
            this.lblEmergencyName.Name = "lblEmergencyName";
            this.lblEmergencyName.Size = new System.Drawing.Size(130, 15);
            this.lblEmergencyName.TabIndex = 20;
            this.lblEmergencyName.Text = "Emergency Contact Name";
            //
            // txtEmergencyName
            //
            this.txtEmergencyName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtEmergencyName.Location = new System.Drawing.Point(24, 376);
            this.txtEmergencyName.Name = "txtEmergencyName";
            this.txtEmergencyName.Size = new System.Drawing.Size(486, 25);
            this.txtEmergencyName.TabIndex = 21;
            //
            // lblEmergencyNo
            //
            this.lblEmergencyNo.AutoSize = true;
            this.lblEmergencyNo.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblEmergencyNo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblEmergencyNo.Location = new System.Drawing.Point(24, 412);
            this.lblEmergencyNo.Name = "lblEmergencyNo";
            this.lblEmergencyNo.Size = new System.Drawing.Size(150, 15);
            this.lblEmergencyNo.TabIndex = 22;
            this.lblEmergencyNo.Text = "Emergency Contact No.";
            //
            // txtEmergencyNo
            //
            this.txtEmergencyNo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtEmergencyNo.Location = new System.Drawing.Point(24, 432);
            this.txtEmergencyNo.Name = "txtEmergencyNo";
            this.txtEmergencyNo.Size = new System.Drawing.Size(220, 25);
            this.txtEmergencyNo.TabIndex = 23;
            //
            // lblUpdated
            //
            this.lblUpdated.AutoSize = true;
            this.lblUpdated.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblUpdated.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(158)))), ((int)(((byte)(169)))));
            this.lblUpdated.Location = new System.Drawing.Point(24, 472);
            this.lblUpdated.Name = "lblUpdated";
            this.lblUpdated.Size = new System.Drawing.Size(160, 15);
            this.lblUpdated.TabIndex = 24;
            this.lblUpdated.Text = "Not yet saved.";
            //
            // btnSave
            //
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(24, 876);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(160, 40);
            this.btnSave.TabIndex = 5;
            this.btnSave.Text = "Save Changes";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // btnClose
            //
            this.btnClose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Location = new System.Drawing.Point(194, 876);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 40);
            this.btnClose.TabIndex = 6;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // StaffBiodataForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(600, 936);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.subtitleLabel);
            this.Controls.Add(this.pnlAccount);
            this.Controls.Add(this.lblWorkHeader);
            this.Controls.Add(this.pnlForm);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StaffBiodataForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit Profile";
            this.pnlAccount.ResumeLayout(false);
            this.pnlAccount.PerformLayout();
            this.pnlForm.ResumeLayout(false);
            this.pnlForm.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label subtitleLabel;
        private System.Windows.Forms.Panel pnlAccount;
        private System.Windows.Forms.Label lblFullNameCap;
        private System.Windows.Forms.TextBox txtFullName;
        private System.Windows.Forms.Label lblUsernameCap;
        private System.Windows.Forms.Label lblUsernameValue;
        private System.Windows.Forms.Label lblPwNote;
        private System.Windows.Forms.Label lblCurPass;
        private System.Windows.Forms.TextBox txtCurPass;
        private System.Windows.Forms.Label lblNewPass;
        private System.Windows.Forms.TextBox txtNewPass;
        private System.Windows.Forms.Label lblConfirmPass;
        private System.Windows.Forms.TextBox txtConfirmPass;
        private System.Windows.Forms.Label lblWorkHeader;
        private System.Windows.Forms.Panel pnlForm;
        private System.Windows.Forms.Label lblEmployeeNo;
        private System.Windows.Forms.TextBox txtEmployeeNo;
        private System.Windows.Forms.Label lblEmploymentStatus;
        private System.Windows.Forms.ComboBox cboEmploymentStatus;
        private System.Windows.Forms.Label lblPosition;
        private System.Windows.Forms.TextBox txtPosition;
        private System.Windows.Forms.Label lblDateHired;
        private System.Windows.Forms.DateTimePicker dtpDateHired;
        private System.Windows.Forms.CheckBox chkDateHired;
        private System.Windows.Forms.Label lblBirthdate;
        private System.Windows.Forms.DateTimePicker dtpBirthdate;
        private System.Windows.Forms.CheckBox chkBirthdate;
        private System.Windows.Forms.Label lblSex;
        private System.Windows.Forms.ComboBox cboSex;
        private System.Windows.Forms.Label lblCivilStatus;
        private System.Windows.Forms.TextBox txtCivilStatus;
        private System.Windows.Forms.Label lblAddress;
        private System.Windows.Forms.TextBox txtAddress;
        private System.Windows.Forms.Label lblContactNo;
        private System.Windows.Forms.TextBox txtContactNo;
        private System.Windows.Forms.Label lblEmergencyName;
        private System.Windows.Forms.TextBox txtEmergencyName;
        private System.Windows.Forms.Label lblEmergencyNo;
        private System.Windows.Forms.TextBox txtEmergencyNo;
        private System.Windows.Forms.Label lblUpdated;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnClose;
    }
}
