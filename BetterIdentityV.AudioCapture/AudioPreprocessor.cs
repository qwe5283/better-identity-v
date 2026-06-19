namespace BetterIdentityV.AudioCapture;

public sealed class AudioPreprocessor
{
    private readonly int _sampleRate;
    private readonly float _highPassCutoffHz;

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

        var forward = ApplyButterworthHighPass(samples);
        Array.Reverse(forward);
        var backward = ApplyButterworthHighPass(forward);
        Array.Reverse(backward);
        return backward;
    }

    private float[] ApplyButterworthHighPass(float[] samples)
    {
        var result = samples.ToArray();

        foreach (var q in Butterworth4Q)
        {
            var section = BiquadFilter.CreateHighPass(_sampleRate, _highPassCutoffHz, q);
            section.ProcessInPlace(result);
        }

        return result;
    }

    private static void NormalizeInPlace(float[] samples)
    {
        if (samples.Length == 0)
        {
            return;
        }

        var mean = 0d;
        for (var i = 0; i < samples.Length; i++)
        {
            mean += samples[i];
        }

        mean /= samples.Length;

        var variance = 0d;
        for (var i = 0; i < samples.Length; i++)
        {
            var delta = samples[i] - mean;
            variance += delta * delta;
        }

        var standardDeviation = Math.Sqrt(variance / samples.Length);
        if (standardDeviation <= 1e-9)
        {
            return;
        }

        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = (float)(samples[i] / standardDeviation);
        }
    }

    private static readonly double[] Butterworth4Q = [0.541196100146197, 1.3065629648763766];

    private sealed class BiquadFilter
    {
        private readonly double _b0;
        private readonly double _b1;
        private readonly double _b2;
        private readonly double _a1;
        private readonly double _a2;

        private double _x1;
        private double _x2;
        private double _y1;
        private double _y2;

        private BiquadFilter(double b0, double b1, double b2, double a1, double a2)
        {
            _b0 = b0;
            _b1 = b1;
            _b2 = b2;
            _a1 = a1;
            _a2 = a2;
        }

        public static BiquadFilter CreateHighPass(int sampleRate, double cutoffHz, double q)
        {
            var normalizedCutoff = Math.Clamp(cutoffHz, 1d, sampleRate / 2d - 1d);
            var omega = 2d * Math.PI * normalizedCutoff / sampleRate;
            var sin = Math.Sin(omega);
            var cos = Math.Cos(omega);
            var alpha = sin / (2d * q);

            var b0 = (1d + cos) / 2d;
            var b1 = -(1d + cos);
            var b2 = (1d + cos) / 2d;
            var a0 = 1d + alpha;
            var a1 = -2d * cos;
            var a2 = 1d - alpha;

            return new BiquadFilter(b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0);
        }

        public void ProcessInPlace(float[] samples)
        {
            for (var i = 0; i < samples.Length; i++)
            {
                var x = samples[i];
                var y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
                _x2 = _x1;
                _x1 = x;
                _y2 = _y1;
                _y1 = y;
                samples[i] = (float)y;
            }
        }
    }
}
