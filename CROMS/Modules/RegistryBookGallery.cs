using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CROMS.Data;

namespace CROMS.Modules
{
    /// <summary>
    /// Step 1 of the Civil Registry Record digitization workflow — the "Registry Books"
    /// gallery a Birth/Marriage/Death Record screen opens on: rectangular book cards laid out
    /// side by side (File-Explorer style), grouped by <c>book_volume</c> exactly the way
    /// RegistryBooksForm already groups the live registries (2026-09-15). Covers EVERY saved
    /// record — registered at the counter and digitized from old books alike — so the
    /// archive shows what the office actually holds (it used to be scoped to
    /// <c>record_source = 'OCR-Backlog'</c> and came up empty on a database of registrations).
    /// <para/>
    /// One reusable control instead of three near-identical copies, so Birth/Marriage/Death
    /// Record share the same design and workflow while each still queries its own table.
    /// </summary>
    public class RegistryBookGallery : Panel
    {
        public class BookInfo
        {
            public string VolRaw;
            public string VolDisplay;
            public int Records;
            public int Pages;
        }

        /// <summary>Raised when a book card is clicked — Step 2, open that book.</summary>
        public event Action<BookInfo> BookOpened;

        /// <summary>Raised by the gallery's own "+ Digitize Old Record" button — Step 3.
        /// Deliberately the ONLY digitize entry point this control offers, kept on the
        /// gallery rather than inside a book.</summary>
        public event Action DigitizeRequested;

        private readonly string _table;
        private FlowLayoutPanel _flow;
        private TextBox _txtSearch;
        private ComboBox _cboYear;
        private Label _lblCount;
        private List<BookInfo> _all = new List<BookInfo>();

        public RegistryBookGallery(string table, string recordTypeLabel)
        {
            _table = table;
            Dock = DockStyle.Fill;
            BuildUi(recordTypeLabel);
        }

        private void BuildUi(string recordTypeLabel)
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var toolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 0, 0, 8)
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            root.Controls.Add(toolbar, 0, 0);

