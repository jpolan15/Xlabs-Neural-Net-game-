using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using Convergence.Presentation.Ride.Level2;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Level 2's look (ADR-023): the rider sits on a bright cargo bike instead of Level 1's dark pod, and rides a country road under a
    /// daylight sky. The bike's front box carries the same dash (subtitles, topic, GO lever) and game table as Level 1's cockpit, so every
    /// panel keeps the reading distance it was tuned for; the handlebars sit below the dash's sight lines. Wheels, pedals and handlebars
    /// move (BikeView); the rider's view never tilts. Also here: the daylight sky material, its cloud noise, the outlined text that stays
    /// readable on a bright sky, and the soft (not additive) glows Level 2 draws with. Development tooling only.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private static void MakeBikeMaterials()
        {
            H("BikeFrame", new Color(1f, 0.36f, 0.3f), 0.75f, 0.3f, Color.white);
            H("BikeCream", new Color(0.99f, 0.94f, 0.84f), 0.7f, 0.2f, Color.white);
            H("BikeChrome", new Color(0.8f, 0.84f, 0.9f), 0.9f, 0.7f, Color.white);
            H("BikeTyre", new Color(0.13f, 0.14f, 0.17f), 0.9f, 0.25f, new Color(0.5f, 0.52f, 0.58f));
            H("BikeBox", new Color(1f, 0.76f, 0.24f), 0.75f, 0.25f, Color.white);
            H("BikeSeat", new Color(0.36f, 0.24f, 0.18f), 0.8f, 0.3f, Color.white);

            // The countryside's solids: vertex-coloured, lit, only lamps glow (vertex alpha); and soft alpha glows, which still show on a
            // bright sky where additive light would vanish.
            Tiles("L2Solid", Color.white, 0.7f, Color.white, 0.1f);
            Particles("L2Soft", Color.white, false, GlowTexture());
        }

        /// <summary>The text material Level 2's world-space words use: the font with a dark outline, readable on sky, grass or a board.</summary>
        private static Material OutlinedFont(TMP_FontAsset font, string name)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(font.material);
                AssetDatabase.CreateAsset(m, path);
            }

            m.shader = font.material.shader;
            m.CopyPropertiesFromMaterial(font.material);
            m.EnableKeyword(ShaderUtilities.Keyword_Outline);
            m.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.24f);
            m.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.05f, 0.07f, 0.15f, 1f));
            m.SetFloat(ShaderUtilities.ID_FaceDilate, 0.12f);
            ShaderUtilities.UpdateShaderRatios(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Convergence/DaySky with its cloud noise and the sun high on the right, so the boards ahead never sit in its glare.</summary>
        private static Material DaySkyMaterial()
        {
            Shader shader = Shader.Find("Convergence/DaySky");
            if (shader == null) throw new FileNotFoundException("Shader Convergence/DaySky is missing (Assets/Shaders/DaySky.shader).");
            string path = MatDir + "/DaySky.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }

            m.shader = shader;
            m.SetTexture("_CloudTex", NoiseTexture());
            m.SetVector("_SunDir", new Vector4(0.62f, 0.6f, 0.5f, 0f));
            m.SetFloat("_GroundY", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>A tileable, contrast-stretched value-noise texture (five octaves), for the clouds and the meadow's patches.</summary>
        private static Texture2D NoiseTexture()
        {
            string path = MatDir + "/L2Noise.png";
            if (!File.Exists(path))
            {
                const int n = 256;
                float Hash(int x, int y, int s)
                {
                    unchecked
                    {
                        uint h = (uint)(x * 374761393 + y * 668265263 + s * 982451653);
                        h = (h ^ (h >> 13)) * 1274126177u;
                        return (h ^ (h >> 16)) / 4294967295f;
                    }
                }

                float Value(float x, float y, int period, int s)
                {
                    int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
                    float fx = x - x0, fy = y - y0;
                    fx = fx * fx * (3f - 2f * fx);
                    fy = fy * fy * (3f - 2f * fy);
                    int xa = ((x0 % period) + period) % period, xb = (xa + 1) % period, ya = ((y0 % period) + period) % period, yb = (ya + 1) % period;
                    return Mathf.Lerp(Mathf.Lerp(Hash(xa, ya, s), Hash(xb, ya, s), fx), Mathf.Lerp(Hash(xa, yb, s), Hash(xb, yb, s), fx), fy);
                }

                var values = new float[n * n];
                float lo = float.MaxValue, hi = float.MinValue;
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float u = x / (float)n, v = y / (float)n, sum = 0f, amp = 0.5f;
                        for (int o = 0; o < 5; o++)
                        {
                            int period = 4 << o;
                            sum += amp * Value(u * period, v * period, period, o);
                            amp *= 0.5f;
                        }

                        values[y * n + x] = sum;
                        lo = Mathf.Min(lo, sum);
                        hi = Mathf.Max(hi, sum);
                    }
                }

                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (int i = 0; i < values.Length; i++)
                {
                    float k = Mathf.InverseLerp(lo, hi, values[i]);
                    tex.SetPixel(i % n, i / n, new Color(k, k, k, 1f));
                }

                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.mipmapEnabled = true;
                imp.sRGBTexture = false;
                imp.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>A round tube from a to b (a stretched cylinder).</summary>
        private static GameObject Tube(string name, Transform parent, Vector3 a, Vector3 b, float thick, string mat)
        {
            Vector3 d = b - a;
            GameObject go = Prim(PrimitiveType.Cylinder, name, parent, (a + b) * 0.5f, new Vector3(thick, d.magnitude * 0.5f, thick), mat);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            return go;
        }

        /// <summary>
        /// The bike, in the pod's space (the rider's eye at 1.3 m over the origin, +z ahead): a cargo bike whose yellow box sits under the
        /// dash (the dash's tilted panel is the box's lid), handlebars just in front of the rider, the frame and saddle under them, the
        /// crank and pedals between the knees, a small front wheel past the box and the back wheel behind.
        /// </summary>
        private static void BuildBike(GameObject pod)
        {
            Transform root = Make("Bike", pod.transform, Vector3.zero).transform;

            // The frame, the box and the saddle: one mesh per material.
            Transform frame = Make("Frame", root, Vector3.zero).transform;
            Transform parts = Make("Parts", frame, Vector3.zero).transform;
            Tube("HeadTube", parts, new Vector3(0f, 0.46f, 0.33f), new Vector3(0f, 0.72f, 0.28f), 0.055f, "BikeFrame");
            Tube("TopTube", parts, new Vector3(0f, 0.69f, 0.29f), new Vector3(0f, 0.64f, -0.16f), 0.045f, "BikeFrame");
            Tube("DownTube", parts, new Vector3(0f, 0.55f, 0.32f), new Vector3(0f, 0.27f, 0.02f), 0.05f, "BikeFrame");
            Tube("SeatTube", parts, new Vector3(0f, 0.27f, 0.02f), new Vector3(0f, 0.70f, -0.19f), 0.045f, "BikeFrame");
            Tube("SeatPost", parts, new Vector3(0f, 0.68f, -0.18f), new Vector3(0f, 0.74f, -0.21f), 0.03f, "BikeChrome");
            Prim(PrimitiveType.Sphere, "Saddle", parts, new Vector3(0f, 0.76f, -0.23f), new Vector3(0.17f, 0.06f, 0.27f), "BikeSeat");
            foreach (float s in new[] { -1f, 1f })
            {
                Tube("ChainStay", parts, new Vector3(s * 0.04f, 0.27f, 0.02f), new Vector3(s * 0.06f, 0.33f, -0.72f), 0.03f, "BikeFrame");
                Tube("SeatStay", parts, new Vector3(s * 0.03f, 0.66f, -0.16f), new Vector3(s * 0.06f, 0.33f, -0.72f), 0.026f, "BikeFrame");
                Tube("Fork", parts, new Vector3(s * 0.05f, 0.56f, 1.12f), new Vector3(s * 0.05f, 0.25f, 1.15f), 0.03f, "BikeChrome");
            }

            Tube("BoxBeamA", parts, new Vector3(0f, 0.5f, 0.33f), new Vector3(0f, 0.2f, 0.45f), 0.05f, "BikeFrame");
            Tube("BoxBeamB", parts, new Vector3(0f, 0.2f, 0.45f), new Vector3(0f, 0.2f, 1.0f), 0.05f, "BikeFrame");
            Tube("BoxBeamC", parts, new Vector3(0f, 0.2f, 1.0f), new Vector3(0f, 0.58f, 1.12f), 0.05f, "BikeFrame");
            Tube("Fender", parts, new Vector3(0f, 0.6f, -0.55f), new Vector3(0f, 0.62f, -0.9f), 0.12f, "BikeCream");
            CombineByMaterial(parts, frame, "BikeFrame");

            // The box: walls under the dash's lid, its top edges trimmed in cream.
            const float x = 0.64f, bottom = 0.21f, backZ = 0.36f, frontZ = 0.885f, backTop = 0.395f, frontTop = 0.925f;
            var box = new MeshData();
            foreach (float s in new[] { -1f, 1f })
            {
                box.Quad(new Vector3(s * x, bottom, backZ), new Vector3(s * x, bottom, frontZ), new Vector3(s * x, frontTop, frontZ), new Vector3(s * x, backTop, backZ), new Vector3(s, 0f, 0f));
            }

            box.Quad(new Vector3(-x, bottom, frontZ), new Vector3(x, bottom, frontZ), new Vector3(x, frontTop, frontZ), new Vector3(-x, frontTop, frontZ), Vector3.forward);
            box.Quad(new Vector3(-x, bottom, backZ), new Vector3(x, bottom, backZ), new Vector3(x, backTop, backZ), new Vector3(-x, backTop, backZ), Vector3.back);
            box.Quad(new Vector3(-x, bottom, backZ), new Vector3(x, bottom, backZ), new Vector3(x, bottom, frontZ), new Vector3(-x, bottom, frontZ), Vector3.down);
            AddMeshObject("Box", root, SaveMesh(box.ToMesh("BikeBox"), "BikeBox"), "BikeBox");
            Transform trim = Make("Trim", root, Vector3.zero).transform;
            Transform trimParts = Make("Parts", trim, Vector3.zero).transform;
            foreach (float s in new[] { -1f, 1f }) Tube("Rim", trimParts, new Vector3(s * x, backTop, backZ), new Vector3(s * x, frontTop, frontZ), 0.035f, "BikeCream");
            Tube("RimFront", trimParts, new Vector3(-x, frontTop, frontZ), new Vector3(x, frontTop, frontZ), 0.035f, "BikeCream");
            Tube("RimBack", trimParts, new Vector3(-x, backTop, backZ), new Vector3(x, backTop, backZ), 0.035f, "BikeCream");
            CombineByMaterial(trimParts, trim, "BikeTrim");

            // The handlebars turn about the head tube: stem, a swept-back bar, cream grips, a bell.
            Transform bars = Make("Bars", root, new Vector3(0f, 0.72f, 0.28f)).transform; // below the dash's sight lines: the GO lever stays in plain view
            Transform barParts = Make("Parts", bars, Vector3.zero).transform;
            Tube("Stem", barParts, Vector3.zero, new Vector3(0f, 0.10f, -0.06f), 0.04f, "BikeChrome");
            foreach (float s in new[] { -1f, 1f })
            {
                Tube("Bar", barParts, new Vector3(0f, 0.10f, -0.07f), new Vector3(s * 0.22f, 0.10f, -0.07f), 0.026f, "BikeChrome");
                Tube("Sweep", barParts, new Vector3(s * 0.22f, 0.10f, -0.07f), new Vector3(s * 0.34f, 0.11f, -0.15f), 0.026f, "BikeChrome");
                Tube("Grip", barParts, new Vector3(s * 0.32f, 0.108f, -0.135f), new Vector3(s * 0.40f, 0.116f, -0.19f), 0.042f, "BikeCream");
                Tube("Brake", barParts, new Vector3(s * 0.25f, 0.115f, -0.09f), new Vector3(s * 0.34f, 0.09f, -0.16f), 0.012f, "BikeChrome");
            }

            GameObject bell = Prim(PrimitiveType.Sphere, "Bell", barParts, new Vector3(0.12f, 0.135f, -0.07f), new Vector3(0.055f, 0.035f, 0.055f), "BikeChrome");
            bell.transform.localRotation = Quaternion.identity;
            CombineByMaterial(barParts, bars, "BikeBars");

            Transform front = BuildWheel(root, "FrontWheel", new Vector3(0f, 0.25f, 1.15f), 0.25f);
            Transform rear = BuildWheel(root, "RearWheel", new Vector3(0f, 0.33f, -0.72f), 0.33f);

            // The crank between the knees: a chainring and two arms; the pedals hang level on their own pivots.
            Transform crank = Make("Crank", root, new Vector3(0f, 0.27f, 0.02f)).transform;
            Transform crankParts = Make("Parts", crank, Vector3.zero).transform;
            GameObject ring = Prim(PrimitiveType.Cylinder, "Chainring", crankParts, new Vector3(0.07f, 0f, 0f), new Vector3(0.2f, 0.006f, 0.2f), "BikeChrome");
            ring.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Tube("ArmR", crankParts, new Vector3(0.09f, 0f, 0f), new Vector3(0.09f, -0.17f, 0f), 0.025f, "BikeChrome");
            Tube("ArmL", crankParts, new Vector3(-0.09f, 0f, 0f), new Vector3(-0.09f, 0.17f, 0f), 0.025f, "BikeChrome");
            CombineByMaterial(crankParts, crank, "BikeCrank");
            Transform pedalR = Make("PedalR", crank, new Vector3(0.15f, -0.17f, 0f)).transform;
            Transform pedalL = Make("PedalL", crank, new Vector3(-0.15f, 0.17f, 0f)).transform;
            foreach (Transform p in new[] { pedalR, pedalL }) Prim(PrimitiveType.Cube, "Pedal", p, Vector3.zero, new Vector3(0.1f, 0.025f, 0.075f), "BikeTyre");

            var view = pod.AddComponent<BikeView>();
            Ref(view, "frontWheel", front);
            Ref(view, "rearWheel", rear);
            Ref(view, "crank", crank);
            Refs(view, "pedals", new System.Collections.Generic.List<Object> { pedalR, pedalL });
            Ref(view, "bars", bars);
            Float(view, "frontRadius", 0.25f);
            Float(view, "rearRadius", 0.33f);
        }

        /// <summary>A wheel on its own pivot at the hub (it turns about x): tyre, rim and spokes, combined per material.</summary>
        private static Transform BuildWheel(Transform parent, string name, Vector3 hub, float radius)
        {
            Transform wheel = Make(name, parent, hub).transform;
            Transform parts = Make("Parts", wheel, Vector3.zero).transform;
            const int n = 18;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                Vector3 p0 = new Vector3(0f, Mathf.Cos(a0), Mathf.Sin(a0)) * radius, p1 = new Vector3(0f, Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
                Tube("Tyre", parts, p0 * 1.04f, p1 * 1.04f, 0.055f, "BikeTyre");
                Tube("Rim", parts, p0 * 0.9f, p1 * 0.9f, 0.025f, "BikeChrome");
            }

            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Tube("Spoke", parts, Vector3.zero, new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a)) * radius * 0.9f, 0.008f, "BikeChrome");
            }

            GameObject hubCap = Prim(PrimitiveType.Cylinder, "Hub", parts, Vector3.zero, new Vector3(0.06f, 0.05f, 0.06f), "BikeFrame");
            hubCap.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            CombineByMaterial(parts, wheel, "Bike" + name);
            return wheel;
        }
    }
}
