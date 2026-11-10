using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Hub;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;
using Convergence.Presentation.Ride.Explainers;
using Convergence.Presentation.Ride.Level2;

namespace Convergence.Presentation.Hub
{
    /// <summary>
    /// The level hub (ADR-024): a dark, starry plaza at the start of the game (Level 1's night sky, a neon grid at the rider's feet,
    /// mountains in silhouette) with one card per level of the <see cref="LevelCatalog"/>, in an arc around the rider. Point a laser at a
    /// card (it grows and ticks) and pull the trigger, or click it on the desk: the card glides to the rider while the others fall away,
    /// the view fades to black while the level loads behind it, and the level only switches in once the black is complete, so its own
    /// boot fade follows with no flash. Each card shows the level's stops, its length and the device's scoreboard (rides
    /// finished, best stars, high score); the level just ridden shows that ride's result. Built in code when the scene loads (as Level 2's
    /// places are), so adding a level to the catalog adds a card with no rebuild.
    /// </summary>
    public sealed class LevelHubView : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [Header("Night sky (Convergence/RideSky)")]
        [SerializeField] private Color skyTop = new Color(0.02f, 0.03f, 0.09f);
        [SerializeField] private Color skyHorizon = new Color(0.07f, 0.1f, 0.24f);
        [Tooltip("The glow along the horizon; alpha is its strength.")]
        [SerializeField] private Color skyGlow = new Color(0.36f, 0.3f, 0.85f, 0.6f);
        [Tooltip("The neon grid on the ground; alpha is its strength.")]
        [SerializeField] private Color gridColor = new Color(0.3f, 0.72f, 1f, 0.35f);
        [SerializeField] private float stars = 1.3f;
        [Tooltip("How far below the eye the grid lies (the seated eye is 1.3 m up, so the grid is the ground).")]
        [SerializeField] private float gridDepth = 1.3f;
        [SerializeField] private float gridSpacing = 1.5f;
        [SerializeField] private float fogDensity = 0.012f;
        [Tooltip("An unlit black material (transparent, drawn last), faded in front of the eyes before a level loads.")]
        [SerializeField] private Material coverMaterial;
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip chooseClip;
        [SerializeField] private AudioClip lockedClip;
        [SerializeField] private AudioSource music;
        [SerializeField] private float musicVolume = 0.12f;
        [Tooltip("The rider's eye in the hub's space; the cards face it.")]
        [SerializeField] private Vector3 eye = new Vector3(0f, 1.3f, 0f);
        [SerializeField] private float radius = 2.8f;
        [SerializeField] private float spacingDegrees = 31f;
        [SerializeField] private Vector2 cardSize = new Vector2(1.4f, 1.9f);
        [Tooltip("Seconds the chosen card glides toward the rider while the others fall away.")]
        [SerializeField] private float flySeconds = 0.6f;
        [Tooltip("Seconds into the glide the fade to black begins.")]
        [SerializeField] private float fadeLead = 0.25f;
        [SerializeField] private float fadeSeconds = 0.6f;
        [Tooltip("Seconds of black when the hub opens, so its first frames settle, before it fades in.")]
        [SerializeField] private float settleSeconds = 0.15f;
        [Tooltip("Seconds of the fade in when the hub opens (at boot, or back from a level).")]
        [SerializeField] private float arriveSeconds = 0.7f;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int SkyParams = Shader.PropertyToID("_RideSkyParams");
        private static readonly Color Ink = new Color(0.05f, 0.07f, 0.14f, 0.94f);
        private static readonly Color OffWhite = new Color(0.92f, 0.94f, 1f);
        private static readonly Color Muted = new Color(0.62f, 0.68f, 0.8f);

        private sealed class Card
        {
            public LevelCatalog.Entry Entry;
            public bool Playable;
            public Transform Root;
            public Transform Body;
            public ShapeBatch Plate;
            public TextMeshPro Stats;
            public TextMeshPro Button;
            public float Hover;
            public float Pop;
            public float Shake;
            public bool Aimed;
            public bool WasAimed;
            public Vector3 Home;
        }

