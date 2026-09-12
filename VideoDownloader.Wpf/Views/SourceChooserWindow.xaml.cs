using System.Windows;
using VideoDownloader.Wpf.ViewModels;

namespace VideoDownloader.Wpf.Views
{
    public partial class SourceChooserWindow : Window
    {
        public SourceChooserWindow(SourceChooserViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += (s, e) =>
            {
                DialogResult = viewModel.DialogResult;
                Close();
            };
        }
    }
}
