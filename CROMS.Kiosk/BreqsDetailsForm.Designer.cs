using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// PSA Copy (BREQS) step - which PSA certificate, the requester's ID, and the details of the
    /// document. Shown between Select Services and Personal Info &amp; Photo only when BREQS was chosen.
    /// Designer-generated visual tree (behaviour in BreqsDetailsForm.cs).
    /// </summary>
    public partial class BreqsDetailsForm
    {
        private System.ComponentModel.IContainer components = null;

        private Panel header;
        private Label lblTitle;
        private StepIndicator _stepInd;
        private Panel footer;
        private Panel footerDivider;
        private Button _btnBack;
        private Button _btnNext;
        private Panel _host;
        private Panel panelStep;
        private Panel _box;
        private RoundPanel leftCard;
        private RoundPanel rightCard;
        private Label lblLeftTitle;
        private Label lblLeftHint;
        private Label lblDocType;
        private Label lblDocTypeReq;
        private PillToggle _pillBirth;
        private PillToggle _pillMarriage;
        private PillToggle _pillDeath;
        private Label lblCopies;
        private RoundPanel hostCopies;
        private ComboBox _cboCopies;
        private Label lblPurpose;
        private RoundPanel hostPurpose;
        private ComboBox _cboPurpose;
        private Label lblRelationship;
        private RoundPanel hostRelationship;
        private ComboBox _cboRelationship;
        private Label lblIdType;
        private Label lblIdTypeReq;
        private RoundPanel hostIdType;
        private ComboBox _cboIdType;
        private Label lblIdNo;
        private Label lblIdNoReq;
        private RoundPanel hostIdNo;
        private TextBox _txtIdNo;
        private Label lblPsaNote;
        private Label lblRightTitle;
        private Label lblRightHint;
        private Label _lblOwnerHead;
        private Label lblOwnerFirst;
        private Label lblOwnerFirstReq;
        private RoundPanel hostOwnerFirst;
        private TextBox _txtOwnerFirst;
        private Label lblOwnerMiddle;
        private RoundPanel hostOwnerMiddle;
        private TextBox _txtOwnerMiddle;
        private Label lblOwnerLast;
        private Label lblOwnerLastReq;
        private RoundPanel hostOwnerLast;
        private TextBox _txtOwnerLast;
        private Label _lblSpouseHead;
        private Label lblSpouseFirst;
        private Label lblSpouseFirstReq;
        private RoundPanel hostSpouseFirst;
        private TextBox _txtSpouseFirst;
        private Label lblSpouseMiddle;
        private RoundPanel hostSpouseMiddle;
        private TextBox _txtSpouseMiddle;
        private Label lblSpouseLast;
        private Label lblSpouseLastReq;
        private RoundPanel hostSpouseLast;
        private TextBox _txtSpouseLast;
        private Label _lblDateHead;
        private RoundPanel hostDate;
        private DateTimePicker _dtpEvent;
        private Label lblCity;
        private RoundPanel hostCity;
        private TextBox _txtCity;
        private Label lblProvince;
        private RoundPanel hostProvince;
        private TextBox _txtProvince;
        private Label lblFather;
        private RoundPanel hostFather;
        private TextBox _txtFather;
        private Label lblMother;
        private RoundPanel hostMother;
        private TextBox _txtMother;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.header = new Panel();
            this.lblTitle = new Label();
            this._stepInd = new StepIndicator();
            this.footer = new Panel();
            this.footerDivider = new Panel();
            this._btnBack = new Button();
            this._btnNext = new Button();
            this._host = new Panel();
            this.panelStep = new Panel();
            this._box = new Panel();
            this.leftCard = new RoundPanel();
            this.rightCard = new RoundPanel();
            this.lblLeftTitle = new Label();
            this.lblLeftHint = new Label();
            this.lblDocType = new Label();
            this.lblDocTypeReq = new Label();
            this._pillBirth = new PillToggle();
            this._pillMarriage = new PillToggle();
            this._pillDeath = new PillToggle();
            this.lblCopies = new Label();
            this.hostCopies = new RoundPanel();
            this._cboCopies = new ComboBox();
            this.lblPurpose = new Label();
            this.hostPurpose = new RoundPanel();
            this._cboPurpose = new ComboBox();
            this.lblRelationship = new Label();
            this.hostRelationship = new RoundPanel();
            this._cboRelationship = new ComboBox();
            this.lblIdType = new Label();
            this.lblIdTypeReq = new Label();
            this.hostIdType = new RoundPanel();
            this._cboIdType = new ComboBox();
            this.lblIdNo = new Label();
            this.lblIdNoReq = new Label();
            this.hostIdNo = new RoundPanel();
            this._txtIdNo = new TextBox();
            this.lblPsaNote = new Label();
            this.lblRightTitle = new Label();
            this.lblRightHint = new Label();
            this._lblOwnerHead = new Label();
            this.lblOwnerFirst = new Label();
            this.lblOwnerFirstReq = new Label();
            this.hostOwnerFirst = new RoundPanel();
            this._txtOwnerFirst = new TextBox();
            this.lblOwnerMiddle = new Label();
            this.hostOwnerMiddle = new RoundPanel();
            this._txtOwnerMiddle = new TextBox();
            this.lblOwnerLast = new Label();
            this.lblOwnerLastReq = new Label();
            this.hostOwnerLast = new RoundPanel();
            this._txtOwnerLast = new TextBox();
            this._lblSpouseHead = new Label();
            this.lblSpouseFirst = new Label();
            this.lblSpouseFirstReq = new Label();
            this.hostSpouseFirst = new RoundPanel();
            this._txtSpouseFirst = new TextBox();
            this.lblSpouseMiddle = new Label();
            this.hostSpouseMiddle = new RoundPanel();
            this._txtSpouseMiddle = new TextBox();
            this.lblSpouseLast = new Label();
            this.lblSpouseLastReq = new Label();
            this.hostSpouseLast = new RoundPanel();
            this._txtSpouseLast = new TextBox();
            this._lblDateHead = new Label();
            this.hostDate = new RoundPanel();
            this._dtpEvent = new DateTimePicker();
            this.lblCity = new Label();
            this.hostCity = new RoundPanel();
            this._txtCity = new TextBox();
            this.lblProvince = new Label();
            this.hostProvince = new RoundPanel();
            this._txtProvince = new TextBox();
            this.lblFather = new Label();
            this.hostFather = new RoundPanel();
            this._txtFather = new TextBox();
            this.lblMother = new Label();
            this.hostMother = new RoundPanel();
            this._txtMother = new TextBox();
            this.header.SuspendLayout();
            this.footer.SuspendLayout();
            this.footerDivider.SuspendLayout();
            this._host.SuspendLayout();
            this.panelStep.SuspendLayout();
            this._box.SuspendLayout();
            this.leftCard.SuspendLayout();
            this.rightCard.SuspendLayout();
            this.hostCopies.SuspendLayout();
            this.hostPurpose.SuspendLayout();
            this.hostRelationship.SuspendLayout();
            this.hostIdType.SuspendLayout();
            this.hostIdNo.SuspendLayout();
            this.hostOwnerFirst.SuspendLayout();
            this.hostOwnerMiddle.SuspendLayout();
            this.hostOwnerLast.SuspendLayout();
            this.hostSpouseFirst.SuspendLayout();
            this.hostSpouseMiddle.SuspendLayout();
            this.hostSpouseLast.SuspendLayout();
            this.hostDate.SuspendLayout();
            this.hostCity.SuspendLayout();
            this.hostProvince.SuspendLayout();
            this.hostFather.SuspendLayout();
            this.hostMother.SuspendLayout();
            this.SuspendLayout();
            //
            // header
            //
            this.header.BackColor = Color.FromArgb(244, 246, 249);
            this.header.Dock = DockStyle.Top;
            this.header.Height = 128;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI", 30F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.FromArgb(23, 26, 36);
            this.lblTitle.Location = new Point(40, 18);
            this.lblTitle.Text = "Request a Service";
            this._stepInd.Location = new Point(40, 74);
            this._stepInd.Size = new Size(760, 52);
            this._stepInd.Steps = new string[] { "Select Services", "PSA Document", "Personal Info & Photo" };
            //
            // footer
            //
            this.footer.BackColor = Color.White;
            this.footer.Dock = DockStyle.Bottom;
            this.footer.Height = 92;
            this.footerDivider.BackColor = Color.FromArgb(225, 229, 236);
            this.footerDivider.Dock = DockStyle.Top;
            this.footerDivider.Height = 1;
            this._btnBack.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this._btnBack.BackColor = Color.White;
            this._btnBack.Cursor = Cursors.Hand;
            this._btnBack.FlatStyle = FlatStyle.Flat;
            this._btnBack.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            this._btnBack.ForeColor = Color.FromArgb(23, 26, 36);
            this._btnBack.Location = new Point(40, 604);
            this._btnBack.Size = new Size(210, 70);
            this._btnBack.Text = "Back";
            this._btnBack.UseVisualStyleBackColor = false;
            this._btnBack.Click += new System.EventHandler(this.BtnBack_Click);
            this._btnNext.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this._btnNext.BackColor = Color.FromArgb(29, 78, 216);
            this._btnNext.Cursor = Cursors.Hand;
            this._btnNext.FlatAppearance.BorderSize = 0;
            this._btnNext.FlatStyle = FlatStyle.Flat;
            this._btnNext.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            this._btnNext.ForeColor = Color.White;
            this._btnNext.Location = new Point(1004, 603);
            this._btnNext.Size = new Size(240, 70);
            this._btnNext.Text = "Next";
            this._btnNext.UseVisualStyleBackColor = false;
            this._btnNext.Click += new System.EventHandler(this.BtnNext_Click);
            //
            // _host / panelStep / _box
            //
            this._host.BackColor = Color.FromArgb(244, 246, 249);
            this._host.Dock = DockStyle.Fill;
            this.panelStep.AutoScroll = true;
            this.panelStep.BackColor = Color.FromArgb(244, 246, 249);
            this.panelStep.Dock = DockStyle.Fill;
            this.panelStep.Resize += new System.EventHandler(this.PanelStep_Resize);
            this._box.BackColor = Color.FromArgb(244, 246, 249);
            this._box.Location = new Point(20, 16);
            this._box.Size = new Size(1200, 700);
            this.leftCard.Fill = Color.White;
            this.leftCard.Location = new Point(6, 6);
            this.leftCard.Radius = 14;
            this.leftCard.Shadow = 8;
            this.leftCard.Size = new Size(600, 688);
            this.rightCard.Fill = Color.White;
            this.rightCard.Location = new Point(620, 6);
            this.rightCard.Radius = 14;
            this.rightCard.Shadow = 8;
            this.rightCard.Size = new Size(574, 688);
            //
            // left card - which document, requester ID
            //
            this.lblLeftTitle.AutoSize = true;
            this.lblLeftTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.lblLeftTitle.ForeColor = Color.FromArgb(23, 26, 36);
            this.lblLeftTitle.Location = new Point(30, 18);
            this.lblLeftTitle.Text = "PSA Document";
            this.lblLeftHint.AutoSize = true;
            this.lblLeftHint.Font = new Font("Segoe UI", 10F);
            this.lblLeftHint.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblLeftHint.Location = new Point(30, 60);
            this.lblLeftHint.Text = "PSA issues this copy. You will come back to claim it.";
            this.lblDocType.AutoSize = true;
            this.lblDocType.Font = new Font("Segoe UI", 9.5F);
            this.lblDocType.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblDocType.Location = new Point(30, 96);
            this.lblDocType.Text = "Which certificate do you need?";
            this.lblDocTypeReq.AutoSize = true;
            this.lblDocTypeReq.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblDocTypeReq.ForeColor = Color.FromArgb(198, 50, 63);
            this.lblDocTypeReq.Location = new Point(30, 96);
            this.lblDocTypeReq.Text = "*";
            this._pillBirth.GlyphCode = 0xE774;
            this._pillBirth.LabelText = "Birth";
            this._pillBirth.Location = new Point(30, 122);
            this._pillBirth.Size = new Size(170, 112);
            this._pillMarriage.GlyphCode = 0xE2A8;
            this._pillMarriage.LabelText = "Marriage";
            this._pillMarriage.Location = new Point(213, 122);
            this._pillMarriage.Size = new Size(170, 112);
            this._pillDeath.GlyphCode = 0xE766;
            this._pillDeath.LabelText = "Death";
            this._pillDeath.Location = new Point(396, 122);
            this._pillDeath.Size = new Size(170, 112);
            this.lblCopies.AutoSize = true;
            this.lblCopies.Font = new Font("Segoe UI", 9.5F);
            this.lblCopies.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblCopies.Location = new Point(30, 252);
            this.lblCopies.Text = "Copies";
            this.hostCopies.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostCopies.BorderWidth = 1.5F;
            this.hostCopies.Fill = Color.White;
            this.hostCopies.Location = new Point(30, 278);
            this.hostCopies.Radius = 8;
            this.hostCopies.Size = new Size(170, 46);
            this._cboCopies.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._cboCopies.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cboCopies.FlatStyle = FlatStyle.Flat;
            this._cboCopies.Font = new Font("Segoe UI", 12F);
            this._cboCopies.Location = new Point(12, 9);
            this._cboCopies.Size = new Size(144, 27);
            this._cboCopies.Enter += new System.EventHandler(this.Field_Enter);
            this._cboCopies.Leave += new System.EventHandler(this.Field_Leave);
            this.lblPurpose.AutoSize = true;
            this.lblPurpose.Font = new Font("Segoe UI", 9.5F);
            this.lblPurpose.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblPurpose.Location = new Point(213, 252);
            this.lblPurpose.Text = "Purpose";
            this.hostPurpose.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostPurpose.BorderWidth = 1.5F;
            this.hostPurpose.Fill = Color.White;
            this.hostPurpose.Location = new Point(213, 278);
            this.hostPurpose.Radius = 8;
            this.hostPurpose.Size = new Size(353, 46);
            this._cboPurpose.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._cboPurpose.DropDownStyle = ComboBoxStyle.DropDown;
            this._cboPurpose.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            this._cboPurpose.AutoCompleteSource = AutoCompleteSource.ListItems;
            this._cboPurpose.FlatStyle = FlatStyle.Flat;
            this._cboPurpose.Font = new Font("Segoe UI", 12F);
            this._cboPurpose.Location = new Point(12, 9);
            this._cboPurpose.Size = new Size(327, 27);
            this._cboPurpose.Enter += new System.EventHandler(this.Field_Enter);
            this._cboPurpose.Leave += new System.EventHandler(this.Field_Leave);
            this.lblRelationship.AutoSize = true;
            this.lblRelationship.Font = new Font("Segoe UI", 9.5F);
            this.lblRelationship.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblRelationship.Location = new Point(30, 338);
            this.lblRelationship.Text = "Relationship to the person on the document";
            this.hostRelationship.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostRelationship.BorderWidth = 1.5F;
            this.hostRelationship.Fill = Color.White;
            this.hostRelationship.Location = new Point(30, 364);
            this.hostRelationship.Radius = 8;
            this.hostRelationship.Size = new Size(536, 46);
            this._cboRelationship.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._cboRelationship.DropDownStyle = ComboBoxStyle.DropDown;
            this._cboRelationship.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            this._cboRelationship.AutoCompleteSource = AutoCompleteSource.ListItems;
            this._cboRelationship.FlatStyle = FlatStyle.Flat;
            this._cboRelationship.Font = new Font("Segoe UI", 12F);
            this._cboRelationship.Location = new Point(12, 9);
            this._cboRelationship.Size = new Size(510, 27);
            this._cboRelationship.Enter += new System.EventHandler(this.Field_Enter);
            this._cboRelationship.Leave += new System.EventHandler(this.Field_Leave);
            this.lblIdType.AutoSize = true;
            this.lblIdType.Font = new Font("Segoe UI", 9.5F);
            this.lblIdType.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblIdType.Location = new Point(30, 424);
            this.lblIdType.Text = "Valid ID you will present";
            this.lblIdTypeReq.AutoSize = true;
            this.lblIdTypeReq.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblIdTypeReq.ForeColor = Color.FromArgb(198, 50, 63);
            this.lblIdTypeReq.Location = new Point(30, 424);
            this.lblIdTypeReq.Text = "*";
            this.hostIdType.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostIdType.BorderWidth = 1.5F;
            this.hostIdType.Fill = Color.White;
            this.hostIdType.Location = new Point(30, 450);
            this.hostIdType.Radius = 8;
            this.hostIdType.Size = new Size(536, 46);
            this._cboIdType.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._cboIdType.DropDownStyle = ComboBoxStyle.DropDown;
            this._cboIdType.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            this._cboIdType.AutoCompleteSource = AutoCompleteSource.ListItems;
            this._cboIdType.FlatStyle = FlatStyle.Flat;
            this._cboIdType.Font = new Font("Segoe UI", 12F);
            this._cboIdType.Location = new Point(12, 9);
            this._cboIdType.Size = new Size(510, 27);
            this._cboIdType.Enter += new System.EventHandler(this.Field_Enter);
            this._cboIdType.Leave += new System.EventHandler(this.Field_Leave);
            this.lblIdNo.AutoSize = true;
            this.lblIdNo.Font = new Font("Segoe UI", 9.5F);
            this.lblIdNo.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblIdNo.Location = new Point(30, 510);
            this.lblIdNo.Text = "ID number";
            this.lblIdNoReq.AutoSize = true;
            this.lblIdNoReq.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblIdNoReq.ForeColor = Color.FromArgb(198, 50, 63);
            this.lblIdNoReq.Location = new Point(30, 510);
            this.lblIdNoReq.Text = "*";
            this.hostIdNo.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostIdNo.BorderWidth = 1.5F;
            this.hostIdNo.Fill = Color.White;
            this.hostIdNo.Location = new Point(30, 536);
            this.hostIdNo.Radius = 8;
            this.hostIdNo.Size = new Size(536, 46);
            this._txtIdNo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtIdNo.BorderStyle = BorderStyle.None;
            this._txtIdNo.Font = new Font("Segoe UI", 13F);
            this._txtIdNo.Location = new Point(14, 12);
            this._txtIdNo.Size = new Size(508, 25);
            this._txtIdNo.Enter += new System.EventHandler(this.Field_Enter);
            this._txtIdNo.Leave += new System.EventHandler(this.Field_Leave);
            this.lblPsaNote.Font = new Font("Segoe UI", 9.5F);
            this.lblPsaNote.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblPsaNote.Location = new Point(30, 596);
            this.lblPsaNote.Size = new Size(536, 76);
            this.lblPsaNote.Text = "PSA releases a birth, marriage or death certificate only to the person named on it or to someone with a right to it (parent, spouse, child, guardian, or an authorized representative). Bring the ID you entered here when you claim it.";
            //
            // right card - details of the document
            //
            this.lblRightTitle.AutoSize = true;
            this.lblRightTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.lblRightTitle.ForeColor = Color.FromArgb(23, 26, 36);
            this.lblRightTitle.Location = new Point(30, 18);
            this.lblRightTitle.Text = "Details of the Document";
            this.lblRightHint.AutoSize = true;
            this.lblRightHint.Font = new Font("Segoe UI", 10F);
            this.lblRightHint.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblRightHint.Location = new Point(30, 60);
            this.lblRightHint.Text = "Only * is required. Fill in what you know - it helps PSA find the record.";
            this._lblOwnerHead.AutoSize = true;
            this._lblOwnerHead.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this._lblOwnerHead.ForeColor = Color.FromArgb(23, 26, 36);
            this._lblOwnerHead.Location = new Point(30, 96);
            this._lblOwnerHead.Text = "Name on the birth certificate";
            this.lblOwnerFirst.AutoSize = true;
            this.lblOwnerFirst.Font = new Font("Segoe UI", 9.5F);
            this.lblOwnerFirst.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblOwnerFirst.Location = new Point(30, 124);
            this.lblOwnerFirst.Text = "First";
            this.lblOwnerFirstReq.AutoSize = true;
            this.lblOwnerFirstReq.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblOwnerFirstReq.ForeColor = Color.FromArgb(198, 50, 63);
            this.lblOwnerFirstReq.Location = new Point(30, 124);
            this.lblOwnerFirstReq.Text = "*";
            this.hostOwnerFirst.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostOwnerFirst.BorderWidth = 1.5F;
            this.hostOwnerFirst.Fill = Color.White;
            this.hostOwnerFirst.Location = new Point(30, 150);
            this.hostOwnerFirst.Radius = 8;
            this.hostOwnerFirst.Size = new Size(164, 46);
            this._txtOwnerFirst.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtOwnerFirst.BorderStyle = BorderStyle.None;
            this._txtOwnerFirst.Font = new Font("Segoe UI", 13F);
            this._txtOwnerFirst.Location = new Point(14, 12);
            this._txtOwnerFirst.Size = new Size(136, 25);
            this._txtOwnerFirst.Enter += new System.EventHandler(this.Field_Enter);
            this._txtOwnerFirst.Leave += new System.EventHandler(this.Field_Leave);
            this.lblOwnerMiddle.AutoSize = true;
            this.lblOwnerMiddle.Font = new Font("Segoe UI", 9.5F);
            this.lblOwnerMiddle.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblOwnerMiddle.Location = new Point(206, 124);
            this.lblOwnerMiddle.Text = "Middle";
            this.hostOwnerMiddle.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostOwnerMiddle.BorderWidth = 1.5F;
            this.hostOwnerMiddle.Fill = Color.White;
            this.hostOwnerMiddle.Location = new Point(206, 150);
            this.hostOwnerMiddle.Radius = 8;
            this.hostOwnerMiddle.Size = new Size(164, 46);
            this._txtOwnerMiddle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtOwnerMiddle.BorderStyle = BorderStyle.None;
            this._txtOwnerMiddle.Font = new Font("Segoe UI", 13F);
            this._txtOwnerMiddle.Location = new Point(14, 12);
            this._txtOwnerMiddle.Size = new Size(136, 25);
            this._txtOwnerMiddle.Enter += new System.EventHandler(this.Field_Enter);
            this._txtOwnerMiddle.Leave += new System.EventHandler(this.Field_Leave);
            this.lblOwnerLast.AutoSize = true;
            this.lblOwnerLast.Font = new Font("Segoe UI", 9.5F);
            this.lblOwnerLast.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblOwnerLast.Location = new Point(382, 124);
            this.lblOwnerLast.Text = "Last";
            this.lblOwnerLastReq.AutoSize = true;
            this.lblOwnerLastReq.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblOwnerLastReq.ForeColor = Color.FromArgb(198, 50, 63);
            this.lblOwnerLastReq.Location = new Point(382, 124);
            this.lblOwnerLastReq.Text = "*";
            this.hostOwnerLast.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostOwnerLast.BorderWidth = 1.5F;
            this.hostOwnerLast.Fill = Color.White;
            this.hostOwnerLast.Location = new Point(382, 150);
            this.hostOwnerLast.Radius = 8;
            this.hostOwnerLast.Size = new Size(164, 46);
            this._txtOwnerLast.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtOwnerLast.BorderStyle = BorderStyle.None;
            this._txtOwnerLast.Font = new Font("Segoe UI", 13F);
            this._txtOwnerLast.Location = new Point(14, 12);
            this._txtOwnerLast.Size = new Size(136, 25);
            this._txtOwnerLast.Enter += new System.EventHandler(this.Field_Enter);
            this._txtOwnerLast.Leave += new System.EventHandler(this.Field_Leave);
            this._lblSpouseHead.AutoSize = true;
            this._lblSpouseHead.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this._lblSpouseHead.ForeColor = Color.FromArgb(23, 26, 36);
            this._lblSpouseHead.Location = new Point(30, 212);
            this._lblSpouseHead.Text = "Wife";
            this.lblSpouseFirst.AutoSize = true;
            this.lblSpouseFirst.Font = new Font("Segoe UI", 9.5F);
            this.lblSpouseFirst.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblSpouseFirst.Location = new Point(30, 240);
            this.lblSpouseFirst.Text = "First";
            this.lblSpouseFirstReq.AutoSize = true;
            this.lblSpouseFirstReq.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblSpouseFirstReq.ForeColor = Color.FromArgb(198, 50, 63);
            this.lblSpouseFirstReq.Location = new Point(30, 240);
            this.lblSpouseFirstReq.Text = "*";
            this.hostSpouseFirst.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostSpouseFirst.BorderWidth = 1.5F;
            this.hostSpouseFirst.Fill = Color.White;
            this.hostSpouseFirst.Location = new Point(30, 266);
            this.hostSpouseFirst.Radius = 8;
            this.hostSpouseFirst.Size = new Size(164, 46);
            this._txtSpouseFirst.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtSpouseFirst.BorderStyle = BorderStyle.None;
            this._txtSpouseFirst.Font = new Font("Segoe UI", 13F);
            this._txtSpouseFirst.Location = new Point(14, 12);
            this._txtSpouseFirst.Size = new Size(136, 25);
            this._txtSpouseFirst.Enter += new System.EventHandler(this.Field_Enter);
            this._txtSpouseFirst.Leave += new System.EventHandler(this.Field_Leave);
            this.lblSpouseMiddle.AutoSize = true;
            this.lblSpouseMiddle.Font = new Font("Segoe UI", 9.5F);
            this.lblSpouseMiddle.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblSpouseMiddle.Location = new Point(206, 240);
            this.lblSpouseMiddle.Text = "Middle";
            this.hostSpouseMiddle.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostSpouseMiddle.BorderWidth = 1.5F;
            this.hostSpouseMiddle.Fill = Color.White;
            this.hostSpouseMiddle.Location = new Point(206, 266);
            this.hostSpouseMiddle.Radius = 8;
            this.hostSpouseMiddle.Size = new Size(164, 46);
            this._txtSpouseMiddle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtSpouseMiddle.BorderStyle = BorderStyle.None;
            this._txtSpouseMiddle.Font = new Font("Segoe UI", 13F);
            this._txtSpouseMiddle.Location = new Point(14, 12);
            this._txtSpouseMiddle.Size = new Size(136, 25);
            this._txtSpouseMiddle.Enter += new System.EventHandler(this.Field_Enter);
            this._txtSpouseMiddle.Leave += new System.EventHandler(this.Field_Leave);
            this.lblSpouseLast.AutoSize = true;
            this.lblSpouseLast.Font = new Font("Segoe UI", 9.5F);
            this.lblSpouseLast.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblSpouseLast.Location = new Point(382, 240);
            this.lblSpouseLast.Text = "Last";
            this.lblSpouseLastReq.AutoSize = true;
            this.lblSpouseLastReq.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblSpouseLastReq.ForeColor = Color.FromArgb(198, 50, 63);
            this.lblSpouseLastReq.Location = new Point(382, 240);
            this.lblSpouseLastReq.Text = "*";
            this.hostSpouseLast.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostSpouseLast.BorderWidth = 1.5F;
            this.hostSpouseLast.Fill = Color.White;
            this.hostSpouseLast.Location = new Point(382, 266);
            this.hostSpouseLast.Radius = 8;
            this.hostSpouseLast.Size = new Size(164, 46);
            this._txtSpouseLast.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtSpouseLast.BorderStyle = BorderStyle.None;
            this._txtSpouseLast.Font = new Font("Segoe UI", 13F);
            this._txtSpouseLast.Location = new Point(14, 12);
            this._txtSpouseLast.Size = new Size(136, 25);
            this._txtSpouseLast.Enter += new System.EventHandler(this.Field_Enter);
            this._txtSpouseLast.Leave += new System.EventHandler(this.Field_Leave);
            this._lblDateHead.AutoSize = true;
            this._lblDateHead.Font = new Font("Segoe UI", 9.5F);
            this._lblDateHead.ForeColor = Color.FromArgb(91, 100, 114);
            this._lblDateHead.Location = new Point(30, 330);
            this._lblDateHead.Text = "Date of birth (tick the box if known)";
            this.hostDate.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostDate.BorderWidth = 1.5F;
            this.hostDate.Fill = Color.White;
            this.hostDate.Location = new Point(30, 356);
            this.hostDate.Radius = 8;
            this.hostDate.Size = new Size(340, 46);
            this._dtpEvent.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._dtpEvent.CustomFormat = "dd MMMM yyyy";
            this._dtpEvent.Format = DateTimePickerFormat.Custom;
            this._dtpEvent.Font = new Font("Segoe UI", 12F);
            this._dtpEvent.Location = new Point(10, 9);
            this._dtpEvent.ShowCheckBox = true;
            this._dtpEvent.Checked = false;
            this._dtpEvent.Size = new Size(320, 27);
            this._dtpEvent.Enter += new System.EventHandler(this.Field_Enter);
            this._dtpEvent.Leave += new System.EventHandler(this.Field_Leave);
            this.lblCity.AutoSize = true;
            this.lblCity.Font = new Font("Segoe UI", 9.5F);
            this.lblCity.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblCity.Location = new Point(30, 416);
            this.lblCity.Text = "City / Municipality";
            this.hostCity.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostCity.BorderWidth = 1.5F;
            this.hostCity.Fill = Color.White;
            this.hostCity.Location = new Point(30, 442);
            this.hostCity.Radius = 8;
            this.hostCity.Size = new Size(252, 46);
            this._txtCity.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtCity.BorderStyle = BorderStyle.None;
            this._txtCity.Font = new Font("Segoe UI", 13F);
            this._txtCity.Location = new Point(14, 12);
            this._txtCity.Size = new Size(224, 25);
            this._txtCity.Enter += new System.EventHandler(this.Field_Enter);
            this._txtCity.Leave += new System.EventHandler(this.Field_Leave);
            this.lblProvince.AutoSize = true;
            this.lblProvince.Font = new Font("Segoe UI", 9.5F);
            this.lblProvince.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblProvince.Location = new Point(294, 416);
            this.lblProvince.Text = "Province";
            this.hostProvince.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostProvince.BorderWidth = 1.5F;
            this.hostProvince.Fill = Color.White;
            this.hostProvince.Location = new Point(294, 442);
            this.hostProvince.Radius = 8;
            this.hostProvince.Size = new Size(252, 46);
            this._txtProvince.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtProvince.BorderStyle = BorderStyle.None;
            this._txtProvince.Font = new Font("Segoe UI", 13F);
            this._txtProvince.Location = new Point(14, 12);
            this._txtProvince.Size = new Size(224, 25);
            this._txtProvince.Enter += new System.EventHandler(this.Field_Enter);
            this._txtProvince.Leave += new System.EventHandler(this.Field_Leave);
            this.lblFather.AutoSize = true;
            this.lblFather.Font = new Font("Segoe UI", 9.5F);
            this.lblFather.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblFather.Location = new Point(30, 502);
            this.lblFather.Text = "Father's full name";
            this.hostFather.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostFather.BorderWidth = 1.5F;
            this.hostFather.Fill = Color.White;
            this.hostFather.Location = new Point(30, 528);
            this.hostFather.Radius = 8;
            this.hostFather.Size = new Size(516, 46);
            this._txtFather.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtFather.BorderStyle = BorderStyle.None;
            this._txtFather.Font = new Font("Segoe UI", 13F);
            this._txtFather.Location = new Point(14, 12);
            this._txtFather.Size = new Size(488, 25);
            this._txtFather.Enter += new System.EventHandler(this.Field_Enter);
            this._txtFather.Leave += new System.EventHandler(this.Field_Leave);
            this.lblMother.AutoSize = true;
            this.lblMother.Font = new Font("Segoe UI", 9.5F);
            this.lblMother.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblMother.Location = new Point(30, 588);
            this.lblMother.Text = "Mother's full maiden name";
            this.hostMother.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostMother.BorderWidth = 1.5F;
            this.hostMother.Fill = Color.White;
            this.hostMother.Location = new Point(30, 614);
            this.hostMother.Radius = 8;
            this.hostMother.Size = new Size(516, 46);
            this._txtMother.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtMother.BorderStyle = BorderStyle.None;
            this._txtMother.Font = new Font("Segoe UI", 13F);
            this._txtMother.Location = new Point(14, 12);
            this._txtMother.Size = new Size(488, 25);
            this._txtMother.Enter += new System.EventHandler(this.Field_Enter);
            this._txtMother.Leave += new System.EventHandler(this.Field_Leave);
            this.header.Controls.Add(this.lblTitle);
            this.header.Controls.Add(this._stepInd);
            this.footer.Controls.Add(this.footerDivider);
            this._host.Controls.Add(this.panelStep);
            this.panelStep.Controls.Add(this._box);
            this._box.Controls.Add(this.leftCard);
            this._box.Controls.Add(this.rightCard);
            this.leftCard.Controls.Add(this.lblLeftTitle);
            this.leftCard.Controls.Add(this.lblLeftHint);
            this.leftCard.Controls.Add(this.lblDocType);
            this.leftCard.Controls.Add(this.lblDocTypeReq);
            this.leftCard.Controls.Add(this._pillBirth);
            this.leftCard.Controls.Add(this._pillMarriage);
            this.leftCard.Controls.Add(this._pillDeath);
            this.leftCard.Controls.Add(this.lblCopies);
            this.leftCard.Controls.Add(this.hostCopies);
            this.leftCard.Controls.Add(this.lblPurpose);
            this.leftCard.Controls.Add(this.hostPurpose);
            this.leftCard.Controls.Add(this.lblRelationship);
            this.leftCard.Controls.Add(this.hostRelationship);
            this.leftCard.Controls.Add(this.lblIdType);
            this.leftCard.Controls.Add(this.lblIdTypeReq);
            this.leftCard.Controls.Add(this.hostIdType);
            this.leftCard.Controls.Add(this.lblIdNo);
            this.leftCard.Controls.Add(this.lblIdNoReq);
            this.leftCard.Controls.Add(this.hostIdNo);
            this.leftCard.Controls.Add(this.lblPsaNote);
            this.hostCopies.Controls.Add(this._cboCopies);
            this.hostPurpose.Controls.Add(this._cboPurpose);
            this.hostRelationship.Controls.Add(this._cboRelationship);
            this.hostIdType.Controls.Add(this._cboIdType);
            this.hostIdNo.Controls.Add(this._txtIdNo);
            this.rightCard.Controls.Add(this.lblRightTitle);
            this.rightCard.Controls.Add(this.lblRightHint);
            this.rightCard.Controls.Add(this._lblOwnerHead);
            this.rightCard.Controls.Add(this.lblOwnerFirst);
            this.rightCard.Controls.Add(this.lblOwnerFirstReq);
            this.rightCard.Controls.Add(this.hostOwnerFirst);
            this.rightCard.Controls.Add(this.lblOwnerMiddle);
            this.rightCard.Controls.Add(this.hostOwnerMiddle);
            this.rightCard.Controls.Add(this.lblOwnerLast);
            this.rightCard.Controls.Add(this.lblOwnerLastReq);
            this.rightCard.Controls.Add(this.hostOwnerLast);
            this.rightCard.Controls.Add(this._lblSpouseHead);
            this.rightCard.Controls.Add(this.lblSpouseFirst);
            this.rightCard.Controls.Add(this.lblSpouseFirstReq);
            this.rightCard.Controls.Add(this.hostSpouseFirst);
            this.rightCard.Controls.Add(this.lblSpouseMiddle);
            this.rightCard.Controls.Add(this.hostSpouseMiddle);
            this.rightCard.Controls.Add(this.lblSpouseLast);
            this.rightCard.Controls.Add(this.lblSpouseLastReq);
            this.rightCard.Controls.Add(this.hostSpouseLast);
            this.rightCard.Controls.Add(this._lblDateHead);
            this.rightCard.Controls.Add(this.hostDate);
            this.rightCard.Controls.Add(this.lblCity);
            this.rightCard.Controls.Add(this.hostCity);
            this.rightCard.Controls.Add(this.lblProvince);
            this.rightCard.Controls.Add(this.hostProvince);
            this.rightCard.Controls.Add(this.lblFather);
            this.rightCard.Controls.Add(this.hostFather);
            this.rightCard.Controls.Add(this.lblMother);
            this.rightCard.Controls.Add(this.hostMother);
            this.hostOwnerFirst.Controls.Add(this._txtOwnerFirst);
            this.hostOwnerMiddle.Controls.Add(this._txtOwnerMiddle);
            this.hostOwnerLast.Controls.Add(this._txtOwnerLast);
            this.hostSpouseFirst.Controls.Add(this._txtSpouseFirst);
            this.hostSpouseMiddle.Controls.Add(this._txtSpouseMiddle);
            this.hostSpouseLast.Controls.Add(this._txtSpouseLast);
            this.hostDate.Controls.Add(this._dtpEvent);
            this.hostCity.Controls.Add(this._txtCity);
            this.hostProvince.Controls.Add(this._txtProvince);
            this.hostFather.Controls.Add(this._txtFather);
            this.hostMother.Controls.Add(this._txtMother);
            //
            // BreqsDetailsForm
            //
            this.AutoScaleMode = AutoScaleMode.None;
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.ClientSize = new Size(1264, 681);
            this.Font = new Font("Segoe UI", 10F);
            this.Text = "CROMS - Client Kiosk";
            this.WindowState = FormWindowState.Maximized;
            this.Controls.Add(this._host);
            this.Controls.Add(this.footer);
            this.Controls.Add(this._btnBack);
            this.Controls.Add(this._btnNext);
            this.Controls.Add(this.header);
            this.hostMother.ResumeLayout(false);
            this.hostFather.ResumeLayout(false);
            this.hostProvince.ResumeLayout(false);
            this.hostCity.ResumeLayout(false);
            this.hostDate.ResumeLayout(false);
            this.hostSpouseLast.ResumeLayout(false);
            this.hostSpouseMiddle.ResumeLayout(false);
            this.hostSpouseFirst.ResumeLayout(false);
            this.hostOwnerLast.ResumeLayout(false);
            this.hostOwnerMiddle.ResumeLayout(false);
            this.hostOwnerFirst.ResumeLayout(false);
            this.hostIdNo.ResumeLayout(false);
            this.hostIdType.ResumeLayout(false);
            this.hostRelationship.ResumeLayout(false);
            this.hostPurpose.ResumeLayout(false);
            this.hostCopies.ResumeLayout(false);
            this.rightCard.ResumeLayout(false);
            this.leftCard.ResumeLayout(false);
            this._box.ResumeLayout(false);
            this.panelStep.ResumeLayout(false);
            this._host.ResumeLayout(false);
            this.footerDivider.ResumeLayout(false);
            this.footer.ResumeLayout(false);
            this.header.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
