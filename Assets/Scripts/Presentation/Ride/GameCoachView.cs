using UnityEngine;
using Convergence.Core.Puzzles;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The coach (owner, 2026-10-27: "make the games intuitive with little hints"): a gold arrow on the game table that
    /// bobs at the one thing to aim at next: the drawing box, then DONE once there is enough ink; the weight cell Core's
    /// hint names (else the grid); the card to send; the step-size buttons; and in the hot-wire hunt, up at the wires.
    /// It hides while a trigger is held and for a moment after a press that did something, so it never sits on what
    /// the rider is doing, and comes back when they stall. Display only: it reads the games and decides nothing.
    /// </summary>
    public sealed class GameCoachView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private Transform table;
        [Tooltip("The chevron, built pointing down (-y) with its tip at its origin.")]
        [SerializeField] private Transform arrow;
        [SerializeField] private DrawMoment draw;
        [SerializeField] private StumpMoment stump;
        [SerializeField] private WeightPaintMoment paint;
        [SerializeField] private ForwardPassMoment send;
        [SerializeField] private TrainMoment train;
        [SerializeField] private BlameMoment blame;

        [Header("Targets (their centres; the arrow stands off by the offsets, in table space)")]
        [SerializeField] private Transform canvas;
        [SerializeField] private float canvasOffset = 0.105f;
        [SerializeField] private Transform done;
        [SerializeField] private float buttonOffset = 0.06f;
        [SerializeField] private Transform grid;
        [SerializeField] private float gridOffset = 0.25f;
        [SerializeField] private Transform[] cells = new Transform[0];
        [SerializeField] private float cellOffset = 0.05f;
        [SerializeField] private Transform card;
        [SerializeField] private float cardOffset = 0.16f;
        [Tooltip("The middle step button: the arrow comes in from its left, pointing right.")]
        [SerializeField] private Transform steps;
        [SerializeField] private float stepsOffset = 0.08f;
        [Tooltip("Where the arrow points up from in the hot-wire hunt, table space.")]
        [SerializeField] private Vector3 upFrom = new Vector3(0f, 0.3f, -0.035f);

        [SerializeField] private float frontOffset = -0.035f;
        [SerializeField] private float bob = 0.012f;
        [SerializeField] private float quietAfterAction = 2f;
        [SerializeField] private float quietAfterRelease = 0.8f;

        private readonly bool[] _held = new bool[2];
        private float _quietUntil;

        private void OnEnable()
        {
            RideAim.Fired += OnFired;
            RideAim.Released += OnReleased;
        }

        private void OnDisable()
        {
            RideAim.Fired -= OnFired;
            RideAim.Released -= OnReleased;
            _held[0] = _held[1] = false;
        }

        private void Start()
        {
            if (arrow != null) arrow.gameObject.SetActive(false);
        }

        private void OnFired(int hand, bool did)
        {
            if (hand == RideHand.Left || hand == RideHand.Right) _held[hand] = true;
            if (did) _quietUntil = Mathf.Max(_quietUntil, Time.time + quietAfterAction);
        }

        private void OnReleased(int hand)
        {
            if (hand == RideHand.Left || hand == RideHand.Right) _held[hand] = false;
            _quietUntil = Mathf.Max(_quietUntil, Time.time + quietAfterRelease);
        }

        private void Update()
        {
            if (arrow == null || table == null) return;
            bool wanted = Pick(out Vector3 at, out float turn);
            bool show = wanted && !_held[0] && !_held[1] && Time.time >= _quietUntil;
            if (arrow.gameObject.activeSelf != show) arrow.gameObject.SetActive(show);
            if (!show) return;

            // Bob toward the target and back.
            Quaternion rotation = Quaternion.Euler(0f, 0f, turn);
            Vector3 pointing = rotation * Vector3.down;
            float b = (0.5f + 0.5f * Mathf.Sin(Time.time * 6f)) * bob;
            arrow.localRotation = rotation;
            arrow.localPosition = at - pointing * b;
        }

        /// <summary>Where the arrow's tip goes in table space and how it turns (0: pointing down, 90: right, 180: up).</summary>
        private bool Pick(out Vector3 at, out float turn)
        {
            at = Vector3.zero;
            turn = 0f;
            if (director == null || !table.gameObject.activeInHierarchy) return false;
            RideMoment m = director.ActiveMoment;
            if (m == null) return false;
            if (draw != null && m == draw && draw.IsLive) return OnCanvas(draw.Canvas, out at);
            if (stump != null && m == stump && stump.IsLive) return OnCanvas(stump.Canvas, out at);
            if (paint != null && m == paint && paint.IsLive)
            {
                int c = paint.HintCell;
                return c >= 0 && c < cells.Length ? Above(cells[c], cellOffset, out at) : Above(grid, gridOffset, out at);
            }

            if (send != null && m == send && send.IsLive) return Above(card, cardOffset, out at);
            if (train != null && m == train && train.IsLive)
            {
                turn = 90f;
                if (steps == null) return false;
                at = Local(steps) + new Vector3(-stepsOffset, 0f, 0f);
                return true;
            }

            if (blame != null && m == blame && blame.IsLive)
            {
                turn = 180f;
                at = upFrom;
                return true;
            }

            return false;
        }

        private bool OnCanvas(DigitCanvas c, out Vector3 at)
        {
            bool ready = c != null && c.HasEnoughInk;
            return ready ? Above(done, buttonOffset, out at) : Above(canvas, canvasOffset, out at);
        }

        private bool Above(Transform target, float offset, out Vector3 at)
        {
            at = target != null ? Local(target) + new Vector3(0f, offset, 0f) : Vector3.zero;
            return target != null;
        }

        private Vector3 Local(Transform target)
        {
            Vector3 p = table.InverseTransformPoint(target.position);
            p.z = frontOffset;
            return p;
        }
    }
}
