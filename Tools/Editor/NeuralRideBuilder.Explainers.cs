using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;
using Convergence.Presentation.Ride;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.EditorTools
{
    /// <summary>
    /// The explainers of Level 1 v4 (TASK_LEVEL1_V4_EXPLAINERS): one component per beat (E0 to E8) on the director's
    /// object, its pieces under its region's root (Region_N/Explainer_EN, saved off: the explainer shows them), its
    /// stage (the region pieces it keeps dark until it unfolds) and its unfolds (explainer piece X becomes region
    /// object Y). E0 (the dock's threes) and E1 (pixels) are built in full from the bake; E2 to E8 get their stage
    /// and unfold cue, and WP3 to WP5 add their pieces here.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        // The segments (Flow): which one explains each beat and by which one it must have unfolded.
        private const int SegPixels = 0, SegOverPixels = 1, SegNeuronExplain = 2, SegNeuronPlay = 3, SegNetworkExplain = 4, SegNetworkSend = 5,
            SegWrong = 6, SegBackExplain = 7, SegRewind = 8, SegSwitch = 9, SegRim = 10, SegValley = 11, SegRight = 12, SegTokens = 13;

        private static readonly Color BoardDark = new Color(0.11f, 0.13f, 0.18f);

        private struct UnfoldSpec
        {
            public Transform Piece, Target;
            public Explainer.UnfoldStyle Style;
            public float Delay, Duration;

            public UnfoldSpec(Transform piece, Transform target, Explainer.UnfoldStyle style, float delay, float duration)
            {
                Piece = piece;
                Target = target;
                Style = style;
                Delay = delay;
                Duration = duration;
            }
        }

        private static void MakeExplainerMaterials()
        {
            Material Flat(string name, Texture tex)
            {
                string path = MatDir + "/" + name + ".mat";
                Shader shader = Shader.Find("Convergence/ExplainerFlat");
                if (shader == null) throw new FileNotFoundException("Shader Convergence/ExplainerFlat is missing (Assets/Shaders/ExplainerFlat.shader).");
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(shader);
                    AssetDatabase.CreateAsset(m, path);
                }

                m.shader = shader;
                m.SetColor("_BaseColor", Color.white);
                m.SetTexture("_BaseMap", tex);
                m.renderQueue = (int)RenderQueue.Transparent;
                EditorUtility.SetDirty(m);
                Mats[name] = m;
                return m;
            }

            double[] hero = _data.File.hero.pixels28;
            Texture2D heroInk = PageTexture("HeroInkWhite", 280, (u, v) =>
            {
                float ink = SampleBilinear(hero, 28, u, v);
                return new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(ink * 1.25f)));
            }, FilterMode.Bilinear);
            Flat("ExplainerFlat", null);
            Flat("ExplainerHero", heroInk);
            Flat("ExplainerThrees", ThreesAtlas());

            // The blackboard's faint distant grid: additive-looking thin lines, alpha driven by RegionAtmosphere.
            Material grid = Flat("BlackboardGrid", null);
            grid.SetColor("_BaseColor", new Color(_theme.explainBlue.r, _theme.explainBlue.g, _theme.explainBlue.b, 0.32f));
        }

        private const int AtlasColumns = 5, AtlasRows = 4, AtlasCell = 128;

        /// <summary>The twenty real threes of the dock's gallery in one texture (one draw), white ink with soft edges.</summary>
        private static Texture2D ThreesAtlas()
        {
            string path = TexDir + "/ThreesAtlas.png";
            Level1PictureSet(out double[][] threes);
            int w = AtlasColumns * AtlasCell, h = AtlasRows * AtlasCell;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, clear);
            }

            const int inner = 112, pad = (AtlasCell - inner) / 2;
            for (int k = 0; k < threes.Length && k < AtlasColumns * AtlasRows; k++)
            {
                int cx = k % AtlasColumns, cy = AtlasRows - 1 - k / AtlasColumns;
                for (int y = 0; y < inner; y++)
                {
                    for (int x = 0; x < inner; x++)
                    {
                        float ink = SampleBilinear(threes[k], 28, (x + 0.5f) / inner, (y + 0.5f) / inner);
                        tex.SetPixel(cx * AtlasCell + pad + x, cy * AtlasCell + pad + y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(ink * 1.25f))));
                    }
                }
            }

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Trilinear;
            imp.mipmapEnabled = true;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void Level1PictureSet(out double[][] threes)
        {
            var list = _data.File.threes;
            threes = new double[list.Length][];
            for (int i = 0; i < list.Length; i++) threes[i] = list[i].pixels28;
        }

        private static Rect AtlasRect(int k)
        {
            int cx = k % AtlasColumns, cy = AtlasRows - 1 - k / AtlasColumns;
            float pad = (AtlasCell - 112) / 2f;
            return new Rect((cx * AtlasCell + pad) / (AtlasColumns * AtlasCell), (cy * AtlasCell + pad) / (AtlasRows * AtlasCell),
                112f / (AtlasColumns * AtlasCell), 112f / (AtlasRows * AtlasCell));
        }

        // ---------- helpers ----------

        private static ShapeBatch Batch(string name, Transform parent, IList<Shape> shapes, string material, int order)
        {
            var go = Make(name, parent, Vector3.zero);
            go.AddComponent<MeshFilter>();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mat(material);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = order;
            var batch = go.AddComponent<ShapeBatch>();
            batch.SetShapes(shapes);
            return batch;
        }

        private static Shape Square(Vector3 at, float size, Color color, float round = 0.08f)
        {
            return new Shape { kind = Shape.Kind.Quad, position = at, size = new Vector2(size, size), color = color, round = round };
        }

        private static Shape Segment(Vector3 a, Vector3 b, float width, Color color, Quaternion? plane = null)
        {
            return new Shape { kind = Shape.Kind.Line, position = a, end = b, size = new Vector2(width, width), color = color, round = 1f, rotation = plane ?? Quaternion.identity };
        }

        /// <summary>Explainer text in CMU Serif: letter-spaced titles, numbers, a formula.</summary>
        private static TextMeshPro Serif(string name, Transform parent, Vector3 pos, string text, float meters, Color color, float width = 4f,
            TextAlignmentOptions align = TextAlignmentOptions.Center, int order = 5)
        {
            TextMeshPro t = Text(name, parent, pos, text, meters, color, width, align);
            t.font = ExplainerFont();
            t.fontStyle = FontStyles.Normal;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.GetComponent<Renderer>().sortingOrder = order;
            return t;
        }

        private static string Num(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

        // ---------- all of them ----------

        private static void BuildExplainers(GameObject host, RideDirector rd, DashScreenView dash, RegionAtmosphere atmosphere, GameObject[] regions)
        {
            var explainers = Make("Explainers", host.transform, Vector3.zero);

            T Add<T>(RideLine[] lines, int explain, int last, bool blackboard, GameObject content, GameObject[] hidden, List<UnfoldSpec> unfolds) where T : Explainer
            {
                var e = explainers.AddComponent<T>();
                Ref(e, "director", rd);
                Ref(e, "dash", dash);
                Ref(e, "atmosphere", atmosphere);
                Ref(e, "theme", _theme);
                Set(e, "lines", p =>
                {
                    p.arraySize = lines.Length;
                    for (int i = 0; i < lines.Length; i++) p.GetArrayElementAtIndex(i).intValue = (int)lines[i];
                });
                Int(e, "explainSegment", explain);
                Int(e, "lastSegment", last);
                Set(e, "blackboard", p => p.boolValue = blackboard);
                Ref(e, "content", content);
                Refs(e, "hidden", hidden ?? new GameObject[0]);
                Set(e, "unfolds", p =>
                {
                    unfolds = unfolds ?? new List<UnfoldSpec>();
                    p.arraySize = unfolds.Count;
                    for (int i = 0; i < unfolds.Count; i++)
                    {
                        SerializedProperty u = p.GetArrayElementAtIndex(i);
                        u.FindPropertyRelative("piece").objectReferenceValue = unfolds[i].Piece;
                        u.FindPropertyRelative("target").objectReferenceValue = unfolds[i].Target;
                        u.FindPropertyRelative("style").enumValueIndex = (int)unfolds[i].Style;
                        u.FindPropertyRelative("delay").floatValue = unfolds[i].Delay;
                        u.FindPropertyRelative("duration").floatValue = unfolds[i].Duration;
                    }
                });
                if (content != null) content.SetActive(false);
                return e;
            }

            GameObject Child(int region, string path) => regions[region].transform.Find(path).gameObject;
            Transform T(int region, string path) => regions[region].transform.Find(path);
            var grow = Explainer.UnfoldStyle.Grow;

            // E0, E1: the quality reference.
            BuildDock(Add<DockExplainer>(new[] { RideLine.Threes, RideLine.MeetPixel, RideLine.PixelBrain, RideLine.PullGo }, -1, SegPixels, true,
                null, new[] { Child(0, "Page"), Child(0, "Desk") }, null), regions[0]);
            BuildPixels(Add<PixelsExplainer>(new[] { RideLine.PixelsSquares, RideLine.PixelsNumbers, RideLine.PixelsFortyNine, RideLine.PixelsFly }, SegPixels, SegPixels, true,
                null, new[] { Child(1, "PixelField") }, null), regions[1]);

            // E2 to E8: stage and unfold cue now; their pieces come with WP3 to WP5.
            BuildNeuron(Add<NeuronExplainer>(new[] { RideLine.NeuronHolds, RideLine.NeuronWires, RideLine.NeuronGreenRed, RideLine.NeuronAdds, RideLine.NeuronYourTurn },
                SegNeuronExplain, SegNeuronPlay, true, null, new[] { Child(2, "Neuron"), Child(2, "Panel") },
                new List<UnfoldSpec> { new UnfoldSpec(null, T(2, "Neuron"), grow, 1.1f, 1.4f), new UnfoldSpec(null, T(2, "Panel"), grow, 0.6f, 1.2f) }), regions[2]);
            BuildNetworkDiagram(Add<NetworkExplainer>(new[] { RideLine.NetworkTeam, RideLine.NetworkAnswers, RideLine.NetworkWire, RideLine.NetworkSend },
                SegNetworkExplain, SegNetworkSend, true, null, new[] { Child(3, "Network") }, null), regions[3]);
            BuildLoss(Add<LossExplainer>(new[] { RideLine.WrongSays, RideLine.WrongRandom, RideLine.WrongWanted, RideLine.WrongLoss }, SegWrong, SegWrong, false, null, null, null), regions[4]);
            BuildBackprop(Add<BackpropExplainer>(new[] { RideLine.BackWish, RideLine.BackFlows, RideLine.BackHot, RideLine.BackGrab }, SegBackExplain, SegRewind, false,
                null, new[] { Child(5, "Blame") }, new List<UnfoldSpec> { new UnfoldSpec(null, T(5, "Blame"), Explainer.UnfoldStyle.Show, 0f, 0.1f) }), regions[5]);
            BuildDescent(Add<DescentExplainer>(new[] { RideLine.DescentHill, RideLine.DescentStep, RideLine.DescentTooBig, RideLine.DescentRealHill }, SegRim, SegValley, true,
                null, new[] { Child(6, "Ground"), Child(6, "Deck"), Child(6, "Sign"), Child(6, "Sun") },
                new List<UnfoldSpec>
                {
                    new UnfoldSpec(null, T(6, "Ground"), Explainer.UnfoldStyle.GrowUp, 0.9f, 2.2f),
                    new UnfoldSpec(null, T(6, "Deck"), grow, 1.1f, 1f),
                    new UnfoldSpec(null, T(6, "Sign"), grow, 1.4f, 1f),
                    new UnfoldSpec(null, T(6, "Sun"), grow, 1.7f, 1.5f),
                }), regions[6]);
            BuildTraining(Add<TrainingExplainer>(new[] { RideLine.RightSays, RideLine.RightTraining, RideLine.RightAccuracy }, SegRight, SegRight, false, null, null, null), regions[7]);
            BuildTokens(Add<TokensExplainer>(new[] { RideLine.TokensWords, RideLine.TokensNumbers, RideLine.TokensPredict, RideLine.TokensNext }, SegTokens, SegTokens, false, null, null, null), regions[7]);
        }

        // ---------- E0, the dock ----------

        private static void BuildDock(DockExplainer e, GameObject region)
        {
            Vector3 eye = new Vector3(0f, 1.3f, 0f);
            // The content lives in world space at the dock: the region root sits at the page.
            Transform content = Make("Explainer_E0", region.transform, -region.transform.position).transform;
            Transform cloud = Make("Cloud", content, eye).transform;
            Color white = _theme.explainWhite, blue = _theme.explainBlue;

            // 21 places around the rider: three rows, the middle one leaving room for the single "3" in the centre;
            // the hero 3 takes the place left of the centre.
            var slots = new List<Vector3>();
            var rng = new System.Random(3);
            float J(float amount) => (float)(rng.NextDouble() * 2.0 - 1.0) * amount;
            void Row(float elevation, float[] azimuths)
            {
                foreach (float az in azimuths)
                {
                    float d = 6.4f + J(0.6f);
                    Quaternion q = Quaternion.Euler(-(elevation + J(1.5f)), az + J(1.5f), 0f);
                    slots.Add(q * Vector3.forward * d);
                }
            }

            // Inside the reading cone (about +-40 degrees across, -8 to +18 up), so none is cut off at the edge of the view.
            Row(16f, new[] { -40f, -27f, -13.5f, 0f, 13.5f, 27f, 40f });
            Row(4.5f, new[] { -40f, -27f, -14f, 14f, 27f, 40f });
            Row(-7f, new[] { -42f, -30f, -18f, -6f, 6f, 18f, 30f, 42f });
            const int heroSlot = 9; // middle row, just left of the centre
            Vector3 heroAt = slots[heroSlot];
            slots.RemoveAt(heroSlot);

            var threes = new List<Shape>();
            var rays = new List<Shape>();
            Vector3 glyphAt = Quaternion.Euler(-5f, 0f, 0f) * Vector3.forward * 6.6f;
            for (int k = 0; k < slots.Count && k < _data.File.threes.Length; k++)
            {
                Vector3 p = slots[k];
                float size = 0.95f + J(0.1f);
                threes.Add(new Shape
                {
                    kind = Shape.Kind.Quad, position = p, rotation = Quaternion.LookRotation(p.normalized, Vector3.up), size = new Vector2(size, size),
                    color = white, uv = AtlasRect(k), round = 0f
                });
                Vector3 toGlyph = (glyphAt - p).normalized;
                rays.Add(Segment(p + toGlyph * (size * 0.5f), glyphAt - toGlyph * 0.7f, 0.011f, new Color(blue.r, blue.g, blue.b, 0.6f),
                    Quaternion.LookRotation(((p + glyphAt) * 0.5f).normalized, Vector3.up)));
            }

            ShapeBatch threesBatch = Batch("Threes", cloud, threes, "ExplainerThrees", 1);
            ShapeBatch raysBatch = Batch("Rays", cloud, rays, "ExplainerFlat", 2);
            TextMeshPro glyph = Serif("Three", cloud, glyphAt, "3", 1.25f, white, 2f);
            glyph.transform.rotation = Quaternion.LookRotation(glyphAt.normalized, Vector3.up);

            // The hero 3 (its own quad, so it can fly into the Page's ink), and where it glides to.
            var hero = Quad("Hero3", content, eye + heroAt, Vector2.one, "ExplainerHero");
            hero.transform.rotation = Quaternion.LookRotation(heroAt.normalized, Vector3.up);
            hero.GetComponent<Renderer>().sortingOrder = 3;
            Transform centre = Make("HeroCentre", content, eye + Quaternion.Euler(-6f, 0f, 0f) * Vector3.forward * 4.6f).transform;
            centre.localScale = Vector3.one * 1.9f;
            Make("PixelAnchor", content, new Vector3(1.15f, 1.45f, 1.9f));

            Transform ink = region.transform.Find("Page/Ink");
            Transform paper = region.transform.Find("Page/Paper");
            Transform paperBack = region.transform.Find("Page/PaperBack");
            Transform desk = region.transform.Find("Desk");
            var g = Explainer.UnfoldStyle.Grow;
            SetUnfolds(e, new List<UnfoldSpec>
            {
                new UnfoldSpec(hero.transform, ink, Explainer.UnfoldStyle.Morph, 0f, 1.6f),
                new UnfoldSpec(null, paper, g, 0.15f, 1.2f),
                new UnfoldSpec(null, paperBack, g, 0.15f, 1.2f),
                new UnfoldSpec(null, desk, g, 0.6f, 1.4f),
            });

            Ref(e, "content", content.gameObject);
            Ref(e, "cloud", cloud);
            Ref(e, "threes", threesBatch);
            Ref(e, "rays", raysBatch);
            Ref(e, "glyph", glyph);
            Ref(e, "hero", hero.transform);
            Ref(e, "heroRenderer", hero.GetComponent<Renderer>());
            Ref(e, "heroCentre", centre);
            Ref(e, "pixelAnchor", content.Find("PixelAnchor"));
            content.gameObject.SetActive(false);
        }

        private static void SetUnfolds(Explainer e, List<UnfoldSpec> unfolds)
        {
            Set(e, "unfolds", p =>
            {
                p.arraySize = unfolds.Count;
                for (int i = 0; i < unfolds.Count; i++)
                {
                    SerializedProperty u = p.GetArrayElementAtIndex(i);
                    u.FindPropertyRelative("piece").objectReferenceValue = unfolds[i].Piece;
                    u.FindPropertyRelative("target").objectReferenceValue = unfolds[i].Target;
                    u.FindPropertyRelative("style").enumValueIndex = (int)unfolds[i].Style;
                    u.FindPropertyRelative("delay").floatValue = unfolds[i].Delay;
                    u.FindPropertyRelative("duration").floatValue = unfolds[i].Duration;
                }
            });
        }

        // ---------- E1, pixels ----------

        private static void BuildPixels(PixelsExplainer e, GameObject region)
        {
            double[] p28 = _data.File.hero.pixels28, p7 = _data.File.hero.pixels7;
            Color white = _theme.explainWhite, blue = _theme.explainBlue, yellow = _theme.explainYellow;
            // Brightness to grey with a gentle curve, so the soft 7x7 still reads as a 3 (the numbers stay the real ones).
            Color Cell(double ink) => Color.Lerp(BoardDark, white, Mathf.Pow((float)ink, 0.75f));

            Transform content = Make("Explainer_E1", region.transform, Vector3.zero).transform;
            // The board: 5.5 m ahead of the E1 stop's eye, a little left, so the lens has room on the right.
            Vector3 eye = Layout_GridEye();
            Transform stage = Make("Stage", content, eye + new Vector3(-1.05f, 0.15f, 5.2f)).transform;
            const float pitch28 = 0.11f, pitch7 = 0.44f;

            var picture = Quad("Picture", stage, new Vector3(0f, 0f, 0.02f), new Vector2(28 * pitch28, 28 * pitch28), "ExplainerHero");
            picture.GetComponent<Renderer>().sortingOrder = 0;

            var grid = new List<Shape>();
            float half = 14 * pitch28;
            for (int i = 0; i <= 28; i++)
            {
                float x = -half + i * pitch28;
                grid.Add(Segment(new Vector3(x, half, -0.01f), new Vector3(x, -half, -0.01f), 0.007f, new Color(blue.r, blue.g, blue.b, 0.6f)));
            }

            for (int i = 0; i <= 28; i++)
            {
                float y = half - i * pitch28;
                grid.Add(Segment(new Vector3(-half, y, -0.01f), new Vector3(half, y, -0.01f), 0.007f, new Color(blue.r, blue.g, blue.b, 0.6f)));
            }

            var squares28 = new List<Shape>();
            for (int r = 0; r < 28; r++)
            {
                for (int c = 0; c < 28; c++) squares28.Add(Square(new Vector3((c - 13.5f) * pitch28, (13.5f - r) * pitch28, 0f), pitch28 * 0.9f, Cell(p28[r * 28 + c]), 0.1f));
            }

            var squares7 = new List<Shape>();
            for (int r = 0; r < 7; r++)
            {
                for (int c = 0; c < 7; c++) squares7.Add(Square(new Vector3((c - 3) * pitch7, (3 - r) * pitch7, 0f), pitch7 * 0.92f, Cell(p7[r * 7 + c]), 0.06f));
            }

            // The lens: the 5×5 patch with the most going on (dark, bright and in between), lifted out to the right.
            PickPatch(p28, out int pr, out int pc);
            Vector3 zoomCentre = new Vector3(2.85f, -0.15f, -1.3f);
            const float zoomPitch = 0.38f;
            var zoom = new List<Shape>();
            var source = new List<int>();
            int zeroCell = -1, oneCell = -1;
            for (int r = 0; r < 5; r++)
            {
                for (int c = 0; c < 5; c++)
                {
                    int src = (pr + r) * 28 + (pc + c);
                    double v = System.Math.Round(p28[src], 1);
                    source.Add(src);
                    zoom.Add(Square(zoomCentre + new Vector3((c - 2) * zoomPitch, (2 - r) * zoomPitch, 0f), zoomPitch * 0.92f, Cell(p28[src]), 0.06f));
                    int k = r * 5 + c, d = System.Math.Abs(c - 2) + System.Math.Abs(r - 2);
                    if (v <= 0.0 && (zeroCell < 0 || d < System.Math.Abs(zeroCell % 5 - 2) + System.Math.Abs(zeroCell / 5 - 2))) zeroCell = k;
                    if (v >= 1.0 && (oneCell < 0 || d < System.Math.Abs(oneCell % 5 - 2) + System.Math.Abs(oneCell / 5 - 2))) oneCell = k;
                }
            }

            if (zeroCell < 0 || oneCell < 0) throw new System.InvalidOperationException("E1's lens patch has no 0.0 or no 1.0 pixel; PickPatch must find one.");

            // The patch outline on the board, the lens outline, and the two lines between them.
            float ph = 2.5f * pitch28, zh = 2.5f * zoomPitch;
            Vector3 pcen = new Vector3((pc + 2 - 13.5f) * pitch28, (13.5f - (pr + 2)) * pitch28, -0.02f);
            var lens = new List<Shape>();
            void Outline(Vector3 c, float h, float width)
            {
                Vector3 a = c + new Vector3(-h, h, 0), b = c + new Vector3(h, h, 0), d = c + new Vector3(h, -h, 0), f = c + new Vector3(-h, -h, 0);
                lens.Add(Segment(a, b, width, yellow));
                lens.Add(Segment(b, d, width, yellow));
                lens.Add(Segment(d, f, width, yellow));
                lens.Add(Segment(f, a, width, yellow));
            }

            Outline(pcen, ph, 0.012f);
            Outline(zoomCentre + new Vector3(0, 0, -0.01f), zh + 0.03f, 0.014f);
            Color faint = new Color(yellow.r, yellow.g, yellow.b, 0.55f);
            lens.Add(Segment(pcen + new Vector3(ph, ph, 0), zoomCentre + new Vector3(-zh - 0.03f, zh + 0.03f, -0.01f), 0.008f, faint));
            lens.Add(Segment(pcen + new Vector3(ph, -ph, 0), zoomCentre + new Vector3(-zh - 0.03f, -zh - 0.03f, -0.01f), 0.008f, faint));

            // The column: 49 circles, the input layer (E3 uses it again).
            var column = new List<Shape>();
            const float columnPitch = 0.062f;
            Vector3 columnTop = new Vector3(2.6f, 24 * columnPitch, -0.4f);
            for (int i = 0; i < 49; i++)
            {
                column.Add(new Shape
                {
                    kind = Shape.Kind.Quad, position = columnTop + Vector3.down * (i * columnPitch), size = Vector2.one * 0.055f,
                    color = new Color(white.r, white.g, white.b, (float)p7[i]), stroke = white, strokeWidth = 0.0065f, round = 1f
                });
            }

            ShapeBatch b28 = Batch("Pixels28", stage, squares28, "ExplainerFlat", 1);
            ShapeBatch bGrid = Batch("Grid28", stage, grid, "ExplainerFlat", 2);
            ShapeBatch b7 = Batch("Pixels7", stage, squares7, "ExplainerFlat", 1);
            ShapeBatch bZoom = Batch("Lens", stage, zoom, "ExplainerFlat", 3);
            ShapeBatch bLens = Batch("LensLines", stage, lens, "ExplainerFlat", 4);
            ShapeBatch bColumn = Batch("Column", stage, column, "ExplainerFlat", 3);

            // The numbers on the lens: one label per square, centred on it, dark on bright squares and light on dark ones.
            Transform valueRoot = Make("Values", stage, Vector3.zero).transform;
            var numbers = new List<Object>();
            for (int r = 0; r < 5; r++)
            {
                for (int c = 0; c < 5; c++)
                {
                    double v = p28[(pr + r) * 28 + (pc + c)];
                    Color ink = v > 0.45 ? _theme.voidNavy : white;
                    TextMeshPro t = Serif("Value" + (r * 5 + c), valueRoot, zoomCentre + new Vector3((c - 2) * zoomPitch, (2 - r) * zoomPitch, -0.02f), Num(v), 0.15f, ink, zoomPitch, TextAlignmentOptions.Center, 6);
                    t.rectTransform.sizeDelta = new Vector2(zoomPitch, zoomPitch);
                    t.verticalAlignment = VerticalAlignmentOptions.Geometry;
                    numbers.Add(t);
                }
            }

            TextMeshPro count = Serif("Count", stage, columnTop + new Vector3(0.62f, -24 * columnPitch, 0f), "49", 0.34f, white, 1f);
            TextMeshPro title = Serif("Title", stage, new Vector3(1.05f, 1.9f, -0.05f), "PIXELS", 0.34f, white, 6f);
            title.characterSpacing = 14f;

            Transform field = region.transform.Find("PixelField");
            var unfolds = new List<UnfoldSpec> { new UnfoldSpec(null, field.Find("Base"), Explainer.UnfoldStyle.Grow, 0.3f, 1.3f) };
            for (int r = 6; r >= 0; r--)
            {
                float landing = (6 - r) * PixelsExplainer.FlyLagPerRow + 6 * PixelsExplainer.FlyLagPerColumn + PixelsExplainer.FlySeconds;
                unfolds.Add(new UnfoldSpec(null, field.Find("Row" + r), Explainer.UnfoldStyle.GrowUp, landing - 0.1f, 1.2f));
            }

            SetUnfolds(e, unfolds);
            Ref(e, "content", content.gameObject);
            Ref(e, "picture", picture.GetComponent<Renderer>());
            Ref(e, "grid28", bGrid);
            Ref(e, "pixels28", b28);
            Ref(e, "lens", bLens);
            Ref(e, "zoom", bZoom);
            Refs(e, "values", numbers);
            Ref(e, "pixels7", b7);
            Ref(e, "column", bColumn);
            Ref(e, "count", count);
            Ref(e, "title", title);
            Set(e, "zoomSource", p =>
            {
                p.arraySize = source.Count;
                for (int i = 0; i < source.Count; i++) p.GetArrayElementAtIndex(i).intValue = source[i];
            });
            Int(e, "zeroCell", zeroCell);
            Int(e, "oneCell", oneCell);
            Ref(e, "field", field);
            Float(e, "fieldPitch", FieldPitch);
            Float(e, "pillarWidth", PillarWidth);
            content.gameObject.SetActive(false);
        }

        // ---------- E2, one neuron ----------

        private static void BuildNeuron(NeuronExplainer e, GameObject region)
        {
            Color white = _theme.explainWhite, blue = _theme.explainBlue, yellow = _theme.explainYellow, green = _theme.explainGreen, red = _theme.explainRed;
            Level1Puzzle puzzle = _data.File.puzzle;
            double[] one = puzzle.digits[0].pixels7;                // a real "1" of the puzzle (it should light up)
            int[] w = puzzle.referenceSolution;                      // weights in {-1, 0, +1}
            double sum = 0;
            for (int i = 0; i < 49; i++) sum += w[i] * one[i];
            if (sum < puzzle.threshold) throw new System.InvalidOperationException("E2 shows the reference weights lighting up a 1, but its sum " + sum + " is under the line " + puzzle.threshold + ".");

            Transform content = Make("Explainer_E2", region.transform, Vector3.zero).transform;
            // The board 5 m ahead of the stop's eye: the digit on the left, the neuron in the middle-right, the glass right.
            Transform stage = Make("Stage", content, new Vector3(0f, EyePoint.y + 0.1f, 4f)).transform;
            const float pitch = 0.3f, diameter = 0.9f, glassH = 1.6f, glassW = 0.36f;
            Vector3 digitCentre = new Vector3(-1.9f, 0.1f, 0f), neuronAt = new Vector3(0.85f, 0.1f, -0.05f), glassAt = new Vector3(2.35f, 0.1f, 0f);
            Vector3 Cell(int i) => digitCentre + new Vector3((i % 7 - 3) * pitch, (3 - i / 7) * pitch, 0f);

            var digit = new List<Shape>();
            var overlay = new List<Shape>();
            var wires = new List<Shape>();
            var products = new List<Shape>();
            for (int i = 0; i < 49; i++)
            {
                digit.Add(Square(Cell(i), pitch * 0.92f, Color.Lerp(BoardDark, white, Mathf.Pow((float)one[i], 0.75f)), 0.08f));
                Color wc = w[i] > 0 ? green : w[i] < 0 ? red : white;
                Shape o = Square(new Vector3((i % 7 - 3) * pitch, (3 - i / 7) * pitch, -0.01f), pitch * 0.92f, wc, 0.08f);
                o.alpha = w[i] == 0 ? 0f : 1f;
                overlay.Add(o);
                Vector3 from = Cell(i) + new Vector3(pitch * 0.46f, 0f, -0.005f);
                wires.Add(Segment(from, neuronAt + new Vector3(-diameter * 0.45f, 0f, 0.02f), 0.01f, new Color(blue.r, blue.g, blue.b, 0.5f)));
                double share = w[i] * one[i];
                products.Add(new Shape
                {
                    kind = Shape.Kind.Quad, position = Cell(i) + Vector3.back * 0.03f, size = Vector2.one * 0.09f, round = 1f,
                    color = share > 0 ? green : red, alpha = System.Math.Abs(share) > 0.05 ? 1f : 0f
                });
            }

            var body = new Shape { kind = Shape.Kind.Quad, position = Vector3.zero, size = Vector2.one * diameter, round = 1f, color = white, stroke = white, strokeWidth = 0.025f };

            var glass = new List<Shape>
            {
                Segment(glassAt + new Vector3(-glassW / 2, glassH / 2, 0f), glassAt + new Vector3(-glassW / 2, -glassH / 2, 0f), 0.02f, white),
                Segment(glassAt + new Vector3(glassW / 2, glassH / 2, 0f), glassAt + new Vector3(glassW / 2, -glassH / 2, 0f), 0.02f, white),
                Segment(glassAt + new Vector3(-glassW / 2, -glassH / 2, 0f), glassAt + new Vector3(glassW / 2, -glassH / 2, 0f), 0.02f, white),
                new Shape { kind = Shape.Kind.Quad, position = glassAt + new Vector3(0f, 0f, 0.005f), size = new Vector2(glassW - 0.05f, glassH), round = 0.1f, color = blue },
            };
            // The line sits half way up the glass; the fill shows the real sum on that scale.
            float lineLevel = 0.5f, sumLevel = Mathf.Min(0.97f, (float)(sum / puzzle.threshold) * lineLevel);
            float lineY = -glassH / 2 + glassH * lineLevel;
            glass.Add(Segment(glassAt + new Vector3(-glassW / 2 - 0.12f, lineY, -0.02f), glassAt + new Vector3(glassW / 2 + 0.12f, lineY, -0.02f), 0.024f, yellow));

            var sparks = new List<Shape>();
            for (int i = 0; i < 12; i++) sparks.Add(Segment(Vector3.zero, Vector3.right * 0.1f, 0.03f, yellow));

            ShapeBatch bWires = Batch("Wires", stage, wires, "ExplainerFlat", 0);
            ShapeBatch bDigit = Batch("Digit", stage, digit, "ExplainerFlat", 1);
            ShapeBatch bWeights = Batch("Weights", stage, overlay, "ExplainerFlat", 2);
            bWeights.transform.localPosition = digitCentre;
            ShapeBatch bGlass = Batch("Glass", stage, glass, "ExplainerFlat", 2);
            ShapeBatch bNeuron = Batch("Neuron", stage, new List<Shape> { body }, "ExplainerFlat", 3);
            bNeuron.transform.localPosition = neuronAt;
            ShapeBatch bProducts = Batch("Products", stage, products, "ExplainerFlat", 4);
            ShapeBatch bSparks = Batch("Sparks", bNeuron.transform, sparks, "ExplainerFlat", 5);

            TextMeshPro value = Serif("Value", bNeuron.transform, new Vector3(0f, 0f, -0.03f), "0.0", 0.32f, white, 0.8f, TextAlignmentOptions.Center, 6);
            value.verticalAlignment = VerticalAlignmentOptions.Geometry;
            TextMeshPro title = Serif("Title", stage, neuronAt + new Vector3(0f, 0.85f, -0.05f), "NEURON", 0.3f, white, 3f);
            title.characterSpacing = 14f;
            TextMeshPro wTitle = Serif("WeightsTitle", stage, new Vector3((digitCentre.x + 3.5f * pitch + neuronAt.x - diameter * 0.5f) * 0.5f, -0.72f, -0.05f), "WEIGHTS", 0.26f, white, 3f);
            wTitle.characterSpacing = 14f;
            TextMeshPro formula = Serif("Formula", stage, new Vector3(0.4f, -1.25f, -0.05f),
                "<i>w</i><sub>1</sub><i>a</i><sub>1</sub> + <i>w</i><sub>2</sub><i>a</i><sub>2</sub> + ... + <i>w</i><sub>49</sub><i>a</i><sub>49</sub>", 0.24f, white, 5f);

            Ref(e, "content", content.gameObject);
            Ref(e, "digit", bDigit);
            Ref(e, "wires", bWires);
            Ref(e, "weights", bWeights);
            Ref(e, "neuron", bNeuron);
            Ref(e, "products", bProducts);
            Ref(e, "glass", bGlass);
            Ref(e, "sparks", bSparks);
            Ref(e, "value", value);
            Ref(e, "title", title);
            Ref(e, "weightsTitle", wTitle);
            Ref(e, "formula", formula);
            Ref(e, "bigNeuron", region.transform.Find("Neuron"));
            Set(e, "weightSigns", p =>
            {
                p.arraySize = 49;
                for (int i = 0; i < 49; i++) p.GetArrayElementAtIndex(i).intValue = w[i];
            });
            Float(e, "sumLevel", sumLevel);
            Float(e, "lineLevel", lineLevel);
            Float(e, "glassHeight", glassH);
            content.gameObject.SetActive(false);
        }

        // ---------- E3, the network ----------

        /// <summary>How big the E3 diagram is next to the giant network it becomes (it sits on the line from the eye).</summary>
        private const float DiagramScale = 0.2f;

        private static void BuildNetworkDiagram(NetworkExplainer e, GameObject region)
        {
            Color white = _theme.explainWhite, blue = _theme.explainBlue, red = _theme.explainRed;
            Level1Weights w = _data.File.initial;
            double[] p7 = _data.File.hero.pixels7;

            Transform content = Make("Explainer_E3", region.transform, Vector3.zero).transform;
            // The diagram in the network's own coordinates, a fifth of the size, on the line from the stop's eye to the
            // network: from the seat it covers the giant network exactly, so growing into it is one smooth move.
            Vector3 eye = _layout.NetViewStop.position + Vector3.up * EyePoint.y;
            Vector3 net = region.transform.position;
            Transform diagram = Make("Diagram", content, Vector3.zero).transform;
            diagram.position = eye + (net - eye) * DiagramScale;
            diagram.rotation = region.transform.rotation;
            diagram.localScale = Vector3.one * DiagramScale;

            var inputs = new List<Shape>();
            for (int i = 0; i < 49; i++)
            {
                inputs.Add(new Shape { kind = Shape.Kind.Quad, position = InputPosition(i), size = Vector2.one * 1.05f, round = 1f,
                    color = new Color(white.r, white.g, white.b, Mathf.Lerp(0.06f, 1f, (float)p7[i])), stroke = white, strokeWidth = 0.09f });
            }

            var hidden = new List<Shape>();
            for (int j = 0; j < 16; j++)
            {
                hidden.Add(new Shape { kind = Shape.Kind.Quad, position = HiddenPosition(j), size = Vector2.one * 1.5f, round = 1f,
                    color = new Color(BoardDark.r, BoardDark.g, BoardDark.b, 1f), stroke = white, strokeWidth = 0.12f });
            }

            var outputs = new List<Shape>();
            for (int k = 0; k < 10; k++)
            {
                outputs.Add(new Shape { kind = Shape.Kind.Quad, position = OutputPosition(k), size = Vector2.one * 0.95f, round = 1f,
                    color = new Color(BoardDark.r, BoardDark.g, BoardDark.b, 1f), stroke = white, strokeWidth = 0.09f });
            }

            // Every wire, coloured by the sign of its real untrained weight (BLUE +, RED -), as strong as its size.
            double max1 = 1e-9, max2 = 1e-9;
            foreach (double v in w.w1) max1 = System.Math.Max(max1, System.Math.Abs(v));
            foreach (double v in w.w2) max2 = System.Math.Max(max2, System.Math.Abs(v));
            var edges = new List<Shape>();
            Shape Edge(Vector3 a, Vector3 b, double v, double max)
            {
                float size = (float)(System.Math.Abs(v) / max);
                Color c = v >= 0 ? blue : red;
                return Segment(a + Vector3.forward * 0.1f, b + Vector3.forward * 0.1f, Mathf.Lerp(0.04f, 0.13f, size), new Color(c.r, c.g, c.b, Mathf.Lerp(0.08f, 0.85f, size)));
            }

            for (int j = 0; j < 16; j++)
            {
                for (int i = 0; i < 49; i++) edges.Add(Edge(InputPosition(i), HiddenPosition(j), w.w1[j * 49 + i], max1));
            }

            for (int k = 0; k < 10; k++)
            {
                for (int j = 0; j < 16; j++) edges.Add(Edge(HiddenPosition(j), OutputPosition(k), w.w2[k * 16 + j], max2));
            }

            ShapeBatch bEdges = Batch("Edges", diagram, edges, "ExplainerFlat", 0);
            ShapeBatch bInputs = Batch("Inputs", diagram, inputs, "ExplainerFlat", 1);
            ShapeBatch bHidden = Batch("Hidden", diagram, hidden, "ExplainerFlat", 1);
            ShapeBatch bOutputs = Batch("Outputs", diagram, outputs, "ExplainerFlat", 1);

            var labels = new List<Object>();
            for (int k = 0; k < 10; k++)
            {
                TextMeshPro t = Serif("Label" + k, diagram, OutputPosition(k) + new Vector3(1.4f, 0f, -0.1f), k.ToString(), 1.0f, white, 1.5f, TextAlignmentOptions.Left, 3);
                t.verticalAlignment = VerticalAlignmentOptions.Geometry;
                labels.Add(t);
            }

            TextMeshPro layerTitle = Serif("LayerTitle", diagram, HiddenPosition(1) + new Vector3(1.2f, 3.0f, -0.1f), "LAYER", 1.4f, white, 12f);
            layerTitle.characterSpacing = 14f;
            TextMeshPro title = Serif("Title", diagram, new Vector3(0f, 16.2f, -0.1f), "NEURAL NETWORK", 1.8f, white, 30f);
            title.characterSpacing = 14f;

            Ref(e, "content", content.gameObject);
            Ref(e, "inputs", bInputs);
            Ref(e, "layer", bHidden);
            Ref(e, "outputs", bOutputs);
            Ref(e, "edges", bEdges);
            Refs(e, "labels", labels);
            Ref(e, "layerTitle", layerTitle);
            Ref(e, "title", title);
            Int(e, "firstLayerEdges", 16 * 49);
            SetUnfolds(e, new List<UnfoldSpec> { new UnfoldSpec(diagram, region.transform.Find("Network"), Explainer.UnfoldStyle.Morph, 0.3f, 2.4f) });
            content.gameObject.SetActive(false);
        }

        // ---------- E4, wrong and the loss ----------

        private static void BuildLoss(LossExplainer e, GameObject region)
        {
            Color white = _theme.explainWhite, green = _theme.explainGreen, red = _theme.explainRed;
            Activations(false, out _, out _, out double[] outputs, out _);
            int guess = 0;
            for (int k = 1; k < outputs.Length; k++) if (outputs[k] > outputs[guess]) guess = k;
            const int wanted = Level1Data.HeroDigit;

            Transform content = Make("Explainer_E4", region.transform, Vector3.zero).transform;
            // A chart on a dark card, left of the answers so the giant network's own answers and the guess sign stay clear.
            Vector3 eye = _layout.WrongStop.position + Vector3.up * EyePoint.y;
            Transform stage = Make("Stage", content, Vector3.zero).transform;
            stage.position = eye + new Vector3(-1.5f, 0.1f, 5.5f);
            stage.rotation = Quaternion.LookRotation(stage.position - eye, Vector3.up);

            const float rowPitch = 0.27f, barH = 0.17f, length = 2.2f, x0 = -1.5f;
            float RowY(int k) => (4.5f - k) * rowPitch;
            var card = new List<Shape> { new Shape { kind = Shape.Kind.Quad, position = new Vector3(0.05f, 0f, 0.05f), size = new Vector2(4.3f, 3.25f), round = 0.06f, color = new Color(0.04f, 0.05f, 0.08f, 0.97f) } };
            var bars = new List<Shape>();
            var marks = new List<Shape>();
            var gaps = new List<Shape>();
            double total = 0;
            for (int k = 0; k < 10; k++)
            {
                float p = (float)outputs[k], want = k == wanted ? 1f : 0f;
                bars.Add(new Shape { kind = Shape.Kind.Quad, position = new Vector3(x0 + p * length * 0.5f, RowY(k), 0f), size = new Vector2(Mathf.Max(0.01f, p * length), barH), round = 0.2f, color = white });
                float mx = x0 + want * length;
                marks.Add(Segment(new Vector3(mx, RowY(k) + 0.14f, -0.02f), new Vector3(mx, RowY(k) - 0.14f, -0.02f), 0.035f, green));
                float a = Mathf.Min(p, want), b = Mathf.Max(p, want);
                gaps.Add(new Shape { kind = Shape.Kind.Quad, position = new Vector3(x0 + (a + b) * 0.5f * length, RowY(k), -0.01f), size = new Vector2(Mathf.Max(0.01f, (b - a) * length), barH * 0.55f), round = 0.2f, color = red });
                total += b - a;
            }

            // The LOSS: every gap stacked into one tall bar on the right, the same scale for all.
            float unit = Mathf.Min(1.2f, 2.5f / (float)total), y = -1.35f;
            var stackPos = new Vector3[10];
            var stackSize = new Vector2[10];
            for (int k = 0; k < 10; k++)
            {
                float gap = gaps[k].size.x / length * unit;
                stackPos[k] = new Vector3(1.45f, y + gap * 0.5f, -0.01f);
                stackSize[k] = new Vector2(0.4f, Mathf.Max(0.004f, gap - 0.008f));
                y += gap;
            }

            Batch("Card", stage, card, "ExplainerFlat", 20);
            ShapeBatch bBars = Batch("Bars", stage, bars, "ExplainerFlat", 21);
            ShapeBatch bMarks = Batch("Marks", stage, marks, "ExplainerFlat", 23);
            ShapeBatch bGaps = Batch("Gaps", stage, gaps, "ExplainerFlat", 22);
            var labels = new List<Object>();
            for (int k = 0; k < 10; k++)
            {
                TextMeshPro t = Serif("Label" + k, stage, new Vector3(x0 - 0.22f, RowY(k), -0.02f), k.ToString(), 0.2f, white, 0.3f, TextAlignmentOptions.Center, 24);
                t.verticalAlignment = VerticalAlignmentOptions.Geometry;
                labels.Add(t);
            }

            TextMeshPro loss = Serif("LossTitle", stage, new Vector3(1.45f, y + 0.25f, -0.02f), "LOSS", 0.26f, red, 1.6f, TextAlignmentOptions.Center, 24);
            loss.characterSpacing = 12f;

            Ref(e, "content", content.gameObject);
            Ref(e, "bars", bBars);
            Ref(e, "marks", bMarks);
            Ref(e, "gaps", bGaps);
            Refs(e, "labels", labels);
            Ref(e, "lossTitle", loss);
            Int(e, "guess", guess);
            Int(e, "wanted", wanted);
            Set(e, "stackPosition", p =>
            {
                p.arraySize = 10;
                for (int k = 0; k < 10; k++) p.GetArrayElementAtIndex(k).vector3Value = stackPos[k];
            });
            Set(e, "stackSize", p =>
            {
                p.arraySize = 10;
                for (int k = 0; k < 10; k++) p.GetArrayElementAtIndex(k).vector2Value = stackSize[k];
            });
            content.gameObject.SetActive(false);
        }

        // ---------- E5, backpropagation ----------

        private static void BuildBackprop(BackpropExplainer e, GameObject region)
        {
            Color green = _theme.explainGreen, red = _theme.explainRed, yellow = _theme.explainYellow, white = _theme.explainWhite;
            Activations(false, out _, out _, out double[] outputs, out _);
            const int wanted = Level1Data.HeroDigit;

            Transform content = Make("Explainer_E5", region.transform, Vector3.zero).transform;
            // Pixel's answers come back as E4's chart (same place, same scale); each bar gets an arrow for its wish.
            Vector3 eye = _layout.NetViewStop.position + Vector3.up * EyePoint.y;
            Transform stage = Make("Stage", content, Vector3.zero).transform;
            stage.position = eye + new Vector3(-1.5f, 0.1f, 5.5f);
            stage.rotation = Quaternion.LookRotation(stage.position - eye, Vector3.up);
            const float rowPitch = 0.27f, barH = 0.17f, length = 2.2f, x0 = -1.5f;
            float RowY(int k) => (4.5f - k) * rowPitch;
            var card = new List<Shape> { new Shape { kind = Shape.Kind.Quad, position = new Vector3(0.05f, 0f, 0.05f), size = new Vector2(4.3f, 3.25f), round = 0.06f, color = new Color(0.04f, 0.05f, 0.08f, 0.97f) } };
            var bars = new List<Shape>();
            double biggest = 1e-9;
            for (int k = 0; k < 10; k++) biggest = System.Math.Max(biggest, System.Math.Abs(outputs[k] - (k == wanted ? 1.0 : 0.0)));
            var arrows = new List<Shape>();
            for (int k = 0; k < 10; k++)
            {
                float p = (float)outputs[k];
                bars.Add(new Shape { kind = Shape.Kind.Quad, position = new Vector3(x0 + p * length * 0.5f, RowY(k), 0f), size = new Vector2(Mathf.Max(0.01f, p * length), barH), round = 0.2f, color = white });
                // The real gradient at answer k (softmax with cross-entropy): output minus wanted. Negative = wants brighter.
                double g = outputs[k] - (k == wanted ? 1.0 : 0.0);
                float len = 0.2f + 1.0f * (float)(System.Math.Abs(g) / biggest);
                float dir = g < 0 ? 1f : -1f;
                Color c = g < 0 ? green : red;
                Vector3 a = new Vector3(x0 + p * length + (dir > 0 ? 0.08f : -0.02f), RowY(k), -0.02f), b = a + Vector3.right * dir * len;
                if (dir < 0 && b.x < x0 - 0.05f) b.x = x0 - 0.05f;
                float head = 0.09f;
                arrows.Add(Segment(a, b, 0.05f, c));
                arrows.Add(Segment(b, b + new Vector3(-dir * head, head, 0f), 0.05f, c));
                arrows.Add(Segment(b, b + new Vector3(-dir * head, -head, 0f), 0.05f, c));
            }

            Batch("Card", stage, card, "ExplainerFlat", 20);
            ShapeBatch bBars = Batch("Bars", stage, bars, "ExplainerFlat", 21);
            var labels = new List<Object>();
            for (int k = 0; k < 10; k++)
            {
                TextMeshPro t = Serif("Label" + k, stage, new Vector3(x0 - 0.22f, RowY(k), -0.02f), k.ToString(), 0.2f, white, 0.3f, TextAlignmentOptions.Center, 24);
                t.verticalAlignment = VerticalAlignmentOptions.Geometry;
                labels.Add(t);
            }

            var sparks = new List<Shape>();
            for (int i = 0; i < 16 + 49; i++) sparks.Add(Segment(Vector3.zero, Vector3.right, 0.22f, yellow));
            Vector3 outputsAt = Vector3.zero;
            for (int k = 0; k < 10; k++) outputsAt += OutputPosition(k) / 10f;

            Batch("Arrows", stage, arrows, "ExplainerFlat", 23);
            Batch("Sparks", content, sparks, "ExplainerFlat", 4);
            TextMeshPro title = Serif("Title", content, new Vector3(0f, 15.6f, -0.6f), "BACKPROPAGATION", 1.7f, white, 30f);
            title.characterSpacing = 12f;

            Ref(e, "content", content.gameObject);
            Ref(e, "card", stage.Find("Card").GetComponent<ShapeBatch>());
            Ref(e, "bars", bBars);
            Refs(e, "labels", labels);
            Ref(e, "arrows", stage.Find("Arrows").GetComponent<ShapeBatch>());
            Ref(e, "sparks", content.Find("Sparks").GetComponent<ShapeBatch>());
            Ref(e, "title", title);
            Int(e, "wanted", wanted);
            Set(e, "outputsAt", p => p.vector3Value = outputsAt + new Vector3(0f, 0f, -0.5f));
            Set(e, "hiddenAt", p =>
            {
                p.arraySize = 16;
                for (int j = 0; j < 16; j++) p.GetArrayElementAtIndex(j).vector3Value = HiddenPosition(j) + new Vector3(0f, 0f, -0.5f);
            });
            Set(e, "inputAt", p =>
            {
                p.arraySize = 49;
                for (int i = 0; i < 49; i++) p.GetArrayElementAtIndex(i).vector3Value = InputPosition(i) + new Vector3(0f, 0f, -0.5f);
            });
            content.gameObject.SetActive(false);
        }

        // ---------- E6, gradient descent ----------

        private static void BuildDescent(DescentExplainer e, GameObject region)
        {
            Color white = _theme.explainWhite, blue = _theme.explainBlue, yellow = _theme.explainYellow, red = _theme.explainRed, green = _theme.explainGreen;
            const float halfWidth = 2.6f, height = 1.9f, startX = -2.2f;
            float H(float x) => DescentExplainer.Height(x, halfWidth, height);
            Vector3 On(float x, float lift = 0.13f) => new Vector3(x, H(x) + lift, -0.02f);

            Transform content = Make("Explainer_E6", region.transform, Vector3.zero).transform;
            Pose rim = _layout.Rim;
            Vector3 eye = rim.position + Vector3.up * EyePoint.y;
            Transform stage = Make("Stage", content, Vector3.zero).transform;
            stage.SetPositionAndRotation(eye + rim.rotation * new Vector3(0f, 0.15f, 5.5f), rim.rotation);

            var curve = new List<Shape>();
            const int samples = 40;
            for (int i = 0; i < samples; i++)
            {
                float x0 = Mathf.Lerp(-halfWidth, halfWidth, i / (float)samples), x1 = Mathf.Lerp(-halfWidth, halfWidth, (i + 1) / (float)samples);
                curve.Add(Segment(new Vector3(x0, H(x0), 0f), new Vector3(x1, H(x1), 0f), 0.035f, blue));
            }

            var ball = new List<Shape> { new Shape { kind = Shape.Kind.Quad, position = On(startX), size = Vector2.one * 0.24f, round = 1f, color = white } };

            var marks = new List<Shape>();
            // The slope arrow: from the ball, down the tangent.
            float slope = (H(startX + 0.01f) - H(startX - 0.01f)) / 0.02f;
            Vector3 down = new Vector3(1f, slope, 0f).normalized * (slope > 0 ? -1f : 1f);
            Vector3 a = On(startX, 0.32f), b = a + down * 0.8f;
            Vector3 back = -down * 0.22f, side = Vector3.Cross(down, Vector3.forward) * 0.16f;
            marks.Add(Segment(a, b, 0.04f, yellow));
            marks.Add(Segment(b, b + back + side, 0.04f, yellow));
            marks.Add(Segment(b, b + back - side, 0.04f, yellow));
            // The big step's arc (the same path the ball's hop takes).
            float bx0 = startX + 0.55f, bx1 = 1.9f;
            Vector3 Arc(float t) => On(Mathf.Lerp(bx0, bx1, t)) + Vector3.up * DescentExplainer.BigLift * Mathf.Sin(t * Mathf.PI);
            for (int k = 0; k < DescentExplainer.ArcStrokes; k++) marks.Add(Segment(Arc(k / (float)DescentExplainer.ArcStrokes), Arc((k + 1) / (float)DescentExplainer.ArcStrokes), 0.03f, red));
            // The small steps' footprints.
            foreach (float x in new[] { 1.35f, 0.9f, 0.55f, 0.3f, 0.12f, 0.03f })
            {
                marks.Add(new Shape { kind = Shape.Kind.Quad, position = On(x, 0.0f) + Vector3.back * 0.01f, size = Vector2.one * 0.08f, round = 1f, color = green });
            }

            ShapeBatch bCurve = Batch("Curve", stage, curve, "ExplainerFlat", 1);
            ShapeBatch bMarks = Batch("Marks", stage, marks, "ExplainerFlat", 2);
            ShapeBatch bBall = Batch("Ball", stage, ball, "ExplainerFlat", 3);
            TextMeshPro title = Serif("Title", stage, new Vector3(0f, 1.75f, -0.05f), "GRADIENT DESCENT", 0.3f, white, 5f);
            title.characterSpacing = 12f;
            TextMeshPro high = Serif("High", stage, new Vector3(-1.55f, 1.15f, -0.05f), "lots of mistakes", 0.2f, _theme.explainGrey, 2.4f);
            TextMeshPro low = Serif("Low", stage, new Vector3(0f, -1.3f, -0.05f), "hardly any", 0.2f, _theme.explainGrey, 2.4f);

            Ref(e, "content", content.gameObject);
            Ref(e, "curve", bCurve);
            Ref(e, "ball", bBall);
            Ref(e, "marks", bMarks);
            Ref(e, "title", title);
            Ref(e, "high", high);
            Ref(e, "low", low);
            Float(e, "halfWidth", halfWidth);
            Float(e, "height", height);
            Float(e, "startX", startX);
            content.gameObject.SetActive(false);
        }

        // ---------- E7, right and training ----------

        private static void BuildTraining(TrainingExplainer e, GameObject region)
        {
            Color white = _theme.explainWhite, red = _theme.explainRed, green = _theme.explainGreen;
            Transform content = Make("Explainer_E7", region.transform, Vector3.zero).transform;
            Pose view = _layout.RightView;
            Vector3 eye = view.position + Vector3.up * EyePoint.y;
            Transform stage = Make("Stage", content, Vector3.zero).transform;
            stage.SetPositionAndRotation(eye + view.rotation * new Vector3(0f, 0.1f, 4.6f), view.rotation);

            // Real test digits (the finale's), one after another, as 7×7 pictures; they fly into the network's middle.
            Level1Digit[] real = _data.File.finale.digits;
            int count = Mathf.Min(24, real.Length);
            const float pitch = 0.07f;
            var stream = new List<Shape>();
            Vector3 spawn = new Vector3(-2.1f, -0.75f, 0f);
            for (int d = 0; d < count; d++)
            {
                for (int k = 0; k < 49; k++)
                {
                    float ink = (float)real[d].pixels7[k];
                    Shape s = Square(spawn + new Vector3((k % 7 - 3) * pitch, (3 - k / 7) * pitch, -0.01f * d), pitch * 0.92f, white, 0.15f);
                    s.alpha = ink > 0.08f ? ink : 0f;
                    stream.Add(s);
                }
            }

            Vector3 into = stage.InverseTransformPoint(_layout.GoldNetwork.position + _layout.GoldNetwork.rotation * new Vector3(0f, 7f, 0f));

            Level1Hop[] hops = _data.File.valley.hops;
            float lossEnd = hops.Length > 1 ? Mathf.Clamp((float)(hops[hops.Length - 1].loss / hops[0].loss), 0.03f, 1f) : 0.1f;
            var bar = new List<Shape> { new Shape { kind = Shape.Kind.Quad, position = new Vector3(-0.85f, -0.3f, 0f), size = new Vector2(1.6f, 0.16f), round = 0.2f, color = red } };

            // A hundred dots: as many GREEN as the real test accuracy, scattered by a fixed seed, the rest RED.
            int right = _data.TestAccuracyPercent;
            var order = new List<int>();
            for (int i = 0; i < 100; i++) order.Add(i);
            var rng = new System.Random(92);
            for (int i = 99; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            var dots = new List<Shape>();
            for (int i = 0; i < 100; i++)
            {
                bool ok = order[i] < right;
                dots.Add(new Shape { kind = Shape.Kind.Quad, position = new Vector3(0.75f + (i % 10) * 0.12f, 0.15f - (i / 10) * 0.12f, 0f), size = Vector2.one * 0.09f, round = 1f, color = ok ? green : red });
            }

            // A dark card under the numbers, so the digit walls on both sides never compete with them.
            var card = new List<Shape> { new Shape { kind = Shape.Kind.Quad, position = new Vector3(-0.05f, -0.17f, 0.05f), size = new Vector2(4.4f, 1.75f), round = 0.08f, color = new Color(0.04f, 0.05f, 0.08f, 0.94f) } };
            Batch("Card", stage, card, "ExplainerFlat", 20);
            ShapeBatch bStream = Batch("Stream", stage, stream, "ExplainerFlat", 22);
            ShapeBatch bBar = Batch("LossBar", stage, bar, "ExplainerFlat", 21);
            ShapeBatch bDots = Batch("Dots", stage, dots, "ExplainerFlat", 21);
            TextMeshPro counter = Serif("Counter", stage, new Vector3(-0.85f, 0.1f, -0.02f), "0 digits", 0.32f, white, 3.2f, TextAlignmentOptions.Center, 24);
            TextMeshPro lossLabel = Serif("LossLabel", stage, new Vector3(-1.98f, -0.3f, -0.02f), "LOSS", 0.2f, red, 1f, TextAlignmentOptions.Center, 24);
            lossLabel.characterSpacing = 10f;
            TextMeshPro score = Serif("Score", stage, new Vector3(1.29f, 0.45f, -0.02f), right + " out of 100", 0.28f, white, 3.4f, TextAlignmentOptions.Center, 24);

            Ref(e, "content", content.gameObject);
            Ref(e, "stream", bStream);
            Ref(e, "lossBar", bBar);
            Ref(e, "dots", bDots);
            Ref(e, "counter", counter);
            Ref(e, "lossLabel", lossLabel);
            Ref(e, "score", score);
            Int(e, "trainCount", _data.File.trainCount);
            Float(e, "lossEnd", lossEnd);
            Int(e, "perDigit", 49);
            Set(e, "into", p => p.vector3Value = into);
            content.gameObject.SetActive(false);
        }

        // ---------- E8, tokens ----------

        private static void BuildTokens(TokensExplainer e, GameObject region)
        {
            Color white = _theme.explainWhite, blue = _theme.explainBlue, yellow = _theme.explainYellow, green = _theme.explainGreen, red = _theme.explainRed;
            Transform content = Make("Explainer_E8", region.transform, Vector3.zero).transform;
            Pose end = _layout.EndStop;
            Vector3 eye = end.position + Vector3.up * EyePoint.y;
            Transform stage = Make("Stage", content, Vector3.zero).transform;
            stage.SetPositionAndRotation(eye + end.rotation * new Vector3(0f, 0.05f, 5f), end.rotation);

            string[] tokens = { "The", "cat", "sat", "on", "the", "mat" };
            Color[] colours = { blue, yellow, green, red, blue, green };
            float[] widths = { 0.62f, 0.56f, 0.56f, 0.44f, 0.56f, 0.66f };
            const float gap = 0.14f, chipY = 0.45f, chipH = 0.34f;
            float total = -gap;
            for (int k = 0; k < tokens.Length - 1; k++) total += widths[k] + gap;
            float x = -total * 0.5f - 0.6f;
            var chips = new List<Shape>();
            var chipX = new float[tokens.Length];
            for (int k = 0; k < tokens.Length; k++)
            {
                chipX[k] = x + widths[k] * 0.5f;
                Color c = colours[k];
                chips.Add(new Shape { kind = Shape.Kind.Quad, position = new Vector3(chipX[k], chipY, 0f), size = new Vector2(widths[k], chipH), round = 0.35f,
                    color = new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 1f), stroke = c, strokeWidth = 0.025f });
                x += widths[k] + gap;
            }

            // Each token's list of numbers, drawn as squares from dark to bright (made up for the picture, fixed seed).
            const int perToken = 6;
            var rng = new System.Random(8);
            var vectors = new List<Shape>();
            for (int k = 0; k < tokens.Length - 1; k++)
            {
                for (int r = 0; r < perToken; r++)
                {
                    float v = (float)rng.NextDouble();
                    vectors.Add(Square(new Vector3(chipX[k], chipY - 0.4f - r * 0.17f, 0f), 0.15f, Color.Lerp(BoardDark, white, 0.15f + 0.85f * v), 0.12f));
                }
            }

            // A little network on the right: 5 inputs, 6 hidden, 1 answer, its wires drawn as lines.
            var net = new List<Shape>();
            Vector3 netAt = new Vector3(2.15f, -0.05f, 0f);
            Vector3 In(int i) => netAt + new Vector3(-0.55f, (i - 2f) * 0.22f, 0f);
            Vector3 Mid(int j) => netAt + new Vector3(0f, (j - 2.5f) * 0.2f, 0f);
            Vector3 Out() => netAt + new Vector3(0.55f, 0f, 0f);
            for (int i = 0; i < 5; i++) for (int j = 0; j < 6; j++) net.Add(Segment(In(i), Mid(j), 0.012f, new Color(blue.r, blue.g, blue.b, 0.55f)));
            for (int j = 0; j < 6; j++) net.Add(Segment(Mid(j), Out(), 0.012f, new Color(blue.r, blue.g, blue.b, 0.55f)));
            for (int i = 0; i < 5; i++) net.Add(new Shape { kind = Shape.Kind.Quad, position = In(i) + Vector3.back * 0.01f, size = Vector2.one * 0.1f, round = 1f, color = BoardDark, stroke = white, strokeWidth = 0.015f });
            for (int j = 0; j < 6; j++) net.Add(new Shape { kind = Shape.Kind.Quad, position = Mid(j) + Vector3.back * 0.01f, size = Vector2.one * 0.1f, round = 1f, color = BoardDark, stroke = white, strokeWidth = 0.015f });
            net.Add(new Shape { kind = Shape.Kind.Quad, position = Out() + Vector3.back * 0.01f, size = Vector2.one * 0.14f, round = 1f, color = yellow, stroke = white, strokeWidth = 0.015f });

            var sparks = new List<Shape>();
            for (int i = 0; i < 12; i++) sparks.Add(Segment(Vector3.zero, Vector3.right * 0.1f, 0.025f, yellow));

            var card = new List<Shape> { new Shape { kind = Shape.Kind.Quad, position = new Vector3(0.35f, -0.08f, 0.05f), size = new Vector2(5.6f, 2.0f), round = 0.08f, color = new Color(0.04f, 0.05f, 0.08f, 0.92f) } };
            Batch("Card", stage, card, "ExplainerFlat", 20);
            ShapeBatch bChips = Batch("Chips", stage, chips, "ExplainerFlat", 21);
            ShapeBatch bVectors = Batch("Vectors", stage, vectors, "ExplainerFlat", 22);
            ShapeBatch bNet = Batch("Net", stage, net, "ExplainerFlat", 21);
            ShapeBatch bSparks = Batch("Sparks", stage, sparks, "ExplainerFlat", 23);
            TextMeshPro sentence = Serif("Sentence", stage, new Vector3(-0.6f, chipY, -0.02f), "The cat sat on the", 0.3f, white, 4f, TextAlignmentOptions.Center, 24);
            var words = new List<Object>();
            for (int k = 0; k < tokens.Length; k++)
            {
                TextMeshPro t = Serif("Word" + k, stage, new Vector3(chipX[k], chipY, -0.03f), tokens[k], 0.22f, white, widths[k], TextAlignmentOptions.Center, 24);
                t.verticalAlignment = VerticalAlignmentOptions.Geometry;
                words.Add(t);
            }

            Ref(e, "content", content.gameObject);
            Ref(e, "sentence", sentence);
            Ref(e, "chips", bChips);
            Refs(e, "words", words);
            Ref(e, "vectors", bVectors);
            Ref(e, "net", bNet);
            Ref(e, "sparks", bSparks);
            Int(e, "perToken", perToken);
            Set(e, "netInput", p => p.vector3Value = netAt + new Vector3(-0.55f, 0f, 0f));
            content.gameObject.SetActive(false);
        }

        private static Layout _layout;

        private static Vector3 Layout_GridEye() => _layout.GridStop.position + Vector3.up * EyePoint.y;

        /// <summary>The 5×5 patch of the 28×28 hero with zeros, ones and in-between values, nearest the stroke's middle.</summary>
        private static void PickPatch(double[] p28, out int bestR, out int bestC)
        {
            bestR = 10;
            bestC = 10;
            float best = float.MinValue;
            for (int r = 2; r <= 21; r++)
            {
                for (int c = 2; c <= 21; c++)
                {
                    int zeros = 0, ones = 0;
                    var distinct = new HashSet<double>();
                    for (int y = 0; y < 5; y++)
                    {
                        for (int x = 0; x < 5; x++)
                        {
                            double v = System.Math.Round(p28[(r + y) * 28 + c + x], 1);
                            if (v <= 0.0) zeros++;
                            if (v >= 1.0) ones++;
                            distinct.Add(v);
                        }
                    }

                    if (zeros < 4 || ones < 4) continue;
                    float score = distinct.Count * 2f + Mathf.Min(zeros, 8) + Mathf.Min(ones, 8) - 0.05f * (Mathf.Abs(r - 11.5f) + Mathf.Abs(c - 11.5f));
                    if (score > best)
                    {
                        best = score;
                        bestR = r;
                        bestC = c;
                    }
                }
            }
        }
    }
}
