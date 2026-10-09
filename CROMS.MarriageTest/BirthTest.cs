using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Forms;
using MySql.Data.MySqlClient;

namespace CROMS.MarriageTest
{
    /// <summary>
    /// Birth Registration against the LIVE database: the new Form-102 fields round-trip (time of
    /// birth, hospital address, 5b, 1st/2nd/3rd birth order), the typed registration fee lands in
    /// the payment log exactly once, a Draft never spends an O.R., a duplicate O.R. is refused,
    /// the informant is pre-filled from the father without overwriting anything typed, and the
    /// attendant's "Others" specify box shows. Every row is tagged and deleted afterwards.
    /// </summary>
    internal static class BirthTest
    {
        private const BindingFlags NF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly string Tag = "ZZB" + DateTime.Now.ToString("HHmmss");
        private static readonly string Or1 = Tag + "-OR1";
        private static int _pass, _fail;
        private static BirthRegistrationForm _f;
        private static Form _host;
        private static readonly List<long> Ids = new List<long>();

        private static void Check(string name, bool ok, string detail = null)
        {
            if (ok) _pass++; else _fail++;
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + (detail == null ? "" : "   -> " + detail));
        }

        private static T G<T>(string field) { return (T)typeof(BirthRegistrationForm).GetField(field, NF).GetValue(_f); }
        private static object Call(string method, params object[] args)
        {
            try { return typeof(BirthRegistrationForm).GetMethod(method, NF).Invoke(_f, args); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }

        public static int Run()
        {
            Console.WriteLine("CROMS birth registration test  -  tag " + Tag);
            DataTable u = Db.Pull("SELECT id, username, full_name FROM users WHERE role='Admin' ORDER BY id LIMIT 1");
            Session.User = new CurrentUser { Id = Convert.ToInt32(u.Rows[0]["id"]), Username = u.Rows[0]["username"].ToString(), FullName = u.Rows[0]["full_name"].ToString(), Role = "Admin" };
            Application.EnableVisualStyles();
            try
            {
                Cleanup();
                Pure();
                PrintCells();
                Build();
                RoundTrip();
                DraftAndDuplicate();
                Informant();
                AttendantOthers();
            }
            catch (Exception ex) { _fail++; Console.WriteLine("CRASH: " + ex); }
            finally
            {
                try { if (_host != null) _host.Close(); } catch { }
                Cleanup();
                int left = Leftovers();
                Check("zero strays after cleanup", left == 0, left + " left");
            }
            Console.WriteLine("PASSED " + _pass + "   FAILED " + _fail);
            return _fail;
        }

        // ---------------------------------------------------------------- pure
        private static void Pure()
        {
            Console.WriteLine("\n[1] Birth order text");
            Type t = typeof(BirthRegistrationForm).Assembly.GetType("CROMS.Forms.BirthOrderText", true);
            Func<string, string> n = s => (string)t.GetMethod("Normalize").Invoke(null, new object[] { s });
            Check("First -> 1st", n("First") == "1st");
            Check("Second -> 2nd, Third -> 3rd, Eighth -> 8th", n("second") == "2nd" && n("THIRD") == "3rd" && n("Eighth") == "8th");
            Check("digits: 2 -> 2nd, 03 -> 3rd, 11 -> 11th, 12 -> 12th, 22 -> 22nd is outside 1-20 so kept", n("2") == "2nd" && n("03") == "3rd" && n("11") == "11th" && n("12th") == "12th" && n("22") == "22");
            Check("1st stays 1st; blank stays blank; junk is left exactly as read", n("1st") == "1st" && n("  ") == "" && n("abc") == "abc");
            Check("Twelfth -> 12th, Twentieth -> 20th", n("Twelfth") == "12th" && n("twentieth") == "20th");
        }

