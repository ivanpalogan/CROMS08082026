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
    /// tree: births, marriages, deaths, marriage licenses, every petition type
    /// (RA9048/RA10172/Legitimation/SupplementalReport/LegalInstrument/CourtOrder),
    /// certificate requests, BREQS, claim requests, releases, and queue tickets.
    /// Read-only throughout — this screen never writes to the database. Selecting a
    /// category lists its rows; "View Full Record" (or double-click) shows every
    /// column plus any stored scan/photo, opened through the existing SoftcopyViewer.
    /// Selecting "Search Records" instead shows a name search bar and a registry-book
    /// detail rail for the highlighted hit; double-click (or the button) jumps to that
    /// record's own registration module.
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
            /// <summary>owner_type value in marriage_requirements for this record's row id —
            /// "License"/"Marriage"/"Birth"/"Petition" — or null when this record type has no
            /// requirements checklist. Drives the read-only requirements panel in ShowDetail.</summary>
            public string ReqOwnerType;
            /// <summary>True only for the synthetic "Search Records" node — routes LoadCategory
            /// into DoSearch() instead of the normal single-table SELECT.</summary>
            public bool IsSearch;
        }

        private readonly List<ArchiveCategory> _categories = new List<ArchiveCategory>();
        private readonly ArchiveCategory _searchCategory = new ArchiveCategory { Label = "Search Records", IsSearch = true };
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

        public RecordsArchiveForm()
        {
            InitializeComponent();
            _gridFullBounds = grid.Bounds;
            _bottomReserve = ClientSize.Height - _gridFullBounds.Bottom;
            _rightReserve = ClientSize.Width - _gridFullBounds.Right;
            cboSearchType.SelectedIndex = 0;
            BuildCategories();
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

            if (_current.IsSearch)
            {
                int top = pnlSearchBar.Bottom + 12;
                const int detailWidth = 330;
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
                grid.Bounds = new Rectangle(_gridFullBounds.X, top, gridWidth, height);
            }
        }

        public void RefreshData()
        {
            if (_current != null) LoadCategory(_current);
        }

        // ------------------------------------------------------------ categories
        private void BuildCategories()
        {
            _categories.Add(new ArchiveCategory
            {
                Group = "Civil Registry Records",
                Label = "Birth Registration",
                DetailTable = "births",
                Sql = "SELECT id, registry_no AS 'Registry No', " +
                      "TRIM(CONCAT(last_name,', ',first_name,' ',COALESCE(middle_name,''))) AS Name, " +
                      "sex AS Sex, date_of_birth AS 'Date of Birth', status AS Status, " +
                      "COALESCE(form_code,'') AS Form, created_at AS Recorded FROM births ORDER BY created_at DESC",
                Images = new[] { ("scan_image", "Scanned Certificate"), ("birth_image", "Birth Image (legacy)") },
                CertKind = DocKind.Birth,
                ReqOwnerType = "Birth"
            });
            _categories.Add(new ArchiveCategory
            {
                Group = "Civil Registry Records",
                Label = "Marriage Registration",
                DetailTable = "marriages",
                Sql = "SELECT id, registry_no AS 'Registry No', " +
                      "TRIM(CONCAT(husband_last_name,', ',husband_first_name)) AS Husband, " +
                      "TRIM(CONCAT(wife_last_name,', ',wife_first_name)) AS Wife, " +
                      "date_of_marriage AS 'Date of Marriage', status AS Status, " +
                      "COALESCE(form_code,'') AS Form, created_at AS Recorded FROM marriages ORDER BY created_at DESC",
                Images = new[] { ("scan_image", "Scanned Certificate") },
                CertKind = DocKind.Marriage,
                ReqOwnerType = "Marriage"
            });
            _categories.Add(new ArchiveCategory
            {
                Group = "Civil Registry Records",
                Label = "Death Registration",
                DetailTable = "deaths",
                Sql = "SELECT id, registry_no AS 'Registry No', full_name AS Name, " +
                      "date_of_death AS 'Date of Death', status AS Status, " +
                      "COALESCE(form_code,'') AS Form, created_at AS Recorded FROM deaths ORDER BY created_at DESC",
                Images = new[] { ("scan_image", "Scanned Certificate") },
                CertKind = DocKind.Death
            });
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
                DetailTable = "breqs_requests",
                Sql = "SELECT id, request_no AS 'Request No', doc_type AS 'Doc Type', " +
                      "TRIM(CONCAT(COALESCE(requester_last,''),', ',COALESCE(requester_first,''))) AS Requester, " +
                      "TRIM(CONCAT(COALESCE(owner_last,''),', ',COALESCE(owner_first,''))) AS 'For (Owner)', " +
                      "status AS Status, created_at AS Recorded FROM breqs_requests ORDER BY created_at DESC",
                Images = new[] { ("scan_image", "PSA Copy Scan") }
            });

            _categories.Add(new ArchiveCategory
            {
                Group = "Claims & Releases",
                Label = "Claim Requests (ID Uploads)",
                DetailTable = "claim_requests",
                Sql = "SELECT id, claim_ticket_no AS 'Claim No', " +
                      "TRIM(CONCAT(COALESCE(last_name,''),', ',COALESCE(first_name,''))) AS Requester, " +
                      "request_details AS Details, status AS Status, created_at AS Recorded " +
                      "FROM claim_requests ORDER BY created_at DESC",
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
        private void BuildTree()
        {
            treeCategories.Nodes.Clear();
            var searchNode = new TreeNode("🔎  " + _searchCategory.Label)
            {
                Tag = _searchCategory,
                ForeColor = UiTheme.Accent,
                NodeFont = new Font(treeCategories.Font, FontStyle.Bold)
            };
            treeCategories.Nodes.Add(searchNode);

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
                groupNode.Nodes.Add(new TreeNode(cat.Label) { Tag = cat });
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
                EnterSearchMode();
                DoSearch();
                return;
            }

            ExitSearchMode();
            lblCategoryTitle.Text = cat.Label;
            try
            {
                DataTable dt = Db.Pull(cat.Sql);
                grid.DataSource = dt;
                if (grid.Columns.Contains("id")) grid.Columns["id"].Visible = false;
                lblCount.Text = dt.Rows.Count + " record(s)";
            }
            catch (Exception ex)
            {
                grid.DataSource = null;
                lblCount.Text = "Load failed: " + ex.Message;
            }
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
                MessageBox.Show("Could not load the record: " + ex.Message, "Records Archive",
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
                        catch (Exception ex) { MessageBox.Show(dlg, "Could not render the application: " + ex.Message, "Form 90", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    };
                    imagesPanel.Controls.Add(btnMf90);
                }

                topSection.Controls.Add(imagesPanel, 0, 0);

                // The supporting-document checklist (marriage_requirements) — reused by every
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
            btnViewRecord.Text = "Open in Module";
            ApplyBounds();
            txtQuery.Focus();
        }

        private void ExitSearchMode()
        {
            pnlSearchBar.Visible = false;
            cardDetail.Visible = false;
            btnViewRecord.Text = "View Full Record";
            ApplyBounds();
        }

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
                HideSearchWorkingColumns();
                lblCount.Text = dt.Rows.Count + " record(s) found" +
                    (type == "All Records" ? "" : "  ·  " + type + " only") +
                    (term.Length == 0 ? "  ·  showing all — type a name to search" :
                     fuzzy ? "  ·  name + sound-alike match" : "  ·  exact name match");
                ShowSearchDetail();
            }
            catch (Exception ex)
            {
                grid.DataSource = null;
                lblCount.Text = "Search failed: " + ex.Message;
            }
        }

        /// <summary>Columns the grid carries for the rail but does not show.</summary>
        private static readonly string[] SearchWorkingColumns =
            { "id", "RegYear", "DateReg", "FormName", "FormCode" };

        private void HideSearchWorkingColumns()
        {
            foreach (string c in SearchWorkingColumns)
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
        private static string SearchBirthQuery(string term, bool fuzzy)
        {
            string where = SearchWhere(term, fuzzy,
                "(first_name LIKE @like OR middle_name LIKE @like OR last_name LIKE @like OR " +
                "CONCAT(first_name,' ',last_name) LIKE @like)",
                "SOUNDEX(last_name) = SOUNDEX(@q) OR SOUNDEX(first_name) = SOUNDEX(@q)");
            return "SELECT 'Birth' AS Type, id, registry_no AS 'Registry No', " +
                   "TRIM(CONCAT(last_name, ', ', first_name, ' ', COALESCE(middle_name,''))) AS Name, " +
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
            return "SELECT 'Death' AS Type, id, registry_no AS 'Registry No', " +
                   "full_name AS Name, date_of_death AS 'Event Date', " +
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
            return "SELECT 'Marriage' AS Type, id, registry_no AS 'Registry No', " +
                   "TRIM(CONCAT(husband_last_name, ', ', husband_first_name, '  &  ', " +
                   "wife_last_name, ', ', wife_first_name)) AS Name, " +
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
        }

        // ----------------------------------------------------- search detail rail
        private void grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_current != null && _current.IsSearch) ShowSearchDetail();
        }

        private void ShowSearchDetail()
        {
            cardDetail.SuspendLayout();
            foreach (Control c in ToArray(cardDetail.Controls))
            {
                cardDetail.Controls.Remove(c);
                c.Dispose();
            }

            DataGridViewRow row = SearchSelectedRow();
            if (row == null)
            {
                SearchStack(cardDetail,
                    MUi.SectionHeader("No record selected",
                        "Click a result on the left to see its registry book details here."));
                cardDetail.ResumeLayout(true);
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
                MUi.Kv("Register", type.Length == 0 ? SearchNotRecorded : type + " register"),
                MUi.Kv("Registry no.", SearchOr(SearchCell(row, "Registry No"))),
                MUi.Kv("Registry year", SearchOr(SearchCell(row, "RegYear"))),
                MUi.Kv("Date registered", SearchDateOr(row, "DateReg", type)),
                MUi.Kv(SearchEventLabel(type), SearchDateOr(row, "Event Date", null)),
                MUi.Kv("Status", SearchOr(SearchCell(row, "Status")), MUi.InkOf(SearchCell(row, "Status"))),
                MUi.Cap("Registry book"),
                MUi.Kv("Book / volume", SearchOr(book)),
                MUi.Kv("Page", SearchOr(page)),
                MUi.Cap("Source form"),
                MUi.Kv("Form", SearchOr(SearchCell(row, "FormName"))),
                MUi.Kv("Form code", SearchOr(SearchCell(row, "FormCode")))
            };

            if (book.Length == 0 || page.Length == 0)
                rows.Add(SearchNote("The book and page are typed on " + type + " Registration, on the " +
                              "record itself — they are blank here because the paper book entry " +
                              "has not been recorded for this record yet."));

            Button open = MUi.Btn("Open in " + type + " Registration", MUi.Kind.Primary);
            open.Dock = DockStyle.Top;
            open.Margin = new Padding(0, 14, 0, 0);
            open.Click += (s, e) => OpenSearchRecord();
            rows.Add(open);

            SearchStack(cardDetail, rows.ToArray());
            cardDetail.ResumeLayout(true);
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

        /// <summary>Walks up the control tree to the application shell (MainForm).</summary>
        private MainForm SearchShell()
        {
            for (Control p = Parent; p != null; p = p.Parent)
                if (p is MainForm mf) return mf;
            return null;
        }
    }
}
