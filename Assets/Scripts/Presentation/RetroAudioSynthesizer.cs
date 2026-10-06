using UnityEngine;

namespace Convergence.Presentation
{
    [RequireComponent(typeof(AudioSource))]
    public class RetroAudioSynthesizer : MonoBehaviour
    {
        [Header("Synth Settings")]
        [Range(0f, 1f)]
        public float volume = 0.5f;
        public float baseFrequency = 440f;
        
        [Header("Vibrato (Telstar Style)")]
        public float vibratoSpeed = 6.0f;
        public float vibratoDepth = 10.0f;

        private float phase = 0f;
        private float sampleRate = 48000f;
        private float _pulseUntil;
        private float _savedVolume;
        private bool _pulsing;

        public void Pulse(float frequency, float seconds)
        {
            if (!_pulsing) _savedVolume = volume;
            _pulsing = true;
            baseFrequency = frequency;
            volume = 0.45f;
            _pulseUntil = Time.unscaledTime + seconds;
        }

        void Update()
        {
            if (!_pulsing) return;
            if (Time.unscaledTime < _pulseUntil) return;
            volume = _savedVolume;
            _pulsing = false;
        }

        void Awake()
        {
            sampleRate = AudioSettings.outputSampleRate;
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.playOnAwake = true;
                audioSource.spatialBlend = 0f; // 2D sound
                audioSource.Play();
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            float time = (float)AudioSettings.dspTime;

            for (int i = 0; i < data.Length; i += channels)
            {
                // Calculate vibrato
                float currentFreq = baseFrequency + Mathf.Sin(time * Mathf.PI * 2 * vibratoSpeed) * vibratoDepth;
                
                // Phase increment
                float increment = currentFreq * 2f * Mathf.PI / sampleRate;
                phase += increment;
                
                if (phase > 2f * Mathf.PI) phase -= 2f * Mathf.PI;

                // Simple Sawtooth/Square hybrid (Clavioline-ish)
                float value = (phase / Mathf.PI) - 1f; 
                if (value > 0.5f) value = 0.5f;
                if (value < -0.5f) value = -0.5f;

                // Simple envelope / filtering could go here, but this raw tone gives the 60s sci-fi feel
                value *= volume;

                for (int c = 0; c < channels; c++)
                {
                    data[i + c] = value;
                }
                
                time += 1f / sampleRate;
            }
        }
    }
}
