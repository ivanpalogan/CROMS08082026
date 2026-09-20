using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// Print preview for documents CROMS draws itself - used for a form whose Crystal report
    /// cannot run on this PC (the Crystal runtime is installed per machine and a client PC may
    /// not have it). Same zoom behaviour as the Crystal viewer so the operator meets one way of
    /// working: Ctrl + mouse wheel, Ctrl + / Ctrl - / Ctrl 0, and a zoom bar.
    /// <para/>
    /// Replaces PrintPreviewDialog for this purpose because that dialog's zoom is a toolbar
    /// dropdown only and it swallows the wheel.
    /// </summary>
    internal sealed partial class ZoomPrintPreviewForm : Form
    {
        private PrintDocument _doc;
        private readonly PrintDocument _originalDoc;
        private CtrlWheelZoom _wheel;
        private int _zoom = 100;

        // Set only when this preview was drawn from an editable certificate template.
        // The form code identifies which template to open; the rebuild delegate re-renders
        // this same record through the template the operator just saved, so the preview
        // shows their change instead of the layout it was opened with.
        private readonly CROMS.Data.TemplateFormInfo _editForm;
        private readonly Func<PrintDocument> _rebuild;

        public int ZoomPercent { get { return _zoom; } }
        public PrintPreviewControl View { get { return _view; } }

        public ZoomPrintPreviewForm(PrintDocument doc, string caption, string note)
            : this(doc, caption, note, null, null) { }

        public ZoomPrintPreviewForm(PrintDocument doc, string caption, string note,
                                    CROMS.Data.TemplateFormInfo editForm, Func<PrintDocument> rebuild)
        {
            _doc = doc;
            _originalDoc = doc;        // the caller owns this one; any rebuilt replacement is ours
            _editForm = editForm;
            _rebuild = rebuild;
            InitializeComponent(caption, note);
        }

        public void SetZoom(int percent)
        {
            _zoom = Math.Max(10, Math.Min(400, percent));
            _view.AutoZoom = false;
            _view.Zoom = _zoom / 100.0;
            _zoomLabel.Text = _zoom + "%";
        }

        private int FitPercent(bool width)
        {
            // PrintPreviewControl lays a page out at 1 display pixel per 1/100 inch at Zoom 1.0.
            PaperSize ps = _doc.DefaultPageSettings.PaperSize;
            double pw = ps.Width, ph = ps.Height;
            double aw = Math.Max(200, _view.ClientSize.Width - 40), ah = Math.Max(200, _view.ClientSize.Height - 40);
            double pct = width ? aw / pw * 100.0 : Math.Min(aw / pw, ah / ph) * 100.0;
            return Math.Max(10, (int)Math.Floor(pct / 5.0) * 5);
        }

        public void FitPage() { SetZoom(FitPercent(false)); }
        public void FitWidth() { SetZoom(FitPercent(true)); }

        /// <summary>Opens the visual Template Designer on the form being previewed, then redraws
        /// this preview from whatever the operator saved. Opens read-only — the designer's own
        /// "Edit Template" button carries the admin re-verification, so this adds no new way
        /// around it. Re-rendering on close is what makes the edit visible without the operator
        /// having to find the record and print it again.</summary>
        private void EditLayout()
        {
            using (var designer = new TemplateDesignerForm(_editForm, false))
                designer.ShowDialog(this);

            if (_rebuild == null) return;
            PrintDocument fresh;
            try { fresh = _rebuild(); }
            catch { return; }          // keep showing the page we already have
            if (fresh == null) return;

            PrintDocument old = _doc;
            _doc = fresh;
            _view.Document = fresh;
            _view.InvalidatePreview();
            FitPage();
            // Never dispose the document the caller handed us — its own using block owns it.
            if (!ReferenceEquals(old, fresh) && !ReferenceEquals(old, _originalDoc)) old.Dispose();
        }

        private void PrintNow()
        {
            using (var dlg = new PrintDialog { Document = _doc, UseEXDialog = true })
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try { _doc.Print(); }
                    catch (Exception ex) { MessageBox.Show(this, ex.Message, "Print failed", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.Oemplus: case Keys.Control | Keys.Add: SetZoom(CtrlWheelZoom.Next(_zoom, 1)); return true;
                case Keys.Control | Keys.OemMinus: case Keys.Control | Keys.Subtract: SetZoom(CtrlWheelZoom.Next(_zoom, -1)); return true;
                case Keys.Control | Keys.D0: case Keys.Control | Keys.NumPad0: FitPage(); return true;
                case Keys.Control | Keys.P: PrintNow(); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (_wheel != null) { _wheel.Dispose(); _wheel = null; }
            // The caller disposes the document it handed us; a rebuilt one is ours to clean up.
            if (!ReferenceEquals(_doc, _originalDoc)) { _doc.Dispose(); _doc = _originalDoc; }
        }
    }
}
