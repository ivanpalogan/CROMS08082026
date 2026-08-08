namespace CROMS.Forms
{
    partial class WindowAssignmentForm
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblSelectWindow = new System.Windows.Forms.Label();
            this.winPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSelectTxn = new System.Windows.Forms.Label();
            this.txnPanel = new System.Windows.Forms.TableLayoutPanel();
            this.chkAll = new System.Windows.Forms.CheckBox();
            this.lblMsg = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnSkip = new System.Windows.Forms.Button();
            this.txnPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblTitle.Location = new System.Drawing.Point(24, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(220, 32);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Window Assignment";
            //
            // lblSubtitle
            //
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblSubtitle.Location = new System.Drawing.Point(26, 56);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(0, 17);
            this.lblSubtitle.TabIndex = 1;
            //
            // lblSelectWindow
            //
            this.lblSelectWindow.AutoSize = true;
            this.lblSelectWindow.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            this.lblSelectWindow.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.lblSelectWindow.Location = new System.Drawing.Point(24, 92);
            this.lblSelectWindow.Name = "lblSelectWindow";
            this.lblSelectWindow.Size = new System.Drawing.Size(105, 19);
            this.lblSelectWindow.TabIndex = 2;
            this.lblSelectWindow.Text = "Select Window";
            //
            // winPanel
            //
            this.winPanel.AutoScroll = true;
            this.winPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.winPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.winPanel.Location = new System.Drawing.Point(24, 118);
            this.winPanel.Name = "winPanel";
            this.winPanel.Size = new System.Drawing.Size(672, 150);
            this.winPanel.TabIndex = 3;
            this.winPanel.WrapContents = true;
            //
            // lblSelectTxn
            //
            this.lblSelectTxn.AutoSize = true;
            this.lblSelectTxn.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            this.lblSelectTxn.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.lblSelectTxn.Location = new System.Drawing.Point(24, 280);
            this.lblSelectTxn.Name = "lblSelectTxn";
            this.lblSelectTxn.Size = new System.Drawing.Size(140, 19);
            this.lblSelectTxn.TabIndex = 4;
            this.lblSelectTxn.Text = "Select Transactions";
            //
            // txnPanel — 2 equal columns, auto-height rows, so long labels never clip.
            //
            this.txnPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txnPanel.ColumnCount = 2;
            this.txnPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.txnPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.txnPanel.Controls.Add(this.chkAll, 0, 0);
            this.txnPanel.SetColumnSpan(this.chkAll, 2);
            this.txnPanel.Location = new System.Drawing.Point(24, 306);
            this.txnPanel.Name = "txnPanel";
            this.txnPanel.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.txnPanel.RowCount = 4;
            this.txnPanel.Size = new System.Drawing.Size(672, 150);
            this.txnPanel.TabIndex = 5;
            //
            // chkAll
            //
            this.chkAll.AutoSize = true;
            this.chkAll.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.chkAll.Margin = new System.Windows.Forms.Padding(8, 8, 8, 6);
            this.chkAll.Name = "chkAll";
            this.chkAll.Size = new System.Drawing.Size(134, 23);
            this.chkAll.TabIndex = 0;
            this.chkAll.Text = "★ Priority Window  (handles ALL transactions)";
            this.chkAll.UseVisualStyleBackColor = true;
            this.chkAll.CheckedChanged += new System.EventHandler(this.chkAll_CheckedChanged);
            //
            // lblMsg
            //
            this.lblMsg.AutoSize = true;
            this.lblMsg.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblMsg.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.lblMsg.Location = new System.Drawing.Point(24, 478);
            this.lblMsg.Name = "lblMsg";
            this.lblMsg.Size = new System.Drawing.Size(0, 15);
            this.lblMsg.TabIndex = 6;
            //
            // btnStart
            //
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.btnStart.FlatAppearance.BorderSize = 0;
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.Location = new System.Drawing.Point(24, 504);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(332, 44);
            this.btnStart.TabIndex = 7;
            this.btnStart.Text = "Start Serving";
            this.btnStart.UseVisualStyleBackColor = false;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            //
            // btnSkip
            //
            this.btnSkip.BackColor = System.Drawing.Color.White;
            this.btnSkip.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(206)))), ((int)(((byte)(212)))), ((int)(((byte)(218)))));
            this.btnSkip.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSkip.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnSkip.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.btnSkip.Location = new System.Drawing.Point(388, 504);
            this.btnSkip.Name = "btnSkip";
            this.btnSkip.Size = new System.Drawing.Size(308, 44);
            this.btnSkip.TabIndex = 8;
            this.btnSkip.Text = "Skip";
            this.btnSkip.UseVisualStyleBackColor = false;
            this.btnSkip.Click += new System.EventHandler(this.btnSkip_Click);
            //
            // WindowAssignmentForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(720, 566);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblSelectWindow);
            this.Controls.Add(this.winPanel);
            this.Controls.Add(this.lblSelectTxn);
            this.Controls.Add(this.txnPanel);
            this.Controls.Add(this.lblMsg);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnSkip);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WindowAssignmentForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CROMS — Window Assignment";
            this.txnPanel.ResumeLayout(false);
            this.txnPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Label lblSelectWindow;
        private System.Windows.Forms.FlowLayoutPanel winPanel;
        private System.Windows.Forms.Label lblSelectTxn;
        private System.Windows.Forms.TableLayoutPanel txnPanel;
        private System.Windows.Forms.CheckBox chkAll;
        private System.Windows.Forms.Label lblMsg;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnSkip;
    }
}
