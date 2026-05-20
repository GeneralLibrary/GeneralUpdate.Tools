using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tools.Models;
using GeneralUpdate.Tools.Services;

namespace GeneralUpdate.Tools.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly LocalizationService _loc = LocalizationService.Instance;

    [ObservableProperty] private ViewModelBase _currentPage = new PatchViewModel();
    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private string _localeText = "EN";

    public ObservableCollection<NavItem> NavItems { get; } = new();

    public MainWindowViewModel()
    {
        SyncNavItems();
        _loc.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(LocalizationService.Locale))
                return;

            SyncNavItems();
            LocaleText = _loc.Locale == "zh-CN" ? "EN" : "中";
        };
    }

    private void SyncNavItems()
    {
        NavItems.Clear();
        NavItems.Add(new("Patch", _loc["Nav.Patch"], typeof(PatchViewModel), true));
        NavItems.Add(new("Extension", _loc["Nav.Extension"], typeof(ExtensionViewModel), false));
        NavItems.Add(new("OSS", _loc["Nav.OSS"], typeof(OSSViewModel), false));
    }

    [RelayCommand] private void Navigate(NavItem item)
    {
        foreach (var n in NavItems) n.IsSelected = false;
        item.IsSelected = true;
        CurrentPage = item.Key switch
        {
            "Patch" => new PatchViewModel(),
            "Extension" => new ExtensionViewModel(),
            _ => new OSSViewModel()
        };
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        var app = Avalonia.Application.Current;
        if (app != null)
            app.RequestedThemeVariant = IsDarkTheme
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;
    }

    [RelayCommand]
    private void ToggleLocale()
    {
        _loc.Locale = _loc.Locale == "zh-CN" ? "en-US" : "zh-CN";
    }
}

public partial class NavItem : ObservableObject
{
    public string Key { get; }
    public string Title { get; }
    public System.Type PageType { get; }
    [ObservableProperty] private bool _isSelected;
    public NavItem(string key, string title, System.Type pageType, bool selected) { Key = key; Title = title; PageType = pageType; _isSelected = selected; }
}
