using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 5, explained, with GPT-2's real numbers on "My robot loves to ...". The machine picks one word ({pick}) from its real odds,
    /// adds it to the sentence and runs again ({again}), three times over: "play", "with", "me". Then the one knob, temperature
    /// ({temp}): cold ({cold}) the odds pile onto the safest word, hot ({hot}) they go nearly flat: the same prompt at 0.2 and 2.5,
    /// from the bake. {road} clears the board for the game.
    /// </summary>
    public sealed class WritingExplainer : Level2Explainer
    {
        private const int Lanes = 4;
        private const int Temps = 6;

        private Level2Kit _kit;
        private ShapeBatch _batch;
        private GlowField _glows;
        private TextMeshPro _title, _sentence, _again, _knob, _coldName, _hotName, _coldNote, _hotNote, _flying;
        private TextMeshPro[] _names, _percent, _coldWords, _hotWords;
        private L2StoryNode[] _rounds;
        private L2TempSetting _cold, _hot;
        private int _plate, _barFirst, _coldFirst, _hotFirst, _dial, _needle;
        private float[] _grow = new float[Lanes];
        private float[] _coldGrow = new float[Temps], _hotGrow = new float[Temps];
        private float _needleAt = 0.5f, _loop;
        private int _round;
        private string _written;
        private static readonly Vector3 Centre = new Vector3(0f, 2.7f, 8f);
        private static readonly Color[] Cols = { new Color(0.45f, 1f, 0.72f), new Color(0.3f, 0.85f, 1f), new Color(1f, 0.82f, 0.4f), new Color(1f, 0.42f, 0.72f), new Color(0.65f, 0.5f, 1f), new Color(1f, 0.6f, 0.4f) };

        protected override void Awake()
        {
            Construct();
            base.Awake();
        }

        private Vector3 LaneBase(int i) => Centre + new Vector3(-3.3f + i * 2.2f, -1.25f, -0.05f);

        private Vector3 TempBase(bool hot, int i) => Centre + new Vector3((hot ? 3.5f : -3.5f) + (i - 2.5f) * 0.62f, -1.2f, -0.05f);

        private void Construct()
        {
            _kit = Level2Kit.Current;
            Level2Data data = source.Data;
            _rounds = new[] { data.StoryNode(""), data.StoryNode("0"), data.StoryNode("00") };
            _cold = data.Temperature(0.2f);
            _hot = data.Temperature(2.5f);
            Transform root = MakeContent("WritingExplain");

            var shapes = new List<Shape>();
            _plate = shapes.Count;
            shapes.Add(Level2Kit.Square(Centre + new Vector3(0f, 0.2f, 0.2f), new Vector2(11.6f, 5.0f), new Color(0.02f, 0.07f, 0.09f, 0.9f), 0.05f, new Color(1f, 0.85f, 0.45f, 0.9f), 0.04f));
            _barFirst = shapes.Count;
            for (int i = 0; i < Lanes; i++) shapes.Add(Level2Kit.Square(LaneBase(i), new Vector2(1.1f, 0.05f), Fx.A(Cols[i], 0.5f), 0.18f, Cols[i], 0.045f));
            _coldFirst = shapes.Count;
            for (int i = 0; i < Temps; i++) shapes.Add(Level2Kit.Square(TempBase(false, i), new Vector2(0.46f, 0.05f), Fx.A(Cols[i], 0.5f), 0.2f, Cols[i], 0.035f));
            _hotFirst = shapes.Count;
            for (int i = 0; i < Temps; i++) shapes.Add(Level2Kit.Square(TempBase(true, i), new Vector2(0.46f, 0.05f), Fx.A(Cols[i], 0.5f), 0.2f, Cols[i], 0.035f));
            _dial = shapes.Count;
            shapes.Add(Level2Kit.Square(Centre + new Vector3(0f, -0.2f, -0.05f), new Vector2(2.2f, 2.2f), new Color(0.1f, 0.1f, 0.2f, 0.9f), 1f, new Color(1f, 0.85f, 0.45f, 0.9f), 0.05f));
            _needle = shapes.Count;
            shapes.Add(Level2Kit.Line(Centre + new Vector3(0f, -0.2f, -0.08f), Centre + new Vector3(0f, 0.7f, -0.08f), 0.08f, new Color(1f, 1f, 1f, 1f)));
            foreach (Shape s in shapes) s.alpha = 0f;
            _batch = _kit.Batch(root, "Writing", shapes, 1);

            _title = _kit.Text(root, "Title", "WRITING", 0.42f, new Color(1f, 0.88f, 0.55f), Centre + new Vector3(0f, 3.05f, 0f), 6f, TextAlignmentOptions.Center, false, true);
            _title.characterSpacing = 8f;
            _written = data.File.story.start;
            _sentence = _kit.Text(root, "Sentence", _written, 0.5f, Color.white, Centre + new Vector3(0f, 1.85f, -0.1f), 11f);
            _names = new TextMeshPro[Lanes];
            _percent = new TextMeshPro[Lanes];
            for (int i = 0; i < Lanes; i++)
            {
                _names[i] = _kit.Text(root, "Lane" + i, "", 0.36f, Color.white, LaneBase(i) + new Vector3(0f, -0.42f, -0.05f), 2f);
                _percent[i] = _kit.Text(root, "Odds" + i, "", 0.32f, Cols[i], LaneBase(i) + new Vector3(0f, 0.5f, -0.05f), 2f);
            }

            _flying = _kit.Text(root, "Flying", "", 0.4f, new Color(1f, 0.9f, 0.5f), Vector3.zero, 3f);
            _again = _kit.Text(root, "Again", "RUN IT AGAIN", 0.28f, new Color(1f, 0.85f, 0.45f), Centre + new Vector3(3.9f, 0.7f, -0.1f), 3.6f);
            _knob = _kit.Text(root, "Knob", "TEMPERATURE", 0.3f, Color.white, Centre + new Vector3(0f, -1.6f, -0.1f), 5f);
            _coldName = _kit.Text(root, "ColdName", "COLD  0.2", 0.34f, new Color(0.5f, 0.85f, 1f), Centre + new Vector3(-3.5f, 1.6f, -0.1f), 4f);
            _hotName = _kit.Text(root, "HotName", "HOT  2.5", 0.34f, new Color(1f, 0.55f, 0.45f), Centre + new Vector3(3.5f, 1.6f, -0.1f), 4f);
            _coldNote = _kit.Text(root, "ColdNote", "ALWAYS THE SAFE WORD", 0.22f, new Color(0.7f, 0.9f, 1f), Centre + new Vector3(-3.5f, -1.95f, -0.1f), 4f);
            _hotNote = _kit.Text(root, "HotNote", "ANY WORD IS FAIR GAME", 0.22f, new Color(1f, 0.75f, 0.65f), Centre + new Vector3(3.5f, -1.95f, -0.1f), 4f);
            _coldWords = new TextMeshPro[Temps];
            _hotWords = new TextMeshPro[Temps];
            for (int i = 0; i < Temps; i++)
            {
                _coldWords[i] = _kit.Text(root, "Cold" + i, i < _cold.top.Length ? Level2Data.Clean(_cold.top[i].word) : "", 0.17f, Color.white, TempBase(false, i) + new Vector3(0f, -0.3f, -0.05f), 0.8f);
                _hotWords[i] = _kit.Text(root, "Hot" + i, i < _hot.top.Length ? Level2Data.Clean(_hot.top[i].word) : "", 0.17f, Color.white, TempBase(true, i) + new Vector3(0f, -0.3f, -0.05f), 0.8f);
            }

            _glows = GlowField.Create(root, "Glows", 6, _kit.Ring, 3);
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

            _grow = new float[Lanes];
            _coldGrow = new float[Temps];
            _hotGrow = new float[Temps];
            _needleAt = 0.5f;
            _loop = 0f;
            _round = 0;
            _written = source.Data.File.story.start;
            _sentence.text = _written;
            _glows.HideAll();
        }

        private IEnumerable<TextMeshPro> Texts()
        {
            yield return _title; yield return _sentence; yield return _again; yield return _knob; yield return _coldName; yield return _hotName;
            yield return _coldNote; yield return _hotNote; yield return _flying;
            foreach (TextMeshPro t in _names) yield return t;
            foreach (TextMeshPro t in _percent) yield return t;
            foreach (TextMeshPro t in _coldWords) yield return t;
            foreach (TextMeshPro t in _hotWords) yield return t;
        }

        protected override void Animate(float deltaTime)
        {
            L2StoryNode node = _rounds[Mathf.Clamp(_round, 0, _rounds.Length - 1)];
            float biggest = 0.01f;
            for (int i = 0; i < Lanes; i++) biggest = Mathf.Max(biggest, node.lanes[i].p);
            for (int i = 0; i < Lanes; i++)
            {
                Shape bar = _batch[_barFirst + i];
                float h = Mathf.Max(0.05f, node.lanes[i].p / biggest * 2.2f * _grow[i]);
                bar.size = new Vector2(1.1f, h);
                bar.position = LaneBase(i) + new Vector3(0f, h * 0.5f, 0f);
                _percent[i].transform.localPosition = LaneBase(i) + new Vector3(0f, h + 0.3f, -0.05f);
            }

            for (int i = 0; i < Temps; i++)
            {
                float hc = Mathf.Max(0.04f, i < _cold.top.Length ? _cold.top[i].p * 2.6f * _coldGrow[i] : 0f);
                Shape c = _batch[_coldFirst + i];
                c.size = new Vector2(0.46f, hc);
                c.position = TempBase(false, i) + new Vector3(0f, hc * 0.5f, 0f);
                // The hot chart is drawn tall enough to see how flat it is: its bars are scaled by the same number as the cold ones, and are tiny.
                float hh = Mathf.Max(0.04f, i < _hot.top.Length ? _hot.top[i].p * 2.6f * _hotGrow[i] : 0f);
                Shape h = _batch[_hotFirst + i];
                h.size = new Vector2(0.46f, hh);
                h.position = TempBase(true, i) + new Vector3(0f, hh * 0.5f, 0f);
            }

            // The needle swings between cold and hot.
            Shape needle = _batch[_needle];
            float angle = Mathf.Lerp(-70f, 70f, _needleAt) * Mathf.Deg2Rad;
            Vector3 pivot = Centre + new Vector3(0f, -0.2f, -0.08f);
            needle.position = pivot;
            needle.end = pivot + new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * 0.85f;
            _batch.MarkDirty();

            _glows.HideAll();
            if (_loop > 0.001f) _glows.Set(0, Centre + new Vector3(4.6f, 0f, -0.1f), new Vector2(1.6f, 1.6f), new Color(1f, 0.85f, 0.45f, 0.7f * _loop), -Time.time * 2.4f);
        }

        private Tween Param(float duration, System.Action<float> apply) => new Tween(duration, apply, Rate.Smooth);

        private List<Tween> Choose(int round, float at)
        {
            L2StoryNode node = _rounds[round];
            string word = Level2Data.Clean(node.lanes[0].word);
            var list = new List<Tween>
            {
                Tween.Call(at, () => _round = round),
            };
            for (int i = 0; i < Lanes; i++)
            {
                int k = i;
                list.Add(Tween.Call(at, () =>
                {
                    _names[k].text = Level2Data.Clean(node.lanes[k].word);
                    _percent[k].text = FormatPercent(node.lanes[k].p);
                    _grow[k] = 0f;
                }));
                list.Add(Anim.FadeIn(_batch, _barFirst + i, Vector3.zero, 0.2f).After(at));
                list.Add(Anim.FadeText(_names[i], 1f, 0.3f).After(at + 0.05f));
                list.Add(Anim.FadeText(_percent[i], 1f, 0.3f).After(at + 0.4f));
                list.Add(Param(0.9f, a => _grow[k] = a).After(at + 0.1f * i));
            }

            // The machine rolls and picks the first lane: it glows, and its word flies to the end of the sentence.
            list.Add(Anim.Indicate(_batch, _barFirst, new Color(1f, 0.9f, 0.5f), 1.0f, 1.12f).After(at + 1.3f));
            list.Add(Tween.Call(at + 1.6f, () =>
            {
                _flying.text = word;
                _flying.gameObject.SetActive(true);
                _flying.alpha = 1f;
            }));
            list.Add(new Tween(0.7f, a =>
            {
                Vector3 from = _names[0].transform.localPosition;
                Vector3 to = _sentence.transform.localPosition + new Vector3(_sentence.preferredWidth * 0.5f + 0.7f, 0f, 0f);
                _flying.transform.localPosition = Vector3.Lerp(from, to, a) + new Vector3(0f, Mathf.Sin(a * Mathf.PI) * 0.6f, 0f);
            }, Rate.Smooth, end: () =>
            {
                _written += " " + word;
                _sentence.text = _written;
                _flying.gameObject.SetActive(false);
            }) { Delay = at + 1.6f });
            return list;
        }

        private static string FormatPercent(float p)
        {
            float pct = p * 100f;
            return pct >= 10f ? Mathf.RoundToInt(pct) + "%" : pct.ToString("0.0") + "%";
        }

        protected override void Declare(Steps s)
        {
            Color gold = new Color(1f, 0.82f, 0.45f);

            // "The machine picks one word."
            s.At("pick", () =>
            {
                var list = new List<Tween> { Anim.FadeIn(_batch, _plate, Vector3.zero, 0.6f), Anim.Write(_title, 0.7f), Anim.Write(_sentence, 1.4f).After(0.4f) };
                list.AddRange(Choose(0, 1.2f));
                return list;
            });

            // "It adds that word to the sentence, and runs the whole machine again. One word at a time."
            s.At("again", () =>
            {
                var list = new List<Tween> { Param(0.6f, a => _loop = a), Anim.Write(_again, 0.8f) };
                list.AddRange(Choose(1, 2.4f));
                list.AddRange(Choose(2, 5.6f));
                return list;
            });

            // "There's one knob: temperature."
            s.At("temp", () =>
            {
                var list = new List<Tween>
                {
                    Param(0.5f, a => _loop = 1f - a),
                    Anim.FadeText(_again, 0f, 0.4f),
                    Anim.FadeText(_sentence, 0f, 0.5f),
                };
                for (int i = 0; i < Lanes; i++)
                {
                    list.Add(Anim.FadeTo(_batch, _barFirst + i, 0f, 0.5f));
                    list.Add(Anim.FadeText(_names[i], 0f, 0.4f));
                    list.Add(Anim.FadeText(_percent[i], 0f, 0.4f));
                }

                list.Add(Anim.FadeIn(_batch, _dial, Vector3.zero, 0.6f).After(0.5f));
                list.Add(Anim.FadeIn(_batch, _needle, Vector3.zero, 0.6f).After(0.6f));
                list.Add(Anim.Write(_knob, 0.9f).After(0.7f));
                return list;
            });

            // "Turn it cold, and the machine plays it safe."
            s.At("cold", () =>
            {
                var list = new List<Tween> { Param(1.2f, a => _needleAt = Mathf.Lerp(0.5f, 0.02f, a)), Anim.Write(_coldName, 0.6f) };
                for (int i = 0; i < Temps; i++)
                {
                    int k = i;
                    list.Add(Anim.FadeIn(_batch, _coldFirst + i, Vector3.zero, 0.2f).After(0.1f * i));
                    list.Add(Param(0.9f, a => _coldGrow[k] = a).After(0.15f + 0.1f * i));
                    list.Add(Anim.Write(_coldWords[i], 0.4f).After(0.3f + 0.1f * i));
                }

                list.Add(Anim.Write(_coldNote, 0.8f).After(1.2f));
                return list;
            });

            // "Turn it hot, and it takes wild risks."
            s.At("hot", () =>
            {
                var list = new List<Tween> { Param(1.4f, a => _needleAt = Mathf.Lerp(0.02f, 0.98f, a)), Anim.Write(_hotName, 0.6f) };
                for (int i = 0; i < Temps; i++)
                {
                    int k = i;
                    list.Add(Anim.FadeIn(_batch, _hotFirst + i, Vector3.zero, 0.2f).After(0.1f * i));
                    list.Add(Param(0.9f, a => _hotGrow[k] = a).After(0.15f + 0.1f * i));
                    list.Add(Anim.Write(_hotWords[i], 0.4f).After(0.3f + 0.1f * i));
                }

                list.Add(Anim.Write(_hotNote, 0.8f).After(1.2f));
                return list;
            });

            // "Your turn: steer the story."
            s.At("road", () =>
            {
                var list = new List<Tween> { Anim.FadeTo(_batch, _plate, 0f, 0.7f) };
                for (int i = 1; i < _batch.Count; i++) list.Add(Anim.FadeTo(_batch, i, 0f, 0.6f));
                foreach (TextMeshPro t in Texts()) if (t.gameObject.activeSelf) list.Add(Anim.FadeText(t, 0f, 0.6f));
                return list;
            });
        }
    }
}
