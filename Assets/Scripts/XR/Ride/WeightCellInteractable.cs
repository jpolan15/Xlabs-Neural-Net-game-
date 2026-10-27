using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.XR.Ride
{
    /// <summary>
    /// One cell of the rider's 7×7 weight grid: a collider, no renderer (the grid is one mesh). A laser shot, a poke with
    /// a fingertip, a grip select, or a mouse click presses it with a hand, and a held laser sweeping over it sprays it
    /// (v6); the cell passes it to the grid (<see cref="DashGridInput"/>), which gives it to the paint game, and that game
    /// decides what it does. A poke or grip counts as the right hand unless it came from the left controller.
    /// </summary>
    public sealed class WeightCellInteractable : XRSimpleInteractable, IRideStrokeTarget
    {
        [SerializeField] private int cell;
        [SerializeField] private float tapHaptic = 0.35f;
        [SerializeField] private float hoverHaptic = 0.08f;
        [Tooltip("Ignore a second select on the same cell within this many seconds (a poke can bounce).")]
        [SerializeField] private float debounceSeconds = 0.25f;
        [SerializeField] private UnityEvent<int> tapped = new UnityEvent<int>();
        [SerializeField] private DashGridInput grid;

        private float _lastTap = -10f;

        public int Cell => cell;

        /// <summary>Raised with the cell index on a tap.</summary>
        public UnityEvent<int> Tapped => tapped;

        /// <summary>Sets the index. Used by the scene builder.</summary>
        public void SetCell(int index) => cell = index;

        /// <summary>Sets the grid the presses go to. Used by the scene builder.</summary>
        public void SetGrid(DashGridInput input) => grid = input;

        /// <summary>A tap with the right hand (mouse, poke).</summary>
        public void Tap() => Press(RideHand.Right);

        /// <summary>A press with a hand. Returns true when the live game used it.</summary>
        public bool Press(int hand)
        {
            if (Time.unscaledTime - _lastTap < debounceSeconds) return false;
            _lastTap = Time.unscaledTime;
            tapped.Invoke(cell);
            return grid != null && grid.Press(cell, hand);
        }

        /// <summary>A held laser landed on this cell (pressed here, or swept onto it): spray it with that hand's brush.</summary>
        public bool StrokeBegin(int hand, Vector3 point, bool pressed)
        {
            if (pressed) return Press(hand);
            tapped.Invoke(cell);
            return grid != null && grid.Spray(cell, hand, false);
        }

        public void StrokeMove(int hand, Vector3 point)
        {
        }

        public void StrokeEnd(int hand)
        {
        }

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);
            if (args.interactorObject is XRBaseInputInteractor input) input.SendHapticImpulse(hoverHaptic, 0.02f);
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            bool left = args.interactorObject is Component c && IsLeft(c.transform);
            Press(left ? RideHand.Left : RideHand.Right);
            if (args.interactorObject is XRBaseInputInteractor input) input.SendHapticImpulse(tapHaptic, 0.04f);
        }

        /// <summary>True when an interactor's transform sits under a controller named "Left…".</summary>
        internal static bool IsLeft(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                if (t.name.StartsWith("Left")) return true;
                if (t.name.StartsWith("Right")) return false;
            }

            return false;
        }
    }
}
