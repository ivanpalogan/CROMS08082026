using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Windows.Forms;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Modules
{
    /// <summary>
    /// The currently opened queue task. This is deliberately separate from the active
    /// ticket at the service window: normal navigation clears this context, so saving a
    /// manually opened transaction can never accidentally finish a queue task.
    /// </summary>
    public static class QueueTaskContext
    {
        public static int TicketId { get; private set; }
        public static int TaskId { get; private set; }
        public static string TicketCode { get; private set; }
        public static string ServiceCode { get; private set; }

        public static bool IsLinked => TicketId > 0 && TaskId > 0;

        public static void Link(int ticketId, int taskId, string ticketCode, string serviceCode)
        {
            TicketId = ticketId;
            TaskId = taskId;
            TicketCode = ticketCode;
            ServiceCode = serviceCode;
        }

        public static void Clear()
        {
            TicketId = 0;
            TaskId = 0;
            TicketCode = null;
            ServiceCode = null;
        }
    }

    /// <summary>
    /// Shared task drawer for the ticket assigned to this operator's service window. It overlays
    /// the CONTENT area rather than docking into it, so opening the drawer never resizes the
    /// active module. Its tab is attached to the drawer's left edge; collapsing immediately
    /// places the body beyond the right edge while leaving the arrow visible.
    ///
    /// Stored service states remain Pending / Serving / Completed for compatibility; the
    /// operator-facing labels are Pending / Current Task / Finished.
    /// </summary>
    public sealed class ClientTasksPanel : Panel
    {
        private const int TabWidth = 30;
        private const int BodyWidth = 340;

        private readonly CROMS.MainForm _shell;
        private readonly Control _overlayArea;

        private readonly Panel _body = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
        private readonly Panel _tab = new Panel { Dock = DockStyle.Left, Width = TabWidth };

        private readonly Label _queue = new Label();
        private readonly Label _client = new Label();
        private readonly Label _meta = new Label();
        private readonly Label _summary = new Label();
        private readonly PictureBox _picClient = new PictureBox();
        private readonly PictureBox _picId = new PictureBox();
        private readonly Label _lblClientState = new Label();
        private readonly Label _lblIdState = new Label();
        private int _photosForTicket = -1;
        private readonly Panel _currentHost = new Panel { Dock = DockStyle.Top, AutoSize = false, Height = 0 };
        private readonly FlowLayoutPanel _tasks = new FlowLayoutPanel();
        private readonly Panel _footer = new Panel { Dock = DockStyle.Bottom, Height = 132, BackColor = UiTheme.Surface, Padding = new Padding(10, 8, 10, 10) };
        private readonly Button _complete = new Button();
        private readonly Button _abandonTask = new Button();
        private readonly Button _abandonAll = new Button();
        private readonly Timer _refresh = new Timer { Interval = 3000 };

        private int _ticketId;
        private string _ticketCode;
        private string _renderSignature;
        private bool _expanded = true;
        private bool _moduleAllowed = true;

        // What the client asked for, per service code — read from the kiosk's own intake rows
        // (ctc_requests / breqs_requests) so the window sees the request the client actually
        // described instead of just a service name. Rebuilt on every reload that changes.
        private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> _details =
            new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>(StringComparer.OrdinalIgnoreCase);
        private int _openTasks;

        public ClientTasksPanel(CROMS.MainForm shell, Control overlayArea)
        {
            _shell = shell;
            _overlayArea = overlayArea;
            Dock = DockStyle.None;
            BackColor = UiTheme.PageBg;
            Visible = false;
            Width = TabWidth + BodyWidth;

            BuildBody();
            BuildTab();

            EnableDoubleBuffering(this);
            EnableDoubleBuffering(_body);
            EnableDoubleBuffering(_tasks);

            // The tab is the drawer handle, so it belongs on the LEFT of the body. When the
            // drawer closes, the whole control moves right until only this handle is in view.
            Controls.Add(_body);
            Controls.Add(_tab);

            _refresh.Tick += (s, e) => { if (Visible) Reload(); };
            _refresh.Start();

            ApplyExpanded();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            if (Parent != null)
            {
                Parent.ClientSizeChanged -= Parent_ClientSizeChanged;
                Parent.ClientSizeChanged += Parent_ClientSizeChanged;
            }
            base.OnParentChanged(e);
            PositionDrawer();
        }

        private void Parent_ClientSizeChanged(object sender, EventArgs e)
        {
            PositionDrawer();
        }

        // ================================================================
        //  Layout
        // ================================================================

        private void BuildBody()
        {
            // Who is at the window, above what they came for. The queue number alone identifies
            // the ticket but not the person — an operator holding a birth certificate has to be
            // able to check the name in front of them without opening another module.
            var head = new Panel { Dock = DockStyle.Top, Height = 208, BackColor = UiTheme.Surface, Padding = new Padding(16, 12, 16, 8) };
            _queue.AutoSize = false;
            _queue.Dock = DockStyle.Top;
            _queue.Height = 28;
            _queue.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            _queue.ForeColor = UiTheme.Ink;

            _client.AutoSize = false;
            _client.Dock = DockStyle.Top;
            _client.Height = 22;
            _client.AutoEllipsis = true;
            _client.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _client.ForeColor = UiTheme.Ink;

            _meta.AutoSize = false;
            _meta.Dock = DockStyle.Top;
            _meta.Height = 34;
            _meta.Font = new Font("Segoe UI", 8.5F);
            _meta.ForeColor = UiTheme.Muted;

            _summary.AutoSize = false;
            _summary.Dock = DockStyle.Top;
            _summary.Height = 20;
            _summary.Font = new Font("Segoe UI", 9F);
            _summary.ForeColor = UiTheme.Muted;

            // Client photo + uploaded ID, side by side — the officer holding this task has to
            // be able to compare a face and a government ID without opening another module.
            var photos = new Panel { Dock = DockStyle.Top, Height = 92, Padding = new Padding(0, 6, 0, 0) };
            Panel clientBlock = BuildPhotoBlock("Client Photo", _picClient, _lblClientState, DockStyle.Left);
            Panel idBlock = BuildPhotoBlock("Uploaded ID", _picId, _lblIdState, DockStyle.Right);
            photos.Controls.Add(idBlock);
            photos.Controls.Add(clientBlock);

            // Dock=Top stacks in reverse add order, so add bottom-most first.
            head.Controls.Add(photos);
            head.Controls.Add(_summary);
            head.Controls.Add(_meta);
            head.Controls.Add(_client);
            head.Controls.Add(_queue);

            _tasks.Dock = DockStyle.Fill;
            _tasks.FlowDirection = FlowDirection.TopDown;
            _tasks.WrapContents = false;
            _tasks.AutoScroll = true;
            _tasks.BackColor = UiTheme.PageBg;
            _tasks.Padding = new Padding(10, 8, 10, 8);

            _complete.Text = "Complete Client Visit";
            _complete.Dock = DockStyle.Bottom;
            _complete.Height = 46;
            _complete.FlatStyle = FlatStyle.Flat;
            _complete.FlatAppearance.BorderSize = 0;
            _complete.BackColor = UiTheme.Success;
            _complete.ForeColor = Color.White;
            _complete.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _complete.Click += (s, e) => CompleteVisit();

            // Abandon is the answer to a client who walked away, or who asked for something the
            // office cannot do today. Without it the task stays Pending for ever and
            // CompleteVisit can never close the visit, so the window stays occupied by someone
            // who is no longer there. Deliberately NOT a delete: the request was made, it is
            // part of the day's queue statistics, and the reason has to stay on the record.
            _abandonTask.Text = "Abandon This Task";
            _abandonTask.Dock = DockStyle.Top;
            _abandonTask.Height = 34;
            _abandonTask.FlatStyle = FlatStyle.Flat;
            _abandonTask.FlatAppearance.BorderSize = 0;
            _abandonTask.BackColor = UiTheme.WarningTint;
            _abandonTask.ForeColor = UiTheme.Warning;
            _abandonTask.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _abandonTask.Click += (s, e) => AbandonCurrentTask();

            _abandonAll.Text = "Abandon All Tasks · End Visit";
            _abandonAll.Dock = DockStyle.Top;
            _abandonAll.Height = 34;
            _abandonAll.Margin = new Padding(0, 6, 0, 0);
            _abandonAll.FlatStyle = FlatStyle.Flat;
            _abandonAll.FlatAppearance.BorderSize = 0;
            _abandonAll.BackColor = UiTheme.DangerTint;
            _abandonAll.ForeColor = UiTheme.Danger;
            _abandonAll.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _abandonAll.Click += (s, e) => AbandonAllTasks();

            var spacerA = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = UiTheme.Surface };
            var spacerB = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = UiTheme.Surface };
            _footer.Controls.Add(_abandonAll);
            _footer.Controls.Add(spacerB);
            _footer.Controls.Add(_abandonTask);
            _footer.Controls.Add(spacerA);
            _footer.Controls.Add(_complete);

            // Current Task is a fixed, non-scrolling section pinned at the top of the body —
            // it must stay visible while the operator scrolls the remaining task list below it.
            _currentHost.BackColor = UiTheme.PageBg;
            _currentHost.Padding = new Padding(10, 10, 10, 0);

            _footer.Height = 150;   // 46 complete + 34 + 34 abandon + gaps, inside the padding

            _body.Controls.Add(_tasks);
            _body.Controls.Add(_currentHost);
            _body.Controls.Add(_footer);
            _body.Controls.Add(head);
        }

        /// <summary>One labeled photo tile (caption on top, image box, state line under it).</summary>
        private static Panel BuildPhotoBlock(string caption, PictureBox pic, Label state, DockStyle side)
        {
            var block = new Panel { Dock = side, Width = 150 };
            var cap = new Label
            {
                Text = caption, Dock = DockStyle.Top, Height = 16,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = UiTheme.Muted
            };
            pic.Dock = DockStyle.Top;
            pic.Height = 62;
            pic.SizeMode = PictureBoxSizeMode.Zoom;
            pic.BackColor = UiTheme.PageBg;
            pic.BorderStyle = BorderStyle.FixedSingle;

            state.Dock = DockStyle.Top;
            state.Height = 14;
            state.AutoEllipsis = true;
            state.Font = new Font("Segoe UI", 7.5F);
            state.ForeColor = UiTheme.Muted;
            state.Text = "—";

            block.Controls.Add(state);
            block.Controls.Add(pic);
            block.Controls.Add(cap);
            return block;
        }

        /// <summary>
        /// Client face photo (queue_tickets.id_image, already on the ticket row) and the ID
        /// uploaded via the claimapp QR (claim_requests, linked by queue_ticket_id — every kiosk
        /// visit now creates that row, not only Release &amp; Claim pickups). Requeried only when
        /// the ticket changes, not on every 3-second refresh.
        /// </summary>
        private void UpdatePhotos(DataRow t, int ticketId)
        {
            if (_photosForTicket == ticketId) return;
            _photosForTicket = ticketId;

            _picClient.Image?.Dispose();
            _picClient.Image = null;
            if (t.Table.Columns.Contains("id_image") && t["id_image"] != DBNull.Value)
            {
                try { using (var ms = new MemoryStream((byte[])t["id_image"])) _picClient.Image = Image.FromStream(ms); }
                catch { /* stored value wasn't a readable image */ }
            }
            _lblClientState.Text = _picClient.Image != null ? "On file" : "No kiosk photo";

            _picId.Image?.Dispose();
            _picId.Image = null;
            DataTable dt = Db.Pull(
                "SELECT id_image FROM claim_requests WHERE queue_ticket_id = @id ORDER BY id DESC LIMIT 1",
                new MySqlParameter("@id", ticketId));
            if (dt.Rows.Count > 0 && dt.Rows[0]["id_image"] != DBNull.Value)
            {
                try { using (var ms = new MemoryStream((byte[])dt.Rows[0]["id_image"])) _picId.Image = Image.FromStream(ms); }
                catch { /* stored value wasn't a readable image */ }
            }
            _lblIdState.Text = _picId.Image != null ? "On file" : "Not yet uploaded";
        }

        private void ClearPhotos()
        {
            _photosForTicket = -1;
            _picClient.Image?.Dispose();
            _picClient.Image = null;
            _picId.Image?.Dispose();
            _picId.Image = null;
            _lblClientState.Text = "—";
            _lblIdState.Text = "—";
        }

        private void BuildTab()
        {
            _tab.BackColor = UiTheme.Accent;
            _tab.Cursor = Cursors.Hand;
            // Panel.DoubleBuffered / SetStyle are protected on Control, so a plain Panel
            // instance can only be double-buffered via reflection (same trick used elsewhere
            // in this codebase for owner-drawn Buttons/Panels) — otherwise every hover repaint
            // flickers.
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(_tab, true, null);
            _tab.Paint += Tab_Paint;
            _tab.Click += (s, e) => ToggleExpanded();
            _tab.MouseEnter += (s, e) => { _tab.BackColor = UiTheme.AccentHover; };
            _tab.MouseLeave += (s, e) => { _tab.BackColor = UiTheme.Accent; };
        }

        private void Tab_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Arrow: points toward the edge that opens the panel further — right (collapse)
            // while expanded, left (expand) while collapsed.
            int cx = _tab.Width / 2;
            int ay = 14;
            using (var pen = new Pen(Color.White, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                if (_expanded)
                {
                    g.DrawLine(pen, cx - 4, ay - 4, cx + 3, ay);
                    g.DrawLine(pen, cx - 4, ay + 4, cx + 3, ay);
                }
                else
                {
                    g.DrawLine(pen, cx + 4, ay - 4, cx - 3, ay);
                    g.DrawLine(pen, cx + 4, ay + 4, cx - 3, ay);
                }
            }

            // Keep the collapsed handle minimal: only the arrow remains visible. The label
            // returns when the drawer is open and has room to read as an attached tab.
            if (_expanded)
            {
                using (var font = new Font("Segoe UI", 9F, FontStyle.Bold))
                using (var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                {
                    GraphicsState state = g.Save();
                    g.TranslateTransform(_tab.Width / 2f, _tab.Height / 2f);
                    g.RotateTransform(-90);
                    g.DrawString("CLIENT TASKS", font, Brushes.White,
                        new RectangleF(-_tab.Height / 2f, -_tab.Width / 2f,
                            _tab.Height, _tab.Width), format);
                    g.Restore(state);
                }
            }
        }

        private void ToggleExpanded()
        {
            _expanded = !_expanded;
            ApplyExpanded();
        }

        private void ApplyExpanded()
        {
            PositionDrawer();
            _tab.Invalidate();
        }

        private void PositionDrawer()
        {
            if (Parent == null) return;

            // The drawer is a sibling of the scrolling module host. Match that host's visible
            // rectangle so the drawer overlays it without becoming part of its scrollable area.
            Point origin = Parent.PointToClient(_overlayArea.PointToScreen(Point.Empty));
            Height = _overlayArea.ClientSize.Height;
            Top = origin.Y;
            Left = origin.X + _overlayArea.ClientSize.Width - (_expanded ? Width : TabWidth);
        }

        private static void EnableDoubleBuffering(Control control)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(control, true, null);
        }

        /// <summary>Expands the panel (e.g. right after a client is called to the window).</summary>
        public void Expand()
        {
            _expanded = true;
            ApplyExpanded();
            Reload();
        }

        /// <summary>Hides the drawer body off the right side, leaving its arrow handle visible.</summary>
        public void Collapse()
        {
            _expanded = false;
            ApplyExpanded();
        }

        // Back-compat names used elsewhere in the shell.
        public void ShowPanel() => Expand();
        public void HidePanel() => Collapse();

        /// <summary>Whether the currently shown module is one Client Tasks should appear on
        /// at all (never the Dashboard, never Settings) — independent of expand/collapse.</summary>
        public void SetModuleAllowed(bool allowed)
        {
            _moduleAllowed = allowed;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            Visible = _moduleAllowed && _ticketId > 0;
            if (Visible)
            {
                PositionDrawer();
                BringToFront();
            }
        }

        // ================================================================
        //  Data
        // ================================================================

        public void Reload()
        {
            if (!Session.HasWindow)
            {
                _ticketId = 0;
                _renderSignature = null;
                UpdateVisibility();
                return;
            }

            DataTable ticket = Db.Pull(
                "SELECT id, ticket_code, full_name, spouse_full_name, contact_no, priority, " +
                "document_type, purpose, type_label, valid_id_type, id_image, " +
                "TIME_FORMAT(TIME(created_at), '%h:%i %p') AS issued " +
                "FROM queue_tickets " +
                "WHERE window_no = @w AND status IN ('Accepted','Serving') " +
                "AND DATE(created_at) = CURDATE() ORDER BY id DESC LIMIT 1",
                new MySqlParameter("@w", Session.WindowId));

            if (ticket.Rows.Count == 0)
            {
                _ticketId = 0;
                _ticketCode = null;
                _renderSignature = null;
                QueueTaskContext.Clear();
                ClearPhotos();
                UpdateVisibility();
                return;
            }

            DataRow t = ticket.Rows[0];
            _ticketId = Convert.ToInt32(t["id"]);
            _ticketCode = t["ticket_code"].ToString();
            _queue.Text = _ticketCode;
            _client.Text = Cell(t, "full_name", "(name not given)");
            _meta.Text = ClientMeta(t);
            _client.ForeColor = UiTheme.Ink;
            UpdatePhotos(t, _ticketId);
            UpdateVisibility();

            DataTable rows = LoadServices();
            LoadRequestDetails(t);

            var signatureBuilder = new StringBuilder().Append(_ticketId).Append(':');
            foreach (DataRow row in rows.Rows)
                signatureBuilder.Append(row["id"]).Append('~')
                    .Append(row["service_code"]).Append('~')
                    .Append(row["service_label"]).Append('~')
                    .Append(row["status"]).Append('|');
            foreach (var pair in _details)
                signatureBuilder.Append(pair.Key).Append('=').Append(string.Join(";", pair.Value)).Append('|');
            string signature = signatureBuilder.ToString();
            if (signature == _renderSignature) return;
            _renderSignature = signature;

            DataRow current = null;
            var pending = new System.Collections.Generic.List<DataRow>();
            var finished = new System.Collections.Generic.List<DataRow>();
            foreach (DataRow row in rows.Rows)
            {
                string status = row["status"].ToString();
                if (status.Equals("Serving", StringComparison.OrdinalIgnoreCase) && current == null) current = row;
                else if (IsClosed(status)) finished.Add(row);
                else pending.Add(row);
            }
            _openTasks = pending.Count + (current != null ? 1 : 0);

            BuildCurrentSection(current);

            _tasks.SuspendLayout();
            _tasks.Controls.Clear();
            foreach (DataRow row in pending) _tasks.Controls.Add(BuildTask(row, false));
            foreach (DataRow row in finished) _tasks.Controls.Add(BuildTask(row, true));
            _tasks.ResumeLayout();

            int total = rows.Rows.Count;
            int done = finished.Count;
            _summary.Text = total == 0
                ? "Legacy single-service ticket"
                : done + " of " + total + " task" + (total == 1 ? "" : "s") + " finished";
            // Keep the completion action visibly green instead of turning into an uncoloured
            // grey strip. CompleteVisit still blocks completion and explains what remains.
            _complete.Enabled = total > 0;
            _complete.BackColor = UiTheme.Success;
            _complete.ForeColor = Color.White;

            // Abandoning needs something still open to abandon; "this task" needs one actually
            // in progress, since that is the one the operator is looking at.
            _abandonTask.Enabled = current != null;
            _abandonAll.Enabled = _openTasks > 0;
        }

        /// <summary>
        /// Per-service rows. <c>abandon_reason</c> arrives with migration 55 — a database that
        /// has not had it applied yet must still show the drawer rather than throw on every
        /// 3-second refresh, so the column is requested and then dropped on 1054.
        /// </summary>
        private DataTable LoadServices()
        {
            try
            {
                return Db.Pull(
                    "SELECT id, service_code, service_label, status, abandon_reason FROM queue_ticket_services " +
                    "WHERE ticket_id = @id ORDER BY id", new MySqlParameter("@id", _ticketId));
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                DataTable dt = Db.Pull(
                    "SELECT id, service_code, service_label, status FROM queue_ticket_services " +
                    "WHERE ticket_id = @id ORDER BY id", new MySqlParameter("@id", _ticketId));
                dt.Columns.Add("abandon_reason", typeof(string));
                return dt;
            }
        }

        private static bool IsClosed(string status) =>
            status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("Abandoned", StringComparison.OrdinalIgnoreCase);

        private static string Cell(DataRow row, string column, string fallback = null)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value) return fallback;
            string s = row[column].ToString().Trim();
            return s.Length == 0 ? fallback : s;
        }

        /// <summary>The line under the client's name: who else is on the ticket, how to reach
        /// them, the priority lane they are in, and when they took the number.</summary>
        private static string ClientMeta(DataRow t)
        {
            var bits = new System.Collections.Generic.List<string>();
            string spouse = Cell(t, "spouse_full_name");
            if (spouse != null) bits.Add("with " + spouse);
            string contact = Cell(t, "contact_no");
            if (contact != null) bits.Add(contact);
            string priority = Cell(t, "priority");
            if (priority != null && !priority.Equals("Regular", StringComparison.OrdinalIgnoreCase))
                bits.Add(priority.ToUpperInvariant() + " LANE");
            string id = Cell(t, "valid_id_type");
            if (id != null) bits.Add("ID: " + id);
            string issued = Cell(t, "issued");
            if (issued != null) bits.Add("issued " + issued);
            return string.Join("  ·  ", bits);
        }

        /// <summary>
        /// What the client actually asked for, per service code. The kiosk writes a structured
        /// intake row for the two services that need one (ctc_requests, breqs_requests), so the
        /// window can read the request instead of asking the client to repeat it. Anything else
        /// falls back to the one line the ticket itself carries.
        /// <para/>
        /// Every query is wrapped: an unmigrated database (no ctc_requests yet) must lose the
        /// detail lines, not the drawer.
        /// </summary>
        private void LoadRequestDetails(DataRow ticket)
        {
            _details.Clear();
            try
            {
                DataTable c = Db.Pull(
                    "SELECT doc_type, copies, purpose, relationship, registry_no, owner_first, owner_middle, owner_last, " +
                    "spouse_first, spouse_middle, spouse_last, event_date, event_city, event_province, " +
                    "father_name, mother_maiden_name, remarks FROM ctc_requests " +
                    "WHERE queue_ticket_id = @id ORDER BY id DESC LIMIT 1",
                    new MySqlParameter("@id", _ticketId));
                if (c.Rows.Count > 0) _details["CTC"] = CtcLines(c.Rows[0]);
            }
            catch { }

            try
            {
                DataTable b = Db.Pull(
                    "SELECT request_no, doc_type, copies, purpose, relationship, owner_first, owner_middle, owner_last, " +
                    "spouse_first, spouse_middle, spouse_last, event_date, event_city, event_province, " +
                    "father_name, mother_maiden_name, valid_id_type, valid_id_no FROM breqs_requests " +
                    "WHERE queue_ticket_id = @id ORDER BY id DESC LIMIT 1",
                    new MySqlParameter("@id", _ticketId));
                if (b.Rows.Count > 0) _details["BREQS"] = BreqsLines(b.Rows[0]);
            }
            catch { }

            // Fallback for every other service: the kiosk's own one-line note on the ticket.
            string note = Cell(ticket, "purpose");
            if (note != null && !_details.ContainsKey("CTC"))
                _details["*"] = new System.Collections.Generic.List<string> { note };
        }

        private static System.Collections.Generic.List<string> CtcLines(DataRow r)
        {
            var lines = new System.Collections.Generic.List<string>();
            string doc = Cell(r, "doc_type", "Record");
            int copies = r["copies"] == DBNull.Value ? 1 : Convert.ToInt32(r["copies"]);
            lines.Add(doc + " certificate  ·  " + copies + (copies == 1 ? " copy" : " copies"));

            string owner = JoinNames(Cell(r, "owner_first"), Cell(r, "owner_middle"), Cell(r, "owner_last"));
            if (owner != null) lines.Add((doc == "Death" ? "Deceased: " : doc == "Marriage" ? "Spouse 1: " : "Name: ") + owner);
            string spouse = JoinNames(Cell(r, "spouse_first"), Cell(r, "spouse_middle"), Cell(r, "spouse_last"));
            if (spouse != null) lines.Add("Spouse 2: " + spouse);

            string reg = Cell(r, "registry_no");
            if (reg != null) lines.Add("Registry no: " + reg);
            string when = DateText(r, "event_date");
            string where = JoinPlace(Cell(r, "event_city"), Cell(r, "event_province"));
            if (when != null || where != null)
                lines.Add("Event: " + string.Join("  ·  ", Kept(when, where)));

            string father = Cell(r, "father_name"), mother = Cell(r, "mother_maiden_name");
            if (father != null) lines.Add("Father: " + father);
            if (mother != null) lines.Add("Mother: " + mother);

            string purpose = Cell(r, "purpose"), rel = Cell(r, "relationship");
            if (purpose != null || rel != null)
                lines.Add(string.Join("  ·  ", Kept(purpose, rel == null ? null : "requester is the " + rel)));
            string remarks = Cell(r, "remarks");
            if (remarks != null) lines.Add("Note: " + remarks);
            return lines;
        }

        private static System.Collections.Generic.List<string> BreqsLines(DataRow r)
        {
            var lines = new System.Collections.Generic.List<string>();
            string no = Cell(r, "request_no");
            string doc = Cell(r, "doc_type", "PSA");
            int copies = r["copies"] == DBNull.Value ? 1 : Convert.ToInt32(r["copies"]);
            lines.Add("PSA " + doc + "  ·  " + copies + (copies == 1 ? " copy" : " copies") + (no == null ? "" : "  ·  " + no));

            string owner = JoinNames(Cell(r, "owner_first"), Cell(r, "owner_middle"), Cell(r, "owner_last"));
            if (owner != null) lines.Add("Name: " + owner);
            string spouse = JoinNames(Cell(r, "spouse_first"), Cell(r, "spouse_middle"), Cell(r, "spouse_last"));
            if (spouse != null) lines.Add("Spouse: " + spouse);
            string when = DateText(r, "event_date");
            string where = JoinPlace(Cell(r, "event_city"), Cell(r, "event_province"));
            if (when != null || where != null) lines.Add("Event: " + string.Join("  ·  ", Kept(when, where)));
            string father = Cell(r, "father_name"), mother = Cell(r, "mother_maiden_name");
            if (father != null) lines.Add("Father: " + father);
            if (mother != null) lines.Add("Mother: " + mother);
            string idType = Cell(r, "valid_id_type"), idNo = Cell(r, "valid_id_no");
            if (idType != null) lines.Add("ID: " + idType + (idNo == null ? "" : " " + idNo));
            string purpose = Cell(r, "purpose");
            if (purpose != null) lines.Add("Purpose: " + purpose);
            return lines;
        }

        private static string DateText(DataRow r, string column)
        {
            if (!r.Table.Columns.Contains(column) || r[column] == DBNull.Value) return null;
            return Convert.ToDateTime(r[column]).ToString("MMMM d, yyyy");
        }

        /// <summary>City and province are two places, not two parts of one name — they read
        /// "Tuguegarao City, Cagayan", never "Tuguegarao City Cagayan".</summary>
        private static string JoinPlace(params string[] parts)
        {
            var kept = Kept(parts);
            return kept.Count == 0 ? null : string.Join(", ", kept);
        }

        private static string JoinNames(params string[] parts)
        {
            var kept = Kept(parts);
            return kept.Count == 0 ? null : string.Join(" ", kept);
        }

        private static System.Collections.Generic.List<string> Kept(params string[] parts)
        {
            var kept = new System.Collections.Generic.List<string>();
            foreach (string p in parts) if (!string.IsNullOrWhiteSpace(p)) kept.Add(p.Trim());
            return kept;
        }

        private System.Collections.Generic.List<string> DetailsFor(string serviceCode)
        {
            System.Collections.Generic.List<string> lines;
            if (_details.TryGetValue(serviceCode ?? "", out lines)) return lines;
            if (_details.TryGetValue("*", out lines)) return lines;
            return null;
        }

        /// <summary>
        /// The emphasized, fixed-at-top section for whichever service is currently being
        /// processed. Deliberately outside the scrollable <see cref="_tasks"/> list so it stays
        /// on screen no matter how far the operator scrolls the remaining tasks.
        /// </summary>
        private void BuildCurrentSection(DataRow current)
        {
            _currentHost.Controls.Clear();
            if (current == null)
            {
                _currentHost.Height = 0;
                return;
            }

            int taskId = Convert.ToInt32(current["id"]);
            string serviceLabel = current["service_label"].ToString();
            var lines = DetailsFor(current["service_code"].ToString());
            int detailH = lines == null ? 0 : lines.Count * 15 + 6;

            var card = new CardPanel
            {
                Dock = DockStyle.Top,
                Height = 118 + detailH,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(14, 12, 14, 12),
                Radius = 10,
                DrawShadow = true,
                CardColor = UiTheme.AccentTint,
                LineColor = UiTheme.Accent
            };

            var tag = new Label
            {
                Text = "CURRENT TASK",
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = UiTheme.Accent,
                BackColor = Color.Transparent
            };
            var name = new Label
            {
                Text = serviceLabel,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 26,
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                BackColor = Color.Transparent
            };
            var sub = new Label
            {
                Text = "This is the transaction you are currently working on.",
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 8F),
                ForeColor = UiTheme.Muted,
                BackColor = Color.Transparent
            };
            Control detail = DetailBlock(lines, detailH);

            var buttonRow = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent };
            var open = MiniButton("Continue", UiTheme.Accent, Color.White);
            open.Location = new Point(0, 0);
            open.Click += (s, e) => _shell.OpenQueueTask(_ticketId, taskId, _ticketCode, current["service_code"].ToString());

            var done = MiniButton("Finish", UiTheme.Success, Color.White);
            done.Location = new Point(154, 0);
            done.Click += (s, e) => FinishTask(taskId, serviceLabel);

            buttonRow.Controls.Add(open);
            buttonRow.Controls.Add(done);

            card.Controls.Add(buttonRow);
            if (detail != null) card.Controls.Add(detail);
            card.Controls.Add(sub);
            card.Controls.Add(name);
            card.Controls.Add(tag);

            _currentHost.Height = 128 + detailH;
            _currentHost.Controls.Add(card);
        }

        /// <summary>
        /// The client's own request, rendered under the service name. Drawn as one wrapped text
        /// block rather than a control per line — the list is short, it is read-only, and one
        /// Label keeps the card's height arithmetic honest.
        /// </summary>
        private static Control DetailBlock(System.Collections.Generic.List<string> lines, int height)
        {
            if (lines == null || lines.Count == 0) return null;
            return new Label
            {
                Text = string.Join(Environment.NewLine, lines),
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = height,
                Font = new Font("Segoe UI", 8F),
                ForeColor = UiTheme.Ink,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 2),
            };
        }

        private Control BuildTask(DataRow row, bool finished)
        {
            int taskId = Convert.ToInt32(row["id"]);
            string serviceCode = row["service_code"].ToString();
            string serviceLabel = row["service_label"].ToString();
            bool abandoned = row["status"].ToString().Equals("Abandoned", StringComparison.OrdinalIgnoreCase);
            string reason = Cell(row, "abandon_reason");

            // An abandoned task keeps its reason where the service detail would go — that is the
            // one thing about it still worth reading.
            var lines = abandoned
                ? new System.Collections.Generic.List<string> { reason == null ? "No reason recorded." : "Reason: " + reason }
                : DetailsFor(serviceCode);
            int detailTop = 57;
            int detailH = lines == null ? 0 : lines.Count * 15 + 4;

            var card = new Panel
            {
                Width = 286,
                Height = (finished ? 62 : 92) + detailH,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(10),
                BackColor = UiTheme.Surface
            };
            var name = new Label
            {
                Text = serviceLabel,
                AutoEllipsis = true,
                Location = new Point(10, 9),
                Size = new Size(266, 23),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = finished ? UiTheme.Muted : UiTheme.Ink
            };
            var state = new Label
            {
                Text = abandoned ? "Abandoned" : finished ? "Finished" : "Pending",
                Location = new Point(10, 35),
                Size = new Size(140, 22),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = abandoned ? UiTheme.Warning : finished ? UiTheme.Success : UiTheme.Muted
            };

            if (lines != null)
            {
                card.Controls.Add(new Label
                {
                    Text = string.Join(Environment.NewLine, lines),
                    AutoSize = false,
                    Location = new Point(10, detailTop),
                    Size = new Size(266, detailH),
                    Font = new Font("Segoe UI", 8F),
                    ForeColor = abandoned ? UiTheme.Muted : UiTheme.Ink,
                });
            }

            if (finished)
            {
                if (!abandoned)
                {
                    var check = new Label
                    {
                        Text = "✓",
                        Location = new Point(250, 33),
                        Size = new Size(26, 26),
                        Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                        ForeColor = UiTheme.Success,
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    card.Controls.Add(check);
                }
            }
            else
            {
                var process = MiniButton("Process", UiTheme.Accent, Color.White);
                process.Location = new Point(10, detailTop + detailH + 3);
                process.Width = card.ClientSize.Width - 20;
                process.Click += (s, e) => ProcessTask(taskId, serviceCode, serviceLabel);
                card.Controls.Add(process);
            }

            card.Controls.Add(name);
            card.Controls.Add(state);
            return card;
        }

        private static Button MiniButton(string text, Color back, Color fore)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(122, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        // ================================================================
        //  Actions
        // ================================================================

        private void ProcessTask(int taskId, string serviceCode, string serviceLabel)
        {
            DataTable active = Db.Pull(
                "SELECT id, service_label FROM queue_ticket_services " +
                "WHERE ticket_id = @ticket AND status = 'Serving' AND id <> @task LIMIT 1",
                new MySqlParameter("@ticket", _ticketId), new MySqlParameter("@task", taskId));
            if (active.Rows.Count > 0)
            {
                MessageBox.Show("Finish the task already in progress (" +
                    active.Rows[0]["service_label"] + ") before starting another one.",
                    "Client tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Db.Push("UPDATE queue_ticket_services SET status = 'Serving' " +
                    "WHERE id = @id AND status = 'Pending'",
                new MySqlParameter("@id", taskId));
            QueueTaskContext.Link(_ticketId, taskId, _ticketCode, serviceCode);
            // MainForm.OpenQueueTask collapses the rail once the processing module is open.
            _shell.OpenQueueTask(_ticketId, taskId, _ticketCode, serviceCode);
            Reload();
        }

        private void FinishTask(int taskId, string serviceLabel)
        {
            if (MessageBox.Show("Mark \"" + serviceLabel + "\" as finished?",
                "Finish task", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            Db.Push("UPDATE queue_ticket_services SET status = 'Completed' " +
                    "WHERE id = @id AND status = 'Serving'",
                new MySqlParameter("@id", taskId));
            if (QueueTaskContext.TaskId == taskId) QueueTaskContext.Clear();
            Reload();
            _shell.RefreshQueueHeader();
        }

        /// <summary>
        /// Drops the task in progress. The reason is REQUIRED and stored: an abandoned request
        /// still counts in the day's queue figures, and "why" is the only thing that tells a
        /// no-show apart from a request the office could not serve.
        /// </summary>
        private void AbandonCurrentTask()
        {
            DataTable active = Db.Pull(
                "SELECT id, service_label FROM queue_ticket_services " +
                "WHERE ticket_id = @t AND status = 'Serving' LIMIT 1", new MySqlParameter("@t", _ticketId));
            if (active.Rows.Count == 0)
            {
                MessageBox.Show("No task is in progress. Press Process on the task you want to start, " +
                    "or use Abandon All Tasks to end the whole visit.",
                    "Client tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Reload();
                return;
            }

            int taskId = Convert.ToInt32(active.Rows[0]["id"]);
            string label = active.Rows[0]["service_label"].ToString();
            string reason = AbandonReasonDialog.Ask(FindForm(), "Abandon \"" + label + "\"?",
                "This task will be closed unfinished and the client can move on to their next one.");
            if (reason == null) return;

            MarkAbandoned("id = " + taskId, reason);
            if (QueueTaskContext.TaskId == taskId) QueueTaskContext.Clear();
            Audit.Write("Update", "queue_ticket_services", taskId,
                "Abandoned " + label + " on " + _ticketCode + " — " + reason);
            Reload();
            _shell.RefreshQueueHeader();
        }

        /// <summary>
        /// Ends the whole visit unfinished — the client left, or nothing on the ticket can be
        /// served today. Every open task is closed with the same reason and the WINDOW IS FREED,
        /// which is the point: otherwise a walked-away client holds a counter until someone
        /// notices. The ticket is kept with final_status 'Abandoned' rather than deleted.
        /// </summary>
        private void AbandonAllTasks()
        {
            int open = Db.GetCount(
                "SELECT id FROM queue_ticket_services WHERE ticket_id = " + _ticketId +
                " AND status NOT IN ('Completed','Abandoned')");
            if (open == 0)
            {
                MessageBox.Show("Every task on this ticket is already finished or abandoned. " +
                    "Use Complete Client Visit to close it.",
                    "Client tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string reason = AbandonReasonDialog.Ask(FindForm(),
                "Abandon all " + open + " remaining task" + (open == 1 ? "" : "s") + " on " + _ticketCode + "?",
                "The visit ends here and your window is freed for the next client. " +
                "The ticket stays on record, marked abandoned.");
            if (reason == null) return;

            MarkAbandoned("ticket_id = " + _ticketId + " AND status NOT IN ('Completed','Abandoned')", reason);
            Db.Push("UPDATE queue_tickets SET status = 'Completed', final_status = 'Abandoned', " +
                    "completed_at = NOW(), window_no = NULL WHERE id = @id",
                new MySqlParameter("@id", _ticketId));
            Audit.Write("Update", "queue_tickets", _ticketId,
                "Abandoned " + _ticketCode + " (" + open + " task(s)) — " + reason);
            QueueTaskContext.Clear();
            _shell.RefreshQueueHeader();
            MessageBox.Show(_ticketCode + " was closed as abandoned. Your window is ready for the next client.",
                "Visit abandoned", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Reload();
        }

        /// <summary>
        /// One write for both abandon paths. The reason columns arrive with migration 55, so a
        /// database without them still records the STATE (which is what unblocks the window)
        /// and simply loses the reason text.
        /// </summary>
        private void MarkAbandoned(string whereClause, string reason)
        {
            try
            {
                Db.Push("UPDATE queue_ticket_services SET status = 'Abandoned', abandoned_at = NOW(), " +
                        "abandon_reason = @r, abandoned_by = @u WHERE " + whereClause,
                    new MySqlParameter("@r", reason),
                    new MySqlParameter("@u", Session.User != null ? (object)Session.User.Id : DBNull.Value));
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                Db.Push("UPDATE queue_ticket_services SET status = 'Abandoned' WHERE " + whereClause);
            }
        }

        private void CompleteVisit()
        {
            // An abandoned task is closed, not outstanding — before this it counted as remaining
            // and made the visit impossible to complete.
            int remaining = Db.GetCount(
                "SELECT id FROM queue_ticket_services WHERE ticket_id = " + _ticketId +
                " AND status NOT IN ('Completed','Abandoned')");
            if (remaining > 0)
            {
                MessageBox.Show("Every client task must be finished before the visit can be completed. " +
                    "If one of them cannot be served today, use Abandon This Task and record why.",
                    "Client tasks", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Reload();
                return;
            }

            Db.Push("UPDATE queue_tickets SET status = 'Completed', final_status = 'Completed', " +
                    "completed_at = NOW(), window_no = NULL WHERE id = @id",
                new MySqlParameter("@id", _ticketId));
            Audit.Write("Update", "queue_tickets", _ticketId, "Completed " + _ticketCode);
            QueueTaskContext.Clear();
            _shell.RefreshQueueHeader();
            MessageBox.Show(_ticketCode + " is complete. Your window is ready for the next client.",
                "Client visit completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refresh.Dispose();
                if (Parent != null) Parent.ClientSizeChanged -= Parent_ClientSizeChanged;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Asks WHY a task is being abandoned. A plain Yes/No box would have been less work, but
    /// then the queue figures would carry abandoned tickets with nothing to explain them — and
    /// "client left" and "record not found in the registry books" are two very different
    /// problems for the office to see at the end of the month.
    /// <para/>
    /// The common answers are one click; anything else is typed. Returns null when cancelled.
    /// </summary>
    internal sealed class AbandonReasonDialog : Form
    {
        private static readonly string[] Common =
        {
            "Client left / no longer at the counter",
            "Client asked to come back another day",
            "Record not found in the registry books",
            "Requirements incomplete",
            "Wrong service selected at the kiosk",
            "Referred to another office",
        };

        private readonly ComboBox _reason = new ComboBox();

        private AbandonReasonDialog(string title, string explain)
        {
            Text = "Abandon task";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = MaximizeBox = false;
            ClientSize = new Size(470, 214);
            BackColor = UiTheme.Surface;
            Font = new Font("Segoe UI", 9.5F);

            Controls.Add(new Label
            {
                Text = title,
                Location = new Point(20, 18),
                Size = new Size(430, 46),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
            });
            Controls.Add(new Label
            {
                Text = explain,
                Location = new Point(20, 62),
                Size = new Size(430, 38),
                ForeColor = UiTheme.Muted,
            });
            Controls.Add(new Label
            {
                Text = "Reason (required)",
                Location = new Point(20, 106),
                Size = new Size(430, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
            });

            // Editable: the six listed reasons cover the usual cases, but an office will always
            // meet a seventh, and forcing it into "Others" would lose the only useful part.
            _reason.DropDownStyle = ComboBoxStyle.DropDown;
            _reason.Items.AddRange(Common);
            _reason.Location = new Point(20, 128);
            _reason.Size = new Size(430, 28);
            Controls.Add(_reason);

            var cancel = Chip("Cancel", UiTheme.Chrome, UiTheme.Ink);
            cancel.Location = new Point(228, 168);
            cancel.DialogResult = DialogResult.Cancel;

            var ok = Chip("Abandon", UiTheme.Warning, Color.White);
            ok.Location = new Point(340, 168);
            ok.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_reason.Text))
                {
                    MessageBox.Show("Please choose or type a reason.", "Reason required",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _reason.Focus();
                    return;
                }
                DialogResult = DialogResult.OK;
            };

            Controls.Add(cancel);
            Controls.Add(ok);
            AcceptButton = ok;
            CancelButton = cancel;
            UiTheme.Polish(this);
        }

        private static Button Chip(string text, Color back, Color fore)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(104, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        public static string Ask(IWin32Window owner, string title, string explain)
        {
            using (var d = new AbandonReasonDialog(title, explain))
                return d.ShowDialog(owner) == DialogResult.OK ? d._reason.Text.Trim() : null;
        }
    }
}
