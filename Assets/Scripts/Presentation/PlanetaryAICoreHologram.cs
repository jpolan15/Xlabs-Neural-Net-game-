using System;
using System.Text;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Holographic Global AI Consciousness Visualizer (SYNAPSE-GPT Core).
    /// Projects a floating planetary neural mesh with orbital gyroscopic rings high in the chamber.
    /// Visually communicates the story stakes and real-time global health of the foundation AI:
    /// - Corrupted/Glitched State: Red/Amber jittering error rings, glitching alert billboards, integrity ~18%.
    /// - Progressive Calibration: Stabilizes as conduits are attached, Step crystal is slotted, and weights balance.
    /// - Harmonic Convergence (100%): Radiant cyan/emerald glow, serene orbital rotation, unsealed gateway aura.
    /// Strictly Presentation layer: observes state and never evaluates or decides puzzle correctness.
    /// </summary>
    [ExecuteAlways]
    public class PlanetaryAICoreHologram : MonoBehaviour
    {
        [Header("State Sources")]
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;

        [Header("Planetary Core Visuals")]
        [SerializeField] private Transform planetSphere;
        [SerializeField] private Transform innerOrbitalRing;
        [SerializeField] private Transform outerOrbitalRing;
        [SerializeField] private Light coreAuraLight;
        [SerializeField] private ParticleSystem glitchParticles;

        [Header("3D Billboard Telemetry")]
        [SerializeField] private TextMesh headerTextMesh;
        [SerializeField] private TextMesh integrityBarTextMesh;
        [SerializeField] private TextMesh statusDetailsTextMesh;

        [Header("Animation Settings")]
        [SerializeField] private float planetRotationSpeed = 12f;
        [SerializeField] private float ring1RotationSpeed = 24f;
        [SerializeField] private float ring2RotationSpeed = -18f;
        [SerializeField] private float levitationAmplitude = 0.04f;
        [SerializeField] private float levitationSpeed = 1.4f;

        [Header("Color Palette")]
        [SerializeField] private Color criticalColor = new Color(0.95f, 0.25f, 0.25f, 1.0f); // Crimson Red
        [SerializeField] private Color warningColor = new Color(0.98f, 0.65f, 0.10f, 1.0f);  // Solar Amber
        [SerializeField] private Color calibratingColor = new Color(0.20f, 0.75f, 1.0f, 1.0f); // Cyber Cyan
        [SerializeField] private Color stabilizedColor = new Color(0.10f, 0.95f, 0.55f, 1.0f); // Mint Emerald

        private Vector3 _initialLocalPos;
        private float _currentIntegrity = 0.18f;
        private float _targetIntegrity = 0.18f;
        private Camera _mainCamera;
        private readonly StringBuilder _barBuffer = new StringBuilder(64);
        private readonly StringBuilder _detailsBuffer = new StringBuilder(256);
        private bool _isHarmonized = false;

        public float GlobalIntegrity => _currentIntegrity;

        private void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            _initialLocalPos = transform.localPosition;
            _mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (neuralState != null) neuralState.OnStateMutated += HandleStateMutated;
            if (chamberController != null)
            {
                chamberController.OnEvaluationComplete += HandleEvaluationComplete;
                chamberController.OnChamberReset += HandleChamberReset;
            }
        }

        private void OnDisable()
        {
            if (neuralState != null) neuralState.OnStateMutated -= HandleStateMutated;
            if (chamberController != null)
            {
                chamberController.OnEvaluationComplete -= HandleEvaluationComplete;
                chamberController.OnChamberReset -= HandleChamberReset;
            }
        }

        private void Start()
        {
            CalculateTargetIntegrity();
            _currentIntegrity = _targetIntegrity;
            UpdateVisuals();
        }

        private void Update()
        {
            // Smooth rotation
            if (planetSphere != null)
            {
                planetSphere.Rotate(Vector3.up, planetRotationSpeed * Time.deltaTime, Space.Self);
            }
            if (innerOrbitalRing != null)
            {
                innerOrbitalRing.Rotate(Vector3.forward + Vector3.right, ring1RotationSpeed * Time.deltaTime, Space.Self);
            }
            if (outerOrbitalRing != null)
            {
                outerOrbitalRing.Rotate(Vector3.up + Vector3.forward, ring2RotationSpeed * Time.deltaTime, Space.Self);
            }

            // Levitation bobbing
            float bob = Mathf.Sin(Time.time * levitationSpeed) * levitationAmplitude;
            transform.localPosition = _initialLocalPos + new Vector3(0, bob, 0);

            // Smooth integrity bar interpolation
            _currentIntegrity = Mathf.MoveTowards(_currentIntegrity, _targetIntegrity, Time.deltaTime * 0.8f);

            // Billboard 3D text toward player camera
            UpdateBillboards();

            UpdateVisuals();
        }

        private void UpdateBillboards()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Transform textRoot = headerTextMesh != null ? headerTextMesh.transform.parent : null;
            if (textRoot != null)
            {
                textRoot.LookAt(textRoot.position + _mainCamera.transform.rotation * Vector3.forward, _mainCamera.transform.rotation * Vector3.up);
            }
        }

        private void HandleStateMutated()
        {
            CalculateTargetIntegrity();
        }

        private void HandleEvaluationComplete(PuzzleEvaluation eval)
        {
            if (eval != null && eval.Passed)
            {
                _isHarmonized = true;
                _targetIntegrity = 1.0f;
            }
            else
            {
                _isHarmonized = false;
                CalculateTargetIntegrity();
            }
        }

        private void HandleChamberReset()
        {
            _isHarmonized = false;
            CalculateTargetIntegrity();
        }

        private void CalculateTargetIntegrity()
        {
            if (neuralState == null)
            {
                _targetIntegrity = 0.18f;
                return;
            }

            if (_isHarmonized)
            {
                _targetIntegrity = 1.0f;
                return;
            }

            float integrity = 0.10f; // Baseline corrupted state

            // Conduits connected (+15% each)
            if (neuralState.Cable1Connected) integrity += 0.15f;
            if (neuralState.Cable2Connected) integrity += 0.15f;

            // Correct Activation Crystal (+20%)
            if (neuralState.Activation == ActivationType.Step) integrity += 0.20f;

            // Proper weights and bias (+20% each)
            if (neuralState.Weight1 >= 0.8 && neuralState.Weight2 >= 0.8) integrity += 0.20f;
            if (neuralState.Bias < 0.0 && neuralState.Bias >= -1.0) integrity += 0.20f;

            _targetIntegrity = Mathf.Clamp(integrity, 0.10f, 0.95f);
        }

        private void UpdateVisuals()
        {
            Color activeColor;
            if (_isHarmonized || _currentIntegrity >= 0.98f)
            {
                activeColor = stabilizedColor;
            }
            else if (_currentIntegrity >= 0.65f)
            {
                activeColor = calibratingColor;
            }
            else if (_currentIntegrity >= 0.35f)
            {
                activeColor = warningColor;
            }
            else
            {
                activeColor = criticalColor;
            }

            // Aura Light
            if (coreAuraLight != null)
            {
                coreAuraLight.color = activeColor;
                coreAuraLight.intensity = _isHarmonized ? 0.9f : (0.4f + Mathf.PingPong(Time.time * 2.0f, 0.25f));
            }

            // Header Text
            if (headerTextMesh != null)
            {
                headerTextMesh.text = "GLOBAL FOUNDATION AI // SYNAPSE-GPT CORE";
                headerTextMesh.color = activeColor;
            }

            // Integrity Bar Text
            if (integrityBarTextMesh != null)
            {
                int totalBlocks = 20;
                int filledBlocks = Mathf.RoundToInt(_currentIntegrity * totalBlocks);

                _barBuffer.Length = 0;
                _barBuffer.Append("INTEGRITY: [");
                for (int i = 0; i < totalBlocks; i++)
                {
                    _barBuffer.Append(i < filledBlocks ? "█" : "░");
                }
                _barBuffer.Append($"] {_currentIntegrity * 100:F0}%");

                if (_isHarmonized)
                {
                    _barBuffer.Append(" [COGNITIVE MATRIX STABILIZED]");
                }
                else if (_currentIntegrity >= 0.65f)
                {
                    _barBuffer.Append(" [SYNAPTIC HARMONY NEAR]");
                }
                else
                {
                    _barBuffer.Append(" [CRITICAL HALLUCINATION DRIFT]");
                }

                integrityBarTextMesh.text = _barBuffer.ToString();
                integrityBarTextMesh.color = activeColor;
            }

            // Status Details Text
            if (statusDetailsTextMesh != null)
            {
                _detailsBuffer.Length = 0;
                if (_isHarmonized)
                {
                    _detailsBuffer.AppendLine("✔ Sector 01 Sensory Perceptron 100% Calibrated");
                    _detailsBuffer.AppendLine("✔ Planetary life support and orbital power grids secure");
                    _detailsBuffer.AppendLine("✔ Awakening Blast Doors Unsealed — Proceed to Chamber 02");
                }
                else
                {
                    if (neuralState != null)
                    {
                        if (!neuralState.Cable1Connected)
                            _detailsBuffer.AppendLine("⚠ RADIATION CONDUIT DISCONNECTED — Sensory channel offline");
                        else
                            _detailsBuffer.AppendLine("✔ Radiation line active");

                        if (!neuralState.Cable2Connected)
                            _detailsBuffer.AppendLine("⚠ BIO-HAZARD CONDUIT DISCONNECTED — Sensory channel offline");
                        else
                            _detailsBuffer.AppendLine("✔ Bio-hazard line active");

                        if (neuralState.Activation != ActivationType.Step)
                            _detailsBuffer.AppendLine($"⚠ INCOMPATIBLE ACTIVATION ({neuralState.Activation}) — Need Step Crystal for 0/1 gate");
                        else
                            _detailsBuffer.AppendLine("✔ Step decision crystal socketed");

                        if (neuralState.Bias >= 0.0)
                            _detailsBuffer.AppendLine("⚠ NOISE BIAS OVER-THRESHOLD — Clean rooms will trigger false alarms!");
                    }
                }

                statusDetailsTextMesh.text = _detailsBuffer.ToString();
                statusDetailsTextMesh.color = Color.white;
            }
        }
    }
}
