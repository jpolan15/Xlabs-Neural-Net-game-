using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// Tactile kinetic power slider / throttle interactor for adjusting synaptic sensitivity weights (W1, W2).
    /// Features detented stepping (0.5 increments), illuminated plasma core gauge tube,
    /// mechanical ratchet audio feedback, and dual-hand VR haptics.
    /// Strictly XR layer: translates physical user interactions into Gameplay commands (NeuralState).
    /// </summary>
    public class KineticWeightSliderInteractor : MonoBehaviour
    {
        [Header("Target Socket")]
        [Tooltip("0 for W1 (Sensor 1 Sensitivity), 1 for W2 (Sensor 2 Sensitivity)")]
        [SerializeField] private int socketIndex = 0;
        [SerializeField] private string sensorName = "Radiation Sentry Sensor";

        [Header("Slider Settings")]
        [SerializeField] private double stepSize = 0.5;
        [SerializeField] private double minValue = -2.0;
        [SerializeField] private double maxValue = 2.0;
        [SerializeField] private float travelDistance = 0.28f; // Physical slider track length in meters

        [Header("Mechanical Visual Elements")]
        [SerializeField] private Transform sliderHandle;
        [SerializeField] private Renderer plasmaGaugeRenderer;
        [SerializeField] private Light handleGlowLight;
        [SerializeField] private TextMesh valueLabelTextMesh;

        [Header("Colors")]
        [SerializeField] private Color positiveExcitationCyan = new Color(0.1f, 0.85f, 1.0f, 1.0f);
        [SerializeField] private Color negativeInhibitionViolet = new Color(0.85f, 0.2f, 1.0f, 1.0f);
        [SerializeField] private Color neutralColor = new Color(0.2f, 0.25f, 0.35f, 0.5f);

        [Header("State Reference")]
        [SerializeField] private NeuralState neuralState;

        private AudioSource _audioSource;
        private static AudioClip _cachedRatchetClip;
        private Vector3 _handleOriginLocal;

        public int SocketIndex => socketIndex;
        public string SensorName => sensorName;
        public double CurrentWeight => neuralState != null ? (socketIndex == 0 ? neuralState.Weight1 : neuralState.Weight2) : 0.0;

        public event Action<int, double> OnSliderAdjusted;

        private void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (sliderHandle != null) _handleOriginLocal = sliderHandle.localPosition;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1.0f;
            }

            if (_cachedRatchetClip == null)
            {
                _cachedRatchetClip = CreateRatchetClickClip();
            }
        }

        private void Start()
        {
            UpdateVisuals(CurrentWeight);
        }

        public void StepAdjust(int steps)
        {
            double newWeight = CurrentWeight + (steps * stepSize);
            newWeight = Math.Round(newWeight / stepSize) * stepSize;
            SetWeight(newWeight);
        }

        public void SetWeight(double value)
        {
            double clamped = Math.Max(minValue, Math.Min(maxValue, value));
            if (neuralState != null)
            {
                neuralState.SetWeight(socketIndex, clamped);
            }
            UpdateVisuals(clamped);
            PlayRatchetFeedback();
            OnSliderAdjusted?.Invoke(socketIndex, clamped);
        }

        public void OnXRInteract()
        {
            // Cycle forward +0.5 on XR trigger/click, wrapping back at max
            double next = CurrentWeight + stepSize;
            if (next > maxValue) next = minValue;
            SetWeight(next);
        }

        private void PlayRatchetFeedback()
        {
            if (_audioSource != null && _cachedRatchetClip != null)
            {
                float pitch = 0.85f + (float)((CurrentWeight - minValue) / (maxValue - minValue)) * 0.5f;
                _audioSource.pitch = pitch;
                _audioSource.PlayOneShot(_cachedRatchetClip, 0.6f);
            }

            SendHapticPulse(0.4f, 0.045f);
        }

        private void SendHapticPulse(float amplitude, float duration)
        {
            var right = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (right.isValid) right.SendHapticImpulse(0u, amplitude, duration);

            var left = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            if (left.isValid) left.SendHapticImpulse(0u, amplitude, duration);
        }

        private void UpdateVisuals(double weight)
        {
            // Position slider handle linearly along Z or Y axis
            if (sliderHandle != null)
            {
                float normalized = (float)((weight - minValue) / (maxValue - minValue)); // 0.0 to 1.0
                float offset = (normalized - 0.5f) * travelDistance;
                sliderHandle.localPosition = _handleOriginLocal + new Vector3(0, 0, offset);
            }

            Color activeColor = (weight > 0.05) ? positiveExcitationCyan : ((weight < -0.05) ? negativeInhibitionViolet : neutralColor);
            float intensity = (float)(Math.Abs(weight) / 2.0) * 2.5f + 0.5f;

            if (plasmaGaugeRenderer != null && plasmaGaugeRenderer.material != null)
            {
                plasmaGaugeRenderer.material.color = activeColor;
                plasmaGaugeRenderer.material.SetColor("_EmissionColor", activeColor * intensity);
            }

            if (handleGlowLight != null)
            {
                handleGlowLight.color = activeColor;
                handleGlowLight.intensity = intensity * 1.5f;
            }

            if (valueLabelTextMesh != null)
            {
                string tag = (weight > 0) ? "EXCITATORY" : ((weight < 0) ? "INHIBITORY" : "MUTED");
                valueLabelTextMesh.text = $"W{socketIndex + 1}: {weight:+0.0;-0.0;0.0} [{tag}]";
            }
        }

        private static AudioClip CreateRatchetClickClip()
        {
            int sampleRate = 44100;
            float dur = 0.04f;
            int count = (int)(sampleRate * dur);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 80f);
                data[i] = (Mathf.Sin(2f * Mathf.PI * 1200f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 2400f * t)) * env * 0.4f;
            }

            AudioClip clip = AudioClip.Create("RatchetClick", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
