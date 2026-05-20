using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GeneralUpdate.Tools.V12.Services;

public class PackageService
{
    public async Task CompressDirectoryAsync(string sourceDir, string outputPath)
    {
        await Task.Run(() => { if (File.Exists(outputPath)) File.Delete(outputPath); ZipFile.CreateFromDirectory(sourceDir, outputPath, CompressionLevel.Optimal, false); });
    }
    public async Task CreateManifestAsync(string zipPath, object manifest)
    {
        await Task.Run(() => {
            var json = JsonConvert.SerializeObject(manifest, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Update);
            var entry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(json);
        });
    }
}

public class HashService
{
    public async Task<string> ComputeHashAsync(string filePath)
    {
        return await Task.Run(() => {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        });
    }
}
