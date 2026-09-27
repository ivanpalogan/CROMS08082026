using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    partial class MarriageEntryForm
    {
        // chrome
        private readonly StepStrip _tabs = new StepStrip(false);
        private readonly Panel[] _pages = new Panel[5];
        private readonly Panel _rail = new Panel();
        private readonly StatusPill _formPill = new StatusPill(), _statusPill = new StatusPill(), _stepPill = new StatusPill();
        private readonly Label _footInfo = MUi.Txt("", 9F, FontStyle.Regular, UiTheme.Muted);
        // migration 60: transaction/queue linkage + Current Step + activity history, shown in
        // one strip under the header (spec: "Create Marriage Registration Record" record view)
        private readonly Panel _infoStrip = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.FromArgb(250, 251, 253) };
        private readonly Label _txnLabel = MUi.Txt("Transaction: -", 9F, FontStyle.Regular, UiTheme.Muted);
        private readonly Label _queueLabel = MUi.Txt("Queue: -", 9F, FontStyle.Regular, UiTheme.Muted);
        private readonly Button _btnLinkTxn = MUi.Btn("Link...", MUi.Kind.Ghost, 60), _btnLinkQueue = MUi.Btn("Link...", MUi.Kind.Ghost, 60),
                                _btnHistory = MUi.Btn("Activity History", MUi.Kind.Ghost);
        private readonly Button _btnMobileCapture = MUi.Btn("Mobile Capture...", MUi.Kind.Primary, 140);
        private readonly Button _btnSoft = MUi.Btn("View softcopy", MUi.Kind.Ghost), _btnPreview = MUi.Btn("Preview on form", MUi.Kind.Ghost),
                                _btnCase = MUi.Btn("Case workflow...", MUi.Kind.Ghost), _btnAckSlip = MUi.Btn("Print acknowledgment slip", MUi.Kind.Ghost),
                                _btnDraft = MUi.Btn("Save as draft", MUi.Kind.Secondary),
                                _btnReview = MUi.Btn("Send for review", MUi.Kind.Secondary), _btnRegister = MUi.Btn("REGISTER MARRIAGE", MUi.Kind.Success);
        private readonly IssueList _issues = new IssueList();

        // tab 1-2
        private readonly TextBox _reg = MUi.Box(), _book = MUi.Box(), _page = MUi.Box();
        // tab 3
        private readonly RadioButton _rbLic = new RadioButton { Text = "WITH MARRIAGE LICENSE", AutoSize = true, Font = MUi.F(9.5F, FontStyle.Bold) },
                                     _rbEx = new RadioButton { Text = "LICENSE EXEMPT", AutoSize = true, Font = MUi.F(9.5F, FontStyle.Bold) };
        private readonly TextBox _licSearch = MUi.Box(), _licPlace = MUi.Box(), _exNotes = MUi.Box();
        private readonly CheckBox _licAll = new CheckBox { Text = "Show all licences", AutoSize = true };
        private readonly ListBox _licList = new ListBox { Font = MUi.F(9.5F), IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle };
        private readonly Panel _licPanel = new Panel(), _exPanel = new Panel(), _licSummary = new Panel();
        // "licence obtained in another province" - backlog Sec.4.1: trigger confirmed 2026-09-13,
        // which document proves it is still open, so this is a generic attachment slot, not a
        // named-document requirement.
        private readonly CheckBox _oop = new CheckBox { Text = "This licence was obtained in ANOTHER province (wedding solemnized here)", AutoSize = true, Font = MUi.F(9F, FontStyle.Bold) };
        private readonly TextBox _oopLicNo = MUi.Box();
        private readonly DateTimePicker _oopLicDate = MUi.Date(true);
        private readonly Panel _oopPanel = new Panel(), _localPanel = new Panel();
        private readonly ComboBox _exBasis = MUi.Combo(false);
        private readonly RequirementsGrid _docs = new RequirementsGrid();
        private readonly Label _docsHint = MUi.Txt("", 9F, FontStyle.Regular, UiTheme.Muted);
        // tab 4
        private readonly DateTimePicker _dom = MUi.Date(true);
        private readonly TextBox _tom = MUi.Box(), _sol = MUi.Box(), _solPos = MUi.Box(), _w1 = MUi.Box(), _w2 = MUi.Box();
        private readonly ComboBox _church = MUi.Combo(false), _prov = MUi.Combo(false), _muni = MUi.Combo(false);
        // tab 5
        private readonly TextBox _recvBy = MUi.Box(), _recvTitle = MUi.Box(), _remarks = MUi.Box(), _delay = MUi.Box();
        private readonly DateTimePicker _recv = MUi.Date(true);
        private readonly Banner _regBanner = new Banner();
        // STEP 9 - Registration Information: what the LCRO actually wrote on the physical,
        // already-signed Form 97 - registry_no/date_registered/remarks are existing columns
        // reused here; registered_by_name is new (migration 62, distinct from the INT
        // registered_by system stamp Register() writes).
        private readonly TextBox _registeredByName = MUi.Box();
        private readonly DateTimePicker _dateRegistered = MUi.Date(true);
        private readonly Button _btnSaveRegInfo = MUi.Btn("Save Registration Information", MUi.Kind.Secondary, 220);
        // "Submitted By" - who is filing this Form 97 registration with the office.
        private readonly RadioButton _rbSubOfficer = new RadioButton { Text = "Solemnizing Officer", AutoSize = true },
                                     _rbSubHusband = new RadioButton { Text = "Husband", AutoSize = true },
                                     _rbSubWife = new RadioButton { Text = "Wife", AutoSize = true },
                                     _rbSubRep = new RadioButton { Text = "Others (specify)", AutoSize = true };
        private readonly TextBox _repName = MUi.Box(), _repOrg = MUi.Box();
        private readonly Panel _repPanel = new Panel();

        private void InitializeComponent()
        {
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1260, 800); MinimumSize = new Size(1100, 700);
            BackColor = UiTheme.PageBg; ShowInTaskbar = false; KeyPreview = true;
        }
    }
}