            var filters = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            var lblSearch = new Label { Text = "Search:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) };
            _txtSearch = new TextBox { Width = 220, Margin = new Padding(0, 4, 18, 4) };
            _txtSearch.TextChanged += (s, e) => ApplyFilter();
            var lblYear = new Label { Text = "Year:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) };
            _cboYear = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110, Margin = new Padding(0, 4, 0, 4) };
            _cboYear.SelectedIndexChanged += (s, e) => ApplyFilter();
            filters.Controls.Add(lblSearch);
            filters.Controls.Add(_txtSearch);
            filters.Controls.Add(lblYear);
            filters.Controls.Add(_cboYear);
            toolbar.Controls.Add(filters, 0, 0);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            var btnDigitize = new Button { Text = "+ Digitize Old Record", Width = 190, Height = 32 };
            btnDigitize.Click += (s, e) => DigitizeRequested?.Invoke();
            var btnRefresh = new Button { Text = "Refresh", Width = 90, Height = 32 };
            btnRefresh.Click += (s, e) => Reload();
            actions.Controls.Add(btnDigitize);
            actions.Controls.Add(btnRefresh);
            toolbar.Controls.Add(actions, 1, 0);

            _lblCount = new Label
            {
                Dock = DockStyle.Top, Text = "", ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI", 9F), AutoSize = true, Margin = new Padding(0, 0, 0, 10)
            };
            root.Controls.Add(_lblCount, 0, 1);

            var scroller = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            _flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = true
            };
            scroller.Controls.Add(_flow);
            root.Controls.Add(scroller, 0, 2);
        }

        public void Reload()
        {
            string sql =
                "SELECT COALESCE(book_volume,'(no volume recorded)') AS VolDisplay, " +
                "book_volume AS VolRaw, COUNT(*) AS Records, " +
                "COUNT(DISTINCT CASE WHEN book_page IS NOT NULL AND book_page<>'' THEN book_page END) AS Pages " +
                "FROM " + _table + " GROUP BY book_volume ORDER BY book_volume DESC";

            _all = new List<BookInfo>();
            try
            {
                DataTable dt = Db.Pull(sql);
                foreach (DataRow r in dt.Rows)
                {
                    _all.Add(new BookInfo
                    {
                        VolRaw = r["VolRaw"] == DBNull.Value ? null : r["VolRaw"].ToString(),
                        VolDisplay = r["VolDisplay"].ToString(),
                        Records = Convert.ToInt32(r["Records"]),
                        Pages = Convert.ToInt32(r["Pages"])
                    });
                }
            }
            catch
            {
                // A load failure leaves the gallery empty rather than throwing out of a
                // module the operator is just trying to open.
            }

            RebuildYearFilter();
            ApplyFilter();
        }

        private void RebuildYearFilter()
        {
            string prev = _cboYear.SelectedItem as string;
            _cboYear.Items.Clear();
            _cboYear.Items.Add("All Years");
            var years = new SortedSet<string>(StringComparer.Ordinal);
            foreach (BookInfo b in _all)
            {
                Match m = Regex.Match(b.VolDisplay ?? "", @"(19|20)\d{2}");
                if (m.Success) years.Add(m.Value);
            }
            foreach (string y in years) _cboYear.Items.Add(y);
            _cboYear.SelectedItem = prev != null && _cboYear.Items.Contains(prev) ? prev : "All Years";
        }

        private void ApplyFilter()
        {
            string term = (_txtSearch.Text ?? "").Trim();
            string year = _cboYear.SelectedItem as string;

            List<BookInfo> filtered = _all.Where(b =>
                (term.Length == 0 || (b.VolDisplay ?? "").IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) &&
                (string.IsNullOrEmpty(year) || year == "All Years" ||
                 (b.VolDisplay ?? "").IndexOf(year, StringComparison.Ordinal) >= 0)
            ).ToList();

            _flow.SuspendLayout();
            foreach (Control c in _flow.Controls) c.Dispose();
            _flow.Controls.Clear();
            foreach (BookInfo b in filtered) _flow.Controls.Add(BuildCard(b));
            _flow.ResumeLayout();

            int totalRecords = _all.Sum(b => b.Records);
            _lblCount.Text = filtered.Count + (filtered.Count == 1 ? " book" : " books") +
                (filtered.Count != _all.Count ? " (of " + _all.Count + ")" : "") +
                "  ·  " + totalRecords + (totalRecords == 1 ? " record" : " records") + " total";
        }

        private Panel BuildCard(BookInfo b)
        {
            var card = new CardPanel
            {
                Size = new Size(212, 132),
                Margin = new Padding(10),
                Radius = 12,
                Cursor = Cursors.Hand,
                CardColor = UiTheme.Surface
            };

            var icon = new IconPanel
            {
                Size = new Size(32, 32),
                Location = new Point(16, 14),
                Drawer = NavIcons.For("books"),
                IconColor = UiTheme.Accent
            };
            var kicker = new Label
            {
                Text = "REGISTRY BOOK", Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = UiTheme.Muted, AutoSize = true, Location = new Point(16, 56),
                BackColor = Color.Transparent
            };
            var vol = new Label
            {
                Text = b.VolDisplay, Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = UiTheme.Ink, AutoSize = false, Size = new Size(182, 24),
                Location = new Point(16, 72), AutoEllipsis = true, BackColor = Color.Transparent
            };
            var count = new Label
            {
                Text = b.Records + (b.Records == 1 ? " Record" : " Records") +
                       (b.Pages > 0 ? "  ·  " + b.Pages + (b.Pages == 1 ? " Page" : " Pages") : ""),
                Font = new Font("Segoe UI", 8.5F), ForeColor = UiTheme.Muted,
                AutoSize = true, Location = new Point(16, 100), BackColor = Color.Transparent
            };

            card.Controls.Add(icon);
            card.Controls.Add(kicker);
            card.Controls.Add(vol);
            card.Controls.Add(count);

            EventHandler click = (s, e) => BookOpened?.Invoke(b);
            card.Click += click;
            icon.Click += click;
            kicker.Click += click;
            vol.Click += click;
            count.Click += click;

            HoverFade.Attach(card, 150, t =>
            {
                card.CardColor = UiTheme.Mix(UiTheme.Surface, UiTheme.AccentTint, t);
                card.Invalidate();
            });

            return card;
        }

        /// <summary>A small transparent-backed panel that owner-draws one glyph from an icon
        /// delegate — the same monochrome-line-art convention NavIcons already uses on the
        /// sidebar, reused here so a book card carries a real icon rather than an emoji.</summary>
        private class IconPanel : Panel
        {
            public Action<Graphics, RectangleF, Color> Drawer;
            public Color IconColor = Color.Black;

            public IconPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
                BackColor = Color.Transparent;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Drawer?.Invoke(e.Graphics, new RectangleF(0, 0, Width, Height), IconColor);
            }
        }
    }
}
