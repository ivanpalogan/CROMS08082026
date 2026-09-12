using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Shown the moment a ticket is Accepted at a window, replacing an OK-only message box that
    /// merely *described* the next control ("press Call Client, or click the window card") and
    /// left the operator to go find it. A new user had no way to discover that the big card in
    /// the middle of the board was clickable at all.
    /// <para/>
    /// Instead this is a prep checklist built from the ticket's OWN requested services, with the
    /// call action on the same window: tick what you have ready, then Call Client. Ticking is
    /// encouraged rather than enforced — closing this dialog leaves the ticket Accepted and the
    /// toolbar's Call Client button still works, so it can never trap anyone.
    /// </summary>
    internal sealed class TicketPrepForm : Form
    {
        private sealed class Item
        {
            public Panel Panel;
            public string Text;
            public bool Checked;
            public float HoverT;
        }

        private readonly List<Item> _items = new List<Item>();
        private Button _btnCall;
        private Label _lblProgress;

        public TicketPrepForm(int ticketId, string ticketCode, string windowName)
        {
            Text = "Prepare — " + ticketCode;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = false;
            ClientSize = new Size(520, 468);
            BackColor = UiTheme.PageBg;
            Font = new Font("Segoe UI", 9.75F);

            // ---- header -------------------------------------------------------
            Controls.Add(new Label
            {
                Text = ticketCode,
                Font = new Font("Consolas", 30F, FontStyle.Bold),
                ForeColor = UiTheme.Accent,
                Location = new Point(28, 20),
                AutoSize = true
            });
            Controls.Add(new Label
            {
                Text = "accepted at " + windowName,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                Location = new Point(30, 74),
                AutoSize = true
            });
            Controls.Add(new Label
            {
                Text = "Get these ready BEFORE calling the client, so they aren't left waiting\n" +
                       "at the counter while you look.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = UiTheme.Muted,
                Location = new Point(30, 98),
                Size = new Size(460, 36)
            });

            // ---- checklist card ----------------------------------------------
            var card = new Panel
            {
                Location = new Point(28, 144),
                Size = new Size(464, 232),
                BackColor = Color.White
            };
            PaintAsCard(card);
            Controls.Add(card);

            foreach (string text in BuildChecklist(ticketId))
                AddItem(card, text);

            _lblProgress = new Label
            {
                Location = new Point(30, 386),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UiTheme.Muted
            };
            Controls.Add(_lblProgress);

            // ---- footer -------------------------------------------------------
            var later = new Button
            {
                Text = "Not yet",
                Location = new Point(28, 412),
                Size = new Size(120, 40),
                BackColor = Color.White,
                ForeColor = UiTheme.Muted,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F)
            };
            later.FlatAppearance.BorderColor = UiTheme.CardLine;
            later.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(later);

            _btnCall = new Button
            {
                Text = "Call Client",
                Location = new Point(292, 412),
                Size = new Size(200, 40),
                BackColor = UiTheme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };
            _btnCall.FlatAppearance.BorderSize = 0;
            _btnCall.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(_btnCall);

            AcceptButton = _btnCall;
            UiTheme.PolishButtons(this);
            UpdateProgress();
        }

        /// <summary>
        /// The ticket's own requested services, so the list is specific rather than generic
        /// advice. Falls back to the ticket's summary label for a legacy single-service ticket.
        /// </summary>
        private static List<string> BuildChecklist(int ticketId)
        {
            var list = new List<string>();
            try
            {
                DataTable svc = Db.Pull(
                    "SELECT service_label FROM queue_ticket_services WHERE ticket_id = @id ORDER BY id",
                    new MySqlParameter("@id", ticketId));
                foreach (DataRow r in svc.Rows)
                    list.Add("Documents ready for " + r["service_label"]);

                if (list.Count == 0)
                {
                    DataTable t = Db.Pull("SELECT type_label FROM queue_tickets WHERE id = @id",
                        new MySqlParameter("@id", ticketId));
                    if (t.Rows.Count > 0 && t.Rows[0]["type_label"] != DBNull.Value)
                        list.Add("Documents ready for " + t.Rows[0]["type_label"]);
                }
            }
            catch { /* DB blip — fall through to the generic item below */ }

            list.Add("Counter is free and ready for the client");
            return list;
        }

        private void AddItem(Panel card, string text)
        {
            var item = new Item { Text = text };
            var row = new Panel
            {
                Location = new Point(12, 12 + _items.Count * 44),
                Size = new Size(card.Width - 24, 40),
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            DoubleBuffer(row);
            item.Panel = row;

            row.Paint += (s, e) => PaintItem(e.Graphics, item);
            row.Click += (s, e) => { item.Checked = !item.Checked; row.Invalidate(); UpdateProgress(); };
            HoverFade.Attach(row, 140, t => { item.HoverT = t; row.Invalidate(); });

            _items.Add(item);
            card.Controls.Add(row);
        }

        private void PaintItem(Graphics g, Item it)
        {
            Panel p = it.Panel;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            float hov = it.Checked ? 0f : it.HoverT;
            Color fill = it.Checked ? UiTheme.AccentTint
                                    : HoverFade.Lerp(Color.White, Color.FromArgb(250, 251, 254), hov);
            Color border = it.Checked ? UiTheme.Accent : HoverFade.Lerp(UiTheme.CardLine, UiTheme.Accent, hov);

            var rect = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
            using (var path = Rounded(rect, 8))
            {
                using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                using (var pen = new Pen(border, it.Checked ? 1.8f : 1f)) g.DrawPath(pen, path);
            }

            var box = new Rectangle(13, (p.Height - 20) / 2, 20, 20);
            using (var bp = Rounded(box, 5))
            {
                if (it.Checked)
                {
                    using (var b = new SolidBrush(UiTheme.Accent)) g.FillPath(b, bp);
                    using (var wp = new Pen(Color.White, 2.1f)
                    { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                        g.DrawLines(wp, new[]
                        {
                            new PointF(box.X + 5f,   box.Y + 10f),
                            new PointF(box.X + 8.5f, box.Y + 13.5f),
                            new PointF(box.X + 15f,  box.Y + 6.5f)
                        });
                }
                else
                {
                    using (var b = new SolidBrush(Color.White)) g.FillPath(b, bp);
                    using (var pen = new Pen(HoverFade.Lerp(Color.FromArgb(198, 204, 214), UiTheme.Accent, hov), 1.4f))
                        g.DrawPath(pen, bp);
                }
            }

            using (var f = new Font("Segoe UI", 10F, it.Checked ? FontStyle.Bold : FontStyle.Regular))
            using (var b = new SolidBrush(it.Checked ? UiTheme.Accent : UiTheme.Ink))
            using (var fmt = new StringFormat
            { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                g.DrawString(it.Text, f, b, new RectangleF(46, 0, p.Width - 58, p.Height), fmt);
        }

        /// <summary>
        /// Reflects progress and nudges toward finishing the list, but never blocks: Call Client
        /// stays enabled throughout. A hard gate would trap an operator whose checklist doesn't
        /// match reality (walk-in with no papers, a service they can serve immediately).
        /// </summary>
        private void UpdateProgress()
        {
            int done = _items.Count(i => i.Checked);
            bool all = done == _items.Count && _items.Count > 0;
            _lblProgress.Text = all
                ? "All set — call the client."
                : done + " of " + _items.Count + " ready";
            _lblProgress.ForeColor = all ? UiTheme.Success : UiTheme.Muted;
            _btnCall.Text = all ? "Call Client" : "Call Client anyway";
        }

        // ------------------------------------------------------------ painting
        private void PaintAsCard(Control panel)
        {
            DoubleBuffer(panel);
            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                var r = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                using (var path = Rounded(r, 10))
                {
                    using (var b = new SolidBrush(Color.White)) g.FillPath(b, path);
                    using (var pen = new Pen(UiTheme.CardLine, 1f)) g.DrawPath(pen, path);
                }
            };
        }

        private static void DoubleBuffer(Control c)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(c, true);
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            if (d <= 0 || d > r.Width || d > r.Height) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
