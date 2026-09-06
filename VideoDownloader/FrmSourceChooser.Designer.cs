namespace VideoDownloader
{
    partial class FrmSourceChooser
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
            label1 = new Label();
            cbVideo = new ComboBox();
            cbAudio = new ComboBox();
            label2 = new Label();
            btnDownload = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(45, 17);
            label1.TabIndex = 0;
            label1.Text = "Video:";
            // 
            // cbVideo
            // 
            cbVideo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cbVideo.DropDownStyle = ComboBoxStyle.DropDownList;
            cbVideo.DropDownWidth = 400;
            cbVideo.FlatStyle = FlatStyle.Flat;
            cbVideo.FormattingEnabled = true;
            cbVideo.Location = new Point(76, 12);
            cbVideo.Name = "cbVideo";
            cbVideo.Size = new Size(306, 25);
            cbVideo.TabIndex = 1;
            // 
            // cbAudio
            // 
            cbAudio.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cbAudio.DropDownStyle = ComboBoxStyle.DropDownList;
            cbAudio.DropDownWidth = 250;
            cbAudio.FlatStyle = FlatStyle.Flat;
            cbAudio.FormattingEnabled = true;
            cbAudio.Location = new Point(76, 44);
            cbAudio.Name = "cbAudio";
            cbAudio.Size = new Size(306, 25);
            cbAudio.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 47);
            label2.Name = "label2";
            label2.Size = new Size(45, 17);
            label2.TabIndex = 2;
            label2.Text = "Audio:";
            // 
            // btnDownload
            // 
            btnDownload.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDownload.Enabled = false;
            btnDownload.FlatStyle = FlatStyle.Flat;
            btnDownload.Location = new Point(282, 89);
            btnDownload.Name = "btnDownload";
            btnDownload.Size = new Size(100, 30);
            btnDownload.TabIndex = 4;
            btnDownload.Text = "Download";
            btnDownload.UseVisualStyleBackColor = true;
            btnDownload.Click += btnDownload_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Location = new Point(12, 89);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 30);
            btnCancel.TabIndex = 5;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // FrmSourceChooser
            // 
            AcceptButton = btnDownload;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(394, 131);
            Controls.Add(btnCancel);
            Controls.Add(btnDownload);
            Controls.Add(cbAudio);
            Controls.Add(label2);
            Controls.Add(cbVideo);
            Controls.Add(label1);
            Font = new Font("Segoe UI", 9.75F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmSourceChooser";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Select sources";
            Load += FrmSourceChooser_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private ComboBox cbVideo;
        private ComboBox cbAudio;
        private Label label2;
        private Button btnDownload;
        private Button btnCancel;
    }
}