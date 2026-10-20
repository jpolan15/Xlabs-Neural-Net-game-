using System;
using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// The pacing of the narrated ride: the lines of the intro, the line said at the dock, and the sequences the
    /// segments play while the pod travels and when it arrives, with how many seconds each line holds the floor (clip
    /// length plus the pause after it). Numbers only. Gameplay paces the ride from this and never touches an AudioClip,
    /// so the flow is testable without audio. The builder writes it from Tools/Editor/RideNarrationData.cs.
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

        [Tooltip("Said when the dock opens and GO goes live.")]
        public RideLine dockLine = RideLine.PullGo;

        [Tooltip("The sequences segments play, by index (RideDirector.Segment.travel and arrival).")]
        public Sequence[] sequences = new Sequence[0];

        [Tooltip("Every line's seconds, including the lines moments say in the moment, so the ride can let a line finish before it moves on.")]
        public Beat[] lines = new Beat[0];

        /// <summary>The sequence at an index, or an empty one for -1 or an index the script does not have.</summary>
        public Sequence SequenceAt(int index)
        {
            return sequences != null && index >= 0 && index < sequences.Length ? sequences[index] : default;
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
