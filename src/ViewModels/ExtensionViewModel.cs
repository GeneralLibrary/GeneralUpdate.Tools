using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tools.Configuration;
using GeneralUpdate.Tools.Models;
using GeneralUpdate.Tools.Services;

namespace GeneralUpdate.Tools.ViewModels;

public partial class ExtensionViewModel : ViewModelBase
{
    private readonly PackageService _pkg = new();
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private readonly AppConfig _config;

    public ExtensionConfigModel Config { get; } = new();
    [ObservableProperty] private bool _isBuilding;
    [ObservableProperty] private string _status;
    [ObservableProperty] private string _newPropKey = "";
    [ObservableProperty] private string _newPropValue = "";
    public ObservableCollection<CustomPropModel> CustomProps { get; } = new();

    public ExtensionViewModel(AppConfig config)
    {
        _config = config;

        // Restore path memory
        Config.ExtensionDirectory = config.LastExtensionDir;
        Config.ExportPath = config.LastExtensionOutputDir;

        _status = _loc["Patch.Ready"];
    }

    string GetFolderPickerTitle()
    {
        var title = _loc["Ext.SelectDirectoryTitle"];
        if (!string.IsNullOrWhiteSpace(title) && title != "Ext.SelectDirectoryTitle") return title;
        return string.Equals(System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "zh",
            StringComparison.OrdinalIgnoreCase)
            ? "选择目录"
            : "Select folder";
    }

    async Task<string?> Pick()
    {
        var tl = Avalonia.Controls.TopLevel.GetTopLevel(
            (Avalonia.Application.Current?.ApplicationLifetime as
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow);
        if (tl == null) return null;
        var r = await tl.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
            { Title = GetFolderPickerTitle(), AllowMultiple = false });
        return r.Count > 0 ? r[0].Path.LocalPath : null;
    }

    [RelayCommand]
    async Task SelectExt()
    {
        var p = await Pick();
        if (p != null)
        {
            Config.ExtensionDirectory = p;
            _config.LastExtensionDir = p;
            ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);
        }
    }

    [RelayCommand]
    async Task SelectExport()
    {
        var p = await Pick();
        if (p != null)
        {
            Config.ExportPath = p;
            _config.LastExtensionOutputDir = p;
            ConfigService.SafeFireAndForgetSave(ConfigServiceSingleton.Instance);
        }
    }

    [RelayCommand]
    void AddProp()
    {
        if (!string.IsNullOrWhiteSpace(NewPropKey) && !string.IsNullOrWhiteSpace(NewPropValue))
        {
            CustomProps.Add(new(NewPropKey, NewPropValue));
            NewPropKey = "";
            NewPropValue = "";
        }
    }

    [RelayCommand]
    void RemoveProp(CustomPropModel? item)
    {
        if (item != null) CustomProps.Remove(item);
    }

    [RelayCommand]
    async Task Generate()
    {
        if (string.IsNullOrWhiteSpace(Config.Name) || string.IsNullOrWhiteSpace(Config.Version))
        {
            Status = _loc["Ext.ValidateNameVer"];
            return;
        }

        if (!SemverValidator.IsValid(Config.Version))
        {
            Status = _loc.T("Ext.InvalidVersion", Config.Version);
            return;
        }

        if (string.IsNullOrWhiteSpace(Config.ExtensionDirectory) || !Directory.Exists(Config.ExtensionDirectory))
        {
            Status = _loc["Ext.ValidateDir"];
            return;
        }

        IsBuilding = true;
        Status = _loc["Ext.Building"];
        try
        {
            var dir = string.IsNullOrWhiteSpace(Config.ExportPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : Config.ExportPath;
            var zip = Path.Combine(dir, $"{Sanitize(Config.Name)}_{Config.Version}.zip");
            await _pkg.CompressDirectoryAsync(Config.ExtensionDirectory, zip);
            await _pkg.CreateManifestAsync(zip, new
            {
                name = Config.Name, version = Config.Version, description = Config.Description,
                publisher = Config.Publisher, license = Config.License, dependencies = Config.Dependencies,
                minHostVersion = Config.MinHostVersion, maxHostVersion = Config.MaxHostVersion,
                isPreRelease = Config.IsPreRelease,
                customProperties = CustomProps.ToDictionary(p => p.Key, p => p.Value)
            });
            Config.OutputPath = zip;
            Status = _loc.T("Ext.Success", Path.GetFileName(zip));
        }
        catch (Exception ex)
        {
            Status = _loc.T("Ext.Failed", ex.Message);
        }
        finally
        {
            IsBuilding = false;
        }
    }

    static string Sanitize(string n) => string.Join("_", n.Split(Path.GetInvalidFileNameChars()));
}

public partial class CustomPropModel(string key, string value) : ObservableObject
{
    public string Key { get; set; } = key;
    public string Value { get; set; } = value;
}
