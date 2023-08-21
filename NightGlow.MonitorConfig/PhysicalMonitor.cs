using static NightGlow.MonitorConfig.WinApi;

namespace NightGlow.MonitorConfig;

public class PhysicalMonitor
{

    PHYSICAL_MONITOR Monitor;

    public string Description;

    Setting Brightness = new();
    Setting Contrast = new();

    public PhysicalMonitor(PHYSICAL_MONITOR monitor)
    {
        Monitor = monitor;
        Description = new string(Monitor.szPhysicalMonitorDescription);
    }

    public Setting GetBrightness()
    {
        bool success = GetMonitorBrightness(Monitor.hPhysicalMonitor, out Brightness.Min, out Brightness.Current, out Brightness.Max);
        return Brightness;
    }

    public void SetBrightness(uint value)
    {
        bool success = SetMonitorBrightness(Monitor.hPhysicalMonitor, value);
        Brightness.Current = value;
    }

    public Setting GetContrast()
    {
        bool success = GetMonitorContrast(Monitor.hPhysicalMonitor, out Contrast.Min, out Contrast.Current, out Contrast.Max);
        return Contrast;
    }

    public void SetContrast(uint value)
    {
        bool success = SetMonitorContrast(Monitor.hPhysicalMonitor, value);
        Contrast.Current = value;
    }

}
