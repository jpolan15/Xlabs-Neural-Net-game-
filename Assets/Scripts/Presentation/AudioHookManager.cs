using System;
using UnityEngine;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Audio presentation manager.
    /// Subscribes to InteractionAudioBus (Gameplay layer) and Gameplay events,
    /// then fires synthesized SFX for every interaction.
    /// No audio clips required — all sounds are produced procedurally via AudioClip.Create.
    ///
    /// Sound catalogue:
    ///   Hover tick         — soft high-frequency blip
    ///   Interact click     — satisfying mid-freq click
    ///   Dial / slider step — short detent tick (pitch shifts per direction)
    ///   Lever pull         — descending sweep
    ///   Cable snap         — bright metallic ping
    ///   Socket crystal     — ascending chime
    ///   Forward pass       — rising sine burst
    ///   Evaluation pass    — bright chord
    ///   Evaluation fail    — low descending tone
    ///   Gateway open       — warm pad swell
    ///   Reset              — neutral soft blip
    ///   Footstep           — low thud
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioHookManager : MonoBehaviour
    {
        [Header("Event Sources")]
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private GatewayController gatewayController;
        [SerializeField] private LevelResetter levelResetter;

        [Header("Optional Audio Clips (leave empty to use procedural synth)")]
        [SerializeField] private AudioClip pulseClip;
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip failureClip;
        [SerializeField] private AudioClip gatewayOpenClip;
        [SerializeField] private AudioClip resetClip;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip interactClip;
        [SerializeField] private AudioClip stepClip;
        [SerializeField] private AudioClip leverClip;
        [SerializeField] private AudioClip cableClip;
        [SerializeField] private AudioClip socketClip;

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.8f;
        [SerializeField] private bool playEvaluationSounds = true;
        [SerializeField] private AudioClip shipBedClip;
        [SerializeField] private AudioSource shipBedSource;

        private AudioSource _audioSource;

        // Pre-built clip cache — avoids per-frame GC (Presentation rules: no per-frame allocations)
        private AudioClip _cachedHoverClip;
        private AudioClip _cachedClickClip;
        private AudioClip _cachedStepUpClip;
        private AudioClip _cachedStepDownClip;
        private AudioClip _cachedLeverClip;
        private AudioClip _cachedCableClip;
        private AudioClip _cachedSocketClip;
        private AudioClip _cachedFootstepClip;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (gatewayController == null) gatewayController = FindAnyObjectByType<GatewayController>();
            if (levelResetter == null)     levelResetter     = FindAnyObjectByType<LevelResetter>();

            BuildClipCache();
        }

        private void OnEnable()
        {
            // Gameplay events
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered += HandleForwardPassTriggered;
                chamberController.OnEvaluationComplete   += HandleEvaluationComplete;
            }
            if (gatewayController != null) gatewayController.OnGatewayOpened += HandleGatewayOpened;
            if (levelResetter != null)     levelResetter.OnResetCompleted     += HandleResetCompleted;

            // XR interaction signals (raised by InteractionAudioBus in Gameplay assembly)
            InteractionAudioBus.OnHoverEnter    += HandleHoverEnter;
            InteractionAudioBus.OnInteractClick += HandleInteractClick;
            InteractionAudioBus.OnDetentStep    += HandleDetentStep;
            InteractionAudioBus.OnLeverPull     += HandleLeverPull;
            InteractionAudioBus.OnCableToggle   += HandleCableToggle;
            InteractionAudioBus.OnSocketCycle   += HandleSocketCycle;
            InteractionAudioBus.OnFootstep      += HandleFootstep;
        }

        private void OnDisable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered -= HandleForwardPassTriggered;
                chamberController.OnEvaluationComplete   -= HandleEvaluationComplete;
            }
            if (gatewayController != null) gatewayController.OnGatewayOpened -= HandleGatewayOpened;
            if (levelResetter != null)     levelResetter.OnResetCompleted     -= HandleResetCompleted;

            InteractionAudioBus.OnHoverEnter    -= HandleHoverEnter;
            InteractionAudioBus.OnInteractClick -= HandleInteractClick;
            InteractionAudioBus.OnDetentStep    -= HandleDetentStep;
            InteractionAudioBus.OnLeverPull     -= HandleLeverPull;
            InteractionAudioBus.OnCableToggle   -= HandleCableToggle;
            InteractionAudioBus.OnSocketCycle   -= HandleSocketCycle;
            InteractionAudioBus.OnFootstep      -= HandleFootstep;
        }

        // ── Gameplay event handlers ──────────────────────────────────────────────

        private void HandleForwardPassTriggered()
        {
            PlayOr(pulseClip, SynthTone(660f, 0.12f));
        }

        private void Start()
        {
            if (shipBedSource != null && shipBedClip != null && !shipBedSource.isPlaying)
            {
                shipBedSource.spatialBlend = 0f;
                shipBedSource.loop = true;
                shipBedSource.clip = shipBedClip;
                shipBedSource.volume = 0.18f;
                shipBedSource.Play();
            }
        }

        private void HandleEvaluationComplete(PuzzleEvaluation eval)
        {
            if (!playEvaluationSounds || eval == null) return;
            if (eval.Passed)
                PlayOr(successClip, SynthChord(new[]{ 523.25f, 659.25f, 783.99f }, 0.45f));
            else
                PlayOr(failureClip, SynthTone(180f, 0.30f));
        }

        private void HandleGatewayOpened()
        {
            PlayOr(gatewayOpenClip, SynthPad(new[]{ 261.63f, 329.63f, 392f }, 0.9f));
        }

        private void HandleResetCompleted(BrokenConfigurationSO _)
        {
            PlayOr(resetClip, SynthTone(330f, 0.18f));
        }

        // ── Interaction bus handlers ─────────────────────────────────────────────

        private void HandleHoverEnter()         => Play(_cachedHoverClip,    hoverClip,   0.35f);
        private void HandleInteractClick()      => Play(_cachedClickClip,    interactClip, 0.70f);
        private void HandleLeverPull()          => Play(_cachedLeverClip,    leverClip,   0.75f);
        private void HandleCableToggle()        => Play(_cachedCableClip,    cableClip,   0.65f);
        private void HandleSocketCycle()        => Play(_cachedSocketClip,   socketClip,  0.70f);
        private void HandleFootstep()           => Play(_cachedFootstepClip, null,        0.20f);
        private void HandleDetentStep(int dir)  => Play(dir >= 0 ? _cachedStepUpClip : _cachedStepDownClip, stepClip, 0.55f);

        // ── Playback helpers ─────────────────────────────────────────────────────

        private void PlayOr(AudioClip userClip, AudioClip procedural)
        {
            AudioClip c = userClip != null ? userClip : procedural;
            if (c != null && _audioSource != null)
                _audioSource.PlayOneShot(c, masterVolume);
        }

        private void Play(AudioClip cached, AudioClip userOverride, float relVol)
        {
            AudioClip c = userOverride != null ? userOverride : cached;
            if (c != null && _audioSource != null)
                _audioSource.PlayOneShot(c, relVol * masterVolume);
        }

        // ── Procedural synth clip builders (all pre-built in Awake) ──────────────

        private void BuildClipCache()
        {
            _cachedHoverClip    = SynthTone(1200f, 0.04f);
            _cachedClickClip    = SynthTick(800f,  0.06f);
            _cachedStepUpClip   = SynthTone(540f,  0.07f);
            _cachedStepDownClip = SynthTone(380f,  0.07f);
            _cachedLeverClip    = SynthSweep(600f, 200f, 0.18f);
            _cachedCableClip    = SynthPing(1100f, 0.14f);
            _cachedSocketClip   = SynthChime(new[]{ 880f, 1100f }, 0.20f);
            _cachedFootstepClip = SynthTone(85f,   0.07f, vol: 0.5f);
        }

        private static AudioClip SynthTone(float freq, float duration, float vol = 0.35f)
        {
            int rate = 44100;
            int n = (int)(rate * duration);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Clamp01(1f - t / duration);
                s[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * vol;
            }
            return MakeClip("Tone", s, rate);
        }

        private static AudioClip SynthTick(float freq, float duration)
        {
            int rate = 44100;
            int n = (int)(rate * duration);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Pow(Mathf.Clamp01(1f - t / duration), 3f);
                s[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
            }
            return MakeClip("Tick", s, rate);
        }

        private static AudioClip SynthSweep(float startFreq, float endFreq, float duration)
        {
            int rate = 44100;
            int n = (int)(rate * duration);
            float[] s = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float alpha = (float)i / n;
                float freq  = Mathf.Lerp(startFreq, endFreq, alpha);
                float env   = Mathf.Clamp01(1f - alpha);
                phase += 2f * Mathf.PI * freq / rate;
                s[i] = Mathf.Sin(phase) * env * 0.35f;
            }
            return MakeClip("Sweep", s, rate);
        }

        private static AudioClip SynthPing(float freq, float duration)
        {
            int rate = 44100;
            int n = (int)(rate * duration);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Pow(Mathf.Clamp01(1f - t / duration), 2f);
                s[i] = (Mathf.Sin(2f * Mathf.PI * freq * t)
                      + Mathf.Sin(2f * Mathf.PI * (freq * 1.007f) * t)) * 0.25f * env;
            }
            return MakeClip("Ping", s, rate);
        }

        private static AudioClip SynthChime(float[] freqs, float duration)
        {
            int rate = 44100;
            float noteLen = duration / freqs.Length;
            int n = (int)(rate * duration);
            float[] s = new float[n];
            for (int fi = 0; fi < freqs.Length; fi++)
            {
                int start = (int)(fi * noteLen * rate);
                int end   = Mathf.Min(n, (int)((fi + 1) * noteLen * rate));
                for (int i = start; i < end; i++)
                {
                    float t = (float)(i - start) / rate;
                    float env = Mathf.Clamp01(1f - t / noteLen);
                    s[i] += Mathf.Sin(2f * Mathf.PI * freqs[fi] * t) * env * 0.30f;
                }
            }
            return MakeClip("Chime", s, rate);
        }

        private static AudioClip SynthChord(float[] freqs, float duration)
        {
            int rate = 44100;
            int n = (int)(rate * duration);
            float[] s = new float[n];
            float vol = 0.28f / freqs.Length;
            foreach (float freq in freqs)
            {
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / rate;
                    float env = Mathf.Clamp01(1f - t / duration);
                    s[i] += Mathf.Sin(2f * Mathf.PI * freq * t) * env * vol;
                }
            }
            return MakeClip("Chord", s, rate);
        }

        private static AudioClip SynthPad(float[] freqs, float duration)
        {
            int rate = 44100;
            int n = (int)(rate * duration);
            float[] s = new float[n];
            float vol = 0.22f / freqs.Length;
            foreach (float freq in freqs)
            {
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / rate;
                    float attack  = Mathf.Clamp01(t / (duration * 0.25f));
                    float release = Mathf.Clamp01(1f - t / duration);
                    s[i] += Mathf.Sin(2f * Mathf.PI * freq * t) * attack * release * vol;
                }
            }
            return MakeClip("Pad", s, rate);
        }

        private static AudioClip MakeClip(string name, float[] samples, int rate)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
