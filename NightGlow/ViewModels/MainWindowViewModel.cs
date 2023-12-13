using CommunityToolkit.Mvvm.ComponentModel;
using NightGlow.Data;
using NightGlow.MonitorConfig;
using NightGlow.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace NightGlow.ViewModels;

public class MainWindowViewModel : ObservableObject
{

    private readonly SettingsService _settingsService;
    private readonly DdcService _ddcService;
    private readonly NightGlowService _nightGlowService;

    public SettingsService SettingsService { get => _settingsService; }
    public NightGlowService NightGlowService { get => _nightGlowService; }


    public ObservableCollection<MonitorItemViewModel> MonitorItems { get; set; }
        = new ObservableCollection<MonitorItemViewModel>();


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
        SetupMonitorItems();
    }

    public void SetupMonitorItems()
    {
        MonitorItems.Clear();

        foreach (VirtualMonitor vm in _ddcService.Monitors.VirtualMonitors)
        {
            PhysicalMonitor? pm = vm.PhysicalMonitors.FirstOrDefault();
            if (pm == null) continue;

            DdcMonitorItem ddcMonitorItem = _settingsService.DdcConfig.GetOrCreateDdcMonitorItem(vm);

            MonitorItemViewModel monitorItemViewModel = new MonitorItemViewModel
            {
                VirtualName = vm.FriendlyName,
                DeviceName = vm.DeviceName,
                PhysicalName = pm.Description,
                Brightness = Convert.ToInt32(pm.GetBrightness().Current),
                Contrast = Convert.ToInt32(pm.GetContrast().Current),
                EnableDdc = ddcMonitorItem.EnableDdc,
                MaxBrightness = ddcMonitorItem.MaxBrightness,
                MinBrightnessPct = ddcMonitorItem.MinBrightnessPct
            };
            monitorItemViewModel.BrightnessChangeEvent += (object sender, DdcItemChangeEventArgs e) =>
            {
                _ddcService.SetBrightness(vm, pm, e.MonitorItem.Brightness);
            };
            monitorItemViewModel.ContrastChangeEvent += (object sender, DdcItemChangeEventArgs e) =>
            {
                _ddcService.SetContrast(vm, pm, e.MonitorItem.Contrast);
            };
            monitorItemViewModel.EnableDdcChangeEvent += (object sender, DdcItemChangeEventArgs e) =>
            {
                _settingsService.DdcConfig.SetEnableDdc(vm, e.MonitorItem.EnableDdc);
                _settingsService.SaveDdcConfig();
            };
            monitorItemViewModel.MinBrightnessPctChangeEvent += (object sender, DdcItemChangeEventArgs e) =>
            {
                _settingsService.DdcConfig.SetMinBrightnessPct(vm, e.MonitorItem.MinBrightnessPct);
                _settingsService.SaveDdcConfig();
            };
            monitorItemViewModel.MaxBrightnessChangeEvent += (object sender, DdcItemChangeEventArgs e) =>
            {
                _settingsService.DdcConfig.SetMaxBrightness(vm, e.MonitorItem.MaxBrightness);
                _settingsService.SaveDdcConfig();
            };
            MonitorItems.Add(monitorItemViewModel);
        }
    }

    public void UpdateDdcMonitors()
    {
        _ddcService.UpdateMonitors();
        SetupMonitorItems();
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

public class MonitorItemViewModel : ObservableObject
{

    public string VirtualName { get; set; }
    public string DeviceName { get; set; }
    public string PhysicalName { get; set; }
    public string CombinedName { get => $"{VirtualName} - {DeviceName}"; }

    private int _brightness;
    private int _contrast;
    private bool _enableDdc;
    private int _minBrightnessPct;
    private int _maxBrightness;

    public int Brightness
    {
        get => _brightness;
        set
        {
            _brightness = value;
            OnPropertyChanged(nameof(Brightness));
            BrightnessChangeEvent?.Invoke(null, new DdcItemChangeEventArgs { MonitorItem = this });
        }
    }

    public int Contrast
    {
        get => _contrast;
        set
        {
            _contrast = value;
            OnPropertyChanged(nameof(Contrast));
            ContrastChangeEvent?.Invoke(null, new DdcItemChangeEventArgs { MonitorItem = this });
        }
    }

    public bool EnableDdc
    {
        get => _enableDdc;
        set
        {
            _enableDdc = value;
            OnPropertyChanged(nameof(EnableDdc));
            EnableDdcChangeEvent?.Invoke(null, new DdcItemChangeEventArgs { MonitorItem = this });
        }
    }

    public int MinBrightnessPct
    {
        get => _minBrightnessPct;
        set
        {
            _minBrightnessPct = value;
            OnPropertyChanged(nameof(MinBrightnessPct));
            MinBrightnessPctChangeEvent?.Invoke(null, new DdcItemChangeEventArgs { MonitorItem = this });
        }
    }

    public int MaxBrightness
    {
        get => _maxBrightness;
        set
        {
            _maxBrightness = value;
            OnPropertyChanged(nameof(MaxBrightness));
            MaxBrightnessChangeEvent?.Invoke(null, new DdcItemChangeEventArgs { MonitorItem = this });
        }
    }

    public event EventHandler<DdcItemChangeEventArgs> EnableDdcChangeEvent;
    public event EventHandler<DdcItemChangeEventArgs> BrightnessChangeEvent;
    public event EventHandler<DdcItemChangeEventArgs> ContrastChangeEvent;
    public event EventHandler<DdcItemChangeEventArgs> MinBrightnessPctChangeEvent;
    public event EventHandler<DdcItemChangeEventArgs> MaxBrightnessChangeEvent;

}

public class DdcItemChangeEventArgs : EventArgs
{
    public MonitorItemViewModel MonitorItem { get; set; }
}
