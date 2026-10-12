using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Regions 0 to 2 of Level 1 and the track: the Page (the real handwritten 3 on paper), the Grid (its 784 pixels as
    /// pillars, then the 49 numbers the machine gets), and One Neuron (the big amber neuron and the "why" panel). All
    /// geometry is built here from the baked data, so the scene and every capture show the real digit.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private const string TexDir = "Assets/_Project/Materials/Level1";

        /// <summary>A combined mesh of boxes and quads with vertex colours (Convergence/HoloTiles), one draw call.</summary>
        private sealed class TileMesh
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Color> C = new List<Color>();
            public readonly List<int> T = new List<int>();

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color)
            {
                int i = V.Count;
                V.AddRange(new[] { a, b, c, d });
                for (int k = 0; k < 4; k++)
                {
                    N.Add(normal);
                    C.Add(color);
                }

                // Wind so the face points along normal.
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0f) T.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                else T.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }

            /// <summary>A flat tile facing -z (toward a viewer in front), centred at c.</summary>
            public void Face(Vector3 c, float w, float h, Color color)
            {
                Quad(c + new Vector3(-w / 2, -h / 2, 0), c + new Vector3(w / 2, -h / 2, 0), c + new Vector3(w / 2, h / 2, 0), c + new Vector3(-w / 2, h / 2, 0), Vector3.back, color);
            }

            /// <summary>An axis-aligned box standing on y = bottom (no floor face).</summary>
            public void Box(Vector3 bottomCentre, Vector3 size, Color color, bool withBottom = false)
            {
                Vector3 h = size * 0.5f;
                Vector3 o = bottomCentre + Vector3.up * h.y;
                Vector3 P(float x, float y, float z) => o + new Vector3(x * h.x, y * h.y, z * h.z);
                Quad(P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1), Vector3.up, color);
                Quad(P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), Vector3.back, color);
                Quad(P(1, -1, 1), P(-1, -1, 1), P(-1, 1, 1), P(1, 1, 1), Vector3.forward, color);
                Quad(P(-1, -1, 1), P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1), Vector3.left, color);
                Quad(P(1, -1, -1), P(1, -1, 1), P(1, 1, 1), P(1, 1, -1), Vector3.right, color);
                if (withBottom) Quad(P(-1, -1, 1), P(1, -1, 1), P(1, -1, -1), P(-1, -1, -1), Vector3.down, color);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                if (V.Count > 60000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(V);
                mesh.SetNormals(N);
                mesh.SetColors(C);
                mesh.SetTriangles(T, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        private static GameObject TileObject(string name, Transform parent, TileMesh tiles, string material)
        {
            return AddMeshObject(name, parent, SaveMesh(tiles.ToMesh(name), "L1_" + parent.name + "_" + name), material);
        }

        private static GameObject RegionRoot(int index, Pose pose)
        {
            var root = new GameObject("Region_" + index);
            root.transform.SetPositionAndRotation(pose.position, pose.rotation);
            return root;
        }

        /// <summary>Turns a sign to face a point (its readable side toward it).</summary>
        private static void FaceToward(Transform t, Vector3 viewer)
        {
            Vector3 away = t.position - viewer;
            if (away.sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
        }

        private static Color Ink(double value) => new Color((float)value, (float)value, (float)value, (float)value);

        // ---------- textures and materials of the page ----------

        private static void MakePageMaterials()
        {
            if (!AssetDatabase.IsValidFolder(TexDir)) Directory.CreateDirectory(TexDir);
            double[] hero = _data.File.hero.pixels28;
            Texture2D paper = PageTexture("PaperRuled", 512, (u, v) =>
            {
                // Faint ruled lines and a margin, so it reads as a sheet of paper from far away.
                Color c = _theme.paper;
                float line = Mathf.Repeat(v * 14f, 1f);
                if (line < 0.035f) c = Color.Lerp(c, new Color(0.62f, 0.75f, 0.92f), 0.6f);
                if (Mathf.Abs(u - 0.12f) < 0.004f) c = Color.Lerp(c, new Color(0.92f, 0.45f, 0.45f), 0.55f);
                return c;
            }, FilterMode.Bilinear);
            Texture2D smooth = PageTexture("InkSmooth", 280, (u, v) =>
            {
                float ink = SampleBilinear(hero, 28, u, v);
                Color c = _theme.ink;
                c.a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(ink * 1.25f));
                return c;
            }, FilterMode.Bilinear);
            Texture2D pixels = PageTexture("InkPixels", 28, (u, v) =>
            {
                int col = Mathf.Clamp((int)(u * 28f), 0, 27), row = Mathf.Clamp((int)((1f - v) * 28f), 0, 27);
                float ink = (float)hero[row * 28 + col];
                Color c = Color.Lerp(_theme.paper, Color.white, 0.2f);
                c.a = ink;
                return c;
            }, FilterMode.Point);
            M("PaperFront", Color.white, Kind.Alpha, paper);
            M("PaperBack", _theme.paper, Kind.Alpha);
            M("InkSmooth", Color.white, Kind.Alpha, smooth);
            M("InkPixels", Color.white, Kind.Alpha, pixels);
            M("Desk", Color.Lerp(_theme.paper, new Color(0.55f, 0.43f, 0.30f), 0.55f));
        }

        private static float SampleBilinear(double[] img, int n, float u, float v)
        {
            float x = Mathf.Clamp(u * n - 0.5f, 0f, n - 1f);
            float y = Mathf.Clamp((1f - v) * n - 0.5f, 0f, n - 1f);
            int x0 = Mathf.Min((int)x, n - 2), y0 = Mathf.Min((int)y, n - 2);
            float tx = x - x0, ty = y - y0;
            float a = Mathf.Lerp((float)img[y0 * n + x0], (float)img[y0 * n + x0 + 1], tx);
            float b = Mathf.Lerp((float)img[(y0 + 1) * n + x0], (float)img[(y0 + 1) * n + x0 + 1], tx);
            return Mathf.Lerp(a, b, ty);
        }

        private static Texture2D PageTexture(string name, int size, System.Func<float, float, Color> pixel, FilterMode filter)
        {
            string path = TexDir + "/" + name + ".png";
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++) tex.SetPixel(x, y, pixel((x + 0.5f) / size, (y + 0.5f) / size));
            }

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = filter;
            imp.mipmapEnabled = filter != FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.sRGBTexture = true;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------- track ----------

        /// <summary>Two thin slate rails under every leg: they give the ride its sense of speed and nothing else glows.</summary>
        private static void BuildTrackRails(Layout layout)
        {
            var track = new GameObject("Track");
            var rails = new Ribbons();
            foreach (RidePath path in new[] { layout.PathA, layout.PathB, layout.PathC })
            {
                foreach (float side in new[] { -1.6f, 1.6f })
                {
                    int count = Mathf.CeilToInt(path.Length / 1.5f) + 1;
                    var points = new Vector3[count];
                    var progress = new float[count];
                    for (int i = 0; i < count; i++)
                    {
                        float s = Mathf.Min(i * 1.5f, path.Length);
                        Vector3 h = path.Heading(s);
                        h.y = 0f;
                        Vector3 right = Vector3.Cross(Vector3.up, h.normalized).normalized;
                        points[i] = path.Position(s) + right * side + Vector3.down * 0.15f;
                    }

                    rails.Strip(points, 0.9f, 1f, progress, 0f);
                }
            }

            Mesh mesh = SaveMesh(ToLinkMesh(rails), "L1_Rails");
            AddMeshObject("Rails", track.transform, mesh, "Rails");
        }

        // ---------- region 0: the page ----------

        private static GameObject BuildPage(Layout layout, RideDirector director)
        {
            GameObject root = RegionRoot(0, layout.PageAnchor);
            Transform page = Make("Page", root.transform, Vector3.zero).transform;
            const float size = 7.5f;
            var front = Quad("Paper", page, new Vector3(0f, 2.4f, 0f), new Vector2(size, size), "PaperFront");
            var back = Quad("PaperBack", page, new Vector3(0f, 2.4f, 0.01f), new Vector2(size, size), "PaperBack");
            back.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var ink = Quad("Ink", page, new Vector3(0f, 2.4f, -0.03f), new Vector2(size, size), "InkSmooth");

            // A desk under the page, so the sheet stands on something in the warm light instead of floating.
            Quad("Desk", root.transform, new Vector3(0f, -1.45f, -12f), new Vector2(40f, 24f), "Desk").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var view = root.AddComponent<PageView>();
            Ref(view, "director", director);
            Refs(view, "paper", new Object[] { front.GetComponent<Renderer>(), back.GetComponent<Renderer>() });
            Ref(view, "ink", ink.GetComponent<Renderer>());
            Ref(view, "inkSmooth", Mat("InkSmooth"));
            Ref(view, "inkPixels", Mat("InkPixels"));
            return root;
        }

        // ---------- region 1: the grid ----------

        /// <summary>
        /// A relief wall of n × n pixels facing the viewer: each pixel a block standing out of a dark backing by its ink
        /// (0..1), so the digit reads face on and the heights read from the side as the cart passes. Row 0 is the top.
        /// </summary>
        private static Transform ReliefWall(string name, Transform parent, Vector3 centre, Vector3 viewer, double[] pixels, int n, float pitch,
            float maxDepth, float label = 0f)
        {
            Transform wall = Make(name, parent, centre).transform;
            FaceToward(wall, viewer);
            var blocks = new TileMesh();
            float face = pitch * 0.86f;
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    float ink = (float)pixels[r * n + c];
                    float depth = 0.06f + maxDepth * ink;
                    Color color = Color.Lerp(new Color(0.16f, 0.18f, 0.24f, 0f), _theme.pillar, ink);
                    color.a = ink;
                    Vector3 o = new Vector3((c - (n - 1) / 2f) * pitch, ((n - 1) / 2f - r) * pitch, -depth / 2f);
                    Vector3 h = new Vector3(face / 2f, face / 2f, depth / 2f);
                    Vector3 P(float x, float y, float z) => o + new Vector3(x * h.x, y * h.y, z * h.z);
                    blocks.Quad(P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), Vector3.back, color);
                    blocks.Quad(P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1), Vector3.up, color);
                    blocks.Quad(P(-1, -1, 1), P(1, -1, 1), P(1, -1, -1), P(-1, -1, -1), Vector3.down, color);
                    blocks.Quad(P(-1, -1, 1), P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1), Vector3.left, color);
                    blocks.Quad(P(1, -1, -1), P(1, -1, 1), P(1, 1, 1), P(1, 1, -1), Vector3.right, color);
                }
            }

            TileObject("Blocks", wall, blocks, "TilesBright");
            Board("Back", wall, Vector3.zero, new Vector2(n * pitch + 0.4f, n * pitch + 0.4f), "Slate", 0.2f);
            return wall;
        }

        private static GameObject BuildGrid(Layout layout)
        {
            GameObject root = RegionRoot(1, new Pose(Vector3.zero, Quaternion.identity));
            double[] hero = _data.File.hero.pixels28;
            double[] pooled = _data.File.hero.pixels7;

            // Inside the picture: the 3's 784 pixels as a relief wall on the left, facing the cart as it comes through the
            // page. Blank paper is a flat block (0), full ink stands 1.8 m out (1). Seen face on it is the 3; from the side,
            // as the cart passes, the heights.
            Vector3 approach = layout.GridApproach.position + Vector3.up * 1.3f;
            const float pitch = 0.45f, depth = 1.8f;
            Transform pixelField = ReliefWall("PixelField", root.transform, new Vector3(-9.5f, 4.2f, 34f), new Vector3(0f, 1.3f, 8f), hero, 28, pitch, depth);

            // Three pixels carry their number, so "every pixel became one number" has a picture.
            int brightest = 0, middle = 0;
            for (int i = 0; i < hero.Length; i++)
            {
                if (hero[i] > hero[brightest]) brightest = i;
                if (Mathf.Abs((float)hero[i] - 0.5f) < Mathf.Abs((float)hero[middle] - 0.5f)) middle = i;
            }

            int blank = 13 * 28 + 3; // blank paper, left of the digit's middle
            Transform values = Make("Values", pixelField, Vector3.zero).transform;
            foreach (int i in new[] { brightest, middle, blank })
            {
                int r = i / 28, c = i % 28;
                float stand = 0.06f + depth * (float)hero[i];
                TextMeshPro label = Text("Value_" + r + "_" + c, values, new Vector3((c - 13.5f) * pitch, (13.5f - r) * pitch, -depth - 0.5f),
                    hero[i].ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), 0.8f, _theme.cyan, 2.2f, bold: true);
                LabelBacking(label, "0.0", 0.2f);
            }

            // The 49 numbers the machine gets: 4×4 blocks averaged, as a coarse relief ahead on the right.
            Vector3 stop = layout.GridStop.position + Vector3.up * 1.3f;
            Transform inputGrid = ReliefWall("InputGrid7", root.transform, new Vector3(7.8f, 3.6f, 56f), stop, pooled, 7, 1.4f, 2.2f);
            TextMeshPro title = Text("Title", inputGrid, new Vector3(0f, 5.5f, -0.4f), "49 NUMBERS", 1.0f, _theme.offWhite, 10f, bold: true);
            LabelBacking(title, "49 NUMBERS", 0.25f);
            return root;
        }

        // ---------- region 2: one neuron ----------

        private static GameObject BuildNeuronRegion(Layout layout, WeightPaintMoment moment)
        {
            GameObject root = RegionRoot(2, layout.NeuronStop);
            WeightPaintPuzzleData puzzle = PuzzleData();

            // The neuron: a big amber cell high and to the left, with 49 dendrites from a ghost 7×7 grid of inputs.
            Transform neuron = Make("Neuron", root.transform, new Vector3(-4.5f, 8f, 17f)).transform;
            Prim(PrimitiveType.Sphere, "Body", neuron, Vector3.zero, Vector3.one * 4.4f, "NeuronAmber");
            var glow = Quad("Glow", neuron, Vector3.zero, new Vector2(11f, 11f), "GlowAmber");
            glow.AddComponent<FaceCamera>();
            var dendrites = new Ribbons();
            for (int r = 0; r < 7; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    Vector3 from = new Vector3((c - 3f) * 0.75f, (3f - r) * 0.75f, -7f);
                    Vector3 bulge = new Vector3((c - 3f) * 0.15f, (3f - r) * 0.15f, 0f);
                    dendrites.Link(from, Vector3.zero, bulge, 0.5f, 1f, 0f, 0f, (r * 7 + c) / 49f);
                }
            }

            AddMeshObject("Dendrites", neuron, SaveMesh(ToLinkMesh(dendrites), "L1_Dendrites"), "Dendrites");

            // The "why" panel, 2.6 m ahead at eye height: the digit on show, ink × weight, the total against the line,
            // the verdict, and all eight digits with a check or a cross.
            Transform panel = Make("Panel", root.transform, new Vector3(0f, 1.35f, 2.6f)).transform;
            Board("Plate", panel, Vector3.zero, new Vector2(2.36f, 1.1f), "PlateWarm");
            Text("Title", panel, new Vector3(0f, 0.44f, -0.01f), "FIRE FOR 1s  -  STAY QUIET FOR 0s", 0.1f, _theme.offWhite, 2.3f, bold: true);

            const float tile = 0.07f;
            var pixels = new TileMesh();
            var shares = new TileMesh();
            for (int r = 0; r < 7; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    pixels.Face(new Vector3(-0.72f + (c - 3) * tile, 0.08f + (3 - r) * tile, -0.01f), tile * 0.9f, tile * 0.9f, Ink(puzzle.Digits[0][r * 7 + c]));
                    shares.Face(new Vector3(0.0f + (c - 3) * tile, 0.08f + (3 - r) * tile, -0.01f), tile * 0.9f, tile * 0.9f, new Color(0.06f, 0.07f, 0.10f, 0f));
                }
            }

            GameObject pixelTiles = TileObject("DigitTiles", panel, pixels, "TilesBright");
            GameObject shareTiles = TileObject("ShareTiles", panel, shares, "TilesBright");
            Text("PixelsCaption", panel, new Vector3(-0.72f, -0.21f, -0.01f), "PIXELS", 0.1f, _theme.offWhite, 0.8f);
            Text("SharesCaption", panel, new Vector3(0.0f, -0.21f, -0.01f), "INK x WEIGHT", 0.1f, _theme.offWhite, 0.9f);

            // The total: a bar from the zero mark, and the line it must reach to fire.
            Transform barRoot = Make("Total", panel, new Vector3(0.6f, -0.08f, -0.02f)).transform;
            var bar = Prim(PrimitiveType.Cube, "Bar", barRoot, new Vector3(0f, 0.08f, 0f), new Vector3(0.1f, 0.16f, 0.02f), "SumQuiet");
            Prim(PrimitiveType.Cube, "Zero", barRoot, Vector3.zero, new Vector3(0.16f, 0.006f, 0.022f), "Locked");
            Prim(PrimitiveType.Cube, "Line", barRoot, new Vector3(0f, (float)puzzle.Threshold * 0.08f, -0.005f), new Vector3(0.2f, 0.012f, 0.024f), "LineMark");
            TextMeshPro sum = Text("SumText", panel, new Vector3(1.0f, 0.06f, -0.01f), "TOTAL 0.0\nLINE 2.0", 0.108f, _theme.offWhite, 0.5f, TextAlignmentOptions.Left);
            sum.rectTransform.sizeDelta = new Vector2(0.5f, 0.3f);
            TextMeshPro verdict = Text("Verdict", panel, new Vector3(0f, -0.32f, -0.01f), "A 1: QUIET - WRONG", 0.11f, _theme.offWhite, 2.2f, bold: true);

            // The eight digits.
            var thumbs = new TileMesh();
            var thumbTransforms = new List<Object>();
            var checks = new List<Object>();
            var crosses = new List<Object>();
            const float thumbTile = 0.017f;
            for (int d = 0; d < puzzle.Digits.Length; d++)
            {
                float x = -0.98f + d * 0.21f;
                Transform anchor = Make("Thumb" + d, panel, new Vector3(x, -0.48f, -0.012f)).transform;
                thumbTransforms.Add(anchor);
                for (int r = 0; r < 7; r++)
                {
                    for (int c = 0; c < 7; c++)
                    {
                        thumbs.Face(new Vector3(x + (c - 3) * thumbTile, -0.48f + (3 - r) * thumbTile, -0.012f), thumbTile, thumbTile, Ink(puzzle.Digits[d][r * 7 + c]));
                    }
                }

                checks.Add(Quad("Check" + d, panel, new Vector3(x + 0.085f, -0.43f, -0.015f), new Vector2(0.06f, 0.06f), "Icon_Check").GetComponent<Renderer>());
                crosses.Add(Quad("Cross" + d, panel, new Vector3(x + 0.085f, -0.43f, -0.015f), new Vector2(0.06f, 0.06f), "Icon_Cross").GetComponent<Renderer>());
            }

            TileObject("Thumbs", panel, thumbs, "TilesBright");
            var focus = Quad("Focus", panel, new Vector3(-0.98f, -0.48f, -0.011f), new Vector2(0.15f, 0.15f), "GlowWhite");
            TextMeshPro score = Text("Score", panel, new Vector3(0.93f, -0.48f, -0.01f), "4 / 8 RIGHT", 0.11f, _theme.offWhite, 0.6f, TextAlignmentOptions.Left);

            var view = root.AddComponent<NeuronPanelView>();
            Ref(view, "moment", moment);
            Ref(view, "theme", _theme);
            Ref(view, "pixels", pixelTiles.GetComponent<MeshFilter>());
            Ref(view, "contributions", shareTiles.GetComponent<MeshFilter>());
            Ref(view, "sumBar", bar.transform);
            Ref(view, "sumBarRenderer", bar.GetComponent<Renderer>());
            Ref(view, "sumFires", Mat("SumFires"));
            Ref(view, "sumQuiet", Mat("SumQuiet"));
            Float(view, "barMetersPerUnit", 0.08f);
            Ref(view, "sumText", sum);
            Ref(view, "verdictText", verdict);
            Ref(view, "scoreText", score);
            Refs(view, "checks", checks);
            Refs(view, "crosses", crosses);
            Refs(view, "thumbnails", thumbTransforms);
            Ref(view, "focusFrame", focus.transform);
            Ref(view, "neuronGlow", glow.transform);
            return root;
        }

        private sealed class WeightPaintPuzzleData
        {
            public double[][] Digits;
            public double Threshold;
        }

        private static WeightPaintPuzzleData PuzzleData()
        {
            Level1Puzzle p = _data.File.puzzle;
            var digits = new double[p.digits.Length][];
            for (int d = 0; d < digits.Length; d++) digits[d] = p.digits[d].pixels7;
            return new WeightPaintPuzzleData { Digits = digits, Threshold = p.threshold };
        }
    }
}
