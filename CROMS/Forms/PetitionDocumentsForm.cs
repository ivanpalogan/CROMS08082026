using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Supporting documents for one petition/case - reuses the marriage licence's requirements
    /// engine and grid unchanged (RequirementsGrid, MarriageService.Requirements/SaveRequirement
    /// are already generic on owner type), the same way DelayedBirthCaseForm reused it for a
    /// delayed birth registration. Self-contained code-built dialog, same in-file pattern as
    /// MarriageLicenseForm / DelayedBirthCaseForm.
    /// </summary>
    internal sealed class PetitionDocumentsForm : Form
    {
        private readonly int _petitionId;
        private readonly string _petitionTypeCode;

        private readonly Label _lblWho = MUi.Txt("", 12F, FontStyle.Bold);
        private readonly Label _lblWhen = MUi.Txt("", 9F, FontStyle.Regular, UiTheme.Muted);
        private readonly Banner _banner = new Banner();
        private readonly RequirementsGrid _grid = new RequirementsGrid();
        private readonly Button _btnClose = MUi.Btn("Close", MUi.Kind.Ghost, 90);
        private readonly Panel _root;

        public PetitionDocumentsForm(int petitionId, string petitionTypeCode, string petitionTypeLabel, string caseSummary)
        {
            _petitionId = petitionId;
            _petitionTypeCode = petitionTypeCode;
            Text = "Case Documents"; StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 620); MinimumSize = new Size(620, 480);
            BackColor = UiTheme.Surface; ShowIcon = false; MaximizeBox = false;

            _root = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20), BackColor = UiTheme.Surface };
            Panel root = _root;
            _lblWho.Dock = DockStyle.Top; _lblWho.AutoSize = false; _lblWho.Height = 26; _lblWho.UseMnemonic = false;
            _lblWho.Text = string.IsNullOrWhiteSpace(caseSummary) ? "(unnamed case)" : caseSummary;
            _lblWhen.Dock = DockStyle.Top; _lblWhen.AutoSize = false; _lblWhen.Height = 20; _lblWhen.Margin = new Padding(0, 0, 0, 8);
            _lblWhen.Text = petitionTypeLabel + " - Case #" + petitionId;

            var reqHead = MUi.Txt("SUPPORTING DOCUMENTS", 9F, FontStyle.Bold, UiTheme.Muted);
            reqHead.Dock = DockStyle.Top; reqHead.Height = 22; reqHead.BackColor = Color.Transparent;
            _grid.Dock = DockStyle.Top; _grid.Margin = new Padding(0, 0, 0, 10);
            // Only Court Order and the generic "any case type" slot have an office-confirmed
            // checklist today (migration 52) - every other type has no named documents yet, so
            // letting the case editor add one of its own is what keeps the screen useful for
            // them rather than showing an empty grid with no way to record anything.
            _grid.AllowAddCustom = true;
            _grid.Changed += () => Refresh_();

            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0) };
            footer.Controls.Add(_btnClose);

            var parts = new Control[] { _lblWho, _lblWhen, _banner, reqHead, _grid };
            // Same ordering fix this codebase has needed twice before (MF-90 2026-09-13, the
            // delayed-birth case form): add in reverse for correct Dock=Top stacking, then state
            // TabIndex explicitly in visual order so Tab/AutoScroll-into-view doesn't open the
            // dialog scrolled past the top.
            for (int i = parts.Length - 1; i >= 0; i--) root.Controls.Add(parts[i]);
            for (int i = 0; i < parts.Length; i++) parts[i].TabIndex = i;
            Controls.Add(root); Controls.Add(footer);

            _btnClose.Click += (s, e) => Close();
            Load += (s, e) => { RefreshRequirements(); _root.AutoScrollPosition = new Point(0, 0); };

            // Modal dialog, not a MainForm module - never passed through ShowModule's polish
            // pass, so it styles itself (same as every other stand-alone dialog in Forms/).
            UiTheme.Polish(this);
        }

        private void RefreshRequirements()
        {
            try
            {
                PetitionDocumentService.SyncRequirements(_petitionTypeCode, _petitionId);
                var needs = PetitionRules.Needs(_petitionTypeCode, PetitionDocumentService.Catalog());
                _grid.Bind(PetitionDocumentService.Owner, _petitionId, needs);
                Refresh_();
            }
            catch (Exception ex) { MUi.Fail(this, ex); }
        }

        private void Refresh_()
        {
            List<ReqRow> rows = PetitionDocumentService.Requirements(_petitionId);
            List<ReqType> catalog = PetitionDocumentService.Catalog();
            List<Need> needs = PetitionRules.Needs(_petitionTypeCode, catalog);
            List<string> outstanding = PetitionRules.OutstandingItems(needs, rows);

            if (needs.Count == 0)
            {
                _banner.Set(RuleSeverity.Info, "No document checklist is defined for this case type yet.",
                    "Use \"Add a document\" below to record anything filed for this case.", true);
            }
            else if (outstanding.Count == 0)
            {
                _banner.Set(RuleSeverity.Info, "Every required document is on file.",
                    "This is what the checklist says - whether the case is ready to advance stays the registrar's own call.", true);
            }
            else
            {
                _banner.Set(RuleSeverity.Warning, outstanding.Count + " required document(s) still outstanding.",
                    string.Join(" ", outstanding), false);
            }
        }
    }
}
