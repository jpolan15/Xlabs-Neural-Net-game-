using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Classic 3D Multi-Layer Neural Network Visualizer for Level 1.
    /// Renders an authentic, floating, 360-degree rotating Neural Network:
    /// - 3 Input Neurons (Red/Coral glowing nodes: X1, X2, Bias)
    /// - 4 Hidden Layer Neurons (Cyan/Blue glowing nodes: H1, H2, H3, H4)
    /// - 2 Output Neurons (Emerald glowing nodes: Y1, Y2)
    /// - 20 Synaptic Axons (12 Input->Hidden + 8 Hidden->Output) with dynamic weight reactivity
    /// - 360-degree continuous orbital rotation and harmonic levitation floating
    /// - Floating 3D holographic layer labels (INPUT, HIDDEN, OUTPUT)
    /// - Animated sequential energy wave packet propagation on pulse triggers and ambient idle flow
    /// Strictly Presentation layer: observes state and never evaluates or decides puzzle correctness.
    /// </summary>
    [ExecuteAlways]
    public class ClassicNeuralNetwork3DVisualizer : MonoBehaviour
    {
        [Header("State Sources")]
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;

        [Header("Rotation & Levitation")]
        [SerializeField] private Transform networkPivot;
        [SerializeField] private bool autoRotate = true;
        [SerializeField] private float rotationSpeed = 8.0f; // Degrees per second (smooth, majestic)
        [SerializeField] private float levitationAmplitude = 0.035f;
        [SerializeField] private float levitationFrequency = 1.2f;
        [SerializeField] private KeyCode toggleRotationKey = KeyCode.R;

        [Header("Layer Nodes")]
        [SerializeField] private Transform[] inputNodes = new Transform[3];   // X1, X2, Bias
        [SerializeField] private Transform[] hiddenNodes = new Transform[4];  // H1, H2, H3, H4
        [SerializeField] private Transform[] outputNodes = new Transform[2];  // Y1, Y2 (or Output Core)

        [Header("Node Visual Renderers")]
        [SerializeField] private Renderer[] inputRenderers = new Renderer[3];
        [SerializeField] private Renderer[] hiddenRenderers = new Renderer[4];
        [SerializeField] private Renderer[] outputRenderers = new Renderer[2];
        [SerializeField] private Light[] layerGlowLights;

        [Header("Synaptic Axon Line Renderers")]
        [SerializeField] private LineRenderer[] inputToHiddenSynapses = new LineRenderer[12]; // 3 x 4 = 12
        [SerializeField] private LineRenderer[] hiddenToOutputSynapses = new LineRenderer[8];  // 4 x 2 = 8
        [SerializeField] private LineRenderer outputToGateBeam;

        [Header("Layer Floating Hologram Labels")]
        [SerializeField] private Transform inputLayerLabel;
        [SerializeField] private Transform hiddenLayerLabel;
        [SerializeField] private Transform outputLayerLabel;

        [Header("Color Palettes")]
        [SerializeField] private Color inputCoral = new Color(0.98f, 0.40f, 0.30f, 1.0f);   // Ruby Coral (Inputs)
        [SerializeField] private Color hiddenAzure = new Color(0.60f, 0.30f, 0.95f, 1.0f);  // Cyber Violet (Hidden)
        [SerializeField] private Color outputEmerald = new Color(0.08f, 0.90f, 0.50f, 1.0f); // Mint Emerald (Output)
        [SerializeField] private Color amberGlow = new Color(0.98f, 0.65f, 0.10f, 1.0f);
        [SerializeField] private Color inactiveColor = new Color(0.18f, 0.22f, 0.30f, 0.35f);

        private Vector3 _initialPivotPosition;
        private bool _isPulsing = false;
        private Camera _mainCamera;

        public bool AutoRotate
        {
            get => autoRotate;
            set => autoRotate = value;
        }

        private void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (networkPivot == null) networkPivot = transform;
            _initialPivotPosition = networkPivot.localPosition;
            _mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered += HandleForwardPassTriggered;
                chamberController.OnEvaluationComplete += HandleEvaluationComplete;
                chamberController.OnChamberReset += HandleChamberReset;
            }
            if (neuralState != null)
            {
                neuralState.OnStateMutated += HandleStateMutated;
            }
        }

        private void OnDisable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered -= HandleForwardPassTriggered;
                chamberController.OnEvaluationComplete -= HandleEvaluationComplete;
                chamberController.OnChamberReset -= HandleChamberReset;
            }
            if (neuralState != null)
            {
                neuralState.OnStateMutated -= HandleStateMutated;
            }
        }

        private void Start()
        {
            UpdateSynapseLines();
            UpdateNodeColors();
        }

        private void Update()
        {
            // Toggle rotation on [R]
            if (Input.GetKeyDown(toggleRotationKey))
            {
                autoRotate = !autoRotate;
            }

            // 1. Continuous 360-degree orbital rotation & floating levitation
            if (networkPivot != null)
            {
                if (autoRotate)
                {
                    networkPivot.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
                }

                // Smooth harmonic levitation bobbing
                float bob = Mathf.Sin(Time.time * levitationFrequency) * levitationAmplitude;
                networkPivot.localPosition = _initialPivotPosition + new Vector3(0, bob, 0);
            }

            // 2. Billboard 3D layer labels to always face the player camera
            UpdateBillboardLabels();

            // 3. Update dynamic synaptic axon lines and node glows
            if (!_isPulsing)
            {
                UpdateSynapseLines();
                UpdateNodeColors();
            }
        }

        private void UpdateBillboardLabels()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Vector3 camPos = _mainCamera.transform.position;

            if (inputLayerLabel != null)
            {
                inputLayerLabel.LookAt(inputLayerLabel.position + _mainCamera.transform.rotation * Vector3.forward, _mainCamera.transform.rotation * Vector3.up);
            }
            if (hiddenLayerLabel != null)
            {
                hiddenLayerLabel.LookAt(hiddenLayerLabel.position + _mainCamera.transform.rotation * Vector3.forward, _mainCamera.transform.rotation * Vector3.up);
            }
            if (outputLayerLabel != null)
            {
                outputLayerLabel.LookAt(outputLayerLabel.position + _mainCamera.transform.rotation * Vector3.forward, _mainCamera.transform.rotation * Vector3.up);
            }
        }

        private void HandleStateMutated()
        {
            UpdateSynapseLines();
            UpdateNodeColors();
        }

        private void HandleForwardPassTriggered()
        {
            if (!gameObject.activeInHierarchy) return;
            StopAllCoroutines();
            StartCoroutine(RoutinePropagateNeuralPulse());
        }

        private void HandleEvaluationComplete(PuzzleEvaluation eval)
        {
            if (eval != null && eval.Passed)
            {
                for (int i = 0; i < outputRenderers.Length; i++)
                {
                    if (outputRenderers[i] != null && outputRenderers[i].material != null)
                    {
                        outputRenderers[i].material.SetColor("_EmissionColor", outputEmerald * 4.0f);
                    }
                }
            }
        }

        private void HandleChamberReset()
        {
            StopAllCoroutines();
            _isPulsing = false;
            if (outputToGateBeam != null) outputToGateBeam.enabled = false;
            UpdateSynapseLines();
            UpdateNodeColors();
        }

        /// <summary>
        /// Updates all 20 synaptic line endpoints, widths, and emissive colors based on network parameters.
        /// </summary>
        public void UpdateSynapseLines()
        {
            if (inputNodes == null || hiddenNodes == null || outputNodes == null) return;

            double w1 = (neuralState != null && neuralState.Cable1Connected) ? neuralState.Weight1 : 0.0;
            double w2 = (neuralState != null && neuralState.Cable2Connected) ? neuralState.Weight2 : 0.0;
            double b = neuralState != null ? neuralState.Bias : 0.0;
            double[] inputWeights = { w1, w2, b };

            // 1. Input -> Hidden Synapses (3 x 4 = 12 synapses)
            int synIdx = 0;
            for (int i = 0; i < 3; i++)
            {
                if (inputNodes[i] == null) continue;
                Vector3 startPos = inputNodes[i].position;
                double weight = inputWeights[i];
                float absW = (float)Math.Abs(weight);

                Color synColor = absW > 0.05f ? (weight >= 0 ? hiddenAzure : amberGlow) : inactiveColor;
                float width = Mathf.Lerp(0.006f, 0.016f, absW / 2.0f);

                // Add subtle ambient idle wave pulse
                float idlePulse = Mathf.PingPong(Time.time * 1.8f + (i * 0.4f), 0.003f);

                for (int h = 0; h < 4; h++)
                {
                    if (synIdx < inputToHiddenSynapses.Length && inputToHiddenSynapses[synIdx] != null && hiddenNodes[h] != null)
                    {
                        var lr = inputToHiddenSynapses[synIdx];
                        lr.useWorldSpace = true;
                        lr.positionCount = 2;
                        lr.SetPosition(0, startPos);
                        lr.SetPosition(1, hiddenNodes[h].position);
                        lr.startWidth = width + idlePulse;
                        lr.endWidth = (width * 1.15f) + idlePulse;
                        lr.startColor = synColor;
                        lr.endColor = synColor;
                    }
                    synIdx++;
                }
            }

            // 2. Hidden -> Output Synapses (4 hidden x 2 output = 8 synapses)
            Color actColor = neuralState != null ? (neuralState.Activation switch
            {
                ActivationType.Step => outputEmerald,
                ActivationType.Linear => hiddenAzure,
                ActivationType.ReLU => amberGlow,
                ActivationType.Sigmoid => new Color(0.85f, 0.0f, 1.0f, 1.0f),
                _ => hiddenAzure
            }) : hiddenAzure;

            int hidOutIdx = 0;
            for (int h = 0; h < 4; h++)
            {
                if (hiddenNodes[h] == null) continue;
                Vector3 hidPos = hiddenNodes[h].position;

                for (int o = 0; o < outputNodes.Length; o++)
                {
                    if (outputNodes[o] == null) continue;
                    if (hidOutIdx < hiddenToOutputSynapses.Length && hiddenToOutputSynapses[hidOutIdx] != null)
                    {
                        var lr = hiddenToOutputSynapses[hidOutIdx];
                        lr.useWorldSpace = true;
                        lr.positionCount = 2;
                        lr.SetPosition(0, hidPos);
                        lr.SetPosition(1, outputNodes[o].position);
                        lr.startWidth = 0.010f;
                        lr.endWidth = 0.012f;
                        lr.startColor = actColor;
                        lr.endColor = actColor;
                    }
                    hidOutIdx++;
                }
            }

            // 3. Output -> Gate Beam
            if (outputToGateBeam != null && !_isPulsing)
            {
                outputToGateBeam.enabled = false;
            }
        }

        private void UpdateNodeColors()
        {
            if (neuralState == null) return;

            // Input Nodes (Red/Coral glowing)
            if (inputRenderers != null)
            {
                if (inputRenderers.Length > 0 && inputRenderers[0] != null)
                    inputRenderers[0].sharedMaterial.SetColor("_EmissionColor", neuralState.Cable1Connected ? inputCoral * 1.3f : inactiveColor);
                if (inputRenderers.Length > 1 && inputRenderers[1] != null)
                    inputRenderers[1].sharedMaterial.SetColor("_EmissionColor", neuralState.Cable2Connected ? inputCoral * 1.3f : inactiveColor);
                if (inputRenderers.Length > 2 && inputRenderers[2] != null)
                    inputRenderers[2].sharedMaterial.SetColor("_EmissionColor", amberGlow * 1.3f);
            }

            // Hidden Nodes (Cyan/Azure glowing with subtle pulsation)
            if (hiddenRenderers != null)
            {
                float pulse = 1.1f + Mathf.PingPong(Time.time * 1.8f, 0.3f);
                for (int h = 0; h < hiddenRenderers.Length; h++)
                {
                    if (hiddenRenderers[h] != null)
                    {
                        hiddenRenderers[h].sharedMaterial.SetColor("_EmissionColor", hiddenAzure * pulse);
                    }
                }
            }

            // Output Nodes (Emerald glowing)
            if (outputRenderers != null)
            {
                float outPulse = 1.2f + Mathf.PingPong(Time.time * 2.0f, 0.4f);
                for (int o = 0; o < outputRenderers.Length; o++)
                {
                    if (outputRenderers[o] != null)
                    {
                        outputRenderers[o].sharedMaterial.SetColor("_EmissionColor", outputEmerald * outPulse);
                    }
                }
            }
        }

        /// <summary>
        /// Sequential 3-stage animated pulse propagation sequence:
        /// 1. Input Layer ignites -> 12 high-energy wave packets rush across synapses.
        /// 2. Hidden Layer detonates with glowing bursts -> 8 packets rush to Output Layer.
        /// 3. Output Nodes discharge plasma energy and fire a laser beam into the sector gate!
        /// </summary>
        private IEnumerator RoutinePropagateNeuralPulse()
        {
            _isPulsing = true;

            // --- Stage 1: Input to Hidden Layer (0.0s - 0.45s) ---
            for (int i = 0; i < inputRenderers.Length; i++)
            {
                if (inputRenderers[i] != null)
                {
                    inputRenderers[i].transform.localScale = Vector3.one * 0.48f;
                }
            }

            float t = 0f;
            float duration1 = 0.45f;
            while (t < duration1)
            {
                t += Time.deltaTime;
                float progress = t / duration1;

                for (int i = 0; i < inputToHiddenSynapses.Length; i++)
                {
                    if (inputToHiddenSynapses[i] != null)
                    {
                        inputToHiddenSynapses[i].startWidth = Mathf.Lerp(0.08f, 0.02f, progress);
                        inputToHiddenSynapses[i].endWidth = Mathf.Lerp(0.10f, 0.03f, progress);
                    }
                }
                yield return null;
            }

            for (int i = 0; i < inputRenderers.Length; i++)
            {
                if (inputRenderers[i] != null)
                {
                    inputRenderers[i].transform.localScale = Vector3.one * 0.35f;
                }
            }

            // --- Stage 2: Hidden to Output Layer (0.45s - 0.85s) ---
            for (int h = 0; h < hiddenRenderers.Length; h++)
            {
                if (hiddenRenderers[h] != null)
                {
                    hiddenRenderers[h].transform.localScale = Vector3.one * 0.52f;
                }
            }

            t = 0f;
            float duration2 = 0.4f;
            while (t < duration2)
            {
                t += Time.deltaTime;
                float progress = t / duration2;

                for (int h = 0; h < hiddenToOutputSynapses.Length; h++)
                {
                    if (hiddenToOutputSynapses[h] != null)
                    {
                        hiddenToOutputSynapses[h].startWidth = Mathf.Lerp(0.10f, 0.03f, progress);
                        hiddenToOutputSynapses[h].endWidth = Mathf.Lerp(0.12f, 0.04f, progress);
                    }
                }
                yield return null;
            }

            for (int h = 0; h < hiddenRenderers.Length; h++)
            {
                if (hiddenRenderers[h] != null)
                {
                    hiddenRenderers[h].transform.localScale = Vector3.one * 0.38f;
                }
            }

            // --- Stage 3: Output Discharge (0.85s - 1.4s) ---
            for (int o = 0; o < outputRenderers.Length; o++)
            {
                if (outputRenderers[o] != null)
                {
                    outputRenderers[o].transform.localScale = Vector3.one * 0.65f;
                }
            }

            if (outputToGateBeam != null)
            {
                outputToGateBeam.enabled = true;
            }

            yield return new WaitForSeconds(0.5f);

            for (int o = 0; o < outputRenderers.Length; o++)
            {
                if (outputRenderers[o] != null)
                {
                    outputRenderers[o].transform.localScale = Vector3.one * 0.45f;
                }
            }

            if (outputToGateBeam != null)
            {
                outputToGateBeam.enabled = false;
            }

            _isPulsing = false;
            UpdateSynapseLines();
            UpdateNodeColors();
        }
    }
}
