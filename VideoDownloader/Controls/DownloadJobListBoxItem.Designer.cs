namespace VideoDownloader.Controls
{
    partial class DownloadJobListBoxItem
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lTitle = new Label();
            lUrl = new Label();
            lResolution = new Label();
            progressBar = new ProgressBarExtended();
            lFormat = new Label();
            lDuration = new Label();
            lSize = new Label();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // lTitle
            // 
            lTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lTitle.Location = new Point(11, 8);
            lTitle.Name = "lTitle";
            lTitle.Size = new Size(651, 17);
            lTitle.TabIndex = 0;
            lTitle.Text = "TITLE";
            // 
            // lUrl
            // 
            lUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lUrl.Font = new Font("Segoe UI", 8.25F, FontStyle.Underline, GraphicsUnit.Point, 238);
            lUrl.ForeColor = Color.SteelBlue;
            lUrl.Location = new Point(11, 25);
            lUrl.Name = "lUrl";
            lUrl.Size = new Size(678, 13);
            lUrl.TabIndex = 1;
            lUrl.Text = "URL";
            // 
            // lResolution
            // 
            lResolution.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 238);
            lResolution.Location = new Point(11, 38);
            lResolution.Name = "lResolution";
            lResolution.Size = new Size(175, 13);
            lResolution.TabIndex = 2;
            lResolution.Text = "Resolution: ";
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(10, 54);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(680, 24);
            progressBar.Step = 1;
            progressBar.TabIndex = 3;
            // 
            // lFormat
            // 
            lFormat.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 238);
            lFormat.Location = new Point(192, 38);
            lFormat.Name = "lFormat";
            lFormat.Size = new Size(175, 13);
            lFormat.TabIndex = 4;
            lFormat.Text = "Format: ";
            // 
            // lDuration
            // 
            lDuration.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 238);
            lDuration.Location = new Point(373, 38);
            lDuration.Name = "lDuration";
            lDuration.Size = new Size(175, 13);
            lDuration.TabIndex = 5;
            lDuration.Text = "Duration: ";
            // 
            // lSize
            // 
            lSize.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lSize.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 238);
            lSize.Location = new Point(554, 38);
            lSize.Name = "lSize";
            lSize.Size = new Size(138, 13);
            lSize.TabIndex = 6;
            lSize.Text = "Size (MB): ";
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Location = new Point(671, 5);
            btnCancel.Margin = new Padding(0);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(21, 23);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "X";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // DownloadJobListBoxItem
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(btnCancel);
            Controls.Add(lSize);
            Controls.Add(lDuration);
            Controls.Add(lFormat);
            Controls.Add(progressBar);
            Controls.Add(lResolution);
            Controls.Add(lUrl);
            Controls.Add(lTitle);
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 238);
            Name = "DownloadJobListBoxItem";
            Padding = new Padding(8);
            Size = new Size(700, 85);
            ResumeLayout(false);
        }

        #endregion

        private Label lTitle;
        private Label lUrl;
        private Label lResolution;
        private ProgressBarExtended progressBar;
        private Label lFormat;
        private Label lDuration;
        private Label lSize;
        private Button btnCancel;
    }
}
