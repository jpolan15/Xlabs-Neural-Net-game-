using System;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// Level 1's "roll it down" game: real gradient descent on a one-weight hill, loss(x) = Curve × (x − Bottom)² + Floor.
    /// Each step is x ← x − η × dloss/dx with the step size the rider picks. A step size above 1 / (2 Curve) lands past
    /// the bottom; above 1 / Curve it climbs higher every step. The game is won when the ball is within
    /// <see cref="Tolerance"/> of the bottom. Stars come from how few steps it took.
    /// </summary>
    public sealed class DescentGame
    {
        public double Curve { get; }
        public double Bottom { get; }
        public double Floor { get; }
        public double Start { get; }
        public double Tolerance { get; }
        public double MinX { get; }
        public double MaxX { get; }

        /// <summary>Steps for three stars; twice this for two.</summary>
        public int Par { get; }

        public double X { get; private set; }
        public int Steps { get; private set; }

        public DescentGame(double curve = 0.5, double bottom = 2.0, double floor = 0.3, double start = -2.5,
            double tolerance = 0.15, double minX = -3.5, double maxX = 7.5, int par = 6)
        {
            if (curve <= 0) throw new ArgumentOutOfRangeException(nameof(curve));
            if (minX >= maxX || start < minX || start > maxX) throw new ArgumentOutOfRangeException(nameof(start));
            Curve = curve;
            Bottom = bottom;
            Floor = floor;
            Start = start;
            Tolerance = tolerance;
            MinX = minX;
            MaxX = maxX;
            Par = par;
            X = start;
        }

        public double Loss(double x) => Curve * (x - Bottom) * (x - Bottom) + Floor;
        public double Slope(double x) => 2.0 * Curve * (x - Bottom);
        public double Loss() => Loss(X);

        public bool Passed => System.Math.Abs(X - Bottom) <= Tolerance;

        /// <summary>Takes one step with step size η. Returns the new x, kept on the drawn hill.</summary>
        public double Step(double learningRate)
        {
            if (learningRate <= 0) throw new ArgumentOutOfRangeException(nameof(learningRate));
            X = System.Math.Max(MinX, System.Math.Min(MaxX, X - learningRate * Slope(X)));
            Steps++;
            return X;
        }

        /// <summary>True when a step of size η lands on the far side of the bottom.</summary>
        public static bool Overshoots(double before, double after, double bottom) => (before - bottom) * (after - bottom) < 0;

        public void Reset()
        {
            X = Start;
            Steps = 0;
        }

        /// <summary>3 stars at par or better, 2 within twice par, else 1.</summary>
        public int Stars => Steps <= Par ? 3 : Steps <= Par * 2 ? 2 : 1;
    }
}
