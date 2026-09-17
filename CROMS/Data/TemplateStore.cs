using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// Loads, saves and restores certificate templates. Every write goes through this
    /// class — nothing else touches the `certificate_templates` /
    /// `certificate_template_images` tables — so the "one default row, one active row
    /// per form" rule can't be violated from somewhere else in the app.
    /// <para/>
    /// The template a certificate actually prints with is whatever is in the ACTIVE
    /// row. The DEFAULT row is seeded once, the first time a form is opened in the
    /// designer, from that form's existing hardcoded C# layout (<see cref="Form3ACert"/>
    /// / <see cref="Form3BCert"/> today) — so "Restore Default" always has the office's
    /// original, proven layout to fall back to, never a blank page.
    /// </summary>
    public static class TemplateStore
    {
        private static readonly JavaScriptSerializer Json =
            new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        /// <summary>Every certificate this app currently knows how to lay out through
        /// the visual designer. Adding a form here is the only step needed to bring it
        /// under the designer — nothing else in this file names a form.</summary>
        public static IReadOnlyList<TemplateFormInfo> KnownForms { get; } = new List<TemplateFormInfo>
        {
            new TemplateFormInfo(Form3ACert.FormCode, Form3ACert.FormName,
                "Marriage", Form3ACert.PageWidth, Form3ACert.PageHeight,
                () => Form3ACert.Cells.Select(ToElement).ToList()),
            new TemplateFormInfo(Form3CCert.FormCode, Form3CCert.FormName,
                "Death", Form3CCert.PageWidth, Form3CCert.PageHeight,
                () => Form3CCert.Cells.Select(ToElement).ToList()),
            new TemplateFormInfo(Form3BCert.FormCode, Form3BCert.FormName,
                "Birth", Form3BCert.PageWidth, Form3BCert.PageHeight,
                () => Form3BCert.Cells.Select(ToElement).ToList()),

            // The six annual assessment reports — one per Reports & Analytics tab. Same
            // designer, same renderer, same "editable once opened" rule as A1/A2/A3; they
            // differ only in which table they count a year of.
            new TemplateFormInfo(AssessmentReport.Birth.FormCode, AssessmentReport.Birth.FormName,
                "Assessment", AssessmentReport.PageWidth, AssessmentReport.PageHeight,
                () => AssessmentReport.Birth.Cells.Select(ToElement).ToList()),
            new TemplateFormInfo(AssessmentReport.Death.FormCode, AssessmentReport.Death.FormName,
                "Assessment", AssessmentReport.PageWidth, AssessmentReport.PageHeight,
                () => AssessmentReport.Death.Cells.Select(ToElement).ToList()),
            new TemplateFormInfo(AssessmentReport.Marriage.FormCode, AssessmentReport.Marriage.FormName,
                "Assessment", AssessmentReport.PageWidth, AssessmentReport.PageHeight,
                () => AssessmentReport.Marriage.Cells.Select(ToElement).ToList()),
            new TemplateFormInfo(AssessmentReport.Queue.FormCode, AssessmentReport.Queue.FormName,
                "Assessment", AssessmentReport.PageWidth, AssessmentReport.PageHeight,
                () => AssessmentReport.Queue.Cells.Select(ToElement).ToList()),
            new TemplateFormInfo(AssessmentReport.Certificate.FormCode, AssessmentReport.Certificate.FormName,
                "Assessment", AssessmentReport.PageWidth, AssessmentReport.PageHeight,
                () => AssessmentReport.Certificate.Cells.Select(ToElement).ToList()),
            new TemplateFormInfo(AssessmentReport.Psa.FormCode, AssessmentReport.Psa.FormName,
                "Assessment", AssessmentReport.PageWidth, AssessmentReport.PageHeight,
                () => AssessmentReport.Psa.Cells.Select(ToElement).ToList()),
        };

        /// <summary>The 1A/2A/3A "Facts Certification" family, in that order — the set every
        /// "Apply Header/Footer to 1A, 2A and 3A" action propagates across. Listed here once so
        /// the designer's propagate buttons and this class's own ApplyBand agree on membership.</summary>
        public static readonly string[] FactsCertificationFamily =
        {
            Form3ACert.FormCode, Form3CCert.FormCode, Form3BCert.FormCode
        };

        public static TemplateFormInfo FindForm(string formCode) =>
            KnownForms.FirstOrDefault(f => string.Equals(f.FormCode, formCode, StringComparison.OrdinalIgnoreCase));

        // ===================================================================
        // Load
        // ===================================================================

        /// <summary>The template that actually prints. Seeds the default (and copies it
        /// into the active row) the first time this form is opened.</summary>
        public static CertTemplate GetActive(string formCode)
        {
            EnsureSeeded(formCode);
            DataTable dt = Db.Pull(
                "SELECT elements_json, name, page_width, page_height, orientation " +
                "FROM certificate_templates WHERE form_code=@f AND is_active=1 LIMIT 1",
                new MySqlParameter("@f", formCode));
            if (dt.Rows.Count == 0) return null;
            return FromRow(dt.Rows[0], formCode);
        }

        /// <summary>The office's original layout — never edited, only restored from.</summary>
        public static CertTemplate GetDefault(string formCode)
        {
            EnsureSeeded(formCode);
            DataTable dt = Db.Pull(
                "SELECT elements_json, name, page_width, page_height, orientation " +
                "FROM certificate_templates WHERE form_code=@f AND is_default=1 LIMIT 1",
                new MySqlParameter("@f", formCode));
            if (dt.Rows.Count == 0) return null;
            return FromRow(dt.Rows[0], formCode);
        }

        /// <summary>True once this form has ever been opened in the designer.</summary>
        public static bool HasCustomTemplate(string formCode)
        {
            return Db.GetCount(
                "SELECT id FROM certificate_templates WHERE form_code='" +
                formCode.Replace("'", "''") + "' AND is_active=1") > 0;
        }

        private static CertTemplate FromRow(DataRow r, string formCode)
        {
            var elements = Json.Deserialize<List<TemplateElement>>((string)r["elements_json"])
                           ?? new List<TemplateElement>();
            return new CertTemplate
            {
                FormCode = formCode,
                Name = r["name"] as string ?? "",
                PageWidth = Convert.ToSingle(r["page_width"], CultureInfo.InvariantCulture),
                PageHeight = Convert.ToSingle(r["page_height"], CultureInfo.InvariantCulture),
                Orientation = r["orientation"] as string ?? "Portrait",
                Elements = elements
            };
        }

        private static void EnsureSeeded(string formCode)
        {
            if (Db.GetCount(
                "SELECT id FROM certificate_templates WHERE form_code='" +
                formCode.Replace("'", "''") + "' AND is_default=1") > 0)
                return;

            TemplateFormInfo info = FindForm(formCode);
            if (info == null) throw new ArgumentException("Unknown template form: " + formCode);

            var seed = new CertTemplate
            {
                FormCode = formCode,
                Name = info.FormName,
                PageWidth = info.PageWidth,
                PageHeight = info.PageHeight,
                Orientation = "Portrait",
                Elements = info.SeedElements()
            };
            string json = Json.Serialize(seed.Elements);

            // Both rows start identical: the default (never touched again) and the
            // active copy (what the operator will actually edit).
            Db.Push(
                "INSERT INTO certificate_templates " +
                "(form_code, name, page_width, page_height, orientation, elements_json, is_default, is_active) " +
                "VALUES (@f, @n, @w, @h, 'Portrait', @j, 1, 0)",
                new MySqlParameter("@f", formCode), new MySqlParameter("@n", info.FormName),
                new MySqlParameter("@w", info.PageWidth), new MySqlParameter("@h", info.PageHeight),
                new MySqlParameter("@j", json));
            Db.Push(
                "INSERT INTO certificate_templates " +
                "(form_code, name, page_width, page_height, orientation, elements_json, is_default, is_active) " +
                "VALUES (@f, @n, @w, @h, 'Portrait', @j, 0, 1)",
                new MySqlParameter("@f", formCode), new MySqlParameter("@n", info.FormName),
                new MySqlParameter("@w", info.PageWidth), new MySqlParameter("@h", info.PageHeight),
                new MySqlParameter("@j", json));
        }

        // ===================================================================
        // Save / Restore
        // ===================================================================

        /// <summary>Writes the operator's design as the new active layout. This is the
        /// ONLY thing that changes what prints — the default row is untouched.</summary>
        public static void Save(CertTemplate t, int? userId)
        {
            EnsureSeeded(t.FormCode);
            string json = Json.Serialize(t.Elements);
            Db.Push(
                "UPDATE certificate_templates SET name=@n, page_width=@w, page_height=@h, " +
                "orientation=@o, elements_json=@j, updated_by=@u " +
                "WHERE form_code=@f AND is_active=1",
                new MySqlParameter("@n", t.Name), new MySqlParameter("@w", t.PageWidth),
                new MySqlParameter("@h", t.PageHeight), new MySqlParameter("@o", t.Orientation),
                new MySqlParameter("@j", json),
                new MySqlParameter("@u", (object)userId ?? DBNull.Value),
                new MySqlParameter("@f", t.FormCode));
        }

        /// <summary>Copies the untouched default layout back over the active one. The
        /// operator's mistaken edits are discarded; the default itself is never affected,
        /// so Restore Default can be used as many times as needed.</summary>
        public static void RestoreDefault(string formCode, int? userId)
        {
            CertTemplate def = GetDefault(formCode);
            if (def == null) return;
            def.FormCode = formCode;
            Save(def, userId);
        }

        /// <summary>
        /// "Apply Header/Footer to 1A, 2A and 3A": replaces every element of <paramref name="band"/>
        /// ("Header" or "Footer") on each target form's ACTIVE template with a copy of
        /// <paramref name="bandElements"/> — the elements the operator is looking at right now on
        /// the source form (whether or not they have been Saved yet), so the propagation always
        /// carries exactly what is on screen. Every other band on the target (its own Body facts
        /// table) is left untouched. New GUIDs are assigned to the copies so the three templates
        /// never share an element identity, and X/Y/Width/Height are copied verbatim — every form
        /// in the family already shares one page size (612x792 pt), so a Header/Footer element sits
        /// at the same point on every page it lands on.
        /// </summary>
        public static void ApplyBand(List<TemplateElement> bandElements, string band,
            IEnumerable<string> targetFormCodes, int? userId)
        {
            if (bandElements == null) return;
            foreach (string formCode in targetFormCodes)
            {
                CertTemplate target = GetActive(formCode);
                if (target == null) continue;
                target.Elements.RemoveAll(e => string.Equals(e.Band, band, StringComparison.OrdinalIgnoreCase));
                foreach (TemplateElement src in bandElements)
                {
                    TemplateElement copy = src.Clone();
                    copy.Id = Guid.NewGuid().ToString("N");
                    copy.Band = band;
                    target.Elements.Add(copy);
                }
                Save(target, userId);
            }
        }

        // ===================================================================
        // Template-owned images (uploaded logos/seals/etc, not the office-wide slots)
        // ===================================================================

        public static int SaveImage(string formCode, string label, byte[] bytes, string contentType)
        {
            EnsureSeeded(formCode);
            int templateRowId = Convert.ToInt32(Db.Pull(
                "SELECT id FROM certificate_templates WHERE form_code=@f AND is_active=1 LIMIT 1",
                new MySqlParameter("@f", formCode)).Rows[0][0]);

            var img = new MySqlParameter("@d", MySqlDbType.LongBlob) { Value = bytes };
            return (int)Db.Insert(
                "INSERT INTO certificate_template_images (template_id, label, content_type, image_data) " +
                "VALUES (@t, @l, @c, @d)",
                new MySqlParameter("@t", templateRowId),
                new MySqlParameter("@l", (object)label ?? DBNull.Value),
                new MySqlParameter("@c", contentType ?? "image/png"), img);
        }

        public static byte[] LoadImage(int imageId)
        {
            DataTable dt = Db.Pull("SELECT image_data FROM certificate_template_images WHERE id=@id",
                new MySqlParameter("@id", imageId));
            return dt.Rows.Count > 0 ? (byte[])dt.Rows[0][0] : null;
        }

        public static void DeleteImage(int imageId)
        {
            Db.Push("DELETE FROM certificate_template_images WHERE id=@id",
                new MySqlParameter("@id", imageId));
        }

        // ===================================================================
        // Converting the existing hardcoded cell lists into starting elements
        // ===================================================================

        private static TemplateElement ToElement(Form3ACell c)
        {
            var e = new TemplateElement
            {
                X = c.X, Y = c.Top, Width = c.Width, Height = c.Height,
                FontSize = c.FontSize, Bold = c.Bold, Italic = c.Italic,
                Align = c.Center ? "Center" : "Left",
            };
            switch (c.Kind)
            {
                case "Static":
                    e.Kind = "Text";
                    e.Text = c.Text;
                    break;
                case "Field":
                    e.Kind = "Field";
                    e.Column = c.Column;
                    break;
                case "Picture":
                    e.Kind = "Image";
                    e.OfficeAsset = c.Asset.ToString();
                    break;
                case "Rule":
                    e.Kind = "Line";
                    e.Height = 1f;
                    break;
                default:
                    e.Kind = "Text";
                    e.Text = c.Text ?? "";
                    break;
            }
            e.Band = BandForY(e.Y);
            return e;
        }

        /// <summary>Y (points from the top of the page) where the letterhead band ends and
        /// the body begins, and where the body ends and the footer begins. These are the
        /// office's own layout on the 612x792 pt sheet every form in
        /// <see cref="FactsCertificationFamily"/> shares — NOT a rule the renderer enforces.
        /// Band is organizational: it decides what "Apply Header/Footer to 1A/2A/3A"
        /// propagates, and what the designer draws as a guide line, nothing else. An element
        /// may sit anywhere on the page whatever its band says.</summary>
        public const float HeaderBandBottom = 110f;
        public const float FooterBandTop = 660f;

        /// <summary>The band an element at this Y belongs to by default. Used when seeding a
        /// template from a form's hardcoded layout; afterwards the operator may change any
        /// element's band from the designer's properties panel.</summary>
        public static string BandForY(float y) =>
            y < HeaderBandBottom ? "Header" : (y > FooterBandTop ? "Footer" : "Body");

        // ===================================================================
        // Field picker — every column this form's data row can supply, derived from
        // the form's OWN existing BuildTable columns rather than a hand-kept list, so
        // it can never drift from what actually gets filled in.
        // ===================================================================

        public static List<TemplateFieldOption> FieldsFor(string formCode)
        {
            DataTable shape = formCode == Form3ACert.FormCode ? Form3ACert.BuildTable(0)
                             : formCode == Form3BCert.FormCode ? Form3BCert.BuildTable(0)
                             : formCode == Form3CCert.FormCode ? Form3CCert.BuildTable(0)
                             : AssessmentReport.ShapeFor(formCode) ?? new DataTable();

            var list = new List<TemplateFieldOption>();
            foreach (DataColumn col in shape.Columns)
                list.Add(new TemplateFieldOption(col.ColumnName, Humanize(col.ColumnName), GroupOf(col.ColumnName)));
            return list;
        }

        private static string GroupOf(string column)
        {
            if (column.StartsWith("husband_", StringComparison.OrdinalIgnoreCase)) return "Husband";
            if (column.StartsWith("wife_", StringComparison.OrdinalIgnoreCase)) return "Wife";
            if (column.StartsWith("mcr_") || column.Contains("registry") || column.Contains("marriage") ||
                column.Contains("book") || column.Contains("page")) return "Marriage / Registry";
            if (column.StartsWith("child_") || column.Contains("birth")) return "Child / Birth";
            if (column.Contains("amount") || column.Contains("or_number") || column.Contains("date_paid")) return "Payment";
            if (column.Contains("registrar") || column.Contains("verified")) return "Officers";
            if (column.Contains("purpose") || column.Contains("date_issued")) return "Certification";
            return "Office";
        }

        private static string Humanize(string column)
        {
            string s = Regex.Replace(column, "_", " ");
            var ti = CultureInfo.InvariantCulture.TextInfo;
            return ti.ToTitleCase(s);
        }
    }

    /// <summary>Everything the designer needs to know about a form before its template
    /// exists yet: its page size and how to build the seed layout on first open.</summary>
    public class TemplateFormInfo
    {
        public string FormCode, FormName, Category;
        public float PageWidth, PageHeight;
        public Func<List<TemplateElement>> SeedElements;

        public TemplateFormInfo(string code, string name, string category,
                                float w, float h, Func<List<TemplateElement>> seed)
        { FormCode = code; FormName = name; Category = category; PageWidth = w; PageHeight = h; SeedElements = seed; }
    }
}
