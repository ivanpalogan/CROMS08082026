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
    /// System audit, part 1: every sidebar module is created by the REAL shell (MainForm), shown at
    /// 1920x1040 and 1366x728, rendered to PNG, and swept for hosting exceptions, overlapping
    /// siblings and controls pushed outside a non-scrolling parent. Part 2: role gating (Admin / Staff
    /// / unknown). Nothing is written to the database. Cancel-only dialog watchdog, so nothing can
    /// reach a printer.
    /// </summary>
    internal static partial class AuditTest
    {
        public static int Pass, Fail;
        public static readonly List<string> Report = new List<string>();
        public static string Out;

        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static void Check(string name, bool ok, string detail = null)
        {
            if (ok) Pass++; else Fail++;
            string line = (ok ? "  PASS  " : "  FAIL  ") + name + (detail == null ? "" : "   -> " + detail);
            Console.WriteLine(line); Report.Add(line);
        }
        public static void Note(string s) { Console.WriteLine("  NOTE  " + s); Report.Add("  NOTE  " + s); }

        public static object Fld(object o, string name)
        {
            for (Type t = o.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, Any | BindingFlags.DeclaredOnly);
                if (f != null) return f.GetValue(o);
            }
            throw new MissingFieldException(o.GetType().Name, name);
        }
        public static object Call(object o, string method, params object[] args)
        {
            MethodInfo m = null;
            for (Type t = o.GetType(); t != null && m == null; t = t.BaseType)
                m = t.GetMethods(Any | BindingFlags.DeclaredOnly).FirstOrDefault(x => x.Name == method && x.GetParameters().Length == args.Length);
            if (m == null) throw new MissingMethodException(o.GetType().Name, method);
            try { return m.Invoke(o, args); } catch (TargetInvocationException ex) { throw ex.InnerException; }
        }
        public static void Pump(int rounds = 6)
        {
            for (int i = 0; i < rounds; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(25); }
        }

        public static readonly List<string> Unhandled = new List<string>();

        public static int Run(string outDir)
        {
            Out = outDir; Directory.CreateDirectory(outDir);
            Console.WriteLine("CROMS system audit - modules, layout, roles");
            Application.EnableVisualStyles();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => { lock (Unhandled) Unhandled.Add(e.Exception.GetType().Name + ": " + e.Exception.Message); };

            var wd = new DialogWatchdog();
            wd.Start();
            Form shell = null;
            try
            {
                Login("Admin");
                Type mt = typeof(Db).Assembly.GetType("CROMS.MainForm", true);
                shell = (Form)Activator.CreateInstance(mt, true);
                shell.Opacity = 0; shell.ShowInTaskbar = false; shell.StartPosition = FormStartPosition.Manual; shell.WindowState = FormWindowState.Normal;
                shell.Location = new Point(0, 0);
                shell.Show(); wd.Ignore.Add(shell.Handle);
                Pump();
                if (OnlyFilter == null) { ModulesPart(shell, wd); RolesPart(shell, wd, mt); }
                OperationsPart(shell, wd);
                if (OnlyFilter == null) KioskDisplayPart(wd);
            }
            catch (Exception ex) { Fail++; Console.WriteLine("CRASH: " + ex); Report.Add("CRASH: " + ex); }
            finally
            {
                try { OperationsCleanup(); } catch (Exception ex) { Console.WriteLine("cleanup failed: " + ex.Message); }
                wd.Stop();
                lock (Unhandled) foreach (string u in Unhandled) Note("unhandled UI exception: " + u);
                Console.WriteLine("PASSED " + Pass + "   FAILED " + Fail);
                File.WriteAllLines(Path.Combine(outDir, "audit_log.txt"), Report.Concat(new[] { "PASSED " + Pass + "   FAILED " + Fail }).ToArray());
            }
            return Fail;
        }

        public static void Login(string role)
        {
            DataTable u = Db.Pull("SELECT id, username, full_name FROM users WHERE role='Admin' ORDER BY id LIMIT 1");
            Session.User = new CurrentUser
            {
                Id = Convert.ToInt32(u.Rows[0]["id"]), Username = u.Rows[0]["username"].ToString(),
                FullName = u.Rows[0]["full_name"].ToString(), Role = role
            };
        }

        // ------------------------------------------------------------------ part 1
        private static readonly Size[] Sizes = { new Size(1920, 1040), new Size(1366, 728) };

        private static void ModulesPart(Form shell, DialogWatchdog wd)
        {
            Console.WriteLine("\n--- A. every module hosted by the real shell, two screen sizes");
            Type regT = typeof(Db).Assembly.GetType("CROMS.Modules.ModuleRegistry", true);
            var all = ((System.Collections.IEnumerable)regT.GetProperty("All").GetValue(null)).Cast<object>().ToList();
            MethodInfo go = shell.GetType().GetMethod("GoToModule");
            foreach (object mi in all)
            {
                string key = (string)mi.GetType().GetProperty("Key").GetValue(mi);
                string title = (string)mi.GetType().GetProperty("Title").GetValue(mi);
                Form form = null; string err = null;
                int mark = wd.Count;
                try
                {
                    shell.ClientSize = Sizes[0]; Pump(4);
                    form = (Form)go.Invoke(shell, new object[] { key });
                    Pump(10);
                }
                catch (Exception ex) { err = (ex.InnerException ?? ex).GetType().Name + ": " + (ex.InnerException ?? ex).Message; }
                Check("module '" + title + "' (" + key + ") opens in the shell", form != null && err == null, err);
                if (form == null) continue;
                foreach (string l in wd.Since(mark)) Note(key + " dialog during open: " + l);

                foreach (Size sz in Sizes)
                {
                    shell.ClientSize = sz; Pump(10);
                    var hits = new List<string>();
                    Sweep(form, hits, form.Name == "" ? form.GetType().Name : form.GetType().Name);
                    string png = Path.Combine(Out, key + "_" + sz.Width + ".png");
                    try
                    {
                        using (var bmp = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height)))
                        {
                            form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                            bmp.Save(png, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    catch (Exception ex) { Note(key + " render failed at " + sz.Width + ": " + ex.Message); }
                    Check("  " + key + " layout @" + sz.Width + "x" + sz.Height + ": no overlaps / no out-of-bounds",
                          hits.Count == 0, hits.Count == 0 ? null : hits.Count + " issue(s): " + string.Join(" | ", hits.Take(5)));
                }
            }
            shell.ClientSize = Sizes[0]; Pump(4);
        }

        /// <summary>Sibling-overlap sweep + out-of-bounds sweep over every visible container.</summary>
        private static void Sweep(Control root, List<string> hits, string path)
        {
            if (!root.Visible) return;
            var kids = root.Controls.Cast<Control>().Where(c => c.Visible && c.Width > 3 && c.Height > 3).ToList();
            bool scroll = root is ScrollableControl sc && sc.AutoScroll;
            bool skipLayout = root is TabControl || root is SplitContainer || root is DataGridView || root is ComboBox || root is NumericUpDown;
            if (!skipLayout)
            {
                for (int i = 0; i < kids.Count; i++)
                    for (int j = i + 1; j < kids.Count; j++)
                    {
                        Control a = kids[i], b = kids[j];
                        Rectangle r = Rectangle.Intersect(a.Bounds, b.Bounds);
                        int small = Math.Min(a.Width * a.Height, b.Width * b.Height);
                        if (r.Width * r.Height < 40 || r.Width * r.Height < 0.25 * small) continue;   // a few px of caption/input slack is not a collision
                        // A transparent label sitting on a panel / picture is a design pattern, not a collision.
                        if (a is PictureBox || b is PictureBox) continue;
                        // A disabled label laid over a text box is its placeholder / cue text, by design.
                        if ((a is Label && !a.Enabled && b is TextBox) || (b is Label && !b.Enabled && a is TextBox)) continue;
                        if (a.GetType().Name.Contains("Glow") || b.GetType().Name.Contains("Glow")) continue;
                        hits.Add(path + ": " + Name(a) + " x " + Name(b) + " (" + r.Width + "x" + r.Height + ")");
                    }
                if (!scroll && !(root is Form))
                    foreach (Control c in kids)
                        if (c.Dock == DockStyle.None && (c.Right > root.ClientSize.Width + 8 || c.Bottom > root.ClientSize.Height + 8) && root.ClientSize.Width > 20 && root.ClientSize.Height > 20)
                            hits.Add(path + ": " + Name(c) + " outside " + Name(root) + " (" + c.Right + "," + c.Bottom + " in " + root.ClientSize.Width + "x" + root.ClientSize.Height + ")");
            }
            foreach (Control c in root.Controls) Sweep(c, hits, path + "/" + Name(c));
        }
        private static string Name(Control c) { return string.IsNullOrEmpty(c.Name) ? c.GetType().Name : c.Name; }

        // ------------------------------------------------------------------ part 2
        private static void RolesPart(Form shell, DialogWatchdog wd, Type mainT)
        {
            Console.WriteLine("\n--- B. roles: Admin / Staff / unknown");
            MethodInfo allowed = mainT.GetMethod("AllowedKeys", BindingFlags.NonPublic | BindingFlags.Static);
            string[] adminOnly = { "masterfiles", "certtemplates", "users", "settings" };
            string[] operational = { "dashboard", "queue", "transactions", "certrequest", "release", "breqs", "birth", "marriage", "death", "petitions", "archive", "ocr", "fees", "reports" };

            var admin = allowed.Invoke(null, new object[] { "Admin" });
            Check("Admin has unrestricted access", admin == null);
            foreach (string role in new[] { "Staff", "Registrar", "", null })
            {
                var set = (HashSet<string>)allowed.Invoke(null, new object[] { role });
                string r = role ?? "(null)";
                Check("role '" + r + "' gets the operational set", set != null && operational.All(set.Contains));
                Check("role '" + r + "' is locked out of Settings / Master Files / Users / Templates", set != null && adminOnly.All(k => !set.Contains(k)),
                      set == null ? "unrestricted!" : string.Join(",", adminOnly.Where(set.Contains)));
            }

            // the door itself: Staff cannot open Settings through GoToModule
            Login("Staff");
            try
            {
                int m0 = wd.Count;
                string activeBefore = (string)Fld(shell, "_activeKey");
                MethodInfo go = shell.GetType().GetMethod("GoToModule");
                go.Invoke(shell, new object[] { "dashboard" }); Pump(6);
                m0 = wd.Count;
                go.Invoke(shell, new object[] { "settings" }); Pump(8);
                string active = (string)Fld(shell, "_activeKey");
                Check("Staff GoToModule('settings') is refused at the door", active != "settings" && wd.Saw(m0, "Administrator only"), "active=" + active);
                m0 = wd.Count;
                go.Invoke(shell, new object[] { "users" }); Pump(6);
                Check("Staff GoToModule('users') is refused", (string)Fld(shell, "_activeKey") != "users");
                m0 = wd.Count;
                go.Invoke(shell, new object[] { "masterfiles" }); Pump(6);
                Check("Staff GoToModule('masterfiles') is refused", (string)Fld(shell, "_activeKey") != "masterfiles");
                go.Invoke(shell, new object[] { "archive" }); Pump(8);
                Check("Staff CAN open Records Archive (search)", (string)Fld(shell, "_activeKey") == "archive");
                Check("MarriageService.CanBypass true for Staff", MarriageService.CanBypass);
                Check("Session.IsAdmin false for Staff", !Session.IsAdmin);
                try { PaymentService.UpdateFee("CTC-BIRTH", 80m, true, Session.User.Id); Check("Staff cannot change the fee schedule", false, "no exception"); }
                catch (UnauthorizedAccessException) { Check("Staff cannot change the fee schedule", true); }
            }
            finally { Login("Admin"); }

            // the sidebar: which buttons exist per role
            Login("Staff");
            try
            {
                Type mt = shell.GetType();
                var shell2 = (Form)Activator.CreateInstance(mt, true);
                shell2.Opacity = 0; shell2.ShowInTaskbar = false; wd.Ignore.Add(shell2.Handle);
                shell2.Show(); Pump(10);
                var btns = (System.Collections.IDictionary)Fld(shell2, "_navButtons");
                var visible = new List<string>(); var hidden = new List<string>();
                foreach (System.Collections.DictionaryEntry e in btns) (((Button)e.Value).Visible ? visible : hidden).Add((string)e.Key);
                Note("Staff sidebar visible: " + string.Join(",", visible.OrderBy(x => x)));
                Check("Staff sidebar hides Settings", !visible.Contains("settings"), string.Join(",", visible));
                Check("Staff sidebar still shows the daily work screens", new[] { "queue", "certrequest", "release", "fees", "birth", "marriage", "death", "petitions", "archive", "ocr", "reports" }.All(visible.Contains),
                      "missing: " + string.Join(",", new[] { "queue", "certrequest", "release", "fees", "birth", "marriage", "death", "petitions", "archive", "ocr", "reports" }.Where(k => !visible.Contains(k))));
                shell2.Hide();
                typeof(Form).GetMethod("Close").Invoke(shell2, null);
            }
            finally { Login("Admin"); }
        }
    }
}
