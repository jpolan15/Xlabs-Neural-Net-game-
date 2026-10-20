using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>
    /// E7, right, and training (the classic: train on MNIST's 60,000; test on digits it has never seen). Beside the
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

        protected override void ResetPieces()
        {
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

        protected override void Declare(Steps s)
        {
            Color yellow = theme.explainYellow;

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
