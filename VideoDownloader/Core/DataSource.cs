using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeDLSharp.Metadata;

namespace VideoDownloader.Core
{
    public class DataSource
    {
        public List<DataVideoSource> VideoSources { get; set; }
        public List<DataAudioSource> AudioSources { get; set; }

        public string VideoTitle { get; set; }
        public float Duration { get; set; }

        public DataSource() 
        {
            VideoSources = new List<DataVideoSource>();
            AudioSources = new List<DataAudioSource>();
        }
    }

    public class Source
    {
        public string Id { get; set; }
        public string Note { get; set; }
        public string Extension { get; set; }
        public string Codec { get; set; }
    }

    public class DataAudioSource : Source
    {
        public string Language { get; set; }

        public static implicit operator DataAudioSource(FormatData format)
        {
            if (format == null)
                return null;

            return new DataAudioSource
            {
                Id = format.FormatId,
                Note = format.FormatNote,
                Extension = format.Extension,
                Codec = format.AudioCodec,
                Language = format.Language
            };
        }

        public override string ToString()
        {
            return $"{Language} | {Extension} | {Codec} | {Note}";
        }
    }

    public class DataVideoSource : Source
    {
        public double Bitrate { get; set; }
        public string Resolution { get; set; }
        public double Fps { get; set; }

        public static implicit operator DataVideoSource(FormatData format)
        {
            if (format == null)
                return null;

            return new DataVideoSource
            {
                Id = format.FormatId,
                Note = format.FormatNote,
                Extension = format.Extension,
                Codec = format.VideoCodec,
                Bitrate = format.VideoBitrate ?? 0.0,
                Resolution = format.Resolution,
                Fps = format.FrameRate ?? 0.0
            };
        }

        public override string ToString()
        {
            return $"{Resolution} | {Extension} | {Codec} | {Fps} fps | {Bitrate} kbps";
        }
    }
}
