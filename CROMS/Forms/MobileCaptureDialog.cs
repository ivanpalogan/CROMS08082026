using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// One Mobile Capture session, shared by every place that starts one: the Marriage
    /// Registration wizard (Form 97) and the Intelligent Document Processing window.
    ///
    /// The phone is only a CAMERA here - it photographs the documents and uploads them; it
    /// reads nothing. This dialog shows the QR, watches the upload arrive and splits the pages
    /// by document (migration 66): step 1 the certificate OCR will read, step 2 the Marriage
    /// License, kept as a picture. "Continue to OCR Review" opens once a certificate page is
    /// in, and hands both images back to the caller in <see cref="Docs"/>.
    /// </summary>
    public partial class MobileCaptureDialog : Form
    {
        private readonly string _purpose;
        private readonly int? _marriageId, _txnId;
        private readonly string _husband, _wife, _txnCode;
        private int _seenCert = -1, _seenLic = -1;

        /// <summary>The capture token behind this session (null if it could not be created).</summary>
        public string Token { get; private set; }

        /// <summary>What the phone sent, sorted by document. Set when the dialog returns OK.</summary>
        public Form97Capture.CapturedDocs Docs { get; private set; }

        public MobileCaptureDialog(string purpose, int? marriageId, int? txnId,
            string husband, string wife, string txnCode)
        {
            InitializeComponent();
            _purpose = purpose ?? Form97Capture.PurposeIncoming;
            _marriageId = marriageId; _txnId = txnId;
            _husband = Blank(husband); _wife = Blank(wife); _txnCode = Blank(txnCode);

            bool ocrOnly = _purpose == Form97Capture.PurposeOcrCapture;
            lblTitle.Text = ocrOnly ? "Mobile Capture  -  Document" : "Mobile Capture  -  Marriage Registration";
            lblStep1.Text = ocrOnly ? "1.  Certificate (Birth, Marriage or Death)" : "1.  Certificate of Marriage (required)";
            lblStep2.Text = ocrOnly ? "2.  Marriage License (marriage only, optional)" : "2.  Marriage License (optional)";
            if (!ocrOnly && (_husband != null || _wife != null || _txnCode != null))
                lblSub.Text = "Transaction: " + (_txnCode ?? "not yet linked") + "     Husband: " + (_husband ?? "-") +
                              "     Wife: " + (_wife ?? "-") + "\nScan the QR with the office phone. The phone only takes the pictures - CROMS reads them here.";

            UiTheme.Polish(this);
            btnContinue.Enabled = false;
            Load += (s, e) => StartSession();
            FormClosed += (s, e) =>
            {
                pollTimer.Stop();
                DisposeImage(picQr); DisposeImage(picCert); DisposeImage(picLic);
            };
        }

        private static string Blank(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private void StartSession()
        {
            try
            {
                Token = Form97Capture.CreateToken(_marriageId, _txnId, _husband, _wife, _txnCode, 20, _purpose);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not start a mobile capture session:\n" + ex.Message,
                    "Mobile Capture", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.Cancel;
                return;
            }

            string url = Form97Capture.BuildMobileUrl(Token);
            txtUrl.Text = url;
            lblCode.Text = Token.Substring(0, 8).ToUpperInvariant();
            Bitmap qr = QrHelper.TryCreate(url, 6);
            if (qr != null) picQr.Image = qr;
            else { picQr.Visible = false; lblNoQr.Visible = true; }
            txtUrl.SelectionStart = 0; txtUrl.SelectionLength = 0;
            ActiveControl = btnCancel;

            pollTimer.Start();
        }

        private void pollTimer_Tick(object sender, EventArgs e)
        {
            if (Token == null) return;
            Form97Capture.Status st = Form97Capture.GetStatus(Token);
            if (st.Expired && st.PageCount == 0)
            {
                lblHint.Text = "This capture code has expired. Cancel and start a new one.";
                lblHint.ForeColor = UiTheme.Danger;
            }
            if (st.CertificatePages == _seenCert && st.LicensePages == _seenLic) return;
            _seenCert = st.CertificatePages; _seenLic = st.LicensePages;

            Form97Capture.CapturedDocs docs;
            try { docs = Form97Capture.FetchDocs(Token); }
            catch { return; } // DB blip - next tick tries again
            Docs = docs;

            ShowPage(picCert, docs.PrimaryCertificate);
            ShowPage(picLic, docs.PrimaryLicense);
            lblStep1State.Text = docs.Certificate.Count == 0 ? "Waiting for the phone..."
                : "Received  ✓  (" + docs.Certificate.Count + (docs.Certificate.Count == 1 ? " page)" : " pages - newest is used)");
            lblStep1State.ForeColor = docs.Certificate.Count == 0 ? UiTheme.Muted : UiTheme.Success;
            lblStep2State.Text = docs.License.Count == 0 ? "Not taken yet - you can skip this."
                : "Received  ✓  (" + docs.License.Count + (docs.License.Count == 1 ? " page)" : " pages)");
            lblStep2State.ForeColor = docs.License.Count == 0 ? UiTheme.Muted : UiTheme.Success;

            btnContinue.Enabled = docs.Certificate.Count > 0;
            lblHint.ForeColor = UiTheme.Muted;
            lblHint.Text = docs.Certificate.Count == 0
                ? "Keep this window open. Each photo appears here the moment the phone uploads it."
                : docs.License.Count == 0
                    ? "Certificate received. Photograph the Marriage License too if the client has it, or continue now."
                    : "Both documents received. Continue to review the OCR reading.";
        }

        private static void ShowPage(PictureBox pic, byte[] bytes)
        {
            DisposeImage(pic);
            if (bytes == null) return;
            try { using (var ms = new MemoryStream(bytes)) pic.Image = new Bitmap(Image.FromStream(ms)); }
            catch { /* a page that will not decode shows as blank; OCR will report it */ }
        }

        private static void DisposeImage(PictureBox pic)
        {
            Image old = pic.Image;
            pic.Image = null;
            old?.Dispose();
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            // One last read so a page uploaded between ticks is not left behind.
            try { Docs = Form97Capture.FetchDocs(Token); } catch { }
            if (Docs == null || Docs.PrimaryCertificate == null)
            {
                MessageBox.Show(this, "No certificate page has been received yet.", "Mobile Capture",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            pollTimer.Stop();
            Form97Capture.Complete(Token);
            DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            pollTimer.Stop();
            if (Token != null) Form97Capture.Complete(Token);
            DialogResult = DialogResult.Cancel;
        }
    }
}
