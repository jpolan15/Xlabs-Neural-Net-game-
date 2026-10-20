using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>
    /// E1, pixels (the classic: the digit on a grid, each square showing its brightness). The hero 3 we dove through
    /// hangs in the dark; a grid draws itself over it and it turns into 28×28 squares (PIXELS). A lens lifts a patch
    /// out and every square shows its number; dark is zero, bright is one. The 28×28 merge into the real 7×7 the
    /// network reads (4×4 averages from the bake), the 49 fly into one column (the input layer of E3), and on
    /// {unfold} the 7×7 lie down and grow into the pixel field the ride flies over: pillar height = brightness.
    /// </summary>
    public sealed class PixelsExplainer : Explainer
    {
        /// <summary>The unfold's timing, shared with the builder (it delays each field row to its landing).</summary>
        public const float FlySeconds = 1.5f, FlyLagPerRow = 0.12f, FlyLagPerColumn = 0.015f;

        [SerializeField] private Renderer picture;
        [SerializeField] private ShapeBatch grid28;
        [SerializeField] private ShapeBatch pixels28;
        [SerializeField] private ShapeBatch lens;
        [SerializeField] private ShapeBatch zoom;
        [SerializeField] private TMP_Text[] values = new TMP_Text[0];
        [SerializeField] private ShapeBatch pixels7;
        [SerializeField] private ShapeBatch column;
        [SerializeField] private TMP_Text count;
        [SerializeField] private TMP_Text title;

        [Tooltip("For each zoom square, the 28×28 square it magnifies.")]
        [SerializeField] private int[] zoomSource = new int[0];
        [SerializeField] private int zeroCell;
        [SerializeField] private int oneCell;

        [Tooltip("The field the 7×7 becomes: cell (r, c) sits at ((c - 3) * pitch, 0, (3 - r) * pitch) in it, pillars width wide.")]
        [SerializeField] private Transform field;
        [SerializeField] private float fieldPitch = 3f;
        [SerializeField] private float pillarWidth = 2.7f;

        protected override void ResetPieces()
        {
            SetPictureAlpha(0f);
            Hide(grid28, true);
            Hide(pixels28, false);
            Hide(lens, true);
            Hide(zoom, false);
            Hide(pixels7, false);
            Hide(column, false);
            var texts = new List<TMP_Text>(values) { count, title };
            foreach (TMP_Text t in texts)
            {
                t.gameObject.SetActive(false);
                t.alpha = 1f;
            }
        }

        private static void Hide(ShapeBatch b, bool lines)
        {
            b.ResetToHome();
            for (int i = 0; i < b.Count; i++)
            {
                b[i].alpha = 0f;
                if (lines) b[i].grow = 0f;
            }

            b.MarkDirty();
        }

        private void SetPictureAlpha(float a)
        {
            var block = new MaterialPropertyBlock();
            picture.GetPropertyBlock(block);
            block.SetColor("_BaseColor", new Color(1f, 1f, 1f, a));
            picture.SetPropertyBlock(block);
        }

        protected override void Declare(Steps s)
        {
            Color yellow = theme.explainYellow;

            // On the way in: the 3 we dove through waits in the dark beyond the page.
            s.At(Begin, () => new[] { new Tween(2f, SetPictureAlpha).After(3f) });

            // "To a computer, a picture is tiny squares." The grid draws itself, and the picture becomes its squares.
            s.At("grid", () =>
            {
                var list = Anim.Each(Anim.Range(0, grid28.Count), 0.015f, i => Anim.Create(grid28, i, 1f));
                for (int i = 0; i < pixels28.Count; i++) list.Add(Anim.FadeTo(pixels28, i, 1f, 0.8f).After(1.0f + (i / 28) * 0.012f));
                list.Add(new Tween(0.8f, a => SetPictureAlpha(1f - a)).After(1.3f));
                return list;
            });

            // "They're called pixels."
            s.AtClause(RideLine.PixelsSquares, 2, () => new[] { Anim.Write(title, 1f) });

            // "Every pixel is just a number." A lens lifts a patch out and writes each square's number.
            s.At("numbers", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < 4; i++) list.Add(Anim.Create(lens, i, 0.6f).After(i * 0.08f));
                for (int k = 0; k < zoom.Count; k++)
                {
                    int z = k;
                    Shape from = pixels28.Home(zoomSource[z]);
                    list.Add(Anim.TransformShape(zoom, z, () => zoom.Home(z).Clone(), 1.1f).After(0.4f + 0.02f * z));
                    PlaceAt(zoom, z, from);
                }

                for (int i = 4; i < lens.Count; i++) list.Add(Anim.Create(lens, i, 0.8f).After(0.9f + (i - 4) * 0.06f));
                for (int v = 0; v < values.Length; v++) list.Add(Anim.Write(values[v], 0.6f).After(1.3f + 0.04f * v));
                return list;
            });

            s.At("zero", () => new[] { Anim.Indicate(zoom, zeroCell, yellow, 1.1f, 1.18f) });
            s.At("one", () => new[] { Anim.Indicate(zoom, oneCell, yellow, 1.1f, 1.18f) });

            // "We'll use seven by seven." The lens closes; every 4×4 block of squares merges into one (its average).
            s.At("shrink", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < lens.Count; i++) list.Add(Anim.FadeOut(lens, i, Vector3.zero, 0.6f));
                for (int k = 0; k < zoom.Count; k++)
                {
                    int z = k;
                    list.Add(Anim.TransformShape(zoom, z, () =>
                    {
                        Shape t = pixels28.Home(zoomSource[z]).Clone();
                        t.alpha = 0f;
                        return t;
                    }, 0.8f));
                }

                foreach (TMP_Text v in values) list.Add(Anim.FadeText(v, 0f, 0.5f));
                for (int i = 0; i < grid28.Count; i++) list.Add(Anim.FadeOut(grid28, i, Vector3.zero, 0.7f).After(0.4f));
                for (int i = 0; i < pixels28.Count; i++)
                {
                    int p = i, block = (i / 28 / 4) * 7 + (i % 28) / 4;
                    list.Add(Anim.TransformShape(pixels28, p, () => pixels7.Home(block).Clone(), 0.9f).After(0.35f + block * 0.006f));
                }

                list.Add(Tween.Call(0.35f + 48 * 0.006f + 0.9f, () =>
                {
                    for (int i = 0; i < pixels28.Count; i++) pixels28[i].alpha = 0f;
                    pixels28.MarkDirty();
                    for (int i = 0; i < pixels7.Count; i++) pixels7[i].alpha = 1f;
                    pixels7.MarkDirty();
                }));
                return list;
            });

            // "That's forty-nine numbers." Each square sends a circle into one tall column: the input layer.
            s.At("column", () =>
            {
                var list = new List<Tween>();
                for (int k = 0; k < column.Count; k++)
                {
                    int c = k;
                    PlaceAt(column, c, pixels7.Home(c));
                    list.Add(Anim.TransformShape(column, c, () => column.Home(c).Clone(), 1.2f).After(0.03f * c));
                }

                list.Add(Anim.Write(count, 0.8f).After(1.4f));
                return list;
            });

            // "It's all Pixel ever sees." A ripple runs down the column.
            s.AtClause(RideLine.PixelsFortyNine, 2, () => Anim.Each(Anim.Range(0, column.Count), 0.025f, i => Anim.Indicate(column, i, yellow, 0.7f, 1.4f)));

            // "Let's fly over them!" The 7×7 lies down and grows into the field; its rows rise (the builder's unfolds).
            s.Unfold("unfold", () =>
            {
                var list = new List<Tween>
                {
                    Anim.FadeText(title, 0f, 0.6f),
                    Anim.FadeText(count, 0f, 0.6f),
                };
                for (int i = 0; i < column.Count; i++) list.Add(Anim.FadeOut(column, i, Vector3.up * 0.3f, 0.7f).After(0.01f * i));
                for (int k = 0; k < pixels7.Count; k++)
                {
                    int p = k, r = k / 7, c = k % 7;
                    float delay = (6 - r) * FlyLagPerRow + c * FlyLagPerColumn;
                    list.Add(Anim.TransformShape(pixels7, p, () => FieldCell(p), FlySeconds).After(delay));
                    list.Add(Anim.FadeTo(pixels7, p, 0f, 0.45f).After(delay + FlySeconds));
                }

                return list;
            });
        }

        /// <summary>Puts a batch's shape where another shape is (same space), keeping its own colours, before it moves home.</summary>
        private static void PlaceAt(ShapeBatch batch, int i, Shape where)
        {
            Shape s = batch[i];
            s.position = where.position;
            s.rotation = where.rotation;
            s.size = where.size;
            s.round = where.round;
            s.color = where.color;
            s.alpha = 1f;
            s.strokeWidth = 0f;
            s.stroke = new Color(1f, 1f, 1f, 0f);
            batch.MarkDirty();
        }

        /// <summary>Square k of the 7×7 lying flat on the field floor, as wide as its pillar, in the 7×7 batch's space.</summary>
        private Shape FieldCell(int k)
        {
            int r = k / 7, c = k % 7;
            Shape t = pixels7[k].Clone();
            Vector3 world = field.TransformPoint(new Vector3((c - 3) * fieldPitch, 0.02f, (3 - r) * fieldPitch));
            Transform b = pixels7.transform;
            t.position = b.InverseTransformPoint(world);
            t.rotation = Quaternion.Inverse(b.rotation) * field.rotation * Quaternion.Euler(90f, 0f, 0f);
            float scale = b.lossyScale.x > 1e-5f ? 1f / b.lossyScale.x : 1f;
            t.size = Vector2.one * pillarWidth * field.lossyScale.x * scale;
            t.alpha = 1f;
            return t;
        }
    }
}
