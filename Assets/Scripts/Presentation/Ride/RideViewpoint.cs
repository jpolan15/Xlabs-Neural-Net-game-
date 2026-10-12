using UnityEngine;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// A place the rider sits still and looks: the dock, each stop, the rim, the middle of the drop, the end. Holds
    /// nothing the ride runs on. The development tools read it: seat captures put the camera here, the sightline check
    /// tests the listed panels from here, and the budget counts what the segment's region mask shows. Written by the
    /// builder.
    /// </summary>
    public sealed class RideViewpoint : MonoBehaviour
    {
        [Tooltip("Short name for captures and reports, for example 'neuron' or 'valley_mid_drop'.")]
        public string label;

        [Tooltip("The segment the rider is in here (-1 for the dock); the streamer's mask for it says what is visible.")]
        public int segment = -1;

        [Tooltip("Panels and signs the rider has to read from here. Every TMP text under them is checked for size too.")]
        public Transform[] targets = new Transform[0];

        [Tooltip("Pod-mounted panels that are live here (for example the weight grid).")]
        public string[] podPanels = new string[0];
    }
}
