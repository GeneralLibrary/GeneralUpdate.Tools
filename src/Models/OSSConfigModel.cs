using CommunityToolkit.Mvvm.ComponentModel;

namespace GeneralUpdate.Tools.Models;

public partial class OSSConfigModel : ObservableObject
{
    [ObservableProperty] private string _packetName = "Packet";
    [ObservableProperty] private string _hash = "";
    [ObservableProperty] private string _version = "1.0.0";
    [ObservableProperty] private string _url = "http://127.0.0.1";
    [ObservableProperty] private string _releaseDate = "";
}
