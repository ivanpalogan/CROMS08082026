using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CROMS.Data;

namespace CROMS.SampleData
{
    /// <summary>
    /// Marriage licence applications (Form 90) and marriages (Form 97), driven through MarriageService so the
    /// requirement rows, deferral rules, history and numbering come out exactly as in real use.
    ///
    /// Registered marriages: Register() needs a confirmed FINAL registered Form 97 (a scanned image), and this
    /// seeder attaches no scans. So the modern, licence-linked marriages stop at the stage the app leaves them
    /// in (Capture / Verify / Register / Final Scan / Returned), and the REGISTERED marriages are the digitized
    /// back-log entries the Old Marriage Records screen writes (record_source = OCR-Backlog, encoding Manual).
    /// </summary>
    internal static class SeedMarriages
    {
        const string Staff1 = "Marites S. Dumlao";

        sealed class Couple
        {
            public Person H, W;
            public string HCit = "Filipino", WCit = "Filipino", HCivil = "Single", WCivil = "Single";
            public bool Consent, Advice, Widowed, Foreign;
        }

        static readonly string[] Solemnizers = { "Hon.|Municipal Mayor", "Rev. Fr.|Parish Priest", "Pastor|Pastor", "Hon. Judge|Presiding Judge", "Imam|Imam" };
        static readonly string[] Churches = { "Penablanca Parish Church", "Living Waters Christian Church", "Iglesia ni Cristo - Lokal ng Penablanca" };

        static int _orCounter = 4700100;
        public static string NextOr()
        {
            while (true)
            {
                string or = (++_orCounter).ToString();
                if (PaymentService.FindByOr(or) == null) return or;
            }
        }

        // ------------------------------------------------------------------------------------ couples
        static Couple MakeCouple(DateTime on, int hAge, int wAge)
        {
            var c = new Couple();
            Muni home = G.PickMuni();
            string hl = G.Surname(), wl = G.Surname();
            while (wl == hl) wl = G.Surname();
            c.H = G.NewPerson(hl, G.Surname(), true, on.AddYears(-hAge).AddDays(-G.R.Next(5, 300)), home);
            c.W = G.NewPerson(wl, G.Surname(), false, on.AddYears(-wAge).AddDays(-G.R.Next(5, 300)), G.Chance(0.7) ? home : G.PickMuni());
            return c;
        }

        static void FillParty(Party pt, Person p, string cit, string civil, bool withConsent, DateTime filed, bool widowed)
        {
            pt.First = p.First; pt.Middle = p.Middle; pt.Last = p.Last; pt.Dob = p.Dob; pt.Sex = p.Sex;
            pt.BirthCountry = "Philippines"; pt.PlaceOfBirth = GeoLookup.JoinPlace(p.M.Name, "Cagayan");
            pt.Citizenship = cit; pt.CivilStatus = civil; pt.Religion = p.Religion;
            pt.ResProvince = "Cagayan"; pt.ResMunicipality = p.M.Name; pt.ResBarangay = p.Barangay; pt.ResHouse = p.House;
            Muni pm = p.M;
            pt.FatherFirst = G.Given(true); pt.FatherMiddle = G.Surname(); pt.FatherLast = p.Last; pt.FatherCitizenship = "Filipino";
            pt.FatherAddr = new Addr { Province = "Cagayan", Municipality = pm.Name, Barangay = p.Barangay, House = p.House };
            pt.MotherFirst = G.Given(false); pt.MotherMiddle = G.Surname(); pt.MotherLast = G.Surname(); pt.MotherCitizenship = "Filipino";
            pt.MotherAddr = new Addr { Province = "Cagayan", Municipality = pm.Name, Barangay = p.Barangay, House = p.House };
            if (withConsent)
            {
                bool dad = G.Chance(0.5);
                pt.ConsentFirst = dad ? pt.FatherFirst : pt.MotherFirst; pt.ConsentMiddle = dad ? pt.FatherMiddle : pt.MotherMiddle;
                pt.ConsentLast = dad ? pt.FatherLast : pt.MotherLast; pt.ConsentRelationship = dad ? "Father" : "Mother";
                pt.ConsentCitizenship = "Filipino";
                pt.ConsentAddr = new Addr { Province = "Cagayan", Municipality = pm.Name, Barangay = p.Barangay, House = p.House };
            }
            if (widowed)
            {
                pt.PrevDissolution = "Death of spouse"; pt.PrevDissolvedMunicipality = pm.Name; pt.PrevDissolvedProvince = "Cagayan";
                pt.PrevDissolvedDate = filed.AddYears(-G.Between(3, 9)).AddDays(-G.R.Next(0, 200));
            }
        }

