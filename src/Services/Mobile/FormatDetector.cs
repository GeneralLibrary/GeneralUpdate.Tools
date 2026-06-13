using System.IO;
using System.IO.Compression;
using GeneralUpdate.Tools.Models;

namespace GeneralUpdate.Tools.Services.Mobile;

public class FormatDetectorResult
{
    public PackageFormat Format { get; init; }
    public bool Success { get; init; }
    public string? DisplayName { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Detects mobile package format by file extension and ZIP internal structure.
/// APK → AndroidManifest.xml at root; AAB → base/manifest/AndroidManifest.xml.
/// </summary>
public class FormatDetector
{
    public FormatDetectorResult Detect(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return new FormatDetectorResult { Format = PackageFormat.Unknown, Success = false, ErrorMessage = "File not found." };

        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        return ext switch
        {
            ".apk" => DetectApk(filePath),
            ".aab" => DetectAab(filePath),
            _ => new FormatDetectorResult
            {
                Format = PackageFormat.Unknown,
                Success = false,
                ErrorMessage = $"Unsupported format: {ext}. Supported: .apk, .aab"
            }
        };
    }

    private static FormatDetectorResult DetectApk(string filePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(filePath);
            if (archive.GetEntry("AndroidManifest.xml") != null)
                return new FormatDetectorResult { Format = PackageFormat.Apk, Success = true, DisplayName = "APK" };
        }
        catch (InvalidDataException)
        {
            return new FormatDetectorResult { Format = PackageFormat.Unknown, Success = false, ErrorMessage = "File is not a valid ZIP/APK." };
        }

        return new FormatDetectorResult { Format = PackageFormat.Apk, Success = true, DisplayName = "APK" };
    }

    private static FormatDetectorResult DetectAab(string filePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(filePath);
            if (archive.GetEntry("base/manifest/AndroidManifest.xml") != null)
                return new FormatDetectorResult { Format = PackageFormat.Aab, Success = true, DisplayName = "Android App Bundle" };
        }
        catch (InvalidDataException)
        {
            return new FormatDetectorResult { Format = PackageFormat.Unknown, Success = false, ErrorMessage = "File is not a valid ZIP/AAB." };
        }

        // Even without the expected manifest path, it's still a .aab file
        return new FormatDetectorResult { Format = PackageFormat.Aab, Success = true, DisplayName = "Android App Bundle" };
    }
}
