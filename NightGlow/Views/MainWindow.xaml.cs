using NightGlow.ViewModels;
using System.Windows;

namespace NightGlow.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        Loaded += viewModel.OnWindowLoaded;
        Closing += viewModel.OnWindowClosing;
    }

}
