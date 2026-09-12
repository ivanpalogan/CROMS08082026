namespace CROMS.Forms
{
    partial class MarriageRegistrationForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        // Layout: one root TableLayoutPanel (header / KPI row / attention board / list),
        // so the desk fills any monitor instead of hugging a fixed 1449px canvas, and on a
        // short screen the form scrolls (AutoScrollMinSize) instead of crushing the list.
        // Spacing scale shared with the other rebuilt modules: page 22, card gap 12,
        // card padding 16/12, KPI row 164 (a KpiCard needs ~148px plus its margin).
        private void InitializeComponent()
        {
            this.tlpRoot = new System.Windows.Forms.TableLayoutPanel();
            this.tlpHeader = new System.Windows.Forms.TableLayoutPanel();
            this.pnlTitle = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.flpActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRegister = new System.Windows.Forms.Button();
            this.btnLicense = new System.Windows.Forms.Button();
            this.btnPsa = new System.Windows.Forms.Button();
            this.tlpKpis = new System.Windows.Forms.TableLayoutPanel();
            this.kpiPosting = new CROMS.Modules.KpiCard();
            this.kpiReady = new CROMS.Modules.KpiCard();
            this.kpiValid = new CROMS.Modules.KpiCard();
            this.kpiExpiring = new CROMS.Modules.KpiCard();
            this.kpiAwaiting = new CROMS.Modules.KpiCard();
            this.kpiPsa = new CROMS.Modules.KpiCard();
            this.cardAttention = new CROMS.Modules.CardPanel();
            this.pnlBoard = new System.Windows.Forms.Panel();
            this.board = new CROMS.Forms.LifecycleBoard();
            this.lblAttention = new System.Windows.Forms.Label();
            this.cardList = new CROMS.Modules.CardPanel();
            this.dgvList = new System.Windows.Forms.DataGridView();
            this.pnlListBar = new System.Windows.Forms.FlowLayoutPanel();
            this.btnTabLicenses = new System.Windows.Forms.Button();
            this.btnTabMarriages = new System.Windows.Forms.Button();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblListCount = new System.Windows.Forms.Label();
            this.tlpRoot.SuspendLayout();
            this.tlpHeader.SuspendLayout();
            this.pnlTitle.SuspendLayout();
            this.flpActions.SuspendLayout();
            this.tlpKpis.SuspendLayout();
            this.cardAttention.SuspendLayout();
            this.pnlBoard.SuspendLayout();
            this.cardList.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvList)).BeginInit();
            this.pnlListBar.SuspendLayout();
            this.SuspendLayout();
            //
            // tlpRoot
            //
            this.tlpRoot.ColumnCount = 1;
            this.tlpRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpRoot.Controls.Add(this.tlpHeader, 0, 0);
            this.tlpRoot.Controls.Add(this.tlpKpis, 0, 1);
            this.tlpRoot.Controls.Add(this.cardAttention, 0, 2);
            this.tlpRoot.Controls.Add(this.cardList, 0, 3);
            this.tlpRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpRoot.Location = new System.Drawing.Point(0, 0);
            this.tlpRoot.Name = "tlpRoot";
            this.tlpRoot.Padding = new System.Windows.Forms.Padding(22, 16, 22, 16);
            this.tlpRoot.RowCount = 4;
            this.tlpRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.tlpRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 164F));
            this.tlpRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.tlpRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.tlpRoot.Size = new System.Drawing.Size(1449, 837);
            this.tlpRoot.TabIndex = 0;
            //
            // tlpHeader
            //
            this.tlpHeader.ColumnCount = 2;
            this.tlpHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpHeader.Controls.Add(this.pnlTitle, 0, 0);
            this.tlpHeader.Controls.Add(this.flpActions, 1, 0);
            this.tlpHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpHeader.Margin = new System.Windows.Forms.Padding(0);
            this.tlpHeader.Name = "tlpHeader";
            this.tlpHeader.RowCount = 1;
            this.tlpHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpHeader.TabIndex = 0;
            //
            // pnlTitle
            //
            this.pnlTitle.Controls.Add(this.lblSubtitle);
            this.pnlTitle.Controls.Add(this.lblTitle);
            this.pnlTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTitle.Margin = new System.Windows.Forms.Padding(0);
            this.pnlTitle.Name = "pnlTitle";
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 17F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(23, 26, 36);
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Text = "MARRIAGE REGISTRATION && LICENSE";
            //
            // lblSubtitle
            //
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(91, 100, 114);
            this.lblSubtitle.Location = new System.Drawing.Point(2, 38);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.UseMnemonic = false;
            this.lblSubtitle.Text = "Municipal Form 90 (application & licence)  ·  Municipal Form 97 (certificate of marriage)  ·  PSA / OCRG transmittal";
            //
            // flpActions
            //
            this.flpActions.Controls.Add(this.btnRegister);
            this.flpActions.Controls.Add(this.btnLicense);
            this.flpActions.Controls.Add(this.btnPsa);
            this.flpActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpActions.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flpActions.Margin = new System.Windows.Forms.Padding(0);
            this.flpActions.Name = "flpActions";
            this.flpActions.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            //
            // btnRegister
            //
            this.btnRegister.BackColor = System.Drawing.Color.FromArgb(29, 78, 216);
            this.btnRegister.FlatAppearance.BorderSize = 0;
            this.btnRegister.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRegister.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnRegister.ForeColor = System.Drawing.Color.White;
            this.btnRegister.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.btnRegister.Name = "btnRegister";
            this.btnRegister.Size = new System.Drawing.Size(190, 38);
            this.btnRegister.Text = "+ REGISTER MARRIAGE";
            this.btnRegister.UseVisualStyleBackColor = false;
            this.btnRegister.Click += new System.EventHandler(this.btnRegister_Click);
            //
            // btnLicense
            //
            this.btnLicense.BackColor = System.Drawing.Color.FromArgb(46, 148, 87);
            this.btnLicense.FlatAppearance.BorderSize = 0;
            this.btnLicense.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLicense.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnLicense.ForeColor = System.Drawing.Color.White;
            this.btnLicense.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.btnLicense.Name = "btnLicense";
            this.btnLicense.Size = new System.Drawing.Size(230, 38);
            this.btnLicense.Text = "+ NEW LICENSE APPLICATION";
            this.btnLicense.UseVisualStyleBackColor = false;
            this.btnLicense.Click += new System.EventHandler(this.btnLicense_Click);
            //
            // btnPsa
            //
            this.btnPsa.BackColor = System.Drawing.Color.FromArgb(238, 241, 246);
            this.btnPsa.FlatAppearance.BorderSize = 0;
            this.btnPsa.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPsa.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnPsa.ForeColor = System.Drawing.Color.FromArgb(23, 26, 36);
            this.btnPsa.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.btnPsa.Name = "btnPsa";
            this.btnPsa.Size = new System.Drawing.Size(160, 38);
            this.btnPsa.Text = "PSA TRANSMITTAL";
            this.btnPsa.UseVisualStyleBackColor = false;
            this.btnPsa.Click += new System.EventHandler(this.btnPsa_Click);
            //
            // tlpKpis
            //
            this.tlpKpis.ColumnCount = 6;
            this.tlpKpis.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.tlpKpis.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.tlpKpis.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.tlpKpis.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.tlpKpis.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.tlpKpis.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.7F));
            this.tlpKpis.Controls.Add(this.kpiPosting, 0, 0);
            this.tlpKpis.Controls.Add(this.kpiReady, 1, 0);
            this.tlpKpis.Controls.Add(this.kpiValid, 2, 0);
            this.tlpKpis.Controls.Add(this.kpiExpiring, 3, 0);
            this.tlpKpis.Controls.Add(this.kpiAwaiting, 4, 0);
            this.tlpKpis.Controls.Add(this.kpiPsa, 5, 0);
            this.tlpKpis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpKpis.Margin = new System.Windows.Forms.Padding(0);
            this.tlpKpis.Name = "tlpKpis";
            this.tlpKpis.RowCount = 1;
            this.tlpKpis.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            //
            // KPI cards
            //
            this.kpiPosting.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiPosting.Margin = new System.Windows.Forms.Padding(0, 0, 12, 14);
            this.kpiPosting.Name = "kpiPosting";
            this.kpiReady.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiReady.Margin = new System.Windows.Forms.Padding(0, 0, 12, 14);
            this.kpiReady.Name = "kpiReady";
            this.kpiValid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiValid.Margin = new System.Windows.Forms.Padding(0, 0, 12, 14);
            this.kpiValid.Name = "kpiValid";
            this.kpiExpiring.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiExpiring.Margin = new System.Windows.Forms.Padding(0, 0, 12, 14);
            this.kpiExpiring.Name = "kpiExpiring";
            this.kpiAwaiting.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiAwaiting.Margin = new System.Windows.Forms.Padding(0, 0, 12, 14);
            this.kpiAwaiting.Name = "kpiAwaiting";
            this.kpiPsa.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiPsa.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
            this.kpiPsa.Name = "kpiPsa";
            //
            // cardAttention
            //
            this.cardAttention.Controls.Add(this.pnlBoard);
            this.cardAttention.Controls.Add(this.lblAttention);
            this.cardAttention.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardAttention.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.cardAttention.Name = "cardAttention";
            this.cardAttention.Padding = new System.Windows.Forms.Padding(16, 12, 16, 12);
            //
            // lblAttention
            //
            this.lblAttention.BackColor = System.Drawing.Color.Transparent;
            this.lblAttention.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAttention.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblAttention.ForeColor = System.Drawing.Color.FromArgb(23, 26, 36);
            this.lblAttention.Height = 28;
            this.lblAttention.Name = "lblAttention";
            this.lblAttention.Text = "ATTENTION / LIFECYCLE";
            //
            // pnlBoard
            //
            this.pnlBoard.AutoScroll = true;
            this.pnlBoard.BackColor = System.Drawing.Color.White;
            this.pnlBoard.Controls.Add(this.board);
            this.pnlBoard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBoard.Name = "pnlBoard";
            //
            // board
            //
            this.board.Dock = System.Windows.Forms.DockStyle.Top;
            this.board.Name = "board";
            this.board.Height = 120;
            //
            // cardList
            //
            this.cardList.Controls.Add(this.dgvList);
            this.cardList.Controls.Add(this.pnlListBar);
            this.cardList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardList.Margin = new System.Windows.Forms.Padding(0);
            this.cardList.Name = "cardList";
            this.cardList.Padding = new System.Windows.Forms.Padding(16, 10, 16, 14);
            //
            // pnlListBar
            //
            this.pnlListBar.BackColor = System.Drawing.Color.Transparent;
            this.pnlListBar.Controls.Add(this.btnTabLicenses);
            this.pnlListBar.Controls.Add(this.btnTabMarriages);
            this.pnlListBar.Controls.Add(this.txtSearch);
            this.pnlListBar.Controls.Add(this.lblListCount);
            this.pnlListBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlListBar.Height = 46;
            this.pnlListBar.Name = "pnlListBar";
            //
            // btnTabLicenses
            //
            this.btnTabLicenses.FlatAppearance.BorderSize = 0;
            this.btnTabLicenses.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabLicenses.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnTabLicenses.Margin = new System.Windows.Forms.Padding(0, 4, 6, 0);
            this.btnTabLicenses.Name = "btnTabLicenses";
            this.btnTabLicenses.Size = new System.Drawing.Size(250, 34);
            this.btnTabLicenses.UseMnemonic = false;
            this.btnTabLicenses.Text = "APPLICATIONS & LICENSES";
            this.btnTabLicenses.UseVisualStyleBackColor = false;
            this.btnTabLicenses.Click += new System.EventHandler(this.btnTabLicenses_Click);
            //
            // btnTabMarriages
            //
            this.btnTabMarriages.FlatAppearance.BorderSize = 0;
            this.btnTabMarriages.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabMarriages.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnTabMarriages.Margin = new System.Windows.Forms.Padding(0, 4, 18, 0);
            this.btnTabMarriages.Name = "btnTabMarriages";
            this.btnTabMarriages.Size = new System.Drawing.Size(200, 34);
            this.btnTabMarriages.Text = "MARRIAGES";
            this.btnTabMarriages.UseVisualStyleBackColor = false;
            this.btnTabMarriages.Click += new System.EventHandler(this.btnTabMarriages_Click);
            //
            // txtSearch
            //
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtSearch.Margin = new System.Windows.Forms.Padding(0, 8, 12, 0);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(320, 25);
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            //
            // lblListCount
            //
            this.lblListCount.AutoSize = true;
            this.lblListCount.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblListCount.ForeColor = System.Drawing.Color.FromArgb(91, 100, 114);
            this.lblListCount.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
            this.lblListCount.Name = "lblListCount";
            this.lblListCount.Text = "Search: name, application no., licence no., registry no.";
            //
            // dgvList
            //
            this.dgvList.AllowUserToAddRows = false;
            this.dgvList.AllowUserToDeleteRows = false;
            this.dgvList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvList.BackgroundColor = System.Drawing.Color.White;
            this.dgvList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvList.Name = "dgvList";
            this.dgvList.ReadOnly = true;
            this.dgvList.RowHeadersVisible = false;
            this.dgvList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvList.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvList_CellDoubleClick);
            //
            // MarriageRegistrationForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(1120, 820);
            this.BackColor = System.Drawing.Color.FromArgb(244, 246, 249);
            this.ClientSize = new System.Drawing.Size(1449, 837);
            this.Controls.Add(this.tlpRoot);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "MarriageRegistrationForm";
            this.Text = "Marriage Registration";
            this.tlpRoot.ResumeLayout(false);
            this.tlpHeader.ResumeLayout(false);
            this.pnlTitle.ResumeLayout(false);
            this.pnlTitle.PerformLayout();
            this.flpActions.ResumeLayout(false);
            this.tlpKpis.ResumeLayout(false);
            this.cardAttention.ResumeLayout(false);
            this.pnlBoard.ResumeLayout(false);
            this.cardList.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvList)).EndInit();
            this.pnlListBar.ResumeLayout(false);
            this.pnlListBar.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlpRoot;
        private System.Windows.Forms.TableLayoutPanel tlpHeader;
        private System.Windows.Forms.Panel pnlTitle;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.FlowLayoutPanel flpActions;
        private System.Windows.Forms.Button btnRegister;
        private System.Windows.Forms.Button btnLicense;
        private System.Windows.Forms.Button btnPsa;
        private System.Windows.Forms.TableLayoutPanel tlpKpis;
        private CROMS.Modules.KpiCard kpiPosting;
        private CROMS.Modules.KpiCard kpiReady;
        private CROMS.Modules.KpiCard kpiValid;
        private CROMS.Modules.KpiCard kpiExpiring;
        private CROMS.Modules.KpiCard kpiAwaiting;
        private CROMS.Modules.KpiCard kpiPsa;
        private CROMS.Modules.CardPanel cardAttention;
        private System.Windows.Forms.Panel pnlBoard;
        private CROMS.Forms.LifecycleBoard board;
        private System.Windows.Forms.Label lblAttention;
        private CROMS.Modules.CardPanel cardList;
        private System.Windows.Forms.DataGridView dgvList;
        private System.Windows.Forms.FlowLayoutPanel pnlListBar;
        private System.Windows.Forms.Button btnTabLicenses;
        private System.Windows.Forms.Button btnTabMarriages;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblListCount;
    }
}
