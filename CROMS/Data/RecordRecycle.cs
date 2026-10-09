using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// Delete for a civil registry record (birth, death, marriage) that can always be undone.
    ///
    /// <para>The row is copied, whole, into <c>deleted_records</c> - with who removed it, when and
    /// why - and removed from its own table in the SAME transaction, so a record is never lost to
    /// a half-finished delete. <see cref="Restore"/> puts it back with its original id.</para>
    ///
    /// <para>WHY A SNAPSHOT AND NOT AN is_deleted FLAG. A flag only protects the registry if every
    /// one of the ~150 statements that read these tables (screens, certificate views, the PSA
    /// monthly counts, analytics, the archive browser) remembers to exclude flagged rows; one
    /// forgotten filter prints a "deleted" record on a certificate or counts it in the return
    /// sent to PSA. Once the row is really gone from the table there is nothing to forget.</para>
    ///
    /// <para>The copy stores each column as a [name, type, value] triple rather than as a fixed
    /// column list, so a column added by a later migration neither breaks nor is lost by a
    /// restore: columns that no longer exist are skipped, new ones take their default.</para>
    /// </summary>
    public static class RecordRecycle
    {
        /// <summary>What a restore brought back (for the audit line).</summary>
        public sealed class Restored
        {
            public string Table;
            public int RecordId;
            public string Label;
            public string RegistryNo;
        }

        // ------------------------------------------------------------------ delete
        /// <summary>
        /// Archives and removes one record. Throws <see cref="InvalidOperationException"/> with a
        /// sentence for the operator when it cannot (no reason, already gone, wrong kind of
        /// record); a database failure - including a foreign key from another table (1451) -
        /// rolls everything back and propagates, leaving the record untouched.
        /// </summary>
        /// <param name="requireSource">When set (e.g. "OCR-Backlog"), only a record whose
        /// <c>record_source</c> equals it may be deleted - the digitized-backlog screens use this
        /// so they can never remove a registered record.</param>
        public static void Delete(string table, long id, string reason, string requireSource = null)
        {
            table = Table(table);
            reason = (reason ?? "").Trim();
            if (reason.Length == 0)
                throw new InvalidOperationException("A reason is required to delete a record.");
            if (reason.Length > 255) reason = reason.Substring(0, 255);

            Tx((c, t) =>
            {
                DataTable row = Fill(c, t, "SELECT * FROM `" + table + "` WHERE id = @id FOR UPDATE",
                    new MySqlParameter("@id", id));
                if (row.Rows.Count == 0)
                    throw new InvalidOperationException(
                        "This record no longer exists - it may already have been deleted from another computer.");
                DataRow r = row.Rows[0];

                if (requireSource != null && !string.Equals(Str(r, "record_source"), requireSource, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Only a digitized backlog record can be deleted from this screen. " +
                        "A registered record is corrected in its own registration module.");

                string children = null;
                string kind = OwnerKind(table);
                if (kind != null)
                {
                    DataTable ch = Fill(c, t,
                        "SELECT * FROM document_requirements WHERE owner_type = @k AND owner_id = @id",
                        new MySqlParameter("@k", kind), new MySqlParameter("@id", id));
                    if (ch.Rows.Count > 0)
                    {
                        children = Json(ch.Rows.Cast<DataRow>().Select(x => Triples(x, ch)).ToList());
                        Exec(c, t, "DELETE FROM document_requirements WHERE owner_type = @k AND owner_id = @id",
                            new MySqlParameter("@k", kind), new MySqlParameter("@id", id));
                    }
                }

                Exec(c, t,
                    "INSERT INTO deleted_records (source_table, record_id, registry_no, record_label, reason, " +
                    "deleted_by, deleted_by_name, row_json, children_json) " +
                    "VALUES (@st, @rid, @reg, @lab, @why, @by, @byn, @row, @ch)",
                    new MySqlParameter("@st", table),
                    new MySqlParameter("@rid", id),
                    new MySqlParameter("@reg", Nullable(Str(r, "registry_no"))),
                    new MySqlParameter("@lab", Nullable(Cut(Label(table, r), 200))),
                    new MySqlParameter("@why", reason),
                    new MySqlParameter("@by", Session.UserIdParam),
                    new MySqlParameter("@byn", Nullable(UserName())),
                    new MySqlParameter("@row", Json(Triples(r, row))),
                    new MySqlParameter("@ch", (object)children ?? DBNull.Value));

                Exec(c, t, "DELETE FROM `" + table + "` WHERE id = @id", new MySqlParameter("@id", id));
            });
        }

        // ----------------------------------------------------------------- restore
        /// <summary>
        /// Puts a deleted record back, with its original id and registry number. Refuses (with a
        /// sentence) when either has since been taken by another record - a restore must never
        /// create a second record with the same legal key.
        /// </summary>
        public static Restored Restore(int deletedId)
        {
            Restored result = new Restored();
            Tx((c, t) =>
            {
                DataTable d = Fill(c, t,
                    "SELECT * FROM deleted_records WHERE id = @id AND restored_at IS NULL FOR UPDATE",
                    new MySqlParameter("@id", deletedId));
                if (d.Rows.Count == 0)
                    throw new InvalidOperationException("This record has already been restored (or was not found).");
                DataRow dr = d.Rows[0];

                string table = Table(Str(dr, "source_table"));
                int recId = Convert.ToInt32(dr["record_id"]);
                string registry = Str(dr, "registry_no");

                if (Convert.ToInt64(Scalar(c, t, "SELECT COUNT(*) FROM `" + table + "` WHERE id = @id",
                        new MySqlParameter("@id", recId))) > 0)
                    throw new InvalidOperationException(
                        "Record number " + recId + " is now used by another record, so this one cannot go back with its original number.");
                if (registry.Length > 0 && Convert.ToInt64(Scalar(c, t,
                        "SELECT COUNT(*) FROM `" + table + "` WHERE registry_no = @r",
                        new MySqlParameter("@r", registry))) > 0)
                    throw new InvalidOperationException(
                        "Registry number " + registry + " is now used by another record, so this one cannot be restored. " +
                        "Correct or remove the other record first.");

                HashSet<string> cols = Columns(c, t, table);
                InsertTriples(c, t, table, ParseTriples(Str(dr, "row_json")), cols, keepId: true);

                string childJson = Str(dr, "children_json");
                if (childJson.Length > 0)
                {
                    HashSet<string> ccols = Columns(c, t, "document_requirements");
                    foreach (List<object[]> row in ParseRows(childJson))
                    {
                        try { InsertTriples(c, t, "document_requirements", row, ccols, keepId: true); }
                        catch (MySqlException ex) when (ex.Number == 1062)
                        { InsertTriples(c, t, "document_requirements", row, ccols, keepId: false); }
                    }
                }

                Exec(c, t, "UPDATE deleted_records SET restored_at = NOW(), restored_by = @u WHERE id = @id",
                    new MySqlParameter("@u", Session.UserIdParam), new MySqlParameter("@id", deletedId));

                result.Table = table;
                result.RecordId = recId;
                result.RegistryNo = registry;
                result.Label = Str(dr, "record_label");
            });
            return result;
        }

        // -------------------------------------------------------------------- list
        /// <summary>The deleted-records log, newest first (capped at 500).</summary>
        public static DataTable List(bool includeRestored)
        {
            return Db.Pull(
                "SELECT id, source_table, record_id, registry_no, record_label, reason, deleted_by_name, " +
                "deleted_at, restored_at FROM deleted_records " +
                (includeRestored ? "" : "WHERE restored_at IS NULL ") +
                "ORDER BY id DESC LIMIT 500");
        }

        /// <summary>Friendly name for a table, for messages and the list.</summary>
        public static string KindName(string table)
        {
            switch (table)
            {
                case "births": return "Birth";
                case "deaths": return "Death";
                case "marriages": return "Marriage";
                default: return table;
            }
        }

        // ---------------------------------------------------------------- internals
        private static string Table(string table)
        {
            switch (table)
            {
                case "births":
                case "deaths":
                case "marriages":
                    return table;
                default:
                    throw new ArgumentException("Not a registry table: " + table, "table");
            }
        }

        /// <summary>The owner_type its requirement rows carry (null when the kind has none).</summary>
        private static string OwnerKind(string table)
        {
            return table == "births" ? "Birth" : table == "marriages" ? "Marriage" : null;
        }

        private static string Label(string table, DataRow r)
        {
            switch (table)
            {
                case "births":
                    return Join(Str(r, "last_name"), Str(r, "first_name"), ", ");
                case "deaths":
                    string full = Str(r, "full_name");
                    return full.Length > 0 ? full : Join(Str(r, "last_name"), Str(r, "first_name"), ", ");
                default:
                    return Join(Str(r, "husband_last_name"), Str(r, "wife_last_name"), " / ");
            }
        }

        private static string Join(string a, string b, string sep)
        {
            if (a.Length > 0 && b.Length > 0) return a + sep + b;
            return a.Length > 0 ? a : b;
        }

        private static string Str(DataRow r, string col)
        {
            if (r == null || r.Table == null || !r.Table.Columns.Contains(col)) return "";
            object v = r[col];
            return v == null || v == DBNull.Value ? "" : Convert.ToString(v, CultureInfo.InvariantCulture).Trim();
        }

        private static string Cut(string s, int n) { return s != null && s.Length > n ? s.Substring(0, n) : s; }

        private static object Nullable(string s) { return string.IsNullOrEmpty(s) ? (object)DBNull.Value : s; }

        private static string UserName()
        {
            try
            {
                if (Session.User == null) return null;
                return string.IsNullOrWhiteSpace(Session.User.FullName) ? Session.User.Username : Session.User.FullName;
            }
            catch { return null; }
        }

        // ---- column <-> JSON triples -------------------------------------------------
        private static List<object[]> Triples(DataRow r, DataTable t)
        {
            var list = new List<object[]>();
            foreach (DataColumn col in t.Columns)
            {
                object v = r[col];
                string type, val;
                if (v == null || v == DBNull.Value) { type = "null"; val = null; }
                else if (v is byte[]) { type = "bytes"; val = Convert.ToBase64String((byte[])v); }
                else if (v is DateTime) { type = "datetime"; val = ((DateTime)v).ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture); }
                else if (v is TimeSpan) { type = "time"; val = ((TimeSpan)v).ToString("c", CultureInfo.InvariantCulture); }
                else if (v is bool) { type = "bool"; val = (bool)v ? "1" : "0"; }
                else if (v is decimal) { type = "decimal"; val = ((decimal)v).ToString(CultureInfo.InvariantCulture); }
                else if (v is double || v is float) { type = "double"; val = Convert.ToDouble(v).ToString("R", CultureInfo.InvariantCulture); }
                else if (v is sbyte || v is byte || v is short || v is ushort || v is int || v is uint || v is long || v is ulong)
                { type = "int"; val = Convert.ToString(v, CultureInfo.InvariantCulture); }
                else { type = "string"; val = Convert.ToString(v, CultureInfo.InvariantCulture); }
                list.Add(new object[] { col.ColumnName, type, val });
            }
            return list;
        }

        private static object Decode(string type, string val)
        {
            switch (type)
            {
                case "null": return DBNull.Value;
                case "bytes": return Convert.FromBase64String(val ?? "");
                case "datetime": return DateTime.ParseExact(val, "yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture);
                case "time": return TimeSpan.Parse(val, CultureInfo.InvariantCulture);
                case "bool": return val == "1";
                case "decimal": return decimal.Parse(val, CultureInfo.InvariantCulture);
                case "double": return double.Parse(val, CultureInfo.InvariantCulture);
                case "int":
                    long l;
                    if (long.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out l)) return l;
                    return ulong.Parse(val, CultureInfo.InvariantCulture);
                default: return val ?? "";
            }
        }

        private static JavaScriptSerializer Ser()
        {
            return new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 20 };
        }

        private static string Json(object o) { return Ser().Serialize(o); }

        private static List<object[]> ParseTriples(string json)
        {
            var raw = Ser().Deserialize<List<object[]>>(json);
            return raw ?? new List<object[]>();
        }

        private static List<List<object[]>> ParseRows(string json)
        {
            var raw = Ser().Deserialize<List<List<object[]>>>(json);
            return raw ?? new List<List<object[]>>();
        }

        private static void InsertTriples(MySqlConnection c, MySqlTransaction t, string table,
            List<object[]> triples, HashSet<string> existing, bool keepId)
        {
            var names = new List<string>();
            var ps = new List<MySqlParameter>();
            foreach (object[] tr in triples)
            {
                string name = Convert.ToString(tr[0]);
                if (!existing.Contains(name)) continue;               // column removed since the delete
                if (!keepId && name == "id") continue;
                object v = Decode(Convert.ToString(tr[1]), tr[2] == null ? null : Convert.ToString(tr[2]));
                var p = new MySqlParameter("@p" + names.Count, v);
                if (v is byte[]) p.MySqlDbType = MySqlDbType.LongBlob;
                names.Add(name);
                ps.Add(p);
            }
            string sql = "INSERT INTO `" + table + "` (" + string.Join(", ", names.Select(n => "`" + n + "`")) +
                         ") VALUES (" + string.Join(", ", names.Select((n, i) => "@p" + i)) + ")";
            Exec(c, t, sql, ps.ToArray());
        }

        private static HashSet<string> Columns(MySqlConnection c, MySqlTransaction t, string table)
        {
            DataTable d = Fill(c, t,
                "SELECT column_name FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = @t",
                new MySqlParameter("@t", table));
            return new HashSet<string>(d.Rows.Cast<DataRow>().Select(r => Convert.ToString(r[0])), StringComparer.OrdinalIgnoreCase);
        }

        // ---- transaction plumbing ----------------------------------------------------
        private static void Tx(Action<MySqlConnection, MySqlTransaction> work)
        {
            using (var c = new MySqlConnection(ServerConfig.EffectiveConnectionString))
            {
                c.Open();
                using (MySqlTransaction t = c.BeginTransaction())
                {
                    try { work(c, t); t.Commit(); }
                    catch { try { t.Rollback(); } catch { } throw; }
                }
            }
        }

        private static DataTable Fill(MySqlConnection c, MySqlTransaction t, string sql, params MySqlParameter[] ps)
        {
            using (var cmd = new MySqlCommand(sql, c, t))
            {
                cmd.Parameters.AddRange(ps);
                var dt = new DataTable();
                using (var da = new MySqlDataAdapter(cmd)) da.Fill(dt);
                return dt;
            }
        }

        private static object Scalar(MySqlConnection c, MySqlTransaction t, string sql, params MySqlParameter[] ps)
        {
            using (var cmd = new MySqlCommand(sql, c, t))
            {
                cmd.Parameters.AddRange(ps);
                return cmd.ExecuteScalar();
            }
        }

        private static void Exec(MySqlConnection c, MySqlTransaction t, string sql, params MySqlParameter[] ps)
        {
            using (var cmd = new MySqlCommand(sql, c, t))
            {
                cmd.CommandTimeout = 120;      // a row with its scans can be tens of MB
                cmd.Parameters.AddRange(ps);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
