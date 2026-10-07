using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.MarriageTest
{
    /// <summary>
    /// Every-transaction sweep. Part A drives the REAL kiosk (CROMS.Kiosk.exe, KioskCore.Submit with
    /// TestMode so nothing prints or blocks) for each of the 13 services, a multi-service ticket,
    /// priority lanes and the refusal cases. Part B hands each ticket to the staff side. All data is
    /// tagged ZZF... and removed afterwards.
    /// </summary>
    internal static partial class FlowsTest
    {
        public static int Pass, Fail;
        private static readonly string Tag = "ZZF" + DateTime.Now.ToString("HHmmss");

        private static Assembly _k;
        private static Type _sessionT, _core;
        private static readonly Dictionary<string, int> _ids = new Dictionary<string, int>();
        private static readonly Dictionary<string, string> _codes = new Dictionary<string, string>();

        // ---- reflection helpers for the staff forms
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static object Fld(object o, string name)
        {
            for (Type t = o.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, Any | BindingFlags.DeclaredOnly);
                if (f != null) return f.GetValue(o);
            }
            throw new MissingFieldException(o.GetType().Name, name);
        }
        private static string Txt(object o, string name) { return ((System.Windows.Forms.Control)Fld(o, name)).Text; }
        private static object NewForm(string typeName, params object[] args)
        {
            Type t = typeof(Db).Assembly.GetType("CROMS.Forms." + typeName, true);
            return Activator.CreateInstance(t, Any, null, args, null);
        }
        private static void Call(object o, string method, params object[] args)
        {
            MethodInfo m = o.GetType().GetMethod(method, Any);
            if (m == null) throw new MissingMethodException(o.GetType().Name, method);
            try { m.Invoke(o, args); } catch (TargetInvocationException ex) { throw ex.InnerException; }
        }

        private static void Check(string name, bool ok, string detail = null)
        {
            if (ok) Pass++; else Fail++;
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + (detail == null ? "" : "   -> " + detail));
        }

        /// <summary>Removes anything a crashed or killed --flows run left behind.</summary>
        public static int CleanOnly()
        {
            Cleanup();
            int left = Leftovers();
            Console.WriteLine("flows cleanup: leftovers = " + left);
            return left;
        }

        public static int Run()
        {
            Console.WriteLine("CROMS every-transaction sweep  -  tag " + Tag);
            try
            {
                Cleanup();
                KioskPart();
                StaffPart();
                QueuePart();
            }
            catch (Exception ex) { Fail++; Console.WriteLine("CRASH: " + ex); }
            finally
            {
                try { Cleanup(); int left = Leftovers(); Check("zero strays after cleanup", left == 0, left + " left"); }
                catch (Exception ex) { Fail++; Console.WriteLine("cleanup FAILED: " + ex.Message); }
            }
            Console.WriteLine("PASSED " + Pass + "   FAILED " + Fail);
            return Fail;
        }

        // ------------------------------------------------------------ kiosk helpers
        private static object NewSession()
        {
            object s = Activator.CreateInstance(_sessionT);
            Set(s, "First", "Maria"); Set(s, "Last", Tag + "Santos"); Set(s, "Contact", "09170001111");
            return s;
        }
        private static void Set(object s, string field, object v) { _sessionT.GetField(field).SetValue(s, v); }
        private static object Get(object s, string field) { return _sessionT.GetField(field).GetValue(s); }
        private static void Pick(object s, params string[] codes)
        {
            var sel = (List<string>)Get(s, "Selected"); sel.Clear(); sel.AddRange(codes);
        }
        private static string Validate(object s)
        {
            object[] a = { s, null };
            bool ok = (bool)_core.GetMethod("Validate").Invoke(null, a);
            return ok ? null : (string)a[1];
        }
        private static bool Submit(object s, out string error)
        {
            object[] a = { s, null };
            bool ok = (bool)_core.GetMethod("Submit").Invoke(null, a);
            error = (string)a[1];
            return ok;
        }
        private static void FillCtc(object s, string doc)
        {
            Set(s, "CtcDocumentType", doc); Set(s, "CtcCopies", 2); Set(s, "CtcPurpose", "Employment");
            Set(s, "CtcRelationship", "Self");
            Set(s, "CtcOwnerFirst", "Pedro"); Set(s, "CtcOwnerLast", Tag + "Reyes");
            Set(s, "CtcEventProvince", "Cagayan"); Set(s, "CtcEventCity", "Penablanca");
            Set(s, "CtcEventDate", (DateTime?)new DateTime(1990, 5, 17));
            if (doc == "Marriage") { Set(s, "CtcSpouseFirst", "Ana"); Set(s, "CtcSpouseLast", Tag + "Cruz"); }
        }
        private static void FillBreqs(object s, string doc)
        {
            Set(s, "BreqsDocType", doc); Set(s, "BreqsCopies", 2); Set(s, "BreqsPurpose", "Passport");
            Set(s, "BreqsRelationship", "Self"); Set(s, "IdType", "Driver's License (LTO)"); Set(s, "IdNo", "N01-23-456789");
            Set(s, "OwnerFirst", "Luz"); Set(s, "OwnerLast", Tag + "Dizon");
            Set(s, "EventDate", (DateTime?)new DateTime(1985, 3, 2));
            if (doc == "Marriage") { Set(s, "SpouseFirst", "Jose"); Set(s, "SpouseLast", Tag + "Dizon"); }
        }

        private static DataTable Ticket(string code)
        {
            return Db.Pull("SELECT * FROM queue_tickets WHERE full_name LIKE @n AND ticket_code = @c",
                           new MySqlParameter("@n", "%" + Tag + "%"), new MySqlParameter("@c", code));
        }

        // ------------------------------------------------------------ Part A: kiosk
        private static void KioskPart()
        {
            string exe = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\CROMS.Kiosk\bin\Debug\CROMS.Kiosk.exe"));
            Check("kiosk build found", File.Exists(exe), exe);
            if (!File.Exists(exe)) return;
            _k = Assembly.LoadFrom(exe);
            _sessionT = _k.GetType("CROMS.Kiosk.KioskSession", true);
            _core = _k.GetType("CROMS.Kiosk.KioskCore", true);
            _core.GetField("TestMode").SetValue(null, true);

            // The ticket INSERT stores the typed name as "First Middle Last"; our tag lives in Last,
            // so Cleanup can find everything by LIKE '%ZZF%'.
            // --- refusal cases (nothing may be written)
            int before = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM queue_tickets WHERE full_name LIKE '%" + Tag + "%'").Rows[0][0]);
            object s = NewSession();
            Check("no service chosen is refused", Validate(s) != null && Validate(s).Contains("service"), Validate(s));
            s = NewSession(); Pick(s, "BIRTHREG"); Set(s, "First", "");
            Check("missing first name is refused", Validate(s) != null && Validate(s).Contains("first and last name"), Validate(s));
            s = NewSession(); Pick(s, "MARRIAGE_APP");
            Check("marriage application needs the spouse", Validate(s) != null && Validate(s).Contains("spouse"), Validate(s));
            s = NewSession(); Pick(s, "MARRIAGE_REG");
            Check("marriage registration needs who is submitting", Validate(s) != null && Validate(s).Contains("submitting"), Validate(s));
            Set(s, "SubmittedBy", "Representative");
            Check("'Others' needs a specification", Validate(s) != null && Validate(s).Contains("specify"), Validate(s));
            s = NewSession(); Pick(s, "CTC");
            Check("CTC needs a document type", Validate(s) != null && Validate(s).Contains("document"), Validate(s));
            FillCtc(s, "Birth"); Set(s, "CtcEventProvince", null);
            Check("CTC needs a province", Validate(s) != null && Validate(s).Contains("province"), Validate(s));
            FillCtc(s, "Marriage"); Set(s, "CtcSpouseFirst", "");
            Check("CTC marriage needs the wife", Validate(s) != null && Validate(s).Contains("wife"), Validate(s));
            s = NewSession(); Pick(s, "BREQS");
            Check("PSA copy refuses an empty request", Validate(s) != null && Validate(s).Contains("PSA certificate"), Validate(s));
            FillBreqs(s, "Birth"); Set(s, "EventDate", (DateTime?)DateTime.Today.AddDays(5));
            Check("PSA copy refuses a future date", Validate(s) != null && Validate(s).Contains("future"), Validate(s));
            s = NewSession(); Pick(s, "CLAIM"); Set(s, "ClaimTicketEntry", "Q-999999");
            Check("claim with an unknown queue number is refused", Validate(s) != null && Validate(s).Contains("not found"), Validate(s));
            int afterRefusals = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM queue_tickets WHERE full_name LIKE '%" + Tag + "%'").Rows[0][0]);
            Check("refusals wrote no ticket", before == afterRefusals, before + " -> " + afterRefusals);

            // --- one real ticket per service
            var created = new Dictionary<string, string>();
            foreach (var svc in new[] { "BIRTHREG", "MARRIAGE_REG", "DEATH", "MARRIAGE_APP", "LEGITIMATION", "LEGITIMATION_RA9255",
                                        "CTC", "BREQS", "CLAIM", "PETITION", "SUPPLEMENTAL_REPORT", "LEGAL_INSTRUMENTS", "COURT_ORDER" })
            {
                s = NewSession(); Pick(s, svc);
                if (svc == "MARRIAGE_REG") { Set(s, "SubmittedBy", "Husband"); Set(s, "HasMarriageLicense", true); }
                if (svc == "MARRIAGE_APP") { Set(s, "First2", "Jose"); Set(s, "Middle2", "Dela"); Set(s, "Last2", Tag + "Cruz"); }
                if (svc == "CTC") FillCtc(s, "Birth");
                if (svc == "BREQS") FillBreqs(s, "Birth");
                string err;
                bool ok;
                try { ok = Submit(s, out err); }
                catch (TargetInvocationException tex) { ok = false; err = tex.InnerException.Message; }
                Check("kiosk issues a ticket for " + svc, ok, err);
                if (!ok) continue;
                DataTable t = Db.Pull("SELECT * FROM queue_tickets WHERE full_name LIKE @n ORDER BY id DESC LIMIT 1", new MySqlParameter("@n", "%" + Tag + "%"));
                string code = t.Rows[0]["ticket_code"].ToString(); created[svc] = code;
                int tid = Convert.ToInt32(t.Rows[0]["id"]);
                _ids[svc] = tid; _codes[svc] = code;
                DataTable sv = Db.Pull("SELECT service_code, status FROM queue_ticket_services WHERE ticket_id=@t", new MySqlParameter("@t", tid));
                Check(svc + ": ticket Waiting, one service row", t.Rows[0]["status"].ToString() == "Waiting" && sv.Rows.Count == 1 && sv.Rows[0]["service_code"].ToString() == svc,
                      code + " / " + sv.Rows.Count + " svc");
            }

            // --- service-specific data landed where staff will read it
            if (created.ContainsKey("MARRIAGE_APP"))
            {
                DataRow t = Ticket(created["MARRIAGE_APP"]).Rows[0];
                Check("marriage application keeps both applicants' name cells",
                      t["app_h_first"].ToString() == "Maria" && t["app_h_last"].ToString() == Tag + "Santos" &&
                      t["app_w_first"].ToString() == "Jose" && t["app_w_middle"].ToString() == "Dela" && t["app_w_last"].ToString() == Tag + "Cruz");
                Check("marriage application stores the spouse's joined name", (t["spouse_full_name"] as string ?? "").Contains(Tag + "Cruz"));
            }
            if (created.ContainsKey("MARRIAGE_REG"))
            {
                DataRow t = Ticket(created["MARRIAGE_REG"]).Rows[0];
                Check("marriage registration records who submits", t["submitted_by"].ToString() == "Husband" && t["spouse_full_name"] == DBNull.Value);
                Check("licence-in-hand note on the ticket", (t["purpose"] as string ?? "").Contains("Marriage License"));
            }
            if (created.ContainsKey("CTC"))
            {
                DataRow t = Ticket(created["CTC"]).Rows[0];
                DataTable c = Db.Pull("SELECT * FROM ctc_requests WHERE queue_ticket_id=@t", new MySqlParameter("@t", Convert.ToInt32(t["id"])));
                Check("CTC intake row saved", c.Rows.Count == 1 && c.Rows[0]["doc_type"].ToString() == "Birth" && Convert.ToInt32(c.Rows[0]["copies"]) == 2 &&
                      c.Rows[0]["owner_last"].ToString() == Tag + "Reyes" && c.Rows[0]["event_province"].ToString() == "Cagayan");
                Check("CTC one-line summary on the ticket", (t["purpose"] as string ?? "").Contains("Birth") && (t["purpose"] as string).Contains(Tag + "Reyes"), t["purpose"].ToString());
            }
            if (created.ContainsKey("BREQS"))
            {
                DataRow t = Ticket(created["BREQS"]).Rows[0];
                DataTable b = Db.Pull("SELECT * FROM breqs_requests WHERE queue_ticket_id=@t", new MySqlParameter("@t", Convert.ToInt32(t["id"])));
                Check("PSA request saved Requested, source Kiosk, fee 2 x 50",
                      b.Rows.Count == 1 && b.Rows[0]["status"].ToString() == "Requested" && b.Rows[0]["source"].ToString() == "Kiosk" &&
                      Convert.ToDecimal(b.Rows[0]["fee_amount"]) == 100m, b.Rows.Count > 0 ? b.Rows[0]["fee_amount"].ToString() : "none");
            }

            // --- priority lanes
            foreach (var lane in new[] { "Senior", "Pwd", "Pregnant" })
            {
                s = NewSession(); Pick(s, "CTC"); FillCtc(s, "Death"); Set(s, lane, true);
                string err; bool ok = Submit(s, out err);
                DataRow t = Db.Pull("SELECT priority FROM queue_tickets WHERE full_name LIKE @n ORDER BY id DESC LIMIT 1", new MySqlParameter("@n", "%" + Tag + "%")).Rows[0];
                Check("lane " + lane + " is stamped on the ticket", ok && t["priority"].ToString() != "Regular", t["priority"].ToString());
            }
            s = NewSession(); Pick(s, "CTC"); FillCtc(s, "Death"); { string e; Submit(s, out e); }
            Check("a normal client is Regular", Db.Pull("SELECT priority FROM queue_tickets WHERE full_name LIKE @n ORDER BY id DESC LIMIT 1", new MySqlParameter("@n", "%" + Tag + "%")).Rows[0][0].ToString() == "Regular");

            // --- one visit, several services
            s = NewSession(); Pick(s, "BIRTHREG", "CTC", "CLAIM"); FillCtc(s, "Birth");
            { string e; bool ok = Submit(s, out e); Check("multi-service ticket issues", ok, e); }
            DataRow mt = Db.Pull("SELECT id FROM queue_tickets WHERE full_name LIKE @n ORDER BY id DESC LIMIT 1", new MySqlParameter("@n", "%" + Tag + "%")).Rows[0];
            Check("multi-service ticket carries all three services",
                  Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM queue_ticket_services WHERE ticket_id=@t", new MySqlParameter("@t", Convert.ToInt32(mt[0]))).Rows[0][0]) == 3);

            // --- numbering: every code unique today
            DataTable dup = Db.Pull("SELECT ticket_code, COUNT(*) c FROM queue_tickets WHERE date = CURDATE() GROUP BY ticket_code HAVING c > 1");
            Check("no duplicate ticket codes today", dup.Rows.Count == 0, dup.Rows.Count + " duplicates");
        }

        // ------------------------------------------------------------ Part B: staff side
        private static void Guard(string name, Action body)
        {
            try { body(); }
            catch (Exception ex) { Check(name, false, "threw " + ex.GetType().Name + ": " + ex.Message); }
        }

        private static void StaffPart()
        {
            Console.WriteLine("-- staff side: each kiosk ticket handed to the real staff form --");
            if (_ids.Count == 0) { Check("staff part has tickets to work with", false); return; }

            Guard("Certificate Request prefills from a CTC kiosk ticket", () =>
            {
                object f = NewForm("CertificateRequestForm");
                Call(f, "PrepareForQueueTicket", _ids["CTC"], _codes["CTC"]);
                string copies = Txt(f, "txtCopies"), purpose = Txt(f, "txtPurpose"), first = Txt(f, "txtFirst"), last = Txt(f, "txtLast");
                var rt = (System.Windows.Forms.ComboBox)Fld(f, "cboRecordType");
                Check("  CTC: copies 2, purpose Employment, record type Birth",
                      copies == "2" && purpose == "Employment" && Convert.ToString(rt.SelectedItem) == "Birth",
                      copies + " / " + purpose + " / " + rt.SelectedItem);
                Check("  CTC: requester name boxes filled from the ticket", first == "Maria" && last == Tag + "Santos", first + " | " + last);
                Check("  CTC: Find Record is enabled once the type is known", ((System.Windows.Forms.Button)Fld(f, "btnFindRecord")).Enabled);
            });

            // Two-word surname: the name is stored joined, and the form splits it by word count.
            Guard("Certificate Request keeps a two-word surname whole", () =>
            {
                object s = NewSession(); Pick(s, "CTC"); FillCtc(s, "Birth");
                Set(s, "First", "Juan"); Set(s, "Last", Tag + " De La Cruz");
                string err; bool ok = Submit(s, out err);
                if (!ok) { Check("  two-word surname ticket issues", false, err); return; }
                DataRow t = Db.Pull("SELECT id, ticket_code FROM queue_tickets WHERE full_name LIKE @n ORDER BY id DESC LIMIT 1",
                                    new MySqlParameter("@n", "%" + Tag + " De La Cruz%")).Rows[0];
                object f = NewForm("CertificateRequestForm");
                Call(f, "PrepareForQueueTicket", Convert.ToInt32(t["id"]), t["ticket_code"].ToString());
                string last = Txt(f, "txtLast"), mid = Txt(f, "txtMiddle");
                Check("  surname 'De La Cruz' lands in Last name as typed", last.EndsWith("De La Cruz"), "Last='" + last + "' Middle='" + mid + "'");
            });

            Guard("Case Tracking prefills requester + case type", () =>
            {
                foreach (var pair in new[] { new[] { "LEGITIMATION", "Legit" }, new[] { "SUPPLEMENTAL_REPORT", "Supplemental" },
                                             new[] { "LEGAL_INSTRUMENTS", "Legal" }, new[] { "COURT_ORDER", "Court" } })
                {
                    object f = NewForm("PetitionsForm");
                    Call(f, "PrepareForQueueTicket", _ids[pair[0]], _codes[pair[0]], pair[0]);
                    string req = Txt(f, "txtRequester"), type = Txt(f, "cboType");
                    Check("  " + pair[0] + ": requester + case type", req.Contains(Tag + "Santos") && type.IndexOf(pair[1], StringComparison.OrdinalIgnoreCase) >= 0, req + " / " + type);
                }
                object p2 = NewForm("PetitionsForm");
                Call(p2, "PrepareForQueueTicket", _ids["PETITION"], _codes["PETITION"], "PETITION");
                Check("  PETITION: requester filled, RA 9048/10172 left for staff", Txt(p2, "txtRequester").Contains(Tag + "Santos"), Txt(p2, "cboType"));
            });

            Guard("Form 90 prefills both applicants from a Marriage Application ticket", () =>
            {
                object f = NewForm("MarriageLicenseForm", new object[] { null });
                Call(f, "PrepareForQueueTicket", _ids["MARRIAGE_APP"], _codes["MARRIAGE_APP"]);
                object h = Fld(f, "_h"), w = Fld(f, "_w");
                Check("  husband First/Last", Txt(h, "First") == "Maria" && Txt(h, "Last") == Tag + "Santos", Txt(h, "First") + " | " + Txt(h, "Last"));
                Check("  wife First/Middle/Last", Txt(w, "First") == "Jose" && Txt(w, "Middle") == "Dela" && Txt(w, "Last") == Tag + "Cruz",
                      Txt(w, "First") + " | " + Txt(w, "Middle") + " | " + Txt(w, "Last"));
                Check("  remarks carry the ticket + contact", Txt(f, "_remarks").Contains(_codes["MARRIAGE_APP"]) && Txt(f, "_remarks").Contains("09170001111"), Txt(f, "_remarks"));
            });

            Guard("Form 97 opens linked to its Marriage Registration ticket", () =>
            {
                object f = NewForm("MarriageEntryForm", new object[] { null });
                Call(f, "PrepareForQueueTicket", _ids["MARRIAGE_REG"], _codes["MARRIAGE_REG"]);
                Check("  'Submitted by: Husband' carried over", ((System.Windows.Forms.RadioButton)Fld(f, "_rbSubHusband")).Checked);
                Check("  queue ticket linked", Convert.ToString(Fld(f, "_queueCode")) == _codes["MARRIAGE_REG"]);
            });

            Guard("PSA desk finds the kiosk request by its ticket", () =>
            {
                object f = NewForm("BreqsForm");
                Call(f, "PrepareFromQueueTicket", _ids["BREQS"]);
                BreqsRequest r = BreqsService.LoadByTicket(_ids["BREQS"]);
                Check("  selected request is the kiosk's", r != null && Convert.ToString(Fld(f, "_selectedId")) == r.Id.ToString(), r == null ? "none" : r.RequestNo);
            });

            // BirthRegistrationForm.PrepareForQueueTicket opens the entry popup with ShowDialog, which
            // blocks an unattended run. Its save path is covered by --birthtest (50 checks).
            Console.WriteLine("  SKIP  Birth Registration hand-off (opens a modal entry window); covered by --birthtest");

            Guard("Release & Claim opens a kiosk CLAIM ticket", () =>
            {
                int tid = _ids["CLAIM"];
                string kioskName = Convert.ToString(Db.Pull("SELECT full_name FROM queue_tickets WHERE id=@t",
                    new MySqlParameter("@t", tid)).Rows[0][0]).Trim();

                // Bare pick-up ticket: no claim_requests row, no transaction.
                object f = NewForm("ReleaseClaimForm");
                Call(f, "PrepareFromQueueTicket", tid);
                string status = Txt(f, "lblClaimStatus");
                Check("  claimant box holds the kiosk name (joined, not split)", Txt(f, "txtClaimant") == kioskName,
                      "\"" + Txt(f, "txtClaimant") + "\" vs \"" + kioskName + "\"");
                Check("  status line names the next step", !status.Contains("no claim request is linked") &&
                      status.Contains("No request is linked") && status.Contains("previous queue number"), status);
                Check("  worklist search pre-filled with the name", Txt(f, "txtSearch") == kioskName, Txt(f, "txtSearch"));
                Check("  nothing auto-selected", Fld(f, "_selectedTxnId") == null && Fld(f, "_pickupClaimId") == null);
                Check("  Release stays disabled", !((System.Windows.Forms.Control)Fld(f, "btnRelease")).Enabled);
                Check("  claim row NOT created here", Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM claim_requests WHERE queue_ticket_id=@t",
                      new MySqlParameter("@t", tid)).Rows[0][0]) == 0);

                // Same ticket linked to a ready transaction: the normal path is kept.
                long txn = Db.Insert("INSERT INTO transactions (txn_code, client_name, type, status) " +
                                     "VALUES (@c, @n, 'Certification', 'ForRelease')",
                                     new MySqlParameter("@c", Tag + "-CLM"), new MySqlParameter("@n", kioskName));
                try
                {
                    Db.Push("UPDATE queue_tickets SET transaction_id=@x WHERE id=@t",
                            new MySqlParameter("@x", txn), new MySqlParameter("@t", tid));
                    object g = NewForm("ReleaseClaimForm");
                    Call(g, "PrepareFromQueueTicket", tid);
                    object sel = Fld(g, "_selectedTxnId");
                    Check("  linked ForRelease ticket preselects its transaction", sel != null && Convert.ToInt64(sel) == txn,
                          sel == null ? "none selected" : sel.ToString());
                    Check("  linked ticket keeps the claimant name", Txt(g, "txtClaimant") == kioskName, Txt(g, "txtClaimant"));
                }
                finally
                {
                    Db.Push("UPDATE queue_tickets SET transaction_id=NULL WHERE id=@t", new MySqlParameter("@t", tid));
                    Db.Push("DELETE FROM transactions WHERE id=@x", new MySqlParameter("@x", txn));
                }
            });
        }

        // ------------------------------------------------------------ cleanup
        private static void Cleanup()
        {
            string like = "%ZZF%";
            DataTable ids = Db.Pull("SELECT id FROM queue_tickets WHERE full_name LIKE @n OR spouse_full_name LIKE @n", new MySqlParameter("@n", like));
            foreach (DataRow r in ids.Rows)
            {
                var p = new MySqlParameter("@t", Convert.ToInt32(r[0]));
                Db.Push("DELETE FROM breqs_history WHERE request_id IN (SELECT id FROM breqs_requests WHERE queue_ticket_id=@t)", p);
                Db.Push("DELETE FROM breqs_requests WHERE queue_ticket_id=@t", new MySqlParameter("@t", Convert.ToInt32(r[0])));
                Db.Push("DELETE FROM ctc_requests WHERE queue_ticket_id=@t", new MySqlParameter("@t", Convert.ToInt32(r[0])));
                Db.Push("DELETE FROM queue_ticket_services WHERE ticket_id=@t", new MySqlParameter("@t", Convert.ToInt32(r[0])));
                Db.Push("DELETE FROM queue_ticket_forwards WHERE ticket_id=@t", new MySqlParameter("@t", Convert.ToInt32(r[0])));
                Db.Push("DELETE FROM queue_tickets WHERE id=@t", new MySqlParameter("@t", Convert.ToInt32(r[0])));
            }
            CleanupQueue();
        }

        private static int Leftovers()
        {
            int n = 0;
            n += Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM queue_tickets WHERE full_name LIKE '%ZZF%'").Rows[0][0]);
            n += Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM ctc_requests WHERE owner_last LIKE '%ZZF%'").Rows[0][0]);
            n += Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM breqs_requests WHERE owner_last LIKE '%ZZF%'").Rows[0][0]);
            n += LeftoversQueue();
            return n;
        }
    }
}