        static LicenseFacts BuildLicence(Couple c, DateTime filed, string note)
        {
            var l = new LicenseFacts { FiledDate = filed, Remarks = G.Marker + (note == null ? "" : " - " + note) };
            FillParty(l.Husband, c.H, c.HCit, c.HCivil, false, filed, c.Widowed);
            FillParty(l.Wife, c.W, c.WCit, c.WCivil, false, filed, false);
            int ha = MarriageRules.AgeOn(c.H.Dob, filed), wa = MarriageRules.AgeOn(c.W.Dob, filed);
            if (ha >= 18 && ha <= 25) FillParty(l.Husband, c.H, c.HCit, c.HCivil, true, filed, c.Widowed);
            if (wa >= 18 && wa <= 25) FillParty(l.Wife, c.W, c.WCit, c.WCivil, true, filed, false);
            return l;
        }

        static void VerifyAll(int licenseId, DateTime when, string advice = "Favorable")
        {
            foreach (ReqRow r in MarriageService.Requirements("License", licenseId))
            {
                r.Status = "Verified"; r.ReferenceNo = "REF-" + r.Code; r.DocDate = when;
                if (r.Code == "PARENTAL_ADVICE") r.Outcome = advice;
                if (r.Code == "PARENTAL_CONSENT" || r.Code == "PARENTAL_ADVICE") r.GivenBy = "Father";
                MarriageService.SaveRequirement(r);
            }
        }

        static void VerifySome(int licenseId, DateTime when, int count)
        {
            foreach (ReqRow r in MarriageService.Requirements("License", licenseId).Take(count))
            {
                r.Status = "Submitted"; r.ReferenceNo = "REF-" + r.Code; r.DocDate = when;
                MarriageService.SaveRequirement(r);
            }
        }

        /// <summary>One licence through the stages the scenario names. Returns the facts re-read from the database.</summary>
        static LicenseFacts Licence(string scenario, Couple c, DateTime filed, DateTime? issue)
        {
            LicenseFacts l = BuildLicence(c, filed, scenario);
            int id = MarriageService.SaveLicense(l);
            DateTime start = filed.AddDays(1);
            if (scenario == "Draft")
            {
                VerifySome(id, filed, 2);
                return MarriageService.LoadLicense(id);
            }
            if (scenario == "Posting" || scenario == "Hold" || scenario == "Cancelled")
            {
                MarriageService.StartPosting(id, G.Min(start, G.Today));
                VerifySome(id, filed, 4);
                if (scenario == "Hold") MarriageService.Hold(id, "Waiting for the applicants' CENOMAR from PSA.");
                if (scenario == "Cancelled") MarriageService.Cancel(id, "Applicants withdrew the application.");
                return MarriageService.LoadLicense(id);
            }
            // issued family: Valid / Expiring / Expired / UsedBy
            DateTime i = issue.Value;
            DateTime posting = i.AddDays(-G.R.Next(10, 14));
            MarriageService.StartPosting(id, posting);
            VerifyAll(id, filed);
            if (c.Widowed || c.HCivil == "Widowed" || c.WCivil == "Widowed")
                MarriageService.SetImpedimentNote(id, "Previous marriage ended by death of spouse; death certificate on file. No impediment.");
            decimal amount = c.Foreign ? 2200m : 1200m;
            MarriageService.RecordPayment(id, NextOr(), amount, posting);
            string no;
            List<RuleIssue> iss = MarriageService.IssueLicense(id, i, out no);
            if (iss.Count > 0) throw new InvalidOperationException("Licence '" + scenario + "' could not be issued: " + string.Join("; ", iss.Select(x => x.Code + " " + x.Message)));
            return MarriageService.LoadLicense(id);
        }

