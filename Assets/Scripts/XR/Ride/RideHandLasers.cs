using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using Convergence.Gameplay.Ride;

namespace Convergence.XR.Ride
{
    /// <summary>
    /// A laser on each hand, always on: the right one green, the left one red, the colours of <see cref="HandPolarityController"/>.
    /// The trigger, or A / X (or B / Y), fires the hand's laser at whatever it points at: a noise speck, a grid cell, a
    /// string or socket, a step button, a hot wire, the GO lever, Pixel. With nothing in its sights, a press pulls GO
    /// whenever GO is live, so pressing A to go just works. Held on a stroke surface (v6) it draws on the canvas or
    /// sprays the weight grid cell by cell as it sweeps. Letting go of the trigger drops a carried wire. The beam passes
    /// through both controllers' own colliders, and a press that just misses a flying digit, a button or a hot wire still
    /// fires it (aim help). It turns input into commands and decides nothing (ADR-007); grip and finger pokes still work
    /// through XRI as before.
    /// </summary>
    public sealed class RideHandLasers : MonoBehaviour
    {
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Material laserMaterial;
        [SerializeField] private Color leftColor = HandPolarityController.Negative;
        [SerializeField] private Color rightColor = HandPolarityController.Positive;
        [SerializeField] private float reach = 80f;
        [SerializeField] private float idleLength = 6f;
        [SerializeField] private float width = 0.006f;
        [SerializeField] private float fireHaptic = 0.45f;
        [SerializeField] private float missHaptic = 0.1f;
        [SerializeField] private float strokeHaptic = 0.08f;
        [Tooltip("A press that misses still fires a shootable thing this many degrees off the beam (aim help on moving digits).")]
        [SerializeField] private float assistDegrees = 5f;
        [Tooltip("How wide the aim help looks around the beam, metres (caps the degrees far away).")]
        [SerializeField] private float assistRadius = 1.2f;

        private readonly RaycastHit[] _rayHits = new RaycastHit[32];
        private readonly RaycastHit[] _assistHits = new RaycastHit[64];
        private readonly Collider[] _nearHits = new Collider[32];
        private readonly System.Collections.Generic.List<Collider> _candidates = new System.Collections.Generic.List<Collider>(96);

        private sealed class Hand
        {
            public int Index;
            public Transform Pose;
            public LineRenderer Line;
            public Transform Dot;
            public InputAction Fire;
            public InputAction Trigger;
            public float FlashUntil;
            public Collider Aimed;
            public Vector3 AimPoint;
            public Color Color;
            public IRideStrokeTarget Stroking;
            public float NextTick;
        }

        private Hand[] _hands;
        private PodLeverInteractable _go;
        private RideDirector _director;

        private void Awake()
        {
            _hands = new[]
            {
                MakeHand(RideHand.Left, leftHand, "LeftHand", leftColor),
                MakeHand(RideHand.Right, rightHand, "RightHand", rightColor),
            };
        }

        private Hand MakeHand(int index, Transform pose, string usage, Color color)
        {
            var hand = new Hand { Index = index, Pose = pose, Color = color };
            hand.Trigger = new InputAction("Trigger" + usage, InputActionType.Button);
            hand.Trigger.AddBinding("<XRController>{" + usage + "}/{TriggerButton}");
            hand.Trigger.AddBinding("<XRController>{" + usage + "}/triggerPressed");
            hand.Fire = new InputAction("Buttons" + usage, InputActionType.Button);
            hand.Fire.AddBinding("<XRController>{" + usage + "}/{PrimaryButton}");
            hand.Fire.AddBinding("<XRController>{" + usage + "}/primaryButton");
            hand.Fire.AddBinding("<XRController>{" + usage + "}/{SecondaryButton}");
            hand.Fire.AddBinding("<XRController>{" + usage + "}/secondaryButton");

            var line = new GameObject("Laser" + usage).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.sharedMaterial = laserMaterial;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0.15f);
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            hand.Line = line;

            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(dot.GetComponent<Collider>());
            dot.name = "LaserDot" + usage;
            dot.transform.SetParent(transform, false);
            dot.transform.localScale = Vector3.one * 0.025f;
            var r = dot.GetComponent<Renderer>();
            r.sharedMaterial = laserMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            r.SetPropertyBlock(block);
            hand.Dot = dot.transform;
            return hand;
        }

        private void OnEnable()
        {
            if (_hands == null) return;
            RideAim.Buzzed += OnBuzzed;
            foreach (Hand h in _hands)
            {
                h.Trigger.Enable();
                h.Fire.Enable();
            }
        }

        private void OnDisable()
        {
            if (_hands == null) return;
            RideAim.Buzzed -= OnBuzzed;
            foreach (Hand h in _hands)
            {
                h.Trigger.Disable();
                h.Fire.Disable();
                RideAim.Set(h.Index, Vector3.zero, Vector3.zero, false);
                EndStroke(h);
                RideAim.RaiseReleased(h.Index);
            }
        }

