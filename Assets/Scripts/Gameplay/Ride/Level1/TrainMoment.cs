using System;
using System.Collections;
using System.Collections.Generic;
using Convergence.Core.Training;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// Region 6, the training game (v6, ADR-020): the rider trains their own network, live. Three step-size buttons
    /// (the learning rate: tiny, good, huge, from the bake); holding one runs Core's <see cref="LiveTrainer"/>, real
    /// mini-batch gradient descent on real MNIST digits, about twenty-five steps a second (the good step size wins in
    /// under ten seconds). Every few steps the network is scored on test digits it never trains on; the game is won when
    /// that accuracy reaches the bake's target. The huge step really blows the loss up: the game then undoes it (back to
    /// the best weights so far) and Pixel says so; the tiny step really crawls. Nothing here is scripted: the loss curve
    /// and the accuracy are what Core measures.
    /// </summary>
    public sealed class TrainMoment : RideGame, IButtonGame
    {
        public const int ButtonCount = 3;

        [SerializeField] private Level1Session session;
        [SerializeField] private float stepsPerSecond = 25f;
        [Tooltip("Steps between two accuracy checks on the test digits.")]
        [SerializeField] private int checkEvery = 10;
        [Tooltip("The loss has blown up when its smoothed value passes this many times the starting loss.")]
        [SerializeField] private float blowUpFactor = 2.5f;
        [Tooltip("Seconds the buttons stay dark after an undo.")]
        [SerializeField] private float coolSeconds = 1.2f;
        [Tooltip("Seconds of holding the tiny step before Pixel suggests a bigger one.")]
        [SerializeField] private float slowSeconds = 5f;
        [SerializeField] private int historyLength = 160;

        private readonly List<float> _losses = new List<float>();
        private readonly List<float> _accuracies = new List<float>();
        private LiveTrainer _trainer;
        private DigitNetwork _best;
        private bool _live;
        private bool _hurry;
        private bool _auto;
        private int _held = -1;
        private int _heldHand = -1;
        private float _heldSince;
        private float _coolUntil;
        private float _budget;
        private int _stepsAtOpen;
        private bool _saidSlow;
        private bool _saidGood;
        private float _lastBlowUpLine = -100f;

        public bool IsLive => _live;

        /// <summary>The button being held (-1: none).</summary>
        public int Held => _auto ? 1 : _held;

        /// <summary>True for a moment after an undo: the buttons do nothing.</summary>
        public bool Cooling => Time.time < _coolUntil;

        /// <summary>The step size (learning rate) of button b.</summary>
        public double Rate(int button) => session.Data.File.live.rates[button];

        public double TargetAccuracy => session.Data.File.live.targetAccuracy;

        /// <summary>The loss over the whole training set when the game opened.</summary>
        public double StartLoss { get; private set; }

        /// <summary>The smoothed batch loss now.</summary>
        public double Loss => _trainer != null && !double.IsNaN(_trainer.SmoothedLoss) ? _trainer.SmoothedLoss : StartLoss;

        /// <summary>Test accuracy at the last check.</summary>
        public double Accuracy { get; private set; }

        public double BestAccuracy { get; private set; }

        /// <summary>Steps taken in this game.</summary>
        public int Steps => _trainer != null ? _trainer.Steps - _stepsAtOpen : 0;

        /// <summary>How many times the loss blew up and was undone.</summary>
        public int BlowUps { get; private set; }

        /// <summary>The smoothed loss, one value every couple of steps, oldest first (the board's curve).</summary>
        public IReadOnlyList<float> Losses => _losses;

        /// <summary>Test accuracy at every check, oldest first.</summary>
        public IReadOnlyList<float> Accuracies => _accuracies;

        /// <summary>Steps were taken this frame.</summary>
        public event Action Stepped;

        /// <summary>The accuracy was measured (the new value).</summary>
        public event Action<double> Checked;

        /// <summary>The loss blew up and the game undid it.</summary>
        public event Action BlewUp;

        /// <summary>A button was pressed (button) or let go (-1).</summary>
        public event Action<int> HeldChanged;

        public event Action<bool> LiveChanged;

        public bool Press(int button, int hand)
        {
            if (!_live || _auto || button < 0 || button >= ButtonCount || Cooling) return false;
            _held = button;
            _heldHand = hand;
            _heldSince = Time.time;
            HeldChanged?.Invoke(button);
            return true;
        }

        public void Release(int button, int hand)
        {
            if (button != _held || hand != _heldHand) return;
            LetGo();
        }

        public override void Hurry() => _hurry = true;

        protected override int StarsEarned() => Steps <= 600 ? 3 : Steps <= 1200 ? 2 : 1;

        protected override bool PlayedItself => _auto && BestAccuracy < TargetAccuracy * 0.5;

        protected override IEnumerator Play()
        {
            _trainer = session.Trainer;
            _stepsAtOpen = _trainer.Steps;
            _best = session.Network.Clone();
            _losses.Clear();
            _accuracies.Clear();
            _hurry = false;
            _auto = false;
            _held = -1;
            _budget = 0f;
            _saidSlow = false;
            _saidGood = false;
            BlowUps = 0;
            StartLoss = _trainer.TrainLoss();
            Accuracy = BestAccuracy = _trainer.TestAccuracy();
            _losses.Add((float)StartLoss);
            _accuracies.Add((float)Accuracy);
            _live = true;
            LiveChanged?.Invoke(true);
            Show();
            float opened = Time.time;

            while (Accuracy < TargetAccuracy)
            {
                if (!_auto && (_hurry || Time.time - opened > giveUpAfter))
                {
                    _auto = true;
                    LetGo();
                }

                int button = Held;
                if (button >= 0 && !Cooling)
                {
                    _budget += Time.deltaTime * stepsPerSecond * (_hurry ? 4f : 1f);
                    int n = Mathf.Min((int)_budget, 12);
                    _budget -= n;
                    for (int i = 0; i < n && Accuracy < TargetAccuracy; i++)
                    {
                        if (!TakeStep(button)) break;
                    }

                    if (n > 0)
                    {
                        session.NotifyWeightsChanged();
                        Stepped?.Invoke();
                        Show();
                    }

                    React(button);
                }
                else
                {
                    _budget = 0f;
                }

                yield return null;
            }

            LetGo();
            _live = false;
            LiveChanged?.Invoke(false);
            session.MarkTrained(Accuracy);
            Progress = 1f;
            SetGoal("Trained! " + Percent(Accuracy) + " on digits it has never seen");
        }

        /// <summary>One real training step. Returns false when the loss blew up (and was undone).</summary>
        private bool TakeStep(int button)
        {
            _trainer.Step(Rate(button));
            if (Steps % 2 == 0) Record(_losses, (float)Loss);
            double loss = _trainer.SmoothedLoss;
            if (double.IsNaN(loss) || double.IsInfinity(loss) || loss > StartLoss * blowUpFactor || !session.Network.IsFinite)
            {
                Undo();
                return false;
            }

            if (Steps % checkEvery == 0)
            {
                Accuracy = _trainer.TestAccuracy();
                Record(_accuracies, (float)Accuracy);
                if (Accuracy > BestAccuracy)
                {
                    BestAccuracy = Accuracy;
                    _best.CopyFrom(session.Network);
                }

                Checked?.Invoke(Accuracy);
            }

            return true;
        }

        /// <summary>The loss blew up: back to the best weights so far, and the buttons go dark for a moment.</summary>
        private void Undo()
        {
            BlowUps++;
            Record(_losses, (float)System.Math.Min(_trainer.SmoothedLoss, StartLoss * blowUpFactor * 1.5));
            _trainer.Restore(_best);
            Accuracy = BestAccuracy;
            Record(_losses, (float)_trainer.TrainLoss());
            LetGo();
            _coolUntil = Time.time + coolSeconds;
            session.NotifyWeightsChanged();
            BlewUp?.Invoke();
            if (Time.time - _lastBlowUpLine > 6f)
            {
                _lastBlowUpLine = Time.time;
                Director.Say(RideLine.TrainTooBig);
            }

            Show();
        }

        private void React(int button)
        {
            if (_auto) return;
            if (button == 0 && !_saidSlow && Time.time - _heldSince > slowSeconds && Accuracy < 0.6)
            {
                _saidSlow = true;
                Director.Say(RideLine.TrainTooSmall);
            }

            if (button == 1 && !_saidGood && Accuracy >= 0.5)
            {
                _saidGood = true;
                Director.Say(RideLine.TrainGood);
            }
        }

        private void LetGo()
        {
            if (_held < 0) return;
            _held = -1;
            _heldHand = -1;
            HeldChanged?.Invoke(-1);
        }

        private void Record(List<float> list, float value)
        {
            list.Add(value);
            if (list.Count > historyLength) list.RemoveRange(0, list.Count - historyLength);
        }

        private void Show()
        {
            Progress = Mathf.Clamp01((float)(Accuracy / TargetAccuracy));
            SetGoal("Loss " + Loss.ToString("0.00") + "    Accuracy " + Percent(Accuracy) + " of " + Percent(TargetAccuracy) + "    Step " + Steps);
        }

        private static string Percent(double v) => Mathf.RoundToInt((float)(v * 100.0)) + "%";
    }
}
