using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace CROMS.Analytics
{
    /// <summary>
    /// Turns the "GROUP BY month" result sets every trend chart returns into a single
    /// ordered category axis that two or more series can share.
    ///
    /// Two series grouped separately in SQL do not line up on their own — one may have
    /// March and the other may not — and binding them to a chart as-is silently shifts one
    /// line sideways against the other. Every dual-series trend in this module goes through
    /// here so both series are indexed against the SAME axis.
    /// </summary>
    public static class TimeBuckets
    {
        /// <summary>Beyond this many months an axis stops zero-filling and shows only months that exist.</summary>
        private const int MaxFilledMonths = 36;

        /// <summary>MySQL DATE_FORMAT pattern matching <see cref="MonthKey"/>.</summary>
        public const string MonthFormat = "%Y-%m";

        public static string MonthKey(DateTime d)
        {
            return d.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        }

        public static DateTime ParseMonthKey(string key)
        {
            DateTime d;
            return DateTime.TryParseExact(key + "-01", "yyyy-MM-dd",
                       CultureInfo.InvariantCulture, DateTimeStyles.None, out d)
                ? d
                : DateTime.MinValue;
        }

        /// <summary>Axis label for a month key: "Aug 26".</summary>
        public static string MonthLabel(string key)
        {
            DateTime d = ParseMonthKey(key);
            return d == DateTime.MinValue ? key : d.ToString("MMM yy");
        }

        /// <summary>
        /// Builds the shared month axis from one or more grouped results.
        /// Months between the first and last are zero-filled so a quiet month reads as a dip
        /// rather than vanishing — but only up to <see cref="MaxFilledMonths"/>, so an
        /// "All Time" range whose floor is 1900 does not generate 1,500 empty categories.
        /// </summary>
        public static List<string> MonthAxis(params DataTable[] tables)
        {
            var present = new SortedSet<string>(StringComparer.Ordinal);
            foreach (DataTable t in tables)
            {
                if (t == null) continue;
                foreach (DataRow r in t.Rows)
                {
                    string k = AnalyticsData.Str(r, "ym");
                    if (k.Length == 7) present.Add(k);
                }
            }

            var keys = new List<string>(present);
            if (keys.Count < 2) return keys;

            DateTime first = ParseMonthKey(keys[0]);
            DateTime last = ParseMonthKey(keys[keys.Count - 1]);
            if (first == DateTime.MinValue || last == DateTime.MinValue) return keys;

            int span = (last.Year - first.Year) * 12 + (last.Month - first.Month) + 1;
            if (span <= 0 || span > MaxFilledMonths) return keys;

            var filled = new List<string>(span);
            for (DateTime d = first; d <= last; d = d.AddMonths(1))
                filled.Add(MonthKey(d));
            return filled;
        }

        /// <summary>
        /// Reads a grouped result into a key-&gt;value map. <paramref name="keyColumn"/> and
        /// <paramref name="valueColumn"/> are the aliases the widget's own SQL declared.
        /// </summary>
        public static Dictionary<string, double> ToMap(DataTable t, string keyColumn, string valueColumn)
        {
            var map = new Dictionary<string, double>(StringComparer.Ordinal);
            if (t == null) return map;
            foreach (DataRow r in t.Rows)
            {
                string k = AnalyticsData.Str(r, keyColumn);
                if (k.Length == 0) continue;
                double v = AnalyticsData.Dbl(r, valueColumn);
                if (map.ContainsKey(k)) map[k] += v; else map[k] = v;
            }
            return map;
        }

        /// <summary>Value for a key, or 0 — the zero-fill that keeps two series aligned.</summary>
        public static double At(Dictionary<string, double> map, string key)
        {
            double v;
            return map.TryGetValue(key, out v) ? v : 0;
        }

        /// <summary>Sum of a map's values.</summary>
        public static double Total(Dictionary<string, double> map)
        {
            double sum = 0;
            foreach (double v in map.Values) sum += v;
            return sum;
        }

        /// <summary>
        /// The key holding the largest value, or "" when the map is empty. Ties resolve to
        /// the earliest key so a caption is stable between refreshes.
        /// </summary>
        public static string Peak(Dictionary<string, double> map, IList<string> order)
        {
            string best = "";
            double bestVal = double.MinValue;
            foreach (string k in order)
            {
                double v = At(map, k);
                if (v > bestVal) { bestVal = v; best = k; }
            }
            return bestVal <= 0 ? "" : best;
        }
    }
}
