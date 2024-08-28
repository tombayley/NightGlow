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

    // Brightness
    private CancellationTokenSource _cancelTokenB = new CancellationTokenSource();
    private readonly object _lockB = new object();

    // Contrast
    private CancellationTokenSource _cancelTokenC = new CancellationTokenSource();
    private readonly object _lockC = new object();

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
        lock (_lockB)
        {
            _cancelTokenB.Cancel();
            _cancelTokenB.Dispose();
            _cancelTokenB = new CancellationTokenSource();
            var cancelToken = _cancelTokenB.Token;

            Task.Run(() => RetrySet(SetMonitorBrightness, GetBrightness, Monitor.hPhysicalMonitor, value, Brightness, cancelToken), cancelToken);
        }
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
        lock (_lockC)
        {
            _cancelTokenC.Cancel();
            _cancelTokenC.Dispose();
            _cancelTokenC = new CancellationTokenSource();
            var cancelToken = _cancelTokenC.Token;

            Task.Run(() => RetrySet(SetMonitorContrast, GetContrast, Monitor.hPhysicalMonitor, value, Contrast, cancelToken), cancelToken);
        }
    }

    private static void RetrySet(
        Func<nint, uint, bool> setFunction,
        Func<Setting> getFunction,
        nint monitor,
        uint value,
        Setting setting,
        CancellationToken cancelToken
    ) {
        try
        {
            for (int attempt = 1; attempt <= DDC_ATTEMPTS; attempt++)
            {
                // Check for cancellation before attempting the operation
                cancelToken.ThrowIfCancellationRequested();

                // Sometimes setting a monitor value (e.g. brightness) will report as success,
                // but monitor does not change brightness.
                // Confirm value has been set by getting current value after doing the set.
                if (setFunction(monitor, value) && getFunction().Current == value)
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
