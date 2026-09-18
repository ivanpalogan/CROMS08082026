// DeathRegistrationForm.Designer.cs - UI controls and InitializeComponent only.
// Layout mirrors BirthRegistrationForm's card/header/tab shell (CardPanel, header bar
// with title/subtitle + action buttons, a TabControl of 4-column field grids, and a
// records card with search) so the two registration screens read as one system.

using System;
using System.Windows.Forms;
using CROMS.Modules;

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
            this.components = new System.ComponentModel.Container();
            this.certificateMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuViewSoftcopy = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFactsCert = new System.Windows.Forms.ToolStripMenuItem();
            this.btnCertificate = new CROMS.Modules.SplitButton();
            this.btnSave = new System.Windows.Forms.Button();
            this.layoutRoot = new System.Windows.Forms.TableLayoutPanel();
            this.layoutMain = new System.Windows.Forms.TableLayoutPanel();
            this.pnlHeader = new System.Windows.Forms.TableLayoutPanel();
            this.pnlHeadText = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.pnlHeadActions = new System.Windows.Forms.FlowLayoutPanel();
            this.cardForm = new CROMS.Modules.CardPanel();
            this.tabControl = new System.Windows.Forms.TabControl();

            this.tabDeceased = new System.Windows.Forms.TabPage();
            this.tblDeceased = new System.Windows.Forms.TableLayoutPanel();
            this.lblLastName = new System.Windows.Forms.Label();
            this.txtLastName = new System.Windows.Forms.TextBox();
            this.lblFirstName = new System.Windows.Forms.Label();
            this.txtFirstName = new System.Windows.Forms.TextBox();
            this.lblMiddleName = new System.Windows.Forms.Label();
            this.txtMiddleName = new System.Windows.Forms.TextBox();
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

            this.tabCause = new System.Windows.Forms.TabPage();
            this.tblCause = new System.Windows.Forms.TableLayoutPanel();
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

            this.tabInformant = new System.Windows.Forms.TabPage();
            this.tblInformant = new System.Windows.Forms.TableLayoutPanel();
            this.lblCInfName = new System.Windows.Forms.Label();
            this.txtCInfName = new System.Windows.Forms.TextBox();
            this.lblCInfRel = new System.Windows.Forms.Label();
            this.txtCInfRel = new System.Windows.Forms.TextBox();
            this.lblCInfDate = new System.Windows.Forms.Label();
            this.dtpCInfDate = new System.Windows.Forms.DateTimePicker();
            this.lblCInfRelOther = new System.Windows.Forms.Label();
            this.txtCInfRelOther = new System.Windows.Forms.TextBox();
            this.lblCInfAddr = new System.Windows.Forms.Label();
            this.txtCInfAddr = new System.Windows.Forms.TextBox();

            this.tabCertification = new System.Windows.Forms.TabPage();
            this.tblCertification = new System.Windows.Forms.TableLayoutPanel();
            this.lblBookVol = new System.Windows.Forms.Label();
            this.txtBookVol = new System.Windows.Forms.TextBox();
            this.lblBookPage = new System.Windows.Forms.Label();
            this.txtBookPage = new System.Windows.Forms.TextBox();
            this.lblCPrepBy = new System.Windows.Forms.Label();
            this.txtCPrepBy = new System.Windows.Forms.TextBox();
            this.lblCRecvBy = new System.Windows.Forms.Label();
            this.txtCRecvBy = new System.Windows.Forms.TextBox();
            this.lblCPrepTitle = new System.Windows.Forms.Label();
            this.txtCPrepTitle = new System.Windows.Forms.TextBox();
            this.lblCRecvTitle = new System.Windows.Forms.Label();
            this.txtCRecvTitle = new System.Windows.Forms.TextBox();
            this.lblCPrepDate = new System.Windows.Forms.Label();
            this.dtpCPrepDate = new System.Windows.Forms.DateTimePicker();
            this.lblCRecvDate = new System.Windows.Forms.Label();
            this.dtpCRecvDate = new System.Windows.Forms.DateTimePicker();
            this.lblCRegBy = new System.Windows.Forms.Label();
            this.txtCRegBy = new System.Windows.Forms.TextBox();
            this.lblCRegTitle = new System.Windows.Forms.Label();
            this.txtCRegTitle = new System.Windows.Forms.TextBox();
            this.lblCRegDate = new System.Windows.Forms.Label();
            this.dtpCRegDate = new System.Windows.Forms.DateTimePicker();

            this.pnlRecordActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnNew = new System.Windows.Forms.Button();

            this.cardRecords = new CROMS.Modules.CardPanel();
            this.layoutRecords = new System.Windows.Forms.TableLayoutPanel();
            this.pnlRecordsHead = new System.Windows.Forms.TableLayoutPanel();
            this.lblRecent = new System.Windows.Forms.Label();
            this.pnlListActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnNewRegistration = new System.Windows.Forms.Button();
            this.pnlSearch = new System.Windows.Forms.TableLayoutPanel();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnClearSearch = new System.Windows.Forms.Button();
            this.dgvDeaths = new System.Windows.Forms.DataGridView();

            this.certificateMenu.SuspendLayout();
            this.layoutRoot.SuspendLayout();
            this.layoutMain.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            this.pnlHeadText.SuspendLayout();
            this.pnlHeadActions.SuspendLayout();
            this.cardForm.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabDeceased.SuspendLayout();
            this.tblDeceased.SuspendLayout();
            this.tabCause.SuspendLayout();
            this.tblCause.SuspendLayout();
            this.tabInformant.SuspendLayout();
            this.tblInformant.SuspendLayout();
            this.tabCertification.SuspendLayout();
            this.tblCertification.SuspendLayout();
            this.pnlRecordActions.SuspendLayout();
            this.cardRecords.SuspendLayout();
            this.layoutRecords.SuspendLayout();
            this.pnlRecordsHead.SuspendLayout();
            this.pnlListActions.SuspendLayout();
            this.pnlSearch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDeaths)).BeginInit();
            this.SuspendLayout();
            //
            // certificateMenu
            //
            this.certificateMenu.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.certificateMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuViewSoftcopy,
            this.mnuFactsCert});
            this.certificateMenu.Name = "certificateMenu";
            this.certificateMenu.Size = new System.Drawing.Size(240, 48);
            this.certificateMenu.Opening += new System.ComponentModel.CancelEventHandler(this.certificateMenu_Opening);
            //
            // mnuViewSoftcopy
            //
            this.mnuViewSoftcopy.Name = "mnuViewSoftcopy";
            this.mnuViewSoftcopy.Size = new System.Drawing.Size(239, 22);
            this.mnuViewSoftcopy.Text = "View Softcopy";
            this.mnuViewSoftcopy.Click += new System.EventHandler(this.btnViewScan_Click);
            //
            // mnuFactsCert
            //
            this.mnuFactsCert.Name = "mnuFactsCert";
            this.mnuFactsCert.Size = new System.Drawing.Size(239, 22);
            this.mnuFactsCert.Text = "Facts Certification (Form 2A)...";
            this.mnuFactsCert.Click += new System.EventHandler(this.mnuFactsCert_Click);
            //
            // btnCertificate
            //
            this.btnCertificate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(241)))), ((int)(((byte)(246)))));
            this.btnCertificate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCertificate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCertificate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCertificate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.btnCertificate.Location = new System.Drawing.Point(220, 4);
            this.btnCertificate.Margin = new System.Windows.Forms.Padding(0);
            this.btnCertificate.Menu = this.certificateMenu;
            this.btnCertificate.Name = "btnCertificate";
            this.btnCertificate.Size = new System.Drawing.Size(240, 40);
            this.btnCertificate.TabIndex = 0;
            this.btnCertificate.Tag = "noskin";
            this.btnCertificate.Text = "Print COD + Burial Permit";
            this.btnCertificate.UseVisualStyleBackColor = false;
            this.btnCertificate.Click += new System.EventHandler(this.btnPrint_Click);
            //
            // btnSave
            //
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(468, 4);
            this.btnSave.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(160, 40);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Register Death";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // layoutRoot
            //
            this.layoutRoot.ColumnCount = 3;
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 0F));
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 0F));
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
            this.layoutMain.Location = new System.Drawing.Point(0, 0);
            this.layoutMain.Margin = new System.Windows.Forms.Padding(0);
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.Padding = new System.Windows.Forms.Padding(20, 16, 20, 18);
            this.layoutMain.RowCount = 3;
            this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 444F));
            this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutMain.Size = new System.Drawing.Size(1400, 900);
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
            this.pnlHeader.Size = new System.Drawing.Size(1360, 80);
            this.pnlHeader.TabIndex = 0;
            //
            // pnlHeadText
            //
            this.pnlHeadText.Controls.Add(this.lblTitle);
            this.pnlHeadText.Controls.Add(this.lblSubtitle);
            this.pnlHeadText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlHeadText.Location = new System.Drawing.Point(0, 0);
            this.pnlHeadText.Margin = new System.Windows.Forms.Padding(0);
            this.pnlHeadText.Name = "pnlHeadText";
            this.pnlHeadText.Size = new System.Drawing.Size(732, 80);
            this.pnlHeadText.TabIndex = 0;
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 19F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(240, 36);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Death Registration";
            //
            // lblSubtitle
            //
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblSubtitle.Location = new System.Drawing.Point(2, 40);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(300, 15);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "MUNICIPAL FORM 103  •  CERTIFICATE OF DEATH";
            this.lblSubtitle.UseMnemonic = false;
            //
            // pnlHeadActions
            //
            this.pnlHeadActions.AutoSize = true;
            this.pnlHeadActions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlHeadActions.Controls.Add(this.btnSave);
            this.pnlHeadActions.Controls.Add(this.btnCertificate);
            this.pnlHeadActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlHeadActions.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlHeadActions.Location = new System.Drawing.Point(732, 0);
            this.pnlHeadActions.Margin = new System.Windows.Forms.Padding(0);
            this.pnlHeadActions.Name = "pnlHeadActions";
            this.pnlHeadActions.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.pnlHeadActions.Size = new System.Drawing.Size(628, 80);
            this.pnlHeadActions.TabIndex = 1;
            this.pnlHeadActions.WrapContents = false;
            //
            // cardForm
            //
            this.cardForm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.cardForm.CardColor = System.Drawing.Color.White;
            this.cardForm.Controls.Add(this.tabControl);
            this.cardForm.Controls.Add(this.pnlRecordActions);
            this.cardForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardForm.DrawShadow = true;
            this.cardForm.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.cardForm.Location = new System.Drawing.Point(20, 96);
            this.cardForm.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
            this.cardForm.Name = "cardForm";
            this.cardForm.Padding = new System.Windows.Forms.Padding(14, 12, 14, 14);
            this.cardForm.Radius = 10;
            this.cardForm.Size = new System.Drawing.Size(1360, 430);
            this.cardForm.TabIndex = 1;
            //
            // tabControl
            //
            this.tabControl.Controls.Add(this.tabDeceased);
            this.tabControl.Controls.Add(this.tabCause);
            this.tabControl.Controls.Add(this.tabInformant);
            this.tabControl.Controls.Add(this.tabCertification);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.tabControl.Location = new System.Drawing.Point(14, 54);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Drawing.Point(14, 5);
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1332, 362);
            this.tabControl.TabIndex = 0;
            //
            // tabDeceased
            //
            this.tabDeceased.AutoScroll = true;
            this.tabDeceased.BackColor = System.Drawing.Color.White;
            this.tabDeceased.Controls.Add(this.tblDeceased);
            this.tabDeceased.Location = new System.Drawing.Point(4, 30);
            this.tabDeceased.Name = "tabDeceased";
            this.tabDeceased.Padding = new System.Windows.Forms.Padding(18, 14, 18, 14);
            this.tabDeceased.Size = new System.Drawing.Size(1324, 328);
            this.tabDeceased.TabIndex = 0;
            this.tabDeceased.Text = "Deceased";
            //
            // tblDeceased
            //
            this.tblDeceased.ColumnCount = 4;
            this.tblDeceased.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblDeceased.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 380F));
            this.tblDeceased.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblDeceased.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 380F));
            this.tblDeceased.Controls.Add(this.lblLastName, 0, 0);
            this.tblDeceased.Controls.Add(this.txtLastName, 1, 0);
            this.tblDeceased.Controls.Add(this.lblFirstName, 2, 0);
            this.tblDeceased.Controls.Add(this.txtFirstName, 3, 0);
            this.tblDeceased.Controls.Add(this.lblMiddleName, 0, 1);
            this.tblDeceased.Controls.Add(this.txtMiddleName, 1, 1);
            this.tblDeceased.Controls.Add(this.lblSex, 0, 2);
            this.tblDeceased.Controls.Add(this.cboSex, 1, 2);
            this.tblDeceased.Controls.Add(this.lblCivil, 2, 2);
            this.tblDeceased.Controls.Add(this.cboCivil, 3, 2);
            this.tblDeceased.Controls.Add(this.lblAge, 0, 3);
            this.tblDeceased.Controls.Add(this.txtAge, 1, 3);
            this.tblDeceased.Controls.Add(this.lblCitizen, 2, 3);
            this.tblDeceased.Controls.Add(this.txtCitizen, 3, 3);
            this.tblDeceased.Controls.Add(this.lblDod, 0, 4);
            this.tblDeceased.Controls.Add(this.dtpDod, 1, 4);
            this.tblDeceased.Controls.Add(this.lblTod, 2, 4);
            this.tblDeceased.Controls.Add(this.dtpTod, 3, 4);
            this.tblDeceased.Controls.Add(this.lblPlace, 0, 5);
            this.tblDeceased.Controls.Add(this.txtPlace, 1, 5);
            this.tblDeceased.Controls.Add(this.lblReligion, 0, 6);
            this.tblDeceased.Controls.Add(this.txtReligion, 1, 6);
            this.tblDeceased.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblDeceased.Location = new System.Drawing.Point(18, 14);
            this.tblDeceased.Name = "tblDeceased";
            this.tblDeceased.RowCount = 7;
            this.tblDeceased.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblDeceased.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblDeceased.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblDeceased.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblDeceased.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblDeceased.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.tblDeceased.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblDeceased.Size = new System.Drawing.Size(1116, 328);
            this.tblDeceased.TabIndex = 0;
            //
            // lblLastName
            //
            this.lblLastName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLastName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblLastName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblLastName.Location = new System.Drawing.Point(3, 0);
            this.lblLastName.Name = "lblLastName";
            this.lblLastName.Size = new System.Drawing.Size(172, 44);
            this.lblLastName.TabIndex = 0;
            this.lblLastName.Text = "Last Name";
            this.lblLastName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtLastName
            //
            this.txtLastName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtLastName.Location = new System.Drawing.Point(181, 9);
            this.txtLastName.Name = "txtLastName";
            this.txtLastName.Size = new System.Drawing.Size(360, 25);
            this.txtLastName.TabIndex = 1;
            //
            // lblFirstName
            //
            this.lblFirstName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFirstName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFirstName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblFirstName.Location = new System.Drawing.Point(361, 0);
            this.lblFirstName.Name = "lblFirstName";
            this.lblFirstName.Size = new System.Drawing.Size(172, 44);
            this.lblFirstName.TabIndex = 2;
            this.lblFirstName.Text = "First Name";
            this.lblFirstName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtFirstName
            //
            this.txtFirstName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtFirstName.Location = new System.Drawing.Point(539, 9);
            this.txtFirstName.Name = "txtFirstName";
            this.txtFirstName.Size = new System.Drawing.Size(360, 25);
            this.txtFirstName.TabIndex = 3;
            //
            // lblMiddleName
            //
            this.lblMiddleName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMiddleName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMiddleName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMiddleName.Location = new System.Drawing.Point(3, 44);
            this.lblMiddleName.Name = "lblMiddleName";
            this.lblMiddleName.Size = new System.Drawing.Size(172, 44);
            this.lblMiddleName.TabIndex = 4;
            this.lblMiddleName.Text = "Middle Name";
            this.lblMiddleName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtMiddleName
            //
            this.txtMiddleName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtMiddleName.Location = new System.Drawing.Point(181, 53);
            this.txtMiddleName.Name = "txtMiddleName";
            this.txtMiddleName.Size = new System.Drawing.Size(360, 25);
            this.txtMiddleName.TabIndex = 5;
            //
            // lblSex
            //
            this.lblSex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSex.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSex.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblSex.Location = new System.Drawing.Point(3, 88);
            this.lblSex.Name = "lblSex";
            this.lblSex.Size = new System.Drawing.Size(172, 44);
            this.lblSex.TabIndex = 6;
            this.lblSex.Text = "Sex";
            this.lblSex.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cboSex
            //
            this.cboSex.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cboSex.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSex.Location = new System.Drawing.Point(181, 97);
            this.cboSex.Name = "cboSex";
            this.cboSex.Size = new System.Drawing.Size(360, 25);
            this.cboSex.TabIndex = 7;
            //
            // lblCivil
            //
            this.lblCivil.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCivil.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCivil.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCivil.Location = new System.Drawing.Point(361, 88);
            this.lblCivil.Name = "lblCivil";
            this.lblCivil.Size = new System.Drawing.Size(172, 44);
            this.lblCivil.TabIndex = 8;
            this.lblCivil.Text = "Civil Status";
            this.lblCivil.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cboCivil
            //
            this.cboCivil.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cboCivil.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCivil.Location = new System.Drawing.Point(539, 97);
            this.cboCivil.Name = "cboCivil";
            this.cboCivil.Size = new System.Drawing.Size(360, 25);
            this.cboCivil.TabIndex = 9;
            //
            // lblAge
            //
            this.lblAge.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAge.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAge.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblAge.Location = new System.Drawing.Point(3, 132);
            this.lblAge.Name = "lblAge";
            this.lblAge.Size = new System.Drawing.Size(172, 44);
            this.lblAge.TabIndex = 10;
            this.lblAge.Text = "Age at Death";
            this.lblAge.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtAge
            //
            this.txtAge.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtAge.Location = new System.Drawing.Point(181, 141);
            this.txtAge.Name = "txtAge";
            this.txtAge.Size = new System.Drawing.Size(200, 25);
            this.txtAge.TabIndex = 11;
            //
            // lblCitizen
            //
            this.lblCitizen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCitizen.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCitizen.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCitizen.Location = new System.Drawing.Point(361, 132);
            this.lblCitizen.Name = "lblCitizen";
            this.lblCitizen.Size = new System.Drawing.Size(172, 44);
            this.lblCitizen.TabIndex = 12;
            this.lblCitizen.Text = "Citizenship";
            this.lblCitizen.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtCitizen
            //
            this.txtCitizen.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtCitizen.Location = new System.Drawing.Point(539, 141);
            this.txtCitizen.Name = "txtCitizen";
            this.txtCitizen.Size = new System.Drawing.Size(360, 25);
            this.txtCitizen.TabIndex = 13;
            //
            // lblDod
            //
            this.lblDod.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDod.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDod.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblDod.Location = new System.Drawing.Point(3, 176);
            this.lblDod.Name = "lblDod";
            this.lblDod.Size = new System.Drawing.Size(172, 44);
            this.lblDod.TabIndex = 14;
            this.lblDod.Text = "Date of Death";
            this.lblDod.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // dtpDod
            //
            this.dtpDod.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpDod.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDod.Location = new System.Drawing.Point(181, 185);
            this.dtpDod.Name = "dtpDod";
            this.dtpDod.Size = new System.Drawing.Size(200, 25);
            this.dtpDod.TabIndex = 15;
            //
            // lblTod
            //
            this.lblTod.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTod.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTod.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblTod.Location = new System.Drawing.Point(361, 176);
            this.lblTod.Name = "lblTod";
            this.lblTod.Size = new System.Drawing.Size(172, 44);
            this.lblTod.TabIndex = 16;
            this.lblTod.Text = "Time of Death";
            this.lblTod.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // dtpTod
            //
            this.dtpTod.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpTod.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpTod.Location = new System.Drawing.Point(539, 185);
            this.dtpTod.Name = "dtpTod";
            this.dtpTod.ShowUpDown = true;
            this.dtpTod.Size = new System.Drawing.Size(200, 25);
            this.dtpTod.TabIndex = 17;
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
            this.lblPlace.Text = "Place of Death";
            //
            // txtPlace
            //
            this.tblDeceased.SetColumnSpan(this.txtPlace, 3);
            this.txtPlace.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.txtPlace.Location = new System.Drawing.Point(181, 225);
            this.txtPlace.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.txtPlace.Name = "txtPlace";
            this.txtPlace.Size = new System.Drawing.Size(930, 25);
            this.txtPlace.TabIndex = 19;
            //
            // lblReligion
            //
            this.lblReligion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblReligion.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblReligion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblReligion.Location = new System.Drawing.Point(3, 284);
            this.lblReligion.Name = "lblReligion";
            this.lblReligion.Size = new System.Drawing.Size(172, 44);
            this.lblReligion.TabIndex = 20;
            this.lblReligion.Text = "Religion";
            this.lblReligion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtReligion
            //
            this.txtReligion.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtReligion.Location = new System.Drawing.Point(181, 293);
            this.txtReligion.Name = "txtReligion";
            this.txtReligion.Size = new System.Drawing.Size(360, 25);
            this.txtReligion.TabIndex = 21;
            //
            // tabCause
            //
            this.tabCause.AutoScroll = true;
            this.tabCause.BackColor = System.Drawing.Color.White;
            this.tabCause.Controls.Add(this.tblCause);
            this.tabCause.Location = new System.Drawing.Point(4, 30);
            this.tabCause.Name = "tabCause";
            this.tabCause.Padding = new System.Windows.Forms.Padding(18, 14, 18, 14);
            this.tabCause.Size = new System.Drawing.Size(1324, 328);
            this.tabCause.TabIndex = 1;
            this.tabCause.Text = "Cause && Disposal";
            //
            // tblCause
            //
            this.tblCause.ColumnCount = 4;
            this.tblCause.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblCause.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 380F));
            this.tblCause.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblCause.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 380F));
            this.tblCause.Controls.Add(this.lblImm, 0, 0);
            this.tblCause.Controls.Add(this.txtImm, 1, 0);
            this.tblCause.Controls.Add(this.lblAnt, 0, 1);
            this.tblCause.Controls.Add(this.txtAnt, 1, 1);
            this.tblCause.Controls.Add(this.lblUnd, 0, 2);
            this.tblCause.Controls.Add(this.txtUnd, 1, 2);
            this.tblCause.Controls.Add(this.lblCertifier, 0, 3);
            this.tblCause.Controls.Add(this.txtCertifier, 1, 3);
            this.tblCause.Controls.Add(this.lblLicense, 2, 3);
            this.tblCause.Controls.Add(this.txtLicense, 3, 3);
            this.tblCause.Controls.Add(this.lblDisposal, 0, 4);
            this.tblCause.Controls.Add(this.cboDisposal, 1, 4);
            this.tblCause.Controls.Add(this.lblDispPlace, 2, 4);
            this.tblCause.Controls.Add(this.txtDispPlace, 3, 4);
            this.tblCause.Controls.Add(this.lblDispDate, 0, 5);
            this.tblCause.Controls.Add(this.dtpDispDate, 1, 5);
            this.tblCause.Controls.Add(this.lblPermit, 2, 5);
            this.tblCause.Controls.Add(this.cboPermit, 3, 5);
            this.tblCause.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblCause.Location = new System.Drawing.Point(18, 14);
            this.tblCause.Name = "tblCause";
            this.tblCause.RowCount = 6;
            this.tblCause.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCause.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCause.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCause.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCause.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCause.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCause.Size = new System.Drawing.Size(1116, 264);
            this.tblCause.TabIndex = 0;
            //
            // lblImm
            //
            this.lblImm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblImm.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblImm.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblImm.Location = new System.Drawing.Point(3, 0);
            this.lblImm.Name = "lblImm";
            this.lblImm.Size = new System.Drawing.Size(172, 44);
            this.lblImm.TabIndex = 0;
            this.lblImm.Text = "Immediate Cause";
            this.lblImm.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtImm
            //
            this.tblCause.SetColumnSpan(this.txtImm, 3);
            this.txtImm.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtImm.Location = new System.Drawing.Point(181, 9);
            this.txtImm.Name = "txtImm";
            this.txtImm.Size = new System.Drawing.Size(930, 25);
            this.txtImm.TabIndex = 1;
            //
            // lblAnt
            //
            this.lblAnt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAnt.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAnt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblAnt.Location = new System.Drawing.Point(3, 44);
            this.lblAnt.Name = "lblAnt";
            this.lblAnt.Size = new System.Drawing.Size(172, 44);
            this.lblAnt.TabIndex = 2;
            this.lblAnt.Text = "Antecedent Cause";
            this.lblAnt.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtAnt
            //
            this.tblCause.SetColumnSpan(this.txtAnt, 3);
            this.txtAnt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAnt.Location = new System.Drawing.Point(181, 53);
            this.txtAnt.Name = "txtAnt";
            this.txtAnt.Size = new System.Drawing.Size(930, 25);
            this.txtAnt.TabIndex = 3;
            //
            // lblUnd
            //
            this.lblUnd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUnd.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblUnd.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblUnd.Location = new System.Drawing.Point(3, 88);
            this.lblUnd.Name = "lblUnd";
            this.lblUnd.Size = new System.Drawing.Size(172, 44);
            this.lblUnd.TabIndex = 4;
            this.lblUnd.Text = "Underlying Cause";
            this.lblUnd.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtUnd
            //
            this.tblCause.SetColumnSpan(this.txtUnd, 3);
            this.txtUnd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtUnd.Location = new System.Drawing.Point(181, 97);
            this.txtUnd.Name = "txtUnd";
            this.txtUnd.Size = new System.Drawing.Size(930, 25);
            this.txtUnd.TabIndex = 5;
            //
            // lblCertifier
            //
            this.lblCertifier.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCertifier.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCertifier.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCertifier.Location = new System.Drawing.Point(3, 132);
            this.lblCertifier.Name = "lblCertifier";
            this.lblCertifier.Size = new System.Drawing.Size(172, 44);
            this.lblCertifier.TabIndex = 6;
            this.lblCertifier.Text = "Medical Certifier";
            this.lblCertifier.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtCertifier
            //
            this.txtCertifier.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtCertifier.Location = new System.Drawing.Point(181, 141);
            this.txtCertifier.Name = "txtCertifier";
            this.txtCertifier.Size = new System.Drawing.Size(360, 25);
            this.txtCertifier.TabIndex = 7;
            //
            // lblLicense
            //
            this.lblLicense.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLicense.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblLicense.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblLicense.Location = new System.Drawing.Point(361, 132);
            this.lblLicense.Name = "lblLicense";
            this.lblLicense.Size = new System.Drawing.Size(172, 44);
            this.lblLicense.TabIndex = 8;
            this.lblLicense.Text = "License No.";
            this.lblLicense.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtLicense
            //
            this.txtLicense.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtLicense.Location = new System.Drawing.Point(539, 141);
            this.txtLicense.Name = "txtLicense";
            this.txtLicense.Size = new System.Drawing.Size(360, 25);
            this.txtLicense.TabIndex = 9;
            //
            // lblDisposal
            //
            this.lblDisposal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDisposal.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDisposal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblDisposal.Location = new System.Drawing.Point(3, 176);
            this.lblDisposal.Name = "lblDisposal";
            this.lblDisposal.Size = new System.Drawing.Size(172, 44);
            this.lblDisposal.TabIndex = 10;
            this.lblDisposal.Text = "Disposal Method";
            this.lblDisposal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cboDisposal
            //
            this.cboDisposal.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cboDisposal.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDisposal.Location = new System.Drawing.Point(181, 185);
            this.cboDisposal.Name = "cboDisposal";
            this.cboDisposal.Size = new System.Drawing.Size(360, 25);
            this.cboDisposal.TabIndex = 11;
            //
            // lblDispPlace
            //
            this.lblDispPlace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDispPlace.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDispPlace.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblDispPlace.Location = new System.Drawing.Point(361, 176);
            this.lblDispPlace.Name = "lblDispPlace";
            this.lblDispPlace.Size = new System.Drawing.Size(172, 44);
            this.lblDispPlace.TabIndex = 12;
            this.lblDispPlace.Text = "Place of Disposal";
            this.lblDispPlace.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtDispPlace
            //
            this.txtDispPlace.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtDispPlace.Location = new System.Drawing.Point(539, 185);
            this.txtDispPlace.Name = "txtDispPlace";
            this.txtDispPlace.Size = new System.Drawing.Size(360, 25);
            this.txtDispPlace.TabIndex = 13;
            //
            // lblDispDate
            //
            this.lblDispDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDispDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDispDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblDispDate.Location = new System.Drawing.Point(3, 220);
            this.lblDispDate.Name = "lblDispDate";
            this.lblDispDate.Size = new System.Drawing.Size(172, 44);
            this.lblDispDate.TabIndex = 14;
            this.lblDispDate.Text = "Date of Disposal";
            this.lblDispDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // dtpDispDate
            //
            this.dtpDispDate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpDispDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDispDate.Location = new System.Drawing.Point(181, 229);
            this.dtpDispDate.Name = "dtpDispDate";
            this.dtpDispDate.ShowCheckBox = true;
            this.dtpDispDate.Size = new System.Drawing.Size(200, 25);
            this.dtpDispDate.TabIndex = 15;
            //
            // lblPermit
            //
            this.lblPermit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPermit.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPermit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblPermit.Location = new System.Drawing.Point(361, 220);
            this.lblPermit.Name = "lblPermit";
            this.lblPermit.Size = new System.Drawing.Size(172, 44);
            this.lblPermit.TabIndex = 16;
            this.lblPermit.Text = "Permit Type";
            this.lblPermit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cboPermit
            //
            this.cboPermit.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cboPermit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPermit.Location = new System.Drawing.Point(539, 229);
            this.cboPermit.Name = "cboPermit";
            this.cboPermit.Size = new System.Drawing.Size(360, 25);
            this.cboPermit.TabIndex = 17;
            //
            // tabInformant
            //
            this.tabInformant.AutoScroll = true;
            this.tabInformant.BackColor = System.Drawing.Color.White;
            this.tabInformant.Controls.Add(this.tblInformant);
            this.tabInformant.Location = new System.Drawing.Point(4, 30);
            this.tabInformant.Name = "tabInformant";
            this.tabInformant.Padding = new System.Windows.Forms.Padding(18, 14, 18, 14);
            this.tabInformant.Size = new System.Drawing.Size(1324, 328);
            this.tabInformant.TabIndex = 2;
            this.tabInformant.Text = "Informant";
            //
            // tblInformant
            //
            this.tblInformant.ColumnCount = 4;
            this.tblInformant.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblInformant.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 380F));
            this.tblInformant.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblInformant.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 380F));
            this.tblInformant.Controls.Add(this.lblCInfName, 0, 0);
            this.tblInformant.Controls.Add(this.txtCInfName, 1, 0);
            this.tblInformant.Controls.Add(this.lblCInfRel, 2, 0);
            this.tblInformant.Controls.Add(this.txtCInfRel, 3, 0);
            this.tblInformant.Controls.Add(this.lblCInfDate, 0, 1);
            this.tblInformant.Controls.Add(this.dtpCInfDate, 1, 1);
            this.tblInformant.Controls.Add(this.lblCInfRelOther, 2, 1);
            this.tblInformant.Controls.Add(this.txtCInfRelOther, 3, 1);
            this.tblInformant.Controls.Add(this.lblCInfAddr, 0, 2);
            this.tblInformant.Controls.Add(this.txtCInfAddr, 1, 2);
            this.tblInformant.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblInformant.Location = new System.Drawing.Point(18, 14);
            this.tblInformant.Name = "tblInformant";
            this.tblInformant.RowCount = 3;
            this.tblInformant.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblInformant.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblInformant.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.tblInformant.Size = new System.Drawing.Size(1116, 152);
            this.tblInformant.TabIndex = 0;
            //
            // lblCInfName
            //
            this.lblCInfName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCInfName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCInfName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCInfName.Location = new System.Drawing.Point(3, 0);
            this.lblCInfName.Name = "lblCInfName";
            this.lblCInfName.Size = new System.Drawing.Size(172, 44);
            this.lblCInfName.TabIndex = 0;
            this.lblCInfName.Text = "Informant - Name in Print";
            this.lblCInfName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtCInfName
            //
            this.txtCInfName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtCInfName.Location = new System.Drawing.Point(181, 9);
            this.txtCInfName.Name = "txtCInfName";
            this.txtCInfName.Size = new System.Drawing.Size(360, 25);
            this.txtCInfName.TabIndex = 1;
            //
            // lblCInfRel
            //
            this.lblCInfRel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCInfRel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCInfRel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCInfRel.Location = new System.Drawing.Point(361, 0);
            this.lblCInfRel.Name = "lblCInfRel";
            this.lblCInfRel.Size = new System.Drawing.Size(172, 44);
            this.lblCInfRel.TabIndex = 2;
            this.lblCInfRel.Text = "Relationship to the Deceased";
            this.lblCInfRel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtCInfRel
            //
            this.txtCInfRel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtCInfRel.Location = new System.Drawing.Point(539, 9);
            this.txtCInfRel.Name = "txtCInfRel";
            this.txtCInfRel.Size = new System.Drawing.Size(360, 25);
            this.txtCInfRel.TabIndex = 3;
            //
            // lblCInfDate
            //
            this.lblCInfDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCInfDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCInfDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCInfDate.Location = new System.Drawing.Point(3, 44);
            this.lblCInfDate.Name = "lblCInfDate";
            this.lblCInfDate.Size = new System.Drawing.Size(172, 44);
            this.lblCInfDate.TabIndex = 4;
            this.lblCInfDate.Text = "Date Signed";
            this.lblCInfDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // dtpCInfDate
            //
            this.dtpCInfDate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpCInfDate.Checked = false;
            this.dtpCInfDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpCInfDate.Location = new System.Drawing.Point(181, 53);
            this.dtpCInfDate.Name = "dtpCInfDate";
            this.dtpCInfDate.ShowCheckBox = true;
            this.dtpCInfDate.Size = new System.Drawing.Size(200, 25);
            this.dtpCInfDate.TabIndex = 5;
            //
            // lblCInfRelOther
            //
            this.lblCInfRelOther.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCInfRelOther.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCInfRelOther.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCInfRelOther.Location = new System.Drawing.Point(361, 44);
            this.lblCInfRelOther.Name = "lblCInfRelOther";
            this.lblCInfRelOther.Size = new System.Drawing.Size(172, 44);
            this.lblCInfRelOther.TabIndex = 6;
            this.lblCInfRelOther.Text = "If Others, specify";
            this.lblCInfRelOther.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblCInfRelOther.Visible = false;
            //
            // txtCInfRelOther
            //
            this.txtCInfRelOther.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtCInfRelOther.Location = new System.Drawing.Point(539, 53);
            this.txtCInfRelOther.Name = "txtCInfRelOther";
            this.txtCInfRelOther.Size = new System.Drawing.Size(360, 25);
            this.txtCInfRelOther.TabIndex = 7;
            this.txtCInfRelOther.Visible = false;
            //
            // lblCInfAddr
            //
            this.lblCInfAddr.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCInfAddr.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCInfAddr.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCInfAddr.Location = new System.Drawing.Point(3, 88);
            this.lblCInfAddr.Name = "lblCInfAddr";
            this.lblCInfAddr.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblCInfAddr.Size = new System.Drawing.Size(172, 64);
            this.lblCInfAddr.TabIndex = 8;
            this.lblCInfAddr.Text = "Informant - Address";
            //
            // txtCInfAddr
            //
            this.tblInformant.SetColumnSpan(this.txtCInfAddr, 3);
            this.txtCInfAddr.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCInfAddr.Location = new System.Drawing.Point(181, 93);
            this.txtCInfAddr.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.txtCInfAddr.Name = "txtCInfAddr";
            this.txtCInfAddr.Size = new System.Drawing.Size(930, 25);
            this.txtCInfAddr.TabIndex = 9;
            //
            // tabCertification
            //
            this.tabCertification.AutoScroll = true;
            this.tabCertification.BackColor = System.Drawing.Color.White;
            this.tabCertification.Controls.Add(this.tblCertification);
            this.tabCertification.Location = new System.Drawing.Point(4, 30);
            this.tabCertification.Name = "tabCertification";
            this.tabCertification.Padding = new System.Windows.Forms.Padding(18, 14, 18, 14);
            this.tabCertification.Size = new System.Drawing.Size(1324, 328);
            this.tabCertification.TabIndex = 3;
            this.tabCertification.Text = "Certification";
            //
            // tblCertification
            //
            this.tblCertification.ColumnCount = 4;
            this.tblCertification.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblCertification.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 380F));
            this.tblCertification.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblCertification.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 380F));
            this.tblCertification.Controls.Add(this.lblBookVol, 0, 0);
            this.tblCertification.Controls.Add(this.txtBookVol, 1, 0);
            this.tblCertification.Controls.Add(this.lblBookPage, 2, 0);
            this.tblCertification.Controls.Add(this.txtBookPage, 3, 0);
            this.tblCertification.Controls.Add(this.lblCPrepBy, 0, 1);
            this.tblCertification.Controls.Add(this.txtCPrepBy, 1, 1);
            this.tblCertification.Controls.Add(this.lblCRecvBy, 2, 1);
            this.tblCertification.Controls.Add(this.txtCRecvBy, 3, 1);
            this.tblCertification.Controls.Add(this.lblCPrepTitle, 0, 2);
            this.tblCertification.Controls.Add(this.txtCPrepTitle, 1, 2);
            this.tblCertification.Controls.Add(this.lblCRecvTitle, 2, 2);
            this.tblCertification.Controls.Add(this.txtCRecvTitle, 3, 2);
            this.tblCertification.Controls.Add(this.lblCPrepDate, 0, 3);
            this.tblCertification.Controls.Add(this.dtpCPrepDate, 1, 3);
            this.tblCertification.Controls.Add(this.lblCRecvDate, 2, 3);
            this.tblCertification.Controls.Add(this.dtpCRecvDate, 3, 3);
            this.tblCertification.Controls.Add(this.lblCRegBy, 0, 4);
            this.tblCertification.Controls.Add(this.txtCRegBy, 1, 4);
            this.tblCertification.Controls.Add(this.lblCRegTitle, 2, 4);
            this.tblCertification.Controls.Add(this.txtCRegTitle, 3, 4);
            this.tblCertification.Controls.Add(this.lblCRegDate, 0, 5);
            this.tblCertification.Controls.Add(this.dtpCRegDate, 1, 5);
            this.tblCertification.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblCertification.Location = new System.Drawing.Point(18, 14);
            this.tblCertification.Name = "tblCertification";
            this.tblCertification.RowCount = 6;
            this.tblCertification.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCertification.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCertification.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCertification.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCertification.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCertification.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tblCertification.Size = new System.Drawing.Size(1116, 264);
            this.tblCertification.TabIndex = 0;
            //
            // lblBookVol
            //
            this.lblBookVol.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBookVol.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBookVol.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBookVol.Location = new System.Drawing.Point(3, 0);
            this.lblBookVol.Name = "lblBookVol";
            this.lblBookVol.Size = new System.Drawing.Size(172, 44);
            this.lblBookVol.TabIndex = 0;
            this.lblBookVol.Text = "Book / Volume";
            this.lblBookVol.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtBookVol
            //
            this.txtBookVol.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtBookVol.Location = new System.Drawing.Point(181, 9);
            this.txtBookVol.Name = "txtBookVol";
            this.txtBookVol.Size = new System.Drawing.Size(360, 25);
            this.txtBookVol.TabIndex = 1;
            //
            // lblBookPage
            //
            this.lblBookPage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBookPage.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBookPage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblBookPage.Location = new System.Drawing.Point(361, 0);
            this.lblBookPage.Name = "lblBookPage";
            this.lblBookPage.Size = new System.Drawing.Size(172, 44);
            this.lblBookPage.TabIndex = 2;
            this.lblBookPage.Text = "Book Page";
            this.lblBookPage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtBookPage
            //
            this.txtBookPage.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtBookPage.Location = new System.Drawing.Point(539, 9);
            this.txtBookPage.Name = "txtBookPage";
            this.txtBookPage.Size = new System.Drawing.Size(360, 25);
            this.txtBookPage.TabIndex = 3;
            //
            // lblCPrepBy
            //
            this.lblCPrepBy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCPrepBy.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCPrepBy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCPrepBy.Location = new System.Drawing.Point(3, 44);
            this.lblCPrepBy.Name = "lblCPrepBy";
            this.lblCPrepBy.Size = new System.Drawing.Size(172, 44);
            this.lblCPrepBy.TabIndex = 4;
            this.lblCPrepBy.Text = "Prepared By - Name";
            this.lblCPrepBy.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtCPrepBy
            //
            this.txtCPrepBy.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtCPrepBy.Location = new System.Drawing.Point(181, 53);
            this.txtCPrepBy.Name = "txtCPrepBy";
            this.txtCPrepBy.Size = new System.Drawing.Size(360, 25);
            this.txtCPrepBy.TabIndex = 5;
            //
            // lblCRecvBy
            //
            this.lblCRecvBy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCRecvBy.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCRecvBy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCRecvBy.Location = new System.Drawing.Point(361, 44);
            this.lblCRecvBy.Name = "lblCRecvBy";
            this.lblCRecvBy.Size = new System.Drawing.Size(172, 44);
            this.lblCRecvBy.TabIndex = 6;
            this.lblCRecvBy.Text = "Received By - Name";
            this.lblCRecvBy.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtCRecvBy
            //
            this.txtCRecvBy.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtCRecvBy.Location = new System.Drawing.Point(539, 53);
            this.txtCRecvBy.Name = "txtCRecvBy";
            this.txtCRecvBy.Size = new System.Drawing.Size(360, 25);
            this.txtCRecvBy.TabIndex = 7;
            //
            // lblCPrepTitle
            //
            this.lblCPrepTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCPrepTitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCPrepTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCPrepTitle.Location = new System.Drawing.Point(3, 88);
            this.lblCPrepTitle.Name = "lblCPrepTitle";
            this.lblCPrepTitle.Size = new System.Drawing.Size(172, 44);
            this.lblCPrepTitle.TabIndex = 8;
            this.lblCPrepTitle.Text = "Prepared By - Title";
            this.lblCPrepTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtCPrepTitle
            //
            this.txtCPrepTitle.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtCPrepTitle.Location = new System.Drawing.Point(181, 97);
            this.txtCPrepTitle.Name = "txtCPrepTitle";
            this.txtCPrepTitle.Size = new System.Drawing.Size(360, 25);
            this.txtCPrepTitle.TabIndex = 9;
            //
            // lblCRecvTitle
            //
            this.lblCRecvTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCRecvTitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCRecvTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCRecvTitle.Location = new System.Drawing.Point(361, 88);
            this.lblCRecvTitle.Name = "lblCRecvTitle";
            this.lblCRecvTitle.Size = new System.Drawing.Size(172, 44);
            this.lblCRecvTitle.TabIndex = 10;
            this.lblCRecvTitle.Text = "Received By - Title";
            this.lblCRecvTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtCRecvTitle
            //
            this.txtCRecvTitle.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtCRecvTitle.Location = new System.Drawing.Point(539, 97);
            this.txtCRecvTitle.Name = "txtCRecvTitle";
            this.txtCRecvTitle.Size = new System.Drawing.Size(360, 25);
            this.txtCRecvTitle.TabIndex = 11;
            //
            // lblCPrepDate
            //
            this.lblCPrepDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCPrepDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCPrepDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCPrepDate.Location = new System.Drawing.Point(3, 132);
            this.lblCPrepDate.Name = "lblCPrepDate";
            this.lblCPrepDate.Size = new System.Drawing.Size(172, 44);
            this.lblCPrepDate.TabIndex = 12;
            this.lblCPrepDate.Text = "Prepared By - Date";
            this.lblCPrepDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // dtpCPrepDate
            //
            this.dtpCPrepDate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpCPrepDate.Checked = false;
            this.dtpCPrepDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpCPrepDate.Location = new System.Drawing.Point(181, 141);
            this.dtpCPrepDate.Name = "dtpCPrepDate";
            this.dtpCPrepDate.ShowCheckBox = true;
            this.dtpCPrepDate.Size = new System.Drawing.Size(200, 25);
            this.dtpCPrepDate.TabIndex = 13;
            //
            // lblCRecvDate
            //
            this.lblCRecvDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCRecvDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCRecvDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCRecvDate.Location = new System.Drawing.Point(361, 132);
            this.lblCRecvDate.Name = "lblCRecvDate";
            this.lblCRecvDate.Size = new System.Drawing.Size(172, 44);
            this.lblCRecvDate.TabIndex = 14;
            this.lblCRecvDate.Text = "Received By - Date";
            this.lblCRecvDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // dtpCRecvDate
            //
            this.dtpCRecvDate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpCRecvDate.Checked = false;
            this.dtpCRecvDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpCRecvDate.Location = new System.Drawing.Point(539, 141);
            this.dtpCRecvDate.Name = "dtpCRecvDate";
            this.dtpCRecvDate.ShowCheckBox = true;
            this.dtpCRecvDate.Size = new System.Drawing.Size(200, 25);
            this.dtpCRecvDate.TabIndex = 15;
            //
            // lblCRegBy
            //
            this.lblCRegBy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCRegBy.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCRegBy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCRegBy.Location = new System.Drawing.Point(3, 176);
            this.lblCRegBy.Name = "lblCRegBy";
            this.lblCRegBy.Size = new System.Drawing.Size(172, 44);
            this.lblCRegBy.TabIndex = 16;
            this.lblCRegBy.Text = "Registered By - Name";
            this.lblCRegBy.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtCRegBy
            //
            this.txtCRegBy.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtCRegBy.Location = new System.Drawing.Point(181, 185);
            this.txtCRegBy.Name = "txtCRegBy";
            this.txtCRegBy.Size = new System.Drawing.Size(360, 25);
            this.txtCRegBy.TabIndex = 17;
            //
            // lblCRegTitle
            //
            this.lblCRegTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCRegTitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCRegTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCRegTitle.Location = new System.Drawing.Point(361, 176);
            this.lblCRegTitle.Name = "lblCRegTitle";
            this.lblCRegTitle.Size = new System.Drawing.Size(172, 44);
            this.lblCRegTitle.TabIndex = 18;
            this.lblCRegTitle.Text = "Registered By - Title";
            this.lblCRegTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtCRegTitle
            //
            this.txtCRegTitle.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtCRegTitle.Location = new System.Drawing.Point(539, 185);
            this.txtCRegTitle.Name = "txtCRegTitle";
            this.txtCRegTitle.Size = new System.Drawing.Size(360, 25);
            this.txtCRegTitle.TabIndex = 19;
            //
            // lblCRegDate
            //
            this.lblCRegDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCRegDate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCRegDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCRegDate.Location = new System.Drawing.Point(3, 220);
            this.lblCRegDate.Name = "lblCRegDate";
            this.lblCRegDate.Size = new System.Drawing.Size(172, 44);
            this.lblCRegDate.TabIndex = 20;
            this.lblCRegDate.Text = "Registered By - Date";
            this.lblCRegDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // dtpCRegDate
            //
            this.dtpCRegDate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpCRegDate.Checked = false;
            this.dtpCRegDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpCRegDate.Location = new System.Drawing.Point(181, 229);
            this.dtpCRegDate.Name = "dtpCRegDate";
            this.dtpCRegDate.ShowCheckBox = true;
            this.dtpCRegDate.Size = new System.Drawing.Size(200, 25);
            this.dtpCRegDate.TabIndex = 21;
            //
            // pnlRecordActions
            //
            this.pnlRecordActions.AutoSize = true;
            this.pnlRecordActions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlRecordActions.Controls.Add(this.btnDelete);
            this.pnlRecordActions.Controls.Add(this.btnUpdate);
            this.pnlRecordActions.Controls.Add(this.btnNew);
            this.pnlRecordActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRecordActions.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlRecordActions.Location = new System.Drawing.Point(14, 12);
            this.pnlRecordActions.Margin = new System.Windows.Forms.Padding(0);
            this.pnlRecordActions.Name = "pnlRecordActions";
            this.pnlRecordActions.Size = new System.Drawing.Size(1332, 42);
            this.pnlRecordActions.TabIndex = 1;
            this.pnlRecordActions.WrapContents = false;
            //
            // btnDelete
            //
            this.btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDelete.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnDelete.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(50)))), ((int)(((byte)(63)))));
            this.btnDelete.Location = new System.Drawing.Point(1232, 6);
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
            this.btnUpdate.Location = new System.Drawing.Point(1126, 6);
            this.btnUpdate.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(100, 30);
            this.btnUpdate.TabIndex = 1;
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            //
            // btnNew
            //
            this.btnNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNew.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnNew.Location = new System.Drawing.Point(1020, 6);
            this.btnNew.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(100, 30);
            this.btnNew.TabIndex = 0;
            this.btnNew.Text = "New";
            this.btnNew.UseVisualStyleBackColor = true;
            this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
            //
            // cardRecords
            //
            this.cardRecords.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.cardRecords.CardColor = System.Drawing.Color.White;
            this.cardRecords.Controls.Add(this.layoutRecords);
            this.cardRecords.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardRecords.DrawShadow = true;
            this.cardRecords.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.cardRecords.Location = new System.Drawing.Point(20, 540);
            this.cardRecords.Margin = new System.Windows.Forms.Padding(0);
            this.cardRecords.Name = "cardRecords";
            this.cardRecords.Padding = new System.Windows.Forms.Padding(16, 10, 16, 14);
            this.cardRecords.Radius = 10;
            this.cardRecords.Size = new System.Drawing.Size(1360, 326);
            this.cardRecords.TabIndex = 2;
            //
            // layoutRecords
            //
            this.layoutRecords.ColumnCount = 1;
            this.layoutRecords.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRecords.Controls.Add(this.pnlRecordsHead, 0, 0);
            this.layoutRecords.Controls.Add(this.pnlSearch, 0, 1);
            this.layoutRecords.Controls.Add(this.dgvDeaths, 0, 2);
            this.layoutRecords.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutRecords.Location = new System.Drawing.Point(16, 10);
            this.layoutRecords.Margin = new System.Windows.Forms.Padding(0);
            this.layoutRecords.Name = "layoutRecords";
            this.layoutRecords.RowCount = 3;
            this.layoutRecords.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.layoutRecords.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.layoutRecords.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRecords.Size = new System.Drawing.Size(1328, 302);
            this.layoutRecords.TabIndex = 0;
            //
            // pnlRecordsHead
            //
            this.pnlRecordsHead.ColumnCount = 2;
            this.pnlRecordsHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlRecordsHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.pnlRecordsHead.Controls.Add(this.lblRecent, 0, 0);
            this.pnlRecordsHead.Controls.Add(this.pnlListActions, 1, 0);
            this.pnlRecordsHead.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRecordsHead.Location = new System.Drawing.Point(0, 0);
            this.pnlRecordsHead.Margin = new System.Windows.Forms.Padding(0);
            this.pnlRecordsHead.Name = "pnlRecordsHead";
            this.pnlRecordsHead.RowCount = 1;
            this.pnlRecordsHead.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlRecordsHead.Size = new System.Drawing.Size(1328, 42);
            this.pnlRecordsHead.TabIndex = 0;
            //
            // lblRecent
            //
            this.lblRecent.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblRecent.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblRecent.Location = new System.Drawing.Point(3, 0);
            this.lblRecent.Name = "lblRecent";
            this.lblRecent.Size = new System.Drawing.Size(600, 42);
            this.lblRecent.TabIndex = 0;
            this.lblRecent.Text = "RECENT DEATH REGISTRATIONS";
            this.lblRecent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // pnlListActions
            //
            this.pnlListActions.AutoSize = true;
            this.pnlListActions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlListActions.Controls.Add(this.btnNewRegistration);
            this.pnlListActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlListActions.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlListActions.Location = new System.Drawing.Point(1108, 0);
            this.pnlListActions.Margin = new System.Windows.Forms.Padding(0);
            this.pnlListActions.Name = "pnlListActions";
            this.pnlListActions.Size = new System.Drawing.Size(220, 42);
            this.pnlListActions.TabIndex = 1;
            this.pnlListActions.WrapContents = false;
            //
            // btnNewRegistration
            //
            this.btnNewRegistration.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.btnNewRegistration.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNewRegistration.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnNewRegistration.ForeColor = System.Drawing.Color.White;
            this.btnNewRegistration.Location = new System.Drawing.Point(6, 6);
            this.btnNewRegistration.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnNewRegistration.Name = "btnNewRegistration";
            this.btnNewRegistration.Size = new System.Drawing.Size(214, 30);
            this.btnNewRegistration.TabIndex = 0;
            this.btnNewRegistration.Text = "+ New Death Registration";
            this.btnNewRegistration.UseVisualStyleBackColor = false;
            this.btnNewRegistration.Click += new System.EventHandler(this.btnNew_Click);
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
            this.pnlSearch.Size = new System.Drawing.Size(1328, 28);
            this.pnlSearch.TabIndex = 2;
            //
            // txtSearch
            //
            this.txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtSearch.Location = new System.Drawing.Point(3, 3);
            this.txtSearch.Margin = new System.Windows.Forms.Padding(3, 3, 6, 0);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(1245, 23);
            this.txtSearch.TabIndex = 0;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            //
            // btnClearSearch
            //
            this.btnClearSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearSearch.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnClearSearch.Location = new System.Drawing.Point(1254, 0);
            this.btnClearSearch.Margin = new System.Windows.Forms.Padding(0);
            this.btnClearSearch.Name = "btnClearSearch";
            this.btnClearSearch.Size = new System.Drawing.Size(74, 28);
            this.btnClearSearch.TabIndex = 1;
            this.btnClearSearch.Text = "Clear";
            this.btnClearSearch.UseVisualStyleBackColor = true;
            this.btnClearSearch.Click += new System.EventHandler(this.btnClearSearch_Click);
            //
            // dgvDeaths
            //
            this.dgvDeaths.AllowUserToAddRows = false;
            this.dgvDeaths.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvDeaths.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvDeaths.BackgroundColor = System.Drawing.Color.White;
            this.dgvDeaths.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDeaths.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDeaths.Location = new System.Drawing.Point(0, 76);
            this.dgvDeaths.Margin = new System.Windows.Forms.Padding(0);
            this.dgvDeaths.Name = "dgvDeaths";
            this.dgvDeaths.ReadOnly = true;
            this.dgvDeaths.RowHeadersVisible = false;
            this.dgvDeaths.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvDeaths.Size = new System.Drawing.Size(1328, 226);
            this.dgvDeaths.TabIndex = 3;
            this.dgvDeaths.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvDeaths_CellClick);
            //
            // DeathRegistrationForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(1000, 900);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(1400, 900);
            this.Controls.Add(this.layoutRoot);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "DeathRegistrationForm";
            this.Text = "Death Registration";
            this.Resize += new System.EventHandler(this.DeathRegistrationForm_Resize);
            this.certificateMenu.ResumeLayout(false);
            this.layoutRoot.ResumeLayout(false);
            this.layoutMain.ResumeLayout(false);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeadText.ResumeLayout(false);
            this.pnlHeadText.PerformLayout();
            this.pnlHeadActions.ResumeLayout(false);
            this.cardForm.ResumeLayout(false);
            this.cardForm.PerformLayout();
            this.tabControl.ResumeLayout(false);
            this.tabDeceased.ResumeLayout(false);
            this.tblDeceased.ResumeLayout(false);
            this.tblDeceased.PerformLayout();
            this.tabCause.ResumeLayout(false);
            this.tblCause.ResumeLayout(false);
            this.tblCause.PerformLayout();
            this.tabInformant.ResumeLayout(false);
            this.tblInformant.ResumeLayout(false);
            this.tblInformant.PerformLayout();
            this.tabCertification.ResumeLayout(false);
            this.tblCertification.ResumeLayout(false);
            this.tblCertification.PerformLayout();
            this.pnlRecordActions.ResumeLayout(false);
            this.cardRecords.ResumeLayout(false);
            this.layoutRecords.ResumeLayout(false);
            this.pnlRecordsHead.ResumeLayout(false);
            this.pnlRecordsHead.PerformLayout();
            this.pnlListActions.ResumeLayout(false);
            this.pnlSearch.ResumeLayout(false);
            this.pnlSearch.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDeaths)).EndInit();
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
        private System.Windows.Forms.Button btnSave;
        private CROMS.Modules.CardPanel cardForm;
        private System.Windows.Forms.TabControl tabControl;

        private System.Windows.Forms.TabPage tabDeceased;
        private System.Windows.Forms.TableLayoutPanel tblDeceased;
        private System.Windows.Forms.Label lblLastName;
        private System.Windows.Forms.TextBox txtLastName;
        private System.Windows.Forms.Label lblFirstName;
        private System.Windows.Forms.TextBox txtFirstName;
        private System.Windows.Forms.Label lblMiddleName;
        private System.Windows.Forms.TextBox txtMiddleName;
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

        private System.Windows.Forms.TabPage tabCause;
        private System.Windows.Forms.TableLayoutPanel tblCause;
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

        private System.Windows.Forms.TabPage tabInformant;
        private System.Windows.Forms.TableLayoutPanel tblInformant;
        private System.Windows.Forms.Label lblCInfName;
        private System.Windows.Forms.TextBox txtCInfName;
        private System.Windows.Forms.Label lblCInfRel;
        private System.Windows.Forms.TextBox txtCInfRel;
        private System.Windows.Forms.Label lblCInfDate;
        private System.Windows.Forms.DateTimePicker dtpCInfDate;
        private System.Windows.Forms.Label lblCInfRelOther;
        private System.Windows.Forms.TextBox txtCInfRelOther;
        private System.Windows.Forms.Label lblCInfAddr;
        private System.Windows.Forms.TextBox txtCInfAddr;

        private System.Windows.Forms.TabPage tabCertification;
        private System.Windows.Forms.TableLayoutPanel tblCertification;
        private System.Windows.Forms.Label lblBookVol;
        private System.Windows.Forms.TextBox txtBookVol;
        private System.Windows.Forms.Label lblBookPage;
        private System.Windows.Forms.TextBox txtBookPage;
        private System.Windows.Forms.Label lblCPrepBy;
        private System.Windows.Forms.TextBox txtCPrepBy;
        private System.Windows.Forms.Label lblCRecvBy;
        private System.Windows.Forms.TextBox txtCRecvBy;
        private System.Windows.Forms.Label lblCPrepTitle;
        private System.Windows.Forms.TextBox txtCPrepTitle;
        private System.Windows.Forms.Label lblCRecvTitle;
        private System.Windows.Forms.TextBox txtCRecvTitle;
        private System.Windows.Forms.Label lblCPrepDate;
        private System.Windows.Forms.DateTimePicker dtpCPrepDate;
        private System.Windows.Forms.Label lblCRecvDate;
        private System.Windows.Forms.DateTimePicker dtpCRecvDate;
        private System.Windows.Forms.Label lblCRegBy;
        private System.Windows.Forms.TextBox txtCRegBy;
        private System.Windows.Forms.Label lblCRegTitle;
        private System.Windows.Forms.TextBox txtCRegTitle;
        private System.Windows.Forms.Label lblCRegDate;
        private System.Windows.Forms.DateTimePicker dtpCRegDate;

        private System.Windows.Forms.FlowLayoutPanel pnlRecordActions;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnNew;

        private CROMS.Modules.CardPanel cardRecords;
        private System.Windows.Forms.TableLayoutPanel layoutRecords;
        private System.Windows.Forms.TableLayoutPanel pnlRecordsHead;
        private System.Windows.Forms.Label lblRecent;
        private System.Windows.Forms.FlowLayoutPanel pnlListActions;
        private System.Windows.Forms.Button btnNewRegistration;
        private System.Windows.Forms.TableLayoutPanel pnlSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Button btnClearSearch;
        private System.Windows.Forms.DataGridView dgvDeaths;
    }
}
