using System;
using System.Text;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// In-world educational tablet and diagnostic field manual designed for complete beginners.
    /// Grounds the single artificial neuron (Perceptron) in a tangible facility emergency scenario:
    /// "The Sector 01 Hazard Containment Purge".
    /// 
    /// Explains:
    /// - WHY the facility is in emergency lockdown and why the AI needs calibration.
    /// - WHY we upload/transmit test data packets (to evaluate the neuron against 4 real hazard scenarios).
    /// - WHAT the 2 sensor inputs represent (x1 = Radiation Detector, x2 = Bio-Hazard Leak Detector).
    /// - WHY we tune Weights (volume knobs / sensor sensitivity).
    /// - WHY we tune Bias (noise resistance threshold).
    /// - WHY we need an Activation Function / Step Crystal (to convert messy continuous energy into a clean binary lockdown switch).
    /// </summary>
    public class EngineerFieldManualVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private NeuralState neuralState;

        [Header("Visual Elements")]
        [SerializeField] private Transform tabletRoot;
        [SerializeField] private Light screenGlowLight;
        [SerializeField] private bool showScreenOverlay = false;

        [Header("Manual Navigation")]
        [SerializeField] private int activePageIndex = 0;

        private const int TotalPages = 5;

        private static readonly string[] PageTitles = new string[]
        {
            "LOG 01: EMERGENCY SCENARIO & THE OR GATE",
            "LOG 02: WHY WE UPLOAD DATA (TRAINING SENSORS)",
            "LOG 03: SENSORS (x) & SENSITIVITY WEIGHTS (w)",
            "LOG 04: NOISE BIAS (b) & THE DECISION CRYSTAL",
            "LOG 05: LIVE TELEMETRY & CALIBRATION FORMULA"
        };

        private readonly StringBuilder _sb = new StringBuilder(512);

        public int ActivePageIndex => activePageIndex;
        public string CurrentPageTitle => PageTitles[Mathf.Clamp(activePageIndex, 0, TotalPages - 1)];

        private void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T)) NextPage();
            if (Input.GetKeyDown(KeyCode.Q)) PreviousPage();
            if (Input.GetKeyDown(KeyCode.H)) showScreenOverlay = !showScreenOverlay;
        }

        public void NextPage()
        {
            activePageIndex = (activePageIndex + 1) % TotalPages;
        }

        public void PreviousPage()
        {
            activePageIndex = (activePageIndex - 1 + TotalPages) % TotalPages;
        }

        public void SetPage(int page)
        {
            activePageIndex = Mathf.Clamp(page, 0, TotalPages - 1);
        }

        public string GetPageContent(int page)
        {
            switch (page)
            {
                case 0:
                    return "CRITICAL MISSION: PREVENT SYNAPSE-GPT COGNITIVE MELTDOWN\n\n" +
                           "WHY WE ARE HERE:\n" +
                           "Deep inside Mainframe CONVERGENCE-01, the world's foundation AI (SYNAPSE-GPT) has suffered severe synaptic drift.\n" +
                           "The foundational Perception Perceptron has corrupted. The AI can no longer distinguish between clean background signals and lethal radiation/toxin leaks!\n" +
                           "As a result, the AI is hallucinating phantom crises, triggering global failsafe blackouts across Earth and Deep Space.\n\n" +
                           "YOUR OBJECTIVE:\n" +
                           "Recalibrate the single Artificial Neuron controlling the sensory gateway so it acts as an automated OR LOGIC GATE:\n" +
                           " • Clean Room [0,0] -> Output 0 (ALL CLEAR / Keep doors open)\n" +
                           " • Any Hazard Detected -> Output 1 (QUARANTINE / Seal the blast doors!)\n\n" +
                           "Press [T] to read Log 02: Why we must transmit test data packets.";

                case 1:
                    return "WHY ARE WE UPLOADING DATA INTO THE NEURAL NET?\n\n" +
                           "1. AN ARTIFICIAL NEURON CANNOT LEARN WITHOUT DATA:\n" +
                           "   A neural network without data is just blank mathematical weights.\n" +
                           "   To prove the AI will not hallucinate in production, we must test it against all 4 recorded environmental conditions.\n\n" +
                           "2. THE 4 CRITICAL SENSORY CONDITIONS (TRUTH TABLE):\n" +
                           "   • Case 1: [0, 0] Clean Room (Rad=0, Bio=0) -> Safe! Target = 0\n" +
                           "   • Case 2: [0, 1] Bio-Hazard Leak (Rad=0, Bio=1) -> Hazard! Target = 1\n" +
                           "   • Case 3: [1, 0] Radiation Flare (Rad=1, Bio=0) -> Hazard! Target = 1\n" +
                           "   • Case 4: [1, 1] Dual Critical Breach (Rad=1, Bio=1) -> Hazard! Target = 1\n\n" +
                           "When you pull the CLOCK LEVER, the system transmits these 4 data packets through the 3D synapses to verify the AI makes the right decision every time!";

                case 2:
                    return "SENSOR INPUTS (x) & SENSITIVITY WEIGHTS (w)\n\n" +
                           "1. SENSOR CONDUITS (x1, x2):\n" +
                           "   • Cable 1 (x1): Radiation Detector (0 = Safe, 1 = Alert)\n" +
                           "   • Cable 2 (x2): Bio-Hazard Leak Detector (0 = Safe, 1 = Alert)\n" +
                           "   * Both conduits must be physically connected to stream sensor telemetry.\n\n" +
                           "2. WEIGHT REGULATORS (w1, w2) — SENSITIVITY MULTIPLIERS:\n" +
                           "   Think of weights as AMPLIFIERS or VOLUME knobs:\n" +
                           "   • Higher Weight: A detected hazard generates strong danger energy.\n" +
                           "   • Zero Weight: Sensor signal is ignored.\n" +
                           "   • Negative Weight: Sensor signal dampens activation.\n\n" +
                           "COMBINED SENSOR SIGNAL:\n" +
                           "   Raw Danger Energy = (x1 × w1) + (x2 × w2)";

                case 3:
                    return "NOISE BIAS (b) & THE ACTIVATION CRYSTAL\n\n" +
                           "1. WHY DO WE NEED BIAS (b)? — BACKGROUND NOISE BUFFER:\n" +
                           "   Sensors in clean rooms generate residual background static.\n" +
                           "   A negative Bias acts as a resistance barrier: it suppresses background noise so clean rooms don't trigger false alarms.\n" +
                           "   - When inputs are quiet (0, 0), the total energy remains below the activation threshold.\n" +
                           "   - When a genuine hazard occurs, sensor energy must be strong enough to overcome this resistance.\n\n" +
                           "2. THE ACTIVATION CRYSTAL — DECISION GATE:\n" +
                           "   Summed energy produces continuous numbers. But the facility lockdown system requires a crisp binary switch:\n" +
                           "   • STEP CRYSTAL: Outputs 1 (Lockdown Alarm) if Total Energy ≥ 0, and 0 (Safe) if Total Energy < 0.";

                case 4:
                    if (neuralState == null) return "Connecting to station telemetry...";
                    double w1 = neuralState.Cable1Connected ? neuralState.Weight1 : 0.0;
                    double w2 = neuralState.Cable2Connected ? neuralState.Weight2 : 0.0;
                    double b = neuralState.Bias;
                    string actName = neuralState.Activation.ToString();

                    _sb.Length = 0;
                    _sb.AppendLine("LIVE HAZARD CLASSIFIER STATUS:");
                    _sb.AppendLine($"W1 (Radiation): {w1:+0.0;-0.0;0.0} | W2 (BioLeak): {w2:+0.0;-0.0;0.0} | Bias (Filter): {b:+0.0;-0.0;0.0} | Crystal: {actName}\n");
                    _sb.AppendLine("SCENARIO ENERGY RESPONSE (z = w1·x1 + w2·x2 + b):");

                    double z1 = b;
                    double z2 = w2 + b;
                    double z3 = w1 + b;
                    double z4 = w1 + w2 + b;

                    _sb.AppendLine($" • [Clean Room  (0,0)]: Energy z = {z1:+0.0;-0.0;0.0} (Target: Safe < 0)");
                    _sb.AppendLine($" • [Bio-Leak    (0,1)]: Energy z = {z2:+0.0;-0.0;0.0} (Target: Alarm ≥ 0)");
                    _sb.AppendLine($" • [Radiation   (1,0)]: Energy z = {z3:+0.0;-0.0;0.0} (Target: Alarm ≥ 0)");
                    _sb.AppendLine($" • [Dual Hazard (1,1)]: Energy z = {z4:+0.0;-0.0;0.0} (Target: Alarm ≥ 0)");
                    _sb.AppendLine();
                    _sb.AppendLine("CALIBRATION GOAL: Calibrate Dials so Clean Room stays below 0 while all hazard conditions reach or exceed 0. Pull the Clock Lever to verify.");
                    return _sb.ToString();

                default:
                    return string.Empty;
            }
        }
    }
}
