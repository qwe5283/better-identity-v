namespace BetterIdentityV.AudioCapture;

public class NormalizedCrossCorrelationMatcher
{
    private readonly float[] _sample;
    private readonly double _sampleNorm;

    public NormalizedCrossCorrelationMatcher(float[] sample)
    {
        _sample = sample;
        _sampleNorm = Norm(sample);
    }

    public double Match(float[] stream)
    {
        if (_sample.Length == 0 || stream.Length == 0 || _sampleNorm <= 1e-9)
        {
            return 0;
        }

        return stream.Length >= _sample.Length
            ? MatchTemplate(stream, _sample, _sampleNorm)
            : MatchTemplate(_sample, stream, Norm(stream));
    }

    private static double MatchTemplate(float[] source, float[] template, double templateNorm)
    {
        if (template.Length == 0 || source.Length < template.Length || templateNorm <= 1e-9)
        {
            return 0;
        }

        var max = 0d;
        var lastStart = source.Length - template.Length;
        for (var start = 0; start <= lastStart; start++)
        {
            var dot = 0d;
            var sourceNormSquare = 0d;
            for (var i = 0; i < template.Length; i++)
            {
                var sourceValue = source[start + i];
                dot += sourceValue * template[i];
                sourceNormSquare += sourceValue * sourceValue;
            }

            var sourceNorm = Math.Sqrt(sourceNormSquare);
            if (sourceNorm <= 1e-9)
            {
                continue;
            }

            var score = Math.Abs(dot / (sourceNorm * templateNorm));
            if (score > max)
            {
                max = score;
            }
        }

        return max;
    }

    private static double Norm(float[] samples)
    {
        var sum = 0d;
        for (var i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }

        return Math.Sqrt(sum);
    }
}