using CommunityToolkit.Mvvm.ComponentModel;
using NightGlow.Models;
using NightGlow.Properties;
using System;

namespace NightGlow.Services;

public class SettingsService : ObservableObject, IDisposable
{

    private readonly RegSwitch _startOnBoot = new(
        @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run",
        App.Name,
        App.ExecutablePath
    );

    public bool StartOnBoot
    {
        get => _startOnBoot.Set;
        set => _startOnBoot.Set = value;
    }

    private readonly RegSwitch _extendedGammaRange = new(
        @"HKLM\Software\Microsoft\Windows NT\CurrentVersion\ICM",
        "GdiICMGammaRange",
        256
    );

    public bool ExtendedGammaRange
    {
        get => _extendedGammaRange.Set;
        set => _extendedGammaRange.Set = value;
    }

    public bool FirstLaunch
    {
        get => Settings.Default.FirstLaunch;
        set
        {
            Settings.Default.FirstLaunch = value;
            Settings.Default.Save();
        }
    }

    public bool IsGammaPollingEnabled { get; set; } = false;

    public double BrightnessMin
    {
        get => Settings.Default.BrightnessMin;
        set
        {
            Settings.Default.BrightnessMin = value;
            Settings.Default.Save();
        }
    }

    public double BrightnessMax
    {
        get => Settings.Default.BrightnessMax;
        set
        {
            Settings.Default.BrightnessMax = value;
            Settings.Default.Save();
        }
    }

    public double BrightnessStep
    {
        get => Settings.Default.BrightnessStep;
        set
        {
            Settings.Default.BrightnessStep = value;
            Settings.Default.Save();
        }
    }

    public int TemperatureMin
    {
        get => Settings.Default.TemperatureMin;
        set
        {
            Settings.Default.TemperatureMin = value;
            Settings.Default.Save();
        }
    }

    public int TemperatureMax
    {
        get => Settings.Default.TemperatureMax;
        set
        {
            Settings.Default.TemperatureMax = value;
            Settings.Default.Save();
        }
    }

    public int TemperatureStep
    {
        get => Settings.Default.TemperatureStep;
        set
        {
            Settings.Default.TemperatureStep = value;
            Settings.Default.Save();
        }
    }

    public HotKey HotKeyBrightnessInc
    {
        get => HotKey.Deserialize(Settings.Default.HotKeyBrightnessInc);
        set
        {
            Settings.Default.HotKeyBrightnessInc = value.Serialize();
            Settings.Default.Save();
        }
    }

    public HotKey HotKeyBrightnessDec
    {
        get => HotKey.Deserialize(Settings.Default.HotKeyBrightnessDec);
        set
        {
            Settings.Default.HotKeyBrightnessDec = value.Serialize();
            Settings.Default.Save();
        }
    }

    public HotKey HotKeyTemperatureInc
    {
        get => HotKey.Deserialize(Settings.Default.HotKeyTemperatureInc);
        set
        {
            Settings.Default.HotKeyTemperatureInc = value.Serialize();
            Settings.Default.Save();
        }
    }

    public HotKey HotKeyTemperatureDec
    {
        get => HotKey.Deserialize(Settings.Default.HotKeyTemperatureDec);
        set
        {
            Settings.Default.HotKeyTemperatureDec = value.Serialize();
            Settings.Default.Save();
        }
    }

    public HotKey HotKeyBrightTempInc
    {
        get => HotKey.Deserialize(Settings.Default.HotKeyBrightTempInc);
        set
        {
            Settings.Default.HotKeyBrightTempInc = value.Serialize();
            Settings.Default.Save();
        }
    }

    public HotKey HotKeyBrightTempDec
    {
        get => HotKey.Deserialize(Settings.Default.HotKeyBrightTempDec);
        set
        {
            Settings.Default.HotKeyBrightTempDec = value.Serialize();
            Settings.Default.Save();
        }
    }

    public SettingsService()
    {

    }

    public void Dispose()
    {

    }
}
