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
    /// written on the ribbons and add up to a hundred percent ({shares}); the numbers are from a real attention head of GPT-2
    /// ({real}). {spot} clears the board for the game.
    /// </summary>
    public sealed class AttentionExplainer : Level2Explainer
    {
        private Level2Kit _kit;
        private ShapeBatch _plate;
        private RibbonField _arcs;
        private GlowField _glows;
        private TextMeshPro _title, _badge, _sum, _note;
        private TextMeshPro[] _words, _percent;
        private Vector3[] _at;
        private float[] _width;
        private L2Attention _round;
        private float _draw, _numbers, _light;
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

            var shapes = new List<Shape> { Level2Kit.Square(Centre + new Vector3(0f, 0.2f, 0.2f), new Vector2(11f, 4.3f), new Color(0.02f, 0.05f, 0.12f, 0.9f), 0.05f, new Color(0.25f, 0.85f, 1f, 0.9f), 0.04f) };
            shapes[0].alpha = 0f;
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
            _sum = _kit.Text(root, "Sum", "ADD UP TO 100%", 0.36f, new Color(1f, 0.85f, 0.5f), Centre + new Vector3(0f, -1.5f, -0.1f), 8f);
            _badge = _kit.Text(root, "Badge", "REAL NUMBERS FROM GPT-2  (LAYER " + (_round.layer + 1) + ", HEAD " + (_round.head + 1) + ")", 0.26f, new Color(1f, 0.85f, 0.5f), Centre + new Vector3(0f, -1.95f, -0.1f), 10f);
            _note = _kit.Text(root, "Note", "A WORD CAN ONLY LOOK BACK", 0.26f, new Color(0.8f, 0.9f, 1f), Centre + new Vector3(0f, -1.0f, -0.1f), 8f);
        }

        protected override void ResetPieces()
        {
            _plate[0].alpha = 0f;
            _plate.MarkDirty();
            foreach (TextMeshPro t in Texts())
            {
                t.gameObject.SetActive(false);
                t.alpha = 1f;
            }

            _draw = _numbers = _light = 0f;
            _arcs.HideAll();
            _glows.HideAll();
        }

        private IEnumerable<TextMeshPro> Texts()
        {
            yield return _title; yield return _badge; yield return _sum; yield return _note;
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
            for (int i = 0; i < q; i++)
            {
                float w = _round.weights[i];
                Vector3 from = _at[i] + new Vector3(0f, 0.55f, 0f);
                float arch = 0.9f + 0.55f * (q - i);
                Color c = Color.Lerp(new Color(0.3f, 0.7f, 1f), new Color(1f, 1f, 1f), Mathf.Clamp01(w * 1.4f));
                c.a = (0.3f + 0.65f * w) * Mathf.Clamp01(_draw * 3f);
                _arcs.Set(i, top, from, arch, 0.06f + 0.55f * w, c, _draw);
                if (_draw > 0.95f) _glows.Set(i, from + new Vector3(0f, 0.1f, 0f), 0.3f + 1.4f * w, Fx.A(new Color(0.7f, 0.95f, 1f), 0.7f * w * 2f));
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
                Param(1.8f, a => _draw = a),
                Anim.Write(_note, 1.0f).After(0.6f),
            });

            // "That's attention. Every word gets a share, and the shares add up to one hundred percent."
            s.At("shares", () =>
            {
                var list = new List<Tween> { Param(1.2f, a => _numbers = a) };
                for (int i = 0; i < q; i++) list.Add(Anim.Write(_percent[i], 0.5f).After(0.15f * i));
                list.Add(Anim.FadeText(_note, 0f, 0.4f));
                list.Add(Anim.Write(_sum, 1.0f).After(0.9f));
                return list;
            });

            // "These are real numbers, from one of GPT-2's attention heads."
            s.At("real", () => new List<Tween> { Anim.Write(_badge, 1.4f) });

            // "Your turn: you are the attention."
            s.At("spot", () =>
            {
                var list = new List<Tween> { Anim.FadeTo(_plate, 0, 0f, 0.7f), Param(0.7f, a => { _draw = 1f - a; _light = 1f - a; }) };
                foreach (TextMeshPro t in Texts()) if (t.gameObject.activeSelf) list.Add(Anim.FadeText(t, 0f, 0.6f));
                return list;
            });
        }
    }
}
