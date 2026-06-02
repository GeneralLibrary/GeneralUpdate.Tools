using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GeneralUpdate.Tools.Configuration;
using GeneralUpdate.Tools.ViewModels;
using GeneralUpdate.Tools.Views;

namespace GeneralUpdate.Tools;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Load translations from embedded JSON files (falls back to built-in dictionaries)
        Services.LocalizationService.Instance.LoadFromResources();
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Initialize configuration
            var configService = ConfigServiceSingleton.Instance;
            await configService.LoadAsync();
            var config = configService.Config;

            // Apply saved theme
            RequestedThemeVariant = config.Theme == "Dark"
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;

            // Apply saved locale
            Services.LocalizationService.Instance.Locale = config.Language;

            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(config)
            };

            // Restore window state
            mainWindow.Width = config.WindowWidth;
            mainWindow.Height = config.WindowHeight;
            if (config.WindowMaximized)
                mainWindow.WindowState = WindowState.Maximized;

            // Save window state on close
            mainWindow.Closing += (_, _) =>
            {
                if (mainWindow.WindowState == WindowState.Maximized)
                {
                    config.WindowMaximized = true;
                }
                else
                {
                    config.WindowMaximized = false;
                    config.WindowWidth = mainWindow.Width;
                    config.WindowHeight = mainWindow.Height;
                }

                // Fire-and-forget save
                _ = configService.SaveAsync();
            };

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}

/// <summary>
/// Singleton accessor for <see cref="ConfigService"/>.
/// Mirrors the pattern used by <see cref="Services.LocalizationService"/>.
/// </summary>
public static class ConfigServiceSingleton
{
    public static ConfigService Instance { get; } = new();
}
