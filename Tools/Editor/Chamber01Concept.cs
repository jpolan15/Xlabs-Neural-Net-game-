using System;
using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using Convergence.Gameplay;
using Convergence.Presentation;

namespace Convergence.EditorTools
{
    /// <summary>
    /// WP3. Builds the neuron diagram, four target cards, and the boundary quad
    /// under NeuronDisplayAnchor on the console. Icons are generated 128 textures.
    /// </summary>
    public static class Chamber01Concept
    {
        const int N = 128;

        public static void Install(NeuralState neuralState, ChamberController chamber)
        {
            var console = GameObject.Find("TactileEngineeringWorkstation");
            if (console == null || neuralState == null || chamber == null)
            {
                Debug.LogError("[Chamber01Concept] Console or gameplay refs missing. Diagram was not built.");
                return;
            }

            Texture2D rock = Icon("Icon_Chamber01_Rock", PaintRock);
            Texture2D ice = Icon("Icon_Chamber01_Ice", PaintIce);
            Texture2D fire = Icon("Icon_Chamber01_Fire", PaintFire);
            Texture2D drone = Icon("Icon_Chamber01_Drone", PaintDrone);
            Texture2D comet = Icon("Icon_Chamber01_Comet", PaintComet);
            Texture2D both = Icon("Icon_Chamber01_Both", PaintBoth);
            Texture2D check = Icon("Icon_Chamber01_Check", PaintCheck);
            Texture2D cross = Icon("Icon_Chamber01_Cross", PaintCross);
            Texture2D dial = Icon("Icon_Chamber01_Dial", PaintDial);
            Texture2D hand = Icon("Icon_Chamber01_Hand", PaintHand);
            Texture2D dash = Icon("Icon_Chamber01_Dash", PaintDash);

            Material white = Unlit("Mat_Chamber01_Icon", Color.white, null, true);
            Material cyan = Unlit("Mat_Chamber01_StateCyan", new Color(0.20f, 0.75f, 0.98f, 1f), null, true);
            Material amber = Unlit("Mat_Chamber01_StateAmber", new Color(0.98f, 0.62f, 0.08f, 1f), null, true);
            Material slate = Unlit("Mat_Chamber01_Slate", new Color(0.30f, 0.38f, 0.46f, 1f), null, true);
            Material rockMat = Unlit("Mat_Chamber01_IconRock", Color.white, rock, true);
            Material iceMat = Unlit("Mat_Chamber01_IconIce", Color.white, ice, true);
            Material fireMat = Unlit("Mat_Chamber01_IconFire", Color.white, fire, true);
            Material dialMat = Unlit("Mat_Chamber01_IconDial", Color.white, dial, true);
            Material droneMat = Unlit("Mat_Chamber01_IconDrone", Color.white, drone, true);
            Material cometMat = Unlit("Mat_Chamber01_IconComet", Color.white, comet, true);
            Material bothMat = Unlit("Mat_Chamber01_IconBoth", Color.white, both, true);
            Material handMat = Unlit("Mat_Chamber01_IconHand", new Color(0.75f, 0.78f, 0.80f, 1f), hand, true);
            Material crosshairBadge = Unlit("Mat_Chamber01_IconCrosshairBadge", new Color(0.75f, 0.78f, 0.80f, 1f), fire, true);
            Material checkMat = Unlit("Mat_Chamber01_IconCheck", new Color(0.20f, 0.75f, 0.98f, 1f), check, true);
            Material crossMat = Unlit("Mat_Chamber01_IconCross", new Color(0.98f, 0.62f, 0.08f, 1f), cross, true);
            Material solidWire = Unlit("Mat_Chamber01_WireCyan", new Color(0.20f, 0.75f, 0.98f, 1f), null, false);
            Material slateWire = Unlit("Mat_Chamber01_WireSlate", new Color(0.30f, 0.38f, 0.46f, 1f), null, false);
            Material dashWire = Unlit("Mat_Chamber01_WireDash", new Color(0.20f, 0.75f, 0.98f, 1f), dash, false);
            Material boundary = BoundaryMaterial();

            var anchor = new GameObject("NeuronDisplayAnchor");
            anchor.transform.SetParent(console.transform, true);
            anchor.transform.position = new Vector3(0f, 1.08f, -0.08f);
            Vector3 eye = Eye();
            Vector3 toEye = eye - anchor.transform.position;
            if (toEye.sqrMagnitude > 0.0001f)
            {
                anchor.transform.rotation = Quaternion.LookRotation(toEye, Vector3.up);
            }

            Quad(anchor.transform, "RockNode", new Vector3(-0.16f, 0.06f, 0f), 0.06f, rockMat);
            Quad(anchor.transform, "IceNode", new Vector3(-0.16f, -0.06f, 0f), 0.06f, iceMat);
            Quad(anchor.transform, "BiasNode", new Vector3(-0.16f, -0.16f, 0f), 0.045f, dialMat);
            var fireNode = Quad(anchor.transform, "FireNode", new Vector3(0.16f, 0.00f, 0f), 0.07f, fireMat);
            var sum = new GameObject("SumReadout");
            sum.transform.SetParent(fireNode.transform, false);
            sum.transform.localPosition = new Vector3(0f, -0.55f, 0.02f);
            var sumText = sum.AddComponent<TextMesh>();
            sumText.text = "—";
            sumText.fontSize = 0;
            sumText.characterSize = 0.35f;
            sumText.anchor = TextAnchor.MiddleCenter;
            sumText.alignment = TextAlignment.Center;
            sumText.color = new Color(0.90f, 0.91f, 0.88f, 1f);

            Wire(anchor.transform, "RockWire", new Vector3(-0.16f, 0.06f, 0f), new Vector3(0.16f, 0.02f, 0f), solidWire);
            Wire(anchor.transform, "IceWire", new Vector3(-0.16f, -0.06f, 0f), new Vector3(0.16f, -0.02f, 0f), solidWire);
            Wire(anchor.transform, "BiasWire", new Vector3(-0.16f, -0.16f, 0f), new Vector3(0.16f, -0.04f, 0f), slateWire);
            Minus(anchor.transform, "RockMinus", new Vector3(0f, 0.04f, 0.01f));
            Minus(anchor.transform, "IceMinus", new Vector3(0f, -0.04f, 0.01f));
            Minus(anchor.transform, "BiasMinus", new Vector3(0f, -0.10f, 0.01f));

            var boundaryGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            boundaryGo.name = "BoundaryQuad";
            boundaryGo.transform.SetParent(anchor.transform, false);
            boundaryGo.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            boundaryGo.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            boundaryGo.GetComponent<Renderer>().sharedMaterial = boundary;
            UnityEngine.Object.DestroyImmediate(boundaryGo.GetComponent<Collider>());

            Material[] icons = { droneMat, cometMat, rockMat, bothMat };
            Material[] badges = { handMat, crosshairBadge, crosshairBadge, crosshairBadge };
            for (int i = 0; i < 4; i++)
            {
                float x = -0.12f + (i * 0.08f);
                var card = new GameObject("Card" + i);
                card.transform.SetParent(anchor.transform, false);
                card.transform.localPosition = new Vector3(x, -0.18f, 0f);
                Quad(card.transform, "Outline", new Vector3(0f, 0f, -0.002f), 0.07f, cyan);
                Quad(card.transform, "Icon", Vector3.zero, 0.05f, icons[i]);
                Quad(card.transform, "Badge", new Vector3(0.02f, -0.025f, 0.002f), 0.02f, badges[i]);
                Quad(card.transform, "MarkCheck", new Vector3(0.02f, 0.02f, 0.003f), 0.02f, checkMat);
                Quad(card.transform, "MarkCross", new Vector3(0.02f, 0.02f, 0.003f), 0.02f, crossMat);
                card.transform.Find("MarkCheck").gameObject.SetActive(false);
                card.transform.Find("MarkCross").gameObject.SetActive(false);
                card.transform.Find("Outline").gameObject.SetActive(false);
            }

            var visual = anchor.AddComponent<NeuronCauseVisual>();
            var so = new SerializedObject(visual);
            so.FindProperty("neuralState").objectReferenceValue = neuralState;
            so.FindProperty("chamberController").objectReferenceValue = chamber;
            so.FindProperty("cyanWire").objectReferenceValue = solidWire;
            so.FindProperty("slateWire").objectReferenceValue = slateWire;
            so.FindProperty("dashWire").objectReferenceValue = dashWire;
            so.FindProperty("wireMin").floatValue = 0.004f;
            so.FindProperty("wireMax").floatValue = 0.03f;
            so.FindProperty("weightMax").floatValue = 2f;
            so.FindProperty("cycleSeconds").floatValue = 3f;
            so.ApplyModifiedProperties();

            if (white == null || slate == null || amber == null)
            {
                Debug.LogWarning("[Chamber01Concept] A tint material failed to build.");
            }
        }

