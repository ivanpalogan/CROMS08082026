using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CROMS.Display
{
    /// <summary>
    /// Public "Now Serving" board — view-only, full screen, self-refreshing every
    /// 2 seconds. Builds one card per ACTIVE window from the shared `windows` table,
    /// so an admin can add / rename / activate / deactivate any number of windows and
    /// this board follows with no code change (was hardcoded to exactly 3 before).
    /// No controls: press Esc or double-click to close.
    /// </summary>
    public class DisplayForm : Form
    {
        // Minutes without a heartbeat before an operator counts as offline.
        // (Kept in sync with the main app's WindowAssignmentForm.StaleMinutes = 2.)
        private const int StaleMinutes = 2;

        // windowId → its ticket-code / sub-text labels. Rebuilt only when the set of
        // active windows changes; label text is refreshed every tick.
        private readonly Dictionary<int, Label> _codeLabels = new Dictionary<int, Label>();
        private readonly Dictionary<int, Label> _subLabels = new Dictionary<int, Label>();
        private string _builtSignature = "";   // ids+names the current grid was built for
        private TableLayoutPanel _grid;
        private Label _header;
        private Label _clock;
        private DataTable _lastWins;           // last data, so a resize can re-lay the grid
        private Timer _timer;

        // Every font/height on this board is a multiple of this factor so the whole
        // board scales with the actual screen — small laptop or big TV, it fits and
        // fills the same way instead of being sized for one fixed resolution.
        // 1080p is the design baseline; clamped so tiny/huge screens stay sane.
#pragma warning disable CS0108 // 'DisplayForm.Scale' hides inherited member 'Control.Scale(float)'. Use the new keyword if hiding was intended.
        private float Scale =>
#pragma warning restore CS0108 // 'DisplayForm.Scale' hides inherited member 'Control.Scale(float)'. Use the new keyword if hiding was intended.
            Math.Min(2.0f, Math.Max(0.4f, ClientSize.Height / 1080f));

        public DisplayForm()
        {
            Text = "CROMS — Now Serving";
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(17, 24, 39);
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
            DoubleClick += (s, e) => Close();

            BuildUi();

            _timer = new Timer { Interval = 2000 };
            _timer.Tick += (s, e) => Reload();
            _timer.Start();
            Load += (s, e) => { ApplyChrome(); Reload(); };

            // Screen / monitor changed size → rescale every font + re-lay the cards so
            // the board fits the new resolution (moved to a bigger TV, smaller laptop…).
            Resize += (s, e) => { ApplyChrome(); _builtSignature = ""; Reload(); };
        }

        /// <summary>Rescales the fixed chrome (title + clock + grid padding) to the screen.</summary>
        private void ApplyChrome()
        {
            float s = Scale;
            if (_header != null)
            {
                _header.Font = new Font(_header.Font.FontFamily, 40F * s, FontStyle.Bold);
                _header.Height = (int)(150 * s);
            }
            if (_clock != null)
            {
                _clock.Font = new Font(_clock.Font.FontFamily, 14F * s);
                _clock.Height = (int)(50 * s);
            }
            if (_grid != null)
                _grid.Padding = new Padding((int)(40 * s));
        }

        private void BuildUi()
        {
            _grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(40),
                BackColor = Color.Transparent
            };

            _header = new Label
            {
                Text = "NOW SERVING",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 40F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 150
            };

            _clock = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 14F),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 50
            };

            // Fill first, then the docked edges, so the grid fills the middle.
            Controls.Add(_grid);
            Controls.Add(_clock);
            Controls.Add(_header);
        }

        private void Reload()
        {
            try
            {
                DataTable wins = Db.Pull(
                    "SELECT id, window_name, " +
                    "(current_operator IS NOT NULL AND last_heartbeat > " +
                    "(NOW() - INTERVAL " + StaleMinutes + " MINUTE)) AS online " +
                    "FROM windows WHERE status = 'Active' ORDER BY display_order, id");

                // Rebuild the layout only when windows are added / removed / renamed.
                var sig = new StringBuilder();
                foreach (DataRow w in wins.Rows)
                    sig.Append(w["id"]).Append(':').Append(w["window_name"]).Append('|');
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

                    // Public board shows the QUEUE NUMBER ONLY (no transaction/service name)
                    // for client privacy + a cleaner display. Only 'Serving' tickets appear
                    // (Accepted ones are still being located and stay private).
                    DataTable dt = Db.Pull(
                        "SELECT ticket_code FROM queue_tickets " +
                        "WHERE status = 'Serving' AND window_no = " + id +
                        " AND DATE(created_at) = CURDATE() ORDER BY id DESC LIMIT 1");

                    if (dt.Rows.Count == 0)
                    {
                        // No ticket: show operator presence (Online/Offline) instead of "idle".
                        _codeLabels[id].Text = "—";
                        _subLabels[id].Text = online ? "● Online — idle" : "○ Offline";
                        _subLabels[id].ForeColor = online
                            ? Color.FromArgb(74, 222, 128)      // green
                            : Color.FromArgb(148, 163, 184);    // grey
                    }
                    else
                    {
                        _codeLabels[id].Text = dt.Rows[0]["ticket_code"].ToString();
                        _subLabels[id].Text = "";   // no service name on the public board
                    }
                }
                _clock.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy   hh:mm:ss tt");
            }
            catch
            {
                // Database unreachable — show a gentle notice instead of crashing.
                _clock.Text = "Waiting for connection to the CROMS database…";
            }
        }

        /// <summary>Lays out one card per active window: up to 4 per row, then wraps.</summary>
        private void RebuildGrid(DataTable wins)
        {
            _lastWins = wins;   // remembered so a resize can re-lay with the new scale
            float sc = Scale;
            _grid.SuspendLayout();
            _grid.Controls.Clear();
            _grid.ColumnStyles.Clear();
            _grid.RowStyles.Clear();
            _codeLabels.Clear();
            _subLabels.Clear();

            int count = wins.Rows.Count;
            if (count == 0)
            {
                _grid.ColumnCount = 1;
                _grid.RowCount = 1;
                _grid.Controls.Add(new Label
                {
                    Text = "No active windows",
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Font = new Font("Segoe UI", 28F * sc),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                }, 0, 0);
                _grid.ResumeLayout();
                return;
            }

            int cols = Math.Min(count, 4);
            int rows = (int)Math.Ceiling(count / (double)cols);
            _grid.ColumnCount = cols;
            _grid.RowCount = rows;
            for (int c = 0; c < cols; c++)
                _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));
            for (int r = 0; r < rows; r++)
                _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));

            // Font scales down a little as more windows share the screen, THEN scales
            // with the screen size so it fits a small laptop or fills a big TV.
            float codeSize = (count <= 3 ? 90F : count <= 6 ? 64F : 46F) * sc;

            int i = 0;
            foreach (DataRow w in wins.Rows)
            {
                int id = Convert.ToInt32(w["id"]);
                string name = w["window_name"].ToString();

                var card = new Panel
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding((int)(20 * sc)),
                    BackColor = Color.FromArgb(31, 41, 55)
                };
                var code = new Label
                {
                    Text = "—",
                    ForeColor = Color.FromArgb(96, 165, 250),
                    Font = new Font("Consolas", codeSize, FontStyle.Bold),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var sub = new Label
                {
                    Text = "idle",
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Font = new Font("Segoe UI", 18F * sc),
                    Dock = DockStyle.Bottom,
                    Height = (int)(70 * sc),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var title = new Label
                {
                    Text = name.ToUpper(),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Font = new Font("Segoe UI", 20F * sc, FontStyle.Bold),
                    Dock = DockStyle.Top,
                    Height = (int)(70 * sc),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                // Fill control added first, then the docked edges (correct dock order).
                card.Controls.Add(code);
                card.Controls.Add(sub);
                card.Controls.Add(title);

                // Ticket code shrinks to fit its card so a long queue number stays
                // readable on the public board — never clipped or cut in half.
                AttachAutoFit(code, codeSize, 16F * sc);

                _codeLabels[id] = code;
                _subLabels[id] = sub;
                _grid.Controls.Add(card, i % cols, i / cols);
                i++;
            }

            _grid.ResumeLayout();
        }

        /// <summary>
        /// Keeps a single-line Label's text fitted to its box: the font never grows past
        /// <paramref name="maxPt"/> (so normal codes keep a consistent size) and shrinks
        /// down to <paramref name="minPt"/> when the text would otherwise overflow.
        /// Re-fits on resize and on text change, so it adapts to card size / screen size.
        /// </summary>
        private static void AttachAutoFit(Label lbl, float maxPt, float minPt)
        {
            if (lbl == null) return;
            EventHandler fit = (s, e) => FitFont(lbl, maxPt, minPt);
            lbl.Resize += fit;
            lbl.TextChanged += fit;
            FitFont(lbl, maxPt, minPt);
        }

        private static void FitFont(Label lbl, float maxPt, float minPt)
        {
            if (lbl == null || lbl.IsDisposed || !lbl.IsHandleCreated) return;
            int w = lbl.ClientSize.Width - lbl.Padding.Horizontal;
            int h = lbl.ClientSize.Height - lbl.Padding.Vertical;
            if (w <= 2 || h <= 2) return;

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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timer.Stop();
            _timer.Dispose();
            base.OnFormClosed(e);
        }
    }
}
