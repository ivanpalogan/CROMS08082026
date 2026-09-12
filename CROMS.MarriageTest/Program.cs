using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.MarriageTest
{
    /// <summary>
    /// End-to-end test of the marriage workflow: Form 90 -> requirements -> posting -> issue ->
    /// 120-day lifecycle -> Form 97 (licensed / exempt / delayed / OCR) -> register -> copies ->
    /// PSA/OCRG transmittal. Drives MarriageService against the LIVE database with test couples
    /// surnamed ZZT..., then deletes every row it created (also any leftovers from a crashed run).
    /// Exit code = number of failed checks.
    /// </summary>
    internal static class Program
    {
        private static int _pass, _fail;
        private static readonly string Tag = "ZZT" + DateTime.Now.ToString("HHmmss");
        private static readonly DateTime Today = DateTime.Today;
        private static readonly List<int> Batches = new List<int>();

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length > 1 && args[0] == "--render")
            {
                try { Cleanup(); LoginAs("Admin"); Render(args[1]); }
                catch (Exception ex) { Console.WriteLine("RENDER CRASH: " + ex); }
                finally { Cleanup(); Console.WriteLine("cleanup: leftovers = " + Leftovers()); }
                return 0;
            }
            if (args.Length > 1 && args[0] == "--mf90")
            {
                try { Cleanup(); LoginAs("Admin"); Mf90Print(args[1]); }
                catch (Exception ex) { _fail++; Console.WriteLine("CRASH: " + ex); }
                finally { Cleanup(); int left = Leftovers(); Check("zero strays after cleanup", left == 0, left + " left"); }
                Console.WriteLine("PASSED " + _pass + "   FAILED " + _fail);
                return _fail;
            }
            if (args.Length > 1 && args[0] == "--form90")
            {
                try { Cleanup(); LoginAs("Admin"); Form90Blocks(args[1]); }
                catch (Exception ex) { _fail++; Console.WriteLine("CRASH: " + ex); }
                finally { Cleanup(); int left = Leftovers(); Check("zero strays after cleanup", left == 0, left + " left"); }
                Console.WriteLine("PASSED " + _pass + "   FAILED " + _fail);
                return _fail;
            }
            Console.WriteLine("CROMS marriage workflow test  -  tag " + Tag);
            try
            {
                Cleanup();
                LoginAs("Admin");
                PureRules();
                LicenceScenarios();
                MarriageScenarios();
            }
            catch (Exception ex)
            {
                _fail++;
                Console.WriteLine("CRASH: " + ex);
            }
            finally
            {
                try { Cleanup(); Console.WriteLine("cleanup: done, leftovers = " + Leftovers()); }
                catch (Exception ex) { Console.WriteLine("cleanup FAILED: " + ex.Message); _fail++; }
            }
            Console.WriteLine();
            Console.WriteLine("PASSED " + _pass + "   FAILED " + _fail);
            return _fail;
        }

        // ------------------------------------------------------------ helpers
        private static void Check(string name, bool ok, string detail = null)
        {
            if (ok) _pass++; else _fail++;
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + (detail == null ? "" : "   -> " + detail));
        }

        private static bool Has(List<RuleIssue> issues, string code) { return issues.Any(i => i.Code == code); }
        private static string Codes(List<RuleIssue> issues) { return string.Join(",", issues.Select(i => i.Code)); }

        private static void LoginAs(string role)
        {
            DataTable u = Db.Pull("SELECT id, username, full_name FROM users WHERE role='Admin' ORDER BY id LIMIT 1");
            Session.User = new CurrentUser
            {
                Id = Convert.ToInt32(u.Rows[0]["id"]), Username = u.Rows[0]["username"].ToString(),
                FullName = u.Rows[0]["full_name"].ToString(), Role = role
            };
        }

        private static LicenseFacts Couple(int hAge, int wAge, string hCivil = "Single", string wCivil = "Single",
                                           string hCit = "Filipino", string wCit = "Filipino")
        {
            var l = new LicenseFacts { FiledDate = Today };
            l.Husband.First = "Juan"; l.Husband.Last = Tag + "H"; l.Husband.Dob = Today.AddYears(-hAge).AddDays(-3);
            l.Husband.Citizenship = hCit; l.Husband.CivilStatus = hCivil;
            l.Wife.First = "Maria"; l.Wife.Last = Tag + "W"; l.Wife.Dob = Today.AddYears(-wAge).AddDays(-3);
            l.Wife.Citizenship = wCit; l.Wife.CivilStatus = wCivil;
            return l;
        }

        private static void VerifyAll(int licenseId, string adviceOutcome = "Favorable")
        {
            foreach (ReqRow r in MarriageService.Requirements("License", licenseId))
            {
                r.Status = "Verified";
                r.ReferenceNo = "REF-" + r.Code;
                if (r.Code == "PARENTAL_ADVICE") r.Outcome = adviceOutcome;
                if (r.Code == "PARENTAL_CONSENT" || r.Code == "PARENTAL_ADVICE") r.GivenBy = "Father";
                MarriageService.SaveRequirement(r);
            }
        }

        private static void VerifyMarriageDocs(int marriageId)
        {
            foreach (ReqRow r in MarriageService.Requirements("Marriage", marriageId))
            {
                r.Status = "Verified"; r.ReferenceNo = "DOC-" + r.Code;
                MarriageService.SaveRequirement(r);
            }
        }

        private static void BackdatePosting(int id, int daysAgo)
        {
            Db.Push("UPDATE marriage_licenses SET posting_start=@s WHERE id=@id",
                new MySqlParameter("@s", Today.AddDays(-daysAgo)), new MySqlParameter("@id", id));
            // earliest issue is derived from posting_start; force a recompute
            Db.Push("UPDATE marriage_licenses SET earliest_issue_date=NULL WHERE id=@id", new MySqlParameter("@id", id));
            MarriageService.RecomputeEarliest(id);
        }

        /// <summary>Posting started long enough ago, everything verified, paid -> an issued licence.</summary>
        private static LicenseFacts IssuedLicence(int hAge = 30, int wAge = 28, DateTime? issue = null)
        {
            LicenseFacts l = Couple(hAge, wAge);
            int id = MarriageService.SaveLicense(l);
            MarriageService.StartPosting(id, Today);
            BackdatePosting(id, 20);
            VerifyAll(id);
            MarriageService.RecordPayment(id, "OR-" + Tag, 300m, Today);
            string no;
            List<RuleIssue> iss = MarriageService.IssueLicense(id, Today, out no);
            if (iss.Count > 0) throw new Exception("could not issue test licence: " + Codes(iss));
            if (issue.HasValue)
                Db.Push("UPDATE marriage_licenses SET issue_date=@i, expiry_date=@x WHERE id=@id",
                    new MySqlParameter("@i", issue.Value), new MySqlParameter("@x", MarriageRules.Expiry(issue.Value, MarriageService.Settings)),
                    new MySqlParameter("@id", id));
            return MarriageService.LoadLicense(id);
        }

        private static Dictionary<string, object> Form97(LicenseFacts lic, DateTime marriageDate, DateTime received,
                                                        string basis = "Licensed")
        {
            DataTable place = Db.Pull("SELECT m.id AS mid, m.province_id AS pid FROM municipalities m WHERE m.province_id IS NOT NULL LIMIT 1");
            var v = new Dictionary<string, object>
            {
                { "husband_first_name", lic != null ? lic.Husband.First : "Pedro" },
                { "husband_last_name",  lic != null ? lic.Husband.Last : Tag + "EH" },
                { "wife_first_name",    lic != null ? lic.Wife.First : "Ana" },
                { "wife_last_name",     lic != null ? lic.Wife.Last : Tag + "EW" },
                { "husband_civil_status", "Single" }, { "wife_civil_status", "Single" },
                { "husband_date_of_birth", Today.AddYears(-35) }, { "wife_date_of_birth", Today.AddYears(-33) },
                { "date_of_marriage", marriageDate }, { "received_by_date", received },
                { "place_municipality_id", place.Rows[0]["mid"] }, { "place_province_id", place.Rows[0]["pid"] },
                { "solemnizer", "Hon. Test Judge" }, { "solemnizer_position", "Presiding Judge" },
                { "witness1_name", "Witness One" }, { "witness2_name", "Witness Two" },
                { "license_basis", basis }, { "license_id", lic != null ? (object)lic.Id : null },
            };
            return v;
        }

        // ------------------------------------------------------------ pure rules
        private static void PureRules()
        {
            Console.WriteLine("\n[1] Rule engine (no database)");
            MarriageSettings s = MarriageSettings.Defaults();
            DateTime start = new DateTime(2026, 9, 5);
            Check("posting 05 Sep + 10 days ends 14 Sep", MarriageRules.PostingEnd(start, s) == new DateTime(2026, 9, 14));
            Check("earliest issue 15 Sep", MarriageRules.EarliestIssue(start, false, s) == new DateTime(2026, 9, 15));
            Check("issued 15 Sep 2026 valid until 13 Jan 2027 (120 days)",
                MarriageRules.Expiry(new DateTime(2026, 9, 15), s) == new DateTime(2027, 1, 13));
            Check("advice deferral pushes earliest to 15 Dec 2026",
                MarriageRules.EarliestIssue(start, true, s) == new DateTime(2026, 12, 15));
            Check("age computed on the date, not the year", MarriageRules.AgeOn(new DateTime(2008, 9, 11), new DateTime(2026, 9, 10)) == 17
                                                          && MarriageRules.AgeOn(new DateTime(2008, 9, 10), new DateTime(2026, 9, 10)) == 18);

            var p17 = new Party("Wife") { First = "Liza", Dob = new DateTime(2009, 6, 1) };
            Check("under 18 = hard stop", MarriageRules.AgeFindings(p17, new DateTime(2026, 9, 10), s, "x").Any(i => i.Severity == RuleSeverity.HardStop));
            var p19 = new Party("Wife") { First = "Rosalie", Dob = new DateTime(2007, 1, 1) };
            List<RuleIssue> a19 = MarriageRules.AgeFindings(p19, new DateTime(2026, 9, 10), s, "x");
            Check("19 -> parental consent, human sentence", a19.Any(i => i.Code == "CONSENT_AGE" && i.Message.StartsWith("Parental consent is required for Rosalie because she is 19")), a19.FirstOrDefault()?.Message);
            var p23 = new Party("Husband") { First = "Danilo", Dob = new DateTime(2003, 1, 1) };
            Check("23 -> parental advice", MarriageRules.AgeFindings(p23, new DateTime(2026, 9, 10), s, "x").Any(i => i.Code == "ADVICE_AGE"));
            // Boundary confirmed by the LCRO 2026-09-13: advice is 21-25, so 25 needs it and 26 does not.
            var p25 = new Party("Husband") { First = "Danilo", Dob = new DateTime(2001, 1, 1) };
            Check("25 -> parental advice (band 21-25, confirmed by the office)", MarriageRules.AgeFindings(p25, new DateTime(2026, 9, 10), s, "x").Any(i => i.Code == "ADVICE_AGE"));
            var p26 = new Party("Husband") { First = "Danilo", Dob = new DateTime(2000, 1, 1) };
            Check("26 -> nothing required", MarriageRules.AgeFindings(p26, new DateTime(2026, 9, 10), s, "x").Count == 0);

            var lic = new LicenseFacts { StoredStatus = "Issued", ExpiryDate = Today.AddDays(10) };
            Check("10 days left = Expiring", MarriageRules.LicenseDisplayStatus(lic, Today, s, 0) == "Expiring");
            lic.ExpiryDate = Today.AddDays(90);
            Check("90 days left = Valid", MarriageRules.LicenseDisplayStatus(lic, Today, s, 0) == "Valid");
            lic.ExpiryDate = Today;
            Check("last valid day is still Expiring, not Expired", MarriageRules.LicenseDisplayStatus(lic, Today, s, 0) == "Expiring");
            lic.ExpiryDate = Today.AddDays(-1);
            Check("day after last valid day = Expired", MarriageRules.LicenseDisplayStatus(lic, Today, s, 0) == "Expired");
            Check("15-day reporting: day 15 timely, day 16 delayed",
                !MarriageRules.IsDelayed(start, start.AddDays(15), false, s) && MarriageRules.IsDelayed(start, start.AddDays(16), false, s));
            Check("exempt marriages get 30 days", !MarriageRules.IsDelayed(start, start.AddDays(30), true, s));
        }

        // ------------------------------------------------------------ licences
        private static void LicenceScenarios()
        {
            Console.WriteLine("\n[2] Form 90 licence workflow (live database)");
            MarriageSettings s = MarriageService.Settings;

            // new application
            LicenseFacts l = Couple(30, 28);
            int id = MarriageService.SaveLicense(l);
            List<ReqRow> reqs = MarriageService.Requirements("License", id);
            Check("new application gets a number " + l.ApplicationNo, !string.IsNullOrEmpty(l.ApplicationNo) && l.ApplicationNo.Contains("-LA-"));
            Check("adults, single, Filipino: 7 requirement rows (birth x2, ID x2, CENOMAR x2, RPFP)", reqs.Count == 7, reqs.Count + " rows");
            Check("no consent/advice/counselling rows for 30 & 28", !reqs.Any(r => r.Code.StartsWith("PARENTAL") || r.Code == "COUNSELING"));

            string no;
            List<RuleIssue> iss = MarriageService.IssueLicense(id, Today, out no);
            Check("issue before posting is refused", Has(iss, "NOT_POSTED"), Codes(iss));

            MarriageService.StartPosting(id, Today.AddDays(-5));
            LicenseFacts p = MarriageService.LoadLicense(id);
            Check("posting day 6 of 10", MarriageRules.PostingDay(p, Today, s) == 6);
            Check("earliest issue computed = start + 10", p.EarliestIssue == Today.AddDays(5));
            iss = MarriageService.IssueLicense(id, Today, out no);
            Check("issue during posting refused with the dates", Has(iss, "POSTING") && iss.First(i => i.Code == "POSTING").Message.Contains("earliest"), Codes(iss));
            Check("issue with missing requirement refused, named", Has(iss, "REQ_CENOMAR"), iss.FirstOrDefault(i => i.Code == "REQ_CENOMAR")?.Message);
            Check("issue with no payment refused", Has(iss, "PAYMENT"));

            // under 18
            LicenseFacts u = Couple(30, 17);
            int uid = MarriageService.SaveLicense(u);
            List<RuleIssue> up = MarriageService.StartPosting(uid, Today);
            Check("applicant under 18: posting hard-stopped (RA 11596)", up.Any(i => i.Severity == RuleSeverity.HardStop), Codes(up));

            // consent case
            LicenseFacts c = Couple(26, 19);
            int cid = MarriageService.SaveLicense(c);
            reqs = MarriageService.Requirements("License", cid);
            Check("19-year-old wife: PARENTAL_CONSENT (Wife) + COUNSELING rows",
                reqs.Any(r => r.Code == "PARENTAL_CONSENT" && r.Party == "Wife") && reqs.Any(r => r.Code == "COUNSELING"));
            Check("26-year-old husband: no consent row for him", !reqs.Any(r => r.Code == "PARENTAL_CONSENT" && r.Party == "Husband"));

            // advice case + unfavourable advice deferral
            LicenseFacts a = Couple(23, 27);
            int aid = MarriageService.SaveLicense(a);
            Check("23-year-old: PARENTAL_ADVICE row", MarriageService.Requirements("License", aid).Any(r => r.Code == "PARENTAL_ADVICE" && r.Party == "Husband"));
            MarriageService.StartPosting(aid, Today);
            DateTime before = MarriageService.LoadLicense(aid).EarliestIssue.Value;
            VerifyAll(aid, "Unfavorable");
            LicenseFacts ad = MarriageService.LoadLicense(aid);
            Check("unfavourable advice defers issue three months after posting",
                ad.EarliestIssue.Value == MarriageRules.PostingEnd(Today, s).AddMonths(3).AddDays(1) && ad.EarliestIssue > before,
                MarriageRules.D(before) + " -> " + MarriageRules.D(ad.EarliestIssue) + " (" + ad.DeferralReason + ")");

            // previous marriage + foreigner
            LicenseFacts w = Couple(40, 35, "Single", "Widowed", "American", "Filipino");
            int wid = MarriageService.SaveLicense(w);
            reqs = MarriageService.Requirements("License", wid);
            Check("widowed wife: PREV_MARRIAGE (Wife)", reqs.Any(r => r.Code == "PREV_MARRIAGE" && r.Party == "Wife"));
            Check("American husband: LEGAL_CAPACITY (Husband)", reqs.Any(r => r.Code == "LEGAL_CAPACITY" && r.Party == "Husband"));

            // subsisting marriage -> registrar finding
            LicenseFacts mm = Couple(40, 35, "Married", "Single");
            int mid = MarriageService.SaveLicense(mm);
            MarriageService.StartPosting(mid, Today); BackdatePosting(mid, 20); VerifyAll(mid);
            MarriageService.RecordPayment(mid, "OR-X", 100m, Today);
            iss = MarriageService.IssueLicense(mid, Today, out no);
            Check("civil status Married blocks until registrar records a finding", Has(iss, "IMPEDIMENT"), Codes(iss));
            MarriageService.SetImpedimentNote(mid, "Test: previous marriage declared void, decree seen");
            iss = MarriageService.IssueLicense(mid, Today, out no);
            Check("after registrar finding the licence issues", iss.Count == 0 && no != null, Codes(iss));

            // role gate
            LoginAs("Staff");
            bool refused = false;
            try { MarriageService.IssueLicense(id, Today, out no); } catch (UnauthorizedAccessException) { refused = true; }
            Check("Staff role cannot issue a licence", refused);
            LoginAs("Admin");

            // success
            BackdatePosting(id, 20); VerifyAll(id);
            MarriageService.RecordPayment(id, "OR-" + Tag, 300m, Today);
            iss = MarriageService.IssueLicense(id, Today, out no);
            LicenseFacts ok = MarriageService.LoadLicense(id);
            Check("successful issue: number " + no, iss.Count == 0 && ok.StoredStatus == "Issued" && no != null && no.Contains("-L-"), Codes(iss));
            Check("issue date = actual date (today), not posting end", ok.IssueDate == Today && ok.PostingEnd != Today);
            Check("expiry = issue + 120 days", ok.ExpiryDate == Today.AddDays(120));
            Check("valid licence shows Valid", MarriageRules.LicenseDisplayStatus(ok, Today, s, 0) == "Valid");

            // expiring / expired / sweep
            LicenseFacts exp = IssuedLicence(issue: Today.AddDays(-100));
            Check("issued 100 days ago: Expiring, 20 days left",
                MarriageRules.LicenseDisplayStatus(exp, Today, s, 0) == "Expiring" && MarriageRules.DaysRemaining(exp, Today) == 20);
            LicenseFacts dead = IssuedLicence(issue: Today.AddDays(-130));
            Check("issued 130 days ago: Expired", MarriageRules.LicenseDisplayStatus(dead, Today, s, 0) == "Expired");
            MarriageService.SweepExpired();
            Check("sweep stores Expired, keeps the row", MarriageService.LoadLicense(dead.Id).StoredStatus == "Expired");
            Check("expiry is in the history", MarriageService.HistoryOf("License", dead.Id).AsEnumerable().Any(r => r["Event"].ToString() == "Licence expired"));
        }

        // ------------------------------------------------------------ marriages
        private static void MarriageScenarios()
        {
            Console.WriteLine("\n[3] Form 97 registration workflow (live database)");
            MarriageSettings s = MarriageService.Settings;

            // valid licence -> timely registration
            LicenseFacts lic = IssuedLicence(issue: Today.AddDays(-30));
            int m1 = MarriageService.SaveMarriage(null, Form97(lic, Today.AddDays(-10), Today.AddDays(-2)));
            List<RuleIssue> v = MarriageService.ValidateMarriage(m1);
            Check("Form 97 on a valid licence validates clean", v.Count(i => i.Blocks) == 0, Codes(v));
            string reg;
            List<RuleIssue> r = MarriageService.Register(m1, out reg);
            DataTable row = Db.Pull("SELECT status, registration_type, date_registered, registered_by FROM marriages WHERE id=" + m1);
            Check("registered, registry number " + reg, r.Count == 0 && reg != null && row.Rows[0]["status"].ToString() == "Registered");
            Check("timely registration recorded with date + staff",
                row.Rows[0]["registration_type"].ToString() == "Timely" && row.Rows[0]["date_registered"] != DBNull.Value && row.Rows[0]["registered_by"] != DBNull.Value);
            Check("licence becomes USED, not deleted", MarriageService.LoadLicense(lic.Id).StoredStatus == "Used");
            Check("four copy rows opened, LCRO file copy Filed",
                MarriageService.Copies(m1).Rows.Count == 4 &&
                MarriageService.Copies(m1).AsEnumerable().Any(x => x["copy_type"].ToString() == "Duplicate" && x["status"].ToString() == "Filed"));

            // duplicate use
            bool dup = false;
            try { MarriageService.SaveMarriage(null, Form97(lic, Today.AddDays(-9), Today.AddDays(-1))); }
            catch (InvalidOperationException) { dup = true; }
            Check("a second marriage cannot link the same licence (unique FK)", dup);

            // expired licence
            LicenseFacts old = IssuedLicence(issue: Today.AddDays(-200));
            int m2 = MarriageService.SaveMarriage(null, Form97(old, Today.AddDays(-20), Today.AddDays(-10)));
            v = MarriageService.ValidateMarriage(m2);
            Check("marriage after licence expiry blocked", Has(v, "LIC_EXPIRED"), v.FirstOrDefault(i => i.Code == "LIC_EXPIRED")?.Message);

            // marriage before issue
            LicenseFacts fresh = IssuedLicence(issue: Today.AddDays(-5));
            int m3 = MarriageService.SaveMarriage(null, Form97(fresh, Today.AddDays(-8), Today.AddDays(-1)));
            v = MarriageService.ValidateMarriage(m3);
            Check("marriage before the licence was issued blocked", Has(v, "LIC_BEFORE"));

            // name mismatch
            var mis = Form97(fresh, Today.AddDays(-2), Today.AddDays(-1));
            mis["wife_first_name"] = "Josefina";
            MarriageService.SaveMarriage(m3, mis);
            v = MarriageService.ValidateMarriage(m3);
            Check("applicant names not matching the licence blocked", Has(v, "MISMATCH_W"), v.FirstOrDefault(i => i.Code == "MISMATCH_W")?.Message);

            // licence-exempt (Art. 34)
            var ex = Form97(null, Today.AddDays(-5), Today.AddDays(-1), "Exempt");
            int m4 = MarriageService.SaveMarriage(null, ex);
            v = MarriageService.ValidateMarriage(m4);
            Check("exempt without basis / affidavit / review blocked",
                Has(v, "EXEMPT_BASIS") && Has(v, "REVIEW") && Has(v, "MREQ_EXEMPT_AFFIDAVIT"), Codes(v));
            ex["exemption_basis"] = "ART34";
            ex["wife_civil_status"] = "Widowed";
            MarriageService.SaveMarriage(m4, ex);
            v = MarriageService.ValidateMarriage(m4);
            Check("widowed wife, no licence file: exact previous-marriage message",
                v.Any(i => i.Code == "MREQ_PREV_MARRIAGE" && i.Message.StartsWith("Wife's civil status is Widowed, but no supporting previous-marriage document has been recorded")),
                v.FirstOrDefault(i => i.Code == "MREQ_PREV_MARRIAGE")?.Message);
            Check("...and it points at Consent & License", v.First(i => i.Code == "MREQ_PREV_MARRIAGE").FixWhere == "Consent & License");
            VerifyMarriageDocs(m4);
            MarriageService.RegistrarReview(m4, true, "Art. 34 affidavit examined");
            v = MarriageService.ValidateMarriage(m4);
            Check("exempt with basis, affidavit, review: clean", v.Count(i => i.Blocks) == 0, Codes(v));
            r = MarriageService.Register(m4, out reg);
            Check("licence-exempt marriage registered with no fake licence",
                r.Count == 0 && Db.Pull("SELECT license_id FROM marriages WHERE id=" + m4).Rows[0][0] == DBNull.Value);

            // delayed registration
            LicenseFacts dl = IssuedLicence(issue: Today.AddDays(-110));
            int m5 = MarriageService.SaveMarriage(null, Form97(dl, Today.AddDays(-100), Today.AddDays(-1)));
            v = MarriageService.ValidateMarriage(m5);
            Check("received 99 days after the marriage: delayed path",
                Has(v, "DELAY_REASON") && Has(v, "DELAY_POSTING") && Has(v, "REVIEW") && Has(v, "MREQ_DELAYED_AFFIDAVIT"), Codes(v));
            var d5 = Form97(dl, Today.AddDays(-100), Today.AddDays(-1));
            d5["delay_reason"] = "Solemnizing officer's secretary misplaced the copies";
            MarriageService.SaveMarriage(m5, d5);
            MarriageService.StartCasePosting(m5, Today);
            VerifyMarriageDocs(m5);
            MarriageService.RegistrarReview(m5, true, "Affidavits in order");
            v = MarriageService.ValidateMarriage(m5);
            Check("delayed notice still posted: registration waits", Has(v, "DELAY_POSTING_OPEN"), Codes(v));
            MarriageService.StartCasePosting(m5, Today.AddDays(-s.DelayedPostingDays));
            r = MarriageService.Register(m5, out reg);
            Check("delayed registration completes and is typed Delayed",
                r.Count == 0 && Db.Pull("SELECT registration_type FROM marriages WHERE id=" + m5).Rows[0][0].ToString() == "Delayed", Codes(r));

            // OCR
            LicenseFacts ol = IssuedLicence(issue: Today.AddDays(-20));
            int m6 = MarriageService.SaveMarriage(null, Form97(ol, Today.AddDays(-3), Today.AddDays(-1)));
            MarriageService.SetOcrContext(m6, "SCN-TEST", 45, 6, true);
            v = MarriageService.ValidateMarriage(m6);
            Check("OCR weak fields hold registration", Has(v, "OCR"), v.FirstOrDefault(i => i.Code == "OCR")?.Message);
            MarriageService.MarkOcrReviewed(m6);
            Check("after OCR review it validates clean", MarriageService.ValidateMarriage(m6).Count(i => i.Blocks) == 0);
            LicenseFacts hl = IssuedLicence(issue: Today.AddDays(-20));
            int m7 = MarriageService.SaveMarriage(null, Form97(hl, Today.AddDays(-3), Today.AddDays(-1)));
            MarriageService.SetOcrContext(m7, "SCN-GOOD", 97, 0, false);
            Check("high-confidence scan needs no review", !Has(MarriageService.ValidateMarriage(m7), "OCR"));

            // under 18 on Form 97
            var kid = Form97(null, Today.AddDays(-3), Today.AddDays(-1), "Exempt");
            kid["wife_date_of_birth"] = Today.AddYears(-16);
            int m8 = MarriageService.SaveMarriage(null, kid);
            Check("party under 18 on Form 97 is a hard stop",
                MarriageService.ValidateMarriage(m8).Any(i => i.Severity == RuleSeverity.HardStop));

            // copies
            DataRow orig = MarriageService.Copies(m1).AsEnumerable().First(x => x["copy_type"].ToString() == "Original");
            MarriageService.UpdateCopy(Convert.ToInt32(orig["id"]), "Released", "Juan (husband)", Today, null, "confirmed by officer");
            Check("copy distribution update", MarriageService.Copies(m1).AsEnumerable().First(x => x["copy_type"].ToString() == "Original")["status"].ToString() == "Released");

            // PSA
            Console.WriteLine("\n[4] PSA / OCRG transmittal");
            string bn;
            int b = MarriageService.CreateBatch(new List<int> { m1, m4, m5 }, Today.Year, Today.Month, out bn);
            Batches.Add(b);
            string st, bno;
            st = MarriageService.PsaStatus(m1, out bno);
            Check("batch " + bn + " created, record In Batch", st == "In Batch" && bno == bn);
            bool twice = false;
            try { int b2 = MarriageService.CreateBatch(new List<int> { m1 }, Today.Year, Today.Month, out bn); Batches.Add(b2); }
            catch (InvalidOperationException) { twice = true; }
            Check("record already in a batch cannot be batched again", twice);
            bool unreg = false;
            try { MarriageService.CreateBatch(new List<int> { m2 }, Today.Year, Today.Month, out bn); } catch (InvalidOperationException) { unreg = true; }
            Check("unregistered record cannot be transmitted", unreg);
            MarriageService.MarkBatchSent(b, Today, "Physical Batch", "PSA Cagayan Provincial Statistical Office", "TR-" + Tag, null);
            Check("marked sent: record Sent to PSA/OCRG, NOT 'available in PSA'",
                MarriageService.PsaStatus(m1, out bno) == "Sent to PSA/OCRG" &&
                Db.Pull("SELECT psa_available_reference FROM marriages WHERE id=" + m1).Rows[0][0] == DBNull.Value);
            Check("triplicate copy follows the batch to Sent",
                MarriageService.Copies(m1).AsEnumerable().First(x => x["copy_type"].ToString() == "Triplicate")["status"].ToString() == "Sent");
            MarriageService.AcknowledgeBatch(b, Today, "ACK-" + Tag);
            Check("acknowledged", MarriageService.PsaStatus(m4, out bno) == "Acknowledged");
            MarriageService.ReturnItem(m5, "Wife's middle name illegible");
            Check("returned for correction", MarriageService.PsaStatus(m5, out bno) == "Returned - Needs Correction");
            int b3 = MarriageService.CreateBatch(new List<int> { m5 }, Today.Year, Today.Month, out bn);
            Batches.Add(b3);
            Check("corrected record can go into a new batch " + bn, MarriageService.PsaStatus(m5, out bno) == "In Batch");
            bool needRef = false;
            try { MarriageService.ConfirmPsaAvailability(m1, Today, " "); } catch (ArgumentException) { needRef = true; }
            Check("PSA availability cannot be claimed without an authoritative reference", needRef);
            Check("full history kept for the registered marriage", MarriageService.HistoryOf("Marriage", m1).Rows.Count >= 5,
                MarriageService.HistoryOf("Marriage", m1).Rows.Count + " events");
        }

        // ------------------------------------------------------------ Form 90 blocks (migration 38)
        /// <summary>
        /// Phase 3A: every new Form 90 applicant column saved through MarriageService.SaveLicense
        /// itself and read back RAW from the table (not through LoadLicense, which could hide a
        /// column that was never written), then the screen driven and rendered.
        /// </summary>
        private static void Form90Blocks(string dir)
        {
            LicenseFacts l = Couple(30, 27, "Single", "Widowed");
            Party h = l.Husband, w = l.Wife;
            // Two-word surname on purpose: a joined "Jose Santos Dela Cruz" cannot be split back.
            h.FatherFirst = "Jose"; h.FatherMiddle = "Santos"; h.FatherLast = "Dela Cruz"; h.FatherCitizenship = "Filipino"; h.FatherResidence = "Bical, Penablanca, Cagayan";
            h.MotherFirst = "Ana"; h.MotherMiddle = "Reyes"; h.MotherLast = "De Guzman"; h.MotherCitizenship = "Filipino"; h.MotherResidence = "Bical, Penablanca, Cagayan";
            h.ConsentFirst = "Ramon"; h.ConsentMiddle = "Lim"; h.ConsentLast = "Pascua"; h.ConsentRelationship = "Guardian"; h.ConsentCitizenship = "Filipino"; h.ConsentResidence = "Tuguegarao City";
            // Husband is Single: whatever sits in his previously-married block must NOT be written.
            h.PrevDissolution = "Annulment"; h.PrevDissolvedProvince = "Cagayan"; h.PrevDissolvedMunicipality = "Tuguegarao City"; h.PrevDissolvedDate = Today.AddYears(-3);
            w.FatherFirst = "Pedro"; w.FatherLast = "Quilang"; w.FatherCitizenship = "Filipino"; w.FatherResidence = "Callao, Penablanca";
            w.MotherFirst = "Luz"; w.MotherMiddle = "Articulo"; w.MotherLast = "Baloso"; w.MotherCitizenship = "Japanese"; w.MotherResidence = "Osaka, Japan";
            w.ConsentFirst = "Luz"; w.ConsentMiddle = "Articulo"; w.ConsentLast = "Baloso"; w.ConsentRelationship = "Mother"; w.ConsentCitizenship = "Japanese"; w.ConsentResidence = "Osaka, Japan";
            w.PrevDissolution = "Death of spouse"; w.PrevDissolvedProvince = "Cagayan"; w.PrevDissolvedMunicipality = "Tuguegarao City"; w.PrevDissolvedDate = new DateTime(2023, 5, 14);

            int id = MarriageService.SaveLicense(l);
            DataRow r = Db.Pull("SELECT * FROM marriage_licenses WHERE id = @id", new MySqlParameter("@id", id)).Rows[0];
            Func<string, string> s = c => r[c] == DBNull.Value ? null : r[c].ToString();
            var expect = new Dictionary<string, string>
            {
                { "husband_father_first_name", "Jose" }, { "husband_father_middle_name", "Santos" }, { "husband_father_last_name", "Dela Cruz" },
                { "husband_father_citizenship", "Filipino" }, { "husband_father_residence", "Bical, Penablanca, Cagayan" },
                { "husband_mother_first_name", "Ana" }, { "husband_mother_middle_name", "Reyes" }, { "husband_mother_last_name", "De Guzman" },
                { "husband_mother_citizenship", "Filipino" }, { "husband_mother_residence", "Bical, Penablanca, Cagayan" },
                { "husband_consent_first_name", "Ramon" }, { "husband_consent_middle_name", "Lim" }, { "husband_consent_last_name", "Pascua" },
                { "husband_consent_relationship", "Guardian" }, { "husband_consent_citizenship", "Filipino" }, { "husband_consent_residence", "Tuguegarao City" },
                { "husband_prev_dissolution", null }, { "husband_prev_dissolved_municipality", null }, { "husband_prev_dissolved_province", null }, { "husband_prev_dissolved_date", null },
                { "wife_father_first_name", "Pedro" }, { "wife_father_middle_name", null }, { "wife_father_last_name", "Quilang" },
                { "wife_father_citizenship", "Filipino" }, { "wife_father_residence", "Callao, Penablanca" },
                { "wife_mother_first_name", "Luz" }, { "wife_mother_middle_name", "Articulo" }, { "wife_mother_last_name", "Baloso" },
                { "wife_mother_citizenship", "Japanese" }, { "wife_mother_residence", "Osaka, Japan" },
                { "wife_consent_first_name", "Luz" }, { "wife_consent_middle_name", "Articulo" }, { "wife_consent_last_name", "Baloso" },
                { "wife_consent_relationship", "Mother" }, { "wife_consent_citizenship", "Japanese" }, { "wife_consent_residence", "Osaka, Japan" },
                { "wife_prev_dissolution", "Death of spouse" }, { "wife_prev_dissolved_municipality", "Tuguegarao City" },
                { "wife_prev_dissolved_province", "Cagayan" },
                // retired joined columns: not written
                { "husband_father_name", null }, { "husband_mother_name", null }, { "wife_father_name", null }, { "wife_mother_name", null },
            };
            int bad = 0;
            foreach (var kv in expect)
                if (s(kv.Key) != kv.Value) { bad++; Check("column " + kv.Key, false, "got '" + s(kv.Key) + "' expected '" + kv.Value + "'"); }
            Check("all " + expect.Count + " checked columns read back as saved (" + (expect.Count - 4) + " new + 4 retired)", bad == 0, bad + " wrong");
            Check("wife_prev_dissolved_date = 2023-05-14", r["wife_prev_dissolved_date"] != DBNull.Value && Convert.ToDateTime(r["wife_prev_dissolved_date"]) == new DateTime(2023, 5, 14));
            Check("Single husband's previously-married block written as NULL (server guard)",
                  s("husband_prev_dissolution") == null && r["husband_prev_dissolved_date"] == DBNull.Value);
            // Named explicitly: a LIKE pattern here once missed every *_citizenship / *_residence.
            var names38 = new List<string>();
            foreach (string pre in new[] { "husband", "wife" })
            {
                foreach (string who in new[] { "father", "mother" })
                    foreach (string c in new[] { "first_name", "middle_name", "last_name", "citizenship", "residence" }) names38.Add(pre + "_" + who + "_" + c);
                foreach (string c in new[] { "first_name", "middle_name", "last_name", "relationship", "citizenship", "residence" }) names38.Add(pre + "_consent_" + c);
                foreach (string c in new[] { "dissolution", "dissolved_municipality", "dissolved_province", "dissolved_date" }) names38.Add(pre + "_prev_" + c);
            }
            int newCols = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'marriage_licenses' " +
                "AND COLUMN_NAME IN ('" + string.Join("','", names38) + "')").Rows[0][0]);
            Check("migration 38 columns present: 40", newCols == 40, newCols.ToString());

            LicenseFacts back = MarriageService.LoadLicense(id);
            Check("joined father for Form 97 keeps the two-word surname", back.Husband.Father == "Jose Santos Dela Cruz", back.Husband.Father);
            Check("LoadLicense round-trips the wife's previously-married block",
                  back.Wife.PrevDissolution == "Death of spouse" && back.Wife.PrevDissolvedProvince == "Cagayan" && back.Wife.PrevDissolvedDate == new DateTime(2023, 5, 14));

            // Legacy fallback: a pre-38 licence shows its joined name whole in the LAST cell.
            DataTable legacy = Db.Pull("SELECT id, husband_father_name FROM marriage_licenses WHERE husband_father_name IS NOT NULL " +
                                       "AND husband_father_first_name IS NULL AND husband_father_last_name IS NULL AND husband_last_name NOT LIKE 'ZZT%' LIMIT 1");
            if (legacy.Rows.Count > 0)
            {
                LicenseFacts old = MarriageService.LoadLicense(Convert.ToInt32(legacy.Rows[0]["id"]));
                string joined = legacy.Rows[0]["husband_father_name"].ToString();
                Check("legacy licence " + legacy.Rows[0]["id"] + ": joined father shown whole in Last, not split", old.Husband.FatherLast == joined && old.Husband.FatherFirst == null, joined);
            }

            // Update path: the wife's status changes to Single -> the block is cleared on save.
            back.Wife.CivilStatus = "Single";
            MarriageService.SaveLicense(back);
            DataRow r2 = Db.Pull("SELECT wife_prev_dissolution, wife_prev_dissolved_date, wife_consent_last_name FROM marriage_licenses WHERE id = @id",
                                 new MySqlParameter("@id", id)).Rows[0];
            Check("update to Single clears the previously-married block, keeps the rest",
                  r2["wife_prev_dissolution"] == DBNull.Value && r2["wife_prev_dissolved_date"] == DBNull.Value && r2["wife_consent_last_name"].ToString() == "Baloso");

            // ---- screen: wife Widowed again so both states render side by side
            back = MarriageService.LoadLicense(id);
            back.Wife.CivilStatus = "Widowed"; back.Wife.PrevDissolution = "Death of spouse"; back.Wife.PrevDissolvedProvince = "Cagayan";
            back.Wife.PrevDissolvedMunicipality = "Tuguegarao City"; back.Wife.PrevDissolvedDate = new DateTime(2023, 5, 14);
            MarriageService.SaveLicense(back);

            System.IO.Directory.CreateDirectory(dir);
            System.Windows.Forms.Application.EnableVisualStyles();
            var asm = typeof(MarriageService).Assembly;
            var bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var f = (System.Windows.Forms.Form)Activator.CreateInstance(asm.GetType("CROMS.Forms.MarriageLicenseForm", true), bf, null, new object[] { (int?)id }, null);
            f.StartPosition = System.Windows.Forms.FormStartPosition.Manual; f.Location = new System.Drawing.Point(-4000, -4000); f.ShowInTaskbar = false;
            f.Show();
            Invoke(f, "ShowStep", 0);
            for (int i = 0; i < 5; i++) System.Windows.Forms.Application.DoEvents();
            var page0 = ((System.Windows.Forms.Panel[])f.GetType().GetField("_pages", bf).GetValue(f))[0];
            var focused = f.ActiveControl;
            while (focused is System.Windows.Forms.ContainerControl && ((System.Windows.Forms.ContainerControl)focused).ActiveControl != null)
                focused = ((System.Windows.Forms.ContainerControl)focused).ActiveControl;
            Check("screen: opens at the top of the page, not scrolled to a lower box", page0.VerticalScroll.Value == 0,
                  "scroll=" + page0.VerticalScroll.Value + " focus=" + (focused == null ? "none" : focused.GetType().Name + " '" + focused.Text + "'"));
            object hb = f.GetType().GetField("_h", bf).GetValue(f), wb = f.GetType().GetField("_w", bf).GetValue(f);
            Func<object, string, object> fld = (o, n) => o.GetType().GetField(n).GetValue(o);
            var rows = (System.Windows.Forms.Control[])fld(hb, "PrevRows");
            var wrows = (System.Windows.Forms.Control[])fld(wb, "PrevRows");
            Check("screen: Single husband's previously-married rows greyed", rows.All(x => !x.Enabled));
            Check("screen: Widowed wife's rows live and loaded", wrows.All(x => x.Enabled) && ((System.Windows.Forms.ComboBox)fld(wb, "PrevHow")).Text == "Death of spouse",
                  ((System.Windows.Forms.ComboBox)fld(wb, "PrevHow")).Text);
            Check("screen: father cells loaded as three cells", ((System.Windows.Forms.TextBox)fld(hb, "FLast")).Text == "Dela Cruz");
            int partyH = (int)f.GetType().GetField("_partyHeight", bf).GetValue(f);
            Console.WriteLine("  info  measured party card height = " + partyH);

            // Tall enough that the whole applicants page draws without scrolling.
            SnapOpen(f, 1220, 780, dir + "\\f90_applicants_top.png");
            SnapOpen(f, 1220, partyH + 420, dir + "\\f90_applicants_full.png");
            // Scroll the page to the bottom so the blocks are seen at the real window size.
            var page = ((System.Windows.Forms.Panel[])f.GetType().GetField("_pages", bf).GetValue(f))[0];
            // AutoScrollPosition, not VerticalScroll.Value - the latter moves only the bar.
            page.AutoScrollPosition = new System.Drawing.Point(0, 5000);
            foreach (string n in new[] { "PrevDate", "PrevHow", "PrevMunicipality", "Dob" })
            {
                var c = (System.Windows.Forms.Control)fld(wb, n);
                Console.WriteLine("  info  " + n + " bounds=" + c.Bounds + " parentW=" + c.Parent.ClientSize.Width + " margin=" + c.Margin);
            }
            System.Drawing.Rectangle dr = ((System.Windows.Forms.Control)fld(wb, "PrevDate")).Bounds;
            var dparent = ((System.Windows.Forms.Control)fld(wb, "PrevDate")).Parent;
            Check("screen: date dissolved stays inside its cell", dr.Right <= dparent.ClientSize.Width, dr.Right + " vs " + dparent.ClientSize.Width);
            var dob = (System.Windows.Forms.Control)fld(wb, "Dob");
            Check("screen: date of birth stays inside its cell (arrow visible)", dob.Bounds.Right <= dob.Parent.ClientSize.Width, dob.Bounds.Right + " vs " + dob.Parent.ClientSize.Width);
            // The window cannot outgrow the screen and DrawToBitmap ignores the page scroll, so the
            // two party cards are drawn on their own at their full measured height.
            var colsHost = wrows[0].Parent.Parent.Parent;
            using (var bmp = new System.Drawing.Bitmap(colsHost.Width, colsHost.Height))
            {
                colsHost.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, colsHost.Width, colsHost.Height));
                bmp.Save(dir + "\\f90_party_cards.png", System.Drawing.Imaging.ImageFormat.Png);
            }
            Console.WriteLine("  ok  f90_party_cards.png (" + colsHost.Width + "x" + colsHost.Height + ")");
            Check("screen: card container is exactly the measured height", colsHost.Height == partyH, colsHost.Height + " vs " + partyH);

            // Drive the wife's civil status to Single on screen: block must grey AND clear.
            ((System.Windows.Forms.ComboBox)fld(wb, "Civil")).SelectedItem = "Single";
            System.Windows.Forms.Application.DoEvents();
            var dp = (System.Windows.Forms.DateTimePicker)fld(wb, "PrevDate");
            Check("screen: switching wife to Single greys and clears the block",
                  wrows.All(x => !x.Enabled) && ((System.Windows.Forms.ComboBox)fld(wb, "PrevHow")).Text == "" &&
                  ((System.Windows.Forms.ComboBox)fld(wb, "PrevProvince")).Text == "" && !(dp.ShowCheckBox && dp.Checked),
                  "how='" + ((System.Windows.Forms.ComboBox)fld(wb, "PrevHow")).Text + "' note='" + ((System.Windows.Forms.Label)fld(wb, "PrevNote")).Text + "'");
            SnapOpen(f, 1220, partyH + 420, dir + "\\f90_wife_single.png");
            f.FormClosing += (x, e) => { };
            typeof(System.Windows.Forms.Form).GetMethod("Dispose", new Type[0]).Invoke(f, null);
        }

        // ------------------------------------------------------------ MF-90 print (Crystal)
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr w, IntPtr l);

        /// <summary>
        /// Phase 3B: the application printed on the real Municipal Form 90. A complete licence is
        /// saved through SaveLicense, turned into the report row, rendered BY CRYSTAL to PDF (the
        /// same .rpt the app ships) and by the fallback renderer to PNG, and the viewer's
        /// Ctrl+wheel zoom is driven with a real Ctrl key state and a real WM_MOUSEWHEEL.
        /// </summary>
        private static void Mf90Print(string dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            LicenseFacts l = Couple(30, 27, "Single", "Widowed");
            Party h = l.Husband, w = l.Wife;
            h.First = "Ryan"; h.Middle = "Panlilio"; h.Sex = "Male"; h.BirthCountry = "Philippines"; h.PlaceOfBirth = "Tuguegarao City, Cagayan";
            h.Religion = "Roman Catholic"; h.Residence = "Bical, Penablanca, Cagayan";
            h.FatherFirst = "Jose"; h.FatherMiddle = "Santos"; h.FatherLast = "Dela Cruz"; h.FatherCitizenship = "Filipino"; h.FatherResidence = "Bical, Penablanca, Cagayan";
            h.MotherFirst = "Ana"; h.MotherMiddle = "Reyes"; h.MotherLast = "De Guzman"; h.MotherCitizenship = "Filipino"; h.MotherResidence = "Bical, Penablanca, Cagayan";
            w.First = "Tofie Fae"; w.Middle = "Cadava"; w.Sex = "Female"; w.BirthCountry = "Japan"; w.PlaceOfBirth = "Osaka";
            w.Religion = "Iglesia ni Cristo"; w.Residence = "Callao, Penablanca, Cagayan";
            w.FatherFirst = "Pedro"; w.FatherLast = "Quilang"; w.FatherCitizenship = "Filipino"; w.FatherResidence = "Callao, Penablanca";
            w.MotherFirst = "Luz"; w.MotherMiddle = "Articulo"; w.MotherLast = "Baloso"; w.MotherCitizenship = "Japanese"; w.MotherResidence = "Osaka, Japan";
            w.ConsentFirst = "Luz"; w.ConsentMiddle = "Articulo"; w.ConsentLast = "Baloso"; w.ConsentRelationship = "Mother"; w.ConsentCitizenship = "Japanese"; w.ConsentResidence = "Osaka, Japan";
            w.PrevDissolution = "Death of spouse"; w.PrevDissolvedProvince = "Cagayan"; w.PrevDissolvedMunicipality = "Tuguegarao City"; w.PrevDissolvedDate = new DateTime(2023, 5, 14);
            int id = MarriageService.SaveLicense(l);
            LicenseFacts back = MarriageService.LoadLicense(id);

            DataTable t = Mf90Form.BuildTable(back);
            DataRow row = t.Rows[0];
            Check("report row has one column per printed box (" + Mf90Form.Cells.Count + ")", t.Columns.Count == Mf90Form.Cells.Count);
            Check("wife applies 'with' the husband, husband 'with' the wife",
                  (string)row["wife_apply_with"] == back.Husband.FullName && (string)row["husband_apply_with"] == back.Wife.FullName);
            Check("date of birth split into Day / Month / Year / Age on the filing date",
                  (string)row["husband_dob_month"] == back.Husband.Dob.Value.ToString("MMMM") && (string)row["husband_age"] == "30");
            Check("foreign birth: country carried in the province box", (string)row["wife_birth_city"] == "Osaka" && (string)row["wife_birth_province"] == "Japan",
                  row["wife_birth_city"] + " / " + row["wife_birth_province"]);
            Check("previously married printed for the widowed wife only",
                  (string)row["wife_prev_month"] == "May" && (string)row["husband_prev_dissolution"] == "");
            Check("registry no. left blank (the office's number, not CROMS's)", (string)row["registry_no"] == "");

            var asm = typeof(MarriageService).Assembly;
            var bfs = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            string rpt = Mf90Form.ReportPath;
            Check("report deployed beside the exe: " + rpt, System.IO.File.Exists(rpt));
            Type runner = asm.GetType("CROMS.Data.CrystalRunner", true);
            string pdf = System.IO.Path.Combine(dir, "mf90_crystal.pdf");
            if (System.IO.File.Exists(pdf)) System.IO.File.Delete(pdf);
            runner.GetMethod("ExportTablePdf", bfs).Invoke(null, new object[] { rpt, t, pdf });
            Check("Crystal rendered the application to PDF", System.IO.File.Exists(pdf) && new System.IO.FileInfo(pdf).Length > 0);

            // Fallback renderer, same cells, at 2x so the text can be read in the PNG.
            using (var bmp = new System.Drawing.Bitmap(1224, 1872))
            using (var blank = System.Drawing.Image.FromFile(Mf90Form.BlankPath))
            {
                // DPI BEFORE the Graphics: a Graphics takes the bitmap's DPI when it is created,
                // so setting it afterwards draws the page at 96 DPI into a 144 DPI canvas.
                bmp.SetResolution(144, 144);
                using (var g = System.Drawing.Graphics.FromImage(bmp)) {
                g.Clear(System.Drawing.Color.White);
                Mf90Form.Draw(g, t, blank); }
                bmp.Save(System.IO.Path.Combine(dir, "mf90_builtin.png"), System.Drawing.Imaging.ImageFormat.Png);
            }
            Console.WriteLine("  ok  mf90_builtin.png");

            // Long value -> the preview warns instead of silently clipping.
            DataTable longT = t.Copy();
            longT.Rows[0]["husband_residence"] = "Purok 7, Barangay San Roque Extension, Poblacion East, Municipality of Penablanca, Cagayan";
            Check("an over-long value is reported before printing", Mf90Form.Overflows(longT).Count == 1 && Mf90Form.Overflows(t).Count == 0,
                  string.Join(" | ", Mf90Form.Overflows(longT)));

            // ---- the viewer: real window, real Ctrl key state, real WM_MOUSEWHEEL.
            System.Windows.Forms.Application.EnableVisualStyles();
            var viewer = (System.Windows.Forms.Form)runner.GetMethod("OpenTable", bfs).Invoke(null, new object[] { rpt, t, "MF-90 test", null });
            viewer.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            viewer.Location = new System.Drawing.Point(40, 20); viewer.ShowInTaskbar = false; viewer.TopMost = true;
            viewer.Size = new System.Drawing.Size(1000, 1000);
            viewer.Show();
            for (int i = 0; i < 40; i++) { System.Windows.Forms.Application.DoEvents(); System.Threading.Thread.Sleep(50); }
            Func<int> zoom = () => (int)viewer.GetType().GetProperty("ZoomPercent").GetValue(viewer);
            int fit = zoom();
            Check("viewer opens at fit-page zoom", fit >= 30 && fit <= 100, fit + "%");
            SnapScreen(viewer, System.IO.Path.Combine(dir, "mf90_viewer_fit.png"));

            System.Windows.Forms.Control target = null;
            foreach (System.Windows.Forms.Control c in viewer.Controls) if (c.GetType().Name == "CrystalReportViewer") target = c;
            var center = target.PointToScreen(new System.Drawing.Point(target.Width / 2, target.Height / 2));
            IntPtr lp = new IntPtr((center.Y << 16) | (center.X & 0xFFFF));
            Action<int> wheel = delta =>
            {
                keybd_event(0x11, 0, 0, UIntPtr.Zero);                      // Ctrl down
                System.Windows.Forms.Application.DoEvents();
                PostMessage(target.Handle, 0x020A, new IntPtr((delta & 0xFFFF) << 16), lp);
                for (int i = 0; i < 6; i++) { System.Windows.Forms.Application.DoEvents(); System.Threading.Thread.Sleep(30); }
                keybd_event(0x11, 0, 2, UIntPtr.Zero);                      // Ctrl up
                System.Windows.Forms.Application.DoEvents();
            };
            wheel(120); wheel(120); wheel(120);
            int zoomedIn = zoom();
            Check("Ctrl + wheel up zooms in", zoomedIn > fit, fit + "% -> " + zoomedIn + "%");
            for (int i = 0; i < 20; i++) { System.Windows.Forms.Application.DoEvents(); System.Threading.Thread.Sleep(40); }
            SnapScreen(viewer, System.IO.Path.Combine(dir, "mf90_viewer_zoomed.png"));
            wheel(-120);
            Check("Ctrl + wheel down zooms out", zoom() < zoomedIn, zoomedIn + "% -> " + zoom() + "%");
            // Plain wheel (no Ctrl) must NOT zoom - it scrolls.
            int before = zoom();
            PostMessage(target.Handle, 0x020A, new IntPtr((-120 & 0xFFFF) << 16), lp);
            for (int i = 0; i < 6; i++) { System.Windows.Forms.Application.DoEvents(); System.Threading.Thread.Sleep(30); }
            Check("plain wheel does not zoom", zoom() == before, before + "% -> " + zoom() + "%");
            viewer.GetType().GetMethod("FitPage").Invoke(viewer, null);
            Check("Fit page returns to the fitted zoom", zoom() == fit, zoom() + "%");
            viewer.Close();
            viewer.Dispose();
        }

        private static void SnapScreen(System.Windows.Forms.Form f, string file)
        {
            f.Activate();
            for (int i = 0; i < 10; i++) { System.Windows.Forms.Application.DoEvents(); System.Threading.Thread.Sleep(40); }
            var r = f.Bounds;
            using (var bmp = new System.Drawing.Bitmap(r.Width, r.Height))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(r.Location, System.Drawing.Point.Empty, r.Size);
                bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
            }
            Console.WriteLine("  ok  " + System.IO.Path.GetFileName(file));
        }

        private static void SnapOpen(System.Windows.Forms.Form f, int w, int h, string file)
        {
            f.Size = new System.Drawing.Size(w + (f.Width - f.ClientSize.Width), h + (f.Height - f.ClientSize.Height));
            for (int i = 0; i < 5; i++) System.Windows.Forms.Application.DoEvents();
            using (var bmp = new System.Drawing.Bitmap(f.Width, f.Height))
            {
                f.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, f.Width, f.Height));
                bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
            }
            Console.WriteLine("  ok  " + System.IO.Path.GetFileName(file));
        }

        // ------------------------------------------------------------ render
        /// <summary>
        /// Seeds a realistic desk (couples at different points on the clocks), opens every
        /// marriage window off-screen exactly as the app would, and saves each as a PNG so the
        /// layout can be LOOKED AT rather than assumed from a clean compile.
        /// </summary>
        private static void Render(string dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            System.Windows.Forms.Application.EnableVisualStyles();
            var asm = typeof(MarriageService).Assembly;

            // posting, day 6, 19-year-old wife, counselling missing
            LicenseFacts a = Couple(26, 19);
            a.Husband.First = "Danilo"; a.Wife.First = "Rosalie";
            int aid = MarriageService.SaveLicense(a);
            MarriageService.StartPosting(aid, Today.AddDays(-5));
            foreach (ReqRow r in MarriageService.Requirements("License", aid))
                if (r.Code != "COUNSELING") { r.Status = r.Code == "CENOMAR" ? "Submitted" : "Verified"; r.GivenBy = r.Code == "PARENTAL_CONSENT" ? "Father - Ramon Pascua" : null; MarriageService.SaveRequirement(r); }
            // ready to issue
            LicenseFacts rdy = Couple(31, 29);
            int rid = MarriageService.SaveLicense(rdy);
            MarriageService.StartPosting(rid, Today); BackdatePosting(rid, 12); VerifyAll(rid);
            MarriageService.RecordPayment(rid, "OR-7781234", 300m, Today);
            // valid + expiring
            LicenseFacts valid = IssuedLicence(issue: Today.AddDays(-23));
            IssuedLicence(issue: Today.AddDays(-112));
            // Form 97 draft on the valid licence, widowed wife, weak OCR
            var draft = Form97(valid, Today.AddDays(-4), Today.AddDays(-1));
            draft["wife_civil_status"] = "Widowed";
            int mid = MarriageService.SaveMarriage(null, draft);
            MarriageService.SetOcrContext(mid, "SCN-260911-004", 62, 6, true);
            // registered + in PSA queue
            LicenseFacts used = IssuedLicence(issue: Today.AddDays(-40));
            int regId = MarriageService.SaveMarriage(null, Form97(used, Today.AddDays(-12), Today.AddDays(-3)));
            string reg; MarriageService.Register(regId, out reg);
            // delayed + exempt case
            var ex = Form97(null, Today.AddDays(-120), Today.AddDays(-2), "Exempt");
            ex["exemption_basis"] = "ART34";
            int caseId = MarriageService.SaveMarriage(null, ex);

            Snap(asm, "CROMS.Forms.MarriageRegistrationForm", new object[0], dir + "\\01_desk.png", 1449, 960, null);
            Snap(asm, "CROMS.Forms.MarriageLicenseForm", new object[] { (int?)aid }, dir + "\\02_form90_applicants.png", 1220, 780, f => Step(f, 0));
            Snap(asm, "CROMS.Forms.MarriageLicenseForm", new object[] { (int?)aid }, dir + "\\03_form90_requirements.png", 1220, 780, f => Step(f, 1));
            Snap(asm, "CROMS.Forms.MarriageLicenseForm", new object[] { (int?)aid }, dir + "\\04_form90_consent.png", 1220, 780, f => Step(f, 2));
            Snap(asm, "CROMS.Forms.MarriageLicenseForm", new object[] { (int?)aid }, dir + "\\05_form90_posting.png", 1220, 780, f => Step(f, 3));
            Snap(asm, "CROMS.Forms.MarriageLicenseForm", new object[] { (int?)aid }, dir + "\\06_form90_issue_blocked.png", 1220, 780, f => Step(f, 4));
            Snap(asm, "CROMS.Forms.IssueLicenseForm", new object[] { MarriageService.LoadLicense(rid) }, dir + "\\07_issue_window.png", 620, 640, null);
            Snap(asm, "CROMS.Forms.MarriageEntryForm", new object[] { (int?)mid }, dir + "\\08_form97_parties.png", 1260, 800, null);
            Snap(asm, "CROMS.Forms.MarriageEntryForm", new object[] { (int?)mid }, dir + "\\09_form97_license.png", 1260, 800, f => Invoke(f, "ShowTab", 2));
            Snap(asm, "CROMS.Forms.MarriageEntryForm", new object[] { (int?)mid }, dir + "\\10_form97_cert.png", 1260, 800, f => Invoke(f, "ShowTab", 4));
            Snap(asm, "CROMS.Forms.MarriageCaseForm", new object[] { caseId }, dir + "\\11_case.png", 900, 760, null);
            Snap(asm, "CROMS.Forms.MarriageRecordForm", new object[] { regId }, dir + "\\12_record.png", 1120, 780, null);
            Snap(asm, "CROMS.Forms.PsaTransmittalForm", new object[] { null }, dir + "\\13_psa.png", 1180, 800, null);
            Console.WriteLine("rendered to " + dir);
        }

        private static void Step(System.Windows.Forms.Form f, int i) { Invoke(f, "ShowStep", i); }

        private static void Invoke(object o, string method, params object[] args)
        {
            o.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
             .Invoke(o, args);
        }

        private static void Snap(System.Reflection.Assembly asm, string type, object[] args, string file, int w, int h, Action<System.Windows.Forms.Form> prep)
        {
            try
            {
                Type t = asm.GetType(type, true);
                var f = (System.Windows.Forms.Form)Activator.CreateInstance(t, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                                                                             System.Reflection.BindingFlags.Public, null, args, null);
                f.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
                f.Location = new System.Drawing.Point(-4000, -4000);
                f.ShowInTaskbar = false;
                f.Show();
                f.Size = new System.Drawing.Size(w + (f.Width - f.ClientSize.Width), h + (f.Height - f.ClientSize.Height));
                System.Windows.Forms.Application.DoEvents();
                if (prep != null) prep(f);
                for (int i = 0; i < 5; i++) System.Windows.Forms.Application.DoEvents();
                using (var bmp = new System.Drawing.Bitmap(f.Width, f.Height))
                {
                    f.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, f.Width, f.Height));
                    bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                }
                f.FormClosing += (s, e) => { };
                f.Dispose();
                Console.WriteLine("  ok  " + System.IO.Path.GetFileName(file));
            }
            catch (Exception ex) { Console.WriteLine("  FAIL " + System.IO.Path.GetFileName(file) + ": " + (ex.InnerException ?? ex).Message); }
        }

        // ------------------------------------------------------------ cleanup
        private static int Leftovers()
        {
            return Convert.ToInt32(Db.Pull(
                "SELECT (SELECT COUNT(*) FROM marriage_licenses WHERE husband_last_name LIKE 'ZZT%') + " +
                "(SELECT COUNT(*) FROM marriages WHERE husband_last_name LIKE 'ZZT%' OR wife_last_name LIKE 'ZZT%')").Rows[0][0]);
        }

        private static void Cleanup()
        {
            DataTable ms = Db.Pull("SELECT id FROM marriages WHERE husband_last_name LIKE 'ZZT%' OR wife_last_name LIKE 'ZZT%'");
            DataTable ls = Db.Pull("SELECT id FROM marriage_licenses WHERE husband_last_name LIKE 'ZZT%'");
            string mids = string.Join(",", ms.AsEnumerable().Select(x => x[0].ToString()).DefaultIfEmpty("0"));
            string lids = string.Join(",", ls.AsEnumerable().Select(x => x[0].ToString()).DefaultIfEmpty("0"));
            DataTable bs = Db.Pull("SELECT DISTINCT batch_id FROM psa_transmittal_items WHERE record_table='marriages' AND record_id IN (" + mids + ")");
            var bids = bs.AsEnumerable().Select(x => Convert.ToInt32(x[0])).Concat(Batches).Distinct().ToList();
            string bl = string.Join(",", bids.Select(x => x.ToString()).DefaultIfEmpty("0"));

            Db.Push("DELETE FROM psa_transmittal_items WHERE batch_id IN (" + bl + ") OR (record_table='marriages' AND record_id IN (" + mids + "))");
            Db.Push("DELETE FROM psa_transmittal_batches WHERE id IN (" + bl + ") AND NOT EXISTS (SELECT 1 FROM psa_transmittal_items i WHERE i.batch_id = psa_transmittal_batches.id)");
            Db.Push("DELETE FROM marriage_copies WHERE marriage_id IN (" + mids + ")");
            Db.Push("DELETE FROM marriage_requirements WHERE (owner_type='Marriage' AND owner_id IN (" + mids + ")) OR (owner_type='License' AND owner_id IN (" + lids + "))");
            Db.Push("DELETE FROM marriage_history WHERE (entity='Marriage' AND entity_id IN (" + mids + ")) OR (entity='License' AND entity_id IN (" + lids + ")) OR (entity='Batch' AND entity_id IN (" + bl + "))");
            Db.Push("DELETE FROM audit_log WHERE (table_name='marriages' AND record_id IN (" + mids + ")) OR (table_name='marriage_licenses' AND record_id IN (" + lids + ")) OR (table_name='psa_transmittal_batches' AND record_id IN (" + bl + "))");
            Db.Push("DELETE FROM marriages WHERE id IN (" + mids + ")");
            Db.Push("DELETE FROM marriage_licenses WHERE id IN (" + lids + ")");
        }
    }
}
