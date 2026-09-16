using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using CROMS.Analytics;
using CROMS.Analytics.Widgets;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Live operations dashboard for the front desk. Shows TODAY's actionable numbers —
    /// Waiting Now, Registered Today, Collections Today, Pending Releases — each on a card
    /// that (where it maps to a module) is clickable to jump straight there, plus a 7-day
    /// compact registration trend, the live Service Windows board, an actionable task list,
    /// and the most recent transactions.
    ///
    /// The layout lives in the Designer as nested TableLayoutPanels; this code-behind fills
    /// values, paints the trend and the list rows, and refreshes on a timer so the numbers stay
    /// live without leaving the screen. Embedded in the MainForm content panel.
    ///
    /// Every colour on this screen comes from <see cref="UiTheme"/>. The four Bootstrap literals
    /// that used to sit at the top of this file (Ink/Green/Red/Blue) are gone — they were the
    /// reason the dashboard read off-palette against every other module.
    /// </summary>
    public partial class DashboardForm : Form, IRefreshable
    {
        // Colour queue red once it crosses this many waiting clients (a simple threshold cue).
        private const int WaitingAlert = 10;

        // Birth / Marriage / Death, per day, for the last 7 days. Stacked, not summed: the
        // office reads "which register is busy", which one total per day cannot answer.
        private readonly int[,] _trend = new int[3, 7];
        private readonly string[] _trendLabels = new string[7];
        private int _tickCount;

        // Service Windows board: rows are rebuilt only when the window SET changes
        // (added/removed/renamed) rather than on every 3s tick — a tick just updates
        // each row's live status in place. Rebuilding (clear + recreate every ListRow,
        // each with its own Fonts + ToolTip) on every tick was what made the board
        // visibly flicker and cost more than the query itself.
        private string _windowsSig;
        private readonly Dictionary<int, ListRow> _windowRows = new Dictionary<int, ListRow>();

        private static readonly string[] TrendTables = { "births", "marriages", "deaths" };
        private static readonly string[] TrendNames = { "Birth", "Marriage", "Death" };
        private static readonly Color[] TrendColors = { UiTheme.Accent, UiTheme.Success, UiTheme.Faint };

        public DashboardForm()
        {
            InitializeComponent();

            // Cards that map to a module become clickable (per modern-dashboard guidance:
            // connect a metric to its action).
            WireCard(cardWaiting);      // → Queue Management
            WireCard(cardCollections);  // → Fees & Payments
            WireCard(cardPending);      // → Release & Claim

            lblTitle.Text = "Dashboard";
            RefreshData();
            statusTimer.Start();
        }

        public void RefreshData()
        {
            LoadKpis();
            LoadTrend();
            LoadWindowStatus();
            LoadAttention();
            LoadRecentTransactions();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        /// <summary>
        /// Windows are created, renamed and activated on the Settings module's Service Windows
        /// tab. Deliberately NOT <c>WindowAssignmentForm</c>, which is the login-flow dialog that
        /// CLAIMS a window for this operator and can end the session — a Dashboard button must
        /// not be able to log somebody out.
        /// </summary>
        private void btnAssignWindows_Click(object sender, EventArgs e)
        {
            Shell()?.GoToModule("settings");
        }

        private void lnkRecentAll_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Shell()?.GoToModule("transactions");
        }

        // ------------------------------------------------------------------ insights
        // Analytics widgets reused on the Dashboard at reduced size. These are the SAME
        // UserControls the Reports & Analytics tabs host — one implementation, two placements,
        // no chart code copied. SetCompact strips the basis line, caption and legend to fit.
        //
        // The widgets each run several aggregate queries, and RefreshData is called on every
        // navigation to the Dashboard AND every fourth status-timer tick (~12 s). Re-running
        // them at that rate is exactly the load the office LAN will not absorb, so they are
        // behind a TTL: at most once every 30 seconds no matter how often RefreshData fires.

        private static readonly TimeSpan InsightsTtl = TimeSpan.FromSeconds(30);

        private readonly System.Collections.Generic.List<AnalyticsWidget> _insights =
            new System.Collections.Generic.List<AnalyticsWidget>();
        private readonly System.Collections.Generic.List<CardPanel> _insightCards =
            new System.Collections.Generic.List<CardPanel>();
        private DateTime _insightsLoadedAt = DateTime.MinValue;

        private void SetupInsights()
        {
            // Deliberately NOT the queue/collection numbers the KPI cards above already show —
            // repeating them would waste the space. These answer questions the cards cannot:
            // what is going stale, which registry books to digitise, and how the wait is moving.
            AddInsight(new CertAgingWidget());
            AddInsight(new CertRegistryYearsWidget());
            AddInsight(new QueueWaitTrendWidget());
        }

        private void AddInsight(AnalyticsWidget w)
        {
            w.SetCompact(true);
            // The widget draws its own square hairline frame for the analytics tabs. Here it is
            // hosted inside a rounded CardPanel, so that second rectangle would show straight
            // through the rounded corners.
            w.DrawFrame = false;
            w.Dock = DockStyle.Fill;
            w.BackColor = UiTheme.Surface;
            w.Margin = new Padding(0);

            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, _insightCards.Count == 2 ? 0 : 14, 0),
                Padding = new Padding(2, 2, 2, 4),
                Height = AnalyticsWidget.CompactHeight + 6,
            };
            card.Controls.Add(w);

            _insights.Add(w);
            _insightCards.Add(card);
            pnlInsights.Controls.Add(card, _insightCards.Count - 1, 0);
        }

        /// <summary>
        /// Reloads the insight widgets, but only if the TTL has elapsed.
        /// <paramref name="force"/> is for a deliberate user-driven refresh, not for navigation.
        /// </summary>
        private void LoadInsights(bool force)
        {
            if (_insights.Count == 0) return;
            if (!force && DateTime.Now - _insightsLoadedAt < InsightsTtl) return;
            _insightsLoadedAt = DateTime.Now;

            // One range for the strip. This year is the useful window for all three: the aging
            // chart ignores the range by design (a backlog is measured from today), and the
            // other two are about the office's current behaviour, not its history.
            DateRange range = DateRange.ThisYear();

            bool any = false;
            for (int i = 0; i < _insights.Count; i++)
            {
                AnalyticsWidget w = _insights[i];
                try { w.Load(range); }
                catch { /* a widget contains its own failure; the strip must not go down with it */ }

                // This is what HasSufficientData is FOR: a widget with nothing to say is hidden
                // rather than shown as an empty panel among live ones.
                //
                // `any` is taken from HasSufficientData, NOT read back off w.Visible.
                // Control.Visible returns EFFECTIVE visibility — it is false while any parent
                // is still unshown — and this runs from the constructor, before the form is
                // shown. Reading it back there returned false for every widget and left the
                // heading permanently hidden above a strip of live charts.
                bool show = w.HasSufficientData;
                _insightCards[i].Visible = show;
                if (show) any = true;
            }

            // With nothing to show, the heading would label an empty strip.
            lblInsightsTitle.Visible = any;
            insightsHead.Visible = any;
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
        private Panel pnlMobileConn = null;
        private PictureBox picMobileQr = null;
        private Label lblMobileMsg = null;
        private Label lblMobileHint = null;
        private Label lblMobileUrl = null;
        private StatusPill pillMobile = null;
        private Label lblDevices = null;
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
                    SetMobileTone("Online" + (m.NetworkType.Length > 0 ? " · " + m.NetworkType : ""),
                                  UiTheme.SuccessTint, UiTheme.Success);
                    ShowRunningQrs(m);          // Wi-Fi QR (join) + App QR (open)
                    break;
                case IonicStatus.Starting:
                    SetMobileTone("Connecting…", UiTheme.WarningTint, UiTheme.Warning);
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
                        SetMobileTone("Online · main PC", UiTheme.SuccessTint, UiTheme.Success);
                        SetMobileQr(remote);
                        lblMobileHint.Text = "Connect the phone to the same Wi-Fi/hotspot, then scan.";
                        lblMobileHint.Visible = true;
                        lblMobileMsg.Visible = false;
                        HideQrExtras();
                    }
                    else
                    {
                        SetMobileTone("Offline", UiTheme.DangerTint, UiTheme.Danger);
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
                        lblClaimApp.ForeColor = UiTheme.Accent;
                        break;
                    case IonicStatus.Starting:
                        lblClaimApp.Text = "claimapp ● starting…";
                        lblClaimApp.ForeColor = UiTheme.Warning;
                        break;
                    default:
                        lblClaimApp.Text = "claimapp ○ offline";
                        lblClaimApp.ForeColor = UiTheme.Faint;
                        break;
                }
            }
            pnlMobileConn.Invalidate();
        }

        /// <summary>The mobile status pill. Replaces the old coloured ● label.</summary>
        private void SetMobileTone(string text, Color tint, Color ink)
        {
            pillMobile.SetTone(tint, ink);
            pillMobile.Text = text;
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
            lblWifiInfo = new Label
            {
                Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
                ForeColor = UiTheme.Accent,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "",
                Visible = false,
            };
            pnlMobileConn.Controls.Add(lblWifiInfo);

            // claimapp (ID-upload app) — second auto-started server, shown as a line.
            lblClaimApp = new Label
            {
                Font = new Font("Segoe UI", 8.25F),
                ForeColor = UiTheme.Faint,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "",
            };
            pnlMobileConn.Controls.Add(lblClaimApp);

            pnlMobileConn.Resize += (s, e) => LayoutMobilePanel();
            pnlMobileConn.Paint += pnlMobileConn_Paint;
            LayoutMobilePanel();
        }

        /// <summary>
        /// Centres the QR and stacks the text under it at whatever width the right column ends
        /// up. Everything here used to be hard-coded to a 271px panel, which is why the card
        /// could not be moved or resized.
        /// </summary>
        private void LayoutMobilePanel()
        {
            const int pad = 16, qr = 150;
            int cw = pnlMobileConn.ClientSize.Width - pad * 2;
            if (cw < 80) return;

            // Tight on purpose. The right column has to hold this card AND "Needs attention",
            // and a backlog the operator can act on outranks whitespace around a QR.
            picMobileQr.SetBounds(pad + (cw - qr) / 2, 6, qr, qr);
            lblMobileMsg.SetBounds(pad, 60, cw, 46);
            lblMobileUrl.SetBounds(pad, 158, cw, 18);
            lblMobileHint.SetBounds(pad, 177, cw, 26);
            if (lblWifiInfo != null) lblWifiInfo.SetBounds(pad, 203, cw, 14);
            if (lblDevices != null) lblDevices.SetBounds(pad, 218, cw, 34);
            if (lblClaimApp != null) lblClaimApp.SetBounds(pad, 253, cw, 14);
        }

        /// <summary>A rounded hairline frame around the QR, drawn behind the PictureBox.</summary>
        private void pnlMobileConn_Paint(object sender, PaintEventArgs e)
        {
            if (!picMobileQr.Visible) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = picMobileQr.Bounds;
            r.Inflate(4, 4);
            using (var path = CardPanel.RoundedRect(r, 8))
            using (var pen = new Pen(UiTheme.CardLine))
                e.Graphics.DrawPath(pen, path);
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
                lblMobileHint.Text = "Connect the phone to this network, then scan the QR."
                                   + SecurityWarningNote(m);
            }
            else
            {
                if (lblWifiInfo != null) lblWifiInfo.Visible = false;
                lblMobileHint.Text = "Scan with the phone, or type this address in the phone browser."
                                   + SecurityWarningNote(m);
            }
            SetMobileQr(m.QrPayload);
        }

        /// <summary>
        /// The scanner is served over HTTPS so the phone will allow its LIVE CAMERA —
        /// a browser refuses camera access on a plain http:// address. The certificate
        /// is self-signed for the office LAN, so the phone shows one warning the first
        /// time. Saying so HERE is the difference between a staff member tapping
        /// through it and a staff member deciding the app is broken.
        /// </summary>
        private static string SecurityWarningNote(IonicServerManager m)
        {
            return (m != null && m.Scheme == "https")
                ? "  The phone will warn once — tap Advanced, then Proceed."
                : "";
        }

        // ------------------------------------------------------------------ KPIs
        private void LoadKpis()
        {
            string who = Session.User != null
                ? (Session.User.Role ?? "") + (string.IsNullOrEmpty(Session.User.FullName)
                                                ? "" : " — " + Session.User.FullName)
                : "not signed in";
            lblToday.Text = DateTime.Now.ToString("dddd, d MMMM yyyy") + "  ·  " + who.TrimStart(' ', '—');

            pillUpdated.Text = "Updated " + DateTime.Now.ToString("HH:mm:ss");
            pillUpdated.SetTone(UiTheme.Surface, UiTheme.Muted);

            bool connected = Db.IsConnected();
            pillConnection.SetTone(connected ? UiTheme.SuccessTint : UiTheme.DangerTint,
                                   connected ? UiTheme.Success : UiTheme.Danger);
            pillConnection.Text = connected ? "Database connected" : "Database unavailable";
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

            cardWaiting.Value = waiting.ToString();
            cardWaiting.ValueColor = waiting > WaitingAlert ? UiTheme.Danger : UiTheme.Ink;   // threshold cue

            cardRegistered.Value = registered.ToString();

            // The peso sign and the centavos are drawn small on the SAME baseline as the digits,
            // so this card lines up with the other three instead of having to shrink to fit.
            cardCollections.Value = decimal.Truncate(collections).ToString("N0");
            cardCollections.Suffix = "." + ((int)((collections - decimal.Truncate(collections)) * 100)).ToString("00");

            cardPending.Value = pending.ToString();
            cardPending.ValueColor = pending > 0 ? UiTheme.Accent : UiTheme.Ink;              // something to hand over

            cardWaiting.Invalidate();
            cardRegistered.Invalidate();
            cardCollections.Invalidate();
            cardPending.Invalidate();
        }

        /// <summary>
        /// The four delta pills. Every one is computed against a real comparison basis, and a
        /// basis that does not exist HIDES the pill: on a registry dashboard an invented "0%"
        /// or a made-up trend is worse than an empty corner, because the operator cannot tell
        /// the difference between "unchanged" and "nothing to compare with".
        /// </summary>
        private void LoadDeltas(int waiting, int registered)
        {
            // --- 1. Waiting, against the same count two hours ago. A ticket counted as waiting
            // then is one issued before that moment and neither called nor completed by then.
            int basis = Scalar(
                "SELECT COUNT(*) FROM queue_tickets " +
                "WHERE DATE(created_at) = CURDATE() AND created_at <= (NOW() - INTERVAL 2 HOUR)");
            if (basis == 0) cardWaiting.HideDelta();     // office has not been open two hours
            else
            {
                int then = Scalar(
                    "SELECT COUNT(*) FROM queue_tickets " +
                    "WHERE DATE(created_at) = CURDATE() AND created_at <= (NOW() - INTERVAL 2 HOUR) " +
                    "  AND (called_at IS NULL OR called_at > (NOW() - INTERVAL 2 HOUR)) " +
                    "  AND (completed_at IS NULL OR completed_at > (NOW() - INTERVAL 2 HOUR))");
                int d = waiting - then;
                string at = DateTime.Now.AddHours(-2).ToString("HH:mm");
                cardWaiting.SetDelta((d > 0 ? "+" : "") + d + " vs " + at,
                    d > 0 ? UiTheme.WarningTint : d < 0 ? UiTheme.SuccessTint : UiTheme.Chrome,
                    d > 0 ? UiTheme.Warning : d < 0 ? UiTheme.Success : UiTheme.Muted);
            }

            // --- 2. Registered, against yesterday. Zero yesterday is not a 100% rise, it is no
            // denominator, so the pill goes away.
            int yesterday = Scalar(
                "SELECT (SELECT COUNT(*) FROM births    WHERE DATE(created_at) = CURDATE() - INTERVAL 1 DAY) + " +
                "       (SELECT COUNT(*) FROM marriages WHERE DATE(created_at) = CURDATE() - INTERVAL 1 DAY) + " +
                "       (SELECT COUNT(*) FROM deaths    WHERE DATE(created_at) = CURDATE() - INTERVAL 1 DAY)");
            if (yesterday == 0) cardRegistered.HideDelta();
            else
            {
                int pct = (int)Math.Round((registered - yesterday) * 100.0 / yesterday);
                bool up = pct >= 0;
                cardRegistered.SetDelta((up ? "▲ " : "▼ ") + Math.Abs(pct) + "%",
                    // A fall in registrations is information, not an alarm — the office does not
                    // control how many births happen — so it is neutral rather than red.
                    up ? UiTheme.SuccessTint : UiTheme.Chrome,
                    up ? UiTheme.Success : UiTheme.Muted);
            }

            // --- 3. Collections: how many official receipts make up the figure.
            int receipts = Scalar(
                "SELECT COUNT(*) FROM payments " +
                "WHERE DATE(paid_at) = CURDATE() AND or_number IS NOT NULL AND or_number <> ''");
            if (receipts == 0) cardCollections.HideDelta();
            else cardCollections.SetDelta(receipts + (receipts == 1 ? " receipt" : " receipts"),
                                          UiTheme.Chrome, UiTheme.Muted);

            // --- 4. Pending releases sitting more than three days.
            int stale = Scalar(
                "SELECT COUNT(*) FROM transactions " +
                "WHERE status = 'ForRelease' AND updated_at < (NOW() - INTERVAL 3 DAY)");
            if (stale == 0) cardPending.HideDelta();
            else cardPending.SetDelta(stale + " over 3 days", UiTheme.DangerTint, UiTheme.Danger);
        }

        /// <summary>
        /// The four sparklines. Each is a real series over the last 8 days (or hours), never a
        /// decorative shape: an all-zero series draws nothing at all rather than a flat line
        /// that would read as a measured result.
        /// </summary>
        private void LoadSparks()
        {
            // Queue arrivals per hour for the last 8 hours of today.
            cardWaiting.SetSpark(Series(
                "SELECT HOUR(created_at) AS k, COUNT(*) AS n FROM queue_tickets " +
                "WHERE DATE(created_at) = CURDATE() GROUP BY k",
                8, i => DateTime.Now.AddHours(-7 + i).Hour));

            // Registrations per day for the last 8 days.
            cardRegistered.SetSpark(Series(
                "SELECT DATEDIFF(CURDATE(), DATE(created_at)) AS k, COUNT(*) AS n FROM (" +
                "  SELECT created_at FROM births    UNION ALL " +
                "  SELECT created_at FROM marriages UNION ALL " +
                "  SELECT created_at FROM deaths) t " +
                "WHERE created_at >= (CURDATE() - INTERVAL 7 DAY) GROUP BY k",
                8, i => 7 - i));

            // Collections per day for the last 8 days.
            cardCollections.SetSpark(Series(
                "SELECT DATEDIFF(CURDATE(), DATE(paid_at)) AS k, SUM(net_amount) AS n FROM payments " +
                "WHERE paid_at >= (CURDATE() - INTERVAL 7 DAY) GROUP BY k",
                8, i => 7 - i));

            // The AGE PROFILE of the release backlog, by the day each transaction last moved.
            // How many were pending on a past day is not recoverable — the table stores a status,
            // not its history — so this is the honest series, not a reconstruction.
            cardPending.SetSpark(Series(
                "SELECT DATEDIFF(CURDATE(), DATE(updated_at)) AS k, COUNT(*) AS n FROM transactions " +
                "WHERE status = 'ForRelease' AND updated_at >= (CURDATE() - INTERVAL 7 DAY) GROUP BY k",
                8, i => 7 - i));
        }

        /// <summary>
        /// Runs a "key, value" aggregate and lays it onto <paramref name="slots"/> buckets, so a
        /// day or hour with no rows is a real zero rather than a missing point.
        /// </summary>
        private static double[] Series(string sql, int slots, Func<int, int> keyForSlot)
        {
            var outp = new double[slots];
            DataTable dt;
            try { dt = Db.Pull(sql); }
            catch { return outp; }

            var map = new System.Collections.Generic.Dictionary<int, double>();
            foreach (DataRow r in dt.Rows)
            {
                if (r["k"] == DBNull.Value || r["n"] == DBNull.Value) continue;
                map[Convert.ToInt32(r["k"])] = Convert.ToDouble(r["n"]);
            }
            for (int i = 0; i < slots; i++)
            {
                double v;
                if (map.TryGetValue(keyForSlot(i), out v)) outp[i] = v;
            }
            return outp;
        }

        // ---------------------------------------------------------------- trend
        /// <summary>Counts registrations per day for the last 7 days, kept per register.</summary>
        private void LoadTrend()
        {
            DateTime start = DateTime.Today.AddDays(-6);
            for (int i = 0; i < 7; i++)
            {
                _trendLabels[i] = start.AddDays(i).ToString("ddd d");
                for (int s = 0; s < 3; s++) _trend[s, i] = 0;
            }

            for (int s = 0; s < TrendTables.Length; s++)                // whitelist, not user input
            {
                try
                {
                    DataTable dt = Db.Pull(
                        "SELECT DATE(created_at) AS d, COUNT(*) AS c FROM " + TrendTables[s] +
                        " WHERE created_at >= (CURDATE() - INTERVAL 6 DAY) GROUP BY DATE(created_at)");
                    foreach (DataRow r in dt.Rows)
                    {
                        if (r["d"] == DBNull.Value) continue;
                        int idx = (Convert.ToDateTime(r["d"]).Date - start).Days;
                        if (idx >= 0 && idx < 7) _trend[s, idx] += Convert.ToInt32(r["c"]);
                    }
                }
                catch { /* a transient DB hiccup just leaves that day at 0 */ }
            }
            pnlTrend.Invalidate();
        }

        private void pnlTrendLegend_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var f = new Font("Segoe UI", 8.25F))
            {
                int x = 0;
                for (int s = 0; s < 3; s++)
                {
                    var sw = new Rectangle(x, (pnlTrendLegend.Height - 9) / 2, 9, 9);
                    using (var path = CardPanel.RoundedRect(sw, 2))
                    using (var b = new SolidBrush(TrendColors[s]))
                        g.FillPath(b, path);
                    x += 9 + 5;
                    Size m = TextRenderer.MeasureText(g, TrendNames[s], f,
                        new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(g, TrendNames[s], f,
                        new Rectangle(x, 0, m.Width + 2, pnlTrendLegend.Height), UiTheme.Muted,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    x += m.Width + 14;
                }
            }
        }

        private void pnlTrend_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle area = Rectangle.FromLTRB(
                pnlTrend.Padding.Left, pnlTrend.Padding.Top,
                pnlTrend.ClientSize.Width - pnlTrend.Padding.Right,
                pnlTrend.ClientSize.Height - pnlTrend.Padding.Bottom);
            if (area.Width < 80 || area.Height < 60) return;

            int n = 7, max = 0;
            var totals = new int[n];
            for (int i = 0; i < n; i++)
            {
                totals[i] = _trend[0, i] + _trend[1, i] + _trend[2, i];
                if (totals[i] > max) max = totals[i];
            }

            if (max == 0)
            {
                using (var fnt = new Font("Segoe UI", 9.5F))
                using (var faint = new SolidBrush(UiTheme.Faint))
                    g.DrawString("No registrations in the last 7 days.", fnt, faint, area,
                        new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                return;
            }

            // A whole-number axis: on a registry where a day holds two records, a gridline
            // labelled "1.5" is a label that cannot correspond to anything.
            int step = max <= 4 ? 1 : (int)Math.Ceiling(max / 4.0);
            int top = ((max + step - 1) / step) * step;

            const int gutter = 30, axisH = 20;
            var plot = Rectangle.FromLTRB(area.X + gutter, area.Y, area.Right, area.Bottom - axisH);
            if (plot.Height < 30) return;

            using (var grid = new Pen(UiTheme.Mix(UiTheme.CardLine, UiTheme.Surface, 0.4f)))
            using (var axisFont = new Font("Segoe UI", 8.25F))
            {
                for (int v = 0; v <= top; v += step)
                {
                    int y = plot.Bottom - (int)Math.Round((double)v / top * (plot.Height - 4));
                    g.DrawLine(grid, plot.X, y, plot.Right, y);         // horizontal only, no border
                    TextRenderer.DrawText(g, v.ToString(), axisFont,
                        new Rectangle(area.X, y - 8, gutter - 8, 16), UiTheme.Faint,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }

            // 46px columns at a 90px pitch is the reference; both shrink together on a narrower
            // monitor so seven columns always fit rather than overlapping.
            float pitch = Math.Min(90f, plot.Width / (float)n);
            float barW = Math.Min(46f, pitch * 0.51f);
            float x0 = plot.X + (plot.Width - pitch * n) / 2f;

            using (var xFont = new Font("Segoe UI", 8.25F))
                for (int i = 0; i < n; i++)
                {
                    float cx = x0 + pitch * i + pitch / 2f;
                    float bottom = plot.Bottom;

                    for (int s = 0; s < 3; s++)
                    {
                        if (_trend[s, i] == 0) continue;
                        float h = (float)_trend[s, i] / top * (plot.Height - 4);
                        var seg = new RectangleF(cx - barW / 2f, bottom - h, barW, h);
                        using (var path = CardPanel.RoundedRect(Rectangle.Round(seg), 3))
                        using (var b = new SolidBrush(TrendColors[s]))
                            g.FillPath(b, path);
                        bottom -= h;
                    }

                    TextRenderer.DrawText(g, _trendLabels[i], xFont,
                        new Rectangle((int)(cx - pitch / 2f), plot.Bottom + 4, (int)pitch, axisH - 4),
                        UiTheme.Muted,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding);
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
            if (++_tickCount % 4 == 0)
            {
                LoadKpis();
                LoadTrend();
                LoadAttention();
                LoadRecentTransactions();
            }
        }

        /// <summary>
        /// Fills the Service Windows panel with one row per active window: number badge, window
        /// and staff name, the ticket it is on, and a Serving / Idle / Closed badge.
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

            // The ticket CODE is not in the query above, and that query is left byte-identical —
            // so the codes come from their own small read-only lookup rather than by editing it.
            var onWindow = new Dictionary<int, string>();
            try
            {
                DataTable tk = Db.Pull(
                    "SELECT window_no, ticket_code FROM queue_tickets " +
                    "WHERE DATE(created_at) = CURDATE() AND window_no IS NOT NULL " +
                    "  AND status IN ('Accepted','Serving') ORDER BY id");
                foreach (DataRow r in tk.Rows)
                {
                    if (r["window_no"] == DBNull.Value || r["ticket_code"] == DBNull.Value) continue;
                    onWindow[Convert.ToInt32(r["window_no"])] = r["ticket_code"].ToString();
                }
            }
            catch { /* no codes; the rows simply show an em-dash */ }

            // Rebuild only when the shown-window SET changes (added/removed/renamed);
            // an ordinary tick just updates each row's live status in place below.
            var sig = new StringBuilder();
            foreach (DataRow r in dt.Rows)
                sig.Append(r["id"]).Append(':').Append(r["window_name"]).Append('|');
            string sigStr = sig.ToString();
            if (sigStr != _windowsSig)
            {
                RebuildWindowRows(dt);
                _windowsSig = sigStr;
            }

            UpdateWindowRows(dt, onWindow);
        }

        private void RebuildWindowRows(DataTable dt)
        {
            pnlWindows.SuspendLayout();
            ClearRows(pnlWindows);
            _windowRows.Clear();

            bool first = true;
            for (int i = dt.Rows.Count - 1; i >= 0; i--)     // Dock=Top stacks in reverse
            {
                int id = Convert.ToInt32(dt.Rows[i]["id"]);
                var row = new ListRow
                {
                    Dock = DockStyle.Top,
                    Height = 54,
                    BadgeText = id.ToString(),
                    BadgeTint = UiTheme.AccentTint,
                    BadgeInk = UiTheme.Accent,
                    ShowTicket = true,
                    Separator = i > 0,
                };
                pnlWindows.Controls.Add(row);
                _windowRows[id] = row;
                first = false;
            }

            if (first)
                pnlWindows.Controls.Add(EmptyLine("No active windows. Add one in Settings → Service Windows."));

            pnlWindows.ResumeLayout();
        }

        private void UpdateWindowRows(DataTable dt, Dictionary<int, string> onWindow)
        {
            foreach (DataRow r in dt.Rows)
            {
                int id = Convert.ToInt32(r["id"]);
                ListRow row;
                if (!_windowRows.TryGetValue(id, out row)) continue;

                bool online = r["online"] != DBNull.Value && Convert.ToInt32(r["online"]) == 1;
                bool busy = r["busy"] != DBNull.Value && Convert.ToInt32(r["busy"]) > 0;
                bool priority = r["is_priority"] != DBNull.Value && Convert.ToInt32(r["is_priority"]) == 1;
                string op = r["operator_name"] != DBNull.Value && r["operator_name"].ToString().Length > 0
                    ? r["operator_name"].ToString() : null;
                string services = r["services"] != DBNull.Value && r["services"].ToString().Length > 0
                    ? r["services"].ToString() : "All services";
                string ticket = onWindow.ContainsKey(id) ? onWindow[id] : null;

                row.Title = r["window_name"] + (priority ? "  ★" : "");
                row.Subtitle = op ?? "unassigned";
                row.Ticket = ticket;
                row.SetStatus(online ? (busy ? "Serving" : "Idle") : "Closed",
                    online ? (busy ? UiTheme.SuccessTint : UiTheme.Chrome) : UiTheme.WarningTint,
                    online ? (busy ? UiTheme.Success : UiTheme.Muted) : UiTheme.Warning);
                row.TitleTip = "Handles: " + (priority ? "All services (Priority)" : services);
                row.Invalidate();
            }
        }

        // ------------------------------------------------------- needs attention
        /// <summary>
        /// The workflow backlogs an operator can act on now. A row whose count is 0 is removed,
        /// not rendered as "0"; the card stays a short task list instead of becoming another
        /// statistics panel.
        /// </summary>
        private void LoadAttention()
        {
            if (pnlAttention == null) return;
            if (!Db.IsConnected())
            {
                lblAttentionBasis.Text = "Unavailable";
                return;
            }

            int readyLicenses = 0, expiringLicenses = 0, missingRequirements = 0;
            int pendingReleases = 0, staleReleases = 0, pendingPsa = 0, delayedPostings = 0;

            try { readyLicenses = Scalar("SELECT COUNT(*) FROM marriage_licenses WHERE status='Posting' AND earliest_issue_date <= CURDATE()"); }
            catch { }
            try { expiringLicenses = Scalar("SELECT COUNT(*) FROM marriage_licenses WHERE status='Issued' AND expiry_date BETWEEN CURDATE() AND (CURDATE() + INTERVAL 7 DAY)"); }
            catch { }
            try
            {
                missingRequirements = Scalar(
                    "SELECT COUNT(DISTINCT owner_id) FROM marriage_requirements " +
                    "WHERE owner_type='License' AND status IN ('Missing','Rejected')");
            }
            catch { }
            try { pendingReleases = Scalar("SELECT COUNT(*) FROM transactions WHERE status='ForRelease'"); }
            catch { }
            try { staleReleases = Scalar("SELECT COUNT(*) FROM transactions WHERE status='ForRelease' AND updated_at < (NOW() - INTERVAL 3 DAY)"); }
            catch { }
            try
            {
                pendingPsa = Scalar(
                    "SELECT COUNT(*) FROM marriages m WHERE m.status='Registered' AND NOT EXISTS (" +
                    "SELECT 1 FROM psa_transmittal_items i WHERE i.record_table='marriages' AND i.record_id=m.id)");
            }
            catch { }
            try
            {
                delayedPostings = Scalar(
                    "SELECT COUNT(*) FROM births WHERE delayed_posting_start IS NOT NULL " +
                    "AND delayed_posting_end >= CURDATE() AND delayed_evaluation_at IS NULL");
            }
            catch { }

            pnlAttention.SuspendLayout();
            ClearRows(pnlAttention);

            var rows = new List<ListRow>();
            if (readyLicenses > 0)
                rows.Add(TaskRow("✓", UiTheme.SuccessTint, UiTheme.Success,
                    readyLicenses + (readyLicenses == 1 ? " marriage license ready to issue" : " marriage licenses ready to issue"),
                    "Posting complete — no license issued yet", "Review", "marriage"));
            if (expiringLicenses > 0)
                rows.Add(TaskRow("!", UiTheme.WarningTint, UiTheme.Warning,
                    expiringLicenses + (expiringLicenses == 1 ? " marriage license expires within 7 days" : " marriage licenses expire within 7 days"),
                    "120-day validity period ending soon", "View list", "marriage"));
            if (missingRequirements > 0)
                rows.Add(TaskRow("×", UiTheme.DangerTint, UiTheme.Danger,
                    missingRequirements + (missingRequirements == 1 ? " application has missing requirements" : " applications have missing requirements"),
                    "Required supporting documents need review", "Open", "marriage"));
            if (pendingReleases > 0)
                rows.Add(TaskRow("!", UiTheme.DangerTint, UiTheme.Danger,
                    pendingReleases + (pendingReleases == 1 ? " document ready for release" : " documents ready for release") +
                    (staleReleases > 0 ? ", " + staleReleases + " unclaimed 3+ days" : ""),
                    "Certificate requests awaiting claimant", "Open", "release"));
            if (pendingPsa > 0)
                rows.Add(TaskRow("⇧", UiTheme.AccentTint, UiTheme.Accent,
                    pendingPsa + (pendingPsa == 1 ? " registered marriage pending PSA transmittal" : " registered marriages pending PSA transmittal"),
                    "Registered in CROMS — endorsement not yet batched", "Open", "marriage"));
            if (delayedPostings > 0)
                rows.Add(TaskRow("◐", UiTheme.AccentTint, UiTheme.Accent,
                    delayedPostings + (delayedPostings == 1 ? " delayed birth registration in posting" : " delayed birth registrations in posting"),
                    "Public posting period is still underway", "Open", "birth"));

            lblAttentionBasis.Text = rows.Count + (rows.Count == 1 ? " item" : " items");

            if (rows.Count == 0)
                pnlAttention.Controls.Add(EmptyLine("Nothing outstanding — you're caught up"));
            else
                for (int i = rows.Count - 1; i >= 0; i--)      // Dock=Top stacks in reverse
                {
                    rows[i].Dock = DockStyle.Top;
                    rows[i].Height = 58;
                    rows[i].Separator = i > 0;
                    pnlAttention.Controls.Add(rows[i]);
                }

            pnlAttention.ResumeLayout();
        }

        private ListRow TaskRow(string badge, Color tint, Color ink, string title,
            string subtitle, string action, string moduleKey)
        {
            var row = new ListRow
            {
                BadgeText = badge,
                BadgeTint = tint,
                BadgeInk = ink,
                Title = title,
                Subtitle = subtitle,
                Tag = moduleKey,
            };
            row.SetStatus(action, UiTheme.Chrome, UiTheme.Ink);
            WireCard(row);
            return row;
        }

        // --------------------------------------------------- recent transactions
        /// <summary>
        /// Shows the four most recently changed client transactions. This is deliberately a
        /// short dispatch list; the full searchable ledger remains in Transactions.
        /// </summary>
        private void LoadRecentTransactions()
        {
            if (pnlRecent == null) return;

            pnlRecent.SuspendLayout();
            ClearRows(pnlRecent);

            if (!Db.IsConnected())
            {
                pnlRecent.Controls.Add(EmptyLine("Recent transactions are unavailable."));
                pnlRecent.ResumeLayout();
                return;
            }

            DataTable dt;
            try
            {
                dt = Db.Pull(
                    "SELECT txn_code, client_name, type, status, updated_at " +
                    "FROM transactions ORDER BY updated_at DESC, id DESC LIMIT 4");
            }
            catch
            {
                pnlRecent.Controls.Add(EmptyLine("Recent transactions are unavailable."));
                pnlRecent.ResumeLayout();
                return;
            }

            if (dt.Rows.Count == 0)
            {
                pnlRecent.Controls.Add(EmptyLine("No transactions recorded yet."));
                pnlRecent.ResumeLayout();
                return;
            }

            var rows = new List<ListRow>();
            foreach (DataRow r in dt.Rows)
            {
                string status = Convert.ToString(r["status"]);
                string type = Convert.ToString(r["type"]);
                string code = Convert.ToString(r["txn_code"]);
                string client = Convert.ToString(r["client_name"]);
                DateTime changed = r["updated_at"] == DBNull.Value
                    ? DateTime.Now : Convert.ToDateTime(r["updated_at"]);

                Color tint = UiTheme.Chrome;
                Color ink = UiTheme.Muted;
                string glyph = "•";
                if (status == "Released") { tint = UiTheme.SuccessTint; ink = UiTheme.Success; glyph = "✓"; }
                else if (status == "ForRelease") { tint = UiTheme.DangerTint; ink = UiTheme.Danger; glyph = "!"; }
                else if (status == "ForPayment") { tint = UiTheme.WarningTint; ink = UiTheme.Warning; glyph = "₱"; }
                else if (status == "Processing") { tint = UiTheme.AccentTint; ink = UiTheme.Accent; glyph = "…"; }

                var row = new ListRow
                {
                    BadgeText = glyph,
                    BadgeTint = tint,
                    BadgeInk = ink,
                    Title = code + " — " + type + (client.Length > 0 ? ", " + client : ""),
                    Subtitle = FriendlyTransactionStatus(status),
                    Tag = "transactions",
                };
                row.SetStatus(changed.ToString("h:mm tt"), UiTheme.Surface, UiTheme.Faint);
                WireCard(row);
                rows.Add(row);
            }

            for (int i = rows.Count - 1; i >= 0; i--)
            {
                rows[i].Dock = DockStyle.Top;
                rows[i].Height = 44;
                rows[i].Separator = i > 0;
                pnlRecent.Controls.Add(rows[i]);
            }
            pnlRecent.ResumeLayout();
        }

        private static string FriendlyTransactionStatus(string status)
        {
            switch (status)
            {
                case "ForPayment": return "Ready for payment";
                case "ForRelease": return "Ready for release";
                case "Released": return "Released to claimant";
                case "Processing": return "In progress";
                case "Cancelled": return "Cancelled";
                default: return string.IsNullOrWhiteSpace(status) ? "Status unavailable" : status;
            }
        }

        // ------------------------------------------------------------------ rows
        private static void ClearRows(Panel host)
        {
            var old = new Control[host.Controls.Count];
            host.Controls.CopyTo(old, 0);
            host.Controls.Clear();
            foreach (Control c in old) c.Dispose();
        }

        private static Label EmptyLine(string text)
        {
            return new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(16, 0, 16, 0),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = UiTheme.Faint,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = text,
            };
        }

        /// <summary>
        /// One row of a card list: a rounded number badge, a two-line name block, an optional
        /// monospace ticket code and an optional status badge. Painted rather than assembled
        /// from Labels so the four elements keep their columns whatever the card's width is —
        /// the old absolute-positioned Labels wrapped into each other as soon as a window name
        /// grew.
        /// </summary>
        private sealed class ListRow : Panel
        {
            private readonly Font _title = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            private readonly Font _sub = new Font("Segoe UI", 8.25F);
            private readonly Font _badge = new Font("Segoe UI", 9F, FontStyle.Bold);
            private readonly Font _ticket = new Font("Consolas", 10.5F, FontStyle.Bold);
            private readonly Font _status = new Font("Segoe UI", 8F, FontStyle.Bold);
            private readonly ToolTip _tip = new ToolTip();

            public string BadgeText = "";
            public Color BadgeTint = UiTheme.AccentTint;
            public Color BadgeInk = UiTheme.Accent;
            public string Title = "";
            public string Subtitle = "";
            public string Ticket;
            public bool ShowTicket;
            public bool Separator = true;

            private string _statusText;
            private Color _statusTint, _statusInk;

            public ListRow()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                       | ControlStyles.OptimizedDoubleBuffer
                       | ControlStyles.SupportsTransparentBackColor, true);
                BackColor = Color.Transparent;
            }

            public void SetStatus(string text, Color tint, Color ink)
            {
                _statusText = text; _statusTint = tint; _statusInk = ink;
            }

            public string TitleTip
            {
                set { if (!string.IsNullOrEmpty(value)) _tip.SetToolTip(this, value); }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                if (Separator)
                    using (var p = new Pen(UiTheme.RowLine))
                        g.DrawLine(p, 16, 0, Width - 16, 0);

                // badge
                var badge = new Rectangle(16, (Height - 28) / 2, 28, 28);
                using (var path = CardPanel.RoundedRect(badge, 8))
                using (var b = new SolidBrush(BadgeTint))
                    g.FillPath(b, path);
                TextRenderer.DrawText(g, BadgeText, _badge, badge, BadgeInk,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                int right = Width - 16;

                // status badge, right-most
                if (!string.IsNullOrEmpty(_statusText))
                {
                    Size m = TextRenderer.MeasureText(g, _statusText, _status,
                        new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                    var pill = new Rectangle(right - (m.Width + 18), (Height - (m.Height + 6)) / 2,
                                             m.Width + 18, m.Height + 6);
                    using (var path = CardPanel.Pill(pill))
                    using (var b = new SolidBrush(_statusTint))
                        g.FillPath(b, path);
                    TextRenderer.DrawText(g, _statusText, _status, pill, _statusInk,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    right = pill.X - 12;
                }

                // current ticket, or an em-dash when the window is idle
                if (ShowTicket)
                {
                    string t = string.IsNullOrEmpty(Ticket) ? "—" : Ticket;
                    Size m = TextRenderer.MeasureText(g, t, _ticket,
                        new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                    var box = new Rectangle(right - m.Width, 0, m.Width, Height);
                    TextRenderer.DrawText(g, t, _ticket, box,
                        string.IsNullOrEmpty(Ticket) ? UiTheme.Faint : UiTheme.Ink,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    right = box.X - 12;
                }

                // name block fills what is left
                int nameX = badge.Right + 12;
                int nameW = right - nameX;
                if (nameW < 40) return;

                int th = TextRenderer.MeasureText(g, "Hg", _title, new Size(int.MaxValue, int.MaxValue),
                                                  TextFormatFlags.NoPadding).Height;
                int sh = TextRenderer.MeasureText(g, "Hg", _sub, new Size(int.MaxValue, int.MaxValue),
                                                  TextFormatFlags.NoPadding).Height;
                int y = (Height - (th + 1 + sh)) / 2;

                TextRenderer.DrawText(g, Title ?? "", _title, new Rectangle(nameX, y, nameW, th),
                    UiTheme.Ink, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(g, Subtitle ?? "", _sub, new Rectangle(nameX, y + th + 1, nameW, sh),
                    UiTheme.Faint, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _title.Dispose(); _sub.Dispose(); _badge.Dispose();
                    _ticket.Dispose(); _status.Dispose(); _tip.Dispose();
                }
                base.Dispose(disposing);
            }
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
