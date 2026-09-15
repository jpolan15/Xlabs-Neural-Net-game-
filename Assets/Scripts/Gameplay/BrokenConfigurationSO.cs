using UnityEngine;
using Convergence.Core.Neural;

namespace Convergence.Gameplay
{
    /// <summary>
    /// ScriptableObject defining a damaged starting state for Level 1.
    /// Exists strictly in Gameplay assembly, bridging Unity data into pure Core models.
    /// </summary>
    [CreateAssetMenu(fileName = "BrokenConfig_A", menuName = "Convergence/Broken Configuration")]
    public class BrokenConfigurationSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string configId = "Config_A";
        [TextArea(2, 4)]
        [SerializeField] private string description = "Cold startup state";

        [Header("Starting Perceptron Parameters")]
        [SerializeField] private double initialW1 = 0.0;
        [SerializeField] private double initialW2 = 0.0;
        [SerializeField] private double initialBias = -1.0;
        [SerializeField] private ActivationType initialActivation = ActivationType.Linear;

        [Header("Starting Hardware Disconnects")]
        [SerializeField] private bool cable1Disconnected = true;
        [SerializeField] private bool cable2Disconnected = false;

        public string ConfigId => configId;
        public string Description => description;
        public double InitialW1 => initialW1;
        public double InitialW2 => initialW2;
        public double InitialBias => initialBias;
        public ActivationType InitialActivation => initialActivation;
        public bool Cable1Disconnected => cable1Disconnected;
        public bool Cable2Disconnected => cable2Disconnected;

        /// <summary>
        /// Converts this serialized configuration into a pure C# NetworkModel instance.
        /// </summary>
        public NetworkModel ToNetworkModel()
        {
            return NetworkModel.CreateSingleNeuronNetwork(
                inputCount: 2,
                initialWeights: new double[] { initialW1, initialW2 },
                initialBias: initialBias,
                activation: initialActivation
            );
        }

        /// <summary>
        /// Applies preset values directly to an existing NeuralState instance.
        /// </summary>
        public void ApplyTo(NeuralState state)
        {
            if (state == null) return;
            state.SetWeight(0, initialW1);
            state.SetWeight(1, initialW2);
            state.SetBias(initialBias);
            state.SetActivation(initialActivation);
            state.SetCableConnected(0, !cable1Disconnected);
            state.SetCableConnected(1, !cable2Disconnected);
        }
    }
}
