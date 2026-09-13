using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CROMS.Data;

namespace CROMS.MarriageTest
{
    /// <summary>
    /// Fee schedule and payment log against the LIVE database: the office's fee card, assessment,
    /// validation, one O.R. recorded once, a walk-in adopted by a module, BREQS writing to the log,
    /// a month's collection report that adds up, an audited fee change, and the screen rendered.
    /// Test payments carry payer / O.R. "ZZF..." and are dated in 2099 so they never reach a real
    /// month; everything is deleted afterwards.
    /// </summary>
    internal static class FeesTest
    {
        public static int Pass, Fail;
        private const string Tag = "ZZF";
        private static readonly string Run_ = DateTime.Now.ToString("HHmmss");
        private static readonly DateTime Jan2099 = new DateTime(2099, 1, 15, 10, 0, 0);

        private static void Check(string name, bool ok, string detail = null)
        {
            if (ok) Pass++; else Fail++;
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + (detail == null ? "" : "   -> " + detail));
        }

        private static bool Throws(Action a, string contains = null)
        {
            try { a(); return false; }
            catch (Exception ex) { return contains == null || ex.Message.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0; }
        }

        private static string Or(string n) { return Tag + "-" + Run_ + "-" + n; }

        public static void Run(string dir, int uid)
        {
            Directory.CreateDirectory(dir);
            Cleanup();
            FeeItem annotation = PaymentService.Fee("ANNOTATION");
            try
            {
                Schedule();
                Validation();
                Recording(uid);
                Breqs(uid);
                Monthly();
                FeeChange(uid, annotation);
                Render(dir);
            }
            finally
            {
                if (annotation != null) PaymentService.UpdateFee("ANNOTATION", annotation.Amount, annotation.Active, uid);
                Cleanup();
                int left = Convert.ToInt32(Db.Pull("SELECT (SELECT COUNT(*) FROM payments WHERE or_number LIKE 'ZZF%' OR payer_name LIKE 'ZZF%') + " +
                                                   "(SELECT COUNT(*) FROM breqs_requests WHERE requester_last LIKE 'ZZF%')").Rows[0][0]);
                Check("zero strays after cleanup", left == 0, left + " left");
            }
        }

        // ------------------------------------------------------------ the office's fee card
        private static void Schedule()
        {
            var want = new Dictionary<string, decimal?>
            {
                { "BREQS", 50m }, { "CTC-BIRTH", 80m }, { "CTC-MARRIAGE", 80m }, { "CTC-DEATH", 80m }, { "NEG-CERT", 130m },
                { "ANNOTATION", 100m }, { "MAR-APPLICATION", 1000m }, { "MAR-SOLEMNIZATION", 1000m }, { "MAR-LICENSE", 200m },
                { "ENDORSE-ELECTRONIC", 500m }, { "REG-OUT-OF-TOWN", 500m }, { "PET-9048-CCE", 1000m }, { "PET-10172", 3000m },
                { "PET-9048-CFN", 3000m }, { "PET-MIGRANT", 1000m }, { "BURIAL", null }, { "TRANSFER-CADAVER", null }
            };
            List<FeeItem> active = PaymentService.Fees(true);
            foreach (var kv in want)
            {
                FeeItem f = active.FirstOrDefault(x => x.Code == kv.Key);
                Check("card fee " + kv.Key + " = " + PaymentService.Money(kv.Value), f != null && f.Amount == kv.Value, f == null ? "missing / inactive" : PaymentService.Money(f.Amount));
            }
            Check("15 priced fees + 2 unpriced on the card are the active schedule", active.Count == 17, active.Count + " active");
            Check("combined PET-9048 retired (the card charges CCE and CFN differently)", !active.Any(f => f.Code == "PET-9048"));
            Check("registration seeds not on the card are inactive", !active.Any(f => f.Code.StartsWith("REG-") && f.Code != "REG-OUT-OF-TOWN"));

            PaymentLine ctc = PaymentService.AssessCertificate("CTC", "Birth", 3);
            Check("CTC birth x3 = 240 (80 each)", ctc.FeeCode == "CTC-BIRTH" && ctc.Quantity == 3 && ctc.LineAmount == 240m, ctc.LineAmount.ToString());
            PaymentLine neg = PaymentService.AssessCertificate("Negative", "Death", 1);
            Check("negative certification = 130", neg.FeeCode == "NEG-CERT" && neg.LineAmount == 130m, neg.LineAmount.ToString());
            PaymentLine zero = PaymentService.AssessCertificate("CTC", "Marriage", 0);
            Check("copies below 1 are charged as 1", zero.Quantity == 1 && zero.LineAmount == 80m);
        }

