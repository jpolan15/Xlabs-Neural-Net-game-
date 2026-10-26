using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The game table: every mini-game is played on it, so the rider always knows where to look. It rises out of the
    /// dash to tablet height (its board about 34 degrees under the eye, ADR-019) when a game opens, glows its frame while the game is live,
    /// and sinks back into the dash when the rider pulls GO. Between games it is stowed and hidden, so it never blocks
    /// the view of the world. The scene keeps it at its raised pose; this only animates it. Display only.
    /// </summary>
    public sealed class GameTableView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private Transform table;
        [SerializeField] private Renderer frame;
        [SerializeField] private Material frameLive;
        [SerializeField] private Material frameIdle;
        [Tooltip("Where the table hides, relative to its raised pose, in the pod's frame.")]
        [SerializeField] private Vector3 stowedOffset = new Vector3(0f, -0.32f, 0.08f);
        [SerializeField] private float riseSeconds = 0.55f;
        [Tooltip("The table also comes up for these segments even without a game (the hot-wire hunt shows its timer).")]
        [SerializeField] private int[] alsoUpAt = new int[0];

        private Vector3 _raised;
        private Quaternion _raisedRotation;
        private float _raisedScale = 1f;
        private float _t;
        private bool _wasUp;

        private void Awake()
        {
            if (table == null) return;
            _raised = table.localPosition;
            _raisedRotation = table.localRotation;
            _raisedScale = table.localScale.x;
        }

        private void Start()
        {
            if (table == null) return;
            _t = 0f;
            Place();
            table.gameObject.SetActive(false);
        }

        private bool WantUp()
        {
            if (director == null) return false;
            if (director.ActiveMoment is RideGame || director.WaitingForGo) return true;
            return System.Array.IndexOf(alsoUpAt, director.SegmentIndex) >= 0 && director.State == RideState.AtStop;
        }

        private void Update()
        {
            if (table == null) return;
            bool up = WantUp();
            if (up && !table.gameObject.activeSelf) table.gameObject.SetActive(true);
            _t = Mathf.MoveTowards(_t, up ? 1f : 0f, Time.deltaTime / riseSeconds);
            Place();
            if (!up && _t <= 0f && table.gameObject.activeSelf) table.gameObject.SetActive(false);

            bool live = up && director.ActiveMoment is RideGame game && game.IsPlaying;
            if (frame != null && frameLive != null && frameIdle != null) frame.sharedMaterial = live ? frameLive : frameIdle;
            _wasUp = up;
        }

        private void Place()
        {
            // Ease out with a small overshoot on the way up; plain ease on the way down.
            float e = _wasUp ? 1f + 2.2f * Mathf.Pow(_t - 1f, 3f) + 1.2f * Mathf.Pow(_t - 1f, 2f) : _t * _t * (3f - 2f * _t);
            table.localPosition = _raised + stowedOffset * (1f - e);
            table.localRotation = _raisedRotation * Quaternion.Euler(-25f * (1f - e), 0f, 0f);
            table.localScale = Vector3.one * (_raisedScale * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(e)));
        }
    }
}
