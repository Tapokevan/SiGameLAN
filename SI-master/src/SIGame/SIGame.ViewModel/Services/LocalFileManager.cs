using SIGame.ViewModel.Contracts;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Utils;

namespace SIGame.ViewModel.Services;

/// <inheritdoc cref="ILocalFileManager" />
internal sealed class LocalFileManager : ILocalFileManager
{
    private readonly HttpClient _client;

    private readonly Task _localFileManagerTask;
    private readonly CancellationTokenSource _cancellation = new();

    private readonly string _rootFolder;

    private readonly ConcurrentDictionary<Uri, Lazy<Task<string?>>> _downloads = new();

    private readonly Channel<FileTask> _processingQueue = Channel.CreateUnbounded<FileTask>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = true
        });

    public event Action<Uri, Exception>? Error;

    public LocalFileManager(HttpClient client)
    {
        _client = client;
        _rootFolder = Path.Combine(Path.GetTempPath(), "SIGame", Guid.NewGuid().ToString());
        _localFileManagerTask = StartAsync(_cancellation.Token);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            while (await _processingQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_processingQueue.Reader.TryRead(out var fileTask))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await GetFileAsync(fileTask.Uri);
                    try { fileTask.OnCompleted?.Invoke(); }
                    catch (Exception ex) { Trace.TraceError(ex.ToString()); }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    public bool AddFile(Uri uri, Action? onCompleted = null) =>
        _processingQueue.Writer.TryWrite(new FileTask { Uri = uri, OnCompleted = onCompleted });

    public async Task<string?> GetFileAsync(Uri uri)
    {
        if (!uri.IsAbsoluteUri || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        if (TryGetFile(uri) is { } cached)
        {
            return cached;
        }

        var download = _downloads.GetOrAdd(uri, key => new Lazy<Task<string?>>(() => DownloadAsync(key)));
        try
        {
            return await download.Value;
        }
        finally
        {
            _downloads.TryRemove(new KeyValuePair<Uri, Lazy<Task<string?>>>(uri, download));
        }
    }

    private async Task<string?> DownloadAsync(Uri uri)
    {
        if (TryGetFile(uri) is { } cached)
        {
            return cached;
        }

        var cancellation = _cancellation.Token;
        var destination = Path.Combine(_rootFolder, FilePathHelper.GetSafeFileName(uri));
        var temporary = destination + ".part";
        try
        {
            Directory.CreateDirectory(_rootFolder);
            using var response = await _client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellation);
            response.EnsureSuccessStatusCode();
            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellation);
            await using (var output = File.Create(temporary))
            {
                await responseStream.CopyToAsync(output, cancellation);
            }

            File.Move(temporary, destination, overwrite: true);
            return destination;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex)
        {
            Error?.Invoke(uri, ex);
            return null;
        }
        finally
        {
            if (File.Exists(temporary))
            {
                try { File.Delete(temporary); } catch (IOException) { }
            }
        }
    }

    public string? TryGetFile(Uri uri)
    {
        if (!uri.IsAbsoluteUri)
        {
            return null;
        }

        var path = Path.Combine(_rootFolder, FilePathHelper.GetSafeFileName(uri));
        return File.Exists(path) ? path : null;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _cancellation.Cancel();
            await _localFileManagerTask;
            await Task.WhenAll(_downloads.Values.Where(task => task.IsValueCreated).Select(task => task.Value));
            _cancellation.Dispose();

            try
            {
                _processingQueue.Writer.Complete();
            }
            catch (ChannelClosedException)
            {

            }

            if (Directory.Exists(_rootFolder))
            {
                Directory.Delete(_rootFolder, true);
            }
        }
        catch (Exception exc)
        {
            Trace.TraceError("LocalFileManager Dispose error: " + exc);
        }
    }

    private readonly struct FileTask
    {
        public Uri Uri { get; init; }

        public Action? OnCompleted { get; init; }
    }
}
