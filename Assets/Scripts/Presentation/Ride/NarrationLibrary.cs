using System;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Everything the ear and the eye get for each <see cref="RideLine"/>: who speaks, the subtitle split into
    /// clauses, the voice clip, and the pause after it. A line with no clip still shows its subtitle for its reading
    /// time, so a recording is added by filename and a builder re-run without touching scene logic. Also holds the
    /// chapter cards shown during briefings. The builder writes it from Tools/Editor/RideNarrationData.cs.
    /// </summary>
    [CreateAssetMenu(menuName = "Convergence/Narration Library", fileName = "NarrationLibrary")]
    public sealed class NarrationLibrary : ScriptableObject
    {
        public enum Speaker
        {
            Guide,
            Aura,
            AuraClean,
            Archival
        }

        /// <summary>
        /// One clause of a subtitle. The weight is its share of the line's time, so captions follow the voice. The cues
        /// name what the rider should see while this clause is spoken (for example "neurons" or "downhill"); the views
        /// that know a cue react to it, so the eye follows the explanation instead of waiting through it.
        /// </summary>
        [Serializable]
        public struct Segment
        {
            public string text;
            public float weight;
            public string[] cues;
        }

        [Serializable]
        public struct Entry
        {
            public RideLine line;
            public Speaker speaker;

            [Tooltip("Shown as the speaker tag, for example GUIDE or NASA, APOLLO 11.")]
            public string tag;

            public AudioClip clip;

            [Tooltip("Silence after the line, seconds.")]
            public float pauseAfter;

            [Tooltip("Speaking time used when there is no clip yet.")]
            public float readSeconds;

            public Segment[] segments;

            public float SpeakSeconds => clip != null && clip.length > 0.2f ? clip.length : readSeconds;
            public float TotalSeconds => SpeakSeconds + pauseAfter;
        }

        /// <summary>A card shown during a briefing: a year, a title, and one plain sentence.</summary>
        [Serializable]
        public struct Chapter
        {
            public string year;
            public string title;
            public string blurb;
        }

        public Entry[] entries = new Entry[0];

        [Tooltip("One per stop, then one for the outro.")]
        public Chapter[] chapters = new Chapter[0];

        public bool TryGet(RideLine line, out Entry entry)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].line != line) continue;
                entry = entries[i];
                return true;
            }

            entry = default;
            return false;
        }

        /// <summary>The caption to show this far into a line, 0 to 1.</summary>
        public static string SegmentAt(Entry entry, float fraction)
        {
            int index = SegmentIndexAt(entry, fraction);
            return index < 0 ? string.Empty : entry.segments[index].text;
        }

        /// <summary>Which clause is being spoken this far into a line, 0 to 1; -1 for a line with no segments.</summary>
        public static int SegmentIndexAt(Entry entry, float fraction)
        {
            Segment[] segments = entry.segments;
            if (segments == null || segments.Length == 0) return -1;

            float total = 0f;
            for (int i = 0; i < segments.Length; i++) total += Mathf.Max(0.0001f, segments[i].weight);

            float at = Mathf.Clamp01(fraction) * total;
            float run = 0f;
            for (int i = 0; i < segments.Length; i++)
            {
                run += Mathf.Max(0.0001f, segments[i].weight);
                if (at <= run) return i;
            }

            return segments.Length - 1;
        }

        /// <summary>How long a clause is spoken, in seconds: its share of the line's speaking time.</summary>
        public static float SegmentSeconds(Entry entry, int index)
        {
            Segment[] segments = entry.segments;
            if (segments == null || index < 0 || index >= segments.Length) return 0f;

            float total = 0f;
            for (int i = 0; i < segments.Length; i++) total += Mathf.Max(0.0001f, segments[i].weight);
            return entry.SpeakSeconds * Mathf.Max(0.0001f, segments[index].weight) / total;
        }

        /// <summary>True when the clause carries the cue.</summary>
        public static bool HasCue(Segment segment, string cue)
        {
            if (segment.cues == null) return false;
            for (int i = 0; i < segment.cues.Length; i++)
            {
                if (segment.cues[i] == cue) return true;
            }

            return false;
        }
    }
}
