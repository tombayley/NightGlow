using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Input;
using System;
using Microsoft.Extensions.DependencyInjection;
using NightGlow.Views;
using NightGlow.Services;

namespace NightGlow.ViewModels;

public partial class NotifyIconViewModel : ObservableObject, IDisposable
{

    private readonly IServiceProvider _serviceProvider;
    private readonly SettingsService _settingsService;
    private readonly NightGlowService _nightGlowService;

    public SettingsService SettingsService { get => _settingsService; }

    public ICommand ShowWindowCommand { get => new RelayCommand(ShowWindow); }

    public NotifyIconViewModel(
        IServiceProvider serviceProvider,
        SettingsService settingsService,
        NightGlowService nightGlowService
    )
    {
        _serviceProvider = serviceProvider;
        _settingsService = settingsService;
        _nightGlowService = nightGlowService;

        _nightGlowService.RegisterHotKeys();
    }

    public void ShowWindow()
    {
        if (!(Application.Current.MainWindow is MainWindow))
        {
            Application.Current.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        }
        Application.Current.MainWindow.Show();
        Application.Current.MainWindow.Activate();
    }

    [RelayCommand]
    public void ExitApplication()
    {
        Application.Current.Shutdown();
    }

    public void Dispose()
    {

    }

}
