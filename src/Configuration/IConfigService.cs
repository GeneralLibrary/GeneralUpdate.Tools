using System.Threading.Tasks;

namespace GeneralUpdate.Tools.Configuration;

/// <summary>
/// Service interface for reading and writing application configuration.
/// </summary>
public interface IConfigService
{
    /// <summary>Current in-memory configuration. Save to persist changes.</summary>
    AppConfig Config { get; }

    /// <summary>Load configuration from disk synchronously. Called once at startup.</summary>
    void Load();

    /// <summary>Load configuration from disk. Called once at startup.</summary>
    Task LoadAsync();

    /// <summary>Persist current configuration to disk (with automatic backup).</summary>
    Task SaveAsync();

    /// <summary>Reset configuration to factory defaults (does not save to disk).</summary>
    void ResetToDefaults();

    /// <summary>Returns the directory where config files are stored.</summary>
    string ConfigDirectory { get; }

    /// <summary>Returns the full path to the main config file.</summary>
    string ConfigFilePath { get; }
}
