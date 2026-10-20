using System;
using System.Collections;
using System.Collections.Generic;
using Convergence.Core.Puzzles;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// Region 2: the rider paints one neuron's 7×7 weights (tap: 0 → + → −) until it fires for every real "1" and for
    /// no real "0". Core's <see cref="WeightPaintPuzzle"/> decides; this only routes taps, raises what changed, says a
    /// reaction when the neuron gets a digit wrong, shows Core's hint after the rider goes quiet, and, so nobody gets
    /// stuck, starts following the hints itself (one visible move at a time) after a long timeout or an operator skip.
    /// </summary>
    public sealed class WeightPaintMoment : RideMoment
    {
        [SerializeField] private Level1DataSource source;
        [Tooltip("Seconds without a tap before Core's hint lights a cell.")]
        [SerializeField] private float hintAfterIdle = 8f;
        [Tooltip("Seconds after the puzzle opens when the ride starts following the hints itself.")]
        [SerializeField] private float autoSolveAfter = 75f;
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
            _lastTouch = Time.time;
            SetHint(-1, 0);
            Apply(cell, WeightPaintPuzzle.NextValue(_weights[cell]), true);
        }

        public override void Hurry() => _hurry = true;

        protected override IEnumerator Run()
        {
            Prepare();
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
                if (!_auto && (_hurry || now - opened > autoSolveAfter))
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
                            Apply(hint.Value.Cell, hint.Value.Value, false);
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
                            Director.Say(firedWrongly ? RideLine.NeuronLitForZero : RideLine.NeuronMissedOne);
                        }
                    }
                }

                yield return null;
            }

            _live = false;
            LiveChanged?.Invoke(false);
            SetHint(-1, 0);
            Director.Narrate(RideLine.NeuronSolved);
            Solved?.Invoke(SolvedByRider);
            yield return Director.WaitForSpeech();
        }

        private void Apply(int cell, int value, bool byRider)
        {
            _weights[cell] = value;
            _result = _puzzle.Evaluate(_weights);
            _reactPending = byRider;
            Changed?.Invoke(_result);
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
