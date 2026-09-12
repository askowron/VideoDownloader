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

        private async void Window_Activated(object sender, EventArgs e)
        {
            await _viewModel.PasteClipboardUrlAsync();
        }

        private void JobList_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.Text) ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
        }

        private async void JobList_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.Text))
            {
                string[] lines = ((string)e.Data.GetData(System.Windows.DataFormats.Text))
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                await _viewModel.LoadMultipleAsync(lines);
            }
        }
    }
}
