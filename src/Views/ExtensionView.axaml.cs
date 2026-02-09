using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GeneralUpdate.Tool.Avalonia.ViewModels;

namespace GeneralUpdate.Tool.Avalonia.Views;

public partial class ExtensionView : UserControl
{
    public ExtensionView()
    {
        InitializeComponent();
        DataContext = new ExtensionViewModel();
    }
}