        // ------------------------------------------------------------------------------------ run
        public static void Run()
        {
            if (G.Already("marriage_licenses", "remarks") || G.Already("marriages", "remarks")) { Console.WriteLine("marriages: already seeded - skipped"); return; }
            Console.WriteLine("Marriage licences and marriages ...");
            foreach (string ch in Churches) LookupStore.Ensure("churches", ch);
            LookupStore.Ensure("nationalities", "Filipino");

            var issuedForMarriage = new List<LicenseFacts>();
            int made = 0;

            // (scenario, how many, issue-date offset range in days ago [from,to], marry?)
            var plan = new List<Tuple<string, int, int, int, bool>>
            {
                Tuple.Create("Draft", 2, 0, 0, false),
                Tuple.Create("Posting", 3, 0, 0, false),
                Tuple.Create("Hold", 1, 0, 0, false),
                Tuple.Create("Cancelled", 1, 0, 0, false),
                Tuple.Create("Valid", 2, 4, 40, false),      // issued, not yet used
                Tuple.Create("Expiring", 3, 98, 114, false), // 6-22 days left
                Tuple.Create("Expired", 2, 128, 165, false),
                Tuple.Create("ForMarriage", 8, 6, 70, true), // issued and linked to a Form 97 below
            };
            // special couples are spread over the issued ones
            int widow = 0, foreign = 0;
            foreach (var step in plan)
            {
                for (int n = 0; n < step.Item2; n++)
                {
                    string sc = step.Item1;
                    DateTime? issue = null;
                    DateTime filed;
                    if (sc == "Draft") filed = G.Today.AddDays(-G.R.Next(1, 9));
                    else if (sc == "Posting" || sc == "Hold" || sc == "Cancelled") filed = G.Today.AddDays(-G.R.Next(3, 9));
                    else
                    {
                        issue = G.Workday(G.Today.AddDays(-G.R.Next(step.Item3, step.Item4 + 1)), G.Today.AddDays(-400), G.Today);
                        filed = issue.Value.AddDays(-G.R.Next(13, 24));
                    }
                    // age profile
                    int ha = G.Between(23, 41), wa = G.Between(21, 38);
                    bool consent = false, advice = false;
                    if (sc != "Draft" && sc != "Cancelled")
                    {
                        if (n == 0 && (sc == "ForMarriage" || sc == "Posting")) { wa = 19; consent = true; }
                        else if (n == 1 && (sc == "ForMarriage" || sc == "Valid")) { ha = 23; wa = 22; advice = true; }
                        else if (n == 2 && sc == "ForMarriage") { ha = 25; advice = true; }
                    }
                    Couple c = MakeCouple(filed, ha, wa);
                    if (sc == "ForMarriage" && widow == 0 && n == 3) { c.Widowed = true; c.HCivil = "Widowed"; c.H.Dob = filed.AddYears(-44).AddDays(-77); widow++; }
                    if (sc == "ForMarriage" && foreign == 0 && n == 4) { c.Foreign = true; c.HCit = "Japanese"; foreign++; LookupStore.Ensure("nationalities", "Japanese"); }
                    LicenseFacts lic = Licence(sc, c, filed, issue);
                    made++;
                    if (step.Item5) issuedForMarriage.Add(lic);
                    // backdate the application's own timestamps to the day it was filed
                    DateTime stamp = G.Office(filed, 8, 16);
                    Db.Push("UPDATE marriage_licenses SET created_at=@c, updated_at=@c WHERE id=@id", G.P("@c", stamp), G.P("@id", lic.Id));
                }
            }
            MarriageService.SweepExpired();
            Console.WriteLine("  {0} licence applications", made);

            // ---- Form 97 for the issued ones, each stopped at a different step
            string[] states = { "Draft", "ForReview", "Verify", "Verify", "Register", "Register", "FinalScan", "Returned" };
            for (int i = 0; i < issuedForMarriage.Count; i++)
            {
                LicenseFacts lic = issuedForMarriage[i];
                DateTime mdate = G.Workday(lic.IssueDate.Value.AddDays(G.R.Next(2, 20)), lic.IssueDate.Value, G.Min(lic.ExpiryDate.Value, G.Today));
                if (mdate > G.Today) mdate = G.Today;
                MakeMarriage(lic, mdate, states[i % states.Length]);
            }
            Console.WriteLine("  {0} licence-linked marriages (Form 97) left at their workflow steps", issuedForMarriage.Count);

            SeedLegacy();
        }

