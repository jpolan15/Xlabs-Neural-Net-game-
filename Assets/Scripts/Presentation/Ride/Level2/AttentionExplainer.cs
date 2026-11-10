using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 3, explained, on one of GPT-2's own sentences: "The fluffy blue creature roamed the quiet forest". The words appear ({sentence});
    /// "creature" looks back, and a ribbon of light runs to each earlier word ({look}); each word gets a share and the shares are
    /// written on the ribbons and add up to a hundred percent ({shares}); "creature" mixes in each word's meaning by its share, and turns
    /// the colours of the words it looked at ({blend}); the numbers are from a real attention head of GPT-2 ({real}). {spot} clears
    /// the board for the game. v3 (owner playtest 2026-11-10: clear for someone who has never heard of attention): the query word asks
    /// its question out loud ({look}), the shares are slices of one bar that fills to 100% ({shares}), and a three-step recap ends the
    /// stop ({recap}).
    /// </summary>
    public sealed class AttentionExplainer : Level2Explainer
    {
        private Level2Kit _kit;
        private ShapeBatch _plate;
        private RibbonField _arcs;
        private GlowField _glows;
        private TextMeshPro _title, _badge, _sum, _note, _blendNote, _question, _recap;
        private TextMeshPro[] _sliceText;
        private int _sliceFirst;
        private static readonly Color[] SliceColor = { new Color(0.55f, 0.65f, 0.9f), new Color(1f, 0.6f, 0.85f), new Color(0.35f, 0.75f, 1f), new Color(0.6f, 1f, 0.7f), new Color(1f, 0.8f, 0.4f) };
        private TextMeshPro[] _words, _percent;
        private Vector3[] _at;
        private float[] _width;
        private L2Attention _round;
        private float _draw, _numbers, _light, _blend;
        private static readonly Vector3 Centre = new Vector3(0f, 2.6f, 8f);

        protected override void Awake()
        {
            Construct();
            base.Awake();
        }

        private void Construct()
        {
            _kit = Level2Kit.Current;
            Level2Data data = source.Data;
            _round = data.Attention(Level2Data.ExplainAttention);
            Transform root = MakeContent("AttentionExplain");

            var shapes = new List<Shape> { Level2Kit.Square(Centre + new Vector3(0f, 0.05f, 0.2f), new Vector2(11.6f, 4.9f), new Color(0.02f, 0.05f, 0.12f, 0.98f), 0.05f, new Color(0.25f, 0.85f, 1f, 0.9f), 0.04f) };

            // The shares as slices of one bar: together they are the whole bar, 100%.
            _sliceFirst = shapes.Count;
            _sliceText = new TextMeshPro[_round.query];
            const float barWidth = 7.2f;
            float sx = -barWidth * 0.5f;
            for (int i = 0; i < _round.query; i++)
            {
                float w = barWidth * _round.weights[i];
                Vector3 at = Centre + new Vector3(sx + w * 0.5f, -1.15f, 0.05f);
                shapes.Add(Level2Kit.Square(at, new Vector2(Mathf.Max(0.05f, w - 0.04f), 0.48f), Fx.A(SliceColor[i % SliceColor.Length], 0.9f), 0.2f));
                _sliceText[i] = _kit.Text(root, "Slice" + i, _round.words[i].ToUpperInvariant() + (w < 1.4f ? "" : " " + Mathf.RoundToInt(_round.weights[i] * 100f) + "%"), 0.2f, new Color(0.05f, 0.05f, 0.12f), at + new Vector3(0f, 0f, -0.1f), Mathf.Max(0.6f, w));
                sx += w;
            }

            foreach (Shape sh in shapes) sh.alpha = 0f;
            _plate = _kit.Batch(root, "Plate", shapes, 1);
            _title = _kit.Text(root, "Title", "ATTENTION", 0.42f, new Color(0.6f, 0.95f, 1f), Centre + new Vector3(0f, 2.55f, 0f), 6f, TextAlignmentOptions.Center, false, true);
            _title.characterSpacing = 8f;

            int n = _round.words.Length;
            _words = new TextMeshPro[n];
            _percent = new TextMeshPro[n];
            _width = new float[n];
            _at = new Vector3[n];
            float total = 0f;
            for (int i = 0; i < n; i++)
            {
                _words[i] = _kit.Text(root, "Word" + i, _round.words[i], 0.5f, Color.white, Vector3.zero, 4f);
                _words[i].ForceMeshUpdate();
                _width[i] = _words[i].preferredWidth;
                total += _width[i];
            }

            // The sentence always fits inside the plate: a longer one is set smaller, never wider.
            total += 0.4f * (n - 1);
            float fit = Mathf.Min(1f, 10.2f / total);
            total *= fit;
            float gap = 0.4f * fit;
            for (int i = 0; i < n; i++)
            {
                _words[i].fontSize *= fit;
                _width[i] *= fit;
            }

            float x = -total * 0.5f;
            for (int i = 0; i < n; i++)
            {
                _at[i] = Centre + new Vector3(x + _width[i] * 0.5f, -0.4f, -0.1f);
                _words[i].transform.localPosition = _at[i];
                _percent[i] = _kit.Text(root, "Percent" + i, "", 0.34f, new Color(1f, 0.9f, 0.55f), _at[i] + new Vector3(0f, 1.3f, 0f), 2f);
                x += _width[i] + gap;
            }

            _arcs = RibbonField.Create(root, "Arcs", n, _kit.Beam, 2);
            _glows = GlowField.Create(root, "Glows", n + 2, _kit.Glow, 3);
            _sum = _kit.Text(root, "Sum", "ADD UP TO 100%", 0.32f, new Color(1f, 0.85f, 0.5f), Centre + new Vector3(0f, -1.75f, -0.1f), 8f);
            _badge = _kit.Text(root, "Badge", "REAL NUMBERS FROM GPT-2:  LAYER " + (_round.layer + 1) + ", HEAD " + (_round.head + 1) + "  (ONE OF ITS 144 HEADS)", 0.26f, new Color(1f, 0.85f, 0.5f), Centre + new Vector3(0f, -1.95f, -0.1f), 10f);
            _blendNote = _kit.Text(root, "Blend", BlendSentence(), 0.26f, new Color(1f, 0.9f, 0.6f), Centre + new Vector3(0f, -1.5f, -0.1f), 10.8f);
            _note = _kit.Text(root, "Note", "A WORD CAN ONLY LOOK BACK", 0.26f, new Color(0.8f, 0.9f, 1f), Centre + new Vector3(0f, -1.0f, -0.1f), 8f);
            _question = _kit.Text(root, "Question", "\"" + _round.words[_round.query].ToUpperInvariant() + "\" ASKS:  WHICH WORDS BEFORE ME EXPLAIN ME?", 0.3f, new Color(1f, 0.88f, 0.55f), Centre + new Vector3(0f, 1.95f, -0.1f), 10.6f);
            _recap = _kit.Text(root, "Recap", "ATTENTION =  1  LOOK BACK   >   2  SHARE OUT 100%   >   3  MIX IN MEANING", 0.24f, Color.white, Centre + new Vector3(0f, -1.55f, -0.12f), 10.8f);
        }

        /// <summary>"CREATURE NOW CARRIES  45% FLUFFY + 43% BLUE + 12% THE": the real shares, biggest first.</summary>
        private string BlendSentence()
        {
            int q = _round.query;
            var order = new List<int>();
            for (int i = 0; i < q; i++) order.Add(i);
            order.Sort((a, b) => _round.weights[b].CompareTo(_round.weights[a]));
            var parts = new List<string>();
            foreach (int i in order) parts.Add(Mathf.RoundToInt(_round.weights[i] * 100f) + "% " + _round.words[i].ToUpperInvariant());
            return _round.words[q].ToUpperInvariant() + " NOW CARRIES   " + string.Join("  +  ", parts);
        }

        protected override void ResetPieces()
        {
            for (int i = 0; i < _plate.Count; i++) _plate[i].alpha = 0f;
            _plate.MarkDirty();
            foreach (TextMeshPro t in Texts())
            {
                t.gameObject.SetActive(false);
                t.alpha = 1f;
            }

            _draw = _numbers = _light = _blend = 0f;
            _words[_round.query].color = Color.white;
            _words[_round.query].transform.localScale = Vector3.one;
            _arcs.HideAll();
            _glows.HideAll();
        }

        private IEnumerable<TextMeshPro> Texts()
        {
            yield return _title; yield return _badge; yield return _sum; yield return _note; yield return _blendNote; yield return _question; yield return _recap;
            foreach (TextMeshPro t in _sliceText) yield return t;
            foreach (TextMeshPro t in _words) yield return t;
            foreach (TextMeshPro t in _percent) yield return t;
        }

        protected override void Animate(float deltaTime)
        {
            _arcs.HideAll();
            _glows.HideAll();
            int q = _round.query;
            Vector3 top = _at[q] + new Vector3(0f, 0.55f, 0f);
            _glows.Set(_round.words.Length, _at[q] + new Vector3(0f, 0f, 0.1f), 2.6f, new Color(1f, 0.85f, 0.4f, 0.35f * _light * (0.8f + 0.2f * Mathf.Sin(Time.time * 4f))));
            if (_blend > 0f)
            {
                // The query word takes on the colour of what it looked at: gold (the describing words) in proportion to their share.
                float share = 0f;
                foreach (int a in _round.answers) share += _round.weights[a];
                _words[q].color = Color.Lerp(Color.white, new Color(1f, 0.78f, 0.3f), _blend * Mathf.Clamp01(share * 1.2f));
                _words[q].transform.localScale = Vector3.one * (1f + 0.18f * _blend * (0.85f + 0.15f * Mathf.Sin(Time.time * 5f)));
            }

            for (int i = 0; i < q; i++)
            {
                float w = _round.weights[i];
                Vector3 from = _at[i] + new Vector3(0f, 0.55f, 0f);
                float arch = 0.9f + 0.55f * (q - i);
                Color c = Color.Lerp(new Color(0.3f, 0.7f, 1f), new Color(1f, 1f, 1f), Mathf.Clamp01(w * 1.4f));
                c.a = (0.3f + 0.65f * w) * Mathf.Clamp01(_draw * 3f);
                _arcs.Set(i, top, from, arch, 0.06f + 0.55f * w, c, _draw);
                if (_draw > 0.95f) _glows.Set(i, from + new Vector3(0f, 0.1f, 0f), 0.3f + 1.4f * w, Fx.A(new Color(0.7f, 0.95f, 1f), 0.7f * w * 2f));
                if (_blend > 0f)
                {
                    // A drop of meaning travels the ribbon into the query, as big as the word's share.
                    float t = Mathf.Repeat(Time.time * 0.6f + i * 0.21f, 1f);
                    Vector3 mid = (from + top) * 0.5f + Vector3.up * arch * 0.5f;
                    Vector3 p = Vector3.Lerp(Vector3.Lerp(from, mid, t), Vector3.Lerp(mid, top, t), t);
                    _glows.Set(i, p, 0.25f + 0.9f * w, Fx.A(new Color(1f, 0.85f, 0.4f), _blend * Mathf.Clamp01(0.4f + w * 1.6f)));
                }
                if (_percent[i].gameObject.activeSelf)
                {
                    _percent[i].text = Mathf.RoundToInt(w * 100f * _numbers) + "%";
                    _percent[i].transform.localPosition = _at[i] + new Vector3(0f, 0.95f + arch * 0.1f, 0f);
                }
            }
        }

        private Tween Param(float duration, System.Action<float> apply) => new Tween(duration, apply, Rate.Smooth);

        protected override void Declare(Steps s)
        {
            Color gold = new Color(1f, 0.82f, 0.45f);
            int q = _round.query;

            // "Words get their meaning from the words around them. A fluffy blue creature isn't just any creature."
            s.At("sentence", () =>
            {
                var list = new List<Tween> { Anim.FadeIn(_plate, 0, Vector3.zero, 0.6f), Anim.Write(_title, 0.7f) };
                for (int i = 0; i < _words.Length; i++)
                {
                    float d = 0.4f + 0.22f * i;
                    list.Add(Anim.Write(_words[i], 0.5f).After(d));
                    if (i > q) list.Add(Anim.FadeText(_words[i], 0.28f, 0.4f).After(d + 0.6f));
                }

                list.Add(Param(0.8f, a => _light = a).After(0.4f + 0.22f * q));
                return list;
            });

            // "So each word looks back at the words before it, and decides which ones matter." Ribbons of light run back from "creature".
            s.At("look", () => new List<Tween>
            {
                Anim.Write(_question, 1.2f),
                Param(1.8f, a => _draw = a).After(1.0f),
                Anim.Write(_note, 1.0f).After(1.6f),
            });

            // "That's attention. Every word gets a share, and the shares add up to one hundred percent."
            s.At("shares", () =>
            {
                var list = new List<Tween> { Param(1.2f, a => _numbers = a) };
                for (int i = 0; i < q; i++) list.Add(Anim.Write(_percent[i], 0.5f).After(0.15f * i));
                list.Add(Anim.FadeText(_note, 0f, 0.4f));
                for (int i = 0; i < q; i++)
                {
                    list.Add(Anim.FadeIn(_plate, _sliceFirst + i, Vector3.left * 0.3f, 0.5f).After(1.0f + 0.35f * i));
                    list.Add(Anim.Write(_sliceText[i], 0.4f).After(1.1f + 0.35f * i));
                }

                list.Add(Anim.Write(_sum, 1.0f).After(1.2f + 0.35f * q));
                return list;
            });

            // "Then the word mixes in a little meaning from each one, by its share. Now creature means: a fluffy, blue creature."
            s.At("blend", () =>
            {
                var list = new List<Tween>
                {
                    Param(1.0f, a => _blend = a),
                    Anim.FadeText(_sum, 0f, 0.4f),
                    Anim.Write(_blendNote, 1.6f).After(0.5f),
                };
                for (int i = 0; i < q; i++)
                {
                    list.Add(Anim.FadeTo(_plate, _sliceFirst + i, 0f, 0.4f));
                    list.Add(Anim.FadeText(_sliceText[i], 0f, 0.4f));
                }

                return list;
            });

            // "These are real numbers from GPT-2. It has a hundred and forty-four attention heads... We picked the one that links words."
            s.At("real", () => new List<Tween> { Anim.Write(_badge, 1.4f) });

            // "So: look back, share out attention, mix in meaning."
            s.At("recap", () => new List<Tween> { Anim.FadeText(_blendNote, 0f, 0.4f), Anim.FadeText(_question, 0f, 0.4f), Anim.Write(_recap, 2.4f).After(0.3f) });

            // "Your turn: you are the attention."
            s.At("spot", () =>
            {
                var list = new List<Tween> { Anim.FadeTo(_plate, 0, 0f, 0.7f), Param(0.7f, a => { _draw = 1f - a; _light = 1f - a; _blend = 1f - a; }) };
                for (int i = 1; i < _plate.Count; i++) list.Add(Anim.FadeTo(_plate, i, 0f, 0.5f));
                foreach (TextMeshPro t in Texts()) if (t.gameObject.activeSelf) list.Add(Anim.FadeText(t, 0f, 0.6f));
                return list;
            });
        }
    }
}
