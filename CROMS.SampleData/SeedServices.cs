using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CROMS.Data;

namespace CROMS.SampleData
{
    /// <summary>Petitions and case tracking, certificate requests (transaction spine, fees, release) and PSA copy (BREQS) requests.</summary>
    internal static class SeedServices
    {
        // ------------------------------------------------------------------------------------ helpers
        sealed class RecRef { public int Id; public string Type, Display, Requester; }

        static List<RecRef> Records(string type)
        {
            string sql;
            if (type == "Birth")
                sql = "SELECT id, CONCAT_WS(' ', first_name, middle_name, last_name) AS n, CONCAT_WS(' ', mother_first_name, mother_last_name) AS r FROM births WHERE remarks LIKE @m AND status='Registered'";
            else if (type == "Marriage")
                sql = "SELECT id, CONCAT(husband_first_name, ' ', husband_last_name, ' & ', wife_first_name, ' ', wife_last_name) AS n, CONCAT(wife_first_name, ' ', wife_last_name) AS r FROM marriages WHERE remarks LIKE @m AND status='Registered'";
            else
                sql = "SELECT id, full_name AS n, informant_name AS r FROM deaths WHERE remarks LIKE @m AND status='Registered'";
            return Db.Pull(sql, G.P("@m", "%" + G.Marker + "%")).AsEnumerable()
                .Select(r => new RecRef { Id = Convert.ToInt32(r["id"]), Type = type, Display = r["n"].ToString(), Requester = r["r"] == DBNull.Value ? null : r["r"].ToString() }).ToList();
        }

        static FeeItem Fee(string code)
        {
            FeeItem f = PaymentService.Fee(code);
            if (f == null) throw new InvalidOperationException("Fee " + code + " is not in the fee schedule.");
            return f;
        }

