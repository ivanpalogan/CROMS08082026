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
    internal sealed partial class PetitionDocumentsForm : Form
    {
        private readonly int _petitionId;
        private readonly string _petitionTypeCode;

        public PetitionDocumentsForm(int petitionId, string petitionTypeCode, string petitionTypeLabel, string caseSummary)
        {
            _petitionId = petitionId;
            _petitionTypeCode = petitionTypeCode;
            InitializeComponent();

            _lblWho.Text = string.IsNullOrWhiteSpace(caseSummary) ? "(unnamed case)" : caseSummary;
            _lblWhen.Text = petitionTypeLabel + " - Case #" + petitionId;
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
