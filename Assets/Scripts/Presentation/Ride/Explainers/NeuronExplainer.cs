using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>
    /// E2, one neuron (the classic: a neuron holds a number; its weights drawn as a green/red picture over the pixels;
    /// it adds them up and lights up past a line). A circle holds a number and glows brighter as it grows; 49 wires run
    /// to it from a real "1" of the puzzle; the wires turn green (I like ink here) and red (no ink here) with the
    /// puzzle's reference weights, and the weights fold back onto the 7×7 as a green/red pattern; the products fly into
    /// a glass that fills to the real weighted sum; a yellow line marks the real threshold and the neuron lights up.
    /// On {paint} the weight picture flies onto the dash (the grid the rider paints) and the circle grows into the big
    /// neuron ahead. Display only: the sum is computed by the builder from the bake.
    /// </summary>
    public sealed class NeuronExplainer : Explainer
    {
        [SerializeField] private ShapeBatch digit;
        [SerializeField] private ShapeBatch wires;
        [SerializeField] private ShapeBatch weights;
        [SerializeField] private ShapeBatch neuron;
        [SerializeField] private ShapeBatch products;
        [SerializeField] private ShapeBatch glass;
        [SerializeField] private ShapeBatch sparks;
        [SerializeField] private TMP_Text value;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text weightsTitle;
        [SerializeField] private TMP_Text formula;
        [SerializeField] private Transform paintGrid;
        [SerializeField] private Transform bigNeuron;

        [Tooltip("Each wire's weight: +1 green, -1 red, 0 none (the puzzle's reference solution).")]
        [SerializeField] private int[] weightSigns = new int[49];
        [Tooltip("The real weighted sum of the shown digit and the threshold, as glass heights 0..1.")]
        [SerializeField] private float sumLevel = 0.8f;
        [SerializeField] private float lineLevel = 0.5f;
        [SerializeField] private float glassHeight = 1.6f;

        // Glass slots: 0..2 outline (left, right, bottom), 3 fill, 4 the line.
        private const int Fill = 3, Line = 4;

        private Pose _weightsHome;
        private Vector3 _weightsScale;
        private bool _known;

        protected override void Awake()
        {
            if (!_known)
            {
                _known = true;
                _weightsHome = new Pose(weights.transform.localPosition, weights.transform.localRotation);
                _weightsScale = weights.transform.localScale;
            }

            base.Awake();
        }

        protected override void ResetPieces()
        {
            HideAll(digit, false);
            HideAll(wires, true);
            HideAll(weights, false);
            HideAll(neuron, false);
            HideAll(products, false);
            HideAll(glass, true);
            HideAll(sparks, true);
            weights.transform.localPosition = _weightsHome.position;
            weights.transform.localRotation = _weightsHome.rotation;
            weights.transform.localScale = _weightsScale;
            neuron.transform.localScale = Vector3.one;
            foreach (TMP_Text t in new[] { value, title, weightsTitle, formula })
            {
                t.gameObject.SetActive(false);
                t.alpha = 1f;
            }
        }

        private static void HideAll(ShapeBatch b, bool lines)
        {
            b.ResetToHome();
            for (int i = 0; i < b.Count; i++)
            {
                b[i].alpha = 0f;
                if (lines) b[i].grow = 0f;
            }

            b.MarkDirty();
        }

        private void SetValue(float v)
        {
            value.text = v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            Shape body = neuron[0];
            body.color = new Color(body.color.r, body.color.g, body.color.b, Mathf.Lerp(0.08f, 1f, v));
            value.color = v > 0.55f ? theme.voidNavy : theme.explainWhite;
            neuron.MarkDirty();
        }

        protected override void Declare(Steps s)
        {
            Color white = theme.explainWhite, yellow = theme.explainYellow, green = theme.explainGreen, red = theme.explainRed, blue = theme.explainBlue;

            // "This circle is a neuron. It holds one number."
            s.At("neuron", () =>
            {
                var list = new List<Tween>
                {
                    Anim.FadeIn(neuron, 0, Vector3.zero, 0.9f),
                    new Tween(0.01f, _ => SetValue(0.2f)).After(0.6f),
                    Anim.Write(value, 0.6f).After(0.6f),
                    Anim.Write(title, 1f).After(0.3f),
                };
                for (int i = 0; i < digit.Count; i++) list.Add(Anim.FadeIn(digit, i, Vector3.zero, 0.8f).After(1.2f + 0.012f * i));
                return list;
            });

            // "The bigger the number, the brighter it glows."
            s.At("glow", () => new[]
            {
                new Tween(1.6f, a => SetValue(Mathf.Lerp(0.2f, 0.9f, a))),
                Anim.Indicate(neuron, 0, yellow, 1.2f, 1.12f).After(1.4f),
            });

            // "It's wired to every pixel. Each wire has a weight."
            s.At("wires", () =>
            {
                var list = Anim.Each(Anim.Range(0, wires.Count), 0.02f, i => Anim.Create(wires, i, 0.8f));
                list.Add(new Tween(0.8f, a => SetValue(Mathf.Lerp(0.9f, 0f, a))));
                list.Add(Anim.Write(weightsTitle, 1f).After(1.2f));
                return list;
            });

            // "Green weights say: I like ink here." Then "Red weights say: no ink here, please."
            s.At("green", () => Recolour(+1, green));
            s.At("red", () =>
            {
                var list = Recolour(-1, red);
                // Wires with no weight fade back: they add nothing.
                for (int i = 0; i < wires.Count; i++)
                {
                    if (weightSigns[i] != 0) continue;
                    int k = i;
                    list.Add(new Tween(0.6f, a =>
                    {
                        wires[k].alpha = Mathf.Lerp(0.5f, 0.12f, a);
                        wires.MarkDirty();
                    }).After(0.8f));
                }

                return list;
            });

            // "The neuron adds it all up." Each ink × weight flies into the glass; the glass fills to the real sum.
            s.At("add", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < 3; i++) list.Add(Anim.Create(glass, i, 0.6f).After(0.08f * i));
                Shape target = glass.Home(Fill);
                Vector3 mouth = target.position + Vector3.up * (glassHeight * 0.5f);
                int n = 0;
                for (int i = 0; i < products.Count; i++)
                {
                    if (products.Home(i).alpha <= 0f) continue;
                    int k = i;
                    float delay = 0.5f + 0.06f * n++;
                    list.Add(Anim.FadeTo(products, k, 1f, 0.2f).After(delay));
                    list.Add(Anim.TransformShape(products, k, () =>
                    {
                        Shape t = products[k].Clone();
                        t.position = mouth;
                        t.scale = 0.6f;
                        return t;
                    }, 0.9f, Rate.RushInto).After(delay + 0.1f));
                    list.Add(Anim.FadeTo(products, k, 0f, 0.15f).After(delay + 0.95f));
                }

                float fillAt = 0.6f + 0.06f * n * 0.5f;
                list.Add(new Tween(1.4f + 0.03f * n, a => SetFill(a * sumLevel)).After(fillAt));
                list.Add(Anim.Write(formula, 1.4f).After(0.4f));
                return list;
            });

            // "If the total is big enough, it lights up!" The line; the fill is above it; the neuron lights.
            s.At("line", () =>
            {
                var list = new List<Tween>
                {
                    Anim.FadeIn(glass, Line, Vector3.zero, 0.5f),
                    Anim.Create(glass, Line, 0.7f),
                    Anim.Indicate(glass, Fill, yellow, 0.9f, 1.04f).After(0.9f),
                    new Tween(0.7f, a => SetValue(a)).After(1.5f),
                };
                Vector3 at = neuron.Home(0).position + Vector3.back * 0.02f;
                list.AddRange(Anim.Flash(sparks, 0, sparks.Count, at, Quaternion.identity, yellow, 0.75f, 0.8f));
                foreach (Tween t in list.GetRange(list.Count - sparks.Count, sparks.Count)) t.After(1.9f);
                return list;
            });

            // "Your turn!" The weight picture flies onto the dash (the grid the rider paints); the circle grows into
            // the big neuron ahead; the rest folds away.
            s.Unfold("paint", () =>
            {
                var list = new List<Tween>
                {
                    Anim.FadeText(title, 0f, 0.6f),
                    Anim.FadeText(weightsTitle, 0f, 0.6f),
                    Anim.FadeText(formula, 0f, 0.6f),
                    Anim.FadeText(value, 0f, 0.5f),
                };
                for (int i = 0; i < wires.Count; i++) list.Add(Anim.FadeOut(wires, i, Vector3.zero, 0.6f).After(0.006f * i));
                for (int i = 0; i < digit.Count; i++) list.Add(Anim.FadeOut(digit, i, Vector3.zero, 0.6f).After(0.2f));
                for (int i = 0; i < glass.Count; i++) list.Add(Anim.FadeOut(glass, i, Vector3.zero, 0.6f));
                if (paintGrid != null)
                {
                    list.Add(Anim.MoveTo(weights.transform, () => (paintGrid.position + paintGrid.rotation * Vector3.back * 0.02f, paintGrid.rotation,
                        Anim.LocalScaleFor(weights.transform.parent, Vector3.one * GridScale())), 1.5f).After(0.3f));
                    for (int i = 0; i < weights.Count; i++) list.Add(Anim.FadeTo(weights, i, 0f, 0.4f).After(1.8f));
                }

                if (bigNeuron != null)
                {
                    list.Add(Anim.MoveTo(neuron.transform, () => (bigNeuron.position, neuron.transform.rotation,
                        Anim.LocalScaleFor(neuron.transform.parent, Vector3.one * 4.4f / NeuronDiameter())), 1.4f).After(0.2f));
                    list.Add(Anim.FadeTo(neuron, 0, 0f, 0.5f).After(1.4f));
                }

                return list;
            });
        }

        private List<Tween> Recolour(int sign, Color color)
        {
            var list = new List<Tween>();
            int n = 0;
            for (int i = 0; i < wires.Count; i++)
            {
                if (weightSigns[i] != sign) continue;
                int k = i;
                float delay = 0.025f * n++;
                Color from = wires[k].color;
                list.Add(new Tween(0.6f, a =>
                {
                    wires[k].color = Color.Lerp(from, color, a);
                    wires[k].alpha = Mathf.Lerp(0.5f, 0.95f, a);
                    wires.MarkDirty();
                }).After(delay));
                list.Add(Anim.FadeTo(weights, k, 0.62f, 0.6f).After(0.4f + delay));
            }

            return list;
        }

        private void SetFill(float level)
        {
            Shape home = glass.Home(Fill), f = glass[Fill];
            float h = Mathf.Max(0.001f, level * glassHeight);
            f.size = new Vector2(home.size.x, h);
            f.position = home.position + Vector3.down * (glassHeight * 0.5f) + Vector3.up * (h * 0.5f);
            f.alpha = level > 0f ? 0.85f : 0f;
            f.color = level >= lineLevel ? theme.explainYellow : theme.explainBlue;
            glass.MarkDirty();
        }

        // The weight picture's width (7 cells) in the batch's own space, and the dash grid's, so it lands cell on cell.
        private float GridScale()
        {
            Shape a = weights.Home(0), b = weights.Home(6);
            float picture = Mathf.Abs(b.position.x - a.position.x) * 7f / 6f;
            float grid = 7f * 0.058f * paintGrid.lossyScale.x;
            return picture > 1e-4f ? grid / picture : 1f;
        }

        private float NeuronDiameter() => Mathf.Max(0.01f, neuron.Home(0).size.x);
    }
}
