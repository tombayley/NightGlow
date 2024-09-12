using NightGlow.Models;

namespace NightGlow.Services;

public class DdcService
{

    private readonly SettingsService _settingsService;

    public readonly Monitors _monitors;

    public Monitors Monitors { get => _monitors; }

    public DdcService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _monitors = new Monitors();
        UpdateMonitors();
    }

    public void UpdateMonitors()
    {
        // TODO limit calls to ExecuteUpdateMonitors. Make sure most recent call is performed, but queued are discarded
        ExecuteUpdateMonitors();
    }

    private void ExecuteUpdateMonitors()
    {
        Monitors.Scan(_settingsService);
    }

    public void SetBrightness(DdcMonitor monitor, int value)
    {
        monitor.SetBrightness((uint)value);
    }

    public void SetContrast(DdcMonitor monitor, int value)
    {
        //monitor.SetContrast((uint)value);
    }

}
