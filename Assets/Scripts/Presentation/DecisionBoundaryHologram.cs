using System;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Interactive 3D Holographic Visualizer for Level 1.
    /// Projects a real-time 2D Decision Boundary classification grid (w1*x1 + w2*x2 + b = 0)
    /// alongside a 1D Activation Function curve graph (y = f(z)).
    /// Directly illustrates how neural parameters separate binary logic cases and why
    /// step thresholding is required. Observes state; does not evaluate or decide correctness.
    /// </summary>
    public class DecisionBoundaryHologram : MonoBehaviour
    {
        [Header("State Listeners")]
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;

        [Header("Display Settings")]
        [SerializeField] private bool showHologram = true;
        [SerializeField] private Vector2 screenPosition = new Vector2(20, 20);

        // Styling
        private readonly Color _panelBg = new Color(0.02f, 0.05f, 0.10f, 0.92f);
        private readonly Color _panelBorder = new Color(0.0f, 0.85f, 1.0f, 0.95f);
        private readonly Color _cyanGlow = new Color(0.0f, 0.95f, 1.0f, 1.0f);
        private readonly Color _emeraldGlow = new Color(0.0f, 1.0f, 0.45f, 1.0f);
        private readonly Color _amberGlow = new Color(1.0f, 0.7f, 0.0f, 1.0f);
        private readonly Color _redGlow = new Color(1.0f, 0.25f, 0.25f, 1.0f);
        private readonly Color _gridLineColor = new Color(0.15f, 0.35f, 0.5f, 0.4f);

        private Texture2D _whiteTex;

        private void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();

            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }

        private void OnGUI()
        {
            if (!showHologram || neuralState == null) return;

            DrawHologramWindow();
        }

        private void DrawHologramWindow()
        {
            float w = 500f;
            float h = 265f;
            // Position on right side of screen
            float x = Screen.width - w - 20f;
            float y = 20f;
            Rect rect = new Rect(x, y, w, h);

            // Frame
            DrawRect(rect, _panelBg);
            DrawBorder(rect, _panelBorder, 2);

            // Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _cyanGlow }
            };
            GUI.Label(new Rect(x + 12, y + 8, w - 24, 20), "NEURAL GEOMETRY // 2D CLASSIFICATION & ACTIVATION", titleStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.6f, 0.75f, 0.85f) }
            };
            GUI.Label(new Rect(x + 12, y + 26, w - 24, 16), "OR Gate Decision Boundary: w1*x1 + w2*x2 + b = 0", subStyle);

            // 1. Left Sub-Panel: 2D Feature Space Grid (x1, x2)
            Rect gridRect = new Rect(x + 16, y + 46, 210, 190);
            DrawFeatureSpaceGrid(gridRect);

            // Divider
            DrawRect(new Rect(x + 238, y + 46, 1, 190), new Color(0.0f, 0.8f, 1.0f, 0.3f));

            // 2. Right Sub-Panel: 1D Activation Function Graph y = f(z)
            Rect graphRect = new Rect(x + 250, y + 46, 230, 190);
            DrawActivationCurveGraph(graphRect);
        }

        private void DrawFeatureSpaceGrid(Rect r)
        {
            // Background box
            DrawRect(r, new Color(0.01f, 0.03f, 0.07f, 0.85f));
            DrawBorder(r, new Color(0.0f, 0.5f, 0.8f, 0.5f), 1);

            float pad = 24f;
            float graphW = r.width - (pad * 2);
            float graphH = r.height - (pad * 2);

            // Axes
            float originX = r.x + pad;
            float originY = r.y + r.height - pad;

            // X1 axis (horizontal)
            DrawLine(new Vector2(originX, originY), new Vector2(originX + graphW, originY), _gridLineColor, 1.5f);
            // X2 axis (vertical)
            DrawLine(new Vector2(originX, originY), new Vector2(originX, originY - graphH), _gridLineColor, 1.5f);

            GUIStyle axisLbl = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                normal = { textColor = _cyanGlow }
            };
            GUI.Label(new Rect(originX + graphW - 16, originY + 2, 20, 16), "X1", axisLbl);
            GUI.Label(new Rect(originX - 18, originY - graphH - 4, 20, 16), "X2", axisLbl);

            // Coordinate mapping: (0,0) is origin, (1,1) is (originX + graphW, originY - graphH)
            Vector2 p00 = new Vector2(originX, originY);
            Vector2 p01 = new Vector2(originX, originY - graphH * 0.85f);
            Vector2 p10 = new Vector2(originX + graphW * 0.85f, originY);
            Vector2 p11 = new Vector2(originX + graphW * 0.85f, originY - graphH * 0.85f);

            // Draw decision boundary line: w1*x1 + w2*x2 + b = 0
            double w1 = neuralState.Cable1Connected ? neuralState.Weight1 : 0.0;
            double w2 = neuralState.Cable2Connected ? neuralState.Weight2 : 0.0;
            double b = neuralState.Bias;

            DrawDecisionBoundaryLine(originX, originY, graphW * 0.85f, graphH * 0.85f, w1, w2, b);

            // Draw 4 Truth Table Points
            // Case 1: [0, 0] -> Target 0 (Amber/Red)
            DrawDataPoint(p00, "(0,0)", 0, (w1 * 0 + w2 * 0 + b));
            // Case 2: [0, 1] -> Target 1 (Emerald/Cyan)
            DrawDataPoint(p01, "(0,1)", 1, (w1 * 0 + w2 * 1 + b));
            // Case 3: [1, 0] -> Target 1 (Emerald/Cyan)
            DrawDataPoint(p10, "(1,0)", 1, (w1 * 1 + w2 * 0 + b));
            // Case 4: [1, 1] -> Target 1 (Emerald/Cyan)
            DrawDataPoint(p11, "(1,1)", 1, (w1 * 1 + w2 * 1 + b));
        }

        private void DrawDecisionBoundaryLine(float ox, float oy, float scaleX, float scaleY, double w1, double w2, double b)
        {
            // If weights are nearly zero
            if (Math.Abs(w1) < 0.001 && Math.Abs(w2) < 0.001) return;

            // Line equation: w1*x1 + w2*x2 + b = 0 => x2 = -(w1*x1 + b) / w2
            Vector2 startPt, endPt;

            if (Math.Abs(w2) > 0.001)
            {
                // Calculate x2 at x1 = -0.3 and x1 = 1.3
                float x1_a = -0.3f;
                float x2_a = (float)(-(w1 * x1_a + b) / w2);

                float x1_b = 1.3f;
                float x2_b = (float)(-(w1 * x1_b + b) / w2);

                startPt = new Vector2(ox + x1_a * scaleX, oy - x2_a * scaleY);
                endPt = new Vector2(ox + x1_b * scaleX, oy - x2_b * scaleY);
            }
            else
            {
                // Vertical line: x1 = -b / w1
                float x1_v = (float)(-b / w1);
                startPt = new Vector2(ox + x1_v * scaleX, oy - (-0.3f * scaleY));
                endPt = new Vector2(ox + x1_v * scaleX, oy - (1.3f * scaleY));
            }

            DrawLine(startPt, endPt, _cyanGlow, 2.5f);
        }

        private void DrawDataPoint(Vector2 pos, string label, int target, double zVal)
        {
            bool correctlyClassified = (target == 1 && zVal >= 0) || (target == 0 && zVal < 0);
            Color dotColor = correctlyClassified ? _emeraldGlow : _redGlow;

            float r = 5f;
            DrawRect(new Rect(pos.x - r, pos.y - r, r * 2, r * 2), dotColor);
            DrawBorder(new Rect(pos.x - r - 1, pos.y - r - 1, r * 2 + 2, r * 2 + 2), Color.white, 1);

            GUIStyle pStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                normal = { textColor = dotColor }
            };
            GUI.Label(new Rect(pos.x + 6, pos.y - 8, 45, 16), label, pStyle);
        }

        private void DrawActivationCurveGraph(Rect r)
        {
            DrawRect(r, new Color(0.01f, 0.03f, 0.07f, 0.85f));
            DrawBorder(r, new Color(0.0f, 0.5f, 0.8f, 0.5f), 1);

            ActivationType act = neuralState.Activation;

            GUIStyle title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _cyanGlow }
            };
            GUI.Label(new Rect(r.x + 8, r.y + 6, r.width - 16, 16), $"Activation Function: {act.ToString().ToUpper()}", title);

            float padX = 24f;
            float padY = 32f;
            float gW = r.width - (padX * 2);
            float gH = r.height - padY - 24f;

            float midX = r.x + padX + (gW * 0.5f); // z = 0
            float midY = r.y + padY + (gH * 0.5f); // y = 0.5 for Sigmoid, or y = 0 for others

            // Axes
            DrawLine(new Vector2(r.x + padX, midY), new Vector2(r.x + padX + gW, midY), _gridLineColor, 1.0f);
            DrawLine(new Vector2(midX, r.y + padY), new Vector2(midX, r.y + padY + gH), _gridLineColor, 1.0f);

            GUIStyle ax = new GUIStyle(GUI.skin.label)
            {
                fontSize = 8,
                normal = { textColor = new Color(0.6f, 0.7f, 0.8f) }
            };
            GUI.Label(new Rect(r.x + padX + gW - 12, midY + 2, 16, 12), "z", ax);
            GUI.Label(new Rect(midX + 2, r.y + padY - 10, 16, 12), "y", ax);

            // Plot curve points: z from -2.5 to 2.5
            int steps = 30;
            Vector2 prevPt = Vector2.zero;

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float z = Mathf.Lerp(-2.5f, 2.5f, t);
                float yVal = EvaluateActivation(act, z);

                // Map (z, yVal) to screen coordinates
                float px = midX + (z / 2.5f) * (gW * 0.5f);
                float py = midY - (yVal - 0.5f) * (gH * 0.7f);

                Vector2 curPt = new Vector2(px, py);
                if (i > 0)
                {
                    DrawLine(prevPt, curPt, _amberGlow, 2.0f);
                }
                prevPt = curPt;
            }

            // Explanatory footnote
            GUIStyle noteStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                wordWrap = true,
                normal = { textColor = (act == ActivationType.Step ? _emeraldGlow : _amberGlow) }
            };

            string note = act switch
            {
                ActivationType.Step => "Step: Threshold cliff at z=0. Outputs clean discrete {0, 1}. REQUIRED.",
                ActivationType.Linear => "Linear: Continuous analog pass-through (y=z). Cannot bound to {0, 1}.",
                ActivationType.ReLU => "ReLU: Clamps negative to 0, but positive grows unbounded (y=z).",
                ActivationType.Sigmoid => "Sigmoid: Smooth probability curve (0, 1). Asymptotic; never exact 1.",
                _ => ""
            };

            GUI.Label(new Rect(r.x + 8, r.y + r.height - 34, r.width - 16, 30), note, noteStyle);
        }

        private float EvaluateActivation(ActivationType act, float z)
        {
            return act switch
            {
                ActivationType.Step => z >= 0f ? 1f : 0f,
                ActivationType.Linear => z,
                ActivationType.ReLU => Mathf.Max(0f, z),
                ActivationType.Sigmoid => 1f / (1f + Mathf.Exp(-z)),
                _ => z
            };
        }

        private void DrawLine(Vector2 p1, Vector2 p2, Color color, float width)
        {
            Color prev = GUI.color;
            GUI.color = color;
            float angle = Mathf.Atan2(p2.y - p1.y, p2.x - p1.x) * Mathf.Rad2Deg;
            float length = Vector2.Distance(p1, p2);

            GUIUtility.RotateAroundPivot(angle, p1);
            GUI.DrawTexture(new Rect(p1.x, p1.y - width * 0.5f, length, width), _whiteTex);
            GUIUtility.RotateAroundPivot(-angle, p1);
            GUI.color = prev;
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
