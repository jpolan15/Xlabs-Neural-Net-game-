using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Explainer section of the Neural Ride builder: the panel under the chapter card and one small diagram per
    /// narration cue (RideNarrationData's {cue} tags), so the rider sees what the Guide is explaining while it is
    /// said, instead of sitting through it. Diagrams are flat bars, the ride's icons, halos and text, all built
    /// here; the motions are ExplainerMotion, the weight demo is WeightDemoView. The build fails if a cue in the
    /// script has nothing to show, or if any explainer text is below the 1.5 degree reading floor.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        // 4.2 m out and 12.5 degrees down: under the chapter card (5 m), and nearer, so the eye reads title, picture,
        // subtitle, top to bottom. Its top edge is about 4 degrees under the horizon, so the network's heart stays in view
        // above it; its edges sit inside the viewing cone (about 27 degrees either side, 4 to 21 degrees down) and above
        // the dash's top edge (about 26 degrees down).
        private const float ExplainerElevation = -12.5f;
        private const float ExplainerDistance = 4.2f;
        private static readonly Vector2 ExplainerSize = new Vector2(4.2f, 1.2f);

        /// <summary>The smallest explainer type: 1.5 degrees at 4.8 m, the ride's reading floor (ADR-013); at 4.2 m it reads 1.8 degrees.</summary>
        private const float ExplainerMinText = 0.13f;

        // Cues acted on elsewhere: the big network (NeuralCoreView) and the lever arrows (HintArrowView).
        private static readonly string[] NetworkCues = { "net_wake", "net_broken", "neurons", "links", "heart", "billions", "liftoff", "jfk_build", "jfk_hard" };
        private static readonly string[] HintCues = { "orange", "go" };

        private static readonly List<KeyValuePair<string, GameObject>> ExplainerVisuals = new List<KeyValuePair<string, GameObject>>();
        private static Transform _visualsRoot;

        private static void MakeExplainerMaterials()
        {
            RideTheme t = _theme;
            Texture2D glow = GlowTexture();
            // Halos sit on the panel, so they draw after its plate (the Glow materials draw before plates on purpose).
            M("HaloCyan", new Color(t.cyan.r, t.cyan.g, t.cyan.b, 0.8f), Kind.Additive, glow);
            M("HaloAmber", new Color(t.amber.r, t.amber.g, t.amber.b, 0.8f), Kind.Additive, glow);
            M("HaloWhite", new Color(1f, 1f, 1f, 0.95f), Kind.Additive, glow);
            M("TileDim", new Color(t.cyan.r, t.cyan.g, t.cyan.b, 0.07f), Kind.Alpha);
            M("TileLit", new Color(t.cyan.r, t.cyan.g, t.cyan.b, 0.24f), Kind.Alpha);
            Icon("DialCyan", "Dial", t.cyan); // the levers' dial is orange because it is grabbable; a picture of one is not
            Icon("Hand", "Hand", t.offWhite);
            AssetDatabase.SaveAssets();
        }

        /// <summary>The panel and its diagrams on the pod. The director is wired in BuildScene (WireExplainer).</summary>
        private static void BuildExplainer(GameObject pod, DashScreenView dash, RideControlSet controls)
        {
            MakeExplainerMaterials();
            ExplainerVisuals.Clear();

            var panel = Make("Explainer", pod.transform, Polar(0f, ExplainerElevation, ExplainerDistance));
            panel.transform.localRotation = Face(0f, ExplainerElevation);
            Quad("Glow", panel.transform, new Vector3(0, 0, 0.03f), new Vector2(4.9f, 1.7f), "GlowCyan");
            Quad("Plate", panel.transform, new Vector3(0, 0, 0.02f), ExplainerSize, "PlateSolid");
            Quad("TrimTop", panel.transform, new Vector3(0, ExplainerSize.y / 2f, 0.004f), new Vector2(ExplainerSize.x, 0.014f), "PipeCyan");
            _visualsRoot = Make("Visuals", panel.transform, Vector3.zero).transform;

            BuildExplainerVisuals(dash, controls);

            var view = pod.AddComponent<ExplainerView>();
            Ref(view, "dash", dash);
            Ref(view, "panel", panel);
            Set(view, "visuals", p =>
            {
                p.arraySize = ExplainerVisuals.Count;
                for (int i = 0; i < ExplainerVisuals.Count; i++)
                {
                    SerializedProperty e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("cue").stringValue = ExplainerVisuals[i].Key;
                    e.FindPropertyRelative("root").objectReferenceValue = ExplainerVisuals[i].Value;
                }
            });

            AssertExplainer(panel.transform);
            foreach (KeyValuePair<string, GameObject> v in ExplainerVisuals) v.Value.SetActive(false);
            panel.SetActive(false); // the view opens it on its cue; captures and checks see the plain cockpit
        }

        private static void WireExplainer(GameObject pod, RideDirector director)
        {
            Ref(pod.GetComponent<ExplainerView>(), "director", director);
            foreach (WeightDemoView demo in pod.GetComponentsInChildren<WeightDemoView>(true)) Ref(demo, "director", director);
        }

        // ---------- the diagrams ----------

        private static void BuildExplainerVisuals(DashScreenView dash, RideControlSet controls)
        {
            RideTheme t = _theme;

            // Intro: AURA is broken.
            Transform v = V("offline");
            ExMotion(ExIcon(v, "Cross", new Vector2(-1.3f, 0f), 0.62f), ExplainerMotion.Mode.Pulse, 2.2f, 0.12f);
            ExHalo(v, "HaloAmber", new Vector2(-1.3f, 0f), 1.1f);
            ExCap(v, "Label", new Vector2(0.45f, 0.18f), "TARGETING", 0.17f, t.offWhite, 2.6f);
            ExCap(v, "State", new Vector2(0.45f, -0.16f), "OFFLINE", 0.34f, t.amber, 2.6f, true);

            v = V("rock_friend");
            ExIcon(v, "Rock", new Vector2(-1.3f, 0.08f), 0.62f);
            ExCap(v, "RockLabel", new Vector2(-1.3f, -0.42f), "ROCK?", 0.15f, t.offWhite, 1.4f);
            ExMotion(ExCap(v, "Question", new Vector2(0f, 0.02f), "?", 0.5f, t.amber, 0.8f, true).gameObject, ExplainerMotion.Mode.Pulse, 1.4f, 0.12f);
            ExIcon(v, "Drone", new Vector2(1.3f, 0.08f), 0.62f);
            ExCap(v, "DroneLabel", new Vector2(1.3f, -0.42f), "FRIEND?", 0.15f, t.offWhite, 1.4f);

            // Intro: one connection, up close (also the dock's hands-on weight).
            BuildWeightDemo(dash, controls);

            // Intro: the plan, one tile lighting per stop as it is named.
            Plan("plan", -1);
            Plan("plan1", 0);
            Plan("plan2", 1);
            Plan("plan3", 2);

            // Briefing 1 (1943): add up the signals, compare with the trigger, fire.
            NeuronFires("sum", 0);
            NeuronFires("trigger", 1);
            NeuronFires("fire", 2);

            v = V("rock_drone");
            ExIcon(v, "Rock", new Vector2(-1.2f, 0.1f), 0.6f);
            ExMotion(ExIcon(v, "Fire", new Vector2(-0.82f, 0.38f), 0.3f), ExplainerMotion.Mode.Pulse, 2f, 0.15f);
            ExCap(v, "RockLabel", new Vector2(-1.2f, -0.42f), "ZAP ROCKS", 0.15f, t.amber, 1.6f, true);
            ExIcon(v, "Drone", new Vector2(1.2f, 0.1f), 0.6f);
            ExIcon(v, "Check", new Vector2(1.58f, 0.38f), 0.3f);
            ExCap(v, "DroneLabel", new Vector2(1.2f, -0.42f), "SPARE DRONES", 0.15f, t.cyan, 1.6f, true);

            // Briefing 2 (1958): weights like volume knobs; two sensors; one wired backwards.
            v = V("knob");
            ExMotion(ExIcon(v, "DialCyan", new Vector2(-1.3f, 0f), 0.7f), ExplainerMotion.Mode.Swing, 0.45f, 55f);
            ExCap(v, "Top", new Vector2(0.5f, 0.17f), "EVERY INPUT GETS", 0.16f, t.offWhite, 2.8f);
            ExCap(v, "Big", new Vector2(0.5f, -0.15f), "ITS OWN WEIGHT", 0.26f, t.cyan, 2.8f, true);

            v = V("sensors");
            ExIcon(v, "Rock", new Vector2(-1.1f, 0.1f), 0.6f);
            ExCap(v, "RockLabel", new Vector2(-1.1f, -0.42f), "ROCK SENSOR", 0.15f, t.offWhite, 1.6f);
            ExCap(v, "Plus", new Vector2(0f, 0.06f), "+", 0.34f, t.offWhite, 0.6f, true);
            ExIcon(v, "Ice", new Vector2(1.1f, 0.1f), 0.6f);
            ExCap(v, "IceLabel", new Vector2(1.1f, -0.42f), "ICE SENSOR", 0.15f, t.cyan, 1.6f);

            v = V("negative");
            ExIcon(v, "Ice", new Vector2(-1.4f, 0.08f), 0.6f);
            ExMotion(Quad("Wire", v, new Vector3(-0.45f, 0.08f, -0.01f), new Vector2(1.1f, 0.08f), "PipeAmber"), ExplainerMotion.Mode.Pulse, 1.2f, 0.08f);
            ExCap(v, "Weight", new Vector2(1.05f, 0.1f), "WEIGHT  -1", 0.26f, t.amber, 1.8f, true);
            ExCap(v, "Label", new Vector2(0f, -0.42f), "PLUGGED IN BACKWARDS", 0.15f, t.offWhite, 3.8f);

            v = V("fix");
            ExIcon(v, "Rock", new Vector2(-1.45f, 0.08f), 0.55f);
            ExMotion(ExIcon(v, "Fire", new Vector2(-1.1f, 0.35f), 0.28f), ExplainerMotion.Mode.Pulse, 2f, 0.15f);
            ExIcon(v, "Ice", new Vector2(-0.5f, 0.08f), 0.55f);
            ExMotion(ExIcon(v, "Fire", new Vector2(-0.15f, 0.35f), 0.28f), ExplainerMotion.Mode.Pulse, 2f, 0.15f);
            ExCap(v, "Top", new Vector2(1.05f, 0.16f), "ROCK OR ICE?", 0.17f, t.offWhite, 1.9f);
            ExCap(v, "Big", new Vector2(1.05f, -0.15f), "ZAP IT", 0.3f, t.amber, 1.9f, true);

            v = V("you_learn");
            ExMotion(ExIcon(v, "Hand", new Vector2(-1.3f, 0f), 0.7f), ExplainerMotion.Mode.Pulse, 1f, 0.06f);
            ExCap(v, "Top", new Vector2(0.5f, 0.17f), "YOU ARE THE", 0.16f, t.offWhite, 2.8f);
            ExCap(v, "Big", new Vector2(0.5f, -0.15f), "LEARNING RULE", 0.28f, t.cyan, 2.8f, true);

            // Briefing 3 (1960): the machine tunes itself; error; downhill; repeat.
            v = V("self_tune");
            ExMotion(ExIcon(v, "DialCyan", new Vector2(-1.3f, 0f), 0.7f), ExplainerMotion.Mode.Spin, 70f);
            ExCap(v, "Top", new Vector2(0.5f, 0.17f), "THE MACHINE TUNES", 0.16f, t.offWhite, 2.8f);
            ExCap(v, "Big", new Vector2(0.5f, -0.15f), "ITS OWN WEIGHTS", 0.26f, t.cyan, 2.8f, true);

            v = V("error");
            ExMotion(ExBar(v, "Guess", new Vector2(-1.3f, -0.32f), 0.36f, 0.34f, "PipeCyan"), ExplainerMotion.Mode.Grow, 1f, 0f, 0.1f, 0.6f);
            ExCap(v, "GuessLabel", new Vector2(-1.3f, -0.46f), "GUESS", 0.13f, t.cyan, 0.9f);
            ExBar(v, "Answer", new Vector2(-0.4f, -0.32f), 0.36f, 0.8f, "LineWhite");
            ExCap(v, "AnswerLabel", new Vector2(-0.4f, -0.46f), "ANSWER", 0.13f, t.offWhite, 0.9f);
            ExMotion(Quad("Gap", v, new Vector3(-0.85f, 0.25f, -0.01f), new Vector2(0.035f, 0.46f), "PipeAmber"), ExplainerMotion.Mode.Pulse, 1.5f, 0.1f);
            Quad("GapTop", v, new Vector3(-0.85f, 0.48f, -0.01f), new Vector2(0.18f, 0.03f), "PipeAmber");
            Quad("GapBottom", v, new Vector3(-0.85f, 0.02f, -0.01f), new Vector2(0.18f, 0.03f), "PipeAmber");
            ExCap(v, "Big", new Vector2(0.95f, 0.2f), "ERROR", 0.3f, t.amber, 1.9f, true);
            ExCap(v, "Small", new Vector2(0.95f, -0.12f), "HOW WRONG IT IS", 0.15f, t.offWhite, 1.9f);

            Downhill("downhill", false, "STEP DOWNHILL");
            Downhill("repeat", true, "REPEAT");

            v = V("today");
            ExCap(v, "Big", new Vector2(0f, 0.12f), "TODAY'S AI", 0.36f, t.cyan, 3.8f, true);
            ExCap(v, "Small", new Vector2(0f, -0.3f), "STILL LEARNS THIS WAY", 0.16f, t.offWhite, 3.8f);

            // Outro: AURA healed; the recap; scale; the next problem.
            v = V("online");
            ExMotion(ExIcon(v, "Check", new Vector2(-1.3f, 0f), 0.62f), ExplainerMotion.Mode.Pulse, 1.2f, 0.1f);
            ExHalo(v, "HaloCyan", new Vector2(-1.3f, 0f), 1.1f);
            ExCap(v, "Label", new Vector2(0.45f, 0.18f), "TARGETING", 0.17f, t.offWhite, 2.6f);
            ExCap(v, "State", new Vector2(0.45f, -0.16f), "ONLINE", 0.34f, t.cyan, 2.6f, true);

            Recap();

            v = V("billions");
            ExMotion(ExCap(v, "Big", new Vector2(0f, 0.14f), "1,000,000,000+", 0.4f, t.cyan, 3.9f, true).gameObject, ExplainerMotion.Mode.Appear, 1f, 0f, 0.1f, 0.5f);
            ExCap(v, "Small", new Vector2(0f, -0.3f), "WEIGHTS, ALL AT ONCE", 0.16f, t.offWhite, 3.8f);

            v = V("xor");
            ExCap(v, "Big", new Vector2(-1.2f, 0.2f), "XOR", 0.38f, t.amber, 1.6f, true);
            ExCap(v, "Small1", new Vector2(-1.2f, -0.17f), "ONE NEURON CAN'T", 0.13f, t.offWhite, 1.6f);
            ExCap(v, "Small2", new Vector2(-1.2f, -0.35f), "SOLVE THIS", 0.13f, t.offWhite, 1.6f);
            TextMeshPro table = ExCap(v, "Table", new Vector2(0.85f, 0f), "0   0   =   0\n0   1   =   1\n1   0   =   1\n1   1   =   0", 0.14f, t.offWhite, 2.0f);
            table.rectTransform.sizeDelta = new Vector2(2.0f, 0.9f);
            ExMotion(ExCap(v, "Question", new Vector2(1.85f, 0.3f), "?", 0.3f, t.amber, 0.5f, true).gameObject, ExplainerMotion.Mode.Pulse, 1.4f, 0.15f);

            v = V("next_ride");
            ExCap(v, "Top", new Vector2(0f, 0.2f), "NEXT RIDE", 0.17f, t.offWhite, 3.8f);
            ExCap(v, "Big", new Vector2(0f, -0.12f), "HIDDEN LAYERS", 0.36f, t.violet, 3.8f, true);
        }

        /// <summary>Three tiles, one per stop; lit = the tile being named (-1: all three, none lit).</summary>
        private static void Plan(string cue, int lit)
        {
            RideTheme t = _theme;
            Transform v = V(cue);
            string[] names = { "ONE SIGNAL", "TWO SIGNALS", "SELF-TAUGHT" };
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 1.35f;
                bool on = i == lit;
                bool dim = lit >= 0 && !on;
                Transform tile = Make("Tile" + (i + 1), v, new Vector3(x, 0f, 0f)).transform;
                Quad("Back", tile, new Vector3(0f, 0f, -0.005f), new Vector2(1.2f, 1.0f), on ? "TileLit" : "TileDim");
                TextMeshPro number = ExCap(tile, "Number", new Vector2(0f, 0.17f), (i + 1).ToString(), 0.36f, dim ? t.locked : t.cyan, 1.1f, true);
                ExCap(tile, "Name", new Vector2(0f, -0.26f), names[i], 0.14f, dim ? t.locked : t.offWhite, 1.15f, on);
                if (on)
                {
                    ExHalo(tile, "HaloCyan", new Vector2(0f, 0.17f), 0.75f);
                    ExMotion(number.gameObject, ExplainerMotion.Mode.Appear, 1f, 0f, 0f, 0.35f);
                }
            }
        }

        /// <summary>
        /// The 1943 neuron in three beats: stage 0 adds up the signals, stage 1 shows the trigger line it must cross,
        /// stage 2 crosses it and fires.
        /// </summary>
        private static void NeuronFires(string cue, int stage)
        {
            RideTheme t = _theme;
            Transform v = V(cue);
            float[] inputs = { 0.22f, 0.34f, 0.27f };
            for (int i = 0; i < inputs.Length; i++)
            {
                ExBar(v, "Signal" + i, new Vector2(-1.75f + i * 0.25f, -0.3f), 0.16f, inputs[i], "PipeCyan");
            }

            ExCap(v, "SignalsLabel", new Vector2(-1.5f, -0.45f), "SIGNALS", 0.13f, t.offWhite, 1.0f);
            ExCap(v, "Plus", new Vector2(-0.9f, -0.06f), "+", 0.34f, t.offWhite, 0.6f, true);

            float total = stage == 0 ? 0.62f : stage == 1 ? 0.4f : 0.72f;
            ExMotion(ExBar(v, "Total", new Vector2(-0.3f, -0.3f), 0.32f, total, stage == 2 ? "PipeAmber" : "PipeCyan"),
                ExplainerMotion.Mode.Grow, 1f, 0f, 0.15f, stage == 2 ? 0.7f : 1.1f);
            ExCap(v, "TotalLabel", new Vector2(-0.3f, -0.45f), "TOTAL", 0.13f, t.offWhite, 0.9f);

            if (stage == 0)
            {
                ExCap(v, "Big", new Vector2(1.15f, 0.12f), "ADD UP", 0.24f, t.cyan, 1.7f, true);
                ExCap(v, "Small", new Vector2(1.15f, -0.16f), "THE SIGNALS", 0.15f, t.offWhite, 1.7f);
                return;
            }

            Quad("TriggerLine", v, new Vector3(-0.3f, 0.17f, -0.012f), new Vector2(0.78f, 0.03f), "PipeAmber");
            ExCap(v, "TriggerLabel", new Vector2(0.55f, 0.17f), "TRIGGER", 0.15f, t.amber, 0.95f, true);
            if (stage == 1)
            {
                ExCap(v, "Question", new Vector2(1.45f, -0.16f), "CROSS IT?", 0.15f, t.offWhite, 1.1f);
                return;
            }

            ExHalo(v, "HaloAmber", new Vector2(1.45f, 0.12f), 0.95f);
            ExMotion(ExIcon(v, "Fire", new Vector2(1.45f, 0.12f), 0.55f), ExplainerMotion.Mode.Pulse, 2.2f, 0.15f);
            ExCap(v, "Fire", new Vector2(1.45f, -0.38f), "FIRE!", 0.2f, t.amber, 1.0f, true);
        }

        /// <summary>The error as a hill and a ball of light on it: rolling down (stepped = one nudge at a time, repeated).</summary>
        private static void Downhill(string cue, bool stepped, string title)
        {
            RideTheme t = _theme;
            Transform v = V(cue);
            const float half = 1.75f;
            const float floor = -0.42f;
            const float rise = 0.74f;
            Func<float, float> hill = x => floor + rise * (x / half) * (x / half);

            LineRenderer curve = Line("Hill", v, "LineAmber", 0.045f);
            curve.positionCount = 25;
            for (int i = 0; i < 25; i++)
            {
                float x = Mathf.Lerp(-half, half, i / 24f);
                curve.SetPosition(i, new Vector3(x, hill(x), -0.012f));
            }

            ExCap(v, "Title", new Vector2(0f, 0.42f), title, 0.2f, t.cyan, 2.4f, true);
            ExCap(v, "Axis", new Vector2(-1.3f, 0.47f), "ERROR", 0.13f, t.amber, 0.9f);

            const float radius = 0.1f;
            var path = new Vector3[7];
            for (int i = 0; i < path.Length; i++)
            {
                float x = Mathf.Lerp(-1.5f, 0f, i / (path.Length - 1f));
                path[i] = new Vector3(x, hill(x) + radius, -0.016f);
            }

            Transform ball = Make("Ball", v, path[0]).transform;
            ExHalo(ball, "HaloCyan", Vector2.zero, 0.55f);
            ExHalo(ball, "HaloWhite", Vector2.zero, 0.22f);
            ExMotion(ball.gameObject, stepped ? ExplainerMotion.Mode.Steps : ExplainerMotion.Mode.Path, 1f, 0f, 0.25f,
                stepped ? 3.2f : 2.2f, path, 4, 1.0f);
        }

        /// <summary>The four ideas of the ride, each popping in as the Guide names it.</summary>
        private static void Recap()
        {
            RideTheme t = _theme;
            Transform v = V("recap");
            string[] names = { "WEIGHTS", "SUMS", "TRIGGERS", "DOWNHILL" };
            for (int i = 0; i < 4; i++)
            {
                float x = -1.5f + i;
                Transform tile = Make("Idea" + (i + 1), v, new Vector3(x, 0f, 0f)).transform;
                switch (i)
                {
                    case 0:
                        ExIcon(tile, "DialCyan", new Vector2(0f, 0.12f), 0.45f);
                        break;
                    case 1:
                        ExCap(tile, "Plus", new Vector2(0f, 0.12f), "+", 0.4f, t.cyan, 0.6f, true);
                        break;
                    case 2:
                        Quad("Line", tile, new Vector3(0f, 0.12f, -0.012f), new Vector2(0.55f, 0.03f), "PipeAmber");
                        ExBar(tile, "Bar", new Vector2(0f, -0.12f), 0.16f, 0.36f, "PipeCyan");
                        break;
                    default:
                        LineRenderer mini = Line("Hill", tile, "LineAmber", 0.03f);
                        mini.positionCount = 9;
                        for (int k = 0; k < 9; k++)
                        {
                            float mx = Mathf.Lerp(-0.3f, 0.3f, k / 8f);
                            mini.SetPosition(k, new Vector3(mx, -0.05f + 0.32f * (mx / 0.3f) * (mx / 0.3f), -0.012f));
                        }

                        ExHalo(tile, "HaloCyan", new Vector2(0.1f, 0.02f), 0.2f);
                        break;
                }

                ExCap(tile, "Name", new Vector2(0f, -0.38f), names[i], 0.14f, t.offWhite, 0.95f, true);
                ExMotion(tile.gameObject, ExplainerMotion.Mode.Appear, 1f, 0f, 0.2f + i * 0.75f, 0.3f);
            }
        }

        /// <summary>One connection up close: input neuron, the weighted wire, the neuron it feeds (WeightDemoView).</summary>
        private static void BuildWeightDemo(DashScreenView dash, RideControlSet controls)
        {
            RideTheme t = _theme;
            Transform v = V("weights");
            Alias("weights_change", v.gameObject);
            Alias("try_weight", v.gameObject);
            Alias("fire_goal", v.gameObject); // the intro's "make this neuron fire"

            const float y = 0.06f;
            ExHalo(v, "HaloCyan", new Vector2(-1.45f, y), 0.75f);
            ExHalo(v, "HaloWhite", new Vector2(-1.45f, y), 0.26f);
            ExCap(v, "InputLabel", new Vector2(-1.45f, -0.24f), "INPUT", 0.13f, t.offWhite, 1.0f);

            Transform output = Make("Output", v, new Vector3(1.45f, y, 0f)).transform;
            // The core scales with the glow, so a neuron that fires is unmistakable next to one that doesn't.
            ExHalo(output, "HaloCyan", Vector2.zero, 1.0f);
            ExHalo(output, "HaloWhite", Vector2.zero, 0.3f);
            ExCap(v, "OutputLabel", new Vector2(1.45f, -0.24f), "NEURON", 0.13f, t.offWhite, 1.0f);

            Transform wire = Make("Wire", v, new Vector3(0f, y, -0.01f)).transform;
            wire.localScale = new Vector3(1f, 0.09f, 1f);
            GameObject fill = Quad("Fill", wire, Vector3.zero, new Vector2(2.4f, 1f), "PipeCyan");

            Transform pulse = Make("Pulse", v, new Vector3(-1.2f, y, -0.014f)).transform;
            ExHalo(pulse, "HaloWhite", Vector2.zero, 0.24f);

            TextMeshPro label = ExCap(v, "Weight", new Vector2(0f, 0.37f), "WEIGHT  +1.0", 0.2f, t.cyan, 2.6f, true);
            TextMeshPro caption = ExCap(v, "Caption", new Vector2(0f, -0.45f), "HOW MUCH ONE NEURON LISTENS TO ANOTHER", 0.13f, t.offWhite, 3.9f);

            var demo = v.gameObject.AddComponent<WeightDemoView>();
            Ref(demo, "dash", dash);
            Ref(demo, "controls", controls);
            Ref(demo, "theme", _theme);
            Ref(demo, "wire", wire);
            Ref(demo, "wireRenderer", fill.GetComponent<Renderer>());
            Ref(demo, "positiveMaterial", Mat("PipeCyan"));
            Ref(demo, "negativeMaterial", Mat("PipeAmber"));
            Ref(demo, "outputGlow", output);
            Ref(demo, "pulse", pulse);
            Ref(demo, "weightLabel", label);
            Ref(demo, "caption", caption);
            Set(demo, "wireStart", p => p.floatValue = -1.2f);
            Set(demo, "wireEnd", p => p.floatValue = 1.2f);
            Set(demo, "captionHold", p => p.stringValue = "HOW MUCH ONE NEURON LISTENS TO ANOTHER");
            Set(demo, "captionSweep", p => p.stringValue = "CHANGE THE WEIGHT, CHANGE THE ANSWER");
            Set(demo, "captionLever", p => p.stringValue = "TRY IT: MOVE THE ROCK LEVER");
            Set(demo, "captionGoal", p => p.stringValue = "MAKE IT FIRE: ROCK LEVER TO {0}");
            Set(demo, "captionFired", p => p.stringValue = "IT FIRED!");
        }

        // ---------- helpers ----------

        private static Transform V(string cue)
        {
            GameObject root = Make("V_" + cue, _visualsRoot, Vector3.zero);
            ExplainerVisuals.Add(new KeyValuePair<string, GameObject>(cue, root));
            return root.transform;
        }

        private static void Alias(string cue, GameObject root) => ExplainerVisuals.Add(new KeyValuePair<string, GameObject>(cue, root));

        private static TextMeshPro ExCap(Transform parent, string name, Vector2 pos, string text, float size, Color color, float width, bool bold = false)
        {
            TextMeshPro t = Text(name, parent, new Vector3(pos.x, pos.y, -0.02f), text, size, color, width, TextAlignmentOptions.Center, bold);
            t.textWrappingMode = TextWrappingModes.NoWrap; // a caption never wraps into the next row; the width check below catches overflow
            return t;
        }

        private static GameObject ExIcon(Transform parent, string icon, Vector2 pos, float size)
        {
            return Quad("Icon_" + icon, parent, new Vector3(pos.x, pos.y, -0.015f), new Vector2(size, size), "Icon_" + icon);
        }

        private static GameObject ExHalo(Transform parent, string mat, Vector2 pos, float size)
        {
            return Quad("Halo", parent, new Vector3(pos.x, pos.y, -0.012f), new Vector2(size, size), mat);
        }

        /// <summary>A bar standing on its bottom edge (the pivot), so it can grow upward.</summary>
        private static GameObject ExBar(Transform parent, string name, Vector2 bottom, float width, float height, string mat)
        {
            GameObject pivot = Make(name, parent, new Vector3(bottom.x, bottom.y, -0.01f));
            Quad("Fill", pivot.transform, new Vector3(0f, height / 2f, 0f), new Vector2(width, height), mat);
            return pivot;
        }

        private static void ExMotion(GameObject go, ExplainerMotion.Mode mode, float speed = 1f, float amount = 0.1f, float delay = 0f,
            float seconds = 1f, Vector3[] path = null, int steps = 4, float rest = 0.8f)
        {
            var m = go.AddComponent<ExplainerMotion>();
            Set(m, "mode", p => p.enumValueIndex = (int)mode);
            Set(m, "speed", p => p.floatValue = speed);
            Set(m, "amount", p => p.floatValue = amount);
            Set(m, "delay", p => p.floatValue = delay);
            Set(m, "seconds", p => p.floatValue = seconds);
            Set(m, "rest", p => p.floatValue = rest);
            Set(m, "steps", p => p.intValue = steps);
            if (path == null) return;
            Set(m, "path", p =>
            {
                p.arraySize = path.Length;
                for (int i = 0; i < path.Length; i++) p.GetArrayElementAtIndex(i).vector3Value = path[i];
            });
        }

        /// <summary>
        /// Every cue the script names must do something somewhere (a typo would otherwise fail silently), and every
        /// explainer text must be readable: at least 1.5 degrees tall at 4.8 m, and inside the panel.
        /// </summary>
        private static void AssertExplainer(Transform panel)
        {
            var bad = new List<string>();
            var known = new HashSet<string>(NetworkCues);
            known.UnionWith(HintCues);
            foreach (KeyValuePair<string, GameObject> v in ExplainerVisuals) known.Add(v.Key);
            foreach (string cue in RideNarrationData.AllCues())
            {
                if (!known.Contains(cue)) bad.Add("the script cues {" + cue + "} but nothing shows it");
            }

            float halfWidth = ExplainerSize.x / 2f;
            foreach (TextMeshPro t in panel.GetComponentsInChildren<TextMeshPro>(true))
            {
                float meters = t.fontSize / 10f;
                if (meters < ExplainerMinText - 1e-4f) bad.Add(t.transform.parent.name + "/" + t.name + " is " + meters.ToString("0.000") + " m, under the " + ExplainerMinText + " m reading floor");
                t.ForceMeshUpdate();
                Bounds b = t.textBounds;
                Vector3 left = panel.InverseTransformPoint(t.transform.TransformPoint(b.min));
                Vector3 right = panel.InverseTransformPoint(t.transform.TransformPoint(b.max));
                if (Mathf.Min(left.x, right.x) < -halfWidth || Mathf.Max(left.x, right.x) > halfWidth)
                {
                    bad.Add(t.transform.parent.name + "/" + t.name + " (\"" + t.text + "\") runs past the panel edge");
                }
            }

            if (bad.Count > 0) throw new InvalidOperationException("Explainer: " + string.Join("; ", bad) + ". Fix Tools/Editor/NeuralRideBuilder.Explainer.cs or the {cue} tags in RideNarrationData.cs.");
        }
    }
}
