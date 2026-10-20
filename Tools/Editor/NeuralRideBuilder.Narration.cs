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
    /// Narration section of the builder: writes the RideScript (pacing) and NarrationLibrary (subtitles, clips) assets
    /// from RideNarrationData, and checks the script's numbers against the bake. A voice clip is bound only when its
    /// sidecar text matches the approved script, so old recordings stay silent until they are regenerated.
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

            _library.entries = entries.ToArray();
            EditorUtility.SetDirty(_library);

            _script.intro = ToSequence("Intro", RideNarrationData.IntroLines, byLine);
            _script.dockLine = RideNarrationData.DockLine;
            var sequences = new RideScript.Sequence[RideNarrationData.Sequences.Length];
            for (int i = 0; i < sequences.Length; i++) sequences[i] = ToSequence(RideNarrationData.Sequences[i].Name, RideNarrationData.Sequences[i].Lines, byLine);
            _script.sequences = sequences;
            _script.lines = entries.ConvertAll(e => new RideScript.Beat { line = e.line, seconds = e.TotalSeconds }).ToArray();
            EditorUtility.SetDirty(_script);

            float total = 0f;
            foreach (NarrationLibrary.Entry e in entries) total += e.TotalSeconds;
            Debug.Log("[NeuralRideBuilder] Narration: " + entries.Count + " lines, " + total.ToString("0") + " s spoken in all; intro "
                + _script.intro.TotalSeconds.ToString("0") + " s. Voice clips bound: " + entries.FindAll(e => e.clip != null).Count + " of " + entries.Count + ".");
        }

        private static RideScript.Sequence ToSequence(string name, RideLine[] lines, Dictionary<RideLine, NarrationLibrary.Entry> byLine)
        {
            var beats = new RideScript.Beat[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                if (!byLine.TryGetValue(lines[i], out NarrationLibrary.Entry entry)) throw new InvalidOperationException(name + " plays " + lines[i] + ", which RideNarrationData lacks.");
                beats[i] = new RideScript.Beat { line = lines[i], seconds = entry.TotalSeconds };
            }

            return new RideScript.Sequence { name = name, beats = beats };
        }

        /// <summary>
        /// The narrator's numbers come from the bake: the untrained guess, the training count, the test accuracy. If the
        /// data and the script disagree, the script changes, never the data (HANDOFF_LEVEL1_FULL_BUILD, rule 3).
        /// </summary>
        private static void AssertNarrationMatchesBake()
        {
            var problems = new List<string>();
            void Expect(RideLine line, string words)
            {
                string spoken = RideNarrationData.PlainText(RideNarrationData.Get(line).Text).ToLowerInvariant();
                if (!spoken.Contains(words)) problems.Add(line + " must say \"" + words + "\" (from the bake) but says \"" + spoken + "\"");
            }

            Expect(RideLine.WrongSays, "says... " + RideNarrationData.Words(_data.File.hero.untrainedGuess));
            Expect(RideLine.RightTraining, RideNarrationData.Words(_data.File.trainCount));
            Expect(RideLine.RightAccuracy, RideNarrationData.Words(_data.TestAccuracyPercent) + " out of a hundred");
            Expect(RideLine.RightSays, "says... " + RideNarrationData.Words(Convergence.Gameplay.Ride.Level1.Level1Data.HeroDigit));
            if (problems.Count > 0) throw new InvalidOperationException("Narration disagrees with the bake: " + string.Join("; ", problems) + ". Fix Tools/Editor/RideNarrationData.cs.");
        }

        private static AudioClip FindVoiceClip(RideNarrationData.Line line)
        {
            string baseName = VoiceDir + "vo_ride_" + ((int)line.Id).ToString("00");
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseName + ".wav");
            if (clip == null) return null;
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
        /// the clause the voice is actually on.
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
