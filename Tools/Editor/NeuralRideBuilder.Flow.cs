using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// The order of Level 1 v6 (TASK_LEVEL1_V6_TRAIN, on v5's games and v4's explainers): every beat is Explain, Unfold,
    /// Play, and Play is a mini-game on the game table (draw your 3, spray one neuron's weights, send your 3 in, the
    /// hot-wire hunt, the training game, the bonus round) that ends with "STOP CLEAR!" and a pull of GO; between stops
    /// the rider zaps noise. One thread runs through it: the rider's own 3 and the network they train (Level1Session). An explain segment
    /// stops the pod (in the dark: its explainer holds the blackboard and keeps its region's pieces off) and plays its
    /// lines while the explainer follows their cues; it unfolds into the place on its last cue, and the play segment
    /// after it runs the moment (the puzzle, the forward pass, the blame, the drop). Fifteen segments over the three
    /// legs (see Layout), each with its region, its lines, its moment and which regions are switched on (the Quest
    /// 2's draw calls), plus the viewpoints the tools capture and check from. The segment numbers are the Seg*
    /// constants in NeuralRideBuilder.Explainers.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private struct SegmentSpec
        {
            public string Name;
            public int Region, Leg, Travel, Arrival, Mask;
            public Pose To;
            public float Cruise, MinSeconds, Hold;
            public RideMoment Moment;
        }

        private static List<SegmentSpec> Segments(Layout L, Moments m)
        {
            const int A = 0, B = 1, C = 2;
            int R(params int[] regions)
            {
                int mask = 0;
                foreach (int r in regions) mask |= 1 << r;
                return mask;
            }

            // Travel / Arrival are RideNarrationData.Sequences indices (-1: none).
            var list = new List<SegmentSpec>
            {
                // E1: through the page into the dark, explain the pixels, unfold the 7x7 into the field, then draw your own 3.
                new SegmentSpec { Name = "E1 pixels: explain, then draw your 3", Region = 1, Leg = A, To = L.GridStop, Cruise = 4f, MinSeconds = 5f, Travel = -1, Arrival = 0, Hold = 0.5f, Moment = m.Draw, Mask = R(0, 1) },
                new SegmentSpec { Name = "E1 to the neuron: zap the noise", Region = 1, Leg = A, To = L.NeuronStop, Cruise = 7f, MinSeconds = 6f, Travel = -1, Arrival = -1, Mask = R(1, 2) },
                // E2: one neuron, then spray its weights.
                new SegmentSpec { Name = "E2 neuron: explain", Region = 2, Leg = A, To = L.NeuronStop, Cruise = 3f, MinSeconds = 0f, Travel = -1, Arrival = 1, Mask = R(2) },
                new SegmentSpec { Name = "E2 neuron: your turn", Region = 2, Leg = A, To = L.NeuronStop, Cruise = 3f, MinSeconds = 0f, Travel = -1, Arrival = 2, Moment = m.Paint, Mask = R(2) },
                // E3: the network, then the rider sends their own 3 through it.
                new SegmentSpec { Name = "E3 network: explain", Region = 3, Leg = A, To = L.NetViewStop, Cruise = 8f, MinSeconds = 7f, Travel = -1, Arrival = 3, Mask = R(2, 3) },
                new SegmentSpec { Name = "E3 network: send your 3", Region = 3, Leg = A, To = L.NetViewStop, Cruise = 4f, MinSeconds = 0f, Travel = -1, Arrival = 4, Moment = m.Forward, Mask = R(3) },
                // E4: the guess (random: nothing is trained yet) and the loss.
                new SegmentSpec { Name = "E4 the guess and the loss", Region = 4, Leg = A, To = L.WrongStop, Cruise = 4f, MinSeconds = 0f, Travel = -1, Arrival = 5, Mask = R(3, 4) },
                // E5: backpropagation, then the rewind with the hot-wire hunt on the rider's 3.
                new SegmentSpec { Name = "E5 backprop: explain", Region = 5, Leg = B, To = L.NetViewStop, Cruise = 1.2f, MinSeconds = 0f, Travel = -1, Arrival = 6, Mask = R(3, 5) },
                new SegmentSpec { Name = "E5 backprop: rewind, hot-wire hunt", Region = 5, Leg = B, To = L.BlameStop, Cruise = 1.2f, MinSeconds = 6f, Travel = -1, Arrival = 7, Moment = m.Blame, Mask = R(3, 5) },
                new SegmentSpec { Name = "Back to the switch", Region = 5, Leg = B, To = L.SpurEnd, Cruise = 5f, MinSeconds = 5f, Travel = -1, Arrival = -1, Mask = R(3, 5) },
                // E6: gradient descent at the rim, then the training game (real training, live), then the plunge.
                new SegmentSpec { Name = "E6 descent: explain, then train", Region = 6, Leg = C, To = L.Rim, Cruise = 6f, MinSeconds = 8f, Travel = -1, Arrival = 8, Moment = m.Train, Mask = R(3, 6) },
                new SegmentSpec { Name = "E6 descent: the Valley", Region = 6, Leg = C, To = L.Rim, Cruise = 1f, MinSeconds = 0f, Travel = -1, Arrival = -1, Moment = m.Valley, Mask = R(6, 7) },
                // E7: Pixel reads the rider's own 3 with the network they trained (its moment says the lines), then the bonus round.
                // The climb out of the Valley is long and straight, so it runs quicker (noise to zap on the way).
                new SegmentSpec { Name = "E7 right: Pixel reads your 3", Region = 7, Leg = C, To = L.RightView, Cruise = 9f, MinSeconds = 6f, Travel = -1, Arrival = -1, Moment = m.Right, Mask = R(6, 7) },
                new SegmentSpec { Name = "Bonus: stump Pixel", Region = 7, Leg = C, To = L.RightView, Cruise = 1f, MinSeconds = 0f, Travel = -1, Arrival = -1, Moment = m.Stump, Mask = R(7) },
                // E8: tokens, then the end card.
                new SegmentSpec { Name = "E8 tokens: explain", Region = 7, Leg = C, To = L.EndStop, Cruise = 4f, MinSeconds = 5f, Travel = -1, Arrival = 9, Mask = R(7) },
            };

            if (list.Count != SegTokens + 1) throw new System.InvalidOperationException("Flow has " + list.Count + " segments but the Seg* constants expect " + (SegTokens + 1) + ".");
            return list;
        }

        private static void WireDirector(RideDirector rd, GameObject pod, Layout L, Moments moments, GameObject[] regions)
        {
            List<SegmentSpec> specs = Segments(L, moments);
            RidePath[] paths = { L.PathA, L.PathB, L.PathC };
            List<Vector3>[] legs = { L.LegA, L.LegB, L.LegC };
            string[] legNames = { "A: dock to the wrong guess", "B: the network's axis (rewind)", "C: branch, Valley, trained network" };
            float rimDistance = L.Dist(L.PathC, L.Rim.position);

            Ref(rd, "pod", pod.transform);
            Ref(rd, "controls", pod.GetComponent<RideControlSet>());
            Ref(rd, "script", _script);
            Set(rd, "legs", p =>
            {
                p.arraySize = legs.Length;
                for (int i = 0; i < legs.Length; i++)
                {
                    SerializedProperty e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("name").stringValue = legNames[i];
                    SerializedProperty pts = e.FindPropertyRelative("points");
                    pts.arraySize = legs[i].Count;
                    for (int k = 0; k < legs[i].Count; k++) pts.GetArrayElementAtIndex(k).vector3Value = legs[i][k];
                    e.FindPropertyRelative("gentleFrom").floatValue = i == 2 ? rimDistance : -1f;
                    e.FindPropertyRelative("gentleBaseY").floatValue = L.Rim.position.y;
                }
            });

            Set(rd, "segments", p =>
            {
                p.arraySize = specs.Count;
                for (int i = 0; i < specs.Count; i++)
                {
                    SegmentSpec s = specs[i];
                    SerializedProperty e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("name").stringValue = s.Name;
                    e.FindPropertyRelative("region").intValue = s.Region;
                    e.FindPropertyRelative("leg").intValue = s.Leg;
                    e.FindPropertyRelative("to").floatValue = L.Dist(paths[s.Leg], s.To.position);
                    e.FindPropertyRelative("cruise").floatValue = s.Cruise;
                    e.FindPropertyRelative("minSeconds").floatValue = s.MinSeconds;
                    e.FindPropertyRelative("travel").intValue = s.Travel;
                    e.FindPropertyRelative("arrival").intValue = s.Arrival;
                    e.FindPropertyRelative("moment").objectReferenceValue = s.Moment;
                    e.FindPropertyRelative("hold").floatValue = s.Hold;
                }
            });

            // Views that belong to the whole ride sit on the director's object (no renderers there).
            var dash = pod.GetComponent<DashScreenView>();
            var streamer = rd.gameObject.AddComponent<RegionStreamer>();
            Ref(streamer, "director", rd);
            Refs(streamer, "roots", regions);
            Int(streamer, "dockMask", 0b11);
            Set(streamer, "segmentMasks", p =>
            {
                p.arraySize = specs.Count;
                for (int i = 0; i < specs.Count; i++) p.GetArrayElementAtIndex(i).intValue = specs[i].Mask;
            });

            var atmosphere = rd.gameObject.AddComponent<RegionAtmosphere>();
            Ref(atmosphere, "director", rd);
            Ref(atmosphere, "theme", _theme);
            Ref(atmosphere, "grid", pod.transform.Find("Blackboard").GetComponent<Renderer>());

            var reveal = rd.gameObject.AddComponent<CueReveal>();
            Ref(reveal, "dash", dash);
            var entries = new List<(string cue, GameObject target, bool show)>
            {
                ("guess", regions[4].transform.Find("GuessSign").gameObject, true),
                ("accuracy", regions[7].transform.Find("AccuracySign").gameObject, true),
            };
            Set(reveal, "entries", p =>
            {
                p.arraySize = entries.Count;
                for (int i = 0; i < entries.Count; i++)
                {
                    SerializedProperty e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("cue").stringValue = entries[i].cue;
                    e.FindPropertyRelative("target").objectReferenceValue = entries[i].target;
                    e.FindPropertyRelative("show").boolValue = entries[i].show;
                }
            });

            BuildExplainers(rd.gameObject, rd, dash, atmosphere, regions);
        }

        /// <summary>Where the rider sits still: one marker per stop, plus the fly-over, the middle of the drop and the valley floor.</summary>
        private static void BuildViewpoints(Layout L, GameObject[] regions)
        {
            var root = new GameObject("Viewpoints");
            Transform Find(int region, string path) => regions[region].transform.Find(path);

            void Point(string label, int segment, Pose pose, string[] podPanels, params Transform[] targets)
            {
                var go = new GameObject(label);
                go.transform.SetParent(root.transform, false);
                go.transform.SetPositionAndRotation(pose.position, pose.rotation);
                var vp = go.AddComponent<RideViewpoint>();
                vp.label = label;
                vp.segment = segment;
                vp.targets = System.Array.FindAll(targets, t => t != null);
                vp.podPanels = podPanels ?? new string[0];
            }

            var valleyRotation = L.Rim.rotation;
            Point("dock", -1, new Pose(L.LegA[0], Quaternion.identity), null, Find(0, "Page"));
            Point("pixels", SegPixels, L.GridStop, new[] { "GameTable", "GameTable/DrawBoard" }, Find(1, "Explainer_E1/Stage/Title"));
            Point("pixel_flyover", SegOverPixels, L.GridApproach, null);
            Point("neuron", SegNeuronPlay, L.NeuronStop, new[] { "GameTable", "GameTable/WeightGrid" }, Find(2, "Panel"), Find(2, "Neuron"));
            Point("network", SegNetworkSend, L.NetViewStop, new[] { "GameTable", "GameTable/SendBoard" }, Find(3, "Network/Signs"), Find(3, "Network/Outputs"));
            Point("wrong", SegWrong, L.WrongStop, null, Find(4, "GuessSign"));
            Point("blame", SegRewind, L.BlameStop, new[] { "GameTable", "GameTable/HuntBoard" });
            Point("rim", SegRim, L.Rim, new[] { "GameTable", "GameTable/TrainBoard" });
            Point("valley_mid_drop", SegValley, new Pose(L.MidDrop, valleyRotation), null, Find(6, "Sign"));
            Point("valley_floor", SegValley, L.ValleyFloor, null);
            Point("right", SegRight, L.RightView, null, Find(7, "AccuracySign"));
            Point("stump", SegStump, L.RightView, new[] { "GameTable", "GameTable/DrawBoard" });
            Point("end", SegTokens, L.EndStop, new[] { "EndCard" });
        }
    }
}
