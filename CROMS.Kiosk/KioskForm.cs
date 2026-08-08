using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using MySql.Data.MySqlClient;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Client self-service kiosk — a guided 2-part wizard (spec section 3): the old
    /// separate "take picture" step is folded into Part 2, so there are only two parts.
    ///
    ///   Part 1  Choose service(s)      — big cards, tap one or more.
    ///   Part 2  Your details &amp; photo   — name + optional contact + priority lane, PLUS the
    ///                                    live webcam capture, all on one screen; the Print
    ///                                    button issues one queue ticket.
    ///
    /// A persistent header shows "Step X of 2"; a footer gives Back / Next (and Print on the
    /// last step). If the client walks away, an idle timer clears every field and returns to
    /// Part 1 so the next person starts fresh (and never sees the previous client's name/photo).
    ///
    /// The database result is unchanged from the previous single-screen version: printing
    /// creates ONE queue_tickets row (one queue number, photo in id_image) plus one
    /// queue_ticket_services row per selected service, so the staff app processes every
    /// requested service from the same ticket.
    /// </summary>
    public class KioskForm : Form, IMessageFilter
    {
        /// <summary>One selectable service. code + label are stored per ticket.</summary>
        private sealed class Service
        {
            public string Code;
            public string Label;
            public string Glyph;
            public Service(string code, string label, string glyph)
            {
                Code = code; Label = label; Glyph = glyph;
            }
        }

        // The service catalogue. Add a future service here — the card grid builds itself.
        private static readonly Service[] Catalogue =
        {
            new Service("NEWREG",   "New Registration",           "📝"),
            new Service("CTC",      "Certified True Copy (CTC)",  "📄"),
            new Service("MARRIAGE", "Marriage Certificate",       "💍"),
            new Service("DEATH",    "Death Certificate",          "🕊"),
            new Service("PETITION", "Petition (Correction)",      "⚖"),
            new Service("VERIFY",   "Verification / Others",      "🔎"),
            new Service("CLAIM",    "Release & Claim (Pick-up)",  "📥"),
        };

        // Palette
        private static readonly Color Bg        = Color.FromArgb(241, 245, 249);
        private static readonly Color CardBg    = Color.White;
        private static readonly Color CardSelBg = Color.FromArgb(232, 240, 254);
        private static readonly Color Accent    = Color.FromArgb(13, 110, 253);
        private static readonly Color Ink       = Color.FromArgb(33, 37, 41);
        private static readonly Color Muted     = Color.FromArgb(108, 117, 125);
        private static readonly Color Line      = Color.FromArgb(222, 226, 230);

        // How many steps, and their short titles (shown in the header).
        private static readonly string[] StepTitles =
        {
            "Choose your service",
            "Your details & photo",
        };

        // Idle handling — clear everything and return to Step 1 after this long with no input.
        private const int IdleSeconds = 90;

        // Selected service codes, in the order the client tapped them.
        private readonly List<string> _selected = new List<string>();
        private readonly Dictionary<string, Panel> _cards = new Dictionary<string, Panel>();

        // Wizard chrome.
        private int _step;
        private readonly Panel[] _stepPanels = new Panel[StepTitles.Length];
        private Panel _host;
        private Label _lblStep;
        private StepIndicator _stepInd;
        private Button _btnBack, _btnNext;
        private Timer _idle;

        // The step content is laid out at a fixed design size; on a smaller laptop it
        // is scaled down once (fields below) so everything fits on screen without a
        // scrollbar. Kept as fields so the fit + re-centre logic can reach them.
        private FlowLayoutPanel _svcGrid;
        private Label _svcHint;
        private Panel _detailsBox;
        private bool _fitApplied;
        // Design size the step content is laid out for. Height covers the tallest step —
        // step 1's 7 service cards (3 rows) + hint + centring margins — so a shorter or
        // display-scaled laptop shrinks enough that the last card isn't cut off.
        private const float DesignW = 1200f, DesignH = 820f;

        // Office-availability gate: the kiosk is only usable while at least one service
        // window is online (an operator signed in with a fresh heartbeat). When none is
        // online, a full-screen overlay blocks the wizard.
        private Panel _offlineOverlay;
        private Timer _availability;
        private const int OfficeStaleMinutes = 2;   // matches the staff app's window heartbeat window

        // Step 2 inputs.
        private TextBox _txtFirst, _txtMiddle, _txtLast, _txtContact;
        private PillToggle _priSenior, _priPwd, _priPregnant;
        // Claim ticket — shown only when the Claim / Pickup service is selected. The client
        // types the claim ticket number they received when they requested (blank = a new
        // claim request, which generates a fresh QR).
        private Panel _claimPanel;
        private TextBox _txtClaimTicket;

        // Step 3 live webcam.
        private PictureBox _picCam;
        private Label _lblCamStatus;
        private Label _lblCamState;   // 🟢 Camera Ready / 🔴 Camera Not Detected
        private Label _lblLive;       // "● LIVE" badge over the preview
        private Button _btnCapture;   // dynamic text (Capture / Retake / Captured)
        private byte[] _idBytes;
        private VideoCaptureDevice _camera;
        private Bitmap _lastFrame;
        private readonly object _frameLock = new object();

        // Right-card claimapp QR — shown (in place of the webcam) when the client picks
        // Release & Claim (Pick-up), so they can scan it with claimapp to upload their ID.
        private Panel _camFrame;      // the webcam preview frame (hidden for a claim)
        private Label _rightTitle, _rightHint;
        private Panel _claimQrPanel;  // the QR block (hidden for a normal request)
        private PictureBox _picClaimQr;
        private Label _lblClaimQrCap;
        private Label _lblClaimUrl;   // camera-free fallback: base URL + ticket to type by hand
        private string _claimQrToken; // lazily-created claim token behind the on-screen QR
        private string _claimQrNo;    // its claim ticket number (CLM-YYYY-####)

        public KioskForm()
        {
            Text = "CROMS — Client Kiosk";
            WindowState = FormWindowState.Maximized;
            BackColor = Bg;
            Font = new Font("Segoe UI", 10F);
            BuildUi();
            BuildOfflineOverlay();
            Load += (s, e) => { StartCamera(); ShowStep(0); UpdateAvailability(); };
            // Once the form is maximized and laid out, shrink the fixed-size step
            // content to fit smaller laptop screens (no-op on large screens).
            Shown += (s, e) => FitToScreen();

            // Poll the office status so the kiosk locks/unlocks itself as windows go
            // online or offline, without needing a restart.
            _availability = new Timer { Interval = 4000 };
            _availability.Tick += (s, e) => UpdateAvailability();
            _availability.Start();

            // Idle auto-reset.
            _idle = new Timer { Interval = 1000 };
            int idleTicks = 0;
            _idle.Tick += (s, e) =>
            {
                idleTicks++;
                if (idleTicks >= IdleSeconds) { idleTicks = 0; if (HasProgress()) ResetForm(); }
            };
            _resetIdle = () => idleTicks = 0;
            _idle.Start();
            Application.AddMessageFilter(this);
        }

        // Called by the message filter on any mouse/keyboard activity.
        private Action _resetIdle;

        /// <summary>True if the client has entered anything worth clearing on idle.</summary>
        private bool HasProgress() =>
            _step != 0 || _selected.Count > 0 ||
            !Blank(_txtFirst) || !Blank(_txtMiddle) || !Blank(_txtLast) ||
            !Blank(_txtContact) || _idBytes != null;

        // Keep the kiosk alive: any input resets the idle countdown.
        public bool PreFilterMessage(ref Message m)
        {
            // WM_MOUSEMOVE, WM_LBUTTONDOWN, WM_KEYDOWN, WM_MOUSEWHEEL
            if (m.Msg == 0x0200 || m.Msg == 0x0201 || m.Msg == 0x0100 || m.Msg == 0x020A)
                _resetIdle?.Invoke();
            return false; // never swallow the message
        }

        // ---------------------------------------------------------------- UI
        private void BuildUi()
        {
            // Header (top): big title + numbered step indicator.
            var header = new Panel { Dock = DockStyle.Top, Height = 128, BackColor = Bg };
            header.Controls.Add(new Label
            {
                Text = "Request a Service",
                Font = new Font("Segoe UI", 30F, FontStyle.Bold),
                ForeColor = Ink,
                AutoSize = true,
                Location = new Point(40, 18)
            });
            _stepInd = new StepIndicator(new[] { "Select Services", "Personal Info & Photo" })
            {
                Location = new Point(42, 78),
                Size = new Size(760, 40)
            };
            _lblStep = new Label
            {
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Muted,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight
            };
            header.Controls.Add(_stepInd);
            header.Controls.Add(_lblStep);
            header.Resize += (s, e) => _lblStep.Location = new Point(header.Width - _lblStep.Width - 40, 34);

            // Footer (bottom): Back (left) + Next / Print (right).
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 92, BackColor = Bg };
            footer.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Line });

            _btnBack = new Button
            {
                Text = "◄  Back",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Ink,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(210, 62),
                Location = new Point(40, 15),
                Cursor = Cursors.Hand
            };
            _btnBack.FlatAppearance.BorderColor = Line;
            _btnBack.FlatAppearance.MouseOverBackColor = Color.FromArgb(233, 236, 239);
            _btnBack.Click += (s, e) => Back();

            _btnNext = new Button
            {
                Text = "Next  ►",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Accent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(300, 62),
                Cursor = Cursors.Hand
            };
            _btnNext.FlatAppearance.BorderSize = 0;
            _btnNext.FlatAppearance.MouseOverBackColor = Color.FromArgb(11, 94, 215);
            _btnNext.Click += (s, e) => Next();

            footer.Controls.Add(_btnBack);
            footer.Controls.Add(_btnNext);
            footer.Resize += (s, e) =>
                _btnNext.Location = new Point(footer.Width - _btnNext.Width - 40, 16);

            // Host (fill): holds the four step panels, one visible at a time.
            _host = new Panel { Dock = DockStyle.Fill, BackColor = Bg };

            _stepPanels[0] = BuildServiceStep();
            _stepPanels[1] = BuildDetailsPhotoStep();   // Part 2 = details + photo combined
            foreach (Panel p in _stepPanels)
            {
                p.Dock = DockStyle.Fill;
                p.Visible = false;
                _host.Controls.Add(p);
            }

            Controls.Add(_host);    // fill
            Controls.Add(footer);   // bottom
            Controls.Add(header);   // top
        }

        // -------------------------------------------- office-availability gate
        /// <summary>Builds the full-screen "office unavailable" overlay (hidden until needed).</summary>
        private void BuildOfflineOverlay()
        {
            _offlineOverlay = new Panel
            {
                Bounds = ClientRectangle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(17, 24, 39),
                Visible = false
            };
            _offlineOverlay.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "The office is currently unavailable.\n\nPlease try again later.\n\n" +
                       "(Waiting for a service window to come online…)"
            });
            Controls.Add(_offlineOverlay);
            _offlineOverlay.BringToFront();
        }

        /// <summary>Shows the overlay when no window is online; hides it once one is.</summary>
        private void UpdateAvailability()
        {
            bool open = OfficeOnline();
            if (open != !_offlineOverlay.Visible)
            {
                _offlineOverlay.Visible = !open;
                if (!open) _offlineOverlay.BringToFront();
            }
            ApplyServiceAvailability();
        }

        /// <summary>
        /// Section 7 — a service is only offered while a qualified staff member is logged
        /// into a window that handles it. Cards for services no online window can serve are
        /// disabled (greyed, un-tappable) and unselected, and re-enable automatically when
        /// such a window comes online. No restart needed.
        /// </summary>
        private void ApplyServiceAvailability()
        {
            HashSet<string> avail = AvailableServiceCodes();
            foreach (var svc in Catalogue)
            {
                if (!_cards.TryGetValue(svc.Code, out Panel card)) continue;
                bool ok = avail.Contains(svc.Code);
                if (!ok && _selected.Contains(svc.Code)) { _selected.Remove(svc.Code); }
                if (card.Enabled == ok) { if (!ok) PaintCard(svc.Code); continue; }

                card.Enabled = ok;
                card.Cursor = ok ? Cursors.Hand : Cursors.No;
                if (ok) { PaintCard(svc.Code); }
                else
                {
                    card.BackColor = Color.FromArgb(233, 236, 239);   // greyed "unavailable"
                    foreach (Control c in card.Controls)
                    {
                        c.BackColor = card.BackColor;
                        if (c is Label l) l.ForeColor = Color.FromArgb(173, 181, 189);
                    }
                }
            }
        }

        /// <summary>
        /// Service codes that at least one ONLINE, active window is assigned to handle. A
        /// window with no rows in window_transactions (or a Priority window) handles ALL
        /// services. Returns an empty set (nothing available) if the DB is unreachable.
        /// </summary>
        private static HashSet<string> AvailableServiceCodes()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DataTable online = Db.Pull(
                    "SELECT w.id, w.is_priority, " +
                    "(SELECT COUNT(*) FROM window_transactions wt WHERE wt.window_id = w.id) AS assigned " +
                    "FROM windows w WHERE w.status = 'Active' AND w.current_operator IS NOT NULL " +
                    "AND w.last_heartbeat > (NOW() - INTERVAL " + OfficeStaleMinutes + " MINUTE)");

                foreach (DataRow w in online.Rows)
                {
                    bool priority = w["is_priority"] != DBNull.Value && Convert.ToInt32(w["is_priority"]) == 1;
                    int assigned = Convert.ToInt32(w["assigned"]);
                    if (priority || assigned == 0)
                    {
                        foreach (var svc in Catalogue) result.Add(svc.Code);   // handles all
                    }
                    else
                    {
                        DataTable codes = Db.Pull(
                            "SELECT service_code FROM window_transactions WHERE window_id = " + w["id"]);
                        foreach (DataRow c in codes.Rows) result.Add(c["service_code"].ToString());
                    }
                }
            }
            catch { /* DB unreachable → nothing available (the office overlay also shows) */ }
            return result;
        }

        /// <summary>True when ≥1 active window has an operator signed in with a fresh heartbeat.</summary>
        private static bool OfficeOnline()
        {
            try
            {
                DataTable dt = Db.Pull(
                    "SELECT COUNT(*) AS n FROM windows " +
                    "WHERE status = 'Active' AND current_operator IS NOT NULL " +
                    "AND last_heartbeat > (NOW() - INTERVAL " + OfficeStaleMinutes + " MINUTE)");
                return dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["n"]) > 0;
            }
            catch
            {
                return false;   // DB unreachable → treat the office as unavailable
            }
        }

        // -------------------------------------------------- Step 1: services
        private Panel BuildServiceStep()
        {
            // AutoScroll = safety net: if the cards still exceed the screen height after
            // scaling, they stay reachable by scrolling instead of being cut off.
            var wrap = new Panel { BackColor = Bg, AutoScroll = true };
            _svcHint = new Label
            {
                Text = "Tap one or more services you need today. You can pick several.",
                Font = new Font("Segoe UI", 13F),
                ForeColor = Muted,
                AutoSize = true
            };
            // Auto-sizing grid of cards, centred in the step (3 across → 3×2 for six cards).
            _svcGrid = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MaximumSize = new Size(1160, 0),
                BackColor = Bg
            };
            foreach (var svc in Catalogue)
            {
                Panel card = BuildCard(svc);
                _cards[svc.Code] = card;
                _svcGrid.Controls.Add(card);
            }
            wrap.Controls.Add(_svcHint);
            wrap.Controls.Add(_svcGrid);
            wrap.Resize += (s, e) => CenterServiceStep(wrap);
            return wrap;
        }

        /// <summary>Centres the service-card grid + its hint inside the step.</summary>
        private void CenterServiceStep(Control wrap)
        {
            if (_svcGrid == null) return;
            _svcGrid.Left = Math.Max(0, (wrap.ClientSize.Width - _svcGrid.Width) / 2);
            _svcGrid.Top = Math.Max(64, (wrap.ClientSize.Height - _svcGrid.Height) / 2);
            _svcHint.Left = _svcGrid.Left + 4;
            _svcHint.Top = _svcGrid.Top - 46;
        }

        private Panel BuildCard(Service svc)
        {
            var card = new Panel
            {
                Size = new Size(348, 200),
                Margin = new Padding(16),
                BackColor = CardBg,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand,
                Tag = svc
            };

            var glyph = new Label
            {
                Text = svc.Glyph,
                Font = new Font("Segoe UI Emoji", 42F),
                AutoSize = false,
                Size = new Size(348, 92),
                Location = new Point(0, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            var name = new Label
            {
                Text = svc.Label,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Ink,
                AutoSize = false,
                Size = new Size(348, 56),
                Location = new Point(0, 128),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };

            card.Controls.Add(glyph);
            card.Controls.Add(name);

            EventHandler toggle = (s, e) => ToggleService(svc.Code);
            card.Click += toggle;
            glyph.Click += toggle;
            name.Click += toggle;

            return card;
        }

        // ------------------------------ Part 2: details + photo (one screen)
        // Spec section 3: the separate "take picture" step is merged in here, so the
        // complete fill-up form, the camera, picture capture and image validation all
        // live on Part 2. Details on the left, live webcam on the right.
        private Panel BuildDetailsPhotoStep()
        {
            var wrap = new Panel { BackColor = Bg, AutoScroll = true };
            var box = new Panel { BackColor = Bg, Size = new Size(1200, 742) };
            _detailsBox = box;

            // ===== Left card: Personal Information =====
            var left = new RoundPanel
            {
                Location = new Point(6, 6),
                Size = new Size(600, 730),
                Radius = 14,
                Shadow = 8,
                Fill = CardBg
            };
            box.Controls.Add(left);

            int lx = 30, lw = 536, y = 22;
            left.Controls.Add(CardTitle("Personal Information", lx, y)); y += 44;

            left.Controls.Add(FieldLabel("First Name", lx, y, false)); y += 26;
            _txtFirst = MakeField(left, lx, y, lw); y += 60;
            left.Controls.Add(FieldLabel("Middle Name", lx, y, true)); y += 26;
            _txtMiddle = MakeField(left, lx, y, lw); y += 60;
            left.Controls.Add(FieldLabel("Last Name", lx, y, false)); y += 26;
            _txtLast = MakeField(left, lx, y, lw); y += 60;
            left.Controls.Add(FieldLabel("Contact Number", lx, y, true)); y += 26;
            _txtContact = MakeField(left, lx, y, lw); y += 60;

            // Priority lane — modern selectable pills.
            left.Controls.Add(new Label
            {
                Text = "PRIORITY LANE",
                AutoSize = true,
                ForeColor = Muted,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(lx, y)
            });
            y += 26;
            _priSenior = MakePill("🧓", "Senior Citizen", lx, y, lw); left.Controls.Add(_priSenior); y += 58;
            _priPwd = MakePill("♿", "Person with Disability (PWD)", lx, y, lw); left.Controls.Add(_priPwd); y += 58;
            _priPregnant = MakePill("🤰", "Pregnant", lx, y, lw); left.Controls.Add(_priPregnant); y += 60;

            // Claim ticket group (only for Claim / Pickup — toggled in ShowStep).
            _claimPanel = new Panel { Location = new Point(lx, y), Size = new Size(lw, 110), BackColor = CardBg };
            _claimPanel.Controls.Add(FieldLabel("Queue Ticket Number (from your last visit)", 0, 0, false));
            _txtClaimTicket = MakeField(_claimPanel, 0, 26, lw);
            _claimPanel.Controls.Add(new Label
            {
                Text = "Type the Q-number on the ticket you got when you requested (e.g. Q-006).\n" +
                       "It is the key that finds the document you asked for. Leave blank only to start a new claim.",
                AutoSize = true,
                ForeColor = Muted,
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(2, 74)
            });
            _claimPanel.Visible = false;
            left.Controls.Add(_claimPanel);

            // ===== Right card: Camera =====
            var right = new RoundPanel
            {
                Location = new Point(624, 6),
                Size = new Size(570, 730),
                Radius = 14,
                Shadow = 8,
                Fill = CardBg
            };
            box.Controls.Add(right);

            int rx = 26, rw = 518;
            _rightTitle = CardTitle("Your Photo", rx, 22);
            right.Controls.Add(_rightTitle);
            _rightHint = new Label
            {
                Text = "Look at the camera, then tap Capture Photo.",
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Muted,
                AutoSize = true,
                Location = new Point(rx, 60)
            };
            right.Controls.Add(_rightHint);

            // Rounded dark preview frame.
            var frame = new RoundPanel
            {
                Location = new Point(rx, 92),
                Size = new Size(rw, 380),
                Radius = 12,
                Fill = Color.FromArgb(17, 24, 39)
            };
            _camFrame = frame;
            right.Controls.Add(frame);

            // ---- claimapp QR block — sits in the RIGHT HALF beside the (shrunk) camera when
            //      Release & Claim is selected, so the client BOTH has a photo AND can scan
            //      the QR to upload their ID. Hidden for a normal request. ----
            int qcw = 258;
            _claimQrPanel = new Panel
            {
                Location = new Point(rx + 262, 92),
                Size = new Size(qcw, 380),
                BackColor = CardBg,
                Visible = false
            };
            _claimQrPanel.Controls.Add(new Label
            {
                Text = "Scan with your phone camera to upload your ID",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Ink,
                AutoSize = false,
                Size = new Size(qcw, 24),
                Location = new Point(0, 0)
            });
            _picClaimQr = new PictureBox
            {
                Location = new Point((qcw - 224) / 2, 30),
                Size = new Size(224, 224),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = CardBg,
                BorderStyle = BorderStyle.FixedSingle
            };
            _claimQrPanel.Controls.Add(_picClaimQr);
            _lblClaimQrCap = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Ink,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(qcw, 26),
                Location = new Point(0, 262)
            };
            _claimQrPanel.Controls.Add(_lblClaimQrCap);
            _lblClaimUrl = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Ink,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(qcw, 56),
                Location = new Point(0, 290)
            };
            _claimQrPanel.Controls.Add(_lblClaimUrl);
            _claimQrPanel.Controls.Add(new Label
            {
                Text = "Optional — you can also just wait for your number at the window.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Muted,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(qcw, 34),
                Location = new Point(0, 348)
            });
            right.Controls.Add(_claimQrPanel);
            _claimQrPanel.BringToFront();
            _picCam = new PictureBox
            {
                Location = new Point(8, 8),
                Size = new Size(rw - 16, 364),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(17, 24, 39),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            _picCam.Paint += DrawCameraGuide;
            frame.Controls.Add(_picCam);

            _lblLive = new Label
            {
                Text = "● LIVE",
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 53, 69),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Padding = new Padding(6, 2, 6, 2),
                Location = new Point(12, 12),
                Visible = false
            };
            _picCam.Controls.Add(_lblLive);

            // Camera-ready indicator.
            _lblCamState = new Label
            {
                Text = "🔴  Camera Not Detected",
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(176, 42, 55),
                Location = new Point(rx, 486)
            };
            right.Controls.Add(_lblCamState);

            // Full-width capture button (dynamic text).
            _btnCapture = new Button
            {
                Text = "📷  Capture Photo",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Accent,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(rx, 518),
                Size = new Size(rw, 56),
                Cursor = Cursors.Hand
            };
            _btnCapture.FlatAppearance.BorderSize = 0;
            _btnCapture.FlatAppearance.MouseOverBackColor = Color.FromArgb(11, 94, 215);
            _btnCapture.Click += (s, e) => CapturePhoto();
            right.Controls.Add(_btnCapture);

            _lblCamStatus = new Label
            {
                Text = "Starting camera…",
                AutoSize = false,
                Size = new Size(rw, 44),
                ForeColor = Muted,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(rx, 586)
            };
            right.Controls.Add(_lblCamStatus);

            wrap.Controls.Add(box);
            wrap.Resize += (s, e) => CenterDetailsStep(wrap);
            return wrap;
        }

        // -------- modern step-2 helpers --------
        private static Label CardTitle(string t, int x, int y) => new Label
        {
            Text = t, AutoSize = true, ForeColor = Ink,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold), Location = new Point(x, y)
        };

        private static Label FieldLabel(string t, int x, int y, bool optional) => new Label
        {
            Text = optional ? t + "   (optional)" : t,
            AutoSize = true,
            ForeColor = optional ? Color.FromArgb(140, 146, 153) : Color.FromArgb(73, 80, 87),
            Font = new Font("Segoe UI", 9.5F),
            Location = new Point(x, y)
        };

        /// <summary>A rounded, white text field whose border turns blue while focused.</summary>
        private TextBox MakeField(Control parent, int x, int y, int w)
        {
            var host = new RoundPanel
            {
                Location = new Point(x, y),
                Size = new Size(w, 46),
                Radius = 8,
                Fill = Color.White,
                BorderColor = Line,
                BorderWidth = 1.5f
            };
            var tb = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 13F),
                Location = new Point(14, 12),
                Width = w - 28,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            tb.Enter += (s, e) => { host.BorderColor = Accent; host.BorderWidth = 2f; host.Invalidate(); };
            tb.Leave += (s, e) => { host.BorderColor = Line; host.BorderWidth = 1.5f; host.Invalidate(); };
            host.Controls.Add(tb);
            parent.Controls.Add(host);
            return tb;
        }

        private PillToggle MakePill(string glyph, string text, int x, int y, int w) =>
            new PillToggle(glyph, text) { Location = new Point(x, y), Size = new Size(w, 50) };

        /// <summary>
        /// Draws a dashed face-guide oval over the live preview (helps the client frame
        /// themselves). NOTE: this is a static guide, not automatic face detection — real
        /// "Face Detected / Move Closer" feedback would need a vision library.
        /// </summary>
        private void DrawCameraGuide(object sender, PaintEventArgs e)
        {
            var pb = (PictureBox)sender;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int gw = (int)(pb.Width * 0.44), gh = (int)(pb.Height * 0.74);
            int gx = (pb.Width - gw) / 2, gy = (pb.Height - gh) / 2;
            using (var pen = new Pen(Color.FromArgb(150, 255, 255, 255), 2f) { DashStyle = DashStyle.Dash })
            using (var path = RoundPanel.Round(new Rectangle(gx, gy, gw, gh), Math.Min(60, gw / 2)))
                g.DrawPath(pen, path);

            if (_idBytes == null)
                TextRenderer.DrawText(g, "Position your face inside the frame",
                    new Font("Segoe UI", 9F, FontStyle.Bold),
                    new Rectangle(0, pb.Height - 30, pb.Width, 24),
                    Color.FromArgb(225, 255, 255, 255),
                    TextFormatFlags.HorizontalCenter);
        }

        /// <summary>
        /// Release &amp; Claim (Pick-up): swap the right card from the webcam to the claimapp QR,
        /// so the client scans it to upload their ID by phone. Any other service keeps the
        /// webcam (a queue-ticket photo).
        /// </summary>
        private void ApplyClaimMode(bool claim)
        {
            if (_camFrame == null || _claimQrPanel == null) return;

            // Camera stays visible in BOTH modes (the client still gets a photo). For a claim,
            // it shrinks to the left half and the QR appears on the right.
            _claimQrPanel.Visible = claim;
            _camFrame.Size = claim ? new Size(250, 380) : new Size(518, 380);

            if (claim)
            {
                _rightTitle.Text = "Your Photo  +  Upload ID";
                _rightHint.Text = "Take your photo, and scan the QR to upload your ID.";
                EnsureClaimQr();
            }
            else
            {
                _rightTitle.Text = "Your Photo";
                _rightHint.Text = "Look at the camera, then tap Capture Photo.";
            }
            if (_camera == null || !_camera.IsRunning) StartCamera();
        }

        /// <summary>
        /// Creates the claim request (once) and shows its claimapp QR. Best-effort — a DB or
        /// QR-library problem just leaves the panel with a friendly note instead of a code.
        /// </summary>
        private void EnsureClaimQr()
        {
            if (_claimQrToken == null)
            {
                var made = CreateClaimRequest();
                _claimQrToken = made.token;
                _claimQrNo = made.ticketNo;
            }

            if (_claimQrToken == null)
            {
                _lblClaimQrCap.Text = "";
                if (_picClaimQr != null) _picClaimQr.Image = null;
                _lblClaimQrCap.Text = "QR unavailable — the staff will assist you at the window.";
                return;
            }

            try
            {
                var old = _picClaimQr.Image;
                _picClaimQr.Image = QrHelper.TryCreate(ClaimLink.Build(_claimQrToken), 8);
                old?.Dispose();
            }
            catch { /* QR lib missing → panel shows the ticket number text only */ }

            _lblClaimQrCap.Text = _claimQrNo != null ? "Claim ticket:  " + _claimQrNo : "";

            // Camera-free fallback: show the base URL to type + the ticket number to enter.
            if (_lblClaimUrl != null)
                _lblClaimUrl.Text = "No camera? On your phone open:\n" + ClaimLink.BaseUrl() +
                    (_claimQrNo != null ? "\nthen enter ticket:  " + _claimQrNo : "");
        }

        /// <summary>Centres the details+photo box inside the step.</summary>
        private void CenterDetailsStep(Control wrap)
        {
            if (_detailsBox == null) return;
            _detailsBox.Left = Math.Max(0, (wrap.ClientSize.Width - _detailsBox.Width) / 2);
            _detailsBox.Top = Math.Max(16, (wrap.ClientSize.Height - _detailsBox.Height) / 2);
        }

        /// <summary>
        /// Shrinks the fixed-size step content to fit the screen when the laptop is
        /// smaller than the design size, so nothing is cut off or hidden behind a
        /// scrollbar. Runs once (on first show). Large screens keep the design size,
        /// centred — never enlarged past 1× to avoid blur. Also re-centres after scaling.
        /// </summary>
        private void FitToScreen()
        {
            if (_fitApplied || _host == null) return;
            int hw = _host.ClientSize.Width, hh = _host.ClientSize.Height;
            if (hw < 100 || hh < 100) return;   // not laid out yet

            float f = Math.Min(1f, Math.Min(hw / DesignW, hh / DesignH));
            _fitApplied = true;
            if (f < 0.999f)
            {
                var factor = new SizeF(f, f);
                _stepPanels[0].Scale(factor);
                _stepPanels[1].Scale(factor);
            }
            CenterServiceStep(_stepPanels[0]);
            CenterDetailsStep(_stepPanels[1]);
        }

        // ------------------------------------------------- small UI helpers
        private static Label Title(string text, int x, int y) => new Label
        {
            Text = text, AutoSize = true, ForeColor = Color.FromArgb(33, 37, 41),
            Font = new Font("Segoe UI", 14F, FontStyle.Bold), Location = new Point(x, y)
        };

        private static Label Cap(string text, int x, int y) => new Label
        {
            Text = text, AutoSize = true, ForeColor = Color.FromArgb(108, 117, 125),
            Font = new Font("Segoe UI", 10F), Location = new Point(x, y)
        };

        private static TextBox Field(int x, int y, int w) => new TextBox
        {
            Font = new Font("Segoe UI", 14F), Location = new Point(x, y), Size = new Size(w, 34)
        };

        private static CheckBox Check(string text, int x, int y) => new CheckBox
        {
            Text = text, AutoSize = true, ForeColor = Color.FromArgb(33, 37, 41),
            Font = new Font("Segoe UI", 12F), Location = new Point(x, y),
            Padding = new Padding(4, 2, 0, 2), Cursor = Cursors.Hand
        };

        // ------------------------------------------------------ navigation
        private void ShowStep(int i)
        {
            _step = Math.Max(0, Math.Min(StepTitles.Length - 1, i));

            for (int k = 0; k < _stepPanels.Length; k++)
                _stepPanels[k].Visible = (k == _step);
            _stepPanels[_step].BringToFront();

            // Claim ticket field only when the Claim / Pickup service is chosen.
            if (_step == 1 && _claimPanel != null)
            {
                bool claim = _selected.Contains("CLAIM");
                _claimPanel.Visible = claim;
                if (!claim) _txtClaimTicket.Clear();
                ApplyClaimMode(claim);
            }

            _lblStep.Text = "Step " + (_step + 1) + " of " + StepTitles.Length;
            _stepInd?.SetStep(_step);
            _lblStep.Location = new Point(_lblStep.Parent.Width - _lblStep.Width - 40, 34);

            _btnBack.Visible = _step > 0;
            bool last = _step == StepTitles.Length - 1;
            _btnNext.Text = last ? "🖨  Print Queue Ticket" : "Next  ►";
            _btnNext.BackColor = last ? Color.FromArgb(25, 135, 84) : Accent;
            _btnNext.FlatAppearance.MouseOverBackColor = last
                ? Color.FromArgb(20, 108, 67)
                : Color.FromArgb(11, 94, 215);
        }

        private void Back()
        {
            if (_step > 0) ShowStep(_step - 1);
        }

        private void Next()
        {
            if (!ValidateStep(_step)) return;
            if (_step == StepTitles.Length - 1) { Submit(); return; }
            ShowStep(_step + 1);
        }

        /// <summary>Per-step gate. Shows a friendly warning instead of a silently dead button.</summary>
        private bool ValidateStep(int step)
        {
            switch (step)
            {
                case 0:
                    if (_selected.Count == 0) { Warn("Please tap at least one service to continue."); return false; }
                    return true;
                case 1:
                    if (Blank(_txtFirst) || Blank(_txtLast))
                    {
                        Warn("Please enter your first and last name.");
                        (Blank(_txtFirst) ? _txtFirst : _txtLast).Focus();
                        return false;
                    }
                    return true;
                default:
                    return true; // photo + review have no hard requirement
            }
        }

        // ------------------------------------------------------ selection
        private void ToggleService(string code)
        {
            if (_selected.Contains(code)) _selected.Remove(code);
            else _selected.Add(code);
            PaintCard(code);
        }

        private void PaintCard(string code)
        {
            if (!_cards.TryGetValue(code, out Panel card)) return;
            bool on = _selected.Contains(code);
            card.BackColor = on ? CardSelBg : CardBg;
            foreach (Control c in card.Controls)
            {
                c.BackColor = card.BackColor;
                if (c is Label l && l.Font.Bold) l.ForeColor = on ? Accent : Ink;
            }
        }

        private static Service Find(string code) => Catalogue.First(s => s.Code == code);

        // ------------------------------------------------------------ live webcam
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
                _lblCamStatus.Text = "Live camera — tap Capture Photo when ready.";
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
            try
            {
                var disp = (Bitmap)frame.Clone();
                var old = _picCam.Image;
                _picCam.Image = disp;
                old?.Dispose();
            }
            catch { /* form closing / cross-thread paint — safe to ignore */ }
        }

        private void CapturePhoto()
        {
            lock (_frameLock)
            {
                if (_lastFrame == null) { Warn("The camera is not ready yet."); return; }
                using (var ms = new MemoryStream())
                {
                    _lastFrame.Save(ms, ImageFormat.Jpeg);
                    _idBytes = ms.ToArray();
                }
            }
            if (_btnCapture != null)
            {
                _btnCapture.Text = "🔄  Retake Photo";
                _btnCapture.BackColor = Color.FromArgb(108, 117, 125);
            }
            if (_lblCamState != null)
            {
                _lblCamState.Text = "✅  Photo Captured";
                _lblCamState.ForeColor = Color.FromArgb(25, 135, 84);
            }
            _picCam?.Invalidate();   // drop the "position your face" hint
            _lblCamStatus.Text = "Photo captured. Tap Retake Photo to redo.";
        }

        /// <summary>Sets the green "Ready" / red "Not Detected" indicator + LIVE badge.</summary>
        private void SetCamState(bool ready)
        {
            if (_lblCamState != null)
            {
                _lblCamState.Text = ready ? "🟢  Camera Ready" : "🔴  Camera Not Detected";
                _lblCamState.ForeColor = ready
                    ? Color.FromArgb(25, 135, 84)
                    : Color.FromArgb(176, 42, 55);
            }
            if (_lblLive != null) _lblLive.Visible = ready;
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Application.RemoveMessageFilter(this);
            _idle?.Stop();
            _availability?.Stop();
            StopCamera();
            base.OnFormClosing(e);
        }

        private MySqlParameter ImageParam()
        {
            var p = new MySqlParameter("@img", MySqlDbType.LongBlob);
            p.Value = (_idBytes == null || _idBytes.Length == 0) ? (object)DBNull.Value : _idBytes;
            return p;
        }

        // -------------------------------------------------------------- submit
        private void Submit()
        {
            if (_selected.Count == 0) { ShowStep(0); Warn("Please select at least one service."); return; }
            if (Blank(_txtFirst) || Blank(_txtLast)) { ShowStep(1); Warn("Please enter your first and last name."); return; }

            // Returning-client pickup: if they entered a queue number that maps to a parked
            // (Waiting-to-Release / awaiting-print) request, this is a reclaim — link the new
            // ticket to that same transaction and push them to the front (priority). Falls
            // back to the claimapp CLM ticket lookup, then to a brand-new claim request.
            long returnTxnId = 0;
            if (_selected.Contains("CLAIM"))
            {
                string entered = _txtClaimTicket.Text.Trim();
                if (entered.Length > 0)
                {
                    returnTxnId = ResolveParkedByQueue(entered);
                    if (returnTxnId == 0)
                    {
                        ShowStep(1);
                        Warn("That queue number was not found among held requests. Check the Q-number " +
                             "on your ticket, or leave it blank to start a new claim.");
                        return;
                    }
                }
            }

            // A reclaim always goes first in line.
            string priority = returnTxnId != 0 ? "Priority" : PriorityValue();
            string joined = string.Join(", ", _selected.Select(c => Find(c).Label));
            string primary = Find(_selected[0]).Label;

            int num = NextNum();
            string code = "Q-" + num.ToString("D3");

            object contact = Blank(_txtContact) ? (object)DBNull.Value : _txtContact.Text.Trim();

            try
            {
                // One ticket = one queue number, with the client's photo in id_image.
                long ticketId = Db.Insert(
                    "INSERT INTO queue_tickets (ticket_code, full_name, contact_no, id_image, number_queue, " +
                    "date, time, status, document_type, type_label, priority) " +
                    "VALUES (@code, @name, @contact, @img, @num, @date, @time, 'Waiting', @doc, @label, @priority)",
                    new MySqlParameter("@code", code),
                    new MySqlParameter("@name", FullName()),
                    new MySqlParameter("@contact", contact),
                    ImageParam(),
                    new MySqlParameter("@num", num),
                    new MySqlParameter("@date", DateTime.Today),
                    new MySqlParameter("@time", DateTime.Now.ToString("HH:mm")),
                    new MySqlParameter("@doc", primary),
                    new MySqlParameter("@label", joined),
                    new MySqlParameter("@priority", priority));

                // One row per requested service, all linked to that ticket.
                foreach (string c in _selected)
                {
                    Service svc = Find(c);
                    Db.Push(
                        "INSERT INTO queue_ticket_services (ticket_id, service_code, service_label) " +
                        "VALUES (@tid, @sc, @sl)",
                        new MySqlParameter("@tid", ticketId),
                        new MySqlParameter("@sc", svc.Code),
                        new MySqlParameter("@sl", svc.Label));
                }

                // Reclaim: point the new priority ticket at the parked transaction so staff
                // resolve it from the queue number and finish it (Resume → Pay → Release).
                if (returnTxnId != 0)
                    Db.Push("UPDATE queue_tickets SET transaction_id = @txn WHERE id = @tid",
                        new MySqlParameter("@txn", returnTxnId),
                        new MySqlParameter("@tid", ticketId));

                // Claim / Pickup handling: the claimapp QR (for uploading the ID by phone) was
                // already created when the client reached step 2 (EnsureClaimQr). Stamp the
                // final name onto it and, for a reclaim, link it to the parked transaction.
                string claimToken = null, claimNo = null;
                if (_selected.Contains("CLAIM"))
                {
                    EnsureClaimQr();
                    claimToken = _claimQrToken;
                    claimNo = _claimQrNo;
                    FinalizeClaimRow(returnTxnId);
                }

                var services = _selected.Select(c => Find(c).Label).ToList();
                int ahead = AheadCount(ticketId, priority, num);
                PrintTicket(code, services, priority, FullName(), ahead, claimToken, claimNo);
                ShowTicket(code, services, claimToken, claimNo);
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sorry, your request could not be submitted:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Maps the three priority checkboxes onto the existing single `priority` column.
        /// Strongest wins; order matches the staff Call-Next ordering
        /// FIELD(priority,'Priority','PWD','Senior','Regular'). Pregnant → 'Priority'.
        /// </summary>
        private string PriorityValue()
        {
            if (_priPregnant.Checked) return "Priority";
            if (_priPwd.Checked) return "PWD";
            if (_priSenior.Checked) return "Senior";
            return "Regular";
        }

        private string FullName()
        {
            var parts = new List<string>();
            if (!Blank(_txtFirst)) parts.Add(_txtFirst.Text.Trim());
            if (!Blank(_txtMiddle)) parts.Add(_txtMiddle.Text.Trim());
            if (!Blank(_txtLast)) parts.Add(_txtLast.Text.Trim());
            return string.Join(" ", parts);
        }

        /// <summary>
        /// Creates a claim_requests row for the self-service Claim Request and returns its
        /// QR token + claim ticket number. Best-effort — never blocks the queue ticket.
        /// </summary>
        private (string token, string ticketNo) CreateClaimRequest()
        {
            try
            {
                string token = Guid.NewGuid().ToString("N");
                string ticketNo = NextClaimNo();
                Db.Push(
                    "INSERT INTO claim_requests " +
                    "(qr_token, claim_ticket_no, first_name, middle_name, last_name, request_details, status) " +
                    "VALUES (@t, @tk, @f, @m, @l, 'Release & Claim (Pick-up)', 'Pending')",
                    new MySqlParameter("@t", token),
                    new MySqlParameter("@tk", ticketNo),
                    new MySqlParameter("@f", Blank(_txtFirst) ? (object)DBNull.Value : _txtFirst.Text.Trim()),
                    new MySqlParameter("@m", Blank(_txtMiddle) ? (object)DBNull.Value : _txtMiddle.Text.Trim()),
                    new MySqlParameter("@l", Blank(_txtLast) ? (object)DBNull.Value : _txtLast.Text.Trim()));
                return (token, ticketNo);
            }
            catch { return (null, null); }
        }

        /// <summary>
        /// Stamps the final name onto the on-screen claim row and, for a reclaim, links it to
        /// the parked transaction so the ID uploaded via claimapp attaches to the right request.
        /// </summary>
        private void FinalizeClaimRow(long txnId)
        {
            if (_claimQrToken == null) return;
            try
            {
                Db.Push(
                    "UPDATE claim_requests SET first_name = @f, middle_name = @m, last_name = @l, " +
                    "transaction_id = @txn WHERE qr_token = @t",
                    new MySqlParameter("@f", Blank(_txtFirst) ? (object)DBNull.Value : _txtFirst.Text.Trim()),
                    new MySqlParameter("@m", Blank(_txtMiddle) ? (object)DBNull.Value : _txtMiddle.Text.Trim()),
                    new MySqlParameter("@l", Blank(_txtLast) ? (object)DBNull.Value : _txtLast.Text.Trim()),
                    new MySqlParameter("@txn", txnId > 0 ? (object)txnId : DBNull.Value),
                    new MySqlParameter("@t", _claimQrToken));
            }
            catch { /* best-effort */ }
        }

        private static string NextClaimNo()
        {
            string year = DateTime.Now.Year.ToString();
            DataTable dt = Db.Pull(
                "SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(claim_ticket_no,'-',-1) AS UNSIGNED)),0)+1 AS n " +
                "FROM claim_requests WHERE claim_ticket_no LIKE 'CLM-" + year + "-%'");
            int n = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["n"]) : 1;
            return "CLM-" + year + "-" + n.ToString("D4");
        }

        /// <summary>
        /// Resolves a queue number the client typed (e.g. "Q-006", "006", or "6") to the id
        /// of the PARKED transaction it belongs to (status WaitingToRelease / ForPrint).
        /// Returns 0 if the number doesn't match a parked request. Because queue numbers are
        /// now continuous (never reset), the number alone is a unique key across all days.
        /// </summary>
        private static long ResolveParkedByQueue(string entered)
        {
            try
            {
                int n = 0;
                int.TryParse(new string(entered.Where(char.IsDigit).ToArray()), out n);
                DataTable dt = Db.Pull(
                    "SELECT t.id FROM queue_tickets qt " +
                    "JOIN transactions t ON t.id = qt.transaction_id " +
                    "WHERE ( qt.ticket_code = @raw OR (@n > 0 AND qt.number_queue = @n) ) " +
                    "AND t.status IN ('WaitingToRelease','ForPrint') " +
                    "ORDER BY qt.id DESC LIMIT 1",
                    new MySqlParameter("@raw", entered),
                    new MySqlParameter("@n", n));
                return dt.Rows.Count > 0 ? Convert.ToInt64(dt.Rows[0]["id"]) : 0;
            }
            catch { return 0; }
        }

        /// <summary>
        /// Next queue number — CONTINUOUS across all days (never resets). Today ends at
        /// Q-005, tomorrow starts at Q-006. Because the number never repeats it is unique
        /// forever, so it doubles as the pull key: a client who parks a request keeps this
        /// number and types it back at the kiosk to reclaim the exact request later. The
        /// per-day client count is still available by counting tickets where DATE = today.
        /// </summary>
        private static int NextNum()
        {
            DataTable dt = Db.Pull(
                "SELECT COALESCE(MAX(number_queue), 0) + 1 AS n FROM queue_tickets " +
                "WHERE ticket_code LIKE 'Q-%'");
            return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["n"]) : 1;
        }

        /// <summary>
        /// How many still-Waiting clients will be called before this ticket — i.e. anyone in a
        /// stronger priority lane, plus anyone in the SAME lane with a smaller queue number.
        /// Matches the staff Call-Next ordering FIELD(priority,'Priority','PWD','Senior','Regular').
        /// Returns -1 if the count can't be read (then the ticket just omits the line).
        /// </summary>
        private static int AheadCount(long selfId, string priority, int num)
        {
            try
            {
                DataTable dt = Db.Pull(
                    "SELECT COUNT(*) AS n FROM queue_tickets " +
                    "WHERE DATE(created_at) = CURDATE() AND status = 'Waiting' AND id <> @self " +
                    "AND ( FIELD(priority,'Priority','PWD','Senior','Regular') < " +
                    "        FIELD(@p,'Priority','PWD','Senior','Regular') " +
                    "   OR ( priority = @p AND number_queue < @num ) )",
                    new MySqlParameter("@self", selfId),
                    new MySqlParameter("@p", priority),
                    new MySqlParameter("@num", num));
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["n"]) : 0;
            }
            catch { return -1; }
        }

        private void ResetForm()
        {
            _selected.Clear();
            foreach (string code in _cards.Keys) PaintCard(code);
            _txtFirst.Clear(); _txtMiddle.Clear(); _txtLast.Clear(); _txtContact.Clear();
            _txtClaimTicket.Clear();

            // Fresh claim QR for the next client.
            _claimQrToken = null; _claimQrNo = null;
            if (_picClaimQr != null) { var old = _picClaimQr.Image; _picClaimQr.Image = null; old?.Dispose(); }
            if (_claimQrPanel != null) _claimQrPanel.Visible = false;
            _priSenior.SetChecked(false); _priPwd.SetChecked(false); _priPregnant.SetChecked(false);
            _idBytes = null;
            if (_btnCapture != null)
            {
                _btnCapture.Text = "📷  Capture Photo";
                _btnCapture.BackColor = Accent;
            }
            _picCam?.Invalidate();
            _lblCamStatus.Text = "Live camera — tap Capture Photo when ready.";
            ShowStep(0);
        }

        // -------------------------------------------------------------- printing
        /// <summary>
        /// Prints the physical queue ticket on a 58mm thermal roll, centred.
        /// Page width is fixed at 58mm (228 hundredths-inch); height is estimated from the
        /// line count so the roll doesn't over-feed. `priorityLane` is the raw priority value
        /// ('Regular' → no badge). `ahead` &lt; 0 hides the position line.
        /// </summary>
        private void PrintTicket(string code, List<string> services, string priorityLane, string name, int ahead,
            string claimToken = null, string claimNo = null)
        {
            try
            {
                using (var doc = new PrintDocument())
                {
                    int h = 330 + services.Count * 24 + (priorityLane != "Regular" ? 36 : 0)
                            + (claimToken != null ? 210 : 0);   // room for the claim QR block
                    doc.DefaultPageSettings.PaperSize = new PaperSize("Q58", 228, h); // 58mm wide
                    doc.DefaultPageSettings.Margins = new Margins(8, 8, 10, 10);
                    doc.DocumentName = "Queue Ticket " + code;
                    doc.PrintPage += (s, e) => DrawTicket(e, code, services, priorityLane, name, ahead, claimToken, claimNo);
                    doc.Print();
                }
            }
            catch
            {
                // No printer / print cancelled — the on-screen ticket still shows the number.
            }
        }

        private static void DrawTicket(PrintPageEventArgs e, string code, List<string> services,
            string priorityLane, string name, int ahead, string claimToken = null, string claimNo = null)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            float left = e.MarginBounds.Left;
            float width = e.MarginBounds.Width;
            float y = e.MarginBounds.Top;

            using (var fOffice = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var fSub    = new Font("Segoe UI", 7.5F))
            using (var fLabel  = new Font("Segoe UI", 8F, FontStyle.Bold))
            using (var fNumber = new Font("Consolas", 30F, FontStyle.Bold))
            using (var fBody   = new Font("Segoe UI", 8.5F))
            using (var fSmall  = new Font("Segoe UI", 7.5F))
            using (var fBadge  = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (var centre  = new StringFormat { Alignment = StringAlignment.Center })
            {
                // Centred line (wraps within the roll width).
                Action<string, Font> mid = (text, font) =>
                {
                    SizeF sz = g.MeasureString(text, font, (int)width);
                    g.DrawString(text, font, Brushes.Black, new RectangleF(left, y, width, sz.Height), centre);
                    y += sz.Height + 2;
                };
                // Left-aligned line (wraps within the roll width).
                Action<string, Font> row = (text, font) =>
                {
                    SizeF sz = g.MeasureString(text, font, (int)width);
                    g.DrawString(text, font, Brushes.Black, new RectangleF(left, y, width, sz.Height));
                    y += sz.Height + 2;
                };
                Action rule = () =>
                {
                    using (var pen = new Pen(Color.Black)) g.DrawLine(pen, left, y + 2, left + width, y + 2);
                    y += 8;
                };

                if (priorityLane != "Regular")
                {
                    mid("★ PRIORITY LANE ★", fBadge);
                    mid("(" + priorityLane + ")", fSmall);
                }

                mid("CROMS — LCRO Peñablanca", fOffice);
                mid("Local Civil Registry Office", fSub);
                mid("Municipality of Peñablanca", fSub);
                rule();

                mid("QUEUE NUMBER", fLabel);
                mid(code, fNumber);
                rule();

                if (!string.IsNullOrEmpty(name)) row("Name:  " + name, fBody);
                row("Date:  " + DateTime.Now.ToString("ddd, dd MMM yyyy"), fBody);
                row("Time:  " + DateTime.Now.ToString("hh:mm tt"), fBody);
                rule();

                row("Services requested:", fBody);
                int i = 1;
                foreach (string svc in services) { row("  " + i + ". " + svc, fBody); i++; }
                rule();

                if (ahead >= 0)
                {
                    mid("People ahead of you: " + ahead, fLabel);
                    rule();
                }

                mid("Please wait for your number", fSmall);
                mid("to be called. Keep this ticket.", fSmall);

                // Self-service Claim Request: print the claimapp QR + claim ticket number.
                if (!string.IsNullOrEmpty(claimToken))
                {
                    rule();
                    mid("CLAIM QR", fLabel);
                    if (!string.IsNullOrEmpty(claimNo)) mid("Ticket: " + claimNo, fSmall);
                    Bitmap qr = QrHelper.TryCreate(ClaimLink.Build(claimToken), 6);
                    if (qr != null)
                    {
                        float size = Math.Min(width, 150);
                        g.DrawImage(qr, left + (width - size) / 2, y, size, size);
                        y += size + 4;
                        qr.Dispose();
                    }
                    else
                    {
                        mid(claimToken, fSmall);   // no QR lib → print the token text
                    }
                    mid("Scan with your phone camera to upload your ID.", fSmall);
                }
            }
        }

        /// <summary>
        /// Big friendly on-screen confirmation: queue number + services. For a self-service
        /// Claim Request it also shows the claimapp QR on screen so the client can scan it
        /// right there to upload their ID.
        /// </summary>
        private static void ShowTicket(string code, List<string> services, string claimToken = null, string claimNo = null)
        {
            bool claim = !string.IsNullOrEmpty(claimToken);
            using (var dlg = new Form())
            {
                dlg.Text = claim ? "Claim QR + Queue Number" : "Your Queue Number";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterScreen;
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = false;
                dlg.ClientSize = new Size(480, claim ? 720 : 420);
                dlg.BackColor = Color.FromArgb(17, 24, 39);

                var ok = new Button
                {
                    Text = "OK",
                    Dock = DockStyle.Bottom, Height = 48,
                    FlatStyle = FlatStyle.Flat, ForeColor = Color.White,
                    BackColor = Color.FromArgb(13, 110, 253),
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold)
                };
                ok.Click += (s, e) => dlg.Close();

                var note = new Label
                {
                    Text = "Please take a seat and wait for your number to be called.",
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Font = new Font("Segoe UI", 10F),
                    Dock = DockStyle.Bottom, Height = 44,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                // Claim QR block (only for a Claim Request) — docked above the buttons.
                Panel claimPanel = null;
                if (claim)
                {
                    claimPanel = new Panel { Dock = DockStyle.Bottom, Height = 300, BackColor = Color.White };
                    claimPanel.Controls.Add(new Label
                    {
                        Text = "SCAN WITH YOUR PHONE CAMERA TO UPLOAD YOUR ID" +
                               (string.IsNullOrEmpty(claimNo) ? "" : "\nClaim Ticket: " + claimNo),
                        Dock = DockStyle.Top, Height = 60, TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(33, 37, 41)
                    });
                    var pic = new PictureBox
                    {
                        Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom,
                        Image = QrHelper.TryCreate(ClaimLink.Build(claimToken), 10),
                        Padding = new Padding(8)
                    };
                    if (pic.Image == null)
                        claimPanel.Controls.Add(new Label
                        {
                            Text = "Token:\n" + claimToken, Dock = DockStyle.Fill,
                            TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Consolas", 9F)
                        });
                    else claimPanel.Controls.Add(pic);
                }

                var list = new Label
                {
                    Text = "Services:\n" + string.Join("\n", services.Select(s => "•  " + s)),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 12F),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var big = new Label
                {
                    Text = code,
                    ForeColor = Color.FromArgb(96, 165, 250),
                    Font = new Font("Consolas", 60F, FontStyle.Bold),
                    Dock = DockStyle.Top, Height = 110,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var header = new Label
                {
                    Text = "YOUR QUEUE NUMBER",
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                    Dock = DockStyle.Top, Height = 50,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                // Add Fill first, then Bottom (buttons/note/qr), then Top (number/header).
                dlg.Controls.Add(list);
                dlg.Controls.Add(ok);
                dlg.Controls.Add(note);
                if (claimPanel != null) dlg.Controls.Add(claimPanel);
                dlg.Controls.Add(big);
                dlg.Controls.Add(header);
                dlg.ShowDialog();
            }
        }

        private static bool Blank(TextBox t) => string.IsNullOrWhiteSpace(t.Text);
        private void Warn(string m) =>
            MessageBox.Show(m, "Please check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    // ============================ modern UI controls ============================

    /// <summary>A panel with rounded corners, an optional soft shadow, and a border.</summary>
    internal class RoundPanel : Panel
    {
        public int Radius = 12;
        public Color Fill = Color.White;
        public Color BorderColor = Color.Empty;
        public float BorderWidth = 1f;
        public int Shadow = 0;        // px of soft drop shadow (0 = none)

        public RoundPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        /// <summary>Rounded-rectangle path (also used to draw the camera guide + pills).</summary>
        public static GraphicsPath Round(Rectangle r, int rad)
        {
            int d = Math.Max(1, rad * 2);
            var p = new GraphicsPath();
            if (d >= r.Width || d >= r.Height) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var full = ClientRectangle;

            if (Shadow > 0)
            {
                for (int i = Shadow; i >= 1; i--)
                {
                    var sr = new Rectangle(full.X + i / 2, full.Y + i,
                        full.Width - i - 1, full.Height - i - 1);
                    int a = Math.Max(3, 26 - i * 3);
                    using (var sp = Round(sr, Radius))
                    using (var sb = new SolidBrush(Color.FromArgb(a, 0, 0, 0)))
                        g.FillPath(sb, sp);
                }
            }

            var rr = new Rectangle(full.X, full.Y,
                full.Width - Shadow - 1, full.Height - Shadow - 1);
            using (var path = Round(rr, Radius))
            {
                using (var b = new SolidBrush(Fill)) g.FillPath(b, path);
                if (BorderColor != Color.Empty)
                    using (var pen = new Pen(BorderColor, BorderWidth)) g.DrawPath(pen, path);
            }
        }
    }

    /// <summary>A selectable priority-lane pill: icon + label, rounded, hover + selected state.</summary>
    internal class PillToggle : Panel
    {
        private readonly string _glyph;
        private readonly string _text;
        private bool _hover;

        public bool Checked { get; private set; }
        public event EventHandler CheckedChanged;

        private static readonly Color Accent = Color.FromArgb(13, 110, 253);
        private static readonly Color SelBg = Color.FromArgb(232, 240, 254);
        private static readonly Color Line = Color.FromArgb(206, 212, 218);
        private static readonly Color Ink = Color.FromArgb(33, 37, 41);

        public PillToggle(string glyph, string text)
        {
            _glyph = glyph;
            _text = text;
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Click += (s, e) => Toggle();
            MouseEnter += (s, e) => { _hover = true; Invalidate(); };
            MouseLeave += (s, e) => { _hover = false; Invalidate(); };
        }

        private void Toggle()
        {
            Checked = !Checked;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetChecked(bool v)
        {
            if (Checked == v) return;
            Checked = v;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);

            Color border = Checked ? Accent : (_hover ? Accent : Line);
            Color fill = Checked ? SelBg : (_hover ? Color.FromArgb(248, 250, 253) : Color.White);
            using (var path = RoundPanel.Round(r, Height / 2))
            {
                using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                using (var pen = new Pen(border, Checked ? 2f : 1.4f)) g.DrawPath(pen, path);
            }

            using (var fg = new Font("Segoe UI Emoji", 15F))
                TextRenderer.DrawText(g, _glyph, fg, new Rectangle(16, 0, 34, Height),
                    Ink, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            using (var ft = new Font("Segoe UI", 11.5F, Checked ? FontStyle.Bold : FontStyle.Regular))
                TextRenderer.DrawText(g, _text, ft, new Rectangle(58, 0, Width - 100, Height),
                    Checked ? Accent : Ink, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            // selected check mark on the right
            if (Checked)
                using (var fc = new Font("Segoe UI", 12F, FontStyle.Bold))
                    TextRenderer.DrawText(g, "✓", fc, new Rectangle(Width - 44, 0, 32, Height),
                        Accent, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        }
    }

    /// <summary>A numbered step indicator (done ✓ / current / upcoming) with labels.</summary>
    internal class StepIndicator : Panel
    {
        private readonly string[] _labels;
        private int _current;

        private static readonly Color Accent = Color.FromArgb(13, 110, 253);
        private static readonly Color Green = Color.FromArgb(25, 135, 84);
        private static readonly Color Idle = Color.FromArgb(206, 212, 218);
        private static readonly Color Muted = Color.FromArgb(108, 117, 125);

        public StepIndicator(string[] labels)
        {
            _labels = labels ?? new string[0];
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        public void SetStep(int i) { _current = i; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int d = 30, y = (Height - d) / 2, x = 0;
            using (var fNum = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (var fCur = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (var fOther = new Font("Segoe UI", 11F))
            {
                for (int i = 0; i < _labels.Length; i++)
                {
                    bool done = i < _current;
                    bool cur = i == _current;
                    Color c = done ? Green : (cur ? Accent : Idle);

                    using (var b = new SolidBrush(c)) g.FillEllipse(b, x, y, d, d);
                    string num = done ? "✓" : (i + 1).ToString();
                    TextRenderer.DrawText(g, num, fNum, new Rectangle(x, y, d, d), Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    var lf = cur ? fCur : fOther;
                    Color lc = cur ? Accent : Muted;
                    int lblW = TextRenderer.MeasureText(_labels[i], lf).Width;
                    TextRenderer.DrawText(g, _labels[i], lf,
                        new Rectangle(x + d + 8, y, lblW + 4, d), lc,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                    x += d + 8 + lblW + 26;
                    if (i < _labels.Length - 1)
                    {
                        using (var pen = new Pen(Idle, 2f))
                            g.DrawLine(pen, x - 20, y + d / 2, x - 4, y + d / 2);
                    }
                }
            }
        }
    }
}
