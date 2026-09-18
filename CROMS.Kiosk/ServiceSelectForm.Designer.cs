using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Step 1 of 2 — choose one or more services. Designer-generated visual tree (behaviour
    /// in ServiceSelectForm.cs).
    /// </summary>
    public partial class ServiceSelectForm
    {
        private System.ComponentModel.IContainer components = null;

        private Panel header;
        private Label lblTitle;
        private StepIndicator _stepInd;
        private Panel footer;
        private Panel footerDivider;
        private Button _btnNext;
        private Button _btnBack;
        private Panel _host;
        private Panel panelStep1;
        private Label _svcHint;
        private Panel _offlineOverlay;
        private Label lblOffline;

        private readonly System.Collections.Generic.Dictionary<string, Panel> _cards =
            new System.Collections.Generic.Dictionary<string, Panel>();

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.header = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this._stepInd = new CROMS.Kiosk.StepIndicator();
            this.footer = new System.Windows.Forms.Panel();
            this.footerDivider = new System.Windows.Forms.Panel();
            this._btnNext = new System.Windows.Forms.Button();
            this._btnBack = new System.Windows.Forms.Button();
            this._host = new System.Windows.Forms.Panel();
            this.panelStep1 = new System.Windows.Forms.Panel();
            this._svcHint = new System.Windows.Forms.Label();
            this._offlineOverlay = new System.Windows.Forms.Panel();
            this.lblOffline = new System.Windows.Forms.Label();
            this.header.SuspendLayout();
            this.footer.SuspendLayout();
            this._host.SuspendLayout();
            this.panelStep1.SuspendLayout();
            this._offlineOverlay.SuspendLayout();
            this.SuspendLayout();
            // 
            // header
            // 
            this.header.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.header.Controls.Add(this.lblTitle);
            this.header.Controls.Add(this._stepInd);
            this.header.Dock = System.Windows.Forms.DockStyle.Top;
            this.header.Location = new System.Drawing.Point(0, 0);
            this.header.Name = "header";
            this.header.Size = new System.Drawing.Size(1264, 128);
            this.header.TabIndex = 3;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 30F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(26)))), ((int)(((byte)(36)))));
            this.lblTitle.Location = new System.Drawing.Point(40, 18);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(355, 54);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Request a Service";
            // 
            // _stepInd
            // 
            this._stepInd.BackColor = System.Drawing.Color.Transparent;
            this._stepInd.Location = new System.Drawing.Point(40, 74);
            this._stepInd.Name = "_stepInd";
            this._stepInd.Size = new System.Drawing.Size(560, 52);
            this._stepInd.Steps = new string[] {
        "Select Services",
        "Personal Info & Photo"};
            this._stepInd.TabIndex = 1;
            // 
            // footer
            // 
            this.footer.BackColor = System.Drawing.Color.White;
            this.footer.Controls.Add(this.footerDivider);
            this.footer.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.footer.Location = new System.Drawing.Point(0, 589);
            this.footer.Name = "footer";
            this.footer.Size = new System.Drawing.Size(1264, 92);
            this.footer.TabIndex = 1;
            // 
            // footerDivider
            // 
            this.footerDivider.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(229)))), ((int)(((byte)(236)))));
            this.footerDivider.Dock = System.Windows.Forms.DockStyle.Top;
            this.footerDivider.Location = new System.Drawing.Point(0, 0);
            this.footerDivider.Name = "footerDivider";
            this.footerDivider.Size = new System.Drawing.Size(1264, 1);
            this.footerDivider.TabIndex = 0;
            // 
            // _btnNext
            // 
            this._btnNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnNext.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
            this._btnNext.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnNext.FlatAppearance.BorderSize = 0;
            this._btnNext.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(68)))), ((int)(((byte)(192)))));
            this._btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnNext.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this._btnNext.ForeColor = System.Drawing.Color.White;
            this._btnNext.Location = new System.Drawing.Point(904, 593);
            this._btnNext.Name = "_btnNext";
            this._btnNext.Size = new System.Drawing.Size(320, 72);
            this._btnNext.TabIndex = 2;
            this._btnNext.Text = "Next Step";
            this._btnNext.UseVisualStyleBackColor = false;
            this._btnNext.Click += new System.EventHandler(this.BtnNext_Click);
            // 
            // _btnBack
            // 
            this._btnBack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._btnBack.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnBack.FlatAppearance.BorderSize = 0;
            this._btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnBack.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this._btnBack.Location = new System.Drawing.Point(40, 593);
            this._btnBack.Name = "_btnBack";
            this._btnBack.Size = new System.Drawing.Size(210, 72);
            this._btnBack.TabIndex = 6;
            this._btnBack.Text = "Back";
            this._btnBack.UseVisualStyleBackColor = false;
            this._btnBack.Click += new System.EventHandler(this.BtnBack_Click);
            // 
            // _host
            // 
            this._host.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this._host.Controls.Add(this.panelStep1);
            this._host.Dock = System.Windows.Forms.DockStyle.Fill;
            this._host.Location = new System.Drawing.Point(0, 128);
            this._host.Name = "_host";
            this._host.Size = new System.Drawing.Size(1264, 461);
            this._host.TabIndex = 0;
            // 
            // panelStep1
            // 
            this.panelStep1.AutoScroll = true;
            this.panelStep1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.panelStep1.Controls.Add(this._svcHint);
            this.panelStep1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelStep1.Location = new System.Drawing.Point(0, 0);
            this.panelStep1.Name = "panelStep1";
            this.panelStep1.Size = new System.Drawing.Size(1264, 461);
            this.panelStep1.TabIndex = 0;
            this.panelStep1.Resize += new System.EventHandler(this.PanelStep1_Resize);
            // 
            // _svcHint
            // 
            this._svcHint.AutoSize = true;
            this._svcHint.Font = new System.Drawing.Font("Segoe UI", 13F);
            this._svcHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(100)))), ((int)(((byte)(114)))));
            this._svcHint.Location = new System.Drawing.Point(20, 20);
            this._svcHint.Name = "_svcHint";
            this._svcHint.Size = new System.Drawing.Size(509, 25);
            this._svcHint.TabIndex = 0;
            this._svcHint.Text = "Tap one or more services you need today. You can pick several.";
            // 
            // _offlineOverlay
            // 
            this._offlineOverlay.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._offlineOverlay.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(17)))), ((int)(((byte)(24)))), ((int)(((byte)(39)))));
            this._offlineOverlay.Controls.Add(this.lblOffline);
            this._offlineOverlay.Location = new System.Drawing.Point(0, 0);
            this._offlineOverlay.Name = "_offlineOverlay";
            this._offlineOverlay.Size = new System.Drawing.Size(1264, 681);
            this._offlineOverlay.TabIndex = 4;
            this._offlineOverlay.Visible = false;
            // 
            // lblOffline
            // 
            this.lblOffline.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOffline.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold);
            this.lblOffline.ForeColor = System.Drawing.Color.White;
            this.lblOffline.Location = new System.Drawing.Point(0, 0);
            this.lblOffline.Name = "lblOffline";
            this.lblOffline.Size = new System.Drawing.Size(1264, 681);
            this.lblOffline.TabIndex = 0;
            this.lblOffline.Text = "The office is currently unavailable.\r\n\r\nPlease try again later.\r\n\r\n(Waiting for a" +
    " service window to come online…)";
            this.lblOffline.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ServiceSelectForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(1264, 681);
            this.Controls.Add(this._host);
            this.Controls.Add(this.footer);
            this.Controls.Add(this._btnNext);
            this.Controls.Add(this._btnBack);
            this.Controls.Add(this.header);
            this.Controls.Add(this._offlineOverlay);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Name = "ServiceSelectForm";
            this.Text = "CROMS — Client Kiosk";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.header.ResumeLayout(false);
            this.header.PerformLayout();
            this.footer.ResumeLayout(false);
            this._host.ResumeLayout(false);
            this.panelStep1.ResumeLayout(false);
            this.panelStep1.PerformLayout();
            this._offlineOverlay.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
