using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VideoDownloader.Wpf.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        public string Version => $"{Core.Localization.T("Version:")} {GetVersion()}";
        public string BuildDate => $"{Core.Localization.T("Build date:")} {GetBuildDate():yyyy-MM-dd HH:mm}";
        public string Author => $"{Core.Localization.T("Author:")} Adam Skowroński";
        public ImageSource? IconSource => GetIconSource();

        private static ImageSource? GetIconSource()
        {
            try
            {
                using var icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
                if (icon == null)
                    return null;

                var bitmap = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private static string GetVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? "0.0.0";

            var plusIndex = version.IndexOf('+');
            return plusIndex < 0 ? version : version[..plusIndex];
        }

        private static DateTime GetBuildDate() =>
            File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location);

        [RelayCommand]
        private void OpenEmail() => OpenLink("mailto:info@appit.pl");

        [RelayCommand]
        private void OpenFFmpeg() => OpenLink("https://ffmpeg.org");

        [RelayCommand]
        private void OpenYtDlp() => OpenLink("https://github.com/yt-dlp/yt-dlp");

        private static void OpenLink(string url) =>
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }
}
