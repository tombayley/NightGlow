using CommunityToolkit.Mvvm.ComponentModel;
using NightGlow.Models;
using System;
using System.Diagnostics;

namespace NightGlow.Services;

// The main service. Manages other services, linking them together to create the experience
public class NightGlowService : ObservableObject, IDisposable
{

    private readonly SettingsService _settingsService;

    private readonly HotKeyService _hotKeyService;

    private readonly GammaService _gammaService;

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
        GammaService gammaService
    )
    {
        _settingsService = settingsService;
        _hotKeyService = hotKeyService;
        _gammaService = gammaService;

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

    private void UpdateConfiguration()
    {
        _gammaService.SetGamma(ColorConfig);
    }

    public void Dispose()
    {

    }

}
