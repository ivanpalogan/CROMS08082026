using System;
using System.Data;
using System.IO;
using System.Linq;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.ReportAppServer.ClientDoc;
using CrystalDecisions.ReportAppServer.Controllers;
using CROMS.Data;
using RD = CrystalDecisions.ReportAppServer.ReportDefModel;
using DD = CrystalDecisions.ReportAppServer.DataDefModel;

namespace CROMS.ReportGen
{
    /// <summary>
    /// Builds CROMS\Reports\MF-90-1993.rpt - a real Crystal report - from Mf90Form.Cells.
    /// <para/>
    /// WHY GENERATED, and why this works when 2026-09-06 recorded Crystal authoring as
    /// impossible: that pass only tried ReportClientDocument.New(), which needs a report server.
    /// Opening an EXISTING .rpt and editing it through its in-process ReportClientDocument does
    /// work with Crystal Reports for Visual Studio, and VS ships a blank one as its item template.
    /// So the seed is loaded, given the datasource, page, background and one field per box,
    /// and saved under a new name. The result is an ordinary .rpt: it opens in the VS Crystal
    /// designer, where positions can be nudged by hand.
    /// <para/>
    /// Coordinates are NOT restated here - they are read from Mf90Form.Cells, the same list the
    /// fallback printer draws, so the two can never disagree.
    /// </summary>
    internal static class Program
    {
        private const int Twips = 20;   // 1 pt = 20 twips

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                string seed = args.Length > 0 && File.Exists(args[0]) ? args[0] : FindSeed();
                string outDir = args.Length > 1 ? args[1] : Path.Combine(RepoRoot(), "CROMS", "Reports");
                if (seed == null) { Console.WriteLine("No seed CrystalReport.rpt found - is Crystal Reports for Visual Studio installed?"); return 2; }
                Directory.CreateDirectory(outDir);

                if (File.Exists(Mf90Form.BlankPath))
                {
                    Console.WriteLine("seed  : " + seed);
                    Console.WriteLine("blank : " + Mf90Form.BlankPath);
                    BuildMf90(seed, outDir);
                    Console.WriteLine("wrote : " + Path.Combine(outDir, Mf90Form.RptFile));
                }
                else Console.WriteLine("skip MF-90: blank form not found: " + Mf90Form.BlankPath);

                // FORM 3A / Civil Registry Form No. 1A / Form 2A (the 1A/2A/3A Facts
                // Certification family) have no scanned
                // blank to embed - each is generated from its own class's Static + Picture
                // cells, so the Crystal report and the no-runtime fallback can never draw a
                // label in a different place. All three share one letterhead/footer geometry
                // (see Form3CCert's class doc), so their generated backgrounds line up as one
                // coordinated family.
                string form3aBlank = Form3ACert.RenderBlankTemplate(outDir);
                Console.WriteLine("blank : " + form3aBlank + " (generated)");
                BuildLetterReport(seed, outDir, Form3ACert.RptFile, Form3ACert.PageWidth, Form3ACert.PageHeight,
                    Form3ACert.Cells, Form3ACert.BuildTable(0), form3aBlank);
                Console.WriteLine("wrote : " + Path.Combine(outDir, Form3ACert.RptFile));

                string form3bBlank = Form3BCert.RenderBlankTemplate(outDir);
                Console.WriteLine("blank : " + form3bBlank + " (generated)");
                BuildLetterReport(seed, outDir, Form3BCert.RptFile, Form3BCert.PageWidth, Form3BCert.PageHeight,
                    Form3BCert.Cells, Form3BCert.BuildTable(0), form3bBlank);
                Console.WriteLine("wrote : " + Path.Combine(outDir, Form3BCert.RptFile));

                string form3cBlank = Form3CCert.RenderBlankTemplate(outDir);
                Console.WriteLine("blank : " + form3cBlank + " (generated)");
                BuildLetterReport(seed, outDir, Form3CCert.RptFile, Form3CCert.PageWidth, Form3CCert.PageHeight,
                    Form3CCert.Cells, Form3CCert.BuildTable(0), form3cBlank);
                Console.WriteLine("wrote : " + Path.Combine(outDir, Form3CCert.RptFile));

