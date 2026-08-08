namespace CROMS.Forms
{
    partial class QueueManagementForm
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.btnPause = new System.Windows.Forms.Button();
            this.btnCallNext = new System.Windows.Forms.Button();
            this.grpServing = new System.Windows.Forms.GroupBox();
            this.pnlWin1 = new System.Windows.Forms.Panel();
            this.lblWin1Title = new System.Windows.Forms.Label();
            this.lblWin1Code = new System.Windows.Forms.Label();
            this.lblWin1Sub = new System.Windows.Forms.Label();
            this.pnlWin2 = new System.Windows.Forms.Panel();
            this.lblWin2Title = new System.Windows.Forms.Label();
            this.lblWin2Code = new System.Windows.Forms.Label();
            this.lblWin2Sub = new System.Windows.Forms.Label();
            this.pnlWin3 = new System.Windows.Forms.Panel();
            this.lblWin3Title = new System.Windows.Forms.Label();
            this.lblWin3Code = new System.Windows.Forms.Label();
            this.lblWin3Sub = new System.Windows.Forms.Label();
            this.lblLive = new System.Windows.Forms.Label();
            this.btnExport = new System.Windows.Forms.Button();
            this.dgvQueue = new System.Windows.Forms.DataGridView();
            this.grpServing.SuspendLayout();
            this.pnlWin1.SuspendLayout();
            this.pnlWin2.SuspendLayout();
            this.pnlWin3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvQueue)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblTitle.Location = new System.Drawing.Point(21, 14);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(277, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Queue Management";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblSubtitle.Location = new System.Drawing.Point(22, 49);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(268, 15);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "KIOSK  •  LIVE QUEUE  •  WINDOW ASSIGNMENTS";
            // 
            // btnPause
            // 
            this.btnPause.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPause.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPause.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPause.Location = new System.Drawing.Point(1114, 19);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(129, 35);
            this.btnPause.TabIndex = 2;
            this.btnPause.Text = "Pause Queue";
            this.btnPause.UseVisualStyleBackColor = true;
            this.btnPause.Click += new System.EventHandler(this.btnPause_Click);
            // 
            // btnCallNext
            // 
            this.btnCallNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCallNext.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.btnCallNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCallNext.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnCallNext.ForeColor = System.Drawing.Color.White;
            this.btnCallNext.Location = new System.Drawing.Point(1251, 19);
            this.btnCallNext.Name = "btnCallNext";
            this.btnCallNext.Size = new System.Drawing.Size(171, 35);
            this.btnCallNext.TabIndex = 3;
            this.btnCallNext.Text = "Call Next";
            this.btnCallNext.UseVisualStyleBackColor = false;
            this.btnCallNext.Click += new System.EventHandler(this.btnCallNext_Click);
            //
            // grpServing
            // 
            this.grpServing.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpServing.Controls.Add(this.pnlWin1);
            this.grpServing.Controls.Add(this.pnlWin2);
            this.grpServing.Controls.Add(this.pnlWin3);
            this.grpServing.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.grpServing.Location = new System.Drawing.Point(21, 71);
            this.grpServing.Name = "grpServing";
            this.grpServing.Size = new System.Drawing.Size(1407, 347);
            this.grpServing.TabIndex = 5;
            this.grpServing.TabStop = false;
            this.grpServing.Text = "NOW SERVING";
            // 
            // pnlWin1
            // 
            this.pnlWin1.BackColor = System.Drawing.Color.White;
            this.pnlWin1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlWin1.Controls.Add(this.lblWin1Title);
            this.pnlWin1.Controls.Add(this.lblWin1Code);
            this.pnlWin1.Controls.Add(this.lblWin1Sub);
            this.pnlWin1.Location = new System.Drawing.Point(21, 39);
            this.pnlWin1.Name = "pnlWin1";
            this.pnlWin1.Size = new System.Drawing.Size(257, 260);
            this.pnlWin1.TabIndex = 0;
            // 
            // lblWin1Title
            // 
            this.lblWin1Title.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblWin1Title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblWin1Title.Location = new System.Drawing.Point(0, 17);
            this.lblWin1Title.Name = "lblWin1Title";
            this.lblWin1Title.Size = new System.Drawing.Size(255, 17);
            this.lblWin1Title.TabIndex = 0;
            this.lblWin1Title.Text = "WINDOW 1";
            this.lblWin1Title.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblWin1Code
            // 
            this.lblWin1Code.Font = new System.Drawing.Font("Consolas", 34F, System.Drawing.FontStyle.Bold);
            this.lblWin1Code.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.lblWin1Code.Location = new System.Drawing.Point(0, 95);
            this.lblWin1Code.Name = "lblWin1Code";
            this.lblWin1Code.Size = new System.Drawing.Size(255, 52);
            this.lblWin1Code.TabIndex = 1;
            this.lblWin1Code.Text = "—";
            this.lblWin1Code.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblWin1Sub
            // 
            this.lblWin1Sub.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblWin1Sub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblWin1Sub.Location = new System.Drawing.Point(0, 160);
            this.lblWin1Sub.Name = "lblWin1Sub";
            this.lblWin1Sub.Size = new System.Drawing.Size(255, 17);
            this.lblWin1Sub.TabIndex = 2;
            this.lblWin1Sub.Text = "idle";
            this.lblWin1Sub.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pnlWin2
            // 
            this.pnlWin2.BackColor = System.Drawing.Color.White;
            this.pnlWin2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlWin2.Controls.Add(this.lblWin2Title);
            this.pnlWin2.Controls.Add(this.lblWin2Code);
            this.pnlWin2.Controls.Add(this.lblWin2Sub);
            this.pnlWin2.Location = new System.Drawing.Point(294, 39);
            this.pnlWin2.Name = "pnlWin2";
            this.pnlWin2.Size = new System.Drawing.Size(257, 260);
            this.pnlWin2.TabIndex = 1;
            // 
            // lblWin2Title
            // 
            this.lblWin2Title.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblWin2Title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblWin2Title.Location = new System.Drawing.Point(0, 17);
            this.lblWin2Title.Name = "lblWin2Title";
            this.lblWin2Title.Size = new System.Drawing.Size(255, 17);
            this.lblWin2Title.TabIndex = 0;
            this.lblWin2Title.Text = "WINDOW 2";
            this.lblWin2Title.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblWin2Code
            // 
            this.lblWin2Code.Font = new System.Drawing.Font("Consolas", 34F, System.Drawing.FontStyle.Bold);
            this.lblWin2Code.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.lblWin2Code.Location = new System.Drawing.Point(0, 95);
            this.lblWin2Code.Name = "lblWin2Code";
            this.lblWin2Code.Size = new System.Drawing.Size(255, 52);
            this.lblWin2Code.TabIndex = 1;
            this.lblWin2Code.Text = "—";
            this.lblWin2Code.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblWin2Sub
            // 
            this.lblWin2Sub.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblWin2Sub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblWin2Sub.Location = new System.Drawing.Point(0, 160);
            this.lblWin2Sub.Name = "lblWin2Sub";
            this.lblWin2Sub.Size = new System.Drawing.Size(255, 17);
            this.lblWin2Sub.TabIndex = 2;
            this.lblWin2Sub.Text = "idle";
            this.lblWin2Sub.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pnlWin3
            // 
            this.pnlWin3.BackColor = System.Drawing.Color.White;
            this.pnlWin3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlWin3.Controls.Add(this.lblWin3Title);
            this.pnlWin3.Controls.Add(this.lblWin3Code);
            this.pnlWin3.Controls.Add(this.lblWin3Sub);
            this.pnlWin3.Location = new System.Drawing.Point(567, 39);
            this.pnlWin3.Name = "pnlWin3";
            this.pnlWin3.Size = new System.Drawing.Size(257, 260);
            this.pnlWin3.TabIndex = 2;
            // 
            // lblWin3Title
            // 
            this.lblWin3Title.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblWin3Title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblWin3Title.Location = new System.Drawing.Point(0, 17);
            this.lblWin3Title.Name = "lblWin3Title";
            this.lblWin3Title.Size = new System.Drawing.Size(255, 17);
            this.lblWin3Title.TabIndex = 0;
            this.lblWin3Title.Text = "WINDOW 3";
            this.lblWin3Title.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblWin3Code
            // 
            this.lblWin3Code.Font = new System.Drawing.Font("Consolas", 34F, System.Drawing.FontStyle.Bold);
            this.lblWin3Code.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.lblWin3Code.Location = new System.Drawing.Point(0, 95);
            this.lblWin3Code.Name = "lblWin3Code";
            this.lblWin3Code.Size = new System.Drawing.Size(255, 52);
            this.lblWin3Code.TabIndex = 1;
            this.lblWin3Code.Text = "—";
            this.lblWin3Code.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblWin3Sub
            // 
            this.lblWin3Sub.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblWin3Sub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblWin3Sub.Location = new System.Drawing.Point(0, 160);
            this.lblWin3Sub.Name = "lblWin3Sub";
            this.lblWin3Sub.Size = new System.Drawing.Size(255, 17);
            this.lblWin3Sub.TabIndex = 2;
            this.lblWin3Sub.Text = "idle";
            this.lblWin3Sub.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLive
            // 
            this.lblLive.AutoSize = true;
            this.lblLive.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblLive.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(73)))), ((int)(((byte)(80)))), ((int)(((byte)(87)))));
            this.lblLive.Location = new System.Drawing.Point(21, 433);
            this.lblLive.Name = "lblLive";
            this.lblLive.Size = new System.Drawing.Size(146, 17);
            this.lblLive.TabIndex = 6;
            this.lblLive.Text = "LIVE QUEUE — TODAY";
            // 
            // btnExport
            // 
            this.btnExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnExport.Location = new System.Drawing.Point(1320, 428);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(103, 28);
            this.btnExport.TabIndex = 7;
            this.btnExport.Text = "Export CSV";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // dgvQueue
            // 
            this.dgvQueue.AllowUserToAddRows = false;
            this.dgvQueue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvQueue.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvQueue.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvQueue.BackgroundColor = System.Drawing.Color.White;
            this.dgvQueue.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvQueue.Location = new System.Drawing.Point(21, 459);
            this.dgvQueue.Name = "dgvQueue";
            this.dgvQueue.ReadOnly = true;
            this.dgvQueue.RowHeadersVisible = false;
            this.dgvQueue.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvQueue.Size = new System.Drawing.Size(1407, 352);
            this.dgvQueue.TabIndex = 8;
            // 
            // QueueManagementForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(1449, 837);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.btnPause);
            this.Controls.Add(this.btnCallNext);
            this.Controls.Add(this.grpServing);
            this.Controls.Add(this.lblLive);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.dgvQueue);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "QueueManagementForm";
            this.Text = "Queue Management";
            this.grpServing.ResumeLayout(false);
            this.pnlWin1.ResumeLayout(false);
            this.pnlWin2.ResumeLayout(false);
            this.pnlWin3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvQueue)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.Button btnCallNext;
        private System.Windows.Forms.GroupBox grpServing;
        private System.Windows.Forms.Panel pnlWin1;
        private System.Windows.Forms.Label lblWin1Title;
        private System.Windows.Forms.Label lblWin1Code;
        private System.Windows.Forms.Label lblWin1Sub;
        private System.Windows.Forms.Panel pnlWin2;
        private System.Windows.Forms.Label lblWin2Title;
        private System.Windows.Forms.Label lblWin2Code;
        private System.Windows.Forms.Label lblWin2Sub;
        private System.Windows.Forms.Panel pnlWin3;
        private System.Windows.Forms.Label lblWin3Title;
        private System.Windows.Forms.Label lblWin3Code;
        private System.Windows.Forms.Label lblWin3Sub;
        private System.Windows.Forms.Label lblLive;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.DataGridView dgvQueue;
    }
}
