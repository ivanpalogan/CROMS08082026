using System;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Live operations dashboard for the front desk. Shows TODAY's actionable numbers —
    /// Waiting Now, Registered Today, Collections Today, Pending Releases — each on a card
    /// that (where it maps to a module) is clickable to jump straight there, plus a 7-day
    /// registrations bar-trend and the live Service Windows Online/Offline board. All cards,
    /// panels and the status timer are placed in the Designer (DashboardForm.Designer.cs);
    /// this code-behind fills their values, draws the trend, and refreshes on a timer so the
    /// numbers stay live without leaving the screen. Embedded in the MainForm content panel.
    /// </summary>
    public partial class DashboardForm : Form, IRefreshable
    {
        // Colour queue red once it crosses this many waiting clients (a simple threshold cue).
        private const int WaitingAlert = 10;

        private static readonly Color Ink   = Color.FromArgb(33, 37, 41);
        private static readonly Color Green = Color.FromArgb(25, 135, 84);
        private static readonly Color Red   = Color.FromArgb(220, 53, 69);
        private static readonly Color Blue  = Color.FromArgb(13, 110, 253);

        private readonly int[] _trend = new int[7];
        private readonly string[] _trendLabels = new string[7];
        private int _tickCount;

        public DashboardForm()
        {
            InitializeComponent();

            // Cards that map to a module become clickable (per modern-dashboard guidance:
            // connect a metric to its action).
            WireCard(cardWaiting);      // → Queue Management
            WireCard(cardCollections);  // → Fees & Payments
            WireCard(cardPending);      // → Release & Claim

            // Mobile App Connection panel: reflect the auto-started Ionic servers live
            // (both the scanner app and the claimapp ID-upload app).
            IonicServerManager.Instance.Changed += OnIonicChanged;
            IonicServerManager.ClaimApp.Changed += OnIonicChanged;
            Disposed += (s, e) =>
            {
                IonicServerManager.Instance.Changed -= OnIonicChanged;
                IonicServerManager.ClaimApp.Changed -= OnIonicChanged;
            };

            // Connected-devices list (paired phones), filled by polling the save-API.
            lblDevices = new Label
            {
                Location = new Point(8, 292),
                Size = new Size(Math.Max(120, pnlMobileConn.Width - 16), 70),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(73, 80, 87),
                Text = "",
            };
            pnlMobileConn.Controls.Add(lblDevices);
            SetupMobilePanel();

            RefreshData();
            statusTimer.Start();
        }

        public void RefreshData()
        {
            LoadKpis();
            LoadTrend();
            LoadWindowStatus();
            UpdateMobileConn();
            UpdateDevices();
        }

        /// <summary>Poll the save-API for paired phones and list them under the QR.</summary>
        private async void UpdateDevices()
        {
            if (lblDevices == null) return;
            // On the server PC the save-API is local; on a client PC it lives on
            // the server, so query the server's address for the paired-phone list.
            string eh = CROMS.Data.ServerConfig.EffectiveHost;
            bool remote = !string.IsNullOrWhiteSpace(eh) && eh != "localhost" && eh != "127.0.0.1" && eh != "::1";
            string apiHost = remote ? eh : "localhost";
            string url = "http://" + apiHost + ":" + IonicServerManager.Instance.ApiPort + "/api/devices";
            string json;
            try { json = await _http.GetStringAsync(url); }
            catch
            {
                SetDevices(remote
                    ? "Phone pairing runs on the main PC."
                    : "Save-API offline — run the server (port " + IonicServerManager.Instance.ApiPort + ") to pair phones.");
                return;
            }
            try
            {
                var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
                var root = (System.Collections.Generic.Dictionary<string, object>)ser.DeserializeObject(json);
                var arr = root.ContainsKey("devices") ? root["devices"] as object[] : null;
                int online = 0;
                var lines = new System.Collections.Generic.List<string>();
                if (arr != null)
                {
                    foreach (var o in arr)
                    {
                        var d = o as System.Collections.Generic.Dictionary<string, object>;
                        if (d == null) continue;
                        bool on = d.ContainsKey("online") && Convert.ToBoolean(d["online"]);
                        if (on) online++;
                        string name = d.ContainsKey("name") ? Convert.ToString(d["name"]) : "Phone";
                        string ip = d.ContainsKey("ip") ? Convert.ToString(d["ip"]) : "";
                        lines.Add((on ? "🟢 " : "⚪ ") + name + (ip.Length > 0 ? "  " + ip : ""));
                    }
                }
                string head = online + " device" + (online == 1 ? "" : "s") + " connected";
                SetDevices(lines.Count > 0 ? head + "\r\n" + string.Join("\r\n", lines.ToArray()) : "No phones paired yet.");
            }
            catch { SetDevices(""); }
        }

        private void SetDevices(string text)
        {
            if (lblDevices == null) return;
            if (InvokeRequired) { if (IsHandleCreated) BeginInvoke((Action)(() => lblDevices.Text = text)); return; }
            lblDevices.Text = text;
        }

        // --------------------------------------------------- mobile connection (QR)
        private string _lastQrUrl = "";
        private Label lblDevices;
        private Label lblWifiInfo;
        private Label lblClaimApp;
        private static readonly System.Net.Http.HttpClient _http =
            new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        private void OnIonicChanged()
        {
            if (InvokeRequired) { if (IsHandleCreated) BeginInvoke((Action)UpdateMobileConn); return; }
            UpdateMobileConn();
        }

        /// <summary>
        /// Shows the mobile connection as a QR of the detected Wi-Fi URL. The raw URL
        /// is kept internal (only shown when App.config ShowMobileUrlDebug = true).
        /// Status maps to Running / Connecting / Offline; the QR is hidden until a
        /// valid, running connection exists.
        /// </summary>
        private void UpdateMobileConn()
        {
            if (pnlMobileConn == null) return;
            var m = IonicServerManager.Instance;

            switch (m.Status)
            {
                case IonicStatus.Running:
                    lblMobileStatus.Text = "● Running" + (m.NetworkType.Length > 0 ? " · " + m.NetworkType : "");
                    lblMobileStatus.ForeColor = Green;
                    ShowRunningQrs(m);          // Wi-Fi QR (join) + App QR (open)
                    break;
                case IonicStatus.Starting:
                    lblMobileStatus.Text = "● Connecting…";
                    lblMobileStatus.ForeColor = Color.FromArgb(255, 193, 7);
                    picMobileQr.Visible = false;
                    lblMobileMsg.Visible = false;
                    lblMobileHint.Visible = false;
                    HideQrExtras();
                    break;
                default: // Stopped / Error
                    // This PC isn't running the mobile server (a client station).
                    // If we know the server PC's address, show a QR that points the
                    // phone at the mobile scanner served by the MAIN PC, so the QR
                    // is available at every station — not just the host.
                    string remote = BuildRemoteMobileUrl();
                    if (remote != null)
                    {
                        lblMobileStatus.Text = "● Mobile scanner · main PC";
                        lblMobileStatus.ForeColor = Green;
                        SetMobileQr(remote);
                        lblMobileHint.Text = "Connect the phone to the same Wi-Fi/hotspot, then scan.";
                        lblMobileHint.Visible = true;
                        lblMobileMsg.Visible = false;
                        HideQrExtras();
                    }
                    else
                    {
                        lblMobileStatus.Text = "● Offline";
                        lblMobileStatus.ForeColor = Red;
                        picMobileQr.Visible = false;
                        lblMobileHint.Visible = false;
                        lblMobileMsg.Text = "Mobile App Connection Unavailable";
                        lblMobileMsg.Visible = true;
                        HideQrExtras();
                        _lastQrUrl = "";
                    }
                    break;
            }

            // Show the URL text too (under the QR) so it can be typed manually.
            lblMobileUrl.Visible = m.Status == IonicStatus.Running;
            if (lblMobileUrl.Visible) lblMobileUrl.Text = m.MobileUrl;

            // claimapp (ID-upload app) status line — second auto-started server.
            if (lblClaimApp != null)
            {
                var ca = IonicServerManager.ClaimApp;
                switch (ca.Status)
                {
                    case IonicStatus.Running:
                        lblClaimApp.Text = "claimapp ● " + ca.MobileUrl;
                        lblClaimApp.ForeColor = Color.FromArgb(111, 66, 193);
                        break;
                    case IonicStatus.Starting:
                        lblClaimApp.Text = "claimapp ● starting…";
                        lblClaimApp.ForeColor = Color.FromArgb(255, 140, 0);
                        break;
                    default:
                        lblClaimApp.Text = "claimapp ○ offline";
                        lblClaimApp.ForeColor = Color.FromArgb(148, 163, 184);
                        break;
                }
            }
        }

        /// <summary>
        /// The mobile-scanner URL on the SERVER PC (which hosts the mobile services),
        /// built from the saved server IP + the mobile scheme/port. Returns null on
        /// the server itself (no saved host) so it keeps its own live-status QR.
        /// </summary>
        private string BuildRemoteMobileUrl()
        {
            string host = CROMS.Data.ServerConfig.EffectiveHost;
            if (string.IsNullOrWhiteSpace(host)) return null;
            // Local DB = this IS the server PC; keep its own live-status QR instead.
            if (host == "localhost" || host == "127.0.0.1" || host == "::1") return null;
            var m = IonicServerManager.Instance;
            string scheme = string.IsNullOrEmpty(m.Scheme) ? "https" : m.Scheme;
            int port = m.Port > 0 ? m.Port : 4200;
            return scheme + "://" + host + ":" + port;
        }

        private void SetMobileQr(string url)
        {
            if (string.IsNullOrEmpty(url)) { picMobileQr.Visible = false; return; }
            if (url == _lastQrUrl && picMobileQr.Image != null) { picMobileQr.Visible = true; lblMobileMsg.Visible = false; lblMobileHint.Visible = true; return; }

            var bmp = QrHelper.TryCreate(url, 6);
            if (bmp != null)
            {
                var old = picMobileQr.Image;
                picMobileQr.Image = bmp;
                if (old != null) old.Dispose();
                picMobileQr.Visible = true;
                lblMobileMsg.Visible = false;
                lblMobileHint.Visible = true;
                _lastQrUrl = url;
            }
            else
            {
                // QRCoder not installed — can't draw a QR; fall back to showing the URL.
                picMobileQr.Visible = false;
                lblMobileMsg.Text = "Install QRCoder to show the QR.\r\n" + url;
                lblMobileMsg.Visible = true;
                lblMobileHint.Visible = false;
            }
        }

        /// <summary>One App QR, centered, with the network to join shown as text below.</summary>
        private void SetupMobilePanel()
        {
            lblMobileStatus.SetBounds(7, 6, 257, 18);
            lblMobileStatus.TextAlign = ContentAlignment.MiddleCenter;

            picMobileQr.SetBounds(51, 26, 170, 170);   // centered in the 271-wide panel

            lblWifiInfo = new Label
            {
                Location = new Point(7, 200), Size = new Size(257, 16),
                Font = new Font("Segoe UI", 8.75F, FontStyle.Bold), ForeColor = Color.FromArgb(13, 110, 253),
                TextAlign = ContentAlignment.MiddleCenter, Text = "", Visible = false,
            };
            pnlMobileConn.Controls.Add(lblWifiInfo);

            lblMobileHint.SetBounds(7, 218, 257, 16);
            lblMobileUrl.SetBounds(7, 236, 257, 20);
            lblDevices.SetBounds(7, 258, 257, 44);

            // claimapp (ID-upload app) — second auto-started server, shown as a line.
            lblClaimApp = new Label
            {
                Location = new Point(7, 304), Size = new Size(257, 16),
                Font = new Font("Segoe UI", 8.25F), ForeColor = Color.FromArgb(111, 66, 193),
                TextAlign = ContentAlignment.MiddleCenter, Text = "",
            };
            pnlMobileConn.Controls.Add(lblClaimApp);
        }

        private void HideQrExtras()
        {
            if (lblWifiInfo != null) lblWifiInfo.Visible = false;
        }

        /// <summary>Show the single App QR + the network to join (Wi-Fi primary, hotspot fallback).</summary>
        private void ShowRunningQrs(IonicServerManager m)
        {
            string ssid, pass, src;
            if (lblWifiInfo != null && HotspotInfo.TryGet(out ssid, out pass, out src))
            {
                string kind = src == "hotspot" ? "Hotspot" : "Wi-Fi";
                lblWifiInfo.Text = "📶 " + kind + ": " + ssid +
                                   (string.IsNullOrEmpty(pass) ? "" : "   •   " + pass);
                lblWifiInfo.Visible = true;
                lblMobileHint.Text = "Connect the phone to this network, then scan the QR.";
            }
            else
            {
                if (lblWifiInfo != null) lblWifiInfo.Visible = false;
                lblMobileHint.Text = "Join the laptop's Wi-Fi/hotspot, then scan the QR to open the app.";
            }
            SetMobileQr(m.QrPayload);
        }

        // ------------------------------------------------------------------ KPIs
        private void LoadKpis()
        {
            lblToday.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");
            lblUpdated.Text = "Updated " + DateTime.Now.ToString("h:mm tt");

            bool connected = Db.IsConnected();
            lblConnection.Text = connected ? "● Database: Connected" : "● Database: Disconnected";
            lblConnection.ForeColor = connected ? Green : Red;
            if (!connected) return;

            int waiting = Scalar(
                "SELECT COUNT(*) FROM queue_tickets WHERE status = 'Waiting' AND DATE(created_at) = CURDATE()");
            int registered = Scalar(
                "SELECT (SELECT COUNT(*) FROM births    WHERE DATE(created_at) = CURDATE()) + " +
                "       (SELECT COUNT(*) FROM marriages WHERE DATE(created_at) = CURDATE()) + " +
                "       (SELECT COUNT(*) FROM deaths    WHERE DATE(created_at) = CURDATE())");
            decimal collections = ScalarDec(
                "SELECT COALESCE(SUM(net_amount), 0) FROM payments WHERE DATE(paid_at) = CURDATE()");
            int pending = Scalar(
                "SELECT COUNT(*) FROM transactions WHERE status = 'ForRelease'");

            lblWaitingValue.Text = waiting.ToString();
            lblWaitingValue.ForeColor = waiting > WaitingAlert ? Red : Ink;   // threshold cue

            lblRegisteredValue.Text = registered.ToString();
            lblCollectionsValue.Text = "₱" + collections.ToString("N2");

            lblPendingValue.Text = pending.ToString();
            lblPendingValue.ForeColor = pending > 0 ? Blue : Ink;             // something to hand over
        }

        // ---------------------------------------------------------------- trend
        /// <summary>Counts registrations (births + marriages + deaths) per day for the last 7 days.</summary>
        private void LoadTrend()
        {
            DateTime start = DateTime.Today.AddDays(-6);
            for (int i = 0; i < 7; i++) { _trend[i] = 0; _trendLabels[i] = start.AddDays(i).ToString("ddd"); }

            foreach (string table in new[] { "births", "marriages", "deaths" })   // whitelist, not user input
            {
                try
                {
                    DataTable dt = Db.Pull(
                        "SELECT DATE(created_at) AS d, COUNT(*) AS c FROM " + table +
                        " WHERE created_at >= (CURDATE() - INTERVAL 6 DAY) GROUP BY DATE(created_at)");
                    foreach (DataRow r in dt.Rows)
                    {
                        if (r["d"] == DBNull.Value) continue;
                        int idx = (Convert.ToDateTime(r["d"]).Date - start).Days;
                        if (idx >= 0 && idx < 7) _trend[idx] += Convert.ToInt32(r["c"]);
                    }
                }
                catch { /* a transient DB hiccup just leaves that day at 0 */ }
            }
            pnlTrend.Invalidate();
        }

        private void pnlTrend_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle area = pnlTrend.ClientRectangle;

            int total = 0, max = 1;
            foreach (int v in _trend) { total += v; if (v > max) max = v; }

            if (total == 0)
            {
                using (var fnt = new Font("Segoe UI", 11F))
                using (var muted = new SolidBrush(Color.FromArgb(108, 117, 125)))
                    g.DrawString("No registrations in the last 7 days.", fnt, muted,
                        area, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                return;
            }

            int pad = 20, axis = 26;
            int n = _trend.Length;
            int plotH = area.Height - pad - axis;
            int slot = (area.Width - pad * 2) / n;
            int barW = (int)(slot * 0.58);

            using (var bar = new SolidBrush(Blue))
            using (var fntV = new Font("Segoe UI", 8.5F, FontStyle.Bold))
            using (var fntD = new Font("Segoe UI", 8.5F))
            using (var ink = new SolidBrush(Ink))
            using (var muted = new SolidBrush(Color.FromArgb(108, 117, 125)))
            {
                var fmt = new StringFormat { Alignment = StringAlignment.Center };
                for (int i = 0; i < n; i++)
                {
                    int val = _trend[i];
                    int h = (int)((double)val / max * (plotH - 22));
                    int slotX = pad + i * slot;
                    int x = slotX + (slot - barW) / 2;
                    int y = pad + (plotH - h);

                    g.FillRectangle(bar, x, y, barW, h);
                    g.DrawString(val.ToString(), fntV, ink, new RectangleF(slotX, y - 17, slot, 15), fmt);
                    g.DrawString(_trendLabels[i], fntD, muted, new RectangleF(slotX, area.Height - axis, slot, 16), fmt);
                }
            }
        }

        // ------------------------------------------------------------ clickable cards
        private void WireCard(Panel card)
        {
            card.Cursor = Cursors.Hand;
            card.Click += Card_Click;
            foreach (Control child in card.Controls)
            {
                child.Cursor = Cursors.Hand;
                child.Click += Card_Click;
            }
        }

        private void Card_Click(object sender, EventArgs e)
        {
            // The click may land on a child label — walk up to the card that carries the module key.
            Control c = sender as Control;
            while (c != null && !(c.Tag is string)) c = c.Parent;
            if (c != null && c.Tag is string key && key.Length > 0)
                Shell()?.GoToModule(key);
        }

        private MainForm Shell()
        {
            for (Control p = Parent; p != null; p = p.Parent)
                if (p is MainForm mf) return mf;
            return null;
        }

        // --------------------------------------------------------- service windows
        private void statusTimer_Tick(object sender, EventArgs e)
        {
            LoadWindowStatus();                              // Online/Offline is the fast-moving part
            if (_tickCount % 2 == 0) UpdateDevices();        // paired phones (~6s)
            if (++_tickCount % 4 == 0) { LoadKpis(); LoadTrend(); UpdateMobileConn(); }   // KPIs/trend/QR every ~12s
        }

        /// <summary>
        /// Fills the Service Windows panel with one row per active window, showing
        /// 🟢 Online (a user is signed in with a fresh heartbeat) or 🔴 Offline.
        /// </summary>
        private void LoadWindowStatus()
        {
            if (pnlWindows == null) return;

            DataTable dt;
            try
            {
                dt = Db.Pull(
                    "SELECT w.id, w.window_name, w.operator_name, w.is_priority, " +
                    "(w.current_operator IS NOT NULL AND w.last_heartbeat > (NOW() - INTERVAL " +
                    WindowAssignmentForm.StaleMinutes + " MINUTE)) AS online, " +
                    "(SELECT COUNT(*) FROM queue_tickets q WHERE q.window_no = w.id " +
                    "   AND q.status IN ('Accepted','Serving') AND DATE(q.created_at) = CURDATE()) AS busy, " +
                    "(SELECT GROUP_CONCAT(wt.service_code SEPARATOR ', ') FROM window_transactions wt " +
                    "   WHERE wt.window_id = w.id) AS services " +
                    "FROM windows w WHERE w.status = 'Active' ORDER BY w.display_order, w.id");
            }
            catch { return; }   // never let a transient DB hiccup crash the dashboard timer

            pnlWindows.SuspendLayout();
            pnlWindows.Controls.Clear();

            int y = 14;
            foreach (DataRow r in dt.Rows)
            {
                bool online = r["online"] != DBNull.Value && Convert.ToInt32(r["online"]) == 1;
                bool busy = r["busy"] != DBNull.Value && Convert.ToInt32(r["busy"]) > 0;
                bool priority = r["is_priority"] != DBNull.Value && Convert.ToInt32(r["is_priority"]) == 1;
                string op = r["operator_name"] != DBNull.Value ? r["operator_name"].ToString() : null;
                string services = r["services"] != DBNull.Value && r["services"].ToString().Length > 0
                    ? r["services"].ToString() : "All services";

                // Section 1 — show Online/Offline, Busy/Available, Priority, and assigned services.
                string state = online ? (busy ? "Online · Busy" : "Online · Available") : "Offline";
                var lbl = new Label
                {
                    AutoSize = true,
                    Location = new Point(18, y),
                    Font = new Font("Segoe UI", 11.5F),
                    ForeColor = online ? Ink : Color.FromArgb(108, 117, 125),
                    Text = (online ? (busy ? "🟠  " : "🟢  ") : "🔴  ") +
                           r["window_name"] + (priority ? "  ★" : "") + " — " + state +
                           (op != null ? "  (" + op + ")" : "")
                };
                pnlWindows.Controls.Add(lbl);
                y += 26;
                pnlWindows.Controls.Add(new Label
                {
                    AutoSize = true,
                    Location = new Point(40, y),
                    Font = new Font("Segoe UI", 8.5F),
                    ForeColor = Color.FromArgb(108, 117, 125),
                    Text = "Handles: " + (priority ? "All services (Priority)" : services)
                });
                y += 26;
            }

            if (dt.Rows.Count == 0)
                pnlWindows.Controls.Add(new Label
                {
                    AutoSize = true,
                    Location = new Point(18, y),
                    Font = new Font("Segoe UI", 11F),
                    ForeColor = Color.FromArgb(108, 117, 125),
                    Text = "No active windows. Add one in Service Windows (Administration)."
                });

            pnlWindows.ResumeLayout();
        }

        // scalar helpers (COUNT/SUM — not the id-returning Db.GetCount)
        private static int Scalar(string sql)
        {
            DataTable dt = Db.Pull(sql);
            return dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value ? Convert.ToInt32(dt.Rows[0][0]) : 0;
        }
        private static decimal ScalarDec(string sql)
        {
            DataTable dt = Db.Pull(sql);
            return dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0][0]) : 0m;
        }

        private void picMobileQr_Click(object sender, EventArgs e)
        {

        }
    }
}
