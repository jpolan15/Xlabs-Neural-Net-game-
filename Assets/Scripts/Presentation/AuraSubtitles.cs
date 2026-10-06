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
        [SerializeField] private OpeningTeleprompter teleprompter;
        [SerializeField] private VoiceLinePlayer voicePlayer;
        [SerializeField] private float holdSeconds = 6f;

        private Coroutine _clear;
        private string _pending;

        private void Awake()
        {
            transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            if (onboarding == null) onboarding = FindAnyObjectByType<ChamberOnboardingController>();
            if (hints == null) hints = FindAnyObjectByType<FailureHintDirector>();
            if (voyage == null) voyage = FindAnyObjectByType<VoyageDirector>();
            if (teleprompter == null) teleprompter = FindAnyObjectByType<OpeningTeleprompter>();
            if (voicePlayer == null) voicePlayer = FindAnyObjectByType<VoiceLinePlayer>();
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

        void Update()
        {
            if (_pending == null || Busy()) return;
            string line = _pending;
            _pending = null;
            ShowNow(line);
        }

        bool Busy()
        {
            if (teleprompter != null && teleprompter.IsPlaying) return true;
            if (voicePlayer != null && voicePlayer.IsBusy) return true;
            return false;
        }

        /// <summary>Shows one line. Holds it while the opening recording or a voice line owns the banner.</summary>
        public void Show(string line)
        {
            if (Busy())
            {
                _pending = line ?? string.Empty;
                return;
            }
            ShowNow(line);
        }

        void ShowNow(string line)
        {
            if (subtitle != null) subtitle.text = line ?? string.Empty;
            if (legacyLine != null) legacyLine.text = line ?? string.Empty;
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