        static int LookupId(string table, string name)
        {
            return G.Scalar("SELECT id FROM `" + table + "` WHERE name = @n ORDER BY id LIMIT 1", G.P("@n", name));
        }

        static string Join3(string a, string b, string c) { return string.Join(" ", new[] { a, b, c }.Where(s => !string.IsNullOrWhiteSpace(s))); }

        static void MakeMarriage(LicenseFacts lic, DateTime mdate, string state)
        {
            bool church = G.Chance(0.5);
            string[] sol = G.Pick(church ? new[] { "Rev. Fr.|Parish Priest", "Pastor|Pastor" } : new[] { "Hon.|Municipal Mayor", "Hon. Judge|Presiding Judge" }).Split('|');
            string solName = sol[0] + " " + G.Given(true) + " " + G.Surname();
            Muni home = G.Home();
            var v = new Dictionary<string, object>
            {
                { "form_code", "MF-97-1993" }, { "form_name", "Certificate of Marriage" },
                { "solemnizer", solName }, { "solemnizer_position", sol[1] },
                { "husband_first_name", lic.Husband.First }, { "husband_middle_name", lic.Husband.Middle }, { "husband_last_name", lic.Husband.Last },
                { "husband_sex", "Male" }, { "husband_date_of_birth", lic.Husband.Dob }, { "husband_age", MarriageRules.AgeOn(lic.Husband.Dob.Value, mdate) },
                { "husband_place_of_birth", lic.Husband.PlaceOfBirth }, { "husband_birth_country", "Philippines" },
                { "husband_civil_status", lic.Husband.CivilStatus }, { "husband_citizenship_id", LookupId("nationalities", lic.Husband.Citizenship) },
                { "husband_religion_id", LookupId("religions", lic.Husband.Religion ?? "Roman Catholic") },
                { "husband_res_province", "Cagayan" }, { "husband_res_municipality", lic.Husband.ResMunicipality }, { "husband_res_barangay", lic.Husband.ResBarangay }, { "husband_res_house", lic.Husband.ResHouse },
                { "husband_father_name", Join3(lic.Husband.FatherFirst, lic.Husband.FatherMiddle, lic.Husband.FatherLast) },
                { "husband_mother_name", Join3(lic.Husband.MotherFirst, lic.Husband.MotherMiddle, lic.Husband.MotherLast) },
                { "husband_father_citizenship", "Filipino" }, { "husband_mother_citizenship", "Filipino" },
                { "wife_first_name", lic.Wife.First }, { "wife_middle_name", lic.Wife.Middle }, { "wife_last_name", lic.Wife.Last },
                { "wife_sex", "Female" }, { "wife_date_of_birth", lic.Wife.Dob }, { "wife_age", MarriageRules.AgeOn(lic.Wife.Dob.Value, mdate) },
                { "wife_place_of_birth", lic.Wife.PlaceOfBirth }, { "wife_birth_country", "Philippines" },
                { "wife_civil_status", lic.Wife.CivilStatus }, { "wife_citizenship_id", LookupId("nationalities", lic.Wife.Citizenship) },
                { "wife_religion_id", LookupId("religions", lic.Wife.Religion ?? "Roman Catholic") },
                { "wife_res_province", "Cagayan" }, { "wife_res_municipality", lic.Wife.ResMunicipality }, { "wife_res_barangay", lic.Wife.ResBarangay }, { "wife_res_house", lic.Wife.ResHouse },
                { "wife_father_name", Join3(lic.Wife.FatherFirst, lic.Wife.FatherMiddle, lic.Wife.FatherLast) },
                { "wife_mother_name", Join3(lic.Wife.MotherFirst, lic.Wife.MotherMiddle, lic.Wife.MotherLast) },
                { "wife_father_citizenship", "Filipino" }, { "wife_mother_citizenship", "Filipino" },
                { "place_municipality_id", home.Id }, { "place_province_id", G.ProvinceId },
                { "date_of_marriage", mdate }, { "time_of_marriage", church ? "10:00" : "14:00" },
                { "witness1_name", G.Given(true) + " " + G.Surname() }, { "witness2_name", G.Given(false) + " " + G.Surname() },
                { "license_id", lic.Id }, { "license_no", lic.LicenseNo }, { "license_date", lic.IssueDate }, { "license_place", "Penablanca, Cagayan" },
                { "license_basis", "Licensed" }, { "marriage_settlement", "None" },
                { "received_by", Staff1 }, { "received_by_title", "Registration Clerk" }, { "received_by_date", G.Min(mdate.AddDays(G.R.Next(1, 4)), G.Today) },
                { "remarks", G.Marker },
            };
            if (church) { int cid = LookupId("churches", G.Pick(Churches)); if (cid > 0) v["church_id"] = cid; }
            if (state == "ForReview") v["status"] = "For Review";
            if (state == "Verify") v["current_step"] = "Verify";
            if (state == "Register" || state == "FinalScan") v["current_step"] = "Register";
            if (state == "Draft" || state == "ForReview") v["current_step"] = "Form 97 Capture";
            foreach (string k in v.Keys.ToList()) if (v[k] is int && (int)v[k] == 0) v.Remove(k);   // unresolved lookup id
            int id = MarriageService.SaveMarriage(null, v);

            if (state == "Register" || state == "FinalScan")
                foreach (ReqRow r in MarriageService.Requirements("Marriage", id))
                {
                    r.Status = "Verified"; r.ReferenceNo = "DOC-" + r.Code; MarriageService.SaveRequirement(r);
                }
            if (state == "FinalScan")
                MarriageService.SaveRegistrationInfo(id, G.Min(mdate.AddDays(2), G.Today), "Lorna B. Macaraeg");
            if (state == "Returned")
                MarriageService.ReturnForCorrection(id, "Husband's date of birth does not match his birth certificate; please correct and resubmit.");
            DateTime stamp = G.Office(G.Min(mdate.AddDays(1), G.Today), 8, 16);
            Db.Push("UPDATE marriages SET created_at=@c, updated_at=@c WHERE id=@id", G.P("@c", stamp), G.P("@id", id));
        }

