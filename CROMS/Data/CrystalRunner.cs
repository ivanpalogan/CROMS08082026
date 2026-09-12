using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using CROMS.Forms;

namespace CROMS.Data
{
    /// <summary>
    /// The ONLY type in CROMS that touches Crystal Reports.
    /// <para/>
    /// It is isolated on purpose. The Crystal assemblies live in the machine's GAC and are
    /// installed by the Crystal runtime, so a PC without that runtime cannot load them —
    /// and the CLR only resolves an assembly when it JITs a method that mentions its
    /// types. Keeping every Crystal reference inside this class means the rest of CROMS
    /// runs normally on such a PC, and only a call that lands HERE fails, which
    /// <see cref="CertificateReport"/> catches and falls back from.
    /// <para/>
    /// Nothing here knows what a birth certificate is. It loads whatever .rpt it is
    /// handed, binds it to the flat report-view row, fills in whatever parameters that
    /// report happens to declare, and shows it in a viewer whose own toolbar provides
    /// print, page setup, zoom and export (PDF, Excel, Word, RTF, CSV) for free.
    /// </summary>
    internal static class CrystalRunner
    {
        /// <summary>
        /// Columns added to the datasource so a report can show the office branding.
        /// A Crystal report renders an image by binding a Blob field to a byte[] column,
        /// which is the only way to do it with the report Engine alone (setting a picture
        /// object's image at runtime needs the licensed Report Creation API). So the logo
        /// and the stamp travel WITH the data, as two independent columns — the .rpt author
        /// drops <c>logo_image</c> in the header and <c>stamp_image</c> wherever the form
        /// reserves for the stamp, and can suppress either one on its own.
        /// </summary>
        public const string LogoColumn = "logo_image";
        public const string StampColumn = "stamp_image";

        public static void Show(string rptPath, FormDefinition def, DataTable data,
                                byte[] logo, byte[] stamp, IWin32Window owner)
        {
            DataTable bound = WithBranding(data, logo, stamp);

            var report = new ReportDocument();
            report.Load(rptPath, OpenReportMethod.OpenReportByTempCopy);

            // Bind the one flat row. A single-table datasource means the .rpt needs no
            // database logon of its own and no joins — everything the certificate prints
            // is already resolved by the view.
            report.SetDataSource(bound);
            ApplyParameters(report, def, bound);

            var host = new CertificateViewerForm(report, def.FormName +
                "  —  Municipal Form No. " + def.MunicipalFormNo);
            host.ShowDialog(owner);
        }

        /// <summary>
        /// Copy the report row and append the branding columns. The original DataTable is
        /// left untouched — <see cref="CertificateReport"/> also hands it to the built-in
        /// renderer, which would otherwise see phantom columns.
        /// </summary>
        private static DataTable WithBranding(DataTable src, byte[] logo, byte[] stamp)
        {
            DataTable t = src.Copy();
            if (!t.Columns.Contains(LogoColumn)) t.Columns.Add(LogoColumn, typeof(byte[]));
            if (!t.Columns.Contains(StampColumn)) t.Columns.Add(StampColumn, typeof(byte[]));

            foreach (DataRow r in t.Rows)
            {
                r[LogoColumn] = (object)logo ?? DBNull.Value;
                r[StampColumn] = (object)stamp ?? DBNull.Value;
            }
            t.AcceptChanges();
            return t;
        }

