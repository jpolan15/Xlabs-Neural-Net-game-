using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Blast the 3s, seen (v7): every flying digit is one picture quad in a single batch (one draw for the whole pool),
    /// turned to face the eye; while the digits fly, each shot throws a bright bolt down the hand's laser; a blasted 3
    /// bursts into its pixels (gold and white), a wrong digit into red ones. Display only: it reads ZapGame and RideAim.
    /// </summary>
    public sealed class BlasterView : MonoBehaviour
    {
        [SerializeField] private ZapGame zap;
        [SerializeField] private ShapeBatch pictures;
        [Tooltip("The atlas cell (uv rect) of each picture, in ZapGame's picture order.")]
        [SerializeField] private Rect[] cells = new Rect[0];
        [SerializeField] private float pictureSize = 0.72f;
        [SerializeField] private LineRenderer[] bolts = new LineRenderer[2];
        [SerializeField] private float boltSeconds = 0.1f;
        [SerializeField] private float boltLength = 1.2f;
        [SerializeField] private ParticleSystem burst;
        [SerializeField] private int burstCount = 40;
        [SerializeField] private Color right = new Color(1f, 0.82f, 0.4f);
        [SerializeField] private Color rightHot = Color.white;
        [SerializeField] private Color wrong = new Color(1f, 0.3f, 0.26f);

        private Renderer _picturesRenderer;
        private Renderer _burstRenderer;
        private readonly float[] _boltAt = { -10f, -10f };
        private readonly Vector3[] _boltFrom = new Vector3[2];
        private readonly Vector3[] _boltTo = new Vector3[2];
        private Camera _eye;

        private void OnEnable()
        {
            RideAim.Fired += OnFired;
            if (zap != null) zap.Shot += OnShot;
        }

        private void OnDisable()
        {
            RideAim.Fired -= OnFired;
            if (zap != null) zap.Shot -= OnShot;
        }

        private void Start()
        {
            if (pictures != null) _picturesRenderer = pictures.GetComponent<Renderer>();
            if (burst != null) _burstRenderer = burst.GetComponent<Renderer>();
            foreach (LineRenderer b in bolts)
            {
                if (b != null) b.enabled = false;
            }
        }

        private void OnFired(int hand, bool hit)
        {
            if (zap == null || !zap.IsRunning || hand < 0 || hand >= bolts.Length) return;
            if (!RideAim.TryGet(hand, out Vector3 origin, out Vector3 point)) return;
            _boltAt[hand] = Time.time;
            _boltFrom[hand] = origin;
            _boltTo[hand] = point;
        }

        private void OnShot(Vector3 where, bool isRight, int points)
        {
            if (burst == null) return;
            if (_burstRenderer != null) _burstRenderer.enabled = true;
            var p = new ParticleSystem.EmitParams { applyShapeToPosition = true, position = where };
            for (int i = 0; i < burstCount; i++)
            {
                p.startColor = isRight ? (i % 3 == 0 ? rightHot : right) : wrong;
                burst.Emit(p, 1);
            }
        }

        private void LateUpdate()
        {
            DrawPictures();
            for (int h = 0; h < bolts.Length; h++) DrawBolt(h);
            if (_burstRenderer != null && _burstRenderer.enabled && burst.particleCount == 0) _burstRenderer.enabled = false;
        }

        private void DrawPictures()
        {
            if (pictures == null || zap == null) return;
            if (_eye == null) _eye = Camera.main;
            var targets = zap.Targets;
            Transform space = pictures.transform;
            bool any = false;
            for (int i = 0; i < pictures.Count; i++)
            {
                Shape s = pictures[i];
                ZapSpeck t = i < targets.Count ? targets[i] : null;
                bool show = t != null && t.gameObject.activeInHierarchy && t.Picture >= 0 && t.Picture < cells.Length;
                s.alpha = show ? 1f : 0f;
                if (!show) continue;
                any = true;
                Vector3 at = t.transform.position;
                Vector3 look = _eye != null ? at - _eye.transform.position : space.forward;
                s.position = space.InverseTransformPoint(at);
                s.rotation = Quaternion.Inverse(space.rotation) * Quaternion.LookRotation(look.sqrMagnitude > 1e-6f ? look : space.forward, Vector3.up);
                s.size = Vector2.one * pictureSize * t.transform.localScale.x;
                s.uv = cells[t.Picture];
            }

            pictures.MarkDirty();
            if (_picturesRenderer != null) _picturesRenderer.enabled = any;
        }

        private void DrawBolt(int hand)
        {
            LineRenderer b = bolts[hand];
            if (b == null) return;
            float t = (Time.time - _boltAt[hand]) / boltSeconds;
            if (t < 0f || t > 1f)
            {
                b.enabled = false;
                return;
            }

            // A short bright slug that races from the hand to the aim point.
            Vector3 from = _boltFrom[hand], to = _boltTo[hand];
            float length = Vector3.Distance(from, to);
            float head = Mathf.Min(length, length * t * 1.3f);
            float tail = Mathf.Max(0f, head - boltLength);
            Vector3 dir = length > 1e-4f ? (to - from) / length : Vector3.forward;
            b.enabled = true;
            b.SetPosition(0, from + dir * tail);
            b.SetPosition(1, from + dir * head);
            float fade = 1f - t * 0.6f;
            b.widthMultiplier = fade;
        }
    }
}
