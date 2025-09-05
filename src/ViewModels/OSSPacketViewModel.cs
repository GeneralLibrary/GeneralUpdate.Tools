using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using GeneralUpdate.Common.Compress;
using GeneralUpdate.Common.HashAlgorithms;
using GeneralUpdate.Common.Shared.Object;

using Nlnet.Avalonia.Controls;

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace GeneralUpdate.Tool.Avalonia.ViewModels;

public partial class OSSPacketViewModel : ObservableObject
{
    #region Private Members

    [ObservableProperty]
    private OSSConfigVM? _currnetConfig;

    #endregion Private Members

    public ObservableCollection<OSSConfigVM> Configs { get; set; } = new();

    private JsonSerializerOptions jsonSerializerSettings;

    public OSSPacketViewModel()
    {
        jsonSerializerSettings = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.CjkUnifiedIdeographs),
            WriteIndented = true,
            // TypeInfoResolver = SourceGenerationContext.Default
        };
    }

    #region Private Methods

  
    private void GenerteJsonContent()
    {
        try
        {
            Configs.Clear();
            Configs.Add(new OSSConfigVM
            {
                Date = CurrnetConfig.Date,
                Time = CurrnetConfig.Time,
                Hash = CurrnetConfig.Hash,
                PacketName = CurrnetConfig.PacketName,
                Url = CurrnetConfig.Url,
                Version = CurrnetConfig.Version
            });

            CurrnetConfig.JsonContent = JsonSerializer.Serialize(Configs, jsonSerializerSettings);
        }
        catch (Exception e)
        {
            MessageBox.Show("Append fail", "Fail", Buttons.OK);
        }
    }

    [RelayCommand]
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

    [RelayCommand]
    private async Task OSSBuildAction()
    {
        try
        {
            var files = await Storage.Instance.SelectFolderDialog();
            if (files is null || files.Count == 0) return;

            var file = files.First();
            if (file is not null)
            {
                var folderName = file.Name;
                var parentFolder = Directory.GetParent(file.Path.AbsolutePath)!.Parent;
                var newZipPath = Path.Combine(parentFolder!.FullName, CurrnetConfig.PacketName + ".zip");

                CompressProvider.Compress(Format.ZIP, file.Path.AbsolutePath, newZipPath, false, Encoding.Default);

                Sha256HashAlgorithm hashAlgorithm = new();
                CurrnetConfig.Hash = hashAlgorithm.ComputeHash(newZipPath);
                CurrnetConfig.Url += "//" + CurrnetConfig.PacketName + ".zip";
                GenerteJsonContent();
                var versionFilePath = Path.Combine(parentFolder.FullName, "version.json");

                var json = JsonSerializer.Serialize(Configs, jsonSerializerSettings);
                await File.WriteAllTextAsync(versionFilePath, json, System.Text.Encoding.UTF8);
                var caption = string.Empty;
                var message = string.Empty;
                if (File.Exists(versionFilePath))
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

    /// <summary>修改为选择文件夹，压缩并计算哈希值</summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task Upload()
    {
    }

    [RelayCommand]
    private void ClearAction()
    {
        CurrnetConfig.JsonContent = "{}";
        Configs.Clear();
    }

    [RelayCommand]
    private void Initialize()
    {
        DateTime dateTime = DateTime.Now;
        CurrnetConfig = new OSSConfigVM
        {
            JsonContent = "{}",
            PacketName = "NewPacket",
            Hash = String.Empty,
            Date = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day),
            Time = new TimeSpan(dateTime.Hour, dateTime.Minute, dateTime.Second),
            Version = "1.0.0.0",
            Url = "http://10.119.30.34:5000"
        };
    }

    #endregion Private Methods
}