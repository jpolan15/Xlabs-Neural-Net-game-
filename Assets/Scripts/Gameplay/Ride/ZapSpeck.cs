using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>One speck of noise: drifts past the pod, wobbles, and pops when the laser hits it.</summary>
    public sealed class ZapSpeck : MonoBehaviour, IRideTarget
    {
        private ZapGame _game;
        private Vector3 _drift;
        private float _dies;
        private float _born;
        private float _phase;

        internal void Init(ZapGame game) => _game = game;

        internal void Launch(Vector3 localPosition, Vector3 drift, float life)
        {
            transform.localPosition = localPosition;
            _drift = drift;
            _born = Time.time;
            _dies = Time.time + life;
            _phase = localPosition.x * 3.1f;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (Time.time > _dies)
            {
                gameObject.SetActive(false);
                return;
            }

            float t = Time.time - _born;
            transform.localPosition += (_drift + new Vector3(Mathf.Sin(t * 2.3f + _phase), Mathf.Cos(t * 1.7f + _phase), 0f) * 0.25f) * Time.deltaTime;
            float grow = Mathf.Clamp01(t * 3f);
            float pulse = 1f + 0.15f * Mathf.Sin(t * 9f + _phase);
            transform.localScale = Vector3.one * (grow * pulse);
            transform.Rotate(0f, 90f * Time.deltaTime, 45f * Time.deltaTime, Space.Self);
        }

        public bool Hit(int hand)
        {
            if (!gameObject.activeSelf || _game == null) return false;
            _game.OnZapped(this);
            gameObject.SetActive(false);
            return true;
        }
    }
}
