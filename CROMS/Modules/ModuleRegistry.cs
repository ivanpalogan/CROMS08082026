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
        public const string GroupCertification = "Certification";
        public const string GroupRecordManagement = "Petitions & Search";
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

            // Certification — register the event, then issue/release its certificate.
            // Birth/Marriage/Death are the source records; Certificate Request/Release &
            // Claim are how a copy of one of those records is issued to a client. One
            // pipeline, one group.
            new ModuleInfo("certrequest", "Certificate Request", GroupCertification,
                () => new CertificateRequestForm()),
            new ModuleInfo("release", "Release & Claim", GroupCertification,
                () => new ReleaseClaimForm()),
            // PSA-issued copies requested through BREQS: logged here or at the kiosk, submitted
            // to PSA, collected, scanned through OCR and released.
            new ModuleInfo("breqs", "PSA Copies (BREQS)", GroupCertification,
                () => new BreqsForm()),
            new ModuleInfo("birth", "Birth Registration", GroupCertification,
                () => new BirthRegistrationForm()),
            new ModuleInfo("marriage", "Marriage Registration", GroupCertification,
                () => new MarriageRegistrationForm()),
            new ModuleInfo("death", "Death Registration", GroupCertification,
                () => new DeathRegistrationForm()),

            // Petitions & Search — post-registration correction (RA 9048 / RA 10172 /
            // RA 9255 legitimation) and cross-record lookup. Separate from Certification:
            // this group AMENDS a record already registered, it doesn't create/issue one.
            new ModuleInfo("petitions", "Petitions", GroupRecordManagement,
                () => new PetitionsForm()),
            new ModuleInfo("books", "Registry Books", GroupRecordManagement,
                () => new RegistryBooksForm()),
            new ModuleInfo("search", "Record Search", GroupRecordManagement,
                () => new RecordSearchForm()),

            // Document Workflow
            new ModuleInfo("ocr", "Intelligent Document Processing", GroupDocumentWorkflow,
                () => new OcrDigitizationForm()),

            // Operations
            new ModuleInfo("fees", "Fees & Payments", GroupOperations,
                () => new FeesPaymentsForm()),
            // Reports & Analytics: six tabbed domains. The PSA / statutory report is
            // rehosted unchanged inside its own tab (ReportsPsaForm), so statutory output
            // stays separate from analytical output.
            new ModuleInfo("reports", "Reports & Analytics", GroupOperations,
                () => new ReportsAnalyticsForm()),

            // Administration
            new ModuleInfo("masterfiles", "Master Files", GroupAdministration,
                () => new MasterFilesForm()),
            new ModuleInfo("settings", "Settings", GroupAdministration,
                () => new SettingsForm()),
            new ModuleInfo("users", "Users & Audit Trail", GroupAdministration,
                () => new UsersAuditForm()),
            // Admin-only browser over every saved record/form/image in the system,
            // grouped by type (birth/marriage/death/petitions/court order/etc).
            // Not in any role's AllowedKeys list in MainForm, so only Admin sees it.
            new ModuleInfo("archive", "Records Archive", GroupAdministration,
                () => new RecordsArchiveForm()),
        };
    }
}
