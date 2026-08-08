namespace CROMS.Forms
{
    partial class OcrDigitizationForm
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
            this.btnLoad = new System.Windows.Forms.Button();
            this.btnRunOcr = new System.Windows.Forms.Button();
            this.btnMode1 = new System.Windows.Forms.Button();
            this.btnMode2 = new System.Windows.Forms.Button();
            this.btnMode3 = new System.Windows.Forms.Button();
            this.grpScan = new System.Windows.Forms.GroupBox();
            this.pnlScanHost = new System.Windows.Forms.Panel();
            this.pbScan = new System.Windows.Forms.PictureBox();
            this.btnZoomOut = new System.Windows.Forms.Button();
            this.btnZoomIn = new System.Windows.Forms.Button();
            this.btnDeskew = new System.Windows.Forms.Button();
            this.grpFields = new System.Windows.Forms.GroupBox();
            this.lblDocClass = new System.Windows.Forms.Label();
            this.txtDocClass = new System.Windows.Forms.TextBox();
            this.lblConf = new System.Windows.Forms.Label();
            this.lblRegNo = new System.Windows.Forms.Label();
            this.txtRegNo = new System.Windows.Forms.TextBox();
            this.lblYear = new System.Windows.Forms.Label();
            this.txtYear = new System.Windows.Forms.TextBox();
            this.lblFirst = new System.Windows.Forms.Label();
            this.txtFirst = new System.Windows.Forms.TextBox();
            this.lblMiddle = new System.Windows.Forms.Label();
            this.txtMiddle = new System.Windows.Forms.TextBox();
            this.lblLast = new System.Windows.Forms.Label();
            this.txtLast = new System.Windows.Forms.TextBox();
            this.lblSex = new System.Windows.Forms.Label();
            this.txtSex = new System.Windows.Forms.TextBox();
            this.lblDob = new System.Windows.Forms.Label();
            this.txtDob = new System.Windows.Forms.TextBox();
            this.lblPlace = new System.Windows.Forms.Label();
            this.txtPlace = new System.Windows.Forms.TextBox();
            this.lblMother = new System.Windows.Forms.Label();
            this.txtMother = new System.Windows.Forms.TextBox();
            this.lblFather = new System.Windows.Forms.Label();
            this.txtFather = new System.Windows.Forms.TextBox();
            this.lblFieldConf = new System.Windows.Forms.Label();
            this.btnReocr = new System.Windows.Forms.Button();
            this.btnDraft = new System.Windows.Forms.Button();
            this.btnCommit = new System.Windows.Forms.Button();
            this.lblBatch = new System.Windows.Forms.Label();
            this.dgvBatch = new System.Windows.Forms.DataGridView();
            this.grpScan.SuspendLayout();
            this.pnlScanHost.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbScan)).BeginInit();
            this.grpFields.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBatch)).BeginInit();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblTitle.Location = new System.Drawing.Point(24, 16);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(202, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "OCR Digitization";
            //
            // lblSubtitle
            //
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblSubtitle.Location = new System.Drawing.Point(26, 56);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(360, 15);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "BACKLOG SCANNING  •  ENDORSEMENT ENCODING  •  ATTACH SUPPORTING DOCS";
            //
            // btnLoad
            //
            this.btnLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoad.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnLoad.Location = new System.Drawing.Point(1310, 22);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(170, 40);
            this.btnLoad.TabIndex = 2;
            this.btnLoad.Text = "Load Image";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            //
            // btnRunOcr
            //
            this.btnRunOcr.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRunOcr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.btnRunOcr.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRunOcr.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnRunOcr.ForeColor = System.Drawing.Color.White;
            this.btnRunOcr.Location = new System.Drawing.Point(1490, 22);
            this.btnRunOcr.Name = "btnRunOcr";
            this.btnRunOcr.Size = new System.Drawing.Size(170, 40);
            this.btnRunOcr.TabIndex = 3;
            this.btnRunOcr.Text = "Run OCR";
            this.btnRunOcr.UseVisualStyleBackColor = false;
            this.btnRunOcr.Click += new System.EventHandler(this.btnRunOcr_Click);
            //
            // btnMode1
            //
            this.btnMode1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.btnMode1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMode1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnMode1.ForeColor = System.Drawing.Color.White;
            this.btnMode1.Location = new System.Drawing.Point(24, 84);
            this.btnMode1.Name = "btnMode1";
            this.btnMode1.Size = new System.Drawing.Size(190, 34);
            this.btnMode1.TabIndex = 4;
            this.btnMode1.Tag = "Backlog Digitization";
            this.btnMode1.Text = "Backlog Digitization";
            this.btnMode1.UseVisualStyleBackColor = false;
            this.btnMode1.Click += new System.EventHandler(this.ModeButton_Click);
            //
            // btnMode2
            //
            this.btnMode2.BackColor = System.Drawing.Color.White;
            this.btnMode2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMode2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnMode2.Location = new System.Drawing.Point(216, 84);
            this.btnMode2.Name = "btnMode2";
            this.btnMode2.Size = new System.Drawing.Size(200, 34);
            this.btnMode2.TabIndex = 5;
            this.btnMode2.Tag = "Single Document Attach";
            this.btnMode2.Text = "Single Document Attach";
            this.btnMode2.UseVisualStyleBackColor = false;
            this.btnMode2.Click += new System.EventHandler(this.ModeButton_Click);
            //
            // btnMode3
            //
            this.btnMode3.BackColor = System.Drawing.Color.White;
            this.btnMode3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMode3.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnMode3.Location = new System.Drawing.Point(418, 84);
            this.btnMode3.Name = "btnMode3";
            this.btnMode3.Size = new System.Drawing.Size(200, 34);
            this.btnMode3.TabIndex = 6;
            this.btnMode3.Tag = "Endorsement Encoding";
            this.btnMode3.Text = "Endorsement Encoding";
            this.btnMode3.UseVisualStyleBackColor = false;
            this.btnMode3.Click += new System.EventHandler(this.ModeButton_Click);
            //
            // grpScan
            //
            this.grpScan.Controls.Add(this.pnlScanHost);
            this.grpScan.Controls.Add(this.btnZoomOut);
            this.grpScan.Controls.Add(this.btnZoomIn);
            this.grpScan.Controls.Add(this.btnDeskew);
            this.grpScan.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpScan.Location = new System.Drawing.Point(24, 130);
            this.grpScan.Name = "grpScan";
            this.grpScan.Size = new System.Drawing.Size(820, 570);
            this.grpScan.TabIndex = 7;
            this.grpScan.TabStop = false;
            this.grpScan.Text = "SCANNED DOCUMENT";
            //
            // pnlScanHost
            //
            this.pnlScanHost.AutoScroll = true;
            this.pnlScanHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(242)))), ((int)(((byte)(238)))));
            this.pnlScanHost.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlScanHost.Controls.Add(this.pbScan);
            this.pnlScanHost.Location = new System.Drawing.Point(20, 60);
            this.pnlScanHost.Name = "pnlScanHost";
            this.pnlScanHost.Size = new System.Drawing.Size(780, 490);
            this.pnlScanHost.TabIndex = 3;
            //
            // pbScan
            //
            this.pbScan.Location = new System.Drawing.Point(0, 0);
            this.pbScan.Name = "pbScan";
            this.pbScan.Size = new System.Drawing.Size(776, 486);
            this.pbScan.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbScan.TabIndex = 0;
            this.pbScan.TabStop = false;
            //
            // btnZoomOut
            //
            this.btnZoomOut.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnZoomOut.Location = new System.Drawing.Point(560, 22);
            this.btnZoomOut.Name = "btnZoomOut";
            this.btnZoomOut.Size = new System.Drawing.Size(70, 30);
            this.btnZoomOut.TabIndex = 0;
            this.btnZoomOut.Text = "Zoom −";
            this.btnZoomOut.UseVisualStyleBackColor = true;
            this.btnZoomOut.Click += new System.EventHandler(this.btnZoomOut_Click);
            //
            // btnZoomIn
            //
            this.btnZoomIn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnZoomIn.Location = new System.Drawing.Point(636, 22);
            this.btnZoomIn.Name = "btnZoomIn";
            this.btnZoomIn.Size = new System.Drawing.Size(70, 30);
            this.btnZoomIn.TabIndex = 1;
            this.btnZoomIn.Text = "Zoom +";
            this.btnZoomIn.UseVisualStyleBackColor = true;
            this.btnZoomIn.Click += new System.EventHandler(this.btnZoomIn_Click);
            //
            // btnDeskew
            //
            this.btnDeskew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeskew.Location = new System.Drawing.Point(712, 22);
            this.btnDeskew.Name = "btnDeskew";
            this.btnDeskew.Size = new System.Drawing.Size(88, 30);
            this.btnDeskew.TabIndex = 2;
            this.btnDeskew.Text = "Deskew";
            this.btnDeskew.UseVisualStyleBackColor = true;
            this.btnDeskew.Click += new System.EventHandler(this.btnDeskew_Click);
            //
            // grpFields
            //
            this.grpFields.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.grpFields.Controls.Add(this.lblDocClass);
            this.grpFields.Controls.Add(this.txtDocClass);
            this.grpFields.Controls.Add(this.lblConf);
            this.grpFields.Controls.Add(this.lblRegNo);
            this.grpFields.Controls.Add(this.txtRegNo);
            this.grpFields.Controls.Add(this.lblYear);
            this.grpFields.Controls.Add(this.txtYear);
            this.grpFields.Controls.Add(this.lblFirst);
            this.grpFields.Controls.Add(this.txtFirst);
            this.grpFields.Controls.Add(this.lblMiddle);
            this.grpFields.Controls.Add(this.txtMiddle);
            this.grpFields.Controls.Add(this.lblLast);
            this.grpFields.Controls.Add(this.txtLast);
            this.grpFields.Controls.Add(this.lblSex);
            this.grpFields.Controls.Add(this.txtSex);
            this.grpFields.Controls.Add(this.lblDob);
            this.grpFields.Controls.Add(this.txtDob);
            this.grpFields.Controls.Add(this.lblPlace);
            this.grpFields.Controls.Add(this.txtPlace);
            this.grpFields.Controls.Add(this.lblMother);
            this.grpFields.Controls.Add(this.txtMother);
            this.grpFields.Controls.Add(this.lblFather);
            this.grpFields.Controls.Add(this.txtFather);
            this.grpFields.Controls.Add(this.lblFieldConf);
            this.grpFields.Controls.Add(this.btnReocr);
            this.grpFields.Controls.Add(this.btnDraft);
            this.grpFields.Controls.Add(this.btnCommit);
            this.grpFields.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpFields.Location = new System.Drawing.Point(860, 130);
            this.grpFields.Name = "grpFields";
            this.grpFields.Size = new System.Drawing.Size(806, 570);
            this.grpFields.TabIndex = 8;
            this.grpFields.TabStop = false;
            this.grpFields.Text = "EXTRACTED FIELDS — REVIEW REQUIRED";
            //
            // lblDocClass
            //
            this.lblDocClass.AutoSize = true;
            this.lblDocClass.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblDocClass.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblDocClass.Location = new System.Drawing.Point(20, 30);
            this.lblDocClass.Name = "lblDocClass";
            this.lblDocClass.Size = new System.Drawing.Size(112, 13);
            this.lblDocClass.TabIndex = 0;
            this.lblDocClass.Text = "DOCUMENT CLASS";
            //
            // txtDocClass
            //
            this.txtDocClass.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtDocClass.Location = new System.Drawing.Point(20, 48);
            this.txtDocClass.Name = "txtDocClass";
            this.txtDocClass.Size = new System.Drawing.Size(600, 25);
            this.txtDocClass.TabIndex = 1;
            //
            // lblConf
            //
            this.lblConf.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(212)))), ((int)(((byte)(237)))), ((int)(((byte)(218)))));
            this.lblConf.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblConf.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.lblConf.Location = new System.Drawing.Point(636, 46);
            this.lblConf.Name = "lblConf";
            this.lblConf.Size = new System.Drawing.Size(150, 28);
            this.lblConf.TabIndex = 2;
            this.lblConf.Text = "CONFIDENCE —";
            this.lblConf.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblRegNo
            //
            this.lblRegNo.AutoSize = true;
            this.lblRegNo.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblRegNo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblRegNo.Location = new System.Drawing.Point(20, 88);
            this.lblRegNo.Name = "lblRegNo";
            this.lblRegNo.Size = new System.Drawing.Size(83, 13);
            this.lblRegNo.TabIndex = 3;
            this.lblRegNo.Text = "REGISTRY NO.";
            //
            // txtRegNo
            //
            this.txtRegNo.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtRegNo.Location = new System.Drawing.Point(20, 106);
            this.txtRegNo.Name = "txtRegNo";
            this.txtRegNo.Size = new System.Drawing.Size(370, 25);
            this.txtRegNo.TabIndex = 4;
            //
            // lblYear
            //
            this.lblYear.AutoSize = true;
            this.lblYear.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblYear.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblYear.Location = new System.Drawing.Point(410, 88);
            this.lblYear.Name = "lblYear";
            this.lblYear.Size = new System.Drawing.Size(34, 13);
            this.lblYear.TabIndex = 5;
            this.lblYear.Text = "YEAR";
            //
            // txtYear
            //
            this.txtYear.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtYear.Location = new System.Drawing.Point(410, 106);
            this.txtYear.Name = "txtYear";
            this.txtYear.Size = new System.Drawing.Size(376, 25);
            this.txtYear.TabIndex = 6;
            //
            // lblFirst
            //
            this.lblFirst.AutoSize = true;
            this.lblFirst.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblFirst.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblFirst.Location = new System.Drawing.Point(20, 148);
            this.lblFirst.Name = "lblFirst";
            this.lblFirst.Size = new System.Drawing.Size(114, 13);
            this.lblFirst.TabIndex = 7;
            this.lblFirst.Text = "CHILD FIRST NAME";
            //
            // txtFirst
            //
            this.txtFirst.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtFirst.Location = new System.Drawing.Point(20, 166);
            this.txtFirst.Name = "txtFirst";
            this.txtFirst.Size = new System.Drawing.Size(370, 25);
            this.txtFirst.TabIndex = 8;
            //
            // lblMiddle
            //
            this.lblMiddle.AutoSize = true;
            this.lblMiddle.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblMiddle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblMiddle.Location = new System.Drawing.Point(410, 148);
            this.lblMiddle.Name = "lblMiddle";
            this.lblMiddle.Size = new System.Drawing.Size(90, 13);
            this.lblMiddle.TabIndex = 9;
            this.lblMiddle.Text = "MIDDLE NAME";
            //
            // txtMiddle
            //
            this.txtMiddle.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtMiddle.Location = new System.Drawing.Point(410, 166);
            this.txtMiddle.Name = "txtMiddle";
            this.txtMiddle.Size = new System.Drawing.Size(376, 25);
            this.txtMiddle.TabIndex = 10;
            //
            // lblLast
            //
            this.lblLast.AutoSize = true;
            this.lblLast.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblLast.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblLast.Location = new System.Drawing.Point(20, 208);
            this.lblLast.Name = "lblLast";
            this.lblLast.Size = new System.Drawing.Size(72, 13);
            this.lblLast.TabIndex = 11;
            this.lblLast.Text = "LAST NAME";
            //
            // txtLast
            //
            this.txtLast.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtLast.Location = new System.Drawing.Point(20, 226);
            this.txtLast.Name = "txtLast";
            this.txtLast.Size = new System.Drawing.Size(370, 25);
            this.txtLast.TabIndex = 12;
            //
            // lblSex
            //
            this.lblSex.AutoSize = true;
            this.lblSex.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblSex.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblSex.Location = new System.Drawing.Point(410, 208);
            this.lblSex.Name = "lblSex";
            this.lblSex.Size = new System.Drawing.Size(29, 13);
            this.lblSex.TabIndex = 13;
            this.lblSex.Text = "SEX";
            //
            // txtSex
            //
            this.txtSex.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtSex.Location = new System.Drawing.Point(410, 226);
            this.txtSex.Name = "txtSex";
            this.txtSex.Size = new System.Drawing.Size(376, 25);
            this.txtSex.TabIndex = 14;
            //
            // lblDob
            //
            this.lblDob.AutoSize = true;
            this.lblDob.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblDob.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblDob.Location = new System.Drawing.Point(20, 268);
            this.lblDob.Name = "lblDob";
            this.lblDob.Size = new System.Drawing.Size(89, 13);
            this.lblDob.TabIndex = 15;
            this.lblDob.Text = "DATE OF BIRTH";
            //
            // txtDob
            //
            this.txtDob.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtDob.Location = new System.Drawing.Point(20, 286);
            this.txtDob.Name = "txtDob";
            this.txtDob.Size = new System.Drawing.Size(766, 25);
            this.txtDob.TabIndex = 16;
            //
            // lblPlace
            //
            this.lblPlace.AutoSize = true;
            this.lblPlace.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblPlace.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblPlace.Location = new System.Drawing.Point(20, 328);
            this.lblPlace.Name = "lblPlace";
            this.lblPlace.Size = new System.Drawing.Size(94, 13);
            this.lblPlace.TabIndex = 17;
            this.lblPlace.Text = "PLACE OF BIRTH";
            //
            // txtPlace
            //
            this.txtPlace.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtPlace.Location = new System.Drawing.Point(20, 346);
            this.txtPlace.Name = "txtPlace";
            this.txtPlace.Size = new System.Drawing.Size(766, 25);
            this.txtPlace.TabIndex = 18;
            //
            // lblMother
            //
            this.lblMother.AutoSize = true;
            this.lblMother.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblMother.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblMother.Location = new System.Drawing.Point(20, 388);
            this.lblMother.Name = "lblMother";
            this.lblMother.Size = new System.Drawing.Size(52, 13);
            this.lblMother.TabIndex = 19;
            this.lblMother.Text = "MOTHER";
            //
            // txtMother
            //
            this.txtMother.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtMother.Location = new System.Drawing.Point(20, 406);
            this.txtMother.Name = "txtMother";
            this.txtMother.Size = new System.Drawing.Size(370, 25);
            this.txtMother.TabIndex = 20;
            //
            // lblFather
            //
            this.lblFather.AutoSize = true;
            this.lblFather.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblFather.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblFather.Location = new System.Drawing.Point(410, 388);
            this.lblFather.Name = "lblFather";
            this.lblFather.Size = new System.Drawing.Size(48, 13);
            this.lblFather.TabIndex = 21;
            this.lblFather.Text = "FATHER";
            //
            // txtFather
            //
            this.txtFather.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtFather.Location = new System.Drawing.Point(410, 406);
            this.txtFather.Name = "txtFather";
            this.txtFather.Size = new System.Drawing.Size(376, 25);
            this.txtFather.TabIndex = 22;
            //
            // lblFieldConf
            //
            this.lblFieldConf.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.lblFieldConf.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblFieldConf.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblFieldConf.Location = new System.Drawing.Point(20, 446);
            this.lblFieldConf.Name = "lblFieldConf";
            this.lblFieldConf.Size = new System.Drawing.Size(766, 40);
            this.lblFieldConf.TabIndex = 23;
            this.lblFieldConf.Text = "FIELD CONFIDENCE  —  run OCR to populate";
            this.lblFieldConf.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // btnReocr
            //
            this.btnReocr.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReocr.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnReocr.Location = new System.Drawing.Point(360, 500);
            this.btnReocr.Name = "btnReocr";
            this.btnReocr.Size = new System.Drawing.Size(100, 40);
            this.btnReocr.TabIndex = 24;
            this.btnReocr.Text = "Re-OCR";
            this.btnReocr.UseVisualStyleBackColor = true;
            this.btnReocr.Click += new System.EventHandler(this.btnReocr_Click);
            //
            // btnDraft
            //
            this.btnDraft.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDraft.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnDraft.Location = new System.Drawing.Point(466, 500);
            this.btnDraft.Name = "btnDraft";
            this.btnDraft.Size = new System.Drawing.Size(120, 40);
            this.btnDraft.TabIndex = 25;
            this.btnDraft.Text = "Save as Draft";
            this.btnDraft.UseVisualStyleBackColor = true;
            this.btnDraft.Click += new System.EventHandler(this.btnDraft_Click);
            //
            // btnCommit
            //
            this.btnCommit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.btnCommit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCommit.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnCommit.ForeColor = System.Drawing.Color.White;
            this.btnCommit.Location = new System.Drawing.Point(592, 500);
            this.btnCommit.Name = "btnCommit";
            this.btnCommit.Size = new System.Drawing.Size(194, 40);
            this.btnCommit.TabIndex = 26;
            this.btnCommit.Text = "Commit to Birth Registry";
            this.btnCommit.UseVisualStyleBackColor = false;
            this.btnCommit.Click += new System.EventHandler(this.btnCommit_Click);
            //
            // lblBatch
            //
            this.lblBatch.AutoSize = true;
            this.lblBatch.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblBatch.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblBatch.Location = new System.Drawing.Point(24, 712);
            this.lblBatch.Name = "lblBatch";
            this.lblBatch.Size = new System.Drawing.Size(126, 17);
            this.lblBatch.TabIndex = 9;
            this.lblBatch.Text = "OCR BATCH — TODAY";
            //
            // dgvBatch
            //
            this.dgvBatch.AllowUserToAddRows = false;
            this.dgvBatch.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvBatch.BackgroundColor = System.Drawing.Color.White;
            this.dgvBatch.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvBatch.Location = new System.Drawing.Point(24, 734);
            this.dgvBatch.Name = "dgvBatch";
            this.dgvBatch.ReadOnly = true;
            this.dgvBatch.RowHeadersVisible = false;
            this.dgvBatch.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvBatch.Size = new System.Drawing.Size(1642, 202);
            this.dgvBatch.TabIndex = 10;
            //
            // OcrDigitizationForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(1690, 966);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.btnRunOcr);
            this.Controls.Add(this.btnMode1);
            this.Controls.Add(this.btnMode2);
            this.Controls.Add(this.btnMode3);
            this.Controls.Add(this.grpScan);
            this.Controls.Add(this.grpFields);
            this.Controls.Add(this.lblBatch);
            this.Controls.Add(this.dgvBatch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "OcrDigitizationForm";
            this.Text = "OCR Digitization";
            this.grpScan.ResumeLayout(false);
            this.pnlScanHost.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbScan)).EndInit();
            this.grpFields.ResumeLayout(false);
            this.grpFields.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBatch)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnRunOcr;
        private System.Windows.Forms.Button btnMode1;
        private System.Windows.Forms.Button btnMode2;
        private System.Windows.Forms.Button btnMode3;
        private System.Windows.Forms.GroupBox grpScan;
        private System.Windows.Forms.Panel pnlScanHost;
        private System.Windows.Forms.PictureBox pbScan;
        private System.Windows.Forms.Button btnZoomOut;
        private System.Windows.Forms.Button btnZoomIn;
        private System.Windows.Forms.Button btnDeskew;
        private System.Windows.Forms.GroupBox grpFields;
        private System.Windows.Forms.Label lblDocClass;
        private System.Windows.Forms.TextBox txtDocClass;
        private System.Windows.Forms.Label lblConf;
        private System.Windows.Forms.Label lblRegNo;
        private System.Windows.Forms.TextBox txtRegNo;
        private System.Windows.Forms.Label lblYear;
        private System.Windows.Forms.TextBox txtYear;
        private System.Windows.Forms.Label lblFirst;
        private System.Windows.Forms.TextBox txtFirst;
        private System.Windows.Forms.Label lblMiddle;
        private System.Windows.Forms.TextBox txtMiddle;
        private System.Windows.Forms.Label lblLast;
        private System.Windows.Forms.TextBox txtLast;
        private System.Windows.Forms.Label lblSex;
        private System.Windows.Forms.TextBox txtSex;
        private System.Windows.Forms.Label lblDob;
        private System.Windows.Forms.TextBox txtDob;
        private System.Windows.Forms.Label lblPlace;
        private System.Windows.Forms.TextBox txtPlace;
        private System.Windows.Forms.Label lblMother;
        private System.Windows.Forms.TextBox txtMother;
        private System.Windows.Forms.Label lblFather;
        private System.Windows.Forms.TextBox txtFather;
        private System.Windows.Forms.Label lblFieldConf;
        private System.Windows.Forms.Button btnReocr;
        private System.Windows.Forms.Button btnDraft;
        private System.Windows.Forms.Button btnCommit;
        private System.Windows.Forms.Label lblBatch;
        private System.Windows.Forms.DataGridView dgvBatch;
    }
}
