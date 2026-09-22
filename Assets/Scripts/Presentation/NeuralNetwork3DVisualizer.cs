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
    /// Physical 3D playable Neural Network Visualizer.
    /// Drives live tangible axon geometries, color/thickness feedback,
    /// dynamic summation core energy swell, activation chamber transformation,
    /// animated multi-stage packet propagation along axons, and live world-space equation breakdown.
    /// Strictly Presentation layer: observes state and never evaluates or decides correctness.
    /// </summary>
    public class NeuralNetwork3DVisualizer : MonoBehaviour
    {
        [Header("State Sources")]
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;

        [Header("Physical Nodes & Hardware")]
        [SerializeField] private Transform terminalX1;
        [SerializeField] private Transform terminalX2;
        [SerializeField] private Transform summationCore;
        [SerializeField] private Transform biasInjector;
        [SerializeField] private Transform activationChamber;
        [SerializeField] private Transform outputEmitter;

        [Header("Synaptic Axon Meshes / Renderers")]
        [SerializeField] private LineRenderer axon1Renderer;
        [SerializeField] private LineRenderer axon2Renderer;
        [SerializeField] private LineRenderer biasConduitRenderer;
        [SerializeField] private LineRenderer outputBeamRenderer;

        [Header("Dial References for Visual Rotation")]
        [SerializeField] private Transform w1RotaryDial;
        [SerializeField] private Transform w2RotaryDial;
        [SerializeField] private Transform biasKnob;

        [Header("Summation Core Plasma")]
        [SerializeField] private Light corePointLight;
        [SerializeField] private Transform coreEnergySphere;

        [Header("Activation Crystal & Chamber Light")]
        [SerializeField] private Light activationLight;

        [Header("Packet Particles")]
        [SerializeField] private Transform packet1Prefab;
        [SerializeField] private Transform packet2Prefab;
        [SerializeField] private Transform biasPacketPrefab;

        [Header("Palettes")]
        [SerializeField] private Color positiveColor = new Color(0.0f, 0.95f, 1.0f, 1.0f);   // Electric Cyan
        [SerializeField] private Color negativeColor = new Color(1.0f, 0.35f, 0.05f, 1.0f);  // Incandescent Amber-Orange
        [SerializeField] private Color zeroColor = new Color(0.25f, 0.3f, 0.35f, 0.4f);       // Inactive Grey
        [SerializeField] private Color passEmerald = new Color(0.0f, 1.0f, 0.45f, 1.0f);     // Harmonic Emerald
        [SerializeField] private Color failRed = new Color(1.0f, 0.2f, 0.2f, 1.0f);          // Mismatch Red

        // Live calculation cached state
        private double _curX1 = 0.0;
        private double _curX2 = 0.0;
        private double _curZ = 0.0;
        private double _curY = 0.0;
        private bool _isPropagating = false;
        private float _propagationProgress = 0.0f; // 0..1 across stages
        private int _activeTargetIndex = -1;

        // Equation display texture / state
        private Camera _mainCam;
        private Texture2D _whiteTex;

        public bool IsPropagating => _isPropagating;

        private void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            _mainCam = Camera.main;

            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }

        private void OnEnable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered += HandleForwardPassTriggered;
                chamberController.OnSingleCaseEvaluated += HandleSingleCaseEvaluated;
                chamberController.OnEvaluationComplete += HandleEvaluationComplete;
                chamberController.OnChamberReset += HandleChamberReset;
            }
        }

        private void OnDisable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered -= HandleForwardPassTriggered;
                chamberController.OnSingleCaseEvaluated -= HandleSingleCaseEvaluated;
                chamberController.OnEvaluationComplete -= HandleEvaluationComplete;
                chamberController.OnChamberReset -= HandleChamberReset;
            }
        }

        private void Start()
        {
            UpdateAxonLines();
            UpdateDialRotations();
            RecalculateLiveValues();
        }

        private void Update()
        {
            UpdateAxonLines();
            UpdateDialRotations();
            UpdateCoreEnergy();
            RecalculateLiveValues();
        }

        public void SetInputTerminalState(double x1, double x2)
        {
            _curX1 = x1;
            _curX2 = x2;
            RecalculateLiveValues();
        }

        private void RecalculateLiveValues()
        {
            if (neuralState == null) return;

            double w1 = neuralState.Cable1Connected ? neuralState.Weight1 : 0.0;
            double w2 = neuralState.Cable2Connected ? neuralState.Weight2 : 0.0;
            double b = neuralState.Bias;

            _curZ = (w1 * _curX1) + (w2 * _curX2) + b;

            _curY = neuralState.Activation switch
            {
                ActivationType.Step => _curZ >= 0.0 ? 1.0 : 0.0,
                ActivationType.Linear => _curZ,
                ActivationType.ReLU => Math.Max(0.0, _curZ),
                ActivationType.Sigmoid => 1.0 / (1.0 + Math.Exp(-_curZ)),
                _ => _curZ
            };
        }

        private void UpdateAxonLines()
        {
            if (neuralState == null || summationCore == null) return;

            // Axon 1 (X1 -> Core)
            if (terminalX1 != null && axon1Renderer != null)
            {
                double w1 = neuralState.Cable1Connected ? neuralState.Weight1 : 0.0;
                ConfigureAxon(axon1Renderer, terminalX1.position, summationCore.position, w1, neuralState.Cable1Connected);
            }

            // Axon 2 (X2 -> Core)
            if (terminalX2 != null && axon2Renderer != null)
            {
                double w2 = neuralState.Cable2Connected ? neuralState.Weight2 : 0.0;
                ConfigureAxon(axon2Renderer, terminalX2.position, summationCore.position, w2, neuralState.Cable2Connected);
            }

            // Bias Conduit (Injector -> Core)
            if (biasInjector != null && biasConduitRenderer != null)
            {
                double b = neuralState.Bias;
                ConfigureAxon(biasConduitRenderer, biasInjector.position, summationCore.position, b, true);
            }

            // Core to Activation Chamber
            if (activationChamber != null && outputBeamRenderer != null && !_isPropagating)
            {
                outputBeamRenderer.enabled = false;
            }
        }

        private void ConfigureAxon(LineRenderer line, Vector3 start, Vector3 end, double weight, bool connected)
        {
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);

            float absVal = (float)Math.Abs(weight);
            float width = connected ? Mathf.Lerp(0.025f, 0.09f, absVal / 2.0f) : 0.015f;
            line.startWidth = width;
            line.endWidth = width * 1.15f;

            Color c;
            if (!connected || absVal < 0.001f)
            {
                c = zeroColor;
            }
            else if (weight > 0)
            {
                c = positiveColor;
            }
            else
            {
                c = negativeColor;
            }

            line.startColor = c;
            line.endColor = c;
        }

        private void UpdateDialRotations()
        {
            if (neuralState == null) return;

            if (w1RotaryDial != null)
            {
                float a1 = Mathf.Lerp(-130f, 130f, Mathf.InverseLerp(-2f, 2f, (float)neuralState.Weight1));
                w1RotaryDial.localRotation = Quaternion.Euler(0, a1, 0);
            }

            if (w2RotaryDial != null)
            {
                float a2 = Mathf.Lerp(-130f, 130f, Mathf.InverseLerp(-2f, 2f, (float)neuralState.Weight2));
                w2RotaryDial.localRotation = Quaternion.Euler(0, a2, 0);
            }

            if (biasKnob != null)
            {
                float ab = Mathf.Lerp(-130f, 130f, Mathf.InverseLerp(-2f, 2f, (float)neuralState.Bias));
                biasKnob.localRotation = Quaternion.Euler(0, ab, 0);
            }
        }

        private void UpdateCoreEnergy()
        {
            if (coreEnergySphere == null) return;

            float pulse = Mathf.Sin(Time.time * 3f) * 0.04f;
            float baseScale = 0.5f;

            if (_curZ > 0)
            {
                float s = baseScale + Mathf.Clamp((float)_curZ * 0.15f, 0f, 0.5f) + pulse;
                coreEnergySphere.localScale = new Vector3(s, s, s);
                if (corePointLight != null)
                {
                    corePointLight.color = positiveColor;
                    corePointLight.intensity = 1.8f + (float)_curZ * 0.8f;
                }
            }
            else
            {
                float s = Mathf.Max(0.2f, baseScale - Mathf.Clamp((float)Math.Abs(_curZ) * 0.1f, 0f, 0.3f)) + pulse * 0.5f;
                coreEnergySphere.localScale = new Vector3(s, s, s);
                if (corePointLight != null)
                {
                    corePointLight.color = negativeColor;
                    corePointLight.intensity = 0.8f + (float)Math.Abs(_curZ) * 0.4f;
                }
            }
        }

        private void HandleForwardPassTriggered()
        {
            if (!gameObject.activeInHierarchy) return;
            StopAllCoroutines();
            StartCoroutine(RoutineForwardPassAnimation(-1));
        }

        private void HandleSingleCaseEvaluated(int caseIndex, CaseDiagnostic diag, bool activationMatches)
        {
            if (!gameObject.activeInHierarchy) return;
            StopAllCoroutines();
            StartCoroutine(RoutineForwardPassAnimation(caseIndex));
        }

        private void HandleEvaluationComplete(PuzzleEvaluation eval)
        {
            // Update terminal state based on current evaluation if needed
        }

        private void HandleChamberReset()
        {
            StopAllCoroutines();
            _isPropagating = false;
            if (outputBeamRenderer != null) outputBeamRenderer.enabled = false;
        }

        /// <summary>
        /// 1.5 second multi-stage physical forward pass sequence:
        /// 1. [0.0 - 0.5s] Packets spawn at X1, X2 and glide down axons.
        /// 2. [0.5 - 0.8s] Packets merge in Summation Core; Bias surge injects vertically. z calculates!
        /// 3. [0.8 - 1.1s] Core discharges into Activation Chamber; crystal ionizes. y calculated!
        /// 4. [1.1 - 1.5s] Laser beam fires into target receptor.
        /// </summary>
        private IEnumerator RoutineForwardPassAnimation(int specificCaseIndex)
        {
            _isPropagating = true;
            _activeTargetIndex = specificCaseIndex;

            // Fetch inputs for this animation
            if (specificCaseIndex >= 0 && chamberController != null && specificCaseIndex < chamberController.TargetReceptors.Count)
            {
                var r = chamberController.TargetReceptors[specificCaseIndex];
                if (r != null)
                {
                    _curX1 = r.InputX1;
                    _curX2 = r.InputX2;
                }
            }
            RecalculateLiveValues();

            Vector3 startX1 = terminalX1 != null ? terminalX1.position : transform.position;
            Vector3 startX2 = terminalX2 != null ? terminalX2.position : transform.position;
            Vector3 corePos = summationCore != null ? summationCore.position : transform.position;
            Vector3 biasPos = biasInjector != null ? biasInjector.position : (corePos - Vector3.up * 0.8f);
            Vector3 actPos = activationChamber != null ? activationChamber.position : (corePos + Vector3.forward * 0.8f);

            // Find target receptor position
            Vector3 targetPos = actPos + Vector3.forward * 5f;
            if (specificCaseIndex >= 0 && chamberController != null && specificCaseIndex < chamberController.TargetReceptors.Count)
            {
                var r = chamberController.TargetReceptors[specificCaseIndex];
                if (r != null) targetPos = r.transform.position;
            }

            // STAGE 1: Axon Packets (0.0s -> 0.5s)
            float t = 0f;
            float durationStage1 = 0.5f;

            while (t < durationStage1)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / durationStage1);
                _propagationProgress = p * 0.35f;

                if (packet1Prefab != null && _curX1 > 0.0)
                {
                    packet1Prefab.gameObject.SetActive(true);
                    packet1Prefab.position = Vector3.Lerp(startX1, corePos, p);
                }
                if (packet2Prefab != null && _curX2 > 0.0)
                {
                    packet2Prefab.gameObject.SetActive(true);
                    packet2Prefab.position = Vector3.Lerp(startX2, corePos, p);
                }

                yield return null;
            }

            if (packet1Prefab != null) packet1Prefab.gameObject.SetActive(false);
            if (packet2Prefab != null) packet2Prefab.gameObject.SetActive(false);

            // STAGE 2: Summation Core Surge & Bias Pulse (0.5s -> 0.8s)
            t = 0f;
            float durationStage2 = 0.3f;
            while (t < durationStage2)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / durationStage2);
                _propagationProgress = 0.35f + (p * 0.25f);

                if (biasPacketPrefab != null)
                {
                    biasPacketPrefab.gameObject.SetActive(true);
                    biasPacketPrefab.position = Vector3.Lerp(biasPos, corePos, p);
                }

                if (corePointLight != null)
                {
                    corePointLight.intensity = 3.5f + Mathf.PingPong(t * 15f, 2f);
                }

                yield return null;
            }
            if (biasPacketPrefab != null) biasPacketPrefab.gameObject.SetActive(false);

            // STAGE 3: Activation Chamber Transformation (0.8s -> 1.1s)
            t = 0f;
            float durationStage3 = 0.3f;
            while (t < durationStage3)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / durationStage3);
                _propagationProgress = 0.6f + (p * 0.2f);

                if (activationLight != null)
                {
                    bool activates = _curY > 0.01;
                    activationLight.color = activates ? passEmerald : failRed;
                    activationLight.intensity = activates ? (3.0f + p * 2.0f) : 0.8f;
                }

                yield return null;
            }

            // STAGE 4: Output Laser to Target Receptor (1.1s -> 1.5s)
            if (outputBeamRenderer != null)
            {
                outputBeamRenderer.enabled = true;
                outputBeamRenderer.positionCount = 2;
                outputBeamRenderer.SetPosition(0, actPos);
                outputBeamRenderer.SetPosition(1, targetPos);

                bool pass = chamberController != null && chamberController.LastEvaluation != null && chamberController.LastEvaluation.Passed;
                Color beamCol = (_curY > 0.01) ? passEmerald : negativeColor;
                outputBeamRenderer.startColor = beamCol;
                outputBeamRenderer.endColor = beamCol;
                outputBeamRenderer.startWidth = 0.06f;
                outputBeamRenderer.endWidth = 0.03f;
            }

            yield return new WaitForSeconds(0.4f);

            if (outputBeamRenderer != null) outputBeamRenderer.enabled = false;
            _isPropagating = false;
        }

        private void OnGUI()
        {
            DrawWorldSpaceEquation();
        }

        private void DrawWorldSpaceEquation()
        {
            if (summationCore == null || neuralState == null) return;
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return;

            Vector3 worldPos = summationCore.position + Vector3.up * 0.95f;
            Vector3 screenPos = _mainCam.WorldToScreenPoint(worldPos);

            if (screenPos.z <= 0.5f || screenPos.z > 20.0f) return;

            float distScale = Mathf.Clamp(1.0f - (screenPos.z / 25.0f), 0.6f, 1.0f);
            float w = 340f * distScale;
            float h = 135f * distScale;
            float x = screenPos.x - (w * 0.5f);
            float y = Screen.height - screenPos.y - (h * 0.5f);

            Rect cardRect = new Rect(x, y, w, h);

            // Background card
            Color bg = new Color(0.02f, 0.04f, 0.08f, 0.88f);
            DrawRect(cardRect, bg);
            DrawBorder(cardRect, new Color(0.0f, 0.85f, 1.0f, 0.75f), 1);

            double w1 = neuralState.Cable1Connected ? neuralState.Weight1 : 0.0;
            double w2 = neuralState.Cable2Connected ? neuralState.Weight2 : 0.0;
            double b = neuralState.Bias;
            double t1 = w1 * _curX1;
            double t2 = w2 * _curX2;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = Mathf.RoundToInt(11 * distScale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = positiveColor }
            };
            GUI.Label(new Rect(x + 5, y + 4, w - 10, 18 * distScale), "SUMMATION CORE // LIVE EQUATION", titleStyle);

            GUIStyle eqStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(10 * distScale),
                normal = { textColor = Color.white }
            };

            float lineH = 18f * distScale;
            float curY = y + 24 * distScale;

            string s1 = $"w1 × x1 = ({w1:+0.0;-0.0;0.0}) × {_curX1:F0} = {t1:+0.00;-0.00;0.00}";
            string s2 = $"w2 × x2 = ({w2:+0.0;-0.0;0.0}) × {_curX2:F0} = {t2:+0.00;-0.00;0.00}";
            string sb = $"b       = {b:+0.00;-0.00;0.00}";
            string sz = $"NET INPUT  z = {t1:+0.00;-0.00;0.00} + {t2:+0.00;-0.00;0.00} + {b:+0.00;-0.00;0.00} = {_curZ:+0.00;-0.00;0.00}";
            string sy = $"OUTPUT     y = {neuralState.Activation}(z) = {_curY:F2}";

            GUI.Label(new Rect(x + 12 * distScale, curY, w - 24, lineH), s1, eqStyle); curY += lineH;
            GUI.Label(new Rect(x + 12 * distScale, curY, w - 24, lineH), s2, eqStyle); curY += lineH;
            GUI.Label(new Rect(x + 12 * distScale, curY, w - 24, lineH), sb, eqStyle); curY += lineH;

            GUIStyle zStyle = new GUIStyle(eqStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = (_curZ >= 0.0 ? positiveColor : negativeColor) }
            };
            GUI.Label(new Rect(x + 12 * distScale, curY, w - 24, lineH), sz, zStyle); curY += lineH;

            GUIStyle yStyle = new GUIStyle(eqStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = (_curY > 0.01 ? passEmerald : failRed) }
            };
            GUI.Label(new Rect(x + 12 * distScale, curY, w - 24, lineH), sy, yStyle);
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _whiteTex);
            GUI.color = prev;
        }

        private void DrawBorder(Rect rect, Color color, int width)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, width), color);
            DrawRect(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            DrawRect(new Rect(rect.x, rect.y, width, rect.height), color);
            DrawRect(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
        }
    }
}
