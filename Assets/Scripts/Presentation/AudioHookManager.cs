using System;
using UnityEngine;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Audio presentation manager. Listens to gameplay events and dispatches spatial SFX
    /// or procedural oscillator feedback tones for pulses, detent clicks, gateway awakenings, and resets.
    /// Strictly adheres to layer boundaries by observing Gameplay state machines only.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioHookManager : MonoBehaviour
    {
        [Header("Event Sources")]
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private GatewayController gatewayController;
        [SerializeField] private LevelResetter levelResetter;

        [Header("Optional Audio Clips")]
        [SerializeField] private AudioClip pulseClip;
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip failureClip;
        [SerializeField] private AudioClip gatewayOpenClip;
        [SerializeField] private AudioClip resetClip;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (gatewayController == null) gatewayController = FindAnyObjectByType<GatewayController>();
            if (levelResetter == null) levelResetter = FindAnyObjectByType<LevelResetter>();
        }

        private void OnEnable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered += HandleForwardPassTriggered;
                chamberController.OnEvaluationComplete += HandleEvaluationComplete;
            }
            if (gatewayController != null) gatewayController.OnGatewayOpened += HandleGatewayOpened;
            if (levelResetter != null) levelResetter.OnResetCompleted += HandleResetCompleted;
        }

        private void OnDisable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered -= HandleForwardPassTriggered;
                chamberController.OnEvaluationComplete -= HandleEvaluationComplete;
            }
            if (gatewayController != null) gatewayController.OnGatewayOpened -= HandleGatewayOpened;
            if (levelResetter != null) levelResetter.OnResetCompleted -= HandleResetCompleted;
        }

        private void HandleForwardPassTriggered()
        {
            PlaySoundOrTone(pulseClip, 660f, 0.12f);
        }

        private void HandleEvaluationComplete(PuzzleEvaluation eval)
        {
            if (eval == null) return;
            if (eval.Passed)
            {
                PlaySoundOrTone(successClip, 880f, 0.4f);
            }
            else
            {
                PlaySoundOrTone(failureClip, 220f, 0.25f);
            }
        }

        private void HandleGatewayOpened()
        {
            PlaySoundOrTone(gatewayOpenClip, 523.25f, 0.8f);
        }

        private void HandleResetCompleted(BrokenConfigurationSO preset)
        {
            PlaySoundOrTone(resetClip, 330f, 0.2f);
        }

        private void PlaySoundOrTone(AudioClip clip, float fallbackFrequency, float duration)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
            else
            {
                // Procedural audio feedback fallback
                CreateSynthesizedBeep(fallbackFrequency, duration);
            }
        }

        private void CreateSynthesizedBeep(float frequency, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(1.0f - (t / duration)); // Linear decay
                samples[i] = Mathf.Sin(2.0f * Mathf.PI * frequency * t) * envelope * 0.35f;
            }

            AudioClip beepClip = AudioClip.Create("SynthesizedTone", sampleCount, 1, sampleRate, false);
            beepClip.SetData(samples, 0);

            if (_audioSource != null)
            {
                _audioSource.PlayOneShot(beepClip);
            }
        }
    }
}
