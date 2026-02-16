using CommunityToolkit.Mvvm.ComponentModel;

namespace GeneralUpdate.Tool.Avalonia.Models;

public class PacketConfigModel : ObservableObject
{
    private string _serverAddress;
    private string _appDirectory, _releaseDirectory, _patchDirectory, _name, _path, _driverDirectory;
    private string _reportUrl, _updateUrl, _appName, _mainAppName, _clientVersion;
    private PlatformModel _platform;
    private FormatModel _format;
    private EncodingModel _encoding;
    
    public string ServerAddress
    {
        get => _serverAddress;
        set
        {
            _serverAddress = value;
            OnPropertyChanged(nameof(ServerAddress));
            
            ReportUrl = $"{_serverAddress}/report";
            UpdateUrl = $"{_serverAddress}/update";
        }
    }
 
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

    /// <summary>
    /// 驱动程序目录
    /// </summary>
    public string DriverDirectory
    {
        get => _driverDirectory;
        set
        {
            _driverDirectory = value;
            OnPropertyChanged(nameof(DriverDirectory));
        }
    }

    /// <summary>
    /// 报告地址
    /// </summary>
    public string ReportUrl
    {
        get => _reportUrl;
        set
        {
            _reportUrl = value;
            OnPropertyChanged(nameof(ReportUrl));
        }
    }

    /// <summary>
    /// 更新地址
    /// </summary>
    public string UpdateUrl
    {
        get => _updateUrl;
        set
        {
            _updateUrl = value;
            OnPropertyChanged(nameof(UpdateUrl));
        }
    }

    /// <summary>
    /// 应用程序名称
    /// </summary>
    public string AppName
    {
        get => _appName;
        set
        {
            _appName = value;
            OnPropertyChanged(nameof(AppName));
        }
    }

    /// <summary>
    /// 主应用程序名称
    /// </summary>
    public string MainAppName
    {
        get => _mainAppName;
        set
        {
            _mainAppName = value;
            OnPropertyChanged(nameof(MainAppName));
        }
    }

    /// <summary>
    /// 客户端版本
    /// </summary>
    public string ClientVersion
    {
        get => _clientVersion;
        set
        {
            _clientVersion = value;
            OnPropertyChanged(nameof(ClientVersion));
        }
    }
}