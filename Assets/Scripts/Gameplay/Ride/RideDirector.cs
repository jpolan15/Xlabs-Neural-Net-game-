using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// Runs the ride: dock, travel, stop, solve, travel, and so on. Moves the pod along a Catmull-Rom path with eased
    /// starts and stops, yaw only, so the pod never pitches or rolls. Stations raise Solved; the director only moves on.
    /// It reads the dash through <see cref="RideControlSet"/> and holds no XR types.
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
        [SerializeField] private float cruiseSpeed = 4f;
        [SerializeField] private float minTravelSeconds = 5f;
        [SerializeField] private float advanceDelay = 3f;
        [SerializeField] private float yawFollow = 3f;

        private RidePath _path;
        private RideState _state = RideState.Dock;
        private int _stopIndex = -1;
        private float _distance;
        private float _speed;
        private float _yaw;
        private Coroutine _routine;
        private RideControlChannel _action;

        public RideState State => _state;
        public int StopIndex => _stopIndex;
        public int StopCount => stops.Length;

        /// <summary>Current speed in meters per second. Drives the motion hum.</summary>
        public float Speed => _speed;

        public RideControlSet Controls => controls;

        public event Action<RideState> StateChanged;
        public event Action<int> StopReached;
        public event Action<RideLine> Said;
        public event Action<StationKind, StationPhase> PhaseChanged;

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
            ConfigureDock();
            StateChanged?.Invoke(_state);
            Said?.Invoke(RideLine.Launch);
            HintChanged?.Invoke(RideControlIds.Action, 1);
        }

        private void OnDestroy()
        {
            if (_action != null) _action.Changed -= OnDockAction;
        }

        private void ConfigureDock()
        {
            _state = RideState.Dock;
            controls.Get(RideControlIds.Rock).Configure(-2f, 2f, 0.5f, 0f, true, false);
            controls.Get(RideControlIds.Ice).Configure(-2f, 2f, 0.5f, 0f, true, false);
            controls.Get(RideControlIds.Trigger).Configure(-1f, 2f, 0.5f, 0f, true, false);
            controls.Get(RideControlIds.Eta).Configure(0f, 2f, 1f, 1f, true, false);
            _action.Configure(0f, 1f, 1f, 0f, false, true);
            _action.Changed += OnDockAction;
        }

        private void OnDockAction(RideControlChannel channel, bool byUser)
        {
            if (!byUser || _state != RideState.Dock || channel.Value < 0.5f) return;
            _action.Changed -= OnDockAction;
            HintChanged?.Invoke(null, 0);
            GoToStop(0);
        }

        private void GoToStop(int index)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(TravelThenStop(index));
        }

        private IEnumerator TravelThenStop(int index)
        {
            _state = RideState.Traveling;
            StateChanged?.Invoke(_state);

            float from = _distance;
            float to = _path.DistanceAtPoint(stops[index].pointIndex);
            float span = Mathf.Max(0.01f, to - from);
            float duration = Mathf.Max(minTravelSeconds, span / cruiseSpeed * 1.2f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
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
            yield return new WaitForSeconds(advanceDelay);

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
            StateChanged?.Invoke(_state);
            Said?.Invoke(RideLine.TargetingRestored);

            // Roll on to the end of the track with the same eased motion as between stops.
            float from = _distance;
            float to = _path.DistanceAtPoint(pathPoints.Length - 1);
            float duration = Mathf.Max(minTravelSeconds, Mathf.Max(0.01f, to - from) / cruiseSpeed * 1.2f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float previous = _distance;
                _distance = Mathf.Lerp(from, to, t * t * t * (t * (t * 6f - 15f) + 10f));
                _speed = Time.deltaTime > 0f ? (_distance - previous) / Time.deltaTime : 0f;
                Place(_distance, false);
                yield return null;
            }

            _speed = 0f;
            Said?.Invoke(RideLine.CourseLocked);
            _state = RideState.Complete;
            StateChanged?.Invoke(_state);
            RideCompleted?.Invoke();
        }

        private void ForwardSaid(RideLine line) => Said?.Invoke(line);

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
