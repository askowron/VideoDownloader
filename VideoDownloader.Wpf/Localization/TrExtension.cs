using System.Windows.Markup;
using VideoDownloader.Core;

namespace VideoDownloader.Wpf.Localization
{
    /// <summary>
    /// XAML markup extension over the shared Core.Localization dictionary: {loc:Tr 'English key'}.
    /// Re-evaluates every bound usage when Localization.LanguageChanged fires.
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public class TrExtension : MarkupExtension
    {
        public string Key { get; set; }

        public TrExtension() { Key = string.Empty; }
        public TrExtension(string key) { Key = key; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var target = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget))!;
            if (target.TargetObject is not System.Windows.DependencyObject targetElement)
                return global::VideoDownloader.Core.Localization.T(Key);

            if (target.TargetProperty is System.Windows.DependencyProperty dp)
            {
                void Refresh(object? s, EventArgs e) =>
                    targetElement.Dispatcher.Invoke(() => targetElement.SetValue(dp, global::VideoDownloader.Core.Localization.T(Key)));

                global::VideoDownloader.Core.Localization.LanguageChanged += Refresh;
            }

            return global::VideoDownloader.Core.Localization.T(Key);
        }
    }
}
