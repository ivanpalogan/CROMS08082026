using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Forms;
using MySql.Data.MySqlClient;
using static CROMS.MarriageTest.AreaA;

namespace CROMS.MarriageTest
{
    /// <summary>Marriage Form 97 + Form 90 + desk + rules input battery.</summary>
    internal static class AreaAMarriage
    {
        private static readonly List<int> MarriageIds = new List<int>();
        private static readonly List<int> LicenseIds = new List<int>();
        private static long HistStart = long.MaxValue;

        // ------------------------------------------------------------------ tree snapshot (controls here have no names)
        private static string Caption(Control c)
        {
            Control p = c.Parent;
            for (int depth = 0; p != null && depth < 4; depth++, p = p.Parent)
            {
                foreach (Control k in p.Controls)
                    if (k is Label l && !string.IsNullOrWhiteSpace(l.Text) && l.Text.Length < 70) return l.Text.Replace("\r", "").Replace("\n", " ").Trim();
            }
            return "?";
        }

        private static List<KeyValuePair<string, string>> SnapTree(Control root)
        {
            var o = new List<KeyValuePair<string, string>>();
            int i = 0;
            foreach (Control c in Walk(root))
            {
                string v = null;
                if (c is TextBox tb && !tb.ReadOnly) v = tb.Text;
                else if (c is ComboBox cb) v = cb.Text;
                else if (c is DateTimePicker dp) v = dp.ShowCheckBox && !dp.Checked ? "(unticked)" : dp.Value.ToString("yyyy-MM-dd");
                else if (c is CheckBox ck) v = ck.Checked.ToString();
                else if (c is RadioButton rb) v = rb.Checked.ToString();
                if (v == null) continue;
                o.Add(new KeyValuePair<string, string>((i++) + ":" + c.GetType().Name.Substring(0, 3) + ":" + Caption(c), v));
            }
            return o;
        }

