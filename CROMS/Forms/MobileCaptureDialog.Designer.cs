namespace CROMS.Forms
{
    partial class MobileCaptureDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSub = new System.Windows.Forms.Label();
            this.picQr = new System.Windows.Forms.PictureBox();
            this.lblNoQr = new System.Windows.Forms.Label();
            this.lblCodeCap = new System.Windows.Forms.Label();
            this.lblCode = new System.Windows.Forms.Label();
            this.txtUrl = new System.Windows.Forms.TextBox();
            this.lblStep1 = new System.Windows.Forms.Label();
            this.lblStep1State = new System.Windows.Forms.Label();
            this.picCert = new System.Windows.Forms.PictureBox();
            this.lblStep2 = new System.Windows.Forms.Label();
            this.lblStep2State = new System.Windows.Forms.Label();
            this.picLic = new System.Windows.Forms.PictureBox();
            this.lblHint = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnContinue = new System.Windows.Forms.Button();
            this.pollTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.picQr)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picCert)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLic)).BeginInit();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = false;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(23, 27, 36);
            this.lblTitle.Location = new System.Drawing.Point(24, 18);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(640, 30);
            this.lblTitle.Text = "Mobile Capture";
            //
            // lblSub
            //
            this.lblSub.AutoSize = false;
            this.lblSub.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSub.ForeColor = System.Drawing.Color.FromArgb(91, 100, 114);
            this.lblSub.Location = new System.Drawing.Point(24, 50);
            this.lblSub.Name = "lblSub";
            this.lblSub.Size = new System.Drawing.Size(640, 36);
            this.lblSub.Text = "Scan this QR with the office phone. The phone only takes the pictures - CROMS reads them here.";
            //
            // picQr
            //
            this.picQr.BackColor = System.Drawing.Color.White;
            this.picQr.Location = new System.Drawing.Point(24, 96);
            this.picQr.Name = "picQr";
            this.picQr.Size = new System.Drawing.Size(230, 230);
            this.picQr.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            //
            // lblNoQr
            //
            this.lblNoQr.AutoSize = false;
            this.lblNoQr.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblNoQr.ForeColor = System.Drawing.Color.FromArgb(91, 100, 114);
            this.lblNoQr.Location = new System.Drawing.Point(24, 150);
            this.lblNoQr.Name = "lblNoQr";
            this.lblNoQr.Size = new System.Drawing.Size(230, 80);
            this.lblNoQr.Text = "QR code unavailable - open the address below on the phone instead.";
            this.lblNoQr.Visible = false;
            //
            // lblCodeCap
            //
            this.lblCodeCap.AutoSize = false;
            this.lblCodeCap.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblCodeCap.ForeColor = System.Drawing.Color.FromArgb(91, 100, 114);
            this.lblCodeCap.Location = new System.Drawing.Point(24, 334);
            this.lblCodeCap.Name = "lblCodeCap";
            this.lblCodeCap.Size = new System.Drawing.Size(230, 16);
            this.lblCodeCap.Text = "Capture Code (if the QR will not scan):";
            //
            // lblCode
            //
            this.lblCode.AutoSize = false;
            this.lblCode.Font = new System.Drawing.Font("Consolas", 16F, System.Drawing.FontStyle.Bold);
            this.lblCode.ForeColor = System.Drawing.Color.FromArgb(29, 78, 216);
            this.lblCode.Location = new System.Drawing.Point(24, 352);
            this.lblCode.Name = "lblCode";
            this.lblCode.Size = new System.Drawing.Size(230, 30);
            this.lblCode.Text = "--------";
            this.lblCode.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // txtUrl
            //
            this.txtUrl.Location = new System.Drawing.Point(24, 388);
            this.txtUrl.Name = "txtUrl";
            this.txtUrl.ReadOnly = true;
            this.txtUrl.Size = new System.Drawing.Size(230, 23);
            //
            // lblStep1
            //
            this.lblStep1.AutoSize = false;
            this.lblStep1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblStep1.ForeColor = System.Drawing.Color.FromArgb(23, 27, 36);
            this.lblStep1.Location = new System.Drawing.Point(284, 96);
            this.lblStep1.Name = "lblStep1";
            this.lblStep1.Size = new System.Drawing.Size(380, 22);
            this.lblStep1.Text = "1.  Certificate of Marriage (required)";
            //
            // lblStep1State
            //
            this.lblStep1State.AutoSize = false;
            this.lblStep1State.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStep1State.ForeColor = System.Drawing.Color.FromArgb(91, 100, 114);
            this.lblStep1State.Location = new System.Drawing.Point(284, 118);
            this.lblStep1State.Name = "lblStep1State";
            this.lblStep1State.Size = new System.Drawing.Size(250, 40);
            this.lblStep1State.Text = "Waiting for the phone...";
            //
            // picCert
            //
            this.picCert.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            this.picCert.Location = new System.Drawing.Point(544, 118);
            this.picCert.Name = "picCert";
            this.picCert.Size = new System.Drawing.Size(120, 96);
            this.picCert.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            //
            // lblStep2
            //
            this.lblStep2.AutoSize = false;
            this.lblStep2.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblStep2.ForeColor = System.Drawing.Color.FromArgb(23, 27, 36);
            this.lblStep2.Location = new System.Drawing.Point(284, 230);
            this.lblStep2.Name = "lblStep2";
            this.lblStep2.Size = new System.Drawing.Size(380, 22);
            this.lblStep2.Text = "2.  Marriage License (optional)";
            //
            // lblStep2State
            //
            this.lblStep2State.AutoSize = false;
            this.lblStep2State.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStep2State.ForeColor = System.Drawing.Color.FromArgb(91, 100, 114);
            this.lblStep2State.Location = new System.Drawing.Point(284, 252);
            this.lblStep2State.Name = "lblStep2State";
            this.lblStep2State.Size = new System.Drawing.Size(250, 40);
            this.lblStep2State.Text = "Not taken yet.";
            //
            // picLic
            //
            this.picLic.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            this.picLic.Location = new System.Drawing.Point(544, 252);
            this.picLic.Name = "picLic";
            this.picLic.Size = new System.Drawing.Size(120, 96);
            this.picLic.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            //
            // lblHint
            //
            this.lblHint.AutoSize = false;
            this.lblHint.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblHint.ForeColor = System.Drawing.Color.FromArgb(91, 100, 114);
            this.lblHint.Location = new System.Drawing.Point(284, 362);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(380, 50);
            this.lblHint.Text = "Keep this window open. Each photo appears here the moment the phone uploads it.";
            //
            // btnCancel
            //
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnCancel.Location = new System.Drawing.Point(284, 432);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(120, 40);
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // btnContinue
            //
            this.btnContinue.BackColor = System.Drawing.Color.FromArgb(29, 78, 216);
            this.btnContinue.Enabled = false;
            this.btnContinue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnContinue.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnContinue.ForeColor = System.Drawing.Color.White;
            this.btnContinue.Location = new System.Drawing.Point(414, 432);
            this.btnContinue.Name = "btnContinue";
            this.btnContinue.Size = new System.Drawing.Size(250, 40);
            this.btnContinue.Text = "Continue to OCR Review";
            this.btnContinue.UseVisualStyleBackColor = false;
            this.btnContinue.Click += new System.EventHandler(this.btnContinue_Click);
            //
            // pollTimer
            //
            this.pollTimer.Interval = 2000;
            this.pollTimer.Tick += new System.EventHandler(this.pollTimer_Tick);
            //
            // MobileCaptureDialog
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(688, 492);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblSub);
            this.Controls.Add(this.lblNoQr);
            this.Controls.Add(this.picQr);
            this.Controls.Add(this.lblCodeCap);
            this.Controls.Add(this.lblCode);
            this.Controls.Add(this.txtUrl);
            this.Controls.Add(this.lblStep1);
            this.Controls.Add(this.lblStep1State);
            this.Controls.Add(this.picCert);
            this.Controls.Add(this.lblStep2);
            this.Controls.Add(this.lblStep2State);
            this.Controls.Add(this.picLic);
            this.Controls.Add(this.lblHint);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnContinue);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MobileCaptureDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Mobile Capture";
            ((System.ComponentModel.ISupportInitialize)(this.picQr)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picCert)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picLic)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSub;
        private System.Windows.Forms.PictureBox picQr;
        private System.Windows.Forms.Label lblNoQr;
        private System.Windows.Forms.Label lblCodeCap;
        private System.Windows.Forms.Label lblCode;
        private System.Windows.Forms.TextBox txtUrl;
        private System.Windows.Forms.Label lblStep1;
        private System.Windows.Forms.Label lblStep1State;
        private System.Windows.Forms.PictureBox picCert;
        private System.Windows.Forms.Label lblStep2;
        private System.Windows.Forms.Label lblStep2State;
        private System.Windows.Forms.PictureBox picLic;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnContinue;
        private System.Windows.Forms.Timer pollTimer;
    }
}
