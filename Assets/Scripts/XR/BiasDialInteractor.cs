using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// XR and physical interactor for adjusting the Threshold Squelch Valve / Noise Filter (b).
    /// Regulates the firing threshold so background noise in clean conditions does not trigger alarms.
    /// Uses stepped detents (0.5 increments), analog gauge needle feedback, and pneumatic hiss sound effects.
    /// Strictly XR layer: sends commands to Gameplay.NeuralState.
    /// </summary>
    public class BiasDialInteractor : MonoBehaviour
    {
        [Header("Detent Settings")]
        [SerializeField] private double stepSize = 0.5;
        [SerializeField] private double minValue = -2.0;
        [SerializeField] private double maxValue = 2.0;

        [Header("Visual Elements")]
        [SerializeField] private Transform valveWheelTransform;
        [SerializeField] private Transform gaugeNeedleTransform;
        [SerializeField] private TextMesh biasLabelTextMesh;
        [SerializeField] private Renderer valveGlowRenderer;

        [Header("State Reference")]
        [SerializeField] private NeuralState neuralState;

        [Header("Audio Settings")]
        [SerializeField] private bool playDetentAudio = true;

        private AudioSource _audioSource;
        private static AudioClip _cachedDetentClip;

        public double CurrentBias => neuralState != null ? neuralState.Bias : 0.0;

        public event Action<double> OnBiasDialAdjusted;

        private void Awake()
        {
            if (neuralState == null)
            {
                neuralState = FindAnyObjectByType<NeuralState>();
            }
            if (valveWheelTransform == null)
            {
                valveWheelTransform = transform;
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null && playDetentAudio)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1.0f;
            }

            if (_cachedDetentClip == null)
            {
                _cachedDetentClip = CreateValveHissClickClip();
            }
        }

        private void Start()
        {
            UpdateVisuals(CurrentBias);
        }

        public void StepAdjust(int steps)
        {
            double newBias = CurrentBias + (steps * stepSize);
            newBias = Math.Round(newBias / stepSize) * stepSize;
            SetBias(newBias);
        }

        public void SetBias(double value)
        {
            double clamped = Math.Max(minValue, Math.Min(maxValue, value));
            if (neuralState != null)
            {
                neuralState.SetBias(clamped);
            }
            UpdateVisuals(clamped);
            PlayDetentFeedback();
            OnBiasDialAdjusted?.Invoke(clamped);
        }

        public void OnXRInteract()
        {
            double next = CurrentBias + stepSize;
            if (next > maxValue) next = minValue;
            SetBias(next);
        }

        private void PlayDetentFeedback()
        {
            if (playDetentAudio && _audioSource != null && _cachedDetentClip != null)
            {
                _audioSource.pitch = 0.85f + (float)((CurrentBias - minValue) / (maxValue - minValue)) * 0.4f;
                _audioSource.PlayOneShot(_cachedDetentClip, 0.55f);
            }

            SendHapticPulse(0.35f, 0.04f);
        }

        private void SendHapticPulse(float amplitude, float duration)
        {
            var right = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (right.isValid) right.SendHapticImpulse(0u, amplitude, duration);

            var left = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            if (left.isValid) left.SendHapticImpulse(0u, amplitude, duration);
        }

        private static AudioClip CreateValveHissClickClip()
        {
            int sampleRate = 44100;
            float dur = 0.05f;
            int count = (int)(sampleRate * dur);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 50f);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.2f;
                float tone = Mathf.Sin(2f * Mathf.PI * 1100f * t) * 0.35f;
                data[i] = (tone + noise) * env;
            }

            AudioClip clip = AudioClip.Create("ValveSquelch", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void UpdateVisuals(double bias)
        {
            if (valveWheelTransform != null)
            {
                float angle = (float)(bias * 45.0);
                valveWheelTransform.localRotation = Quaternion.Euler(0f, angle, 0f);
            }

            if (gaugeNeedleTransform != null)
            {
                float needleAngle = (float)(bias * 35.0);
                gaugeNeedleTransform.localRotation = Quaternion.Euler(0f, 0f, -needleAngle);
            }

            if (valveGlowRenderer != null && valveGlowRenderer.material != null)
            {
                Color col = bias < 0 ? new Color(0.1f, 0.9f, 0.6f) : (bias > 0 ? new Color(1.0f, 0.6f, 0.1f) : new Color(0.3f, 0.4f, 0.5f));
                valveGlowRenderer.material.SetColor("_EmissionColor", col * 1.5f);
            }

            if (biasLabelTextMesh != null)
            {
                string tag = bias < 0 ? "SUPPRESS FALSE ALARMS" : (bias > 0 ? "HYPER-SENSITIVE" : "NEUTRAL");
                biasLabelTextMesh.text = $"THRESHOLD SQUELCH (b): {bias:+0.0;-0.0;0.0}\n[{tag}]";
            }
        }
    }
}
