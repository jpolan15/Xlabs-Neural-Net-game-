using System;
using System.Collections;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;

namespace Convergence.Gameplay
{
    /// <summary>
    /// Simplified 3-step onboarding for Level 1 — Guided Sandbox mode.
    ///
    /// Steps:
    ///   0. Awakening      — stasis pod opens, you wake up
    ///   1. ConnectSensors — snap both cables in (crystal auto-inserts when done)
    ///   2. TuneSensitivity — turn the dial until the sentry can tell friend from foe
    ///   3. FireTest       — pull the lever to run the diagnostic
    ///   4. Completed      — all clear!
    ///
    /// No countdown timer, no shield damage, no death. Pure experimentation.
    /// </summary>
    public enum OnboardingStep
    {
        Awakening = 0,
        ConnectSensors = 1,
        TuneSensitivity = 2,
        FireTest = 3,
        Completed = 4,

        // Legacy aliases kept for serialization compatibility
        ReconnectConduit = 1,
        TuneWeight = 2,
        InsertStepCrystal = 2,  // collapsed into TuneSensitivity
        TransmitPulse = 3
    }

    /// <summary>
    /// Experiential step-by-step guide for Level 1.
    /// Listens to NeuralState and ChamberController events to advance stages.
    /// </summary>
    public class ChamberOnboardingController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private GatewayController gatewayController;

        [Header("Stasis Pod References")]
        [SerializeField] private Transform stasisDoorLeft;
        [SerializeField] private Transform stasisDoorRight;
        [SerializeField] private Light stasisPodLight;

        [Header("Physical Guidance Lighting")]
        // 0=Cable1, 1=Cable2, 2=Sensitivity Slider, 3=Lever
        [SerializeField] private Light[] interactableLights;
        [SerializeField] private Light onboardingPromptLight;

        [Header("State")]
        [SerializeField] private OnboardingStep currentStep = OnboardingStep.Awakening;
        [SerializeField] private float awakeningDelay = 2.0f;

        private float _pulseTimer;
        private float _stepTimeElapsed;
        private int _progressiveHintIndex;

        public OnboardingStep CurrentStep => currentStep;

        public event Action<OnboardingStep> OnStepChanged;
        public event Action<string> OnAnnouncerVoicePrompt;

        /// <summary>
        /// Returns a short, friendly single-line prompt for the current step.
        /// Shown in the bottom action strip — no jargon, no all-caps walls of text.
        /// </summary>
        public string GetCurrentStepPrompt()
        {
            return currentStep switch
            {
                OnboardingStep.Awakening      => "Waking up… the facility AI is calling for help.",
                OnboardingStep.ConnectSensors => "Step 1 of 3 — Grab both sensor cables and plug them in.",
                OnboardingStep.TuneSensitivity => "Step 2 of 3 — Turn the sensitivity dial until all targets show green.",
                OnboardingStep.FireTest       => "Step 3 of 3 — Pull the lever to run the final test!",
                OnboardingStep.Completed      => "✓ All clear! The sentry is calibrated. Great work.",
                _                             => ""
            };
        }

