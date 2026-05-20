using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GeneralUpdate.Tools.Services;

/// <summary>
/// Simple dictionary-based localization. Supports zh-CN and en-US.
/// Usage: LocalizationService.Instance["Patch.Title"]
/// </summary>
public class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    private string _locale = "zh-CN";

    public string Locale
    {
        get => _locale;
        set
        {
            if (_locale != value)
            {
                _locale = value;
                OnPropertyChanged();
                OnPropertyChanged("Item");
            }
        }
    }

    public string this[string key]
    {
        get
        {
            if (_strings.TryGetValue(_locale, out var langDict) && langDict.TryGetValue(key, out var val))
                return val;
            // Fallback to zh-CN
            if (_locale != "zh-CN" && _strings.TryGetValue("zh-CN", out var fallback) &&
                fallback.TryGetValue(key, out var fb))
                return fb;
            return key;
        }
    }

    private readonly Dictionary<string, Dictionary<string, string>> _strings = new()
    {
        ["zh-CN"] = new()
        {
            ["App.Title"] = "GeneralUpdate Tools",
            ["Nav.Patch"] = "补丁包",
            ["Nav.Extension"] = "扩展包",
            ["Nav.OSS"] = "OSS配置",
            ["Patch.Title"] = "补丁包生成",
            ["Patch.CorePaths"] = "核心路径",
            ["Patch.OldDir"] = "旧版本目录",
            ["Patch.NewDir"] = "新版本目录",
            ["Patch.Select"] = "选择",
            ["Patch.OldPlaceholder"] = "选择旧版本应用目录...",
            ["Patch.NewPlaceholder"] = "选择新版本发布目录...",
            ["Patch.PackageInfo"] = "包信息",
            ["Patch.PackageName"] = "包名",
            ["Patch.Version"] = "版本",
            ["Patch.OutputDir"] = "输出目录",
            ["Patch.OutputPlaceholder"] = "桌面 (默认)",
            ["Patch.Build"] = "开始构建",
            ["Patch.Ready"] = "就绪",
            ["Patch.ValidateDirs"] = "请选择新旧版本目录",
            ["Patch.Building"] = "正在生成差分补丁...",
            ["Patch.Comparing"] = "对比目录差异 + 生成补丁...",
            ["Patch.PatchDone"] = "补丁生成完成",
            ["Patch.Packing"] = "打包: {0}",
            ["Patch.Success"] = "成功: {0} ({1:F1} KB)",
            ["Patch.Failed"] = "失败: {0}",
            ["Patch.Error"] = "错误: {0}",
            ["Patch.OldSelected"] = "旧版本: {0}",
            ["Patch.NewSelected"] = "新版本: {0}",
            ["Patch.TempDir"] = "临时目录: {0}",
            ["Ext.Title"] = "扩展包生成",
            ["Ext.BasicInfo"] = "基本信息",
            ["Ext.Name"] = "名称",
            ["Ext.Version"] = "版本",
            ["Ext.Description"] = "描述",
            ["Ext.DescPlaceholder"] = "扩展功能描述...",
            ["Ext.Publisher"] = "发布者",
            ["Ext.License"] = "许可证",
            ["Ext.LicensePlaceholder"] = "MIT",
            ["Ext.Paths"] = "路径",
            ["Ext.ExtDir"] = "扩展目录",
            ["Ext.ExportDir"] = "导出目录",
            ["Ext.CustomProps"] = "自定义属性",
            ["Ext.Key"] = "Key",
            ["Ext.Value"] = "Value",
            ["Ext.AddProp"] = "+ 添加",
            ["Ext.Generate"] = "生成扩展包",
            ["Ext.ValidateNameVer"] = "请填写扩展名称和版本",
            ["Ext.ValidateDir"] = "请选择有效的扩展目录",
            ["Ext.Building"] = "正在生成扩展包...",
            ["Ext.Success"] = "成功: {0}",
            ["Ext.Failed"] = "失败: {0}",
            ["OSS.Title"] = "OSS 配置生成",
            ["OSS.NewEntry"] = "新建条目",
            ["OSS.PacketName"] = "包名",
            ["OSS.Version"] = "版本",
            ["OSS.Url"] = "URL",
            ["OSS.SHA256"] = "SHA256",
            ["OSS.ComputeHash"] = "计算",
            ["OSS.AddToList"] = "添加到列表",
            ["OSS.ConfigList"] = "配置列表",
            ["OSS.Clear"] = "清空",
            ["OSS.Export"] = "导出 JSON",
            ["OSS.Added"] = "已添加",
            ["OSS.Cleared"] = "已清空",
            ["OSS.Exported"] = "导出: {0} 条",
            ["OSS.HashResult"] = "SHA256: {0}",
            ["Theme.Light"] = "浅色",
            ["Theme.Dark"] = "深色",
            ["Theme.Toggle"] = "切换主题",
            ["Nav.Simulate"] = "模拟更新",
            ["Sim.Title"] = "模拟更新",
            ["Sim.TestTarget"] = "测试目标",
            ["Sim.OldAppDir"] = "旧版本应用目录",
            ["Sim.PatchFile"] = "补丁包文件",
            ["Sim.Select"] = "选择",
            ["Sim.UpdateConfig"] = "更新配置",
            ["Sim.CurrentVer"] = "当前版本",
            ["Sim.TargetVer"] = "目标版本",
            ["Sim.Platform"] = "平台",
            ["Sim.AppType"] = "应用类型",
            ["Sim.AppSecret"] = "应用密钥",
            ["Sim.ProductId"] = "产品ID",
            ["Sim.Output"] = "输出",
            ["Sim.OutputDir"] = "模拟目录",
            ["Sim.Start"] = "开始模拟",
            ["Sim.SelectAppDir"] = "选择旧版本应用目录",
            ["Sim.SelectPatch"] = "选择补丁包",
            ["Sim.SelectOutput"] = "选择模拟输出目录",
            ["Sim.ValidateDirs"] = "请填写所有必填项",
            ["Sim.DotnetCheck"] = "需要 .NET 10.0 SDK，请先安装",
            ["Sim.AutoRun"] = "自动启动服务器并运行客户端",
            ["Sim.CompileUpgrade"] = "编译 upgrade 为 .exe",
            ["Sim.ManualMode"] = "服务器/客户端已生成，可手动运行:\ndotnet script client.csx",
            ["Sim.Starting"] = "正在启动模拟...",
            ["Sim.Completed"] = "模拟完成 ({0:F1}s)",
            ["Sim.Failed"] = "模拟失败: {0}",
            ["Sim.Report"] = "报告: {0}",
        },
        ["en-US"] = new()
        {
            ["App.Title"] = "GeneralUpdate Tools",
            ["Nav.Patch"] = "Patch",
            ["Nav.Extension"] = "Extension",
            ["Nav.OSS"] = "OSS Config",
            ["Patch.Title"] = "Patch Package",
            ["Patch.CorePaths"] = "Core Paths",
            ["Patch.OldDir"] = "Old Directory",
            ["Patch.NewDir"] = "New Directory",
            ["Patch.Select"] = "Select",
            ["Patch.OldPlaceholder"] = "Select old version directory...",
            ["Patch.NewPlaceholder"] = "Select new version directory...",
            ["Patch.PackageInfo"] = "Package Info",
            ["Patch.PackageName"] = "Package Name",
            ["Patch.Version"] = "Version",
            ["Patch.OutputDir"] = "Output Directory",
            ["Patch.OutputPlaceholder"] = "Desktop (default)",
            ["Patch.Build"] = "Build",
            ["Patch.Ready"] = "Ready",
            ["Patch.ValidateDirs"] = "Please select both old and new directories",
            ["Patch.Building"] = "Generating diff patch...",
            ["Patch.Comparing"] = "Comparing directories + generating patches...",
            ["Patch.PatchDone"] = "Patch generation complete",
            ["Patch.Packing"] = "Packing: {0}",
            ["Patch.Success"] = "Success: {0} ({1:F1} KB)",
            ["Patch.Failed"] = "Failed: {0}",
            ["Patch.Error"] = "Error: {0}",
            ["Patch.OldSelected"] = "Old version: {0}",
            ["Patch.NewSelected"] = "New version: {0}",
            ["Patch.TempDir"] = "Temp directory: {0}",
            ["Ext.Title"] = "Extension Package",
            ["Ext.BasicInfo"] = "Basic Info",
            ["Ext.Name"] = "Name",
            ["Ext.Version"] = "Version",
            ["Ext.Description"] = "Description",
            ["Ext.DescPlaceholder"] = "Extension description...",
            ["Ext.Publisher"] = "Publisher",
            ["Ext.License"] = "License",
            ["Ext.LicensePlaceholder"] = "MIT",
            ["Ext.Paths"] = "Paths",
            ["Ext.ExtDir"] = "Extension Directory",
            ["Ext.ExportDir"] = "Export Directory",
            ["Ext.CustomProps"] = "Custom Properties",
            ["Ext.Key"] = "Key",
            ["Ext.Value"] = "Value",
            ["Ext.AddProp"] = "+ Add",
            ["Ext.Generate"] = "Generate Extension",
            ["Ext.ValidateNameVer"] = "Please fill in extension name and version",
            ["Ext.ValidateDir"] = "Please select a valid extension directory",
            ["Ext.Building"] = "Generating extension package...",
            ["Ext.Success"] = "Success: {0}",
            ["Ext.Failed"] = "Failed: {0}",
            ["OSS.Title"] = "OSS Config Generator",
            ["OSS.NewEntry"] = "New Entry",
            ["OSS.PacketName"] = "Package Name",
            ["OSS.Version"] = "Version",
            ["OSS.Url"] = "URL",
            ["OSS.SHA256"] = "SHA256",
            ["OSS.ComputeHash"] = "Compute",
            ["OSS.AddToList"] = "Add to List",
            ["OSS.ConfigList"] = "Config List",
            ["OSS.Clear"] = "Clear",
            ["OSS.Export"] = "Export JSON",
            ["OSS.Added"] = "Added",
            ["OSS.Cleared"] = "Cleared",
            ["OSS.Exported"] = "Exported: {0} entries",
            ["OSS.HashResult"] = "SHA256: {0}",
            ["Theme.Light"] = "Light",
            ["Theme.Dark"] = "Dark",
            ["Theme.Toggle"] = "Toggle Theme",
            ["Nav.Simulate"] = "Simulate",
            ["Sim.Title"] = "Simulate Update",
            ["Sim.TestTarget"] = "Test Target",
            ["Sim.OldAppDir"] = "Old App Directory",
            ["Sim.PatchFile"] = "Patch Package",
            ["Sim.Select"] = "Select",
            ["Sim.UpdateConfig"] = "Update Config",
            ["Sim.CurrentVer"] = "Current Version",
            ["Sim.TargetVer"] = "Target Version",
            ["Sim.Platform"] = "Platform",
            ["Sim.AppType"] = "App Type",
            ["Sim.AppSecret"] = "App Secret",
            ["Sim.ProductId"] = "Product ID",
            ["Sim.Output"] = "Output",
            ["Sim.OutputDir"] = "Simulate Directory",
            ["Sim.Start"] = "Start Simulation",
            ["Sim.SelectAppDir"] = "Select old version directory",
            ["Sim.SelectPatch"] = "Select patch package",
            ["Sim.SelectOutput"] = "Select simulation output directory",
            ["Sim.ValidateDirs"] = "Please fill in all required fields",
            ["Sim.DotnetCheck"] = ".NET 10.0 SDK is required. Please install it first.",
            ["Sim.AutoRun"] = "Auto-start server and run client",
            ["Sim.CompileUpgrade"] = "Compile upgrade to .exe",
            ["Sim.ManualMode"] = "Server/client generated. Run manually:\ndotnet script client.csx",
            ["Sim.Starting"] = "Starting simulation...",
            ["Sim.Completed"] = "Simulation completed ({0:F1}s)",
            ["Sim.Failed"] = "Simulation failed: {0}",
            ["Sim.Report"] = "Report: {0}",
        }
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public string T(string key) => this[key];
    public string T(string key, params object[] args) => string.Format(this[key], args);
}