using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Forms;
using MySql.Data.MySqlClient;
using static CROMS.MarriageTest.AreaA;

namespace CROMS.MarriageTest
{
    /// <summary>Birth Registration (Form 102) input battery + round trips + status flow.</summary>
    internal static class AreaABirth
    {
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
        private const int WM_CHAR = 0x102;

        private static BirthRegistrationForm _f;
        private static Form _host;
        private static readonly List<long> Ids = new List<long>();

        private static object C(string m, params object[] a) { return Call(_f, m, a); }
        private static T F<T>(string n) { return G<T>(_f, n); }

        public static void Run()
        {
            Head("BIRTH setup");
            _f = new BirthRegistrationForm();
            _host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(1500, 900) };
            _f.TopLevel = false; _f.FormBorderStyle = FormBorderStyle.None; _f.Dock = DockStyle.Fill;
            _host.Controls.Add(_f); _host.Show(); _f.Show();
            F<Control>("cardForm").Visible = true;
            IntPtr hnd = F<TabControl>("tabControl").Handle;
            for (int i = 0; i < 5; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }
            try
            {
                Cleanup();
                EmptyValidation();
                Battery_RoundTrip();
                NumericBoxes();
                RequiredAndNames();
                DatesAndDelayed();
                FeeBoxes();
                StatusFlow();
                AutosaveDuplicate();
                CascadeResets();
                LayoutSweep();
            }
            catch (Exception ex) { Fail++; Console.WriteLine("BIRTH CRASH: " + ex); }
            finally
            {
                try { _host.Close(); } catch { }
                Cleanup();
                int left = Leftovers();
                Check("birth: zero strays after cleanup", left == 0, left + " left");
            }
        }

        // -------------------------------------------------------------------------------- empty
        private static void EmptyValidation()
        {
            Head("B1 empty form: required messages");
            C("ClearForm");
            bool ok = (bool)C("ValidateChild");
            Check("empty form refused by ValidateChild", !ok);
            var msgs = F<System.Collections.IDictionary>("_fieldMsgs");
            var texts = new List<string>();
            foreach (System.Collections.DictionaryEntry de in msgs)
            {
                object fm = de.Value;
                Label l = (Label)fm.GetType().GetField("Text").GetValue(fm);
                if (l.Visible && l.Text.Length > 0) texts.Add(((Control)de.Key).Name + ": " + l.Text);
            }
            Console.WriteLine("    messages: " + string.Join(" | ", texts));
            Check("child first/last/sex each show a plain sentence",
                texts.Count >= 3 && texts.All(t => !t.Contains("Exception") && !t.Contains("System.") && t.EndsWith(".")));
        }

        // -------------------------------------------------------------------------------- fill helpers
        private static readonly string[] NumericNames = { "txtWeight", "txtMAge", "txtFAge", "txtMBornAlive", "txtMLiving", "txtMDead" };

        private static void FillAll(string v, string last)
        {
            var all = Walk(_f).ToList();
            foreach (Control c in all)
            {
                if (c is TextBox tb && !tb.ReadOnly && !string.IsNullOrEmpty(tb.Name))
                {
                    if (tb.Name == "txtSearch" || tb.Name == "txtFee" || tb.Name == "txtFeeOr") continue;
                    if (all.Any(x => x.Name == tb.Name + "Lookup0")) continue;      // replaced by combo cells
                    if (NumericNames.Contains(tb.Name)) { tb.Text = "12"; continue; }
                    tb.Text = Fit(v, tb.MaxLength);
                }
                else if (c is ComboBox cb && cb.Enabled && !string.IsNullOrEmpty(cb.Name))
                {
                    if (cb.DropDownStyle == ComboBoxStyle.DropDown) cb.Text = Fit(v, cb.MaxLength);
                }
            }
            F<TextBox>("txtLastName").Text = last;
        }

        // Pick-only lists and the cascades need real selections, not text.
        private static void PickLists()
        {
            F<ComboBox>("cboSex").SelectedItem = "Female";
            var pob = F<ComboBox[]>("_pob");
            GeoLookup.Select(F<ComboBox>("_pobCountry"), GeoLookup.HomeCountry);
            GeoLookup.Select(pob[1], "Cagayan");
            if (pob[2].Items.Count > 1) GeoLookup.Select(pob[2], (string)pob[2].Items[1]);
            var order = F<ComboBox>("_cboBirthOrder"); if (order.Items.Count > 2) order.SelectedIndex = 2;
            F<DateTimePicker>("dtpDob").Value = DateTime.Today.AddDays(-3);
        }