        // ---------------------------------------------------------------- printed cells
        private static void PrintCells()
        {
            Console.WriteLine("\n[1b] Printed certificate cells");
            Assembly asm = typeof(BirthRegistrationForm).Assembly;
            Type cr = asm.GetType("CROMS.Data.CertificateReport", true);
            Type pc = asm.GetType("CROMS.Data.PrintCell", true);
            MethodInfo cellText = cr.GetMethod("CellText", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var t = new DataTable();
            foreach (string c in new[] { "place_of_birth", "place_of_birth_house", "place_of_birth_barangay", "time_of_birth" }) t.Columns.Add(c, typeof(string));
            t.Rows.Add("CVMC, Cagayan, Tuguegarao City", "Carig Rd", "Carig Sur", "15:15");
            DataRow row = t.Rows[0];

            object facility = Activator.CreateInstance(pc, "place_of_birth", 0f, 0f, 7.5f, 0, false);
            pc.GetField("Extra").SetValue(facility, new[] { "place_of_birth_house", "place_of_birth_barangay" });
            Check("facility box prints name, house/street and barangay together",
                (string)cellText.Invoke(null, new[] { row, facility }) == "CVMC, Carig Rd, Carig Sur", (string)cellText.Invoke(null, new[] { row, facility }));
            object municipality = Activator.CreateInstance(pc, "place_of_birth", 0f, 0f, 7.5f, 2, false);
            Check("city/municipality box still prints only the municipality", (string)cellText.Invoke(null, new[] { row, municipality }) == "Tuguegarao City");

            object time = Activator.CreateInstance(pc, "time_of_birth", 0f, 0f, 7.5f, -1, false);
            pc.GetField("IsTime").SetValue(time, true);
            Check("time of birth prints as 3:15 PM, not 15:15", (string)cellText.Invoke(null, new[] { row, time }) == "3:15 PM", (string)cellText.Invoke(null, new[] { row, time }));
            row["time_of_birth"] = "";
            Check("a blank time prints blank", (string)cellText.Invoke(null, new[] { row, time }) == "");
            row["place_of_birth_house"] = ""; row["place_of_birth_barangay"] = "";
            Check("a facility with no address prints just its name", (string)cellText.Invoke(null, new[] { row, facility }) == "CVMC");
        }

        // ---------------------------------------------------------------- build
        private static void Build()
        {
            Console.WriteLine("\n[2] Form 102 entry tabs");
            _f = new BirthRegistrationForm();
            _host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(1500, 900) };
            _f.TopLevel = false; _f.FormBorderStyle = FormBorderStyle.None; _f.Dock = DockStyle.Fill;
            _host.Controls.Add(_f); _host.Show(); _f.Show();
            // The entry card is hidden while the list shows, and a TabControl that has no window handle
            // never raises SelectedIndexChanged. In the real app the popup shows it; do the same here.
            G<Control>("cardForm").Visible = true;
            IntPtr hnd = G<TabControl>("tabControl").Handle;
            for (int i = 0; i < 5; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }

            var order = G<ComboBox>("_cboBirthOrder");
            Check("birth order is a pick-list 1st..20th (no typing)",
                order.DropDownStyle == ComboBoxStyle.DropDownList && order.Items.Count == 21 && (string)order.Items[1] == "1st" && (string)order.Items[20] == "20th",
                order.Items.Count + " items");
            var multi = G<ComboBox>("cboMultiple");
            var type = G<ComboBox>("cboTypeOfBirth");
            Check("5b greyed for a single birth", !multi.Enabled);
            type.Text = "Twin"; Application.DoEvents();
            Check("5b opens for a twin", multi.Enabled);
            type.Text = "Single"; Application.DoEvents();
            Check("5b greys and clears again for Single", !multi.Enabled && multi.SelectedIndex == -1);

            var tbl = G<TableLayoutPanel>("tblChild");
            Func<string, int> row = name => tbl.GetRow((Control)tbl.Controls.Cast<Control>().First(c => c.Name == name || c.Controls.Find(name, true).Length > 0));
            Check("Child tab follows the form: place of birth sits above type of birth, hospital address between them",
                row("txtPlace") < row("txtPlaceAddr") && row("txtPlaceAddr") < row("cboTypeOfBirth") && row("txtPlace") == 3,
                "place r" + row("txtPlace") + ", address r" + row("txtPlaceAddr") + ", type r" + row("cboTypeOfBirth"));
            var cert = G<TableLayoutPanel>("tblCert");
            Func<TableLayoutPanel, Control, int> rowOf = (t, c) =>
            {
                while (c != null && c.Parent != t) c = c.Parent;       // inputs may sit inside a field-message wrapper
                return c == null ? -1 : t.GetRow(c);
            };
            Check("registration fee boxes live on the Certification tab, right under Registry/Status",
                rowOf(cert, G<TextBox>("txtFee")) == 2 && rowOf(cert, G<TextBox>("txtFeeOr")) == 2,
                "fee r" + rowOf(cert, G<TextBox>("txtFee")) + ", O.R. r" + rowOf(cert, G<TextBox>("txtFeeOr")));
            var mother = G<TableLayoutPanel>("tblMother");
            int rowAlive = rowOf(mother, G<TextBox>("txtMBornAlive")), rowOcc = rowOf(mother, G<ComboBox>("_cboMOcc")), rowAge = rowOf(mother, G<TextBox>("txtMAge"));
            Check("mother tab: children counts (10a-c) come before occupation and age (11, 12)",
                rowAlive >= 0 && rowAlive < rowOcc && rowOcc == rowAge, "10a r" + rowAlive + ", 11 r" + rowOcc + ", 12 r" + rowAge);
        }

