using System;
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
using GeneralUpdate.Common.HashAlgorithms;
using GeneralUpdate.Differential;
using GeneralUpdate.Tool.Avalonia.Models;
using Nlnet.Avalonia.Controls;

namespace GeneralUpdate.Tool.Avalonia.ViewModels;

public class MainWindowViewModel : ObservableObject
{
    private PacketConfigModel? _configModel;
    private Sha256HashAlgorithm _hashAlgorithms = new Sha256HashAlgorithm();
    
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
            await DifferentialCore.Instance.Clean(ConfigModel.AppDirectory,
                ConfigModel.ReleaseDirectory,
                ConfigModel.PatchDirectory);

            var directoryInfo = new DirectoryInfo(ConfigModel.PatchDirectory);
            var parentDirectory = directoryInfo.Parent!.FullName;
            var operationType = ConfigModel.Format.Value;
            var encoding = ConfigModel.Encoding.Value;
           
            CompressProvider.Compress(operationType, ConfigModel.PatchDirectory, Path.Combine(parentDirectory,ConfigModel.Name+ ConfigModel.Format.Value), false, encoding);
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
            Trace.WriteLine(e.Message);
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
}