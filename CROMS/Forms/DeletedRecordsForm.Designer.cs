namespace CROMS.Forms
{
    partial class DeletedRecordsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblHint = new System.Windows.Forms.Label();
            this.chkRestored = new System.Windows.Forms.CheckBox();
            this.dgvDeleted = new System.Windows.Forms.DataGridView();
            this.lblCount = new System.Windows.Forms.Label();
            this.btnRestore = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDeleted)).BeginInit();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(23, 27, 36);
            this.lblTitle.Location = new System.Drawing.Point(22, 16);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Text = "Deleted Records";
            //
            // lblHint
            //
            this.lblHint.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblHint.ForeColor = System.Drawing.Color.FromArgb(91, 100, 114);
            this.lblHint.Location = new System.Drawing.Point(24, 48);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(992, 36);
            this.lblHint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHint.UseMnemonic = false;
            this.lblHint.Text = "A deleted birth, death or marriage is kept here with who removed it, when and why. " +
                "Restore puts it back exactly as it was, with its original record and registry number.";
            //
            // chkRestored
            //
            this.chkRestored.AutoSize = true;
            this.chkRestored.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.chkRestored.Location = new System.Drawing.Point(24, 90);
            this.chkRestored.Name = "chkRestored";
            this.chkRestored.Text = "Also show records that were already restored";
            this.chkRestored.UseVisualStyleBackColor = true;
            this.chkRestored.CheckedChanged += new System.EventHandler(this.chkRestored_CheckedChanged);
            //
            // dgvDeleted
            //
            this.dgvDeleted.AllowUserToAddRows = false;
            this.dgvDeleted.AllowUserToDeleteRows = false;
            this.dgvDeleted.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvDeleted.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvDeleted.BackgroundColor = System.Drawing.Color.White;
            this.dgvDeleted.Location = new System.Drawing.Point(24, 120);
            this.dgvDeleted.MultiSelect = false;
            this.dgvDeleted.Name = "dgvDeleted";
            this.dgvDeleted.ReadOnly = true;
            this.dgvDeleted.RowHeadersVisible = false;
            this.dgvDeleted.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvDeleted.Size = new System.Drawing.Size(992, 380);
            this.dgvDeleted.TabIndex = 1;
            this.dgvDeleted.SelectionChanged += new System.EventHandler(this.dgvDeleted_SelectionChanged);
            //
            // lblCount
            //
            this.lblCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCount.AutoSize = true;
            this.lblCount.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCount.ForeColor = System.Drawing.Color.FromArgb(91, 100, 114);
            this.lblCount.Location = new System.Drawing.Point(24, 522);
            this.lblCount.Name = "lblCount";
            //
            // btnRestore
            //
            this.btnRestore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRestore.BackColor = System.Drawing.Color.FromArgb(29, 78, 216);
            this.btnRestore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRestore.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnRestore.ForeColor = System.Drawing.Color.White;
            this.btnRestore.Location = new System.Drawing.Point(752, 514);
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.Size = new System.Drawing.Size(150, 38);
            this.btnRestore.TabIndex = 2;
            this.btnRestore.Text = "Restore Selected";
            this.btnRestore.UseVisualStyleBackColor = false;
            this.btnRestore.Click += new System.EventHandler(this.btnRestore_Click);
            //
            // btnClose
            //
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnClose.Location = new System.Drawing.Point(912, 514);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 38);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            //
            // DeletedRecordsForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(1040, 570);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblHint);
            this.Controls.Add(this.chkRestored);
            this.Controls.Add(this.dgvDeleted);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.btnRestore);
            this.Controls.Add(this.btnClose);
            this.MinimumSize = new System.Drawing.Size(760, 420);
            this.Name = "DeletedRecordsForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Deleted Records";
            ((System.ComponentModel.ISupportInitialize)(this.dgvDeleted)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.CheckBox chkRestored;
        private System.Windows.Forms.DataGridView dgvDeleted;
        private System.Windows.Forms.Label lblCount;
        private System.Windows.Forms.Button btnRestore;
        private System.Windows.Forms.Button btnClose;
    }
}
