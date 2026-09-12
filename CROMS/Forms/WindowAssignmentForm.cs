using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Shown right after login (from Program.cs). The operator picks the ONE service window they
    /// will man: the left card lists every Active window with live occupancy (Available / In use
    /// by <em>name</em> / Priority), and the right card configures what that window handles.
    /// <para/>
    /// It deliberately edits ONLY the chosen window. The earlier build was a wizard that stepped
    /// Window 1..N and let any operator rewrite the whole office's routing at login, which both
    /// mismatched what this screen is for and handed an admin capability to every user.
    /// <para/>
    /// Services are NOT exclusive: several windows may handle the same transaction, which is how
    /// a real counter works — two staff can both accept New Registration so the queue drains
    /// faster. Call Next simply routes a ticket to any online window that handles its service.
    /// <para/>
    /// All three exits SAVE this window's `window_transactions`/`is_priority`, because that list
    /// is office configuration rather than session state. They differ only in what happens next:
    /// Start Serving also claims the window (marks it Online) and enters the app; Skip enters the
    /// app without claiming; Log out returns to the sign-in screen. There is no X — leaving is
    /// always one of those three deliberate choices.
    /// </summary>
    public partial class WindowAssignmentForm : Form
    {
        /// <summary>The kiosk service catalogue — code + friendly label.</summary>
        public static readonly (string Code, string Label)[] Catalogue =
        {
            ("BIRTHREG", "Birth Registration"),
            ("CTC", "Certified True Copy (CTC)"),
            ("MARRIAGE_APP", "Marriage Application"),
            ("MARRIAGE_REG", "Marriage Registration"),
            ("DEATH", "Death Certificate"),
            ("PETITION", "Petition (Correction)"),
            ("VERIFY", "Verification / Others"),
            ("CLAIM", "Release & Claim (Pick-up)"),
            ("LEGITIMATION", "Legitimation"),
            ("BREKS", "Breks"),
            ("SUPPLEMENTAL", "Supplemental"),
            ("COURT_ORDER", "Court Order"),
            ("SUPPLEMENTAL_REPORT", "Supplemental Report"),
            ("LEGAL_INSTRUMENTS", "Legal Instruments"),
            ("LEGITIMATION_RA9255", "Legitimation RA-9255"),
        };

        /// <summary>Minutes without a heartbeat before a window is treated as Offline.</summary>
        public const int StaleMinutes = 2;

        private struct Win { public int Id; public string Name; }

        private readonly List<Win> _wins = new List<Win>();
        // windowId -> its assigned service codes. Only the SELECTED window's set is ever edited;
        // the others are loaded just so the picker can show how many each already handles.
        private readonly Dictionary<int, HashSet<string>> _assign = new Dictionary<int, HashSet<string>>();
        // windowId -> is this a Priority window (handles ALL transactions).
        private readonly Dictionary<int, bool> _priority = new Dictionary<int, bool>();
        // windowId -> operator name, for windows currently claimed by ANOTHER operator
        // (Online = fresh heartbeat). Read from the shared DB so a claim on one PC shows here.
        private readonly Dictionary<int, string> _occupant = new Dictionary<int, string>();
        /// <summary>One owner-drawn service row (replaces the old native CheckBox).</summary>
        private sealed class TxnRow
        {
            public Panel Panel;
            public string Code;
            public string Label;
            public bool Checked;
            public bool Disabled;       // Priority is on, so per-service picks don't apply
            public float HoverT;
        }

        private readonly List<TxnRow> _txnRows = new List<TxnRow>();
        private readonly Dictionary<int, Panel> _winRows = new Dictionary<int, Panel>();
        private readonly ToolTip _tips = new ToolTip();

        private int _selectedId;          // 0 = nothing chosen yet
        private bool _loading;            // suppresses change handlers while the panel is rebuilt

        // Navy Blue palette — same tokens as LoginForm / LauncherForm / the kiosk.
        private static readonly Color Accent   = Color.FromArgb(29, 78, 216);
        private static readonly Color AccentBg = Color.FromArgb(234, 241, 254);
        private static readonly Color CardLine = Color.FromArgb(225, 229, 236);
        private static readonly Color Ink      = Color.FromArgb(23, 26, 36);
        private static readonly Color Muted    = Color.FromArgb(91, 100, 114);
        private static readonly Color Faint    = Color.FromArgb(137, 145, 163);
        private static readonly Color Good     = Color.FromArgb(46, 148, 87);
        private static readonly Color Danger   = Color.FromArgb(198, 50, 63);

        public WindowAssignmentForm()
        {
            InitializeComponent();
            ControlBox = false;   // no X — leave via Start Serving / Skip / Log out

            lblSubtitle.Text = (Session.User != null ? Session.User.FullName + " — " : "") +
                               "pick the window you'll be working at, then confirm what it handles.";

            PaintAsCard(pnlWindows);
            PaintAsCard(pnlPriority);
            PaintAsCard(txnPanel);
            CROMS.Modules.UiTheme.PolishButtons(this);

            // The DB can be briefly unreachable right after login (server asleep, Wi-Fi still
            // associating). Previously that threw straight out of the constructor and took the
            // app down; fall into a graceful dead-end instead and let the operator continue.
            string loadError = null;
            try { LoadWindowsAndAssignments(); }
            catch (Exception ex) { loadError = ex.Message; }

            if (loadError != null || _wins.Count == 0)
            {
                lblChooseWindow.Text = loadError != null ? "Cannot reach the database" : "No active windows";
                lblSubtitle.Text = loadError != null
                    ? "Check the server connection, then reopen. You can continue without a window."
                    : "Ask an admin to add one in Settings → Window Management.";
                if (loadError != null) lblMsg.Text = loadError;
                btnStart.Enabled = tglPriority.Enabled = false;
                lblSelectTxn.Visible = lblStatus.Visible = pnlPriority.Visible = txnPanel.Visible = false;
                return;
            }

            BuildWindowList();
            // Land on the first window the operator can actually take.
            Win first = _wins.FirstOrDefault(w => !_occupant.ContainsKey(w.Id));
            SelectWindow(first.Id != 0 ? first.Id : _wins[0].Id);
        }

        // ------------------------------------------------ load current routing
        private void LoadWindowsAndAssignments()
        {
            _wins.Clear(); _assign.Clear(); _priority.Clear(); _occupant.Clear();

            DataTable wins = Db.Pull(
                "SELECT id, window_name, is_priority FROM windows " +
                "WHERE status = 'Active' ORDER BY display_order, id");
            foreach (DataRow w in wins.Rows)
            {
                int id = Convert.ToInt32(w["id"]);
                _wins.Add(new Win { Id = id, Name = w["window_name"].ToString() });
                _assign[id] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _priority[id] = w["is_priority"] != DBNull.Value && Convert.ToInt32(w["is_priority"]) == 1;
            }

            DataTable rows = Db.Pull("SELECT window_id, service_code FROM window_transactions");
            foreach (DataRow r in rows.Rows)
            {
                int id = Convert.ToInt32(r["window_id"]);
                if (!_assign.ContainsKey(id)) continue;
                string code = r["service_code"].ToString();
                if (code == "NEWREG") _assign[id].Add("BIRTHREG");
                else if (code == "MARRIAGE")
                {
                    _assign[id].Add("MARRIAGE_APP");
                    _assign[id].Add("MARRIAGE_REG");
                }
                else _assign[id].Add(code);
            }

            RefreshOccupants();
        }

        /// <summary>
        /// Reloads which windows another operator currently holds. Builds into a temp map and
        /// only swaps it in on success — clearing first would mean a transient DB blip wipes
        /// occupancy and shows every window as free, inviting someone to take a window that is
        /// actually in use.
        /// </summary>
        private void RefreshOccupants()
        {
            int me = Session.User?.Id ?? -1;
            DataTable occ = Db.Pull(
                "SELECT id, operator_name FROM windows " +
                "WHERE current_operator IS NOT NULL AND current_operator <> @me " +
                "AND last_heartbeat > (NOW() - INTERVAL " + StaleMinutes + " MINUTE)",
                new MySqlParameter("@me", me));

            var fresh = new Dictionary<int, string>();
            foreach (DataRow o in occ.Rows)
            {
                int id = Convert.ToInt32(o["id"]);
                fresh[id] = o["operator_name"] == DBNull.Value ? "another operator"
                                                               : o["operator_name"].ToString();
            }
            _occupant.Clear();
            foreach (var kv in fresh) _occupant[kv.Key] = kv.Value;
        }

        // ------------------------------------------------------ window picker
        /// <summary>Builds one selectable row per Active window, showing live occupancy.</summary>
        private void BuildWindowList()
        {
            pnlWindows.Controls.Clear();
            _winRows.Clear();

            int y = 12;
            foreach (Win w in _wins)
            {
                int id = w.Id;
                string name = w.Name;
                var row = new Panel
                {
                    Location = new Point(12, y),
                    Size = new Size(pnlWindows.Width - 40, 62),
                    BackColor = Color.White,
                    Tag = id,
                    Cursor = _occupant.ContainsKey(id) ? Cursors.No : Cursors.Hand
                };
                typeof(Control).GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(row, true);

                row.Paint += (s, e) => PaintWindowRow(e.Graphics, row, id, name);
                row.Click += (s, e) => SelectWindow(id);

                pnlWindows.Controls.Add(row);
                _winRows[id] = row;
                y += 70;
            }
        }

        /// <summary>
        /// One picker row: window name, live status, and a tick when chosen. Occupied windows
        /// are greyed and name the operator holding them, so it's obvious at a glance which
        /// windows are actually free — the whole point of this screen.
        /// </summary>
        private void PaintWindowRow(Graphics g, Panel row, int id, string name)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            bool taken = _occupant.ContainsKey(id);
            bool chosen = id == _selectedId && !taken;

            Color fill = taken ? Color.FromArgb(247, 248, 250) : (chosen ? AccentBg : Color.White);
            Color border = chosen ? Accent : CardLine;
            float bw = chosen ? 2f : 1f;

            var r = new Rectangle(0, 0, row.Width - 1, row.Height - 1);
            using (var path = RoundedRect(r, 8))
            {
                using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                using (var pen = new Pen(border, bw)) g.DrawPath(pen, path);
            }

            using (var fName = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (var bName = new SolidBrush(taken ? Faint : (chosen ? Accent : Ink)))
                g.DrawString(name, fName, bName, new PointF(14, 9));

            string status;
            Color statusColor;
            if (taken) { status = "In use by " + _occupant[id]; statusColor = Danger; }
            else if (_priority.TryGetValue(id, out bool p) && p)
            { status = "Priority — handles all transactions"; statusColor = Accent; }
            else
            {
                int n = _assign.ContainsKey(id) ? _assign[id].Count : 0;
                status = n == 0 ? "Available — nothing assigned yet"
                                : "Available — " + n + " transaction" + (n == 1 ? "" : "s");
                statusColor = n == 0 ? Muted : Good;
            }
            using (var fSt = new Font("Segoe UI", 8.75F))
            using (var bSt = new SolidBrush(statusColor))
                g.DrawString(status, fSt, bSt, new PointF(16, 34));

            if (chosen)
            {
                var badge = new Rectangle(row.Width - 34, 20, 22, 22);
                using (var b = new SolidBrush(Accent)) g.FillEllipse(b, badge);
                using (var wp = new Pen(Color.White, 2.2f)
                {
                    StartCap = System.Drawing.Drawing2D.LineCap.Round,
                    EndCap = System.Drawing.Drawing2D.LineCap.Round,
                    LineJoin = System.Drawing.Drawing2D.LineJoin.Round
                })
                    g.DrawLines(wp, new[]
                    {
                        new PointF(badge.X + 6, badge.Y + 11),
                        new PointF(badge.X + 9.5f, badge.Y + 15),
                        new PointF(badge.X + 16, badge.Y + 7)
                    });
            }
        }

        /// <summary>Repaints every picker row (selection / occupancy state changed).</summary>
        private void RefreshWindowRows()
        {
            foreach (var kv in _winRows) kv.Value.Invalidate();
        }

        private void SelectWindow(int id)
        {
            if (_occupant.ContainsKey(id))
            {
                lblMsg.Text = "That window is in use by " + _occupant[id] + ". Pick another one.";
                return;
            }
            if (_selectedId != 0) SaveCurrent();     // keep edits made to the previous choice
            _selectedId = id;
            lblMsg.Text = "";
            RefreshWindowRows();
            RenderSelected();
        }

        // --------------------------------------------- render the chosen window
        private void RenderSelected()
        {
            _loading = true;
            RefreshPresence();

            Win cur = _wins.First(w => w.Id == _selectedId);
            lblSelectTxn.Text = "What " + cur.Name + " Handles";

            txnPanel.AutoScroll = true;
            txnPanel.AutoScrollPosition = Point.Empty;
            txnPanel.Controls.Clear();
            _txnRows.Clear();

            int y = 12;
            foreach (var svc in Catalogue)
            {
                var rowState = new TxnRow
                {
                    Code = svc.Code,
                    Label = svc.Label,
                    Checked = _assign[cur.Id].Contains(svc.Code)
                };

                var row = new Panel
                {
                    Location = new Point(14, y),
                    Size = new Size(txnPanel.Width - 28, 40),
                    BackColor = Color.White,
                    Cursor = Cursors.Hand
                };
                typeof(Control).GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(row, true);
                rowState.Panel = row;

                row.Paint += (s2, e2) => PaintTxnRow(e2.Graphics, rowState);
                row.Click += (s2, e2) => ToggleTxn(rowState);
                CROMS.Modules.HoverFade.Attach(row, 140, t => { rowState.HoverT = t; row.Invalidate(); });
                _tips.SetToolTip(row, svc.Label);

                _txnRows.Add(rowState);
                txnPanel.Controls.Add(row);
                y += 44;
            }

            tglPriority.SetCheckedSilently(_priority[cur.Id]);
            tglPriority.Enabled = true;
            ApplyPriorityToBoxes();

            btnStart.Enabled = true;
            _loading = false;

            UpdateStatus();
        }

        /// <summary>
        /// One service row: a rounded selectable card with its own check indicator, matching the
        /// window picker and the kiosk service cards. Replaced native CheckBoxes, which read as a
        /// plain form list rather than something you pick from.
        /// </summary>
        private void PaintTxnRow(Graphics g, TxnRow r)
        {
            Panel p = r.Panel;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            float hov = !r.Disabled && !r.Checked ? r.HoverT : 0f;

            Color fill = r.Disabled ? Color.FromArgb(248, 249, 251)
                       : r.Checked ? AccentBg
                       : CROMS.Modules.HoverFade.Lerp(Color.White, Color.FromArgb(250, 251, 254), hov);
            Color border = r.Checked ? Accent
                         : CROMS.Modules.HoverFade.Lerp(CardLine, Accent, hov);
            float bw = r.Checked ? 1.8f : 1f;
            Color ink = r.Disabled ? Faint : (r.Checked ? Accent : Ink);

            var rect = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
            using (var path = RoundedRect(rect, 8))
            {
                using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                using (var pen = new Pen(border, bw)) g.DrawPath(pen, path);
            }

            // Check indicator — filled accent square with a tick when selected.
            var box = new Rectangle(13, (p.Height - 20) / 2, 20, 20);
            using (var bp = RoundedRect(box, 5))
            {
                if (r.Checked)
                {
                    using (var b = new SolidBrush(Accent)) g.FillPath(b, bp);
                    using (var wp = new Pen(Color.White, 2.1f)
                    {
                        StartCap = System.Drawing.Drawing2D.LineCap.Round,
                        EndCap = System.Drawing.Drawing2D.LineCap.Round,
                        LineJoin = System.Drawing.Drawing2D.LineJoin.Round
                    })
                        g.DrawLines(wp, new[]
                        {
                            new PointF(box.X + 5f,  box.Y + 10f),
                            new PointF(box.X + 8.5f, box.Y + 13.5f),
                            new PointF(box.X + 15f, box.Y + 6.5f)
                        });
                }
                else
                {
                    using (var b = new SolidBrush(r.Disabled ? Color.FromArgb(243, 245, 248) : Color.White))
                        g.FillPath(b, bp);
                    using (var pen = new Pen(r.Disabled ? CardLine
                                             : CROMS.Modules.HoverFade.Lerp(Color.FromArgb(198, 204, 214), Accent, hov), 1.4f))
                        g.DrawPath(pen, bp);
                }
            }

            using (var f = new Font("Segoe UI", 10F, r.Checked ? FontStyle.Bold : FontStyle.Regular))
            using (var b = new SolidBrush(ink))
            using (var fmt = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                g.DrawString(r.Label, f, b, new RectangleF(46, 0, p.Width - 60, p.Height), fmt);

        }

        private void ToggleTxn(TxnRow r)
        {
            if (r.Disabled || _loading) return;
            r.Checked = !r.Checked;
            r.Panel.Invalidate();
            SaveCurrent();
            UpdateStatus();
            RefreshWindowRows();
        }

        /// <summary>Greys out the per-service rows when this window is Priority (handles all).</summary>
        private void ApplyPriorityToBoxes()
        {
            bool pri = tglPriority.Checked;
            foreach (TxnRow r in _txnRows)
            {
                if (pri) { r.Checked = false; r.Disabled = true; }
                else r.Disabled = false;
                r.Panel.Cursor = r.Disabled ? Cursors.No : Cursors.Hand;
                r.Panel.Invalidate();
            }
            lblWarn.Visible = pri;
            lblWarn.ForeColor = Color.FromArgb(180, 83, 9);
            lblWarn.Text = pri
                ? "★ This window handles ALL transactions. Other windows' specific assignments still apply."
                : "";
        }

        // ----------------------------------------------------- assignment model
        /// <summary>Re-reads which windows are claimed by another operator, so a claim made on
        /// another PC after this dialog opened shows up here. Non-fatal if the DB blips.</summary>
        private void RefreshPresence()
        {
            try { RefreshOccupants(); }
            catch { /* non-fatal — fall back to the open-time snapshot */ }
        }

        private string WindowName(int id)
        {
            int i = _wins.FindIndex(w => w.Id == id);
            return i >= 0 ? _wins[i].Name : ("Window " + id);
        }

        /// <summary>Reads the on-screen selections into the in-memory map for the chosen window.</summary>
        private void SaveCurrent()
        {
            if (_selectedId == 0) return;
            _priority[_selectedId] = tglPriority.Checked;
            var set = _assign[_selectedId];
            set.Clear();
            if (!tglPriority.Checked)
                foreach (TxnRow r in _txnRows)
                    if (!r.Disabled && r.Checked) set.Add(r.Code);
        }

        private void UpdateStatus()
        {
            if (_selectedId == 0) { lblStatus.Text = ""; return; }
            if (tglPriority.Checked) { lblStatus.Text = "Handles ALL transactions"; return; }
            int assigned = _txnRows.Count(r => !r.Disabled && r.Checked);
            int available = _txnRows.Count(r => !r.Disabled);
            lblStatus.Text = assigned + " selected · " + (available - assigned) + " available";
        }

        // ------------------------------------------------------------ painting
        /// <summary>
        /// Gives a plain Panel the rounded white card look used across the app. Painted rather
        /// than a BorderStyle so the corners match the Login / Launcher / kiosk cards.
        /// </summary>
        private void PaintAsCard(Control panel)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(panel, true);
            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                var r = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                using (var path = RoundedRect(r, 10))
                {
                    using (var b = new SolidBrush(Color.White)) g.FillPath(b, path);
                    using (var pen = new Pen(CardLine, 1f)) g.DrawPath(pen, path);
                }
            };
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new System.Drawing.Drawing2D.GraphicsPath();
            if (d <= 0 || d > r.Width || d > r.Height) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        // ----------------------------------------------------------- handlers
        private void chkAll_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            ApplyPriorityToBoxes();
            SaveCurrent();
            UpdateStatus();
            RefreshWindowRows();
        }

        private void btnSkip_Click(object sender, EventArgs e)
        {
            // Enter the app without manning a window. The transaction list is office
            // configuration, so it is still saved — same rule as Log out. Only the CLAIM
            // (Online presence) is skipped.
            SaveCurrent();
            SaveRouting(_selectedId, out string err);
            if (err != null) { lblMsg.Text = "Could not save the assignment: " + err; return; }

            DialogResult = DialogResult.OK;   // enter the app with no window claimed
            Close();
        }

        /// <summary>
        /// Signs out without entering the app and returns to the login screen. This replaced the
        /// window's X, which used to drop the operator INTO the app with no window claimed and no
        /// warning — the opposite of what someone closing this screen usually means.
        /// Program.Main loops back to LoginForm on DialogResult.Abort.
        /// </summary>
        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Log out and return to the sign-in screen?", "Log out",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            // Confirmed — keep the window's transaction list. It is office configuration, not
            // session state, so it should survive the operator signing out. Only the CLAIM
            // (Online presence) is skipped, since they are not manning the window.
            SaveCurrent();
            SaveRouting(_selectedId, out string err);   // best effort; never block a logout
            if (err != null) { lblMsg.Text = "Could not save the assignment: " + err; return; }

            try { Audit.Write(Audit.Logout, "users", Session.User?.Id, "Signed out at window selection"); }
            catch { /* never block a logout on the audit write */ }

            DialogResult = DialogResult.Abort;   // Abort => Program returns to the login screen
            Close();
        }

        /// <summary>
        /// Writes ONE window's transaction list + priority flag. Shared by Start Serving (which
        /// then also claims the window) and Log out (which does not).
        /// </summary>
        private void SaveRouting(int windowId, out string error)
        {
            error = null;
            if (windowId == 0) return;
            try
            {
                Db.Push("UPDATE windows SET is_priority = @p WHERE id = @id",
                    new MySqlParameter("@p", _priority[windowId] ? 1 : 0),
                    new MySqlParameter("@id", windowId));
                Db.Push("DELETE FROM window_transactions WHERE window_id = @id",
                    new MySqlParameter("@id", windowId));
                if (!_priority[windowId])
                    foreach (string code in _assign[windowId])
                        Db.Push("INSERT INTO window_transactions (window_id, service_code) VALUES (@w, @c)",
                            new MySqlParameter("@w", windowId), new MySqlParameter("@c", code));
            }
            catch (Exception ex) { error = ex.Message; }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (_selectedId == 0) { lblMsg.Text = "Pick a window first."; return; }
            SaveCurrent();
            Win cur = _wins.First(w => w.Id == _selectedId);

            if (!tglPriority.Checked && _assign[cur.Id].Count == 0)
            {
                lblMsg.Text = "Choose at least one transaction for this window, or switch on Priority Window.";
                return;
            }

            int myId = Session.User?.Id ?? -1;
            if (IsOccupiedByOther(cur.Id, myId))
            {
                lblMsg.Text = "This window was just taken by another operator. Pick another one.";
                RefreshPresence();
                BuildWindowList();
                return;
            }

            // Only THIS window's routing is written — the operator can't touch anyone else's.
            SaveRouting(cur.Id, out string err);
            if (err != null) { lblMsg.Text = "Could not save the assignment: " + err; return; }

            try
            {
                Db.Push(
                    "UPDATE windows SET current_operator = @op, operator_name = @nm, last_heartbeat = NOW() " +
                    "WHERE id = @id",
                    new MySqlParameter("@op", Session.UserIdParam),
                    new MySqlParameter("@nm", (object)(Session.User?.FullName) ?? DBNull.Value),
                    new MySqlParameter("@id", cur.Id));
            }
            catch (Exception ex)
            {
                lblMsg.Text = "Could not claim the window: " + ex.Message;
                return;
            }

            Session.WindowId = cur.Id;
            Session.WindowName = cur.Name;
            string codes = tglPriority.Checked ? "Priority — all transactions"
                                               : string.Join(", ", _assign[cur.Id]);
            Audit.Write(Audit.Update, "windows", cur.Id, "Signed in to " + cur.Name + " (" + codes + ")");

            DialogResult = DialogResult.OK;
            Close();
        }

        // ---- shared presence helpers (used by MainForm heartbeat / logout) ----

        private static bool IsOccupiedByOther(int windowId, int myUserId)
        {
            DataTable dt = Db.Pull(
                "SELECT current_operator FROM windows WHERE id = @id AND current_operator IS NOT NULL " +
                "AND current_operator <> @me AND last_heartbeat > (NOW() - INTERVAL " + StaleMinutes + " MINUTE)",
                new MySqlParameter("@id", windowId), new MySqlParameter("@me", myUserId));
            return dt.Rows.Count > 0;
        }

        /// <summary>Keeps the window's Online presence alive (called on a timer).</summary>
        public static void Heartbeat(int windowId)
        {
            if (windowId <= 0) return;
            try
            {
                Db.Push("UPDATE windows SET last_heartbeat = NOW() WHERE id = @id",
                    new MySqlParameter("@id", windowId));
            }
            catch { /* non-fatal */ }
        }

        /// <summary>Frees a window (sets it Offline) — on logout / app exit.</summary>
        public static void Release(int windowId)
        {
            if (windowId <= 0) return;
            try
            {
                Db.Push("UPDATE windows SET current_operator = NULL, operator_name = NULL, " +
                        "last_heartbeat = NULL WHERE id = @id",
                    new MySqlParameter("@id", windowId));
            }
            catch { /* non-fatal */ }
        }
    }
}
