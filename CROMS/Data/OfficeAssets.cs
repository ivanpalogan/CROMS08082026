using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>Which branding image is meant. Stored separately so the office can
    /// replace one without disturbing the other. The four Header/Footer kinds are the
    /// letterhead images on Form 3A (Marriage Available Certification) - added migration 47.</summary>
    public enum AssetKind { Logo, Stamp, HeaderLogoLeft, HeaderLogoRight1, HeaderLogoRight2, FooterBanner }

    /// <summary>The registering office's own details, as printed in a form's header and
    /// signature block. One row in `office_profile`; read, never guessed.</summary>
    public class OfficeProfile
    {
        public string OfficeName = "Office of the Local Civil Registrar";
        public string Municipality = "";
        public string Province = "";
        public string Region = "";
        public string RegistrarName = "";
        public string RegistrarTitle = "Municipal Civil Registrar";
        public string Address = "";
        public string Contact = "";
        public string Email = "";
        public string VerifyingOfficerName = "";
        public string VerifyingOfficerTitle = "Registration Officer II";

        public string HeaderLine =>
            string.Join(", ", new[] { OfficeName, Municipality, Province }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        /// <summary>
        /// The municipality as it should be PRINTED. The letterhead of every certification
        /// used to carry the literal "PENABLANCA" — ASCII, no tilde — which threw away the
        /// exact spelling migration 26 went out of its way to store correctly, and meant a
        /// second LGU could not use the app without a rebuild. The fallback spells the
        /// tilde as ñ rather than as a literal character: a .cs file without a BOM can
        /// be read in the machine's own code page, which is the same class of mistake that
        /// corrupted this name once already.
        /// </summary>
        public string MunicipalityForPrint =>
            string.IsNullOrWhiteSpace(Municipality) ? "Peñablanca" : Municipality.Trim();

        /// <summary>The province as it should be printed, with the same fallback reasoning
        /// as <see cref="MunicipalityForPrint"/>.</summary>
        public string ProvinceForPrint =>
            string.IsNullOrWhiteSpace(Province) ? "Cagayan" : Province.Trim();
    }

    /// <summary>
    /// The office's logo and stamp, and the office profile that goes with them.
    /// <para/>
    /// Logo and stamp are DELIBERATELY separate records of the same table rather than two
    /// columns of one: the office replaces a stamp far more often than a seal, a form
    /// revision can carry its own stamp, and a report that applies a stamp only to issued
    /// copies has to be able to ask for one without pulling the other. A row with a
    /// <c>form_code</c> wins over the office-wide default for that form.
    /// <para/>
    /// Images are cached per key for the life of the process — a certificate print asks
    /// for the same seal on every page, and re-reading a blob per page is what makes a
    /// batch print crawl. <see cref="ClearCache"/> after an upload.
    /// </summary>
    public static class OfficeAssets
    {
        private static readonly System.Collections.Generic.Dictionary<string, Image> _cache =
            new System.Collections.Generic.Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static OfficeProfile _profile;

        private static string Key(AssetKind kind, string formCode) =>
            kind + "|" + (formCode ?? "");

        /// <summary>
        /// The active image for this kind, preferring one registered for this specific
        /// form over the office-wide default. Returns null when the office has not
        /// supplied one — every caller treats a missing asset as "print without it",
        /// never as an error, because a certificate is still valid unstamped.
        /// </summary>
        public static Image Get(AssetKind kind, string formCode = null)
        {
            string k = Key(kind, formCode);
            if (_cache.TryGetValue(k, out Image cached)) return cached;

            Image img = null;
            try
            {
                // Ordering by (form_code IS NULL) puts the form-specific row first (0
                // sorts before 1) and the office-wide default second, so one query
                // settles the precedence.
                DataTable dt = Db.Pull(
                    "SELECT image FROM office_assets " +
                    "WHERE asset_kind = @kind AND is_active = 1 " +
                    "  AND (form_code IS NULL OR form_code = @form) " +
                    "ORDER BY (form_code IS NULL), created_at DESC LIMIT 1",
                    new MySqlParameter("@kind", kind.ToString()),
                    new MySqlParameter("@form", (object)formCode ?? DBNull.Value));

                if (dt.Rows.Count > 0 && dt.Rows[0]["image"] != DBNull.Value)
                    img = FromBytes((byte[])dt.Rows[0]["image"]);
            }
            catch (MySqlException)
            {
                // Migration 26 has not been run yet. A missing table means no branding,
                // which the printers already handle — it must not stop a print.
            }

            _cache[k] = img;
            return img;
        }

        /// <summary>
        /// The active asset as raw bytes, using the same form-specific-before-default
        /// precedence as <see cref="Get"/>. Crystal Reports renders an image by binding a
        /// Blob field to a byte[] column of the datasource, so the report path needs the
        /// bytes rather than a decoded <see cref="Image"/>.
        /// </summary>
        public static byte[] GetBytes(AssetKind kind, string formCode = null)
        {
            try
            {
                DataTable dt = Db.Pull(
                    "SELECT image FROM office_assets " +
                    "WHERE asset_kind = @kind AND is_active = 1 " +
                    "  AND (form_code IS NULL OR form_code = @form) " +
                    "ORDER BY (form_code IS NULL), created_at DESC LIMIT 1",
                    new MySqlParameter("@kind", kind.ToString()),
                    new MySqlParameter("@form", (object)formCode ?? DBNull.Value));

                if (dt.Rows.Count > 0 && dt.Rows[0]["image"] != DBNull.Value)
                    return (byte[])dt.Rows[0]["image"];
            }
            catch (MySqlException) { /* pre-migration: no branding */ }
            return null;
        }

        /// <summary>Decode a stored blob without holding the stream open. Image.FromStream
        /// keeps the stream alive for the lifetime of the Image, so the bytes are copied
        /// into a new bitmap and the stream is released.</summary>
        public static Image FromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            try
            {
                using (var ms = new MemoryStream(bytes))
                using (var loaded = Image.FromStream(ms))
                    return new Bitmap(loaded);
            }
            catch { return null; }   // not an image, or a truncated blob
        }

        /// <summary>
        /// Store or replace an asset. Existing active rows of the same kind and scope are
        /// deactivated rather than deleted, so a stamp that turns out to be the wrong one
        /// can be traced — a civil registry does not silently lose the seal that was on a
        /// certificate it already issued.
        /// </summary>
        public static void Save(AssetKind kind, string name, byte[] image,
                                string formCode = null, string mimeType = null,
                                string source = "Uploaded", string scanId = null)
        {
            if (image == null || image.Length == 0)
                throw new ArgumentException("No image data.", nameof(image));

            Db.Push(
                "UPDATE office_assets SET is_active = 0 " +
                "WHERE asset_kind = @kind AND is_active = 1 " +
                "  AND ((form_code IS NULL AND @form IS NULL) OR form_code = @form)",
                new MySqlParameter("@kind", kind.ToString()),
                new MySqlParameter("@form", (object)formCode ?? DBNull.Value));

            Db.Push(
                "INSERT INTO office_assets (asset_kind, name, form_code, image, mime_type, " +
                "is_active, source, scan_id, uploaded_by) " +
                "VALUES (@kind, @name, @form, @img, @mime, 1, @src, @scan, @user)",
                new MySqlParameter("@kind", kind.ToString()),
                new MySqlParameter("@name", name ?? kind.ToString()),
                new MySqlParameter("@form", (object)formCode ?? DBNull.Value),
                new MySqlParameter("@img", MySqlDbType.LongBlob) { Value = image },
                new MySqlParameter("@mime", (object)mimeType ?? DBNull.Value),
                new MySqlParameter("@src", source),
                new MySqlParameter("@scan", (object)scanId ?? DBNull.Value),
                new MySqlParameter("@user", Session.User?.Username ?? "(unknown)"));

            ClearCache();
        }

        public static void ClearCache()
        {
            foreach (Image i in _cache.Values) i?.Dispose();
            _cache.Clear();
        }

        /// <summary>The office profile, read once. Falls back to the built-in defaults if
        /// migration 26 has not been applied, so nothing that prints a header breaks.</summary>
        public static OfficeProfile Profile
        {
            get
            {
                if (_profile != null) return _profile;
                var p = new OfficeProfile();
                try
                {
                    DataTable dt = Db.Pull("SELECT * FROM office_profile WHERE id = 1");
                    if (dt.Rows.Count > 0)
                    {
                        DataRow r = dt.Rows[0];
                        string S(string c) => dt.Columns.Contains(c) && r[c] != DBNull.Value
                                            ? r[c].ToString().Trim() : "";
                        p.OfficeName = S("office_name") == "" ? p.OfficeName : S("office_name");
                        p.Municipality = S("municipality");
                        p.Province = S("province");
                        p.Region = S("region");
                        p.RegistrarName = S("registrar_name");
                        p.RegistrarTitle = S("registrar_title") == ""
                                         ? p.RegistrarTitle : S("registrar_title");
                        p.Address = S("address");
                        p.Contact = S("contact");
                        p.Email = S("email");
                        p.VerifyingOfficerName = S("verifying_officer_name");
                        p.VerifyingOfficerTitle = S("verifying_officer_title") == ""
                                                 ? p.VerifyingOfficerTitle : S("verifying_officer_title");
                    }
                }
                // Broad on purpose. The letterhead of every certification is now built from
                // this profile, and CROMS.ReportGen renders those blank backgrounds with no
                // database and no connection string at all — where the failure is a config
                // error, not a MySqlException. Falling back to the defaults must never be
                // able to stop a form from being drawn.
                catch (Exception) { /* pre-migration, or no database at all: keep the defaults */ }
                return _profile = p;
            }
        }

        public static void ReloadProfile() { _profile = null; }
    }
}
