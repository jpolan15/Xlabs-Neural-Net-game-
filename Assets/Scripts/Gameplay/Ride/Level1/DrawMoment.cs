using System;
using System.Collections;
using Convergence.Core.Puzzles;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// Region 1, a mini-game: "draw the 3". The rider inks the dash's 7×7 grid over a faint outline of the hero 3 (the
    /// real handwritten digit, shrunk to 7×7): the right hand inks, the left rubs out. Core's <see cref="DrawPuzzle"/>
    /// decides when it looks enough like the 3. Each outline cell inked scores; Core's hint lights a cell after the
    /// rider goes quiet, and after <see cref="RideGame.giveUpAfter"/> (or an operator hurry) the game inks the rest
    /// itself, one cell at a time.
    /// </summary>
    public sealed class DrawMoment : RideGame
    {
        [SerializeField] private Level1DataSource source;
        [SerializeField] private float hintAfterIdle = 7f;
        [SerializeField] private float autoStepSeconds = 0.5f;

        private DrawPuzzle _puzzle;
        private bool _live;
        private bool _hurry;
        private bool _auto;
        private bool _byRider = true;
        private bool _saidHint;
        private int _strokes;
        private float _lastTouch;
        private int _hintCell = -1;

        public DrawPuzzle Puzzle => _puzzle;
        public bool IsLive => _live;
        public int HintCell => _hintCell;

        /// <summary>A cell changed (or everything, with -1).</summary>
        public event Action<int> CellChanged;

        public event Action<int> HintChanged;
        public event Action<bool> LiveChanged;

        public void Prepare()
        {
            if (_puzzle != null || source == null) return;
            _puzzle = new DrawPuzzle(source.Data.File.hero.pixels7);
        }

        private void Start() => Prepare();

        public override void Hurry() => _hurry = true;

        /// <summary>A press on a cell: the right hand inks, the left rubs out. Returns true when it changed the drawing.</summary>
        public bool Press(int cell, int hand)
        {
            if (!_live || _auto || _puzzle == null) return false;
            _lastTouch = Time.time;
            bool ink = hand != RideHand.Left;
            if (!_puzzle.Set(cell, ink)) return false;
            _strokes++;
            if (ink && _puzzle.IsTarget(cell)) AddScore(10, Vector3.zero);
            SetHint(-1);
            Changed(cell);
            return true;
        }

        protected override IEnumerator Play()
        {
            Prepare();
            _puzzle.Clear();
            _strokes = 0;
            _hurry = false;
            _auto = false;
            _byRider = true;
            _saidHint = false;
            SetHint(-1);
            _live = true;
            LiveChanged?.Invoke(true);
            Changed(-1);
            float opened = Time.time;
            _lastTouch = opened;
            float nextAuto = 0f;
            while (!_puzzle.Passed)
            {
                float now = Time.time;
                if (!_auto && (_hurry || now - opened > giveUpAfter))
                {
                    _auto = true;
                    _byRider = false;
                }

                if (_auto)
                {
                    if (now >= nextAuto)
                    {
                        nextAuto = now + (_hurry ? autoStepSeconds * 0.3f : autoStepSeconds);
                        int cell = _puzzle.Hint(out bool ink);
                        if (cell >= 0)
                        {
                            _puzzle.Set(cell, ink);
                            SetHint(cell);
                            Changed(cell);
                        }
                    }
                }
                else if (_hintCell < 0 && now - _lastTouch > hintAfterIdle)
                {
                    SetHint(_puzzle.Hint(out _));
                    if (!_saidHint)
                    {
                        _saidHint = true;
                        Director.Say(RideLine.DrawHint);
                    }
                }

                yield return null;
            }

            _live = false;
            SetHint(-1);
            LiveChanged?.Invoke(false);
        }

        protected override int StarsEarned()
        {
            int needed = _puzzle.CoverCount;
            return _strokes <= needed + 3 ? 3 : _strokes <= needed * 2 ? 2 : 1;
        }

        protected override bool PlayedItself => !_byRider;

        private void Changed(int cell)
        {
            Progress = (float)_puzzle.Progress;
            SetGoal("Inked " + _puzzle.Covered + " of " + _puzzle.TargetCount + " squares (" + _puzzle.CoverCount + " to win)" + (_puzzle.Stray > _puzzle.StrayAllowed ? "  (rub out the extra ink!)" : ""));
            CellChanged?.Invoke(cell);
        }

        private void SetHint(int cell)
        {
            if (cell == _hintCell) return;
            _hintCell = cell;
            HintChanged?.Invoke(cell);
        }
    }
}
