using System;
using System.Collections;
using Convergence.Core.Puzzles;
using Convergence.Core.Training;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// The bonus round (v6, ADR-020): "stump Pixel". Pixel asks for a digit; the rider draws it on the canvas and
    /// Pixel reads it live, stroke by stroke, with the network the rider trained (Core's forward pass, the ten
    /// probabilities shown as bars). DONE locks the guess in: a point when Pixel reads it right. A few prompts, then
    /// the win beat. It never decides whether a drawing is "good": only whether the trained network read it as the
    /// digit asked for.
    /// </summary>
    public sealed class StumpMoment : RideGame, ICanvasGame, IButtonGame
    {
        public const int ButtonDone = 0;
        public const int ButtonClear = 1;

        [SerializeField] private Level1Session session;
        [SerializeField] private int prompts = 4;
        [SerializeField] private int seed = 17;
        [SerializeField] private float readEvery = 0.08f;
        [SerializeField] private float revealSeconds = 1.4f;

        private readonly DigitCanvas _canvas = new DigitCanvas();
        private readonly double[] _probabilities = new double[DigitNetwork.Outputs];
        private readonly double[] _hidden = new double[DigitNetwork.Hidden];
        private int[] _order;
        private bool _live;
        private bool _drawing;
        private bool _hurry;
        private bool _done;
        private bool _dirty;
        private float _nextRead;
        private int _strokingHand = -1;
        private bool _saidWrong;

        public DigitCanvas Canvas => _canvas;

        /// <summary>True while the rider can draw.</summary>
        public bool IsLive => _live && _drawing;

        /// <summary>The digit Pixel asks for now (-1 between prompts).</summary>
        public int Prompt { get; private set; } = -1;

        /// <summary>Which prompt this is, from 0.</summary>
        public int PromptIndex { get; private set; }

        public int PromptCount => prompts;

        /// <summary>Pixel's live guess at the drawing (-1 when the canvas is empty).</summary>
        public int Guess { get; private set; } = -1;

        /// <summary>The ten probabilities of the live guess.</summary>
        public double[] Probabilities => _probabilities;

        /// <summary>Prompts Pixel read right so far.</summary>
        public int Right { get; private set; }

        public event Action InkChanged;

        /// <summary>The live guess changed (or the canvas cleared: guess -1).</summary>
        public event Action<int> Guessed;

        /// <summary>A new prompt (the digit to draw).</summary>
        public event Action<int> Prompted;

        /// <summary>DONE was shot: the digit asked for, Pixel's answer, right or not.</summary>
        public event Action<int, int, bool> Judged;

        public event Action<bool> LiveChanged;

        public bool StrokeBegin(int hand, Vector2 point, bool pressed)
        {
            if (!IsLive) return false;
            _canvas.BeginStroke(point.x, point.y);
            _strokingHand = hand;
            Touched();
            return true;
        }

        public void StrokeMove(int hand, Vector2 point)
        {
            if (!IsLive || hand != _strokingHand) return;
            if (_canvas.AddPoint(point.x, point.y)) Touched();
        }

        public void StrokeEnd(int hand)
        {
            if (hand == _strokingHand) _strokingHand = -1;
        }

        public bool Press(int button, int hand)
        {
            if (!IsLive) return false;
            if (button == ButtonClear)
            {
                if (_canvas.StrokeCount == 0) return false;
                _canvas.Clear();
                Touched();
                return true;
            }

            if (button != ButtonDone || !_canvas.HasEnoughInk) return false;
            _done = true;
            return true;
        }

        public void Release(int button, int hand)
        {
        }

        public override void Hurry() => _hurry = true;

        protected override int StarsEarned() => Right >= prompts - 1 ? 3 : Right >= prompts / 2 ? 2 : 1;

        protected override bool PlayedItself => false;

        protected override IEnumerator Play()
        {
            var rng = new System.Random(seed);
            _order = new[] { 0, 1, 2, 4, 5, 6, 7, 8, 9 };
            for (int i = _order.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (_order[i], _order[j]) = (_order[j], _order[i]);
            }

            Right = 0;
            _hurry = false;
            _saidWrong = false;
            _live = true;
            LiveChanged?.Invoke(true);
            Director.Narrate(RideLine.StumpYourTurn);
            float opened = Time.time;

            for (PromptIndex = 0; PromptIndex < prompts && !_hurry && Time.time - opened < giveUpAfter; PromptIndex++)
            {
                Prompt = _order[PromptIndex % _order.Length];
                _canvas.Clear();
                Guess = -1;
                Array.Clear(_probabilities, 0, _probabilities.Length);
                _done = false;
                _drawing = true;
                Prompted?.Invoke(Prompt);
                Guessed?.Invoke(-1);
                InkChanged?.Invoke();
                SetGoal("Draw a " + Prompt + "   (" + (PromptIndex + 1) + " of " + prompts + ")    Pixel read " + Right);
                Progress = (float)PromptIndex / prompts;

                while (!_done && !_hurry && Time.time - opened < giveUpAfter)
                {
                    if (_dirty && Time.time >= _nextRead) Read();
                    yield return null;
                }

                _drawing = false;
                if (!_done) break;
                Read();
                bool right = Guess == Prompt;
                if (right)
                {
                    Right++;
                    AddScore(50, Vector3.zero);
                    Director.Say(RideLine.StumpRight);
                }
                else if (!_saidWrong)
                {
                    _saidWrong = true;
                    Director.Say(RideLine.StumpWrong);
                }

                Judged?.Invoke(Prompt, Guess, right);
                float t = 0f;
                while (t < revealSeconds && !_hurry)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
            }

            _drawing = false;
            _live = false;
            Prompt = -1;
            LiveChanged?.Invoke(false);
            SetGoal("Pixel read " + Right + " of your " + Mathf.Max(1, PromptIndex) + " digits");
        }

        private void Touched()
        {
            _dirty = true;
            InkChanged?.Invoke();
        }

        private void Read()
        {
            _dirty = false;
            _nextRead = Time.time + readEvery;
            if (_canvas.StrokeCount == 0)
            {
                Guess = -1;
                Array.Clear(_probabilities, 0, _probabilities.Length);
            }
            else
            {
                Guess = session.Network.Forward(_canvas.ToPixels7(), _hidden, _probabilities);
            }

            Guessed?.Invoke(Guess);
        }
    }
}
