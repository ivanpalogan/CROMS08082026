using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CROMS.Data;

namespace CROMS.SampleData
{
    /// <summary>
    /// Municipal Form 103 sample deaths (the columns DeathRegistrationForm writes). Ages are computed from the
    /// date of birth and the date of death, never typed, so the two cannot disagree.
    /// </summary>
    internal static class SeedDeaths
    {
        sealed class Cause { public string Immediate, Antecedent, Underlying, Interval1, Interval2, Interval3; public int MinAge; }

        static readonly Cause[] Causes =
        {
            new Cause { Immediate = "Cardiopulmonary arrest", Antecedent = "Acute myocardial infarction", Underlying = "Coronary artery disease", Interval1 = "30 minutes", Interval2 = "2 hours", Interval3 = "6 years", MinAge = 40 },
            new Cause { Immediate = "Respiratory failure", Antecedent = "Pneumonia", Underlying = "Chronic obstructive pulmonary disease", Interval1 = "1 hour", Interval2 = "4 days", Interval3 = "8 years", MinAge = 50 },
            new Cause { Immediate = "Cerebrovascular accident", Antecedent = "Intracerebral hemorrhage", Underlying = "Hypertension", Interval1 = "2 hours", Interval2 = "1 day", Interval3 = "12 years", MinAge = 45 },
            new Cause { Immediate = "Acute renal failure", Antecedent = "Chronic kidney disease", Underlying = "Diabetes mellitus", Interval1 = "1 day", Interval2 = "2 years", Interval3 = "15 years", MinAge = 45 },
            new Cause { Immediate = "Sepsis", Antecedent = "Pneumonia", Underlying = "Pulmonary tuberculosis", Interval1 = "2 days", Interval2 = "3 weeks", Interval3 = "5 months", MinAge = 20 },
            new Cause { Immediate = "Congestive heart failure", Antecedent = "Hypertensive cardiovascular disease", Underlying = "Hypertension", Interval1 = "3 hours", Interval2 = "1 year", Interval3 = "10 years", MinAge = 55 },
            new Cause { Immediate = "Hypovolemic shock", Antecedent = "Gastrointestinal bleeding", Underlying = "Liver cirrhosis", Interval1 = "1 hour", Interval2 = "2 days", Interval3 = "4 years", MinAge = 35 },
            new Cause { Immediate = "Multiple organ failure", Antecedent = "Septic shock", Underlying = "Urinary tract infection", Interval1 = "4 hours", Interval2 = "3 days", Interval3 = "1 week", MinAge = 60 },
        };
        static readonly string[] InformantRel = { "Wife", "Husband", "Son", "Daughter", "Sister", "Brother", "Grandchild", "Niece", "Nephew" };

