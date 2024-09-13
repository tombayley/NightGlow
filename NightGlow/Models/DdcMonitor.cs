using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static NightGlow.Helper.WinApi;

namespace NightGlow.Models;

public class DdcMonitor
{
    // Sometimes setting/getting monitor settings can fail. Retry this many times before assuming failure.
    private const int DDC_ATTEMPTS = 3;

    public string DeviceInstanceId { get; }
    public string Description { get; }
    public byte DisplayIndex { get; }
    public byte MonitorIndex { get; }
    public RECT MonitorRect { get; }
    public string DeviceName { get; }

    private readonly SafePhysicalMonitorHandle _handle;

    private readonly Setting Brightness = new();
    private readonly Setting Contrast = new();

    // Brightness
    private CancellationTokenSource _cancelTokenB = new CancellationTokenSource();
    private readonly object _lockB = new object();

    public DdcMonitor(
        string deviceInstanceId,
        string description,
        byte displayIndex,
        byte monitorIndex,
        RECT monitorRect,
        SafePhysicalMonitorHandle handle)
    {
        if (string.IsNullOrWhiteSpace(deviceInstanceId))
            throw new ArgumentNullException(nameof(deviceInstanceId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentNullException(nameof(description));

        this.DeviceInstanceId = deviceInstanceId;
        this.Description = description;
        this.DisplayIndex = displayIndex;
        this.MonitorIndex = monitorIndex;
        this.MonitorRect = monitorRect;
        this.DeviceName = $"\\\\.\\DISPLAY{displayIndex}";


        this._handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    private bool RetryGet(Func<SafePhysicalMonitorHandle, bool> getFunction, SafePhysicalMonitorHandle hMonitor)
    {
        for (int attempt = 1; attempt <= DDC_ATTEMPTS; attempt++)
        {
            if (getFunction(hMonitor))
                return true;
        }
        return false;
    }

    private static void RetrySet(
        Func<SafePhysicalMonitorHandle, uint, bool> setFunction,
        Func<Setting> getFunction,
        SafePhysicalMonitorHandle hMonitor,
        uint value,
        Setting setting,
        CancellationToken cancelToken
    )
    {
        try
        {
            for (int attempt = 1; attempt <= DDC_ATTEMPTS; attempt++)
            {
                if (cancelToken.IsCancellationRequested)
                    return;

                // Sometimes setting a monitor value (e.g. brightness) will report as success,
                // but monitor does not change brightness.
                // Confirm value has been set by getting current value after doing the set.
                if (setFunction(hMonitor, value) && getFunction().Current == value)
                {
                    setting.Current = value;
                    return;
                }
            }
        }
        catch (TaskCanceledException)
        {
            Debug.WriteLine("Operation cancelled.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    public Setting GetBrightness()
    {
        bool success = RetryGet((handle) => GetMonitorBrightness(
            _handle, out Brightness.Min, out Brightness.Current, out Brightness.Max), _handle
        );
        return Brightness;
    }

    public void SetBrightness(uint value)
    {
        lock (_lockB)
        {
            _cancelTokenB.Cancel();
            _cancelTokenB.Dispose();
            _cancelTokenB = new CancellationTokenSource();
            var cancelToken = _cancelTokenB.Token;

            Task.Run(() => RetrySet(SetMonitorBrightness, GetBrightness, _handle, value, Brightness, cancelToken), cancelToken);
        }
    }

    public void Dispose()
    {
        _handle.Dispose();
    }
}

public class SafePhysicalMonitorHandle : SafeHandle
{
    public SafePhysicalMonitorHandle(IntPtr handle) : base(IntPtr.Zero, true)
    {
        this.handle = handle; // IntPtr.Zero may be a valid handle.
    }

    public override bool IsInvalid => false; // The validity cannot be checked by the handle.

    protected override bool ReleaseHandle()
    {
        return DestroyPhysicalMonitor(handle);
    }

    [DllImport("Dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyPhysicalMonitor(
        IntPtr hMonitor);

}