        private readonly List<Card> _cards = new List<Card>();
        private readonly List<Transform> _floaters = new List<Transform>();
        private readonly List<Vector3> _floatHome = new List<Vector3>();
        private readonly List<Transform> _chrome = new List<Transform>(); // the title and the help line: they step aside on the way out
        private readonly RaycastHit[] _hits = new RaycastHit[8];
        private Renderer _cover;
        private MaterialPropertyBlock _block;
        private bool _loading;
        private float _age;
        private int _chosen = -1;
        private float _leave;

        /// <summary>The cards, in catalog order (tests and the bot read them).</summary>
        public int CardCount => _cards.Count;

        /// <summary>True once a level was chosen and is loading.</summary>
        public bool IsLoading => _loading;

        public LevelCard CardAt(int index) => index >= 0 && index < _cards.Count ? _cards[index].Root.GetComponent<LevelCard>() : null;

        private void Awake()
        {
            var look = new RideTheme.RegionLook { skyTop = skyTop, skyHorizon = skyHorizon, skyGlow = skyGlow, floor = gridColor, stars = stars, fog = skyHorizon };
            RegionAtmosphere.ApplySky(look, null);
            Shader.SetGlobalVector(SkyParams, new Vector4(stars, gridDepth, gridSpacing, 0f));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = skyHorizon;
            RenderSettings.fogDensity = fogDensity;

            BuildScenery();
            BuildTitle();
            BuildCards();
        }

        private void OnEnable() => LevelProgress.Changed += RefreshStats;

        private void OnDisable() => LevelProgress.Changed -= RefreshStats;

        private void Start() => StartCoroutine(Arrive());

