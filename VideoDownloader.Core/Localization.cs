using System.Globalization;
using VideoDownloader.Core.Windows;

namespace VideoDownloader.Core
{
    public enum AppLanguage
    {
        Auto,
        English,
        Polish
    }

    /// <summary>
    /// Lightweight app-wide translator. English strings are used as dictionary keys, so any
    /// string without a Polish entry simply falls back to the English text passed in.
    /// </summary>
    public static class Localization
    {
        private static AppLanguage _language;

        public static event EventHandler LanguageChanged;

        static Localization()
        {
            if (!Enum.TryParse(Registry.GetLanguage(), out _language))
                _language = AppLanguage.Auto;
        }

        public static AppLanguage Language
        {
            get => _language;
            set
            {
                if (_language == value) return;

                _language = value;
                Registry.SetLanguage(value.ToString());
                LanguageChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static bool IsPolish =>
            _language == AppLanguage.Polish ||
            (_language == AppLanguage.Auto && CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("pl", StringComparison.OrdinalIgnoreCase));

        public static string T(string english)
        {
            return IsPolish && _pl.TryGetValue(english, out var polish) ? polish : english;
        }

        private static readonly Dictionary<string, string> _pl = new()
        {
            // FrmMain
            ["Source (link)"] = "Źródło (link)",
            ["Destination (path)"] = "Miejsce docelowe (ścieżka)",
            ["Download"] = "Pobierz",
            ["Browse"] = "Przeglądaj",
            ["Load TXT"] = "Wczytaj TXT",
            ["Open destination folder"] = "Otwórz folder docelowy",
            ["☕ Buy me a coffee"] = "☕ Postaw mi kawę",
            ["ℹ️ About"] = "ℹ️ O programie",
            ["Simultaneous downloads: {0}"] = "Jednoczesne pobierania: {0}",
            ["Jobs: {0} | Active: {1} | Completed: {2}"] = "Zadania: {0} | Aktywne: {1} | Zakończone: {2}",
            ["Jobs: {0} | Completed: {0}"] = "Zadania: {0} | Zakończone: {0}",
            ["Please select a valid destination path before downloading."] = "Wybierz prawidłową ścieżkę docelową przed pobraniem.",
            ["Could not update yt-dlp:"] = "Nie udało się zaktualizować yt-dlp:",
            ["yt-dlp updated successfully"] = "yt-dlp zaktualizowany pomyślnie",
            ["Found {0} urls! Are you sure you want to download them all?"] = "Znaleziono {0} adresów URL! Czy na pewno chcesz pobrać je wszystkie?",
            ["A download is in progress. Are you sure you want to exit?"] = "Pobieranie jest w toku. Czy na pewno chcesz zakończyć?",
            ["Exit"] = "Zakończ",
            ["Yes"] = "Tak",
            ["No"] = "Nie",

            // FrmSourceChooser
            ["Select sources"] = "Wybierz źródła",
            ["Video:"] = "Wideo:",
            ["Audio:"] = "Audio:",
            ["Cancel"] = "Anuluj",
            ["Failed to retrieve sources."] = "Nie udało się pobrać źródeł.",
            ["Error"] = "Błąd",
            ["An error occurred while loading sources:"] = "Wystąpił błąd podczas wczytywania źródeł:",
            ["No downloadable video/audio formats found."] = "Nie znaleziono możliwych do pobrania formatów wideo/audio.",
            ["No downloader assigned."] = "Nie przypisano modułu pobierania.",

            // FrmAbout
            ["About VideoDownloader"] = "O programie VideoDownloader",
            ["Version:"] = "Wersja:",
            ["Build date:"] = "Data kompilacji:",
            ["Author:"] = "Autor:",
            ["Uses the following tools:"] = "Korzysta z następujących narzędzi:",
            ["Close"] = "Zamknij",

            // History
            ["🕑 History"] = "🕑 Historia",
            ["Download history"] = "Historia pobrań",
            ["Date"] = "Data",
            ["Title"] = "Tytuł",
            ["URL"] = "URL",
            ["Duration"] = "Czas trwania",
            ["Size"] = "Rozmiar",
            ["Avg. speed"] = "Śr. prędkość",
            ["Status"] = "Status",
            ["Completed"] = "Zakończono",
            ["Failed"] = "Niepowodzenie",
            ["Canceled"] = "Anulowano",
            ["Search"] = "Szukaj",
            ["Actions"] = "Akcje",
            ["Redownload"] = "Pobierz ponownie",
            ["Open in browser"] = "Otwórz w przeglądarce",
            ["Clear history"] = "Wyczyść historię",
            ["Are you sure you want to clear the entire download history?"] = "Czy na pewno chcesz wyczyścić całą historię pobrań?",

            // DownloadJobListBoxItem
            ["Duration:"] = "Czas trwania:",
            ["Resolution:"] = "Rozdzielczość:",
            ["Size (MB):"] = "Rozmiar (MB):",
            ["Format:"] = "Format:",
            ["Are you sure you want to cancel downloading?"] = "Czy na pewno chcesz anulować pobieranie?",
            ["Downloading"] = "Pobieranie",

            // ProgressBarExtended
            ["Progress: {0:F1} %"] = "Postęp: {0:F1} %",
            ["Speed: {0}"] = "Prędkość: {0}",
            ["ETA: {0}"] = "Pozostały czas: {0}",

            // Errors / Downloader
            ["Restricted access! Payable content?"] = "Brak dostępu! Płatna zawartość?",
            ["Invalid URL! '{0}'"] = "Nieprawidłowy adres URL! '{0}'",
            ["Unknown error!"] = "Nieznany błąd!",
            ["Could not resolve the requested playlist entry."] = "Nie udało się rozwiązać wybranego elementu playlisty.",
            ["Download failed ({0}): {1}"] = "Pobieranie nie powiodło się ({0}): {1}",
        };
    }
}
