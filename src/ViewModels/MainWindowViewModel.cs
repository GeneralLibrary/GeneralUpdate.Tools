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
    [ObservableProperty] private string _themeLabel = string.Empty;
    [ObservableProperty] private string _localeLabel = string.Empty;

    public ObservableCollection<NavItem> NavItems { get; } = new();

    public MainWindowViewModel(AppConfig config)
    {
        _config = config;

        // Apply saved theme
        IsDarkTheme = config.Theme == "Dark";
        ApplyTheme(IsDarkTheme);

        // Apply saved locale
        SyncLabels();
        SyncNavItems();

        _loc.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(LocalizationService.Locale))
                return;

            SyncNavItems();
            SyncLabels();
        };

        // Default to Patch page
        CurrentPage = new PatchViewModel(config);
    }

    partial void OnIsDarkThemeChanged(bool value) => SyncThemeLabel();

    private void SyncLabels()
    {
        SyncThemeLabel();
        SyncLocaleLabel();
    }

    private void SyncThemeLabel()
    {
        // Show what you'll switch TO: light → "暗", dark → "明"
        ThemeLabel = IsDarkTheme ? _loc["Theme.Light"] : _loc["Theme.Dark"];
    }

    private void SyncLocaleLabel()
    {
        // Show what you'll switch TO: zh-CN → "英", en-US → "中"
        LocaleLabel = _loc.Locale == "zh-CN" ? _loc["Locale.En"] : _loc["Locale.Zh"];
    }

    private void SyncNavItems()
    {
        NavItems.Clear();
        NavItems.Add(new("Patch", _loc["Nav.Patch"], "\U0001FA79", typeof(PatchViewModel), true));
        NavItems.Add(new("Extension", _loc["Nav.Extension"], "\U0001F9E9", typeof(ExtensionViewModel), false));
        NavItems.Add(new("OSS", _loc["Nav.OSS"], "☁️", typeof(OSSViewModel), false));
        NavItems.Add(new("Simulate", _loc["Nav.Simulate"], "\U0001F504", typeof(SimulateViewModel), false));
        NavItems.Add(new("Mobile", _loc["Nav.Mobile"], "\U0001F4F1", typeof(MobilePackagingViewModel), false));
        NavItems.Add(new("Config", _loc["Nav.Config"], "⚙️", typeof(ConfigViewModel), false));
        NavItems.Add(new("Settings", _loc["Nav.Settings"], "\U0001F6E0", typeof(SettingsViewModel), false));
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
            "Mobile" => new MobilePackagingViewModel(_config),
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
        ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);
    }

    [RelayCommand]
    private void ToggleLocale()
    {
        var newLocale = _loc.Locale == "zh-CN" ? "en-US" : "zh-CN";

        // Update legacy localization (triggers PropertyChanged → Lingua syncs via listener)
        _loc.Locale = newLocale;

        // Also update Lingua directly for reactive observable subscribers
        Irihi.Lingua.AppLanguageManager.Instance.UpdateCulture(
            new System.Globalization.CultureInfo(newLocale));

        // Persist
        _config.Language = newLocale;
        ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);
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
    public string Icon { get; }
    public System.Type PageType { get; }
    [ObservableProperty] private bool _isSelected;

    public NavItem(string key, string title, string icon, System.Type pageType, bool selected)
    {
        Key = key;
        Title = title;
        Icon = icon;
        PageType = pageType;
        _isSelected = selected;
    }
}
