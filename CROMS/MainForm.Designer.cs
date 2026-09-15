namespace CROMS
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.mainPanel = new System.Windows.Forms.Panel();
            this.contentPanel = new System.Windows.Forms.Panel();
            this.headerPanel = new System.Windows.Forms.Panel();
            this.headerLabel = new System.Windows.Forms.Label();
            this.sidebarPanel = new System.Windows.Forms.Panel();
            this.navFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.lblGrpClient = new System.Windows.Forms.Label();
            this.btnDashboard = new System.Windows.Forms.Button();
            this.btnQueue = new System.Windows.Forms.Button();
            this.btnTransactions = new System.Windows.Forms.Button();
            this.btnCertRequest = new System.Windows.Forms.Button();
            this.btnRelease = new System.Windows.Forms.Button();
            this.btnBreqs = new System.Windows.Forms.Button();
            this.lblGrpCertification = new System.Windows.Forms.Label();
            this.lblGrpRecord = new System.Windows.Forms.Label();
            this.btnBirth = new System.Windows.Forms.Button();
            this.btnMarriage = new System.Windows.Forms.Button();
            this.btnDeath = new System.Windows.Forms.Button();
            this.btnPetitions = new System.Windows.Forms.Button();
            this.btnBooks = new System.Windows.Forms.Button();
            this.btnSearch = new System.Windows.Forms.Button();
            this.lblGrpDocument = new System.Windows.Forms.Label();
            this.btnOcr = new System.Windows.Forms.Button();
            this.lblGrpOperations = new System.Windows.Forms.Label();
            this.btnFees = new System.Windows.Forms.Button();
            this.btnReports = new System.Windows.Forms.Button();
            this.lblGrpAdmin = new System.Windows.Forms.Label();
            this.btnMaster = new System.Windows.Forms.Button();
            this.btnUsers = new System.Windows.Forms.Button();
            this.btnArchive = new System.Windows.Forms.Button();
            this.btnCertTemplates = new System.Windows.Forms.Button();
            this.btnWindows = new System.Windows.Forms.Button();
            this.brandPanel = new System.Windows.Forms.Panel();
            this.brandLabel = new System.Windows.Forms.Label();
            this.mainPanel.SuspendLayout();
            this.headerPanel.SuspendLayout();
            this.sidebarPanel.SuspendLayout();
            this.navFlow.SuspendLayout();
            this.brandPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // mainPanel
            //
            this.mainPanel.Controls.Add(this.contentPanel);
            this.mainPanel.Controls.Add(this.headerPanel);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(220, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(964, 681);
            this.mainPanel.TabIndex = 1;
            //
            // contentPanel
            //
            this.contentPanel.AutoScroll = true;
            this.contentPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentPanel.Location = new System.Drawing.Point(0, 56);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Size = new System.Drawing.Size(964, 625);
            this.contentPanel.TabIndex = 1;
            //
            // headerPanel
            //
            this.headerPanel.BackColor = System.Drawing.Color.White;
            this.headerPanel.Controls.Add(this.headerLabel);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(964, 56);
            this.headerPanel.TabIndex = 0;
            //
            // headerLabel
            //
            this.headerLabel.AutoSize = true;
            this.headerLabel.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
            this.headerLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.headerLabel.Location = new System.Drawing.Point(20, 12);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Size = new System.Drawing.Size(114, 28);
            this.headerLabel.TabIndex = 0;
            this.headerLabel.Text = "Dashboard";
            //
            // sidebarPanel
            //
            this.sidebarPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.sidebarPanel.Controls.Add(this.navFlow);
            this.sidebarPanel.Controls.Add(this.brandPanel);
            this.sidebarPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.sidebarPanel.Location = new System.Drawing.Point(0, 0);
            this.sidebarPanel.Name = "sidebarPanel";
            this.sidebarPanel.Size = new System.Drawing.Size(220, 681);
            this.sidebarPanel.TabIndex = 0;
            //
            // navFlow
            //
            this.navFlow.AutoScroll = true;
            this.navFlow.Controls.Add(this.lblGrpClient);
            this.navFlow.Controls.Add(this.btnDashboard);
            this.navFlow.Controls.Add(this.btnQueue);
            this.navFlow.Controls.Add(this.btnTransactions);
            this.navFlow.Controls.Add(this.lblGrpCertification);
            this.navFlow.Controls.Add(this.btnCertRequest);
            this.navFlow.Controls.Add(this.btnRelease);
            this.navFlow.Controls.Add(this.btnBreqs);
            this.navFlow.Controls.Add(this.btnBirth);
            this.navFlow.Controls.Add(this.btnMarriage);
            this.navFlow.Controls.Add(this.btnDeath);
            this.navFlow.Controls.Add(this.lblGrpRecord);
            this.navFlow.Controls.Add(this.btnPetitions);
            this.navFlow.Controls.Add(this.btnBooks);
            this.navFlow.Controls.Add(this.btnSearch);
            this.navFlow.Controls.Add(this.lblGrpDocument);
            this.navFlow.Controls.Add(this.btnOcr);
            this.navFlow.Controls.Add(this.lblGrpOperations);
            this.navFlow.Controls.Add(this.btnFees);
            this.navFlow.Controls.Add(this.btnReports);
            this.navFlow.Controls.Add(this.lblGrpAdmin);
            this.navFlow.Controls.Add(this.btnMaster);
            this.navFlow.Controls.Add(this.btnWindows);
            this.navFlow.Controls.Add(this.btnUsers);
            this.navFlow.Controls.Add(this.btnArchive);
            this.navFlow.Controls.Add(this.btnCertTemplates);
            this.navFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.navFlow.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.navFlow.Location = new System.Drawing.Point(0, 64);
            this.navFlow.Name = "navFlow";
            this.navFlow.Size = new System.Drawing.Size(220, 617);
            this.navFlow.TabIndex = 1;
            this.navFlow.WrapContents = false;
            //
            // lblGrpClient
            //
            this.lblGrpClient.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblGrpClient.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(154)))), ((int)(((byte)(182)))));
            this.lblGrpClient.Margin = new System.Windows.Forms.Padding(12, 10, 0, 2);
            this.lblGrpClient.Name = "lblGrpClient";
            this.lblGrpClient.Size = new System.Drawing.Size(200, 24);
            this.lblGrpClient.TabIndex = 0;
            this.lblGrpClient.Text = "CLIENT SERVICES";
            this.lblGrpClient.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            //
            // btnDashboard
            //
            this.btnDashboard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnDashboard.FlatAppearance.BorderSize = 0;
            this.btnDashboard.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnDashboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDashboard.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnDashboard.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnDashboard.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnDashboard.Name = "btnDashboard";
            this.btnDashboard.Size = new System.Drawing.Size(204, 38);
            this.btnDashboard.TabIndex = 1;
            this.btnDashboard.Tag = "dashboard";
            this.btnDashboard.Text = "   Dashboard";
            this.btnDashboard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDashboard.UseVisualStyleBackColor = false;
            this.btnDashboard.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnQueue
            //
            this.btnQueue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnQueue.FlatAppearance.BorderSize = 0;
            this.btnQueue.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnQueue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnQueue.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnQueue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnQueue.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnQueue.Name = "btnQueue";
            this.btnQueue.Size = new System.Drawing.Size(204, 38);
            this.btnQueue.TabIndex = 2;
            this.btnQueue.Tag = "queue";
            this.btnQueue.Text = "   Queue Management";
            this.btnQueue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnQueue.UseVisualStyleBackColor = false;
            this.btnQueue.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnTransactions
            //
            this.btnTransactions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnTransactions.FlatAppearance.BorderSize = 0;
            this.btnTransactions.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnTransactions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTransactions.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnTransactions.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnTransactions.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnTransactions.Name = "btnTransactions";
            this.btnTransactions.Size = new System.Drawing.Size(204, 38);
            this.btnTransactions.TabIndex = 3;
            this.btnTransactions.Tag = "transactions";
            this.btnTransactions.Text = "   Transactions";
            this.btnTransactions.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnTransactions.UseVisualStyleBackColor = false;
            this.btnTransactions.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnCertRequest
            //
            this.btnCertRequest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnCertRequest.FlatAppearance.BorderSize = 0;
            this.btnCertRequest.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnCertRequest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCertRequest.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnCertRequest.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnCertRequest.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnCertRequest.Name = "btnCertRequest";
            this.btnCertRequest.Size = new System.Drawing.Size(204, 38);
            this.btnCertRequest.TabIndex = 4;
            this.btnCertRequest.Tag = "certrequest";
            this.btnCertRequest.Text = "   Certificate Request";
            this.btnCertRequest.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCertRequest.UseVisualStyleBackColor = false;
            this.btnCertRequest.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnRelease
            //
            this.btnRelease.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnRelease.FlatAppearance.BorderSize = 0;
            this.btnRelease.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnRelease.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRelease.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnRelease.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnRelease.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnRelease.Name = "btnRelease";
            this.btnRelease.Size = new System.Drawing.Size(204, 38);
            this.btnRelease.TabIndex = 5;
            this.btnRelease.Tag = "release";
            this.btnRelease.Text = "   Release && Claim";
            this.btnRelease.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnRelease.UseVisualStyleBackColor = false;
            this.btnRelease.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnBreqs
            //
            this.btnBreqs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnBreqs.FlatAppearance.BorderSize = 0;
            this.btnBreqs.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnBreqs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBreqs.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnBreqs.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnBreqs.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnBreqs.Name = "btnBreqs";
            this.btnBreqs.Size = new System.Drawing.Size(204, 38);
            this.btnBreqs.TabIndex = 5;
            this.btnBreqs.Tag = "breqs";
            this.btnBreqs.Text = "   PSA Copies (BREQS)";
            this.btnBreqs.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnBreqs.UseVisualStyleBackColor = false;
            this.btnBreqs.Click += new System.EventHandler(this.NavButton_Click);
            //
            // lblGrpCertification
            //
            this.lblGrpCertification.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblGrpCertification.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(154)))), ((int)(((byte)(182)))));
            this.lblGrpCertification.Margin = new System.Windows.Forms.Padding(12, 10, 0, 2);
            this.lblGrpCertification.Name = "lblGrpCertification";
            this.lblGrpCertification.Size = new System.Drawing.Size(200, 24);
            this.lblGrpCertification.TabIndex = 6;
            this.lblGrpCertification.Text = "CERTIFICATION";
            this.lblGrpCertification.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            //
            // lblGrpRecord
            //
            this.lblGrpRecord.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblGrpRecord.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(154)))), ((int)(((byte)(182)))));
            this.lblGrpRecord.Margin = new System.Windows.Forms.Padding(12, 10, 0, 2);
            this.lblGrpRecord.Name = "lblGrpRecord";
            this.lblGrpRecord.Size = new System.Drawing.Size(200, 24);
            this.lblGrpRecord.TabIndex = 10;
            this.lblGrpRecord.Text = "PETITIONS & SEARCH";
            this.lblGrpRecord.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            //
            // btnBirth
            //
            this.btnBirth.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnBirth.FlatAppearance.BorderSize = 0;
            this.btnBirth.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnBirth.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBirth.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnBirth.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnBirth.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnBirth.Name = "btnBirth";
            this.btnBirth.Size = new System.Drawing.Size(204, 38);
            this.btnBirth.TabIndex = 7;
            this.btnBirth.Tag = "birth";
            this.btnBirth.Text = "   Birth Registration";
            this.btnBirth.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnBirth.UseVisualStyleBackColor = false;
            this.btnBirth.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnMarriage
            //
            this.btnMarriage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnMarriage.FlatAppearance.BorderSize = 0;
            this.btnMarriage.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnMarriage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMarriage.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnMarriage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnMarriage.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnMarriage.Name = "btnMarriage";
            this.btnMarriage.Size = new System.Drawing.Size(204, 38);
            this.btnMarriage.TabIndex = 8;
            this.btnMarriage.Tag = "marriage";
            this.btnMarriage.Text = "   Marriage Registration";
            this.btnMarriage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnMarriage.UseVisualStyleBackColor = false;
            this.btnMarriage.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnDeath
            //
            this.btnDeath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnDeath.FlatAppearance.BorderSize = 0;
            this.btnDeath.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnDeath.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeath.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnDeath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnDeath.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnDeath.Name = "btnDeath";
            this.btnDeath.Size = new System.Drawing.Size(204, 38);
            this.btnDeath.TabIndex = 9;
            this.btnDeath.Tag = "death";
            this.btnDeath.Text = "   Death Registration";
            this.btnDeath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDeath.UseVisualStyleBackColor = false;
            this.btnDeath.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnPetitions
            //
            this.btnPetitions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnPetitions.FlatAppearance.BorderSize = 0;
            this.btnPetitions.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnPetitions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPetitions.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPetitions.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnPetitions.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnPetitions.Name = "btnPetitions";
            this.btnPetitions.Size = new System.Drawing.Size(204, 38);
            this.btnPetitions.TabIndex = 11;
            this.btnPetitions.Tag = "petitions";
            this.btnPetitions.Text = "   Petitions";
            this.btnPetitions.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnPetitions.UseVisualStyleBackColor = false;
            this.btnPetitions.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnSearch
            //
            this.btnSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnSearch.FlatAppearance.BorderSize = 0;
            this.btnSearch.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSearch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnSearch.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnSearch.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(204, 38);
            this.btnSearch.TabIndex = 12;
            this.btnSearch.Tag = "search";
            this.btnSearch.Text = "   Record Search";
            this.btnSearch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSearch.UseVisualStyleBackColor = false;
            this.btnSearch.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnBooks
            //
            this.btnBooks.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnBooks.FlatAppearance.BorderSize = 0;
            this.btnBooks.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnBooks.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBooks.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnBooks.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnBooks.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnBooks.Name = "btnBooks";
            this.btnBooks.Size = new System.Drawing.Size(204, 38);
            this.btnBooks.TabIndex = 13;
            this.btnBooks.Tag = "books";
            this.btnBooks.Text = "   Registry Books";
            this.btnBooks.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnBooks.UseVisualStyleBackColor = false;
            this.btnBooks.Click += new System.EventHandler(this.NavButton_Click);
            //
            // lblGrpDocument
            //
            this.lblGrpDocument.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblGrpDocument.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(154)))), ((int)(((byte)(182)))));
            this.lblGrpDocument.Margin = new System.Windows.Forms.Padding(12, 10, 0, 2);
            this.lblGrpDocument.Name = "lblGrpDocument";
            this.lblGrpDocument.Size = new System.Drawing.Size(200, 24);
            this.lblGrpDocument.TabIndex = 13;
            this.lblGrpDocument.Text = "DOCUMENT WORKFLOW";
            this.lblGrpDocument.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            //
            // btnOcr
            //
            this.btnOcr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnOcr.FlatAppearance.BorderSize = 0;
            this.btnOcr.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnOcr.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOcr.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnOcr.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnOcr.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnOcr.Name = "btnOcr";
            this.btnOcr.Size = new System.Drawing.Size(204, 38);
            this.btnOcr.TabIndex = 15;
            this.btnOcr.Tag = "ocr";
            this.btnOcr.Text = "   Intelligent Document Processing";
            this.btnOcr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOcr.UseVisualStyleBackColor = false;
            this.btnOcr.Click += new System.EventHandler(this.NavButton_Click);
            //
            // lblGrpOperations
            //
            this.lblGrpOperations.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblGrpOperations.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(154)))), ((int)(((byte)(182)))));
            this.lblGrpOperations.Margin = new System.Windows.Forms.Padding(12, 10, 0, 2);
            this.lblGrpOperations.Name = "lblGrpOperations";
            this.lblGrpOperations.Size = new System.Drawing.Size(200, 24);
            this.lblGrpOperations.TabIndex = 16;
            this.lblGrpOperations.Text = "OPERATIONS";
            this.lblGrpOperations.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            //
            // btnFees
            //
            this.btnFees.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnFees.FlatAppearance.BorderSize = 0;
            this.btnFees.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnFees.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFees.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnFees.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnFees.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnFees.Name = "btnFees";
            this.btnFees.Size = new System.Drawing.Size(204, 38);
            this.btnFees.TabIndex = 17;
            this.btnFees.Tag = "fees";
            this.btnFees.Text = "   Fees && Payments";
            this.btnFees.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnFees.UseVisualStyleBackColor = false;
            this.btnFees.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnReports
            //
            this.btnReports.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnReports.FlatAppearance.BorderSize = 0;
            this.btnReports.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnReports.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReports.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnReports.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnReports.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnReports.Name = "btnReports";
            this.btnReports.Size = new System.Drawing.Size(204, 38);
            this.btnReports.TabIndex = 18;
            this.btnReports.Tag = "reports";
            this.btnReports.Text = "   Reports && Analytics";
            this.btnReports.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnReports.UseVisualStyleBackColor = false;
            this.btnReports.Click += new System.EventHandler(this.NavButton_Click);
            //
            // lblGrpAdmin
            //
            this.lblGrpAdmin.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblGrpAdmin.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(154)))), ((int)(((byte)(182)))));
            this.lblGrpAdmin.Margin = new System.Windows.Forms.Padding(12, 10, 0, 2);
            this.lblGrpAdmin.Name = "lblGrpAdmin";
            this.lblGrpAdmin.Size = new System.Drawing.Size(200, 24);
            this.lblGrpAdmin.TabIndex = 19;
            this.lblGrpAdmin.Text = "ADMINISTRATION";
            this.lblGrpAdmin.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            //
            // btnMaster
            //
            this.btnMaster.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnMaster.FlatAppearance.BorderSize = 0;
            this.btnMaster.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnMaster.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMaster.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnMaster.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnMaster.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnMaster.Name = "btnMaster";
            this.btnMaster.Size = new System.Drawing.Size(204, 38);
            this.btnMaster.TabIndex = 19;
            this.btnMaster.Tag = "masterfiles";
            this.btnMaster.Text = "   Master Files";
            this.btnMaster.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnMaster.UseVisualStyleBackColor = false;
            this.btnMaster.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnUsers
            //
            this.btnUsers.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnUsers.FlatAppearance.BorderSize = 0;
            this.btnUsers.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnUsers.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUsers.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnUsers.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnUsers.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnUsers.Name = "btnUsers";
            this.btnUsers.Size = new System.Drawing.Size(204, 38);
            this.btnUsers.TabIndex = 20;
            this.btnUsers.Tag = "users";
            this.btnUsers.Text = "   Users && Audit Trail";
            this.btnUsers.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnUsers.UseVisualStyleBackColor = false;
            this.btnUsers.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnArchive
            //
            this.btnArchive.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnArchive.FlatAppearance.BorderSize = 0;
            this.btnArchive.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnArchive.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnArchive.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnArchive.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnArchive.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnArchive.Name = "btnArchive";
            this.btnArchive.Size = new System.Drawing.Size(204, 38);
            this.btnArchive.TabIndex = 22;
            this.btnArchive.Tag = "archive";
            this.btnArchive.Text = "   Records Archive";
            this.btnArchive.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnArchive.UseVisualStyleBackColor = false;
            this.btnArchive.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnCertTemplates
            //
            this.btnCertTemplates.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnCertTemplates.FlatAppearance.BorderSize = 0;
            this.btnCertTemplates.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnCertTemplates.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCertTemplates.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnCertTemplates.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnCertTemplates.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnCertTemplates.Name = "btnCertTemplates";
            this.btnCertTemplates.Size = new System.Drawing.Size(204, 38);
            this.btnCertTemplates.TabIndex = 23;
            this.btnCertTemplates.Tag = "certtemplates";
            this.btnCertTemplates.Text = "   Certificate Templates";
            this.btnCertTemplates.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCertTemplates.UseVisualStyleBackColor = false;
            this.btnCertTemplates.Click += new System.EventHandler(this.NavButton_Click);
            //
            // btnWindows
            //
            this.btnWindows.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(36)))), ((int)(((byte)(65)))));
            this.btnWindows.FlatAppearance.BorderSize = 0;
            this.btnWindows.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(48)))), ((int)(((byte)(82)))));
            this.btnWindows.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnWindows.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnWindows.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(206)))), ((int)(((byte)(227)))));
            this.btnWindows.Margin = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.btnWindows.Name = "btnWindows";
            this.btnWindows.Size = new System.Drawing.Size(204, 38);
            this.btnWindows.TabIndex = 21;
            this.btnWindows.Tag = "settings";
            this.btnWindows.Text = "   Settings";
            this.btnWindows.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnWindows.UseVisualStyleBackColor = false;
            this.btnWindows.Click += new System.EventHandler(this.NavButton_Click);
            //
            // brandPanel
            //
            this.brandPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(12)))), ((int)(((byte)(24)))), ((int)(((byte)(44)))));
            this.brandPanel.Controls.Add(this.brandLabel);
            this.brandPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.brandPanel.Location = new System.Drawing.Point(0, 0);
            this.brandPanel.Name = "brandPanel";
            this.brandPanel.Size = new System.Drawing.Size(220, 64);
            this.brandPanel.TabIndex = 0;
            //
            // brandLabel
            //
            this.brandLabel.AutoSize = true;
            this.brandLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.brandLabel.ForeColor = System.Drawing.Color.White;
            this.brandLabel.Location = new System.Drawing.Point(16, 16);
            this.brandLabel.Name = "brandLabel";
            this.brandLabel.Size = new System.Drawing.Size(91, 30);
            this.brandLabel.TabIndex = 0;
            this.brandLabel.Text = "CROMS";
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1184, 681);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.sidebarPanel);
            this.MinimumSize = new System.Drawing.Size(1200, 720);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CROMS — Civil Registry Operations Management System";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.mainPanel.ResumeLayout(false);
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            this.sidebarPanel.ResumeLayout(false);
            this.navFlow.ResumeLayout(false);
            this.brandPanel.ResumeLayout(false);
            this.brandPanel.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.Panel contentPanel;
        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.Label headerLabel;
        private System.Windows.Forms.Panel sidebarPanel;
        private System.Windows.Forms.FlowLayoutPanel navFlow;
        private System.Windows.Forms.Label lblGrpClient;
        private System.Windows.Forms.Button btnDashboard;
        private System.Windows.Forms.Button btnQueue;
        private System.Windows.Forms.Button btnTransactions;
        private System.Windows.Forms.Button btnCertRequest;
        private System.Windows.Forms.Button btnRelease;
        private System.Windows.Forms.Button btnBreqs;
        private System.Windows.Forms.Label lblGrpCertification;
        private System.Windows.Forms.Label lblGrpRecord;
        private System.Windows.Forms.Button btnBirth;
        private System.Windows.Forms.Button btnMarriage;
        private System.Windows.Forms.Button btnDeath;
        private System.Windows.Forms.Button btnPetitions;
        private System.Windows.Forms.Button btnBooks;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Label lblGrpDocument;
        private System.Windows.Forms.Button btnOcr;
        private System.Windows.Forms.Label lblGrpOperations;
        private System.Windows.Forms.Button btnFees;
        private System.Windows.Forms.Button btnReports;
        private System.Windows.Forms.Label lblGrpAdmin;
        private System.Windows.Forms.Button btnMaster;
        private System.Windows.Forms.Button btnUsers;
        private System.Windows.Forms.Button btnArchive;
        private System.Windows.Forms.Button btnCertTemplates;
        private System.Windows.Forms.Button btnWindows;
        private System.Windows.Forms.Panel brandPanel;
        private System.Windows.Forms.Label brandLabel;
    }
}
