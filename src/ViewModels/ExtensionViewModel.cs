using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tool.Avalonia.Common;
using GeneralUpdate.Tool.Avalonia.Models;
using Newtonsoft.Json;
using Nlnet.Avalonia.Controls;

namespace GeneralUpdate.Tool.Avalonia.ViewModels;

public class ExtensionViewModel : ObservableObject
{
    #region Private Members

    
    private ExtensionConfigModel? _configModel;
    private AsyncRelayCommand? _generateCommand;
    private AsyncRelayCommand<string?>? _selectFolderCommand;
    private RelayCommand? _loadedCommand;
    private RelayCommand? _clearCommand;
    private AsyncRelayCommand? _selectDependenciesCommand;
    private ExtensionDependencySelectionModel? _selectedDependency;
    private AsyncRelayCommand<CustomPropertyModel>? _removeCustomPropertyCommand;
    private AsyncRelayCommand? _addCustomPropertyCommand;
    private string? _newCustomPropertyKey;
    private string? _newCustomPropertyValue;

    #endregion

    #region Public Properties

    public RelayCommand LoadedCommand
    {
        get { return _loadedCommand ??= new RelayCommand(LoadedAction); }
    }

    public AsyncRelayCommand<string?> SelectFolderCommand
    {
        get => _selectFolderCommand ??= new AsyncRelayCommand<string?>(SelectFolderAction);
    }

    public AsyncRelayCommand GenerateCommand
    {
        get => _generateCommand ??= new AsyncRelayCommand(GeneratePackageAction);
    }
    
    public RelayCommand ClearCommand
    {
        get => _clearCommand ??= new RelayCommand(ClearAction);
    }

    public ObservableCollection<PlatformModel> Platforms { get; set; } =
    [
        new PlatformModel { DisplayName = "Windows", Value = 1 },
        new PlatformModel { DisplayName = "Linux", Value = 2 },
        new PlatformModel { DisplayName = "MacOS", Value = 3 }
    ];

