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

                // FORM 3A / 3B have no scanned blank to embed - each is generated from its own
                // class's Static + Picture cells, so the Crystal report and the no-runtime
                // fallback can never draw a label in a different place.
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
        /// Builds one "letter" report (Form 3A, Form 3B, ...) the same way BuildMf90 builds a
        /// scanned-form report - the only difference is the background is a GENERATED image
        /// (<c>RenderBlankTemplate</c> on the calling class), not a scan, because neither
        /// letter has an office blank on file. Every Field cell becomes one FieldObject on top
        /// of it; Static/Picture cells are already baked into the background so they are not
        /// repeated here. Shared by <c>Form3ACert</c> and <c>Form3BCert</c> - both use the same
        /// <c>Form3ACell</c> shape, so one generator serves any class built on that shape.
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
