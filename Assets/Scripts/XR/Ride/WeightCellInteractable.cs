using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Convergence.XR.Ride
{
    /// <summary>
    /// One cell of the rider's 7×7 weight grid: a collider, no renderer (the grid is one mesh). A poke with a fingertip,
    /// a ray select, or a mouse click raises <see cref="Tapped"/> with the cell index; the scene wires that to the
    /// puzzle moment's Cycle, which decides what the tap does. A short pulse in the hand confirms it.
    /// </summary>
    public sealed class WeightCellInteractable : XRSimpleInteractable
    {
        [SerializeField] private int cell;
        [SerializeField] private float tapHaptic = 0.35f;
        [SerializeField] private float hoverHaptic = 0.08f;
        [Tooltip("Ignore a second select on the same cell within this many seconds (a poke can bounce).")]
        [SerializeField] private float debounceSeconds = 0.25f;
        [SerializeField] private UnityEvent<int> tapped = new UnityEvent<int>();

        private float _lastTap = -10f;

        public int Cell => cell;

        /// <summary>Raised with the cell index on a tap.</summary>
        public UnityEvent<int> Tapped => tapped;

        /// <summary>Sets the index. Used by the scene builder.</summary>
        public void SetCell(int index) => cell = index;

        /// <summary>A tap from any source (poke, ray, mouse).</summary>
        public void Tap()
        {
            if (Time.unscaledTime - _lastTap < debounceSeconds) return;
            _lastTap = Time.unscaledTime;
            tapped.Invoke(cell);
        }

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);
            if (args.interactorObject is XRBaseInputInteractor input) input.SendHapticImpulse(hoverHaptic, 0.02f);
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            Tap();
            if (args.interactorObject is XRBaseInputInteractor input) input.SendHapticImpulse(tapHaptic, 0.04f);
        }
    }
}
