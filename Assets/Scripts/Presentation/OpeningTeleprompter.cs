using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Plays the player's opening recording once and reveals the banner lines in time with it.
    /// </summary>
    public class OpeningTeleprompter : MonoBehaviour
    {
        static readonly float[] LineStart = { 0.00f, 3.06f, 5.16f, 7.24f, 12.48f };
        static readonly float[] LineEnd = { 2.70f, 4.62f, 6.72f, 11.72f, 16.72f };
        static readonly string[] LineText =
        {
            "Warning, passengers of Neural.",
            "This is not a drill.",
            "Our ship has lost connection.",
            "It is your job to reconnect it, or we will be stuck in space forever.",
            "Warning, warning, warning, warning."
        };

        [SerializeField] private ChamberOnboardingController onboarding;
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioClip voiceClip;
        [SerializeField] private AudioSource bedSource;
        [SerializeField] private AudioClip bedClip;
        [SerializeField] private AudioClip computerTick;
        [SerializeField] private TextMesh banner;
        [SerializeField] private float recordingLength = 18.24f;

        float _elapsed = -1f;
        int _line = -1;
        bool _finished;

        public bool IsPlaying => _elapsed >= 0f && !_finished;
        public event Action OnFinished;

        void Awake()
        {
            if (onboarding == null) onboarding = FindAnyObjectByType<ChamberOnboardingController>();
        }

        void OnEnable()
        {
            if (onboarding != null) onboarding.OnOpeningBriefing += Begin;
        }

        void OnDisable()
        {
            if (onboarding != null) onboarding.OnOpeningBriefing -= Begin;
        }

        public void Begin()
        {
            if (_elapsed >= 0f) return;
            _elapsed = 0f;
            _line = -1;
            if (banner != null) banner.text = string.Empty;

            if (voiceSource != null)
            {
                voiceSource.spatialBlend = 1f;
                voiceSource.playOnAwake = false;
                voiceSource.loop = false;
                if (computerTick != null) voiceSource.PlayOneShot(computerTick, 0.25f);
                if (voiceClip != null) voiceSource.PlayOneShot(voiceClip, 1f);
            }

            if (bedSource != null && bedClip != null && !bedSource.isPlaying)
            {
                bedSource.spatialBlend = 0f;
                bedSource.loop = true;
                bedSource.clip = bedClip;
                bedSource.volume = 0.18f;
                bedSource.Play();
            }
        }

        void Update()
        {
            if (_elapsed < 0f || _finished) return;
            _elapsed += Time.deltaTime;

            int show = -1;
            for (int i = 0; i < LineStart.Length; i++)
            {
                if (_elapsed >= LineStart[i] && _elapsed < LineEnd[i]) show = i;
            }

            if (show != _line)
            {
                _line = show;
                // The scrolling warning is replaced by the amber AlertGlyph. Audio still plays.
                if (banner != null) banner.text = string.Empty;
            }

            if (_elapsed >= recordingLength)
            {
                _finished = true;
                if (banner != null && _elapsed >= LineEnd[LineEnd.Length - 1]) banner.text = string.Empty;
                OnFinished?.Invoke();
            }
        }
    }
}
