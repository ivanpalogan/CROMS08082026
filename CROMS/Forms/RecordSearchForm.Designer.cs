namespace CROMS.Forms
{
    partial class RecordSearchForm
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        ///
        /// Layout: the header and the result count stay at fixed points (they are top-left
        /// anchored and nothing below them depends on their width), while the FILTER ROW and
        /// the BODY are TableLayoutPanels anchored to every edge they need, so the screen fills
        /// a 1920 monitor and still reaches every control on a 1366 one. The body is two
        /// columns: the results grid takes whatever width is left, and the record detail rail
        /// (registry book information for the selected row) is a fixed 340px card on the right.
        /// </summary>
        private void InitializeComponent()
        {
            this.titleLabel = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.pnlFilters = new System.Windows.Forms.TableLayoutPanel();
            this.lblCapQuery = new System.Windows.Forms.Label();
            this.lblCapType = new System.Windows.Forms.Label();
            this.lblCapFuzzy = new System.Windows.Forms.Label();
            this.txtQuery = new System.Windows.Forms.TextBox();
            this.cboType = new System.Windows.Forms.ComboBox();
            this.chkFuzzy = new System.Windows.Forms.CheckBox();
            this.lblCount = new System.Windows.Forms.Label();
            this.bodyLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grid = new System.Windows.Forms.DataGridView();
            this.cardDetail = new CROMS.Modules.CardPanel();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.pnlFilters.SuspendLayout();
            this.bodyLayout.SuspendLayout();
            this.SuspendLayout();
            //
            // titleLabel
            //
            this.titleLabel.AutoSize = false;
            this.titleLabel.BackColor = System.Drawing.Color.Transparent;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.titleLabel.Location = new System.Drawing.Point(32, 24);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(420, 38);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Record Search";
            this.titleLabel.UseMnemonic = false;
            //
            // lblSubtitle
            //
            this.lblSubtitle.AutoSize = false;
            this.lblSubtitle.BackColor = System.Drawing.Color.Transparent;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblSubtitle.Location = new System.Drawing.Point(34, 62);
            this.lblSubtitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(900, 20);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "One place to find every civil registry record - birth, marriage and death - with the registry book it was written into.";
            this.lblSubtitle.UseMnemonic = false;
            //
            // pnlFilters
            //
            this.pnlFilters.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlFilters.BackColor = System.Drawing.Color.Transparent;
            this.pnlFilters.ColumnCount = 3;
            this.pnlFilters.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlFilters.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 190F));
            this.pnlFilters.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.pnlFilters.Controls.Add(this.lblCapQuery, 0, 0);
            this.pnlFilters.Controls.Add(this.lblCapType, 1, 0);
            this.pnlFilters.Controls.Add(this.lblCapFuzzy, 2, 0);
            this.pnlFilters.Controls.Add(this.txtQuery, 0, 1);
            this.pnlFilters.Controls.Add(this.cboType, 1, 1);
            this.pnlFilters.Controls.Add(this.chkFuzzy, 2, 1);
            this.pnlFilters.Location = new System.Drawing.Point(34, 94);
            this.pnlFilters.Name = "pnlFilters";
            this.pnlFilters.RowCount = 2;
            this.pnlFilters.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.pnlFilters.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.pnlFilters.Size = new System.Drawing.Size(1012, 52);
            this.pnlFilters.TabIndex = 2;
            //
            // lblCapQuery
            //
            this.lblCapQuery.AutoSize = true;
            this.lblCapQuery.BackColor = System.Drawing.Color.Transparent;
            this.lblCapQuery.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblCapQuery.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCapQuery.Margin = new System.Windows.Forms.Padding(0, 2, 3, 0);
            this.lblCapQuery.Name = "lblCapQuery";
            this.lblCapQuery.Size = new System.Drawing.Size(110, 13);
            this.lblCapQuery.TabIndex = 0;
            this.lblCapQuery.Text = "SEARCH BY NAME";
            this.lblCapQuery.UseMnemonic = false;
            //
            // lblCapType
            //
            this.lblCapType.AutoSize = true;
            this.lblCapType.BackColor = System.Drawing.Color.Transparent;
            this.lblCapType.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblCapType.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCapType.Margin = new System.Windows.Forms.Padding(0, 2, 3, 0);
            this.lblCapType.Name = "lblCapType";
            this.lblCapType.Size = new System.Drawing.Size(110, 13);
            this.lblCapType.TabIndex = 1;
            this.lblCapType.Text = "DOCUMENT TYPE";
            this.lblCapType.UseMnemonic = false;
            //
            // lblCapFuzzy
            //
            this.lblCapFuzzy.AutoSize = true;
            this.lblCapFuzzy.BackColor = System.Drawing.Color.Transparent;
            this.lblCapFuzzy.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblCapFuzzy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCapFuzzy.Margin = new System.Windows.Forms.Padding(0, 2, 3, 0);
            this.lblCapFuzzy.Name = "lblCapFuzzy";
            this.lblCapFuzzy.Size = new System.Drawing.Size(110, 13);
            this.lblCapFuzzy.TabIndex = 2;
            this.lblCapFuzzy.Text = "SPELLING";
            this.lblCapFuzzy.UseMnemonic = false;
            //
            // txtQuery
            //
            this.txtQuery.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtQuery.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtQuery.Margin = new System.Windows.Forms.Padding(0, 0, 14, 0);
            this.txtQuery.Name = "txtQuery";
            this.txtQuery.Size = new System.Drawing.Size(628, 27);
            this.txtQuery.TabIndex = 3;
            this.txtQuery.TextChanged += new System.EventHandler(this.txtQuery_TextChanged);
            //
            // cboType
            //
            this.cboType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboType.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cboType.FormattingEnabled = true;
            this.cboType.Items.AddRange(new object[] {
            "All Records",
            "Birth",
            "Marriage",
            "Death"});
            this.cboType.Margin = new System.Windows.Forms.Padding(0, 0, 14, 0);
            this.cboType.Name = "cboType";
            this.cboType.Size = new System.Drawing.Size(176, 25);
            this.cboType.TabIndex = 4;
            this.cboType.SelectedIndexChanged += new System.EventHandler(this.cboType_SelectedIndexChanged);
            //
            // chkFuzzy
            //
            this.chkFuzzy.AutoSize = true;
            this.chkFuzzy.BackColor = System.Drawing.Color.Transparent;
            this.chkFuzzy.Checked = true;
            this.chkFuzzy.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFuzzy.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkFuzzy.Margin = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.chkFuzzy.Name = "chkFuzzy";
            this.chkFuzzy.Size = new System.Drawing.Size(150, 23);
            this.chkFuzzy.TabIndex = 5;
            this.chkFuzzy.Text = "Match sound-alikes";
            this.chkFuzzy.UseMnemonic = false;
            this.chkFuzzy.UseVisualStyleBackColor = false;
            this.chkFuzzy.CheckedChanged += new System.EventHandler(this.chkFuzzy_CheckedChanged);
            //
            // lblCount
            //
            this.lblCount.AutoSize = false;
            this.lblCount.BackColor = System.Drawing.Color.Transparent;
            this.lblCount.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this.lblCount.Location = new System.Drawing.Point(34, 154);
            this.lblCount.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(940, 18);
            this.lblCount.TabIndex = 6;
            this.lblCount.UseMnemonic = false;
            //
            // bodyLayout
            //
            this.bodyLayout.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.bodyLayout.BackColor = System.Drawing.Color.Transparent;
            this.bodyLayout.ColumnCount = 2;
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 340F));
            this.bodyLayout.Controls.Add(this.grid, 0, 0);
            this.bodyLayout.Controls.Add(this.cardDetail, 1, 0);
            this.bodyLayout.Location = new System.Drawing.Point(34, 180);
            this.bodyLayout.Name = "bodyLayout";
            this.bodyLayout.RowCount = 1;
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bodyLayout.Size = new System.Drawing.Size(1012, 442);
            this.bodyLayout.TabIndex = 7;
            //
            // grid
            //
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Margin = new System.Windows.Forms.Padding(0, 0, 16, 0);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(656, 442);
            this.grid.TabIndex = 0;
            this.grid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.grid_CellDoubleClick);
            this.grid.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.grid_CellFormatting);
            this.grid.SelectionChanged += new System.EventHandler(this.grid_SelectionChanged);
            //
            // cardDetail
            //
            this.cardDetail.AutoScroll = true;
            this.cardDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardDetail.Margin = new System.Windows.Forms.Padding(0);
            this.cardDetail.Name = "cardDetail";
            this.cardDetail.Padding = new System.Windows.Forms.Padding(18, 16, 18, 16);
            this.cardDetail.Size = new System.Drawing.Size(340, 442);
            this.cardDetail.TabIndex = 1;
            //
            // RecordSearchForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // A Dock=Fill child SHRINKS and contributes nothing to an AutoScroll host's scroll
            // extent, so the floor has to be stated on the FORM. Below this the screen scrolls
            // instead of crushing the grid or putting the detail rail out of reach.
            this.AutoScrollMinSize = new System.Drawing.Size(980, 560);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(1080, 648);
            this.Controls.Add(this.bodyLayout);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.pnlFilters);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.titleLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "RecordSearchForm";
            this.Text = "Record Search";
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.pnlFilters.ResumeLayout(false);
            this.pnlFilters.PerformLayout();
            this.bodyLayout.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.TableLayoutPanel pnlFilters;
        private System.Windows.Forms.Label lblCapQuery;
        private System.Windows.Forms.Label lblCapType;
        private System.Windows.Forms.Label lblCapFuzzy;
        private System.Windows.Forms.TextBox txtQuery;
        private System.Windows.Forms.ComboBox cboType;
        private System.Windows.Forms.CheckBox chkFuzzy;
        private System.Windows.Forms.Label lblCount;
        private System.Windows.Forms.TableLayoutPanel bodyLayout;
        private System.Windows.Forms.DataGridView grid;
        private CROMS.Modules.CardPanel cardDetail;
    }
}
