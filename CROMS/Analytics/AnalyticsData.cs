using System;
using System.Data;
using System.Text.RegularExpressions;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Analytics
{
    /// <summary>
    /// Read-only query helpers shared by every analytics widget. This module issues
    /// SELECT statements ONLY — nothing here writes, and no INSERT/UPDATE/DELETE/DDL
    /// appears anywhere under Analytics\.
    ///
    /// On parameterisation: every VALUE reaches MySQL as a <see cref="MySqlParameter"/>.
    /// Table and column names cannot be parameters in SQL, so where a helper takes an
    /// identifier it comes from a compile-time literal in widget code (never from the
    /// user or the database) and is additionally run through <see cref="Ident"/>, which
    /// throws on anything that is not a plain lower-case identifier. That makes an
    /// injection through this path structurally impossible rather than merely unlikely.
    /// </summary>
    public static class AnalyticsData
    {
        private static readonly Regex SafeIdent = new Regex(@"^[a-z_][a-z0-9_]*$", RegexOptions.Compiled);

        /// <summary>Validates a code-supplied table/column name. Throws if it is not a bare identifier.</summary>
        public static string Ident(string name)
        {
            if (name == null || !SafeIdent.IsMatch(name))
                throw new ArgumentException("Unsafe SQL identifier: " + (name ?? "<null>"), nameof(name));
            return name;
        }

        // ------------------------------------------------------------------ queries
        /// <summary>Runs a SELECT with @from / @to already bound from the range.</summary>
        public static DataTable Query(string sql, DateRange range, params MySqlParameter[] extra)
        {
            var ps = new System.Collections.Generic.List<MySqlParameter>
            {
                new MySqlParameter("@from", MySqlDbType.DateTime) { Value = range.From },
                new MySqlParameter("@to",   MySqlDbType.DateTime) { Value = range.ToExclusive }
            };
            if (extra != null) ps.AddRange(extra);
            return Db.Pull(sql, ps.ToArray());
        }

        /// <summary>Runs a SELECT with no range binding.</summary>
        public static DataTable Query(string sql, params MySqlParameter[] ps)
        {
            return Db.Pull(sql, ps);
        }

        /// <summary>First column of the first row as an int (0 when empty/NULL).</summary>
        public static int Scalar(string sql, params MySqlParameter[] ps)
        {
            DataTable dt = Db.Pull(sql, ps);
            if (dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return 0;
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        /// <summary>First column of the first row as a decimal (0 when empty/NULL).</summary>
        public static decimal ScalarDec(string sql, params MySqlParameter[] ps)
        {
            DataTable dt = Db.Pull(sql, ps);
            if (dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return 0m;
            return Convert.ToDecimal(dt.Rows[0][0]);
        }

        /// <summary>First column of the first row as a double, or null when empty/NULL.</summary>
        public static double? ScalarDbl(string sql, params MySqlParameter[] ps)
        {
            DataTable dt = Db.Pull(sql, ps);
            if (dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return null;
            return Convert.ToDouble(dt.Rows[0][0]);
        }

        /// <summary>Scalar int restricted to a date column inside the range.</summary>
        public static int ScalarInRange(string sql, DateRange range, params MySqlParameter[] extra)
        {
            DataTable dt = Query(sql, range, extra);
            if (dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return 0;
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        /// <summary>COUNT(*) over one table restricted to a date column inside the range.</summary>
        public static int CountInRange(string table, string dateColumn, DateRange range)
        {
            string t = Ident(table), c = Ident(dateColumn);
            return ScalarInRange(
                "SELECT COUNT(*) FROM " + t + " WHERE " + c + " >= @from AND " + c + " < @to", range);
        }

        // ----------------------------------------------------------------- coverage
        /// <summary>
        /// How much of a column is actually filled inside a range. The office's live data has
        /// several columns that are NULL in nearly every row (mother_age, weight_grams,
        /// attendant_type, disposal_method), and a chart drawn over three of fourteen records
        /// is more misleading than no chart at all — so every widget checks coverage first and
        /// says what it is based on.
        /// </summary>
        public struct Coverage
        {
            public int Filled;
            public int Total;
            public string Column;

            public int Unfilled { get { return Total - Filled; } }
            public bool None { get { return Filled == 0; } }
            public bool Partial { get { return Filled > 0 && Filled < Total; } }

            /// <summary>The empty-state wording the spec fixes for an unfilled column.</summary>
            public string EmptyMessage
            {
                get
                {
                    return "No data yet — " + Column + " is unfilled in " +
                           Unfilled + " of " + Total + " records.";
                }
            }

            /// <summary>Sentence appended to a caption when a column is only partly filled.</summary>
            public string PartialNote
            {
                get
                {
                    return "Based on " + Filled + " of " + Total + " records; " + Column +
                           " is unfilled in " + Unfilled + ".";
                }
            }
        }

        /// <summary>
        /// Counts filled vs total for <paramref name="column"/> in <paramref name="table"/>,
        /// restricted to <paramref name="dateColumn"/> inside the range. Empty strings count
        /// as unfilled — several of these columns are VARCHARs that OCR left blank rather
        /// than NULL, and treating an empty string as filled would report false coverage.
        /// </summary>
        public static Coverage GetCoverage(string table, string column, string dateColumn, DateRange range)
        {
            string t = Ident(table), c = Ident(column), d = Ident(dateColumn);
            DataTable dt = Query(
                "SELECT COUNT(*) AS total, " +
                "SUM(CASE WHEN " + c + " IS NOT NULL AND TRIM(CONCAT('', " + c + ")) <> '' THEN 1 ELSE 0 END) AS filled " +
                "FROM " + t + " WHERE " + d + " >= @from AND " + d + " < @to", range);

            var cov = new Coverage();
            cov.Column = column;
            cov.Total = 0;
            cov.Filled = 0;
            if (dt.Rows.Count > 0)
            {
                cov.Total = dt.Rows[0]["total"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[0]["total"]);
                cov.Filled = dt.Rows[0]["filled"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[0]["filled"]);
            }
            return cov;
        }

        // --------------------------------------------------------------- formatting
        /// <summary>
        /// A comparison line for a summary card: "3 more than last month (+25%)".
        /// Returns a plain "no comparison" phrase when the previous period held nothing —
        /// a percentage against zero is not a fact, and the spec forbids comparison language
        /// where there is nothing to compare against.
        /// </summary>
        public static string Compare(double current, double previous, string previousLabel)
        {
            if (previous <= 0)
            {
                return current > 0
                    ? "No " + previousLabel + " to compare against"
                    : "Nothing in this period or " + previousLabel;
            }

            double diff = current - previous;
            if (Math.Abs(diff) < 0.0001) return "Unchanged from " + previousLabel;

            double pct = diff / previous * 100.0;
            string dir = diff > 0 ? "more" : "fewer";
            return Fmt(Math.Abs(diff)) + " " + dir + " than " + previousLabel +
                   " (" + (diff > 0 ? "+" : "-") + Math.Abs(pct).ToString("0.#") + "%)";
        }

        private static string Fmt(double v)
        {
            return Math.Abs(v - Math.Round(v)) < 0.0001
                ? Math.Round(v).ToString("N0")
                : v.ToString("N1");
        }

        /// <summary>Safe percentage-of-total, returning 0 rather than NaN when the total is 0.</summary>
        public static double Pct(double part, double total)
        {
            return total <= 0 ? 0 : part / total * 100.0;
        }

        /// <summary>Reads a DataRow cell as int, treating NULL as 0.</summary>
        public static int Int(DataRow r, string col)
        {
            return r[col] == DBNull.Value ? 0 : Convert.ToInt32(r[col]);
        }

        /// <summary>Reads a DataRow cell as double, treating NULL as 0.</summary>
        public static double Dbl(DataRow r, string col)
        {
            return r[col] == DBNull.Value ? 0 : Convert.ToDouble(r[col]);
        }

        /// <summary>Reads a DataRow cell as a trimmed string, treating NULL as "".</summary>
        public static string Str(DataRow r, string col)
        {
            return r[col] == DBNull.Value ? "" : Convert.ToString(r[col]).Trim();
        }
    }
}
