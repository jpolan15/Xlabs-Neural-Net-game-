using System;
using System.Collections;
using Convergence.Core.Puzzles;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// Region 6, a mini-game before the drop: "roll it down". A ball sits high on a loss hill on the dash. The rider
    /// shoots one of three step buttons (tiny, medium, giant); each shot is one real gradient-descent step of that size
    /// on Core's <see cref="DescentGame"/>. A giant step flies past the bottom, tiny steps crawl; reach the bottom in
    /// as few steps as you can. After <see cref="RideGame.giveUpAfter"/> (or an operator hurry) the game takes medium
    /// steps itself. Then the win line sends the pod over the rim of the real Valley.
    /// </summary>
    public sealed class DescentMoment : RideGame
    {
        [Tooltip("The step size (η) of each button: tiny, medium, giant.")]
        [SerializeField] private float[] stepSizes = { 0.12f, 0.7f, 1.85f };
        [SerializeField] private float stepSeconds = 0.55f;
        [SerializeField] private float autoStepSeconds = 1.1f;

        private readonly DescentGame _game = new DescentGame();
        private bool _live;
        private bool _hurry;
        private bool _byRider = true;
        private bool _saidOvershoot;
        private bool _saidSlow;
        private int _tinyInARow;
        private float _busyUntil;

        public DescentGame Game => _game;
        public bool IsLive => _live;
        public int ButtonCount => stepSizes.Length;
        public float StepSize(int button) => stepSizes[button];

        /// <summary>A step was taken: from x, to x, and the button (step size) used.</summary>
        public event Action<double, double, int> Stepped;

        public event Action<bool> LiveChanged;

        public override void Hurry() => _hurry = true;

        /// <summary>The laser hit step button b. Ignored while the ball is still rolling from the last step.</summary>
        public bool Press(int button, Vector3 where)
        {
            if (!_live || button < 0 || button >= stepSizes.Length || Time.time < _busyUntil) return false;
            Take(button, where, true);
            return true;
        }

        protected override IEnumerator Play()
        {
            _game.Reset();
            _hurry = false;
            _byRider = true;
            _saidOvershoot = false;
            _saidSlow = false;
            _tinyInARow = 0;
            _busyUntil = 0f;
            _live = true;
            LiveChanged?.Invoke(true);
            Show();
            float opened = Time.time;
            float nextAuto = 0f;
            while (!_game.Passed)
            {
                if ((_hurry || Time.time - opened > giveUpAfter) && Time.time >= nextAuto && Time.time >= _busyUntil)
                {
                    _byRider = false;
                    nextAuto = Time.time + (_hurry ? autoStepSeconds * 0.3f : autoStepSeconds);
                    Take(1, Vector3.zero, false);
                }

                yield return null;
            }

            // Let the last roll finish before the win beat.
            while (Time.time < _busyUntil) yield return null;
            _live = false;
            LiveChanged?.Invoke(false);
        }

        private void Take(int button, Vector3 where, bool byRider)
        {
            double before = _game.X;
            double lossBefore = _game.Loss();
            double after = _game.Step(stepSizes[button]);
            _busyUntil = Time.time + stepSeconds;
            Stepped?.Invoke(before, after, button);
            if (byRider && _game.Loss() < lossBefore) AddScore(Mathf.RoundToInt((float)(lossBefore - _game.Loss()) * 20f) + 5, where);

            if (byRider && DescentGame.Overshoots(before, after, _game.Bottom) && !_game.Passed && !_saidOvershoot)
            {
                _saidOvershoot = true;
                Director.Say(RideLine.DescentGameOvershoot);
            }

            _tinyInARow = button == 0 ? _tinyInARow + 1 : 0;
            if (byRider && _tinyInARow >= 3 && !_saidSlow)
            {
                _saidSlow = true;
                Director.Say(RideLine.DescentGameSlow);
            }

            Show();
        }

        private void Show()
        {
            double start = _game.Loss(_game.Start), floor = _game.Floor;
            Progress = Mathf.Clamp01((float)((start - _game.Loss()) / (start - floor)));
            SetGoal("Loss " + _game.Loss().ToString("0.00") + "   Steps " + _game.Steps + "   Par " + _game.Par);
        }

        protected override int StarsEarned() => _game.Stars;

        protected override bool PlayedItself => !_byRider;
    }
}
