namespace Convergence.Core.Neural
{
    /// <summary>
    /// Supported neural network activation function types.
    /// </summary>
    public enum ActivationType
    {
        /// <summary>
        /// Heaviside step function: 1 if z >= 0, else 0.
        /// </summary>
        Step,

        /// <summary>
        /// Identity linear activation: f(z) = z.
        /// </summary>
        Linear,

        /// <summary>
        /// Rectified linear unit: max(0, z).
        /// </summary>
        ReLU,

        /// <summary>
        /// Logistic sigmoid: 1 / (1 + e^-z).
        /// </summary>
        Sigmoid,

        /// <summary>
        /// Hyperbolic tangent: tanh(z).
        /// </summary>
        Tanh,

        /// <summary>
        /// Vector-valued softmax: exp(z_i - max(z)) / sum(exp(z_j - max(z))).
        /// Note: Evaluated at the layer level, not on individual scalar neurons.
        /// </summary>
        Softmax
    }
}
