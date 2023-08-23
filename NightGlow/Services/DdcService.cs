using NightGlow.MonitorConfig;
using System.Threading;

namespace NightGlow.Services;

public class DdcService
{

    public Monitors Monitors = new Monitors();

    private Timer timer;
    private bool isUpdatingMonitors = false;

    public DdcService()
    {
        UpdateMonitors();
    }

    public void UpdateMonitors()
    {
        // UpdateMonitors could be called multiple times in a short time by windows event listeners
        // Scanning monitors takes time, so wait for short time while calls come in, and only execute after short time has passed.
        if (isUpdatingMonitors)
        {
            timer.Change(1000, Timeout.Infinite);
        }
        else
        {
            isUpdatingMonitors = true;
            timer = new Timer(ExecuteUpdateMonitors, null, 1000, Timeout.Infinite);
        }
    }

    private void ExecuteUpdateMonitors(object? state)
    {
        Monitors.Scan();

        isUpdatingMonitors = false;
        timer.Dispose();
    }

    public void SetBrightness(VirtualMonitor vm, PhysicalMonitor pm, int value)
    {
        pm.SetBrightness((uint)value);
    }

    public void SetContrast(VirtualMonitor vm, PhysicalMonitor pm, int value)
    { 
        pm.SetContrast((uint)value);
    }

}