        // -------------------------------------------------------------------------------- battery
        private static void Battery_RoundTrip()
        {
            Head("B2 battery: every box hostile text -> save -> reload -> compare");
            int n = 0;
            foreach (string v in Battery)
            {
                n++;
                C("ClearForm");
                string last = Tag + "B" + n;
                FillAll(v, last);
                PickLists();
                // first name must be non-blank for the record to be valid
                if (string.IsNullOrWhiteSpace(v)) F<TextBox>("txtFirstName").Text = "Blankcase";
                var typed = Snapshot(_f);
                bool valid = (bool)C("ValidateChild");
                if (!valid) { Check("battery " + n + " [" + Esc(v) + "] passes validation", false, "ValidateChild refused"); continue; }
                long? id = (long?)C("Create", "Draft", true);
                if (!id.HasValue) { Check("battery " + n + " [" + Esc(v) + "] saves", false, "Create returned null"); continue; }
                Ids.Add(id.Value);
                var back = Snapshot(_f);                  // Create(keepOpen) already reloads
                C("ClearForm"); C("LoadBirth", (int)id.Value);
                back = Snapshot(_f);
                var diffs = Diff(typed, back);
                // Trim() on names is intended; whitespace-only boxes become empty. Anything else is a finding.
                var real = diffs.Where(d => !IsExpectedTrim(d)).ToList();
                Check("battery " + n + " [" + Esc(v) + "] round-trips unchanged", real.Count == 0,
                      real.Count == 0 ? null : real.Count + " differences");
                foreach (string d in real.Take(14)) Console.WriteLine("      " + d);
                // DB value of first name must be the Trimmed text, bound as a parameter (not concatenated)
                DataRow r = Db.Pull("SELECT first_name, last_name, remarks FROM births WHERE id=" + id.Value).Rows[0];
                string expectFirst = string.IsNullOrWhiteSpace(v) ? "Blankcase" : Fit(v, 50).Trim();
                Check("   DB first_name equals what was typed", Convert.ToString(r["first_name"]) == expectFirst,
                      "[" + Esc(Convert.ToString(r["first_name"])) + "] vs [" + Esc(expectFirst) + "]");
            }
            int cnt = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM births WHERE last_name LIKE @t", new MySqlParameter("@t", Tag + "B%")).Rows[0][0]);
            Check("births table still exists and holds the battery rows (injection did nothing)", cnt >= 1, cnt + " rows");
        }

        private static bool IsExpectedTrim(string diff)
        {
            // typed "  x " -> reload shows "x" (Trim on save) is intended
            int a = diff.IndexOf("typed [") + 7, b = diff.IndexOf("] back [");
            if (a < 7 || b < 0) return false;
            string t = diff.Substring(a, b - a), bk = diff.Substring(b + 8).TrimEnd(']');
            return t.Trim() == bk || (t.Trim() == "" && bk == "");
        }

        // -------------------------------------------------------------------------------- numeric
        private static void Type(TextBox tb, string s)
        {
            tb.Clear();
            IntPtr h = tb.Handle;
            foreach (char ch in s) SendMessage(h, WM_CHAR, (IntPtr)ch, IntPtr.Zero);
        }

