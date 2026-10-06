using System.Collections.Generic;
using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// Uniform Catmull-Rom path through control points, sampled into an arc-length table so the pod can ride it
    /// at a chosen speed. Same curve family as the ADR-002 cables. No package involved.
    /// </summary>
    public sealed class RidePath
    {
        private readonly Vector3[] _ext;
        private readonly float[] _cumulative;
        private readonly int _perSegment;
        private readonly int _segments;

        /// <summary>Total arc length in meters.</summary>
        public float Length => _cumulative[_cumulative.Length - 1];

        /// <summary>Creates a path through at least two points.</summary>
        public RidePath(IReadOnlyList<Vector3> points, int samplesPerSegment = 24)
        {
            if (points == null || points.Count < 2) throw new System.ArgumentException("A path needs two points.", nameof(points));
            _perSegment = Mathf.Max(2, samplesPerSegment);
            _segments = points.Count - 1;

            _ext = new Vector3[points.Count + 2];
            for (int i = 0; i < points.Count; i++) _ext[i + 1] = points[i];
            _ext[0] = 2f * points[0] - points[1];
            _ext[_ext.Length - 1] = 2f * points[points.Count - 1] - points[points.Count - 2];

            _cumulative = new float[_segments * _perSegment + 1];
            Vector3 previous = points[0];
            for (int i = 1; i < _cumulative.Length; i++)
            {
                SegmentAndT(i, out int seg, out float t);
                Vector3 p = Sample(seg, t);
                _cumulative[i] = _cumulative[i - 1] + Vector3.Distance(previous, p);
                previous = p;
            }
        }

        /// <summary>Arc distance at control point <paramref name="index"/>.</summary>
        public float DistanceAtPoint(int index)
        {
            return _cumulative[Mathf.Clamp(index, 0, _segments) * _perSegment];
        }

        /// <summary>World position at an arc distance, clamped to the path.</summary>
        public Vector3 Position(float distance)
        {
            Locate(distance, out int seg, out float t);
            return Sample(seg, t);
        }

        /// <summary>Unit direction of travel at an arc distance.</summary>
        public Vector3 Heading(float distance)
        {
            Locate(distance, out int seg, out float t);
            Vector3 d = Derivative(seg, t);
            return d.sqrMagnitude < 1e-8f ? Vector3.forward : d.normalized;
        }

        private void Locate(float distance, out int seg, out float t)
        {
            float d = Mathf.Clamp(distance, 0f, Length);
            int lo = 0;
            int hi = _cumulative.Length - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (_cumulative[mid] <= d) lo = mid; else hi = mid;
            }

            float span = _cumulative[hi] - _cumulative[lo];
            float f = span < 1e-6f ? 0f : (d - _cumulative[lo]) / span;
            float global = (lo + f) / _perSegment;
            seg = Mathf.Min(_segments - 1, Mathf.FloorToInt(global));
            t = Mathf.Clamp01(global - seg);
        }

        private void SegmentAndT(int sampleIndex, out int seg, out float t)
        {
            seg = Mathf.Min(_segments - 1, sampleIndex / _perSegment);
            t = (sampleIndex - seg * _perSegment) / (float)_perSegment;
        }

        private Vector3 Sample(int seg, float t)
        {
            Vector3 p0 = _ext[seg], p1 = _ext[seg + 1], p2 = _ext[seg + 2], p3 = _ext[seg + 3];
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private Vector3 Derivative(int seg, float t)
        {
            Vector3 p0 = _ext[seg], p1 = _ext[seg + 1], p2 = _ext[seg + 2], p3 = _ext[seg + 3];
            float t2 = t * t;
            return 0.5f * ((-p0 + p2) + 2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t + 3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2);
        }
    }
}
