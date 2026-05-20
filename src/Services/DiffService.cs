using System;
using System.IO;
using System.Threading.Tasks;
using GeneralUpdate.Differential;

namespace GeneralUpdate.Tools.Services;

public class DiffService
{
    public async Task GeneratePatchAsync(string oldDir, string newDir, string patchDir)
    {
        if (!Directory.Exists(oldDir)) throw new DirectoryNotFoundException("Old: " + oldDir);
        if (!Directory.Exists(newDir)) throw new DirectoryNotFoundException("New: " + newDir);
        Directory.CreateDirectory(patchDir);
        await Task.Run(() => DifferentialCore.Clean(oldDir, newDir, patchDir).GetAwaiter().GetResult());
    }
}