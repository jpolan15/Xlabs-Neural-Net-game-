using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Animates the cyber CPU motherboard environment.
    /// Drives pulsating circuit bus-traces, periodic clock-cycle wave pulses,
    /// flickering binary register LEDs on memory microchips, and reactive ambiance on forward pass.
    /// Strictly Presentation layer: observes state and never decides correctness.
    /// </summary>
    [ExecuteAlways]
    public class MotherboardEnvironmentVisual : MonoBehaviour
    {
        [Header("State Listener")]
        [SerializeField] private ChamberController chamberController;

        [Header("Bus-Trace Materials")]
        [SerializeField] private Material busTraceMaterial;
        [SerializeField] private Material cpuSubstrateMaterial;

        [Header("Lighting")]
        [SerializeField] private Light clockCyclePulseLight;
        [SerializeField] private Color normalBusColor = new Color(0.15f, 0.35f, 0.60f, 1.0f); // Soft Slate-Cyan
        [SerializeField] private Color passBusColor = new Color(0.08f, 0.85f, 0.48f, 1.0f);   // Harmonic Mint
        [SerializeField] private Color failBusColor = new Color(0.95f, 0.30f, 0.20f, 1.0f);   // Warning Coral

        [Header("Memory Chips & LEDs")]
        [SerializeField] private Renderer[] memoryChipLeds;

        private float _pulseIntensity = 0f;
        private Color _targetColor;
        private float _clockTimer = 0f;

        private void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            _targetColor = normalBusColor;
        }

        private void OnEnable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered += HandleForwardPass;
                chamberController.OnEvaluationComplete += HandleEvaluationComplete;
                chamberController.OnChamberReset += HandleReset;
            }
        }

        private void OnDisable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered -= HandleForwardPass;
                chamberController.OnEvaluationComplete -= HandleEvaluationComplete;
                chamberController.OnChamberReset -= HandleReset;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Idle ambient clock-cycle pulse (1.0 Hz rhythmic heartbeat)
            _clockTimer += dt;
            float idleWave = Mathf.Sin(_clockTimer * 2.5f) * 0.5f + 0.5f;

            // Decay forward pass spike
            if (_pulseIntensity > 0f)
            {
                _pulseIntensity = Mathf.MoveTowards(_pulseIntensity, 0f, dt * 2.5f);
            }

            float finalEmission = 0.25f + (idleWave * 0.15f) + (_pulseIntensity * 1.5f);

            // Update bus-trace material emission
            if (busTraceMaterial != null)
            {
                Color currentCol = Color.Lerp(normalBusColor, _targetColor, _pulseIntensity);
                busTraceMaterial.SetColor("_EmissionColor", currentCol * finalEmission);
            }

            // Update point light if assigned
            if (clockCyclePulseLight != null)
            {
                clockCyclePulseLight.color = _targetColor;
                clockCyclePulseLight.intensity = 1.0f + (_pulseIntensity * 4.0f);
            }

            // Flicker memory chip LEDs
            if (memoryChipLeds != null && memoryChipLeds.Length > 0)
            {
                for (int i = 0; i < memoryChipLeds.Length; i++)
                {
                    if (memoryChipLeds[i] != null && memoryChipLeds[i].material != null)
                    {
                        bool activeLed = Mathf.PerlinNoise(Time.time * 6f, i * 1.7f) > 0.45f;
                        Color ledCol = activeLed ? normalBusColor * 2.0f : Color.black;
                        memoryChipLeds[i].material.SetColor("_EmissionColor", ledCol);
                    }
                }
            }
        }

        private void HandleForwardPass()
        {
            _pulseIntensity = 1.0f;
            _targetColor = normalBusColor;
        }

        private void HandleEvaluationComplete(Convergence.Core.Puzzles.PuzzleEvaluation eval)
        {
            _pulseIntensity = 1.2f;
            if (eval != null)
            {
                _targetColor = eval.Passed ? passBusColor : failBusColor;
            }
        }

        private void HandleReset()
        {
            _targetColor = normalBusColor;
            _pulseIntensity = 0f;
        }
    }
}
