using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Step 2 of 2 — personal details, priority lane, live webcam photo, and (for a Release &amp;
    /// Claim pickup) the claimapp QR. Designer-generated visual tree (behaviour in
    /// DetailsPhotoForm.cs).
    /// </summary>
    public partial class DetailsPhotoForm
    {
        private System.ComponentModel.IContainer components = null;

        private Panel header;
        private Label lblTitle;
        private StepIndicator _stepInd;
        private Panel footer;
        private Panel footerDivider;
        private Button _btnBack;
        private Button _btnPrint;
        private Panel _host;
        private Panel panelStep2;
        private Panel _detailsBox;

        private RoundPanel leftCard;
        private Label lblPersonalInfo;
        private Label lblFirst, lblMiddle, lblLast, lblContact, lblPriority;
        private Label lblFirstReq, lblLastReq, lblPriorityHelp;
        private RoundPanel hostFirst, hostMiddle, hostLast, hostContact;
        private TextBox _txtFirst, _txtMiddle, _txtLast, _txtContact;
        private PillToggle _priSenior, _priPwd, _priPregnant;

        // Marriage Application / Marriage Registration only: a second person (spouse) beside
        // the fields above (which become "husband" in that mode). Hidden/unused otherwise.
        private Label _lblHeadA, _lblHeadB;
        private Label lblFirst2, lblMiddle2, lblLast2;
        private Label lblFirst2Req, lblLast2Req;
        private RoundPanel hostFirst2, hostMiddle2, hostLast2;
        private TextBox _txtFirst2, _txtMiddle2, _txtLast2;
        private Label lblValidId;
        private Label lblIdType;
        private RoundPanel hostIdType;
        private ComboBox _cboIdType;
        private Label lblIdNo;
        private RoundPanel hostIdNo;
        private TextBox _txtIdNo;
        private Panel _claimPanel;
        private Label lblClaimField, lblClaimHelp;
        private RoundPanel hostClaim;
        private TextBox _txtClaimTicket;

        private RoundPanel rightCard;
        private Label _rightTitle, _rightHint;
        private RoundPanel _camFrame;
        private PictureBox _picCam;
        private Label _lblLive;
        private Label _lblCamState, _lblCamStatus;
        private Button _btnCapture;

        // Marriage only: which of the two people the (single) camera is about to photograph.
        private Button _btnPersonA, _btnPersonB;

        private Panel _offlineOverlay;
        private Label lblOffline;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
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
            this._btnPrint = new Button();
            this._host = new Panel();
            this.panelStep2 = new Panel();
            this._detailsBox = new Panel();
            this.leftCard = new RoundPanel();
            this.lblPersonalInfo = new Label();
            this.lblFirst = new Label(); this.hostFirst = new RoundPanel(); this._txtFirst = new TextBox();
            this.lblMiddle = new Label(); this.hostMiddle = new RoundPanel(); this._txtMiddle = new TextBox();
            this.lblLast = new Label(); this.hostLast = new RoundPanel(); this._txtLast = new TextBox();
            this.lblContact = new Label(); this.hostContact = new RoundPanel(); this._txtContact = new TextBox();
            this.lblPriority = new Label();
            this.lblFirstReq = new Label();
            this.lblLastReq = new Label();
            this.lblPriorityHelp = new Label();
            this._priSenior = new PillToggle();
            this._priPwd = new PillToggle();
            this._priPregnant = new PillToggle();
            this._lblHeadA = new Label();
            this._lblHeadB = new Label();
            this.lblFirst2 = new Label(); this.hostFirst2 = new RoundPanel(); this._txtFirst2 = new TextBox();
            this.lblMiddle2 = new Label(); this.hostMiddle2 = new RoundPanel(); this._txtMiddle2 = new TextBox();
            this.lblLast2 = new Label(); this.hostLast2 = new RoundPanel(); this._txtLast2 = new TextBox();
            this.lblFirst2Req = new Label();
            this.lblLast2Req = new Label();
            this.lblValidId = new Label();
            this.lblIdType = new Label();
            this.hostIdType = new RoundPanel();
            this._cboIdType = new ComboBox();
            this.lblIdNo = new Label();
            this.hostIdNo = new RoundPanel();
            this._txtIdNo = new TextBox();
            this._claimPanel = new Panel();
            this.lblClaimField = new Label();
            this.hostClaim = new RoundPanel();
            this._txtClaimTicket = new TextBox();
            this.lblClaimHelp = new Label();
            this.rightCard = new RoundPanel();
            this._rightTitle = new Label();
            this._rightHint = new Label();
            this._camFrame = new RoundPanel();
            this._picCam = new PictureBox();
            this._lblLive = new Label();
            this._lblCamState = new Label();
            this._btnCapture = new Button();
            this._btnPersonA = new Button();
            this._btnPersonB = new Button();
            this._lblCamStatus = new Label();
            this._offlineOverlay = new Panel();
            this.lblOffline = new Label();
            this.SuspendLayout();
            //
            // header
            //
            this.header.BackColor = Color.FromArgb(244, 246, 249);
            this.header.Dock = DockStyle.Top;
            this.header.Height = 128;
            this.header.Controls.Add(this.lblTitle);
            this.header.Controls.Add(this._stepInd);
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI", 30F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.FromArgb(23, 26, 36);
            this.lblTitle.Location = new Point(40, 18);
            this.lblTitle.Text = "Request a Service";
            //
            // _stepInd
            //
            this._stepInd.Location = new Point(40, 74);
            this._stepInd.Size = new Size(560, 52);
            this._stepInd.Steps = new string[] { "Select Services", "Personal Info & Photo" };
            //
            // footer
            //
            this.footer.BackColor = Color.White;
            this.footer.Dock = DockStyle.Bottom;
            this.footer.Height = 92;
            this.footer.Controls.Add(this.footerDivider);
            //
            // footerDivider
            //
            this.footerDivider.BackColor = Color.FromArgb(225, 229, 236);
            this.footerDivider.Dock = DockStyle.Top;
            this.footerDivider.Height = 1;
            //
            // _btnBack  (child of the FORM, anchored bottom-left)
            //
            this._btnBack.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this._btnBack.BackColor = Color.White;
            this._btnBack.Cursor = Cursors.Hand;
            this._btnBack.FlatAppearance.BorderColor = Color.FromArgb(225, 229, 236);
            this._btnBack.FlatAppearance.MouseOverBackColor = Color.FromArgb(238, 240, 244);
            this._btnBack.FlatStyle = FlatStyle.Flat;
            this._btnBack.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            this._btnBack.ForeColor = Color.FromArgb(23, 26, 36);
            this._btnBack.Location = new Point(40, 604);
            this._btnBack.Size = new Size(210, 70);
            this._btnBack.Text = "Back";
            this._btnBack.UseVisualStyleBackColor = false;
            this._btnBack.Click += new System.EventHandler(this.BtnBack_Click);
            //
            // _btnPrint  (child of the FORM, anchored bottom-right)
            //
            this._btnPrint.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this._btnPrint.BackColor = Color.FromArgb(46, 148, 87);
            this._btnPrint.Cursor = Cursors.Hand;
            this._btnPrint.FlatAppearance.BorderSize = 0;
            this._btnPrint.FlatAppearance.MouseOverBackColor = Color.FromArgb(36, 120, 70);
            this._btnPrint.FlatStyle = FlatStyle.Flat;
            this._btnPrint.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            this._btnPrint.ForeColor = Color.White;
            this._btnPrint.Location = new Point(924, 603);
            this._btnPrint.Size = new Size(320, 70);
            this._btnPrint.Text = "Get Queue Number";
            this._btnPrint.UseVisualStyleBackColor = false;
            this._btnPrint.Click += new System.EventHandler(this.BtnPrint_Click);
            //
            // _host
            //
            this._host.BackColor = Color.FromArgb(244, 246, 249);
            this._host.Dock = DockStyle.Fill;
            this._host.Controls.Add(this.panelStep2);
            //
            // panelStep2
            //
            this.panelStep2.AutoScroll = true;
            this.panelStep2.BackColor = Color.FromArgb(244, 246, 249);
            this.panelStep2.Dock = DockStyle.Fill;
            this.panelStep2.Controls.Add(this._detailsBox);
            this.panelStep2.Resize += new System.EventHandler(this.PanelStep2_Resize);
            //
            // _detailsBox
            //
            this._detailsBox.BackColor = Color.FromArgb(244, 246, 249);
            this._detailsBox.Location = new Point(20, 16);
            this._detailsBox.Size = new Size(1200, 988);
            this._detailsBox.Controls.Add(this.leftCard);
            this._detailsBox.Controls.Add(this.rightCard);
            //
            // leftCard
            //
            this.leftCard.Fill = Color.White;
            this.leftCard.Location = new Point(6, 6);
            this.leftCard.Radius = 14;
            this.leftCard.Shadow = 8;
            this.leftCard.Size = new Size(600, 976);
            this.leftCard.Controls.Add(this.lblPersonalInfo);
            this.leftCard.Controls.Add(this.lblFirst);
            this.leftCard.Controls.Add(this.lblFirstReq);
            this.leftCard.Controls.Add(this.hostFirst);
            this.leftCard.Controls.Add(this.lblMiddle);
            this.leftCard.Controls.Add(this.hostMiddle);
            this.leftCard.Controls.Add(this.lblLast);
            this.leftCard.Controls.Add(this.lblLastReq);
            this.leftCard.Controls.Add(this.hostLast);
            this.leftCard.Controls.Add(this.lblContact);
            this.leftCard.Controls.Add(this.hostContact);
            this.leftCard.Controls.Add(this.lblPriority);
            this.leftCard.Controls.Add(this.lblPriorityHelp);
            this.leftCard.Controls.Add(this._priSenior);
            this.leftCard.Controls.Add(this._priPwd);
            this.leftCard.Controls.Add(this._priPregnant);
            this.leftCard.Controls.Add(this._lblHeadA);
            this.leftCard.Controls.Add(this._lblHeadB);
            this.leftCard.Controls.Add(this.lblFirst2);
            this.leftCard.Controls.Add(this.lblFirst2Req);
            this.leftCard.Controls.Add(this.hostFirst2);
            this.leftCard.Controls.Add(this.lblMiddle2);
            this.leftCard.Controls.Add(this.hostMiddle2);
            this.leftCard.Controls.Add(this.lblLast2);
            this.leftCard.Controls.Add(this.lblLast2Req);
            this.leftCard.Controls.Add(this.hostLast2);
            this.leftCard.Controls.Add(this.lblValidId);
            this.leftCard.Controls.Add(this.lblIdType);
            this.leftCard.Controls.Add(this.hostIdType);
            this.leftCard.Controls.Add(this.lblIdNo);
            this.leftCard.Controls.Add(this.hostIdNo);
            this.leftCard.Controls.Add(this._claimPanel);
            //
            // lblPersonalInfo
            //
            this.lblPersonalInfo.AutoSize = true;
            this.lblPersonalInfo.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.lblPersonalInfo.ForeColor = Color.FromArgb(23, 26, 36);
            this.lblPersonalInfo.Location = new Point(30, 22);
            this.lblPersonalInfo.Text = "Personal Information";
            //
            // lblFirst / hostFirst / _txtFirst
            //
            this.lblFirst.AutoSize = true;
            this.lblFirst.Font = new Font("Segoe UI", 9.5F);
            this.lblFirst.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblFirst.Location = new Point(30, 66);
            this.lblFirst.Text = "First Name";
            this.lblFirstReq.AutoSize = true;
            this.lblFirstReq.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblFirstReq.ForeColor = Color.FromArgb(198, 50, 63);
            this.lblFirstReq.Location = new Point(96, 66);
            this.lblFirstReq.Text = "*";
            this.hostFirst.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostFirst.BorderWidth = 1.5F;
            this.hostFirst.Fill = Color.White;
            this.hostFirst.Location = new Point(30, 92);
            this.hostFirst.Radius = 8;
            this.hostFirst.Size = new Size(536, 46);
            this.hostFirst.Controls.Add(this._txtFirst);
            this._txtFirst.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtFirst.BorderStyle = BorderStyle.None;
            this._txtFirst.Font = new Font("Segoe UI", 13F);
            this._txtFirst.Location = new Point(14, 12);
            this._txtFirst.Size = new Size(508, 25);
            this._txtFirst.Enter += new System.EventHandler(this.Field_Enter);
            this._txtFirst.Leave += new System.EventHandler(this.Field_Leave);
            //
            // lblMiddle / hostMiddle / _txtMiddle
            //
            this.lblMiddle.AutoSize = true;
            this.lblMiddle.Font = new Font("Segoe UI", 9.5F);
            this.lblMiddle.ForeColor = Color.FromArgb(137, 145, 163);
            this.lblMiddle.Location = new Point(30, 152);
            this.lblMiddle.Text = "Middle Name   (optional)";
            this.hostMiddle.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostMiddle.BorderWidth = 1.5F;
            this.hostMiddle.Fill = Color.White;
            this.hostMiddle.Location = new Point(30, 178);
            this.hostMiddle.Radius = 8;
            this.hostMiddle.Size = new Size(536, 46);
            this.hostMiddle.Controls.Add(this._txtMiddle);
            this._txtMiddle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtMiddle.BorderStyle = BorderStyle.None;
            this._txtMiddle.Font = new Font("Segoe UI", 13F);
            this._txtMiddle.Location = new Point(14, 12);
            this._txtMiddle.Size = new Size(508, 25);
            this._txtMiddle.Enter += new System.EventHandler(this.Field_Enter);
            this._txtMiddle.Leave += new System.EventHandler(this.Field_Leave);
            //
            // lblLast / hostLast / _txtLast
            //
            this.lblLast.AutoSize = true;
            this.lblLast.Font = new Font("Segoe UI", 9.5F);
            this.lblLast.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblLast.Location = new Point(30, 238);
            this.lblLast.Text = "Last Name";
            this.lblLastReq.AutoSize = true;
            this.lblLastReq.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblLastReq.ForeColor = Color.FromArgb(198, 50, 63);
            this.lblLastReq.Location = new Point(94, 238);
            this.lblLastReq.Text = "*";
            this.hostLast.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostLast.BorderWidth = 1.5F;
            this.hostLast.Fill = Color.White;
            this.hostLast.Location = new Point(30, 264);
            this.hostLast.Radius = 8;
            this.hostLast.Size = new Size(536, 46);
            this.hostLast.Controls.Add(this._txtLast);
            this._txtLast.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtLast.BorderStyle = BorderStyle.None;
            this._txtLast.Font = new Font("Segoe UI", 13F);
            this._txtLast.Location = new Point(14, 12);
            this._txtLast.Size = new Size(508, 25);
            this._txtLast.Enter += new System.EventHandler(this.Field_Enter);
            this._txtLast.Leave += new System.EventHandler(this.Field_Leave);
            //
            // _lblHeadA / _lblHeadB (Husband / Wife column headers — marriage services only)
            //
            this._lblHeadA.AutoSize = true;
            this._lblHeadA.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            this._lblHeadA.ForeColor = Color.FromArgb(29, 78, 216);
            this._lblHeadA.Location = new Point(30, 64);
            this._lblHeadA.Text = "HUSBAND";
            this._lblHeadA.Visible = false;
            this._lblHeadB.AutoSize = true;
            this._lblHeadB.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            this._lblHeadB.ForeColor = Color.FromArgb(29, 78, 216);
            this._lblHeadB.Location = new Point(306, 64);
            this._lblHeadB.Text = "WIFE";
            this._lblHeadB.Visible = false;
            //
            // lblFirst2 / hostFirst2 / _txtFirst2 (wife — marriage services only)
            //
            this.lblFirst2.AutoSize = true;
            this.lblFirst2.Font = new Font("Segoe UI", 9.5F);
            this.lblFirst2.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblFirst2.Location = new Point(306, 90);
            this.lblFirst2.Text = "First Name";
            this.lblFirst2.Visible = false;
            this.lblFirst2Req.AutoSize = true;
            this.lblFirst2Req.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblFirst2Req.ForeColor = Color.FromArgb(198, 50, 63);
            this.lblFirst2Req.Location = new Point(372, 90);
            this.lblFirst2Req.Text = "*";
            this.lblFirst2Req.Visible = false;
            this.hostFirst2.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostFirst2.BorderWidth = 1.5F;
            this.hostFirst2.Fill = Color.White;
            this.hostFirst2.Location = new Point(306, 108);
            this.hostFirst2.Radius = 8;
            this.hostFirst2.Size = new Size(260, 46);
            this.hostFirst2.Visible = false;
            this.hostFirst2.Controls.Add(this._txtFirst2);
            this._txtFirst2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtFirst2.BorderStyle = BorderStyle.None;
            this._txtFirst2.Font = new Font("Segoe UI", 13F);
            this._txtFirst2.Location = new Point(14, 12);
            this._txtFirst2.Size = new Size(232, 25);
            this._txtFirst2.Enter += new System.EventHandler(this.Field_Enter);
            this._txtFirst2.Leave += new System.EventHandler(this.Field_Leave);
            //
            // lblMiddle2 / hostMiddle2 / _txtMiddle2 (wife)
            //
            this.lblMiddle2.AutoSize = true;
            this.lblMiddle2.Font = new Font("Segoe UI", 9.5F);
            this.lblMiddle2.ForeColor = Color.FromArgb(137, 145, 163);
            this.lblMiddle2.Location = new Point(306, 164);
            this.lblMiddle2.Text = "Middle Name (optional)";
            this.lblMiddle2.Visible = false;
            this.hostMiddle2.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostMiddle2.BorderWidth = 1.5F;
            this.hostMiddle2.Fill = Color.White;
            this.hostMiddle2.Location = new Point(306, 182);
            this.hostMiddle2.Radius = 8;
            this.hostMiddle2.Size = new Size(260, 46);
            this.hostMiddle2.Visible = false;
            this.hostMiddle2.Controls.Add(this._txtMiddle2);
            this._txtMiddle2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtMiddle2.BorderStyle = BorderStyle.None;
            this._txtMiddle2.Font = new Font("Segoe UI", 13F);
            this._txtMiddle2.Location = new Point(14, 12);
            this._txtMiddle2.Size = new Size(232, 25);
            this._txtMiddle2.Enter += new System.EventHandler(this.Field_Enter);
            this._txtMiddle2.Leave += new System.EventHandler(this.Field_Leave);
            //
            // lblLast2 / hostLast2 / _txtLast2 (wife)
            //
            this.lblLast2.AutoSize = true;
            this.lblLast2.Font = new Font("Segoe UI", 9.5F);
            this.lblLast2.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblLast2.Location = new Point(306, 238);
            this.lblLast2.Text = "Last Name";
            this.lblLast2.Visible = false;
            this.lblLast2Req.AutoSize = true;
            this.lblLast2Req.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.lblLast2Req.ForeColor = Color.FromArgb(198, 50, 63);
            this.lblLast2Req.Location = new Point(370, 238);
            this.lblLast2Req.Text = "*";
            this.lblLast2Req.Visible = false;
            this.hostLast2.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostLast2.BorderWidth = 1.5F;
            this.hostLast2.Fill = Color.White;
            this.hostLast2.Location = new Point(306, 256);
            this.hostLast2.Radius = 8;
            this.hostLast2.Size = new Size(260, 46);
            this.hostLast2.Visible = false;
            this.hostLast2.Controls.Add(this._txtLast2);
            this._txtLast2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtLast2.BorderStyle = BorderStyle.None;
            this._txtLast2.Font = new Font("Segoe UI", 13F);
            this._txtLast2.Location = new Point(14, 12);
            this._txtLast2.Size = new Size(232, 25);
            this._txtLast2.Enter += new System.EventHandler(this.Field_Enter);
            this._txtLast2.Leave += new System.EventHandler(this.Field_Leave);
            //
            // lblContact / hostContact / _txtContact
            //
            this.lblContact.AutoSize = true;
            this.lblContact.Font = new Font("Segoe UI", 9.5F);
            this.lblContact.ForeColor = Color.FromArgb(137, 145, 163);
            this.lblContact.Location = new Point(30, 324);
            this.lblContact.Text = "Contact Number   (optional)";
            this.hostContact.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostContact.BorderWidth = 1.5F;
            this.hostContact.Fill = Color.White;
            this.hostContact.Location = new Point(30, 350);
            this.hostContact.Radius = 8;
            this.hostContact.Size = new Size(536, 46);
            this.hostContact.Controls.Add(this._txtContact);
            this._txtContact.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtContact.BorderStyle = BorderStyle.None;
            this._txtContact.Font = new Font("Segoe UI", 13F);
            this._txtContact.Location = new Point(14, 12);
            this._txtContact.Size = new Size(508, 25);
            this._txtContact.Enter += new System.EventHandler(this.Field_Enter);
            this._txtContact.Leave += new System.EventHandler(this.Field_Leave);
            //
            // lblPriority
            //
            this.lblPriority.AutoSize = true;
            this.lblPriority.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.lblPriority.ForeColor = Color.FromArgb(23, 26, 36);
            this.lblPriority.Location = new Point(30, 406);
            this.lblPriority.Text = "Priority Lane";
            this.lblPriorityHelp.AutoSize = true;
            this.lblPriorityHelp.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            this.lblPriorityHelp.ForeColor = Color.FromArgb(137, 145, 163);
            this.lblPriorityHelp.Location = new Point(148, 411);
            this.lblPriorityHelp.Text = "Select only if applicable.";
            //
            // priority pills
            //
            this._priSenior.GlyphCode = KioskCore.IconUser;   // user
            this._priSenior.LabelText = "Senior Citizen";
            this._priSenior.Location = new Point(30, 436);
            this._priSenior.Size = new Size(170, 112);
            this._priPwd.GlyphCode = KioskCore.IconWheelchair;   // wheelchair
            this._priPwd.LabelText = "PWD";
            this._priPwd.Location = new Point(213, 436);
            this._priPwd.Size = new Size(170, 112);
            this._priPregnant.GlyphCode = KioskCore.IconBaby;   // baby
            this._priPregnant.LabelText = "Pregnant";
            this._priPregnant.Location = new Point(396, 436);
            this._priPregnant.Size = new Size(170, 112);
            //
            // lblValidId / lblIdType / hostIdType / _cboIdType
            //
            this.lblValidId.AutoSize = true;
            this.lblValidId.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.lblValidId.ForeColor = Color.FromArgb(23, 26, 36);
            this.lblValidId.Location = new Point(30, 566);
            this.lblValidId.Text = "Valid ID  (optional)";
            this.lblIdType.AutoSize = true;
            this.lblIdType.Font = new Font("Segoe UI", 9.5F);
            this.lblIdType.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblIdType.Location = new Point(30, 598);
            this.lblIdType.Text = "ID Type";
            this.hostIdType.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostIdType.BorderWidth = 1.5F;
            this.hostIdType.Fill = Color.White;
            this.hostIdType.Location = new Point(30, 622);
            this.hostIdType.Radius = 8;
            this.hostIdType.Size = new Size(536, 46);
            this.hostIdType.Controls.Add(this._cboIdType);
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
            //
            // lblIdNo / hostIdNo / _txtIdNo — typed ID number, alongside (not instead of) the
            // QR upload on the right card. Some offices still want the number on file even
            // when a photo was also uploaded.
            //
            this.lblIdNo.AutoSize = true;
            this.lblIdNo.Font = new Font("Segoe UI", 9.5F);
            this.lblIdNo.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblIdNo.Location = new Point(30, 674);
            this.lblIdNo.Text = "ID Number";
            this.hostIdNo.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostIdNo.BorderWidth = 1.5F;
            this.hostIdNo.Fill = Color.White;
            this.hostIdNo.Location = new Point(30, 698);
            this.hostIdNo.Radius = 8;
            this.hostIdNo.Size = new Size(536, 46);
            this.hostIdNo.Controls.Add(this._txtIdNo);
            this._txtIdNo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtIdNo.BorderStyle = BorderStyle.None;
            this._txtIdNo.Font = new Font("Segoe UI", 13F);
            this._txtIdNo.Location = new Point(14, 12);
            this._txtIdNo.Size = new Size(508, 25);
            this._txtIdNo.Enter += new System.EventHandler(this.Field_Enter);
            this._txtIdNo.Leave += new System.EventHandler(this.Field_Leave);
            //
            // _claimPanel
            //
            this._claimPanel.BackColor = Color.White;
            this._claimPanel.Location = new Point(30, 812);
            this._claimPanel.Size = new Size(536, 110);
            this._claimPanel.Visible = false;
            this._claimPanel.Controls.Add(this.lblClaimField);
            this._claimPanel.Controls.Add(this.hostClaim);
            this._claimPanel.Controls.Add(this.lblClaimHelp);
            this.lblClaimField.AutoSize = true;
            this.lblClaimField.Font = new Font("Segoe UI", 9.5F);
            this.lblClaimField.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblClaimField.Location = new Point(0, 0);
            this.lblClaimField.Text = "Previous Queue Ticket Number";
            this.hostClaim.BorderColor = Color.FromArgb(225, 229, 236);
            this.hostClaim.BorderWidth = 1.5F;
            this.hostClaim.Fill = Color.White;
            this.hostClaim.Location = new Point(0, 26);
            this.hostClaim.Radius = 8;
            this.hostClaim.Size = new Size(536, 46);
            this.hostClaim.Controls.Add(this._txtClaimTicket);
            this._txtClaimTicket.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._txtClaimTicket.BorderStyle = BorderStyle.None;
            this._txtClaimTicket.Font = new Font("Segoe UI", 13F);
            this._txtClaimTicket.Location = new Point(14, 12);
            this._txtClaimTicket.Size = new Size(508, 25);
            this._txtClaimTicket.Enter += new System.EventHandler(this.Field_Enter);
            this._txtClaimTicket.Leave += new System.EventHandler(this.Field_Leave);
            this.lblClaimHelp.AutoSize = true;
            this.lblClaimHelp.Font = new Font("Segoe UI", 8.5F);
            this.lblClaimHelp.ForeColor = Color.FromArgb(91, 100, 114);
            this.lblClaimHelp.Location = new Point(2, 74);
            this.lblClaimHelp.Text = "Leave blank if this is your first request.";
            //
            // rightCard
            //
            this.rightCard.Fill = Color.White;
            this.rightCard.Location = new Point(624, 6);
            this.rightCard.Radius = 14;
            this.rightCard.Shadow = 8;
            this.rightCard.Size = new Size(570, 976);
            this.rightCard.Controls.Add(this._rightTitle);
            this.rightCard.Controls.Add(this._rightHint);
            this.rightCard.Controls.Add(this._camFrame);
            this.rightCard.Controls.Add(this._lblCamState);
            this.rightCard.Controls.Add(this._btnPersonA);
            this.rightCard.Controls.Add(this._btnPersonB);
            this.rightCard.Controls.Add(this._btnCapture);
            this.rightCard.Controls.Add(this._lblCamStatus);
            //
            // _rightTitle
            //
            this._rightTitle.AutoSize = true;
            this._rightTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this._rightTitle.ForeColor = Color.FromArgb(23, 26, 36);
            this._rightTitle.Location = new Point(26, 22);
            this._rightTitle.Text = "Your Photo";
            //
            // _rightHint
            //
            this._rightHint.AutoSize = true;
            this._rightHint.Font = new Font("Segoe UI", 10.5F);
            this._rightHint.ForeColor = Color.FromArgb(91, 100, 114);
            this._rightHint.Location = new Point(26, 60);
            this._rightHint.Text = "Look at the camera, then tap Capture Photo.";
            //
            // _camFrame / _picCam / _lblLive
            //
            this._camFrame.Fill = Color.FromArgb(17, 24, 39);
            this._camFrame.Location = new Point(26, 92);
            this._camFrame.Radius = 12;
            this._camFrame.Size = new Size(518, 380);
            this._camFrame.Controls.Add(this._picCam);
            this._picCam.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this._picCam.BackColor = Color.FromArgb(17, 24, 39);
            this._picCam.Location = new Point(8, 8);
            this._picCam.Size = new Size(502, 364);
            this._picCam.SizeMode = PictureBoxSizeMode.Zoom;
            this._picCam.Controls.Add(this._lblLive);
            this._picCam.Paint += new PaintEventHandler(this.DrawCameraGuide);
            this._lblLive.AutoSize = true;
            this._lblLive.BackColor = Color.FromArgb(198, 50, 63);
            this._lblLive.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this._lblLive.ForeColor = Color.White;
            this._lblLive.Location = new Point(12, 12);
            this._lblLive.Padding = new Padding(6, 2, 6, 2);
            this._lblLive.Text = "● LIVE";
            this._lblLive.Visible = false;
            //
            // _btnPersonA / _btnPersonB (marriage services only — which person the camera is about to photograph)
            //
            this._btnPersonA.Location = new Point(26, 96);
            this._btnPersonA.Size = new Size(128, 36);
            this._btnPersonA.Text = "Husband";
            this._btnPersonA.Visible = false;
            this._btnPersonA.Click += new System.EventHandler(this.BtnPersonA_Click);
            this._btnPersonB.Location = new Point(162, 96);
            this._btnPersonB.Size = new Size(132, 36);
            this._btnPersonB.Text = "Wife";
            this._btnPersonB.Visible = false;
            this._btnPersonB.Click += new System.EventHandler(this.BtnPersonB_Click);
            //
            // _lblCamState
            //
            this._lblCamState.AutoSize = true;
            this._lblCamState.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this._lblCamState.ForeColor = Color.FromArgb(160, 40, 50);
            this._lblCamState.Location = new Point(26, 486);
            this._lblCamState.Text = "Camera Not Detected";
            //
            // _btnCapture
            //
            this._btnCapture.BackColor = Color.FromArgb(29, 78, 216);
            this._btnCapture.Cursor = Cursors.Hand;
            this._btnCapture.FlatAppearance.BorderSize = 0;
            this._btnCapture.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 68, 192);
            this._btnCapture.FlatStyle = FlatStyle.Flat;
            this._btnCapture.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            this._btnCapture.ForeColor = Color.White;
            this._btnCapture.Location = new Point(26, 518);
            this._btnCapture.Size = new Size(518, 56);
            this._btnCapture.Text = "Capture Photo";
            this._btnCapture.UseVisualStyleBackColor = false;
            this._btnCapture.Click += new System.EventHandler(this.BtnCapture_Click);
            //
            // _lblCamStatus
            //
            this._lblCamStatus.Font = new Font("Segoe UI", 10F);
            this._lblCamStatus.ForeColor = Color.FromArgb(91, 100, 114);
            this._lblCamStatus.Location = new Point(26, 586);
            this._lblCamStatus.Size = new Size(518, 44);
            this._lblCamStatus.Text = "Starting camera…";
            //
            // _offlineOverlay
            //
            this._offlineOverlay.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this._offlineOverlay.BackColor = Color.FromArgb(17, 24, 39);
            this._offlineOverlay.Location = new Point(0, 0);
            this._offlineOverlay.Size = new Size(1264, 681);
            this._offlineOverlay.Visible = false;
            this._offlineOverlay.Controls.Add(this.lblOffline);
            this.lblOffline.Dock = DockStyle.Fill;
            this.lblOffline.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            this.lblOffline.ForeColor = Color.White;
            this.lblOffline.TextAlign = ContentAlignment.MiddleCenter;
            this.lblOffline.Text = "The office is currently unavailable.\r\n\r\nPlease try again later.\r\n\r\n" +
                "(Waiting for a service window to come online…)";
            //
            // DetailsPhotoForm
            //
            this.AutoScaleMode = AutoScaleMode.None;
            this.BackColor = Color.FromArgb(244, 246, 249);
            this.ClientSize = new Size(1264, 681);
            this.Font = new Font("Segoe UI", 10F);
            this.Text = "CROMS — Client Kiosk";
            this.WindowState = FormWindowState.Maximized;
            this.Controls.Add(this._host);
            this.Controls.Add(this.footer);
            this.Controls.Add(this._btnBack);
            this.Controls.Add(this._btnPrint);
            this.Controls.Add(this.header);
            this.Controls.Add(this._offlineOverlay);
            this.ResumeLayout(false);
        }
    }
}
