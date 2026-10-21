using System;
using System.Collections.Generic;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;
using Convergence.Presentation.Ride.Explainers;
using UnityEngine;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Pixel, the little robot the rider teaches to read (TASK_LEVEL1_V4_EXPLAINERS, section 2). Pixel floats beside the
    /// pod, looks at the rider or at what the narrator is talking about, shows how it feels on its face screen (happy,
    /// curious, confused, sad, celebrate, thinking), beeps (no voice: there is one narrator), and points at the cell a
    /// hint wants. It meets the rider at the dock's anchor on {pixel} and rides along from GO. Display only: every
    /// reaction follows a narration clause or a moment event; nothing here decides anything.
    /// </summary>
    public sealed class PixelBuddyView : MonoBehaviour, IPettable
    {
        public enum Mood
        {
            Happy,
            Curious,
            Confused,
            Sad,
            Celebrate,
            Thinking
        }

        /// <summary>How Pixel reacts when a clause of a line starts (clause -1: the line's first clause).</summary>
        [Serializable]
        public struct LineMood
        {
            public RideLine line;
            public int clause;
            public Mood mood;
            [Tooltip("Pixel turns to the rider (the narrator is talking about Pixel, or to the rider).")]
            public bool atRider;
        }

        [SerializeField] private RideDirector director;
        [SerializeField] private DashScreenView dash;
        [SerializeField] private RideTheme theme;
        [SerializeField] private DockExplainer dock;
        [SerializeField] private Transform body;
        [SerializeField] private ShapeBatch face;
        [SerializeField] private Renderer glow;
        [SerializeField] private LineRenderer pointer;
        [SerializeField] private AudioSource beeper;
        [SerializeField] private Vector3 home = new Vector3(0.98f, 0.95f, 1.02f);
        [SerializeField] private LineMood[] lineMoods = new LineMood[0];
        [Tooltip("Pixel's size at the dock, where it floats farther away and is introduced.")]
        [SerializeField] private float dockScale = 1.5f;
        [Tooltip("Pixel's size riding beside the pod.")]
        [SerializeField] private float rideScale = 0.85f;
        [Tooltip("How long Pixel looks at the board when a new clause starts, before it turns back to the rider.")]
        [SerializeField] private float glanceSeconds = 1.6f;

        [Header("Moments")]
        [SerializeField] private WeightPaintMoment paint;
        [SerializeField] private Transform hintRing;
        [SerializeField] private BlameMoment blame;
        [SerializeField] private ValleyRunMoment valley;

        // Face slots in the batch: two eyes of two strokes each, a mouth of four strokes, an open mouth, two cheeks.
        private const int EyeL = 0, EyeR = 2, Mouth = 4, MouthStrokes = 4, Open = 8, CheekL = 9, CheekR = 10, Slots = 11;

        private struct Face
        {
            public float chevron;  // 0 = tall pill eyes, 1 = happy ^ ^ eyes
            public float droop;    // sad eyes slant down at the outside
            public float eyeH;     // pill height
            public float rightEye; // right eye scale (confused squints one eye)
            public float smile;    // + smile, - frown
            public float mouthW;
            public float wave;     // a wobbly mouth (confused)
            public float open;     // the open "yay" mouth
            public float blush;
            public Vector2 gaze;

            public static Face Lerp(Face a, Face b, float t)
            {
                return new Face
                {
                    chevron = Mathf.Lerp(a.chevron, b.chevron, t),
                    droop = Mathf.Lerp(a.droop, b.droop, t),
                    eyeH = Mathf.Lerp(a.eyeH, b.eyeH, t),
                    rightEye = Mathf.Lerp(a.rightEye, b.rightEye, t),
                    smile = Mathf.Lerp(a.smile, b.smile, t),
                    mouthW = Mathf.Lerp(a.mouthW, b.mouthW, t),
                    wave = Mathf.Lerp(a.wave, b.wave, t),
                    open = Mathf.Lerp(a.open, b.open, t),
                    blush = Mathf.Lerp(a.blush, b.blush, t),
                    gaze = Vector2.Lerp(a.gaze, b.gaze, t),
                };
            }
        }

        private static Face FaceFor(Mood mood)
        {
            switch (mood)
            {
                case Mood.Curious: return new Face { eyeH = 0.062f, rightEye = 1.12f, smile = 0.25f, mouthW = 0.05f, gaze = new Vector2(0f, 0.004f) };
                case Mood.Confused: return new Face { eyeH = 0.05f, rightEye = 0.55f, smile = 0f, mouthW = 0.07f, wave = 1f };
                case Mood.Sad: return new Face { eyeH = 0.04f, droop = 1f, rightEye = 1f, smile = -0.8f, mouthW = 0.06f, gaze = new Vector2(0f, -0.008f) };
                case Mood.Celebrate: return new Face { chevron = 1f, eyeH = 0.05f, rightEye = 1f, smile = 1f, mouthW = 0.08f, open = 1f, blush = 1f };
                case Mood.Thinking: return new Face { eyeH = 0.045f, rightEye = 1f, smile = 0.1f, mouthW = 0.035f, gaze = new Vector2(0.012f, 0.012f) };
                default: return new Face { chevron = 1f, eyeH = 0.05f, rightEye = 1f, smile = 1f, mouthW = 0.075f, blush = 0.6f };
            }
        }

        private Mood _mood = Mood.Happy;
        private Face _faceFrom, _face;
        private float _faceT = 1f;
        private float _clock, _blinkAt, _moodSince, _glanceUntil = -1f;
        private bool _met, _atRider, _riding;
        private float _hop, _spin, _shake, _droop, _grow, _size = 1.5f;
        private Vector3 _vel;
        private Transform _rider;
        private Vector3 _pointAt;
        private bool _pointing;
        private float _pointerAlpha;
        private AudioClip _chirp, _blip, _sad, _huh, _yay, _hmm, _pettedCoo;
        private float _pet, _lastPetCoo = -10f;
        private MaterialPropertyBlock _block;

        public Mood CurrentMood => _mood;
        public bool IsShowing => body != null && body.gameObject.activeSelf;
        public bool IsPointing => _pointing;

        /// <summary>Plays a mood now (also used by tests and the capture tool).</summary>
        public void React(Mood mood, bool beep = true)
        {
            if (mood == _mood && _clock - _moodSince < 0.6f) return;
            _faceFrom = _face;
            _faceT = 0f;
            _mood = mood;
            _moodSince = _clock;
            switch (mood)
            {
                case Mood.Celebrate:
                    _hop = 1f;
                    _spin = 1f;
                    break;
                case Mood.Happy:
                    _hop = Mathf.Max(_hop, 0.5f);
                    break;
                case Mood.Confused:
                    _shake = 1f;
                    break;
            }

            if (beep) Beep(ClipFor(mood));
        }

        /// <summary>
        /// The rider pets Pixel: it leans in, blushes, wiggles and purrs. Cosmetic only; ignored until Pixel has
        /// been met, and a quick tap-tap-tap does not stack more than one coo every 0.35 seconds.
        /// </summary>
        public void Pet()
        {
            if (!_met || body == null || !body.gameObject.activeSelf) return;
            if (_mood != Mood.Happy && _mood != Mood.Celebrate) React(Mood.Happy, false);
            _atRider = true;
            _glanceUntil = -1f;
            _pet = 1f;
            _hop = Mathf.Max(_hop, 0.45f);
            _shake = Mathf.Max(_shake, 0.35f);
            if (_clock - _lastPetCoo < 0.35f) return;
            _lastPetCoo = _clock;
            Beep(_pettedCoo);
        }

        private AudioClip ClipFor(Mood mood)
        {
            switch (mood)
            {
                case Mood.Celebrate: return _yay;
                case Mood.Sad: return _sad;
                case Mood.Confused: return _huh;
                case Mood.Thinking: return _hmm;
                case Mood.Curious: return _blip;
                default: return _chirp;
            }
        }

        private void Beep(AudioClip clip)
        {
            if (beeper == null || clip == null || !beeper.isActiveAndEnabled) return;
            beeper.pitch = UnityEngine.Random.Range(0.96f, 1.05f);
            beeper.PlayOneShot(clip);
        }

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _face = _faceFrom = FaceFor(Mood.Happy);
            _chirp = Tones("PixelChirp", new[] { 880f, 1320f }, 0.07f);
            _blip = Tones("PixelBlip", new[] { 1175f }, 0.05f);
            _sad = Tones("PixelSad", new[] { 660f, 520f, 390f }, 0.13f);
            _huh = Tones("PixelHuh", new[] { 700f, 560f, 820f }, 0.08f);
            _yay = Tones("PixelYay", new[] { 784f, 988f, 1175f, 1568f }, 0.075f);
            _hmm = Tones("PixelHmm", new[] { 520f, 540f }, 0.12f);
            _pettedCoo = Tones("PixelCoo", new[] { 988f, 1109f, 1319f, 1109f }, 0.06f);
            if (pointer != null) pointer.enabled = false;

            // Something to click or touch: a small sphere on the body (the body scales, so this scales with it).
            if (body != null && body.GetComponentInChildren<Collider>() == null)
            {
                var hit = body.gameObject.AddComponent<SphereCollider>();
                hit.radius = 0.2f;
                hit.isTrigger = false;
            }
        }

        private void OnEnable()
        {
            if (dash != null) dash.SegmentShown += OnClause;
            if (director != null)
            {
                director.SegmentStarted += OnSegmentStarted;
                director.NarrationSkipped += OnSkipped;
            }

            if (paint != null)
            {
                paint.Changed += OnPainted;
                paint.HintChanged += OnHint;
                paint.Solved += OnSolved;
            }

            if (blame != null) blame.Nudged += OnNudged;
            if (valley != null) valley.HopReached += OnHop;
        }

        private void OnDisable()
        {
            if (dash != null) dash.SegmentShown -= OnClause;
            if (director != null)
            {
                director.SegmentStarted -= OnSegmentStarted;
                director.NarrationSkipped -= OnSkipped;
            }

            if (paint != null)
            {
                paint.Changed -= OnPainted;
                paint.HintChanged -= OnHint;
                paint.Solved -= OnSolved;
            }

            if (blame != null) blame.Nudged -= OnNudged;
            if (valley != null) valley.HopReached -= OnHop;
        }

        private void Start()
        {
            Camera head = Camera.main;
            _rider = head != null ? head.transform : null;
            body.gameObject.SetActive(false);
            body.localPosition = home;
            DrawFace(0f);
        }

        // ---------- what Pixel hears ----------

        private void OnClause(RideLine line, int clause, NarrationLibrary.Segment segment)
        {
            // A new idea on the board: Pixel glances at it, then back at the rider (a buddy watching with you, face on).
            _glanceUntil = _clock + glanceSeconds;
            if (NarrationLibrary.HasCue(segment, "pixel")) Meet();
            if (NarrationLibrary.HasCue(segment, "go")) _riding = true;
            for (int i = 0; i < lineMoods.Length; i++)
            {
                LineMood m = lineMoods[i];
                if (m.line != line) continue;
                if (m.clause == clause || (m.clause < 0 && clause == 0))
                {
                    if (!_met) Meet();
                    _atRider = m.atRider;
                    React(m.mood);
                    return;
                }
            }
        }

        private void OnSegmentStarted(int index)
        {
            _riding = true;
            if (!_met) Meet();
            _atRider = false;
            StopPointing();
        }

        private void OnSkipped()
        {
            if (!_met) Meet();
        }

        private void OnPainted(Convergence.Core.Puzzles.WeightPaintResult result)
        {
            // Every tap gets an answer at once: a blip and a little nod.
            Beep(_blip);
            _hop = Mathf.Max(_hop, 0.35f);
        }

        private void OnHint(int cell, int value)
        {
            if (cell < 0 || hintRing == null)
            {
                StopPointing();
                return;
            }

            _pointing = true;
            if (_mood != Mood.Curious) React(Mood.Curious);
        }

        private void OnSolved(bool byRider) => React(Mood.Celebrate);

        private void OnNudged(int wire, double before, double after)
        {
            Beep(_blip);
            _hop = Mathf.Max(_hop, 0.5f);
        }

        private void OnHop(int hop) => _hop = Mathf.Max(_hop, 0.3f);

        private void StopPointing() => _pointing = false;

        /// <summary>Pixel pops into view (the dock's {pixel}, or at once when the ride starts without it).</summary>
        public void Meet()
        {
            if (_met) return;
            _met = true;
            body.gameObject.SetActive(true);
            body.localPosition = Target();
            body.localScale = Vector3.zero;
            _grow = 0f;
            _hop = 1f;
            _spin = 1f;
            Beep(_chirp);
        }

        // ---------- every frame ----------

        private Vector3 Target()
        {
            // At the dock Pixel floats at the explainer's anchor; from GO it rides beside the pod.
            if (!_riding && dock != null && dock.PixelAnchor != null)
            {
                return transform.InverseTransformPoint(dock.PixelAnchor.position);
            }

            return home;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _clock += dt;
            if (!_met) return;

            // Grow in.
            _grow = Mathf.MoveTowards(_grow, 1f, dt * 1.8f);
            // Bigger when it meets the rider at the dock (farther away, and the star of that moment); normal size riding.
            _size = Mathf.MoveTowards(_size, _riding ? rideScale : dockScale, dt * 0.6f);
            body.localScale = Vector3.one * (Rate.Smooth(_grow) * _size * (1f + 0.12f * Mathf.Sin(_hop * Mathf.PI)));

            // Float: follow the target softly, bob, and hop.
            Vector3 target = Target();
            if (_pointing && hintRing != null && hintRing.gameObject.activeInHierarchy)
            {
                _pointAt = hintRing.position;
                Vector3 local = transform.InverseTransformPoint(_pointAt);
                target = Vector3.Lerp(target, new Vector3(local.x + 0.35f, Mathf.Max(local.y + 0.45f, 1.05f), Mathf.Max(local.z + 0.55f, 1.2f)), 0.55f);
            }

            _hop = Mathf.MoveTowards(_hop, 0f, dt * 1.6f);
            _shake = Mathf.MoveTowards(_shake, 0f, dt * 1.4f);
            _droop = Mathf.MoveTowards(_droop, _mood == Mood.Sad ? 1f : 0f, dt * 1.5f);
            float bob = Mathf.Sin(_clock * 1.7f) * 0.025f + Mathf.Sin(_hop * Mathf.PI) * 0.12f - _droop * 0.06f;
            Vector3 goal = target + Vector3.up * bob;
            body.localPosition = Vector3.SmoothDamp(body.localPosition, goal, ref _vel, 0.35f, 4f, dt);

            // Look: at the hint, at the rider, or ahead with the rider.
            Vector3 lookWorld;
            if (_pointing && hintRing != null && hintRing.gameObject.activeInHierarchy) lookWorld = _pointAt;
            else if (_clock < _glanceUntil && !_atRider) lookWorld = transform.TransformPoint(new Vector3(0f, 1.6f, 6f));
            else if (_rider != null) lookWorld = _rider.position;
            else lookWorld = transform.TransformPoint(new Vector3(0f, 1.6f, 6f));
            if (_rider != null && _mood == Mood.Celebrate) lookWorld = _rider.position;
            Vector3 dir = lookWorld - body.position;
            if (dir.sqrMagnitude > 1e-4f)
            {
                Quaternion look = Quaternion.LookRotation(dir, transform.up);
                float roll = _mood == Mood.Confused ? 14f : _mood == Mood.Thinking ? -8f : 0f;
                roll += Mathf.Sin(_clock * 18f) * 10f * _shake;
                float pitch = _mood == Mood.Curious ? 8f : _mood == Mood.Sad ? 12f : 0f;
                float yawSpin = Rate.Smooth(1f - _spin) * 360f;
                look = look * Quaternion.Euler(pitch, _spin > 0f ? yawSpin : 0f, roll);
                body.rotation = Quaternion.Slerp(body.rotation, look, 1f - Mathf.Exp(-dt * 6f));
            }

            _spin = Mathf.MoveTowards(_spin, 0f, dt * 1.25f);

            // A celebration settles back to happy.
            if (_mood == Mood.Celebrate && _clock - _moodSince > 2.6f) React(Mood.Happy, false);

            // Face: ease to the mood, blink now and then.
            _faceT = Mathf.MoveTowards(_faceT, 1f, dt / 0.35f);
            _face = Face.Lerp(_faceFrom, FaceFor(_mood), Rate.Smooth(_faceT));
            _pet = Mathf.MoveTowards(_pet, 0f, dt * 0.45f);
            if (_pet > 0f)
            {
                // Petted: happy ^ ^ eyes, a big smile and full blush that fade out over a couple of seconds.
                float p = Mathf.Min(1f, _pet * 2f);
                _face.chevron = Mathf.Max(_face.chevron, p);
                _face.smile = Mathf.Max(_face.smile, p);
                _face.blush = Mathf.Max(_face.blush, p);
            }

            if (_clock - _blinkAt > 0.14f && UnityEngine.Random.value < dt / 3.2f) _blinkAt = _clock;
            float blink = _clock - _blinkAt < 0.14f ? 1f - Mathf.Sin((_clock - _blinkAt) / 0.14f * Mathf.PI) : 1f;
            DrawFace(blink);

            // The glow (antennae, rings, thruster) breathes, and flashes on a celebration.
            if (glow != null)
            {
                float g = 1f + 0.25f * Mathf.Sin(_clock * 2.4f) + 1.4f * _hop * (_mood == Mood.Celebrate ? 1f : 0.3f);
                glow.GetPropertyBlock(_block);
                Color c = theme != null ? theme.cyan : new Color(0.1f, 0.88f, 1f);
                _block.SetColor("_Emission", c * g);
                glow.SetPropertyBlock(_block);
            }

            UpdatePointer(dt);
        }

        private void UpdatePointer(float dt)
        {
            if (pointer == null) return;
            bool on = _pointing && hintRing != null && hintRing.gameObject.activeInHierarchy;
            _pointerAlpha = Mathf.MoveTowards(_pointerAlpha, on ? 1f : 0f, dt * 4f);
            pointer.enabled = _pointerAlpha > 0.01f;
            if (!pointer.enabled) return;
            Vector3 from = body.TransformPoint(new Vector3(0f, 0.02f, 0.2f));
            Vector3 to = Vector3.Lerp(from, _pointAt, Rate.Smooth(_pointerAlpha));
            pointer.SetPosition(0, from);
            pointer.SetPosition(1, to);
            Color y = theme != null ? theme.explainYellow : Color.yellow;
            float pulse = 0.65f + 0.35f * Mathf.Sin(_clock * 6f);
            y.a = _pointerAlpha * pulse;
            pointer.startColor = y;
            pointer.endColor = y;
        }

        // ---------- the face ----------

        private void DrawFace(float blink)
        {
            if (face == null || face.Count < Slots) return;
            Draw(i => face[i], _face, blink, theme != null ? theme.cyan : new Color(0.1f, 0.88f, 1f));
            face.MarkDirty();
        }

        /// <summary>The face slots, drawn for a mood: the builder uses it for the face Pixel shows before it moves.</summary>
        public static List<Shape> FaceShapes(Mood mood, Color ink)
        {
            var shapes = new List<Shape>();
            for (int i = 0; i < Slots; i++) shapes.Add(new Shape());
            Draw(i => shapes[i], FaceFor(mood), 1f, ink);
            return shapes;
        }

        private static void Draw(Func<int, Shape> slot, Face f, float blink, Color ink)
        {
            const float eyeX = 0.052f, eyeY = 0.022f, stroke = 0.026f;

            void Eye(int index, float x, float scale, float side)
            {
                float h = f.eyeH * scale * blink;
                Vector3 c = new Vector3(x + f.gaze.x, eyeY + f.gaze.y, 0f);
                // Pill: a vertical stroke (two halves). Chevron: ^ . Droop: slanted down to the outside.
                Vector3 top = c + new Vector3(0f, h * 0.5f, 0f);
                Vector3 bottom = c - new Vector3(0f, h * 0.5f, 0f);
                Vector3 hl = c + new Vector3(-0.026f * scale, -0.012f, 0f), hm = c + new Vector3(0f, 0.014f * Mathf.Max(blink, 0.2f), 0f), hr = c + new Vector3(0.026f * scale, -0.012f, 0f);
                Vector3 sl = c + new Vector3(-0.024f * side, 0.008f, 0f), sr = c + new Vector3(0.024f * side, -0.012f, 0f);
                Vector3 a = Vector3.Lerp(Vector3.Lerp(bottom, sl, f.droop), hl, f.chevron);
                Vector3 m = Vector3.Lerp(Vector3.Lerp(c, (sl + sr) * 0.5f, f.droop), hm, f.chevron);
                Vector3 b = Vector3.Lerp(Vector3.Lerp(top, sr, f.droop), hr, f.chevron);
                float w = stroke * Mathf.Lerp(1.15f, 0.8f, f.chevron);
                Stroke(slot(index), a, m, w, ink, 1f);
                Stroke(slot(index + 1), m, b, w, ink, 1f);
            }

            Eye(EyeL, -eyeX, 1f, -1f);
            Eye(EyeR, eyeX, f.rightEye, 1f);

            // Mouth: a curve of four strokes; smile bends it, wave wobbles it.
            const float mouthY = -0.042f;
            for (int i = 0; i < MouthStrokes; i++)
            {
                float u0 = (float)i / MouthStrokes * 2f - 1f, u1 = (float)(i + 1) / MouthStrokes * 2f - 1f;
                Stroke(slot(Mouth + i), MouthPoint(u0, f, mouthY), MouthPoint(u1, f, mouthY), 0.016f, ink, 1f - f.open * 0.85f);
            }

            Shape open = slot(Open);
            open.kind = Shape.Kind.Quad;
            open.position = new Vector3(0f, mouthY - 0.012f, 0f);
            open.size = new Vector2(0.06f, 0.045f) * Mathf.Max(f.open, 0.001f);
            open.color = ink;
            open.round = 1f;
            open.alpha = f.open;

            Color blushColor = new Color(1f, 0.45f, 0.62f);
            for (int k = 0; k < 2; k++)
            {
                Shape cheek = slot(k == 0 ? CheekL : CheekR);
                cheek.kind = Shape.Kind.Quad;
                cheek.position = new Vector3(k == 0 ? -0.085f : 0.085f, -0.022f, 0.001f);
                cheek.size = new Vector2(0.03f, 0.016f);
                cheek.color = blushColor;
                cheek.round = 1f;
                cheek.alpha = f.blush * 0.7f;
            }
        }

        private static Vector3 MouthPoint(float u, Face f, float y)
        {
            float x = u * f.mouthW * 0.5f;
            float curve = -f.smile * 0.02f * (1f - u * u);
            float wobble = f.wave * 0.007f * Mathf.Sin(u * Mathf.PI * 2f);
            return new Vector3(x, y + curve + wobble, 0f);
        }

        private static void Stroke(Shape s, Vector3 a, Vector3 b, float width, Color color, float alpha)
        {
            s.kind = Shape.Kind.Line;
            s.position = a;
            s.end = b;
            s.size = new Vector2(width, width);
            s.color = color;
            s.round = 1f;
            s.grow = 1f;
            s.alpha = alpha;
        }

        // ---------- beeps (made here, no files) ----------

        private static AudioClip Tones(string name, float[] notes, float noteSeconds)
        {
            const int rate = 24000;
            int per = Mathf.RoundToInt(noteSeconds * rate);
            int gap = Mathf.RoundToInt(0.012f * rate);
            var data = new float[(per + gap) * notes.Length];
            for (int n = 0; n < notes.Length; n++)
            {
                float f0 = notes[n], f1 = n + 1 < notes.Length ? Mathf.Lerp(notes[n], notes[n + 1], 0.15f) : notes[n];
                double phase = 0;
                for (int i = 0; i < per; i++)
                {
                    float t = (float)i / per;
                    float f = Mathf.Lerp(f0, f1, t);
                    phase += 2.0 * Math.PI * f / rate;
                    // A soft square-ish tone (sine plus a little third harmonic), with a quick attack and release.
                    float env = Mathf.Min(1f, t * 25f) * Mathf.Min(1f, (1f - t) * 8f);
                    float v = (float)(Math.Sin(phase) + 0.22 * Math.Sin(3.0 * phase));
                    data[n * (per + gap) + i] = v * env * 0.16f;
                }
            }

            AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
