using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GeneralUpdate.Tool.Avalonia.Models;

/// <summary>
/// Model for extension dependency selection
/// </summary>
public class ExtensionDependencySelectionModel : ObservableObject
{
    private bool _isSelected;

    /// <summary>
    /// Extension ID (GUID)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Extension Name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Extension Version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Extension Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Release Date
    /// </summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// Whether this extension is selected as a dependency
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}