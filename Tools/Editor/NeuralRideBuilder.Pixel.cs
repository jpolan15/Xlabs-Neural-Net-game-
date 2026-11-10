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
            // Level 2's upgrades (owner playtest 2026-11-10): gold trim and camera goggles (it learned to see), a locked chat bubble.
            H("PixelGold", new Color(1f, 0.74f, 0.25f), shade: 0.75f, rim: 0.5f, rimColor: new Color(1f, 0.95f, 0.7f), emission: new Color(0.18f, 0.11f, 0.02f));
            H("PixelLens", new Color(0.05f, 0.12f, 0.2f), shade: 0.9f, rim: 0.8f, rimColor: _theme.cyan, emission: new Color(0.02f, 0.1f, 0.14f));
            H("PixelBubbleLocked", new Color(0.55f, 0.58f, 0.64f), shade: 0.8f, rim: 0.3f, rimColor: Color.white);
            H("PixelBubbleOpen", new Color(0.97f, 0.98f, 1f), shade: 0.9f, rim: 0.6f, rimColor: _theme.cyan, emission: new Color(0.1f, 0.25f, 0.3f));

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

        /// <summary>
        /// Pixel. In Level 1 (moments set) the buddy who learns to read; in Level 2 (_level2, moments null) the same robot one level on
        /// (owner playtest 2026-11-10: "a tiny bit buffed; it got vision, but still needs to unlock chatting"): gold side rings and
        /// gold-rimmed camera goggles over its eyes, and a grey chat bubble at its shoulder with a gold padlock that flies off on the
        /// finale's {unlock}. Level 2's meshes are saved as Pixel2_*, so Level 1's Pixel is untouched.
        /// </summary>
        private static PixelBuddyView BuildPixel(GameObject pod, RideDirector rd, DashScreenView dash, Moments moments)
        {
            bool upgraded = _level2;
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
                var ring = Prim(PrimitiveType.Cylinder, side < 0 ? "RingL" : "RingR", parts, new Vector3(side * 0.175f, -0.01f, 0f), upgraded ? new Vector3(0.19f, 0.028f, 0.19f) : new Vector3(0.17f, 0.022f, 0.17f), upgraded ? "PixelGold" : "PixelTrim");
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
            if (upgraded)
            {
                // Camera goggles: a gold rim and a dark lens behind each eye (the face draws on top), a bridge, and a band round the crown.
                for (int side = -1; side <= 1; side += 2)
                {
                    var rim = Prim(PrimitiveType.Cylinder, side < 0 ? "GoggleRimL" : "GoggleRimR", parts, new Vector3(side * 0.054f, 0.064f, 0.168f), new Vector3(0.088f, 0.008f, 0.088f), "PixelGold");
                    rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var lens = Prim(PrimitiveType.Cylinder, side < 0 ? "LensL" : "LensR", parts, new Vector3(side * 0.054f, 0.064f, 0.174f), new Vector3(0.072f, 0.006f, 0.072f), "PixelLens");
                    lens.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }

                Prim(PrimitiveType.Cube, "GoggleBridge", parts, new Vector3(0f, 0.07f, 0.172f), new Vector3(0.03f, 0.012f, 0.008f), "PixelGold");
                Prim(PrimitiveType.Sphere, "CrownBand", parts, new Vector3(0f, 0.115f, -0.01f), new Vector3(0.31f, 0.035f, 0.30f), "PixelGold");
            }

            Renderer glow = CombineByMaterial(parts, body.transform, upgraded ? "Pixel2" : "Pixel")["PixelGlow"];

            Transform chatLock = null;
            Renderer chatBubble = null;
            if (upgraded)
            {
                // The chat bubble at its shoulder, grey and padlocked: Pixel cannot talk yet.
                var chat = Make("Chat", body.transform, new Vector3(0.2f, 0.3f, 0.04f)).transform;
                chatBubble = Prim(PrimitiveType.Sphere, "Bubble", chat, Vector3.zero, new Vector3(0.17f, 0.12f, 0.05f), "PixelBubbleLocked").GetComponent<Renderer>();
                Prim(PrimitiveType.Sphere, "Tail", chat, new Vector3(-0.06f, -0.06f, 0f), new Vector3(0.045f, 0.045f, 0.035f), "PixelBubbleLocked");
                chatLock = Make("Lock", chat, new Vector3(0f, -0.005f, 0.03f)).transform;
                Prim(PrimitiveType.Cube, "LockBody", chatLock, new Vector3(0f, -0.012f, 0f), new Vector3(0.05f, 0.04f, 0.02f), "PixelGold");
                for (int side = -1; side <= 1; side += 2) Prim(PrimitiveType.Cylinder, side < 0 ? "ShackleL" : "ShackleR", chatLock, new Vector3(side * 0.016f, 0.017f, 0f), new Vector3(0.008f, 0.016f, 0.008f), "PixelGold");
                Prim(PrimitiveType.Cube, "ShackleTop", chatLock, new Vector3(0f, 0.033f, 0f), new Vector3(0.04f, 0.008f, 0.008f), "PixelGold");
                Prim(PrimitiveType.Cube, "Keyhole", chatLock, new Vector3(0f, -0.014f, 0.011f), new Vector3(0.008f, 0.016f, 0.002f), "PixelTrim");
            }

            // The face: two eyes (two strokes each), a mouth of four strokes, an open mouth, two cheeks. The view draws them.
            List<Shape> shapes = PixelBuddyView.FaceShapes(Mood.Happy, _theme.cyan);
            ShapeBatch face = Batch("Face", body.transform, shapes, "ExplainerFlat", 6);
            face.transform.localPosition = new Vector3(0f, 0.04f, upgraded ? 0.19f : 0.18f); // in front of the goggles' lenses

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
            Ref(view, "chatLock", chatLock);
            Ref(view, "chatBubble", chatBubble);
            Ref(view, "chatOpen", upgraded ? Mat("PixelBubbleOpen") : null);
            Ref(view, "body", body.transform);
            Set(view, "home", p => p.vector3Value = body.transform.localPosition);
            Ref(view, "face", face);
            Ref(view, "glow", glow);
            Ref(view, "pointer", pointer);
            Ref(view, "beeper", beeper);
            if (moments != null)
            {
                Ref(view, "paint", moments.Paint);
                Ref(view, "hintRing", pod.transform.Find("GameTable/WeightGrid/HintRing"));
                Ref(view, "blame", moments.Blame);
                Ref(view, "valley", moments.Valley);
            }

            PixelBuddyView.LineMood[] moods = upgraded ? PixelMoods2 : PixelMoods;
            Set(view, "lineMoods", p =>
            {
                p.arraySize = moods.Length;
                for (int i = 0; i < moods.Length; i++)
                {
                    SerializedProperty e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("line").intValue = (int)moods[i].line;
                    e.FindPropertyRelative("clause").intValue = moods[i].clause;
                    e.FindPropertyRelative("mood").enumValueIndex = (int)moods[i].mood;
                    e.FindPropertyRelative("atRider").boolValue = moods[i].atRider;
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
            PM(RideLine.MeetPixel, 1, Mood.Sad, true),          // "Pixel can't read a single one."
            PM(RideLine.PixelBrain, 0, Mood.Curious, true),
            PM(RideLine.PixelBrain, 2, Mood.Celebrate, true),   // "right here."
            PM(RideLine.PullGo, 0, Mood.Happy),
            PM(RideLine.PixelsSquares, 0, Mood.Curious),
            PM(RideLine.PixelsFortyNine, 2, Mood.Thinking, true), // "That's all the network ever sees."
            PM(RideLine.DrawYourTurn, 0, Mood.Happy, true),
            PM(RideLine.DrawBigger, 0, Mood.Confused, true),
            PM(RideLine.DrawDoneNudge, 0, Mood.Happy),
            PM(RideLine.DrawDone, 0, Mood.Celebrate, true),
            PM(RideLine.ZapIntro, 0, Mood.Happy, true),
            PM(RideLine.NeuronHolds, 0, Mood.Curious),
            PM(RideLine.NeuronAdds, 4, Mood.Happy),             // "High means the neuron fires."
            PM(RideLine.NeuronYourTurn, 0, Mood.Happy, true),
            PM(RideLine.NeuronLitForOther, 0, Mood.Confused, true),
            PM(RideLine.NeuronMissedThree, 0, Mood.Confused, true),
            PM(RideLine.NeuronSolved, 0, Mood.Celebrate, true),
            PM(RideLine.NetworkTeam, 0, Mood.Curious),
            PM(RideLine.NetworkWire, 1, Mood.Happy, true),       // "and that's a neural network."
            PM(RideLine.NetworkSend, 0, Mood.Happy),
            PM(RideLine.WrongSays, 0, Mood.Thinking),
            PM(RideLine.WrongSays, 1, Mood.Confused, true),      // "But look how unsure it is."
            PM(RideLine.WrongSays, 2, Mood.Sad, true),           // "Every weight is still random,"
            PM(RideLine.WrongLoss, 0, Mood.Thinking),
            PM(RideLine.BackWish, 0, Mood.Thinking),
            PM(RideLine.BackGrab, 0, Mood.Happy, true),
            PM(RideLine.BackHurry, 0, Mood.Confused),
            PM(RideLine.BackDone, 0, Mood.Celebrate, true),
            PM(RideLine.StopClear, 0, Mood.Celebrate, true),
            PM(RideLine.DescentHill, 0, Mood.Curious),
            PM(RideLine.TrainYourTurn, 0, Mood.Happy, true),
            PM(RideLine.TrainTooBig, 0, Mood.Confused, true),
            PM(RideLine.TrainTooSmall, 0, Mood.Sad),
            PM(RideLine.TrainGood, 0, Mood.Happy, true),
            PM(RideLine.TrainDone, 0, Mood.Celebrate, true),
            PM(RideLine.RightYours, 0, Mood.Thinking),
            PM(RideLine.RightYes, 0, Mood.Celebrate, true),
            PM(RideLine.RightStudy, 0, Mood.Thinking, true),
            PM(RideLine.RightNowYes, 0, Mood.Celebrate, true),
            PM(RideLine.RightAccuracy, 0, Mood.Happy, true),
            PM(RideLine.RightLoop, 0, Mood.Happy),
            PM(RideLine.StumpYourTurn, 0, Mood.Happy, true),
            PM(RideLine.StumpRight, 0, Mood.Celebrate, true),
            PM(RideLine.StumpWrong, 0, Mood.Confused, true),
            PM(RideLine.StumpDone, 0, Mood.Celebrate, true),
            PM(RideLine.TokensWords, 0, Mood.Curious),
            PM(RideLine.TokensNext, 0, Mood.Celebrate, true),
        };

        // Level 2's Pixel (clauses follow Level2NarrationData's [pX] marks): it can see, it is sad its chat is locked, it cheers the
        // rider's wins, and it celebrates when chat unlocks.
        private static readonly PixelBuddyView.LineMood[] PixelMoods2 =
        {
            PM(RideLine.L2Hello, 4, Mood.Celebrate, true),  // "Pixel can see now!"
            PM(RideLine.L2Hello, 5, Mood.Sad, true),        // "But its chat is still locked."
            PM(RideLine.L2Hello, 7, Mood.Happy, true),      // "we unlock it."
            PM(RideLine.PullGo, 0, Mood.Happy),
            PM(RideLine.TokChop, 0, Mood.Curious),
            PM(RideLine.TokGlue, 0, Mood.Thinking),
            PM(RideLine.TokDone, 0, Mood.Celebrate, true),
            PM(RideLine.EmbName, 0, Mood.Curious),
            PM(RideLine.EmbMath, 0, Mood.Thinking),
            PM(RideLine.EmbWrong, 0, Mood.Confused, true),
            PM(RideLine.EmbDone, 0, Mood.Celebrate, true),
            PM(RideLine.AttMeaning, 0, Mood.Curious),
            PM(RideLine.AttWrong, 0, Mood.Confused, true),
            PM(RideLine.AttDone, 0, Mood.Celebrate, true),
            PM(RideLine.PredJob, 0, Mood.Thinking),
            PM(RideLine.PredBeat, 0, Mood.Celebrate, true),
            PM(RideLine.PredMiss, 0, Mood.Confused, true),
            PM(RideLine.PredDone, 0, Mood.Happy, true),
            PM(RideLine.GenLoop, 0, Mood.Curious),
            PM(RideLine.GenWild, 0, Mood.Celebrate, true),
            PM(RideLine.GenDone, 0, Mood.Happy, true),
            PM(RideLine.OutTalk, 0, Mood.Happy, true),
            PM(RideLine.OutCareful, 0, Mood.Thinking, true),
            PM(RideLine.OutEnd, 0, Mood.Celebrate, true),
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
