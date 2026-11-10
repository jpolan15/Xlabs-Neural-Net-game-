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
    /// the rider's turn) which clears the board. v3 (owner playtest 2026-11-10: "what makes a token?"): {glue}, {the}, {chatbot} and
    /// {same} show how GPT-2 decides where to chop: its glue list, replayed letter by letter on "the" (glued whole) and "chatbot" (no
    /// rule glues chat + bot, so it stays two), each glue labelled with its real rule number from GPT-2's merges.
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

        // The glue demo: two words, one tile per letter, re-laid out at each glue step.
        private static readonly Color[] GlueColor = { new Color(0.42f, 1f, 0.72f), new Color(1f, 0.5f, 0.72f) };
        private static readonly float[] GlueX = { -2.75f, 1.75f };
        private L2Merge[] _glue;
        private int[] _glueFirst;
        private TextMeshPro[][] _glueText;
        private TextMeshPro[] _glueRule, _glueCount;
        private TextMeshPro _glueTitle, _glueList, _same, _noRule;
        private readonly float[] _glueT = new float[2];
        private readonly bool[] _glueOn = new bool[2];
        private readonly int[] _glueShown = { -1, -1 };
        private readonly float[] _glueFlash = new float[2];
        private readonly int[] _glueNew = { -1, -1 };
        private Vector3 _centre;

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
            _centre = centre;

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

            _glue = new[] { data.Merge(Level2Data.CommonGlueWord), data.Merge(Level2Data.RareGlueWord) };
            _glueFirst = new int[2];
            for (int w = 0; w < 2; w++)
            {
                _glueFirst[w] = shapes.Count;
                Color c = GlueColor[w];
                for (int i = 0; i < _glue[w].steps[0].pieces.Length; i++) shapes.Add(Level2Kit.Square(centre, new Vector2(0.5f, 0.66f), Fx.A(c, 0.2f), 0.35f, Fx.A(c, 0.95f), 0.035f));
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

            _glueText = new TextMeshPro[2][];
            _glueRule = new TextMeshPro[2];
            _glueCount = new TextMeshPro[2];
            for (int w = 0; w < 2; w++)
            {
                int n = _glue[w].steps[0].pieces.Length;
                _glueText[w] = new TextMeshPro[n];
                for (int i = 0; i < n; i++) _glueText[w][i] = _kit.Text(root, "Glue" + w + "_" + i, "", 0.34f, Color.white, centre, 2f);
                _glueRule[w] = _kit.Text(root, "GlueRule" + w, "", 0.24f, new Color(1f, 0.85f, 0.45f), centre + new Vector3(GlueX[w], 0.7f, -0.05f), 4.2f);
                _glueCount[w] = _kit.Text(root, "GlueCount" + w, "", 0.34f, GlueColor[w], centre + new Vector3(GlueX[w], -0.9f, -0.05f), 4.2f);
            }

            _glueTitle = _kit.Text(root, "GlueTitle", "WHERE TO CHOP?  GPT-2'S GLUE LIST", 0.3f, new Color(1f, 0.85f, 0.45f), centre + new Vector3(0f, 1.6f, -0.05f), 9f);
            _glueList = _kit.Text(root, "GlueList", "MADE BEFORE TRAINING:  MOST COMMON PAIR GLUED FIRST  -  50,000 RULES", 0.19f, new Color(0.85f, 0.9f, 1f), centre + new Vector3(0f, 1.2f, -0.05f), 9.2f);
            _noRule = _kit.Text(root, "NoRule", "NO RULE GLUES  CHAT + BOT", 0.24f, new Color(1f, 0.55f, 0.5f), centre + new Vector3(GlueX[1], 0.7f, -0.05f), 4.2f);
            _same = _kit.Text(root, "Same", "SAME LIST, SAME ORDER  =  SAME CHOPS, EVERY TIME", 0.26f, Color.white, centre + new Vector3(0f, -1.5f, -0.05f), 9f);
        }

        /// <summary>Lays one glue word out at the glue step it has reached: a tile per piece, centred under its rule label.</summary>
        private void LayGlue(int w)
        {
            L2Merge m = _glue[w];
            int step = Mathf.Clamp(Mathf.FloorToInt(_glueT[w] + 1e-3f), 0, m.steps.Length - 1);
            if (step != _glueShown[w])
            {
                // A new glue: find the piece it made, flash it, and name the rule.
                _glueNew[w] = -1;
                if (step > 0)
                {
                    string[] before = m.steps[step - 1].pieces, after = m.steps[step].pieces;
                    for (int i = 0; i < after.Length && _glueNew[w] < 0; i++)
                    {
                        if (i >= before.Length || after[i] != before[i]) _glueNew[w] = i;
                    }

                    int k = Mathf.Max(0, _glueNew[w]);
                    string a = k < before.Length ? before[k] : "", b = k + 1 < before.Length ? before[k + 1] : "";
                    _glueRule[w].text = "GLUE RULE #" + m.steps[step].rule.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + ":   " + a + " + " + b;
                    _glueRule[w].gameObject.SetActive(true);
                    _glueRule[w].alpha = 1f;
                    _glueFlash[w] = 1f;
                }

                _glueShown[w] = step;
            }

            string[] pieces = m.steps[step].pieces;
            const float gap = 0.12f;
            float total = gap * (pieces.Length - 1);
            foreach (string p in pieces) total += 0.19f * p.Length + 0.3f;
            float x = GlueX[w] - total * 0.5f;
            for (int i = 0; i < _glueText[w].Length; i++)
            {
                Shape tile = _batch[_glueFirst[w] + i];
                TextMeshPro t = _glueText[w][i];
                if (i < pieces.Length)
                {
                    float width = 0.19f * pieces[i].Length + 0.3f;
                    Vector3 at = _centre + new Vector3(x + width * 0.5f, -0.05f, 0f);
                    bool fresh = i == _glueNew[w];
                    tile.position = at;
                    tile.size = new Vector2(width, 0.66f);
                    tile.alpha = 1f;
                    tile.scale = 1f + (fresh ? 0.18f * _glueFlash[w] : 0f);
                    tile.color = Color.Lerp(Fx.A(GlueColor[w], 0.2f), new Color(1f, 0.85f, 0.4f, 0.85f), fresh ? _glueFlash[w] : 0f);
                    t.gameObject.SetActive(true);
                    t.text = pieces[i];
                    t.transform.localPosition = at + new Vector3(0f, 0f, -0.05f);
                    x += width + gap;
                }
                else
                {
                    tile.alpha = 0f;
                    t.gameObject.SetActive(false);
                }
            }
        }

        protected override void Animate(float deltaTime)
        {
            bool any = false;
            for (int w = 0; w < 2; w++)
            {
                if (!_glueOn[w]) continue;
                _glueFlash[w] = Mathf.MoveTowards(_glueFlash[w], 0f, deltaTime * 1.4f);
                LayGlue(w);
                any = true;
            }

            if (any) _batch.MarkDirty();
        }

        private void HideGlue()
        {
            for (int w = 0; w < 2; w++)
            {
                _glueOn[w] = false;
                _glueShown[w] = -1;
                _glueT[w] = 0f;
                for (int i = 0; i < _glueText[w].Length; i++)
                {
                    _batch[_glueFirst[w] + i].alpha = 0f;
                    _glueText[w][i].gameObject.SetActive(false);
                }
            }

            _batch.MarkDirty();
        }

        private List<Tween> ShowGlue(int w)
        {
            _glueOn[w] = true;
            _glueT[w] = 0f;
            _glueShown[w] = -1;
            LayGlue(w);
            _batch.MarkDirty();
            var list = new List<Tween>();
            foreach (TextMeshPro t in _glueText[w]) list.Add(Anim.Write(t, 0.4f));
            return list;
        }

        private Tween GlueTo(int w, int step, float seconds)
        {
            float from = 0f;
            return new Tween(seconds, a => _glueT[w] = Mathf.Lerp(from, step, a), Rate.Smooth, begin: () => from = _glueT[w]);
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
            if (_glueText != null) HideGlue();
        }

        private IEnumerable<TextMeshPro> Texts()
        {
            yield return _glueTitle;
            yield return _glueList;
            yield return _same;
            yield return _noRule;
            foreach (TextMeshPro t in _glueRule) yield return t;
            foreach (TextMeshPro t in _glueCount) yield return t;
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

            // "So where does it chop? ... a glue list." The sentence's chips clear; the glue list is named.
            s.At("glue", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < _chips; i++)
                {
                    list.Add(Anim.FadeTo(_batch, _chipFirst + i, 0f, 0.5f));
                    list.Add(Anim.FadeTo(_batch, _lineFirst + i, 0f, 0.4f));
                    list.Add(Anim.FadeText(_chipText[i], 0f, 0.45f));
                    list.Add(Anim.FadeText(_idText[i], 0f, 0.4f));
                }

                list.Add(Anim.FadeText(_title, 0f, 0.4f));
                list.Add(Anim.Write(_glueTitle, 0.9f).After(0.5f));
                list.Add(Anim.Write(_glueList, 1.6f).After(3.0f));
                return list;
            });

            // "Take the word the. H and E are glue number three. Then T joins them. One token." Clauses 7 to 10 of TokGlue.
            int theSteps = _glue[0].steps.Length - 1;
            s.At("the", () => ShowGlue(0));
            for (int k = 1; k <= theSteps; k++)
            {
                int step = k;
                s.AtClause(RideLine.TokGlue, 7 + k, () => new List<Tween> { GlueTo(0, step, 0.3f) });
            }

            s.AtClause(RideLine.TokGlue, 8 + theSteps, () => new List<Tween>
            {
                Tween.Call(0f, () => _glueCount[0].text = "1 TOKEN"),
                Anim.Write(_glueCount[0], 0.6f),
            });

            // "Chatbot was rare back then. It gets glued into chat and bot, but no rule glues those two. So it stays two tokens."
            int chat = 9 + theSteps; // the {chatbot} clause
            s.At("chatbot", () => ShowGlue(1));
            s.AtClause(RideLine.TokGlue, chat + 1, () => new List<Tween> { GlueTo(1, _glue[1].steps.Length - 1, 2.6f) });
            s.AtClause(RideLine.TokGlue, chat + 2, () => new List<Tween> { Anim.FadeText(_glueRule[1], 0f, 0.3f), Anim.Write(_noRule, 0.7f).After(0.2f) });
            s.AtClause(RideLine.TokGlue, chat + 3, () => new List<Tween>
            {
                Tween.Call(0f, () => _glueCount[1].text = _glue[1].steps[_glue[1].steps.Length - 1].pieces.Length + " TOKENS"),
                Anim.Write(_glueCount[1], 0.6f),
            });

            // "When you type, the chopper follows the same glue list, in the same order."
            s.At("same", () => new List<Tween> { Anim.Write(_same, 1.2f) });

            // "A word the glue list has rarely seen gets chopped a lot." The glue demo clears; one very long word is typed.
            s.At("rare", () =>
            {
                var list = new List<Tween>
                {
                    Tween.Call(0.6f, HideGlue),
                    Anim.FadeText(_glueTitle, 0f, 0.4f), Anim.FadeText(_glueList, 0f, 0.4f), Anim.FadeText(_same, 0f, 0.4f), Anim.FadeText(_noRule, 0f, 0.4f),
                    Anim.Write(_title, 0.5f),
                };
                for (int w = 0; w < 2; w++)
                {
                    list.Add(Anim.FadeText(_glueRule[w], 0f, 0.4f));
                    list.Add(Anim.FadeText(_glueCount[w], 0f, 0.4f));
                    foreach (TextMeshPro t in _glueText[w]) list.Add(Anim.FadeText(t, 0f, 0.4f));
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
