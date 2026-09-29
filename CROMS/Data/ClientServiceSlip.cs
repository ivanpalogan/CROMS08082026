using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>One printed item on the Client Service Slip, in points from the page's
    /// top-left. Kinds: Static, Field, Picture, Rule, Box, Fill (dark bar; its text is white).</summary>
    public sealed class SlipCell
    {
        public string Kind, Text, Column;
        public AssetKind Asset;
        public float X, Top, Width, Height, FontSize = 8f;
        public bool Bold, Center;
        public RectangleF Rect { get { return new RectangleF(X, Top, Width, Height); } }
    }

    /// <summary>What one slip says. The head (requester, owner, type, date, staff, remarks) is kept
    /// in client_service_slips against the control number; the per-type details are read from the
    /// source record each time the slip is (re)printed.</summary>
    public sealed class SlipRequest
    {
        public string SourceTable;       // 'petitions' / 'transactions'
        public int SourceId;
        public string RequesterName, Relationship, DocumentOwner;
        public string DocType;           // Birth / Death / Marriage / Others
        public string DocTypeOther;
        public DateTime TxnDate = DateTime.Today;
        public string AttendingStaff, Remarks;
        public string BirthName, BirthDate, MotherMaiden, FatherName, DeathName, MarriageCouple;
    }

    /// <summary>
    /// Client Service Slip - the slip a client receives for a tracking-only transaction
    /// (petition / case tracking, Transactions ledger). Control number is auto-generated
    /// ("YYYY-N"), UNIQUE, and stable: printing the same source record again shows the SAME
    /// number. It prints through CROMS\Reports\CLIENT-SLIP.rpt (Crystal) - open that file in the
    /// Crystal designer and edit it freely, the app prints whatever the .rpt says. If the .rpt
    /// or the Crystal runtime is missing it falls back to the direct-draw layout below.
    /// </summary>
    public static class ClientServiceSlip
    {
        public const string FormCode = "CLIENT-SERVICE-SLIP";
        public const string FormName = "Client Service Slip";
        public const string RptFile = "CLIENT-SLIP.rpt";
        public const float PageWidth = 396f, PageHeight = 612f;

        private static List<SlipCell> _cells;
        public static IReadOnlyList<SlipCell> Cells { get { return _cells ?? (_cells = BuildCells()); } }

        /// <summary>The cells as Form3ACell, so CROMS.ReportGen can reuse BuildLetterReport.</summary>
        public static List<Form3ACell> ReportCells()
        {
            return Cells.Select(c => new Form3ACell { Kind = c.Kind, Text = c.Text, Column = c.Column, Asset = c.Asset,
                X = c.X, Top = c.Top, Width = c.Width, Height = c.Height, FontSize = c.FontSize, Bold = c.Bold, Center = c.Center }).ToList();
        }

        private static List<SlipCell> BuildCells()
        {
            var c = new List<SlipCell>();
            Action<string, float, float, float, float, float, bool, bool> stat = (t, x, yy, w, h, s, b, ctr) =>
                c.Add(new SlipCell { Kind = "Static", Text = t, X = x, Top = yy, Width = w, Height = h, FontSize = s, Bold = b, Center = ctr });
            Action<string, float, float, float, float, float, bool, bool> field = (col, x, yy, w, h, s, b, ctr) =>
                c.Add(new SlipCell { Kind = "Field", Column = col, X = x, Top = yy, Width = w, Height = h, FontSize = s, Bold = b, Center = ctr });
            Action<float, float, float> rule = (x, yy, w) => c.Add(new SlipCell { Kind = "Rule", X = x, Top = yy, Width = w, Height = 1f });
            Action<float, float, float, float> box = (x, yy, w, h) => c.Add(new SlipCell { Kind = "Box", X = x, Top = yy, Width = w, Height = h });
            Action<string, float, float, float, float, float> fill = (t, x, yy, w, h, s) =>
                c.Add(new SlipCell { Kind = "Fill", Text = t, X = x, Top = yy, Width = w, Height = h, FontSize = s, Bold = true, Center = true });

            fill("THIS FORM IS NOT FOR SALE", 16f, 10f, 364f, 16f, 9.5f);
            c.Add(new SlipCell { Kind = "Picture", Asset = AssetKind.HeaderLogoLeft, X = 22f, Top = 32f, Width = 46f, Height = 46f });
            c.Add(new SlipCell { Kind = "Picture", Asset = AssetKind.HeaderLogoRight1, X = 328f, Top = 32f, Width = 46f, Height = 46f });
            stat("LOCAL CIVIL REGISTRY OFFICE", 70f, 38f, 256f, 16f, 12.5f, true, true);
            field("office_line", 70f, 54f, 256f, 14f, 11f, true, true);
            stat("CLIENT'S CONTROL NUMBER", 70f, 78f, 256f, 12f, 8f, true, true);
            box(100f, 90f, 196f, 28f);
            field("control_no", 100f, 94f, 196f, 22f, 15f, true, true);
            rule(16f, 128f, 364f);
            fill("CLIENT SERVICE SLIP", 16f, 134f, 364f, 20f, 13f);

            const float lx = 22f, vx = 198f, vw = 176f;
            float y = 164f;
            Action<string, string> row = (label, col) =>
            {
                stat(label, lx, y, 176f, 22f, 7.5f, true, false);
                field(col, vx, y, vw, 14f, 9f, false, false);
                rule(vx, y + 14f, vw);
                y += 24f;
            };
            row("1.  NAME OF THE REQUESTER", "requester_name");
            row("2.  RELATIONSHIP TO THE DOCUMENT OWNER", "relationship");
            row("3.  DOCUMENT OWNER", "document_owner");

            stat("4.  DOCUMENT TYPE", lx, y, 170f, 12f, 7.5f, true, false);
            float bx = 198f;
            foreach (var t in new[] { Tuple.Create("chk_birth", "Birth", 26f), Tuple.Create("chk_death", "Death", 26f),
                                      Tuple.Create("chk_marriage", "Marriage", 38f), Tuple.Create("chk_others", "Others", 28f) })
            {
                box(bx, y + 1f, 10f, 10f);
                field(t.Item1, bx, y + 1.5f, 10f, 10f, 8f, true, true);
                stat(t.Item2, bx + 13f, y + 1.5f, t.Item3, 10f, 7.5f, false, false);
                bx += 13f + t.Item3 + 2f;
            }
            y += 16f;
            stat("Others, specify:", 198f, y, 70f, 10f, 6.5f, false, false);
            field("doc_type_other", 268f, y - 1f, 106f, 11f, 8f, false, false);
            rule(268f, y + 9f, 106f);
            y += 16f;

            stat("5.  TRANSACTION DATE", lx, y, 170f, 12f, 7.5f, true, false);
            field("txn_month", 198f, y, 44f, 12f, 9f, false, true); rule(198f, y + 13f, 44f);
            stat("/", 244f, y, 8f, 12f, 9f, false, true);
            field("txn_day", 252f, y, 44f, 12f, 9f, false, true); rule(252f, y + 13f, 44f);
            stat("/", 298f, y, 8f, 12f, 9f, false, true);
            field("txn_year", 306f, y, 68f, 12f, 9f, false, true); rule(306f, y + 13f, 68f);
            stat("(MM)", 198f, y + 14f, 44f, 9f, 6f, false, true);
            stat("(DD)", 252f, y + 14f, 44f, 9f, 6f, false, true);
            stat("(YYYY)", 306f, y + 14f, 68f, 9f, 6f, false, true);
            y += 28f;

            row("6.  ATTENDING STAFF", "attending_staff");
            y += 2f;

            // ---- per-type details
            float top = y;
            box(16f, top, 364f, 88f);
            stat("BIRTH", 24f, top + 34f, 60f, 14f, 10f, true, false);
            float dy = top + 6f;
            foreach (var d in new[] { Tuple.Create("Full Name:", "birth_name"), Tuple.Create("Birthdate:", "birth_date"),
                                      Tuple.Create("Mother's Maiden Name:", "mother_maiden"), Tuple.Create("Father's Full Name:", "father_name") })
            {
                stat(d.Item1, 92f, dy, 92f, 12f, 7.5f, false, false);
                field(d.Item2, 184f, dy, 190f, 12f, 8.5f, false, false);
                rule(184f, dy + 13f, 190f);
                dy += 20f;
            }
            top += 88f;
            box(16f, top, 364f, 28f);
            stat("DEATH", 24f, top + 8f, 60f, 14f, 10f, true, false);
            stat("Full Name:", 92f, top + 8f, 92f, 12f, 7.5f, false, false);
            field("death_name", 184f, top + 8f, 190f, 12f, 8.5f, false, false); rule(184f, top + 21f, 190f);
            top += 28f;
            box(16f, top, 364f, 28f);
            stat("MARRIAGE", 24f, top + 8f, 66f, 14f, 10f, true, false);
            stat("Full Name of Couple:", 92f, top + 8f, 92f, 12f, 7.5f, false, false);
            field("marriage_couple", 184f, top + 8f, 190f, 12f, 8.5f, false, false); rule(184f, top + 21f, 190f);
            top += 28f;

            box(16f, top, 364f, 66f);
            stat("REMARKS:", 22f, top + 5f, 52f, 12f, 7.5f, true, false);
            field("remarks_text", 76f, top + 5f, 298f, 56f, 8f, false, false);
            rule(76f, top + 18f, 298f); rule(76f, top + 34f, 298f); rule(76f, top + 50f, 298f);
            top += 66f;

            box(16f, top + 6f, 364f, 30f);
            stat("NOTE:  Present Valid ID preferably National ID; Digital, Ephil or Physical ID.", 24f, top + 14f, 350f, 20f, 7f, true, false);
            stat("THANK YOU!", 16f, top + 44f, 364f, 12f, 9f, true, true);
            stat("We are here to serve you.", 16f, top + 56f, 364f, 10f, 7.5f, false, true);
            return c;
        }

        // ================================================================ control number

        /// <summary>Issues (or returns the existing) control number for a source record and stores the
        /// slip head. Re-saving the same petition keeps its number; a new record gets the next
        /// "YYYY-N" of the year. The unique keys make two PCs racing for the same number safe.</summary>
        public static string Issue(SlipRequest r)
        {
            object staffId = Session.User != null ? (object)Session.User.Id : DBNull.Value;
            Func<MySqlParameter[]> head = () => new[] {
                new MySqlParameter("@rq", N(r.RequesterName)), new MySqlParameter("@rel", N(r.Relationship)),
                new MySqlParameter("@own", N(r.DocumentOwner)), new MySqlParameter("@dt", N(r.DocType)),
                new MySqlParameter("@dto", N(r.DocTypeOther)), new MySqlParameter("@td", r.TxnDate.Date),
                new MySqlParameter("@st", N(r.AttendingStaff)), new MySqlParameter("@rem", N(r.Remarks)),
                new MySqlParameter("@src", r.SourceTable), new MySqlParameter("@sid", r.SourceId) };
            Func<string> existing = () =>
            {
                DataTable d = Db.Pull("SELECT control_no FROM client_service_slips WHERE source_table=@src AND source_id=@sid",
                    new MySqlParameter("@src", r.SourceTable), new MySqlParameter("@sid", r.SourceId));
                return d.Rows.Count > 0 ? d.Rows[0][0].ToString() : null;
            };

            string have = existing();
            if (have != null)
            {
                Db.Push("UPDATE client_service_slips SET requester_name=@rq, relationship=@rel, document_owner=@own, doc_type=@dt, " +
                        "doc_type_other=@dto, txn_date=@td, attending_staff=@st, remarks=@rem WHERE source_table=@src AND source_id=@sid", head());
                return have;
            }
            int year = r.TxnDate.Year;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                int seq = Convert.ToInt32(Db.Pull("SELECT COALESCE(MAX(slip_seq),0)+1 FROM client_service_slips WHERE slip_year=@y",
                    new MySqlParameter("@y", year)).Rows[0][0]);
                string no = year + "-" + seq;
                try
                {
                    Db.Push("INSERT INTO client_service_slips (control_no, slip_year, slip_seq, source_table, source_id, requester_name, relationship, " +
                            "document_owner, doc_type, doc_type_other, txn_date, attending_staff, remarks, created_by) " +
                            "VALUES (@no, @y, @seq, @src, @sid, @rq, @rel, @own, @dt, @dto, @td, @st, @rem, @by)",
                        head().Concat(new[] { new MySqlParameter("@no", no), new MySqlParameter("@y", year),
                                              new MySqlParameter("@seq", seq), new MySqlParameter("@by", staffId) }).ToArray());
                    return no;
                }
                catch (MySqlException m) when (m.Number == 1062)
                {
                    // Another PC took this number (or the same record raced): retry / reuse.
                    string again = existing();
                    if (again != null) return again;
                }
            }
            throw new InvalidOperationException("Could not issue a control number - please try again.");
        }

        private static object N(string s) { return string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim(); }

        // ================================================================ data

        public static DataTable BuildTable(string controlNo, SlipRequest r)
        {
            var t = new DataTable("client_service_slip");
            foreach (SlipCell cell in Cells.Where(x => x.Kind == "Field"))
                if (!t.Columns.Contains(cell.Column)) t.Columns.Add(cell.Column, typeof(string));
            DataRow row = t.NewRow();
            foreach (DataColumn col in t.Columns) row[col] = "";
            if (r == null) { t.Rows.Add(row); return t; }

            OfficeProfile office = OfficeAssets.Profile;
            row["office_line"] = "LGU-" + (office.MunicipalityForPrint ?? "").ToUpperInvariant();
            row["control_no"] = controlNo ?? "";
            row["requester_name"] = r.RequesterName ?? "";
            row["relationship"] = r.Relationship ?? "";
            row["document_owner"] = r.DocumentOwner ?? "";
            string dt = (r.DocType ?? "").Trim().ToLowerInvariant();
            row["chk_birth"] = dt == "birth" ? "X" : "";
            row["chk_death"] = dt == "death" ? "X" : "";
            row["chk_marriage"] = dt == "marriage" ? "X" : "";
            bool other = dt.Length > 0 && dt != "birth" && dt != "death" && dt != "marriage";
            row["chk_others"] = other ? "X" : "";
            row["doc_type_other"] = other ? (r.DocTypeOther ?? "") : "";
            row["txn_month"] = r.TxnDate.ToString("MM");
            row["txn_day"] = r.TxnDate.ToString("dd");
            row["txn_year"] = r.TxnDate.ToString("yyyy");
            row["attending_staff"] = r.AttendingStaff ?? "";
            row["birth_name"] = r.BirthName ?? "";
            row["birth_date"] = r.BirthDate ?? "";
            row["mother_maiden"] = r.MotherMaiden ?? "";
            row["father_name"] = r.FatherName ?? "";
            row["death_name"] = r.DeathName ?? "";
            row["marriage_couple"] = r.MarriageCouple ?? "";
            row["remarks_text"] = r.Remarks ?? "";
            t.Rows.Add(row);
            t.AcceptChanges();
            return t;
        }

        // ================================================================ show / draw

        /// <summary>Issues the control number and shows the slip (Crystal .rpt first, built-in fallback).</summary>
        public static string Show(SlipRequest r, System.Windows.Forms.IWin32Window owner)
        {
            string no = Issue(r);
            DataTable t = BuildTable(no, r);
            string rpt = System.IO.Path.Combine(CertificateReport.ReportsFolder, RptFile);
            if (CertificateReport.CrystalAvailable && System.IO.File.Exists(rpt))
            {
                try { CrystalRunner.ShowTable(rpt, t, FormName + " " + no, null, owner); return no; }
                catch { /* fall through to the built-in renderer */ }
            }
            using (var doc = BuiltInDocument(t))
            using (var f = new CROMS.Forms.ZoomPrintPreviewForm(doc, FormName + " " + no, null))
                f.ShowDialog(owner);
            return no;
        }

        public static System.Drawing.Printing.PrintDocument BuiltInDocument(DataTable t)
        {
            var doc = new System.Drawing.Printing.PrintDocument { DocumentName = FormName };
            try { doc.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("Slip",
                (int)(PageWidth / 72f * 100f), (int)(PageHeight / 72f * 100f)); } catch { }
            doc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0);
            doc.PrintPage += (s, e) =>
            {
                e.Graphics.PageUnit = GraphicsUnit.Point;
                if (!doc.PrintController.IsPreview)
                    e.Graphics.TranslateTransform(-e.PageSettings.HardMarginX * 0.72f, -e.PageSettings.HardMarginY * 0.72f);
                Draw(e.Graphics, t);
                e.HasMorePages = false;
            };
            return doc;
        }

        public static void Draw(Graphics g, DataTable t)
        {
            g.PageUnit = GraphicsUnit.Point;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            DataRow r = t.Rows.Count > 0 ? t.Rows[0] : null;
            using (var left = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Near })
            using (var center = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center })
            using (var pen = new Pen(Color.Black, 0.75f))
            using (var darkBrush = new SolidBrush(Color.FromArgb(38, 38, 38)))
            {
                foreach (SlipCell c in Cells)
                {
                    switch (c.Kind)
                    {
                        case "Rule": g.DrawLine(pen, c.X, c.Top, c.X + c.Width, c.Top); break;
                        case "Box": g.DrawRectangle(pen, c.X, c.Top, c.Width, c.Height); break;
                        case "Fill":
                            g.FillRectangle(darkBrush, c.Rect);
                            using (var f = new Font("Arial", c.FontSize, FontStyle.Bold))
                                g.DrawString(c.Text, f, Brushes.White, new RectangleF(c.X, c.Top + 3f, c.Width, c.Height), center);
                            break;
                        case "Static":
                            using (var f = new Font("Arial", c.FontSize, c.Bold ? FontStyle.Bold : FontStyle.Regular))
                                g.DrawString(c.Text, f, Brushes.Black, c.Rect, c.Center ? center : left);
                            break;
                        case "Picture":
                            Image img = OfficeAssets.Get(c.Asset, FormCode);
                            if (img != null) g.DrawImage(img, c.Rect);
                            break;
                        default:
                            string v = r != null && t.Columns.Contains(c.Column) ? r[c.Column] as string : null;
                            if (string.IsNullOrEmpty(v)) break;
                            using (var f = new Font("Arial", c.FontSize, c.Bold ? FontStyle.Bold : FontStyle.Regular))
                                g.DrawString(v, f, Brushes.Black, c.Rect, c.Center ? center : left);
                            break;
                    }
                }
            }
        }

        /// <summary>Blank background for the Crystal report: every non-Field cell drawn once.</summary>
        public static string RenderBlankTemplate(string outDir)
        {
            const float scale = 2f;
            using (var bmp = new Bitmap((int)(PageWidth * scale), (int)(PageHeight * scale)))
            {
                bmp.SetResolution(72f, 72f);   // before FromImage: Draw uses PageUnit=Point, which applies dpi/72 on top of the scale
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    g.ScaleTransform(scale, scale);
                    Draw(g, new DataTable());
                }
                System.IO.Directory.CreateDirectory(outDir);
                string path = System.IO.Path.Combine(outDir, "ClientSlipBlank_generated.png");
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                return path;
            }
        }
    }
}