        // ------------------------------------------------------------ validation
        private static PaymentEntry WalkIn(string payer, string or, params PaymentLine[] lines)
        {
            var e = new PaymentEntry { Source = PaymentService.SourceWalkIn, PayerName = payer, Purpose = "Certified copy", OrNumber = or, PaidAt = Jan2099 };
            e.Lines.AddRange(lines);
            return e;
        }

        private static PaymentLine L(string code, decimal unit, int qty = 1) { return new PaymentLine { FeeCode = code, Description = code + " fee", UnitAmount = unit, Quantity = qty }; }

        private static void Validation()
        {
            Check("no O.R. refused", PaymentService.Validate(WalkIn(Tag + "A", "", L("CTC-BIRTH", 80))).Any(x => x.Contains("Official Receipt")));
            Check("walk-in without payer refused", PaymentService.Validate(WalkIn("", Or("v1"), L("CTC-BIRTH", 80))).Any(x => x.Contains("payer")));
            var noPurpose = WalkIn(Tag + "A", Or("v2"), L("CTC-BIRTH", 80)); noPurpose.Purpose = " ";
            Check("walk-in without purpose refused", PaymentService.Validate(noPurpose).Any(x => x.Contains("purpose")));
            Check("no fee lines refused", PaymentService.Validate(WalkIn(Tag + "A", Or("v3"))).Any(x => x.Contains("at least one fee")));
            Check("unpriced fee left at 0 refused", PaymentService.Validate(WalkIn(Tag + "A", Or("v4"), L("BURIAL", 0))).Any(x => x.Contains("more than zero")));
            var gcash = WalkIn(Tag + "A", Or("v5"), L("CTC-BIRTH", 80)); gcash.Method = "GCash";
            Check("GCash without reference refused", PaymentService.Validate(gcash).Any(x => x.Contains("reference")));
            var shortCash = WalkIn(Tag + "A", Or("v6"), L("CTC-BIRTH", 80)); shortCash.Tendered = 50;
            Check("cash tendered below total refused", PaymentService.Validate(shortCash).Any(x => x.Contains("tendered")));
            var ok = WalkIn(Tag + "A", Or("v7"), L("CTC-BIRTH", 80, 2), L("ANNOTATION", 100)); ok.Additional = 20; ok.Tendered = 300;
            Check("valid itemised walk-in passes, total 280", PaymentService.Validate(ok).Count == 0 && ok.Total == 280m, string.Join("; ", PaymentService.Validate(ok)));
        }

