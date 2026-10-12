using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Region 6, the Valley: the ground is the bake's ribbon of real loss. Each row is a real point on the training
    /// path (the one big step, then the recorded steps), each column steps sideways along one fixed direction, and
    /// the height is the loss on all 60,000 training digits (5 m per unit of loss). The centre row is the track, so
    /// the coaster rides the real training run. The mesh is closed (skirts and a floor) so it never vanishes from
    /// any side. Contour lines mark every 0.25 of loss. A sunset in pink and violet, never orange.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private static void MakeValleyMaterials()
        {
            Shader shader = Shader.Find("Convergence/LossTerrain");
            if (shader == null) throw new System.IO.FileNotFoundException("Shader Convergence/LossTerrain is missing (Assets/Shaders/LossTerrain.shader).");
            string path = MatDir + "/LossTerrain.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }

            m.shader = shader;
            m.SetColor("_LowColor", Color.Lerp(_theme.valleyViolet, Color.black, 0.35f));
            m.SetColor("_HighColor", _theme.valleyPink);
            m.SetColor("_ContourColor", new Color(1f, 0.86f, 0.95f));
            m.SetColor("_RimColor", new Color(1f, 0.6f, 0.85f));
            EditorUtility.SetDirty(m);
            Mats["LossTerrain"] = m;
            LinkMaterial("ValleyTrail", Color.Lerp(_theme.valleyPink, Color.white, 0.5f), _theme.valleyPink, 0.18f, 1f, 2f, 8f);
        }

        private static GameObject BuildValleyRegion(Layout layout, RideDirector director, DashScreenView dash, ValleyRunMoment moment)
        {
            GameObject root = RegionRoot(6, layout.Rim);
            Transform ground = Make("Ground", root.transform, Vector3.zero).transform;
            int rows = layout.Rows, cols = layout.Columns, mid = cols / 2;

            Vector3 Local(int r, int c) => new Vector3((c - mid) * Layout.ColumnSpacing,
                ((float)_data.ValleyLoss(r, c) - layout.StartLoss) * Layout.MetersPerLoss - Layout.Clearance, layout.RowZ(r));

            // The surface, its skirts and a floor, as one closed mesh.
            var v = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();
            float lowest = float.MaxValue, highest = float.MinValue;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Vector3 p = Local(r, c);
                    lowest = Mathf.Min(lowest, p.y);
                    highest = Mathf.Max(highest, p.y);
                    v.Add(p);
                    colors.Add(Color.white);
                }
            }

            for (int r = 0; r < rows - 1; r++)
            {
                for (int c = 0; c < cols - 1; c++)
                {
                    int a = r * cols + c, b = a + 1, d = a + cols, e = d + 1;
                    tris.AddRange(new[] { a, d, b, b, d, e });
                }
            }

            float baseY = lowest - 3f;
            void Skirt(IList<int> edge)
            {
                int first = v.Count;
                foreach (int i in edge)
                {
                    v.Add(v[i]);
                    colors.Add(Color.black);
                    v.Add(new Vector3(v[i].x, baseY, v[i].z));
                    colors.Add(Color.black);
                }

                for (int i = 0; i < edge.Count - 1; i++)
                {
                    int a = first + i * 2;
                    tris.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 });
                }
            }

            var front = new List<int>();
            var back = new List<int>();
            var left = new List<int>();
            var right = new List<int>();
            for (int c = 0; c < cols; c++) { front.Add(c); back.Add((rows - 1) * cols + c); }
            for (int r = 0; r < rows; r++) { left.Add(r * cols); right.Add(r * cols + cols - 1); }
            Skirt(front);
            Skirt(back);
            Skirt(left);
            Skirt(right);
            int f0 = v.Count;
            float halfWidth = mid * Layout.ColumnSpacing, length = layout.RowZ(rows - 1);
            foreach (Vector3 corner in new[] { new Vector3(-halfWidth, baseY, 0f), new Vector3(halfWidth, baseY, 0f), new Vector3(halfWidth, baseY, length), new Vector3(-halfWidth, baseY, length) })
            {
                v.Add(corner);
                colors.Add(Color.black);
            }

            tris.AddRange(new[] { f0, f0 + 1, f0 + 2, f0, f0 + 2, f0 + 3 });
            var mesh = new Mesh { name = "L1_Valley" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AddMeshObject("Terrain", ground, SaveMesh(mesh, "L1_Valley"), "LossTerrain");

            Material terrain = Mat("LossTerrain");
            terrain.SetFloat("_HeightMin", lowest);
            terrain.SetFloat("_HeightMax", highest);
            terrain.SetFloat("_ContourSpacing", 0.25f * Layout.MetersPerLoss);
            terrain.SetFloat("_ContourOffset", -layout.StartLoss * Layout.MetersPerLoss - Layout.Clearance);
            terrain.SetFloat("_ContourWidth", 0.07f);
            EditorUtility.SetDirty(terrain);

            // The training path itself, a glowing line on the centre row.
            var trail = new Ribbons();
            var points = new Vector3[rows];
            var progress = new float[rows];
            for (int r = 0; r < rows; r++) points[r] = Local(r, mid) + Vector3.up * 0.06f;
            trail.Strip(points, 1f, 1f, progress, 0f);
            AddMeshObject("Trail", ground, SaveMesh(ToLinkMesh(trail), "L1_ValleyTrail"), "ValleyTrail");

            // A post at every hop: where the weights stood after each group of real steps.
            var posts = new TileMesh();
            Level1Hop[] hops = _data.File.valley.hops;
            foreach (Level1Hop hop in hops)
            {
                Vector3 at = Local(hop.row, mid);
                foreach (float side in new[] { -2.2f, 2.2f })
                {
                    posts.Box(at + new Vector3(side, -0.2f, 0f), new Vector3(0.18f, 1.6f, 0.18f), _theme.valleyPink);
                }
            }

            TileObject("HopPosts", ground, posts, "TilesBright");

            // The deck the cart arrives on, before the first row (not data: a dark platform).
            Prim(PrimitiveType.Cube, "Deck", root.transform, new Vector3(0f, -Layout.Clearance - 0.15f, -5f), new Vector3(4.4f, 0.3f, 10f), "Deck");

            // What the height means hangs over the track partway down, where the rider looks on the way down.
            Transform sign = Make("Sign", root.transform, new Vector3(0f, -2f, 60f)).transform;
            TextMeshPro text = Text("Text", sign, Vector3.zero, "HEIGHT = HOW WRONG\nLOWER = BETTER", 1.6f, Color.Lerp(_theme.valleyPink, Color.white, 0.6f), 24f, bold: true);
            text.rectTransform.sizeDelta = new Vector2(24f, 4f);
            LabelBacking(text, "HEIGHT = HOW WRONG", 0.3f);
            FaceToward(sign, layout.MidDrop + Vector3.up * 1.3f);

            // The sun, low over the far end of the valley: pink, never orange.
            Transform sun = Make("Sun", root.transform, new Vector3(-28f, 13f, 105f)).transform;
            Prim(PrimitiveType.Sphere, "Disc", sun, Vector3.zero, Vector3.one * 18f, "Sun");
            var halo = Quad("Halo", sun, Vector3.zero, new Vector2(70f, 70f), "GlowPink");
            halo.AddComponent<FaceCamera>();

            var view = root.AddComponent<ValleyView>();
            Ref(view, "director", director);
            Ref(view, "dash", dash);
            Ref(view, "moment", moment);
            Ref(view, "ground", ground);
            return root;
        }
    }
}