        // ---------------------------------------------------------------- the full round trip
        private static void FillMinimum(string first)
        {
            ((TextBox)G<TextBox>("txtFirstName")).Text = first;
            G<TextBox>("txtLastName").Text = Tag;
            G<ComboBox>("cboSex").SelectedItem = G<ComboBox>("cboSex").Items.Cast<object>().First(i => i.ToString() == "Male");
        }

        private static void RoundTrip()
        {
            Console.WriteLine("\n[3] Save + reload: time, hospital address, 5b, birth order, fee");
            Call("ClearForm");
            FillMinimum(Tag + "A");
            G<DateTimePicker>("dtpDob").Value = DateTime.Today.AddDays(-5);
            var tob = G<DateTimePicker>("dtpTimeOfBirth");
            tob.Value = DateTime.Today.AddHours(15).AddMinutes(15); tob.Checked = true;

            var pob = G<ComboBox[]>("_pob");
            var country = G<ComboBox>("_pobCountry");
            GeoLookup.Select(country, GeoLookup.HomeCountry);
            GeoLookup.Select(pob[1], "Cagayan");
            Check("province loaded municipalities", pob[2].Items.Count > 1, pob[2].Items.Count + " items");
            string muni = (string)pob[2].Items[1];
            GeoLookup.Select(pob[2], muni);
            var hb = G<ComboBox>("_hospBarangay");
            Check("hospital barangay list follows the municipality", hb.Items.Count > 1, muni + ": " + hb.Items.Count + " items");
            string brgy = (string)hb.Items[1];
            GeoLookup.Select(hb, brgy);
            G<ComboBox>("_hospHouse").Text = Tag + " Hospital Rd";
            DataTable h = Db.Pull("SELECT name FROM hospitals ORDER BY id LIMIT 1");
            string hosp = h.Rows.Count > 0 ? h.Rows[0][0].ToString() : "";
            if (hosp != "") GeoLookup.Select(pob[0], hosp);

            G<ComboBox>("cboTypeOfBirth").Text = "Twin";
            Application.DoEvents();
            G<ComboBox>("cboMultiple").SelectedItem = "2nd";
            var order = G<ComboBox>("_cboBirthOrder");
            order.SelectedItem = "3rd";

            G<TextBox>("txtFee").Text = "250.00";
            G<TextBox>("txtFeeOr").Text = Or1;
            Check("a valid fee passes validation", (bool)Call("ValidateChild"));

            long? id = (long?)Call("Create", "Pending Approval", true);
            Check("record saved", id.HasValue && id.Value > 0, id.HasValue ? id.ToString() : "null");
            if (!id.HasValue) return;
            Ids.Add(id.Value);

            DataRow r = Db.Pull("SELECT * FROM births WHERE id=" + id.Value).Rows[0];
            Check("time_of_birth stored as 15:15", Convert.ToString(r["time_of_birth"]) == "15:15", Convert.ToString(r["time_of_birth"]));
            Check("hospital house + barangay stored in their own columns",
                Convert.ToString(r["place_of_birth_house"]) == Tag + " Hospital Rd" && Convert.ToString(r["place_of_birth_barangay"]) == brgy,
                Convert.ToString(r["place_of_birth_house"]) + " | " + Convert.ToString(r["place_of_birth_barangay"]));
            Check("place_of_birth still the 'facility, province, municipality' triple (no address leaked in)",
                !Convert.ToString(r["place_of_birth"]).Contains(brgy) && !Convert.ToString(r["place_of_birth"]).Contains("Hospital Rd"), Convert.ToString(r["place_of_birth"]));
            Check("5b '2nd' and birth order '3rd' stored",
                Convert.ToString(r["multiple_birth_order"]) == "2nd" && Convert.ToString(r["birth_order"]) == "3rd");

            DataTable pay = Db.Pull("SELECT p.id, p.or_number, p.net_amount, p.source, p.source_table, p.source_id, p.payer_name, p.purpose, " +
                                    "(SELECT COUNT(*) FROM payment_items i WHERE i.payment_id = p.id AND i.fee_code='REG-BIRTH') AS items " +
                                    "FROM payments p WHERE p.or_number = @o", new MySqlParameter("@o", Or1));
            Check("exactly one payment, linked to this birth, as 'Birth Registration'",
                pay.Rows.Count == 1 && Convert.ToString(pay.Rows[0]["source"]) == "Birth Registration" &&
                Convert.ToString(pay.Rows[0]["source_table"]) == "births" && Convert.ToInt64(pay.Rows[0]["source_id"]) == id.Value,
                pay.Rows.Count + " rows");
            if (pay.Rows.Count == 1)
            {
                Check("amount 250.00 with one REG-BIRTH fee line", Convert.ToDecimal(pay.Rows[0]["net_amount"]) == 250m && Convert.ToInt32(pay.Rows[0]["items"]) == 1);
                Check("purpose names the child", Convert.ToString(pay.Rows[0]["purpose"]).Contains("Birth registration") && Convert.ToString(pay.Rows[0]["purpose"]).Contains(Tag));
            }

            // reload into the form
            Call("ClearForm");
            Call("LoadBirth", (int)id.Value);
            Check("reload: time of birth shows 15:15 ticked", G<DateTimePicker>("dtpTimeOfBirth").Checked && G<DateTimePicker>("dtpTimeOfBirth").Value.ToString("HH:mm") == "15:15");
            Check("reload: hospital address, 5b and birth order come back",
                G<ComboBox>("_hospBarangay").Text == brgy && G<ComboBox>("_hospHouse").Text == Tag + " Hospital Rd" &&
                G<ComboBox>("cboMultiple").Text == "2nd" && G<ComboBox>("_cboBirthOrder").Text == "3rd");
            Check("reload: the recorded fee is shown and locked (an O.R. is issued once)",
                G<TextBox>("txtFee").Text == "250.00" && G<TextBox>("txtFeeOr").Text == Or1 && G<TextBox>("txtFee").ReadOnly && G<TextBox>("txtFeeOr").ReadOnly);

            // updating the record must not record the fee a second time
            Call("RecordRegistrationFee", id.Value, "Pending Approval", (IWin32Window)_f);
            int again = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM payments WHERE or_number=@o", new MySqlParameter("@o", Or1)).Rows[0][0]);
            Check("saving again does not double-record the fee", again == 1, again + " payments");
        }

