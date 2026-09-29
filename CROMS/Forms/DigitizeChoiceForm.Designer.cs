using CROMS.Modules;

namespace CROMS.Forms
{
    partial class DigitizeChoiceForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblBody = new System.Windows.Forms.Label();
            this.btnOcr = new System.Windows.Forms.Button();
            this.lblOcrHint = new System.Windows.Forms.Label();
            this.btnManual = new System.Windows.Forms.Button();
            this.lblManualHint = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // lblTitle
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = UiTheme.Ink;
            this.lblTitle.Location = new System.Drawing.Point(24, 20);
            this.lblTitle.Name = "lblTitle";
            // lblBody
            this.lblBody.AutoSize = false;
            this.lblBody.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblBody.ForeColor = UiTheme.Muted;
            this.lblBody.Location = new System.Drawing.Point(24, 54);
            this.lblBody.Name = "lblBody";
            this.lblBody.Size = new System.Drawing.Size(412, 70);
            // btnOcr
            this.btnOcr.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            this.btnOcr.Location = new System.Drawing.Point(24, 136);
            this.btnOcr.Name = "btnOcr";
            this.btnOcr.Size = new System.Drawing.Size(412, 56);
            this.btnOcr.Text = "📷  Scan / Upload with OCR";
            this.btnOcr.Click += new System.EventHandler(this.btnOcr_Click);
            // lblOcrHint
            this.lblOcrHint.AutoSize = true;
            this.lblOcrHint.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblOcrHint.ForeColor = UiTheme.Muted;
            this.lblOcrHint.Location = new System.Drawing.Point(24, 196);
            this.lblOcrHint.Name = "lblOcrHint";
            this.lblOcrHint.Text = "Reads the page automatically, then you review and correct the values.";
            // btnManual
            this.btnManual.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            this.btnManual.Location = new System.Drawing.Point(24, 222);
            this.btnManual.Name = "btnManual";
            this.btnManual.Size = new System.Drawing.Size(412, 56);
            this.btnManual.Text = "✏  Manual Entry";
            this.btnManual.Click += new System.EventHandler(this.btnManual_Click);
            // lblManualHint
            this.lblManualHint.AutoSize = true;
            this.lblManualHint.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblManualHint.ForeColor = UiTheme.Muted;
            this.lblManualHint.Location = new System.Drawing.Point(24, 282);
            this.lblManualHint.Name = "lblManualHint";
            this.lblManualHint.Text = "Type the record in yourself, straight from the ledger.";
            // DigitizeChoiceForm
            this.BackColor = UiTheme.PageBg;
            this.ClientSize = new System.Drawing.Size(460, 316);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblBody);
            this.Controls.Add(this.btnOcr);
            this.Controls.Add(this.lblOcrHint);
            this.Controls.Add(this.btnManual);
            this.Controls.Add(this.lblManualHint);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DigitizeChoiceForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblBody;
        private System.Windows.Forms.Button btnOcr;
        private System.Windows.Forms.Label lblOcrHint;
        private System.Windows.Forms.Button btnManual;
        private System.Windows.Forms.Label lblManualHint;
    }
}
