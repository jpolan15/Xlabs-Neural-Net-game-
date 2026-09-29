using System;
using Convergence.Core.Math;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// Chamber 04 nav-log attention. Tokens are fixed vectors, not a language model.
    /// Masking the corrupted NOISE token is what moves the prediction to JUMP_HOME.
    /// </summary>
    public static class NavLogAttention
    {
        /// <summary>Context tokens, in order. The query asks which jump the log supports.</summary>
        public static readonly string[] ContextTokens = { "EARTH_LOCK", "FUEL_OK", "NOISE" };

        /// <summary>Candidate the circuit should predict once noise is masked.</summary>
        public const string HomeToken = "JUMP_HOME";

        // keyDim = 2. EARTH_LOCK aligns with the query. NOISE copies that key but points at a different value.
        private static readonly double[,] Query = { { 1.0, 0.0 } };
        private static readonly double[,] Keys =
        {
            { 1.0, 0.0 },
            { 0.15, 0.10 },
            { 1.4, 0.0 }
        };
        private static readonly double[,] Values =
        {
            { 1.0, 0.0 },
            { 0.25, 0.05 },
            { 0.0, 1.0 }
        };
        private static readonly double[] HomePrototype = { 1.0, 0.0 };
        private static readonly double[] NoisePrototype = { 0.0, 1.0 };

        /// <summary>
        /// Runs attention over the three log tokens. <paramref name="maskNoise"/> drops the NOISE key.
        /// </summary>
        public static NavAttentionReport Predict(bool maskNoise)
        {
            var mask = new bool[1, 3];
            mask[0, 0] = true;
            mask[0, 1] = true;
            mask[0, 2] = !maskNoise;

            Attention.Result result = Attention.ScaledDotProduct(Query, Keys, Values, mask);
            double home = Dot(result.Output, HomePrototype);
            double noise = Dot(result.Output, NoisePrototype);
            string predicted = home >= noise ? HomeToken : "NOISE";

            return new NavAttentionReport(
                predicted,
                result.Weights[0, 0],
                result.Weights[0, 1],
                result.Weights[0, 2],
                predicted == HomeToken);
        }

        private static double Dot(double[,] row, double[] vector)
        {
            double sum = 0.0;
            for (int i = 0; i < vector.Length; i++)
            {
                sum += row[0, i] * vector[i];
            }

            return sum;
        }
    }

    /// <summary>
    /// Attention weights and the nearest jump token.
    /// </summary>
    public sealed class NavAttentionReport
    {
        /// <summary>Token the output is closer to.</summary>
        public string PredictedToken { get; }

        /// <summary>Weight on EARTH_LOCK.</summary>
        public double EarthLockWeight { get; }

        /// <summary>Weight on FUEL_OK.</summary>
        public double FuelWeight { get; }

        /// <summary>Weight on NOISE. Zero when that token is masked.</summary>
        public double NoiseWeight { get; }

        /// <summary>True only when the nearest token is JUMP_HOME.</summary>
        public bool PredictsJumpHome { get; }

        /// <summary>Creates a report.</summary>
        public NavAttentionReport(string predictedToken, double earthLockWeight, double fuelWeight, double noiseWeight, bool predictsJumpHome)
        {
            PredictedToken = predictedToken ?? string.Empty;
            EarthLockWeight = earthLockWeight;
            FuelWeight = fuelWeight;
            NoiseWeight = noiseWeight;
            PredictsJumpHome = predictsJumpHome;
        }
    }
}
