using System.Diagnostics;
using static NightGlow.MonitorConfig.WinApi;

namespace NightGlow.MonitorConfig;

public class PhysicalMonitor
{

    // Sometimes setting/getting monitor settings can fail. Retry this many times before assuming failure.
    private const int DDC_ATTEMPTS = 3;

    PHYSICAL_MONITOR Monitor;

    public string Description;

    private readonly Setting Brightness = new();
    private readonly Setting Contrast = new();

    public PhysicalMonitor(PHYSICAL_MONITOR monitor)
    {
        Monitor = monitor;
        Description = new string(Monitor.szPhysicalMonitorDescription);
    }

    public Setting GetBrightness()
    {
        bool success = RetryGet((monitor) => GetMonitorBrightness(
            monitor, out Brightness.Min, out Brightness.Current, out Brightness.Max), Monitor.hPhysicalMonitor
        );
        return Brightness;
    }

    public void SetBrightness(uint value)
    {
        bool success = RetrySet(SetMonitorBrightness, GetBrightness, Monitor.hPhysicalMonitor, value);
        if (success)
            Brightness.Current = value;
    }

    public Setting GetContrast()
    {
        bool success = RetryGet((monitor) => GetMonitorContrast(
            monitor, out Contrast.Min, out Contrast.Current, out Contrast.Max), Monitor.hPhysicalMonitor
        );
        return Contrast;
    }

    public void SetContrast(uint value)
    {
        bool success = RetrySet(SetMonitorContrast, GetContrast, Monitor.hPhysicalMonitor, value);
        if (success)
            Contrast.Current = value;
    }

    private bool RetrySet(Func<nint, uint, bool> setFunction, Func<Setting> getFunction, nint monitor, uint value)
    {
        for (int attempt = 1; attempt <= DDC_ATTEMPTS; attempt++)
        {
            // Sometimes setting a monitor value (e.g. brightness) will report as success,
            // but monitor does not change brightness.
            // Confirm value has been set by getting current value after doing the set.
            if (setFunction(monitor, value) && getFunction().Current == value)
                return true;
        }
        return false;
    }

    private bool RetryGet(Func<nint, bool> getFunction, nint monitor)
    {
        for (int attempt = 1; attempt <= DDC_ATTEMPTS; attempt++)
        {
            if (getFunction(monitor))
                return true;
        }
        return false;
    }

}
