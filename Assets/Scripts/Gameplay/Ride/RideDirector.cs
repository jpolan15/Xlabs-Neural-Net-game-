using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// Runs the ride: intro, dock, travel, stop, solve, travel, and so on. Moves the pod along a Catmull-Rom path with
    /// eased starts and stops, yaw only, so the pod never pitches or rolls. Stations raise Solved; the director only
    /// moves on. It reads the dash through <see cref="RideControlSet"/> and holds no XR types.
    /// With a <see cref="RideScript"/> it also paces the narration: a spoken intro that holds GO (with hands-on beats
    /// that wait for the rider or time out), a briefing during every travel that must finish before the next station
    /// goes live, and an outro. Without a script it behaves as the plain ride (ADR-011).
    /// </summary>
    public sealed class RideDirector : MonoBehaviour
    {
        [Serializable]
        public struct Stop
        {
            [Tooltip("Index into Path Points where the pod halts.")]
            public int pointIndex;
            public StationController station;
        }

        [SerializeField] private Transform pod;
        [SerializeField] private RideControlSet controls;
        [SerializeField] private Transform[] pathPoints = new Transform[0];
        [SerializeField] private Stop[] stops = new Stop[0];
        [SerializeField] private RideScript script;
        [SerializeField] private float cruiseSpeed = 4f;
        [SerializeField] private float minTravelSeconds = 5f;
        [SerializeField] private float advanceDelay = 3f;
        [SerializeField] private float yawFollow = 3f;
        [SerializeField] private float briefingPadSeconds = 1.5f;
        [SerializeField] private float skipHoldSeconds = 2f;

        [Tooltip("After a stop is solved, the pod waits for the line said at the solve to finish (plus this), so the lesson's last words are never cut off by the next briefing.")]
        [SerializeField] private float speechPadSeconds = 0.6f;

        [Tooltip("The longest the pod waits for that line, so a long line can never stall the ride.")]
        [SerializeField] private float maxSpeechHoldSeconds = 15f;

        [Tooltip("After a hands-on intro beat is done (the rider woke AURA, the neuron fired), the reaction gets at least this long before the next line.")]
        [SerializeField] private float taskReactSeconds = 1.8f;

        private RidePath _path;
        private float _speechEndsAt;
        private RideState _state = RideState.Dock;
        private int _stopIndex = -1;
        private float _distance;
        private float _speed;
        private float _yaw;
        private Coroutine _routine;
        private RideControlChannel _action;
        private bool _narrating;
        private bool _skipRequested;
        private bool _skippedThisLeg;
        private float _holdSeconds;
        private bool _holdNeedsRelease;

        public RideState State => _state;
        public int StopIndex => _stopIndex;
        public int StopCount => stops.Length;

        /// <summary>Current speed in meters per second. Drives the motion hum.</summary>
        public float Speed => _speed;

        /// <summary>How far along the track the pod is, 0 at the dock to 1 at the end.</summary>
        public float Progress01 => _path != null && _path.Length > 0f ? Mathf.Clamp01(_distance / _path.Length) : 0f;

        public RideControlSet Controls => controls;

        /// <summary>The pacing data the ride runs on (null for the plain unscripted ride).</summary>
        public RideScript Script => script;

        /// <summary>True while an intro, briefing, or outro line sequence is running.</summary>
        public bool IsNarrating => _narrating;

        public event Action<RideState> StateChanged;
        public event Action<int> StopReached;

        /// <summary>A line a station or the dock says in the moment (hints and reactions).</summary>
        public event Action<RideLine> Said;

        /// <summary>A scripted line has the floor now (intro, briefing, outro). It plays at once and replaces any hint chatter.</summary>
        public event Action<RideLine> Narrated;

        /// <summary>The operator skipped the running sequence. Views cut off the current line.</summary>
        public event Action NarrationSkipped;

        /// <summary>The briefing for this stop begins; it ends before the stop goes live.</summary>
        public event Action<int> BriefingStarted;
        public event Action<int> BriefingEnded;

        /// <summary>The last stop is solved and the outro begins. The big network ignites here.</summary>
        public event Action OutroStarted;

        public event Action<StationKind, StationPhase> PhaseChanged;

        /// <summary>A hands-on intro beat opens: the ride waits for the rider to set this control to at least this value.</summary>
        public event Action<string, float> RiderTaskStarted;

        /// <summary>That beat is done: true if the rider did it, false if it timed out and the ride did it for them.</summary>
        public event Action<string, bool> RiderTaskDone;

        /// <summary>Which lever to nudge and which way. A null id clears the hint.</summary>
        public event Action<string, int> HintChanged;
        public event Action<StationSummary> StationSolved;
        public event Action RideCompleted;

        private void Start()
        {
            if (pod == null || controls == null || pathPoints.Length < 2)
            {
                Debug.LogError("[RideDirector] Needs a pod, a control set, and at least two path points.");
                enabled = false;
                return;
            }

            var points = new List<Vector3>(pathPoints.Length);
            for (int i = 0; i < pathPoints.Length; i++) points.Add(pathPoints[i].position);
            _path = new RidePath(points);
            _distance = 0f;
            Place(_distance, true);

            _action = controls.Get(RideControlIds.Action);
            ConfigureLevers();
            if (script != null && !script.intro.IsEmpty) _routine = StartCoroutine(IntroThenDock());
            else EnterDock(true);
        }

        private void OnDestroy()
        {
            if (_action != null) _action.Changed -= OnDockAction;
        }

        private void Update()
        {
            // During the intro GO is a hold-to-skip handle: a short touch does nothing, two seconds skips. The pull that
            // wakes AURA is not a skip, even if the rider keeps holding: the timer only counts again after a release.
            if (_state != RideState.Intro || _action == null)
            {
                _holdSeconds = 0f;
                _holdNeedsRelease = false;
                return;
            }

            if (_action.Value < 0.5f)
            {
                _holdSeconds = 0f;
                _holdNeedsRelease = false;
                return;
            }

            if (_holdNeedsRelease)
            {
                _holdSeconds = 0f;
                return;
            }

            _holdSeconds += Time.unscaledDeltaTime;
            if (_holdSeconds < skipHoldSeconds) return;
            _holdSeconds = 0f;
            SkipNarration();
        }

        /// <summary>Cuts the running intro, briefing, or outro short. Safe to call at any time.</summary>
        public void SkipNarration()
        {
            if (_narrating) _skipRequested = true;
        }

        private void ConfigureLevers()
        {
            controls.Get(RideControlIds.Rock).Configure(-2f, 2f, 0.5f, 0f, true, false);
            controls.Get(RideControlIds.Ice).Configure(-2f, 2f, 0.5f, 0f, true, false);
            controls.Get(RideControlIds.Trigger).Configure(-1f, 2f, 0.5f, 0f, true, false);
            controls.Get(RideControlIds.Eta).Configure(0f, 2f, 1f, 1f, true, false);
        }

        private void EnterDock(bool announceLaunch)
        {
            _state = RideState.Dock;
            _action.Configure(0f, 1f, 1f, 0f, false, true);

            // While the rider decides to launch, the ROCK lever is a weight they can play with: the explainer panel shows
            // one connection whose weight follows it ("change the weights, and you change what the AI thinks").
            // It locks again the moment GO is pulled.
            controls.Get(RideControlIds.Rock).Configure(-2f, 2f, 0.5f, 1f, false, true);
            _action.Changed -= OnDockAction;
            _action.Changed += OnDockAction;
            StateChanged?.Invoke(_state);
            if (announceLaunch) Said?.Invoke(RideLine.Launch);
            HintChanged?.Invoke(RideControlIds.Action, 1);
        }

        private IEnumerator IntroThenDock()
        {
            _state = RideState.Intro;
            _action.Configure(0f, 1f, 1f, 0f, false, true);
            StateChanged?.Invoke(_state);
            yield return RunBeats(script.intro);
            EnterDock(false);
        }

        private void OnDockAction(RideControlChannel channel, bool byUser)
        {
            if (!byUser || _state != RideState.Dock || channel.Value < 0.5f) return;
            _action.Changed -= OnDockAction;
            HintChanged?.Invoke(null, 0);
            ConfigureLevers(); // the dock's weight toy locks; each stop sets up its own levers
            GoToStop(0);
        }

        private void GoToStop(int index)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(TravelThenStop(index));
        }

        /// <summary>Plays one sequence beat by beat. Each beat holds the floor for its seconds (or until the rider acts) unless skipped.</summary>
        private IEnumerator RunBeats(RideScript.Sequence sequence)
        {
            _narrating = true;
            _skipRequested = false;
            RideScript.Beat[] beats = sequence.beats ?? new RideScript.Beat[0];
            for (int i = 0; i < beats.Length && !_skipRequested; i++)
            {
                Narrated?.Invoke(beats[i].line);
                if (beats[i].WaitsForRider) yield return RiderBeat(beats[i]);
                else yield return Hold(beats[i].seconds);
            }

            bool skipped = _skipRequested;
            _skipRequested = false;
            _narrating = false;
            if (skipped)
            {
                _skippedThisLeg = true;
                NarrationSkipped?.Invoke();
            }
        }

        private IEnumerator Hold(float seconds)
        {
            float held = 0f;
            while (held < seconds && !_skipRequested)
            {
                held += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// A hands-on beat: its line plays and the ride waits for the rider to set a control to at least a value. If
        /// nobody does by the timeout, the ride sets it itself (except GO, which would read as a skip), so the moment
        /// still happens and nobody gets stuck. Either way the line finishes and the reaction gets a moment of its own.
        /// </summary>
        private IEnumerator RiderBeat(RideScript.Beat beat)
        {
            RideControlChannel control = controls.Get(beat.waitFor);
            if (control == null)
            {
                Debug.LogError("[RideDirector] The intro waits for control '" + beat.waitFor + "', which the dash does not have; the beat only holds its seconds.");
                yield return Hold(beat.seconds);
                yield break;
            }

            bool isGo = control == _action;
            if (!isGo) control.Configure(control.Min, control.Max, control.Step, beat.waitFrom, false, true);
            HintChanged?.Invoke(beat.waitFor, 1);
            RiderTaskStarted?.Invoke(beat.waitFor, beat.waitAtLeast);

            float held = 0f;
            float doneAt = -1f;
            while (!_skipRequested)
            {
                if (doneAt < 0f)
                {
                    bool byRider = control.Value >= beat.waitAtLeast;
                    if (byRider || held >= beat.seconds + beat.waitTimeout)
                    {
                        doneAt = held;
                        if (!byRider && !isGo) control.Drive(beat.waitAtLeast);
                        if (byRider && isGo) _holdNeedsRelease = true;
                        HintChanged?.Invoke(null, 0);
                        RiderTaskDone?.Invoke(beat.waitFor, byRider);
                    }
                }
                else if (held >= beat.seconds && held >= doneAt + taskReactSeconds)
                {
                    break;
                }

                held += Time.deltaTime;
                yield return null;
            }

            if (doneAt < 0f) HintChanged?.Invoke(null, 0);

            // The lever stays in view, locked, until the dock hands it back as a toy (EnterDock).
            if (!isGo) control.Configure(control.Min, control.Max, control.Step, control.Value, true, true);
        }

        private IEnumerator PlayBriefing(int index, RideScript.Sequence sequence)
        {
            _narrating = true;
            BriefingStarted?.Invoke(index);
            yield return RunBeats(sequence);
            BriefingEnded?.Invoke(index);
        }

        /// <summary>
        /// Moves along the track over the given seconds with the eased profile. If the operator skips the narration
        /// mid-leg, time speeds up smoothly toward the plain pace, so the pod never lurches.
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
                _speed = Time.deltaTime > 0f ? (_distance - previous) / Time.deltaTime : 0f;
                Place(_distance, false);
                yield return null;
            }

            _distance = to;
            _speed = 0f;
            Place(_distance, false);
        }

        private IEnumerator TravelThenStop(int index)
        {
            _state = RideState.Traveling;
            _skippedThisLeg = false;
            StateChanged?.Invoke(_state);

            float to = _path.DistanceAtPoint(stops[index].pointIndex);
            float span = Mathf.Max(0.01f, to - _distance);
            float plainDuration = Mathf.Max(minTravelSeconds, span / cruiseSpeed * 1.2f);
            float duration = plainDuration;

            RideScript.Sequence briefing = script != null ? script.BriefingFor(index) : default;
            if (!briefing.IsEmpty)
            {
                // The travel lasts at least as long as the lesson, so the lesson is never cut off by the puzzle.
                duration = Mathf.Max(duration, briefing.TotalSeconds + briefingPadSeconds);
                StartCoroutine(PlayBriefing(index, briefing));
            }

            yield return Travel(to, duration, plainDuration);

            // Hold the levers until the briefing is done.
            while (_narrating) yield return null;

            _stopIndex = index;
            _state = RideState.AtStop;
            StateChanged?.Invoke(_state);
            StopReached?.Invoke(index);

            StationController station = stops[index].station;
            station.Said += ForwardSaid;
            station.Solved += ForwardSolved;
            station.PhaseChanged += ForwardPhase;
            station.HintChanged += ForwardHint;
            station.Begin(controls);

            while (!station.IsSolved) yield return null;

            // The line said at the solve is the lesson's punchline ("That's all a neuron is: math."). Let it finish
            // before the pod moves on, because the next briefing would cut it off.
            float solvedAt = Time.time;
            while (Time.time < solvedAt + advanceDelay
                   || (Time.time < _speechEndsAt + speechPadSeconds && Time.time < solvedAt + maxSpeechHoldSeconds))
            {
                yield return null;
            }

            station.Said -= ForwardSaid;
            station.Solved -= ForwardSolved;
            station.PhaseChanged -= ForwardPhase;
            station.HintChanged -= ForwardHint;
            station.End();

            if (index + 1 < stops.Length) GoToStop(index + 1);
            else _routine = StartCoroutine(Finish());
        }

        private IEnumerator Finish()
        {
            _state = RideState.Traveling;
            _skippedThisLeg = false;
            StateChanged?.Invoke(_state);
            OutroStarted?.Invoke();

            // Roll on to the end of the track with the same eased motion as between stops.
            float to = _path.DistanceAtPoint(pathPoints.Length - 1);
            float plainDuration = Mathf.Max(minTravelSeconds, Mathf.Max(0.01f, to - _distance) / cruiseSpeed * 1.2f);
            float duration = plainDuration;

            RideScript.Sequence outro = script != null ? script.outro : default;
            if (!outro.IsEmpty)
            {
                duration = Mathf.Max(duration, outro.TotalSeconds + briefingPadSeconds);
                StartCoroutine(RunBeats(outro));
            }
            else
            {
                Said?.Invoke(RideLine.TargetingRestored);
            }

            yield return Travel(to, duration, plainDuration);
            while (_narrating) yield return null;

            if (outro.IsEmpty) Said?.Invoke(RideLine.CourseLocked);
            _state = RideState.Complete;
            StateChanged?.Invoke(_state);
            RideCompleted?.Invoke();
        }

        private void ForwardSaid(RideLine line)
        {
            // The newest line is the one on screen (a closing line cuts off older chatter), so its end is what to wait for.
            _speechEndsAt = Time.time + (script != null ? script.SecondsOf(line) : 0f);
            Said?.Invoke(line);
        }

        private void ForwardSolved(StationSummary summary) => StationSolved?.Invoke(summary);

        private void ForwardHint(string id, int direction) => HintChanged?.Invoke(id, direction);

        private void ForwardPhase(StationKind kind, StationPhase phase) => PhaseChanged?.Invoke(kind, phase);

        private void Place(float distance, bool snapYaw)
        {
            pod.position = _path.Position(distance);
            Vector3 heading = _path.Heading(distance);
            heading.y = 0f;
            if (heading.sqrMagnitude < 1e-6f) return;
            float target = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
            _yaw = snapYaw ? target : Mathf.LerpAngle(_yaw, target, 1f - Mathf.Exp(-yawFollow * Time.deltaTime));
            pod.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }
    }
}
