using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private readonly Label _summary = new Label();
        private readonly Panel _currentHost = new Panel { Dock = DockStyle.Top, AutoSize = false, Height = 0 };
        private readonly FlowLayoutPanel _tasks = new FlowLayoutPanel();
        private readonly Button _complete = new Button();
        private readonly Timer _refresh = new Timer { Interval = 3000 };

        private int _ticketId;
        private string _ticketCode;
        private string _renderSignature;
        private bool _expanded = true;
        private bool _moduleAllowed = true;

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
            var head = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = UiTheme.Surface, Padding = new Padding(16, 12, 16, 8) };
            _queue.AutoSize = false;
            _queue.Dock = DockStyle.Top;
            _queue.Height = 28;
            _queue.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            _queue.ForeColor = UiTheme.Ink;

            _summary.AutoSize = false;
            _summary.Dock = DockStyle.Top;
            _summary.Height = 24;
            _summary.Font = new Font("Segoe UI", 9F);
            _summary.ForeColor = UiTheme.Muted;

            head.Controls.Add(_summary);
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
            _complete.Margin = new Padding(10);
            _complete.FlatStyle = FlatStyle.Flat;
            _complete.FlatAppearance.BorderSize = 0;
            _complete.BackColor = UiTheme.Success;
            _complete.ForeColor = Color.White;
            _complete.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _complete.Click += (s, e) => CompleteVisit();

            // Current Task is a fixed, non-scrolling section pinned at the top of the body —
            // it must stay visible while the operator scrolls the remaining task list below it.
            _currentHost.BackColor = UiTheme.PageBg;
            _currentHost.Padding = new Padding(10, 10, 10, 0);

            _body.Controls.Add(_tasks);
            _body.Controls.Add(_currentHost);
            _body.Controls.Add(_complete);
            _body.Controls.Add(head);
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
                "SELECT id, ticket_code FROM queue_tickets " +
                "WHERE window_no = @w AND status IN ('Accepted','Serving') " +
                "AND DATE(created_at) = CURDATE() ORDER BY id DESC LIMIT 1",
                new MySqlParameter("@w", Session.WindowId));

            if (ticket.Rows.Count == 0)
            {
                _ticketId = 0;
                _ticketCode = null;
                _renderSignature = null;
                QueueTaskContext.Clear();
                UpdateVisibility();
                return;
            }

            _ticketId = Convert.ToInt32(ticket.Rows[0]["id"]);
            _ticketCode = ticket.Rows[0]["ticket_code"].ToString();
            _queue.Text = "Client " + _ticketCode;
            UpdateVisibility();

            DataTable rows = Db.Pull(
                "SELECT id, service_code, service_label, status FROM queue_ticket_services " +
                "WHERE ticket_id = @id ORDER BY id", new MySqlParameter("@id", _ticketId));

            var signatureBuilder = new StringBuilder().Append(_ticketId).Append(':');
            foreach (DataRow row in rows.Rows)
                signatureBuilder.Append(row["id"]).Append('~')
                    .Append(row["service_code"]).Append('~')
                    .Append(row["service_label"]).Append('~')
                    .Append(row["status"]).Append('|');
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
                else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase)) finished.Add(row);
                else pending.Add(row);
            }

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

            var card = new CardPanel
            {
                Dock = DockStyle.Top,
                Height = 118,
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
            card.Controls.Add(sub);
            card.Controls.Add(name);
            card.Controls.Add(tag);

            _currentHost.Height = 128;
            _currentHost.Controls.Add(card);
        }

        private Control BuildTask(DataRow row, bool finished)
        {
            int taskId = Convert.ToInt32(row["id"]);
            string serviceCode = row["service_code"].ToString();
            string serviceLabel = row["service_label"].ToString();

            var card = new Panel
            {
                Width = 286,
                Height = 92,
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
                Text = finished ? "Finished" : "Pending",
                Location = new Point(10, 35),
                Size = new Size(120, 22),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = finished ? UiTheme.Success : UiTheme.Muted
            };

            if (finished)
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
            else
            {
                var process = MiniButton("Process", UiTheme.Accent, Color.White);
                process.Location = new Point(10, 60);
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

        private void CompleteVisit()
        {
            int remaining = Db.GetCount(
                "SELECT id FROM queue_ticket_services WHERE ticket_id = " + _ticketId +
                " AND status <> 'Completed'");
            if (remaining > 0)
            {
                MessageBox.Show("Every client task must be finished before the visit can be completed.",
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
}
