using CommunityToolkit.Mvvm.ComponentModel;
using NightGlow.Services;

namespace NightGlow.ViewModels;

public partial class PopupViewModel : ObservableObject
{

    private readonly SettingsService _settingsService;
    private readonly NightGlowService _nightGlowService;

    public SettingsService SettingsService { get => _settingsService; }
    public NightGlowService NightGlowService { get => _nightGlowService; }

    public PopupViewModel(SettingsService settingsService, NightGlowService nightGlowService)
    {
        _settingsService = settingsService;
        _nightGlowService = nightGlowService;
    }

}
