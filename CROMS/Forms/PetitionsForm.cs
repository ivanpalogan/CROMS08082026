using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Petitions (RA 9048 / RA 10172) plus the four TRACK-ONLY case types from backlog Phase 4 —
    /// Legitimation (RA 9858), Supplemental Report, Legal Instrument (RA 9255 Acknowledgment /
    /// AUSF) and Court Order annotation. One generic tracker, not four near-copies.
    ///
    /// Redesigned (2026-09-16) onto the app's card system: left card is a searchable/filterable
    /// case list with stage-tinted badges and a small counts strip; right card is the case
    /// editor, whose single most-emphasized action is always the one clear next step — "Advance
    /// to &lt;next stage&gt;" while editing an existing case, or "Save Petition" while filing a
    /// new one. Save/New/Delete are demoted to a secondary row so the screen never shows four
    /// equal-weight buttons with no indication of which one to press. Data access, validation
    /// rules and the RA-vs-track-only stage sequences are unchanged from the original build.
    /// </summary>
    public partial class PetitionsForm : Form, IRefreshable
    {
        private int? _editingId;
        private DataTable _dt;

        // enum code <-> friendly label maps (DB stores the codes). Order MUST match the
        // cboType item order added in BuildLayout.
        private static readonly string[] TypeCodes =
            { "RA9048", "RA10172", "Legitimation", "SupplementalReport", "LegalInstrument", "CourtOrder" };
        private static readonly string[] TypeFilterLabels =
        {
            "RA 9048", "RA 10172", "Legitimation (RA 9858)",
            "Supplemental Report", "Legal Instrument", "Court Order"
        };

        // RA9048/RA10172 carry a real statutory 15-day public-posting requirement; the other
        // four case types have no posting period, so they get "Under Review" in its place.
        private static readonly string[] StageCodesRA = { "Filed", "Posted", "Decision", "PSA_Endorsement" };
        private static readonly string[] StageLabelsRA = { "Filed", "Posted", "Decision", "PSA Endorsement" };
        private static readonly string[] StageCodesTrack = { "Filed", "UnderReview", "Decision", "PSA_Endorsement" };
        private static readonly string[] StageLabelsTrack = { "Filed", "Under Review", "Decision", "PSA Endorsement" };

        private static bool IsRa(string typeCode) => typeCode == "RA9048" || typeCode == "RA10172";
        private static string[] StageCodesFor(string typeCode) => IsRa(typeCode) ? StageCodesRA : StageCodesTrack;
        private static string[] StageLabelsFor(string typeCode) => IsRa(typeCode) ? StageLabelsRA : StageLabelsTrack;

        // The fee card (Database/41_fee_schedule_and_payment_log.sql) prices RA 9048 and RA 10172
        // filing - PET-9048-CCE/PET-9048-CFN and PET-10172. The four track-only case types
        // (Legitimation, Supplemental Report, Legal Instrument, Court Order) carry no fee on the
        // card - CROMS "records and tracks" those, it does not assess a charge for them.
        private static bool IsPayable(string typeCode) => IsRa(typeCode);

        // ------------------------------------------------------------ controls (all built in code)
        private DataGridView grid;
        private TextBox txtSearch;
        private ComboBox cboFilterType;
        private Label lblCount;
        private Panel tileTotal, tileReview, tileDecision, tilePsa;
        private Label valTotal, valReview, valDecision, valPsa;

        private Label lblSel;
        private Label lblContextHint;
        private ComboBox cboType;
        private ComboBox cboRecordType;
        private ComboBox cboRecord;
        private DateTimePicker dtpFiled;
        private ComboBox cboStage;
        private Label lblStageInfo;
        private TextBox txtRemarks;
        private Label lblValidation;
        private Button btnAdvance;
        private Button btnDocuments;
        private Label lblFeeStatus;
        private Button btnFee;
        private Button btnAck;
        private Button btnSave;
        private Button btnNew;
        private Button btnDelete;

        public PetitionsForm()
        {
            InitializeComponent();
            BuildLayout();
            UiTheme.Polish(this);
            LoadGrid();
            ClearForm();
        }

        public void RefreshData() => LoadGrid();

        // ================================================================ layout
        private void BuildLayout()
        {
            var titleLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                Location = new Point(32, 24),
                Text = "Petitions & Case Tracking",
                UseMnemonic = false
            };
            var lblSubtitle = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 10F),
                ForeColor = UiTheme.Muted,
                Location = new Point(34, 60),
                Text = "Correction petitions, plus tracked cases: legitimation, supplemental reports, legal instruments, court orders",
                UseMnemonic = false
            };
            Controls.Add(lblSubtitle);
            Controls.Add(titleLabel);

            var root = new TableLayoutPanel
            {
                Location = new Point(24, 96),
                Size = new Size(ClientSize.Width - 48, ClientSize.Height - 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            Controls.Add(root);

            root.Controls.Add(BuildListCard(), 0, 0);
            root.Controls.Add(BuildEditorCard(), 1, 0);
        }

        private Control BuildListCard()
        {
            var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18), BackColor = Color.Transparent };
            card.Controls.Add(body);

            // The grid (Dock=Fill) is added FIRST and the fixed-height header block SECOND —
            // this codebase's own Release & Claim fix (2026-08-04) found that a Fill child
            // added AFTER an edge-docked one claims space before the edge dock reserves its
            // own, so Fill goes first here to avoid the same overlap.
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.CellClick += grid_CellClick;
            grid.CellFormatting += grid_CellFormatting;
            body.Controls.Add(grid);

            var topBlock = new Panel { Dock = DockStyle.Top, Height = 174, Margin = new Padding(0, 0, 0, 8) };
            body.Controls.Add(topBlock);

            var header = new Label
            {
                Text = "Cases",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            topBlock.Controls.Add(header);

            // counts strip
            int tileY = 34, tileW = 0;
            tileTotal = MakeTile("TOTAL CASES", out valTotal);
            tileReview = MakeTile("IN REVIEW / POSTED", out valReview);
            tileDecision = MakeTile("AT DECISION", out valDecision);
            tilePsa = MakeTile("PSA ENDORSED", out valPsa);
            foreach (var t in new[] { tileTotal, tileReview, tileDecision, tilePsa })
            {
                t.Location = new Point(tileW, tileY);
                topBlock.Controls.Add(t);
                tileW += t.Width + 10;
            }

            txtSearch = new TextBox
            {
                Location = new Point(0, 104),
                Width = 300,
                Font = new Font("Segoe UI", 9.75F)
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            var lblSearchHint = MakePlaceholder(txtSearch, "Search by name or case type...");
            topBlock.Controls.Add(txtSearch);
            topBlock.Controls.Add(lblSearchHint);

            cboFilterType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(312, 104), Width = 200 };
            cboFilterType.Items.Add("All case types");
            cboFilterType.Items.AddRange(TypeFilterLabels);
            cboFilterType.SelectedIndex = 0;
            cboFilterType.SelectedIndexChanged += (s, e) => ApplyFilter();
            topBlock.Controls.Add(cboFilterType);

            lblCount = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = UiTheme.Faint,
                Location = new Point(0, 134)
            };
            topBlock.Controls.Add(lblCount);

            return card;
        }

        private Panel MakeTile(string caption, out Label value)
        {
            var p = new Panel { Size = new Size(150, 56), BackColor = UiTheme.PageBg };
            var capL = new Label
            {
                Text = caption,
                AutoSize = false,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = UiTheme.Faint,
                Location = new Point(10, 8),
                Size = new Size(130, 16)
            };
            var valL = new Label
            {
                Text = "0",
                AutoSize = false,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                Location = new Point(10, 24),
                Size = new Size(130, 26)
            };
            p.Controls.Add(capL);
            p.Controls.Add(valL);
            value = valL;
            return p;
        }

        /// <summary>A Label overlaid on a TextBox that hides itself once the box has text or focus
        /// — a lightweight cue text (WinForms TextBox has no PlaceholderText on .NET Framework
        /// 4.8's control, per this project's own 2026-07-14 note).</summary>
        private Label MakePlaceholder(TextBox host, string text)
        {
            var l = new Label
            {
                Text = text,
                AutoSize = false,
                Font = host.Font,
                ForeColor = UiTheme.Faint,
                BackColor = Color.Transparent,
                Location = new Point(host.Left + 4, host.Top + 3),
                Size = new Size(host.Width - 10, host.Height - 4),
                Enabled = false
            };
            host.TextChanged += (s, e) => l.Visible = host.Text.Length == 0;
            host.Enter += (s, e) => l.Visible = false;
            host.Leave += (s, e) => l.Visible = host.Text.Length == 0;
            return l;
        }

        private Control BuildEditorCard()
        {
            // AutoScroll so the fee/acknowledgment row added below the existing fields can never
            // clip off the bottom of a shorter screen — the card scrolls instead of hiding a button.
            var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(20), AutoScroll = true };

            lblSel = new Label
            {
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                AutoSize = true,
                Location = new Point(20, 16),
                Text = "New Petition"
            };
            lblContextHint = new Label
            {
                Font = new Font("Segoe UI", 8.75F),
                ForeColor = UiTheme.Muted,
                AutoSize = false,
                Size = new Size(320, 32),
                Location = new Point(20, 42),
                Text = "Fill in the case details below, then Save."
            };
            card.Controls.Add(lblSel);
            card.Controls.Add(lblContextHint);

            int y = 84;
            cboType = AddField(card, "Case type", ref y, out _);
            cboType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboType.Items.AddRange(new object[]
            {
                "RA 9048 — Clerical Error / Change of First Name",
                "RA 10172 — Day / Month / Sex Correction",
                "RA 9858 — Legitimation (parents married after birth)",
                "Supplemental Report — up to 2 missing entries",
                "Legal Instrument — Acknowledgment / AUSF",
                "Court Order — annotate per final decision"
            });
            cboType.SelectedIndexChanged += cboType_SelectedIndexChanged;

            cboRecordType = AddField(card, "Record type", ref y, out _);
            cboRecordType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboRecordType.Items.AddRange(new object[] { "Birth", "Marriage", "Death" });
            cboRecordType.SelectedIndexChanged += cboRecordType_SelectedIndexChanged;

            cboRecord = AddField(card, "Record", ref y, out _);
            cboRecord.DropDownStyle = ComboBoxStyle.DropDownList;

            dtpFiled = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(20, y + 20), Width = 320 };
            var lblFiled = FieldCaption("Filed date", y);
            card.Controls.Add(lblFiled);
            card.Controls.Add(dtpFiled);
            y += 60;

            var lblStage = FieldCaption("Current stage", y);
            card.Controls.Add(lblStage);
            cboStage = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(20, y + 20), Width = 320, Font = new Font("Segoe UI", 9.75F) };
            cboStage.SelectedIndexChanged += cboStage_SelectedIndexChanged;
            card.Controls.Add(cboStage);
            lblStageInfo = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8F),
                ForeColor = UiTheme.Faint,
                Location = new Point(20, y + 48)
            };
            card.Controls.Add(lblStageInfo);
            y += 70;

            var lblRemarks = FieldCaption("Remarks (optional)", y);
            card.Controls.Add(lblRemarks);
            txtRemarks = new TextBox
            {
                Location = new Point(20, y + 20),
                Width = 320,
                Height = 56,
                Multiline = true,
                Font = new Font("Segoe UI", 9.75F)
            };
            card.Controls.Add(txtRemarks);
            y += 86;

            lblValidation = new Label
            {
                AutoSize = false,
                Visible = false,
                Location = new Point(20, y),
                Size = new Size(320, 36),
                BackColor = UiTheme.DangerTint,
                ForeColor = UiTheme.Danger,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0)
            };
            card.Controls.Add(lblValidation);
            y += 44;

            // Primary next-step action — the single most emphasized control on this card.
            btnAdvance = new Button
            {
                Location = new Point(20, y),
                Size = new Size(320, 46),
                Text = "Advance to next stage",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                BackColor = UiTheme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnAdvance.Click += btnAdvance_Click;
            card.Controls.Add(btnAdvance);
            y += 58;

            // Documents — same requirements-checklist-with-attachment engine the marriage
            // licence and delayed birth registration already use, under owner_type "Petition".
            // Disabled until the case exists (a requirement row needs a real petition id to
            // attach to), same reasoning as Advance being hidden for a new, unsaved case.
            btnDocuments = new Button
            {
                Location = new Point(20, y),
                Size = new Size(320, 38),
                Text = "📎 Case Documents",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = UiTheme.Chrome,
                ForeColor = UiTheme.Ink,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnDocuments.Click += btnDocuments_Click;
            card.Controls.Add(btnDocuments);
            y += 50;

            // Fee & receipt — payable case types (RA 9048 / RA 10172) record their Treasury filing
            // fee here, connected to the same Fees & Payments log every other module writes to
            // (BREQS, the marriage licence). A track-only case type has no fee on the office's card,
            // so it gets a printed Acknowledgment of Submission instead — only one of the two
            // buttons is ever shown for a given case type.
            lblFeeStatus = new Label
            {
                AutoSize = false,
                Location = new Point(20, y),
                Size = new Size(320, 36),
                Font = new Font("Segoe UI", 8.25F),
                ForeColor = UiTheme.Muted
            };
            card.Controls.Add(lblFeeStatus);
            y += 40;

            btnFee = new Button
            {
                Location = new Point(20, y),
                Size = new Size(320, 38),
                Text = "💳 Record Filing Fee",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = UiTheme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnFee.Click += btnFee_Click;
            card.Controls.Add(btnFee);

            btnAck = new Button
            {
                Location = new Point(20, y),
                Size = new Size(320, 38),
                Text = "🖨 Print Acknowledgment",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = UiTheme.Chrome,
                ForeColor = UiTheme.Ink,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnAck.Click += btnAck_Click;
            card.Controls.Add(btnAck);
            y += 50;

            // Secondary actions — visibly lighter weight than Advance.
            btnSave = new Button
            {
                Location = new Point(20, y),
                Size = new Size(150, 38),
                Text = "Save",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = UiTheme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnSave.Click += btnSave_Click;
            card.Controls.Add(btnSave);

            btnNew = new Button
            {
                Location = new Point(190, y),
                Size = new Size(150, 38),
                Text = "Clear / New",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = UiTheme.Chrome,
                ForeColor = UiTheme.Ink,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnNew.Click += btnNew_Click;
            card.Controls.Add(btnNew);
            y += 50;

            // Delete kept visually apart (own row, quieter until hovered) so it can't be
            // mistaken for one of the equal-weight actions above it.
            btnDelete = new Button
            {
                Location = new Point(20, y),
                Size = new Size(150, 34),
                Text = "Delete this case",
                Font = new Font("Segoe UI", 8.75F, FontStyle.Bold),
                BackColor = UiTheme.DangerTint,
                ForeColor = UiTheme.Danger,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnDelete.Click += btnDelete_Click;
            card.Controls.Add(btnDelete);

            return card;
        }

        private Label FieldCaption(string text, int y)
        {
            return new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = UiTheme.Muted,
                Location = new Point(20, y)
            };
        }

        private ComboBox AddField(Control parent, string caption, ref int y, out Label label)
        {
            label = FieldCaption(caption, y);
            parent.Controls.Add(label);
            var combo = new ComboBox { Location = new Point(20, y + 20), Width = 320, Font = new Font("Segoe UI", 9.75F) };
            parent.Controls.Add(combo);
            y += 46;
            return combo;
        }

        // ------------------------------------------------------------ event handlers
        private void cboRecordType_SelectedIndexChanged(object sender, EventArgs e) => LoadRecords();

        /// <summary>Swaps the Stage list to whichever sequence applies to the newly chosen
        /// case type (Posted for RA petitions, Under Review for the track-only types). Fires
        /// from both a user pick and LoadPetition setting cboType.SelectedIndex.</summary>
        private void cboType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboType.SelectedIndex < 0) return;
            RepopulateStage(TypeCodes[cboType.SelectedIndex], 0);
        }

        private void cboStage_SelectedIndexChanged(object sender, EventArgs e) => RefreshStageUi();

        private void RepopulateStage(string typeCode, int index)
        {
            var labels = StageLabelsFor(typeCode);
            cboStage.Items.Clear();
            cboStage.Items.AddRange(labels);
            cboStage.SelectedIndex = Math.Min(Math.Max(index, 0), labels.Length - 1);
            // cboStage.SelectedIndexChanged already calls RefreshStageUi()
        }

        /// <summary>Keeps the "Step X of 4" caption and the primary Advance button's label,
        /// colour and enabled state in sync with whatever stage is currently picked.</summary>
        private void RefreshStageUi()
        {
            if (cboStage.SelectedIndex < 0 || cboStage.Items.Count == 0) return;
            string[] labels = new string[cboStage.Items.Count];
            for (int i = 0; i < labels.Length; i++) labels[i] = cboStage.Items[i].ToString();
            int cur = cboStage.SelectedIndex;
            lblStageInfo.Text = "Step " + (cur + 1) + " of " + labels.Length + " — " + string.Join(" › ", labels);

            RefreshFeeAckUi();

            if (_editingId == null)
            {
                btnAdvance.Visible = false;
                btnDocuments.Enabled = false;
                return;
            }
            btnAdvance.Visible = true;
            btnDocuments.Enabled = true;
            if (cur >= labels.Length - 1)
            {
                btnAdvance.Enabled = false;
                btnAdvance.Text = "✓ Final stage reached";
                btnAdvance.BackColor = UiTheme.SuccessTint;
                btnAdvance.ForeColor = UiTheme.Success;
            }
            else
            {
                btnAdvance.Enabled = true;
                btnAdvance.Text = "Advance to \"" + labels[cur + 1] + "\"";
                btnAdvance.BackColor = UiTheme.Accent;
                btnAdvance.ForeColor = Color.White;
            }
        }

        private void btnSave_Click(object sender, EventArgs e) => Save();
        private void btnAdvance_Click(object sender, EventArgs e) => AdvanceStage();
        private void btnNew_Click(object sender, EventArgs e) => ClearForm();
        private void btnDelete_Click(object sender, EventArgs e) => Delete();

        private void btnDocuments_Click(object sender, EventArgs e)
        {
            if (_editingId == null || cboType.SelectedIndex < 0)
            {
                ShowValidation("Save the case first, then attach its documents.");
                return;
            }
            string typeCode = TypeCodes[cboType.SelectedIndex];
            string recordName = cboRecord.SelectedIndex >= 0 ? cboRecord.Text : "(no record linked)";
            string summary = (cboRecordType.SelectedItem?.ToString() ?? "Record") + ": " + recordName;
            using (var dlg = new PetitionDocumentsForm(_editingId.Value, typeCode,
                TypeFilterLabelFor(typeCode), summary))
            {
                dlg.ShowDialog(this);
            }
        }

        /// <summary>Shows exactly one of Record Filing Fee / Print Acknowledgment, and states the
        /// fee's paid/unpaid status — driven by the SAME fee schedule and payment log every other
        /// module (BREQS, the marriage licence) writes to, never a second one invented here.</summary>
        private void RefreshFeeAckUi()
        {
            bool haveType = cboType.SelectedIndex >= 0;
            bool payable = haveType && IsPayable(TypeCodes[cboType.SelectedIndex]);
            btnFee.Visible = !haveType || payable;
            btnAck.Visible = haveType && !payable;

            if (_editingId == null)
            {
                btnFee.Enabled = false;
                btnAck.Enabled = false;
                lblFeeStatus.Text = "Save the case first to record its filing fee or print an acknowledgment.";
                lblFeeStatus.ForeColor = UiTheme.Faint;
                return;
            }

            if (payable)
            {
                DataRow paid = LoadPetitionPayment(_editingId.Value);
                if (paid == null)
                {
                    btnFee.Enabled = true;
                    btnFee.Text = "💳 Record Filing Fee";
                    lblFeeStatus.Text = "Filing fee not yet paid at the Treasury.";
                    lblFeeStatus.ForeColor = UiTheme.Warning;
                }
                else
                {
                    btnFee.Enabled = false;
                    btnFee.Text = "✓ Filing Fee Paid";
                    lblFeeStatus.Text = "Paid — O.R. " + paid["or_number"] + ", " +
                        PaymentService.Money(Convert.ToDecimal(paid["net_amount"])) + " on " +
                        Convert.ToDateTime(paid["paid_at"]).ToString("MMM d, yyyy") +
                        ". See Fees & Payments → Payment Log for the receipt.";
                    lblFeeStatus.ForeColor = UiTheme.Success;
                }
            }
            else
            {
                btnAck.Enabled = true;
                lblFeeStatus.Text = "This case type has no filing fee on the office's fee schedule — " +
                                     "print an acknowledgment of filing instead.";
                lblFeeStatus.ForeColor = UiTheme.Muted;
            }
        }

        /// <summary>The most recent payment recorded against this petition (source_table
        /// 'petitions'), or null when its filing fee has not been paid yet.</summary>
        private static DataRow LoadPetitionPayment(int petitionId)
        {
            DataTable t = Db.Pull(
                "SELECT p.or_number, p.net_amount, p.paid_at FROM payments p " +
                "WHERE p.source_table = 'petitions' AND p.source_id = @id ORDER BY p.id DESC LIMIT 1",
                new MySqlParameter("@id", petitionId));
            return t.Rows.Count == 0 ? null : t.Rows[0];
        }

        private void btnFee_Click(object sender, EventArgs e)
        {
            if (_editingId == null || cboType.SelectedIndex < 0)
            {
                ShowValidation("Save the case first, then record its filing fee.");
                return;
            }
            string typeCode = TypeCodes[cboType.SelectedIndex];
            string recordName = cboRecord.SelectedIndex >= 0 ? cboRecord.Text : "(no record linked)";
            using (var dlg = new PetitionFeeDialog(typeCode, _editingId.Value, recordName, TypeFilterLabelFor(typeCode)))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshFeeAckUi();
                    MessageBox.Show(this, "Filing fee recorded. It now appears in Fees & Payments → Payment Log, " +
                        "itemised against this case.", "Filing Fee", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>Prints the Acknowledgment of Submission slip for a track-only case type that
        /// carries no filing fee - reuses the SAME slip Birth/Marriage/Death Registration already
        /// print (Data/AcknowledgmentSlip.cs), which already carries the reference no., name, type,
        /// date, status, receiving staff and the "NOT a certificate" disclaimer this asked for.</summary>
        private void btnAck_Click(object sender, EventArgs e)
        {
            if (_editingId == null || cboType.SelectedIndex < 0)
            {
                ShowValidation("Save the case first, then print its acknowledgment.");
                return;
            }
            string typeCode = TypeCodes[cboType.SelectedIndex];
            string recordName = cboRecord.SelectedIndex >= 0 ? cboRecord.Text : "(no record linked)";
            string reference = "PET-" + dtpFiled.Value.Year + "-" + _editingId.Value.ToString("D6");
            string statusLabel = cboStage.SelectedIndex >= 0 ? cboStage.Text : "Filed";
            List<string> docLines = PetitionDocumentService.Requirements(_editingId.Value)
                .Select(r => r.Label + " — " + r.Status).ToList();

            AcknowledgmentSlip.Print(this, reference, recordName,
                TypeFilterLabelFor(typeCode) + " (Case Tracking)", dtpFiled.Value, statusLabel,
                docLines, Session.User != null ? Session.User.FullName : "Front Desk");
        }

        private void ApplyFilter()
        {
            if (_dt == null) return;
            string term = (txtSearch.Text ?? "").Trim().Replace("'", "''");
            string typeText = cboFilterType.SelectedIndex > 0 ? cboFilterType.Text.Replace("'", "''") : null;

            var parts = new List<string>();
            if (term.Length > 0) parts.Add("([Type] LIKE '%" + term + "%' OR [Record] LIKE '%" + term + "%')");
            if (typeText != null) parts.Add("[Type] = '" + typeText + "'");

            _dt.DefaultView.RowFilter = string.Join(" AND ", parts);
            lblCount.Text = _dt.DefaultView.Count + " of " + _dt.Rows.Count + " shown";
        }

        // ---------------------------------------------------------------- data
        private void LoadGrid()
        {
            _dt = Db.Pull(
                "SELECT p.id, " +
                "CASE p.petition_type " +
                "  WHEN 'RA9048' THEN 'RA 9048' WHEN 'RA10172' THEN 'RA 10172' " +
                "  WHEN 'Legitimation' THEN 'Legitimation (RA 9858)' " +
                "  WHEN 'SupplementalReport' THEN 'Supplemental Report' " +
                "  WHEN 'LegalInstrument' THEN 'Legal Instrument' " +
                "  WHEN 'CourtOrder' THEN 'Court Order' " +
                "  ELSE p.petition_type END AS Type, " +
                "CASE p.record_type " +
                "  WHEN 'Birth'    THEN (SELECT TRIM(CONCAT(last_name,', ',first_name)) FROM births b WHERE b.id = p.record_id) " +
                "  WHEN 'Death'    THEN (SELECT full_name FROM deaths d WHERE d.id = p.record_id) " +
                "  WHEN 'Marriage' THEN (SELECT TRIM(CONCAT(husband_last_name,' & ',wife_last_name)) FROM marriages m WHERE m.id = p.record_id) " +
                "  ELSE '—' END AS Record, " +
                "CASE p.stage " +
                "  WHEN 'UnderReview' THEN 'Under Review' WHEN 'PSA_Endorsement' THEN 'PSA Endorsement' " +
                "  ELSE p.stage END AS Stage, " +
                "p.filed_date AS Filed, p.remarks AS Remarks " +
                "FROM petitions p ORDER BY p.id DESC");

            grid.DataSource = _dt.DefaultView;
            if (grid.Columns.Contains("id")) grid.Columns["id"].Visible = false;
            if (grid.Columns.Contains("Remarks")) grid.Columns["Remarks"].Visible = false;

            UpdateTiles();
            ApplyFilter();
        }

        private void UpdateTiles()
        {
            int total = _dt.Rows.Count, review = 0, decision = 0, psa = 0;
            foreach (DataRow r in _dt.Rows)
            {
                string stage = r["Stage"] as string ?? "";
                if (stage == "Filed") continue;
                else if (stage == "Posted" || stage == "Under Review") review++;
                else if (stage == "Decision") decision++;
                else if (stage == "PSA Endorsement") psa++;
            }
            valTotal.Text = total.ToString();
            valReview.Text = review.ToString();
            valDecision.Text = decision.ToString();
            valPsa.Text = psa.ToString();
        }

        private void grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (grid.Columns[e.ColumnIndex].Name != "Stage" || e.Value == null) return;
            string stage = e.Value.ToString();
            Color tint, ink;
            switch (stage)
            {
                case "Filed": tint = UiTheme.AccentTint; ink = UiTheme.Accent; break;
                case "Posted":
                case "Under Review": tint = UiTheme.WarningTint; ink = UiTheme.Warning; break;
                case "PSA Endorsement": tint = UiTheme.SuccessTint; ink = UiTheme.Success; break;
                default: tint = UiTheme.Chrome; ink = UiTheme.Ink; break; // Decision
            }
            e.CellStyle.BackColor = tint;
            e.CellStyle.ForeColor = ink;
            e.CellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }

        /// <summary>Fills the Record dropdown with records of the chosen type.</summary>
        private void LoadRecords()
        {
            string type = cboRecordType.SelectedItem?.ToString();
            string sql;
            switch (type)
            {
                case "Birth":
                    sql = "SELECT id, TRIM(CONCAT(last_name, ', ', first_name)) AS name FROM births ORDER BY last_name"; break;
                case "Death":
                    sql = "SELECT id, full_name AS name FROM deaths ORDER BY full_name"; break;
                case "Marriage":
                    sql = "SELECT id, TRIM(CONCAT(husband_last_name, ' & ', wife_last_name)) AS name FROM marriages ORDER BY id DESC"; break;
                default:
                    cboRecord.DataSource = null; return;
            }
            DataTable dt = Db.Pull(sql);
            cboRecord.DataSource = dt;
            cboRecord.DisplayMember = "name";
            cboRecord.ValueMember = "id";
            cboRecord.SelectedIndex = -1;
        }

        private void grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            object idCell = grid.Rows[e.RowIndex].Cells["id"].Value;
            if (idCell == null || idCell == DBNull.Value) return;
            LoadPetition(Convert.ToInt32(idCell));
        }

        private void LoadPetition(int id)
        {
            DataTable dt = Db.Pull("SELECT * FROM petitions WHERE id = " + id);
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            _editingId = id;

            string typeCode = Str(r["petition_type"]);
            cboType.SelectedIndex = Array.IndexOf(TypeCodes, typeCode);   // triggers RepopulateStage(...,0)
            cboRecordType.SelectedItem = Str(r["record_type"]);            // triggers LoadRecords
            if (r["record_id"] != DBNull.Value) cboRecord.SelectedValue = Convert.ToInt32(r["record_id"]);
            RepopulateStage(typeCode, Math.Max(0, Array.IndexOf(StageCodesFor(typeCode), Str(r["stage"]))));
            if (r["filed_date"] != DBNull.Value) dtpFiled.Value = Convert.ToDateTime(r["filed_date"]);
            txtRemarks.Text = Str(r["remarks"]);
            HideValidation();

            string label = TypeFilterLabelFor(typeCode);
            lblSel.Text = "Editing Case #" + id;
            lblContextHint.Text = label + " — filed " + dtpFiled.Value.ToString("MMM d, yyyy") + ". Advance it below, or edit and Save.";
            RefreshStageUi();
        }

        private static string TypeFilterLabelFor(string typeCode)
        {
            int i = Array.IndexOf(TypeCodes, typeCode);
            return i >= 0 ? TypeFilterLabels[i] : typeCode;
        }

        private void ShowValidation(string message)
        {
            lblValidation.Text = "⚠ " + message;
            lblValidation.Visible = true;
        }

        private void HideValidation() => lblValidation.Visible = false;

        private void Save()
        {
            if (cboType.SelectedIndex < 0 || cboRecordType.SelectedIndex < 0)
            {
                ShowValidation("Choose a case type and a record type before saving.");
                return;
            }
            HideValidation();

            string typeCode = TypeCodes[cboType.SelectedIndex];
            object recordId = cboRecord.SelectedValue is int rid ? (object)rid : DBNull.Value;
            var ps = new[]
            {
                new MySqlParameter("@pt", typeCode),
                new MySqlParameter("@rt", cboRecordType.SelectedItem.ToString()),
                new MySqlParameter("@rid", recordId),
                new MySqlParameter("@stage", StageCodesFor(typeCode)[Math.Max(0, cboStage.SelectedIndex)]),
                new MySqlParameter("@filed", dtpFiled.Value.Date),
                new MySqlParameter("@remarks", string.IsNullOrWhiteSpace(txtRemarks.Text)
                    ? (object)DBNull.Value : txtRemarks.Text.Trim()),
            };
            try
            {
                if (_editingId == null)
                {
                    Db.Push("INSERT INTO petitions (petition_type, record_type, record_id, stage, filed_date, remarks) " +
                            "VALUES (@pt, @rt, @rid, @stage, @filed, @remarks)", ps);
                    Audit.Write(Audit.Create, "petitions", null, typeCode + " on " + cboRecordType.SelectedItem);
                }
                else
                {
                    var up = new List<MySqlParameter>(ps) { new MySqlParameter("@id", _editingId.Value) };
                    Db.Push("UPDATE petitions SET petition_type=@pt, record_type=@rt, record_id=@rid, " +
                            "stage=@stage, filed_date=@filed, remarks=@remarks WHERE id=@id", up.ToArray());
                    Audit.Write(Audit.Update, "petitions", _editingId.Value, null);
                }
                ClearForm();
                LoadGrid();
            }
            catch (Exception ex) { Fail(ex); }
        }

        /// <summary>Moves the selected case to the next stage of ITS OWN sequence (RA petitions
        /// go through Posted; the four track-only case types go through Under Review instead).</summary>
        private void AdvanceStage()
        {
            if (_editingId == null || cboType.SelectedIndex < 0) return;
            string typeCode = TypeCodes[cboType.SelectedIndex];
            string[] codes = StageCodesFor(typeCode);
            string[] labels = StageLabelsFor(typeCode);
            int cur = Math.Max(0, cboStage.SelectedIndex);
            if (cur >= codes.Length - 1) return; // Advance is already disabled at this point

            string next = codes[cur + 1];
            try
            {
                Db.Push("UPDATE petitions SET stage=@s WHERE id=@id",
                    new MySqlParameter("@s", next),
                    new MySqlParameter("@id", _editingId.Value));
                Audit.Write(Audit.Update, "petitions", _editingId.Value, "Stage → " + labels[cur + 1]);
                cboStage.SelectedIndex = cur + 1; // fires cboStage_SelectedIndexChanged -> RefreshStageUi
                LoadGrid();
                if (_editingId.HasValue)
                    lblContextHint.Text = TypeFilterLabelFor(typeCode) + " — now at " + labels[cur + 1] + ".";
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void Delete()
        {
            if (_editingId == null)
            {
                ShowValidation("Pick a case from the list first, then Delete.");
                return;
            }
            if (MessageBox.Show("Delete this case? This cannot be undone.", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                Db.Push("DELETE FROM petitions WHERE id=@id", new MySqlParameter("@id", _editingId.Value));
                Audit.Write(Audit.Delete, "petitions", _editingId.Value, null);
                ClearForm();
                LoadGrid();
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void ClearForm()
        {
            _editingId = null;
            lblSel.Text = "New Petition";
            lblContextHint.Text = "Fill in the case details below, then Save.";
            cboType.SelectedIndex = -1;
            cboRecordType.SelectedIndex = -1;
            cboRecord.DataSource = null;
            RepopulateStage("RA9048", 0);   // neutral default list until a type is chosen
            dtpFiled.Value = DateTime.Today;
            txtRemarks.Clear();
            HideValidation();
            RefreshStageUi(); // hides Advance — nothing to advance until this is saved
        }

        private static string Str(object v) => v == null || v == DBNull.Value ? "" : v.ToString();
        private static void Fail(Exception ex) =>
            MessageBox.Show("Operation failed: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>
    /// Records a petition's Treasury filing fee against the same fee schedule and payment log
    /// every other module writes to (Data/PaymentService.cs) - the same reuse this project already
    /// applies for BREQS and the marriage licence (Forms/BreqsDialogs.cs, MarriageLicenseForm.cs).
    /// CROMS does not collect money here; it records the Official Receipt the Treasury issued.
    /// RA 9048 prices two different filings under one petition type (PET-9048-CCE / PET-9048-CFN),
    /// so it alone gets a Filing Type picker; RA 10172 has one fixed fee (PET-10172).
    /// </summary>
    internal sealed class PetitionFeeDialog : Form
    {
        private readonly string _typeCode;
        private readonly int _petitionId;
        private readonly string _payerName;
        private readonly string _caseLabel;

        private ComboBox _cboFilingType;
        private Label _lblFeeDesc;
        private TextBox _txtOr;
        private DateTimePicker _dtpPaid;
        private TextBox _txtAmount;

        private static readonly string[] Ra9048Codes = { "PET-9048-CCE", "PET-9048-CFN" };
        private static readonly string[] Ra9048Labels =
            { "Clerical Error Correction (CCE)", "Change of First Name (CFN)" };

        public PetitionFeeDialog(string typeCode, int petitionId, string payerName, string caseLabel)
        {
            _typeCode = typeCode; _petitionId = petitionId; _payerName = payerName; _caseLabel = caseLabel;

            Text = "Record Filing Fee";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false; MaximizeBox = false;
            BackColor = Color.White;
            ClientSize = new Size(420, typeCode == "RA9048" ? 458 : 414);

            int y = 20;
            Controls.Add(Info("Filed under: " + caseLabel +
                (string.IsNullOrWhiteSpace(payerName) ? "" : "\n" + payerName), y, 44));
            y += 52;

            if (typeCode == "RA9048")
            {
                Controls.Add(Cap("Filing Type", y)); y += 24;
                _cboFilingType = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(20, y), Width = 380, Font = new Font("Segoe UI", 9.75F)
                };
                _cboFilingType.Items.AddRange(Ra9048Labels);
                _cboFilingType.SelectedIndexChanged += (s, e) => UpdateFee();
                Controls.Add(_cboFilingType);
                y += 44;
            }

            _lblFeeDesc = new Label
            {
                AutoSize = false, Location = new Point(20, y), Size = new Size(380, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = UiTheme.Accent
            };
            Controls.Add(_lblFeeDesc);
            y += 32;

            Controls.Add(Cap("Treasury Official Receipt No.", y)); y += 24;
            _txtOr = Field(y); Controls.Add(_txtOr); y += 40;

            Controls.Add(Cap("Date Paid", y)); y += 24;
            _dtpPaid = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short, MaxDate = DateTime.Today, Value = DateTime.Today,
                Location = new Point(20, y), Width = 380
            };
            Controls.Add(_dtpPaid); y += 44;

            Controls.Add(Cap("Amount (PHP)", y)); y += 24;
            _txtAmount = Field(y); Controls.Add(_txtAmount); y += 44;

            var record = new Button
            {
                Text = "Record Payment", Location = new Point(20, y), Size = new Size(180, 42),
                FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = UiTheme.Success,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold), UseVisualStyleBackColor = false
            };
            record.Click += Record_Click;
            var cancel = new Button
            {
                Text = "Cancel", Location = new Point(216, y), Size = new Size(184, 42),
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel,
                Font = new Font("Segoe UI", 9.75F)
            };
            Controls.Add(record); Controls.Add(cancel);
            AcceptButton = record; CancelButton = cancel;

            if (_cboFilingType != null) _cboFilingType.SelectedIndex = 0; else UpdateFee();
        }

        private string CurrentFeeCode() =>
            _typeCode == "RA9048" ? Ra9048Codes[Math.Max(0, _cboFilingType.SelectedIndex)] : "PET-10172";

        private void UpdateFee()
        {
            FeeItem f = PaymentService.Fee(CurrentFeeCode());
            _lblFeeDesc.Text = f != null ? f.Description + "  —  " + PaymentService.Money(f.Amount)
                                          : "Fee not found on the schedule.";
            _txtAmount.Text = f != null && f.Amount.HasValue ? f.Amount.Value.ToString("0.00") : "";
        }

        private void Record_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtOr.Text))
            {
                MessageBox.Show(this, "Enter the Treasury official receipt number.", "Filing Fee",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            decimal amt;
            if (!decimal.TryParse(_txtAmount.Text.Trim(), out amt) || amt <= 0)
            {
                MessageBox.Show(this, "Enter the amount on the receipt.", "Filing Fee",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string code = CurrentFeeCode();
            FeeItem f = PaymentService.Fee(code);
            try
            {
                // Checked BEFORE the payment is written, so a receipt already used elsewhere is
                // refused rather than silently double-counting a Treasury collection.
                PaymentService.EnsureOrFree(_txtOr.Text, "petitions", _petitionId);
                PaymentService.RecordForModule(new PaymentEntry
                {
                    Source = PaymentService.SourcePetition, SourceTable = "petitions", SourceId = _petitionId,
                    PayerName = _payerName, Purpose = "Petition filing - " + _caseLabel,
                    OrNumber = _txtOr.Text, PaidAt = _dtpPaid.Value.Date,
                    Lines = { new PaymentLine
                    {
                        FeeCode = code, Description = f != null ? f.Description : code,
                        Quantity = 1, UnitAmount = amt
                    } }
                }, Session.User == null ? (int?)null : Session.User.Id);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Filing Fee", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Label Cap(string t, int y) => new Label
        {
            Text = t, AutoSize = true, Location = new Point(20, y),
            Font = new Font("Segoe UI", 9F), ForeColor = UiTheme.Muted
        };
        private static Label Info(string t, int y, int h) => new Label
        {
            Text = t, AutoSize = false, Location = new Point(20, y), Size = new Size(380, h),
            Font = new Font("Segoe UI", 8.75F), ForeColor = UiTheme.Faint
        };
        private static TextBox Field(int y) => new TextBox
        {
            Location = new Point(20, y), Size = new Size(380, 26), Font = new Font("Segoe UI", 9.75F)
        };
    }
}