        // ------------------------------------------------------------------------------------ petitions
        public static void Petitions()
        {
            if (G.Already("petitions", "remarks")) { Console.WriteLine("petitions: already seeded - skipped"); return; }
            Console.WriteLine("Petitions and case tracking ...");
            var births = Records("Birth"); var marriages = Records("Marriage"); var deaths = Records("Death");
            if (births.Count == 0) { G.Note("no sample births to attach petitions to - petitions need Birth records first"); return; }

            // type, record kind, remark, requester relationship
            var plan = new[]
            {
                new { T = "RA9048", K = "Birth", R = "Correction of clerical error: misspelled first name of the child." , Rel = "Mother" },
                new { T = "RA9048", K = "Birth", R = "Correction of clerical error: wrong entry in the sex of the child.", Rel = "Father" },
                new { T = "RA9048", K = "Birth", R = "Change of first name (nickname used since childhood).", Rel = "Self" },
                new { T = "RA9048", K = "Marriage", R = "Correction of clerical error: misspelled surname of the husband.", Rel = "Husband" },
                new { T = "RA10172", K = "Birth", R = "Correction of day and month of birth.", Rel = "Mother" },
                new { T = "RA10172", K = "Birth", R = "Correction of sex entry from Male to Female (congenital condition, medical certification attached).", Rel = "Self" },
                new { T = "Legitimation", K = "Birth", R = "Legitimation of the child by subsequent marriage of the parents (RA 9858).", Rel = "Father" },
                new { T = "Legitimation", K = "Birth", R = "Legitimation of the child by subsequent marriage of the parents (RA 9858).", Rel = "Mother" },
                new { T = "SupplementalReport", K = "Birth", R = "Supplemental report: father's occupation omitted at registration.", Rel = "Mother" },
                new { T = "SupplementalReport", K = "Death", R = "Supplemental report: cause of death omitted at registration.", Rel = "Son" },
                new { T = "LegalInstrument", K = "Birth", R = "Affidavit of Acknowledgment / Admission of Paternity (RA 9255).", Rel = "Father" },
                new { T = "LegalInstrument", K = "Birth", R = "Affidavit to Use the Surname of the Father (AUSF).", Rel = "Mother" },
                new { T = "LegalInstrument", K = "Birth", R = "Private handwritten instrument of acknowledgment.", Rel = "Father" },
                new { T = "CourtOrder", K = "Marriage", R = "Court decree of annulment received for annotation on the marriage record.", Rel = "Husband" },
                new { T = "CourtOrder", K = "Birth", R = "Court order for change of surname, with Certificate of Finality.", Rel = "Self" },
            };
            string[] rastages = { "Filed", "Posted", "Decision", "PSA_Endorsement" };
            string[] otherstages = { "Filed", "UnderReview", "Decision", "PSA_Endorsement" };
            int done = 0, paid = 0;
            foreach (var p in plan.OrderBy(x => G.R.Next()))
            {
                List<RecRef> pool = p.K == "Birth" ? births : p.K == "Marriage" ? marriages : deaths;
                if (pool.Count == 0) pool = births;
                RecRef rec = G.Pick(pool);
                bool ra = p.T == "RA9048" || p.T == "RA10172";
                string[] stages = ra ? rastages : otherstages;
                int stageIdx = done % 4 == 0 ? 3 : done % 4 == 1 ? 0 : done % 4 == 2 ? 2 : 1;
                DateTime filed = G.Workday(G.Today.AddDays(-G.R.Next(4, 160)), G.Today.AddDays(-200), G.Today);
                string requester = p.Rel == "Self" ? rec.Display : rec.Requester ?? G.Given(true) + " " + G.Surname();
                var v = new Dictionary<string, object>
                {
                    { "petition_type", p.T }, { "record_type", rec.Type }, { "record_id", rec.Id }, { "stage", stages[stageIdx] },
                    { "filed_date", filed }, { "remarks", G.Marker + " - " + p.R },
                    { "requester_name", requester }, { "requester_relationship", p.Rel },
                    { "created_at", G.Office(filed) }, { "updated_at", G.Office(G.Min(filed.AddDays(stageIdx * 9), G.Today)) }
                };
                int id = (int)G.Insert("petitions", v);
                PetitionDocumentService.SyncRequirements(p.T, id);
                foreach (ReqRow r in PetitionDocumentService.Requirements(id).Take(stageIdx == 0 ? 1 : 10))
                {
                    r.Status = "Verified"; r.ReferenceNo = "PD-" + id + "-" + r.Code; r.DocDate = filed;
                    MarriageService.SaveRequirement(r);
                }
                // filing fee, for the types the fee card prices
                string feeCode = p.T == "RA9048" ? (p.R.StartsWith("Change") ? "PET-9048-CFN" : "PET-9048-CCE") : p.T == "RA10172" ? "PET-10172" : null;
                if (feeCode != null && filed <= G.Today)
                {
                    FeeItem fee = Fee(feeCode);
                    PaymentService.Record(new PaymentEntry
                    {
                        Source = PaymentService.SourcePetition, SourceTable = "petitions", SourceId = id, PayerName = requester,
                        Purpose = "Petition filing - " + p.T, OrNumber = SeedMarriages.NextOr(), Method = "Cash",
                        Tendered = fee.Amount, PaidAt = G.Office(filed, 9, 15), Remarks = G.Marker,
                        Lines = { new PaymentLine { FeeCode = fee.Code, Description = fee.Description, Quantity = 1, UnitAmount = fee.Amount ?? 0m } }
                    }, G.UserId);
                    paid++;
                }
                done++;
            }
            Console.WriteLine("  {0} petitions / cases ({1} with a filing-fee payment)", done, paid);
        }

        // ------------------------------------------------------------------------------------ certificate requests
        sealed class Cr
        {
            public string Status, Type, Purpose; public RecRef Rec; public int Copies; public string Client; public DateTime Created;
            public bool Negative;
        }

