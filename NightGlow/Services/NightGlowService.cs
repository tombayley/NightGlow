using CommunityToolkit.Mvvm.ComponentModel;
using NightGlow.Data;
using NightGlow.Models;
using NightGlow.WindowsApi;
using NightGlow.Utils.Extensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using NightGlow.Views;
using Microsoft.Extensions.DependencyInjection;
using NightGlow.ViewModels;

namespace NightGlow.Services;

// The main service. Manages other services, linking them together to create the experience
public class NightGlowService : ObservableObject, IDisposable
{

    private readonly SettingsService _settingsService;
    private readonly HotKeyService _hotKeyService;
    private readonly GammaService _gammaService;
    private readonly DdcService _ddcService;

    private readonly IServiceProvider _serviceProvider;

    private readonly IDisposable _eventRegistration;

    public double Brightness
    {
        get => ColorConfig.Brightness;
        set => SetBrightness(value);
    }

    public int Temperature
    {
        get => ColorConfig.Temperature;
        set => SetTemperature(value);
    }

    public ColorConfiguration ColorConfig { get; set; } = ColorConfiguration.Default;

    public NightGlowService(
        SettingsService settingsService,
        HotKeyService hotKeyService,
        GammaService gammaService,
        DdcService ddcService,
        IServiceProvider serviceProvider
    )
    {
        _settingsService = settingsService;
        _hotKeyService = hotKeyService;
        _gammaService = gammaService;
        _ddcService = ddcService;
        _serviceProvider = serviceProvider;

        RegisterHotKeys();

        // Register for all system events that may indicate that the device context or gamma was changed from outside
        _eventRegistration = new[]
        {
            // Sometimes gamma is reset in display related Windows settings
            //SystemHook.TryRegister(
            //    SystemHook.ForegroundWindowChanged,
            //    InvalidateGamma
            //) ?? Disposable.Empty,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.ConsoleDisplayStateChanged,
                PotentialGammaChange
            ) ?? Disposable.Empty,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.PowerSavingStatusChanged,
                PotentialGammaChange
            ) ?? Disposable.Empty,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.SessionDisplayStatusChanged,
                PotentialGammaChange
            ) ?? Disposable.Empty,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.MonitorPowerStateChanged,
                PotentialGammaChange
            ) ?? Disposable.Empty,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.AwayModeChanged,
                PotentialGammaChange
            ) ?? Disposable.Empty,

            SystemEvent.Register(
                SystemEvent.Ids.DisplayChanged,
                DisplaysChanged
            ),
            SystemEvent.Register(
                SystemEvent.Ids.PaletteChanged,
                DisplaysChanged
            ),
            SystemEvent.Register(
                SystemEvent.Ids.SettingsChanged,
                DisplaysChanged
            ),
            SystemEvent.Register(
                SystemEvent.Ids.SystemColorsChanged,
                DisplaysChanged
            )
        }.Aggregate();
    }

    private void PotentialGammaChange()
    {
        _gammaService.InvalidateGamma();
    }

    private void DisplaysChanged()
    {
        _gammaService.InvalidateDeviceContexts();
        _ddcService.UpdateMonitors();

        Task.Delay(2000).ContinueWith(_ => UpdateConfiguration());
    }

    public void UnregisterHotKeys()
    {
        _hotKeyService.UnregisterAllHotKeys();
    }

    public void RegisterHotKeys()
    {
        _hotKeyService.UnregisterAllHotKeys();

        RegisterKey(_settingsService.HotKeyBrightnessInc, () => 
            ChangeConfig(_settingsService.BrightnessStep, 0));
        RegisterKey(_settingsService.HotKeyBrightnessDec, () =>
            ChangeConfig(-_settingsService.BrightnessStep, 0));

        RegisterKey(_settingsService.HotKeyTemperatureInc, () =>
            ChangeConfig(0, _settingsService.TemperatureStep));
        RegisterKey(_settingsService.HotKeyTemperatureDec, () =>
            ChangeConfig(0, -_settingsService.TemperatureStep));

        RegisterKey(_settingsService.HotKeyBrightTempInc, () =>
            ChangeConfig(_settingsService.BrightnessStep, _settingsService.TemperatureStep));
        RegisterKey(_settingsService.HotKeyBrightTempDec, () =>
            ChangeConfig(-_settingsService.BrightnessStep, -_settingsService.TemperatureStep));
    }

    private void RegisterKey(HotKey hotKey, Action callback)
    {
        if (hotKey == HotKey.None) return;
        _hotKeyService.RegisterHotKey(hotKey, callback);
    }

    public void SetBrightness(double brightness)
    {
        SetConfig(brightness, ColorConfig.Temperature);
    }

    public void SetTemperature(int temperature)
    {
        SetConfig(ColorConfig.Brightness, temperature);
    }

    private void SetConfig(double brightness, int temperature)
    {
        ColorConfig = new ColorConfiguration(temperature, brightness)
            .Clamp(
                _settingsService.TemperatureMin,
                _settingsService.TemperatureMax,
                _settingsService.BrightnessMin,
                _settingsService.BrightnessMax
            );

        OnPropertyChanged(nameof(Brightness));
        OnPropertyChanged(nameof(Temperature));

        UpdateConfiguration();
    }

    private void ChangeConfig(double brightnessOffset, int temperatureOffset)
    {
        ColorConfig = ColorConfig
            .Offset(temperatureOffset, brightnessOffset)
            .Clamp(
                _settingsService.TemperatureMin,
                _settingsService.TemperatureMax,
                _settingsService.BrightnessMin,
                _settingsService.BrightnessMax
            );

        OnPropertyChanged(nameof(Brightness));
        OnPropertyChanged(nameof(Temperature));

        UpdateConfiguration();

        if (_settingsService.ShowBrightTempPopup)
        {
            _serviceProvider.GetRequiredService<PopupWindow>().ShowPopup();
        }
    }

    // Using the brightness value, check wheter to use ddc/gamma to set the brightness
    // E.g. if ddcMonitorItem.MinBrightnessPct is 60 and DDC is enabled for the monitor, use the ranges below
    //
    // 0 -------------- 60 -------- 100
    // |    Use gamma    |  Use DDC  |
    //
    // DDC used first, then gamma is used to decrease brightness further
    private void UpdateConfiguration()
    {
        Debug.WriteLine("");

        foreach (MonitorViewModel monitor in _ddcService.Monitors.MonitorItems)
        {
            int configBrightnessPct = Convert.ToInt32(Brightness * 100);

            var ddcMonitor = monitor.DdcMonitor;
            DdcConfigMonitor ddcConfigMonitor = _settingsService.DdcConfig.GetOrCreateDdcConfigMonitor(ddcMonitor);

            if (ddcConfigMonitor.EnableDdc && configBrightnessPct > ddcConfigMonitor.MinBrightnessPct)
            {
                // Use DDC to set the brightness

                // Find the percentage value the brightness is between lower (ddcMonitorItem.MinBrightnessPct) and upper (100)
                double ddcRangePct = (double)(configBrightnessPct - ddcConfigMonitor.MinBrightnessPct) / (100 - ddcConfigMonitor.MinBrightnessPct);
                int ddcBrightness = Convert.ToInt32(ddcRangePct * ddcConfigMonitor.MaxBrightness);

                ColorConfiguration gamma = new ColorConfiguration(Temperature, _settingsService.BrightnessMax);

                Debug.WriteLine($"Using DDC. DDC: {ddcBrightness}. Gamma: {gamma.Brightness}. Monitor: {ddcMonitor.Description}");
                _ddcService.SetBrightness(ddcMonitor, ddcBrightness);
                _gammaService.SetDeviceGamma(gamma, ddcMonitor.DeviceName);

            }
            else if (ddcConfigMonitor.EnableDdc)
            {
                // Use Use gamma to set the brightness when DDC brightness is at minimum

                double gammaRangePct = (double)(configBrightnessPct - _settingsService.BrightnessMin) / (ddcConfigMonitor.MinBrightnessPct - _settingsService.BrightnessMin);
                double gammaBrightness = gammaRangePct;

                ColorConfiguration gamma = new ColorConfiguration(Temperature, gammaBrightness);

                Debug.WriteLine($"Using Gam. DDC: 0. Gamma: {gammaBrightness}. Monitor: {ddcMonitor.Description}");
                _ddcService.SetBrightness(ddcMonitor, 0);
                _gammaService.SetDeviceGamma(gamma, ddcMonitor.DeviceName);

            }
            else
            {
                // Use Use gamma to set the brightness

                Debug.WriteLine($"Using Gam. DDC: x. Gamma: {Brightness}. Monitor: {ddcMonitor.Description}");
                _gammaService.SetDeviceGamma(ColorConfig, ddcMonitor.DeviceName);

            }
        }
    }

    public void Dispose()
    {
        _eventRegistration.Dispose();
    }

}
