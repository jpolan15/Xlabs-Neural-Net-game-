using System.Collections.Generic;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Where everything in Level 1 sits. One switchback track in three legs:
    /// Leg A runs from the dock through the Page, the Grid and the Neuron stop, then an S-curve (at most about 23°
    /// of yaw, eased) to the switch J and on to the stop 30 m in front of the network, a giant diagram facing the rider
    /// (49 pixels on the left, 16 hidden in the middle, 10 answers on the right). Leg B is the straight line through J
    /// from a dead-end spur behind it to that stop: the cart rolls backward along it while the error flows back (the
    /// rider keeps facing the network while it recedes). Leg C leaves the spur forward through J, turns 40° right
    /// (eased over 30 m, about 6°/s) past the right edge of the diagram, runs to the Valley's rim, follows the real
    /// training path down the Valley (height = real loss), climbs back up and ends in front of the trained network. All distances come from here, so moving a region
    /// moves its stops, viewpoints and captures with it.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private sealed class Layout
        {
            // Network: one diagram in the plane z = 0 of its pose, seen from ViewDistance (NeuralRideBuilder.Network).
            public const float ViewDistance = 30f;

            // Valley: one hop is this long; the ribbon's columns are this far apart; loss to meters.
            public const float HopLength = 7f;
            public const float ColumnSpacing = 2.2f;
            public const float MetersPerLoss = 5f;
            public const float Clearance = 0.4f;
            public const float BranchYaw = 40f;

            public readonly List<Vector3> LegA = new List<Vector3>();
            public readonly List<Vector3> LegB = new List<Vector3>();
            public readonly List<Vector3> LegC = new List<Vector3>();

            public Pose PageAnchor, GridAnchor, NeuronStop, GridStop, GridApproach, NetViewStop, WrongStop, BlameStop, SpurEnd, Junction;
            public Pose BlueNetwork, Rim, ValleyFloor, RightView, EndStop, GoldNetwork;
            public Vector3 MidDrop;

            public int RimIndex;
            public float RowSpacing;
            public int Rows, Columns;
            public float StartLoss;
            public float[] HopDistances;

            public RidePath PathA, PathB, PathC;

            public float Dist(RidePath path, Vector3 p) => path.ClosestDistance(p);

            public static Layout Make(Level1Data data)
            {
                var L = new Layout();
                Quaternion straight = Quaternion.identity;
                Quaternion branch = Quaternion.Euler(0f, BranchYaw, 0f);

                // ----- Leg A
                for (float z = 0f; z <= 84f; z += 6f) L.LegA.Add(new Vector3(0f, 0f, z));
                for (int i = 1; i <= 15; i++)
                {
                    float t = i / 15f;
                    L.LegA.Add(new Vector3(6f * (1f - Mathf.Cos(Mathf.PI * t)), 0f, 84f + 45f * t));
                }

                foreach (float z in new[] { 135f, 141f, 145f }) L.LegA.Add(new Vector3(12f, 0f, z));

                L.PageAnchor = new Pose(new Vector3(0f, 0f, 10f), straight);
                L.GridAnchor = new Pose(new Vector3(0f, -3.5f, 30f), straight);
                L.GridStop = new Pose(new Vector3(0f, 0f, 36f), straight);
                L.GridApproach = new Pose(new Vector3(0f, 0f, 13f), straight);
                L.NeuronStop = new Pose(new Vector3(0f, 0f, 80f), straight);
                L.Junction = new Pose(new Vector3(12f, 0f, 129f), straight);
                L.NetViewStop = new Pose(new Vector3(12f, 0f, 145f), straight);
                L.BlueNetwork = new Pose(new Vector3(12f, 0f, 145f + ViewDistance), straight);
                L.WrongStop = L.NetViewStop;
                L.BlameStop = new Pose(new Vector3(12f, 0f, 135f), straight);
                L.SpurEnd = new Pose(new Vector3(12f, 0f, 112f), straight);

                // ----- Leg B: the straight through J, spur end to the stop in front of the network
                foreach (float z in new[] { 112f, 118f, 124f, 129f, 135f, 141f, 145f }) L.LegB.Add(new Vector3(12f, 0f, z));

                // ----- Leg C: spur, J, the branch, the rim, the Valley, the climb, the trained network
                foreach (float z in new[] { 112f, 118f, 124f, 129f }) L.LegC.Add(new Vector3(12f, 0f, z));
                Vector3 p = new Vector3(12f, 0f, 129f);
                float yaw = 0f;
                const float curveLength = 30f;
                for (float s = 0f; s < curveLength; s += 0.1f)
                {
                    float u = Mathf.Clamp01((s + 0.05f) / curveLength);
                    yaw = BranchYaw * u * u * (3f - 2f * u);
                    p += Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 0.1f;
                    if (Mathf.Repeat(s + 0.1f, 2f) < 0.1f) L.LegC.Add(p);
                }

                Vector3 forward = branch * Vector3.forward;
                for (int i = 1; i <= 10; i++) L.LegC.Add(p + forward * (3f * i));
                Vector3 rim = p + forward * 30f;
                L.Rim = new Pose(rim, branch);

                Level1Valley valley = data.File.valley;
                L.Rows = valley.rows;
                L.Columns = valley.columns;
                L.RowSpacing = HopLength / valley.samplesPerHop;
                int centre = valley.columns / 2;
                L.StartLoss = (float)data.ValleyLoss(0, centre);
                L.RimIndex = L.LegC.Count;
                for (int r = 0; r < valley.rows; r++)
                {
                    float y = ((float)data.ValleyLoss(r, centre) - L.StartLoss) * MetersPerLoss;
                    L.LegC.Add(rim + forward * (r * L.RowSpacing) + Vector3.up * y);
                }

                Vector3 floor = L.LegC[L.LegC.Count - 1];
                L.ValleyFloor = new Pose(floor, branch);
                Level1Hop[] hops = valley.hops;
                int mid = Mathf.Min(hops.Length - 1, 3);
                L.MidDrop = L.LegC[L.RimIndex + hops[mid].row];

                // Climb back to the rim's height over 48 m, then the trained network.
                const float climb = 48f;
                for (float s = 3f; s <= climb; s += 3f)
                {
                    float u = s / climb;
                    float y = Mathf.Lerp(floor.y, 0f, u * u * (3f - 2f * u));
                    Vector3 q = floor + forward * s;
                    q.y = y;
                    L.LegC.Add(q);
                }

                Vector3 top = floor + forward * climb;
                top.y = 0f;
                Vector3 view = top + forward * 6f;
                L.RightView = new Pose(view, branch);
                L.GoldNetwork = new Pose(view + forward * ViewDistance, branch);
                L.EndStop = new Pose(view + forward * 10f, branch);
                for (float s = 3f; s < 16f - 0.01f; s += 3f) L.LegC.Add(top + forward * s);
                L.LegC.Add(L.EndStop.position);

                L.PathA = new RidePath(L.LegA);
                L.PathB = new RidePath(L.LegB);
                L.PathC = new RidePath(L.LegC);
                L.HopDistances = new float[hops.Length];
                for (int k = 0; k < hops.Length; k++) L.HopDistances[k] = L.PathC.DistanceAtPoint(L.RimIndex + hops[k].row);
                return L;
            }

            /// <summary>World position of ribbon row r, column c of the Valley at its real loss (before the clearance).</summary>
            public Vector3 Ribbon(Level1Data data, int r, int c)
            {
                float y = ((float)data.ValleyLoss(r, c) - StartLoss) * MetersPerLoss;
                return Rim.position + Rim.rotation * new Vector3((c - Columns / 2) * ColumnSpacing, y, r * RowSpacing);
            }
        }
    }
}
