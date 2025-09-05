using CommunityToolkit.Mvvm.ComponentModel;

using System;

namespace GeneralUpdate.Tool.Avalonia.ViewModels;

public partial class OSSConfigVM : ObservableObject
{
    [ObservableProperty]
    private string _packetName;

    [ObservableProperty]
    private string _hash;

    [ObservableProperty]
    private string _version;

    [ObservableProperty]
    private string _url;

    [ObservableProperty]
    private string _jsonContent;

    /// <summary>本地使用</summary>
    [ObservableProperty]
    private DateTime _date;

    /// <summary>本地使用</summary>
    [ObservableProperty]
    private TimeSpan _time;

    public DateTime PubTime
    {
        get => Date + Time;
    }
}