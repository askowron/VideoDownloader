using System.Diagnostics;
using System.Reflection;
using VideoDownloader.Core;

namespace VideoDownloader
{
    public partial class FrmAbout : Form
    {
        public FrmAbout()
        {
            InitializeComponent();
        }

        private void FrmAbout_Load(object sender, EventArgs e)
        {
            try
            {
                using Icon? icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon != null)
                    pbIcon.Image = icon.ToBitmap();
            }
            catch { }

            Text = Localization.T("About VideoDownloader");
            lAuthor.Text = $"{Localization.T("Author:")} Adam Skowroński";
            lToolsUsed.Text = Localization.T("Uses the following tools:");
            btnClose.Text = Localization.T("Close");

            lVersion.Text = $"{Localization.T("Version:")} {Application.ProductVersion}";

            DateTime buildDate = File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location);
            lBuildDate.Text = $"{Localization.T("Build date:")} {buildDate:yyyy-MM-dd HH:mm}";
        }

        private void llEmail_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenLink("mailto:info@appit.pl");
        }

        private void llFFmpeg_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenLink("https://ffmpeg.org");
        }

        private void llYtDlp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenLink("https://github.com/yt-dlp/yt-dlp");
        }

        private static void OpenLink(string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
