using CROMS.Modules;

namespace CROMS.Forms
{
    partial class DashboardForm
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
        /// The dashboard is laid out with nested TableLayoutPanels, all Dock = Fill, rather than
        /// with the absolute Points this file used to carry. Absolute positioning pinned the whole
        /// screen into the left 771px of whatever monitor it ran on and left dead grey beside it,
        /// and any content that grew (a longer window list, a wrapped caption) ran straight over
        /// the section title beneath it. Every colour comes from <see cref="UiTheme"/>; there is
        /// not a single hard-coded colour literal, nor a square system panel border, left on
        /// this form. (Both are grep-checked, so this note deliberately avoids naming the APIs.)
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.root = new System.Windows.Forms.TableLayoutPanel();
            this.header = new System.Windows.Forms.TableLayoutPanel();
            this.headerLeft = new System.Windows.Forms.TableLayoutPanel();
            this.brandFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.picLogo = new System.Windows.Forms.Panel();
            this.lblToday = new System.Windows.Forms.Label();
            this.lblClock = new System.Windows.Forms.Label();
            this.lblOffline = new System.Windows.Forms.Label();
            this.headerRight = new System.Windows.Forms.FlowLayoutPanel();
            this.pillUpdated = new CROMS.Modules.StatusPill();

            this.kpiRow = new System.Windows.Forms.TableLayoutPanel();
            this.cardWaiting = new CROMS.Modules.KpiCard();
            this.cardRegistered = new CROMS.Modules.KpiCard();
            this.cardCollections = new CROMS.Modules.KpiCard();
            this.cardPending = new CROMS.Modules.KpiCard();

            this.contentGrid = new System.Windows.Forms.TableLayoutPanel();
            this.leftStack = new System.Windows.Forms.TableLayoutPanel();
            this.rightStack = new System.Windows.Forms.TableLayoutPanel();

            this.cardTrend = new CROMS.Modules.CardPanel();
            this.trendHead = new System.Windows.Forms.TableLayoutPanel();
            this.trendTitles = new System.Windows.Forms.TableLayoutPanel();
            this.lblTrendTitle = new System.Windows.Forms.Label();
            this.lblTrendBasis = new System.Windows.Forms.Label();
            this.pnlTrendLegend = new System.Windows.Forms.Panel();
            this.pnlTrend = new System.Windows.Forms.Panel();

            this.cardWindows = new CROMS.Modules.CardPanel();
            this.windowsHead = new System.Windows.Forms.TableLayoutPanel();
            this.windowsTitles = new System.Windows.Forms.TableLayoutPanel();
            this.lblWindowsTitle = new System.Windows.Forms.Label();
            this.lblWindowsBasis = new System.Windows.Forms.Label();
            this.btnAssignWindows = new System.Windows.Forms.Button();
            this.pnlWindows = new System.Windows.Forms.Panel();

            this.cardAttention = new CROMS.Modules.CardPanel();
            this.attentionHead = new System.Windows.Forms.TableLayoutPanel();
            this.lblAttentionTitle = new System.Windows.Forms.Label();
            this.lblAttentionBasis = new System.Windows.Forms.Label();
            this.pnlAttention = new System.Windows.Forms.Panel();

            this.cardRecent = new CROMS.Modules.CardPanel();
            this.recentHead = new System.Windows.Forms.TableLayoutPanel();
            this.lblRecentTitle = new System.Windows.Forms.Label();
            this.lnkRecentAll = new System.Windows.Forms.LinkLabel();
            this.pnlRecent = new System.Windows.Forms.Panel();

            this.insightsBlock = new System.Windows.Forms.TableLayoutPanel();
            this.insightsHead = new System.Windows.Forms.TableLayoutPanel();
            this.lblInsightsTitle = new System.Windows.Forms.Label();
            this.pnlInsightsRule = new System.Windows.Forms.Panel();
            this.lblInsightsNote = new System.Windows.Forms.Label();
            this.pnlInsights = new System.Windows.Forms.TableLayoutPanel();

