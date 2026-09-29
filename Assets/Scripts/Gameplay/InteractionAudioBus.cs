using System;

namespace Convergence.Gameplay
{
    /// <summary>
    /// Thin static event bus for interaction audio signals.
    /// Lives in Convergence.Gameplay so both the XR layer (raises) and
    /// the Presentation layer (subscribes) can reference it without
    /// creating a forbidden assembly dependency.
    ///
    /// XR raises → Presentation listens. Neither layer references the other.
    /// </summary>
    public static class InteractionAudioBus
    {
        public static event Action OnHoverEnter;
        public static event Action OnInteractClick;
        public static event Action<int> OnDetentStep;   // +1 up, -1 down
        public static event Action OnLeverPull;
        public static event Action OnCableToggle;
        public static event Action OnSocketCycle;
        public static event Action OnFootstep;

        public static void RaiseHoverEnter()        => OnHoverEnter?.Invoke();
        public static void RaiseInteractClick()     => OnInteractClick?.Invoke();
        public static void RaiseDetentStep(int dir) => OnDetentStep?.Invoke(dir);
        public static void RaiseLeverPull()         => OnLeverPull?.Invoke();
        public static void RaiseCableToggle()       => OnCableToggle?.Invoke();
        public static void RaiseSocketCycle()       => OnSocketCycle?.Invoke();
        public static void RaiseFootstep()          => OnFootstep?.Invoke();
    }
}
