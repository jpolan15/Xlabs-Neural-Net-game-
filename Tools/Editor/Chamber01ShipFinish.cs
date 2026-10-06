using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Presentation;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Window archetypes, damage markers, and the Quest lighting pass.
    /// Called from the level builder. Does not bake lightmaps.
    /// </summary>
    public static class Chamber01ShipFinish
    {
        public static void Apply()
        {
            MovePlanet();
            HideRoomStars();
            BuildArchetypes();
            TightenDust();
            AddDamage();
            Lights();
        }

        static void MovePlanet()
        {
            var planet = GameObject.Find("Celestial_GasGiantPlanet");
            if (planet == null) return;
            planet.transform.position = new Vector3(40f, 25f, 160f);
            planet.transform.localScale = Vector3.one * 36f;
        }

        static void HideRoomStars()
        {
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.gameObject.name.StartsWith("Star_"))
                {
                    renderer.gameObject.SetActive(false);
                }
            }
        }

        static void BuildArchetypes()
        {
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.gameObject.name.StartsWith("Asteroid_"))
                {
                    renderer.gameObject.SetActive(false);
                }
            }

            var parent = GameObject.Find("ForwardObservationViewportAndDeepSpaceVista");
            if (parent == null) parent = new GameObject("WindowArchetypes");
            var root = new GameObject("WindowArchetypes");
            root.transform.SetParent(parent.transform, false);

            var rockMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            rockMat.color = new Color(0.34f, 0.32f, 0.30f);
            rockMat.SetFloat("_Smoothness", 0.15f);
            var iceMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            iceMat.color = new Color(0.62f, 0.68f, 0.72f);
            iceMat.SetFloat("_Smoothness", 0.45f);

            Place("Rock", root.transform, new Vector3(-2.2f, 1.6f, 14f), RockMesh(11), rockMat, "Mat_Chamber01_IconRock");
            Place("Comet", root.transform, new Vector3(1.4f, 2.1f, 16f), CometMesh(), iceMat, "Mat_Chamber01_IconComet");
            Place("Both", root.transform, new Vector3(3.2f, 1.3f, 13f), RockMesh(29), rockMat, "Mat_Chamber01_IconBoth");
            PlaceDrone(root.transform, new Vector3(-0.4f, 2.4f, 18f));
        }

        static void Place(string name, Transform parent, Vector3 position, Mesh mesh, Material body, string iconMaterial)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = body;
            Tag(go.transform, iconMaterial, 2.2f);
        }

        static void PlaceDrone(Transform parent, Vector3 position)
        {
            var go = new GameObject("Drone");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var body = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Mainframe_DarkPlating.mat");
            Part(go.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.55f, 0.22f, 0.9f), body);
            Part(go.transform, PrimitiveType.Cube, new Vector3(0f, 0.16f, -0.1f), new Vector3(0.28f, 0.16f, 0.4f), body);
            Part(go.transform, PrimitiveType.Cylinder, new Vector3(0.42f, 0f, 0.1f), new Vector3(0.12f, 0.08f, 0.28f), body);
            Part(go.transform, PrimitiveType.Cylinder, new Vector3(-0.42f, 0f, 0.1f), new Vector3(0.12f, 0.08f, 0.28f), body);
            var beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "Beacon";
            beacon.transform.SetParent(go.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 0.22f, 0.35f);
            beacon.transform.localScale = Vector3.one * 0.12f;
            Object.DestroyImmediate(beacon.GetComponent<Collider>());
            Tag(go.transform, "Mat_Chamber01_IconDrone", 2.2f);
        }

        static void Part(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        static void Tag(Transform target, string materialName, float distance)
        {
            float height = distance * Mathf.Tan(2f * Mathf.Deg2Rad);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = target.name + "Tag";
            quad.transform.SetParent(target, false);
            quad.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            quad.transform.localScale = Vector3.one * height;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + materialName + ".mat");
            if (mat != null) quad.GetComponent<Renderer>().sharedMaterial = mat;
            var eye = target.position + new Vector3(0f, 0f, -distance);
            quad.transform.rotation = Quaternion.LookRotation(quad.transform.position - eye, Vector3.up);
        }

        static void TightenDust()
        {
            var dust = GameObject.Find("DataStreamParticles");
            if (dust == null) return;
            dust.transform.position = new Vector3(0f, 1.05f, -0.1f);
            var ps = dust.GetComponent<ParticleSystem>();
            if (ps == null) return;
            var main = ps.main;
            main.startColor = new Color(0.45f, 0.48f, 0.52f, 0.18f);
            main.maxParticles = 24;
            var shape = ps.shape;
            shape.scale = new Vector3(0.7f, 0.35f, 0.5f);
        }

        static void AddDamage()
        {
            Mark("EmergencySparkParticles", "spark");
            var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name = "DeadScreen";
            screen.transform.position = new Vector3(-1.15f, 1.15f, -0.05f);
            screen.transform.localScale = new Vector3(0.28f, 0.16f, 1f);
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            Mark(screen, "screen");

            var crack = GameObject.CreatePrimitive(PrimitiveType.Quad);
            crack.name = "WindowCrack";
            crack.transform.position = new Vector3(2.6f, 2.4f, 3.52f);
            crack.transform.localScale = new Vector3(0.45f, 0.55f, 1f);
            Object.DestroyImmediate(crack.GetComponent<Collider>());
            Mark(crack, "crack");

            for (int i = 0; i < 3; i++)
            {
                var cable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cable.name = "HangingCable_" + i;
                cable.transform.position = new Vector3(-1.4f + (i * 0.15f), 2.6f, -1.2f);
                cable.transform.localScale = new Vector3(0.02f, 0.35f, 0.02f);
                Object.DestroyImmediate(cable.GetComponent<Collider>());
                Mark(cable, "cable");
            }

            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "AmberEmergencyStrip";
            strip.transform.position = new Vector3(0f, 3.4f, -1.5f);
            strip.transform.localScale = new Vector3(2.4f, 0.04f, 0.06f);
            Object.DestroyImmediate(strip.GetComponent<Collider>());
            var amber = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Spaceship_WarningAmber.mat");
            if (amber != null) strip.GetComponent<Renderer>().sharedMaterial = amber;
            Mark(strip, "strip");

            var host = new GameObject("ChamberDamage");
            host.AddComponent<ChamberDamageVisual>();
        }

        static void Mark(string objectName, string kind)
        {
            var go = GameObject.Find(objectName);
            if (go != null) Mark(go, kind);
        }

        static void Mark(GameObject go, string kind)
        {
            var mark = go.GetComponent<StateBoundDamage>();
            if (mark == null) mark = go.AddComponent<StateBoundDamage>();
            var so = new SerializedObject(mark);
            so.FindProperty("kind").stringValue = kind;
            so.ApplyModifiedProperties();
        }

        static void Lights()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.04f, 0.06f, 0.09f);
            RenderSettings.ambientEquatorColor = new Color(0.07f, 0.08f, 0.09f);
            RenderSettings.ambientGroundColor = new Color(0.01f, 0.01f, 0.012f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = 0.25f;
            if (RenderSettings.skybox != null)
            {
                RenderSettings.customReflectionTexture = RenderSettings.skybox.GetTexture("_Tex");
            }

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (light.name == "DirectionalLight_StellarKey")
                {
                    light.type = LightType.Directional;
                    light.shadows = LightShadows.None;
                    light.lightmapBakeType = LightmapBakeType.Realtime;
                    light.intensity = 0.65f;
                    light.color = new Color(0.75f, 0.82f, 0.92f);
                    continue;
                }

                if (light.name == "NeuralNetwork_Spotlight")
                {
                    light.enabled = true;
                    light.shadows = LightShadows.None;
                    light.intensity = 0.8f;
                    light.color = new Color(0.85f, 0.90f, 0.95f);
                    continue;
                }

                if (light.name == "AmberEmergencyLight" || light.name == "CoreAccentLight")
                {
                    light.enabled = true;
                    light.shadows = LightShadows.None;
                    continue;
                }

                light.enabled = false;
            }

            var amberGo = new GameObject("AmberEmergencyLight");
            amberGo.transform.position = new Vector3(0f, 3.2f, -1.2f);
            var amberLight = amberGo.AddComponent<Light>();
            amberLight.type = LightType.Point;
            amberLight.range = 4f;
            amberLight.intensity = 0.35f;
            amberLight.shadows = LightShadows.None;
            amberLight.color = new Color(0.98f, 0.60f, 0.08f);
            amberLight.lightmapBakeType = LightmapBakeType.Realtime;

            var coreGo = new GameObject("CoreAccentLight");
            coreGo.transform.position = new Vector3(0f, 1.2f, 0.2f);
            var core = coreGo.AddComponent<Light>();
            core.type = LightType.Point;
            core.range = 2.5f;
            core.intensity = 0.4f;
            core.shadows = LightShadows.None;
            core.color = new Color(0.20f, 0.75f, 0.98f);
            core.lightmapBakeType = LightmapBakeType.Realtime;

            var probes = new GameObject("PlayerLightProbes");
            var group = probes.AddComponent<LightProbeGroup>();
            group.probePositions = new[]
            {
                new Vector3(0f, 0.4f, -0.4f),
                new Vector3(-1.2f, 1.2f, 0.2f),
                new Vector3(1.2f, 1.2f, 0.2f),
                new Vector3(0f, 1.6f, 1.2f),
                new Vector3(0f, 0.8f, -1.4f)
            };
        }

        static Mesh RockMesh(int seed)
        {
            var rng = new System.Random(seed);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            Icosahedron(verts, tris);
            for (int sub = 0; sub < 2; sub++) Subdivide(verts, tris);
            var flatV = new List<Vector3>();
            var flatT = new List<int>();
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 a = Displace(verts[tris[i]], rng);
                Vector3 b = Displace(verts[tris[i + 1]], rng);
                Vector3 c = Displace(verts[tris[i + 2]], rng);
                int n = flatV.Count;
                flatV.Add(a);
                flatV.Add(b);
                flatV.Add(c);
                flatT.Add(n);
                flatT.Add(n + 1);
                flatT.Add(n + 2);
            }

            var mesh = new Mesh();
            mesh.name = "Rock_" + seed;
            mesh.SetVertices(flatV);
            mesh.SetTriangles(flatT, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh CometMesh()
        {
            var mesh = RockMesh(3);
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 p = verts[i].normalized;
                float stretch = 1f + Mathf.Max(0f, p.z) * 0.8f;
                verts[i] = new Vector3(p.x * 0.45f, p.y * 0.45f, p.z * stretch);
            }

            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Vector3 Displace(Vector3 v, System.Random rng)
        {
            float n = (float)rng.NextDouble() * 0.18f;
            return v.normalized * (0.55f + n);
        }

        static void Icosahedron(List<Vector3> verts, List<int> tris)
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            Vector3[] p =
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < p.Length; i++) verts.Add(p[i].normalized);
            int[] idx =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };
            tris.AddRange(idx);
        }

        static void Subdivide(List<Vector3> verts, List<int> tris)
        {
            var next = new List<int>();
            var mid = new Dictionary<long, int>();
            for (int i = 0; i < tris.Count; i += 3)
            {
                int a = tris[i];
                int b = tris[i + 1];
                int c = tris[i + 2];
                int ab = Mid(verts, mid, a, b);
                int bc = Mid(verts, mid, b, c);
                int ca = Mid(verts, mid, c, a);
                next.Add(a); next.Add(ab); next.Add(ca);
                next.Add(b); next.Add(bc); next.Add(ab);
                next.Add(c); next.Add(ca); next.Add(bc);
                next.Add(ab); next.Add(bc); next.Add(ca);
            }

            tris.Clear();
            tris.AddRange(next);
        }

        static int Mid(List<Vector3> verts, Dictionary<long, int> mid, int a, int b)
        {
            int lo = Mathf.Min(a, b);
            int hi = Mathf.Max(a, b);
            long key = ((long)lo << 32) | (uint)hi;
            if (mid.TryGetValue(key, out int found)) return found;
            int index = verts.Count;
            verts.Add((verts[a] + verts[b]).normalized);
            mid[key] = index;
            return index;
        }
    }
}
