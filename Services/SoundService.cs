using System.IO;
using System.Media;
using System.Windows;

namespace TimerWidget.Services;

/// <summary>
/// Создаётся только при первом реальном воспроизведении.
/// При выключенной настройке аудиоресурс не читается и устройство не используется.
/// </summary>
internal sealed class SoundService : IDisposable
{
    private readonly MemoryStream _audio;
    private readonly SoundPlayer _player;

    public SoundService()
    {
        var resource = Application.GetResourceStream(
            new Uri("pack://application:,,,/Assets/alarm.wav", UriKind.Absolute))
            ?? throw new InvalidOperationException("Не найден звуковой ресурс alarm.wav.");

        _audio = new MemoryStream();
        using (resource.Stream)
            resource.Stream.CopyTo(_audio);
        _audio.Position = 0;

        _player = new SoundPlayer(_audio);
        _player.Load();
    }

    public void Play()
    {
        _player.Stop();
        _audio.Position = 0;
        _player.Play();
    }

    public void Stop() => _player.Stop();

    public void Dispose()
    {
        _player.Stop();
        _player.Dispose();
        _audio.Dispose();
    }
}
