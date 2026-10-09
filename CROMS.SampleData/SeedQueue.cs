using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CROMS.Data;

namespace CROMS.SampleData
{
    /// <summary>
    /// Fourteen days of kiosk queue tickets, written the way KioskCore.Submit and the window flow leave them
    /// (continuous Q-numbers, one queue_ticket_services row per service, accept -> call -> serve -> complete
    /// timestamps). Every ticket is already CLOSED (completed, abandoned or never called): a ticket left
    /// Waiting would be offered to a real window by Call Next.
    /// </summary>
    internal static class SeedQueue
    {
        static readonly string[,] Services =
        {
            { "BIRTHREG", "Birth Registration" }, { "MARRIAGE_REG", "Marriage Registration" }, { "DEATH", "Death Registration" },
            { "MARRIAGE_APP", "Marriage Application" }, { "LEGITIMATION", "Legitimation" }, { "CTC", "Certified True Copy (CTC)" },
            { "BREQS", "PSA Copy (BREQS)" }, { "CLAIM", "Release & Claim (Pick-up)" }, { "PETITION", "Petition (Correction)" },
            { "SUPPLEMENTAL_REPORT", "Supplemental Report" }, { "LEGAL_INSTRUMENTS", "Legal Instruments" }, { "COURT_ORDER", "Court Order" }
        };
        static readonly int[] ServiceWeight = { 12, 6, 9, 7, 2, 28, 12, 14, 4, 2, 2, 2 };
        static readonly string[] Operators = { "Marites S. Dumlao", "Ramon P. Aggabao", "Josefina A. Pascua", "Elmer R. Binuya" };
        static readonly int[] HourWeight = { 8, 14, 16, 11, 3, 9, 15, 12, 6 };   // 08:00 .. 16:00