        public static void CertificateRequests()
        {
            if (G.Already("certificate_requests", "purpose")) { Console.WriteLine("certificate requests: already seeded - skipped"); return; }
            Console.WriteLine("Certificate requests, payments and releases ...");
            var pools = new Dictionary<string, List<RecRef>> { { "Birth", Records("Birth") }, { "Marriage", Records("Marriage") }, { "Death", Records("Death") } };
            if (pools.Values.All(p => p.Count == 0)) { G.Note("no sample records to request certificates for - run births/deaths/marriages first"); return; }
            string[] purposes = { "Passport application", "School enrollment", "Employment requirement", "PhilHealth enrollment", "SSS claim", "Bank requirement", "Scholarship application", "Insurance claim", "Land title transfer", "Personal copy" };

            var statuses = new List<string>();
            statuses.AddRange(Enumerable.Repeat("ForPrint", 3)); statuses.AddRange(Enumerable.Repeat("ForPayment", 4));
            statuses.AddRange(Enumerable.Repeat("ForRelease", 5)); statuses.AddRange(Enumerable.Repeat("WaitingToRelease", 3));
            statuses.AddRange(Enumerable.Repeat("Released", 13)); statuses.AddRange(Enumerable.Repeat("Cancelled", 2));
            var list = new List<Cr>();
            foreach (string st in statuses)
            {
                string type = G.Weighted(new[] { "Birth", "Marriage", "Death" }, new[] { 55, 20, 25 });
                if (pools[type].Count == 0) type = pools.First(p => p.Value.Count > 0).Key;
                RecRef rec = G.Pick(pools[type]);
                int daysAgo = st == "Released" ? G.Between(2, 75) : st == "Cancelled" ? G.Between(5, 40) : st == "ForPrint" ? G.Between(0, 1) : G.Between(0, 14);
                DateTime d = G.Workday(G.Today.AddDays(-daysAgo), G.Today.AddDays(-120), G.Today);
                bool neg = G.Chance(0.08);
                list.Add(new Cr
                {
                    Status = st, Type = type, Rec = neg ? null : rec, Negative = neg, Copies = G.Weighted(new[] { 1, 2, 3 }, new[] { 62, 28, 10 }),
                    Purpose = G.Pick(purposes), Created = G.Office(d, 8, 15),
                    Client = rec.Requester ?? G.Given(G.Chance(0.5)) + " " + G.Surname()
                });
            }

            int n = 0, pays = 0, rels = 0;
            foreach (Cr c in list.OrderBy(x => x.Created))
            {
                string code = NextTxnCode(c.Created.Year);
                string txnStatus = c.Status;
                var txn = new Dictionary<string, object>
                {
                    { "txn_code", code }, { "client_name", c.Client }, { "type", "Certification" }, { "status", txnStatus },
                    { "created_by", G.UserId }, { "created_at", c.Created }, { "updated_at", c.Created }
                };
                if (c.Status == "WaitingToRelease")
                {
                    txn["parked_at"] = c.Created.AddMinutes(G.Between(20, 90));
                    txn["parked_reason"] = G.Pick(new[] { "Client left to withdraw cash", "Client will return with a valid ID", "Waiting for the registrar's signature" });
                }
                int txnId = (int)G.Insert("transactions", txn);
                string crStatus = c.Status == "ForRelease" ? "Ready" : c.Status == "Released" ? "Released" : c.Status == "Cancelled" ? "Requested" : "Processing";
                G.Insert("certificate_requests", new Dictionary<string, object>
                {
                    { "transaction_id", txnId }, { "record_type", c.Negative ? null : c.Type }, { "record_id", c.Rec == null ? null : (object)c.Rec.Id },
                    { "cert_type", c.Negative ? "Negative" : "CTC" }, { "copies", c.Copies },
                    { "purpose", c.Purpose + " · " + G.Marker }, { "status", crStatus }, { "created_at", c.Created }
                });

                DateTime last = c.Created;
                if (c.Status == "ForRelease" || c.Status == "Released")
                {
                    DateTime paidAt = G.Office(G.Min(c.Created.Date.AddDays(G.R.Next(0, 3)), G.Today), 8, 16);
                    if (paidAt < c.Created) paidAt = c.Created.AddMinutes(G.Between(15, 90));
                    if (paidAt > DateTime.Now) paidAt = DateTime.Now.AddMinutes(-G.Between(2, 30));
                    PaymentLine line = PaymentService.AssessCertificate(c.Negative ? "Negative" : "CTC", c.Type, c.Copies);
                    bool cash = G.Chance(0.86);
                    decimal total = line.LineAmount;
                    PaymentService.Record(new PaymentEntry
                    {
                        TransactionId = txnId, Source = PaymentService.SourceTransaction, PayerName = c.Client, Purpose = c.Purpose,
                        OrNumber = SeedMarriages.NextOr(), Method = cash ? "Cash" : "GCash", ReferenceNo = cash ? null : "GC" + G.Between(10000000, 99999999),
                        Tendered = cash ? (decimal?)(Math.Ceiling(total / 100m) * 100m) : null, PaidAt = paidAt, Remarks = G.Marker,
                        Lines = { line }
                    }, G.UserId);
                    pays++; last = paidAt;
                }
                if (c.Status == "Released")
                {
                    DateTime relAt = last.AddMinutes(G.Between(10, 180));
                    if (relAt > DateTime.Now) relAt = DateTime.Now.AddMinutes(-G.Between(1, 20));
                    bool rep = G.Chance(0.2);
                    G.Insert("releases", new Dictionary<string, object>
                    {
                        { "transaction_id", txnId }, { "claimant_name", rep ? G.Given(G.Chance(0.5)) + " " + G.Surname() : c.Client },
                        { "is_representative", rep ? 1 : 0 },
                        { "representative_id_type", rep ? G.Pick(GovIds.All.Where(x => x != "Other").ToList()) : null },
                        { "representative_id_number", rep ? G.Between(10, 99) + "-" + G.Between(1000, 9999) + "-" + G.Between(100000, 999999) : null },
                        { "released_by", G.UserId }, { "released_at", relAt }
                    });
                    rels++; last = relAt;
                }
                Db.Push("UPDATE transactions SET updated_at=@u WHERE id=@id", G.P("@u", last), G.P("@id", txnId));
                n++;
            }
            Console.WriteLine("  {0} certificate requests ({1} paid, {2} released)", n, pays, rels);
        }

