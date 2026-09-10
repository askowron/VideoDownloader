using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VideoDownloader.Core.Windows
{
    public static class Registry
    {
        public static string COMPANY 
        { 
            get 
            {
                return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "APPIT";
            } 
        }
        public static string APPNAME
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "YTDownloader";
            }
        }
        public static string YTDL_REGISTRY_KEY = @$"SOFTWARE\{COMPANY}\{APPNAME}";
        public const string YTDL_REGISTRY_VALUE_LAST_DESTINATION_PATH = "LastDestinationPath";
        public const string YTDL_REGISTRY_VALUE_MAX_CONCURRENT_DOWNLOADS = "MaxConcurrentDownloads";
        public const string YTDL_REGISTRY_VALUE_LANGUAGE = "Language";

        public static string ReadString(string keyPath, string valueName)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath))
                {
                    if (key != null)
                    {
                        var value = key.GetValue(valueName) as string;
                        return value ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            return string.Empty;
        }

        public static void WriteString(string keyPath, string valueName, string value)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    if (key != null)
                    {
                        key.SetValue(valueName, value, Microsoft.Win32.RegistryValueKind.String);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        #region COMMON

        public static string GetLastDestinationPath()
        {
            return ReadString(YTDL_REGISTRY_KEY, YTDL_REGISTRY_VALUE_LAST_DESTINATION_PATH);
        }

        public static void SetLastDestinationPath(string path)
        {
            WriteString(YTDL_REGISTRY_KEY, YTDL_REGISTRY_VALUE_LAST_DESTINATION_PATH, path);
        }

        public static int GetMaxConcurrentDownloads(int defaultValue = 3)
        {
            string raw = ReadString(YTDL_REGISTRY_KEY, YTDL_REGISTRY_VALUE_MAX_CONCURRENT_DOWNLOADS);
            if (int.TryParse(raw, out int value) && value >= 1 && value <= 20)
                return value;
            return defaultValue;
        }

        public static void SetMaxConcurrentDownloads(int value)
        {
            WriteString(YTDL_REGISTRY_KEY, YTDL_REGISTRY_VALUE_MAX_CONCURRENT_DOWNLOADS, value.ToString());
        }

        public static string GetLanguage()
        {
            return ReadString(YTDL_REGISTRY_KEY, YTDL_REGISTRY_VALUE_LANGUAGE);
        }

        public static void SetLanguage(string language)
        {
            WriteString(YTDL_REGISTRY_KEY, YTDL_REGISTRY_VALUE_LANGUAGE, language);
        }

        #endregion
    }
}
