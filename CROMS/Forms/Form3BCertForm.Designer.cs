using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;

namespace CROMS.Forms
{
    partial class Form3BCertForm
    {
        private readonly TextBox _txtSearch = new TextBox { Left = 16, Top = 16, Width = 320 };
        private readonly Button _btnSearch = new Button { Left = 344, Top = 14, Width = 90, Text = "Search" };
        private readonly DataGridView _dgvResults = new DataGridView
        {
            Left = 16, Top = 48, Width = 720, Height = 150,
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        private readonly DataGridView _dgvFields = new DataGridView
        {
            Left = 16, Top = 210, Width = 720, Height = 360,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        private readonly Button _btnPreview = new Button { Left = 16, Top = 580, Width = 160, Text = "Preview" };
        private readonly Button _btnPrint = new Button { Left = 184, Top = 580, Width = 160, Text = "Print" };
        private readonly Button _btnAssets = new Button { Left = 460, Top = 580, Width = 276, Text = "Header/Footer Images..." };
        private readonly Label _lblStatus = new Label { Left = 16, Top = 616, Width = 720, Height = 20, ForeColor = Color.DimGray };

        private void InitializeComponent()
        {
            Text = "Print Certification (Form 1A - Birth Available)";
            Width = 780; Height = 700;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            _dgvFields.Columns.Add("Field", "Field");
            _dgvFields.Columns.Add("Value", "Value");
            _dgvFields.Columns[0].ReadOnly = true;

            _btnSearch.Click += (s, e) => RunSearch();
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; RunSearch(); } };
            _dgvResults.CellDoubleClick += (s, e) => LoadSelected();
            _btnPreview.Click += (s, e) => PrintOrPreview(false);
            _btnPrint.Click += (s, e) => PrintOrPreview(true);
            _btnAssets.Click += (s, e) =>
            {
                using (var f = new HeaderFooterImagesForm(Form3BCert.FormCode, "Form 1A - Birth Available",
                    new[]
                    {
                        new HeaderFooterImagesForm.ImageFieldSpec(AssetKind.HeaderLogoLeft, "Header logo - left (LCRO seal)"),
                        new HeaderFooterImagesForm.ImageFieldSpec(AssetKind.HeaderLogoRight1, "Header badge - right 1"),
                        new HeaderFooterImagesForm.ImageFieldSpec(AssetKind.HeaderLogoRight2, "Header badge - right 2"),
                        new HeaderFooterImagesForm.ImageFieldSpec(AssetKind.FooterBanner, "Footer banner"),
                    }))
                    f.ShowDialog(this);
            };

            Controls.Add(_txtSearch);
            Controls.Add(_btnSearch);
            Controls.Add(_dgvResults);
            Controls.Add(_dgvFields);
            Controls.Add(_btnPreview);
            Controls.Add(_btnPrint);
            Controls.Add(_btnAssets);
            Controls.Add(_lblStatus);
        }
    }
}
