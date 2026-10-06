using UnityEngine;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Payoff, fail pulse, hint pulse, cold-open glow, and synthesized tones.
    /// Reads evaluation. Does not decide it. No camera motion.
    /// </summary>
    public sealed class ChamberFeedbackVisual : MonoBehaviour
    {
        public const float MasterVolume = 0.35f;

        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private LiveEvaluationRelay live;

        ChamberPromptBoard _board;
        AudioSource _audio;
        AudioClip _tick;
        AudioClip _confirm;
        AudioClip _success;
        Renderer _curtain;
        MaterialPropertyBlock _block;
        Color _accent = AccentTimeline.Amber;
        float _time;
        bool _wasPassed;
        bool _revealed;
        float _revealTimer;
        int _revealStep;
        string _hintId;
        float _hintUntil;
        bool _coldPlayed;
        Transform _glow;
        Vector3 _glowScale;
        Transform _hintTarget;
        Vector3 _hintScale;
        ParticleSystem _sparks;
        TMPro.TMP_Text _testLabel;
        NeuronCauseVisual _cause;
        float _nextRay;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (live == null) live = FindAnyObjectByType<LiveEvaluationRelay>();
            _board = gameObject.AddComponent<ChamberPromptBoard>();
            gameObject.AddComponent<PlaytestEventLog>();
            _board.Set("Targeting offline.");
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _tick = Tone(880f, 0.06f);
            _confirm = Tone(520f, 0.12f);
            _success = Tone(660f, 0.28f);
            var curtain = GameObject.Find("PortalEnergyCurtain");
            if (curtain != null) _curtain = curtain.GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
            var cable = GameObject.Find("NeuralCable_1");
            if (cable != null)
            {
                _glow = cable.transform;
                _glowScale = _glow.localScale;
            }

            var sparkGo = GameObject.Find("EmergencySparkParticles");
            if (sparkGo != null) _sparks = sparkGo.GetComponent<ParticleSystem>();
            var button = GameObject.Find("RunTestButton");
            if (button != null) _testLabel = button.GetComponentInChildren<TMPro.TMP_Text>();
            var anchor = GameObject.Find("NeuronDisplayAnchor");
            if (anchor != null)
            {
                _cause = anchor.GetComponent<NeuronCauseVisual>();
                for (int i = 0; i < 4; i++)
                {
                    Transform card = anchor.transform.Find("Card" + i);
                    if (card != null && card.GetComponent<Collider>() == null) card.gameObject.AddComponent<BoxCollider>();
                }
            }
        }

        void OnEnable()
        {
            if (neuralState != null) neuralState.OnStateMutated += HandleMutated;
            if (chamberController != null) chamberController.OnEvaluationComplete += HandleEvaluation;
            if (live != null) live.OnHintControl += HandleHint;
        }

        void OnDisable()
        {
            if (neuralState != null) neuralState.OnStateMutated -= HandleMutated;
            if (chamberController != null) chamberController.OnEvaluationComplete -= HandleEvaluation;
            if (live != null) live.OnHintControl -= HandleHint;
        }

        void Update()
        {
            _time += Time.deltaTime;
            bool passed = chamberController != null && chamberController.LastEvaluation != null && chamberController.LastEvaluation.Passed;
            _accent = AccentTimeline.Step(_accent, passed, Time.deltaTime, _time, out float emission);
            Paint(emission);
            PulseHint();
            PulseGlow(passed);
            if (_revealed) AdvanceReveal(Time.deltaTime);
            if (_sparks != null && passed && _sparks.isPlaying) _sparks.Stop();
            if (_testLabel != null)
            {
                string next = passed ? "ENGAGE" : "SELF-TEST";
                if (_testLabel.text != next) _testLabel.text = next;
            }

            if (Time.unscaledTime >= _nextRay)
            {
                _nextRay = Time.unscaledTime + 0.2f;
                HoverCard();
            }
        }

        void HoverCard()
        {
            if (_cause == null || Camera.main == null) return;
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, 4f)) 
            {
                _cause.SetHoveredCase(-1);
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                if (hit.collider != null && hit.collider.gameObject.name == "Card" + i)
                {
                    _cause.SetHoveredCase(i);
                    return;
                }
            }
        }

        void HandleMutated()
        {
            if (_audio != null && _tick != null) _audio.PlayOneShot(_tick, MasterVolume);
            if (!_coldPlayed && neuralState != null && neuralState.Cable1Connected)
            {
                _coldPlayed = true;
                string ice = chamberController != null && chamberController.Curriculum != null
                    ? chamberController.Curriculum.InputNames[1]
                    : "input 2";
                _board.Set("Now " + ice + ".");
            }
        }

        void HandleEvaluation(PuzzleEvaluation eval)
        {
            if (eval == null) return;
            bool passed = eval.Passed;
            if (passed && !_wasPassed)
            {
                _wasPassed = true;
                if (_audio != null && _success != null) _audio.PlayOneShot(_success, MasterVolume);
                _revealed = true;
                _revealStep = 0;
                _revealTimer = 0f;
                _board.Set("That wire strength is called a weight.");
                var hud = FindAnyObjectByType<WorldSpaceHud>();
                if (hud != null) hud.SetShowMath(true);
            }
            else if (!passed)
            {
                _wasPassed = false;
                if (eval.PassedCases > 0 && _audio != null && _confirm != null) _audio.PlayOneShot(_confirm, MasterVolume * 0.7f);
            }
        }

        void AdvanceReveal(float dt)
        {
            _revealTimer += dt;
            if (_revealStep == 0 && _revealTimer > 2.5f)
            {
                _revealStep = 1;
                _board.Set("This dial is the bias.");
            }
            else if (_revealStep == 1 && _revealTimer > 5f)
            {
                _revealStep = 2;
                _revealed = false;
                _board.Set("The gate is opening.");
            }
        }

        void HandleHint(string id)
        {
            _hintId = id;
            _hintUntil = _time + 4f;
            string name = id == "0" ? "SliderTrack_W1" : id == "1" ? "SliderTrack_W2" : "ThresholdSquelchValve";
            var go = GameObject.Find(name);
            _hintTarget = go != null ? go.transform : null;
            if (_hintTarget != null) _hintScale = _hintTarget.localScale;
            _board.Set("Try the glowing control.");
        }

        void PulseHint()
        {
            if (_hintTarget == null) return;
            if (_time > _hintUntil)
            {
                _hintTarget.localScale = _hintScale;
                _hintTarget = null;
                return;
            }

            float wave = 1f + (0.08f * Mathf.Sin(_time * (2f * Mathf.PI * 0.5f)));
            _hintTarget.localScale = _hintScale * wave;
        }

        void PulseGlow(bool passed)
        {
            if (_glow == null || passed) return;
            if (neuralState != null && neuralState.Cable1Connected) return;
            float wave = 1f + (0.08f * Mathf.Sin(_time * (2f * Mathf.PI * 0.5f)));
            _glow.localScale = _glowScale * wave;
        }

        void Paint(float emission)
        {
            if (_curtain == null) return;
            _curtain.GetPropertyBlock(_block);
            _block.SetColor(EmissionId, _accent * emission);
            _curtain.SetPropertyBlock(_block);
        }

        static AudioClip Tone(float freq, float seconds)
        {
            const int rate = 44100;
            int count = Mathf.Max(1, Mathf.RoundToInt(rate * seconds));
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 18f);
            }

            var clip = AudioClip.Create("tone", count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
