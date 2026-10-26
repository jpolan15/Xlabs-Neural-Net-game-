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
    /// whenever GO is live, so pressing A to go just works. Letting go of the trigger over a socket plugs in a string
    /// being dragged. It turns input into commands and decides nothing (ADR-007); grip and finger pokes still work
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
        }

        private Hand[] _hands;
        private PodLeverInteractable _go;

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
            bool hit = Physics.Raycast(ray, out RaycastHit info, reach, ~0, QueryTriggerInteraction.Collide);
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
            if (pressed)
            {
                h.FlashUntil = Time.unscaledTime + 0.09f;
                bool did = h.Aimed != null && RideTargeting.Fire(h.Aimed, h.Index);
                if (!did) did = PullGoIfLive();
                RideAim.RaiseFired(h.Index, did);
                Haptic(h.Index, did ? fireHaptic : missHaptic, did ? 0.06f : 0.02f);
            }

            if (h.Trigger.WasReleasedThisFrame() || h.Fire.WasReleasedThisFrame())
            {
                if (h.Aimed != null && RideTargeting.Release(h.Aimed, h.Index)) Haptic(h.Index, fireHaptic, 0.06f);
                RideAim.RaiseReleased(h.Index);
            }
        }

        private bool PullGoIfLive()
        {
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