        public static void Run()
        {
            if (G.Already("deaths", "remarks")) { Console.WriteLine("deaths: already seeded - skipped"); return; }
            Console.WriteLine("Deaths ...");
            foreach (var c in Causes) { LookupStore.Ensure("causes_of_death", c.Immediate); LookupStore.Ensure("causes_of_death", c.Antecedent); LookupStore.Ensure("causes_of_death", c.Underlying); }
            foreach (string o in new[] { "Farmer", "Housewife", "Retired", "Teacher", "Driver", "Fisherman", "Laborer", "Vendor" }) LookupStore.Ensure("occupations", o);

            var recs = new List<Dictionary<string, object>>();
            DateTime from = new DateTime(2024, 2, 1), to = G.Today.AddDays(-2);
            int span = (int)(to - from).TotalDays;
            var dods = Enumerable.Range(0, 30).Select(i => from.AddDays(G.R.Next(0, span + 1))).OrderBy(d => d).ToList();
            // a handful are the newest, still being verified
            for (int i = 0; i < dods.Count; i++)
            {
                var v = new Dictionary<string, object>();
                DateTime dod = G.Workday(dods[i], from, to);
                bool male = G.Chance(0.52);
                // age profile: mostly elderly, a few younger, one infant
                int ageYears = i == 3 ? 0 : G.Weighted(new[] { 22, 38, 52, 64, 72, 80, 88 }, new[] { 4, 6, 10, 18, 26, 24, 12 }) + G.Between(-4, 4);
                if (ageYears < 0) ageYears = 1;
                DateTime dob = ageYears == 0 ? dod.AddDays(-G.R.Next(5, 200)) : dod.AddYears(-ageYears).AddDays(-G.R.Next(0, 360));
                int age = G.Age(dob, dod);
                var elig = Causes.Where(c => c.MinAge <= age).ToList();
                Cause cause = elig.Count == 0 ? Causes[4] : G.Pick(elig);
                if (age < 20) cause = Causes[4];

                Muni home = G.PickMuni();
                string last = G.Surname();
                Person p = G.NewPerson(last, G.Surname(), male, dob, home);
                string civil = age < 20 ? "Single" : G.Weighted(new[] { "Married", "Widowed", "Single" }, new[] { 55, 30, 15 });
                string father = G.Given(true) + " " + last, mother = G.Given(false) + " " + G.Surname();

                string facility = G.Weighted(new[] { "Residence of the deceased", "Cagayan Valley Medical Center", "Penablanca Rural Health Unit" }, new[] { 45, 35, 20 });
                Muni deathMuni = facility == "Cagayan Valley Medical Center" ? G.Munis.First(m => LearningLibrary.Normalize(m.Name) == "tuguegarao city")
                               : facility == "Penablanca Rural Health Unit" ? G.Home() : home;
                if (facility != "Residence of the deceased") LookupStore.Ensure("hospitals", facility);
                // stored order for place of death: facility, municipality, province
                string place = string.Join(", ", new[] { facility, deathMuni.Name, "Cagayan" });

                string disposal = G.Weighted(new[] { "Burial", "Cremation" }, new[] { 88, 12 });
                string permit = disposal == "Burial" ? "Burial Permit" : "Cremation Permit";
                DateTime ddate = dod.AddDays(G.R.Next(1, 5));
                if (ddate > G.Today) ddate = dod;
                DateTime reg = G.Workday(G.Min(dod.AddDays(G.R.Next(1, 6)), G.Today), dod, G.Today);
                if (reg < dod) reg = dod;

                // informant: a relative living with the family
                string infRel = G.Pick(InformantRel);
                if (civil != "Married" && (infRel == "Wife" || infRel == "Husband")) infRel = G.Pick(new[] { "Son", "Daughter", "Sister" });
                string infName = G.Given(infRel != "Wife" && infRel != "Daughter" && infRel != "Sister" && infRel != "Niece") + " " + (infRel == "Wife" ? last : last);
                string certName = "Dr. " + G.Given(G.Chance(0.5)) + " " + G.Surname();

                v["form_code"] = "MF-103-2016"; v["form_name"] = "Certificate of Death"; v["record_source"] = "Registration";
                v["full_name"] = p.Full; v["first_name"] = p.First; v["middle_name"] = p.Middle; v["last_name"] = p.Last;
                v["sex"] = p.Sex; v["civil_status"] = civil; v["citizenship"] = "Filipino"; v["religion_name"] = p.Religion;
                v["date_of_death"] = dod; v["date_of_birth"] = dob; v["age"] = age;
                v["time_of_death"] = string.Format("{0:D2}:{1:D2}", G.Between(0, 23), G.Between(0, 59));
                v["place_of_death"] = place;
                v["father_name"] = father; v["mother_name"] = mother;
                v["immediate_cause"] = cause.Immediate; v["antecedent_cause"] = cause.Antecedent; v["underlying_cause"] = cause.Underlying;
                v["interval_immediate"] = cause.Interval1; v["interval_antecedent"] = cause.Interval2; v["interval_underlying"] = cause.Interval3;
                v["autopsy"] = "No";
                v["medical_certifier"] = certName; v["certifier_license_no"] = "PRC " + G.Between(1000000, 1999999);
                v["certifier_title"] = facility == "Residence of the deceased" ? "Municipal Health Officer" : "Attending Physician";
                v["certifier_address"] = facility == "Residence of the deceased" ? "Penablanca Rural Health Unit, Cagayan" : facility + ", Cagayan";
                v["attendant_type"] = facility == "Residence of the deceased" ? "Public health officer" : "Private physician";
                v["certifier_attended"] = 1;
                v["attendance_from"] = dod.AddDays(-G.R.Next(1, 14)); v["attendance_to"] = dod;
                v["disposal_method"] = disposal; v["place_of_disposal"] = disposal == "Burial" ? "Penablanca Municipal Cemetery" : "Tuguegarao Memorial Crematorium";
                v["date_of_disposal"] = ddate; v["permit_type"] = permit;
                v["burial_permit_no"] = disposal == "Burial" ? "BP-" + reg.Year + "-" + (i + 1).ToString("D3") : null;
                v["burial_permit_date"] = disposal == "Burial" ? (object)reg : null;
                v["informant_name"] = infName; v["informant_relationship"] = infRel;
                v["informant_address"] = string.Join(", ", new[] { "Cagayan", home.Name, p.Barangay });
                v["remarks"] = G.Marker;
                v["_reg"] = reg; v["_dod"] = dod;
                recs.Add(v);
            }

            // the three newest are still Pending Verification; everything else is Registered
            var order = recs.OrderBy(r => (DateTime)r["_reg"]).ToList();
            int n = 0, pending = 0;
            foreach (var v in order)
            {
                DateTime reg = (DateTime)v["_reg"];
                bool pend = n >= order.Count - 3;
                v.Remove("_reg"); v.Remove("_dod");
                v["status"] = pend ? "Pending Verification" : "Registered";
                string rn = G.NextReg("deaths", 'D', reg.Year);
                int seq = int.Parse(rn.Substring(rn.LastIndexOf('-') + 1));
                v["registry_no"] = rn; v["book_volume"] = reg.Year.ToString(); v["book_page"] = ((seq + 1) / 2).ToString();
                v["prepared_by"] = "Marites S. Dumlao"; v["prepared_by_title"] = "Registration Clerk"; v["prepared_by_date"] = reg;
                v["received_by"] = "Ramon P. Aggabao"; v["received_by_title"] = "Administrative Aide"; v["received_by_date"] = reg;
                v["informant_date"] = reg;
                if (!pend) { v["registered_by"] = "Lorna B. Macaraeg"; v["registered_by_title"] = "Municipal Civil Registrar"; v["registered_by_date"] = reg; }
                DateTime created = G.Office(reg, 8, 16);
                v["created_at"] = created; v["updated_at"] = created;
                // lookups the Master Files tie to (occupation, cause)
                DataTable oc = Db.Pull("SELECT id FROM occupations WHERE name=@n LIMIT 1", G.P("@n", ((string)v["sex"] == "Female" ? "Housewife" : "Farmer")));
                if (oc.Rows.Count > 0 && (int)v["age"] >= 18) v["occupation_id"] = oc.Rows[0][0];
                DataTable cd = Db.Pull("SELECT id FROM causes_of_death WHERE name=@n LIMIT 1", G.P("@n", (string)v["immediate_cause"]));
                if (cd.Rows.Count > 0) v["cause_of_death_id"] = cd.Rows[0][0];
                G.Insert("deaths", v);
                if (pend) pending++;
                n++;
            }
            Console.WriteLine("  {0} deaths ({1} Registered, {2} Pending Verification)", order.Count, order.Count - pending, pending);
        }
    }
}
