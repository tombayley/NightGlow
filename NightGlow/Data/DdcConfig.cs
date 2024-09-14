using NightGlow.Models;
using System;
using System.Collections.Generic;

namespace NightGlow.Data;

[Serializable]
public class DdcConfig
{

    public IList<DdcConfigMonitor> DdcConfigMonitorItems;

    public DdcConfig()
    {
        DdcConfigMonitorItems = new List<DdcConfigMonitor>();
    }

    public DdcConfigMonitor GetOrCreateDdcConfigMonitor(DdcMonitor monitor)
    {
        foreach (var item in DdcConfigMonitorItems)
            if (item.DeviceInstanceId.Equals(monitor.DeviceInstanceId))
                return item;

        DdcConfigMonitor newItem = new DdcConfigMonitor
        {
            Description = monitor.Description,
            DeviceInstanceId = monitor.DeviceInstanceId,
            EnableDdc = false,
            MinBrightnessPct = 0,
            MaxBrightness = 100
        };
        DdcConfigMonitorItems.Add(newItem);
        return newItem;
    }

    public void SetEnableDdc(DdcMonitor monitor, bool value)
    {
        DdcConfigMonitor item = GetOrCreateDdcConfigMonitor(monitor);
        item.EnableDdc = value;
    }

    public void SetMinBrightnessPct(DdcMonitor monitor, int value)
    {
        DdcConfigMonitor item = GetOrCreateDdcConfigMonitor(monitor);
        item.MinBrightnessPct = value;
    }

    public void SetMaxBrightness(DdcMonitor monitor, int value)
    {
        DdcConfigMonitor item = GetOrCreateDdcConfigMonitor(monitor);
        item.MaxBrightness = value;
    }

}

[Serializable]
public class DdcConfigMonitor
{
    public string Description;
    public string DeviceInstanceId;

    public bool EnableDdc;

    public int MinBrightnessPct;
    public int MaxBrightness;
}
