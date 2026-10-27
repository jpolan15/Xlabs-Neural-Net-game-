using System;
using UnityEngine;
using Convergence.Core.Puzzles;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The ride's sound effects. It only listens: the director (launch, crossing into a region, skips, the end), the
    /// neuron puzzle (a tick per tap, a chime when it is solved), the wrong guess, each nudged wire, and every hop down
    /// the Valley (pitch falls with the loss, so you hear it go downhill). A cockpit hum runs the whole ride and an
    /// engine loop follows the pod's speed; both drop under the narration. One-shots come from a fixed pool of sources,
    /// so nothing allocates during play. The builder sets every level from measured clip loudness
    /// (ArtSource/sfx/make_sfx.py), so effects sit under the voice by numbers, not by ear.
    /// </summary>
    public sealed class RideSfxView : MonoBehaviour
    {
        [Serializable]
        public struct Cue
        {
            public AudioClip clip;

            [Range(0f, 1f)] public float volume;

            [Tooltip("Random pitch spread, plus or minus, so repeats do not sound identical.")]
            public float pitchJitter;
        }

        [SerializeField] private RideDirector director;
        [SerializeField] private DashScreenView dash;
        [SerializeField] private WeightPaintMoment puzzle;
        [SerializeField] private BlameMoment blame;
        [SerializeField] private ValleyRunMoment valley;
        [SerializeField] private ZapGame zap;
        [SerializeField] private TrainMoment train;
        [SerializeField] private StumpMoment stump;
        [SerializeField] private ForwardPassMoment send;

        [Header("Sources")]
        [SerializeField] private AudioSource hum;
        [SerializeField] private AudioSource engine;

        [Tooltip("One-shot voices, used round robin.")]
        [SerializeField] private AudioSource[] pool = new AudioSource[0];

        [Header("Loops")]
        [SerializeField] private float humVolume = 0.08f;
        [SerializeField] private float engineVolume = 0.17f;

        [Tooltip("Pod speed, metres per second, at which the engine loop is at full volume.")]
        [SerializeField] private float engineFullSpeed = 4f;

        [Tooltip("The loops drop to this fraction of their level while a line is spoken.")]
        [SerializeField] private float duckUnderVoice = 0.55f;

        [Header("Cues")]
        [SerializeField] private Cue launch;
        [SerializeField] private Cue launchRumble;
        [SerializeField] private Cue region;
        [SerializeField] private Cue tick;
        [SerializeField] private Cue solved;
        [SerializeField] private Cue solvedShimmer;
        [SerializeField] private Cue wrong;
        [SerializeField] private Cue fire;
        [SerializeField] private Cue pop;
        [SerializeField] private Cue plugIn;
        [SerializeField] private Cue win;
        [SerializeField] private Cue winShimmer;
        [SerializeField] private Cue nudge;
        [SerializeField] private Cue hop;
        [SerializeField] private Cue finale;
        [SerializeField] private Cue finaleShimmer;
        [SerializeField] private Cue skip;

        private int _next;
        private bool _launched;

        private void OnEnable()
        {
            if (director != null)
            {
                director.SegmentStarted += OnSegment;
                director.RegionChanged += OnRegion;
                director.NarrationSkipped += OnSkipped;
                director.RideCompleted += OnCompleted;
            }

            if (dash != null) dash.SegmentShown += OnCue;
            if (puzzle != null)
            {
                puzzle.Stroked += OnStroked;
                puzzle.Solved += OnSolved;
            }

            if (blame != null) blame.Connected += OnConnected;
            if (valley != null) valley.HopReached += OnHop;
            if (zap != null) zap.Shot += OnShot;
            if (train != null)
            {
                train.Checked += OnChecked;
                train.BlewUp += OnBlewUp;
            }

            if (stump != null) stump.Judged += OnJudged;
            if (send != null) send.Thrown += OnThrown;
            RideAim.Fired += OnFired;
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.SegmentStarted -= OnSegment;
                director.RegionChanged -= OnRegion;
                director.NarrationSkipped -= OnSkipped;
                director.RideCompleted -= OnCompleted;
            }

            if (dash != null) dash.SegmentShown -= OnCue;
            if (puzzle != null)
            {
                puzzle.Stroked -= OnStroked;
                puzzle.Solved -= OnSolved;
            }

            if (blame != null) blame.Connected -= OnConnected;
            if (valley != null) valley.HopReached -= OnHop;
            if (zap != null) zap.Shot -= OnShot;
            if (train != null)
            {
                train.Checked -= OnChecked;
                train.BlewUp -= OnBlewUp;
            }

            if (stump != null) stump.Judged -= OnJudged;
            if (send != null) send.Thrown -= OnThrown;
            RideAim.Fired -= OnFired;
            WatchGame(null);
        }

        private RideGame _game;

        private void WatchGame(RideGame game)
        {
            if (game == _game) return;
            if (_game != null) _game.Won -= OnWon;
            _game = game;
            if (_game != null) _game.Won += OnWon;
        }

        private void OnWon(int stars, bool byRider)
        {
            Play(win, 1f);
            Play(winShimmer, 1f);
        }

        // A blaster shot while the digits fly; elsewhere (drawing, buttons) a soft click.
        private void OnFired(int hand, bool hit)
        {
            if (zap != null && zap.IsRunning) Play(fire, hit ? 1.1f : 0.95f);
            else Play(tick, hit ? 1.2f : 1f);
        }

        // A 3 bursts (higher with the streak); any other digit buzzes wrong.
        private void OnShot(Vector3 where, bool right, int points)
        {
            if (right) Play(pop, 1f + 0.08f * (zap.LastMultiplier - 1));
            else Play(wrong, 1f);
        }

        // The training game ticks higher as the accuracy climbs, and a blow-up sounds wrong.
        private void OnChecked(double accuracy) => Play(tick, 0.7f + (float)accuracy * 0.8f);

        private void OnBlewUp() => Play(wrong, 0.8f);

        private void OnJudged(int prompt, int guess, bool right) => Play(right ? solved : wrong, right ? 1.1f : 1f);

        private void OnThrown(bool byRider) => Play(plugIn, 1.1f);

        /// <summary>Adds every clip this view can play to a list (the boot loads them before the ride starts).</summary>
        public void CollectClips(System.Collections.Generic.List<AudioClip> into)
        {
            foreach (Cue cue in new[] { launch, launchRumble, region, tick, solved, solvedShimmer, wrong, fire, pop, plugIn, win, winShimmer, nudge, hop, finale, finaleShimmer, skip })
            {
                if (cue.clip != null) into.Add(cue.clip);
            }

            if (hum != null && hum.clip != null) into.Add(hum.clip);
            if (engine != null && engine.clip != null) into.Add(engine.clip);
        }

        private void Start()
        {
            StartLoop(hum, humVolume);
            StartLoop(engine, 0f);
        }

        private void Update()
        {
            if (director != null) WatchGame(director.ActiveMoment as RideGame);
            float duck = dash != null && dash.IsSpeaking ? duckUnderVoice : 1f;
            float step = Time.deltaTime * 0.6f;
            if (hum != null) hum.volume = Mathf.MoveTowards(hum.volume, humVolume * duck, step * humVolume);
            if (engine != null && director != null)
            {
                float speed01 = Mathf.Clamp01(Mathf.Abs(director.Speed) / Mathf.Max(0.1f, engineFullSpeed));
                engine.volume = Mathf.MoveTowards(engine.volume, engineVolume * speed01 * duck, step * 2f * engineVolume);
                engine.pitch = Mathf.Lerp(0.85f, 1.1f, speed01);
            }
        }

        private static void StartLoop(AudioSource source, float volume)
        {
            if (source == null || source.clip == null) return;
            source.loop = true;
            source.volume = volume;
            source.Play();
        }

        // ---------- events ----------

        private void OnSegment(int index)
        {
            if (index != 0 || _launched) return;
            _launched = true;
            Play(launch, 1f);
            Play(launchRumble, 1f);
        }

        private void OnRegion(int index) => Play(region, Mathf.Lerp(0.85f, 1.2f, index / 7f));

        private void OnSkipped() => Play(skip, 1f);

        // The right hand paints +, green, with a bright high click; the left paints −, red, with a lower one; a rub-out
        // (the same colour again) is a soft middle tick. The pitch also climbs a little as more digits come out right.
        private void OnStroked(int cell, int brush, int value, bool byRider)
        {
            if (puzzle == null || !puzzle.IsLive) return;
            float hand = value == 0 ? 1f : brush > 0 ? 1.4f : 0.75f;
            float progress = puzzle.Result != null ? puzzle.Result.CorrectCount / 8f : 0f;
            Play(tick, hand * Mathf.Lerp(0.95f, 1.1f, Mathf.Clamp01(progress)));
        }

        private void OnSolved(bool byRider)
        {
            Play(solved, 1f);
            Play(solvedShimmer, 1f);
        }

        private void OnCue(RideLine line, int index, NarrationLibrary.Segment segment)
        {
            if (NarrationLibrary.HasCue(segment, "wrong")) Play(wrong, 1f);
            if (NarrationLibrary.HasCue(segment, "right")) Play(solvedShimmer, 1.1f);
        }

        // Each wire that snaps onto the 3 rings a little higher than the last (1.00x, 1.12x, 1.25x, 1.40x ...), with a
        // plug-in click under it, so finishing the set sounds like climbing a scale.
        private void OnConnected(int k, int hand, int order)
        {
            float pitch = Mathf.Min(1.8f, Mathf.Pow(1.12f, order));
            Play(nudge, pitch);
            Play(plugIn, pitch);
        }

        private void OnHop(int k)
        {
            if (k == 0 || valley == null) return;
            // The tick falls in pitch as the loss falls: you hear the weights roll downhill.
            float start = (float)valley.Hop(0).loss;
            float loss = (float)valley.Hop(k).loss;
            Play(hop, Mathf.Lerp(0.75f, 1.35f, Mathf.Clamp01(loss / Mathf.Max(0.01f, start * 1.4f))));
        }

        private void OnCompleted()
        {
            Play(finale, 1f);
            Play(finaleShimmer, 1f);
        }

        // ---------- playback ----------

        private void Play(Cue cue, float pitch)
        {
            if (cue.clip == null || cue.volume <= 0f || pool.Length == 0) return;

            AudioSource source = pool[_next];
            _next = (_next + 1) % pool.Length;
            source.transform.localPosition = Vector3.zero;
            source.spatialBlend = 0f;
            source.clip = cue.clip;
            source.volume = cue.volume;
            source.pitch = pitch * (1f + UnityEngine.Random.Range(-cue.pitchJitter, cue.pitchJitter));
            source.Play();
        }
    }
}
