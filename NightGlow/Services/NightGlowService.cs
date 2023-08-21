using CommunityToolkit.Mvvm.ComponentModel;
using NightGlow.Data;
using NightGlow.Models;
using NightGlow.MonitorConfig;
using System;
using System.Diagnostics;
using System.Linq;

namespace NightGlow.Services;

// The main service. Manages other services, linking them together to create the experience
public class NightGlowService : ObservableObject, IDisposable
{

    private readonly SettingsService _settingsService;

    private readonly HotKeyService _hotKeyService;

    private readonly GammaService _gammaService;

    private readonly DdcService _ddcService;

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
        DdcService ddcService
    )
    {
        _settingsService = settingsService;
        _hotKeyService = hotKeyService;
        _gammaService = gammaService;
        _ddcService = ddcService;

        RegisterHotKeys();
    }

    public void UnregisterHotKeys()
    {
        _hotKeyService.UnregisterAllHotKeys();
    }

    public void RegisterHotKeys()
    {
        _hotKeyService.UnregisterAllHotKeys();

        RegisterKey(_settingsService.HotKeyBrightnessInc, () =>
        {
            ChangeConfig(_settingsService.BrightnessStep, 0);
        });
        RegisterKey(_settingsService.HotKeyBrightnessDec, () =>
        {
            ChangeConfig(-_settingsService.BrightnessStep, 0);
        });

        RegisterKey(_settingsService.HotKeyTemperatureInc, () =>
        {
            ChangeConfig(0, _settingsService.TemperatureStep);
        });
        RegisterKey(_settingsService.HotKeyTemperatureDec, () =>
        {
            ChangeConfig(0, -_settingsService.TemperatureStep);
        });

        RegisterKey(_settingsService.HotKeyBrightTempInc, () =>
        {
            ChangeConfig(_settingsService.BrightnessStep, _settingsService.TemperatureStep);
        });
        RegisterKey(_settingsService.HotKeyBrightTempDec, () =>
        {
            ChangeConfig(-_settingsService.BrightnessStep, -_settingsService.TemperatureStep);
        });
    }

    private void RegisterKey(HotKey hotKey, Action callback)
    {
        if (hotKey == HotKey.None) return;
        _hotKeyService.RegisterHotKey(hotKey, callback);
    }

    public void SetBrightness(double brightness)
    {
        Debug.WriteLine(string.Format("SetBrightness {0}", brightness));
        SetConfig(brightness, ColorConfig.Temperature);
    }

    public void SetTemperature(int temperature)
    {
        Debug.WriteLine(string.Format("SetTemperature {0}", temperature));
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

        foreach (VirtualMonitor vm in _ddcService.Monitors.VirtualMonitors)
        {
            PhysicalMonitor? pm = vm.PhysicalMonitors.FirstOrDefault();
            if (pm == null) continue;

            int configBrightnessPct = Convert.ToInt32(Brightness * 100);

            DdcMonitorItem ddcMonitorItem = _settingsService.DdcConfig.GetOrCreateDdcMonitorItem(vm);

            if (ddcMonitorItem.EnableDdc && configBrightnessPct > ddcMonitorItem.MinBrightnessPct)
            {
                // Use DDC to set the brightness

                // Find the percentage value the brightness is between lower (ddcMonitorItem.MinBrightnessPct) and upper (100)
                double ddcRangePct = (double)(configBrightnessPct - ddcMonitorItem.MinBrightnessPct) / (100 - ddcMonitorItem.MinBrightnessPct);
                int ddcBrightness = Convert.ToInt32(ddcRangePct * ddcMonitorItem.MaxBrightness);

                ColorConfiguration gamma = new ColorConfiguration(Temperature, _settingsService.BrightnessMax);

                Debug.WriteLine($"Using DDC. DDC: {ddcBrightness}. Gamma: {gamma.Brightness}. Monitor: {ddcMonitorItem.Name}");
                _ddcService.SetBrightness(vm, pm, ddcBrightness);
                _gammaService.SetDeviceGamma(gamma, vm.DeviceName);

            }
            else if (ddcMonitorItem.EnableDdc)
            {
                // Use Use gamma to set the brightness when DDC brightness is at minimum

                double gammaRangePct = (double)(configBrightnessPct - _settingsService.BrightnessMin) / (ddcMonitorItem.MinBrightnessPct - _settingsService.BrightnessMin);
                double gammaBrightness = gammaRangePct;

                ColorConfiguration gamma = new ColorConfiguration(Temperature, gammaBrightness);

                Debug.WriteLine($"Using Gam. DDC: 0. Gamma: {gammaBrightness}. Monitor: {ddcMonitorItem.Name}");
                _ddcService.SetBrightness(vm, pm, 0);
                _gammaService.SetDeviceGamma(gamma, vm.DeviceName);

            }
            else
            {
                // Use Use gamma to set the brightness

                Debug.WriteLine($"Using Gam. DDC: x. Gamma: {Brightness}. Monitor: {ddcMonitorItem.Name}");
                _gammaService.SetDeviceGamma(ColorConfig, vm.DeviceName);

            }
        }
    }

    public void Dispose()
    {

    }

}
