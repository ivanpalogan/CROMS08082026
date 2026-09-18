using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Attract / "touch to start" screen shown between clients. Designer-generated visual tree
    /// (behaviour in WelcomeForm.cs).
    /// </summary>
    public partial class WelcomeForm
    {
        private System.ComponentModel.IContainer components = null;

        private Panel _center;
        private Label _lblOffice;
        private Label _lblMuni;
        private Label _lblHeadline;
        private Panel _cta;
        private Label _lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._center = new System.Windows.Forms.Panel();
            this._lblOffice = new System.Windows.Forms.Label();
            this._lblMuni = new System.Windows.Forms.Label();
            this._lblHeadline = new System.Windows.Forms.Label();
            this._cta = new System.Windows.Forms.Panel();
            this._lblStatus = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this._center.SuspendLayout();
            this.SuspendLayout();
            // 
            // _center
            // 
            this._center.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this._center.Controls.Add(this.panel2);
            this._center.Controls.Add(this.panel1);
            this._center.Controls.Add(this._lblOffice);
            this._center.Controls.Add(this._lblMuni);
            this._center.Controls.Add(this._lblHeadline);
            this._center.Controls.Add(this._cta);
            this._center.Controls.Add(this._lblStatus);
            this._center.Location = new System.Drawing.Point(0, 0);
            this._center.Name = "_center";
            this._center.Size = new System.Drawing.Size(760, 400);
            this._center.TabIndex = 0;
            //
            // _lblOffice
            // 
            this._lblOffice.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this._lblOffice.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this._lblOffice.Location = new System.Drawing.Point(0, 116);
            this._lblOffice.Name = "_lblOffice";
            this._lblOffice.Size = new System.Drawing.Size(760, 32);
            this._lblOffice.TabIndex = 1;
            this._lblOffice.Text = "Local Civil Registry Office";
            this._lblOffice.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _lblMuni
            // 
            this._lblMuni.Font = new System.Drawing.Font("Segoe UI", 11F);
            this._lblMuni.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this._lblMuni.Location = new System.Drawing.Point(0, 150);
            this._lblMuni.Name = "_lblMuni";
            this._lblMuni.Size = new System.Drawing.Size(760, 26);
            this._lblMuni.TabIndex = 2;
            this._lblMuni.Text = "Municipality of Peñablanca, Cagayan";
            this._lblMuni.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _lblHeadline
            // 
            this._lblHeadline.Font = new System.Drawing.Font("Segoe UI", 34F, System.Drawing.FontStyle.Bold);
            this._lblHeadline.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this._lblHeadline.Location = new System.Drawing.Point(0, 200);
            this._lblHeadline.Name = "_lblHeadline";
            this._lblHeadline.Size = new System.Drawing.Size(760, 66);
            this._lblHeadline.TabIndex = 3;
            this._lblHeadline.Text = "Welcome";
            this._lblHeadline.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _cta
            // 
            this._cta.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this._cta.Location = new System.Drawing.Point(170, 288);
            this._cta.Name = "_cta";
            this._cta.Size = new System.Drawing.Size(420, 76);
            this._cta.TabIndex = 4;
            // 
            // _lblStatus
            // 
            this._lblStatus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this._lblStatus.Location = new System.Drawing.Point(0, 372);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(760, 26);
            this._lblStatus.TabIndex = 5;
            this._lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.panel2.BackgroundImage = global::CROMS.Kiosk.Properties.Resources.images__1_;
            this.panel2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel2.Location = new System.Drawing.Point(268, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(96, 96);
            this.panel2.TabIndex = 6;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.panel1.BackgroundImage = global::CROMS.Kiosk.Properties.Resources.images;
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel1.Location = new System.Drawing.Point(396, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(96, 96);
            this.panel1.TabIndex = 1;
            // 
            // WelcomeForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(1264, 681);
            this.Controls.Add(this._center);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Name = "WelcomeForm";
            this.Text = "CROMS — Client Kiosk";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this._center.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private Panel panel1;
        private Panel panel2;
    }
}
