using NightGlow.MonitorConfig;
using System;
using System.Collections.Generic;

namespace NightGlow.Data;

[Serializable]
public class DdcConfig
{

    public IList<DdcMonitorItem> DdcMonitorItems;

    public DdcConfig()
    {
        DdcMonitorItems = new List<DdcMonitorItem>();
    }

    public DdcMonitorItem GetOrCreateDdcMonitorItem(VirtualMonitor vm)
    {
        foreach (var item in DdcMonitorItems)
            if (item.Name.Equals(vm.FriendlyName) && item.DeviceName.Equals(vm.DeviceName))
                return item;

        DdcMonitorItem newItem = new DdcMonitorItem
        {
            Name = vm.FriendlyName,
            DeviceName = vm.DeviceName,
            EnableDdc = false,
            MinBrightnessPct = 0,
            MaxBrightness = 100
        };
        DdcMonitorItems.Add(newItem);
        return newItem;
    }

    public void SetEnableDdc(VirtualMonitor vm, bool value)
    {
        DdcMonitorItem item = GetOrCreateDdcMonitorItem(vm);
        item.EnableDdc = value;
    }

    public void SetMinBrightnessPct(VirtualMonitor vm, int value)
    {
        DdcMonitorItem item = GetOrCreateDdcMonitorItem(vm);
        item.MinBrightnessPct = value;
    }

    public void SetMaxBrightness(VirtualMonitor vm, int value)
    {
        DdcMonitorItem item = GetOrCreateDdcMonitorItem(vm);
        item.MaxBrightness = value;
    }

}

[Serializable]
public class DdcMonitorItem
{
    public string Name;
    public string DeviceName;

    public bool EnableDdc;

    public int MinBrightnessPct;
    public int MaxBrightness;
}
