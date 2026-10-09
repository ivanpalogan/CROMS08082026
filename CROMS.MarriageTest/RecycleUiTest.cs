using System;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Forms;
using MySql.Data.MySqlClient;

namespace CROMS.MarriageTest
{
    /// <summary>
    /// The real Birth Registration "Delete" button and the real Old Birth Records delete, driven the
    /// way an operator does it: confirm, give a reason (or cancel the reason box), and the record is
    /// archived - or left untouched. Dialogs are answered by <see cref="DialogWatchdog"/>.
    /// </summary>
    internal static class RecycleUiTest
    {
        private const BindingFlags NF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static string _reasonMode = "ok";
        private const string Reason = "UI test delete reason";

        private static long Count(string sql, params MySqlParameter[] ps) { return RecycleTest.Count(sql, ps); }
        private static void Check(string n, bool ok, string d = null) { RecycleTest.Check(n, ok, d); }

        /// <summary>
        /// Intelligent Document Processing: Commit refuses a blank Registry Number (the record's legal
        /// key), a filled one passes. Save as Draft is not gated, so only the Commit check is exercised.
        /// </summary>
        private static void RegistryRequired()
        {
            Console.WriteLine("\n[OCR] Commit needs a registry number");
            Assembly asm = typeof(OcrDigitizationForm).Assembly;
            Type ft = typeof(OcrDigitizationForm);
            Type kindT = asm.GetType("CROMS.Data.DocKind", true), dfT = asm.GetType("CROMS.Data.DocField", true), drT = asm.GetType("CROMS.Data.DocAiResult", true);
            Type fc = asm.GetType("CROMS.Data.FormCatalog", true);
            var form = new OcrDigitizationForm();
            try
            {
                foreach (string kind in new[] { "Birth", "Death", "Marriage" })
                {
                    object k = Enum.Parse(kindT, kind);
                    ft.GetField("_kind", NF).SetValue(form, k);
                    ft.GetField("_formDef", NF).SetValue(form, fc.GetMethod("Current").Invoke(null, new object[] { k }));
                    MethodInfo check = ft.GetMethod("RegistryNumberPresent", NF);

                    Func<string, bool> present = value =>
                    {
                        object r = Activator.CreateInstance(drT);
                        ((System.Collections.IList)drT.GetField("Fields").GetValue(r)).Add(Activator.CreateInstance(dfT, new object[] { "RegistryNo", "Registry No.", value, false }));
                        ft.GetField("_result", NF).SetValue(form, r);
                        return (bool)check.Invoke(form, new object[0]);
                    };
                    Check(kind + ": a blank registry number is refused by Commit", !present(""));
                    Check(kind + ": a spaces-only registry number is refused", !present("   "));
                    Check(kind + ": a typed registry number passes", present("2005-297"));
                }
            }
            finally { form.Dispose(); }
        }

        /// <summary>
        /// What a clerk sees when something fails: the sentence the code wrote itself is shown as
        /// written; a database or program error is NOT shown raw (it goes to the error log).
        /// </summary>
        private static void ErrorMessages()
        {
            Console.WriteLine("\n[Errors] what the clerk sees");
            Type el = typeof(OcrDigitizationForm).Assembly.GetType("CROMS.Data.ErrorLog", true);
            Func<string, Exception, string> call = (m, ex) => (string)el.GetMethod(m, BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { ex });

            Check("a rule message is shown as written (Text)", call("Text", new InvalidOperationException("Only an administrator can do that.")) == "Only an administrator can do that.");
            Check("a rule message is shown as written (Reason)", call("Reason", new ArgumentException("Pick a window first.")) == "Pick a window first.");
            string npe = call("Text", new NullReferenceException("Object reference not set to an instance of an object SECRET-DETAIL"));
            Check("a program error is not shown raw", !npe.Contains("SECRET-DETAIL") && !npe.Contains("Object reference") && npe.StartsWith("Sorry"), npe);
            string disposed = call("Text", new ObjectDisposedException("SomeForm"));
            Check("an ObjectDisposedException (an InvalidOperationException subclass) is not shown raw", disposed.StartsWith("Sorry"), disposed);

            Exception missing = null;
            try { Db.Pull("SELECT * FROM table_that_does_not_exist_zzr"); } catch (Exception ex) { missing = ex; }
            string t = call("Text", missing), r = call("Reason", missing);
            Check("a missing-table SQL error says the database needs updating (Text)", missing != null && t.Contains("has not been updated"), t);
            Check("... and reads after a lead-in (Reason)", r.StartsWith("this computer's database has not been updated") && !r.Contains("table_that_does_not_exist_zzr"), r);

            string log = System.IO.File.Exists(ErrLogPath()) ? System.IO.File.ReadAllText(ErrLogPath()) : "";
            Check("the technical detail went to the error log", log.Contains("SECRET-DETAIL"));
        }

        private static string ErrLogPath() { return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "croms-error.log"); }

