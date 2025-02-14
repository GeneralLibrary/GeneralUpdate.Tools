using Avalonia.Controls;
using GeneralUpdate.Tool.Avalonia.ViewModels;

namespace GeneralUpdate.Tool.Avalonia.Views;

public partial class OSSPacketView : UserControl
{
    public OSSPacketView()
    {
        InitializeComponent();
        ClipboardUtility.CreateClipboard(this);
        DataContext = new OSSPacketViewModel();
    }
}