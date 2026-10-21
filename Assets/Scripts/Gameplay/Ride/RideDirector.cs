using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// Runs Level 1 as one continuous ride: a narrated intro that holds GO, the dock, then an ordered list of segments.
    /// Level 1 v4 pairs them: an explain segment stops in the dark while its explainer plays on the narration's cues
    /// (Presentation), holds while it unfolds into the place, and the play segment that follows runs the moment.
    /// Each segment moves the pod along one leg of track to a distance (forward, or backward for the rewind), plays
    /// its travel lines on the way and its arrival lines once stopped, then runs its moment (the puzzle, the forward
    /// pass, the blame, the Valley drop) and waits for it to finish and for the last line to end before moving on.
    /// The pod only ever yaws, with eased starts and stops; facing follows the track's own direction, so on the
    /// rewind the rider keeps facing the network while it recedes. It reads the dash through
    /// <see cref="RideControlSet"/> and holds no XR types. Moments decide nothing about the ride's order; the director
    /// decides nothing about a moment's outcome.
    /// </summary>
    public sealed class RideDirector : MonoBehaviour
    {
        [Serializable]
        public struct Leg
        {
            public string name;
            public Vector3[] points;

            [Tooltip("Gentle mode scales the height of every point at or past this distance about Gentle Base Y (-1: never).")]
            public float gentleFrom;
            public float gentleBaseY;
        }

        [Serializable]
        public struct Segment
        {
            public string name;

            [Tooltip("Which of the eight regions (0 The Page to 7 Right) this segment is in.")]
            public int region;

            public int leg;

            [Tooltip("Where on the leg the segment ends, in meters along it. Lower than where the pod is: it rolls backward.")]
            public float to;

            [Tooltip("Plain speed for the travel, m/s.")]
            public float cruise;

            public float minSeconds;

            [Tooltip("RideScript sequence played while moving (-1: none).")]
            public int travel;

            [Tooltip("RideScript sequence played once stopped (-1: none).")]
            public int arrival;

            [Tooltip("Runs after the arrival lines; the ride moves on when it finishes.")]
            public RideMoment moment;

            [Tooltip("Seconds the pod stays after the lines and the moment (an explainer unfolds into the place). Skippable.")]
            public float hold;
        }

        [SerializeField] private Transform pod;
        [SerializeField] private RideControlSet controls;
        [SerializeField] private Leg[] legs = new Leg[0];
        [SerializeField] private Segment[] segments = new Segment[0];
        [SerializeField] private RideScript script;
        [SerializeField] private float yawFollow = 3f;
        [SerializeField] private float narrationPadSeconds = 1.0f;
        [SerializeField] private float skipHoldSeconds = 2f;
        [SerializeField] private float speechPadSeconds = 0.6f;
        [SerializeField] private float maxSpeechHoldSeconds = 15f;
        [Tooltip("Gentle mode: the Valley drop is this much of its height, and travel this much slower.")]
        [SerializeField] private float gentleScale = 0.5f;

        private RidePath[] _paths;
        private RideState _state = RideState.Dock;
        private int _segment = -1;
        private int _leg;
        private int _region;
        private float _distance;
        private float _speed;
        private float _yaw;
        private float _segmentFraction;
        private float _speechEndsAt;
        private Coroutine _routine;
        private RideControlChannel _action;
        private bool _narrating;
        private bool _skipRequested;
        private bool _skippedThisLeg;
        private bool _holding;
        private bool _skipHold;
        private float _holdSeconds;
        private RideMoment _moment;
        private bool _gentle;

        public RideState State => _state;

        /// <summary>The segment running now, or -1 at the intro and the dock.</summary>
        public int SegmentIndex => _segment;
        public int SegmentCount => segments.Length;
        public IReadOnlyList<Segment> Segments => segments;

        /// <summary>The region the ride is in: 0 at the dock, then each segment's own.</summary>
        public int Region => _region;

        /// <summary>Current speed along the track in meters per second (negative while rolling backward).</summary>
        public float Speed => _speed;

        /// <summary>How far through the whole ride the pod is, 0 at the dock to 1 at the end.</summary>
        public float Progress01 => segments.Length == 0 ? 0f
            : _state == RideState.Complete ? 1f
            : Mathf.Clamp01((Mathf.Max(0, _segment) + (_segment < 0 ? 0f : _segmentFraction)) / segments.Length);

        public RideControlSet Controls => controls;
        public RideScript Script => script;
        public Transform Pod => pod;
        public int LegIndex => _leg;
        public float Distance => _distance;

        /// <summary>True while an intro or segment line sequence is running.</summary>
        public bool IsNarrating => _narrating;

        /// <summary>The moment running now, or null.</summary>
        public RideMoment ActiveMoment => _moment;

        /// <summary>Gentle mode for first-time riders: half the drop, slower travel. G on the desk.</summary>
        public bool GentleMode
        {
            get => _gentle;
            set
            {
                if (_gentle == value) return;
                _gentle = value;
                GentleModeChanged?.Invoke(value);
                if (_paths != null && _state != RideState.Traveling) Place(_distance, false);
            }
        }

        /// <summary>How much of its height the Valley keeps now (1, or the gentle scale).</summary>
        public float VerticalScale => _gentle ? gentleScale : 1f;

        public event Action<RideState> StateChanged;

        /// <summary>A line said in the moment (a hint or a reaction). It queues behind what is playing.</summary>
        public event Action<RideLine> Said;

        /// <summary>A line that has the floor now. It plays at once and replaces any chatter.</summary>
        public event Action<RideLine> Narrated;

        /// <summary>The operator skipped the running sequence. Views cut off the current line.</summary>
        public event Action NarrationSkipped;

        /// <summary>Which lever to nudge and which way. A null id clears the hint.</summary>
        public event Action<string, int> HintChanged;

        public event Action<int> SegmentStarted;

        /// <summary>The pod stopped at the end of a segment's travel; its arrival lines and moment follow.</summary>
        public event Action<int> SegmentArrived;
        public event Action<int> SegmentEnded;
        public event Action<int> RegionChanged;
        public event Action<bool> GentleModeChanged;
        public event Action RideCompleted;

        private void Start()
        {
            if (pod == null || controls == null || legs.Length == 0 || segments.Length == 0)
            {
                Debug.LogError("[RideDirector] Needs a pod, a control set, track legs and segments.");
                enabled = false;
                return;
            }

            _paths = new RidePath[legs.Length];
            for (int i = 0; i < legs.Length; i++) _paths[i] = new RidePath(legs[i].points);
            _leg = 0;
            _distance = 0f;
            Place(_distance, true);
            _action = controls.Get(RideControlIds.Action);
            if (_action == null)
            {
                Debug.LogError("[RideDirector] The dash has no GO control.");
                enabled = false;
                return;
            }

            if (script != null && !script.intro.IsEmpty) _routine = StartCoroutine(IntroThenDock());
            else EnterDock(true);
        }

        private void OnDestroy()
        {
            if (_action != null) _action.Changed -= OnDockAction;
        }

        private void Update()
        {
            // During the intro GO is a hold-to-skip handle: a short touch does nothing, two seconds skips.
            if (_state != RideState.Intro || _action == null || _action.Value < 0.5f)
            {
                _holdSeconds = 0f;
                return;
            }

            _holdSeconds += Time.unscaledDeltaTime;
            if (_holdSeconds < skipHoldSeconds) return;
            _holdSeconds = 0f;
            SkipNarration();
        }

        // ---------- operator ----------

        /// <summary>
        /// Cuts the running intro or segment lines short, and hurries a running moment to its end (the puzzle solves
        /// itself from the hints, the blame nudges itself). Safe to call at any time.
        /// </summary>
        public void SkipNarration()
        {
            if (!SkipLines() && _moment != null) _moment.Hurry();
        }

        /// <summary>
        /// Cuts the running lines (or a hold) short and nothing else: a running puzzle is left for the rider to play.
        /// Returns true when it skipped something.
        /// </summary>
        public bool SkipLines()
        {
            if (_narrating)
            {
                _skipRequested = true;
                return true;
            }

            if (_moment == null && _holding)
            {
                _skipHold = true;
                NarrationSkipped?.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>Hurries the running moment to its end (the puzzle follows its own hints). No-op between moments.</summary>
        public void HurryMoment()
        {
            if (_moment != null) _moment.Hurry();
        }

        // ---------- what moments use ----------

        /// <summary>Queues a line said in the moment (hints, reactions).</summary>
        public void Say(RideLine line)
        {
            _speechEndsAt = Mathf.Max(_speechEndsAt, Time.time) + (script != null ? script.SecondsOf(line) : 0f);
            Said?.Invoke(line);
        }

        /// <summary>Gives a line the floor at once. Returns how long it holds it.</summary>
        public float Narrate(RideLine line)
        {
            float seconds = script != null ? script.SecondsOf(line) : 0f;
            _speechEndsAt = Time.time + seconds;
            Narrated?.Invoke(line);
            return seconds;
        }

        /// <summary>Waits until the last line said or narrated has finished (plus a pad), or a long line's cap.</summary>
        public IEnumerator WaitForSpeech()
        {
            float started = Time.time;
            while (Time.time < _speechEndsAt + speechPadSeconds && Time.time < started + maxSpeechHoldSeconds) yield return null;
        }

        /// <summary>For a moment that moves the pod (the Valley): an eased move along the current leg.</summary>
        public IEnumerator TravelTo(float distance, float seconds)
        {
            yield return Travel(distance, Mathf.Max(0.05f, seconds), Mathf.Max(0.05f, seconds));
        }

        /// <summary>The height the pod has at a distance on a leg, after gentle mode.</summary>
        public Vector3 PositionOn(int leg, float distance)
        {
            if (_paths == null) return Vector3.zero;
            Vector3 p = _paths[leg].Position(distance);
            Leg l = legs[leg];
            if (_gentle && l.gentleFrom >= 0f && distance >= l.gentleFrom) p.y = l.gentleBaseY + (p.y - l.gentleBaseY) * gentleScale;
            return p;
        }

        // ---------- flow ----------

        private IEnumerator IntroThenDock()
        {
            SetState(RideState.Intro);
            _action.Configure(0f, 1f, 1f, 0f, false, true);
            yield return RunBeats(script.intro);
            EnterDock(true);
        }

        private void EnterDock(bool announce)
        {
            SetState(RideState.Dock);
            _action.Configure(0f, 1f, 1f, 0f, false, true);
            _action.Changed -= OnDockAction;
            _action.Changed += OnDockAction;
            if (announce && script != null) Narrate(script.dockLine);
            HintChanged?.Invoke(RideControlIds.Action, 1);
        }

        private void OnDockAction(RideControlChannel channel, bool byUser)
        {
            if (!byUser || _state != RideState.Dock || channel.Value < 0.5f) return;
            _action.Changed -= OnDockAction;
            HintChanged?.Invoke(null, 0);
            _action.Configure(0f, 1f, 1f, 1f, true, true);
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(RunSegments());
        }

        private IEnumerator RunSegments()
        {
            for (int i = 0; i < segments.Length; i++) yield return RunSegment(i);
            SetState(RideState.Complete);
            RideCompleted?.Invoke();
        }

        private IEnumerator RunSegment(int index)
        {
            Segment s = segments[index];
            _segment = index;
            _segmentFraction = 0f;
            _skippedThisLeg = false;
            if (s.leg != _leg)
            {
                // Switching legs: the builder lays the legs so they meet here.
                _leg = Mathf.Clamp(s.leg, 0, _paths.Length - 1);
                _distance = _paths[_leg].ClosestDistance(pod.position);
            }

            if (s.region != _region)
            {
                _region = s.region;
                RegionChanged?.Invoke(_region);
            }

            SegmentStarted?.Invoke(index);
            SetState(RideState.Traveling);

            float span = Mathf.Abs(s.to - _distance);
            RideScript.Sequence travelLines = script != null ? script.SequenceAt(s.travel) : default;
            if (!travelLines.IsEmpty) StartCoroutine(PlaySequence(travelLines));
            if (span > 0.01f)
            {
                float plain = Mathf.Max(s.minSeconds, span / Mathf.Max(0.5f, s.cruise) * 1.2f) * (_gentle ? 1f / gentleScale * 0.8f : 1f);
                float duration = travelLines.IsEmpty ? plain : Mathf.Max(plain, travelLines.TotalSeconds + narrationPadSeconds);
                yield return Travel(s.to, duration, plain);
            }

            // The lesson of the way is never cut off by what happens at the stop.
            while (_narrating) yield return null;
            _segmentFraction = 1f;
            SetState(RideState.AtStop);
            SegmentArrived?.Invoke(index);

            RideScript.Sequence arrivalLines = script != null ? script.SequenceAt(s.arrival) : default;
            if (!arrivalLines.IsEmpty) yield return PlaySequence(arrivalLines);

            if (s.moment != null)
            {
                _moment = s.moment;
                yield return s.moment.RunMoment(this);
                _moment = null;
            }

            yield return WaitForSpeech();
            if (s.hold > 0f)
            {
                _holding = true;
                _skipHold = false;
                float held = 0f;
                while (held < s.hold && !_skipHold)
                {
                    held += Time.deltaTime;
                    yield return null;
                }

                _holding = false;
                _skipHold = false;
            }

            SegmentEnded?.Invoke(index);
        }

        private IEnumerator PlaySequence(RideScript.Sequence sequence)
        {
            yield return RunBeats(sequence);
        }

        /// <summary>Plays one sequence beat by beat. Each beat holds the floor for its seconds unless skipped.</summary>
        private IEnumerator RunBeats(RideScript.Sequence sequence)
        {
            _narrating = true;
            _skipRequested = false;
            RideScript.Beat[] beats = sequence.beats ?? new RideScript.Beat[0];
            for (int i = 0; i < beats.Length && !_skipRequested; i++)
            {
                Narrate(beats[i].line);
                float held = 0f;
                while (held < beats[i].seconds && !_skipRequested)
                {
                    held += Time.deltaTime;
                    yield return null;
                }
            }

            bool skipped = _skipRequested;
            _skipRequested = false;
            _narrating = false;
            if (skipped)
            {
                _skippedThisLeg = true;
                _speechEndsAt = 0f;
                NarrationSkipped?.Invoke();
            }
        }

        /// <summary>
        /// Moves along the current leg over the given seconds with an eased start and stop. If the operator skips the
        /// lines mid-leg, time speeds up smoothly toward the plain pace, so the pod never lurches.
        /// </summary>
        private IEnumerator Travel(float to, float duration, float plainDuration)
        {
            float from = _distance;
            float elapsed = 0f;
            float rate = 1f;
            while (elapsed < duration)
            {
                float target = _skippedThisLeg ? Mathf.Max(1f, duration / Mathf.Max(0.01f, plainDuration)) : 1f;
                rate = Mathf.MoveTowards(rate, target, Time.deltaTime);
                elapsed += Time.deltaTime * rate;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * t * (t * (t * 6f - 15f) + 10f);
                float previous = _distance;
                _distance = Mathf.Lerp(from, to, eased);
                _segmentFraction = Mathf.Max(_segmentFraction, eased * 0.99f);
                _speed = Time.deltaTime > 0f ? (_distance - previous) / Time.deltaTime : 0f;
                Place(_distance, false);
                yield return null;
            }

            _distance = to;
            _speed = 0f;
            Place(_distance, false);
        }

        private void Place(float distance, bool snapYaw)
        {
            RidePath path = _paths[_leg];
            pod.position = PositionOn(_leg, distance);
            Vector3 heading = path.Heading(distance);
            heading.y = 0f;
            if (heading.sqrMagnitude < 1e-6f) return;
            float target = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
            _yaw = snapYaw ? target : Mathf.LerpAngle(_yaw, target, 1f - Mathf.Exp(-yawFollow * Time.deltaTime));
            pod.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        private void SetState(RideState state)
        {
            _state = state;
            StateChanged?.Invoke(state);
        }
    }
}
