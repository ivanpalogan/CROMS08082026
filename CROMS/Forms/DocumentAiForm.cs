using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Document AI — Auto-Fill. Upload a Birth/Marriage/Death certificate image; CROMS
    /// runs offline OCR (Tesseract), classifies the document by its labels, extracts the
    /// fields, shows a confidence score and an editable review grid (uncertain values
    /// flagged), then auto-fills the matching registration form. No manual re-typing.
    /// <para/>
    /// UI layout lives in DocumentAiForm.Designer.cs; this file holds only the logic.
    /// All recognition work is in <see cref="DocumentAI"/> so new document types plug in
    /// without touching this screen.
    /// </summary>
    public partial class DocumentAiForm : Form, IRefreshable
    {
        private DocAiResult _result;
        private byte[] _scanBytes;   // the original uploaded file, saved as the record's softcopy

        public DocumentAiForm()
        {
            InitializeComponent();
            BuildGrid();
            bool ok = DocumentAI.IsAvailable();
            lblEngine.Text = ok ? "OCR engine: ready" : "OCR engine: NOT FOUND (install Tesseract eng data)";
            lblEngine.ForeColor = ok ? Color.FromArgb(25, 135, 84) : Color.FromArgb(220, 53, 69);
        }

        public void RefreshData() { /* nothing to reload — driven by upload */ }

        private void BuildGrid()
        {
            dgvFields.AutoGenerateColumns = false;
            dgvFields.Columns.Clear();
            dgvFields.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Field", HeaderText = "Field", ReadOnly = true, FillWeight = 40 });
            dgvFields.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Value", HeaderText = "Extracted Value (editable)", FillWeight = 48 });
            dgvFields.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Status", HeaderText = "", ReadOnly = true, FillWeight = 12 });
        }

        private async void btnUpload_Click(object sender, EventArgs e)
        {
            if (!DocumentAI.IsAvailable())
            {
                MessageBox.Show("The OCR engine (Tesseract 'eng' language data) is not available on this PC.",
                    "Document AI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var ofd = new OpenFileDialog
            {
                Title = "Upload document",
                Filter = "Documents (*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.pdf)|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.pdf|All files (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog() != DialogResult.OK) return;
                await AnalyzeFile(ofd.FileName);
            }
        }

        private async Task AnalyzeFile(string path)
        {
            Bitmap image;
            try
            {
                _scanBytes = System.IO.File.ReadAllBytes(path);   // keep the ORIGINAL file as the softcopy
            }
            catch { _scanBytes = null; }

            try
            {
                image = DocumentAI.LoadImage(path);   // throws for PDF / unsupported
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(ex.Message, "Document AI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open the image: " + ex.Message, "Document AI",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            pic.Image?.Dispose();
            pic.Image = new Bitmap(image);

            SetBusy(true);
            DocAiResult result;
            try
            {
                result = await Task.Run(() => DocumentAI.Analyze(image));
            }
            finally
            {
                image.Dispose();
                SetBusy(false);
            }

            _result = result;
            ShowResult(result);
        }

        private void SetBusy(bool busy)
        {
            progress.Visible = busy;
            btnUpload.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            if (busy) { lblDetected.Text = "Analyzing…"; btnAutoFill.Enabled = false; }
        }

        private void ShowResult(DocAiResult r)
        {
            if (!string.IsNullOrEmpty(r.Error))
            {
                lblDetected.Text = "Document Detected: —";
                MessageBox.Show(r.Error, "Document AI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (r.Kind == DocKind.Unknown)
            {
                lblDetected.Text = "Document Detected: Unknown";
                lblDetected.ForeColor = Color.FromArgb(220, 53, 69);
                lblConf.Text = "Recognition: " + r.OcrConfidence + "%";
                dgvFields.Rows.Clear();
                btnAutoFill.Enabled = false;
                MessageBox.Show(
                    "Unable to identify the uploaded document. Please upload a valid Birth, " +
                    "Marriage, or Death Certificate.",
                    "Document AI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblDetected.Text = "Document Detected:  ✔ " + DocumentAI.KindName(r.Kind);
            lblDetected.ForeColor = Color.FromArgb(25, 135, 84);
            lblConf.Text = "Classification: " + r.ClassifyConfidence + "%    Recognition: " + r.OcrConfidence +
                           "%    Extracted: " + r.ExtractedCount + "    Missing: " + r.MissingCount;

            FillGrid(r);

            btnAutoFill.Enabled = true;
            switch (r.Kind)
            {
                case DocKind.Birth:    btnAutoFill.Text = "Auto-Fill Birth Form";    break;
                case DocKind.Marriage: btnAutoFill.Text = "Auto-Fill Marriage Form"; break;
                case DocKind.Death:    btnAutoFill.Text = "Auto-Fill Death Form";    break;
                default:               btnAutoFill.Text = "Auto-Fill"; btnAutoFill.Enabled = false; break;
            }

            if (r.OcrConfidence < 90)
                MessageBox.Show(
                    "Recognition confidence is below 90% (" + r.OcrConfidence + "%). Please review the " +
                    "extracted fields — values flagged ⚠ are uncertain — and correct them before auto-filling.",
                    "Please review", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void FillGrid(DocAiResult r)
        {
            dgvFields.Rows.Clear();
            foreach (DocField f in r.Fields)
            {
                int i = dgvFields.Rows.Add(f.Label, f.Value,
                    string.IsNullOrWhiteSpace(f.Value) ? "—" : (f.Uncertain ? "⚠" : "✔"));
                DataGridViewRow row = dgvFields.Rows[i];
                row.Tag = f.Key;
                if (string.IsNullOrWhiteSpace(f.Value))
                    row.Cells["Status"].Style.ForeColor = Color.FromArgb(173, 181, 189);
                else if (f.Uncertain)
                {
                    row.Cells["Status"].Style.ForeColor = Color.FromArgb(255, 153, 0);
                    row.Cells["Value"].Style.BackColor = Color.FromArgb(255, 248, 225);   // amber highlight
                }
                else
                    row.Cells["Status"].Style.ForeColor = Color.FromArgb(25, 135, 84);
            }
        }

        /// <summary>Collect the (possibly operator-edited) grid values by canonical key.</summary>
        private Dictionary<string, string> GridValues()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in dgvFields.Rows)
            {
                if (!(row.Tag is string key)) continue;
                string val = row.Cells["Value"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(val)) d[key] = val.Trim();
            }
            return d;
        }

        private void btnAutoFill_Click(object sender, EventArgs e)
        {
            if (_result == null) return;

            MainForm shell = Shell();
            if (shell == null) return;

            var vals = GridValues();
            LearnFromExtraction(_result.Kind, vals);   // remember the (operator-corrected) values
            string note = "Review every field, complete the rest, then Save.";

            if (_result.Kind == DocKind.Birth)
            {
                Form form = shell.GoToModule("birth");
                if (form is BirthRegistrationForm birth)
                {
                    birth.SetScanImage(_scanBytes);
                    birth.PrimeFromExtraction(vals);
                    MessageBox.Show("The Birth Registration form has been filled from the document. " +
                        "The scanned copy will be saved with the record when you Save. " + note,
                        "Auto-filled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else if (_result.Kind == DocKind.Death)
            {
                Form form = shell.GoToModule("death");
                if (form is DeathRegistrationForm death)
                {
                    death.SetScanImage(_scanBytes);
                    death.PrimeFromExtraction(vals);
                    MessageBox.Show("The Death Registration form has been filled from the document. " +
                        "The scanned copy will be saved with the record when you Save. " + note,
                        "Auto-filled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else if (_result.Kind == DocKind.Marriage)
            {
                // Marriage data entry is a modal dialog (Municipal Form 97).
                using (var dlg = new MarriageEntryForm(null))
                {
                    dlg.SetScanImage(_scanBytes);
                    dlg.PrimeFromExtraction(vals);
                    dlg.ShowDialog(this);
                }
                shell.GoToModule("marriage");   // refresh the overview after the dialog closes
            }
        }

        /// <summary>
        /// Feed the (operator-corrected) extracted values into the shared Learning Library
        /// under the right categories, so new places/hospitals/names/etc. become available
        /// everywhere and corrected spellings win on future scans.
        /// </summary>
        private static void LearnFromExtraction(DocKind kind, Dictionary<string, string> v)
        {
            void L(string key, string category)
            {
                if (v.TryGetValue(key, out string val)) LearningLibrary.Learn(category, val);
            }

            switch (kind)
            {
                case DocKind.Birth:
                    L("PlaceOfBirth", LearningLibrary.PlaceOfBirth);
                    L("PlaceHospital", LearningLibrary.Hospital);
                    L("PlaceMunicipality", LearningLibrary.Municipality);
                    L("PlaceProvince", LearningLibrary.Province);
                    L("ChildFirst", LearningLibrary.GivenName);
                    L("ChildLast", LearningLibrary.Surname);
                    L("FatherFirst", LearningLibrary.GivenName);
                    L("FatherLast", LearningLibrary.Surname);
                    L("MotherFirst", LearningLibrary.GivenName);
                    L("MotherLast", LearningLibrary.Surname);
                    L("MotherOccupation", LearningLibrary.Occupation);
                    L("FatherOccupation", LearningLibrary.Occupation);
                    L("Nationality", LearningLibrary.Nationality);
                    break;
                case DocKind.Marriage:
                    L("PlaceOfMarriage", LearningLibrary.PlaceOfMarriage);
                    L("PlaceOfMarriage", LearningLibrary.Church);
                    L("Solemnizer", LearningLibrary.Officer);
                    L("HusbandFirst", LearningLibrary.GivenName);
                    L("HusbandLast", LearningLibrary.Surname);
                    L("WifeFirst", LearningLibrary.GivenName);
                    L("WifeLast", LearningLibrary.Surname);
                    L("Nationality", LearningLibrary.Nationality);
                    break;
                case DocKind.Death:
                    L("PlaceOfDeath", LearningLibrary.PlaceOfDeath);
                    L("DeceasedFirst", LearningLibrary.GivenName);
                    L("DeceasedLast", LearningLibrary.Surname);
                    L("Citizenship", LearningLibrary.Nationality);
                    break;
            }
        }

        private MainForm Shell()
        {
            for (Control p = Parent; p != null; p = p.Parent)
                if (p is MainForm mf) return mf;
            return null;
        }
    }
}
