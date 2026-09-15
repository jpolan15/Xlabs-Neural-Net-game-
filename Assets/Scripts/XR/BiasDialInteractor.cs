using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// XR and physical interactor for adjusting the central bias calibration dial (b).
    /// Uses stepped detents (0.5 increments). Sends commands to Gameplay.NeuralState.
    /// </summary>
    public class BiasDialInteractor : MonoBehaviour
    {
        [Header("Detent Settings")]
        [SerializeField] private double stepSize = 0.5;
        [SerializeField] private double minValue = -2.0;
        [SerializeField] private double maxValue = 2.0;

        [Header("State Reference")]
        [SerializeField] private NeuralState neuralState;

        public double CurrentBias => neuralState != null ? neuralState.Bias : 0.0;

        public event Action<double> OnBiasDialAdjusted;

        private void Awake()
        {
            if (neuralState == null)
            {
                neuralState = FindAnyObjectByType<NeuralState>();
            }
        }

        public void StepAdjust(int steps)
        {
            double newBias = CurrentBias + (steps * stepSize);
            newBias = System.Math.Round(newBias / stepSize) * stepSize;
            SetBias(newBias);
        }

        public void SetBias(double value)
        {
            double clamped = System.Math.Max(minValue, System.Math.Min(maxValue, value));
            if (neuralState != null)
            {
                neuralState.SetBias(clamped);
            }
            OnBiasDialAdjusted?.Invoke(clamped);
        }
    }
}
