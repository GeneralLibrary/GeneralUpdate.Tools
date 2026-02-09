using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GeneralUpdate.Tool.Avalonia.Models;

public class ExtensionConfigModel : ObservableObject
{
    private string _name;
    private string _version;
    private string _description;
    private string _path;
    private string _extensionDirectory;
    private bool _isUploadToServer;
    private PlatformModel _platform;
    private string _dependencies;
    private string _displayName;
    private string _publisher;
    private string _license;
    private List<string> _categories;
    private string _minHostVersion;
    private string _maxHostVersion;
    private DateTime? _releaseDate;
    private bool _isPreRelease;
    private string _format;
    private string _hash;
    private Dictionary<string, string> _customProperties;
    private bool _showCustomProperties;

    /// <summary>
    /// Extension name
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            SetProperty(ref _name, value);
        }
    }

    /// <summary>
    /// Extension version
    /// </summary>
    public string Version
    {
        get => _version;
        set
        {
            SetProperty(ref _version, value);
        }
    }

    /// <summary>
    /// Extension description
    /// </summary>
    public string Description
    {
        get => _description;
        set
        {
            SetProperty(ref _description, value);
        }
    }

    /// <summary>
    /// Extension export path
    /// </summary>
    public string Path
    {
        get => _path;
        set
        {
            SetProperty(ref _path, value);
        }
    }

    /// <summary>
    /// Extension directory
    /// </summary>
    public string ExtensionDirectory
    {
        get => _extensionDirectory;
        set
        {
            SetProperty(ref _extensionDirectory, value);
        }
    }

    /// <summary>
    /// Whether to upload directly to server
    /// </summary>
    public bool IsUploadToServer
    {
        get => _isUploadToServer;
        set
        {
            SetProperty(ref _isUploadToServer, value);
        }
    }

    /// <summary>
    /// Platform
    /// </summary>
    public PlatformModel Platform
    {
        get => _platform ??= new PlatformModel();
        set => SetProperty(ref _platform, value);
    }

    /// <summary>
    /// Dependencies (comma-separated)
    /// </summary>
    public string Dependencies
    {
        get => _dependencies;
        set
        {
            SetProperty(ref _dependencies, value);
        }
    }
    
    /// <summary>
    /// Display name for UI
    /// </summary>
    public string DisplayName
    {
        get => _displayName;
        set
        {
            SetProperty(ref _displayName, value);
        }
    }

    /// <summary>
    /// Publisher name
    /// </summary>
    public string Publisher
    {
        get => _publisher;
        set
        {
            SetProperty(ref _publisher, value);
        }
    }

    /// <summary>
    /// License identifier (e.g., MIT, Apache-2.0)
    /// </summary>
    public string License
    {
        get => _license;
        set
        {
            SetProperty(ref _license, value);
        }
    }

    /// <summary>
    /// Categories list (comma-separated)
    /// </summary>
    public List<string> Categories
    {
        get => _categories ??= new List<string>();
        set
        {
            SetProperty(ref _categories, value);
        }
    }

    /// <summary>
    /// Categories as a comma-separated string for UI binding
    /// </summary>
    public string CategoriesText
    {
        get => Categories != null && Categories.Count > 0 ? string.Join(", ", Categories) : string.Empty;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Categories = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToList();
            }
            else
            {
                Categories = new List<string>();
            }
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CategoriesText)));
        }
    }

    /// <summary>
    /// Minimum host version required
    /// </summary>
    public string MinHostVersion
    {
        get => _minHostVersion;
        set
        {
            SetProperty(ref _minHostVersion, value);
        }
    }

    /// <summary>
    /// Maximum host version supported
    /// </summary>
    public string MaxHostVersion
    {
        get => _maxHostVersion;
        set
        {
            SetProperty(ref _maxHostVersion, value);
        }
    }

    /// <summary>
    /// Release date
    /// </summary>
    public DateTime? ReleaseDate
    {
        get => _releaseDate;
        set
        {
            SetProperty(ref _releaseDate, value);
        }
    }

    /// <summary>
    /// Is this a pre-release version
    /// </summary>
    public bool IsPreRelease
    {
        get => _isPreRelease;
        set
        {
            SetProperty(ref _isPreRelease, value);
        }
    }

    /// <summary>
    /// File format (.dll, .zip, .so, .dylib, .exe)
    /// </summary>
    public string Format
    {
        get => _format;
        set
        {
            SetProperty(ref _format, value);
        }
    }

    /// <summary>
    /// File hash for integrity verification
    /// </summary>
    public string Hash
    {
        get => _hash;
        set
        {
            SetProperty(ref _hash, value);
        }
    }

    /// <summary>
    /// Custom properties (key-value pairs)
    /// </summary>
    public Dictionary<string, string> CustomProperties
    {
        get => _customProperties ??= new Dictionary<string, string>();
        set => SetProperty(ref _customProperties, value);
    }

    /// <summary>
    /// Controls visibility of CustomProperties input area
    /// </summary>
    public bool ShowCustomProperties
    {
        get => _showCustomProperties;
        set => SetProperty(ref _showCustomProperties, value);
    }
    
    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSize { get; set; } 
}