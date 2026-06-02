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
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Load translations from embedded JSON files (must be after platform init)
            Services.LocalizationService.Instance.LoadFromResources();

            // Initialize configuration (synchronous path for startup reliability)
            var config = LoadConfigSafe();

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
            var configService = ConfigServiceSingleton.Instance;
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

    /// <summary>
    /// Load configuration synchronously with exception protection.
    /// Uses blocking I/O on first load to avoid async-initialization races in Avalonia.
    /// Save operations remain async (fire-and-forget from window close / property changes).
    /// </summary>
    private static AppConfig LoadConfigSafe()
    {
        try
        {
            var configService = ConfigServiceSingleton.Instance;
            // Synchronous load for startup reliability
            configService.Load();
            return configService.Config;
        }
        catch
        {
            return new AppConfig();
        }
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
