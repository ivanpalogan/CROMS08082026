using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// Province → Municipality → Barangay pickers, shared by Birth, Marriage and Death.
    /// <para/>
    /// The three registration screens each had their own copy of "fill a combo from a
    /// lookup table", which is how they drifted apart — Birth cascaded, Death listed every
    /// municipality in the country regardless of province, and Marriage bound ids while
    /// the others bound text. One implementation means a fix reaches all three.
    /// <para/>
    /// The lists come from the PSGC set loaded by migration 29: 87 provinces, 1,647 cities
    /// and municipalities, 42,029 barangays, each row carrying its parent. Nothing is ever
    /// loaded whole — a barangay list is only ever the barangays of one municipality, which
    /// is what keeps a 42,000-row table usable in a dropdown.
    /// <para/>
    /// A place the master file does not have is still selectable: <see cref="Select"/> adds
    /// the value to the list rather than dropping it, so a legacy record or a scanned value
    /// naming somewhere unlisted still shows what it says instead of silently blanking.
    /// </summary>
    public static class GeoLookup
    {
        /// <summary>The country the PSGC lists below actually describe.</summary>
        public const string HomeCountry = "Philippines";

        /// <summary>
        /// True when this country's places ARE the PSGC set, so the province / municipality /
        /// barangay cascade applies. Anything else is somewhere CROMS holds no lists for.
        /// </summary>
        public static bool IsHome(string country)
        {
            return string.IsNullOrWhiteSpace(country)
                || string.Equals(country.Trim(), HomeCountry, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Every country, plus the leading blank that means "not stated".</summary>
        public static void LoadCountries(ComboBox combo)
        {
            if (combo == null) return;
            Reset(combo);
            try
            {
                using (DataTable dt = Db.Pull("SELECT name FROM countries ORDER BY name = '"
                                              + HomeCountry + "' DESC, name"))
                    foreach (DataRow row in dt.Rows) combo.Items.Add(row["name"].ToString());
            }
            catch
            {
                // Migration 37 not applied yet. Offer the one country this office is in
                // rather than an empty box, so a Philippine birth is still recordable.
                combo.Items.Add(HomeCountry);
            }
        }

        /// <summary>
        /// Wire a country picker to the place cells under it.
        /// <para/>
        /// A foreign birth cannot cascade: there is no province list for Japan and no
        /// barangay list for anywhere outside the PSGC. So the cells stay in place and keep
        /// their meaning, but their PHILIPPINE lists are emptied — the boxes are already
        /// free-text dropdowns, so the clerk types the foreign locality instead of being
        /// offered Philippine places that are certainly wrong.
        /// <para/>
        /// Switching country CLEARS what was typed. A province of the old country is not a
        /// place in the new one, and leaving it on screen would let it be saved as one — the
        /// same rule the province → municipality → barangay cascade already follows.
        /// </summary>
        public static void CascadeCountry(ComboBox country, ComboBox province, ComboBox municipality, ComboBox barangay)
        {
            if (country == null || province == null) return;
            country.SelectedIndexChanged += (s, e) => ApplyCountry(country.Text, province, municipality, barangay);
        }

        /// <summary>Put the place cells into the shape the given country calls for.</summary>
        public static void ApplyCountry(string country, ComboBox province, ComboBox municipality, ComboBox barangay)
        {
            if (IsHome(country))
            {
                LoadProvinces(province);
                Reset(municipality);
                Reset(barangay);
            }
            else
            {
                // No lists to offer: emptied, not disabled. The cells still have to be
                // fillable, or a foreign birth could not be recorded at all.
                Reset(province);
                Reset(municipality);
                Reset(barangay);
            }
        }

        /// <summary>Every province, plus the leading blank that means "not stated".</summary>
        public static void LoadProvinces(ComboBox combo)
        {
            if (combo == null) return;
            Reset(combo);
            try
            {
                using (DataTable dt = Db.Pull("SELECT name FROM provinces ORDER BY name"))
                    foreach (DataRow row in dt.Rows) combo.Items.Add(row["name"].ToString());
            }
            catch
            {
                // Master file not installed yet: a blank list is the honest answer. An
                // unfiltered one would invite the operator to pick a place at random.
            }
        }

        /// <summary>The municipalities of one province, by the province's NAME.</summary>
        public static void LoadMunicipalities(ComboBox combo, string province)
        {
            if (combo == null) return;
            Reset(combo);
            if (string.IsNullOrWhiteSpace(province)) return;
            try
            {
                using (DataTable dt = Db.Pull(
                    "SELECT m.name FROM municipalities m " +
                    "JOIN provinces p ON p.id = m.province_id " +
                    "WHERE p.name = @p ORDER BY m.name",
                    new MySqlParameter("@p", province.Trim())))
                    foreach (DataRow row in dt.Rows) combo.Items.Add(row["name"].ToString());
            }
            catch { }
        }

        /// <summary>The barangays of one municipality, qualified by province.
        /// The province matters: there are several San Isidros.</summary>
        public static void LoadBarangays(ComboBox combo, string province, string municipality)
        {
            if (combo == null) return;
            Reset(combo);
            if (string.IsNullOrWhiteSpace(province) || string.IsNullOrWhiteSpace(municipality)) return;
            try
            {
                using (DataTable dt = Db.Pull(
                    "SELECT b.name FROM barangays b " +
                    "JOIN municipalities m ON m.id = b.municipality_id " +
                    "JOIN provinces p ON p.id = m.province_id " +
                    "WHERE p.name = @p AND m.name = @m ORDER BY b.name",
                    new MySqlParameter("@p", province.Trim()),
                    new MySqlParameter("@m", municipality.Trim())))
                    foreach (DataRow row in dt.Rows) combo.Items.Add(row["name"].ToString());
            }
            catch { }
        }

        /// <summary>
        /// Wire province → municipality → barangay so each choice narrows the next.
        /// <para/>
        /// Changing the province empties the barangay as well as the municipality: the
        /// barangay under the old municipality is not a place in the new province, and
        /// leaving it on screen would let it be saved as one.
        /// </summary>
        public static void CascadeAddress(ComboBox province, ComboBox municipality, ComboBox barangay)
        {
            if (province == null || municipality == null) return;
            // Same widths as the DB columns (migration 83), so a box cannot take more than the
            // record can store.
            province.MaxLength = 60; municipality.MaxLength = 80;
            if (barangay != null) barangay.MaxLength = 80;
            // A Philippine place must be a listed one (or already in the Learning Library).
            // The lists are emptied for a foreign country, which is what switches this off.
            Func<bool> philippine = () => province.Items.Count > 1;
            Strict(province, LearningLibrary.Province, "province", philippine);
            Strict(municipality, LearningLibrary.Municipality, "city / municipality", philippine);
            Strict(barangay, LearningLibrary.Barangay, "barangay", philippine);
            province.SelectedIndexChanged += (s, e) =>
            {
                LoadMunicipalities(municipality, province.Text);
                Reset(barangay);
            };
            if (barangay != null)
                municipality.SelectedIndexChanged += (s, e) =>
                    LoadBarangays(barangay, province.Text, municipality.Text);
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ComboBox, object> _strict =
            new System.Runtime.CompilerServices.ConditionalWeakTable<ComboBox, object>();

        /// <summary>
        /// Refuse a typed place that is not in the combo's list and not in the Learning Library.
        /// <para/>
        /// These cells are editable so a foreign locality can be typed, which also let
        /// "sdcfd" be saved as a Philippine province. On leaving the box, a value that
        /// matches a listed one (any case / accent) is snapped to the listed spelling; a
        /// library value is accepted; anything else is CLEARED with a short notice, so
        /// nothing unlisted can reach a record. <paramref name="applies"/> switches it off
        /// for a foreign country, where there is no list to hold the value to.
        /// </summary>
        public static void Strict(ComboBox combo, string libraryCategory, string what, Func<bool> applies)
        {
            if (combo == null) return;
            object seen;
            if (_strict.TryGetValue(combo, out seen)) return;
            _strict.Add(combo, new object());
            var tip = new ToolTip { IsBalloon = true, ToolTipTitle = "Not a listed " + what };
            combo.Leave += (s, e) =>
            {
                if (!Unlisted(combo, libraryCategory, applies)) return;
                string bad = combo.Text.Trim();
                combo.Text = "";
                combo.SelectedIndex = combo.Items.Count > 0 ? 0 : -1;
                try { tip.Show("\"" + bad + "\" is not in the list. Choose a " + what + " from the list.", combo, 0, combo.Height + 2, 3500); }
                catch { }
            };
        }

        /// <summary>True when the combo holds text that is neither listed nor in the library
        /// (after snapping a case/accent variant to the listed spelling). Also usable as a
        /// save-time guard: <c>if (GeoLookup.Unlisted(...)) refuse</c>.</summary>
        public static bool Unlisted(ComboBox combo, string libraryCategory, Func<bool> applies)
        {
            if (combo == null) return false;
            string text = (combo.Text ?? "").Trim();
            if (text.Length == 0) return false;
            if (applies != null && !applies()) return false;
            string key = LearningLibrary.Normalize(text);
            for (int i = 0; i < combo.Items.Count; i++)
            {
                string item = combo.Items[i] == null ? "" : combo.Items[i].ToString();
                if (item.Length > 0 && LearningLibrary.Normalize(item) == key)
                {
                    if (combo.SelectedIndex != i) combo.SelectedIndex = i;
                    return false;
                }
            }
            if (LearningLibrary.Contains(libraryCategory, text)) { Select(combo, text); return false; }
            return true;
        }

        /// <summary>Province → municipality only, for a place of birth / death / marriage,
        /// where the third cell is the facility rather than a barangay.</summary>
        public static void CascadePlace(ComboBox province, ComboBox municipality)
        {
            CascadeAddress(province, municipality, null);
        }

        /// <summary>
        /// Select a stored address into a wired province/municipality/barangay trio,
        /// loading each child list before selecting into it.
        /// <para/>
        /// This is the part a plain "split on commas and set each box" gets wrong: setting
        /// the barangay before its municipality has been chosen selects into a list that is
        /// still empty, so the value shows but the dropdown behind it is the wrong one.
        /// </summary>
        public static void SetAddress(ComboBox province, ComboBox municipality, ComboBox barangay,
                                      string provinceName, string municipalityName, string barangayName)
        {
            Select(province, provinceName);
            LoadMunicipalities(municipality, provinceName);
            Select(municipality, municipalityName);
            LoadBarangays(barangay, provinceName, municipalityName);
            Select(barangay, barangayName);
        }

        /// <summary>
        /// Select a stored country + province + municipality into cells wired by
        /// <see cref="CascadeCountry"/> and <see cref="CascadePlace"/>.
        /// <para/>
        /// Order is the whole point, and it relies on those handlers: setting the country
        /// rebuilds the province list, and setting the province rebuilds the municipality
        /// list, so each value has to go in AFTER the list that holds it exists. A record
        /// with no stored country reads as home, which is what a pre-migration-37 row is.
        /// </summary>
        public static void SetCountryPlace(ComboBox country, ComboBox province, ComboBox municipality,
                                           string countryName, string provinceName, string municipalityName)
        {
            Select(country, countryName);
            Select(province, provinceName);
            Select(municipality, municipalityName);
        }

        /// <summary>Select a value, adding it to the list when the master file has not got
        /// it — a record must never be made to show a place other than the one it holds.</summary>
        public static void Select(ComboBox combo, string value)
        {
            if (combo == null) return;
            if (string.IsNullOrWhiteSpace(value))
            {
                combo.SelectedIndex = combo.Items.Count > 0 ? 0 : -1;
                combo.Text = "";
                return;
            }
            value = value.Trim();
            int idx = combo.Items.IndexOf(value);
            if (idx < 0) { combo.Items.Add(value); idx = combo.Items.Count - 1; }
            combo.SelectedIndex = idx;
        }

        /// <summary>Empty a combo back to the single blank "not stated" entry.</summary>
        public static void Reset(ComboBox combo)
        {
            if (combo == null) return;
            combo.Items.Clear();
            combo.Items.Add("");
            combo.SelectedIndex = 0;
            combo.Text = "";
        }

        /// <summary>
        /// "City / municipality, Province" - the joined shape a table stores a place of birth
        /// in when it has no separate province column (marriage_licenses.husband_place_of_birth,
        /// marriages.husband_place_of_birth). Shared with <see cref="ProvinceOf"/> /
        /// <see cref="MunicipalityOf"/> so every screen that joins/splits this shape agrees on it.
        /// </summary>
        public static string JoinPlace(string municipality, string province)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(municipality)) parts.Add(municipality.Trim());
            if (!string.IsNullOrWhiteSpace(province)) parts.Add(province.Trim());
            return parts.Count == 0 ? null : string.Join(", ", parts);
        }

        /// <summary>
        /// The province out of a stored place of birth - the LAST comma-separated part.
        /// <para/>
        /// A record filed before the field was split holds whatever the clerk typed into one
        /// box, and some of those carry three parts ("Bical, Penablanca, Cagayan"). The last
        /// part is the province either way; everything before it goes to the municipality
        /// cell rather than being dropped, so a legacy value is shown in full for the clerk
        /// to correct instead of being quietly truncated.
        /// </summary>
        public static string ProvinceOf(string place)
        {
            string[] bits = (place ?? "").Split(',');
            return bits.Length < 2 ? null : Norm(bits[bits.Length - 1]);
        }

        /// <summary>The municipality (and anything before the province) out of a stored place.</summary>
        public static string MunicipalityOf(string place)
        {
            string[] bits = (place ?? "").Split(',');
            if (bits.Length < 2) return Norm(place);
            var joined = new System.Text.StringBuilder();
            for (int i = 0; i < bits.Length - 1; i++)
            {
                if (i > 0) joined.Append(", ");
                joined.Append(bits[i].Trim());
            }
            return Norm(joined.ToString());
        }

        private static string Norm(string s) { return string.IsNullOrWhiteSpace(s) ? null : s.Trim(); }
    }
}
