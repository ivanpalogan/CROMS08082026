using System;
using System.Data;
using System.Linq;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.MarriageTest
{
    /// <summary>
    /// Delete-with-undo for civil registry records (RecordRecycle, migration 86), against the LIVE
    /// database: a delete needs a reason, archives the whole row (scan images and requirement rows
    /// included) and removes it in one step; the registry number of a deleted record is not reused;
    /// a restore brings back an IDENTICAL row, and refuses (with a sentence) when the record number
    /// or registry number has been taken since. Every row is tagged ZZR and removed afterwards.
    /// </summary>
    internal static class RecycleTest
    {
        public static int Pass, Fail;
        private const string Tag = "ZZR";

        internal static void Check(string name, bool ok, string detail = null)
        {
            if (ok) Pass++; else Fail++;
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + (detail == null ? "" : "   -> " + detail));
        }

        internal static long Birth(string registry, string source, byte[] image)
        {
            var img = new MySqlParameter("@img", MySqlDbType.LongBlob) { Value = (object)image ?? DBNull.Value };
            return Db.Insert(
                "INSERT INTO births (first_name, last_name, sex, date_of_birth, status, registry_no, record_source, birth_image, scan_image) " +
                "VALUES (@f, @l, 'Female', @dob, 'Registered', @reg, @src, @img, @img)",
                new MySqlParameter("@f", Tag + "First"), new MySqlParameter("@l", Tag + "Last"),
                new MySqlParameter("@dob", new DateTime(2001, 3, 4)),
                new MySqlParameter("@reg", (object)registry ?? DBNull.Value),
                new MySqlParameter("@src", source), img);
        }

        internal static long Count(string sql, params MySqlParameter[] ps)
        {
            return Convert.ToInt64(Db.Pull(sql, ps).Rows[0][0]);
        }

        private static bool SameRow(DataRow a, DataRow b)
        {
            for (int i = 0; i < a.ItemArray.Length; i++)
            {
                object x = a[i], y = b[i];
                if (x is byte[] && y is byte[]) { if (!((byte[])x).SequenceEqual((byte[])y)) return false; }
                else if (!Equals(x, y)) return false;
            }
            return true;
        }

        private static string Thrown(Action a)
        {
            try { a(); return null; }
            catch (Exception ex) { return ex is InvalidOperationException ? "InvalidOperation: " + ex.Message : ex.GetType().Name + ": " + ex.Message; }
        }

        public static void Run()
        {
            int year = DateTime.Now.Year;
            var rnd = new Random(7);
            byte[] img = new byte[60000]; rnd.NextBytes(img);

            // ---- setup: a registered birth with scans and one requirement row
            long id1 = Birth("2099-B-9001", "Registration", img);
            Db.Push("INSERT INTO document_requirements (owner_type, owner_id, req_code) VALUES ('Birth', @id, 'ZZR_REQ')",
                new MySqlParameter("@id", id1));
            DataRow before = Db.Pull("SELECT * FROM births WHERE id = @id", new MySqlParameter("@id", id1)).Rows[0];

            // ---- the rules
            Check("a reason is required",
                (Thrown(() => RecordRecycle.Delete("births", id1, "  ")) ?? "").StartsWith("InvalidOperation"));
            Check("record still there after the refused delete", Count("SELECT COUNT(*) FROM births WHERE id = @id", new MySqlParameter("@id", id1)) == 1);
            string wrong = Thrown(() => RecordRecycle.Delete("births", id1, "x", "OCR-Backlog"));
            Check("a registered record cannot be deleted from the backlog screens", (wrong ?? "").StartsWith("InvalidOperation"), wrong);
            Check("table name is whitelisted", (Thrown(() => RecordRecycle.Delete("users", 1, "x")) ?? "").StartsWith("ArgumentException"));

            // ---- delete
            RecordRecycle.Delete("births", id1, "entered twice by mistake");
            Check("record is gone from its table", Count("SELECT COUNT(*) FROM births WHERE id = @id", new MySqlParameter("@id", id1)) == 0);
            Check("its requirement row went with it", Count("SELECT COUNT(*) FROM document_requirements WHERE owner_type='Birth' AND owner_id=@id", new MySqlParameter("@id", id1)) == 0);
            DataTable dl = Db.Pull("SELECT * FROM deleted_records WHERE source_table='births' AND record_id=@id", new MySqlParameter("@id", id1));
            Check("exactly one archive row", dl.Rows.Count == 1);
            DataRow d = dl.Rows[0];
            Check("archive keeps the reason, registry number and label",
                Convert.ToString(d["reason"]) == "entered twice by mistake" && Convert.ToString(d["registry_no"]) == "2099-B-9001" &&
                Convert.ToString(d["record_label"]) == Tag + "Last, " + Tag + "First");
            Check("archive records who deleted it", d["deleted_by"] != DBNull.Value && Convert.ToString(d["deleted_by_name"]).Length > 0);
            Check("archive holds the requirement rows", Convert.ToString(d["children_json"]).Contains("ZZR_REQ"));
            Check("archive holds the scan images", Convert.ToString(d["row_json"]).Length > 60000);
            int delId = Convert.ToInt32(d["id"]);
            Check("the log lists it", RecordRecycle.List(false).Rows.Cast<DataRow>().Any(r => Convert.ToInt32(r["id"]) == delId));

            // ---- the registry number of a deleted record is not handed out again
            long id9 = Birth(year + "-B-9777", "OCR-Backlog", null);
            RecordRecycle.Delete("births", id9, "test of number reservation", "OCR-Backlog");
            string next = RegistryNumber.Next("births", 'B');
            Check("a deleted record's number stays reserved", next == year + "-B-9778", next);

            // ---- restore
            RecordRecycle.Restored rr = RecordRecycle.Restore(delId);
            Check("restore reports what it brought back", rr.Table == "births" && rr.RecordId == id1 && rr.RegistryNo == "2099-B-9001");
            DataTable back = Db.Pull("SELECT * FROM births WHERE id = @id", new MySqlParameter("@id", id1));
            Check("restored row exists with its original id", back.Rows.Count == 1);
            Check("restored row is IDENTICAL to the original (every column, scans included)", back.Rows.Count == 1 && SameRow(before, back.Rows[0]));
            Check("its requirement row is back", Count("SELECT COUNT(*) FROM document_requirements WHERE owner_type='Birth' AND owner_id=@id AND req_code='ZZR_REQ'", new MySqlParameter("@id", id1)) == 1);
            Check("archive row is marked restored", Count("SELECT COUNT(*) FROM deleted_records WHERE id=@id AND restored_at IS NOT NULL", new MySqlParameter("@id", delId)) == 1);
            string again = Thrown(() => RecordRecycle.Restore(delId));
            Check("a second restore is refused with a sentence", (again ?? "").StartsWith("InvalidOperation"), again);
            Check("the log hides restored rows by default", !RecordRecycle.List(false).Rows.Cast<DataRow>().Any(r => Convert.ToInt32(r["id"]) == delId));

            // ---- restore refuses when the registry number has been taken
            RecordRecycle.Delete("births", id1, "second delete");
            long other = Birth("2099-B-9001", "Registration", null);          // someone took the number
            int delId2 = Convert.ToInt32(Db.Pull("SELECT MAX(id) FROM deleted_records WHERE source_table='births' AND record_id=@id", new MySqlParameter("@id", id1)).Rows[0][0]);
            string clash = Thrown(() => RecordRecycle.Restore(delId2));
            Check("restore refuses a registry number now used by another record", (clash ?? "").StartsWith("InvalidOperation") && clash.Contains("2099-B-9001"), clash);
            Check("nothing was written by the refused restore", Count("SELECT COUNT(*) FROM births WHERE id=@id", new MySqlParameter("@id", id1)) == 0);

            // ---- ... or the record number
            Db.Push("DELETE FROM births WHERE id = @id", new MySqlParameter("@id", other));
            Db.Push("INSERT INTO births (id, first_name, last_name, sex, date_of_birth, status, registry_no) VALUES (@id, @f, @l, 'Male', @dob, 'Draft', NULL)",
                new MySqlParameter("@id", id1), new MySqlParameter("@f", Tag + "Squatter"), new MySqlParameter("@l", Tag + "Last"),
                new MySqlParameter("@dob", new DateTime(2001, 3, 4)));
            string idClash = Thrown(() => RecordRecycle.Restore(delId2));
            Check("restore refuses a record number now used by another record", (idClash ?? "").StartsWith("InvalidOperation") && idClash.Contains(id1.ToString()), idClash);
            Db.Push("DELETE FROM births WHERE id = @id", new MySqlParameter("@id", id1));
            RecordRecycle.Restore(delId2);
            Check("restores once the way is clear", Count("SELECT COUNT(*) FROM births WHERE id=@id AND registry_no='2099-B-9001'", new MySqlParameter("@id", id1)) == 1);
        }

        public static void Cleanup()
        {
            Db.Push("DELETE FROM document_requirements WHERE req_code = 'ZZR_REQ'");
            Db.Push("DELETE FROM births WHERE last_name = @t OR first_name = @f OR registry_no LIKE '2099-B-9%'",
                new MySqlParameter("@t", Tag + "Last"), new MySqlParameter("@f", Tag + "First"));
            Db.Push("DELETE FROM deleted_records WHERE record_label LIKE 'ZZR%' OR registry_no LIKE '2099-B-9%' OR registry_no LIKE '%-B-9777'");
        }

        public static int Leftovers()
        {
            return (int)(Count("SELECT COUNT(*) FROM births WHERE last_name = 'ZZRLast' OR first_name = 'ZZRFirst'") +
                         Count("SELECT COUNT(*) FROM deleted_records WHERE record_label LIKE 'ZZR%' OR registry_no LIKE '2099-B-9%' OR registry_no LIKE '%-B-9777'") +
                         Count("SELECT COUNT(*) FROM document_requirements WHERE req_code = 'ZZR_REQ'"));
        }
    }
}
