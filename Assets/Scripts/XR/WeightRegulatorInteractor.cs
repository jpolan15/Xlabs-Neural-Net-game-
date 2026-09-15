using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// XR and physical interactor for adjusting synaptic weight regulators (W1, W2).
    /// Uses stepped detents (0.5 increments) to ensure predictable calibration.
    /// Sends commands to Gameplay.NeuralState; never evaluates puzzles directly.
    /// </summary>
    public class WeightRegulatorInteractor : MonoBehaviour
    {
        [Header("Target Socket")]
        [Tooltip("0 for W1 (Signal 1), 1 for W2 (Signal 2)")]
        [SerializeField] private int socketIndex = 0;

        [Header("Detent Settings")]
        [SerializeField] private double stepSize = 0.5;
        [SerializeField] private double minValue = -2.0;
        [SerializeField] private double maxValue = 2.0;

        [Header("State Reference")]
        [SerializeField] private NeuralState neuralState;

        public int SocketIndex => socketIndex;
        public double CurrentWeight => neuralState != null ? (socketIndex == 0 ? neuralState.Weight1 : neuralState.Weight2) : 0.0;

        public event Action<int, double> OnRegulatorAdjusted;

        private void Awake()
        {
            if (neuralState == null)
            {
                neuralState = FindAnyObjectByType<NeuralState>();
            }
        }

        /// <summary>
        /// Adjusts weight by delta steps (e.g. +1 step = +0.5, -1 step = -0.5).
        /// Clamps to [minValue, maxValue].
        /// </summary>
        public void StepAdjust(int steps)
        {
            double newWeight = CurrentWeight + (steps * stepSize);
            newWeight = System.Math.Round(newWeight / stepSize) * stepSize;
            SetWeight(newWeight);
        }

        public void SetWeight(double value)
        {
            double clamped = System.Math.Max(minValue, System.Math.Min(maxValue, value));
            if (neuralState != null)
            {
                neuralState.SetWeight(socketIndex, clamped);
            }
            OnRegulatorAdjusted?.Invoke(socketIndex, clamped);
        }
    }
}
