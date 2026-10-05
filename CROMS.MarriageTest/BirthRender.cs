using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using CROMS.Forms;

namespace CROMS.MarriageTest
{
    /// <summary>
    /// Renders the real Birth Registration entry popup, every tab, to PNG so layout changes can be
    /// LOOKED AT (the popup is modal, so a UI timer drives it from inside ShowDialog).
    /// Read-only: nothing is saved.
    /// </summary>
    internal static class BirthRender
    {
        private const BindingFlags NF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public static void Run(string dir)
        {
            Directory.CreateDirectory(dir);
            Application.EnableVisualStyles();
            var form = new BirthRegistrationForm();
            var host = new Form
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-4000, -4000),
                ShowInTaskbar = false,
                ClientSize = new Size(1500, 900)
            };
            form.TopLevel = false; form.FormBorderStyle = FormBorderStyle.None; form.Dock = DockStyle.Fill;
            host.Controls.Add(form);
            host.Show(); form.Show();
            for (int i = 0; i < 6; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

            Type t = typeof(BirthRegistrationForm);
            var tabs = (TabControl)t.GetField("tabControl", NF).GetValue(form);
            int tab = 0, wait = 0; bool selected = false;
            var timer = new Timer { Interval = 700 };
            timer.Tick += delegate
            {
                var dlg = (Form)t.GetField("_entryDialog", NF).GetValue(form);
                if (dlg == null || tab >= tabs.TabPages.Count) return;
                if (++wait < 3) return;           // let Shown/ApplyEntryTier settle
                try
                {
                    if (!selected) { tabs.SelectedIndex = tab; selected = true; return; }   // select now, capture on the next tick
                    selected = false;
                    using (var bmp = new Bitmap(dlg.Width, dlg.Height))
                    {
                        dlg.DrawToBitmap(bmp, new Rectangle(0, 0, dlg.Width, dlg.Height));
                        string f = Path.Combine(dir, "birth_tab" + tab + ".png");
                        bmp.Save(f, System.Drawing.Imaging.ImageFormat.Png);
                        Console.WriteLine("  ok  " + Path.GetFileName(f) + "  (" + tabs.TabPages[tab].Text + ")");
                    }
                }
                catch (Exception ex) { Console.WriteLine("  FAIL tab " + tab + ": " + (ex.InnerException ?? ex).Message); }
                if (!selected && ++tab >= tabs.TabPages.Count) { timer.Stop(); dlg.Close(); }
            };
            timer.Start();
            t.GetMethod("ShowEntryView", NF).Invoke(form, null);
            timer.Dispose();
            host.Close();
        }
    }
}
