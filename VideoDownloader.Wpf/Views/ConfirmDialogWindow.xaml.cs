using System.Windows;

namespace VideoDownloader.Wpf.Views
{
    public partial class ConfirmDialogWindow : Window
    {
        public ConfirmDialogWindow(string title, string message, string yesText, string noText)
        {
            InitializeComponent();

            Title = title;
            MessageText.Text = message;
            YesButton.Content = yesText;
            NoButton.Content = noText;
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
