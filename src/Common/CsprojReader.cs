using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GeneralUpdate.Tool.Avalonia.Common;

/// <summary>
/// Utility class for reading .csproj files
/// </summary>
public static class CsprojReader
{
    /// <summary>
    /// Read MainAppName from .csproj file
    /// </summary>
    public static string ReadMainAppName(string releaseDirectory)
    {
        try
        {
            var csprojFile = FindCsprojFile(releaseDirectory);
            if (string.IsNullOrEmpty(csprojFile))
                return string.Empty;

            var doc = XDocument.Load(csprojFile);
            var outputType = GetElementValue(doc, "OutputType");
            
            // Check if OutputType contains WinExe/Exe (case-insensitive)
            if (string.IsNullOrEmpty(outputType) || 
                (!outputType.Equals("WinExe", StringComparison.OrdinalIgnoreCase) && 
                 !outputType.Equals("Exe", StringComparison.OrdinalIgnoreCase)))
            {
                return string.Empty;
            }

            // Extract .csproj filename without extension
            var projectName = Path.GetFileNameWithoutExtension(csprojFile);
            
            // Search for matching .exe file recursively
            var exeFile = FindExeFile(releaseDirectory, projectName);
            if (!string.IsNullOrEmpty(exeFile))
            {
                return Path.GetFileNameWithoutExtension(exeFile);
            }

            // Fallback to AssemblyName or OutputName
            var assemblyName = GetElementValue(doc, "AssemblyName");
            if (!string.IsNullOrEmpty(assemblyName))
                return assemblyName;

            var outputName = GetElementValue(doc, "OutputName");
            if (!string.IsNullOrEmpty(outputName))
                return outputName;

            return string.Empty;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error reading MainAppName: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Read ClientVersion from .csproj file or .exe file version
    /// </summary>
    public static string ReadClientVersion(string releaseDirectory)
    {
        try
        {
            var csprojFile = FindCsprojFile(releaseDirectory);
            if (string.IsNullOrEmpty(csprojFile))
                return string.Empty;

            var doc = XDocument.Load(csprojFile);
            
            // Try to read Version tag
            var version = GetElementValue(doc, "Version");
            if (!string.IsNullOrEmpty(version))
                return version;

            // Fallback to .exe file version
            var projectName = Path.GetFileNameWithoutExtension(csprojFile);
            var exeFile = FindExeFile(releaseDirectory, projectName);
            
            if (!string.IsNullOrEmpty(exeFile) && File.Exists(exeFile))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(exeFile);
                if (!string.IsNullOrEmpty(versionInfo.FileVersion))
                    return versionInfo.FileVersion;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error reading ClientVersion: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Read OutputPath from .csproj file
    /// </summary>
    public static string ReadOutputPath(string releaseDirectory)
    {
        try
        {
            var csprojFile = FindCsprojFile(releaseDirectory);
            if (string.IsNullOrEmpty(csprojFile))
                return string.Empty;

            var doc = XDocument.Load(csprojFile);
            var outputPath = GetElementValue(doc, "OutputPath");
            
            return outputPath ?? string.Empty;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error reading OutputPath: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Find .csproj file in the directory
    /// </summary>
    private static string FindCsprojFile(string directory)
    {
        if (!Directory.Exists(directory))
            return string.Empty;

        var csprojFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly);
        
        if (csprojFiles.Length == 0)
            return string.Empty;
        
        if (csprojFiles.Length > 1)
        {
            Trace.WriteLine($"Warning: Multiple .csproj files found in {directory}. Using the first one: {csprojFiles[0]}");
        }
        
        return csprojFiles[0];
    }

    /// <summary>
    /// Find .exe file with matching name recursively
    /// Note: Uses SearchOption.AllDirectories which may be slow for large directory trees.
    /// This is acceptable as release directories are typically small.
    /// </summary>
    private static string FindExeFile(string directory, string baseName)
    {
        if (!Directory.Exists(directory))
            return string.Empty;

        try
        {
            // First try to find .exe file (Windows)
            var exeFiles = Directory.GetFiles(directory, $"{baseName}.exe", SearchOption.AllDirectories);
            if (exeFiles.Any())
                return exeFiles.First();

            // Then try to find executable without extension (Linux/Mac)
            var allFiles = Directory.GetFiles(directory, baseName, SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                var fileInfo = new FileInfo(file);
                // Check if file is executable (on Unix systems) or if it's an exact match
                if (fileInfo.Name == baseName)
                    return file;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error searching for exe file: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Get element value from XDocument
    /// </summary>
    private static string GetElementValue(XDocument doc, string elementName)
    {
        try
        {
            // Search in all PropertyGroup elements
            var elements = doc.Descendants()
                .Where(e => e.Name.LocalName == elementName);
            
            return elements.FirstOrDefault()?.Value?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
