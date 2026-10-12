using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Region 0, the Page: a sheet with a real handwritten 3 from the test set. When GO is pulled the ink lifts off the
    /// page toward the rider and turns into its 28×28 pixels, and the page fades as the pod dives through it into the
    /// picture. Display only.
    /// </summary>
    public sealed class PageView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private Renderer[] paper = new Renderer[0];
        [SerializeField] private Renderer ink;
        [SerializeField] private Material inkSmooth;
        [SerializeField] private Material inkPixels;
        [SerializeField] private float liftMeters = 2.5f;
        [SerializeField] private float liftSeconds = 2.5f;
        [SerializeField] private float pixelateAt = 0.9f;
        [Tooltip("The page is fully there farther than this from the eye, and gone at Gone Within.")]
        [SerializeField] private float fadeFrom = 7f;
        [SerializeField] private float goneWithin = 1.6f;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _block;
        private Vector3 _inkHome;
        private float _liftedAt = -1f;
        private Color[] _paperColors;
        private Color _inkColor;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _paperColors = new Color[paper.Length];
            for (int i = 0; i < paper.Length; i++) _paperColors[i] = paper[i].sharedMaterial.GetColor(BaseColor);
            if (ink != null)
            {
                _inkHome = ink.transform.localPosition;
                _inkColor = inkSmooth.GetColor(BaseColor);
            }
        }

        private void OnEnable()
        {
            if (director != null) director.SegmentStarted += OnSegment;
        }

        private void OnDisable()
        {
            if (director != null) director.SegmentStarted -= OnSegment;
        }

        private void OnSegment(int index)
        {
            if (index == 0 && _liftedAt < 0f) _liftedAt = Time.time;
        }

        private void Update()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            float distance = Mathf.Abs(transform.InverseTransformPoint(cam.transform.position).z);
            float alpha = Mathf.Clamp01((distance - goneWithin) / Mathf.Max(0.1f, fadeFrom - goneWithin));
            for (int i = 0; i < paper.Length; i++) SetAlpha(paper[i], _paperColors[i], alpha);

            if (ink == null) return;
            float lift = _liftedAt < 0f ? 0f : Mathf.Clamp01((Time.time - _liftedAt) / liftSeconds);
            float e = lift * lift * (3f - 2f * lift);
            ink.transform.localPosition = _inkHome + Vector3.back * (liftMeters * e);
            ink.transform.localScale = Vector3.one * (1f + 0.15f * e);
            ink.sharedMaterial = lift >= pixelateAt ? inkPixels : inkSmooth;
            SetAlpha(ink, _inkColor, alpha);
        }

        private void SetAlpha(Renderer r, Color baseColor, float alpha)
        {
            baseColor.a *= alpha;
            r.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, baseColor);
            r.SetPropertyBlock(_block);
            r.enabled = alpha > 0.01f;
        }
    }
}
