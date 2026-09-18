using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Records Archive (Admin only) — one screen that browses every record, form and
    /// image the system has ever saved, grouped by type in the left tree: births,
    /// marriages, deaths, marriage licenses, every petition type (RA9048/RA10172/
    /// Legitimation/SupplementalReport/LegalInstrument/CourtOrder), certificate
    /// requests, BREQS, claim requests, releases, and queue tickets. Read-only — this
    /// screen never writes to the database. Selecting a category lists its rows;
    /// "View Full Record" (or double-click) shows every column plus any stored
    /// scan/photo, opened through the existing SoftcopyViewer.
    ///
    /// Not registered in any role's AllowedKeys list in MainForm, so only Admin sees
    /// it in the sidebar (MainForm.AllowedKeys returns null — full access — for any
    /// role not explicitly listed).
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
        }

        private readonly List<ArchiveCategory> _categories = new List<ArchiveCategory>();
        private ArchiveCategory _current;

        public RecordsArchiveForm()
        {
            InitializeComponent();
            BuildCategories();
            BuildTree();
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
                CertKind = DocKind.Birth
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
                CertKind = DocKind.Marriage
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
                IsMf90 = true
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
                Images = new (string, string)[0]
            });
        }

        // ------------------------------------------------------------ tree
        private void BuildTree()
        {
            treeCategories.Nodes.Clear();
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
        }

        private void treeCategories_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is ArchiveCategory cat) LoadCategory(cat);
        }

        // ------------------------------------------------------------ list
        private void LoadCategory(ArchiveCategory cat)
        {
            _current = cat;
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
            if (e.RowIndex >= 0) ViewSelected();
        }

        private void btnViewRecord_Click(object sender, EventArgs e) => ViewSelected();

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

        private void ShowDetail(DataRow row, ArchiveCategory cat)
        {
            using (var dlg = new Form())
            {
                dlg.Text = cat.Label + " — Record #" + row["id"];
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.Size = cat.IsMf90 ? new Size(820, 720) : new Size(740, 640);
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

                // Form 90's supporting-document checklist (marriage_requirements): what was
                // required, whether it was attached, and whether an Admin BYPASSED it instead
                // of it being checked — the same bypassed_by/bypassed_at/bypass_reason facts
                // MarriageUi.RequirementsGrid shows on the live editing screen, read-only here
                // (this screen never writes — no Bypass/Attach actions, View only).
                if (cat.IsMf90)
                {
                    long licenseId = Convert.ToInt64(row["id"]);
                    List<ReqRow> reqs = MarriageService.Requirements("License", (int)licenseId);
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
    }
}
