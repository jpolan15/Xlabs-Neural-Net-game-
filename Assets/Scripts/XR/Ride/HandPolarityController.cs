using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.XR.Ride
{
    /// <summary>
    /// One per controller. It gives the hand its colour and its fingertip. The right hand is positive, green (#2ECC71);
    /// the left is negative, red (#E74C3C); the hand lasers and the glowing orb on the fingertip wear it, so the rider
    /// sees the rule (right = +, left = −) without reading anything. The orb shows while a grid game (draw the 3, paint
    /// the weights) is live. The fingertip is also a small trigger on a kinematic body, so a finger that reaches a grid
    /// cell presses it with this hand, the same press a laser shot or an XRI poke makes (a cell ignores a second press
    /// within a moment, so a poke and a touch together count once). It turns reach into commands and decides nothing.
    /// </summary>
    public sealed class HandPolarityController : MonoBehaviour, IHandTip
    {
        /// <summary>#2ECC71: the right hand, +1.</summary>
        public static readonly Color Positive = new Color(0.180f, 0.800f, 0.443f, 0.9f);

        /// <summary>#E74C3C: the left hand, −1.</summary>
        public static readonly Color Negative = new Color(0.906f, 0.298f, 0.235f, 0.9f);

        [SerializeField] private int hand = RideHand.Right;
        [SerializeField] private Material orbMaterial;
        [Tooltip("How far ahead of the controller the fingertip is, metres.")]
        [SerializeField] private float tipForward = 0.09f;
        [SerializeField] private float tipRadius = 0.012f;
        [SerializeField] private float orbSize = 0.024f;
        [SerializeField] private float touchHaptic = 0.35f;

        private Transform _tip;
        private Renderer _orb;
        private MaterialPropertyBlock _block;
        private RideDirector _director;
        private float _findAt;
        private float _flashUntil;

        public int Hand => hand;

        public Vector3 Position => _tip != null ? _tip.position : transform.position;

        /// <summary>The colour of a hand: green for the right, red for the left.</summary>
        public static Color ColorOf(int hand) => hand == RideHand.Left ? Negative : Positive;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _tip = new GameObject("Fingertip").transform;
            _tip.SetParent(transform, false);
            _tip.localPosition = new Vector3(0f, 0f, tipForward);

            var sphere = _tip.gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = tipRadius;
            var body = _tip.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _tip.gameObject.AddComponent<Touch>().Owner = this;

            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "Orb";
            Destroy(orb.GetComponent<Collider>());
            orb.transform.SetParent(_tip, false);
            orb.transform.localScale = Vector3.one * orbSize;
            _orb = orb.GetComponent<Renderer>();
            _orb.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _orb.receiveShadows = false;
            if (orbMaterial != null) _orb.sharedMaterial = orbMaterial;
            _orb.enabled = false;
        }

        private void Update()
        {
            if (_orb == null) return;
            if (_director == null && Time.unscaledTime >= _findAt)
            {
                _findAt = Time.unscaledTime + 1f;
                _director = FindAnyObjectByType<RideDirector>();
            }

            bool grid = _director != null && (_director.ActiveMoment is WeightPaintMoment || _director.ActiveMoment is DrawMoment);
            bool tracked = transform.localPosition != Vector3.zero;
            _orb.enabled = grid && tracked;
            if (!_orb.enabled) return;

            float flash = Time.unscaledTime < _flashUntil ? 1.8f : 1f;
            float pulse = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 6f);
            _orb.transform.localScale = Vector3.one * (orbSize * flash * pulse);
            Color c = ColorOf(hand);
            _orb.GetPropertyBlock(_block);
            _block.SetColor("_BaseColor", c);
            _block.SetColor("_Color", c);
            _orb.SetPropertyBlock(_block);
        }

        private void Reached(Collider other)
        {
            WeightCellInteractable cell = other.GetComponent<WeightCellInteractable>();
            if (cell == null || !cell.isActiveAndEnabled) return;
            if (!cell.Press(hand)) return;
            _flashUntil = Time.unscaledTime + 0.12f;
            RideAim.Buzz(hand, touchHaptic, 0.04f);
        }

        /// <summary>Lives on the fingertip body, where Unity sends the trigger events, and passes them up.</summary>
        private sealed class Touch : MonoBehaviour
        {
            public HandPolarityController Owner;

            private void OnTriggerEnter(Collider other)
            {
                if (Owner != null) Owner.Reached(other);
            }
        }
    }
}
