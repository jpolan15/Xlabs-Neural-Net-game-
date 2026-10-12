using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Core.Neural;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;
using Convergence.Presentation.Ride;
using Convergence.XR.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Regions 3, 4, 5 and 7: the real 49 → 16 → 10 network, twice (untrained in blue, trained in gold), as one giant
    /// diagram standing across the rider's view 30 m ahead: the 49 pixels as a 7×7 grid on the left (it shows the 3),
    /// the 16 hidden neurons as a 4×4 grid in the middle (high enough to pass under), the 10 answers as a column on the
    /// right, labelled 0 to 9, with all 944 wires between them bowing a little toward the rider. Every wire is one
    /// ribbon in one mesh (width and brightness = |weight|, colour = sign) and every neuron one billboard in a second
    /// mesh (size and brightness = its activation for the hero 3, from Core's own forward pass), so each network is
    /// two draw calls plus its labels and reads as three layers from the seat and from far away. The rewind adds the blame of every wire (Core's
    /// backward pass) as a third mesh, and the few brightest wires as pointable targets.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private sealed class Ribbons
        {
            public readonly List<Vector3> Position = new List<Vector3>();
            public readonly List<Vector3> Direction = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>();
            public readonly List<Color> Data = new List<Color>();
            public readonly List<int> Triangles = new List<int>();

            /// <summary>One ribbon along a polyline. data = (weight size, sign, progress per point, phase).</summary>
            public void Strip(IList<Vector3> points, float weight, float sign, IList<float> progress, float phase)
            {
                int first = Position.Count;
                float run = 0f;
                for (int i = 0; i < points.Count; i++)
                {
                    Vector3 before = points[Mathf.Max(0, i - 1)];
                    Vector3 after = points[Mathf.Min(points.Count - 1, i + 1)];
                    Vector3 tangent = (after - before).normalized;
                    if (i > 0) run += Vector3.Distance(points[i - 1], points[i]);
                    var data = new Color(weight, sign, progress[i], phase);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Position.Add(points[i]);
                        Direction.Add(tangent);
                        Uv.Add(new Vector2(run, side));
                        Data.Add(data);
                    }
                }

                for (int i = 0; i < points.Count - 1; i++)
                {
                    int a = first + i * 2;
                    Triangles.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                }
            }

            /// <summary>A connection of 8 points (16 vertices): a quadratic curve that bows out by bulge.</summary>
            public void Link(Vector3 from, Vector3 to, Vector3 bulge, float weight, float sign, float progressFrom, float progressTo, float phase)
            {
                const int samples = 8;
                Vector3 control = (from + to) * 0.5f + bulge;
                var points = new Vector3[samples];
                var progress = new float[samples];
                for (int i = 0; i < samples; i++)
                {
                    float t = i / (float)(samples - 1);
                    points[i] = Vector3.Lerp(Vector3.Lerp(from, control, t), Vector3.Lerp(control, to, t), t);
                    progress[i] = Mathf.Lerp(progressFrom, progressTo, t);
                }

                Strip(points, weight, sign, progress, phase);
            }
        }

        private sealed class Billboards
        {
            public readonly List<Vector3> Position = new List<Vector3>();
            public readonly List<Vector2> Corner = new List<Vector2>();
            public readonly List<Vector2> Radius = new List<Vector2>();
            public readonly List<Color> Data = new List<Color>();
            public readonly List<int> Triangles = new List<int>();

            /// <summary>data = (progress, hue: here the activation 0..1, kind, phase).</summary>
            public void Add(Vector3 center, float radius, float progress, float hue, float kind, float phase)
            {
                int first = Position.Count;
                var data = new Color(progress, hue, kind, phase);
                foreach (Vector2 corner in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) })
                {
                    Position.Add(center);
                    Corner.Add(corner);
                    Radius.Add(new Vector2(radius, 0f));
                    Data.Add(data);
                }

                Triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }
        }

        private static Material NetworkMaterial(string name, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new System.IO.FileNotFoundException("Shader " + shaderName + " is missing. Is it imported from Assets/Shaders?");
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }

            m.shader = shader;
            Mats[name] = m;
            return m;
        }

        private static Material LinkMaterial(string name, Color pos, Color neg, float width, float dim, float minPixels = 1.3f, float maxPixels = 5f)
        {
            Material m = NetworkMaterial(name, "Convergence/NeuralLinks");
            m.SetColor("_PosColor", pos);
            m.SetColor("_NegColor", neg);
            m.SetFloat("_Width", width);
            m.SetFloat("_MinPixels", minPixels);
            m.SetFloat("_MaxPixels", maxPixels);
            m.SetFloat("_Dim", dim);
            m.SetFloat("_PulseSpacing", _theme.pulseSpacing);
            m.SetFloat("_PulseSpeed", _theme.pulseSpeed);
            m.SetFloat("_FarStart", 70f);
            m.SetFloat("_FarEnd", 150f);
            m.SetFloat("_LitUpTo", 2f);
            m.SetFloat("_Reveal", 1f);
            m.SetFloat("_Focus", 1f);
            m.SetFloat("_Ignite", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material NodeMaterial(string name, Color low, Color high, float dim)
        {
            Material m = NetworkMaterial(name, "Convergence/NeuralNodes");
            m.SetColor("_ColorA", low);
            m.SetColor("_ColorB", high);
            m.SetFloat("_Dim", dim);
            m.SetFloat("_HazeStrength", 0.12f);
            m.SetFloat("_FarStart", 80f);
            m.SetFloat("_FarEnd", 150f);
            m.SetFloat("_LitUpTo", 2f);
            m.SetFloat("_Reveal", 1f);
            m.SetFloat("_Focus", 1f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void MakeNetworkMaterials()
        {
            RideTheme t = _theme;
            Color rail = t.locked * 0.45f;
            rail.a = 1f;
            Material rails = LinkMaterial("Rails", rail, rail, 0.07f, 1f, 1.2f, 2.5f);
            rails.SetFloat("_FarStart", 60f);
            rails.SetFloat("_FarEnd", 120f);
            LinkMaterial("Dendrites", t.amber, t.amber, 0.06f, 1f, 1.4f, 4f);
            LinkMaterial("NetLinksBlue", t.networkBlue * 0.5f, t.negativeWeight * 0.45f, 0.05f, t.networkDim, 1.0f, 4f);
            NodeMaterial("NetNodesBlue", new Color(0.10f, 0.20f, 0.42f), new Color(0.80f, 0.95f, 1f), t.networkDim);
            LinkMaterial("NetLinksGold", t.gold * 0.5f, t.negativeWeight * 0.4f, 0.05f, t.networkDim, 1.0f, 4f);
            NodeMaterial("NetNodesGold", new Color(0.30f, 0.20f, 0.05f), new Color(1f, 0.96f, 0.75f), t.networkDim);
            LinkMaterial("BlameLinks", t.rewindMagenta, t.rewindMagenta, 0.09f, 0.08f, 1.3f, 6f);
            LinkMaterial("BlameTop", Color.Lerp(t.rewindMagenta, Color.white, 0.45f), t.rewindMagenta, 0.28f, 1f, 3f, 14f);
            M("GlowRed", new Color(t.wrongRed.r, t.wrongRed.g, t.wrongRed.b, 0.75f), Kind.Additive, Mat("GlowWhite").GetTexture("_BaseMap"));
        }

        // ---------- the network ----------

        private sealed class NetworkParts
        {
            public GameObject Root;
            public Renderer Links, Nodes;
            public Transform[] Outputs;
            public TMP_Text[] Labels;
            public Transform GuessGlow;
            public Vector3[] InputPos, HiddenPos, OutputPos;
        }

        // The diagram, in the network pose's plane z = 0 (y up from the track, x right as the rider sees it).
        private const float InputX = -16f, OutputX = 15f;
        private static Vector3 InputPosition(int i) => new Vector3(InputX + (i % 7 - 3f) * 1.5f, 6.0f + (3 - i / 7) * 1.5f, 0f);
        private static Vector3 HiddenPosition(int j) => new Vector3((j % 4 - 1.5f) * 2.4f, 7.0f + (1.5f - j / 4) * 2.4f, 0f);
        private static Vector3 OutputPosition(int k) => new Vector3(OutputX, 11.7f - k * 1.2f, 0f);

        /// <summary>The real activations Core computes for the hero digit: pixels, hidden, answers.</summary>
        private static void Activations(bool trained, out double[] inputs, out double[] hidden, out double[] outputs, out NetworkModel net)
        {
            net = trained ? _data.CreateTrained() : _data.CreateUntrained();
            inputs = _data.File.hero.pixels7;
            outputs = net.Forward(inputs);
            var neurons = net.Layers[0].Neurons;
            hidden = new double[neurons.Count];
            for (int j = 0; j < neurons.Count; j++) hidden[j] = neurons[j].LastOutput;
        }

        private static NetworkParts BuildNetwork(Transform parent, bool trained, string tint)
        {
            Activations(trained, out double[] inputs, out double[] hidden, out double[] outputs, out NetworkModel net);
            Level1Weights w = trained ? _data.File.trained : _data.File.initial;

            var parts = new NetworkParts { Root = Make("Network", parent, Vector3.zero) };
            Transform root = parts.Root.transform;
            parts.InputPos = new Vector3[49];
            parts.HiddenPos = new Vector3[16];
            parts.OutputPos = new Vector3[10];
            for (int i = 0; i < 49; i++) parts.InputPos[i] = InputPosition(i);
            for (int j = 0; j < 16; j++) parts.HiddenPos[j] = HiddenPosition(j);
            for (int k = 0; k < 10; k++) parts.OutputPos[k] = OutputPosition(k);

            var rng = new System.Random(trained ? 7 : 3);
            float Phase() => (float)rng.NextDouble();

            var links = new Ribbons();
            double max1 = 1e-9, max2 = 1e-9;
            foreach (double v in w.w1) max1 = System.Math.Max(max1, System.Math.Abs(v));
            foreach (double v in w.w2) max2 = System.Math.Max(max2, System.Math.Abs(v));
            for (int j = 0; j < 16; j++)
            {
                for (int i = 0; i < 49; i++)
                {
                    double v = w.w1[j * 49 + i];
                    links.Link(parts.InputPos[i], parts.HiddenPos[j], new Vector3(0f, 0f, -1.4f), (float)(System.Math.Abs(v) / max1), v >= 0 ? 1f : 0f, 0f, 0.5f, Phase());
                }
            }

            for (int k = 0; k < 10; k++)
            {
                for (int j = 0; j < 16; j++)
                {
                    double v = w.w2[k * 16 + j];
                    links.Link(parts.HiddenPos[j], parts.OutputPos[k], new Vector3(0f, 0f, -1.2f), (float)(System.Math.Abs(v) / max2), v >= 0 ? 1f : 0f, 0.5f, 1f, Phase());
                }
            }

            // Neurons: size and brightness follow the real activation (hue channel = activation, so a silent neuron is dark).
            var nodes = new Billboards();
            for (int i = 0; i < 49; i++) nodes.Add(parts.InputPos[i], 0.4f + 0.62f * (float)inputs[i], 0f, (float)inputs[i], 0f, Phase());
            for (int j = 0; j < 16; j++) nodes.Add(parts.HiddenPos[j], 0.6f + 0.6f * (float)hidden[j], 0.5f, (float)hidden[j], 0f, Phase());
            for (int k = 0; k < 10; k++) nodes.Add(parts.OutputPos[k], 0.35f + 0.7f * (float)outputs[k], 1f, (float)outputs[k], 0f, Phase());

            string tag = trained ? "Gold" : "Blue";
            parts.Links = AddMeshObject("Links", root, SaveMesh(ToLinkMesh(links), "L1_NetLinks" + tag), "NetLinks" + tag).GetComponent<Renderer>();
            parts.Nodes = AddMeshObject("Nodes", root, SaveMesh(ToNodeMesh(nodes), "L1_NetNodes" + tag), "NetNodes" + tag).GetComponent<Renderer>();

            // The ten answers, labelled 0 to 9 beside each node, on solid tags (1.5 degrees from 30 m).
            Transform outputs10 = Make("Outputs", root, Vector3.zero).transform;
            parts.Outputs = new Transform[10];
            parts.Labels = new TMP_Text[10];
            for (int k = 0; k < 10; k++)
            {
                parts.Outputs[k] = Make("Answer" + k, outputs10, parts.OutputPos[k]).transform;
                TextMeshPro label = Text("Label" + k, outputs10, parts.OutputPos[k] + new Vector3(1.7f, -0.05f, 0f), k.ToString(), 1.35f, _theme.offWhite, 1.5f, bold: true);
                LabelBacking(label, "0", 0.12f);
                parts.Labels[k] = label;
            }

            var glow = Quad("GuessGlow", outputs10, parts.OutputPos[0], new Vector2(2.6f, 2.6f), trained ? "GlowGold" : "GlowWhite");
            glow.AddComponent<FaceCamera>();
            parts.GuessGlow = glow.transform;

            // Layer names under each layer, facing the stop 30 m out (only the first time: the trained network has none).
            if (trained) return parts;
            Transform signs = Make("Signs", root, Vector3.zero).transform;
            Vector3 viewer = new Vector3(0f, 1.3f, -Layout.ViewDistance);
            foreach ((string text, Vector3 at) in new[]
            {
                ("49 PIXELS", new Vector3(InputX, 0.0f, -0.3f)),
                ("16 HIDDEN", new Vector3(0f, 1.9f, -0.3f)),
                ("10 ANSWERS", new Vector3(OutputX + 0.6f, -0.45f, -0.3f)),
            })
            {
                TextMeshPro sign = Text(text.Replace(" ", ""), signs, at, text, 1.35f, trained ? _theme.gold : _theme.networkBlue, 11f, bold: true);
                LabelBacking(sign, text, 0.25f);
                FaceToward(sign.transform, root.TransformPoint(viewer));
            }

            return parts;
        }

        private static NetworkView AddNetworkView(GameObject host, NetworkParts parts, RideDirector director, DashScreenView dash, ForwardPassMoment moment,
            Color answer, string flashCue, int dimInRegion)
        {
            var view = host.AddComponent<NetworkView>();
            Ref(view, "director", director);
            Ref(view, "dash", dash);
            Ref(view, "moment", moment);
            Ref(view, "theme", _theme);
            Ref(view, "links", parts.Links);
            Ref(view, "nodes", parts.Nodes);
            Refs(view, "outputNodes", parts.Outputs);
            Refs(view, "outputLabels", parts.Labels);
            Ref(view, "guessGlow", parts.GuessGlow);
            Ref(view, "guessGlowRenderer", parts.GuessGlow.GetComponent<Renderer>());
            Set(view, "answerColor", p => p.colorValue = answer);
            Set(view, "flashCue", p => p.stringValue = flashCue);
            Int(view, "dimInRegion", dimInRegion);
            return view;
        }

        private static NetworkParts _blueParts;

        private static GameObject BuildNetworkRegion(Layout layout, RideDirector director, DashScreenView dash, ForwardPassMoment moment)
        {
            GameObject root = RegionRoot(3, layout.BlueNetwork);
            _blueParts = BuildNetwork(root.transform, false, "Blue");
            AddNetworkView(root, _blueParts, director, dash, moment, Color.white, "wrong", 5);
            return root;
        }

        // ---------- region 4: wrong ----------

        private static GameObject BuildWrongRegion(Layout layout, RideDirector director, DashScreenView dash, GameObject network)
        {
            GameObject root = RegionRoot(4, layout.BlueNetwork);
            Activations(false, out _, out _, out double[] outputs, out _);
            int guess = 0;
            for (int k = 1; k < outputs.Length; k++) if (outputs[k] > outputs[guess]) guess = k;
            if (guess != _data.File.hero.untrainedGuess) throw new System.InvalidOperationException("Core's untrained guess " + guess + " differs from the bake's " + _data.File.hero.untrainedGuess + ". Re-run the bake.");

            Transform sign = Make("GuessSign", root.transform, new Vector3(OutputX, 13.3f, -0.5f)).transform;
            TextMeshPro text = Text("Text", sign, Vector3.zero, "IT SAYS: " + guess, 1.3f, _theme.wrongRed, 9f, bold: true);
            LabelBacking(text, "IT SAYS: 0", 0.25f);
            FaceToward(sign, layout.WrongStop.position + Vector3.up * 1.3f);
            return root;
        }

        // ---------- region 5: rewind and blame ----------

        private static GameObject BuildBlameRegion(Layout layout, RideDirector director, DashScreenView dash, BlameMoment moment)
        {
            GameObject root = RegionRoot(5, layout.BlueNetwork);
            Transform blame = Make("Blame", root.transform, Vector3.zero).transform;
            List<BlameMoment.Wire> wires = BlameMoment.ComputeBlame(_data, out _);
            double biggest = System.Math.Abs(wires[0].Gradient);

            Vector3 From(BlameMoment.Wire w) => w.Layer == 0 ? InputPosition(w.Input) : HiddenPosition(w.Input);
            Vector3 To(BlameMoment.Wire w) => w.Layer == 0 ? HiddenPosition(w.Neuron) : OutputPosition(w.Neuron);

            // Every wire burns by its real blame. Progress runs backward (answers 0, pixels 1), so the light flows back.
            var all = new Ribbons();
            var rng = new System.Random(5);
            foreach (BlameMoment.Wire w in wires)
            {
                float size = Mathf.Sqrt((float)(System.Math.Abs(w.Gradient) / biggest));
                float from = w.Layer == 0 ? 1f : 0.5f, to = w.Layer == 0 ? 0.5f : 0f;
                all.Link(From(w), To(w), Vector3.zero, size, 1f, from, to, (float)rng.NextDouble());
            }

            GameObject links = AddMeshObject("BlameLinks", blame, SaveMesh(ToLinkMesh(all), "L1_BlameLinks"), "BlameLinks");

            // The brightest few: thick, pulsing, and pointable. A nudged one cools (BlameView).
            int top = Mathf.Min(6, wires.Count);
            var targets = new Ribbons();
            Transform pointables = Make("Targets", blame, Vector3.zero).transform;
            for (int k = 0; k < top; k++)
            {
                BlameMoment.Wire w = wires[k];
                Vector3 a = From(w), b = To(w);
                targets.Link(a, b, Vector3.zero, 1f, 1f, 0f, 0f, k / (float)top);
                var target = Make("Wire" + k, pointables, (a + b) * 0.5f);
                target.transform.localRotation = Quaternion.LookRotation((b - a).normalized, Vector3.up);
                var capsule = target.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.height = Vector3.Distance(a, b);
                capsule.radius = 0.55f;
                var interactable = target.AddComponent<BlameWireInteractable>();
                interactable.SetWire(k);
                UnityEventTools.AddPersistentListener(interactable.Pointed, moment.Nudge);
            }

            GameObject topWires = AddMeshObject("TopWires", blame, SaveMesh(ToLinkMesh(targets), "L1_BlameTop"), "BlameTop");
            Int(moment, "topCount", top);

            var view = root.AddComponent<BlameView>();
            Ref(view, "director", director);
            Ref(view, "dash", dash);
            Ref(view, "moment", moment);
            Ref(view, "blameLinks", links.GetComponent<Renderer>());
            Ref(view, "topWires", topWires.GetComponent<MeshFilter>());
            Int(view, "vertsPerWire", 16);
            return root;
        }

        // ---------- region 7: right ----------

        private static GameObject BuildRightRegion(Layout layout, RideDirector director, DashScreenView dash, ForwardPassMoment moment)
        {
            GameObject root = RegionRoot(7, layout.GoldNetwork);
            NetworkParts gold = BuildNetwork(root.transform, true, "Gold");
            AddNetworkView(root, gold, director, dash, moment, _theme.gold, "", -1);

            // The accuracy and the count, from the bake, over the middle of the network.
            Transform accuracy = Make("AccuracySign", root.transform, new Vector3(0f, 12.3f, -0.5f)).transform;
            TextMeshPro acc = Text("Text", accuracy, Vector3.zero,
                _data.TestAccuracyPercent + "% RIGHT ON " + _data.File.testCount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)
                + " NEW DIGITS\nTRAINED ON " + _data.File.trainCount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture),
                1.2f, _theme.gold, 24f, bold: true);
            acc.rectTransform.sizeDelta = new Vector2(24f, 3.2f);
            LabelBacking(acc, "92% RIGHT ON 10,000 NEW DIGITS", 0.25f);
            FaceToward(accuracy, layout.RightView.position + Vector3.up * 1.3f);

            BuildDigitWalls(root.transform);

            // The teaser: the same machinery, reading words.
            Transform words = Make("Words", root.transform, new Vector3(0f, 1.3f, -6f)).transform;
            TextMeshPro wordText = Text("Text", words, Vector3.zero, "the  cat  sat  on  the  mat", 0.9f, _theme.offWhite, 16f, bold: true);
            LabelBacking(wordText, "the  cat  sat  on  the  mat", 0.3f);
            FaceToward(words, layout.EndStop.position + Vector3.up * 1.3f);
            return root;
        }

        /// <summary>The 60 real test digits the trained network read, with its real answer under each (wrong ones in red).</summary>
        private static void BuildDigitWalls(Transform root)
        {
            Level1Digit[] digits = _data.File.finale.digits;
            Transform walls = Make("DigitWall", root, Vector3.zero).transform;
            const int perWall = 30, columns = 10;
            const float cell = 0.17f, pitch = 1.65f;
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                var tiles = new TileMesh();
                for (int n = 0; n < perWall && side * perWall + n < digits.Length; n++)
                {
                    Level1Digit d = digits[side * perWall + n];
                    int row = n / columns, col = n % columns;
                    // Local to the wall: x along the track (toward +z), y up. The wall faces the track.
                    float along = -Layout.ViewDistance + 1.5f + col * pitch;
                    float up = 3.9f - row * 1.45f;
                    for (int r = 0; r < 7; r++)
                    {
                        for (int c = 0; c < 7; c++)
                        {
                            Color ink = Ink(d.pixels7[r * 7 + c]);
                            ink = Color.Lerp(new Color(0.12f, 0.1f, 0.05f, 0f), _theme.gold, ink.r);
                            ink.a = (float)d.pixels7[r * 7 + c];
                            float z = along + (c - 3) * cell * sign * -1f;
                            tiles.Quad(
                                new Vector3(0f, up + (3 - r) * cell - cell / 2, z - cell / 2),
                                new Vector3(0f, up + (3 - r) * cell - cell / 2, z + cell / 2),
                                new Vector3(0f, up + (3 - r) * cell + cell / 2, z + cell / 2),
                                new Vector3(0f, up + (3 - r) * cell + cell / 2, z - cell / 2),
                                new Vector3(-sign, 0f, 0f), ink);
                        }
                    }

                    Color answer = d.prediction == d.label ? _theme.offWhite : _theme.wrongRed;
                    answer.a = 1f;
                    AddGlyph(tiles, d.prediction, along, up - 0.86f, 0.075f, -sign, new Vector3(-sign, 0f, 0f), answer);
                }

                var wall = Make(side == 0 ? "Left" : "Right", walls, new Vector3(sign * 6.5f, 0f, 0f));
                TileObject("Tiles", wall.transform, tiles, "TilesBright");
            }
        }

        // A 3×5 bitmap font for the answers under the finale's digits, drawn as tiles in the same mesh: exact
        // alignment and no extra draw calls. Rows top to bottom, '#' = lit.
        private static readonly string[] DigitGlyphs =
        {
            "###|#.#|#.#|#.#|###", "..#|..#|..#|..#|..#", "###|..#|###|#..|###", "###|..#|###|..#|###", "#.#|#.#|###|..#|..#",
            "###|#..|###|..#|###", "###|#..|###|#.#|###", "###|..#|..#|..#|..#", "###|#.#|###|#.#|###", "###|#.#|###|..#|###",
        };

        /// <summary>Adds digit n as 3×5 tiles centred at (along, up) on a wall facing normal; columns run toward +right.</summary>
        private static void AddGlyph(TileMesh tiles, int n, float along, float up, float cell, float rightSign, Vector3 normal, Color color)
        {
            string[] rows = DigitGlyphs[n].Split('|');
            for (int r = 0; r < 5; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (rows[r][c] != '#') continue;
                    float z = along + (c - 1) * cell * rightSign;
                    float y = up + (2 - r) * cell;
                    float h = cell * 0.45f;
                    tiles.Quad(new Vector3(0.01f * normal.x, y - h, z - h), new Vector3(0.01f * normal.x, y - h, z + h),
                        new Vector3(0.01f * normal.x, y + h, z + h), new Vector3(0.01f * normal.x, y + h, z - h), normal, color);
                }
            }
        }

        // ---------- meshes ----------

        private static Mesh ToLinkMesh(Ribbons r)
        {
            var mesh = new Mesh { name = "Links" };
            if (r.Position.Count > 60000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(r.Position);
            mesh.SetNormals(r.Direction);
            mesh.SetUVs(0, r.Uv);
            mesh.SetColors(r.Data);
            mesh.SetTriangles(r.Triangles, 0);
            mesh.RecalculateBounds();
            Bounds b = mesh.bounds;
            b.Expand(4f);
            mesh.bounds = b;
            return mesh;
        }

        private static Mesh ToNodeMesh(Billboards n)
        {
            var mesh = new Mesh { name = "Nodes" };
            if (n.Position.Count > 60000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(n.Position);
            mesh.SetUVs(0, n.Corner);
            mesh.SetUVs(1, n.Radius);
            mesh.SetColors(n.Data);
            mesh.SetTriangles(n.Triangles, 0);
            mesh.RecalculateBounds();
            Bounds b = mesh.bounds;
            b.Expand(3f);
            mesh.bounds = b;
            return mesh;
        }
    }
}
