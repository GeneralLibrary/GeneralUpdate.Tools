using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GeneralUpdate.Tools.V12.ViewModels;
using GeneralUpdate.Tools.V12.Views;

namespace GeneralUpdate.Tools.V12;

public partial class App : Application
{
    public override void Initialize() { AvaloniaXamlLoader.Load(this); }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() };
        base.OnFrameworkInitializationCompleted();
    }
}