        /// <summary>
        /// Fill in the report's parameters from the form identity and the record, but only
        /// the ones that report actually declares.
        /// <para/>
        /// This is what lets one piece of code serve every form's report: a .rpt author
        /// adds a parameter named <c>FormName</c> or <c>RegistryNo</c> and it is populated;
        /// a report that declares none still renders. Setting a parameter a report does not
        /// have throws, and a report parameter left unset prompts the operator with a
        /// dialog — so both directions have to be checked rather than assumed.
        /// </summary>
        private static void ApplyParameters(ReportDocument report, FormDefinition def,
                                            DataTable data)
        {
            DataRow row = data.Rows.Count > 0 ? data.Rows[0] : null;
            OfficeProfile office = OfficeAssets.Profile;

            string FromRow(string column)
            {
                if (row == null || !data.Columns.Contains(column)) return "";
                object v = row[column];
                return v == DBNull.Value || v == null ? "" : v.ToString().Trim();
            }

            var values = new System.Collections.Generic.Dictionary<string, string>
                (StringComparer.OrdinalIgnoreCase)
            {
                // Form identification, which is the point of storing it on the record.
                { "FormName",         def.FormName },
                { "FormCode",         def.FormCode },
                { "FormType",         def.FormType.ToString() },
                { "MunicipalFormNo",  def.MunicipalFormNo },
                { "Revision",         def.Revision },
                { "RegistryNo",       FromRow("registry_no") },
                { "BookVolume",       FromRow("book_volume") },
                { "BookPage",         FromRow("book_page") },
                // The registering office, for the header and signature block.
                { "OfficeName",       office.OfficeName },
                { "Municipality",     office.Municipality },
                { "Province",         office.Province },
                { "RegistrarName",    office.RegistrarName },
                { "RegistrarTitle",   office.RegistrarTitle },
                // Who produced this copy and when — a printed certificate should say.
                { "PrintedBy",        Session.User?.FullName ?? Session.User?.Username ?? "" },
                { "PrintedAt",        DateTime.Now.ToString("dd MMMM yyyy h:mm tt") },
            };

            foreach (ParameterFieldDefinition p in report.DataDefinition.ParameterFields)
            {
                // Skip anything the report computes for itself rather than being told.
                if (p.ParameterType != ParameterType.ReportParameter) continue;

                if (values.TryGetValue(p.ParameterFieldName, out string v))
                {
                    try { report.SetParameterValue(p.ParameterFieldName, v ?? ""); }
                    catch { /* type mismatch on a report-defined parameter: leave it */ }
                }
                else
                {
                    // An unfilled parameter makes Crystal prompt the operator mid-print.
                    // Better a blank than a dialog nobody can answer.
                    try { report.SetParameterValue(p.ParameterFieldName, ""); }
                    catch { }
                }
            }
        }

        // ------------------------------------------------------------ table reports
        // Documents that are not registry certificates (the MF-90 application) have no
        // FormDefinition and no view: CROMS builds the one row itself, already split into the
        // form's boxes, and the report only places it.

        /// <summary>Load a report and bind one DataTable to it. The caller owns disposal.</summary>
        private static ReportDocument LoadTable(string rptPath, DataTable data)
        {
            var report = new ReportDocument();
            report.Load(rptPath, OpenReportMethod.OpenReportByTempCopy);
            report.SetDataSource(data);
            return report;
        }

        /// <summary>Preview a table report in the zoomable viewer (print and export from its toolbar).</summary>
        public static void ShowTable(string rptPath, DataTable data, string caption, string note, IWin32Window owner)
        {
            using (Form host = OpenTable(rptPath, data, caption, note))
                host.ShowDialog(owner);
        }

        /// <summary>The viewer window, not yet shown - the test harness drives zoom on it.</summary>
        public static Form OpenTable(string rptPath, DataTable data, string caption, string note)
        {
            return new CertificateViewerForm(LoadTable(rptPath, data), caption, note);
        }

        /// <summary>Render a table report to PDF with no viewer.</summary>
        public static void ExportTablePdf(string rptPath, DataTable data, string outPath)
        {
            ReportDocument report = LoadTable(rptPath, data);
            try { report.ExportToDisk(ExportFormatType.PortableDocFormat, outPath); }
            finally { report.Close(); report.Dispose(); }
        }

        /// <summary>
        /// Export a report straight to a file, with no viewer. Used by callers that want a
        /// PDF without the operator clicking through the viewer toolbar.
        /// </summary>
        public static void ExportPdf(string rptPath, FormDefinition def, DataTable data,
                                     byte[] logo, byte[] stamp, string outPath)
        {
            var report = new ReportDocument();
            report.Load(rptPath, OpenReportMethod.OpenReportByTempCopy);
            report.SetDataSource(WithBranding(data, logo, stamp));
            ApplyParameters(report, def, data);
            try
            {
                report.ExportToDisk(ExportFormatType.PortableDocFormat, outPath);
            }
            finally
            {
                report.Close();
                report.Dispose();
            }
        }
    }
}
