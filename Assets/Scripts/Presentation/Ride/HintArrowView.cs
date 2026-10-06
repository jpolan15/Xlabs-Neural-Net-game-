using System;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Shows a pulsing arrow beside the one lever the player should move, pointing the way to move it. No words.
    /// The direction comes from Gameplay, which scores every single-step move through the real evaluator.
    /// </summary>
    public sealed class HintArrowView : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public Transform arrow;
        }

        [SerializeField] private RideDirector director;
        [SerializeField] private Entry[] arrows = new Entry[0];
        [SerializeField] private float travel = 0.025f;
        [SerializeField] private float speed = 3.2f;

        private Vector3[] _home;
        private int _active = -1;
        private int _direction = 1;

        private void OnEnable()
        {
            _home = new Vector3[arrows.Length];
            for (int i = 0; i < arrows.Length; i++)
            {
                _home[i] = arrows[i].arrow.localPosition;
                arrows[i].arrow.gameObject.SetActive(false);
            }

            if (director != null) director.HintChanged += OnHint;
        }

        private void OnDisable()
        {
            if (director != null) director.HintChanged -= OnHint;
        }

        private void OnHint(string id, int direction)
        {
            _active = -1;
            _direction = direction >= 0 ? 1 : -1;
            for (int i = 0; i < arrows.Length; i++)
            {
                bool on = id != null && arrows[i].id == id;
                arrows[i].arrow.gameObject.SetActive(on);
                if (!on) continue;
                _active = i;
                arrows[i].arrow.localRotation = Quaternion.Euler(0f, 0f, _direction > 0 ? 0f : 180f);
            }
        }

        private void Update()
        {
            if (_active < 0) return;
            float pulse = Mathf.Repeat(Time.time * speed, 1f);
            arrows[_active].arrow.localPosition = _home[_active] + Vector3.up * (_direction * pulse * travel);
        }
    }
}
