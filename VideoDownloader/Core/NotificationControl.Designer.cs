namespace VideoDownloader.Core
{
    partial class NotificationControl
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
            lMessage = new Label();
            SuspendLayout();
            //
            // lMessage
            //
            lMessage.Dock = DockStyle.Fill;
            lMessage.TextAlign = ContentAlignment.MiddleLeft;
            lMessage.Name = "lMessage";
            lMessage.Size = new Size(380, 24);
            lMessage.TabIndex = 0;
            lMessage.Text = "MESSAGE";
            //
            // NotificationControl
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(lMessage);
            Margin = new Padding(6);
            Name = "NotificationControl";
            Padding = new Padding(10, 8, 10, 8);
            Size = new Size(400, 40);
            ResumeLayout(false);
        }

        #endregion

        private Label lMessage;
    }
}
