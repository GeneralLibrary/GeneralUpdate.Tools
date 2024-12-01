using CommunityToolkit.Mvvm.ComponentModel;

namespace GeneralUpdate.Tool.Avalonia.Models;

public class PacketConfigModel : ObservableObject
{
    private string _appDirectory, _releaseDirectory, _patchDirectory, _name, _path;
    private PlatformModel _platform;
    private FormatModel _format;
    private EncodingModel _encoding;
 
    /// <summary>
    /// 压缩包格式
    /// </summary>
    public FormatModel Format
    {
        get => _format;
        set
        {
            _format = value;
            OnPropertyChanged(nameof(Format));
        }
    }

    /// <summary>
    /// 压缩包编码
    /// </summary>
    public EncodingModel Encoding
    { 
        get => _encoding; 
        set
        {
            _encoding = value;
            OnPropertyChanged(nameof(Encoding));
        }
    }

    /// <summary>
    /// 最近一次发布的应用程序目录
    /// </summary>
    public string AppDirectory 
    { 
        get => _appDirectory;
        set
        {
            _appDirectory = value;
            OnPropertyChanged(nameof(AppDirectory));
        }
    }

    /// <summary>
    /// 发布程序目录
    /// </summary>
    public string ReleaseDirectory 
    { 
        get => _releaseDirectory;
        set
        {
            _releaseDirectory = value;
            OnPropertyChanged(nameof(ReleaseDirectory));
        } 
    }

    /// <summary>
    /// 补丁包生成目录
    /// </summary>
    public string PatchDirectory
    { 
        get => _patchDirectory;
        set
        {
            _patchDirectory = value;
            OnPropertyChanged(nameof(PatchDirectory));
        }
    }

    /// <summary>
    /// 补丁包名称
    /// </summary>
    public string Name 
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    public string Path
    {
        get => _path;
        set
        {
            _path = value;
            OnPropertyChanged(nameof(Path));
        }
    }
}