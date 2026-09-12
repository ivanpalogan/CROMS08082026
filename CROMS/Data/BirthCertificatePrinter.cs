using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// Prints a filled Municipal Form 102 (Certificate of Live Birth) as a full visual replica:
    /// draws the scanned blank form as a background image, then overlays the record's data at
    /// the exact coordinates of each numbered box (measured from the real PSA form, in points —
    /// the form page is modeled as 792 x 1224 pt = 11 x 17 in, matching the source scan).
    /// No report engine involved — plain GDI+ drawing via PrintDocument, previewed with the
    /// stock PrintPreviewDialog. Coordinates are anchored to Graphics.PageUnit = Point so they
    /// can be used as-is regardless of the paper size chosen at print time.
    /// </summary>
    internal static class BirthCertificatePrinter
    {
        private const float PageW = 792f;   // 11 in
        private const float PageH = 1224f;  // 17 in

        private static Image _bg;

        private static Image Background()
        {
            if (_bg == null)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Form102Blank.png");
                if (File.Exists(path)) _bg = Image.FromFile(path);
            }
            return _bg;
        }

        public static void Print(int birthId, IWin32Window owner)
        {
            DataTable dt = Db.Pull("SELECT * FROM births WHERE id=@id", new MySqlParameter("@id", birthId));
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Record not found.", "Print Certificate",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DataRow r = dt.Rows[0];

            var pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = false;
            try { pd.DefaultPageSettings.PaperSize = new PaperSize("Form 102", 1100, 1700); } catch { }
            pd.PrintPage += (s, e) => DrawCertificate(e, r);

            using (var preview = new PrintPreviewDialog())
            {
                preview.Document = pd;
                preview.Width = 950;
                preview.Height = 1000;
                preview.StartPosition = FormStartPosition.CenterParent;
                preview.ShowDialog(owner);
            }
        }

        private static void DrawCertificate(PrintPageEventArgs e, DataRow r)
        {
            Graphics g = e.Graphics;
            g.PageUnit = GraphicsUnit.Point;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Image bg = Background();
            if (bg != null) g.DrawImage(bg, 0, 0, PageW, PageH);

            using (var f = new Font("Arial", 9f))
            using (var fSmall = new Font("Arial", 7.5f))
            using (var fMark = new Font("Arial", 9f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.Black))
            {
                string V(string col) => r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? r[col].ToString().Trim() : "";
                void S(string text, float x, float y, Font font = null) { if (!string.IsNullOrEmpty(text)) g.DrawString(text, font ?? f, brush, x, y); }
                void Mark(float x, float y) => g.DrawString("X", fMark, brush, x, y);
                string FmtDate(string col)
                {
                    string v = V(col);
                    if (v == "") return "";
                    DateTime d;
                    return DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out d)
                        ? d.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture) : v;
                }
                string[] SplitPlace(string col)
                {
                    string v = V(col);
                    string[] parts = v.Split(new[] { ',' }, 3, StringSplitOptions.None);
                    string[] outp = { "", "", "" };
                    for (int i = 0; i < parts.Length && i < 3; i++) outp[i] = parts[i].Trim();
                    return outp;
                }

                // ---- Header: registering LGU (constant) + registry no. ----
                S("Cagayan", 150, 168, fSmall);
                S(V("registry_no"), 490, 168, fSmall);
                S("Peñablanca", 180, 180, fSmall);

                // ---- Item 1: Child's name ----
                S(V("first_name"), 250, 234);
                S(V("middle_name"), 328, 234);
                S(V("last_name"), 447, 234);

                // ---- Item 2: Sex ----
                string sex = V("sex");
                if (sex.Equals("Male", StringComparison.OrdinalIgnoreCase)) Mark(155, 261);
                else if (sex.Equals("Female", StringComparison.OrdinalIgnoreCase)) Mark(222, 261);

                // ---- Item 3: Date of birth ----
                S(FmtDate("date_of_birth"), 419, 276, fSmall);

                // ---- Item 4: Place of birth (Hospital / City-Municipality / Province) ----
                string[] place = SplitPlace("place_of_birth");
                S(place[0], 225, 328, fSmall);
                S(place[1], 376, 328, fSmall);
                S(place[2], 470, 328, fSmall);

                // ---- Item 5a: Type of birth ----
                string tob = V("type_of_birth");
                if (tob.Equals("Single", StringComparison.OrdinalIgnoreCase)) Mark(136, 355);
                else if (tob.Equals("Twin", StringComparison.OrdinalIgnoreCase)) Mark(210, 355);
                else if (!string.IsNullOrEmpty(tob)) Mark(170, 365); // Triplet, Quadruplet, etc.

                // ---- Item c/d: birth order + weight ----
                S(V("birth_order"), 196, 419, fSmall);
                S(V("weight_grams"), 408, 419, fSmall);

                // ---- Item 6: Mother's maiden name ----
                S(V("mother_first_name"), 253, 469, fSmall);
                S(V("mother_middle_name"), 332, 469, fSmall);
                S(V("mother_last_name"), 445, 469, fSmall);

                // ---- Item 7/8: Mother citizenship / religion ----
                S(V("mother_citizenship"), 152, 500, fSmall);
                S(V("mother_religion"), 407, 500, fSmall);

                // ---- Item 9a/b/c: Mother's children counts ----
                S(V("mother_children_born_alive"), 180, 545, fSmall);
                S(V("mother_children_living"), 335, 545, fSmall);
                S(V("mother_children_dead"), 503, 545, fSmall);

                // ---- Item 10/11: Mother occupation / age ----
                S(V("mother_occupation"), 152, 595, fSmall);
                S(V("mother_age"), 492, 578, fSmall);

                // ---- Item 12: Mother residence ----
                S(V("mother_residence"), 235, 634, fSmall);

                // ---- Item 13: Father's name ----
                S(V("father_first_name"), 250, 668);
                S(V("father_middle_name"), 328, 668);
                S(V("father_last_name"), 450, 668);

                // ---- Item 14/15: Father citizenship / religion ----
                S(V("father_citizenship"), 155, 712, fSmall);
                S(V("father_religion"), 432, 712, fSmall);

                // ---- Item 16/17: Father occupation / age ----
                S(V("father_occupation"), 158, 762, fSmall);
                S(V("father_age"), 492, 752, fSmall);

                // ---- Item 18: Date and place of marriage of parents ----
                string marrPlace = V("parents_marriage_place");
                string marrLine = FmtDate("parents_marriage_date");
                if (!string.IsNullOrEmpty(marrPlace)) marrLine += (marrLine != "" ? " at " : "") + marrPlace;
                S(marrLine, 95, 802, fSmall);

                // ---- Item 19a: Attendant ----
                string att = V("attendant_type");
                if (att.Equals("Physician", StringComparison.OrdinalIgnoreCase)) Mark(160, 828);
                else if (att.Equals("Nurse", StringComparison.OrdinalIgnoreCase)) Mark(318, 828);
                else if (att.Equals("Midwife", StringComparison.OrdinalIgnoreCase)) Mark(478, 828);
                else if (att.StartsWith("Hilot", StringComparison.OrdinalIgnoreCase)) Mark(160, 838);
                else if (!string.IsNullOrEmpty(att)) Mark(318, 838);

                // ---- Item 19b: Certification of birth (time) ----
                S(V("time_of_birth"), 456, 878, fSmall);
                S(V("attendant_address"), 404, 908, fSmall);
                S(V("attendant_name"), 188, 924, fSmall);
                S(V("attendant_title"), 195, 948, fSmall);

                // ---- Item 20: Informant ----
                S(V("informant_address"), 404, 1004, fSmall);
                S(V("informant_name"), 189, 1020, fSmall);
                S(V("informant_relationship"), 228, 1044, fSmall);
                S(FmtDate("informant_date"), 390, 1044, fSmall);

                // ---- Item 21/22: Prepared by / Received at the office ----
                S(V("prepared_by"), 188, 1118, fSmall);
                S(V("received_by"), 425, 1118, fSmall);
            }
        }
    }
}
