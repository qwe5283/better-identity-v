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

    public static float[] ResampleLinear(float[] samples, int sourceRate, int targetRate)
    {
        if (sourceRate == targetRate || samples.Length == 0)
        {
            return samples;
        }

        var targetLength = Math.Max(1, (int)Math.Round(samples.Length * (double)targetRate / sourceRate));
        var result = new float[targetLength];
        var ratio = (samples.Length - 1d) / Math.Max(1, targetLength - 1);

        for (var i = 0; i < targetLength; i++)
        {
            var sourcePosition = i * ratio;
            var left = (int)Math.Floor(sourcePosition);
            var right = Math.Min(samples.Length - 1, left + 1);
            var fraction = (float)(sourcePosition - left);
            result[i] = samples[left] + (samples[right] - samples[left]) * fraction;
        }

        return result;
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