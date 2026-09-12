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
    /// document. Reused by Birth / Marriage / Death. UI lives in SoftcopyViewer.Designer.cs.
    /// </summary>
    public partial class SoftcopyViewer : Form
    {
        private readonly Image _image;

        public SoftcopyViewer(byte[] bytes, string caption)
        {
            InitializeComponent();
            Text = caption ?? "Softcopy — Original Document";

            try
            {
                if (bytes != null && bytes.Length > 0)
                    using (var ms = new MemoryStream(bytes)) _image = Image.FromStream(ms);
            }
            catch { _image = null; }

            if (_image == null)
            {
                lblNone.Visible = true;
                pic.Visible = false;
                btnPrint.Enabled = false;
            }
            else
            {
                pic.Image = _image;
            }
        }

        private void btnClose_Click(object sender, EventArgs e) => Close();

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (_image == null) return;
            using (var doc = new PrintDocument())
            {
                doc.PrintPage += (s, ev) =>
                {
                    Rectangle area = ev.MarginBounds;
                    // Scale the image to fit the printable page, preserving aspect ratio.
                    float scale = Math.Min((float)area.Width / _image.Width, (float)area.Height / _image.Height);
                    int w = (int)(_image.Width * scale), h = (int)(_image.Height * scale);
                    int x = area.Left + (area.Width - w) / 2, y = area.Top + (area.Height - h) / 2;
                    ev.Graphics.DrawImage(_image, x, y, w, h);
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

        /// <summary>Convenience: load a record's softcopy from a table and show it.</summary>
        public static void Show(byte[] bytes, string caption, IWin32Window owner)
        {
            using (var v = new SoftcopyViewer(bytes, caption)) v.ShowDialog(owner);
        }
    }
}
