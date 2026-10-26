using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;
using Convergence.Presentation.Ride.Explainers;
using Mood = Convergence.Presentation.Ride.PixelBuddyView.Mood;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Pixel, the buddy (TASK_LEVEL1_V4_EXPLAINERS WP2): our own little robot from ArtSource/scout_drone/concept/sheet.png,
    /// porcelain-white egg body, dark face screen with a cyan face drawn by a ShapeBatch, two side rings, two antennae
    /// with glowing tips, a thruster glow underneath. Built from primitives and combined into one mesh per material
    /// (4 draws, plus the face and the pointer), under Pod/Pixel so it rides with the rider. PixelBuddyView animates it.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private static void MakePixelMaterials()
        {
            H("PixelShell", new Color(0.93f, 0.94f, 0.96f), shade: 0.8f, rim: 0.45f, rimColor: _theme.cyan);
            H("PixelTrim", new Color(0.30f, 0.33f, 0.38f), shade: 0.8f, rim: 0.35f, rimColor: _theme.cyan);
            H("PixelScreen", new Color(0.03f, 0.06f, 0.10f), shade: 0.9f, rim: 0.6f, rimColor: _theme.cyan, emission: new Color(0.0f, 0.05f, 0.08f));
            H("PixelGlow", _theme.cyan, shade: 1f, rim: 0.2f, rimColor: Color.white, emission: _theme.cyan);

            // The pointer: a thin line whose colour (YELLOW, "look here") and fade come from its vertex colours.
            string path = MatDir + "/PixelPointer.mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }

            m.shader = shader;
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            Mats["PixelPointer"] = m;
        }

        private static PixelBuddyView BuildPixel(GameObject pod, RideDirector rd, DashScreenView dash, Moments moments)
        {
            MakePixelMaterials();
            var root = Make("Pixel", pod.transform, Vector3.zero);
            var body = Make("Body", root.transform, new Vector3(0.98f, 0.95f, 1.02f));

            body.transform.localRotation = Quaternion.LookRotation(EyePoint - body.transform.localPosition);

            // Parts, in the body's space (+z = the face, toward whatever Pixel looks at). About 0.45 m tall.
            var parts = Make("Parts", body.transform, Vector3.zero).transform;
            Prim(PrimitiveType.Sphere, "Shell", parts, new Vector3(0f, 0f, 0f), new Vector3(0.34f, 0.40f, 0.32f), "PixelShell");
            Prim(PrimitiveType.Sphere, "Crown", parts, new Vector3(0f, 0.12f, -0.01f), new Vector3(0.30f, 0.20f, 0.29f), "PixelShell");
            Prim(PrimitiveType.Sphere, "Belly", parts, new Vector3(0f, -0.13f, 0.02f), new Vector3(0.22f, 0.12f, 0.24f), "PixelTrim");
            Prim(PrimitiveType.Sphere, "Screen", parts, new Vector3(0f, 0.04f, 0.135f), new Vector3(0.27f, 0.22f, 0.08f), "PixelScreen");
            for (int side = -1; side <= 1; side += 2)
            {
                var ring = Prim(PrimitiveType.Cylinder, side < 0 ? "RingL" : "RingR", parts, new Vector3(side * 0.175f, -0.01f, 0f), new Vector3(0.17f, 0.022f, 0.17f), "PixelTrim");
                ring.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                var hub = Prim(PrimitiveType.Cylinder, side < 0 ? "HubL" : "HubR", parts, new Vector3(side * 0.19f, -0.01f, 0f), new Vector3(0.11f, 0.02f, 0.11f), "PixelGlow");
                hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                var cap = Prim(PrimitiveType.Cylinder, side < 0 ? "CapL" : "CapR", parts, new Vector3(side * 0.205f, -0.01f, 0f), new Vector3(0.06f, 0.012f, 0.06f), "PixelShell");
                cap.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

                var stalk = Prim(PrimitiveType.Cylinder, side < 0 ? "AntennaL" : "AntennaR", parts, new Vector3(side * 0.075f, 0.255f, -0.02f), new Vector3(0.012f, 0.05f, 0.012f), "PixelTrim");
                stalk.transform.localRotation = Quaternion.Euler(0f, 0f, -side * 14f);
                Prim(PrimitiveType.Sphere, side < 0 ? "TipL" : "TipR", parts, new Vector3(side * 0.1f, 0.305f, -0.02f), Vector3.one * 0.034f, "PixelGlow");
                Prim(PrimitiveType.Sphere, side < 0 ? "ChestL" : "ChestR", parts, new Vector3(side * 0.065f, -0.105f, 0.135f), new Vector3(0.03f, 0.036f, 0.02f), "PixelGlow");
            }

            Prim(PrimitiveType.Sphere, "Thruster", parts, new Vector3(0f, -0.2f, 0f), new Vector3(0.1f, 0.035f, 0.1f), "PixelGlow");
            Renderer glow = CombineByMaterial(parts, body.transform, "Pixel")["PixelGlow"];

            // The face: two eyes (two strokes each), a mouth of four strokes, an open mouth, two cheeks. The view draws them.
            List<Shape> shapes = PixelBuddyView.FaceShapes(Mood.Happy, _theme.cyan);
            ShapeBatch face = Batch("Face", body.transform, shapes, "ExplainerFlat", 6);
            face.transform.localPosition = new Vector3(0f, 0.04f, 0.18f);

            var pointer = Make("Pointer", root.transform, Vector3.zero).AddComponent<LineRenderer>();
            pointer.sharedMaterial = Mat("PixelPointer");
            pointer.useWorldSpace = true;
            pointer.positionCount = 2;
            pointer.widthMultiplier = 0.01f;
            pointer.numCapVertices = 4;
            pointer.shadowCastingMode = ShadowCastingMode.Off;
            pointer.receiveShadows = false;
            pointer.enabled = false;

            var beeper = body.AddComponent<AudioSource>();
            beeper.playOnAwake = false;
            beeper.spatialBlend = 0.6f;
            beeper.volume = 0.35f;
            beeper.dopplerLevel = 0f;

            var view = root.AddComponent<PixelBuddyView>();
            Ref(view, "director", rd);
            Ref(view, "dash", dash);
            Ref(view, "theme", _theme);
            Ref(view, "dock", rd.GetComponentInChildren<DockExplainer>(true));
            Ref(view, "body", body.transform);
            Set(view, "home", p => p.vector3Value = body.transform.localPosition);
            Ref(view, "face", face);
            Ref(view, "glow", glow);
            Ref(view, "pointer", pointer);
            Ref(view, "beeper", beeper);
            Ref(view, "paint", moments.Paint);
            Ref(view, "hintRing", pod.transform.Find("GameTable/WeightGrid/HintRing"));
            Ref(view, "blame", moments.Blame);
            Ref(view, "valley", moments.Valley);
            Set(view, "lineMoods", p =>
            {
                p.arraySize = PixelMoods.Length;
                for (int i = 0; i < PixelMoods.Length; i++)
                {
                    SerializedProperty e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("line").intValue = (int)PixelMoods[i].line;
                    e.FindPropertyRelative("clause").intValue = PixelMoods[i].clause;
                    e.FindPropertyRelative("mood").enumValueIndex = (int)PixelMoods[i].mood;
                    e.FindPropertyRelative("atRider").boolValue = PixelMoods[i].atRider;
                }
            });
            body.SetActive(true);
            return view;
        }

        private static PixelBuddyView.LineMood PM(RideLine line, int clause, Mood mood, bool atRider = false)
        {
            return new PixelBuddyView.LineMood { line = line, clause = clause, mood = mood, atRider = atRider };
        }

        // How Pixel feels through the story: it is the one learning, so it is lost at first, sad when wrong, and
        // proud at the end. Clause numbers follow the [pX] marks of RideNarrationData.
        private static readonly PixelBuddyView.LineMood[] PixelMoods =
        {
            PM(RideLine.MeetPixel, 0, Mood.Happy, true),
            PM(RideLine.MeetPixel, 2, Mood.Sad, true),          // "and Pixel can't read yet."
            PM(RideLine.PixelBrain, 0, Mood.Curious, true),
            PM(RideLine.PixelBrain, 1, Mood.Celebrate, true),   // "and teach it to read threes."
            PM(RideLine.PullGo, 0, Mood.Happy),
            PM(RideLine.PixelsSquares, 0, Mood.Curious),
            PM(RideLine.PixelsFortyNine, 2, Mood.Thinking, true), // "It's all Pixel ever sees."
            PM(RideLine.DrawYourTurn, 0, Mood.Happy, true),
            PM(RideLine.DrawDone, 0, Mood.Celebrate, true),
            PM(RideLine.ZapIntro, 0, Mood.Happy, true),
            PM(RideLine.NeuronHolds, 0, Mood.Curious),
            PM(RideLine.NeuronAdds, 1, Mood.Happy),
            PM(RideLine.NeuronYourTurn, 0, Mood.Happy, true),
            PM(RideLine.NeuronLitForOther, 0, Mood.Confused, true),
            PM(RideLine.NeuronMissedThree, 0, Mood.Confused, true),
            PM(RideLine.NeuronSolved, 0, Mood.Celebrate, true),
            PM(RideLine.NetworkTeam, 0, Mood.Curious),
            PM(RideLine.NetworkWire, 2, Mood.Happy, true),       // "This is Pixel's brain!"
            PM(RideLine.NetworkSend, 0, Mood.Happy),
            PM(RideLine.WrongSays, 0, Mood.Thinking),
            PM(RideLine.WrongRandom, 0, Mood.Sad, true),
            PM(RideLine.WrongLoss, 0, Mood.Confused),
            PM(RideLine.PlugYourTurn, 0, Mood.Happy, true),
            PM(RideLine.PlugWrong, 0, Mood.Confused, true),
            PM(RideLine.PlugDone, 0, Mood.Celebrate, true),
            PM(RideLine.PlugTooMany, 0, Mood.Thinking),
            PM(RideLine.BackWish, 0, Mood.Thinking),
            PM(RideLine.BackGrab, 0, Mood.Happy, true),
            PM(RideLine.BackDone, 0, Mood.Celebrate, true),
            PM(RideLine.DescentHill, 0, Mood.Curious),
            PM(RideLine.DescentYourTurn, 0, Mood.Happy, true),
            PM(RideLine.DescentGameOvershoot, 0, Mood.Confused, true),
            PM(RideLine.DescentRealHill, 0, Mood.Celebrate, true),
            PM(RideLine.StopClear, 0, Mood.Celebrate, true),
            PM(RideLine.DescentOvershoot, 0, Mood.Confused),
            PM(RideLine.DescentSettle, 0, Mood.Happy),
            PM(RideLine.RightSays, 0, Mood.Thinking),
            PM(RideLine.RightSays, 3, Mood.Celebrate, true),     // "three!"
            PM(RideLine.RightTraining, 0, Mood.Happy, true),
            PM(RideLine.RightAccuracy, 0, Mood.Celebrate, true),
            PM(RideLine.TokensWords, 0, Mood.Curious),
            PM(RideLine.TokensNext, 0, Mood.Celebrate, true),
        };

        /// <summary>Combines a hierarchy of primitives into one mesh per material under <paramref name="into"/>, and removes them.</summary>
        private static Dictionary<string, Renderer> CombineByMaterial(Transform parts, Transform into, string prefix)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            Matrix4x4 toLocal = into.worldToLocalMatrix;
            foreach (MeshRenderer r in parts.GetComponentsInChildren<MeshRenderer>())
            {
                Material m = r.sharedMaterial;
                if (!groups.TryGetValue(m, out var list)) groups[m] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = r.GetComponent<MeshFilter>().sharedMesh, transform = toLocal * r.transform.localToWorldMatrix });
            }

            var made = new Dictionary<string, Renderer>();
            foreach (var g in groups)
            {
                var mesh = new Mesh { name = prefix + "_" + g.Key.name };
                mesh.CombineMeshes(g.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                var go = Make(prefix + "_" + g.Key.name, into, Vector3.zero);
                go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(mesh, mesh.name);
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = g.Key;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                made[g.Key.name] = r;
            }

            Object.DestroyImmediate(parts.gameObject);
            return made;
        }
    }
}
