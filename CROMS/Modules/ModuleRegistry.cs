using System.Collections.Generic;
using CROMS.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// Central list of every module CROMS exposes, in sidebar order and grouped by the
    /// office's own workflow (what a client visit actually goes through) rather than by
    /// the database tables underneath. The MainForm builds its navigation from this list,
    /// so adding a module here is all that is needed to wire it up.
    ///
    /// Administration tools (Master Files, Users &amp; Access, Certificate Templates) are
    /// still registered here — cross-module hand-offs and ModuleTitle() resolve through
    /// this list — but they no longer carry their own sidebar button: they are pages of
    /// the Settings screen. Settings itself is Admin-only (MainForm.AllowedKeys), so
    /// hosting them there does not widen anybody's access.
    /// </summary>
    public static class ModuleRegistry
    {
        // Group labels shown as sidebar section headers.
        public const string GroupDashboard = "Dashboard";
        public const string GroupTransactions = "Transactions";
        public const string GroupCivilRegistration = "Civil Registration";
        public const string GroupPetitions = "Petitions & Cases";
        public const string GroupRecords = "Records & Documents";
        public const string GroupReports = "Reports";
        public const string GroupSystem = "System";

        public static IReadOnlyList<ModuleInfo> All { get; } = new List<ModuleInfo>
        {
            new ModuleInfo("dashboard", "Dashboard", GroupDashboard,
                () => new DashboardForm()),

            // Transactions — the front desk, in the order a client passes through it:
            // take a number, lodge the request, submit it to PSA if it is a BREQS copy,
            // pay, collect.
            new ModuleInfo("queue", "Queue Management", GroupTransactions,
                () => new QueueManagementForm()),
            new ModuleInfo("certrequest", "Certificate Request", GroupTransactions,
                () => new CertificateRequestForm()),
            // PSA-issued copies requested through BREQS: logged here or at the kiosk, submitted
            // to PSA, collected, scanned through OCR and released.
            new ModuleInfo("breqs", "PSA Copies (BREQS)", GroupTransactions,
                () => new BreqsForm()),
            new ModuleInfo("release", "Release & Claim", GroupTransactions,
                () => new ReleaseClaimForm()),
            new ModuleInfo("fees", "Fees & Payments", GroupTransactions,
                () => new FeesPaymentsForm()),

            // Civil Registration — registering the EVENT itself (Municipal Forms 102/97/103).
            // Deliberately separate from the certification counter above: these create the
            // registry record, they do not issue a copy of one.
            new ModuleInfo("birth", "Birth Registration", GroupCivilRegistration,
                () => new BirthRegistrationForm()),
            new ModuleInfo("marriage", "Marriage Registration", GroupCivilRegistration,
                () => new MarriageRegistrationForm()),
            new ModuleInfo("death", "Death Registration", GroupCivilRegistration,
                () => new DeathRegistrationForm()),

            // Petitions & Cases — one screen for every post-registration case type
            // (RA 9048 / RA 10172 correction, legitimation, supplemental report, legal
            // instrument, court order). The case type is picked inside the screen, which
            // is why there is one button here and not six.
            new ModuleInfo("petitions", "Petition & Case Tracking", GroupPetitions,
                () => new PetitionsForm()),

            // Records & Documents — finding and handling records that already exist.
            // Record Search is the SINGLE place a record is found, whichever register it is
            // in, and it now carries each hit's registry book identity (book volume, page,
            // registry number/year, date of registration) beside the record itself -- which
            // is what the separate Registry Books screen used to be opened for.
            new ModuleInfo("search", "Record Search", GroupRecords,
                () => new RecordSearchForm()),
            // Admin-only browser over every saved record/form/image in the system.
            // Not in OperationalKeys in MainForm, so only Admin sees the button.
            new ModuleInfo("archive", "Records Archive", GroupRecords,
                () => new RecordsArchiveForm()),
            new ModuleInfo("ocr", "Document Processing", GroupRecords,
                () => new OcrDigitizationForm()),

            // Reports — reading what the office has done, never changing it.
            // Reports & Analytics: six tabbed domains. The PSA / statutory report is
            // rehosted unchanged inside its own tab (ReportsPsaForm), so statutory output
            // stays separate from analytical output.
            new ModuleInfo("reports", "Reports & Analytics", GroupReports,
                () => new ReportsAnalyticsForm()),
            // Read-only master ledger of every client visit — a history list, not a desk.
            new ModuleInfo("transactions", "Transaction History", GroupReports,
                () => new TransactionsForm()),

            // System
            new ModuleInfo("settings", "Settings", GroupSystem,
                () => new SettingsForm()),

            // ---- Registered but not on the sidebar ----
            // Registry Books: the shelf view of the paper books (one row per volume with its
            // record and page counts). Folded into Record Search, so it no longer carries a
            // sidebar button -- but the screen, the book/page columns and every record on
            // them are untouched, and the key stays registered so ModuleTitle() and any
            // cross-module GoToModule("books") still resolve.
            new ModuleInfo("books", "Registry Books", GroupRecords,
                () => new RegistryBooksForm()),

            // ---- Pages of the Settings screen (see class note) ----
            new ModuleInfo("masterfiles", "Master Files", GroupSystem,
                () => new MasterFilesForm()),
            // Visual, non-technical editor for how a certificate PRINTS — logo/text/field
            // position, font, lines. Never touches a civil registry record's own data.
            new ModuleInfo("certtemplates", "Certificate Templates", GroupSystem,
                () => new TemplateManagementForm()),
            new ModuleInfo("users", "Users & Access", GroupSystem,
                () => new UsersAuditForm()),
        };
    }
}
