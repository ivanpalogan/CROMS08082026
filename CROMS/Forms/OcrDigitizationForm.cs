using System;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// OCR Digitization — scan a registry-book page, run Tesseract OCR (via
    /// <see cref="OcrService"/>, based on the team's old engine), review/correct the
    /// extracted fields, then commit the record to the birth registry. Every OCR run
    /// is logged in `ocr_batch`. Controls are placed in the Designer.
    /// </summary>
    public partial class OcrDigitizationForm : Form
    {
        private Bitmap _image;
        private float _zoom = 1f;
        private string _mode = "Backlog Digitization";
        private string _scanId;

        public OcrDigitizationForm()
        {
            InitializeComponent();
            LoadBatch();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp)|*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp"
            })
            {
                if (ofd.ShowDialog() != DialogResult.OK) return;

                _image?.Dispose();
                _image = new Bitmap(ofd.FileName);
                _zoom = 1f;
                pbScan.SizeMode = PictureBoxSizeMode.Zoom;
                pbScan.Size = pnlScanHost.ClientSize;
                pbScan.Image = _image;
                grpScan.Text = "SCANNED DOCUMENT — " + System.IO.Path.GetFileName(ofd.FileName);
            }
        }

        private void btnRunOcr_Click(object sender, EventArgs e) => RunOcr();
        private void btnReocr_Click(object sender, EventArgs e) => RunOcr();

        private void RunOcr()
        {
            if (_image == null)
            {
                MessageBox.Show("Load a scanned image first.", "OCR",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!DocumentAI.IsAvailable())
            {
                MessageBox.Show(
                    "Tesseract language data (eng.traineddata) was not found.\n" +
                    "Install Tesseract-OCR or place tessdata\\eng.traineddata next to the app.",
                    "OCR unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                // Shared engine: same classification + PSA Form-102-tuned extraction as the
                // Document AI module, so both screens read the same fields the same way.
                DocAiResult r = DocumentAI.Analyze(_image);
                Cursor = Cursors.Default;

                if (!string.IsNullOrEmpty(r.Error))
                {
                    MessageBox.Show(r.Error, "OCR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                txtDocClass.Text = r.Kind == DocKind.Birth
                    ? "Certificate of Live Birth (MF-102)"
                    : DocumentAI.KindName(r.Kind);
                FillFromDocAi(r);

                int conf = r.OcrConfidence;
                lblConf.Text = "CONFIDENCE  " + conf + "%";
                bool low = conf < 70;
                lblConf.BackColor = low ? Color.FromArgb(255, 243, 205) : Color.FromArgb(212, 237, 218);
                lblConf.ForeColor = low ? Color.FromArgb(133, 100, 4) : Color.FromArgb(25, 135, 84);
                lblFieldConf.Text = "FIELD CONFIDENCE  —  overall " + conf +
                    "%. Review highlighted fields before committing.";

                LogBatch(conf, low ? "Low Confidence" : "For Review", r.RawText);
                LoadBatch();
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                MessageBox.Show("OCR failed: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Map the Document-AI extracted fields onto this screen's textboxes.</summary>
        private void FillFromDocAi(DocAiResult r)
        {
            var m = r.Map();

            // Clear the review fields first so stale values from a prior scan don't linger.
            foreach (var t in new[] { txtRegNo, txtYear, txtFirst, txtMiddle, txtLast,
                                       txtSex, txtDob, txtPlace, txtMother, txtFather })
                t.Clear();

            void S(TextBox t, string key) { if (m.TryGetValue(key, out string v)) t.Text = v; }
            S(txtRegNo, "RegistryNo");
            S(txtFirst, "ChildFirst");
            S(txtMiddle, "ChildMiddle");
            S(txtLast, "ChildLast");
            S(txtSex, "Sex");
            S(txtDob, "DateOfBirth");
            S(txtPlace, "PlaceOfBirth");

            txtMother.Text = JoinName(m, "MotherFirst", "MotherMiddle", "MotherLast");
            txtFather.Text = JoinName(m, "FatherFirst", "FatherMiddle", "FatherLast");

            if (m.TryGetValue("DateOfBirth", out string dob) && dob.Length >= 4)
                txtYear.Text = dob.Substring(0, 4);
        }

        private static string JoinName(System.Collections.Generic.IDictionary<string, string> m,
            params string[] keys)
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (string k in keys)
                if (m.TryGetValue(k, out string v) && !string.IsNullOrWhiteSpace(v)) parts.Add(v.Trim());
            return string.Join(" ", parts);
        }

        private void LogBatch(int confidence, string status, string rawText)
        {
            int n = Db.GetCount("SELECT id FROM ocr_batch WHERE DATE(created_at) = CURDATE()") + 1;
            _scanId = "SCN-" + DateTime.Now.ToString("yyMMdd") + "-" + n.ToString("D3");

            Db.Push(
                "INSERT INTO ocr_batch (scan_id, source_book, doc_class, confidence, status, raw_text) " +
                "VALUES (@id, @book, @class, @conf, @status, @raw)",
                new MySqlParameter("@id", _scanId),
                new MySqlParameter("@book", grpScan.Text.Replace("SCANNED DOCUMENT — ", "")),
                new MySqlParameter("@class", "COLB (MF-102)"),
                new MySqlParameter("@conf", confidence),
                new MySqlParameter("@status", status),
                new MySqlParameter("@raw", rawText));
        }

        private void btnCommit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFirst.Text) || string.IsNullOrWhiteSpace(txtLast.Text))
            {
                MessageBox.Show("Child first name and last name are required to commit.",
                    "Missing data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SaveBirth("Registered");
            if (_scanId != null)
                Db.Push("UPDATE ocr_batch SET status = 'Committed' WHERE scan_id = @id",
                    new MySqlParameter("@id", _scanId));
            LoadBatch();
            MessageBox.Show("Committed to the birth registry.", "OCR",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnDraft_Click(object sender, EventArgs e)
        {
            SaveBirth("Draft");
            MessageBox.Show("Saved as draft in the birth registry.", "OCR",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SaveBirth(string status)
        {
            object dob = DateTime.TryParse(txtDob.Text, out DateTime d) ? (object)d.Date : DBNull.Value;
            object sex = txtSex.Text.StartsWith("F", StringComparison.OrdinalIgnoreCase) ? "Female"
                       : txtSex.Text.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "Male"
                       : (object)DBNull.Value;

            Db.Push(
                "INSERT INTO births (registry_no, book_volume, status, first_name, middle_name, " +
                "last_name, sex, date_of_birth, place_of_birth, mother_last_name, father_last_name) " +
                "VALUES (@reg, @book, @status, @fn, @mn, @ln, @sex, @dob, @place, @mother, @father)",
                new MySqlParameter("@reg", NullIfEmpty(txtRegNo.Text)),
                new MySqlParameter("@book", NullIfEmpty(txtYear.Text)),
                new MySqlParameter("@status", status),
                new MySqlParameter("@fn", txtFirst.Text.Trim()),
                new MySqlParameter("@mn", NullIfEmpty(txtMiddle.Text)),
                new MySqlParameter("@ln", txtLast.Text.Trim()),
                new MySqlParameter("@sex", sex),
                new MySqlParameter("@dob", dob),
                new MySqlParameter("@place", NullIfEmpty(txtPlace.Text)),
                new MySqlParameter("@mother", NullIfEmpty(txtMother.Text)),
                new MySqlParameter("@father", NullIfEmpty(txtFather.Text)));
        }

        private void LoadBatch()
        {
            dgvBatch.DataSource = Db.Pull(
                "SELECT scan_id AS 'Scan ID', source_book AS 'Source Book', doc_class AS Class, " +
                "CONCAT(confidence, '%') AS Conf, status AS Status " +
                "FROM ocr_batch WHERE DATE(created_at) = CURDATE() ORDER BY id DESC");
        }

        // ---- image tools ----
        private void btnZoomIn_Click(object sender, EventArgs e) => Zoom(1.25f);
        private void btnZoomOut_Click(object sender, EventArgs e) => Zoom(0.8f);

        private void Zoom(float factor)
        {
            if (_image == null) return;
            _zoom = Math.Max(0.25f, Math.Min(6f, _zoom * factor));
            pbScan.Size = new Size(
                (int)(pnlScanHost.ClientSize.Width * _zoom),
                (int)(pnlScanHost.ClientSize.Height * _zoom));
        }

        private void btnDeskew_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Auto-deskew isn't enabled yet — straighten the page on the scanner for best OCR.",
                "Deskew", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ModeButton_Click(object sender, EventArgs e)
        {
            _mode = ((Control)sender).Tag.ToString();
            foreach (var b in new[] { btnMode1, btnMode2, btnMode3 })
            {
                bool active = b == sender;
                b.BackColor = active ? Color.FromArgb(13, 110, 253) : Color.White;
                b.ForeColor = active ? Color.White : Color.Black;
                b.Font = new Font("Segoe UI", 9F, active ? FontStyle.Bold : FontStyle.Regular);
            }
        }

        private static object NullIfEmpty(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();
        }
    }
}
