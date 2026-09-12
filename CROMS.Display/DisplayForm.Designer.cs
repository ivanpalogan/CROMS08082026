using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Display
{
    /// <summary>Designer-side of <see cref="DisplayForm"/> (static chrome; cards built in code).</summary>
    public partial class DisplayForm
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel _grid;
        private Label _header;
        private Label _clock;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._grid = new TableLayoutPanel();
            this._header = new Label();
            this._clock = new Label();
            this.SuspendLayout();
            //
            // _grid
            //
            this._grid.BackColor = Color.Transparent;
            this._grid.Dock = DockStyle.Fill;
            this._grid.Padding = new Padding(40);
            //
            // _header
            //
            this._header.Dock = DockStyle.Top;
            this._header.Font = new Font("Segoe UI", 40F, FontStyle.Bold);
            this._header.ForeColor = Color.White;
            this._header.Height = 150;
            this._header.Text = "NOW SERVING";
            this._header.TextAlign = ContentAlignment.MiddleCenter;
            //
            // _clock
            //
            this._clock.Dock = DockStyle.Bottom;
            this._clock.Font = new Font("Segoe UI", 14F);
            this._clock.ForeColor = Color.FromArgb(148, 163, 184);
            this._clock.Height = 50;
            this._clock.TextAlign = ContentAlignment.MiddleCenter;
            //
            // DisplayForm
            //
            this.BackColor = Color.FromArgb(17, 24, 39);
            this.FormBorderStyle = FormBorderStyle.None;
            this.KeyPreview = true;
            this.Text = "CROMS — Now Serving";
            this.WindowState = FormWindowState.Maximized;
            // Fill first, then the docked edges, so the grid fills the middle.
            this.Controls.Add(this._grid);
            this.Controls.Add(this._clock);
            this.Controls.Add(this._header);
            this.ResumeLayout(false);
        }
    }
}
