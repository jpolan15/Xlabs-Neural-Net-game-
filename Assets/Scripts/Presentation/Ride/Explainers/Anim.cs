using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>
    /// The explainers' animation primitives, after 3Blue1Brown's manim: Write, Create, FadeIn (with a shift), FadeOut,
    /// Transform, Indicate, Flash, GrowFromCenter, and LaggedStart to cascade many of them. Each returns tweens for an
    /// explainer's <see cref="Timeline"/>; every tween eases with <see cref="Rate.Smooth"/> unless told otherwise and
    /// is correct when completed at once (the operator's skip). Default timings follow the look rules: moves 0.6 to
    /// 1.5 s, cascades 0.03 to 0.1 s apart.
    /// </summary>
    public static class Anim
    {
        public const float Move = 1f;
        public const float Lag = 0.05f;

        // ---------- cascades ----------

        /// <summary>LaggedStart: each tween starts lagSeconds after the one before it. Returns the same tweens.</summary>
        public static List<Tween> LaggedStart(float lagSeconds, IEnumerable<Tween> tweens)
        {
            var list = new List<Tween>(tweens);
            for (int i = 0; i < list.Count; i++) list[i].Delay += i * lagSeconds;
            return list;
        }

        public static List<Tween> LaggedStart(float lagSeconds, params Tween[] tweens) => LaggedStart(lagSeconds, (IEnumerable<Tween>)tweens);

        /// <summary>One tween per index, cascaded.</summary>
        public static List<Tween> Each(IList<int> indices, float lagSeconds, Func<int, Tween> make)
        {
            var list = new List<Tween>(indices.Count);
            for (int k = 0; k < indices.Count; k++)
            {
                Tween t = make(indices[k]);
                t.Delay += k * lagSeconds;
                list.Add(t);
            }

            return list;
        }

        public static int[] Range(int from, int count)
        {
            var r = new int[count];
            for (int i = 0; i < count; i++) r[i] = from + i;
            return r;
        }

        // ---------- shapes in a batch ----------

        /// <summary>FadeIn(shift): the shape appears from transparent, sliding in from position - shift.</summary>
        public static Tween FadeIn(ShapeBatch batch, int i, Vector3 shift = default, float duration = Move)
        {
            Vector3 home = Vector3.zero, endHome = Vector3.zero;
            float alpha = 1f;
            return new Tween(duration, a =>
            {
                Shape s = batch[i];
                s.alpha = alpha * a;
                s.position = home - shift * (1f - a);
                s.end = endHome - shift * (1f - a);
                batch.MarkDirty();
            }, begin: () =>
            {
                Shape s = batch[i];
                home = s.position;
                endHome = s.end;
                alpha = s.alpha > 0.001f ? s.alpha : 1f;
            });
        }

        /// <summary>FadeOut(shift): the shape fades away while drifting by shift.</summary>
        public static Tween FadeOut(ShapeBatch batch, int i, Vector3 shift = default, float duration = Move)
        {
            Vector3 from = Vector3.zero, endFrom = Vector3.zero;
            float alpha = 1f;
            return new Tween(duration, a =>
            {
                Shape s = batch[i];
                s.alpha = alpha * (1f - a);
                s.position = from + shift * a;
                s.end = endFrom + shift * a;
                batch.MarkDirty();
            }, begin: () =>
            {
                Shape s = batch[i];
                from = s.position;
                endFrom = s.end;
                alpha = s.alpha;
            });
        }

        /// <summary>Fades a shape's alpha to a value (dim, brighten) without moving it.</summary>
        public static Tween FadeTo(ShapeBatch batch, int i, float alpha, float duration = Move)
        {
            float from = 0f;
            return new Tween(duration, a =>
            {
                batch[i].alpha = Mathf.Lerp(from, alpha, a);
                batch.MarkDirty();
            }, begin: () => from = batch[i].alpha);
        }

        /// <summary>Create: a line grows from its start to its end.</summary>
        public static Tween Create(ShapeBatch batch, int i, float duration = Move)
        {
            return new Tween(duration, a =>
            {
                Shape s = batch[i];
                s.grow = a;
                if (s.alpha <= 0.001f) s.alpha = 1f;
                batch.MarkDirty();
            }, begin: () =>
            {
                batch[i].grow = 0f;
                if (batch[i].alpha <= 0.001f) batch[i].alpha = 1f;
            });
        }

        /// <summary>
        /// Transform: one shape becomes another (position, rotation, size, colour, stroke, roundness), the target
        /// read when the tween starts. A square can become a circle, a pixel a pillar top.
        /// </summary>
        public static Tween TransformShape(ShapeBatch batch, int i, Func<Shape> target, float duration = Move, Func<float, float> rate = null)
        {
            Shape from = null, to = null;
            return new Tween(duration, a =>
            {
                Shape s = batch[i];
                s.position = Vector3.LerpUnclamped(from.position, to.position, a);
                s.end = Vector3.LerpUnclamped(from.end, to.end, a);
                s.rotation = Quaternion.Slerp(from.rotation, to.rotation, a);
                s.size = Vector2.LerpUnclamped(from.size, to.size, a);
                s.color = Color.LerpUnclamped(from.color, to.color, a);
                s.stroke = Color.LerpUnclamped(from.stroke, to.stroke, a);
                s.strokeWidth = Mathf.LerpUnclamped(from.strokeWidth, to.strokeWidth, a);
                s.round = Mathf.LerpUnclamped(from.round, to.round, a);
                s.alpha = Mathf.LerpUnclamped(from.alpha, to.alpha, a);
                s.scale = Mathf.LerpUnclamped(from.scale, to.scale, a);
                s.grow = Mathf.LerpUnclamped(from.grow, to.grow, a);
                batch.MarkDirty();
            }, rate, begin: () =>
            {
                from = batch[i].Clone();
                to = target();
                if (from.alpha <= 0.001f) from.alpha = 0f;
            });
        }

        /// <summary>Indicate: the shape swells to 1.15 and flashes YELLOW (its stroke), then settles back.</summary>
        public static Tween Indicate(ShapeBatch batch, int i, Color yellow, float duration = 1f, float swell = 1.15f)
        {
            Color stroke = default;
            float width = 0f, scale = 1f;
            return new Tween(duration, a =>
            {
                Shape s = batch[i];
                s.scale = scale * Mathf.Lerp(1f, swell, a);
                s.stroke = Color.Lerp(stroke, yellow, a);
                s.strokeWidth = Mathf.Lerp(width, Mathf.Max(width, s.size.x * 0.13f), a);
                batch.MarkDirty();
            }, Rate.ThereAndBack, begin: () =>
            {
                stroke = batch[i].stroke;
                width = batch[i].strokeWidth;
                scale = batch[i].scale;
            });
        }

        /// <summary>
        /// Flash: a ring of short lines bursts out from a point and fades (success sparkle). Uses lines first to
        /// first + count - 1 of a batch kept for sparkles, laid out by the builder around the origin; they are moved
        /// to the point.
        /// </summary>
        public static List<Tween> Flash(ShapeBatch sparks, int first, int count, Vector3 at, Quaternion facing, Color color, float radius = 0.35f, float duration = 0.6f)
        {
            var list = new List<Tween>(count);
            for (int k = 0; k < count; k++)
            {
                int i = first + k;
                float angle = k * Mathf.PI * 2f / count;
                Vector3 dir = facing * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                list.Add(new Tween(duration, a =>
                {
                    Shape s = sparks[i];
                    float inner = Mathf.Lerp(0.3f, 1f, a) * radius, outer = Mathf.Lerp(0.45f, 1.35f, Mathf.Min(1f, a * 1.4f)) * radius;
                    s.position = at + dir * inner;
                    s.end = at + dir * outer;
                    s.alpha = 1f - a;
                    s.grow = 1f;
                    s.color = color;
                    s.rotation = facing;
                    sparks.MarkDirty();
                }, Rate.RushFrom));
            }

            return list;
        }

        // ---------- transforms ----------

        /// <summary>Transform: an object moves, turns and scales into a target's world pose (the unfold's core move).</summary>
        public static Tween MoveTo(Transform piece, Func<(Vector3 position, Quaternion rotation, Vector3 scale)> target, float duration = Move, Func<float, float> rate = null)
        {
            Vector3 p0 = Vector3.zero, s0 = Vector3.one;
            Quaternion r0 = Quaternion.identity;
            (Vector3 position, Quaternion rotation, Vector3 scale) to = default;
            return new Tween(duration, a =>
            {
                piece.SetPositionAndRotation(Vector3.LerpUnclamped(p0, to.position, a), Quaternion.Slerp(r0, to.rotation, a));
                piece.localScale = Vector3.LerpUnclamped(s0, to.scale, a);
            }, rate, begin: () =>
            {
                p0 = piece.position;
                r0 = piece.rotation;
                s0 = piece.localScale;
                to = target();
            });
        }

        /// <summary>The local scale that gives a child of parent this world (lossy) scale; parent rotation ignored.</summary>
        public static Vector3 LocalScaleFor(Transform parent, Vector3 worldScale)
        {
            Vector3 p = parent != null ? parent.lossyScale : Vector3.one;
            return new Vector3(worldScale.x / Mathf.Max(1e-5f, p.x), worldScale.y / Mathf.Max(1e-5f, p.y), worldScale.z / Mathf.Max(1e-5f, p.z));
        }

        /// <summary>GrowFromCenter: the object scales up from nothing to its scale, optionally flying out from a point.</summary>
        public static Tween GrowFromCenter(Transform t, Vector3 homeScale, Vector3? fromWorld = null, float duration = 1.2f)
        {
            Vector3 home = Vector3.zero, start = Vector3.zero;
            return new Tween(duration, a =>
            {
                t.localScale = Vector3.LerpUnclamped(homeScale * 0.001f, homeScale, a);
                t.position = Vector3.LerpUnclamped(start, home, a);
            }, begin: () =>
            {
                t.gameObject.SetActive(true);
                home = t.position;
                start = fromWorld ?? home;
                t.localScale = homeScale * 0.001f;
                t.position = start;
            });
        }

        /// <summary>GrowUp: the object rises out of its own floor (its local y scales from nothing), like a pillar extruding.</summary>
        public static Tween GrowUp(Transform t, Vector3 homeScale, float duration = 1.2f)
        {
            return new Tween(duration, a => t.localScale = new Vector3(homeScale.x, Mathf.LerpUnclamped(homeScale.y * 0.001f, homeScale.y, a), homeScale.z),
                begin: () =>
                {
                    t.gameObject.SetActive(true);
                    t.localScale = new Vector3(homeScale.x, homeScale.y * 0.001f, homeScale.z);
                });
        }

        /// <summary>Fades every renderer under an object through its material's _BaseColor alpha (transparent materials only).</summary>
        public static Tween FadeRenderers(GameObject root, float from, float to, float duration = Move)
        {
            Renderer[] renderers = null;
            Color[] baseColors = null;
            var block = new MaterialPropertyBlock();
            int id = Shader.PropertyToID("_BaseColor");
            return new Tween(duration, a =>
            {
                float alpha = Mathf.Lerp(from, to, a);
                for (int k = 0; k < renderers.Length; k++)
                {
                    Color c = baseColors[k];
                    c.a *= alpha;
                    renderers[k].GetPropertyBlock(block);
                    block.SetColor(id, c);
                    renderers[k].SetPropertyBlock(block);
                }
            }, begin: () =>
            {
                root.SetActive(true);
                renderers = root.GetComponentsInChildren<Renderer>(true);
                baseColors = new Color[renderers.Length];
                for (int k = 0; k < renderers.Length; k++)
                {
                    Material m = renderers[k].sharedMaterial;
                    baseColors[k] = m != null && m.HasProperty(id) ? m.GetColor(id) : Color.white;
                }
            });
        }

        /// <summary>Tints a renderer through a property block (_BaseColor), from its current tint to a colour.</summary>
        public static Tween Tint(Renderer r, Color to, float duration = Move)
        {
            var block = new MaterialPropertyBlock();
            int id = Shader.PropertyToID("_BaseColor");
            Color from = Color.white;
            return new Tween(duration, a =>
            {
                r.GetPropertyBlock(block);
                block.SetColor(id, Color.Lerp(from, to, a));
                r.SetPropertyBlock(block);
            }, begin: () =>
            {
                r.GetPropertyBlock(block);
                Color c = block.GetColor(id);
                from = block.isEmpty || c == default ? (r.sharedMaterial != null ? r.sharedMaterial.GetColor(id) : Color.white) : c;
            });
        }

        // ---------- text ----------

        /// <summary>
        /// Write: the text appears letter by letter, each fading in while rising a little into place (lagRatio is the
        /// share of one letter's time between letter starts, as in manim).
        /// </summary>
        public static Tween Write(TMP_Text text, float duration = 1f, float lagRatio = 0.25f, float riseEm = 0.35f)
        {
            Color32[][] colors = null;
            Vector3[][] vertices = null;
            float rise = 0f;
            return new Tween(duration, a =>
            {
                if (colors == null) return;
                TMP_TextInfo info = text.textInfo;
                int visible = 0;
                for (int c = 0; c < info.characterCount; c++) if (info.characterInfo[c].isVisible) visible++;
                int n = 0;
                for (int c = 0; c < info.characterCount; c++)
                {
                    TMP_CharacterInfo ch = info.characterInfo[c];
                    if (!ch.isVisible) continue;
                    float p = a >= 1f ? 1f : Rate.Smooth(Rate.Lagged(a, n++, visible, lagRatio));
                    int m = ch.materialReferenceIndex, v = ch.vertexIndex;
                    if (m >= colors.Length || colors[m] == null || v + 3 >= colors[m].Length) continue;
                    for (int k = 0; k < 4; k++)
                    {
                        Color32 col = colors[m][v + k];
                        col.a = (byte)(col.a * p);
                        info.meshInfo[m].colors32[v + k] = col;
                        info.meshInfo[m].vertices[v + k] = vertices[m][v + k] + Vector3.down * (rise * (1f - p));
                    }
                }

                text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32 | TMP_VertexDataUpdateFlags.Vertices);
            }, Rate.Linear, begin: () =>
            {
                text.gameObject.SetActive(true);
                text.alpha = 1f;
                text.ForceMeshUpdate(true, true);
                TMP_TextInfo info = text.textInfo;
                colors = new Color32[info.meshInfo.Length][];
                vertices = new Vector3[info.meshInfo.Length][];
                for (int m = 0; m < info.meshInfo.Length; m++)
                {
                    colors[m] = info.meshInfo[m].colors32 != null ? (Color32[])info.meshInfo[m].colors32.Clone() : null;
                    vertices[m] = info.meshInfo[m].vertices != null ? (Vector3[])info.meshInfo[m].vertices.Clone() : null;
                }

                rise = text.fontSize * riseEm * 0.1f;
            });
        }

        /// <summary>Fades a text's alpha (FadeIn / FadeOut of a label), with an optional drift in its local space.</summary>
        public static Tween FadeText(TMP_Text text, float to, float duration = Move, Vector3 shift = default)
        {
            float from = 0f;
            Vector3 home = Vector3.zero;
            return new Tween(duration, a =>
            {
                text.alpha = Mathf.Lerp(from, to, a);
                text.transform.localPosition = home + shift * a;
            }, begin: () =>
            {
                text.gameObject.SetActive(true);
                from = text.alpha;
                home = text.transform.localPosition;
            }, end: () =>
            {
                if (to <= 0.001f) text.gameObject.SetActive(false);
            });
        }
    }
}
