using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace GeneralUpdate.Tool.Avalonia.Common;

/// <summary>
/// Utility class for zip file compression operations
/// </summary>
public static class ZipUtility
{
    /// <summary>
    /// Characters that are invalid in file names across all platforms
    /// Includes platform-specific invalid chars and common problematic characters
    /// </summary>
    private static readonly char[] InvalidFileNameChars = 
        Path.GetInvalidFileNameChars()
            .Concat(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' })
            .Distinct()
            .ToArray();

    /// <summary>
    /// Sanitizes a string to be used as a filename by replacing invalid characters
    /// </summary>
    /// <param name="fileName">The filename to sanitize</param>
    /// <param name="replacement">The replacement character for invalid characters (default: '_')</param>
    /// <returns>Sanitized filename</returns>
    public static string SanitizeFileName(string fileName, char replacement = '_')
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return fileName;

        var sanitized = fileName;
        foreach (var invalidChar in InvalidFileNameChars)
        {
            sanitized = sanitized.Replace(invalidChar, replacement);
        }

        return sanitized;
    }
    /// <summary>
    /// Compresses a directory into a zip file
    /// </summary>
    /// <param name="sourceDirectory">Source directory to compress</param>
    /// <param name="destinationZipFile">Destination zip file path</param>
    /// <param name="compressionLevel">Compression level (default: Optimal)</param>
    /// <param name="includeBaseDirectory">Whether to include the base directory in the archive</param>
    /// <exception cref="ArgumentNullException">Thrown when sourceDirectory or destinationZipFile is null or empty</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when sourceDirectory does not exist</exception>
    public static void CompressDirectory(
        string sourceDirectory, 
        string destinationZipFile, 
        CompressionLevel compressionLevel = CompressionLevel.Optimal,
        bool includeBaseDirectory = false)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory))
            throw new ArgumentNullException(nameof(sourceDirectory));

        if (string.IsNullOrWhiteSpace(destinationZipFile))
            throw new ArgumentNullException(nameof(destinationZipFile));

        if (!Directory.Exists(sourceDirectory))
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory}");

        // Ensure the destination directory exists
        var destinationDir = Path.GetDirectoryName(destinationZipFile);
        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        // Delete existing zip file if it exists
        if (File.Exists(destinationZipFile))
        {
            File.Delete(destinationZipFile);
        }

        // Create the zip archive
        ZipFile.CreateFromDirectory(sourceDirectory, destinationZipFile, compressionLevel, includeBaseDirectory);
    }

    /// <summary>
    /// Compresses a directory into a zip file asynchronously
    /// </summary>
    /// <param name="sourceDirectory">Source directory to compress</param>
    /// <param name="destinationZipFile">Destination zip file path</param>
    /// <param name="compressionLevel">Compression level (default: Optimal)</param>
    /// <param name="includeBaseDirectory">Whether to include the base directory in the archive</param>
    /// <returns>Task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when sourceDirectory or destinationZipFile is null or empty</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when sourceDirectory does not exist</exception>
    public static Task CompressDirectoryAsync(
        string sourceDirectory, 
        string destinationZipFile, 
        CompressionLevel compressionLevel = CompressionLevel.Optimal,
        bool includeBaseDirectory = false)
    {
        return Task.Run(() => CompressDirectory(sourceDirectory, destinationZipFile, compressionLevel, includeBaseDirectory));
    }

    /// <summary>
    /// Extracts a zip file to a directory
    /// </summary>
    /// <param name="sourceZipFile">Source zip file to extract</param>
    /// <param name="destinationDirectory">Destination directory for extraction</param>
    /// <param name="overwriteFiles">Whether to overwrite existing files</param>
    /// <exception cref="ArgumentNullException">Thrown when sourceZipFile or destinationDirectory is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when sourceZipFile does not exist</exception>
    /// <exception cref="InvalidOperationException">Thrown when a zip entry attempts to extract outside the destination directory (zip slip attack)</exception>
    public static void ExtractZipFile(
        string sourceZipFile, 
        string destinationDirectory, 
        bool overwriteFiles = true)
    {
        if (string.IsNullOrWhiteSpace(sourceZipFile))
            throw new ArgumentNullException(nameof(sourceZipFile));

        if (string.IsNullOrWhiteSpace(destinationDirectory))
            throw new ArgumentNullException(nameof(destinationDirectory));

        if (!File.Exists(sourceZipFile))
            throw new FileNotFoundException($"Source zip file not found: {sourceZipFile}");

        // Ensure the destination directory exists
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        // Get the normalized full path of the destination directory
        var normalizedDestination = Path.GetFullPath(destinationDirectory);

        // Extract the zip archive with zip slip protection
        using (var archive = System.IO.Compression.ZipFile.OpenRead(sourceZipFile))
        {
            foreach (var entry in archive.Entries)
            {
                // Get the full path where the entry will be extracted
                var entryPath = Path.Combine(destinationDirectory, entry.FullName);
                var normalizedEntryPath = Path.GetFullPath(entryPath);

                // Validate that the entry path is within the destination directory (zip slip protection)
                if (!normalizedEntryPath.StartsWith(normalizedDestination, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Zip entry '{entry.FullName}' attempts to extract outside the destination directory. " +
                        "This may indicate a zip slip attack.");
                }

                // Create directory for the entry if needed
                if (string.IsNullOrEmpty(entry.Name))
                {
                    // This is a directory entry
                    Directory.CreateDirectory(normalizedEntryPath);
                }
                else
                {
                    // This is a file entry
                    var entryDirectory = Path.GetDirectoryName(normalizedEntryPath);
                    if (!string.IsNullOrEmpty(entryDirectory) && !Directory.Exists(entryDirectory))
                    {
                        Directory.CreateDirectory(entryDirectory);
                    }

                    // Extract the file
                    entry.ExtractToFile(normalizedEntryPath, overwriteFiles);
                }
            }
        }
    }

    /// <summary>
    /// Extracts a zip file to a directory asynchronously
    /// </summary>
    /// <param name="sourceZipFile">Source zip file to extract</param>
    /// <param name="destinationDirectory">Destination directory for extraction</param>
    /// <param name="overwriteFiles">Whether to overwrite existing files</param>
    /// <returns>Task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when sourceZipFile or destinationDirectory is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when sourceZipFile does not exist</exception>
    /// <exception cref="InvalidOperationException">Thrown when a zip entry attempts to extract outside the destination directory (zip slip attack)</exception>
    public static Task ExtractZipFileAsync(
        string sourceZipFile, 
        string destinationDirectory, 
        bool overwriteFiles = true)
    {
        return Task.Run(() => ExtractZipFile(sourceZipFile, destinationDirectory, overwriteFiles));
    }

    /// <summary>
    /// Adds a file to an existing zip archive
    /// </summary>
    /// <param name="zipFilePath">Path to the zip file</param>
    /// <param name="entryName">Entry name in the archive</param>
    /// <param name="content">Content to add</param>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null or empty</exception>
    /// <exception cref="ArgumentException">Thrown when parameters are empty or whitespace</exception>
    /// <exception cref="FileNotFoundException">Thrown when zipFilePath does not exist</exception>
    public static void AddFileToZip(string zipFilePath, string entryName, string content)
    {
        if (string.IsNullOrWhiteSpace(zipFilePath))
            throw new ArgumentException("Zip file path cannot be null or empty", nameof(zipFilePath));

        if (string.IsNullOrWhiteSpace(entryName))
            throw new ArgumentException("Entry name cannot be null or empty", nameof(entryName));

        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (!File.Exists(zipFilePath))
            throw new FileNotFoundException($"Zip file not found: {zipFilePath}");

        using (var archive = System.IO.Compression.ZipFile.Open(zipFilePath, ZipArchiveMode.Update))
        {
            // Remove existing entry if it exists
            var existingEntry = archive.GetEntry(entryName);
            existingEntry?.Delete();

            // Create new entry
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open()))
            {
                writer.Write(content);
            }
        }
    }

    /// <summary>
    /// Adds a file to an existing zip archive asynchronously
    /// </summary>
    /// <param name="zipFilePath">Path to the zip file</param>
    /// <param name="entryName">Entry name in the archive</param>
    /// <param name="content">Content to add</param>
    /// <returns>Task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when parameters are empty or whitespace</exception>
    /// <exception cref="FileNotFoundException">Thrown when zipFilePath does not exist</exception>
    public static Task AddFileToZipAsync(string zipFilePath, string entryName, string content)
    {
        return Task.Run(() => AddFileToZip(zipFilePath, entryName, content));
    }
}