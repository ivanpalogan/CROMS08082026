using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;

namespace CROMS.Forms
{
    /// <summary>
    /// Views the stored softcopy (the original scanned certificate image) of a registry
    /// record and prints it 1:1 on a full page — so staff can re-print the exact original
    /// document. Reused by Birth / Marriage / Death. Code-built (no Designer needed).
    /// </summary>
    public class SoftcopyViewer : Form
    {
        private readonly Image _image;

        public SoftcopyViewer(byte[] bytes, string caption)
        {
            Text = caption ?? "Softcopy — Original Document";
            StartPosition = FormStartPosition.CenterParent;
            Width = 720; Height = 900;
            BackColor = Color.FromArgb(248, 249, 250);

            try
            {
                if (bytes != null && bytes.Length > 0)
                    using (var ms = new MemoryStream(bytes)) _image = Image.FromStream(ms);
            }
            catch { _image = null; }

            var bar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White };
            var btnPrint = new Button
            {
                Text = "🖨  Print Original",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(13, 110, 253),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(12, 9), Size = new Size(160, 34)
            };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += (s, e) => Print();
            btnPrint.Enabled = _image != null;

            var btnClose = new Button
            {
                Text = "Close",
                Font = new Font("Segoe UI", 10F),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(180, 9), Size = new Size(100, 34)
            };
            btnClose.Click += (s, e) => Close();
            bar.Controls.Add(btnPrint);
            bar.Controls.Add(btnClose);

            var pic = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(33, 37, 41),
                Image = _image
            };
            if (_image == null)
            {
                var lbl = new Label
                {
                    Text = "No softcopy saved for this record.",
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 12F), ForeColor = Color.FromArgb(108, 117, 125)
                };
                Controls.Add(lbl);
            }
            else Controls.Add(pic);

            Controls.Add(bar);
        }

        private void Print()
        {
            if (_image == null) return;
            using (var doc = new PrintDocument())
            {
                doc.PrintPage += (s, e) =>
                {
                    Rectangle area = e.MarginBounds;
                    // Scale the image to fit the printable page, preserving aspect ratio.
                    float scale = Math.Min((float)area.Width / _image.Width, (float)area.Height / _image.Height);
                    int w = (int)(_image.Width * scale), h = (int)(_image.Height * scale);
                    int x = area.Left + (area.Width - w) / 2, y = area.Top + (area.Height - h) / 2;
                    e.Graphics.DrawImage(_image, x, y, w, h);
                };
                using (var dlg = new PrintDialog { Document = doc })
                    if (dlg.ShowDialog() == DialogResult.OK) doc.Print();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _image?.Dispose();
            base.OnFormClosed(e);
        }

        /// <summary>Convenience: load a record's softcopy from a table and show it. Returns
        /// false (and shows a note) when the record has no stored image.</summary>
        public static void Show(byte[] bytes, string caption, IWin32Window owner)
        {
            using (var v = new SoftcopyViewer(bytes, caption)) v.ShowDialog(owner);
        }
    }
}
