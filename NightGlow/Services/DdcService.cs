using NightGlow.MonitorConfig;

namespace NightGlow.Services;

public class DdcService
{

    public Monitors Monitors;

    public DdcService()
    {

    }

    public void UpdateMonitors()
    {
        if (Monitors == null)
        {
            Monitors = new Monitors();
        }
        Monitors.Scan();
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
