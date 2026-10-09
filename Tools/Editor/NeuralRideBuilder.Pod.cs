using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Pod hull and shared layout helpers for the Neural Ride builder (WP2). The old overhead rim and four posts crossed
    /// the player's view, so the hull is now a low cockpit tub: sides up to 0.95 m behind the player, falling to
    /// 0.48 m at the front, nothing above 1.1 m anywhere in the forward 70 degrees. It is two meshes (wall and trim).
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        /// <summary>The virtual eye every panel is laid out for. PodSeat lifts the rig so the real eye sits here.</summary>
        private static readonly Vector3 EyePoint = new Vector3(0f, 1.30f, 0f);

        /// <summary>A point at an azimuth (degrees, right of forward is positive), elevation (up is positive) and distance from the eye.</summary>
        private static Vector3 Polar(float azimuthDeg, float elevationDeg, float distance)
        {
            return EyePoint + Quaternion.Euler(-elevationDeg, azimuthDeg, 0f) * Vector3.forward * distance;
        }

        /// <summary>The rotation that turns a panel's back (+Z) away from the eye, so its face looks at the viewer.</summary>
        private static Quaternion Face(float azimuthDeg, float elevationDeg)
        {
            return Quaternion.Euler(-elevationDeg, azimuthDeg, 0f);
        }

        private sealed class MeshData
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<int> Triangles = new List<int>();

            /// <summary>Adds a flat quad a-b-c-d whose face points toward outward, with hard edges.</summary>
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
            {
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(n, outward) < 0f)
                {
                    Vector3 swap = b;
                    b = d;
                    d = swap;
                    n = -n;
                }

                int i = Vertices.Count;
                Vertices.AddRange(new[] { a, b, c, d });
                Normals.AddRange(new[] { n, n, n, n });
                Triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(Vertices);
                mesh.SetNormals(Normals);
                mesh.SetTriangles(Triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        private static Mesh SaveMesh(Mesh mesh, string file)
        {
            string dir = PrefabDir + "/Meshes";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(PrefabDir, "Meshes");
            string path = dir + "/" + file + ".asset";
            // Recreate the asset instead of copying into the old one: updating a loaded mesh in place leaves the Editor
            // drawing its previous GPU data (the finale orb and the haze clouds silently never appeared), so each build
            // makes a fresh mesh object. Everything that references it is rebuilt in the same run.
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void BuildTub(Transform pod)
        {
            const int segments = 48;
            const float startDeg = 52f, endDeg = 308f;   // open at the front, where the dash and the view are
            const float rIn = 1.28f, rOut = 1.35f;
            const float rearHeight = 0.95f, frontHeight = 0.48f;

            var wall = new MeshData();
            var trim = new MeshData();

            Vector3 At(float r, float deg, float y) => new Vector3(Mathf.Sin(deg * Mathf.Deg2Rad) * r, y, Mathf.Cos(deg * Mathf.Deg2Rad) * r);

            float HeightAt(float deg)
            {
                float fromRear = Mathf.Abs(deg - 180f);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(70f, 128f, fromRear));
                return Mathf.Lerp(rearHeight, frontHeight, k);
            }

            for (int i = 0; i < segments; i++)
            {
                float d0 = Mathf.Lerp(startDeg, endDeg, i / (float)segments);
                float d1 = Mathf.Lerp(startDeg, endDeg, (i + 1) / (float)segments);
                float h0 = HeightAt(d0), h1 = HeightAt(d1);
                Vector3 radial0 = At(1f, d0, 0f), radial1 = At(1f, d1, 0f);

                wall.Quad(At(rIn, d0, 0f), At(rIn, d1, 0f), At(rIn, d1, h1), At(rIn, d0, h0), -(radial0 + radial1));
                wall.Quad(At(rOut, d0, 0f), At(rOut, d1, 0f), At(rOut, d1, h1), At(rOut, d0, h0), radial0 + radial1);
                wall.Quad(At(rIn, d0, h0), At(rIn, d1, h1), At(rOut, d1, h1), At(rOut, d0, h0), Vector3.up);

                // A thin glowing strip down the middle of the top edge.
                trim.Quad(At(rIn + 0.02f, d0, h0 + 0.004f), At(rIn + 0.02f, d1, h1 + 0.004f), At(rOut - 0.02f, d1, h1 + 0.004f), At(rOut - 0.02f, d0, h0 + 0.004f), Vector3.up);
            }

            foreach (float deg in new[] { startDeg, endDeg })
            {
                float h = HeightAt(deg);
                Vector3 tangent = At(1f, deg + 1f, 0f) - At(1f, deg, 0f);
                wall.Quad(At(rIn, deg, 0f), At(rOut, deg, 0f), At(rOut, deg, h), At(rIn, deg, h), deg < 180f ? -tangent : tangent);
            }

            Mesh wallMesh = SaveMesh(wall.ToMesh("PodTub"), "PodTub");
            Mesh trimMesh = SaveMesh(trim.ToMesh("PodTubTrim"), "PodTubTrim");
            AddMeshObject("Tub", pod, wallMesh, "PodMid");
            AddMeshObject("TubTrim", pod, trimMesh, "CyanDim"); // dim, like the dash trims: the network is the brightest line in view

            // A headrest behind the player's head, outside every forward sight line.
            Prim(PrimitiveType.Cube, "Headrest", pod, new Vector3(0, 1.32f, -0.74f), new Vector3(0.36f, 0.26f, 0.10f), "PodMid");
            Prim(PrimitiveType.Cube, "HeadrestTrim", pod, new Vector3(0, 1.455f, -0.69f), new Vector3(0.36f, 0.012f, 0.012f), "Cyan");
        }

        private static GameObject AddMeshObject(string name, Transform parent, Mesh mesh, string material)
        {
            var go = Make(name, parent, Vector3.zero);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Mat(material);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        /// <summary>A flat stand-in for the loss landscape, built at runtime, so the sightline check can measure it.</summary>
        private static Mesh PlaceholderLandscape(float size)
        {
            var data = new MeshData();
            float h = size * 0.5f;
            data.Quad(new Vector3(-h, 0, -h), new Vector3(-h, 0, h), new Vector3(h, 0, h), new Vector3(h, 0, -h), Vector3.up);
            return SaveMesh(data.ToMesh("LandscapePlaceholder"), "LandscapePlaceholder");
        }
    }
}
