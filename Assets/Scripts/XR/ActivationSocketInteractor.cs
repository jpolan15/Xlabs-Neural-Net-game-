using System;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// Physical/XR socket interactor that accepts insertable activation crystals.
    /// Mutates NeuralState.Activation only upon physical insertion or removal of crystals.
    /// Never fakes or hardcodes activation state.
    /// </summary>
    public class ActivationSocketInteractor : MonoBehaviour
    {
        [Header("State Reference")]
        [SerializeField] private NeuralState neuralState;

        [Header("Socket Status")]
        [SerializeField] private bool hasCrystalInserted = true;
        [SerializeField] private ActivationType currentCrystalType = ActivationType.Linear;

        [Header("Audio Settings")]
        [SerializeField] private bool playAudio = true;

        private AudioSource _audioSource;
        private static AudioClip _cachedCrystalClip;

        public bool HasCrystal => hasCrystalInserted;
        public ActivationType CurrentCrystal => currentCrystalType;

        public event Action<ActivationType> OnCrystalInserted;
        public event Action OnCrystalRemoved;

        private void Awake()
        {
            if (neuralState == null)
            {
                neuralState = FindAnyObjectByType<NeuralState>();
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null && playAudio)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1.0f;
            }

            if (_cachedCrystalClip == null) _cachedCrystalClip = CreateCrystalResonanceClip();
        }

        private void Start()
        {
            if (neuralState != null)
            {
                // Reflect starting state from NeuralState
                currentCrystalType = neuralState.Activation;
                hasCrystalInserted = true;
            }
        }

        /// <summary>
        /// Inserts an activation crystal into the socket and updates the live network model.
        /// </summary>
        public void InsertCrystal(ActivationType type)
        {
            hasCrystalInserted = true;
            currentCrystalType = type;
            if (neuralState != null)
            {
                neuralState.SetActivation(type);
            }

            if (playAudio && _audioSource != null && _cachedCrystalClip != null)
            {
                _audioSource.pitch = type switch
                {
                    ActivationType.Step => 1.25f,
                    ActivationType.ReLU => 1.0f,
                    ActivationType.Sigmoid => 1.1f,
                    _ => 0.9f
                };
                _audioSource.PlayOneShot(_cachedCrystalClip, 0.7f);
            }

            SendHapticPulse(0.6f, 0.08f);
            OnCrystalInserted?.Invoke(type);
        }

        /// <summary>
        /// Removes the crystal from the socket. Defaults to Linear (raw pre-activation pass-through).
        /// </summary>
        public void RemoveCrystal()
        {
            hasCrystalInserted = false;
            currentCrystalType = ActivationType.Linear;
            if (neuralState != null)
            {
                neuralState.SetActivation(ActivationType.Linear);
            }
            SendHapticPulse(0.3f, 0.04f);
            OnCrystalRemoved?.Invoke();
        }

        /// <summary>
        /// Convenience cycle method for desktop keyboard fallback (Tab key) and XR interact.
        /// Cycles through Linear -> ReLU -> Step -> Sigmoid.
        /// </summary>
        public void CycleCrystal()
        {
            ActivationType next;
            switch (currentCrystalType)
            {
                case ActivationType.Linear:
                    next = ActivationType.ReLU;
                    break;
                case ActivationType.ReLU:
                    next = ActivationType.Step;
                    break;
                case ActivationType.Step:
                    next = ActivationType.Sigmoid;
                    break;
                default:
                    next = ActivationType.Linear;
                    break;
            }
            InsertCrystal(next);
        }

        public void OnXRInteract()
        {
            CycleCrystal();
        }

        private void SendHapticPulse(float amplitude, float duration)
        {
            var right = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (right.isValid) right.SendHapticImpulse(0u, amplitude, duration);

            var left = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            if (left.isValid) left.SendHapticImpulse(0u, amplitude, duration);
        }

        private static AudioClip CreateCrystalResonanceClip()
        {
            int sampleRate = 44100;
            float dur = 0.25f;
            int count = (int)(sampleRate * dur);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-10.0f * t);
                data[i] = (Mathf.Sin(2f * Mathf.PI * 880f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 1760f * t)) * env * 0.3f;
            }

            AudioClip clip = AudioClip.Create("CrystalResonance", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
