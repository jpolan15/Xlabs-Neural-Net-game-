using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>
    /// E6, gradient descent (the classic: a ball rolling down a curve; the step size; overshooting). At the rim, in the
    /// dark, a loss curve draws itself (a hill: high is lots of mistakes, low is hardly any) and a ball sits high on
    /// it. A YELLOW arrow shows which way is down and the ball takes a step (GRADIENT DESCENT). A big step flies past
    /// the bottom up the far side (RED); small steps settle in (GREEN). On {landscape} the curve sweeps away and the
    /// Valley rises (the builder's unfolds): the real hill the ride drops into.
    /// </summary>
    public sealed class DescentExplainer : Explainer
    {
        [SerializeField] private ShapeBatch curve;
        [SerializeField] private ShapeBatch ball;
        [SerializeField] private ShapeBatch marks;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text high;
        [SerializeField] private TMP_Text low;
        [Tooltip("The curve: y = height * (x / halfWidth)^2 shaped, sampled by the builder; the ball rides on it.")]
        [SerializeField] private float halfWidth = 2.6f;
        [SerializeField] private float height = 1.9f;
        [SerializeField] private float startX = -2.2f;

        /// <summary>Strokes in the big step's arc (the builder draws that many).</summary>
        public const int ArcStrokes = 8;

        // Marks: 0..2 the slope arrow (shaft, head, head), then the big step's arc strokes, then the small steps' dots.
        private const int Slope = 0, BigArc = 3, Dots = BigArc + ArcStrokes;
        private float _x;

        protected override void ResetPieces()
        {
            Hide(curve, true);
            Hide(ball, false);
            Hide(marks, true);
            foreach (TMP_Text t in new[] { title, high, low })
            {
                t.gameObject.SetActive(false);
                t.alpha = 1f;
            }

            _x = startX;
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

        /// <summary>The curve's height at x (the same shape the builder samples): a bowl with a gentle shoulder.</summary>
        public static float Height(float x, float halfWidth, float height)
        {
            float u = x / halfWidth;
            return height * (u * u * (0.75f + 0.25f * u * u)) - height * 0.5f;
        }

        private Vector3 OnCurve(float x) => new Vector3(x, Height(x, halfWidth, height) + 0.13f, -0.02f);

        private void PutBall(float x)
        {
            _x = x;
            Shape s = ball[0];
            s.position = OnCurve(x);
            ball.MarkDirty();
        }

        // A hop from the ball's x to x1 along an arc (up and over), as one tween.
        private Tween Hop(float x1, float duration, float lift)
        {
            float x0 = 0f;
            return new Tween(duration, a =>
            {
                float x = Mathf.Lerp(x0, x1, a);
                PutBall(x);
                Shape s = ball[0];
                s.position += Vector3.up * lift * Mathf.Sin(a * Mathf.PI);
                ball.MarkDirty();
            }, Rate.Smooth, begin: () => x0 = _x);
        }

        protected override void Declare(Steps s)
        {
            Color yellow = theme.explainYellow, red = theme.explainRed, green = theme.explainGreen;

            // "Think of the loss as a hill." The curve draws itself.
            s.At("hill", () => Anim.Each(Anim.Range(0, curve.Count), 0.03f, i => Anim.Create(curve, i, 0.25f)));

            // "High up means lots of mistakes. Down low means hardly any." The ball sits high.
            s.At("ball", () => new[]
            {
                new Tween(0.01f, _ => PutBall(startX)),
                Anim.FadeIn(ball, 0, Vector3.up * 0.4f, 0.8f),
                Anim.Write(high, 0.8f).After(0.4f),
                Anim.Write(low, 0.8f).After(2.2f),
            });

            // "Feel which way is down, and take a step." The slope arrow, then one step.
            s.At("slope", () =>
            {
                var list = new List<Tween>();
                for (int i = Slope; i < Slope + 3; i++)
                {
                    list.Add(Anim.FadeIn(marks, i, Vector3.zero, 0.2f).After(i == Slope ? 0f : 0.45f));
                    list.Add(Anim.Create(marks, i, i == Slope ? 0.6f : 0.3f).After(i == Slope ? 0f : 0.45f));
                }

                list.Add(Hop(startX + 0.55f, 0.8f, 0.12f).After(1.4f));
                for (int i = Slope; i < Slope + 3; i++) list.Add(Anim.FadeTo(marks, i, 0f, 0.5f).After(2.4f));
                return list;
            });

            // "That's gradient descent."
            s.AtClause(RideLine.DescentStep, 2, () => new[] { Anim.Write(title, 1.2f) });

            // "Take too big a step, and you fly past the bottom." A big RED arc up the far side.
            s.At("bigstep", () =>
            {
                var list = new List<Tween> { Hop(1.9f, 1.3f, 0.9f) };
                for (int i = BigArc; i < Dots; i++)
                {
                    float d = 0.1f + 1.1f * (i - BigArc) / ArcStrokes;
                    list.Add(Anim.FadeIn(marks, i, Vector3.zero, 0.1f).After(d));
                    list.Add(Anim.Create(marks, i, 1.1f / ArcStrokes).After(d));
                }

                list.Add(Anim.Indicate(ball, 0, red, 0.8f, 1.3f).After(1.3f));
                return list;
            });

            // "Small steps settle in." Little GREEN hops down to the bottom.
            s.At("smallsteps", () =>
            {
                var list = new List<Tween>();
                float[] xs = { 1.35f, 0.9f, 0.55f, 0.3f, 0.12f, 0.03f };
                for (int k = 0; k < xs.Length; k++)
                {
                    list.Add(Hop(xs[k], 0.45f, 0.06f).After(0.5f * k));
                    if (Dots + k < marks.Count)
                    {
                        int i = Dots + k;
                        list.Add(Anim.FadeIn(marks, i, Vector3.zero, 0.3f).After(0.5f * k + 0.45f));
                    }
                }

                for (int i = BigArc; i < Dots; i++) list.Add(Anim.FadeTo(marks, i, 0.25f, 0.6f));
                list.Add(Anim.Indicate(ball, 0, green, 0.9f, 1.3f).After(0.5f * xs.Length));
                return list;
            });

            // "And now, the real hill." The curve sweeps away and the Valley rises (the builder's unfolds).
            s.Unfold("landscape", () =>
            {
                var list = new List<Tween>
                {
                    Anim.FadeText(title, 0f, 0.6f),
                    Anim.FadeText(high, 0f, 0.6f),
                    Anim.FadeText(low, 0f, 0.6f),
                    Anim.FadeOut(ball, 0, Vector3.down * 0.6f, 0.9f),
                };
                for (int i = 0; i < curve.Count; i++) list.Add(Anim.FadeOut(curve, i, Vector3.down * 0.8f, 0.9f).After(0.015f * i));
                for (int i = 0; i < marks.Count; i++) list.Add(Anim.FadeTo(marks, i, 0f, 0.5f));
                return list;
            });
        }
    }
}
