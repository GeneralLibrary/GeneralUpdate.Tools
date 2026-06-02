using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeneralUpdate.Tools.Configuration;
using GeneralUpdate.Tools.Services;

namespace GeneralUpdate.Tools.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly AppConfig _config;
    private readonly LocalizationService _loc = LocalizationService.Instance;

    // ── Upload Settings ──────────────────────────────────────

    [ObservableProperty] private string _serverUrl = string.Empty;
    [ObservableProperty] private string _uploadEndpoint = "/api/v1/packages/upload";
    [ObservableProperty] private int _timeoutSeconds = 300;
    [ObservableProperty] private int _retryCount = 3;
    [ObservableProperty] private bool _autoUploadEnabled;

    // ── Auth Settings ────────────────────────────────────────

    [ObservableProperty] private int _selectedAuthScheme;
    [ObservableProperty] private string _authUsername = string.Empty;
    [ObservableProperty] private string _authPassword = string.Empty;
    [ObservableProperty] private string _authToken = string.Empty;
    [ObservableProperty] private string _loginUrl = string.Empty;
    [ObservableProperty] private string _apiKeyHeaderName = "X-API-Key";
    [ObservableProperty] private string _apiKey = string.Empty;

    public List<AuthSchemeItem> AuthSchemes { get; } = new()
    {
        new(AuthScheme.None, "None"),
        new(AuthScheme.Basic, "Basic Auth"),
        new(AuthScheme.BearerToken, "Bearer Token"),
        new(AuthScheme.ApiKey, "API Key"),
    };

    // ── Auth field visibility (computed from SelectedAuthScheme, not fragile index magic) ──

    [ObservableProperty] private bool _isBasicAuthVisible;
    [ObservableProperty] private bool _isBearerTokenVisible;
    [ObservableProperty] private bool _isApiKeyVisible;

    private void RefreshAuthVisibility()
    {
        var scheme = SelectedAuthScheme >= 0 && SelectedAuthScheme < AuthSchemes.Count
            ? AuthSchemes[SelectedAuthScheme].Scheme
            : AuthScheme.None;
        IsBasicAuthVisible = scheme == AuthScheme.Basic;
        IsBearerTokenVisible = scheme == AuthScheme.BearerToken;
        IsApiKeyVisible = scheme == AuthScheme.ApiKey;
    }

    partial void OnSelectedAuthSchemeChanged(int value) => RefreshAuthVisibility();

    // ── Simulation Settings ─────────────────────────────────

    [ObservableProperty] private string _simulationPort = "5000";
    [ObservableProperty] private bool _simulationRequireAuth;

    // ── Feature Switches ─────────────────────────────────────

    [ObservableProperty] private bool _encryptionScanEnabled = true;
    [ObservableProperty] private bool _autoValidateSemver = true;
    [ObservableProperty] private bool _showJsonPreview = true;

    // ── Status ───────────────────────────────────────────────

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isTesting;

    public SettingsViewModel(AppConfig config)
    {
        _config = config;
        LoadFromConfig();
    }

    /// <summary>Populate VM fields from the current AppConfig.</summary>
    private void LoadFromConfig()
    {
        // Upload
        ServerUrl = _config.UploadServerUrl;
        UploadEndpoint = _config.UploadEndpoint;
        TimeoutSeconds = _config.UploadTimeoutSeconds;
        RetryCount = _config.UploadRetryCount;
        AutoUploadEnabled = _config.AutoUploadEnabled;

        // Auth
        SelectedAuthScheme = AuthSchemes.FindIndex(a => a.Scheme == _config.UploadAuth.Scheme);
        if (SelectedAuthScheme < 0) SelectedAuthScheme = 0;
        AuthUsername = _config.UploadAuth.Username;
        AuthPassword = AuthCredentialEncryptor.Unprotect(_config.UploadAuth.EncryptedPassword);
        AuthToken = AuthCredentialEncryptor.Unprotect(_config.UploadAuth.EncryptedToken);
        LoginUrl = _config.UploadAuth.LoginUrl;
        ApiKeyHeaderName = _config.UploadAuth.ApiKeyHeaderName;
        ApiKey = AuthCredentialEncryptor.Unprotect(_config.UploadAuth.EncryptedApiKey);

        // Simulation
        SimulationPort = _config.SimulationServerPort;
        SimulationRequireAuth = _config.SimulationRequireAuth;

        // Feature switches
        EncryptionScanEnabled = _config.EncryptionScanEnabled;
        AutoValidateSemver = _config.AutoValidateSemver;
        ShowJsonPreview = _config.ShowJsonPreview;

        RefreshAuthVisibility();
    }

    /// <summary>Persist VM fields back to AppConfig and save.</summary>
    private async Task SaveToConfigAsync()
    {
        _config.UploadServerUrl = ServerUrl;
        _config.UploadEndpoint = UploadEndpoint;
        _config.UploadTimeoutSeconds = TimeoutSeconds;
        _config.UploadRetryCount = RetryCount;
        _config.AutoUploadEnabled = AutoUploadEnabled;

        // Auth
        if (SelectedAuthScheme >= 0 && SelectedAuthScheme < AuthSchemes.Count)
            _config.UploadAuth.Scheme = AuthSchemes[SelectedAuthScheme].Scheme;
        else
            _config.UploadAuth.Scheme = AuthScheme.None;

        _config.UploadAuth.Username = AuthUsername;
        _config.UploadAuth.EncryptedPassword = AuthCredentialEncryptor.Protect(AuthPassword);
        _config.UploadAuth.EncryptedToken = AuthCredentialEncryptor.Protect(AuthToken);
        _config.UploadAuth.LoginUrl = LoginUrl;
        _config.UploadAuth.ApiKeyHeaderName = ApiKeyHeaderName;
        _config.UploadAuth.EncryptedApiKey = AuthCredentialEncryptor.Protect(ApiKey);

        // Simulation
        _config.SimulationServerPort = SimulationPort;
        _config.SimulationRequireAuth = SimulationRequireAuth;

        // Feature switches
        _config.EncryptionScanEnabled = EncryptionScanEnabled;
        _config.AutoValidateSemver = AutoValidateSemver;
        _config.ShowJsonPreview = ShowJsonPreview;

        await ConfigServiceSingleton.Instance.SaveAsync();
    }

    [RelayCommand]
    private async Task Save()
    {
        await SaveToConfigAsync();
        StatusMessage = _loc["Settings.Saved"];
    }

    [RelayCommand]
    private async Task TestConnection()
    {
        IsTesting = true;
        StatusMessage = _loc["Settings.Testing"];

        try
        {
            // Save current settings to config first, then test
            await SaveToConfigAsync();

            var uploadConfig = new UploadConfig
            {
                ServerUrl = _config.UploadServerUrl,
                UploadEndpoint = _config.UploadEndpoint,
                TimeoutSeconds = 15,
                RetryCount = 0,
                Auth = _config.UploadAuth,
            };

            var svc = new HttpUploadService();
            var ok = await svc.ValidateConnectionAsync(uploadConfig);

            StatusMessage = ok
                ? _loc["Settings.ConnectionOk"]
                : _loc["Settings.ConnectionFailed"];
        }
        catch
        {
            StatusMessage = _loc["Settings.ConnectionFailed"];
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private async Task ResetToDefaults()
    {
        ConfigServiceSingleton.Instance.ResetToDefaults();
        LoadFromConfig();
        await ConfigServiceSingleton.Instance.SaveAsync();
        StatusMessage = _loc["Settings.ResetDone"];
    }
}

/// <summary>
/// Display item for the authentication scheme dropdown.
/// </summary>
public class AuthSchemeItem
{
    public AuthScheme Scheme { get; }
    public string DisplayName { get; }

    public AuthSchemeItem(AuthScheme scheme, string displayName)
    {
        Scheme = scheme;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}
