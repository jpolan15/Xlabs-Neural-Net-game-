using System;
using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// The pacing of the narrated ride: which lines play in the intro, in each briefing, and in the outro, and how
    /// many seconds each one holds the floor (clip length plus the pause after it). Numbers only. Gameplay paces the
    /// ride from this and never touches an AudioClip, so the flow is testable without audio. The builder writes it
    /// from the narration data (Tools/Editor/RideNarrationData.cs).
    /// </summary>
    [CreateAssetMenu(menuName = "Convergence/Ride Script", fileName = "RideScript")]
    public sealed class RideScript : ScriptableObject
    {
        [Serializable]
        public struct Beat
        {
            public RideLine line;

            [Tooltip("How long this line holds the floor: clip length (or reading time) plus the pause after it.")]
            public float seconds;

            [Tooltip("Empty for a spoken beat. A control id (RideControlIds) makes it a hands-on beat: the ride waits for the rider to set that control to at least Wait At Least, or for Wait Timeout after the line, whichever comes first.")]
            public string waitFor;

            [Tooltip("Where the control is set when the hands-on beat opens. Ignored for GO, which the rider may be holding.")]
            public float waitFrom;

            public float waitAtLeast;

            [Tooltip("Seconds after the line ends before the ride does it for the rider, so nobody gets stuck.")]
            public float waitTimeout;

            public bool WaitsForRider => !string.IsNullOrEmpty(waitFor);
        }

        [Serializable]
        public struct Sequence
        {
            public string name;
            public Beat[] beats;

            public bool IsEmpty => beats == null || beats.Length == 0;

            public float TotalSeconds
            {
                get
                {
                    float total = 0f;
                    if (beats == null) return total;
                    for (int i = 0; i < beats.Length; i++) total += beats[i].seconds;
                    return total;
                }
            }
        }

        public Sequence intro;

        [Tooltip("One briefing per stop, played while the pod travels to it.")]
        public Sequence[] briefings = new Sequence[0];

        public Sequence outro;

        [Tooltip("Every line's seconds, including the station lines said in the moment, so the ride can let a line finish before it moves on.")]
        public Beat[] lines = new Beat[0];

        public Sequence BriefingFor(int stopIndex)
        {
            return briefings != null && stopIndex >= 0 && stopIndex < briefings.Length ? briefings[stopIndex] : default;
        }

        /// <summary>How long a line holds the floor, or 0 if the script does not know it.</summary>
        public float SecondsOf(RideLine line)
        {
            if (lines == null) return 0f;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].line == line) return lines[i].seconds;
            }

            return 0f;
        }
    }
}
