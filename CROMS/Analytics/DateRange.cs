using System;

namespace CROMS.Analytics
{
    /// <summary>The preset periods offered by the date-range control on every analytics tab.</summary>
    public enum RangePreset
    {
        ThisWeek,
        ThisMonth,
        ThisQuarter,
        ThisYear,
        AllTime,
        Custom
    }

    /// <summary>
    /// An inclusive [From..To] span of DATES (no time-of-day). Analytics queries always
    /// compare with <c>&gt;= @from AND &lt; @toExclusive</c>, so a datetime column's last
    /// day is included whole — the classic "records after 00:00 on the end date go missing"
    /// bug this pattern exists to avoid.
    /// </summary>
    public struct DateRange
    {
        public DateTime From { get; }
        public DateTime To { get; }

        public DateRange(DateTime from, DateTime to)
        {
            From = from.Date;
            To = to.Date;
            if (To < From) { var t = From; From = To; To = t; }
        }

        /// <summary>Exclusive upper bound used in SQL (<c>&lt; @to</c>).</summary>
        public DateTime ToExclusive => To.AddDays(1);

        public int Days => (int)(To - From).TotalDays + 1;

        /// <summary>Roughly how many whole months the span covers — decides month vs day buckets.</summary>
        public int ApproxMonths => (To.Year - From.Year) * 12 + (To.Month - From.Month) + 1;

        public bool SpansMoreThanOneYear => From.Year != To.Year;

        public override string ToString() =>
            From.ToString("dd MMM yyyy") + " – " + To.ToString("dd MMM yyyy");

        // ------------------------------------------------------------------ presets
        /// <summary>
        /// Resolves a preset against today. AllTime deliberately starts at 1900-01-01 rather
        /// than querying MIN(created_at) — the tabs cover several tables with different
        /// earliest rows, and a fixed floor keeps every widget on one comparable axis.
        /// </summary>
        public static DateRange Resolve(RangePreset preset)
        {
            DateTime today = DateTime.Today;
            switch (preset)
            {
                case RangePreset.ThisWeek:
                    // Week starts Monday (office practice), not the CLR's Sunday default.
                    int back = ((int)today.DayOfWeek + 6) % 7;
                    return new DateRange(today.AddDays(-back), today);

                case RangePreset.ThisMonth:
                    return new DateRange(new DateTime(today.Year, today.Month, 1), today);

                case RangePreset.ThisQuarter:
                    int qStart = ((today.Month - 1) / 3) * 3 + 1;
                    return new DateRange(new DateTime(today.Year, qStart, 1), today);

                case RangePreset.ThisYear:
                    return new DateRange(new DateTime(today.Year, 1, 1), today);

                default: // AllTime (Custom is supplied by the control itself)
                    return new DateRange(new DateTime(1900, 1, 1), today);
            }
        }

        public static string Label(RangePreset p)
        {
            switch (p)
            {
                case RangePreset.ThisWeek: return "This Week";
                case RangePreset.ThisMonth: return "This Month";
                case RangePreset.ThisQuarter: return "This Quarter";
                case RangePreset.ThisYear: return "This Year";
                case RangePreset.AllTime: return "All Time";
                default: return "Custom";
            }
        }

        // ------------------------------------------------- fixed periods used by cards
        // Summary cards define their OWN periods (spec: the range control filters charts
        // only), so those periods live here rather than being re-derived in each tab.
        public static DateRange ThisMonth()
        {
            DateTime t = DateTime.Today;
            return new DateRange(new DateTime(t.Year, t.Month, 1), t);
        }

        public static DateRange LastMonth()
        {
            DateTime firstThis = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime firstLast = firstThis.AddMonths(-1);
            return new DateRange(firstLast, firstThis.AddDays(-1));
        }

        public static DateRange ThisYear()
        {
            DateTime t = DateTime.Today;
            return new DateRange(new DateTime(t.Year, 1, 1), t);
        }

        public static DateRange LastYear()
        {
            int y = DateTime.Today.Year - 1;
            return new DateRange(new DateTime(y, 1, 1), new DateTime(y, 12, 31));
        }

        public static DateRange Today()
        {
            return new DateRange(DateTime.Today, DateTime.Today);
        }

        public static DateRange Everything()
        {
            return new DateRange(new DateTime(1900, 1, 1), DateTime.Today);
        }
    }
}
