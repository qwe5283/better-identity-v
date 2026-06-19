using MathNet.Numerics.IntegralTransforms;
using System.Numerics;

namespace BetterIdentityV.AudioCapture;

public sealed class NormalizedCrossCorrelationMatcher
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
            ? MatchTemplateFft(stream, _sample, _sampleNorm)
            : MatchTemplateFft(_sample, stream, Norm(stream));
    }

    private static double MatchTemplateFft(float[] source, float[] template, double templateNorm)
    {
        if (template.Length == 0 || source.Length < template.Length || templateNorm <= 1e-9)
        {
            return 0;
        }

        var sourceNormSquares = BuildNormSquares(source, template.Length);
        var correlation = CorrelateValid(source, template);
        var max = 0d;

        for (var start = 0; start < correlation.Length; start++)
        {
            var sourceNorm = Math.Sqrt(sourceNormSquares[start]);
            if (sourceNorm <= 1e-9)
            {
                continue;
            }

            var score = Math.Abs(correlation[start] / (sourceNorm * templateNorm));
            if (score > max)
            {
                max = score;
            }
        }

        return max;
    }

    private static double[] CorrelateValid(float[] source, float[] template)
    {
        var convolutionLength = source.Length + template.Length - 1;
        var fftLength = NextPowerOfTwo(convolutionLength);
        var sourceComplex = new Complex[fftLength];
        var templateComplex = new Complex[fftLength];

        for (var i = 0; i < source.Length; i++)
        {
            sourceComplex[i] = new Complex(source[i], 0d);
        }

        for (var i = 0; i < template.Length; i++)
        {
            templateComplex[i] = new Complex(template[template.Length - 1 - i], 0d);
        }

        Fourier.Forward(sourceComplex, FourierOptions.Matlab);
        Fourier.Forward(templateComplex, FourierOptions.Matlab);

        for (var i = 0; i < fftLength; i++)
        {
            sourceComplex[i] *= templateComplex[i];
        }

        Fourier.Inverse(sourceComplex, FourierOptions.Matlab);

        var validLength = source.Length - template.Length + 1;
        var valid = new double[validLength];
        var offset = template.Length - 1;
        for (var i = 0; i < validLength; i++)
        {
            valid[i] = sourceComplex[offset + i].Real;
        }

        return valid;
    }

    private static double[] BuildNormSquares(float[] source, int windowLength)
    {
        var validLength = source.Length - windowLength + 1;
        var result = new double[validLength];
        var sum = 0d;

        for (var i = 0; i < source.Length; i++)
        {
            sum += source[i] * source[i];
            if (i >= windowLength)
            {
                sum -= source[i - windowLength] * source[i - windowLength];
            }

            if (i >= windowLength - 1)
            {
                result[i - windowLength + 1] = sum;
            }
        }

        return result;
    }

    private static int NextPowerOfTwo(int value)
    {
        var result = 1;
        while (result < value)
        {
            result <<= 1;
        }

        return result;
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