    public ExtensionConfigModel ConfigModel
    {
        get => _configModel ??= new ExtensionConfigModel();
        set
        {
            _configModel = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(ConfigModel)));
        }
    }

    public ExtensionDependencySelectionModel? SelectedDependency
    {
        get => _selectedDependency;
        set => SetProperty(ref _selectedDependency, value);
    }

    public AsyncRelayCommand<CustomPropertyModel> RemoveCustomPropertyCommand
    {
        get => _removeCustomPropertyCommand ??= new AsyncRelayCommand<CustomPropertyModel>(RemoveCustomPropertyAction);
    }

    public AsyncRelayCommand AddCustomPropertyCommand
    {
        get => _addCustomPropertyCommand ??= new AsyncRelayCommand(AddCustomPropertyAction, CanAddCustomProperty);
    }

    public string? NewCustomPropertyKey
    {
        get => _newCustomPropertyKey;
        set
        {
            if (SetProperty(ref _newCustomPropertyKey, value))
            {
                AddCustomPropertyCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? NewCustomPropertyValue
    {
        get => _newCustomPropertyValue;
        set
        {
            if (SetProperty(ref _newCustomPropertyValue, value))
            {
                AddCustomPropertyCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<CustomPropertyModel> CustomPropertiesCollection { get; set; } = new();

    #endregion

    #region Private Methods

    /// <summary>
    /// Maps legacy platform model value to TargetPlatform enum
    /// </summary>
    /// <param name="platformValue">Platform model value (0=All, 1=Windows, 2=Linux, 3=MacOS)</param>
    /// <returns>Corresponding TargetPlatform enum value</returns>
    private static TargetPlatform MapPlatformValue(int platformValue)
    {
        return platformValue switch
        {
            1 => TargetPlatform.Windows,
            2 => TargetPlatform.Linux,
            3 => TargetPlatform.MacOS,
            _ => TargetPlatform.All
        };
    }

    private void LoadedAction()
    {
        ResetAction();
    }

    private void ResetAction()
    {
        ConfigModel.Name = string.Empty;
        ConfigModel.Version = "1.0.0.0";
        ConfigModel.Description = string.Empty;
        ConfigModel.ExtensionDirectory = string.Empty;
        ConfigModel.Path = string.Empty;
        ConfigModel.Dependencies = string.Empty;
        ConfigModel.IsUploadToServer = false;
        ConfigModel.Platform = Platforms.First();
        ConfigModel.DisplayName = string.Empty;
        ConfigModel.Publisher = string.Empty;
        ConfigModel.License = string.Empty;
        ConfigModel.CategoriesText = string.Empty;
        ConfigModel.MinHostVersion = string.Empty;
        ConfigModel.MaxHostVersion = string.Empty;
        ConfigModel.ReleaseDate = DateTime.Now;
        ConfigModel.IsPreRelease = false;
        ConfigModel.Format = ".zip";
        ConfigModel.Hash = string.Empty;
        SelectedDependency = null;
        ConfigModel.CustomProperties.Clear();
        ConfigModel.ShowCustomProperties = false;
        CustomPropertiesCollection.Clear();
        NewCustomPropertyKey = string.Empty;
        NewCustomPropertyValue = string.Empty;
    }

    private async Task SelectFolderAction(string? value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                await MessageBox.ShowAsync("Invalid folder selection parameter", "Error", Buttons.OK);
                return;
            }

            var folders = await Storage.Instance.SelectFolderDialog();
            if (!folders.Any()) return;

            var folder = folders.First();
            if (folder?.Path?.LocalPath == null)
            {
                await MessageBox.ShowAsync("Selected folder path is invalid", "Error", Buttons.OK);
                return;
            }

            switch (value)
            {
                case "ExtensionDirectory":
                    ConfigModel.ExtensionDirectory = folder.Path.LocalPath;
                    break;
                case "ExportPath":
                    ConfigModel.Path = folder.Path.LocalPath;
                    break;
                default:
                    await MessageBox.ShowAsync($"Unknown folder selection type: {value}", "Error", Buttons.OK);
                    break;
            }
        }
        catch (Exception ex)
        {
            await MessageBox.ShowAsync($"Failed to select folder: {ex.Message}", "Error", Buttons.OK);
        }
    }

    /// <summary>
    /// Generate update package (compress extension directory and optionally upload)
    /// </summary>
    private async Task GeneratePackageAction()
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(ConfigModel.Name))
            {
                await MessageBox.ShowAsync("Extension name is required", "Validation Error", Buttons.OK);
                return;
            }

            if (string.IsNullOrWhiteSpace(ConfigModel.Version))
            {
                await MessageBox.ShowAsync("Extension version is required", "Validation Error", Buttons.OK);
                return;
            }

            if (string.IsNullOrWhiteSpace(ConfigModel.ExtensionDirectory))
            {
                await MessageBox.ShowAsync("Extension directory is required", "Validation Error", Buttons.OK);
                return;
            }

            if (!Directory.Exists(ConfigModel.ExtensionDirectory))
            {
                await MessageBox.ShowAsync($"Extension directory does not exist: {ConfigModel.ExtensionDirectory}", "Validation Error", Buttons.OK);
                return;
            }

            if (string.IsNullOrWhiteSpace(ConfigModel.Path))
            {
                await MessageBox.ShowAsync("Export path is required", "Validation Error", Buttons.OK);
                return;
            }

            // ConfigModel.Path is the export directory (not the final zip file path)
            var exportDirectory = ConfigModel.Path;
            
            // Ensure export directory exists
            if (!Directory.Exists(exportDirectory))
            {
                Directory.CreateDirectory(exportDirectory);
            }

            // Sanitize extension name and version to create a valid filename
            var sanitizedName = ZipUtility.SanitizeFileName(ConfigModel.Name);
            var sanitizedVersion = ZipUtility.SanitizeFileName(ConfigModel.Version);

            // Create zip file name: ExtensionName_Version.zip
            var zipFileName = $"{sanitizedName}_{sanitizedVersion}.zip";
            var zipFilePath = Path.Combine(exportDirectory, zipFileName);

            // Compress the extension directory into a zip file
            await ZipUtility.CompressDirectoryAsync(
                ConfigModel.ExtensionDirectory, 
                zipFilePath, 
                System.IO.Compression.CompressionLevel.Optimal,
                includeBaseDirectory: false);

            // Update the Path field to point to the compressed zip file for upload
            ConfigModel.Path = zipFilePath;

            // Create manifest.json with all ExtensionDTO fields
            var platformValue = ConfigModel.Platform?.Value ?? 0;
            var targetPlatform = MapPlatformValue(platformValue);
            ConfigModel.Platform = new PlatformModel{ DisplayName = targetPlatform.ToString(), Value = platformValue };
            
            // Get file info for the zip
            var fileInfo = new FileInfo(zipFilePath);
            ConfigModel.FileSize = fileInfo.Length;
            
            // Serialize manifest to JSON
            var manifestJson = JsonConvert.SerializeObject(ConfigModel);
            if (!string.IsNullOrEmpty(manifestJson))
            {
                // Add manifest.json to the zip file
                await ZipUtility.AddFileToZipAsync(zipFilePath, "manifest.json", manifestJson);
            }

            await MessageBox.ShowAsync($"Extension package created successfully at:\n{zipFilePath}", "Success", Buttons.OK);
        }
        catch (UnauthorizedAccessException ex)
        {
            await MessageBox.ShowAsync($"Access denied: {ex.Message}\nPlease check file permissions.", "Error", Buttons.OK);
        }
        catch (IOException ex)
        {
            await MessageBox.ShowAsync($"I/O error: {ex.Message}", "Error", Buttons.OK);
        }
        catch (Exception ex)
        {
            await MessageBox.ShowAsync($"Failed to generate package: {ex.Message}", "Error", Buttons.OK);
        }
    }
    
    private void ClearAction() => ResetAction();

    private bool CanAddCustomProperty()
    {
        return !string.IsNullOrWhiteSpace(NewCustomPropertyKey) &&
               !string.IsNullOrWhiteSpace(NewCustomPropertyValue);
    }

    private async Task AddCustomPropertyAction()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewCustomPropertyKey))
            {
                await MessageBox.ShowAsync("Property key cannot be empty", "Validation Error", Buttons.OK);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewCustomPropertyValue))
            {
                await MessageBox.ShowAsync("Property value cannot be empty", "Validation Error", Buttons.OK);
                return;
            }

            // Check if key already exists
            if (ConfigModel.CustomProperties.ContainsKey(NewCustomPropertyKey))
            {
                await MessageBox.ShowAsync($"Property key '{NewCustomPropertyKey}' already exists", "Validation Error", Buttons.OK);
                return;
            }

            // Add to dictionary
            ConfigModel.CustomProperties[NewCustomPropertyKey] = NewCustomPropertyValue;

            // Add to observable collection for UI binding
            CustomPropertiesCollection.Add(new CustomPropertyModel
            {
                Key = NewCustomPropertyKey,
                Value = NewCustomPropertyValue
            });

            // Clear input fields
            NewCustomPropertyKey = string.Empty;
            NewCustomPropertyValue = string.Empty;
        }
        catch (Exception ex)
        {
            await MessageBox.ShowAsync($"Failed to add custom property: {ex.Message}", "Error", Buttons.OK);
        }
    }

    private async Task RemoveCustomPropertyAction(CustomPropertyModel? property)
    {
        try
        {
            if (property == null)
            {
                await MessageBox.ShowAsync("No property selected to remove", "Validation Error", Buttons.OK);
                return;
            }

            // Remove from dictionary
            if (ConfigModel.CustomProperties.ContainsKey(property.Key))
            {
                ConfigModel.CustomProperties.Remove(property.Key);
            }

            // Remove from observable collection
            CustomPropertiesCollection.Remove(property);
        }
        catch (Exception ex)
        {
            await MessageBox.ShowAsync($"Failed to remove custom property: {ex.Message}", "Error", Buttons.OK);
        }
    }

    #endregion
}