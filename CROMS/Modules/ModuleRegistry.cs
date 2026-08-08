using System.Collections.Generic;
using CROMS.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// Central list of every module CROMS exposes, in sidebar order and grouped
    /// exactly as documented in CLAUDE.md. The MainForm builds its navigation
    /// from this list, so adding a module here is all that is needed to wire it up.
    /// </summary>
    public static class ModuleRegistry
    {
        // Group labels shown as sidebar section headers.
        public const string GroupClientServices = "Client Services";
        public const string GroupRecordManagement = "Record Management";
        public const string GroupDocumentWorkflow = "Document Workflow";
        public const string GroupOperations = "Operations";
        public const string GroupAdministration = "Administration";

        public static IReadOnlyList<ModuleInfo> All { get; } = new List<ModuleInfo>
        {
            // Client Services
            new ModuleInfo("dashboard", "Dashboard", GroupClientServices,
                () => new DashboardForm()),
            new ModuleInfo("queue", "Queue Management", GroupClientServices,
                () => new QueueManagementForm()),
            new ModuleInfo("transactions", "Transactions", GroupClientServices,
                () => new TransactionsForm()),
            new ModuleInfo("certrequest", "Certificate Request", GroupClientServices,
                () => new CertificateRequestForm()),
            new ModuleInfo("release", "Release & Claim", GroupClientServices,
                () => new ReleaseClaimForm()),

            // Record Management
            new ModuleInfo("birth", "Birth Registration", GroupRecordManagement,
                () => new BirthRegistrationForm()),
            new ModuleInfo("marriage", "Marriage Registration", GroupRecordManagement,
                () => new MarriageRegistrationForm()),
            new ModuleInfo("death", "Death Registration", GroupRecordManagement,
                () => new DeathRegistrationForm()),
            new ModuleInfo("petitions", "Petitions", GroupRecordManagement,
                () => new PetitionsForm()),
            new ModuleInfo("search", "Record Search", GroupRecordManagement,
                () => new RecordSearchForm()),

            // Document Workflow
            new ModuleInfo("ocr", "OCR Digitization", GroupDocumentWorkflow,
                () => new OcrDigitizationForm()),
            new ModuleInfo("docai", "Document AI", GroupDocumentWorkflow,
                () => new DocumentAiForm()),

            // Operations
            new ModuleInfo("fees", "Fees & Payments", GroupOperations,
                () => new FeesPaymentsForm()),
            new ModuleInfo("reports", "Reports & PSA", GroupOperations,
                () => new ReportsPsaForm()),

            // Administration
            new ModuleInfo("masterfiles", "Master Files", GroupAdministration,
                () => new MasterFilesForm()),
            new ModuleInfo("settings", "Settings", GroupAdministration,
                () => new SettingsForm()),
            new ModuleInfo("users", "Users & Audit Trail", GroupAdministration,
                () => new UsersAuditForm()),
        };
    }
}
