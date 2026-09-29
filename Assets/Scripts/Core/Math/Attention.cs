using System;

namespace Convergence.Core.Math
{
    /// <summary>
    /// Scaled dot-product attention.
    /// Attention(Q, K, V) = softmax(Q K^T / sqrt(d_k)) V.
    /// </summary>
    public static class Attention
    {
        /// <summary>
        /// Result of one attention evaluation. Weights are the softmax matrix [queryCount, keyCount].
        /// Output is [queryCount, valueDim].
        /// </summary>
        public sealed class Result
        {
            /// <summary>Attention weights after softmax. Each row sums to 1.</summary>
            public double[,] Weights { get; }

            /// <summary>Weighted sum of values.</summary>
            public double[,] Output { get; }

            internal Result(double[,] weights, double[,] output)
            {
                Weights = weights;
                Output = output;
            }
        }

        /// <summary>
        /// Computes scaled dot-product attention.
        /// Query is [queryCount, keyDim], key is [keyCount, keyDim], value is [keyCount, valueDim].
        /// A false entry in <paramref name="keepMask"/> (shaped [queryCount, keyCount]) sends that logit to negative infinity before softmax.
        /// </summary>
        public static Result ScaledDotProduct(double[,] query, double[,] key, double[,] value, bool[,] keepMask = null)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            int queryCount = query.GetLength(0);
            int keyDim = query.GetLength(1);
            int keyCount = key.GetLength(0);
            int valueDim = value.GetLength(1);

            if (queryCount == 0 || keyCount == 0 || keyDim == 0 || valueDim == 0)
            {
                throw new ArgumentException("Attention matrices must be non-empty.");
            }
            if (key.GetLength(1) != keyDim)
            {
                throw new ArgumentException("Key dimension must match the query dimension.");
            }
            if (value.GetLength(0) != keyCount)
            {
                throw new ArgumentException("Value row count must match the key row count.");
            }
            if (keepMask != null && (keepMask.GetLength(0) != queryCount || keepMask.GetLength(1) != keyCount))
            {
                throw new ArgumentException("Mask shape must be [queryCount, keyCount].");
            }

            double scale = 1.0 / System.Math.Sqrt(keyDim);
            var weights = new double[queryCount, keyCount];
            var output = new double[queryCount, valueDim];

            for (int q = 0; q < queryCount; q++)
            {
                var logits = new double[keyCount];
                bool anyKept = keepMask == null;
                for (int k = 0; k < keyCount; k++)
                {
                    bool keep = keepMask == null || keepMask[q, k];
                    if (!keep)
                    {
                        logits[k] = double.NegativeInfinity;
                        continue;
                    }

                    anyKept = true;
                    double dot = 0.0;
                    for (int d = 0; d < keyDim; d++)
                    {
                        dot += query[q, d] * key[k, d];
                    }

                    logits[k] = dot * scale;
                }

                if (!anyKept)
                {
                    throw new ArgumentException("Every query must keep at least one key.");
                }

                double[] row = ActivationFunctions.Softmax(logits);
                for (int k = 0; k < keyCount; k++)
                {
                    weights[q, k] = row[k];
                    for (int v = 0; v < valueDim; v++)
                    {
                        output[q, v] += row[k] * value[k, v];
                    }
                }
            }

            return new Result(weights, output);
        }
    }
}