        private static void NumericBoxes()
        {
            Head("B3 number boxes: letters, negative, zero, decimals, 1e5, huge, separators (typed by key message)");
            C("ClearForm");
            foreach (string name in NumericNames)
            {
                var tb = F<TextBox>(name);
                foreach (string s in new[] { "abc", "-5", "0", "3.7", "1e5", "99999999999", "1,000", " 12 ", "٣" })
                {
                    Type(tb, s);
                    string got = tb.Text;
                    Console.WriteLine("    " + name + " max" + tb.MaxLength + "  typed [" + s + "] -> box shows [" + Esc(got) + "]");
                }
            }
            // what actually gets saved when the box holds non-numbers
            C("ClearForm");
            F<TextBox>("txtFirstName").Text = "Num"; F<TextBox>("txtLastName").Text = Tag + "N";
            F<TextBox>("txtMAge").Text = "abc"; F<TextBox>("txtFAge").Text = "-5"; F<TextBox>("txtWeight").Text = "0";
            F<TextBox>("txtMBornAlive").Text = "1"; F<TextBox>("txtMLiving").Text = "9"; F<TextBox>("txtMDead").Text = "9";
            var ps = (MySqlParameter[])C("FieldParams", "Draft");
            object Pv(string p) { return ps.First(x => x.ParameterName == p).Value; }
            Check("letters in mother's age: saved as blank WITHOUT any message (silent data loss)", Pv("@mage") == DBNull.Value, "@mage=" + Pv("@mage"));
            Check("negative father's age is refused or flagged", !Equals(Pv("@fage"), -5), "@fage=" + Pv("@fage"));
            Check("weight 0 g is refused or flagged", !Equals(Pv("@weight"), 0), "@weight=" + Pv("@weight"));
            Check("9 living + 9 dead of 1 born alive is flagged", false, "@mba=" + Pv("@mba") + " @mlv=" + Pv("@mlv") + " @mdd=" + Pv("@mdd") + "  (ValidateChild=" + C("ValidateChild") + ")");
        }

        // -------------------------------------------------------------------------------- required
        private static void RequiredAndNames()
        {
            Head("B4 required fields + name shapes");
            string[] bad = { "   ", "", "\t" };
            foreach (string b in bad)
            {
                C("ClearForm");
                F<TextBox>("txtFirstName").Text = b; F<TextBox>("txtLastName").Text = Tag + "R"; F<ComboBox>("cboSex").SelectedItem = "Male";
                Check("first name [" + Esc(b) + "] refused", !(bool)C("ValidateChild"));
            }
            string[] names = { "De La Cruz Jr.", "Dela-Cruz", "A", "DE LOS SANTOS", "o'neil", "123", "!!!", "Néstor", "Zoeë" };
            foreach (string nm in names)
            {
                C("ClearForm");
                F<TextBox>("txtFirstName").Text = nm; F<TextBox>("txtLastName").Text = Tag + "R"; F<ComboBox>("cboSex").SelectedItem = "Male";
                bool v = (bool)C("ValidateChild");
                Console.WriteLine("    first name [" + nm + "] -> ValidateChild " + v);
            }
            Note("names like '123' and '!!!' are accepted as a child's first name (no character check) - see summary");
        }

        // -------------------------------------------------------------------------------- dates
        private static void DatesAndDelayed()
        {
            Head("B5 dates: future / 1900 / 0001 / delayed");
            C("ClearForm");
            var dob = F<DateTimePicker>("dtpDob");
            Check("DOB MaxDate is today", dob.MaxDate.Date == DateTime.Today, dob.MaxDate.ToString());
            try { dob.Value = DateTime.Today.AddDays(1); Check("future DOB cannot be set", dob.Value.Date <= DateTime.Today, dob.Value.ToString()); }
            catch (ArgumentOutOfRangeException) { Check("future DOB cannot be set (picker refuses)", true); }
            try { dob.Value = new DateTime(1900, 1, 1); Console.WriteLine("    DOB 1900-01-01 accepted by picker; delayed label: " + F<CheckBox>("chkDelayed").Text); }
            catch (Exception e) { Console.WriteLine("    DOB 1900: " + e.GetType().Name); }
            Check("1753 minimum? picker MinDate", true, "MinDate=" + dob.MinDate.ToString("yyyy-MM-dd"));
            dob.Value = DateTime.Today.AddDays(-31);
            Application.DoEvents();
            Check("31 days old -> delayed flag ticked", F<CheckBox>("chkDelayed").Checked, F<CheckBox>("chkDelayed").Text);
            dob.Value = DateTime.Today.AddDays(-30);
            Application.DoEvents();
            Console.WriteLine("    30 days old -> delayed=" + F<CheckBox>("chkDelayed").Checked + " [" + F<CheckBox>("chkDelayed").Text + "]");
            // marriage of parents date: before 1900 / future
            var md = F<DateTimePicker>("dtpMarrDate");
            Check("parents' marriage MaxDate is today", md.MaxDate.Date == DateTime.Today);
            foreach (string name in new[] { "dtpAttDate", "dtpInfDate", "dtpPreparedDate", "dtpReceivedDate", "dtpRegisteredDate" })
                Check(name + " MaxDate is today", F<DateTimePicker>(name).MaxDate.Date == DateTime.Today);
            // sign date before the birth
            C("ClearForm");
            F<TextBox>("txtFirstName").Text = "Dated"; F<TextBox>("txtLastName").Text = Tag + "D"; F<ComboBox>("cboSex").SelectedItem = "Male";
            dob.Value = DateTime.Today.AddDays(-2);
            var ad = F<DateTimePicker>("dtpAttDate"); ad.Checked = true; ad.Value = DateTime.Today.AddYears(-3);
            Check("attendant signed 3 years BEFORE the birth is flagged", !(bool)C("ValidateChild"), "ValidateChild=" + C("ValidateChild"));
            // parents married after the child (legit for legitimation) must be allowed
            ad.Checked = false;
            md.Checked = true; md.Value = DateTime.Today.AddDays(-1);
            Check("parents married AFTER the birth is allowed (RA 9858)", (bool)C("ValidateChild"));
        }

