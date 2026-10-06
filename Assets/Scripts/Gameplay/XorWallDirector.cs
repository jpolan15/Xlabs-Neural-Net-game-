using System;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;

namespace Convergence.Gameplay
{
    /// <summary>
    /// Chamber 04 stays one neuron until the wall unlocks. Cartridges then build a hidden layer.
    /// The player still has to pass evaluation. This does not mark the puzzle solved.
    /// </summary>
    public sealed class XorWallDirector : MonoBehaviour
    {
        [SerializeField] private ChamberController chamber;
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private SavedCircuitLibrary library;
        [SerializeField] private float unlockSeconds = 90f;

        readonly string[] _slots = new string[3];
        float _unsolved;
        bool _unlocked;

        public bool Unlocked => _unlocked;
        public event Action OnWallUnlocked;
        public event Action<NetworkModel> OnNetworkRebuilt;

        void Awake()
        {
            if (chamber == null) chamber = FindAnyObjectByType<ChamberController>();
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (library == null) library = FindAnyObjectByType<SavedCircuitLibrary>();
        }

        void Update()
        {
            if (_unlocked || chamber == null || chamber.Curriculum == null) return;
            if (chamber.Curriculum.Id != "chamber04") return;
            if (chamber.LastEvaluation != null && chamber.LastEvaluation.Passed) return;
            _unsolved += Time.deltaTime;
            if (_unsolved < unlockSeconds) return;
            _unlocked = true;
            OnWallUnlocked?.Invoke();
        }

        public void Slot(int slot, string circuitId)
        {
            if (slot < 0 || slot > 2 || string.IsNullOrWhiteSpace(circuitId) || library == null) return;
            _slots[slot] = circuitId;
            if (_slots[0] == null || _slots[1] == null || _slots[2] == null) return;
            NetworkModel a = library.Find(_slots[0]);
            NetworkModel b = library.Find(_slots[1]);
            NetworkModel c = library.Find(_slots[2]);
            if (a == null || b == null || c == null) return;
            if (a.SingleNeuron == null || b.SingleNeuron == null || c.SingleNeuron == null) return;

            var hidden = new LayerModel(new[]
            {
                new NeuronModel(a.SingleNeuron.Weights, a.SingleNeuron.Bias, ActivationType.Step),
                new NeuronModel(b.SingleNeuron.Weights, b.SingleNeuron.Bias, ActivationType.Step)
            });
            var output = new LayerModel(new[]
            {
                new NeuronModel(c.SingleNeuron.Weights, c.SingleNeuron.Bias, ActivationType.Step)
            });
            var network = new NetworkModel(new[] { hidden, output });
            neuralState.Initialize(network);
            neuralState.SelectEditableNeuron(1, 0);
            OnNetworkRebuilt?.Invoke(network);
        }
    }
}
