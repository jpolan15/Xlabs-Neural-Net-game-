using System;
using System.Collections.Generic;

namespace Convergence.Core.Math
{
    /// <summary>
    /// Small dense-vector helpers for word vectors and attention. Every method checks that lengths match
    /// and returns a new array, so callers never share state.
    /// </summary>
    public static class VectorMath
    {
        /// <summary>Element-wise a + b.</summary>
        public static double[] Add(double[] a, double[] b)
        {
            CheckPair(a, b);
            var result = new double[a.Length];
            for (int i = 0; i < a.Length; i++) result[i] = a[i] + b[i];
            return result;
        }

        /// <summary>Element-wise a − b.</summary>
        public static double[] Sub(double[] a, double[] b)
        {
            CheckPair(a, b);
            var result = new double[a.Length];
            for (int i = 0; i < a.Length; i++) result[i] = a[i] - b[i];
            return result;
        }

        /// <summary>Element-wise s · a.</summary>
        public static double[] Scale(double[] a, double s)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            var result = new double[a.Length];
            for (int i = 0; i < a.Length; i++) result[i] = a[i] * s;
            return result;
        }

        /// <summary>Dot product Σ aᵢ bᵢ.</summary>
        public static double Dot(double[] a, double[] b)
        {
            CheckPair(a, b);
            double sum = 0.0;
            for (int i = 0; i < a.Length; i++) sum += a[i] * b[i];
            return sum;
        }

        /// <summary>Euclidean length √(a · a).</summary>
        public static double Length(double[] a)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            return System.Math.Sqrt(Dot(a, a));
        }

        /// <summary>Euclidean distance |a − b|.</summary>
        public static double Distance(double[] a, double[] b)
        {
            CheckPair(a, b);
            double sum = 0.0;
            for (int i = 0; i < a.Length; i++)
            {
                double d = a[i] - b[i];
                sum += d * d;
            }

            return System.Math.Sqrt(sum);
        }

        /// <summary>
        /// Cosine similarity (a · b) / (|a| |b|), in [−1, 1]. Returns 0 when either vector has zero length,
        /// because a zero vector points nowhere.
        /// </summary>
        public static double Cosine(double[] a, double[] b)
        {
            CheckPair(a, b);
            double la = Length(a);
            double lb = Length(b);
            if (la == 0.0 || lb == 0.0) return 0.0;
            double c = Dot(a, b) / (la * lb);
            if (c > 1.0) return 1.0;
            if (c < -1.0) return -1.0;
            return c;
        }

        /// <summary>
        /// Index of the row of <paramref name="table"/> closest to <paramref name="target"/> by Euclidean distance,
        /// skipping every index in <paramref name="exclude"/> (may be null). Ties go to the lower index.
        /// Returns −1 when every row is excluded.
        /// </summary>
        public static int Nearest(double[] target, IReadOnlyList<double[]> table, ICollection<int> exclude = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (table == null) throw new ArgumentNullException(nameof(table));

            int best = -1;
            double bestDistance = double.PositiveInfinity;
            for (int i = 0; i < table.Count; i++)
            {
                if (exclude != null && exclude.Contains(i)) continue;
                double d = Distance(target, table[i]);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = i;
                }
            }

            return best;
        }

        private static void CheckPair(double[] a, double[] b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (a.Length != b.Length)
            {
                throw new ArgumentException($"Length mismatch: {a.Length} != {b.Length}.");
            }
        }
    }
}
