using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Intelligent Document Processing — the office's one document screen. Load a scanned
    /// registry page or a certificate a client brought in and the engine straightens it,
    /// reads it, decides what KIND of certificate it is from its own headings, labels and
    /// layout, extracts that certificate's fields, scores each one, repairs obvious
    /// misreadings, and checks the result against the rules of that form. The operator
    /// compares the grid against the scan, corrects what is wrong, and then either:
    /// <list type="bullet">
    /// <item><b>Commit / Draft</b> — writes a birth record straight into `births`;</item>
    /// <item><b>Auto-Fill</b> — hands the confirmed values to the Birth / Marriage / Death
    /// registration form, which owns that certificate's column mapping;</item>
    /// <item><b>Preview on Form</b> — draws the reviewed values onto the certificate they
    /// came off, watermarked and unsaved, so a value sitting in the wrong row is visible
    /// before it reaches the registry;</item>
    /// <item><b>Send to Manual Review</b> — parks an unreadable or unrecognised scan for a
    /// human to encode, with everything the engine saw kept on the record.</item>
    /// </list>
    /// Nothing reaches a registration form without the operator confirming it, an
    /// unidentified or low-confidence document cannot be committed at all, and every field
    /// is written to `ocr_field_audit` with what OCR originally read beside what was
    /// actually saved. Controls are placed in the Designer.
    /// </summary>
    public partial class OcrDigitizationForm : Form, IRefreshable
    {
        private Bitmap _image;              // what is displayed (already straightened)
        private byte[] _scanBytes;          // original file, kept as the record's softcopy
        private float _zoom = 1f;
        private string _scanId;
        private DocAiResult _result;
        private DocKind _kind = DocKind.Unknown;

        /// <summary>
        /// The form this scan was identified as — the entry in <see cref="FormCatalog"/>
        /// that tells CROMS which registry table and report view the record belongs to,
        /// which sections to show it in, and which Crystal report lays it out. Everything
        /// form-specific on this screen reads from here rather than switching on
        /// <see cref="_kind"/>, so a new certificate type needs no change in this file.
        /// </summary>
        private FormDefinition _formDef;

        /// <summary>The row this scan produced, once committed — what the Print
        /// Certificate button renders.</summary>
        private long? _savedRecordId;

        /// <summary>
        /// The seals and stamps found on this page. They are NOT text and must never
        /// become field values, so a reading that came from inside one is suppressed; and
        /// because a seal on a certificate is often the office's own, one can be captured
        /// straight off the scan as the logo or stamp used on printed certificates.
        /// </summary>
        private List<SealDetector.Seal> _seals = new List<SealDetector.Seal>();
        private bool _updatingFieldGrid;

        public OcrDigitizationForm()
        {
            InitializeComponent();
            dgvBatch.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            BuildFieldGrid();
            SetupScanQrButton();

            bool ok = DocumentAI.IsAvailable();
            lblEngine.Text = ok ? "OCR engine: ready" : "OCR engine: NOT FOUND (install Tesseract eng data)";
            lblEngine.ForeColor = ok ? Color.FromArgb(25, 135, 84) : Color.FromArgb(220, 53, 69);

            lblSubtitle.Text = "Load a scanned certificate or registry page. CROMS identifies the " +
                "document, extracts its fields with a confidence for each, and flags anything to check.";

            // Comparing the grid against the scan is the whole review step, so selecting a
            // field draws its box on the page.
            dgvFields.SelectionChanged += (s, e) => pbScan.Invalidate();
            dgvFields.CellEndEdit += DgvFields_CellEndEdit;
            dgvFields.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvFields.IsCurrentCellDirty && dgvFields.CurrentCell != null
                    && dgvFields.CurrentCell.OwningColumn.Name == "Verified")
                    dgvFields.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvFields.CellValueChanged += DgvFields_CellValueChanged;
            pbScan.Paint += PbScan_Paint;

            // A phone scan lands here first (uploaded via the save-API, no on-device OCR) —
            // double-clicking a still-unopened 'Mobile' row loads it into the engine the same
            // way Load Image does, then runs the full desktop pipeline on it.
            dgvBatch.CellDoubleClick += DgvBatch_CellDoubleClick;

            ApplyResultToUi();
            UpdateBatchModeButtons();
            LoadBatch();
        }

        public void RefreshData() { LoadBatch(); }

        // ---- mobile scanner QR ----------------------------------------------

        /// <summary>
        /// Adds a "Scan with Phone" button beside Load Image that opens the QR for the
        /// CROMS Mobile Scanner (ORCMobile_Application) — the same phone app used to
        /// capture a certificate in the field. Scanning it opens the app straight to its
        /// camera, so a page can be captured on the phone and reviewed here without a
        /// USB cable or emailing the photo.
        /// </summary>
        private void SetupScanQrButton()
        {
            var btn = new Button
            {
                Text = "📱 Scan with Phone",
                FlatStyle = FlatStyle.Flat,
                Font = btnLoad.Font,
                Size = new Size(190, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(btnLoad.Left - 200, btnLoad.Top),
                UseVisualStyleBackColor = true,
            };
            btn.Click += (s, e) => ShowScanQrDialog();
            Controls.Add(btn);
            btn.BringToFront();
        }

        /// <summary>
        /// The URL this PC's phone-scanner QR should point at. On the machine actually
        /// running the mobile server it is the live detected address; on a client PC (the
        /// server runs elsewhere) it is built from the saved server IP instead, so every
        /// station can show a working QR, not just the host.
        /// </summary>
        private static string ScanAppUrl()
        {
            var m = IonicServerManager.Instance;
            if (m.Status == IonicStatus.Running) return m.QrPayload;

            string host = ServerConfig.EffectiveHost;
            if (string.IsNullOrWhiteSpace(host) ||
                host == "localhost" || host == "127.0.0.1" || host == "::1")
                return null;

            string scheme = string.IsNullOrEmpty(m.Scheme) ? "https" : m.Scheme;
            int port = m.Port > 0 ? m.Port : 4200;
            return scheme + "://" + host + ":" + port;
        }

        /// <summary>
        /// Shows the phone-scanner QR and keeps it LIVE while the dialog is open: if the
        /// laptop switches network (hotspot to Wi-Fi, one Wi-Fi to another, or back), the
        /// detected address changes underneath <see cref="IonicServerManager"/> within its
        /// own ~10s poll, and this dialog re-renders the QR to match — no restart, no
        /// closing/reopening the dialog needed. Subscribes to the manager's own
        /// <c>Changed</c> event for an immediate update, plus a 3s poll as a fallback for
        /// the client-PC path (which reads a saved config, not a live-detected address).
        /// </summary>
        private void ShowScanQrDialog()
        {
            string lastUrl = null;

            using (var dlg = new Form
            {
                Text = "Scan with Phone",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new Size(300, 420),
            })
            {
                var title = new Label
                {
                    Text = "CROMS Mobile Scanner",
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(20, 16),
                };
                var lblNet = new Label
                {
                    Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(25, 135, 84),
                    AutoSize = false,
                    Size = new Size(260, 16),
                    Location = new Point(20, 42),
                    Text = "",
                };
                var pic = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Location = new Point(50, 62),
                    Size = new Size(200, 200),
                    Visible = false,
                };
                var lblMsg = new Label
                {
                    AutoSize = false,
                    Size = new Size(260, 70),
                    Location = new Point(20, 68),
                };
                var lblHint = new Label
                {
                    Text = "Scan with the phone camera to open the scanner and " +
                           "capture a document, or type this address:",
                    AutoSize = false,
                    Size = new Size(260, 40),
                    Location = new Point(20, 270),
                    Visible = false,
                };
                var txt = new TextBox
                {
                    ReadOnly = true,
                    Location = new Point(20, 314),
                    Size = new Size(260, 22),
                    Visible = false,
                };
                var lblFoot = new Label
                {
                    Text = "Connect the phone to the same Wi-Fi/hotspot as this PC — " +
                           "switching networks here updates the QR automatically.",
                    ForeColor = Color.FromArgb(108, 117, 125),
                    AutoSize = false,
                    Size = new Size(260, 46),
                    Location = new Point(20, 344),
                    Visible = false,
                };
                dlg.Controls.Add(title);
                dlg.Controls.Add(lblNet);
                dlg.Controls.Add(pic);
                dlg.Controls.Add(lblMsg);
                dlg.Controls.Add(lblHint);
                dlg.Controls.Add(txt);
                dlg.Controls.Add(lblFoot);

                IonicStatus lastStatus = (IonicStatus)(-1);

                Action refresh = () =>
                {
                    var m = IonicServerManager.Instance;
                    lblNet.Text = m.Status == IonicStatus.Running && !string.IsNullOrEmpty(m.NetworkType)
                        ? "Connected via " + m.NetworkType
                        : "";

                    string url = ScanAppUrl();

                    // Show the server's own state (Starting/Error) even while the URL itself
                    // hasn't changed (still null) — otherwise the dialog is stuck on a stale
                    // "isn't running" message for the ~30-60s the first-time build takes.
                    if (url == null && m.Status != lastStatus)
                    {
                        lastStatus = m.Status;
                        pic.Visible = false; lblHint.Visible = false; txt.Visible = false; lblFoot.Visible = false;
                        lblMsg.Visible = true;
                        lblMsg.Text = m.Status == IonicStatus.Starting
                            ? "Starting the mobile scanner server… this can take under a " +
                              "minute the first time. The QR code will appear automatically."
                            : m.Status == IonicStatus.Error
                                ? "The mobile scanner failed to start: " + m.LastError
                                : "The mobile scanner isn't running, and no server " +
                                  "address is configured on this PC yet.";
                    }

                    if (url == lastUrl) return;   // no change — leave the QR as-is
                    lastUrl = url;
                    lastStatus = m.Status;

                    if (url == null) return;   // message already set above

                    var bmp = QrHelper.TryCreate(url, 6);
                    var old = pic.Image;
                    if (bmp != null)
                    {
                        pic.Image = bmp;
                        pic.Visible = true;
                        lblMsg.Visible = false;
                    }
                    else
                    {
                        pic.Visible = false;
                        lblMsg.Visible = true;
                        lblMsg.Text = "(QRCoder not installed — type the address below.)";
                    }
                    if (old != null) old.Dispose();

                    txt.Text = url;
                    lblHint.Visible = true;
                    txt.Visible = true;
                    lblFoot.Visible = true;
                };

                refresh();

                Action changedHandler = () =>
                {
                    if (dlg.IsDisposed) return;
                    if (dlg.InvokeRequired) dlg.BeginInvoke(refresh);
                    else refresh();
                };
                IonicServerManager.Instance.Changed += changedHandler;

                var poll = new Timer { Interval = 3000 };
                poll.Tick += (s, e) => refresh();
                poll.Start();

                var close = new Button
                {
                    Text = "Close",
                    DialogResult = DialogResult.OK,
                    Location = new Point(dlg.ClientSize.Width - 100, dlg.ClientSize.Height - 40),
                    Size = new Size(80, 28),
                };
                dlg.Controls.Add(close);
                dlg.AcceptButton = close;

                dlg.FormClosed += (s, e) =>
                {
                    poll.Stop(); poll.Dispose();
                    IonicServerManager.Instance.Changed -= changedHandler;
                    if (pic.Image != null) pic.Image.Dispose();
                };

                dlg.ShowDialog(this);
            }
        }

        // ---- load ---------------------------------------------------------

        private void btnLoad_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog
            {
                Title = "Load scan / document",
                // PDF is deliberately absent: DocumentAI.LoadImage cannot read one, and
                // offering it in the filter only produces a rejection after the fact.
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp)|*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp"
            })
            {
                if (ofd.ShowDialog() != DialogResult.OK) return;

                Bitmap loaded;
                try { loaded = DocumentAI.LoadImage(ofd.FileName); }
                catch (NotSupportedException ex)
                {
                    MessageBox.Show(ex.Message, "Document", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not open the image: " + ex.Message, "Document",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _image?.Dispose();
                _image = loaded;
                try { _scanBytes = System.IO.File.ReadAllBytes(ofd.FileName); }
                catch { _scanBytes = null; }
                _zoom = 1f;
                pbScan.SizeMode = PictureBoxSizeMode.Zoom;
                pbScan.Size = pnlScanHost.ClientSize;
                pbScan.Image = _image;
                grpScan.Text = "SCANNED DOCUMENT — " + System.IO.Path.GetFileName(ofd.FileName);

                // A new page invalidates the previous reading.
                _scanId = null;
                _result = null;
                _kind = DocKind.Unknown;
                _formDef = null;
                _savedRecordId = null;
                _seals = new List<SealDetector.Seal>();
                txtDocClass.Clear();
                dgvFields.Rows.Clear();
                ApplyResultToUi();
            }
        }

        // ---- run the engine -------------------------------------------------

        private async void btnRunOcr_Click(object sender, EventArgs e) { await Analyze(); }
        private async void btnReocr_Click(object sender, EventArgs e) { await Analyze(); }

        private async Task Analyze()
        {
            if (_image == null)
            {
                MessageBox.Show("Load a scanned image first.", "Document",
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
                SetBusy(true);
                // Orientation probe + two resolution passes: seconds of work, off the UI thread.
                DocAiResult r = await Task.Run(() => DocumentAI.Analyze(_image));
                SetBusy(false);

                if (!string.IsNullOrEmpty(r.Error))
                {
                    MessageBox.Show(r.Error, "Document", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // The field boxes are measured on the STRAIGHTENED page, so the operator
                // has to be looking at the straightened page too.
                if (r.RotationApplied != 0)
                {
                    Bitmap turned = OcrService.Rotate(_image, r.RotationApplied);
                    _image.Dispose();
                    _image = turned;
                    pbScan.Image = _image;
                }

                _result = r;
                _kind = r.Kind;
                // Identify the FORM, not merely the kind of certificate: the office
                // receives several revisions of each, and the revision decides the field
                // positions, the sections and the report layout.
                _formDef = FormCatalog.Identify(r);
                _savedRecordId = null;

                // Seals and stamps are images, not text. Find them and refuse any value
                // that was read from inside one, before the operator ever sees the grid.
                _seals = SealDetector.Detect(_image);
                SuppressSealReadings(r);

                UpdateFormIdentity();
                txtDocClass.Text = ClassLabel(r.Kind) +
                    (r.Kind == DocKind.Unknown ? "" : "   —   detected " + r.ClassifyConfidence + "% sure") +
                    (r.RotationApplied != 0 ? "   —   page turned " + r.RotationApplied + "°" : "") +
                    (r.LayoutRejected == null ? "" : r.RescanRecommended
                        ? "   —   FORM CHECK AFTER RESCAN (closest " + (r.CandidateLayoutCode ?? "known form") + ")"
                        : "   —   POSSIBLE NEW FORM/REVISION (closest " + (r.CandidateLayoutCode ?? "known form") + ")") +
                    "   —   SCAN " + r.ScanQualityGrade + " " + r.ScanQualityScore + "%";

                _reviewNote = ReviewNote(r);
                FillFieldGrid(r);
                LogBatch(r);
                ApplyResultToUi();
                LoadBatch();

                // Deliberately NO pop-up here. Everything these used to say is already on
                // the screen the operator is looking at: the status line below names the
                // reason, the confidence badge is amber, every flagged row is highlighted
                // with its own reason, and Commit and Auto-Fill are disabled. A modal on top
                // of that adds nothing but a click - and it has to be dismissed BEFORE the
                // grid it tells you to read becomes reachable.
                //
                // The one thing genuinely NOT on screen - the long explanation of why a form
                // CROMS recognises was not read box by box - is kept, and offered on demand
                // from the status line.
            }
            catch (Exception ex)
            {
                SetBusy(false);
                _result = null;
                ApplyResultToUi();
                MessageBox.Show("OCR failed: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetBusy(bool busy)
        {
            progress.Visible = busy;
            btnLoad.Enabled = !busy;
            btnRunOcr.Enabled = !busy;
            btnReocr.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            if (busy)
            {
                lblFieldConf.Text = "Straightening the page, reading it at two resolutions, checking the fields…";
                btnAutoFill.Enabled = false;
                btnCommit.Enabled = false;
                btnDraft.Enabled = false;
                btnReview.Enabled = false;
            }
        }

        // ---- what the document allows ---------------------------------------

        /// <summary>
        /// The document decides which actions are live. Commit and Draft write into
        /// `births`, so they need a birth certificate that passed its checks. Auto-Fill
        /// needs a recognised class. Anything unidentified, or carrying a field that failed
        /// a rule, can only go to manual review until the operator fixes it in the grid.
        /// </summary>
        private void ApplyResultToUi()
        {
            bool have = _result != null && _result.Kind != DocKind.Unknown;
            bool blocked = _result == null || _result.NeedsManualReview;
            bool birth = have && _kind == DocKind.Birth;

            btnCommit.Enabled = birth && !blocked;
            btnDraft.Enabled = birth && !blocked;
            btnAutoFill.Enabled = have && !blocked;
            btnReview.Enabled = _result != null;

            btnAutoFill.Text = have ? "Auto-Fill " + ModuleName(_kind) : "Auto-Fill Form";
            btnSeal.Enabled = _seals.Count > 0 && _image != null;
            btnSeal.Text = _seals.Count == 0
                ? "Seal / Stamp"
                : "Seals & Signatures (" + _seals.Count + ")";
            dgvFields.ReadOnly = _result == null;

            if (_result == null)
            {
                _reviewNote = "";
                lblFieldConf.Cursor = Cursors.Default;
                grpFields.Text = "EXTRACTED FIELDS — LOAD A DOCUMENT AND RUN OCR";
                lblFieldConf.Text = "No document read yet.";
                lblFieldConf.ForeColor = Color.FromArgb(73, 80, 87);
                lblConf.Text = "CONFIDENCE  —";
                lblConf.BackColor = Color.FromArgb(233, 236, 239);
                lblConf.ForeColor = Color.FromArgb(73, 80, 87);
                return;
            }

            grpFields.Text = "EXTRACTED FIELDS — " +
                (have ? DocumentAI.KindName(_kind).ToUpperInvariant() : "UNCLASSIFIED") +
                ", EDIT ANY VALUE BEFORE SAVING";

            int conf = _result.OverallConfidence;
            bool operatorVerified = !_result.RescanRecommended
                && _result.Fields.Any(f => f.Required)
                && _result.Fields.Where(f => f.Required).All(f =>
                    f.EditedByUser && !string.IsNullOrWhiteSpace(f.Value)
                    && f.Status != FieldStatus.Invalid && f.Status != FieldStatus.Conflict);
            lblConf.Text = operatorVerified
                ? "OPERATOR VERIFIED  100%  (OCR " + conf + "%)"
                : "OCR CONFIDENCE  " + conf + "%";
            bool low = conf < DocIntelligence.ReviewBelow;
            lblConf.BackColor = operatorVerified || !low
                ? Color.FromArgb(212, 237, 218) : Color.FromArgb(255, 243, 205);
            lblConf.ForeColor = operatorVerified || !low
                ? Color.FromArgb(25, 135, 84) : Color.FromArgb(133, 100, 4);

            int flagged = _result.Fields.Count(f => f.Status == FieldStatus.Invalid
                                                 || f.Status == FieldStatus.Conflict);
            int weak = _result.Fields.Count(f => f.Status == FieldStatus.Uncertain);
            int corrected = _result.Fields.Count(f => f.Corrected);

            lblFieldConf.Text =
                (have ? DocumentAI.KindName(_kind).ToUpperInvariant() + " (class " + _result.ClassifyConfidence + "%)"
                      : "UNCLASSIFIED DOCUMENT") +
                "  —  SCAN " + _result.ScanQualityGrade + " " + _result.ScanQualityScore + "%" +
                "  —  " + _result.ExtractedCount + " read, " + _result.MissingCount + " blank, " +
                weak + " weak, " + flagged + " flagged" +
                (corrected > 0 ? ", " + corrected + " auto-corrected" : "") +
                (operatorVerified ? "  —  REQUIRED VALUES VERIFIED BY OPERATOR" : "") +
                (_seals.Count > 0 ? ", " + _seals.Count + " seal/signature region(s) kept as images, not text" : "") +
                (blocked ? "  —  MANUAL REVIEW: " + _result.ReviewReason : "");
            lblFieldConf.ForeColor = blocked ? Color.FromArgb(133, 100, 4) : Color.FromArgb(73, 80, 87);

            bool explain = !string.IsNullOrEmpty(_reviewNote);
            if (explain) lblFieldConf.Text += "   \u2014   click for details";
            lblFieldConf.Cursor = explain ? Cursors.Hand : Cursors.Default;
        }

        /// <summary>
        /// The full explanation behind the review flag, or "" when there is nothing to add
        /// beyond what the grid already shows. Shown only when the operator asks for it.
        /// </summary>
        private static string ReviewNote(DocAiResult r)
        {
            if (r == null) return "";

            if (r.RescanRecommended)
                return r.ScanQualityNote +
                       "\n\nThe scan stays in manual review and cannot be committed automatically.";

            if (r.Kind == DocKind.Unknown)
                return "The document type could not be identified from its headings, labels or " +
                       "layout.\n\nCheck the scan quality, or send it to manual review and encode " +
                       "it in the matching registration module.";

            if (r.NeedsManualReview)
                return "This document needs a closer look: " + r.ReviewReason + ".\n\n" +
                       (r.LayoutRejected == null ? "" : LayoutRejectedNote(r) + "\n\n") +
                       "Correct the flagged fields in the grid \u2014 the checks re-run as you " +
                       "type \u2014 or send the scan to manual review.";

            // Nothing is holding the document, but a refused layout still explains its blanks.
            return r.LayoutRejected == null ? "" : LayoutRejectedNote(r);
        }

        private string _reviewNote = "";

        /// <summary>
        /// The status line doubles as the way in to the long explanation, so the detail stays
        /// one click away instead of arriving uninvited after every scan.
        /// </summary>
        private void lblFieldConf_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_reviewNote)) return;
            MessageBox.Show(this, _reviewNote, "Why this scan is flagged",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Explain, in the operator's terms, why a certificate they recognise was not read
        /// box by box. CROMS knows the KIND of form from its headings, but the boxes it
        /// reads from are calibrated on one revision, and the office receives several —
        /// an older sheet, or a copy issued in a different layout, puts the same fields
        /// in different places. When the printed labels do not land where the template
        /// expects, reading it anyway would put a value from the wrong row into the
        /// registry, so CROMS falls back to following the labels themselves and leaves
        /// blank whatever it cannot follow.
        /// </summary>
        private static string LayoutRejectedNote(DocAiResult r)
        {
            if (r.RescanRecommended)
                return "CROMS found text from " + (r.CandidateLayoutCode ?? "a known certificate")
                     + ", but this scan is too unclear or misaligned to verify its boxes. Rescan it first; "
                     + "only treat it as a new form if the clearer scan still does not align.\n\n("
                     + r.LayoutRejected + ")";

            return "POSSIBLE NEW FORM OR REVISION. This layout is not in the CROMS form library — the boxes of the "
                 + "closest form it knows do not line up with this page, so it was read by "
                 + "following the printed labels instead.\n\n"
                 + "Anything the labels could not be followed to is left BLANK rather than "
                 + "guessed at. Type those in from the scan.\n\n(" + r.LayoutRejected + ")";
        }

        private static string ClassLabel(DocKind k)
        {
            switch (k)
            {
                case DocKind.Birth: return "COLB (MF-102)";
                case DocKind.Marriage: return "COM (MF-97)";
                case DocKind.Death: return "COD (MF-103)";
                default: return "Unclassified";
            }
        }

        private static string ModuleName(DocKind k)
        {
            switch (k)
            {
                case DocKind.Birth: return "Birth Registration";
                case DocKind.Marriage: return "Marriage Registration";
                case DocKind.Death: return "Death Registration";
                default: return "the matching module";
            }
        }

        /// <summary>
        /// Throw away any value that was read from inside a seal or stamp.
        /// <para/>
        /// Tesseract does not know a seal from a word: it reads the emblem as a few
        /// nonsense tokens, and when a seal sits over a field's box those tokens are
        /// returned as that field's value. A garbled word saved as a residence or as the
        /// solemnizing officer is the worst kind of error this project has — it looks
        /// filled in. The field is emptied and flagged so the operator types it, and the
        /// original reading is kept in <see cref="DocField.OcrValue"/> for the audit trail.
        /// </summary>
        private void SuppressSealReadings(DocAiResult r)
        {
            if (_seals.Count == 0 || r == null) return;

            foreach (DocField f in r.Fields)
            {
                if (string.IsNullOrWhiteSpace(f.Value)) continue;
                if (f.EditedByUser) continue;                 // the operator's word wins
                if (f.RegionNorm.IsEmpty) continue;           // nowhere to compare
                if (!SealDetector.OverlapsSeal(f.RegionNorm, _seals)) continue;

                if (string.IsNullOrEmpty(f.OcrValue)) f.OcrValue = f.Value;
                f.Value = "";
                f.Confidence = 0;
                f.Status = FieldStatus.Missing;
                f.Uncertain = true;
                f.Issue = "A seal, stamp or signature covers this box — what OCR read " +
                          "here (\"" + f.OcrValue + "\") is part of that ink, not a " +
                          "value. Type it in from the scan.";
            }

            // The verdicts and the overall confidence were computed before the suppression,
            // so they have to be recomputed against what is actually left.
            DocIntelligence.Revalidate(r);
        }

        /// <summary>
        /// Capture a seal or stamp found on this scan and keep it as an IMAGE — the
        /// office's logo or its stamp — rather than as OCR text. The seal on a certificate
        /// the office itself issued is usually the very emblem that should appear on the
        /// certificates CROMS prints, so this is the shortest path from "we have a good
        /// scan" to "our printed certificates carry our seal".
        /// </summary>
        private void btnSeal_Click(object sender, EventArgs e)
        {
            if (_image == null || _seals.Count == 0)
            {
                MessageBox.Show(
                    "No seal, stamp or signature was found on this page. Run OCR first — " +
                    "they are located during the same pass.",
                    "Seal / Stamp", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // The operator must SEE the crop before it becomes the office's seal. The
            // detector deliberately also catches signatures (they are ink that must not
            // become a value either), so on a page whose only mark is a signature, a
            // numbers-only prompt would happily let someone save a doctor's signature as
            // the municipal seal.
            using (var dlg = new SealPickerForm(_image, _seals))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Chosen == null) return;

                try
                {
                    AssetKind kind = dlg.SaveAsStamp ? AssetKind.Stamp : AssetKind.Logo;
                    // Saved office-wide (form_code null) and tagged with the scan it came
                    // from, so the source of the office's seal stays traceable.
                    OfficeAssets.Save(kind, kind + " captured from scan " + (_scanId ?? "—"),
                                      dlg.Chosen, null, "image/png", "Scan", _scanId);
                    Audit.Write(Audit.Update, "office_assets", 0,
                        kind + " captured from OCR scan " + _scanId);
                    MessageBox.Show(
                        "Saved as the office " + kind.ToString().ToLowerInvariant() +
                        ". It will appear on the next certificate printed.\n\n" +
                        "Replace or remove it in Settings → Certificates & Forms.",
                        "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not save: " + ex.Message, "Seal / Stamp",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Shows each detected ink mark as a picture and lets the operator keep one as the
        /// office logo or stamp. A modal dialog built in code, following the same
        /// convention as the other dialogs in this project.
        /// </summary>
        private class SealPickerForm : Form
        {
            private readonly Bitmap _page;
            private readonly List<SealDetector.Seal> _found;
            private readonly PictureBox _preview = new PictureBox();
            private readonly Label _caption = new Label();
            private readonly Button _prev = new Button();
            private readonly Button _next = new Button();
            private int _index;

            /// <summary>The chosen crop as PNG bytes, or null if nothing was chosen.</summary>
            public byte[] Chosen { get; private set; }
            public bool SaveAsStamp { get; private set; }

            public SealPickerForm(Bitmap page, List<SealDetector.Seal> found)
            {
                _page = page;
                _found = found;

                Text = "Seals, Stamps and Signatures Found on This Scan";
                StartPosition = FormStartPosition.CenterParent;
                ClientSize = new Size(520, 470);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = MinimizeBox = false;
                BackColor = UiTheme.PageBg;

                Controls.Add(new Label
                {
                    Text = "These marks were read as IMAGES, not as text, so nothing " +
                           "inside them was saved as a field value.\r\n" +
                           "If one of them is the office's own seal or stamp, keep it here " +
                           "and it will be printed on certificates.",
                    Location = new Point(16, 12),
                    Size = new Size(488, 44),
                    Font = new Font("Segoe UI", 9f),
                    ForeColor = UiTheme.Muted
                });

                _preview.SetBounds(16, 64, 488, 260);
                _preview.SizeMode = PictureBoxSizeMode.Zoom;
                _preview.BackColor = UiTheme.Surface;
                _preview.BorderStyle = BorderStyle.FixedSingle;
                Controls.Add(_preview);

                _caption.SetBounds(16, 330, 488, 20);
                _caption.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                _caption.ForeColor = UiTheme.Ink;
                _caption.TextAlign = ContentAlignment.MiddleCenter;
                Controls.Add(_caption);

                _prev.SetBounds(16, 356, 100, 30);
                _prev.Text = "◀ Previous";
                _prev.FlatStyle = FlatStyle.Flat;
                _prev.Click += (s, e) => Step(-1);
                Controls.Add(_prev);

                _next.SetBounds(404, 356, 100, 30);
                _next.Text = "Next ▶";
                _next.FlatStyle = FlatStyle.Flat;
                _next.Click += (s, e) => Step(1);
                Controls.Add(_next);

                var stamp = new Button
                {
                    Text = "Keep as office STAMP",
                    Bounds = new Rectangle(16, 400, 190, 36),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = UiTheme.Accent,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold)
                };
                stamp.Click += (s, e) => Take(true);
                Controls.Add(stamp);

                var logo = new Button
                {
                    Text = "Keep as office LOGO",
                    Bounds = new Rectangle(214, 400, 190, 36),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9f)
                };
                logo.Click += (s, e) => Take(false);
                Controls.Add(logo);

                var cancel = new Button
                {
                    Text = "Cancel",
                    Bounds = new Rectangle(412, 400, 92, 36),
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel
                };
                Controls.Add(cancel);
                CancelButton = cancel;

                Show(0);
                UiTheme.Polish(this);
            }

            private void Step(int delta)
            {
                Show(((_index + delta) % _found.Count + _found.Count) % _found.Count);
            }

            private void Show(int index)
            {
                _index = index;
                SealDetector.Seal s = _found[_index];

                _preview.Image?.Dispose();
                _preview.Image = SealDetector.Crop(_page, s);

                _caption.Text = (_index + 1) + " of " + _found.Count + "   —   " +
                    (int)(s.Rect.Width * 100) + "% × " + (int)(s.Rect.Height * 100) +
                    "% of the page, " + (int)(s.InkRatio * 100) + "% inked";

                _prev.Enabled = _next.Enabled = _found.Count > 1;
            }

            private void Take(bool asStamp)
            {
                if (_preview.Image == null) return;
                using (var ms = new System.IO.MemoryStream())
                {
                    _preview.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    Chosen = ms.ToArray();
                }
                SaveAsStamp = asStamp;
                DialogResult = DialogResult.OK;
                Close();
            }

            protected override void OnFormClosed(FormClosedEventArgs e)
            {
                base.OnFormClosed(e);
                _preview.Image?.Dispose();
            }
        }

        /// <summary>The form in the words a person would use, for messages and the audit
        /// trail. Falls back to the kind when nothing was identified.</summary>
        private string FormLabel()
        {
            return _formDef == null
                ? DocumentAI.KindName(_kind)
                : _formDef.FormName + " (Municipal Form No. " + _formDef.MunicipalFormNo +
                  ", " + _formDef.Revision + ")";
        }

        private static string TableFor(DocKind k)
        {
            switch (k)
            {
                case DocKind.Birth: return "births";
                case DocKind.Marriage: return "marriages";
                case DocKind.Death: return "deaths";
                default: return null;
            }
        }

        // ---- the review grid ------------------------------------------------

        private void BuildFieldGrid()
        {
            dgvFields.AutoGenerateColumns = false;
            dgvFields.Columns.Clear();
            dgvFields.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Field", HeaderText = "Field", ReadOnly = true, FillWeight = 23 });
            // The value is what the operator actually compares against the scan, so it gets
            // the width. The Check column says the same few sentences over and over and can
            // afford to wrap or ellipsise; a half-shown place name cannot be checked at all.
            dgvFields.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Value", HeaderText = "Extracted Value (editable)", FillWeight = 40 });
            dgvFields.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Conf", HeaderText = "Conf", ReadOnly = true, FillWeight = 9 });
            dgvFields.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Check", HeaderText = "Check", ReadOnly = true, FillWeight = 20 });
            dgvFields.Columns.Add(new DataGridViewCheckBoxColumn
            { Name = "Verified", HeaderText = "Verified", FillWeight = 8,
              ToolTipText = "Check only after comparing this value with the scanned document." });
        }

        /// <summary>
        /// Show WHICH FORM this is, in the terms the paper itself uses, and the registry
        /// number that identifies the record. Both are stored on the record when it is
        /// saved, so a certificate can be reprinted in the right layout years later.
        /// </summary>
        private void UpdateFormIdentity()
        {
            if (_formDef == null)
            {
                txtFormId.Text = _result == null
                    ? ""
                    : "Not identified — no form layout matches this page";
                txtRegistryNo.Text = "";
                btnReport.Enabled = false;
                return;
            }

            txtFormId.Text = _formDef.FormName +
                "   ·   Municipal Form No. " + _formDef.MunicipalFormNo +
                "   ·   " + _formDef.Revision +
                "   ·   " + _formDef.FormCode +
                // Say so when the layout was refused: the record is filed under the
                // revision the office issues, which is NOT the same claim as "read box by
                // box off this revision".
                (_result?.LayoutRejected != null ? "   ·   layout not on file" : "");

            string reg = V("RegistryNo");
            txtRegistryNo.Text = reg.Length == 0 ? "(not read — type it in)" : reg;
            txtRegistryNo.ForeColor = reg.Length == 0
                ? Color.FromArgb(134, 142, 150) : Color.FromArgb(23, 27, 36);

            // Two different jobs behind one button, so it must say which one it is about to
            // do. Before commit it draws a watermarked, unsaved PREVIEW - useful precisely
            // while the record is still wrong, which is why it is not gated on the save.
            // After commit it prints the registry entry.
            btnReport.Enabled = _formDef != null && (_savedRecordId.HasValue || _result != null);
            btnReport.Text = _savedRecordId.HasValue ? "Print Certificate" : "Preview on Form";
        }

        /// <summary>
        /// Fill the review grid IN THE ORDER THE PAPER FORM PRINTS, grouped under the
        /// certificate's own section headings ("1-5. Child", "6-12. Mother", …) rather than
        /// in whatever order the extractor happened to produce. The operator is comparing
        /// the grid against the document in front of them, so the two must read the same
        /// way down the page.
        /// <para/>
        /// Sections come from the recognised <see cref="FormDefinition"/>. A field the form
        /// has no section for is still shown, under "Other Entries" — a value is never
        /// hidden from the operator because the catalog forgot it.
        /// </summary>
        private void FillFieldGrid(DocAiResult r)
        {
            dgvFields.Rows.Clear();

            if (_formDef == null || _formDef.Sections.Count == 0)
            {
                foreach (DocField f in r.Fields) AddFieldRow(f);
                return;
            }

            var byKey = new Dictionary<string, DocField>(StringComparer.OrdinalIgnoreCase);
            foreach (DocField f in r.Fields) byKey[f.Key] = f;

            foreach (var section in _formDef.OrderedSections(byKey.Keys))
            {
                AddSectionRow(section.Key);
                foreach (string key in section.Value)
                    if (byKey.TryGetValue(key, out DocField f)) AddFieldRow(f);
            }
        }

        private void AddFieldRow(DocField f)
        {
            int i = dgvFields.Rows.Add(f.Label, f.Value, "", "", f.EditedByUser);
            dgvFields.Rows[i].Tag = f;
            PaintRow(dgvFields.Rows[i], f);
        }

        /// <summary>
        /// A section heading row. Tagged with the title string rather than a
        /// <see cref="DocField"/>, which is what every handler already tests for — so a
        /// heading can never be edited, saved, or counted as a field.
        /// </summary>
        private void AddSectionRow(string title)
        {
            int i = dgvFields.Rows.Add(title.ToUpperInvariant(), "", "", "", false);
            DataGridViewRow row = dgvFields.Rows[i];
            row.Tag = title;
            row.ReadOnly = true;
            row.DefaultCellStyle.BackColor = Color.FromArgb(240, 243, 247);
            row.DefaultCellStyle.ForeColor = Color.FromArgb(73, 80, 87);
            row.DefaultCellStyle.Font = new Font(dgvFields.Font, FontStyle.Bold);
            row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 243, 247);
            row.DefaultCellStyle.SelectionForeColor = Color.FromArgb(73, 80, 87);
        }

        /// <summary>One row's confidence, verdict and colour — the operator's whole signal.</summary>
        private void PaintRow(DataGridViewRow row, DocField f)
        {
            _updatingFieldGrid = true;
            row.Cells["Value"].Value = f.Value;
            // The column cannot be wide enough for every place string, and a value the
            // operator cannot finish reading cannot be checked against the scan.
            row.Cells["Value"].ToolTipText = string.IsNullOrWhiteSpace(f.Value) ? "" : f.Value;
            row.Cells["Conf"].Value = string.IsNullOrWhiteSpace(f.Value) ? "—" : f.Confidence + "%";
            row.Cells["Verified"].Value = f.EditedByUser;
            row.Cells["Verified"].ReadOnly = string.IsNullOrWhiteSpace(f.Value)
                || f.Status == FieldStatus.Invalid || f.Status == FieldStatus.Conflict;

            string note;
            Color fore, back = Color.White;
            switch (f.Status)
            {
                case FieldStatus.Invalid:
                    note = "✖ " + f.Issue;
                    fore = Color.FromArgb(176, 42, 55); back = Color.FromArgb(255, 235, 238);
                    break;
                case FieldStatus.Conflict:
                    note = "⚠ " + f.Issue;
                    fore = Color.FromArgb(133, 100, 4); back = Color.FromArgb(255, 243, 205);
                    break;
                case FieldStatus.Missing:
                    note = "— not found on the page";
                    fore = Color.FromArgb(134, 142, 150);
                    break;
                case FieldStatus.Uncertain:
                    note = "⚠ weak reading — check the scan";
                    fore = Color.FromArgb(191, 122, 0); back = Color.FromArgb(255, 248, 225);
                    break;
                default:
                    note = f.EditedByUser ? "✔ confirmed by operator"
                         : f.Corrected ? "✔ auto-corrected from \"" + f.OcrValue + "\""
                         : "✔";
                    fore = Color.FromArgb(25, 135, 84);
                    break;
            }

            row.Cells["Check"].Value = note;
            row.Cells["Check"].Style.ForeColor = fore;
            row.Cells["Value"].Style.BackColor = back;
            row.Cells["Conf"].Style.ForeColor = fore;
            row.Cells["Check"].ToolTipText = f.Corrected
                ? "OCR read: \"" + f.OcrValue + "\"" : "";
            row.Cells["Verified"].ToolTipText = f.EditedByUser
                ? "Compared with the scan and confirmed by the operator."
                : "Check only after comparing the value with the scan.";
            _updatingFieldGrid = false;
        }

        /// <summary>
        /// A check mark is an explicit human verification, not an OCR score. It therefore
        /// earns 100% only after the operator compares the value with the document; invalid,
        /// conflicting or blank readings cannot be confirmed.
        /// </summary>
        private void DgvFields_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_updatingFieldGrid || _result == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvFields.Columns[e.ColumnIndex].Name != "Verified") return;
            DataGridViewRow changed = dgvFields.Rows[e.RowIndex];
            if (!(changed.Tag is DocField f)) return;

            bool verified = Convert.ToBoolean(changed.Cells["Verified"].Value ?? false);
            if (verified && (string.IsNullOrWhiteSpace(f.Value)
                || f.Status == FieldStatus.Invalid || f.Status == FieldStatus.Conflict))
            {
                _updatingFieldGrid = true;
                changed.Cells["Verified"].Value = false;
                _updatingFieldGrid = false;
                return;
            }

            f.EditedByUser = verified;
            DocIntelligence.Revalidate(_result);
            foreach (DataGridViewRow row in dgvFields.Rows)
                if (row.Tag is DocField df) PaintRow(row, df);
            ApplyResultToUi();
        }

        /// <summary>
        /// The operator's edit is the last word on a field, so the rules re-run against it
        /// immediately: a corrected date can clear the manual-review block and re-enable
        /// Commit without another OCR pass.
        /// </summary>
        private void DgvFields_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_result == null) return;
            if (!(dgvFields.Rows[e.RowIndex].Tag is DocField f)) return;

            string typed = (dgvFields.Rows[e.RowIndex].Cells["Value"].Value ?? "").ToString().Trim();
            if (string.Equals(typed, f.Value ?? "", StringComparison.Ordinal)) return;

            f.Value = typed;
            f.EditedByUser = true;

            DocIntelligence.Revalidate(_result);
            foreach (DataGridViewRow row in dgvFields.Rows)
                if (row.Tag is DocField df) PaintRow(row, df);
            // A typed registry number is part of the record's identity, so the
            // identification strip has to follow the grid rather than the last OCR run.
            UpdateFormIdentity();
            ApplyResultToUi();
        }

        /// <summary>
        /// Two modes, decided by whether the record exists yet.
        /// <para/>
        /// COMMITTED: prints the certificate from the saved registry entry, through the same
        /// reusable report path every other screen uses — Crystal when a .rpt exists for this
        /// form, otherwise CROMS's own renderer.
        /// <para/>
        /// NOT YET COMMITTED: draws the reviewed values onto the same form as an UNSAVED
        /// PREVIEW — watermarked on every page, no office stamp, never through Crystal. That
        /// is deliberately allowed while the record is still wrong, because laying the values
        /// out in their real boxes is how an operator spots a value read into the wrong row.
        /// It stays a preview: nothing is written, and a certificate is still only printed
        /// from a committed entry.
        /// </summary>
        private void btnReport_Click(object sender, EventArgs e)
        {
            if (_formDef == null)
            {
                MessageBox.Show(
                    "Run OCR first. CROMS has to identify which certificate form this is " +
                    "before it can lay the values out on it.",
                    "No form identified yet", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_savedRecordId.HasValue)
            {
                CertificateReport.Show(_formDef.FormCode, _savedRecordId.Value, this);
                return;
            }

            if (_result == null)
            {
                MessageBox.Show(
                    "Nothing has been read from this document yet.",
                    "Nothing to preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            CertificateReport.ShowOcrPreview(_formDef, Values(), this);
        }

        /// <summary>
        /// Draw the selected field's box on the scan. The regions are measured on the
        /// prepared page, which is a scaled copy of what is displayed, so they are mapped
        /// through both that scale and the PictureBox's own letterboxing.
        /// </summary>
        private void PbScan_Paint(object sender, PaintEventArgs e)
        {
            if (_image == null || _result == null || dgvFields.CurrentRow == null) return;
            if (!(dgvFields.CurrentRow.Tag is DocField f)) return;
            if (f.Region.Width <= 0 || f.Region.Height <= 0) return;
            if (_result.PageWidth <= 0 || _result.PageHeight <= 0) return;

            // page pixels → source image pixels
            double toImage = (double)_image.Width / _result.PageWidth;

            // source image pixels → the box, honouring PictureBoxSizeMode.Zoom
            double fit = Math.Min((double)pbScan.ClientSize.Width / _image.Width,
                                  (double)pbScan.ClientSize.Height / _image.Height);
            double offsetX = (pbScan.ClientSize.Width - _image.Width * fit) / 2.0;
            double offsetY = (pbScan.ClientSize.Height - _image.Height * fit) / 2.0;

            var rect = new RectangleF(
                (float)(f.Region.X * toImage * fit + offsetX),
                (float)(f.Region.Y * toImage * fit + offsetY),
                (float)(f.Region.Width * toImage * fit),
                (float)(f.Region.Height * toImage * fit));
            rect.Inflate(4f, 4f);

            using (var pen = new Pen(Color.FromArgb(220, 13, 110, 253), 2f))
            using (var wash = new SolidBrush(Color.FromArgb(40, 13, 110, 253)))
            {
                e.Graphics.FillRectangle(wash, rect);
                e.Graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        /// <summary>The reviewed values by canonical key — what actually gets written.</summary>
        private Dictionary<string, string> Values()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (_result == null) return d;
            foreach (DocField f in _result.Fields)
                if (!string.IsNullOrWhiteSpace(f.Value)) d[f.Key] = f.Value.Trim();
            return d;
        }

        private string V(string key)
        {
            DocField f = _result?.Fields.FirstOrDefault(
                x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            return f?.Value == null ? "" : f.Value.Trim();
        }

        // ---- actions ----------------------------------------------------------

        /// <summary>
        /// Nothing leaves this screen without the operator saying so. The dialog names what
        /// is still flagged, so "confirm" means confirming a document they have actually
        /// looked at rather than clicking past a warning they never read.
        /// </summary>
        private bool Confirm(string action)
        {
            if (_result == null) return false;

            var flagged = _result.Fields
                .Where(f => f.Status == FieldStatus.Invalid || f.Status == FieldStatus.Conflict
                         || f.Status == FieldStatus.Uncertain)
                .Take(8).ToList();
            int blank = _result.MissingCount;

            string detail = "Document: " + DocumentAI.KindName(_kind) +
                "   Overall confidence: " + _result.OverallConfidence + "%\n" +
                _result.ExtractedCount + " field(s) read, " + blank + " blank.\n";

            if (flagged.Count > 0)
                detail += "\nStill flagged:\n  • " +
                    string.Join("\n  • ", flagged.Select(f =>
                        f.Label + " = \"" + f.Value + "\" (" + f.Confidence + "%)" +
                        (string.IsNullOrEmpty(f.Issue) ? "" : " — " + f.Issue))) + "\n";

            detail += "\nYou are confirming these values are correct against the scan.\n" +
                      "Proceed with " + action + "?";

            return MessageBox.Show(detail, "Confirm " + action,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
                == DialogResult.Yes;
        }

        private void btnAutoFill_Click(object sender, EventArgs e)
        {
            if (_result == null || _kind == DocKind.Unknown)
            {
                MessageBox.Show("Run OCR on a Birth, Marriage or Death certificate first.",
                    "Not classified", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!RequiredFieldsPresent()) return;
            if (!Confirm("auto-fill " + ModuleName(_kind))) return;

            Dictionary<string, string> vals = Values();
            LearnFromExtraction(_kind, vals);   // the corrected spellings, not the raw ones

            // Carry the FORM IDENTITY across with the values. The registration module is
            // what finally writes the row, so it is the module that has to store which
            // form the record came off — otherwise an auto-filled record loses the one
            // fact a later reprint depends on.
            if (_formDef != null)
            {
                vals["FormCode"] = _formDef.FormCode;
                vals["FormName"] = _formDef.FormName;
            }

            if (!PrimeModule(_kind, vals)) return;

            WriteAudit(OcrAudit.AutoFilled);
            // record_id stays NULL until the target form is saved — the batch grid shows
            // this as "<table> (pending)".
            MarkBatch("Auto-Filled", TableFor(_kind), null);
            LoadBatch();

            MessageBox.Show(
                "The " + ModuleName(_kind) + " form has been filled from the confirmed values. The " +
                "scanned copy will be saved with the record when you Save.",
                "Auto-filled", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnCommit_Click(object sender, EventArgs e)
        {
            if (!ReadyToSave()) return;
            if (!Confirm("commit to the birth registry")) return;

            long id = SaveBirth("Registered");
            _savedRecordId = id;
            WriteAudit(OcrAudit.Committed);
            MarkBatch("Committed", "births", id);
            Audit.Write(Audit.Create, "births", id,
                "Committed from OCR scan " + _scanId + " as " + FormLabel());
            LoadBatch();
            UpdateFormIdentity();
            MessageBox.Show(
                "Committed to the birth registry as " + FormLabel() + ".\n\n" +
                "Press Print Certificate to produce the document.",
                "Document", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnDraft_Click(object sender, EventArgs e)
        {
            if (!ReadyToSave()) return;

            long id = SaveBirth("Draft");
            _savedRecordId = id;
            WriteAudit(OcrAudit.Draft);
            MarkBatch("Draft", "births", id);
            Audit.Write(Audit.Create, "births", id,
                "Draft from OCR scan " + _scanId + " as " + FormLabel());
            LoadBatch();
            UpdateFormIdentity();
            MessageBox.Show("Saved as draft in the birth registry.", "Document",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Park the scan for a human. Used for a document the engine could not identify, or
        /// one whose values it does not trust — everything it saw (its raw text, every
        /// field, every confidence) stays on the record so the reviewer starts from the
        /// engine's work rather than from nothing.
        /// </summary>
        private void btnReview_Click(object sender, EventArgs e)
        {
            if (_result == null) return;

            WriteAudit(OcrAudit.ManualReview);
            MarkBatch("Manual Review", TableFor(_kind), null);
            LoadBatch();
            MessageBox.Show(
                "Sent to manual review" +
                (string.IsNullOrEmpty(_result.ReviewReason) ? "" : " — " + _result.ReviewReason) +
                ".\nIt stays in today's batch with everything the engine read.",
                "Manual review", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool ReadyToSave()
        {
            if (_result == null || _kind == DocKind.Unknown)
            {
                MessageBox.Show(
                    "The document type has not been identified — run OCR first, and save only a scan " +
                    "recognised as a Certificate of Live Birth.",
                    "Not classified", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (_kind != DocKind.Birth)
            {
                MessageBox.Show(
                    "Only a birth certificate is written to the registry from this screen. Use \"" +
                    btnAutoFill.Text + "\" for this document.",
                    "Wrong document type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (_result.NeedsManualReview)
            {
                MessageBox.Show(
                    "This document is held for review: " + _result.ReviewReason + ".\n\n" +
                    "Correct the flagged fields in the grid, or send it to manual review.",
                    "Held for review", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return RequiredFieldsPresent();
        }

        /// <summary>
        /// Refuse to leave this screen with a record the certificate itself says is
        /// incomplete. The registration modules do the final validation, but letting an
        /// incomplete record through only moves the problem into the registry — and the
        /// required list comes from the recognised FORM, so it is right for a marriage or
        /// death scan being routed out as well as for a birth being committed here.
        /// </summary>
        private bool RequiredFieldsPresent()
        {
            if (_result == null) return true;
            List<string> missing = _result.Fields
                .Where(f => f.Required && V(f.Key).Length == 0)
                .Select(f => f.Label)
                .ToList();
            if (missing.Count == 0) return true;

            MessageBox.Show(
                "This record cannot be saved or routed yet. The certificate requires:\n\n  " +
                string.Join("\n  ", missing) +
                "\n\nType the value in from the scan, then continue.",
                "Incomplete record", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private void WriteAudit(string action)
        {
            if (!OcrAudit.WriteFields(_scanId, _result, action, out string error) && error != null)
                MessageBox.Show(
                    "The record was saved, but its per-field audit trail could not be written: " +
                    error + "\n\nRun Database\\25_document_intelligence.sql on this database.",
                    "Audit not written", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Hand a scan to the module that owns its column mapping, pre-filled with the
        /// confirmed values. Returns false if the shell could not be reached.
        /// </summary>
        private bool PrimeModule(DocKind kind, Dictionary<string, string> vals)
        {
            MainForm shell = Shell();
            if (shell == null)
            {
                MessageBox.Show("Open this screen from the CROMS main window to route the scan.",
                    "Cannot route", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Carry the upload's own id along with the values (same convention as
            // FormCode/FormName above), so the registration module can write back to
            // ocr_batch — mark it Processed, with the final registry number — the moment
            // IT saves the record. Marriage gets this through SetOcrContext below instead.
            if (!string.IsNullOrEmpty(_scanId)) vals["OcrScanId"] = _scanId;

            // PrimeFromExtraction calls ClearForm(), which nulls the pending scan, so the
            // image is attached only AFTER the extracted values are primed.
            if (kind == DocKind.Birth)
            {
                if (shell.GoToModule("birth") is BirthRegistrationForm birth)
                {
                    birth.PrimeFromExtraction(vals);
                    birth.SetScanImage(_scanBytes);
                }
            }
            else if (kind == DocKind.Death)
            {
                if (shell.GoToModule("death") is DeathRegistrationForm death)
                {
                    death.PrimeFromExtraction(vals);
                    death.SetScanImage(_scanBytes);
                }
            }
            else if (kind == DocKind.Marriage)
            {
                // Marriage data entry is a modal dialog (Municipal Form 97).
                using (var dlg = new MarriageEntryForm(null))
                {
                    dlg.SetScanImage(_scanBytes);
                    dlg.PrimeFromExtraction(vals);
                    // The OCR run travels with the record: its confidence, its weak fields
                    // (highlighted on Form 97) and a review hold that must be cleared before
                    // the marriage can be registered.
                    dlg.SetOcrContext(_scanId, _result);
                    dlg.ShowDialog(this);
                }
                shell.GoToModule("marriage");   // refresh the overview after the dialog closes
            }
            else return false;

            return true;
        }

        private MainForm Shell()
        {
            for (Control p = Parent; p != null; p = p.Parent)
                if (p is MainForm mf) return mf;
            return null;
        }

        /// <summary>
        /// Feed the CONFIRMED values into the shared Learning Library, so corrected
        /// spellings win on future scans and become available across the office.
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

        // ---- batch log --------------------------------------------------------

        /// <summary>
        /// Log this OCR run to <c>ocr_batch</c>. When <see cref="_scanId"/> is already set —
        /// a mobile upload's own id, carried in by <see cref="DgvBatch_CellDoubleClick"/> —
        /// the SAME row is updated in place rather than a second row being inserted and the
        /// first retired. That keeps ONE stable Upload ID for a scan from the moment the
        /// client's phone creates it through to the record it finally produces, which is
        /// what lets <see cref="OcrAudit.MarkProcessed"/> find it again by that id. A scan
        /// loaded locally on this PC has no prior id and still gets a fresh SCN- one.
        /// </summary>
        private void LogBatch(DocAiResult r)
        {
            bool reuse = !string.IsNullOrEmpty(_scanId);
            if (!reuse)
            {
                int n = Db.GetCount("SELECT id FROM ocr_batch WHERE DATE(created_at) = CURDATE()") + 1;
                _scanId = "SCN-" + DateTime.Now.ToString("yyMMdd") + "-" + n.ToString("D3");
            }

            string status = r.Kind == DocKind.Unknown ? "Unclassified"
                          : r.NeedsManualReview ? "Needs Review" : "For Review";

            // form_code is NULL when the layout was refused, while doc_kind still says
            // Birth — which is exactly the distinction an auditor needs between "read box
            // by box off this revision" and "read by labels".
            object formCode = r.LayoutRejected == null
                ? (object)(FormCatalog.ByLayoutCode(r.LayoutCode)?.FormCode) ?? DBNull.Value
                : DBNull.Value;
            object formName = (object)_formDef?.FormName ?? DBNull.Value;
            object reason = string.IsNullOrEmpty(r.ReviewReason)
                ? (object)DBNull.Value : Truncate(r.ReviewReason, 250);
            string username = Session.User?.Username ?? "(unknown)";
            string book = grpScan.Text.Replace("SCANNED DOCUMENT — ", "");

            try
            {
                if (reuse)
                {
                    // source_image's only job was carrying the bytes here from the phone;
                    // clear it now the scan has actually been read, per 54_mobile_scan_
                    // upload.sql's own stated intent — everything else the mobile row
                    // carried (source, client_name, requested_type, created_at) is left
                    // untouched by this UPDATE.
                    Db.Push(
                        "UPDATE ocr_batch SET source_book=@book, doc_class=@class, doc_kind=@kind, " +
                        "form_code=@fcode, form_name=@fname, confidence=@conf, " +
                        "overall_confidence=@oconf, needs_review=@need, review_reason=@reason, " +
                        "rotation_applied=@rot, username=@user, status=@status, raw_text=@raw, " +
                        "source_image=NULL WHERE scan_id=@id",
                        new MySqlParameter("@id", _scanId),
                        new MySqlParameter("@book", book),
                        new MySqlParameter("@class", ClassLabel(r.Kind)),
                        new MySqlParameter("@kind", r.Kind.ToString()),
                        new MySqlParameter("@fcode", formCode),
                        new MySqlParameter("@fname", formName),
                        new MySqlParameter("@conf", r.OcrConfidence),
                        new MySqlParameter("@oconf", r.OverallConfidence),
                        new MySqlParameter("@need", r.NeedsManualReview ? 1 : 0),
                        new MySqlParameter("@reason", reason),
                        new MySqlParameter("@rot", r.RotationApplied),
                        new MySqlParameter("@user", username),
                        new MySqlParameter("@status", status),
                        new MySqlParameter("@raw", r.RawText));
                }
                else
                {
                    Db.Push(
                        "INSERT INTO ocr_batch (scan_id, source_book, doc_class, doc_kind, form_code, " +
                        "form_name, confidence, " +
                        "overall_confidence, needs_review, review_reason, rotation_applied, username, " +
                        "status, raw_text) " +
                        "VALUES (@id, @book, @class, @kind, @fcode, @fname, @conf, @oconf, @need, " +
                        "@reason, @rot, @user, @status, @raw)",
                        new MySqlParameter("@id", _scanId),
                        new MySqlParameter("@book", book),
                        new MySqlParameter("@class", ClassLabel(r.Kind)),
                        new MySqlParameter("@kind", r.Kind.ToString()),
                        new MySqlParameter("@fcode", formCode),
                        new MySqlParameter("@fname", formName),
                        new MySqlParameter("@conf", r.OcrConfidence),
                        new MySqlParameter("@oconf", r.OverallConfidence),
                        new MySqlParameter("@need", r.NeedsManualReview ? 1 : 0),
                        new MySqlParameter("@reason", reason),
                        new MySqlParameter("@rot", r.RotationApplied),
                        new MySqlParameter("@user", username),
                        new MySqlParameter("@status", status),
                        new MySqlParameter("@raw", r.RawText));
                }
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                // Migration 25 (or 58, for the reuse path) has not been run: keep working
                // on the older columns rather than losing the batch row entirely.
                if (reuse)
                {
                    Db.Push(
                        "UPDATE ocr_batch SET source_book=@book, doc_class=@class, doc_kind=@kind, " +
                        "confidence=@conf, status=@status, raw_text=@raw WHERE scan_id=@id",
                        new MySqlParameter("@id", _scanId),
                        new MySqlParameter("@book", book),
                        new MySqlParameter("@class", ClassLabel(r.Kind)),
                        new MySqlParameter("@kind", r.Kind.ToString()),
                        new MySqlParameter("@conf", r.OcrConfidence),
                        new MySqlParameter("@status", status),
                        new MySqlParameter("@raw", r.RawText));
                }
                else
                {
                    Db.Push(
                        "INSERT INTO ocr_batch (scan_id, source_book, doc_class, doc_kind, confidence, " +
                        "status, raw_text) VALUES (@id, @book, @class, @kind, @conf, @status, @raw)",
                        new MySqlParameter("@id", _scanId),
                        new MySqlParameter("@book", book),
                        new MySqlParameter("@class", ClassLabel(r.Kind)),
                        new MySqlParameter("@kind", r.Kind.ToString()),
                        new MySqlParameter("@conf", r.OcrConfidence),
                        new MySqlParameter("@status", status),
                        new MySqlParameter("@raw", r.RawText));
                }
            }
        }

        private static string Truncate(string s, int max)
        {
            return s.Length <= max ? s : s.Substring(0, max);
        }

        /// <summary>Record what became of this scan: its status and the registry row it produced.</summary>
        private void MarkBatch(string status, string table, long? recordId)
        {
            if (_scanId == null) return;
            Db.Push(
                "UPDATE ocr_batch SET status = @status, record_table = @table, record_id = @rid " +
                "WHERE scan_id = @id",
                new MySqlParameter("@status", status),
                new MySqlParameter("@table", (object)table ?? DBNull.Value),
                new MySqlParameter("@rid", recordId.HasValue ? (object)recordId.Value : DBNull.Value),
                new MySqlParameter("@id", _scanId));
        }

        // ---- write the record --------------------------------------------------

        /// <summary>
        /// Write the confirmed values into `births` using the Form-102 columns each value
        /// belongs to. Names stay in the separate first / middle / last cells the extractor
        /// produced, and the values with no dedicated box — time of birth, type of birth,
        /// weight, occupations, religion, citizenship, informant — are carried over. The
        /// scan itself is stored as the record's softcopy.
        /// </summary>
        private long SaveBirth(string status)
        {
            string dobText = V("DateOfBirth");
            bool parsed = DateTime.TryParse(dobText, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime d);
            object dob = parsed ? (object)d.Date : DBNull.Value;

            string sexText = V("Sex");
            object sex = sexText.StartsWith("F", StringComparison.OrdinalIgnoreCase) ? "Female"
                       : sexText.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "Male"
                       : (object)DBNull.Value;

            // This office identifies a registry book by its year.
            string year = parsed ? d.Year.ToString()
                        : (dobText.Length >= 4 && dobText.Substring(0, 4).All(char.IsDigit)
                            ? dobText.Substring(0, 4) : "");

            object weight = int.TryParse(V("Weight"), out int g) ? (object)g : DBNull.Value;
            string religion = V("Religion");
            string citizenship = V("Nationality");

            // FORM IDENTITY on the record. Without it a certificate cannot be reprinted
            // in the layout it came off — `births` is shared by every MF-102 revision, so
            // the revision is not recoverable from the row itself.
            string formCode = _formDef?.FormCode;
            string formName = _formDef?.FormName;

            return Db.Insert(
                "INSERT INTO births (form_code, form_name, registry_no, book_volume, status, first_name, middle_name, " +
                "last_name, sex, date_of_birth, time_of_birth, place_of_birth, type_of_birth, weight_grams, " +
                "mother_first_name, mother_middle_name, mother_last_name, mother_occupation, " +
                "mother_religion, mother_citizenship, " +
                "father_first_name, father_middle_name, father_last_name, father_occupation, " +
                "father_religion, father_citizenship, " +
                "attendant_type, attendant_name, attendant_title, attendant_address, attendant_date, " +
                "informant_name, informant_relationship, informant_address, informant_date, " +
                "prepared_by, prepared_by_title, prepared_by_date, " +
                "received_by, received_by_title, received_by_date, " +
                "registered_by, registered_by_title, registered_by_date, " +
                "birth_image, scan_image) " +
                "VALUES (@fcode, @fname, @reg, @book, @status, @fn, @mn, @ln, @sex, @dob, @tob, @place, @btype, @weight, " +
                "@mf, @mm, @ml, @mocc, @mrel, @mcit, " +
                "@ff, @fm, @fl, @focc, @frel, @fcit, " +
                "@atype, @aname, @atitle, @aaddr, @adate, " +
                "@informant, @irel, @iaddr, @idate, " +
                "@prep, @preptitle, @prepdate, " +
                "@recv, @recvtitle, @recvdate, " +
                "@regby, @regbytitle, @regbydate, " +
                "@img, @scan)",
                new MySqlParameter("@fcode", NullIfEmpty(formCode)),
                new MySqlParameter("@fname", NullIfEmpty(formName)),
                new MySqlParameter("@reg", NullIfEmpty(V("RegistryNo"))),
                new MySqlParameter("@book", NullIfEmpty(year)),
                new MySqlParameter("@status", status),
                new MySqlParameter("@fn", V("ChildFirst")),
                new MySqlParameter("@mn", NullIfEmpty(V("ChildMiddle"))),
                new MySqlParameter("@ln", V("ChildLast")),
                new MySqlParameter("@sex", sex),
                new MySqlParameter("@dob", dob),
                new MySqlParameter("@tob", NullIfEmpty(V("TimeOfBirth"))),
                new MySqlParameter("@place", NullIfEmpty(V("PlaceOfBirth"))),
                new MySqlParameter("@btype", NullIfEmpty(V("TypeOfBirth"))),
                new MySqlParameter("@weight", weight),
                new MySqlParameter("@mf", NullIfEmpty(V("MotherFirst"))),
                new MySqlParameter("@mm", NullIfEmpty(V("MotherMiddle"))),
                new MySqlParameter("@ml", NullIfEmpty(V("MotherLast"))),
                new MySqlParameter("@mocc", NullIfEmpty(V("MotherOccupation"))),
                new MySqlParameter("@mrel", NullIfEmpty(religion)),
                new MySqlParameter("@mcit", NullIfEmpty(citizenship)),
                new MySqlParameter("@ff", NullIfEmpty(V("FatherFirst"))),
                new MySqlParameter("@fm", NullIfEmpty(V("FatherMiddle"))),
                new MySqlParameter("@fl", NullIfEmpty(V("FatherLast"))),
                new MySqlParameter("@focc", NullIfEmpty(V("FatherOccupation"))),
                new MySqlParameter("@frel", NullIfEmpty(religion)),
                new MySqlParameter("@fcit", NullIfEmpty(citizenship)),
                // 19a/21a Attendant, 19b/21b Certification of Attendant at Birth.
                new MySqlParameter("@atype", NullIfEmpty(V("Attendant"))),
                new MySqlParameter("@aname", NullIfEmpty(V("AttendantName"))),
                new MySqlParameter("@atitle", NullIfEmpty(V("AttendantTitle"))),
                new MySqlParameter("@aaddr", NullIfEmpty(V("AttendantAddress"))),
                new MySqlParameter("@adate", DateOrNull("AttendantDate")),
                // 20/22 Certification of Informant.
                new MySqlParameter("@informant", NullIfEmpty(V("Informant"))),
                new MySqlParameter("@irel", NullIfEmpty(V("InformantRelationship"))),
                new MySqlParameter("@iaddr", NullIfEmpty(V("InformantAddress"))),
                new MySqlParameter("@idate", DateOrNull("InformantDate")),
                // 21/23 Prepared by, 22/24 Received at the office, 25 Registered.
                new MySqlParameter("@prep", NullIfEmpty(V("PreparedByName"))),
                new MySqlParameter("@preptitle", NullIfEmpty(V("PreparedByTitle"))),
                new MySqlParameter("@prepdate", DateOrNull("PreparedByDate")),
                new MySqlParameter("@recv", NullIfEmpty(V("ReceivedByName"))),
                new MySqlParameter("@recvtitle", NullIfEmpty(V("ReceivedByTitle"))),
                new MySqlParameter("@recvdate", DateOrNull("ReceivedByDate")),
                new MySqlParameter("@regby", NullIfEmpty(V("RegisteredByName"))),
                new MySqlParameter("@regbytitle", NullIfEmpty(V("RegisteredByTitle"))),
                new MySqlParameter("@regbydate", DateOrNull("RegisteredByDate")),
                // birth_image is this module's own copy; scan_image is the softcopy the
                // Birth Registration form and the softcopy viewer read back.
                new MySqlParameter("@img", MySqlDbType.LongBlob)
                    { Value = _scanBytes == null ? (object)DBNull.Value : _scanBytes },
                new MySqlParameter("@scan", MySqlDbType.LongBlob)
                    { Value = _scanBytes == null ? (object)DBNull.Value : _scanBytes });
        }

        /// <summary>
        /// Pending = not yet linked to a final record (<c>record_id IS NULL</c>): a mobile
        /// upload nobody has opened yet, or one already opened but not yet saved into a
        /// registration module. Processed = the history — every scan that DID produce a
        /// record, newest first, with the registry number it finally carries. One physical
        /// grid, switched by <see cref="_batchMode"/>, rather than two grids that would have
        /// to agree on every column.
        /// </summary>
        private string _batchMode = "Pending";

        private void btnBatchPending_Click(object sender, EventArgs e)
        {
            _batchMode = "Pending";
            UpdateBatchModeButtons();
            LoadBatch();
        }

        private void btnBatchProcessed_Click(object sender, EventArgs e)
        {
            _batchMode = "Processed";
            UpdateBatchModeButtons();
            LoadBatch();
        }

        private void UpdateBatchModeButtons()
        {
            bool pending = _batchMode == "Pending";
            btnBatchPending.BackColor = pending ? Color.FromArgb(13, 110, 253) : Color.FromArgb(233, 236, 239);
            btnBatchPending.ForeColor = pending ? Color.White : Color.FromArgb(33, 37, 41);
            btnBatchProcessed.BackColor = pending ? Color.FromArgb(233, 236, 239) : Color.FromArgb(13, 110, 253);
            btnBatchProcessed.ForeColor = pending ? Color.FromArgb(33, 37, 41) : Color.White;
        }

        private void LoadBatch()
        {
            try
            {
                if (_batchMode == "Processed") LoadProcessedBatch();
                else LoadPendingBatch();
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                // Migration 58 (client_name/requested_type/final_registry_no) has not been
                // run yet — fall back to the original combined "today's documents" view
                // rather than showing nothing.
                LoadBatchLegacy();
            }
        }

        /// <summary>Outstanding scans, oldest first — first-come-first-served, like a queue.</summary>
        private void LoadPendingBatch()
        {
            dgvBatch.DataSource = Db.Pull(
                "SELECT id AS '_Id', scan_id AS 'Upload ID', COALESCE(client_name, '') AS 'Name', " +
                "COALESCE(requested_type, doc_kind, '') AS 'Type', created_at AS 'Date', " +
                "CASE WHEN needs_review = 1 THEN CONCAT(status, ' ⚠') ELSE status END AS 'Status' " +
                "FROM ocr_batch WHERE record_id IS NULL AND status <> 'Opened on Desktop' " +
                "ORDER BY created_at ASC");
            lblBatch.Text = "PENDING OCR — " + dgvBatch.Rows.Count + " AWAITING REVIEW (double-click to open)";
            HideLookupColumns();
        }

        /// <summary>Everything a scan HAS produced, newest first — the permanent history.</summary>
        private void LoadProcessedBatch()
        {
            dgvBatch.DataSource = Db.Pull(
                "SELECT id AS '_Id', COALESCE(final_registry_no, '') AS 'Reg. No.', " +
                "COALESCE(client_name, '') AS 'Name', COALESCE(requested_type, doc_kind, '') AS 'Type', " +
                "created_at AS 'Processed Date', status AS 'Status' " +
                "FROM ocr_batch WHERE record_id IS NOT NULL ORDER BY created_at DESC");
            lblBatch.Text = "PROCESSED — " + dgvBatch.Rows.Count + " (double-click to view)";
            HideLookupColumns();
        }

        /// <summary>Pre-migration-58 fallback — the original combined "today's documents" list.</summary>
        private void LoadBatchLegacy()
        {
            try
            {
                dgvBatch.DataSource = Db.Pull(
                    "SELECT id AS '_Id', " +
                    "scan_id AS 'Scan ID', source_book AS 'Document', doc_class AS Class, " +
                    "CONCAT(COALESCE(overall_confidence, confidence), '%') AS Conf, " +
                    "CASE WHEN needs_review = 1 THEN CONCAT(status, ' ⚠') ELSE status END AS Status, " +
                    "COALESCE(review_reason, '') AS 'Reason', " +
                    "CASE WHEN record_table IS NULL THEN '' " +
                    "WHEN record_id IS NULL THEN CONCAT(record_table, ' (pending)') " +
                    "ELSE CONCAT(record_table, ' #', record_id) END AS 'Saved To', " +
                    "COALESCE(username, '') AS 'User' " +
                    "FROM ocr_batch WHERE DATE(created_at) = CURDATE() ORDER BY id DESC");
                lblBatch.Text = "TODAY'S DOCUMENTS — RUN MIGRATION 58_ocr_upload_metadata.sql FOR PENDING/PROCESSED VIEWS";
                HideLookupColumns();
                return;
            }
            catch (MySqlException ex) when (ex.Number == 1054) { /* fall through */ }

            try
            {
                dgvBatch.DataSource = Db.Pull(
                    "SELECT scan_id AS 'Scan ID', source_book AS 'Document', doc_class AS Class, " +
                    "CONCAT(confidence, '%') AS Conf, status AS Status, " +
                    "CASE WHEN record_table IS NULL THEN '' " +
                    "WHEN record_id IS NULL THEN CONCAT(record_table, ' (pending)') " +
                    "ELSE CONCAT(record_table, ' #', record_id) END AS 'Saved To' " +
                    "FROM ocr_batch WHERE DATE(created_at) = CURDATE() ORDER BY id DESC");
                lblBatch.Text = "TODAY'S DOCUMENTS — RUN MIGRATION 25_document_intelligence.sql";
            }
            catch
            {
                dgvBatch.DataSource = null;
                lblBatch.Text = "TODAY'S DOCUMENTS — RUN MIGRATIONS 24 AND 25";
            }
        }

        private void HideLookupColumns()
        {
            if (dgvBatch.Columns.Contains("_Id")) dgvBatch.Columns["_Id"].Visible = false;
            if (dgvBatch.Columns.Contains("_Source")) dgvBatch.Columns["_Source"].Visible = false;
        }

        /// <summary>
        /// Double-click a row in either grid mode. Always re-reads the FULL row by its
        /// hidden id rather than trusting whatever columns happen to be bound on screen, so
        /// the same handler works for the Pending and the Processed layouts without either
        /// one needing to carry columns the other does not.
        /// </summary>
        private async void DgvBatch_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (!dgvBatch.Columns.Contains("_Id")) return;
            object idVal = dgvBatch.Rows[e.RowIndex].Cells["_Id"].Value;
            if (idVal == null || idVal == DBNull.Value) return;
            long batchId = Convert.ToInt64(idVal);

            DataTable result = Db.Pull(
                "SELECT scan_id, source, source_book, status, record_table, record_id, " +
                "final_registry_no, client_name FROM ocr_batch WHERE id = @id",
                new MySqlParameter("@id", batchId));
            if (result.Rows.Count == 0) return;
            DataRow dr = result.Rows[0];

            if (_batchMode == "Processed" || dr["record_id"] != DBNull.Value)
            {
                ViewProcessed(dr);
                return;
            }

            string source = dr["source"] as string;
            string status = dr["status"] as string;

            if (!string.Equals(source, "Mobile", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(status, "Pending Review", StringComparison.OrdinalIgnoreCase))
            {
                // A desktop-loaded scan still under review, or a mobile scan already opened
                // in a PRIOR run of Analyze() this session — there is no separately stored
                // image to reopen; the scan is either already on screen above, or (if the
                // app was closed mid-review) has nothing left here for this screen to load.
                MessageBox.Show(
                    "This document is still being reviewed and has no separate image stored " +
                    "here to reopen. If it is not already showing in the panel above, load it " +
                    "again from its original file.",
                    "Still under review", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var img = Db.Pull(
                "SELECT source_image FROM ocr_batch WHERE id = @id",
                new MySqlParameter("@id", batchId));
            byte[] bytes = img.Rows.Count > 0 ? img.Rows[0]["source_image"] as byte[] : null;
            if (bytes == null || bytes.Length == 0)
            {
                MessageBox.Show("This scan has no image attached.", "Document",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Bitmap loaded;
            try { loaded = DocumentAI.LoadImageBytes(bytes); }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open the phone scan: " + ex.Message, "Document",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _image?.Dispose();
            _image = loaded;
            _scanBytes = bytes;
            _zoom = 1f;
            pbScan.SizeMode = PictureBoxSizeMode.Zoom;
            pbScan.Size = pnlScanHost.ClientSize;
            pbScan.Image = _image;
            grpScan.Text = "SCANNED DOCUMENT — " + (dr["source_book"] as string ?? "Phone scan");

            // Keep the SAME upload id rather than clearing it — LogBatch (inside Analyze,
            // below) sees a non-null _scanId and UPDATEs this exact row in place instead of
            // inserting a second one, so the client's own upload id is what eventually gets
            // linked to the record it produces.
            _scanId = dr["scan_id"] as string;
            _result = null;
            _kind = DocKind.Unknown;
            _formDef = null;
            _savedRecordId = null;
            _seals = new List<SealDetector.Seal>();
            txtDocClass.Clear();
            dgvFields.Rows.Clear();
            ApplyResultToUi();

            await Analyze();
            LoadBatch();
        }

        /// <summary>
        /// The "view" action for a processed row — what it was, what it became, and the
        /// registry number it carries (or a plain statement that none was read/assigned).
        /// A shortcut to the record itself when this PC has that module open.
        /// </summary>
        private void ViewProcessed(DataRow dr)
        {
            string name = dr["client_name"] as string;
            string reg = dr["final_registry_no"] as string;
            string table = dr["record_table"] as string;
            object recIdObj = dr["record_id"];

            string detail =
                "Upload ID: " + dr["scan_id"] + "\n" +
                "Name: " + (string.IsNullOrWhiteSpace(name) ? "(not given)" : name) + "\n" +
                "Status: " + dr["status"] + "\n" +
                "Registry No.: " + (string.IsNullOrWhiteSpace(reg) ? "(none — handwritten or not yet assigned)" : reg) + "\n" +
                "Saved to: " + (string.IsNullOrWhiteSpace(table) ? "(not saved)" : table +
                    (recIdObj == DBNull.Value ? "" : " #" + recIdObj));

            string moduleKey = table == "births" ? "birth" : table == "deaths" ? "death"
                              : table == "marriages" ? "marriage" : null;

            if (moduleKey != null && recIdObj != DBNull.Value
                && MessageBox.Show(detail + "\n\nOpen this record now?", "Processed document",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                Shell()?.GoToModule(moduleKey);
            }
            else if (moduleKey == null || recIdObj == DBNull.Value)
            {
                MessageBox.Show(detail, "Processed document", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
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
            pbScan.Invalidate();
        }

        /// <summary>
        /// Turn the page by hand. The engine already probes the four right angles on load,
        /// so this is for the operator who wants to read the scan the other way up; the
        /// field boxes are re-measured on the next OCR run.
        /// </summary>
        private void btnDeskew_Click(object sender, EventArgs e)
        {
            if (_image == null)
            {
                MessageBox.Show("Load a scanned image first.", "Rotate",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Bitmap turned = OcrService.Rotate(_image, 90);
            _image.Dispose();
            _image = turned;
            pbScan.Image = _image;
            pbScan.Invalidate();

            if (_result != null)
                lblFieldConf.Text = "Page turned by hand — run Re-OCR so the fields are read the new way up.";
        }

        private static object NullIfEmpty(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();
        }

        /// <summary>
        /// A grid value bound for a DATE column. The certification blocks are typed rather
        /// than handwritten, so their dates usually normalise to yyyy-MM-dd — but a reading
        /// the extractor could not resolve to a real date is left as it was printed, and
        /// that must reach the column as NULL rather than as a date MySQL guessed at.
        /// </summary>
        private object DateOrNull(string key)
        {
            // yyyy-MM-dd ONLY, and deliberately so. The extractor normalises a date only
            // when a NAMED MONTH fixed the order; a purely numeric reading such as the
            // "JUN 2 6 2018" stamp coming back as "2-6-2018" is left as printed because
            // nothing on the page says which number is the month. A loose parse would
            // resolve that to 6 February and write a date the certificate never carried -
            // the same fabrication CorrectDate was fixed for on 2026-09-04. An ambiguous
            // reading stays NULL and stays visible in the grid for the operator to type.
            return DateTime.TryParseExact(V(key), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                          DateTimeStyles.None, out DateTime d)
                ? (object)d.Date : DBNull.Value;
        }
    }
}
