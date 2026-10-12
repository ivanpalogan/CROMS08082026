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
    /// Records Archive — one screen that both SEARCHES for a record (by name, with
    /// SOUNDEX sound-alike matching, across births/marriages/deaths — this absorbed
    /// the standalone Record Search module on 2026-09-28) and browses every other
    /// record, form and image the system has ever saved, grouped by type in the left
    /// tree: marriage licenses, every petition type
    /// (RA9048/RA10172/Legitimation/SupplementalReport/LegalInstrument/CourtOrder),
    /// certificate requests, BREQS, claim requests, releases, and queue tickets.
    /// Read-only throughout for every one of THOSE categories — this screen never
    /// writes to the database on their behalf. Selecting a category lists its rows;
    /// "View Full Record" (or double-click) shows every column plus any stored
    /// scan/photo, opened through the existing SoftcopyViewer. Selecting "Search
    /// Records" instead shows a name search bar and a registry-book detail rail for
    /// the highlighted hit; double-click (or the button) jumps to that record's own
    /// registration module.
    ///
    /// The "Civil Registry Records" group is the one exception: "Birth Record" /
    /// "Marriage Record" / "Death Record" (renamed 2026-09-28 from the generic
    /// read-only "Birth/Marriage/Death Registration" browse categories) each embed a
    /// full Add/Edit/View/Delete digitization workbench in place of the generic
    /// read-only grid — see ArchiveCategory.Workbench / EnterWorkbenchMode.
    ///
    /// In MainForm.OperationalKeys, so every operational role sees this module — the
    /// search bar is now the system's only record-search screen and is used daily.
    /// </summary>
    public partial class RecordsArchiveForm : Form, IRefreshable
    {
        private class ArchiveCategory
        {
            public string Group;
            public string Label;
            public string Sql;
            public string DetailTable;
            public (string Column, string Label)[] Images;
            public DocKind? CertKind;
            public bool IsMf90;
            /// <summary>owner_type value in document_requirements for this record's row id —
            /// "License"/"Marriage"/"Birth"/"Petition" — or null when this record type has no
            /// requirements checklist. Drives the read-only requirements panel in ShowDetail.</summary>
            public string ReqOwnerType;
            /// <summary>True only for the synthetic "Search Records" node — routes LoadCategory
            /// into DoSearch() instead of the normal single-table SELECT.</summary>
            public bool IsSearch;
            /// <summary>Non-null only for a "Birth Record" / "Marriage Record" / "Death Record"
            /// workbench category — a full Add/Edit/View/Delete screen (the retired standalone
            /// "Old Birth/Marriage/Death Records (OCR)" modules) embedded here instead of the
            /// generic read-only grid. LoadCategory routes into EnterWorkbenchMode() instead of
            /// running Sql.</summary>
            public Func<Form> Workbench;
        }

        private readonly List<ArchiveCategory> _categories = new List<ArchiveCategory>();
        private readonly ArchiveCategory _searchCategory = new ArchiveCategory { Label = "Search Records", IsSearch = true };
        private readonly Dictionary<ArchiveCategory, TreeNode> _categoryNodes = new Dictionary<ArchiveCategory, TreeNode>();
        // Cached like MainForm._cache caches a module: a workbench form is built once per
        // ArchiveCategory and reused, so New/Edit/Delete state and list position survive
        // switching to another tree category and back.
        private readonly Dictionary<ArchiveCategory, Form> _workbenches = new Dictionary<ArchiveCategory, Form>();
        private ArchiveCategory _current;

        // Suppresses DoSearch() while the designer-wired handlers fire during
        // InitializeComponent (cboSearchType.SelectedIndex — set in the ctor below —
        // would otherwise query before the grid/tree exist).
        private bool _ready;

        // Full-width grid bounds captured once at design size, so both search mode and
        // non-search categories know where the grid STARTS (its left/top offset never
        // changes). The grid's actual WIDTH/HEIGHT are recomputed from the form's real,
        // current ClientSize on every layout — not from this frozen rectangle — because
        // this form is embedded into MainForm's content panel via Dock=Fill AFTER the
        // constructor runs, so ClientSize here is still the small design size until then.
        private Rectangle _gridFullBounds;

        // Constant margins (design-time gaps between the grid and the form's own right/
        // bottom edge, e.g. room left for btnViewRecord below the grid) — captured once
        // so every later layout can re-derive full-window bounds from whatever the real
        // ClientSize is, instead of re-deriving it from ClientSize itself (which is what
        // silently cancelled out to a fixed size on every earlier layout attempt here).
        private int _bottomReserve;
        private int _rightReserve;

        // Search mode: the grid runs to this far above the bottom edge, and the two record
        // buttons sit at the foot of the detail rail, beside the record they act on. In every
        // other category they go back under the grid (home bounds captured at design size).
        private const int SearchBottomMargin = 16;
        private Rectangle _recordButtonHome;
        private Rectangle _certButtonHome;
        private int _buttonBottomOffset;
        private Panel _railBody;
        private Panel _railActions;
        private Panel _railGap;

        public RecordsArchiveForm()
        {
            InitializeComponent();
            BuildCategories();
            SetupTreeStyle();
            WidenTreeToFit();   // before the grid bounds are captured: it moves the grid's left edge
            _gridFullBounds = grid.Bounds;
            _bottomReserve = ClientSize.Height - _gridFullBounds.Bottom;
            _rightReserve = ClientSize.Width - _gridFullBounds.Right;
            _recordButtonHome = btnViewRecord.Bounds;
            _certButtonHome = btnViewCertificate.Bounds;
            _buttonBottomOffset = ClientSize.Height - btnViewRecord.Top;
            SetupRail();
            SearchHost.SetCue(txtQuery, "Search by name");
            new ToolTip().SetToolTip(chkFuzzy, "Also finds names that sound the same but are spelled differently.");
            grid.DataBindingComplete += (s, e) => MarkQueryOrder();
            grid.CellPainting += grid_CellPainting;
            cboSearchType.SelectedIndex = 0;
            _ready = true;
            BuildTree();
            Resize += (s, e) => ApplyBounds();
        }

        /// <summary>Re-lays the grid (and, in search mode, cardDetail) to fill whatever
        /// space this form actually has right now — called on every category change AND
        /// on every resize, so the screen always uses the full window instead of the
        /// small design-time rectangle it starts life at before MainForm docks it.</summary>
        private void ApplyBounds()
        {
            if (_current == null) return;
            int bottom = ClientSize.Height - _bottomReserve;
            // The tree runs to the same bottom edge as the search grid and the rail.
            treeCategories.Height = Math.Max(120, ClientSize.Height - SearchBottomMargin - treeCategories.Top);

            if (_current.IsSearch)
            {
                // The two record buttons live in the rail in this mode, so the strip that
                // is reserved for them under the grid goes back to the grid.
                bottom = ClientSize.Height - SearchBottomMargin;
                int top = pnlSearchBar.Bottom + 12;
                // On a narrow window the rail gives some width back to the list.
                int detailWidth = ClientSize.Width < 1300 ? 280 : 330;
                const int gap = 12;
                int height = Math.Max(120, bottom - top);
                int rightEdge = ClientSize.Width - _rightReserve;
                int gridWidth = Math.Max(200, rightEdge - detailWidth - gap - _gridFullBounds.X);
                grid.Bounds = new Rectangle(_gridFullBounds.X, top, gridWidth, height);
                cardDetail.Bounds = new Rectangle(grid.Right + gap, top, detailWidth, height);
            }
            else
            {
                int top = _gridFullBounds.Y;
                int height = Math.Max(120, bottom - top);
                int gridWidth = Math.Max(200, ClientSize.Width - _rightReserve - _gridFullBounds.X);
                var rect = new Rectangle(_gridFullBounds.X, top, gridWidth, height);
                grid.Bounds = rect;
                pnlWorkbench.Bounds = rect;
            }
        }

        public void RefreshData()
        {
            if (_current != null) LoadCategory(_current);
        }

        // ------------------------------------------------------------ categories
        private void BuildCategories()
        {
            // Civil Registry Records — the office's own tree naming for these three (renamed
            // 2026-09-28 from the generic read-only "Birth/Marriage/Death Registration" browse
            // categories). Each is now the SAME full Add/Edit/View/Delete digitization workbench
            // "Legacy Digitized Records" used to host under a duplicate label — merged here so
            // there is one tree entry per record type, not two with the same name in different
            // groups. See ArchiveCategory.Workbench / EnterWorkbenchMode.
            _categories.Add(new ArchiveCategory { Group = "Civil Registry Records", Label = "Birth Record", Workbench = () => new OldBirthRecordsForm() });
            _categories.Add(new ArchiveCategory { Group = "Civil Registry Records", Label = "Marriage Record", Workbench = () => new OldMarriageRecordsForm() });
            _categories.Add(new ArchiveCategory { Group = "Civil Registry Records", Label = "Death Record", Workbench = () => new OldDeathRecordsForm() });
            _categories.Add(new ArchiveCategory
            {
                Group = "Marriage Licensing",
                Label = "Marriage License Applications (Form 90)",
                DetailTable = "marriage_licenses",
                Sql = "SELECT id, license_no AS 'License No', husband_name AS Husband, wife_name AS Wife, " +
                      "filed_date AS 'Filed Date', posting_ends AS 'Posting Ends', status AS Status, " +
                      "created_at AS Recorded FROM marriage_licenses ORDER BY created_at DESC",
                Images = new (string, string)[0],
                IsMf90 = true,
                ReqOwnerType = "License"
            });

            AddPetitionCategory("RA9048", "Correction of Entry (RA 9048)");
            AddPetitionCategory("RA10172", "Change of First Name (RA 10172)");
            AddPetitionCategory("Legitimation", "Legitimation (RA 9858)");
            AddPetitionCategory("SupplementalReport", "Supplemental Report");
            AddPetitionCategory("LegalInstrument", "Legal Instrument (RA 9255)");
            AddPetitionCategory("CourtOrder", "Court Order");

            _categories.Add(new ArchiveCategory
            {
                Group = "Certification & PSA Copies",
                Label = "Certificate Requests (CTC / Negative)",
                DetailTable = "certificate_requests",
                Sql = "SELECT id, record_type AS Type, cert_type AS 'Cert Type', copies AS Copies, " +
                      "purpose AS Purpose, status AS Status, created_at AS Recorded " +
                      "FROM certificate_requests ORDER BY created_at DESC",
                Images = new (string, string)[0]
            });
            _categories.Add(new ArchiveCategory
            {
                Group = "Certification & PSA Copies",
                Label = "PSA Copies (BREQS)",
                DetailTable = "psa_copy_requests",
                Sql = "SELECT id, request_no AS 'Request No', doc_type AS 'Doc Type', " +
                      "TRIM(CONCAT(COALESCE(requester_last,''),', ',COALESCE(requester_first,''))) AS Requester, " +
                      "TRIM(CONCAT(COALESCE(owner_last,''),', ',COALESCE(owner_first,''))) AS 'For (Owner)', " +
                      "status AS Status, created_at AS Recorded FROM psa_copy_requests ORDER BY created_at DESC",
                Images = new[] { ("scan_image", "PSA Copy Scan") }
            });

            _categories.Add(new ArchiveCategory
            {
                Group = "Claims & Releases",
                Label = "Claim Requests (ID Uploads)",
                DetailTable = "claimant_id_uploads",
                Sql = "SELECT id, claim_ticket_no AS 'Claim No', " +
                      "TRIM(CONCAT(COALESCE(last_name,''),', ',COALESCE(first_name,''))) AS Requester, " +
                      "request_details AS Details, status AS Status, created_at AS Recorded " +
                      "FROM claimant_id_uploads ORDER BY created_at DESC",
                Images = new[] { ("id_image", "Uploaded Valid ID") }
            });
            _categories.Add(new ArchiveCategory
            {
                Group = "Claims & Releases",
                Label = "Releases (Claimant Photos)",
                DetailTable = "releases",
                Sql = "SELECT id, transaction_id AS 'Txn ID', claimant_name AS Claimant, " +
                      "CASE is_representative WHEN 1 THEN 'Representative' ELSE 'Owner' END AS 'Claimed By', " +
                      "representative_id_type AS 'Rep ID Type', released_at AS 'Released At' " +
                      "FROM releases ORDER BY released_at DESC",
                Images = new[] { ("claimant_photo", "Claimant Photo") }
            });

            _categories.Add(new ArchiveCategory
            {
                Group = "Front Desk",
                Label = "Queue Tickets",
                DetailTable = "queue_tickets",
                Sql = "SELECT id, ticket_code AS Ticket, full_name AS Name, " +
                      "COALESCE(type_label, document_type) AS Service, status AS Status, " +
                      "created_at AS Issued FROM queue_tickets ORDER BY created_at DESC",
                Images = new[] { ("id_image", "Face Photo (kiosk)"), ("spouse_image", "Spouse Photo (kiosk)") }
            });

        }

        private void AddPetitionCategory(string typeCode, string label)
        {
            _categories.Add(new ArchiveCategory
            {
                Group = "Petitions & Legal Instruments",
                Label = label,
                DetailTable = "petitions",
                Sql = "SELECT p.id, p.record_type AS Type, " +
                      "CASE p.record_type " +
                      "  WHEN 'Birth'    THEN (SELECT TRIM(CONCAT(last_name,', ',first_name)) FROM births b WHERE b.id = p.record_id) " +
                      "  WHEN 'Death'    THEN (SELECT full_name FROM deaths d WHERE d.id = p.record_id) " +
                      "  WHEN 'Marriage' THEN (SELECT TRIM(CONCAT(husband_last_name,' & ',wife_last_name)) FROM marriages m WHERE m.id = p.record_id) " +
                      "  ELSE '—' END AS Record, " +
                      "CASE p.stage WHEN 'UnderReview' THEN 'Under Review' WHEN 'PSA_Endorsement' THEN 'PSA Endorsement' ELSE p.stage END AS Stage, " +
                      "p.filed_date AS Filed, p.remarks AS Remarks, p.created_at AS Recorded " +
                      "FROM petitions p WHERE p.petition_type = '" + typeCode + "' ORDER BY p.created_at DESC",
                Images = new (string, string)[0],
                ReqOwnerType = "Petition"
            });
        }

        // ------------------------------------------------------------ tree
        private const int TreeGroupIndent = 26;   // text start of a group / the search row
        private const int TreeChildIndent = 34;   // text start of a category under a group
        private Font _treeBold;

        /// <summary>
        /// Draws the category tree flat: no dotted lines or plus/minus boxes, a chevron on each
        /// group, and the chosen category as a soft tinted row. A solid-blue selected node made
        /// the tree and the selected grid row both look like "the selection".
        /// </summary>
        private void SetupTreeStyle()
        {
            _treeBold = new Font(treeCategories.Font, FontStyle.Bold);
            treeCategories.ShowLines = false;
            treeCategories.ShowPlusMinus = false;
            treeCategories.ShowRootLines = false;
            treeCategories.BorderStyle = BorderStyle.None;
            treeCategories.BackColor = UiTheme.Surface;
            treeCategories.ItemHeight = 30;
            treeCategories.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            treeCategories.DrawNode += treeCategories_DrawNode;
            treeCategories.BeforeSelect += treeCategories_BeforeSelect;
            treeCategories.NodeMouseClick += treeCategories_NodeMouseClick;
            Disposed += (s, e) => _treeBold.Dispose();
        }

        /// <summary>Makes the tree as wide as its longest caption and moves everything beside it over.</summary>
        private void WidenTreeToFit()
        {
            const TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            int need = TreeGroupIndent + TextRenderer.MeasureText(_searchCategory.Label, _treeBold, Size.Empty, flags).Width;
            foreach (ArchiveCategory cat in _categories)
            {
                need = Math.Max(need, TreeGroupIndent + TextRenderer.MeasureText(cat.Group, _treeBold, Size.Empty, flags).Width);
                need = Math.Max(need, TreeChildIndent + TextRenderer.MeasureText(cat.Label, treeCategories.Font, Size.Empty, flags).Width);
            }
            need += 16 + SystemInformation.VerticalScrollBarWidth;

            int delta = Math.Min(need, 380) - treeCategories.Width;
            if (delta <= 0) return;
            treeCategories.Width += delta;
            foreach (Control c in new Control[] { lblCategoryTitle, pnlSearchBar, btnViewRecord, btnViewCertificate })
                c.Left += delta;
            grid.SetBounds(grid.Left + delta, grid.Top, grid.Width - delta, grid.Height);
            pnlWorkbench.SetBounds(pnlWorkbench.Left + delta, pnlWorkbench.Top, pnlWorkbench.Width - delta, pnlWorkbench.Height);
        }

        private void treeCategories_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Bounds.Height <= 0) return;
            Graphics g = e.Graphics;
            var row = new Rectangle(0, e.Bounds.Y, treeCategories.ClientSize.Width, e.Bounds.Height);
            using (var back = new SolidBrush(treeCategories.BackColor)) g.FillRectangle(back, row);

            bool isGroup = e.Node.Tag == null;
            bool isSearch = ReferenceEquals(e.Node.Tag, _searchCategory);
            bool selected = !isGroup && e.Node == treeCategories.SelectedNode;
            Color fill = treeCategories.BackColor;

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if (selected)
            {
                fill = UiTheme.AccentTint;
                var pill = new Rectangle(row.X + 6, row.Y + 2, row.Width - 12, row.Height - 4);
                using (var path = RoundedRow(pill, 6))
                using (var b = new SolidBrush(fill)) g.FillPath(b, path);
            }

            int cy = row.Y + row.Height / 2;
            Color ink = selected || isSearch ? UiTheme.Accent : isGroup ? UiTheme.Ink : UiTheme.Muted;
            if (isGroup)
            {
                using (var pen = new Pen(UiTheme.Muted, 1.6F))
                {
                    if (e.Node.IsExpanded) g.DrawLines(pen, new[] { new Point(10, cy - 2), new Point(14, cy + 2), new Point(18, cy - 2) });
                    else g.DrawLines(pen, new[] { new Point(12, cy - 4), new Point(16, cy), new Point(12, cy + 4) });
                }
            }
            else if (isSearch)
            {
                using (var pen = new Pen(UiTheme.Accent, 1.7F))
                {
                    g.DrawEllipse(pen, 12, cy - 6, 8, 8);
                    g.DrawLine(pen, 19, cy + 2, 22, cy + 5);
                }
            }

            int x = isGroup ? TreeGroupIndent : isSearch ? TreeGroupIndent + 4 : TreeChildIndent;
            // NoPrefix: two group names carry an ampersand, which would otherwise be eaten as a mnemonic.
            TextRenderer.DrawText(g, e.Node.Text, isGroup || isSearch ? _treeBold : treeCategories.Font,
                new Rectangle(x, row.Y, Math.Max(0, row.Width - x - 8), row.Height), ink, fill,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix |
                TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRow(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new System.Drawing.Drawing2D.GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>A group heading opens and closes; it is never the selected category.</summary>
        private void treeCategories_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node != null && e.Node.Tag == null && e.Action == TreeViewAction.ByMouse) e.Cancel = true;
        }

        private void treeCategories_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node == null || e.Node.Tag != null || e.Button != MouseButtons.Left) return;
            if (e.Node.IsExpanded) e.Node.Collapse(); else e.Node.Expand();
        }

        private void BuildTree()
        {
            treeCategories.Nodes.Clear();
            _categoryNodes.Clear();
            var searchNode = new TreeNode(_searchCategory.Label)
            {
                Tag = _searchCategory,
                ForeColor = UiTheme.Accent,
                NodeFont = new Font(treeCategories.Font, FontStyle.Bold)
            };
            treeCategories.Nodes.Add(searchNode);
            _categoryNodes[_searchCategory] = searchNode;

            var groups = new Dictionary<string, TreeNode>();
            foreach (var cat in _categories)
            {
                if (!groups.TryGetValue(cat.Group, out TreeNode groupNode))
                {
                    groupNode = new TreeNode(cat.Group) { ForeColor = Color.FromArgb(19, 36, 65) };
                    groupNode.NodeFont = new Font(treeCategories.Font, FontStyle.Bold);
                    treeCategories.Nodes.Add(groupNode);
                    groups[cat.Group] = groupNode;
                }
                var node = new TreeNode(cat.Label) { Tag = cat };
                groupNode.Nodes.Add(node);
                _categoryNodes[cat] = node;
            }
            treeCategories.ExpandAll();
            treeCategories.SelectedNode = searchNode;
        }

        private void treeCategories_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is ArchiveCategory cat) LoadCategory(cat);
        }

        // ------------------------------------------------------------ list
        private void LoadCategory(ArchiveCategory cat)
        {
            _current = cat;
            if (cat.IsSearch)
            {
                HideWorkbench();
                EnterSearchMode();
                DoSearch();
                return;
            }

            ExitSearchMode();

            if (cat.Workbench != null)
            {
                EnterWorkbenchMode(cat);
                return;
            }
            HideWorkbench();

            lblCategoryTitle.Text = cat.Label;
            try
            {
                DataTable dt = Db.Pull(cat.Sql);
                // Unbind first: the grid keeps a column whose name it already has (Name, Status,
                // Type) in its old place, which shuffled the column order between categories.
                grid.DataSource = null;
                grid.DataSource = dt;
                if (grid.Columns.Contains("id")) grid.Columns["id"].Visible = false;
                // The search list sets its own column weights; a column of the same name here
                // (Name, Status) kept them and came out too narrow to read.
                foreach (DataGridViewColumn col in grid.Columns) col.FillWeight = 100;
                lblCount.Text = CountText(dt.Rows.Count);
            }
            catch (Exception ex)
            {
                grid.DataSource = null;
                lblCount.Text = "Load failed: " + CROMS.Data.ErrorLog.Reason(ex);
            }
        }

        /// <summary>Hides the grid/count/View-Record button and shows the cached (or
        /// newly-built) workbench form embedded in `pnlWorkbench`, exactly the way MainForm
        /// embeds a module form — TopLevel=false, Dock=Fill, refreshed via IRefreshable every
        /// time it is shown again.</summary>
        private void EnterWorkbenchMode(ArchiveCategory cat)
        {
            lblCategoryTitle.Text = cat.Label;
            grid.Visible = false;
            btnViewRecord.Visible = false;
            btnViewCertificate.Visible = false;
            lblCount.Visible = false;
            btnRefresh.Visible = false;

            if (!_workbenches.TryGetValue(cat, out Form form))
            {
                form = cat.Workbench();
                form.TopLevel = false;
                form.FormBorderStyle = FormBorderStyle.None;
                form.Dock = DockStyle.Fill;
                form.AutoScroll = true;
                _workbenches[cat] = form;
                pnlWorkbench.Controls.Add(form);
                form.Show();
                // lblCategoryTitle above already reads "Birth Record"/etc — hide the workbench's
                // OWN duplicate title label the same way MainForm.ShowModule does for a module.
                MainForm.SuppressDuplicateModuleTitle(form, cat.Label);
                UiTheme.PolishButtons(form);
            }
            form.BringToFront();
            if (form is IRefreshable refreshable) refreshable.RefreshData();

            pnlWorkbench.Visible = true;
            ApplyBounds();
        }

        private void HideWorkbench()
        {
            if (!pnlWorkbench.Visible) return;
            pnlWorkbench.Visible = false;
            grid.Visible = true;
            btnViewRecord.Visible = true;
            lblCount.Visible = true;
            btnRefresh.Visible = true;
        }

        /// <summary>Cross-module hand-off for Intelligent Document Processing (the standalone
        /// "oldbirth" module key this used to reach was retired 2026-09-28): selects the
        /// "Birth Record" workbench in the tree, building it if this is the first visit, and
        /// returns it so the caller can land on the committed record.</summary>
        public OldBirthRecordsForm OpenBirthRecordWorkbench()
        {
            ArchiveCategory cat = _categories.Find(c => c.Workbench != null && c.Label == "Birth Record");
            if (cat == null) return null;
            if (_categoryNodes.TryGetValue(cat, out TreeNode node)) treeCategories.SelectedNode = node;
            else LoadCategory(cat);
            return _workbenches.TryGetValue(cat, out Form form) ? form as OldBirthRecordsForm : null;
        }

        /// <summary>Same hand-off as <see cref="OpenBirthRecordWorkbench"/>, for the Marriage
        /// Record workbench.</summary>
        public OldMarriageRecordsForm OpenMarriageRecordWorkbench()
        {
            ArchiveCategory cat = _categories.Find(c => c.Workbench != null && c.Label == "Marriage Record");
            if (cat == null) return null;
            if (_categoryNodes.TryGetValue(cat, out TreeNode node)) treeCategories.SelectedNode = node;
            else LoadCategory(cat);
            return _workbenches.TryGetValue(cat, out Form form) ? form as OldMarriageRecordsForm : null;
        }

        /// <summary>Same hand-off as <see cref="OpenBirthRecordWorkbench"/>, for the Death
        /// Record workbench.</summary>
        public OldDeathRecordsForm OpenDeathRecordWorkbench()
        {
            ArchiveCategory cat = _categories.Find(c => c.Workbench != null && c.Label == "Death Record");
            if (cat == null) return null;
            if (_categoryNodes.TryGetValue(cat, out TreeNode node)) treeCategories.SelectedNode = node;
            else LoadCategory(cat);
            return _workbenches.TryGetValue(cat, out Form form) ? form as OldDeathRecordsForm : null;
        }

        /// <summary>
        /// Certificate Request's "Find Record": opens the Birth / Marriage / Death Record
        /// section of this screen in pick mode — filtered to the client's own details (see
        /// <see cref="RecordCriteria"/>), with its own search bar over every book to widen it, plus a
        /// "Use This Record" button. The chosen record (id + a one-line label) goes to
        /// <paramref name="onPicked"/>; "← Cancel" calls <paramref name="onCancelled"/>.
        /// Returns false when the section could not be opened.
        /// </summary>
        public bool BeginRecordPick(string recordType, RecordCriteria criteria,
                                    Action<int, string> onPicked, Action onCancelled)
        {
            Form workbench;
            switch (recordType)
            {
                case "Birth": workbench = OpenBirthRecordWorkbench(); break;
                case "Marriage": workbench = OpenMarriageRecordWorkbench(); break;
                case "Death": workbench = OpenDeathRecordWorkbench(); break;
                default: return false;
            }

            if (workbench is OldBirthRecordsForm b) b.BeginPick(criteria, onPicked, onCancelled);
            else if (workbench is OldMarriageRecordsForm m) m.BeginPick(criteria, onPicked, onCancelled);
            else if (workbench is OldDeathRecordsForm d) d.BeginPick(criteria, onPicked, onCancelled);
            else return false;
            return true;
        }

        private void btnRefresh_Click(object sender, EventArgs e) => RefreshData();

        // ------------------------------------------------------------ detail
        private void grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_current != null && _current.IsSearch) OpenSearchRecord();
            else ViewSelected();
        }

        private void btnViewRecord_Click(object sender, EventArgs e)
        {
            if (_current != null && _current.IsSearch) OpenSearchRecord();
            else ViewSelected();
        }

        private void ViewSelected()
        {
            if (_current == null || grid.CurrentRow == null)
            {
                MessageBox.Show("Select a category, then click a row.", "Records Archive",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!grid.Columns.Contains("id")) return;
            object idVal = grid.CurrentRow.Cells["id"].Value;
            if (idVal == null || idVal == DBNull.Value) return;
            int id = Convert.ToInt32(idVal);

            try
            {
                // Birth / Marriage / Death open straight on the certificate (Crystal .rpt when
                // present, else the built-in replica). When the original scan is also on file
                // the operator is asked which one to see. Hold Shift to get the field list,
                // scans and requirements dialog instead; it is also the fallback if no report
                // renders.
                if (_current.CertKind.HasValue && (Control.ModifierKeys & Keys.Shift) == 0)
                {
                    byte[] pic = null;
                    string picLabel = null;
                    if (_current.Images.Length > 0)
                    {
                        // Column names are compile-time literals from the category list above.
                        var cols = new List<string>();
                        foreach (var img in _current.Images) cols.Add("`" + img.Column + "`");
                        DataTable it = Db.Pull(
                            "SELECT " + string.Join(", ", cols) + " FROM " + _current.DetailTable + " WHERE id = @id",
                            new MySqlParameter("@id", id));
                        if (it.Rows.Count > 0)
                        {
                            foreach (var img in _current.Images)
                            {
                                var b = it.Columns.Contains(img.Column) ? it.Rows[0][img.Column] as byte[] : null;
                                if (b != null && b.Length > 0) { pic = b; picLabel = img.Label; break; }
                            }
                        }
                    }

                    // 0 = cancelled, 1 = digital certificate, 2 = picture of the original.
                    int choice = pic == null ? 1 : AskCertificateView();
                    if (choice == 0) return;
                    if (choice == 2)
                    {
                        SoftcopyViewer.Show(pic, _current.Label + " — " + picLabel, this);
                        return;
                    }
                    if (CertificateReport.ShowFor(_current.CertKind.Value, id, this) != null)
                        return;
                }

                DataTable dt = Db.Pull("SELECT * FROM " + _current.DetailTable + " WHERE id = @id",
                    new MySqlParameter("@id", id));
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("That record no longer exists.", "Records Archive",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                ShowDetail(dt.Rows[0], _current);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load the record: " + CROMS.Data.ErrorLog.Reason(ex), "Records Archive",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Asks which copy of a certificate to open when both exist. Returns 0 when
        /// cancelled, 1 for the digital certificate, 2 for the picture of the original.
        /// </summary>
        private int AskCertificateView()
        {
            int result = 0;
            using (var dlg = new Form())
            {
                dlg.Text = "View Certificate";
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = false;
                dlg.ShowInTaskbar = false;
                dlg.ClientSize = new Size(440, 168);
                dlg.BackColor = UiTheme.PageBg;

                var lbl = new Label
                {
                    Text = "Which copy do you want to view?",
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    ForeColor = UiTheme.Ink,
                    AutoSize = false,
                    Location = new Point(20, 16),
                    Size = new Size(400, 26)
                };
                var hint = new Label
                {
                    Text = "The original scan is on file for this record.",
                    ForeColor = UiTheme.Muted,
                    AutoSize = false,
                    Location = new Point(20, 44),
                    Size = new Size(400, 20)
                };

                var btnDigital = new Button
                {
                    Text = "Digital Certificate",
                    Location = new Point(20, 80),
                    Size = new Size(195, 44),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = UiTheme.Accent,
                    ForeColor = Color.White
                };
                var btnPicture = new Button
                {
                    Text = "Picture of Original",
                    Location = new Point(225, 80),
                    Size = new Size(195, 44),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = UiTheme.Navy,
                    ForeColor = Color.White
                };
                var btnCancel = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(325, 132),
                    Size = new Size(95, 28),
                    FlatStyle = FlatStyle.Flat
                };

                btnDigital.Click += (s, e) => { result = 1; dlg.DialogResult = DialogResult.OK; };
                btnPicture.Click += (s, e) => { result = 2; dlg.DialogResult = DialogResult.OK; };

                dlg.Controls.AddRange(new Control[] { lbl, hint, btnDigital, btnPicture, btnCancel });
                dlg.AcceptButton = btnDigital;
                dlg.CancelButton = btnCancel;
                UiTheme.Polish(dlg);
                dlg.ShowDialog(this);
            }
            return result;
        }

        private void ShowDetail(DataRow row, ArchiveCategory cat)
        {
            using (var dlg = new Form())
            {
                dlg.Text = cat.Label + " — Record #" + row["id"];
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.Size = !string.IsNullOrEmpty(cat.ReqOwnerType) ? new Size(820, 720) : new Size(740, 640);
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = false;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;

                var btnClose = new Button
                {
                    Text = "Close",
                    DialogResult = DialogResult.OK,
                    Dock = DockStyle.Bottom,
                    Height = 42,
                    FlatStyle = FlatStyle.Flat
                };

                // topSection holds imagesPanel then (for a Form 90 record) the requirements
                // grid, as TableLayoutPanel rows — unambiguous top-to-bottom order, unlike two
                // sibling Dock=Top controls (this codebase's own established Dock=Top trap).
                var topSection = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    ColumnCount = 1
                };
                topSection.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                var imagesPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    Padding = new Padding(14, 10, 14, 4),
                    WrapContents = true
                };
                foreach (var img in cat.Images)
                {
                    if (!row.Table.Columns.Contains(img.Column)) continue;
                    object val = row[img.Column];
                    var bytes = val as byte[];
                    if (bytes == null || bytes.Length == 0) continue;
                    string caption = cat.Label + " — " + img.Label;
                    var btn = new Button
                    {
                        Text = "🖼 View " + img.Label,
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(29, 78, 216),
                        ForeColor = Color.White,
                        Margin = new Padding(0, 0, 10, 6),
                        Padding = new Padding(8, 4, 8, 4)
                    };
                    btn.Click += (s, e) => SoftcopyViewer.Show(bytes, caption, dlg);
                    imagesPanel.Controls.Add(btn);
                }

                // The certificate record types can always be rendered on the official
                // blank form (already in Assets) filled with the saved data — even when
                // no scan was ever attached — the same fallback the registration screens'
                // "View Softcopy" buttons use. Offered here too, since Records Archive is
                // read-only and this needs no unsaved-record guard.
                if (cat.CertKind.HasValue)
                {
                    string formCode = row.Table.Columns.Contains("form_code") && row["form_code"] != DBNull.Value
                        ? row["form_code"].ToString()
                        : null;
                    if (string.IsNullOrWhiteSpace(formCode))
                        formCode = FormCatalog.Current(cat.CertKind.Value)?.FormCode;
                    long certId = Convert.ToInt64(row["id"]);
                    var btnCert = new Button
                    {
                        Text = "🖹 View Certificate (Official Form)",
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(19, 36, 65),
                        ForeColor = Color.White,
                        Margin = new Padding(0, 0, 10, 6),
                        Padding = new Padding(8, 4, 8, 4)
                    };
                    btnCert.Click += (s, e) => CertificateReport.Show(formCode, certId, dlg);
                    imagesPanel.Controls.Add(btnCert);
                }

                // Marriage License applications (Form 90) render the same way — the blank
                // asset filled from the saved application — but through their own renderer
                // (Mf90Form), since Form 90 is not one of the FormCatalog certificate kinds.
                if (cat.IsMf90)
                {
                    long licenseId = Convert.ToInt64(row["id"]);
                    var btnMf90 = new Button
                    {
                        Text = "🖹 View Application (Official Form)",
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(19, 36, 65),
                        ForeColor = Color.White,
                        Margin = new Padding(0, 0, 10, 6),
                        Padding = new Padding(8, 4, 8, 4)
                    };
                    btnMf90.Click += (s, e) =>
                    {
                        try { Mf90Form.Show(MarriageService.LoadLicense((int)licenseId), dlg); }
                        catch (Exception ex) { MessageBox.Show(dlg, "Could not render the application: " + CROMS.Data.ErrorLog.Reason(ex), "Form 90", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    };
                    imagesPanel.Controls.Add(btnMf90);
                }

                topSection.Controls.Add(imagesPanel, 0, 0);

                // The supporting-document checklist (document_requirements) — reused by every
                // record type that has one: a marriage license application (owner_type
                // "License"), a marriage registration ("Marriage"), a delayed birth case
                // ("Birth" — blank/absent when the birth was never flagged/worked as delayed,
                // since nothing was ever synced for it), and every petition type ("Petition").
                // Shows what was required, whether it was attached, and whether an Admin
                // BYPASSED it instead of it being checked — the same bypassed_by/bypassed_at/
                // bypass_reason facts MarriageUi.RequirementsGrid shows on the live editing
                // screens, read-only here (this screen never writes — no Bypass/Attach actions,
                // View only).
                if (!string.IsNullOrEmpty(cat.ReqOwnerType))
                {
                    int ownerId = Convert.ToInt32(row["id"]);
                    List<ReqRow> reqs = MarriageService.Requirements(cat.ReqOwnerType, ownerId);
                    if (reqs.Count > 0)
                    {
                        topSection.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                        topSection.Controls.Add(BuildRequirementsPanel(reqs, dlg), 0, 1);
                    }
                }

                var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(14, 6, 14, 14) };
                var table = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Top };
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                foreach (DataColumn col in row.Table.Columns)
                {
                    if (col.DataType == typeof(byte[])) continue;
                    var lbl = new Label
                    {
                        Text = col.ColumnName,
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(91, 100, 114),
                        AutoSize = true,
                        Margin = new Padding(0, 6, 10, 6)
                    };
                    object v = row[col];
                    string text = (v == null || v == DBNull.Value) ? "—" : v.ToString();
                    var val2 = new Label
                    {
                        Text = text,
                        AutoSize = true,
                        MaximumSize = new Size(480, 0),
                        Margin = new Padding(0, 6, 0, 6)
                    };
                    table.RowCount++;
                    table.Controls.Add(lbl);
                    table.Controls.Add(val2);
                }
                body.Controls.Add(table);

                dlg.Controls.Add(topSection);
                dlg.Controls.Add(btnClose);
                dlg.Controls.Add(body);
                dlg.AcceptButton = btnClose;
                dlg.ShowDialog(this);
            }
        }

        /// <summary>
        /// Read-only requirements checklist for a Form 90 license — what MarriageUi.
        /// RequirementsGrid shows on the live editing screen, minus every write path (no
        /// Attach/Bypass buttons, no editable cells): a name, its status, whether an Admin
        /// bypassed it instead of it being checked (and by whom/why), and a View button when
        /// a scan/photo was attached to it. This screen only ever reads.
        /// </summary>
        private Control BuildRequirementsPanel(List<ReqRow> reqs, IWin32Window dlgOwner)
        {
            var wrap = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(14, 4, 14, 8) };
            var title = new Label
            {
                Text = "License Requirements",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(19, 36, 65),
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 4)
            };

            var grid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = Math.Min(240, 40 + reqs.Count * 30),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Party", HeaderText = "For", FillWeight = 50 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Req", HeaderText = "Requirement", FillWeight = 190 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Bypassed", HeaderText = "Bypassed", FillWeight = 200 });
            grid.Columns.Add(new DataGridViewButtonColumn { Name = "File", HeaderText = "Attachment", FillWeight = 70, FlatStyle = FlatStyle.Flat });

            foreach (ReqRow r in reqs)
            {
                string bypassed;
                if (r.IsBypassed)
                {
                    string who = string.IsNullOrEmpty(r.BypassedByName) ? "an Admin" : r.BypassedByName;
                    string when = r.BypassedAt.HasValue ? r.BypassedAt.Value.ToString("MM/dd/yyyy h:mm tt") : "";
                    bypassed = "Yes — by " + who + (when == "" ? "" : " on " + when) +
                        (string.IsNullOrEmpty(r.BypassReason) ? "" : ": " + r.BypassReason);
                }
                else bypassed = "No";

                int i = grid.Rows.Add(r.Party == "Both" ? "Both" : r.Party, r.Label ?? r.Code, r.Status,
                    bypassed, r.HasAttachment ? "View" : "—");
                DataGridViewRow row = grid.Rows[i];
                row.Tag = r;
                if (r.IsBypassed) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 205);
                grid.Columns["File"].MinimumWidth = 70;
                grid.Columns["Bypassed"].MinimumWidth = 160;
                if (!r.HasAttachment) row.Cells["File"].Style.ForeColor = Color.FromArgb(150, 150, 150);
            }
            grid.CellContentClick += (s, e) =>
            {
                if (e.RowIndex < 0 || grid.Columns[e.ColumnIndex].Name != "File") return;
                var r = grid.Rows[e.RowIndex].Tag as ReqRow;
                if (r == null || !r.HasAttachment) return;
                string fileName;
                byte[] bytes = MarriageService.RequirementAttachment(r.Id, out fileName);
                if (bytes == null || bytes.Length == 0)
                {
                    MessageBox.Show(dlgOwner, "No attachment stored for this requirement.", "Attachment",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                SoftcopyViewer.Show(bytes, (r.Label ?? r.Code) + " — " + fileName, dlgOwner);
            };

            wrap.Controls.Add(grid);
            wrap.Controls.Add(title);
            return wrap;
        }

        // ============================================================= SEARCH RECORDS
        // Absorbed from the retired standalone Record Search module (2026-09-28): matches
        // by name (LIKE) and, when sound-alike matching is on, also by MySQL SOUNDEX so
        // spelling variants match (e.g. "Dela Cruz" finds "dela cruz" / "de la Cruz").
        // Results from births, marriages and deaths UNION into one grid. Registry-book
        // information (book volume/page, registry number/year, date of registration,
        // Municipal Form revision) rides along on every hit and is shown in the rail —
        // this is what the separate Registry Books screen used to be reached for.

        private void EnterSearchMode()
        {
            lblCategoryTitle.Text = _searchCategory.Label;
            pnlSearchBar.Visible = true;
            cardDetail.Visible = true;
            btnViewRecord.Text = "View Record Details";
            btnViewCertificate.Visible = true;
            DockButtonsInRail(true);
            ApplyBounds();
            txtQuery.Focus();
        }

        private void ExitSearchMode()
        {
            pnlSearchBar.Visible = false;
            cardDetail.Visible = false;
            btnViewRecord.Text = "View Full Record";
            btnViewCertificate.Visible = false;
            btnViewRecord.Enabled = btnViewCertificate.Enabled = true;
            DockButtonsInRail(false);
            ApplyBounds();
        }

        /// <summary>
        /// Splits the rail into a scrolling body (the record's facts, rebuilt on every
        /// selection) and a fixed foot that holds the two record buttons.
        /// </summary>
        private void SetupRail()
        {
            cardDetail.AutoScroll = false;
            cardDetail.Padding = Padding.Empty;
            _railBody = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(14) };
            _railActions = new Panel { Dock = DockStyle.Bottom, BackColor = Color.White, Padding = new Padding(14, 12, 14, 14) };
            _railActions.Height = _railActions.Padding.Vertical + btnViewRecord.Height + 8 + btnViewCertificate.Height;
            _railActions.Paint += (s, e) =>
            {
                using (var p = new Pen(UiTheme.RowLine)) e.Graphics.DrawLine(p, 0, 0, _railActions.Width, 0);
            };
            _railGap = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.White };
            // Fill first, Bottom after: a docked child added later is laid out first.
            cardDetail.Controls.Add(_railBody);
            cardDetail.Controls.Add(_railActions);
        }

        private void DockButtonsInRail(bool inRail)
        {
            if (inRail)
            {
                if (btnViewRecord.Parent == _railActions) return;
                _railActions.SuspendLayout();
                btnViewRecord.Anchor = btnViewCertificate.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                // Added bottom-up: the last one added docks at the top.
                _railActions.Controls.Add(btnViewCertificate);
                _railActions.Controls.Add(_railGap);
                _railActions.Controls.Add(btnViewRecord);
                btnViewCertificate.Dock = btnViewRecord.Dock = DockStyle.Top;
                _railActions.ResumeLayout(true);
                return;
            }

            if (btnViewRecord.Parent == this) return;
            btnViewRecord.Dock = btnViewCertificate.Dock = DockStyle.None;
            Controls.Add(btnViewRecord);
            Controls.Add(btnViewCertificate);
            int top = ClientSize.Height - _buttonBottomOffset;
            btnViewRecord.SetBounds(_recordButtonHome.X, top, _recordButtonHome.Width, _recordButtonHome.Height);
            btnViewCertificate.SetBounds(_certButtonHome.X, top, _certButtonHome.Width, _certButtonHome.Height);
            btnViewRecord.Anchor = btnViewCertificate.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        }

        private static string CountText(int n) => n == 1 ? "1 record" : n + " records";

        private void txtQuery_TextChanged(object sender, EventArgs e) => DoSearch();
        private void cboSearchType_SelectedIndexChanged(object sender, EventArgs e) => DoSearch();
        private void chkFuzzy_CheckedChanged(object sender, EventArgs e) => DoSearch();

        private void DoSearch()
        {
            if (!_ready || _current == null || !_current.IsSearch) return;
            string term = txtQuery.Text.Trim();
            bool fuzzy = chkFuzzy.Checked && term.Length > 0;
            string type = cboSearchType.SelectedItem?.ToString() ?? "All Records";

            var parts = new List<string>();
            if (type == "All Records" || type == "Birth") parts.Add(SearchBirthQuery(term, fuzzy));
            if (type == "All Records" || type == "Marriage") parts.Add(SearchMarriageQuery(term, fuzzy));
            if (type == "All Records" || type == "Death") parts.Add(SearchDeathQuery(term, fuzzy));

            // A record with no name on it sorts LAST: sorted first, the screen opened on a blank row.
            string sql = "SELECT * FROM (" + string.Join(" UNION ALL ", parts) + ") r " +
                         "ORDER BY (r.Name = ''), r.Name LIMIT " + SearchLimit;

            var ps = new List<MySqlParameter>();
            if (term.Length > 0)
            {
                ps.Add(new MySqlParameter("@like", "%" + term + "%"));
                ps.Add(new MySqlParameter("@q", term));
            }

            // A column the operator sorted by stays sorted while they keep typing.
            string sortColumn = null;
            var sortDirection = System.ComponentModel.ListSortDirection.Ascending;
            if (grid.SortedColumn != null && grid.Columns.Contains("RegYear"))
            {
                sortColumn = grid.SortedColumn.Name;
                if (grid.SortOrder == SortOrder.Descending) sortDirection = System.ComponentModel.ListSortDirection.Descending;
            }

            try
            {
                DataTable dt = ps.Count == 0 ? Db.Pull(sql) : Db.Pull(sql, ps.ToArray());
                // Coming from another category: drop its columns so these come out in query order.
                if (!grid.Columns.Contains("RegYear")) grid.DataSource = null;
                grid.DataSource = dt;
                HideSearchWorkingColumns();
                if (sortColumn != null && grid.Columns.Contains(sortColumn))
                    grid.Sort(grid.Columns[sortColumn], sortDirection);
                else
                    MarkQueryOrder();
                lblCount.Text = dt.Rows.Count >= SearchLimit
                    ? "First " + SearchLimit + " shown, type a name to narrow"
                    : CountText(dt.Rows.Count);
                ShowSearchDetail();
            }
            catch (Exception ex)
            {
                grid.DataSource = null;
                lblCount.Text = "Search failed: " + CROMS.Data.ErrorLog.Reason(ex);
            }
        }

        /// <summary>Columns the grid carries for the rail but does not show.</summary>
        private static readonly string[] SearchWorkingColumns =
            { "id", "RegYear", "DateReg", "FormName", "FormCode" };

        private void HideSearchWorkingColumns()
        {
            foreach (string c in SearchWorkingColumns)
                if (grid.Columns.Contains(c)) grid.Columns[c].Visible = false;

            SetWeight("Type", 12);
            SetWeight("Registry No", 17);
            SetWeight("Name", 26);
            SetWeight("Event Date", 14);
            SetWeight("Book", 9);
            SetWeight("Page", 7);
            SetWeight("Status", 15);

            // Same date wording as the rail: 08/10/2026 can be read as two different days.
            if (grid.Columns.Contains("Event Date"))
            {
                DataGridViewCellStyle st = grid.Columns["Event Date"].DefaultCellStyle;
                st.Format = SearchDateFormat;
                st.FormatProvider = CultureInfo.InvariantCulture;
            }

            foreach (DataGridViewRow row in grid.Rows)
                foreach (DataGridViewCell cell in row.Cells)
                    if (cell.OwningColumn.Visible)
                    {
                        object v = cell.Value;
                        cell.ToolTipText = v == null || v == DBNull.Value ? ""
                            : v is DateTime ? ((DateTime)v).ToString(SearchDateFormat, CultureInfo.InvariantCulture)
                            : v.ToString();
                    }
        }

        /// <summary>
        /// Shows the sort arrow on Name while the list is still in the order the query
        /// returned (by name, blanks last). Also run when binding completes: the grid rebuilds
        /// its columns then and drops an arrow set earlier.
        /// </summary>
        private void MarkQueryOrder()
        {
            if (_current == null || !_current.IsSearch) return;
            if (grid.SortedColumn != null || !grid.Columns.Contains("Name")) return;
            grid.Columns["Name"].HeaderCell.SortGlyphDirection = SortOrder.Ascending;
        }

        private const int SearchLimit = 300;
        private const string SearchDateFormat = "dd MMM yyyy";
        private const string SearchNoName = "(no name recorded)";

        /// <summary>
        /// SQL for "Last, First Middle" out of three columns, '' when all of them are blank.
        /// A plain CONCAT printed a lone comma for a record with no name, and NULL when only
        /// one part was missing.
        /// </summary>
        private static string SqlName(string last, string first, string middle)
        {
            string l = "COALESCE(" + last + ",'')";
            string f = "COALESCE(" + first + ",'')";
            string m = middle == null ? "''" : "COALESCE(" + middle + ",'')";
            return "TRIM(CONCAT(" + l + ", CASE WHEN " + l + " <> '' AND " + f + " <> '' THEN ', ' ELSE '' END, " +
                   f + ", ' ', " + m + "))";
        }

        private void SetWeight(string column, int weight)
        {
            if (grid.Columns.Contains(column)) grid.Columns[column].FillWeight = weight;
        }

        // Each sub-query returns the SAME columns in the same order so they can be UNIONed:
        //   Type | id | Registry No | Name | Event Date | Book | Page | Status
        //   | RegYear | DateReg | FormName | FormCode
        private static string SearchBirthQuery(string term, bool fuzzy)
        {
            string where = SearchWhere(term, fuzzy,
                "(first_name LIKE @like OR middle_name LIKE @like OR last_name LIKE @like OR " +
                "CONCAT(first_name,' ',last_name) LIKE @like)",
                "SOUNDEX(last_name) = SOUNDEX(@q) OR SOUNDEX(first_name) = SOUNDEX(@q)");
            return "SELECT 'Birth' AS Type, id, registry_no AS 'Registry No', " +
                   SqlName("last_name", "first_name", "middle_name") + " AS Name, " +
                   "date_of_birth AS 'Event Date', " +
                   "book_volume AS Book, book_page AS Page, status AS Status, " +
                   SearchRegistryYear + ", date_registered AS DateReg, " +
                   "form_name AS FormName, form_code AS FormCode FROM births" + where;
        }

        private static string SearchDeathQuery(string term, bool fuzzy)
        {
            string where = SearchWhere(term, fuzzy,
                "full_name LIKE @like",
                "SOUNDEX(full_name) = SOUNDEX(@q)");
            // `deaths` has no date_registered column — reported as NOT RECORDED rather than
            // substituted with created_at, which for a digitized backlog record is the
            // SCANNING date, not the date the office registered the death.
            // Last name first, like births and marriages, so sorting by Name is one alphabet.
            // A record that only has the joined full_name keeps it as stored: it is not split by guess.
            return "SELECT 'Death' AS Type, id, registry_no AS 'Registry No', " +
                   "CASE WHEN COALESCE(last_name,'') <> '' THEN " + SqlName("last_name", "first_name", "middle_name") +
                   " ELSE COALESCE(full_name,'') END AS Name, date_of_death AS 'Event Date', " +
                   "book_volume AS Book, book_page AS Page, status AS Status, " +
                   SearchRegistryYear + ", CAST(NULL AS DATE) AS DateReg, " +
                   "form_name AS FormName, form_code AS FormCode FROM deaths" + where;
        }

        private static string SearchMarriageQuery(string term, bool fuzzy)
        {
            string where = SearchWhere(term, fuzzy,
                "(husband_first_name LIKE @like OR husband_last_name LIKE @like OR " +
                "wife_first_name LIKE @like OR wife_last_name LIKE @like)",
                "SOUNDEX(husband_last_name) = SOUNDEX(@q) OR SOUNDEX(wife_last_name) = SOUNDEX(@q)");
            string h = SqlName("husband_last_name", "husband_first_name", null);
            string w = SqlName("wife_last_name", "wife_first_name", null);
            return "SELECT 'Marriage' AS Type, id, registry_no AS 'Registry No', " +
                   "CASE WHEN " + h + " <> '' AND " + w + " <> '' THEN CONCAT(" + h + ", '  &  ', " + w + ") " +
                   "ELSE CONCAT(" + h + ", " + w + ") END AS Name, " +
                   "date_of_marriage AS 'Event Date', " +
                   "book_volume AS Book, book_page AS Page, status AS Status, " +
                   SearchRegistryYear + ", date_registered AS DateReg, " +
                   "form_name AS FormName, form_code AS FormCode FROM marriages" + where;
        }

        /// <summary>
        /// The registry YEAR, read off the record and never inferred from the event — see
        /// the long-standing rationale in the retired RecordSearchForm: no fall-back to
        /// YEAR(event date), and the leading-digit pattern requires a trailing separator so
        /// a bare legacy number ("239103") is never reported as a fabricated year.
        /// </summary>
        private const string SearchRegistryYear =
            "CASE WHEN registry_no REGEXP '^(19|20)[0-9]{2}[^0-9]' THEN LEFT(registry_no,4) " +
            "WHEN book_volume REGEXP '^(19|20)[0-9]{2}$' THEN book_volume END AS RegYear";

        private static string SearchWhere(string term, bool fuzzy, string likeClause, string soundexClause)
        {
            if (term.Length == 0) return "";
            return " WHERE (" + likeClause + (fuzzy ? " OR " + soundexClause : "") + ")";
        }

        // ----------------------------------------------------- search results badge
        private void grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_current == null || !_current.IsSearch) return;
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
            else if (column == "Name" &&
                     (e.Value == null || e.Value == DBNull.Value || e.Value.ToString().Trim().Length == 0))
            {
                e.Value = SearchNoName;
                e.CellStyle.ForeColor = UiTheme.Faint;
                e.FormattingApplied = true;
            }
        }

        /// <summary>
        /// Status as a coloured pill WITH its text (never colour alone), so a record that
        /// still needs action stands out from the registered ones around it.
        /// </summary>
        private void grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (_current == null || !_current.IsSearch || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].Name != "Status") return;
            string text = e.Value == null || e.Value == DBNull.Value ? "" : e.Value.ToString().Trim();
            if (text.Length == 0) return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);

            Color tint, ink;
            MUi.ToneOf(text, out tint, out ink);
            const TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine |
                                          TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter |
                                          TextFormatFlags.EndEllipsis;
            using (var font = new Font(e.CellStyle.Font.FontFamily, 8.5F, FontStyle.Bold))
            {
                int height = Math.Min(22, e.CellBounds.Height - 8);
                int width = Math.Min(e.CellBounds.Width - 14,
                    // Measured WITHOUT the ellipsis flag: with it, an unbounded measure comes back already cut.
                    TextRenderer.MeasureText(e.Graphics, text, font, new Size(int.MaxValue, int.MaxValue),
                        TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width + 16);
                if (height < 12 || width < 24) { e.Handled = true; return; }
                var pill = new Rectangle(e.CellBounds.X + 7, e.CellBounds.Y + (e.CellBounds.Height - height) / 2, width, height);

                var old = e.Graphics.SmoothingMode;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var path = RoundedRow(pill, height / 2 - 1))
                using (var b = new SolidBrush(tint)) e.Graphics.FillPath(b, path);
                e.Graphics.SmoothingMode = old;
                TextRenderer.DrawText(e.Graphics, text, font, pill, ink, tint, flags);
            }
            e.Handled = true;
        }

        // ----------------------------------------------------- search detail rail
        private void grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_current != null && _current.IsSearch) ShowSearchDetail();
        }

        private void ShowSearchDetail()
        {
            _railBody.SuspendLayout();
            foreach (Control c in ToArray(_railBody.Controls))
            {
                _railBody.Controls.Remove(c);
                c.Dispose();
            }

            DataGridViewRow row = SearchSelectedRow();
            // Both buttons act on the selected record, so with none selected they have nothing to do.
            btnViewRecord.Enabled = btnViewCertificate.Enabled = row != null;
            if (row == null)
            {
                SearchStack(_railBody,
                    MUi.SectionHeader("No record selected",
                        "Click a result on the left to see its registry book details here."));
                _railBody.ResumeLayout(true);
                return;
            }

            string type = SearchCell(row, "Type");
            string name = SearchCell(row, "Name");
            string book = SearchCell(row, "Book");
            string page = SearchCell(row, "Page");

            var badgeHost = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.Transparent, Margin = new Padding(0) };
            StatusPill badge = MUi.RecordPill(type);
            badge.Location = new Point(0, 0);
            badgeHost.Controls.Add(badge);

            var nameLabel = new Label
            {
                Text = name.Length == 0 ? SearchNoName : name,
                Dock = DockStyle.Top, Height = 46, AutoSize = false, UseMnemonic = false,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = name.Length == 0 ? UiTheme.Faint : UiTheme.Ink,
                BackColor = Color.Transparent, Margin = new Padding(0, 6, 0, 4)
            };

            var rows = new List<Control>
            {
                badgeHost,
                nameLabel,
                SearchGap(6),
                MUi.Cap("Registry entry"),
                MUi.Kv("Register", type.Length == 0 ? SearchNotRecorded : type + " register"),
                MUi.Kv("Registry no.", SearchOr(SearchCell(row, "Registry No"))),
                MUi.Kv("Registry year", SearchOr(SearchCell(row, "RegYear"))),
                MUi.Kv("Date registered", SearchDateOr(row, "DateReg", type)),
                MUi.Kv(SearchEventLabel(type), SearchDateOr(row, "Event Date", null)),
                MUi.Kv("Status", SearchOr(SearchCell(row, "Status")), MUi.InkOf(SearchCell(row, "Status"))),
                SearchGap(14),
                MUi.Cap("Registry book"),
                MUi.Kv("Book / volume", SearchOr(book)),
                MUi.Kv("Page", SearchOr(page))
            };

            if (book.Length == 0 || page.Length == 0)
                rows.Add(SearchNote("The book and page are typed on " + type + " Registration, on the " +
                              "record itself — they are blank here because the paper book entry " +
                              "has not been recorded for this record yet."));

            List<Control> facts = SearchRecordFacts(type, SearchCell(row, "id"));
            if (facts.Count > 0)
            {
                rows.Add(SearchGap(14));
                rows.Add(MUi.Cap("On the record"));
                rows.AddRange(facts);
            }

            rows.Add(SearchGap(14));
            rows.Add(MUi.Cap("Source form"));
            rows.Add(MUi.Kv("Form", SearchOr(SearchCell(row, "FormName"))));
            rows.Add(MUi.Kv("Form code", SearchOr(SearchCell(row, "FormCode"))));

            // The two record buttons sit in the rail's fixed foot (see SetupRail), not in this list.
            SearchStack(_railBody, rows.ToArray());
            _railBody.ResumeLayout(true);
        }

        private static Control SearchGap(int height)
        {
            return new Panel { Height = height, BackColor = Color.Transparent, Margin = Padding.Empty };
        }

        /// <summary>
        /// A few more facts off the selected record, read from its certificate view (lookups
        /// already resolved to names). One small query per selection; a failure just leaves
        /// the section out, it never blocks the rail.
        /// </summary>
        private List<Control> SearchRecordFacts(string type, string idText)
        {
            var list = new List<Control>();
            long id;
            if (!long.TryParse(idText, out id)) return list;

            string sql;
            switch (type)
            {
                case "Birth":
                    sql = "SELECT sex, place_of_birth AS place, " +
                          "CONCAT_WS(' ', NULLIF(mother_first_name,''), NULLIF(mother_middle_name,''), NULLIF(mother_last_name,'')) AS mother, " +
                          "father_full_name AS father FROM v_birth_certificate WHERE record_id = @id";
                    break;
                case "Marriage":
                    sql = "SELECT COALESCE(NULLIF(place_of_marriage,''), CONCAT_WS(', ', NULLIF(church_name,''), " +
                          "NULLIF(place_municipality,''), NULLIF(place_province,''))) AS place, solemnizer " +
                          "FROM v_marriage_certificate WHERE record_id = @id";
                    break;
                case "Death":
                    sql = "SELECT sex, age, place_of_death AS place FROM v_death_certificate WHERE record_id = @id";
                    break;
                default:
                    return list;
            }

            try
            {
                DataTable dt = Db.Pull(sql, new MySqlParameter("@id", id));
                if (dt.Rows.Count == 0) return list;
                DataRow r = dt.Rows[0];
                Func<string, string> val = c =>
                    dt.Columns.Contains(c) && r[c] != DBNull.Value ? r[c].ToString().Trim() : "";

                if (type == "Birth")
                {
                    list.Add(MUi.Kv("Sex", SearchOr(val("sex"))));
                    list.Add(SearchFact("Place of birth", val("place")));
                    list.Add(SearchFact("Mother (maiden name)", val("mother")));
                    list.Add(SearchFact("Father", val("father")));
                }
                else if (type == "Marriage")
                {
                    list.Add(SearchFact("Place of marriage", val("place")));
                    list.Add(SearchFact("Solemnizing officer", val("solemnizer")));
                }
                else
                {
                    list.Add(MUi.Kv("Sex", SearchOr(val("sex"))));
                    list.Add(MUi.Kv("Age at death", SearchOr(val("age"))));
                    list.Add(SearchFact("Place of death", val("place")));
                }
            }
            catch
            {
                list.Clear();
            }
            return list;
        }

        /// <summary>
        /// A caption with its value on the line(s) below. Places and names are too long for
        /// the two-column row, which cuts them with "...".
        /// </summary>
        private Control SearchFact(string key, string value)
        {
            bool blank = value.Length == 0;
            var font = new Font("Segoe UI", 9F, FontStyle.Bold);
            int width = Math.Max(120, cardDetail.ClientSize.Width - _railBody.Padding.Horizontal -
                                      SystemInformation.VerticalScrollBarWidth);
            int textHeight = TextRenderer.MeasureText(blank ? SearchNotRecorded : value, font, new Size(width, 0),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;

            var host = new Panel { Height = 24 + textHeight + 8, BackColor = Color.Transparent, Margin = Padding.Empty };
            host.Controls.Add(new Label
            {
                Text = blank ? SearchNotRecorded : value, Dock = DockStyle.Fill, AutoSize = false, UseMnemonic = false,
                Font = font, ForeColor = blank ? UiTheme.Faint : UiTheme.Ink, BackColor = Color.Transparent
            });
            host.Controls.Add(new Label
            {
                Text = key, Dock = DockStyle.Top, Height = 24, AutoSize = false, UseMnemonic = false,
                TextAlign = ContentAlignment.BottomLeft,
                Font = new Font("Segoe UI", 9F), ForeColor = UiTheme.Muted, BackColor = Color.Transparent
            });
            host.Paint += (s, e) =>
            {
                using (var p = new Pen(UiTheme.RowLine)) e.Graphics.DrawLine(p, 0, host.Height - 1, host.Width, host.Height - 1);
            };
            return host;
        }

        /// <summary>
        /// The row the rail and the jump both act on. The grid is FullRowSelect, so
        /// SelectedRows is the authoritative answer and CurrentRow is only a fallback.
        /// </summary>
        private DataGridViewRow SearchSelectedRow()
        {
            if (grid.SelectedRows.Count > 0 && !grid.SelectedRows[0].IsNewRow)
                return grid.SelectedRows[0];
            DataGridViewRow cur = grid.CurrentRow;
            return cur != null && !cur.IsNewRow ? cur : null;
        }

        private const string SearchNotRecorded = "not recorded";

        private static string SearchOr(string value) => value.Length == 0 ? SearchNotRecorded : value;

        private static Control[] ToArray(Control.ControlCollection cc)
        {
            var list = new Control[cc.Count];
            cc.CopyTo(list, 0);
            return list;
        }

        /// <summary>Adds controls in VISUAL order — see the retired RecordSearchForm's note on
        /// why Dock must be forced and TabIndex stated explicitly here.</summary>
        private static void SearchStack(Control host, params Control[] visualOrder)
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

        private static Control SearchNote(string text)
        {
            return new Label
            {
                Text = text, Dock = DockStyle.Top, Height = 62, AutoSize = false, UseMnemonic = false,
                Font = new Font("Segoe UI", 8.5F), ForeColor = UiTheme.Muted,
                BackColor = Color.Transparent, Margin = new Padding(0, 10, 0, 0)
            };
        }

        private static string SearchEventLabel(string type)
        {
            switch (type)
            {
                case "Birth": return "Date of birth";
                case "Marriage": return "Date of marriage";
                case "Death": return "Date of death";
                default: return "Event date";
            }
        }

        private static string SearchCell(DataGridViewRow row, string column)
        {
            if (!row.DataGridView.Columns.Contains(column)) return "";
            object v = row.Cells[column].Value;
            return v == null || v == DBNull.Value ? "" : v.ToString().Trim();
        }

        /// <summary>A date column, formatted from its own typed value — never re-parsed from a
        /// culture-formatted string.</summary>
        private static string SearchDateOr(DataGridViewRow row, string column, string typeForDeathNote)
        {
            if (!row.DataGridView.Columns.Contains(column)) return SearchNotRecorded;
            object v = row.Cells[column].Value;
            if (v is DateTime dt) return dt.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
            if (v == null || v == DBNull.Value)
                return typeForDeathNote == "Death" ? "not kept for deaths" : SearchNotRecorded;
            string s = v.ToString().Trim();
            return s.Length == 0 ? SearchNotRecorded : s;
        }

        /// <summary>Opens the selected search hit directly, loaded into its own registration
        /// module's edit form — view, edit or delete it there — instead of merely navigating
        /// to the module and leaving the operator to find the row by hand.</summary>
        private void OpenSearchRecord()
        {
            DataGridViewRow row = SearchSelectedRow();
            if (row == null) return;

            string type = SearchCell(row, "Type");
            string idText = SearchCell(row, "id");
            int id;
            if (!int.TryParse(idText, out id)) return;

            string key;
            switch (type)
            {
                case "Birth": key = "birth"; break;
                case "Marriage": key = "marriage"; break;
                case "Death": key = "death"; break;
                default: return;
            }

            MainForm shell = SearchShell();
            if (shell == null) return;
            Form target = shell.GoToModule(key);

            var birth = target as BirthRegistrationForm;
            var marriage = target as MarriageRegistrationForm;
            var death = target as DeathRegistrationForm;
            if (birth != null) birth.OpenRecordForEdit(id);
            else if (marriage != null) marriage.OpenRecordForEdit(id);
            else if (death != null) death.OpenRecordForEdit(id);
        }

        /// <summary>
        /// Opens the selected hit's certificate (preview, with Print from the viewer) straight
        /// from the archive — the same shared path Birth Registration's Print Certificate uses,
        /// so no registration window has to be opened. The record's own stored form revision
        /// decides the layout (ShowFor reads form_code), so an old record prints on its own form.
        /// </summary>
        private void btnViewCertificate_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = SearchSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Select a record in the list first.", "View Certificate",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DocKind kind;
            switch (SearchCell(row, "Type"))
            {
                case "Birth": kind = DocKind.Birth; break;
                case "Marriage": kind = DocKind.Marriage; break;
                case "Death": kind = DocKind.Death; break;
                default: return;
            }

            long id;
            if (!long.TryParse(SearchCell(row, "id"), out id)) return;

            try
            {
                // ShowFor explains itself (record not found / unknown form) when it returns null.
                CertificateReport.ShowFor(kind, id, this);
            }
            catch (Exception ex)
            {
                ErrorLog.Report(this, "Records Archive", ex, "open");
            }
        }

        /// <summary>Walks up the control tree to the application shell (MainForm).</summary>
        private MainForm SearchShell()
        {
            for (Control p = Parent; p != null; p = p.Parent)
                if (p is MainForm mf) return mf;
            return null;
        }
    }
}