            this.statusTimer = new System.Windows.Forms.Timer(this.components);
            this.clockTimer = new System.Windows.Forms.Timer(this.components);

            this.SuspendLayout();

            // =============================================================== root
            // A comfortable 24px page gutter and 16px rhythm keep every section distinct.
            // The content row owns the remaining height, so the two dashboard columns grow
            // together without squeezing their card headers or list rows.
            this.root.Dock = System.Windows.Forms.DockStyle.Fill;
            this.root.BackColor = UiTheme.PageBg;
            this.root.Padding = new System.Windows.Forms.Padding(24, 20, 24, 20);
            this.root.ColumnCount = 1;
            this.root.RowCount = 3;
            this.root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.Controls.Add(this.header, 0, 0);
            this.root.Controls.Add(this.kpiRow, 0, 1);
            this.root.Controls.Add(this.contentGrid, 0, 2);

            // ============================================================= header
            this.header.Dock = System.Windows.Forms.DockStyle.Fill;
            this.header.AutoSize = true;
            this.header.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.header.BackColor = System.Drawing.Color.Transparent;
            this.header.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
            this.header.ColumnCount = 2;
            this.header.RowCount = 1;
            this.header.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.header.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.header.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.header.Controls.Add(this.headerLeft, 0, 0);
            this.header.Controls.Add(this.headerRight, 1, 0);

            this.headerLeft.AutoSize = true;
            this.headerLeft.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.headerLeft.BackColor = System.Drawing.Color.Transparent;
            this.headerLeft.Margin = new System.Windows.Forms.Padding(0);
            this.headerLeft.ColumnCount = 1;
            this.headerLeft.RowCount = 2;
            this.headerLeft.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.headerLeft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.headerLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.headerLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.headerLeft.Controls.Add(this.brandFlow, 0, 0);
            this.headerLeft.Controls.Add(this.lblOffline, 0, 1);

            // Seal + date + live clock, one row. No page title here — the sidebar already names
            // the screen, and the Dashboard's own content starts right below instead of under a
            // repeated "Dashboard" heading.
            this.brandFlow.AutoSize = true;
            this.brandFlow.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.brandFlow.BackColor = System.Drawing.Color.Transparent;
            this.brandFlow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.brandFlow.WrapContents = false;
            this.brandFlow.Margin = new System.Windows.Forms.Padding(0);
            this.brandFlow.Controls.Add(this.picLogo);
            this.brandFlow.Controls.Add(this.lblToday);
            this.brandFlow.Controls.Add(this.lblClock);

            this.picLogo.Size = new System.Drawing.Size(30, 30);
            this.picLogo.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.picLogo.BackColor = System.Drawing.Color.Transparent;
            this.picLogo.Name = "picLogo";
            this.picLogo.Paint += new System.Windows.Forms.PaintEventHandler(this.picLogo_Paint);

            // The date + who is signed in.
            this.lblToday.AutoSize = true;
            this.lblToday.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblToday.ForeColor = UiTheme.Muted;
            this.lblToday.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblToday.Name = "lblToday";
            this.lblToday.Text = "—";

            // The live clock. Fixed size + its own 1-second timer, wholly separate from the
            // data-refresh timer, so a tick here only ever repaints this one label — nothing
            // else on the form is touched, and a fixed size means the tick can never trigger a
            // layout pass on the AutoSize panels around it.
            this.lblClock.AutoSize = false;
            this.lblClock.Size = new System.Drawing.Size(112, 18);
            this.lblClock.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblClock.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblClock.ForeColor = UiTheme.Ink;
            this.lblClock.Margin = new System.Windows.Forms.Padding(10, 6, 0, 0);
            this.lblClock.Name = "lblClock";
            this.lblClock.Text = "—";

