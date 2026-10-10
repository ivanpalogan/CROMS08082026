using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using CROMS.Data;

namespace CROMS.MarriageTest
{
    /// <summary>
    /// "--shots &lt;dir&gt; [name,name]": one PNG per data-entry screen, drawn from the REAL forms against the
    /// connected schema (run it against the DEMO schema - existing sample records are opened, nothing is
    /// saved). A manifest (shots.json) says which table each PNG documents. The only write is a temporary
    /// online window so the kiosk shows its real, enabled screens; it is removed afterwards.
    /// </summary>
    internal static class ShotsCapture
    {
        private const BindingFlags NF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static string Out;
        private static Form Shell;
        private static DialogWatchdog Wd;
        private static HashSet<string> Only;
        private static int Ok, Bad;
        private static readonly List<string> Manifest = new List<string>();
        private static long _tmpWindow;

        private static bool Want(string group) { return Only == null || Only.Contains(group); }

        private static void Add(string file, string caption, params string[] tables)
        {
            Manifest.Add("{\"file\":\"" + J(file) + "\",\"caption\":\"" + J(caption) + "\",\"tables\":[" +
                         string.Join(",", tables.Select(t => "\"" + t + "\"")) + "]}");
        }
        private static string J(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\""); }

        public static int Run(string dir, string only)
        {
            Out = dir; Directory.CreateDirectory(dir);
            if (!string.IsNullOrEmpty(only)) Only = new HashSet<string>(only.Split(',').Select(x => x.Trim()));
            Console.WriteLine("CROMS data-entry screenshots -> " + dir + "   database=" + ServerConfig.DatabaseName);
            Application.EnableVisualStyles();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => Console.WriteLine("  (ui exception) " + e.Exception.GetType().Name + ": " + e.Exception.Message);
            Wd = new DialogWatchdog(); Wd.CloseUnexpected = false; Wd.Start();
            try
            {
                AuditTest.Login("Admin");
                Type mt = typeof(Db).Assembly.GetType("CROMS.MainForm", true);
                Shell = (Form)Activator.CreateInstance(mt, true);
                Shell.Opacity = 0; Shell.ShowInTaskbar = false; Shell.StartPosition = FormStartPosition.Manual;
                Shell.WindowState = FormWindowState.Normal; Shell.Location = new Point(0, 0);
                Shell.Show(); Wd.Ignore.Add(Shell.Handle); Shell.ClientSize = new Size(1920, 1040);
                AuditTest.Pump(8);

                if (Want("birth")) Guard("birth", Birth);
                if (Want("death")) Guard("death", Death);
                if (Want("marriage")) Guard("marriage", Marriage);
                if (Want("modules")) Guard("modules", Modules);
                if (Want("settings")) Guard("settings", SettingsScreens);
                if (Want("ocr")) Guard("ocr", Ocr);
                if (Want("kiosk")) Guard("kiosk", Kiosk);
            }
            catch (Exception ex) { Console.WriteLine("CRASH: " + ex); Bad++; }
            finally
            {
                try { if (_tmpWindow != 0) { Db.Push("DELETE FROM window_service_assignments WHERE window_id=" + _tmpWindow); Db.Push("DELETE FROM windows WHERE id=" + _tmpWindow); } } catch { }
                Wd.Stop();
                File.WriteAllText(Path.Combine(Out, "shots.json"), "[" + string.Join(",\n", Manifest) + "]", new UTF8Encoding(false));
                Console.WriteLine("shots ok=" + Ok + " failed=" + Bad);
            }
            return Bad;
        }

        private static void Guard(string name, Action a)
        {
            Console.WriteLine("\n--- " + name);
            try { a(); } catch (Exception ex) { Bad++; var e = ex.InnerException ?? ex; Console.WriteLine("  FAIL group " + name + ": " + e.GetType().Name + ": " + e.Message); }
        }

        private static string Id(string sql)
        {
            DataTable t = Db.Pull(sql);
            return t.Rows.Count == 0 || t.Rows[0][0] == DBNull.Value ? null : t.Rows[0][0].ToString();
        }

        // ------------------------------------------------------------------ png helpers
        private static void SavePng(Control c, string name)
        {
            for (int i = 0; i < 4; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }
            using (var bmp = new Bitmap(Math.Max(1, c.Width), Math.Max(1, c.Height)))
            {
                c.DrawToBitmap(bmp, new Rectangle(0, 0, c.Width, c.Height));
                bmp.Save(Path.Combine(Out, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
            }
            Ok++; Console.WriteLine("  ok  " + name + ".png");
        }

        private static void Polish(Control c)
        {
            try
            {
                Type t = typeof(Db).Assembly.GetType("CROMS.Modules.UiTheme");
                MethodInfo m = t == null ? null : t.GetMethod("Polish", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Control) }, null);
                if (m != null) m.Invoke(null, new object[] { c });
            }
            catch { }
        }

