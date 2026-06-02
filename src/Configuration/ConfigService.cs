using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GeneralUpdate.Tools.Configuration;

/// <summary>
/// Persistent configuration service. Reads/writes <c>config.json</c> in the
/// application's AppData folder, with automatic backup and schema migration.
/// </summary>
public class ConfigService : IConfigService
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
    };

    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly string _configDir;
    private readonly string _configPath;
    private readonly string _backupPath;

    public AppConfig Config { get; private set; } = new();

    public string ConfigDirectory => _configDir;
    public string ConfigFilePath => _configPath;

    public ConfigService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _configDir = Path.Combine(appData, "GeneralUpdate.Tools");
        _configPath = Path.Combine(_configDir, "config.json");
        _backupPath = Path.Combine(_configDir, "config.json.backup");
    }

    /// <inheritdoc />
    public void Load()
    {
        Directory.CreateDirectory(_configDir);

        if (!File.Exists(_configPath))
        {
            // Try to recover from backup
            if (File.Exists(_backupPath))
            {
                try
                {
                    var backupJson = File.ReadAllText(_backupPath);
                    Config = JsonConvert.DeserializeObject<AppConfig>(backupJson, JsonSettings) ?? new AppConfig();
                    Config.Sanitize();  // repair invalid enum values etc.
                    Save();
                    return;
                }
                catch
                {
                    // Backup is corrupted; fall through to defaults
                }
            }

            Config = new AppConfig();
            Save();
            return;
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            Config = JsonConvert.DeserializeObject<AppConfig>(json, JsonSettings) ?? new AppConfig();
            Config.Sanitize();

            // Run schema migrations
            Migrate();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Config file is corrupted or inaccessible; try backup
            if (File.Exists(_backupPath))
            {
                try
                {
                    var backupJson = File.ReadAllText(_backupPath);
                    Config = JsonConvert.DeserializeObject<AppConfig>(backupJson, JsonSettings) ?? new AppConfig();
                    Config.Sanitize();
                    Save();
                    return;
                }
                catch
                {
                    // Backup also corrupted; reset
                }
            }

            Config = new AppConfig();
            Save();
        }
    }

    /// <summary>Synchronous save for internal use during Load().</summary>
    private void Save()
    {
        _saveLock.Wait();
        try
        {
            Directory.CreateDirectory(_configDir);

            if (File.Exists(_configPath))
            {
                try { File.Copy(_configPath, _backupPath, overwrite: true); }
                catch { /* Non-critical */ }
            }

            var json = JsonConvert.SerializeObject(Config, JsonSettings);
            var tempPath = _configPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _configPath, overwrite: true);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        Directory.CreateDirectory(_configDir);

        if (!File.Exists(_configPath))
        {
            // Try to recover from backup
            if (File.Exists(_backupPath))
            {
                try
                {
                    var backupJson = await File.ReadAllTextAsync(_backupPath);
                    Config = JsonConvert.DeserializeObject<AppConfig>(backupJson, JsonSettings) ?? new AppConfig();
                    await SaveAsync(); // Restore main file from backup
                    return;
                }
                catch
                {
                    // Backup is corrupted; fall through to defaults
                }
            }

            // First run: save defaults so the file exists
            Config = new AppConfig();
            await SaveAsync();
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configPath);
            Config = JsonConvert.DeserializeObject<AppConfig>(json, JsonSettings) ?? new AppConfig();

            // Run schema migrations
            Migrate();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Config file is corrupted or inaccessible; try backup
            if (File.Exists(_backupPath))
            {
                try
                {
                    var backupJson = await File.ReadAllTextAsync(_backupPath);
                    Config = JsonConvert.DeserializeObject<AppConfig>(backupJson, JsonSettings) ?? new AppConfig();
                    Config.Sanitize();
                    await SaveAsync();
                    return;
                }
                catch
                {
                    // Backup also corrupted; reset
                }
            }

            Config = new AppConfig();
            await SaveAsync();
        }
    }

    /// <summary>
    /// Fire-and-forget safe save. Logs exceptions via System.Diagnostics.Trace
    /// rather than losing them silently — no crash, no dialog, just a trace log.
    /// </summary>
    public static void SafeFireAndForgetSave(ConfigService service)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await service.SaveAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[GeneralUpdate.Tools] Config save failed: {ex.Message}");
            }
        });
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            Directory.CreateDirectory(_configDir);

            // Create backup of existing config before overwriting
            if (File.Exists(_configPath))
            {
                try
                {
                    File.Copy(_configPath, _backupPath, overwrite: true);
                }
                catch
                {
                    // Non-critical: backup failed, proceed with save
                }
            }

            var json = JsonConvert.SerializeObject(Config, JsonSettings);

            // Atomic write: write to temp file, then move
            var tempPath = _configPath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);

            // On Windows, File.Move with overwrite is atomic within the same volume
            File.Move(tempPath, _configPath, overwrite: true);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <inheritdoc />
    public void ResetToDefaults()
    {
        Config = new AppConfig();
    }

    // ── Schema Migration ──────────────────────────────────────

    /// <summary>
    /// Apply forward migrations based on <see cref="AppConfig.SchemaVersion"/>.
    /// Add new migration steps here as the config schema evolves.
    /// </summary>
    private void Migrate()
    {
        // Schema v1 is the baseline — no migration needed yet.
        // Example for future v2 migration:
        //
        // if (Config.SchemaVersion < 2)
        // {
        //     Config.SomeNewField = "default value";
        //     Config.SchemaVersion = 2;
        // }
        //
        // Chain further migrations in order.
    }
}
