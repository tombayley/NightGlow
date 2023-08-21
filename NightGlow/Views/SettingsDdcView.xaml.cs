using NightGlow.ViewModels;
using System.Windows.Controls;

namespace NightGlow.Views
{
    public partial class SettingsDdcView : UserControl
    {
        public SettingsDdcView()
        {
            InitializeComponent();
        }

        private void RefreshButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ((MainWindowViewModel)DataContext).UpdateDdcMonitors();
        }
    }
}
