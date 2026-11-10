using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 4, explained, on the level's own sentence: "The cat chased the mouse because it was ___". The machine's one job is to guess the
    /// next word ({prompt}); it scores every word it knows and turns the scores into odds, and a skyline of GPT-2's real top guesses
    /// rises, with the shares of the rest written beside it ({bars}); it is not sure ({unsure}) and the word the sentence meant,
    /// "hungry", got only a sliver ({hungry}); it learned by guessing and being surprised, over and over ({learn}). {bet} clears
    /// the board for the game.
    /// </summary>
    public sealed class OddsExplainer : Level2Explainer
    {
        private const int Shown = 6;

        private Level2Kit _kit;
        private ShapeBatch _batch;
        private TextMeshPro _title, _prompt, _rest, _hungry, _unsure, _loopTitle, _web, _level1;
        private TextMeshPro[] _names, _percent, _loop;
        private L2Prediction _p;
        private int _plate, _barFirst, _hungryBar, _loopFirst;
        private float[] _grow;
        private float _hungryGrow, _loopLit;
        private Vector3[] _base;
        private const float Max = 2.3f;
        private static readonly Vector3 Centre = new Vector3(0f, 2.6f, 8f);
        private static readonly Color[] Bars =
        {
            new Color(1f, 0.42f, 0.7f), new Color(0.65f, 0.5f, 1f), new Color(0.3f, 0.85f, 1f), new Color(1f, 0.6f, 0.4f), new Color(0.5f, 1f, 0.7f), new Color(1f, 0.85f, 0.4f),
        };

        protected override void Awake()
        {
            Construct();
            base.Awake();
        }

        private void Construct()
        {
            _kit = Level2Kit.Current;
            Level2Data data = source.Data;
            _p = data.Prediction(Level2Data.ExplainPrompt);
            Transform root = MakeContent("OddsExplain");

            var shapes = new List<Shape>();
            _plate = shapes.Count;
            shapes.Add(Level2Kit.Square(Centre + new Vector3(0f, 0.15f, 0.2f), new Vector2(11.2f, 4.9f), new Color(0.07f, 0.02f, 0.1f, 0.98f), 0.05f, new Color(1f, 0.45f, 0.75f, 0.9f), 0.04f));
            _barFirst = shapes.Count;
            _base = new Vector3[Shown + 1];
            for (int i = 0; i < Shown; i++)
            {
                _base[i] = Centre + new Vector3(-4.2f + i * 1.25f, -1.1f, -0.05f);
                shapes.Add(Level2Kit.Square(_base[i], new Vector2(0.9f, 0.05f), Fx.A(Bars[i], 0.55f), 0.18f, Bars[i], 0.04f));
            }

            _hungryBar = shapes.Count;
            _base[Shown] = Centre + new Vector3(4.3f, -1.1f, -0.05f);
            shapes.Add(Level2Kit.Square(_base[Shown], new Vector2(0.9f, 0.05f), new Color(1f, 0.85f, 0.4f, 0.65f), 0.18f, new Color(1f, 0.85f, 0.4f, 1f), 0.05f));
            // The cycle of learning: guess, measure the surprise, nudge.
            _loopFirst = shapes.Count;
            for (int k = 0; k < 3; k++)
            {
                Vector3 at = Centre + new Vector3(-3.4f + k * 3.4f, -0.1f, -0.05f);
                shapes.Add(Level2Kit.Square(at, new Vector2(2.9f, 1.0f), Fx.A(Bars[k * 2], 0.16f), 0.35f, Fx.A(Bars[k * 2], 0.95f), 0.04f));
            }

            for (int k = 0; k < 2; k++)
            {
                Vector3 a = Centre + new Vector3(-1.75f + k * 3.4f, -0.1f, -0.05f), b = a + new Vector3(0.5f, 0f, 0f);
                shapes.Add(Level2Kit.Line(a, b, 0.06f, new Color(1f, 1f, 1f, 0.9f)));
                shapes.Add(Level2Kit.Line(b, b + new Vector3(-0.18f, 0.16f, 0f), 0.06f, new Color(1f, 1f, 1f, 0.9f)));
                shapes.Add(Level2Kit.Line(b, b + new Vector3(-0.18f, -0.16f, 0f), 0.06f, new Color(1f, 1f, 1f, 0.9f)));
            }

            foreach (Shape s in shapes) s.alpha = 0f;
            _batch = _kit.Batch(root, "Skyline", shapes, 1);

            _title = _kit.Text(root, "Title", "PREDICTION", 0.42f, new Color(1f, 0.6f, 0.82f), Centre + new Vector3(0f, 2.95f, 0f), 7f, TextAlignmentOptions.Center, false, true);
            _title.characterSpacing = 8f;
            _prompt = _kit.Text(root, "Prompt", _p.prompt + " ___", 0.44f, Color.white, Centre + new Vector3(0f, 2.05f, -0.1f), 10.4f);
            _names = new TextMeshPro[Shown];
            _percent = new TextMeshPro[Shown];
            for (int i = 0; i < Shown; i++)
            {
                _names[i] = _kit.Text(_batch.transform.parent, "Name" + i, Level2Data.Clean(_p.top[i].word), 0.28f, Color.white, _base[i] + new Vector3(0f, -0.38f, -0.05f), 1.4f);
                _percent[i] = _kit.Text(_batch.transform.parent, "Percent" + i, (_p.top[i].p * 100f).ToString("0.0") + "%", 0.3f, Bars[i], _base[i] + new Vector3(0f, 0.45f, -0.05f), 1.4f);
            }

            float rest = 1f;
            for (int i = 0; i < Shown; i++) rest -= _p.top[i].p;
            _rest = _kit.Text(root, "Rest", "...AND 50,000 OTHER WORDS SHARE THE LAST " + Mathf.RoundToInt(rest * 100f) + "%", 0.26f, new Color(0.85f, 0.8f, 0.95f), Centre + new Vector3(0f, -1.95f, -0.1f), 10.4f);
            _unsure = _kit.Text(root, "Unsure", "NO CLEAR\nWINNER", 0.44f, new Color(1f, 0.85f, 0.45f), Centre + new Vector3(4.0f, 0.75f, -0.1f), 3.4f); // in the empty column the hungry bar takes next
            _hungry = _kit.Text(root, "Hungry", Level2Data.Clean(_p.truth) + "\n" + (_p.truthP * 100f).ToString("0.0") + "%", 0.3f, new Color(1f, 0.85f, 0.4f), _base[Shown] + new Vector3(0f, 1.0f, -0.05f), 2.4f);

            _loop = new TextMeshPro[3];
            string[] steps = { "GUESS THE\nNEXT WORD", "HOW SURPRISED\nWAS IT?", "NUDGE THE\nWEIGHTS" };
            for (int k = 0; k < 3; k++) _loop[k] = _kit.Text(root, "Loop" + k, steps[k], 0.3f, Color.white, Centre + new Vector3(-3.4f + k * 3.4f, -0.1f, -0.1f), 2.8f);
            _loopTitle = _kit.Text(root, "LoopTitle", "AGAIN AND AGAIN", 0.34f, new Color(1f, 0.85f, 0.45f), Centre + new Vector3(0f, 1.15f, -0.1f), 7f);
            // The two tricks were taught in Level 1: say so on the board (owner playtest 2026-11-10).
            _level1 = _kit.Text(root, "Level1", "YOU LEARNED THESE TWO IN LEVEL 1, TRAINING PIXEL", 0.26f, new Color(0.45f, 1f, 0.75f), Centre + new Vector3(0f, -1.95f, -0.1f), 12f);
            _web = _kit.Text(root, "Web", "MILLIONS OF WEB PAGES   -   BACKPROPAGATION   -   GRADIENT DESCENT", 0.24f, new Color(0.85f, 0.9f, 1f), Centre + new Vector3(0f, -1.45f, -0.1f), 12f);
            _grow = new float[Shown];
        }

        protected override void ResetPieces()
        {
            for (int i = 0; i < _batch.Count; i++) _batch[i].alpha = 0f;
            _batch.MarkDirty();
            foreach (TextMeshPro t in Texts())
            {
                t.gameObject.SetActive(false);
                t.alpha = 1f;
            }

            for (int i = 0; i < Shown; i++) _grow[i] = 0f;
            _hungryGrow = _loopLit = 0f;
        }

        private IEnumerable<TextMeshPro> Texts()
        {
            yield return _title; yield return _prompt; yield return _rest; yield return _hungry; yield return _unsure; yield return _loopTitle; yield return _web; yield return _level1;
            foreach (TextMeshPro t in _names) yield return t;
            foreach (TextMeshPro t in _percent) yield return t;
            foreach (TextMeshPro t in _loop) yield return t;
        }

        protected override void Animate(float deltaTime)
        {
            for (int i = 0; i < Shown; i++)
            {
                Shape bar = _batch[_barFirst + i];
                float h = Mathf.Max(0.05f, _p.top[i].p / Mathf.Max(0.01f, _p.top[0].p) * Max * _grow[i]);
                bar.size = new Vector2(0.9f, h);
                bar.position = _base[i] + new Vector3(0f, h * 0.5f, 0f);
                _percent[i].transform.localPosition = _base[i] + new Vector3(0f, h + 0.32f, -0.05f);
            }

            Shape hb = _batch[_hungryBar];
            float hh = Mathf.Max(0.05f, _p.truthP / Mathf.Max(0.01f, _p.top[0].p) * Max * _hungryGrow);
            hb.size = new Vector2(0.9f, hh);
            hb.position = _base[Shown] + new Vector3(0f, hh * 0.5f, 0f);
            _hungry.transform.localPosition = _base[Shown] + new Vector3(0f, hh + 0.55f, -0.05f);
            for (int k = 0; k < 3; k++)
            {
                Shape box = _batch[_loopFirst + k];
                float lit = Mathf.Clamp01(_loopLit * 3f - k);
                box.scale = 1f + 0.06f * lit * Mathf.Sin(Time.time * 6f);
                box.color = Fx.A(Bars[k * 2], 0.16f + 0.3f * lit);
            }

            _batch.MarkDirty();
        }

        private Tween Param(float duration, System.Action<float> apply) => new Tween(duration, apply, Rate.Smooth);

        protected override void Declare(Steps s)
        {
            Color gold = new Color(1f, 0.82f, 0.45f);

            // "After all that, the machine has one job: guess the next word."
            s.At("prompt", () => new List<Tween>
            {
                Anim.FadeIn(_batch, _plate, Vector3.zero, 0.6f),
                Anim.Write(_title, 0.7f),
                Anim.Write(_prompt, 2.2f).After(0.5f),
            });

            // "It scores every word it knows... and turns the scores into odds. These are the numbers GPT-2 really gave."
            s.At("bars", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < Shown; i++)
                {
                    int k = i;
                    float d = 0.25f * i;
                    list.Add(Anim.FadeIn(_batch, _barFirst + i, Vector3.zero, 0.2f).After(d));
                    list.Add(Param(1.1f, a => _grow[k] = a).After(d));
                    list.Add(Anim.Write(_names[i], 0.4f).After(d + 0.2f));
                    list.Add(Anim.Write(_percent[i], 0.4f).After(d + 0.6f));
                }

                list.Add(Anim.Write(_rest, 1.4f).After(2.2f));
                return list;
            });

            // "It isn't sure. It never is."
            s.At("unsure", () => new List<Tween> { Anim.Write(_unsure, 0.9f) });

            // "The word we meant, hungry, only got one percent."
            s.At("hungry", () => new List<Tween>
            {
                Anim.FadeText(_unsure, 0f, 0.4f),
                Anim.FadeIn(_batch, _hungryBar, Vector3.zero, 0.3f),
                Param(1.0f, a => _hungryGrow = a),
                Anim.Write(_hungry, 0.7f).After(0.5f),
                Anim.Indicate(_batch, _hungryBar, gold, 1.2f, 1.2f).After(1.0f),
            });

            // "GPT-2 read millions of web pages, guessing every next word, and nudging its weights each time it was surprised."
            s.At("learn", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < Shown; i++)
                {
                    list.Add(Anim.FadeTo(_batch, _barFirst + i, 0f, 0.5f));
                    list.Add(Anim.FadeText(_names[i], 0f, 0.4f));
                    list.Add(Anim.FadeText(_percent[i], 0f, 0.4f));
                }

                list.Add(Anim.FadeTo(_batch, _hungryBar, 0f, 0.5f));
                list.Add(Anim.FadeText(_hungry, 0f, 0.4f));
                list.Add(Anim.FadeText(_rest, 0f, 0.4f));
                list.Add(Anim.FadeText(_prompt, 0f, 0.4f));
                list.Add(Anim.Write(_loopTitle, 0.8f).After(0.5f));
                for (int k = 0; k < 3; k++)
                {
                    list.Add(Anim.FadeIn(_batch, _loopFirst + k, Vector3.zero, 0.5f).After(0.7f + 0.6f * k));
                    list.Add(Anim.Write(_loop[k], 0.6f).After(0.8f + 0.6f * k));
                }

                for (int k = 0; k < 6; k++) list.Add(Anim.FadeIn(_batch, _loopFirst + 3 + k, Vector3.zero, 0.3f).After(1.6f + 0.3f * (k / 3)));
                list.Add(Param(5f, a => _loopLit = a * 3f).After(1.2f));
                list.Add(Anim.Write(_web, 1.6f).After(3.4f));
                return list;
            });

            // "Then backpropagation and gradient descent nudged its weights: the same two tricks you used to train Pixel in Level 1."
            s.At("level1", () => new List<Tween> { Anim.Write(_level1, 1.2f), Anim.Indicate(_batch, _loopFirst + 2, new Color(0.45f, 1f, 0.75f), 1.2f, 1.12f) });

            // "Your turn: place your chips."
            s.At("bet", () =>
            {
                var list = new List<Tween> { Anim.FadeTo(_batch, _plate, 0f, 0.7f) };
                for (int i = 1; i < _batch.Count; i++) list.Add(Anim.FadeTo(_batch, i, 0f, 0.6f));
                foreach (TextMeshPro t in Texts()) if (t.gameObject.activeSelf) list.Add(Anim.FadeText(t, 0f, 0.6f));
                return list;
            });
        }
    }
}
