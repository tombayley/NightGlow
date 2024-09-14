using NightGlow.Data;
using NightGlow.Services;
using NightGlow.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using static NightGlow.Helper.WinApi;

namespace NightGlow.Models;

public class Monitors
{

    public ObservableCollection<MonitorViewModel> MonitorItems { get; set; }
        = new ObservableCollection<MonitorViewModel>();

    protected readonly object _monitorsLock = new();

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private CancellationTokenSource _cts = new CancellationTokenSource();

    public Monitors()
    {
        BindingOperations.EnableCollectionSynchronization(MonitorItems, _monitorsLock);
    }

    public Task Scan(SettingsService _settingsService) => ScanAsync(_settingsService);

    private async Task ScanAsync(SettingsService _settingsService)
    {
        var newCts = new CancellationTokenSource();

        CancellationTokenSource prevCts = Interlocked.Exchange(ref _cts, newCts);
        prevCts.Cancel();
        prevCts.Dispose();

        await _semaphore.WaitAsync();
        try
        {
            await Task.Run(async () =>
            {
                await ScanAsync(_settingsService, newCts.Token);
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ScanAsync(SettingsService settingsService, CancellationToken cts)
    {
        if (cts.IsCancellationRequested)
            return;

        lock (_monitorsLock)
        {
            foreach (var monitor in MonitorItems)
            {
                monitor.Dispose();
            }
            MonitorItems.Clear();
        }

        foreach (var ddcMonitor in await EnumerateMonitorsAsync())
        {
            if (cts.IsCancellationRequested)
                return;

            Debug.WriteLine($"{ddcMonitor.DisplayIndex}, {ddcMonitor.DeviceInstanceId}, {ddcMonitor.Description}");

            DdcConfigMonitor ddcConfigMonitor = settingsService.DdcConfig.GetOrCreateDdcConfigMonitor(ddcMonitor);

            var monitorVm = new MonitorViewModel(ddcMonitor)
            {
                Brightness = Convert.ToInt32(ddcMonitor.GetBrightness().Current),
                EnableDdc = ddcConfigMonitor.EnableDdc,
                MaxBrightness = ddcConfigMonitor.MaxBrightness,
                MinBrightnessPct = ddcConfigMonitor.MinBrightnessPct,
            };
            monitorVm.BrightnessChangeEvent += (s, e) =>
            {
                ddcMonitor.SetBrightness((uint)e.Monitor.Brightness);
            };
            monitorVm.ContrastChangeEvent += (s, e) =>
            {
                //item.SetContrast(item, e.Monitor.Contrast);
            };
            monitorVm.EnableDdcChangeEvent += (s, e) =>
            {
                settingsService.DdcConfig.SetEnableDdc(ddcMonitor, e.Monitor.EnableDdc);
                settingsService.SaveDdcConfig();
            };
            monitorVm.MinBrightnessPctChangeEvent += (s, e) =>
            {
                settingsService.DdcConfig.SetMinBrightnessPct(ddcMonitor, e.Monitor.MinBrightnessPct);
                settingsService.SaveDdcConfig();
            };
            monitorVm.MaxBrightnessChangeEvent += (s, e) =>
            {
                settingsService.DdcConfig.SetMaxBrightness(ddcMonitor, e.Monitor.MaxBrightness);
                settingsService.SaveDdcConfig();
            };

            lock (_monitorsLock)
            {
                MonitorItems.Add(monitorVm);
            }
        }
    }

    private static async Task<IEnumerable<DdcMonitor>> EnumerateMonitorsAsync(CancellationToken cancellationToken = default)
    {
        var deviceItems = EnumerateMonitorDevices().ToArray();
        var displayItems = EnumerateDisplayConfigs().ToArray();

        IEnumerable<BasicItem> EnumerateBasicItems()
        {
            foreach (var deviceItem in deviceItems)
            {
                var displayItem = displayItems.FirstOrDefault(x => string.Equals(deviceItem.DeviceInstanceId, x.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(displayItem.DisplayName))
                {
                    yield return new BasicItem(deviceItem, displayItem.DisplayName);
                }
            }
        }

        var basicItems = EnumerateBasicItems().Where(x => !string.IsNullOrWhiteSpace(x.AlternateDescription)).ToList();
        if (basicItems.Count == 0)
            return Enumerable.Empty<DdcMonitor>();

        var handleItems = GetMonitorHandles();

        var physicalItemsTasks = handleItems.Select(x => Task.Run(() => (x, physicalItems: EnumeratePhysicalMonitors(x.MonitorHandle))))
        .ToArray();
        await Task.WhenAll(physicalItemsTasks);
        cancellationToken.ThrowIfCancellationRequested();
        var physicalItemsPairs = physicalItemsTasks.Where(x => x.Status == TaskStatus.RanToCompletion).Select(x => x.Result);

        IEnumerable<DdcMonitor> EnumerateMonitorItems()
        {
            foreach ((var handleItem, var physicalItems) in physicalItemsPairs)
            {
                foreach (var physicalItem in physicalItems)
                {
                    int index = basicItems.FindIndex(x =>
                        (x.DisplayIndex == handleItem.DisplayIndex) &&
                        (x.MonitorIndex == physicalItem.MonitorIndex) &&
                        string.Equals(x.Description, physicalItem.Description, StringComparison.OrdinalIgnoreCase));
                    if (index < 0)
                    {
                        physicalItem.Handle.Dispose();
                        continue;
                    }

                    var basicItem = basicItems[index];

                    yield return new DdcMonitor(
                        deviceInstanceId: basicItem.DeviceInstanceId,
                        description: basicItem.AlternateDescription,
                        displayIndex: basicItem.DisplayIndex,
                        monitorIndex: basicItem.MonitorIndex,
                        monitorRect: handleItem.MonitorRect,
                        handle: physicalItem.Handle);

                    basicItems.RemoveAt(index);
                    if (basicItems.Count == 0)
                        yield break;
                }
            }
        }

        return EnumerateMonitorItems();
    }

    public static IEnumerable<PhysicalItem> EnumeratePhysicalMonitors(IntPtr monitorHandle)
    {
        if (!GetNumberOfPhysicalMonitorsFromHMONITOR(monitorHandle, out uint count))
        {
            Debug.WriteLine($"Failed to get the number of physical monitors.");
            yield break;
        }

        if (count == 0)
            yield break;

        var physicalMonitors = new PHYSICAL_MONITOR[count];

        try
        {
            if (!GetPhysicalMonitorsFromHMONITOR(monitorHandle, count, physicalMonitors))
            {
                Debug.WriteLine($"Failed to get an array of physical monitors.");
                yield break;
            }

            int monitorIndex = 0;

            foreach (var physicalMonitor in physicalMonitors)
            {
                var handle = new SafePhysicalMonitorHandle(physicalMonitor.hPhysicalMonitor);

                yield return new PhysicalItem(
                    description: physicalMonitor.szPhysicalMonitorDescription,
                    monitorIndex: monitorIndex,
                    handle: handle);

                monitorIndex++;
            }
        }
        finally
        {
            // The physical monitor handles should be destroyed at a later stage.
        }
    }

    public static HandleItem[] GetMonitorHandles()
    {
        var handleItems = new List<HandleItem>();

        if (EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Proc, IntPtr.Zero))
            return handleItems.ToArray();

        return Array.Empty<HandleItem>();

        bool Proc(IntPtr monitorHandle, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData)
        {
            var monitorInfo = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>() };

            if (GetMonitorInfo(monitorHandle, ref monitorInfo))
            {
                if (TryGetDisplayIndex(monitorInfo.szDevice, out byte displayIndex))
                {
                    handleItems.Add(new HandleItem(
                        displayIndex: displayIndex,
                        monitorRect: monitorInfo.rcMonitor,
                        monitorHandle: monitorHandle));
                }
            }
            return true;
        }
    }

    public static IEnumerable<DeviceItem> EnumerateMonitorDevices()
    {
        foreach (var (_, displayIndex, monitor, monitorIndex) in EnumerateDevices())
        {
            var deviceInstanceId = ConvertToDeviceInstanceId(monitor.DeviceID);
            if (string.IsNullOrEmpty(deviceInstanceId))
                continue;

            yield return new DeviceItem(
                deviceInstanceId: deviceInstanceId,
                description: monitor.DeviceString,
                displayIndex: displayIndex,
                monitorIndex: monitorIndex);
        }
    }

    private static IEnumerable<(DISPLAY_DEVICE display, byte displayIndex, DISPLAY_DEVICE monitor, byte monitorIndex)> EnumerateDevices()
    {
        var size = (uint)Marshal.SizeOf<DISPLAY_DEVICE>();
        var display = new DISPLAY_DEVICE { cb = size };
        var monitor = new DISPLAY_DEVICE { cb = size };

        for (uint i = 0; EnumDisplayDevices(null, i, ref display, EDD_GET_DEVICE_INTERFACE_NAME); i++)
        {
            if (!TryGetDisplayIndex(display.DeviceName, out byte displayIndex))
                continue;

            byte monitorIndex = 0;

            for (uint j = 0; EnumDisplayDevices(display.DeviceName, j, ref monitor, EDD_GET_DEVICE_INTERFACE_NAME); j++)
            {
                if (!monitor.StateFlags.HasFlag(DISPLAY_DEVICE_FLAG.DISPLAY_DEVICE_ACTIVE))
                    continue;

                yield return (display, displayIndex, monitor, monitorIndex);

                monitorIndex++;
            }
        }
    }

    public static IEnumerable<DisplayItem> EnumerateDisplayConfigs()
    {
        if (GetDisplayConfigBufferSizes(
            QDC_ONLY_ACTIVE_PATHS,
            out uint pathCount,
            out uint modeCount) != ERROR_SUCCESS)
            yield break;

        var displayPaths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var displayModes = new DISPLAYCONFIG_MODE_INFO[modeCount];

        if (QueryDisplayConfig(
            QDC_ONLY_ACTIVE_PATHS,
            ref pathCount,
            displayPaths,
            ref modeCount,
            displayModes,
            IntPtr.Zero) != ERROR_SUCCESS)
            yield break;

        foreach (var displayPath in displayPaths)
        {
            var displayMode = displayModes
                .Where(x => x.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.DISPLAYCONFIG_MODE_INFO_TYPE_TARGET)
                .FirstOrDefault(x => x.id == displayPath.targetInfo.id);
            if (displayMode.Equals(default(DISPLAYCONFIG_MODE_INFO)))
                continue;

            var deviceName = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
            deviceName.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>();
            deviceName.header.adapterId = displayMode.adapterId;
            deviceName.header.id = displayMode.id;
            deviceName.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;

            if (DisplayConfigGetDeviceInfo(ref deviceName) != ERROR_SUCCESS)
                continue;

            var deviceInstanceId = ConvertToDeviceInstanceId(deviceName.monitorDevicePath);

            yield return new DisplayItem(
                deviceInstanceId: deviceInstanceId,
                displayName: deviceName.monitorFriendlyDeviceName,
                isInternal: (deviceName.outputTechnology == DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL),
                refreshRate: displayPath.targetInfo.refreshRate.Numerator / (float)displayPath.targetInfo.refreshRate.Denominator,
                isAvailable: displayPath.targetInfo.targetAvailable);
        }
    }

    private static bool TryGetDisplayIndex(string deviceName, out byte index)
    {
        var match = Regex.Match(deviceName, @"DISPLAY(?<index>\d{1,2})\s*$");
        if (match.Success)
        {
            index = byte.Parse(match.Groups["index"].Value);
            return true;
        }
        index = 0;
        return false;
    }

    internal static string ConvertToDeviceInstanceId(string devicePath)
    {
        // The typical format of device path is as follows:
        // \\?\DISPLAY#<hardware-specific-ID>#<instance-specific-ID>#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}
        // \\?\ is extended-length path prefix.
        // DISPLAY indicates display device.
        // {e6f07b5f-ee97-4a90-b076-33f57bf4eaa7} means GUID_DEVINTERFACE_MONITOR.

        int index = devicePath.IndexOf("DISPLAY", StringComparison.Ordinal);
        if (index < 0)
            return null;

        var fields = devicePath.Substring(index).Split('#');
        if (fields.Length < 3)
            return null;

        return string.Join(@"\", fields.Take(3));
    }

    public class PhysicalItem
    {
        public string Description { get; }

        public int MonitorIndex { get; }

        public SafePhysicalMonitorHandle Handle { get; }

        public PhysicalItem(string description, int monitorIndex, SafePhysicalMonitorHandle handle)
        {
            this.Description = description;
            this.MonitorIndex = monitorIndex;
            this.Handle = handle;
        }
    }

    public class HandleItem
    {
        public int DisplayIndex { get; }

        public RECT MonitorRect { get; }
        private string _monitorRectString;

        public IntPtr MonitorHandle { get; }

        public HandleItem(int displayIndex, RECT monitorRect, IntPtr monitorHandle)
        {
            this.DisplayIndex = displayIndex;
            this.MonitorRect = monitorRect;
            this.MonitorHandle = monitorHandle;
        }
    }

    private class BasicItem
    {
        private readonly DeviceItem _deviceItem;

        public string DeviceInstanceId => _deviceItem.DeviceInstanceId;
        public string Description => _deviceItem.Description;
        public string AlternateDescription { get; }
        public byte DisplayIndex => _deviceItem.DisplayIndex;
        public byte MonitorIndex => _deviceItem.MonitorIndex;

        public BasicItem(DeviceItem deviceItem, string alternateDescription = null)
        {
            this._deviceItem = deviceItem ?? throw new ArgumentNullException(nameof(deviceItem));
            this.AlternateDescription = alternateDescription ?? deviceItem.Description;
        }
    }

    public class DeviceItem
    {
        public string DeviceInstanceId { get; }

        public string Description { get; }

        public byte DisplayIndex { get; }

        public byte MonitorIndex { get; }

        public DeviceItem(
            string deviceInstanceId,
            string description,
            byte displayIndex,
            byte monitorIndex)
        {
            this.DeviceInstanceId = deviceInstanceId;
            this.Description = description;
            this.DisplayIndex = displayIndex;
            this.MonitorIndex = monitorIndex;
        }
    }

    public class DisplayItem
    {
        public string DeviceInstanceId { get; }

        public string DisplayName { get; }

        public bool IsInternal { get; }

        public float RefreshRate { get; }

        public bool IsAvailable { get; }

        public DisplayItem(
            string deviceInstanceId,
            string displayName,
            bool isInternal,
            float refreshRate,
            bool isAvailable)
        {
            this.DeviceInstanceId = deviceInstanceId;
            this.DisplayName = displayName;
            this.IsInternal = isInternal;
            this.RefreshRate = refreshRate;
            this.IsAvailable = isAvailable;
        }
    }

}
