using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Convergence.XR.Ride
{
    /// <summary>
    /// One of the brightest-blamed wires in the rewind: capsule colliders along it, no renderer. Pointing at it with a
    /// ray (or clicking it on the desk) raises <see cref="Pointed"/> with the wire's rank; the scene wires that to the
    /// blame moment's Nudge, which applies the real gradient step.
    /// </summary>
    public sealed class BlameWireInteractable : XRSimpleInteractable
    {
        [SerializeField] private int wire;
        [SerializeField] private float haptic = 0.5f;
        [SerializeField] private UnityEvent<int> pointed = new UnityEvent<int>();

        public int Wire => wire;

        /// <summary>Raised with the wire's rank when the rider selects it.</summary>
        public UnityEvent<int> Pointed => pointed;

        /// <summary>Sets the rank. Used by the scene builder.</summary>
        public void SetWire(int rank) => wire = rank;

        /// <summary>A select from any source (ray, mouse).</summary>
        public void Point() => pointed.Invoke(wire);

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            Point();
            if (args.interactorObject is XRBaseInputInteractor input) input.SendHapticImpulse(haptic, 0.08f);
        }
    }
}
