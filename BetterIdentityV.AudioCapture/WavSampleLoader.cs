using NAudio.Wave;

namespace BetterIdentityV.AudioCapture;

public static class WavSampleLoader
{
    public static float[] LoadMono(string path, int targetSampleRate)
    {
        using var reader = new AudioFileReader(path);
        var sourceFormat = reader.WaveFormat;
        var source = new List<float>();
        var buffer = new float[sourceFormat.SampleRate * sourceFormat.Channels];
        int read;

        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (var i = 0; i < read; i += sourceFormat.Channels)
            {
                var sum = 0f;
                for (var channel = 0; channel < sourceFormat.Channels && i + channel < read; channel++)
                {
                    sum += buffer[i + channel];
                }

                source.Add(sum / sourceFormat.Channels);
            }
        }

        return AudioSampleConverter.Resample(source.ToArray(), sourceFormat.SampleRate, targetSampleRate);
    }
}