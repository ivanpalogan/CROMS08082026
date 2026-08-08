using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Queue Management — live queue and window assignment.
    /// Tickets are issued by the client kiosk (CROMS.Kiosk); this screen calls the
    /// next waiting ticket (priority lane first) to a service window and shows
    /// Now Serving + Live Queue from the `queue_tickets` table. Double-click a
    /// live-queue row to see all services bundled on that ticket.
    /// Controls are placed in the Designer.
    /// </summary>
    public partial class QueueManagementForm : Form, IRefreshable
    {
        private bool _paused;

        private ClientDisplayForm _display;

        public QueueManagementForm()
        {
            InitializeComponent();
            WireQueueDoubleClick();
            SetupServingArea();
            SetupWorkflowToolbar();
            AddDisplayButton();
            SetupSyncTimer();
            RefreshAll();
        }

        // service_code (from queue_ticket_services) → module key to open.
        private static readonly System.Collections.Generic.Dictionary<string, string> ServiceModule =
            new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "CTC",      "certrequest" },
                { "MARRIAGE", "marriage"    },
                { "DEATH",    "death"       },
                { "PETITION", "petitions"   },
                { "VERIFY",   "certrequest" },
                { "CLAIM",    "release"     },   // pickup: Release & Claim → Claim by QR
                // NEWREG is handled specially (asks Birth / Marriage / Death).
            };

        // Now Serving is built dynamically from the active `windows` rows, so there is
        // no fixed number of windows. Window-based access control: when an operator is
        // signed in to a specific window, ONLY that window is shown and controllable
        // here; an admin / monitor with no claimed window sees every active window.
        // The card grid is rebuilt only when the shown-window set changes (signature),
        // and the live text/colours are updated in place each refresh, so the board can
        // refresh on a timer with no flicker and without dropping a click.
        private TableLayoutPanel _servingGrid;
        private string _servingSig = "";
        private readonly Dictionary<int, Panel> _cardByWin = new Dictionary<int, Panel>();
        private readonly Dictionary<int, Label> _titleByWin = new Dictionary<int, Label>();
        private readonly Dictionary<int, Label> _presByWin = new Dictionary<int, Label>();
        private readonly Dictionary<int, Label> _codeByWin = new Dictionary<int, Label>();
        private readonly Dictionary<int, Label> _subByWin = new Dictionary<int, Label>();

        private void SetupServingArea()
        {
            grpServing.Controls.Clear();   // drop the 3 hardcoded designer cards
            _servingGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 8, 12, 8),
                AutoScroll = true,
                BackColor = grpServing.BackColor
            };
            grpServing.Controls.Add(_servingGrid);

            // Header shows the access scope so the operator knows this station only
            // controls its own window.
            grpServing.Text = Session.HasWindow
                ? "NOW SERVING — " + (Session.WindowName ?? ("Window " + Session.WindowId)) + " (your window)"
                : "NOW SERVING";
        }

        /// <summary>
        /// The active windows this workstation may see and control: only the operator's
        /// own claimed window, or every active window when none is claimed (admin / monitor).
        /// </summary>
        private static DataTable ActiveWindows()
        {
            string sql = "SELECT id, window_name FROM windows WHERE status = 'Active'";
            if (Session.HasWindow) sql += " AND id = " + Session.WindowId;
            sql += " ORDER BY display_order, id";
            return Db.Pull(sql);
        }

        /// <summary>
        /// Rebuilds the card layout only when the shown-window set changes, then updates
        /// each card's live ticket / presence in place. Cards fill the row for a few
        /// windows and wrap for many, so adding or removing a window changes the board
        /// with no code edit.
        /// </summary>
        private void RefreshServing()
        {
            if (_servingGrid == null) return;
            DataTable wins = ActiveWindows();

            var sig = new StringBuilder();
            foreach (DataRow w in wins.Rows)
                sig.Append(w["id"]).Append(':').Append(w["window_name"]).Append('|');
            if (sig.ToString() != _servingSig)
            {
                RebuildServingGrid(wins);
                _servingSig = sig.ToString();
            }
            UpdateServingCards(wins);
        }

        private void RebuildServingGrid(DataTable wins)
        {
            _servingGrid.SuspendLayout();
            _servingGrid.Controls.Clear();
            _servingGrid.ColumnStyles.Clear();
            _servingGrid.RowStyles.Clear();
            _cardByWin.Clear(); _titleByWin.Clear();
            _presByWin.Clear(); _codeByWin.Clear(); _subByWin.Clear();

            if (wins.Rows.Count == 0)
            {
                _servingGrid.ColumnCount = 1;
                _servingGrid.RowCount = 1;
                _servingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                _servingGrid.Controls.Add(new Label
                {
                    Text = Session.HasWindow
                        ? "Your window is not active. Ask an admin to activate it in Settings."
                        : "No active windows. Add one in Settings (Administration).",
                    AutoSize = true,
                    ForeColor = Color.FromArgb(108, 117, 125),
                    Font = new Font("Segoe UI", 10F),
                    Margin = new Padding(8)
                }, 0, 0);
                _servingGrid.ResumeLayout();
                return;
            }

            // Equal-width columns (up to 4 across, then wrap to more rows) so the cards
            // always fill the whole board width — no dead space on the right.
            int count = wins.Rows.Count;
            int cols = Math.Min(count, 4);
            int rows = (int)Math.Ceiling(count / (double)cols);
            _servingGrid.ColumnCount = cols;
            _servingGrid.RowCount = rows;
            for (int c = 0; c < cols; c++)
                _servingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));
            for (int r = 0; r < rows; r++)
                _servingGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));

            int i = 0;
            foreach (DataRow w in wins.Rows)
            {
                int id = Convert.ToInt32(w["id"]);
                string name = w["window_name"].ToString();
                _servingGrid.Controls.Add(CreateWindowCard(id, name), i % cols, i / cols);
                i++;
            }

            _servingGrid.ResumeLayout();
        }

        /// <summary>Builds one window card with persistent labels (updated in place later).</summary>
        private Panel CreateWindowCard(int windowId, string windowName)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8),
                MinimumSize = new Size(0, 140),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            var title = new Label
            {
                Text = windowName.ToUpper(),
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(108, 117, 125),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            var presence = new Label
            {
                Text = "",
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            var codeLbl = new Label
            {
                Text = "—",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(13, 110, 253),
                Font = new Font("Consolas", 26F, FontStyle.Bold)
            };
            var subLbl = new Label
            {
                Text = "",
                Dock = DockStyle.Bottom,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5F)
            };

            card.Controls.Add(codeLbl);
            card.Controls.Add(subLbl);
            card.Controls.Add(presence);
            card.Controls.Add(title);

            // Responsive text: the ticket code (and services subtitle) shrink to fit
            // their card so a long queue code never clips — see AutoFitText.
            AutoFitText.Attach(codeLbl, 26F, 9F);
            AutoFitText.Attach(subLbl, 8.5F, 6.5F);

            EventHandler click = (s, e) => WindowClicked(windowId);
            card.Click += click;
            title.Click += click;
            presence.Click += click;
            codeLbl.Click += click;
            subLbl.Click += click;

            _cardByWin[windowId] = card;
            _titleByWin[windowId] = title;
            _presByWin[windowId] = presence;
            _codeByWin[windowId] = codeLbl;
            _subByWin[windowId] = subLbl;
            return card;
        }

        /// <summary>Updates each card's current ticket, presence and colour in place.</summary>
        private void UpdateServingCards(DataTable wins)
        {
            foreach (DataRow w in wins.Rows)
            {
                int id = Convert.ToInt32(w["id"]);
                if (!_codeByWin.ContainsKey(id)) continue;

                DataTable dt = Db.Pull(
                    "SELECT ticket_code, type_label, status FROM queue_tickets " +
                    "WHERE status IN ('Accepted','Serving') AND window_no = @w AND DATE(created_at) = CURDATE() " +
                    "ORDER BY id DESC LIMIT 1", new MySqlParameter("@w", id));
                bool serving = dt.Rows.Count > 0;
                bool accepted = serving &&
                    string.Equals(dt.Rows[0]["status"].ToString(), "Accepted", StringComparison.OrdinalIgnoreCase);

                DataTable p = Db.Pull(
                    "SELECT operator_name, (current_operator IS NOT NULL AND last_heartbeat > " +
                    "(NOW() - INTERVAL " + WindowAssignmentForm.StaleMinutes + " MINUTE)) AS online " +
                    "FROM windows WHERE id = @w", new MySqlParameter("@w", id));
                bool online = p.Rows.Count > 0 && p.Rows[0]["online"] != DBNull.Value
                    && Convert.ToInt32(p.Rows[0]["online"]) == 1;
                string operatorName = p.Rows.Count > 0 && p.Rows[0]["operator_name"] != DBNull.Value
                    ? p.Rows[0]["operator_name"].ToString() : null;

                string services = serving && dt.Rows[0]["type_label"] != DBNull.Value
                    ? dt.Rows[0]["type_label"].ToString()
                    : AssignedLabel(id);
                // Accepted tickets show a "ready to call" hint (they are NOT on the public
                // board yet); Serving tickets show the requested services.
                if (accepted) services = "▶ Ready — click to Call Client";

                _codeByWin[id].Text = serving ? dt.Rows[0]["ticket_code"].ToString() : "—";
                _subByWin[id].Text = services;
                _subByWin[id].ForeColor = accepted
                    ? Color.FromArgb(255, 140, 0)
                    : (serving ? Color.FromArgb(33, 37, 41) : Color.FromArgb(148, 163, 184));
                _presByWin[id].Text = online
                    ? "● Online" + (operatorName != null ? " · " + operatorName : "")
                    : "○ Offline";
                _presByWin[id].ForeColor = online
                    ? Color.FromArgb(25, 135, 84) : Color.FromArgb(173, 181, 189);
                _cardByWin[id].BackColor = online
                    ? Color.White : Color.FromArgb(248, 249, 250);
            }
        }

        /// <summary>The transactions a window is assigned to handle, for the card subtitle.</summary>
        private static string AssignedLabel(int windowId)
        {
            DataTable dt = Db.Pull(
                "SELECT service_code FROM window_transactions WHERE window_id = @w ORDER BY id",
                new MySqlParameter("@w", windowId));
            if (dt.Rows.Count == 0) return "All transactions";

            var labels = new System.Collections.Generic.List<string>();
            foreach (DataRow r in dt.Rows)
            {
                string c = r["service_code"].ToString();
                string label = c;
                foreach (var svc in WindowAssignmentForm.Catalogue)
                    if (svc.Code == c) { label = svc.Label; break; }
                labels.Add(label);
            }
            return string.Join(", ", labels);
        }

        /// <summary>
        /// Sequential same-ticket processing. Clicking a window works through the
        /// ticket's services one at a time, in the order they were selected
        /// (queue_ticket_services ordered by id), keeping the SAME ticket number:
        ///   • first click opens the first Pending service and marks it 'Serving';
        ///   • the next click confirms that service is done → 'Completed', then opens
        ///     the next Pending service;
        ///   • when the last service is completed the whole ticket is marked Completed
        ///     and the window is freed.
        /// Because each service's status is stored, work resumes from the last
        /// unfinished service after a refresh, and the ticket stays on the Now Serving
        /// board until every service is done.
        /// </summary>
        private void WindowClicked(int window)
        {
            // Window-based access control: an operator signed in to a window may only
            // process that window's tickets. (Defence in depth — a restricted board
            // only shows the operator's own card, so this normally can't be reached.)
            if (Session.HasWindow && window != Session.WindowId)
            {
                MessageBox.Show(
                    "You can only process tickets at your own window (" +
                    (Session.WindowName ?? ("Window " + Session.WindowId)) + ").",
                    "Window locked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable t = Db.Pull(
                "SELECT id, ticket_code, status FROM queue_tickets " +
                "WHERE status IN ('Accepted','Serving') AND window_no = @w AND DATE(created_at) = CURDATE() " +
                "ORDER BY id DESC LIMIT 1", new MySqlParameter("@w", window));
            if (t.Rows.Count == 0) return;   // idle window — nothing to process

            int ticketId = Convert.ToInt32(t.Rows[0]["id"]);
            string code = t.Rows[0]["ticket_code"].ToString();

            // Accepted but not yet called: clicking the card calls the client first
            // (Serving + voice callout + on the public board), then processing begins.
            if (string.Equals(t.Rows[0]["status"].ToString(), "Accepted", StringComparison.OrdinalIgnoreCase))
            {
                DoCallClient(ticketId, code, window);
                return;
            }

            DataTable svc = Db.Pull(
                "SELECT id, service_code, service_label, status FROM queue_ticket_services " +
                "WHERE ticket_id = @id ORDER BY id", new MySqlParameter("@id", ticketId));

            // Legacy / single-service ticket with no service rows: keep the old one-shot
            // behaviour — open the one form, and completing frees the window.
            if (svc.Rows.Count == 0)
            {
                FinishTicket(ticketId, code, window);
                return;
            }

            // A service already in progress? Clicking again means "I finished it".
            DataRow serving = FindRow(svc, "Serving");
            if (serving != null)
            {
                DialogResult done = MessageBox.Show(
                    code + " — mark '" + serving["service_label"] + "' as completed?",
                    "Service done?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (done == DialogResult.Cancel)
                {
                    OpenServiceForm(serving["service_code"].ToString(), ticketId, code);
                    return;   // reopened, still in progress
                }
                if (done == DialogResult.No)
                {
                    OpenServiceForm(serving["service_code"].ToString(), ticketId, code);
                    return;   // not done yet — reopen the same form
                }
                Db.Push("UPDATE queue_ticket_services SET status = 'Completed' WHERE id = @id",
                    new MySqlParameter("@id", Convert.ToInt32(serving["id"])));
            }

            // Open the next Pending service (first by order), marking it Serving.
            DataRow next = FindRow(svc, "Pending");
            // re-pull after the completion above so a just-completed row isn't picked
            if (serving != null)
            {
                svc = Db.Pull(
                    "SELECT id, service_code, service_label, status FROM queue_ticket_services " +
                    "WHERE ticket_id = @id ORDER BY id", new MySqlParameter("@id", ticketId));
                next = FindRow(svc, "Pending");
            }

            if (next != null)
            {
                Db.Push("UPDATE queue_ticket_services SET status = 'Serving' WHERE id = @id",
                    new MySqlParameter("@id", Convert.ToInt32(next["id"])));
                OpenServiceForm(next["service_code"].ToString(), ticketId, code);
                RefreshAll();
                MessageBox.Show(
                    code + " — now serving: " + next["service_label"] +
                    "\nComplete and save this form, then click " + WindowName(window) +
                    " again for the next service.",
                    "Now serving", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // No Pending left — every service done. Finish the whole ticket.
            FinishTicket(ticketId, code, window);
        }

        /// <summary>Marks the ticket Completed and frees the window.</summary>
        private void FinishTicket(int ticketId, string code, int window)
        {
            // Section 8 — stamp the completion time + final status for processing-time
            // monitoring. window_no kept NULL-able but we retain accepted_window history.
            Db.Push("UPDATE queue_tickets SET status = 'Completed', final_status = 'Completed', " +
                    "completed_at = NOW(), window_no = NULL WHERE id = @id",
                new MySqlParameter("@id", ticketId));
            Audit.Write("Update", "queue_tickets", ticketId, "Completed " + code);
            RefreshAll();
            MessageBox.Show(
                code + " — all services completed. " + WindowName(window) +
                " is now free; press Call Next for the next client.",
                "Ticket completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static DataRow FindRow(DataTable dt, string status)
        {
            foreach (DataRow r in dt.Rows)
                if (string.Equals(r["status"].ToString(), status, StringComparison.OrdinalIgnoreCase))
                    return r;
            return null;
        }

        /// <summary>Walks up to the shell and opens the module for a service code.</summary>
        private void OpenServiceForm(string serviceCode, int ticketId, string ticketCode)
        {
            MainForm shell = Shell();
            if (shell == null) return;

            string key;
            if (serviceCode.Equals("NEWREG", StringComparison.OrdinalIgnoreCase))
            {
                key = PromptRecordType();          // Birth / Marriage / Death
                if (key == null) return;
            }
            else if (!ServiceModule.TryGetValue(serviceCode, out key))
            {
                MessageBox.Show("No form is mapped to service '" + serviceCode + "' yet.",
                    "Queue", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Form form = shell.GoToModule(key);
            // Pre-fill the opened form with what the client entered on the kiosk
            // (name, contact, photo) so staff don't re-key it.
            if (key == "birth" && form is BirthRegistrationForm birth)
                birth.PrepareForQueueTicket(ticketId);
            else if (key == "certrequest" && form is CertificateRequestForm cert)
                cert.PrepareForQueueTicket(ticketId, ticketCode);
        }

        /// <summary>Walks up the control tree to the application shell (MainForm).</summary>
        private MainForm Shell()
        {
            for (Control p = Parent; p != null; p = p.Parent)
                if (p is MainForm mf) return mf;
            return null;
        }

        /// <summary>
        /// Small modal chooser for a New Registration service: returns the module key
        /// "birth" / "marriage" / "death", or null if closed without choosing.
        /// </summary>
        private static string PromptRecordType()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Registration";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = false;
                dlg.ClientSize = new System.Drawing.Size(320, 120);

                dlg.Controls.Add(new Label
                {
                    Text = "What is this client registering?",
                    AutoSize = true,
                    Location = new System.Drawing.Point(16, 16),
                    Font = new System.Drawing.Font("Segoe UI", 9.75F)
                });

                string chosen = null;
                string[] labels = { "Birth", "Marriage", "Death" };
                string[] keys = { "birth", "marriage", "death" };
                for (int i = 0; i < labels.Length; i++)
                {
                    var b = new Button
                    {
                        Text = labels[i],
                        Tag = keys[i],
                        Location = new System.Drawing.Point(16 + i * 102, 56),
                        Size = new System.Drawing.Size(86, 40),
                        FlatStyle = FlatStyle.Flat,
                        Font = new System.Drawing.Font("Segoe UI", 10F)
                    };
                    b.Click += (s, e) =>
                    {
                        chosen = (string)((Button)s).Tag;
                        dlg.DialogResult = DialogResult.OK;
                    };
                    dlg.Controls.Add(b);
                }

                dlg.ShowDialog();
                return chosen;
            }
        }

        /// <summary>
        /// Adds a header button that opens the public, view-only "Now Serving" board
        /// (a separate full-screen window for the waiting area).
        /// </summary>
        private void AddDisplayButton()
        {
            var btn = new Button
            {
                Text = "Client Display",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10F),
                Location = new System.Drawing.Point(965, 19),
                Size = new System.Drawing.Size(140, 35)
            };
            btn.Click += (s, e) =>
            {
                if (_display == null || _display.IsDisposed)
                    _display = new ClientDisplayForm();
                _display.Show();
                _display.BringToFront();
            };
            Controls.Add(btn);
            btn.BringToFront();
        }

        // ================================================================
        //  Workflow toolbar: Call Client, Recall, Forward to Another Window
        //  (spec sections 4 and 6). Buttons are added in code — the Now Serving
        //  cards are already built dynamically, so the toolbar is too.
        // ================================================================

        private Button _btnCallClient, _btnRecall, _btnForward;

        private void SetupWorkflowToolbar()
        {
            _btnCallClient = MakeToolButton("Call Client", 310, Color.FromArgb(25, 135, 84), true);
            _btnCallClient.Click += (s, e) => CallClient();

            _btnRecall = MakeToolButton("Recall", 452, Color.FromArgb(255, 193, 7), false);
            _btnRecall.ForeColor = Color.FromArgb(33, 37, 41);
            _btnRecall.Click += (s, e) => RecallCurrent();

            _btnForward = MakeToolButton("Forward →", 566, Color.FromArgb(111, 66, 193), true);
            _btnForward.Click += (s, e) => ForwardCurrent();
        }

        private Button MakeToolButton(string text, int x, Color back, bool whiteText)
        {
            var b = new Button
            {
                Text = text,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = whiteText ? Color.White : Color.FromArgb(33, 37, 41),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(x, 19),
                Size = new Size(text.Length > 8 ? 138 : 110, 35),
                UseVisualStyleBackColor = false
            };
            Controls.Add(b);
            b.BringToFront();
            return b;
        }

        // Voice announcement (waiting-area callout). Uses System.Speech on the staff
        // machine's speakers; guarded so a PC with no audio never crashes the flow.
        private System.Speech.Synthesis.SpeechSynthesizer _voice;

        private void Announce(string ticketCode, string windowName)
        {
            try
            {
                if (_voice == null) _voice = new System.Speech.Synthesis.SpeechSynthesizer();
                string spoken = "Now serving " + Spell(ticketCode) + ", please proceed to " + windowName + ".";
                _voice.SpeakAsync(spoken);
            }
            catch { /* no audio device / speech engine — silent, callout still shows on screen */ }
        }

        // "MARRIAGE-100" reads better spoken as "MARRIAGE 1 0 0".
        private static string Spell(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            var sb = new StringBuilder();
            foreach (char c in code.Replace('-', ' '))
                sb.Append(char.IsDigit(c) ? " " + c : c.ToString());
            return sb.ToString();
        }

        /// <summary>The ticket currently at the operator's window (Accepted or Serving).</summary>
        private DataRow CurrentTicket(out int window)
        {
            window = OperatingWindow();
            if (window == 0) return null;
            DataTable t = Db.Pull(
                "SELECT id, ticket_code, status FROM queue_tickets " +
                "WHERE status IN ('Accepted','Serving') AND window_no = @w AND DATE(created_at) = CURDATE() " +
                "ORDER BY id DESC LIMIT 1", new MySqlParameter("@w", window));
            return t.Rows.Count > 0 ? t.Rows[0] : null;
        }

        /// <summary>
        /// The single window this toolbar acts on: the operator's own window, or — for an
        /// admin/monitor with no claimed window — the one active window currently holding a
        /// ticket (ambiguous if several, so it asks the operator to use the card instead).
        /// </summary>
        private int OperatingWindow()
        {
            if (Session.HasWindow) return Session.WindowId;
            DataTable busy = Db.Pull(
                "SELECT DISTINCT window_no FROM queue_tickets " +
                "WHERE status IN ('Accepted','Serving') AND window_no IS NOT NULL AND DATE(created_at) = CURDATE()");
            if (busy.Rows.Count == 1) return Convert.ToInt32(busy.Rows[0]["window_no"]);
            return 0;   // 0 or many → can't disambiguate from the toolbar
        }

        /// <summary>
        /// Section 6 — the client is only called AFTER the records assistant has located
        /// the documents. Call Next merely ACCEPTS a ticket to a window (status Accepted,
        /// off the public board); this flips it to Serving, stamps started_at, shows it on
        /// the Now Serving display, and plays the voice callout.
        /// </summary>
        private void CallClient()
        {
            int window;
            DataRow t = CurrentTicket(out window);
            if (t == null)
            {
                MessageBox.Show(Session.HasWindow || window != 0
                    ? "No accepted ticket at this window. Press Call Next to accept the next client."
                    : "Open your window's card to call its client (multiple windows are busy).",
                    "Call Client", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int id = Convert.ToInt32(t["id"]);
            string code = t["ticket_code"].ToString();
            DoCallClient(id, code, window);
        }

        private void DoCallClient(int ticketId, string code, int window)
        {
            Db.Push(
                "UPDATE queue_tickets SET status = 'Serving', called_at = NOW(), " +
                "started_at = COALESCE(started_at, NOW()) WHERE id = @id",
                new MySqlParameter("@id", ticketId));
            string wname = WindowName(window);
            Audit.Write("Update", "queue_tickets", ticketId, "Called client " + code + " to " + wname);
            Announce(code, wname);
            RefreshAll();
            MessageBox.Show(code + " — now called to " + wname + ".\n" +
                "Click the window card to process the requested services.",
                "Client called", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Section 4 — Recall. Re-calls the SAME queue number to the same window (e.g. the
        /// client stepped away, or was told to wait for the document). Bumps recall_count,
        /// replays the voice callout, and refreshes the display.
        /// </summary>
        private void RecallCurrent()
        {
            int window;
            DataRow t = CurrentTicket(out window);
            if (t == null)
            {
                MessageBox.Show("No active ticket at this window to recall.", "Recall",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int id = Convert.ToInt32(t["id"]);
            string code = t["ticket_code"].ToString();
            // Recalling also (re)serves it so it shows on the public board.
            Db.Push("UPDATE queue_tickets SET status = 'Serving', recall_count = recall_count + 1, " +
                    "called_at = NOW(), started_at = COALESCE(started_at, NOW()) WHERE id = @id",
                    new MySqlParameter("@id", id));
            string wname = WindowName(window);
            Audit.Write("Update", "queue_tickets", id, "Recalled " + code + " to " + wname);
            Announce(code, wname);
            RefreshAll();
            MessageBox.Show(code + " recalled to " + wname + ".", "Recall",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Section 4 — Forward to Another Window. Forwards the ticket's remaining
        /// (pending) service to a window assigned to that service, keeping the SAME queue
        /// number and full history (queue_ticket_forwards). A completed service is left
        /// completed; the ticket moves as 'Accepted' at the destination. A Priority Window
        /// never forwards (section 1) unless an administrator overrides.
        /// </summary>
        private void ForwardCurrent()
        {
            int window;
            DataRow t = CurrentTicket(out window);
            if (t == null)
            {
                MessageBox.Show("No active ticket at this window to forward.", "Forward",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int id = Convert.ToInt32(t["id"]);
            string code = t["ticket_code"].ToString();

            if (IsPriorityWindow(window) && !AdminOverride(
                "This is a Priority Window — its tickets stay until completion and are not " +
                "normally forwarded. An administrator override is required to forward."))
                return;

            // Which service to forward = the first still-pending service on the ticket.
            DataTable svc = Db.Pull(
                "SELECT id, service_code, service_label FROM queue_ticket_services " +
                "WHERE ticket_id = @id AND status IN ('Pending','Serving') ORDER BY id LIMIT 1",
                new MySqlParameter("@id", id));
            if (svc.Rows.Count == 0)
            {
                MessageBox.Show("No remaining service to forward — every service on " + code +
                    " is already completed.", "Forward",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string svcCode = svc.Rows[0]["service_code"].ToString();
            string svcLabel = svc.Rows[0]["service_label"].ToString();
            int svcId = Convert.ToInt32(svc.Rows[0]["id"]);

            // Destination = an active window (other than this one) assigned to that service.
            int dest = PickDestinationWindow(svcCode, window);
            if (dest == 0)
            {
                MessageBox.Show("No other window is assigned to '" + svcLabel + "'.\n" +
                    "Assign a window to that transaction (Window Assignment) first.",
                    "Cannot forward", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string destName = WindowName(dest);
            if (MessageBox.Show("Forward '" + svcLabel + "' on " + code + " to " + destName +
                    "?\nThe queue number stays the same; completed services are kept.",
                    "Forward to Another Window", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes) return;

            // Reset the forwarded service to Pending at the destination, move the ticket.
            Db.Push("UPDATE queue_ticket_services SET status = 'Pending', locked_by_window = @d WHERE id = @s",
                new MySqlParameter("@d", dest), new MySqlParameter("@s", svcId));
            Db.Push("UPDATE queue_tickets SET status = 'Accepted', window_no = @d, " +
                    "accepted_window = @d, accepted_at = NOW(), called_at = NULL WHERE id = @id",
                new MySqlParameter("@d", dest), new MySqlParameter("@id", id));
            Db.Push("INSERT INTO queue_ticket_forwards " +
                    "(ticket_id, from_window, to_window, service_code, service_label, forwarded_by) " +
                    "VALUES (@t, @f, @to, @c, @l, @by)",
                new MySqlParameter("@t", id), new MySqlParameter("@f", window),
                new MySqlParameter("@to", dest), new MySqlParameter("@c", svcCode),
                new MySqlParameter("@l", svcLabel),
                new MySqlParameter("@by", Session.UserIdParam));
            Audit.Write("Update", "queue_tickets", id,
                "Forwarded " + code + " (" + svcLabel + ") " + WindowName(window) + " → " + destName);
            RefreshAll();
            MessageBox.Show(code + " forwarded to " + destName + " for '" + svcLabel + "'.\n" +
                destName + " can now Call Next / Call Client for it.", "Forwarded",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static bool IsPriorityWindow(int windowId) =>
            Db.GetCount("SELECT id FROM windows WHERE id = " + windowId + " AND is_priority = 1") > 0;

        /// <summary>Requires an Admin/Registrar re-verification to override a rule.</summary>
        private bool AdminOverride(string reason)
        {
            MessageBox.Show(reason, "Administrator override required",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            using (var v = new AdminVerificationForm())
                return v.ShowDialog(this) == DialogResult.OK;
        }

        /// <summary>An active window other than <paramref name="exclude"/> assigned to the service.</summary>
        private static int PickDestinationWindow(string code, int exclude)
        {
            DataTable wins = Db.Pull(
                "SELECT id FROM windows WHERE status = 'Active' AND id <> @x ORDER BY display_order, id",
                new MySqlParameter("@x", exclude));
            foreach (DataRow w in wins.Rows)
            {
                int id = Convert.ToInt32(w["id"]);
                if (WindowHandles(id, code)) return id;
            }
            return 0;
        }

        /// <summary>
        /// Double-clicking a live-queue row shows every service the client requested on
        /// that one ticket (the kiosk lets a client bundle several services under one
        /// queue number, stored in `queue_ticket_services`). This is how staff retrieve
        /// all requested services from the same ticket.
        /// </summary>
        private void WireQueueDoubleClick()
        {
            dgvQueue.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                if (!dgvQueue.Columns.Contains("id")) return;
                object idVal = dgvQueue.Rows[e.RowIndex].Cells["id"].Value;
                if (idVal == null || idVal == DBNull.Value) return;
                ShowTicketServices(Convert.ToInt32(idVal),
                    dgvQueue.Rows[e.RowIndex].Cells["Queue No"].Value?.ToString() ?? "");
            };
        }

        /// <summary>Lists the services bundled on one ticket (falls back to type_label).</summary>
        private void ShowTicketServices(int ticketId, string code)
        {
            DataTable dt = Db.Pull(
                "SELECT service_label, status FROM queue_ticket_services " +
                "WHERE ticket_id = @id ORDER BY id", new MySqlParameter("@id", ticketId));

            string body;
            if (dt.Rows.Count == 0)
            {
                DataTable one = Db.Pull(
                    "SELECT type_label FROM queue_tickets WHERE id = @id",
                    new MySqlParameter("@id", ticketId));
                string label = one.Rows.Count > 0 && one.Rows[0]["type_label"] != DBNull.Value
                    ? one.Rows[0]["type_label"].ToString() : "(none)";
                body = "Requested service:\n  • " + label;
            }
            else
            {
                var sb = new StringBuilder("Requested services on this ticket:\n");
                foreach (DataRow r in dt.Rows)
                    sb.AppendLine("  • " + r["service_label"] + "   [" + r["status"] + "]");
                body = sb.ToString();
            }

            MessageBox.Show(body, "Ticket " + code,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Re-pulls the queue and windows whenever this module is shown again — so a
        /// ticket auto-finished elsewhere (e.g. a CTC request created in Certificate
        /// Request) shows its window freed the moment you return here.
        /// </summary>
        public void RefreshData() => RefreshAll();

        private void RefreshAll()
        {
            ReconcileApprovals();
            LoadQueue();
            RefreshServing();
        }

        // ---- real-time synchronization ----

        private Timer _syncTimer;
        private Label _lblSync;
        private bool _online = true;

        /// <summary>
        /// Starts the live-refresh timer and a small connection indicator. Multiple
        /// workstations share one database, so changes made elsewhere (a ticket called,
        /// completed, or a record updated) must appear here on their own.
        /// </summary>
        private void SetupSyncTimer()
        {
            _lblSync = new Label
            {
                AutoSize = true,
                Text = "● Live",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 135, 84),
                BackColor = Color.Transparent,
                Location = new Point(1115, 27),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Controls.Add(_lblSync);
            _lblSync.BringToFront();

            _syncTimer = new Timer { Interval = 3000 };
            _syncTimer.Tick += (s, e) => SyncTick();
            _syncTimer.Start();
        }

        /// <summary>
        /// One live-refresh pass. Wrapped so a dropped or slow connection never crashes
        /// the timer: on failure it shows "Reconnecting…" and simply retries next tick.
        /// When the database comes back, the next successful tick re-queries the full
        /// current state, so any updates missed while offline are picked up automatically
        /// — no manual reload needed. Retries once immediately to ride out a brief blip.
        /// </summary>
        private void SyncTick()
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    RefreshAll();
                    if (!_online) _online = true;   // reconnected → this pass caught us up
                    _lblSync.Text = "● Live";
                    _lblSync.ForeColor = Color.FromArgb(25, 135, 84);
                    return;
                }
                catch
                {
                    // brief pause then one retry before giving up until next tick
                    if (attempt == 0) System.Threading.Thread.Sleep(150);
                }
            }
            _online = false;
            _lblSync.Text = "● Reconnecting…";
            _lblSync.ForeColor = Color.FromArgb(220, 53, 69);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _syncTimer?.Stop();
            _syncTimer?.Dispose();
            base.OnFormClosed(e);
        }

        /// <summary>
        /// Second-visit bridge: any "Pending Approval" ticket whose linked birth record
        /// has since been approved (Registered) flips to "For Receiving", so it can be
        /// called again for the client to collect the copy.
        /// </summary>
        private void ReconcileApprovals()
        {
            try
            {
                Db.Push(
                    "UPDATE queue_tickets q JOIN births b ON b.id = q.birth_id " +
                    "SET q.status = 'For Receiving' " +
                    "WHERE q.status = 'Pending Approval' AND b.status = 'Registered' " +
                    "AND DATE(q.created_at) = CURDATE()");
            }
            catch { /* non-fatal — just skip reconcile if the query can't run */ }
        }

        /// <summary>
        /// Call the next waiting ticket to a FREE ONLINE window that is authorized for
        /// the ticket's first pending transaction. Walks the queue in priority order
        /// and assigns the first ticket that some eligible window can serve, so each
        /// window only ever receives tickets matching its assigned transactions.
        /// </summary>
        private void btnCallNext_Click(object sender, EventArgs e)
        {
            if (_paused)
            {
                MessageBox.Show("The queue is paused. Resume it first.", "Queue paused",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable cand = Db.Pull(
                "SELECT id, ticket_code FROM queue_tickets " +
                "WHERE status IN ('Waiting','For Receiving') AND DATE(created_at) = CURDATE() " +
                "ORDER BY FIELD(status,'For Receiving','Waiting'), " +
                "FIELD(priority,'Priority','PWD','Senior','Regular'), id");

            if (cand.Rows.Count == 0)
            {
                MessageBox.Show("No one is waiting in the queue.", "Queue",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Find the first waiting ticket an eligible (online, free, authorized) window can take.
            int id = 0, window = 0; string code = null, svc = null;
            foreach (DataRow t in cand.Rows)
            {
                int tid = Convert.ToInt32(t["id"]);
                string s = FirstPendingServiceCode(tid);
                int w = PickWindowFor(s);
                if (w != 0) { id = tid; code = t["ticket_code"].ToString(); window = w; svc = s; break; }
            }

            if (window == 0)
            {
                if (OnlineWindowCount() == 0)
                    MessageBox.Show("No window is online. Sign in to a window (Window Assignment at login) first.",
                        "No online windows", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show("No waiting transactions match an available window.\n" +
                        "Every eligible window is busy, or no online window is assigned to the waiting transaction types.",
                        "Nothing to call", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Section 6 — ACCEPT the ticket to the window without putting it on the public
            // Now Serving board yet. The records assistant locates the documents; the
            // client is only called (→ Serving, on the board, voice callout) once ready,
            // via the Call Client button or the window card. Stamp the accept time.
            Db.Push("UPDATE queue_tickets SET status = 'Accepted', window_no = @w, " +
                    "accepted_window = @w, accepted_at = NOW(), " +
                    "is_priority_ticket = @pri WHERE id = @id",
                    new MySqlParameter("@w", window),
                    new MySqlParameter("@pri", IsPriorityWindow(window) ? 1 : 0),
                    new MySqlParameter("@id", id));
            Audit.Write("Update", "queue_tickets", id, "Accepted " + code + " at " + WindowName(window));

            RefreshAll();
            MessageBox.Show(code + "  accepted at  " + WindowName(window) +
                "\nLocate the client's documents, then press Call Client (or click the window " +
                "card) to call the client to the counter.", "Ticket accepted",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// The first not-yet-completed service on a ticket (in selection order), which
        /// decides which windows may call it. Returns null for a legacy single-service
        /// ticket with no service rows (any online window may take it).
        /// </summary>
        private static string FirstPendingServiceCode(int ticketId)
        {
            DataTable svc = Db.Pull(
                "SELECT service_code FROM queue_ticket_services " +
                "WHERE ticket_id = @id AND status = 'Pending' ORDER BY id LIMIT 1",
                new MySqlParameter("@id", ticketId));
            return svc.Rows.Count > 0 ? svc.Rows[0]["service_code"].ToString() : null;
        }

        /// <summary>
        /// First ONLINE, FREE window (by display order) authorized for <paramref name="code"/>.
        /// A window with no rows in `window_transactions` handles all transactions.
        /// Returns 0 if none is eligible.
        /// </summary>
        private static int PickWindowFor(string code)
        {
            DataTable wins = Db.Pull(
                "SELECT id FROM windows WHERE status = 'Active' AND current_operator IS NOT NULL " +
                "AND last_heartbeat > (NOW() - INTERVAL " + WindowAssignmentForm.StaleMinutes + " MINUTE) " +
                // Window-based access control: a signed-in operator calls tickets only
                // to their own window; a monitor with no window may call to any.
                (Session.HasWindow ? "AND id = " + Session.WindowId + " " : "") +
                "ORDER BY display_order, id");

            foreach (DataRow w in wins.Rows)
            {
                int id = Convert.ToInt32(w["id"]);
                if (!WindowHandles(id, code)) continue;
                // A window is busy while it holds an Accepted (located, not yet called) OR a
                // Serving ticket — Call Next never double-books an occupied window.
                int busy = Db.GetCount(
                    "SELECT id FROM queue_tickets WHERE status IN ('Accepted','Serving') AND window_no = " + id +
                    " AND DATE(created_at) = CURDATE()");
                if (busy == 0) return id;
            }
            return 0;
        }

        /// <summary>True if the window may serve this service code (no assignment = all).</summary>
        private static bool WindowHandles(int windowId, string code)
        {
            // A Priority Window handles every transaction type (spec section 1).
            if (IsPriorityWindow(windowId)) return true;
            int total = Db.GetCount("SELECT id FROM window_transactions WHERE window_id = " + windowId);
            if (total == 0) return true;          // handles All Transactions
            if (code == null) return true;        // legacy ticket, unknown service → don't strand it
            DataTable dt = Db.Pull(
                "SELECT id FROM window_transactions WHERE window_id = @w AND service_code = @c",
                new MySqlParameter("@w", windowId), new MySqlParameter("@c", code));
            return dt.Rows.Count > 0;
        }

        private static int OnlineWindowCount() => Db.GetCount(
            "SELECT id FROM windows WHERE status = 'Active' AND current_operator IS NOT NULL " +
            "AND last_heartbeat > (NOW() - INTERVAL " + WindowAssignmentForm.StaleMinutes + " MINUTE)");

        private static string WindowName(int windowId)
        {
            DataTable dt = Db.Pull("SELECT window_name FROM windows WHERE id = @id",
                new MySqlParameter("@id", windowId));
            return dt.Rows.Count > 0 ? dt.Rows[0]["window_name"].ToString() : "Window " + windowId;
        }

        private string _queueSig = "";

        private void LoadQueue()
        {
            DataTable dt = Db.Pull(
                "SELECT q.id, q.ticket_code AS 'Queue No', q.type_label AS Type, " +
                "DATE_FORMAT(q.created_at, '%H:%i') AS Issued, " +
                "CASE WHEN q.status = 'Completed' THEN '—' " +
                "     ELSE CONCAT(TIMESTAMPDIFF(MINUTE, q.created_at, NOW()), ' min') END AS Wait, " +
                "q.priority AS Priority, q.recall_count AS Recalls, " +
                // Section 8 — total processing time: from accepted/started to completed (or now).
                "CASE WHEN q.accepted_at IS NULL THEN '—' ELSE " +
                "  CONCAT(FLOOR(TIMESTAMPDIFF(SECOND, COALESCE(q.started_at,q.accepted_at), " +
                "    COALESCE(q.completed_at, NOW()))/60), 'm ', " +
                "  MOD(TIMESTAMPDIFF(SECOND, COALESCE(q.started_at,q.accepted_at), " +
                "    COALESCE(q.completed_at, NOW())),60), 's') END AS 'Proc. Time', " +
                "CASE WHEN q.status = 'Serving'  THEN CONCAT('Serving · ',  COALESCE(w.window_name, CONCAT('W', q.window_no))) " +
                "     WHEN q.status = 'Accepted' THEN CONCAT('Accepted · ', COALESCE(w.window_name, CONCAT('W', q.window_no))) " +
                "     ELSE q.status END AS Status " +
                "FROM queue_tickets q LEFT JOIN windows w ON w.id = q.window_no " +
                "WHERE DATE(q.created_at) = CURDATE() ORDER BY q.id DESC");

            // Rebind only when the data actually changed — so a timer-driven refresh
            // doesn't reset the grid's scroll/selection every few seconds when nothing
            // moved, but updates immediately when it does.
            var sig = new StringBuilder();
            foreach (DataRow r in dt.Rows)
                for (int c = 0; c < dt.Columns.Count; c++)
                    sig.Append(r[c]).Append('|');
            if (sig.ToString() == _queueSig && dgvQueue.DataSource is DataTable) return;
            _queueSig = sig.ToString();

            dgvQueue.DataSource = dt;
            // Hidden key column — used to look up the ticket's services on double-click.
            if (dgvQueue.Columns.Contains("id")) dgvQueue.Columns["id"].Visible = false;
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            _paused = !_paused;
            btnPause.Text = _paused ? "Resume Queue" : "Pause Queue";
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (!(dgvQueue.DataSource is DataTable dt) || dt.Rows.Count == 0)
            {
                MessageBox.Show("Nothing to export.", "Export CSV",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv",
                FileName = "queue_" + DateTime.Now.ToString("yyyyMMdd") + ".csv"
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                for (int c = 0; c < dt.Columns.Count; c++)
                    sb.Append(c == 0 ? "" : ",").Append(Csv(dt.Columns[c].ColumnName));
                sb.AppendLine();
                foreach (DataRow row in dt.Rows)
                {
                    for (int c = 0; c < dt.Columns.Count; c++)
                        sb.Append(c == 0 ? "" : ",").Append(Csv(row[c].ToString()));
                    sb.AppendLine();
                }
                File.WriteAllText(sfd.FileName, sb.ToString());
                MessageBox.Show("Exported to " + sfd.FileName, "Export CSV",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string Csv(string value)
        {
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }

    /// <summary>
    /// Public "Now Serving" board for the waiting area — view-only, full screen, and
    /// self-refreshing every 2 seconds. Shows which ticket each window is serving and
    /// has no controls (press Esc or double-click to close). Defined here rather than
    /// in its own file so it needs no .csproj change.
    /// </summary>
    public class ClientDisplayForm : Form
    {
        // windowId → (code label, services label). Rebuilt only when the set of active
        // windows changes; label text is refreshed every tick.
        private readonly System.Collections.Generic.Dictionary<int, Label> _codeLabels =
            new System.Collections.Generic.Dictionary<int, Label>();
        private readonly System.Collections.Generic.Dictionary<int, Label> _subLabels =
            new System.Collections.Generic.Dictionary<int, Label>();
        private string _builtSignature = "";     // ids+names the current grid was built for
        private TableLayoutPanel _grid;
        private readonly Timer _timer;

        public ClientDisplayForm()
        {
            Text = "CROMS — Now Serving";
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = System.Drawing.Color.FromArgb(17, 24, 39);
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
            DoubleClick += (s, e) => Close();

            var header = new Label
            {
                Text = "NOW SERVING",
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 130
            };
            _grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(40),
                BackColor = System.Drawing.Color.Transparent
            };
            Controls.Add(_grid);      // fill first
            Controls.Add(header);     // then the top header

            _timer = new Timer { Interval = 2000 };
            _timer.Tick += (s, e) => Reload();
            _timer.Start();
            Load += (s, e) => Reload();
        }

        /// <summary>Active windows + their current ticket; rebuilds the grid on change.</summary>
        private void Reload()
        {
            try { ReloadCore(); }
            catch { /* DB unreachable — keep the last board, retry next tick */ }
        }

        private void ReloadCore()
        {
            DataTable wins = Db.Pull(
                "SELECT id, window_name, operator_name, " +
                "(current_operator IS NOT NULL AND last_heartbeat > (NOW() - INTERVAL " +
                WindowAssignmentForm.StaleMinutes + " MINUTE)) AS online " +
                "FROM windows WHERE status = 'Active' ORDER BY display_order, id");

            // Signature so we only rebuild the layout when windows are added/removed/renamed.
            var sig = new StringBuilder();
            foreach (DataRow w in wins.Rows) sig.Append(w["id"]).Append(':').Append(w["window_name"]).Append('|');
            if (sig.ToString() != _builtSignature)
            {
                RebuildGrid(wins);
                _builtSignature = sig.ToString();
            }

            foreach (DataRow w in wins.Rows)
            {
                int id = Convert.ToInt32(w["id"]);
                if (!_codeLabels.ContainsKey(id)) continue;

                bool online = w["online"] != DBNull.Value && Convert.ToInt32(w["online"]) == 1;

                // Public board shows only Serving tickets (Accepted ones are still being
                // located and are kept private). Section 5: display the QUEUE NUMBER ONLY —
                // no transaction/service name — for client privacy + a cleaner board.
                DataTable dt = Db.Pull(
                    "SELECT ticket_code FROM queue_tickets " +
                    "WHERE status = 'Serving' AND window_no = @w AND DATE(created_at) = CURDATE() " +
                    "ORDER BY id DESC LIMIT 1", new MySqlParameter("@w", id));

                if (dt.Rows.Count == 0)
                {
                    // No ticket: show the operator presence instead of a bare "idle".
                    _codeLabels[id].Text = "—";
                    _subLabels[id].Text = online ? "● Online — idle" : "○ Offline";
                    _subLabels[id].ForeColor = online
                        ? System.Drawing.Color.FromArgb(74, 222, 128)     // green
                        : System.Drawing.Color.FromArgb(148, 163, 184);   // grey
                }
                else
                {
                    _codeLabels[id].Text = dt.Rows[0]["ticket_code"].ToString();
                    _subLabels[id].Text = "";   // no service name on the public board
                }
            }
        }

        private void RebuildGrid(DataTable wins)
        {
            _grid.SuspendLayout();
            _grid.Controls.Clear();
            _grid.ColumnStyles.Clear();
            _grid.RowStyles.Clear();
            _codeLabels.Clear();
            _subLabels.Clear();

            int count = wins.Rows.Count;
            if (count == 0)
            {
                _grid.ColumnCount = 1; _grid.RowCount = 1;
                _grid.Controls.Add(new Label
                {
                    Text = "No active windows",
                    ForeColor = System.Drawing.Color.FromArgb(148, 163, 184),
                    Font = new System.Drawing.Font("Segoe UI", 24F),
                    Dock = DockStyle.Fill,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                }, 0, 0);
                _grid.ResumeLayout();
                return;
            }

            // Responsive grid: up to 4 cards per row, then wrap to more rows.
            int cols = Math.Min(count, 4);
            int rows = (int)Math.Ceiling(count / (double)cols);
            _grid.ColumnCount = cols;
            _grid.RowCount = rows;
            for (int c = 0; c < cols; c++) _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));
            for (int r = 0; r < rows; r++) _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));

            int i = 0;
            foreach (DataRow w in wins.Rows)
            {
                int id = Convert.ToInt32(w["id"]);
                string name = w["window_name"].ToString();

                var card = new Panel { Dock = DockStyle.Fill, Margin = new Padding(16), BackColor = System.Drawing.Color.FromArgb(31, 41, 55) };
                var code = new Label
                {
                    Text = "—",
                    ForeColor = System.Drawing.Color.FromArgb(96, 165, 250),
                    Font = new System.Drawing.Font("Consolas", 56F, System.Drawing.FontStyle.Bold),
                    Dock = DockStyle.Fill,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                };
                var sub = new Label
                {
                    Text = "idle",
                    ForeColor = System.Drawing.Color.FromArgb(148, 163, 184),
                    Font = new System.Drawing.Font("Segoe UI", 14F),
                    Dock = DockStyle.Bottom, Height = 54,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                };
                var title = new Label
                {
                    Text = name.ToUpper(),
                    ForeColor = System.Drawing.Color.FromArgb(148, 163, 184),
                    Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold),
                    Dock = DockStyle.Top, Height = 52,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                };

                card.Controls.Add(code);
                card.Controls.Add(sub);
                card.Controls.Add(title);

                // Ticket code shrinks to fit the card so a long queue code stays readable.
                AutoFitText.Attach(code, 56F, 14F);

                _codeLabels[id] = code;
                _subLabels[id] = sub;
                _grid.Controls.Add(card, i % cols, i / cols);
                i++;
            }

            _grid.ResumeLayout();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timer.Stop();
            _timer.Dispose();
            base.OnFormClosed(e);
        }
    }

    /// <summary>
    /// Shrinks a (single-line) Label's font so its text always fits the control's
    /// client area — used for the "Now Serving" ticket codes so a long queue number
    /// (e.g. MARRIAGE-100) never clips or gets cut in half. The font never grows past
    /// <paramref name="maxPt"/> (the design size), so normal codes look consistent and
    /// only over-long ones scale down. Re-fits automatically on resize and whenever the
    /// text changes, so it adapts to any card size / screen resolution.
    /// </summary>
    internal static class AutoFitText
    {
        public static void Attach(Label lbl, float maxPt, float minPt)
        {
            if (lbl == null) return;
            EventHandler fit = (s, e) => Fit(lbl, maxPt, minPt);
            lbl.Resize += fit;
            lbl.TextChanged += fit;
            Fit(lbl, maxPt, minPt);
        }

        public static void Fit(Label lbl, float maxPt, float minPt)
        {
            if (lbl == null || lbl.IsDisposed || !lbl.IsHandleCreated) return;
            int w = lbl.ClientSize.Width - lbl.Padding.Horizontal;
            int h = lbl.ClientSize.Height - lbl.Padding.Vertical;
            if (w <= 2 || h <= 2) return;   // not laid out yet — refit fires on Resize

            string text = string.IsNullOrEmpty(lbl.Text) ? " " : lbl.Text;
            FontStyle style = lbl.Font.Style;
            FontFamily family = lbl.Font.FontFamily;

            float best = minPt;
            using (var g = lbl.CreateGraphics())
            {
                for (float size = maxPt; size >= minPt; size -= 1f)
                {
                    using (var f = new Font(family, size, style))
                    {
                        SizeF sz = g.MeasureString(text, f, int.MaxValue,
                            StringFormat.GenericTypographic);
                        if (sz.Width <= w && sz.Height <= h) { best = size; break; }
                    }
                }
            }

            if (Math.Abs(lbl.Font.Size - best) > 0.5f)
            {
                Font old = lbl.Font;
                lbl.Font = new Font(family, best, style);
                old.Dispose();
            }
        }
    }
}
