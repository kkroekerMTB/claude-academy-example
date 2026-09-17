using System.IO;
using System.Text;
using System.Windows.Media;

namespace SwarmCatcher.App;

internal sealed class BuzzAudio : IDisposable
{
    private const int SampleRate = 22_050;

    private readonly MediaPlayer _player = new();
    private readonly string _audioPath;
    private bool _isPlaying;
    private bool _isAvailable;

    public BuzzAudio()
    {
        _audioPath = Path.Combine(Path.GetTempPath(), $"swarm-catcher-buzz-{Environment.ProcessId}.wav");

        try
        {
            WriteBuzzWave(_audioPath);
            _player.Open(new Uri(_audioPath));
            _player.MediaEnded += Loop;
            _isAvailable = true;
        }
        catch (IOException)
        {
            _isAvailable = false;
        }
        catch (UnauthorizedAccessException)
        {
            _isAvailable = false;
        }
    }

    public double Volume
    {
        get => _player.Volume;
        set => _player.Volume = Math.Clamp(value, 0, 1);
    }

    public void Play()
    {
        if (!_isAvailable || _isPlaying)
        {
            return;
        }

        _player.Play();
        _isPlaying = true;
    }

    public void Stop()
    {
        if (!_isPlaying)
        {
            return;
        }

        _player.Stop();
        _isPlaying = false;
    }

    public void Dispose()
    {
        _player.MediaEnded -= Loop;
        _player.Close();
        TryDeleteAudioFile();
    }

    private void Loop(object? sender, EventArgs e)
    {
        _player.Position = TimeSpan.Zero;
        _player.Play();
    }

    private void TryDeleteAudioFile()
    {
        try
        {
            File.Delete(_audioPath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void WriteBuzzWave(string path)
    {
        const short channelCount = 1;
        const short bitsPerSample = 16;
        int sampleCount = SampleRate * 2;
        int dataLength = sampleCount * sizeof(short);

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channelCount);
        writer.Write(SampleRate);
        writer.Write(SampleRate * channelCount * bitsPerSample / 8);
        writer.Write((short)(channelCount * bitsPerSample / 8));
        writer.Write(bitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);

        for (int index = 0; index < sampleCount; index++)
        {
            double time = index / (double)SampleRate;
            double pulse = 0.65 + 0.35 * Math.Sin(Math.Tau * 3.5 * time);
            double sample = Math.Sin(Math.Tau * 185 * time) * 0.45 +
                Math.Sin(Math.Tau * 223 * time) * 0.25 +
                Math.Sin(Math.Tau * 271 * time) * 0.15;
            writer.Write((short)(sample * pulse * short.MaxValue * 0.22));
        }
    }
}
