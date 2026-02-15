using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Common.Compress;
using GeneralUpdate.Common.Shared.Object;
using GeneralUpdate.Differential;
using GeneralUpdate.Tool.Avalonia.Common;
using GeneralUpdate.Tool.Avalonia.Models;
using Newtonsoft.Json;
using Nlnet.Avalonia.Controls;

namespace GeneralUpdate.Tool.Avalonia.ViewModels;

public class PacketViewModel : ObservableObject
{
    private PacketConfigModel? _configModel;
    
    private RelayCommand? _clearCommand;
    private RelayCommand? _loadedCommand;
    private AsyncRelayCommand? _buildCommand;
    private AsyncRelayCommand<string>? _selectFolderCommand;
    
    public ObservableCollection<AppTypeModel> AppTypes { get; set; } = new();

    public ObservableCollection<FormatModel> Formats { get; set; } =
    [
        new FormatModel { DisplayName = ".zip", Type = 1, Value = ".zip" }
    ];

    public ObservableCollection<EncodingModel> Encodings { get; set; } =
    [
        new EncodingModel { DisplayName = "Default", Value = Encoding.Default, Type = 1 },
        new EncodingModel { DisplayName = "UTF-8", Value = Encoding.UTF8, Type = 2 },
        new EncodingModel { DisplayName = "UTF-7", Value = Encoding.UTF7, Type = 3 },
        new EncodingModel { DisplayName = "Unicode", Value = Encoding.GetEncoding("Unicode"), Type = 4 },
        new EncodingModel { DisplayName = "UTF-32", Value = Encoding.UTF32, Type = 5 },
        new EncodingModel { DisplayName = "BigEndianUnicode", Value = Encoding.BigEndianUnicode, Type = 6 },
        new EncodingModel { DisplayName = "Latin1", Value = Encoding.GetEncoding("Latin1"), Type = 7 },
        new EncodingModel { DisplayName = "ASCII", Value = Encoding.ASCII, Type = 8 }
    ];

    public ObservableCollection<PlatformModel> Platforms { get; set; } = 
    [
        new PlatformModel { DisplayName = "Windows", Value = 1 },
        new PlatformModel { DisplayName = "Linux", Value = 2 }
    ];
    
    public PacketConfigModel ConfigModel
    { 
        get => _configModel ??= new PacketConfigModel() ;
        set
        {
            _configModel = value;
            SetProperty(ref _configModel, value);
        }
    }
    
    public RelayCommand LoadedCommand
    {
        get { return _loadedCommand ??= new (LoadedAction); }
    }
    
    public AsyncRelayCommand<string> SelectFolderCommand
    {
        get => _selectFolderCommand ??= new (SelectFolderAction);
    }

    public AsyncRelayCommand BuildCommand
    {
        get => _buildCommand ??= new (BuildPacketAction);
    }
    
    public RelayCommand ClearCommand
    {
        get => _clearCommand ??= new (ClearAction);
    }
    
    private void LoadedAction()
    {
        AppTypes.Clear();
        AppTypes.Add(new AppTypeModel{ DisplayName = "ClientApp", Value = 1 });
        AppTypes.Add(new AppTypeModel{ DisplayName = "UpgradeApp", Value = 2 });
        ResetAction();
    }
    
    private void ResetAction() 
    {
        ConfigModel.Name = GenerateFileName("1.0.0.0");
        ConfigModel.ReleaseDirectory = GetPlatformSpecificPath();
        ConfigModel.AppDirectory = GetPlatformSpecificPath();
        ConfigModel.PatchDirectory = GetPlatformSpecificPath();
        ConfigModel.DriverDirectory = string.Empty;
        ConfigModel.ReportUrl = string.Empty;
        ConfigModel.UpdateUrl = string.Empty;
        ConfigModel.AppName = "Update";
        ConfigModel.MainAppName = string.Empty;
        ConfigModel.ClientVersion = string.Empty;
        ConfigModel.Encoding = Encodings.First();
        ConfigModel.Format = Formats.First();
    }
    
    /// <summary>
    /// Choose a path
    /// </summary>
    /// <param name="value"></param>
    private async Task SelectFolderAction(string value)
    {
        try
        {
            var folders = await Storage.Instance.SelectFolderDialog();
            if (!folders.Any()) return;

            var folder = folders.First();
            switch (value)
            {
                case "App":
                    ConfigModel.AppDirectory = folder.Path.LocalPath;
                    break;

                case "Release":
                    ConfigModel.ReleaseDirectory = folder!.Path.LocalPath;
                    break;

                case "Patch":
                    ConfigModel.PatchDirectory = folder!.Path.LocalPath;
                    break;

                case "Driver":
                    ConfigModel.DriverDirectory = folder!.Path.LocalPath;
                    break;
            }
        }
        catch (Exception e)
        {
            Trace.WriteLine(e.Message);
        }
    }

