using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Level 1's place (owner, 2026-11-17: "keep it night, but give it a scene: a night festival in a town, lanterns,
    /// lamp posts, everything lit up"). The track becomes an elevated festival tram line above a town street 7 m below
    /// (the sky's grid floor sits at the same depth): wooden supports, lamp posts beside the rails with paper lanterns on
    /// strings between them, lantern canopies overhead between stops, a vermilion festival gate before every stop with
    /// the stop's number and name, food stalls and lit houses on the street, the pixel field on a stage, and sky
    /// lanterns rising into the night (the moon is in the sky shader). Each stop keeps its topic colour as its lantern
    /// colour, mixed with festival red and warm white.
    /// Budget (Quest 2): per region one opaque mesh (Convergence/Festival, vertex colours, no textures) and one additive
    /// mesh of glows (Convergence/FestivalGlow, billboards in the shader); the risers are one more glow mesh on the Track.
    /// Nothing solid stands in the box the rider looks through at a stop (RideSightlines checks it on every build).
    /// Everything is placed from the Layout, so moving a stop moves its gate and keep-clear zone with it.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        /// <summary>The town street, metres (the track is at 0, the sky's floor 7 m under a 1.2 m eye).</summary>
        private const float StreetY = -5.8f;

        private static readonly Color FestivalRed = new Color(1f, 0.30f, 0.18f);
        private static readonly Color WarmWhite = new Color(1f, 0.86f, 0.62f);
        private static readonly Color Vermilion = new Color(0.78f, 0.17f, 0.10f);
        private static readonly Color DarkWood = new Color(0.16f, 0.10f, 0.08f);
        private static readonly Color RoofTile = new Color(0.07f, 0.065f, 0.09f);
        private static readonly Color Lacquer = new Color(0.05f, 0.04f, 0.04f);

        /// <summary>Each region's lantern colour: its topic's colour (the dock is a classic festival red).</summary>
        private static Color TopicLantern(int region)
        {
            switch (region)
            {
                case 1: return new Color(0.37f, 0.91f, 1f);
                case 2: return new Color(1f, 0.71f, 0.28f);
                case 3: return new Color(0.43f, 0.71f, 1f);
                case 4: return new Color(1f, 0.33f, 0.36f);
                case 5: return new Color(0.88f, 0.44f, 1f);
                case 6: return new Color(1f, 0.50f, 0.63f);
                case 7: return new Color(1f, 0.82f, 0.40f);
                default: return FestivalRed;
            }
        }

        // ---------- mesh kits ----------

        /// <summary>Opaque festival geometry: vertex colour rgb, alpha = glow (0 matte wood/roofs, 1 lantern paper, lit windows).</summary>
        private sealed class FestivalMesh
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Color> C = new List<Color>();
            public readonly List<int> T = new List<int>();

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
            {
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                int i = V.Count;
                V.AddRange(new[] { a, b, c, d });
                for (int k = 0; k < 4; k++)
                {
                    N.Add(n);
                    C.Add(color);
                }

                T.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Color color)
            {
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                int i = V.Count;
                V.AddRange(new[] { a, b, c });
                for (int k = 0; k < 3; k++)
                {
                    N.Add(n);
                    C.Add(color);
                }

                T.AddRange(new[] { i, i + 1, i + 2 });
            }

            /// <summary>A box centred at c, rotated, without a bottom face unless asked.</summary>
            public void Box(Vector3 c, Vector3 size, Quaternion rot, Color color, bool bottom = false)
            {
                Vector3 h = size * 0.5f;
                Vector3 P(float x, float y, float z) => c + rot * new Vector3(x * h.x, y * h.y, z * h.z);
                Quad(P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), color);
                Quad(P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1), color);
                Quad(P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), P(-1, -1, 1), color);
                Quad(P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1), color);
                Quad(P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1), color);
                if (bottom) Quad(P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), color);
            }

            /// <summary>A paper lantern (chōchin): a ribbed barrel of glowing paper with dark lacquer caps.</summary>
            public void Lantern(Vector3 c, float radius, float height, Color paper)
            {
                const int sides = 6, bands = 4;
                Color glow = new Color(paper.r, paper.g, paper.b, 1f);
                var rings = new Vector3[bands + 1][];
                for (int k = 0; k <= bands; k++)
                {
                    float t = k / (float)bands;
                    float r = radius * (0.55f + 0.45f * Mathf.Sin(Mathf.PI * t));
                    float y = (t - 0.5f) * height;
                    rings[k] = new Vector3[sides];
                    for (int s = 0; s < sides; s++)
                    {
                        float a = s * Mathf.PI * 2f / sides;
                        rings[k][s] = c + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                    }
                }

                for (int k = 0; k < bands; k++)
                {
                    Color band = k % 2 == 0 ? glow : new Color(glow.r * 0.82f, glow.g * 0.82f, glow.b * 0.82f, 1f);
                    for (int s = 0; s < sides; s++)
                    {
                        int n = (s + 1) % sides;
                        Quad(rings[k][s], rings[k + 1][s], rings[k + 1][n], rings[k][n], band);
                    }
                }

                // Caps: a dark rim fan at each end.
                Vector3 top = c + Vector3.up * (height * 0.5f + 0.03f), bottom = c - Vector3.up * (height * 0.5f + 0.03f);
                for (int s = 0; s < sides; s++)
                {
                    int n = (s + 1) % sides;
                    Tri(top, rings[bands][n] + Vector3.up * 0.03f, rings[bands][s] + Vector3.up * 0.03f, Lacquer);
                    Tri(bottom, rings[0][s] - Vector3.up * 0.03f, rings[0][n] - Vector3.up * 0.03f, Lacquer);
                }
            }

            /// <summary>A thin cord sagging between two points; returns points along it for hanging lanterns.</summary>
            public Vector3 Cord(Vector3 a, Vector3 b, float sag, float t)
            {
                Vector3 p = Vector3.Lerp(a, b, t);
                p.y -= sag * 4f * t * (1f - t);
                return p;
            }

            public void CordMesh(Vector3 a, Vector3 b, float sag, Color color)
            {
                const int pieces = 8;
                for (int i = 0; i < pieces; i++)
                {
                    Vector3 p = Cord(a, b, sag, i / (float)pieces), q = Cord(a, b, sag, (i + 1) / (float)pieces);
                    Vector3 side = Vector3.Cross((q - p).normalized, Vector3.up).normalized * 0.025f;
                    Vector3 up = Vector3.up * 0.025f;
                    Quad(p - side, q - side, q + side, p + side, color);
                    Quad(p - up, q - up, q + up, p + up, color);
                }
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = V.Count > 60000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(V);
                mesh.SetNormals(N);
                mesh.SetColors(C);
                mesh.SetTriangles(T, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        /// <summary>Additive glows: camera-facing halos (size > 0), flat light pools, risers, the moon disc (see FestivalGlow.shader).</summary>
        private sealed class GlowMesh
        {
            private readonly List<Vector3> _v = new List<Vector3>();
            private readonly List<Color> _c = new List<Color>();
            private readonly List<Vector2> _u0 = new List<Vector2>();
            private readonly List<Vector2> _u1 = new List<Vector2>();
            private readonly List<Vector2> _u2 = new List<Vector2>();
            private readonly List<int> _t = new List<int>();
            private Bounds _bounds;
            private bool _any;

            private static readonly Vector2[] Corners = { new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1) };

            public int Count => _v.Count / 4;

            public void Halo(Vector3 centre, float half, Color color, float strength, float rise = 0f, float loop = 0f)
            {
                int i = _v.Count;
                for (int k = 0; k < 4; k++)
                {
                    _v.Add(centre);
                    _c.Add(new Color(color.r, color.g, color.b, strength));
                    _u0.Add(Corners[k]);
                    _u1.Add(new Vector2(half, rise));
                    _u2.Add(new Vector2(0f, loop));
                }

                _t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                Grow(new Bounds(centre + Vector3.up * loop * 0.5f, new Vector3(half * 2f + 2f, half * 2f + loop + 2f, half * 2f + 2f)));
            }

            /// <summary>A flat pool of light lying on the street (or any level surface).</summary>
            public void Pool(Vector3 centre, float half, Color color, float strength)
            {
                int i = _v.Count;
                for (int k = 0; k < 4; k++)
                {
                    _v.Add(centre + new Vector3(Corners[k].x * half, 0.02f, Corners[k].y * half));
                    _c.Add(new Color(color.r, color.g, color.b, strength));
                    _u0.Add(Corners[k]);
                    _u1.Add(Vector2.zero);
                    _u2.Add(Vector2.zero);
                }

                _t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                Grow(new Bounds(centre, new Vector3(half * 2f, 0.2f, half * 2f)));
            }

            private void Grow(Bounds b)
            {
                if (!_any) _bounds = b;
                else _bounds.Encapsulate(b);
                _any = true;
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = _v.Count > 60000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(_v);
                mesh.SetColors(_c);
                mesh.SetUVs(0, _u0);
                mesh.SetUVs(1, _u1);
                mesh.SetUVs(2, _u2);
                mesh.SetTriangles(_t, 0);
                mesh.bounds = _bounds;
                return mesh;
            }
        }

        private sealed class FestivalRegion
        {
            public readonly FestivalMesh Solid = new FestivalMesh();
            public readonly GlowMesh Glow = new GlowMesh();
            public readonly List<(string label, Vector3 at, Quaternion rot, Color color)> Signs = new List<(string, Vector3, Quaternion, Color)>();
        }

        private static Material FestivalMaterial(string name, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new System.IO.FileNotFoundException("Shader " + shaderName + " is missing (Assets/Shaders).");
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }

            m.shader = shader;
            EditorUtility.SetDirty(m);
            Mats[name] = m;
            return m;
        }

        // ---------- the town ----------

        private struct TrackSample
        {
            public Vector3 Pos, Forward, Right;
            public int Region;
            public float Along;
        }

        /// <summary>Builds the festival under each region root (and the risers under the Track). Call after the regions exist.</summary>
        private static void BuildFestival(Layout L, GameObject[] regions)
        {
            FestivalMaterial("Festival", "Convergence/Festival");
            FestivalMaterial("FestivalGlow", "Convergence/FestivalGlow");
            var rnd = new System.Random(1117);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

            var parts = new Dictionary<int, FestivalRegion>();
            FestivalRegion Part(int region)
            {
                if (!parts.TryGetValue(region, out FestivalRegion p)) parts[region] = p = new FestivalRegion();
                return p;
            }

            // The stops the rider looks out from: nothing solid in front of them (from the stop to 40 m on, |lateral| < 7).
            var stops = new List<Pose> { L.GridStop, L.NeuronStop, L.NetViewStop, L.Rim, L.RightView };
            bool InViewBox(Vector3 p, float clearLateral)
            {
                foreach (Pose s in stops)
                {
                    Vector3 d = p - s.position;
                    Vector3 f = s.rotation * Vector3.forward;
                    float ahead = Vector3.Dot(d, f);
                    float side = Mathf.Abs(Vector3.Dot(d, s.rotation * Vector3.right));
                    if (ahead > -1f && ahead < 42f && side < clearLateral) return true;
                }

                return false;
            }

            // Big set pieces nothing may crowd: the blue network (and its view), the gold one, the pixel field.
            Vector3 blue = L.BlueNetwork.position, gold = L.GoldNetwork.position;
            Vector3 field = L.PixelField.position;
            bool NearSetPiece(Vector3 p, float pad)
            {
                if (Mathf.Abs(p.x - blue.x) < 26f + pad && p.z > L.NetViewStop.position.z - 4f && p.z < blue.z + 14f + pad) return true;
                Vector3 g = Quaternion.Inverse(L.GoldNetwork.rotation) * (p - gold);
                if (Mathf.Abs(g.x) < 26f + pad && g.z > -40f - pad && g.z < 14f + pad) return true;
                if (Mathf.Abs(p.x - field.x) < 11.5f + pad && Mathf.Abs(p.z - field.z) < 11.5f + pad) return true;
                return false;
            }

            // The Valley and its climb lie below the street: no street, stall or house may stand over them.
            float valleyHalf = L.Columns / 2 * Layout.ColumnSpacing + 8f;
            float valleyEnd = L.RowZ(L.Rows - 1) + 52f;
            bool InValley(Vector3 p)
            {
                Vector3 v = Quaternion.Inverse(L.Rim.rotation) * (p - L.Rim.position);
                return v.z > -4f && v.z < valleyEnd && Mathf.Abs(v.x) < valleyHalf;
            }

            // ----- samples along the track (leg A all; leg C from the junction on, skipping the Valley itself)
            var samples = new List<TrackSample>();
            void Sample(RidePath path, float from, float to, System.Func<Vector3, int> region)
            {
                for (float d = from; d <= to; d += 2f)
                {
                    Vector3 p = path.Position(d);
                    Vector3 f = path.Heading(d);
                    f.y = 0f;
                    if (f.sqrMagnitude < 1e-4f) f = Vector3.forward;
                    f.Normalize();
                    samples.Add(new TrackSample { Pos = p, Forward = f, Right = Vector3.Cross(Vector3.up, f), Region = region(p), Along = d });
                }
            }

            Sample(L.PathA, 0f, L.PathA.Length, p => p.z < 13f ? 0 : p.z < 54f ? 1 : p.z < 98f ? 2 : 3);
            float junction = L.Dist(L.PathC, L.Junction.position);
            float rim = L.Dist(L.PathC, L.Rim.position);
            float climbTop = L.Dist(L.PathC, L.RightView.position) - 6f;
            Sample(L.PathC, junction + 2f, rim, p => 6);
            Sample(L.PathC, climbTop, L.PathC.Length, p => 7);

            // ----- lamp posts and the lantern strings between them, both sides
            var lastPost = new Dictionary<(int region, int side), Vector3>();
            int sinceCross = 0;
            for (int i = 0; i < samples.Count; i += 5)
            {
                TrackSample s = samples[i];
                FestivalRegion part = Part(s.Region);
                Color topic = TopicLantern(s.Region);
                bool onStage = Mathf.Abs(s.Pos.z - field.z) < 13f && s.Region == 1;
                float lateral = onStage ? 12.6f : InViewBox(s.Pos + s.Right * 4.2f, 7f) ? 7.4f : 4.2f;
                Vector3[] tops = new Vector3[2];
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1f : 1f;
                    Vector3 basePos = s.Pos + s.Right * (sign * lateral);
                    if (NearSetPiece(basePos, 0f) && !onStage) continue;
                    float postTop = 3.6f;
                    // The pole, a small roof cap, an arm toward the track and its lantern.
                    part.Solid.Box(new Vector3(basePos.x, (StreetY + postTop) * 0.5f, basePos.z), new Vector3(0.18f, postTop - StreetY, 0.18f), Quaternion.LookRotation(s.Forward), DarkWood);
                    part.Solid.Box(new Vector3(basePos.x, postTop + 0.08f, basePos.z), new Vector3(0.5f, 0.12f, 0.5f), Quaternion.LookRotation(s.Forward), RoofTile);
                    Vector3 armEnd = basePos - s.Right * (sign * 0.75f) + Vector3.up * 3.35f;
                    part.Solid.Box((new Vector3(basePos.x, 3.35f, basePos.z) + armEnd) * 0.5f, new Vector3(0.09f, 0.09f, 0.8f), Quaternion.LookRotation(s.Right), DarkWood);
                    Vector3 lantern = armEnd + Vector3.down * 0.42f;
                    Color paper = (i / 5 + side) % 3 == 0 ? FestivalRed : topic;
                    part.Solid.Lantern(lantern, 0.24f, 0.5f, paper);
                    part.Glow.Halo(lantern, 0.95f, paper, 0.55f);
                    part.Glow.Pool(new Vector3(basePos.x, StreetY, basePos.z), 3.2f, Color.Lerp(paper, WarmWhite, 0.5f), 0.22f);
                    tops[side] = new Vector3(basePos.x, postTop - 0.1f, basePos.z);

                    // A string of small lanterns to the previous post on this side.
                    // No string across a stop's view: the explanations get a clean night behind them (teacher, 2026-11-17).
                    bool clearView = !InViewBox(tops[side], 12f) && !InViewBox(lastPost.TryGetValue((s.Region, side), out Vector3 was) ? was : tops[side], 12f);
                    if (clearView && lastPost.TryGetValue((s.Region, side), out Vector3 prev) && (prev - tops[side]).sqrMagnitude < 14f * 14f)
                    {
                        part.Solid.CordMesh(prev, tops[side], 0.5f, Lacquer);
                        for (int k = 1; k <= 4; k++)
                        {
                            Vector3 at = part.Solid.Cord(prev, tops[side], 0.5f, k / 5f) + Vector3.down * 0.2f;
                            Color c = k % 2 == 0 ? WarmWhite : topic;
                            part.Solid.Lantern(at, 0.15f, 0.3f, c);
                            part.Glow.Halo(at, 0.5f, c, 0.35f);
                        }
                    }

                    lastPost[(s.Region, side)] = tops[side];
                }

                // An overhead canopy between stops: two tall masts and a sagging string of big lanterns across the track.
                sinceCross++;
                Vector3 mid = s.Pos;
                bool clearAhead = !InViewBox(mid, 9f) && !InViewBox(mid + s.Forward * 6f, 9f) && !NearSetPiece(mid, 4f) && !onStage;
                if (clearAhead && sinceCross >= 2 && s.Region != 6)
                {
                    sinceCross = 0;
                    Vector3 a = s.Pos - s.Right * 5.4f, b = s.Pos + s.Right * 5.4f;
                    foreach (Vector3 m in new[] { a, b })
                    {
                        part.Solid.Box(new Vector3(m.x, (StreetY + 8.2f) * 0.5f, m.z), new Vector3(0.22f, 8.2f - StreetY, 0.22f), Quaternion.LookRotation(s.Forward), Vermilion);
                        part.Solid.Box(new Vector3(m.x, 8.3f, m.z), new Vector3(0.42f, 0.18f, 0.42f), Quaternion.identity, Lacquer);
                    }

                    Vector3 ca = new Vector3(a.x, 8.0f, a.z), cb = new Vector3(b.x, 8.0f, b.z);
                    part.Solid.CordMesh(ca, cb, 1.1f, Lacquer);
                    for (int k = 1; k <= 7; k++)
                    {
                        Vector3 at = part.Solid.Cord(ca, cb, 1.1f, k / 8f) + Vector3.down * 0.35f;
                        Color c = k % 3 == 0 ? WarmWhite : k % 3 == 1 ? FestivalRed : topic;
                        part.Solid.Lantern(at, 0.3f, 0.6f, c);
                        part.Glow.Halo(at, 1.2f, c, 0.5f);
                    }
                }

                // Wooden supports under the rails: an elevated festival tram line (none through the pixel stage).
                if (!onStage && i % 10 == 0)
                {
                    foreach (float side in new[] { -1.6f, 1.6f })
                    {
                        Vector3 p = s.Pos + s.Right * side;
                        part.Solid.Box(new Vector3(p.x, (StreetY - 0.25f) * 0.5f, p.z), new Vector3(0.35f, -0.25f - StreetY, 0.35f), Quaternion.LookRotation(s.Forward), DarkWood);
                    }

                    part.Solid.Box(new Vector3(s.Pos.x, -0.4f, s.Pos.z), new Vector3(3.8f, 0.25f, 0.4f), Quaternion.LookRotation(s.Forward), DarkWood);
                }
            }

            // ----- the festival gates: one before every stop, with its number and name
            void Gate(int region, RidePath path, Pose stop, float before, string label)
            {
                float d = Mathf.Max(1f, L.Dist(path, stop.position) - before);
                Vector3 p = path.Position(d);
                Vector3 f = path.Heading(d);
                f.y = 0f;
                f.Normalize();
                Vector3 r = Vector3.Cross(Vector3.up, f);
                Quaternion rot = Quaternion.LookRotation(f);
                FestivalRegion part = Part(region);
                Color topic = TopicLantern(region);
                const float half = 4.8f, beam = 6.7f, tie = 5.5f;
                foreach (float side in new[] { -half, half })
                {
                    Vector3 c = p + r * side;
                    part.Solid.Box(new Vector3(c.x, (StreetY + beam) * 0.5f, c.z), new Vector3(0.6f, beam - StreetY, 0.6f), rot, Vermilion);
                    part.Solid.Box(new Vector3(c.x, StreetY + 0.5f, c.z), new Vector3(0.85f, 1f, 0.85f), rot, Lacquer);
                    // A big lantern on each pillar, facing the rider.
                    Vector3 l = c - f * 0.55f + Vector3.up * 2.4f;
                    part.Solid.Lantern(l, 0.42f, 0.95f, topic);
                    part.Glow.Halo(l, 1.8f, topic, 0.6f);
                }

                // Top beam with upturned ends, its black cap, the tie beam, the centre strut.
                part.Solid.Box(p + Vector3.up * beam, new Vector3(half * 2f + 2.2f, 0.5f, 0.75f), rot, Vermilion);
                part.Solid.Box(p + Vector3.up * (beam + 0.36f), new Vector3(half * 2f + 3.0f, 0.24f, 0.9f), rot, Lacquer);
                foreach (float side in new[] { -1f, 1f })
                {
                    Quaternion tilt = rot * Quaternion.Euler(0f, 0f, side * -14f);
                    part.Solid.Box(p + r * (side * (half + 1.6f)) + Vector3.up * (beam + 0.55f), new Vector3(1.2f, 0.24f, 0.9f), tilt, Lacquer);
                }

                part.Solid.Box(p + Vector3.up * tie, new Vector3(half * 2f + 0.8f, 0.36f, 0.42f), rot, Vermilion);
                part.Solid.Box(p + Vector3.up * ((tie + beam) * 0.5f), new Vector3(0.3f, beam - tie, 0.3f), rot, Vermilion);

                // The name plaque under the tie beam, and a row of small lanterns either side of it.
                Vector3 plaque = p + Vector3.up * (tie - 0.75f);
                part.Solid.Box(plaque + f * 0.02f, new Vector3(3.3f, 1.1f, 0.12f), rot, new Color(0.85f, 0.62f, 0.25f));
                part.Solid.Box(plaque - f * 0.02f, new Vector3(3.05f, 0.9f, 0.14f), rot, new Color(0.08f, 0.05f, 0.04f));
                part.Signs.Add((label, plaque - f * 0.1f, rot, topic));
                foreach (float x in new[] { -3.9f, -2.65f, 2.65f, 3.9f })
                {
                    Vector3 at = p + r * x + Vector3.up * (tie - 0.65f);
                    part.Solid.CordMesh(at + Vector3.up * 0.65f, at + Vector3.up * 0.3f, 0f, Lacquer);
                    Color c = Mathf.Abs(x) > 3f ? FestivalRed : WarmWhite;
                    part.Solid.Lantern(at, 0.26f, 0.52f, c);
                    part.Glow.Halo(at, 0.9f, c, 0.45f);
                }
            }

            Gate(0, L.PathA, L.GridStop, 11f, "1  PIXELS");
            Gate(2, L.PathA, L.NeuronStop, 12f, "2  NEURON");
            Gate(3, L.PathA, L.NetViewStop, 12f, "3  NETWORK");
            Gate(6, L.PathC, L.Rim, 12f, "5  TRAINING");
            Gate(7, L.PathC, L.RightView, 12f, "PIXEL CAN READ!");

            // ----- the pixel stage: a raised wooden stage under the 7x7 pillars, lanterns on its corners
            {
                FestivalRegion part = Part(1);
                float deck = field.y - 0.05f;
                part.Solid.Box(new Vector3(field.x, (StreetY + deck) * 0.5f, field.z), new Vector3(23f, deck - StreetY, 23f), Quaternion.identity, new Color(0.11f, 0.07f, 0.06f));
                part.Solid.Box(new Vector3(field.x, deck - 0.4f, field.z), new Vector3(23.6f, 0.5f, 23.6f), Quaternion.identity, Vermilion); // its top 15 cm under the deck: no z-fighting
                foreach (Vector2 c in new[] { new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(1, 1) })
                {
                    Vector3 at = new Vector3(field.x + c.x * 11.4f, deck + 1.2f, field.z + c.y * 11.4f);
                    part.Solid.Box(new Vector3(at.x, deck + 0.5f, at.z), new Vector3(0.2f, 1.0f, 0.2f), Quaternion.identity, DarkWood);
                    part.Solid.Lantern(at + Vector3.up * 0.2f, 0.32f, 0.65f, TopicLantern(1));
                    part.Glow.Halo(at + Vector3.up * 0.2f, 1.3f, TopicLantern(1), 0.55f);
                }
            }

            // ----- the street: food stalls near the line, lit houses farther out
            List<TrackSample> all = samples;
            float NearestTrack(Vector3 p)
            {
                float best = float.MaxValue;
                foreach (TrackSample s in all) best = Mathf.Min(best, (new Vector2(p.x, p.z) - new Vector2(s.Pos.x, s.Pos.z)).sqrMagnitude);
                return Mathf.Sqrt(best);
            }

            for (int i = 0; i < all.Count; i += 6)
            {
                TrackSample s = all[i];
                FestivalRegion part = Part(s.Region);
                Color topic = TopicLantern(s.Region);
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1f : 1f;
                    // A stall facing the line, 9-13 m out.
                    Vector3 stall = s.Pos + s.Right * (sign * R(9.5f, 13f)) + s.Forward * R(-3f, 3f);
                    stall.y = StreetY;
                    if (!NearSetPiece(stall, 3f) && !InValley(stall) && NearestTrack(stall) > 8f && rnd.NextDouble() < 0.75) Stall(part, stall, -s.Right * sign, topic, rnd);

                    // Two rows of houses behind.
                    for (int row = 0; row < 2; row++)
                    {
                        Vector3 house = s.Pos + s.Right * (sign * (22f + row * 13f + R(0f, 5f))) + s.Forward * R(-4f, 4f);
                        house.y = StreetY;
                        if (NearSetPiece(house, 6f) || InValley(house) || NearestTrack(house) < 18f) continue;
                        House(part, house, -s.Right * sign, R(6f, 10f), R(6f, 11f + row * 3f), rnd, topic);
                    }
                }
            }

            // ----- the valley: a lantern on every hop post down the training path, and lantern posts along both edges
            {
                FestivalRegion part = Part(6);
                Color pink = TopicLantern(6);
                Vector3 right = L.Rim.rotation * Vector3.right;
                Level1Hop[] hops = _data.File.valley.hops;
                for (int h = 0; h < hops.Length; h++)
                {
                    Vector3 c = L.Ribbon(_data, hops[h].row, L.Columns / 2) - Vector3.up * Layout.Clearance;
                    foreach (float side in new[] { -2.2f, 2.2f })
                    {
                        Vector3 l = c + right * side + Vector3.up * 0.95f;
                        Color lc = h % 2 == 0 ? pink : WarmWhite;
                        part.Solid.Lantern(l, 0.2f, 0.4f, lc);
                        part.Glow.Halo(l, 0.85f, lc, 0.55f);
                    }
                }

                for (int r = 0; r < L.Rows; r += 3)
                {
                    foreach (int c in new[] { 0, L.Columns - 1 })
                    {
                        Vector3 p = L.Ribbon(_data, r, c) - Vector3.up * Layout.Clearance;
                        part.Solid.Box(p + Vector3.up * 1.2f, new Vector3(0.14f, 2.4f, 0.14f), L.Rim.rotation, DarkWood);
                        Vector3 l = p + Vector3.up * 2.15f;
                        Color lc = (r / 3) % 3 == 0 ? FestivalRed : pink;
                        part.Solid.Lantern(l, 0.24f, 0.48f, lc);
                        part.Glow.Halo(l, 1.0f, lc, 0.5f);
                        part.Glow.Pool(p, 2.2f, lc, 0.18f);
                    }
                }
            }

            // ----- a lit skyline behind each network (the view the rider faces longest), with a pagoda off to one side
            void Backdrop(int region, Pose net, float pagodaSide)
            {
                FestivalRegion part = Part(region);
                Vector3 f = net.rotation * Vector3.forward, r = net.rotation * Vector3.right;
                for (int row = 0; row < 2; row++)
                {
                    for (float x = -54f; x <= 54f; x += R(8.5f, 11f))
                    {
                        Vector3 at = net.position + f * (24f + row * 12f + R(0f, 3f)) + r * x;
                        at.y = StreetY;
                        // Low enough that the roofs stay at the horizon, under the diagram's wires.
                        House(part, at, -f, R(6.5f, 9.5f), R(4f, 5.5f + row * 1.5f), rnd, TopicLantern(region));
                    }
                }

                // Far off to the side (about 60 degrees from the diagram's centre), never behind the diagram.
                Vector3 pagoda = net.position - f * 6f + r * (pagodaSide * 46f);
                pagoda.y = StreetY;
                Pagoda(part, pagoda, -f, TopicLantern(region));
            }

            Backdrop(3, L.BlueNetwork, 1f);
            Backdrop(7, L.GoldNetwork, -1f);

            // ----- the street itself: dark paving 34 m either side of the line, warm where lamps stand
            var cells = new Dictionary<(int, int), int>();
            const float cell = 4f;
            foreach (TrackSample s in all)
            {
                for (int dx = -9; dx <= 9; dx++)
                {
                    for (int dz = -9; dz <= 9; dz++)
                    {
                        var key = (Mathf.FloorToInt(s.Pos.x / cell) + dx, Mathf.FloorToInt(s.Pos.z / cell) + dz);
                        if (!cells.ContainsKey(key)) cells[key] = s.Region;
                    }
                }
            }

            foreach (KeyValuePair<(int, int), int> kv in cells)
            {
                Vector3 c = new Vector3((kv.Key.Item1 + 0.5f) * cell, StreetY, (kv.Key.Item2 + 0.5f) * cell);
                if (NearestTrack(c) > 36f || InValley(c)) continue;
                float v = R(0.85f, 1.15f);
                Color stone = new Color(0.085f * v, 0.065f * v, 0.08f * v, 0f);
                Vector3 h = new Vector3(cell * 0.5f - 0.05f, 0f, cell * 0.5f - 0.05f);
                Part(kv.Value).Solid.Quad(c + new Vector3(-h.x, 0, -h.z), c + new Vector3(-h.x, 0, h.z), c + new Vector3(h.x, 0, h.z), c + new Vector3(h.x, 0, -h.z), stone);
                // Where the street meets the Valley, a stone retaining wall drops away, so from the valley floor the town stands on
                // a terrace instead of floating.
                foreach (Vector2 d in new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) })
                {
                    if (!InValley(c + new Vector3(d.x, 0f, d.y) * cell)) continue;
                    Vector3 mid = c + new Vector3(d.x, 0f, d.y) * (cell * 0.5f);
                    Vector3 along = new Vector3(-d.y, 0f, d.x) * (cell * 0.5f);
                    Color wall = new Color(0.12f * v, 0.09f * v, 0.1f * v, 0f);
                    Part(kv.Value).Solid.Quad(mid - along, mid + along, mid + along + Vector3.down * 16f, mid - along + Vector3.down * 16f, wall);
                }
            }

            // ----- write each region's two meshes, and the signs
            foreach (KeyValuePair<int, FestivalRegion> kv in parts)
            {
                var root = Make("Festival", regions[kv.Key].transform, Vector3.zero);
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                AddMeshObject("Town", root.transform, SaveMesh(kv.Value.Solid.ToMesh("Festival" + kv.Key), "L1_Festival" + kv.Key), "Festival");
                AddMeshObject("Lights", root.transform, SaveMesh(kv.Value.Glow.ToMesh("FestivalGlow" + kv.Key), "L1_FestivalGlow" + kv.Key), "FestivalGlow");
                foreach (var sign in kv.Value.Signs)
                {
                    TextMeshPro t = Text("Sign", root.transform, Vector3.zero, sign.label, 0.62f, sign.color, 3f, TextAlignmentOptions.Center, true);
                    t.transform.SetPositionAndRotation(sign.at, sign.rot);
                    t.textWrappingMode = TextWrappingModes.NoWrap;
                    t.enableAutoSizing = true;
                    t.fontSizeMin = 1.5f;
                    t.fontSizeMax = 6.2f;
                    t.rectTransform.sizeDelta = new Vector2(2.85f, 0.8f);
                }
            }

            // ----- sky lanterns rising all over town (one draw, on the Track so they are always there)
            var sky = new GlowMesh();
            Vector3 min = new Vector3(float.MaxValue, 0, float.MaxValue), max = new Vector3(float.MinValue, 0, float.MinValue);
            foreach (TrackSample s in all)
            {
                min = Vector3.Min(min, s.Pos);
                max = Vector3.Max(max, s.Pos);
            }

            for (int i = 0; i < 160; i++)
            {
                Vector3 p = new Vector3(R(min.x - 90f, max.x + 90f), StreetY + R(-2f, 6f), R(min.z - 40f, max.z + 90f));
                float d = NearestTrack(p);
                if (d < 16f || d > 110f) continue;
                Color c = Color.Lerp(new Color(1f, 0.55f, 0.22f), WarmWhite, R(0f, 0.5f));
                sky.Halo(p, R(0.45f, 0.8f), c, R(0.7f, 1.1f), R(0.25f, 0.55f), R(38f, 55f));
            }

            Transform track = GameObject.Find("Track").transform;
            var skyRoot = Make("SkyLanterns", track, Vector3.zero);
            skyRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            AddMeshObject("Risers", skyRoot.transform, SaveMesh(sky.ToMesh("SkyLanterns"), "L1_SkyLanterns"), "FestivalGlow");
        }

        /// <summary>A food stall: counter, glowing front, a striped awning in the topic colour, lanterns along the eave.</summary>
        private static void Stall(FestivalRegion part, Vector3 at, Vector3 facing, Color topic, System.Random rnd)
        {
            facing.y = 0f;
            facing.Normalize();
            Quaternion rot = Quaternion.LookRotation(facing);
            Vector3 r = rot * Vector3.right;
            const float w = 3.4f, h = 2.3f, d = 2.2f;
            part.Solid.Box(at + Vector3.up * (h * 0.5f), new Vector3(w, h, d), rot, DarkWood);
            // The lit counter front, a warm strip, and the roof.
            part.Solid.Box(at + facing * (d * 0.5f + 0.03f) + Vector3.up * 1.05f, new Vector3(w - 0.3f, 0.7f, 0.06f), rot, new Color(1f, 0.78f, 0.45f, 1f));
            part.Solid.Box(at + Vector3.up * (h + 0.75f), new Vector3(w + 0.4f, 0.18f, d + 0.4f), rot, RoofTile);
            // Corner posts holding the roof up.
            foreach (Vector2 c in new[] { new Vector2(-1, 1), new Vector2(1, 1) })
            {
                part.Solid.Box(at + r * (c.x * (w * 0.5f)) + facing * (d * 0.5f + 0.7f) + Vector3.up * ((h + 0.7f) * 0.5f), new Vector3(0.1f, h + 0.7f, 0.1f), rot, DarkWood);
            }

            // Striped awning sloping out over the front.
            const int stripes = 7;
            Vector3 back = at + facing * (d * 0.5f) + Vector3.up * (h + 0.65f), front = at + facing * (d * 0.5f + 0.85f) + Vector3.up * (h + 0.1f);
            for (int k = 0; k < stripes; k++)
            {
                float x0 = -w * 0.5f - 0.15f + k * (w + 0.3f) / stripes, x1 = x0 + (w + 0.3f) / stripes;
                Color c = k % 2 == 0 ? new Color(topic.r * 0.8f, topic.g * 0.8f, topic.b * 0.8f, 0.25f) : new Color(0.85f, 0.82f, 0.78f, 0.15f);
                part.Solid.Quad(back + r * x0, back + r * x1, front + r * x1, front + r * x0, c);
            }

            for (int k = 0; k < 3; k++)
            {
                Vector3 l = front + r * ((k - 1) * 1.15f) + Vector3.down * 0.3f;
                Color c = k == 1 ? FestivalRed : WarmWhite;
                part.Solid.Lantern(l, 0.17f, 0.34f, c);
                part.Glow.Halo(l, 0.6f, c, 0.4f);
            }

            part.Glow.Pool(at + facing * 2.2f, 3.4f, new Color(1f, 0.62f, 0.3f), 0.3f);
        }

        /// <summary>A five-tier pagoda: shrinking storeys, wide dark roofs with vermilion trim, a lantern at every corner, a spire.</summary>
        private static void Pagoda(FestivalRegion part, Vector3 at, Vector3 facing, Color topic)
        {
            facing.y = 0f;
            facing.Normalize();
            Quaternion rot = Quaternion.LookRotation(facing);
            float y = 0f;
            for (int tier = 0; tier < 5; tier++)
            {
                float w = 7.5f - tier * 1.1f, h = tier == 0 ? 4.2f : 3.1f;
                part.Solid.Box(at + Vector3.up * (y + h * 0.5f), new Vector3(w, h, w), rot, new Color(0.12f, 0.07f, 0.06f));
                // A lit band of paper screens round each storey.
                part.Solid.Box(at + Vector3.up * (y + h * 0.55f), new Vector3(w + 0.3f, 0.9f, w + 0.3f), rot, new Color(1f, 0.7f, 0.36f, 0.85f));
                y += h;
                float roof = w + 3.2f;
                part.Solid.Box(at + Vector3.up * (y + 0.2f), new Vector3(roof, 0.45f, roof), rot, RoofTile);
                part.Solid.Box(at + Vector3.up * (y - 0.08f), new Vector3(roof + 0.2f, 0.16f, roof + 0.2f), rot, Vermilion);
                foreach (Vector2 c in new[] { new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(1, 1) })
                {
                    Vector3 corner = at + rot * new Vector3(c.x * roof * 0.5f, 0f, c.y * roof * 0.5f) + Vector3.up * (y - 0.75f);
                    Color lc = (tier + (int)(c.x + c.y)) % 2 == 0 ? FestivalRed : topic;
                    part.Solid.Lantern(corner, 0.35f, 0.7f, lc);
                    part.Glow.Halo(corner, 1.4f, lc, 0.5f);
                }

                y += 0.5f;
            }

            part.Solid.Box(at + Vector3.up * (y + 2.2f), new Vector3(0.3f, 4.4f, 0.3f), rot, new Color(0.85f, 0.62f, 0.25f));
        }

        /// <summary>A two-storey town house with a dark tiled gable roof and warm windows on the side facing the line.</summary>
        private static void House(FestivalRegion part, Vector3 at, Vector3 facing, float width, float height, System.Random rnd, Color topic)
        {
            facing.y = 0f;
            facing.Normalize();
            Quaternion rot = Quaternion.LookRotation(facing);
            Vector3 r = rot * Vector3.right;
            float depth = 6f;
            Color wall = new Color(0.10f, 0.075f, 0.075f);
            part.Solid.Box(at + Vector3.up * (height * 0.5f), new Vector3(width, height, depth), rot, wall);
            // Gable roof: two slopes over the full depth, with eaves.
            float ridge = 2.2f;
            Vector3 e0 = at + Vector3.up * height, ridgeC = e0 + Vector3.up * ridge;
            Vector3 hw = r * (width * 0.5f + 0.5f), hd = facing * (depth * 0.5f + 0.7f);
            part.Solid.Quad(e0 - hw - hd + facing * 0f, ridgeC - hd, ridgeC + hd, e0 - hw + hd, RoofTile);
            part.Solid.Quad(e0 + hw + hd, ridgeC + hd, ridgeC - hd, e0 + hw - hd, RoofTile);
            part.Solid.Tri(e0 - r * (width * 0.5f) + facing * (depth * 0.5f), ridgeC + facing * (depth * 0.5f), e0 + r * (width * 0.5f) + facing * (depth * 0.5f), wall);
            // Windows: a grid on the front, some lit warm, a few in the topic colour (paper screens).
            int cols = Mathf.Max(2, Mathf.FloorToInt(width / 1.8f)), floors = Mathf.Max(1, Mathf.FloorToInt((height - 1f) / 2.6f));
            Vector3 face = at + facing * (depth * 0.5f + 0.1f); // windows stand off the wall: no z-fighting from afar
            for (int fl = 0; fl < floors; fl++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double roll = rnd.NextDouble();
                    if (roll < 0.25) continue;
                    Color lit = roll < 0.75 ? new Color(1f, 0.72f, 0.38f, 1f) : roll < 0.9 ? new Color(topic.r, topic.g, topic.b, 1f) : new Color(0.12f, 0.1f, 0.1f, 0f);
                    float x = -width * 0.5f + (c + 0.5f) * width / cols;
                    float y = 1.4f + fl * 2.6f;
                    Vector3 wc = face + r * x + Vector3.up * y;
                    Vector3 wx = r * 0.45f, wy = Vector3.up * 0.6f;
                    part.Solid.Quad(wc - wx - wy, wc - wx + wy, wc + wx + wy, wc + wx - wy, lit);
                }
            }

            // A lantern by the door.
            Vector3 door = face + r * (width * 0.25f) + Vector3.up * 2.2f + facing * 0.3f;
            part.Solid.Lantern(door, 0.22f, 0.45f, FestivalRed);
            part.Glow.Halo(door, 0.8f, FestivalRed, 0.4f);
        }
    }
}
