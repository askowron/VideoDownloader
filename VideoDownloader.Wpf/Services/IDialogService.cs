using VideoDownloader.Core;

namespace VideoDownloader.Wpf.Services
{
    public interface IDialogService
    {
        void ShowAbout();

        /// <summary>Shows the download history screen. <paramref name="onRedownload"/> is invoked
        /// with a history row's URL when the user clicks its redownload action.</summary>
        Task ShowHistory(Func<string, Task> onRedownload);

        /// <summary>Shows a Yes/No confirmation dialog; returns true if the user confirmed.</summary>
        bool Confirm(string message, string title);

        /// <summary>Shows the source chooser; returns null if the user cancelled.</summary>
        Task<(DataVideoSource video, DataAudioSource audio, string videoTitle, float duration)?> ShowSourceChooser(Downloader downloader);

        /// <summary>Auto-picks best quality, matching <paramref name="preferred"/> if given, without showing UI.</summary>
        Task<(DataVideoSource video, DataAudioSource audio, string videoTitle, float duration)?> ChooseBestQuality(Downloader downloader, (DataVideoSource video, DataAudioSource audio)? preferred);
    }
}
