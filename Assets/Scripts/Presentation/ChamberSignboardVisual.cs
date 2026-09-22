using System;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Backlit wall-mounted Portal Test Chamber Signboard.
    /// Provides diegetic environmental telemetry and clean status feedback:
    /// - Chamber Number ("01")
    /// - 4 Circular Door Lock LEDs (amber = locked, emerald = harmonized)
    /// - Minimalist iconic pictograms (Cable -> Dial -> Crystal -> Gate)
    /// Strictly presentation layer: observes Gameplay state machines only.
    /// </summary>
    public class ChamberSignboardVisual : MonoBehaviour
    {
        [Header("State Sources")]
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private NeuralState neuralState;

        [Header("Signboard 3D Components")]
        [SerializeField] private TextMesh chamberNumberText;
        [SerializeField] private TextMesh chamberSubtitleText;
        [SerializeField] private Renderer[] caseStatusLeds = new Renderer[4];
        [SerializeField] private Light signBacklight;

        [Header("Colors")]
        [SerializeField] private Color lockedAmber = new Color(1.0f, 0.6f, 0.0f, 1.0f);
        [SerializeField] private Color unlockedEmerald = new Color(0.0f, 1.0f, 0.45f, 1.0f);
        [SerializeField] private Color signGlowCyan = new Color(0.0f, 0.85f, 1.0f, 1.0f);

        private void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
        }

        private void OnEnable()
        {
            if (neuralState != null)
            {
                neuralState.OnStateMutated += UpdateSignStatus;
            }
            if (chamberController != null)
            {
                chamberController.OnEvaluationComplete += HandleEvaluationComplete;
                chamberController.OnChamberReset += HandleChamberReset;
            }
        }

        private void OnDisable()
        {
            if (neuralState != null)
            {
                neuralState.OnStateMutated -= UpdateSignStatus;
            }
            if (chamberController != null)
            {
                chamberController.OnEvaluationComplete -= HandleEvaluationComplete;
                chamberController.OnChamberReset -= HandleChamberReset;
            }
        }

        private void Start()
        {
            if (chamberNumberText != null)
            {
                chamberNumberText.text = "01";
                chamberNumberText.color = signGlowCyan;
            }
            if (chamberSubtitleText != null)
            {
                chamberSubtitleText.text = "NEURAL CALIBRATION // OR GATE";
                chamberSubtitleText.color = Color.white;
            }

            UpdateSignStatus();
        }

        private void HandleEvaluationComplete(PuzzleEvaluation eval)
        {
            UpdateSignStatus();
        }

        private void HandleChamberReset()
        {
            UpdateSignStatus();
        }

        public void UpdateSignStatus()
        {
            if (neuralState == null) return;

            double w1 = neuralState.Cable1Connected ? neuralState.Weight1 : 0.0;
            double w2 = neuralState.Cable2Connected ? neuralState.Weight2 : 0.0;
            double b = neuralState.Bias;
            bool isStep = (neuralState.Activation == ActivationType.Step);

            double[][] cases = new double[][]
            {
                new double[] { 0.0, 0.0, 0.0 },
                new double[] { 0.0, 1.0, 1.0 },
                new double[] { 1.0, 0.0, 1.0 },
                new double[] { 1.0, 1.0, 1.0 }
            };

            int passedCount = 0;

            for (int i = 0; i < 4; i++)
            {
                double x1 = cases[i][0];
                double x2 = cases[i][1];
                double target = cases[i][2];

                double z = (x1 * w1) + (x2 * w2) + b;
                double y = isStep ? (z >= 0.0 ? 1.0 : 0.0) : z;
                bool pass = (Math.Abs(y - target) < 1e-4) && isStep;

                if (pass) passedCount++;

                if (i < caseStatusLeds.Length && caseStatusLeds[i] != null)
                {
                    Color ledCol = pass ? unlockedEmerald : lockedAmber;
                    caseStatusLeds[i].sharedMaterial.SetColor("_EmissionColor", ledCol * 2.5f);
                }
            }

            if (signBacklight != null)
            {
                signBacklight.color = (passedCount == 4) ? unlockedEmerald : signGlowCyan;
            }
        }
    }
}
