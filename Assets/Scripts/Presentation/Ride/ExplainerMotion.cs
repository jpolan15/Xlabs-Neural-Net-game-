using UnityEngine;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// One small motion for a part of an explainer diagram: a pulse, a knob that swings or turns by itself, a bar
    /// that grows, a tile that pops in on its word, or a ball that rolls down a path (smoothly, or one step at a
    /// time). It restarts whenever the diagram appears, so each one plays from the beginning while the Guide explains
    /// it. Nothing allocates per frame.
    /// </summary>
    public sealed class ExplainerMotion : MonoBehaviour
    {
        public enum Mode
        {
            Pulse,
            Swing,
            Spin,
            Grow,
            Path,
            Steps,
            Appear
        }

        [SerializeField] private Mode mode = Mode.Pulse;

        [Tooltip("Cycles per second (Pulse, Swing) or degrees per second (Spin).")]
        [SerializeField] private float speed = 1f;

        [Tooltip("Scale amplitude (Pulse) or degrees either side (Swing).")]
        [SerializeField] private float amount = 0.1f;

        [Tooltip("Seconds before the motion starts (Grow, Path, Steps).")]
        [SerializeField] private float delay;

        [Tooltip("Seconds to grow (Grow), or for one pass along the path (Path, Steps).")]
        [SerializeField] private float seconds = 1f;

        [Tooltip("Seconds to rest at the end of the path before it starts again.")]
        [SerializeField] private float rest = 0.8f;

        [Tooltip("Local points the part travels through (Path, Steps).")]
        [SerializeField] private Vector3[] path = new Vector3[0];

        [Tooltip("How many steps down the path (Steps).")]
        [SerializeField] private int steps = 4;

        private Vector3 _scale;
        private Quaternion _rotation;
        private float[] _lengths;
        private float _total;
        private float _t;

        private void Awake()
        {
            _scale = transform.localScale;
            _rotation = transform.localRotation;
            _lengths = new float[Mathf.Max(0, path.Length)];
            _total = 0f;
            for (int i = 1; i < path.Length; i++)
            {
                _total += Vector3.Distance(path[i - 1], path[i]);
                _lengths[i] = _total;
            }
        }

        private void OnEnable()
        {
            _t = 0f;
            Apply();
        }

        private void Update()
        {
            _t += Time.deltaTime;
            Apply();
        }

        private void Apply()
        {
            switch (mode)
            {
                case Mode.Pulse:
                    transform.localScale = _scale * (1f + amount * Mathf.Sin(_t * speed * Mathf.PI * 2f));
                    break;
                case Mode.Swing:
                    transform.localRotation = _rotation * Quaternion.Euler(0f, 0f, amount * Mathf.Sin(_t * speed * Mathf.PI * 2f));
                    break;
                case Mode.Spin:
                    transform.localRotation = _rotation * Quaternion.Euler(0f, 0f, -speed * _t);
                    break;
                case Mode.Grow:
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_t - delay) / Mathf.Max(0.01f, seconds)));
                    transform.localScale = new Vector3(_scale.x, _scale.y * Mathf.Max(0.001f, k), _scale.z);
                    break;
                case Mode.Appear:
                    // Pops in on its cue word with a small overshoot, then stays.
                    float a = Mathf.Clamp01((_t - delay) / Mathf.Max(0.01f, seconds));
                    transform.localScale = _scale * Mathf.Max(0.001f, a * (1f + 0.18f * Mathf.Sin(a * Mathf.PI)));
                    break;
                case Mode.Path:
                case Mode.Steps:
                    if (path.Length < 2) return;
                    float cycle = Mathf.Max(0.01f, seconds) + rest;
                    float u = Mathf.Clamp01(Mathf.Repeat(Mathf.Max(0f, _t - delay), cycle) / Mathf.Max(0.01f, seconds));
                    if (mode == Mode.Steps)
                    {
                        // Step, settle, step: "nudge every weight a little bit downhill, repeat".
                        float scaled = u * steps;
                        float whole = Mathf.Floor(Mathf.Min(scaled, steps - 0.0001f));
                        float part = Mathf.Clamp01((scaled - whole) * 2f);
                        u = (whole + Mathf.SmoothStep(0f, 1f, part)) / steps;
                    }
                    else
                    {
                        u = u * u; // rolls faster as it goes downhill
                    }

                    transform.localPosition = At(u * _total);
                    break;
            }
        }

        private Vector3 At(float distance)
        {
            for (int i = 1; i < path.Length; i++)
            {
                if (distance > _lengths[i] && i < path.Length - 1) continue;
                float span = Mathf.Max(1e-5f, _lengths[i] - _lengths[i - 1]);
                return Vector3.Lerp(path[i - 1], path[i], Mathf.Clamp01((distance - _lengths[i - 1]) / span));
            }

            return path[path.Length - 1];
        }
    }
}