        private static void OnBuzzed(int hand, float amplitude, float seconds) => Haptic(hand, amplitude, seconds);

        private void OnDestroy()
        {
            if (_hands == null) return;
            foreach (Hand h in _hands)
            {
                h.Trigger.Dispose();
                h.Fire.Dispose();
            }
        }

        private void Update()
        {
            foreach (Hand h in _hands) UpdateHand(h);
        }

        private void UpdateHand(Hand h)
        {
            bool tracked = h.Pose != null && h.Pose.gameObject.activeInHierarchy && h.Pose.localPosition != Vector3.zero;
            h.Line.enabled = tracked;
            h.Dot.gameObject.SetActive(false);
            if (!tracked)
            {
                RideAim.Set(h.Index, Vector3.zero, Vector3.zero, false);
                return;
            }

            var ray = new Ray(h.Pose.position, h.Pose.forward);
            bool hit = CastPastHands(ray, out RaycastHit info);
            h.Aimed = hit ? info.collider : null;
            h.AimPoint = hit ? info.point : ray.GetPoint(idleLength);
            RideAim.Set(h.Index, ray.origin, h.AimPoint, true);

            float flash = Time.unscaledTime < h.FlashUntil ? 2.6f : 1f;
            h.Line.startWidth = width * flash;
            h.Line.endWidth = width * 0.4f * flash;
            h.Line.SetPosition(0, ray.origin);
            h.Line.SetPosition(1, h.AimPoint);
            if (hit)
            {
                h.Dot.gameObject.SetActive(true);
                h.Dot.position = info.point;
            }

            bool pressed = h.Trigger.WasPressedThisFrame() || h.Fire.WasPressedThisFrame();
            bool held = h.Trigger.IsPressed() || h.Fire.IsPressed();
            IRideStrokeTarget under = h.Aimed != null ? h.Aimed.GetComponentInParent<IRideStrokeTarget>() : null;
            if (pressed)
            {
                h.FlashUntil = Time.unscaledTime + 0.09f;
                bool did = false;
                if (under != null && under.StrokeBegin(h.Index, h.AimPoint, true))
                {
                    h.Stroking = under;
                    did = true;
                }
                else
                {
                    did = h.Aimed != null && RideTargeting.Fire(h.Aimed, h.Index);
                    if (!did) did = FireNearMiss(ray, h.Index);
                    if (!did) did = PullGoIfLive();
                }

                RideAim.RaiseFired(h.Index, did);
                Haptic(h.Index, did ? fireHaptic : missHaptic, did ? 0.06f : 0.02f);
            }
            else if (held)
            {
                // A held laser draws on the canvas and sprays the grid cell by cell as it sweeps.
                if (h.Stroking != null && under == h.Stroking)
                {
                    h.Stroking.StrokeMove(h.Index, h.AimPoint);
                }
                else
                {
                    EndStroke(h);
                    if (under != null && under.StrokeBegin(h.Index, h.AimPoint, false)) h.Stroking = under;
                }

                if (h.Stroking != null && Time.unscaledTime >= h.NextTick)
                {
                    h.NextTick = Time.unscaledTime + 0.06f;
                    Haptic(h.Index, strokeHaptic, 0.02f);
                }
            }

            if (h.Trigger.WasReleasedThisFrame() || h.Fire.WasReleasedThisFrame())
            {
                EndStroke(h);
                if (h.Aimed != null && RideTargeting.Release(h.Aimed, h.Index)) Haptic(h.Index, fireHaptic, 0.06f);
                RideAim.RaiseReleased(h.Index);
            }
        }

        /// <summary>
        /// The nearest thing on the ray that is not part of a hand. Each controller carries its own colliders (the fingertip
        /// trigger 9 cm ahead, XRI's interactors): a plain raycast from the controller stops on them, so the laser never
        /// reached anything (found on the Quest, 2026-10-27).
        /// </summary>
        private bool CastPastHands(Ray ray, out RaycastHit nearest)
        {
            nearest = default;
            int count = Physics.RaycastNonAlloc(ray, _rayHits, reach, ~0, QueryTriggerInteraction.Collide);
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                if (IsHand(_rayHits[i].collider) || (found && _rayHits[i].distance >= nearest.distance)) continue;
                nearest = _rayHits[i];
                found = true;
            }

