using Avalonia;
using Avalonia.Controls;

namespace GeneralUpdate.Tool.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Storage.Instance.SetStorageProvider(this);
    }
}