        private void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (neuralState == null)       neuralState       = FindAnyObjectByType<NeuralState>();
            if (gatewayController == null) gatewayController = FindAnyObjectByType<GatewayController>();
        }

        private void OnEnable()
        {
            if (neuralState != null)
                neuralState.OnStateMutated += HandleNeuralStateMutated;
            if (chamberController != null)
                chamberController.OnEvaluationComplete += HandleEvaluationComplete;
        }

        private void OnDisable()
        {
            if (neuralState != null)
                neuralState.OnStateMutated -= HandleNeuralStateMutated;
            if (chamberController != null)
                chamberController.OnEvaluationComplete -= HandleEvaluationComplete;
        }

        private void Start()
        {
            StartCoroutine(RoutineAwakeningSequence());
        }

        private void Update()
        {
            // Pulse guidance lights for the active step only
            if (interactableLights != null && interactableLights.Length > 0)
            {
                _pulseTimer += Time.deltaTime * 3.5f;
                float pulseMultiplier = 1.0f + 0.35f * Mathf.Sin(_pulseTimer);

                for (int i = 0; i < interactableLights.Length; i++)
                {
                    if (interactableLights[i] == null) continue;
                    bool isActive = IsLightActiveForStep(i, currentStep);
                    float baseIntensity = isActive ? 1.6f : 0.08f;
                    interactableLights[i].intensity = isActive ? baseIntensity * pulseMultiplier : baseIntensity;
                }
            }

            // Progressive hints — faster than before so players don't get stuck
            if (currentStep != OnboardingStep.Awakening && currentStep != OnboardingStep.Completed)
            {
                _stepTimeElapsed += Time.deltaTime;
                CheckProgressiveHints();
            }
        }

        private void CheckProgressiveHints()
        {
            // First hint at 20 s, second at 45 s (was 40/85 — too slow)
            if (_stepTimeElapsed > 20f && _progressiveHintIndex == 0)
            {
                _progressiveHintIndex = 1;
                TriggerProgressiveHint(1);
            }
            else if (_stepTimeElapsed > 45f && _progressiveHintIndex == 1)
            {
                _progressiveHintIndex = 2;
                TriggerProgressiveHint(2);
            }
        }

        private void TriggerProgressiveHint(int hintLevel)
        {
            switch (currentStep)
            {
                case OnboardingStep.ConnectSensors:
                    OnAnnouncerVoicePrompt?.Invoke("Hint: walk up to the two glowing cables and click / grab each one to plug them in.");
                    break;

                case OnboardingStep.TuneSensitivity:
                    if (hintLevel == 1)
                        OnAnnouncerVoicePrompt?.Invoke("Hint: turn the sensitivity dial clockwise. You need it high enough that a single alert triggers the sentry.");
                    else
                        OnAnnouncerVoicePrompt?.Invoke("Hint: try setting sensitivity to around 1.0. The hologram will go green when all four scenarios are correct.");
                    break;

                case OnboardingStep.FireTest:
                    if (hintLevel == 1)
                        OnAnnouncerVoicePrompt?.Invoke("Hint: grab the big lever on the right and pull it down, or just press Space.");
                    else
                        OnAnnouncerVoicePrompt?.Invoke("Hint: check the target pods — the friendly drone (no hazard) should be safe, the other three should be intercepted.");
                    break;
            }
        }

        /// <summary>Returns which light indices should pulse for a given step.</summary>
        private bool IsLightActiveForStep(int lightIndex, OnboardingStep step)
        {
            return step switch
            {
                OnboardingStep.ConnectSensors   => lightIndex == 0 || lightIndex == 1,
                OnboardingStep.TuneSensitivity  => lightIndex == 2,
                OnboardingStep.FireTest         => lightIndex == 3,
                OnboardingStep.Completed        => true,
                _                               => false
            };
        }

        private IEnumerator RoutineAwakeningSequence()
        {
            currentStep = OnboardingStep.Awakening;
            _stepTimeElapsed = 0f;
            _progressiveHintIndex = 0;
            OnStepChanged?.Invoke(currentStep);
            OnAnnouncerVoicePrompt?.Invoke("Emergency: the facility AI is losing its mind. We need your help to fix it before it's too late.");

            yield return new WaitForSeconds(awakeningDelay);

            // Open stasis pod doors smoothly
            float elapsed = 0f;
            float duration = 1.8f;
            Vector3 leftStart  = stasisDoorLeft  != null ? stasisDoorLeft.localPosition  : Vector3.zero;
            Vector3 rightStart = stasisDoorRight != null ? stasisDoorRight.localPosition : Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                if (stasisDoorLeft  != null) stasisDoorLeft.localPosition  = leftStart  + new Vector3(-0.9f * t, 0, 0);
                if (stasisDoorRight != null) stasisDoorRight.localPosition = rightStart + new Vector3( 0.9f * t, 0, 0);

                yield return null;
            }

            TransitionToStep(OnboardingStep.ConnectSensors);
        }

        private void HandleNeuralStateMutated()
        {
            if (neuralState == null) return;

            _stepTimeElapsed = 0f;
            _progressiveHintIndex = 0;

            if (currentStep == OnboardingStep.ConnectSensors)
            {
                if (neuralState.Cable1Connected && neuralState.Cable2Connected)
                {
                    // Auto-configure: ensure Step activation is set before moving on
                    neuralState.AutoConfigureForLevel1();
                    TransitionToStep(OnboardingStep.TuneSensitivity);
                }
            }
            // TuneSensitivity doesn't auto-advance — the player decides when to pull the lever
        }

        private void HandleEvaluationComplete(PuzzleEvaluation eval)
        {
            if (eval != null && eval.Passed)
            {
                TransitionToStep(OnboardingStep.Completed);
            }
            else if (currentStep == OnboardingStep.FireTest && eval != null && !eval.Passed)
            {
                // Give specific, friendly feedback based on which cases failed
                string advice = BuildRetryAdvice(eval);
                OnAnnouncerVoicePrompt?.Invoke(advice);
            }
        }

        /// <summary>
        /// Builds a short, actionable "try this" message instead of "CONTAINMENT BREACH".
        /// </summary>
        private string BuildRetryAdvice(PuzzleEvaluation eval)
        {
            if (eval == null) return "Something went wrong — try pulling the lever again.";

            if (!eval.ActivationMatches)
                return "The sentry needs a Step crystal to make binary decisions. Check the socket.";

            bool friendlyFired = eval.Diagnostics != null && eval.Diagnostics.Count > 0 && eval.Diagnostics[0].ActualOutput >= 0.5;
            bool hazardMissed  = eval.Diagnostics != null && eval.PassedCases < eval.TotalCases - (friendlyFired ? 0 : 1);

            if (friendlyFired && hazardMissed)
                return "Close! The sensitivity is a bit off — try dialing it down slightly so the quiet room stays safe.";
            if (friendlyFired)
                return "The friendly drone was hit — the sensitivity is too high. Try turning it down a little.";
            if (hazardMissed)
                return "Some hazards slipped through — the sensitivity is too low. Turn the dial up a notch.";

            return $"Almost there — {eval.PassedCases}/{eval.TotalCases} correct. Adjust the dial and try again.";
        }

        public void TransitionToStep(OnboardingStep newStep)
        {
            currentStep = newStep;
            _stepTimeElapsed = 0f;
            _progressiveHintIndex = 0;
            OnStepChanged?.Invoke(currentStep);

            switch (newStep)
            {
                case OnboardingStep.ConnectSensors:
                    OnAnnouncerVoicePrompt?.Invoke("The sensors are offline. Grab those two glowing cables and plug them into the console.");
                    break;

                case OnboardingStep.TuneSensitivity:
                    OnAnnouncerVoicePrompt?.Invoke("Sensors live! Now turn the sensitivity dial. The sentry needs to react to real threats — but not to silence.");
                    break;

                case OnboardingStep.FireTest:
                    OnAnnouncerVoicePrompt?.Invoke("Looks good. Pull the lever to run the diagnostic across all four scenarios.");
                    break;

                case OnboardingStep.Completed:
                    OnAnnouncerVoicePrompt?.Invoke("Perfect calibration. The sentry is online and the blast doors are unsealing. You did it!");
                    break;
            }
        }
    }
}
