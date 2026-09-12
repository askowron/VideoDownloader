using System.Windows;
using VideoDownloader.Wpf.Services;

namespace VideoDownloader.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly IDialogService _dialogService = new DialogService();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void About_Click(object sender, RoutedEventArgs e) => _dialogService.ShowAbout();
    }
}
