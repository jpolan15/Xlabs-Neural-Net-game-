using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// The big neural network the pod flies through (WP5). Seven layers of glowing neurons ring the track, with the
    /// three stops docked at the neurons of layers 2, 4 and 6. Every connection is one ribbon in a single mesh and
    /// every neuron and speck of dust is one billboard in a second mesh. The ride's own scaffolding (the two rails
    /// and the gates) is a third, dim slate mesh that nothing drives, so only the network glows: three draw calls.
    /// The activations are a real forward pass (tanh of the weighted sum, fixed seed) baked into the meshes: the
    /// width and brightness of a connection is the size of weight times input, and a neuron's size follows its
    /// activation. Nothing sits closer than 4 m to the axis, so nothing passes near the player's head.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private const float DockForward = 6.4f;   // the core's distance ahead of the pod at a stop (see BuildStation)
        private const float DockHeight = 2.4f;    // and its height

        private sealed class Ribbons
        {
            public readonly List<Vector3> Position = new List<Vector3>();
            public readonly List<Vector3> Direction = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>();
            public readonly List<Color> Data = new List<Color>();
            public readonly List<int> Triangles = new List<int>();

            /// <summary>One ribbon along a polyline. data.b is overridden per point from progress.</summary>
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

            /// <summary>A curved connection: a quadratic curve that bows outward by bulge.</summary>
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

        private static GameObject BuildTrack()
        {
            var track = new GameObject("Track");
            var path = new RidePath(PathPoints);
            float length = path.Length;
            var rng = new System.Random(2026);
            float Rand(float lo, float hi) => lo + (float)rng.NextDouble() * (hi - lo);

            // Seven layers; the stops dock at layers 1, 3 and 5 (counting from 0).
            float d1 = path.DistanceAtPoint(StopPoints[0]), d2 = path.DistanceAtPoint(StopPoints[1]), d3 = path.DistanceAtPoint(StopPoints[2]);
            float[] layerAt = { Mathf.Max(8f, d1 - (d2 - d1) * 0.5f), d1, (d1 + d2) * 0.5f, d2, (d2 + d3) * 0.5f, d3, Mathf.Min(length - 8f, d3 + (d3 - d2) * 0.5f) };
            int[] sizes = { 5, 8, 9, 10, 9, 8, 5 };
            int[] dockLayers = { 1, 3, 5 };

            var layers = new List<Vector3[]>();
            var activation = new List<float[]>();
            for (int l = 0; l < layerAt.Length; l++)
            {
                Vector3 center = path.Position(layerAt[l]);
                Vector3 heading = path.Heading(layerAt[l]).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, heading).normalized;
                Vector3 up = Vector3.Cross(heading, right).normalized;
                var ring = new Vector3[sizes[l]];
                for (int i = 0; i < sizes[l]; i++)
                {
                    float angle = (i + Rand(-0.2f, 0.2f)) / sizes[l] * Mathf.PI * 2f + l * 0.37f;
                    float radius = Rand(5.6f, 7.8f);
                    ring[i] = center + heading * Rand(-1.5f, 1.5f) + right * Mathf.Cos(angle) * radius * 1.3f + up * Mathf.Sin(angle) * radius * 0.85f;
                }

                layers.Add(ring);
                var a = new float[sizes[l]];
                for (int i = 0; i < a.Length; i++) a[i] = l == 0 ? Rand(0.3f, 1f) : 0f;
                activation.Add(a);
            }

            var links = new Ribbons();
            var structure = new Ribbons();   // the rails and the gates: scaffolding, kept apart from the real connections
            var nodes = new Billboards();

            // Connections with a real forward pass. Each neuron feeds its three nearest around the ring in the next layer.
            for (int l = 0; l < layers.Count - 1; l++)
            {
                Vector3[] from = layers[l], to = layers[l + 1];
                var pre = new float[to.Length];
                var contributions = new List<(int i, int j, float value)>();
                for (int i = 0; i < from.Length; i++)
                {
                    int target = Mathf.RoundToInt(i * (float)to.Length / from.Length);
                    for (int k = -1; k <= 2; k++)
                    {
                        int j = ((target + k) % to.Length + to.Length) % to.Length;
                        float weight = Rand(-0.6f, 1f);
                        float contribution = weight * activation[l][i];
                        pre[j] += contribution;
                        contributions.Add((i, j, contribution));
                    }
                }

                float biggest = 0.0001f;
                foreach (var c in contributions) biggest = Mathf.Max(biggest, Mathf.Abs(c.value));
                float progressFrom = layerAt[l] / length, progressTo = layerAt[l + 1] / length;
                Vector3 axisMid = path.Position((layerAt[l] + layerAt[l + 1]) * 0.5f);
                foreach (var c in contributions)
                {
                    Vector3 mid = (from[c.i] + to[c.j]) * 0.5f;
                    Vector3 outward = (mid - axisMid);
                    outward.y *= 0.8f;
                    Vector3 bulge = outward.normalized * Vector3.Distance(from[c.i], to[c.j]) * 0.10f + new Vector3(Rand(-0.6f, 0.6f), Rand(-0.6f, 0.6f), Rand(-0.6f, 0.6f));
                    float size = 0.25f + 0.75f * Mathf.Abs(c.value) / biggest;
                    links.Link(from[c.i], to[c.j], bulge, size, c.value >= 0f ? 1f : 0f, progressFrom, progressTo, Rand(0f, 1f));
                }

                for (int j = 0; j < to.Length; j++) activation[l + 1][j] = (float)System.Math.Tanh(pre[j]);
            }

            // Neurons: size follows activation, hue runs from cyan at the input to violet at the output.
            for (int l = 0; l < layers.Count; l++)
            {
                for (int i = 0; i < layers[l].Length; i++)
                {
                    float radius = 0.9f + 0.8f * Mathf.Abs(activation[l][i]) + Rand(0f, 0.2f);
                    nodes.Add(layers[l][i], radius, layerAt[l] / length, l / (float)(layers.Count - 1) + Rand(-0.08f, 0.08f), 0f, Rand(0f, 1f));
                    // Lateral links round the ring make each layer read as a gate.
                    Vector3 next = layers[l][(i + 1) % layers[l].Length];
                    links.Link(layers[l][i], next, ((layers[l][i] + next) * 0.5f - path.Position(layerAt[l])).normalized * 1.0f, 0.18f, 1f, layerAt[l] / length, layerAt[l] / length, Rand(0f, 1f));
                }
            }

            // The neuron each stop docks at: a big bright one exactly where that station's core hangs. It feeds the next layer.
            for (int s = 0; s < dockLayers.Length; s++)
            {
                float at = layerAt[dockLayers[s]];
                Vector3 flatHeading = path.Heading(at);
                flatHeading.y = 0f;
                flatHeading.Normalize();
                Vector3 dock = path.Position(at) + flatHeading * DockForward + Vector3.up * DockHeight;
                nodes.Add(dock, 3.0f, at / length, 0.1f, 1f, 0f);

                Vector3[] next = layers[dockLayers[s] + 1];
                float progress = at / length, progressNext = layerAt[dockLayers[s] + 1] / length;
                for (int j = 0; j < next.Length; j += 2)
                {
                    Vector3 mid = (dock + next[j]) * 0.5f;
                    Vector3 bulge = (mid - path.Position(at)).normalized * 1.2f;
                    links.Link(dock, next[j], bulge, 0.85f, 1f, progress, progressNext, Rand(0f, 1f));
                }
            }

            // Two rails and faint gates every 14 m give the ride its sense of speed. They are scaffolding, not network: their own
            // dim slate mesh, always lit and never driven at runtime, so the connections are the only bright thing.
            foreach (float side in new[] { -1.9f, 1.9f })
            {
                int count = Mathf.CeilToInt(length / 2f) + 1;
                var rail = new Vector3[count];
                var railProgress = new float[count];
                for (int i = 0; i < count; i++)
                {
                    float s = Mathf.Min(i * 2f, length);
                    Vector3 h = path.Heading(s);
                    Vector3 right = Vector3.Cross(Vector3.up, h).normalized;
                    rail[i] = path.Position(s) + right * side + Vector3.down * 0.12f;
                    railProgress[i] = 0f;
                }

                structure.Strip(rail, 0.9f, 1f, railProgress, 0f);
            }

            for (float s = 14f; s < length; s += 14f)
            {
                Vector3 center = path.Position(s);
                Vector3 heading = path.Heading(s).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, heading).normalized;
                Vector3 up = Vector3.Cross(heading, right).normalized;
                const int n = 40;
                var gate = new Vector3[n + 1];
                var gateProgress = new float[n + 1];
                for (int i = 0; i <= n; i++)
                {
                    float angle = i / (float)n * Mathf.PI * 2f;
                    gate[i] = center + right * Mathf.Cos(angle) * 11f + up * Mathf.Sin(angle) * 8.5f;
                    gateProgress[i] = 0f;
                }

                structure.Strip(gate, 0.12f, 1f, gateProgress, Rand(0f, 1f));
            }

            // Dust: far from the axis, so it adds depth and never crowds the cockpit.
            for (int i = 0; i < 600; i++)
            {
                float s = Rand(0f, length);
                Vector3 center = path.Position(s);
                Vector3 heading = path.Heading(s).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, heading).normalized;
                Vector3 up = Vector3.Cross(heading, right).normalized;
                float angle = Rand(0f, Mathf.PI * 2f);
                float radius = Rand(5.5f, 26f);
                nodes.Add(center + right * Mathf.Cos(angle) * radius + up * Mathf.Sin(angle) * radius * 0.8f, Rand(0.05f, 0.16f), 0f, Rand(0f, 1f), 0.5f, Rand(0f, 1f));
            }

            // The destination: AURA's core, a huge neuron beyond the last stop. The last layer converges into it and three portal
            // rings frame it, so the ride ends on something to look at. It ignites with the rest of the network at the outro.
            Vector3 endHeading = (path.Position(length) - path.Position(length - 3f)).normalized;
            Vector3 aura = path.Position(length) + endHeading * 36f + Vector3.down * 1.5f;
            nodes.Add(aura, 15f, 1f, 0.5f, 1f, 0.3f);
            Vector3[] lastLayer = layers[layers.Count - 1];
            float lastProgress = layerAt[layers.Count - 1] / length;
            foreach (Vector3 from in lastLayer)
            {
                Vector3 bulge = ((from + aura) * 0.5f - path.Position(layerAt[layers.Count - 1])).normalized * 3f;
                links.Link(from, aura, bulge, 0.9f, 1f, lastProgress, 1f, Rand(0f, 1f));
            }

            Vector3 portalRight = Vector3.Cross(Vector3.up, endHeading).normalized;
            Vector3 portalUp = Vector3.Cross(endHeading, portalRight).normalized;
            float[] portalRadius = { 15f, 21f, 28f };
            float[] portalBack = { 4f, 9f, 15f };
            for (int r = 0; r < portalRadius.Length; r++)
            {
                const int n = 56;
                var ringPoints = new Vector3[n + 1];
                var ringProgress = new float[n + 1];
                Vector3 ringCenter = aura - endHeading * portalBack[r];
                for (int i = 0; i <= n; i++)
                {
                    float angle = i / (float)n * Mathf.PI * 2f;
                    ringPoints[i] = ringCenter + portalRight * Mathf.Cos(angle) * portalRadius[r] + portalUp * Mathf.Sin(angle) * portalRadius[r];
                    ringProgress[i] = 1f;
                }

                links.Strip(ringPoints, 0.7f, 1f, ringProgress, Rand(0f, 1f));
            }

            // Haze: a few big, very faint colour clouds far to the sides and ahead, so the void has depth instead of flat black.
            // Eight billboards, drawn with the same one call as the neurons; kept faint so they never compete with the teaching panels.
            for (int i = 0; i < 8; i++)
            {
                float s = length * (0.05f + 0.9f * i / 7f);
                Vector3 center = path.Position(s);
                Vector3 heading = path.Heading(s).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, heading).normalized;
                float sideSign = i % 2 == 0 ? 1f : -1f;
                Vector3 cloud = center + right * sideSign * Rand(26f, 38f) + Vector3.up * Rand(-10f, 16f) + heading * Rand(10f, 30f);
                nodes.Add(cloud, Rand(16f, 24f), 0f, i / 7f, 0.15f, Rand(0f, 1f));
            }

            nodes.Add(path.Position(length) + endHeading * 34f + Vector3.up * 4f, 26f, 0f, 0.35f, 0.15f, 0.5f);

            Mesh linkMesh = SaveMesh(ToLinkMesh(links), "NetworkLinks");
            Mesh structureMesh = SaveMesh(ToLinkMesh(structure), "NetworkStructure");
            Mesh nodeMesh = SaveMesh(ToNodeMesh(nodes), "NetworkNodes");

            RideTheme t = _theme;
            Material linkMaterial = NetworkMaterial("NetLinks", "Convergence/NeuralLinks");
            linkMaterial.SetColor("_PosColor", t.cyan);
            linkMaterial.SetColor("_NegColor", t.amber);
            linkMaterial.SetFloat("_Dim", t.networkDim);
            linkMaterial.SetFloat("_Width", 0.20f);
            linkMaterial.SetFloat("_MinPixels", 2.2f);   // a strong connection is a clear line even from across the net
            linkMaterial.SetFloat("_MaxPixels", 9f);
            linkMaterial.SetFloat("_PulseSpacing", t.pulseSpacing);
            linkMaterial.SetFloat("_PulseSpeed", t.pulseSpeed);
            EditorUtility.SetDirty(linkMaterial);
            // The rails and gates: a thin, dim slate line, always lit, so they frame the ride without competing with the network.
            Material structureMaterial = NetworkMaterial("NetStructure", "Convergence/NeuralLinks");
            Color slate = t.locked * 0.45f;
            slate.a = 1f;
            structureMaterial.SetColor("_PosColor", slate);
            structureMaterial.SetColor("_NegColor", slate);
            structureMaterial.SetFloat("_Width", 0.08f);
            structureMaterial.SetFloat("_MinPixels", 1.2f);
            structureMaterial.SetFloat("_MaxPixels", 2.5f);
            structureMaterial.SetFloat("_Dim", 1f);
            structureMaterial.SetFloat("_LitUpTo", 2f);
            structureMaterial.SetFloat("_Focus", 1f);
            structureMaterial.SetFloat("_Reveal", 1f);
            structureMaterial.SetFloat("_Ignite", 0f);
            EditorUtility.SetDirty(structureMaterial);
            Material nodeMaterial = NetworkMaterial("NetNodes", "Convergence/NeuralNodes");
            nodeMaterial.SetColor("_ColorA", t.cyan);
            nodeMaterial.SetColor("_ColorB", t.violet);
            nodeMaterial.SetFloat("_Dim", t.networkDim);
            nodeMaterial.SetFloat("_HazeStrength", 0.16f);
            EditorUtility.SetDirty(nodeMaterial);

            GameObject linkObject = AddMeshObject("Links", track.transform, linkMesh, "NetLinks");
            AddMeshObject("Structure", track.transform, structureMesh, "NetStructure");
            GameObject nodeObject = AddMeshObject("Nodes", track.transform, nodeMesh, "NetNodes");

            var view = track.AddComponent<NeuralCoreView>();
            Ref(view, "theme", _theme);
            Ref(view, "links", linkObject.GetComponent<Renderer>());
            Ref(view, "nodes", nodeObject.GetComponent<Renderer>());

            int neuronCount = 0;
            foreach (Vector3[] ring in layers) neuronCount += ring.Length;
            Debug.Log("[NeuralRideBuilder] Network: " + sizes.Length + " layers, " + neuronCount + " neurons, "
                + (links.Position.Count / 2) + " ribbon points, " + (structure.Position.Count / 2) + " structure points, "
                + (nodes.Position.Count / 4) + " billboards, 3 draw calls.");
            return track;
        }

        private static Mesh ToLinkMesh(Ribbons r)
        {
            var mesh = new Mesh { name = "NetworkLinks" };
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
            var mesh = new Mesh { name = "NetworkNodes" };
            if (n.Position.Count > 60000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(n.Position);
            mesh.SetUVs(0, n.Corner);
            mesh.SetUVs(1, n.Radius);
            mesh.SetColors(n.Data);
            mesh.SetTriangles(n.Triangles, 0);
            mesh.RecalculateBounds();
            Bounds b = mesh.bounds;
            b.Expand(6f);
            mesh.bounds = b;
            return mesh;
        }
    }
}
