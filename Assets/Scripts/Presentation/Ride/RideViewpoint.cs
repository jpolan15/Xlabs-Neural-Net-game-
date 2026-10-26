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

        /// <summary>The game table's boards: only one shows at a time, the one its game uses.</summary>
        public static readonly string[] GameBoards = { "WeightGrid", "PlugBoard", "DescentBoard", "HuntBoard" };

        /// <summary>
        /// True for a part of the game table that is not up at this viewpoint: the whole table unless "GameTable" is
        /// listed, and each board unless "GameTable/&lt;board&gt;" is listed. The stamp and the +N popup only flash.
        /// </summary>
        public static bool HiddenGamePart(Transform t, RideViewpoint vp)
        {
            if (t.name == "Stamp" || t.name == "Popup") return true;
            if (t.name == "GameTable") return System.Array.IndexOf(vp.podPanels, "GameTable") < 0;
            if (t.parent != null && t.parent.name == "GameTable" && System.Array.IndexOf(GameBoards, t.name) >= 0)
            {
                return System.Array.IndexOf(vp.podPanels, "GameTable/" + t.name) < 0;
            }

            return false;
        }
    }
}
