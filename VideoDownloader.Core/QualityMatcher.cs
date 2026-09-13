using System.Collections.Generic;
using System.Linq;

namespace VideoDownloader.Core
{
    /// <summary>
    /// Pure quality-matching logic shared by the WinForms (<c>FrmSourceChooser</c>) and WPF
    /// (<c>DialogService</c>) source choosers: given a list of fetched sources and a preferred
    /// source from an earlier video in the same batch, find the closest match so a whole batch
    /// downloads at a consistent quality.
    /// </summary>
    public static class QualityMatcher
    {
        public static DataVideoSource? MatchVideo(List<DataVideoSource> sources, DataVideoSource? preferred)
        {
            if (preferred == null) return null;
            return sources.FirstOrDefault(s => s.Resolution == preferred.Resolution && s.Extension == preferred.Extension)
                ?? sources.FirstOrDefault(s => s.Resolution == preferred.Resolution);
        }

        public static DataAudioSource? MatchAudio(List<DataAudioSource> sources, DataAudioSource? preferred)
        {
            if (preferred == null) return null;
            return sources.FirstOrDefault(s => s.Language == preferred.Language && s.Extension == preferred.Extension)
                ?? sources.FirstOrDefault(s => s.Language == preferred.Language);
        }
    }
}
