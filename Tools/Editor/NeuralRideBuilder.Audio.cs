using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Audio section of the Neural Ride builder: import settings for voice and effects, and the RideSfxView with its
    /// sources and cues. Every effect level is computed from the clip's measured loudness and the loudness it should
    /// play at (ArtSource/sfx/make_sfx.py prints the table), so effects sit under the voice (-16 LUFS) by numbers.
    /// Nobody on the build side can listen: the headset check at 50% Quest volume is still the final word.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private const string SfxDir = "Assets/_Project/Audio/Sfx/";
        private const string KenneyDir = "Assets/_Project/Audio/Kenney/";
        private const string SciFiDir = KenneyDir + "sci-fi/Audio/";

        /// <summary>Loudest any effect may play, relative to the voice's -16 LUFS. The build fails above it.</summary>
        private const float SfxCeilingLufs = -20f;

        private struct CueSpec
        {
            public string Field;
            public string Path;
            public float ClipLufs;   // measured, integrated (ArtSource/sfx/make_sfx.py)
            public float TargetLufs; // how loud it should play
            public float Jitter;

            public CueSpec(string field, string path, float clipLufs, float targetLufs, float jitter = 0f)
            {
                Field = field;
                Path = path;
                ClipLufs = clipLufs;
                TargetLufs = targetLufs;
                Jitter = jitter;
            }

            public float Volume => Mathf.Min(1f, Mathf.Pow(10f, (TargetLufs - ClipLufs) / 20f));
        }

        // Kenney Sci-fi and Interface Sounds are CC0 (Assets/_Project/Audio/Kenney/License.txt); ride_* are derived or
        // synthesized from them by ArtSource/sfx/make_sfx.py (Assets/_Project/Audio/Sfx/LICENSE.txt).
        private static readonly CueSpec[] Cues =
        {
            new CueSpec("launch", SciFiDir + "thrusterFire_000.ogg", -19.9f, -25f),
            new CueSpec("launchRumble", SciFiDir + "lowFrequency_explosion_000.ogg", -18.3f, -27f),
            new CueSpec("region", SciFiDir + "doorOpen_000.ogg", -14.3f, -28f),
            new CueSpec("tick", KenneyDir + "click_001.wav", -26.1f, -29f, 0.04f),
            new CueSpec("solved", KenneyDir + "confirmation_001.wav", -11.6f, -22f),
            new CueSpec("solvedShimmer", SciFiDir + "forceField_002.ogg", -11.3f, -27f),
            new CueSpec("wrong", SfxDir + "ride_wrong.wav", -7.3f, -24f),
            new CueSpec("nudge", SciFiDir + "laserLarge_000.ogg", -19.6f, -25f, 0.05f),
            new CueSpec("hop", KenneyDir + "click_001.wav", -26.1f, -30f),
            new CueSpec("finale", SciFiDir + "lowFrequency_explosion_001.ogg", -15.9f, -21f),
            new CueSpec("finaleShimmer", SciFiDir + "forceField_004.ogg", -11.4f, -24f),
            new CueSpec("skip", KenneyDir + "click_001.wav", -26.1f, -30f),
        };

        private static readonly CueSpec Hum = new CueSpec("hum", SfxDir + "ride_hum_loop.wav", -16.4f, -38f);
        private static readonly CueSpec Engine = new CueSpec("engine", SfxDir + "ride_engine_loop.wav", -15.5f, -31f);

        private const int SfxPoolSize = 8;

        /// <summary>Voice: Vorbis 70, compressed in memory (one plays at a time). Effects: Vorbis 70, decompressed on load (no decode spikes on Quest).</summary>
        private static void ImportAudio()
        {
            foreach (string guid in AssetDatabase.FindAssets("vo_ride_ t:AudioClip", new[] { VoiceDir.TrimEnd('/') }))
            {
                Configure(AssetDatabase.GUIDToAssetPath(guid), AudioClipLoadType.CompressedInMemory);
            }

            var effects = new HashSet<string> { Hum.Path, Engine.Path };
            foreach (CueSpec cue in Cues) effects.Add(cue.Path);
            foreach (string path in effects) Configure(path, AudioClipLoadType.DecompressOnLoad);
        }

        private static void Configure(string path, AudioClipLoadType loadType)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
            {
                throw new System.IO.FileNotFoundException("Ride audio missing at " + path + ". Effects: run ArtSource/sfx/make_sfx.py. Voice: see ArtSource/voice/README.md.");
            }

            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            bool same = importer.forceToMono && s.loadType == loadType && s.compressionFormat == AudioCompressionFormat.Vorbis
                && Mathf.Approximately(s.quality, 0.7f) && s.preloadAudioData;
            if (same) return;

            importer.forceToMono = true;
            s.loadType = loadType;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
            s.preloadAudioData = true;
            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();
        }

        private static AudioClip Clip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new System.IO.FileNotFoundException("Ride audio missing at " + path + ". Run ArtSource/sfx/make_sfx.py, then rebuild.");
            return clip;
        }

        private static AudioSource Source(string name, Transform parent, bool world)
        {
            var source = Make(name, parent, Vector3.zero).AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.spatialBlend = world ? 0.75f : 0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 4f;   // the stations' streams sit 3 to 5 m out: no drop inside the station
            source.maxDistance = 40f;
            return source;
        }

        /// <summary>The effects rig on the pod. The director and the moments are wired in WireSfx.</summary>
        private static void BuildSfx(GameObject pod, DashScreenView dash)
        {
            Transform root = Make("Sfx", pod.transform, new Vector3(0, 1.2f, 0)).transform;
            AudioSource hum = Source("Hum", root, false);
            hum.clip = Clip(Hum.Path);
            AudioSource engine = Source("Engine", root, false);
            engine.clip = Clip(Engine.Path);
            var pool = new List<UnityEngine.Object>();
            for (int i = 0; i < SfxPoolSize; i++) pool.Add(Source("OneShot" + i, root, true));

            var view = pod.AddComponent<RideSfxView>();
            Ref(view, "dash", dash);
            Ref(view, "hum", hum);
            Ref(view, "engine", engine);
            Refs(view, "pool", pool);
            Set(view, "humVolume", p => p.floatValue = Hum.Volume);
            Set(view, "engineVolume", p => p.floatValue = Engine.Volume);
            foreach (CueSpec cue in Cues)
            {
                CueSpec c = cue;
                Set(view, c.Field, p =>
                {
                    p.FindPropertyRelative("clip").objectReferenceValue = Clip(c.Path);
                    p.FindPropertyRelative("volume").floatValue = c.Volume;
                    p.FindPropertyRelative("pitchJitter").floatValue = c.Jitter;
                });
            }
        }

        private static void WireSfx(GameObject pod, RideDirector director, Moments moments)
        {
            var view = pod.GetComponent<RideSfxView>();
            Ref(view, "director", director);
            Ref(view, "puzzle", moments.Paint);
            Ref(view, "blame", moments.Blame);
            Ref(view, "valley", moments.Valley);
        }

        /// <summary>Fails the build if an effect is missing or would play louder than SfxCeilingLufs, or a loop above the music.</summary>
        private static void AssertRideSfxLevels(GameObject pod)
        {
            var bad = new List<string>();
            var view = pod.GetComponent<RideSfxView>();
            if (view == null) bad.Add("the pod has no RideSfxView");
            else
            {
                var so = new SerializedObject(view);
                foreach (CueSpec cue in Cues)
                {
                    SerializedProperty p = so.FindProperty(cue.Field);
                    var clip = p.FindPropertyRelative("clip").objectReferenceValue as AudioClip;
                    float volume = p.FindPropertyRelative("volume").floatValue;
                    if (clip == null) bad.Add(cue.Field + " has no clip");
                    float plays = cue.ClipLufs + 20f * Mathf.Log10(Mathf.Max(1e-4f, volume));
                    if (plays > SfxCeilingLufs + 0.05f) bad.Add(cue.Field + " plays at " + plays.ToString("0.0") + " LUFS, above " + SfxCeilingLufs);
                }

                if (Hum.TargetLufs > -30f || Engine.TargetLufs > -30f) bad.Add("a loop is set above -30 LUFS, which competes with the music bed");
            }

            if (bad.Count > 0) throw new InvalidOperationException("Ride effects: " + string.Join("; ", bad) + ". Fix the Cues table in Tools/Editor/NeuralRideBuilder.Audio.cs.");
        }
    }
}
