using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The card with the rider's digit (v6) on the table at the network stop: the 49 numbers as a 7×7 of lit tiles. It
    /// pulses while <see cref="ForwardPassMoment"/> waits for the rider to shoot it, then flies out toward the network
    /// and fades as the light races in. One tile mesh, 49 faces. Display only.
    /// </summary>
    public sealed class SendCardView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private Level1Session session;
        [SerializeField] private ForwardPassMoment moment;
        [SerializeField] private GameObject board;
        [SerializeField] private int showSegment = -1;
        [SerializeField] private Transform card;
        [SerializeField] private MeshFilter tiles;
        [SerializeField] private float flySeconds = 0.7f;
        [Tooltip("Where the card flies, in the pod's frame (out over the rim, toward the network).")]
        [SerializeField] private Vector3 flyTo = new Vector3(0f, 1.6f, 4f);
        [SerializeField] private Color dark = new Color(0.04f, 0.06f, 0.12f, 0.9f);
        [SerializeField] private Color lit = new Color(0.55f, 0.95f, 1f, 1f);

        private Mesh _mesh;
        private Color[] _colors;
        private Vector3 _home;
        private Vector3 _homeScale;
        private float _thrownAt = -1f;

        private void Awake()
        {
            if (tiles != null)
            {
                _mesh = Instantiate(tiles.sharedMesh);
                tiles.sharedMesh = _mesh;
                _colors = _mesh.colors;
            }

            if (card != null)
            {
                _home = card.localPosition;
                _homeScale = card.localScale;
            }
        }

        private void OnEnable()
        {
            if (director != null)
            {
                director.SegmentStarted += OnSegment;
                director.SegmentEnded += OnSegmentEnded;
            }

            if (moment != null) moment.Thrown += OnThrown;
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.SegmentStarted -= OnSegment;
                director.SegmentEnded -= OnSegmentEnded;
            }

            if (moment != null) moment.Thrown -= OnThrown;
        }

        private void Start()
        {
            if (board != null) board.SetActive(false);
        }

        private void OnSegment(int index)
        {
            if (index != showSegment || board == null) return;
            board.SetActive(true);
            _thrownAt = -1f;
            if (card != null)
            {
                card.localPosition = _home;
                card.localScale = _homeScale;
                card.gameObject.SetActive(true);
            }

            Paint();
        }

        private void OnSegmentEnded(int index)
        {
            if (index == showSegment && board != null) board.SetActive(false);
        }

        private void OnThrown(bool byRider) => _thrownAt = Time.time;

        private void Paint()
        {
            if (_mesh == null || session == null) return;
            double[] d = session.Digit;
            for (int i = 0; i < 49 && i * 4 + 3 < _colors.Length; i++)
            {
                Color c = Color.Lerp(dark, lit, Mathf.Clamp01((float)d[i]));
                _colors[i * 4] = _colors[i * 4 + 1] = _colors[i * 4 + 2] = _colors[i * 4 + 3] = c;
            }

            _mesh.colors = _colors;
        }

        private void Update()
        {
            if (board == null || !board.activeSelf || card == null) return;
            if (_thrownAt < 0f)
            {
                float pulse = moment != null && moment.IsLive ? 1f + 0.06f * Mathf.Sin(Time.time * 6f) : 1f;
                card.localScale = _homeScale * pulse;
                return;
            }

            float t = Mathf.Clamp01((Time.time - _thrownAt) / flySeconds);
            float e = t * t;
            // The card leaves the table toward the network: from its home on the board to a point out in the pod's frame.
            Vector3 target = card.parent != null ? card.parent.InverseTransformPoint(director.Pod.TransformPoint(flyTo)) : flyTo;
            card.localPosition = Vector3.Lerp(_home, target, e);
            card.localScale = _homeScale * Mathf.Lerp(1f, 0.2f, e);
            if (t >= 1f) card.gameObject.SetActive(false);
        }
    }
}