        private static void DraftAndDuplicate()
        {
            Console.WriteLine("\n[4] Draft spends no O.R.; a used O.R. is refused");
            Call("ClearForm");
            FillMinimum(Tag + "B");
            string orDraft = Tag + "-OR2";
            G<TextBox>("txtFee").Text = "100"; G<TextBox>("txtFeeOr").Text = orDraft;
            long? did = (long?)Call("Create", "Draft", true);
            if (did.HasValue) Ids.Add(did.Value);
            int n = Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM payments WHERE or_number=@o", new MySqlParameter("@o", orDraft)).Rows[0][0]);
            Check("a Draft records no payment", did.HasValue && n == 0, n + " payments");

            Call("ClearForm");
            FillMinimum(Tag + "C");
            G<TextBox>("txtFee").Text = "100"; G<TextBox>("txtFeeOr").Text = Or1;   // already used by record A
            Check("validation refuses an O.R. already in the log", !(bool)Call("ValidateChild"));

            Call("ClearForm");
            FillMinimum(Tag + "D");
            G<TextBox>("txtFee").Text = "100"; G<TextBox>("txtFeeOr").Text = "";
            Check("amount without an O.R. is refused", !(bool)Call("ValidateChild"));
            Call("ClearForm");
            FillMinimum(Tag + "E");
            G<TextBox>("txtFee").Text = ""; G<TextBox>("txtFeeOr").Text = "";
            Check("no fee at all is fine (nothing collected)", (bool)Call("ValidateChild"));
        }

