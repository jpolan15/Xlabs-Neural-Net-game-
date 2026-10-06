using System;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Shows a station's scenery only while that stop is live, so only one stop is ever drawn.
    /// Each group lists the kinds of stop it belongs to.
    /// </summary>
    public sealed class StationReveal : MonoBehaviour
    {
        [Serializable]
        public struct Group
        {
            public GameObject root;
            public bool oneSignal;
            public bool twoSignals;
            public bool learns;
        }

        [SerializeField] private StationController station;
        [SerializeField] private Group[] groups = new Group[0];
        [SerializeField] private float lingerSeconds = 2.5f;

        private float _hideAt = -1f;

        private void OnEnable()
        {
            if (station == null) return;
            station.Began += OnBegan;
            station.Ended += OnEnded;
            SetAll(false);
        }

        private void OnDisable()
        {
            if (station == null) return;
            station.Began -= OnBegan;
            station.Ended -= OnEnded;
        }

        private void OnBegan(StationKind kind)
        {
            _hideAt = -1f;
            for (int i = 0; i < groups.Length; i++)
            {
                bool show = kind == StationKind.OneSignal ? groups[i].oneSignal
                    : kind == StationKind.TwoSignals ? groups[i].twoSignals
                    : groups[i].learns;
                if (groups[i].root != null) groups[i].root.SetActive(show);
            }
        }

        private void OnEnded()
        {
            _hideAt = Time.time + lingerSeconds;
        }

        private void Update()
        {
            if (_hideAt > 0f && Time.time >= _hideAt)
            {
                _hideAt = -1f;
                SetAll(false);
            }
        }

        private void SetAll(bool on)
        {
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i].root != null) groups[i].root.SetActive(on);
            }
        }
    }
}
