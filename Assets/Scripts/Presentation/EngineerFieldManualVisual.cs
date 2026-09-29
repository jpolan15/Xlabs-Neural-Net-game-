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
            "LOG 01: EMERGENCY WARP FAILURE & EARTH RETURN",
            "LOG 02: WHY WE FEED EARTH IMAGES (TRAINING)",
            "LOG 03: PLANETARY SENSORS (x) & WEIGHTS (w)",
            "LOG 04: COSMIC NOISE FILTER (b) & WARP CRYSTAL",
            "LOG 05: LIVE TELEMETRY & WARP LOCK STATUS"
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
                    return "CRITICAL EMERGENCY: USS CONVERGENCE WARP BREAKDOWN\n\n" +
                           "SITUATION REPORT:\n" +
                           "The starship's hyperspace coils have suffered catastrophic failure. We are adrift in uncharted deep space.\n" +
                           "The ship's emergency warp navigation AI has lost starfield lock. To save the vessel and return home safely, we must feed the neural net images and telemetry of EARTH.\n\n" +
                           "YOUR OBJECTIVE:\n" +
                           "Calibrate the single Artificial Neuron governing the hyperspace drive so it locks onto Earth telemetry:\n" +
                           " • Deep Space Void [0,0] -> Output 0 (FILTER NOISE / Hold Jump)\n" +
                           " • Earth Telemetry Detected -> Output 1 (WARP LOCK / Open Jump Corridor)\n\n" +
                           "Press [T] to read Log 02: Why we feed Earth planetary telemetry.";

                case 1:
                    return "WHY ARE WE FEEDING EARTH IMAGES TO THE NEURAL NET?\n\n" +
                           "1. AN ARTIFICIAL NEURON CANNOT NAVIGATE WITHOUT TRAINING DATA:\n" +
                           "   Without calibrated weights, the AI cannot differentiate between barren cosmic radiation and Earth's planetary signature.\n" +
                           "   To ensure a safe jump home, we feed the network 4 diagnostic planetary sensor feeds.\n\n" +
                           "2. THE 4 PLANETARY SENSOR FEEDS (TRUTH TABLE):\n" +
                           "   • Case 1: [0, 0] Deep Space Void (Land=0, Atmos=0) -> Target = 0 (Filter noise!)\n" +
                           "   • Case 2: [0, 1] Earth Atmosphere (Land=0, Atmos=1) -> Target = 1 (Lock jump vector!)\n" +
                           "   • Case 3: [1, 0] Earth Continents (Land=1, Atmos=0) -> Target = 1 (Lock jump vector!)\n" +
                           "   • Case 4: [1, 1] Earth Orbital Fix (Land=1, Atmos=1) -> Target = 1 (Lock jump vector!)\n\n" +
                           "When you pull the JUMP LEVER, the system streams these 4 sensor feeds through the 3D synapses to verify the AI locks onto Earth!";

                case 2:
                    return "PLANETARY SENSORS (x) & SENSITIVITY WEIGHTS (w)\n\n" +
                           "1. SENSOR CONDUITS (x1, x2):\n" +
                           "   • Conduit 1 (x1): Continental Landmass Sensor (0 = Void, 1 = Continent Detected)\n" +
                           "   • Conduit 2 (x2): Atmospheric O2/N2 Sensor (0 = Void, 1 = Atmosphere Detected)\n" +
                           "   * Both conduits must be plugged into the console to stream planetary data.\n\n" +
                           "2. WEIGHT REGULATORS (w1, w2) — SENSITIVITY MULTIPLIERS:\n" +
                           "   Think of weights as AMPLIFIERS for planetary features:\n" +
                           "   • Higher Weight: A detected Earth feature generates strong warp lock energy.\n" +
                           "   • Zero Weight: Planetary sensor is ignored.\n" +
                           "   • Negative Weight: Sensor signal dampens activation.\n\n" +
                           "COMBINED SENSOR SIGNAL:\n" +
                           "   Raw Warp Energy = (x1 × w1) + (x2 × w2)";

                case 3:
                    return "COSMIC NOISE FILTER (b) & THE WARP CRYSTAL\n\n" +
                           "1. WHY DO WE NEED BIAS (b)? — COSMIC BACKGROUND FILTER:\n" +
                           "   Deep space is never silent; cosmic background radiation produces constant static.\n" +
                           "   A negative Bias sets a resistance barrier that filters out void noise so the ship doesn't blindly jump into the void.\n" +
                           "   - When looking at empty void (0, 0), energy stays below 0 (no jump).\n" +
                           "   - When an Earth feature appears, sensor energy overcomes the barrier and triggers warp lock.\n\n" +
                           "2. THE ACTIVATION CRYSTAL — WARP LOCK GATE:\n" +
                           "   The hyperspace coils require a decisive binary engage switch:\n" +
                           "   • STEP CRYSTAL: Outputs 1 (Warp Lock) if Energy ≥ 0, and 0 (Hold) if Energy < 0.";

                case 4:
                    if (neuralState == null) return "Connecting to bridge telemetry...";
                    double w1 = neuralState.Cable1Connected ? neuralState.Weight1 : 0.0;
                    double w2 = neuralState.Cable2Connected ? neuralState.Weight2 : 0.0;
                    double b = neuralState.Bias;
                    string actName = neuralState.Activation.ToString();

                    _sb.Length = 0;
                    _sb.AppendLine("LIVE WARP NAVIGATION AI STATUS:");
                    _sb.AppendLine($"W1 (Landmass): {w1:+0.0;-0.0;0.0} | W2 (Atmos): {w2:+0.0;-0.0;0.0} | Bias (Filter): {b:+0.0;-0.0;0.0} | Crystal: {actName}\n");
                    _sb.AppendLine("TELEMETRY ENERGY RESPONSE (z = w1·x1 + w2·x2 + b):");

                    double z1 = b;
                    double z2 = w2 + b;
                    double z3 = w1 + b;
                    double z4 = w1 + w2 + b;

                    _sb.AppendLine($" • [Deep Space Void  (0,0)]: Energy z = {z1:+0.0;-0.0;0.0} (Target: Void < 0)");
                    _sb.AppendLine($" • [Earth Atmosphere (0,1)]: Energy z = {z2:+0.0;-0.0;0.0} (Target: Lock ≥ 0)");
                    _sb.AppendLine($" • [Earth Continents (1,0)]: Energy z = {z3:+0.0;-0.0;0.0} (Target: Lock ≥ 0)");
                    _sb.AppendLine($" • [Earth Orbital Fix(1,1)]: Energy z = {z4:+0.0;-0.0;0.0} (Target: Lock ≥ 0)");
                    _sb.AppendLine();
                    _sb.AppendLine("CALIBRATION GOAL: Tune Dials so Deep Space stays below 0 while all Earth telemetry reaches or exceeds 0. Pull Jump Lever to verify.");
                    return _sb.ToString();

                default:
                    return string.Empty;
            }
        }
    }
}