        static string NextTxnCode(int year)
        {
            int n = G.Scalar("SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(txn_code,'-',-1) AS UNSIGNED)),0)+1 FROM transactions WHERE txn_code LIKE @p", G.P("@p", "TXN-" + year + "-%"));
            return string.Format("TXN-{0}-{1:D6}", year, n);
        }

        // ------------------------------------------------------------------------------------ BREQS
        public static void Breqs()
        {
            if (G.Already("psa_copy_requests", "remarks")) { Console.WriteLine("PSA copy requests: already seeded - skipped"); return; }
            Console.WriteLine("PSA copy (BREQS) requests ...");
            string[] stages = { "Requested", "Requested", "Paid", "Paid", "Paid", "Submitted", "Submitted", "Submitted", "NoRecord", "Cancelled" };
            int i = 0;
            foreach (string stage in stages)
            {
                string doc = G.Weighted(new[] { BreqsService.Birth, BreqsService.Marriage, BreqsService.Death }, new[] { 60, 25, 15 });
                DateTime created = G.Workday(G.Today.AddDays(-(stage == "Submitted" && i == 5 ? 22 : G.R.Next(2, 40))), G.Today.AddDays(-60), G.Today);
                string last = G.Surname();
                bool male = G.Chance(0.5);
                var r = new BreqsRequest
                {
                    Source = i % 4 == 3 ? BreqsService.SourceKiosk : BreqsService.SourceCounter,
                    RequesterFirst = G.Given(G.Chance(0.5)), RequesterLast = last, Relationship = G.Pick(BreqsService.Relationships.Take(4).ToList()),
                    ValidIdType = G.Pick(GovIds.All.Where(x => x != "Other").Take(8).ToList()), ValidIdNo = G.Between(1000, 9999) + "-" + G.Between(100000, 999999),
                    ContactNo = null, DocType = doc, Copies = G.Weighted(new[] { 1, 2, 3 }, new[] { 70, 22, 8 }),
                    Purpose = G.Pick(BreqsService.Purposes.Take(8).ToList()),
                    OwnerFirst = G.Given(male), OwnerMiddle = G.Surname(), OwnerLast = last,
                    EventDate = created.AddYears(-G.Between(2, 40)).AddDays(-G.R.Next(0, 300)), EventCity = "Penablanca", EventProvince = "Cagayan",
                    Remarks = G.Marker
                };
                if (doc == BreqsService.Marriage) { r.SpouseFirst = G.Given(false); r.SpouseMiddle = G.Surname(); r.SpouseLast = G.Surname(); r.OwnerFirst = G.Given(true); }
                if (doc == BreqsService.Birth) { r.FatherName = G.Given(true) + " " + G.Surname() + " " + last; r.MotherMaidenName = G.Given(false) + " " + G.Surname() + " " + G.Surname(); }
                int id = BreqsService.Save(r, G.UserId);
                Db.Push("UPDATE psa_copy_requests SET created_at=@c, updated_at=@c WHERE id=@id", G.P("@c", G.Office(created)), G.P("@id", id));

                decimal fee = BreqsService.Settings.FeePerCopy * r.Copies;
                if (stage == "Paid" || stage == "Submitted" || stage == "NoRecord")
                    BreqsService.RecordPayment(id, SeedMarriages.NextOr(), G.Min(created.AddDays(G.R.Next(0, 2)), G.Today), fee, G.UserId);
                if (stage == "Submitted" || stage == "NoRecord")
                {
                    DateTime sub = G.Min(created.AddDays(G.R.Next(1, 4)), G.Today.AddDays(-1));
                    BreqsService.SubmitToPsa(id, "BQ" + sub.ToString("yyMMdd") + G.Between(100, 999), sub, G.UserId);
                }
                if (stage == "NoRecord") BreqsService.MarkNoRecord(id, "PSA found no record: the event may not have been transmitted. Negative certification issued.", G.UserId);
                if (stage == "Cancelled") BreqsService.Cancel(id, "Requester no longer needs the document.", G.UserId);
                i++;
            }
            Console.WriteLine("  {0} PSA copy requests (Requested 2, Paid 3, Submitted 3 (one past its expected date), No record 1, Cancelled 1)", stages.Length);
            G.Note("PSA copies stop at 'Submitted to PSA': 'Received' needs a scanned copy and no scans are attached");
        }
    }
}