        // ------------------------------------------------------------ recording
        private static void Recording(int uid)
        {
            var e = WalkIn(Tag + "Juan Dela Cruz", Or("r1"), L("CTC-BIRTH", 80, 2), L("ANNOTATION", 100));
            e.Additional = 20; e.Tendered = 300; e.Remarks = "test";
            long auditFloor = Convert.ToInt64(Db.Pull("SELECT COALESCE(MAX(id), 0) FROM audit_log").Rows[0][0]);
            int id = PaymentService.Record(e, uid);
            DataRow p = Db.Pull("SELECT * FROM payments WHERE id = " + id).Rows[0];
            Check("payment row: net 280, gross 260, change 20, walk-in, no transaction",
                  Convert.ToDecimal(p["net_amount"]) == 280m && Convert.ToDecimal(p["gross_amount"]) == 260m &&
                  Convert.ToDecimal(p["change_amount"]) == 20m && (string)p["source"] == "Walk-in" && p["transaction_id"] == DBNull.Value);
            DataTable items = Db.Pull("SELECT fee_code, fee_id, quantity, unit_amount, line_amount FROM payment_items WHERE payment_id = " + id + " ORDER BY id");
            Check("two fee lines, fee_id resolved from the code",
                  items.Rows.Count == 2 && (string)items.Rows[0]["fee_code"] == "CTC-BIRTH" && Convert.ToInt32(items.Rows[0]["quantity"]) == 2 &&
                  Convert.ToDecimal(items.Rows[0]["line_amount"]) == 160m && items.Rows[1]["fee_id"] != DBNull.Value);
            int audits = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM audit_log WHERE table_name='payments' AND record_id = '" + id + "' AND id > " + auditFloor).Rows[0][0]);
            Check("payment audited", audits == 1, audits + " audit rows for payment " + id);

            Check("same O.R. recorded twice is refused",
                  Throws(() => PaymentService.Record(WalkIn(Tag + "Other", Or("r1"), L("CTC-DEATH", 80)), uid), "recorded once"));

            DataTable log = PaymentService.Log(Jan2099.Date, Jan2099.Date, Or("r1"));
            Check("log search by O.R. finds it, fees listed", log.Rows.Count == 1 && log.Rows[0]["Fees"].ToString().Contains("x2"), log.Rows.Count == 0 ? "none" : log.Rows[0]["Fees"].ToString());

            // A walk-in logged first, then the same O.R. recorded in BREQS: adopted, not doubled.
            int walk = PaymentService.Record(WalkIn(Tag + "Adopt", Or("r2"), L("BREQS", 50)), uid);
            int adopted = PaymentService.RecordForModule(new PaymentEntry
            {
                Source = PaymentService.SourceBreqs, SourceTable = "breqs_requests", SourceId = 999999001, OrNumber = Or("r2"),
                Lines = { L("BREQS", 50) }, PaidAt = Jan2099
            }, uid);
            DataRow a = Db.Pull("SELECT source, source_table, source_id FROM payments WHERE id = " + walk).Rows[0];
            Check("unlinked walk-in O.R. adopted by the module (same row, now linked)",
                  adopted == walk && (string)a["source"] == "BREQS" && Convert.ToInt32(a["source_id"]) == 999999001);
            Check("same module + same O.R. again returns the same row",
                  PaymentService.RecordForModule(new PaymentEntry { Source = PaymentService.SourceBreqs, SourceTable = "breqs_requests", SourceId = 999999001, OrNumber = Or("r2"), Lines = { L("BREQS", 50) } }, uid) == walk);
            Check("O.R. linked to another record refused before a module moves",
                  Throws(() => PaymentService.EnsureOrFree(Or("r2"), "breqs_requests", 999999002), "recorded once"));
            Check("O.R. linked to this record is not refused", !Throws(() => PaymentService.EnsureOrFree(Or("r2"), "breqs_requests", 999999001)));
        }

        // ------------------------------------------------------------ BREQS writes to the log
        private static void Breqs(int uid)
        {
            var r = new BreqsRequest
            {
                Source = BreqsService.SourceCounter, RequesterFirst = "Sheila", RequesterLast = Tag + "Talosig", ContactNo = "09171234567",
                Relationship = "Parent", ValidIdType = "Philippine National ID (PhilSys)", ValidIdNo = "1234", DocType = BreqsService.Birth, Copies = 3,
                Purpose = "Passport / DFA", OwnerFirst = "Shellian", OwnerLast = "Talosig", EventDate = new DateTime(2018, 6, 12),
                EventCity = "Tuguegarao City", EventProvince = "Cagayan", FatherName = "Gilbert Talosig", MotherMaidenName = "Sheila Baloso"
            };
            int id = BreqsService.Save(r, uid);
            BreqsService.RecordPayment(id, Or("b1"), Jan2099.Date, 150m, uid);
            DataTable p = Db.Pull("SELECT p.id, p.net_amount, p.source, p.payer_name, i.fee_code, i.quantity, i.unit_amount FROM payments p " +
                                  "JOIN payment_items i ON i.payment_id = p.id WHERE p.source_table = 'breqs_requests' AND p.source_id = " + id);
            Check("BREQS payment in the log: 3 copies x 50, source BREQS, payer is the requester",
                  p.Rows.Count == 1 && Convert.ToDecimal(p.Rows[0]["net_amount"]) == 150m && (string)p.Rows[0]["source"] == "BREQS" &&
                  (string)p.Rows[0]["fee_code"] == "BREQS" && Convert.ToInt32(p.Rows[0]["quantity"]) == 3 && Convert.ToDecimal(p.Rows[0]["unit_amount"]) == 50m &&
                  p.Rows[0]["payer_name"].ToString().Contains(Tag + "Talosig"), p.Rows.Count + " rows");

            r.Id = 0;   // a second, separate request (Save updates when the object already has an id)
            int other = BreqsService.Save(r, uid);
            Check("BREQS refuses an O.R. already used by another request, and stays Requested",
                  Throws(() => BreqsService.RecordPayment(other, Or("b1"), Jan2099.Date, 150m, uid), "recorded once") &&
                  BreqsService.Load(other).Status == BreqsService.Requested);
        }

