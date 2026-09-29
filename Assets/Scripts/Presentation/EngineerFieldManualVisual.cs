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
                    return "ASTEROID STRIKE. A.U.R.A. LOST HER WEIGHTS.\n\n" +
                           "SITUATION REPORT:\n" +
                           "An asteroid knocked the ship out of hyperspace. You are alone with A.U.R.A.\n" +
                           "The navigation weights are gone. This bay is the sensor array: one neuron, two beacons.\n\n" +
                           "YOUR OBJECTIVE:\n" +
                           "Wake the array when a radio beacon OR a light signature is present:\n" +
                           " • Both quiet [0,0] -> Output 0\n" +
                           " • Either beacon, or both -> Output 1\n\n" +
                           "Later bays teach a hidden layer, photo labels, and attention. This one is only the OR neuron.";

                case 1:
                    return "THE FOUR SENSOR PINGS\n\n" +
                           "1. A NEURON DOES NOTHING USEFUL UNTIL YOU TEST IT:\n" +
                           "   The lever sends four pings. Each ping is one row of the OR table.\n\n" +
                           "2. THE TABLE:\n" +
                           "   • Quiet: radio 0, light 0 -> stay off\n" +
                           "   • Radio only: radio 1, light 0 -> wake\n" +
                           "   • Light only: radio 0, light 1 -> wake\n" +
                           "   • Both beacons: radio 1, light 1 -> wake\n\n" +
                           "A green ping matched. A red ping did not. The picture above the console is this same neuron.";

                case 2:
                    return "PLANETARY SENSORS (x) & SENSITIVITY WEIGHTS (w)\n\n" +
                           "1. SENSOR CONDUITS (x1, x2):\n" +
                           "   • Conduit 1 (x1): Radio beacon (0 = silent, 1 = beacon)\n" +
                           "   • Conduit 2 (x2): Light signature (0 = dark, 1 = signature)\n" +
                           "   * A disconnected cable forces that beacon to 0.\n\n" +
                           "2. WEIGHT REGULATORS (w1, w2) — SEPARATE NUMBERS:\n" +
                           "   W1 does not write W2. Neither writes the bias.\n" +
                           "   • Higher weight: that beacon counts for more.\n" +
                           "   • Zero weight: that beacon is ignored.\n" +
                           "   • Negative weight: that beacon pushes the sum down.\n\n" +
                           "COMBINED SENSOR SIGNAL:\n" +
                           "   Raw Warp Energy = (x1 × w1) + (x2 × w2)";

                case 3:
                    return "COSMIC NOISE FILTER (b) & THE WARP CRYSTAL\n\n" +
                           "1. WHY DO WE NEED BIAS (b)? — COSMIC BACKGROUND FILTER:\n" +
                           "   Deep space is never silent; cosmic background radiation produces constant static.\n" +
                           "   A negative Bias sets a resistance barrier that filters out void noise so the ship doesn't blindly jump into the void.\n" +
                           "   - When looking at empty void (0, 0), energy stays below 0 (no jump).\n" +
                           "   - When either beacon is present, the sum has to be able to reach zero.\n\n" +
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
                    _sb.AppendLine($"W1 (Radio): {w1:+0.0;-0.0;0.0} | W2 (Light): {w2:+0.0;-0.0;0.0} | Bias: {b:+0.0;-0.0;0.0} | Crystal: {actName}\n");
                    _sb.AppendLine("TELEMETRY ENERGY RESPONSE (z = w1·x1 + w2·x2 + b):");

                    double z1 = b;
                    double z2 = w2 + b;
                    double z3 = w1 + b;
                    double z4 = w1 + w2 + b;

                    _sb.AppendLine($" • [Quiet (0,0)]: Energy z = {z1:+0.0;-0.0;0.0} (want the sum below 0)");
                    _sb.AppendLine($" • [Radio (0,1)]: Energy z = {z2:+0.0;-0.0;0.0} (want the sum at 0 or above)");
                    _sb.AppendLine($" • [Light (1,0)]: Energy z = {z3:+0.0;-0.0;0.0} (want the sum at 0 or above)");
                    _sb.AppendLine($" • [Both  (1,1)]: Energy z = {z4:+0.0;-0.0;0.0} (want the sum at 0 or above)");
                    _sb.AppendLine();
                    _sb.AppendLine("Pull the lever to send the four pings. The test, not this page, decides if the array is awake.");
                    return _sb.ToString();

                default:
                    return string.Empty;
            }
        }
    }
}
