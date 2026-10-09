namespace CROMS.Forms
{
    partial class RecordsArchiveForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.TreeView treeCategories;
        private System.Windows.Forms.Label lblCategoryTitle;
        private System.Windows.Forms.Label lblCount;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnViewRecord;
        private System.Windows.Forms.Button btnViewCertificate;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.Panel pnlSearchBar;
        private System.Windows.Forms.TextBox txtQuery;
        private System.Windows.Forms.ComboBox cboSearchType;
        private System.Windows.Forms.CheckBox chkFuzzy;
        private System.Windows.Forms.Panel cardDetail;
        private System.Windows.Forms.Panel pnlWorkbench;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.treeCategories = new System.Windows.Forms.TreeView();
            this.lblCategoryTitle = new System.Windows.Forms.Label();
            this.lblCount = new System.Windows.Forms.Label();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnViewRecord = new System.Windows.Forms.Button();
            this.btnViewCertificate = new System.Windows.Forms.Button();
            this.grid = new System.Windows.Forms.DataGridView();
            this.pnlSearchBar = new System.Windows.Forms.Panel();
            this.txtQuery = new System.Windows.Forms.TextBox();
            this.cboSearchType = new System.Windows.Forms.ComboBox();
            this.chkFuzzy = new System.Windows.Forms.CheckBox();
            this.cardDetail = new System.Windows.Forms.Panel();
            this.pnlWorkbench = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.pnlSearchBar.SuspendLayout();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(24, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(260, 30);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Records Archive";
            //
            // lblSubtitle
            //
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblSubtitle.Location = new System.Drawing.Point(27, 54);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(560, 15);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Every saved record, form and image in the system, grouped by type, plus a name" +
    "/SOUNDEX search across births, marriages and deaths. Read-only.";
            //
            // treeCategories
            //
            this.treeCategories.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left);
            this.treeCategories.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.treeCategories.ShowNodeToolTips = true;   // the long category names are cut off on a narrow screen
            this.treeCategories.FullRowSelect = true;
            this.treeCategories.HideSelection = false;
            this.treeCategories.ItemHeight = 26;
            this.treeCategories.Location = new System.Drawing.Point(24, 88);
            this.treeCategories.Name = "treeCategories";
            this.treeCategories.Size = new System.Drawing.Size(300, 690);
            this.treeCategories.TabIndex = 2;
            this.treeCategories.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeCategories_AfterSelect);
            //
            // lblCategoryTitle
            //
            this.lblCategoryTitle.AutoSize = true;
            this.lblCategoryTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblCategoryTitle.Location = new System.Drawing.Point(340, 88);
            this.lblCategoryTitle.Name = "lblCategoryTitle";
            this.lblCategoryTitle.Size = new System.Drawing.Size(220, 21);
            this.lblCategoryTitle.TabIndex = 3;
            this.lblCategoryTitle.Text = "Select a category on the left";
            //
            // lblCount
            //
            this.lblCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            // Right-aligned in a fixed box that ends before Refresh: as a growing AutoSize label the long
            // search caption ran under the Refresh button and off the edge of the window.
            this.lblCount.AutoSize = false;
            this.lblCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCount.Location = new System.Drawing.Point(809, 87);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(460, 20);
            this.lblCount.TabIndex = 4;
            this.lblCount.Text = "0 record(s)";
            //
            // btnRefresh
            //
            this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Location = new System.Drawing.Point(1281, 82);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(90, 30);
            this.btnRefresh.TabIndex = 5;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            //
            // btnViewRecord
            //
            this.btnViewRecord.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnViewRecord.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this.btnViewRecord.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnViewRecord.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnViewRecord.ForeColor = System.Drawing.Color.White;
            this.btnViewRecord.Location = new System.Drawing.Point(340, 792);
            this.btnViewRecord.Name = "btnViewRecord";
            this.btnViewRecord.Size = new System.Drawing.Size(230, 40);
            this.btnViewRecord.TabIndex = 6;
            this.btnViewRecord.Text = "View Full Record";
            this.btnViewRecord.UseVisualStyleBackColor = false;
            this.btnViewRecord.Click += new System.EventHandler(this.btnViewRecord_Click);
            //
            // btnViewCertificate
            //
            this.btnViewCertificate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnViewCertificate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnViewCertificate.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnViewCertificate.Location = new System.Drawing.Point(580, 792);
            this.btnViewCertificate.Name = "btnViewCertificate";
            this.btnViewCertificate.Size = new System.Drawing.Size(200, 40);
            this.btnViewCertificate.TabIndex = 7;
            this.btnViewCertificate.Text = "View Certificate";
            this.btnViewCertificate.UseVisualStyleBackColor = true;
            this.btnViewCertificate.Visible = false;
            this.btnViewCertificate.Click += new System.EventHandler(this.btnViewCertificate_Click);
            //
            // grid
            //
            this.grid.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.Location = new System.Drawing.Point(340, 115);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(1085, 665);
            this.grid.TabIndex = 7;
            this.grid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.grid_CellDoubleClick);
            this.grid.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.grid_CellFormatting);
            this.grid.SelectionChanged += new System.EventHandler(this.grid_SelectionChanged);
            //
            // pnlSearchBar
            //
            this.pnlSearchBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.pnlSearchBar.Controls.Add(this.txtQuery);
            this.pnlSearchBar.Controls.Add(this.cboSearchType);
            this.pnlSearchBar.Controls.Add(this.chkFuzzy);
            this.pnlSearchBar.Location = new System.Drawing.Point(340, 114);
            this.pnlSearchBar.Name = "pnlSearchBar";
            this.pnlSearchBar.Size = new System.Drawing.Size(720, 32);
            this.pnlSearchBar.TabIndex = 8;
            this.pnlSearchBar.Visible = false;
            //
            // txtQuery
            //
            this.txtQuery.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtQuery.Location = new System.Drawing.Point(0, 3);
            this.txtQuery.Name = "txtQuery";
            this.txtQuery.Size = new System.Drawing.Size(300, 25);
            this.txtQuery.TabIndex = 0;
            this.txtQuery.TextChanged += new System.EventHandler(this.txtQuery_TextChanged);
            //
            // cboSearchType
            //
            this.cboSearchType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSearchType.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cboSearchType.Items.AddRange(new object[] { "All Records", "Birth", "Marriage", "Death" });
            this.cboSearchType.Location = new System.Drawing.Point(310, 2);
            this.cboSearchType.Name = "cboSearchType";
            this.cboSearchType.Size = new System.Drawing.Size(150, 25);
            this.cboSearchType.TabIndex = 1;
            this.cboSearchType.SelectedIndexChanged += new System.EventHandler(this.cboSearchType_SelectedIndexChanged);
            //
            // chkFuzzy
            //
            this.chkFuzzy.AutoSize = true;
            this.chkFuzzy.Location = new System.Drawing.Point(474, 6);
            this.chkFuzzy.Name = "chkFuzzy";
            this.chkFuzzy.Size = new System.Drawing.Size(160, 19);
            this.chkFuzzy.TabIndex = 2;
            this.chkFuzzy.Text = "Sound-alike match (SOUNDEX)";
            this.chkFuzzy.UseVisualStyleBackColor = true;
            this.chkFuzzy.CheckedChanged += new System.EventHandler(this.chkFuzzy_CheckedChanged);
            //
            // cardDetail
            //
            this.cardDetail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cardDetail.AutoScroll = true;
            this.cardDetail.BackColor = System.Drawing.Color.White;
            this.cardDetail.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardDetail.Location = new System.Drawing.Point(1095, 150);
            this.cardDetail.Name = "cardDetail";
            this.cardDetail.Padding = new System.Windows.Forms.Padding(14);
            this.cardDetail.Size = new System.Drawing.Size(330, 630);
            this.cardDetail.TabIndex = 9;
            this.cardDetail.Visible = false;
            //
            // pnlWorkbench
            //
            // Hosts a full Add/Edit/View/Delete workbench form (Old Birth/Marriage/Death
            // Records) embedded exactly like MainForm embeds a module — same bounds as
            // `grid`, kept in sync by ApplyBounds() and shown instead of it while a
            // workbench category is selected in the tree.
            this.pnlWorkbench.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
            this.pnlWorkbench.Location = new System.Drawing.Point(340, 115);
            this.pnlWorkbench.Name = "pnlWorkbench";
            this.pnlWorkbench.Size = new System.Drawing.Size(1085, 665);
            this.pnlWorkbench.TabIndex = 10;
            this.pnlWorkbench.Visible = false;
            //
            // RecordsArchiveForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1449, 845);
            this.Controls.Add(this.pnlWorkbench);
            this.Controls.Add(this.cardDetail);
            this.Controls.Add(this.grid);
            this.Controls.Add(this.pnlSearchBar);
            this.Controls.Add(this.btnViewCertificate);
            this.Controls.Add(this.btnViewRecord);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.lblCategoryTitle);
            this.Controls.Add(this.treeCategories);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblTitle);
            this.Name = "RecordsArchiveForm";
            this.Text = "Records Archive";
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.pnlSearchBar.ResumeLayout(false);
            this.pnlSearchBar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