        // -------------------------------------------------------------------------------- fee
        private static void FeeBoxes()
        {
            Head("B6 registration fee + O.R.");
            C("ClearForm");
            F<TextBox>("txtFirstName").Text = "Fee"; F<TextBox>("txtLastName").Text = Tag + "F"; F<ComboBox>("cboSex").SelectedItem = "Male";
            var fee = F<TextBox>("txtFee"); var or = F<TextBox>("txtFeeOr");
            Console.WriteLine("    txtFee max " + fee.MaxLength + "   txtFeeOr max " + or.MaxLength);
            DataTable w = Db.Pull("SELECT character_maximum_length FROM information_schema.columns WHERE table_schema='croms' AND table_name='payments' AND column_name='or_number'");
            int orw = Convert.ToInt32(w.Rows[0][0]);
            Console.WriteLine("    payments.or_number width " + orw);
            foreach (string s in new[] { "abc", "-5", "0", "3.7", "1e5", "99999999999", "1,000", "250.123" })
            {
                Type(fee, s);
                or.Text = Tag + "-FEE";
                bool ok = (bool)C("ValidateChild");
                Console.WriteLine("    fee typed [" + s + "] -> box [" + Esc(fee.Text) + "]  ValidateChild " + ok);
            }
            Type(fee, "250"); or.Text = new string('Z', orw + 1);
            bool longOk = (bool)C("ValidateChild");
            Check("O.R. number one over the column width is stopped by the box or refused by validation", or.Text.Length <= orw || !longOk, "box length " + or.Text.Length + " valid " + longOk);
            or.Text = ""; fee.Text = "";
        }

