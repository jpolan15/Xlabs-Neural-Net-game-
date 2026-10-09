using System;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Shows a pulsing arrow beside the one lever the player should move, pointing the way to move it. No words.
    /// The direction comes from Gameplay, which scores every single-step move through the real evaluator. The
    /// narration can point too: while the Guide says "anything orange, you can grab" or "pull the orange GO lever"
    /// (cues "orange" and "go") the arrow sits on GO for that line; a gameplay hint always wins.
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
        [SerializeField] private DashScreenView dash;
        [SerializeField] private Entry[] arrows = new Entry[0];
        [SerializeField] private float travel = 0.025f;
        [SerializeField] private float speed = 3.2f;

        private Vector3[] _home;
        private int _active = -1;
        private int _direction = 1;
        private string _gameplayHint;
        private int _gameplayDirection = 1;
        private bool _cueHint;

        private void OnEnable()
        {
            _home = new Vector3[arrows.Length];
            for (int i = 0; i < arrows.Length; i++)
            {
                _home[i] = arrows[i].arrow.localPosition;
                arrows[i].arrow.gameObject.SetActive(false);
            }

            if (director != null) director.HintChanged += OnHint;
            if (dash != null)
            {
                dash.SegmentShown += OnSegment;
                dash.LineCleared += OnLineCleared;
            }
        }

        private void OnDisable()
        {
            if (director != null) director.HintChanged -= OnHint;
            if (dash != null)
            {
                dash.SegmentShown -= OnSegment;
                dash.LineCleared -= OnLineCleared;
            }
        }

        private void OnHint(string id, int direction)
        {
            _gameplayHint = id;
            _gameplayDirection = direction;
            _cueHint = false;
            Show(id, direction);
        }

        private void OnSegment(RideLine line, int index, NarrationLibrary.Segment segment)
        {
            if (!NarrationLibrary.HasCue(segment, "orange") && !NarrationLibrary.HasCue(segment, "go")) return;
            _cueHint = true;
            Show(RideControlIds.Action, 1);
        }

        private void OnLineCleared()
        {
            if (!_cueHint) return;
            _cueHint = false;
            Show(_gameplayHint, _gameplayDirection);
        }

        private void Show(string id, int direction)
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