            return found;
        }

        private bool IsHand(Collider c)
        {
            Transform t = c.transform;
            return (leftHand != null && t.IsChildOf(leftHand)) || (rightHand != null && t.IsChildOf(rightHand));
        }

        /// <summary>
        /// A press that just missed: fires the shootable thing (a flying digit, a button, a hot wire) closest to the ray
        /// within <see cref="assistDegrees"/>, so a shot that is almost on a moving 3 still counts.
        /// </summary>
        private bool FireNearMiss(Ray ray, int hand)
        {
            // The sweep misses what already overlaps the hand's sphere (the table's buttons), so those are added too.
            _candidates.Clear();
            int swept = Physics.SphereCastNonAlloc(ray, assistRadius, _assistHits, reach, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < swept; i++) _candidates.Add(_assistHits[i].collider);
            int near = Physics.OverlapSphereNonAlloc(ray.origin, assistRadius, _nearHits, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < near; i++) _candidates.Add(_nearHits[i]);

            for (int tries = 0; tries < 3; tries++)
            {
                int best = -1;
                float bestAngle = assistDegrees;
                for (int i = 0; i < _candidates.Count; i++)
                {
                    Collider c = _candidates[i];
                    if (c == null || IsHand(c) || !RideTargeting.IsShootable(c)) continue;
                    Vector3 to = c.bounds.center - ray.origin;
                    if (Vector3.Dot(to, ray.direction) <= 0f) continue;
                    float halfSize = Mathf.Atan2(c.bounds.extents.magnitude * 0.7f, Mathf.Max(0.05f, to.magnitude)) * Mathf.Rad2Deg;
                    float angle = Vector3.Angle(ray.direction, to) - halfSize;
                    if (angle >= bestAngle) continue;
                    bestAngle = angle;
                    best = i;
                }

                if (best < 0) return false;
                if (RideTargeting.Fire(_candidates[best], hand)) return true;
                _candidates[best] = null;
            }

            return false;
        }

        private static void EndStroke(Hand h)
        {
            if (h.Stroking == null) return;
            h.Stroking.StrokeEnd(h.Index);
            h.Stroking = null;
        }

        private bool PullGoIfLive()
        {
            // During the intro GO is a hold-to-skip handle: a stray press must not latch it and skip the intro.
            if (_director == null) _director = FindAnyObjectByType<RideDirector>();
            if (_director != null && _director.State == RideState.Intro) return false;
            if (_go == null) _go = RideTargeting.FindGo();
            if (_go == null || _go.Channel == null || _go.Channel.Locked || !_go.Channel.Visible) return false;
            if (_go.Channel.Value >= _go.Channel.Max) return false;
            _go.Tap();
            return true;
        }

        private static void Haptic(int hand, float amplitude, float duration)
        {
            XRController device = hand == RideHand.Left ? XRController.leftHand : XRController.rightHand;
            if (device == null) return;
            var command = UnityEngine.InputSystem.XR.Haptics.SendHapticImpulseCommand.Create(0, amplitude, duration);
            device.ExecuteCommand(ref command);
        }
    }

    /// <summary>What a laser shot (or a desk click) does to what it hit. Shared by the hand lasers and the desk seat.</summary>
    public static class RideTargeting
    {
        /// <summary>Fires at a collider with a hand. Returns true when the hit did something.</summary>
        public static bool Fire(Collider c, int hand)
        {
            IRideTarget target = c.GetComponentInParent<IRideTarget>();
            if (target != null && target.Hit(hand)) return true;

            WeightCellInteractable cell = c.GetComponent<WeightCellInteractable>();
            if (cell != null && cell.isActiveAndEnabled) return cell.Press(hand);

            BlameWireInteractable wire = c.GetComponentInParent<BlameWireInteractable>();
            if (wire != null && wire.isActiveAndEnabled) return wire.Grab(hand);

            PodLeverInteractable lever = c.GetComponentInParent<PodLeverInteractable>();
            if (lever != null && lever.isActiveAndEnabled && lever.Channel != null && !lever.Channel.Locked)
            {
                lever.Tap();
                return true;
            }

            IPettable pettable = c.GetComponentInParent<IPettable>();
            if (pettable != null)
            {
                pettable.Pet();
                return true;
            }

            return false;
        }

        /// <summary>True for what a near miss may still fire: a target (a flying digit, a button, the card) or a hot wire.</summary>
        public static bool IsShootable(Collider c)
        {
            if (!c.gameObject.activeInHierarchy) return false;
            if (c.GetComponentInParent<IRideTarget>() != null) return true;
            BlameWireInteractable wire = c.GetComponentInParent<BlameWireInteractable>();
            return wire != null && wire.isActiveAndEnabled;
        }

        /// <summary>The trigger let go over a collider (drop a dragged string on a socket).</summary>
        public static bool Release(Collider c, int hand)
        {
            IRideReleaseTarget target = c.GetComponentInParent<IRideReleaseTarget>();
            return target != null && target.Release(hand);
        }

        /// <summary>The dash's GO lever.</summary>
        public static PodLeverInteractable FindGo()
        {
            foreach (PodLeverInteractable lever in Object.FindObjectsByType<PodLeverInteractable>(FindObjectsInactive.Include))
            {
                if (lever.Channel != null && lever.Channel.Id == RideControlIds.Action) return lever;
            }

            return null;
        }
    }
}
