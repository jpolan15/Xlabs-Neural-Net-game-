using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>
    /// E4, wrong, and the loss (the classic: random weights give garbage; the cost adds up how far each answer is from
    /// what we wanted). Over the network, a chart of Pixel's ten real answers for the 3 (the untrained network's output,
    /// a bar per digit); its guess is picked out, and it is wrong. GREEN marks what we wanted (the 3 full, the rest
    /// empty); RED bars fill each gap; on {oops} the red bars fly together into one tall bar: the LOSS.
    /// Display only: every length comes from the bake through the builder.
    /// </summary>
    public sealed class LossExplainer : Explainer
    {
        [SerializeField] private ShapeBatch bars;
        [SerializeField] private ShapeBatch marks;
        [SerializeField] private ShapeBatch gaps;
        [SerializeField] private TMP_Text[] labels = new TMP_Text[0];
        [SerializeField] private TMP_Text lossTitle;
        [SerializeField] private int guess;
        [SerializeField] private int wanted = 3;
        [Tooltip("Where each red gap lands in the LOSS stack (same space as the gaps), and its size there.")]
        [SerializeField] private Vector3[] stackPosition = new Vector3[10];
        [SerializeField] private Vector2[] stackSize = new Vector2[10];

        protected override void ResetPieces()
        {
            Hide(bars, false);
            Hide(marks, true);
            Hide(gaps, false);
            var texts = new List<TMP_Text>(labels) { lossTitle };
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

        // A bar grows to the right from its left end.
        private static Tween GrowRight(ShapeBatch b, int i, float duration)
        {
            return new Tween(duration, a =>
            {
                Shape home = b.Home(i), s = b[i];
                float w = home.size.x * a;
                s.size = new Vector2(Mathf.Max(0.0005f, w), home.size.y);
                s.position = home.position + Vector3.left * (home.size.x - w) * 0.5f;
                s.alpha = home.alpha * Mathf.Min(1f, a * 4f);
                b.MarkDirty();
            });
        }

        protected override void Declare(Steps s)
        {
            Color yellow = theme.explainYellow, red = theme.explainRed;

            // "Pixel says..." Ten answers, a bar each; its guess is the longest.
            s.At("guess", () =>
            {
                var list = new List<Tween>();
                for (int k = 0; k < labels.Length; k++) list.Add(Anim.Write(labels[k], 0.4f).After(0.05f * k));
                for (int k = 0; k < bars.Count; k++) list.Add(GrowRight(bars, k, 1.1f).After(0.3f + 0.06f * k));
                list.Add(Anim.Indicate(bars, guess, yellow, 1.1f, 1.12f).After(1.9f));
                return list;
            });

            // "Wrong!" The guess flashes red.
            s.At("wrong", () => new[] { Anim.Indicate(bars, guess, red, 1f, 1.15f), Anim.Indicate(bars, guess, red, 1f, 1.15f).After(1f) });

            // "We wanted the three bright, and all the others dark." GREEN marks: the 3 full, the rest empty.
            s.At("want", () =>
            {
                var list = new List<Tween>
                {
                    Anim.FadeIn(marks, wanted, Vector3.zero, 0.3f),
                    Anim.Create(marks, wanted, 0.6f),
                    Anim.Indicate(marks, wanted, yellow, 0.9f, 1.3f).After(0.7f),
                };
                int n = 0;
                for (int k = 0; k < marks.Count; k++)
                {
                    if (k == wanted) continue;
                    float d = 1.6f + 0.05f * n++;
                    list.Add(Anim.FadeIn(marks, k, Vector3.zero, 0.3f).After(d));
                    list.Add(Anim.Create(marks, k, 0.5f).After(d));
                }

                return list;
            });

            // "Add up how far off each answer is." The gaps fill RED, then fly into one tall bar.
            s.At("oops", () =>
            {
                var list = new List<Tween>();
                for (int k = 0; k < gaps.Count; k++) list.Add(GrowRight(gaps, k, 0.7f).After(0.06f * k));
                for (int k = 0; k < gaps.Count; k++)
                {
                    int i = k;
                    list.Add(Anim.TransformShape(gaps, i, () =>
                    {
                        Shape t = gaps[i].Clone();
                        t.position = stackPosition[i];
                        t.size = stackSize[i];
                        t.alpha = 1f;
                        return t;
                    }, 1.1f).After(1.4f + 0.12f * k));
                }

                return list;
            });

            // "That's the loss."
            s.AtClause(RideLine.WrongLoss, 1, () => new[] { Anim.Write(lossTitle, 0.9f) });

            // "A big loss means a big mistake."
            s.AtClause(RideLine.WrongLoss, 2, () => Anim.Each(Anim.Range(0, gaps.Count), 0.03f, k => Anim.Indicate(gaps, k, yellow, 0.8f, 1.1f)));
        }
    }
}
