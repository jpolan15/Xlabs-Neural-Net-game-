using System.Collections.Generic;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Core.Training;

namespace Convergence.Gameplay
{
    /// <summary>
    /// Moves the ship from the sensor OR, to XOR, to photo training, to nav-log attention, then the jump.
    /// A chamber advances only when its evaluation passes.
    /// </summary>
    public class VoyageDirector : MonoBehaviour
    {
        public enum Leg
        {
            SensorOr = 0,
            SpectrumXor = 1,
            EarthPhotos = 2,
            NavAttention = 3,
            Jumped = 4
        }

        [SerializeField] private ChamberController chamber;
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ShipDoor[] doors;

        private readonly List<PhotoSample> _filed = new List<PhotoSample>(8);
        private Leg _leg = Leg.SensorOr;
        private bool _noiseMasked;

        public Leg CurrentLeg => _leg;
        public bool NoiseMasked => _noiseMasked;
        public IReadOnlyList<PhotoSample> FiledPhotos => _filed;

        /// <summary>Raised when A.U.R.A. has a new line.</summary>
        public event System.Action<string> OnAuraLine;

        /// <summary>Raised when the jump is allowed to play.</summary>
        public event System.Action OnJumpHome;

        /// <summary>Raised after a chamber actually advances the voyage.</summary>
        public event System.Action<Leg> OnLegChanged;

        /// <summary>Journal lines collected from chambers the player actually finished.</summary>
        public IReadOnlyList<string> Journal => _journal;
        private readonly List<string> _journal = new List<string>(4);

        private void Awake()
        {
            if (chamber == null) chamber = FindAnyObjectByType<ChamberController>();
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (doors == null || doors.Length == 0) doors = FindObjectsByType<ShipDoor>(FindObjectsSortMode.None);
        }

        private void OnEnable()
        {
            if (chamber != null) chamber.OnPuzzleSolved += HandlePuzzleSolved;
        }

        private void OnDisable()
        {
            if (chamber != null) chamber.OnPuzzleSolved -= HandlePuzzleSolved;
        }

        private void Start()
        {
            OnAuraLine?.Invoke("Sensor array dark. Either beacon should wake it.");
        }

        private void HandlePuzzleSolved()
        {
            if (_leg == Leg.SensorOr)
            {
                _leg = Leg.SpectrumXor;
                _journal.Add("A single neuron can fire when either sensor beacon is on, and stay quiet when both are off.");
                Unlock("SensorBay");
                Unlock("Observatory");
                chamber.BeginPuzzle(PuzzleDefinition.CreateXorPuzzle());
                OnLegChanged?.Invoke(_leg);
                OnAuraLine?.Invoke("Array awake. Drives are only warming up. One straight cut cannot separate the spectrum filter.");
            }
            else if (_leg == Leg.SpectrumXor)
            {
                _leg = Leg.EarthPhotos;
                _journal.Add("XOR needed a hidden layer. A single neuron left one of the four points on the wrong side.");
                OnLegChanged?.Invoke(_leg);
                OnAuraLine?.Invoke("Telescope online. Aim, take a photo, and file the card EARTH or NOT EARTH.");
            }
        }

        /// <summary>Installs a two-neuron hidden layer. Weights start at zero. The player still has to tune them.</summary>
        public void InstallHiddenLayer()
        {
            if (_leg != Leg.SpectrumXor || neuralState == null) return;
            var hidden = new LayerModel(new[]
            {
                new NeuronModel(2, 0.0, ActivationType.Step),
                new NeuronModel(2, 0.0, ActivationType.Step)
            });
            var output = new LayerModel(new[] { new NeuronModel(2, 0.0, ActivationType.Step) });
            neuralState.Initialize(new NetworkModel(new[] { hidden, output }));
            neuralState.SelectEditableNeuron(0, 0);
            OnAuraLine?.Invoke("Hidden layer installed, weights still blank. Select a neuron and turn the dials.");
        }

        /// <summary>Cycles which neuron the existing dials edit.</summary>
        public void CycleEditableNeuron()
        {
            if (neuralState == null || neuralState.Network == null) return;
            int layer = neuralState.EditLayer;
            int neuron = neuralState.EditNeuron + 1;
            if (neuron >= neuralState.Network.Layers[layer].NeuronCount)
            {
                neuron = 0;
                layer = (layer + 1) % neuralState.Network.Layers.Count;
            }
            neuralState.SelectEditableNeuron(layer, neuron);
        }

