using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Forms;
using MySql.Data.MySqlClient;

namespace CROMS.MarriageTest
{
    /// <summary>
    /// Window A (civil registration) input-battery tests: every text box is filled with hostile / odd
    /// values, saved through the REAL form code, reloaded, and compared with what was typed. Rows are
    /// tagged ZZT and removed by tag + id (never audit rows by record id alone).
    /// Modes:  --areaA birth | death | marriage | petitions | all
    /// </summary>
    internal static class AreaA
    {
        internal const BindingFlags NF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
        internal static readonly string Tag = "ZZT" + DateTime.Now.ToString("HHmmss");
        internal static int Pass, Fail, Info;
        internal static long AuditStart;
        internal static DialogWatchdog Dog;

        internal static void Check(string name, bool ok, string detail = null)
        {
            if (ok) Pass++; else Fail++;
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + (detail == null ? "" : "   -> " + detail));
        }
        internal static void Note(string s) { Info++; Console.WriteLine("  NOTE  " + s); }
        internal static void Head(string s) { Console.WriteLine("\n[" + s + "]"); }

        internal static T G<T>(object o, string field)
        {
            Type t = o.GetType();
            FieldInfo f = null;
            while (t != null && f == null) { f = t.GetField(field, NF); t = t.BaseType; }
            if (f == null) throw new MissingFieldException(o.GetType().Name + "." + field);
            return (T)f.GetValue(o);
        }
        internal static object Call(object o, string method, params object[] args)
        {
            Type t = o.GetType();
            MethodInfo m = null;
            while (t != null && m == null)
            {
                foreach (MethodInfo c in t.GetMethods(NF | BindingFlags.DeclaredOnly))
                    if (c.Name == method && c.GetParameters().Length == args.Length) { m = c; break; }
                t = t.BaseType;
            }
            if (m == null) throw new MissingMethodException(o.GetType().Name + "." + method + "/" + args.Length);
            try { return m.Invoke(o, args); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }
        internal static void Set(object o, string field, object value)
        {
            Type t = o.GetType(); FieldInfo f = null;
            while (t != null && f == null) { f = t.GetField(field, NF); t = t.BaseType; }
            f.SetValue(o, value);
        }

        internal static IEnumerable<Control> Walk(Control c)
        {
            foreach (Control k in c.Controls) { yield return k; foreach (Control d in Walk(k)) yield return d; }
        }

        /// <summary>One line per input: name -> value. Used to compare "typed" with "reloaded".</summary>
        internal static SortedDictionary<string, string> Snapshot(Control root)
        {
            var d = new SortedDictionary<string, string>();
            var all = Walk(root).ToList();
            foreach (Control c in all)
            {
                if (string.IsNullOrEmpty(c.Name)) continue;
                // hidden placeholders that a lookup combo replaced carry no data
                if (c is TextBox && all.Any(x => x.Name == c.Name + "Lookup0")) continue;
                string v = null;
                if (c is TextBox tb) v = tb.Text;
                else if (c is ComboBox cb) v = cb.Text;
                else if (c is DateTimePicker dp) v = (dp.ShowCheckBox && !dp.Checked) ? "(unticked)" : (dp.Format == DateTimePickerFormat.Time ? dp.Value.ToString("HH:mm") : dp.Value.ToString("yyyy-MM-dd"));
                else if (c is CheckBox ck) v = ck.Checked.ToString();
                else if (c.GetType().Name == "ToggleSwitch") v = Convert.ToString(c.GetType().GetProperty("Checked").GetValue(c));
                if (v != null) d[c.Name] = v;
            }
            return d;
        }

        internal static List<string> Diff(IDictionary<string, string> typed, IDictionary<string, string> back)
        {
            var o = new List<string>();
            foreach (var kv in typed)
            {
                string b;
                if (!back.TryGetValue(kv.Key, out b)) { o.Add(kv.Key + ": missing after reload"); continue; }
                if (b != kv.Value) o.Add(kv.Key + ": typed [" + Esc(kv.Value) + "] back [" + Esc(b) + "]");
            }
            return o;
        }

