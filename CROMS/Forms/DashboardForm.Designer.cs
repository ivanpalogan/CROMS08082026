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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblToday = new System.Windows.Forms.Label();
            this.lblUpdated = new System.Windows.Forms.Label();
            this.lblConnection = new System.Windows.Forms.Label();
            this.cardWaiting = new System.Windows.Forms.Panel();
            this.lblWaitingValue = new System.Windows.Forms.Label();
            this.lblWaitingCap = new System.Windows.Forms.Label();
            this.lblWaitingSub = new System.Windows.Forms.Label();
            this.stripeWaiting = new System.Windows.Forms.Panel();
            this.cardRegistered = new System.Windows.Forms.Panel();
            this.lblRegisteredValue = new System.Windows.Forms.Label();
            this.lblRegisteredCap = new System.Windows.Forms.Label();
            this.lblRegisteredSub = new System.Windows.Forms.Label();
            this.stripeRegistered = new System.Windows.Forms.Panel();
            this.cardCollections = new System.Windows.Forms.Panel();
            this.lblCollectionsValue = new System.Windows.Forms.Label();
            this.lblCollectionsCap = new System.Windows.Forms.Label();
            this.lblCollectionsSub = new System.Windows.Forms.Label();
            this.stripeCollections = new System.Windows.Forms.Panel();
            this.cardPending = new System.Windows.Forms.Panel();
            this.lblPendingValue = new System.Windows.Forms.Label();
            this.lblPendingCap = new System.Windows.Forms.Label();
            this.lblPendingSub = new System.Windows.Forms.Label();
            this.stripePending = new System.Windows.Forms.Panel();
            this.lblTrendTitle = new System.Windows.Forms.Label();
            this.pnlTrend = new System.Windows.Forms.Panel();
            this.lblWindowsTitle = new System.Windows.Forms.Label();
            this.pnlWindows = new System.Windows.Forms.Panel();
            this.statusTimer = new System.Windows.Forms.Timer(this.components);
            this.lblMobileTitle = new System.Windows.Forms.Label();
            this.pnlMobileConn = new System.Windows.Forms.Panel();
            this.lblMobileStatus = new System.Windows.Forms.Label();
            this.picMobileQr = new System.Windows.Forms.PictureBox();
            this.lblMobileMsg = new System.Windows.Forms.Label();
            this.lblMobileHint = new System.Windows.Forms.Label();
            this.lblMobileUrl = new System.Windows.Forms.Label();
            this.cardWaiting.SuspendLayout();
            this.cardRegistered.SuspendLayout();
            this.cardCollections.SuspendLayout();
            this.cardPending.SuspendLayout();
            this.pnlMobileConn.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picMobileQr)).BeginInit();
            this.SuspendLayout();
            // 
            // lblToday
            // 
            this.lblToday.AutoSize = true;
            this.lblToday.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblToday.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblToday.Location = new System.Drawing.Point(21, 14);
            this.lblToday.Name = "lblToday";
            this.lblToday.Size = new System.Drawing.Size(75, 30);
            this.lblToday.TabIndex = 0;
            this.lblToday.Text = "Today";
            // 
            // lblUpdated
            // 
            this.lblUpdated.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblUpdated.AutoSize = true;
            this.lblUpdated.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblUpdated.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblUpdated.Location = new System.Drawing.Point(600, 16);
            this.lblUpdated.Name = "lblUpdated";
            this.lblUpdated.Size = new System.Drawing.Size(67, 15);
            this.lblUpdated.TabIndex = 1;
            this.lblUpdated.Text = "Updated —";
            this.lblUpdated.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblConnection
            // 
            this.lblConnection.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblConnection.AutoSize = true;
            this.lblConnection.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblConnection.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.lblConnection.Location = new System.Drawing.Point(600, 33);
            this.lblConnection.Name = "lblConnection";
            this.lblConnection.Size = new System.Drawing.Size(98, 17);
            this.lblConnection.TabIndex = 2;
            this.lblConnection.Text = "● Database: —";
            this.lblConnection.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // cardWaiting
            // 
            this.cardWaiting.BackColor = System.Drawing.Color.White;
            this.cardWaiting.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardWaiting.Controls.Add(this.lblWaitingValue);
            this.cardWaiting.Controls.Add(this.lblWaitingCap);
            this.cardWaiting.Controls.Add(this.lblWaitingSub);
            this.cardWaiting.Controls.Add(this.stripeWaiting);
            this.cardWaiting.Location = new System.Drawing.Point(21, 55);
            this.cardWaiting.Name = "cardWaiting";
            this.cardWaiting.Size = new System.Drawing.Size(180, 104);
            this.cardWaiting.TabIndex = 3;
            this.cardWaiting.Tag = "queue";
            // 
            // lblWaitingValue
            // 
            this.lblWaitingValue.AutoSize = true;
            this.lblWaitingValue.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold);
            this.lblWaitingValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblWaitingValue.Location = new System.Drawing.Point(15, 10);
            this.lblWaitingValue.Name = "lblWaitingValue";
            this.lblWaitingValue.Size = new System.Drawing.Size(38, 45);
            this.lblWaitingValue.TabIndex = 0;
            this.lblWaitingValue.Text = "0";
            // 
            // lblWaitingCap
            // 
            this.lblWaitingCap.AutoSize = true;
            this.lblWaitingCap.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblWaitingCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(58)))), ((int)(((byte)(64)))));
            this.lblWaitingCap.Location = new System.Drawing.Point(17, 57);
            this.lblWaitingCap.Name = "lblWaitingCap";
            this.lblWaitingCap.Size = new System.Drawing.Size(95, 19);
            this.lblWaitingCap.TabIndex = 1;
            this.lblWaitingCap.Text = "Waiting Now";
            // 
            // lblWaitingSub
            // 
            this.lblWaitingSub.AutoSize = true;
            this.lblWaitingSub.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblWaitingSub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblWaitingSub.Location = new System.Drawing.Point(17, 78);
            this.lblWaitingSub.Name = "lblWaitingSub";
            this.lblWaitingSub.Size = new System.Drawing.Size(121, 13);
            this.lblWaitingSub.TabIndex = 2;
            this.lblWaitingSub.Text = "clients in queue today";
            // 
            // stripeWaiting
            // 
            this.stripeWaiting.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.stripeWaiting.Dock = System.Windows.Forms.DockStyle.Left;
            this.stripeWaiting.Location = new System.Drawing.Point(0, 0);
            this.stripeWaiting.Name = "stripeWaiting";
            this.stripeWaiting.Size = new System.Drawing.Size(5, 102);
            this.stripeWaiting.TabIndex = 3;
            // 
            // cardRegistered
            // 
            this.cardRegistered.BackColor = System.Drawing.Color.White;
            this.cardRegistered.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardRegistered.Controls.Add(this.lblRegisteredValue);
            this.cardRegistered.Controls.Add(this.lblRegisteredCap);
            this.cardRegistered.Controls.Add(this.lblRegisteredSub);
            this.cardRegistered.Controls.Add(this.stripeRegistered);
            this.cardRegistered.Location = new System.Drawing.Point(209, 55);
            this.cardRegistered.Name = "cardRegistered";
            this.cardRegistered.Size = new System.Drawing.Size(180, 104);
            this.cardRegistered.TabIndex = 4;
            // 
            // lblRegisteredValue
            // 
            this.lblRegisteredValue.AutoSize = true;
            this.lblRegisteredValue.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold);
            this.lblRegisteredValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblRegisteredValue.Location = new System.Drawing.Point(15, 10);
            this.lblRegisteredValue.Name = "lblRegisteredValue";
            this.lblRegisteredValue.Size = new System.Drawing.Size(38, 45);
            this.lblRegisteredValue.TabIndex = 0;
            this.lblRegisteredValue.Text = "0";
            // 
            // lblRegisteredCap
            // 
            this.lblRegisteredCap.AutoSize = true;
            this.lblRegisteredCap.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblRegisteredCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(58)))), ((int)(((byte)(64)))));
            this.lblRegisteredCap.Location = new System.Drawing.Point(17, 57);
            this.lblRegisteredCap.Name = "lblRegisteredCap";
            this.lblRegisteredCap.Size = new System.Drawing.Size(126, 19);
            this.lblRegisteredCap.TabIndex = 1;
            this.lblRegisteredCap.Text = "Registered Today";
            // 
            // lblRegisteredSub
            // 
            this.lblRegisteredSub.AutoSize = true;
            this.lblRegisteredSub.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblRegisteredSub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblRegisteredSub.Location = new System.Drawing.Point(17, 78);
            this.lblRegisteredSub.Name = "lblRegisteredSub";
            this.lblRegisteredSub.Size = new System.Drawing.Size(150, 13);
            this.lblRegisteredSub.TabIndex = 2;
            this.lblRegisteredSub.Text = "births + marriages + deaths";
            // 
            // stripeRegistered
            // 
            this.stripeRegistered.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.stripeRegistered.Dock = System.Windows.Forms.DockStyle.Left;
            this.stripeRegistered.Location = new System.Drawing.Point(0, 0);
            this.stripeRegistered.Name = "stripeRegistered";
            this.stripeRegistered.Size = new System.Drawing.Size(5, 102);
            this.stripeRegistered.TabIndex = 3;
            // 
            // cardCollections
            // 
            this.cardCollections.BackColor = System.Drawing.Color.White;
            this.cardCollections.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardCollections.Controls.Add(this.lblCollectionsValue);
            this.cardCollections.Controls.Add(this.lblCollectionsCap);
            this.cardCollections.Controls.Add(this.lblCollectionsSub);
            this.cardCollections.Controls.Add(this.stripeCollections);
            this.cardCollections.Location = new System.Drawing.Point(398, 55);
            this.cardCollections.Name = "cardCollections";
            this.cardCollections.Size = new System.Drawing.Size(180, 104);
            this.cardCollections.TabIndex = 5;
            this.cardCollections.Tag = "fees";
            // 
            // lblCollectionsValue
            // 
            this.lblCollectionsValue.AutoSize = true;
            this.lblCollectionsValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblCollectionsValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblCollectionsValue.Location = new System.Drawing.Point(15, 14);
            this.lblCollectionsValue.Name = "lblCollectionsValue";
            this.lblCollectionsValue.Size = new System.Drawing.Size(89, 37);
            this.lblCollectionsValue.TabIndex = 0;
            this.lblCollectionsValue.Text = "₱0.00";
            // 
            // lblCollectionsCap
            // 
            this.lblCollectionsCap.AutoSize = true;
            this.lblCollectionsCap.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblCollectionsCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(58)))), ((int)(((byte)(64)))));
            this.lblCollectionsCap.Location = new System.Drawing.Point(17, 57);
            this.lblCollectionsCap.Name = "lblCollectionsCap";
            this.lblCollectionsCap.Size = new System.Drawing.Size(127, 19);
            this.lblCollectionsCap.TabIndex = 1;
            this.lblCollectionsCap.Text = "Collections Today";
            // 
            // lblCollectionsSub
            // 
            this.lblCollectionsSub.AutoSize = true;
            this.lblCollectionsSub.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblCollectionsSub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblCollectionsSub.Location = new System.Drawing.Point(17, 78);
            this.lblCollectionsSub.Name = "lblCollectionsSub";
            this.lblCollectionsSub.Size = new System.Drawing.Size(117, 13);
            this.lblCollectionsSub.TabIndex = 2;
            this.lblCollectionsSub.Text = "official receipts today";
            // 
            // stripeCollections
            // 
            this.stripeCollections.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(193)))), ((int)(((byte)(7)))));
            this.stripeCollections.Dock = System.Windows.Forms.DockStyle.Left;
            this.stripeCollections.Location = new System.Drawing.Point(0, 0);
            this.stripeCollections.Name = "stripeCollections";
            this.stripeCollections.Size = new System.Drawing.Size(5, 102);
            this.stripeCollections.TabIndex = 3;
            // 
            // cardPending
            // 
            this.cardPending.BackColor = System.Drawing.Color.White;
            this.cardPending.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardPending.Controls.Add(this.lblPendingValue);
            this.cardPending.Controls.Add(this.lblPendingCap);
            this.cardPending.Controls.Add(this.lblPendingSub);
            this.cardPending.Controls.Add(this.stripePending);
            this.cardPending.Location = new System.Drawing.Point(586, 55);
            this.cardPending.Name = "cardPending";
            this.cardPending.Size = new System.Drawing.Size(180, 104);
            this.cardPending.TabIndex = 6;
            this.cardPending.Tag = "release";
            // 
            // lblPendingValue
            // 
            this.lblPendingValue.AutoSize = true;
            this.lblPendingValue.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold);
            this.lblPendingValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblPendingValue.Location = new System.Drawing.Point(15, 10);
            this.lblPendingValue.Name = "lblPendingValue";
            this.lblPendingValue.Size = new System.Drawing.Size(38, 45);
            this.lblPendingValue.TabIndex = 0;
            this.lblPendingValue.Text = "0";
            // 
            // lblPendingCap
            // 
            this.lblPendingCap.AutoSize = true;
            this.lblPendingCap.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblPendingCap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(58)))), ((int)(((byte)(64)))));
            this.lblPendingCap.Location = new System.Drawing.Point(17, 57);
            this.lblPendingCap.Name = "lblPendingCap";
            this.lblPendingCap.Size = new System.Drawing.Size(125, 19);
            this.lblPendingCap.TabIndex = 1;
            this.lblPendingCap.Text = "Pending Releases";
            // 
            // lblPendingSub
            // 
            this.lblPendingSub.AutoSize = true;
            this.lblPendingSub.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblPendingSub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblPendingSub.Location = new System.Drawing.Point(17, 78);
            this.lblPendingSub.Name = "lblPendingSub";
            this.lblPendingSub.Size = new System.Drawing.Size(143, 13);
            this.lblPendingSub.TabIndex = 2;
            this.lblPendingSub.Text = "ready to hand over — click";
            // 
            // stripePending
            // 
            this.stripePending.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.stripePending.Dock = System.Windows.Forms.DockStyle.Left;
            this.stripePending.Location = new System.Drawing.Point(0, 0);
            this.stripePending.Name = "stripePending";
            this.stripePending.Size = new System.Drawing.Size(5, 102);
            this.stripePending.TabIndex = 3;
            // 
            // lblTrendTitle
            // 
            this.lblTrendTitle.AutoSize = true;
            this.lblTrendTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblTrendTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblTrendTitle.Location = new System.Drawing.Point(21, 177);
            this.lblTrendTitle.Name = "lblTrendTitle";
            this.lblTrendTitle.Size = new System.Drawing.Size(213, 21);
            this.lblTrendTitle.TabIndex = 7;
            this.lblTrendTitle.Text = "Registrations — last 7 days";
            // 
            // pnlTrend
            // 
            this.pnlTrend.BackColor = System.Drawing.Color.White;
            this.pnlTrend.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlTrend.Location = new System.Drawing.Point(21, 201);
            this.pnlTrend.Name = "pnlTrend";
            this.pnlTrend.Size = new System.Drawing.Size(446, 174);
            this.pnlTrend.TabIndex = 8;
            this.pnlTrend.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlTrend_Paint);
            // 
            // lblWindowsTitle
            // 
            this.lblWindowsTitle.AutoSize = true;
            this.lblWindowsTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblWindowsTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblWindowsTitle.Location = new System.Drawing.Point(21, 388);
            this.lblWindowsTitle.Name = "lblWindowsTitle";
            this.lblWindowsTitle.Size = new System.Drawing.Size(141, 21);
            this.lblWindowsTitle.TabIndex = 9;
            this.lblWindowsTitle.Text = "Service Windows";
            // 
            // pnlWindows
            // 
            this.pnlWindows.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.pnlWindows.AutoScroll = true;
            this.pnlWindows.BackColor = System.Drawing.Color.White;
            this.pnlWindows.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlWindows.Location = new System.Drawing.Point(21, 413);
            this.pnlWindows.Name = "pnlWindows";
            this.pnlWindows.Size = new System.Drawing.Size(446, 106);
            this.pnlWindows.TabIndex = 10;
            // 
            // statusTimer
            // 
            this.statusTimer.Interval = 3000;
            this.statusTimer.Tick += new System.EventHandler(this.statusTimer_Tick);
            // 
            // lblMobileTitle
            // 
            this.lblMobileTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMobileTitle.AutoSize = true;
            this.lblMobileTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblMobileTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblMobileTitle.Location = new System.Drawing.Point(480, 177);
            this.lblMobileTitle.Name = "lblMobileTitle";
            this.lblMobileTitle.Size = new System.Drawing.Size(191, 21);
            this.lblMobileTitle.TabIndex = 11;
            this.lblMobileTitle.Text = "Mobile App Connection";
            // 
            // pnlMobileConn
            // 
            this.pnlMobileConn.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlMobileConn.BackColor = System.Drawing.Color.White;
            this.pnlMobileConn.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlMobileConn.Controls.Add(this.lblMobileStatus);
            this.pnlMobileConn.Controls.Add(this.picMobileQr);
            this.pnlMobileConn.Controls.Add(this.lblMobileMsg);
            this.pnlMobileConn.Controls.Add(this.lblMobileHint);
            this.pnlMobileConn.Controls.Add(this.lblMobileUrl);
            this.pnlMobileConn.Location = new System.Drawing.Point(480, 201);
            this.pnlMobileConn.Name = "pnlMobileConn";
            this.pnlMobileConn.Size = new System.Drawing.Size(271, 317);
            this.pnlMobileConn.TabIndex = 11;
            // 
            // lblMobileStatus
            // 
            this.lblMobileStatus.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblMobileStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(193)))), ((int)(((byte)(7)))));
            this.lblMobileStatus.Location = new System.Drawing.Point(7, 14);
            this.lblMobileStatus.Name = "lblMobileStatus";
            this.lblMobileStatus.Size = new System.Drawing.Size(257, 21);
            this.lblMobileStatus.TabIndex = 0;
            this.lblMobileStatus.Text = "● Connecting…";
            this.lblMobileStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // picMobileQr
            // 
            this.picMobileQr.BackColor = System.Drawing.Color.White;
            this.picMobileQr.Location = new System.Drawing.Point(41, 45);
            this.picMobileQr.Name = "picMobileQr";
            this.picMobileQr.Size = new System.Drawing.Size(189, 191);
            this.picMobileQr.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picMobileQr.TabIndex = 1;
            this.picMobileQr.TabStop = false;
            this.picMobileQr.Visible = false;
            this.picMobileQr.Click += new System.EventHandler(this.picMobileQr_Click);
            // 
            // lblMobileMsg
            // 
            this.lblMobileMsg.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblMobileMsg.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.lblMobileMsg.Location = new System.Drawing.Point(15, 113);
            this.lblMobileMsg.Name = "lblMobileMsg";
            this.lblMobileMsg.Size = new System.Drawing.Size(240, 52);
            this.lblMobileMsg.TabIndex = 2;
            this.lblMobileMsg.Text = "Mobile App Connection Unavailable";
            this.lblMobileMsg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblMobileMsg.Visible = false;
            // 
            // lblMobileHint
            // 
            this.lblMobileHint.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMobileHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblMobileHint.Location = new System.Drawing.Point(7, 244);
            this.lblMobileHint.Name = "lblMobileHint";
            this.lblMobileHint.Size = new System.Drawing.Size(257, 17);
            this.lblMobileHint.TabIndex = 3;
            this.lblMobileHint.Text = "Scan the QR code, or type this address in your phone browser:";
            this.lblMobileHint.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblMobileHint.Visible = false;
            // 
            // lblMobileUrl
            // 
            this.lblMobileUrl.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblMobileUrl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(253)))));
            this.lblMobileUrl.Location = new System.Drawing.Point(7, 265);
            this.lblMobileUrl.Name = "lblMobileUrl";
            this.lblMobileUrl.Size = new System.Drawing.Size(257, 22);
            this.lblMobileUrl.TabIndex = 4;
            this.lblMobileUrl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblMobileUrl.Visible = false;
            // 
            // DashboardForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(771, 537);
            this.Controls.Add(this.lblToday);
            this.Controls.Add(this.lblUpdated);
            this.Controls.Add(this.lblConnection);
            this.Controls.Add(this.cardWaiting);
            this.Controls.Add(this.cardRegistered);
            this.Controls.Add(this.cardCollections);
            this.Controls.Add(this.cardPending);
            this.Controls.Add(this.lblTrendTitle);
            this.Controls.Add(this.pnlTrend);
            this.Controls.Add(this.lblWindowsTitle);
            this.Controls.Add(this.pnlWindows);
            this.Controls.Add(this.lblMobileTitle);
            this.Controls.Add(this.pnlMobileConn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "DashboardForm";
            this.Text = "Dashboard";
            this.cardWaiting.ResumeLayout(false);
            this.cardWaiting.PerformLayout();
            this.cardRegistered.ResumeLayout(false);
            this.cardRegistered.PerformLayout();
            this.cardCollections.ResumeLayout(false);
            this.cardCollections.PerformLayout();
            this.cardPending.ResumeLayout(false);
            this.cardPending.PerformLayout();
            this.pnlMobileConn.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picMobileQr)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblToday;
        private System.Windows.Forms.Label lblUpdated;
        private System.Windows.Forms.Label lblConnection;
        private System.Windows.Forms.Panel cardWaiting;
        private System.Windows.Forms.Label lblWaitingValue;
        private System.Windows.Forms.Label lblWaitingCap;
        private System.Windows.Forms.Label lblWaitingSub;
        private System.Windows.Forms.Panel stripeWaiting;
        private System.Windows.Forms.Panel cardRegistered;
        private System.Windows.Forms.Label lblRegisteredValue;
        private System.Windows.Forms.Label lblRegisteredCap;
        private System.Windows.Forms.Label lblRegisteredSub;
        private System.Windows.Forms.Panel stripeRegistered;
        private System.Windows.Forms.Panel cardCollections;
        private System.Windows.Forms.Label lblCollectionsValue;
        private System.Windows.Forms.Label lblCollectionsCap;
        private System.Windows.Forms.Label lblCollectionsSub;
        private System.Windows.Forms.Panel stripeCollections;
        private System.Windows.Forms.Panel cardPending;
        private System.Windows.Forms.Label lblPendingValue;
        private System.Windows.Forms.Label lblPendingCap;
        private System.Windows.Forms.Label lblPendingSub;
        private System.Windows.Forms.Panel stripePending;
        private System.Windows.Forms.Label lblTrendTitle;
        private System.Windows.Forms.Panel pnlTrend;
        private System.Windows.Forms.Label lblWindowsTitle;
        private System.Windows.Forms.Panel pnlWindows;
        private System.Windows.Forms.Timer statusTimer;
        private System.Windows.Forms.Label lblMobileTitle;
        private System.Windows.Forms.Panel pnlMobileConn;
        private System.Windows.Forms.Label lblMobileStatus;
        private System.Windows.Forms.PictureBox picMobileQr;
        private System.Windows.Forms.Label lblMobileMsg;
        private System.Windows.Forms.Label lblMobileHint;
        private System.Windows.Forms.Label lblMobileUrl;
    }
}
