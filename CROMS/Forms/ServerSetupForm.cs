using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// First-run (and reconnect) screen: the operator types the IP address of the
    /// PC that hosts the CROMS database, tests it, and saves it. The address is
    /// stored in %APPDATA%\CROMS\server.cfg via <see cref="ServerConfig"/>, so a
    /// client PC never edits App.config, and a changed server IP is fixed here
    /// instead of in a text file. Code-built (a startup dialog, not a module).
    /// </summary>
    public partial class ServerSetupForm : Form
    {
        private CancellationTokenSource _scanCts;

        public ServerSetupForm(string message = null)
        {
            InitializeComponent();

            // Runtime text + prefill (message is a caller-supplied reconnect note).
            help.Text = message ?? "Enter the IP address of the computer that has the CROMS database " +
                                   "(the \"server\" PC). Make sure this PC is on the same Wi-Fi or hotspot.";
            if (message != null) help.ForeColor = Color.FromArgb(176, 0, 32);

            // Pre-fill with the saved value, else the App.config default host.
            _txtHost.Text = ServerConfig.IsConfigured ? ServerConfig.Host : DefaultHost();
            _txtPort.Text = (ServerConfig.IsConfigured ? ServerConfig.Port : 3306).ToString();

            AcceptButton = _btnSave;
            CancelButton = _btnCancel;
            ActiveControl = _txtHost;
        }

        private async void btnScan_Click(object sender, EventArgs e) => await ScanAsync();
        private async void btnTest_Click(object sender, EventArgs e) => await TestAsync();
        private async void btnSave_Click(object sender, EventArgs e) => await SaveAsync();

        /// <summary>
        /// Silently sweep the network for the CROMS server, showing a small
        /// "Looking for server..." splash. Returns the found IP (already tested with
        /// the CROMS credentials) or null. Used at startup so a client whose saved
        /// server IP changed (new Wi-Fi/hotspot) self-heals with zero clicks before
        /// the manual Connect-to-Server screen is ever shown.
        /// </summary>
        public static string AutoDiscover(int port = 3306)
        {
            string result = null;
            using (var splash = new Form())
            {
                splash.Text = "CROMS";
                splash.FormBorderStyle = FormBorderStyle.FixedDialog;
                splash.StartPosition = FormStartPosition.CenterScreen;
                splash.MaximizeBox = splash.MinimizeBox = false;
                splash.ControlBox = false;
                splash.ClientSize = new Size(420, 120);
                splash.BackColor = Color.White;
                splash.Font = new Font("Segoe UI", 9.75f);

                var title = new Label
                {
                    Text = "Looking for the CROMS server on your network...",
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(33, 37, 41),
                    AutoSize = false, Location = new Point(20, 20), Size = new Size(380, 26)
                };
                var detail = new Label
                {
                    Text = "Please wait.",
                    ForeColor = Color.FromArgb(90, 90, 90),
                    AutoSize = false, Location = new Point(20, 52), Size = new Size(380, 22)
                };
                var bar = new ProgressBar
                {
                    Style = ProgressBarStyle.Continuous,
                    Location = new Point(20, 82), Size = new Size(380, 18),
                    Minimum = 0, Maximum = 100, Value = 0
                };
                splash.Controls.AddRange(new Control[] { title, detail, bar });

                Action<int, int> onProgress = (done, total) =>
                {
                    if (splash.IsDisposed) return;
                    try { splash.BeginInvoke((Action)(() =>
                    {
                        detail.Text = "Checked " + done + " of " + total + " addresses...";
                        if (total > 0) bar.Value = Math.Min(100, done * 100 / total);
                    })); }
                    catch { }
                };

                splash.Shown += async (s, e) =>
                {
                    try { result = await ServerConfig.DiscoverServerAsync(port, onProgress); }
                    catch { result = null; }
                    if (!splash.IsDisposed) splash.Close();
                };

                splash.ShowDialog();
            }
            return result;
        }

        private static string DefaultHost()
        {
            try { return new MySqlConnectionStringBuilder(ServerConfig.BaseConnectionString).Server; }
            catch { return "127.0.0.1"; }
        }

        private bool ReadInputs(out string host, out int port)
        {
            host = _txtHost.Text.Trim();
            if (!int.TryParse(_txtPort.Text.Trim(), out port) || port <= 0) port = 3306;
            if (host.Length == 0)
            {
                SetStatus("Please enter the server IP address.", false);
                _txtHost.Focus();
                return false;
            }
            return true;
        }

        private async Task ScanAsync()
        {
            int port = int.TryParse(_txtPort.Text.Trim(), out int p) && p > 0 ? p : 3306;
            Busy(true, "Searching the network for the CROMS server...");
            _scanCts = new CancellationTokenSource();

            Action<int, int> onProgress = (done, total) =>
            {
                if (IsDisposed) return;
                try { BeginInvoke((Action)(() =>
                    SetStatus("Searching... " + done + " / " + total + " addresses checked", null))); }
                catch { /* form closing */ }
            };

            string ip = null;
            try { ip = await ServerConfig.DiscoverServerAsync(port, onProgress, _scanCts.Token); }
            catch { /* swallow — reported below */ }

            Busy(false, null);
            if (!string.IsNullOrEmpty(ip))
            {
                _txtHost.Text = ip;
                SetStatus("✔  Found the CROMS server at " + ip + ". Click Save && Continue.", true);
            }
            else
            {
                SetStatus("✖  No CROMS server found. Check that the server PC is on " +
                          "and this PC is on the same Wi-Fi/hotspot, then type the IP.", false);
            }
        }

        private async Task<bool> TestAsync()
        {
            if (!ReadInputs(out string host, out int port)) return false;
            Busy(true, "Testing connection to " + host + " ...");
            string err = null;
            bool ok = await Task.Run(() => ServerConfig.TestConnection(host, port, out err));
            Busy(false, null);
            if (ok) SetStatus("✔  Connected successfully.", true);
            else SetStatus("✖  Could not connect: " + Short(err), false);
            return ok;
        }

        private async Task SaveAsync()
        {
            if (!ReadInputs(out string host, out int port)) return;
            Busy(true, "Testing connection to " + host + " ...");
            string err = null;
            bool ok = await Task.Run(() => ServerConfig.TestConnection(host, port, out err));
            Busy(false, null);

            if (!ok)
            {
                var choice = MessageBox.Show(
                    "Could not connect to " + host + ":\n\n" + Short(err) +
                    "\n\nSave this address anyway?",
                    "Connection failed", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (choice != DialogResult.Yes)
                {
                    SetStatus("✖  Not saved. Fix the address and try again.", false);
                    return;
                }
            }

            ServerConfig.Save(host, port);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Busy(bool busy, string status)
        {
            _btnScan.Enabled = _btnTest.Enabled = _btnSave.Enabled = !busy;
            if (status != null) SetStatus(status, null);
        }

        private void SetStatus(string text, bool? good)
        {
            _lblStatus.Text = text;
            _lblStatus.ForeColor = good == null ? Color.FromArgb(90, 90, 90)
                : (good.Value ? Color.FromArgb(25, 135, 84) : Color.FromArgb(176, 0, 32));
        }

        private static string Short(string err)
        {
            if (string.IsNullOrEmpty(err)) return "unknown error";
            return err.Length > 160 ? err.Substring(0, 160) + "..." : err;
        }
    }
}