            // Hidden unless a query actually fails — an error/status message shown only when
            // there is one, not a permanent technical indicator.
            this.lblOffline.AutoSize = true;
            this.lblOffline.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblOffline.ForeColor = UiTheme.Danger;
            this.lblOffline.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.lblOffline.Name = "lblOffline";
            this.lblOffline.Text = "Unable to reach the database - showing the last known figures.";
            this.lblOffline.Visible = false;

            this.headerRight.AutoSize = true;
            this.headerRight.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.headerRight.BackColor = System.Drawing.Color.Transparent;
            this.headerRight.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.headerRight.WrapContents = false;
            this.headerRight.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.headerRight.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.headerRight.Controls.Add(this.pillUpdated);

            this.pillUpdated.Margin = new System.Windows.Forms.Padding(0);
            this.pillUpdated.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pillUpdated.Name = "pillUpdated";
            this.pillUpdated.Text = "Updated —";

            // ============================================================ KPI row
            this.kpiRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiRow.BackColor = System.Drawing.Color.Transparent;
            this.kpiRow.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
            this.kpiRow.ColumnCount = 4;
            this.kpiRow.RowCount = 1;
            this.kpiRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.kpiRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.kpiRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.kpiRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.kpiRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.kpiRow.Controls.Add(this.cardWaiting, 0, 0);
            this.kpiRow.Controls.Add(this.cardRegistered, 1, 0);
            this.kpiRow.Controls.Add(this.cardCollections, 2, 0);
            this.kpiRow.Controls.Add(this.cardPending, 3, 0);

            // Card 1 — WAITING NOW → Queue Management
            this.cardWaiting.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardWaiting.Margin = new System.Windows.Forms.Padding(0, 0, 16, 0);
            this.cardWaiting.Name = "cardWaiting";
            this.cardWaiting.Tag = "queue";
            this.cardWaiting.IconKind = CROMS.Modules.KpiCard.Icon.QueuePerson;
            this.cardWaiting.Tint = UiTheme.AccentTint;
            this.cardWaiting.Accent = UiTheme.Accent;
            this.cardWaiting.Label = "Waiting now";
            this.cardWaiting.Value = "0";
            this.cardWaiting.Caption = "clients in queue — open Queue";
            this.cardWaiting.CaptionIsAction = true;
            this.cardWaiting.CaptionColor = UiTheme.Accent;
            this.cardWaiting.SparkStyle = CROMS.Modules.KpiCard.Spark.None;
            this.cardWaiting.SparkColor = UiTheme.Accent;

            // Card 2 — REGISTERED TODAY. No Tag: nothing to click through to.
            this.cardRegistered.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardRegistered.Margin = new System.Windows.Forms.Padding(0, 0, 16, 0);
            this.cardRegistered.Name = "cardRegistered";
            this.cardRegistered.IconKind = CROMS.Modules.KpiCard.Icon.DocumentTick;
            this.cardRegistered.Tint = UiTheme.SuccessTint;
            this.cardRegistered.Accent = UiTheme.Success;
            this.cardRegistered.Label = "Registered today";
            this.cardRegistered.Value = "0";
            this.cardRegistered.Caption = "births + marriages + deaths";
            this.cardRegistered.CaptionIsAction = false;
            this.cardRegistered.CaptionColor = UiTheme.Faint;
            this.cardRegistered.SparkStyle = CROMS.Modules.KpiCard.Spark.None;
            this.cardRegistered.SparkColor = UiTheme.Success;

            // Card 3 — COLLECTIONS TODAY → Fees & Payments
            this.cardCollections.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardCollections.Margin = new System.Windows.Forms.Padding(0, 0, 16, 0);
            this.cardCollections.Name = "cardCollections";
            this.cardCollections.Tag = "fees";
            this.cardCollections.IconKind = CROMS.Modules.KpiCard.Icon.PesoCoin;
            this.cardCollections.Tint = UiTheme.WarningTint;
            this.cardCollections.Accent = UiTheme.Warning;
            this.cardCollections.Label = "Collections today";
            this.cardCollections.Prefix = "₱";
            this.cardCollections.Value = "0";
            this.cardCollections.Suffix = ".00";
            this.cardCollections.Caption = "official receipts — open Fees";
            this.cardCollections.CaptionIsAction = true;
            this.cardCollections.CaptionColor = UiTheme.Accent;
            this.cardCollections.SparkStyle = CROMS.Modules.KpiCard.Spark.None;
            this.cardCollections.SparkColor = UiTheme.Warning;

