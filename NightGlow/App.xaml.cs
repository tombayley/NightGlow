using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using NightGlow.Properties;
using NightGlow.Services;
using NightGlow.ViewModels;
using NightGlow.Views;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace NightGlow;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    private TaskbarIcon? notifyIcon;

    private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

    public static string Name { get; } = Assembly.GetName().Name!;

    public static Version Version { get; } = Assembly.GetName().Version!;

    public static string VersionString { get; } = Version.ToString(3);

    public static string ExecutablePath { get; } = Path.ChangeExtension(Assembly.Location, "exe");

    public static string UrlReleaseHistory { get; } = "https://github.com/tombayley/NightGlow/releases";

    public App()
    {
        IServiceCollection services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<SettingsService>();
        services.AddSingleton<HotKeyService>();
        services.AddSingleton<GammaService>();
        services.AddSingleton<DdcService>();
        services.AddSingleton<NightGlowService>();

        services.AddSingleton<NotifyIconViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<PopupViewModel>();

        services.AddSingleton<PopupWindow>();
        services.AddTransient<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (Settings.Default.UpgradeRequired)
        {
            Settings.Default.Upgrade();
            Settings.Default.UpgradeRequired = false;
            Settings.Default.Save();
        }

        notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        notifyIcon.ForceCreate();
        notifyIcon.DataContext = _serviceProvider.GetRequiredService<NotifyIconViewModel>();

        SettingsService settingsService = _serviceProvider.GetRequiredService<SettingsService>();

        // Open the main window only on first launch
        if (settingsService.FirstLaunch)
        {
            OpenMainWindow();
            settingsService.FirstLaunch = false;
        }
    }

    private void OpenMainWindow()
    {
        Current.MainWindow ??= _serviceProvider.GetRequiredService<MainWindow>();
        Current.MainWindow.Show();
        Current.MainWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider.Dispose();
        notifyIcon?.Dispose();
        base.OnExit(e);
    }

}
