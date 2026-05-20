using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GeneralUpdate.Tools.Services;

public class LocalUpdateServer : IAsyncDisposable
{
    private WebApplication? _app;
    private int _port;
    private Task? _runTask;

    public int Port => _port;
    public string BaseUrl => $"http://127.0.0.1:{_port}";

    public List<(string CurrentVersion, string TargetVersion, string Hash, string ZipPath, int AppType)> Updates { get; } = new();

    public async Task StartAsync(int port = 5000)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        _app = builder.Build();

        // GET /Upgrade/Verification
        _app.MapGet("/Upgrade/Verification", async (HttpContext context) =>
        {
            var q = context.Request.Query;
            var currentVer = q["currentVersion"].ToString();
            _ = int.TryParse(q["appType"].ToString(), out var appType);

            var match = Updates.Find(u => u.CurrentVersion == currentVer);
            if (match == default)
            {
                await context.Response.WriteAsJsonAsync(new { Code = 204, Body = Array.Empty<object>() });
                return;
            }

            var body = new[]
            {
                new
                {
                    Name = Path.GetFileName(match.ZipPath),
                    Version = match.TargetVersion,
                    Hash = match.Hash,
                    Url = $"{BaseUrl}/patch/{Uri.EscapeDataString(Path.GetFileName(match.ZipPath))}",
                    AppType = match.AppType,
                    ReleaseDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    IsForcibly = false
                }
            };
            await context.Response.WriteAsJsonAsync(new { Code = 200, Body = body });
        });

        // POST /Upgrade/Report
        _app.MapPost("/Upgrade/Report", () => Results.Ok(new { Code = 200 }));

        // GET /patch/{filename}
        _app.MapGet("/patch/{filename}", async (string filename) =>
        {
            var filePath = LocalUpdateServerFiles.TryGet(filename);
            if (filePath == null || !File.Exists(filePath))
                return Results.NotFound();
            return Results.File(filePath, "application/zip", filename);
        });

        _runTask = _app.RunAsync();
        // Give Kestrel a moment to bind
        await Task.Delay(500);
        // Read actual port from addresses
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
}

internal static class LocalUpdateServerFiles
{
    private static readonly Dictionary<string, string> _files = new();
    public static void Register(string filename, string filePath) => _files[filename] = filePath;
    public static string? TryGet(string filename) => _files.TryGetValue(filename, out var p) ? p : null;
    public static void Clear() => _files.Clear();
}
