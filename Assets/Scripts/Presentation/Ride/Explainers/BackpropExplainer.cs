using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>
    /// E5, backpropagation (the classic: 3Blue1Brown's "nudges"). Pixel's ten answers come back as E4's chart, and an
    /// arrow on each bar shows what that answer wishes: the 3 a GREEN arrow to brighter, the others RED arrows to
    /// dimmer, each as long as the real gradient at that answer (the bake's untrained output minus what we wanted).
    /// While the card is up the network's layer names are off (they showed through it). On {back} the card fades and
    /// the wish flows backwards:
    /// sparks run from the answers to the hidden layer and on to the pixels, the network's real blame lights up
    /// behind them (BlameView, the Show unfold), and BACKPROPAGATION writes itself above.
    /// </summary>
    public sealed class BackpropExplainer : Explainer
    {
        [SerializeField] private ShapeBatch card;
        [SerializeField] private ShapeBatch bars;
        [SerializeField] private TMP_Text[] labels = new TMP_Text[0];
        [SerializeField] private ShapeBatch arrows;
        [SerializeField] private ShapeBatch sparks;
        [SerializeField] private TMP_Text title;
        [Tooltip("The network's layer names: they showed through the card (owner, 2026-10-27), so they are off while it is up.")]
        [SerializeField] private GameObject coveredSigns;
        [SerializeField] private int wanted = 3;
        [Tooltip("Where the sparks go: the 16 hidden neurons, then the 49 pixels (sparks' space).")]
        [SerializeField] private Vector3[] hiddenAt = new Vector3[16];
        [SerializeField] private Vector3[] inputAt = new Vector3[49];
        [SerializeField] private Vector3 outputsAt;

        // Arrows: three lines per answer (shaft, two head strokes).
        private int Shaft(int k) => k * 3;

        protected override void ResetPieces()
        {
            Hide(card);
            Hide(bars);
            Hide(arrows);
            Hide(sparks);
            foreach (TMP_Text t in labels)
            {
                t.gameObject.SetActive(false);
                t.alpha = 1f;
            }

            title.gameObject.SetActive(false);
            title.alpha = 1f;
            if (coveredSigns != null) coveredSigns.SetActive(true);
        }

        private static void Hide(ShapeBatch b)
        {
            b.ResetToHome();
            for (int i = 0; i < b.Count; i++)
            {
                b[i].alpha = 0f;
                b[i].grow = 0f;
            }

            b.MarkDirty();
        }

        private List<Tween> Arrow(int k, float delay)
        {
            var list = new List<Tween>();
            for (int j = 0; j < 3; j++)
            {
                int i = Shaft(k) + j;
                list.Add(Anim.FadeIn(arrows, i, Vector3.zero, 0.2f).After(delay + (j == 0 ? 0f : 0.5f)));
                list.Add(Anim.Create(arrows, i, j == 0 ? 0.7f : 0.35f).After(delay + (j == 0 ? 0f : 0.5f)));
            }

            return list;
        }

        protected override void Declare(Steps s)
        {
            Color yellow = theme.explainYellow;

            // "The three wants to be brighter." The answers come back as bars; the 3's arrow points to brighter.
            s.At("wish", () =>
            {
                var list = new List<Tween> { Anim.FadeIn(card, 0, Vector3.zero, 0.5f), new Tween(0.01f, _ => SetSigns(false)) };
                for (int k = 0; k < bars.Count; k++) list.Add(Anim.FadeIn(bars, k, Vector3.zero, 0.5f).After(0.05f * k));
                for (int k = 0; k < labels.Length; k++) list.Add(Anim.Write(labels[k], 0.4f).After(0.05f * k));
                list.AddRange(Arrow(wanted, 0.8f));
                list.Add(Anim.Indicate(arrows, Shaft(wanted), yellow, 0.9f, 1.2f).After(1.9f));
                return list;
            });

            // "The one wants to be dimmer." (and every other answer too, by as much as it is wrongly lit)
            s.At("dim", () =>
            {
                var list = new List<Tween>();
                int n = 0;
                for (int k = 0; k < 10; k++)
                {
                    if (k == wanted) continue;
                    list.AddRange(Arrow(k, 0.08f * n++));
                }

                return list;
            });

            // "That wish flows backwards, through every wire." The card steps aside (it hid the network, owner 2026-10-27)
            // and sparks run back to the hidden layer, then to the pixels.
            s.Unfold("back", () =>
            {
                var list = StepAside(0.6f);
                list.Add(new Tween(0.01f, _ => SetSigns(true)).After(0.6f));
                for (int j = 0; j < 16; j++)
                {
                    int i = j;
                    float d = 0.04f * j;
                    list.Add(Spark(i, outputsAt, hiddenAt[j], d, 1.2f));
                }

                for (int p = 0; p < 49; p++)
                {
                    int i = 16 + p;
                    Vector3 from = hiddenAt[(p * 7) % 16];
                    list.Add(Spark(i, from, inputAt[p], 1.4f + 0.02f * p, 1.3f));
                }

                return list;
            });

            // "That's backpropagation."
            s.AtClause(RideLine.BackFlows, 2, () => new[] { Anim.Write(title, 1.3f) });

            // "Grab the hot wires, and nudge them!" The title and arrows step aside for the play.
            s.At("grab", () =>
            {
                var list = StepAside(0.8f);
                list.Add(Anim.FadeText(title, 0f, 0.8f));
                return list;
            });
        }

        /// <summary>The card, its bars, arrows and digits fade out.</summary>
        private List<Tween> StepAside(float seconds)
        {
            var list = new List<Tween> { Anim.FadeTo(card, 0, 0f, seconds) };
            for (int i = 0; i < arrows.Count; i++) list.Add(Anim.FadeTo(arrows, i, 0f, seconds));
            for (int i = 0; i < bars.Count; i++) list.Add(Anim.FadeTo(bars, i, 0f, seconds));
            foreach (TMP_Text t in labels) list.Add(Anim.FadeText(t, 0f, seconds));
            return list;
        }

        private void SetSigns(bool on)
        {
            if (coveredSigns != null) coveredSigns.SetActive(on);
        }

        private Tween Spark(int i, Vector3 from, Vector3 to, float delay, float duration)
        {
            return new Tween(duration, a =>
            {
                Shape s = sparks[i];
                Vector3 head = Vector3.Lerp(from, to, a), tail = Vector3.Lerp(from, to, Mathf.Max(0f, a - 0.18f));
                s.position = tail;
                s.end = head;
                s.grow = 1f;
                s.alpha = Mathf.Sin(a * Mathf.PI);
                sparks.MarkDirty();
            }, Rate.Smooth).After(delay);
        }
    }
}
