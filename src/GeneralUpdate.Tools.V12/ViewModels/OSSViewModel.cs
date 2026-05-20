using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tools.V12.Models;
using GeneralUpdate.Tools.V12.Services;
using Newtonsoft.Json;

namespace GeneralUpdate.Tools.V12.ViewModels;

public partial class OSSViewModel : ViewModelBase
{
    private readonly HashService _hash = new();
    public ObservableCollection<OSSConfigModel> Configs { get; } = new();
    [ObservableProperty] private OSSConfigModel _current = new();
    [ObservableProperty] private string _status = "就绪";

    [RelayCommand] async Task ComputeHash() { var tl = Avalonia.Controls.TopLevel.GetTopLevel((Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow); if (tl == null) return; var files = await tl.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions { Title = "选择文件", AllowMultiple = false }); if (files.Count > 0) { Current.Hash = await _hash.ComputeHashAsync(files[0].Path.LocalPath); Status = $"SHA256: {Current.Hash}"; } }
    [RelayCommand] void Append() { Configs.Add(new() { PacketName = Current.PacketName, Hash = Current.Hash, Version = Current.Version, Url = Current.Url, ReleaseDate = Current.ReleaseDate }); Status = "已添加"; }
    [RelayCommand] void Remove(OSSConfigModel? item) { if (item != null) Configs.Remove(item); }
    [RelayCommand] void Clear() { Configs.Clear(); Status = "已清空"; }
    [RelayCommand] async Task Export() { var tl = Avalonia.Controls.TopLevel.GetTopLevel((Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow); if (tl == null) return; var file = await tl.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions { Title = "导出JSON", DefaultExtension = ".json", SuggestedFileName = "oss_config.json" }); if (file != null) { await File.WriteAllTextAsync(file.Path.LocalPath, JsonConvert.SerializeObject(Configs, Formatting.Indented), System.Text.Encoding.UTF8); Status = $"导出: {Configs.Count} 条"; } }
}