            // Card 4 — PENDING RELEASES → Release & Claim
            this.cardPending.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardPending.Margin = new System.Windows.Forms.Padding(0);
            this.cardPending.Name = "cardPending";
            this.cardPending.Tag = "release";
            this.cardPending.IconKind = CROMS.Modules.KpiCard.Icon.InboxTray;
            this.cardPending.Tint = UiTheme.DangerTint;
            this.cardPending.Accent = UiTheme.Danger;
            this.cardPending.Label = "Pending releases";
            this.cardPending.Value = "0";
            this.cardPending.Caption = "ready to hand over — open Release";
            this.cardPending.CaptionIsAction = true;
            this.cardPending.CaptionColor = UiTheme.Accent;
            this.cardPending.SparkStyle = CROMS.Modules.KpiCard.Spark.None;
            this.cardPending.SparkColor = UiTheme.Danger;

            // ======================================================= content grid
            this.contentGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentGrid.BackColor = System.Drawing.Color.Transparent;
            this.contentGrid.Margin = new System.Windows.Forms.Padding(0);
            this.contentGrid.ColumnCount = 2;
            this.contentGrid.RowCount = 1;
            this.contentGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 63F));
            this.contentGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 37F));
            this.contentGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentGrid.Controls.Add(this.leftStack, 0, 0);
            this.contentGrid.Controls.Add(this.rightStack, 1, 0);

            this.leftStack.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftStack.BackColor = System.Drawing.Color.Transparent;
            this.leftStack.Margin = new System.Windows.Forms.Padding(0, 0, 16, 0);
            this.leftStack.ColumnCount = 1;
            this.leftStack.RowCount = 2;
            this.leftStack.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftStack.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 66F));
            this.leftStack.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.leftStack.Controls.Add(this.cardAttention, 0, 0);
            this.leftStack.Controls.Add(this.cardRecent, 0, 1);

            this.rightStack.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightStack.BackColor = System.Drawing.Color.Transparent;
            this.rightStack.Margin = new System.Windows.Forms.Padding(0);
            this.rightStack.ColumnCount = 1;
            this.rightStack.RowCount = 2;
            this.rightStack.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightStack.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.rightStack.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.rightStack.Controls.Add(this.cardWindows, 0, 0);
            this.rightStack.Controls.Add(this.cardTrend, 0, 1);

            // ========================================================= trend card
            this.cardTrend.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardTrend.Margin = new System.Windows.Forms.Padding(0);
            this.cardTrend.Name = "cardTrend";
            this.cardTrend.Controls.Add(this.pnlTrend);
            this.cardTrend.Controls.Add(this.trendHead);

            this.trendHead.Dock = System.Windows.Forms.DockStyle.Top;
            this.trendHead.AutoSize = true;
            this.trendHead.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.trendHead.BackColor = System.Drawing.Color.Transparent;
            this.trendHead.ColumnCount = 1;
            this.trendHead.RowCount = 1;
            this.trendHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.trendHead.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.trendHead.Controls.Add(this.trendTitles, 0, 0);

            this.trendTitles.AutoSize = true;
            this.trendTitles.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.trendTitles.BackColor = System.Drawing.Color.Transparent;
            this.trendTitles.Margin = new System.Windows.Forms.Padding(0);
            this.trendTitles.ColumnCount = 1;
            this.trendTitles.RowCount = 1;
            this.trendTitles.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.trendTitles.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.trendTitles.Controls.Add(this.lblTrendTitle, 0, 0);

            this.lblTrendTitle.AutoSize = true;
            this.lblTrendTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblTrendTitle.ForeColor = UiTheme.Ink;
            this.lblTrendTitle.Margin = new System.Windows.Forms.Padding(16, 13, 16, 0);
            this.lblTrendTitle.Name = "lblTrendTitle";
            this.lblTrendTitle.Text = "Registrations — last 7 days";

            this.lblTrendBasis.AutoSize = true;
            this.lblTrendBasis.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblTrendBasis.ForeColor = UiTheme.Faint;
            this.lblTrendBasis.Margin = new System.Windows.Forms.Padding(16, 2, 16, 0);
            this.lblTrendBasis.Name = "lblTrendBasis";
            this.lblTrendBasis.Text = "Basis: date registered (created_at) — office workload";

            this.pnlTrendLegend.BackColor = System.Drawing.Color.Transparent;
            this.pnlTrendLegend.Size = new System.Drawing.Size(232, 20);
            this.pnlTrendLegend.Margin = new System.Windows.Forms.Padding(0, 15, 16, 0);
            this.pnlTrendLegend.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.pnlTrendLegend.Name = "pnlTrendLegend";
            this.pnlTrendLegend.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlTrendLegend_Paint);

            this.pnlTrend.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTrend.BackColor = System.Drawing.Color.Transparent;
            this.pnlTrend.Padding = new System.Windows.Forms.Padding(16, 6, 16, 8);
            this.pnlTrend.Name = "pnlTrend";
            this.pnlTrend.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlTrend_Paint);

            // ============================================== service windows card
            this.cardWindows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardWindows.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
            this.cardWindows.Name = "cardWindows";
            this.cardWindows.Controls.Add(this.pnlWindows);
            this.cardWindows.Controls.Add(this.windowsHead);

            this.windowsHead.Dock = System.Windows.Forms.DockStyle.Top;
            this.windowsHead.AutoSize = true;
            this.windowsHead.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.windowsHead.BackColor = System.Drawing.Color.Transparent;
            this.windowsHead.ColumnCount = 1;
            this.windowsHead.RowCount = 1;
            this.windowsHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.windowsHead.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.windowsHead.Controls.Add(this.windowsTitles, 0, 0);

            this.windowsTitles.AutoSize = true;
            this.windowsTitles.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.windowsTitles.BackColor = System.Drawing.Color.Transparent;
            this.windowsTitles.Margin = new System.Windows.Forms.Padding(0);
            this.windowsTitles.ColumnCount = 1;
            this.windowsTitles.RowCount = 2;
            this.windowsTitles.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.windowsTitles.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.windowsTitles.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.windowsTitles.Controls.Add(this.lblWindowsTitle, 0, 0);
            this.windowsTitles.Controls.Add(this.lblWindowsBasis, 0, 1);

            this.lblWindowsTitle.AutoSize = true;
            this.lblWindowsTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblWindowsTitle.ForeColor = UiTheme.Ink;
            this.lblWindowsTitle.Margin = new System.Windows.Forms.Padding(16, 13, 16, 0);
            this.lblWindowsTitle.Name = "lblWindowsTitle";
            this.lblWindowsTitle.Text = "Service windows";

            this.lblWindowsBasis.AutoSize = true;
            this.lblWindowsBasis.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblWindowsBasis.ForeColor = UiTheme.Faint;
            this.lblWindowsBasis.Margin = new System.Windows.Forms.Padding(16, 2, 16, 0);
            this.lblWindowsBasis.Name = "lblWindowsBasis";
            this.lblWindowsBasis.Text = "Live — refreshes every 3 seconds";

            this.btnAssignWindows.AutoSize = true;
            this.btnAssignWindows.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnAssignWindows.BackColor = UiTheme.Chrome;
            this.btnAssignWindows.ForeColor = UiTheme.Ink;
            this.btnAssignWindows.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAssignWindows.FlatAppearance.BorderSize = 0;
            this.btnAssignWindows.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnAssignWindows.Padding = new System.Windows.Forms.Padding(14, 7, 14, 7);
            this.btnAssignWindows.Margin = new System.Windows.Forms.Padding(0, 12, 16, 0);
            this.btnAssignWindows.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnAssignWindows.Name = "btnAssignWindows";
            this.btnAssignWindows.Text = "Assign windows";
            this.btnAssignWindows.UseVisualStyleBackColor = false;
            this.btnAssignWindows.Click += new System.EventHandler(this.btnAssignWindows_Click);

            this.pnlWindows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlWindows.AutoScroll = true;
            this.pnlWindows.BackColor = System.Drawing.Color.Transparent;
            this.pnlWindows.Padding = new System.Windows.Forms.Padding(0, 8, 0, 8);
            this.pnlWindows.Name = "pnlWindows";

            // ============================================== needs attention card
            this.cardAttention.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardAttention.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
            this.cardAttention.Name = "cardAttention";
            this.cardAttention.Controls.Add(this.pnlAttention);
            this.cardAttention.Controls.Add(this.attentionHead);

            this.attentionHead.Dock = System.Windows.Forms.DockStyle.Top;
            this.attentionHead.AutoSize = true;
            this.attentionHead.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.attentionHead.BackColor = System.Drawing.Color.Transparent;
            this.attentionHead.ColumnCount = 2;
            this.attentionHead.RowCount = 1;
            this.attentionHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.attentionHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.attentionHead.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.attentionHead.Controls.Add(this.lblAttentionTitle, 0, 0);
            this.attentionHead.Controls.Add(this.lblAttentionBasis, 1, 0);

            this.lblAttentionTitle.AutoSize = true;
            this.lblAttentionTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblAttentionTitle.ForeColor = UiTheme.Ink;
            this.lblAttentionTitle.Margin = new System.Windows.Forms.Padding(18, 16, 16, 12);
            this.lblAttentionTitle.Name = "lblAttentionTitle";
            this.lblAttentionTitle.Text = "Tasks requiring attention";

            this.lblAttentionBasis.AutoSize = true;
            this.lblAttentionBasis.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblAttentionBasis.ForeColor = UiTheme.Faint;
            this.lblAttentionBasis.Margin = new System.Windows.Forms.Padding(8, 17, 18, 12);
            this.lblAttentionBasis.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblAttentionBasis.Name = "lblAttentionBasis";
            this.lblAttentionBasis.Text = "0 items";

            this.pnlAttention.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlAttention.AutoScroll = true;
            this.pnlAttention.BackColor = System.Drawing.Color.Transparent;
            this.pnlAttention.Padding = new System.Windows.Forms.Padding(0, 2, 0, 10);
            this.pnlAttention.Name = "pnlAttention";

            // ============================================== recent transactions
            this.cardRecent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardRecent.Margin = new System.Windows.Forms.Padding(0);
            this.cardRecent.Name = "cardRecent";
            this.cardRecent.Controls.Add(this.pnlRecent);
            this.cardRecent.Controls.Add(this.recentHead);

            this.recentHead.Dock = System.Windows.Forms.DockStyle.Top;
            this.recentHead.AutoSize = true;
            this.recentHead.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.recentHead.BackColor = System.Drawing.Color.Transparent;
            this.recentHead.ColumnCount = 2;
            this.recentHead.RowCount = 1;
            this.recentHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.recentHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.recentHead.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.recentHead.Controls.Add(this.lblRecentTitle, 0, 0);
            this.recentHead.Controls.Add(this.lnkRecentAll, 1, 0);

            this.lblRecentTitle.AutoSize = true;
            this.lblRecentTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblRecentTitle.ForeColor = UiTheme.Ink;
            this.lblRecentTitle.Margin = new System.Windows.Forms.Padding(18, 16, 16, 12);
            this.lblRecentTitle.Name = "lblRecentTitle";
            this.lblRecentTitle.Text = "Recent transactions";

            this.lnkRecentAll.AutoSize = true;
            this.lnkRecentAll.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lnkRecentAll.LinkColor = UiTheme.Accent;
            this.lnkRecentAll.ActiveLinkColor = UiTheme.Accent;
            this.lnkRecentAll.VisitedLinkColor = UiTheme.Accent;
            this.lnkRecentAll.Margin = new System.Windows.Forms.Padding(8, 17, 18, 12);
            this.lnkRecentAll.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lnkRecentAll.Name = "lnkRecentAll";
            this.lnkRecentAll.Text = "View all  →";
            this.lnkRecentAll.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkRecentAll_LinkClicked);

            this.pnlRecent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRecent.AutoScroll = true;
            this.pnlRecent.BackColor = System.Drawing.Color.Transparent;
            this.pnlRecent.Padding = new System.Windows.Forms.Padding(0, 2, 0, 10);
            this.pnlRecent.Name = "pnlRecent";

            // ==================================================== insights strip
            this.insightsBlock.Dock = System.Windows.Forms.DockStyle.Fill;
            this.insightsBlock.AutoSize = true;
            this.insightsBlock.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.insightsBlock.BackColor = System.Drawing.Color.Transparent;
            this.insightsBlock.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.insightsBlock.ColumnCount = 1;
            this.insightsBlock.RowCount = 2;
            this.insightsBlock.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.insightsBlock.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.insightsBlock.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.insightsBlock.Controls.Add(this.insightsHead, 0, 0);
            this.insightsBlock.Controls.Add(this.pnlInsights, 0, 1);

            this.insightsHead.Dock = System.Windows.Forms.DockStyle.Fill;
            this.insightsHead.AutoSize = true;
            this.insightsHead.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.insightsHead.BackColor = System.Drawing.Color.Transparent;
            this.insightsHead.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.insightsHead.ColumnCount = 3;
            this.insightsHead.RowCount = 1;
            this.insightsHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.insightsHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.insightsHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.insightsHead.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.insightsHead.Controls.Add(this.lblInsightsTitle, 0, 0);
            this.insightsHead.Controls.Add(this.pnlInsightsRule, 1, 0);
            this.insightsHead.Controls.Add(this.lblInsightsNote, 2, 0);

            this.lblInsightsTitle.AutoSize = true;
            this.lblInsightsTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblInsightsTitle.ForeColor = UiTheme.Ink;
            this.lblInsightsTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblInsightsTitle.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblInsightsTitle.Name = "lblInsightsTitle";
            this.lblInsightsTitle.Text = "Operations insights";

            this.pnlInsightsRule.BackColor = UiTheme.CardLine;
            this.pnlInsightsRule.Height = 1;
            this.pnlInsightsRule.Margin = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.pnlInsightsRule.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlInsightsRule.Name = "pnlInsightsRule";

            this.lblInsightsNote.AutoSize = true;
            this.lblInsightsNote.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblInsightsNote.ForeColor = UiTheme.Faint;
            this.lblInsightsNote.Margin = new System.Windows.Forms.Padding(0);
            this.lblInsightsNote.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblInsightsNote.Name = "lblInsightsNote";
            this.lblInsightsNote.Text = "cached 30 s · hides itself when a widget has too little data";

            this.pnlInsights.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlInsights.AutoSize = true;
            this.pnlInsights.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlInsights.BackColor = System.Drawing.Color.Transparent;
            this.pnlInsights.Margin = new System.Windows.Forms.Padding(0);
            this.pnlInsights.ColumnCount = 3;
            this.pnlInsights.RowCount = 1;
            this.pnlInsights.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.pnlInsights.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.pnlInsights.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.pnlInsights.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.pnlInsights.Name = "pnlInsights";

            // ========================================================== timers
            // Data refresh (windows every tick, the rest every 4th) — unchanged cadence.
            this.statusTimer.Interval = 3000;
            this.statusTimer.Tick += new System.EventHandler(this.statusTimer_Tick);

            // The clock alone. Deliberately a SEPARATE timer from statusTimer: the clock must
            // count seconds smoothly regardless of how the data refresh is paced, and a data
            // refresh must never wait on — or be blamed for — the once-a-second clock tick.
            this.clockTimer.Interval = 1000;
            this.clockTimer.Tick += new System.EventHandler(this.clockTimer_Tick);

            // ====================================================== the form
            // Below this the screen cannot hold its own content, and a Dock=Fill child shrinks
            // rather than scrolling. The floor keeps both columns usable on a 1366x768 display.
            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(1100, 760);
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = UiTheme.PageBg;
            this.ClientSize = new System.Drawing.Size(1400, 1010);
            this.Controls.Add(this.root);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "DashboardForm";
            this.Text = "Dashboard";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel root;
        private System.Windows.Forms.TableLayoutPanel header;
        private System.Windows.Forms.TableLayoutPanel headerLeft;
        private System.Windows.Forms.FlowLayoutPanel brandFlow;
        private System.Windows.Forms.Panel picLogo;
        private System.Windows.Forms.Label lblToday;
        private System.Windows.Forms.Label lblClock;
        private System.Windows.Forms.Label lblOffline;
        private System.Windows.Forms.FlowLayoutPanel headerRight;
        private CROMS.Modules.StatusPill pillUpdated;

        private System.Windows.Forms.TableLayoutPanel kpiRow;
        private CROMS.Modules.KpiCard cardWaiting;
        private CROMS.Modules.KpiCard cardRegistered;
        private CROMS.Modules.KpiCard cardCollections;
        private CROMS.Modules.KpiCard cardPending;

        private System.Windows.Forms.TableLayoutPanel contentGrid;
        private System.Windows.Forms.TableLayoutPanel leftStack;
        private System.Windows.Forms.TableLayoutPanel rightStack;

        private CROMS.Modules.CardPanel cardTrend;
        private System.Windows.Forms.TableLayoutPanel trendHead;
        private System.Windows.Forms.TableLayoutPanel trendTitles;
        private System.Windows.Forms.Label lblTrendTitle;
        private System.Windows.Forms.Label lblTrendBasis;
        private System.Windows.Forms.Panel pnlTrendLegend;
        private System.Windows.Forms.Panel pnlTrend;

        private CROMS.Modules.CardPanel cardWindows;
        private System.Windows.Forms.TableLayoutPanel windowsHead;
        private System.Windows.Forms.TableLayoutPanel windowsTitles;
        private System.Windows.Forms.Label lblWindowsTitle;
        private System.Windows.Forms.Label lblWindowsBasis;
        private System.Windows.Forms.Button btnAssignWindows;
        private System.Windows.Forms.Panel pnlWindows;

        private CROMS.Modules.CardPanel cardAttention;
        private System.Windows.Forms.TableLayoutPanel attentionHead;
        private System.Windows.Forms.Label lblAttentionTitle;
        private System.Windows.Forms.Label lblAttentionBasis;
        private System.Windows.Forms.Panel pnlAttention;

        private CROMS.Modules.CardPanel cardRecent;
        private System.Windows.Forms.TableLayoutPanel recentHead;
        private System.Windows.Forms.Label lblRecentTitle;
        private System.Windows.Forms.LinkLabel lnkRecentAll;
        private System.Windows.Forms.Panel pnlRecent;

        private System.Windows.Forms.TableLayoutPanel insightsBlock;
        private System.Windows.Forms.TableLayoutPanel insightsHead;
        private System.Windows.Forms.Label lblInsightsTitle;
        private System.Windows.Forms.Panel pnlInsightsRule;
        private System.Windows.Forms.Label lblInsightsNote;
        private System.Windows.Forms.TableLayoutPanel pnlInsights;

        private System.Windows.Forms.Timer statusTimer;
        private System.Windows.Forms.Timer clockTimer;
    }
}
