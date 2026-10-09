using System;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The ride's sound effects. It only listens: the director (launch, docking, skips, the finale), the stations
    /// (a stop going live, solves, learning steps), the data streams (sensor scans and zaps, at the object's
    /// position), the chapter card, and the lever channels (a detent tick under the player's hand). A cockpit hum
    /// runs the whole ride and an engine loop follows the pod's speed; both drop under the narration. One-shots come
    /// from a fixed pool of sources, so nothing allocates during play. The builder sets every level from measured clip
    /// loudness (ArtSource/sfx/make_sfx.py), so effects sit under the voice by numbers, not by ear.
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
        [SerializeField] private RideControlSet controls;
        [SerializeField] private DashScreenView dash;
        [SerializeField] private ChapterCardView chapterCard;
        [SerializeField] private StationController[] stations = new StationController[0];
        [SerializeField] private DataStreamView[] streams = new DataStreamView[0];

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

        [Tooltip("In-world one-shots blend this much 3D, so a zap comes from the object it hits.")]
        [SerializeField] private float worldSpatialBlend = 0.75f;

        [Header("Opening")]
        [Tooltip("When the rider wakes AURA the hum swells to this many times its level, then settles.")]
        [SerializeField] private float wakeHumSwell = 2.5f;

        [Tooltip("Seconds the sting lands before the end of JFK's last clause, with the network's flash on \"hard\".")]
        [SerializeField] private float stingLead = 0.35f;

        [Header("Cues")]
        [SerializeField] private Cue launch;
        [SerializeField] private Cue launchRumble;
        [SerializeField] private Cue dock;
        [SerializeField] private Cue stationLive;
        [SerializeField] private Cue scan;
        [SerializeField] private Cue zap;
        [SerializeField] private Cue shatter;
        [SerializeField] private Cue wrong;
        [SerializeField] private Cue tick;
        [SerializeField] private Cue solved;
        [SerializeField] private Cue solvedShimmer;
        [SerializeField] private Cue learnStep;
        [SerializeField] private Cue chapter;
        [SerializeField] private Cue finale;
        [SerializeField] private Cue finaleShimmer;
        [SerializeField] private Cue skip;

        private const float TickGapSeconds = 0.045f;

        private int _next;
        private float _lastTick;
        private double _firstLoss;
        private float _swell;
        private float _stingAt = -1f;

        private void OnEnable()
        {
            if (director != null)
            {
                director.StateChanged += OnState;
                director.StopReached += OnStopReached;
                director.StationSolved += OnSolved;
                director.NarrationSkipped += OnSkipped;
                director.RideCompleted += OnCompleted;
                director.RiderTaskDone += OnRiderTaskDone;
            }

            if (dash != null) dash.SegmentShown += OnSegment;
            if (chapterCard != null) chapterCard.Shown += OnChapterShown;

            for (int i = 0; i < stations.Length; i++)
            {
                if (stations[i] == null) continue;
                stations[i].Began += OnStationBegan;
                stations[i].TrainingStepped += OnTrainingStep;
            }

            for (int i = 0; i < streams.Length; i++)
            {
                if (streams[i] == null) continue;
                streams[i].Scanned += OnScanned;
                streams[i].Decided += OnDecided;
            }

            if (controls != null)
            {
                for (int i = 0; i < controls.Channels.Count; i++) controls.Channels[i].Changed += OnLever;
            }
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.StateChanged -= OnState;
                director.StopReached -= OnStopReached;
                director.StationSolved -= OnSolved;
                director.NarrationSkipped -= OnSkipped;
                director.RideCompleted -= OnCompleted;
                director.RiderTaskDone -= OnRiderTaskDone;
            }

            if (dash != null) dash.SegmentShown -= OnSegment;
            if (chapterCard != null) chapterCard.Shown -= OnChapterShown;

            for (int i = 0; i < stations.Length; i++)
            {
                if (stations[i] == null) continue;
                stations[i].Began -= OnStationBegan;
                stations[i].TrainingStepped -= OnTrainingStep;
            }

            for (int i = 0; i < streams.Length; i++)
            {
                if (streams[i] == null) continue;
                streams[i].Scanned -= OnScanned;
                streams[i].Decided -= OnDecided;
            }

            if (controls != null)
            {
                for (int i = 0; i < controls.Channels.Count; i++) controls.Channels[i].Changed -= OnLever;
            }
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

            _swell = Mathf.MoveTowards(_swell, 0f, Time.deltaTime / 4f);
            float humTarget = humVolume * duck * Mathf.Lerp(1f, wakeHumSwell, _swell);
            if (hum != null) hum.volume = Mathf.MoveTowards(hum.volume, humTarget, step * humVolume * (_swell > 0f ? 4f : 1f));

            if (_stingAt >= 0f && Time.time >= _stingAt)
            {
                _stingAt = -1f;
                Play(finaleShimmer, null, 1f);
            }

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

        private void OnState(RideState state)
        {
            if (state != RideState.Traveling) return;
            Play(launch, null, 1f);
            Play(launchRumble, null, 1f);
        }

        private void OnStopReached(int stopIndex) => Play(dock, null, 1f);

        private void OnStationBegan(StationKind kind) => Play(stationLive, null, 1f);

        private void OnSolved(StationSummary summary)
        {
            Play(solved, null, 1f);
            Play(solvedShimmer, null, 1f);
        }

        private void OnSkipped()
        {
            _stingAt = -1f;
            Play(skip, null, 1f);
        }

        private void OnRiderTaskDone(string control, bool byRider)
        {
            if (control == RideControlIds.Action)
            {
                // AURA wakes: a deep rumble, a shimmer, and the ship's hum swells.
                Play(launchRumble, null, 1f);
                Play(finaleShimmer, null, 1.15f);
                _swell = 1f;
            }
            else
            {
                Play(solved, null, 1f); // the neuron fired
            }
        }

        private void OnSegment(RideLine line, int index, NarrationLibrary.Segment segment)
        {
            if (NarrationLibrary.HasCue(segment, "jfk_hard")) _stingAt = Time.time + Mathf.Max(0.1f, dash.CurrentSegmentSeconds - stingLead);
        }

        private void OnCompleted()
        {
            Play(finale, null, 1f);
            Play(finaleShimmer, null, 1f);
        }

        private void OnChapterShown() => Play(chapter, null, 1f);

        private void OnScanned(CaseOutcome outcome, Vector3 at)
        {
            // Only when a sensor lamp lights, so the tick means "the sensor saw something".
            if (outcome.RockSignal >= 0.5 || outcome.IceSignal >= 0.5) Play(scan, at, 1f);
        }

        private void OnDecided(CaseOutcome outcome, Vector3 at)
        {
            if (!outcome.Fired) return;
            Play(zap, at, 1f);
            Play(outcome.Correct ? shatter : wrong, at, 1f);
        }

        private void OnTrainingStep(TrainingFrame frame)
        {
            // The tick falls in pitch as the error falls: you hear the weights roll downhill.
            if (frame.Step <= 1 || _firstLoss <= 0.0) _firstLoss = Math.Max(1e-6, frame.Loss);
            float error01 = Mathf.Clamp01((float)(frame.Loss / _firstLoss));
            Play(learnStep, null, Mathf.Lerp(0.8f, 1.35f, error01));
        }

        private void OnLever(RideControlChannel channel, bool byUser)
        {
            if (!byUser || Time.unscaledTime - _lastTick < TickGapSeconds) return;
            _lastTick = Time.unscaledTime;
            Play(tick, channel.transform.position, Mathf.Lerp(0.9f, 1.25f, channel.Fraction));
        }

        // ---------- playback ----------

        private void Play(Cue cue, Vector3? at, float pitch)
        {
            if (cue.clip == null || cue.volume <= 0f || pool.Length == 0) return;

            AudioSource source = pool[_next];
            _next = (_next + 1) % pool.Length;

            if (at.HasValue)
            {
                source.transform.position = at.Value;
                source.spatialBlend = worldSpatialBlend;
            }
            else
            {
                source.transform.localPosition = Vector3.zero;
                source.spatialBlend = 0f;
            }

            source.clip = cue.clip;
            source.volume = cue.volume;
            source.pitch = pitch * (1f + UnityEngine.Random.Range(-cue.pitchJitter, cue.pitchJitter));
            source.Play();
        }
    }
}
