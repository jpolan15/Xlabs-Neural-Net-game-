using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// Physical hand-lever / mechanical slam button triggering a master Clock Pulse (Forward Pass).
    /// Can be grabbed and pulled down in VR or clicked in Desktop mode.
    /// Commands the ChamberController to trigger an evaluation pass.
    /// </summary>
    public class ClockPulseLeverInteractor : MonoBehaviour
    {
        [Header("Controller Reference")]
        [SerializeField] private ChamberController chamberController;

        [Header("Lever Geometry")]
        [SerializeField] private Transform leverArm;
        [SerializeField] private Renderer buttonGlowRenderer;
        [SerializeField] private Light statusLight;

        [Header("Colors")]
        [SerializeField] private Color readyColor = new Color(0.0f, 0.9f, 1.0f);
        [SerializeField] private Color firingColor = new Color(1.0f, 0.85f, 0.0f);

        [Header("Audio Settings")]
        [SerializeField] private bool playAudio = true;

        private AudioSource _audioSource;
        private static AudioClip _cachedClunkClip;
        private float _pullAnimationTimer = 0f;

        private void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null && playAudio)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1.0f;
            }

            if (_cachedClunkClip == null)
            {
                _cachedClunkClip = CreateLeverClunkClip();
            }
        }

        private void Update()
        {
            if (_pullAnimationTimer > 0f)
            {
                _pullAnimationTimer -= Time.deltaTime;
                float p = Mathf.Clamp01(_pullAnimationTimer / 0.3f);
                if (leverArm != null)
                {
                    leverArm.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 40f, p), 0, 0);
                }

                if (buttonGlowRenderer != null && buttonGlowRenderer.material != null)
                {
                    buttonGlowRenderer.material.SetColor("_EmissionColor", firingColor * 3.0f);
                }
            }
            else
            {
                if (leverArm != null)
                {
                    leverArm.localRotation = Quaternion.identity;
                }
                if (buttonGlowRenderer != null && buttonGlowRenderer.material != null)
                {
                    buttonGlowRenderer.material.SetColor("_EmissionColor", readyColor * 1.5f);
                }
            }
        }

        /// <summary>
        /// Pulls the lever and fires the clock cycle forward pass.
        /// </summary>
        public void PullLever()
        {
            _pullAnimationTimer = 0.35f;

            if (playAudio && _audioSource != null && _cachedClunkClip != null)
            {
                _audioSource.PlayOneShot(_cachedClunkClip, 0.8f);
            }

            SendHapticPulse(0.85f, 0.15f);

            if (chamberController != null)
            {
                chamberController.TriggerForwardPass();
            }
        }

        public void OnXRInteract()
        {
            PullLever();
        }

        private void SendHapticPulse(float amplitude, float duration)
        {
            var right = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (right.isValid) right.SendHapticImpulse(0u, amplitude, duration);

            var left = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            if (left.isValid) left.SendHapticImpulse(0u, amplitude, duration);
        }

        private static AudioClip CreateLeverClunkClip()
        {
            int sampleRate = 44100;
            float dur = 0.18f;
            int count = (int)(sampleRate * dur);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-18.0f * t);
                float freq = Mathf.Lerp(120f, 45f, t / dur);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.7f;
            }

            AudioClip clip = AudioClip.Create("LeverClunk", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