        private static void Informant()
        {
            Console.WriteLine("\n[5] Informant pre-filled from the father");
            Call("ClearForm");
            var tabs = G<TabControl>("tabControl");
            G<TextBox>("txtFFirst").Text = "Alberto"; G<TextBox>("txtFMiddle").Text = "Buraga"; G<TextBox>("txtFLast").Text = "Talosig";
            var fres = G<ComboBox[]>("_fres");
            GeoLookup.Select(fres[1], "Cagayan");
            string muni = (string)fres[2].Items[1];
            GeoLookup.Select(fres[2], muni);
            string brgy = (string)fres[3].Items[1];
            GeoLookup.Select(fres[3], brgy);

            tabs.SelectedTab = G<TabPage>("tabInformant");
            Application.DoEvents();
            Check("opening the Informant tab fills name from the father", G<TextBox>("txtInfName").Text == "Alberto Buraga Talosig", G<TextBox>("txtInfName").Text);
            Check("relationship set to Father", G<ComboBox>("_cboInfRel").Text == "Father", G<ComboBox>("_cboInfRel").Text);
            var inf = G<ComboBox[]>("_infAddr");
            Check("address copied from the father's residence", inf[0].Text == "Cagayan" && inf[1].Text == muni && inf[2].Text == brgy, inf[0].Text + "/" + inf[1].Text + "/" + inf[2].Text);
            Check("a line says it was pre-filled", G<Label>("lblInformantHint").Visible);

            G<TextBox>("txtInfName").Text = "Someone Else";
            Check("changing the name drops the 'pre-filled' line", !G<Label>("lblInformantHint").Visible);
            tabs.SelectedTab = G<TabPage>("tabChild"); tabs.SelectedTab = G<TabPage>("tabInformant");
            Check("what the operator typed is never overwritten", G<TextBox>("txtInfName").Text == "Someone Else");

            // a father with no name leaves the informant alone
            Call("ClearForm");
            tabs.SelectedTab = G<TabPage>("tabChild"); tabs.SelectedTab = G<TabPage>("tabInformant");
            Check("no father entered -> informant stays blank", G<TextBox>("txtInfName").Text == "");

            // choosing Father as the relationship fills an empty informant
            Call("ClearForm");
            G<TextBox>("txtFFirst").Text = "Jose"; G<TextBox>("txtFLast").Text = "Dela Cruz";
            var rel = G<ComboBox>("_cboInfRel");
            int idx = rel.Items.IndexOf("Father");
            if (idx >= 0) rel.SelectedIndex = idx;
            Check("picking Father as the relationship fills the name", G<TextBox>("txtInfName").Text == "Jose Dela Cruz", G<TextBox>("txtInfName").Text);
        }

        private static void AttendantOthers()
        {
            Console.WriteLine("\n[6] Attendant: Others + specify");
            Call("ClearForm");
            var tabs = G<TabControl>("tabControl");
            tabs.SelectedTab = G<TabPage>("tabAttendant"); Application.DoEvents();
            var type = G<ComboBox>("cboAttType");
            Check("attendant list offers Others", type.Items.Cast<object>().Any(i => i.ToString() == "Others"));
            var other = G<TextBox>("txtAttTypeOther");
            Check("specify box hidden until Others is picked", !other.Visible);
            type.SelectedItem = "Others"; Application.DoEvents();
            Check("picking Others shows the specify box", other.Visible);
            var tbl = G<TableLayoutPanel>("tblAttendant");
            Check("specify box sits on the SAME row as the attendant type", tbl.GetRow(other) == tbl.GetRow(type));
            other.Text = "Traditional Birth Attendant";
            var ps = (MySqlParameter[])typeof(BirthRegistrationForm).GetMethod("FieldParams", NF).Invoke(_f, new object[] { "Draft" });
            Check("stored as 'Others - <what was typed>'", Convert.ToString(ps.First(p => p.ParameterName == "@atype").Value) == "Others - Traditional Birth Attendant");
        }

        // ---------------------------------------------------------------- cleanup
        private static void Cleanup()
        {
            DataTable ids = Db.Pull("SELECT id FROM births WHERE last_name = @t", new MySqlParameter("@t", Tag));
            string list = string.Join(",", ids.AsEnumerable().Select(r => r[0].ToString()).Concat(Ids.Select(i => i.ToString())).Distinct().DefaultIfEmpty("0"));
            // Payments by THIS run's O.R. numbers only - an id alone is not an identity once rows are deleted.
            Db.Push("DELETE FROM audit_log WHERE table_name = 'payments' AND details LIKE @o", new MySqlParameter("@o", "%O.R. " + Tag + "%"));
            Db.Push("DELETE FROM payment_items WHERE payment_id IN (SELECT id FROM payments WHERE or_number LIKE @o)", new MySqlParameter("@o", Tag + "-OR%"));
            Db.Push("DELETE FROM payments WHERE or_number LIKE @o", new MySqlParameter("@o", Tag + "-OR%"));
            Db.Push("DELETE FROM audit_log WHERE table_name = 'births' AND record_id IN (" + list + ") AND details LIKE @t", new MySqlParameter("@t", "%" + Tag + "%"));
            Db.Push("DELETE FROM document_requirements WHERE owner_type = 'Birth' AND owner_id IN (" + list + ")");
            Db.Push("DELETE FROM births WHERE id IN (" + list + ")");
        }

        private static int Leftovers()
        {
            return Convert.ToInt32(Db.Pull(
                "SELECT (SELECT COUNT(*) FROM births WHERE last_name = @t) + (SELECT COUNT(*) FROM payments WHERE or_number LIKE @o)",
                new MySqlParameter("@t", Tag), new MySqlParameter("@o", Tag + "-OR%")).Rows[0][0]);
        }
    }
}
