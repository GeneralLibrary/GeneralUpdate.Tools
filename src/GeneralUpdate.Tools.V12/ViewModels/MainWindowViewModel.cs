using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tools.V12.Models;

namespace GeneralUpdate.Tools.V12.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentPage = new PatchViewModel();
    public ObservableCollection<NavItem> NavItems { get; } = new();

    public MainWindowViewModel()
    {
        NavItems.Add(new("Patch", "补丁包", typeof(PatchViewModel), true));
        NavItems.Add(new("Extension", "扩展包", typeof(ExtensionViewModel), false));
        NavItems.Add(new("OSS", "OSS配置", typeof(OSSViewModel), false));
    }

    [RelayCommand] private void Navigate(NavItem item) { foreach (var n in NavItems) n.IsSelected = false; item.IsSelected = true; CurrentPage = item.Key switch { "Patch" => new PatchViewModel(), "Extension" => new ExtensionViewModel(), _ => new OSSViewModel() }; }
}

public partial class NavItem : ObservableObject
{
    public string Key { get; }
    public string Title { get; }
    public System.Type PageType { get; }
    [ObservableProperty] private bool _isSelected;
    public NavItem(string key, string title, System.Type pageType, bool selected) { Key = key; Title = title; PageType = pageType; _isSelected = selected; }
}