        // -------------------------------------------------------------------------------- status flow
        private static void StatusFlow()
        {
            Head("B7 Draft -> Submit -> Approve -> Registered; Update; Delete; messages");
            C("ClearForm");
            F<TextBox>("txtFirstName").Text = "Flow"; F<TextBox>("txtLastName").Text = Tag + "S"; F<ComboBox>("cboSex").SelectedItem = "Female";
            F<DateTimePicker>("dtpDob").Value = DateTime.Today.AddDays(-2);
            long? id = (long?)C("Create", "Draft", true);
            Check("draft saved", id.HasValue); if (!id.HasValue) return;
            Ids.Add(id.Value);
            DataRow r = Db.Pull("SELECT status, registry_no FROM births WHERE id=" + id.Value).Rows[0];
            Check("draft has NO registry number", Convert.ToString(r["registry_no"]) == "", "[" + r["registry_no"] + "]");

            int mark = Dog.Count;
            C("SubmitRecord", (string)null);
            Application.DoEvents();
            r = Db.Pull("SELECT status, registry_no FROM births WHERE id=" + id.Value).Rows[0];
            Check("submit -> Pending Approval with a registry number", Convert.ToString(r["status"]) == "Pending Approval" && Convert.ToString(r["registry_no"]).Length > 4, Convert.ToString(r["status"]) + " / " + r["registry_no"]);
            foreach (string l in Dog.Since(mark)) Console.WriteLine("      dialog: " + l);
            Check("submit shows a plain 'Submitted for approval' message, no raw exception",
                Dog.Since(mark).Any(l => l.Contains("Submitted for approval")) && !Dog.Since(mark).Any(l => l.Contains("Exception")));
            int dup = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM births WHERE last_name=@l", new MySqlParameter("@l", Tag + "S")).Rows[0][0]);
            Check("submitting a draft that was saved earlier updates in place (1 row, not 2)", dup == 1, dup + " rows");

            // approve through the real pending grid
            C("ClearForm");
            C("LoadPendingBirths");
            var dgv = F<DataGridView>("_dgvPending");
            dgv.ClearSelection();
            DataGridViewRow row = dgv.Rows.Cast<DataGridViewRow>().FirstOrDefault(x => Convert.ToInt64(x.Cells["id"].Value) == id.Value);
            Check("record appears in the Pending Approval list", row != null);
            if (row != null)
            {
                dgv.CurrentCell = row.Cells[1]; row.Selected = true;
                mark = Dog.Count;
                C("ApprovePending");
                r = Db.Pull("SELECT status FROM births WHERE id=" + id.Value).Rows[0];
                Check("approve -> Registered", Convert.ToString(r["status"]) == "Registered", Convert.ToString(r["status"]));
                foreach (string l in Dog.Since(mark)) Console.WriteLine("      dialog: " + l);
            }

            // editing a registered record through the module's own controls
            C("LoadBirth", (int)id.Value);
            mark = Dog.Count;
            C("SubmitRecord", (string)null);
            Console.WriteLine("    Submit on a Registered record -> " + string.Join(" || ", Dog.Since(mark)));
            F<TextBox>("txtRemarks").Text = "edited " + Tag;
            mark = Dog.Count;
            C("btnUpdate_Click", null, EventArgs.Empty);
            string rem = Convert.ToString(Db.Pull("SELECT remarks FROM births WHERE id=" + id.Value).Rows[0][0]);
            Console.WriteLine("    Update on Registered -> remarks in DB [" + rem + "]  dialogs: " + string.Join(" || ", Dog.Since(mark)));
            // is Update/Delete reachable from the entry popup? (buttons are removed from cardForm while open)
            Note("In the real popup btnUpdate/btnDelete are not shown (pnlRecordActions is detached), so a Registered birth can only be Submitted (refused) - editing needs Records Archive, which is view-only for Registered rows.");

            mark = Dog.Count;
            C("LoadBirth", (int)id.Value);
            C("btnDelete_Click", null, EventArgs.Empty);
            int still = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM births WHERE id=" + id.Value).Rows[0][0]);
            Console.WriteLine("    Delete -> rows left " + still + "  dialogs: " + string.Join(" || ", Dog.Since(mark)));
            if (still == 0) Ids.Remove(id.Value);
        }

        // -------------------------------------------------------------------------------- autosave duplicate
        private static void AutosaveDuplicate()
        {
            Head("B8 autosave + Save Draft / double submit must not create two rows");
            C("ClearForm");
            Set(_f, "_entryDialog", new Form());           // autosave only runs inside the popup
            F<TextBox>("txtFirstName").Text = "Auto"; F<TextBox>("txtLastName").Text = Tag + "A"; F<ComboBox>("cboSex").SelectedItem = "Male";
            Set(_f, "_autoSaveDirty", true);
            C("AutoSaveTick");
            int n1 = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM births WHERE last_name=@l", new MySqlParameter("@l", Tag + "A")).Rows[0][0]);
            Check("autosave wrote exactly one Draft", n1 == 1, n1 + " rows");
            // Save Draft pressed after autosave already created the row
            Set(_f, "_autoSaveDirty", false);
            long? id = (long?)C("Create", "Draft", false);
            int n2 = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM births WHERE last_name=@l", new MySqlParameter("@l", Tag + "A")).Rows[0][0]);
            Check("Save Draft after an autosave updates the same row (still 1)", n2 == 1, n2 + " rows");
            foreach (DataRow d in Db.Pull("SELECT id FROM births WHERE last_name=@l", new MySqlParameter("@l", Tag + "A")).Rows) Ids.Add(Convert.ToInt64(d[0]));
            try { ((Form)F<Form>("_entryDialog")).Dispose(); } catch { }
            Set(_f, "_entryDialog", null);
            // rapid double Submit on a fresh valid form
            C("ClearForm");
            F<TextBox>("txtFirstName").Text = "Twice"; F<TextBox>("txtLastName").Text = Tag + "T"; F<ComboBox>("cboSex").SelectedItem = "Male";
            C("SubmitRecord", (string)null);
            C("SubmitRecord", (string)null);          // second click on the now-cleared form
            int n3 = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM births WHERE last_name=@l", new MySqlParameter("@l", Tag + "T")).Rows[0][0]);
            Check("double Submit creates one record", n3 == 1, n3 + " rows");
            foreach (DataRow d in Db.Pull("SELECT id FROM births WHERE last_name=@l", new MySqlParameter("@l", Tag + "T")).Rows) Ids.Add(Convert.ToInt64(d[0]));
        }

