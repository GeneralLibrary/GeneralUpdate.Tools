using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeneralUpdate.Tools.Configuration;

namespace GeneralUpdate.Tools.Services;

/// <summary>
/// HTTP-based upload service using multipart/form-data.
/// Supports Basic, Bearer Token, and API Key authentication.
/// </summary>
public class HttpUploadService : IHttpUploadService
{
    private readonly HttpClient _http;

    public HttpUploadService()
    {
        _http = new HttpClient();
    }

    /// <summary>
    /// Constructor with custom HttpClient (for testing or shared instances).
    /// </summary>
    public HttpUploadService(HttpClient http)
    {
        _http = http;
    }

    /// <inheritdoc />
    public async Task<UploadResult> UploadAsync(
        string filePath,
        UploadConfig config,
        IProgress<UploadProgressEventArgs>? progress = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            return UploadResult.Fail($"File not found: {filePath}");

        var fileInfo = new FileInfo(filePath);
        var fullUrl = BuildUrl(config);

        Report(progress, 0, fileInfo.Length, UploadPhase.Connecting, "Connecting...");

        var lastError = string.Empty;
        var maxRetries = Math.Max(0, config.RetryCount);

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                using var cts = new CancellationTokenSource(
                    TimeSpan.FromSeconds(config.TimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

                using var content = new MultipartFormDataContent();
                var fileStream = File.OpenRead(filePath);
                var streamContent = new ProgressStreamContent(fileStream, (uploaded, total) =>
                {
                    Report(progress, uploaded, total, UploadPhase.Uploading,
                        $"Uploading... {uploaded / 1024}/{total / 1024} KB");
                });

                streamContent.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");
                content.Add(streamContent, "file", Path.GetFileName(filePath));

                using var request = new HttpRequestMessage(HttpMethod.Post, fullUrl)
                {
                    Content = content,
                };

                // Apply authentication
                ApplyAuth(request, config);

                Report(progress, 0, fileInfo.Length, UploadPhase.Authenticating,
                    $"Authenticating ({config.Auth.Scheme})...");

                using var response = await _http.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    linkedCts.Token);

                var responseBody = await response.Content.ReadAsStringAsync(linkedCts.Token);

                Report(progress, fileInfo.Length, fileInfo.Length, UploadPhase.Verifying,
                    $"Server responded: {(int)response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    Report(progress, fileInfo.Length, fileInfo.Length, UploadPhase.Completed,
                        "Upload complete.");
                    return UploadResult.Ok((int)response.StatusCode, responseBody, fullUrl);
                }

                // Non-retryable status codes
                if ((int)response.StatusCode is >= 400 and < 500 && response.StatusCode !=
                    System.Net.HttpStatusCode.RequestTimeout &&
                    response.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
                {
                    return UploadResult.Fail(
                        $"Server rejected upload: {(int)response.StatusCode} - {responseBody}",
                        (int)response.StatusCode);
                }

                lastError = $"HTTP {(int)response.StatusCode}: {responseBody}";
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                lastError = "Upload timed out";
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex.Message;
            }

            // Wait before retry (exponential backoff: 1s, 2s, 4s...)
            if (attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                Report(progress, 0, fileInfo.Length, UploadPhase.Connecting,
                    $"Retry {attempt + 1}/{maxRetries} in {delay.TotalSeconds:F0}s...");
                await Task.Delay(delay, ct);
            }
        }

        Report(progress, 0, fileInfo.Length, UploadPhase.Failed, lastError);
        return UploadResult.Fail(lastError);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateConnectionAsync(UploadConfig config, CancellationToken ct = default)
    {
        try
        {
            var fullUrl = BuildUrl(config);

            // Issue a lightweight HEAD or GET to the base URL
            using var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
            ApplyAuth(request, config);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

            using var response = await _http.SendAsync(request, linkedCts.Token);

            // Any response (even 401/403) means the server is reachable
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── Helpers ──────────────────────────────────────────────

    private static string BuildUrl(UploadConfig config)
    {
        var server = config.ServerUrl.TrimEnd('/');
        var endpoint = config.UploadEndpoint.StartsWith('/')
            ? config.UploadEndpoint
            : "/" + config.UploadEndpoint;
        return server + endpoint;
    }

    private static void ApplyAuth(HttpRequestMessage request, UploadConfig config)
    {
        var plain = AuthCredentialEncryptor.Decrypt(config.Auth);

        switch (plain.Scheme)
        {
            case AuthScheme.Basic:
                if (!string.IsNullOrEmpty(plain.Username) && !string.IsNullOrEmpty(plain.Password))
                {
                    var credential = Convert.ToBase64String(
                        Encoding.UTF8.GetBytes($"{plain.Username}:{plain.Password}"));
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Basic", credential);
                }
                break;

            case AuthScheme.BearerToken:
                if (!string.IsNullOrEmpty(plain.Token))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", plain.Token);
                }
                break;

            case AuthScheme.ApiKey:
                if (!string.IsNullOrEmpty(plain.ApiKey) && !string.IsNullOrEmpty(plain.ApiKeyHeaderName))
                {
                    request.Headers.TryAddWithoutValidation(plain.ApiKeyHeaderName, plain.ApiKey);
                }
                break;

            case AuthScheme.None:
            default:
                break;
        }
    }

    private static void Report(
        IProgress<UploadProgressEventArgs>? progress,
        long uploaded,
        long total,
        UploadPhase phase,
        string message)
    {
        progress?.Report(new UploadProgressEventArgs
        {
            BytesUploaded = uploaded,
            TotalBytes = total,
            Phase = phase,
            Message = message,
        });
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}

/// <summary>
/// Wraps a <see cref="Stream"/> to report read progress.
/// </summary>
internal class ProgressStreamContent : StreamContent
{
    private readonly Stream _stream;
    private readonly Action<long, long> _onProgress;

    public ProgressStreamContent(Stream stream, Action<long, long> onProgress)
        : base(new ProgressStream(stream, onProgress))
    {
        _stream = stream;
        _onProgress = onProgress;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream.Dispose();
        }
        base.Dispose(disposing);
    }

    private class ProgressStream : Stream
    {
        private readonly Stream _inner;
        private readonly Action<long, long> _onProgress;
        private long _read;

        public ProgressStream(Stream inner, Action<long, long> onProgress)
        {
            _inner = inner;
            _onProgress = onProgress;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = _inner.Read(buffer, offset, count);
            if (n > 0)
            {
                _read += n;
                _onProgress(_read, _inner.Length);
            }
            return n;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
