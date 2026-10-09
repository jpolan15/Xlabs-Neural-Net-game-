using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Narration section of the Neural Ride builder: writes the RideScript (pacing) and NarrationLibrary (subtitles,
    /// clips, chapter cards) assets from RideNarrationData. A voice clip is bound only when its sidecar text matches
    /// the approved script, so old recordings stay silent until they are regenerated.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private const string ScriptPath = "Assets/ScriptableObjects/RideScript.asset";
        private const string LibraryPath = "Assets/ScriptableObjects/NarrationLibrary.asset";

        private static RideScript _script;
        private static NarrationLibrary _library;

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void BuildNarration()
        {
            _library = LoadOrCreate<NarrationLibrary>(LibraryPath);
            _script = LoadOrCreate<RideScript>(ScriptPath);

            var entries = new List<NarrationLibrary.Entry>();
            var byLine = new Dictionary<RideLine, NarrationLibrary.Entry>();
            foreach (RideNarrationData.Line line in RideNarrationData.Lines)
            {
                if (byLine.ContainsKey(line.Id)) throw new InvalidOperationException("RideNarrationData lists " + line.Id + " twice.");
                var entry = new NarrationLibrary.Entry
                {
                    line = line.Id,
                    speaker = line.Speaker,
                    tag = line.Tag,
                    clip = FindVoiceClip(line),
                    pauseAfter = line.PauseAfter,
                    readSeconds = RideNarrationData.ReadSeconds(line),
                    segments = RideNarrationData.Segments(line.Text, out _)
                };
                if (entry.clip != null) ApplyMeasuredTiming(line, entry.segments);
                entries.Add(entry);
                byLine[line.Id] = entry;
            }

            foreach (RideLine id in Enum.GetValues(typeof(RideLine)))
            {
                if (!byLine.ContainsKey(id)) throw new InvalidOperationException("RideLine." + id + " has no entry in RideNarrationData. Every line needs a subtitle and a duration.");
            }

            _library.entries = entries.ToArray();
            _library.chapters = RideNarrationData.Chapters;
            EditorUtility.SetDirty(_library);

            _script.intro = ToSequence("Intro", RideNarrationData.IntroLines, byLine, RideNarrationData.IntroTasks);
            var briefings = new RideScript.Sequence[RideNarrationData.BriefingLines.Length];
            for (int i = 0; i < briefings.Length; i++) briefings[i] = ToSequence("Briefing " + (i + 1), RideNarrationData.BriefingLines[i], byLine);
            _script.briefings = briefings;
            _script.outro = ToSequence("Outro", RideNarrationData.OutroLines, byLine);
            _script.lines = entries.ConvertAll(e => new RideScript.Beat { line = e.line, seconds = e.TotalSeconds }).ToArray();
            EditorUtility.SetDirty(_script);

            Debug.Log("[NeuralRideBuilder] Narration: intro " + _script.intro.TotalSeconds.ToString("0") + " s, briefings "
                + string.Join(" / ", Array.ConvertAll(briefings, b => b.TotalSeconds.ToString("0"))) + " s, outro "
                + _script.outro.TotalSeconds.ToString("0") + " s. Voice clips bound: " + entries.FindAll(e => e.clip != null).Count + " of " + entries.Count + ".");
        }

        private static RideScript.Sequence ToSequence(string name, RideLine[] lines, Dictionary<RideLine, NarrationLibrary.Entry> byLine,
            RideNarrationData.RiderTask[] tasks = null)
        {
            var beats = new RideScript.Beat[lines.Length];
            for (int i = 0; i < lines.Length; i++) beats[i] = new RideScript.Beat { line = lines[i], seconds = byLine[lines[i]].TotalSeconds };

            foreach (RideNarrationData.RiderTask task in tasks ?? new RideNarrationData.RiderTask[0])
            {
                int at = Array.IndexOf(lines, task.Line);
                if (at < 0) throw new InvalidOperationException("RideNarrationData.IntroTasks waits on " + task.Line + ", which is not in the " + name + ".");
                if (task.TimeoutSeconds <= 0f) throw new InvalidOperationException("The hands-on beat on " + task.Line + " needs a timeout, or a rider who does nothing is stuck.");
                beats[at].waitFor = task.Control;
                beats[at].waitFrom = task.From;
                beats[at].waitAtLeast = task.AtLeast;
                beats[at].waitTimeout = task.TimeoutSeconds;
            }

            return new RideScript.Sequence { name = name, beats = beats };
        }

        private static AudioClip FindVoiceClip(RideNarrationData.Line line)
        {
            string baseName = VoiceDir + "vo_ride_" + ((int)line.Id).ToString("00");
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseName + ".wav");
            if (clip == null) return null;

            // Archival clips (JFK, NASA) carry their own words; their sidecar documents the source, not our text.
            if (line.FixedSpeakSeconds > 0f) return clip;

            string recorded = ReadSidecarSpeech(baseName + ".txt");
            if (recorded == null || Normalize(recorded) != Normalize(RideNarrationData.PlainText(line.Text)))
            {
                Debug.LogWarning("[NeuralRideBuilder] " + baseName + ".wav says different words than the approved script for "
                    + line.Id + ", so it is not used. Regenerate it from RideNarrationData and keep the .txt sidecar with the exact text.");
                return null;
            }

            return clip;
        }

        private static readonly string[] SidecarHeaders = { "Source:", "Speech:", "Segments:" };

        private static string ReadSidecarSpeech(string path)
        {
            if (!File.Exists(path)) return null;
            var kept = new List<string>();
            foreach (string raw in File.ReadAllLines(path))
            {
                string row = raw.Trim('﻿', ' ', '\t');
                if (row.Length == 0 || Array.Exists(SidecarHeaders, h => row.StartsWith(h, StringComparison.OrdinalIgnoreCase))) continue;
                kept.Add(row);
            }

            return string.Join(" ", kept);
        }

        /// <summary>
        /// A rendered clip's sidecar carries "Segments:" with the measured seconds of each subtitle clause (speech plus
        /// the pause after it; ArtSource/voice/render_voices.py). Used as the caption weights, the subtitle changes on
        /// the clause the voice is actually on, not on a words-per-second estimate.
        /// </summary>
        private static void ApplyMeasuredTiming(RideNarrationData.Line line, NarrationLibrary.Segment[] segments)
        {
            string path = VoiceDir + "vo_ride_" + ((int)line.Id).ToString("00") + ".txt";
            if (!File.Exists(path)) return;
            foreach (string raw in File.ReadAllLines(path))
            {
                string row = raw.Trim('﻿', ' ', '\t');
                if (!row.StartsWith("Segments:", StringComparison.OrdinalIgnoreCase)) continue;
                string[] parts = row.Substring("Segments:".Length).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != segments.Length)
                {
                    Debug.LogWarning("[NeuralRideBuilder] " + path + " times " + parts.Length + " clauses but the script has " + segments.Length + "; captions use the estimate.");
                    return;
                }

                var seconds = new float[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    if (!float.TryParse(parts[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds[i]) || seconds[i] <= 0f)
                    {
                        Debug.LogWarning("[NeuralRideBuilder] " + path + " has a bad Segments value '" + parts[i] + "'; captions use the estimate.");
                        return;
                    }
                }

                for (int i = 0; i < seconds.Length; i++) segments[i].weight = seconds[i];
                return;
            }
        }

        private static string Normalize(string text) => Regex.Replace(text.ToLowerInvariant(), "[^a-z0-9]", string.Empty);
    }
}