        // ------------------------------------------------------------------------------------ digitized, registered
        static void SeedLegacy()
        {
            var rows = new List<Dictionary<string, object>>();
            DateTime from = new DateTime(2024, 1, 15), to = G.Today.AddDays(-20);
            int span = (int)(to - from).TotalDays;
            var dates = Enumerable.Range(0, 12).Select(i => G.Workday(from.AddDays(G.R.Next(0, span + 1)), from, to)).OrderBy(d => d).ToList();
            for (int i = 0; i < dates.Count; i++)
            {
                DateTime md = dates[i];
                int ha = G.Between(22, 45), wa = G.Between(20, 40);
                Couple c = MakeCouple(md, ha, wa);
                bool church = G.Chance(0.55);
                string[] sol = G.Pick(church ? new[] { "Rev. Fr.|Parish Priest", "Pastor|Pastor" } : new[] { "Hon.|Municipal Mayor", "Hon. Judge|Presiding Judge" }).Split('|');
                DateTime reg = G.Workday(G.Min(md.AddDays(G.R.Next(1, 14)), G.Today), md, G.Today);
                bool delayed = i == 5;
                if (delayed) reg = G.Workday(G.Min(md.AddDays(G.R.Next(40, 90)), G.Today.AddDays(-5)), md, G.Today);
                var v = new Dictionary<string, object>
                {
                    { "form_code", "MF-97-1993" }, { "form_name", "Certificate of Marriage" }, { "status", "Registered" }, { "current_step", "Registered" }, { "record_source", "OCR-Backlog" },
                    { "husband_first_name", c.H.First }, { "husband_middle_name", c.H.Middle }, { "husband_last_name", c.H.Last }, { "husband_sex", "Male" },
                    { "husband_age", ha }, { "husband_date_of_birth", c.H.Dob }, { "husband_place_of_birth", GeoLookup.JoinPlace(c.H.M.Name, "Cagayan") }, { "husband_civil_status", i == 7 ? "Widowed" : "Single" },
                    { "wife_first_name", c.W.First }, { "wife_middle_name", c.W.Middle }, { "wife_last_name", c.W.Last }, { "wife_sex", "Female" },
                    { "wife_age", wa }, { "wife_date_of_birth", c.W.Dob }, { "wife_place_of_birth", GeoLookup.JoinPlace(c.W.M.Name, "Cagayan") }, { "wife_civil_status", "Single" },
                    { "date_of_marriage", md }, { "time_of_marriage", church ? "10:00" : "14:00" },
                    { "place_of_marriage", church ? G.Pick(Churches) + ", " + G.Home().Name + ", Cagayan" : "Office of the Municipal Mayor, " + G.Home().Name + ", Cagayan" },
                    { "solemnizer", sol[0] + " " + G.Given(true) + " " + G.Surname() }, { "solemnizer_position", sol[1] },
                    { "license_basis", "Licensed" }, { "license_no", string.Format("{0}-L-{1:D4}", md.AddDays(-15).Year, G.Between(10, 380)) }, { "license_date", md.AddDays(-G.R.Next(8, 60)) },
                    { "license_place", "Penablanca, Cagayan" },
                    { "date_registered", reg }, { "registration_type", delayed ? "Delayed" : "Timely" },
                    { "delay_reason", delayed ? "Certificate of Marriage reached the registrar after the reglementary period." : null },
                    { "digitized_by", Staff1 }, { "date_digitized", G.Office(G.Min(reg.AddDays(G.R.Next(0, 3)), G.Today)) }, { "encoding_method", "Manual" },
                    { "source_reference", "Registry of Marriages, Book " + reg.Year },
                    { "remarks", G.Marker + " - digitized from the registry book" },
                    { "_reg", reg }
                };
                rows.Add(v);
            }
            var ids = new List<Tuple<int, DateTime>>();
            foreach (var v in rows.OrderBy(r => (DateTime)r["_reg"]))
            {
                DateTime reg = (DateTime)v["_reg"]; v.Remove("_reg");
                string rn = G.NextReg("marriages", 'M', reg.Year);
                int seq = int.Parse(rn.Substring(rn.LastIndexOf('-') + 1));
                v["registry_no"] = rn; v["book_volume"] = reg.Year.ToString(); v["book_page"] = ((seq + 1) / 2).ToString();
                DateTime created = G.Office(reg);
                v["created_at"] = created; v["updated_at"] = created;
                int id = (int)G.Insert("marriages", v);
                ids.Add(Tuple.Create(id, reg));
            }
            Console.WriteLine("  {0} registered marriages (digitized registry-book entries)", ids.Count);

            // PSA transmittal: the oldest three go out in a batch that was sent and acknowledged; the next two form a draft batch.
            try
            {
                var old = ids.OrderBy(x => x.Item2).Take(3).ToList();
                string no1;
                int b1 = MarriageService.CreateBatch(old.Select(x => x.Item1).ToList(), old.Max(x => x.Item2).Year, old.Max(x => x.Item2).Month, out no1);
                DateTime sent = G.Min(old.Max(x => x.Item2).AddDays(12), G.Today.AddDays(-15));
                MarriageService.MarkBatchSent(b1, sent, "Hand-carried", "PSA Provincial Statistical Office - Cagayan", "TR-" + sent.ToString("yyyyMMdd") + "-01", G.Marker);
                MarriageService.AcknowledgeBatch(b1, sent.AddDays(6), "ACK-" + sent.AddDays(6).ToString("yyyyMMdd"));
                var next = ids.OrderBy(x => x.Item2).Skip(3).Take(2).ToList();
                string no2;
                MarriageService.CreateBatch(next.Select(x => x.Item1).ToList(), G.Today.Year, G.Today.Month, out no2);
                Console.WriteLine("  PSA transmittal batches {0} (sent, acknowledged) and {1} (draft)", no1, no2);
            }
            catch (Exception ex) { G.Note("PSA transmittal batches were not created: " + ex.Message); }
        }
    }
}
