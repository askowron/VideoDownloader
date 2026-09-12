using VideoDownloader.Core;

namespace VideoDownloader.Wpf.Services
{
    public interface IDialogService
    {
        void ShowAbout();

        /// <summary>Shows the source chooser; returns null if the user cancelled.</summary>
        Task<(DataVideoSource video, DataAudioSource audio, string videoTitle, float duration)?> ShowSourceChooser(Downloader downloader);

        /// <summary>Auto-picks best quality, matching <paramref name="preferred"/> if given, without showing UI.</summary>
        Task<(DataVideoSource video, DataAudioSource audio, string videoTitle, float duration)?> ChooseBestQuality(Downloader downloader, (DataVideoSource video, DataAudioSource audio)? preferred);
    }
}
