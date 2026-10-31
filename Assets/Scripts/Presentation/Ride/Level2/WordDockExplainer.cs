using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// The intro of Level 2. A chat window floats ahead: a question is typed, an answer starts to come back, and the window rushes past
    /// the rider (we ride inside the machine). Five stop chips light as the narrator names them and a GPT-2 badge says where the numbers
    /// come from. Cues: {chat}, {type}, {inside}, the clauses of the stops line, and {real}.
    /// </summary>
    public sealed class WordDockExplainer : Level2Explainer
    {
        private static readonly string[] Stops = { "1  TOKENS", "2  MEANING", "3  ATTENTION", "4  PREDICTION", "5  WRITING" };

        private static readonly Color[] Palette =
        {
            new Color(1f, 0.72f, 0.3f), new Color(0.62f, 0.5f, 1f), new Color(0.3f, 0.86f, 1f), new Color(1f, 0.45f, 0.7f), new Color(0.45f, 1f, 0.72f),
        };

        private Level2Kit _kit;
        private ShapeBatch _batch, _chips;
        private Transform _window;
        private TextMeshPro _header, _prompt, _badge;
        private TextMeshPro[] _stopText;
        private GlowField _flash;
        private int _panel, _headerBar, _userBubble, _replyBubble, _inputBox, _send, _cursor, _dots, _badgeBack;
        private const int ChipFirst = 0;
        private bool _typing;
        private float _typeStart;
        private bool _insideDone;

        protected override void Awake()
        {
            Construct();
            base.Awake();
        }

        private void Construct()
        {
            _kit = Level2Kit.Current;
            Level2Data data = source.Data;
            Transform root = MakeContent("DockIntro");
            _window = new GameObject("Window").transform;
            _window.SetParent(root, false);
            Vector3 c = new Vector3(0f, 2.75f, 8.6f);
            _window.localPosition = Vector3.zero;

            var shapes = new List<Shape>();
            _panel = shapes.Count;
            shapes.Add(Level2Kit.Square(c, new Vector2(7.4f, 3.7f), new Color(0.05f, 0.04f, 0.15f, 0.98f), 0.06f, new Color(0.62f, 0.5f, 1f, 0.95f), 0.045f));
            _headerBar = shapes.Count;
            shapes.Add(Level2Kit.Square(c + new Vector3(0f, 1.6f, -0.02f), new Vector2(7.2f, 0.5f), new Color(0.2f, 0.14f, 0.42f, 0.95f), 0.3f));
            for (int i = 0; i < 3; i++) shapes.Add(Level2Kit.Square(c + new Vector3(-3.3f + i * 0.34f, 1.6f, -0.04f), new Vector2(0.2f, 0.2f), Palette[i], 1f));
            _userBubble = shapes.Count;
            shapes.Add(Level2Kit.Square(c + new Vector3(0.1f, 0.62f, -0.02f), new Vector2(6.8f, 0.85f), new Color(0.25f, 0.4f, 0.95f, 0.9f), 0.3f));
            _replyBubble = shapes.Count;
            shapes.Add(Level2Kit.Square(c + new Vector3(-2.55f, -0.38f, -0.02f), new Vector2(1.7f, 0.7f), new Color(0.18f, 0.16f, 0.34f, 0.95f), 0.5f));
            _dots = shapes.Count;
            for (int i = 0; i < 3; i++) shapes.Add(Level2Kit.Square(c + new Vector3(-2.95f + i * 0.4f, -0.38f, -0.04f), new Vector2(0.2f, 0.2f), new Color(0.85f, 0.8f, 1f, 0.95f), 1f));
            _inputBox = shapes.Count;
            shapes.Add(Level2Kit.Square(c + new Vector3(-0.35f, -1.38f, -0.02f), new Vector2(6.0f, 0.5f), new Color(0.03f, 0.03f, 0.1f, 0.9f), 0.5f, new Color(0.3f, 0.86f, 1f, 0.9f), 0.03f));
            _send = shapes.Count;
            shapes.Add(Level2Kit.Square(c + new Vector3(3.05f, -1.38f, -0.02f), new Vector2(0.5f, 0.5f), new Color(0.3f, 0.86f, 1f, 0.95f), 1f));
            _cursor = shapes.Count;
            shapes.Add(Level2Kit.Square(c + new Vector3(-3.2f, -1.38f, -0.05f), new Vector2(0.05f, 0.34f), Color.white, 0.3f));

            foreach (Shape sh in shapes) sh.alpha = 0f;
            _batch = _kit.Batch(_window, "ChatShapes", shapes, 1);

            // The five stops, a row of chips under the window, and the badge of where the numbers come from.
            var chips = new List<Shape>();
            for (int i = 0; i < Stops.Length; i++)
            {
                Vector3 at = new Vector3((i - 2) * 2.7f, 1.9f, 8.5f);
                chips.Add(Level2Kit.Square(at, new Vector2(2.5f, 0.72f), new Color(0.04f, 0.05f, 0.12f, 0.94f), 0.4f, Fx.A(Palette[i], 0.95f), 0.05f)); // dark chips read over the daylight road
            }

            _badgeBack = chips.Count;
            chips.Add(Level2Kit.Square(new Vector3(2.9f, 5.0f, 8.55f), new Vector2(2.9f, 0.55f), new Color(0.04f, 0.05f, 0.12f, 0.94f), 0.4f, new Color(1f, 0.82f, 0.4f, 0.95f), 0.035f));
            foreach (Shape sh in chips) sh.alpha = 0f;
            _chips = _kit.Batch(root, "StopChips", chips, 1);

            _header = _kit.Text(_window, "Header", "CHATBOT", 0.26f, new Color(0.85f, 0.8f, 1f), c + new Vector3(0f, 1.6f, -0.06f), 4f, TextAlignmentOptions.Center, true);
            _header.characterSpacing = 6f;
            _prompt = _kit.Text(_window, "Prompt", data.File.thread.text, 0.24f, Color.white, c + new Vector3(0.1f, 0.62f, -0.06f), 6.6f);
            _stopText = new TextMeshPro[Stops.Length];
            for (int i = 0; i < Stops.Length; i++) _stopText[i] = _kit.Text(root, "Stop" + i, Stops[i], 0.27f, Color.white, new Vector3((i - 2) * 2.7f, 1.9f, 8.45f), 2.5f);
            _badge = _kit.Text(root, "Badge", "REAL MODEL: GPT-2", 0.22f, new Color(1f, 0.85f, 0.45f), new Vector3(2.9f, 5.0f, 8.5f), 2.9f);
            _flash = GlowField.Create(root, "Flash", 12, _kit.Glow, 3);
        }

        protected override void ResetPieces()
        {
            for (int i = 0; i < _batch.Count; i++) _batch[i].alpha = 0f;
            for (int i = 0; i < _chips.Count; i++) _chips[i].alpha = 0f;
            _batch.MarkDirty();
            _chips.MarkDirty();
            _header.gameObject.SetActive(false);
            _prompt.gameObject.SetActive(false);
            _badge.gameObject.SetActive(false);
            foreach (TextMeshPro t in _stopText) t.gameObject.SetActive(false);
            _window.localScale = Vector3.one;
            _window.localPosition = Vector3.zero;
            _window.gameObject.SetActive(true);
            _flash.HideAll();
            _typing = false;
            _insideDone = false;
        }

        protected override void Animate(float deltaTime)
        {
            // The cursor blinks; the three dots of an answer on its way bounce.
            if (_batch[_cursor].alpha > 0f)
            {
                _batch[_cursor].alpha = Mathf.Repeat(Time.time, 0.9f) < 0.5f ? 1f : 0.15f;
                _batch.MarkDirty();
            }

            if (_batch[_dots].alpha > 0.01f)
            {
                for (int i = 0; i < 3; i++)
                {
                    Shape d = _batch[_dots + i];
                    float bounce = Mathf.Max(0f, Mathf.Sin(Time.time * 7f - i * 0.9f));
                    d.position = new Vector3(d.position.x, 2.75f - 0.38f + bounce * 0.12f, d.position.z);
                }

                _batch.MarkDirty();
            }
        }

        protected override void Declare(Steps s)
        {
            Color gold = new Color(1f, 0.82f, 0.45f);

            // "Last time, you taught Pixel to read numbers. Now Pixel wants to talk." A chat window fades in.
            s.At("chat", () =>
            {
                var list = new List<Tween> { Anim.Write(_header, 0.8f).After(0.4f) };
                foreach (int i in new[] { _panel, _headerBar, _inputBox, _send, _cursor }) list.Add(Anim.FadeIn(_batch, i, Vector3.up * 0.15f, 0.8f));
                for (int i = 0; i < 3; i++) list.Add(Anim.FadeIn(_batch, _headerBar + 1 + i, Vector3.zero, 0.6f).After(0.4f));
                return list;
            });

            // "Type a question into a chatbot, and an answer comes back, one word at a time."
            s.At("type", () => new List<Tween>
            {
                Anim.FadeIn(_batch, _userBubble, Vector3.right * 0.5f, 0.5f),
                Anim.Write(_prompt, 2.4f).After(0.3f),
                Anim.FadeIn(_batch, _replyBubble, Vector3.left * 0.4f, 0.5f).After(3.0f),
                Anim.FadeIn(_batch, _dots, Vector3.zero, 0.3f).After(3.2f),
                Anim.FadeIn(_batch, _dots + 1, Vector3.zero, 0.3f).After(3.2f),
                Anim.FadeIn(_batch, _dots + 2, Vector3.zero, 0.3f).After(3.2f),
            });

            // "Today, we ride inside the machine." The window rushes at the rider and over their head.
            s.At("inside", () =>
            {
                var list = new List<Tween>
                {
                    new Tween(1.9f, a =>
                    {
                        float e = Rate.RushInto(a);
                        _window.localPosition = new Vector3(0f, 0f, -9.4f * e);
                        for (int i = 0; i < 12; i++)
                        {
                            float ang = i * 0.52f;
                            _flash.Set(i, new Vector3(Mathf.Cos(ang) * e * 5.5f, 2.8f + Mathf.Sin(ang) * e * 2.6f, 9f - e * 5f), 0.4f + e * 1.4f, new Color(0.8f, 0.7f, 1f, 0.8f * (1f - e)));
                        }
                    }, Rate.Linear, end: () =>
                    {
                        _insideDone = true;
                        _flash.HideAll();
                        _window.gameObject.SetActive(false);
                    }),
                };

                return list;
            });

            // "Five stops: tokens, meaning, attention, prediction, and writing."
            s.AtClause(RideLine.L2Stops, 0, () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < Stops.Length; i++)
                {
                    list.Add(Anim.FadeIn(_chips, ChipFirst + i, Vector3.up * 0.2f, 0.5f).After(0.1f * i));
                    list.Add(Anim.FadeText(_stopText[i], 0.45f, 0.5f).After(0.1f * i));
                }

                return list;
            });

            for (int k = 0; k < Stops.Length; k++)
            {
                int stop = k;
                s.AtClause(RideLine.L2Stops, k + 1, () => new List<Tween>
                {
                    Anim.FadeTo(_chips, ChipFirst + stop, 1f, 0.3f),
                    Anim.Indicate(_chips, ChipFirst + stop, gold, 0.9f, 1.1f),
                    Anim.FadeText(_stopText[stop], 1f, 0.3f),
                });
            }

            // "Everything you'll see comes from a real language model, called GPT-2."
            s.At("real", () => new List<Tween>
            {
                Anim.FadeIn(_chips, _badgeBack, Vector3.zero, 0.5f),
                Anim.Write(_badge, 0.9f),
                Anim.Indicate(_chips, _badgeBack, gold, 0.9f, 1.12f).After(0.8f),
            });
        }
    }
}
