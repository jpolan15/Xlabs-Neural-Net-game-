using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// XR and physical interactor for adjusting synaptic weight regulators (W1, W2).
    /// Uses stepped detents (0.5 increments) to ensure predictable calibration.
    /// Sends commands to Gameplay.NeuralState; never evaluates puzzles directly.
    /// </summary>
    public class WeightRegulatorInteractor : MonoBehaviour
    {
        [Header("Target Socket")]
        [Tooltip("0 for W1 (Signal 1), 1 for W2 (Signal 2)")]
        [SerializeField] private int socketIndex = 0;

        [Header("Detent Settings")]
        [SerializeField] private double stepSize = 0.5;
        [SerializeField] private double minValue = -2.0;
        [SerializeField] private double maxValue = 2.0;

        [Header("Visual Elements")]
        [SerializeField] private Transform knobTransform;
        [SerializeField] private TextMesh dialLabelTextMesh;

        [Header("State Reference")]
        [SerializeField] private NeuralState neuralState;

        [Header("Audio Settings")]
        [SerializeField] private bool playDetentAudio = true;

        private AudioSource _audioSource;
        private static AudioClip _cachedDetentClip;

        public int SocketIndex => socketIndex;
        public double CurrentWeight => neuralState != null ? (socketIndex == 0 ? neuralState.Weight1 : neuralState.Weight2) : 0.0;

        public event Action<int, double> OnRegulatorAdjusted;

        private void Awake()
        {
            if (neuralState == null)
            {
                neuralState = FindAnyObjectByType<NeuralState>();
            }
            if (knobTransform == null)
            {
                knobTransform = transform;
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null && playDetentAudio)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1.0f; // 3D spatial audio
            }

            if (_cachedDetentClip == null)
            {
                _cachedDetentClip = CreateDetentClickClip();
            }
        }

        private void Start()
        {
            UpdateVisuals(CurrentWeight);
        }

        /// <summary>
        /// Adjusts weight by delta steps (e.g. +1 step = +0.5, -1 step = -0.5).
        /// Clamps to [minValue, maxValue].
        /// </summary>
        public void StepAdjust(int steps)
        {
            double newWeight = CurrentWeight + (steps * stepSize);
            newWeight = System.Math.Round(newWeight / stepSize) * stepSize;
            SetWeight(newWeight);
        }

        public void SetWeight(double value)
        {
            double clamped = System.Math.Max(minValue, System.Math.Min(maxValue, value));
            if (neuralState != null)
            {
                neuralState.SetWeight(socketIndex, clamped);
            }
            UpdateVisuals(clamped);
            PlayDetentFeedback();
            OnRegulatorAdjusted?.Invoke(socketIndex, clamped);
        }

        public void OnXRInteract()
        {
            // Cycle forward +0.5 on XR trigger/click, wrapping back at max
            double next = CurrentWeight + stepSize;
            if (next > maxValue) next = minValue;
            SetWeight(next);
        }

        private void PlayDetentFeedback()
        {
            if (playDetentAudio && _audioSource != null && _cachedDetentClip != null)
            {
                _audioSource.pitch = 0.9f + (float)((CurrentWeight - minValue) / (maxValue - minValue)) * 0.4f;
                _audioSource.PlayOneShot(_cachedDetentClip, 0.5f);
            }

            // Haptic dispatch
            SendHapticPulse(0.35f, 0.04f);
        }

        private void SendHapticPulse(float amplitude, float duration)
        {
            var right = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (right.isValid) right.SendHapticImpulse(0u, amplitude, duration);

            var left = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            if (left.isValid) left.SendHapticImpulse(0u, amplitude, duration);
        }

        private static AudioClip CreateDetentClickClip()
        {
            int sampleRate = 44100;
            float dur = 0.035f;
            int count = (int)(sampleRate * dur);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1.0f - (t / dur);
                data[i] = Mathf.Sin(2f * Mathf.PI * 1800f * t) * env * 0.3f;
            }

            AudioClip clip = AudioClip.Create("DetentClick", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void UpdateVisuals(double weight)
        {
            if (knobTransform != null)
            {
                // Rotate knob +/- 90 degrees based on [-2.0, +2.0]
                float angle = (float)(weight * 45.0);
                knobTransform.localRotation = Quaternion.Euler(0f, angle, 0f);
            }

            if (dialLabelTextMesh != null)
            {
                dialLabelTextMesh.text = $"W{socketIndex + 1}: {weight:+0.0;-0.0;0.0}";
            }
        }
    }
}
