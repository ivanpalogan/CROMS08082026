using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Step 1 of 2 — choose one or more services. Designer-generated visual tree (behaviour
    /// in ServiceSelectForm.cs).
    /// </summary>
    public partial class ServiceSelectForm
    {
        private System.ComponentModel.IContainer components = null;

        private Panel header;
        private Label lblTitle;
        private StepIndicator _stepInd;
        private Panel footer;
        private Panel footerDivider;
        private Button _btnNext;
        private Button _btnBack;
        private Panel _host;
        private Panel panelStep1;
        private Label _svcHint;
        private Panel _svcGrid;
        private Panel _card0, _card1, _card2, _card3, _card4, _card5, _card6, _card7, _card8, _card9, _card10, _card11, _card12, _card13, _card14;
        private Label lblName0, lblName1, lblName2, lblName3, lblName4, lblName5, lblName6, lblName7, lblName8, lblName9, lblName10, lblName11, lblName12, lblName13, lblName14;
        private Panel _offlineOverlay;
        private Label lblOffline;

        private readonly System.Collections.Generic.Dictionary<string, Panel> _cards =
            new System.Collections.Generic.Dictionary<string, Panel>();

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.header = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.footer = new System.Windows.Forms.Panel();
            this.footerDivider = new System.Windows.Forms.Panel();
            this._btnNext = new System.Windows.Forms.Button();
            this._btnBack = new System.Windows.Forms.Button();
            this._host = new System.Windows.Forms.Panel();
            this.panelStep1 = new System.Windows.Forms.Panel();
            this._svcHint = new System.Windows.Forms.Label();
            this._svcGrid = new System.Windows.Forms.Panel();
            this._card0 = new System.Windows.Forms.Panel();
            this.lblName0 = new System.Windows.Forms.Label();
            this._card1 = new System.Windows.Forms.Panel();
            this.lblName1 = new System.Windows.Forms.Label();
            this._card2 = new System.Windows.Forms.Panel();
            this.lblName2 = new System.Windows.Forms.Label();
            this._card3 = new System.Windows.Forms.Panel();
            this.lblName3 = new System.Windows.Forms.Label();
            this._card4 = new System.Windows.Forms.Panel();
            this.lblName4 = new System.Windows.Forms.Label();
            this._card5 = new System.Windows.Forms.Panel();
            this.lblName5 = new System.Windows.Forms.Label();
            this._card6 = new System.Windows.Forms.Panel();
            this.lblName6 = new System.Windows.Forms.Label();
            this._card7 = new System.Windows.Forms.Panel();
            this.lblName7 = new System.Windows.Forms.Label();
            this._card8 = new System.Windows.Forms.Panel();
            this.lblName8 = new System.Windows.Forms.Label();
            this._card9 = new System.Windows.Forms.Panel();
            this.lblName9 = new System.Windows.Forms.Label();
            this._card10 = new System.Windows.Forms.Panel();
            this.lblName10 = new System.Windows.Forms.Label();
            this._card11 = new System.Windows.Forms.Panel();
            this.lblName11 = new System.Windows.Forms.Label();
            this._card12 = new System.Windows.Forms.Panel();
            this.lblName12 = new System.Windows.Forms.Label();
            this._card13 = new System.Windows.Forms.Panel();
            this.lblName13 = new System.Windows.Forms.Label();
            this._card14 = new System.Windows.Forms.Panel();
            this.lblName14 = new System.Windows.Forms.Label();
            this._offlineOverlay = new System.Windows.Forms.Panel();
            this.lblOffline = new System.Windows.Forms.Label();
            this._stepInd = new CROMS.Kiosk.StepIndicator();
            this.header.SuspendLayout();
            this.footer.SuspendLayout();
            this._host.SuspendLayout();
            this.panelStep1.SuspendLayout();
            this._svcGrid.SuspendLayout();
            this._card0.SuspendLayout();
            this._card1.SuspendLayout();
            this._card2.SuspendLayout();
            this._card3.SuspendLayout();
            this._card4.SuspendLayout();
            this._card5.SuspendLayout();
            this._card6.SuspendLayout();
            this._card7.SuspendLayout();
            this._card8.SuspendLayout();
            this._card9.SuspendLayout();
            this._card10.SuspendLayout();
            this._card11.SuspendLayout();
            this._card12.SuspendLayout();
            this._card13.SuspendLayout();
            this._card14.SuspendLayout();
            this._offlineOverlay.SuspendLayout();
            this.SuspendLayout();
            // 
            // header
            // 
            this.header.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.header.Controls.Add(this.lblTitle);
            this.header.Controls.Add(this._stepInd);
            this.header.Dock = System.Windows.Forms.DockStyle.Top;
            this.header.Location = new System.Drawing.Point(0, 0);
            this.header.Name = "header";
            this.header.Size = new System.Drawing.Size(1264, 128);
            this.header.TabIndex = 3;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 30F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblTitle.Location = new System.Drawing.Point(40, 18);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(355, 54);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Request a Service";
            // 
            // footer
            // 
            this.footer.BackColor = System.Drawing.Color.White;
            this.footer.Controls.Add(this.footerDivider);
            this.footer.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.footer.Location = new System.Drawing.Point(0, 589);
            this.footer.Name = "footer";
            this.footer.Size = new System.Drawing.Size(1264, 92);
            this.footer.TabIndex = 1;
            // 
            // footerDivider
            // 
            this.footerDivider.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.footerDivider.Dock = System.Windows.Forms.DockStyle.Top;
            this.footerDivider.Location = new System.Drawing.Point(0, 0);
            this.footerDivider.Name = "footerDivider";
            this.footerDivider.Size = new System.Drawing.Size(1264, 1);
            this.footerDivider.TabIndex = 0;
            // 
            // _btnNext
            // 
            this._btnNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnNext.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this._btnNext.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnNext.FlatAppearance.BorderSize = 0;
            this._btnNext.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(68)))), ((int)(((byte)(192)))));
            this._btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnNext.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this._btnNext.ForeColor = System.Drawing.Color.White;
            this._btnNext.Location = new System.Drawing.Point(904, 593);
            this._btnNext.Name = "_btnNext";
            this._btnNext.Size = new System.Drawing.Size(320, 72);
            this._btnNext.TabIndex = 2;
            this._btnNext.Text = "Next Step";
            this._btnNext.UseVisualStyleBackColor = false;
            this._btnNext.Click += new System.EventHandler(this.BtnNext_Click);
            // 
            // _btnBack
            // 
            this._btnBack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._btnBack.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnBack.FlatAppearance.BorderSize = 0;
            this._btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnBack.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this._btnBack.Location = new System.Drawing.Point(40, 593);
            this._btnBack.Name = "_btnBack";
            this._btnBack.Size = new System.Drawing.Size(210, 72);
            this._btnBack.TabIndex = 6;
            this._btnBack.Text = "Back";
            this._btnBack.UseVisualStyleBackColor = false;
            this._btnBack.Click += new System.EventHandler(this.BtnBack_Click);
            // 
            // _host
            // 
            this._host.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this._host.Controls.Add(this.panelStep1);
            this._host.Dock = System.Windows.Forms.DockStyle.Fill;
            this._host.Location = new System.Drawing.Point(0, 128);
            this._host.Name = "_host";
            this._host.Size = new System.Drawing.Size(1264, 461);
            this._host.TabIndex = 0;
            // 
            // panelStep1
            // 
            this.panelStep1.AutoScroll = true;
            this.panelStep1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.panelStep1.Controls.Add(this._svcHint);
            this.panelStep1.Controls.Add(this._svcGrid);
            this.panelStep1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelStep1.Location = new System.Drawing.Point(0, 0);
            this.panelStep1.Name = "panelStep1";
            this.panelStep1.Size = new System.Drawing.Size(1264, 461);
            this.panelStep1.TabIndex = 0;
            this.panelStep1.Resize += new System.EventHandler(this.PanelStep1_Resize);
            // 
            // _svcHint
            // 
            this._svcHint.AutoSize = true;
            this._svcHint.Font = new System.Drawing.Font("Segoe UI", 13F);
            this._svcHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this._svcHint.Location = new System.Drawing.Point(20, 20);
            this._svcHint.Name = "_svcHint";
            this._svcHint.Size = new System.Drawing.Size(509, 25);
            this._svcHint.TabIndex = 0;
            this._svcHint.Text = "Tap one or more services you need today. You can pick several.";
            // 
            // _svcGrid
            // 
            this._svcGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this._svcGrid.Controls.Add(this._card0);
            this._svcGrid.Controls.Add(this._card1);
            this._svcGrid.Controls.Add(this._card2);
            this._svcGrid.Controls.Add(this._card3);
            this._svcGrid.Controls.Add(this._card4);
            this._svcGrid.Controls.Add(this._card5);
            this._svcGrid.Controls.Add(this._card6);
            this._svcGrid.Controls.Add(this._card7);
            this._svcGrid.Controls.Add(this._card8);
            this._svcGrid.Controls.Add(this._card9);
            this._svcGrid.Controls.Add(this._card10);
            this._svcGrid.Controls.Add(this._card11);
            this._svcGrid.Controls.Add(this._card12);
            this._svcGrid.Controls.Add(this._card13);
            this._svcGrid.Controls.Add(this._card14);
            this._svcGrid.Location = new System.Drawing.Point(20, 66);
            this._svcGrid.Name = "_svcGrid";
            this._svcGrid.Size = new System.Drawing.Size(1076, 1064);
            this._svcGrid.TabIndex = 1;
            // 
            // _card0
            // 
            this._card0.BackColor = System.Drawing.Color.White;
            this._card0.Controls.Add(this.lblName0);
            this._card0.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card0.Location = new System.Drawing.Point(0, 0);
            this._card0.Name = "_card0";
            this._card0.Size = new System.Drawing.Size(348, 200);
            this._card0.TabIndex = 0;
            this._card0.Tag = "BIRTHREG";
            this._card0.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName0
            // 
            this.lblName0.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName0.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName0.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName0.Location = new System.Drawing.Point(0, 138);
            this.lblName0.Name = "lblName0";
            this.lblName0.Size = new System.Drawing.Size(348, 46);
            this.lblName0.TabIndex = 0;
            this.lblName0.Text = "Birth Registration";
            this.lblName0.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName0.Click += new System.EventHandler(this.Card_Click);
            // 
            // _card1
            // 
            this._card1.BackColor = System.Drawing.Color.White;
            this._card1.Controls.Add(this.lblName1);
            this._card1.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card1.Location = new System.Drawing.Point(364, 0);
            this._card1.Name = "_card1";
            this._card1.Size = new System.Drawing.Size(348, 200);
            this._card1.TabIndex = 1;
            this._card1.Tag = "CTC";
            this._card1.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName1
            // 
            this.lblName1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName1.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName1.Location = new System.Drawing.Point(0, 138);
            this.lblName1.Name = "lblName1";
            this.lblName1.Size = new System.Drawing.Size(348, 46);
            this.lblName1.TabIndex = 0;
            this.lblName1.Text = "Certified True Copy (CTC)";
            this.lblName1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName1.Click += new System.EventHandler(this.Card_Click);
            // 
            // _card2
            // 
            this._card2.BackColor = System.Drawing.Color.White;
            this._card2.Controls.Add(this.lblName2);
            this._card2.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card2.Location = new System.Drawing.Point(728, 0);
            this._card2.Name = "_card2";
            this._card2.Size = new System.Drawing.Size(348, 200);
            this._card2.TabIndex = 2;
            this._card2.Tag = "MARRIAGE_APP";
            this._card2.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName2
            // 
            this.lblName2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName2.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName2.Location = new System.Drawing.Point(0, 138);
            this.lblName2.Name = "lblName2";
            this.lblName2.Size = new System.Drawing.Size(348, 46);
            this.lblName2.TabIndex = 0;
            this.lblName2.Text = "Marriage Application";
            this.lblName2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName2.Click += new System.EventHandler(this.Card_Click);
            // 
            // _card3
            // 
            this._card3.BackColor = System.Drawing.Color.White;
            this._card3.Controls.Add(this.lblName3);
            this._card3.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card3.Location = new System.Drawing.Point(0, 216);
            this._card3.Name = "_card3";
            this._card3.Size = new System.Drawing.Size(348, 200);
            this._card3.TabIndex = 3;
            this._card3.Tag = "MARRIAGE_REG";
            this._card3.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName3
            // 
            this.lblName3.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName3.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName3.Location = new System.Drawing.Point(0, 138);
            this.lblName3.Name = "lblName3";
            this.lblName3.Size = new System.Drawing.Size(348, 46);
            this.lblName3.TabIndex = 0;
            this.lblName3.Text = "Marriage Registration";
            this.lblName3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName3.Click += new System.EventHandler(this.Card_Click);
            // 
            // _card4
            // 
            this._card4.BackColor = System.Drawing.Color.White;
            this._card4.Controls.Add(this.lblName4);
            this._card4.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card4.Location = new System.Drawing.Point(364, 216);
            this._card4.Name = "_card4";
            this._card4.Size = new System.Drawing.Size(348, 200);
            this._card4.TabIndex = 4;
            this._card4.Tag = "DEATH";
            this._card4.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName4
            // 
            this.lblName4.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName4.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName4.Location = new System.Drawing.Point(0, 138);
            this.lblName4.Name = "lblName4";
            this.lblName4.Size = new System.Drawing.Size(348, 46);
            this.lblName4.TabIndex = 0;
            this.lblName4.Text = "Death Certificate";
            this.lblName4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName4.Click += new System.EventHandler(this.Card_Click);
            // 
            // _card5
            // 
            this._card5.BackColor = System.Drawing.Color.White;
            this._card5.Controls.Add(this.lblName5);
            this._card5.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card5.Location = new System.Drawing.Point(728, 216);
            this._card5.Name = "_card5";
            this._card5.Size = new System.Drawing.Size(348, 200);
            this._card5.TabIndex = 5;
            this._card5.Tag = "PETITION";
            this._card5.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName5
            // 
            this.lblName5.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName5.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName5.Location = new System.Drawing.Point(0, 138);
            this.lblName5.Name = "lblName5";
            this.lblName5.Size = new System.Drawing.Size(348, 46);
            this.lblName5.TabIndex = 0;
            this.lblName5.Text = "Petition (Correction)";
            this.lblName5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName5.Click += new System.EventHandler(this.Card_Click);
            // 
            // _card6
            // 
            this._card6.BackColor = System.Drawing.Color.White;
            this._card6.Controls.Add(this.lblName6);
            this._card6.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card6.Location = new System.Drawing.Point(0, 432);
            this._card6.Name = "_card6";
            this._card6.Size = new System.Drawing.Size(348, 200);
            this._card6.TabIndex = 6;
            this._card6.Tag = "VERIFY";
            this._card6.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName6
            // 
            this.lblName6.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName6.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName6.Location = new System.Drawing.Point(0, 138);
            this.lblName6.Name = "lblName6";
            this.lblName6.Size = new System.Drawing.Size(348, 46);
            this.lblName6.TabIndex = 0;
            this.lblName6.Text = "Verification / Others";
            this.lblName6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName6.Click += new System.EventHandler(this.Card_Click);
            // 
            // _card7
            // 
            this._card7.BackColor = System.Drawing.Color.White;
            this._card7.Controls.Add(this.lblName7);
            this._card7.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card7.Location = new System.Drawing.Point(364, 432);
            this._card7.Name = "_card7";
            this._card7.Size = new System.Drawing.Size(348, 200);
            this._card7.TabIndex = 7;
            this._card7.Tag = "CLAIM";
            this._card7.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName7
            // 
            this.lblName7.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName7.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName7.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName7.Location = new System.Drawing.Point(0, 138);
            this.lblName7.Name = "lblName7";
            this.lblName7.Size = new System.Drawing.Size(348, 46);
            this.lblName7.TabIndex = 0;
            this.lblName7.Text = "Release & Claim (Pick-up)";
            this.lblName7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName7.Click += new System.EventHandler(this.Card_Click);
            // 
            // _card8
            // 
            this._card8.BackColor = System.Drawing.Color.White;
            this._card8.Controls.Add(this.lblName8);
            this._card8.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card8.Location = new System.Drawing.Point(728, 432);
            this._card8.Name = "_card8";
            this._card8.Size = new System.Drawing.Size(348, 200);
            this._card8.TabIndex = 8;
            this._card8.Tag = "LEGITIMATION";
            this._card8.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName8
            // 
            this.lblName8.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName8.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName8.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName8.Location = new System.Drawing.Point(0, 138);
            this.lblName8.Name = "lblName8";
            this.lblName8.Size = new System.Drawing.Size(348, 46);
            this.lblName8.TabIndex = 0;
            this.lblName8.Text = "Legitimation";
            this.lblName8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName8.Click += new System.EventHandler(this.Card_Click);
            // 
            // _card9
            // 
            this._card9.BackColor = System.Drawing.Color.White;
            this._card9.Controls.Add(this.lblName9);
            this._card9.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card9.Location = new System.Drawing.Point(0, 648);
            this._card9.Name = "_card9";
            this._card9.Size = new System.Drawing.Size(348, 200);
            this._card9.TabIndex = 9;
            this._card9.Tag = "BREKS";
            this._card9.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName9
            // 
            this.lblName9.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName9.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName9.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName9.Location = new System.Drawing.Point(0, 138);
            this.lblName9.Name = "lblName9";
            this.lblName9.Size = new System.Drawing.Size(348, 46);
            this.lblName9.TabIndex = 0;
            this.lblName9.Text = "Breks";
            this.lblName9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName9.Click += new System.EventHandler(this.Card_Click);
            // 
            // _card10
            // 
            this._card10.BackColor = System.Drawing.Color.White;
            this._card10.Controls.Add(this.lblName10);
            this._card10.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card10.Location = new System.Drawing.Point(364, 648);
            this._card10.Name = "_card10";
            this._card10.Size = new System.Drawing.Size(348, 200);
            this._card10.TabIndex = 10;
            this._card10.Tag = "SUPPLEMENTAL";
            this._card10.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName10
            // 
            this.lblName10.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName10.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName10.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName10.Location = new System.Drawing.Point(0, 138);
            this.lblName10.Name = "lblName10";
            this.lblName10.Size = new System.Drawing.Size(348, 46);
            this.lblName10.TabIndex = 0;
            this.lblName10.Text = "Supplemental";
            this.lblName10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName10.Click += new System.EventHandler(this.Card_Click);
            // 
            // _card11
            // 
            this._card11.BackColor = System.Drawing.Color.White;
            this._card11.Controls.Add(this.lblName11);
            this._card11.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card11.Location = new System.Drawing.Point(728, 648);
            this._card11.Name = "_card11";
            this._card11.Size = new System.Drawing.Size(348, 200);
            this._card11.TabIndex = 11;
            this._card11.Tag = "COURT_ORDER";
            this._card11.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName11
            // 
            this.lblName11.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName11.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName11.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName11.Location = new System.Drawing.Point(0, 138);
            this.lblName11.Name = "lblName11";
            this.lblName11.Size = new System.Drawing.Size(348, 46);
            this.lblName11.TabIndex = 0;
            this.lblName11.Text = "Court Order";
            this.lblName11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName11.Click += new System.EventHandler(this.Card_Click);
            // 
            // _card12
            // 
            this._card12.BackColor = System.Drawing.Color.White;
            this._card12.Controls.Add(this.lblName12);
            this._card12.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card12.Location = new System.Drawing.Point(0, 864);
            this._card12.Name = "_card12";
            this._card12.Size = new System.Drawing.Size(348, 200);
            this._card12.TabIndex = 12;
            this._card12.Tag = "SUPPLEMENTAL_REPORT";
            this._card12.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName12
            // 
            this.lblName12.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName12.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName12.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName12.Location = new System.Drawing.Point(0, 138);
            this.lblName12.Name = "lblName12";
            this.lblName12.Size = new System.Drawing.Size(348, 46);
            this.lblName12.TabIndex = 0;
            this.lblName12.Text = "Supplemental Report";
            this.lblName12.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName12.Click += new System.EventHandler(this.Card_Click);
            // 
            // _card13
            // 
            this._card13.BackColor = System.Drawing.Color.White;
            this._card13.Controls.Add(this.lblName13);
            this._card13.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card13.Location = new System.Drawing.Point(364, 864);
            this._card13.Name = "_card13";
            this._card13.Size = new System.Drawing.Size(348, 200);
            this._card13.TabIndex = 13;
            this._card13.Tag = "LEGAL_INSTRUMENTS";
            this._card13.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName13
            // 
            this.lblName13.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName13.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName13.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName13.Location = new System.Drawing.Point(0, 138);
            this.lblName13.Name = "lblName13";
            this.lblName13.Size = new System.Drawing.Size(348, 46);
            this.lblName13.TabIndex = 0;
            this.lblName13.Text = "Legal Instruments";
            this.lblName13.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName13.Click += new System.EventHandler(this.Card_Click);
            // 
            // _card14
            // 
            this._card14.BackColor = System.Drawing.Color.White;
            this._card14.Controls.Add(this.lblName14);
            this._card14.Cursor = System.Windows.Forms.Cursors.Hand;
            this._card14.Location = new System.Drawing.Point(728, 864);
            this._card14.Name = "_card14";
            this._card14.Size = new System.Drawing.Size(348, 200);
            this._card14.TabIndex = 14;
            this._card14.Tag = "LEGITIMATION_RA9255";
            this._card14.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblName14
            // 
            this.lblName14.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblName14.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblName14.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblName14.Location = new System.Drawing.Point(0, 138);
            this.lblName14.Name = "lblName14";
            this.lblName14.Size = new System.Drawing.Size(348, 46);
            this.lblName14.TabIndex = 0;
            this.lblName14.Text = "Legitimation RA-9255";
            this.lblName14.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblName14.Click += new System.EventHandler(this.Card_Click);
            // 
            // _offlineOverlay
            // 
            this._offlineOverlay.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._offlineOverlay.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(17)))), ((int)(((byte)(24)))), ((int)(((byte)(39)))));
            this._offlineOverlay.Controls.Add(this.lblOffline);
            this._offlineOverlay.Location = new System.Drawing.Point(0, 0);
            this._offlineOverlay.Name = "_offlineOverlay";
            this._offlineOverlay.Size = new System.Drawing.Size(1264, 681);
            this._offlineOverlay.TabIndex = 4;
            this._offlineOverlay.Visible = false;
            // 
            // lblOffline
            // 
            this.lblOffline.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOffline.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold);
            this.lblOffline.ForeColor = System.Drawing.Color.White;
            this.lblOffline.Location = new System.Drawing.Point(0, 0);
            this.lblOffline.Name = "lblOffline";
            this.lblOffline.Size = new System.Drawing.Size(1264, 681);
            this.lblOffline.TabIndex = 0;
            this.lblOffline.Text = "The office is currently unavailable.\r\n\r\nPlease try again later.\r\n\r\n(Waiting for a" +
    " service window to come online…)";
            this.lblOffline.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _stepInd
            // 
            this._stepInd.BackColor = System.Drawing.Color.Transparent;
            this._stepInd.Location = new System.Drawing.Point(40, 74);
            this._stepInd.Name = "_stepInd";
            this._stepInd.Size = new System.Drawing.Size(560, 52);
            this._stepInd.Steps = new string[] {
        "Select Services",
        "Personal Info & Photo"};
            this._stepInd.TabIndex = 1;
            // 
            // ServiceSelectForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(1264, 681);
            this.Controls.Add(this._host);
            this.Controls.Add(this.footer);
            this.Controls.Add(this._btnNext);
            this.Controls.Add(this._btnBack);
            this.Controls.Add(this.header);
            this.Controls.Add(this._offlineOverlay);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Name = "ServiceSelectForm";
            this.Text = "CROMS — Client Kiosk";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.header.ResumeLayout(false);
            this.header.PerformLayout();
            this.footer.ResumeLayout(false);
            this._host.ResumeLayout(false);
            this.panelStep1.ResumeLayout(false);
            this.panelStep1.PerformLayout();
            this._svcGrid.ResumeLayout(false);
            this._card0.ResumeLayout(false);
            this._card1.ResumeLayout(false);
            this._card2.ResumeLayout(false);
            this._card3.ResumeLayout(false);
            this._card4.ResumeLayout(false);
            this._card5.ResumeLayout(false);
            this._card6.ResumeLayout(false);
            this._card7.ResumeLayout(false);
            this._card8.ResumeLayout(false);
            this._card9.ResumeLayout(false);
            this._card10.ResumeLayout(false);
            this._card11.ResumeLayout(false);
            this._card12.ResumeLayout(false);
            this._card13.ResumeLayout(false);
            this._card14.ResumeLayout(false);
            this._offlineOverlay.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
