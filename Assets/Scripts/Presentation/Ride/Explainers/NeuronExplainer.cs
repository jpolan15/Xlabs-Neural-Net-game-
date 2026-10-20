namespace Convergence.Presentation.Ride.Explainers
{
    // WP1 gives this beat its stage (the region pieces it keeps dark until its unfold cue, written by the builder in
    // NeuralRideBuilder.Explainers) and its unfold cue; WP3 to WP5 add the steps, declared like PixelsExplainer's:
    //     s.At("neuron", () => new[] { Anim.GrowFromCenter(neuron, Vector3.one) });
    /// <summary>E2, one neuron: unfolds on {paint} into the big neuron and the puzzle (WP3 builds the explainer).</summary>
    public sealed class NeuronExplainer : Explainer
    {
        protected override void Declare(Steps s) => s.Unfold("paint");
    }
}
