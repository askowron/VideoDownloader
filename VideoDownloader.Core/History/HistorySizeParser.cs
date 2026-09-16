using System.Globalization;
using System.Text.RegularExpressions;

namespace VideoDownloader.Core.History
{
    /// <summary>Parses yt-dlp's IEC-formatted size strings (e.g. "45.67MiB") into a byte count.</summary>
    public static class HistorySizeParser
    {
        private static readonly Regex SizePattern = new(@"^\s*([\d.,]+)\s*([KMGT]?i?B)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static long? ParseBytes(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var match = SizePattern.Match(text);
            if (!match.Success)
                return null;

            if (!double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                return null;

            double multiplier = match.Groups[2].Value.ToUpperInvariant() switch
            {
                "B" => 1,
                "KB" or "KIB" => 1024,
                "MB" or "MIB" => 1024 * 1024,
                "GB" or "GIB" => 1024d * 1024 * 1024,
                "TB" or "TIB" => 1024d * 1024 * 1024 * 1024,
                _ => 0
            };

            if (multiplier == 0)
                return null;

            return (long)(value * multiplier);
        }
    }
}
