using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using GeneralUpdate.Tools.Models;

namespace GeneralUpdate.Tools.Services.Mobile;

public class MobileCsprojInfo
{
    public string? PackageName { get; init; }             // ApplicationId
    public string? VersionName { get; init; }              // ApplicationDisplayVersion
    public string? VersionCode { get; init; }              // ApplicationVersion
    public string? TargetFramework { get; init; }
    public string? AndroidPackageFormat { get; init; }     // apk / aab
    public bool IsMaui { get; init; }                      // <UseMaui>true</UseMaui>
    public string? AssemblyName { get; init; }
    public string? ProjectDir { get; init; }
    public string? ProjectName { get; init; }
    public string? ErrorMessage { get; init; }
    public bool Success { get; init; }
}

/// <summary>
/// Parses .csproj files for MAUI / Avalonia Android projects to extract
/// ApplicationId, ApplicationDisplayVersion, ApplicationVersion, etc.
/// </summary>
public static class MobileCsprojParser
{
    public static MobileCsprojInfo? Parse(string csprojPath)
    {
        if (string.IsNullOrWhiteSpace(csprojPath) || !File.Exists(csprojPath))
            return new MobileCsprojInfo { Success = false, ErrorMessage = "File not found." };

        try
        {
            var doc = XDocument.Load(csprojPath);
            var projectDir = Path.GetDirectoryName(Path.GetFullPath(csprojPath)) ?? "";
            var projectName = Path.GetFileNameWithoutExtension(csprojPath);

            var targetFramework = GetElementValue(doc, "TargetFramework");
            var targetFrameworks = GetElementValue(doc, "TargetFrameworks");

            if (targetFramework == null && targetFrameworks == null)
                return new MobileCsprojInfo
                {
                    Success = false,
                    ErrorMessage = "Missing <TargetFramework> or <TargetFrameworks> element in .csproj."
                };

            // Prefer single TFM; if multi-TFM, pick the one containing "-android"
            var resolvedTfm = targetFramework;
            if (resolvedTfm == null && targetFrameworks != null)
            {
                var tfms = targetFrameworks
                    .Split(';', ',')
                    .Select(t => t.Trim())
                    .ToArray();

                resolvedTfm = tfms.FirstOrDefault(t => t.Contains("-android", StringComparison.OrdinalIgnoreCase))
                            ?? tfms.FirstOrDefault();
            }

            if (resolvedTfm == null)
                return new MobileCsprojInfo
                {
                    Success = false,
                    ErrorMessage = "Unable to resolve a target framework from the .csproj."
                };

            // Verify it's an Android project
            if (!resolvedTfm.Contains("-android", StringComparison.OrdinalIgnoreCase))
                return new MobileCsprojInfo
                {
                    Success = false,
                    ErrorMessage = $"TargetFramework '{resolvedTfm}' is not an Android project. Must contain '-android'."
                };

            var assemblyName = GetElementValue(doc, "AssemblyName") ?? projectName;
            var useMauiStr = GetElementValue(doc, "UseMaui");
            var isMaui = useMauiStr != null && useMauiStr.Equals("true", System.StringComparison.OrdinalIgnoreCase);

            return new MobileCsprojInfo
            {
                Success = true,
                PackageName = GetElementValue(doc, "ApplicationId"),
                VersionName = GetElementValue(doc, "ApplicationDisplayVersion"),
                VersionCode = GetElementValue(doc, "ApplicationVersion"),
                TargetFramework = resolvedTfm,
                AndroidPackageFormat = GetElementValue(doc, "AndroidPackageFormat"),
                IsMaui = isMaui,
                AssemblyName = assemblyName,
                ProjectDir = projectDir,
                ProjectName = projectName
            };
        }
        catch (System.Xml.XmlException ex)
        {
            return new MobileCsprojInfo { Success = false, ErrorMessage = $"XML parse error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Gets the default publish output directory for an Android project.
    /// </summary>
    public static string GetDefaultPublishDir(MobileCsprojInfo info)
    {
        if (info.ProjectDir == null || info.TargetFramework == null)
            return string.Empty;

        return Path.Combine(info.ProjectDir, "bin", "Release", info.TargetFramework, "publish");
    }

    /// <summary>
    /// Gets expected APK/AAB file extension based on AndroidPackageFormat.
    /// Default (aab;apk) or empty → prefers .apk
    /// </summary>
    public static string GetExpectedExtension(MobileCsprojInfo info)
    {
        var fmt = info.AndroidPackageFormat?.ToLowerInvariant() ?? "";
        if (fmt == "apk") return ".apk";
        if (fmt == "aab") return ".aab";
        return ".apk"; // default (aab;apk) → prefer apk
    }

    /// <summary>
    /// Gets the project type display name.
    /// </summary>
    public static string GetProjectTypeDisplay(MobileCsprojInfo info)
    {
        if (info.IsMaui) return "MAUI";
        return "Avalonia";
    }

    private static string? GetElementValue(XContainer doc, string elementName)
    {
        var ns = doc.Document?.Root?.GetDefaultNamespace() ?? XNamespace.None;
        var el = doc.Descendants(ns + elementName).FirstOrDefault();
        if (el != null && !string.IsNullOrWhiteSpace(el.Value))
            return el.Value.Trim();

        // Fallback: search without namespace
        foreach (var d in doc.Descendants())
        {
            if (d.Name.LocalName == elementName && !string.IsNullOrWhiteSpace(d.Value))
                return d.Value.Trim();
        }

        return null;
    }
}
