using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tools.Configuration;
using GeneralUpdate.Tools.Models;
using GeneralUpdate.Tools.Services;

namespace GeneralUpdate.Tools.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private readonly AppConfig _config;

    [ObservableProperty] private ViewModelBase _currentPage;
    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private string _localeText = "EN";

    public ObservableCollection<NavItem> NavItems { get; } = new();

    public MainWindowViewModel(AppConfig config)
    {
        _config = config;

        // Apply saved theme
        IsDarkTheme = config.Theme == "Dark";
        ApplyTheme(IsDarkTheme);

        // Apply saved locale
        LocaleText = _loc.Locale == "zh-CN" ? "EN" : "中";

        SyncNavItems();

        _loc.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(LocalizationService.Locale))
                return;

            SyncNavItems();
            LocaleText = _loc.Locale == "zh-CN" ? "EN" : "中";
        };

        // Default to Patch page
        CurrentPage = new PatchViewModel(config);
    }

    private void SyncNavItems()
    {
        NavItems.Clear();
        NavItems.Add(new("Patch", _loc["Nav.Patch"], typeof(PatchViewModel), true));
        NavItems.Add(new("Extension", _loc["Nav.Extension"], typeof(ExtensionViewModel), false));
        NavItems.Add(new("OSS", _loc["Nav.OSS"], typeof(OSSViewModel), false));
        NavItems.Add(new("Simulate", _loc["Nav.Simulate"], typeof(SimulateViewModel), false));
        NavItems.Add(new("Config", _loc["Nav.Config"], typeof(ConfigViewModel), false));
        NavItems.Add(new("Settings", _loc["Nav.Settings"], typeof(SettingsViewModel), false));
    }

    [RelayCommand]
    private void Navigate(NavItem item)
    {
        foreach (var n in NavItems) n.IsSelected = false;
        item.IsSelected = true;
        CurrentPage = item.Key switch
        {
            "Patch" => new PatchViewModel(_config),
            "Extension" => new ExtensionViewModel(_config),
            "OSS" => new OSSViewModel(_config),
            "Config" => new ConfigViewModel(_config),
            "Settings" => new SettingsViewModel(_config),
            _ => new SimulateViewModel(_config)
        };
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        ApplyTheme(IsDarkTheme);

        // Persist
        _config.Theme = IsDarkTheme ? "Dark" : "Light";
        _ = ConfigServiceSingleton.Instance.SaveAsync();
    }

    [RelayCommand]
    private void ToggleLocale()
    {
        _loc.Locale = _loc.Locale == "zh-CN" ? "en-US" : "zh-CN";

        // Persist
        _config.Language = _loc.Locale;
        _ = ConfigServiceSingleton.Instance.SaveAsync();
    }

    private static void ApplyTheme(bool isDark)
    {
        var app = Avalonia.Application.Current;
        if (app != null)
            app.RequestedThemeVariant = isDark
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;
    }
}

public partial class NavItem : ObservableObject
{
    public string Key { get; }
    public string Title { get; }
    public System.Type PageType { get; }
    [ObservableProperty] private bool _isSelected;

    public NavItem(string key, string title, System.Type pageType, bool selected)
    {
        Key = key;
        Title = title;
        PageType = pageType;
        _isSelected = selected;
    }
}
