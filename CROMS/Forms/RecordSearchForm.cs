using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Record Search — THE one place to find a civil registry record, whichever register it
    /// is in. Matches by name (LIKE) and, when sound-alike matching is on, also by MySQL
    /// SOUNDEX so spelling variants match (e.g. "Dela Cruz" finds "dela cruz" / "de la Cruz").
    /// Results from births, marriages and deaths UNION into one grid.
    ///
    /// REGISTRY BOOK INFORMATION IS PART OF THE RESULT, not a separate screen. The office's
    /// paper registry book is identified by its volume and the page a record was written on
    /// (the book_volume / book_page columns that Birth, Marriage and Death Registration let
    /// staff type); those now travel with every search hit, and the rail on the right states
    /// the record's full registry identity — registry number, registry year, book, page, date
    /// of registration, which register it is in and which Municipal Form revision it came off.
    /// This is what the standalone Registry Books screen used to be reached for; that screen's
    /// data and its book/page columns are untouched, it simply is no longer a separate stop.
    ///
    /// Read-only throughout: SELECT statements only. Double-clicking a row — or pressing the
    /// rail's button — jumps to that record's own registration module.
    ///
    /// UI is declared in RecordSearchForm.Designer.cs so every control is visible/editable on
    /// the design canvas; this file holds the data access, the badge painting and the rail.
    /// </summary>
    public partial class RecordSearchForm : Form, IRefreshable
    {
        // Suppresses Search() while the designer-wired handlers fire during InitializeComponent
        // (setting cboType default / chkFuzzy checked would otherwise query before we're ready).
        private bool _ready;

        public RecordSearchForm()
        {
            InitializeComponent();
            cboType.SelectedIndex = 0;
            _ready = true;
            Search();
        }

        public void RefreshData() => Search();

        // ------------------------------------------------------------ event handlers
        private void txtQuery_TextChanged(object sender, EventArgs e) => Search();
        private void cboType_SelectedIndexChanged(object sender, EventArgs e) => Search();
        private void chkFuzzy_CheckedChanged(object sender, EventArgs e) => Search();
        private void grid_SelectionChanged(object sender, EventArgs e) => ShowDetail();

        // ---------------------------------------------------------------- data
        private void Search()
        {
            if (!_ready) return;
            string term = txtQuery.Text.Trim();
            bool fuzzy = chkFuzzy.Checked && term.Length > 0;
            string type = cboType.SelectedItem?.ToString() ?? "All Records";

            var parts = new List<string>();
            if (type == "All Records" || type == "Birth") parts.Add(BirthQuery(term, fuzzy));
            if (type == "All Records" || type == "Marriage") parts.Add(MarriageQuery(term, fuzzy));
            if (type == "All Records" || type == "Death") parts.Add(DeathQuery(term, fuzzy));

            string sql = string.Join(" UNION ALL ", parts) + " ORDER BY Name LIMIT 300";

            var ps = new List<MySqlParameter>();
            if (term.Length > 0)
            {
                ps.Add(new MySqlParameter("@like", "%" + term + "%"));
                ps.Add(new MySqlParameter("@q", term));
            }

            try
            {
                DataTable dt = ps.Count == 0 ? Db.Pull(sql) : Db.Pull(sql, ps.ToArray());
                grid.DataSource = dt;
                HideWorkingColumns();
                lblCount.Text = dt.Rows.Count + " record(s) found" +
                    (type == "All Records" ? "" : "  ·  " + type + " only") +
                    (term.Length == 0 ? "  ·  showing all — type a name to search" :
                     fuzzy ? "  ·  name + sound-alike match" : "  ·  exact name match");
                ShowDetail();
            }
            catch (Exception ex)
            {
                lblCount.Text = "Search failed: " + ex.Message;
            }
        }

        /// <summary>
        /// Columns the grid carries for the rail but does not show. `id` identifies the row;
        /// the rest are registry facts the rail states in full and the grid has no room for.
        /// </summary>
        private static readonly string[] WorkingColumns =
            { "id", "RegYear", "DateReg", "FormName", "FormCode" };

        /// <summary>
        /// Hides the rail-only columns and shares the grid width by how much each column has
        /// to say — with Fill mode's default equal weights a long name truncated to
        /// "ABAD, GEORGE D..." while Book and Page, which hold at most a few characters, each
        /// held the same width. Every value also gets its full text as a tooltip: a name the
        /// operator cannot finish reading cannot be checked against the certificate in front
        /// of them (same fix the OCR review grid needed on 2026-09-08).
        /// </summary>
        private void HideWorkingColumns()
        {
            foreach (string c in WorkingColumns)
                if (grid.Columns.Contains(c)) grid.Columns[c].Visible = false;

            SetWeight("Type", 9);
            SetWeight("Registry No", 14);
            SetWeight("Name", 30);
            SetWeight("Event Date", 13);
            SetWeight("Book", 8);
            SetWeight("Page", 7);
            SetWeight("Status", 19);

            foreach (DataGridViewRow row in grid.Rows)
                foreach (DataGridViewCell cell in row.Cells)
                    if (cell.OwningColumn.Visible)
                    {
                        object v = cell.Value;
                        cell.ToolTipText = v == null || v == DBNull.Value ? "" : v.ToString();
                    }
        }

        private void SetWeight(string column, int weight)
        {
            if (grid.Columns.Contains(column)) grid.Columns[column].FillWeight = weight;
        }

        // Each sub-query returns the SAME columns in the same order so they can be UNIONed:
        //   Type | id | Registry No | Name | Event Date | Book | Page | Status
        //   | RegYear | DateReg | FormName | FormCode
        private static string BirthQuery(string term, bool fuzzy)
        {
            string where = Where(term, fuzzy,
                "(first_name LIKE @like OR middle_name LIKE @like OR last_name LIKE @like OR " +
                "CONCAT(first_name,' ',last_name) LIKE @like)",
                "SOUNDEX(last_name) = SOUNDEX(@q) OR SOUNDEX(first_name) = SOUNDEX(@q)");
            return "SELECT 'Birth' AS Type, id, registry_no AS 'Registry No', " +
                   "TRIM(CONCAT(last_name, ', ', first_name, ' ', COALESCE(middle_name,''))) AS Name, " +
                   "date_of_birth AS 'Event Date', " +
                   "book_volume AS Book, book_page AS Page, status AS Status, " +
                   RegistryYear + ", date_registered AS DateReg, " +
                   "form_name AS FormName, form_code AS FormCode FROM births" + where;
        }

        private static string DeathQuery(string term, bool fuzzy)
        {
            string where = Where(term, fuzzy,
                "full_name LIKE @like",
                "SOUNDEX(full_name) = SOUNDEX(@q)");
            // `deaths` has no date_registered column — migrations 27 and 33 added one to births
            // and marriages only. It is reported as NOT RECORDED rather than substituted with
            // created_at, which for a digitized backlog record is the SCANNING date, not the
            // date the office registered the death.
            return "SELECT 'Death' AS Type, id, registry_no AS 'Registry No', " +
                   "full_name AS Name, date_of_death AS 'Event Date', " +
                   "book_volume AS Book, book_page AS Page, status AS Status, " +
                   RegistryYear + ", CAST(NULL AS DATE) AS DateReg, " +
                   "form_name AS FormName, form_code AS FormCode FROM deaths" + where;
        }

        private static string MarriageQuery(string term, bool fuzzy)
        {
            string where = Where(term, fuzzy,
                "(husband_first_name LIKE @like OR husband_last_name LIKE @like OR " +
                "wife_first_name LIKE @like OR wife_last_name LIKE @like)",
                "SOUNDEX(husband_last_name) = SOUNDEX(@q) OR SOUNDEX(wife_last_name) = SOUNDEX(@q)");
            return "SELECT 'Marriage' AS Type, id, registry_no AS 'Registry No', " +
                   "TRIM(CONCAT(husband_last_name, ', ', husband_first_name, '  &  ', " +
                   "wife_last_name, ', ', wife_first_name)) AS Name, " +
                   "date_of_marriage AS 'Event Date', " +
                   "book_volume AS Book, book_page AS Page, status AS Status, " +
                   RegistryYear + ", date_registered AS DateReg, " +
                   "form_name AS FormName, form_code AS FormCode FROM marriages" + where;
        }

        /// <summary>
        /// The registry YEAR, read off the record and never inferred from the event.
        ///
        /// The registry number itself carries it — the office numbers YYYY-NNNN ("2018-4555",
        /// "2026-B-0005") — and a book_volume the office typed as a bare year is the same year
        /// stated as the book. Those are the only two sources; there is deliberately no
        /// fall-back to YEAR(event date), because a DELAYED registration of a 1983 birth sits
        /// in the year it was registered, not the year it happened, so guessing from the event
        /// would put records in a book they are not in. A record stating neither returns NULL
        /// and the rail says "not recorded".
        ///
        /// The year must be a 19xx/20xx FOLLOWED BY A SEPARATOR, which is what makes this safe
        /// on the office's own legacy data: a first cut matched any leading four digits and
        /// reported the bare numbers already on file ("239103", "765432") as registry years
        /// 2391 and 7654 — a fabricated fact, and exactly the class of error this project keeps
        /// refusing. Those now correctly come back blank.
        /// </summary>
        private const string RegistryYear =
            "CASE WHEN registry_no REGEXP '^(19|20)[0-9]{2}[^0-9]' THEN LEFT(registry_no,4) " +
            "WHEN book_volume REGEXP '^(19|20)[0-9]{2}$' THEN book_volume END AS RegYear";

        /// <summary>Builds the WHERE clause: nothing when the term is blank (show all),
        /// LIKE match otherwise, plus the SOUNDEX clause when fuzzy is on.</summary>
        private static string Where(string term, bool fuzzy, string likeClause, string soundexClause)
        {
            if (term.Length == 0) return "";
            return " WHERE (" + likeClause + (fuzzy ? " OR " + soundexClause : "") + ")";
        }

        // ------------------------------------------------------- document-type badge
        /// <summary>
        /// Paints the document-type badge on every row. The Type cell carries the register's
        /// own colour — birth blue, marriage purple, death neutral grey, from MUi.RecordTone,
        /// which is the same palette the marriage windows already state their statuses in.
        ///
        /// COLOUR IS NEVER THE ONLY SIGNAL: the cell still reads "Birth" / "Marriage" /
        /// "Death" in words, so the badge works in greyscale, on a projector and for a
        /// colour-blind operator. A blank book or page prints an em dash rather than an empty
        /// cell, so "not recorded" cannot be mistaken for a rendering fault.
        /// </summary>
        private void grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var g = sender as DataGridView;
            if (g == null || e.RowIndex < 0 || e.RowIndex >= g.Rows.Count) return;
            string column = g.Columns[e.ColumnIndex].Name;

            if (column == "Type")
            {
                string type = g.Rows[e.RowIndex].Cells["Type"].Value as string;
                Color tint, ink;
                MUi.RecordTone(type, out tint, out ink);
                e.CellStyle.BackColor = tint;
                e.CellStyle.ForeColor = ink;
                e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Bold);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                // Keep the badge legible while the row is selected: the grid's own selection
                // tint would otherwise repaint it as every other cell.
                e.CellStyle.SelectionBackColor = UiTheme.Mix(tint, ink, 0.18f);
                e.CellStyle.SelectionForeColor = ink;
            }
            else if ((column == "Book" || column == "Page" || column == "Registry No") &&
                     (e.Value == null || e.Value == DBNull.Value || e.Value.ToString().Trim().Length == 0))
            {
                e.Value = "—";
                e.CellStyle.ForeColor = UiTheme.Faint;
                e.FormattingApplied = true;
            }
        }

        // --------------------------------------------------------------- detail rail
        /// <summary>
        /// Rebuilds the rail for the selected row: the document-type badge, the name, and the
        /// record's full registry-book identity. Every value the record does not carry says so
        /// in words — nothing here is derived or filled in on the record's behalf.
        /// </summary>
        private void ShowDetail()
        {
            if (!_ready) return;
            cardDetail.SuspendLayout();
            foreach (Control c in ToArray(cardDetail.Controls))
            {
                cardDetail.Controls.Remove(c);
                c.Dispose();
            }

            DataGridViewRow row = SelectedRow();
            if (row == null)
            {
                Stack(cardDetail,
                    MUi.SectionHeader("No record selected",
                        "Click a result on the left to see its registry book details here."));
                cardDetail.ResumeLayout(true);
                return;
            }

            string type = Cell(row, "Type");
            string name = Cell(row, "Name");
            string book = Cell(row, "Book");
            string page = Cell(row, "Page");

            var badgeHost = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.Transparent, Margin = new Padding(0) };
            StatusPill badge = MUi.RecordPill(type);
            badge.Location = new Point(0, 0);
            badgeHost.Controls.Add(badge);

            var nameLabel = new Label
            {
                Text = name.Length == 0 ? "(unnamed record)" : name,
                Dock = DockStyle.Top, Height = 46, AutoSize = false, UseMnemonic = false,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UiTheme.Ink,
                BackColor = Color.Transparent, Margin = new Padding(0, 6, 0, 4)
            };

            var rows = new List<Control>
            {
                badgeHost,
                nameLabel,
                MUi.Cap("Registry entry"),
                MUi.Kv("Register", type.Length == 0 ? NotRecorded : type + " register"),
                MUi.Kv("Registry no.", Or(Cell(row, "Registry No"))),
                MUi.Kv("Registry year", Or(Cell(row, "RegYear"))),
                MUi.Kv("Date registered", DateOr(row, "DateReg", type)),
                MUi.Kv(EventLabel(type), DateOr(row, "Event Date", null)),
                MUi.Kv("Status", Or(Cell(row, "Status")), MUi.InkOf(Cell(row, "Status"))),
                MUi.Cap("Registry book"),
                MUi.Kv("Book / volume", Or(book)),
                MUi.Kv("Page", Or(page)),
                MUi.Cap("Source form"),
                MUi.Kv("Form", Or(Cell(row, "FormName"))),
                MUi.Kv("Form code", Or(Cell(row, "FormCode")))
            };

            if (book.Length == 0 || page.Length == 0)
                rows.Add(Note("The book and page are typed on " + type + " Registration, on the " +
                              "record itself — they are blank here because the paper book entry " +
                              "has not been recorded for this record yet."));

            Button open = MUi.Btn("Open in " + type + " Registration", MUi.Kind.Primary);
            open.Dock = DockStyle.Top;
            open.Margin = new Padding(0, 14, 0, 0);
            open.Click += (s, e) => OpenSelected();
            rows.Add(open);

            Stack(cardDetail, rows.ToArray());
            cardDetail.ResumeLayout(true);
        }

        /// <summary>
        /// The row the rail and the jump both act on. The grid is FullRowSelect, so
        /// SelectedRows is the authoritative answer and CurrentRow is only a fallback:
        /// CurrentRow lags a programmatic selection change by an event, which had the rail
        /// showing a DIFFERENT record from the highlighted row — caught by driving the real
        /// grid, and the exact class of mismatch that would let an operator read one
        /// certificate's registry book while looking at another's name.
        /// </summary>
        private DataGridViewRow SelectedRow()
        {
            if (grid.SelectedRows.Count > 0 && !grid.SelectedRows[0].IsNewRow)
                return grid.SelectedRows[0];
            DataGridViewRow cur = grid.CurrentRow;
            return cur != null && !cur.IsNewRow ? cur : null;
        }

        private const string NotRecorded = "not recorded";

        private static string Or(string value) => value.Length == 0 ? NotRecorded : value;

        private static Control[] ToArray(Control.ControlCollection cc)
        {
            var list = new Control[cc.Count];
            cc.CopyTo(list, 0);
            return list;
        }

        /// <summary>
        /// Adds controls in VISUAL order. Dock=Top lays out in reverse z-order, so the list is
        /// added last-to-first and each control's TabIndex is stated explicitly — leaving tab
        /// order to fall out of the Add order is what opened two earlier screens in this app
        /// scrolled to their own bottom control.
        ///
        /// Dock is forced here rather than assumed: MUi.Cap / MUi.Txt are AUTOSIZE labels built
        /// for the marriage windows' flow layouts and carry no Dock, so added as-is they all sat
        /// at (0,0) on top of each other — the whole rail rendered as one pile. Found by a
        /// sibling-overlap sweep over the real form, not by compiling.
        /// </summary>
        private static void Stack(Control host, params Control[] visualOrder)
        {
            for (int i = visualOrder.Length - 1; i >= 0; i--)
            {
                Control c = visualOrder[i];
                c.TabIndex = i;
                if (c.AutoSize && c is Label) { c.AutoSize = false; c.Height = 20; }
                c.Dock = DockStyle.Top;
                host.Controls.Add(c);
            }
        }

        private static Control Note(string text)
        {
            return new Label
            {
                Text = text, Dock = DockStyle.Top, Height = 62, AutoSize = false, UseMnemonic = false,
                Font = new Font("Segoe UI", 8.5F), ForeColor = UiTheme.Muted,
                BackColor = Color.Transparent, Margin = new Padding(0, 10, 0, 0)
            };
        }

        private static string EventLabel(string type)
        {
            switch (type)
            {
                case "Birth": return "Date of birth";
                case "Marriage": return "Date of marriage";
                case "Death": return "Date of death";
                default: return "Event date";
            }
        }

        private static string Cell(DataGridViewRow row, string column)
        {
            if (!row.DataGridView.Columns.Contains(column)) return "";
            object v = row.Cells[column].Value;
            return v == null || v == DBNull.Value ? "" : v.ToString().Trim();
        }

        /// <summary>
        /// A date column, formatted from its own typed value. Never re-parsed from a
        /// culture-formatted string — that is what silently transposed day and month on the
        /// certifications (2026-09-16), and a wrong date on a registry record is not
        /// self-evident the way a blank one is.
        /// </summary>
        private static string DateOr(DataGridViewRow row, string column, string typeForDeathNote)
        {
            if (!row.DataGridView.Columns.Contains(column)) return NotRecorded;
            object v = row.Cells[column].Value;
            if (v is DateTime dt) return dt.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
            if (v == null || v == DBNull.Value)
                return typeForDeathNote == "Death" ? "not kept for deaths" : NotRecorded;
            string s = v.ToString().Trim();
            return s.Length == 0 ? NotRecorded : s;
        }

        // --------------------------------------------------------------- jump
        private void grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            OpenSelected();
        }

        /// <summary>Opens the selected record's own registration module and names the row to find.</summary>
        private void OpenSelected()
        {
            DataGridViewRow row = SelectedRow();
            if (row == null) return;

            string type = Cell(row, "Type");
            string name = Cell(row, "Name");
            string reg = Cell(row, "Registry No");
            if (reg.Length == 0) reg = "(unnumbered)";
            string book = Cell(row, "Book");
            string page = Cell(row, "Page");

            string key;
            switch (type)
            {
                case "Birth": key = "birth"; break;
                case "Marriage": key = "marriage"; break;
                case "Death": key = "death"; break;
                default: return;
            }

            MainForm shell = Shell();
            if (shell == null) return;
            shell.GoToModule(key);
            MessageBox.Show(
                "Opened " + type + " Registration.\nFind this record in the list:\n\n" +
                reg + "  —  " + name +
                "\nBook " + (book.Length == 0 ? NotRecorded : book) +
                ", page " + (page.Length == 0 ? NotRecorded : page),
                "Go to record", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>Walks up the control tree to the application shell (MainForm).</summary>
        private MainForm Shell()
        {
            for (Control p = Parent; p != null; p = p.Parent)
                if (p is MainForm mf) return mf;
            return null;
        }
    }
}