        // -------------------------------------------------------------------------------- cascade
        private static void CascadeResets()
        {
            Head("B9 province -> city -> barangay resets");
            C("ClearForm");
            var res = F<ComboBox[]>("_mres");              // house, province, municipality, barangay (array order)
            Console.WriteLine("    mother residence cells: " + string.Join(", ", res.Select(c => c.Name + "(" + c.Items.Count + ")")));
            GeoLookup.Select(res[1], "Cagayan");
            if (res[2].Items.Count > 1) GeoLookup.Select(res[2], (string)res[2].Items[1]);
            if (res[3].Items.Count > 1) GeoLookup.Select(res[3], (string)res[3].Items[1]);
            string before = res[2].Text + "/" + res[3].Text;
            GeoLookup.Select(res[1], "Isabela");
            Application.DoEvents();
            Check("changing the province clears municipality and barangay", res[2].Text == "" && res[3].Text == "", "was " + before + " now [" + res[2].Text + "]/[" + res[3].Text + "]");
            res[1].Text = "Atlantis"; res[1].Focus(); Application.DoEvents();
            res[0].Focus(); Application.DoEvents();
            Console.WriteLine("    type unlisted province 'Atlantis' then leave -> [" + res[1].Text + "]");
        }

        // -------------------------------------------------------------------------------- layout
        private static void LayoutSweep()
        {
            Head("B10 layout sweep (sibling overlaps) at 1500x900 host");
            int overlaps = 0;
            foreach (TabPage tp in F<TabControl>("tabControl").TabPages)
            {
                F<TabControl>("tabControl").SelectedTab = tp; Application.DoEvents();
                foreach (Control p in Walk(tp).Where(c => c.HasChildren || c is TableLayoutPanel).Concat(new[] { (Control)tp }))
                {
                    if (p is TableLayoutPanel) continue;
                    var kids = p.Controls.Cast<Control>().Where(k => k.Visible && k.Width > 0 && k.Height > 0 && !(k is Label && k.Dock != DockStyle.None)).ToList();
                    for (int i = 0; i < kids.Count; i++)
                        for (int j = i + 1; j < kids.Count; j++)
                        {
                            Rectangle a = kids[i].Bounds, b = kids[j].Bounds; Rectangle x = Rectangle.Intersect(a, b);
                            if (x.Width > 3 && x.Height > 3 && kids[i].Dock == DockStyle.None && kids[j].Dock == DockStyle.None)
                            { overlaps++; Console.WriteLine("      overlap in " + tp.Text + ": " + kids[i].Name + " x " + kids[j].Name); }
                        }
                }
            }
            Check("no overlapping controls on any step", overlaps == 0, overlaps + " overlaps");
        }

        // -------------------------------------------------------------------------------- cleanup
        private static void Cleanup()
        {
            DataTable ids = Db.Pull("SELECT id FROM births WHERE last_name LIKE 'ZZT%'");
            var all = ids.AsEnumerable().Select(r => Convert.ToInt64(r[0])).Concat(Ids).Distinct().ToList();
            string list = string.Join(",", all.DefaultIfEmpty(0));
            DeleteAuditSince("births", all, "%ZZT%");
            Db.Push("DELETE FROM marriage_requirements WHERE owner_type = 'Birth' AND owner_id IN (" + list + ")");
            Db.Push("DELETE FROM births WHERE id IN (" + list + ") AND last_name LIKE 'ZZT%'");
        }

        private static int Leftovers()
        {
            return Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM births WHERE last_name LIKE 'ZZT%'").Rows[0][0]);
        }
    }
}
