using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using Convergence.Core.Training;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>
    /// E7, right, and training (the classic: train on MNIST's 60,000; test on digits it has never seen). First (v7), what
    /// training learned: on {learned} the neuron the rider painted by hand appears as a 7×7 of GREEN (+) and RED (−); on
    /// {sixteen} the sixteen hidden neurons of the rider's own trained network appear beside it, each one's 49 real weights
    /// as a 7×7 (colour by sign, brightness by size against that neuron's biggest); on {right} they clear for the reading.
    /// Then, beside the
    /// gold network, on {practice}, real test digits stream into it while a counter climbs to the real training count
    /// and the RED loss bar shrinks from the bake's first loss to its last; on {accuracy} a 10×10 grid of a hundred
    /// digits fills, as many GREEN as the real test accuracy, the rest RED.
    /// </summary>
    public sealed class TrainingExplainer : Explainer
    {
        [SerializeField] private ShapeBatch stream;
        [SerializeField] private ShapeBatch lossBar;
        [SerializeField] private ShapeBatch dots;
        [SerializeField] private TMP_Text counter;
        [SerializeField] private TMP_Text lossLabel;
        [SerializeField] private TMP_Text score;
        [SerializeField] private int trainCount = 60000;
        [SerializeField] private float lossEnd = 0.1f;
        [Tooltip("Shapes per streamed digit (a 7×7 picture), and where they fly to (the network), in the stream's space.")]
        [SerializeField] private int perDigit = 49;
        [SerializeField] private Vector3 into;

        [Header("What it learned (v7)")]
        [SerializeField] private Level1Session session;
        [SerializeField] private WeightPaintMoment paint;
        [Tooltip("A backing (shape 0), 16 × 49 squares (hidden neuron j's weight on pixel i is square 1 + j*49 + i), then the rider's painted 49.")]
        [SerializeField] private ShapeBatch learned;
        [SerializeField] private TMP_Text yoursTitle;
        [SerializeField] private TMP_Text learnedTitle;

        protected override void ResetPieces()
        {
            if (learned != null) Hide(learned);
            foreach (TMP_Text t in new[] { yoursTitle, learnedTitle })
            {
                if (t == null) continue;
                t.gameObject.SetActive(false);
                t.alpha = 1f;
            }

            Hide(stream);
            Hide(lossBar);
            Hide(dots);
            foreach (TMP_Text t in new[] { counter, lossLabel, score })
            {
                t.gameObject.SetActive(false);
                t.alpha = 1f;
            }
        }

        private static void Hide(ShapeBatch b)
        {
            b.ResetToHome();
            for (int i = 0; i < b.Count; i++) b[i].alpha = 0f;
            b.MarkDirty();
        }

        private void SetCount(float a)
        {
            int n = Mathf.RoundToInt(Mathf.Lerp(0f, trainCount, a));
            counter.text = n.ToString("N0", CultureInfo.InvariantCulture) + " digits";
        }

        private void SetLoss(float a)
        {
            Shape home = lossBar.Home(0), s = lossBar[0];
            float w = home.size.x * Mathf.Lerp(1f, lossEnd, a);
            s.size = new Vector2(Mathf.Max(0.01f, w), home.size.y);
            s.position = home.position + Vector3.left * (home.size.x - w) * 0.5f;
            s.alpha = 1f;
            lossBar.MarkDirty();
        }

        private const int Pixels = DigitNetwork.Inputs, HiddenCount = DigitNetwork.Hidden, First = 1, Painted = First + HiddenCount * Pixels;

        /// <summary>Colours the squares from the weights as they are now: the rider's painted cells, and the trained W1.</summary>
        private void PaintLearned()
        {
            Color green = theme.explainGreen, red = theme.explainRed, off = new Color(0.25f, 0.27f, 0.32f);
            var painted = paint != null ? paint.Weights : null;
            for (int i = 0; i < Pixels; i++)
            {
                int w = painted != null && i < painted.Count ? painted[i] : 0;
                learned[Painted + i].color = w > 0 ? green : w < 0 ? red : off;
            }

            double[] w1 = session != null && session.Network != null ? session.Network.W1 : null;
            for (int j = 0; j < HiddenCount; j++)
            {
                double biggest = 1e-9;
                for (int i = 0; i < Pixels; i++) biggest = System.Math.Max(biggest, w1 == null ? 0.0 : System.Math.Abs(w1[j * Pixels + i]));
                for (int i = 0; i < Pixels; i++)
                {
                    double w = w1 == null ? 0.0 : w1[j * Pixels + i];
                    float size = Mathf.Pow((float)(System.Math.Abs(w) / biggest), 0.8f);
                    Color c = w >= 0 ? green : red;
                    learned[First + j * Pixels + i].color = Color.Lerp(new Color(0.06f, 0.07f, 0.1f, 1f), c, size);
                }
            }

            learned.MarkDirty();
        }

        protected override void Declare(Steps s)
        {
            Color yellow = theme.explainYellow;

            // "Remember the neuron you painted by hand?" It appears, square by square.
            s.At("learned", () =>
            {
                PaintLearned();
                var list = Anim.Each(Anim.Range(Painted, Pixels), 0.012f, i => Anim.FadeIn(learned, i, Vector3.zero, 0.35f));
                list.Insert(0, Anim.FadeIn(learned, 0, Vector3.zero, 0.5f));
                list.Add(Anim.Write(yoursTitle, 0.6f));
                return list;
            });

            // "Training just painted all sixteen hidden neurons." One after another, each a 7×7 of its real weights.
            s.At("sixteen", () =>
            {
                PaintLearned();
                var list = new List<Tween> { Anim.Write(learnedTitle, 0.7f) };
                for (int j = 0; j < HiddenCount; j++)
                {
                    for (int i = 0; i < Pixels; i++) list.Add(Anim.FadeIn(learned, First + j * Pixels + i, Vector3.zero, 0.4f).After(0.2f + j * 0.12f + (i / 7) * 0.02f));
                }

                return list;
            });

            // "Now, the real test." The weights clear so the network is in full view.
            s.At("right", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < learned.Count; i++) list.Add(Anim.FadeOut(learned, i, Vector3.zero, 0.5f));
                list.Add(Anim.FadeText(yoursTitle, 0f, 0.5f));
                list.Add(Anim.FadeText(learnedTitle, 0f, 0.5f));
                return list;
            });

            // "Pixel practised on sixty thousand digits." Digits stream in; the count climbs; the loss shrinks.
            s.At("practice", () =>
            {
                var list = new List<Tween>
                {
                    new Tween(0.01f, _ => SetCount(0f)),
                    Anim.Write(counter, 0.5f),
                    Anim.Write(lossLabel, 0.5f).After(0.2f),
                    new Tween(0.3f, a => SetLoss(0f)).After(0.2f),
                    new Tween(5f, SetCount, Rate.RushInto).After(0.6f),
                    new Tween(5f, SetLoss, Rate.RushFrom).After(0.6f),
                };
                int digits = stream.Count / perDigit;
                for (int d = 0; d < digits; d++)
                {
                    float delay = 0.6f + d * (4.4f / Mathf.Max(1, digits));
                    int middle = d * perDigit + perDigit / 2;
                    for (int k = 0; k < perDigit; k++)
                    {
                        int i = d * perDigit + k;
                        if (stream.Home(i).alpha <= 0f) continue;
                        list.Add(Anim.FadeTo(stream, i, 1f, 0.15f).After(delay));
                        list.Add(Anim.TransformShape(stream, i, () =>
                        {
                            Shape t = stream[i].Clone();
                            t.position = into + (stream.Home(i).position - stream.Home(middle).position) * 0.3f;
                            t.scale = 0.3f;
                            t.alpha = 0f;
                            return t;
                        }, 1.1f, Rate.RushInto).After(delay + 0.1f));
                    }
                }

                list.Add(Anim.Indicate(lossBar, 0, yellow, 0.9f, 1.1f).After(5.8f));
                return list;
            });

            // "Now Pixel reads ninety-two out of a hundred, even ones it has never seen."
            s.At("accuracy", () =>
            {
                var list = Anim.Each(Anim.Range(0, dots.Count), 0.025f, i => Anim.FadeIn(dots, i, Vector3.zero, 0.35f));
                list.Add(Anim.Write(score, 0.8f).After(dots.Count * 0.025f + 0.2f));
                return list;
            });
        }
    }
}