        static Material BoundaryMaterial()
        {
            const string path = "Assets/Materials/Mat_Chamber01_Boundary.mat";
            var shader = Shader.Find("Chamber01/NeuronBoundary");
            if (shader == null)
            {
                Debug.LogError("[Chamber01Concept] Chamber01/NeuronBoundary shader was not found.");
                return null;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.SetFloat("_W1", 0f);
            mat.SetFloat("_W2", 0f);
            mat.SetFloat("_B", -1f);
            mat.SetFloat("_UseHidden", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Vector3 Eye()
        {
            var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            if (origin == null) return new Vector3(0f, 1.36144f, -0.9f);
            float height = 1.36144f;
            Transform offset = origin.transform.Find("Camera Offset");
            if (offset != null) height = offset.localPosition.y;
            return origin.transform.position + Vector3.up * height;
        }

        static GameObject Quad(Transform parent, string name, Vector3 local, float size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * size;
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void Wire(Transform parent, string name, Vector3 a, Vector3 b, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, a);
            line.SetPosition(1, b);
            line.startWidth = 0.004f;
            line.endWidth = 0.004f;
            line.sharedMaterial = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        static void Minus(Transform parent, string name, Vector3 local)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = new Vector3(0.03f, 0.012f, 1f);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.SetActive(false);
        }

        static Material Unlit(string name, Color color, Texture2D texture, bool transparent)
        {
            string path = "Assets/Materials/" + name + ".mat";
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (texture != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
            if (transparent)
            {
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Texture2D Icon(string file, Action<Color[]> paint)
        {
            var pixels = new Color[N * N];
            paint(pixels);
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            string path = "Assets/Materials/" + file + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.sRGBTexture = true;
                importer.maxTextureSize = 128;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void PaintRock(Color[] px)
        {
            Disc(px, 64, 70, 28);
            Disc(px, 40, 60, 18);
            Disc(px, 86, 58, 16);
            Disc(px, 70, 42, 14);
        }

        static void PaintIce(Color[] px)
        {
            for (int y = 20; y < 108; y++)
            for (int x = 20; x < 108; x++)
                if (Mathf.Abs(x - 64) + Mathf.Abs(y - 64) < 40) Plot(px, x, y);
        }

        static void PaintFire(Color[] px)
        {
            Ring(px, 64, 64, 30, 4);
            Bar(px, 64, 28, 64, 100, 3);
            Bar(px, 28, 64, 100, 64, 3);
        }

        static void PaintDrone(Color[] px)
        {
            Disc(px, 64, 64, 12);
            Bar(px, 20, 64, 108, 64, 6);
            Disc(px, 24, 64, 8);
            Disc(px, 104, 64, 8);
        }

        static void PaintComet(Color[] px)
        {
            Disc(px, 78, 64, 18);
            Bar(px, 20, 64, 70, 64, 4);
        }

        static void PaintBoth(Color[] px)
        {
            PaintRock(px);
            for (int y = 40; y < 88; y++)
            for (int x = 40; x < 88; x++)
                if (Mathf.Abs(x - 64) + Mathf.Abs(y - 64) < 16) Plot(px, x, y);
        }

        static void PaintCheck(Color[] px)
        {
            Bar(px, 28, 60, 52, 36, 6);
            Bar(px, 52, 36, 100, 92, 6);
        }

        static void PaintCross(Color[] px)
        {
            Bar(px, 28, 28, 100, 100, 6);
            Bar(px, 28, 100, 100, 28, 6);
        }

        static void PaintDial(Color[] px)
        {
            Ring(px, 64, 64, 34, 5);
            Bar(px, 64, 64, 96, 80, 4);
        }

        static void PaintHand(Color[] px)
        {
            Bar(px, 40, 20, 40, 80, 8);
            Bar(px, 55, 40, 55, 96, 7);
            Bar(px, 70, 36, 70, 90, 7);
            Bar(px, 85, 44, 85, 84, 7);
        }

        static void PaintDash(Color[] px)
        {
            for (int x = 0; x < N; x++)
            {
                if ((x / 16) % 2 == 0) continue;
                for (int y = 48; y < 80; y++) Plot(px, x, y);
            }
        }

        static void Disc(Color[] px, int cx, int cy, int r)
        {
            for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) Plot(px, x, y);
        }

        static void Ring(Color[] px, int cx, int cy, int r, int t)
        {
            int outer = r * r;
            int inner = (r - t) * (r - t);
            for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                int d = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (d <= outer && d >= inner) Plot(px, x, y);
            }
        }

        static void Bar(Color[] px, int x0, int y0, int x1, int y1, int t)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (steps < 1) steps = 1;
            for (int i = 0; i <= steps; i++)
            {
                int x = x0 + (x1 - x0) * i / steps;
                int y = y0 + (y1 - y0) * i / steps;
                Disc(px, x, y, t);
            }
        }

        static void Plot(Color[] px, int x, int y)
        {
            if ((uint)x >= N || (uint)y >= N) return;
            px[(y * N) + x] = Color.white;
        }
    }
}
