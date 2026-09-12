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
        private Panel _seal;
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
            this._center = new Panel();
            this._seal = new Panel();
            this._lblOffice = new Label();
            this._lblMuni = new Label();
            this._lblHeadline = new Label();
            this._cta = new Panel();
            this._lblStatus = new Label();
            this.SuspendLayout();
            //
            // _center  (fixed-size content block; WelcomeForm.cs centres it on resize)
            //
            this._center.BackColor = Color.FromArgb(244, 246, 249);
            this._center.Size = new Size(760, 400);
            this._center.Controls.Add(this._seal);
            this._center.Controls.Add(this._lblOffice);
            this._center.Controls.Add(this._lblMuni);
            this._center.Controls.Add(this._lblHeadline);
            this._center.Controls.Add(this._cta);
            this._center.Controls.Add(this._lblStatus);
            //
            // _seal  (owner-drawn navy badge + building mark, same brand mark as the desktop app)
            //
            this._seal.BackColor = Color.FromArgb(244, 246, 249);
            this._seal.Location = new Point(332, 0);
            this._seal.Size = new Size(96, 96);
            //
            // _lblOffice
            //
            this._lblOffice.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this._lblOffice.ForeColor = Color.FromArgb(23, 26, 36);
            this._lblOffice.Location = new Point(0, 116);
            this._lblOffice.Size = new Size(760, 32);
            this._lblOffice.Text = "Local Civil Registry Office";
            this._lblOffice.TextAlign = ContentAlignment.MiddleCenter;
            //
            // _lblMuni
            //
            this._lblMuni.Font = new Font("Segoe UI", 11F);
            this._lblMuni.ForeColor = Color.FromArgb(91, 100, 114);
            this._lblMuni.Location = new Point(0, 150);
            this._lblMuni.Size = new Size(760, 26);
            this._lblMuni.Text = "Municipality of Peñablanca, Cagayan";
            this._lblMuni.TextAlign = ContentAlignment.MiddleCenter;
            //
            // _lblHeadline
            //
            this._lblHeadline.Font = new Font("Segoe UI", 34F, FontStyle.Bold);
            this._lblHeadline.ForeColor = Color.FromArgb(23, 26, 36);
            this._lblHeadline.Location = new Point(0, 200);
            this._lblHeadline.Size = new Size(760, 66);
            this._lblHeadline.Text = "Welcome";
            this._lblHeadline.TextAlign = ContentAlignment.MiddleCenter;
            //
            // _cta  (owner-drawn pulsing pill)
            //
            this._cta.BackColor = Color.FromArgb(244, 246, 249);
            this._cta.Location = new Point(170, 288);
            this._cta.Size = new Size(420, 76);
            //
            // _lblStatus  (office availability; filled at runtime)
            //
            this._lblStatus.Font = new Font("Segoe UI", 10F);
            this._lblStatus.ForeColor = Color.FromArgb(91, 100, 114);
            this._lblStatus.Location = new Point(0, 372);
            this._lblStatus.Size = new Size(760, 26);
            this._lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            //
            // WelcomeForm
            //
            this.AutoScaleMode = AutoScaleMode.None;
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.ClientSize = new Size(1264, 681);
            this.Controls.Add(this._center);
            this.Font = new Font("Segoe UI", 10F);
            this.Name = "WelcomeForm";
            this.Text = "CROMS — Client Kiosk";
            this.WindowState = FormWindowState.Maximized;
            this.ResumeLayout(false);
        }
    }
}
