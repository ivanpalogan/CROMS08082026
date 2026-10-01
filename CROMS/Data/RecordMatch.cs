using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// What the client already told the kiosk about the record they want — passed to Records
    /// Archive so staff land on LIKELY matches instead of the whole register.
    /// </summary>
    public class RecordCriteria
    {
        public string DocType;                       // Birth / Marriage / Death
        public string RegistryNo;
        public string First, Middle, Last;           // the record owner (the husband, on a marriage)
        public string SpouseFirst, SpouseMiddle, SpouseLast;   // marriage only
        public DateTime? EventDate;                  // date of birth / marriage / death, if known
        public string City, Province;

        public bool HasName => !string.IsNullOrWhiteSpace(Last) || !string.IsNullOrWhiteSpace(First);
        public bool HasAnything => HasName || !string.IsNullOrWhiteSpace(RegistryNo);

        /// <summary>One line for the header: who we are looking for, and what else is known.</summary>
        public string Summary(string type)
        {
            var parts = new List<string>();
            string owner = RecordMatch.Join(First, Middle, Last);
            string spouse = RecordMatch.Join(SpouseFirst, SpouseMiddle, SpouseLast);
            if (owner.Length > 0) parts.Add(type == "Marriage" && spouse.Length > 0 ? owner + " & " + spouse : owner);
            if (SameType(type) && !string.IsNullOrWhiteSpace(RegistryNo)) parts.Add("Reg. no. " + RegistryNo.Trim());
            if (SameType(type) && EventDate.HasValue) parts.Add(EventDate.Value.ToString("d MMM yyyy"));
            if (SameType(type))
            {
                string place = string.Join(", ", new[] { City, Province }
                    .Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()));
                if (place.Length > 0) parts.Add(place);
            }
            return string.Join("  ·  ", parts);
        }

        /// <summary>Date, place and registry number only mean something for the document type the
        /// client actually asked for — a birth date says nothing about a marriage.</summary>
        public bool SameType(string type) =>
            string.Equals(DocType, type, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Turns <see cref="RecordCriteria"/> into SQL for the births / marriages / deaths tables.
    /// Matching is in LEVELS, tightest first, and the caller stops at the first level that finds
    /// anything — so staff see the few records that really fit, and only fall back to looser
    /// guesses (same surname, or one that sounds alike) when nothing fits exactly. A record is
    /// never hidden because the client misremembered a date or a place: those only RANK.
    /// </summary>
    public static class RecordMatch
    {
        public const int LevelRegistry = 0;   // the client gave a registry number and it exists
        public const int LevelName = 1;       // every name the client gave matches
        public const int LevelSurname = 2;    // same surname, or one that sounds alike

        public static string Describe(int level)
        {
            switch (level)
            {
                case LevelRegistry: return "matched by registry number";
                case LevelName: return "name matches";
                default: return "no exact name match — showing the same surname or similar-sounding ones";
            }
        }

        public static string Table(string type)
        {
            switch (type)
            {
                case "Birth": return "births";
                case "Marriage": return "marriages";
                case "Death": return "deaths";
                default: return null;
            }
        }

        /// <summary>The levels worth trying, tightest first.</summary>
        public static List<int> Levels(string type, RecordCriteria c)
        {
            var levels = new List<int>();
            if (c == null) return levels;
            if (c.SameType(type) && !string.IsNullOrWhiteSpace(c.RegistryNo)) levels.Add(LevelRegistry);
            if (c.HasName) levels.Add(LevelName);
            if (!string.IsNullOrWhiteSpace(c.Last) || !string.IsNullOrWhiteSpace(c.SpouseLast)) levels.Add(LevelSurname);
            return levels;
        }

        /// <summary>True when the client told us enough to filter this register at all.</summary>
        public static bool Applies(string type, RecordCriteria c) => Levels(type, c).Count > 0;

        /// <summary>Every parameter the WHERE and ORDER BY fragments below may use.</summary>
        public static List<MySqlParameter> Params(string type, RecordCriteria c)
        {
            string last = StripSuffix(c.Last), sLast = StripSuffix(c.SpouseLast);
            bool same = c.SameType(type);
            return new List<MySqlParameter>
            {
                P("@mr", same ? Clean(c.RegistryNo) : null),
                P("@mf", Like(c.First)),   P("@ml", Like(last)),   P("@mm", Like(c.Middle)),
                P("@msf", Like(c.SpouseFirst)), P("@msl", Like(sLast)), P("@msm", Like(c.SpouseMiddle)),
                P("@mlraw", Clean(last)),  P("@mslraw", Clean(sLast)),
                new MySqlParameter("@mdate", same && c.EventDate.HasValue ? (object)c.EventDate.Value.Date : DBNull.Value),
                P("@mcity", same ? Like(c.City) : null), P("@mprov", same ? Like(c.Province) : null)
            };
        }

        /// <summary>The WHERE fragment (no leading AND) for a level, or null if the level has
        /// nothing to match on.</summary>
        public static string Where(string type, RecordCriteria c, int level)
        {
            bool hasF = Has(c.First), hasL = Has(c.Last), hasSF = Has(c.SpouseFirst), hasSL = Has(c.SpouseLast);

            switch (type)
            {
                case "Birth":
                    if (level == LevelRegistry) return "registry_no = @mr";
                    if (level == LevelName)
                        return And(hasF ? "first_name LIKE @mf" : null, hasL ? "last_name LIKE @ml" : null);
                    return hasL ? "(last_name LIKE @ml OR SOUNDEX(last_name) = SOUNDEX(@mlraw))" : null;

                case "Death":
                    if (level == LevelRegistry) return "registry_no = @mr";
                    if (level == LevelName)
                        return And(hasF ? "full_name LIKE @mf" : null, hasL ? "full_name LIKE @ml" : null);
                    return hasL ? "(full_name LIKE @ml OR SOUNDEX(SUBSTRING_INDEX(full_name, ' ', -1)) = SOUNDEX(@mlraw))" : null;

                case "Marriage":
                    if (level == LevelRegistry) return "registry_no = @mr";
                    if (level == LevelName)
                    {
                        // The client may have named the pair either way round.
                        string ownerAsH = Person("husband_first_name", "husband_last_name", "@mf", "@ml", hasF, hasL);
                        string ownerAsW = Person("wife_first_name", "wife_last_name", "@mf", "@ml", hasF, hasL);
                        string spouseAsW = Person("wife_first_name", "wife_last_name", "@msf", "@msl", hasSF, hasSL);
                        string spouseAsH = Person("husband_first_name", "husband_last_name", "@msf", "@msl", hasSF, hasSL);
                        if (spouseAsW != null && ownerAsH != null)
                            return "((" + ownerAsH + " AND " + spouseAsW + ") OR (" + ownerAsW + " AND " + spouseAsH + "))";
                        if (ownerAsH != null) return "(" + ownerAsH + " OR " + ownerAsW + ")";
                        return null;
                    }
                    {
                        var any = new List<string>();
                        if (hasL) any.Add("husband_last_name LIKE @ml OR wife_last_name LIKE @ml OR " +
                                          "SOUNDEX(husband_last_name) = SOUNDEX(@mlraw) OR SOUNDEX(wife_last_name) = SOUNDEX(@mlraw)");
                        if (hasSL) any.Add("husband_last_name LIKE @msl OR wife_last_name LIKE @msl OR " +
                                           "SOUNDEX(husband_last_name) = SOUNDEX(@mslraw) OR SOUNDEX(wife_last_name) = SOUNDEX(@mslraw)");
                        return any.Count == 0 ? null : "(" + string.Join(" OR ", any) + ")";
                    }
            }
            return null;
        }

        /// <summary>The ORDER BY expression (no keyword): the more of what the client told us a
        /// record agrees with, the higher it sits. Dates and places only rank, never exclude.</summary>
        public static string OrderBy(string type, RecordCriteria c)
        {
            var s = new List<string>();
            bool same = c.SameType(type);
            if (same && Has(c.RegistryNo)) s.Add("IF(registry_no = @mr, 6, 0)");

            switch (type)
            {
                case "Birth":
                    if (Has(c.Last)) s.Add("IF(last_name LIKE @ml, 3, 0)");
                    if (Has(c.First)) s.Add("IF(first_name LIKE @mf, 3, 0)");
                    if (Has(c.Middle)) s.Add("IF(middle_name LIKE @mm, 1, 0)");
                    if (same && c.EventDate.HasValue) s.Add("IF(date_of_birth = @mdate, 4, 0)");
                    if (same && Has(c.City)) s.Add("IF(place_of_birth LIKE @mcity, 1, 0)");
                    if (same && Has(c.Province)) s.Add("IF(place_of_birth LIKE @mprov, 1, 0)");
                    break;
                case "Death":
                    if (Has(c.Last)) s.Add("IF(full_name LIKE @ml, 3, 0)");
                    if (Has(c.First)) s.Add("IF(full_name LIKE @mf, 3, 0)");
                    if (Has(c.Middle)) s.Add("IF(full_name LIKE @mm, 1, 0)");
                    if (same && c.EventDate.HasValue) s.Add("IF(date_of_death = @mdate, 4, 0)");
                    if (same && Has(c.City)) s.Add("IF(place_of_death LIKE @mcity, 1, 0)");
                    if (same && Has(c.Province)) s.Add("IF(place_of_death LIKE @mprov, 1, 0)");
                    break;
                case "Marriage":
                    if (Has(c.Last)) s.Add("IF(husband_last_name LIKE @ml OR wife_last_name LIKE @ml, 3, 0)");
                    if (Has(c.First)) s.Add("IF(husband_first_name LIKE @mf OR wife_first_name LIKE @mf, 3, 0)");
                    if (Has(c.SpouseLast)) s.Add("IF(husband_last_name LIKE @msl OR wife_last_name LIKE @msl, 3, 0)");
                    if (Has(c.SpouseFirst)) s.Add("IF(husband_first_name LIKE @msf OR wife_first_name LIKE @msf, 3, 0)");
                    if (same && c.EventDate.HasValue) s.Add("IF(date_of_marriage = @mdate, 4, 0)");
                    break;
            }
            return (s.Count == 0 ? "0" : string.Join(" + ", s)) + " DESC, id DESC";
        }

        // ------------------------------------------------------------ helpers
        private static string Person(string firstCol, string lastCol, string pf, string pl, bool hasF, bool hasL) =>
            And(hasF ? firstCol + " LIKE " + pf : null, hasL ? lastCol + " LIKE " + pl : null);

        private static string And(string a, string b)
        {
            if (a == null) return b;
            if (b == null) return a;
            return "(" + a + " AND " + b + ")";
        }

        private static bool Has(string s) => !string.IsNullOrWhiteSpace(s);
        private static string Clean(string s) => Has(s) ? s.Trim() : null;
        private static string Like(string s) => Has(s) ? "%" + s.Trim() + "%" : null;
        private static MySqlParameter P(string n, string v) => new MySqlParameter(n, (object)v ?? DBNull.Value);

        private static readonly Regex SuffixTail =
            new Regex(@"[\s,]+(jr|sr|ii|iii|iv|v)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>"Dela Cruz Jr." -> "Dela Cruz": the registry keeps the suffix out of the surname.</summary>
        public static string StripSuffix(string last) => Has(last) ? SuffixTail.Replace(last.Trim(), "") : last;

        public static string Join(string first, string middle, string last)
        {
            var parts = new List<string>();
            foreach (string p in new[] { first, middle, last })
                if (Has(p)) parts.Add(p.Trim());
            return string.Join(" ", parts);
        }

        // ------------------------------------------------------------ cue banner
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        /// <summary>Grey hint text inside an empty TextBox.</summary>
        public static void Cue(TextBox box, string text)
        {
            EventHandler set = (s, e) => SendMessage(box.Handle, 0x1501 /* EM_SETCUEBANNER */, (IntPtr)1, text ?? "");
            if (box.IsHandleCreated) set(box, EventArgs.Empty);
            else box.HandleCreated += set;
        }
    }
}
