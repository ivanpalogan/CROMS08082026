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

        // ---------------------------------------------------------------------------
        // ONE spacing scale for the whole screen. Every gap below is one of these, so
        // nothing is a one-off guess:
        //   PagePad   24  page margin
        //   CardPad   20/16/20/18   inside a card (l/t/r/b)
        //   Gutter    20  between the two columns
        //   CardGap   14  between stacked cards
        //   FieldGap  16  between a field column and the next
        //   GroupGap  16  above a new field row inside a card
        //   LabelGap   5  label -> its input
        // ---------------------------------------------------------------------------
        private void InitializeComponent()
        {
            this.root = new System.Windows.Forms.TableLayoutPanel();
            this.tblHeader = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.pillQueueRef = new CROMS.Modules.StatusPill();
            this.tblBody = new System.Windows.Forms.TableLayoutPanel();
            this.tblLeft = new System.Windows.Forms.TableLayoutPanel();
            this.cardClient = new CROMS.Modules.CardPanel();
            this.tblClient = new System.Windows.Forms.TableLayoutPanel();
            this.flowHead1 = new System.Windows.Forms.FlowLayoutPanel();
            this.pillStep1 = new CROMS.Modules.StatusPill();
            this.lblClientTitle = new System.Windows.Forms.Label();
            this.lblClientDesc = new System.Windows.Forms.Label();
            this.lblFirst = new System.Windows.Forms.Label();
            this.lblMiddle = new System.Windows.Forms.Label();
            this.lblLast = new System.Windows.Forms.Label();
            this.txtFirst = new System.Windows.Forms.TextBox();
            this.txtMiddle = new System.Windows.Forms.TextBox();
            this.txtLast = new System.Windows.Forms.TextBox();
            this.cardCertificate = new CROMS.Modules.CardPanel();
            this.tblCert = new System.Windows.Forms.TableLayoutPanel();
            this.flowHead2 = new System.Windows.Forms.FlowLayoutPanel();
            this.pillStep2 = new CROMS.Modules.StatusPill();
            this.lblCertTitle = new System.Windows.Forms.Label();
            this.lblCertDesc = new System.Windows.Forms.Label();
            this.lblCertType = new System.Windows.Forms.Label();
            this.lblRecordType = new System.Windows.Forms.Label();
            this.cboCertType = new System.Windows.Forms.ComboBox();
            this.cboRecordType = new System.Windows.Forms.ComboBox();
            this.lblSelRecord = new System.Windows.Forms.Label();
            this.cboRecord = new System.Windows.Forms.ComboBox();
            this.cardPurpose = new CROMS.Modules.CardPanel();
            this.tblPurpose = new System.Windows.Forms.TableLayoutPanel();
            this.flowHead3 = new System.Windows.Forms.FlowLayoutPanel();
            this.pillStep3 = new CROMS.Modules.StatusPill();
            this.lblPurposeTitle = new System.Windows.Forms.Label();
            this.lblPurposeDesc = new System.Windows.Forms.Label();
            this.lblCopies = new System.Windows.Forms.Label();
            this.lblPurpose = new System.Windows.Forms.Label();
            this.txtCopies = new System.Windows.Forms.TextBox();
            this.txtPurpose = new System.Windows.Forms.TextBox();
            this.lblPurposeHint = new System.Windows.Forms.Label();
            this.tblRight = new System.Windows.Forms.TableLayoutPanel();
            this.cardSummary = new CROMS.Modules.CardPanel();
            this.tblSummary = new System.Windows.Forms.TableLayoutPanel();
            this.lblSummaryTitle = new System.Windows.Forms.Label();
            this.lblSummarySub = new System.Windows.Forms.Label();
            this.lblCapClient = new System.Windows.Forms.Label();
            this.lblSumClient = new System.Windows.Forms.Label();
            this.lblCapCertType = new System.Windows.Forms.Label();
            this.lblSumCertType = new System.Windows.Forms.Label();
            this.lblCapRecordType = new System.Windows.Forms.Label();
            this.lblSumRecordType = new System.Windows.Forms.Label();
            this.lblCapRecord = new System.Windows.Forms.Label();
            this.lblSumRecord = new System.Windows.Forms.Label();
            this.lblCapCopies = new System.Windows.Forms.Label();
            this.lblSumCopies = new System.Windows.Forms.Label();
            this.lblCapPurpose = new System.Windows.Forms.Label();
            this.lblSumPurpose = new System.Windows.Forms.Label();
            this.pnlDivider = new System.Windows.Forms.Panel();
            this.pnlNextStep = new System.Windows.Forms.Panel();
            this.pnlNextBody = new System.Windows.Forms.Panel();
            this.lblNextStep = new System.Windows.Forms.Label();
            this.lblNextCap = new System.Windows.Forms.Label();
            this.pnlNextBar = new System.Windows.Forms.Panel();
            this.cardPhoto = new CROMS.Modules.CardPanel();
            this.picClient = new System.Windows.Forms.PictureBox();
            this.lblPhotoCap = new System.Windows.Forms.Label();
            this.pnlValidation = new System.Windows.Forms.Panel();
            this.lblValidation = new System.Windows.Forms.Label();
            this.pnlValBar = new System.Windows.Forms.Panel();
            this.flowActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCreate = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.cardRecent = new CROMS.Modules.CardPanel();
            this.tblRecent = new System.Windows.Forms.TableLayoutPanel();
            this.tblRecentHead = new System.Windows.Forms.TableLayoutPanel();
            this.lblRecent = new System.Windows.Forms.Label();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.dgvReq = new System.Windows.Forms.DataGridView();
            this.root.SuspendLayout();
            this.tblHeader.SuspendLayout();
            this.tblBody.SuspendLayout();
            this.tblLeft.SuspendLayout();
            this.cardClient.SuspendLayout();
            this.tblClient.SuspendLayout();
            this.flowHead1.SuspendLayout();
            this.cardCertificate.SuspendLayout();
            this.tblCert.SuspendLayout();
            this.flowHead2.SuspendLayout();
            this.cardPurpose.SuspendLayout();
            this.tblPurpose.SuspendLayout();
            this.flowHead3.SuspendLayout();
            this.tblRight.SuspendLayout();
            this.cardSummary.SuspendLayout();
            this.tblSummary.SuspendLayout();
            this.pnlNextStep.SuspendLayout();
            this.pnlNextBody.SuspendLayout();
            this.cardPhoto.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picClient)).BeginInit();
            this.pnlValidation.SuspendLayout();
            this.flowActions.SuspendLayout();
            this.cardRecent.SuspendLayout();
            this.tblRecent.SuspendLayout();
            this.tblRecentHead.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReq)).BeginInit();
            this.SuspendLayout();
            // 
            // root
            // 
            this.root.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.root.ColumnCount = 1;
            this.root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.Controls.Add(this.tblHeader, 0, 0);
            this.root.Controls.Add(this.tblBody, 0, 1);
            this.root.Controls.Add(this.pnlValidation, 0, 2);
            this.root.Controls.Add(this.flowActions, 0, 3);
            this.root.Controls.Add(this.cardRecent, 0, 4);
            this.root.Dock = System.Windows.Forms.DockStyle.Fill;
            this.root.Location = new System.Drawing.Point(0, 0);
            this.root.Name = "root";
            this.root.Padding = new System.Windows.Forms.Padding(24, 20, 24, 20);
            this.root.RowCount = 5;
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 570F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.Size = new System.Drawing.Size(1466, 950);
            this.root.TabIndex = 0;
            // 
            // tblHeader
            // 
            this.tblHeader.AutoSize = true;
            this.tblHeader.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.tblHeader.ColumnCount = 2;
            this.tblHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tblHeader.Controls.Add(this.lblTitle, 0, 0);
            this.tblHeader.Controls.Add(this.lblSubtitle, 0, 1);
            this.tblHeader.Controls.Add(this.pillQueueRef, 1, 0);
            this.tblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblHeader.Location = new System.Drawing.Point(24, 20);
            this.tblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.tblHeader.Name = "tblHeader";
            this.tblHeader.RowCount = 2;
            this.tblHeader.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblHeader.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblHeader.Size = new System.Drawing.Size(1418, 56);
            this.tblHeader.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(262, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Certificate Request";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblSubtitle.Location = new System.Drawing.Point(2, 39);
            this.lblSubtitle.Margin = new System.Windows.Forms.Padding(2, 2, 0, 0);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(322, 17);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Create a certified true copy or a negative certification.";
            // 
            // pillQueueRef
            // 
            this.pillQueueRef.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.pillQueueRef.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(241)))), ((int)(((byte)(254)))));
            this.pillQueueRef.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(207)))), ((int)(((byte)(219)))), ((int)(((byte)(249)))));
            this.pillQueueRef.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.pillQueueRef.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.pillQueueRef.Inset = new System.Windows.Forms.Padding(11, 5, 11, 5);
            this.pillQueueRef.Location = new System.Drawing.Point(1383, 23);
            this.pillQueueRef.Margin = new System.Windows.Forms.Padding(20, 0, 0, 0);
            this.pillQueueRef.Name = "pillQueueRef";
            this.tblHeader.SetRowSpan(this.pillQueueRef, 2);
            this.pillQueueRef.ShowDot = true;
            this.pillQueueRef.Size = new System.Drawing.Size(35, 10);
            this.pillQueueRef.TabIndex = 2;
            this.pillQueueRef.Visible = false;
            // 
            // tblBody
            // 
            this.tblBody.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.tblBody.ColumnCount = 2;
            this.tblBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 64F));
            this.tblBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36F));
            this.tblBody.Controls.Add(this.tblLeft, 0, 0);
            this.tblBody.Controls.Add(this.tblRight, 1, 0);
            this.tblBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblBody.Location = new System.Drawing.Point(24, 94);
            this.tblBody.Margin = new System.Windows.Forms.Padding(0, 18, 0, 0);
            this.tblBody.Name = "tblBody";
            this.tblBody.RowCount = 1;
            this.tblBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblBody.Size = new System.Drawing.Size(1418, 552);
            this.tblBody.TabIndex = 1;
            // 
            // tblLeft
            // 
            this.tblLeft.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.tblLeft.ColumnCount = 1;
            this.tblLeft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblLeft.Controls.Add(this.cardClient, 0, 0);
            this.tblLeft.Controls.Add(this.cardCertificate, 0, 1);
            this.tblLeft.Controls.Add(this.cardPurpose, 0, 2);
            this.tblLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblLeft.Location = new System.Drawing.Point(0, 0);
            this.tblLeft.Margin = new System.Windows.Forms.Padding(0);
            this.tblLeft.Name = "tblLeft";
            this.tblLeft.RowCount = 4;
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 168F));
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 224F));
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblLeft.Size = new System.Drawing.Size(907, 552);
            this.tblLeft.TabIndex = 0;
            // 
            // cardClient
            // 
            this.cardClient.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.cardClient.CardColor = System.Drawing.Color.White;
            this.cardClient.Controls.Add(this.tblClient);
            this.cardClient.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardClient.DrawShadow = true;
            this.cardClient.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.cardClient.Location = new System.Drawing.Point(0, 0);
            this.cardClient.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
            this.cardClient.Name = "cardClient";
            this.cardClient.Radius = 10;
            this.cardClient.Size = new System.Drawing.Size(907, 154);
            this.cardClient.TabIndex = 0;
            // 
            // tblClient
            // 
            this.tblClient.BackColor = System.Drawing.Color.Transparent;
            this.tblClient.ColumnCount = 3;
            this.tblClient.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.tblClient.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.tblClient.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.tblClient.Controls.Add(this.flowHead1, 0, 0);
            this.tblClient.Controls.Add(this.lblClientDesc, 0, 1);
            this.tblClient.Controls.Add(this.lblFirst, 0, 2);
            this.tblClient.Controls.Add(this.lblMiddle, 1, 2);
            this.tblClient.Controls.Add(this.lblLast, 2, 2);
            this.tblClient.Controls.Add(this.txtFirst, 0, 3);
            this.tblClient.Controls.Add(this.txtMiddle, 1, 3);
            this.tblClient.Controls.Add(this.txtLast, 2, 3);
            this.tblClient.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblClient.Location = new System.Drawing.Point(0, 0);
            this.tblClient.Name = "tblClient";
            this.tblClient.Padding = new System.Windows.Forms.Padding(20, 16, 20, 18);
            this.tblClient.RowCount = 4;
            this.tblClient.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblClient.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblClient.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblClient.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblClient.Size = new System.Drawing.Size(907, 154);
            this.tblClient.TabIndex = 0;
            // 
            // flowHead1
            // 
            this.flowHead1.AutoSize = true;
            this.flowHead1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowHead1.BackColor = System.Drawing.Color.Transparent;
            this.tblClient.SetColumnSpan(this.flowHead1, 3);
            this.flowHead1.Controls.Add(this.pillStep1);
            this.flowHead1.Controls.Add(this.lblClientTitle);
            this.flowHead1.Location = new System.Drawing.Point(20, 16);
            this.flowHead1.Margin = new System.Windows.Forms.Padding(0);
            this.flowHead1.Name = "flowHead1";
            this.flowHead1.Size = new System.Drawing.Size(100, 27);
            this.flowHead1.TabIndex = 0;
            this.flowHead1.WrapContents = false;
            // 
            // pillStep1
            // 
            this.pillStep1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.pillStep1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(58)))), ((int)(((byte)(101)))), ((int)(((byte)(221)))));
            this.pillStep1.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.pillStep1.ForeColor = System.Drawing.Color.White;
            this.pillStep1.Inset = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.pillStep1.Location = new System.Drawing.Point(0, 0);
            this.pillStep1.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.pillStep1.Name = "pillStep1";
            this.pillStep1.ShowDot = false;
            this.pillStep1.Size = new System.Drawing.Size(35, 27);
            this.pillStep1.TabIndex = 0;
            this.pillStep1.Text = "1";
            // 
            // lblClientTitle
            // 
            this.lblClientTitle.AutoSize = true;
            this.lblClientTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblClientTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblClientTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblClientTitle.Location = new System.Drawing.Point(45, 3);
            this.lblClientTitle.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.lblClientTitle.Name = "lblClientTitle";
            this.lblClientTitle.Size = new System.Drawing.Size(55, 21);
            this.lblClientTitle.TabIndex = 1;
            this.lblClientTitle.Text = "Client";
            // 
            // lblClientDesc
            // 
            this.lblClientDesc.AutoSize = true;
            this.lblClientDesc.BackColor = System.Drawing.Color.Transparent;
            this.tblClient.SetColumnSpan(this.lblClientDesc, 3);
            this.lblClientDesc.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblClientDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblClientDesc.Location = new System.Drawing.Point(20, 47);
            this.lblClientDesc.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.lblClientDesc.Name = "lblClientDesc";
            this.lblClientDesc.Size = new System.Drawing.Size(336, 15);
            this.lblClientDesc.TabIndex = 1;
            this.lblClientDesc.Text = "Enter the name of the client as it appears in the registry record.";
            // 
            // lblFirst
            // 
            this.lblFirst.AutoSize = true;
            this.lblFirst.BackColor = System.Drawing.Color.Transparent;
            this.lblFirst.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.lblFirst.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblFirst.Location = new System.Drawing.Point(20, 78);
            this.lblFirst.Margin = new System.Windows.Forms.Padding(0, 16, 16, 5);
            this.lblFirst.Name = "lblFirst";
            this.lblFirst.Size = new System.Drawing.Size(70, 15);
            this.lblFirst.TabIndex = 2;
            this.lblFirst.Text = "First name *";
            // 
            // lblMiddle
            // 
            this.lblMiddle.AutoSize = true;
            this.lblMiddle.BackColor = System.Drawing.Color.Transparent;
            this.lblMiddle.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.lblMiddle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblMiddle.Location = new System.Drawing.Point(308, 78);
            this.lblMiddle.Margin = new System.Windows.Forms.Padding(0, 16, 16, 5);
            this.lblMiddle.Name = "lblMiddle";
            this.lblMiddle.Size = new System.Drawing.Size(77, 15);
            this.lblMiddle.TabIndex = 3;
            this.lblMiddle.Text = "Middle name";
            // 
            // lblLast
            // 
            this.lblLast.AutoSize = true;
            this.lblLast.BackColor = System.Drawing.Color.Transparent;
            this.lblLast.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.lblLast.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblLast.Location = new System.Drawing.Point(596, 78);
            this.lblLast.Margin = new System.Windows.Forms.Padding(0, 16, 0, 5);
            this.lblLast.Name = "lblLast";
            this.lblLast.Size = new System.Drawing.Size(69, 15);
            this.lblLast.TabIndex = 4;
            this.lblLast.Text = "Last name *";
            // 
            // txtFirst
            // 
            this.txtFirst.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFirst.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtFirst.Location = new System.Drawing.Point(20, 104);
            this.txtFirst.Margin = new System.Windows.Forms.Padding(0, 0, 16, 0);
            this.txtFirst.Name = "txtFirst";
            this.txtFirst.Size = new System.Drawing.Size(272, 25);
            this.txtFirst.TabIndex = 0;
            // 
            // txtMiddle
            // 
            this.txtMiddle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMiddle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtMiddle.Location = new System.Drawing.Point(308, 104);
            this.txtMiddle.Margin = new System.Windows.Forms.Padding(0, 0, 16, 0);
            this.txtMiddle.Name = "txtMiddle";
            this.txtMiddle.Size = new System.Drawing.Size(272, 25);
            this.txtMiddle.TabIndex = 1;
            // 
            // txtLast
            // 
            this.txtLast.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLast.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtLast.Location = new System.Drawing.Point(596, 104);
            this.txtLast.Margin = new System.Windows.Forms.Padding(0);
            this.txtLast.Name = "txtLast";
            this.txtLast.Size = new System.Drawing.Size(291, 25);
            this.txtLast.TabIndex = 2;
            // 
            // cardCertificate
            // 
            this.cardCertificate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.cardCertificate.CardColor = System.Drawing.Color.White;
            this.cardCertificate.Controls.Add(this.tblCert);
            this.cardCertificate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardCertificate.DrawShadow = true;
            this.cardCertificate.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.cardCertificate.Location = new System.Drawing.Point(0, 168);
            this.cardCertificate.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
            this.cardCertificate.Name = "cardCertificate";
            this.cardCertificate.Radius = 10;
            this.cardCertificate.Size = new System.Drawing.Size(907, 210);
            this.cardCertificate.TabIndex = 1;
            // 
            // tblCert
            // 
            this.tblCert.BackColor = System.Drawing.Color.Transparent;
            this.tblCert.ColumnCount = 2;
            this.tblCert.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblCert.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblCert.Controls.Add(this.flowHead2, 0, 0);
            this.tblCert.Controls.Add(this.lblCertDesc, 0, 1);
            this.tblCert.Controls.Add(this.lblCertType, 0, 2);
            this.tblCert.Controls.Add(this.lblRecordType, 1, 2);
            this.tblCert.Controls.Add(this.cboCertType, 0, 3);
            this.tblCert.Controls.Add(this.cboRecordType, 1, 3);
            this.tblCert.Controls.Add(this.lblSelRecord, 0, 4);
            this.tblCert.Controls.Add(this.cboRecord, 0, 5);
            this.tblCert.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblCert.Location = new System.Drawing.Point(0, 0);
            this.tblCert.Name = "tblCert";
            this.tblCert.Padding = new System.Windows.Forms.Padding(20, 16, 20, 18);
            this.tblCert.RowCount = 6;
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblCert.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblCert.Size = new System.Drawing.Size(907, 210);
            this.tblCert.TabIndex = 0;
            // 
            // flowHead2
            // 
            this.flowHead2.AutoSize = true;
            this.flowHead2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowHead2.BackColor = System.Drawing.Color.Transparent;
            this.tblCert.SetColumnSpan(this.flowHead2, 2);
            this.flowHead2.Controls.Add(this.pillStep2);
            this.flowHead2.Controls.Add(this.lblCertTitle);
            this.flowHead2.Location = new System.Drawing.Point(20, 16);
            this.flowHead2.Margin = new System.Windows.Forms.Padding(0);
            this.flowHead2.Name = "flowHead2";
            this.flowHead2.Size = new System.Drawing.Size(134, 27);
            this.flowHead2.TabIndex = 0;
            this.flowHead2.WrapContents = false;
            // 
            // pillStep2
            // 
            this.pillStep2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.pillStep2.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(58)))), ((int)(((byte)(101)))), ((int)(((byte)(221)))));
            this.pillStep2.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.pillStep2.ForeColor = System.Drawing.Color.White;
            this.pillStep2.Inset = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.pillStep2.Location = new System.Drawing.Point(0, 0);
            this.pillStep2.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.pillStep2.Name = "pillStep2";
            this.pillStep2.ShowDot = false;
            this.pillStep2.Size = new System.Drawing.Size(35, 27);
            this.pillStep2.TabIndex = 0;
            this.pillStep2.Text = "2";
            // 
            // lblCertTitle
            // 
            this.lblCertTitle.AutoSize = true;
            this.lblCertTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblCertTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblCertTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblCertTitle.Location = new System.Drawing.Point(45, 3);
            this.lblCertTitle.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.lblCertTitle.Name = "lblCertTitle";
            this.lblCertTitle.Size = new System.Drawing.Size(89, 21);
            this.lblCertTitle.TabIndex = 1;
            this.lblCertTitle.Text = "Certificate";
            // 
            // lblCertDesc
            // 
            this.lblCertDesc.AutoSize = true;
            this.lblCertDesc.BackColor = System.Drawing.Color.Transparent;
            this.tblCert.SetColumnSpan(this.lblCertDesc, 2);
            this.lblCertDesc.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblCertDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCertDesc.Location = new System.Drawing.Point(20, 47);
            this.lblCertDesc.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.lblCertDesc.Name = "lblCertDesc";
            this.lblCertDesc.Size = new System.Drawing.Size(363, 15);
            this.lblCertDesc.TabIndex = 1;
            this.lblCertDesc.Text = "Choose the certificate type and select the applicable registry record.";
            // 
            // lblCertType
            // 
            this.lblCertType.AutoSize = true;
            this.lblCertType.BackColor = System.Drawing.Color.Transparent;
            this.lblCertType.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.lblCertType.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCertType.Location = new System.Drawing.Point(20, 78);
            this.lblCertType.Margin = new System.Windows.Forms.Padding(0, 16, 16, 5);
            this.lblCertType.Name = "lblCertType";
            this.lblCertType.Size = new System.Drawing.Size(95, 15);
            this.lblCertType.TabIndex = 2;
            this.lblCertType.Text = "Certificate type *";
            // 
            // lblRecordType
            // 
            this.lblRecordType.AutoSize = true;
            this.lblRecordType.BackColor = System.Drawing.Color.Transparent;
            this.lblRecordType.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.lblRecordType.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblRecordType.Location = new System.Drawing.Point(453, 78);
            this.lblRecordType.Margin = new System.Windows.Forms.Padding(0, 16, 0, 5);
            this.lblRecordType.Name = "lblRecordType";
            this.lblRecordType.Size = new System.Drawing.Size(70, 15);
            this.lblRecordType.TabIndex = 3;
            this.lblRecordType.Text = "Record type";
            // 
            // cboCertType
            // 
            this.cboCertType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboCertType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCertType.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboCertType.Location = new System.Drawing.Point(20, 98);
            this.cboCertType.Margin = new System.Windows.Forms.Padding(0, 0, 16, 0);
            this.cboCertType.Name = "cboCertType";
            this.cboCertType.Size = new System.Drawing.Size(417, 25);
            this.cboCertType.TabIndex = 3;
            // 
            // cboRecordType
            // 
            this.cboRecordType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboRecordType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboRecordType.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboRecordType.Location = new System.Drawing.Point(453, 98);
            this.cboRecordType.Margin = new System.Windows.Forms.Padding(0);
            this.cboRecordType.Name = "cboRecordType";
            this.cboRecordType.Size = new System.Drawing.Size(434, 25);
            this.cboRecordType.TabIndex = 4;
            // 
            // lblSelRecord
            // 
            this.lblSelRecord.AutoSize = true;
            this.lblSelRecord.BackColor = System.Drawing.Color.Transparent;
            this.tblCert.SetColumnSpan(this.lblSelRecord, 2);
            this.lblSelRecord.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.lblSelRecord.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblSelRecord.Location = new System.Drawing.Point(20, 137);
            this.lblSelRecord.Margin = new System.Windows.Forms.Padding(0, 14, 0, 5);
            this.lblSelRecord.Name = "lblSelRecord";
            this.lblSelRecord.Size = new System.Drawing.Size(205, 15);
            this.lblSelRecord.TabIndex = 5;
            this.lblSelRecord.Text = "Select registry record  (type to search)";
            // 
            // cboRecord
            // 
            this.cboRecord.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboRecord.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cboRecord.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.tblCert.SetColumnSpan(this.cboRecord, 2);
            this.cboRecord.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cboRecord.Location = new System.Drawing.Point(20, 162);
            this.cboRecord.Margin = new System.Windows.Forms.Padding(0);
            this.cboRecord.Name = "cboRecord";
            this.cboRecord.Size = new System.Drawing.Size(867, 25);
            this.cboRecord.TabIndex = 5;
            // 
            // cardPurpose
            // 
            this.cardPurpose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.cardPurpose.CardColor = System.Drawing.Color.White;
            this.cardPurpose.Controls.Add(this.tblPurpose);
            this.cardPurpose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardPurpose.DrawShadow = true;
            this.cardPurpose.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.cardPurpose.Location = new System.Drawing.Point(0, 392);
            this.cardPurpose.Margin = new System.Windows.Forms.Padding(0);
            this.cardPurpose.Name = "cardPurpose";
            this.cardPurpose.Radius = 10;
            this.cardPurpose.Size = new System.Drawing.Size(907, 178);
            this.cardPurpose.TabIndex = 2;
            // 
            // tblPurpose
            // 
            this.tblPurpose.BackColor = System.Drawing.Color.Transparent;
            this.tblPurpose.ColumnCount = 2;
            this.tblPurpose.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tblPurpose.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblPurpose.Controls.Add(this.flowHead3, 0, 0);
            this.tblPurpose.Controls.Add(this.lblPurposeDesc, 0, 1);
            this.tblPurpose.Controls.Add(this.lblCopies, 0, 2);
            this.tblPurpose.Controls.Add(this.lblPurpose, 1, 2);
            this.tblPurpose.Controls.Add(this.txtCopies, 0, 3);
            this.tblPurpose.Controls.Add(this.txtPurpose, 1, 3);
            this.tblPurpose.Controls.Add(this.lblPurposeHint, 1, 4);
            this.tblPurpose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblPurpose.Location = new System.Drawing.Point(0, 0);
            this.tblPurpose.Name = "tblPurpose";
            this.tblPurpose.Padding = new System.Windows.Forms.Padding(20, 16, 20, 18);
            this.tblPurpose.RowCount = 5;
            this.tblPurpose.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblPurpose.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblPurpose.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblPurpose.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblPurpose.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblPurpose.Size = new System.Drawing.Size(907, 178);
            this.tblPurpose.TabIndex = 0;
            // 
            // flowHead3
            // 
            this.flowHead3.AutoSize = true;
            this.flowHead3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowHead3.BackColor = System.Drawing.Color.Transparent;
            this.tblPurpose.SetColumnSpan(this.flowHead3, 2);
            this.flowHead3.Controls.Add(this.pillStep3);
            this.flowHead3.Controls.Add(this.lblPurposeTitle);
            this.flowHead3.Location = new System.Drawing.Point(20, 16);
            this.flowHead3.Margin = new System.Windows.Forms.Padding(0);
            this.flowHead3.Name = "flowHead3";
            this.flowHead3.Size = new System.Drawing.Size(203, 27);
            this.flowHead3.TabIndex = 0;
            this.flowHead3.WrapContents = false;
            // 
            // pillStep3
            // 
            this.pillStep3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.pillStep3.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(58)))), ((int)(((byte)(101)))), ((int)(((byte)(221)))));
            this.pillStep3.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.pillStep3.ForeColor = System.Drawing.Color.White;
            this.pillStep3.Inset = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.pillStep3.Location = new System.Drawing.Point(0, 0);
            this.pillStep3.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.pillStep3.Name = "pillStep3";
            this.pillStep3.ShowDot = false;
            this.pillStep3.Size = new System.Drawing.Size(35, 27);
            this.pillStep3.TabIndex = 0;
            this.pillStep3.Text = "3";
            // 
            // lblPurposeTitle
            // 
            this.lblPurposeTitle.AutoSize = true;
            this.lblPurposeTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblPurposeTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblPurposeTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblPurposeTitle.Location = new System.Drawing.Point(45, 3);
            this.lblPurposeTitle.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.lblPurposeTitle.Name = "lblPurposeTitle";
            this.lblPurposeTitle.Size = new System.Drawing.Size(158, 21);
            this.lblPurposeTitle.TabIndex = 1;
            this.lblPurposeTitle.Text = "Purpose and copies";
            // 
            // lblPurposeDesc
            // 
            this.lblPurposeDesc.AutoSize = true;
            this.lblPurposeDesc.BackColor = System.Drawing.Color.Transparent;
            this.tblPurpose.SetColumnSpan(this.lblPurposeDesc, 2);
            this.lblPurposeDesc.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblPurposeDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblPurposeDesc.Location = new System.Drawing.Point(20, 47);
            this.lblPurposeDesc.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.lblPurposeDesc.Name = "lblPurposeDesc";
            this.lblPurposeDesc.Size = new System.Drawing.Size(257, 15);
            this.lblPurposeDesc.TabIndex = 1;
            this.lblPurposeDesc.Text = "Indicate the purpose and the number of copies.";
            // 
            // lblCopies
            // 
            this.lblCopies.AutoSize = true;
            this.lblCopies.BackColor = System.Drawing.Color.Transparent;
            this.lblCopies.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.lblCopies.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCopies.Location = new System.Drawing.Point(20, 78);
            this.lblCopies.Margin = new System.Windows.Forms.Padding(0, 16, 16, 5);
            this.lblCopies.Name = "lblCopies";
            this.lblCopies.Size = new System.Drawing.Size(102, 15);
            this.lblCopies.TabIndex = 2;
            this.lblCopies.Text = "Number of copies";
            // 
            // lblPurpose
            // 
            this.lblPurpose.AutoSize = true;
            this.lblPurpose.BackColor = System.Drawing.Color.Transparent;
            this.lblPurpose.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.lblPurpose.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblPurpose.Location = new System.Drawing.Point(170, 78);
            this.lblPurpose.Margin = new System.Windows.Forms.Padding(0, 16, 0, 5);
            this.lblPurpose.Name = "lblPurpose";
            this.lblPurpose.Size = new System.Drawing.Size(50, 15);
            this.lblPurpose.TabIndex = 3;
            this.lblPurpose.Text = "Purpose";
            // 
            // txtCopies
            // 
            this.txtCopies.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCopies.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtCopies.Location = new System.Drawing.Point(20, 98);
            this.txtCopies.Margin = new System.Windows.Forms.Padding(0, 0, 16, 0);
            this.txtCopies.Name = "txtCopies";
            this.txtCopies.Size = new System.Drawing.Size(134, 25);
            this.txtCopies.TabIndex = 6;
            this.txtCopies.Text = "1";
            // 
            // txtPurpose
            // 
            this.txtPurpose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPurpose.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtPurpose.Location = new System.Drawing.Point(170, 98);
            this.txtPurpose.Margin = new System.Windows.Forms.Padding(0);
            this.txtPurpose.Name = "txtPurpose";
            this.txtPurpose.Size = new System.Drawing.Size(717, 25);
            this.txtPurpose.TabIndex = 7;
            // 
            // lblPurposeHint
            // 
            this.lblPurposeHint.AutoSize = true;
            this.lblPurposeHint.BackColor = System.Drawing.Color.Transparent;
            this.lblPurposeHint.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblPurposeHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(137)))), ((int)(((byte)(145)))), ((int)(((byte)(163)))));
            this.lblPurposeHint.Location = new System.Drawing.Point(170, 128);
            this.lblPurposeHint.Margin = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.lblPurposeHint.Name = "lblPurposeHint";
            this.lblPurposeHint.Size = new System.Drawing.Size(347, 13);
            this.lblPurposeHint.TabIndex = 8;
            this.lblPurposeHint.Text = "Examples: School requirement, Local employment, Travel, Legal use";
            // 
            // tblRight
            // 
            this.tblRight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.tblRight.ColumnCount = 1;
            this.tblRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblRight.Controls.Add(this.cardSummary, 0, 0);
            this.tblRight.Controls.Add(this.cardPhoto, 0, 1);
            this.tblRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblRight.Location = new System.Drawing.Point(927, 0);
            this.tblRight.Margin = new System.Windows.Forms.Padding(20, 0, 0, 0);
            this.tblRight.Name = "tblRight";
            this.tblRight.RowCount = 2;
            this.tblRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 423F));
            this.tblRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblRight.Size = new System.Drawing.Size(491, 552);
            this.tblRight.TabIndex = 1;
            // 
            // cardSummary
            // 
            this.cardSummary.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.cardSummary.CardColor = System.Drawing.Color.White;
            this.cardSummary.Controls.Add(this.tblSummary);
            this.cardSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardSummary.DrawShadow = true;
            this.cardSummary.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.cardSummary.Location = new System.Drawing.Point(0, 0);
            this.cardSummary.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
            this.cardSummary.Name = "cardSummary";
            this.cardSummary.Radius = 10;
            this.cardSummary.Size = new System.Drawing.Size(491, 409);
            this.cardSummary.TabIndex = 0;
            // 
            // tblSummary
            // 
            this.tblSummary.BackColor = System.Drawing.Color.Transparent;
            this.tblSummary.ColumnCount = 2;
            this.tblSummary.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 46F));
            this.tblSummary.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 54F));
            this.tblSummary.Controls.Add(this.lblSummaryTitle, 0, 0);
            this.tblSummary.Controls.Add(this.lblSummarySub, 0, 1);
            this.tblSummary.Controls.Add(this.lblCapClient, 0, 2);
            this.tblSummary.Controls.Add(this.lblSumClient, 1, 2);
            this.tblSummary.Controls.Add(this.lblCapCertType, 0, 3);
            this.tblSummary.Controls.Add(this.lblSumCertType, 1, 3);
            this.tblSummary.Controls.Add(this.lblCapRecordType, 0, 4);
            this.tblSummary.Controls.Add(this.lblSumRecordType, 1, 4);
            this.tblSummary.Controls.Add(this.lblCapRecord, 0, 5);
            this.tblSummary.Controls.Add(this.lblSumRecord, 1, 5);
            this.tblSummary.Controls.Add(this.lblCapCopies, 0, 6);
            this.tblSummary.Controls.Add(this.lblSumCopies, 1, 6);
            this.tblSummary.Controls.Add(this.lblCapPurpose, 0, 7);
            this.tblSummary.Controls.Add(this.lblSumPurpose, 1, 7);
            this.tblSummary.Controls.Add(this.pnlDivider, 0, 8);
            this.tblSummary.Controls.Add(this.pnlNextStep, 0, 9);
            this.tblSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblSummary.Location = new System.Drawing.Point(0, 0);
            this.tblSummary.Name = "tblSummary";
            this.tblSummary.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
            this.tblSummary.RowCount = 10;
            this.tblSummary.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblSummary.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblSummary.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblSummary.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblSummary.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblSummary.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblSummary.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblSummary.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblSummary.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblSummary.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblSummary.Size = new System.Drawing.Size(491, 409);
            this.tblSummary.TabIndex = 0;
            // 
            // lblSummaryTitle
            // 
            this.lblSummaryTitle.AutoSize = true;
            this.lblSummaryTitle.BackColor = System.Drawing.Color.Transparent;
            this.tblSummary.SetColumnSpan(this.lblSummaryTitle, 2);
            this.lblSummaryTitle.Font = new System.Drawing.Font("Segoe UI", 12.5F, System.Drawing.FontStyle.Bold);
            this.lblSummaryTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblSummaryTitle.Location = new System.Drawing.Point(20, 18);
            this.lblSummaryTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblSummaryTitle.Name = "lblSummaryTitle";
            this.lblSummaryTitle.Size = new System.Drawing.Size(154, 23);
            this.lblSummaryTitle.TabIndex = 0;
            this.lblSummaryTitle.Text = "Request summary";
            // 
            // lblSummarySub
            // 
            this.lblSummarySub.AutoSize = true;
            this.lblSummarySub.BackColor = System.Drawing.Color.Transparent;
            this.tblSummary.SetColumnSpan(this.lblSummarySub, 2);
            this.lblSummarySub.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblSummarySub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblSummarySub.Location = new System.Drawing.Point(20, 45);
            this.lblSummarySub.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.lblSummarySub.Name = "lblSummarySub";
            this.lblSummarySub.Size = new System.Drawing.Size(257, 15);
            this.lblSummarySub.TabIndex = 1;
            this.lblSummarySub.Text = "Review your entries before creating the request.";
            // 
            // lblCapClient
            // 
            this.lblCapClient.AutoSize = true;
            this.lblCapClient.BackColor = System.Drawing.Color.Transparent;
            this.lblCapClient.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCapClient.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCapClient.Location = new System.Drawing.Point(20, 78);
            this.lblCapClient.Margin = new System.Windows.Forms.Padding(0, 18, 0, 7);
            this.lblCapClient.Name = "lblCapClient";
            this.lblCapClient.Size = new System.Drawing.Size(71, 15);
            this.lblCapClient.TabIndex = 2;
            this.lblCapClient.Text = "Client name";
            // 
            // lblSumClient
            // 
            this.lblSumClient.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSumClient.AutoEllipsis = true;
            this.lblSumClient.BackColor = System.Drawing.Color.Transparent;
            this.lblSumClient.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblSumClient.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(137)))), ((int)(((byte)(145)))), ((int)(((byte)(163)))));
            this.lblSumClient.Location = new System.Drawing.Point(227, 76);
            this.lblSumClient.Margin = new System.Windows.Forms.Padding(0, 15, 0, 5);
            this.lblSumClient.Name = "lblSumClient";
            this.lblSumClient.Size = new System.Drawing.Size(244, 18);
            this.lblSumClient.TabIndex = 3;
            this.lblSumClient.Text = "—";
            this.lblSumClient.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCapCertType
            // 
            this.lblCapCertType.AutoSize = true;
            this.lblCapCertType.BackColor = System.Drawing.Color.Transparent;
            this.lblCapCertType.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCapCertType.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCapCertType.Location = new System.Drawing.Point(20, 108);
            this.lblCapCertType.Margin = new System.Windows.Forms.Padding(0, 8, 0, 7);
            this.lblCapCertType.Name = "lblCapCertType";
            this.lblCapCertType.Size = new System.Drawing.Size(87, 15);
            this.lblCapCertType.TabIndex = 4;
            this.lblCapCertType.Text = "Certificate type";
            // 
            // lblSumCertType
            // 
            this.lblSumCertType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSumCertType.AutoEllipsis = true;
            this.lblSumCertType.BackColor = System.Drawing.Color.Transparent;
            this.lblSumCertType.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblSumCertType.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(137)))), ((int)(((byte)(145)))), ((int)(((byte)(163)))));
            this.lblSumCertType.Location = new System.Drawing.Point(227, 106);
            this.lblSumCertType.Margin = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.lblSumCertType.Name = "lblSumCertType";
            this.lblSumCertType.Size = new System.Drawing.Size(244, 18);
            this.lblSumCertType.TabIndex = 5;
            this.lblSumCertType.Text = "—";
            this.lblSumCertType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCapRecordType
            // 
            this.lblCapRecordType.AutoSize = true;
            this.lblCapRecordType.BackColor = System.Drawing.Color.Transparent;
            this.lblCapRecordType.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCapRecordType.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCapRecordType.Location = new System.Drawing.Point(20, 138);
            this.lblCapRecordType.Margin = new System.Windows.Forms.Padding(0, 8, 0, 7);
            this.lblCapRecordType.Name = "lblCapRecordType";
            this.lblCapRecordType.Size = new System.Drawing.Size(70, 15);
            this.lblCapRecordType.TabIndex = 6;
            this.lblCapRecordType.Text = "Record type";
            // 
            // lblSumRecordType
            // 
            this.lblSumRecordType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSumRecordType.AutoEllipsis = true;
            this.lblSumRecordType.BackColor = System.Drawing.Color.Transparent;
            this.lblSumRecordType.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblSumRecordType.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(137)))), ((int)(((byte)(145)))), ((int)(((byte)(163)))));
            this.lblSumRecordType.Location = new System.Drawing.Point(227, 136);
            this.lblSumRecordType.Margin = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.lblSumRecordType.Name = "lblSumRecordType";
            this.lblSumRecordType.Size = new System.Drawing.Size(244, 18);
            this.lblSumRecordType.TabIndex = 7;
            this.lblSumRecordType.Text = "—";
            this.lblSumRecordType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCapRecord
            // 
            this.lblCapRecord.AutoSize = true;
            this.lblCapRecord.BackColor = System.Drawing.Color.Transparent;
            this.lblCapRecord.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCapRecord.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCapRecord.Location = new System.Drawing.Point(20, 168);
            this.lblCapRecord.Margin = new System.Windows.Forms.Padding(0, 8, 0, 7);
            this.lblCapRecord.Name = "lblCapRecord";
            this.lblCapRecord.Size = new System.Drawing.Size(44, 15);
            this.lblCapRecord.TabIndex = 8;
            this.lblCapRecord.Text = "Record";
            // 
            // lblSumRecord
            // 
            this.lblSumRecord.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSumRecord.AutoEllipsis = true;
            this.lblSumRecord.BackColor = System.Drawing.Color.Transparent;
            this.lblSumRecord.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblSumRecord.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(137)))), ((int)(((byte)(145)))), ((int)(((byte)(163)))));
            this.lblSumRecord.Location = new System.Drawing.Point(227, 166);
            this.lblSumRecord.Margin = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.lblSumRecord.Name = "lblSumRecord";
            this.lblSumRecord.Size = new System.Drawing.Size(244, 18);
            this.lblSumRecord.TabIndex = 9;
            this.lblSumRecord.Text = "—";
            this.lblSumRecord.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCapCopies
            // 
            this.lblCapCopies.AutoSize = true;
            this.lblCapCopies.BackColor = System.Drawing.Color.Transparent;
            this.lblCapCopies.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCapCopies.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCapCopies.Location = new System.Drawing.Point(20, 198);
            this.lblCapCopies.Margin = new System.Windows.Forms.Padding(0, 8, 0, 7);
            this.lblCapCopies.Name = "lblCapCopies";
            this.lblCapCopies.Size = new System.Drawing.Size(102, 15);
            this.lblCapCopies.TabIndex = 10;
            this.lblCapCopies.Text = "Number of copies";
            // 
            // lblSumCopies
            // 
            this.lblSumCopies.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSumCopies.AutoEllipsis = true;
            this.lblSumCopies.BackColor = System.Drawing.Color.Transparent;
            this.lblSumCopies.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblSumCopies.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(137)))), ((int)(((byte)(145)))), ((int)(((byte)(163)))));
            this.lblSumCopies.Location = new System.Drawing.Point(227, 196);
            this.lblSumCopies.Margin = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.lblSumCopies.Name = "lblSumCopies";
            this.lblSumCopies.Size = new System.Drawing.Size(244, 18);
            this.lblSumCopies.TabIndex = 11;
            this.lblSumCopies.Text = "—";
            this.lblSumCopies.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCapPurpose
            // 
            this.lblCapPurpose.AutoSize = true;
            this.lblCapPurpose.BackColor = System.Drawing.Color.Transparent;
            this.lblCapPurpose.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCapPurpose.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCapPurpose.Location = new System.Drawing.Point(20, 228);
            this.lblCapPurpose.Margin = new System.Windows.Forms.Padding(0, 8, 0, 7);
            this.lblCapPurpose.Name = "lblCapPurpose";
            this.lblCapPurpose.Size = new System.Drawing.Size(50, 15);
            this.lblCapPurpose.TabIndex = 12;
            this.lblCapPurpose.Text = "Purpose";
            // 
            // lblSumPurpose
            // 
            this.lblSumPurpose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSumPurpose.AutoEllipsis = true;
            this.lblSumPurpose.BackColor = System.Drawing.Color.Transparent;
            this.lblSumPurpose.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblSumPurpose.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(137)))), ((int)(((byte)(145)))), ((int)(((byte)(163)))));
            this.lblSumPurpose.Location = new System.Drawing.Point(227, 226);
            this.lblSumPurpose.Margin = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.lblSumPurpose.Name = "lblSumPurpose";
            this.lblSumPurpose.Size = new System.Drawing.Size(244, 18);
            this.lblSumPurpose.TabIndex = 13;
            this.lblSumPurpose.Text = "—";
            this.lblSumPurpose.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // pnlDivider
            // 
            this.pnlDivider.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlDivider.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.tblSummary.SetColumnSpan(this.pnlDivider, 2);
            this.pnlDivider.Location = new System.Drawing.Point(20, 266);
            this.pnlDivider.Margin = new System.Windows.Forms.Padding(0, 16, 0, 0);
            this.pnlDivider.Name = "pnlDivider";
            this.pnlDivider.Size = new System.Drawing.Size(451, 1);
            this.pnlDivider.TabIndex = 14;
            // 
            // pnlNextStep
            // 
            this.pnlNextStep.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlNextStep.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(241)))), ((int)(((byte)(254)))));
            this.tblSummary.SetColumnSpan(this.pnlNextStep, 2);
            this.pnlNextStep.Controls.Add(this.pnlNextBody);
            this.pnlNextStep.Controls.Add(this.pnlNextBar);
            this.pnlNextStep.Location = new System.Drawing.Point(20, 306);
            this.pnlNextStep.Margin = new System.Windows.Forms.Padding(0, 16, 0, 0);
            this.pnlNextStep.Name = "pnlNextStep";
            this.pnlNextStep.Size = new System.Drawing.Size(451, 62);
            this.pnlNextStep.TabIndex = 15;
            // 
            // pnlNextBody
            // 
            this.pnlNextBody.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(241)))), ((int)(((byte)(254)))));
            this.pnlNextBody.Controls.Add(this.lblNextStep);
            this.pnlNextBody.Controls.Add(this.lblNextCap);
            this.pnlNextBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlNextBody.Location = new System.Drawing.Point(4, 0);
            this.pnlNextBody.Name = "pnlNextBody";
            this.pnlNextBody.Padding = new System.Windows.Forms.Padding(12, 9, 12, 9);
            this.pnlNextBody.Size = new System.Drawing.Size(447, 62);
            this.pnlNextBody.TabIndex = 0;
            // 
            // lblNextStep
            // 
            this.lblNextStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNextStep.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            this.lblNextStep.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblNextStep.Location = new System.Drawing.Point(12, 24);
            this.lblNextStep.Name = "lblNextStep";
            this.lblNextStep.Size = new System.Drawing.Size(423, 29);
            this.lblNextStep.TabIndex = 0;
            this.lblNextStep.Text = "Complete all required fields and click Create request.";
            // 
            // lblNextCap
            // 
            this.lblNextCap.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblNextCap.Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Bold);
            this.lblNextCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.lblNextCap.Location = new System.Drawing.Point(12, 9);
            this.lblNextCap.Name = "lblNextCap";
            this.lblNextCap.Size = new System.Drawing.Size(423, 15);
            this.lblNextCap.TabIndex = 1;
            this.lblNextCap.Text = "NEXT STEP";
            // 
            // pnlNextBar
            // 
            this.pnlNextBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.pnlNextBar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlNextBar.Location = new System.Drawing.Point(0, 0);
            this.pnlNextBar.Name = "pnlNextBar";
            this.pnlNextBar.Size = new System.Drawing.Size(4, 62);
            this.pnlNextBar.TabIndex = 1;
            // 
            // cardPhoto
            // 
            this.cardPhoto.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.cardPhoto.CardColor = System.Drawing.Color.White;
            this.cardPhoto.Controls.Add(this.picClient);
            this.cardPhoto.Controls.Add(this.lblPhotoCap);
            this.cardPhoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardPhoto.DrawShadow = true;
            this.cardPhoto.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.cardPhoto.Location = new System.Drawing.Point(0, 423);
            this.cardPhoto.Margin = new System.Windows.Forms.Padding(0);
            this.cardPhoto.Name = "cardPhoto";
            this.cardPhoto.Padding = new System.Windows.Forms.Padding(16, 14, 16, 16);
            this.cardPhoto.Radius = 10;
            this.cardPhoto.Size = new System.Drawing.Size(491, 129);
            this.cardPhoto.TabIndex = 1;
            this.cardPhoto.Visible = false;
            // 
            // picClient
            // 
            this.picClient.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.picClient.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picClient.Location = new System.Drawing.Point(16, 36);
            this.picClient.Name = "picClient";
            this.picClient.Size = new System.Drawing.Size(459, 77);
            this.picClient.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picClient.TabIndex = 0;
            this.picClient.TabStop = false;
            // 
            // lblPhotoCap
            // 
            this.lblPhotoCap.BackColor = System.Drawing.Color.Transparent;
            this.lblPhotoCap.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPhotoCap.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblPhotoCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblPhotoCap.Location = new System.Drawing.Point(16, 14);
            this.lblPhotoCap.Name = "lblPhotoCap";
            this.lblPhotoCap.Size = new System.Drawing.Size(459, 22);
            this.lblPhotoCap.TabIndex = 1;
            this.lblPhotoCap.Text = "Client photo (from kiosk)";
            // 
            // pnlValidation
            // 
            this.pnlValidation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlValidation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(251)))), ((int)(((byte)(234)))), ((int)(((byte)(236)))));
            this.pnlValidation.Controls.Add(this.lblValidation);
            this.pnlValidation.Controls.Add(this.pnlValBar);
            this.pnlValidation.Location = new System.Drawing.Point(24, 664);
            this.pnlValidation.Margin = new System.Windows.Forms.Padding(0, 18, 0, 0);
            this.pnlValidation.Name = "pnlValidation";
            this.pnlValidation.Size = new System.Drawing.Size(1418, 48);
            this.pnlValidation.TabIndex = 2;
            this.pnlValidation.Visible = false;
            // 
            // lblValidation
            // 
            this.lblValidation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblValidation.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblValidation.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(50)))), ((int)(((byte)(63)))));
            this.lblValidation.Location = new System.Drawing.Point(4, 0);
            this.lblValidation.Name = "lblValidation";
            this.lblValidation.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            this.lblValidation.Size = new System.Drawing.Size(1414, 48);
            this.lblValidation.TabIndex = 0;
            this.lblValidation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlValBar
            // 
            this.pnlValBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(50)))), ((int)(((byte)(63)))));
            this.pnlValBar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlValBar.Location = new System.Drawing.Point(0, 0);
            this.pnlValBar.Name = "pnlValBar";
            this.pnlValBar.Size = new System.Drawing.Size(4, 48);
            this.pnlValBar.TabIndex = 1;
            // 
            // flowActions
            // 
            this.flowActions.AutoSize = true;
            this.flowActions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowActions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.flowActions.Controls.Add(this.btnCreate);
            this.flowActions.Controls.Add(this.btnClear);
            this.flowActions.Location = new System.Drawing.Point(24, 730);
            this.flowActions.Margin = new System.Windows.Forms.Padding(0, 18, 0, 0);
            this.flowActions.Name = "flowActions";
            this.flowActions.Size = new System.Drawing.Size(352, 48);
            this.flowActions.TabIndex = 3;
            this.flowActions.WrapContents = false;
            // 
            // btnCreate
            // 
            this.btnCreate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.btnCreate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCreate.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            this.btnCreate.ForeColor = System.Drawing.Color.White;
            this.btnCreate.Location = new System.Drawing.Point(0, 0);
            this.btnCreate.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(190, 48);
            this.btnCreate.TabIndex = 8;
            this.btnCreate.Text = "Create request";
            this.btnCreate.UseVisualStyleBackColor = false;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            // 
            // btnClear
            // 
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnClear.Location = new System.Drawing.Point(202, 0);
            this.btnClear.Margin = new System.Windows.Forms.Padding(0);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(150, 48);
            this.btnClear.TabIndex = 9;
            this.btnClear.Text = "Clear form";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // cardRecent
            // 
            this.cardRecent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.cardRecent.CardColor = System.Drawing.Color.White;
            this.cardRecent.Controls.Add(this.tblRecent);
            this.cardRecent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardRecent.DrawShadow = true;
            this.cardRecent.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.cardRecent.Location = new System.Drawing.Point(24, 798);
            this.cardRecent.Margin = new System.Windows.Forms.Padding(0, 20, 0, 0);
            this.cardRecent.Name = "cardRecent";
            this.cardRecent.Radius = 10;
            this.cardRecent.Size = new System.Drawing.Size(1418, 132);
            this.cardRecent.TabIndex = 4;
            // 
            // tblRecent
            // 
            this.tblRecent.BackColor = System.Drawing.Color.Transparent;
            this.tblRecent.ColumnCount = 1;
            this.tblRecent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblRecent.Controls.Add(this.tblRecentHead, 0, 0);
            this.tblRecent.Controls.Add(this.dgvReq, 0, 1);
            this.tblRecent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblRecent.Location = new System.Drawing.Point(0, 0);
            this.tblRecent.Name = "tblRecent";
            this.tblRecent.Padding = new System.Windows.Forms.Padding(16, 14, 16, 16);
            this.tblRecent.RowCount = 2;
            this.tblRecent.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblRecent.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblRecent.Size = new System.Drawing.Size(1418, 132);
            this.tblRecent.TabIndex = 0;
            // 
            // tblRecentHead
            // 
            this.tblRecentHead.AutoSize = true;
            this.tblRecentHead.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tblRecentHead.BackColor = System.Drawing.Color.Transparent;
            this.tblRecentHead.ColumnCount = 2;
            this.tblRecentHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblRecentHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tblRecentHead.Controls.Add(this.lblRecent, 0, 0);
            this.tblRecentHead.Controls.Add(this.btnRefresh, 1, 0);
            this.tblRecentHead.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblRecentHead.Location = new System.Drawing.Point(16, 14);
            this.tblRecentHead.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.tblRecentHead.Name = "tblRecentHead";
            this.tblRecentHead.RowCount = 1;
            this.tblRecentHead.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblRecentHead.Size = new System.Drawing.Size(1386, 34);
            this.tblRecentHead.TabIndex = 0;
            // 
            // lblRecent
            // 
            this.lblRecent.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblRecent.AutoSize = true;
            this.lblRecent.BackColor = System.Drawing.Color.Transparent;
            this.lblRecent.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblRecent.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblRecent.Location = new System.Drawing.Point(4, 7);
            this.lblRecent.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblRecent.Name = "lblRecent";
            this.lblRecent.Size = new System.Drawing.Size(195, 20);
            this.lblRecent.TabIndex = 0;
            this.lblRecent.Text = "Recent certificate requests";
            // 
            // btnRefresh
            // 
            this.btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Location = new System.Drawing.Point(1352, 0);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(0);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(34, 34);
            this.btnRefresh.TabIndex = 10;
            this.btnRefresh.Tag = "noskin";
            this.btnRefresh.UseVisualStyleBackColor = false;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // dgvReq
            // 
            this.dgvReq.AllowUserToAddRows = false;
            this.dgvReq.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvReq.BackgroundColor = System.Drawing.Color.White;
            this.dgvReq.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvReq.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvReq.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvReq.Location = new System.Drawing.Point(16, 58);
            this.dgvReq.Margin = new System.Windows.Forms.Padding(0);
            this.dgvReq.Name = "dgvReq";
            this.dgvReq.ReadOnly = true;
            this.dgvReq.RowHeadersVisible = false;
            this.dgvReq.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvReq.Size = new System.Drawing.Size(1386, 58);
            this.dgvReq.TabIndex = 11;
            // 
            // CertificateRequestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(1150, 950);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(1483, 837);
            this.Controls.Add(this.root);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "CertificateRequestForm";
            this.Text = "Certificate Request";
            this.root.ResumeLayout(false);
            this.root.PerformLayout();
            this.tblHeader.ResumeLayout(false);
            this.tblHeader.PerformLayout();
            this.tblBody.ResumeLayout(false);
            this.tblLeft.ResumeLayout(false);
            this.cardClient.ResumeLayout(false);
            this.tblClient.ResumeLayout(false);
            this.tblClient.PerformLayout();
            this.flowHead1.ResumeLayout(false);
            this.flowHead1.PerformLayout();
            this.cardCertificate.ResumeLayout(false);
            this.tblCert.ResumeLayout(false);
            this.tblCert.PerformLayout();
            this.flowHead2.ResumeLayout(false);
            this.flowHead2.PerformLayout();
            this.cardPurpose.ResumeLayout(false);
            this.tblPurpose.ResumeLayout(false);
            this.tblPurpose.PerformLayout();
            this.flowHead3.ResumeLayout(false);
            this.flowHead3.PerformLayout();
            this.tblRight.ResumeLayout(false);
            this.cardSummary.ResumeLayout(false);
            this.tblSummary.ResumeLayout(false);
            this.tblSummary.PerformLayout();
            this.pnlNextStep.ResumeLayout(false);
            this.pnlNextBody.ResumeLayout(false);
            this.cardPhoto.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picClient)).EndInit();
            this.pnlValidation.ResumeLayout(false);
            this.flowActions.ResumeLayout(false);
            this.cardRecent.ResumeLayout(false);
            this.tblRecent.ResumeLayout(false);
            this.tblRecent.PerformLayout();
            this.tblRecentHead.ResumeLayout(false);
            this.tblRecentHead.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReq)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel root;
        private System.Windows.Forms.TableLayoutPanel tblHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private CROMS.Modules.StatusPill pillQueueRef;

        private System.Windows.Forms.TableLayoutPanel tblBody;
        private System.Windows.Forms.TableLayoutPanel tblLeft;
        private System.Windows.Forms.TableLayoutPanel tblRight;

        private CROMS.Modules.CardPanel cardClient;
        private System.Windows.Forms.TableLayoutPanel tblClient;
        private System.Windows.Forms.FlowLayoutPanel flowHead1;
        private CROMS.Modules.StatusPill pillStep1;
        private System.Windows.Forms.Label lblClientTitle;
        private System.Windows.Forms.Label lblClientDesc;
        private System.Windows.Forms.Label lblFirst;
        private System.Windows.Forms.Label lblMiddle;
        private System.Windows.Forms.Label lblLast;
        private System.Windows.Forms.TextBox txtFirst;
        private System.Windows.Forms.TextBox txtMiddle;
        private System.Windows.Forms.TextBox txtLast;

        private CROMS.Modules.CardPanel cardCertificate;
        private System.Windows.Forms.TableLayoutPanel tblCert;
        private System.Windows.Forms.FlowLayoutPanel flowHead2;
        private CROMS.Modules.StatusPill pillStep2;
        private System.Windows.Forms.Label lblCertTitle;
        private System.Windows.Forms.Label lblCertDesc;
        private System.Windows.Forms.Label lblCertType;
        private System.Windows.Forms.Label lblRecordType;
        private System.Windows.Forms.ComboBox cboCertType;
        private System.Windows.Forms.ComboBox cboRecordType;
        private System.Windows.Forms.Label lblSelRecord;
        private System.Windows.Forms.ComboBox cboRecord;

        private CROMS.Modules.CardPanel cardPurpose;
        private System.Windows.Forms.TableLayoutPanel tblPurpose;
        private System.Windows.Forms.FlowLayoutPanel flowHead3;
        private CROMS.Modules.StatusPill pillStep3;
        private System.Windows.Forms.Label lblPurposeTitle;
        private System.Windows.Forms.Label lblPurposeDesc;
        private System.Windows.Forms.Label lblCopies;
        private System.Windows.Forms.Label lblPurpose;
        private System.Windows.Forms.TextBox txtCopies;
        private System.Windows.Forms.TextBox txtPurpose;
        private System.Windows.Forms.Label lblPurposeHint;

        private CROMS.Modules.CardPanel cardSummary;
        private System.Windows.Forms.TableLayoutPanel tblSummary;
        private System.Windows.Forms.Label lblSummaryTitle;
        private System.Windows.Forms.Label lblSummarySub;
        private System.Windows.Forms.Label lblCapClient;
        private System.Windows.Forms.Label lblSumClient;
        private System.Windows.Forms.Label lblCapCertType;
        private System.Windows.Forms.Label lblSumCertType;
        private System.Windows.Forms.Label lblCapRecordType;
        private System.Windows.Forms.Label lblSumRecordType;
        private System.Windows.Forms.Label lblCapRecord;
        private System.Windows.Forms.Label lblSumRecord;
        private System.Windows.Forms.Label lblCapCopies;
        private System.Windows.Forms.Label lblSumCopies;
        private System.Windows.Forms.Label lblCapPurpose;
        private System.Windows.Forms.Label lblSumPurpose;
        private System.Windows.Forms.Panel pnlDivider;
        private System.Windows.Forms.Panel pnlNextStep;
        private System.Windows.Forms.Panel pnlNextBar;
        private System.Windows.Forms.Panel pnlNextBody;
        private System.Windows.Forms.Label lblNextCap;
        private System.Windows.Forms.Label lblNextStep;

        private CROMS.Modules.CardPanel cardPhoto;
        private System.Windows.Forms.Label lblPhotoCap;
        private System.Windows.Forms.PictureBox picClient;

        private System.Windows.Forms.Panel pnlValidation;
        private System.Windows.Forms.Panel pnlValBar;
        private System.Windows.Forms.Label lblValidation;

        private System.Windows.Forms.FlowLayoutPanel flowActions;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.Button btnClear;

        private CROMS.Modules.CardPanel cardRecent;
        private System.Windows.Forms.TableLayoutPanel tblRecent;
        private System.Windows.Forms.TableLayoutPanel tblRecentHead;
        private System.Windows.Forms.Label lblRecent;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.DataGridView dgvReq;
    }
}
