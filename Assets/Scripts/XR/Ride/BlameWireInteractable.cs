using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.XR.Ride
{
    /// <summary>
    /// One of the hottest wires in the rewind, and the glowing handle on its loose end: colliders, no renderer. A laser
    /// press (or a mouse click on the desk) on it picks the wire up with that hand (<see cref="BlameMoment.Grab"/>); the
    /// moment then follows the laser and decides when the wire has reached the 3. An XRI select does the same, and
    /// letting go of the select drops the wire.
    /// </summary>
    public sealed class BlameWireInteractable : XRSimpleInteractable
    {
        [SerializeField] private int wire;
        [SerializeField] private BlameMoment moment;
        [SerializeField] private float haptic = 0.5f;

        public int Wire => wire;

        /// <summary>Sets the rank. Used by the scene builder.</summary>
        public void SetWire(int rank) => wire = rank;

        /// <summary>A press from a hand (laser, mouse): picks the wire up. Returns true when it did.</summary>
        public bool Grab(int hand) => moment != null && moment.Grab(wire, hand);

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            bool left = args.interactorObject is Component c && WeightCellInteractable.IsLeft(c.transform);
            if (Grab(left ? RideHand.Left : RideHand.Right) && args.interactorObject is XRBaseInputInteractor input)
            {
                input.SendHapticImpulse(haptic, 0.08f);
            }
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            bool left = args.interactorObject is Component c && WeightCellInteractable.IsLeft(c.transform);
            if (moment != null) moment.Drop(left ? RideHand.Left : RideHand.Right);
        }
    }
}
