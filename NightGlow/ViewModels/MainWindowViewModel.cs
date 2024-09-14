using CommunityToolkit.Mvvm.ComponentModel;
using NightGlow.Services;
using System.ComponentModel;
using System.Windows;

namespace NightGlow.ViewModels;

public class MainWindowViewModel : ObservableObject
{

    private readonly SettingsService _settingsService;
    private readonly DdcService _ddcService;
    private readonly NightGlowService _nightGlowService;

    public SettingsService SettingsService { get => _settingsService; }
    public DdcService DdcService { get => _ddcService; }
    public NightGlowService NightGlowService { get => _nightGlowService; }

    private int _selectedTabIndex;

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (_selectedTabIndex == value) return;

            // Disable global hotkeys so they dont interfere with setting new hotkeys
            if (value == 2)
                // Entered hotkey tab
                _nightGlowService.UnregisterHotKeys();
            else if (_selectedTabIndex == 2)
                // Left hotkey tab
                _nightGlowService.RegisterHotKeys();

            _selectedTabIndex = value;
            OnPropertyChanged(nameof(SelectedTabIndex));
        }
    }

    public MainWindowViewModel(
        SettingsService settingsService,
        DdcService ddcService,
        NightGlowService nightGlowService
    )
    {
        _settingsService = settingsService;
        _ddcService = ddcService;
        _nightGlowService = nightGlowService;

        UpdateDdcMonitors();
    }

    public void UpdateDdcMonitors()
    {
        DdcService.UpdateMonitors();
    }

    public void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (SelectedTabIndex == 2)
            _nightGlowService.UnregisterHotKeys();
    }

    public void OnWindowClosing(object sender, CancelEventArgs e)
    {
        if (SelectedTabIndex == 2)
            _nightGlowService.RegisterHotKeys();
    }

}
