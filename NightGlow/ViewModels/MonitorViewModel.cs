using CommunityToolkit.Mvvm.ComponentModel;
using NightGlow.Models;
using System;

namespace NightGlow.ViewModels;

public class MonitorViewModel : ObservableObject, IDisposable
{

    public DdcMonitor DdcMonitor;
    public string CombinedName { get => $"{DdcMonitor.Description} (Display {DdcMonitor.DisplayIndex})"; }

    private int _brightness;
    private int _contrast;

    // User config
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
            BrightnessChangeEvent?.Invoke(null, new DdcItemChangeEventArgs { Monitor = this });
        }
    }

    public int Contrast
    {
        get => _contrast;
        set
        {
            _contrast = value;
            OnPropertyChanged(nameof(Contrast));
            ContrastChangeEvent?.Invoke(null, new DdcItemChangeEventArgs { Monitor = this });
        }
    }

    public bool EnableDdc
    {
        get => _enableDdc;
        set
        {
            _enableDdc = value;
            OnPropertyChanged(nameof(EnableDdc));
            EnableDdcChangeEvent?.Invoke(null, new DdcItemChangeEventArgs { Monitor = this });
        }
    }

    public int MinBrightnessPct
    {
        get => _minBrightnessPct;
        set
        {
            _minBrightnessPct = value;
            OnPropertyChanged(nameof(MinBrightnessPct));
            MinBrightnessPctChangeEvent?.Invoke(null, new DdcItemChangeEventArgs { Monitor = this });
        }
    }

    public int MaxBrightness
    {
        get => _maxBrightness;
        set
        {
            _maxBrightness = value;
            OnPropertyChanged(nameof(MaxBrightness));
            MaxBrightnessChangeEvent?.Invoke(null, new DdcItemChangeEventArgs { Monitor = this });
        }
    }

    public event EventHandler<DdcItemChangeEventArgs> EnableDdcChangeEvent;
    public event EventHandler<DdcItemChangeEventArgs> BrightnessChangeEvent;
    public event EventHandler<DdcItemChangeEventArgs> ContrastChangeEvent;
    public event EventHandler<DdcItemChangeEventArgs> MinBrightnessPctChangeEvent;
    public event EventHandler<DdcItemChangeEventArgs> MaxBrightnessChangeEvent;

    public MonitorViewModel(DdcMonitor _ddcMonitor)
    {
        DdcMonitor = _ddcMonitor;
    }

    public void Dispose()
    {
        DdcMonitor.Dispose();
    }
}

public class DdcItemChangeEventArgs : EventArgs
{
    public MonitorViewModel Monitor { get; set; }
}