        public static void Run()
        {
            Console.WriteLine("\n[UI] Delete buttons on the real screens");
            var wd = new DialogWatchdog();
            wd.Rules["ReasonPromptForm"] = f =>
            {
                if (_reasonMode == "ok")
                {
                    ((TextBox)f.GetType().GetField("txtReason", NF).GetValue(f)).Text = Reason;
                    ((Button)f.GetType().GetField("btnOk", NF).GetValue(f)).PerformClick();
                }
                else { f.DialogResult = DialogResult.Cancel; f.Close(); }
            };
            wd.Start();
            Form host = null;
            try
            {
                long id = RecycleTest.Birth("2099-B-9101", "Registration", null);
                var form = new BirthRegistrationForm();
                host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(1500, 900) };
                form.TopLevel = false; form.Dock = DockStyle.Fill;
                wd.Ignore.Add(host.Handle);     // the watchdog closes any top-level form it does not know - tell it BEFORE the host is shown
                host.Controls.Add(form); host.Show(); form.Show();
                MethodInfo del = typeof(BirthRegistrationForm).GetMethod("btnDelete_Click", NF);
                FieldInfo editing = typeof(BirthRegistrationForm).GetField("_editingId", NF);

                _reasonMode = "cancel";
                editing.SetValue(form, (int?)(int)id);
                del.Invoke(form, new object[] { null, EventArgs.Empty });
                Check("Birth: cancelling the reason box deletes nothing",
                    Count("SELECT COUNT(*) FROM births WHERE id=@id", new MySqlParameter("@id", id)) == 1 &&
                    Count("SELECT COUNT(*) FROM deleted_records WHERE source_table='births' AND record_id=@id", new MySqlParameter("@id", id)) == 0);

                _reasonMode = "ok";
                editing.SetValue(form, (int?)(int)id);
                del.Invoke(form, new object[] { null, EventArgs.Empty });
                Check("Birth: delete with a reason removes the record",
                    Count("SELECT COUNT(*) FROM births WHERE id=@id", new MySqlParameter("@id", id)) == 0);
                DataTable dl = Db.Pull("SELECT * FROM deleted_records WHERE source_table='births' AND record_id=@id", new MySqlParameter("@id", id));
                Check("Birth: the archive row carries the typed reason", dl.Rows.Count == 1 && Convert.ToString(dl.Rows[0]["reason"]) == Reason);
                Check("Birth: the audit trail names the reason",
                    Count("SELECT COUNT(*) FROM audit_log WHERE table_name='births' AND action='Delete' AND record_id=@r AND details LIKE @d",
                        new MySqlParameter("@r", id.ToString()), new MySqlParameter("@d", "%" + Reason + "%")) == 1);
                if (dl.Rows.Count == 1)
                {
                    RecordRecycle.Restore(Convert.ToInt32(dl.Rows[0]["id"]));
                    Check("Birth: and it can be restored", Count("SELECT COUNT(*) FROM births WHERE id=@id", new MySqlParameter("@id", id)) == 1);
                }

                // ---- the digitized backlog screen
                long oid = RecycleTest.Birth("2099-B-9102", "OCR-Backlog", null);
                var old = new OldBirthRecordsForm();
                MethodInfo doDelete = typeof(OldBirthRecordsForm).GetMethod("DoDelete", NF);
                _reasonMode = "cancel";
                bool r1 = (bool)doDelete.Invoke(old, new object[] { oid });
                Check("Old Birth: cancelling the reason box deletes nothing",
                    !r1 && Count("SELECT COUNT(*) FROM births WHERE id=@id", new MySqlParameter("@id", oid)) == 1);
                _reasonMode = "ok";
                bool r2 = (bool)doDelete.Invoke(old, new object[] { oid });
                Check("Old Birth: delete with a reason archives the record",
                    r2 && Count("SELECT COUNT(*) FROM births WHERE id=@id", new MySqlParameter("@id", oid)) == 0 &&
                    Count("SELECT COUNT(*) FROM deleted_records WHERE source_table='births' AND record_id=@id AND reason=@w",
                        new MySqlParameter("@id", oid), new MySqlParameter("@w", Reason)) == 1);

                long reg = RecycleTest.Birth("2099-B-9103", "Registration", null);
                bool r3 = (bool)doDelete.Invoke(old, new object[] { reg });
                Check("Old Birth: a registered record is refused",
                    !r3 && Count("SELECT COUNT(*) FROM births WHERE id=@id", new MySqlParameter("@id", reg)) == 1);
                old.Dispose();
                form.Dispose();

                RegistryRequired();
                ErrorMessages();
            }
            finally
            {
                wd.Stop();
                try { if (host != null) host.Close(); } catch { }
                Db.Push("DELETE FROM audit_log WHERE table_name='births' AND (details LIKE @d OR details LIKE 'Restored from Deleted Records: ZZR%')",
                    new MySqlParameter("@d", "%" + Reason + "%"));
                Console.WriteLine("  (dialogs handled: " + wd.Count + ")");
            }
        }
    }
}
