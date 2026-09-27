namespace CROMS.Forms
{
    partial class RegistryBooksForm
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
        /// </summary>
        private void InitializeComponent()
        {
            this.titleLabel = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.pnlGallery = new System.Windows.Forms.Panel();
            this.dgvBooks = new System.Windows.Forms.DataGridView();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.lblBooksHeader = new System.Windows.Forms.Label();
            this.pnlBook = new System.Windows.Forms.Panel();
            this.dgvOpenedRecords = new System.Windows.Forms.DataGridView();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.lblBookMeta = new System.Windows.Forms.Label();
            this.lblBookHeader = new System.Windows.Forms.Label();
            this.btnBack = new System.Windows.Forms.Button();
            this.pnlGallery.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBooks)).BeginInit();
            this.pnlBook.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOpenedRecords)).BeginInit();
            this.SuspendLayout();
            //
            // titleLabel
            //
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.titleLabel.Location = new System.Drawing.Point(32, 24);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(200, 37);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Registry Books";
            this.titleLabel.UseMnemonic = false;
            //
            // lblSubtitle
            //
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblSubtitle.Location = new System.Drawing.Point(34, 66);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(460, 19);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "The office\'s paper registry books, by year (volume) and page - as staff typed th" +
    "em on Birth, Marriage and Death Registration.";
            //
            // pnlGallery
            //
            this.pnlGallery.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlGallery.BackColor = System.Drawing.Color.White;
            this.pnlGallery.Controls.Add(this.dgvBooks);
            this.pnlGallery.Controls.Add(this.btnRefresh);
            this.pnlGallery.Controls.Add(this.lblBooksHeader);
            this.pnlGallery.Location = new System.Drawing.Point(0, 98);
            this.pnlGallery.Name = "pnlGallery";
            this.pnlGallery.Size = new System.Drawing.Size(900, 522);
            this.pnlGallery.TabIndex = 2;
            //
            // dgvBooks
            //
            this.dgvBooks.AllowUserToAddRows = false;
            this.dgvBooks.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvBooks.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvBooks.BackgroundColor = System.Drawing.Color.White;
            this.dgvBooks.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvBooks.Location = new System.Drawing.Point(34, 30);
            this.dgvBooks.Name = "dgvBooks";
            this.dgvBooks.ReadOnly = true;
            this.dgvBooks.RowHeadersVisible = false;
            this.dgvBooks.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvBooks.Size = new System.Drawing.Size(832, 480);
            this.dgvBooks.TabIndex = 2;
            this.dgvBooks.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvBooks_CellClick);
            //
            // btnRefresh
            //
            this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnRefresh.Location = new System.Drawing.Point(766, 0);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(100, 30);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "⟳ Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            //
            // lblBooksHeader
            //
            this.lblBooksHeader.AutoSize = true;
            this.lblBooksHeader.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblBooksHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblBooksHeader.Location = new System.Drawing.Point(34, 6);
            this.lblBooksHeader.Name = "lblBooksHeader";
            this.lblBooksHeader.Size = new System.Drawing.Size(93, 17);
            this.lblBooksHeader.TabIndex = 0;
            this.lblBooksHeader.Text = "BOOKS ON FILE";
            //
            // pnlBook
            //
            this.pnlBook.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlBook.BackColor = System.Drawing.Color.White;
            this.pnlBook.Controls.Add(this.dgvOpenedRecords);
            this.pnlBook.Controls.Add(this.txtSearch);
            this.pnlBook.Controls.Add(this.lblSearch);
            this.pnlBook.Controls.Add(this.lblBookMeta);
            this.pnlBook.Controls.Add(this.lblBookHeader);
            this.pnlBook.Controls.Add(this.btnBack);
            this.pnlBook.Location = new System.Drawing.Point(0, 98);
            this.pnlBook.Name = "pnlBook";
            this.pnlBook.Size = new System.Drawing.Size(900, 522);
            this.pnlBook.TabIndex = 3;
            this.pnlBook.Visible = false;
            //
            // btnBack
            //
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnBack.Location = new System.Drawing.Point(34, 0);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(180, 30);
            this.btnBack.TabIndex = 0;
            this.btnBack.Text = "← Back to Registry Books";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            //
            // lblBookHeader
            //
            this.lblBookHeader.AutoSize = true;
            this.lblBookHeader.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblBookHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblBookHeader.Location = new System.Drawing.Point(34, 40);
            this.lblBookHeader.Name = "lblBookHeader";
            this.lblBookHeader.Size = new System.Drawing.Size(200, 26);
            this.lblBookHeader.TabIndex = 1;
            this.lblBookHeader.Text = "Registry Book";
            this.lblBookHeader.UseMnemonic = false;
            //
            // lblBookMeta
            //
            this.lblBookMeta.AutoSize = true;
            this.lblBookMeta.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblBookMeta.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblBookMeta.Location = new System.Drawing.Point(34, 70);
            this.lblBookMeta.Name = "lblBookMeta";
            this.lblBookMeta.Size = new System.Drawing.Size(200, 17);
            this.lblBookMeta.TabIndex = 2;
            this.lblBookMeta.Text = "Year: —   •   0 records";
            //
            // lblSearch
            //
            this.lblSearch.AutoSize = true;
            this.lblSearch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSearch.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblSearch.Location = new System.Drawing.Point(34, 104);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(50, 15);
            this.lblSearch.TabIndex = 3;
            this.lblSearch.Text = "Search:";
            //
            // txtSearch
            //
            this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtSearch.Location = new System.Drawing.Point(566, 100);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(300, 23);
            this.txtSearch.TabIndex = 4;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            //
            // dgvOpenedRecords
            //
            this.dgvOpenedRecords.AllowUserToAddRows = false;
            this.dgvOpenedRecords.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvOpenedRecords.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvOpenedRecords.BackgroundColor = System.Drawing.Color.White;
            this.dgvOpenedRecords.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvOpenedRecords.Location = new System.Drawing.Point(34, 132);
            this.dgvOpenedRecords.Name = "dgvOpenedRecords";
            this.dgvOpenedRecords.ReadOnly = true;
            this.dgvOpenedRecords.RowHeadersVisible = false;
            this.dgvOpenedRecords.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvOpenedRecords.Size = new System.Drawing.Size(832, 378);
            this.dgvOpenedRecords.TabIndex = 5;
            this.dgvOpenedRecords.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvOpenedRecords_CellClick);
            this.dgvOpenedRecords.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvOpenedRecords_CellDoubleClick);
            //
            // RegistryBooksForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(900, 620);
            this.Controls.Add(this.pnlBook);
            this.Controls.Add(this.pnlGallery);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.titleLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "RegistryBooksForm";
            this.Text = "Registry Books";
            this.pnlGallery.ResumeLayout(false);
            this.pnlGallery.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBooks)).EndInit();
            this.pnlBook.ResumeLayout(false);
            this.pnlBook.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOpenedRecords)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Panel pnlGallery;
        private System.Windows.Forms.Label lblBooksHeader;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.DataGridView dgvBooks;
        private System.Windows.Forms.Panel pnlBook;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Label lblBookHeader;
        private System.Windows.Forms.Label lblBookMeta;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.DataGridView dgvOpenedRecords;
    }
}
