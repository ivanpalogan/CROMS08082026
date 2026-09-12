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
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblToday = new System.Windows.Forms.Label();
            this.headerRight = new System.Windows.Forms.FlowLayoutPanel();
            this.pillConnection = new CROMS.Modules.StatusPill();
            this.pillUpdated = new CROMS.Modules.StatusPill();
            this.btnRefresh = new System.Windows.Forms.Button();

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

            this.cardMobile = new CROMS.Modules.CardPanel();
            this.mobileHead = new System.Windows.Forms.TableLayoutPanel();
            this.lblMobileTitle = new System.Windows.Forms.Label();
            this.pillMobile = new CROMS.Modules.StatusPill();
            this.pnlMobileConn = new System.Windows.Forms.Panel();
            this.picMobileQr = new System.Windows.Forms.PictureBox();
            this.lblMobileMsg = new System.Windows.Forms.Label();
            this.lblMobileHint = new System.Windows.Forms.Label();
            this.lblMobileUrl = new System.Windows.Forms.Label();

            this.cardAttention = new CROMS.Modules.CardPanel();
            this.attentionHead = new System.Windows.Forms.TableLayoutPanel();
            this.lblAttentionTitle = new System.Windows.Forms.Label();
            this.lblAttentionBasis = new System.Windows.Forms.Label();
            this.pnlAttention = new System.Windows.Forms.Panel();

            this.insightsBlock = new System.Windows.Forms.TableLayoutPanel();
            this.insightsHead = new System.Windows.Forms.TableLayoutPanel();
            this.lblInsightsTitle = new System.Windows.Forms.Label();
            this.pnlInsightsRule = new System.Windows.Forms.Panel();
            this.lblInsightsNote = new System.Windows.Forms.Label();
            this.pnlInsights = new System.Windows.Forms.TableLayoutPanel();

            this.statusTimer = new System.Windows.Forms.Timer(this.components);

            ((System.ComponentModel.ISupportInitialize)(this.picMobileQr)).BeginInit();
            this.SuspendLayout();

            // =============================================================== root
            // 18/18/22 outer padding, 14px between every block. Row 3 is the only 100% row, so
            // growth goes to the content grid rather than to a gap.
            this.root.Dock = System.Windows.Forms.DockStyle.Fill;
            this.root.BackColor = UiTheme.PageBg;
            this.root.Padding = new System.Windows.Forms.Padding(18, 18, 22, 0);
            this.root.ColumnCount = 1;
            this.root.RowCount = 4;
            this.root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 186F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.root.Controls.Add(this.header, 0, 0);
            this.root.Controls.Add(this.kpiRow, 0, 1);
            this.root.Controls.Add(this.contentGrid, 0, 2);
            this.root.Controls.Add(this.insightsBlock, 0, 3);

            // ============================================================= header
            this.header.Dock = System.Windows.Forms.DockStyle.Fill;
            this.header.AutoSize = true;
            this.header.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.header.BackColor = System.Drawing.Color.Transparent;
            this.header.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
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
            this.headerLeft.Controls.Add(this.lblTitle, 0, 0);
            this.headerLeft.Controls.Add(this.lblToday, 0, 1);

            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = UiTheme.Ink;
            this.lblTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Text = "Dashboard";

            // The date + who is signed in. Replaces the old 16pt "Today" heading — the date is
            // context for the numbers, not the name of the screen.
            this.lblToday.AutoSize = true;
            this.lblToday.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblToday.ForeColor = UiTheme.Muted;
            this.lblToday.Margin = new System.Windows.Forms.Padding(0);
            this.lblToday.Name = "lblToday";
            this.lblToday.Text = "—";

            this.headerRight.AutoSize = true;
            this.headerRight.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.headerRight.BackColor = System.Drawing.Color.Transparent;
            this.headerRight.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.headerRight.WrapContents = false;
            this.headerRight.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.headerRight.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.headerRight.Controls.Add(this.btnRefresh);
            this.headerRight.Controls.Add(this.pillUpdated);
            this.headerRight.Controls.Add(this.pillConnection);

            this.btnRefresh.AutoSize = true;
            this.btnRefresh.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnRefresh.BackColor = UiTheme.Chrome;
            this.btnRefresh.ForeColor = UiTheme.Ink;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnRefresh.Padding = new System.Windows.Forms.Padding(14, 7, 14, 7);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(9, 0, 0, 0);
            this.btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = false;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            this.pillUpdated.Margin = new System.Windows.Forms.Padding(9, 0, 0, 0);
            this.pillUpdated.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pillUpdated.Name = "pillUpdated";
            this.pillUpdated.Text = "Updated —";

            this.pillConnection.ShowDot = true;
            this.pillConnection.Margin = new System.Windows.Forms.Padding(0);
            this.pillConnection.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pillConnection.Name = "pillConnection";
            this.pillConnection.Text = "Database —";

            // ============================================================ KPI row
            this.kpiRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kpiRow.BackColor = System.Drawing.Color.Transparent;
            this.kpiRow.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
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
            this.cardWaiting.Margin = new System.Windows.Forms.Padding(0, 0, 14, 0);
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
            this.cardWaiting.SparkStyle = CROMS.Modules.KpiCard.Spark.Area;
            this.cardWaiting.SparkColor = UiTheme.Accent;

            // Card 2 — REGISTERED TODAY. No Tag: nothing to click through to.
            this.cardRegistered.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardRegistered.Margin = new System.Windows.Forms.Padding(0, 0, 14, 0);
            this.cardRegistered.Name = "cardRegistered";
            this.cardRegistered.IconKind = CROMS.Modules.KpiCard.Icon.DocumentTick;
            this.cardRegistered.Tint = UiTheme.SuccessTint;
            this.cardRegistered.Accent = UiTheme.Success;
            this.cardRegistered.Label = "Registered today";
            this.cardRegistered.Value = "0";
            this.cardRegistered.Caption = "births + marriages + deaths";
            this.cardRegistered.CaptionIsAction = false;
            this.cardRegistered.CaptionColor = UiTheme.Faint;
            this.cardRegistered.SparkStyle = CROMS.Modules.KpiCard.Spark.Bars;
            this.cardRegistered.SparkColor = UiTheme.Success;

            // Card 3 — COLLECTIONS TODAY → Fees & Payments
            this.cardCollections.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardCollections.Margin = new System.Windows.Forms.Padding(0, 0, 14, 0);
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
            this.cardCollections.SparkStyle = CROMS.Modules.KpiCard.Spark.Area;
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
            this.cardPending.SparkStyle = CROMS.Modules.KpiCard.Spark.Area;
            this.cardPending.SparkColor = UiTheme.Danger;

            // ======================================================= content grid
            this.contentGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentGrid.BackColor = System.Drawing.Color.Transparent;
            this.contentGrid.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
            this.contentGrid.ColumnCount = 3;
            this.contentGrid.RowCount = 1;
            this.contentGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.contentGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentGrid.Controls.Add(this.leftStack, 0, 0);
            this.contentGrid.Controls.Add(this.rightStack, 2, 0);
            this.contentGrid.SetColumnSpan(this.leftStack, 2);

            this.leftStack.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftStack.BackColor = System.Drawing.Color.Transparent;
            this.leftStack.Margin = new System.Windows.Forms.Padding(0, 0, 14, 0);
            this.leftStack.ColumnCount = 1;
            this.leftStack.RowCount = 2;
            this.leftStack.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftStack.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 236F));
            this.leftStack.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftStack.Controls.Add(this.cardTrend, 0, 0);
            this.leftStack.Controls.Add(this.cardWindows, 0, 1);

            this.rightStack.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightStack.BackColor = System.Drawing.Color.Transparent;
            this.rightStack.Margin = new System.Windows.Forms.Padding(0);
            this.rightStack.ColumnCount = 1;
            this.rightStack.RowCount = 2;
            this.rightStack.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightStack.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.rightStack.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightStack.Controls.Add(this.cardMobile, 0, 0);
            this.rightStack.Controls.Add(this.cardAttention, 0, 1);

            // ========================================================= trend card
            this.cardTrend.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardTrend.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
            this.cardTrend.Name = "cardTrend";
            this.cardTrend.Controls.Add(this.pnlTrend);
            this.cardTrend.Controls.Add(this.trendHead);

            this.trendHead.Dock = System.Windows.Forms.DockStyle.Top;
            this.trendHead.AutoSize = true;
            this.trendHead.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.trendHead.BackColor = System.Drawing.Color.Transparent;
            this.trendHead.ColumnCount = 2;
            this.trendHead.RowCount = 1;
            this.trendHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.trendHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.trendHead.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.trendHead.Controls.Add(this.trendTitles, 0, 0);
            this.trendHead.Controls.Add(this.pnlTrendLegend, 1, 0);

            this.trendTitles.AutoSize = true;
            this.trendTitles.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.trendTitles.BackColor = System.Drawing.Color.Transparent;
            this.trendTitles.Margin = new System.Windows.Forms.Padding(0);
            this.trendTitles.ColumnCount = 1;
            this.trendTitles.RowCount = 2;
            this.trendTitles.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.trendTitles.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.trendTitles.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.trendTitles.Controls.Add(this.lblTrendTitle, 0, 0);
            this.trendTitles.Controls.Add(this.lblTrendBasis, 0, 1);

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
            this.cardWindows.Margin = new System.Windows.Forms.Padding(0);
            this.cardWindows.Name = "cardWindows";
            this.cardWindows.Controls.Add(this.pnlWindows);
            this.cardWindows.Controls.Add(this.windowsHead);

            this.windowsHead.Dock = System.Windows.Forms.DockStyle.Top;
            this.windowsHead.AutoSize = true;
            this.windowsHead.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.windowsHead.BackColor = System.Drawing.Color.Transparent;
            this.windowsHead.ColumnCount = 2;
            this.windowsHead.RowCount = 1;
            this.windowsHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.windowsHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.windowsHead.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.windowsHead.Controls.Add(this.windowsTitles, 0, 0);
            this.windowsHead.Controls.Add(this.btnAssignWindows, 1, 0);

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

            // ======================================================== mobile card
            this.cardMobile.AutoSize = true;
            this.cardMobile.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.cardMobile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardMobile.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
            this.cardMobile.Name = "cardMobile";
            this.cardMobile.Controls.Add(this.pnlMobileConn);
            this.cardMobile.Controls.Add(this.mobileHead);

            this.mobileHead.Dock = System.Windows.Forms.DockStyle.Top;
            this.mobileHead.AutoSize = true;
            this.mobileHead.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mobileHead.BackColor = System.Drawing.Color.Transparent;
            this.mobileHead.ColumnCount = 2;
            this.mobileHead.RowCount = 1;
            this.mobileHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mobileHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.mobileHead.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.mobileHead.Controls.Add(this.lblMobileTitle, 0, 0);
            this.mobileHead.Controls.Add(this.pillMobile, 1, 0);

            this.lblMobileTitle.AutoSize = true;
            this.lblMobileTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblMobileTitle.ForeColor = UiTheme.Ink;
            this.lblMobileTitle.Margin = new System.Windows.Forms.Padding(16, 14, 8, 0);
            this.lblMobileTitle.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblMobileTitle.Name = "lblMobileTitle";
            this.lblMobileTitle.Text = "Mobile app connection";

            this.pillMobile.ShowDot = true;
            this.pillMobile.Margin = new System.Windows.Forms.Padding(0, 12, 16, 0);
            this.pillMobile.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.pillMobile.Name = "pillMobile";
            this.pillMobile.Text = "Connecting";

            // Fixed height: the QR, the URL and the paired-phone lines come and go with the
            // server's state, and letting the card resize itself would make the whole right
            // column jump every time a phone connects.
            this.pnlMobileConn.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlMobileConn.Height = 270;
            this.pnlMobileConn.BackColor = System.Drawing.Color.Transparent;
            this.pnlMobileConn.Name = "pnlMobileConn";
            this.pnlMobileConn.Controls.Add(this.picMobileQr);
            this.pnlMobileConn.Controls.Add(this.lblMobileMsg);
            this.pnlMobileConn.Controls.Add(this.lblMobileHint);
            this.pnlMobileConn.Controls.Add(this.lblMobileUrl);

            this.picMobileQr.BackColor = UiTheme.Surface;
            this.picMobileQr.Size = new System.Drawing.Size(150, 150);
            this.picMobileQr.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picMobileQr.TabStop = false;
            this.picMobileQr.Visible = false;
            this.picMobileQr.Name = "picMobileQr";
            this.picMobileQr.Click += new System.EventHandler(this.picMobileQr_Click);

            this.lblMobileMsg.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblMobileMsg.ForeColor = UiTheme.Danger;
            this.lblMobileMsg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblMobileMsg.BackColor = System.Drawing.Color.Transparent;
            this.lblMobileMsg.Visible = false;
            this.lblMobileMsg.Name = "lblMobileMsg";
            this.lblMobileMsg.Text = "Mobile App Connection Unavailable";

            this.lblMobileHint.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblMobileHint.ForeColor = UiTheme.Faint;
            this.lblMobileHint.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.lblMobileHint.BackColor = System.Drawing.Color.Transparent;
            this.lblMobileHint.Visible = false;
            this.lblMobileHint.Name = "lblMobileHint";
            this.lblMobileHint.Text = "Scan with the phone, or type this address in the phone browser.";

            this.lblMobileUrl.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold);
            this.lblMobileUrl.ForeColor = UiTheme.Accent;
            this.lblMobileUrl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblMobileUrl.BackColor = System.Drawing.Color.Transparent;
            this.lblMobileUrl.Visible = false;
            this.lblMobileUrl.Name = "lblMobileUrl";

            // ============================================== needs attention card
            this.cardAttention.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardAttention.Margin = new System.Windows.Forms.Padding(0);
            this.cardAttention.Name = "cardAttention";
            this.cardAttention.Controls.Add(this.pnlAttention);
            this.cardAttention.Controls.Add(this.attentionHead);

            this.attentionHead.Dock = System.Windows.Forms.DockStyle.Top;
            this.attentionHead.AutoSize = true;
            this.attentionHead.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.attentionHead.BackColor = System.Drawing.Color.Transparent;
            this.attentionHead.ColumnCount = 1;
            this.attentionHead.RowCount = 2;
            this.attentionHead.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.attentionHead.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.attentionHead.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.attentionHead.Controls.Add(this.lblAttentionTitle, 0, 0);
            this.attentionHead.Controls.Add(this.lblAttentionBasis, 0, 1);

            this.lblAttentionTitle.AutoSize = true;
            this.lblAttentionTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblAttentionTitle.ForeColor = UiTheme.Ink;
            this.lblAttentionTitle.Margin = new System.Windows.Forms.Padding(16, 13, 16, 0);
            this.lblAttentionTitle.Name = "lblAttentionTitle";
            this.lblAttentionTitle.Text = "Needs attention";

            this.lblAttentionBasis.AutoSize = false;
            this.lblAttentionBasis.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblAttentionBasis.ForeColor = UiTheme.Faint;
            this.lblAttentionBasis.Margin = new System.Windows.Forms.Padding(16, 2, 16, 0);
            this.lblAttentionBasis.Height = 30;
            this.lblAttentionBasis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAttentionBasis.Name = "lblAttentionBasis";
            this.lblAttentionBasis.Text = "Ignores the date filter — a backlog you filter is a backlog you hide";

            this.pnlAttention.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlAttention.AutoScroll = true;
            this.pnlAttention.BackColor = System.Drawing.Color.Transparent;
            this.pnlAttention.Padding = new System.Windows.Forms.Padding(0, 6, 0, 8);
            this.pnlAttention.Name = "pnlAttention";

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

            // ========================================================== timer
            this.statusTimer.Interval = 3000;
            this.statusTimer.Tick += new System.EventHandler(this.statusTimer_Tick);

            // ====================================================== the form
            // Below this the screen cannot hold its own content, and a Dock=Fill child SHRINKS
            // rather than scrolling — at 1366x768 the service-window board, the mobile card and
            // the backlog list vanished entirely instead of moving below the fold. AutoScroll
            // alone does NOT fix that: a docked child adds nothing to the scroll extent, so the
            // floor has to be stated here as AutoScrollMinSize (MinimumSize on the child is
            // ignored by the layout and was measured doing nothing).
            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(1100, 940);
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = UiTheme.PageBg;
            this.ClientSize = new System.Drawing.Size(1400, 1010);
            this.Controls.Add(this.root);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "DashboardForm";
            this.Text = "Dashboard";
            ((System.ComponentModel.ISupportInitialize)(this.picMobileQr)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel root;
        private System.Windows.Forms.TableLayoutPanel header;
        private System.Windows.Forms.TableLayoutPanel headerLeft;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblToday;
        private System.Windows.Forms.FlowLayoutPanel headerRight;
        private CROMS.Modules.StatusPill pillConnection;
        private CROMS.Modules.StatusPill pillUpdated;
        private System.Windows.Forms.Button btnRefresh;

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

        private CROMS.Modules.CardPanel cardMobile;
        private System.Windows.Forms.TableLayoutPanel mobileHead;
        private System.Windows.Forms.Label lblMobileTitle;
        private CROMS.Modules.StatusPill pillMobile;
        private System.Windows.Forms.Panel pnlMobileConn;
        private System.Windows.Forms.PictureBox picMobileQr;
        private System.Windows.Forms.Label lblMobileMsg;
        private System.Windows.Forms.Label lblMobileHint;
        private System.Windows.Forms.Label lblMobileUrl;

        private CROMS.Modules.CardPanel cardAttention;
        private System.Windows.Forms.TableLayoutPanel attentionHead;
        private System.Windows.Forms.Label lblAttentionTitle;
        private System.Windows.Forms.Label lblAttentionBasis;
        private System.Windows.Forms.Panel pnlAttention;

        private System.Windows.Forms.TableLayoutPanel insightsBlock;
        private System.Windows.Forms.TableLayoutPanel insightsHead;
        private System.Windows.Forms.Label lblInsightsTitle;
        private System.Windows.Forms.Panel pnlInsightsRule;
        private System.Windows.Forms.Label lblInsightsNote;
        private System.Windows.Forms.TableLayoutPanel pnlInsights;

        private System.Windows.Forms.Timer statusTimer;
    }
}
