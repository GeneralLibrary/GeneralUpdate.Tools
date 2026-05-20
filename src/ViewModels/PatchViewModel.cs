using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tools.Models;
using GeneralUpdate.Tools.Services;

namespace GeneralUpdate.Tools.ViewModels;

public partial class PatchViewModel : ViewModelBase
{
    private readonly DiffService _diff = new();
    private readonly PackageService _pkg = new();
    public PatchConfigModel Config { get; } = new();
    [ObservableProperty] private bool _isBuilding;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _status = "就绪";
    [ObservableProperty] private ObservableCollection<string> _log = new();

    async Task<string?> Pick() { var tl = Avalonia.Controls.TopLevel.GetTopLevel((Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow); if (tl == null) return null; var r = await tl.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = "选择目录", AllowMultiple = false }); return r.Count > 0 ? r[0].Path.LocalPath : null; }

    [RelayCommand] async Task SelectOld() { var p = await Pick(); if (p != null) { Config.OldDirectory = p; L($"旧版本: {p}"); } }
    [RelayCommand] async Task SelectNew() { var p = await Pick(); if (p != null) { Config.NewDirectory = p; L($"新版本: {p}"); } }
    [RelayCommand] async Task SelectOut() { var p = await Pick(); if (p != null) Config.OutputPath = p; }

    [RelayCommand] async Task Build()
    {
        if (string.IsNullOrWhiteSpace(Config.OldDirectory) || string.IsNullOrWhiteSpace(Config.NewDirectory)) { Status = "请选择新旧版本目录"; return; }
        IsBuilding = true; Log.Clear(); Progress<> = 0; Status = "正在生成差分补丁...";
        try
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"gupatch_{DateTime.Now:yyyyMMddHHmmss}"); Directory.CreateDirectory(tmp);
            L($"临时目录: {tmp}"); Progress<> = 20;
            L("对比目录差异 + 生成 BSDiff40 补丁...");
            await _diff.GeneratePatchAsync(Config.OldDirectory, Config.NewDirectory, tmp);
            Progress<> = 70; L("补丁生成完成");
            var outDir = string.IsNullOrWhiteSpace(Config.OutputPath) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : Config.OutputPath;
            var name = string.IsNullOrWhiteSpace(Config.PackageName) ? $"patch_{DateTime.Now:yyyyMMddHHmmss}" : Config.PackageName;
            var zip = Path.Combine(outDir, $"{name}.zip");
            L($"打包: {Path.GetFileName(zip)}");
            await _pkg.CompressDirectoryAsync(tmp, zip);
            Directory.Delete(tmp, true);
            Progress<> = 100; Config.OutputPath = zip;
            Status = $"成功: {Path.GetFileName(zip)} ({new FileInfo(zip).Length/1024.0:F1} KB)";
            L(Status);
        }
        catch (Exception ex) { Status = $"失败: {ex.Message}"; L($"错误: {ex}"); }
        finally { IsBuilding = false; }
    }
    void L(string m) => Log.Add($"[{DateTime.Now:HH:mm:ss}] {m}");
}
