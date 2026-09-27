using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Shown only when the client picked "Marriage Registration". Registering a marriage in
    /// CROMS is recording a wedding that already happened under a licence the couple already
    /// obtained (Forms/MarriageEntryForm.cs's own workflow) — it is not the same thing as
    /// APPLYING for that licence (MARRIAGE_APP / Forms/MarriageLicenseForm.cs). A client who taps
    /// "Marriage Registration" without having applied yet has picked the wrong card, and the
    /// staff window would otherwise find that out only after the ticket is called.
    /// <para/>
    /// So the kiosk asks here, before the ticket is issued: has the licence already been applied
    /// for? "No" swaps the selection to Marriage Application (MARRIAGE_APP) so the client is
    /// routed to the step they actually need — nothing about the visit is lost, they just were
    /// on the wrong card. "Yes" asks the client to PHOTOGRAPH the physical licence with the
    /// kiosk's own webcam — not type its number (2026-09-27): a typed registry/licence number is
    /// the least trustworthy field on any of these forms (this project has hit that exact
    /// failure on 2026-09-06 and again on 2026-09-10), while a photo is the document itself and
    /// lets the desk read whatever it needs off it, alongside the scanned certificate.
    /// </summary>
    public sealed partial class MarriageLicenseCheckForm : Form, IMessageFilter
    {
        private readonly KioskSession _session;
        private Timer _idle;
        private Action _resetIdle;
        private bool? _answer;   // null = unanswered, true = "Yes, I have a licence", false = "No"
        // Set by every DELIBERATE close so OnFormClosing can tell navigation from a real quit.
        private bool _navigating;

        private VideoCaptureDevice _camera;
        private Bitmap _lastFrame;
        private readonly object _frameLock = new object();

        public MarriageLicenseCheckForm(KioskSession session)
        {
            _session = session;
            InitializeComponent();

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

        private void MarriageLicenseCheckForm_Load(object sender, EventArgs e)
        {
            LoadFromSession();
            CenterCard();
        }

        private void MarriageLicenseCheckForm_Resize(object sender, EventArgs e) => CenterCard();

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x0200 || m.Msg == 0x0201 || m.Msg == 0x0100 || m.Msg == 0x020A) _resetIdle?.Invoke();
            return false;
        }

        private void No_Click(object sender, EventArgs e) => SetAnswer(false);
        private void Yes_Click(object sender, EventArgs e) => SetAnswer(true);

        private void SetAnswer(bool yes)
        {
            _answer = yes;
            ApplyAnswerStyle();
            _photoCaption.Visible = yes;
            _picLicense.Visible = yes;
            _lblCamStatus.Visible = yes;
            _btnCapture.Visible = yes;

            if (yes)
            {
                if (_session.MarriageLicenseImage != null) ShowCaptured();
                else StartCamera();
            }
            else
            {
                StopCamera();
            }
        }

        private void Back_Click(object sender, EventArgs e)
        {
            _navigating = true;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void Continue_Click(object sender, EventArgs e)
        {
            if (_answer == null)
            {
                Warn("Please tell us whether you already applied for your Marriage License.");
                return;
            }

            if (_answer == true)
            {
                if (_session.MarriageLicenseImage == null)
                {
                    Warn("Please take a photo of your Marriage License.");
                    return;
                }
            }
            else
            {
                // Wrong card for this client — route them to Marriage Application instead.
                _session.MarriageLicenseImage = null;
                _session.Selected.Remove("MARRIAGE_REG");
                if (!_session.Selected.Contains("MARRIAGE_APP")) _session.Selected.Add("MARRIAGE_APP");
            }

            StopCamera();
            _navigating = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static void Warn(string text)
        {
            MessageBox.Show(text, "Please check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ------------------------------------------------------- session <-> fields
        private void LoadFromSession()
        {
            if (_session.MarriageLicenseImage != null)
            {
                SetAnswer(true);
            }
            else
            {
                _answer = null;
                ApplyAnswerStyle();
                _photoCaption.Visible = false;
                _picLicense.Visible = false;
                _lblCamStatus.Visible = false;
                _btnCapture.Visible = false;
            }
        }

        // --------------------------------------------------------- webcam capture
        private void StartCamera()
        {
            try
            {
                var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (devices.Count == 0)
                {
                    _lblCamStatus.Text = "No camera found — connect a webcam.";
                    _btnCapture.Enabled = false;
                    return;
                }
                _camera = new VideoCaptureDevice(devices[0].MonikerString);
                _camera.NewFrame += OnFrame;
                _camera.Start();
                _btnCapture.Enabled = true;
                _btnCapture.Text = "📷 Capture Photo";
                _lblCamStatus.Text = "Hold the Marriage License steady in front of the camera, then tap Capture Photo.";
            }
            catch (Exception ex)
            {
                _lblCamStatus.Text = "Camera error: " + ex.Message;
                _btnCapture.Enabled = false;
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
            try
            {
                if (_picLicense.IsHandleCreated)
                    _picLicense.BeginInvoke((Action)(() =>
                    {
                        var old = _picLicense.Image;
                        Bitmap shown;
                        lock (_frameLock) { shown = _lastFrame == null ? null : (Bitmap)_lastFrame.Clone(); }
                        if (shown != null) { _picLicense.Image = shown; old?.Dispose(); }
                    }));
            }
            catch { /* form closing — safe to ignore */ }
        }

        private void Capture_Click(object sender, EventArgs e)
        {
            if (_session.MarriageLicenseImage != null)
            {
                // Already captured — this click is "Retake".
                _session.MarriageLicenseImage = null;
                StartCamera();
                return;
            }

            lock (_frameLock)
            {
                if (_lastFrame == null) { Warn("The camera is not ready yet."); return; }
                using (var ms = new MemoryStream())
                {
                    _lastFrame.Save(ms, ImageFormat.Jpeg);
                    _session.MarriageLicenseImage = ms.ToArray();
                }
            }
            StopCamera();
            ShowCaptured();
        }

        private void ShowCaptured()
        {
            using (var ms = new MemoryStream(_session.MarriageLicenseImage))
            {
                var old = _picLicense.Image;
                _picLicense.Image = new Bitmap(ms);
                old?.Dispose();
            }
            _btnCapture.Text = "🔄 Retake Photo";
            _btnCapture.Enabled = true;
            _lblCamStatus.Text = "Photo captured. Tap Retake Photo to redo.";
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

        // ------------------------------------------------------------------ helpers
        private void ApplyAnswerStyle()
        {
            bool yes = _answer == true, no = _answer == false;
            _btnYes.BackColor = yes ? KioskCore.Accent : KioskCore.CardBg;
            _btnYes.ForeColor = yes ? System.Drawing.Color.White : KioskCore.Ink;
            _btnYes.FlatAppearance.BorderColor = yes ? KioskCore.Accent : KioskCore.Line;
            _btnNo.BackColor = no ? KioskCore.Accent : KioskCore.CardBg;
            _btnNo.ForeColor = no ? System.Drawing.Color.White : KioskCore.Ink;
            _btnNo.FlatAppearance.BorderColor = no ? KioskCore.Accent : KioskCore.Line;
        }

        private void CenterCard()
        {
            _card.Location = new System.Drawing.Point(
                Math.Max(0, (ClientSize.Width - _card.Width) / 2),
                Math.Max(0, (ClientSize.Height - _card.Height) / 2));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Application.RemoveMessageFilter(this);
            _idle?.Stop();
            StopCamera();
            if (!_navigating && e.CloseReason == CloseReason.UserClosing) Environment.Exit(0);
            base.OnFormClosing(e);
        }
    }
}
