using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.XR.Ride
{
    /// <summary>
    /// One of the hottest wires in the backprop hunt ("cool the hot wires"), and the big orb on it: colliders, no renderer.
    /// A laser shot (trigger or A, or a mouse click on the desk) on it is <see cref="BlameMoment.Shoot"/> with that hand;
    /// the moment decides what the shot does. An XRI select does the same. Once its wire is cooled its colliders switch
    /// off, so a cooled wire never stops a shot meant for a hot one behind it.
    /// </summary>
    public sealed class BlameWireInteractable : XRSimpleInteractable
    {
        [SerializeField] private int wire;
        [SerializeField] private BlameMoment moment;
        [SerializeField] private float haptic = 0.5f;

        private Collider[] _own;

        public int Wire => wire;

        /// <summary>Sets the rank. Used by the scene builder.</summary>
        public void SetWire(int rank) => wire = rank;

        /// <summary>A shot from a hand (laser, mouse): cools the wire. Returns true when it did.</summary>
        public bool Shoot(int hand) => moment != null && moment.Shoot(wire, hand);

        protected override void OnEnable()
        {
            base.OnEnable();
            if (moment == null) return;
            moment.Nudged += OnNudged;
            moment.BlameChanged += Refresh;
            Refresh();
        }

        protected override void OnDisable()
        {
            if (moment != null)
            {
                moment.Nudged -= OnNudged;
                moment.BlameChanged -= Refresh;
            }

            base.OnDisable();
        }

        private void OnNudged(int k, double before, double after)
        {
            if (k == wire) Refresh();
        }

        private void Refresh()
        {
            if (_own == null) _own = GetComponentsInChildren<Collider>(true);
            bool hot = moment == null || !moment.IsNudged(wire);
            foreach (Collider c in _own) if (c != null) c.enabled = hot;
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            bool left = args.interactorObject is Component c && WeightCellInteractable.IsLeft(c.transform);
            if (Shoot(left ? RideHand.Left : RideHand.Right) && args.interactorObject is XRBaseInputInteractor input)
            {
                input.SendHapticImpulse(haptic, 0.08f);
            }
        }
    }
}
