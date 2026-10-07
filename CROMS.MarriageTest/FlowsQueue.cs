using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.MarriageTest
{
    /// <summary>
    /// Answers the dialogs a real operator would answer, so queue / payment / release handlers can
    /// run unattended. SAFETY RULE: a print dialog is never confirmed - it is closed (cancelled),
    /// so nothing can reach a physical printer. Message boxes get Yes, else OK. Known modal forms
    /// are driven by a rule; any other form that appears is logged and closed.
    /// </summary>
    internal sealed class DialogWatchdog
    {
        private delegate bool EnumProc(IntPtr h, IntPtr l);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc p, IntPtr l);
        [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr h, EnumProc p, IntPtr l);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr h, StringBuilder sb, int n);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr h, StringBuilder sb, int n);
        [DllImport("user32.dll")] private static extern int GetDlgCtrlID(IntPtr h);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
        private const int BM_CLICK = 0xF5, WM_CLOSE = 0x10;

        public readonly List<string> Log = new List<string>();
        public readonly HashSet<IntPtr> Ignore = new HashSet<IntPtr>();
        public readonly Dictionary<string, Action<Form>> Rules = new Dictionary<string, Action<Form>>();

        private readonly HashSet<IntPtr> _handled = new HashSet<IntPtr>();
        private readonly Dictionary<IntPtr, long> _lastAct = new Dictionary<IntPtr, long>();
        private readonly uint _pid = (uint)Process.GetCurrentProcess().Id;
        private Thread _t;
        private volatile bool _stop;

        public void Start()
        {
            _t = new Thread(Loop) { IsBackground = true, Name = "dialog-watchdog" };
            _t.Start();
        }
        public void Stop() { _stop = true; if (_t != null) _t.Join(2000); }

        public int Count { get { lock (Log) return Log.Count; } }
        public List<string> Since(int index) { lock (Log) return Log.Skip(index).ToList(); }
        public bool Saw(int since, string titleContains)
        {
            return Since(since).Any(l => l.IndexOf(titleContains, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        private void Add(string s) { lock (Log) Log.Add(s); }

        private void Loop()
        {
            while (!_stop)
            {
                try { EnumWindows((h, l) => { Sweep(h); return true; }, IntPtr.Zero); } catch { }
                Thread.Sleep(60);
            }
        }

        private static string ClassOf(IntPtr h) { var sb = new StringBuilder(128); GetClassName(h, sb, sb.Capacity); return sb.ToString(); }
        private static string TextOf(IntPtr h) { var sb = new StringBuilder(1024); GetWindowText(h, sb, sb.Capacity); return sb.ToString(); }

        private void Sweep(IntPtr h)
        {
            if (!IsWindowVisible(h)) return;
            uint pid; GetWindowThreadProcessId(h, out pid);
            if (pid != _pid || Ignore.Contains(h)) return;

            if (ClassOf(h) == "#32770") { DialogBox(h); return; }

            Form f = Control.FromHandle(h) as Form;
            // The shell under test is never touched: closing it would dispose every hosted module.
            if (f == null || f.GetType().Name == "MainForm" || !_handled.Add(h)) return;
            Action<Form> rule;
            if (Rules.TryGetValue(f.GetType().Name, out rule))
            {
                Add("FORM|" + f.GetType().Name + "|" + f.Text);
                f.BeginInvoke(new Action(() => rule(f)));
            }
            else
            {
                Add("UNEXPECTED|" + f.GetType().Name + "|" + f.Text);
                f.BeginInvoke(new Action(f.Close));
            }
        }

        private void DialogBox(IntPtr h)
        {
            long now = Environment.TickCount;
            long last;
            if (_lastAct.TryGetValue(h, out last) && now - last < 350) return;
            _lastAct[h] = now;

            var kids = new List<Tuple<string, int, string, IntPtr>>();
            EnumChildWindows(h, (c, l) => { kids.Add(Tuple.Create(ClassOf(c), GetDlgCtrlID(c), TextOf(c), c)); return true; }, IntPtr.Zero);
            string title = TextOf(h);
            string body = string.Join(" ", kids.Where(k => k.Item1 == "Static" && k.Item3.Length > 0).Select(k => k.Item3)).Replace("\r\n", " ").Replace("\n", " ");

            bool common = kids.Any(k => k.Item1 == "ComboBox" || k.Item1 == "ComboBoxEx32" || k.Item1 == "SysTabControl32" ||
                                        k.Item1 == "SysListView32" || k.Item1 == "ListBox");
            bool printBtn = kids.Any(k => k.Item1 == "Button" && k.Item3.Replace("&", "").Trim().Equals("Print", StringComparison.OrdinalIgnoreCase));
            if (common || printBtn)
            {
                Add("COMMON|" + title + "|cancelled, never confirmed");
                PostMessage(h, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                return;
            }

            foreach (int id in new[] { 6, 1, 4, 2, 7 })   // Yes, OK, Retry, Cancel, No
            {
                var b = kids.FirstOrDefault(k => k.Item1 == "Button" && k.Item2 == id);
                if (b == null) continue;
                Add("MB|" + title + "|" + body + "|clicked " + b.Item3.Replace("&", ""));
                SendMessage(b.Item4, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                return;
            }
            Add("MB?|" + title + "|" + body + "|no known button, closed");
            PostMessage(h, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }
    }

    internal static partial class FlowsTest
    {
        private static DataRow QRow(int id)
        {
            return Db.Pull("SELECT * FROM queue_tickets WHERE id=@i", new MySqlParameter("@i", id)).Rows[0];
        }
        private static string S(object v) { return v == null || v == DBNull.Value ? null : v.ToString(); }
        private static void SetFld(object o, string name, object value)
        {
            for (Type t = o.GetType(); t != null; t = t.BaseType)
            {
                System.Reflection.FieldInfo f = t.GetField(name, Any | System.Reflection.BindingFlags.DeclaredOnly);
                if (f != null) { f.SetValue(o, value); return; }
            }
            throw new MissingFieldException(o.GetType().Name, name);
        }

        private static int MakeTicket(out string code, Action<object> tweak, params string[] svcs)
        {
            object s = NewSession(); Pick(s, svcs);
            if (svcs.Contains("CTC")) FillCtc(s, "Birth");
            if (tweak != null) tweak(s);
            string err;
            if (!Submit(s, out err)) throw new Exception("kiosk refused the ticket: " + err);
            DataRow r = Db.Pull("SELECT id, ticket_code FROM queue_tickets WHERE full_name LIKE @n ORDER BY id DESC LIMIT 1",
                                new MySqlParameter("@n", "%" + Tag + "%")).Rows[0];
            code = r["ticket_code"].ToString();
            return Convert.ToInt32(r["id"]);
        }

        private static void QueuePart()
        {
            Console.WriteLine("-- queue + payment: real forms in a real shell, dialogs auto-answered (print dialogs are cancelled) --");
            // The kiosk/staff parts above left ~20 tagged tickets Waiting; Call Next would take those
            // before the three made here. Remove them first.
            Cleanup();
            int waitingReal = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM queue_tickets WHERE DATE(created_at)=CURDATE() AND status IN ('Waiting','For Receiving') AND full_name NOT LIKE '%ZZF%'").Rows[0][0]);
            Check("no real client is waiting today (the test would otherwise call them)", waitingReal == 0, waitingReal + " waiting");
            if (waitingReal != 0) return;

            var wd = new DialogWatchdog();
            wd.Rules["CertificatePrintForm"] = f => { var pay = (Button)Fld(f, "_btnPay"); pay.Enabled = true; pay.PerformClick(); };
            wd.Rules["ReleaseVerifyDialog"] = f => { f.DialogResult = DialogResult.OK; };
            wd.Rules["AbandonReasonDialog"] = f => { ((ComboBox)Fld(f, "_reason")).Text = "ZZF test - client left"; f.DialogResult = DialogResult.OK; };

            Form shell = null;
            long wa = 0, wb = 0;
            try
            {
                int admin = Session.User.Id;
                string ins = "INSERT INTO windows (window_name, description, status, is_priority, current_operator, operator_name, last_heartbeat, display_order) " +
                             "VALUES (@n, 'ZZF test window', 'Active', 0, @u, 'ZZF tester', NOW(), @o)";
                wa = Db.Insert(ins, new MySqlParameter("@n", "ZZF Win A"), new MySqlParameter("@u", admin), new MySqlParameter("@o", 900));
                wb = Db.Insert(ins, new MySqlParameter("@n", "ZZF Win B"), new MySqlParameter("@u", admin), new MySqlParameter("@o", 901));
                Db.Push("INSERT INTO window_transactions (window_id, service_code) VALUES (@w, 'CTC')", new MySqlParameter("@w", wb));
                Session.WindowId = (int)wa; Session.WindowName = "ZZF Win A";

                wd.Start();
                Type mt = typeof(Db).Assembly.GetType("CROMS.MainForm", true);
                shell = (Form)Activator.CreateInstance(mt, true);
                shell.Opacity = 0; shell.ShowInTaskbar = false; shell.Show();
                wd.Ignore.Add(shell.Handle);
                Application.DoEvents();
                Func<string, Form> go = k => (Form)shell.GetType().GetMethod("GoToModule").Invoke(shell, new object[] { k });

                // ---- tickets from the real kiosk: created in this order
                string cReg, cMulti, cSen;
                int tReg = MakeTicket(out cReg, null, "CTC");
                int tMulti = MakeTicket(out cMulti, null, "CTC", "DEATH");
                int tSen = MakeTicket(out cSen, s => Set(s, "Senior", true), "BIRTHREG");
                string cPick;
                int tPick = MakeTicket(out cPick, null, "DEATH");
                Check("four kiosk tickets are Waiting", S(QRow(tReg)["status"]) == "Waiting" && S(QRow(tMulti)["status"]) == "Waiting" && S(QRow(tSen)["status"]) == "Waiting" && S(QRow(tPick)["status"]) == "Waiting",
                      cReg + ", " + cMulti + ", " + cSen + ", " + cPick);

                Form q = go("queue");
                Check("queue module hosted by the shell", q != null && q.GetType().Name == "QueueManagementForm");
                Action<string> qcall = m => Call(q, m);
                EventArgs ea = EventArgs.Empty;

                // ---- pause blocks Call Next
                int m0 = wd.Count;
                Call(q, "btnPause_Click", null, ea);
                Call(q, "btnCallNext_Click", null, ea);
                Check("paused queue refuses Call Next and says why", wd.Saw(m0, "Queue paused") && S(QRow(tSen)["status"]) == "Waiting");
                Call(q, "btnPause_Click", null, ea);
                Check("queue resumes", !(bool)Fld(q, "_paused"));

                // ---- Call Next: the Senior-lane client is accepted first, to MY window
                Call(q, "btnCallNext_Click", null, ea);
                DataRow r = QRow(tSen);
                Check("Call Next takes the priority-lane ticket first", S(r["status"]) == "Accepted" && Convert.ToInt32(r["window_no"]) == wa && r["accepted_at"] != DBNull.Value,
                      cSen + " -> " + S(r["status"]));
                Check("the regular tickets keep waiting", S(QRow(tReg)["status"]) == "Waiting" && S(QRow(tMulti)["status"]) == "Waiting");

                m0 = wd.Count;
                Call(q, "btnCallNext_Click", null, ea);
                Check("a second Call Next is refused while the window is busy", wd.Saw(m0, "Nothing to call") && S(QRow(tReg)["status"]) == "Waiting");

                // ---- Call Client, Recall
                Call(q, "CallClient");
                r = QRow(tSen);
                Check("Call Client puts it on the board (Serving, timestamps set)", S(r["status"]) == "Serving" && r["called_at"] != DBNull.Value && r["started_at"] != DBNull.Value);
                Call(q, "RecallCurrent");
                Check("Recall keeps it Serving and counts the recall", Convert.ToInt32(QRow(tSen)["recall_count"]) == 1 && S(QRow(tSen)["status"]) == "Serving");

                // ---- Forward to another window (same queue number)
                Call(q, "ForwardCurrent");
                r = QRow(tSen);
                DataTable fw = Db.Pull("SELECT * FROM queue_ticket_forwards WHERE ticket_id=@t", new MySqlParameter("@t", tSen));
                Check("Forward moves the ticket on (Accepted elsewhere) and logs the hand-off",
                      S(r["status"]) == "Accepted" && Convert.ToInt32(r["window_no"]) != wa && fw.Rows.Count == 1 && Convert.ToInt32(fw.Rows[0]["from_window"]) == wa,
                      "now at window " + S(r["window_no"]));
                Check("my window is free again", Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM queue_tickets WHERE window_no=@w AND status IN ('Accepted','Serving')", new MySqlParameter("@w", wa)).Rows[0][0]) == 0);

                // ---- an explicit pick (a row the operator clicked) beats first-come-first-served
                object panel = Fld(shell, "_clientTasks");
                SetFld(q, "_pickedTicketId", tPick);
                Call(q, "btnCallNext_Click", null, ea);
                Check("a row the operator picked is called out of turn", S(QRow(tPick)["status"]) == "Accepted" && Convert.ToInt32(QRow(tPick)["window_no"]) == wa && S(QRow(tReg)["status"]) == "Waiting", cPick);
                Call(q, "CallClient");
                Call(panel, "Reload");
                Call(panel, "AbandonAllTasks");
                Check("the picked client's visit closed (abandoned) and the window is free", S(QRow(tPick)["final_status"]) == "Abandoned" && QRow(tPick)["window_no"] == DBNull.Value);

                // ---- with nothing picked, Call Next is first-come-first-served (oldest regular, not newest)
                Call(q, "btnCallNext_Click", null, ea);
                Check("Call Next with no pick takes the OLDEST regular ticket", S(QRow(tReg)["status"]) == "Accepted" && Convert.ToInt32(QRow(tReg)["window_no"]) == wa && S(QRow(tMulti)["status"]) == "Waiting", cReg);
                Call(q, "CallClient");

                Call(panel, "Reload");
                DataRow svc = Db.Pull("SELECT id, service_label FROM queue_ticket_services WHERE ticket_id=@t AND service_code='CTC'", new MySqlParameter("@t", tReg)).Rows[0];
                int ctcTask = Convert.ToInt32(svc["id"]); string ctcLabel = S(svc["service_label"]);
                Call(panel, "ProcessTask", ctcTask, "CTC", ctcLabel);
                Check("Process opens the CTC task", Db.Pull("SELECT status FROM queue_ticket_services WHERE id=@i", new MySqlParameter("@i", ctcTask)).Rows[0][0].ToString() == "Serving");

                Form cert = go("certrequest");
                Check("Certificate Request opened with the kiosk request (copies 2)", Txt(cert, "txtCopies") == "2" && Txt(cert, "txtLast").Length > 0, Txt(cert, "txtFirst") + " " + Txt(cert, "txtLast"));
                Call(cert, "btnCreate_Click", null, ea);

                DataRow tx = Db.Pull("SELECT * FROM transactions WHERE client_name LIKE @n ORDER BY id DESC LIMIT 1", new MySqlParameter("@n", "%" + Tag + "%")).Rows[0];
                long txn = Convert.ToInt64(tx["id"]);
                DataRow cr = Db.Pull("SELECT * FROM certificate_requests WHERE transaction_id=@t", new MySqlParameter("@t", txn)).Rows[0];
                Check("request opened a transaction at ForPayment after 'Certificate Ready'", S(tx["status"]) == "ForPayment" && S(tx["created_by"]) == admin.ToString(), S(tx["txn_code"]) + " " + S(tx["status"]));
                Check("certificate request is Ready, 2 copies, CTC", S(cr["status"]) == "Ready" && Convert.ToInt32(cr["copies"]) == 2 && S(cr["cert_type"]) == "CTC");
                Check("the queue ticket is linked to the transaction", S(QRow(tReg)["transaction_id"]) == txn.ToString());

                // ---- Fees & Payments
                Form fees = go("fees");
                decimal expected = PaymentService.AssessCertificate("CTC", "Birth", 2).LineAmount;
                string orNo = "ZZF-OR-" + DateTime.Now.ToString("HHmmss");
                ((TextBox)Fld(fees, "_txtOr")).Text = orNo;
                ((TextBox)Fld(fees, "_txtTendered")).Text = "1.00";
                m0 = wd.Count;
                Call(fees, "RecordPayment");
                Check("tendering less than the total is refused", wd.Saw(m0, "Insufficient payment") &&
                      Db.Pull("SELECT status FROM transactions WHERE id=@t", new MySqlParameter("@t", txn)).Rows[0][0].ToString() == "ForPayment");

                ((TextBox)Fld(fees, "_txtTendered")).Text = (expected + 100m).ToString("0.00");
                m0 = wd.Count;
                Call(fees, "RecordPayment");
                DataTable pay = Db.Pull("SELECT * FROM payments WHERE transaction_id=@t", new MySqlParameter("@t", txn));
                Check("payment recorded: one O.R., net = assessed fee x copies, 100 change",
                      pay.Rows.Count == 1 && S(pay.Rows[0]["or_number"]) == orNo && Convert.ToDecimal(pay.Rows[0]["net_amount"]) == expected,
                      "expected " + expected + (pay.Rows.Count > 0 ? ", got " + pay.Rows[0]["net_amount"] : ""));
                Check("transaction moves to ForRelease", Db.Pull("SELECT status FROM transactions WHERE id=@t", new MySqlParameter("@t", txn)).Rows[0][0].ToString() == "ForRelease");
                Check("the 'Paid' notice appeared and the print dialog was cancelled, not confirmed", wd.Saw(m0, "|Paid|") && wd.Saw(m0, "COMMON|"));
                Check("payment line itemised", pay.Rows.Count == 1 && Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM payment_items WHERE payment_id=@p", new MySqlParameter("@p", pay.Rows[0]["id"])).Rows[0][0]) == 1);

                // ---- Finish is refused until the document is actually released
                m0 = wd.Count;
                Call(panel, "FinishTask", ctcTask, ctcLabel);
                Check("Finish is refused while the request is not released", wd.Saw(m0, "Not yet released") &&
                      Db.Pull("SELECT status FROM queue_ticket_services WHERE id=@i", new MySqlParameter("@i", ctcTask)).Rows[0][0].ToString() == "Serving");

                // ---- Release & Claim
                Form rel = go("release");
                ((TextBox)Fld(rel, "txtClaimant")).Text = "ZZF Claimant";
                ((CheckBox)Fld(rel, "chkRep")).Checked = true;
                ((TextBox)Fld(rel, "txtIdNum")).Text = "";
                Call(rel, "btnRelease_Click", null, ea);
                Check("a representative without an ID number is refused", Txt(rel, "lblValidation").Contains("Representative ID number"), Txt(rel, "lblValidation"));
                ((CheckBox)Fld(rel, "chkRep")).Checked = false;

                m0 = wd.Count;
                Call(rel, "btnRelease_Click", null, ea);
                DataTable rl = Db.Pull("SELECT * FROM releases WHERE transaction_id=@t", new MySqlParameter("@t", txn));
                Check("release saved: claimant, not a representative, released_by me",
                      rl.Rows.Count == 1 && S(rl.Rows[0]["claimant_name"]) == "ZZF Claimant" && Convert.ToInt32(rl.Rows[0]["is_representative"]) == 0 && S(rl.Rows[0]["released_by"]) == admin.ToString());
                Check("transaction and certificate request are Released",
                      Db.Pull("SELECT status FROM transactions WHERE id=@t", new MySqlParameter("@t", txn)).Rows[0][0].ToString() == "Released" &&
                      Db.Pull("SELECT status FROM certificate_requests WHERE transaction_id=@t", new MySqlParameter("@t", txn)).Rows[0][0].ToString() == "Released");
                Check("verification window shown and the 'Released' notice named the transaction", wd.Saw(m0, "FORM|ReleaseVerifyDialog") && wd.Saw(m0, "|Released|"));

                // ---- close the task and the visit
                Call(panel, "FinishTask", ctcTask, ctcLabel);
                Check("Finish now succeeds", Db.Pull("SELECT status FROM queue_ticket_services WHERE id=@i", new MySqlParameter("@i", ctcTask)).Rows[0][0].ToString() == "Completed");
                Call(panel, "CompleteVisit");
                r = QRow(tReg);
                Check("visit completed: ticket Completed, window freed, time stamped", S(r["status"]) == "Completed" && S(r["final_status"]) == "Completed" && r["window_no"] == DBNull.Value && r["completed_at"] != DBNull.Value);

                // ---- two-service client: guards, abandon one, abandon the rest
                Call(q, "btnCallNext_Click", null, ea);
                Check("next client (two services) accepted", S(QRow(tMulti)["status"]) == "Accepted" && Convert.ToInt32(QRow(tMulti)["window_no"]) == wa, cMulti);
                Call(q, "CallClient");
                Call(panel, "Reload");
                DataTable ts = Db.Pull("SELECT id, service_code, service_label FROM queue_ticket_services WHERE ticket_id=@t ORDER BY id", new MySqlParameter("@t", tMulti));
                int tCtc = Convert.ToInt32(ts.Rows[0]["id"]), tDeath = Convert.ToInt32(ts.Rows[1]["id"]);
                Call(panel, "ProcessTask", tCtc, "CTC", S(ts.Rows[0]["service_label"]));

                m0 = wd.Count;
                Call(panel, "ProcessTask", tDeath, "DEATH", S(ts.Rows[1]["service_label"]));
                Check("a second task cannot start while one is in progress", wd.Saw(m0, "Finish the task already in progress") &&
                      Db.Pull("SELECT status FROM queue_ticket_services WHERE id=@i", new MySqlParameter("@i", tDeath)).Rows[0][0].ToString() == "Pending");
                m0 = wd.Count;
                Call(panel, "CompleteVisit");
                Check("the visit cannot be completed with tasks open", wd.Saw(m0, "Every client task must be finished") && S(QRow(tMulti)["status"]) == "Serving");

                Call(panel, "AbandonCurrentTask");
                DataRow ab = Db.Pull("SELECT status, abandon_reason, abandoned_by FROM queue_ticket_services WHERE id=@i", new MySqlParameter("@i", tCtc)).Rows[0];
                Check("abandoning a task records why and who", S(ab["status"]) == "Abandoned" && (S(ab["abandon_reason"]) ?? "").Contains("client left") && S(ab["abandoned_by"]) == admin.ToString());
                Call(panel, "ProcessTask", tDeath, "DEATH", S(ts.Rows[1]["service_label"]));
                Check("the other task can start now", Db.Pull("SELECT status FROM queue_ticket_services WHERE id=@i", new MySqlParameter("@i", tDeath)).Rows[0][0].ToString() == "Serving");

                Call(panel, "AbandonAllTasks");
                r = QRow(tMulti);
                Check("abandon all: ticket closed as Abandoned and the window is freed",
                      S(r["status"]) == "Completed" && S(r["final_status"]) == "Abandoned" && r["window_no"] == DBNull.Value &&
                      Db.Pull("SELECT status FROM queue_ticket_services WHERE id=@i", new MySqlParameter("@i", tDeath)).Rows[0][0].ToString() == "Abandoned");

                // ---- every dialog the run met
                var unexpected = wd.Since(0).Where(l => l.StartsWith("UNEXPECTED")).ToList();
                Check("no unexpected window appeared", unexpected.Count == 0, string.Join(" ; ", unexpected));
                Console.WriteLine("  dialogs answered during the queue/payment run:");
                foreach (string l in wd.Since(0)) Console.WriteLine("    " + (l.Length > 170 ? l.Substring(0, 170) + "..." : l));
            }
            catch (Exception ex)
            {
                Check("queue/payment run completed without crashing", false, ex.GetType().Name + ": " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                if (shell != null)
                {
                    Console.WriteLine("  DIAG dialogs/forms the watchdog met so far:");
                    foreach (string l in wd.Since(0)) Console.WriteLine("    " + (l.Length > 200 ? l.Substring(0, 200) : l));
                    Console.Out.Flush();
                    Console.WriteLine("  DIAG disposed controls still in the shell tree:");
                    DumpDisposed(shell, "shell");
                    Session.WindowId = (int)wa;
                    foreach (string key in new[] { "fees", "release", "certrequest", "queue" })
                    {
                        try { shell.GetType().GetMethod("GoToModule").Invoke(shell, new object[] { key }); Console.WriteLine("  DIAG shell hosts '" + key + "': ok"); }
                        catch (Exception e2) { Exception ie = e2.InnerException ?? e2; Console.WriteLine("  DIAG shell hosts '" + key + "': " + ie.GetType().Name + ": " + ie.Message.Replace("\r\n", " ")); }
                    }
                    Console.WriteLine("  DIAG exception: " + (ex.InnerException ?? ex).StackTrace.Split('\n').Take(4).Aggregate((a, b) => a + " | " + b.Trim()));
                }
            }
            finally
            {
                wd.Stop();
                Session.WindowId = 0; Session.WindowName = null;
                try { if (shell != null) shell.Dispose(); } catch { }
            }
        }

        private static void DumpDisposed(Control c, string path)
        {
            foreach (Control k in c.Controls)
            {
                string p = path + "/" + k.GetType().Name + (string.IsNullOrEmpty(k.Name) ? "" : "(" + k.Name + ")");
                if (k.IsDisposed) Console.WriteLine("    DISPOSED " + p);
                else DumpDisposed(k, p);
            }
        }

        // ---- cleanup of everything the queue/payment run creates (called from Cleanup)
        private static void CleanupQueue()
        {
            foreach (DataRow w in Db.Pull("SELECT id FROM windows WHERE window_name LIKE 'ZZF%'").Rows)
            {
                int id = Convert.ToInt32(w[0]);
                // Audit rows written while a test window was the operator's window - exact, not by id of a record.
                Db.Push("DELETE FROM audit_log WHERE window_id = @w", new MySqlParameter("@w", id));
                Db.Push("DELETE FROM window_transactions WHERE window_id = @w", new MySqlParameter("@w", id));
                Db.Push("UPDATE queue_ticket_services SET locked_by_window = NULL WHERE locked_by_window = @w", new MySqlParameter("@w", id));
                Db.Push("DELETE FROM windows WHERE id = @w", new MySqlParameter("@w", id));
            }
            foreach (DataRow t in Db.Pull("SELECT id FROM transactions WHERE client_name LIKE '%ZZF%'").Rows)
            {
                var p = new MySqlParameter("@t", Convert.ToInt64(t[0]));
                Db.Push("DELETE FROM payment_items WHERE payment_id IN (SELECT id FROM payments WHERE transaction_id = @t)", p);
                Db.Push("DELETE FROM payments WHERE transaction_id = @t", new MySqlParameter("@t", Convert.ToInt64(t[0])));
                Db.Push("DELETE FROM releases WHERE transaction_id = @t", new MySqlParameter("@t", Convert.ToInt64(t[0])));
                Db.Push("DELETE FROM certificate_requests WHERE transaction_id = @t", new MySqlParameter("@t", Convert.ToInt64(t[0])));
                Db.Push("DELETE FROM claim_requests WHERE transaction_id = @t", new MySqlParameter("@t", Convert.ToInt64(t[0])));
                Db.Push("DELETE FROM transactions WHERE id = @t", new MySqlParameter("@t", Convert.ToInt64(t[0])));
            }
        }

        private static int LeftoversQueue()
        {
            int n = 0;
            n += Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM windows WHERE window_name LIKE 'ZZF%'").Rows[0][0]);
            n += Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM transactions WHERE client_name LIKE '%ZZF%'").Rows[0][0]);
            n += Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM payments WHERE or_number LIKE 'ZZF-OR%'").Rows[0][0]);
            n += Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM releases WHERE claimant_name LIKE 'ZZF%'").Rows[0][0]);
            return n;
        }
    }
}