        public static void Run()
        {
            if (G.Already("queue_tickets", "purpose")) { Console.WriteLine("queue tickets: already seeded - skipped"); return; }
            Console.WriteLine("Queue tickets ...");
            int num = G.Scalar("SELECT COALESCE(MAX(number_queue), 0) FROM queue_tickets WHERE ticket_code LIKE 'Q-%'");
            // active windows (ids), for window_no / accepted_window
            var windows = Db.Pull("SELECT id FROM windows WHERE status='Active' ORDER BY display_order, id").AsEnumerable().Select(r => Convert.ToInt32(r[0])).ToList();
            if (windows.Count == 0) windows.Add(1);

            var txnByRecent = Db.Pull("SELECT t.id FROM transactions t JOIN certificate_requests c ON c.transaction_id=t.id WHERE c.purpose LIKE @m ORDER BY t.id",
                                      G.P("@m", "%" + G.Marker + "%")).AsEnumerable().Select(r => Convert.ToInt32(r[0])).ToList();
            var birthIds = Db.Pull("SELECT id FROM births WHERE remarks LIKE @m AND status='Registered' ORDER BY id", G.P("@m", "%" + G.Marker + "%"))
                             .AsEnumerable().Select(r => Convert.ToInt32(r[0])).ToList();

            int tickets = 0, completed = 0, abandoned = 0, noShow = 0, intake = 0, linkedTxn = 0;
            for (int back = 13; back >= 0; back--)
            {
                DateTime day = G.Today.AddDays(-back);
                if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday) continue;
                int count = day.DayOfWeek == DayOfWeek.Friday ? G.Between(7, 11) : G.Between(8, 14);
                var times = new List<DateTime>();
                for (int i = 0; i < count; i++)
                {
                    int hour = 8 + G.Weighted(Enumerable.Range(0, HourWeight.Length).ToList(), HourWeight);
                    DateTime t = day.AddHours(hour).AddMinutes(G.R.Next(0, 60)).AddSeconds(G.R.Next(0, 60));
                    // a ticket is only as old as the clock allows (a queue for today ends before now)
                    if (day == G.Today && t > DateTime.Now.AddMinutes(-40)) continue;
                    times.Add(t);
                }
                foreach (DateTime issued in times.OrderBy(x => x))
                {
                    num++;
                    string code = "Q-" + num.ToString("D3");
                    int nServices = G.Weighted(new[] { 1, 2, 3 }, new[] { 70, 24, 6 });
                    var picked = new List<int>();
                    while (picked.Count < nServices)
                    {
                        int s = G.Weighted(Enumerable.Range(0, ServiceWeight.Length).ToList(), ServiceWeight);
                        if (!picked.Contains(s)) picked.Add(s);
                    }
                    string label = string.Join(", ", picked.Select(i => Services[i, 1]));
                    string priority = G.Weighted(new[] { "Regular", "Senior", "PWD", "Priority" }, new[] { 80, 11, 6, 3 });
                    bool male = G.Chance(0.45);
                    string name = G.Given(male) + " " + G.Surname();
                    string spouse = null;
                    if (picked.Any(i => Services[i, 0] == "MARRIAGE_APP")) spouse = G.Given(!male) + " " + G.Surname();

                    // the dice: never called / abandoned / served
                    double roll = G.R.NextDouble();
                    bool noshow = roll < 0.04, abandon = !noshow && roll < 0.11;
                    int win = windows[G.R.Next(windows.Count)];
                    DateTime accepted = issued.AddMinutes(G.Between(2, day == G.Today ? 20 : 38));
                    DateTime called = accepted.AddMinutes(G.Between(1, 4));
                    DateTime started = called.AddMinutes(G.Between(0, 2));
                    int svcMinutes = 5 + 7 * picked.Count + G.Between(0, 18);
                    DateTime done = started.AddMinutes(svcMinutes);
                    if (!noshow && done > DateTime.Now) { done = DateTime.Now.AddMinutes(-2); if (started > done) { noshow = true; } }

                    var v = new Dictionary<string, object>
                    {
                        { "ticket_code", code }, { "full_name", name }, { "spouse_full_name", spouse },
                        { "purpose", label + " [" + G.Marker + "]" }, { "valid_id_type", G.Chance(0.6) ? G.Pick(GovIds.All.Where(x => x != "Other").Take(8).ToList()) : null },
                        { "number_queue", num }, { "date", day }, { "time", issued.ToString("HH:mm") },
                        { "document_type", Services[picked[0], 1] }, { "type_label", label }, { "priority", priority },
                        { "is_priority_ticket", priority == "Regular" ? 0 : 1 },
                        { "created_at", issued }
                    };
                    if (noshow)
                    {
                        v["status"] = "Cancelled"; v["final_status"] = "Cancelled";
                        noShow++;
                    }
                    else
                    {
                        v["status"] = "Completed"; v["final_status"] = abandon ? "Abandoned" : "Completed";
                        v["accepted_at"] = accepted; v["accepted_window"] = win; v["called_at"] = called; v["started_at"] = started;
                        v["completed_at"] = done; v["window_no"] = win; v["served_by"] = Operators[(win - 1) % Operators.Length];
                        v["recall_count"] = G.Chance(0.12) ? G.Between(1, 2) : 0;
                        if (abandon) abandoned++; else completed++;
                    }
                    // link a CTC ticket to a certificate-request transaction, a Birth Registration ticket to a registered birth
                    if (!noshow && !abandon)
                    {
                        if (picked.Any(i => Services[i, 0] == "CTC") && txnByRecent.Count > 0 && G.Chance(0.5))
                        { v["transaction_id"] = txnByRecent[linkedTxn % txnByRecent.Count]; linkedTxn++; }
                        else if (picked.Any(i => Services[i, 0] == "BIRTHREG") && birthIds.Count > 0)
                            v["birth_id"] = birthIds[tickets % birthIds.Count];
                    }
                    int tid = (int)G.Insert("queue_tickets", v);

                    // one row per service; a multi-service visit is worked in order
                    DateTime svcAt = started;
                    for (int k = 0; k < picked.Count; k++)
                    {
                        string st = noshow ? "Pending" : abandon && k > 0 ? "Abandoned" : "Completed";
                        var sv = new Dictionary<string, object>
                        {
                            { "ticket_id", tid }, { "service_code", Services[picked[k], 0] }, { "service_label", Services[picked[k], 1] },
                            { "status", noshow ? "Pending" : st }, { "created_at", issued }
                        };
                        if (noshow) sv["status"] = "Abandoned";
                        if (st == "Abandoned" || noshow)
                        {
                            sv["abandoned_at"] = noshow ? issued.AddMinutes(45) : done; sv["abandoned_by"] = G.UserId;
                            sv["abandon_reason"] = noshow ? "Client did not respond when called." : G.Pick(new[] { "Client left before the remaining task could be served.", "Missing requirement; client will return." });
                        }
                        G.Insert("queue_ticket_services", sv);
                    }
                    // the CTC intake the kiosk collected
                    if (picked.Any(i => Services[i, 0] == "CTC") && G.Chance(0.6))
                    {
                        string doc = G.Weighted(new[] { "Birth", "Marriage", "Death" }, new[] { 60, 22, 18 });
                        string ol = G.Surname();
                        G.Insert("kiosk_ctc_intake", new Dictionary<string, object>
                        {
                            { "source", "Kiosk" }, { "queue_ticket_id", tid }, { "transaction_id", v.ContainsKey("transaction_id") ? v["transaction_id"] : null },
                            { "doc_type", doc }, { "copies", G.Weighted(new[] { 1, 2, 3 }, new[] { 70, 22, 8 }) }, { "purpose", G.Pick(new[] { "Passport", "School", "Employment", "Personal copy" }) },
                            { "relationship", G.Pick(new[] { "Self", "Parent", "Child", "Spouse" }) },
                            { "owner_first", G.Given(male) }, { "owner_last", ol },
                            { "event_city", "Penablanca" }, { "event_province", "Cagayan" },
                            { "remarks", G.Marker }, { "status", "Requested" }, { "created_at", issued }, { "updated_at", issued }
                        });
                        intake++;
                    }
                    tickets++;
                }
            }
            Console.WriteLine("  {0} tickets over 14 days ({1} completed, {2} abandoned, {3} never called), {4} CTC intake rows, {5} linked to a certificate request",
                tickets, completed, abandoned, noShow, intake, linkedTxn);
        }
    }
}
