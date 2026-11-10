using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// The finale. Pixel itself, a big copy of the rider's buddy standing on the finish line's screen ({talk}); its padlock flies off the
    /// chat bubble ({unlock}) and it says its first sentence: the story the rider steered, typed in a speech bubble while its mouth moves.
    /// The whole machine once more, a loop of six steps lit one per clause ({recap}). Then the size of things ({scale}): Pixel with its
    /// 970 numbers, one GPT-2 chip with 124 million, and a wall of chips for the biggest chatbots' hundreds of billions (owner playtest
    /// 2026-11-10: pictures, not blobs; the chip is our own emblem, not a company logo). The warning that likely is not true
    /// ({careful}); and the last words, Pixel back and happy ({end}).
    /// </summary>
    public sealed class OutroExplainer : Level2Explainer
    {
        [SerializeField] private Level2Session session;
        [Tooltip("The big copy of Pixel on the finish screen (built by NeuralRideBuilder.Level2), its face, and its chat padlock.")]
        [SerializeField] private Transform portrait;
        [SerializeField] private ShapeBatch portraitFace;
        [SerializeField] private Transform portraitLock;
        [SerializeField] private Renderer portraitBubble;
        [SerializeField] private Material bubbleOpen;

        private Level2Kit _kit;
        private ShapeBatch _batch;
        private GlowField _glows;
        private static readonly string[] Loop = { "TOKENS", "MEANING", "ATTENTION", "ODDS", "ROLL", "AGAIN" };

        private TextMeshPro _story, _name, _small, _mid, _big, _end, _tag, _careful, _check, _chipLabel;
        private TextMeshPro[] _loopText;
        private int _loopFirst, _loopCount, _warnCard, _screen, _bubble, _tail;
        private int _chipFirst, _chipCount, _wallFirst, _wallCount;
        private bool _talking, _happy;
        private Vector3 _portraitHome, _portraitScale, _lockHome;
        private Quaternion _lockRot;
        private Material _bubbleLocked;
        private float _unlock = -1f;
        private static readonly Vector3 PixelAt = new Vector3(-3.4f, 2.3f, 7.6f);
        private static readonly Vector3 SmallPixelAt = new Vector3(-4.6f, 2.55f, 7.9f);
        private static readonly Vector3 ChipAt = new Vector3(-0.6f, 2.7f, 8.1f);
        private static readonly Vector3 WallAt = new Vector3(3.9f, 3.25f, 8.2f);

        protected override void Awake()
        {
            if (portrait != null)
            {
                _portraitHome = portrait.localPosition;
                _portraitScale = portrait.localScale;
            }

            if (portraitLock != null)
            {
                _lockHome = portraitLock.localPosition;
                _lockRot = portraitLock.localRotation;
            }

            if (portraitBubble != null) _bubbleLocked = portraitBubble.sharedMaterial;
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
            if (portrait != null) portrait.gameObject.SetActive(false);
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
            _bubble = shapes.Count;
            shapes.Add(Level2Kit.Square(new Vector3(2.3f, 2.9f, 8f), new Vector2(6.6f, 2.2f), new Color(0.06f, 0.07f, 0.2f, 0.94f), 0.2f, new Color(1f, 0.9f, 0.55f, 0.95f), 0.05f));
            _tail = shapes.Count;
            shapes.Add(Level2Kit.Line(new Vector3(-1.1f, 2.7f, 8f), new Vector3(-0.8f, 2.55f, 8f), 0.3f, new Color(1f, 0.9f, 0.55f, 0.95f)));

            // One GPT-2: a chip with pins on every side.
            Color violet = new Color(0.62f, 0.5f, 1f);
            _chipFirst = shapes.Count;
            shapes.Add(Level2Kit.Square(ChipAt, new Vector2(1.7f, 1.7f), new Color(0.1f, 0.07f, 0.25f, 0.98f), 0.12f, violet, 0.07f));
            shapes.Add(Level2Kit.Square(ChipAt + new Vector3(0f, 0f, -0.01f), new Vector2(1.15f, 1.15f), Fx.A(violet, 0.25f), 0.1f, Fx.A(violet, 0.9f), 0.03f));
            for (int side = 0; side < 4; side++)
            {
                for (int k = 0; k < 5; k++)
                {
                    float u = -0.6f + k * 0.3f;
                    Vector3 dir = side == 0 ? Vector3.up : side == 1 ? Vector3.down : side == 2 ? Vector3.left : Vector3.right;
                    Vector3 along = side < 2 ? new Vector3(u, 0f, 0f) : new Vector3(0f, u, 0f);
                    Vector3 a = ChipAt + along + dir * 0.86f;
                    shapes.Add(Level2Kit.Line(a, a + dir * 0.22f, 0.06f, Fx.A(violet, 0.95f)));
                }
            }

            _chipCount = shapes.Count - _chipFirst;

            // The biggest chatbots: a wall of chips, many GPT-2s over.
            Color pink = new Color(1f, 0.5f, 0.75f);
            _wallFirst = shapes.Count;
            shapes.Add(Level2Kit.Square(WallAt, new Vector2(3.1f, 3.1f), Fx.A(pink, 0.1f), 0.08f, Fx.A(pink, 0.9f), 0.05f));
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    Vector3 at = WallAt + new Vector3(-1.2f + x * 0.6f, -1.2f + y * 0.6f, -0.01f);
                    shapes.Add(Level2Kit.Square(at, new Vector2(0.44f, 0.44f), new Color(0.2f, 0.07f, 0.17f, 0.98f), 0.12f, Fx.A(pink, 0.95f), 0.03f));
                }
            }

            _wallCount = shapes.Count - _wallFirst;

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

            _name = _kit.Text(root, "Name", "PIXEL", 0.36f, cyan, PixelAt + new Vector3(0f, 1.75f, 0.3f), 3f, TextAlignmentOptions.Center, false, true);
            _name.characterSpacing = 8f;
            string story = session != null && !string.IsNullOrEmpty(session.Story) ? session.Story : "My robot loves to talk to me.";
            _story = _kit.Text(root, "Story", "", 0.46f, Color.white, new Vector3(2.3f, 2.9f, 7.95f), 6.0f);
            _story.textWrappingMode = TextWrappingModes.Normal;
            _story.rectTransform.sizeDelta = new Vector2(6.0f, 2.0f);
            _story.text = story;
            _small = _kit.Text(root, "Small", "PIXEL\n970 NUMBERS", 0.28f, new Color(0.5f, 1f, 0.72f), SmallPixelAt + new Vector3(0f, -1.45f, 0.05f), 2.6f);
            _mid = _kit.Text(root, "Mid", "GPT-2\n124 MILLION", 0.28f, new Color(0.75f, 0.65f, 1f), ChipAt + new Vector3(0f, -1.6f, -0.15f), 3.2f);
            _chipLabel = _kit.Text(root, "ChipLabel", "GPT-2", 0.34f, Color.white, ChipAt + new Vector3(0f, 0f, -0.08f), 1.6f);
            _big = _kit.Text(root, "Big", "BIGGEST CHATBOTS\nHUNDREDS OF BILLIONS", 0.26f, new Color(1f, 0.6f, 0.82f), WallAt + new Vector3(0f, -2.05f, -0.25f), 4.4f);
            _tag = _kit.Text(root, "Tag", "NUMBERS IN THE BRAIN", 0.3f, new Color(0.9f, 0.92f, 1f), new Vector3(0f, 4.95f, 7.95f), 8f);
            _end = _kit.Text(root, "End", "NOTHING MAGIC.\nJUST NUMBERS, AND A VERY GOOD GUESS.", 0.36f, new Color(1f, 0.9f, 0.6f), new Vector3(2.0f, 3.0f, 7.95f), 8.4f);
            _end.rectTransform.sizeDelta = new Vector2(8.4f, 2f);
            _glows = GlowField.Create(root, "Sparks", 40, _kit.Glow, 4);
        }

        private static Vector3 LoopAt(int i) => new Vector3(-4.75f + i * 1.9f, 3.75f, 7.9f);

        private IEnumerable<TextMeshPro> Texts()
        {
            foreach (TextMeshPro t in new[] { _story, _name, _small, _mid, _big, _end, _tag, _careful, _check, _chipLabel }) yield return t;
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
            _unlock = -1f;
            _glows.HideAll();
            if (portrait != null)
            {
                portrait.gameObject.SetActive(false);
                portrait.localPosition = _portraitHome;
                portrait.localScale = Vector3.zero;
            }

            if (portraitLock != null)
            {
                portraitLock.gameObject.SetActive(true);
                portraitLock.localPosition = _lockHome;
                portraitLock.localRotation = _lockRot;
                portraitLock.localScale = Vector3.one;
            }

            if (portraitBubble != null && _bubbleLocked != null) portraitBubble.sharedMaterial = _bubbleLocked;
        }

        protected override void Animate(float deltaTime)
        {
            // Pixel: it blinks, and its mouth moves while it talks.
            if (portrait != null && portrait.gameObject.activeSelf)
            {
                float blinkPhase = Mathf.Repeat(Time.time, 3.2f);
                float blink = blinkPhase < 0.12f ? 0.1f : 1f;
                float talk = _talking ? Mathf.Abs(Mathf.Sin(Time.time * 11f)) : 0f;
                PixelBuddyView.PaintFace(portraitFace, _happy ? PixelBuddyView.Mood.Celebrate : PixelBuddyView.Mood.Happy, talk, blink, new Color(0.1f, 0.88f, 1f));
                portrait.localRotation = Quaternion.Euler(0f, 180f + 6f * Mathf.Sin(Time.time * 0.7f), 2f * Mathf.Sin(Time.time * 1.3f));
            }

            if (_unlock >= 0f && portraitLock != null && portraitLock.gameObject.activeSelf)
            {
                _unlock += deltaTime;
                float t = Mathf.Clamp01(_unlock / 1.2f);
                portraitLock.localPosition = _lockHome + new Vector3(0.05f * t, 0.2f * Mathf.Sin(t * Mathf.PI * 0.6f), 0f);
                portraitLock.localRotation = _lockRot * Quaternion.Euler(0f, 0f, -220f * t);
                portraitLock.localScale = Vector3.one * (1f - t * t);
                if (t >= 1f) portraitLock.gameObject.SetActive(false);
            }

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

        /// <summary>Moves the big Pixel to a place and size on the screen (scale 0 hides it).</summary>
        private Tween PixelTo(Vector3 at, float scale, float duration)
        {
            Vector3 fromPos = Vector3.zero, fromScale = Vector3.zero;
            return new Tween(duration, a =>
            {
                if (portrait == null) return;
                portrait.localPosition = Vector3.Lerp(fromPos, at, a);
                portrait.localScale = Vector3.Lerp(fromScale, _portraitScale * scale, a);
                if (a >= 1f && scale <= 0f) portrait.gameObject.SetActive(false);
            }, Rate.Smooth, begin: () =>
            {
                if (portrait == null) return;
                if (scale > 0f && !portrait.gameObject.activeSelf)
                {
                    portrait.gameObject.SetActive(true);
                    portrait.localPosition = at;
                    portrait.localScale = Vector3.zero;
                }

                fromPos = portrait.localPosition;
                fromScale = portrait.localScale;
            });
        }

        private IEnumerable<Tween> Group(int first, int count, float alpha, float delay, float duration)
        {
            for (int i = 0; i < count; i++)
            {
                yield return alpha > 0f
                    ? Anim.FadeIn(_batch, first + i, Vector3.zero, duration).After(delay + 0.015f * i)
                    : Anim.FadeTo(_batch, first + i, 0f, duration).After(delay);
            }
        }

        protected override void Declare(Steps s)
        {
            // "And that's how Pixel learns to talk." Pixel itself, big, on the finish screen; its chat bubble still padlocked.
            s.At("talk", () => new List<Tween>
            {
                Anim.FadeIn(_batch, _screen, Vector3.zero, 0.6f),
                PixelTo(_portraitHome, 1f, 0.9f).After(0.2f),
                Anim.Write(_name, 0.6f).After(0.5f),
            });

            // "Chat: unlocked!" The padlock flies off, the bubble lights, and Pixel says the rider's own story.
            s.At("unlock", () =>
            {
                _story.text = session != null && !string.IsNullOrEmpty(session.Story) ? session.Story : "My robot loves to talk to me.";
                return new List<Tween>
                {
                    Tween.Call(0f, () =>
                    {
                        _unlock = 0f;
                        if (portraitBubble != null && bubbleOpen != null) portraitBubble.sharedMaterial = bubbleOpen;
                        RideSounds.Play("winShimmer", 0.8f);
                    }),
                    Anim.FadeIn(_batch, _bubble, Vector3.left * 0.4f, 0.5f).After(0.8f),
                    Anim.FadeIn(_batch, _tail, Vector3.zero, 0.4f).After(0.8f),
                    Tween.Call(1.3f, () => _talking = true),
                    Anim.Write(_story, 3.0f).After(1.3f),
                    Tween.Call(4.4f, () => _talking = false),
                };
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

        private List<Tween> RecapStep()
        {
            var list = new List<Tween>
            {
                PixelTo(_portraitHome + new Vector3(0f, -0.5f, 0f), 0f, 0.6f),
                Anim.FadeOut(_batch, _bubble, Vector3.up * 0.2f, 0.5f), Anim.FadeOut(_batch, _tail, Vector3.zero, 0.3f),
                Anim.FadeText(_story, 0f, 0.4f), Anim.FadeText(_name, 0f, 0.4f),
                Tween.Call(0f, () => _talking = false),
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

        // "Pixel's number brain had nine hundred and seventy numbers. GPT-2 has one hundred and twenty-four million. Today's biggest..."
        private List<Tween> ScaleStep()
        {
            var list = new List<Tween>
            {
                Anim.FadeOut(_batch, _bubble, Vector3.up * 0.2f, 0.5f), Anim.FadeOut(_batch, _tail, Vector3.zero, 0.3f),
                Anim.FadeText(_story, 0f, 0.4f), Anim.FadeText(_name, 0f, 0.4f),
                Anim.Write(_tag, 0.8f).After(0.4f),
                PixelTo(SmallPixelAt, 0.32f, 0.8f).After(0.6f), Anim.Write(_small, 0.6f).After(0.9f),
                Anim.Write(_chipLabel, 0.6f).After(3.6f), Anim.Write(_mid, 0.6f).After(3.8f),
                Anim.Write(_big, 0.7f).After(7.6f),
            };
            list.AddRange(Group(_chipFirst, _chipCount, 1f, 3.3f, 0.4f));
            list.AddRange(Group(_wallFirst, _wallCount, 1f, 6.8f, 0.35f));
            list.AddRange(FadeLoop());
            return list;
        }

        // "One last thing. The machine picks words that sound likely, not words that are true... Always check."
        private List<Tween> CarefulStep()
        {
            var list = new List<Tween>
            {
                PixelTo(SmallPixelAt, 0f, 0.5f),
                Anim.FadeText(_small, 0f, 0.5f), Anim.FadeText(_mid, 0f, 0.5f), Anim.FadeText(_big, 0f, 0.5f), Anim.FadeText(_tag, 0f, 0.5f), Anim.FadeText(_chipLabel, 0f, 0.5f),
                Anim.FadeIn(_batch, _warnCard, Vector3.zero, 0.6f).After(0.4f),
                Anim.Write(_careful, 1.4f).After(0.8f),
                Anim.Write(_check, 0.8f).After(4.2f),
            };
            list.AddRange(Group(_chipFirst, _chipCount, 0f, 0f, 0.5f));
            list.AddRange(Group(_wallFirst, _wallCount, 0f, 0f, 0.5f));
            list.AddRange(FadeLoop());
            return list;
        }

        // "That's the machine inside the chatbot. Nothing magic. Just numbers, and a very good guess."
        private List<Tween> EndStep()
        {
            var list = new List<Tween>
            {
                Anim.FadeText(_small, 0f, 0.5f), Anim.FadeText(_mid, 0f, 0.5f), Anim.FadeText(_big, 0f, 0.5f), Anim.FadeText(_tag, 0f, 0.5f), Anim.FadeText(_chipLabel, 0f, 0.5f),
                Anim.FadeTo(_batch, _warnCard, 0f, 0.6f), Anim.FadeText(_careful, 0f, 0.5f), Anim.FadeText(_check, 0f, 0.5f),
                PixelTo(_portraitHome + new Vector3(-0.6f, 0f, 0f), 0.85f, 0.9f).After(0.3f),
                Tween.Call(0.5f, () => _happy = true),
                Anim.Write(_end, 2.4f).After(0.7f),
            };
            list.AddRange(Group(_chipFirst, _chipCount, 0f, 0f, 0.5f));
            list.AddRange(Group(_wallFirst, _wallCount, 0f, 0f, 0.5f));
            list.AddRange(FadeLoop());
            return list;
        }
    }
}