                // The registry certificates (birth / marriage / death). Each is built from the
                // form's own print map + blank-sheet image, exactly what the built-in overlay
                // draws, so the Crystal report and the overlay place every value identically.
                // A revision with no blank image (MF-102 1993) gets no report of its own:
                // CertificateReport prints it through the current revision's report.
                foreach (FormDefinition fd in FormCatalog.All)
                {
                    string blank = string.IsNullOrEmpty(fd.BlankAsset) ? null
                        : Path.Combine(RepoRoot(), "CROMS", "Assets", fd.BlankAsset);
                    if (blank == null || !File.Exists(blank) || fd.Cells.Count == 0 || string.IsNullOrEmpty(fd.RptFile))
                    {
                        Console.WriteLine("skip " + fd.FormCode + ": no blank-form image");
                        continue;
                    }
                    Console.WriteLine("blank : " + blank);
                    BuildRegistry(seed, outDir, fd, blank);
                    Console.WriteLine("wrote : " + Path.Combine(outDir, fd.RptFile));
                }

                // Mission/Vision/Goal/Objectives/Core Values - no per-record data (every
                // cell is Static text), same generation technique as the three above.
                string mvcBlank = OfficeMissionCert.RenderBlankTemplate(outDir);
                Console.WriteLine("blank : " + mvcBlank + " (generated)");
                BuildLetterReport(seed, outDir, OfficeMissionCert.RptFile, OfficeMissionCert.PageWidth, OfficeMissionCert.PageHeight,
                    OfficeMissionCert.Cells, OfficeMissionCert.BuildTable(), mvcBlank);
                Console.WriteLine("wrote : " + Path.Combine(outDir, OfficeMissionCert.RptFile));
                return 0;
            }
            catch (Exception ex) { Console.WriteLine("FAILED: " + ex); return 1; }
        }

        private static void BuildMf90(string seed, string outDir)
        {
            var doc = new ReportDocument();
            doc.Load(seed);
            ISCDReportClientDocument rcd = doc.ReportClientDocument;

            // ---- datasource: an ADO.NET table with exactly the columns the app binds at runtime.
            var ds = new DataSet("CROMS");
            ds.Tables.Add(Mf90Form.BuildTable(new LicenseFacts()).Clone());
            rcd.DatabaseController.AddDataSource(CrystalDecisions.ReportAppServer.DataSetConversion.DataSetConverter.Convert(ds));
            DD.Table table = (DD.Table)rcd.Database.Tables[0];

            // ---- page: the form's own 8.5 x 13 in long bond, no margins (the blank carries its own).
            int pageW = (int)(Mf90Form.PageWidth * Twips), pageH = (int)(Mf90Form.PageHeight * Twips);
            rcd.PrintOutputController.ModifyUserPaperSize(pageH, pageW);
            rcd.PrintOutputController.ModifyPageMargins(0, 0, 0, 0);

            // ---- one Detail section the height of the page; everything else suppressed.
            // The detail is 1 pt SHORT of the page: at exactly the page height Crystal decides the
            // section does not fit and spills a blank second page (measured in the probe).
            int bodyH = pageH - Twips;
            RD.ReportDefinition def = rcd.ReportDefController.ReportDefinition;
            ReportSectionController sections = rcd.ReportDefController.ReportSectionController;
            foreach (RD.ISCRArea area in new RD.ISCRArea[] { def.ReportHeaderArea, def.PageHeaderArea, def.ReportFooterArea, def.PageFooterArea })
                foreach (RD.Section s in area.Sections)
                {
                    var f = (RD.SectionFormat)s.Format.Clone(true);
                    f.EnableSuppress = true;
                    sections.SetProperty(s, CrReportSectionPropertyEnum.crReportSectionPropertyFormat, f);
                    sections.SetProperty(s, CrReportSectionPropertyEnum.crReportSectionPropertyHeight, 0);
                }
            RD.Section body = def.DetailArea.Sections[0];
            sections.SetProperty(body, CrReportSectionPropertyEnum.crReportSectionPropertyHeight, bodyH);

            ReportObjectController objects = rcd.ReportDefController.ReportObjectController;

            // ---- the blank form, embedded in the report, first so every field sits on top of it.
            RD.ISCRReportObject pic = objects.ImportPicture(Mf90Form.BlankPath, body, 0, 0);
            var sized = (RD.ISCRReportObject)pic.Clone(true);
            sized.Left = 0; sized.Top = 0; sized.Width = pageW; sized.Height = bodyH;
            objects.Modify(pic, sized);

            // ---- one field per printed box.
            foreach (Mf90Cell cell in Mf90Form.Cells)
            {
                DD.Field field = table.DataFields.Cast<DD.Field>().First(x => string.Equals(x.Name, cell.Column, StringComparison.OrdinalIgnoreCase));
                var fo = new RD.FieldObject
                {
                    Name = cell.Column,
                    DataSourceName = field.FormulaForm,
                    FieldValueType = field.Type,
                    Left = (int)Math.Round(cell.X * Twips),
                    Top = (int)Math.Round(cell.Top * Twips),
                    Width = (int)Math.Round(cell.Width * Twips),
                    Height = (int)Math.Round(cell.Height * Twips)
                };
                var font = new RD.Font { Name = "Arial", Size = (decimal)cell.FontSize };
                fo.FontColor = new RD.FontColor { Font = font, Color = 0 };
                var fmt = new RD.ObjectFormat
                {
                    HorizontalAlignment = cell.Center ? RD.CrAlignmentEnum.crAlignmentHorizontalCenter : RD.CrAlignmentEnum.crAlignmentLeft,
                    // A box on paper cannot grow; growing would push the value onto the next row.
                    EnableCanGrow = false
                };
                fo.Format = fmt;
                objects.Add(fo, body, -1);
            }

            // Height LAST. Importing the picture at its native size (18721 twips) grows the
            // section, and shrinking the picture afterwards does not shrink it back - measured:
            // a section set to 18700 BEFORE the import saved as 18721 and spilled a blank page 2.
            sections.SetProperty(body, CrReportSectionPropertyEnum.crReportSectionPropertyHeight, bodyH);

            string target = Path.Combine(outDir, Mf90Form.RptFile);
            if (File.Exists(target)) File.Delete(target);
            object dir = outDir;
            rcd.SaveAs(Mf90Form.RptFile, ref dir, 0);
            doc.Close();
            Console.WriteLine("fields: " + Mf90Form.Cells.Count);
        }

        /// <summary>
        /// Builds one "letter" report (Form 3A, Civil Registry Form No. 1A, Form 2A) the same way
        /// BuildMf90 builds a scanned-form report - the only difference is the background is a
        /// GENERATED image (<c>RenderBlankTemplate</c> on the calling class), not a scan, because
        /// none of the three letters has an office blank on file. Every Field cell becomes one
        /// FieldObject on top of it; Static/Picture cells are already baked into the background
        /// so they are not repeated here. Shared by <c>Form3ACert</c>, <c>Form3BCert</c> and
        /// <c>Form3CCert</c> - all three use the same <c>Form3ACell</c> shape, so one generator
        /// serves any class built on that shape.
        /// </summary>
        private static void BuildLetterReport(string seed, string outDir, string rptFile,
            float pageWidthPt, float pageHeightPt, System.Collections.Generic.IReadOnlyList<Form3ACell> cells,
            DataTable seedTable, string blankPath)
        {
            var doc = new ReportDocument();
            doc.Load(seed);
            ISCDReportClientDocument rcd = doc.ReportClientDocument;

            var ds = new DataSet("CROMS");
            ds.Tables.Add(seedTable.Clone());
            rcd.DatabaseController.AddDataSource(CrystalDecisions.ReportAppServer.DataSetConversion.DataSetConverter.Convert(ds));
            DD.Table table = (DD.Table)rcd.Database.Tables[0];

            int pageW = (int)(pageWidthPt * Twips), pageH = (int)(pageHeightPt * Twips);
            rcd.PrintOutputController.ModifyUserPaperSize(pageH, pageW);
            rcd.PrintOutputController.ModifyPageMargins(0, 0, 0, 0);

            int bodyH = pageH - Twips;
            RD.ReportDefinition def = rcd.ReportDefController.ReportDefinition;
            ReportSectionController sections = rcd.ReportDefController.ReportSectionController;
            foreach (RD.ISCRArea area in new RD.ISCRArea[] { def.ReportHeaderArea, def.PageHeaderArea, def.ReportFooterArea, def.PageFooterArea })
                foreach (RD.Section s in area.Sections)
                {
                    var f = (RD.SectionFormat)s.Format.Clone(true);
                    f.EnableSuppress = true;
                    sections.SetProperty(s, CrReportSectionPropertyEnum.crReportSectionPropertyFormat, f);
                    sections.SetProperty(s, CrReportSectionPropertyEnum.crReportSectionPropertyHeight, 0);
                }
            RD.Section body = def.DetailArea.Sections[0];
            sections.SetProperty(body, CrReportSectionPropertyEnum.crReportSectionPropertyHeight, bodyH);

            ReportObjectController objects = rcd.ReportDefController.ReportObjectController;

            RD.ISCRReportObject pic = objects.ImportPicture(blankPath, body, 0, 0);
            var sized = (RD.ISCRReportObject)pic.Clone(true);
            sized.Left = 0; sized.Top = 0; sized.Width = pageW; sized.Height = bodyH;
            objects.Modify(pic, sized);

            foreach (Form3ACell cell in cells)
            {
                if (cell.Kind != "Field") continue;   // Static/Picture are already in the background
                DD.Field field = table.DataFields.Cast<DD.Field>().First(x => string.Equals(x.Name, cell.Column, StringComparison.OrdinalIgnoreCase));
                var fo = new RD.FieldObject
                {
                    Name = cell.Column,
                    DataSourceName = field.FormulaForm,
                    FieldValueType = field.Type,
                    Left = (int)Math.Round(cell.X * Twips),
                    Top = (int)Math.Round(cell.Top * Twips),
                    Width = (int)Math.Round(cell.Width * Twips),
                    Height = (int)Math.Round(cell.Height * Twips)
                };
                var font = new RD.Font { Name = "Arial", Size = (decimal)cell.FontSize, Bold = cell.Bold };
                fo.FontColor = new RD.FontColor { Font = font, Color = 0 };
                var fmt = new RD.ObjectFormat
                {
                    HorizontalAlignment = cell.Center ? RD.CrAlignmentEnum.crAlignmentHorizontalCenter : RD.CrAlignmentEnum.crAlignmentLeft,
                    EnableCanGrow = false
                };
                fo.Format = fmt;
                objects.Add(fo, body, -1);
            }

            sections.SetProperty(body, CrReportSectionPropertyEnum.crReportSectionPropertyHeight, bodyH);

            string target = Path.Combine(outDir, rptFile);
            if (File.Exists(target)) File.Delete(target);
            object dir = outDir;
            rcd.SaveAs(rptFile, ref dir, 0);
            doc.Close();
            Console.WriteLine("fields: " + cells.Count(c => c.Kind == "Field"));
        }

        /// <summary>
        /// Builds the Crystal report for one registry certificate from FormDefinition's print map:
        /// the blank sheet as a full-page picture, then one string field per printed box (and a
        /// blob field for the stamp) bound to the "cert_print" table CertificateReport.BuildPrintTable
        /// fills at run time. The report holds no logic - dates, place splits and tick boxes are
        /// already resolved in the table - so it cannot disagree with the overlay.
        /// </summary>
        private static void BuildRegistry(string seed, string outDir, FormDefinition fd, string blankPath)
        {
            var doc = new ReportDocument();
            doc.Load(seed);
            ISCDReportClientDocument rcd = doc.ReportClientDocument;

            var ds = new DataSet("CROMS");
            ds.Tables.Add(CertificateReport.BuildPrintTable(fd, null).Clone());
            rcd.DatabaseController.AddDataSource(CrystalDecisions.ReportAppServer.DataSetConversion.DataSetConverter.Convert(ds));
            DD.Table table = (DD.Table)rcd.Database.Tables[0];

            int pageW = (int)(fd.PrintPage.Width * Twips), pageH = (int)(fd.PrintPage.Height * Twips);
            rcd.PrintOutputController.ModifyUserPaperSize(pageH, pageW);
            rcd.PrintOutputController.ModifyPageMargins(0, 0, 0, 0);

            int bodyH = pageH - Twips;
            RD.ReportDefinition def = rcd.ReportDefController.ReportDefinition;
            ReportSectionController sections = rcd.ReportDefController.ReportSectionController;
            foreach (RD.ISCRArea area in new RD.ISCRArea[] { def.ReportHeaderArea, def.PageHeaderArea, def.ReportFooterArea, def.PageFooterArea })
                foreach (RD.Section sec in area.Sections)
                {
                    var f = (RD.SectionFormat)sec.Format.Clone(true);
                    f.EnableSuppress = true;
                    sections.SetProperty(sec, CrReportSectionPropertyEnum.crReportSectionPropertyFormat, f);
                    sections.SetProperty(sec, CrReportSectionPropertyEnum.crReportSectionPropertyHeight, 0);
                }
            RD.Section body = def.DetailArea.Sections[0];
            sections.SetProperty(body, CrReportSectionPropertyEnum.crReportSectionPropertyHeight, bodyH);

            ReportObjectController objects = rcd.ReportDefController.ReportObjectController;

            string gray = ToGrayBmp(blankPath);
            RD.ISCRReportObject pic = objects.ImportPicture(gray, body, 0, 0);
            var sized = (RD.ISCRReportObject)pic.Clone(true);
            sized.Left = 0; sized.Top = 0; sized.Width = pageW; sized.Height = bodyH;
            objects.Modify(pic, sized);

            int n = 0;
            foreach (CertificateReport.PrintBox box in CertificateReport.PrintBoxes(fd))
            {
                DD.Field field = table.DataFields.Cast<DD.Field>().First(x => string.Equals(x.Name, box.Column, StringComparison.OrdinalIgnoreCase));
                var fo = new RD.FieldObject
                {
                    Name = box.Column,
                    DataSourceName = field.FormulaForm,
                    FieldValueType = field.Type,
                    Left = (int)Math.Round(box.X * Twips),
                    Top = (int)Math.Round(box.Y * Twips),
                    Width = (int)Math.Round(box.Width * Twips),
                    Height = (int)Math.Round(box.Height * Twips)
                };
                if (!box.IsStamp)
                {
                    var font = new RD.Font { Name = "Arial", Size = (decimal)box.FontSize, Bold = box.Bold };
                    fo.FontColor = new RD.FontColor { Font = font, Color = 0 };
                    fo.Format = new RD.ObjectFormat
                    {
                        HorizontalAlignment = box.Bold ? RD.CrAlignmentEnum.crAlignmentHorizontalCenter : RD.CrAlignmentEnum.crAlignmentLeft,
                        EnableCanGrow = false
                    };
                }
                objects.Add(fo, body, -1);
                n++;
            }

            // Height LAST (see BuildMf90): importing the picture grows the section.
            sections.SetProperty(body, CrReportSectionPropertyEnum.crReportSectionPropertyHeight, bodyH);

            string target = Path.Combine(outDir, fd.RptFile);
            if (File.Exists(target)) File.Delete(target);
            object dir = outDir;
            rcd.SaveAs(fd.RptFile, ref dir, 0);
            doc.Close();
            try { File.Delete(gray); } catch { }
            Console.WriteLine("fields: " + n);
        }

        /// <summary>Crystal stores an embedded picture as an uncompressed bitmap; the blank sheets
        /// are black line art, so 8-bit grayscale is a fraction of the size with no visible change.</summary>
        private static string ToGrayBmp(string path)
        {
            string outPath = Path.Combine(Path.GetTempPath(), "croms-blank-" + Path.GetFileNameWithoutExtension(path) + ".bmp");
            using (var src = new System.Drawing.Bitmap(path))
            using (var dst = new System.Drawing.Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed))
            {
                var pal = dst.Palette;
                for (int i = 0; i < 256; i++) pal.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                dst.Palette = pal;
                dst.SetResolution(150, 150);
                var bd = dst.LockBits(new System.Drawing.Rectangle(0, 0, dst.Width, dst.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, dst.PixelFormat);
                var row = new byte[dst.Width];
                for (int y = 0; y < dst.Height; y++)
                {
                    for (int x = 0; x < dst.Width; x++)
                    {
                        System.Drawing.Color c = src.GetPixel(x, y);
                        double a = c.A / 255.0;   // flatten alpha onto white
                        double v = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) * a + 255 * (1 - a);
                        row[x] = (byte)Math.Round(v);
                    }
                    System.Runtime.InteropServices.Marshal.Copy(row, 0, bd.Scan0 + y * bd.Stride, dst.Width);
                }
                dst.UnlockBits(bd);
                dst.Save(outPath, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            return outPath;
        }

        private static string FindSeed()
        {
            string[] roots =
            {
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019",
                @"C:\Program Files\Microsoft Visual Studio\2022",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2022",
            };
            foreach (string root in roots.Where(Directory.Exists))
                foreach (string edition in Directory.GetDirectories(root))
                {
                    string p = Path.Combine(edition, @"Common7\IDE\ItemTemplates\CSharp\Reporting\1033\CrystalReport\CrystalReport.rpt");
                    if (File.Exists(p)) return p;
                }
            return null;
        }

        private static string RepoRoot()
        {
            var d = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "CROMS.sln"))) d = d.Parent;
            if (d == null) throw new InvalidOperationException("CROMS.sln not found above " + AppDomain.CurrentDomain.BaseDirectory);
            return d.FullName;
        }
    }
}
