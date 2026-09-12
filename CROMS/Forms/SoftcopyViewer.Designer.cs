using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Forms
{
    /// <summary>Designer-side of <see cref="SoftcopyViewer"/> (static shell; image loaded in code).</summary>
    public partial class SoftcopyViewer
    {
        private System.ComponentModel.IContainer components = null;

        private Panel bar;
        private Button btnPrint;
        private Button btnClose;
        private PictureBox pic;
        private Label lblNone;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.bar = new Panel();
            this.btnPrint = new Button();
            this.btnClose = new Button();
            this.pic = new PictureBox();
            this.lblNone = new Label();
            this.SuspendLayout();
            //
            // bar
            //
            this.bar.BackColor = Color.White;
            this.bar.Dock = DockStyle.Top;
            this.bar.Height = 52;
            this.bar.Controls.Add(this.btnPrint);
            this.bar.Controls.Add(this.btnClose);
            //
            // btnPrint
            //
            this.btnPrint.BackColor = Color.FromArgb(13, 110, 253);
            this.btnPrint.FlatAppearance.BorderSize = 0;
            this.btnPrint.FlatStyle = FlatStyle.Flat;
            this.btnPrint.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnPrint.ForeColor = Color.White;
            this.btnPrint.Location = new Point(12, 9);
            this.btnPrint.Size = new Size(160, 34);
            this.btnPrint.Text = "🖨  Print Original";
            this.btnPrint.UseVisualStyleBackColor = false;
            this.btnPrint.Click += new System.EventHandler(this.btnPrint_Click);
            //
            // btnClose
            //
            this.btnClose.FlatStyle = FlatStyle.Flat;
            this.btnClose.Font = new Font("Segoe UI", 10F);
            this.btnClose.Location = new Point(180, 9);
            this.btnClose.Size = new Size(100, 34);
            this.btnClose.Text = "Close";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // pic
            //
            this.pic.BackColor = Color.FromArgb(33, 37, 41);
            this.pic.Dock = DockStyle.Fill;
            this.pic.SizeMode = PictureBoxSizeMode.Zoom;
            //
            // lblNone
            //
            this.lblNone.Dock = DockStyle.Fill;
            this.lblNone.Font = new Font("Segoe UI", 12F);
            this.lblNone.ForeColor = Color.FromArgb(108, 117, 125);
            this.lblNone.Text = "No softcopy saved for this record.";
            this.lblNone.TextAlign = ContentAlignment.MiddleCenter;
            this.lblNone.Visible = false;
            //
            // SoftcopyViewer
            //
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.ClientSize = new Size(720, 900);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Softcopy — Original Document";
            this.Controls.Add(this.pic);
            this.Controls.Add(this.lblNone);
            this.Controls.Add(this.bar);
            this.ResumeLayout(false);
        }
    }
}