        internal static string Esc(string s)
        {
            if (s == null) return "(null)";
            var sb = new StringBuilder();
            foreach (char ch in s)
            {
                if (ch == '\r') sb.Append("\\r"); else if (ch == '\n') sb.Append("\\n"); else if (ch == '\t') sb.Append("\\t");
                else if (ch < 32) sb.Append("\\x" + ((int)ch).ToString("X2")); else sb.Append(ch);
            }
            string r = sb.ToString();
            return r.Length > 70 ? r.Substring(0, 67) + "..." : r;
        }

        // Hostile / odd values. The apostrophe / backslash / percent ones prove nothing is built by string concat.
        internal static readonly string[] Battery =
        {
            "Maria-Jose O'Brien",
            "'; DROP TABLE births;--",
            "Peñablanca José Àéîõü",
            "<script>alert(1)</script> 100% _x_ [a] \\ \"q\"",
            "  Juan  Dela  Cruz  Jr.  ",
            "   ",
            "A\r\nB",
            "DE LA CRUZ JR.",
            "\U0001F600 emoji ᜀᜁ Tagalog",
            "1e5",
        };

        internal static string Fit(string v, int max)
        {
            if (max > 0 && max < 32767 && v.Length > max) v = v.Substring(0, max);
            return v;
        }

        internal static void DeleteAuditSince(string table, IEnumerable<long> ids, string tagLike)
        {
            string list = string.Join(",", ids.Distinct().DefaultIfEmpty(0));
            Db.Push("DELETE FROM audit_log WHERE id > @s AND table_name=@t AND record_id IN (" + list + ") AND details LIKE @l",
                new MySqlParameter("@s", AuditStart), new MySqlParameter("@t", table), new MySqlParameter("@l", tagLike));
        }

        internal static void Login()
        {
            DataTable u = Db.Pull("SELECT id, username, full_name FROM users WHERE role='Admin' ORDER BY id LIMIT 1");
            Session.User = new CurrentUser { Id = Convert.ToInt32(u.Rows[0]["id"]), Username = u.Rows[0]["username"].ToString(), FullName = u.Rows[0]["full_name"].ToString(), Role = "Admin" };
        }
        internal static void LoginStaff()
        {
            DataTable u = Db.Pull("SELECT id, username, full_name FROM users WHERE role='Staff' AND is_active=1 ORDER BY id LIMIT 1");
            if (u.Rows.Count == 0) { Session.User = new CurrentUser { Id = 0, Username = "zztstaff", FullName = "ZZT Staff", Role = "Staff" }; return; }
            Session.User = new CurrentUser { Id = Convert.ToInt32(u.Rows[0]["id"]), Username = u.Rows[0]["username"].ToString(), FullName = u.Rows[0]["full_name"].ToString(), Role = "Staff" };
        }

        public static int Run(string which)
        {
            Console.WriteLine("CROMS window A input battery  -  tag " + Tag + "  -  " + which);
            Application.EnableVisualStyles();
            Login();
            AuditStart = Convert.ToInt64(Db.Pull("SELECT IFNULL(MAX(id),0) FROM audit_log").Rows[0][0]);
            Dog = new DialogWatchdog();
            Dog.Rules["Form"] = f => { };
            Dog.Start();
            try
            {
                if (which == "birth" || which == "all") AreaABirth.Run();
                if (which == "death" || which == "all") AreaADeath.Run();
                if (which == "marriage" || which == "all") AreaAMarriage.Run();
                if (which == "petitions" || which == "all") AreaAPetitions.Run();
            }
            catch (Exception ex) { Fail++; Console.WriteLine("CRASH: " + ex); }
            finally { Dog.Stop(); }
            Console.WriteLine("\nPASSED " + Pass + "   FAILED " + Fail + "   NOTES " + Info);
            return Fail;
        }
    }
}
