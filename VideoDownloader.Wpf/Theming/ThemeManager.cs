using System.Linq;
using AppRegistry = VideoDownloader.Core.Windows.Registry;
using Application = System.Windows.Application;
using ResourceDictionary = System.Windows.ResourceDictionary;

namespace VideoDownloader.Wpf.Theming
{
    public enum AppTheme
    {
        System,
        Light,
        Dark
    }

    /// <summary>
    /// Owns the app's color palette (<c>Themes/ColorsLight.xaml</c> / <c>ColorsDark.xaml</c>) and
    /// swaps it live into <see cref="Application.Resources"/>. All theme-dependent brushes in
    /// Styles.xaml and every Window are looked up via <c>DynamicResource</c> (not
    /// <c>StaticResource</c>), which is what lets this swap repaint the whole app without a
    /// restart - <c>StaticResource</c> resolves once at XAML load time and would not react.
    /// </summary>
    public static class ThemeManager
    {
        private static AppTheme _theme;

        static ThemeManager()
        {
            if (!Enum.TryParse(AppRegistry.GetTheme(), out _theme))
                _theme = AppTheme.System;
        }

        public static AppTheme Theme
        {
            get => _theme;
            set
            {
                if (_theme == value) return;

                _theme = value;
                AppRegistry.SetTheme(value.ToString());
                Apply();
            }
        }

        /// <summary>Applies the persisted theme and starts following OS theme changes while
        /// <see cref="Theme"/> is <see cref="AppTheme.System"/>. Call once at startup, before the
        /// first window is shown.</summary>
        public static void Initialize()
        {
            Apply();

            Microsoft.Win32.SystemEvents.UserPreferenceChanged += (s, e) =>
            {
                if (e.Category != Microsoft.Win32.UserPreferenceCategory.General || _theme != AppTheme.System)
                    return;

                Application.Current?.Dispatcher.Invoke(Apply);
            };
        }

        private static bool IsSystemDark()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return false;
            }
        }

        private static void Apply()
        {
            bool dark = _theme == AppTheme.Dark || (_theme == AppTheme.System && IsSystemDark());
            var uri = new Uri(dark ? "Themes/ColorsDark.xaml" : "Themes/ColorsLight.xaml", UriKind.Relative);
            var palette = new ResourceDictionary { Source = uri };

            var merged = Application.Current.Resources.MergedDictionaries;
            var previous = merged.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Themes/Colors"));

            // Insert the new palette before removing the old one: both list the same resource
            // keys, so briefly having both present (new last, so it wins ties) avoids a one-frame
            // gap where a DynamicResource lookup would find nothing.
            merged.Insert(0, palette);
            if (previous != null)
                merged.Remove(previous);
        }
    }
}
