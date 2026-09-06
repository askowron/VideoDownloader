namespace VideoDownloader.Core.Tools
{
    internal class DataSize
    {
        public static string FromBytes(double bytes)
        {
            return string.Format("{0:F2} MB", bytes / 1024.0 / 1024.0);
        }
    }
}
