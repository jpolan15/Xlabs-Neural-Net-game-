using System;
using System.Collections.Generic;

namespace Convergence.Core.Puzzles
{
    /// <summary>
    /// Level 1's "draw the 3" game: the rider inks cells of a grid over a faint outline of a real handwritten digit.
    /// The outline is every cell whose brightness reaches <see cref="InkLevel"/>. The drawing passes when it covers
    /// at least <see cref="CoverNeeded"/> of the outline and has no more than <see cref="StrayAllowed"/> inked cells
    /// off it, so a scribble over the whole grid never passes.
    /// </summary>
    public sealed class DrawPuzzle
    {
        private readonly bool[] _target;
        private readonly bool[] _ink;

        public int CellCount => _target.Length;

        /// <summary>Cells on the outline.</summary>
        public int TargetCount { get; }

        public double InkLevel { get; }
        public double CoverNeeded { get; }
        public int StrayAllowed { get; }

        public DrawPuzzle(IReadOnlyList<double> pixels, double inkLevel = 0.3, double coverNeeded = 0.75, int strayAllowed = 3)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (pixels.Count == 0) throw new ArgumentException("The digit needs pixels.", nameof(pixels));
            _target = new bool[pixels.Count];
            _ink = new bool[pixels.Count];
            for (int i = 0; i < pixels.Count; i++)
            {
                _target[i] = pixels[i] >= inkLevel;
                if (_target[i]) TargetCount++;
            }

            if (TargetCount == 0) throw new ArgumentException("No cell reaches the ink level.", nameof(pixels));
            InkLevel = inkLevel;
            CoverNeeded = coverNeeded;
            StrayAllowed = strayAllowed;
        }

        public bool IsTarget(int cell) => _target[cell];
        public bool IsInked(int cell) => _ink[cell];

        /// <summary>Inks (true) or erases (false) a cell. Returns true when that changed it.</summary>
        public bool Set(int cell, bool ink)
        {
            if (cell < 0 || cell >= _ink.Length || _ink[cell] == ink) return false;
            _ink[cell] = ink;
            return true;
        }

        public void Clear() => Array.Clear(_ink, 0, _ink.Length);

        /// <summary>Outline cells inked.</summary>
        public int Covered
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _ink.Length; i++) if (_ink[i] && _target[i]) n++;
                return n;
            }
        }

        /// <summary>Inked cells off the outline.</summary>
        public int Stray
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _ink.Length; i++) if (_ink[i] && !_target[i]) n++;
                return n;
            }
        }

        /// <summary>Outline cells needed to pass.</summary>
        public int CoverCount => (int)System.Math.Ceiling(TargetCount * CoverNeeded - 1e-9);

        /// <summary>0 to 1 toward passing: covered cells over the cells needed, held back while there is too much stray ink.</summary>
        public double Progress => System.Math.Min(1.0, (double)Covered / CoverCount) * (Stray > StrayAllowed ? 0.9 : 1.0);

        public bool Passed => Covered >= CoverCount && Stray <= StrayAllowed;

        /// <summary>The next cell to change: erase the first stray cell if there are too many, else ink the first missing one. -1 when passed.</summary>
        public int Hint(out bool ink)
        {
            ink = true;
            if (Passed) return -1;
            if (Stray > StrayAllowed)
            {
                ink = false;
                for (int i = 0; i < _ink.Length; i++) if (_ink[i] && !_target[i]) return i;
            }

            for (int i = 0; i < _ink.Length; i++) if (_target[i] && !_ink[i]) return i;
            return -1;
        }
    }
}
