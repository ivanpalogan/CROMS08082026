namespace CROMS.Forms
{
    partial class SettingsForm
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
            this.tabs = new System.Windows.Forms.TabControl();
            this.tabWindows = new System.Windows.Forms.TabPage();
            this.dgvWindows = new System.Windows.Forms.DataGridView();
            this.bar = new System.Windows.Forms.FlowLayoutPanel();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnEnable = new System.Windows.Forms.Button();
            this.btnDisable = new System.Windows.Forms.Button();
            this.btnUp = new System.Windows.Forms.Button();
            this.btnDown = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnEnableAll = new System.Windows.Forms.Button();
            this.btnDisableAll = new System.Windows.Forms.Button();
            this.lblWinSub = new System.Windows.Forms.Label();
            this.btnUnlock = new System.Windows.Forms.Button();
            this.tabManual = new System.Windows.Forms.TabPage();
            this.rtbContent = new System.Windows.Forms.RichTextBox();
            this.lstTopics = new System.Windows.Forms.ListBox();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.tabs.SuspendLayout();
            this.tabWindows.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvWindows)).BeginInit();
            this.bar.SuspendLayout();
            this.tabManual.SuspendLayout();
            this.SuspendLayout();
            //
            // tabs
            //
            this.tabs.Controls.Add(this.tabWindows);
            this.tabs.Controls.Add(this.tabManual);
            this.tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabs.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tabs.Location = new System.Drawing.Point(0, 0);
            this.tabs.Name = "tabs";
            this.tabs.Padding = new System.Drawing.Point(16, 6);
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(1120, 720);
            this.tabs.TabIndex = 0;
            //
            // tabWindows
            //
            this.tabWindows.BackColor = System.Drawing.Color.White;
            this.tabWindows.Controls.Add(this.dgvWindows);
            this.tabWindows.Controls.Add(this.bar);
            this.tabWindows.Controls.Add(this.btnUnlock);
            this.tabWindows.Controls.Add(this.lblWinSub);
            this.tabWindows.Location = new System.Drawing.Point(4, 32);
            this.tabWindows.Name = "tabWindows";
            this.tabWindows.Padding = new System.Windows.Forms.Padding(3);
            this.tabWindows.Size = new System.Drawing.Size(1112, 684);
            this.tabWindows.TabIndex = 0;
            this.tabWindows.Text = "Window Management";
            //
            // dgvWindows
            //
            this.dgvWindows.AllowUserToAddRows = false;
            this.dgvWindows.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvWindows.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvWindows.BackgroundColor = System.Drawing.Color.White;
            this.dgvWindows.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvWindows.Location = new System.Drawing.Point(20, 108);
            this.dgvWindows.Name = "dgvWindows";
            this.dgvWindows.ReadOnly = true;
            this.dgvWindows.RowHeadersVisible = false;
            this.dgvWindows.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvWindows.Size = new System.Drawing.Size(1072, 556);
            this.dgvWindows.TabIndex = 2;
            //
            // bar
            //
            this.bar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.bar.Controls.Add(this.btnAdd);
            this.bar.Controls.Add(this.btnEdit);
            this.bar.Controls.Add(this.btnEnable);
            this.bar.Controls.Add(this.btnDisable);
            this.bar.Controls.Add(this.btnEnableAll);
            this.bar.Controls.Add(this.btnDisableAll);
            this.bar.Controls.Add(this.btnUp);
            this.bar.Controls.Add(this.btnDown);
            this.bar.Controls.Add(this.btnDelete);
            this.bar.Location = new System.Drawing.Point(20, 56);
            this.bar.Name = "bar";
            this.bar.Size = new System.Drawing.Size(1072, 46);
            this.bar.TabIndex = 1;
            //
            // btnAdd
            //
            this.btnAdd.AutoSize = true;
            this.btnAdd.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.btnAdd.FlatAppearance.BorderSize = 0;
            this.btnAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdd.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAdd.ForeColor = System.Drawing.Color.White;
            this.btnAdd.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnAdd.Size = new System.Drawing.Size(120, 38);
            this.btnAdd.TabIndex = 0;
            this.btnAdd.Text = "Add Window";
            this.btnAdd.UseVisualStyleBackColor = false;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            //
            // btnEdit
            //
            this.btnEdit.AutoSize = true;
            this.btnEdit.BackColor = System.Drawing.Color.White;
            this.btnEdit.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(206)))), ((int)(((byte)(212)))), ((int)(((byte)(218)))));
            this.btnEdit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEdit.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnEdit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.btnEdit.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnEdit.Size = new System.Drawing.Size(90, 38);
            this.btnEdit.TabIndex = 1;
            this.btnEdit.Text = "Edit Window";
            this.btnEdit.UseVisualStyleBackColor = false;
            this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
            //
            // btnEnable
            //
            this.btnEnable.AutoSize = true;
            this.btnEnable.BackColor = System.Drawing.Color.White;
            this.btnEnable.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(206)))), ((int)(((byte)(212)))), ((int)(((byte)(218)))));
            this.btnEnable.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEnable.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnEnable.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.btnEnable.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnEnable.Name = "btnEnable";
            this.btnEnable.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnEnable.Size = new System.Drawing.Size(70, 38);
            this.btnEnable.TabIndex = 2;
            this.btnEnable.Text = "Enable";
            this.btnEnable.UseVisualStyleBackColor = false;
            this.btnEnable.Click += new System.EventHandler(this.btnEnable_Click);
            //
            // btnDisable
            //
            this.btnDisable.AutoSize = true;
            this.btnDisable.BackColor = System.Drawing.Color.White;
            this.btnDisable.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(206)))), ((int)(((byte)(212)))), ((int)(((byte)(218)))));
            this.btnDisable.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDisable.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnDisable.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.btnDisable.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnDisable.Name = "btnDisable";
            this.btnDisable.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnDisable.Size = new System.Drawing.Size(74, 38);
            this.btnDisable.TabIndex = 3;
            this.btnDisable.Text = "Disable";
            this.btnDisable.UseVisualStyleBackColor = false;
            this.btnDisable.Click += new System.EventHandler(this.btnDisable_Click);
            //
            // btnUp
            //
            this.btnUp.AutoSize = true;
            this.btnUp.BackColor = System.Drawing.Color.White;
            this.btnUp.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(206)))), ((int)(((byte)(212)))), ((int)(((byte)(218)))));
            this.btnUp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUp.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnUp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.btnUp.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnUp.Name = "btnUp";
            this.btnUp.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnUp.Size = new System.Drawing.Size(90, 38);
            this.btnUp.TabIndex = 4;
            this.btnUp.Text = "Move Up";
            this.btnUp.UseVisualStyleBackColor = false;
            this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
            //
            // btnDown
            //
            this.btnDown.AutoSize = true;
            this.btnDown.BackColor = System.Drawing.Color.White;
            this.btnDown.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(206)))), ((int)(((byte)(212)))), ((int)(((byte)(218)))));
            this.btnDown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDown.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnDown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.btnDown.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnDown.Name = "btnDown";
            this.btnDown.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnDown.Size = new System.Drawing.Size(100, 38);
            this.btnDown.TabIndex = 5;
            this.btnDown.Text = "Move Down";
            this.btnDown.UseVisualStyleBackColor = false;
            this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
            //
            // btnDelete
            //
            this.btnDelete.AutoSize = true;
            this.btnDelete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.btnDelete.FlatAppearance.BorderSize = 0;
            this.btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDelete.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnDelete.ForeColor = System.Drawing.Color.White;
            this.btnDelete.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnDelete.Size = new System.Drawing.Size(120, 38);
            this.btnDelete.TabIndex = 6;
            this.btnDelete.Text = "Delete Window";
            this.btnDelete.UseVisualStyleBackColor = false;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            //
            // btnEnableAll
            //
            this.btnEnableAll.AutoSize = true;
            this.btnEnableAll.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.btnEnableAll.FlatAppearance.BorderSize = 0;
            this.btnEnableAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEnableAll.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnEnableAll.ForeColor = System.Drawing.Color.White;
            this.btnEnableAll.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnEnableAll.Name = "btnEnableAll";
            this.btnEnableAll.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnEnableAll.Size = new System.Drawing.Size(110, 38);
            this.btnEnableAll.TabIndex = 4;
            this.btnEnableAll.Text = "Activate All";
            this.btnEnableAll.UseVisualStyleBackColor = false;
            this.btnEnableAll.Click += new System.EventHandler(this.btnEnableAll_Click);
            //
            // btnDisableAll
            //
            this.btnDisableAll.AutoSize = true;
            this.btnDisableAll.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.btnDisableAll.FlatAppearance.BorderSize = 0;
            this.btnDisableAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDisableAll.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnDisableAll.ForeColor = System.Drawing.Color.White;
            this.btnDisableAll.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnDisableAll.Name = "btnDisableAll";
            this.btnDisableAll.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnDisableAll.Size = new System.Drawing.Size(120, 38);
            this.btnDisableAll.TabIndex = 5;
            this.btnDisableAll.Text = "Deactivate All";
            this.btnDisableAll.UseVisualStyleBackColor = false;
            this.btnDisableAll.Click += new System.EventHandler(this.btnDisableAll_Click);
            //
            // lblWinSub
            //
            this.lblWinSub.AutoSize = true;
            this.lblWinSub.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblWinSub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblWinSub.Location = new System.Drawing.Point(18, 18);
            this.lblWinSub.Name = "lblWinSub";
            this.lblWinSub.Size = new System.Drawing.Size(620, 19);
            this.lblWinSub.TabIndex = 0;
            this.lblWinSub.Text = "Add, edit, enable/disable, reorder and delete service windows. Changes apply everywhere instantly.";
            //
            // btnUnlock
            //
            this.btnUnlock.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUnlock.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(193)))), ((int)(((byte)(7)))));
            this.btnUnlock.FlatAppearance.BorderSize = 0;
            this.btnUnlock.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUnlock.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnUnlock.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.btnUnlock.Location = new System.Drawing.Point(852, 12);
            this.btnUnlock.Name = "btnUnlock";
            this.btnUnlock.Size = new System.Drawing.Size(240, 36);
            this.btnUnlock.TabIndex = 3;
            this.btnUnlock.Text = "🔒 Verify to Manage Windows";
            this.btnUnlock.UseVisualStyleBackColor = false;
            this.btnUnlock.Click += new System.EventHandler(this.btnUnlock_Click);
            //
            // tabManual
            //
            this.tabManual.BackColor = System.Drawing.Color.White;
            this.tabManual.Controls.Add(this.rtbContent);
            this.tabManual.Controls.Add(this.lstTopics);
            this.tabManual.Controls.Add(this.txtSearch);
            this.tabManual.Controls.Add(this.lblSearch);
            this.tabManual.Location = new System.Drawing.Point(4, 32);
            this.tabManual.Name = "tabManual";
            this.tabManual.Padding = new System.Windows.Forms.Padding(3);
            this.tabManual.Size = new System.Drawing.Size(1112, 684);
            this.tabManual.TabIndex = 1;
            this.tabManual.Text = "User Manual";
            //
            // rtbContent
            //
            this.rtbContent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbContent.BackColor = System.Drawing.Color.White;
            this.rtbContent.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rtbContent.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.rtbContent.Location = new System.Drawing.Point(408, 60);
            this.rtbContent.Name = "rtbContent";
            this.rtbContent.ReadOnly = true;
            this.rtbContent.Size = new System.Drawing.Size(684, 604);
            this.rtbContent.TabIndex = 3;
            this.rtbContent.Text = "";
            //
            // lstTopics
            //
            this.lstTopics.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.lstTopics.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstTopics.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lstTopics.FormattingEnabled = true;
            this.lstTopics.IntegralHeight = false;
            this.lstTopics.ItemHeight = 17;
            this.lstTopics.Location = new System.Drawing.Point(20, 60);
            this.lstTopics.Name = "lstTopics";
            this.lstTopics.Size = new System.Drawing.Size(372, 604);
            this.lstTopics.TabIndex = 2;
            this.lstTopics.SelectedIndexChanged += new System.EventHandler(this.lstTopics_SelectedIndexChanged);
            //
            // txtSearch
            //
            this.txtSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtSearch.Location = new System.Drawing.Point(20, 20);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(372, 27);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            //
            // lblSearch
            //
            this.lblSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSearch.AutoSize = true;
            this.lblSearch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblSearch.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblSearch.Location = new System.Drawing.Point(408, 24);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(430, 19);
            this.lblSearch.TabIndex = 0;
            this.lblSearch.Text = "Type a keyword (Login, Queue, Window, Reports, Settings…) to filter topics.";
            //
            // SettingsForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1120, 720);
            this.Controls.Add(this.tabs);
            this.Name = "SettingsForm";
            this.Text = "Settings";
            this.tabs.ResumeLayout(false);
            this.tabWindows.ResumeLayout(false);
            this.tabWindows.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvWindows)).EndInit();
            this.bar.ResumeLayout(false);
            this.bar.PerformLayout();
            this.tabManual.ResumeLayout(false);
            this.tabManual.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage tabWindows;
        private System.Windows.Forms.Label lblWinSub;
        private System.Windows.Forms.Button btnUnlock;
        private System.Windows.Forms.FlowLayoutPanel bar;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnEnable;
        private System.Windows.Forms.Button btnDisable;
        private System.Windows.Forms.Button btnUp;
        private System.Windows.Forms.Button btnDown;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnEnableAll;
        private System.Windows.Forms.Button btnDisableAll;
        private System.Windows.Forms.DataGridView dgvWindows;
        private System.Windows.Forms.TabPage tabManual;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.ListBox lstTopics;
        private System.Windows.Forms.RichTextBox rtbContent;
    }
}
