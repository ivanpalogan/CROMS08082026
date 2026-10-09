using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;

namespace CROMS.MarriageTest
{
    /// <summary>
    /// Demo-environment indicator: the header badge and the window title must say DEMO ENVIRONMENT
    /// whenever the connected schema is not "croms", and must not appear on the live schema.
    /// Run with the app's own configuration (so against a demo schema, use the demo .config):
    ///   CROMS.MarriageTest.exe --envbadge
    /// Read-only: it builds the real MainForm but writes nothing.
    /// </summary>
    internal static class EnvBadgeTest
    {
        public static int Pass, Fail;
        private static void Check(string what, bool ok, string detail = "")
        {
            if (ok) Pass++; else Fail++;
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + what + (detail.Length > 0 ? "   -> " + detail : ""));
        }

        private static Label FindBadge(Control root)
        {
            foreach (Control c in root.Controls)
            {
                if (c is Label l && "demo-badge".Equals(l.Tag as string)) return l;
                Label inner = FindBadge(c);
                if (inner != null) return inner;
            }
            return null;
        }

        public static int Run()
        {
            Application.EnableVisualStyles();
            Console.WriteLine("Demo-environment indicator");

            // Pure rule: only an explicit, different, non-empty schema name is "demo".
            Check("croms is live (no badge)",            !ServerConfig.IsDemoName("croms"));
            Check("CROMS (case) is live",                !ServerConfig.IsDemoName("CROMS"));
            Check("' croms ' (spaces) is live",          !ServerConfig.IsDemoName(" croms "));
            Check("croms_demo is demo",                   ServerConfig.IsDemoName("croms_demo"));
            Check("croms_test2 is demo",                  ServerConfig.IsDemoName("croms_test2"));
            Check("empty name is not demo",              !ServerConfig.IsDemoName(""));
            Check("null name is not demo",               !ServerConfig.IsDemoName(null));

            string dbName = ServerConfig.DatabaseName;
            string actual = Db.Pull("SELECT DATABASE()").Rows[0][0].ToString();
            Check("configured database name = the schema actually connected", string.Equals(dbName, actual, StringComparison.OrdinalIgnoreCase), dbName + " / " + actual);
            Console.WriteLine("  (connected schema: " + actual + ", IsDemoEnvironment=" + ServerConfig.IsDemoEnvironment + ")");

            // The real shell.
            DataTable u = Db.Pull("SELECT id, username, full_name FROM users WHERE role='Admin' ORDER BY id LIMIT 1");
            Session.User = new CurrentUser { Id = Convert.ToInt32(u.Rows[0]["id"]), Username = u.Rows[0]["username"].ToString(), FullName = u.Rows[0]["full_name"].ToString(), Role = "Admin" };
            Form shell = null;
            try
            {
                shell = (Form)Activator.CreateInstance(typeof(Db).Assembly.GetType("CROMS.MainForm", true), true);
                shell.Opacity = 0; shell.ShowInTaskbar = false;
                Label badge = FindBadge(shell);
                bool demo = ServerConfig.IsDemoEnvironment;
                Check("badge " + (demo ? "present" : "absent") + " for schema '" + actual + "'", (badge != null) == demo);
                Check("window title " + (demo ? "carries" : "does not carry") + " DEMO ENVIRONMENT", shell.Text.Contains("DEMO ENVIRONMENT") == demo, shell.Text);
                if (demo && badge != null)
                {
                    Check("badge names the schema", badge.Text.Contains(actual), badge.Text);
                    Check("badge is the orange warning colour", badge.BackColor == Color.FromArgb(234, 88, 12));
                    Check("badge sits in the header bar (visible parent chain)", badge.Parent != null && badge.Parent.Parent != null);
                }
            }
            finally { if (shell != null) { shell.Dispose(); } }
            Console.WriteLine("PASSED " + Pass + "   FAILED " + Fail);
            return Fail;
        }
    }
}
