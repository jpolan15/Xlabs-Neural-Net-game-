using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// The finale. Pixel, a flat friendly face, says its first sentence: the story the rider steered, typed in a speech bubble while its mouth
    /// moves ({talk}). The whole machine once more, a loop of six steps lit one per clause ({recap}). Then the size of things: Pixel's 970
    /// numbers, GPT-2's 124 million, the biggest chatbots' hundreds of billions ({scale}); the warning that likely is not true ({careful});
    /// and the last words ({end}).
    /// </summary>
    public sealed class OutroExplainer : Level2Explainer
    {
        [SerializeField] private Level2Session session;

        private Level2Kit _kit;
        private ShapeBatch _batch;
        private GlowField _glows;
        private static readonly string[] Loop = { "TOKENS", "MEANING", "ATTENTION", "ODDS", "ROLL", "AGAIN" };

        private TextMeshPro _story, _name, _small, _mid, _big, _end, _tag, _careful, _check;
        private TextMeshPro[] _loopText;
        private int _loopFirst, _loopCount, _warnCard, _screen;
        private int _face, _eyeL, _eyeR, _mouth, _bubble, _tail, _dotA, _dotB, _dotC;
        private bool _talking, _happy;
        private float _blink;
        private float _scaleIn, _faceIn;
        private static readonly Vector3 Centre = new Vector3(0f, 2.6f, 8f);
        private static readonly Vector3 FaceAt = new Vector3(-3.4f, 2.5f, 8f);

        protected override void Awake()
        {
            Construct();
            base.Awake();
        }

        // The finale stays up to the last word (keepContent), then steps aside for the end card, which hangs in front of it.
        protected override void OnEnable()
        {
            base.OnEnable();
            if (director != null) director.RideCompleted += OnRideCompleted;
        }

        protected override void OnDisable()
        {
            if (director != null) director.RideCompleted -= OnRideCompleted;
            base.OnDisable();
        }

        private void OnRideCompleted()
        {
            if (content != null) content.SetActive(false);
        }

        private void Construct()
        {
            _kit = Level2Kit.Current;
            Transform root = MakeContent("Finale");
            var shapes = new List<Shape>();
            Color cyan = new Color(0.3f, 0.88f, 1f);
            // The finish line's big screen: a dark board behind everything the finale shows, so it reads against the dusk (ADR-023).
            _screen = shapes.Count;
            shapes.Add(Level2Kit.Square(new Vector3(0.6f, 2.85f, 8.45f), new Vector2(14.2f, 5.0f), new Color(0.03f, 0.04f, 0.1f, 0.98f), 0.05f, new Color(1f, 0.85f, 0.5f, 0.95f), 0.05f));
            _face = shapes.Count;
            shapes.Add(Level2Kit.Square(FaceAt, new Vector2(2.3f, 1.9f), new Color(0.04f, 0.14f, 0.22f, 0.95f), 0.3f, cyan, 0.08f));
            _eyeL = shapes.Count;
            shapes.Add(Level2Kit.Square(FaceAt + new Vector3(-0.5f, 0.3f, -0.02f), new Vector2(0.34f, 0.44f), cyan, 0.5f));
            _eyeR = shapes.Count;
            shapes.Add(Level2Kit.Square(FaceAt + new Vector3(0.5f, 0.3f, -0.02f), new Vector2(0.34f, 0.44f), cyan, 0.5f));
            _mouth = shapes.Count;
            shapes.Add(Level2Kit.Square(FaceAt + new Vector3(0f, -0.4f, -0.02f), new Vector2(0.9f, 0.1f), cyan, 0.5f));
            _bubble = shapes.Count;
            shapes.Add(Level2Kit.Square(new Vector3(2.3f, 2.9f, 8f), new Vector2(6.6f, 2.2f), new Color(0.06f, 0.07f, 0.2f, 0.94f), 0.2f, new Color(1f, 0.9f, 0.55f, 0.95f), 0.05f));
            _tail = shapes.Count;
            shapes.Add(Level2Kit.Line(new Vector3(-1.1f, 2.7f, 8f), new Vector3(-0.8f, 2.55f, 8f), 0.3f, new Color(1f, 0.9f, 0.55f, 0.95f)));
            _dotA = shapes.Count;
            shapes.Add(Level2Kit.Square(new Vector3(-4.6f, 2.5f, 8f), new Vector2(0.14f, 0.14f), new Color(0.5f, 1f, 0.72f), 1f));
            _dotB = shapes.Count;
            shapes.Add(Level2Kit.Square(new Vector3(-0.8f, 2.5f, 8f), new Vector2(1.5f, 1.5f), new Color(0.6f, 0.5f, 1f, 0.3f), 1f, new Color(0.6f, 0.5f, 1f, 1f), 0.06f));
            _dotC = shapes.Count;
            shapes.Add(Level2Kit.Square(new Vector3(4.9f, 2.8f, 8.2f), new Vector2(4.2f, 4.2f), new Color(1f, 0.5f, 0.75f, 0.18f), 1f, new Color(1f, 0.5f, 0.75f, 1f), 0.07f));

            // The recap loop: six chips in a row joined by arrows, and the way back from AGAIN to TOKENS underneath.
            _loopFirst = shapes.Count;
            for (int i = 0; i < Loop.Length; i++)
            {
                Color c = Color.Lerp(new Color(0.3f, 0.88f, 1f), new Color(1f, 0.6f, 0.82f), i / (Loop.Length - 1f));
                shapes.Add(Level2Kit.Square(LoopAt(i), new Vector2(1.55f, 0.75f), new Color(0.04f, 0.06f, 0.16f, 0.95f), 0.35f, c, 0.05f));
            }

            Color way = new Color(1f, 0.9f, 0.6f, 0.95f);
            for (int i = 0; i < Loop.Length - 1; i++) shapes.Add(Level2Kit.Line(LoopAt(i) + new Vector3(0.82f, 0f, 0f), LoopAt(i + 1) - new Vector3(0.82f, 0f, 0f), 0.05f, way));
            Vector3 under = new Vector3(0f, -0.75f, 0f);
            shapes.Add(Level2Kit.Line(LoopAt(Loop.Length - 1) - new Vector3(0f, 0.4f, 0f), LoopAt(Loop.Length - 1) + under, 0.05f, way));
            shapes.Add(Level2Kit.Line(LoopAt(Loop.Length - 1) + under, LoopAt(0) + under, 0.05f, way));
            shapes.Add(Level2Kit.Line(LoopAt(0) + under, LoopAt(0) - new Vector3(0f, 0.4f, 0f), 0.05f, way));
            _loopCount = shapes.Count - _loopFirst;

            // The warning card.
            _warnCard = shapes.Count;
            shapes.Add(Level2Kit.Square(new Vector3(2.2f, 2.7f, 8.05f), new Vector2(8.2f, 2.6f), new Color(0.12f, 0.04f, 0.06f, 0.95f), 0.12f, new Color(1f, 0.5f, 0.42f, 0.95f), 0.06f));
            foreach (Shape s in shapes) s.alpha = 0f;
            _batch = _kit.Batch(root, "Pixel", shapes, 1);
            _loopText = new TextMeshPro[Loop.Length];
            for (int i = 0; i < Loop.Length; i++) _loopText[i] = _kit.Text(root, "Loop" + i, Loop[i], 0.24f, Color.white, LoopAt(i) + new Vector3(0f, 0f, -0.05f), 1.5f);
            _careful = _kit.Text(root, "Careful", "LIKELY IS NOT THE SAME AS TRUE", 0.38f, Color.white, new Vector3(2.2f, 3.05f, 7.95f), 7.8f);
            _check = _kit.Text(root, "Check", "ALWAYS CHECK", 0.5f, new Color(1f, 0.6f, 0.5f), new Vector3(2.2f, 2.2f, 7.95f), 6f);

            _name = _kit.Text(root, "Name", "PIXEL", 0.36f, cyan, FaceAt + new Vector3(0f, 1.3f, -0.05f), 3f, TextAlignmentOptions.Center, false, true);
            _name.characterSpacing = 8f;
            string story = session != null && !string.IsNullOrEmpty(session.Story) ? session.Story : "My robot loves to talk to me.";
            _story = _kit.Text(root, "Story", "", 0.46f, Color.white, new Vector3(2.3f, 2.9f, 7.95f), 6.0f);
            _story.textWrappingMode = TextWrappingModes.Normal;
            _story.rectTransform.sizeDelta = new Vector2(6.0f, 2.0f);
            _story.text = story;
            _small = _kit.Text(root, "Small", "970\nPIXEL", 0.3f, new Color(0.5f, 1f, 0.72f), new Vector3(-4.6f, 1.3f, 7.95f), 2.4f);
            _mid = _kit.Text(root, "Mid", "124 MILLION\nGPT-2", 0.3f, new Color(0.75f, 0.65f, 1f), new Vector3(-0.8f, 1.2f, 7.95f), 3.2f);
            _big = _kit.Text(root, "Big", "HUNDREDS OF\nBILLIONS", 0.34f, new Color(1f, 0.6f, 0.82f), new Vector3(4.9f, 2.8f, 7.95f), 4.0f);
            _tag = _kit.Text(root, "Tag", "NUMBERS IN THE BRAIN", 0.3f, new Color(0.9f, 0.92f, 1f), new Vector3(0f, 4.55f, 7.95f), 8f);
            _end = _kit.Text(root, "End", "NOTHING MAGIC.\nJUST NUMBERS, AND A VERY GOOD GUESS.", 0.44f, new Color(1f, 0.9f, 0.6f), new Vector3(0.6f, 4.2f, 7.95f), 13f);
            _end.rectTransform.sizeDelta = new Vector2(13f, 2f);
            _glows = GlowField.Create(root, "Sparks", 40, _kit.Glow, 4);
        }

        private static Vector3 LoopAt(int i) => new Vector3(-4.75f + i * 1.9f, 3.75f, 7.9f);

        private IEnumerable<TextMeshPro> Texts()
        {
            foreach (TextMeshPro t in new[] { _story, _name, _small, _mid, _big, _end, _tag, _careful, _check }) yield return t;
            foreach (TextMeshPro t in _loopText) yield return t;
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

            _talking = _happy = false;
            _scaleIn = _faceIn = 0f;
            _glows.HideAll();
        }

        protected override void Animate(float deltaTime)
        {
            // Pixel: it blinks, and its mouth moves while it talks.
            _blink = Mathf.Repeat(Time.time, 3.2f);
            float eye = _blink < 0.12f ? 0.12f : 1f;
            float pop = Fx.Pop(_faceIn);
            Shape face = _batch[_face];
            face.scale = pop;
            Shape l = _batch[_eyeL], r = _batch[_eyeR], m = _batch[_mouth];
            l.scale = r.scale = pop;
            l.size = r.size = new Vector2(0.34f, 0.44f * eye * (_happy ? 0.7f : 1f));
            float open = _talking ? 0.08f + 0.2f * Mathf.Abs(Mathf.Sin(Time.time * 11f)) : _happy ? 0.28f : 0.07f;
            m.size = new Vector2(_happy ? 1.2f : 0.9f, open);
            m.round = 0.5f;
            m.scale = pop;
            _batch.MarkDirty();

            _glows.HideAll();
            if (_happy)
            {
                for (int i = 0; i < 40; i++)
                {
                    float t = Mathf.Repeat(Time.time * 0.4f + i * 0.137f, 1f);
                    float a = i * 2.4f;
                    Vector3 p = new Vector3(Mathf.Cos(a) * (2f + 6f * t), 2.6f + Mathf.Sin(a) * (1.5f + 3f * t), 8f - t);
                    _glows.Set(i, p, 0.2f + 0.2f * (1f - t), Fx.A(Color.Lerp(new Color(1f, 0.9f, 0.5f), new Color(0.6f, 0.9f, 1f), (i % 3) / 2f), 0.9f * (1f - t)));
                }
            }
        }

        private Tween Param(float duration, System.Action<float> apply) => new Tween(duration, apply, Rate.Smooth);

        protected override void Declare(Steps s)
        {
            // "And that's how Pixel learns to talk." Pixel appears and says the rider's own story.
            s.At("talk", () =>
            {
                // Whatever the rider steered at the last stop (or the example story if they skipped it).
                _story.text = session != null && !string.IsNullOrEmpty(session.Story) ? session.Story : "My robot loves to talk to me.";
                return Talk();
            });

            // "Let's ride the whole machine once more." The loop appears; each next clause lights its step.
            s.AtClause(RideLine.OutRecap, 0, RecapStep);
            for (int k = 0; k < Loop.Length; k++)
            {
                int i = k;
                s.AtClause(RideLine.OutRecap, k + 1, () => new List<Tween>
                {
                    Anim.FadeTo(_batch, _loopFirst + i, 1f, 0.3f),
                    Anim.Indicate(_batch, _loopFirst + i, new Color(1f, 0.85f, 0.45f), 0.9f, 1.15f),
                    Anim.FadeText(_loopText[i], 1f, 0.3f),
                });
            }

            s.At("scale", ScaleStep);
            s.At("careful", CarefulStep);
            s.At("end", EndStep);
        }

        private List<Tween> Talk()
        {
            return new List<Tween>
            {
                Anim.FadeIn(_batch, _screen, Vector3.zero, 0.6f),
                Param(0.7f, a => _faceIn = a),
                Anim.FadeIn(_batch, _face, Vector3.zero, 0.5f),
                Anim.FadeIn(_batch, _eyeL, Vector3.zero, 0.5f),
                Anim.FadeIn(_batch, _eyeR, Vector3.zero, 0.5f),
                Anim.FadeIn(_batch, _mouth, Vector3.zero, 0.5f),
                Anim.Write(_name, 0.6f).After(0.4f),
                Anim.FadeIn(_batch, _bubble, Vector3.left * 0.4f, 0.5f).After(0.9f),
                Anim.FadeIn(_batch, _tail, Vector3.zero, 0.4f).After(0.9f),
                Tween.Call(1.5f, () => _talking = true),
                Anim.Write(_story, 3.0f).After(1.5f),
                Tween.Call(4.6f, () => _talking = false),
            };
        }

        private List<Tween> RecapStep()
        {
            var list = new List<Tween>
            {
                Anim.FadeTo(_batch, _face, 0f, 0.6f), Anim.FadeTo(_batch, _eyeL, 0f, 0.6f), Anim.FadeTo(_batch, _eyeR, 0f, 0.6f), Anim.FadeTo(_batch, _mouth, 0f, 0.6f),
                Anim.FadeOut(_batch, _bubble, Vector3.up * 0.2f, 0.5f), Anim.FadeOut(_batch, _tail, Vector3.zero, 0.3f),
                Anim.FadeText(_story, 0f, 0.4f), Anim.FadeText(_name, 0f, 0.4f),
            };
            for (int i = 0; i < _loopCount; i++) list.Add(Anim.FadeIn(_batch, _loopFirst + i, Vector3.zero, 0.5f).After(0.3f + 0.05f * i));
            for (int i = 0; i < Loop.Length; i++)
            {
                list.Add(Anim.Write(_loopText[i], 0.4f).After(0.4f + 0.08f * i));
                list.Add(Anim.FadeTo(_batch, _loopFirst + i, 0.45f, 0.4f).After(1.2f));
                list.Add(Anim.FadeText(_loopText[i], 0.45f, 0.4f).After(1.2f));
            }

            return list;
        }

        private IEnumerable<Tween> FadeLoop()
        {
            for (int i = 0; i < _loopCount; i++) yield return Anim.FadeTo(_batch, _loopFirst + i, 0f, 0.5f);
            foreach (TextMeshPro t in _loopText) yield return Anim.FadeText(t, 0f, 0.4f);
        }

        // "One last thing. The machine picks words that sound likely, not words that are true... Always check."
        private List<Tween> CarefulStep()
        {
            var list = new List<Tween>
            {
                Anim.FadeTo(_batch, _dotA, 0f, 0.6f), Anim.FadeTo(_batch, _dotB, 0f, 0.6f), Anim.FadeTo(_batch, _dotC, 0f, 0.6f),
                Anim.FadeText(_small, 0f, 0.5f), Anim.FadeText(_mid, 0f, 0.5f), Anim.FadeText(_big, 0f, 0.5f), Anim.FadeText(_tag, 0f, 0.5f),
                Anim.FadeIn(_batch, _warnCard, Vector3.zero, 0.6f).After(0.4f),
                Anim.Write(_careful, 1.4f).After(0.8f),
                Anim.Write(_check, 0.8f).After(4.2f),
            };
            list.AddRange(FadeLoop());
            return list;
        }

        // "Pixel's number brain had nine hundred and seventy numbers. GPT-2 has one hundred and twenty-four million..."
        private List<Tween> ScaleStep()
        {
            {
                var list = new List<Tween>
                {
                    Anim.FadeTo(_batch, _face, 0.3f, 0.6f), Anim.FadeTo(_batch, _eyeL, 0.3f, 0.6f), Anim.FadeTo(_batch, _eyeR, 0.3f, 0.6f), Anim.FadeTo(_batch, _mouth, 0.3f, 0.6f),
                    Anim.FadeOut(_batch, _bubble, Vector3.up * 0.2f, 0.5f), Anim.FadeOut(_batch, _tail, Vector3.zero, 0.3f),
                    Anim.FadeText(_story, 0f, 0.4f), Anim.FadeText(_name, 0f, 0.4f),
                    Anim.Write(_tag, 0.8f).After(0.4f),
                    Anim.FadeIn(_batch, _dotA, Vector3.zero, 0.4f).After(0.7f), Anim.Write(_small, 0.6f).After(0.8f),
                    Anim.FadeIn(_batch, _dotB, Vector3.zero, 0.6f).After(3.4f), Anim.Write(_mid, 0.6f).After(3.5f),
                    Anim.FadeIn(_batch, _dotC, Vector3.zero, 0.9f).After(7.0f), Anim.Write(_big, 0.7f).After(7.2f),
                };
                list.AddRange(FadeLoop());
                return list;
            }
        }

        // "That's the machine inside the chatbot. Nothing magic. Just numbers, and a very good guess."
        private List<Tween> EndStep()
        {
            {
                var list = new List<Tween>
                {
                    Anim.FadeTo(_batch, _dotA, 0f, 0.6f), Anim.FadeTo(_batch, _dotB, 0f, 0.6f), Anim.FadeTo(_batch, _dotC, 0f, 0.6f),
                    Anim.FadeText(_small, 0f, 0.5f), Anim.FadeText(_mid, 0f, 0.5f), Anim.FadeText(_big, 0f, 0.5f), Anim.FadeText(_tag, 0f, 0.5f),
                    Anim.FadeTo(_batch, _face, 1f, 0.6f), Anim.FadeTo(_batch, _eyeL, 1f, 0.6f), Anim.FadeTo(_batch, _eyeR, 1f, 0.6f), Anim.FadeTo(_batch, _mouth, 1f, 0.6f),
                    Anim.FadeTo(_batch, _warnCard, 0f, 0.6f), Anim.FadeText(_careful, 0f, 0.5f), Anim.FadeText(_check, 0f, 0.5f),
                    Tween.Call(0.5f, () => _happy = true),
                    Anim.Write(_end, 2.4f).After(0.7f),
                };
                list.AddRange(FadeLoop());
                return list;
            }
        }
    }
}
