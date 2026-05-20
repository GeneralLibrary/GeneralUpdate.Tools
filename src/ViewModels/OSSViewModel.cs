using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tools.Models;
using GeneralUpdate.Tools.Services;
using Newtonsoft.Json;

namespace GeneralUpdate.Tools.ViewModels;

public partial class OSSViewModel : ViewModelBase
{
    private readonly HashService _hash = new();
    private readonly LocalizationService _loc = LocalizationService.Instance;

    public ObservableCollection<OSSConfigModel> Configs { get; } = new();
    [ObservableProperty] private OSSConfigModel _current = new();
    [ObservableProperty] private string _status;

    public OSSViewModel() { _status = _loc["Patch.Ready"]; }

    private string GetOpenFilePickerTitle()
    {
        var title = _loc["Patch.SelectFile"];
        if (string.IsNullOrWhiteSpace(title) || title == "Patch.SelectFile" || title == _loc["Patch.Select"])
        {
            return "选择文件";
        }

        return title;
    }

    [RelayCommand] async Task ComputeHash()
    {
        var tl = Avalonia.Controls.TopLevel.GetTopLevel((Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow);
        if (tl == null) return;
        var files = await tl.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions { Title = GetOpenFilePickerTitle(), AllowMultiple = false });
        if (files.Count > 0) { Current.Hash = await _hash.ComputeHashAsync(files[0].Path.LocalPath); Status = _loc.T("OSS.HashResult", Current.Hash); }
    }
    [RelayCommand] void Append() { Configs.Add(new() { PacketName = Current.PacketName, Hash = Current.Hash, Version = Current.Version, Url = Current.Url, ReleaseDate = Current.ReleaseDate }); Status = _loc["OSS.Added"]; }
    [RelayCommand] void Remove(OSSConfigModel? item) { if (item != null) Configs.Remove(item); }
    [RelayCommand] void Clear() { Configs.Clear(); Status = _loc["OSS.Cleared"]; }
    [RelayCommand] async Task Export()
    {
        var tl = Avalonia.Controls.TopLevel.GetTopLevel((Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow);
        if (tl == null) return;
        var file = await tl.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions { Title = _loc["OSS.Export"], DefaultExtension = ".json", SuggestedFileName = "oss_config.json" });
        if (file != null) { await File.WriteAllTextAsync(file.Path.LocalPath, JsonConvert.SerializeObject(Configs, Formatting.Indented), System.Text.Encoding.UTF8); Status = _loc.T("OSS.Exported", Configs.Count); }
    }
}
