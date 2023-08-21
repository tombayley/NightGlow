namespace NightGlow.MonitorConfig;

public class Monitors
{

    public List<VirtualMonitor> VirtualMonitors = new List<VirtualMonitor>();

    public bool Scan()
    {
        VirtualMonitors.Clear();
        bool success = WinApi.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumCallback, IntPtr.Zero);
        return success;
    }

    private bool MonitorEnumCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref WinApi.RECT lprcMonitor, IntPtr dwData)
    {
        VirtualMonitors.Add(new VirtualMonitor(hMonitor, VirtualMonitors.Count));
        return true;
    }

}