        /// <summary>Stores one photographed feature vector and the tray the player chose.</summary>
        public void FilePhoto(double blue, double white, double brightness, bool labeledEarth)
        {
            if (_leg != Leg.EarthPhotos) return;
            _filed.Add(new PhotoSample(blue, white, brightness, labeledEarth ? 1.0 : 0.0));
            OnAuraLine?.Invoke(_filed.Count + " cards filed.");
        }

        /// <summary>
        /// Trains on the filed cards with SGD, then grades the held-out Earth set.
        /// A bad filing fails in words the player can read.
        /// </summary>
        public PuzzleEvaluation TrainAndGrade(double learningRate, int epochs)
        {
            if (_leg != Leg.EarthPhotos) return null;
            if (_filed.Count < 2)
            {
                OnAuraLine?.Invoke("File at least two cards before training.");
                return null;
            }

            var hidden = new LayerModel(3, 4, ActivationType.Sigmoid);
            var output = new LayerModel(new[] { new NeuronModel(4, 0.0, ActivationType.Sigmoid) });
            var network = new NetworkModel(new[] { hidden, output });

            var inputs = new List<double[]>(_filed.Count);
            var targets = new List<double[]>(_filed.Count);
            for (int i = 0; i < _filed.Count; i++)
            {
                inputs.Add(new[] { _filed[i].Blue, _filed[i].White, _filed[i].Brightness });
                targets.Add(new[] { _filed[i].Label });
            }

            int steps = Mathf.Max(1, epochs);
            for (int epoch = 0; epoch < steps; epoch++)
            {
                StochasticGradientDescent.Run(network, inputs, targets, learningRate, LossKind.BinaryCrossEntropy, seed: epoch);
            }

            var puzzle = PuzzleDefinition.CreateEarthPhotoPuzzle();
            PuzzleEvaluation eval = PuzzleEvaluator.Evaluate(network, puzzle);
            if (eval.Passed)
            {
                _leg = Leg.NavAttention;
                _journal.Add("The recognizer was graded on photos that were not in the tray, including an ice giant that is blue and is not Earth.");
                Unlock("Engine");
                OnLegChanged?.Invoke(_leg);
                OnAuraLine?.Invoke("Held-out photos agree. Engine room is open. A corrupted log token is pulling the jump.");
            }
            else
            {
                OnAuraLine?.Invoke(DescribeTrainingFailure(eval));
            }

            return eval;
        }

        /// <summary>Masks or restores the NOISE token, then reads the attention prediction.</summary>
        public NavAttentionReport SetNoiseMasked(bool masked)
        {
            _noiseMasked = masked;
            NavAttentionReport report = NavLogAttention.Predict(masked);
            if (_leg == Leg.NavAttention && report.PredictsJumpHome)
            {
                _leg = Leg.Jumped;
                _journal.Add("Masking NOISE let EARTH_LOCK take the attention weight, and the circuit predicted JUMP_HOME.");
                OnLegChanged?.Invoke(_leg);
                OnAuraLine?.Invoke("Jump lock. Burning out of the field. Earth orbit ahead.");
                OnJumpHome?.Invoke();
            }
            else if (_leg == Leg.NavAttention)
            {
                OnAuraLine?.Invoke("Attention is still on " + report.PredictedToken + ". The corrupted token is in the window.");
            }

            return report;
        }

        private void Unlock(string id)
        {
            if (doors == null) return;
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] != null && doors[i].DoorId == id) doors[i].Unlock();
            }
        }

        private static string DescribeTrainingFailure(PuzzleEvaluation eval)
        {
            if (eval.Diagnostics == null) return "The held-out set disagreed.";
            for (int i = 0; i < eval.Diagnostics.Count; i++)
            {
                CaseDiagnostic diag = eval.Diagnostics[i];
                if (diag.IsCorrect) continue;
                if (diag.Label == "Ice giant" && diag.ActualOutput >= 0.5)
                {
                    return "Held-out ice giant was called Earth. It is blue, and it is not Earth. The tray only taught blue means Earth.";
                }

                return "Held-out card " + diag.Label + " was called " + (diag.ActualOutput >= 0.5 ? "EARTH" : "NOT EARTH") + ".";
            }

            return eval.Summary;
        }
    }

    /// <summary>One filed photo. Features come from the capture, not from a handwritten label.</summary>
    public readonly struct PhotoSample
    {
        public double Blue { get; }
        public double White { get; }
        public double Brightness { get; }
        public double Label { get; }

        public PhotoSample(double blue, double white, double brightness, double label)
        {
            Blue = blue;
            White = white;
            Brightness = brightness;
            Label = label;
        }
    }
}
