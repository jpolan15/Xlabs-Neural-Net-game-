using UnityEngine;

namespace Convergence.Presentation
{
    /// <summary>
    /// Marks a damage prop that should change when the chamber passes.
    /// The state test counts these. It does not decide correctness.
    /// </summary>
    public sealed class StateBoundDamage : MonoBehaviour
    {
        [SerializeField] private string kind = "unspecified";

        public string Kind => kind;
    }
}
