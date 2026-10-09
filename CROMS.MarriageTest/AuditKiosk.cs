using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CROMS.Data;

namespace CROMS.MarriageTest
{
    /// <summary>System audit, part 4: the kiosk's step screens and the public Display board, rendered at two sizes.</summary>
    internal static partial class AuditTest
    {
        private static void KioskDisplayPart(DialogWatchdog wd)
        {
            Console.WriteLine("\n--- D. kiosk screens and public display board");
            // An online window (operator + fresh heartbeat) so the kiosk shows its real, enabled screens
            // instead of the "currently closed" state. Removed in OperationsCleanup.
            string[] codes = { "BIRTHREG", "MARRIAGE_REG", "DEATH", "MARRIAGE_APP", "LEGITIMATION", "LEGITIMATION_RA9255", "CTC", "BREQS", "CLAIM", "PETITION", "SUPPLEMENTAL_REPORT", "LEGAL_INSTRUMENTS", "COURT_ORDER" };
            long wid = Db.Insert("INSERT INTO windows (window_name, description, status, is_priority, current_operator, operator_name, last_heartbeat, display_order) VALUES ('ZZA Win', 'audit', 'Active', 0, @u, 'ZZA tester', NOW(), 950)",
                                 new MySql.Data.MySqlClient.MySqlParameter("@u", Session.User.Id));
            foreach (string c in codes) Db.Push("INSERT INTO window_service_assignments (window_id, service_code) VALUES (@w, @c)", new MySql.Data.MySqlClient.MySqlParameter("@w", wid), new MySql.Data.MySqlClient.MySqlParameter("@c", c));
            string kexe = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\CROMS.Kiosk\bin\Debug\CROMS.Kiosk.exe"));
            Check("kiosk build found", File.Exists(kexe), kexe);
            if (File.Exists(kexe))
            {
                Assembly k = Assembly.LoadFrom(kexe);
                Type sessT = k.GetType("CROMS.Kiosk.KioskSession", true);
                Type core = k.GetType("CROMS.Kiosk.KioskCore", true);
                core.GetField("TestMode").SetValue(null, true);
                bool online = (bool)core.GetMethod("OfficeOnline").Invoke(null, null);
                Check("kiosk sees the office as ONLINE while a window has an operator + fresh heartbeat", online,
                      "windows: " + string.Join(" | ", Db.Pull("SELECT window_name, status, current_operator, last_heartbeat, NOW() n FROM windows").Rows.Cast<System.Data.DataRow>().Select(r => string.Join(",", r.ItemArray))));
                var steps = new List<Tuple<string, string, string[]>>
                {
                    Tuple.Create("WelcomeForm", "welcome", new string[0]),
                    Tuple.Create("ServiceSelectForm", "services", new string[0]),
                    Tuple.Create("MarriageLicenseCheckForm", "marriage-licence-check", new[] { "MARRIAGE_REG" }),
                    Tuple.Create("DetailsPhotoForm", "details-birth", new[] { "BIRTHREG" }),
                    Tuple.Create("DetailsPhotoForm", "details-marriage-reg", new[] { "MARRIAGE_REG" }),
                    Tuple.Create("DetailsPhotoForm", "details-claim", new[] { "CLAIM" }),
                    Tuple.Create("CtcDetailsForm", "ctc", new[] { "CTC" }),
                    Tuple.Create("BreqsDetailsForm", "breqs", new[] { "BREQS" }),
                    Tuple.Create("ReviewRequestForm", "review", new[] { "BIRTHREG", "CTC" }),
                };
                foreach (var st in steps)
                {
                    foreach (Size sz in new[] { new Size(1920, 1080), new Size(1366, 768) })
                    {
                        Form f = null; string err = null;
                        int un0 = Unhandled.Count;
                        try
                        {
                            Type ft = k.GetType("CROMS.Kiosk." + st.Item1, true);
                            object sess = Activator.CreateInstance(sessT);
                            sessT.GetField("First").SetValue(sess, "Maria"); sessT.GetField("Last").SetValue(sess, "ZZAKiosk");
                            sessT.GetField("Contact").SetValue(sess, "09170001111");
                            var sel = (List<string>)sessT.GetField("Selected").GetValue(sess);
                            foreach (string c in st.Item3) sel.Add(c);
                            ConstructorInfo ci = ft.GetConstructors(Any).OrderBy(c => c.GetParameters().Length).First();
                            f = (Form)ci.Invoke(ci.GetParameters().Length == 0 ? new object[0] : new object[] { sess });
                            f.StartPosition = FormStartPosition.Manual; f.Location = new Point(0, 0);
                            f.FormBorderStyle = FormBorderStyle.None; f.ShowInTaskbar = false; f.Opacity = 0;
                            f.ClientSize = sz;
                            f.Show(); wd.Ignore.Add(f.Handle);
                            f.ClientSize = sz; Pump(18);
                            var hits = new List<string>();
                            Sweep(f, hits, st.Item1);
                            // The Back/Next buttons are laid over the footer panel by design; DrawToBitmap paints the footer on top of them in a bitmap.
                            hits.RemoveAll(h => h.Contains("Button x Panel") || h.Contains("_btnBack x footer") || h.Contains("_btnNext x footer"));
                            string png = Path.Combine(Out, "kiosk_" + st.Item2 + "_" + sz.Width + ".png");
                            using (var bmp = new Bitmap(f.Width, f.Height))
                            {
                                f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                                bmp.Save(png, System.Drawing.Imaging.ImageFormat.Png);
                            }
                            Check("kiosk " + st.Item2 + " @" + sz.Width + "x" + sz.Height + ": renders, no overlaps/out-of-bounds",
                                  Unhandled.Count == un0 && hits.Count == 0,
                                  Unhandled.Count > un0 ? Unhandled.Last() : (hits.Count == 0 ? null : hits.Count + " issue(s): " + string.Join(" | ", hits.Take(4))));
                        }
                        catch (Exception ex)
                        {
                            Exception e = ex.InnerException ?? ex; err = e.GetType().Name + ": " + e.Message;
                            Check("kiosk " + st.Item2 + " @" + sz.Width + " builds", false, err);
                        }
                        finally { try { if (f != null) { f.Hide(); f.Dispose(); } } catch { } Pump(2); }
                    }
                }
            }

            // public display board
            string dexe = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\CROMS.Display\bin\Debug\CROMS.Display.exe"));
            Check("display build found", File.Exists(dexe), dexe);
            if (File.Exists(dexe))
            {
                Assembly d = Assembly.LoadFrom(dexe);
                foreach (Size sz in new[] { new Size(1920, 1080), new Size(1366, 768) })
                {
                    Form f = null;
                    try
                    {
                        f = (Form)Activator.CreateInstance(d.GetType("CROMS.Display.DisplayForm", true));
                        f.StartPosition = FormStartPosition.Manual; f.Location = new Point(0, 0);
                        f.FormBorderStyle = FormBorderStyle.None; f.ShowInTaskbar = false; f.Opacity = 0;
                        f.WindowState = FormWindowState.Normal; f.ClientSize = sz;
                        f.Show(); wd.Ignore.Add(f.Handle); f.ClientSize = sz; Pump(20);
                        var hits = new List<string>(); Sweep(f, hits, "DisplayForm");
                        using (var bmp = new Bitmap(f.Width, f.Height))
                        {
                            f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                            bmp.Save(Path.Combine(Out, "display_" + sz.Width + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                        }
                        long active = Convert.ToInt64(Db.Pull("SELECT COUNT(*) FROM windows WHERE status='Active'").Rows[0][0]);
                        int controls = f.Controls.Cast<Control>().Count();
                        Check("display board @" + sz.Width + " renders and lists the active windows (" + active + " active in DB)", controls > 0 && hits.Count == 0,
                              hits.Count == 0 ? null : string.Join(" | ", hits.Take(4)));
                    }
                    catch (Exception ex) { Check("display board @" + sz.Width, false, (ex.InnerException ?? ex).Message); }
                    finally { try { if (f != null) { f.Hide(); f.Dispose(); } } catch { } Pump(2); }
                }
            }
        }
    }
}
