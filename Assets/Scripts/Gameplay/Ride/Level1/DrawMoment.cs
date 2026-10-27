using System;
using System.Collections;
using Convergence.Core.Puzzles;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// Region 1, a mini-game (v6, ADR-020): "draw your 3". The rider holds the trigger and draws on the canvas with
    /// either hand; Core's <see cref="DigitCanvas"/> turns the strokes into the 49 numbers the network reads, live, the
    /// way MNIST was made. CLEAR wipes it; DONE keeps it. The drawing is never judged: it becomes the rider's digit in
    /// <see cref="Level1Session"/>, and every later stop uses it. If the rider never draws enough ink (an operator
    /// hurry, or <see cref="RideGame.giveUpAfter"/>), the ride keeps the bake's real hero 3 instead.
    /// </summary>
    public sealed class DrawMoment : RideGame, ICanvasGame, IButtonGame
    {
        public const int ButtonDone = 0;
        public const int ButtonClear = 1;

        [SerializeField] private Level1Session session;
        [Tooltip("Seconds without a stroke, once there is enough ink, before Pixel suggests DONE.")]
        [SerializeField] private float nudgeAfterIdle = 5f;
        [Tooltip("Seconds without a stroke, once there is enough ink, before the drawing is kept as it is.")]
        [SerializeField] private float keepAfterIdle = 14f;
        [SerializeField] private float pixelsEvery = 0.08f;

        private DigitCanvas _canvas;
        private bool _live;
        private bool _hurry;
        private bool _done;
        private bool _saidNudge;
        private bool _saidBigger;
        private int _strokingHand = -1;
        private float _lastInk;
        private float _nextPixels;
        private bool _pixelsDirty;
        private double[] _pixels = new double[DigitCanvas.PixelCount];

        public DigitCanvas Canvas => _canvas ?? (_canvas = new DigitCanvas());

        public bool IsLive => _live;

        /// <summary>The 49 numbers of the drawing as it is now.</summary>
        public double[] Pixels => _pixels;

        /// <summary>True when the rider kept their own drawing (false: the ride used the hero 3).</summary>
        public bool KeptRidersDrawing { get; private set; }

        /// <summary>The strokes changed (a new point, a new stroke, a clear).</summary>
        public event Action InkChanged;

        /// <summary>The 49 numbers changed.</summary>
        public event Action<double[]> PixelsChanged;

        public event Action<bool> LiveChanged;

        /// <summary>The drawing was kept (true: the rider's; false: the hero 3 stands in).</summary>
        public event Action<bool> Kept;

        public override void Hurry() => _hurry = true;

        public bool StrokeBegin(int hand, Vector2 point, bool pressed)
        {
            if (!_live || _done) return false;
            Canvas.BeginStroke(point.x, point.y);
            _strokingHand = hand;
            Touched();
            return true;
        }

        public void StrokeMove(int hand, Vector2 point)
        {
            if (!_live || _done || hand != _strokingHand) return;
            if (Canvas.AddPoint(point.x, point.y)) Touched();
        }

        public void StrokeEnd(int hand)
        {
            if (hand == _strokingHand) _strokingHand = -1;
        }

        public bool Press(int button, int hand)
        {
            if (!_live || _done) return false;
            if (button == ButtonClear)
            {
                if (Canvas.StrokeCount == 0) return false;
                Canvas.Clear();
                _saidNudge = false;
                Touched();
                return true;
            }

            if (button != ButtonDone) return false;
            if (!Canvas.HasEnoughInk)
            {
                if (!_saidBigger)
                {
                    _saidBigger = true;
                    Director.Say(RideLine.DrawBigger);
                }

                return false;
            }

            _done = true;
            return true;
        }

        public void Release(int button, int hand)
        {
        }

        protected override int StarsEarned() => 3;

        protected override bool PlayedItself => !KeptRidersDrawing;

        protected override IEnumerator Play()
        {
            Canvas.Clear();
            Array.Clear(_pixels, 0, _pixels.Length);
            _hurry = false;
            _done = false;
            _saidNudge = false;
            _saidBigger = false;
            _strokingHand = -1;
            KeptRidersDrawing = false;
            _live = true;
            LiveChanged?.Invoke(true);
            InkChanged?.Invoke();
            PixelsChanged?.Invoke(_pixels);
            float opened = Time.time;
            _lastInk = opened;
            SetGoal("Hold the trigger and draw a 3");

            while (!_done)
            {
                float now = Time.time;
                if (_pixelsDirty && now >= _nextPixels) UpdatePixels();
                bool enough = Canvas.HasEnoughInk;
                Progress = Mathf.Clamp01((float)(Canvas.InkLength / Canvas.InkNeeded)) * 0.9f;
                if (enough && _strokingHand < 0)
                {
                    float idle = now - _lastInk;
                    if (!_saidNudge && idle > nudgeAfterIdle)
                    {
                        _saidNudge = true;
                        SetGoal("Shoot DONE when it looks like a 3");
                        Director.Say(RideLine.DrawDoneNudge);
                    }

                    if (idle > keepAfterIdle) _done = true;
                }

                if (_hurry || now - opened > giveUpAfter) break;
                yield return null;
            }

            UpdatePixels();
            _live = false;
            LiveChanged?.Invoke(false);
            KeptRidersDrawing = Canvas.HasEnoughInk;
            if (KeptRidersDrawing) session.SetDigit(_pixels, true);
            else
            {
                Array.Copy(session.Data.File.hero.pixels7, _pixels, _pixels.Length);
                session.SetDigit(_pixels, false);
                PixelsChanged?.Invoke(_pixels);
            }

            Kept?.Invoke(KeptRidersDrawing);
            SetGoal(KeptRidersDrawing ? "Your 3, as 49 numbers" : "A real handwritten 3, as 49 numbers");
        }

        private void Touched()
        {
            _lastInk = Time.time;
            _pixelsDirty = true;
            InkChanged?.Invoke();
        }

        private void UpdatePixels()
        {
            _pixelsDirty = false;
            _nextPixels = Time.time + pixelsEvery;
            _pixels = Canvas.ToPixels7();
            PixelsChanged?.Invoke(_pixels);
            Refresh();
        }
    }
}
