using System;
using System.Text;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using VideoDownloader.Core.Tools;

namespace VideoDownloader.Core
{
    public class Downloader : IDisposable
    {
        protected string videoUrl;
        protected YoutubeDL ytdl = new YoutubeDL();

        public string DestinationPath { get; set; }
        private Notifications _Notifications;

        public Downloader(Notifications notifications)
        {
            ytdl = new YoutubeDL();
            ytdl.YoutubeDLPath = Path.Combine(AppContext.BaseDirectory, "yt-dlp.exe");
            ytdl.FFmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            videoUrl = string.Empty;

            _Notifications = notifications;

        }

        public void Dispose()
        {
            // Cleanup resources if needed
        }

        public string SourceURL
        {
            get { return videoUrl; }
            set { videoUrl = value; }
        }

        public async Task<DataSource> FetchSources()
        {
            if (!Helper.URL.Verify(videoUrl))
                throw new Exception(string.Format(Localization.T("Invalid URL! '{0}'"), videoUrl));

            var video_info = await ytdl.RunVideoDataFetch(videoUrl);

            if (video_info?.Data == null)
                throw new Exception(video_info?.ErrorOutput?.Length > 0 ? string.Join("\n", video_info.ErrorOutput) : Localization.T("Unknown error!"));

            if (video_info.Data?.Formats == null && video_info.Data?.Entries?.Length > 0)
            {
                var entyId = videoUrl.Split(',').Last();
                videoUrl = video_info.Data.Entries.Where(e => e.ID.Equals(entyId)).FirstOrDefault()?.Url;
                if (videoUrl == null)
                    throw new Exception(Localization.T("Could not resolve the requested playlist entry."));

                video_info = await ytdl.RunVideoDataFetch(videoUrl);

                if (video_info?.Data == null)
                    throw new Exception(video_info?.ErrorOutput?.Length > 0 ? string.Join("\n", video_info.ErrorOutput) : Localization.T("Unknown error!"));
            }
            var videoFormats = video_info.Data?.Formats?.Where(f => !f.VideoCodec.Equals("none")).ToArray();
            var audioFormats = video_info.Data?.Formats?
                    .Where(f => (f.AudioCodec != null && !f.AudioCodec.Equals("none")) || "audio only".Equals(f.Resolution))
                    .Where(f => f.FormatNote == null ||
                        !f.FormatNote.Contains("description", StringComparison.OrdinalIgnoreCase) &&
                        !f.FormatNote.Contains("hard of hearing", StringComparison.OrdinalIgnoreCase) &&
                        !f.FormatNote.Contains("visual impairment", StringComparison.OrdinalIgnoreCase) &&
                        !f.FormatNote.Contains("Audiodeskrypcja", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

            DataSource dataSource = new DataSource();
            if (videoFormats != null && videoFormats.Length > 0)
                dataSource.VideoSources = videoFormats.Select(f => (DataVideoSource)f).ToList();
            if (audioFormats != null && audioFormats.Length > 0)
                dataSource.AudioSources = audioFormats.Select(f => (DataAudioSource)f).ToList();

            string title = video_info.Data?.Title ?? "??? {Unknown title} ???";
            dataSource.VideoTitle =
                (video_info.Data?.Series?.Length > 0 ? video_info.Data.Series + " - " : "") +
                (video_info.Data?.Episode?.Length > 0 ? video_info.Data.Episode + " - " : "") +
                title;

            dataSource.Duration = video_info.Data.Duration ?? 0f;

            return dataSource;
        }

        public async Task Download((DataVideoSource video, DataAudioSource audio) sources, DownloadJob job)
        {
            var progress = new Progress<DownloadProgress>(p => {
                if (p.Progress > 0)
                    job.ProgressPercentage = (double)p.Progress * 100;

                job.Speed = p.DownloadSpeed;
                job.ETA = p.ETA;
                job.FileSize = p.TotalDownloadSize;
            });

            job.DownloadBegin();

            try
            {
                string outputPath = Path.Combine(
                    Path.TrimEndingDirectorySeparator(DestinationPath),
                    string.Join("_", job.Title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim() + "." +
                    job.Format);

                var extraOptions = new OptionSet()
                {
                    Output = outputPath,
                    CheckFormats = true,
                    //CheckAllFormats = true,
                    CustomOptions = new IOption[]
                    {
                        new Option<string>("--progress-delta", "0.01"),
                        new Option<string>("-v", null)
                    }
                };

                var result = await ytdl.RunVideoDownload(videoUrl,
                    $"{sources.video.Id}+{sources.audio.Id}",
                    DownloadMergeFormat.Mp4,
                    VideoRecodeFormat.Mp4,
                    job.CancellationTokenSource!.Token,
                    progress,
                    null,
                    extraOptions
                );

                // yt-dlp's progress hook reports the size of the separate video+audio streams
                // being fetched, not the final merged/recoded MP4 - relying on it left the
                // history's Size/Avg. speed columns wrong or empty. The real output file on
                // disk is the source of truth for the final size.
                if (File.Exists(outputPath))
                    job.FileSize = DataSize.FromBytes(new FileInfo(outputPath).Length);

                job.DownloadEnd();
            }
            catch (OperationCanceledException)
            {
                job.State = DownloadingState.Canceled;
                return;
            }
            catch (Exception ex)
            {
                job.State = DownloadingState.Failed;
                job.ErrorMessage = Errors.ParseErrorMessage(ex);
                _Notifications.Error(string.Format(Localization.T("Download failed ({0}): {1}"), job.Title, job.ErrorMessage));
            }
            finally
            {
                job.CancellationTokenSource?.Dispose();
            }

        }


    }
}
