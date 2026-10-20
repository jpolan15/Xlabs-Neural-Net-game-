using System;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>
    /// Rate functions for the explainers' motion, after 3Blue1Brown's manim (manim/utils/rate_functions.py): they map
    /// a tween's linear progress t (0 to 1) to how far along the move is. <see cref="Smooth"/> is the default for every
    /// move (a sigmoid-shaped smoothstep: slow out, slow in, never a snap). Pure math with no UnityEngine, so EditMode
    /// tests check it directly. Inputs outside 0 to 1 are clamped.
    /// </summary>
    public static class Rate
    {
        /// <summary>manim's default steepness for smooth.</summary>
        public const float Inflection = 10f;

        public static float Linear(float t) => Clamp01(t);

        /// <summary>manim's smooth: a sigmoid rescaled to run exactly from 0 to 1. Symmetric: Smooth(1 - t) = 1 - Smooth(t).</summary>
        public static float Smooth(float t) => Smooth(t, Inflection);

        public static float Smooth(float t, float inflection)
        {
            t = Clamp01(t);
            double error = Sigmoid(-inflection / 2.0);
            double v = (Sigmoid(inflection * (t - 0.5)) - error) / (1.0 - 2.0 * error);
            return Clamp01((float)v);
        }

        /// <summary>Starts slowly and arrives at full speed (the first half of smooth, stretched).</summary>
        public static float RushInto(float t) => 2f * Smooth(Clamp01(t) / 2f);

        /// <summary>Leaves at full speed and settles slowly (the second half of smooth, stretched).</summary>
        public static float RushFrom(float t) => 2f * Smooth(Clamp01(t) / 2f + 0.5f) - 1f;

        /// <summary>Goes there and comes back: 0 at both ends, 1 in the middle (Indicate, a flash).</summary>
        public static float ThereAndBack(float t)
        {
            t = Clamp01(t);
            return Smooth(t < 0.5f ? 2f * t : 2f * (1f - t));
        }

        /// <summary>Arrives at 80 % of the time and lingers there (manim's lingering: linear, squished into 0 to 0.8).</summary>
        public static float Lingering(float t) => Clamp01(Clamp01(t) / 0.8f);

        /// <summary>
        /// manim's LaggedStart for one item: with n items, each starting lagRatio of a sub-move after the previous, the
        /// progress of item i (0 to 1) when the whole group is at t (0 to 1). The last item ends exactly at t = 1.
        /// </summary>
        public static float Lagged(float t, int index, int count, float lagRatio)
        {
            if (count <= 1) return Clamp01(t);
            float sub = 1f / (1f + Math.Max(0f, lagRatio) * (count - 1));
            return Clamp01((Clamp01(t) - index * lagRatio * sub) / sub);
        }

        /// <summary>Named rate functions, for data written by the builder.</summary>
        public static Func<float, float> ByName(string name)
        {
            switch (name)
            {
                case "linear": return Linear;
                case "rush_into": return RushInto;
                case "rush_from": return RushFrom;
                case "there_and_back": return ThereAndBack;
                case "lingering": return Lingering;
                default: return Smooth;
            }
        }

        private static double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));

        private static float Clamp01(float t) => t < 0f ? 0f : t > 1f ? 1f : t;
    }
}
