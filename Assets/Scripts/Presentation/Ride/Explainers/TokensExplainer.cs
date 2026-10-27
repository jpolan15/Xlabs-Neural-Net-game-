using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>
    /// E8 opens with the recap (owner, 2026-10-27): on {recap} the five stops come in as dim numbered rows, and each
    /// lights on {r1}..{r5} as the narrator names it, with what it showed; they step aside on {sentence}.
    /// Then tokens: the bridge to Level 2 (the classic: a sentence split into tokens, each token a list of numbers, a
    /// network guessing the next word). "The cat sat on the" writes itself; on {tokens} it splits into coloured chips;
    /// on {vectors} each chip drops a column of numbers drawn as squares (dark to bright, just like pixels); on
    /// {predict} the columns flow into a little network; on {mat} the next word, "mat", arrives with a sparkle.
    /// An illustration: the numbers are made up for the picture, and the narration says so by saying "just like".
    /// </summary>
    public sealed class TokensExplainer : Explainer
    {
        [SerializeField] private TMP_Text sentence;
        [SerializeField] private ShapeBatch chips;
        [SerializeField] private TMP_Text[] words = new TMP_Text[0];
        [SerializeField] private ShapeBatch vectors;
        [SerializeField] private ShapeBatch net;
        [SerializeField] private ShapeBatch sparks;
        [SerializeField] private int perToken = 6;
        [Tooltip("Where the numbers flow to: the little network's first layer (vectors' space).")]
        [SerializeField] private Vector3 netInput;

        [Header("Recap (owner, 2026-10-27)")]
        [Tooltip("One highlight bar per stop, in the ride's order.")]
        [SerializeField] private ShapeBatch recap;
        [SerializeField] private TMP_Text recapTitle;
        [Tooltip("\"1  PIXELS\" and so on, one per bar.")]
        [SerializeField] private TMP_Text[] recapNames = new TMP_Text[0];
        [Tooltip("What each stop showed, one per bar.")]
        [SerializeField] private TMP_Text[] recapLines = new TMP_Text[0];

        // Chips: one per token, then the answer chip last.
        private int Answer => chips.Count - 1;

        protected override void ResetPieces()
        {
            Hide(chips, false);
            Hide(vectors, false);
            Hide(net, true);
            Hide(sparks, true);
            if (recap != null) Hide(recap, false);
            var texts = new List<TMP_Text>(words) { sentence };
            texts.AddRange(recapNames);
            texts.AddRange(recapLines);
            if (recapTitle != null) texts.Add(recapTitle);
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

        protected override void Declare(Steps s)
        {
            Color yellow = theme.explainYellow;
            int rows = recap != null ? Mathf.Min(recap.Count, Mathf.Min(recapNames.Length, recapLines.Length)) : 0;

            // "Quick recap. Five stops." The five rows come in dim, the way the intro's roadmap did.
            if (rows > 0)
            {
                s.At("recap", () =>
                {
                    var list = new List<Tween>();
                    if (recapTitle != null) list.Add(Anim.Write(recapTitle, 0.6f));
                    for (int k = 0; k < rows; k++)
                    {
                        list.Add(Anim.FadeIn(recap, k, Vector3.down * 0.1f, 0.5f).After(0.1f + 0.08f * k));
                        list.Add(Anim.FadeTo(recap, k, 0.35f, 0.3f).After(0.65f + 0.08f * k));
                        list.Add(Anim.Write(recapNames[k], 0.4f).After(0.1f + 0.08f * k));
                        list.Add(Anim.FadeText(recapNames[k], 0.4f, 0.3f).After(0.65f + 0.08f * k));
                    }

                    return list;
                });

                // "One, pixels: your three became forty-nine numbers." Each stop lights as it is named, and says what it showed.
                for (int k = 0; k < rows; k++)
                {
                    int row = k;
                    s.At("r" + (k + 1), () => new[]
                    {
                        Anim.FadeTo(recap, row, 1f, 0.3f),
                        Anim.Indicate(recap, row, yellow, 0.8f, 1.04f),
                        Anim.FadeText(recapNames[row], 1f, 0.3f),
                        Anim.Write(recapLines[row], 0.8f),
                    });
                }
            }

            // "Language works the same way." The recap steps aside for the sentence.
            s.At("sentence", () =>
            {
                var list = new List<Tween>();
                for (int k = 0; k < rows; k++)
                {
                    list.Add(Anim.FadeOut(recap, k, Vector3.up * 0.2f, 0.5f));
                    list.Add(Anim.FadeText(recapNames[k], 0f, 0.5f));
                    list.Add(Anim.FadeText(recapLines[k], 0f, 0.5f));
                }

                if (rows > 0 && recapTitle != null) list.Add(Anim.FadeText(recapTitle, 0f, 0.5f));
                list.Add(Anim.Write(sentence, 1.4f).After(rows > 0 ? 0.4f : 0f));
                return list;
            });

            // "We chop a sentence into pieces called tokens." The sentence becomes chips, one per token.
            s.At("tokens", () =>
            {
                var list = new List<Tween> { Anim.FadeText(sentence, 0f, 0.5f) };
                for (int k = 0; k < Answer; k++)
                {
                    list.Add(Anim.FadeIn(chips, k, Vector3.down * 0.15f, 0.6f).After(0.3f + 0.12f * k));
                    list.Add(Anim.Write(words[k], 0.4f).After(0.35f + 0.12f * k));
                }

                return list;
            });

            // "Each token becomes a list of numbers, just like pixels." Each chip drops a column of squares.
            s.At("vectors", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < vectors.Count; i++)
                {
                    int token = i / perToken, row = i % perToken;
                    list.Add(Anim.FadeIn(vectors, i, Vector3.up * 0.25f, 0.5f).After(0.15f * token + 0.06f * row));
                }

                return list;
            });

            // "A giant network guesses the next word." The numbers flow into a little network; it lights up.
            s.At("predict", () =>
            {
                var list = Anim.Each(Anim.Range(0, net.Count), 0.03f, i => Anim.Create(net, i, 0.5f));
                for (int i = 0; i < net.Count; i++) list.Add(Anim.FadeIn(net, i, Vector3.zero, 0.3f).After(0.03f * i));
                for (int i = 0; i < vectors.Count; i++)
                {
                    int k = i;
                    list.Add(Anim.TransformShape(vectors, k, () =>
                    {
                        Shape t = vectors[k].Clone();
                        t.position = netInput + new Vector3(0f, (k % perToken - perToken * 0.5f) * 0.05f, 0f);
                        t.scale = 0.5f;
                        t.alpha = 0f;
                        return t;
                    }, 1.1f, Rate.RushInto).After(1.0f + 0.02f * k));
                }

                return list;
            });

            // "That's how a chatbot talks." The next word arrives.
            s.At("mat", () =>
            {
                var list = new List<Tween>
                {
                    Anim.FadeIn(chips, Answer, Vector3.left * 0.6f, 0.8f),
                    Anim.Write(words[Answer], 0.5f).After(0.3f),
                    Anim.Indicate(chips, Answer, yellow, 1f, 1.15f).After(0.9f),
                };
                Vector3 at = chips.Home(Answer).position + Vector3.back * 0.02f;
                foreach (Tween t in Anim.Flash(sparks, 0, sparks.Count, at, Quaternion.identity, yellow, 0.5f, 0.7f)) list.Add(t.After(0.8f));
                return list;
            });
        }
    }
}
