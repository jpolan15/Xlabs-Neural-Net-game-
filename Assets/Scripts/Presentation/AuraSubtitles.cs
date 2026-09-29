using System.Collections;
using TMPro;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// World-space subtitles for A.U.R.A. Lines come from gameplay events. This script does not decide them.
    /// </summary>
    public class AuraSubtitles : MonoBehaviour
    {
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private TextMesh legacyLine;
        [SerializeField] private ChamberOnboardingController onboarding;
        [SerializeField] private FailureHintDirector hints;
        [SerializeField] private VoyageDirector voyage;
        [SerializeField] private AudioSource voice;
        [SerializeField] private AudioClip lineClip;
        [SerializeField] private float holdSeconds = 6f;

        private Coroutine _clear;

        private void Awake()
        {
            if (onboarding == null) onboarding = FindAnyObjectByType<ChamberOnboardingController>();
            if (hints == null) hints = FindAnyObjectByType<FailureHintDirector>();
            if (voyage == null) voyage = FindAnyObjectByType<VoyageDirector>();
        }

        private void OnEnable()
        {
            if (onboarding != null) onboarding.OnAnnouncerVoicePrompt += Show;
            if (hints != null) hints.OnHint += Show;
            if (voyage != null) voyage.OnAuraLine += Show;
        }

        private void OnDisable()
        {
            if (onboarding != null) onboarding.OnAnnouncerVoicePrompt -= Show;
            if (hints != null) hints.OnHint -= Show;
            if (voyage != null) voyage.OnAuraLine -= Show;
        }

        /// <summary>Shows one line and plays the interface clip when one is assigned.</summary>
        public void Show(string line)
        {
            if (subtitle != null) subtitle.text = line ?? string.Empty;
            if (legacyLine != null) legacyLine.text = line ?? string.Empty;
            if (voice != null && lineClip != null) voice.PlayOneShot(lineClip);
            if (_clear != null) StopCoroutine(_clear);
            if (isActiveAndEnabled) _clear = StartCoroutine(ClearAfter());
        }

        private IEnumerator ClearAfter()
        {
            yield return new WaitForSeconds(holdSeconds);
            if (subtitle != null) subtitle.text = string.Empty;
            if (legacyLine != null) legacyLine.text = string.Empty;
        }
    }
}
