using System;
using System.Collections;
using Convergence.Core.Training;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// Regions 3 and 7 (v6, ADR-020): the rider's own digit goes through the rider's own network. In Region 3 the
    /// network is still untrained: the rider shoots their digit card to send it in (or it goes by itself after a few
    /// seconds), the light races through, and the network guesses (a random guess, whatever it is). In Region 7 the
    /// network is the one the rider trained: Pixel reads the rider's 3, and if it misses, it studies that 3 with real
    /// training steps (half of each batch is the rider's 3) until it reads it, then reads it again. Core computes every
    /// activation; this exposes them and runs the clock the light follows.
    /// </summary>
    public sealed class ForwardPassMoment : RideMoment, IButtonGame
    {
        [SerializeField] private Level1Session session;
        [SerializeField] private bool trained;
        [SerializeField] private float raceSeconds = 3.5f;
        [SerializeField] private float holdAfter = 0.6f;
        [Tooltip("Region 3: seconds to wait for the rider to shoot their digit before it goes in by itself.")]
        [SerializeField] private float throwWait = 9f;
        [Tooltip("Region 7: studying the rider's 3 stops after this many steps (it reads it long before).")]
        [SerializeField] private int maxStudySteps = 300;
        [SerializeField] private int studyStepsPerFrame = 2;

        private double[] _hidden;
        private double[] _outputs;
        private bool _hurry;
        private bool _waitingForThrow;
        private bool _thrown;

        public bool Trained => trained;

        /// <summary>0 before the race, 1 when the light has reached the answers.</summary>
        public float Progress { get; private set; }

        /// <summary>Hidden activations from Core (16).</summary>
        public double[] Hidden => _hidden;

        /// <summary>Output probabilities from Core (10).</summary>
        public double[] Outputs => _outputs;

        /// <summary>The network's answer: the biggest output.</summary>
        public int Guess { get; private set; } = -1;

        /// <summary>Region 7: true when Pixel had to study the rider's 3 before it could read it.</summary>
        public bool Studied { get; private set; }

        /// <summary>Region 7: steps spent studying the rider's 3.</summary>
        public int StudySteps { get; private set; }

        /// <summary>Region 3: true while the moment waits for the rider to shoot their digit.</summary>
        public bool IsLive => _waitingForThrow;

        public event Action RaceStarted;
        public event Action<int> Revealed;

        /// <summary>Region 3: the rider's digit was sent in (true: the rider shot it).</summary>
        public event Action<bool> Thrown;

        /// <summary>Region 7: Pixel starts studying the rider's 3, and each study frame after (steps so far, p(3)).</summary>
        public event Action<int, double> Studying;

        /// <summary>Runs Core's forward pass on the rider's digit with the rider's network as it is now.</summary>
        public void Compute()
        {
            Guess = session.Read(out _outputs, out _hidden);
        }

        public bool Press(int button, int hand)
        {
            if (!_waitingForThrow) return false;
            _thrown = true;
            return true;
        }

        public void Release(int button, int hand)
        {
        }

        public override void Hurry() => _hurry = true;

        protected override IEnumerator Run()
        {
            _hurry = false;
            Studied = false;
            StudySteps = 0;
            Compute();
            Progress = 0f;

            if (trained)
            {
                yield return Say(RideLine.RightYours);
                yield return Race();
                yield return Hold(holdAfter);
                if (Guess == session.DigitLabel)
                {
                    yield return Say(RideLine.RightYes);
                }
                else
                {
                    yield return Say(RideLine.RightStudy);
                    yield return Study();
                    Compute();
                    yield return Race();
                    yield return Say(RideLine.RightNowYes);
                }

                yield return Say(RideLine.RightAccuracy);
                yield return Say(RideLine.RightLoop);
                yield break;
            }

            // Region 3: the rider sends their own digit in.
            _thrown = false;
            _waitingForThrow = true;
            float waited = 0f;
            while (!_thrown && !_hurry && waited < throwWait)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            _waitingForThrow = false;
            Thrown?.Invoke(_thrown);
            yield return Race();
            yield return Hold(holdAfter);
        }

        private IEnumerator Race()
        {
            Progress = 0f;
            RaceStarted?.Invoke();
            while (Progress < 1f)
            {
                Progress = Mathf.Min(1f, Progress + Time.deltaTime / Mathf.Max(0.1f, _hurry ? raceSeconds * 0.25f : raceSeconds));
                yield return null;
            }

            Revealed?.Invoke(Guess);
        }

        /// <summary>Real training steps where half of every batch is the rider's 3, until the network reads it.</summary>
        private IEnumerator Study()
        {
            Studied = true;
            LiveTrainer trainer = session.Trainer;
            double rate = session.Data.File.live.rates[1];
            double[] digit = session.Digit;
            var h = new double[DigitNetwork.Hidden];
            var p = new double[DigitNetwork.Outputs];
            int label = session.DigitLabel;
            while (StudySteps < maxStudySteps && session.Network.Forward(digit, h, p) != label)
            {
                for (int i = 0; i < studyStepsPerFrame && StudySteps < maxStudySteps; i++)
                {
                    trainer.Step(rate, digit, label);
                    StudySteps++;
                }

                session.NotifyWeightsChanged();
                session.Network.Forward(digit, h, p);
                Studying?.Invoke(StudySteps, p[label]);
                yield return null;
            }

            // Keep going a little so it reads the 3 with some confidence, not by a hair.
            for (int i = 0; i < 10 && StudySteps < maxStudySteps; i++, StudySteps++) trainer.Step(rate, digit, label);
            session.NotifyWeightsChanged();
        }

        private IEnumerator Say(RideLine line)
        {
            float seconds = Director.Narrate(line);
            yield return Hold(seconds);
        }

        private IEnumerator Hold(float seconds)
        {
            float t = 0f;
            while (t < seconds && !_hurry)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
    }
}
