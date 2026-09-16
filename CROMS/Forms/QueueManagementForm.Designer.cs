using System;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Modules;

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

        // One spacing scale for this screen, stated once so the next edit reuses it instead of
        // guessing: page 20, section gap 14, card padding 12, control gutter 8.
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.layoutRoot = new System.Windows.Forms.TableLayoutPanel();
            this.pnlHeader = new System.Windows.Forms.TableLayoutPanel();
            this.pnlTitle = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.pnlHeaderRight = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlClock = new System.Windows.Forms.Panel();
            this._lblClock = new System.Windows.Forms.Label();
            this._lblClockDate = new System.Windows.Forms.Label();
            this._lblSync = new System.Windows.Forms.Label();
            this.btnClientDisplay = new System.Windows.Forms.Button();

            this.pnlKpis = new System.Windows.Forms.TableLayoutPanel();
            this._kpiWaiting = new CROMS.Modules.KpiCard();
            this._kpiAvgWait = new CROMS.Modules.KpiCard();
            this._kpiServed = new CROMS.Modules.KpiCard();
            this._kpiLongest = new CROMS.Modules.KpiCard();

            this.pnlServingHead = new System.Windows.Forms.Panel();
            this.lblServingTitle = new System.Windows.Forms.Label();
            this.lblServingHint = new System.Windows.Forms.Label();
            this.cardServing = new CROMS.Modules.CardPanel();
            this._servingGrid = new System.Windows.Forms.TableLayoutPanel();

            this.pnlWorkflow = new System.Windows.Forms.TableLayoutPanel();
            this.pnlWorkflowButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCallNext = new System.Windows.Forms.Button();
            this._btnCallClient = new System.Windows.Forms.Button();
            this._btnRecall = new System.Windows.Forms.Button();
            this._btnForward = new System.Windows.Forms.Button();
            this._lblNextStep = new System.Windows.Forms.Label();

            this.pnlQueueHead = new System.Windows.Forms.TableLayoutPanel();
            this.lblLive = new System.Windows.Forms.Label();
            this.pnlChips = new System.Windows.Forms.FlowLayoutPanel();
            this._chipAll = new System.Windows.Forms.Button();
            this._chipNewReg = new System.Windows.Forms.Button();
            this._chipCtc = new System.Windows.Forms.Button();
            this._chipMarriage = new System.Windows.Forms.Button();
            this._chipDeath = new System.Windows.Forms.Button();
            this._chipPetition = new System.Windows.Forms.Button();
            this._chipClaim = new System.Windows.Forms.Button();
            this._txtSearch = new System.Windows.Forms.TextBox();

            this.pnlQueueArea = new System.Windows.Forms.TableLayoutPanel();
            this.cardQueue = new CROMS.Modules.CardPanel();
            this.dgvQueue = new System.Windows.Forms.DataGridView();
            this.cardPriority = new CROMS.Modules.CardPanel();
            this.dgvPriority = new System.Windows.Forms.DataGridView();
            this.lblPriorityHead = new System.Windows.Forms.Label();

            this.pnlFooter = new System.Windows.Forms.TableLayoutPanel();
            this._lblLegend = new System.Windows.Forms.Label();
            this.pnlFooterButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnPause = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();

            this._syncTimer = new System.Windows.Forms.Timer(this.components);
            this._clockTimer = new System.Windows.Forms.Timer(this.components);

            ((System.ComponentModel.ISupportInitialize)(this.dgvQueue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPriority)).BeginInit();
            this.SuspendLayout();

            //
            // layoutRoot — the whole screen. Rows are stated once here so nothing is positioned
            // by absolute pixels: only the queue list grows, everything else is a fixed band.
            //
            this.layoutRoot.ColumnCount = 1;
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.RowCount = 8;
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 66F));   // header
            // 128: tightened from 148 (KpiCard's own insets/gaps were tightened to match — see
            // Modules/KpiCard.cs) so the KPI strip stops crowding the queue table beneath it,
            // while still leaving room for chip(32) + label + value ascent + caption.
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 128F));  // KPI tiles
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));   // my-window heading
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 152F));  // window card
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));   // workflow bar
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));   // queue heading + filters
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));   // queue list
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));   // legend + actions
            this.layoutRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutRoot.Padding = new System.Windows.Forms.Padding(20, 14, 20, 10);
            this.layoutRoot.BackColor = System.Drawing.Color.Transparent;
            this.layoutRoot.Name = "layoutRoot";

            //
            // pnlHeader
            //
            this.pnlHeader.ColumnCount = 2;
            this.pnlHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.pnlHeader.RowCount = 1;
            // A TableLayoutPanel with no RowStyle gives its child the default 100px row, which
            // overflows the band it sits in and clips the text. Every inner table states its row.
            this.pnlHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlHeader.Margin = new System.Windows.Forms.Padding(0);
            this.pnlHeader.BackColor = System.Drawing.Color.Transparent;
            this.pnlHeader.Name = "pnlHeader";

            this.pnlTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTitle.Margin = new System.Windows.Forms.Padding(0);
            this.pnlTitle.BackColor = System.Drawing.Color.Transparent;
            this.pnlTitle.Name = "pnlTitle";

            // Sized explicitly, not AutoSize: a 17pt AutoSize label is taller than it looks and
            // ran into the subtitle underneath it.
            this.lblTitle.AutoSize = false;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 17F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = CROMS.Modules.UiTheme.Ink;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Size = new System.Drawing.Size(560, 38);
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Text = "Queue Management";
            this.lblTitle.UseMnemonic = false;

            this.lblSubtitle.AutoSize = false;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = CROMS.Modules.UiTheme.Muted;
            this.lblSubtitle.Location = new System.Drawing.Point(2, 40);
            this.lblSubtitle.Size = new System.Drawing.Size(560, 20);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Text = "LCRO Peñablanca  ·  live ticket board";
            this.lblSubtitle.UseMnemonic = false;

            this.pnlTitle.Controls.Add(this.lblTitle);
            this.pnlTitle.Controls.Add(this.lblSubtitle);

            // Right of the header, laid out right-to-left so the clock always sits at the edge
            // and the button flows to its left however wide the window name gets.
            this.pnlHeaderRight.AutoSize = true;
            this.pnlHeaderRight.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlHeaderRight.WrapContents = false;
            this.pnlHeaderRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlHeaderRight.Margin = new System.Windows.Forms.Padding(0);
            this.pnlHeaderRight.BackColor = System.Drawing.Color.Transparent;
            this.pnlHeaderRight.Name = "pnlHeaderRight";

            this.pnlClock.Size = new System.Drawing.Size(210, 52);
            this.pnlClock.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.pnlClock.BackColor = System.Drawing.Color.Transparent;
            this.pnlClock.Name = "pnlClock";

            this._lblClock.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblClock.Height = 28;
            this._lblClock.Font = new System.Drawing.Font("Consolas", 15F, System.Drawing.FontStyle.Bold);
            this._lblClock.ForeColor = CROMS.Modules.UiTheme.Ink;
            this._lblClock.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this._lblClock.Name = "_lblClock";
            this._lblClock.Text = "--:--:--";

            this._lblClockDate.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblClockDate.Height = 18;
            this._lblClockDate.Font = new System.Drawing.Font("Segoe UI", 8F);
            this._lblClockDate.ForeColor = CROMS.Modules.UiTheme.Faint;
            this._lblClockDate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this._lblClockDate.Name = "_lblClockDate";
            this._lblClockDate.Text = "";

            this.pnlClock.Controls.Add(this._lblClockDate);
            this.pnlClock.Controls.Add(this._lblClock);

            this._lblSync.AutoSize = true;
            this._lblSync.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this._lblSync.ForeColor = CROMS.Modules.UiTheme.Success;
            this._lblSync.Margin = new System.Windows.Forms.Padding(0, 18, 14, 0);
            this._lblSync.Name = "_lblSync";
            this._lblSync.Text = "● Live";

            this.btnClientDisplay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClientDisplay.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnClientDisplay.Size = new System.Drawing.Size(138, 34);
            this.btnClientDisplay.Margin = new System.Windows.Forms.Padding(0, 12, 14, 0);
            this.btnClientDisplay.Name = "btnClientDisplay";
            this.btnClientDisplay.Text = "Client Display";
            this.btnClientDisplay.Click += new System.EventHandler(this.btnClientDisplay_Click);

            this.pnlHeaderRight.Controls.Add(this.pnlClock);
            this.pnlHeaderRight.Controls.Add(this._lblSync);
            this.pnlHeaderRight.Controls.Add(this.btnClientDisplay);

            this.pnlHeader.Controls.Add(this.pnlTitle, 0, 0);
            this.pnlHeader.Controls.Add(this.pnlHeaderRight, 1, 0);

            //
            // pnlKpis — four equal tiles, so they share one baseline whatever the window width.
            //
            this.pnlKpis.ColumnCount = 4;
            this.pnlKpis.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpis.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpis.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpis.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.pnlKpis.RowCount = 1;
            this.pnlKpis.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlKpis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlKpis.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.pnlKpis.BackColor = System.Drawing.Color.Transparent;
            this.pnlKpis.Name = "pnlKpis";

            this._kpiWaiting.Dock = System.Windows.Forms.DockStyle.Fill;
            this._kpiWaiting.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this._kpiWaiting.Name = "_kpiWaiting";
            this._kpiWaiting.Label = "WAITING NOW";
            this._kpiWaiting.IconKind = CROMS.Modules.KpiCard.Icon.QueuePerson;

            this._kpiAvgWait.Dock = System.Windows.Forms.DockStyle.Fill;
            this._kpiAvgWait.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this._kpiAvgWait.Name = "_kpiAvgWait";
            this._kpiAvgWait.Label = "AVERAGE WAIT";
            this._kpiAvgWait.IconKind = CROMS.Modules.KpiCard.Icon.InboxTray;

            this._kpiServed.Dock = System.Windows.Forms.DockStyle.Fill;
            this._kpiServed.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this._kpiServed.Name = "_kpiServed";
            this._kpiServed.Label = "SERVED TODAY";
            this._kpiServed.IconKind = CROMS.Modules.KpiCard.Icon.DocumentTick;

            this._kpiLongest.Dock = System.Windows.Forms.DockStyle.Fill;
            this._kpiLongest.Margin = new System.Windows.Forms.Padding(0);
            this._kpiLongest.Name = "_kpiLongest";
            this._kpiLongest.Label = "LONGEST WAIT";
            this._kpiLongest.IconKind = CROMS.Modules.KpiCard.Icon.QueuePerson;

            this.pnlKpis.Controls.Add(this._kpiWaiting, 0, 0);
            this.pnlKpis.Controls.Add(this._kpiAvgWait, 1, 0);
            this.pnlKpis.Controls.Add(this._kpiServed, 2, 0);
            this.pnlKpis.Controls.Add(this._kpiLongest, 3, 0);

            //
            // pnlServingHead
            //
            // Docked, not anchored: a Right-anchored label added to a container that has not yet
            // reached its real width locks in a huge negative margin and lands off-screen — the
            // same trap that hid the kiosk's step label.
            this.pnlServingHead.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlServingHead.Margin = new System.Windows.Forms.Padding(0);
            this.pnlServingHead.BackColor = System.Drawing.Color.Transparent;
            this.pnlServingHead.Name = "pnlServingHead";

            this.lblServingHint.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblServingHint.Width = 420;
            this.lblServingHint.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblServingHint.ForeColor = CROMS.Modules.UiTheme.Faint;
            this.lblServingHint.Name = "lblServingHint";
            this.lblServingHint.Text = "";
            this.lblServingHint.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblServingHint.UseMnemonic = false;

            this.lblServingTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblServingTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblServingTitle.ForeColor = CROMS.Modules.UiTheme.Muted;
            this.lblServingTitle.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblServingTitle.Name = "lblServingTitle";
            this.lblServingTitle.Text = "MY WINDOW";
            this.lblServingTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblServingTitle.UseMnemonic = false;

            this.pnlServingHead.Controls.Add(this.lblServingTitle);   // fill first
            this.pnlServingHead.Controls.Add(this.lblServingHint);    // then the right edge

            //
            // cardServing
            //
            this.cardServing.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardServing.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.cardServing.Padding = new System.Windows.Forms.Padding(10, 10, 10, 12);
            this.cardServing.Radius = 12;
            this.cardServing.Name = "cardServing";

            this._servingGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._servingGrid.BackColor = System.Drawing.Color.Transparent;
            this._servingGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._servingGrid.Name = "_servingGrid";
            this.cardServing.Controls.Add(this._servingGrid);

            //
            // pnlWorkflow — the actions, then the next step in words beside them.
            //
            this.pnlWorkflow.ColumnCount = 2;
            this.pnlWorkflow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.pnlWorkflow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlWorkflow.RowCount = 1;
            this.pnlWorkflow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlWorkflow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlWorkflow.Margin = new System.Windows.Forms.Padding(0);
            this.pnlWorkflow.BackColor = System.Drawing.Color.Transparent;
            this.pnlWorkflow.Name = "pnlWorkflow";

            this.pnlWorkflowButtons.AutoSize = true;
            this.pnlWorkflowButtons.WrapContents = false;
            this.pnlWorkflowButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlWorkflowButtons.Margin = new System.Windows.Forms.Padding(0);
            this.pnlWorkflowButtons.BackColor = System.Drawing.Color.Transparent;
            this.pnlWorkflowButtons.Name = "pnlWorkflowButtons";

            this.btnCallNext.BackColor = CROMS.Modules.UiTheme.Accent;
            this.btnCallNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCallNext.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnCallNext.ForeColor = System.Drawing.Color.White;
            this.btnCallNext.Size = new System.Drawing.Size(152, 38);
            this.btnCallNext.Margin = new System.Windows.Forms.Padding(0, 8, 8, 0);
            this.btnCallNext.Name = "btnCallNext";
            this.btnCallNext.Text = "Call Next";
            this.btnCallNext.UseVisualStyleBackColor = false;
            this.btnCallNext.Click += new System.EventHandler(this.btnCallNext_Click);

            this._btnCallClient.BackColor = CROMS.Modules.UiTheme.Success;
            this._btnCallClient.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCallClient.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this._btnCallClient.ForeColor = System.Drawing.Color.White;
            this._btnCallClient.Size = new System.Drawing.Size(132, 38);
            this._btnCallClient.Margin = new System.Windows.Forms.Padding(0, 8, 8, 0);
            this._btnCallClient.Name = "_btnCallClient";
            this._btnCallClient.Text = "Call Client";
            this._btnCallClient.UseVisualStyleBackColor = false;
            this._btnCallClient.Click += new System.EventHandler(this.btnCallClient_Click);

            this._btnRecall.BackColor = CROMS.Modules.UiTheme.Chrome;
            this._btnRecall.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnRecall.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnRecall.ForeColor = CROMS.Modules.UiTheme.Ink;
            this._btnRecall.Size = new System.Drawing.Size(96, 38);
            this._btnRecall.Margin = new System.Windows.Forms.Padding(0, 8, 8, 0);
            this._btnRecall.Name = "_btnRecall";
            this._btnRecall.Text = "Recall";
            this._btnRecall.UseVisualStyleBackColor = false;
            this._btnRecall.Click += new System.EventHandler(this.btnRecall_Click);

            this._btnForward.BackColor = CROMS.Modules.UiTheme.Chrome;
            this._btnForward.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnForward.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnForward.ForeColor = CROMS.Modules.UiTheme.Ink;
            this._btnForward.Size = new System.Drawing.Size(150, 38);
            this._btnForward.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this._btnForward.Name = "_btnForward";
            this._btnForward.Text = "Forward to Window";
            this._btnForward.UseVisualStyleBackColor = false;
            this._btnForward.Click += new System.EventHandler(this.btnForward_Click);

            this.pnlWorkflowButtons.Controls.Add(this.btnCallNext);
            this.pnlWorkflowButtons.Controls.Add(this._btnCallClient);
            this.pnlWorkflowButtons.Controls.Add(this._btnRecall);
            this.pnlWorkflowButtons.Controls.Add(this._btnForward);

            this._lblNextStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblNextStep.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._lblNextStep.ForeColor = CROMS.Modules.UiTheme.Muted;
            this._lblNextStep.Margin = new System.Windows.Forms.Padding(16, 8, 0, 0);
            this._lblNextStep.Name = "_lblNextStep";
            this._lblNextStep.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._lblNextStep.UseMnemonic = false;

            this.pnlWorkflow.Controls.Add(this.pnlWorkflowButtons, 0, 0);
            this.pnlWorkflow.Controls.Add(this._lblNextStep, 1, 0);

            //
            // pnlQueueHead — section name, service filters, search.
            //
            this.pnlQueueHead.ColumnCount = 3;
            this.pnlQueueHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 206F));
            this.pnlQueueHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlQueueHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 232F));
            this.pnlQueueHead.RowCount = 1;
            this.pnlQueueHead.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlQueueHead.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlQueueHead.Margin = new System.Windows.Forms.Padding(0);
            this.pnlQueueHead.BackColor = System.Drawing.Color.Transparent;
            this.pnlQueueHead.Name = "pnlQueueHead";

            this.lblLive.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLive.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblLive.ForeColor = CROMS.Modules.UiTheme.Muted;
            this.lblLive.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblLive.Name = "lblLive";
            this.lblLive.Text = "TODAY'S QUEUE";
            this.lblLive.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblLive.UseMnemonic = false;

            this.pnlChips.AutoSize = false;
            this.pnlChips.WrapContents = false;
            this.pnlChips.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlChips.Margin = new System.Windows.Forms.Padding(0);
            this.pnlChips.BackColor = System.Drawing.Color.Transparent;
            this.pnlChips.Name = "pnlChips";

            // No "Priority" chip: the priority lane is its own always-visible table below, so a
            // filter that hid everything else would only take the regular queue off screen.
            this.ConfigureChip(this._chipAll, "_chipAll", "All", "ALL");
            this.ConfigureChip(this._chipNewReg, "_chipNewReg", "New Reg.", "NEWREG");
            this.ConfigureChip(this._chipCtc, "_chipCtc", "CTC", "CTC");
            this.ConfigureChip(this._chipMarriage, "_chipMarriage", "Marriage", "MARRIAGE");
            this.ConfigureChip(this._chipDeath, "_chipDeath", "Death", "DEATH");
            this.ConfigureChip(this._chipPetition, "_chipPetition", "Petition", "PETITION");
            this.ConfigureChip(this._chipClaim, "_chipClaim", "Claim", "CLAIM");

            this.pnlChips.Controls.Add(this._chipAll);
            this.pnlChips.Controls.Add(this._chipNewReg);
            this.pnlChips.Controls.Add(this._chipCtc);
            this.pnlChips.Controls.Add(this._chipMarriage);
            this.pnlChips.Controls.Add(this._chipDeath);
            this.pnlChips.Controls.Add(this._chipPetition);
            this.pnlChips.Controls.Add(this._chipClaim);

            this._txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._txtSearch.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this._txtSearch.Size = new System.Drawing.Size(226, 26);
            this._txtSearch.Margin = new System.Windows.Forms.Padding(6, 8, 2, 0);
            this._txtSearch.Name = "_txtSearch";
            this._txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);

            this.pnlQueueHead.Controls.Add(this.lblLive, 0, 0);
            this.pnlQueueHead.Controls.Add(this.pnlChips, 1, 0);
            this.pnlQueueHead.Controls.Add(this._txtSearch, 2, 0);

            //
            // pnlQueueArea — the PRIORITY LANE keeps a fixed band ON TOP, always on screen with
            // no scrolling needed: it is a lane the law requires be served first (RA 11261), so
            // it must be the first thing staff see, not something found by scrolling past the
            // regular queue. The regular queue grows to fill the rest.
            //
            this.pnlQueueArea.ColumnCount = 1;
            this.pnlQueueArea.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlQueueArea.RowCount = 2;
            this.pnlQueueArea.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 168F));
            this.pnlQueueArea.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlQueueArea.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlQueueArea.Margin = new System.Windows.Forms.Padding(0);
            this.pnlQueueArea.BackColor = System.Drawing.Color.Transparent;
            this.pnlQueueArea.Name = "pnlQueueArea";

            //
            // cardQueue + dgvQueue
            //
            this.cardQueue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardQueue.Margin = new System.Windows.Forms.Padding(0, 6, 0, 4);
            this.cardQueue.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.cardQueue.Radius = 12;
            this.cardQueue.Name = "cardQueue";

            this.dgvQueue.AllowUserToAddRows = false;
            this.dgvQueue.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvQueue.BackgroundColor = System.Drawing.Color.White;
            this.dgvQueue.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvQueue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvQueue.Name = "dgvQueue";
            this.dgvQueue.ReadOnly = true;
            this.dgvQueue.RowHeadersVisible = false;
            this.dgvQueue.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvQueue.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvQueue_CellDoubleClick);
            this.dgvQueue.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgvQueue_CellFormatting);
            this.cardQueue.Controls.Add(this.dgvQueue);

            //
            // cardPriority + dgvPriority — the same columns and the same row rendering, so the two
            // lists read as one table split by lane rather than as two different screens.
            //
            this.cardPriority.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardPriority.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.cardPriority.Padding = new System.Windows.Forms.Padding(8, 6, 8, 8);
            this.cardPriority.Radius = 12;
            this.cardPriority.LineColor = CROMS.Modules.UiTheme.Mix(CROMS.Modules.UiTheme.WarningTint, CROMS.Modules.UiTheme.Warning, 0.35F);
            this.cardPriority.Name = "cardPriority";

            this.lblPriorityHead.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPriorityHead.Height = 24;
            this.lblPriorityHead.BackColor = System.Drawing.Color.Transparent;
            this.lblPriorityHead.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblPriorityHead.ForeColor = CROMS.Modules.UiTheme.Warning;
            this.lblPriorityHead.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblPriorityHead.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblPriorityHead.UseMnemonic = false;
            this.lblPriorityHead.Name = "lblPriorityHead";
            this.lblPriorityHead.Text = "PRIORITY LANE";

            this.dgvPriority.AllowUserToAddRows = false;
            this.dgvPriority.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPriority.BackgroundColor = System.Drawing.Color.White;
            this.dgvPriority.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvPriority.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPriority.Name = "dgvPriority";
            this.dgvPriority.ReadOnly = true;
            this.dgvPriority.RowHeadersVisible = false;
            this.dgvPriority.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPriority.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvQueue_CellDoubleClick);
            this.dgvPriority.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgvQueue_CellFormatting);

            this.cardPriority.Controls.Add(this.dgvPriority);        // fill first
            this.cardPriority.Controls.Add(this.lblPriorityHead);    // then the heading above it

            this.pnlQueueArea.Controls.Add(this.cardPriority, 0, 0);
            this.pnlQueueArea.Controls.Add(this.cardQueue, 0, 1);

            //
            // pnlFooter — legend on the left (it explains the colours above it), actions right.
            //
            this.pnlFooter.ColumnCount = 2;
            this.pnlFooter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlFooter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.pnlFooter.RowCount = 1;
            this.pnlFooter.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlFooter.Margin = new System.Windows.Forms.Padding(0);
            this.pnlFooter.BackColor = System.Drawing.Color.Transparent;
            this.pnlFooter.Name = "pnlFooter";

            this._lblLegend.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblLegend.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this._lblLegend.ForeColor = CROMS.Modules.UiTheme.Faint;
            this._lblLegend.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._lblLegend.Name = "_lblLegend";
            this._lblLegend.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._lblLegend.UseMnemonic = false;
            this._lblLegend.Text =
                "Waiting time:  under 5 min  ·  5–15 min (amber)  ·  over 15 min (red, recall).    " +
                "Priority lane = Senior Citizen / PWD / Pregnant (RA 11261).    Double-click a row to see every service on that ticket.";

            this.pnlFooterButtons.AutoSize = true;
            this.pnlFooterButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlFooterButtons.WrapContents = false;
            this.pnlFooterButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlFooterButtons.Margin = new System.Windows.Forms.Padding(0);
            this.pnlFooterButtons.BackColor = System.Drawing.Color.Transparent;
            this.pnlFooterButtons.Name = "pnlFooterButtons";

            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnExport.Size = new System.Drawing.Size(104, 30);
            this.btnExport.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.btnExport.Name = "btnExport";
            this.btnExport.Text = "Export CSV";
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);

            this.btnPause.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPause.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnPause.Size = new System.Drawing.Size(116, 30);
            this.btnPause.Margin = new System.Windows.Forms.Padding(0, 2, 10, 0);
            this.btnPause.Name = "btnPause";
            this.btnPause.Text = "Pause Queue";
            this.btnPause.Click += new System.EventHandler(this.btnPause_Click);

            this.pnlFooterButtons.Controls.Add(this.btnExport);
            this.pnlFooterButtons.Controls.Add(this.btnPause);

            this.pnlFooter.Controls.Add(this._lblLegend, 0, 0);
            this.pnlFooter.Controls.Add(this.pnlFooterButtons, 1, 0);

            //
            // rows into the root
            //
            this.layoutRoot.Controls.Add(this.pnlHeader, 0, 0);
            this.layoutRoot.Controls.Add(this.pnlKpis, 0, 1);
            this.layoutRoot.Controls.Add(this.pnlServingHead, 0, 2);
            this.layoutRoot.Controls.Add(this.cardServing, 0, 3);
            this.layoutRoot.Controls.Add(this.pnlWorkflow, 0, 4);
            this.layoutRoot.Controls.Add(this.pnlQueueHead, 0, 5);
            this.layoutRoot.Controls.Add(this.pnlQueueArea, 0, 6);
            this.layoutRoot.Controls.Add(this.pnlFooter, 0, 7);

            //
            // _syncTimer / _clockTimer
            //
            this._syncTimer.Interval = 3000;
            this._syncTimer.Tick += new System.EventHandler(this.syncTimer_Tick);
            this._clockTimer.Interval = 1000;
            this._clockTimer.Tick += new System.EventHandler(this.clockTimer_Tick);

            //
            // QueueManagementForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = CROMS.Modules.UiTheme.PageBg;
            this.ClientSize = new System.Drawing.Size(1449, 837);
            // A short screen scrolls instead of crushing the queue list: a Dock=Fill child
            // shrinks and contributes nothing to the scroll extent, so the floor is stated here.
            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(1120, 880);
            this.Controls.Add(this.layoutRoot);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "QueueManagementForm";
            this.Text = "Queue Management";
            ((System.ComponentModel.ISupportInitialize)(this.dgvQueue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPriority)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>One look for every filter chip; the Tag carries the filter it applies.</summary>
        private void ConfigureChip(Button chip, string name, string text, string key)
        {
            chip.Name = name;
            chip.Text = text;
            chip.Tag = key;
            chip.FlatStyle = FlatStyle.Flat;
            chip.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            chip.BackColor = UiTheme.Chrome;
            chip.ForeColor = UiTheme.Muted;
            chip.AutoSize = false;
            chip.Size = new Size(104, 30);
            chip.Margin = new Padding(0, 8, 6, 0);
            chip.Click += new EventHandler(this.chip_Click);
        }

        #endregion

        // Runtime visual factories (no database access or queue operations).

        /// <summary>
        /// One window card: window name, presence, the queue number, and what a click does.
        /// Painted as a <see cref="CardPanel"/> so it matches every other surface in the app —
        /// a FixedSingle border draws a hard system rectangle the palette does not own.
        /// </summary>
        private static CardPanel CreateWindowCardLayout(string windowName,
            out Label title, out Label presence, out Label codeLbl, out Label subLbl)
        {
            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6),
                MinimumSize = new Size(0, 132),
                Radius = 10,
                DrawShadow = false,          // nested inside cardServing
                BackColor = UiTheme.Surface, // the page behind THIS card is the outer card face
                CardColor = UiTheme.Surface,
                Padding = new Padding(10, 8, 10, 8),
                Cursor = Cursors.Hand
            };
            title = new Label
            {
                Text = windowName,
                Dock = DockStyle.Top,
                Height = 24,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = UiTheme.Ink,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };
            presence = new Label
            {
                Text = "",
                Dock = DockStyle.Top,
                Height = 18,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            codeLbl = new Label
            {
                Text = "—",
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = UiTheme.Accent,
                UseMnemonic = false,
                Font = new Font("Consolas", 26F, FontStyle.Bold)
            };
            subLbl = new Label
            {
                Text = "",
                Dock = DockStyle.Bottom,
                Height = 34,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 8.5F)
            };

            card.Controls.Add(codeLbl);
            card.Controls.Add(subLbl);
            card.Controls.Add(presence);
            card.Controls.Add(title);

            return card;
        }

        private int ResetServingGridLayout(int count)
        {
            _servingGrid.SuspendLayout();
            // Removed cards must be disposed when the active window set changes.
            while (_servingGrid.Controls.Count > 0)
            {
                Control card = _servingGrid.Controls[0];
                _servingGrid.Controls.RemoveAt(0);
                card.Dispose();
            }
            _servingGrid.ColumnStyles.Clear();
            _servingGrid.RowStyles.Clear();

            // One window (an operator signed in to their own) gets a centred card with gutters
            // rather than a card stretched across the whole screen holding one queue number.
            if (count == 1)
            {
                _servingGrid.ColumnCount = 3;
                _servingGrid.RowCount = 1;
                _servingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26F));
                _servingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
                _servingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26F));
                _servingGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                return 3;
            }

            int cols = Math.Max(1, Math.Min(count, 4));
            int rows = Math.Max(1, (int)Math.Ceiling(count / (double)cols));
            _servingGrid.ColumnCount = cols;
            _servingGrid.RowCount = rows;
            for (int c = 0; c < cols; c++)
                _servingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));
            for (int r = 0; r < rows; r++)
                _servingGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));
            return cols;
        }

        private static Label CreateServingPlaceholder(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = UiTheme.Faint,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(10)
            };
        }

        private static Form CreateRecordTypeDialog()
        {
            var dlg = new Form();
            dlg.Text = "New Registration";
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.MinimizeBox = false;
            dlg.MaximizeBox = false;
            dlg.BackColor = UiTheme.Surface;
            dlg.ClientSize = new System.Drawing.Size(320, 120);

            dlg.Controls.Add(new Label
            {
                Text = "What is this client registering?",
                AutoSize = true,
                ForeColor = UiTheme.Ink,
                Location = new System.Drawing.Point(16, 16),
                Font = new System.Drawing.Font("Segoe UI", 9.75F)
            });

            string[] labels = { "Birth", "Marriage", "Death" };
            string[] keys = { "birth", "marriage", "death" };
            for (int i = 0; i < labels.Length; i++)
            {
                var b = new Button
                {
                    Text = labels[i],
                    Tag = keys[i],
                    Location = new System.Drawing.Point(16 + i * 102, 56),
                    Size = new System.Drawing.Size(86, 40),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = UiTheme.Chrome,
                    ForeColor = UiTheme.Ink,
                    Font = new System.Drawing.Font("Segoe UI", 10F)
                };
                dlg.Controls.Add(b);
            }
            return dlg;
        }

        private TableLayoutPanel layoutRoot;
        private TableLayoutPanel pnlHeader;
        private Panel pnlTitle;
        private FlowLayoutPanel pnlHeaderRight;
        private Panel pnlClock;
        private Label _lblClock;
        private Label _lblClockDate;
        private TableLayoutPanel pnlKpis;
        private KpiCard _kpiWaiting;
        private KpiCard _kpiAvgWait;
        private KpiCard _kpiServed;
        private KpiCard _kpiLongest;
        private Panel pnlServingHead;
        private Label lblServingTitle;
        private Label lblServingHint;
        private CardPanel cardServing;
        private TableLayoutPanel pnlWorkflow;
        private FlowLayoutPanel pnlWorkflowButtons;
        private TableLayoutPanel pnlQueueHead;
        private FlowLayoutPanel pnlChips;
        private Button _chipAll;
        private Button _chipNewReg;
        private Button _chipCtc;
        private Button _chipMarriage;
        private Button _chipDeath;
        private Button _chipPetition;
        private Button _chipClaim;
        private TextBox _txtSearch;
        private TableLayoutPanel pnlQueueArea;
        private CardPanel cardQueue;
        private CardPanel cardPriority;
        private Label lblPriorityHead;
        private DataGridView dgvPriority;
        private TableLayoutPanel pnlFooter;
        private Label _lblLegend;
        private FlowLayoutPanel pnlFooterButtons;

        private TableLayoutPanel _servingGrid;
        private Button _btnCallClient;
        private Button _btnRecall;
        private Button _btnForward;
        private Button btnClientDisplay;
        private Label _lblNextStep;
        private Label _lblSync;
        private System.Windows.Forms.Timer _syncTimer;
        private System.Windows.Forms.Timer _clockTimer;

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.Button btnCallNext;
        private System.Windows.Forms.Label lblLive;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.DataGridView dgvQueue;
    }

    partial class ClientDisplayForm
    {
        private System.ComponentModel.IContainer components;
        private TableLayoutPanel _grid;
        private Label _header;
        private System.Windows.Forms.Timer _timer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            SuspendLayout();
            Text = "CROMS — Now Serving";
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = System.Drawing.Color.FromArgb(17, 24, 39);
            KeyPreview = true;
            KeyDown += ClientDisplayForm_KeyDown;
            DoubleClick += ClientDisplayForm_DoubleClick;

            _header = new Label
            {
                Text = "NOW SERVING",
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 130
            };
            _grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(40),
                BackColor = System.Drawing.Color.Transparent
            };
            Controls.Add(_grid);      // fill first
            Controls.Add(_header);     // then the top header

            _timer = new System.Windows.Forms.Timer(components) { Interval = 2000 };
            _timer.Tick += displayTimer_Tick;
            Load += ClientDisplayForm_Load;
            Name = "ClientDisplayForm";
            ResumeLayout(false);
        }

        private int ResetDisplayGridLayout(int count)
        {
            _grid.SuspendLayout();
            // Removed cards must be disposed when the active window set changes.
            while (_grid.Controls.Count > 0)
            {
                Control card = _grid.Controls[0];
                _grid.Controls.RemoveAt(0);
                card.Dispose();
            }
            _grid.ColumnStyles.Clear();
            _grid.RowStyles.Clear();
            int cols = Math.Max(1, Math.Min(count, 4));
            int rows = Math.Max(1, (int)Math.Ceiling(count / (double)cols));
            _grid.ColumnCount = cols;
            _grid.RowCount = rows;
            for (int c = 0; c < cols; c++)
                _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));
            for (int r = 0; r < rows; r++)
                _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));
            return cols;
        }

        private static Panel CreateDisplayCardLayout(string name, out Label code, out Label sub)
        {
            var card = new Panel { Dock = DockStyle.Fill, Margin = new Padding(16), BackColor = System.Drawing.Color.FromArgb(31, 41, 55) };
            code = new Label
            {
                Text = "—",
                ForeColor = System.Drawing.Color.FromArgb(96, 165, 250),
                Font = new System.Drawing.Font("Consolas", 56F, System.Drawing.FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            sub = new Label
            {
                Text = "idle",
                ForeColor = System.Drawing.Color.FromArgb(148, 163, 184),
                Font = new System.Drawing.Font("Segoe UI", 14F),
                Dock = DockStyle.Bottom,
                Height = 54,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            var title = new Label
            {
                Text = name.ToUpper(),
                ForeColor = System.Drawing.Color.FromArgb(148, 163, 184),
                Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 52,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            card.Controls.Add(code);
            card.Controls.Add(sub);
            card.Controls.Add(title);

            return card;
        }

        private static Label CreateDisplayPlaceholder()
        {
            return new Label
            {
                Text = "No active windows",
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 24F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
        }
    }
}
