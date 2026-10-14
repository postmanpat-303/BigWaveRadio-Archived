using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;

namespace BigWaveRadio.App.Services;

/// <summary>
/// Analyses the audio actually playing on the default output device (WASAPI
/// loopback) and exposes 32 spectrum band levels (0..1) for visualisers.
/// Falls back to a gentle simulated animation when loopback capture is not
/// available (rare devices / remote sessions) so the UI never looks dead.
/// </summary>
public sealed class SpectrumService
{
    public static SpectrumService Instance { get; } = new();

    public const int BandCount = 32;

    private const int FftSize = 2048;
    private const double FftSizeLog2 = 11;

    private readonly object _gate = new();
    private readonly float[] _bands = new float[BandCount];
    private readonly float[] _smooth = new float[BandCount];
    private readonly float[] _bandMax = new float[BandCount];
    private readonly float[] _simTarget = new float[BandCount];
    private readonly Random _rng = new();

    private WasapiLoopbackCapture? _capture;
    private readonly List<float> _mono = new();
    private Timer? _simTimer;
    private bool _running;
    private bool _simulated;

    /// <summary>Snapshot of the current 32 band levels (0..1), thread-safe.</summary>
    public float[] Snapshot()
    {
        lock (_gate)
        {
            var copy = new float[BandCount];
            Array.Copy(_smooth, copy, BandCount);
            return copy;
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_running) return;
            _running = true;
        }

        try
        {
            _capture = new WasapiLoopbackCapture();
            _mono.Clear();
            _capture.DataAvailable += OnDataAvailable;
            _capture.StartRecording();
            _simulated = false;
        }
        catch
        {
            // Loopback unavailable — animate a plausible spectrum instead.
            _capture?.Dispose();
            _capture = null;
            _simulated = true;
            _simTimer ??= new Timer(_ => TickSimulation(), null, Timeout.Infinite, Timeout.Infinite);
            _simTimer.Change(0, 50);
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (!_running) return;
            _running = false;
        }

        try
        {
            if (_capture is not null)
            {
                _capture.DataAvailable -= OnDataAvailable;
                _capture.StopRecording();
                _capture.Dispose();
                _capture = null;
            }
            _simTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
        catch
        {
            // Shutting down must never throw.
        }

        lock (_gate)
            Array.Clear(_smooth, 0, BandCount);
    }

    // ---- Real audio path ------------------------------------------------------

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_capture is null) return;
        var fmt = _capture.WaveFormat;
        if (fmt.Encoding != WaveFormatEncoding.IeeeFloat || fmt.BitsPerSample != 32) return;

        int channels = fmt.Channels;
        int floats = e.BytesRecorded / 4;
        var wave = new WaveBuffer(e.Buffer);
        lock (_gate)
        {
            for (int i = 0; i + channels <= floats; i += channels)
            {
                float sum = 0;
                for (int c = 0; c < channels; c++) sum += wave.FloatBuffer[i + c];
                _mono.Add(sum / channels);
            }
        }

        while (true)
        {
            float[] window;
            lock (_gate)
            {
                if (_mono.Count < FftSize) break;
                window = _mono.GetRange(0, FftSize).ToArray();
                _mono.RemoveRange(0, FftSize / 2); // 50% overlap
            }
            ComputeBands(window, fmt.SampleRate);
        }
    }

    private void ComputeBands(float[] samples, int sampleRate)
    {
        var fft = new Complex[FftSize];
        for (int i = 0; i < FftSize; i++)
        {
            // Hann window, applied inline.
            double hann = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (FftSize - 1)));
            fft[i].X = (float)(samples[i] * hann);
        }

        FastFourierTransform.FFT(true, (int)FftSizeLog2, fft);

        double binHz = (double)sampleRate / FftSize;
        double fMin = 40, fMax = Math.Min(12000, sampleRate / 2.0);
        double ratio = Math.Pow(fMax / fMin, 1.0 / BandCount);

        lock (_gate)
        {
            for (int b = 0; b < BandCount; b++)
            {
                int lo = Math.Max(1, (int)(fMin * Math.Pow(ratio, b) / binHz));
                int hi = Math.Max(lo, (int)(fMin * Math.Pow(ratio, b + 1) / binHz));

                double sum = 0;
                for (int k = lo; k <= hi && k < FftSize / 2; k++)
                    sum += Math.Sqrt(fft[k].X * fft[k].X + fft[k].Y * fft[k].Y);
                double avg = sum / (hi - lo + 1);

                // Per-band auto-gain: each bar is scaled against its own recent
                // peak (with slow decay), so the visualiser stays lively at any
                // listening volume.
                _bandMax[b] = Math.Max((float)Math.Max(avg, 0.0001), _bandMax[b] * 0.999f);
                float level = (float)Math.Clamp(avg / _bandMax[b], 0, 1);
                level = (float)Math.Pow(level, 0.7);
                _bands[b] = level;
            }

            // Fast attack, slower decay, so bars dance with the music.
            for (int b = 0; b < BandCount; b++)
                _smooth[b] = Math.Max(_bands[b], _smooth[b] * 0.82f);
        }
    }

    // ---- Simulated fallback ---------------------------------------------------

    private void TickSimulation()
    {
        lock (_gate)
        {
            for (int i = 0; i < BandCount; i++)
            {
                if (_rng.NextDouble() < 0.08)
                    _simTarget[i] = (float)(0.25 + 0.75 * _rng.NextDouble());
                float t = (float)(_simTarget[i] * (0.55 + 0.45 * Math.Sin(Environment.TickCount / 900.0 + i * 0.6)));
                _smooth[i] += (t - _smooth[i]) * 0.25f;
            }
        }
    }
}
