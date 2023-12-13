using System.Diagnostics;
using System.Runtime.InteropServices;
using static NightGlow.MonitorConfig.WinApi;

namespace NightGlow.MonitorConfig;

public class VirtualMonitor
{

    public string DeviceName = "";
    public string FriendlyName = "";

    public List<PhysicalMonitor> PhysicalMonitors = new();

    private MONITORINFOEX MonitorInfo;

    public VirtualMonitor(IntPtr hMonitor, int index)
    {
        MonitorInfo = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>() };

        if (!GetMonitorInfo(hMonitor, ref MonitorInfo))
        {
            // TODO throw error or log
            Debug.WriteLine("Error: GetMonitorInfo");
        }

        DeviceName = MonitorInfo.szDevice;

        LoadFriendlyName(index);

        GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint physicalMonitorCount);
        if (physicalMonitorCount == 0)
            return;

        PHYSICAL_MONITOR[] physicalMonitorArray = new PHYSICAL_MONITOR[physicalMonitorCount];
        GetPhysicalMonitorsFromHMONITOR(hMonitor, physicalMonitorCount, physicalMonitorArray);

        for (int i = 0; i < physicalMonitorCount; i++)
        {
            PhysicalMonitors.Add(new PhysicalMonitor(physicalMonitorArray[i]));
        }
    }

    public bool IsPrimary()
    {
        return (MonitorInfo.dwFlags & MONITORINFOF.MONITORINFOF_PRIMARY) == MONITORINFOF.MONITORINFOF_PRIMARY;
    }

    private void LoadFriendlyName(int index)
    {
        FriendlyName = "";

        uint pathCount = 0, modeCount = 0;

        long error = GetDisplayConfigBufferSizes(QUERY_DEVICE_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS,
            ref pathCount, ref modeCount);
        if (error != ERROR_SUCCESS)
        {
            // TODO throw error or log
            Debug.WriteLine("Error: GetDisplayConfigBufferSizes");
            return;
        }

        DISPLAYCONFIG_PATH_INFO[] displayPaths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        DISPLAYCONFIG_MODE_INFO[] displayModes = new DISPLAYCONFIG_MODE_INFO[modeCount];

        error = QueryDisplayConfig(QUERY_DEVICE_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS,
                ref pathCount, displayPaths, ref modeCount, displayModes, IntPtr.Zero);
        if (error != ERROR_SUCCESS)
        {
            // TODO throw error or log
            Debug.WriteLine("Error: QueryDisplayConfig");
            return;
        }

        int modeTargetCount = 0;
        for (int i = 0; i < modeCount; i++)
        {
            if (displayModes[i].infoType != DISPLAYCONFIG_MODE_INFO_TYPE.DISPLAYCONFIG_MODE_INFO_TYPE_TARGET)
                continue;

            if (modeTargetCount != index)
            {
                modeTargetCount++;
                continue;
            }

            DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
            deviceName.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_TARGET_DEVICE_NAME));
            deviceName.header.adapterId = displayModes[i].adapterId;
            deviceName.header.id = displayModes[i].id;
            deviceName.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;

            error = DisplayConfigGetDeviceInfo(ref deviceName);
            if (error != ERROR_SUCCESS)
            {
                // TODO throw error or log
                Debug.WriteLine("Error: DisplayConfigGetDeviceInfo");
                return;
            }
            FriendlyName = deviceName.monitorFriendlyDeviceName;
            return;
        }
    }

}
