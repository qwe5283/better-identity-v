namespace BetterIdentityV.AudioCapture;

public class AudioPreprocessor
{
    private readonly int _sampleRate;
    private readonly float _highPassCutoffHz;
    private float _lastInput;
    private float _lastOutput;

    public AudioPreprocessor(int sampleRate, float highPassCutoffHz = 1000)
    {
        _sampleRate = sampleRate;
        _highPassCutoffHz = highPassCutoffHz;
    }

    public float[] Process(float[] samples)
    {
        var filtered = HighPass(samples);
        NormalizeInPlace(filtered);
        return filtered;
    }

    private float[] HighPass(float[] samples)
    {
        if (_highPassCutoffHz <= 0 || samples.Length == 0)
        {
            return samples.ToArray();
        }

        var result = new float[samples.Length];
        var rc = 1d / (2d * Math.PI * _highPassCutoffHz);
        var dt = 1d / _sampleRate;
        var alpha = (float)(rc / (rc + dt));

        for (var i = 0; i < samples.Length; i++)
        {
            var output = alpha * (_lastOutput + samples[i] - _lastInput);
            result[i] = output;
            _lastInput = samples[i];
            _lastOutput = output;
        }

        return result;
    }

    private static void NormalizeInPlace(float[] samples)
    {
        if (samples.Length == 0)
        {
            return;
        }

        var meanSquare = 0d;
        for (var i = 0; i < samples.Length; i++)
        {
            meanSquare += samples[i] * samples[i];
        }

        var rms = Math.Sqrt(meanSquare / samples.Length);
        if (rms <= 1e-9)
        {
            return;
        }

        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = (float)(samples[i] / rms);
        }
    }
}