using System.Windows;
using VideoDownloader.Wpf.Services;
using VideoDownloader.Wpf.ViewModels;

namespace VideoDownloader.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel(new DialogService());
            DataContext = _viewModel;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            // Clipboard-paste-on-activate is wired in Task 15.
        }

        private void JobList_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.Text) ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
        }

        private void JobList_Drop(object sender, System.Windows.DragEventArgs e)
        {
            // Multi-URL drag-drop is wired in Task 15.
        }
    }
}
