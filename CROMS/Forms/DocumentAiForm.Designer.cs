namespace CROMS.Forms
{
    partial class DocumentAiForm
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSub = new System.Windows.Forms.Label();
            this.btnUpload = new System.Windows.Forms.Button();
            this.lblEngine = new System.Windows.Forms.Label();
            this.pic = new System.Windows.Forms.PictureBox();
            this.lblDetected = new System.Windows.Forms.Label();
            this.lblConf = new System.Windows.Forms.Label();
            this.progress = new System.Windows.Forms.ProgressBar();
            this.dgvFields = new System.Windows.Forms.DataGridView();
            this.btnAutoFill = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pic)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFields)).BeginInit();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblTitle.Location = new System.Drawing.Point(24, 18);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(322, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Document AI — Auto-Fill";
            //
            // lblSub
            //
            this.lblSub.AutoSize = true;
            this.lblSub.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblSub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblSub.Location = new System.Drawing.Point(26, 56);
            this.lblSub.Name = "lblSub";
            this.lblSub.Size = new System.Drawing.Size(560, 19);
            this.lblSub.TabIndex = 1;
            this.lblSub.Text = "Upload a Birth/Marriage/Death certificate image; CROMS detects the type and extracts the fields.";
            //
            // btnUpload
            //
            this.btnUpload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.btnUpload.FlatAppearance.BorderSize = 0;
            this.btnUpload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUpload.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnUpload.ForeColor = System.Drawing.Color.White;
            this.btnUpload.Location = new System.Drawing.Point(28, 84);
            this.btnUpload.Name = "btnUpload";
            this.btnUpload.Size = new System.Drawing.Size(190, 44);
            this.btnUpload.TabIndex = 2;
            this.btnUpload.Text = "Upload Document";
            this.btnUpload.UseVisualStyleBackColor = false;
            this.btnUpload.Click += new System.EventHandler(this.btnUpload_Click);
            //
            // lblEngine
            //
            this.lblEngine.AutoSize = true;
            this.lblEngine.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblEngine.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblEngine.Location = new System.Drawing.Point(232, 98);
            this.lblEngine.Name = "lblEngine";
            this.lblEngine.Size = new System.Drawing.Size(90, 15);
            this.lblEngine.TabIndex = 3;
            this.lblEngine.Text = "OCR engine: —";
            //
            // pic
            //
            this.pic.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.pic.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pic.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pic.Location = new System.Drawing.Point(28, 144);
            this.pic.Name = "pic";
            this.pic.Size = new System.Drawing.Size(500, 552);
            this.pic.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pic.TabIndex = 4;
            this.pic.TabStop = false;
            //
            // lblDetected
            //
            this.lblDetected.AutoSize = true;
            this.lblDetected.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
            this.lblDetected.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblDetected.Location = new System.Drawing.Point(556, 144);
            this.lblDetected.Name = "lblDetected";
            this.lblDetected.Size = new System.Drawing.Size(240, 28);
            this.lblDetected.TabIndex = 5;
            this.lblDetected.Text = "Document Detected: —";
            //
            // lblConf
            //
            this.lblConf.AutoSize = true;
            this.lblConf.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblConf.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblConf.Location = new System.Drawing.Point(558, 180);
            this.lblConf.Name = "lblConf";
            this.lblConf.Size = new System.Drawing.Size(200, 19);
            this.lblConf.TabIndex = 6;
            this.lblConf.Text = "Recognition: —   Extracted: —   Missing: —";
            //
            // progress
            //
            this.progress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progress.Location = new System.Drawing.Point(560, 210);
            this.progress.MarqueeAnimationSpeed = 30;
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(532, 10);
            this.progress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progress.TabIndex = 7;
            this.progress.Visible = false;
            //
            // dgvFields
            //
            this.dgvFields.AllowUserToAddRows = false;
            this.dgvFields.AllowUserToDeleteRows = false;
            this.dgvFields.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvFields.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvFields.BackgroundColor = System.Drawing.Color.White;
            this.dgvFields.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvFields.Location = new System.Drawing.Point(560, 232);
            this.dgvFields.Name = "dgvFields";
            this.dgvFields.RowHeadersVisible = false;
            this.dgvFields.Size = new System.Drawing.Size(532, 400);
            this.dgvFields.TabIndex = 8;
            //
            // btnAutoFill
            //
            this.btnAutoFill.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAutoFill.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.btnAutoFill.Enabled = false;
            this.btnAutoFill.FlatAppearance.BorderSize = 0;
            this.btnAutoFill.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAutoFill.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnAutoFill.ForeColor = System.Drawing.Color.White;
            this.btnAutoFill.Location = new System.Drawing.Point(560, 644);
            this.btnAutoFill.Name = "btnAutoFill";
            this.btnAutoFill.Size = new System.Drawing.Size(532, 46);
            this.btnAutoFill.TabIndex = 9;
            this.btnAutoFill.Text = "Auto-Fill Form";
            this.btnAutoFill.UseVisualStyleBackColor = false;
            this.btnAutoFill.Click += new System.EventHandler(this.btnAutoFill_Click);
            //
            // DocumentAiForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1120, 720);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblSub);
            this.Controls.Add(this.btnUpload);
            this.Controls.Add(this.lblEngine);
            this.Controls.Add(this.pic);
            this.Controls.Add(this.lblDetected);
            this.Controls.Add(this.lblConf);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.dgvFields);
            this.Controls.Add(this.btnAutoFill);
            this.Name = "DocumentAiForm";
            this.Text = "Document AI";
            ((System.ComponentModel.ISupportInitialize)(this.pic)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFields)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSub;
        private System.Windows.Forms.Button btnUpload;
        private System.Windows.Forms.Label lblEngine;
        private System.Windows.Forms.PictureBox pic;
        private System.Windows.Forms.Label lblDetected;
        private System.Windows.Forms.Label lblConf;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.DataGridView dgvFields;
        private System.Windows.Forms.Button btnAutoFill;
    }
}
