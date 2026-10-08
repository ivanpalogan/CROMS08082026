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
    /// <summary>Death Registration (Form 103) input battery + round trips.</summary>
    internal static class AreaADeath
    {
        private static DeathRegistrationForm _f;
        private static Form _host;
        private static readonly List<long> Ids = new List<long>();
        private static object C(string m, params object[] a) { return Call(_f, m, a); }
        private static T F<T>(string n) { return G<T>(_f, n); }

        public static void Run()
        {
            Head("DEATH setup");
            _f = new DeathRegistrationForm();
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
                Battery();
                AgeAndDates();
                TimeOfDeath();
                PendingVerification();
                UpdateAndDouble();
                OthersBoxCheck();
            }
            catch (Exception ex) { Fail++; Console.WriteLine("DEATH CRASH: " + ex); }
            finally
            {
                try { _host.Close(); } catch { }
                Cleanup();
                int left = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM deaths WHERE last_name LIKE 'ZZT%' OR full_name LIKE '%ZZT%'").Rows[0][0]);
                Check("death: zero strays after cleanup", left == 0, left + " left");
            }
        }

        private static void EmptyValidation()
        {
            Head("D1 empty form: required messages (Register must refuse, no raw text)");
            C("ClearForm");
            int mark = Dog.Count;
            bool ok = (bool)C("ValidateAllSteps");
            Check("empty Register refused", !ok);
            var msgs = F<System.Collections.IDictionary>("_fieldMsgs");
            var texts = new List<string>();
            foreach (System.Collections.DictionaryEntry de in msgs)
            {
                object fm = de.Value;
                Label l = (Label)fm.GetType().GetField("Text").GetValue(fm);
                if (l.Visible && l.Text.Length > 0) texts.Add(l.Text);
            }
            Console.WriteLine("    " + texts.Count + " messages: " + string.Join(" | ", texts));
            Check("every required field shows a sentence", texts.Count >= 10 && texts.All(t => t.EndsWith(".")), texts.Count + " messages");
            Check("no pop-up for field mistakes", Dog.Since(mark).Count == 0, string.Join(" || ", Dog.Since(mark)));
        }

        // ------------------------------------------------------------------ fill
        private static void FillAllText(string v, string last)
        {
            var all = Walk(_f).ToList();
            foreach (Control c in all)
            {
                if (c is TextBox tb && !tb.ReadOnly)
                {
                    if (tb.Name == "txtSearch") continue;
                    if (all.Any(x => x.Name == tb.Name + "Lookup0")) continue;
                    if (tb.Name == "txtAge") continue;
                    tb.Text = Fit(v, tb.MaxLength);
                }
                else if (c is ComboBox cb && cb.Enabled && cb.DropDownStyle == ComboBoxStyle.DropDown && !all.Any(x => x.Name == "") )
                {
                    cb.Text = Fit(v, cb.MaxLength);
                }
            }
            F<TextBox>("txtLastName").Text = last;
        }

        private static void PickRequired()
        {
            F<ComboBox>("cboSex").SelectedItem = "Male";
            if (F<ComboBox>("cboCivil").Items.Count > 1) F<ComboBox>("cboCivil").SelectedIndex = 1;
            var cit = F<ComboBox>("_cboDCit"); if (cit.Items.Count > 1) cit.SelectedIndex = 1;
            F<TextBox>("txtAge").Text = "40";
            var pod = F<ComboBox[]>("_pod");               // facility, province, municipality (screen order)
            GeoLookup.Select(pod[1], "Cagayan");
            if (pod[2].Items.Count > 1) GeoLookup.Select(pod[2], (string)pod[2].Items[1]);
            F<ComboBox>("_cboDImm").Text = "Cardiopulmonary arrest";
            var disp = F<ComboBox>("cboDisposal"); if (disp.Items.Count > 1) disp.SelectedIndex = 1;
            var rel = F<ComboBox>("_cboInfRel"); if (rel.Items.Count > 1) rel.SelectedIndex = 1;
            F<DateTimePicker>("dtpDod").Value = DateTime.Today.AddDays(-5);
        }

        private static void Battery()
        {
            Head("D2 battery: hostile text in every box -> Register -> reload -> compare");
            int n = 0;
            foreach (string v in AreaA.Battery)
            {
                n++;
                C("ClearForm");
                FillAllText(v, Tag + "D" + n);
                if (string.IsNullOrWhiteSpace(v)) { F<TextBox>("txtFirstName").Text = "Blankcase"; F<TextBox>("txtCInfName").Text = "Informant"; F<TextBox>("txtCInfAddr").Text = "Addr"; F<TextBox>("txtDispPlace").Text = "Cemetery"; }
                PickRequired();
                // fields the harness text-fill may have disturbed: re-set the cascade & cause
                var typed = Snapshot(_f);
                long? id = null;
                int mark = Dog.Count;
                try { id = (long?)C("Register", true); }
                catch (Exception ex) { Check("death battery " + n + " register threw", false, ex.Message); continue; }
                if (!id.HasValue)
                {
                    var msgs = F<System.Collections.IDictionary>("_fieldMsgs");
                    var t = new List<string>();
                    foreach (System.Collections.DictionaryEntry de in msgs) { object fm = de.Value; Label l = (Label)fm.GetType().GetField("Text").GetValue(fm); if (l.Visible && l.Text.Length > 0) t.Add(((Control)de.Key).Name + ": " + l.Text); }
                    Check("death battery " + n + " [" + Esc(v) + "] registers", false, "refused: " + string.Join(" | ", t) + " dialogs: " + string.Join("||", Dog.Since(mark)));
                    continue;
                }
                Ids.Add(id.Value);
                C("ClearForm"); C("LoadDeath", (int)id.Value);
                var back = Snapshot(_f);
                var diffs = Diff(typed, back).Where(d => !IsExpectedTrim(d)).ToList();
                Check("death battery " + n + " [" + Esc(v) + "] round-trips unchanged", diffs.Count == 0, diffs.Count + " differences");
                foreach (string d in diffs.Take(12)) Console.WriteLine("      " + d);
                DataRow r = Db.Pull("SELECT first_name, last_name, full_name FROM deaths WHERE id=" + id.Value).Rows[0];
                Console.WriteLine("      DB first_name [" + Esc(Convert.ToString(r["first_name"])) + "]  full_name [" + Esc(Convert.ToString(r["full_name"])) + "]");
            }
        }

        private static bool IsExpectedTrim(string diff)
        {
            int a = diff.IndexOf("typed [") + 7, b = diff.IndexOf("] back [");
            if (a < 7 || b < 0) return false;
            string t = diff.Substring(a, b - a), bk = diff.Substring(b + 8).TrimEnd(']');
            return t.Trim() == bk || (t.Trim() == "" && bk == "");
        }

        // ------------------------------------------------------------------ age / dates
        private static void AgeAndDates()
        {
            Head("D3 age computation + date rules");
            C("ClearForm");
            var dob = F<DateTimePicker>("dtpDob"); var dod = F<DateTimePicker>("dtpDod"); var age = F<TextBox>("txtAge");
            Check("DOD MaxDate is today", dod.MaxDate.Date == DateTime.Today);
            try { dod.Value = DateTime.Today.AddDays(1); Check("future date of death cannot be set", dod.Value.Date <= DateTime.Today, dod.Value.ToString()); }
            catch (ArgumentOutOfRangeException) { Check("future date of death refused by picker", true); }
            dod.Value = new DateTime(2020, 3, 1);
            dob.Checked = true; dob.Value = new DateTime(1990, 3, 1);
            Check("age 30 on the 30th birthday", age.Text == "30" && age.ReadOnly, "[" + age.Text + "] ro=" + age.ReadOnly);
            dob.Value = new DateTime(1990, 3, 2);
            Check("age 29 the day before the 30th birthday", age.Text == "29", "[" + age.Text + "]");
            dob.Value = new DateTime(2020, 3, 1);
            Check("born the day of death -> 0 years", age.Text == "0", "[" + age.Text + "]");
            dob.Value = new DateTime(1900, 1, 1);
            Console.WriteLine("    dob 1900-01-01 / dod 2020-03-01 -> age [" + age.Text + "]  note: " + F<Label>("lblAgeNote").Text);
            // 29 Feb: born on a leap day, died on non-leap 28 Feb / 1 Mar
            dob.Value = new DateTime(2000, 2, 29); dod.Value = new DateTime(2021, 2, 28);
            Console.WriteLine("    born 29 Feb 2000, died 28 Feb 2021 -> age [" + age.Text + "]");
            dod.Value = new DateTime(2021, 3, 1);
            Console.WriteLine("    born 29 Feb 2000, died 1 Mar 2021 -> age [" + age.Text + "]");
            // death before birth
            dod.Value = new DateTime(2020, 1, 1); dob.Value = new DateTime(2021, 1, 1);
            Check("death before birth: age blank and the note says so", age.Text == "" && F<Label>("lblAgeNote").Text.Contains("after"), "[" + age.Text + "] " + F<Label>("lblAgeNote").Text);
            C("ClearForm");
            F<TextBox>("txtFirstName").Text = "Zed"; F<TextBox>("txtLastName").Text = Tag + "DA";
            PickRequired();
            dod.Value = new DateTime(2020, 1, 1); dob.Checked = true; dob.Value = new DateTime(2021, 1, 1);
            Check("Register refused when birth is after death", !(bool)C("ValidateAllSteps"));
            dob.Checked = false;
            foreach (string s in new[] { "abc", "-5", "0", "131", "999", "1e1", "3.7" })
            {
                age.Text = s;
                bool ok = (bool)C("ValidateAllSteps");
                Console.WriteLine("    age box [" + s + "] -> ValidateAllSteps " + ok);
            }
            age.Text = "999"; Check("age 999 refused", !(bool)C("ValidateAllSteps"));
            age.Text = "0"; Check("age 0 accepted (infant)", (bool)C("ValidateAllSteps"));
        }

        private static void TimeOfDeath()
        {
            Head("D4 time of death default: stored even when never touched?");
            C("ClearForm");
            var tod = F<DateTimePicker>("dtpTod");
            Console.WriteLine("    dtpTod after ClearForm: " + tod.Value.ToString("HH:mm") + "  ShowCheckBox=" + tod.ShowCheckBox + " Checked=" + tod.Checked);
            F<TextBox>("txtFirstName").Text = "Tod"; F<TextBox>("txtLastName").Text = Tag + "DT";
            PickRequired();
            F<TextBox>("txtCInfName").Text = "Inf"; F<TextBox>("txtCInfAddr").Text = "Addr"; F<TextBox>("txtDispPlace").Text = "Cem";
            long? id = (long?)C("Register", true);
            if (id.HasValue) Ids.Add(id.Value);
            string stored = id.HasValue ? Convert.ToString(Db.Pull("SELECT time_of_death FROM deaths WHERE id=" + id.Value).Rows[0][0]) : "(no row)";
            Check("a time of death that nobody entered is not stored as a real-looking time", stored == "" || stored == "00:00", "stored [" + stored + "]");
        }

        // ------------------------------------------------------------------ pending verification
        private static void PendingVerification()
        {
            Head("D5 'Pending Verification' (ack slip) -> can it ever be Registered?");
            C("ClearForm");
            F<TextBox>("txtFirstName").Text = "Pend"; F<TextBox>("txtLastName").Text = Tag + "DP";
            long? id = (long?)C("SubmitPendingVerification", true);
            Check("pending-verification record saved with only a name", id.HasValue);
            if (!id.HasValue) return;
            Ids.Add(id.Value);
            string st = Convert.ToString(Db.Pull("SELECT status FROM deaths WHERE id=" + id.Value).Rows[0][0]);
            Check("status is Pending Verification", st == "Pending Verification", st);
            // complete it and press the real save button
            PickRequired();
            F<TextBox>("txtCInfName").Text = "Inf"; F<TextBox>("txtCInfAddr").Text = "Addr"; F<TextBox>("txtDispPlace").Text = "Cem";
            Console.WriteLine("    save caption on the open pending record: [" + F<Button>("btnSave").Text + "]");
            C("btnSave_Click", null, EventArgs.Empty);
            st = Convert.ToString(Db.Pull("SELECT status FROM deaths WHERE id=" + id.Value).Rows[0][0]);
            Check("a completed pending record can be registered", st == "Registered", "status now [" + st + "]");
            int n = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM deaths WHERE last_name=@l", new MySqlParameter("@l", Tag + "DP")).Rows[0][0]);
            Check("no duplicate row", n == 1, n + " rows");
        }

        private static void UpdateAndDouble()
        {
            Head("D6 Update an existing record; double Register creates one row");
            C("ClearForm");
            F<TextBox>("txtFirstName").Text = "Twice"; F<TextBox>("txtLastName").Text = Tag + "DX";
            PickRequired();
            F<TextBox>("txtCInfName").Text = "Inf"; F<TextBox>("txtCInfAddr").Text = "Addr"; F<TextBox>("txtDispPlace").Text = "Cem";
            C("btnSave_Click", null, EventArgs.Empty);
            C("btnSave_Click", null, EventArgs.Empty);      // second click on the cleared form
            DataTable t = Db.Pull("SELECT id FROM deaths WHERE last_name=@l", new MySqlParameter("@l", Tag + "DX"));
            Check("double Register -> one record", t.Rows.Count == 1, t.Rows.Count + " rows");
            foreach (DataRow r in t.Rows) Ids.Add(Convert.ToInt64(r[0]));
            if (t.Rows.Count < 1) return;
            int id = Convert.ToInt32(t.Rows[0][0]);
            C("LoadDeath", id);
            F<TextBox>("txtMiddleName").Text = "Edited";
            F<TextBox>("txtBookVol").Text = "2026"; F<TextBox>("txtBookPage").Text = "17";
            C("btnSave_Click", null, EventArgs.Empty);
            DataRow row = Db.Pull("SELECT middle_name, book_volume, book_page FROM deaths WHERE id=" + id).Rows[0];
            Check("update saved the edited cells", Convert.ToString(row[0]) == "Edited" && Convert.ToString(row[1]) == "2026" && Convert.ToString(row[2]) == "17", Convert.ToString(row[0]) + "/" + row[1] + "/" + row[2]);
            Check("Delete is gone from the form", !Walk(_f).Any(c => c is Button b && b.Text.IndexOf("delete", StringComparison.OrdinalIgnoreCase) >= 0 && b.Visible));
        }

        private static void OthersBoxCheck()
        {
            Head("D7 informant relationship Others + specify");
            C("ClearForm");
            var rel = F<ComboBox>("_cboInfRel");
            var other = F<TextBox>("txtCInfRelOther");
            int idx = rel.Items.Cast<object>().ToList().FindIndex(i => i.ToString().StartsWith("Other", StringComparison.OrdinalIgnoreCase));
            Check("relationship list offers an Others choice", idx >= 0);
            if (idx < 0) return;
            rel.SelectedIndex = idx; Application.DoEvents();
            other.Text = "Barangay Captain";
            var ps = (MySqlParameter[])C("FieldParams");
            Check("stored as 'Others - Barangay Captain'", Convert.ToString(ps.First(p => p.ParameterName == "@irel").Value) == "Others - Barangay Captain", Convert.ToString(ps.First(p => p.ParameterName == "@irel").Value));
            rel.SelectedIndex = 1; Application.DoEvents();
            ps = (MySqlParameter[])C("FieldParams");
            Check("switching away clears the specify text", Convert.ToString(ps.First(p => p.ParameterName == "@irel").Value) == rel.Text, Convert.ToString(ps.First(p => p.ParameterName == "@irel").Value));
        }

        private static void Cleanup()
        {
            DataTable ids = Db.Pull("SELECT id FROM deaths WHERE last_name LIKE 'ZZT%' OR full_name LIKE '%ZZT%'");
            var all = ids.AsEnumerable().Select(r => Convert.ToInt64(r[0])).Concat(Ids).Distinct().ToList();
            string list = string.Join(",", all.DefaultIfEmpty(0));
            // Death audit rows carry the registry number (not the id) as record ref; match by details tag only, after the start of this run.
            Db.Push("DELETE FROM audit_log WHERE id > @s AND table_name='deaths' AND details LIKE '%ZZT%'", new MySqlParameter("@s", AuditStart));
            Db.Push("DELETE FROM audit_log WHERE id > @s AND table_name='deaths' AND record_id IN (" + list + ")", new MySqlParameter("@s", AuditStart));
            Db.Push("DELETE FROM deaths WHERE id IN (" + list + ") AND (last_name LIKE 'ZZT%' OR full_name LIKE '%ZZT%')");
        }
    }
}
