using System;
using UnityEngine;

namespace Convergence.Gameplay
{
    /// <summary>
    /// Coordinates level reset operations. Supports both deterministic preset reloading
    /// and seeded/random preset re-selection.
    /// </summary>
    public class LevelResetter : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private GatewayController gatewayController;
        [SerializeField] private PerformanceTracker performanceTracker;

        [Header("Presets")]
        [SerializeField] private BrokenConfigurationSO[] availablePresets;
        [SerializeField] private BrokenConfigurationSO activePreset;

        public BrokenConfigurationSO ActivePreset => activePreset;
        public BrokenConfigurationSO[] AvailablePresets => availablePresets;

        public event Action<BrokenConfigurationSO> OnResetCompleted;

        private void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (gatewayController == null) gatewayController = FindAnyObjectByType<GatewayController>();
            if (performanceTracker == null) performanceTracker = FindAnyObjectByType<PerformanceTracker>();

            if (activePreset == null && availablePresets != null && availablePresets.Length > 0)
            {
                activePreset = availablePresets[0];
            }
        }

        /// <summary>
        /// Deterministic reset: Reloads the currently active preset and restores initial chamber state.
        /// </summary>
        public void ResetToActivePreset()
        {
            ApplyReset(activePreset);
        }

        /// <summary>
        /// Seeded or random reset: Selects a preset from availablePresets and restores initial chamber state.
        /// </summary>
        public void ResetRandom(int? seed = null)
        {
            if (availablePresets == null || availablePresets.Length == 0)
            {
                ResetToActivePreset();
                return;
            }

            var random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            int index = random.Next(0, availablePresets.Length);
            activePreset = availablePresets[index];
            ApplyReset(activePreset);
        }

        /// <summary>
        /// Explicit preset selection reset.
        /// </summary>
        public void ResetToPreset(BrokenConfigurationSO preset)
        {
            if (preset == null) return;
            activePreset = preset;
            ApplyReset(activePreset);
        }

        private void ApplyReset(BrokenConfigurationSO preset)
        {
            // 1. Reset neural parameters to preset
            if (preset != null && neuralState != null)
            {
                preset.ApplyTo(neuralState);
            }

            // 2. Reset performance metrics
            performanceTracker?.ResetTracker();

            // 3. Close the gateway
            gatewayController?.Close();

            // 4. Notify chamber state machine
            chamberController?.NotifyReset();

            OnResetCompleted?.Invoke(preset);
        }
    }
}