        // ------------------------------------------------------------ monthly collection report
        private static void Monthly()
        {
            // A payment from before fee lines existed: counted, reported as not itemised, never assigned a fee.
            Db.Push("INSERT INTO payments (payer_name, source, or_number, payment_method, gross_amount, additional_fee, net_amount, paid_at) " +
                    "VALUES ('" + Tag + "Legacy', 'Transaction', '" + Or("legacy") + "', 'Cash', 55, 0, 55, '2099-01-20 09:00:00')");
            PaymentService.MonthlyCollection m = PaymentService.Monthly(2099, 1);
            decimal byFee = m.ByFee.AsEnumerable().Sum(x => Convert.ToDecimal(x["Amount"]));
            decimal bySource = m.BySource.AsEnumerable().Sum(x => Convert.ToDecimal(x["Amount"]));
            decimal byMethod = m.ByMethod.AsEnumerable().Sum(x => Convert.ToDecimal(x["Amount"]));
            Check("January 2099: 4 receipts, PHP 535 (280 + 50 + 150 + 55)", m.Payments == 4 && m.Total == 535m, m.Payments + " / " + m.Total);
            Check("by-fee rows add up to the total (fees + additional + not itemised)", byFee == m.Total, byFee + " vs " + m.Total);
            Check("by-source and by-method add up to the total", bySource == m.Total && byMethod == m.Total, bySource + " / " + byMethod);
            Check("legacy payment reported as not itemised (55)", m.Unitemised == 55m &&
                  m.ByFee.AsEnumerable().Any(x => x["Fee"].ToString().StartsWith("Payments not itemised")));
            Check("additional charges are their own row (20)", m.Additional == 20m && m.ByFee.AsEnumerable().Any(x => x["Fee"].ToString() == "Additional charges"));
            DataRow ctc = m.ByFee.AsEnumerable().FirstOrDefault(x => x["Code"].ToString() == "CTC-BIRTH");
            var breqsRows = m.ByFee.AsEnumerable().Where(x => x["Code"].ToString() == "BREQS").ToList();
            Check("BREQS is ONE row although its lines were worded differently: qty 4, PHP 200, named from the schedule",
                  breqsRows.Count == 1 && Convert.ToInt32(breqsRows[0]["Quantity"]) == 4 && Convert.ToDecimal(breqsRows[0]["Amount"]) == 200m &&
                  breqsRows[0]["Fee"].ToString() == PaymentService.Fee("BREQS").Description, breqsRows.Count + " rows");
            Check("CTC-BIRTH: quantity 2, PHP 160", ctc != null && Convert.ToInt32(ctc["Quantity"]) == 2 && Convert.ToDecimal(ctc["Amount"]) == 160m);
            Check("real months untouched by the test (Jan 2099 only)", PaymentService.Monthly(DateTime.Today.Year, DateTime.Today.Month).Detail.AsEnumerable()
                  .All(x => !x["O.R. No."].ToString().StartsWith(Tag)));
        }

