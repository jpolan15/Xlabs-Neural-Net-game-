using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Gameplay.Ride;
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
            Add<NeuronExplainer>(new[] { RideLine.NeuronHolds, RideLine.NeuronWires, RideLine.NeuronGreenRed, RideLine.NeuronAdds, RideLine.NeuronYourTurn },
                SegNeuronExplain, SegNeuronPlay, true, null, new[] { Child(2, "Neuron"), Child(2, "Panel") },
                new List<UnfoldSpec> { new UnfoldSpec(null, T(2, "Neuron"), grow, 0f, 1.4f), new UnfoldSpec(null, T(2, "Panel"), grow, 0.3f, 1.2f) });
            Add<NetworkExplainer>(new[] { RideLine.NetworkTeam, RideLine.NetworkAnswers, RideLine.NetworkWire, RideLine.NetworkSend },
                SegNetworkExplain, SegNetworkSend, true, null, new[] { Child(3, "Network") },
                new List<UnfoldSpec> { new UnfoldSpec(null, T(3, "Network"), grow, 0f, 2f) });
            Add<LossExplainer>(new[] { RideLine.WrongSays, RideLine.WrongRandom, RideLine.WrongWanted, RideLine.WrongLoss }, SegWrong, SegWrong, false, null, null, null);
            Add<BackpropExplainer>(new[] { RideLine.BackWish, RideLine.BackFlows, RideLine.BackHot, RideLine.BackGrab }, SegBackExplain, SegRewind, false,
                null, new[] { Child(5, "Blame") }, new List<UnfoldSpec> { new UnfoldSpec(null, T(5, "Blame"), Explainer.UnfoldStyle.Show, 0f, 0.1f) });
            Add<DescentExplainer>(new[] { RideLine.DescentHill, RideLine.DescentStep, RideLine.DescentTooBig, RideLine.DescentRealHill }, SegRim, SegValley, true,
                null, new[] { Child(6, "Ground"), Child(6, "Deck"), Child(6, "Sign"), Child(6, "Sun") },
                new List<UnfoldSpec>
                {
                    new UnfoldSpec(null, T(6, "Ground"), Explainer.UnfoldStyle.GrowUp, 0f, 1.8f),
                    new UnfoldSpec(null, T(6, "Deck"), grow, 0.2f, 1f),
                    new UnfoldSpec(null, T(6, "Sign"), grow, 0.5f, 1f),
                    new UnfoldSpec(null, T(6, "Sun"), grow, 0.8f, 1.5f),
                });
            Add<TrainingExplainer>(new[] { RideLine.RightSays, RideLine.RightTraining, RideLine.RightAccuracy }, SegRight, SegRight, false, null, null, null);
            Add<TokensExplainer>(new[] { RideLine.TokensWords, RideLine.TokensNumbers, RideLine.TokensPredict, RideLine.TokensNext }, SegTokens, SegTokens, false, null, null, null);
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
