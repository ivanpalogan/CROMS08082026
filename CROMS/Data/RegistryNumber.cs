using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// Issues the office's registry numbers (<c>YYYY-B-0001</c> / <c>-M-</c> / <c>-D-</c>)
    /// and recognises the one error that means somebody else took the number first.
    ///
    /// <para>WHY THIS IS SHARED. Birth, Marriage and Death each had their own private copy
    /// of the same year-scoped MAX+1 query, so a fix to one left the other two wrong. More
    /// importantly the number is read in one statement and written in another, so two
    /// registrars saving in the same moment both read the same maximum and both write the
    /// same number. Since migration 27 <c>registry_no</c> is UNIQUE, the second write now
    /// FAILS (error 1062) instead of quietly duplicating the record's legal key — and a
    /// caller that does not handle that failure has merely traded silent corruption for a
    /// crash. <see cref="WasTaken"/> is how a save tells that specific collision apart from
    /// any other error so it can take the next number and try again; the constraint and the
    /// retry only work as a pair.</para>
    /// </summary>
    public static class RegistryNumber
    {
        /// <summary>
        /// How many times a save should re-take a number before giving up. Each retry means
        /// another workstation won the same instant; more than a few in a row is a real
        /// problem to surface, not to keep absorbing.
        /// </summary>
        public const int MaxRetries = 5;

        /// <summary>
        /// Next unused number for the given registry table, scoped to the current year —
        /// e.g. <c>Next("births", 'B')</c> gives <c>2026-B-0007</c>.
        ///
        /// <para>MAX+1 rather than COUNT+1: counting reissues a number after a record is
        /// deleted, which would collide with the record that already carries it.</para>
        /// </summary>
        public static string Next(string table, char kind)
        {
            int year = DateTime.Now.Year;
            DataTable dt = Db.Pull(
                "SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(registry_no, '-', -1) AS UNSIGNED)), 0) + 1 AS n " +
                "FROM `" + Table(table) + "` WHERE registry_no LIKE @p",
                new MySqlParameter("@p", year + "-" + kind + "-%"));

            int next = (dt.Rows.Count > 0 && dt.Rows[0]["n"] != DBNull.Value)
                ? Convert.ToInt32(dt.Rows[0]["n"]) : 1;
            return string.Format("{0}-{1}-{2:D4}", year, kind, next);
        }

        /// <summary>
        /// True when this exception is specifically "that registry number is already used"
        /// — a duplicate-key violation naming the registry-number index — and the save
        /// should therefore take the next number and retry.
        ///
        /// <para>The index name is checked, not just the 1062 code, so a collision on some
        /// OTHER unique column is never mistaken for this and silently retried into an
        /// endless loop.</para>
        /// </summary>
        public static bool WasTaken(MySqlException ex, string table)
        {
            if (ex == null || ex.Number != 1062) return false;
            string key = "ux_" + Table(table) + "_registry_no";
            return ex.Message != null &&
                   ex.Message.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Guards the one place a table name reaches SQL as text. A table name cannot be a
        /// query parameter, so it is checked against the three registries by name; every
        /// caller passes a compile-time literal, and anything else is a bug worth throwing on.
        /// </summary>
        private static string Table(string table)
        {
            switch (table)
            {
                case "births":
                case "marriages":
                case "deaths":
                    return table;
                default:
                    throw new ArgumentException(
                        "Not a registry table: " + table, "table");
            }
        }
    }
}