        private static List<string> DiffTree(List<KeyValuePair<string, string>> a, List<KeyValuePair<string, string>> b)
        {
            var o = new List<string>();
            if (a.Count != b.Count) { o.Add("control count differs: " + a.Count + " vs " + b.Count); return o; }
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].Value == b[i].Value) continue;
                string t = a[i].Value, bk = b[i].Value;
                if (t.Trim() == bk || (t.Trim() == "" && bk == "")) continue;      // Trim on save is intended
                o.Add(a[i].Key + ": typed [" + Esc(t) + "] back [" + Esc(bk) + "]");
            }
            return o;
        }

        private static void FillBattery(Control root, string v)
        {
            foreach (Control c in Walk(root))
            {
                if (c is TextBox tb && !tb.ReadOnly && tb.Parent != null) tb.Text = Fit(v, tb.MaxLength);
                else if (c is ComboBox cb)
                {
                    if (cb.DropDownStyle == ComboBoxStyle.DropDown) cb.Text = Fit(v, cb.MaxLength);
                }
            }
        }

        private static T Fld<T>(object o, string name) { return G<T>(o, name); }
        private static Form NewLic(int? id)
        {
            Type t = typeof(MarriageEntryForm).Assembly.GetType("CROMS.Forms.MarriageLicenseForm", true);
            return (Form)Activator.CreateInstance(t, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new object[] { id }, null);
        }

        // ------------------------------------------------------------------ Form 97
        public static void Run()
        {
            HistStart = Convert.ToInt64(Db.Pull("SELECT IFNULL(MAX(id),0) FROM marriage_case_history").Rows[0][0]);
            try
            {
                Cleanup();
                Form97Battery();
                Form97Rules();
                Form90Battery();
                RoleChecks();
                DeskSearch();
                DoubleSave();
            }
            catch (Exception ex) { Fail++; Console.WriteLine("MARRIAGE CRASH: " + ex); }
            finally
            {
                Cleanup();
                int left = Convert.ToInt32(Db.Pull("SELECT (SELECT COUNT(*) FROM marriages WHERE husband_last_name LIKE 'ZZT%' OR wife_last_name LIKE 'ZZT%') + (SELECT COUNT(*) FROM marriage_licenses WHERE husband_last_name LIKE 'ZZT%' OR wife_last_name LIKE 'ZZT%')").Rows[0][0]);
                Check("marriage: zero strays after cleanup", left == 0, left + " left");
            }
        }

        private static void Form97Battery()
        {
            Head("M1 Form 97: hostile text in every box -> Save draft -> reopen -> compare");
            int n = 0;
            foreach (string v in AreaA.Battery)
            {
                n++;
                var f = new MarriageEntryForm(null);
                f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-4000, -4000); f.ShowInTaskbar = false;
                object h = Fld<object>(f, "_h"), w = Fld<object>(f, "_w");
                FillBattery(f, v);
                // minimal valid couple
                foreach (var pr in new[] { Tuple.Create(h, "H"), Tuple.Create(w, "W") })
                {
                    Fld<TextBox>(pr.Item1, "First").Text = string.IsNullOrWhiteSpace(v) ? "Blankcase" : Fit(v, 50);
                    Fld<TextBox>(pr.Item1, "Last").Text = Tag + pr.Item2 + n;
                    Fld<DateTimePicker>(pr.Item1, "Dob").Value = DateTime.Today.AddYears(-30);
                    var civ = Fld<ComboBox>(pr.Item1, "Civil"); if (civ.Items.Count > 0) civ.SelectedIndex = 0;
                }
                Fld<ComboBox>(h, "Sex").SelectedItem = "Male"; Fld<ComboBox>(w, "Sex").SelectedItem = "Female";
                var typed = SnapTree(f);
                bool saved = false; int mark = Dog.Count;
                try { saved = (bool)Call(f, "Save", "Draft"); }
                catch (Exception ex) { Check("form97 battery " + n + " save threw", false, ex.GetType().Name + ": " + ex.Message); }
                int? id = Fld<int?>(f, "_id");
                if (!saved || !id.HasValue)
                {
                    Check("form97 battery " + n + " [" + Esc(v) + "] saves", false, "dialogs: " + string.Join(" || ", Dog.Since(mark)));
                    f.Dispose(); continue;
                }
                MarriageIds.Add(id.Value);
                f.Dispose();
                var g = new MarriageEntryForm(id.Value);
                var back = SnapTree(g);
                var diffs = DiffTree(typed, back);
                Check("form97 battery " + n + " [" + Esc(v) + "] reopens unchanged", diffs.Count == 0, diffs.Count + " differences");
                foreach (string d in diffs.Take(16)) Console.WriteLine("      " + d);
                DataRow r = Db.Pull("SELECT husband_first_name, remarks, solemnizer FROM marriages WHERE id=" + id.Value).Rows[0];
                Console.WriteLine("      DB husband_first_name [" + Esc(Convert.ToString(r[0])) + "] remarks [" + Esc(Convert.ToString(r[1])) + "]");
                g.Dispose();
            }
        }

        // ------------------------------------------------------------------ rules
        private static void Form97Rules()
        {
            Head("M2 Form 97 rules: under 18, DOB after wedding, future wedding, same person, same sex, old age");
            Type t = typeof(MarriageRules);
            // Build a minimal couple via the real rule engine
            var husband = new Party("Husband") { First = "Ramon", Last = Tag + "RH", Dob = DateTime.Today.AddYears(-30), CivilStatus = "Single", Sex = "Male" };
            var wife = new Party("Wife") { First = "Rosa", Last = Tag + "RW", Dob = DateTime.Today.AddYears(-28), CivilStatus = "Single", Sex = "Female" };
            Console.WriteLine("    MarriageRules methods: " + string.Join(", ", t.GetMethods().Where(m => m.IsStatic && m.DeclaringType == t && m.Name.StartsWith("Validate")).Select(m => m.Name)));
            var mf = new MarriageFacts { Husband = husband, Wife = wife, DateOfMarriage = DateTime.Today.AddDays(-1), Basis = "Exempt", ExemptionBasis = "Art. 27 - articulo mortis", HasPlace = true, Solemnizer = "Rev. Test", Witness1 = "A", Witness2 = "B" };
            var settings = MarriageService.Settings;
            var catalog = MarriageService.Catalog();
            Func<MarriageFacts, List<RuleIssue>> val = m => MarriageRules.ValidateMarriage(m, null, catalog, DateTime.Today, settings);
            List<RuleIssue> ok = val(mf);
            Console.WriteLine("    clean couple issues: " + string.Join(" | ", ok.Select(i => i.Code)));
            Check("a clean adult couple has no age/date hard stop", !ok.Any(i => i.Severity == RuleSeverity.HardStop || i.Code == "DOM_FUTURE" || i.Code == "SAME_NAME"), string.Join("; ", ok.Select(i => i.Message)));

            Func<Action<MarriageFacts>, List<RuleIssue>> run = mut => { var c = Clone(mf); mut(c); return val(c); };
            Func<List<RuleIssue>, bool> blocks = l => l.Any(i => i.Blocks && (i.Severity == RuleSeverity.HardStop || i.Code.StartsWith("DOM") || i.Code.StartsWith("AGE") || i.Code.Contains("DOB") || i.Code.Contains("SEX") || i.Code.Contains("SAME")));

            Check("husband 17 on the wedding day is blocked", blocks(run(c => c.Husband.Dob = DateTime.Today.AddYears(-17))));
            Check("wife 17 years 11 months is blocked", blocks(run(c => c.Wife.Dob = DateTime.Today.AddYears(-18).AddDays(1))));
            Check("exactly 18 today is allowed", !blocks(run(c => c.Wife.Dob = DateTime.Today.AddYears(-18))));
            Check("husband born AFTER the wedding date is blocked", blocks(run(c => c.Husband.Dob = DateTime.Today.AddDays(1))), "none blocking");
            Check("wedding date in the future is blocked (a marriage cannot be registered before it happens)", blocks(run(c => c.DateOfMarriage = DateTime.Today.AddDays(5))));
            Check("same person as husband and wife (same name + DOB) is flagged", run(c => { c.Wife.First = c.Husband.First; c.Wife.Last = c.Husband.Last; c.Wife.Dob = c.Husband.Dob; }).Count > ok.Count);
            Check("two males (FC Art. 2: a man and a woman) is flagged", run(c => c.Wife.Sex = "Male").Count > ok.Count, "issues " + run(c => c.Wife.Sex = "Male").Count);
            Check("DOB 1850 (>150 years) is flagged", run(c => c.Husband.Dob = new DateTime(1850, 1, 1)).Count > ok.Count);
        }

        private static MarriageFacts Clone(MarriageFacts m)
        {
            var c = new MarriageFacts();
            foreach (var f in typeof(MarriageFacts).GetFields()) f.SetValue(c, f.GetValue(m));
            foreach (var p in typeof(MarriageFacts).GetProperties().Where(x => x.CanWrite && x.GetIndexParameters().Length == 0)) p.SetValue(c, p.GetValue(m));
            c.Husband = ClonePartyP(m.Husband); c.Wife = ClonePartyP(m.Wife);
            return c;
        }
        private static Party ClonePartyP(Party p)
        {
            var c = new Party(p.Role == "Wife" ? "Wife" : "Husband");
            foreach (var f in typeof(Party).GetFields().Where(x => !x.IsInitOnly)) f.SetValue(c, f.GetValue(p));
            foreach (var pr in typeof(Party).GetProperties().Where(x => x.CanWrite && x.GetIndexParameters().Length == 0)) pr.SetValue(c, pr.GetValue(p));
            return c;
        }

        // ------------------------------------------------------------------ Form 90
        private static void Form90Battery()
        {
            Head("M3 Form 90: hostile text in every box -> Save draft -> reopen -> compare");
            int n = 0;
            foreach (string v in AreaA.Battery)
            {
                n++;
                var f = NewLic(null);
                f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-4000, -4000); f.ShowInTaskbar = false;
                object h = Fld<object>(f, "_h"), w = Fld<object>(f, "_w");
                FillBattery(f, v);
                foreach (var pr in new[] { Tuple.Create(h, "H"), Tuple.Create(w, "W") })
                {
                    Fld<TextBox>(pr.Item1, "First").Text = string.IsNullOrWhiteSpace(v) ? "Blankcase" : Fit(v, 60);
                    Fld<TextBox>(pr.Item1, "Last").Text = Tag + pr.Item2 + n;
                    Fld<DateTimePicker>(pr.Item1, "Dob").Value = DateTime.Today.AddYears(-30);
                    var civ = Fld<ComboBox>(pr.Item1, "Civil"); civ.SelectedItem = "Single";
                }
                Fld<ComboBox>(h, "Sex").SelectedItem = "Male"; Fld<ComboBox>(w, "Sex").SelectedItem = "Female";
                var typed = SnapTree(f);
                int mark = Dog.Count; bool saved = false;
                try { saved = (bool)Call(f, "SaveDraft", true); }
                catch (Exception ex) { Check("form90 battery " + n + " save threw", false, ex.GetType().Name + ": " + ex.Message); }
                var l = Fld<LicenseFacts>(f, "_l");
                if (!saved || l.Id <= 0) { Check("form90 battery " + n + " [" + Esc(v) + "] saves", false, "dialogs: " + string.Join(" || ", Dog.Since(mark))); f.Dispose(); continue; }
                LicenseIds.Add(l.Id);
                f.Dispose();
                var g = NewLic(l.Id);
                var back = SnapTree(g);
                var diffs = DiffTree(typed, back);
                Check("form90 battery " + n + " [" + Esc(v) + "] reopens unchanged", diffs.Count == 0, diffs.Count + " differences");
                foreach (string d in diffs.Take(16)) Console.WriteLine("      " + d);
                g.Dispose();
            }
        }

        // ------------------------------------------------------------------ roles
        private static void RoleChecks()
        {
            Head("M4 roles: who may bypass a requirement / issue");
            var keep = Session.User;
            Session.User = new CurrentUser { Id = keep.Id, Username = keep.Username, FullName = keep.FullName, Role = "Admin" };
            Check("Admin may bypass", MarriageService.CanBypass);
            Session.User = new CurrentUser { Id = keep.Id, Username = keep.Username, FullName = keep.FullName, Role = "Staff" };
            Check("Staff may bypass", MarriageService.CanBypass);
            Session.User = new CurrentUser { Id = keep.Id, Username = keep.Username, FullName = keep.FullName, Role = "Cashier" };
            Check("a removed role (Cashier) may not", !MarriageService.CanBypass);
            Session.User = null;
            Check("no signed-in user may not", !MarriageService.CanBypass);
            Session.User = keep;
        }

        // ------------------------------------------------------------------ desk search
        private static void DeskSearch()
        {
            Head("M5 desk search box: hostile strings must not throw or run");
            var f = new MarriageRegistrationForm();
            f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-4000, -4000); f.ShowInTaskbar = false;
            f.Show(); for (int i = 0; i < 5; i++) Application.DoEvents();
            var tb = Fld<TextBox>(f, "txtSearch");
            foreach (string s in new[] { "", "a", new string('x', 5000), "O'Brien", "'; DROP TABLE marriages;--", "100%", "_", "[", "]", "[abc", "*", "\\", "\"", "<script>", "Peñablanca", "\U0001F600", "a\r\nb" })
            {
                string res;
                try { tb.Text = s; Application.DoEvents(); res = "ok"; }
                catch (Exception ex) { res = "THREW " + ex.GetType().Name + ": " + ex.Message; }
                Check("desk search [" + Esc(s.Length > 20 ? s.Substring(0, 20) : s) + "] does not throw", res == "ok", res);
            }
            int cnt = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM marriages").Rows[0][0]);
            Check("marriages table intact", cnt > 0);
            f.Close();
        }

        // ------------------------------------------------------------------ double save
        private static void DoubleSave()
        {
            Head("M6 double Save of a new Form 97 -> one record");
            var f = new MarriageEntryForm(null);
            object h = Fld<object>(f, "_h"), w = Fld<object>(f, "_w");
            Fld<TextBox>(h, "First").Text = "Dbl"; Fld<TextBox>(h, "Last").Text = Tag + "DBL";
            Fld<DateTimePicker>(h, "Dob").Value = DateTime.Today.AddYears(-30);
            Call(f, "Save", "Draft"); Call(f, "Save", "Draft");
            DataTable t = Db.Pull("SELECT id FROM marriages WHERE husband_last_name=@l", new MySqlParameter("@l", Tag + "DBL"));
            Check("two Saves on the same open window -> one row", t.Rows.Count == 1, t.Rows.Count + " rows");
            foreach (DataRow r in t.Rows) MarriageIds.Add(Convert.ToInt32(r[0]));
            f.Dispose();
        }

        private static void Cleanup()
        {
            DataTable m = Db.Pull("SELECT id FROM marriages WHERE husband_last_name LIKE 'ZZT%' OR wife_last_name LIKE 'ZZT%'");
            var mids = m.AsEnumerable().Select(r => Convert.ToInt64(r[0])).Concat(MarriageIds.Select(x => (long)x)).Distinct().ToList();
            DataTable l = Db.Pull("SELECT id FROM marriage_licenses WHERE husband_last_name LIKE 'ZZT%' OR wife_last_name LIKE 'ZZT%'");
            var lids = l.AsEnumerable().Select(r => Convert.ToInt64(r[0])).Concat(LicenseIds.Select(x => (long)x)).Distinct().ToList();
            string ml = string.Join(",", mids.DefaultIfEmpty(0)), ll = string.Join(",", lids.DefaultIfEmpty(0));
            DeleteAuditSince("marriages", mids, "%ZZT%");
            DeleteAuditSince("marriage_licenses", lids, "%ZZT%");
            Db.Push("DELETE FROM marriage_case_history WHERE id > @h AND entity='Marriage' AND entity_id IN (" + ml + ")", new MySqlParameter("@h", HistStart));
            Db.Push("DELETE FROM marriage_case_history WHERE id > @h AND entity='License' AND entity_id IN (" + ll + ")", new MySqlParameter("@h", HistStart));
            Db.Push("DELETE FROM document_requirements WHERE owner_type='Marriage' AND owner_id IN (" + ml + ")");
            Db.Push("DELETE FROM document_requirements WHERE owner_type='License' AND owner_id IN (" + ll + ")");
            Db.Push("DELETE FROM marriages WHERE id IN (" + ml + ") AND (husband_last_name LIKE 'ZZT%' OR wife_last_name LIKE 'ZZT%')");
            Db.Push("DELETE FROM marriage_licenses WHERE id IN (" + ll + ") AND (husband_last_name LIKE 'ZZT%' OR wife_last_name LIKE 'ZZT%')");
        }
    }
}
