using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.MarriageTest
{
    /// <summary>
    /// BREQS end to end against the LIVE database: the pure rules, a counter request through every
    /// status (payment, submission, a REAL scan through the OCR engine, release), the two exits, the
    /// kiosk's own save path, and the screens rendered. Test rows are surnamed ZZB... and deleted.
    /// </summary>
    internal static class BreqsTest
    {
        public static int Pass, Fail;
        private const string Tag = "ZZB";

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

        public static void Run(string dir, int userId)
        {
            Directory.CreateDirectory(dir);
            Cleanup();
            try
            {
                Rules();
                Lifecycle(dir, userId);
                Exits(userId);
                Kiosk();
                Render(dir, userId);
            }
            finally
            {
                Cleanup();
                int left = Convert.ToInt32(Db.Pull("SELECT (SELECT COUNT(*) FROM breqs_requests WHERE requester_last LIKE 'ZZB%' OR owner_last LIKE 'ZZB%') + " +
                                                   "(SELECT COUNT(*) FROM queue_tickets WHERE ticket_code LIKE 'ZZB%')").Rows[0][0]);
                Check("zero strays after cleanup", left == 0, left + " left");
            }
        }

        // ------------------------------------------------------------ pure rules
        private static void Rules()
        {
            Check("Requested -> Paid allowed", BreqsService.CanMove(BreqsService.Requested, BreqsService.Paid));
            Check("Requested -> Released refused (not paid, not collected)", !BreqsService.CanMove(BreqsService.Requested, BreqsService.Released));
            Check("Paid -> Received refused (never submitted)", !BreqsService.CanMove(BreqsService.Paid, BreqsService.Received));
            Check("Released is final", !BreqsService.Statuses.Any(s => BreqsService.CanMove(BreqsService.Released, s)));
            Check("Submitted -> No Record allowed", BreqsService.CanMove(BreqsService.Submitted, BreqsService.NoRecord));

            var s7 = new BreqsSettings { TurnaroundDays = 7, UnclaimedDays = 30 };
            var r = new BreqsRequest { Status = BreqsService.Submitted, ExpectedDate = DateTime.Today };
            Check("expected today is not overdue yet", !BreqsService.IsOverdue(r, DateTime.Today));
            Check("a day past expected is overdue", BreqsService.IsOverdue(r, DateTime.Today.AddDays(1)));
            Check("overdue shows as 'Overdue at PSA'", BreqsService.DisplayStatus(r, DateTime.Today.AddDays(1), s7) == "Overdue at PSA");
            var rec = new BreqsRequest { Status = BreqsService.Received, ReceivedAt = DateTime.Today.AddDays(-31) };
            Check("received 31 days ago is unclaimed", BreqsService.IsUnclaimed(rec, DateTime.Today, s7));
            Check("received shows as 'Ready for release'", BreqsService.DisplayStatus(new BreqsRequest { Status = BreqsService.Received, ReceivedAt = DateTime.Today }, DateTime.Today, s7) == "Ready for release");

            var owner = new BreqsRequest { OwnerFirst = "Juan", OwnerLast = "Dela Cruz" };
            Check("name match ignores case and spacing", BreqsService.CompareName(owner, "JUAN", "DELACRUZ") == "Match");
            Check("same surname, other first name = Partial", BreqsService.CompareName(owner, "Pedro", "DELA CRUZ") == "Partial");
            Check("different person = Mismatch", BreqsService.CompareName(owner, "Maria", "Santos") == "Mismatch");
            Check("nothing read = Unread", BreqsService.CompareName(owner, null, "") == "Unread");

            List<string> v = BreqsService.Validate(new BreqsRequest { DocType = BreqsService.Marriage });
            Check("validation names what is missing (requester, ID, husband, wife)",
                  v.Any(x => x.Contains("requester")) && v.Any(x => x.Contains("ID number")) && v.Any(x => x.Contains("husband")) && v.Any(x => x.Contains("wife")),
                  string.Join(" | ", v));
        }

        private static BreqsRequest NewBirth()
        {
            return new BreqsRequest
            {
                Source = BreqsService.SourceCounter, RequesterFirst = "Sheila", RequesterMiddle = "Articulo", RequesterLast = Tag + "Talosig",
                ContactNo = "09171234567", Relationship = "Parent", ValidIdType = "Philippine National ID (PhilSys)", ValidIdNo = "1234-5678-9012",
                DocType = BreqsService.Birth, Copies = 2, Purpose = "Passport / DFA",
                // The office's real sample scan (Downloads\Birth Certificate.jpeg) is of this child.
                OwnerFirst = "Shellian Clear", OwnerMiddle = "Baloso", OwnerLast = "Talosig",
                EventDate = new DateTime(2018, 6, 12), EventCity = "Tuguegarao City", EventProvince = "Cagayan",
                FatherName = "Gilbert Cataggatan Talosig", MotherMaidenName = "Sheila Articulo Baloso"
            };
        }

        // ------------------------------------------------------------ full lifecycle
        private static void Lifecycle(string dir, int uid)
        {
            BreqsRequest r = NewBirth();
            int id = BreqsService.Save(r, uid);
            BreqsRequest back = BreqsService.Load(id);
            Check("saved with a BREQS-" + DateTime.Today.Year + "-#### number", back.RequestNo != null && back.RequestNo.StartsWith("BREQS-" + DateTime.Today.Year + "-"), back.RequestNo);
            Check("starts as Requested, fee = 50 x 2 copies", back.Status == BreqsService.Requested && back.FeeAmount == 100m, back.Status + " " + back.FeeAmount);
            Check("birth keeps parents, has no spouse", back.FatherName == "Gilbert Cataggatan Talosig" && back.SpouseFirst == null);

            back.Purpose = "School / Enrollment";
            BreqsService.Save(back, uid);
            Check("details editable while Requested", BreqsService.Load(id).Purpose == "School / Enrollment");

            Check("cannot release an unpaid request", Throws(() => BreqsService.Release(id, "X", "Y", "Z", false, uid), "cannot be marked"));
            BreqsService.RecordPayment(id, "OR-0045123", DateTime.Today, 100m, uid);
            back = BreqsService.Load(id);
            Check("payment recorded -> Paid", back.Status == BreqsService.Paid && back.OrNo == "OR-0045123");

            BreqsService.SubmitToPsa(id, "BRQ-PSA-77812", DateTime.Today.AddDays(-2), uid);
            back = BreqsService.Load(id);
            Check("submitted -> expected = submitted + 7 days", back.Status == BreqsService.Submitted && back.ExpectedDate == DateTime.Today.AddDays(5),
                  MarriageRules.D(back.ExpectedDate));
            Check("details locked once sent to PSA", Throws(() => { back.Purpose = "Travel / Visa"; BreqsService.Save(back, uid); }, "can no longer be edited"));

            // ---- REAL scan through the REAL OCR engine.
            string scan = @"C:\Users\ivan palogan\Downloads\Birth Certificate.jpeg";
            if (File.Exists(scan) && OcrService.IsAvailable())
            {
                Bitmap bmp = DocumentAI.LoadImage(scan);
                DocAiResult ocr = DocumentAI.Analyze(bmp);
                string first, last; BreqsService.OcrOwner(BreqsService.Birth, ocr, out first, out last);
                Console.WriteLine("  info  OCR on the real scan: " + ocr.Kind + " " + ocr.ClassifyConfidence + "% class, " + ocr.OcrConfidence + "% recognition, name read '" + first + " / " + last + "'");
                BreqsService.ReceiveFromPsa(id, File.ReadAllBytes(scan), ocr, "SECPA-A1234567", uid);
                back = BreqsService.Load(id);
                Check("received -> scan stored, OCR kind + match recorded", back.Status == BreqsService.Received && back.HasScan && back.OcrDocKind == "Birth",
                      back.OcrDocKind + " / " + back.OcrMatch + " / " + back.OcrName);
                Check("the real scan names the requested child (Match)", back.OcrMatch == "Match", back.OcrMatch + " (" + back.OcrName + ")");
                DataTable batch = Db.Pull("SELECT doc_kind, status FROM ocr_batch WHERE record_table = 'breqs_requests' AND record_id = @id", new MySqlParameter("@id", id));
                Check("scan logged to ocr_batch against the request", batch.Rows.Count == 1 && (string)batch.Rows[0]["status"] == "Attached");

                // Same scan attached to a DEATH request must be caught as the wrong document.
                BreqsRequest wrong = NewBirth(); wrong.DocType = BreqsService.Death; wrong.FatherName = wrong.MotherMaidenName = null;
                int wid = BreqsService.Save(wrong, uid);
                BreqsService.RecordPayment(wid, "OR-9", DateTime.Today, 50m, uid);
                BreqsService.SubmitToPsa(wid, null, DateTime.Today, uid);
                BreqsService.ReceiveFromPsa(wid, File.ReadAllBytes(scan), ocr, null, uid);
                Check("a birth copy on a death request is flagged 'Wrong document'", BreqsService.Load(wid).OcrMatch == "Wrong document", BreqsService.Load(wid).OcrMatch);
            }
            else
            {
                BreqsService.ReceiveFromPsa(id, new byte[] { 1, 2, 3 }, null, null, uid);
                Check("received without OCR (engine unavailable) -> Unread", BreqsService.Load(id).OcrMatch == "Unread");
            }

            Check("cannot receive twice", Throws(() => BreqsService.ReceiveFromPsa(id, new byte[] { 1 }, null, null, uid), "cannot be marked"));
            Check("release needs the claimant's ID", Throws(() => BreqsService.Release(id, "Sheila Talosig", "", "", false, uid), "valid ID"));
            BreqsService.Release(id, "Sheila Articulo Talosig", "Philippine National ID (PhilSys)", "1234-5678-9012", false, uid);
            back = BreqsService.Load(id);
            Check("released -> closed, claimant recorded", back.Status == BreqsService.Released && back.ReleasedAt.HasValue && back.ClaimantName == "Sheila Articulo Talosig");
            int hist = BreqsService.History(id).Rows.Count;
            Check("history kept for every step (logged, edit, paid, submitted, received, released)", hist >= 6, hist + " rows");
            Check("audit trail written", Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM audit_log WHERE table_name = 'breqs_requests' AND record_id = @id", new MySqlParameter("@id", id)).Rows[0][0]) >= 5);
        }

        private static void Exits(int uid)
        {
            BreqsRequest m = NewBirth();
            m.DocType = BreqsService.Marriage; m.OwnerFirst = "Ryan"; m.OwnerLast = Tag + "Macanang"; m.SpouseFirst = "Tofie Fae"; m.SpouseLast = "Quilang";
            int id = BreqsService.Save(m, uid);
            BreqsRequest back = BreqsService.Load(id);
            Check("marriage request keeps the wife, drops the parents", back.SpouseFirst == "Tofie Fae" && back.FatherName == null);
            BreqsService.RecordPayment(id, "OR-1", DateTime.Today, 50m, uid);
            BreqsService.SubmitToPsa(id, "REF-1", DateTime.Today, uid);
            Check("no-record needs a reason", Throws(() => BreqsService.MarkNoRecord(id, " ", uid), "State what PSA returned"));
            BreqsService.MarkNoRecord(id, "PSA: no record found - negative certification issued", uid);
            Check("no record at PSA -> closed", BreqsService.Load(id).Status == BreqsService.NoRecord);

            int cid = BreqsService.Save(NewBirth(), uid);
            BreqsService.Cancel(cid, "Client withdrew the request", uid);
            Check("cancelled -> closed, reason kept", BreqsService.Load(cid).Status == BreqsService.Cancelled && BreqsService.Load(cid).OutcomeReason.StartsWith("Client"));
            Check("open list excludes closed requests", !BreqsService.List(Tag, false).Any(x => x.Id == cid) && BreqsService.List(Tag, true).Any(x => x.Id == cid));
        }

        // ------------------------------------------------------------ kiosk save path
        private static void Kiosk()
        {
            string exe = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\CROMS.Kiosk\bin\Debug\CROMS.Kiosk.exe"));
            Check("kiosk build found", File.Exists(exe), exe);
            if (!File.Exists(exe)) return;
            Assembly k = Assembly.LoadFrom(exe);
            Type sessionT = k.GetType("CROMS.Kiosk.KioskSession", true), core = k.GetType("CROMS.Kiosk.KioskCore", true);
            object s = Activator.CreateInstance(sessionT);
            Action<string, object> set = (n, v) => sessionT.GetField(n).SetValue(s, v);
            ((List<string>)sessionT.GetField("Selected").GetValue(s)).Add("BREQS");
            set("First", "Maria"); set("Last", Tag + "Kiosk"); set("Contact", "09998887777");
            var problem = (string)core.GetMethod("BreqsProblem").Invoke(null, new[] { s });
            Check("kiosk refuses an incomplete PSA request", problem != null && problem.Contains("certificate"), problem);

            set("BreqsDocType", "Death"); set("IdType", "Driver's License (LTO)"); set("IdNo", "N01-23-456789");
            set("OwnerFirst", "Pedro"); set("OwnerLast", Tag + "Quilang"); set("BreqsCopies", 1); set("EventDate", (DateTime?)new DateTime(2020, 1, 5));
            set("FatherName", "should not be kept on a death request");
            Check("complete request passes the kiosk check", core.GetMethod("BreqsProblem").Invoke(null, new[] { s }) == null);

            long ticket = Db.Insert("INSERT INTO queue_tickets (ticket_code, full_name, number_queue, date, time, status, document_type, type_label, priority) " +
                                    "VALUES ('ZZB-001', 'Maria ZZBKiosk', 99998, CURDATE(), CURTIME(), 'Waiting', 'PSA Copy (BREQS)', 'PSA Copy (BREQS)', 'Regular')");
            string no = (string)core.GetMethod("SaveBreqsRequest").Invoke(null, new object[] { s, ticket, "ZZB-001" });
            BreqsRequest kr = BreqsService.LoadByTicket((int)ticket);
            Check("kiosk request saved, linked to its ticket, source Kiosk", kr != null && kr.RequestNo == no && kr.Source == "Kiosk" && kr.Status == BreqsService.Requested, no);
            Check("kiosk keeps the death details and drops the father", kr != null && kr.DocType == "Death" && kr.EventDate == new DateTime(2020, 1, 5) && kr.FatherName == null);
            Check("staff desk can open the kiosk request by ticket", BreqsService.LoadByTicket((int)ticket).Id == kr.Id);
        }

        // ------------------------------------------------------------ screens
        private static void Render(string dir, int uid)
        {
            // A desk at every stage.
            int a = BreqsService.Save(NewBirth(), uid);
            BreqsRequest pd = NewBirth(); pd.DocType = BreqsService.Marriage; pd.OwnerFirst = "Ryan"; pd.OwnerLast = Tag + "Macanang"; pd.SpouseFirst = "Tofie Fae"; pd.SpouseLast = "Quilang"; pd.FatherName = pd.MotherMaidenName = null;
            int b = BreqsService.Save(pd, uid); BreqsService.RecordPayment(b, "OR-2", DateTime.Today, 50m, uid);
            BreqsRequest od = NewBirth(); od.DocType = BreqsService.Death; od.OwnerFirst = "Pedro"; od.OwnerLast = Tag + "Quilang"; od.FatherName = od.MotherMaidenName = null;
            int c = BreqsService.Save(od, uid); BreqsService.RecordPayment(c, "OR-3", DateTime.Today, 50m, uid); BreqsService.SubmitToPsa(c, "REF-9", DateTime.Today.AddDays(-10), uid);
            int d = BreqsService.Save(NewBirth(), uid); BreqsService.RecordPayment(d, "OR-4", DateTime.Today, 100m, uid); BreqsService.SubmitToPsa(d, "REF-10", DateTime.Today.AddDays(-3), uid);
            string scan = @"C:\Users\ivan palogan\Downloads\Birth Certificate.jpeg";
            if (File.Exists(scan)) BreqsService.ReceiveFromPsa(d, File.ReadAllBytes(scan), null, null, uid);

            Application.EnableVisualStyles();
            var asm = typeof(BreqsService).Assembly;
            var form = (Form)Activator.CreateInstance(asm.GetType("CROMS.Forms.BreqsForm", true), true);
            Snap(form, 1500, 960, Path.Combine(dir, "breqs_desk.png"), f =>
            {
                var t = f.GetType();
                var search = (TextBox)t.GetField("_search", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f);
                search.Text = Tag;
                for (int i = 0; i < 12; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(40); }
                t.GetMethod("ShowDetail", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f, new object[] { c });
            });
            var form2 = (Form)Activator.CreateInstance(asm.GetType("CROMS.Forms.BreqsForm", true), true);
            Snap(form2, 1500, 960, Path.Combine(dir, "breqs_desk_ready.png"), f =>
                f.GetType().GetMethod("ShowDetail", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f, new object[] { d }));

            var dlgT = asm.GetType("CROMS.Forms.BreqsRequestDialog", true);
            Snap((Form)Activator.CreateInstance(dlgT, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                     new object[] { BreqsService.Load(b), BreqsService.Settings }, null), 780, 820, Path.Combine(dir, "breqs_dialog_marriage.png"), null);
            Snap((Form)Activator.CreateInstance(dlgT, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                     new object[] { BreqsService.Load(a), BreqsService.Settings }, null), 780, 820, Path.Combine(dir, "breqs_dialog_birth.png"), null);

            // Kiosk step, Marriage selected.
            string exe = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\CROMS.Kiosk\bin\Debug\CROMS.Kiosk.exe"));
            if (File.Exists(exe))
            {
                Assembly k = Assembly.LoadFrom(exe);
                object s = Activator.CreateInstance(k.GetType("CROMS.Kiosk.KioskSession", true));
                var st = s.GetType();
                ((List<string>)st.GetField("Selected").GetValue(s)).Add("BREQS");
                st.GetField("BreqsDocType").SetValue(s, "Marriage");
                st.GetField("OwnerFirst").SetValue(s, "Ryan"); st.GetField("OwnerLast").SetValue(s, "Macanang");
                foreach (var size in new[] { new Size(1920, 1040), new Size(1366, 728) })
                {
                    var kf = (Form)Activator.CreateInstance(k.GetType("CROMS.Kiosk.BreqsDetailsForm", true), s);
                    SnapKiosk(kf, size, Path.Combine(dir, "kiosk_breqs_marriage_" + size.Width + ".png"));
                }
                object s2 = Activator.CreateInstance(k.GetType("CROMS.Kiosk.KioskSession", true));
                ((List<string>)st.GetField("Selected").GetValue(s2)).Add("BREQS");
                st.GetField("BreqsDocType").SetValue(s2, "Birth");
                SnapKiosk((Form)Activator.CreateInstance(k.GetType("CROMS.Kiosk.BreqsDetailsForm", true), s2), new Size(1920, 1040),
                          Path.Combine(dir, "kiosk_breqs_birth_1920.png"));
            }
        }

        /// <summary>A kiosk step sized BEFORE it is shown, as the maximized kiosk is - its fit-to-screen runs once, on Shown.</summary>
        private static void SnapKiosk(Form f, Size client, string file)
        {
            try
            {
                f.WindowState = FormWindowState.Normal; f.StartPosition = FormStartPosition.Manual;
                f.Location = new Point(-4000, -4000); f.ShowInTaskbar = false;
                f.ClientSize = client;
                f.Show();
                for (int i = 0; i < 10; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }
                using (var bmp = new Bitmap(f.Width, f.Height))
                {
                    f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                    bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                }
                Console.WriteLine("  ok  " + Path.GetFileName(file));
                f.GetType().GetField("_navigating", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(f, true);
                f.Close(); f.Dispose();
            }
            catch (Exception ex) { Console.WriteLine("  FAIL render " + Path.GetFileName(file) + ": " + (ex.InnerException ?? ex).Message); Fail++; }
        }

        private static void Snap(Form f, int w, int h, string file, Action<Form> prep)
        {
            try
            {
                f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-4000, -4000); f.ShowInTaskbar = false;
                f.Show();
                f.Size = new Size(w + (f.Width - f.ClientSize.Width), h + (f.Height - f.ClientSize.Height));
                for (int i = 0; i < 8; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }
                if (prep != null) prep(f);
                for (int i = 0; i < 8; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }
                using (var bmp = new Bitmap(f.Width, f.Height))
                {
                    f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                    bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                }
                Console.WriteLine("  ok  " + Path.GetFileName(file));
                // Kiosk forms end the process on a user close they did not initiate - mark it deliberate.
                var nav = f.GetType().GetField("_navigating", BindingFlags.Instance | BindingFlags.NonPublic);
                if (nav != null) nav.SetValue(f, true);
                f.Close(); f.Dispose();
            }
            catch (Exception ex) { Console.WriteLine("  FAIL render " + Path.GetFileName(file) + ": " + (ex.InnerException ?? ex).Message); Fail++; }
        }

        private static void Cleanup()
        {
            DataTable ids = Db.Pull("SELECT id FROM breqs_requests WHERE requester_last LIKE 'ZZB%' OR owner_last LIKE 'ZZB%'");
            string list = string.Join(",", ids.AsEnumerable().Select(r => r[0].ToString()).DefaultIfEmpty("0"));
            Db.Push("DELETE FROM ocr_batch WHERE record_table = 'breqs_requests' AND record_id IN (" + list + ")");
            Db.Push("DELETE FROM audit_log WHERE table_name = 'breqs_requests' AND record_id IN (" + list + ")");
            Db.Push("DELETE FROM breqs_requests WHERE id IN (" + list + ")");   // history cascades
            Db.Push("DELETE FROM queue_tickets WHERE ticket_code LIKE 'ZZB%'");
        }
    }
}
