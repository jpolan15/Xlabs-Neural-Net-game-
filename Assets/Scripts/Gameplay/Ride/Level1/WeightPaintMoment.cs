using System;
using System.Collections;
using System.Collections.Generic;
using Convergence.Core.Puzzles;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// Region 2, a mini-game: the rider paints one neuron's 7×7 weights, right hand green (+1), left hand red (−1), the
    /// same colour again clears, until it fires for every real "3" and for none of the other real digits. Core's
    /// <see cref="WeightPaintPuzzle"/> decides; this only routes strokes, raises what changed, scores each digit the
    /// neuron gets right, says a reaction when it gets one wrong, and shows Core's hint after the rider goes quiet. So
    /// nobody gets stuck, it starts following the hints itself (one visible move at a time) after
    /// <see cref="RideGame.giveUpAfter"/> or an operator hurry. Stars come from how few strokes it took.
    /// </summary>
    public sealed class WeightPaintMoment : RideGame
    {
        [SerializeField] private Level1DataSource source;
        [Tooltip("Seconds without a tap before Core's hint lights a cell.")]
        [SerializeField] private float hintAfterIdle = 8f;
        [SerializeField] private float autoStepSeconds = 0.9f;
        [Tooltip("A reaction line waits this long after the last tap, and comes at most this often.")]
        [SerializeField] private float reactAfter = 1.5f;
        [SerializeField] private float reactEvery = 9f;

        private WeightPaintPuzzle _puzzle;
        private readonly int[] _weights = new int[49];
        private WeightPaintResult _result;
        private bool _live;
        private bool _solved;
        private bool _hurry;
        private bool _auto;
        private float _lastTouch;
        private float _lastReact = -100f;
        private bool _reactPending;
        private int _hintCell = -1;
        private int _hintValue;
        private int _strokes;
        private int _bestCorrect;

        /// <summary>The puzzle (null before the moment first runs).</summary>
        public WeightPaintPuzzle Puzzle => _puzzle;

        /// <summary>The 49 weights as painted now, each −1, 0 or +1.</summary>
        public IReadOnlyList<int> Weights => _weights;

        /// <summary>Core's verdict on the current weights.</summary>
        public WeightPaintResult Result => _result;

        /// <summary>True while taps count.</summary>
        public bool IsLive => _live;

        public int HintCell => _hintCell;
        public int HintValue => _hintValue;

        /// <summary>True when the rider solved it; false when the ride finished it by following the hints.</summary>
        public bool SolvedByRider { get; private set; }

        /// <summary>The weights changed (by a tap or by the ride's own hint moves).</summary>
        public event Action<WeightPaintResult> Changed;

        /// <summary>The cell Core suggests changing and the value to give it; -1 clears it.</summary>
        public event Action<int, int> HintChanged;

        public event Action<bool> LiveChanged;

        /// <summary>
        /// A cell was painted: its index, the brush that did it (+1 green right hand, −1 red left hand), the value it now
        /// holds (−1, 0 or +1; 0 is a rub-out by the same colour) and whether the rider did it (false: the ride's own hint move).
        /// </summary>
        public event Action<int, int, int, bool> Stroked;

        /// <summary>Every digit is right. True when the rider did it.</summary>
        public event Action<bool> Solved;

        /// <summary>Prepares the puzzle so views can show it before the moment opens.</summary>
        public void Prepare()
        {
            if (_puzzle != null || source == null) return;
            _puzzle = source.Data.CreatePuzzle();
            Array.Clear(_weights, 0, _weights.Length);
            _result = _puzzle.Evaluate(_weights);
        }

        private void Start() => Prepare();

        /// <summary>A tap on a cell: the next value in 0 → + → −. Ignored unless the puzzle is live.</summary>
        public void Cycle(int cell)
        {
            if (!_live || _auto || cell < 0 || cell >= _weights.Length) return;
            int next = WeightPaintPuzzle.NextValue(_weights[cell]);
            Stroke(cell, next, next != 0 ? next : _weights[cell]);
        }

        /// <summary>The real digit test digit d is (3, or one of the others).</summary>
        public int DigitLabel(int d) => source != null ? source.Data.File.puzzle.digits[d].label : -1;

        /// <summary>A brush stroke on a cell: +1 green (right hand), −1 red (left hand); the same colour again clears it.</summary>
        public bool Press(int cell, int brush)
        {
            if (!_live || _auto || cell < 0 || cell >= _weights.Length) return false;
            Stroke(cell, WeightPaintPuzzle.PaintValue(_weights[cell], brush), brush);
            return true;
        }

        /// <summary>
        /// A held laser swept onto a cell (v6): it takes the brush's colour, and a cell already that colour stays. So a
        /// sweep paints a whole stroke; only a fresh press on a cell of the same colour clears it.
        /// </summary>
        public bool Spray(int cell, int brush)
        {
            if (!_live || _auto || cell < 0 || cell >= _weights.Length || _weights[cell] == brush) return false;
            Stroke(cell, brush, brush);
            return true;
        }

        private void Stroke(int cell, int value, int brush)
        {
            _lastTouch = Time.time;
            _strokes++;
            SetHint(-1, 0);
            Apply(cell, value, true, brush);
        }

        protected override int StarsEarned()
        {
            int best = source != null ? Mathf.Max(1, source.Data.File.puzzle.hintMovesFromEmpty) : 4;
            // A sweep paints a cell per stroke, so a clean solution is a few sweeps of cells, not a few clicks.
            return _strokes <= best * 4 ? 3 : _strokes <= best * 8 ? 2 : 1;
        }

        protected override bool PlayedItself => !SolvedByRider;

        public override void Hurry() => _hurry = true;

        protected override IEnumerator Play()
        {
            Prepare();
            _strokes = 0;
            _bestCorrect = _result != null ? _result.CorrectCount : 0;
            Progress = _result != null ? (float)_result.CorrectCount / Mathf.Max(1, _puzzle.DigitCount) : 0f;
            _solved = false;
            _hurry = false;
            _auto = false;
            float opened = Time.time;
            _lastTouch = opened;
            _live = true;
            LiveChanged?.Invoke(true);
            Changed?.Invoke(_result);

            float nextAuto = 0f;
            while (!_solved)
            {
                float now = Time.time;
                if (!_auto && (_hurry || now - opened > giveUpAfter))
                {
                    _auto = true;
                    nextAuto = now;
                }

                if (_auto)
                {
                    if (now >= nextAuto)
                    {
                        nextAuto = now + (_hurry ? autoStepSeconds * 0.3f : autoStepSeconds);
                        WeightPaintHint? hint = _puzzle.Hint(_weights);
                        if (hint.HasValue)
                        {
                            SetHint(hint.Value.Cell, hint.Value.Value);
                            Apply(hint.Value.Cell, hint.Value.Value, false, hint.Value.Value != 0 ? hint.Value.Value : _weights[hint.Value.Cell]);
                        }
                    }
                }
                else
                {
                    if (_hintCell < 0 && now - _lastTouch > hintAfterIdle)
                    {
                        WeightPaintHint? hint = _puzzle.Hint(_weights);
                        if (hint.HasValue) SetHint(hint.Value.Cell, hint.Value.Value);
                    }

                    if (_reactPending && now - _lastTouch > reactAfter && now - _lastReact > reactEvery && !_result.Passed)
                    {
                        _reactPending = false;
                        _lastReact = now;
                        if (_result.FirstWrong(out bool firedWrongly) >= 0)
                        {
                            Director.Say(firedWrongly ? RideLine.NeuronLitForOther : RideLine.NeuronMissedThree);
                        }
                    }
                }

                yield return null;
            }

            _live = false;
            LiveChanged?.Invoke(false);
            SetHint(-1, 0);
            Solved?.Invoke(SolvedByRider);
        }

        private void Apply(int cell, int value, bool byRider, int brush)
        {
            _weights[cell] = value;
            _result = _puzzle.Evaluate(_weights);
            _reactPending = byRider;
            Progress = (float)_result.CorrectCount / Mathf.Max(1, _puzzle.DigitCount);
            if (_result.CorrectCount > _bestCorrect)
            {
                if (byRider) AddScore(50 * (_result.CorrectCount - _bestCorrect), Vector3.zero);
                _bestCorrect = _result.CorrectCount;
            }

            Stroked?.Invoke(cell, brush, value, byRider);
            Changed?.Invoke(_result);
            Refresh();
            if (_result.Passed && !_solved)
            {
                _solved = true;
                SolvedByRider = byRider;
            }
        }

        private void SetHint(int cell, int value)
        {
            if (cell == _hintCell && value == _hintValue) return;
            _hintCell = cell;
            _hintValue = value;
            HintChanged?.Invoke(cell, value);
        }
    }
}