        /// <summary>Build a form by type name, show it off-screen at w x h, optionally prepare it, save a PNG.</summary>
        private static Form Snap(string type, object[] args, string name, int w, int h, Action<Form> prep, string caption, params string[] tables)
        {
            try
            {
                Type t = typeof(Db).Assembly.GetType(type, true);
                var f = (Form)Activator.CreateInstance(t, NF, null, args ?? new object[0], null);
                f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-4000, -4000); f.ShowInTaskbar = false;
                f.Show();
                f.Size = new Size(w + (f.Width - f.ClientSize.Width), h + (f.Height - f.ClientSize.Height));
                Wd.Ignore.Add(f.Handle);
                AuditTest.Pump(8);
                if (prep != null) prep(f);
                AuditTest.Pump(8);
                SavePng(f, name);
                Add(name + ".png", caption, tables);
                return f;
            }
            catch (Exception ex) { Bad++; var e = ex.InnerException ?? ex; Console.WriteLine("  FAIL " + name + ": " + e.GetType().Name + ": " + e.Message); return null; }
        }
        private static void SnapClose(string type, object[] args, string name, int w, int h, Action<Form> prep, string caption, params string[] tables)
        {
            Form f = Snap(type, args, name, w, h, prep, caption, tables);
            try { if (f != null) { f.Hide(); f.Dispose(); } } catch { }
            AuditTest.Pump(2);
        }

        /// <summary>A module hosted by the real shell, saved at the shell's content size.</summary>
        private static Form ModuleShot(string key, string name, Action<Form> prep, string caption, params string[] tables)
        {
            try
            {
                Shell.ClientSize = new Size(1920, 1040); AuditTest.Pump(4);
                var f = (Form)Shell.GetType().GetMethod("GoToModule").Invoke(Shell, new object[] { key });
                AuditTest.Pump(14);
                if (prep != null) prep(f);
                AuditTest.Pump(10);
                SavePng(f, name);
                Add(name + ".png", caption, tables);
                return f;
            }
            catch (Exception ex) { Bad++; var e = ex.InnerException ?? ex; Console.WriteLine("  FAIL " + name + ": " + e.GetType().Name + ": " + e.Message); return null; }
        }

        // ------------------------------------------------------------------ birth / death (modal entry popups)
        /// <summary>Opens the entry popup of Birth / Death Registration on an EXISTING record and saves one PNG per tab.</summary>
        private static void EntryTabs(string formType, string loadMethod, int id, string prefix, string table, string caption)
        {
            Type t = typeof(Db).Assembly.GetType(formType, true);
            var form = (Form)Activator.CreateInstance(t, true);
            var host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(1500, 900) };
            form.TopLevel = false; form.FormBorderStyle = FormBorderStyle.None; form.Dock = DockStyle.Fill;
            host.Controls.Add(form);
            host.Show(); form.Show();
            AuditTest.Pump(6);
            t.GetMethod(loadMethod, NF).Invoke(form, new object[] { id });
            AuditTest.Pump(6);
            var tabs = (TabControl)t.GetField("tabControl", NF).GetValue(form);
            int tab = 0, wait = 0; bool selected = false;
            var timer = new Timer { Interval = 700 };
            timer.Tick += delegate
            {
                var dlg = (Form)t.GetField("_entryDialog", NF).GetValue(form);
                if (dlg == null || tab >= tabs.TabPages.Count) return;
                if (++wait < 3) return;
                try
                {
                    Wd.Ignore.Add(dlg.Handle);
                    if (!selected) { tabs.SelectedIndex = tab; selected = true; return; }
                    selected = false;
                    string text = tabs.TabPages[tab].Text.Trim();
                    string name = prefix + "_step" + (tab + 1);
                    SavePng(dlg, name);
                    Add(name + ".png", caption + " - step " + (tab + 1) + ": " + text, table);
                }
                catch (Exception ex) { Bad++; Console.WriteLine("  FAIL " + prefix + " tab " + tab + ": " + (ex.InnerException ?? ex).Message); }
                if (!selected && ++tab >= tabs.TabPages.Count) { timer.Stop(); dlg.Close(); }
            };
            timer.Start();
            t.GetMethod("ShowEntryView", NF).Invoke(form, null);
            timer.Dispose();
            host.Close();
        }

        private static void Birth()
        {
            string id = Id("SELECT id FROM births WHERE status='Registered' ORDER BY id LIMIT 1 OFFSET 2") ?? Id("SELECT id FROM births ORDER BY id LIMIT 1");
            if (id == null) throw new Exception("no birth records in this schema");
            EntryTabs("CROMS.Forms.BirthRegistrationForm", "LoadBirth", int.Parse(id), "births", "births", "Birth Registration (Form 102)");
            ModuleShot("birth", "births_list", null, "Birth Registration - list screen", "births");
        }

        private static void Death()
        {
            string id = Id("SELECT id FROM deaths ORDER BY id LIMIT 1 OFFSET 2") ?? Id("SELECT id FROM deaths ORDER BY id LIMIT 1");
            if (id == null) throw new Exception("no death records in this schema");
            EntryTabs("CROMS.Forms.DeathRegistrationForm", "LoadDeathCore", int.Parse(id), "deaths", "deaths", "Death Registration (Form 103)");
            ModuleShot("death", "deaths_list", null, "Death Registration - list screen", "deaths");
        }

        // ------------------------------------------------------------------ marriage windows
        private static void Marriage()
        {
            string lic = Id("SELECT id FROM marriage_licenses WHERE status='Issued' ORDER BY id LIMIT 1") ?? Id("SELECT id FROM marriage_licenses ORDER BY id LIMIT 1");
            string mar = Id("SELECT id FROM marriages WHERE license_id IS NOT NULL ORDER BY id LIMIT 1") ?? Id("SELECT id FROM marriages ORDER BY id LIMIT 1");
            string reg = Id("SELECT id FROM marriages WHERE status='Registered' ORDER BY id LIMIT 1") ?? mar;
            string[] st = { "applicants", "requirements", "consent_advice", "posting", "issue" };
            string[] stc = { "Applicants", "Requirements", "Consent and advice", "Posting period", "Issue" };
            for (int i = 0; i < 5; i++)
            {
                int k = i;
                SnapClose("CROMS.Forms.MarriageLicenseForm", new object[] { int.Parse(lic) }, "marriage_licenses_step" + (i + 1) + "_" + st[i], 1280, 820,
                    f => AuditTest.Call(f, "ShowStep", k), "Marriage License Application (Form 90) - " + stc[i], "marriage_licenses");
            }
            string[] t97 = { "contracting_parties", "parents", "licence_solemnization", "witnesses", "certification" };
            for (int i = 0; i < 5; i++)
            {
                int k = i;
                SnapClose("CROMS.Forms.MarriageEntryForm", new object[] { (int?)int.Parse(mar) }, "marriages_step" + (i + 1) + "_" + t97[i], 1320, 860,
                    f => AuditTest.Call(f, "ShowTab", k), "Marriage Registration (Form 97) - tab " + (i + 1), "marriages");
            }
            SnapClose("CROMS.Forms.MarriageRegistrationForm", new object[0], "marriage_desk", 1560, 960, null, "Marriage Registration desk", "marriage_licenses", "marriages");
            SnapClose("CROMS.Forms.MarriageRecordForm", new object[] { int.Parse(reg) }, "marriage_copies_record", 1180, 820, null, "Marriage record - copies requested", "marriage_copies");
            SnapClose("CROMS.Forms.PsaTransmittalForm", new object[] { null }, "psa_transmittal_batches_screen", 1240, 840, null, "PSA transmittal batch", "psa_transmittal_batches");
        }

        // ------------------------------------------------------------------ modules in the shell
        private static void Modules()
        {
            ModuleShot("petitions", "petitions_entry", f =>
            {
                string id = Id("SELECT id FROM petitions ORDER BY id LIMIT 1 OFFSET 1") ?? Id("SELECT id FROM petitions ORDER BY id LIMIT 1");
                if (id != null) AuditTest.Call(f, "LoadPetition", int.Parse(id));
            }, "Case Tracking - petition / case entry", "petitions");

            ModuleShot("certrequest", "certificate_requests_entry", f =>
            {
                Fill(f, "txtFirst", "Maria"); Fill(f, "txtMiddle", "Reyes"); Fill(f, "txtLast", "Santos"); Fill(f, "txtCopies", "2"); Fill(f, "txtPurpose", "Employment");
                Control[] cb = f.Controls.Find("cboCertType", true); if (cb.Length > 0 && ((ComboBox)cb[0]).Items.Count > 1) ((ComboBox)cb[0]).SelectedIndex = 1;
                cb = f.Controls.Find("cboRecordType", true); if (cb.Length > 0 && ((ComboBox)cb[0]).Items.Count > 1) ((ComboBox)cb[0]).SelectedIndex = 1;
            }, "Certificate Request - request entry (creates a transaction)", "certificate_requests", "transactions");

            ModuleShot("breqs", "psa_copy_requests_desk", f =>
            {
                string id = Id("SELECT id FROM psa_copy_requests ORDER BY id LIMIT 1");
                if (id != null) AuditTest.Call(f, "ShowDetail", int.Parse(id));
            }, "PSA Copies (BREQS) desk", "psa_copy_requests");
            string rq = Id("SELECT id FROM psa_copy_requests ORDER BY id LIMIT 1");
            if (rq != null)
            {
                Type asm = typeof(BreqsService);
                SnapClose("CROMS.Forms.BreqsRequestDialog", new object[] { BreqsService.Load(int.Parse(rq)), BreqsService.Settings }, "psa_copy_requests_dialog", 820, 860, null,
                          "PSA Copies - request dialog", "psa_copy_requests");
            }

            ModuleShot("fees", "payments_awaiting", f => { SelectTab(f, "_tabs", 0); string tx = Id("SELECT id FROM transactions WHERE status='ForPayment' ORDER BY id LIMIT 1"); if (tx != null) AuditTest.Call(f, "PreselectTransaction", long.Parse(tx)); }, "Fees & Payments - awaiting payment (records the Official Receipt)", "payments", "payment_items");
            ModuleShot("fees", "payments_walkin", f => SelectTab(f, "_tabs", 1), "Fees & Payments - walk-in payment (payer, purpose, fee lines)", "payments", "payment_items");
            ModuleShot("fees", "fees_schedule", f => SelectTab(f, "_tabs", 2), "Fees & Payments - fee schedule", "fees");

            ModuleShot("release", "releases_screen", f =>
            {
                string tx = Id("SELECT id FROM transactions WHERE status='ForRelease' ORDER BY id LIMIT 1");
                if (tx != null) AuditTest.Call(f, "PreselectTransaction", long.Parse(tx));
            }, "Release & Claim - claimant details and release", "releases", "claimant_id_uploads");

            ModuleShot("queue", "queue_tickets_management", null, "Queue Management - the staff list of kiosk tickets", "queue_tickets", "queue_ticket_services");
        }

        private static void SelectTab(Form f, string field, int index)
        {
            var tc = (TabControl)AuditTest.Fld(f, field);
            tc.SelectedIndex = index;
        }

        private static void Fill(Form f, string name, string text)
        {
            Control[] c = f.Controls.Find(name, true);
            if (c.Length > 0) c[0].Text = text;
        }

        // ------------------------------------------------------------------ settings screens (stand-alone forms)
        private static void SettingsScreens()
        {
            SnapClose("CROMS.Forms.UsersAuditForm", new object[0], "users_screen", 1500, 900, Polished, "Settings > Users & Access", "users");
            string uid = Id("SELECT id FROM users ORDER BY id LIMIT 1");
            SnapClose("CROMS.Forms.StaffBiodataForm", new object[0], "staff_biodata_screen", 1100, 820, Polished, "Settings > Users & Access > Staff Biodata", "staff_biodata");
            SnapClose("CROMS.Forms.WindowManagementForm", new object[0], "windows_screen", 1400, 800, Polished, "Settings > Window Management", "windows");
            SnapClose("CROMS.Forms.WindowAssignmentForm", new object[0], "window_service_assignments_screen", 1100, 780, Polished, "Sign-in window picker - services a window handles", "window_service_assignments");
            SnapClose("CROMS.Forms.OfficeAssetsForm", new object[0], "office_profile_screen", 1100, 820, Polished, "Settings > Forms & Templates > Office branding", "office_profile");

            string[] cats = { "Provinces", "Municipalities", "Barangays", "Hospitals", "Churches", "Occupations", "Religions", "Nationalities / Citizenship",
                              "Relationships", "Causes of Death", "Countries", "Birth Orders", "Type of Births", "Civil Statuses", "Residences" };
            string[] tbl = { "provinces", "municipalities", "barangays", "hospitals", "churches", "occupations", "religions", "nationalities",
                             "relationships", "causes_of_death", "countries", "birth_orders", "type_of_births", "civil_statuses", "residences" };
            try
            {
                Type t = typeof(Db).Assembly.GetType("CROMS.Forms.MasterFilesForm", true);
                var f = (Form)Activator.CreateInstance(t, true);
                f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-4000, -4000); f.ShowInTaskbar = false;
                f.Show(); f.Size = new Size(1500 + (f.Width - f.ClientSize.Width), 860 + (f.Height - f.ClientSize.Height));
                Wd.Ignore.Add(f.Handle); Polish(f); AuditTest.Pump(10);
                var cbo = (ComboBox)AuditTest.Fld(f, "cboCategory");
                for (int i = 0; i < cats.Length; i++)
                {
                    cbo.SelectedIndex = cbo.Items.IndexOf(cats[i]);
                    AuditTest.Pump(14); System.Threading.Thread.Sleep(350); AuditTest.Pump(6);
                    SavePng(f, "master_" + tbl[i]);
                    Add("master_" + tbl[i] + ".png", "Settings > Master Files - " + cats[i], tbl[i]);
                }
                f.Hide(); f.Dispose();
            }
            catch (Exception ex) { Bad++; Console.WriteLine("  FAIL master files: " + (ex.InnerException ?? ex).Message); }
        }
        private static readonly Action<Form> Polished = f => Polish(f);

        // ------------------------------------------------------------------ OCR: a real scan read by the real engine
        private static void Ocr()
        {
            // A scan is loaded only when CROMS_SHOTS_SCAN names one: the office's sample scans are real-looking
            // civil-registry documents and must not end up in a report by default.
            string scan = Environment.GetEnvironmentVariable("CROMS_SHOTS_SCAN");
            if (scan != null && !File.Exists(scan)) scan = null;
            Form f = null;
            try
            {
                Shell.ClientSize = new Size(1920, 1040); AuditTest.Pump(4);
                f = (Form)Shell.GetType().GetMethod("GoToModule").Invoke(Shell, new object[] { "ocr" });
                AuditTest.Pump(12);
                if (scan != null)
                {
                    Type t = f.GetType();
                    var bmp = DocumentAI.LoadImage(scan);
                    t.GetField("_image", NF).SetValue(f, bmp);
                    t.GetField("_scanBytes", NF).SetValue(f, File.ReadAllBytes(scan));
                    t.GetField("_sourceLabel", NF).SetValue(f, Path.GetFileName(scan));
                    var pb = (PictureBox)AuditTest.Fld(f, "pbScan");
                    pb.SizeMode = PictureBoxSizeMode.Zoom; pb.Image = bmp;
                    var task = (System.Threading.Tasks.Task)t.GetMethod("Analyze", NF).Invoke(f, null);
                    DateTime end = DateTime.Now.AddMinutes(4);
                    while (!task.IsCompleted && DateTime.Now < end) { Application.DoEvents(); System.Threading.Thread.Sleep(80); }
                }
                AuditTest.Pump(20);
                SavePng(f, "ocr_batch_screen");
                Add("ocr_batch_screen.png", scan != null ? "Document Processing - a scan read by the OCR engine, fields reviewed before saving"
                                                         : "Document Processing - load a scan, the OCR engine fills the review grid, the operator confirms and commits", "ocr_batch");
            }
            catch (Exception ex) { Bad++; Console.WriteLine("  FAIL ocr: " + (ex.InnerException ?? ex).Message); }
        }

        // ------------------------------------------------------------------ kiosk
        private static void Kiosk()
        {
            string[] codes = { "BIRTHREG", "MARRIAGE_REG", "DEATH", "MARRIAGE_APP", "LEGITIMATION", "LEGITIMATION_RA9255", "CTC", "BREQS", "CLAIM", "PETITION", "SUPPLEMENTAL_REPORT", "LEGAL_INSTRUMENTS", "COURT_ORDER" };
            _tmpWindow = Db.Insert("INSERT INTO windows (window_name, description, status, is_priority, current_operator, operator_name, last_heartbeat, display_order) VALUES ('ZZS Window', 'screenshots', 'Active', 0, @u, 'ZZS operator', NOW(), 951)",
                                   new MySql.Data.MySqlClient.MySqlParameter("@u", Session.User.Id));
            foreach (string c in codes) Db.Push("INSERT INTO window_service_assignments (window_id, service_code) VALUES (@w, @c)",
                                                new MySql.Data.MySqlClient.MySqlParameter("@w", _tmpWindow), new MySql.Data.MySqlClient.MySqlParameter("@c", c));
            string kexe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CROMS.Kiosk.exe");
            if (!File.Exists(kexe)) kexe = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\CROMS.Kiosk\bin\Debug\CROMS.Kiosk.exe"));
            if (!File.Exists(kexe)) throw new FileNotFoundException("CROMS.Kiosk.exe not found", kexe);
            Assembly k = Assembly.LoadFrom(kexe);
            Type core = k.GetType("CROMS.Kiosk.KioskCore", true);
            core.GetField("TestMode").SetValue(null, true);

            KioskSnap(k, "ServiceSelectForm", "kiosk_services", new string[0], null, "Kiosk - choose services (one queue_ticket_services row per tick)", "queue_ticket_services", "queue_tickets");
            KioskSnap(k, "DetailsPhotoForm", "kiosk_personal_info", new[] { "BIRTHREG" }, s =>
            {
                Set(s, "First", "Maria"); Set(s, "Middle", "Reyes"); Set(s, "Last", "Santos"); Set(s, "Contact", "09170001111");
            }, "Kiosk - personal information and photo (queue_tickets row)", "queue_tickets");
            KioskSnap(k, "CtcDetailsForm", "kiosk_ctc_request", new[] { "CTC" }, s =>
            {
                Set(s, "CtcDocumentType", "Birth"); Set(s, "CtcCopies", 2); Set(s, "CtcPurpose", "Employment");
                Set(s, "CtcRelationship", "Self"); Set(s, "CtcOwnerFirst", "Maria"); Set(s, "CtcOwnerMiddle", "Reyes"); Set(s, "CtcOwnerLast", "Santos");
                Set(s, "CtcEventProvince", "Cagayan");
            }, "Kiosk - Certified True Copy request (kiosk_ctc_intake row)", "kiosk_ctc_intake");
            KioskSnap(k, "BreqsDetailsForm", "kiosk_psa_copy", new[] { "BREQS" }, s =>
            {
                Set(s, "BreqsDocType", "Birth"); Set(s, "OwnerFirst", "Maria"); Set(s, "OwnerLast", "Santos");
            }, "Kiosk - PSA copy request (psa_copy_requests row)", "psa_copy_requests");
            KioskSnap(k, "ReviewRequestForm", "kiosk_review", new[] { "BIRTHREG", "CTC" }, s =>
            {
                Set(s, "First", "Maria"); Set(s, "Last", "Santos"); Set(s, "Contact", "09170001111");
            }, "Kiosk - review before the ticket is issued", "queue_tickets", "queue_ticket_services");
        }

        private static void Set(object o, string field, object value)
        {
            FieldInfo fi = o.GetType().GetField(field, NF);
            if (fi != null) fi.SetValue(o, value);
        }

        private static void KioskSnap(Assembly k, string type, string name, string[] selected, Action<object> fill, string caption, params string[] tables)
        {
            Form f = null;
            try
            {
                Type sessT = k.GetType("CROMS.Kiosk.KioskSession", true);
                object sess = Activator.CreateInstance(sessT);
                var sel = (List<string>)sessT.GetField("Selected").GetValue(sess);
                foreach (string c in selected) sel.Add(c);
                if (fill != null) fill(sess);
                Type ft = k.GetType("CROMS.Kiosk." + type, true);
                ConstructorInfo ci = ft.GetConstructors(NF).OrderBy(c => c.GetParameters().Length).First();
                f = (Form)ci.Invoke(ci.GetParameters().Length == 0 ? new object[0] : new object[] { sess });
                f.StartPosition = FormStartPosition.Manual; f.Location = new Point(0, 0);
                f.FormBorderStyle = FormBorderStyle.None; f.ShowInTaskbar = false; f.Opacity = 0;
                f.ClientSize = new Size(1920, 1080);
                f.Show(); Wd.Ignore.Add(f.Handle);
                f.ClientSize = new Size(1920, 1080); AuditTest.Pump(18);
                SavePng(f, name);
                Add(name + ".png", caption, tables);
            }
            catch (Exception ex) { Bad++; var e = ex.InnerException ?? ex; Console.WriteLine("  FAIL " + name + ": " + e.GetType().Name + ": " + e.Message); }
            finally
            {
                try
                {
                    if (f != null)
                    {
                        FieldInfo nav = f.GetType().GetField("_navigating", NF); if (nav != null) nav.SetValue(f, true);
                        f.Hide(); f.Dispose();
                    }
                }
                catch { }
                AuditTest.Pump(2);
            }
        }
    }
}
