using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace NightGlow.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    private void ButtonVersion_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(App.UrlReleaseHistory) { UseShellExecute = true });
    }
}
