using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// The order of Level 1: ten segments over the three legs (see Layout), each with its region, its lines and its
    /// moment, plus which regions are switched on in each (the Quest 2's draw calls) and the viewpoints the tools
    /// capture and check from.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private const int NeuronSegment = 1;

        private struct SegmentSpec
        {
            public string Name;
            public int Region, Leg, Travel, Arrival, Mask;
            public Pose To;
            public float Cruise, MinSeconds;
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

            return new List<SegmentSpec>
            {
                new SegmentSpec { Name = "Into the picture", Region = 1, Leg = A, To = L.GridStop, Cruise = 3.5f, MinSeconds = 10f, Travel = 0, Arrival = -1, Mask = R(0, 1, 2) },
                new SegmentSpec { Name = "One neuron", Region = 2, Leg = A, To = L.NeuronStop, Cruise = 3f, MinSeconds = 8f, Travel = 1, Arrival = 2, Moment = m.Paint, Mask = R(1, 2) },
                new SegmentSpec { Name = "To the network", Region = 3, Leg = A, To = L.NetViewStop, Cruise = 4.5f, MinSeconds = 10f, Travel = 3, Arrival = 4, Moment = m.Forward, Mask = R(2, 3) },
                new SegmentSpec { Name = "Wrong", Region = 4, Leg = A, To = L.WrongStop, Cruise = 4f, MinSeconds = 0f, Travel = -1, Arrival = 5, Mask = R(3, 4) },
                new SegmentSpec { Name = "Rewind", Region = 5, Leg = B, To = L.BlameStop, Cruise = 1.2f, MinSeconds = 8f, Travel = 6, Arrival = 7, Moment = m.Blame, Mask = R(3, 5) },
                new SegmentSpec { Name = "Back to the switch", Region = 5, Leg = B, To = L.SpurEnd, Cruise = 3.5f, MinSeconds = 7f, Travel = -1, Arrival = -1, Mask = R(3, 5) },
                new SegmentSpec { Name = "To the rim", Region = 6, Leg = C, To = L.Rim, Cruise = 4.5f, MinSeconds = 10f, Travel = 8, Arrival = 9, Mask = R(3, 6) },
                new SegmentSpec { Name = "The Valley", Region = 6, Leg = C, To = L.Rim, Cruise = 1f, MinSeconds = 0f, Travel = -1, Arrival = -1, Moment = m.Valley, Mask = R(6, 7) },
                new SegmentSpec { Name = "Right", Region = 7, Leg = C, To = L.RightView, Cruise = 4f, MinSeconds = 8f, Travel = 10, Arrival = -1, Moment = m.Right, Mask = R(6, 7) },
                new SegmentSpec { Name = "Words", Region = 7, Leg = C, To = L.EndStop, Cruise = 3f, MinSeconds = 10f, Travel = 11, Arrival = -1, Mask = R(7) },
            };
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

            var reveal = rd.gameObject.AddComponent<CueReveal>();
            Ref(reveal, "dash", dash);
            var entries = new List<(string cue, GameObject target, bool show)>
            {
                ("guess", regions[4].transform.Find("GuessSign").gameObject, true),
                ("accuracy", regions[7].transform.Find("AccuracySign").gameObject, true),
                ("words", regions[7].transform.Find("Words").gameObject, true),
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
        }

        /// <summary>Where the rider sits still: one marker per stop, plus the middle of the drop and the valley floor.</summary>
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
            Point("grid_approach", 0, L.GridApproach, null, Find(1, "PixelField"));
            Point("grid", 0, L.GridStop, null, Find(1, "InputGrid7/Title"));
            Point("neuron", 1, L.NeuronStop, new[] { "Dash/WeightGrid" }, Find(2, "Panel"), Find(2, "Neuron"));
            Point("network", 2, L.NetViewStop, null, Find(3, "Network/Signs"), Find(3, "Network/Outputs"));
            Point("wrong", 3, L.WrongStop, null, Find(4, "GuessSign"));
            Point("blame", 4, L.BlameStop, null);
            Point("rim", 6, L.Rim, null);
            Point("valley_mid_drop", 7, new Pose(L.MidDrop, valleyRotation), null, Find(6, "Sign"));
            Point("valley_floor", 7, L.ValleyFloor, null);
            Point("right", 8, L.RightView, null, Find(7, "AccuracySign"));
            Point("finale_words", 9, new Pose(L.GoldNetwork.position - L.GoldNetwork.rotation * new Vector3(0f, 0f, 25f), L.GoldNetwork.rotation), null, Find(7, "Words"));
            Point("end", 9, L.EndStop, new[] { "EndCard" });
        }
    }
}
