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
    ///   2. TuneSensitivity — turn the dials until either beacon wakes the array
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

        /// <summary>Fired once when the opening strike begins. Presentation plays the recording from here.</summary>
        public event Action OnOpeningBriefing;

        /// <summary>
        /// Returns a short, friendly single-line prompt for the current step.
        /// Shown in the bottom action strip — no jargon, no all-caps walls of text.
        /// </summary>
        public string GetCurrentStepPrompt()
        {
            return currentStep switch
            {
                OnboardingStep.Awakening      => "Two cables popped out. You will plug them back in.",
                OnboardingStep.ConnectSensors => "Grab the ROCK cable and click it in. Then do the same for ICE.",
                OnboardingStep.TuneSensitivity => "Left dial is ROCK. Right dial is ICE. The third dial moves every flier together.",
                OnboardingStep.FireTest       => "Watch the lamps. ROCK or ICE should light FIRE. The drone should not.",
                OnboardingStep.Completed      => "The laser learned the rule. The door opens when the swarm is done.",
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
            {
                neuralState.OnStateMutated += HandleNeuralStateMutated;
                neuralState.OnCableStateChanged += HandleCableChanged;
            }
            if (chamberController != null)
                chamberController.OnEvaluationComplete += HandleEvaluationComplete;
        }

        private void OnDisable()
        {
            if (neuralState != null)
            {
                neuralState.OnStateMutated -= HandleNeuralStateMutated;
                neuralState.OnCableStateChanged -= HandleCableChanged;
            }
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
            if (_stepTimeElapsed > 8f && _progressiveHintIndex == 0)
            {
                _progressiveHintIndex = 1;
                TriggerProgressiveHint(1);
            }
            else if (_stepTimeElapsed > 22f && _progressiveHintIndex == 1)
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
                    OnAnnouncerVoicePrompt?.Invoke("The glowing cables are loose. Pick one up and click it into the console.");
                    break;

                case OnboardingStep.TuneSensitivity:
                    if (hintLevel == 1)
                        OnAnnouncerVoicePrompt?.Invoke("A rock alone should burn. Ice alone should burn. The green drone, with neither, should dock.");
                    else
                        OnAnnouncerVoicePrompt?.Invoke("If a rock gets through, its dial is too low. If the drone burns, the third dial is too high.");
                    break;

                case OnboardingStep.FireTest:
                    if (hintLevel == 1)
                        OnAnnouncerVoicePrompt?.Invoke("SELF-TEST runs all four fliers at once. Or just watch the next one come in.");
                    else
                        OnAnnouncerVoicePrompt?.Invoke("Change one dial, then let the next flier show you the result.");
                    break;
            }
        }

        /// <summary>Returns which light indices should pulse for a given step.</summary>
        private bool IsLightActiveForStep(int lightIndex, OnboardingStep step)
        {
            return step switch
            {
                OnboardingStep.ConnectSensors   => lightIndex == 0 || lightIndex == 1,
                OnboardingStep.TuneSensitivity  => lightIndex == 2 || lightIndex == 3 || lightIndex == 4,
                OnboardingStep.FireTest         => lightIndex == 6,
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
            OnOpeningBriefing?.Invoke();

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

        private void HandleCableChanged(int index, bool connected)
        {
            if (currentStep != OnboardingStep.ConnectSensors || neuralState == null) return;
            if (neuralState.Cable1Connected && neuralState.Cable2Connected) return;

            if (neuralState.Cable1Connected)
                OnAnnouncerVoicePrompt?.Invoke("ROCK is in. The ICE cable is still loose.");
            else if (neuralState.Cable2Connected)
                OnAnnouncerVoicePrompt?.Invoke("ICE is in. The ROCK cable is still loose.");
        }

        static string DescribeMiss(PuzzleEvaluation eval)
        {
            if (eval.Diagnostics == null) return "Change one dial, then watch the next flier.";
            for (int i = 0; i < eval.Diagnostics.Count; i++)
            {
                CaseDiagnostic diag = eval.Diagnostics[i];
                if (diag == null || diag.IsCorrect) continue;
                string name = i switch
                {
                    0 => "The repair drone",
                    1 => "The icy comet",
                    2 => "The rocky asteroid",
                    _ => "The rock-and-ice chunk"
                };
                bool fired = diag.ActualOutput >= 0.5;
                if (!fired && diag.ExpectedOutput >= 0.5)
                {
                    if (i == 1) return name + " got through. Turn the ICE dial up. The sum has to reach zero.";
                    if (i == 2) return name + " got through. Turn the ROCK dial up. The sum has to reach zero.";
                    return name + " got through. Either sensor dial can lift it. The sum has to reach zero.";
                }
                return name + " burned. It has neither rock nor ice. Lower the third dial so an empty pair stays dark.";
            }
            return "Change one dial, then watch the next flier.";
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
                if (currentStep != OnboardingStep.Completed)
                {
                    TransitionToStep(OnboardingStep.Completed);
                }
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
            if (eval == null) return "Change one dial, then watch the next flier.";

            if (!eval.ActivationMatches)
                return "The crystal in the socket should be Step. The dials can be right and the laser still wrong.";

            return DescribeMiss(eval);
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
                    OnAnnouncerVoicePrompt?.Invoke("Grab the glowing ROCK cable and click it in. Then the ICE cable.");
                    break;

                case OnboardingStep.TuneSensitivity:
                    OnAnnouncerVoicePrompt?.Invoke("Sensors are live. Watch the lamps on the next flier, then turn one dial.");
                    break;

                case OnboardingStep.FireTest:
                    OnAnnouncerVoicePrompt?.Invoke("ROCK or ICE should light FIRE. The drone should stay dark and dock.");
                    break;

                case OnboardingStep.Completed:
                    OnAnnouncerVoicePrompt?.Invoke("It has the rule. Watch the swarm. The door opens at the end.");
                    break;
            }
        }
    }
}
