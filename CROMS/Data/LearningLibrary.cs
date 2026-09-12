using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// Smart Learning Library — a single shared reference store of frequently used values
    /// (places, hospitals, municipalities, churches, cemeteries, names, nationalities,
    /// occupations, ...) backed by the <c>reference_library</c> table.
    /// <para/>
    /// Document AI calls <see cref="Learn"/> for every value it auto-fills (using the
    /// operator-corrected text, so corrections become the preferred spelling); every
    /// module calls <see cref="Attach"/> to give a textbox live autocomplete from the same
    /// store. <see cref="Normalize"/> folds case + diacritics so "Peñablanca", "PENABLANCA"
    /// and "penablanca" are treated as one value (no duplicates), while the proper spelling
    /// is preserved in the <c>value</c> column.
    /// </summary>
    public static class LearningLibrary
    {
        // Canonical categories. Kept as plain strings so new ones can be added freely.
        public const string PlaceOfBirth    = "PlaceOfBirth";
        public const string PlaceOfMarriage = "PlaceOfMarriage";
        public const string PlaceOfDeath    = "PlaceOfDeath";
        public const string Municipality    = "Municipality";
        public const string Province        = "Province";
        public const string Barangay        = "Barangay";
        public const string Hospital        = "Hospital";
        public const string HealthCenter    = "HealthCenter";
        public const string Church          = "Church";
        public const string Cemetery        = "Cemetery";
        public const string FuneralHome     = "FuneralHome";
        public const string EventLocation   = "EventLocation";
        public const string Nationality     = "Nationality";
        public const string Occupation      = "Occupation";
        public const string Surname         = "Surname";
        public const string GivenName       = "GivenName";
        public const string Officer         = "Officer";

        private static bool _seeded;
        private static readonly object _seedLock = new object();

        // ---- normalization / validation -----------------------------------

        /// <summary>Duplicate-detection key: lower-cased, diacritic-stripped, punctuation-
        /// collapsed. "Peñablanca, Cagayan" and "PENABLANCA CAGAYAN" fold to the same key.</summary>
        public static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            string t = s.Trim().ToLowerInvariant();
            // strip diacritics (ñ → n, é → e)
            t = t.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(t.Length);
            foreach (char c in t)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            t = sb.ToString().Normalize(NormalizationForm.FormC);
            // punctuation → space, collapse whitespace
            t = Regex.Replace(t, @"[.,;:_/\\|""'()\-]+", " ");
            t = Regex.Replace(t, @"\s{2,}", " ").Trim();
            return t;
        }

        /// <summary>A value worth remembering: not blank, ≥2 chars, contains a letter.</summary>
        private static bool IsLearnable(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            string v = value.Trim();
            return v.Length >= 2 && v.Any(char.IsLetter);
        }

        // ---- learning ------------------------------------------------------

        /// <summary>Remember a value under a category (insert new, or bump usage if it exists).
        /// Safe to call often; never throws (library must never block a real save).</summary>
        public static void Learn(string category, string value)
        {
            if (string.IsNullOrWhiteSpace(category) || !IsLearnable(value)) return;
            string norm = Normalize(value);
            if (norm.Length == 0) return;
            try
            {
                Db.Push(
                    "INSERT INTO reference_library (category, value, normalized) VALUES (@c, @v, @n) " +
                    "ON DUPLICATE KEY UPDATE usage_count = usage_count + 1, updated_at = CURRENT_TIMESTAMP",
                    new MySqlParameter("@c", category),
                    new MySqlParameter("@v", value.Trim()),
                    new MySqlParameter("@n", norm));
            }
            catch { /* library is best-effort; a failure must not affect the caller */ }
        }

        /// <summary>Learn a batch of (category, value) pairs.</summary>
        public static void LearnMany(IEnumerable<KeyValuePair<string, string>> items)
        {
            if (items == null) return;
            foreach (var kv in items) Learn(kv.Key, kv.Value);
        }

        /// <summary>
        /// Learn a user correction: the corrected value becomes the preferred spelling.
        /// If the AI's raw value normalises to the same key, only the count is bumped
        /// (the proper spelling already replaced it); otherwise the correction is added.
        /// </summary>
        public static void LearnCorrection(string category, string corrected) => Learn(category, corrected);

        // ---- suggestions / autocomplete ------------------------------------

        /// <summary>Values in a category matching a prefix (case/diacritic-insensitive),
        /// most-used first. Blank prefix returns the most common values.</summary>
        public static List<string> Suggest(string category, string prefix, int limit = 25)
        {
            var list = new List<string>();
            try
            {
                DataTable dt;
                if (string.IsNullOrWhiteSpace(prefix))
                    dt = Db.Pull(
                        "SELECT value FROM reference_library WHERE category = @c " +
                        "ORDER BY usage_count DESC, value LIMIT " + limit,
                        new MySqlParameter("@c", category));
                else
                    dt = Db.Pull(
                        "SELECT value FROM reference_library WHERE category = @c AND normalized LIKE @p " +
                        "ORDER BY usage_count DESC, value LIMIT " + limit,
                        new MySqlParameter("@c", category),
                        new MySqlParameter("@p", Normalize(prefix) + "%"));
                foreach (DataRow r in dt.Rows) list.Add(r["value"].ToString());
            }
            catch { /* return whatever we have */ }
            return list;
        }

        /// <summary>Full autocomplete source for a category (all values, common first).</summary>
        public static AutoCompleteStringCollection Source(string category)
        {
            var src = new AutoCompleteStringCollection();
            src.AddRange(Suggest(category, null, 500).ToArray());
            return src;
        }

        /// <summary>
        /// Wire a textbox to the library: suggest-and-append autocomplete from the category,
        /// and learn whatever the operator finally typed when they leave the field. One call
        /// gives a control both read (suggestions) and write (continuous learning).
        /// </summary>
        public static void Attach(TextBox box, string category)
        {
            if (box == null) return;
            EnsureSeeded();
            box.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            box.AutoCompleteSource = AutoCompleteSource.CustomSource;
            box.AutoCompleteCustomSource = Source(category);

            box.Leave += (s, e) =>
            {
                string v = box.Text;
                if (IsLearnable(v))
                {
                    Learn(category, v);
                    // refresh this box's own source so the new value is instantly available
                    if (!box.AutoCompleteCustomSource.Contains(v.Trim()))
                        box.AutoCompleteCustomSource.Add(v.Trim());
                }
            };
        }

        // ---- one-time seed from existing data ------------------------------

        /// <summary>
        /// Populate the library the first time from existing lookups + records, so
        /// autocomplete has data before any document is scanned. Runs once per process,
        /// and only actually seeds when the table is empty.
        /// </summary>
        public static void EnsureSeeded()
        {
            if (_seeded) return;
            lock (_seedLock)
            {
                if (_seeded) return;
                _seeded = true;
                try
                {
                    if (Db.GetCount("SELECT id FROM reference_library") > 0) return;

                    SeedLookup(Municipality, "municipalities");
                    SeedLookup(Province, "provinces");
                    SeedLookup(Barangay, "barangays");
                    SeedLookup(Hospital, "hospitals");
                    SeedLookup(Church, "churches");
                    SeedLookup(Nationality, "nationalities");
                    SeedLookup(Occupation, "occupations");

                    // Names from existing registry records.
                    SeedQuery(GivenName, "SELECT DISTINCT first_name AS v FROM births WHERE first_name<>''");
                    SeedQuery(Surname,   "SELECT DISTINCT last_name  AS v FROM births WHERE last_name<>''");
                    SeedQuery(PlaceOfBirth, "SELECT DISTINCT place_of_birth AS v FROM births WHERE place_of_birth<>''");
                    SeedQuery(PlaceOfDeath, "SELECT DISTINCT place_of_death AS v FROM deaths WHERE place_of_death<>''");
                }
                catch { /* seeding is best-effort */ }
            }
        }

        private static void SeedLookup(string category, string table)
        {
            try
            {
                DataTable dt = Db.Pull("SELECT name AS v FROM " + table);
                foreach (DataRow r in dt.Rows) Learn(category, r["v"].ToString());
            }
            catch { }
        }

        private static void SeedQuery(string category, string sql)
        {
            try
            {
                DataTable dt = Db.Pull(sql);
                foreach (DataRow r in dt.Rows) Learn(category, r["v"].ToString());
            }
            catch { }
        }
    }

    /// <summary>
    /// Persists newly-seen values into the Master-File lookup tables that the registration
    /// ComboBoxes bind to — so a value Document AI extracts from a certificate (a hospital,
    /// municipality, occupation, ...) becomes a permanent, selectable option in the Birth /
    /// Marriage / Death forms. Table names are whitelisted (no injection), and inserts are
    /// deduplicated case/diacritic-insensitively via <see cref="LearningLibrary.Normalize"/>.
    /// </summary>
    public static class LookupStore
    {
        // Only these (id, name) Master-File tables may be auto-extended.
        private static readonly HashSet<string> Tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hospitals", "municipalities", "provinces", "barangays", "churches",
            "nationalities", "religions", "occupations", "residences", "relationships",
            "birth_orders", "causes_of_death", "civil_statuses", "type_of_births",
            "countries"
        };

        /// <summary>Ensure a value exists in a whitelisted lookup table (dedup by normalized
        /// name). Returns true if it was newly inserted. Best-effort — never throws.</summary>
        public static bool Ensure(string table, string name)
        {
            if (table == null || !Tables.Contains(table) || string.IsNullOrWhiteSpace(name)) return false;
            string val = name.Trim();
            string norm = LearningLibrary.Normalize(val);
            if (norm.Length == 0) return false;
            try
            {
                DataTable dt = Db.Pull("SELECT name FROM " + table);
                foreach (DataRow r in dt.Rows)
                    if (LearningLibrary.Normalize(r["name"].ToString()) == norm) return false;   // already present
                Db.Push("INSERT INTO " + table + " (name) VALUES (@n)", new MySqlParameter("@n", val));
                return true;
            }
            catch { return false; }
        }
    }
}