        /// <summary>
        /// The way in: the hub opens behind black (the level the rider came from faded out), lets the first frames settle, then fades in
        /// with its music, so coming home is as clean as leaving.
        /// </summary>
        private IEnumerator Arrive()
        {
            SetCover(1f);
            bool hasMusic = music != null && music.clip != null;
            if (hasMusic)
            {
                music.loop = true;
                music.volume = 0f;
                music.Play();
            }

            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(settleSeconds);
            for (float t = 0f; t < arriveSeconds && !_loading; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
            {
                float k = Fx.Smooth(t / arriveSeconds);
                SetCover(1f - k);
                if (hasMusic) music.volume = musicVolume * k;
                yield return null;
            }

            if (_loading) yield break; // a card was chosen mid-fade: the way out owns the black now
            SetCover(0f);
            if (hasMusic) music.volume = musicVolume;
        }

        // ---------- building ----------

        private Vector3 EyeWorld => transform.TransformPoint(eye);

        private void BuildScenery()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            var m = new SolidMesh();

            // Mountains in silhouette all around, darker than the horizon glow behind them.
            for (int i = 0; i < 14; i++)
            {
                float a = (i / 14f) * Mathf.PI * 2f + 0.2f * Fx.Hash(i + 3);
                float r = 55f + 12f * Fx.Rand01(i + 11);
                Color ridge = Color.Lerp(new Color(0.03f, 0.04f, 0.1f, 0f), new Color(0.07f, 0.08f, 0.18f, 0f), Fx.Rand01(i + 21));
                m.Ball(new Vector3(Mathf.Sin(a) * r, -3f, Mathf.Cos(a) * r), new Vector3(16f + 10f * Fx.Rand01(i + 3), 10f + 9f * Fx.Rand01(i + 5), 14f), ridge, 8, 5, 0.5f);
            }

            // A dark round plaza under the rider, ringed in light, and a second ring of light on the ground under the cards.
            const int sides = 32;
            Color tileA = new Color(0.06f, 0.08f, 0.15f, 0f), tileB = new Color(0.09f, 0.11f, 0.2f, 0f);
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 p0 = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)) * 1.6f, p1 = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1)) * 1.6f;
                m.Quad(Vector3.up * 0.02f, p0 + Vector3.up * 0.02f, p1 + Vector3.up * 0.02f, Vector3.up * 0.02f, Vector3.up, i % 2 == 0 ? tileA : tileB);
            }

            Ring(m, 1.62f, 0.035f, new Color(0.3f, 0.85f, 1f, 1f));
            Ring(m, radius, 0.025f, new Color(0.62f, 0.45f, 1f, 1f));

            m.Build(transform, "Night", kit.Solid);

            // A few things from the levels drift in the sky behind the cards: digits (Level 1) and words (Level 2).
            string[] bits = { "3", "cat", "7", "the", "0", "machine", "5", "dog", "1", "story", "9", "learn" };
            for (int i = 0; i < bits.Length; i++)
            {
                // Off to both sides of the cards and the title (beyond 55 degrees), so they never cross the words that matter.
                float side = i % 2 == 0 ? -1f : 1f;
                float a = side * Mathf.Lerp(58f, 110f, (i / 2) / (bits.Length / 2 - 1f)) * Mathf.Deg2Rad;
                float r = 8f + 3f * Fx.Rand01(i + 90);
                Vector3 at = new Vector3(Mathf.Sin(a) * r, 3.2f + 2.6f * Fx.Rand01(i + 120), Mathf.Cos(a) * r);
                Color[] neon = { Level2Kit.Cyan, Level2Kit.Violet, Level2Kit.Pink, Level2Kit.Gold, Level2Kit.Mint };
                TextMeshPro t = kit.Text(transform, "Drift_" + bits[i], bits[i], 0.55f + 0.25f * Fx.Rand01(i + 7), neon[i % neon.Length], at, 4f);
                t.GetComponent<Renderer>().sortingOrder = Level2Scenery.SceneryOrder;
                Level2Scenery.FaceEye(t.transform, EyeWorld);
                _floaters.Add(t.transform);
                _floatHome.Add(t.transform.localPosition);
            }
        }

        /// <summary>A thin ring of light on the ground (vertex alpha makes the kit's solid glow).</summary>
        private static void Ring(SolidMesh m, float r, float width, Color color)
        {
            const int segments = 64;
            float length = 2f * Mathf.PI * r / segments * 1.04f;
            for (int i = 0; i < segments; i++)
            {
                float a = (i + 0.5f) * Mathf.PI * 2f / segments;
                m.Block(new Vector3(Mathf.Sin(a) * r, 0.03f, Mathf.Cos(a) * r), new Vector3(length, 0.02f, width), Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f), color); // its length along the ring
            }
        }

        private void BuildTitle()
        {
            Level2Kit kit = Level2Kit.Current;
            if (kit == null) return;
            var title = new GameObject("Title").transform;
            title.SetParent(transform, false);
            title.localPosition = new Vector3(0f, 2.95f, 3.4f);
            Level2Scenery.FaceEye(title, EyeWorld, false);
            _chrome.Add(title);
            kit.Text(title, "Name", "CONVERGENCE", 0.42f, Level2Kit.Gold, Vector3.zero, 6f);
            kit.Text(title, "Sub", "RIDE INSIDE AN AI  -  PICK A LEVEL", 0.12f, Color.white, new Vector3(0f, -0.34f, 0f), 6f);

            var help = new GameObject("Help").transform;
            help.SetParent(transform, false);
            help.localPosition = new Vector3(0f, 0.22f, 2.2f); // under the cards, never in front of them
            Level2Scenery.FaceEye(help, EyeWorld, false);
            _chrome.Add(help);
            var plate = new List<Shape> { Level2Kit.Square(new Vector3(0f, 0f, 0.01f), new Vector2(2.2f, 0.24f), new Color(0.05f, 0.07f, 0.14f, 0.85f), 0.5f) };
            kit.Batch(help, "Plate", plate, 1);
            kit.Text(help, "Text", "POINT AT A LEVEL AND PULL THE TRIGGER", 0.07f, Color.white, Vector3.zero, 2.1f);
        }

        private void BuildCards()
        {
            if (catalog == null) return;
            LevelCatalog.Entry[] levels = catalog.Levels;
            for (int i = 0; i < levels.Length; i++)
            {
                float angle = (i - (levels.Length - 1) * 0.5f) * spacingDegrees;
                _cards.Add(BuildCard(levels[i], i, angle));
            }

            RefreshStats();
        }

        private Card BuildCard(LevelCatalog.Entry entry, int index, float angle)
        {
            Level2Kit kit = Level2Kit.Current;
            var card = new Card { Entry = entry, Playable = LevelCatalog.IsPlayable(entry) };
            var root = new GameObject("Card_" + index).transform;
            root.SetParent(transform, false);
            float a = angle * Mathf.Deg2Rad;
            root.localPosition = new Vector3(Mathf.Sin(a) * radius, eye.y, Mathf.Cos(a) * radius);
            Level2Scenery.FaceEye(root, EyeWorld);
            card.Root = root;
            card.Home = root.localPosition;

            var collider = root.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(cardSize.x, cardSize.y, 0.1f);
            collider.isTrigger = true;
            var target = root.gameObject.AddComponent<LevelCard>();
            target.Hub = this;
            target.Index = index;

            card.Body = new GameObject("Body").transform;
            card.Body.SetParent(root, false);
            if (kit == null) return card;

            Color accent = card.Playable ? entry.accent : Muted;
            float w = cardSize.x, h = cardSize.y;
            float top = h * 0.5f;
            var shapes = new List<Shape>
            {
                Level2Kit.Square(new Vector3(0f, 0f, 0.03f), new Vector2(w + 0.16f, h + 0.16f), Fx.A(accent, 0f), 0.14f), // hover glow
                Level2Kit.Square(new Vector3(0f, 0f, 0.02f), new Vector2(w, h), Ink, 0.08f, Fx.A(accent, 0.9f), 0.02f),
                Level2Kit.Square(new Vector3(0f, top - 0.16f, 0.01f), new Vector2(w - 0.08f, 0.24f), Fx.A(accent, card.Playable ? 1f : 0.5f), 0.4f),
                Level2Kit.Square(new Vector3(0f, -top + 0.2f, 0.01f), new Vector2(0.9f, 0.22f), Fx.A(accent, card.Playable ? 1f : 0.35f), 1f),
            };
            card.Plate = kit.Batch(card.Body, "Plate", shapes, 1);

            kit.Text(card.Body, "Badge", entry.badge, 0.11f, Level2Kit.Ink, new Vector3(0f, top - 0.16f, 0f), w - 0.1f);
            TextMeshPro title = kit.Text(card.Body, "Title", entry.title, 0.125f, Color.white, new Vector3(0f, top - 0.52f, 0f), w - 0.1f);
            title.textWrappingMode = TextWrappingModes.Normal;
            title.rectTransform.sizeDelta = new Vector2(w - 0.12f, 0.4f);
            TextMeshPro tagline = kit.Text(card.Body, "Tagline", entry.tagline, 0.072f, OffWhite, new Vector3(0f, top - 0.92f, 0f), w - 0.12f, bold: false);
            tagline.textWrappingMode = TextWrappingModes.Normal;
            tagline.rectTransform.sizeDelta = new Vector2(w - 0.16f, 0.36f);
            tagline.alignment = TextAlignmentOptions.Top;

            if (entry.stops != null && entry.stops.Length > 0)
            {
                var stops = new System.Text.StringBuilder();
                for (int i = 0; i < entry.stops.Length; i++)
                {
                    if (i > 0) stops.Append(i == 3 ? "\n" : "   ");
                    stops.Append(i + 1).Append(' ').Append(entry.stops[i]);
                }

                TextMeshPro stopText = kit.Text(card.Body, "Stops", stops.ToString(), 0.06f, accent, new Vector3(0f, -0.2f, 0f), w - 0.1f);
                stopText.rectTransform.sizeDelta = new Vector2(w - 0.1f, 0.2f);
            }

            string length = !card.Playable ? "" : entry.minutes > 0f ? "ABOUT " + Mathf.RoundToInt(entry.minutes) + " MINUTES" : "";
            kit.Text(card.Body, "Length", length, 0.055f, Muted, new Vector3(0f, -0.36f, 0f), w - 0.1f, bold: false);
            // Two lines at most, between the length and the button: shrinks to fit rather than run into either.
            card.Stats = kit.Text(card.Body, "Stats", "", 0.055f, Level2Kit.Gold, new Vector3(0f, -0.525f, 0f), w - 0.14f);
            card.Stats.rectTransform.sizeDelta = new Vector2(w - 0.14f, 0.15f);
            card.Stats.enableAutoSizing = true;
            card.Stats.fontSizeMax = 0.55f;
            card.Stats.fontSizeMin = 0.3f;
            card.Button = kit.Text(card.Body, "Button", card.Playable ? "RIDE IT" : "COMING SOON", 0.1f, card.Playable ? Level2Kit.Ink : Muted, new Vector3(0f, -top + 0.2f, 0f), 0.9f);
            return card;
        }

        private void RefreshStats()
        {
            foreach (Card card in _cards)
            {
                if (card.Stats == null) continue;
                if (!card.Playable)
                {
                    card.Stats.text = "";
                    continue;
                }

                LevelProgress.Record r = LevelProgress.Get(card.Entry.scene);
                if (!r.Ridden)
                {
                    card.Stats.text = "NOT RIDDEN YET - BE THE FIRST!";
                    continue;
                }

                string stars = r.MaxStars > 0 ? r.BestStars + " / " + r.MaxStars + " STARS" : r.BestStars + " STARS";
                bool justRode = LevelProgressRecorder.LastLevel == card.Entry.scene;
                card.Stats.text = justRode
                    ? (LevelProgressRecorder.LastWasBest ? "NEW RECORD!  " : "YOUR RIDE:  ") + LevelProgressRecorder.LastStars + " STARS  " + LevelProgressRecorder.LastScore + " POINTS\nBEST " + stars + "  HIGH SCORE " + r.HighScore
                    : "BEST " + stars + "  HIGH SCORE " + r.HighScore + "\nRIDDEN " + r.Finished + (r.Finished == 1 ? " TIME" : " TIMES");
            }
        }

        // ---------- choosing ----------

        /// <summary>A laser shot (or a click) on a card. Returns true when it did something (the hand buzzes).</summary>
        public bool Choose(int index, int hand)
        {
            if (_loading || index < 0 || index >= _cards.Count) return false;
            Card card = _cards[index];
            if (!card.Playable)
            {
                card.Shake = 1f;
                Play(lockedClip != null ? lockedClip : hoverClip, 0.6f);
                return true;
            }

            card.Pop = 1f;
            Play(chooseClip, 1f);
            RideAim.Buzz(hand, 0.6f, 0.12f);
            StartCoroutine(Load(index));
            return true;
        }

        /// <summary>
        /// The way out: the level starts loading at once, behind the scenes, while the chosen card glides to the rider, the others fall
        /// away and the view fades to black with the music. The new scene switches in only once the black is complete and the level is
        /// loaded, and its own boot keeps the eyes covered until it is ready, so the rider sees one fade out and one fade in.
        /// </summary>
        private IEnumerator Load(int index)
        {
            _loading = true;
            _chosen = index;
            _leave = 0f;
            string scene = _cards[index].Entry.scene;
            AsyncOperation op = LevelHubNav.LoadLevel(scene);
            if (op == null)
            {
                // Not in the build after all: stay in the hub.
                Debug.LogWarning("[LevelHub] Scene '" + scene + "' is not in the build settings.");
                _loading = false;
                _chosen = -1;
                yield break;
            }

            op.allowSceneActivation = false;
            float total = fadeLead + fadeSeconds;
            while (_leave < total)
            {
                // Capped per frame: starting the load can cost one long frame, and the glide and fade must still play in full.
                _leave += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                float k = Fx.Smooth(Mathf.Clamp01((_leave - fadeLead) / fadeSeconds));
                SetCover(k);
                if (music != null) music.volume = musicVolume * (1f - k);
                yield return null;
            }

            SetCover(1f);
            while (op.progress < 0.9f) yield return null;
            yield return null; // one whole frame of black before the switch
            op.allowSceneActivation = true;
        }

        private void Play(AudioClip clip, float volume)
        {
            if (sfx != null && clip != null) sfx.PlayOneShot(clip, volume);
        }

        // ---------- every frame ----------

        private void Update()
        {
            _age += Time.deltaTime;
            foreach (Card c in _cards) c.Aimed = false;
            if (!_loading)
            {
                AimFrom(RideHand.Left);
                AimFrom(RideHand.Right);
            }

            for (int i = 0; i < _cards.Count; i++) Animate(_cards[i], i);
            if (_chosen >= 0)
            {
                // The title, the help line and the drifting bits shrink away first, so nothing draws over the card coming in.
                float gone = 1f - Fx.Smooth(Mathf.Clamp01(_leave / (flySeconds * 0.5f)));
                foreach (Transform t in _chrome) t.localScale = Vector3.one * gone;
                foreach (Transform t in _floaters) t.localScale = Vector3.one * gone;
            }
            for (int i = 0; i < _floaters.Count; i++)
            {
                _floaters[i].localPosition = _floatHome[i] + Vector3.up * (0.35f * Mathf.Sin(_age * 0.45f + i * 1.7f));
            }
        }

        private void AimFrom(int hand)
        {
            if (!RideAim.TryGet(hand, out Vector3 origin, out Vector3 point)) return;
            Vector3 d = point - origin;
            float length = d.magnitude;
            if (length < 0.01f) return;
            int count = Physics.RaycastNonAlloc(new Ray(origin, d / length), _hits, length + 0.1f, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                LevelCard target = _hits[i].collider.GetComponent<LevelCard>();
                if (target == null || target.Hub != this) continue;
                Card card = _cards[target.Index];
                if (!card.Aimed && !card.WasAimed)
                {
                    Play(hoverClip, 0.35f);
                    RideAim.Buzz(hand, 0.15f, 0.03f);
                }

                card.Aimed = true;
            }
        }

        private void Animate(Card c, int index)
        {
            float dt = Time.deltaTime;
            c.WasAimed = c.Aimed;
            c.Hover = Mathf.MoveTowards(c.Hover, c.Aimed ? 1f : 0f, dt * 6f);
            c.Pop = Mathf.MoveTowards(c.Pop, 0f, dt * 2f);
            c.Shake = Mathf.MoveTowards(c.Shake, 0f, dt * 2.5f);
            float bob = Mathf.Sin(_age * 0.9f + index * 1.3f) * 0.025f;
            float shake = Mathf.Sin(_age * 60f) * 0.04f * c.Shake;
            float pop = 0.1f * Mathf.Sin(Mathf.PI * c.Pop);
            float leave = _chosen < 0 ? 0f : Fx.Smooth(Mathf.Clamp01(_leave / flySeconds));
            if (_chosen == index)
            {
                // The chosen card glides toward the rider, straight ahead of the eye on its own bearing, and stays lit.
                Vector3 toward = c.Home - eye;
                toward.y = 0f;
                Vector3 near = eye + toward.normalized * 1.5f;
                c.Root.localPosition = Vector3.Lerp(c.Home, near, leave);
                c.Body.localPosition = new Vector3(0f, bob * (1f - leave), 0f);
                c.Body.localScale = Vector3.one * (1.06f + pop);
            }
            else
            {
                // The others sink and shrink away.
                c.Body.localPosition = new Vector3(shake, bob - 0.6f * leave, -0.12f * Fx.Smooth(c.Hover) + 0.8f * leave);
                c.Body.localScale = Vector3.one * ((1f + 0.06f * Fx.Smooth(c.Hover) + pop) * (1f - leave));
            }

            if (c.Plate != null && c.Plate.Count > 0)
            {
                Shape glow = c.Plate[0];
                float pulse = 0.5f + 0.5f * Mathf.Sin(_age * 5f);
                // A neon halo under a laser, full on the chosen card (none at rest: a dim fill reads as a grey box on the night).
                float alpha = c.Pop > 0f || _chosen == index ? 0.9f : 0.5f * Fx.Smooth(c.Hover) * (0.8f + 0.2f * pulse);
                if (Mathf.Abs(glow.color.a - alpha) > 0.004f)
                {
                    glow.color = Fx.A(glow.color, alpha);
                    c.Plate.MarkDirty();
                }
            }
        }

        // ---------- the black ----------

        private void SetCover(float alpha)
        {
            if (_cover == null)
            {
                Camera head = Camera.main;
                if (head == null || coverMaterial == null) return;
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "HubCover";
                Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(head.transform, false);
                quad.transform.localPosition = new Vector3(0f, 0f, 0.12f);
                quad.transform.localScale = new Vector3(2f, 2f, 1f);
                _cover = quad.GetComponent<Renderer>();
                _cover.sharedMaterial = coverMaterial;
                _cover.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _cover.receiveShadows = false;
                // Over everything: sorting order outranks the render queue, and the boards and words sort above 0.
                _cover.sortingOrder = short.MaxValue;
                _block = new MaterialPropertyBlock();
            }

            _cover.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, new Color(0f, 0f, 0f, Mathf.Clamp01(alpha)));
            _cover.SetPropertyBlock(_block);
            _cover.enabled = alpha > 0.001f;
        }
    }
}