    /// <summary>
    ///  Build patch package
    /// </summary>
    private async Task BuildPacketAction()
    {
        try
        {
            // Validate required fields
            if (!await ValidateRequiredFields())
                return;

            // Read configuration from .csproj
            ReadProjectConfiguration();

            await DifferentialCore.Instance.Clean(ConfigModel.AppDirectory,
                ConfigModel.ReleaseDirectory,
                ConfigModel.PatchDirectory);

            // Copy driver files to drivers folder if driver directory is specified
            if (!string.IsNullOrWhiteSpace(ConfigModel.DriverDirectory) && 
                Directory.Exists(ConfigModel.DriverDirectory))
            {
                try
                {
                    var driversFolder = Path.Combine(ConfigModel.PatchDirectory, "drivers");
                    Directory.CreateDirectory(driversFolder);
                    
                    CopyDriverFiles(ConfigModel.DriverDirectory, driversFolder);
                }
                catch (Exception ex)
                {
                    await MessageBox.ShowAsync("Failed to copy driver files. Please check the driver directory permissions and available disk space.", "Warning", Buttons.OK);
                }
            }

            // Create and save ConfigInfo JSON file
            await CreateConfigInfoFile();

            var directoryInfo = new DirectoryInfo(ConfigModel.PatchDirectory);
            var parentDirectory = directoryInfo.Parent!.FullName;
            var operationType = ConfigModel.Format.Value;
            var encoding = ConfigModel.Encoding.Value;
            
            CompressProvider.Compress(operationType
                , ConfigModel.PatchDirectory
                , Path.Combine(parentDirectory,ConfigModel.Name + ConfigModel.Format.Value)
                , false, encoding);
            
            if (Directory.Exists(ConfigModel.PatchDirectory))
                DeleteDirectoryRecursively(ConfigModel.PatchDirectory);
            
            var packetInfo = new FileInfo(Path.Combine(parentDirectory, $"{ConfigModel.Name}{ConfigModel.Format.Value}"));
            if (packetInfo.Exists)
            {
                ConfigModel.Path = packetInfo.FullName;
                await MessageBox.ShowAsync("Build success", "Success", Buttons.OK);
            }
            else
            {
                await MessageBox.ShowAsync("Build fail", "Fail", Buttons.OK);
            }
        }
        catch (Exception e)
        {
            await MessageBox.ShowAsync(e.Message, "Fail", Buttons.OK);
        }
    }
    
    private void ClearAction() => ResetAction();
    
