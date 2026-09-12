namespace CROMS.Data
{
    /// <summary>
    /// Government-issued IDs commonly accepted for identity verification across Philippine
    /// government offices (PSA / LGU front desks). One list for every screen that asks for a
    /// valid ID, so Release &amp; Claim and BREQS offer the same choices. Combos using it are
    /// editable, so an ID not on the list can still be typed, and "Other" is provided.
    /// (The kiosk is a separate application and keeps its own copy in KioskCore.IdTypes.)
    /// </summary>
    public static class GovIds
    {
        public static readonly string[] All =
        {
            "Philippine National ID (PhilSys)",
            "Philippine Passport (DFA)",
            "Driver's License (LTO)",
            "UMID (Unified Multi-Purpose ID)",
            "SSS ID",
            "GSIS eCard",
            "PRC ID (Professional License)",
            "Voter's ID / COMELEC Certification",
            "Postal ID (PHLPost)",
            "PhilHealth ID",
            "TIN ID (BIR)",
            "Pag-IBIG Loyalty Card Plus",
            "Senior Citizen ID (OSCA)",
            "PWD ID",
            "Solo Parent ID",
            "Barangay ID / Certification (with photo)",
            "NBI Clearance",
            "Police Clearance",
            "OWWA ID / iDOLE",
            "Seafarer's Record Book (SIRB)",
            "IBP ID (Integrated Bar of the Philippines)",
            "AFP / PVAO ID",
            "Company / School ID",
            "Other"
        };
    }
}
