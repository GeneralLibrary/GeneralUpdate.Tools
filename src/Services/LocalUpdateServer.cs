using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace GeneralUpdate.Tools.Services;

/// <summary>
/// Local mock update server — modelled after GeneralUpdate-Samples/src/Server.
/// Serves version verification, status reporting, and package download endpoints.
/// </summary>
public class LocalUpdateServer : IAsyncDisposable
{
    private WebApplication? _app;
    private int _port;
    private Task? _runTask;
    private int _nextRecordId = 1;

    public int Port => _port;
    public string BaseUrl => $"http://127.0.0.1:{_port}";

    /// <summary>Registered updates: (CurrentVersion, TargetVersion, Hash, ZipPath, AppType, Platform, ProductId).</summary>
    public List<VersionRecord> Versions { get; } = new();

    public async Task StartAsync(int port = 5000)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        _app = builder.Build();

        // ── POST /Upgrade/Verification ──────────────────────────
        // Matches the sample server: receives VerifyDTO, returns only
        // versions HIGHER than the client's current version.
        _app.MapPost("/Upgrade/Verification", async (HttpContext context) =>
        {
            VerifyDTO? request;
            try
            {
                request = await JsonSerializer.DeserializeAsync<VerifyDTO>(
                    context.Request.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                request = null;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Version))
            {
                await WriteJsonAsync(context, 204, Array.Empty<VerificationResultDTO>());
                return;
            }

            var clientVersion = request.Version;
            var appType = request.AppType;
            var platform = request.Platform;
            var productId = request.ProductId;

            // Filter: only return versions higher than client's current version.
            // This naturally breaks the update loop — once the client is at the
            // latest version, no updates are returned.
            var matches = Versions
                .Where(v =>
                {
                    // AppType filter
                    if (appType.HasValue && v.AppType != appType.Value) return false;
                    // Platform filter
                    if (platform.HasValue && v.Platform != platform.Value) return false;
                    // ProductId filter
                    if (!string.IsNullOrWhiteSpace(productId) &&
                        !string.IsNullOrWhiteSpace(v.ProductId) &&
                        !string.Equals(v.ProductId, productId, StringComparison.OrdinalIgnoreCase))
                        return false;
                    // Version filter: only return versions higher than client's.
                    // Exclude unparseable versions — silently including them
                    // would defeat the update-loop guard.
                    if (!Version.TryParse(v.TargetVersion, out var itemVer)) return false;
                    if (!Version.TryParse(clientVersion, out var clientVer)) return false;
                    return itemVer > clientVer;
                })
                .OrderByDescending(v => Version.TryParse(v.TargetVersion, out var ver) ? ver : new Version(0, 0))
                .ToList();

            if (matches.Count == 0)
            {
                await WriteJsonAsync(context, 200, Array.Empty<VerificationResultDTO>());
                return;
            }

            var results = matches.Select(m =>
            {
                var zipName = Path.GetFileName(m.ZipPath);
                var fileInfo = new FileInfo(m.ZipPath);
                return new VerificationResultDTO
                {
                    RecordId = _nextRecordId++,
                    Name = Path.GetFileNameWithoutExtension(zipName),
                    Version = m.TargetVersion,
                    Hash = m.Hash,
                    Url = $"{BaseUrl}/patch/{Uri.EscapeDataString(zipName)}",
                    AppType = m.AppType,
                    Platform = m.Platform,
                    ProductId = m.ProductId,
                    ReleaseDate = fileInfo.LastWriteTimeUtc,
                    IsForcibly = false,
                    Format = ".zip",
                    Size = fileInfo.Length,
                    IsFreeze = false,
                    UpgradeMode = 1
                };
            }).ToList();

            await WriteJsonAsync(context, 200, results);
        });

        // ── POST /Upgrade/Report ─────────────────────────────────
        _app.MapPost("/Upgrade/Report", () => Results.Ok(new { Code = 200 }));

        // ── GET /patch/{filename} ────────────────────────────────
        _app.MapGet("/patch/{filename}", (string filename) =>
        {
            var filePath = LocalUpdateServerFiles.TryGet(filename);
            if (filePath == null || !File.Exists(filePath))
                return Results.NotFound();
            return Results.File(filePath, "application/zip", filename);
        });

        _runTask = _app.RunAsync();
        await Task.Delay(500);

        var urls = _app.Urls;
        if (urls.Count > 0)
        {
            var uri = new Uri(urls.First());
            _port = uri.Port;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
        if (_runTask != null)
            await _runTask;
    }

    private static async Task WriteJsonAsync<T>(HttpContext context, int code, T body)
    {
        // Always HTTP 200 — the JSON body's "Code" field carries the semantic status.
        // HTTP 204 forbids a response body, which would break JSON deserialization on the client.
        await context.Response.WriteAsJsonAsync(new { Code = code, Body = body });
    }
}

// ── DTOs (matching GeneralUpdate-Samples/src/Server/DTOs) ──────────

public class VerifyDTO
{
    public string? Version { get; set; }
    public int? AppType { get; set; }
    public string? AppKey { get; set; }
    public int? Platform { get; set; }
    public string? ProductId { get; set; }
    public int? UpgradeMode { get; set; }
}

public class VerificationResultDTO
{
    public int RecordId { get; set; }
    public string? Name { get; set; }
    public string? Hash { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? Url { get; set; }
    public DateTime? UrlExpireTimeUtc { get; set; }
    public string? Version { get; set; }
    public int? AppType { get; set; }
    public int? Platform { get; set; }
    public string? ProductId { get; set; }
    public bool? IsForcibly { get; set; }
    public string? Format { get; set; }
    public long? Size { get; set; }
    public bool? IsFreeze { get; set; }
    public int? UpgradeMode { get; set; }
    public bool? IsCrossVersion { get; set; }
    public string? FromVersion { get; set; }
    public string? ToVersion { get; set; }
}

public class VersionRecord
{
    public string CurrentVersion { get; set; } = "";
    public string TargetVersion { get; set; } = "";
    public string Hash { get; set; } = "";
    public string ZipPath { get; set; } = "";
    public int AppType { get; set; } = 1;
    public int Platform { get; set; } = 1;
    public string? ProductId { get; set; }
}

internal static class LocalUpdateServerFiles
{
    private static readonly Dictionary<string, string> _files = new();
    public static void Register(string filename, string filePath) => _files[filename] = filePath;
    public static string? TryGet(string filename) => _files.TryGetValue(filename, out var p) ? p : null;
    public static void Clear() => _files.Clear();
}
