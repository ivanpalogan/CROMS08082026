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
        private System.Windows.Forms.DataGridView grid;

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
            this.grid = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
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
            this.lblSubtitle.Text = "Every saved record, form and image in the system, grouped by type. Admin only, " +
    "read-only.";
            //
            // treeCategories
            //
            this.treeCategories.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left);
            this.treeCategories.Font = new System.Drawing.Font("Segoe UI", 9.75F);
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
            this.lblCount.AutoSize = true;
            this.lblCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCount.Location = new System.Drawing.Point(1105, 93);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(70, 15);
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
            //
            // RecordsArchiveForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1449, 845);
            this.Controls.Add(this.grid);
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
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
