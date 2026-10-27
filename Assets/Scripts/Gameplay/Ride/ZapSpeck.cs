using System;
using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>One target of the travel game: a handwritten digit that flies at the pod, wobbles, and bursts when the laser hits it.</summary>
    public sealed class ZapSpeck : MonoBehaviour, IRideTarget
    {
        private ZapGame _game;
        private Vector3 _drift;
        private float _dies;
        private float _born;
        private float _phase;
        private bool _retired;

        /// <summary>The atlas cell it shows (-1: none).</summary>
        public int Picture { get; private set; } = -1;

        /// <summary>The digit it is (-1: no picture, always one to blast).</summary>
        public int Label { get; private set; } = -1;

        /// <summary>It was given a new picture and launched (views set what it shows).</summary>
        public event Action Launched;

        internal void Init(ZapGame game) => _game = game;

        internal void Launch(Vector3 localPosition, Vector3 drift, float life, int picture, int label)
        {
            Picture = picture;
            Label = label;
            transform.localPosition = localPosition;
            _drift = drift;
            _born = Time.time;
            _dies = Time.time + life;
            _phase = localPosition.x * 3.1f;
            _retired = false;
            gameObject.SetActive(true);
            Launched?.Invoke();
        }

        private void Update()
        {
            if (Time.time > _dies)
            {
                gameObject.SetActive(false);
                if (_game != null && !_retired) _game.OnExpired(this);
                return;
            }

            float t = Time.time - _born;
            transform.localPosition += (_drift + new Vector3(Mathf.Sin(t * 2.3f + _phase), Mathf.Cos(t * 1.7f + _phase), 0f) * 0.18f) * Time.deltaTime;
            float grow = Mathf.Clamp01(t * 3f);
            float fade = Mathf.Clamp01((_dies - Time.time) * 3f);
            float pulse = 1f + 0.06f * Mathf.Sin(t * 6f + _phase);
            transform.localScale = Vector3.one * (grow * fade * pulse);
        }

        /// <summary>The ride stopped: fade out within this many seconds, without counting as a 3 that got away. Can't be hit meanwhile.</summary>
        internal void Retire(float seconds)
        {
            if (_retired) return;
            _retired = true;
            _dies = Mathf.Min(_dies, Time.time + seconds);
        }

        public bool Hit(int hand)
        {
            if (!gameObject.activeSelf || _game == null || _retired) return false;
            _game.OnShot(this, hand);
            gameObject.SetActive(false);
            return true;
        }
    }
}
