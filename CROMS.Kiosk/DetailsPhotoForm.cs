using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Step 2 of 2 — details + priority + live webcam photo. For Marriage Application / Marriage Registration the personal
    /// info and photo are for TWO people (husband and wife) — see
    /// <see cref="ApplyMarriagePersons"/>; every other service is a single person, unchanged.
    /// Reads/writes the shared <see cref="KioskSession"/>. Print submits via
    /// <see cref="KioskCore.Submit"/> and returns DialogResult.OK (flow resets for the next
    /// client); Back returns DialogResult.Cancel (flow re-shows Step 1 with selections intact).
    /// </summary>
    public partial class DetailsPhotoForm : Form, IMessageFilter
    {
        private readonly KioskSession _session;
        private Timer _idle;
        private Timer _availability;
        private Action _resetIdle;
        // Set by every DELIBERATE close (Next / Back / idle / submit) so OnFormClosing can tell
        // our own navigation apart from the operator really quitting the kiosk. CloseReason is
        // NOT usable for this: a programmatic Close() also reports UserClosing, and WinForms
        // auto-assigns DialogResult.Cancel when the X is clicked, so neither one discriminates.
        private bool _navigating;


        private VideoCaptureDevice _camera;
        private Bitmap _lastFrame;
        private readonly object _frameLock = new object();

        // Marriage services only: one camera, two people — which one Capture writes to next.
        private bool _capturingWife;

        public DetailsPhotoForm(KioskSession session)
        {
            _session = session;
            InitializeComponent();
            _designSize = _detailsBox.Size;   // cache BEFORE any Scale() call, ever

            _btnBack.BringToFront();
            _btnPrint.BringToFront();
            _offlineOverlay.Bounds = ClientRectangle;
            _offlineOverlay.BringToFront();
            _stepInd.Steps = _session.StepLabels();
            _stepInd.SetStep(_session.DetailsStepIndex());
            _stepInd.FitToContent(Math.Max(300, header.ClientSize.Width - _stepInd.Left * 2));

            // One shared button treatment across both kiosk steps (see KioskButtons).
            KioskButtons.Style(_btnBack, KioskButtonKind.Secondary, KioskCore.IconArrowLeft,
                backdrop: footer.BackColor);
            KioskButtons.Style(_btnPrint, KioskButtonKind.Success, KioskCore.IconPrinter,
                backdrop: footer.BackColor);
            KioskButtons.Style(_btnCapture, KioskButtonKind.Primary, KioskCore.IconCamera);
            KioskButtons.Style(_btnPersonA, KioskButtonKind.Primary);
            KioskButtons.Style(_btnPersonB, KioskButtonKind.Secondary);
            _cboIdType.Items.AddRange(KioskCore.IdTypes);
            OthersBox.AttachInline(_cboIdType, 60);
            AutoCaps.Attach(_txtFirst, _txtMiddle, _txtLast, _txtFirst2, _txtMiddle2, _txtLast2);
            // The camera-state line draws its own status dot (no emoji).
            AttachStateDot(_lblCamState);

            // Senior Citizen and Pregnant are mutually exclusive: "senior" is 60+ under RA 9994,
            // which is past menopause, so that pairing cannot be a real client and would only
            // pollute the priority statistics. PWD stays independent — it validly combines with
            // either. Safe to enforce because all three grant the SAME priority lane, so
            // clearing one never costs the client their priority.
            _priSenior.CheckedChanged += (s, e) =>
            { if (_priSenior.Checked) _priPregnant.SetChecked(false); };
            _priPregnant.CheckedChanged += (s, e) =>
            { if (_priPregnant.Checked) _priSenior.SetChecked(false); };

            Load += (s, e) =>
            {
                LoadFromSession();
                ApplyIdentityStep(_session.HasClaim);
                // Everything above (LoadFromSession -> ApplyMarriagePersons,
                // ApplyIdentityStep -> LayoutPhotoStep) just wrote the per-session
                // baseline layout in raw, design-scale coordinates. Only NOW is it safe to
                // let FitToScreen scale the tree — see _baselineReady.
                _baselineReady = true;
                StartCamera();
                UpdateAvailability();
                Cue(_txtContact, "09XX XXX XXXX");
                Cue(_txtClaimTicket, "Example: Q-006");
            };
            Shown += (s, e) => { FitToScreen(); CenterDetailsStep(panelStep2); };
            panelStep2.Resize += (s, e) => { FitToScreen(); CenterDetailsStep(panelStep2); };

            _availability = new Timer { Interval = 4000 };
            _availability.Tick += (s, e) => UpdateAvailability();
            _availability.Start();

            // Idle: if the client walked away, reset and hand control back to a fresh Step 1.
            _idle = new Timer { Interval = 1000 };
            int idleTicks = 0;
            _idle.Tick += (s, e) =>
            {
                idleTicks++;
                if (idleTicks >= KioskCore.IdleSeconds)
                {
                    idleTicks = 0;
                    _session.Reset();
                    _navigating = true;
                    DialogResult = DialogResult.OK;   // OK => flow resets for the next client
                    Close();
                }
            };
            _resetIdle = () => idleTicks = 0;
            _idle.Start();
            Application.AddMessageFilter(this);
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x0200 || m.Msg == 0x0201 || m.Msg == 0x0100 || m.Msg == 0x020A)
                _resetIdle?.Invoke();
            return false;
        }

        // ------------------------------------------------ session <-> fields
        private void LoadFromSession()
        {
            _txtFirst.Text = _session.First ?? "";
            _txtMiddle.Text = _session.Middle ?? "";
            _txtLast.Text = _session.Last ?? "";
            _txtContact.Text = _session.Contact ?? "";
            _priSenior.SetChecked(_session.Senior);
            _priPwd.SetChecked(_session.Pwd);
            _priPregnant.SetChecked(_session.Pregnant);
            _txtClaimTicket.Text = _session.ClaimTicketEntry ?? "";
            OthersBox.SetValue(_cboIdType, _session.IdType);
            _txtIdNo.Text = _session.IdNo ?? "";

            if (_session.HasMarriage)
            {
                _txtFirst2.Text = _session.First2 ?? "";
                _txtMiddle2.Text = _session.Middle2 ?? "";
                _txtLast2.Text = _session.Last2 ?? "";
            }
            ApplyMarriagePersons(_session.HasMarriage);
            RefreshCaptureUi();
        }

        private void SaveToSession()
        {
            _session.First = _txtFirst.Text.Trim();
            _session.Middle = _txtMiddle.Text.Trim();
            _session.Last = _txtLast.Text.Trim();
            _session.First2 = _txtFirst2.Text.Trim();
            _session.Middle2 = _txtMiddle2.Text.Trim();
            _session.Last2 = _txtLast2.Text.Trim();
            _session.Contact = _txtContact.Text.Trim();
            _session.Senior = _priSenior.Checked;
            _session.Pwd = _priPwd.Checked;
            _session.Pregnant = _priPregnant.Checked;
            _session.ClaimTicketEntry = _txtClaimTicket.Text.Trim();
            _session.IdType = OthersBox.Value(_cboIdType);
            _session.IdNo = _txtIdNo.Text.Trim();
        }

        // -------------------------------------------------- marriage (two people)
        /// <summary>
        /// Marriage Application / Marriage Registration are about TWO people, not one — reveals
        /// a second "Wife" column of name fields beside the existing fields (relabelled
        /// "Husband" for that case), and the husband/wife photo toggle on the right card. Every
        /// other service leaves the screen exactly as it always was: no header, no second
        /// column, single camera person. Runs once at Load — each visit gets a fresh form.
        /// </summary>
        private void ApplyMarriagePersons(bool marriage)
        {
            _lblHeadA.Visible = marriage;
            _lblHeadB.Visible = marriage;
            lblFirst2.Visible = marriage;
            lblFirst2Req.Visible = marriage;
            hostFirst2.Visible = marriage;
            lblMiddle2.Visible = marriage;
            hostMiddle2.Visible = marriage;
            lblLast2.Visible = marriage;
            lblLast2Req.Visible = marriage;
            hostLast2.Visible = marriage;
            _btnPersonA.Visible = marriage;
            _btnPersonB.Visible = marriage;

            if (!marriage) return;   // Designer layout already fits the single-person case

            lblPersonalInfo.Text = "Couple's Information";

            // Husband's fields (the same controls every other service uses) narrow to the left
            // half-column so the wife's column fits beside them.
            hostFirst.Location = new Point(30, 108);
            hostFirst.Size = new Size(260, 46);
            lblFirst.Location = new Point(30, 90);
            lblFirstReq.Location = new Point(96, 90);
            hostMiddle.Location = new Point(30, 182);
            hostMiddle.Size = new Size(260, 46);
            lblMiddle.Location = new Point(30, 164);
            hostLast.Location = new Point(30, 256);
            hostLast.Size = new Size(260, 46);
            lblLast.Location = new Point(30, 238);
            lblLastReq.Location = new Point(94, 238);

            // Everything below the name block shifts down to clear the HUSBAND/WIFE header row.
            const int shift = 28;
            lblContact.Location = new Point(30, 324 + shift);
            hostContact.Location = new Point(30, 350 + shift);
            lblPriority.Location = new Point(30, 406 + shift);
            lblPriorityHelp.Location = new Point(148, 411 + shift);
            _priSenior.Location = new Point(30, 436 + shift);
            _priPwd.Location = new Point(213, 436 + shift);
            _priPregnant.Location = new Point(396, 436 + shift);
            lblValidId.Location = new Point(30, 566 + shift);
            lblIdType.Location = new Point(30, 598 + shift);
            hostIdType.Location = new Point(30, 622 + shift);
            lblIdNo.Location = new Point(30, 674 + shift);
            hostIdNo.Location = new Point(30, 698 + shift);
            _claimPanel.Location = new Point(30, 812 + shift);
        }

        /// <summary>Which slot (husband/wife) the next Capture writes into, and its label text.</summary>
        private void SetCapturingWife(bool wife)
        {
            _capturingWife = wife;
            KioskButtons.SetKind(_btnPersonA, wife ? KioskButtonKind.Secondary : KioskButtonKind.Primary);
            KioskButtons.SetKind(_btnPersonB, wife ? KioskButtonKind.Primary : KioskButtonKind.Secondary);
            RefreshCaptureUi();
        }

        private void BtnPersonA_Click(object sender, EventArgs e) => SetCapturingWife(false);
        private void BtnPersonB_Click(object sender, EventArgs e) => SetCapturingWife(true);

        /// <summary>
        /// Updates the two person-toggle button captions (✓ once that person's photo is taken)
        /// and the Capture button's own caption/state for whichever person is current — reused
        /// on load, on toggle, and right after a capture.
        /// </summary>
        private void RefreshCaptureUi()
        {
            if (_session.HasMarriage)
            {
                _btnPersonA.Text = _session.Photo != null ? "Husband  ✓" : "Husband";
                _btnPersonB.Text = _session.Photo2 != null ? "Wife  ✓" : "Wife";
            }
            byte[] current = _capturingWife && _session.HasMarriage ? _session.Photo2 : _session.Photo;
            if (current != null)
            {
                _btnCapture.Text = "Retake Photo";
                KioskButtons.SetGlyph(_btnCapture, KioskCore.IconRetake);
                KioskButtons.SetKind(_btnCapture, KioskButtonKind.Secondary);
            }
            else
            {
                _btnCapture.Text = "Capture Photo";
                KioskButtons.SetGlyph(_btnCapture, KioskCore.IconCamera);
                KioskButtons.SetKind(_btnCapture, KioskButtonKind.Primary);
            }
        }

        // ---------------------------------------------------- photo step
        /// <summary>
        /// Right card is the client's photo only: a large live camera, the Capture button and
        /// its status line. (The ID-upload QR was removed from this step.) The husband/wife
        /// toggle row sits above the camera for marriage services, one camera and two people.
        /// <paramref name="resumingPickup"/> only controls the LEFT card's "previous queue
        /// number" field, which is specific to resuming an earlier parked pickup.
        /// </summary>
        private void ApplyIdentityStep(bool resumingPickup)
        {
            _claimPanel.Visible = resumingPickup;
            if (!resumingPickup) _txtClaimTicket.Clear();

            LayoutPhotoStep();
        }

        private void LayoutPhotoStep()
        {
            _rightTitle.Text = "Take Your Photo";
            _rightHint.Text = "Look at the camera, then tap Capture Photo.";
            _rightTitle.Location = new Point(26, 22);
            _rightHint.Location = new Point(26, 60);

            bool marriage = _session.HasMarriage;
            _btnPersonA.Visible = marriage;
            _btnPersonB.Visible = marriage;
            int camY = marriage ? 148 : 104;
            int camH = marriage ? 440 : 500;

            if (marriage)
            {
                _btnPersonA.Location = new Point(26, camY - 44);
                _btnPersonB.Location = new Point(162, camY - 44);
            }

            _camFrame.Location = new Point(26, camY);
            _camFrame.Size = new Size(518, camH);

            _lblCamState.Location = new Point(26, camY + camH + 14);

            _btnCapture.Location = new Point(26, camY + camH + 46);
            _btnCapture.Size = new Size(518, 62);

            _lblCamStatus.Location = new Point(26, camY + camH + 118);
            _lblCamStatus.Size = new Size(518, 44);
        }
        // ------------------------------------------------- availability
        private void UpdateAvailability()
        {
            bool open = KioskCore.OfficeOnline();
            _offlineOverlay.Visible = !open;
            if (!open) _offlineOverlay.BringToFront();
        }

        // ------------------------------------------------------- layout
        private void PanelStep2_Resize(object sender, EventArgs e) => CenterDetailsStep(panelStep2);

        private void CenterDetailsStep(Control wrap)
        {
            if (_detailsBox == null) return;
            _detailsBox.Left = Math.Max(0, (wrap.ClientSize.Width - _detailsBox.Width) / 2);
            _detailsBox.Top = Math.Max(16, (wrap.ClientSize.Height - _detailsBox.Height) / 2);
        }

        // Fixed design size of _detailsBox, cached before any Scale() ever runs — every
        // FitToScreen call computes its ratio against THIS, never against the box's current
        // (possibly already-scaled) size, or repeated calls would compound/drift.
        private readonly Size _designSize;
        // The scale factor already applied, relative to _designSize (1f = none yet).
        private float _appliedScale = 1f;
        // True once Load has finished writing the per-session baseline layout (single vs.
        // marriage/couple fields, identity-step geometry) in raw design-scale coordinates.
        // panelStep2 can receive its real (maximized) size and fire Resize BEFORE Load runs —
        // if FitToScreen scaled the tree on that early event, it would shrink rightCard itself
        // using the stock Designer positions, and _appliedScale would then read as "already
        // correct" for the final size. Load's ApplyMarriagePersons/LayoutPhotoStep would
        // then overwrite rightCard's (and, in marriage mode, leftCard's) CHILDREN back to raw,
        // unscaled coordinates without rightCard's own (already-shrunk) size being touched —
        // an inconsistent tree where the children overflow their own container, and the later
        // Shown-time FitToScreen call would see f == _appliedScale and skip the correction
        // entirely. Blocking every FitToScreen call until the baseline is in place guarantees
        // the first real scale always applies to a fully consistent, un-scaled tree.
        private bool _baselineReady;

        /// <summary>
        /// Fits the fixed-size details box (and all its children — cards, camera, QR,
        /// buttons) to the available screen, so the same layout adapts to a small laptop or
        /// a big monitor. It scales DOWN on small screens (no overflow) and UP on large /
        /// high-DPI screens (fills the space) — uniform scale, so nothing distorts — capped
        /// so it never grows absurdly large.
        ///
        /// Recomputed on EVERY resize, not once: the kiosk form's first Shown/Resize can fire
        /// before WindowState=Maximized has settled into its final ClientSize (reporting a
        /// smaller, transient size), which used to bake in a wrong scale permanently — the
        /// details box came out wider than the real screen, got left-clamped to X=0 by
        /// CenterDetailsStep, and its right edge (the QR panel) ran off the visible screen.
        /// Always ratio-ing against the fixed <see cref="_designSize"/> makes every call
        /// self-correcting regardless of what an earlier call computed.
        /// </summary>
        // Guards against re-entrancy: Control.Scale() lays out and paints its subtree, which
        // can pump the message queue and deliver an already-queued Resize for panelStep2
        // WHILE this method is still running. A nested call would then read the stale
        // (pre-Scale) _appliedScale and Bounds captured by the OUTER call's now-stale
        // locals, and the outer call would resume and apply its own already-computed delta
        // on top of what the nested call just corrected — scaling the tree twice. Skipping
        // any call that arrives while one is already in flight avoids that; the outer call
        // still lands on the correct target, and if the real available size did change in
        // the meantime, the next natural Resize event (after this one returns) reruns it.
        private bool _scaling;

        private void FitToScreen()
        {
            if (_detailsBox == null || !_baselineReady || _scaling) return;
            int hw = panelStep2.ClientSize.Width, hh = panelStep2.ClientSize.Height;
            if (hw < 100 || hh < 100) return;   // not laid out yet

            const float MaxGrow = 1.6f;   // cap so it fills without becoming oversized
            float f = Math.Min(MaxGrow, Math.Min((hw - 24) / (float)_designSize.Width,
                                                 (hh - 24) / (float)_designSize.Height));
            if (Math.Abs(f - _appliedScale) < 0.01f) return;   // no meaningful change

            _scaling = true;
            try
            {
                float delta = f / _appliedScale;
                _detailsBox.Scale(new SizeF(delta, delta));
                // Fonts don't follow Scale() on their own — on a short screen (1366x768) that
                // left full-size captions clipping/overlapping inside the shrunk fields. Only
                // on the shrink path: growth intentionally keeps fonts at design size.
                if (f < 1f) FontScaler.Scale(_detailsBox, delta);
                _appliedScale = f;
            }
            finally { _scaling = false; }
        }

        private void Field_Enter(object sender, EventArgs e)
        {
            if (((Control)sender).Parent is RoundPanel h)
            { h.BorderColor = KioskCore.Accent; h.BorderWidth = 2f; h.Invalidate(); }
        }

        private void Field_Leave(object sender, EventArgs e)
        {
            if (((Control)sender).Parent is RoundPanel h)
            { h.BorderColor = KioskCore.Line; h.BorderWidth = 1.5f; h.Invalidate(); }
        }

        // --------------------------------------------------- live webcam
        private void StartCamera()
        {
            try
            {
                var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (devices.Count == 0)
                {
                    SetCamState(false);
                    _lblCamStatus.Text = "No camera found — connect a webcam.";
                    return;
                }
                _camera = new VideoCaptureDevice(devices[0].MonikerString);
                _camera.NewFrame += OnFrame;
                _camera.Start();
                SetCamState(true);
                _lblCamStatus.Text = "Live camera is ready. Tap Capture Photo when ready.";
            }
            catch (Exception ex)
            {
                SetCamState(false);
                _lblCamStatus.Text = "Camera error: " + ex.Message;
            }
        }

        private void OnFrame(object sender, NewFrameEventArgs e)
        {
            var frame = (Bitmap)e.Frame.Clone();
            lock (_frameLock)
            {
                _lastFrame?.Dispose();
                _lastFrame = frame;
            }
            // Repaint the preview on the UI thread; the frame is drawn cover-fit in DrawCameraGuide.
            try { if (_picCam.IsHandleCreated) _picCam.BeginInvoke((Action)(() => _picCam.Invalidate())); }
            catch { /* form closing — safe to ignore */ }
        }

        private void BtnCapture_Click(object sender, EventArgs e)
        {
            bool wife = _capturingWife && _session.HasMarriage;
            lock (_frameLock)
            {
                if (_lastFrame == null) { Warn("The camera is not ready yet."); return; }
                using (var ms = new MemoryStream())
                {
                    _lastFrame.Save(ms, ImageFormat.Jpeg);
                    if (wife) _session.Photo2 = ms.ToArray();
                    else _session.Photo = ms.ToArray();
                }
            }
            RefreshCaptureUi();
            _lblCamState.Text = "Photo Captured";
            _lblCamState.ForeColor = Color.FromArgb(46, 148, 87);
            _picCam?.Invalidate();
            _lblCamStatus.Text = _session.HasMarriage
                ? (wife ? "Wife's photo captured. Switch to Husband, or Retake." : "Husband's photo captured. Switch to Wife, or Retake.")
                : "Photo captured. Tap Retake Photo to redo.";
        }

        /// <summary>
        /// Draws a small status dot in the label's own ForeColor, left of its text, replacing the
        /// emoji that used to be baked into the caption. Keeps the status legible in one glance
        /// without depending on the machine's emoji-font support.
        /// </summary>
        private static void AttachStateDot(Label lbl)
        {
            const int dot = 11, gap = 9, pad = 2;
            lbl.Padding = new Padding(dot + gap + pad, 0, 0, 0);
            lbl.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var b = new SolidBrush(lbl.ForeColor))
                    g.FillEllipse(b, pad, (lbl.Height - dot) / 2f, dot, dot);
            };
        }

        private void SetCamState(bool ready)
        {
            _lblCamState.Text = ready ? "Camera Ready" : "Camera Not Detected";
            _lblCamState.ForeColor = ready ? Color.FromArgb(46, 148, 87) : Color.FromArgb(160, 40, 50);
            _lblCamState.Invalidate();
            if (_lblLive != null) _lblLive.Visible = false;   // LIVE badge removed
        }

        private void StopCamera()
        {
            if (_camera != null && _camera.IsRunning)
            {
                _camera.NewFrame -= OnFrame;
                _camera.SignalToStop();
                _camera.WaitForStop();
            }
        }

        /// <summary>
        /// Draws the live preview COVER-fit (fills the box, centre-cropped, never distorted —
        /// like CSS object-fit: cover) then a centred dashed face guide on top. Works at any
        /// box size, so it stays contained and centred on every resolution / window size.
        /// </summary>
        private void DrawCameraGuide(object sender, PaintEventArgs e)
        {
            var pb = (PictureBox)sender;
            var g = e.Graphics;
            g.Clear(Color.FromArgb(17, 24, 39));   // dark backing (shows before the first frame)

            lock (_frameLock)
            {
                if (_lastFrame != null && pb.Width > 0 && pb.Height > 0)
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    // Cover fit: scale to fill the box on the larger axis, centre the overflow.
                    float scale = Math.Max((float)pb.Width / _lastFrame.Width,
                                           (float)pb.Height / _lastFrame.Height);
                    int dw = (int)Math.Ceiling(_lastFrame.Width * scale);
                    int dh = (int)Math.Ceiling(_lastFrame.Height * scale);
                    int dx = (pb.Width - dw) / 2, dy = (pb.Height - dh) / 2;
                    // MIRROR the preview (selfie view) so moving left goes left on screen.
                    // The saved capture stays un-mirrored (normal orientation for the ID photo).
                    g.TranslateTransform(pb.Width, 0);
                    g.ScaleTransform(-1, 1);
                    g.DrawImage(_lastFrame, dx, dy, dw, dh);
                    g.ResetTransform();
                }
            }

            // Clean live preview — no overlay guide or text.
        }

        // ----------------------------------------------------- navigation
        private void BtnBack_Click(object sender, EventArgs e)
        {
            SaveToSession();               // keep typed details for the return trip
            _navigating = true;
            DialogResult = DialogResult.Cancel;   // Cancel => flow re-shows Step 1
            Close();
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            SaveToSession();
            try
            {
                if (!KioskCore.Validate(_session, out string error))
                {
                    Warn(error);
                    return;   // stay on this step so the client can fix it
                }
                _navigating = true;
                DialogResult = DialogResult.OK;   // valid => final review
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sorry, your request could not be submitted:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Application.RemoveMessageFilter(this);
            _idle?.Stop();
            _availability?.Stop();
            StopCamera();
            if (!_navigating && e.CloseReason == CloseReason.UserClosing)
                Environment.Exit(0);
            base.OnFormClosing(e);
        }

        private void Warn(string m) =>
            MessageBox.Show(m, "Please check", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // Grey placeholder text shown inside an empty textbox (Win32 cue banner).
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
        private const int EM_SETCUEBANNER = 0x1501;
        private static void Cue(TextBox t, string text)
        {
            if (t != null && t.IsHandleCreated)
                SendMessage(t.Handle, EM_SETCUEBANNER, (IntPtr)1, text);
        }
    }
}
