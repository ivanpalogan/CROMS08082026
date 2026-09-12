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
            this.lblEngine = new System.Windows.Forms.Label();
            this.dgvFields = new System.Windows.Forms.DataGridView();
            this.btnAutoFill = new System.Windows.Forms.Button();
            this.btnReview = new System.Windows.Forms.Button();
            this.progress = new System.Windows.Forms.ProgressBar();
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
            this.lblFieldConf = new System.Windows.Forms.Label();
            this.lblFormCap = new System.Windows.Forms.Label();
            this.txtFormId = new System.Windows.Forms.TextBox();
            this.lblRegCap = new System.Windows.Forms.Label();
            this.txtRegistryNo = new System.Windows.Forms.TextBox();
            this.btnReport = new System.Windows.Forms.Button();
            this.btnSeal = new System.Windows.Forms.Button();
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
            ((System.ComponentModel.ISupportInitialize)(this.dgvFields)).BeginInit();
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
            this.lblTitle.Text = "Intelligent Document Processing";
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
            // lblEngine
            //
            this.lblEngine.AutoSize = true;
            this.lblEngine.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblEngine.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblEngine.Location = new System.Drawing.Point(24, 92);
            this.lblEngine.Name = "lblEngine";
            this.lblEngine.Size = new System.Drawing.Size(90, 15);
            this.lblEngine.TabIndex = 6;
            this.lblEngine.Text = "OCR engine: —";
            //
            // grpScan
            //
            this.grpScan.Controls.Add(this.pnlScanHost);
            this.grpScan.Controls.Add(this.btnZoomOut);
            this.grpScan.Controls.Add(this.btnZoomIn);
            this.grpScan.Controls.Add(this.btnDeskew);
            this.grpScan.Controls.Add(this.btnSeal);
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
            // btnSeal
            //
            this.btnSeal.Enabled = false;
            this.btnSeal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSeal.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnSeal.Location = new System.Drawing.Point(380, 22);
            this.btnSeal.Name = "btnSeal";
            this.btnSeal.Size = new System.Drawing.Size(200, 30);
            this.btnSeal.TabIndex = 35;
            this.btnSeal.Text = "Seal / Stamp";
            this.btnSeal.UseVisualStyleBackColor = true;
            this.btnSeal.Click += new System.EventHandler(this.btnSeal_Click);
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
            this.grpFields.Controls.Add(this.dgvFields);
            this.grpFields.Controls.Add(this.progress);
            this.grpFields.Controls.Add(this.btnAutoFill);
            this.grpFields.Controls.Add(this.btnReview);
            this.grpFields.Controls.Add(this.lblDocClass);
            this.grpFields.Controls.Add(this.txtDocClass);
            this.grpFields.Controls.Add(this.lblConf);
            this.grpFields.Controls.Add(this.lblFieldConf);
            this.grpFields.Controls.Add(this.lblFormCap);
            this.grpFields.Controls.Add(this.txtFormId);
            this.grpFields.Controls.Add(this.lblRegCap);
            this.grpFields.Controls.Add(this.txtRegistryNo);
            this.grpFields.Controls.Add(this.btnReport);
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
            this.lblFieldConf.Click += new System.EventHandler(this.lblFieldConf_Click);
            this.lblFieldConf.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // btnReocr
            //
            this.btnReocr.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReocr.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnReocr.Location = new System.Drawing.Point(20, 500);
            this.btnReocr.Name = "btnReocr";
            this.btnReocr.Size = new System.Drawing.Size(90, 40);
            this.btnReocr.TabIndex = 24;
            this.btnReocr.Text = "Re-OCR";
            this.btnReocr.UseVisualStyleBackColor = true;
            this.btnReocr.Click += new System.EventHandler(this.btnReocr_Click);
            //
            // btnDraft
            //
            this.btnDraft.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDraft.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnDraft.Location = new System.Drawing.Point(292, 500);
            this.btnDraft.Name = "btnDraft";
            this.btnDraft.Size = new System.Drawing.Size(110, 40);
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
            this.btnCommit.Location = new System.Drawing.Point(408, 500);
            this.btnCommit.Name = "btnCommit";
            this.btnCommit.Size = new System.Drawing.Size(170, 40);
            this.btnCommit.TabIndex = 26;
            this.btnCommit.Text = "Commit to Registry";
            this.btnCommit.UseVisualStyleBackColor = false;
            this.btnCommit.Click += new System.EventHandler(this.btnCommit_Click);
            //
            // lblFormCap
            //
            this.lblFormCap.AutoSize = true;
            this.lblFormCap.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblFormCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblFormCap.Location = new System.Drawing.Point(20, 80);
            this.lblFormCap.Name = "lblFormCap";
            this.lblFormCap.Size = new System.Drawing.Size(130, 13);
            this.lblFormCap.TabIndex = 30;
            this.lblFormCap.Text = "FORM IDENTIFICATION";
            //
            // txtFormId
            //
            this.txtFormId.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.txtFormId.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtFormId.Location = new System.Drawing.Point(20, 98);
            this.txtFormId.Name = "txtFormId";
            this.txtFormId.ReadOnly = true;
            this.txtFormId.Size = new System.Drawing.Size(440, 23);
            this.txtFormId.TabIndex = 31;
            //
            // lblRegCap
            //
            this.lblRegCap.AutoSize = true;
            this.lblRegCap.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblRegCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblRegCap.Location = new System.Drawing.Point(470, 80);
            this.lblRegCap.Name = "lblRegCap";
            this.lblRegCap.Size = new System.Drawing.Size(90, 13);
            this.lblRegCap.TabIndex = 32;
            this.lblRegCap.Text = "REGISTRY NO.";
            //
            // txtRegistryNo
            //
            this.txtRegistryNo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.txtRegistryNo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.txtRegistryNo.Location = new System.Drawing.Point(470, 98);
            this.txtRegistryNo.Name = "txtRegistryNo";
            this.txtRegistryNo.ReadOnly = true;
            this.txtRegistryNo.Size = new System.Drawing.Size(170, 23);
            this.txtRegistryNo.TabIndex = 33;
            //
            // btnReport
            //
            this.btnReport.Enabled = false;
            this.btnReport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReport.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnReport.Location = new System.Drawing.Point(650, 96);
            this.btnReport.Name = "btnReport";
            this.btnReport.Size = new System.Drawing.Size(136, 27);
            this.btnReport.TabIndex = 34;
            this.btnReport.Text = "Print Certificate";
            this.btnReport.UseVisualStyleBackColor = true;
            this.btnReport.Click += new System.EventHandler(this.btnReport_Click);
            //
            // dgvFields
            //
            this.dgvFields.AllowUserToAddRows = false;
            this.dgvFields.AllowUserToDeleteRows = false;
            this.dgvFields.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvFields.BackgroundColor = System.Drawing.Color.White;
            this.dgvFields.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvFields.Location = new System.Drawing.Point(20, 134);
            this.dgvFields.Name = "dgvFields";
            this.dgvFields.RowHeadersVisible = false;
            this.dgvFields.Size = new System.Drawing.Size(766, 306);
            this.dgvFields.TabIndex = 27;
            //
            // progress
            //
            this.progress.Location = new System.Drawing.Point(20, 486);
            this.progress.MarqueeAnimationSpeed = 30;
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(766, 10);
            this.progress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progress.TabIndex = 28;
            this.progress.Visible = false;
            //
            // btnAutoFill
            //
            this.btnAutoFill.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.btnAutoFill.Enabled = false;
            this.btnAutoFill.FlatAppearance.BorderSize = 0;
            this.btnAutoFill.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAutoFill.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnAutoFill.ForeColor = System.Drawing.Color.White;
            this.btnAutoFill.Location = new System.Drawing.Point(584, 500);
            this.btnAutoFill.Name = "btnAutoFill";
            this.btnAutoFill.Size = new System.Drawing.Size(202, 40);
            this.btnAutoFill.TabIndex = 29;
            this.btnAutoFill.Text = "Auto-Fill Form";
            this.btnAutoFill.UseVisualStyleBackColor = false;
            this.btnAutoFill.Click += new System.EventHandler(this.btnAutoFill_Click);
            //
            // btnReview
            //
            this.btnReview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(193)))), ((int)(((byte)(7)))));
            this.btnReview.Enabled = false;
            this.btnReview.FlatAppearance.BorderSize = 0;
            this.btnReview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReview.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnReview.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(66)))), ((int)(((byte)(50)))), ((int)(((byte)(3)))));
            this.btnReview.Location = new System.Drawing.Point(116, 500);
            this.btnReview.Name = "btnReview";
            this.btnReview.Size = new System.Drawing.Size(170, 40);
            this.btnReview.TabIndex = 30;
            this.btnReview.Text = "Send to Manual Review";
            this.btnReview.UseVisualStyleBackColor = false;
            this.btnReview.Click += new System.EventHandler(this.btnReview_Click);
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
            this.Controls.Add(this.lblEngine);
            this.Controls.Add(this.grpScan);
            this.Controls.Add(this.grpFields);
            this.Controls.Add(this.lblBatch);
            this.Controls.Add(this.dgvBatch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "OcrDigitizationForm";
            this.Text = "Intelligent Document Processing";
            this.grpScan.ResumeLayout(false);
            this.pnlScanHost.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbScan)).EndInit();
            this.grpFields.ResumeLayout(false);
            this.grpFields.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFields)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBatch)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnRunOcr;
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
        private System.Windows.Forms.Label lblFieldConf;
        private System.Windows.Forms.Label lblFormCap;
        private System.Windows.Forms.TextBox txtFormId;
        private System.Windows.Forms.Label lblRegCap;
        private System.Windows.Forms.TextBox txtRegistryNo;
        private System.Windows.Forms.Button btnReport;
        private System.Windows.Forms.Button btnSeal;
        private System.Windows.Forms.Button btnReocr;
        private System.Windows.Forms.Button btnDraft;
        private System.Windows.Forms.Button btnCommit;
        private System.Windows.Forms.Label lblBatch;
        private System.Windows.Forms.Label lblEngine;
        private System.Windows.Forms.DataGridView dgvFields;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.Button btnAutoFill;
        private System.Windows.Forms.Button btnReview;
        private System.Windows.Forms.DataGridView dgvBatch;
    }
}