    private string GetPlatformSpecificPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows-specific path, defaulting to C: drive
            return @"C:\";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux-specific path, defaulting to /home/user
            return "/home";
        }
        
        throw new PlatformNotSupportedException("Unsupported OS");
    }
    
    private string GenerateFileName(string version)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        return $"packet_{timestamp}_{version}";
    }
    
    private void DeleteDirectoryRecursively(string targetDir)
    {
        foreach (var file in Directory.GetFiles(targetDir))
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (var dir in Directory.GetDirectories(targetDir))
        {
            DeleteDirectoryRecursively(dir);
        }
        Directory.Delete(targetDir, false);
    }

    private void CopyDriverFiles(string sourceDir, string targetDir)
    {
        // Copy all files from source to target
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(targetDir, fileName);
            File.Copy(file, destFile, true);
        }

        // Copy all subdirectories and their files recursively
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            var destDir = Path.Combine(targetDir, dirName);
            Directory.CreateDirectory(destDir);
            CopyDriverFiles(dir, destDir);
        }
    }

    /// <summary>
    /// Validate required fields
    /// </summary>
    private async Task<bool> ValidateRequiredFields()
    {
        var errors = new System.Collections.Generic.List<string>();

        if (string.IsNullOrWhiteSpace(ConfigModel.UpdateUrl))
            errors.Add("UpdateUrl");
        
        if (string.IsNullOrWhiteSpace(ConfigModel.ReportUrl))
            errors.Add("ReportUrl");
        
        if (string.IsNullOrWhiteSpace(ConfigModel.AppDirectory))
            errors.Add("AppDirectory");
        
        if (string.IsNullOrWhiteSpace(ConfigModel.ReleaseDirectory))
            errors.Add("ReleaseDirectory");
        
        if (string.IsNullOrWhiteSpace(ConfigModel.PatchDirectory))
            errors.Add("PatchDirectory");

        if (errors.Any())
        {
            var message = $"The following required fields must be filled:\n{string.Join(", ", errors)}";
            await MessageBox.ShowAsync(message, "Validation Error", Buttons.OK);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Read project configuration from .csproj file
    /// </summary>
    private void ReadProjectConfiguration()
    {
        try
        {
            // Read MainAppName
            ConfigModel.MainAppName = CsprojReader.ReadMainAppName(ConfigModel.ReleaseDirectory);
            
            // Read ClientVersion
            ConfigModel.ClientVersion = CsprojReader.ReadClientVersion(ConfigModel.ReleaseDirectory);

            // Read ReleaseDirectory
            var directory = SearchExeFileAndGetDirectory(ConfigModel.ReleaseDirectory, ConfigModel.MainAppName);
            var outputPath = CsprojReader.ReadOutputPath(ConfigModel.ReleaseDirectory);

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                ConfigModel.ReleaseDirectory = outputPath;
            }
            else if (!string.IsNullOrWhiteSpace(directory))
            {
                ConfigModel.ReleaseDirectory = SearchExeFileAndGetDirectory(ConfigModel.ReleaseDirectory, ConfigModel.MainAppName);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error reading project configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Create Configinfo JSON file in patch directory
    /// </summary>
    private async Task<string> CreateConfigInfoFile()
    {
        try
        {
            var configInfo = new Configinfo
            {
                ReportUrl = ConfigModel.ReportUrl,
                UpdateUrl = ConfigModel.UpdateUrl,
                AppName = ConfigModel.AppName,
                MainAppName = ConfigModel.MainAppName,
                ClientVersion = ConfigModel.ClientVersion
            };

            var json = JsonConvert.SerializeObject(configInfo, Formatting.Indented);
            var configFilePath = Path.Combine(ConfigModel.PatchDirectory, "config.json");
            
            await File.WriteAllTextAsync(configFilePath, json, Encoding.UTF8);
            
            return configFilePath;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error creating config info file: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Searches for the specified exe file in release/debug directories under bin directories 
    /// in the specified root directory, and returns the directories of the found exe files as a concatenated string
    /// </summary>
    /// <param name="rootDirectory">Root directory path</param>
    /// <param name="exeFileName">Name of the exe file to search (including extension)</param>
    /// <returns>Paths of directories containing matching exe files, separated by semicolons; 
    /// returns empty string if no files are found</returns>
    /// <exception cref="ArgumentNullException">Thrown when parameter is null or whitespace</exception>
    /// <exception cref="ArgumentException">Thrown when parameter format is invalid</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when root directory does not exist</exception>
    private static string SearchExeFileAndGetDirectory(string rootDirectory, string exeFileName)
    {
        // Temporarily store directories of found exe files
        List<string> exeDirectories = new List<string>();

        // ===================== Step 1: Comprehensive parameter validation =====================
        // 1. Validate if rootDirectory is null, empty or whitespace
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentNullException(nameof(rootDirectory), "Root directory path cannot be null, empty or whitespace");
        }

        // 2. Validate if rootDirectory path format is legal (avoid invalid paths like "::/\\abc")
        try
        {
            // Attempt to resolve the path, exception will be thrown if format is illegal
            string fullPath = Path.GetFullPath(rootDirectory);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid root directory path format: {ex.Message}", nameof(rootDirectory), ex);
        }

        // 3. Validate if exeFileName is null, empty or whitespace
        if (string.IsNullOrWhiteSpace(exeFileName))
        {
            throw new ArgumentNullException(nameof(exeFileName), "Exe file name to search cannot be null, empty or whitespace");
        }

        // 6. Basic validation: Check if root directory exists (kept after format validation)
        if (!Directory.Exists(rootDirectory))
        {
            throw new DirectoryNotFoundException($"Specified root directory does not exist: {rootDirectory}");
        }

        // ===================== Step 2: Original core search logic =====================
        try
        {
            // 1. Recursively find all bin directories (case-insensitive)
            var binDirectories = Directory.EnumerateDirectories(
                rootDirectory,
                "bin",
                SearchOption.AllDirectories)
                .Where(dir => string.Equals(Path.GetFileName(dir), "bin", StringComparison.OrdinalIgnoreCase));

            foreach (string binDir in binDirectories)
            {
                string targetDir = null;

                // 2. Prioritize checking release directory
                string releaseDir = Path.Combine(binDir, "release");
                if (Directory.Exists(releaseDir))
                {
                    targetDir = releaseDir;
                }
                else
                {
                    // Fallback to check debug directory
                    string debugDir = Path.Combine(binDir, "debug");
                    if (Directory.Exists(debugDir))
                    {
                        targetDir = debugDir;
                    }
                }

                // 3. If release/debug directory is found, search for exe files
                if (!string.IsNullOrEmpty(targetDir))
                {
                    var exeFiles = Directory.EnumerateFiles(
                        targetDir,
                        "*.exe",
                        SearchOption.AllDirectories);

                    foreach (string filePath in exeFiles)
                    {
                        string fileName = Path.GetFileName(filePath);
                        if (fileName.Contains(exeFileName))
                        {
                            string exeDir = Path.GetDirectoryName(filePath);
                            if (!exeDirectories.Contains(exeDir))
                            {
                                exeDirectories.Add(exeDir);
                            }
                        }
                    }
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Insufficient permissions to access directory: {ex.Message}");
        }
        catch (PathTooLongException ex)
        {
            Console.WriteLine($"File path is too long: {ex.Message}");
        }

        // Concatenate directories into a string and return
        return string.Join(";", exeDirectories);
    }
}