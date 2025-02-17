using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Common.HashAlgorithms;
using GeneralUpdate.Tool.Avalonia.Models;
using Newtonsoft.Json;
using Nlnet.Avalonia.Controls;

namespace GeneralUpdate.Tool.Avalonia.ViewModels;

public class OSSPacketViewModel : ObservableObject
{
    #region Private Members
    
    private OSSConfigModel? _currnetConfig;
    
    private AsyncRelayCommand? _copyCommand;
    private AsyncRelayCommand? _buildCommand;
    private AsyncRelayCommand? _hashCommand;
    private RelayCommand? _appendCommand;
    private RelayCommand? _clearCommand;
    private RelayCommand? _loadedCommand;

    #endregion

    #region Public Properties

    public ObservableCollection<OSSConfigModel> Configs { get; set; } = new();

    public OSSConfigModel CurrnetConfig
    {
        get => _currnetConfig; 
        set => SetProperty(ref _currnetConfig, value);
    }

    public AsyncRelayCommand BuildCommand { get => _buildCommand ??= new AsyncRelayCommand(OSSBuildAction); }

    public RelayCommand AppendCommand { get => _appendCommand ??= new RelayCommand(AppendAction); }

    public AsyncRelayCommand CopyCommand { get => _copyCommand ??= new AsyncRelayCommand(CopyAction); }
    
    public AsyncRelayCommand HashCommand { get => _hashCommand ??= new AsyncRelayCommand(HashAction); }
    
    public RelayCommand ClearCommand { get => _clearCommand ??= new RelayCommand(ClearAction); }
    
    public RelayCommand LoadedCommand
    {
        get { return _loadedCommand ??= new (LoadedAction); }
    }
    
    #endregion

    #region Private Methods
    
    private async Task OSSBuildAction()
    {
        try
        {
            var file = await Storage.Instance.SaveFilePickerAsync();
            if (file != null)
            {
                var json = JsonConvert.SerializeObject(Configs);
                await File.WriteAllTextAsync(file.Path.AbsolutePath, json, System.Text.Encoding.UTF8);
                var caption = string.Empty;
                var message = string.Empty;
                if (File.Exists(file.Path.AbsolutePath))
                {
                    caption = "Success";
                    message = "Build success";
                }
                else
                {
                    caption = "Fail";
                    message = "Build fail";
                }

                await MessageBox.ShowAsync(message, caption, Buttons.OK);
            }
        }
        catch (Exception e)
        {
            await MessageBox.ShowAsync("Build fail", "Fail", Buttons.OK);
        }
    }

    private void AppendAction()
    {
        try
        {
            Configs.Add(new OSSConfigModel
            {
                Date = CurrnetConfig.Date,
                Time = CurrnetConfig.Time,
                Hash = CurrnetConfig.Hash,
                PacketName = CurrnetConfig.PacketName,
                Url = CurrnetConfig.Url,
                Version = CurrnetConfig.Version
            });
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore
            };
            CurrnetConfig.JsonContent = JsonConvert.SerializeObject(Configs, settings);
        }
        catch (Exception e)
        {
            MessageBox.Show("Append fail", "Fail", Buttons.OK);
        }
    }

    private async Task CopyAction()
    {
        try
        {
            await ClipboardUtility.SetText(CurrnetConfig.JsonContent);
            await MessageBox.ShowAsync("Copy success", "Success", Buttons.OK);
        }
        catch (Exception e)
        {
            await MessageBox.ShowAsync("Copy fail", "Fail", Buttons.OK);
        }
    }
    
    private async Task HashAction()
    {
        var files = await Storage.Instance.OpenFileDialog();
        if (files is null || files.Count == 0) return;

        var file = files.First();
        if (file is not null)
        {
            Sha256HashAlgorithm hashAlgorithm = new();
            CurrnetConfig.Hash = hashAlgorithm.ComputeHash(file.Path.LocalPath);
        }
    }
    
    private void ClearAction()
    {
        CurrnetConfig.JsonContent = "{}";
        Configs.Clear();
    }

    private void LoadedAction() => Initialize();

    private void Initialize() 
    {
        DateTime dateTime = DateTime.Now;
        CurrnetConfig = new OSSConfigModel
        {
            JsonContent = "{}", 
            PacketName = "Packet", 
            Hash = String.Empty, 
            Date = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day),
            Time = new TimeSpan(dateTime.Hour, dateTime.Minute, dateTime.Second),
            Version = "1.0.0.0",
            Url = "http://127.0.0.1"
        };
    }
    
    #endregion
}