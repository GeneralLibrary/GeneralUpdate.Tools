using Avalonia;
using Avalonia.Controls;

namespace GeneralUpdate.Tool.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ClipboardUtility.CreateClipboard(this);
        Storage.Instance.SetStorageProvider(this);
    }
}