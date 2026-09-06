namespace VideoDownloader
{
    partial class FrmAbout
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pbIcon = new PictureBox();
            lAppName = new Label();
            lVersion = new Label();
            lBuildDate = new Label();
            lAuthor = new Label();
            llEmail = new LinkLabel();
            lToolsUsed = new Label();
            llFFmpeg = new LinkLabel();
            llYtDlp = new LinkLabel();
            btnClose = new Button();
            ((System.ComponentModel.ISupportInitialize)pbIcon).BeginInit();
            SuspendLayout();
            //
            // pbIcon
            //
            pbIcon.Location = new Point(12, 12);
            pbIcon.Name = "pbIcon";
            pbIcon.Size = new Size(48, 48);
            pbIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            pbIcon.TabIndex = 0;
            pbIcon.TabStop = false;
            //
            // lAppName
            //
            lAppName.AutoSize = true;
            lAppName.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lAppName.Location = new Point(70, 12);
            lAppName.Name = "lAppName";
            lAppName.Size = new Size(174, 25);
            lAppName.TabIndex = 1;
            lAppName.Text = "VideoDownloader";
            //
            // lVersion
            //
            lVersion.AutoSize = true;
            lVersion.Location = new Point(70, 44);
            lVersion.Name = "lVersion";
            lVersion.Size = new Size(66, 17);
            lVersion.TabIndex = 2;
            lVersion.Text = "Version:";
            //
            // lBuildDate
            //
            lBuildDate.AutoSize = true;
            lBuildDate.Location = new Point(70, 64);
            lBuildDate.Name = "lBuildDate";
            lBuildDate.Size = new Size(88, 17);
            lBuildDate.TabIndex = 3;
            lBuildDate.Text = "Build date:";
            //
            // lAuthor
            //
            lAuthor.AutoSize = true;
            lAuthor.Location = new Point(12, 96);
            lAuthor.Name = "lAuthor";
            lAuthor.Size = new Size(185, 17);
            lAuthor.TabIndex = 4;
            lAuthor.Text = "Author: Adam Skowroński";
            //
            // llEmail
            //
            llEmail.AutoSize = true;
            llEmail.Location = new Point(12, 118);
            llEmail.Name = "llEmail";
            llEmail.Size = new Size(76, 17);
            llEmail.TabIndex = 5;
            llEmail.TabStop = true;
            llEmail.Text = "info@appit.pl";
            llEmail.LinkClicked += llEmail_LinkClicked;
            //
            // lToolsUsed
            //
            lToolsUsed.AutoSize = true;
            lToolsUsed.Location = new Point(12, 152);
            lToolsUsed.Name = "lToolsUsed";
            lToolsUsed.Size = new Size(139, 17);
            lToolsUsed.TabIndex = 6;
            lToolsUsed.Text = "Uses the following tools:";
            //
            // llFFmpeg
            //
            llFFmpeg.AutoSize = true;
            llFFmpeg.Location = new Point(28, 174);
            llFFmpeg.Name = "llFFmpeg";
            llFFmpeg.Size = new Size(53, 17);
            llFFmpeg.TabIndex = 7;
            llFFmpeg.TabStop = true;
            llFFmpeg.Text = "FFmpeg";
            llFFmpeg.LinkClicked += llFFmpeg_LinkClicked;
            //
            // llYtDlp
            //
            llYtDlp.AutoSize = true;
            llYtDlp.Location = new Point(28, 196);
            llYtDlp.Name = "llYtDlp";
            llYtDlp.Size = new Size(44, 17);
            llYtDlp.TabIndex = 8;
            llYtDlp.TabStop = true;
            llYtDlp.Text = "yt-dlp";
            llYtDlp.LinkClicked += llYtDlp_LinkClicked;
            //
            // btnClose
            //
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.DialogResult = DialogResult.OK;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Location = new Point(202, 228);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(100, 30);
            btnClose.TabIndex = 9;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            //
            // FrmAbout
            //
            AcceptButton = btnClose;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(314, 270);
            Controls.Add(pbIcon);
            Controls.Add(lAppName);
            Controls.Add(lVersion);
            Controls.Add(lBuildDate);
            Controls.Add(lAuthor);
            Controls.Add(llEmail);
            Controls.Add(lToolsUsed);
            Controls.Add(llFFmpeg);
            Controls.Add(llYtDlp);
            Controls.Add(btnClose);
            Font = new Font("Segoe UI", 9.75F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmAbout";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "About VideoDownloader";
            Load += FrmAbout_Load;
            ((System.ComponentModel.ISupportInitialize)pbIcon).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pbIcon;
        private Label lAppName;
        private Label lVersion;
        private Label lBuildDate;
        private Label lAuthor;
        private LinkLabel llEmail;
        private Label lToolsUsed;
        private LinkLabel llFFmpeg;
        private LinkLabel llYtDlp;
        private Button btnClose;
    }
}
