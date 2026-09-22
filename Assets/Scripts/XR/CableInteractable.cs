using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// Interactable conduit representing physical neural connections.
    /// Supports reconnecting/cutting conduits with tools.
    /// Updates NeuralState.CableConnected state.
    /// </summary>
    public class CableInteractable : MonoBehaviour
    {
        [Header("Cable Socket Identity")]
        [Tooltip("0 for Input 1 conduit, 1 for Input 2 conduit")]
        [SerializeField] private int cableIndex = 0;

        [Header("State Reference")]
        [SerializeField] private NeuralState neuralState;

        [Header("Audio Settings")]
        [SerializeField] private bool playAudio = true;

        private AudioSource _audioSource;
        private static AudioClip _cachedConnectClip;
        private static AudioClip _cachedDisconnectClip;

        public int CableIndex => cableIndex;
        public bool IsConnected => neuralState != null ? (cableIndex == 0 ? neuralState.Cable1Connected : neuralState.Cable2Connected) : true;

        public event Action<int, bool> OnConnectionStateChanged;

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

            if (_cachedConnectClip == null) _cachedConnectClip = CreateSnapClip(true);
            if (_cachedDisconnectClip == null) _cachedDisconnectClip = CreateSnapClip(false);
        }

        public void Connect()
        {
            SetConnected(true);
        }

        public void Disconnect()
        {
            SetConnected(false);
        }

        public void ToggleConnection()
        {
            SetConnected(!IsConnected);
        }

        public void OnXRInteract()
        {
            ToggleConnection();
        }

        private void SetConnected(bool connected)
        {
            if (neuralState != null)
            {
                neuralState.SetCableConnected(cableIndex, connected);
            }

            if (playAudio && _audioSource != null)
            {
                AudioClip clip = connected ? _cachedConnectClip : _cachedDisconnectClip;
                if (clip != null) _audioSource.PlayOneShot(clip, 0.6f);
            }

            SendHapticPulse(0.5f, 0.08f);
            OnConnectionStateChanged?.Invoke(cableIndex, connected);
        }

        private void SendHapticPulse(float amplitude, float duration)
        {
            var right = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (right.isValid) right.SendHapticImpulse(0u, amplitude, duration);

            var left = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            if (left.isValid) left.SendHapticImpulse(0u, amplitude, duration);
        }

        private static AudioClip CreateSnapClip(bool isConnect)
        {
            int sampleRate = 44100;
            float dur = 0.08f;
            int count = (int)(sampleRate * dur);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1.0f - (t / dur);
                float freq = isConnect ? Mathf.Lerp(400f, 1200f, t / dur) : Mathf.Lerp(1200f, 300f, t / dur);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create(isConnect ? "CableSnapIn" : "CableSnapOut", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
