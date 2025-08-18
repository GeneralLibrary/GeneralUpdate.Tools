using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using GeneralUpdate.Common.HashAlgorithms;
using GeneralUpdate.Tool.Avalonia.Models;

using Nlnet.Avalonia.Controls;

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace GeneralUpdate.Tool.Avalonia.ViewModels;

public partial class OSSPacketViewModel : ObservableObject
{
    #region Private Members

    [ObservableProperty]
    private OSSConfigModel? _currnetConfig;

    private AsyncRelayCommand? _copyCommand;
   
    private AsyncRelayCommand? _hashCommand;
    private RelayCommand? _appendCommand;

    private RelayCommand? _loadedCommand;

    #endregion Private Members

    #region Public Properties

    public ObservableCollection<OSSConfigModel> Configs { get; set; } = new();

  

    public RelayCommand AppendCommand { get => _appendCommand ??= new RelayCommand(AppendAction); }

    public AsyncRelayCommand CopyCommand { get => _copyCommand ??= new AsyncRelayCommand(CopyAction); }

    public AsyncRelayCommand HashCommand { get => _hashCommand ??= new AsyncRelayCommand(HashAction); }

    public RelayCommand LoadedCommand
    {
        get { return _loadedCommand ??= new(LoadedAction); }
    }

    #endregion Public Properties

    #region Private Methods
    [RelayCommand]
    private async Task OSSBuildAction()
    {
        try
        {
            var file = await Storage.Instance.SaveFilePickerAsync();
            if (file != null)
            {
                var json = JsonSerializer.Serialize(Configs);
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
            var jsonSerializerSettings = new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.CjkUnifiedIdeographs),
                WriteIndented = true,
                // TypeInfoResolver = SourceGenerationContext.Default
            };
            CurrnetConfig.JsonContent = JsonSerializer.Serialize(Configs, jsonSerializerSettings);
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

    [RelayCommand]
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

    #endregion Private Methods
}