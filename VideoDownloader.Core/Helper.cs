using System.Text.RegularExpressions;

namespace VideoDownloader.Core
{
    public static class Helper
    {
        public static class URL
        {
            public static bool Verify(string text)
            {
                string pattern = @"^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=,]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=,]*)$";
                return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
            }
        }
    }
}
