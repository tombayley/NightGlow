using CommunityToolkit.Mvvm.ComponentModel;
using NightGlow.Models;
using System;

namespace NightGlow.Services;

// The main service. Manages other services, linking them together to create the experience
public class NightGlowService : ObservableObject, IDisposable
{

    private readonly SettingsService _settingsService;

    private readonly HotKeyService _hotKeyService;

    private readonly GammaService _gammaService;

    public double Brightness
    {
        get => Convert.ToInt32(ColorConfig.Brightness * 100);
        set => SetConfig((double)value/100, Temperature);
    }
    public int Temperature
    {
        get => ColorConfig.Temperature;
        set => SetConfig(Brightness, value);
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
