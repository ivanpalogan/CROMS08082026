using System;
using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// PSA Copy (BREQS) step - shown only when the client picked "PSA Copy (BREQS)". Asks which PSA
    /// certificate, how many copies, the valid ID the client will present, and the details PSA needs
    /// to find the record. The fields follow the certificate: a birth asks for the parents, a
    /// marriage for both spouses, a death for the deceased only.
    /// <para/>
    /// Next returns DialogResult.OK (flow goes on to Personal Info &amp; Photo); Back returns Cancel
    /// (flow re-shows Select Services); an idle timeout returns Abort (flow resets to Welcome).
    /// </summary>
    public partial class BreqsDetailsForm : Form, IMessageFilter
    {
        private readonly KioskSession _session;
        private Timer _idle;
        private Action _resetIdle;
        // Set by every DELIBERATE close so OnFormClosing can tell navigation from a real quit
        // (a programmatic Close() also reports UserClosing - the trap recorded 2026-08-29).
        private bool _navigating;
        private float _scale = 1f;      // cumulative scale factor applied, relative to _designSize
        private Size _designSize;        // _box's designed size, cached before any Scale() ever runs
        private int _appliedShift;       // the wife-block gap currently applied, in design pixels

        public BreqsDetailsForm(KioskSession session)
        {
            _session = session;
            InitializeComponent();
            _designSize = _box.Size;

            _btnBack.BringToFront();
            _btnNext.BringToFront();
            _stepInd.Steps = _session.StepLabels();
            _stepInd.SetStep(1);
            PlaceStars();
            KioskButtons.Style(_btnBack, KioskButtonKind.Secondary, KioskCore.IconArrowLeft, backdrop: footer.BackColor);
            KioskButtons.Style(_btnNext, KioskButtonKind.Primary, KioskCore.IconArrowRight, iconRight: true, backdrop: footer.BackColor);

            for (int i = 1; i <= 10; i++) _cboCopies.Items.Add(i.ToString());
            _cboPurpose.Items.AddRange(KioskCore.BreqsPurposes);
            _cboRelationship.Items.AddRange(KioskCore.BreqsRelationships);
            _cboIdType.Items.AddRange(KioskCore.IdTypes);

            // One certificate per request: picking one clears the others.
            _pillBirth.CheckedChanged += (s, e) => { if (_pillBirth.Checked) { _pillMarriage.SetChecked(false); _pillDeath.SetChecked(false); } ApplyDocType(); };
            _pillMarriage.CheckedChanged += (s, e) => { if (_pillMarriage.Checked) { _pillBirth.SetChecked(false); _pillDeath.SetChecked(false); } ApplyDocType(); };
            _pillDeath.CheckedChanged += (s, e) => { if (_pillDeath.Checked) { _pillBirth.SetChecked(false); _pillMarriage.SetChecked(false); } ApplyDocType(); };

            Load += (s, e) => { LoadFromSession(); ApplyDocType(); EnsureClaimQr(); };
            Shown += (s, e) => { FitToScreen(); CenterBox(); };
            panelStep.Resize += (s, e) => { FitToScreen(); CenterBox(); };

            _idle = new Timer { Interval = 1000 };
            int ticks = 0;
            _idle.Tick += (s, e) =>
            {
                if (++ticks < KioskCore.IdleSeconds) return;
                ticks = 0;
                _session.Reset();
                _navigating = true;
                DialogResult = DialogResult.Abort;
                Close();
            };
            _resetIdle = () => ticks = 0;
            _idle.Start();
            Application.AddMessageFilter(this);
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x0200 || m.Msg == 0x0201 || m.Msg == 0x0100 || m.Msg == 0x020A) _resetIdle?.Invoke();
            return false;
        }

        /// <summary>
        /// Each red * sits right after its caption, placed from the caption's MEASURED width.
        /// Estimating it from the character count put the star on top of the text ("Las*",
        /// "ID num*er") - caught on the render.
        /// </summary>
        private void PlaceStars()
        {
            foreach (var pair in new[] {
                Tuple.Create(lblDocType, lblDocTypeReq), Tuple.Create(lblIdType, lblIdTypeReq), Tuple.Create(lblIdNo, lblIdNoReq),
                Tuple.Create(lblOwnerFirst, lblOwnerFirstReq), Tuple.Create(lblOwnerLast, lblOwnerLastReq),
                Tuple.Create(lblSpouseFirst, lblSpouseFirstReq), Tuple.Create(lblSpouseLast, lblSpouseLastReq) })
            {
                pair.Item2.Left = pair.Item1.Left + pair.Item1.PreferredWidth - 2;
                pair.Item2.Top = pair.Item1.Top;
            }
        }

        private string DocType
        {
            get { return _pillBirth.Checked ? "Birth" : _pillMarriage.Checked ? "Marriage" : _pillDeath.Checked ? "Death" : null; }
        }

        // ------------------------------------------------ session <-> fields
        private void LoadFromSession()
        {
            _pillBirth.SetChecked(_session.BreqsDocType == "Birth");
            _pillMarriage.SetChecked(_session.BreqsDocType == "Marriage");
            _pillDeath.SetChecked(_session.BreqsDocType == "Death");
            _cboCopies.SelectedItem = Math.Max(1, Math.Min(10, _session.BreqsCopies)).ToString();
            _cboPurpose.Text = _session.BreqsPurpose ?? "";
            _cboRelationship.Text = _session.BreqsRelationship ?? "";
            _cboIdType.Text = _session.IdType ?? "";
            _txtIdNo.Text = _session.IdNo ?? "";
            _txtOwnerFirst.Text = _session.OwnerFirst ?? ""; _txtOwnerMiddle.Text = _session.OwnerMiddle ?? ""; _txtOwnerLast.Text = _session.OwnerLast ?? "";
            _txtSpouseFirst.Text = _session.SpouseFirst ?? ""; _txtSpouseMiddle.Text = _session.SpouseMiddle ?? ""; _txtSpouseLast.Text = _session.SpouseLast ?? "";
            if (_session.EventDate.HasValue) { _dtpEvent.Value = _session.EventDate.Value; _dtpEvent.Checked = true; }
            else _dtpEvent.Checked = false;
            _txtCity.Text = _session.EventCity ?? ""; _txtProvince.Text = _session.EventProvince ?? "";
            _txtFather.Text = _session.FatherName ?? ""; _txtMother.Text = _session.MotherMaidenName ?? "";
        }

        private void SaveToSession()
        {
            _session.BreqsDocType = DocType;
            int copies;
            _session.BreqsCopies = int.TryParse(_cboCopies.SelectedItem as string, out copies) ? copies : 1;
            _session.BreqsPurpose = Blank(_cboPurpose.Text);
            _session.BreqsRelationship = Blank(_cboRelationship.Text);
            _session.IdType = Blank(_cboIdType.Text);   // shared with Personal Info - picked once
            _session.IdNo = Blank(_txtIdNo.Text);
            _session.OwnerFirst = Blank(_txtOwnerFirst.Text); _session.OwnerMiddle = Blank(_txtOwnerMiddle.Text); _session.OwnerLast = Blank(_txtOwnerLast.Text);
            bool marriage = DocType == "Marriage", birth = DocType == "Birth";
            _session.SpouseFirst = marriage ? Blank(_txtSpouseFirst.Text) : null;
            _session.SpouseMiddle = marriage ? Blank(_txtSpouseMiddle.Text) : null;
            _session.SpouseLast = marriage ? Blank(_txtSpouseLast.Text) : null;
            // Unticked date = "I don't know it", never today.
            _session.EventDate = _dtpEvent.Checked ? _dtpEvent.Value.Date : (DateTime?)null;
            _session.EventCity = Blank(_txtCity.Text); _session.EventProvince = Blank(_txtProvince.Text);
            _session.FatherName = birth ? Blank(_txtFather.Text) : null;
            _session.MotherMaidenName = birth ? Blank(_txtMother.Text) : null;
        }

        private static string Blank(string s) { return string.IsNullOrWhiteSpace(s) ? null : s.Trim(); }

        /// <summary>
        /// Same claimapp QR as Personal Info & Photo (DetailsPhotoForm.EnsureClaimQr) — the ID
        /// number field on THIS step is still typed and required (KioskCore.BreqsProblem), but
        /// a client who would rather not type it out can scan here to upload a photo of it
        /// instead of only typing the number. EnsureClaimRequest is idempotent (a token already
        /// created here is reused, not duplicated, when Personal Info later calls it too).
        /// </summary>
        private void EnsureClaimQr()
        {
            KioskCore.EnsureClaimRequest(_session);
            if (_session.ClaimQrToken == null)
            {
                picBreqsQr.Image = null;
                lblQrTicketNo.Text = "";
                lblQrNote.Text = "QR unavailable — the staff will assist you at the window.";
                return;
            }
            try
            {
                var old = picBreqsQr.Image;
                picBreqsQr.Image = QrHelper.TryCreate(ClaimLink.Build(_session.ClaimQrToken), 6);
                old?.Dispose();
            }
            catch { /* QR lib missing → text only */ }

            lblQrTicketNo.Text = _session.ClaimQrNo ?? "";
            lblQrNote.Text = "Scan with your phone camera to upload a photo of the ID you will present.\n" +
                "No phone camera? On your phone open " + ClaimLink.BaseUrl() +
                " and enter the ticket number above.";
        }

        // ------------------------------------------------ the fields follow the certificate
        private void ApplyDocType()
        {
            string t = DocType;
            bool marriage = t == "Marriage", birth = t == "Birth";
            _lblOwnerHead.Text = marriage ? "Husband" : t == "Death" ? "Name of the person who died" : "Name on the birth certificate";

            Control[] spouse = { _lblSpouseHead, lblSpouseFirst, lblSpouseFirstReq, hostSpouseFirst, lblSpouseMiddle, hostSpouseMiddle, lblSpouseLast, lblSpouseLastReq, hostSpouseLast };
            foreach (Control c in spouse) c.Visible = marriage;
            Control[] parents = { lblFather, hostFather, lblMother, hostMother };
            foreach (Control c in parents) c.Visible = birth;

            // Close the gap the hidden wife block leaves, so the date sits right under the names.
            // Moved by a DELTA scaled with the box: absolute positions would be wrong once
            // FitToScreen has rescaled the whole box.
            int shift = marriage ? 0 : -116;
            int delta = (int)Math.Round((shift - _appliedShift) * _scale);
            if (delta != 0)
                foreach (Control c in new Control[] { _lblDateHead, hostDate, lblCity, hostCity, lblProvince, hostProvince, lblFather, hostFather, lblMother, hostMother })
                    c.Top += delta;
            _appliedShift = shift;
            _lblDateHead.Text = "Date of " + (t == null ? "birth / marriage / death" : t.ToLowerInvariant()) + "   (tick the box if you know it)";
            _btnNext.Enabled = t != null;
        }

        // ------------------------------------------------ navigation
        private void BtnNext_Click(object sender, EventArgs e)
        {
            SaveToSession();
            string problem = FirstProblem();
            if (problem != null)
            {
                MessageBox.Show(problem, "Please check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _navigating = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// The same check KioskCore.Submit makes, minus the requester's own name (asked on the next
        /// step). Checked here too so the client hears about a missing field on the screen that has it.
        /// </summary>
        private string FirstProblem()
        {
            string first = _session.First, last = _session.Last;
            _session.First = _session.First ?? "x"; _session.Last = _session.Last ?? "x";
            try { return KioskCore.BreqsProblem(_session); }
            finally { _session.First = first; _session.Last = last; }
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            SaveToSession();
            _navigating = true;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Application.RemoveMessageFilter(this);
            _idle?.Stop();
            if (!_navigating && e.CloseReason == CloseReason.UserClosing)
                Environment.Exit(0);
            base.OnFormClosing(e);
        }

        // ------------------------------------------------ layout (same approach as Personal Info)
        private void PanelStep_Resize(object sender, EventArgs e) => CenterBox();

        private void CenterBox()
        {
            if (_box == null) return;
            _box.Left = Math.Max(0, (panelStep.ClientSize.Width - _box.Width) / 2);
            _box.Top = Math.Max(16, (panelStep.ClientSize.Height - _box.Height) / 2);
        }

        // Recomputed on EVERY resize, ratio-ed against the fixed _designSize (never against
        // _box's current, possibly already-scaled size) — a one-shot fit baked in whatever
        // ClientSize the form reported on its FIRST Shown/Resize, which can be a transient
        // size before WindowState=Maximized settles, permanently mis-scaling the box (same
        // bug found and fixed on the Personal Info & Photo step, 2026-09-13).
        private void FitToScreen()
        {
            if (_box == null) return;
            int hw = panelStep.ClientSize.Width, hh = panelStep.ClientSize.Height;
            if (hw < 100 || hh < 100) return;
            const float MaxGrow = 1.6f;
            float f = Math.Min(MaxGrow, Math.Min((hw - 24) / (float)_designSize.Width, (hh - 24) / (float)_designSize.Height));
            if (Math.Abs(f - _scale) < 0.01f) return;

            float delta = f / _scale;
            _box.Scale(new SizeF(delta, delta));
            // Control.Scale moves and resizes but does NOT touch fonts (AutoScaleMode.None), so on a
            // short screen the boxes shrank under full-size text and captions ran into each other -
            // measured on a 1366x768 render. Fonts follow the box when it shrinks.
            if (f < 1f) FontScaler.Scale(_box, delta);
            _scale = f;
            PlaceStars();   // captions changed width
        }

        private void Field_Enter(object sender, EventArgs e)
        {
            if (((Control)sender).Parent is RoundPanel h) { h.BorderColor = KioskCore.Accent; h.BorderWidth = 2f; h.Invalidate(); }
        }

        private void Field_Leave(object sender, EventArgs e)
        {
            if (((Control)sender).Parent is RoundPanel h) { h.BorderColor = KioskCore.Line; h.BorderWidth = 1.5f; h.Invalidate(); }
        }
    }
}
