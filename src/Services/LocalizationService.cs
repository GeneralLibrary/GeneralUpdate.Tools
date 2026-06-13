using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;

namespace GeneralUpdate.Tools.Services;

/// <summary>
/// Localization service that loads translations from embedded JSON files at runtime.
/// Supports runtime locale switching and falls back to built-in dictionaries
/// if the JSON files are unavailable.
///
/// Usage: LocalizationService.Instance["Patch.Title"]
/// </summary>
public class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    private string _locale = "zh-CN";

    /// <summary>
    /// Current locale code (e.g. "zh-CN", "en-US").
    /// Changing this triggers PropertyChanged for both "Locale" and "Item"
    /// so that all bindings refresh.
    /// </summary>
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

    /// <summary>
    /// Indexer for XAML bindings: <c>{Binding [Key]}</c>
    /// </summary>
    public string this[string key]
    {
        get
        {
            // Try the current locale first
            if (_cache.TryGetValue(_locale, out var langDict) && langDict.TryGetValue(key, out var val))
                return val;

            // Fallback to zh-CN
            if (_locale != "zh-CN" && _cache.TryGetValue("zh-CN", out var fallback) &&
                fallback.TryGetValue(key, out var fb))
                return fb;

            // Try built-in fallback regardless of whether JSON resources loaded
            if (FallbackStrings.TryGetValue(_locale, out var fbDict) &&
                fbDict.TryGetValue(key, out var fbVal))
                return fbVal;
            if (_locale != "zh-CN" &&
                FallbackStrings.TryGetValue("zh-CN", out var fbZh) && fbZh.TryGetValue(key, out var fbZhVal))
                return fbZhVal;

            return key;
        }
    }

    /// <summary>
    /// Returns a localized string (same as indexer).
    /// </summary>
    public string T(string key) => this[key];

    /// <summary>
    /// Returns a formatted localized string.
    /// </summary>
    public string T(string key, params object[] args) => string.Format(this[key], args);

    /// <summary>
    /// Return a copy of all strings for a given locale.
    /// Used by <see cref="Irihi.Lingua.AppLanguageManager"/> to seed the Lingua resource store.
    /// </summary>
    public IReadOnlyDictionary<string, string> GetAllStrings(string locale)
    {
        var result = new Dictionary<string, string>();

        // Prefer cached (loaded from JSON), then fall back to built-in
        if (_cache.TryGetValue(locale, out var cached))
        {
            foreach (var kvp in cached) result[kvp.Key] = kvp.Value;
        }
        else if (FallbackStrings.TryGetValue(locale, out var fb))
        {
            foreach (var kvp in fb) result[kvp.Key] = kvp.Value;
        }

        return result;
    }

    /// <summary>
    /// Load translations from embedded JSON files bundled as Avalonia resources.
    /// Called once at startup; idempotent.
    /// </summary>
    public void LoadFromResources()
    {
        try
        {
            var loader = Avalonia.Platform.AssetLoader.Open;
            foreach (var locale in SupportedLocales)
            {
                var uri = new Uri($"avares://GeneralUpdate.Tools/Resources/Locales/{locale}.json");
                using var stream = loader(uri);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var json = reader.ReadToEnd();
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (dict != null)
                    _cache[locale] = dict;
            }
        }
        catch
        {
            // AssetLoader not available (design mode, unit tests):
            // fall back to the built-in dictionaries.
            foreach (var kvp in FallbackStrings)
                if (!_cache.ContainsKey(kvp.Key))
                    _cache[kvp.Key] = new Dictionary<string, string>(kvp.Value);
        }
    }

    // ── Supported locales ────────────────────────────────────

    /// <summary>Locales for which JSON files exist.</summary>
    public static readonly string[] SupportedLocales = { "zh-CN", "en-US" };

    // ── Internal cache ───────────────────────────────────────

    private readonly Dictionary<string, Dictionary<string, string>> _cache = new();

    // ── INotifyPropertyChanged ───────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ── Built-in fallback dictionaries ───────────────────────
    //     Mirrors the JSON files. Used when AssetLoader is unavailable
    //     (design mode, unit tests, or file read failure).

    private static readonly Dictionary<string, Dictionary<string, string>> FallbackStrings = new()
    {
        // Same content as zh-CN.json and en-US.json — kept in sync for resilience.
        // These are indexed by locale code and used only as a last resort.
        ["zh-CN"] = BuildZhCN(),
        ["en-US"] = BuildEnUS(),
    };

    private static Dictionary<string, string> BuildZhCN() => new()
    {
        ["App.Title"] = "GeneralUpdate Tools",
        ["Nav.Patch"] = "补丁包", ["Nav.Extension"] = "扩展包", ["Nav.OSS"] = "OSS配置",
        ["Nav.Simulate"] = "模拟更新", ["Nav.Config"] = "配置生成", ["Nav.Settings"] = "设置",
        ["Patch.Title"] = "🩹 补丁包生成", ["Patch.CorePaths"] = "核心路径",
        ["Patch.OldDir"] = "旧版本目录", ["Patch.NewDir"] = "新版本目录",
        ["Patch.Select"] = "选择", ["Patch.OldPlaceholder"] = "选择旧版本应用目录...",
        ["Patch.NewPlaceholder"] = "选择新版本发布目录...", ["Patch.PackageInfo"] = "包信息",
        ["Patch.PackageName"] = "包名", ["Patch.Version"] = "版本", ["Patch.OutputDir"] = "输出目录",
        ["Patch.OutputPlaceholder"] = "桌面 (默认)", ["Patch.Build"] = "开始构建",
        ["Patch.Ready"] = "就绪", ["Patch.ValidateDirs"] = "请选择新旧版本目录",
        ["Patch.Building"] = "正在生成差分补丁...", ["Patch.Comparing"] = "对比目录差异 + 生成补丁...",
        ["Patch.PatchDone"] = "补丁生成完成", ["Patch.Packing"] = "打包: {0}",
        ["Patch.Success"] = "成功: {0} ({1:F1} KB)", ["Patch.Failed"] = "失败: {0}",
        ["Patch.Error"] = "错误: {0}",
        ["Patch.InvalidVersion"] = "版本号 '{0}' 不符合 semver 规范 (https://semver.org/lang/zh-CN/)。示例: 1.0.0",
        ["Patch.OldSelected"] = "旧版本: {0}", ["Patch.NewSelected"] = "新版本: {0}",
        ["Patch.TempDir"] = "临时目录: {0}",
        ["Patch.EncryptionCheck"] = "打包前检测加密文件",
        ["Patch.EncryptionCheckTip"] = "扫描补丁包中是否包含被加密/加壳的文件",
        ["Patch.Scanning"] = "正在扫描加密文件...", ["Patch.ScanFound"] = "检测到 {0} 个可疑文件",
        ["Patch.ScanClean"] = "未检测到加密文件",
        ["Patch.ScanDialogTitle"] = "⚠️ 检测到加密/可疑文件",
        ["Patch.ScanDialogHeader"] = "以下文件可能存在加密或加壳保护，打包后可能导致客户端更新失败：",
        ["Patch.ScanHighRisk"] = "🔴 高风险", ["Patch.ScanMediumRisk"] = "🟡 中风险",
        ["Patch.ScanLowRisk"] = "⚪ 低风险", ["Patch.ScanFilesScanned"] = "共扫描 {0} 个文件",
        ["Patch.ScanBtnSkip"] = "跳过可疑文件，继续打包", ["Patch.ScanBtnInclude"] = "仍然打包全部文件",
        ["Patch.ScanBtnCancel"] = "取消", ["Patch.ScanSkipped"] = "已跳过 {0} 个可疑文件",
        ["Patch.ScanCancelled"] = "用户取消打包",
        ["Ext.Title"] = "🧩 扩展包生成", ["Ext.BasicInfo"] = "基本信息",
        ["Ext.Name"] = "名称", ["Ext.Version"] = "版本", ["Ext.Description"] = "描述",
        ["Ext.DescPlaceholder"] = "扩展功能描述...", ["Ext.Publisher"] = "发布者",
        ["Ext.License"] = "许可证", ["Ext.LicensePlaceholder"] = "MIT",
        ["Ext.Paths"] = "路径", ["Ext.ExtDir"] = "扩展目录", ["Ext.ExportDir"] = "导出目录",
        ["Ext.CustomProps"] = "自定义属性", ["Ext.Key"] = "Key", ["Ext.Value"] = "Value",
        ["Ext.AddProp"] = "+ 添加", ["Ext.Generate"] = "生成扩展包",
        ["Ext.ValidateNameVer"] = "请填写扩展名称和版本",
        ["Ext.InvalidVersion"] = "版本号 '{0}' 不符合 semver 规范。示例: 1.0.0",
        ["Ext.ValidateDir"] = "请选择有效的扩展目录",
        ["Ext.Building"] = "正在生成扩展包...", ["Ext.Success"] = "成功: {0}", ["Ext.Failed"] = "失败: {0}",
        ["OSS.Title"] = "☁️ OSS 配置生成", ["OSS.NewEntry"] = "新建条目",
        ["OSS.PacketName"] = "包名", ["OSS.Version"] = "版本", ["OSS.Url"] = "URL",
        ["OSS.SHA256"] = "SHA256", ["OSS.ComputeHash"] = "计算", ["OSS.AddToList"] = "添加到列表",
        ["OSS.ConfigList"] = "配置列表", ["OSS.Clear"] = "清空", ["OSS.Export"] = "导出 JSON",
        ["OSS.Added"] = "已添加", ["OSS.Cleared"] = "已清空", ["OSS.Exported"] = "导出: {0} 条",
        ["OSS.HashResult"] = "SHA256: {0}",
        ["OSS.InvalidVersion"] = "版本号 '{0}' 不符合 semver 规范。示例: 1.0.0",
        ["Theme.Light"] = "明", ["Theme.Dark"] = "暗", ["Theme.Toggle"] = "切换主题",
        ["Locale.Zh"] = "中", ["Locale.En"] = "英", ["Locale.Toggle"] = "切换语言",
        ["Sim.Title"] = "🔄 模拟更新", ["Sim.TestTarget"] = "测试目标",
        ["Sim.OldAppDir"] = "旧版本应用目录", ["Sim.PatchFile"] = "补丁包文件",
        ["Sim.Select"] = "选择", ["Sim.UpdateConfig"] = "更新配置",
        ["Sim.CurrentVer"] = "当前版本", ["Sim.TargetVer"] = "目标版本",
        ["Sim.Platform"] = "平台", ["Sim.AppType"] = "应用类型",
        ["Sim.AppSecret"] = "应用密钥", ["Sim.ProductId"] = "产品ID",
        ["Sim.UpdatePath"] = "更新路径", ["Sim.Output"] = "输出",
        ["Sim.OutputDir"] = "模拟目录", ["Sim.Start"] = "开始模拟",
        ["Sim.SelectAppDir"] = "选择旧版本应用目录", ["Sim.SelectPatch"] = "选择补丁包",
        ["Sim.SelectOutput"] = "选择模拟输出目录", ["Sim.ValidateDirs"] = "请填写所有必填项",
        ["Sim.InvalidVersion"] = "版本号 '{0}' 不符合 semver 规范。示例: 1.0.0",
        ["Sim.DotnetCheck"] = "需要 .NET 10.0 SDK，请先安装",
        ["Sim.AutoRun"] = "自动启动服务器并运行客户端",
        ["Sim.ManualMode"] = "服务器/客户端已生成，可手动运行:\ndotnet script client.csx",
        ["Sim.Starting"] = "正在启动模拟...", ["Sim.Completed"] = "模拟完成 ({0:F1}s)",
        ["Sim.Failed"] = "模拟失败: {0}", ["Sim.Report"] = "报告: {0}",
        ["Config.Title"] = "⚙️ 配置生成器",
        ["Config.ClientPath"] = "Client 项目 (.csproj)", ["Config.UpgradePath"] = "Upgrade 项目 (.csproj)",
        ["Config.Browse"] = "浏览", ["Config.BrowseClient"] = "选择 Client 项目文件",
        ["Config.BrowseUpgrade"] = "选择 Upgrade 项目文件",
        ["Config.Analyze"] = "分析", ["Config.Analyzing"] = "正在分析...",
        ["Config.Analyzed"] = "分析完成", ["Config.Fields"] = "配置字段",
        ["Config.AutoAnalyzed"] = "── 自动分析（AssemblyName）──",
        ["Config.ManualInput"] = "── 手动输入（版本号需符合 semver）──",
        ["Config.MainAppName"] = "主程序名称", ["Config.ClientVersion"] = "主程序版本",
        ["Config.UpdateAppName"] = "升级器名称", ["Config.UpgradeClientVersion"] = "升级器版本",
        ["Config.AppType"] = "应用类型", ["Config.ProductId"] = "产品 ID",
        ["Config.UpdatePath"] = "升级器子目录", ["Config.Generate"] = "生成配置文件",
        ["Config.Generated"] = "已生成配置文件", ["Config.GenerateSample"] = "生成示例项目结构",
        ["Config.SampleGenerated"] = "示例已生成", ["Config.Publishing"] = "正在 dotnet publish...",
        ["Config.Preview"] = "JSON 预览",
        ["Config.CodeHint"] = "new GeneralUpdateBootstrap()\n    .SetSource(\"https://...\", \"your-key\", \"your-token\", \"https\")\n    .LaunchAsync();",
        ["Config.NoClientPath"] = "请选择 Client 项目路径", ["Config.Failed"] = "失败",
        ["Config.OpenManifestDir"] = "生成后打开 manifest.json 所在目录",
        ["Config.OpenSampleDir"] = "生成后打开示例项目结构目录",
        ["Settings.UploadSection"] = "上传服务器配置",
        ["Settings.UploadDesc"] = "配置补丁包自动上传的目标服务器",
        ["Settings.ServerUrl"] = "服务器地址", ["Settings.UploadEndpoint"] = "上传接口路径",
        ["Settings.Timeout"] = "超时时间 (秒)", ["Settings.RetryCount"] = "重试次数",
        ["Settings.AutoUpload"] = "补丁生成后自动上传", ["Settings.TestConnection"] = "测试连接",
        ["Settings.Testing"] = "正在测试连接...", ["Settings.ConnectionOk"] = "✓ 连接成功",
        ["Settings.ConnectionFailed"] = "✗ 连接失败", ["Settings.AuthSection"] = "身份认证",
        ["Settings.AuthScheme"] = "认证方式", ["Settings.Username"] = "用户名",
        ["Settings.Password"] = "密码", ["Settings.Token"] = "Token / JWT",
        ["Settings.LoginUrl"] = "登录接口地址（可选）", ["Settings.ApiKeyHeader"] = "API Key 请求头名称",
        ["Settings.ApiKey"] = "API Key", ["Settings.SimSection"] = "模拟测试",
        ["Settings.SimPort"] = "本地服务器端口", ["Settings.SimRequireAuth"] = "模拟服务器需要身份认证",
        ["Settings.FeatureSection"] = "功能开关", ["Settings.EncryptionScan"] = "打包前检测加密文件",
        ["Settings.AutoSemver"] = "自动校验 SemVer 版本号", ["Settings.JsonPreview"] = "显示 JSON 预览",
        ["Settings.Save"] = "保存设置", ["Settings.Saved"] = "✓ 设置已保存",
        ["Settings.Reset"] = "恢复默认", ["Settings.ResetDone"] = "已恢复默认设置",
        ["Patch.Upload"] = "📤 上传",
        ["Upload.Success"] = "上传成功", ["Upload.Failed"] = "上传失败: {0}",
        ["Upload.Uploading"] = "正在上传...",
        ["Result.OpenOutput"] = "打开输出目录",
        ["Result.Exit"] = "退出",
        ["Result.Running"] = "执行中...",
        ["Result.ValidationTitle"] = "验证",
        ["Result.BuildFirst"] = "请先构建项目以找到输出安装包文件。",
    };

    private static Dictionary<string, string> BuildEnUS() => new()
    {
        ["App.Title"] = "GeneralUpdate Tools",
        ["Nav.Patch"] = "Patch", ["Nav.Extension"] = "Extension", ["Nav.OSS"] = "OSS Config",
        ["Nav.Simulate"] = "Simulate", ["Nav.Config"] = "Config", ["Nav.Settings"] = "Settings",
        ["Patch.Title"] = "🩹 Patch Package", ["Patch.CorePaths"] = "Core Paths",
        ["Patch.OldDir"] = "Old Directory", ["Patch.NewDir"] = "New Directory",
        ["Patch.Select"] = "Select", ["Patch.OldPlaceholder"] = "Select old version directory...",
        ["Patch.NewPlaceholder"] = "Select new version directory...",
        ["Patch.PackageInfo"] = "Package Info", ["Patch.PackageName"] = "Package Name",
        ["Patch.Version"] = "Version", ["Patch.OutputDir"] = "Output Directory",
        ["Patch.OutputPlaceholder"] = "Desktop (default)", ["Patch.Build"] = "Build",
        ["Patch.Ready"] = "Ready", ["Patch.ValidateDirs"] = "Please select both old and new directories",
        ["Patch.Building"] = "Generating diff patch...",
        ["Patch.Comparing"] = "Comparing directories + generating patches...",
        ["Patch.PatchDone"] = "Patch generation complete", ["Patch.Packing"] = "Packing: {0}",
        ["Patch.Success"] = "Success: {0} ({1:F1} KB)", ["Patch.Failed"] = "Failed: {0}",
        ["Patch.Error"] = "Error: {0}",
        ["Patch.InvalidVersion"] = "Version '{0}' does not follow semver. Example: 1.0.0",
        ["Patch.OldSelected"] = "Old version: {0}", ["Patch.NewSelected"] = "New version: {0}",
        ["Patch.TempDir"] = "Temp directory: {0}",
        ["Patch.EncryptionCheck"] = "Scan for encrypted files before packaging",
        ["Patch.EncryptionCheckTip"] = "Detect encrypted/packed files in the patch package",
        ["Patch.Scanning"] = "Scanning for encrypted files...",
        ["Patch.ScanFound"] = "Found {0} suspicious file(s)",
        ["Patch.ScanClean"] = "No encrypted files detected",
        ["Patch.ScanDialogTitle"] = "⚠️ Encrypted / Suspicious Files Detected",
        ["Patch.ScanDialogHeader"] = "The following files may be encrypted or packed, which could cause client update failures:",
        ["Patch.ScanHighRisk"] = "🔴 High Risk", ["Patch.ScanMediumRisk"] = "🟡 Medium Risk",
        ["Patch.ScanLowRisk"] = "⚪ Low Risk", ["Patch.ScanFilesScanned"] = "Scanned {0} file(s) total",
        ["Patch.ScanBtnSkip"] = "Skip suspicious files and continue",
        ["Patch.ScanBtnInclude"] = "Include all files anyway",
        ["Patch.ScanBtnCancel"] = "Cancel", ["Patch.ScanSkipped"] = "Skipped {0} suspicious file(s)",
        ["Patch.ScanCancelled"] = "Packaging cancelled by user",
        ["Ext.Title"] = "🧩 Extension Package", ["Ext.BasicInfo"] = "Basic Info",
        ["Ext.Name"] = "Name", ["Ext.Version"] = "Version", ["Ext.Description"] = "Description",
        ["Ext.DescPlaceholder"] = "Extension description...", ["Ext.Publisher"] = "Publisher",
        ["Ext.License"] = "License", ["Ext.LicensePlaceholder"] = "MIT",
        ["Ext.Paths"] = "Paths", ["Ext.ExtDir"] = "Extension Directory",
        ["Ext.ExportDir"] = "Export Directory", ["Ext.CustomProps"] = "Custom Properties",
        ["Ext.Key"] = "Key", ["Ext.Value"] = "Value", ["Ext.AddProp"] = "+ Add",
        ["Ext.Generate"] = "Generate Extension",
        ["Ext.ValidateNameVer"] = "Please fill in extension name and version",
        ["Ext.InvalidVersion"] = "Version '{0}' does not follow semver. Example: 1.0.0",
        ["Ext.ValidateDir"] = "Please select a valid extension directory",
        ["Ext.Building"] = "Generating extension package...",
        ["Ext.Success"] = "Success: {0}", ["Ext.Failed"] = "Failed: {0}",
        ["OSS.Title"] = "☁️ OSS Config Generator", ["OSS.NewEntry"] = "New Entry",
        ["OSS.PacketName"] = "Package Name", ["OSS.Version"] = "Version", ["OSS.Url"] = "URL",
        ["OSS.SHA256"] = "SHA256", ["OSS.ComputeHash"] = "Compute",
        ["OSS.AddToList"] = "Add to List", ["OSS.ConfigList"] = "Config List",
        ["OSS.Clear"] = "Clear", ["OSS.Export"] = "Export JSON",
        ["OSS.Added"] = "Added", ["OSS.Cleared"] = "Cleared",
        ["OSS.Exported"] = "Exported: {0} entries", ["OSS.HashResult"] = "SHA256: {0}",
        ["OSS.InvalidVersion"] = "Version '{0}' does not follow semver. Example: 1.0.0",
        ["Theme.Light"] = "Light", ["Theme.Dark"] = "Dark", ["Theme.Toggle"] = "Toggle Theme",
        ["Locale.Zh"] = "中", ["Locale.En"] = "EN", ["Locale.Toggle"] = "Switch Language",
        ["Sim.Title"] = "🔄 Simulate Update", ["Sim.TestTarget"] = "Test Target",
        ["Sim.OldAppDir"] = "Old App Directory", ["Sim.PatchFile"] = "Patch Package",
        ["Sim.Select"] = "Select", ["Sim.UpdateConfig"] = "Update Config",
        ["Sim.CurrentVer"] = "Current Version", ["Sim.TargetVer"] = "Target Version",
        ["Sim.Platform"] = "Platform", ["Sim.AppType"] = "App Type",
        ["Sim.AppSecret"] = "App Secret", ["Sim.ProductId"] = "Product ID",
        ["Sim.UpdatePath"] = "Update Path", ["Sim.Output"] = "Output",
        ["Sim.OutputDir"] = "Simulate Directory", ["Sim.Start"] = "Start Simulation",
        ["Sim.SelectAppDir"] = "Select old version directory",
        ["Sim.SelectPatch"] = "Select patch package",
        ["Sim.SelectOutput"] = "Select simulation output directory",
        ["Sim.ValidateDirs"] = "Please fill in all required fields",
        ["Sim.InvalidVersion"] = "Version '{0}' does not follow semver. Example: 1.0.0",
        ["Sim.DotnetCheck"] = ".NET 10.0 SDK is required. Please install it first.",
        ["Sim.AutoRun"] = "Auto-start server and run client",
        ["Sim.ManualMode"] = "Server/client generated. Run manually:\ndotnet script client.csx",
        ["Sim.Starting"] = "Starting simulation...",
        ["Sim.Completed"] = "Simulation completed ({0:F1}s)",
        ["Sim.Failed"] = "Simulation failed: {0}", ["Sim.Report"] = "Report: {0}",
        ["Config.Title"] = "⚙️ Config Generator",
        ["Config.ClientPath"] = "Client Project (.csproj)",
        ["Config.UpgradePath"] = "Upgrade Project (.csproj)",
        ["Config.Browse"] = "Browse", ["Config.BrowseClient"] = "Select Client project file",
        ["Config.BrowseUpgrade"] = "Select Upgrade project file",
        ["Config.Analyze"] = "Analyze", ["Config.Analyzing"] = "Analyzing...",
        ["Config.Analyzed"] = "Analysis complete", ["Config.Fields"] = "Configuration Fields",
        ["Config.AutoAnalyzed"] = "-- Auto-analyzed (AssemblyName) --",
        ["Config.ManualInput"] = "-- Manual input (version must follow semver) --",
        ["Config.MainAppName"] = "Main App Name", ["Config.ClientVersion"] = "Client Version",
        ["Config.UpdateAppName"] = "Upgrade App Name",
        ["Config.UpgradeClientVersion"] = "Upgrade Version",
        ["Config.AppType"] = "App Type", ["Config.ProductId"] = "Product ID",
        ["Config.UpdatePath"] = "Upgrade Subdirectory", ["Config.Generate"] = "Generate Config",
        ["Config.Generated"] = "Config file generated",
        ["Config.GenerateSample"] = "Generate Sample Structure",
        ["Config.SampleGenerated"] = "Sample generated",
        ["Config.Publishing"] = "Running dotnet publish...",
        ["Config.Preview"] = "JSON Preview",
        ["Config.CodeHint"] = "new GeneralUpdateBootstrap()\n    .SetSource(\"https://...\", \"your-key\", \"your-token\", \"https\")\n    .LaunchAsync();",
        ["Config.NoClientPath"] = "Please select Client project path", ["Config.Failed"] = "Failed",
        ["Config.OpenManifestDir"] = "Open manifest.json folder after generation",
        ["Config.OpenSampleDir"] = "Open sample structure folder after generation",
        ["Settings.UploadSection"] = "Upload Server Configuration",
        ["Settings.UploadDesc"] = "Configure the target server for automatic patch upload",
        ["Settings.ServerUrl"] = "Server URL", ["Settings.UploadEndpoint"] = "Upload Endpoint",
        ["Settings.Timeout"] = "Timeout (seconds)", ["Settings.RetryCount"] = "Retry Count",
        ["Settings.AutoUpload"] = "Auto-upload after patch generation",
        ["Settings.TestConnection"] = "Test Connection", ["Settings.Testing"] = "Testing connection...",
        ["Settings.ConnectionOk"] = "✓ Connection successful",
        ["Settings.ConnectionFailed"] = "✗ Connection failed",
        ["Settings.AuthSection"] = "Authentication", ["Settings.AuthScheme"] = "Auth Scheme",
        ["Settings.Username"] = "Username", ["Settings.Password"] = "Password",
        ["Settings.Token"] = "Token / JWT", ["Settings.LoginUrl"] = "Login URL (optional)",
        ["Settings.ApiKeyHeader"] = "API Key Header Name", ["Settings.ApiKey"] = "API Key",
        ["Settings.SimSection"] = "Simulation", ["Settings.SimPort"] = "Local Server Port",
        ["Settings.SimRequireAuth"] = "Simulation server requires auth",
        ["Settings.FeatureSection"] = "Feature Switches",
        ["Settings.EncryptionScan"] = "Detect encrypted files before packaging",
        ["Settings.AutoSemver"] = "Auto-validate SemVer versions",
        ["Settings.JsonPreview"] = "Show JSON preview",
        ["Settings.Save"] = "Save Settings", ["Settings.Saved"] = "✓ Settings saved",
        ["Settings.Reset"] = "Reset to Defaults", ["Settings.ResetDone"] = "Settings reset to defaults",
        ["Patch.Upload"] = "📤 Upload",
        ["Upload.Success"] = "Upload successful", ["Upload.Failed"] = "Upload failed: {0}",
        ["Upload.Uploading"] = "Uploading...",
        ["Result.OpenOutput"] = "Open Output Directory",
        ["Result.Exit"] = "Exit",
        ["Result.Running"] = "Running...",
        ["Result.ValidationTitle"] = "Validation",
        ["Result.BuildFirst"] = "Please build the project first to locate the output package file.",
    };
}
