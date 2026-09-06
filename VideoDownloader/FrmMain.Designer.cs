namespace VideoDownloader
{
    partial class FrmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            label1 = new Label();
            tbLink = new TextBox();
            btnDownload = new Button();
            icons = new ImageList(components);
            label2 = new Label();
            tbDestinationPath = new TextBox();
            btnBrowse = new Button();
            flpJobs = new FlowLayoutPanel();
            pParameters = new Panel();
            btnOpen = new Button();
            btnLoad = new Button();
            flpNotifications = new FlowLayoutPanel();
            tlpMain = new TableLayoutPanel();
            status = new StatusStrip();
            lStatus = new ToolStripStatusLabel();
            lMaxConcurrent = new ToolStripStatusLabel();
            tbMaxConcurrent = new TrackBar();
            tshMaxConcurrent = new ToolStripControlHost(tbMaxConcurrent);
            lBuyMeCoffee = new ToolStripStatusLabel();
            lAbout = new ToolStripStatusLabel();
            cbLanguage = new ToolStripComboBox();
            pParameters.SuspendLayout();
            tlpMain.SuspendLayout();
            status.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 12);
            label1.Name = "label1";
            label1.Size = new Size(79, 17);
            label1.TabIndex = 0;
            label1.Text = "Source (link)";
            // 
            // tbLink
            // 
            tbLink.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbLink.Location = new Point(120, 8);
            tbLink.Name = "tbLink";
            tbLink.Size = new Size(433, 25);
            tbLink.TabIndex = 1;
            tbLink.Enter += tbLink_Enter;
            // 
            // btnDownload
            // 
            btnDownload.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDownload.FlatAppearance.BorderColor = SystemColors.ActiveBorder;
            btnDownload.FlatStyle = FlatStyle.Flat;
            btnDownload.ImageAlign = ContentAlignment.MiddleLeft;
            btnDownload.ImageKey = "download";
            btnDownload.ImageList = icons;
            btnDownload.Location = new Point(597, 8);
            btnDownload.Name = "btnDownload";
            btnDownload.Size = new Size(108, 25);
            btnDownload.TabIndex = 3;
            btnDownload.Text = "Download";
            btnDownload.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnDownload.UseVisualStyleBackColor = true;
            btnDownload.Click += btnDownload_Click;
            // 
            // icons
            // 
            icons.ColorDepth = ColorDepth.Depth32Bit;
            icons.ImageStream = (ImageListStreamer)resources.GetObject("icons.ImageStream");
            icons.TransparentColor = Color.Transparent;
            icons.Images.SetKeyName(0, "load");
            icons.Images.SetKeyName(1, "download");
            icons.Images.SetKeyName(2, "open");
            icons.Images.SetKeyName(3, "browse");
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(3, 43);
            label2.Name = "label2";
            label2.Size = new Size(111, 17);
            label2.TabIndex = 4;
            label2.Text = "Destination (path)";
            // 
            // tbDestinationPath
            // 
            tbDestinationPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbDestinationPath.Location = new Point(120, 39);
            tbDestinationPath.Name = "tbDestinationPath";
            tbDestinationPath.Size = new Size(433, 25);
            tbDestinationPath.TabIndex = 5;
            // 
            // btnBrowse
            // 
            btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowse.FlatAppearance.BorderColor = SystemColors.ActiveBorder;
            btnBrowse.FlatStyle = FlatStyle.Flat;
            btnBrowse.ImageAlign = ContentAlignment.MiddleLeft;
            btnBrowse.ImageKey = "browse";
            btnBrowse.ImageList = icons;
            btnBrowse.Location = new Point(597, 39);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(108, 25);
            btnBrowse.TabIndex = 6;
            btnBrowse.Text = "Browse";
            btnBrowse.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;
            // 
            // flpJobs
            // 
            flpJobs.AllowDrop = true;
            flpJobs.AutoScroll = true;
            flpJobs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            flpJobs.BackColor = SystemColors.Window;
            flpJobs.BorderStyle = BorderStyle.FixedSingle;
            flpJobs.Location = new Point(3, 111);
            flpJobs.Name = "flpJobs";
            flpJobs.Size = new Size(708, 325);
            flpJobs.TabIndex = 7;
            flpJobs.ControlAdded += flpJobs_ControlAdded;
            flpJobs.ControlRemoved += flpJobs_ControlRemoved;
            flpJobs.DragDrop += flpJobs_DragDrop;
            flpJobs.DragEnter += flpJobs_DragEnter;
            flpJobs.Resize += flp_Resize;
            // 
            // pParameters
            // 
            pParameters.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            pParameters.Controls.Add(btnOpen);
            pParameters.Controls.Add(btnLoad);
            pParameters.Controls.Add(label1);
            pParameters.Controls.Add(tbLink);
            pParameters.Controls.Add(btnBrowse);
            pParameters.Controls.Add(btnDownload);
            pParameters.Controls.Add(tbDestinationPath);
            pParameters.Controls.Add(label2);
            pParameters.Location = new Point(3, 9);
            pParameters.MinimumSize = new Size(200, 100);
            pParameters.Name = "pParameters";
            pParameters.Size = new Size(708, 100);
            pParameters.TabIndex = 8;
            // 
            // btnOpen
            // 
            btnOpen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpen.FlatAppearance.BorderColor = SystemColors.ActiveBorder;
            btnOpen.FlatStyle = FlatStyle.Flat;
            btnOpen.ImageKey = "open";
            btnOpen.ImageList = icons;
            btnOpen.Location = new Point(559, 39);
            btnOpen.Name = "btnOpen";
            btnOpen.Size = new Size(32, 25);
            btnOpen.TabIndex = 8;
            btnOpen.UseVisualStyleBackColor = true;
            btnOpen.Click += btnOpen_Click;
            // 
            // btnLoad
            // 
            btnLoad.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLoad.FlatAppearance.BorderColor = SystemColors.ActiveBorder;
            btnLoad.FlatAppearance.MouseDownBackColor = Color.FromArgb(224, 224, 224);
            btnLoad.FlatStyle = FlatStyle.Flat;
            btnLoad.ImageKey = "load";
            btnLoad.ImageList = icons;
            btnLoad.Location = new Point(559, 8);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(32, 25);
            btnLoad.TabIndex = 7;
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // flpNotifications
            // 
            flpNotifications.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            flpNotifications.FlowDirection = FlowDirection.TopDown;
            flpNotifications.Location = new Point(3, 3);
            flpNotifications.MinimumSize = new Size(708, 0);
            flpNotifications.Name = "flpNotifications";
            flpNotifications.Size = new Size(708, 0);
            flpNotifications.TabIndex = 9;
            flpNotifications.ControlAdded += flpNotifications_ControlAdded;
            flpNotifications.ControlRemoved += flpNotifications_ControlRemoved;
            flpNotifications.Resize += flp_Resize;
            // 
            // tlpMain
            // 
            tlpMain.ColumnCount = 1;
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMain.Controls.Add(flpNotifications, 0, 0);
            tlpMain.Controls.Add(pParameters, 0, 1);
            tlpMain.Controls.Add(flpJobs, 0, 2);
            tlpMain.Dock = DockStyle.Fill;
            tlpMain.Location = new Point(0, 0);
            tlpMain.Name = "tlpMain";
            tlpMain.RowCount = 3;
            tlpMain.RowStyles.Add(new RowStyle());
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 102F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMain.Size = new Size(714, 439);
            tlpMain.TabIndex = 10;
            // 
            // status
            // 
            status.Items.AddRange(new ToolStripItem[] { lStatus, lMaxConcurrent, tshMaxConcurrent, lBuyMeCoffee, lAbout, cbLanguage });
            status.Location = new Point(0, 439);
            status.Name = "status";
            status.Size = new Size(714, 22);
            status.TabIndex = 11;
            status.Text = "statusStrip1";
            // 
            // lStatus
            // 
            lStatus.Name = "lStatus";
            lStatus.Size = new Size(39, 17);
            lStatus.Spring = true;
            lStatus.Text = "Status";
            // 
            // lMaxConcurrent
            // 
            lMaxConcurrent.Name = "lMaxConcurrent";
            lMaxConcurrent.Size = new Size(160, 17);
            lMaxConcurrent.Text = "Simultaneous downloads: 3";
            lMaxConcurrent.ToolTipText = "Maximum number of downloads that run at the same time (1-20).";
            // 
            // tbMaxConcurrent
            // 
            tbMaxConcurrent.AutoSize = false;
            tbMaxConcurrent.Minimum = 1;
            tbMaxConcurrent.Maximum = 20;
            tbMaxConcurrent.SmallChange = 1;
            tbMaxConcurrent.LargeChange = 1;
            tbMaxConcurrent.TickFrequency = 1;
            tbMaxConcurrent.TickStyle = TickStyle.None;
            tbMaxConcurrent.Value = 3;
            tbMaxConcurrent.Size = new Size(140, 20);
            tbMaxConcurrent.ValueChanged += tbMaxConcurrent_ValueChanged;
            // 
            // tshMaxConcurrent
            // 
            tshMaxConcurrent.AutoSize = false;
            tshMaxConcurrent.Name = "tshMaxConcurrent";
            tshMaxConcurrent.Size = new Size(140, 20);
            tshMaxConcurrent.ToolTipText = "Maximum number of downloads that run at the same time (1-20).";
            // 
            // lBuyMeCoffee
            // 
            lBuyMeCoffee.ForeColor = Color.FromArgb(0, 102, 204);
            lBuyMeCoffee.Name = "lBuyMeCoffee";
            lBuyMeCoffee.Size = new Size(112, 17);
            lBuyMeCoffee.Text = "☕ Buy me a coffee";
            lBuyMeCoffee.TextAlign = ContentAlignment.MiddleRight;
            lBuyMeCoffee.ToolTipText = "https://buycoffee.to/rico";
            lBuyMeCoffee.Click += lBuyMeCoffee_Click;
            // 
            // lAbout
            // 
            lAbout.ForeColor = Color.FromArgb(0, 102, 204);
            lAbout.Name = "lAbout";
            lAbout.Size = new Size(50, 17);
            lAbout.Text = "ℹ️ About";
            lAbout.TextAlign = ContentAlignment.MiddleRight;
            lAbout.Click += lAbout_Click;
            // 
            // cbLanguage
            // 
            cbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cbLanguage.Items.AddRange(new object[] { "Domyślna", "EN", "PL" });
            cbLanguage.Name = "cbLanguage";
            cbLanguage.Size = new Size(80, 23);
            cbLanguage.ToolTipText = "Language / Język";
            cbLanguage.SelectedIndexChanged += cbLanguage_SelectedIndexChanged;
            // 
            // FrmMain
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(714, 461);
            Controls.Add(tlpMain);
            Controls.Add(status);
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 238);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(730, 250);
            Name = "FrmMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Video Downloader";
            Activated += FrmMain_Activated;
            Load += FrmMain_Load;
            pParameters.ResumeLayout(false);
            pParameters.PerformLayout();
            tlpMain.ResumeLayout(false);
            status.ResumeLayout(false);
            status.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox tbLink;
        private Button btnDownload;
        private Label label2;
        private TextBox tbDestinationPath;
        private Button btnBrowse;
        private FlowLayoutPanel flpJobs;
        private Panel pParameters;
        private FlowLayoutPanel flpNotifications;
        private TableLayoutPanel tlpMain;
        private Button btnLoad;
        private Button btnOpen;
        private StatusStrip status;
        private ToolStripStatusLabel lStatus;
        private ToolStripStatusLabel lMaxConcurrent;
        private TrackBar tbMaxConcurrent;
        private ToolStripControlHost tshMaxConcurrent;
        private ToolStripStatusLabel lBuyMeCoffee;
        private ToolStripStatusLabel lAbout;
        private ToolStripComboBox cbLanguage;
        private ImageList icons;
    }
}
