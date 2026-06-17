using NAudio.Wave;

namespace BetterIdentityV.AudioCapture;

public static class AudioSampleConverter
{
    public static float[] ToMonoFloat(byte[] buffer, int bytesRecorded, WaveFormat waveFormat)
    {
        var channels = Math.Max(1, waveFormat.Channels);
        var sampleCount = bytesRecorded / (waveFormat.BitsPerSample / 8) / channels;
        var result = new float[sampleCount];

        for (var i = 0; i < sampleCount; i++)
        {
            var sum = 0f;
            for (var channel = 0; channel < channels; channel++)
            {
                var sampleIndex = i * channels + channel;
                sum += ReadSample(buffer, sampleIndex, waveFormat);
            }

            result[i] = sum / channels;
        }

        return result;
    }

    public static float[] Resample(float[] samples, int sourceRate, int targetRate)
    {
        if (sourceRate == targetRate || samples.Length == 0)
        {
            return samples;
        }

        var targetLength = Math.Max(1, (int)Math.Round(samples.Length * (double)targetRate / sourceRate));
        var result = new float[targetLength];
        var ratio = sourceRate / (double)targetRate;
        var cutoff = Math.Min(1d, targetRate / (double)sourceRate) * 0.475d;
        const int radius = 16;

        for (var i = 0; i < targetLength; i++)
        {
            var sourcePosition = i * ratio;
            var center = (int)Math.Floor(sourcePosition);
            var sum = 0d;
            var weightSum = 0d;

            for (var tap = center - radius + 1; tap <= center + radius; tap++)
            {
                if (tap < 0 || tap >= samples.Length)
                {
                    continue;
                }

                var x = sourcePosition - tap;
                var window = HannWindow(x, radius);
                var weight = 2d * cutoff * Sinc(2d * cutoff * x) * window;
                sum += samples[tap] * weight;
                weightSum += weight;
            }

            result[i] = Math.Abs(weightSum) <= 1e-12 ? 0f : (float)(sum / weightSum);
        }

        return result;
    }

    private static double Sinc(double x)
    {
        if (Math.Abs(x) <= 1e-12)
        {
            return 1d;
        }

        var pix = Math.PI * x;
        return Math.Sin(pix) / pix;
    }

    private static double HannWindow(double distance, int radius)
    {
        var normalized = Math.Abs(distance) / radius;
        if (normalized >= 1d)
        {
            return 0d;
        }

        return 0.5d + 0.5d * Math.Cos(Math.PI * normalized);
    }

    private static float ReadSample(byte[] buffer, int sampleIndex, WaveFormat waveFormat)
    {
        return waveFormat.Encoding switch
        {
            WaveFormatEncoding.IeeeFloat when waveFormat.BitsPerSample == 32
                => BitConverter.ToSingle(buffer, sampleIndex * 4),
            WaveFormatEncoding.Pcm when waveFormat.BitsPerSample == 16
                => BitConverter.ToInt16(buffer, sampleIndex * 2) / 32768f,
            WaveFormatEncoding.Pcm when waveFormat.BitsPerSample == 24
                => ReadInt24(buffer, sampleIndex * 3) / 8388608f,
            WaveFormatEncoding.Pcm when waveFormat.BitsPerSample == 32
                => BitConverter.ToInt32(buffer, sampleIndex * 4) / 2147483648f,
            _ => 0f,
        };
    }

    private static int ReadInt24(byte[] buffer, int offset)
    {
        var value = buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16);
        if ((value & 0x800000) != 0)
        {
            value |= unchecked((int)0xFF000000);
        }

        return value;
    }
}
