using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace CROMS.Data
{
    // =====================================================================
    // The marriage rule engine. PURE: no UI, no database writes, so every rule
    // can be exercised by CROMS.MarriageTest without a screen and every screen
    // states the same rule the same way.
    //
    // PRINCIPLE (kept deliberately): CROMS says what the RECORD does and does not
    // contain - "consent missing", "licence expired on 06 Oct 2026", "applicants
    // do not match". It never pronounces a marriage lawful or unlawful. The one
    // exception is the statutory hard stop of RA 11596 (under 18), which the law
    // itself makes absolute.
    //
    // Sources checked for every rule below:
    //   Family Code Arts. 5, 12-18, 20, 21, 23, 27-34; RA 11596 (2021);
    //   RA 10354 s.15; PSA/NSO AO 1 s.1993 (delayed registration of marriage).
    // Every NUMBER comes from app_settings (MarriageSettings) so the office can
    // correct its own reading of an age band or a period without a rebuild.
    // =====================================================================

    /// <summary>Statutory numbers, read from <c>app_settings</c> with the defaults below.</summary>
    public sealed class MarriageSettings
    {
        public int PostingDays = 10;          // FC Art. 17
        public int ValidityDays = 120;        // FC Art. 20
        public int ExpiringSoonDays = 30;
        public int ConsentAgeFrom = 18, ConsentAgeTo = 20;   // FC Art. 14
        public int AdviceAgeFrom = 21, AdviceAgeTo = 25;     // FC Art. 15 - 21-25 confirmed by the LCRO 2026-09-13
        public int DeferralMonths = 3;        // FC Arts. 15-16
        public int ReportDaysLicensed = 15;   // FC Art. 23
        public int ReportDaysExempt = 30;     // FC Art. 30
        public int DelayedPostingDays = 10;   // AO 1 s.1993 / LCRO charters
        public bool LicensePaymentRequired = true;
        public int PsaDueDay = 10;

        public static MarriageSettings Defaults() { return new MarriageSettings(); }

        /// <summary>Loads the office's values; anything missing or unreadable keeps its default.</summary>
        public static MarriageSettings Load()
        {
            var s = new MarriageSettings();
            try
            {
                DataTable dt = Db.Pull("SELECT setting_key, setting_value FROM app_settings");
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (DataRow r in dt.Rows)
                    map[r["setting_key"].ToString()] = r["setting_value"] == DBNull.Value ? "" : r["setting_value"].ToString();

                s.PostingDays = Int(map, "MARRIAGE_POSTING_DAYS", s.PostingDays);
                s.ValidityDays = Int(map, "MARRIAGE_LICENSE_VALIDITY_DAYS", s.ValidityDays);
                s.ExpiringSoonDays = Int(map, "MARRIAGE_EXPIRING_SOON_DAYS", s.ExpiringSoonDays);
                s.ConsentAgeFrom = Int(map, "MARRIAGE_CONSENT_AGE_FROM", s.ConsentAgeFrom);
                s.ConsentAgeTo = Int(map, "MARRIAGE_CONSENT_AGE_TO", s.ConsentAgeTo);
                s.AdviceAgeFrom = Int(map, "MARRIAGE_ADVICE_AGE_FROM", s.AdviceAgeFrom);
                s.AdviceAgeTo = Int(map, "MARRIAGE_ADVICE_AGE_TO", s.AdviceAgeTo);
                s.DeferralMonths = Int(map, "MARRIAGE_DEFERRAL_MONTHS", s.DeferralMonths);
                s.ReportDaysLicensed = Int(map, "MARRIAGE_REPORT_DAYS_LICENSED", s.ReportDaysLicensed);
                s.ReportDaysExempt = Int(map, "MARRIAGE_REPORT_DAYS_EXEMPT", s.ReportDaysExempt);
                s.DelayedPostingDays = Int(map, "MARRIAGE_DELAYED_POSTING_DAYS", s.DelayedPostingDays);
                s.LicensePaymentRequired = Int(map, "MARRIAGE_LICENSE_PAYMENT_REQUIRED", 1) != 0;
                s.PsaDueDay = Int(map, "PSA_TRANSMITTAL_DUE_DAY", s.PsaDueDay);
            }
            catch { /* table absent (migration 33 not applied): statutory defaults */ }
            return s;
        }

        private static int Int(Dictionary<string, string> map, string key, int fallback)
        {
            string v;
            int n;
            return map.TryGetValue(key, out v) && int.TryParse(v, out n) && n >= 0 ? n : fallback;
        }
    }

    public enum RuleSeverity { HardStop, Blocking, Warning, Info }

    /// <summary>One finding, in words, with the place the operator fixes it.</summary>
    public sealed class RuleIssue
    {
        public RuleSeverity Severity;
        public string Message;
        /// <summary>The step / tab / window where this is corrected, e.g. "Consent &amp; Advice".</summary>
        public string FixWhere;
        public string Code;

        public RuleIssue(RuleSeverity sev, string code, string message, string fixWhere)
        { Severity = sev; Code = code; Message = message; FixWhere = fixWhere; }

        public bool Blocks { get { return Severity == RuleSeverity.HardStop || Severity == RuleSeverity.Blocking; } }
        public override string ToString() { return Severity + ": " + Message + (FixWhere == null ? "" : "  [" + FixWhere + "]"); }
    }

    /// <summary>One contracting party / applicant, as far as the rules need to know them.</summary>
    public sealed class Party
    {
        public string Role;           // "Husband" / "Wife"
        public string First, Middle, Last;
        public DateTime? Dob;
        public string Sex;
        /// <summary>Country of birth. Its own field, never folded into <see cref="PlaceOfBirth"/>.</summary>
        public string BirthCountry;
        public string PlaceOfBirth, Citizenship, CivilStatus, Religion, Residence;

        /// <summary>
        /// Father's / mother's name as ONE string - what Form 97 prints and what a licence hands
        /// to it. On a licence it is derived from the three cells below (or, for a licence filed
        /// before migration 38, the joined value it was saved with); never split back into cells.
        /// </summary>
        public string Father, Mother;

        // Form 90 prints each of these as its own cell (migration 38).
        public string FatherFirst, FatherMiddle, FatherLast, FatherCitizenship, FatherResidence;
        public string MotherFirst, MotherMiddle, MotherLast, MotherCitizenship, MotherResidence;

        /// <summary>"Person who gave consent or advice" - ONE slot per party, as on the form.</summary>
        public string ConsentFirst, ConsentMiddle, ConsentLast, ConsentRelationship, ConsentCitizenship, ConsentResidence;

        /// <summary>"If previously married" - only meaningful when <see cref="MarriageRules.IsPreviouslyMarried"/>.</summary>
        public string PrevDissolution, PrevDissolvedMunicipality, PrevDissolvedProvince;
        public DateTime? PrevDissolvedDate;

        public Party(string role) { Role = role; }

        /// <summary>The sex Form 90 expects for this column, used only as the initial pick.</summary>
        public string ExpectedSex { get { return MarriageRules.SexForRole(Role); } }

        // "" rather than null: callers upper-case it for print (LicensePrinter).
        public string FullName { get { return MarriageRules.JoinName(First, Middle, Last) ?? ""; } }

        /// <summary>Empty the previously-married block - it does not apply to this civil status.</summary>
        public void ClearPrevMarriage()
        {
            PrevDissolution = PrevDissolvedMunicipality = PrevDissolvedProvince = null;
            PrevDissolvedDate = null;
        }

        /// <summary>First name for sentences ("Parental consent is required for Maria ...").</summary>
        public string Called
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(First)) return MarriageRules.Title(First.Trim());
                return Role == "Wife" ? "the wife / party 2" : "the husband / party 1";
            }
        }

        public string RoleLabel { get { return Role == "Wife" ? "Wife / Party 2" : "Husband / Party 1"; } }
    }

    public sealed class ReqType
    {
        public string Code, Label, AppliesTo, RuleKey, Basis;
        public bool PerParty, Blocking, Active;
        public int Sort;
        /// <summary>When set, this row is one of SEVERAL that together need only
        /// <see cref="GroupMin"/> Verified - "any two of eight", not every row required. Null
        /// for an ordinary row, which is unaffected and means exactly what it always meant.</summary>
        public string GroupCode;
        public int GroupMin = 1;
    }

    public sealed class ReqRow
    {
        public int Id;
        public string Party = "Both", Code, Label, Status = "Missing", Outcome, GivenBy, ReferenceNo, Notes;
        public DateTime? DocDate, VerifiedAt;
        public int? VerifiedBy;
        public bool HasAttachment;
    }

    /// <summary>A supporting document the rules say this couple needs, and why.</summary>
    public sealed class Need
    {
        public string Code, Label, Party, Reason, Basis;
        public bool Blocking;
        public int Sort;
    }

    /// <summary>Everything the rules need about a licence application.</summary>
    public sealed class LicenseFacts
    {
        public int Id;
        public string ApplicationNo, LicenseNo, StoredStatus = "Draft";
        public Party Husband = new Party("Husband"), Wife = new Party("Wife");
        public DateTime? FiledDate, PostingStart, PostingEnd, EarliestIssue, IssueDate, ExpiryDate;
        public string DeferralReason, PaymentOr, ImpedimentNote, HoldReason, CancelReason, Remarks;
        public decimal? PaymentAmount;
        public DateTime? PaymentDate;
        public int? OverrideBy;
        public List<ReqRow> Requirements = new List<ReqRow>();
        public int? UsedByMarriageId;
        public string UsedByRegistryNo, UsedByStatus;
    }

    /// <summary>Everything the rules need about a Form 97 record.</summary>
    public sealed class MarriageFacts
    {
        public int Id;
        public string Status = "Draft", RegistryNo;
        public Party Husband = new Party("Husband"), Wife = new Party("Wife");
        public DateTime? DateOfMarriage, DateReceived, CasePostingStart;
        public bool HasPlace;
        public string Solemnizer, SolemnizerPosition, Witness1, Witness2;
        public string Basis;          // Licensed / Exempt / null (not chosen)
        public int? LicenseId;
        public bool OutOfProvinceLicense;   // true = the licence was obtained in ANOTHER province (backlog Sec.4.1)
        public string ExternalLicenseNo;    // typed, not linked - only meaningful when OutOfProvinceLicense
        public DateTime? ExternalLicenseDate;
        public string ExemptionBasis, DelayReason, RegistrarReview, OcrReviewStatus;
        public int? OcrWeakFields;
        public List<ReqRow> Requirements = new List<ReqRow>();
    }

    public static class MarriageRules
    {
        public static readonly string[] CivilStatuses =
            { "Single", "Widowed", "Annulled", "Divorced", "Married", "Separated" };

        public static readonly string[] Sexes = { "Male", "Female" };

        /// <summary>
        /// Suggestions for "Relationship" of the person who gave consent or advice. Taken from the
        /// office's own paper, not invented: MF No. 06 prints Father / Mother / Guardian, MF No. 68
        /// adds Legal Guardian / Head of Institution, and FC Art. 14 names the person having legal
        /// charge. The box stays editable - this is a suggestion list, not a rule.
        /// </summary>
        public static readonly string[] ConsentRelationships =
            { "Father", "Mother", "Guardian", "Legal Guardian", "Person having legal charge", "Head of Institution" };

        /// <summary>
        /// Suggestions for how a previous marriage was dissolved. Editable, not a closed list - the
        /// form asks a free question, and a foreign divorce recognised under FC Art. 26 or one under
        /// PD 1083 is written as the paper states it.
        /// </summary>
        public static readonly string[] Dissolutions =
            { "Death of spouse", "Annulment", "Declaration of nullity", "Divorce" };

        /// <summary>First / middle / last joined for display, blanks skipped. Never reversed.</summary>
        public static string JoinName(string first, string middle, string last)
        {
            string s = string.Join(" ", new[] { first, middle, last }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
            return s.Length == 0 ? null : s;
        }

        /// <summary>What Form 90's husband / wife column expects. Only ever an initial pick.</summary>
        public static string SexForRole(string role)
        {
            return role == "Wife" ? "Female" : "Male";
        }

        /// <summary>License-exemption bases actually in the Family Code (Arts. 27-34).</summary>
        public static readonly string[,] ExemptionBases =
        {
            { "ART27", "Art. 27 - in articulo mortis (one party at the point of death)" },
            { "ART28", "Art. 28 - residence with no means of transport to the civil registrar" },
            { "ART31", "Art. 31 - by a ship captain / airplane chief, in articulo mortis" },
            { "ART32", "Art. 32 - by a military commander, in articulo mortis" },
            { "ART33", "Art. 33 - Muslims / ethnic cultural communities, per their customs" },
            { "ART34", "Art. 34 - lived together as husband and wife at least five years" },
        };

        public static string ExemptionLabel(string code)
        {
            for (int i = 0; i < ExemptionBases.GetLength(0); i++)
                if (string.Equals(ExemptionBases[i, 0], code, StringComparison.OrdinalIgnoreCase))
                    return ExemptionBases[i, 1];
            return code;
        }

        // ------------------------------------------------------------ basics
        public static int AgeOn(DateTime dob, DateTime on)
        {
            int age = on.Year - dob.Year;
            if (on.Date < dob.Date.AddYears(age)) age--;
            return age;
        }

        public static bool IsFilipino(string citizenship)
        {
            if (string.IsNullOrWhiteSpace(citizenship)) return true;   // unknown is not "foreign"
            string c = citizenship.Trim().ToLowerInvariant();
            return c.StartsWith("filipin") || c.StartsWith("pilipin") || c == "ph" || c == "philippines";
        }

        public static bool IsForeign(string citizenship)
        {
            return !string.IsNullOrWhiteSpace(citizenship) && !IsFilipino(citizenship);
        }

        /// <summary>A previous marriage that has ENDED - needs its Art. 13 proof.</summary>
        public static bool IsPreviouslyMarried(string civilStatus)
        {
            string c = (civilStatus ?? "").Trim().ToLowerInvariant();
            return c == "widowed" || c == "widow" || c == "widower" || c == "annulled"
                || c == "divorced" || c.StartsWith("nullif") || c.Contains("void");
        }

        /// <summary>A marriage that has NOT ended (legal separation does not dissolve one).</summary>
        public static bool HasSubsistingMarriage(string civilStatus)
        {
            string c = (civilStatus ?? "").Trim().ToLowerInvariant();
            return c == "married" || c == "separated" || c.StartsWith("legally separated");
        }

        public static string Title(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.Trim().ToLowerInvariant());
        }

        public static string D(DateTime? d) { return d.HasValue ? d.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) : "-"; }

        // -------------------------------------------------------- dates
        /// <summary>Last day of posting. Day 1 is the posting start (FC Art. 17: ten consecutive days).</summary>
        public static DateTime PostingEnd(DateTime start, MarriageSettings s)
        {
            return start.Date.AddDays(Math.Max(1, s.PostingDays) - 1);
        }

        /// <summary>
        /// First day the licence may issue: the day after posting ends, or - when advice is
        /// unfavourable / not obtained (Art. 15) or required counselling is not attached
        /// (Art. 16) - the day after three months following completion of the posting.
        /// </summary>
        public static DateTime EarliestIssue(DateTime postingStart, bool deferred, MarriageSettings s)
        {
            DateTime end = PostingEnd(postingStart, s);
            return deferred ? end.AddMonths(s.DeferralMonths).AddDays(1) : end.AddDays(1);
        }

        /// <summary>Last valid day: issue date + 120 (first day excluded, last included).</summary>
        public static DateTime Expiry(DateTime issueDate, MarriageSettings s)
        {
            return issueDate.Date.AddDays(s.ValidityDays);
        }

        public static DateTime ReportingDeadline(DateTime dateOfMarriage, bool exempt, MarriageSettings s)
        {
            return dateOfMarriage.Date.AddDays(exempt ? s.ReportDaysExempt : s.ReportDaysLicensed);
        }

        /// <summary>True when the certificate reached the office after the reporting period.</summary>
        public static bool IsDelayed(DateTime dateOfMarriage, DateTime dateReceived, bool exempt, MarriageSettings s)
        {
            return dateReceived.Date > ReportingDeadline(dateOfMarriage, exempt, s);
        }

        // --------------------------------------------------- display status
        /// <summary>
        /// What the licence IS today. Only Draft / Posting / On Hold / Issued / Expired / Used /
        /// Cancelled are stored; the rest are derived from dates so they can never go stale:
        /// "Ready to Issue" is a Posting licence whose earliest issue date has arrived, and
        /// Valid / Expiring / Expired are an Issued licence read against today.
        /// </summary>
        public static string LicenseDisplayStatus(LicenseFacts l, DateTime today, MarriageSettings s,
                                                  int outstandingBlocking)
        {
            string st = l.StoredStatus ?? "Draft";
            if (st == "Cancelled") return "Cancelled";
            if (st == "Used" || (l.UsedByMarriageId.HasValue && l.UsedByStatus == "Registered")) return "Used";
            if (st == "On Hold") return "On Hold";
            if (st == "Issued" || st == "Expired")
            {
                if (!l.ExpiryDate.HasValue) return "Valid";
                if (today.Date > l.ExpiryDate.Value.Date) return "Expired";
                int left = (l.ExpiryDate.Value.Date - today.Date).Days;
                return left <= s.ExpiringSoonDays ? "Expiring" : "Valid";
            }
            if (st == "Posting")
            {
                if (l.EarliestIssue.HasValue && today.Date >= l.EarliestIssue.Value.Date)
                    return outstandingBlocking > 0 ? "Posting Complete" : "Ready to Issue";
                return "Posting";
            }
            return outstandingBlocking > 0 ? "Requirements Incomplete" : "Ready for Posting";
        }

        public static int DaysRemaining(LicenseFacts l, DateTime today)
        {
            return l.ExpiryDate.HasValue ? (l.ExpiryDate.Value.Date - today.Date).Days : 0;
        }

        /// <summary>1-based posting day, clamped to the posting length.</summary>
        public static int PostingDay(LicenseFacts l, DateTime today, MarriageSettings s)
        {
            if (!l.PostingStart.HasValue) return 0;
            int d = (today.Date - l.PostingStart.Value.Date).Days + 1;
            return Math.Max(0, Math.Min(d, s.PostingDays));
        }

        // ---------------------------------------------------- age findings
        /// <summary>Human-readable age findings for one applicant on a date.</summary>
        public static List<RuleIssue> AgeFindings(Party p, DateTime on, MarriageSettings s, string context)
        {
            var list = new List<RuleIssue>();
            if (!p.Dob.HasValue)
            {
                list.Add(new RuleIssue(RuleSeverity.Blocking, "DOB_MISSING",
                    p.RoleLabel + ": date of birth is needed - CROMS uses it to work out which consents apply.",
                    context));
                return list;
            }
            if (p.Dob.Value.Date > on.Date)
            {
                list.Add(new RuleIssue(RuleSeverity.Blocking, "DOB_FUTURE",
                    p.RoleLabel + ": date of birth " + D(p.Dob) + " is after " + D(on) + ".", context));
                return list;
            }
            int age = AgeOn(p.Dob.Value, on);
            if (age < 18)
                list.Add(new RuleIssue(RuleSeverity.HardStop, "UNDER_18",
                    p.Called + " is " + age + " on " + D(on) + ". A marriage where either party is under 18 is void " +
                    "and prohibited (RA 11596; Family Code Art. 5). This cannot proceed as an ordinary marriage.",
                    context));
            else if (age >= s.ConsentAgeFrom && age <= s.ConsentAgeTo)
                list.Add(new RuleIssue(RuleSeverity.Info, "CONSENT_AGE",
                    "Parental consent is required for " + p.Called + " because " + Pronoun(p) + " is " + age +
                    " on " + D(on) + ". (Family Code Art. 14)", "Consent & Advice"));
            else if (age >= s.AdviceAgeFrom && age <= s.AdviceAgeTo)
                list.Add(new RuleIssue(RuleSeverity.Info, "ADVICE_AGE",
                    "Parental advice is required for " + p.Called + " because " + Pronoun(p) + " is " + age +
                    " on " + D(on) + ". If it is unfavourable or not obtained, the licence waits three months " +
                    "after posting ends. (Family Code Art. 15)", "Consent & Advice"));
            return list;
        }

        private static string Pronoun(Party p) { return p.Role == "Wife" ? "she" : "he"; }

        public static bool InConsentBand(Party p, DateTime on, MarriageSettings s)
        {
            if (!p.Dob.HasValue) return false;
            int a = AgeOn(p.Dob.Value, on);
            return a >= s.ConsentAgeFrom && a <= s.ConsentAgeTo;
        }

        public static bool InAdviceBand(Party p, DateTime on, MarriageSettings s)
        {
            if (!p.Dob.HasValue) return false;
            int a = AgeOn(p.Dob.Value, on);
            return a >= s.AdviceAgeFrom && a <= s.AdviceAgeTo;
        }

        // ------------------------------------------------------------ needs
        /// <summary>
        /// Which catalogue documents apply to this couple, each with the sentence that says
        /// why. <paramref name="on"/> is the application date for a licence, the marriage
        /// date for a certificate.
        /// </summary>
        public static List<Need> Needs(Party h, Party w, DateTime on, IEnumerable<ReqType> catalog,
                                       string appliesTo, MarriageSettings s,
                                       bool exempt = false, bool delayed = false,
                                       bool outOfProvinceLicense = false)
        {
            var needs = new List<Need>();
            var parties = new[] { h, w };
            foreach (ReqType t in catalog.Where(c => c.Active &&
                         string.Equals(c.AppliesTo, appliesTo, StringComparison.OrdinalIgnoreCase))
                                         .OrderBy(c => c.Sort))
            {
                switch (t.RuleKey)
                {
                    case "Always":
                        if (t.PerParty) foreach (Party p in parties) needs.Add(N(t, p.Role, "Required for every applicant."));
                        else needs.Add(N(t, "Both", "Required for every licence application."));
                        break;

                    case "ConsentAge":
                        foreach (Party p in parties)
                            if (InConsentBand(p, on, s))
                                needs.Add(N(t, p.Role, p.Called + " is " + AgeOn(p.Dob.Value, on) +
                                    " on " + D(on) + " (consent band " + s.ConsentAgeFrom + "-" + s.ConsentAgeTo + ")."));
                        break;

                    case "AdviceAge":
                        foreach (Party p in parties)
                            if (InAdviceBand(p, on, s))
                                needs.Add(N(t, p.Role, p.Called + " is " + AgeOn(p.Dob.Value, on) +
                                    " on " + D(on) + " (advice band " + s.AdviceAgeFrom + "-" + s.AdviceAgeTo + ")."));
                        break;

                    case "CounselingAge":
                        // Art. 16: where consent OR advice is needed, "the contracting parties"
                        // attach one counselling certificate - so it is one row for the couple.
                        Party who = parties.FirstOrDefault(p => InConsentBand(p, on, s) || InAdviceBand(p, on, s));
                        if (who != null)
                            needs.Add(N(t, "Both", "Needed because " + who.Called +
                                " requires parental " + (InConsentBand(who, on, s) ? "consent" : "advice") + "."));
                        break;

                    case "PreviouslyMarried":
                        foreach (Party p in parties)
                            if (IsPreviouslyMarried(p.CivilStatus))
                                needs.Add(N(t, p.Role, p.Called + "'s civil status is " + p.CivilStatus + "."));
                        break;

                    case "Foreigner":
                        foreach (Party p in parties)
                            if (IsForeign(p.Citizenship))
                                needs.Add(N(t, p.Role, p.Called + " is a " + p.Citizenship + " citizen."));
                        break;

                    case "Exempt":
                        if (exempt) needs.Add(N(t, "Both", "The marriage was solemnized without a licence."));
                        break;

                    case "Delayed":
                        if (delayed) needs.Add(N(t, "Both", "The certificate was received after the reporting period."));
                        break;

                    case "OutOfProvinceLicense":
                        if (outOfProvinceLicense)
                            needs.Add(N(t, "Both", "The marriage licence for this couple was obtained in another province."));
                        break;
                }
            }
            return needs;
        }

        private static Need N(ReqType t, string party, string reason)
        {
            return new Need { Code = t.Code, Label = t.Label, Party = party, Reason = reason,
                              Basis = t.Basis, Blocking = t.Blocking, Sort = t.Sort };
        }

        public static ReqRow RowFor(IEnumerable<ReqRow> rows, Need n)
        {
            return rows.FirstOrDefault(r => r.Code == n.Code && r.Party == n.Party);
        }

        /// <summary>
        /// A requirement is satisfied when staff have VERIFIED it. "Submitted" means received but
        /// not yet checked, which is exactly what the final check exists to do. A counselling
        /// certificate may instead be "Waived", which the law allows only as a three-month
        /// deferral (Art. 16) - handled by <see cref="Deferral"/>, not by pretending it arrived.
        /// </summary>
        public static bool Satisfied(ReqRow r)
        {
            if (r == null) return false;
            if (r.Status == "Verified") return true;
            return r.Status == "Waived" && r.Code == "COUNSELING";
        }

        /// <summary>Why the licence must wait three months after posting, or null.</summary>
        public static string Deferral(IEnumerable<ReqRow> rows)
        {
            var reasons = new List<string>();
            foreach (ReqRow r in rows)
            {
                if (r.Code == "PARENTAL_ADVICE" && r.Status == "Verified" &&
                    !string.IsNullOrEmpty(r.Outcome) && r.Outcome != "Favorable")
                    reasons.Add("parental advice for the " + r.Party.ToLowerInvariant() + " is " +
                                r.Outcome.ToLowerInvariant() + " (Art. 15)");
                if (r.Code == "COUNSELING" && r.Status == "Waived")
                    reasons.Add("the marriage counselling certificate was not attached (Art. 16)");
            }
            return reasons.Count == 0 ? null : string.Join("; ", reasons);
        }

        public static string NotSatisfiedText(Need n, ReqRow r, Party h, Party w)
        {
            string who = n.Party == "Both" ? "" : " - " + (n.Party == "Wife" ? w.Called : h.Called);
            string state;
            if (r == null || r.Status == "Missing") state = "is still missing.";
            else if (r.Status == "Submitted") state = "was submitted but has not been verified.";
            else if (r.Status == "Rejected") state = "was rejected" + (string.IsNullOrWhiteSpace(r.Notes) ? "." : ": " + r.Notes);
            else state = "is " + r.Status.ToLowerInvariant() + ".";
            return n.Label + who + " " + state;
        }

        /// <summary>Outstanding blocking requirements, as sentences.</summary>
        public static List<RuleIssue> RequirementIssues(List<Need> needs, List<ReqRow> rows, Party h, Party w, string fixWhere)
        {
            var list = new List<RuleIssue>();
            foreach (Need n in needs.Where(x => x.Blocking))
            {
                ReqRow r = RowFor(rows, n);
                if (Satisfied(r)) continue;
                string where = (n.Code == "PARENTAL_CONSENT" || n.Code == "PARENTAL_ADVICE" || n.Code == "COUNSELING")
                    ? "Consent & Advice" : fixWhere;
                list.Add(new RuleIssue(RuleSeverity.Blocking, "REQ_" + n.Code, NotSatisfiedText(n, r, h, w), where));
            }
            return list;
        }

        // ---------------------------------------------------- applicant data
        public static List<RuleIssue> ApplicantIssues(Party p, DateTime on, MarriageSettings s, string fixWhere)
        {
            var list = new List<RuleIssue>();
            if (string.IsNullOrWhiteSpace(p.First) || string.IsNullOrWhiteSpace(p.Last))
                list.Add(new RuleIssue(RuleSeverity.Blocking, "NAME", p.RoleLabel + ": first and last name are required.", fixWhere));
            if (string.IsNullOrWhiteSpace(p.Citizenship))
                list.Add(new RuleIssue(RuleSeverity.Blocking, "CITIZENSHIP", p.RoleLabel + ": citizenship is required.", fixWhere));
            if (string.IsNullOrWhiteSpace(p.CivilStatus))
                list.Add(new RuleIssue(RuleSeverity.Blocking, "CIVIL", p.RoleLabel + ": civil status is required.", fixWhere));
            foreach (RuleIssue a in AgeFindings(p, on, s, fixWhere))
                if (a.Severity != RuleSeverity.Info) list.Add(a);
            return list;
        }

        /// <summary>
        /// An existing, undissolved marriage is a legal impediment. Art. 18 still has the
        /// registrar issue the licence after posting unless a court orders otherwise, so this
        /// is not a hard stop - it blocks until the registrar records a note on it.
        /// </summary>
        public static List<RuleIssue> ImpedimentIssues(Party h, Party w, string note, string fixWhere)
        {
            var list = new List<RuleIssue>();
            foreach (Party p in new[] { h, w })
                if (HasSubsistingMarriage(p.CivilStatus) && string.IsNullOrWhiteSpace(note))
                    list.Add(new RuleIssue(RuleSeverity.Blocking, "IMPEDIMENT",
                        p.Called + "'s civil status is recorded as \"" + p.CivilStatus + "\" - an existing marriage " +
                        "is a possible impediment. The registrar must record a finding before this can proceed " +
                        "(Family Code Art. 18).", fixWhere));
            return list;
        }

        // ------------------------------------------------ licence: posting / issue
        public static List<RuleIssue> ValidateForPosting(LicenseFacts l, MarriageSettings s)
        {
            DateTime on = l.FiledDate ?? DateTime.Today;
            var list = new List<RuleIssue>();
            if (l.StoredStatus != "Draft")
                list.Add(new RuleIssue(RuleSeverity.Blocking, "STATE", "Posting can only start from a draft application (this one is " + l.StoredStatus + ").", "Posting"));
            list.AddRange(ApplicantIssues(l.Husband, on, s, "Applicants"));
            list.AddRange(ApplicantIssues(l.Wife, on, s, "Applicants"));
            return list;
        }

        /// <summary>Every reason the licence cannot be issued on <paramref name="issueDate"/>.</summary>
        public static List<RuleIssue> ValidateForIssue(LicenseFacts l, IEnumerable<ReqType> catalog,
                                                       DateTime issueDate, MarriageSettings s)
        {
            DateTime on = l.FiledDate ?? issueDate;
            var list = new List<RuleIssue>();
            list.AddRange(ApplicantIssues(l.Husband, on, s, "Applicants"));
            list.AddRange(ApplicantIssues(l.Wife, on, s, "Applicants"));
            list.AddRange(ImpedimentIssues(l.Husband, l.Wife, l.ImpedimentNote, "Applicants"));

            if (l.StoredStatus == "Draft")
                list.Add(new RuleIssue(RuleSeverity.Blocking, "NOT_POSTED",
                    "Posting has not started. Start the ten-day posting first.", "Posting"));
            else if (l.StoredStatus == "On Hold")
                list.Add(new RuleIssue(RuleSeverity.Blocking, "ON_HOLD",
                    "The application is on hold" + (string.IsNullOrWhiteSpace(l.HoldReason) ? "." : ": " + l.HoldReason), "Posting"));
            else if (l.StoredStatus != "Posting")
                list.Add(new RuleIssue(RuleSeverity.Blocking, "STATE",
                    "A licence cannot be issued from status " + l.StoredStatus + ".", "Issue License"));
            else if (!l.EarliestIssue.HasValue || issueDate.Date < l.EarliestIssue.Value.Date)
            {
                string msg = l.PostingEnd.HasValue
                    ? "Posting has not completed. It runs " + D(l.PostingStart) + " - " + D(l.PostingEnd) +
                      "; the earliest the licence can issue is " + D(l.EarliestIssue) + "."
                    : "Posting has not completed.";
                if (!string.IsNullOrEmpty(l.DeferralReason)) msg += " It is deferred because " + l.DeferralReason + ".";
                list.Add(new RuleIssue(RuleSeverity.Blocking, "POSTING", msg, "Posting"));
            }

            List<Need> needs = Needs(l.Husband, l.Wife, on, catalog, "License", s);
            list.AddRange(RequirementIssues(needs, l.Requirements, l.Husband, l.Wife, "Requirements"));

            if (s.LicensePaymentRequired && string.IsNullOrWhiteSpace(l.PaymentOr))
                list.Add(new RuleIssue(RuleSeverity.Blocking, "PAYMENT",
                    "No payment is recorded - enter the Treasury official receipt number.", "Issue License"));
            return list;
        }

        // ------------------------------------------------ Form 97
        public static bool NamesMatch(Party onCertificate, Party onLicense)
        {
            return Same(onCertificate.First, onLicense.First) && Same(onCertificate.Last, onLicense.Last);
        }

        private static bool Same(string a, string b)
        {
            return LearningLibrary.Normalize(a ?? "") == LearningLibrary.Normalize(b ?? "");
        }

        /// <summary>
        /// Every reason this Certificate of Marriage cannot be registered yet. The linked
        /// licence is passed in already loaded (null when none is selected).
        /// </summary>
        public static List<RuleIssue> ValidateMarriage(MarriageFacts m, LicenseFacts lic,
                                                       IEnumerable<ReqType> catalog, DateTime today,
                                                       MarriageSettings s)
        {
            var list = new List<RuleIssue>();
            const string Parties = "Contracting Parties", Consent = "Consent & License",
                         Sol = "Solemnization", Cert = "Certification", Case = "Case Workflow";

            foreach (Party p in new[] { m.Husband, m.Wife })
            {
                if (string.IsNullOrWhiteSpace(p.First) || string.IsNullOrWhiteSpace(p.Last))
                    list.Add(new RuleIssue(RuleSeverity.Blocking, "NAME", p.RoleLabel + ": first and last name are required.", Parties));
                if (string.IsNullOrWhiteSpace(p.CivilStatus))
                    list.Add(new RuleIssue(RuleSeverity.Blocking, "CIVIL", p.RoleLabel + ": civil status is required.", Parties));
            }

            if (Same(m.Husband.First, m.Wife.First) && Same(m.Husband.Last, m.Wife.Last) &&
                !string.IsNullOrWhiteSpace(m.Husband.First))
                list.Add(new RuleIssue(RuleSeverity.Warning, "SAME_NAME",
                    "Both parties carry the same name - the classic two-column misreading of Form 97. Check against the paper.", Parties));

            if (!m.DateOfMarriage.HasValue)
                list.Add(new RuleIssue(RuleSeverity.Blocking, "DOM", "Date of marriage is required.", Sol));
            else if (m.DateOfMarriage.Value.Date > today.Date)
                list.Add(new RuleIssue(RuleSeverity.Blocking, "DOM_FUTURE", "Date of marriage " + D(m.DateOfMarriage) + " is in the future.", Sol));

            if (m.DateOfMarriage.HasValue)
                foreach (Party p in new[] { m.Husband, m.Wife })
                    foreach (RuleIssue a in AgeFindings(p, m.DateOfMarriage.Value, s, Parties))
                        if (a.Severity == RuleSeverity.HardStop) list.Add(a);

            if (!m.HasPlace)
                list.Add(new RuleIssue(RuleSeverity.Blocking, "PLACE", "Place of marriage (municipality and province) is required.", Sol));
            if (string.IsNullOrWhiteSpace(m.Solemnizer))
                list.Add(new RuleIssue(RuleSeverity.Blocking, "SOLEMNIZER", "Solemnizing officer's name is required.", Sol));
            if (string.IsNullOrWhiteSpace(m.SolemnizerPosition))
                list.Add(new RuleIssue(RuleSeverity.Warning, "SOL_POS",
                    "Solemnizing officer's position is blank. CROMS records it but does not judge authority - the registrar confirms it.", Sol));
            if (string.IsNullOrWhiteSpace(m.Witness1) || string.IsNullOrWhiteSpace(m.Witness2))
                list.Add(new RuleIssue(RuleSeverity.Blocking, "WITNESS",
                    "Two witnesses are required on the certificate (Family Code Art. 3).", Sol));

            if (!m.DateReceived.HasValue)
                list.Add(new RuleIssue(RuleSeverity.Blocking, "RECEIVED",
                    "Date the certificate was received at this office is required - it decides timely vs delayed registration.", Cert));
            else if (m.DateOfMarriage.HasValue && m.DateReceived.Value.Date < m.DateOfMarriage.Value.Date)
                list.Add(new RuleIssue(RuleSeverity.Blocking, "RECEIVED_EARLY",
                    "Date received " + D(m.DateReceived) + " is before the date of marriage " + D(m.DateOfMarriage) + ".", Cert));

            bool exempt = m.Basis == "Exempt";
            if (m.Basis != "Licensed" && m.Basis != "Exempt")
                list.Add(new RuleIssue(RuleSeverity.Blocking, "BASIS",
                    "Choose LICENSE REQUIRED (select the licence) or LICENSE EXEMPT (state the legal basis).", Consent));

            // ---- licence link
            if (m.Basis == "Licensed" && m.OutOfProvinceLicense)
            {
                // No local marriage_licenses row to link - this office never issued it. The
                // licence's own number/date are typed in (backlog Sec.4.1); the attachment that
                // proves it is enforced separately below, through the OUT_OF_PROVINCE_LICENSE
                // requirement row, the same way PREV_MARRIAGE/EXEMPT_AFFIDAVIT already are.
                if (string.IsNullOrWhiteSpace(m.ExternalLicenseNo))
                    list.Add(new RuleIssue(RuleSeverity.Blocking, "OOP_LICNO",
                        "Enter the licence number issued by the other LCRO.", Consent));
                if (!m.ExternalLicenseDate.HasValue)
                    list.Add(new RuleIssue(RuleSeverity.Blocking, "OOP_LICDATE",
                        "Enter the date that licence was issued.", Consent));
                else if (m.DateOfMarriage.HasValue && m.DateOfMarriage.Value.Date < m.ExternalLicenseDate.Value.Date)
                    list.Add(new RuleIssue(RuleSeverity.Blocking, "OOP_LIC_BEFORE",
                        "Marriage date " + D(m.DateOfMarriage) + " is before the licence was issued (" +
                        D(m.ExternalLicenseDate) + ").", Consent));
            }
            else if (m.Basis == "Licensed")
            {
                if (!m.LicenseId.HasValue || lic == null)
                    list.Add(new RuleIssue(RuleSeverity.Blocking, "NO_LICENSE", "Select the marriage licence this marriage was solemnized under.", Consent));
                else
                {
                    if (lic.StoredStatus == "Cancelled")
                        list.Add(new RuleIssue(RuleSeverity.Blocking, "LIC_CANCELLED", "Licence " + lic.LicenseNo + " was cancelled.", Consent));
                    else if (!lic.IssueDate.HasValue || (lic.StoredStatus != "Issued" && lic.StoredStatus != "Expired" && lic.StoredStatus != "Used"))
                        list.Add(new RuleIssue(RuleSeverity.Blocking, "LIC_NOT_ISSUED",
                            "Application " + lic.ApplicationNo + " has not been issued a licence.", Consent));

                    if (lic.UsedByMarriageId.HasValue && lic.UsedByMarriageId.Value != m.Id)
                        list.Add(new RuleIssue(RuleSeverity.Blocking, "LIC_USED",
                            "Licence " + lic.LicenseNo + " is already linked to marriage " +
                            (lic.UsedByRegistryNo ?? "#" + lic.UsedByMarriageId) + ". One licence supports one marriage.", Consent));

                    if (m.DateOfMarriage.HasValue && lic.IssueDate.HasValue && lic.ExpiryDate.HasValue)
                    {
                        if (m.DateOfMarriage.Value.Date < lic.IssueDate.Value.Date)
                            list.Add(new RuleIssue(RuleSeverity.Blocking, "LIC_BEFORE",
                                "Marriage date " + D(m.DateOfMarriage) + " is before licence " + lic.LicenseNo +
                                " was issued (" + D(lic.IssueDate) + ").", Consent));
                        else if (m.DateOfMarriage.Value.Date > lic.ExpiryDate.Value.Date)
                            list.Add(new RuleIssue(RuleSeverity.Blocking, "LIC_EXPIRED",
                                "Licence " + lic.LicenseNo + " expired on " + D(lic.ExpiryDate) +
                                ". Marriage date " + D(m.DateOfMarriage) + " falls outside the recorded licence validity.", Consent));
                    }

                    if (!NamesMatch(m.Husband, lic.Husband))
                        list.Add(new RuleIssue(RuleSeverity.Blocking, "MISMATCH_H",
                            "Applicants do not match the selected licence: it names the husband / party 1 as " +
                            lic.Husband.FullName + ", this certificate as " + m.Husband.FullName + ".", Parties));
                    if (!NamesMatch(m.Wife, lic.Wife))
                        list.Add(new RuleIssue(RuleSeverity.Blocking, "MISMATCH_W",
                            "Applicants do not match the selected licence: it names the wife / party 2 as " +
                            lic.Wife.FullName + ", this certificate as " + m.Wife.FullName + ".", Parties));
                }
            }

            // ---- exempt path
            if (exempt)
            {
                if (string.IsNullOrWhiteSpace(m.ExemptionBasis))
                    list.Add(new RuleIssue(RuleSeverity.Blocking, "EXEMPT_BASIS", "State the legal basis of the licence exemption.", Case));
            }

            // ---- timely / delayed
            bool delayed = m.DateOfMarriage.HasValue && m.DateReceived.HasValue &&
                           IsDelayed(m.DateOfMarriage.Value, m.DateReceived.Value, exempt, s);
            if (delayed)
            {
                if (string.IsNullOrWhiteSpace(m.DelayReason))
                    list.Add(new RuleIssue(RuleSeverity.Blocking, "DELAY_REASON",
                        "Received " + D(m.DateReceived) + ", after the " + (exempt ? s.ReportDaysExempt : s.ReportDaysLicensed) +
                        "-day reporting period (deadline " + D(ReportingDeadline(m.DateOfMarriage.Value, exempt, s)) +
                        "). This is a DELAYED registration - record the reason for the delay.", Case));
                if (!m.CasePostingStart.HasValue)
                    list.Add(new RuleIssue(RuleSeverity.Blocking, "DELAY_POSTING",
                        "Post the notice of delayed registration (" + s.DelayedPostingDays + " days) before registering.", Case));
                else
                {
                    DateTime ends = m.CasePostingStart.Value.Date.AddDays(s.DelayedPostingDays - 1);
                    if (today.Date <= ends)
                        list.Add(new RuleIssue(RuleSeverity.Blocking, "DELAY_POSTING_OPEN",
                            "Delayed-registration notice is posted until " + D(ends) + "; registration is possible from " + D(ends.AddDays(1)) + ".", Case));
                }
            }

            if ((exempt || delayed) && m.RegistrarReview != "Approved")
                list.Add(new RuleIssue(RuleSeverity.Blocking, "REVIEW",
                    (exempt && delayed ? "Licence-exempt and delayed" : exempt ? "Licence-exempt" : "Delayed") +
                    " registrations need registrar review and approval before registering" +
                    (m.RegistrarReview == "Returned" ? " (the registrar returned it)." : "."), Case));

            // ---- supporting documents (marriage-level; licensed marriages satisfy
            //      previous-marriage proof from the licence file)
            DateTime on = m.DateOfMarriage ?? today;
            List<Need> needs = Needs(m.Husband, m.Wife, on, catalog, "Marriage", s, exempt, delayed, m.OutOfProvinceLicense);
            foreach (Need n in needs.Where(x => x.Blocking))
            {
                ReqRow r = RowFor(m.Requirements, n);
                if (Satisfied(r)) continue;
                if (n.Code == "PREV_MARRIAGE" && lic != null && Satisfied(lic.Requirements.FirstOrDefault(x => x.Code == "PREV_MARRIAGE" && x.Party == n.Party)))
                    continue;
                string whose = n.Party == "Wife" ? m.Wife.Called : m.Husband.Called;
                string msg = n.Code == "PREV_MARRIAGE"
                    ? (n.Party == "Wife" ? "Wife" : "Husband") + "'s civil status is " +
                      (n.Party == "Wife" ? m.Wife.CivilStatus : m.Husband.CivilStatus) +
                      ", but no supporting previous-marriage document has been recorded" +
                      (lic != null ? " on this record or on its licence." : ".")
                    : NotSatisfiedText(n, r, m.Husband, m.Wife);
                list.Add(new RuleIssue(RuleSeverity.Blocking, "MREQ_" + n.Code, msg,
                    n.Code == "PREV_MARRIAGE" ? Consent : Case));
            }

            if (m.OcrReviewStatus == "Required")
                list.Add(new RuleIssue(RuleSeverity.Blocking, "OCR",
                    "This record came from a scan with " + (m.OcrWeakFields ?? 0) + " weak field(s). Compare it with the " +
                    "scan and mark the OCR review complete.", "Source Document"));

            return list;
        }

        public static bool WouldBeDelayed(MarriageFacts m, MarriageSettings s)
        {
            return m.DateOfMarriage.HasValue && m.DateReceived.HasValue &&
                   IsDelayed(m.DateOfMarriage.Value, m.DateReceived.Value, m.Basis == "Exempt", s);
        }
    }
}
