using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using SIUI.Behaviors;
using SIUI.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SIUI;

/// <summary>
/// Plays a cached question video using the bundled decoder.
/// </summary>
public sealed class VideoPlaybackView : UserControl
{
    private static readonly Lazy<LibVLC> Engine = new(() =>
    {
        Core.Initialize();
        return new LibVLC("--no-video-title-show", "--avcodec-hw=none");
    });

    public static readonly DependencyProperty AddressProperty = DependencyProperty.Register(
        nameof(Address), typeof(string), typeof(VideoPlaybackView),
        new PropertyMetadata(null, (d, _) => ((VideoPlaybackView)d).Reload()));

    public static readonly DependencyProperty OriginalAddressProperty = DependencyProperty.Register(
        nameof(OriginalAddress), typeof(string), typeof(VideoPlaybackView));

    public static readonly DependencyProperty TableInfoProperty = DependencyProperty.Register(
        nameof(TableInfo), typeof(TableInfoViewModel), typeof(VideoPlaybackView),
        new PropertyMetadata(null, (d, _) => ((VideoPlaybackView)d).Reload()));

    public string? Address
    {
        get => (string?)GetValue(AddressProperty);
        set => SetValue(AddressProperty, value);
    }

    public string? OriginalAddress
    {
        get => (string?)GetValue(OriginalAddressProperty);
        set => SetValue(OriginalAddressProperty, value);
    }

    public TableInfoViewModel? TableInfo
    {
        get => (TableInfoViewModel?)GetValue(TableInfoProperty);
        set => SetValue(TableInfoProperty, value);
    }

    private readonly VideoView _surface = new();
    private readonly DispatcherTimer _progressTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private LibVLCSharp.Shared.MediaPlayer? _player;
    private Media? _media;
    private TableInfoViewModel? _activeTable;
    private int _generation;

    public VideoPlaybackView()
    {
        Content = _surface;
        Loaded += (_, _) => Reload();
        Unloaded += (_, _) => Reset();
        _progressTimer.Tick += (_, _) =>
        {
            if (_player is { Length: > 0 } player)
            {
                _activeTable?.OnMediaProgress((double)player.Time / player.Length);
            }
        };
    }

    private async void Reload()
    {
        Reset();
        if (!IsLoaded || TableInfo is not { } table || !Uri.TryCreate(Address, UriKind.Absolute, out var address))
        {
            return;
        }

        var generation = _generation;
        var originalAddress = OriginalAddress;
        try
        {
            var source = address;
            if (!source.IsFile)
            {
                if (table.ResolvePlaybackUri is not { } resolve)
                {
                    throw new InvalidOperationException("Источник видео должен быть локальным файлом.");
                }

                source = await resolve(address);
            }

            if (generation != _generation || !IsLoaded)
            {
                return;
            }

            if (source is not { IsFile: true })
            {
                throw new InvalidOperationException("Не удалось получить локальную копию видео.");
            }

            var player = new LibVLCSharp.Shared.MediaPlayer(Engine.Value);
            _player = player;
            _surface.MediaPlayer = player;
            _media = new Media(Engine.Value, source.LocalPath, FromType.FromPath);
            _activeTable = table;
            table.MediaPause += PauseVideo;
            table.MediaResume += ResumeVideo;
            table.MediaSeek += Seek;
            table.VolumeChanged += ChangeVolume;
            player.Volume = (int)(MediaController.Volume * 100);
            var opened = false;
            player.Playing += (_, _) => Dispatch(generation, () =>
            {
                if (!opened)
                {
                    opened = true;
                    table.OnMediaLoad();
                    table.OnMediaStart();
                    _progressTimer.Start();
                }

                if (table.Pause || table.IsMediaStopped)
                {
                    PauseVideo();
                }
            });
            player.EndReached += (_, _) => Dispatch(generation, () =>
            {
                _progressTimer.Stop();
                table.OnMediaEnd();
                table.ReportPlaybackCompleted?.Invoke("video", originalAddress ?? address.AbsoluteUri);
            });
            player.EncounteredError += (_, _) => Dispatch(generation, () =>
                table.OnMediaLoadError(new MediaLoadException(address.ToString(),
                    new InvalidOperationException("Декодер не смог воспроизвести видео."))));
            if (!player.Play(_media))
            {
                throw new InvalidOperationException("Не удалось запустить видео.");
            }
        }
        catch (Exception ex)
        {
            if (generation == _generation)
            {
                Reset();
                table.OnMediaLoadError(new MediaLoadException(address.ToString(), ex));
            }
        }
    }

    private void Dispatch(int generation, Action action) => Dispatcher.BeginInvoke(() =>
    {
        if (generation == _generation)
        {
            action();
        }
    });

    private void PauseVideo() => _player?.SetPause(true);

    private void ResumeVideo() => _player?.SetPause(false);

    private void Seek(int percent)
    {
        if (_player is { IsSeekable: true } player)
        {
            player.Position = Math.Clamp(percent / 100f, 0f, 1f);
        }
    }

    private void ChangeVolume(double factor)
    {
        if (_player is { } player)
        {
            player.Volume = Math.Clamp((int)(player.Volume * factor), 0, 100);
        }
    }

    private void Reset()
    {
        _generation++;
        _progressTimer.Stop();
        if (_activeTable is { } table)
        {
            table.MediaPause -= PauseVideo;
            table.MediaResume -= ResumeVideo;
            table.MediaSeek -= Seek;
            table.VolumeChanged -= ChangeVolume;
            _activeTable = null;
        }

        _player?.Stop();
        _surface.MediaPlayer = null;
        _player?.Dispose();
        _player = null;
        _media?.Dispose();
        _media = null;
    }
}