        // ------------------------------------------------------------ an audited fee change
        private static void FeeChange(int uid, FeeItem original)
        {
            int before = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM audit_log WHERE table_name='fees'").Rows[0][0]);
            _auditFloor = Convert.ToInt32(Db.Pull("SELECT COALESCE(MAX(id), 0) FROM audit_log").Rows[0][0]);
            PaymentService.UpdateFee("ANNOTATION", 125m, true, uid);
            Check("fee changed to 125", PaymentService.Fee("ANNOTATION").Amount == 125m);
            DataRow au = Db.Pull("SELECT details FROM audit_log WHERE table_name='fees' ORDER BY id DESC LIMIT 1").Rows[0];
            Check("fee change audited with old and new amount", au["details"].ToString().Contains("100.00") && au["details"].ToString().Contains("125.00"), au["details"].ToString());
            PaymentService.UpdateFee("ANNOTATION", 125m, true, uid);
            Check("an unchanged save writes no audit row",
                  Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM audit_log WHERE table_name='fees'").Rows[0][0]) == before + 1);
            Check("negative fee refused", Throws(() => PaymentService.UpdateFee("ANNOTATION", -1m, true, uid), "negative"));
            PaymentService.UpdateFee("ANNOTATION", original.Amount, original.Active, uid);
            Check("fee restored", PaymentService.Fee("ANNOTATION").Amount == original.Amount);
        }

        // ------------------------------------------------------------ the screen
        private static void Render(string dir)
        {
            Application.EnableVisualStyles();
            Type t = typeof(PaymentService).Assembly.GetType("CROMS.Forms.FeesPaymentsForm", true);
            const BindingFlags nf = BindingFlags.Instance | BindingFlags.NonPublic;
            for (int tab = 0; tab < 5; tab++)
            {
                int which = tab;
                var f = (Form)Activator.CreateInstance(t, true);
                Snap(f, 1500, 960, Path.Combine(dir, "fees_tab" + which + ".png"), x =>
                {
                    var tabs = (TabControl)t.GetField("_tabs", nf).GetValue(x);
                    if (which == 1)
                    {
                        var items = (List<PaymentLine>)t.GetField("_wItems", nf).GetValue(x);
                        items.Add(L("CTC-BIRTH", 80, 2)); items.Add(L("ANNOTATION", 100));
                        ((TextBox)t.GetField("_wPayer", nf).GetValue(x)).Text = "Juan Dela Cruz";
                        ((ComboBox)t.GetField("_wPurpose", nf).GetValue(x)).Text = "Certified copy";
                    }
                    if (which == 2)
                    {
                        ((DateTimePicker)t.GetField("_lFrom", nf).GetValue(x)).Value = Jan2099.Date;
                        ((DateTimePicker)t.GetField("_lTo", nf).GetValue(x)).Value = new DateTime(2099, 1, 31);
                    }
                    if (which == 3)
                    {
                        var year = (ComboBox)t.GetField("_mYear", nf).GetValue(x);
                        year.Items.Insert(0, "2099"); year.SelectedIndex = 0;
                        ((ComboBox)t.GetField("_mMonth", nf).GetValue(x)).SelectedIndex = 0;
                    }
                    tabs.SelectedIndex = which;
                    if (which == 1) t.GetMethod("BindLines", nf).Invoke(x, null);
                    if (which == 2) t.GetMethod("LoadLog", nf).Invoke(x, null);
                    if (which == 3)
                    {
                        t.GetMethod("LoadMonthly", nf).Invoke(x, null);
                        string sum = ((Label)t.GetField("_mSummary", nf).GetValue(x)).Text;
                        Check("monthly tab shows January 2099, 4 receipts, PHP 535.00", sum.Contains("January 2099") && sum.Contains("4 receipts") && sum.Contains("535.00"), sum);
                    }
                });
            }
        }

        private static void Snap(Form f, int w, int h, string file, Action<Form> prep)
        {
            try
            {
                f.FormBorderStyle = FormBorderStyle.Sizable;
                f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-4000, -4000); f.ShowInTaskbar = false;
                f.Show();
                f.Size = new Size(w + (f.Width - f.ClientSize.Width), h + (f.Height - f.ClientSize.Height));
                for (int i = 0; i < 8; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }
                if (prep != null) prep(f);
                for (int i = 0; i < 12; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }
                using (var bmp = new Bitmap(f.Width, f.Height))
                {
                    f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                    bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                }
                Console.WriteLine("  ok  " + Path.GetFileName(file));
                f.Close(); f.Dispose();
            }
            catch (Exception ex) { Console.WriteLine("  FAIL render " + Path.GetFileName(file) + ": " + (ex.InnerException ?? ex).Message); Fail++; }
        }

        private static int _auditFloor = int.MaxValue;

        private static void Cleanup()
        {
            // The test's own fee-change audit rows (the fee itself is restored in Run's finally).
            Db.Push("DELETE FROM audit_log WHERE table_name = 'fees' AND id > " + _auditFloor);
            DataTable br = Db.Pull("SELECT id FROM breqs_requests WHERE requester_last LIKE 'ZZF%'");
            string bl = string.Join(",", br.AsEnumerable().Select(r => r[0].ToString()).DefaultIfEmpty("0"));
            // matched on the O.R. in the details, not the bare id: payment ids are reused and older audit rows can carry one
            Db.Push("DELETE a FROM audit_log a JOIN payments p ON a.table_name = 'payments' AND a.record_id = CAST(p.id AS CHAR) AND a.details LIKE CONCAT('%O.R. ', p.or_number, '%') WHERE p.or_number LIKE 'ZZF%' OR p.payer_name LIKE 'ZZF%' OR (p.source_table = 'breqs_requests' AND p.source_id IN (" + bl + "))");
            Db.Push("DELETE FROM payments WHERE or_number LIKE 'ZZF%' OR payer_name LIKE 'ZZF%' OR (source_table = 'breqs_requests' AND source_id IN (" + bl + "))");
            Db.Push("DELETE FROM audit_log WHERE table_name = 'breqs_requests' AND record_id IN (" + bl + ")");
            Db.Push("DELETE FROM breqs_requests WHERE id IN (" + bl + ")");
        }
    }
}
