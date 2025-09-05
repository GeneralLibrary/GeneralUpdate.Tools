using CommunityToolkit.Mvvm.ComponentModel;

using GeneralUpdate.Tool.Avalonia.Models;

using System.Text.Json.Serialization;

namespace GeneralUpdate.Tool.Avalonia.ViewModels;

public partial class PacketConfigVM : ObservableObject
{
    [ObservableProperty]
    private string _path;

    /// <summary>补丁包名称</summary>
    [ObservableProperty]
    private string _name;

    /// <summary>补丁包生成目录</summary>
    [ObservableProperty]
    private string _patchDirectory;

    /// <summary>发布程序目录</summary>
    [ObservableProperty]
    private string _releaseDirectory;

    /// <summary>最近一次发布的应用程序目录</summary>
    [ObservableProperty]
    private string _appDirectory;

    /// <summary>暂时未使用</summary>
    [ObservableProperty]
    private PlatformModel _platform;

    /// <summary>压缩包格式</summary>
    [ObservableProperty]
    private FormatModel _format;

    /// <summary>压缩包编码</summary>
    [JsonIgnore]
    [ObservableProperty]
    private EncodingModel _encoding;

    public PacketConfigM ToModel()
    {
        return new PacketConfigM
        {
            Path = Path,
            Name = Name,
            PatchDirectory = PatchDirectory,
            ReleaseDirectory = ReleaseDirectory,
            AppDirectory = AppDirectory,
        };
    }
}

public class PacketConfigM
{
    public string Path { get; set; }

    public string Name { get; set; }
    public string PatchDirectory { get; set; }
    public string ReleaseDirectory { get; set; }
    public string AppDirectory { get; set; }
    public int PlatformIndex { get; set; }

    public int FormatIndex { get; set; }
    public int EncodingIndex { get; set; }

    public PacketConfigVM ToVM()
    {
        return new PacketConfigVM
        {
            Path = Path,
            Name = Name,
            PatchDirectory = PatchDirectory,
            ReleaseDirectory = ReleaseDirectory,
            AppDirectory = AppDirectory,
        };
    }
}