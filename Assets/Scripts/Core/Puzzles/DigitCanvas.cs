using System;
using System.Collections.Generic;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// The rider's drawing, turned into the 49 numbers the network reads the way MNIST itself was made (LeCun et al.):
    /// the strokes are scaled to fit a 20×20 box (keeping their shape), drawn with a soft round pen into a 28×28 image,
    /// moved so their centre of mass sits at the image's centre, and then averaged in 4×4 blocks to 7×7 (exactly the
    /// bake's <c>pool7</c>). So a three drawn small, big, or off to one side becomes the same input, and the network
    /// that learned from MNIST can read it. Canvas coordinates run 0 to 1, x to the right and y down (row 0 at the top).
    /// It never judges what was drawn: any drawing is a valid input.
    /// </summary>
    public sealed class DigitCanvas
    {
        public const int FullSize = 28;
        public const int Size = 7;
        public const int PixelCount = Size * Size;
        private const double Box = 20.0;
        private const int Block = FullSize / Size;

        private readonly List<List<Point>> _strokes = new List<List<Point>>();
        private readonly double[] _full = new double[FullSize * FullSize];

        /// <param name="penRadius">The pen's radius in 28×28 pixels (MNIST's strokes are about 3 pixels wide).</param>
        /// <param name="minSpacing">Points closer than this to the last one (canvas units) are skipped.</param>
        /// <param name="inkNeeded">Total stroke length (canvas units) before the drawing counts as a digit.</param>
        public DigitCanvas(double penRadius = 1.6, double minSpacing = 0.006, double inkNeeded = 0.5)
        {
            if (penRadius <= 0 || penRadius >= Box / 2) throw new ArgumentOutOfRangeException(nameof(penRadius));
            PenRadius = penRadius;
            MinSpacing = minSpacing;
            InkNeeded = inkNeeded;
        }

        public double PenRadius { get; }
        public double MinSpacing { get; }
        public double InkNeeded { get; }

        public int StrokeCount => _strokes.Count;

        public int PointCount
        {
            get
            {
                int n = 0;
                foreach (List<Point> s in _strokes) n += s.Count;
                return n;
            }
        }

        /// <summary>Total length of every stroke, in canvas units.</summary>
        public double InkLength { get; private set; }

        /// <summary>True once there is enough ink to read.</summary>
        public bool HasEnoughInk => InkLength >= InkNeeded;

        /// <summary>Starts a new stroke at a canvas point (clamped to the canvas).</summary>
        public void BeginStroke(double x, double y)
        {
            _strokes.Add(new List<Point> { Clamp(x, y) });
        }

        /// <summary>Continues the last stroke. Returns true when the point was kept.</summary>
        public bool AddPoint(double x, double y)
        {
            if (_strokes.Count == 0)
            {
                BeginStroke(x, y);
                return true;
            }

            List<Point> s = _strokes[_strokes.Count - 1];
            Point p = Clamp(x, y);
            Point last = s[s.Count - 1];
            double step = System.Math.Sqrt((p.X - last.X) * (p.X - last.X) + (p.Y - last.Y) * (p.Y - last.Y));
            if (step < MinSpacing) return false;
            s.Add(p);
            InkLength += step;
            return true;
        }

        public void Clear()
        {
            _strokes.Clear();
            InkLength = 0.0;
        }

        /// <summary>The points of stroke s (canvas units), for a view to draw the ink.</summary>
        public IReadOnlyList<Point> Stroke(int s) => _strokes[s];

        /// <summary>The normalized 28×28 image (row-major, row 0 at the top, 0 = paper, 1 = ink). All zero when empty.</summary>
        public double[] ToPixels28()
        {
            var image = new double[FullSize * FullSize];
            Render(image);
            return image;
        }

        /// <summary>The 49 numbers the network reads: <see cref="ToPixels28"/> averaged in 4×4 blocks.</summary>
        public double[] ToPixels7()
        {
            Render(_full);
            var pixels = new double[PixelCount];
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    double sum = 0.0;
                    for (int y = 0; y < Block; y++)
                    {
                        for (int x = 0; x < Block; x++) sum += _full[(r * Block + y) * FullSize + c * Block + x];
                    }

                    pixels[r * Size + c] = sum / (Block * Block);
                }
            }

            return pixels;
        }

        private void Render(double[] image)
        {
            Array.Clear(image, 0, image.Length);
            if (_strokes.Count == 0) return;

            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (List<Point> s in _strokes)
            {
                foreach (Point p in s)
                {
                    minX = System.Math.Min(minX, p.X);
                    maxX = System.Math.Max(maxX, p.X);
                    minY = System.Math.Min(minY, p.Y);
                    maxY = System.Math.Max(maxY, p.Y);
                }
            }

            // Fit the strokes (plus the pen) into MNIST's 20×20 box, keeping their shape, centred for a first pass.
            double size = System.Math.Max(System.Math.Max(maxX - minX, maxY - minY), 1e-3);
            double scale = (Box - 2.0 * PenRadius) / size;
            double cx = (minX + maxX) * 0.5, cy = (minY + maxY) * 0.5;
            double ox = FullSize * 0.5, oy = FullSize * 0.5;
            Draw(image, scale, cx, cy, ox, oy);

            // Then move the centre of mass to the image's centre, as MNIST did, and draw again.
            double mass = 0.0, mx = 0.0, my = 0.0;
            for (int y = 0; y < FullSize; y++)
            {
                for (int x = 0; x < FullSize; x++)
                {
                    double v = image[y * FullSize + x];
                    mass += v;
                    mx += v * (x + 0.5);
                    my += v * (y + 0.5);
                }
            }

            if (mass <= 0.0) return;
            ox += FullSize * 0.5 - mx / mass;
            oy += FullSize * 0.5 - my / mass;
            Array.Clear(image, 0, image.Length);
            Draw(image, scale, cx, cy, ox, oy);
        }

        private void Draw(double[] image, double scale, double cx, double cy, double ox, double oy)
        {
            foreach (List<Point> s in _strokes)
            {
                for (int i = 0; i < s.Count; i++)
                {
                    Point a = s[System.Math.Max(0, i - 1)];
                    Point b = s[i];
                    if (i == 0 && s.Count > 1) continue;
                    Segment(image,
                        (a.X - cx) * scale + ox, (a.Y - cy) * scale + oy,
                        (b.X - cx) * scale + ox, (b.Y - cy) * scale + oy);
                }
            }
        }

        /// <summary>Soft round pen along one segment: a pixel's ink is 1 inside the pen, fading to 0 over one pixel.</summary>
        private void Segment(double[] image, double ax, double ay, double bx, double by)
        {
            double reach = PenRadius + 0.5;
            int x0 = System.Math.Max(0, (int)System.Math.Floor(System.Math.Min(ax, bx) - reach));
            int x1 = System.Math.Min(FullSize - 1, (int)System.Math.Ceiling(System.Math.Max(ax, bx) + reach));
            int y0 = System.Math.Max(0, (int)System.Math.Floor(System.Math.Min(ay, by) - reach));
            int y1 = System.Math.Min(FullSize - 1, (int)System.Math.Ceiling(System.Math.Max(ay, by) + reach));
            double dx = bx - ax, dy = by - ay;
            double length2 = dx * dx + dy * dy;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    double px = x + 0.5, py = y + 0.5;
                    double t = length2 > 0.0 ? ((px - ax) * dx + (py - ay) * dy) / length2 : 0.0;
                    t = t < 0.0 ? 0.0 : t > 1.0 ? 1.0 : t;
                    double ex = px - (ax + t * dx), ey = py - (ay + t * dy);
                    double ink = reach - System.Math.Sqrt(ex * ex + ey * ey);
                    if (ink <= 0.0) continue;
                    if (ink > 1.0) ink = 1.0;
                    int at = y * FullSize + x;
                    if (ink > image[at]) image[at] = ink;
                }
            }
        }

        private static Point Clamp(double x, double y) => new Point(x < 0 ? 0 : x > 1 ? 1 : x, y < 0 ? 0 : y > 1 ? 1 : y);

        /// <summary>A point on the canvas, 0 to 1, y down.</summary>
        public readonly struct Point
        {
            public readonly double X;
            public readonly double Y;

            public Point(double x, double y)
            {
                X = x;
                Y = y;
            }
        }
    }
}
