using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 1, explained: a sentence is typed, then chopped into GPT-2's real tokens (a chip each), each token gets its real number,
    /// and a very long word is chopped into eleven. Cues: {sentence}, {chop}, {ids}, {rare}, {boss}, and {slice} (the next line,
    /// the rider's turn) which clears the board.
    /// </summary>
    public sealed class TokenizeExplainer : Level2Explainer
    {
        private static readonly Color[] Palette =
        {
            new Color(0.35f, 0.85f, 1f), new Color(1f, 0.78f, 0.35f), new Color(0.62f, 1f, 0.62f),
            new Color(1f, 0.5f, 0.72f), new Color(0.72f, 0.6f, 1f), new Color(1f, 0.62f, 0.4f),
        };

        private Level2Kit _kit;
        private TextMeshPro _sentence, _rare, _counter, _title;
        private TextMeshPro[] _chipText, _idText, _pieceText;
        private ShapeBatch _batch;
        private int _chips, _pieces, _chipFirst, _lineFirst, _pieceFirst, _plate;
        private GlowField _sparks;
        private Vector3[] _chipAt, _pieceAt;
        private Vector2[] _chipSize, _pieceSize;

        protected override void Awake()
        {
            Construct();
            base.Awake();
        }

        private void Construct()
        {
            _kit = Level2Kit.Current;
            Level2Data data = source.Data;
            Transform root = MakeContent("TokenizeExplain");
            Vector3 centre = new Vector3(0f, 2.55f, 8f);

            L2Token[] tokens = data.File.thread.tokens;
            _chips = tokens.Length;
            L2Word boss = data.Word(Level2Data.BossWord);
            _pieces = boss.tokens.Length;

            // Lay the chips in two rows, left to right.
            _chipAt = new Vector3[_chips];
            _chipSize = new Vector2[_chips];
            int firstRow = 6;
            for (int row = 0; row < 2; row++)
            {
                int from = row == 0 ? 0 : firstRow, to = row == 0 ? firstRow : _chips;
                float total = 0f;
                for (int i = from; i < to; i++)
                {
                    _chipSize[i] = new Vector2(Mathf.Max(0.6f, 0.2f * Fx.Clean(tokens[i].text).Length + 0.46f), 0.62f);
                    total += _chipSize[i].x;
                }

                total += 0.16f * (to - from - 1);
                float x = -total * 0.5f;
                for (int i = from; i < to; i++)
                {
                    _chipAt[i] = centre + new Vector3(x + _chipSize[i].x * 0.5f, row == 0 ? 0.55f : -0.7f, 0f);
                    x += _chipSize[i].x + 0.16f;
                }
            }

            _pieceAt = new Vector3[_pieces];
            _pieceSize = new Vector2[_pieces];
            int pieceRow = 6;
            for (int row = 0; row < 2; row++)
            {
                int from = row == 0 ? 0 : pieceRow, to = row == 0 ? pieceRow : _pieces;
                float total = 0f;
                for (int i = from; i < to; i++)
                {
                    _pieceSize[i] = new Vector2(Mathf.Max(0.62f, 0.2f * Fx.Clean(boss.tokens[i]).Length + 0.46f), 0.62f);
                    total += _pieceSize[i].x;
                }

                total += 0.16f * (to - from - 1);
                float x = -total * 0.5f;
                for (int i = from; i < to; i++)
                {
                    _pieceAt[i] = centre + new Vector3(x + _pieceSize[i].x * 0.5f, row == 0 ? 0.5f : -0.55f, 0f);
                    x += _pieceSize[i].x + 0.16f;
                }
            }

            var shapes = new List<Shape>();
            _plate = shapes.Count;
            shapes.Add(Level2Kit.Square(centre, new Vector2(9.6f, 3.9f), new Color(0.03f, 0.04f, 0.1f, 0.98f), 0.07f, new Color(1f, 0.7f, 0.3f, 0.8f), 0.04f));
            _chipFirst = shapes.Count;
            for (int i = 0; i < _chips; i++)
            {
                Color c = Palette[i % Palette.Length];
                shapes.Add(Level2Kit.Square(_chipAt[i], _chipSize[i], Fx.A(c, 0.2f), 0.35f, Fx.A(c, 0.95f), 0.035f));
            }

            _lineFirst = shapes.Count;
            for (int i = 0; i < _chips; i++)
            {
                Vector3 a = _chipAt[i] + new Vector3(0f, -0.33f, 0f);
                shapes.Add(Level2Kit.Line(a, a + new Vector3(0f, -0.2f, 0f), 0.02f, Fx.A(Palette[i % Palette.Length], 0.8f)));
            }

            _pieceFirst = shapes.Count;
            for (int i = 0; i < _pieces; i++)
            {
                Color c = Palette[i % Palette.Length];
                shapes.Add(Level2Kit.Square(_pieceAt[i], _pieceSize[i], Fx.A(c, 0.2f), 0.35f, Fx.A(c, 0.95f), 0.035f));
            }

            foreach (Shape s in shapes) s.alpha = 0f;
            _batch = _kit.Batch(root, "Chips", shapes, 1);

            _title = _kit.Text(root, "Title", "TOKENS", 0.42f, new Color(1f, 0.82f, 0.45f), centre + new Vector3(0f, 2.35f, 0f), 6f, TextAlignmentOptions.Center, false, true);
            _title.characterSpacing = 8f;
            _sentence = _kit.Text(root, "Sentence", data.File.thread.text, 0.3f, Color.white, centre + new Vector3(0f, 0.05f, -0.05f), 9.4f);
            _chipText = new TextMeshPro[_chips];
            _idText = new TextMeshPro[_chips];
            for (int i = 0; i < _chips; i++)
            {
                _chipText[i] = _kit.Text(root, "Token" + i, Fx.Clean(tokens[i].text), 0.3f, Color.white, _chipAt[i] + new Vector3(0f, 0f, -0.05f), _chipSize[i].x);
                _idText[i] = _kit.Text(root, "Id" + i, tokens[i].id.ToString(), 0.2f, new Color(0.45f, 0.95f, 1f), _chipAt[i] + new Vector3(0f, -0.58f, -0.05f), 1.6f);
            }

            _rare = _kit.Text(root, "RareWord", Level2Data.BossWord, 0.3f, Color.white, centre + new Vector3(0f, 0.05f, -0.05f), 9.4f);
            _pieceText = new TextMeshPro[_pieces];
            for (int i = 0; i < _pieces; i++) _pieceText[i] = _kit.Text(root, "Piece" + i, Fx.Clean(boss.tokens[i]), 0.28f, Color.white, _pieceAt[i] + new Vector3(0f, 0f, -0.05f), _pieceSize[i].x);
            _counter = _kit.Text(root, "Counter", _pieces + " TOKENS", 0.5f, new Color(1f, 0.82f, 0.45f), centre + new Vector3(0f, -1.6f, -0.05f), 6f);
            _sparks = GlowField.Create(root, "Sparks", 90, _kit.Glow, 3);
        }

        protected override void ResetPieces()
        {
            for (int i = 0; i < _batch.Count; i++)
            {
                _batch[i].alpha = 0f;
                _batch[i].grow = 1f;
            }

            _batch.MarkDirty();
            foreach (TextMeshPro t in Texts())
            {
                t.gameObject.SetActive(false);
                t.alpha = 1f;
            }

            _sparks.HideAll();
        }

        private IEnumerable<TextMeshPro> Texts()
        {
            yield return _sentence;
            yield return _rare;
            yield return _counter;
            yield return _title;
            foreach (TextMeshPro t in _chipText) yield return t;
            foreach (TextMeshPro t in _idText) yield return t;
            foreach (TextMeshPro t in _pieceText) yield return t;
        }

        private Tween Burst(Vector3 at, Color color, float delay, int first, int count = 7)
        {
            return new Tween(0.7f, a =>
            {
                for (int k = 0; k < count; k++)
                {
                    float ang = (k / (float)count) * Mathf.PI * 2f + first;
                    Vector3 dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                    float fade = 1f - a;
                    _sparks.Set(first + k, at + dir * (0.15f + a * 0.55f), 0.16f * fade + 0.02f, Fx.A(color, 0.9f * fade));
                }
            }, Rate.RushFrom, end: () =>
            {
                for (int k = 0; k < count; k++) _sparks.Hide(first + k);
            }) { Delay = delay };
        }

        protected override void Declare(Steps s)
        {
            Color gold = new Color(1f, 0.82f, 0.45f);
            Level2Data data = source.Data;

            // "A computer can't read letters..." The sentence is typed on the board.
            s.At("sentence", () =>
            {
                var list = new List<Tween> { Anim.FadeIn(_batch, _plate, Vector3.zero, 0.6f), Anim.Write(_title, 0.7f), Anim.Write(_sentence, 2.2f).After(0.5f) };
                return list;
            });

            // "The pieces are called tokens." The sentence falls apart into chips: GPT-2's real tokens.
            s.At("chop", () =>
            {
                var list = new List<Tween> { Anim.FadeText(_sentence, 0f, 0.5f) };
                for (int i = 0; i < _chips; i++)
                {
                    float d = 0.4f + 0.14f * i;
                    list.Add(Anim.FadeIn(_batch, _chipFirst + i, Vector3.down * 0.3f, 0.5f).After(d));
                    list.Add(Anim.Write(_chipText[i], 0.45f).After(d + 0.05f));
                    list.Add(Burst(_chipAt[i] + new Vector3(-_chipSize[i].x * 0.5f - 0.08f, 0f, 0f), Palette[i % Palette.Length], d, i * 7, 6));
                }

                return list;
            });

            // "Every token gets a number." A tag line drops from each chip to its real id.
            s.At("ids", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < _chips; i++)
                {
                    float d = 0.12f * i;
                    list.Add(Anim.FadeIn(_batch, _lineFirst + i, Vector3.zero, 0.2f).After(d));
                    list.Add(Anim.Create(_batch, _lineFirst + i, 0.35f).After(d));
                    list.Add(Anim.Write(_idText[i], 0.5f).After(d + 0.25f));
                }

                return list;
            });

            // "Common words stay whole. Rare words get chopped smaller." The sentence clears; one very long word is typed.
            s.At("rare", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < _chips; i++)
                {
                    list.Add(Anim.FadeTo(_batch, _chipFirst + i, 0f, 0.5f));
                    list.Add(Anim.FadeTo(_batch, _lineFirst + i, 0f, 0.4f));
                    list.Add(Anim.FadeText(_chipText[i], 0f, 0.45f));
                    list.Add(Anim.FadeText(_idText[i], 0f, 0.4f));
                }

                list.Add(Anim.Write(_rare, 2.0f).After(0.7f));
                return list;
            });

            // "This one becomes eleven tokens!" The word splits into GPT-2's real eleven pieces.
            s.At("boss", () =>
            {
                var list = new List<Tween> { Anim.FadeText(_rare, 0f, 0.4f) };
                for (int i = 0; i < _pieces; i++)
                {
                    float d = 0.1f + 0.11f * i;
                    list.Add(Anim.FadeIn(_batch, _pieceFirst + i, Vector3.down * 0.25f, 0.4f).After(d));
                    list.Add(Anim.Write(_pieceText[i], 0.35f).After(d + 0.05f));
                    list.Add(Burst(_pieceAt[i] + new Vector3(-_pieceSize[i].x * 0.5f - 0.08f, 0f, 0f), Palette[i % Palette.Length], d, 42 + i * 4, 4));
                }

                list.Add(Anim.Write(_counter, 0.7f).After(1.6f));
                for (int i = 0; i < _pieces; i++) list.Add(Anim.Indicate(_batch, _pieceFirst + i, gold, 0.5f, 1.12f).After(2.0f + 0.05f * i));
                return list;
            });

            // "Your turn: be the chopper." The board clears for the game.
            s.At("slice", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < _batch.Count; i++) list.Add(Anim.FadeTo(_batch, i, 0f, 0.7f));
                foreach (TextMeshPro t in Texts()) if (t.gameObject.activeSelf) list.Add(Anim.FadeText(t, 0f, 0.7f));
                return list;
            });
        }
    }
}
