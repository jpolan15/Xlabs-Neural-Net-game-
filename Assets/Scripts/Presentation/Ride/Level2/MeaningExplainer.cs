using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 2, explained. A token's number is a name tag ({name}); each word really is 768 numbers, squashed to three so they can be seen
    /// ({squash}); the three numbers are a place in space, and the real baked picture of GPT-2's forty words appears around the rider,
    /// related words clustering ({space}, {close}); and directions mean things: king - man + woman lands on queen ({math}, real
    /// similarity from the bake). {sort} clears the board for the game, whose galaxy is the same picture.
    /// </summary>
    public sealed class MeaningExplainer : Level2Explainer
    {
        private const int Squares = 96;

        private Level2Kit _kit;
        private ShapeBatch _plate, _grid, _math;
        private GlowField _stars, _links;
        private RibbonField _near;
        private TextMeshPro _title, _cat, _number, _tag, _label768, _label3, _nearList, _mathLine, _mathResult, _royal, _female, _dogLabel;
        private TextMeshPro[] _axis, _words, _mathWords;
        private readonly List<L2GalaxyWord> _all = new List<L2GalaxyWord>();
        private readonly List<int> _labelled = new List<int>();
        private float _galaxy, _close, _dim = 1f;
        private int _catStar = -1, _dogStar = -1;
        private int _plateIndex, _chipIndex, _arrowIndex, _gridFirst;
        private int _mp1, _mp2, _mp3, _mp4, _mLines;

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
            Transform root = MakeContent("MeaningExplain");

            // The name tag.
            var shapes = new List<Shape>();
            _plateIndex = shapes.Count;
            shapes.Add(Level2Kit.Square(Centre, new Vector2(9.2f, 4.2f), new Color(0.04f, 0.03f, 0.13f, 0.9f), 0.06f, new Color(0.62f, 0.5f, 1f, 0.9f), 0.04f));
            _chipIndex = shapes.Count;
            shapes.Add(Level2Kit.Square(Centre + new Vector3(-2.7f, 0.1f, -0.02f), new Vector2(2.0f, 1.0f), new Color(0.3f, 0.85f, 1f, 0.18f), 0.35f, new Color(0.3f, 0.85f, 1f, 0.95f), 0.04f));
            _arrowIndex = shapes.Count;
            shapes.Add(Level2Kit.Line(Centre + new Vector3(-1.4f, 0.1f, -0.02f), Centre + new Vector3(0.7f, 0.1f, -0.02f), 0.05f, new Color(1f, 0.82f, 0.45f, 0.95f)));
            shapes.Add(Level2Kit.Line(Centre + new Vector3(0.7f, 0.1f, -0.02f), Centre + new Vector3(0.45f, 0.28f, -0.02f), 0.05f, new Color(1f, 0.82f, 0.45f, 0.95f)));
            shapes.Add(Level2Kit.Line(Centre + new Vector3(0.7f, 0.1f, -0.02f), Centre + new Vector3(0.45f, -0.08f, -0.02f), 0.05f, new Color(1f, 0.82f, 0.45f, 0.95f)));
            shapes.Add(Level2Kit.Square(Centre + new Vector3(2.7f, 0.1f, -0.02f), new Vector2(2.4f, 1.0f), new Color(1f, 0.82f, 0.45f, 0.14f), 0.35f, new Color(1f, 0.82f, 0.45f, 0.95f), 0.04f));
            foreach (Shape s in shapes) s.alpha = 0f;
            _plate = _kit.Batch(root, "Plate", shapes, 1);
            _title = _kit.Text(root, "Title", "MEANING", 0.42f, new Color(0.78f, 0.7f, 1f), Centre + new Vector3(0f, 2.45f, 0f), 6f, TextAlignmentOptions.Center, false, true);
            _title.characterSpacing = 8f;
            L2Token cat = System.Array.Find(data.File.thread.tokens, t => t.text == " cat");
            _cat = _kit.Text(root, "Cat", "cat", 0.5f, Color.white, Centre + new Vector3(-2.7f, 0.1f, -0.06f), 2f);
            _number = _kit.Text(root, "Number", cat != null ? cat.id.ToString() : "3797", 0.55f, new Color(1f, 0.85f, 0.5f), Centre + new Vector3(2.7f, 0.1f, -0.06f), 2.4f);
            _tag = _kit.Text(root, "Tag", "A NAME TAG, NOT A MEANING", 0.3f, new Color(0.85f, 0.8f, 1f), Centre + new Vector3(0f, -1.25f, -0.06f), 8f);

            // 768 numbers squashed to three: a grid of little squares that fly together into x, y and z.
            var grid = new List<Shape>();
            _gridFirst = 0;
            var rng = new System.Random(2);
            for (int i = 0; i < Squares; i++)
            {
                int col = i % 16, row = i / 16;
                float v = (float)rng.NextDouble();
                Color c = Color.Lerp(new Color(0.18f, 0.2f, 0.45f), new Color(0.55f, 0.95f, 1f), v);
                grid.Add(Level2Kit.Square(Centre + new Vector3(-3.7f + col * 0.2f, 0.9f - row * 0.2f, -0.03f), new Vector2(0.16f, 0.16f), c, 0.2f));
            }

            for (int k = 0; k < 3; k++)
            {
                Color c = k == 0 ? new Color(1f, 0.5f, 0.5f) : k == 1 ? new Color(0.5f, 1f, 0.6f) : new Color(0.5f, 0.7f, 1f);
                grid.Add(Level2Kit.Square(Centre + new Vector3(2.3f + k * 0.9f, 0.1f, -0.03f), new Vector2(0.7f, 0.7f), Fx.A(c, 0.25f), 0.25f, c, 0.04f));
            }

            foreach (Shape s in grid) s.alpha = 0f;
            _grid = _kit.Batch(root, "Squash", grid, 2);
            _label768 = _kit.Text(root, "Label768", "768 NUMBERS", 0.3f, new Color(0.6f, 0.95f, 1f), Centre + new Vector3(-2.1f, -1.35f, -0.06f), 4f);
            _label3 = _kit.Text(root, "Label3", "3 NUMBERS", 0.3f, new Color(1f, 0.85f, 0.5f), Centre + new Vector3(3.2f, -0.75f, -0.06f), 4f);
            _axis = new TextMeshPro[3];
            string[] names = { "x", "y", "z" };
            for (int k = 0; k < 3; k++) _axis[k] = _kit.Text(root, "Axis" + k, names[k], 0.36f, Color.white, Centre + new Vector3(2.3f + k * 0.9f, 0.1f, -0.07f), 0.8f);

            // The galaxy: the real picture of the forty words, around the rider.
            foreach (string category in Level2Data.Neighbourhoods) _all.AddRange(data.Neighbourhood(category));
            _stars = GlowField.Create(root, "Stars", _all.Count + Level2Data.Neighbourhoods.Length * 2, _kit.Glow, 1);
            _links = GlowField.Create(root, "NearSparks", 12, _kit.Glow, 3);
            _near = RibbonField.Create(root, "Near", 3, _kit.Beam, 2);
            _words = new TextMeshPro[Level2Data.Neighbourhoods.Length * 3];
            int n = 0;
            for (int c = 0; c < Level2Data.Neighbourhoods.Length; c++)
            {
                int shown = 0;
                for (int i = 0; i < _all.Count && shown < 3; i++)
                {
                    if (_all[i].category != Level2Data.Neighbourhoods[c]) continue;
                    _labelled.Add(i);
                    _words[n] = _kit.Text(root, "Word" + n, _all[i].word, 0.34f, Color.white, GalaxyMoment.Map(new Vector3(_all[i].x, _all[i].y, _all[i].z)) + new Vector3(0f, 0.32f, 0f), 3f);
                    n++;
                    shown++;
                }
            }

            // Cat and dog are the pair the {close} step joins; dog is named when the line is drawn.
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i].word == "cat") _catStar = i;
                if (_all[i].word == "dog") _dogStar = i;
            }

            if (_dogStar >= 0)
            {
                L2GalaxyWord dog = _all[_dogStar];
                _dogLabel = _kit.Text(root, "DogWord", dog.word, 0.34f, Color.white, GalaxyMoment.Map(new Vector3(dog.x, dog.y, dog.z)) + new Vector3(0f, 0.32f, 0f), 3f);
            }

            L2Neighbours nb = System.Array.Find(data.File.neighbours, x => x.word == "cat");
            string list = "CLOSEST TO CAT (GPT-2)";
            if (nb != null) for (int i = 0; i < Mathf.Min(3, nb.nearest.Length); i++) list += "\n" + nb.nearest[i].word + "   " + nb.nearest[i].similarity.ToString("0.00");
            _nearList = _kit.Text(root, "NearList", list, 0.26f, new Color(0.85f, 1f, 0.9f), Centre + new Vector3(3.9f, -0.2f, 0f), 4f, TextAlignmentOptions.Left);
            _nearList.lineSpacing = 8f;

            // The directions: king - man + woman.
            L2Analogy analogy = System.Array.Find(data.File.analogies, a => a.a == "king");
            float sim = analogy != null && analogy.answers.Length > 0 ? analogy.answers[0].similarity : 0.71f;
            Vector3 m = new Vector3(0f, 2.7f, 6.4f);
            var diagram = new List<Shape>();
            diagram.Add(Level2Kit.Square(m, new Vector2(7.4f, 3.9f), new Color(0.04f, 0.03f, 0.13f, 0.92f), 0.06f, new Color(1f, 0.82f, 0.45f, 0.85f), 0.04f));
            Vector3 man = m + new Vector3(-2.1f, -0.8f, -0.02f), woman = m + new Vector3(0.5f, -0.6f, -0.02f), king = m + new Vector3(-2.1f, 0.75f, -0.02f), queen = m + new Vector3(0.5f, 0.95f, -0.02f);
            _mp1 = diagram.Count;
            foreach (Vector3 p in new[] { man, woman, king, queen }) diagram.Add(Level2Kit.Square(p, new Vector2(0.34f, 0.34f), new Color(1f, 1f, 1f, 0.95f), 1f));
            _mLines = diagram.Count;
            Color green = new Color(0.5f, 1f, 0.65f, 0.95f), yellow = new Color(1f, 0.82f, 0.45f, 0.95f);
            diagram.Add(Level2Kit.Line(man + new Vector3(0f, 0.2f, 0f), king + new Vector3(0f, -0.2f, 0f), 0.06f, green));           // man -> king: royalty
            diagram.Add(Level2Kit.Line(man + new Vector3(0.2f, 0f, 0f), woman + new Vector3(-0.2f, 0f, 0f), 0.06f, yellow));         // man -> woman: female
            diagram.Add(Level2Kit.Line(woman + new Vector3(0f, 0.2f, 0f), queen + new Vector3(0f, -0.2f, 0f), 0.06f, green));        // woman -> queen: the same royalty
            diagram.Add(Level2Kit.Line(king + new Vector3(0.2f, 0f, 0f), queen + new Vector3(-0.2f, 0f, 0f), 0.06f, yellow));        // king -> queen: the same female
            foreach (Shape s in diagram) s.alpha = 0f;
            _math = _kit.Batch(root, "Directions", diagram, 4);
            _mathWords = new TextMeshPro[4];
            string[] mw = { "man", "woman", "king", "queen" };
            Vector3[] at = { man, woman, king, queen };
            for (int k = 0; k < 4; k++) _mathWords[k] = _kit.Text(root, "Math" + mw[k], mw[k], 0.34f, k == 3 ? new Color(1f, 0.85f, 0.45f) : Color.white, at[k] + new Vector3(k % 2 == 0 ? -0.75f : 0.8f, 0f, -0.05f), 1.6f);
            _royal = _kit.Text(root, "Royal", "ROYAL", 0.22f, green, (man + king) * 0.5f + new Vector3(-0.65f, 0f, -0.05f), 1.4f);
            _female = _kit.Text(root, "Female", "FEMALE", 0.22f, yellow, (man + woman) * 0.5f + new Vector3(0f, -0.35f, -0.05f), 1.6f);
            _mathLine = _kit.Text(root, "MathLine", "king - man + woman", 0.4f, Color.white, m + new Vector3(2.9f, 0.5f, -0.05f), 3.4f);
            _mathResult = _kit.Text(root, "MathResult", "= queen", 0.5f, new Color(1f, 0.85f, 0.45f), m + new Vector3(2.9f, -0.15f, -0.05f), 3.4f);
            TextMeshPro sub = _kit.Text(root, "MathSim", "GPT-2 says " + sim.ToString("0.00") + " alike", 0.22f, new Color(0.8f, 0.85f, 1f), m + new Vector3(2.9f, -0.8f, -0.05f), 3.4f);
            _mathWords = AppendText(_mathWords, sub);
        }

        private static TextMeshPro[] AppendText(TextMeshPro[] list, TextMeshPro add)
        {
            var longer = new TextMeshPro[list.Length + 1];
            list.CopyTo(longer, 0);
            longer[list.Length] = add;
            return longer;
        }

        protected override void ResetPieces()
        {
            foreach (ShapeBatch b in new[] { _plate, _grid, _math })
            {
                for (int i = 0; i < b.Count; i++) b[i].alpha = 0f;
                b.MarkDirty();
            }

            foreach (TextMeshPro t in Texts())
            {
                t.gameObject.SetActive(false);
                t.alpha = 1f;
            }

            _galaxy = _close = 0f;
            _dim = 1f;
            _near.HideAll();
        }

        private IEnumerable<TextMeshPro> Texts()
        {
            yield return _title; yield return _cat; yield return _number; yield return _tag; yield return _label768; yield return _label3;
            yield return _nearList; yield return _mathLine; yield return _mathResult; yield return _royal; yield return _female;
            if (_dogLabel != null) yield return _dogLabel;
            foreach (TextMeshPro t in _axis) yield return t;
            foreach (TextMeshPro t in _words) yield return t;
            foreach (TextMeshPro t in _mathWords) yield return t;
        }

        protected override void Animate(float deltaTime)
        {
            // The galaxy of real words: stars twinkle, each neighbourhood in its own colour, and the nearest words of "cat" are joined.
            _stars.HideAll();
            _links.HideAll();
            if (_galaxy <= 0.001f) return;
            L2Data(out Level2Data data);
            for (int i = 0; i < _all.Count; i++)
            {
                L2GalaxyWord w = _all[i];
                Color c = Level2Kit.CategoryColor(w.category);
                float twinkle = 0.75f + 0.25f * Mathf.Sin(Time.time * 2.2f + i * 1.7f);
                c.a = _galaxy * _dim * twinkle;
                _stars.Set(i, GalaxyMoment.Map(new Vector3(w.x, w.y, w.z)), 0.3f + 0.1f * twinkle, c);
            }

            for (int c = 0; c < Level2Data.Neighbourhoods.Length; c++)
            {
                L2Centroid centre = data.Centre(Level2Data.Neighbourhoods[c]);
                Color col = Level2Kit.CategoryColor(Level2Data.Neighbourhoods[c]);
                col.a = 0.2f * _galaxy * _dim;
                Vector3 p = GalaxyMoment.Map(new Vector3(centre.x, centre.y, centre.z));
                _stars.Set(_all.Count + c * 2, p, 4.2f + 0.3f * Mathf.Sin(Time.time * 0.8f + c), col);
                col.a = 0.1f * _galaxy * _dim;
                _stars.Set(_all.Count + c * 2 + 1, p, 7.5f, col);
            }

            if (_close > 0.001f && _catStar >= 0 && _dogStar >= 0)
            {
                Vector3 a = GalaxyMoment.Map(new Vector3(_all[_catStar].x, _all[_catStar].y, _all[_catStar].z));
                Vector3 b = GalaxyMoment.Map(new Vector3(_all[_dogStar].x, _all[_dogStar].y, _all[_dogStar].z));
                _near.Set(0, a, b, 0.35f, 0.07f, new Color(0.7f, 1f, 0.85f, 0.9f * _close), _close);
                _links.Set(0, a, 0.9f + 0.2f * Mathf.Sin(Time.time * 5f), new Color(0.8f, 1f, 0.9f, 0.5f * _close));
                _links.Set(1, b, 0.9f + 0.2f * Mathf.Sin(Time.time * 5f + 1f), new Color(0.8f, 1f, 0.9f, 0.5f * _close));
            }
            else
            {
                _near.HideAll();
            }
        }

        private void L2Data(out Level2Data data) => data = source.Data;

        private Tween Param(float duration, System.Action<float> apply, System.Func<float, float> rate = null) => new Tween(duration, apply, rate);

        protected override void Declare(Steps s)
        {
            Color gold = new Color(1f, 0.82f, 0.45f);

            // "A token's number is just a name tag. The number for cat tells you nothing about cats."
            s.At("name", () => new List<Tween>
            {
                Anim.FadeIn(_plate, _plateIndex, Vector3.zero, 0.6f),
                Anim.Write(_title, 0.7f),
                Anim.FadeIn(_plate, _chipIndex, Vector3.right * 0.3f, 0.5f).After(0.4f),
                Anim.Write(_cat, 0.5f).After(0.5f),
                Anim.FadeIn(_plate, _arrowIndex, Vector3.zero, 0.2f).After(1.0f),
                Anim.Create(_plate, _arrowIndex, 0.5f).After(1.0f),
                Anim.FadeIn(_plate, _arrowIndex + 1, Vector3.zero, 0.2f).After(1.4f),
                Anim.FadeIn(_plate, _arrowIndex + 2, Vector3.zero, 0.2f).After(1.4f),
                Anim.FadeIn(_plate, _arrowIndex + 3, Vector3.left * 0.3f, 0.5f).After(1.3f),
                Anim.Write(_number, 0.6f).After(1.5f),
                Anim.Write(_tag, 1.0f).After(2.4f),
            });

            // "Inside the machine, each word becomes seven hundred and sixty-eight numbers. We squashed them down to three."
            s.At("squash", () =>
            {
                var list = new List<Tween>();
                foreach (TextMeshPro t in new[] { _cat, _number, _tag }) list.Add(Anim.FadeText(t, 0f, 0.5f));
                for (int i = _arrowIndex; i <= _arrowIndex + 3; i++) list.Add(Anim.FadeTo(_plate, i, 0f, 0.4f));
                list.Add(Anim.FadeTo(_plate, _chipIndex, 0f, 0.4f));
                for (int i = 0; i < Squares; i++) list.Add(Anim.FadeIn(_grid, i, Vector3.zero, 0.3f).After(0.5f + 0.008f * i));
                list.Add(Anim.Write(_label768, 0.7f).After(0.8f));
                for (int k = 0; k < 3; k++)
                {
                    list.Add(Anim.FadeIn(_grid, Squares + k, Vector3.zero, 0.4f).After(2.4f + 0.15f * k));
                    list.Add(Anim.Write(_axis[k], 0.4f).After(2.5f + 0.15f * k));
                }

                list.Add(Anim.Write(_label3, 0.6f).After(2.6f));
                for (int i = 0; i < Squares; i++)
                {
                    int idx = i;
                    int target = i % 3;
                    list.Add(Anim.TransformShape(_grid, idx, () =>
                    {
                        Shape t = _grid[idx].Clone();
                        Shape dest = _grid[Squares + target];
                        t.position = dest.position + new Vector3(0f, 0f, 0.01f);
                        t.size = new Vector2(0.3f, 0.3f);
                        t.alpha = 0f;
                        return t;
                    }, 1.2f, Rate.RushInto).After(3.0f + 0.01f * i));
                }

                return list;
            });

            // "Now every word is a point in space." The board clears and the real galaxy appears around the rider.
            s.At("space", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < _plate.Count; i++) list.Add(Anim.FadeTo(_plate, i, 0f, 0.6f));
                for (int i = 0; i < _grid.Count; i++) list.Add(Anim.FadeTo(_grid, i, 0f, 0.5f));
                foreach (TextMeshPro t in new[] { _title, _label768, _label3, _axis[0], _axis[1], _axis[2] }) list.Add(Anim.FadeText(t, 0f, 0.4f));
                list.Add(Param(2.2f, a => _galaxy = a));
                for (int i = 0; i < _words.Length; i++) list.Add(Anim.Write(_words[i], 0.5f).After(0.6f + 0.12f * i));
                return list;
            });

            // "Words that mean similar things end up close together." Cat and dog are joined; the real nearest words are listed.
            s.At("close", () =>
            {
                var list = new List<Tween>
                {
                    Param(0.9f, a => _close = a),
                    Anim.Write(_nearList, 1.6f).After(0.5f),
                };
                if (_dogLabel != null) list.Add(Anim.Write(_dogLabel, 0.4f).After(0.2f));
                return list;
            });

            // "Even directions mean something. king, subtract man, add woman... you land on queen."
            s.At("math", () =>
            {
                var list = new List<Tween>
                {
                    Param(0.8f, a => _dim = Mathf.Lerp(1f, 0.25f, a)),
                    Anim.FadeText(_nearList, 0f, 0.4f),
                    Anim.FadeIn(_math, 0, Vector3.zero, 0.6f).After(0.2f),
                };
                for (int k = 0; k < 3; k++)
                {
                    list.Add(Anim.FadeIn(_math, _mp1 + k, Vector3.zero, 0.3f).After(0.7f + 0.2f * k));
                    list.Add(Anim.Write(_mathWords[k], 0.4f).After(0.8f + 0.2f * k));
                }

                list.Add(Anim.FadeIn(_math, _mLines, Vector3.zero, 0.2f).After(1.6f));
                list.Add(Anim.Create(_math, _mLines, 0.7f).After(1.6f));
                list.Add(Anim.Write(_royal, 0.4f).After(1.9f));
                list.Add(Anim.FadeIn(_math, _mLines + 1, Vector3.zero, 0.2f).After(2.6f));
                list.Add(Anim.Create(_math, _mLines + 1, 0.7f).After(2.6f));
                list.Add(Anim.Write(_female, 0.4f).After(2.9f));
                list.Add(Anim.Write(_mathLine, 1.2f).After(3.2f));
                return list;
            });

            // "You land on queen."
            s.At("queen", () => new List<Tween>
            {
                Anim.FadeIn(_math, _mp1 + 3, Vector3.zero, 0.3f),
                Anim.Write(_mathWords[3], 0.5f),
                Anim.FadeIn(_math, _mLines + 2, Vector3.zero, 0.2f),
                Anim.Create(_math, _mLines + 2, 0.7f),
                Anim.FadeIn(_math, _mLines + 3, Vector3.zero, 0.2f).After(0.5f),
                Anim.Create(_math, _mLines + 3, 0.7f).After(0.5f),
                Anim.Write(_mathResult, 0.8f).After(0.9f),
                Anim.Write(_mathWords[4], 0.8f).After(1.5f),
                Anim.Indicate(_math, _mp1 + 3, gold, 1.2f, 1.6f).After(1.2f),
            });

            // "Your turn: sort the loose words."
            s.At("sort", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < _math.Count; i++) list.Add(Anim.FadeTo(_math, i, 0f, 0.6f));
                foreach (TextMeshPro t in Texts()) if (t.gameObject.activeSelf) list.Add(Anim.FadeText(t, 0f, 0.6f));
                list.Add(Param(0.8f, a => _dim = Mathf.Lerp(0.25f, 1f, a)));
                list.Add(Param(1.0f, a => _galaxy = 1f - a));
                return list;
            });
        }
    }
}
