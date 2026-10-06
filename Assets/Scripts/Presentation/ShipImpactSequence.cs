using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Opening strike. Lights, sparks, a sliding asteroid field, and four alarm pulses.
    /// The camera is never moved.
    /// </summary>
    public class ShipImpactSequence : MonoBehaviour
    {
        static readonly float[] AlarmTimes = { 12.48f, 13.72f, 14.92f, 16.20f };

        [SerializeField] private ChamberOnboardingController onboarding;
        [SerializeField] private AsteroidDefenseDirector director;
        [SerializeField] private NeuronMachineVisual neuron;
        [SerializeField] private RetroAudioSynthesizer synth;
        [SerializeField] private Transform asteroidField;
        [SerializeField] private ParticleSystem sparks;
        [SerializeField] private AudioSource boomSource;
        [SerializeField] private AudioClip boomClip;
        [SerializeField] private Light[] alarmLights;
        [SerializeField] private Color alarmColor = new Color(1f, 0.18f, 0.12f);
        [SerializeField] private Color restoredColor = new Color(0.35f, 0.75f, 1f);
        [SerializeField] private float slideDistance = 28f;

        float _elapsed = -1f;
        int _alarmsFired;
        bool _axonsDark;
        bool _restored;
        Vector3 _fieldRest;
        bool _haveFieldRest;

        void Awake()
        {
            if (onboarding == null) onboarding = FindAnyObjectByType<ChamberOnboardingController>();
            if (director == null) director = FindAnyObjectByType<AsteroidDefenseDirector>();
            if (neuron == null) neuron = FindAnyObjectByType<NeuronMachineVisual>();
            if (synth == null) synth = FindAnyObjectByType<RetroAudioSynthesizer>();
            if (asteroidField == null)
            {
                GameObject field = GameObject.Find("FloatingAsteroidField");
                if (field != null) asteroidField = field.transform;
            }
        }

        void OnEnable()
        {
            if (onboarding != null) onboarding.OnOpeningBriefing += Begin;
            if (director != null) director.OnRestored += HandleRestored;
        }

        void OnDisable()
        {
            if (onboarding != null) onboarding.OnOpeningBriefing -= Begin;
            if (director != null) director.OnRestored -= HandleRestored;
        }

        public void Begin()
        {
            if (_elapsed >= 0f) return;
            _elapsed = 0f;
            if (asteroidField != null && !_haveFieldRest)
            {
                _fieldRest = asteroidField.position;
                _haveFieldRest = true;
                asteroidField.position = _fieldRest + new Vector3(0f, 0f, slideDistance);
            }
            if (sparks != null) sparks.Play();
            if (boomSource != null && boomClip != null) boomSource.PlayOneShot(boomClip, 0.9f);
            PaintLights(alarmColor, 2.2f);
        }

        void Update()
        {
            if (_elapsed < 0f || _restored) return;
            _elapsed += Time.deltaTime;

            bool dark = _elapsed >= 5f && _elapsed < 7.5f;
            if (dark != _axonsDark)
            {
                _axonsDark = dark;
                if (neuron != null) neuron.SetAxonsDark(dark);
                if (dark && sparks != null) sparks.Play();
            }

            if (_haveFieldRest && _elapsed >= 10f && _elapsed <= 12f)
            {
                float t = (_elapsed - 10f) / 2f;
                asteroidField.position = Vector3.Lerp(_fieldRest + new Vector3(0f, 0f, slideDistance), _fieldRest, Mathf.SmoothStep(0f, 1f, t));
            }

            if (_alarmsFired < AlarmTimes.Length && _elapsed >= AlarmTimes[_alarmsFired])
            {
                _alarmsFired++;
                if (synth != null) synth.Pulse(620f + (_alarmsFired * 40f), 0.35f);
                PaintLights(alarmColor, 3.4f);
            }
            else if (alarmLights != null && _elapsed > 1.2f)
            {
                float pulse = 0.7f + 0.3f * Mathf.Abs(Mathf.Sin(_elapsed * 6f));
                for (int i = 0; i < alarmLights.Length; i++)
                {
                    if (alarmLights[i] != null) alarmLights[i].intensity = pulse;
                }
            }
        }

        void HandleRestored()
        {
            _restored = true;
            if (neuron != null) neuron.SetAxonsDark(false);
            if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            PaintLights(restoredColor, 1.4f);
        }

        void PaintLights(Color color, float intensity)
        {
            if (alarmLights == null) return;
            for (int i = 0; i < alarmLights.Length; i++)
            {
                if (alarmLights[i] == null) continue;
                alarmLights[i].color = color;
                alarmLights[i].intensity = intensity;
            }
        }
    }
}
