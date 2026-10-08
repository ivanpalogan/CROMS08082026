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
        private string _sourceLabel;        // Step 10 - where _scanBytes came from (filename / "Mobile Capture" / "Phone scan")
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

        /// <summary>
        /// The Marriage License photographed in the same Mobile Capture session as the
        /// certificate. OCR never reads it - it travels with the reviewed values to Form 97 and
        /// is saved as marriages.license_image. Cleared whenever a different page is loaded.
        /// </summary>
        private byte[] _licenseBytes;

        /// <summary>
        /// Set when this window was opened FROM a Marriage Registration (the Form 97 wizard) to
        /// review a capture. Auto-Fill then fills THAT form - the same record, queue ticket and
        /// transaction - and closes this window, instead of opening a second, blank Form 97.
        /// </summary>
        private MarriageEntryForm _returnTo;

        /// <summary>
        /// Set when this window was opened FROM the Birth Record Digitization wizard
        /// (Registry Books gallery -&gt; "+ Digitize Old Record" -&gt; Scan/Upload with OCR) to
        /// review an already-registered physical record being converted into a digital one.
        /// Auto-Fill fills THAT wizard's fields and closes this window - the record is never
        /// committed straight from here, unlike the routine backlog batch flow below.
        /// </summary>
        private BirthRegistrationForm _returnToBirth;

        /// <summary>
        /// Set by <see cref="RunLegacyDigitization"/> when this window was opened from a
        /// Civil Registry Record's "+ Digitize Old Record" -&gt; "Scan / Upload with OCR"
        /// chooser (Birth, Marriage or Death Record). In this mode Commit/Draft write
        /// straight into the backlog table (record_source = 'OCR-Backlog') for ALL THREE
        /// kinds — including Marriage, which outside this mode only ever Auto-Fills into the
        /// live Form 97 dialog — and Auto-Fill is disabled outright, because a digitized old
        /// record must never be routed into a live registration screen.
        /// </summary>
        private bool _legacyBacklogMode;

        /// <summary>The record type the Civil Registry Record screen that opened this window
        /// expects back — a scan read as a different kind is refused rather than silently
        /// filed under the wrong record type.</summary>
        private DocKind? _legacyExpectedKind;

        // ---- Civil Registry Record digitization wizard (STEP 6) -------------------------
        // Groups the review grid into the certificate's own logical blocks instead of showing
        // every field at once. Every OCR-filled value stays in the SAME grid cells this screen
        // has always used, so nothing about how a field is edited or saved changes; only which
        // rows are visible at a time does. One StepStrip/GoToStep pair is reused for all three
        // kinds — WizardStepTitles(kind)/WizardStepSections(kind) pick the right array, and the
        // step strip's own captions are rewritten (StepStrip.SetTitle) whenever the wizard turns
        // on for a document of a different kind. Only offered for a document with a recognised
        // FormDefinition (Sections.Count > 0) — an unrecognised layout keeps the flat grid.
        private static readonly string[] BirthWizardStepTitles =
        {
            "Child Information", "Mother Information", "Father Information",
            "Other Birth Certificate Information", "Registry Information", "Review and Save"
        };

        private static readonly string[][] BirthWizardStepSections =
        {
            new[] { "1-5. Child" },
            new[] { "6-12. Mother" },
            new[] { "13-17. Father" },
            new[] { "18. Marriage of Parents", "19/21a. Attendant at Birth",
                     "Read from the whole page", "Other Entries" },
            new[] { "Form Identification", "19b/21b. Certification of Attendant",
                     "20/22. Certification of Informant", "21/23. Prepared By",
                     "22/24. Received at the Office of the Civil Registrar",
                     "25. Registered at the Civil Registrar" },
            null // Review and Save — every row, regardless of section
        };

        // MF-97: First Spouse / Second Spouse / Marriage Information (licence, place+date,
        // solemnizer, witnesses) / Other Marriage Certificate Information (parents of the
        // contracting parties + anything the catalog has no section for) / Registry
        // Information (form identity + the office's own receiving block) / Review and Save.
        private static readonly string[] MarriageWizardStepTitles =
        {
            "First Spouse Information", "Second Spouse Information", "Marriage Information",
            "Other Marriage Certificate Information", "Registry Information", "Review and Save"
        };

        private static readonly string[][] MarriageWizardStepSections =
        {
            new[] { "1-8. Husband" },
            new[] { "1-8. Wife" },
            new[] { "13. Marriage Licence", "14-16. Place and Date of Marriage",
                     "17. Solemnizing Officer", "18. Witnesses" },
            new[] { "9-12. Parents of the Contracting Parties", "Other Entries" },
            new[] { "Form Identification", "Received at the Office of the Civil Registrar" },
            null // Review and Save — every row, regardless of section
        };

        // MF-103: Deceased Information / Death Information (cause + disposal) / Parent-Family
        // Information / Other Death Certificate Information (informant + anything unsectioned)
        // / Registry Information (form identity + prepared/received/registered) / Review+Save.
        private static readonly string[] DeathWizardStepTitles =
        {
            "Deceased Information", "Death Information", "Parent/Family Information",
            "Other Death Certificate Information", "Registry Information", "Review and Save"
        };

        private static readonly string[][] DeathWizardStepSections =
        {
            new[] { "1-8. Deceased" },
            new[] { "Medical Certificate", "24-25. Corpse Disposal" },
            new[] { "9-10. Parents" },
            new[] { "26. Certification of Informant", "Other Entries" },
            new[] { "Form Identification", "27. Prepared By", "28. Received By",
                     "29. Registered by the Civil Registrar" },
            null // Review and Save — every row, regardless of section
        };

        private static string[] WizardStepTitles(DocKind kind)
        {
            switch (kind)
            {
                case DocKind.Marriage: return MarriageWizardStepTitles;
                case DocKind.Death: return DeathWizardStepTitles;
                default: return BirthWizardStepTitles;
            }
        }

        private static string[][] WizardStepSections(DocKind kind)
        {
            switch (kind)
            {
                case DocKind.Marriage: return MarriageWizardStepSections;
                case DocKind.Death: return DeathWizardStepSections;
                default: return BirthWizardStepSections;
            }
        }

        private StepStrip _stepStrip;
        private Button _btnStepBack, _btnStepNext;
        private bool _wizardActive;
        private int _stepIndex;
        private Point _dgvHomeLoc;
        private Size _dgvHomeSize;
        private readonly Dictionary<string, string> _keyToSection =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public OcrDigitizationForm()
        {
            InitializeComponent();
            dgvBatch.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            BuildFieldGrid();
            SetupWizardChrome();
            SetupScanQrButton();

            bool ok = DocumentAI.IsAvailable();
            lblEngine.Text = ok ? "OCR engine: ready" : "OCR engine: NOT FOUND (install Tesseract eng data)";
            lblEngine.ForeColor = ok ? Color.FromArgb(25, 135, 84) : Color.FromArgb(220, 53, 69);

            lblSubtitle.Text = "Load a scanned certificate or registry page. CROMS identifies the " +
                "document, extracts its fields with a confidence for each, and flags anything to check.";

            // Comparing the grid against the scan is the whole review step, so selecting a
            // field draws its box on the page.
            dgvFields.SelectionChanged += (s, e) => pbScan.Invalidate();
            // Clicking a field name also zooms the scan onto where that value was read.
            dgvFields.CurrentCellChanged += DgvFields_ZoomOnSelect;
            dgvFields.CellEndEdit += DgvFields_CellEndEdit;
            dgvFields.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvFields.IsCurrentCellDirty && dgvFields.CurrentCell != null
                    && dgvFields.CurrentCell.OwningColumn.Name == "Verified")
                    dgvFields.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvFields.CellValueChanged += DgvFields_CellValueChanged;
            pbScan.Paint += PbScan_Paint;

            // Two-finger pinch + drag on a touchscreen; Ctrl + wheel covers a mouse and a
            // precision touchpad's pinch. All three zoom about the point under the fingers.
            var pinch = new PinchZoom(ZoomAt, PanBy, pnlScanHost, pbScan);
            var wheel = new CtrlWheelZoom(pnlScanHost,
                dir => ZoomAt(dir > 0 ? 1.15f : 1f / 1.15f, Cursor.Position));
            Disposed += (s, e) => { pinch.Dispose(); wheel.Dispose(); };

            // A phone scan lands here first (uploaded via the save-API, no on-device OCR) —
            // double-clicking a still-unopened 'Mobile' row loads it into the engine the same
            // way Load Image does, then runs the full desktop pipeline on it.
            dgvBatch.CellDoubleClick += DgvBatch_CellDoubleClick;
            // Single-click the Upload ID / Reg. No. cell (the record link) opens it too, so
            // the operator doesn't have to know double-click is required.
            dgvBatch.CellClick += DgvBatch_CellClick;

            ApplyResultToUi();
            UpdateBatchModeButtons();
            LoadBatch();
        }

        public void RefreshData() { LoadBatch(); }

        // ---- mobile scanner QR ----------------------------------------------

        /// <summary>
        /// Adds the "Mobile Capture" button beside Load Image: a QR that turns the office
        /// phone into a camera for THIS window. The phone only takes the picture (and a
        /// Marriage License photo, when there is one) - the desktop OCR reads it here, no
        /// queue ticket or registration needs to be open first. The older "Phone Scanner
        /// App" QR (a deep link into ORCMobile_Application's own scan screen) is retired -
        /// this button covers the same job through one capture path.
        /// </summary>
        private void SetupScanQrButton()
        {
            var cap = new Button
            {
                Text = "Mobile Capture",
                FlatStyle = FlatStyle.Flat,
                Font = new Font(btnLoad.Font, FontStyle.Bold),
                BackColor = UiTheme.Accent,
                ForeColor = Color.White,
                Size = new Size(180, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(btnLoad.Left - 190, btnLoad.Top),
                UseVisualStyleBackColor = false,
            };
            cap.Click += async (s, e) => await StartMobileCapture();
            Controls.Add(cap);
            cap.BringToFront();
        }

        /// <summary>Opens a Mobile Capture session and, once the certificate arrives, runs OCR on it here.</summary>
        private async Task StartMobileCapture()
        {
            using (var dlg = new MobileCaptureDialog(Form97Capture.PurposeOcrCapture, null, null, null, null, null))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Docs == null) return;
                await LoadCapture(dlg.Docs.PrimaryCertificate, dlg.Docs.PrimaryLicense, "Mobile Capture");
            }
        }

        /// <summary>
        /// Puts a phone-captured certificate on screen exactly as Load Image does for a file,
        /// keeps the licence photo beside it, and runs the desktop OCR on the certificate.
        /// </summary>
        public async Task LoadCapture(byte[] certificate, byte[] license, string sourceLabel)
        {
            if (certificate == null || certificate.Length == 0) return;
            Bitmap loaded;
            try { loaded = DocumentAI.LoadImageBytes(certificate); }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not open the captured photo: " + ex.Message, "Mobile Capture",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _image?.Dispose();
            _image = loaded;
            _scanBytes = certificate;
            _sourceLabel = sourceLabel ?? "Mobile Capture";
            _licenseBytes = license;
            _zoom = 1f;
            pbScan.SizeMode = PictureBoxSizeMode.Zoom;
            pnlScanHost.AutoScrollPosition = Point.Empty;
            pbScan.Location = Point.Empty;
            pbScan.Size = pnlScanHost.ClientSize;
            pbScan.Image = _image;
            grpScan.Text = "SCANNED DOCUMENT — " + (sourceLabel ?? "Mobile Capture") +
                (license != null ? "   (+ Marriage License photo)" : "");

            _scanId = null;
            _result = null;
            _kind = DocKind.Unknown;
            _formDef = null;
            _savedRecordId = null;
            _seals = new List<SealDetector.Seal>();
            txtDocClass.Clear();
            dgvFields.Rows.Clear();
            SetupWizard(false);
            ApplyResultToUi();

            await Analyze();
        }

        /// <summary>
        /// The Form 97 wizard's review step: opens this window as a dialog over the Marriage
        /// Registration, reads the captured certificate, and lets the operator check the
        /// fields exactly as on the normal screen. Auto-Fill fills <paramref name="target"/>
        /// (the same record) and closes. Returns true when the form was filled.
        /// </summary>
        public static bool ReviewForMarriage(IWin32Window owner, MarriageEntryForm target,
            byte[] certificate, byte[] license, string sourceLabel)
        {
            using (var ocr = new OcrDigitizationForm())
            {
                ocr._returnTo = target;
                ocr.FormBorderStyle = FormBorderStyle.Sizable;
                ocr.StartPosition = FormStartPosition.CenterParent;
                ocr.WindowState = FormWindowState.Maximized;
                ocr.ShowInTaskbar = false;
                ocr.MinimizeBox = false;
                ocr.Text = "OCR Review  -  Marriage Registration (Form 97)";
                ocr.lblSubtitle.Text = "Check every field against the photo, correct anything flagged, then press " +
                    "Auto-Fill Marriage to send the values back to the Marriage Registration.";
                UiTheme.Polish(ocr);
                ocr.Shown += async (s, e) => await ocr.LoadCapture(certificate, license, sourceLabel);
                return ocr.ShowDialog(owner) == DialogResult.OK;
            }
        }

        /// <summary>
        /// The Birth Record Digitization wizard's "Scan / Upload with OCR" step: opens this
        /// window as a dialog over that wizard (Mode = Legacy Birth Digitization,
        /// RecordType = Birth), reads the scanned physical record, and lets the operator check
        /// the fields exactly as on the normal screen. Auto-Fill fills <paramref name="target"/>
        /// and closes - the record is reviewed and saved through the wizard, never committed
        /// straight from here. Returns true when the wizard was filled.
        /// </summary>
        public static bool ReviewForBirthDigitization(IWin32Window owner, BirthRegistrationForm target,
            byte[] scan, string sourceLabel)
        {
            using (var ocr = new OcrDigitizationForm())
            {
                ocr._returnToBirth = target;
                ocr.FormBorderStyle = FormBorderStyle.Sizable;
                ocr.StartPosition = FormStartPosition.CenterParent;
                ocr.WindowState = FormWindowState.Maximized;
                ocr.ShowInTaskbar = false;
                ocr.MinimizeBox = false;
                ocr.Text = "OCR Review  -  Birth Record Digitization";
                ocr.lblSubtitle.Text = "Legacy Birth Digitization - check every field against the photo, correct anything " +
                    "flagged, then press Auto-Fill Birth to send the values back to the digitization wizard.";
                UiTheme.Polish(ocr);
                ocr.Shown += async (s, e) => await ocr.LoadCapture(scan, null, sourceLabel);
                return ocr.ShowDialog(owner) == DialogResult.OK;
            }
        }

        /// <summary>
        /// Civil Registry Record's "+ Digitize Old Record" -&gt; "Scan / Upload with OCR"
        /// chooser. Opens this screen standalone (not attached to any live registration form)
        /// so Commit/Draft — which write straight into the backlog table tagged
        /// <c>record_source = 'OCR-Backlog'</c> — are the only way values leave this window;
        /// Auto-Fill is disabled here for every kind, since routing a digitized OLD record
        /// into a live New Registration screen is exactly what Steps 3-4 of the digitization
        /// workflow forbid. A scan read as something other than <paramref name="expectedKind"/>
        /// is still shown, but Commit/Draft stay disabled until it matches.
        /// </summary>
        public static void RunLegacyDigitization(IWin32Window owner, DocKind expectedKind)
        {
            using (var ocr = new OcrDigitizationForm())
            {
                ocr._legacyBacklogMode = true;
                ocr._legacyExpectedKind = expectedKind;
                ocr.FormBorderStyle = FormBorderStyle.Sizable;
                ocr.StartPosition = FormStartPosition.CenterParent;
                ocr.WindowState = FormWindowState.Maximized;
                ocr.Text = DocumentAI.KindName(expectedKind) + " Record Digitization";
                ocr.lblSubtitle.Text =
                    "Scan or upload the physical registry page for this already-registered " +
                    DocumentAI.KindName(expectedKind).ToLowerInvariant() + " record, review the " +
                    "values against the page, then Commit to add it to the backlog. This does " +
                    "not create a new registration.";
                UiTheme.Polish(ocr);
                ocr.Shown += async (s, e) => await ocr.AskScanSource();
                ocr.ShowDialog(owner);
            }
        }

        /// <summary>
        /// Legacy digitization only: asks where the page comes from, then hands off to the
        /// same paths the toolbar buttons use. Closing the chooser leaves the window open so
        /// the operator can still use Load Image / Mobile Capture themselves.
        /// </summary>
        private async Task AskScanSource()
        {
            int pick = 0; // 1 = phone QR, 2 = image file
            using (var dlg = new Form())
            {
                dlg.Text = "Add the scanned page";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false; dlg.MinimizeBox = false; dlg.ShowInTaskbar = false;
                dlg.ClientSize = new Size(420, 210);
                var head = new Label
                {
                    Text = "How do you want to add the page?",
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    AutoSize = false, Location = new Point(20, 16), Size = new Size(380, 28)
                };
                var btnPhone = new Button
                {
                    Text = "Mobile Capture (scan QR with phone)",
                    Location = new Point(20, 60), Size = new Size(380, 48)
                };
                var btnFile = new Button
                {
                    Text = "Upload image file from this PC",
                    Location = new Point(20, 116), Size = new Size(380, 48)
                };
                var btnSkip = new Button
                {
                    Text = "Cancel", Location = new Point(300, 170), Size = new Size(100, 30),
                    DialogResult = DialogResult.Cancel
                };
                btnPhone.Click += (s, e) => { pick = 1; dlg.DialogResult = DialogResult.OK; };
                btnFile.Click += (s, e) => { pick = 2; dlg.DialogResult = DialogResult.OK; };
                dlg.Controls.AddRange(new Control[] { head, btnPhone, btnFile, btnSkip });
                dlg.CancelButton = btnSkip;
                UiTheme.Polish(dlg);
                dlg.ShowDialog(this);
            }

            if (pick == 1) await StartMobileCapture();
            else if (pick == 2) btnLoad_Click(this, EventArgs.Empty);
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
                _sourceLabel = System.IO.Path.GetFileName(ofd.FileName);
                _licenseBytes = null;   // a file has no licence photo with it
                _zoom = 1f;
                pbScan.SizeMode = PictureBoxSizeMode.Zoom;
                pnlScanHost.AutoScrollPosition = Point.Empty;
                pbScan.Location = Point.Empty;
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
                SetupWizard(false);
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
                // The engine gets its OWN copy. _image is also what pbScan is painting, and GDI+ refuses
                // a read on one thread while another is drawing the same bitmap ("Object is currently in
                // use elsewhere"), so any repaint during the 20-40 s read would fail the whole scan.
                Bitmap work = new Bitmap(_image);
                DocAiResult r;
                try { r = await Task.Run(() => DocumentAI.Analyze(work)); }
                finally { work.Dispose(); }
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

                // Registry Information (Step 7 of the Civil Registry Record digitization
                // workflow): where this record belongs in the archive — never read off the
                // page and never generated from it, so these start blank for the operator to
                // copy from the physical ledger. Always shown for Birth and Death (both commit
                // straight to their backlog table from this screen, in or out of legacy mode);
                // shown for Marriage only in legacy mode, since a normal-mode Marriage scan
                // never writes a book/page itself — it only Auto-Fills into the live Form 97.
                // Revalidate folds them into the normal Missing/Ok scoring without dragging
                // down OverallConfidence (blanks are excluded from that average).
                if (r.Kind == DocKind.Birth || r.Kind == DocKind.Death
                    || (_legacyBacklogMode && r.Kind == DocKind.Marriage))
                {
                    EnsureRegistryInfoFields(r);
                    DocIntelligence.Revalidate(r);
                }

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

                // Civil Registry Record digitization opened expecting one kind of certificate
                // (Birth Record, Marriage Record or Death Record) — a scan that reads as a
                // different kind is shown, same as any other scan, but Commit/Draft stay
                // disabled (see ApplyResultToUi) and this is the one case worth a pop-up: the
                // operator is standing at a screen for a specific record type and needs to
                // know they loaded the wrong page.
                if (_legacyBacklogMode && _legacyExpectedKind != null
                    && r.Kind != DocKind.Unknown && r.Kind != _legacyExpectedKind)
                {
                    MessageBox.Show(
                        "This page reads as a " + DocumentAI.KindName(r.Kind) + " document, not a " +
                        DocumentAI.KindName(_legacyExpectedKind.Value) +
                        " — it cannot be committed to the " + DocumentAI.KindName(_legacyExpectedKind.Value) +
                        " Record backlog from here. Load the correct page, or close this window and open " +
                        "\"+ Digitize Old Record\" from the " + DocumentAI.KindName(r.Kind) + " Record screen instead.",
                        "Wrong document type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

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
            bool death = have && _kind == DocKind.Death;
            bool marriage = have && _kind == DocKind.Marriage;

            // Birth and Death commit straight into the database from this screen — that is
            // the whole point of digitizing an old registry book, and it must never pass
            // through the live Birth/Death Registration screens (those are for today's
            // walk-in registrations). Only Marriage still routes through Auto-Fill, into the
            // Form 97 dialog. The Birth Record Digitization wizard's OCR review step
            // (_returnToBirth) is the third case: it also routes through Auto-Fill, into that
            // wizard's own fields, so Commit/Draft — which would write a second, wizard-less
            // row straight into `births` — are disabled while a wizard is waiting for this
            // scan's values.
            bool birthReturn = _returnToBirth != null;
            // The wizard's own "Review and Save" step is where Commit / Draft / Auto-Fill
            // actually run — every earlier step is for reading and correcting one certificate
            // block at a time, not for saving from.
            bool reviewStepOk = !_wizardActive || _stepIndex == WizardStepTitles(_kind).Length - 1;

            if (_legacyBacklogMode)
            {
                // Civil Registry Record digitization: an already-registered PAPER record is
                // being converted into a database row, never a new registration. So Commit/
                // Draft write straight to the backlog for all three kinds — including
                // Marriage, which outside this mode has no backlog-commit path at all — and
                // Auto-Fill is switched off entirely, because routing a digitized old record
                // into a live registration screen is exactly what this mode exists to avoid.
                bool matchesExpected = _legacyExpectedKind == null || _kind == _legacyExpectedKind;
                bool canCommit = have && matchesExpected && !blocked && reviewStepOk;
                btnCommit.Enabled = canCommit;
                btnDraft.Enabled = canCommit;
                btnCommit.Text = (_wizardActive && have && reviewStepOk)
                    ? "Save Digitized Record" : "Commit to Registry";
                btnAutoFill.Enabled = false;
                btnAutoFill.Visible = false;
                btnReview.Enabled = _result != null;
            }
            else
            {
                btnCommit.Enabled = (birth || death) && !blocked && !birthReturn && reviewStepOk;
                btnDraft.Enabled = (birth || death) && !blocked && !birthReturn && reviewStepOk;
                // Final Verification (the wizard's last step) is a distinct check from OCR
                // verification — it asks whether the record is correct and complete, not
                // whether OCR read it right — so its own commit button says so, rather than
                // reusing the generic "Commit to Registry" caption used everywhere else.
                btnCommit.Text = (_wizardActive && have && reviewStepOk)
                    ? "Save Digitized Record" : "Commit to Registry";
                btnAutoFill.Enabled = have && !blocked && reviewStepOk &&
                    (marriage || (birthReturn && birth));
                btnAutoFill.Visible = true;
                btnReview.Enabled = _result != null;
            }

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
        /// STEP 7 — REGISTRY INFORMATION. Adds Date of Registration, Registry Book Number
        /// and Page Number to the review grid as plain manual-entry rows alongside the
        /// already-extracted Registry Number — where this record belongs in the Birth
        /// Records Archive.
        /// <para/>
        /// These three are NEVER auto-generated and NEVER read off the page: the office's
        /// physical ledger is the only source, so every one of them starts blank and stays
        /// blank until the operator copies it from the book in front of them. This is the
        /// fix for a real defect this same commit path used to have — it silently derived
        /// a "book" number from the child's parsed date of birth (a YEAR guess, not the
        /// office's actual book/page), and never wrote a page number or a registration
        /// date at all. A guessed archive location is exactly the kind of fabricated fact
        /// this project's OCR pipeline has refused everywhere else since 2026-09-04.
        /// </summary>
        private void EnsureRegistryInfoFields(DocAiResult r)
        {
            void AddIfMissing(string key, string label)
            {
                if (r.Fields.Any(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase)))
                    return;
                r.Fields.Add(new DocField(key, label, "", false));
            }

            AddIfMissing("DateOfRegistration", "Date of Registration");
            // No separate Year field: this office's book_volume already IS the registry
            // book's year (established when Birth's own Year box was retired on 2026-09-02
            // in favour of this one column — Registry Books groups records by it the same
            // way). A second Year box would just be the same fact typed twice, with nothing
            // to stop the two disagreeing.
            AddIfMissing("BookVolume", "Registry Book Number (Year)");
            AddIfMissing("BookPage", "Page Number");
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
            _keyToSection.Clear();

            if (_formDef == null || _formDef.Sections.Count == 0)
            {
                foreach (DocField f in r.Fields) AddFieldRow(f);
                SetupWizard(false);
                return;
            }

            var byKey = new Dictionary<string, DocField>(StringComparer.OrdinalIgnoreCase);
            foreach (DocField f in r.Fields) byKey[f.Key] = f;

            foreach (var section in _formDef.OrderedSections(byKey.Keys))
            {
                AddSectionRow(section.Key);
                foreach (string key in section.Value)
                {
                    if (byKey.TryGetValue(key, out DocField f))
                    {
                        AddFieldRow(f);
                        _keyToSection[key] = section.Key;
                    }
                }
            }

            SetupWizard(_kind == DocKind.Birth || _kind == DocKind.Marriage || _kind == DocKind.Death);
        }

        /// <summary>
        /// Builds the step strip + Back/Next controls once, on top of the grid's own
        /// footprint — hidden until a document with a recognised layout is on screen. The
        /// strip's captions are placeholder Birth titles at construction time; whichever kind
        /// activates the wizard first rewrites them via <see cref="StepStrip.SetTitle"/> in
        /// <see cref="SetupWizard"/>. Code-built rather than added to the Designer: this
        /// screen's Designer has been silently regenerated before (2026-09-02/09-10 entries),
        /// and a control added purely in code cannot be lost that way.
        /// </summary>
        private void SetupWizardChrome()
        {
            _dgvHomeLoc = dgvFields.Location;
            _dgvHomeSize = dgvFields.Size;

            _stepStrip = new StepStrip(true)
            {
                Dock = DockStyle.None,
                Location = _dgvHomeLoc,
                Size = new Size(_dgvHomeSize.Width, 54),
                Visible = false
            };
            foreach (string title in BirthWizardStepTitles) _stepStrip.AddStep(title, "");
            _stepStrip.StepClicked += GoToStep;
            grpFields.Controls.Add(_stepStrip);
            _stepStrip.BringToFront();

            _btnStepBack = new Button
            {
                Text = "◀ Back",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Location = new Point(116, 500),
                Size = new Size(80, 40),
                Visible = false
            };
            _btnStepBack.Click += (s, e) => GoToStep(_stepIndex - 1);

            _btnStepNext = new Button
            {
                Text = "Next ▶",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Location = new Point(200, 500),
                Size = new Size(88, 40),
                Visible = false
            };
            _btnStepNext.Click += (s, e) => GoToStep(_stepIndex + 1);

            grpFields.Controls.Add(_btnStepBack);
            grpFields.Controls.Add(_btnStepNext);
        }

        /// <summary>
        /// Turns the step wizard on/off for the document currently on screen. Any of Birth,
        /// Marriage or Death gets it, as long as the document's layout was recognised (so it
        /// has real sections to group by); everything else keeps the plain, fully-visible
        /// sectioned grid it always had. Rewrites the strip's captions to match the current
        /// kind's step titles before showing it — the same six-step strip is reused for all
        /// three, not a separate wizard per kind.
        /// </summary>
        private void SetupWizard(bool active)
        {
            _wizardActive = active && _stepStrip != null;
            _stepStrip.Visible = _wizardActive;
            _btnStepBack.Visible = _wizardActive;
            _btnStepNext.Visible = _wizardActive;

            if (_wizardActive)
            {
                string[] titles = WizardStepTitles(_kind);
                for (int s = 0; s < _stepStrip.Count && s < titles.Length; s++)
                    _stepStrip.SetTitle(s, titles[s]);

                dgvFields.Location = new Point(_dgvHomeLoc.X, _dgvHomeLoc.Y + _stepStrip.Height);
                dgvFields.Size = new Size(_dgvHomeSize.Width, _dgvHomeSize.Height - _stepStrip.Height);
                GoToStep(0);
            }
            else
            {
                dgvFields.Location = _dgvHomeLoc;
                dgvFields.Size = _dgvHomeSize;
                _stepIndex = 0;
                foreach (DataGridViewRow row in dgvFields.Rows) row.Visible = true;
                ApplyResultToUi();
            }
        }

        /// <summary>
        /// Shows only the rows that belong to step <paramref name="i"/>'s certificate blocks
        /// — every OCR-filled value stays editable in place, only which rows are visible
        /// changes. "Review and Save" (the last step) shows every row, and Commit / Draft /
        /// Auto-Fill only become enabled once the operator has reached it (see
        /// <see cref="ApplyResultToUi"/>). Reads whichever kind is currently on screen —
        /// <see cref="WizardStepTitles"/>/<see cref="WizardStepSections"/> — so the one strip
        /// serves Birth, Marriage and Death without three copies of this method.
        /// </summary>
        private void GoToStep(int i)
        {
            if (!_wizardActive) return;
            string[] titles = WizardStepTitles(_kind);
            i = Math.Max(0, Math.Min(titles.Length - 1, i));
            _stepIndex = i;

            bool lastStep = i == titles.Length - 1;
            string[] allow = WizardStepSections(_kind)[i];

            DataGridViewRow firstVisible = null;
            foreach (DataGridViewRow row in dgvFields.Rows)
            {
                bool visible;
                if (lastStep) visible = true;
                else if (row.Tag is string title) visible = allow.Contains(title, StringComparer.OrdinalIgnoreCase);
                else if (row.Tag is DocField f && _keyToSection.TryGetValue(f.Key, out string sec))
                    visible = allow.Contains(sec, StringComparer.OrdinalIgnoreCase);
                else
                    visible = false; // an unsectioned row is only ever shown on Review

                row.Visible = visible;
                if (visible && firstVisible == null) firstVisible = row;
            }
            if (firstVisible != null) dgvFields.FirstDisplayedScrollingRowIndex = firstVisible.Index;

            for (int s = 0; s < _stepStrip.Count; s++)
                _stepStrip.SetState(s, s < i ? StepStrip.State.Done
                                    : s == i ? StepStrip.State.Current : StepStrip.State.Todo);

            _btnStepBack.Enabled = i > 0;
            _btnStepNext.Text = lastStep ? "Review" : "Next ▶";
            _btnStepNext.Enabled = !lastStep;

            ApplyResultToUi();
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
            RectangleF? box = FieldRectOnPicture(f, out bool estimated);
            if (box == null) return;
            RectangleF rect = box.Value;
            rect.Inflate(4f, 4f);

            if (estimated)
            {
                // No measured region (blank field, or the layout did not fit this page) —
                // this is the TEMPLATE's own position for the field, drawn as a guess, not
                // a confirmed read. Dashed amber rather than solid blue so the operator
                // never mistakes "roughly here" for "this is where it was read".
                using (var pen = new Pen(Color.FromArgb(230, 217, 119, 6), 2f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                using (var wash = new SolidBrush(Color.FromArgb(28, 217, 119, 6)))
                using (var font = new Font(Font.FontFamily, 8f, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.FromArgb(230, 180, 90, 0)))
                {
                    e.Graphics.FillRectangle(wash, rect);
                    e.Graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                    e.Graphics.DrawString("not found — estimated position, type it in",
                        font, textBrush, rect.X, Math.Max(0, rect.Y - 16));
                }
            }
            else
            {
                using (var pen = new Pen(Color.FromArgb(220, 13, 110, 253), 2f))
                using (var wash = new SolidBrush(Color.FromArgb(40, 13, 110, 253)))
                {
                    e.Graphics.FillRectangle(wash, rect);
                    e.Graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
        }

        /// <summary>
        /// Where the field's region lands inside pbScan at its CURRENT size. Page pixels
        /// -> source image pixels -> the box, honouring PictureBoxSizeMode.Zoom
        /// letterboxing.
        /// <para/>
        /// When the field was never measured (blank, or the page's layout was rejected so
        /// nothing was region-read at all), <paramref name="estimated"/> comes back true
        /// and the box is the TEMPLATE's own field rectangle for this document's
        /// candidate form — read by <see cref="TemplateRect"/> — under the assumption that
        /// this page has roughly the template's proportions. That is a guess, not a
        /// measurement: there is no page-fit to correct it, so it is only ever offered so
        /// the operator has somewhere to look while typing the value in by hand, never
        /// treated as a real region elsewhere (audit, printing, scoring all still see the
        /// field as blank).
        /// </summary>
        private RectangleF? FieldRectOnPicture(DocField f, out bool estimated)
        {
            estimated = false;
            if (_image == null || _result == null) return null;
            if (_result.PageWidth <= 0 || _result.PageHeight <= 0) return null;

            double toImage = (double)_image.Width / _result.PageWidth;
            double normX, normY, normW, normH;

            if (f.Region.Width > 0 && f.Region.Height > 0)
            {
                normX = f.Region.X * toImage;
                normY = f.Region.Y * toImage;
                normW = f.Region.Width * toImage;
                normH = f.Region.Height * toImage;
            }
            else
            {
                RectangleF? tmpl = TemplateRect(f.Key);
                if (tmpl == null) return null;
                estimated = true;
                normX = tmpl.Value.X * _image.Width;
                normY = tmpl.Value.Y * _image.Height;
                normW = tmpl.Value.Width * _image.Width;
                normH = tmpl.Value.Height * _image.Height;
            }

            double fit = Math.Min((double)pbScan.ClientSize.Width / _image.Width,
                                  (double)pbScan.ClientSize.Height / _image.Height);
            double offsetX = (pbScan.ClientSize.Width - _image.Width * fit) / 2.0;
            double offsetY = (pbScan.ClientSize.Height - _image.Height * fit) / 2.0;

            return new RectangleF(
                (float)(normX * fit + offsetX),
                (float)(normY * fit + offsetY),
                (float)(normW * fit),
                (float)(normH * fit));
        }

        /// <summary>
        /// The candidate form's own template rectangle (0-1 page space) for a field key,
        /// used only as a fallback when nothing was actually region-read for it. Matched
        /// against <see cref="DocAiResult.CandidateLayoutCode"/> — the form the page was
        /// classified as, even when its layout did not fit and was rejected — so a blank
        /// field on an unfitted page can still point at roughly where it belongs.
        /// </summary>
        private RectangleF? TemplateRect(string key)
        {
            string code = _result?.CandidateLayoutCode;
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(key)) return null;
            FormLayout layout = DocLayouts.All.FirstOrDefault(l => l.Code == code);
            FieldSpec spec = layout?.Fields.FirstOrDefault(s => s.Key == key);
            return spec?.Rect;
        }

        /// <summary>
        /// Zoom the scan in on the selected field and scroll it to the middle of the
        /// viewer, so clicking a field name shows exactly what was read. The zoom is
        /// absolute (fits the field to ~45% of the viewer width, 1x-6x), so clicking
        /// the next field re-frames instead of compounding.
        /// </summary>
        private void ZoomToField(DocField f)
        {
            if (_image == null || f == null) return;

            // measure at the base (fit-page) size so the target zoom is independent of
            // whatever zoom the operator is currently at
            pbScan.Size = pnlScanHost.ClientSize;
            RectangleF? baseBox = FieldRectOnPicture(f, out _);
            if (baseBox == null) { _zoom = 1f; pbScan.Invalidate(); return; }

            float target = pnlScanHost.ClientSize.Width * 0.45f / Math.Max(1f, baseBox.Value.Width);
            _zoom = Math.Max(1f, Math.Min(6f, target));

            pbScan.Size = new Size(
                (int)(pnlScanHost.ClientSize.Width * _zoom),
                (int)(pnlScanHost.ClientSize.Height * _zoom));

            RectangleF box = FieldRectOnPicture(f, out _).Value;
            int x = (int)(box.X + box.Width / 2f - pnlScanHost.ClientSize.Width / 2f);
            int y = (int)(box.Y + box.Height / 2f - pnlScanHost.ClientSize.Height / 2f);
            pnlScanHost.AutoScrollPosition = new Point(Math.Max(0, x), Math.Max(0, y));
            pbScan.Invalidate();
        }

        private void DgvFields_ZoomOnSelect(object sender, EventArgs e)
        {
            if (_updatingFieldGrid) return;
            if (!(dgvFields.CurrentRow?.Tag is DocField f)) return;
            if (ReferenceEquals(f, _zoomedField)) return;   // same row, another column
            _zoomedField = f;
            ZoomToField(f);
        }
        private DocField _zoomedField;

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

            if (_returnTo != null || _returnToBirth != null)
            {
                // Wizard review step done - back to the same Marriage Registration, or to the
                // Birth Record Digitization wizard.
                DialogResult = DialogResult.OK;
                return;
            }

            MessageBox.Show(
                "The " + ModuleName(_kind) + " form has been filled from the confirmed values. The " +
                "scanned copy will be saved with the record when you Save.",
                "Auto-filled", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnCommit_Click(object sender, EventArgs e)
        {
            if (!ReadyToSave()) return;
            string table = TableFor(_kind);
            string registry = RegistryWord(_kind);
            if (!Confirm("commit to the " + registry + " registry")) return;

            // STEP 9 — locate the registry book by the STAFF-VERIFIED Book No. + Year
            // (EnsureRegistryInfoFields), never by unverified OCR text. A book that does not
            // already exist is never created silently — the operator confirms it first.
            // Applies to all three kinds: Marriage can now commit straight to its own
            // backlog too, in legacy digitization mode.
            if (!ConfirmRegistryBook()) return;

            long id;
            if (!TrySaveOnce(() => _kind == DocKind.Death ? SaveDeath("Registered")
                                 : _kind == DocKind.Marriage ? SaveMarriageLegacy("Registered")
                                 : SaveBirth("Registered"), out id)) return;
            _savedRecordId = id;
            WriteAudit(OcrAudit.Committed);
            MarkBatch("Committed", table, id);
            Audit.Write(Audit.Create, table, id,
                "Committed as an old record from OCR scan " + _scanId + " as " + FormLabel());
            LoadBatch();
            UpdateFormIdentity();

            // Step 11 — result after saving: land on the record's own registry book
            // (creating its card in the gallery the moment this re-queries, if it's brand
            // new) instead of leaving the operator to go find it by hand. The old standalone
            // "oldbirth"/"olddeath" module keys are gone (2026-09-28) — every workbench now
            // lives inside Records Archive's "Civil Registry Records" group.
            OpenBacklogRecordResult(_kind, id, "Committed to");
        }

        private void btnDraft_Click(object sender, EventArgs e)
        {
            if (!ReadyToSave()) return;
            string table = TableFor(_kind);

            long id;
            if (!TrySaveOnce(() => _kind == DocKind.Death ? SaveDeath("Draft")
                                 : _kind == DocKind.Marriage ? SaveMarriageLegacy("Draft")
                                 : SaveBirth("Draft"), out id)) return;
            _savedRecordId = id;
            WriteAudit(OcrAudit.Draft);
            MarkBatch("Draft", table, id);
            Audit.Write(Audit.Create, table, id,
                "Draft old record from OCR scan " + _scanId + " as " + FormLabel());
            LoadBatch();
            UpdateFormIdentity();

            OpenBacklogRecordResult(_kind, id, "Saved as a draft old record in");
        }

        /// <summary>
        /// Runs a save exactly once per scan and turns a duplicate registry number into a
        /// plain sentence instead of an unhandled exception. A scan that already produced a
        /// record (Commit pressed twice, or Commit after Draft) is refused before touching the
        /// database; a registry number that belongs to ANOTHER record trips the UNIQUE index
        /// (1062) and is reported with the number so the operator can correct it.
        /// </summary>
        private bool TrySaveOnce(Func<long> save, out long id)
        {
            id = 0;
            if (_savedRecordId.HasValue)
            {
                MessageBox.Show(this,
                    "This scan was already saved as record #" + _savedRecordId.Value + ".\n\n" +
                    "Open Records Archive to view or edit it. To digitize another certificate, " +
                    "load a new scan.",
                    "Already saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            try
            {
                id = save();
                return true;
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                ErrorLog.Write("OcrDigitization.Save", ex);
                string reg = V("RegistryNo");
                MessageBox.Show(this,
                    (reg.Length > 0
                        ? "Registry No. \"" + reg + "\" is already used by another record."
                        : "A record with the same registry number already exists.") +
                    "\n\nIf this certificate is already in the registry, do not save it again. " +
                    "Otherwise correct the Registry No. (or Book / Page) in the grid and try again.",
                    "Duplicate registry number", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        private static string RegistryWord(DocKind k)
        {
            switch (k)
            {
                case DocKind.Birth: return "birth";
                case DocKind.Marriage: return "marriage";
                case DocKind.Death: return "death";
                default: return "record";
            }
        }

        private static string RecordLabel(DocKind k)
        {
            switch (k)
            {
                case DocKind.Birth: return "Birth Record";
                case DocKind.Marriage: return "Marriage Record";
                case DocKind.Death: return "Death Record";
                default: return "Record";
            }
        }

        /// <summary>
        /// Step 11 — land on the just-saved record's own workbench (Birth / Marriage / Death
        /// Record under Civil Registry Records), or, if the shell can't be reached, tell the
        /// operator plainly where to find it. One method for all three kinds so Commit and
        /// Draft can't drift into disagreeing about where a record ends up.
        /// </summary>
        private void OpenBacklogRecordResult(DocKind kind, long id, string verbPhrase)
        {
            RecordsArchiveForm archive = Shell()?.GoToModule("archive") as RecordsArchiveForm;
            Form workbench = null;
            if (archive != null)
            {
                if (kind == DocKind.Birth) workbench = archive.OpenBirthRecordWorkbench();
                else if (kind == DocKind.Marriage) workbench = archive.OpenMarriageRecordWorkbench();
                else if (kind == DocKind.Death) workbench = archive.OpenDeathRecordWorkbench();
            }
            if (workbench is OldBirthRecordsForm obr) { obr.OpenToRecord(id); return; }
            if (workbench is OldMarriageRecordsForm omr) { omr.OpenToRecord(id); return; }
            if (workbench is OldDeathRecordsForm odr) { odr.OpenToRecord(id); return; }

            MessageBox.Show(
                verbPhrase + " the " + RegistryWord(kind) + " registry as " + FormLabel() + ".\n\n" +
                "Find, edit, view or delete it from Records Archive → \"" + RecordLabel(kind) +
                "\" — this old record does not appear on the live " +
                RecordLabel(kind).Replace(" Record", "") + " Registration screen.",
                "Document", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        /// <summary>
        /// STEP 9 — find the correct registry book before the record is written.
        /// "Registry book" here is the office's own <c>book_volume</c> (Registry Books groups
        /// records by that column exactly the same way — 2026-09-15). If a book carrying that
        /// value already exists, the record simply belongs there. If none does, this asks
        /// before creating one — a book must never be invented from an unverified OCR read.
        /// A blank Book No./Year (nothing copied off the ledger yet) has nothing to check and
        /// is let through unchanged.
        /// </summary>
        private bool ConfirmRegistryBook()
        {
            string book = V("BookVolume");
            if (string.IsNullOrWhiteSpace(book)) return true;
            if (BookVolumeExists(book)) return true;

            switch (ShowRegistryBookNotFoundDialog(book))
            {
                case BookDecision.Create:
                    return true;
                case BookDecision.Change:
                    dgvFields.Select();
                    foreach (DataGridViewRow gridRow in dgvFields.Rows)
                    {
                        if (gridRow.Tag is DocField f && f.Key == "BookVolume")
                        {
                            dgvFields.CurrentCell = gridRow.Cells[1];
                            break;
                        }
                    }
                    return false;
                default:
                    return false;
            }
        }

        private bool BookVolumeExists(string bookVolume)
        {
            // TableFor(_kind) is one of the three fixed table names off the DocKind enum
            // (never operator-typed text), so building the SQL with it is safe — the same
            // pattern RegistryNumber.Table() already uses.
            string table = TableFor(_kind);
            if (table == null) return true; // unknown kind: nothing to check, fail open
            try
            {
                DataTable dt = Db.Pull("SELECT COUNT(*) c FROM " + table + " WHERE book_volume = @v",
                    new MySqlParameter("@v", bookVolume));
                return dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["c"]) > 0;
            }
            catch
            {
                // A lookup failure must never invent a missing book by itself — fail OPEN
                // to "exists" so a DB hiccup asks nothing rather than risking a duplicate
                // book prompt on top of a real one.
                return true;
            }
        }

        private enum BookDecision { Create, Change, Cancel }

        private BookDecision ShowRegistryBookNotFoundDialog(string book)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Registry Book Not Found";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ShowInTaskbar = false;
                dlg.ClientSize = new Size(440, 300);
                dlg.BackColor = UiTheme.Surface;

                var lblTitle = new Label
                {
                    Text = "Registry Book Not Found",
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = UiTheme.Ink,
                    AutoSize = true,
                    Location = new Point(24, 20)
                };
                var lblBody = new Label
                {
                    Text = "Book No. / Year: " + book + "\n\n" +
                           "No matching registry book exists.",
                    Font = new Font("Segoe UI", 9.5F),
                    ForeColor = UiTheme.Muted,
                    AutoSize = true,
                    Location = new Point(24, 60),
                    MaximumSize = new Size(392, 0)
                };

                var btnCreate = new Button
                {
                    Text = "Create Registry Book && Save Record",
                    Size = new Size(392, 40),
                    Location = new Point(24, 150),
                    DialogResult = DialogResult.OK
                };
                var btnChange = new Button
                {
                    Text = "Change Book Information",
                    Size = new Size(392, 36),
                    Location = new Point(24, 198),
                    DialogResult = DialogResult.Retry
                };
                var btnCancel = new Button
                {
                    Text = "Cancel",
                    Size = new Size(392, 32),
                    Location = new Point(24, 242),
                    DialogResult = DialogResult.Cancel
                };

                dlg.Controls.AddRange(new Control[] { lblTitle, lblBody, btnCreate, btnChange, btnCancel });
                dlg.CancelButton = btnCancel;
                UiTheme.Polish(dlg);

                DialogResult r = dlg.ShowDialog(this);
                return r == DialogResult.OK ? BookDecision.Create
                     : r == DialogResult.Retry ? BookDecision.Change
                     : BookDecision.Cancel;
            }
        }

        private bool ReadyToSave()
        {
            if (_result == null || _kind == DocKind.Unknown)
            {
                MessageBox.Show(
                    "The document type has not been identified — run OCR first, and save only a scan " +
                    "recognised as a Certificate of Live Birth or a Certificate of Death.",
                    "Not classified", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            // A Marriage document only commits straight to the backlog in legacy digitization
            // mode (Civil Registry Record -> "+ Digitize Old Record"); outside that mode it
            // only ever Auto-Fills into the live Form 97 dialog.
            bool marriageAllowed = _legacyBacklogMode && _kind == DocKind.Marriage;
            if (_kind != DocKind.Birth && _kind != DocKind.Death && !marriageAllowed)
            {
                MessageBox.Show(
                    "Only a birth, marriage or death certificate is written straight to the database " +
                    "from this screen, and a marriage certificate only from \"+ Digitize Old Record\" on " +
                    "the Marriage Record screen. Use \"Auto-Fill Marriage\" here for a live Form 97 instead.",
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
            // Opened from the Form 97 wizard: fill THAT form, not a new one. It is a dialog
            // over the Marriage Registration, so there is no shell to route through.
            if (_returnTo != null)
            {
                if (kind != DocKind.Marriage)
                {
                    MessageBox.Show(this,
                        "This photo was read as a " + ModuleName(kind) + " document, not a Certificate of Marriage.\n\n" +
                        "Close this window and capture the Certificate of Marriage again.",
                        "Not a marriage certificate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                if (!string.IsNullOrEmpty(_scanId)) vals["OcrScanId"] = _scanId;
                _returnTo.PrimeFromExtraction(vals);
                _returnTo.SetScanImage(_scanBytes);
                if (_licenseBytes != null) _returnTo.SetLicenseImage(_licenseBytes);
                _returnTo.SetOcrContext(_scanId, _result);
                return true;
            }

            // Opened from the Birth Record Digitization wizard: fill THAT wizard's fields,
            // not the live daily registration screen, and keep the scanned original with it.
            if (_returnToBirth != null)
            {
                if (kind != DocKind.Birth)
                {
                    MessageBox.Show(this,
                        "This photo was read as a " + ModuleName(kind) + " document, not a Certificate of Live Birth.\n\n" +
                        "Close this window and scan the birth record again.",
                        "Not a birth certificate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                if (!string.IsNullOrEmpty(_scanId)) vals["OcrScanId"] = _scanId;
                _returnToBirth.PrimeFromExtraction(vals);
                _returnToBirth.SetScanImage(_scanBytes);
                return true;
            }

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
                    if (_licenseBytes != null) dlg.SetLicenseImage(_licenseBytes);
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
                    // source_image is KEPT here, re-analyze after re-analyze, so a pending
                    // upload can be reopened (Upload ID click) any number of times before it
                    // is actually committed to a record — clearing it is MarkBatch's job now,
                    // once the scan has a real row to be reopened FROM instead
                    // (births/deaths/marriages all keep their own scan_image copy).
                    Db.Push(
                        "UPDATE ocr_batch SET source_book=@book, doc_class=@class, doc_kind=@kind, " +
                        "form_code=@fcode, form_name=@fname, confidence=@conf, " +
                        "overall_confidence=@oconf, needs_review=@need, review_reason=@reason, " +
                        "rotation_applied=@rot, username=@user, status=@status, raw_text=@raw " +
                        "WHERE scan_id=@id",
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

        /// <summary>
        /// Record what became of this scan: its status and the registry row it produced.
        /// Only once a real record id lands (Committed/Draft, not the record_id-less
        /// Auto-Filled/Manual-Review states) does source_image get cleared here — from
        /// that point the record itself carries the softcopy (births/deaths/marriages all
        /// keep their own scan_image), so keeping a second copy on ocr_batch is dead weight.
        /// </summary>
        private void MarkBatch(string status, string table, long? recordId)
        {
            if (_scanId == null) return;
            try
            {
                Db.Push(
                    "UPDATE ocr_batch SET status = @status, record_table = @table, record_id = @rid" +
                    (recordId.HasValue ? ", source_image = NULL" : "") +
                    " WHERE scan_id = @id",
                    new MySqlParameter("@status", status),
                    new MySqlParameter("@table", (object)table ?? DBNull.Value),
                    new MySqlParameter("@rid", recordId.HasValue ? (object)recordId.Value : DBNull.Value),
                    new MySqlParameter("@id", _scanId));
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                // Migration 54 (source_image) not applied — the status/table/id update still
                // matters, just without the column that doesn't exist yet.
                Db.Push(
                    "UPDATE ocr_batch SET status = @status, record_table = @table, record_id = @rid " +
                    "WHERE scan_id = @id",
                    new MySqlParameter("@status", status),
                    new MySqlParameter("@table", (object)table ?? DBNull.Value),
                    new MySqlParameter("@rid", recordId.HasValue ? (object)recordId.Value : DBNull.Value),
                    new MySqlParameter("@id", _scanId));
            }
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

            object weight = int.TryParse(V("Weight"), out int g) ? (object)g : DBNull.Value;
            string religion = V("Religion");
            string citizenship = V("Nationality");

            // FORM IDENTITY on the record. Without it a certificate cannot be reprinted
            // in the layout it came off — `births` is shared by every MF-102 revision, so
            // the revision is not recoverable from the row itself.
            string formCode = _formDef?.FormCode;
            string formName = _formDef?.FormName;

            return Db.Insert(
                "INSERT INTO births (form_code, form_name, registry_no, book_volume, book_page, " +
                "date_registered, status, first_name, middle_name, " +
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
                "birth_image, scan_image, record_source, " +
                "digitized_by, date_digitized, encoding_method, source_reference) " +
                "VALUES (@fcode, @fname, @reg, @book, @page, @dateReg, @status, @fn, @mn, @ln, @sex, @dob, @tob, @place, @btype, @weight, " +
                "@mf, @mm, @ml, @mocc, @mrel, @mcit, " +
                "@ff, @fm, @fl, @focc, @frel, @fcit, " +
                "@atype, @aname, @atitle, @aaddr, @adate, " +
                "@informant, @irel, @iaddr, @idate, " +
                "@prep, @preptitle, @prepdate, " +
                "@recv, @recvtitle, @recvdate, " +
                "@regby, @regbytitle, @regbydate, " +
                "@img, @scan, 'OCR-Backlog', " +
                "@digby, @digdate, @encmethod, @srcref)",
                new MySqlParameter("@fcode", NullIfEmpty(formCode)),
                new MySqlParameter("@fname", NullIfEmpty(formName)),
                new MySqlParameter("@reg", NullIfEmpty(V("RegistryNo"))),
                // STEP 7 — Registry Information. All three come only from what the operator
                // copied off the physical ledger onto the grid (EnsureRegistryInfoFields
                // seeds them blank; nothing here derives or guesses a book, page or
                // registration date on the record's behalf).
                new MySqlParameter("@book", NullIfEmpty(V("BookVolume"))),
                new MySqlParameter("@page", NullIfEmpty(V("BookPage"))),
                new MySqlParameter("@dateReg", DateOrNull("DateOfRegistration")),
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
                    { Value = _scanBytes == null ? (object)DBNull.Value : _scanBytes },
                // STEP 10 — digitization metadata: who committed it, when, how (this path
                // always goes through OCR + operator review before Commit is reachable), and
                // where the image came from.
                new MySqlParameter("@digby", DigitizedBy()),
                new MySqlParameter("@digdate", DateTime.Now),
                new MySqlParameter("@encmethod", "OCR + Manual Verification"),
                new MySqlParameter("@srcref", NullIfEmpty(_sourceLabel)));
        }

        /// <summary>Step 10 — who to credit as having digitized/committed this record.</summary>
        private static object DigitizedBy()
        {
            var u = Session.User;
            if (u == null) return DBNull.Value;
            string name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username;
            return NullIfEmpty(name);
        }

        /// <summary>
        /// Writes an old, already-registered death certificate straight into <c>deaths</c>,
        /// tagged <c>record_source = 'OCR-Backlog'</c> — the same direct-to-database path
        /// <see cref="SaveBirth"/> uses, and it never opens the live Death Registration
        /// screen. Reads the grid by the same canonical keys <c>FormCatalog</c>'s MF-103 map
        /// already uses for printing (RegistryNo/DeceasedFirst.../PlaceOfDeath/...), so the
        /// extraction, the print map and this insert cannot disagree about which field is
        /// which.
        /// </summary>
        private long SaveDeath(string status)
        {
            string dodText = V("DateOfDeath");
            bool parsed = DateTime.TryParse(dodText, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime d);
            object dod = parsed ? (object)d.Date : DBNull.Value;

            string sexText = V("Sex");
            object sex = sexText.StartsWith("F", StringComparison.OrdinalIgnoreCase) ? "Female"
                       : sexText.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "Male"
                       : (object)DBNull.Value;

            object age = int.TryParse(V("Age"), out int a) ? (object)a : DBNull.Value;

            // `deaths` keeps one joined full_name column (BR-2026-09-19), not separate
            // first/middle/last columns — join here rather than write dead columns.
            string first = V("DeceasedFirst"), middle = V("DeceasedMiddle"), last = V("DeceasedLast");
            string fullName = V("FullName");
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = string.Join(" ", new[] { first, middle, last }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

            string formCode = _formDef?.FormCode;
            string formName = _formDef?.FormName;

            return Db.Insert(
                "INSERT INTO deaths (form_code, form_name, registry_no, book_volume, book_page, status, " +
                "full_name, sex, civil_status, age, citizenship, date_of_death, place_of_death, " +
                "religion_name, immediate_cause, disposal_method, place_of_disposal, " +
                "informant_name, informant_relationship, informant_address, informant_date, " +
                "prepared_by, prepared_by_title, prepared_by_date, " +
                "received_by, received_by_title, received_by_date, " +
                "registered_by, registered_by_title, registered_by_date, " +
                "death_image, scan_image, record_source, " +
                "digitized_by, date_digitized, encoding_method, source_reference) " +
                "VALUES (@fcode, @fname, @reg, @book, @page, @status, " +
                "@name, @sex, @civil, @age, @cit, @dod, @place, " +
                "@religion, @imm, @disp, @dplace, " +
                "@informant, @irel, @iaddr, @idate, " +
                "@prep, @preptitle, @prepdate, " +
                "@recv, @recvtitle, @recvdate, " +
                "@regby, @regbytitle, @regbydate, " +
                "@img, @scan, 'OCR-Backlog', " +
                "@digby, @digdate, @encmethod, @srcref)",
                new MySqlParameter("@fcode", NullIfEmpty(formCode)),
                new MySqlParameter("@fname", NullIfEmpty(formName)),
                new MySqlParameter("@reg", NullIfEmpty(V("RegistryNo"))),
                // STEP 7/9 — Registry Information, from what the operator copied off the
                // physical ledger (EnsureRegistryInfoFields seeds both blank) — never derived
                // from the date of death. A guessed archive location is the same fabrication
                // this project refused for Birth on 2026-09-28.
                new MySqlParameter("@book", NullIfEmpty(V("BookVolume"))),
                new MySqlParameter("@page", NullIfEmpty(V("BookPage"))),
                new MySqlParameter("@status", status),
                new MySqlParameter("@name", NullIfEmpty(fullName)),
                new MySqlParameter("@sex", sex),
                new MySqlParameter("@civil", NullIfEmpty(V("CivilStatus"))),
                new MySqlParameter("@age", age),
                new MySqlParameter("@cit", NullIfEmpty(V("Citizenship"))),
                new MySqlParameter("@dod", dod),
                new MySqlParameter("@place", NullIfEmpty(V("PlaceOfDeath"))),
                new MySqlParameter("@religion", NullIfEmpty(V("Religion"))),
                new MySqlParameter("@imm", NullIfEmpty(V("CauseOfDeath"))),
                new MySqlParameter("@disp", NullIfEmpty(V("CorpseDisposal"))),
                new MySqlParameter("@dplace", NullIfEmpty(V("PlaceOfDisposal"))),
                new MySqlParameter("@informant", NullIfEmpty(V("Informant"))),
                new MySqlParameter("@irel", NullIfEmpty(V("InformantRelationship"))),
                new MySqlParameter("@iaddr", NullIfEmpty(V("InformantAddress"))),
                new MySqlParameter("@idate", DateOrNull("InformantDate")),
                new MySqlParameter("@prep", NullIfEmpty(V("PreparedByName"))),
                new MySqlParameter("@preptitle", NullIfEmpty(V("PreparedByTitle"))),
                new MySqlParameter("@prepdate", DateOrNull("PreparedByDate")),
                new MySqlParameter("@recv", NullIfEmpty(V("ReceivedByName"))),
                new MySqlParameter("@recvtitle", NullIfEmpty(V("ReceivedByTitle"))),
                new MySqlParameter("@recvdate", DateOrNull("ReceivedByDate")),
                new MySqlParameter("@regby", NullIfEmpty(V("RegisteredByName"))),
                new MySqlParameter("@regbytitle", NullIfEmpty(V("RegisteredByTitle"))),
                new MySqlParameter("@regbydate", DateOrNull("RegisteredByDate")),
                new MySqlParameter("@img", MySqlDbType.LongBlob)
                    { Value = _scanBytes == null ? (object)DBNull.Value : _scanBytes },
                new MySqlParameter("@scan", MySqlDbType.LongBlob)
                    { Value = _scanBytes == null ? (object)DBNull.Value : _scanBytes },
                new MySqlParameter("@digby", DigitizedBy()),
                new MySqlParameter("@digdate", DateTime.Now),
                new MySqlParameter("@encmethod", "OCR + Manual Verification"),
                new MySqlParameter("@srcref", NullIfEmpty(_sourceLabel)));
        }

        /// <summary>
        /// Writes an old, already-registered marriage certificate straight into
        /// <c>marriages</c>, tagged <c>record_source = 'OCR-Backlog'</c> — reachable only from
        /// legacy digitization mode (Civil Registry Record -&gt; "+ Digitize Old Record"),
        /// never from the normal Auto-Fill path that fills the live Form 97. Writes the same
        /// simplified free-text columns the Marriage Record backlog screen itself manages
        /// (husband/wife name cells, civil status, place of birth) rather than the live
        /// screen's FK-based citizenship/church lookups — a decades-old ledger entry naming a
        /// church or nationality that was never entered into those lookup tables should not
        /// force a lossy best-fit match, the same reasoning `births.place_of_birth` already
        /// follows as free text.
        /// </summary>
        private long SaveMarriageLegacy(string status)
        {
            string dateText = V("DateOfMarriage");
            bool parsed = DateTime.TryParse(dateText, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime d);
            object mdate = parsed ? (object)d.Date : DBNull.Value;

            string formCode = _formDef?.FormCode;
            string formName = _formDef?.FormName;

            return Db.Insert(
                "INSERT INTO marriages (form_code, form_name, registry_no, book_volume, book_page, status, " +
                "husband_first_name, husband_middle_name, husband_last_name, husband_sex, " +
                "wife_first_name, wife_middle_name, wife_last_name, wife_sex, " +
                "date_of_marriage, place_of_marriage, solemnizer, " +
                "scan_image, record_source, digitized_by, date_digitized, encoding_method, source_reference) " +
                "VALUES (@fcode, @fname, @reg, @book, @page, @status, " +
                "@hfn, @hmn, @hln, 'Male', " +
                "@wfn, @wmn, @wln, 'Female', " +
                "@mdate, @mplace, @solemnizer, " +
                "@scan, 'OCR-Backlog', @digby, @digdate, @encmethod, @srcref)",
                new MySqlParameter("@fcode", NullIfEmpty(formCode)),
                new MySqlParameter("@fname", NullIfEmpty(formName)),
                new MySqlParameter("@reg", NullIfEmpty(V("RegistryNo"))),
                // STEP 7/9 — Registry Information, from what the operator copied off the
                // physical ledger (EnsureRegistryInfoFields seeds both blank, in legacy mode).
                new MySqlParameter("@book", NullIfEmpty(V("BookVolume"))),
                new MySqlParameter("@page", NullIfEmpty(V("BookPage"))),
                new MySqlParameter("@status", status),
                new MySqlParameter("@hfn", V("HusbandFirst")),
                new MySqlParameter("@hmn", NullIfEmpty(V("HusbandMiddle"))),
                new MySqlParameter("@hln", V("HusbandLast")),
                new MySqlParameter("@wfn", V("WifeFirst")),
                new MySqlParameter("@wmn", NullIfEmpty(V("WifeMiddle"))),
                new MySqlParameter("@wln", V("WifeLast")),
                new MySqlParameter("@mdate", mdate),
                new MySqlParameter("@mplace", NullIfEmpty(V("PlaceOfMarriage"))),
                new MySqlParameter("@solemnizer", NullIfEmpty(V("Solemnizer"))),
                new MySqlParameter("@scan", MySqlDbType.LongBlob)
                    { Value = _scanBytes == null ? (object)DBNull.Value : _scanBytes },
                new MySqlParameter("@digby", DigitizedBy()),
                new MySqlParameter("@digdate", DateTime.Now),
                new MySqlParameter("@encmethod", "OCR + Manual Verification"),
                new MySqlParameter("@srcref", NullIfEmpty(_sourceLabel)));
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
            lblBatch.Text = "PENDING OCR — " + dgvBatch.Rows.Count + " AWAITING REVIEW (click Upload ID to open)";
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
            lblBatch.Text = "PROCESSED — " + dgvBatch.Rows.Count + " (click Reg. No. to view)";
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
        /// Single-click the link column (Upload ID on the Pending list, Reg. No. on
        /// Processed) opens the same document a double-click would — the record name is the
        /// obvious thing to click, so it shouldn't take two clicks to know that.
        /// </summary>
        private void DgvBatch_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string colName = dgvBatch.Columns[e.ColumnIndex].Name;
            if (colName != "Upload ID" && colName != "Reg. No." && colName != "Scan ID") return;
            OpenBatchRow(e.RowIndex);
        }

        /// <summary>
        /// Double-click a row in either grid mode. Always re-reads the FULL row by its
        /// hidden id rather than trusting whatever columns happen to be bound on screen, so
        /// the same handler works for the Pending and the Processed layouts without either
        /// one needing to carry columns the other does not.
        /// </summary>
        private void DgvBatch_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            OpenBatchRow(e.RowIndex);
        }

        private async void OpenBatchRow(int rowIndex)
        {
            if (rowIndex < 0) return;
            if (!dgvBatch.Columns.Contains("_Id")) return;
            object idVal = dgvBatch.Rows[rowIndex].Cells["_Id"].Value;
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

            var img = Db.Pull(
                "SELECT source_image FROM ocr_batch WHERE id = @id",
                new MySqlParameter("@id", batchId));
            byte[] bytes = img.Rows.Count > 0 ? img.Rows[0]["source_image"] as byte[] : null;
            if (bytes == null || bytes.Length == 0)
            {
                // The stored bytes are gone — either this row was already opened once this
                // session (LogBatch clears source_image the moment a scan is actually read,
                // per migration 54's own intent) or it never had one. Either way there is
                // nothing this screen can pull back; the scan is either still on screen
                // above, or has to be loaded again from its original file.
                MessageBox.Show(
                    "The stored image for this upload is no longer kept here (it was cleared " +
                    "once read). If it is not already showing in the panel above, load it " +
                    "again from its original file.",
                    "No stored image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _sourceLabel = dr["source_book"] as string ?? "Phone scan";
            if (!ShowScanImage(bytes, "SCANNED DOCUMENT — " + _sourceLabel))
                return;

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
            SetupWizard(false);
            ApplyResultToUi();

            await Analyze();
            LoadBatch();
        }

        /// <summary>
        /// Loads a scan's bytes into the SCANNED DOCUMENT frame (decode + display only —
        /// does not touch _scanId/_result, so it's safe to call for a record that's already
        /// been committed and must not be re-analyzed). Returns false (and shows why) when
        /// there is nothing to show.
        /// </summary>
        private bool ShowScanImage(byte[] bytes, string caption)
        {
            if (bytes == null || bytes.Length == 0)
            {
                MessageBox.Show("This scan has no image attached.", "Document",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            Bitmap loaded;
            try { loaded = DocumentAI.LoadImageBytes(bytes); }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open the scan: " + ex.Message, "Document",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            _image?.Dispose();
            _image = loaded;
            _scanBytes = bytes;
            _licenseBytes = null;
            _zoom = 1f;
            pbScan.SizeMode = PictureBoxSizeMode.Zoom;
            pnlScanHost.AutoScrollPosition = Point.Empty;
            pbScan.Location = Point.Empty;
            pbScan.Size = pnlScanHost.ClientSize;
            pbScan.Image = _image;
            grpScan.Text = caption;
            return true;
        }

        /// <summary>
        /// The "view" action for a processed row — what it was, what it became, and the
        /// registry number it carries (or a plain statement that none was read/assigned).
        /// Also pulls the saved softcopy off the record itself (births/deaths/marriages
        /// all keep a scan_image column) and puts it back on screen, so clicking a
        /// processed row shows the document again, not just a text summary.
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

            if (moduleKey != null && recIdObj != DBNull.Value)
            {
                // table is one of the three literals above, never user input — safe to
                // interpolate directly.
                var scan = Db.Pull(
                    "SELECT scan_image FROM " + table + " WHERE id = @id",
                    new MySqlParameter("@id", recIdObj));
                byte[] scanBytes = scan.Rows.Count > 0 ? scan.Rows[0]["scan_image"] as byte[] : null;
                ShowScanImage(scanBytes, "SCANNED DOCUMENT — " + table + " #" + recIdObj +
                    (string.IsNullOrWhiteSpace(reg) ? "" : " (" + reg + ")"));
            }

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
            // Buttons zoom about the middle of the viewer.
            var mid = new Point(pnlScanHost.ClientSize.Width / 2, pnlScanHost.ClientSize.Height / 2);
            ZoomAt(factor, pnlScanHost.PointToScreen(mid));
        }

        /// <summary>
        /// Zoom by <paramref name="factor"/> keeping the spot under <paramref name="screenPt"/>
        /// where it is - what a pinch or Ctrl + wheel should do - instead of growing from the
        /// top-left corner.
        /// </summary>
        private void ZoomAt(float factor, Point screenPt)
        {
            if (_image == null) return;
            Point cp = pnlScanHost.PointToClient(screenPt);
            // Where the point sits in the picture, as a fraction, before resizing.
            float fx = pbScan.Width > 0 ? (cp.X - pbScan.Left) / (float)pbScan.Width : 0.5f;
            float fy = pbScan.Height > 0 ? (cp.Y - pbScan.Top) / (float)pbScan.Height : 0.5f;

            float old = _zoom;
            _zoom = Math.Max(0.25f, Math.Min(6f, _zoom * factor));
            if (Math.Abs(_zoom - old) < 0.0001f) return;

            pbScan.Size = new Size(
                (int)(pnlScanHost.ClientSize.Width * _zoom),
                (int)(pnlScanHost.ClientSize.Height * _zoom));
            pnlScanHost.PerformLayout();
            pnlScanHost.AutoScrollPosition = new Point(
                Math.Max(0, (int)(fx * pbScan.Width - cp.X)),
                Math.Max(0, (int)(fy * pbScan.Height - cp.Y)));
            pbScan.Invalidate();
        }

        /// <summary>Drag the page with the fingers: content moves the way the fingers do.</summary>
        private void PanBy(int dx, int dy)
        {
            if (_image == null) return;
            Point cur = pnlScanHost.AutoScrollPosition;   // negative of the scroll offset
            pnlScanHost.AutoScrollPosition = new Point(
                Math.Max(0, -cur.X - dx), Math.Max(0, -cur.Y - dy));
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
