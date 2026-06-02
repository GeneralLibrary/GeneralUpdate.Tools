using System;
using System.Globalization;
using GeneralUpdate.Tools.Services;

namespace Irihi.Lingua;

/// <summary>
/// Concrete Lingua language manager for GeneralUpdate.Tools.
/// Loads resources from the existing <see cref="LocalizationService"/> JSON files
/// and bridges reactive culture switching with the legacy localization system.
///
/// Usage:
/// <code>
/// // Switch language (updates both Lingua observables AND legacy bindings)
/// AppLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-CN"));
///
/// // Reactive access
/// var title = AppLanguageManager.Instance.App_Title;
/// title.Subscribe(t => Console.WriteLine(t));
///
/// // XAML with TranslateExtension
/// &lt;TextBlock Text="{Translate {x:Static lingua:AppLanguageManager+Keys.App_Title}}" /&gt;
/// </code>
/// </summary>
[LinguaManager("./Resources/Locales")]
public sealed class AppLanguageManager : LanguageManager
{
    public static AppLanguageManager Instance { get; } = new();

    private AppLanguageManager() : base("zh-CN")
    {
        // Load all resources from existing LocalizationService
        LoadFromLocalizationService();

        // Set initial culture
        UpdateCulture(new CultureInfo(LocalizationService.Instance.Locale));

        // Listen for legacy locale changes and sync back to Lingua
        LocalizationService.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LocalizationService.Locale))
                UpdateCulture(new CultureInfo(LocalizationService.Instance.Locale));
        };
    }

    private void LoadFromLocalizationService()
    {
        var loc = LocalizationService.Instance;

        // Register zh-CN from the loaded JSON or fallback dictionary
        AddResources(new CultureInfo("zh-CN"), loc.GetAllStrings("zh-CN"));

        // Register en-US
        AddResources(new CultureInfo("en-US"), loc.GetAllStrings("en-US"));
    }

    // ── Strongly-typed observable properties ─────────────────
    //     Only the most commonly used keys are exposed as properties.
    //     All other keys are accessible via GetObservable("Key.Name").

    public IObservable<string?> App_Title         => GetObservable("App.Title");
    public IObservable<string?> Nav_Patch         => GetObservable("Nav.Patch");
    public IObservable<string?> Nav_Extension     => GetObservable("Nav.Extension");
    public IObservable<string?> Nav_OSS           => GetObservable("Nav.OSS");
    public IObservable<string?> Nav_Simulate      => GetObservable("Nav.Simulate");
    public IObservable<string?> Nav_Config        => GetObservable("Nav.Config");
    public IObservable<string?> Nav_Settings      => GetObservable("Nav.Settings");
    public IObservable<string?> Patch_Title       => GetObservable("Patch.Title");
    public IObservable<string?> Patch_Build       => GetObservable("Patch.Build");

    /// <summary>
    /// Gets the current translation for a key (synchronous, non-observable).
    /// </summary>
    public string this[string key] => Resolve(key) ?? key;

    // ── Keys class (mirrors source-generator output) ─────────

    /// <summary>
    /// Strongly-typed keys for use with <see cref="TranslateExtension"/> in XAML.
    /// Usage: <c>{Translate {x:Static lingua:AppLanguageManager+Keys.App_Title}}</c>
    /// </summary>
    public static class Keys
    {
        public static LinguaKey App_Title         => new("App.Title", Instance);
        public static LinguaKey Nav_Patch         => new("Nav.Patch", Instance);
        public static LinguaKey Nav_Extension     => new("Nav.Extension", Instance);
        public static LinguaKey Nav_OSS           => new("Nav.OSS", Instance);
        public static LinguaKey Nav_Simulate      => new("Nav.Simulate", Instance);
        public static LinguaKey Nav_Config        => new("Nav.Config", Instance);
        public static LinguaKey Nav_Settings      => new("Nav.Settings", Instance);
        public static LinguaKey Patch_Title       => new("Patch.Title", Instance);
        public static LinguaKey Patch_OldDir      => new("Patch.OldDir", Instance);
        public static LinguaKey Patch_NewDir      => new("Patch.NewDir", Instance);
        public static LinguaKey Patch_Build       => new("Patch.Build", Instance);
        public static LinguaKey Patch_PackageInfo => new("Patch.PackageInfo", Instance);
        public static LinguaKey Patch_OutputDir   => new("Patch.OutputDir", Instance);
        public static LinguaKey Patch_Select      => new("Patch.Select", Instance);
        public static LinguaKey Ext_Title         => new("Ext.Title", Instance);
        public static LinguaKey OSS_Title         => new("OSS.Title", Instance);
        public static LinguaKey Sim_Title         => new("Sim.Title", Instance);
        public static LinguaKey Config_Title      => new("Config.Title", Instance);
        public static LinguaKey Settings_UploadSection => new("Settings.UploadSection", Instance);
        public static LinguaKey Settings_Save     => new("Settings.Save", Instance);
        public static LinguaKey Settings_Reset    => new("Settings.Reset", Instance);
        public static LinguaKey Upload_Success    => new("Upload.Success", Instance);
        public static LinguaKey Upload_Failed     => new("Upload.Failed", Instance);
    }
}
