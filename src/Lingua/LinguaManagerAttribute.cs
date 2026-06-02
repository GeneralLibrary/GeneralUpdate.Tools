using System;

namespace Irihi.Lingua;

/// <summary>
/// Marks a partial class as a Lingua language manager.
/// The resource path points to the base resource file (e.g. .resx or .json).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class LinguaManagerAttribute : Attribute
{
    /// <summary>Path to the base resource file, relative to the project root.</summary>
    public string ResourcePath { get; }

    public LinguaManagerAttribute(string resourcePath)
    {
        ResourcePath = resourcePath;
    }
}
