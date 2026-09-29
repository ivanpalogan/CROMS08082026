using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// The one-time (and change-later) Mobile Capture setup: the office's free DuckDNS name and token.
    /// Replaces hand-editing App.config. "Test Connection" checks the network, DuckDNS and the runtime
    /// without saving; "Save &amp; Configure" tests, stores the settings (token encrypted for this
    /// Windows account, never shown again), then lets CROMS do the rest on its own - point the name at
    /// this PC, get the trusted certificate, restart the Mobile Capture service over HTTPS and verify it
    /// exactly as a phone would. The window follows that progress live and reports the real failure
    /// if any step cannot finish. Closing it never cancels the background work.
    /// </summary>
    public partial class MobileCaptureSetupForm : Form
    {
        private const int VerifyAttempts = 24;          // x 1.5 s: the service restarts after the certificate arrives
        private bool _busy;                             // a Test / Save round-trip is running
        private bool _followSetup;                      // watching TrustedHost until Ready or Failed
        private bool _locked;                           // configured + not an administrator: status and Retry only
        private int _verifyTries;
        private bool _verifying;
        private bool _verified;
        private string _verifyMessage = "";

        public MobileCaptureSetupForm()
        {
            InitializeComponent();

            _locked = MobileCaptureConfig.IsConfigured && !(Session.User != null && Session.User.Role == "Admin");

            lblHelp.Text =
                "One-time setup so a phone can photograph documents straight into CROMS over a trusted https:// " +
                "address - nothing is installed on the phone.\r\n" +
                "Create a free name at duckdns.org, then enter the name and the token shown on that page. " +
                "The name only ever points at this PC's private office network address.";

            string h = MobileCaptureConfig.Hostname;
            if (h.EndsWith(".duckdns.org")) h = h.Substring(0, h.Length - ".duckdns.org".Length);
            txtHost.Text = h;
            ShowTokenState();

            if (_locked)
            {
                txtHost.Enabled = false; txtToken.Enabled = false; btnTest.Visible = false;
                btnSave.Text = "Retry";
                btnSave.Left = btnTest.Left; btnSave.Width = 220;
            }

            AcceptButton = btnSave;
            CancelButton = btnClose;
            UiTheme.Polish(this);
            btnSave.ForeColor = Color.White;

            // If something is already configured, show where it stands and follow it.
            if (MobileCaptureConfig.IsConfigured)
            {
                _followSetup = true;
                pollTimer.Start();
                ApplyPhase();
            }
            else if (MobileCaptureConfig.TokenUnreadable)
                SetStatus(TrustedHost.StatusNote(), UiTheme.Danger);
        }

        // ------------------------------------------------------------------ entry point

        /// <summary>
        /// Call before handing a phone a Mobile Capture link. Returns true when the trusted https address
        /// is ready. Otherwise it does the useful thing instead of an error box: not configured (or the saved
        /// token cannot be read) opens this setup window; configured but still working or failed opens it in
        /// status mode with the real reason; no network says so plainly.
        /// </summary>
        public static bool EnsureReady(IWin32Window owner)
        {
            if (TrustedHost.Phase == MobilePhase.Ready) return true;

            if (TrustedHost.Phase == MobilePhase.NetworkUnavailable)
            {
                MessageBox.Show(owner, "Mobile Capture network unavailable." + Environment.NewLine + Environment.NewLine +
                    "This PC is not connected to a Wi-Fi or LAN network. Connect it to the office network and try again.",
                    "Mobile Capture", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            using (var f = new MobileCaptureSetupForm())
                f.ShowDialog(owner);
            return TrustedHost.Phase == MobilePhase.Ready;
        }

        // ------------------------------------------------------------------ helpers

        private void ShowTokenState()
        {
            if (MobileCaptureConfig.HasToken)
            {
                lblTokenState.Text = "A token is saved on this PC (hidden). Leave the box blank to keep it.";
                lblTokenState.ForeColor = UiTheme.Success;
            }
            else
            {
                lblTokenState.Text = MobileCaptureConfig.TokenUnreadable
                    ? "The saved token belongs to a different Windows account - enter it again."
                    : "Copy the token from your DuckDNS page. It is stored encrypted and never shown again.";
                lblTokenState.ForeColor = MobileCaptureConfig.TokenUnreadable ? UiTheme.Danger : UiTheme.Muted;
            }
        }

        private void SetStatus(string text, Color color)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
        }

        private void SetBusy(bool busy, bool showProgress)
        {
            _busy = busy;
            progress.Visible = showProgress;
            btnTest.Enabled = !busy;
            btnSave.Enabled = !busy;
        }

        // ------------------------------------------------------------------ Test Connection

        private async void btnTest_Click(object sender, EventArgs e)
        {
            string host = txtHost.Text, token = txtToken.Text;
            SetBusy(true, true);
            SetStatus("Testing the network, DuckDNS and the Mobile Capture runtime...", UiTheme.Muted);
            string msg = null; bool ok = false;
            await Task.Run(() => { ok = TrustedHost.Test(host, token, out msg); });
            if (IsDisposed) return;
            SetBusy(false, false);
            SetStatus(msg, ok ? UiTheme.Success : UiTheme.Danger);
        }

        // ------------------------------------------------------------------ Save & Configure

        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (_locked)
            {
                // Status-only mode: just try again with what is already saved.
                RestartFollow();
                TrustedHost.Apply();
                return;
            }

            string host = txtHost.Text, token = txtToken.Text;
            string err;
            string full = MobileCaptureConfig.NormalizeHost(host, out err);
            if (full.Length == 0) { SetStatus(err, UiTheme.Danger); txtHost.Focus(); return; }
            if (token.Trim().Length > 0 ? !MobileCaptureConfig.TokenLooksValid(token, out err) : !MobileCaptureConfig.HasToken)
            {
                if (err.Length == 0) err = "Enter your DuckDNS token.";
                SetStatus(err, UiTheme.Danger); txtToken.Focus(); return;
            }

            SetBusy(true, true);
            SetStatus("Checking the DuckDNS name and token...", UiTheme.Muted);
            string msg = null; bool ok = false;
            await Task.Run(() => { ok = TrustedHost.Test(host, token, out msg); });
            if (IsDisposed) return;
            if (!ok)
            {
                SetBusy(false, false);
                SetStatus(msg + "\r\nNothing was saved.", UiTheme.Danger);
                return;
            }

            if (!MobileCaptureConfig.Save(host, token, out err))
            {
                SetBusy(false, false);
                SetStatus(err, UiTheme.Danger);
                return;
            }
            txtToken.Clear();
            ShowTokenState();
            Audit.Write(Audit.Update, "mobile_capture", 0, "Mobile Capture hostname set to " + full);

            SetBusy(false, true);
            RestartFollow();
            SetStatus("Saved. CROMS is now getting the trusted certificate for " + full +
                      " - the first time this takes a few minutes and needs Internet.", UiTheme.Muted);
            TrustedHost.Apply();
        }

        private void RestartFollow()
        {
            _verified = false; _verifying = false; _verifyTries = 0; _verifyMessage = "";
            _followSetup = true;
            progress.Visible = true;
            if (!pollTimer.Enabled) pollTimer.Start();
        }

        // ------------------------------------------------------------------ following the setup

        private void pollTimer_Tick(object sender, EventArgs e)
        {
            if (_busy || !_followSetup) return;
            ApplyPhase();
        }

        private void ApplyPhase()
        {
            switch (TrustedHost.Phase)
            {
                case MobilePhase.NotConfigured:
                    progress.Visible = false;
                    break;

                case MobilePhase.NetworkUnavailable:
                    progress.Visible = false;
                    SetStatus("Mobile Capture network unavailable.\r\nConnect this PC to the office Wi-Fi or LAN; CROMS continues on its own once it is connected.", UiTheme.Danger);
                    break;

                case MobilePhase.Working:
                    progress.Visible = true;
                    SetStatus("Getting the trusted certificate for " + TrustedHost.Host +
                              " (first time takes a few minutes, needs Internet)...", UiTheme.Muted);
                    break;

                case MobilePhase.Failed:
                    progress.Visible = false;
                    _followSetup = false;
                    SetStatus("Mobile Capture setup failed:\r\n" + TrustedHost.LastError +
                              "\r\nFix the cause, then press " + btnSave.Text.Replace("&&", "&") + " to try again.", UiTheme.Danger);
                    break;

                case MobilePhase.Ready:
                    VerifyReady();
                    break;
            }
        }

        // Certificate on disk: prove it end to end the way a phone would, retrying while the service restarts.
        private void VerifyReady()
        {
            if (_verified) return;
            if (_verifying) { progress.Visible = true; return; }
            if (_verifyTries >= VerifyAttempts)
            {
                progress.Visible = false;
                _followSetup = false;
                SetStatus("The certificate is ready, but the HTTPS check did not pass:\r\n" + _verifyMessage, UiTheme.Danger);
                return;
            }
            _verifying = true; _verifyTries++;
            progress.Visible = true;
            SetStatus("Certificate ready. Verifying https://" + TrustedHost.Host + " ...", UiTheme.Muted);
            Task.Run(() =>
            {
                string msg; bool ok = TrustedHost.VerifyHttps(out msg);
                try
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke((Action)(() =>
                    {
                        _verifying = false;
                        if (ok)
                        {
                            _verified = true; _followSetup = false;
                            progress.Visible = false;
                            SetStatus("Ready. The Mobile Capture QR will use https://" + TrustedHost.Host + " and follows this PC if its Wi-Fi changes.\r\n" +
                                      "If a phone ever shows \"CROMS server is not reachable from this Wi-Fi network\", that Wi-Fi blocks device-to-device traffic.", UiTheme.Success);
                            btnClose.Text = "Done";
                        }
                        else _verifyMessage = msg;
                    }));
                }
                catch { _verifying = false; }
            });
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult = TrustedHost.CertReady ? DialogResult.OK : DialogResult.Cancel;
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            pollTimer.Stop();
            base.OnFormClosed(e);
        }
    }
}
