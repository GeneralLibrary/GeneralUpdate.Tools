using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GeneralUpdate.Tool.Avalonia.Models;

public class OSSConfigModel : ObservableObject
{
    private string _packetName, _hash, _version, _url, _jsonContent;
    private DateTime _date;
    private TimeSpan _time;

    public string PacketName 
    { 
        get => _packetName;
        set
        {
            SetProperty(ref _packetName, value);
        }
    }

    public string Hash
    { 
        get => _hash;
        set
        {
            SetProperty(ref _hash, value);
        }
    }

    public string Version 
    { 
        get => _version;
        set
        {
            SetProperty(ref _version, value);
        }
    }

    public string Url 
    { 
        get => _url;
        set
        {
            SetProperty(ref _url, value);
        }
    }

    public string JsonContent
    {
        get => _jsonContent;
        set
        {
            SetProperty(ref _jsonContent, value);
        }
    }

    public DateTime Date
    { 
        get => _date;
        set
        {
            SetProperty(ref _date, value);
        }
    }

    public TimeSpan Time
    { 
        get => _time;
        set
        {
            SetProperty(ref _time, value);
        }
    }
    
    public DateTime PubTime
    { 
        get => Date + Time;
    } 
}