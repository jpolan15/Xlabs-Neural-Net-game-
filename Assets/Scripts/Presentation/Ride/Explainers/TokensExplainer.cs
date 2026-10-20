namespace Convergence.Presentation.Ride.Explainers
{
    // WP1 gives this beat its stage (the region pieces it keeps dark until its unfold cue, written by the builder in
    // NeuralRideBuilder.Explainers) and its unfold cue; WP3 to WP5 add the steps, declared like PixelsExplainer's:
    //     s.At("neuron", () => new[] { Anim.GrowFromCenter(neuron, Vector3.one) });
    /// <summary>E8, tokens: the bridge to Level 2, before the end card (WP5).</summary>
    public sealed class TokensExplainer : Explainer
    {
        protected override void Declare(Steps s)
        {
        }
    }
}
