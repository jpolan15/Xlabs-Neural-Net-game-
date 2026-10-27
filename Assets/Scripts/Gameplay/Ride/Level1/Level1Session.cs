using System;
using Convergence.Core.Training;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level1
{
    /// <summary>
    /// The rider's own run through Level 1 (v6, ADR-020), the thread every stop follows: the digit they drew at the
    /// pixels stop, and the one network they teach. The network starts from the bake's untrained weights; the hot-wire
    /// hunt nudges it, the training game trains it, and Pixel reads the rider's digit with it at the end. Until the
    /// rider has drawn (or if the drawing was skipped), the digit is the bake's hero 3, a real handwritten three.
    /// Moments change it; views listen. It decides nothing about any game.
    /// </summary>
    public sealed class Level1Session : MonoBehaviour
    {
        [SerializeField] private Level1DataSource source;

        private double[] _digit;
        private DigitNetwork _network;
        private LiveTrainer _trainer;

        /// <summary>The 49 numbers of the rider's digit (the hero 3 until they draw one).</summary>
        public double[] Digit
        {
            get
            {
                if (_digit == null) _digit = (double[])source.Data.File.hero.pixels7.Clone();
                return _digit;
            }
        }

        /// <summary>True once the digit is the rider's own drawing.</summary>
        public bool DigitIsRiders { get; private set; }

        /// <summary>The label the rider's digit is meant to be (Level 1 follows a three).</summary>
        public int DigitLabel => Level1Data.HeroDigit;

        /// <summary>The network the rider teaches, starting from the bake's untrained weights.</summary>
        public DigitNetwork Network
        {
            get
            {
                if (_network == null) _network = source.Data.CreateDigitNetwork(false);
                return _network;
            }
        }

        /// <summary>The live trainer on <see cref="Network"/> (real training and test digits from the bake).</summary>
        public LiveTrainer Trainer
        {
            get
            {
                if (_trainer == null) _trainer = source.Data.CreateLiveTrainer(Network);
                return _trainer;
            }
        }

        public Level1Data Data => source.Data;

        /// <summary>True once the training game is won.</summary>
        public bool IsTrained { get; private set; }

        /// <summary>The test accuracy measured when training was won (0 before).</summary>
        public double TrainedAccuracy { get; private set; }

        /// <summary>The rider's digit changed.</summary>
        public event Action DigitChanged;

        /// <summary>The network's weights changed (a nudge, a training step, an undo).</summary>
        public event Action WeightsChanged;

        /// <summary>Sets the rider's digit (49 numbers, 0 = paper, 1 = ink).</summary>
        public void SetDigit(double[] pixels, bool byRider)
        {
            if (pixels == null || pixels.Length != DigitNetwork.Inputs) throw new ArgumentException("Expected 49 pixels.", nameof(pixels));
            _digit = (double[])pixels.Clone();
            DigitIsRiders = byRider;
            DigitChanged?.Invoke();
        }

        /// <summary>Moments call this after they change the network's weights.</summary>
        public void NotifyWeightsChanged() => WeightsChanged?.Invoke();

        /// <summary>The training game was won at this test accuracy.</summary>
        public void MarkTrained(double accuracy)
        {
            IsTrained = true;
            TrainedAccuracy = accuracy;
        }

        /// <summary>What the network makes of the rider's digit now: the guess, with the ten probabilities and the hidden layer.</summary>
        public int Read(out double[] probabilities, out double[] hidden)
        {
            probabilities = new double[DigitNetwork.Outputs];
            hidden = new double[DigitNetwork.Hidden];
            return Network.Forward(Digit, hidden, probabilities);
        }
    }
}
