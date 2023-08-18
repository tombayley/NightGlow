using CommunityToolkit.Mvvm.ComponentModel;
using NightGlow.Properties;
using NightGlow.Services;
using System;

namespace NightGlow.ViewModels;

public class MainWindowViewModel : ObservableObject
{

    private readonly SettingsService _settingsService;
    private readonly NightGlowService _nightGlowService;

    public SettingsService SettingsService { get => _settingsService; }
    public NightGlowService NightGlowService { get => _nightGlowService; }

    public double BrightnessMinInt
    {
        get => Convert.ToInt32(Settings.Default.BrightnessMin * 100);
        set
        {
            Settings.Default.BrightnessMin = (double)value / 100;
            Settings.Default.Save();
        }
    }

    public double BrightnessMaxInt
    {
        get => Convert.ToInt32(Settings.Default.BrightnessMax * 100);
        set
        {
            Settings.Default.BrightnessMax = (double)value / 100;
            Settings.Default.Save();
        }
    }

    public double BrightnessStepInt
    {
        get => Convert.ToInt32(Settings.Default.BrightnessStep * 100);
        set
        {
            Settings.Default.BrightnessStep = (double)value / 100;
            Settings.Default.Save();
        }
    }

    private int _selectedTabIndex;

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (_selectedTabIndex == value) return;

            // The Hotkey tab
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

    public MainWindowViewModel(SettingsService settingsService, NightGlowService nightGlowService)
    {
        _settingsService = settingsService;
        _nightGlowService = nightGlowService;
    }
}
