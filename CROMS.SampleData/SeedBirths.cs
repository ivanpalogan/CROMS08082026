using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.SampleData
{
    /// <summary>
    /// Municipal Form 102 sample births. The column set is the one BirthRegistrationForm writes
    /// (its Columns constant), plus created_at / updated_at / record_source so the registered-vs-occurred
    /// analytics see a believable spread of dates. Registry numbers follow the form's own YYYY-B-#### scheme.
    /// </summary>
    internal static class SeedBirths
    {
        const string FormCode = "MF-102-2007", FormName = "Certificate of Live Birth";
        static readonly string[] Ordinals = { "1st", "2nd", "3rd", "4th", "5th", "6th" };

        sealed class B
        {
            public Dictionary<string, object> V = new Dictionary<string, object>();
            public DateTime Dob, Reg, Created;
            public string Status = "Registered";
            public bool Delayed, Twin;
            public int GroupSort;         // keeps multiples together and consecutive
        }

        public static void Run()
        {
            if (G.Already("births", "remarks")) { Console.WriteLine("births: already seeded - skipped"); return; }
            Console.WriteLine("Births ...");

            // Lookups the form's pick-lists offer (Ensure = insert only if missing).
            foreach (string h in new[] { "Cagayan Valley Medical Center", "Penablanca Rural Health Unit" }) LookupStore.Ensure("hospitals", h);
            foreach (string o in new[] { "Housewife", "Farmer", "Driver", "Fisherman", "Laborer", "Teacher", "Government Employee", "Vendor", "Self-employed", "Business Owner", "Nurse", "Engineer", "Overseas Filipino Worker (OFW)" })
                LookupStore.Ensure("occupations", o);

            var recs = new List<B>();
            DateTime from = new DateTime(2024, 1, 2), to = G.Today.AddDays(-1);
            int span = (int)(to - from).TotalDays;

            // Families: 2 twin sets, 1 triplet set, the rest singles = 60 children.
            int[] familySizes = Enumerable.Repeat(1, 53).Concat(new[] { 2, 2, 3 }).ToArray();
            familySizes = familySizes.OrderBy(x => G.R.Next()).ToArray();
            int sortKey = 0;
            foreach (int kids in familySizes)
            {
                // skew toward recent years: 2024 = 25%, 2025 = 35%, 2026 = 40%
                DateTime dob;
                double roll = G.R.NextDouble();
                if (roll < 0.25) dob = from.AddDays(G.R.Next(0, 364));
                else if (roll < 0.60) dob = new DateTime(2025, 1, 1).AddDays(G.R.Next(0, 364));
                else dob = new DateTime(2026, 1, 1).AddDays(G.R.Next(0, (int)(to - new DateTime(2026, 1, 1)).TotalDays + 1));
                if (dob > to) dob = to;
                MakeFamily(recs, kids, dob, ++sortKey);
            }

            // Registration dates, status, delay.
            DateTime sixtyAgo = G.Today.AddDays(-60);
            var byDobDesc = recs.OrderByDescending(r => r.Dob).ToList();
            // delayed: 6 children (not multiples) born at least 60 days ago, registered 45-400 days late
            var delayedPool = recs.Where(r => !r.Twin && r.Dob <= sixtyAgo).OrderBy(r => G.R.Next()).Take(6).ToList();
            foreach (B r in recs)
            {
                DateTime reg;
                if (delayedPool.Contains(r))
                {
                    r.Delayed = true;
                    reg = r.Dob.AddDays(G.R.Next(45, 401));
                    if (reg > G.Today.AddDays(-3)) reg = G.Today.AddDays(-3 - G.R.Next(0, 20));
                }
                else reg = r.Dob.AddDays(G.R.Next(1, 26));
                reg = G.Workday(G.Min(reg, G.Today), r.Dob, G.Today);
                if (reg < r.Dob) reg = r.Dob;
                r.Reg = reg;
            }
            // multiples share one registration day (the first one's)
            foreach (var grp in recs.Where(r => r.Twin).GroupBy(r => r.GroupSort))
            {
                DateTime d = grp.Min(r => r.Reg);
                foreach (B r in grp) r.Reg = d;
            }
            // newest births are the ones still being worked on: 6 Drafts, 8 Pending Approval
            var newest = recs.Where(r => !r.Delayed && (r.Dob >= G.Today.AddDays(-160))).OrderByDescending(r => r.Dob).Select(r => r).ToList();
            foreach (B r in newest.Take(6)) r.Status = "Draft";
            foreach (B r in newest.Skip(6).Take(8)) r.Status = "Pending Approval";
            // a Draft's twins/triplets follow it (a multiple birth is entered together)
            foreach (var grp in recs.Where(r => r.Twin).GroupBy(r => r.GroupSort))
            {
                string st = grp.Select(r => r.Status).OrderByDescending(s => s == "Draft" ? 2 : s == "Pending Approval" ? 1 : 0).First();
                foreach (B r in grp) r.Status = st;
            }

            // Insert in registration order so registry numbers rise with the date.
            int inserted = 0;
            var delayedIds = new List<int>();
            foreach (B r in recs.OrderBy(x => x.Status == "Draft" ? 1 : 0).ThenBy(x => x.Reg).ThenBy(x => x.GroupSort))
            {
                var v = r.V;
                bool draft = r.Status == "Draft";
                v["status"] = r.Status;
                v["is_delayed"] = r.Delayed ? 1 : 0;
                v["date_registered"] = draft ? (object)null : r.Reg;
                if (!draft)
                {
                    string reg = G.NextReg("births", 'B', r.Reg.Year);
                    int n = int.Parse(reg.Substring(reg.LastIndexOf('-') + 1));
                    v["registry_no"] = reg;
                    v["book_volume"] = r.Reg.Year.ToString();
                    v["book_page"] = ((n + 1) / 2).ToString();
                }
                r.Created = draft ? G.Office(G.Workday(G.Min(r.Dob.AddDays(G.R.Next(2, 6)), G.Today), r.Dob, G.Today))
                                  : G.Office(r.Reg, 8, 16);
                v["created_at"] = r.Created; v["updated_at"] = r.Created;

                // signature block: staff who handled it
                if (!draft)
                {
                    v["prepared_by"] = "Marites S. Dumlao"; v["prepared_by_title"] = "Registration Clerk"; v["prepared_by_date"] = r.Reg;
                    v["received_by"] = "Ramon P. Aggabao"; v["received_by_title"] = "Administrative Aide"; v["received_by_date"] = r.Reg;
                    v["informant_date"] = r.Reg;
                    if (r.Status == "Registered")
                    {
                        v["registered_by"] = "Lorna B. Macaraeg"; v["registered_by_title"] = "Municipal Civil Registrar"; v["registered_by_date"] = r.Reg;
                    }
                }
                v["remarks"] = G.Marker + (r.Delayed ? " - delayed registration, affidavit on file" : "");
                int id = (int)G.Insert("births", v);
                inserted++;
                if (r.Delayed) delayedIds.Add(id);
            }

            // Delayed registrations: facts + the PSA MC 2024-17 checklist, posting on half of them.
            int k = 0;
            foreach (int id in delayedIds)
            {
                DelayedBirthCase c = DelayedBirthService.Load(id);
                c.MotherUnavailable = false; c.ParentDeceased = false; c.RegistrantDeceased = false;
                DelayedBirthService.SaveCase(c, G.UserId);
                if (k < 4)
                {
                    DateTime ps = (c.DateRegistered ?? G.Today).AddDays(-11);
                    if (c.DateOfBirth.HasValue && ps < c.DateOfBirth.Value) ps = c.DateOfBirth.Value.AddDays(1);
                    DelayedBirthService.StartPosting(id, ps, G.UserId);
                }
                if (k < 2) DelayedBirthService.SetEvaluation(id, "Documents complete and posting period observed; recommended for registration.", G.UserId);
                // the first requirements are verified as the registrar went through the card
                var rows = DelayedBirthService.Requirements(id);
                foreach (ReqRow row in rows.Take(k < 2 ? rows.Count : 3))
                {
                    row.Status = "Verified"; row.ReferenceNo = "DOC-" + row.Code;
                    MarriageService.SaveRequirement(row);
                }
                k++;
            }
            Console.WriteLine("  {0} births ({1} Registered, {2} Pending Approval, {3} Draft, {4} delayed)", inserted,
                recs.Count(r => r.Status == "Registered"), recs.Count(r => r.Status == "Pending Approval"), recs.Count(r => r.Status == "Draft"), delayedIds.Count);
        }

        // ------------------------------------------------------------------------------ a family and its child(ren)
        static void MakeFamily(List<B> recs, int kids, DateTime dob, int sortKey)
        {
            bool married = G.Chance(0.82);
            Muni home = G.PickMuni();
            string fatherLast = G.Surname(), motherMaiden = G.Surname();
            while (motherMaiden == fatherLast) motherMaiden = G.Surname();
            int momAge = G.Between(18, 38);
            int dadAge = momAge + G.Between(0, 7);
            Person mom = G.NewPerson(motherMaiden, G.Surname(), false, dob.AddYears(-momAge).AddDays(-G.R.Next(0, 300)), home);
            Person dad = G.NewPerson(fatherLast, G.Surname(), true, dob.AddYears(-dadAge).AddDays(-G.R.Next(0, 300)), home);
            dad.Barangay = mom.Barangay; dad.House = mom.House;   // they live together
            bool fatherKnown = married || G.Chance(0.4);
            string childLast = married || fatherKnown ? fatherLast : motherMaiden;
            string childMiddle = married || fatherKnown ? motherMaiden : null;
            int order0 = G.Weighted(new[] { 1, 2, 3, 4, 5 }, new[] { 45, 30, 15, 7, 3 });

            // facility
            string facility = G.Weighted(new[] { "Cagayan Valley Medical Center", "Penablanca Rural Health Unit", "Home" }, new[] { 34, 41, 25 });
            if (kids > 1 && facility == "Home") facility = "Cagayan Valley Medical Center";   // multiples are delivered in a hospital
            Muni birthMuni = facility == "Cagayan Valley Medical Center"
                ? G.Munis.First(m => LearningLibrary.Normalize(m.Name) == "tuguegarao city")
                : facility == "Home" ? home : G.Home();
            string placeFacility = facility == "Home" ? "Residence of the mother" : facility;

            // the parents' marriage, only if married and plausible
            DateTime? marrDate = null; string marrPlace = null;
            if (married)
            {
                DateTime earliest = mom.Dob.AddYears(18).AddDays(30);
                DateTime latest = dob.AddDays(-60);
                if (latest > earliest)
                {
                    DateTime d = dob.AddDays(-G.R.Next(120, 365 * 7));
                    if (d < earliest) d = earliest.AddDays(G.R.Next(0, (int)Math.Max(1, (latest - earliest).TotalDays)));
                    if (d > latest) d = latest;
                    marrDate = d.Date;
                    marrPlace = string.Join(", ", new[] { G.Chance(0.6) ? "Parish Church" : "Municipal Hall", "Cagayan", home.Name });
                }
                else married = false;
            }

            string[] attendantPick = facility == "Home"
                ? new[] { "Hilot", "Midwife" }
                : facility == "Cagayan Valley Medical Center" ? new[] { "Physician", "Physician", "Nurse" } : new[] { "Midwife", "Nurse", "Physician" };
            string attType = G.Pick(attendantPick);
            string attName = attType == "Physician" ? "Dr. " + G.Given(G.Chance(0.5)) + " " + G.Surname()
                           : attType == "Hilot" ? G.Given(false) + " " + G.Surname()
                           : G.Given(false) + " " + G.Surname() + (attType == "Midwife" ? ", RM" : ", RN");
            string attTitle = attType == "Physician" ? (facility == "Home" ? "Physician" : "Attending Physician")
                            : attType == "Midwife" ? "Rural Health Midwife" : attType == "Nurse" ? "Staff Nurse" : "Traditional Birth Attendant (Hilot)";
            string attAddr = string.Join(", ", new[] { "Cagayan", birthMuni.Name, G.Pick(birthMuni.Barangays) });

            for (int i = 0; i < kids; i++)
            {
                bool male = G.Chance(0.51);
                int order = Math.Min(order0 + i, 6);
                string bt = kids == 1 ? "Single" : kids == 2 ? "Twin" : "Triplet";
                int weight = kids == 1 ? (int)Math.Max(2200, Math.Min(4200, 3100 + G.Between(-450, 650)))
                           : kids == 2 ? 2400 + G.Between(-250, 300) : 1950 + G.Between(-200, 300);
                string tob = string.Format("{0:D2}:{1:D2}", G.Between(0, 23), G.Between(0, 59));

                string infName, infRel, infAddrRes;
                double ir = G.R.NextDouble();
                if (ir < 0.70) { infName = mom.Full; infRel = "Mother"; infAddrRes = null; }
                else if (ir < 0.85 && fatherKnown) { infName = dad.Full; infRel = "Father"; infAddrRes = null; }
                else if (ir < 0.95) { infName = G.Given(false) + " " + motherMaiden; infRel = "Grandmother"; infAddrRes = null; }
                else { infName = G.Given(false) + " " + motherMaiden; infRel = "Aunt"; infAddrRes = null; }
                string infAddr = string.Join(", ", new[] { "Cagayan", mom.M.Name, mom.Barangay });

                var rec = new B { Dob = dob, Twin = kids > 1, GroupSort = sortKey };
                var v = rec.V;
                v["form_code"] = FormCode; v["form_name"] = FormName; v["record_source"] = "Registration";
                v["first_name"] = G.Given(male); v["middle_name"] = childMiddle; v["last_name"] = childLast; v["sex"] = male ? "Male" : "Female";
                v["date_of_birth"] = dob; v["time_of_birth"] = tob;
                v["place_of_birth"] = string.Join(", ", new[] { placeFacility, "Cagayan", birthMuni.Name });
                v["place_of_birth_house"] = facility == "Home" ? mom.House : null;
                v["place_of_birth_barangay"] = facility == "Home" ? mom.Barangay : null;
                v["birth_country"] = "Philippines";
                v["type_of_birth"] = bt; v["multiple_birth_order"] = kids > 1 ? Ordinals[i] : null;
                v["birth_order"] = Ordinals[order - 1]; v["weight_grams"] = weight;

                v["mother_first_name"] = mom.First; v["mother_middle_name"] = mom.Middle; v["mother_last_name"] = mom.Last;
                v["mother_citizenship"] = "Filipino"; v["mother_religion"] = mom.Religion; v["mother_occupation"] = mom.Occupation;
                v["mother_age"] = momAge; v["mother_children_born_alive"] = order;
                v["mother_children_living"] = order; v["mother_children_dead"] = 0;
                v["mother_residence"] = mom.Residence;
                if (fatherKnown)
                {
                    v["father_first_name"] = dad.First; v["father_middle_name"] = dad.Middle; v["father_last_name"] = dad.Last;
                    v["father_citizenship"] = "Filipino"; v["father_religion"] = dad.Religion; v["father_occupation"] = dad.Occupation;
                    v["father_age"] = dadAge; v["father_residence"] = dad.Residence;
                }
                v["parents_married"] = married ? 1 : 0;
                v["parents_marriage_date"] = married ? (object)marrDate : null;
                v["parents_marriage_place"] = married ? marrPlace : null;

                v["attendant_type"] = attType; v["attendant_name"] = attName; v["attendant_title"] = attTitle;
                v["attendant_address"] = attAddr; v["attendant_date"] = dob;
                v["informant_name"] = infName; v["informant_relationship"] = infRel; v["informant_address"] = infAddr;
                recs.Add(rec);
            }
        }

    }
}
