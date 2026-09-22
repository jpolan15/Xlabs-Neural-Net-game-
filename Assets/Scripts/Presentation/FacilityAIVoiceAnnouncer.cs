using System;
using System.Collections;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Facility AI Announcer (A.U.R.A.) inspired by GLaDOS/Aperture Science in Portal.
    /// Provides subtle, atmospheric radio audio chimes and a sleek, translucent lower-third
    /// cinematic subtitle bar that automatically fades in and out.
    /// Strictly presentation layer: observes Gameplay.ChamberOnboardingController events.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FacilityAIVoiceAnnouncer : MonoBehaviour
    {
        [Header("State Source")]
        [SerializeField] private ChamberOnboardingController onboardingController;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip radioChimeClip;
        [SerializeField] private float chimeFrequency = 587.33f; // D5 chime

        [Header("Subtitle Display Settings")]
        [SerializeField] private TextMesh worldSubtitleTextMesh;
        [SerializeField] private float displayDuration = 5.0f;
        [SerializeField] private float fadeDuration = 0.5f;

        private AudioSource _audioSource;
        private string _currentSubtitle = "";
        private float _subtitleAlpha = 0.0f;
        private Coroutine _subtitleCoroutine;
        private Texture2D _whiteTexture;

        // Styling
        private readonly Color _boxBg = new Color(0.02f, 0.05f, 0.08f, 0.85f);
        private readonly Color _boxBorder = new Color(0.0f, 0.85f, 1.0f, 0.75f);
        private readonly Color _aiCyan = new Color(0.2f, 0.95f, 1.0f, 1.0f);

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            if (onboardingController == null)
            {
                onboardingController = FindAnyObjectByType<ChamberOnboardingController>();
            }

            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();

            if (worldSubtitleTextMesh != null)
            {
                worldSubtitleTextMesh.text = "";
            }
        }

        private void OnEnable()
        {
            if (onboardingController != null)
            {
                onboardingController.OnAnnouncerVoicePrompt += SpeakLine;
            }
        }

        private void OnDisable()
        {
            if (onboardingController != null)
            {
                onboardingController.OnAnnouncerVoicePrompt -= SpeakLine;
            }
        }

        private string _lastSpokenLine = "";
        private float _lastSpokenTime = -10f;

        public void SpeakLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (text == _lastSpokenLine && Time.time - _lastSpokenTime < 4.0f) return;

            _lastSpokenLine = text;
            _lastSpokenTime = Time.time;

            PlayRadioChime();

            if (_subtitleCoroutine != null)
            {
                StopCoroutine(_subtitleCoroutine);
            }
            _subtitleCoroutine = StartCoroutine(RoutineShowSubtitle(text));
        }

        private void PlayRadioChime()
        {
            if (radioChimeClip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(radioChimeClip, 0.7f);
            }
            else
            {
                // Procedural two-tone Portal-style radio chirp
                CreateSynthesizedChirp();
            }
        }

        private void CreateSynthesizedChirp()
        {
            int sampleRate = 44100;
            float dur = 0.15f;
            int count = (int)(sampleRate * dur);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float freq = (t < 0.07f) ? 587.33f : 880f; // D5 -> A5 chime
                float env = Mathf.Sin((t / dur) * Mathf.PI);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.25f;
            }

            AudioClip chirp = AudioClip.Create("RadioChirp", count, 1, sampleRate, false);
            chirp.SetData(data, 0);

            if (_audioSource != null)
            {
                _audioSource.PlayOneShot(chirp, 0.6f);
            }
        }

        private IEnumerator RoutineShowSubtitle(string text)
        {
            _currentSubtitle = text;
            if (worldSubtitleTextMesh != null)
            {
                worldSubtitleTextMesh.text = $"A.U.R.A. — {text}";
            }

            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _subtitleAlpha = Mathf.Clamp01(elapsed / fadeDuration);
                if (worldSubtitleTextMesh != null)
                {
                    worldSubtitleTextMesh.color = new Color(_aiCyan.r, _aiCyan.g, _aiCyan.b, _subtitleAlpha);
                }
                yield return null;
            }

            _subtitleAlpha = 1.0f;
            if (worldSubtitleTextMesh != null)
            {
                worldSubtitleTextMesh.color = _aiCyan;
            }
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _subtitleAlpha = 1.0f - Mathf.Clamp01(elapsed / fadeDuration);
                if (worldSubtitleTextMesh != null)
                {
                    worldSubtitleTextMesh.color = new Color(_aiCyan.r, _aiCyan.g, _aiCyan.b, _subtitleAlpha);
                }
                yield return null;
            }

            _subtitleAlpha = 0.0f;
            _currentSubtitle = "";
            if (worldSubtitleTextMesh != null)
            {
                worldSubtitleTextMesh.text = "";
            }
        }

        private void OnGUI()
        {
            if (_subtitleAlpha <= 0.01f || string.IsNullOrEmpty(_currentSubtitle)) return;

            float boxW = Mathf.Min(700f, Screen.width - 40f);
            float boxH = 50f;
            Rect subRect = new Rect((Screen.width - boxW) * 0.5f, Screen.height - boxH - 24f, boxW, boxH);

            Color prev = GUI.color;
            Color bg = _boxBg;
            bg.a *= _subtitleAlpha;
            Color border = _boxBorder;
            border.a *= _subtitleAlpha;

            DrawRect(subRect, bg);
            DrawBorder(subRect, border, 1);

            // Announcer Tag
            GUIStyle tagStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(_aiCyan.r, _aiCyan.g, _aiCyan.b, _subtitleAlpha) }
            };
            GUI.Label(new Rect(subRect.x + 16, subRect.y + 6, boxW - 32, 16), "A.U.R.A.", tagStyle);

            // Subtitle text
            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(1f, 1f, 1f, _subtitleAlpha) }
            };
            GUI.Label(new Rect(subRect.x + 16, subRect.y + 18, boxW - 32, 26), _currentSubtitle, textStyle);

            GUI.color = prev;
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _whiteTexture);
            GUI.color = prev;
        }

        private void DrawBorder(Rect rect, Color color, int width)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, width), color);
            DrawRect(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            DrawRect(new Rect(rect.x, rect.y, width, rect.height), color);
            DrawRect(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
        }
    }
}
