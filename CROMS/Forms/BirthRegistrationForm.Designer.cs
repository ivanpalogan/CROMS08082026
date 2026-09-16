// BirthRegistrationForm.Designer.cs - UI controls and InitializeComponent only

using System;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Forms
{
    partial class BirthRegistrationForm
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
            this.components = new System.ComponentModel.Container();
            this.certificateMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuViewSoftcopy = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFactsCert = new System.Windows.Forms.ToolStripMenuItem();
            this.btnCertificate = new CROMS.Modules.SplitButton();
            this.btnNew = new System.Windows.Forms.Button();
            this.layoutRoot = new System.Windows.Forms.TableLayoutPanel();
            this.layoutMain = new System.Windows.Forms.TableLayoutPanel();
            this.pnlHeader = new System.Windows.Forms.TableLayoutPanel();
            this.pnlHeadText = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.chkDelayed = new System.Windows.Forms.CheckBox();
            this.pnlHeadActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSubmit = new System.Windows.Forms.Button();
            this.btnSaveDraft = new System.Windows.Forms.Button();
            this.btnOCRLiveBirth = new System.Windows.Forms.Button();
            this.cardForm = new CROMS.Modules.CardPanel();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabChild = new System.Windows.Forms.TabPage();
            this.tblChild = new System.Windows.Forms.TableLayoutPanel();
            this.lblFirstName = new System.Windows.Forms.Label();
            this.txtFirstName = new System.Windows.Forms.TextBox();
            this.lblMiddleName = new System.Windows.Forms.Label();
            this.txtMiddleName = new System.Windows.Forms.TextBox();
            this.lblLastName = new System.Windows.Forms.Label();
            this.txtLastName = new System.Windows.Forms.TextBox();
            this.lblSex = new System.Windows.Forms.Label();
            this.cboSex = new System.Windows.Forms.ComboBox();
            this.lblDob = new System.Windows.Forms.Label();
            this.dtpDob = new System.Windows.Forms.DateTimePicker();
            this.lblTypeOfBirth = new System.Windows.Forms.Label();
            this.cboTypeOfBirth = new System.Windows.Forms.ComboBox();
            this.lblBirthOrder = new System.Windows.Forms.Label();
            this.txtBirthOrder = new System.Windows.Forms.TextBox();
            this.lblWeight = new System.Windows.Forms.Label();
            this.txtWeight = new System.Windows.Forms.TextBox();
            this.lblPlace = new System.Windows.Forms.Label();
            this.txtPlace = new System.Windows.Forms.TextBox();
            this.tabMother = new System.Windows.Forms.TabPage();
            this.tblMother = new System.Windows.Forms.TableLayoutPanel();
            this.lblMFirst = new System.Windows.Forms.Label();
            this.txtMFirst = new System.Windows.Forms.TextBox();
            this.lblMMiddle = new System.Windows.Forms.Label();
            this.txtMMiddle = new System.Windows.Forms.TextBox();
            this.lblMLast = new System.Windows.Forms.Label();
            this.txtMLast = new System.Windows.Forms.TextBox();
            this.lblMCitizen = new System.Windows.Forms.Label();
            this.txtMCitizen = new System.Windows.Forms.TextBox();
            this.lblMReligion = new System.Windows.Forms.Label();
            this.txtMReligion = new System.Windows.Forms.TextBox();
            this.lblMOccupation = new System.Windows.Forms.Label();
            this.txtMOccupation = new System.Windows.Forms.TextBox();
            this.lblMAge = new System.Windows.Forms.Label();
            this.txtMAge = new System.Windows.Forms.TextBox();
            this.lblMBornAlive = new System.Windows.Forms.Label();
            this.txtMBornAlive = new System.Windows.Forms.TextBox();
            this.lblMLiving = new System.Windows.Forms.Label();
            this.txtMLiving = new System.Windows.Forms.TextBox();
            this.lblMDead = new System.Windows.Forms.Label();
            this.txtMDead = new System.Windows.Forms.TextBox();
            this.lblMResidence = new System.Windows.Forms.Label();
            this.txtMResidence = new System.Windows.Forms.TextBox();
            this.tabFather = new System.Windows.Forms.TabPage();
            this.tblFather = new System.Windows.Forms.TableLayoutPanel();
            this.lblFFirst = new System.Windows.Forms.Label();
            this.txtFFirst = new System.Windows.Forms.TextBox();
            this.lblFMiddle = new System.Windows.Forms.Label();
            this.txtFMiddle = new System.Windows.Forms.TextBox();
            this.lblFLast = new System.Windows.Forms.Label();
            this.txtFLast = new System.Windows.Forms.TextBox();
            this.lblFCitizen = new System.Windows.Forms.Label();
            this.txtFCitizen = new System.Windows.Forms.TextBox();
            this.lblFReligion = new System.Windows.Forms.Label();
            this.txtFReligion = new System.Windows.Forms.TextBox();
            this.lblFOccupation = new System.Windows.Forms.Label();
            this.txtFOccupation = new System.Windows.Forms.TextBox();
            this.lblFAge = new System.Windows.Forms.Label();
            this.txtFAge = new System.Windows.Forms.TextBox();
            this.lblFResidence = new System.Windows.Forms.Label();
            this.txtFResidence = new System.Windows.Forms.TextBox();
            this.tabMarriage = new System.Windows.Forms.TabPage();
            this.tblMarriage = new System.Windows.Forms.TableLayoutPanel();
            this.lblParentsMarried = new System.Windows.Forms.Label();
            this.pnlParentsMarried = new System.Windows.Forms.Panel();
            this.tglParentsMarried = new CROMS.Modules.ToggleSwitch();
            this.lblParentsMarriedState = new System.Windows.Forms.Label();
            this.lblMarrDate = new System.Windows.Forms.Label();
            this.dtpMarrDate = new System.Windows.Forms.DateTimePicker();
            this.lblMarrPlace = new System.Windows.Forms.Label();
            this.txtMarrPlace = new System.Windows.Forms.TextBox();
            this.tabAttendant = new System.Windows.Forms.TabPage();
            this.tblAttendant = new System.Windows.Forms.TableLayoutPanel();
            this.lblAttType = new System.Windows.Forms.Label();
            this.cboAttType = new System.Windows.Forms.ComboBox();
            this.lblAttName = new System.Windows.Forms.Label();
            this.txtAttName = new System.Windows.Forms.TextBox();
            this.lblAttTitle = new System.Windows.Forms.Label();
            this.txtAttTitle = new System.Windows.Forms.TextBox();
            this.lblAttDate = new System.Windows.Forms.Label();
            this.dtpAttDate = new System.Windows.Forms.DateTimePicker();
            this.lblAttAddress = new System.Windows.Forms.Label();
            this.txtAttAddress = new System.Windows.Forms.TextBox();
            this.lblAttTypeOther = new System.Windows.Forms.Label();
            this.txtAttTypeOther = new System.Windows.Forms.TextBox();
            this.tabInformant = new System.Windows.Forms.TabPage();
            this.tblInformant = new System.Windows.Forms.TableLayoutPanel();
            this.lblInfName = new System.Windows.Forms.Label();
            this.txtInfName = new System.Windows.Forms.TextBox();
            this.lblInfRel = new System.Windows.Forms.Label();
            this.txtInfRel = new System.Windows.Forms.TextBox();
            this.lblInfDate = new System.Windows.Forms.Label();
            this.dtpInfDate = new System.Windows.Forms.DateTimePicker();
            this.lblInfRelOther = new System.Windows.Forms.Label();
            this.txtInfRelOther = new System.Windows.Forms.TextBox();
            this.lblInfAddress = new System.Windows.Forms.Label();
            this.txtInfAddress = new System.Windows.Forms.TextBox();
            this.tabCert = new System.Windows.Forms.TabPage();
            this.tblCert = new System.Windows.Forms.TableLayoutPanel();
            this.lblRegNo = new System.Windows.Forms.Label();
            this.txtRegNo = new System.Windows.Forms.TextBox();
            this.lblBook = new System.Windows.Forms.Label();
            this.txtBook = new System.Windows.Forms.TextBox();
            this.lblBookPage = new System.Windows.Forms.Label();
            this.txtBookPage = new System.Windows.Forms.TextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.cboStatus = new System.Windows.Forms.ComboBox();
            this.lblPreparedBy = new System.Windows.Forms.Label();
            this.txtPreparedBy = new System.Windows.Forms.TextBox();
            this.lblReceivedBy = new System.Windows.Forms.Label();
            this.txtReceivedBy = new System.Windows.Forms.TextBox();
            this.lblPreparedTitle = new System.Windows.Forms.Label();
            this.txtPreparedTitle = new System.Windows.Forms.TextBox();
            this.lblReceivedTitle = new System.Windows.Forms.Label();
            this.txtReceivedTitle = new System.Windows.Forms.TextBox();
            this.lblPreparedDate = new System.Windows.Forms.Label();
            this.dtpPreparedDate = new System.Windows.Forms.DateTimePicker();
            this.lblReceivedDate = new System.Windows.Forms.Label();
            this.dtpReceivedDate = new System.Windows.Forms.DateTimePicker();
            this.lblRegisteredBy = new System.Windows.Forms.Label();
            this.txtRegisteredBy = new System.Windows.Forms.TextBox();
            this.lblRegisteredTitle = new System.Windows.Forms.Label();
            this.txtRegisteredTitle = new System.Windows.Forms.TextBox();
            this.lblRegisteredDate = new System.Windows.Forms.Label();
            this.dtpRegisteredDate = new System.Windows.Forms.DateTimePicker();
            this.lblRemarks = new System.Windows.Forms.Label();
            this.txtRemarks = new System.Windows.Forms.TextBox();
            this.cardRecords = new CROMS.Modules.CardPanel();
            this.layoutRecords = new System.Windows.Forms.TableLayoutPanel();
            this.pnlRecordsHead = new System.Windows.Forms.TableLayoutPanel();
            this.lblRecent = new System.Windows.Forms.Label();
            this.pnlRecordActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.dgvBirths = new System.Windows.Forms.DataGridView();
            this.pnlSearch = new System.Windows.Forms.TableLayoutPanel();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnClearSearch = new System.Windows.Forms.Button();
            this.certificateMenu.SuspendLayout();
            this.layoutRoot.SuspendLayout();
            this.layoutMain.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            this.pnlHeadText.SuspendLayout();
            this.pnlHeadActions.SuspendLayout();
            this.cardForm.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabChild.SuspendLayout();
            this.tblChild.SuspendLayout();
            this.tabMother.SuspendLayout();
            this.tblMother.SuspendLayout();
            this.tabFather.SuspendLayout();
            this.tblFather.SuspendLayout();
            this.tabMarriage.SuspendLayout();
            this.tblMarriage.SuspendLayout();
            this.pnlParentsMarried.SuspendLayout();
            this.tabAttendant.SuspendLayout();
            this.tblAttendant.SuspendLayout();
            this.tabInformant.SuspendLayout();
            this.tblInformant.SuspendLayout();
            this.tabCert.SuspendLayout();
            this.tblCert.SuspendLayout();
            this.cardRecords.SuspendLayout();
            this.layoutRecords.SuspendLayout();
            this.pnlRecordsHead.SuspendLayout();
            this.pnlRecordActions.SuspendLayout();
            this.pnlSearch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBirths)).BeginInit();
            this.SuspendLayout();
            // 
            // certificateMenu
            // 
            this.certificateMenu.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.certificateMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuViewSoftcopy,
            this.mnuFactsCert});
            this.certificateMenu.Name = "certificateMenu";
            this.certificateMenu.Size = new System.Drawing.Size(220, 48);
            this.certificateMenu.Opening += new System.ComponentModel.CancelEventHandler(this.certificateMenu_Opening);
            //
            // mnuViewSoftcopy
            //
            this.mnuViewSoftcopy.Name = "mnuViewSoftcopy";
            this.mnuViewSoftcopy.Size = new System.Drawing.Size(219, 22);
            this.mnuViewSoftcopy.Text = "View Softcopy";
            this.mnuViewSoftcopy.Click += new System.EventHandler(this.btnViewScan_Click);
            //
            // mnuFactsCert
            //
            this.mnuFactsCert.Name = "mnuFactsCert";
            this.mnuFactsCert.Size = new System.Drawing.Size(219, 22);
            this.mnuFactsCert.Text = "Facts Certification (Form 1A)...";
            this.mnuFactsCert.Click += new System.EventHandler(this.mnuFactsCert_Click);
            // 
            // btnCertificate
            // 
            this.btnCertificate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(241)))), ((int)(((byte)(246)))));
            this.btnCertificate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCertificate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCertificate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCertificate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.btnCertificate.Location = new System.Drawing.Point(6, 6);
            this.btnCertificate.Margin = this.btnNew.Margin;
            this.btnCertificate.Menu = this.certificateMenu;
            this.btnCertificate.Name = "btnCertificate";
            this.btnCertificate.Size = new System.Drawing.Size(230, 30);
            this.btnCertificate.TabIndex = 3;
            this.btnCertificate.Tag = "noskin";
            this.btnCertificate.Text = "Print Certificate of Live Birth";
            this.btnCertificate.UseVisualStyleBackColor = false;
            this.btnCertificate.Click += new System.EventHandler(this.btnPrintCert_Click);
            // 
            // btnNew
            // 
            this.btnNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNew.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnNew.Location = new System.Drawing.Point(242, 6);
            this.btnNew.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(100, 30);
            this.btnNew.TabIndex = 0;
            this.btnNew.Text = "New Form";
            this.btnNew.UseVisualStyleBackColor = true;
            this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
            // 
            // layoutRoot
            // 
            this.layoutRoot.ColumnCount = 3;
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 1240F));
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.layoutRoot.Controls.Add(this.layoutMain, 1, 0);
            this.layoutRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutRoot.Location = new System.Drawing.Point(0, 0);
            this.layoutRoot.Name = "layoutRoot";
            this.layoutRoot.RowCount = 1;
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.Size = new System.Drawing.Size(1400, 900);
            this.layoutRoot.TabIndex = 0;
            // 
            // layoutMain
            // 
            this.layoutMain.ColumnCount = 1;
            this.layoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutMain.Controls.Add(this.pnlHeader, 0, 0);
            this.layoutMain.Controls.Add(this.cardForm, 0, 1);
            this.layoutMain.Controls.Add(this.cardRecords, 0, 2);
            this.layoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutMain.Location = new System.Drawing.Point(80, 0);
            this.layoutMain.Margin = new System.Windows.Forms.Padding(0);
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.Padding = new System.Windows.Forms.Padding(20, 16, 20, 18);
            this.layoutMain.RowCount = 3;
            this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 444F));
            this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutMain.Size = new System.Drawing.Size(1240, 900);
            this.layoutMain.TabIndex = 0;
            // 
            // pnlHeader
            // 
            this.pnlHeader.ColumnCount = 2;
            this.pnlHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.pnlHeader.Controls.Add(this.pnlHeadText, 0, 0);
            this.pnlHeader.Controls.Add(this.pnlHeadActions, 1, 0);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlHeader.Location = new System.Drawing.Point(20, 16);
            this.pnlHeader.Margin = new System.Windows.Forms.Padding(0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.RowCount = 1;
            this.pnlHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlHeader.Size = new System.Drawing.Size(1200, 96);
            this.pnlHeader.TabIndex = 0;
            // 
            // pnlHeadText
            // 
            this.pnlHeadText.Controls.Add(this.lblTitle);
            this.pnlHeadText.Controls.Add(this.lblSubtitle);
            this.pnlHeadText.Controls.Add(this.chkDelayed);
            this.pnlHeadText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlHeadText.Location = new System.Drawing.Point(0, 0);
            this.pnlHeadText.Margin = new System.Windows.Forms.Padding(0);
            this.pnlHeadText.Name = "pnlHeadText";
            this.pnlHeadText.Size = new System.Drawing.Size(716, 96);
            this.pnlHeadText.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 19F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(230, 36);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Birth Registration";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblSubtitle.Location = new System.Drawing.Point(2, 40);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(315, 15);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "MUNICIPAL FORM 102  •  NEW & DELAYED REGISTRATION";
            this.lblSubtitle.UseMnemonic = false;
            // 
            // chkDelayed
            // 
            this.chkDelayed.AutoSize = true;
            this.chkDelayed.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkDelayed.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.chkDelayed.Location = new System.Drawing.Point(2, 64);
            this.chkDelayed.Name = "chkDelayed";
            this.chkDelayed.Size = new System.Drawing.Size(297, 19);
            this.chkDelayed.TabIndex = 2;
            this.chkDelayed.Text = "Delayed Registration (event more than 30 days ago)";
            this.chkDelayed.UseVisualStyleBackColor = true;
            // 
            // pnlHeadActions
            // 
            this.pnlHeadActions.AutoSize = true;
            this.pnlHeadActions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlHeadActions.Controls.Add(this.btnSubmit);
            this.pnlHeadActions.Controls.Add(this.btnSaveDraft);
            this.pnlHeadActions.Controls.Add(this.btnOCRLiveBirth);
            this.pnlHeadActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlHeadActions.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlHeadActions.Location = new System.Drawing.Point(716, 0);
            this.pnlHeadActions.Margin = new System.Windows.Forms.Padding(0);
            this.pnlHeadActions.Name = "pnlHeadActions";
            this.pnlHeadActions.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.pnlHeadActions.Size = new System.Drawing.Size(484, 96);
            this.pnlHeadActions.TabIndex = 1;
            this.pnlHeadActions.WrapContents = false;
            // 
            // btnSubmit
            // 
            this.btnSubmit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.btnSubmit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSubmit.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSubmit.ForeColor = System.Drawing.Color.White;
            this.btnSubmit.Location = new System.Drawing.Point(312, 4);
            this.btnSubmit.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.btnSubmit.Name = "btnSubmit";
            this.btnSubmit.Size = new System.Drawing.Size(172, 40);
            this.btnSubmit.TabIndex = 1;
            this.btnSubmit.Text = "Submit for Approval";
            this.btnSubmit.UseVisualStyleBackColor = false;
            this.btnSubmit.Click += new System.EventHandler(this.btnSubmit_Click);
            // 
            // btnSaveDraft
            // 
            this.btnSaveDraft.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveDraft.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnSaveDraft.Location = new System.Drawing.Point(164, 4);
            this.btnSaveDraft.Margin = new System.Windows.Forms.Padding(0);
            this.btnSaveDraft.Name = "btnSaveDraft";
            this.btnSaveDraft.Size = new System.Drawing.Size(140, 40);
            this.btnSaveDraft.TabIndex = 0;
            this.btnSaveDraft.Text = "Save Draft";
            this.btnSaveDraft.UseVisualStyleBackColor = true;
            this.btnSaveDraft.Click += new System.EventHandler(this.btnSaveDraft_Click);
            // 
            // btnOCRLiveBirth
            // 
            this.btnOCRLiveBirth.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(241)))), ((int)(((byte)(254)))));
            this.btnOCRLiveBirth.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOCRLiveBirth.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnOCRLiveBirth.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.btnOCRLiveBirth.Location = new System.Drawing.Point(0, 4);
            this.btnOCRLiveBirth.Margin = new System.Windows.Forms.Padding(0, 0, 14, 0);
            this.btnOCRLiveBirth.Name = "btnOCRLiveBirth";
            this.btnOCRLiveBirth.Size = new System.Drawing.Size(150, 40);
            this.btnOCRLiveBirth.TabIndex = 2;
            this.btnOCRLiveBirth.Text = "Scan Document";
            this.btnOCRLiveBirth.UseVisualStyleBackColor = false;
            this.btnOCRLiveBirth.Click += new System.EventHandler(this.btnOCRLiveBirth_Click);
            // 
            // cardForm
            // 
            this.cardForm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.cardForm.CardColor = System.Drawing.Color.White;
            this.cardForm.Controls.Add(this.tabControl);
            this.cardForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardForm.DrawShadow = true;
            this.cardForm.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.cardForm.Location = new System.Drawing.Point(20, 112);
            this.cardForm.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
            this.cardForm.Name = "cardForm";
            this.cardForm.Padding = new System.Windows.Forms.Padding(14, 12, 14, 14);
            this.cardForm.Radius = 10;
            this.cardForm.Size = new System.Drawing.Size(1200, 430);
            this.cardForm.TabIndex = 1;
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabChild);
            this.tabControl.Controls.Add(this.tabMother);
            this.tabControl.Controls.Add(this.tabFather);
            this.tabControl.Controls.Add(this.tabMarriage);
            this.tabControl.Controls.Add(this.tabAttendant);
            this.tabControl.Controls.Add(this.tabInformant);
            this.tabControl.Controls.Add(this.tabCert);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.tabControl.Location = new System.Drawing.Point(14, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Drawing.Point(14, 5);
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1172, 404);
            this.tabControl.TabIndex = 0;
            // 
            // tabChild
            // 
            this.tabChild.AutoScroll = true;
            this.tabChild.BackColor = System.Drawing.Color.White;
            this.tabChild.Controls.Add(this.tblChild);
            this.tabChild.Location = new System.Drawing.Point(4, 30);
            this.tabChild.Name = "tabChild";
            this.tabChild.Padding = new System.Windows.Forms.Padding(18, 14, 18, 14);
            this.tabChild.Size = new System.Drawing.Size(1164, 370);
            this.tabChild.TabIndex = 0;
            this.tabChild.Text = "Child";
            // 
            // tblChild
            // 
            this.tblChild.ColumnCount = 4;
            this.tblChild.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblChild.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblChild.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblChild.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblChild.Controls.Add(this.lblFirstName, 0, 0);
            this.tblChild.Controls.Add(this.txtFirstName, 1, 0);
            this.tblChild.Controls.Add(this.lblMiddleName, 2, 0);
            this.tblChild.Controls.Add(this.txtMiddleName, 3, 0);
            this.tblChild.Controls.Add(this.lblLastName, 0, 1);
            this.tblChild.Controls.Add(this.txtLastName, 1, 1);
            this.tblChild.Controls.Add(this.lblSex, 2, 1);
            this.tblChild.Controls.Add(this.cboSex, 3, 1);
            this.tblChild.Controls.Add(this.lblDob, 0, 2);
            this.tblChild.Controls.Add(this.dtpDob, 1, 2);
            this.tblChild.Controls.Add(this.lblTypeOfBirth, 0, 3);
            this.tblChild.Controls.Add(this.cboTypeOfBirth, 1, 3);
            this.tblChild.Controls.Add(this.lblBirthOrder, 2, 3);
            this.tblChild.Controls.Add(this.txtBirthOrder, 3, 3);
            this.tblChild.Controls.Add(this.lblWeight, 0, 4);
            this.tblChild.Controls.Add(this.txtWeight, 1, 4);
            this.tblChild.Controls.Add(this.lblPlace, 0, 5);
            this.tblChild.Controls.Add(this.txtPlace, 1, 5);
            this.tblChild.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblChild.Location = new System.Drawing.Point(18, 14);
            this.tblChild.Name = "tblChild";
            this.tblChild.RowCount = 6;
            this.tblChild.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblChild.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblChild.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblChild.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblChild.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblChild.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.tblChild.Size = new System.Drawing.Size(1128, 284);
            this.tblChild.TabIndex = 0;
            // 
            // lblFirstName
            // 
            this.lblFirstName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFirstName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFirstName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblFirstName.Location = new System.Drawing.Point(3, 0);
            this.lblFirstName.Name = "lblFirstName";
            this.lblFirstName.Size = new System.Drawing.Size(172, 44);
            this.lblFirstName.TabIndex = 0;
            this.lblFirstName.Text = "First Name";
            this.lblFirstName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtFirstName
            // 
            this.txtFirstName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFirstName.Location = new System.Drawing.Point(181, 9);
            this.txtFirstName.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtFirstName.Name = "txtFirstName";
            this.txtFirstName.Size = new System.Drawing.Size(359, 25);
            this.txtFirstName.TabIndex = 1;
            // 
            // lblMiddleName
            // 
            this.lblMiddleName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMiddleName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMiddleName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMiddleName.Location = new System.Drawing.Point(567, 0);
            this.lblMiddleName.Name = "lblMiddleName";
            this.lblMiddleName.Size = new System.Drawing.Size(172, 44);
            this.lblMiddleName.TabIndex = 2;
            this.lblMiddleName.Text = "Middle Name";
            this.lblMiddleName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtMiddleName
            // 
            this.txtMiddleName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMiddleName.Location = new System.Drawing.Point(745, 9);
            this.txtMiddleName.Name = "txtMiddleName";
            this.txtMiddleName.Size = new System.Drawing.Size(380, 25);
            this.txtMiddleName.TabIndex = 3;
            // 
            // lblLastName
            // 
            this.lblLastName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLastName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblLastName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblLastName.Location = new System.Drawing.Point(3, 44);
            this.lblLastName.Name = "lblLastName";
            this.lblLastName.Size = new System.Drawing.Size(172, 44);
            this.lblLastName.TabIndex = 4;
            this.lblLastName.Text = "Last Name";
            this.lblLastName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtLastName
            // 
            this.txtLastName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLastName.Location = new System.Drawing.Point(181, 53);
            this.txtLastName.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtLastName.Name = "txtLastName";
            this.txtLastName.Size = new System.Drawing.Size(359, 25);
            this.txtLastName.TabIndex = 5;
            // 
            // lblSex
            // 
            this.lblSex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSex.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSex.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblSex.Location = new System.Drawing.Point(567, 44);
            this.lblSex.Name = "lblSex";
            this.lblSex.Size = new System.Drawing.Size(172, 44);
            this.lblSex.TabIndex = 6;
            this.lblSex.Text = "Sex";
            this.lblSex.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboSex
            // 
            this.cboSex.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cboSex.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cboSex.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cboSex.Items.AddRange(new object[] {
            "Male",
            "Female"});
            this.cboSex.Location = new System.Drawing.Point(745, 53);
            this.cboSex.Name = "cboSex";
            this.cboSex.Size = new System.Drawing.Size(200, 25);
            this.cboSex.TabIndex = 7;
            // 
            // lblDob
            // 
            this.lblDob.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDob.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDob.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblDob.Location = new System.Drawing.Point(3, 88);
            this.lblDob.Name = "lblDob";
            this.lblDob.Size = new System.Drawing.Size(172, 44);
            this.lblDob.TabIndex = 8;
            this.lblDob.Text = "Date of Birth";
            this.lblDob.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dtpDob
            // 
            this.dtpDob.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpDob.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDob.Location = new System.Drawing.Point(181, 97);
            this.dtpDob.Name = "dtpDob";
            this.dtpDob.Size = new System.Drawing.Size(200, 25);
            this.dtpDob.TabIndex = 9;
            // 
            // lblTypeOfBirth
            // 
            this.lblTypeOfBirth.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTypeOfBirth.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTypeOfBirth.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblTypeOfBirth.Location = new System.Drawing.Point(3, 132);
            this.lblTypeOfBirth.Name = "lblTypeOfBirth";
            this.lblTypeOfBirth.Size = new System.Drawing.Size(172, 44);
            this.lblTypeOfBirth.TabIndex = 12;
            this.lblTypeOfBirth.Text = "Type of Birth";
            this.lblTypeOfBirth.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboTypeOfBirth
            // 
            this.cboTypeOfBirth.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cboTypeOfBirth.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cboTypeOfBirth.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cboTypeOfBirth.Items.AddRange(new object[] {
            "Single",
            "Twin",
            "Triplet",
            "Quadruplet"});
            this.cboTypeOfBirth.Location = new System.Drawing.Point(181, 141);
            this.cboTypeOfBirth.Name = "cboTypeOfBirth";
            this.cboTypeOfBirth.Size = new System.Drawing.Size(240, 25);
            this.cboTypeOfBirth.TabIndex = 13;
            // 
            // lblBirthOrder
            // 
            this.lblBirthOrder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBirthOrder.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBirthOrder.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBirthOrder.Location = new System.Drawing.Point(567, 132);
            this.lblBirthOrder.Name = "lblBirthOrder";
            this.lblBirthOrder.Size = new System.Drawing.Size(172, 44);
            this.lblBirthOrder.TabIndex = 14;
            this.lblBirthOrder.Text = "Birth Order";
            this.lblBirthOrder.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtBirthOrder
            // 
            this.txtBirthOrder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtBirthOrder.Location = new System.Drawing.Point(745, 141);
            this.txtBirthOrder.Name = "txtBirthOrder";
            this.txtBirthOrder.Size = new System.Drawing.Size(260, 25);
            this.txtBirthOrder.TabIndex = 15;
            // 
            // lblWeight
            // 
            this.lblWeight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWeight.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblWeight.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblWeight.Location = new System.Drawing.Point(3, 176);
            this.lblWeight.Name = "lblWeight";
            this.lblWeight.Size = new System.Drawing.Size(172, 44);
            this.lblWeight.TabIndex = 16;
            this.lblWeight.Text = "Weight at Birth (grams)";
            this.lblWeight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtWeight
            // 
            this.txtWeight.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtWeight.Location = new System.Drawing.Point(181, 185);
            this.txtWeight.Name = "txtWeight";
            this.txtWeight.Size = new System.Drawing.Size(200, 25);
            this.txtWeight.TabIndex = 17;
            // 
            // lblPlace
            // 
            this.lblPlace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlace.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPlace.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblPlace.Location = new System.Drawing.Point(3, 220);
            this.lblPlace.Name = "lblPlace";
            this.lblPlace.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblPlace.Size = new System.Drawing.Size(172, 64);
            this.lblPlace.TabIndex = 18;
            this.lblPlace.Text = "Place of Birth";
            // 
            // txtPlace
            // 
            this.txtPlace.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tblChild.SetColumnSpan(this.txtPlace, 3);
            this.txtPlace.Location = new System.Drawing.Point(181, 225);
            this.txtPlace.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.txtPlace.Name = "txtPlace";
            this.txtPlace.Size = new System.Drawing.Size(944, 25);
            this.txtPlace.TabIndex = 19;
            // 
            // tabMother
            // 
            this.tabMother.AutoScroll = true;
            this.tabMother.BackColor = System.Drawing.Color.White;
            this.tabMother.Controls.Add(this.tblMother);
            this.tabMother.Location = new System.Drawing.Point(4, 30);
            this.tabMother.Name = "tabMother";
            this.tabMother.Padding = new System.Windows.Forms.Padding(18, 14, 18, 14);
            this.tabMother.Size = new System.Drawing.Size(1164, 370);
            this.tabMother.TabIndex = 1;
            this.tabMother.Text = "Mother";
            // 
            // tblMother
            // 
            this.tblMother.ColumnCount = 4;
            this.tblMother.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblMother.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblMother.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblMother.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblMother.Controls.Add(this.lblMFirst, 0, 0);
            this.tblMother.Controls.Add(this.txtMFirst, 1, 0);
            this.tblMother.Controls.Add(this.lblMMiddle, 2, 0);
            this.tblMother.Controls.Add(this.txtMMiddle, 3, 0);
            this.tblMother.Controls.Add(this.lblMLast, 0, 1);
            this.tblMother.Controls.Add(this.txtMLast, 1, 1);
            this.tblMother.Controls.Add(this.lblMCitizen, 2, 1);
            this.tblMother.Controls.Add(this.txtMCitizen, 3, 1);
            this.tblMother.Controls.Add(this.lblMReligion, 0, 2);
            this.tblMother.Controls.Add(this.txtMReligion, 1, 2);
            this.tblMother.Controls.Add(this.lblMOccupation, 2, 2);
            this.tblMother.Controls.Add(this.txtMOccupation, 3, 2);
            this.tblMother.Controls.Add(this.lblMAge, 0, 3);
            this.tblMother.Controls.Add(this.txtMAge, 1, 3);
            this.tblMother.Controls.Add(this.lblMBornAlive, 2, 3);
            this.tblMother.Controls.Add(this.txtMBornAlive, 3, 3);
            this.tblMother.Controls.Add(this.lblMLiving, 0, 4);
            this.tblMother.Controls.Add(this.txtMLiving, 1, 4);
            this.tblMother.Controls.Add(this.lblMDead, 2, 4);
            this.tblMother.Controls.Add(this.txtMDead, 3, 4);
            this.tblMother.Controls.Add(this.lblMResidence, 0, 5);
            this.tblMother.Controls.Add(this.txtMResidence, 1, 5);
            this.tblMother.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblMother.Location = new System.Drawing.Point(18, 14);
            this.tblMother.Name = "tblMother";
            this.tblMother.RowCount = 6;
            this.tblMother.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblMother.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblMother.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblMother.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblMother.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblMother.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.tblMother.Size = new System.Drawing.Size(1128, 284);
            this.tblMother.TabIndex = 0;
            // 
            // lblMFirst
            // 
            this.lblMFirst.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMFirst.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMFirst.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMFirst.Location = new System.Drawing.Point(3, 0);
            this.lblMFirst.Name = "lblMFirst";
            this.lblMFirst.Size = new System.Drawing.Size(172, 44);
            this.lblMFirst.TabIndex = 0;
            this.lblMFirst.Text = "Maiden First Name";
            this.lblMFirst.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtMFirst
            // 
            this.txtMFirst.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMFirst.Location = new System.Drawing.Point(181, 9);
            this.txtMFirst.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtMFirst.Name = "txtMFirst";
            this.txtMFirst.Size = new System.Drawing.Size(359, 25);
            this.txtMFirst.TabIndex = 1;
            // 
            // lblMMiddle
            // 
            this.lblMMiddle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMMiddle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMMiddle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMMiddle.Location = new System.Drawing.Point(567, 0);
            this.lblMMiddle.Name = "lblMMiddle";
            this.lblMMiddle.Size = new System.Drawing.Size(172, 44);
            this.lblMMiddle.TabIndex = 2;
            this.lblMMiddle.Text = "Middle Name";
            this.lblMMiddle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtMMiddle
            // 
            this.txtMMiddle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMMiddle.Location = new System.Drawing.Point(745, 9);
            this.txtMMiddle.Name = "txtMMiddle";
            this.txtMMiddle.Size = new System.Drawing.Size(380, 25);
            this.txtMMiddle.TabIndex = 3;
            // 
            // lblMLast
            // 
            this.lblMLast.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMLast.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMLast.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMLast.Location = new System.Drawing.Point(3, 44);
            this.lblMLast.Name = "lblMLast";
            this.lblMLast.Size = new System.Drawing.Size(172, 44);
            this.lblMLast.TabIndex = 4;
            this.lblMLast.Text = "Last Name";
            this.lblMLast.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtMLast
            // 
            this.txtMLast.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMLast.Location = new System.Drawing.Point(181, 53);
            this.txtMLast.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtMLast.Name = "txtMLast";
            this.txtMLast.Size = new System.Drawing.Size(359, 25);
            this.txtMLast.TabIndex = 5;
            // 
            // lblMCitizen
            // 
            this.lblMCitizen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMCitizen.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMCitizen.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMCitizen.Location = new System.Drawing.Point(567, 44);
            this.lblMCitizen.Name = "lblMCitizen";
            this.lblMCitizen.Size = new System.Drawing.Size(172, 44);
            this.lblMCitizen.TabIndex = 6;
            this.lblMCitizen.Text = "Citizenship";
            this.lblMCitizen.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtMCitizen
            // 
            this.txtMCitizen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMCitizen.Location = new System.Drawing.Point(745, 53);
            this.txtMCitizen.Name = "txtMCitizen";
            this.txtMCitizen.Size = new System.Drawing.Size(380, 25);
            this.txtMCitizen.TabIndex = 7;
            // 
            // lblMReligion
            // 
            this.lblMReligion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMReligion.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMReligion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMReligion.Location = new System.Drawing.Point(3, 88);
            this.lblMReligion.Name = "lblMReligion";
            this.lblMReligion.Size = new System.Drawing.Size(172, 44);
            this.lblMReligion.TabIndex = 8;
            this.lblMReligion.Text = "Religion";
            this.lblMReligion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtMReligion
            // 
            this.txtMReligion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMReligion.Location = new System.Drawing.Point(181, 97);
            this.txtMReligion.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtMReligion.Name = "txtMReligion";
            this.txtMReligion.Size = new System.Drawing.Size(359, 25);
            this.txtMReligion.TabIndex = 9;
            // 
            // lblMOccupation
            // 
            this.lblMOccupation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMOccupation.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMOccupation.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMOccupation.Location = new System.Drawing.Point(567, 88);
            this.lblMOccupation.Name = "lblMOccupation";
            this.lblMOccupation.Size = new System.Drawing.Size(172, 44);
            this.lblMOccupation.TabIndex = 10;
            this.lblMOccupation.Text = "Occupation";
            this.lblMOccupation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtMOccupation
            // 
            this.txtMOccupation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMOccupation.Location = new System.Drawing.Point(745, 97);
            this.txtMOccupation.Name = "txtMOccupation";
            this.txtMOccupation.Size = new System.Drawing.Size(380, 25);
            this.txtMOccupation.TabIndex = 11;
            // 
            // lblMAge
            // 
            this.lblMAge.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMAge.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMAge.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMAge.Location = new System.Drawing.Point(3, 132);
            this.lblMAge.Name = "lblMAge";
            this.lblMAge.Size = new System.Drawing.Size(172, 44);
            this.lblMAge.TabIndex = 12;
            this.lblMAge.Text = "Age at time of birth";
            this.lblMAge.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtMAge
            // 
            this.txtMAge.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtMAge.Location = new System.Drawing.Point(181, 141);
            this.txtMAge.Name = "txtMAge";
            this.txtMAge.Size = new System.Drawing.Size(120, 25);
            this.txtMAge.TabIndex = 13;
            // 
            // lblMBornAlive
            // 
            this.lblMBornAlive.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMBornAlive.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMBornAlive.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMBornAlive.Location = new System.Drawing.Point(567, 132);
            this.lblMBornAlive.Name = "lblMBornAlive";
            this.lblMBornAlive.Size = new System.Drawing.Size(172, 44);
            this.lblMBornAlive.TabIndex = 14;
            this.lblMBornAlive.Text = "Total children born alive";
            this.lblMBornAlive.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtMBornAlive
            // 
            this.txtMBornAlive.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtMBornAlive.Location = new System.Drawing.Point(745, 141);
            this.txtMBornAlive.Name = "txtMBornAlive";
            this.txtMBornAlive.Size = new System.Drawing.Size(120, 25);
            this.txtMBornAlive.TabIndex = 15;
            // 
            // lblMLiving
            // 
            this.lblMLiving.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMLiving.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMLiving.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMLiving.Location = new System.Drawing.Point(3, 176);
            this.lblMLiving.Name = "lblMLiving";
            this.lblMLiving.Size = new System.Drawing.Size(172, 44);
            this.lblMLiving.TabIndex = 16;
            this.lblMLiving.Text = "Children still living";
            this.lblMLiving.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtMLiving
            // 
            this.txtMLiving.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtMLiving.Location = new System.Drawing.Point(181, 185);
            this.txtMLiving.Name = "txtMLiving";
            this.txtMLiving.Size = new System.Drawing.Size(120, 25);
            this.txtMLiving.TabIndex = 17;
            // 
            // lblMDead
            // 
            this.lblMDead.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMDead.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMDead.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMDead.Location = new System.Drawing.Point(567, 176);
            this.lblMDead.Name = "lblMDead";
            this.lblMDead.Size = new System.Drawing.Size(172, 44);
            this.lblMDead.TabIndex = 18;
            this.lblMDead.Text = "Children born alive, now dead";
            this.lblMDead.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtMDead
            // 
            this.txtMDead.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtMDead.Location = new System.Drawing.Point(745, 185);
            this.txtMDead.Name = "txtMDead";
            this.txtMDead.Size = new System.Drawing.Size(120, 25);
            this.txtMDead.TabIndex = 19;
            // 
            // lblMResidence
            // 
            this.lblMResidence.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMResidence.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMResidence.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMResidence.Location = new System.Drawing.Point(3, 220);
            this.lblMResidence.Name = "lblMResidence";
            this.lblMResidence.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblMResidence.Size = new System.Drawing.Size(172, 64);
            this.lblMResidence.TabIndex = 20;
            this.lblMResidence.Text = "Residence";
            // 
            // txtMResidence
            // 
            this.txtMResidence.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tblMother.SetColumnSpan(this.txtMResidence, 3);
            this.txtMResidence.Location = new System.Drawing.Point(181, 225);
            this.txtMResidence.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.txtMResidence.Name = "txtMResidence";
            this.txtMResidence.Size = new System.Drawing.Size(944, 25);
            this.txtMResidence.TabIndex = 21;
            // 
            // tabFather
            // 
            this.tabFather.AutoScroll = true;
            this.tabFather.BackColor = System.Drawing.Color.White;
            this.tabFather.Controls.Add(this.tblFather);
            this.tabFather.Location = new System.Drawing.Point(4, 30);
            this.tabFather.Name = "tabFather";
            this.tabFather.Padding = new System.Windows.Forms.Padding(18, 14, 18, 14);
            this.tabFather.Size = new System.Drawing.Size(1164, 370);
            this.tabFather.TabIndex = 2;
            this.tabFather.Text = "Father";
            // 
            // tblFather
            // 
            this.tblFather.ColumnCount = 4;
            this.tblFather.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblFather.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblFather.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblFather.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblFather.Controls.Add(this.lblFFirst, 0, 0);
            this.tblFather.Controls.Add(this.txtFFirst, 1, 0);
            this.tblFather.Controls.Add(this.lblFMiddle, 2, 0);
            this.tblFather.Controls.Add(this.txtFMiddle, 3, 0);
            this.tblFather.Controls.Add(this.lblFLast, 0, 1);
            this.tblFather.Controls.Add(this.txtFLast, 1, 1);
            this.tblFather.Controls.Add(this.lblFCitizen, 2, 1);
            this.tblFather.Controls.Add(this.txtFCitizen, 3, 1);
            this.tblFather.Controls.Add(this.lblFReligion, 0, 2);
            this.tblFather.Controls.Add(this.txtFReligion, 1, 2);
            this.tblFather.Controls.Add(this.lblFOccupation, 2, 2);
            this.tblFather.Controls.Add(this.txtFOccupation, 3, 2);
            this.tblFather.Controls.Add(this.lblFAge, 0, 3);
            this.tblFather.Controls.Add(this.txtFAge, 1, 3);
            this.tblFather.Controls.Add(this.lblFResidence, 0, 4);
            this.tblFather.Controls.Add(this.txtFResidence, 1, 4);
            this.tblFather.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblFather.Location = new System.Drawing.Point(18, 14);
            this.tblFather.Name = "tblFather";
            this.tblFather.RowCount = 5;
            this.tblFather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblFather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblFather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblFather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblFather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.tblFather.Size = new System.Drawing.Size(1128, 240);
            this.tblFather.TabIndex = 0;
            // 
            // lblFFirst
            // 
            this.lblFFirst.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFFirst.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFFirst.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblFFirst.Location = new System.Drawing.Point(3, 0);
            this.lblFFirst.Name = "lblFFirst";
            this.lblFFirst.Size = new System.Drawing.Size(172, 44);
            this.lblFFirst.TabIndex = 0;
            this.lblFFirst.Text = "First Name";
            this.lblFFirst.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtFFirst
            // 
            this.txtFFirst.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFFirst.Location = new System.Drawing.Point(181, 9);
            this.txtFFirst.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtFFirst.Name = "txtFFirst";
            this.txtFFirst.Size = new System.Drawing.Size(359, 25);
            this.txtFFirst.TabIndex = 1;
            // 
            // lblFMiddle
            // 
            this.lblFMiddle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFMiddle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFMiddle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblFMiddle.Location = new System.Drawing.Point(567, 0);
            this.lblFMiddle.Name = "lblFMiddle";
            this.lblFMiddle.Size = new System.Drawing.Size(172, 44);
            this.lblFMiddle.TabIndex = 2;
            this.lblFMiddle.Text = "Middle Name";
            this.lblFMiddle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtFMiddle
            // 
            this.txtFMiddle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFMiddle.Location = new System.Drawing.Point(745, 9);
            this.txtFMiddle.Name = "txtFMiddle";
            this.txtFMiddle.Size = new System.Drawing.Size(380, 25);
            this.txtFMiddle.TabIndex = 3;
            // 
            // lblFLast
            // 
            this.lblFLast.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFLast.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFLast.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblFLast.Location = new System.Drawing.Point(3, 44);
            this.lblFLast.Name = "lblFLast";
            this.lblFLast.Size = new System.Drawing.Size(172, 44);
            this.lblFLast.TabIndex = 4;
            this.lblFLast.Text = "Last Name";
            this.lblFLast.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtFLast
            // 
            this.txtFLast.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFLast.Location = new System.Drawing.Point(181, 53);
            this.txtFLast.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtFLast.Name = "txtFLast";
            this.txtFLast.Size = new System.Drawing.Size(359, 25);
            this.txtFLast.TabIndex = 5;
            // 
            // lblFCitizen
            // 
            this.lblFCitizen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFCitizen.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFCitizen.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblFCitizen.Location = new System.Drawing.Point(567, 44);
            this.lblFCitizen.Name = "lblFCitizen";
            this.lblFCitizen.Size = new System.Drawing.Size(172, 44);
            this.lblFCitizen.TabIndex = 6;
            this.lblFCitizen.Text = "Citizenship";
            this.lblFCitizen.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtFCitizen
            // 
            this.txtFCitizen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFCitizen.Location = new System.Drawing.Point(745, 53);
            this.txtFCitizen.Name = "txtFCitizen";
            this.txtFCitizen.Size = new System.Drawing.Size(380, 25);
            this.txtFCitizen.TabIndex = 7;
            // 
            // lblFReligion
            // 
            this.lblFReligion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFReligion.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFReligion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblFReligion.Location = new System.Drawing.Point(3, 88);
            this.lblFReligion.Name = "lblFReligion";
            this.lblFReligion.Size = new System.Drawing.Size(172, 44);
            this.lblFReligion.TabIndex = 8;
            this.lblFReligion.Text = "Religion";
            this.lblFReligion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtFReligion
            // 
            this.txtFReligion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFReligion.Location = new System.Drawing.Point(181, 97);
            this.txtFReligion.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtFReligion.Name = "txtFReligion";
            this.txtFReligion.Size = new System.Drawing.Size(359, 25);
            this.txtFReligion.TabIndex = 9;
            // 
            // lblFOccupation
            // 
            this.lblFOccupation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFOccupation.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFOccupation.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblFOccupation.Location = new System.Drawing.Point(567, 88);
            this.lblFOccupation.Name = "lblFOccupation";
            this.lblFOccupation.Size = new System.Drawing.Size(172, 44);
            this.lblFOccupation.TabIndex = 10;
            this.lblFOccupation.Text = "Occupation";
            this.lblFOccupation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtFOccupation
            // 
            this.txtFOccupation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFOccupation.Location = new System.Drawing.Point(745, 97);
            this.txtFOccupation.Name = "txtFOccupation";
            this.txtFOccupation.Size = new System.Drawing.Size(380, 25);
            this.txtFOccupation.TabIndex = 11;
            // 
            // lblFAge
            // 
            this.lblFAge.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFAge.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFAge.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblFAge.Location = new System.Drawing.Point(3, 132);
            this.lblFAge.Name = "lblFAge";
            this.lblFAge.Size = new System.Drawing.Size(172, 44);
            this.lblFAge.TabIndex = 12;
            this.lblFAge.Text = "Age at time of birth";
            this.lblFAge.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtFAge
            // 
            this.txtFAge.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtFAge.Location = new System.Drawing.Point(181, 141);
            this.txtFAge.Name = "txtFAge";
            this.txtFAge.Size = new System.Drawing.Size(120, 25);
            this.txtFAge.TabIndex = 13;
            // 
            // lblFResidence
            // 
            this.lblFResidence.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFResidence.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFResidence.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblFResidence.Location = new System.Drawing.Point(3, 176);
            this.lblFResidence.Name = "lblFResidence";
            this.lblFResidence.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblFResidence.Size = new System.Drawing.Size(172, 64);
            this.lblFResidence.TabIndex = 14;
            this.lblFResidence.Text = "Residence";
            // 
            // txtFResidence
            // 
            this.txtFResidence.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tblFather.SetColumnSpan(this.txtFResidence, 3);
            this.txtFResidence.Location = new System.Drawing.Point(181, 181);
            this.txtFResidence.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.txtFResidence.Name = "txtFResidence";
            this.txtFResidence.Size = new System.Drawing.Size(944, 25);
            this.txtFResidence.TabIndex = 15;
            // 
            // tabMarriage
            // 
            this.tabMarriage.AutoScroll = true;
            this.tabMarriage.BackColor = System.Drawing.Color.White;
            this.tabMarriage.Controls.Add(this.tblMarriage);
            this.tabMarriage.Location = new System.Drawing.Point(4, 30);
            this.tabMarriage.Name = "tabMarriage";
            this.tabMarriage.Padding = new System.Windows.Forms.Padding(18, 14, 18, 14);
            this.tabMarriage.Size = new System.Drawing.Size(1164, 370);
            this.tabMarriage.TabIndex = 3;
            this.tabMarriage.Text = "Marriage of Parents";
            // 
            // tblMarriage
            // 
            this.tblMarriage.ColumnCount = 4;
            this.tblMarriage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblMarriage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblMarriage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblMarriage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblMarriage.Controls.Add(this.lblParentsMarried, 0, 0);
            this.tblMarriage.Controls.Add(this.pnlParentsMarried, 1, 0);
            this.tblMarriage.Controls.Add(this.lblMarrDate, 0, 1);
            this.tblMarriage.Controls.Add(this.dtpMarrDate, 1, 1);
            this.tblMarriage.Controls.Add(this.lblMarrPlace, 0, 2);
            this.tblMarriage.Controls.Add(this.txtMarrPlace, 1, 2);
            this.tblMarriage.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblMarriage.Location = new System.Drawing.Point(18, 14);
            this.tblMarriage.Name = "tblMarriage";
            this.tblMarriage.RowCount = 3;
            this.tblMarriage.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblMarriage.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblMarriage.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.tblMarriage.Size = new System.Drawing.Size(1128, 152);
            this.tblMarriage.TabIndex = 0;
            // 
            // lblParentsMarried
            // 
            this.lblParentsMarried.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblParentsMarried.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblParentsMarried.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblParentsMarried.Location = new System.Drawing.Point(3, 0);
            this.lblParentsMarried.Name = "lblParentsMarried";
            this.lblParentsMarried.Size = new System.Drawing.Size(172, 44);
            this.lblParentsMarried.TabIndex = 0;
            this.lblParentsMarried.Text = "Parents Married?";
            this.lblParentsMarried.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlParentsMarried
            // 
            this.pnlParentsMarried.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.pnlParentsMarried.BackColor = System.Drawing.Color.Transparent;
            this.pnlParentsMarried.Controls.Add(this.tglParentsMarried);
            this.pnlParentsMarried.Controls.Add(this.lblParentsMarriedState);
            this.pnlParentsMarried.Location = new System.Drawing.Point(181, 8);
            this.pnlParentsMarried.Name = "pnlParentsMarried";
            this.pnlParentsMarried.Size = new System.Drawing.Size(380, 28);
            this.pnlParentsMarried.TabIndex = 1;
            // 
            // tglParentsMarried
            // 
            this.tglParentsMarried.BackColor = System.Drawing.Color.Transparent;
            this.tglParentsMarried.Checked = false;
            this.tglParentsMarried.Cursor = System.Windows.Forms.Cursors.Hand;
            this.tglParentsMarried.KnobColor = System.Drawing.Color.White;
            this.tglParentsMarried.Location = new System.Drawing.Point(0, 2);
            this.tglParentsMarried.Name = "tglParentsMarried";
            this.tglParentsMarried.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(206)))), ((int)(((byte)(212)))), ((int)(((byte)(218)))));
            this.tglParentsMarried.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.tglParentsMarried.Size = new System.Drawing.Size(52, 26);
            this.tglParentsMarried.TabIndex = 0;
            // 
            // lblParentsMarriedState
            // 
            this.lblParentsMarriedState.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblParentsMarriedState.Location = new System.Drawing.Point(62, 2);
            this.lblParentsMarriedState.Name = "lblParentsMarriedState";
            this.lblParentsMarriedState.Size = new System.Drawing.Size(350, 26);
            this.lblParentsMarriedState.TabIndex = 1;
            this.lblParentsMarriedState.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMarrDate
            // 
            this.lblMarrDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMarrDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMarrDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMarrDate.Location = new System.Drawing.Point(3, 44);
            this.lblMarrDate.Name = "lblMarrDate";
            this.lblMarrDate.Size = new System.Drawing.Size(172, 44);
            this.lblMarrDate.TabIndex = 0;
            this.lblMarrDate.Text = "Date of Marriage";
            this.lblMarrDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dtpMarrDate
            // 
            this.dtpMarrDate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpMarrDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpMarrDate.Location = new System.Drawing.Point(181, 53);
            this.dtpMarrDate.Name = "dtpMarrDate";
            this.dtpMarrDate.ShowCheckBox = true;
            this.dtpMarrDate.Size = new System.Drawing.Size(220, 25);
            this.dtpMarrDate.TabIndex = 1;
            // 
            // lblMarrPlace
            // 
            this.lblMarrPlace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMarrPlace.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMarrPlace.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMarrPlace.Location = new System.Drawing.Point(3, 88);
            this.lblMarrPlace.Name = "lblMarrPlace";
            this.lblMarrPlace.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblMarrPlace.Size = new System.Drawing.Size(172, 64);
            this.lblMarrPlace.TabIndex = 2;
            this.lblMarrPlace.Text = "Place of Marriage";
            // 
            // txtMarrPlace
            // 
            this.txtMarrPlace.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tblMarriage.SetColumnSpan(this.txtMarrPlace, 3);
            this.txtMarrPlace.Location = new System.Drawing.Point(181, 93);
            this.txtMarrPlace.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.txtMarrPlace.Name = "txtMarrPlace";
            this.txtMarrPlace.Size = new System.Drawing.Size(944, 25);
            this.txtMarrPlace.TabIndex = 3;
            // 
            // tabAttendant
            // 
            this.tabAttendant.AutoScroll = true;
            this.tabAttendant.BackColor = System.Drawing.Color.White;
            this.tabAttendant.Controls.Add(this.tblAttendant);
            this.tabAttendant.Location = new System.Drawing.Point(4, 30);
            this.tabAttendant.Name = "tabAttendant";
            this.tabAttendant.Padding = new System.Windows.Forms.Padding(18, 14, 18, 14);
            this.tabAttendant.Size = new System.Drawing.Size(1164, 370);
            this.tabAttendant.TabIndex = 4;
            this.tabAttendant.Text = "Attendant";
            // 
            // tblAttendant
            // 
            this.tblAttendant.ColumnCount = 4;
            this.tblAttendant.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblAttendant.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblAttendant.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblAttendant.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblAttendant.Controls.Add(this.lblAttType, 0, 0);
            this.tblAttendant.Controls.Add(this.cboAttType, 1, 0);
            this.tblAttendant.Controls.Add(this.lblAttName, 2, 0);
            this.tblAttendant.Controls.Add(this.txtAttName, 3, 0);
            this.tblAttendant.Controls.Add(this.lblAttTitle, 0, 1);
            this.tblAttendant.Controls.Add(this.txtAttTitle, 1, 1);
            this.tblAttendant.Controls.Add(this.lblAttDate, 2, 1);
            this.tblAttendant.Controls.Add(this.dtpAttDate, 3, 1);
            this.tblAttendant.Controls.Add(this.lblAttAddress, 0, 2);
            this.tblAttendant.Controls.Add(this.txtAttAddress, 1, 2);
            this.tblAttendant.Controls.Add(this.lblAttTypeOther, 2, 2);
            this.tblAttendant.Controls.Add(this.txtAttTypeOther, 3, 2);
            this.tblAttendant.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblAttendant.Location = new System.Drawing.Point(18, 14);
            this.tblAttendant.Name = "tblAttendant";
            this.tblAttendant.RowCount = 3;
            this.tblAttendant.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblAttendant.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblAttendant.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.tblAttendant.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tblAttendant.Size = new System.Drawing.Size(1128, 152);
            this.tblAttendant.TabIndex = 0;
            // 
            // lblAttType
            // 
            this.lblAttType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAttType.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAttType.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblAttType.Location = new System.Drawing.Point(3, 0);
            this.lblAttType.Name = "lblAttType";
            this.lblAttType.Size = new System.Drawing.Size(172, 44);
            this.lblAttType.TabIndex = 0;
            this.lblAttType.Text = "Attendant Type";
            this.lblAttType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboAttType
            // 
            this.cboAttType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cboAttType.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cboAttType.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cboAttType.Items.AddRange(new object[] {
            "Physician",
            "Nurse",
            "Midwife",
            "Hilot (Traditional)",
            "Others"});
            this.cboAttType.Location = new System.Drawing.Point(181, 9);
            this.cboAttType.Name = "cboAttType";
            this.cboAttType.Size = new System.Drawing.Size(260, 25);
            this.cboAttType.TabIndex = 1;
            // 
            // lblAttName
            // 
            this.lblAttName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAttName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAttName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblAttName.Location = new System.Drawing.Point(567, 0);
            this.lblAttName.Name = "lblAttName";
            this.lblAttName.Size = new System.Drawing.Size(172, 44);
            this.lblAttName.TabIndex = 2;
            this.lblAttName.Text = "Name";
            this.lblAttName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtAttName
            // 
            this.txtAttName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAttName.Location = new System.Drawing.Point(745, 9);
            this.txtAttName.Name = "txtAttName";
            this.txtAttName.Size = new System.Drawing.Size(380, 25);
            this.txtAttName.TabIndex = 3;
            // 
            // lblAttTitle
            // 
            this.lblAttTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAttTitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAttTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblAttTitle.Location = new System.Drawing.Point(3, 44);
            this.lblAttTitle.Name = "lblAttTitle";
            this.lblAttTitle.Size = new System.Drawing.Size(172, 44);
            this.lblAttTitle.TabIndex = 4;
            this.lblAttTitle.Text = "Title / Position";
            this.lblAttTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtAttTitle
            // 
            this.txtAttTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAttTitle.Location = new System.Drawing.Point(181, 53);
            this.txtAttTitle.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtAttTitle.Name = "txtAttTitle";
            this.txtAttTitle.Size = new System.Drawing.Size(359, 25);
            this.txtAttTitle.TabIndex = 5;
            // 
            // lblAttDate
            // 
            this.lblAttDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAttDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAttDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblAttDate.Location = new System.Drawing.Point(567, 44);
            this.lblAttDate.Name = "lblAttDate";
            this.lblAttDate.Size = new System.Drawing.Size(172, 44);
            this.lblAttDate.TabIndex = 20;
            this.lblAttDate.Text = "Date Signed";
            this.lblAttDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dtpAttDate
            // 
            this.dtpAttDate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpAttDate.Checked = false;
            this.dtpAttDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpAttDate.Location = new System.Drawing.Point(745, 53);
            this.dtpAttDate.Name = "dtpAttDate";
            this.dtpAttDate.ShowCheckBox = true;
            this.dtpAttDate.Size = new System.Drawing.Size(200, 25);
            this.dtpAttDate.TabIndex = 21;
            // 
            // lblAttAddress
            // 
            this.lblAttAddress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAttAddress.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAttAddress.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblAttAddress.Location = new System.Drawing.Point(3, 88);
            this.lblAttAddress.Name = "lblAttAddress";
            this.lblAttAddress.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblAttAddress.Size = new System.Drawing.Size(172, 64);
            this.lblAttAddress.TabIndex = 6;
            this.lblAttAddress.Text = "Address";
            // 
            // txtAttAddress
            // 
            this.txtAttAddress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tblAttendant.SetColumnSpan(this.txtAttAddress, 3);
            this.txtAttAddress.Location = new System.Drawing.Point(181, 93);
            this.txtAttAddress.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.txtAttAddress.Name = "txtAttAddress";
            this.txtAttAddress.Size = new System.Drawing.Size(944, 25);
            this.txtAttAddress.TabIndex = 7;
            // 
            // lblAttTypeOther
            // 
            this.lblAttTypeOther.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAttTypeOther.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAttTypeOther.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblAttTypeOther.Location = new System.Drawing.Point(3, 152);
            this.lblAttTypeOther.Name = "lblAttTypeOther";
            this.lblAttTypeOther.Size = new System.Drawing.Size(172, 20);
            this.lblAttTypeOther.TabIndex = 22;
            this.lblAttTypeOther.Text = "If Others, specify";
            this.lblAttTypeOther.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblAttTypeOther.Visible = false;
            // 
            // txtAttTypeOther
            // 
            this.txtAttTypeOther.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAttTypeOther.Location = new System.Drawing.Point(181, 155);
            this.txtAttTypeOther.Name = "txtAttTypeOther";
            this.txtAttTypeOther.Size = new System.Drawing.Size(380, 25);
            this.txtAttTypeOther.TabIndex = 23;
            this.txtAttTypeOther.Visible = false;
            // 
            // tabInformant
            // 
            this.tabInformant.AutoScroll = true;
            this.tabInformant.BackColor = System.Drawing.Color.White;
            this.tabInformant.Controls.Add(this.tblInformant);
            this.tabInformant.Location = new System.Drawing.Point(4, 30);
            this.tabInformant.Name = "tabInformant";
            this.tabInformant.Padding = new System.Windows.Forms.Padding(18, 14, 18, 14);
            this.tabInformant.Size = new System.Drawing.Size(1164, 370);
            this.tabInformant.TabIndex = 5;
            this.tabInformant.Text = "Informant";
            // 
            // tblInformant
            // 
            this.tblInformant.ColumnCount = 4;
            this.tblInformant.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblInformant.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblInformant.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblInformant.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblInformant.Controls.Add(this.lblInfName, 0, 0);
            this.tblInformant.Controls.Add(this.txtInfName, 1, 0);
            this.tblInformant.Controls.Add(this.lblInfRel, 2, 0);
            this.tblInformant.Controls.Add(this.txtInfRel, 3, 0);
            this.tblInformant.Controls.Add(this.lblInfDate, 0, 1);
            this.tblInformant.Controls.Add(this.dtpInfDate, 1, 1);
            this.tblInformant.Controls.Add(this.lblInfRelOther, 2, 1);
            this.tblInformant.Controls.Add(this.txtInfRelOther, 3, 1);
            this.tblInformant.Controls.Add(this.lblInfAddress, 0, 2);
            this.tblInformant.Controls.Add(this.txtInfAddress, 1, 2);
            this.tblInformant.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblInformant.Location = new System.Drawing.Point(18, 14);
            this.tblInformant.Name = "tblInformant";
            this.tblInformant.RowCount = 3;
            this.tblInformant.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblInformant.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblInformant.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.tblInformant.Size = new System.Drawing.Size(1128, 152);
            this.tblInformant.TabIndex = 0;
            // 
            // lblInfName
            // 
            this.lblInfName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInfName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblInfName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblInfName.Location = new System.Drawing.Point(3, 0);
            this.lblInfName.Name = "lblInfName";
            this.lblInfName.Size = new System.Drawing.Size(172, 44);
            this.lblInfName.TabIndex = 0;
            this.lblInfName.Text = "Name";
            this.lblInfName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtInfName
            // 
            this.txtInfName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInfName.Location = new System.Drawing.Point(181, 9);
            this.txtInfName.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtInfName.Name = "txtInfName";
            this.txtInfName.Size = new System.Drawing.Size(359, 25);
            this.txtInfName.TabIndex = 1;
            // 
            // lblInfRel
            // 
            this.lblInfRel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInfRel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblInfRel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblInfRel.Location = new System.Drawing.Point(567, 0);
            this.lblInfRel.Name = "lblInfRel";
            this.lblInfRel.Size = new System.Drawing.Size(172, 44);
            this.lblInfRel.TabIndex = 2;
            this.lblInfRel.Text = "Relationship to child";
            this.lblInfRel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtInfRel
            // 
            this.txtInfRel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInfRel.Location = new System.Drawing.Point(745, 9);
            this.txtInfRel.Name = "txtInfRel";
            this.txtInfRel.Size = new System.Drawing.Size(380, 25);
            this.txtInfRel.TabIndex = 3;
            // 
            // lblInfDate
            // 
            this.lblInfDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInfDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblInfDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblInfDate.Location = new System.Drawing.Point(3, 44);
            this.lblInfDate.Name = "lblInfDate";
            this.lblInfDate.Size = new System.Drawing.Size(172, 44);
            this.lblInfDate.TabIndex = 4;
            this.lblInfDate.Text = "Date";
            this.lblInfDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dtpInfDate
            // 
            this.dtpInfDate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpInfDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpInfDate.Location = new System.Drawing.Point(181, 53);
            this.dtpInfDate.Name = "dtpInfDate";
            this.dtpInfDate.Size = new System.Drawing.Size(200, 25);
            this.dtpInfDate.TabIndex = 5;
            // 
            // lblInfRelOther
            // 
            this.lblInfRelOther.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInfRelOther.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblInfRelOther.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblInfRelOther.Location = new System.Drawing.Point(567, 44);
            this.lblInfRelOther.Name = "lblInfRelOther";
            this.lblInfRelOther.Size = new System.Drawing.Size(172, 44);
            this.lblInfRelOther.TabIndex = 6;
            this.lblInfRelOther.Text = "If Others, specify";
            this.lblInfRelOther.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblInfRelOther.Visible = false;
            // 
            // txtInfRelOther
            // 
            this.txtInfRelOther.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInfRelOther.Location = new System.Drawing.Point(745, 53);
            this.txtInfRelOther.Name = "txtInfRelOther";
            this.txtInfRelOther.Size = new System.Drawing.Size(380, 25);
            this.txtInfRelOther.TabIndex = 7;
            this.txtInfRelOther.Visible = false;
            // 
            // lblInfAddress
            // 
            this.lblInfAddress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInfAddress.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblInfAddress.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblInfAddress.Location = new System.Drawing.Point(3, 88);
            this.lblInfAddress.Name = "lblInfAddress";
            this.lblInfAddress.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblInfAddress.Size = new System.Drawing.Size(172, 64);
            this.lblInfAddress.TabIndex = 6;
            this.lblInfAddress.Text = "Address";
            // 
            // txtInfAddress
            // 
            this.txtInfAddress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tblInformant.SetColumnSpan(this.txtInfAddress, 3);
            this.txtInfAddress.Location = new System.Drawing.Point(181, 93);
            this.txtInfAddress.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.txtInfAddress.Name = "txtInfAddress";
            this.txtInfAddress.Size = new System.Drawing.Size(944, 25);
            this.txtInfAddress.TabIndex = 7;
            // 
            // tabCert
            // 
            this.tabCert.AutoScroll = true;
            this.tabCert.BackColor = System.Drawing.Color.White;
            this.tabCert.Controls.Add(this.tblCert);
            this.tabCert.Location = new System.Drawing.Point(4, 30);
            this.tabCert.Name = "tabCert";
            this.tabCert.Padding = new System.Windows.Forms.Padding(18, 14, 18, 14);
            this.tabCert.Size = new System.Drawing.Size(1164, 370);
            this.tabCert.TabIndex = 6;
            this.tabCert.Text = "Certification";
            // 
            // tblCert
            // 
            this.tblCert.ColumnCount = 4;
            this.tblCert.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblCert.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblCert.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblCert.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblCert.Controls.Add(this.lblRegNo, 0, 0);
            this.tblCert.Controls.Add(this.txtRegNo, 1, 0);
            this.tblCert.Controls.Add(this.lblBook, 2, 0);
            this.tblCert.Controls.Add(this.txtBook, 3, 0);
            this.tblCert.Controls.Add(this.lblStatus, 0, 1);
            this.tblCert.Controls.Add(this.cboStatus, 1, 1);
            this.tblCert.Controls.Add(this.lblPreparedBy, 0, 2);
            this.tblCert.Controls.Add(this.txtPreparedBy, 1, 2);
            this.tblCert.Controls.Add(this.lblReceivedBy, 2, 2);
            this.tblCert.Controls.Add(this.txtReceivedBy, 3, 2);
            this.tblCert.Controls.Add(this.lblPreparedTitle, 0, 3);
            this.tblCert.Controls.Add(this.txtPreparedTitle, 1, 3);
            this.tblCert.Controls.Add(this.lblReceivedTitle, 2, 3);
            this.tblCert.Controls.Add(this.txtReceivedTitle, 3, 3);
            this.tblCert.Controls.Add(this.lblPreparedDate, 0, 4);
            this.tblCert.Controls.Add(this.dtpPreparedDate, 1, 4);
            this.tblCert.Controls.Add(this.lblReceivedDate, 2, 4);
            this.tblCert.Controls.Add(this.dtpReceivedDate, 3, 4);
            this.tblCert.Controls.Add(this.lblRegisteredBy, 0, 5);
            this.tblCert.Controls.Add(this.txtRegisteredBy, 1, 5);
            this.tblCert.Controls.Add(this.lblRegisteredTitle, 2, 5);
            this.tblCert.Controls.Add(this.txtRegisteredTitle, 3, 5);
            this.tblCert.Controls.Add(this.lblRegisteredDate, 0, 6);
            this.tblCert.Controls.Add(this.dtpRegisteredDate, 1, 6);
            this.tblCert.Controls.Add(this.lblRemarks, 0, 7);
            this.tblCert.Controls.Add(this.txtRemarks, 1, 7);
            this.tblCert.Controls.Add(this.lblBookPage, 0, 8);
            this.tblCert.Controls.Add(this.txtBookPage, 1, 8);
            this.tblCert.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblCert.Location = new System.Drawing.Point(18, 14);
            this.tblCert.Name = "tblCert";
            this.tblCert.RowCount = 9;
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCert.Size = new System.Drawing.Size(1111, 448);
            this.tblCert.TabIndex = 0;
            // 
            // lblRegNo
            // 
            this.lblRegNo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRegNo.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRegNo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblRegNo.Location = new System.Drawing.Point(3, 0);
            this.lblRegNo.Name = "lblRegNo";
            this.lblRegNo.Size = new System.Drawing.Size(172, 44);
            this.lblRegNo.TabIndex = 0;
            this.lblRegNo.Text = "Registry Number";
            this.lblRegNo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtRegNo
            // 
            this.txtRegNo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRegNo.Location = new System.Drawing.Point(181, 9);
            this.txtRegNo.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtRegNo.Name = "txtRegNo";
            this.txtRegNo.Size = new System.Drawing.Size(350, 25);
            this.txtRegNo.TabIndex = 1;
            // 
            // lblBook
            // 
            this.lblBook.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBook.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBook.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBook.Location = new System.Drawing.Point(558, 0);
            this.lblBook.Name = "lblBook";
            this.lblBook.Size = new System.Drawing.Size(172, 44);
            this.lblBook.TabIndex = 2;
            this.lblBook.Text = "Book / Volume";
            this.lblBook.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtBook
            // 
            this.txtBook.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBook.Location = new System.Drawing.Point(736, 9);
            this.txtBook.Name = "txtBook";
            this.txtBook.Size = new System.Drawing.Size(372, 25);
            this.txtBook.TabIndex = 3;
            //
            // lblBookPage
            //
            this.lblBookPage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBookPage.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBookPage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBookPage.Location = new System.Drawing.Point(3, 404);
            this.lblBookPage.Name = "lblBookPage";
            this.lblBookPage.Size = new System.Drawing.Size(172, 44);
            this.lblBookPage.TabIndex = 12;
            this.lblBookPage.Text = "Book Page";
            this.lblBookPage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtBookPage
            //
            this.txtBookPage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBookPage.Location = new System.Drawing.Point(181, 413);
            this.txtBookPage.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtBookPage.Name = "txtBookPage";
            this.txtBookPage.Size = new System.Drawing.Size(350, 25);
            this.txtBookPage.TabIndex = 13;
            //
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblStatus.Location = new System.Drawing.Point(3, 44);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(172, 44);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "Status";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboStatus
            // 
            this.cboStatus.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cboStatus.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cboStatus.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cboStatus.Items.AddRange(new object[] {
            "Draft",
            "Pending Approval",
            "Registered",
            "Delayed Posting"});
            this.cboStatus.Location = new System.Drawing.Point(181, 53);
            this.cboStatus.Name = "cboStatus";
            this.cboStatus.Size = new System.Drawing.Size(240, 25);
            this.cboStatus.TabIndex = 5;
            // 
            // lblPreparedBy
            // 
            this.lblPreparedBy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPreparedBy.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPreparedBy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblPreparedBy.Location = new System.Drawing.Point(3, 88);
            this.lblPreparedBy.Name = "lblPreparedBy";
            this.lblPreparedBy.Size = new System.Drawing.Size(172, 44);
            this.lblPreparedBy.TabIndex = 6;
            this.lblPreparedBy.Text = "Prepared By";
            this.lblPreparedBy.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtPreparedBy
            // 
            this.txtPreparedBy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPreparedBy.Location = new System.Drawing.Point(181, 97);
            this.txtPreparedBy.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtPreparedBy.Name = "txtPreparedBy";
            this.txtPreparedBy.Size = new System.Drawing.Size(350, 25);
            this.txtPreparedBy.TabIndex = 7;
            // 
            // lblReceivedBy
            // 
            this.lblReceivedBy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblReceivedBy.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblReceivedBy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblReceivedBy.Location = new System.Drawing.Point(558, 88);
            this.lblReceivedBy.Name = "lblReceivedBy";
            this.lblReceivedBy.Size = new System.Drawing.Size(172, 44);
            this.lblReceivedBy.TabIndex = 8;
            this.lblReceivedBy.Text = "Received By";
            this.lblReceivedBy.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtReceivedBy
            // 
            this.txtReceivedBy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtReceivedBy.Location = new System.Drawing.Point(736, 97);
            this.txtReceivedBy.Name = "txtReceivedBy";
            this.txtReceivedBy.Size = new System.Drawing.Size(372, 25);
            this.txtReceivedBy.TabIndex = 9;
            // 
            // lblPreparedTitle
            // 
            this.lblPreparedTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPreparedTitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPreparedTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblPreparedTitle.Location = new System.Drawing.Point(3, 132);
            this.lblPreparedTitle.Name = "lblPreparedTitle";
            this.lblPreparedTitle.Size = new System.Drawing.Size(172, 44);
            this.lblPreparedTitle.TabIndex = 22;
            this.lblPreparedTitle.Text = "Prepared By - Title";
            this.lblPreparedTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtPreparedTitle
            // 
            this.txtPreparedTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPreparedTitle.Location = new System.Drawing.Point(181, 141);
            this.txtPreparedTitle.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtPreparedTitle.Name = "txtPreparedTitle";
            this.txtPreparedTitle.Size = new System.Drawing.Size(350, 25);
            this.txtPreparedTitle.TabIndex = 23;
            // 
            // lblReceivedTitle
            // 
            this.lblReceivedTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblReceivedTitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblReceivedTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblReceivedTitle.Location = new System.Drawing.Point(558, 132);
            this.lblReceivedTitle.Name = "lblReceivedTitle";
            this.lblReceivedTitle.Size = new System.Drawing.Size(172, 44);
            this.lblReceivedTitle.TabIndex = 24;
            this.lblReceivedTitle.Text = "Received By - Title";
            this.lblReceivedTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtReceivedTitle
            // 
            this.txtReceivedTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtReceivedTitle.Location = new System.Drawing.Point(736, 141);
            this.txtReceivedTitle.Name = "txtReceivedTitle";
            this.txtReceivedTitle.Size = new System.Drawing.Size(372, 25);
            this.txtReceivedTitle.TabIndex = 25;
            // 
            // lblPreparedDate
            // 
            this.lblPreparedDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPreparedDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPreparedDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblPreparedDate.Location = new System.Drawing.Point(3, 176);
            this.lblPreparedDate.Name = "lblPreparedDate";
            this.lblPreparedDate.Size = new System.Drawing.Size(172, 44);
            this.lblPreparedDate.TabIndex = 26;
            this.lblPreparedDate.Text = "Prepared By - Date";
            this.lblPreparedDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dtpPreparedDate
            // 
            this.dtpPreparedDate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpPreparedDate.Checked = false;
            this.dtpPreparedDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpPreparedDate.Location = new System.Drawing.Point(181, 185);
            this.dtpPreparedDate.Name = "dtpPreparedDate";
            this.dtpPreparedDate.ShowCheckBox = true;
            this.dtpPreparedDate.Size = new System.Drawing.Size(200, 25);
            this.dtpPreparedDate.TabIndex = 27;
            // 
            // lblReceivedDate
            // 
            this.lblReceivedDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblReceivedDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblReceivedDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblReceivedDate.Location = new System.Drawing.Point(558, 176);
            this.lblReceivedDate.Name = "lblReceivedDate";
            this.lblReceivedDate.Size = new System.Drawing.Size(172, 44);
            this.lblReceivedDate.TabIndex = 28;
            this.lblReceivedDate.Text = "Received By - Date";
            this.lblReceivedDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dtpReceivedDate
            // 
            this.dtpReceivedDate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpReceivedDate.Checked = false;
            this.dtpReceivedDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpReceivedDate.Location = new System.Drawing.Point(736, 185);
            this.dtpReceivedDate.Name = "dtpReceivedDate";
            this.dtpReceivedDate.ShowCheckBox = true;
            this.dtpReceivedDate.Size = new System.Drawing.Size(200, 25);
            this.dtpReceivedDate.TabIndex = 29;
            // 
            // lblRegisteredBy
            // 
            this.lblRegisteredBy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRegisteredBy.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRegisteredBy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblRegisteredBy.Location = new System.Drawing.Point(3, 220);
            this.lblRegisteredBy.Name = "lblRegisteredBy";
            this.lblRegisteredBy.Size = new System.Drawing.Size(172, 44);
            this.lblRegisteredBy.TabIndex = 30;
            this.lblRegisteredBy.Text = "Registered By";
            this.lblRegisteredBy.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtRegisteredBy
            // 
            this.txtRegisteredBy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRegisteredBy.Location = new System.Drawing.Point(181, 229);
            this.txtRegisteredBy.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.txtRegisteredBy.Name = "txtRegisteredBy";
            this.txtRegisteredBy.Size = new System.Drawing.Size(350, 25);
            this.txtRegisteredBy.TabIndex = 31;
            // 
            // lblRegisteredTitle
            // 
            this.lblRegisteredTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRegisteredTitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRegisteredTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblRegisteredTitle.Location = new System.Drawing.Point(558, 220);
            this.lblRegisteredTitle.Name = "lblRegisteredTitle";
            this.lblRegisteredTitle.Size = new System.Drawing.Size(172, 44);
            this.lblRegisteredTitle.TabIndex = 32;
            this.lblRegisteredTitle.Text = "Registered By - Title";
            this.lblRegisteredTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtRegisteredTitle
            // 
            this.txtRegisteredTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRegisteredTitle.Location = new System.Drawing.Point(736, 229);
            this.txtRegisteredTitle.Name = "txtRegisteredTitle";
            this.txtRegisteredTitle.Size = new System.Drawing.Size(372, 25);
            this.txtRegisteredTitle.TabIndex = 33;
            // 
            // lblRegisteredDate
            // 
            this.lblRegisteredDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRegisteredDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRegisteredDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblRegisteredDate.Location = new System.Drawing.Point(3, 264);
            this.lblRegisteredDate.Name = "lblRegisteredDate";
            this.lblRegisteredDate.Size = new System.Drawing.Size(172, 44);
            this.lblRegisteredDate.TabIndex = 34;
            this.lblRegisteredDate.Text = "Registered By - Date";
            this.lblRegisteredDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dtpRegisteredDate
            // 
            this.dtpRegisteredDate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpRegisteredDate.Checked = false;
            this.dtpRegisteredDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpRegisteredDate.Location = new System.Drawing.Point(181, 273);
            this.dtpRegisteredDate.Name = "dtpRegisteredDate";
            this.dtpRegisteredDate.ShowCheckBox = true;
            this.dtpRegisteredDate.Size = new System.Drawing.Size(200, 25);
            this.dtpRegisteredDate.TabIndex = 35;
            // 
            // lblRemarks
            // 
            this.lblRemarks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRemarks.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRemarks.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblRemarks.Location = new System.Drawing.Point(3, 308);
            this.lblRemarks.Name = "lblRemarks";
            this.lblRemarks.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblRemarks.Size = new System.Drawing.Size(172, 96);
            this.lblRemarks.TabIndex = 10;
            this.lblRemarks.Text = "Remarks";
            // 
            // txtRemarks
            // 
            this.tblCert.SetColumnSpan(this.txtRemarks, 3);
            this.txtRemarks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRemarks.Location = new System.Drawing.Point(181, 313);
            this.txtRemarks.Margin = new System.Windows.Forms.Padding(3, 5, 3, 8);
            this.txtRemarks.Multiline = true;
            this.txtRemarks.Name = "txtRemarks";
            this.txtRemarks.Size = new System.Drawing.Size(927, 83);
            this.txtRemarks.TabIndex = 11;
            // 
            // cardRecords
            // 
            this.cardRecords.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.cardRecords.CardColor = System.Drawing.Color.White;
            this.cardRecords.Controls.Add(this.layoutRecords);
            this.cardRecords.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardRecords.DrawShadow = true;
            this.cardRecords.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.cardRecords.Location = new System.Drawing.Point(20, 556);
            this.cardRecords.Margin = new System.Windows.Forms.Padding(0);
            this.cardRecords.Name = "cardRecords";
            this.cardRecords.Padding = new System.Windows.Forms.Padding(16, 10, 16, 14);
            this.cardRecords.Radius = 10;
            this.cardRecords.Size = new System.Drawing.Size(1200, 326);
            this.cardRecords.TabIndex = 2;
            // 
            // layoutRecords
            // 
            this.layoutRecords.ColumnCount = 1;
            this.layoutRecords.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRecords.Controls.Add(this.pnlRecordsHead, 0, 0);
            this.layoutRecords.Controls.Add(this.pnlSearch, 0, 1);
            this.layoutRecords.Controls.Add(this.dgvBirths, 0, 2);
            this.layoutRecords.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutRecords.Location = new System.Drawing.Point(16, 10);
            this.layoutRecords.Margin = new System.Windows.Forms.Padding(0);
            this.layoutRecords.Name = "layoutRecords";
            this.layoutRecords.RowCount = 3;
            this.layoutRecords.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.layoutRecords.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.layoutRecords.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRecords.Size = new System.Drawing.Size(1168, 302);
            this.layoutRecords.TabIndex = 0;
            // 
            // pnlRecordsHead
            // 
            this.pnlRecordsHead.ColumnCount = 2;
            this.pnlRecordsHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlRecordsHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.pnlRecordsHead.Controls.Add(this.lblRecent, 0, 0);
            this.pnlRecordsHead.Controls.Add(this.pnlRecordActions, 1, 0);
            this.pnlRecordsHead.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRecordsHead.Location = new System.Drawing.Point(0, 0);
            this.pnlRecordsHead.Margin = new System.Windows.Forms.Padding(0);
            this.pnlRecordsHead.Name = "pnlRecordsHead";
            this.pnlRecordsHead.RowCount = 1;
            this.pnlRecordsHead.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlRecordsHead.Size = new System.Drawing.Size(1168, 42);
            this.pnlRecordsHead.TabIndex = 0;
            // 
            // lblRecent
            // 
            this.lblRecent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRecent.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblRecent.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblRecent.Location = new System.Drawing.Point(3, 0);
            this.lblRecent.Name = "lblRecent";
            this.lblRecent.Size = new System.Drawing.Size(608, 42);
            this.lblRecent.TabIndex = 0;
            this.lblRecent.Text = "RECENT BIRTH REGISTRATIONS";
            this.lblRecent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlRecordActions
            // 
            this.pnlRecordActions.AutoSize = true;
            this.pnlRecordActions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlRecordActions.Controls.Add(this.btnDelete);
            this.pnlRecordActions.Controls.Add(this.btnUpdate);
            this.pnlRecordActions.Controls.Add(this.btnNew);
            this.pnlRecordActions.Controls.Add(this.btnCertificate);
            this.pnlRecordActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRecordActions.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlRecordActions.Location = new System.Drawing.Point(614, 0);
            this.pnlRecordActions.Margin = new System.Windows.Forms.Padding(0);
            this.pnlRecordActions.Name = "pnlRecordActions";
            this.pnlRecordActions.Size = new System.Drawing.Size(554, 42);
            this.pnlRecordActions.TabIndex = 1;
            this.pnlRecordActions.WrapContents = false;
            this.pnlRecordActions.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlRecordActions_Paint);
            // 
            // btnDelete
            // 
            this.btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDelete.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnDelete.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(50)))), ((int)(((byte)(63)))));
            this.btnDelete.Location = new System.Drawing.Point(454, 6);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(100, 30);
            this.btnDelete.TabIndex = 2;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnUpdate
            // 
            this.btnUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUpdate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnUpdate.Location = new System.Drawing.Point(348, 6);
            this.btnUpdate.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(100, 30);
            this.btnUpdate.TabIndex = 1;
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            //
            // pnlSearch
            //
            this.pnlSearch.ColumnCount = 2;
            this.pnlSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.pnlSearch.Controls.Add(this.txtSearch, 0, 0);
            this.pnlSearch.Controls.Add(this.btnClearSearch, 1, 0);
            this.pnlSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlSearch.Location = new System.Drawing.Point(0, 42);
            this.pnlSearch.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.pnlSearch.Name = "pnlSearch";
            this.pnlSearch.RowCount = 1;
            this.pnlSearch.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlSearch.Size = new System.Drawing.Size(1168, 28);
            this.pnlSearch.TabIndex = 2;
            //
            // txtSearch
            //
            this.txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtSearch.Location = new System.Drawing.Point(3, 3);
            this.txtSearch.Margin = new System.Windows.Forms.Padding(3, 3, 6, 0);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(1085, 23);
            this.txtSearch.TabIndex = 0;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            //
            // btnClearSearch
            //
            this.btnClearSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearSearch.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnClearSearch.Location = new System.Drawing.Point(1094, 0);
            this.btnClearSearch.Margin = new System.Windows.Forms.Padding(0);
            this.btnClearSearch.Name = "btnClearSearch";
            this.btnClearSearch.Size = new System.Drawing.Size(74, 28);
            this.btnClearSearch.TabIndex = 1;
            this.btnClearSearch.Text = "Clear";
            this.btnClearSearch.UseVisualStyleBackColor = true;
            this.btnClearSearch.Click += new System.EventHandler(this.btnClearSearch_Click);
            //
            // dgvBirths
            //
            this.dgvBirths.AllowUserToAddRows = false;
            this.dgvBirths.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvBirths.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvBirths.BackgroundColor = System.Drawing.Color.White;
            this.dgvBirths.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvBirths.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvBirths.Location = new System.Drawing.Point(0, 76);
            this.dgvBirths.Margin = new System.Windows.Forms.Padding(0);
            this.dgvBirths.Name = "dgvBirths";
            this.dgvBirths.ReadOnly = true;
            this.dgvBirths.RowHeadersVisible = false;
            this.dgvBirths.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvBirths.Size = new System.Drawing.Size(1168, 226);
            this.dgvBirths.TabIndex = 3;
            this.dgvBirths.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvBirths_CellClick);
            // 
            // BirthRegistrationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(900, 800);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(1400, 900);
            this.Controls.Add(this.layoutRoot);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "BirthRegistrationForm";
            this.Text = "Birth Registration";
            this.certificateMenu.ResumeLayout(false);
            this.layoutRoot.ResumeLayout(false);
            this.layoutMain.ResumeLayout(false);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlHeadText.ResumeLayout(false);
            this.pnlHeadText.PerformLayout();
            this.pnlHeadActions.ResumeLayout(false);
            this.cardForm.ResumeLayout(false);
            this.tabControl.ResumeLayout(false);
            this.tabChild.ResumeLayout(false);
            this.tblChild.ResumeLayout(false);
            this.tblChild.PerformLayout();
            this.tabMother.ResumeLayout(false);
            this.tblMother.ResumeLayout(false);
            this.tblMother.PerformLayout();
            this.tabFather.ResumeLayout(false);
            this.tblFather.ResumeLayout(false);
            this.tblFather.PerformLayout();
            this.tabMarriage.ResumeLayout(false);
            this.tblMarriage.ResumeLayout(false);
            this.tblMarriage.PerformLayout();
            this.pnlParentsMarried.ResumeLayout(false);
            this.tabAttendant.ResumeLayout(false);
            this.tblAttendant.ResumeLayout(false);
            this.tblAttendant.PerformLayout();
            this.tabInformant.ResumeLayout(false);
            this.tblInformant.ResumeLayout(false);
            this.tblInformant.PerformLayout();
            this.tabCert.ResumeLayout(false);
            this.tblCert.ResumeLayout(false);
            this.tblCert.PerformLayout();
            this.cardRecords.ResumeLayout(false);
            this.layoutRecords.ResumeLayout(false);
            this.pnlRecordsHead.ResumeLayout(false);
            this.pnlRecordsHead.PerformLayout();
            this.pnlRecordActions.ResumeLayout(false);
            this.pnlSearch.ResumeLayout(false);
            this.pnlSearch.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBirths)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private SplitButton btnCertificate;
        private ContextMenuStrip certificateMenu;
        private ToolStripMenuItem mnuViewSoftcopy;
        private ToolStripMenuItem mnuFactsCert;

        private System.Windows.Forms.TableLayoutPanel layoutRoot;
        private System.Windows.Forms.TableLayoutPanel layoutMain;
        private System.Windows.Forms.TableLayoutPanel pnlHeader;
        private System.Windows.Forms.Panel pnlHeadText;
        private System.Windows.Forms.FlowLayoutPanel pnlHeadActions;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.CheckBox chkDelayed;
        private System.Windows.Forms.Button btnSaveDraft;
        private System.Windows.Forms.Button btnSubmit;
        private CROMS.Modules.CardPanel cardForm;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabChild;
        private System.Windows.Forms.TableLayoutPanel tblChild;
        private System.Windows.Forms.Label lblFirstName;
        private System.Windows.Forms.TextBox txtFirstName;
        private System.Windows.Forms.Label lblMiddleName;
        private System.Windows.Forms.TextBox txtMiddleName;
        private System.Windows.Forms.Label lblLastName;
        private System.Windows.Forms.TextBox txtLastName;
        private System.Windows.Forms.Label lblSex;
        private System.Windows.Forms.ComboBox cboSex;
        private System.Windows.Forms.Label lblDob;
        private System.Windows.Forms.DateTimePicker dtpDob;
        private System.Windows.Forms.Label lblTypeOfBirth;
        private System.Windows.Forms.ComboBox cboTypeOfBirth;
        private System.Windows.Forms.Label lblBirthOrder;
        private System.Windows.Forms.TextBox txtBirthOrder;
        private System.Windows.Forms.Label lblWeight;
        private System.Windows.Forms.TextBox txtWeight;
        private System.Windows.Forms.Label lblPlace;
        private System.Windows.Forms.TextBox txtPlace;
        private System.Windows.Forms.TabPage tabMother;
        private System.Windows.Forms.TableLayoutPanel tblMother;
        private System.Windows.Forms.Label lblMFirst;
        private System.Windows.Forms.TextBox txtMFirst;
        private System.Windows.Forms.Label lblMMiddle;
        private System.Windows.Forms.TextBox txtMMiddle;
        private System.Windows.Forms.Label lblMLast;
        private System.Windows.Forms.TextBox txtMLast;
        private System.Windows.Forms.Label lblMCitizen;
        private System.Windows.Forms.TextBox txtMCitizen;
        private System.Windows.Forms.Label lblMReligion;
        private System.Windows.Forms.TextBox txtMReligion;
        private System.Windows.Forms.Label lblMOccupation;
        private System.Windows.Forms.TextBox txtMOccupation;
        private System.Windows.Forms.Label lblMAge;
        private System.Windows.Forms.TextBox txtMAge;
        private System.Windows.Forms.Label lblMBornAlive;
        private System.Windows.Forms.TextBox txtMBornAlive;
        private System.Windows.Forms.Label lblMLiving;
        private System.Windows.Forms.TextBox txtMLiving;
        private System.Windows.Forms.Label lblMDead;
        private System.Windows.Forms.TextBox txtMDead;
        private System.Windows.Forms.Label lblMResidence;
        private System.Windows.Forms.TextBox txtMResidence;
        private System.Windows.Forms.TabPage tabFather;
        private System.Windows.Forms.TableLayoutPanel tblFather;
        private System.Windows.Forms.Label lblFFirst;
        private System.Windows.Forms.TextBox txtFFirst;
        private System.Windows.Forms.Label lblFMiddle;
        private System.Windows.Forms.TextBox txtFMiddle;
        private System.Windows.Forms.Label lblFLast;
        private System.Windows.Forms.TextBox txtFLast;
        private System.Windows.Forms.Label lblFCitizen;
        private System.Windows.Forms.TextBox txtFCitizen;
        private System.Windows.Forms.Label lblFReligion;
        private System.Windows.Forms.TextBox txtFReligion;
        private System.Windows.Forms.Label lblFOccupation;
        private System.Windows.Forms.TextBox txtFOccupation;
        private System.Windows.Forms.Label lblFAge;
        private System.Windows.Forms.TextBox txtFAge;
        private System.Windows.Forms.Label lblFResidence;
        private System.Windows.Forms.TextBox txtFResidence;
        private System.Windows.Forms.TabPage tabMarriage;
        private System.Windows.Forms.TableLayoutPanel tblMarriage;
        private System.Windows.Forms.Label lblMarrDate;
        private System.Windows.Forms.DateTimePicker dtpMarrDate;
        private System.Windows.Forms.Label lblMarrPlace;
        private System.Windows.Forms.TextBox txtMarrPlace;
        private System.Windows.Forms.Label lblAttTypeOther;
        private System.Windows.Forms.TextBox txtAttTypeOther;
        private System.Windows.Forms.Label lblInfRelOther;
        private System.Windows.Forms.TextBox txtInfRelOther;
        private System.Windows.Forms.Label lblParentsMarried;
        private System.Windows.Forms.Panel pnlParentsMarried;
        private CROMS.Modules.ToggleSwitch tglParentsMarried;
        private System.Windows.Forms.Label lblParentsMarriedState;
        private System.Windows.Forms.TabPage tabAttendant;
        private System.Windows.Forms.TableLayoutPanel tblAttendant;
        private System.Windows.Forms.Label lblAttType;
        private System.Windows.Forms.ComboBox cboAttType;
        private System.Windows.Forms.Label lblAttName;
        private System.Windows.Forms.TextBox txtAttName;
        private System.Windows.Forms.Label lblAttTitle;
        private System.Windows.Forms.TextBox txtAttTitle;
        private System.Windows.Forms.Label lblAttAddress;
        private System.Windows.Forms.TextBox txtAttAddress;
        private System.Windows.Forms.TabPage tabInformant;
        private System.Windows.Forms.TableLayoutPanel tblInformant;
        private System.Windows.Forms.Label lblInfName;
        private System.Windows.Forms.TextBox txtInfName;
        private System.Windows.Forms.Label lblInfRel;
        private System.Windows.Forms.TextBox txtInfRel;
        private System.Windows.Forms.Label lblInfDate;
        private System.Windows.Forms.DateTimePicker dtpInfDate;
        private System.Windows.Forms.Label lblInfAddress;
        private System.Windows.Forms.TextBox txtInfAddress;
        private System.Windows.Forms.TabPage tabCert;
        private System.Windows.Forms.TableLayoutPanel tblCert;
        private System.Windows.Forms.Label lblRegNo;
        private System.Windows.Forms.TextBox txtRegNo;
        private System.Windows.Forms.Label lblBook;
        private System.Windows.Forms.TextBox txtBook;
        private System.Windows.Forms.Label lblBookPage;
        private System.Windows.Forms.TextBox txtBookPage;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ComboBox cboStatus;
        private System.Windows.Forms.Label lblPreparedBy;
        private System.Windows.Forms.TextBox txtPreparedBy;
        private System.Windows.Forms.Label lblReceivedBy;
        private System.Windows.Forms.TextBox txtReceivedBy;
        private System.Windows.Forms.Label lblAttDate;
        private System.Windows.Forms.DateTimePicker dtpAttDate;
        private System.Windows.Forms.Label lblPreparedTitle;
        private System.Windows.Forms.TextBox txtPreparedTitle;
        private System.Windows.Forms.Label lblReceivedTitle;
        private System.Windows.Forms.TextBox txtReceivedTitle;
        private System.Windows.Forms.Label lblPreparedDate;
        private System.Windows.Forms.DateTimePicker dtpPreparedDate;
        private System.Windows.Forms.Label lblReceivedDate;
        private System.Windows.Forms.DateTimePicker dtpReceivedDate;
        private System.Windows.Forms.Label lblRegisteredBy;
        private System.Windows.Forms.TextBox txtRegisteredBy;
        private System.Windows.Forms.Label lblRegisteredTitle;
        private System.Windows.Forms.TextBox txtRegisteredTitle;
        private System.Windows.Forms.Label lblRegisteredDate;
        private System.Windows.Forms.DateTimePicker dtpRegisteredDate;
        private System.Windows.Forms.Label lblRemarks;
        private System.Windows.Forms.TextBox txtRemarks;
        private CROMS.Modules.CardPanel cardRecords;
        private System.Windows.Forms.TableLayoutPanel layoutRecords;
        private System.Windows.Forms.TableLayoutPanel pnlRecordsHead;
        private System.Windows.Forms.Label lblRecent;
        private System.Windows.Forms.FlowLayoutPanel pnlRecordActions;
        private System.Windows.Forms.Button btnNew;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.DataGridView dgvBirths;
        private System.Windows.Forms.Button btnOCRLiveBirth;
        private System.Windows.Forms.TableLayoutPanel pnlSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Button btnClearSearch;
    }
}