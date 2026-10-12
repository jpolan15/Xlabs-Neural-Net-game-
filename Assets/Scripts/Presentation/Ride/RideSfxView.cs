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
                puzzle.Changed += OnPainted;
                puzzle.Solved += OnSolved;
            }

            if (blame != null) blame.Nudged += OnNudged;
            if (valley != null) valley.HopReached += OnHop;
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
                puzzle.Changed -= OnPainted;
                puzzle.Solved -= OnSolved;
            }

            if (blame != null) blame.Nudged -= OnNudged;
            if (valley != null) valley.HopReached -= OnHop;
        }

        private void Start()
        {
            StartLoop(hum, humVolume);
            StartLoop(engine, 0f);
        }

        private void Update()
        {
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

        private void OnPainted(WeightPaintResult result)
        {
            if (puzzle != null && puzzle.IsLive) Play(tick, Mathf.Lerp(0.9f, 1.25f, result.CorrectCount / 8f));
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

        private void OnNudged(int k, double before, double after) => Play(nudge, 1f + 0.05f * k);

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
