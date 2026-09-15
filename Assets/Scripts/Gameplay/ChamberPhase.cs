namespace Convergence.Gameplay
{
    /// <summary>
    /// Lifecycle phases of Level 1 — The Awakening Gate.
    /// </summary>
    public enum ChamberPhase
    {
        /// <summary>
        /// Player spawns; alien machine is cold, silent, and sealed gateway is locked.
        /// </summary>
        Arrival,

        /// <summary>
        /// Player is actively interacting with weights, bias, activation crystals, and cables.
        /// </summary>
        NeuralRepair,

        /// <summary>
        /// 100% accuracy achieved; alien neural energy awakens, gateway unseals.
        /// </summary>
        Awakening,

        /// <summary>
        /// Gateway fully open; level complete; player free to step through or reset.
        /// </summary>
        Complete
    }
}
