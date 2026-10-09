using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Drives the big neural network the pod flies through. The network is two combined meshes (connections and
    /// neurons), so it costs two draw calls. This view only feeds their shaders a few numbers: how far along the
    /// track the light has reached, an ignite flash, a flow clock that keeps the pulses racing, and a focus level per
    /// mesh. Solving a stop lights the connections around that neuron; the finale ignites the whole network.
    /// It also acts out the narration: while the Guide says "every glowing point is a neuron" the neurons flare and
    /// the connections step back, and the other way round for "every line is a connection" (cues from the dash, see
    /// RideNarrationData). The opening is staged on it: the intro starts in the dark, the network appears neuron by
    /// neuron while JFK speaks, flashes on "hard" and goes dark; when the rider pulls the lever that wakes AURA, light
    /// races out from the pod to the far end, then falls back as the Guide says something in it has gone wrong.
    /// It decides nothing about correctness: it listens to the director and the dash and draws.
    /// </summary>
    public sealed class NeuralCoreView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private DashScreenView dash;
        [SerializeField] private RideTheme theme;
        [SerializeField] private Renderer links;
        [SerializeField] private Renderer nodes;
        [SerializeField] private float startLit = 0.22f; // the first layers are awake at the dock, so the intro has a network to point at

        [Header("Narration cues")]
        [Tooltip("Brightness of the part being named (neurons or connections) while the Guide names it.")]
        [SerializeField] private float cueFocus = 1.8f;

        [Tooltip("Brightness of the other part while one is being named.")]
        [SerializeField] private float cueBackFocus = 0.3f;

        [Tooltip("Seconds the network flickers for \"something in it has gone wrong\".")]
        [SerializeField] private float flickerSeconds = 2.4f;

        [Tooltip("How fast the damaged network's brightness wanders. Keep it slow: the network fills much of the view in a headset, and more than three flashes a second can trigger photosensitive seizures (WCAG 2.3.1). Measured: at 2 the worst second has 2 flashes (mean 0.9/s); the old 9 reached 6.")]
        [SerializeField] private float flickerRate = 2f;

        [Header("Opening")]
        [Tooltip("How much of the network has appeared when JFK reaches \"not because they are easy\" (0 to 1).")]
        [SerializeField] private float jfkBuildReveal = 0.55f;

        [Tooltip("What is left after the flash on \"hard\": a few embers in the dark until the rider wakes AURA.")]
        [SerializeField] private float embersReveal = 0.1f;

        [Tooltip("Seconds the flash lands before the end of JFK's last clause, so it hits the word \"hard\".")]
        [SerializeField] private float flashLead = 0.35f;

        [Tooltip("Seconds the wake light takes to race from the pod to the far end of the network.")]
        [SerializeField] private float wakeWaveSeconds = 2.4f;

        private static readonly int LitUpToId = Shader.PropertyToID("_LitUpTo");
        private static readonly int IgniteId = Shader.PropertyToID("_Ignite");
        private static readonly int FlowId = Shader.PropertyToID("_Flow");
        private static readonly int FocusId = Shader.PropertyToID("_Focus");
        private static readonly int RevealId = Shader.PropertyToID("_Reveal");

        private Material _linkMaterial;
        private Material _nodeMaterial;
        private float _lit;
        private float _litTarget;
        private float _ignite;
        private float _igniteTarget;
        private float _flow;
        private float _flowRate = 1f;
        private float _igniteAge = -1f;
        private float _focus = 1f;
        private float _focusTarget = 1f;
        private float _boost;

        private float _nodeEmphasis = 1f;
        private float _linkEmphasis = 1f;
        private float _nodeEmphasisTarget = 1f;
        private float _linkEmphasisTarget = 1f;
        private float _flowRateTarget = 1f;
        private float _cueFlash;
        private float _flickerUntil = -1f;

        private float _reveal = 1f;
        private float _revealTarget = 1f;
        private float _revealRate = 1f;
        private float _flashAt = -1f;
        private float _litSpeed; // 0: the light eases toward its target; above 0: it sweeps at this many progress units a second

        private void OnEnable()
        {
            _linkMaterial = links != null ? links.material : null;
            _nodeMaterial = nodes != null ? nodes.material : null;
            _lit = _litTarget = startLit;
            _ignite = _igniteTarget = 0f;
            _igniteAge = -1f;
            _reveal = _revealTarget = 1f;
            _flashAt = -1f;
            _litSpeed = 0f;
            Push();

            if (dash != null)
            {
                dash.SegmentShown += OnSegment;
                dash.LineCleared += ClearCue;
            }

            if (director == null) return;
            director.StationSolved += OnSolved;
            director.OutroStarted += OnOutroStarted;
            director.StateChanged += OnState;
            director.RiderTaskDone += OnRiderTaskDone;
        }

        private void OnDisable()
        {
            if (dash != null)
            {
                dash.SegmentShown -= OnSegment;
                dash.LineCleared -= ClearCue;
            }

            if (director != null)
            {
                director.StationSolved -= OnSolved;
                director.OutroStarted -= OnOutroStarted;
                director.StateChanged -= OnState;
                director.RiderTaskDone -= OnRiderTaskDone;
            }

            if (_linkMaterial != null) Destroy(_linkMaterial);
            if (_nodeMaterial != null) Destroy(_nodeMaterial);
        }

        private void OnState(RideState state)
        {
            // While a stop is live the network steps back; in travel, at the dock and at the finale it takes the stage.
            _focusTarget = state == RideState.AtStop ? (theme != null ? theme.stopFocus : 0.4f) : 1f;
            if (state == RideState.AtStop) ClearCue();

            if (state == RideState.Intro)
            {
                // The opening starts in the dark; JFK's words bring the network in.
                _reveal = _revealTarget = 0f;
                _lit = _litTarget = 0f;
            }
            else if (state == RideState.Dock)
            {
                // However the intro ended (played through or skipped), the dock shows the awake network, first layers lit.
                Reveal(1f, 1.2f);
                _flashAt = -1f;
                _litTarget = startLit;
                _litSpeed = 0f;
            }
        }

        private void OnRiderTaskDone(string control, bool byRider)
        {
            if (control == RideControlIds.Action)
            {
                // AURA wakes: the whole network appears and light races out from the pod to the far end.
                Reveal(1f, 0.6f);
                _flashAt = -1f;
                _litTarget = 1.15f;
                _litSpeed = _litTarget / Mathf.Max(0.1f, wakeWaveSeconds);
                _cueFlash = Mathf.Max(_cueFlash, 0.9f);
            }
            else
            {
                _cueFlash = Mathf.Max(_cueFlash, 1.2f); // the neuron on the panel fired; the big network answers
                _boost = 1f;
            }
        }

        /// <summary>Moves the reveal to a level over a number of seconds.</summary>
        private void Reveal(float level, float seconds)
        {
            _revealTarget = level;
            _revealRate = Mathf.Abs(level - _reveal) / Mathf.Max(0.05f, seconds);
        }

        private void OnSolved(StationSummary summary)
        {
            _boost = 1f; // the reward: the network flares back to full for a moment as its light spreads
            float reach = theme != null ? theme.lightReach : 0.12f;
            _litTarget = Mathf.Max(_litTarget, director.Progress01 + reach);
        }

        private void OnOutroStarted()
        {
            _litTarget = 1.3f;
            _igniteAge = 0f;
        }

        // ---------- narration cues ----------

        private void OnSegment(RideLine line, int index, NarrationLibrary.Segment segment)
        {
            if (NarrationLibrary.HasCue(segment, "neurons"))
            {
                Emphasize(cueFocus, cueBackFocus, 0.6f);
            }
            else if (NarrationLibrary.HasCue(segment, "links"))
            {
                Emphasize(cueBackFocus, cueFocus, 2.2f); // the pulses race, so "a connection" visibly carries something
            }
            else
            {
                Emphasize(1f, 1f, 1f);
            }

            if (NarrationLibrary.HasCue(segment, "net_wake")) _cueFlash = Mathf.Max(_cueFlash, 0.7f);
            if (NarrationLibrary.HasCue(segment, "liftoff")) _cueFlash = Mathf.Max(_cueFlash, 0.9f);
            if (NarrationLibrary.HasCue(segment, "heart")) _cueFlash = Mathf.Max(_cueFlash, 0.8f);
            if (NarrationLibrary.HasCue(segment, "billions"))
            {
                _cueFlash = Mathf.Max(_cueFlash, 1.1f);
                _flowRateTarget = 2.5f;
            }

            if (NarrationLibrary.HasCue(segment, "net_broken"))
            {
                _flickerUntil = Time.time + flickerSeconds;
                _litTarget = Mathf.Min(_litTarget, startLit); // after the wake the light falls back: the network is damaged
                _litSpeed = 0f;
            }

            float clause = dash != null ? dash.CurrentSegmentSeconds : 0f;
            if (NarrationLibrary.HasCue(segment, "jfk_build")) Reveal(jfkBuildReveal, clause);
            if (NarrationLibrary.HasCue(segment, "jfk_hard"))
            {
                float untilHard = Mathf.Max(0.1f, clause - flashLead);
                Reveal(1f, untilHard);
                _flashAt = Time.time + untilHard;
            }
        }

        private void ClearCue() => Emphasize(1f, 1f, 1f);

        private void Emphasize(float node, float link, float flowRate)
        {
            _nodeEmphasisTarget = node;
            _linkEmphasisTarget = link;
            _flowRateTarget = flowRate;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float follow = theme != null ? theme.lightFollowSeconds : 2.5f;
            if (_litSpeed > 0f) _lit = Mathf.MoveTowards(_lit, _litTarget, dt * _litSpeed);
            else _lit = Mathf.Lerp(_lit, _litTarget, 1f - Mathf.Exp(-dt * 3f / Mathf.Max(0.1f, follow)));

            if (_flashAt >= 0f && Time.time >= _flashAt)
            {
                // "...because they are hard": everything flashes once, then the dark comes back.
                _flashAt = -1f;
                _cueFlash = Mathf.Max(_cueFlash, 1.3f);
                _reveal = 1f;
                Reveal(embersReveal, 1.6f);
            }

            _reveal = Mathf.MoveTowards(_reveal, _revealTarget, dt * _revealRate);
            _flowRate = Mathf.Lerp(_flowRate, _flowRateTarget, 1f - Mathf.Exp(-dt * 3f));
            _flow += dt * _flowRate;

            if (_igniteAge >= 0f)
            {
                // Flash up fast, settle to a steady glow: the whole network is awake.
                _igniteAge += dt;
                float rise = Mathf.Clamp01(_igniteAge / 0.8f);
                float settle = Mathf.Clamp01((_igniteAge - 0.8f) / Mathf.Max(0.1f, theme != null ? theme.igniteSeconds : 5f));
                _igniteTarget = Mathf.Lerp(rise, 0.35f, settle);
            }

            _ignite = Mathf.Lerp(_ignite, _igniteTarget, 1f - Mathf.Exp(-dt * 6f));
            _boost = Mathf.MoveTowards(_boost, 0f, dt / 2.5f);
            _focus = Mathf.Lerp(_focus, Mathf.Max(_focusTarget, _boost), 1f - Mathf.Exp(-dt * 3f));
            _cueFlash = Mathf.MoveTowards(_cueFlash, 0f, dt / 2.2f);
            float ease = 1f - Mathf.Exp(-dt * 5f);
            _nodeEmphasis = Mathf.Lerp(_nodeEmphasis, _nodeEmphasisTarget, ease);
            _linkEmphasis = Mathf.Lerp(_linkEmphasis, _linkEmphasisTarget, ease);
            Push();
        }

        /// <summary>A damaged network: brightness wanders unsteadily (slow smooth noise, so it never strobes; see flickerRate).</summary>
        private float Flicker()
        {
            if (Time.time >= _flickerUntil) return 1f;
            float n = Mathf.PerlinNoise(Time.time * flickerRate, 0.37f);
            return Mathf.Lerp(0.2f, 1f, n * n);
        }

        private void Push()
        {
            float flicker = Flicker();
            float ignite = _ignite + _cueFlash;
            if (_linkMaterial != null)
            {
                _linkMaterial.SetFloat(LitUpToId, _lit);
                _linkMaterial.SetFloat(IgniteId, ignite);
                _linkMaterial.SetFloat(FlowId, _flow);
                _linkMaterial.SetFloat(FocusId, _focus * _linkEmphasis * flicker);
                _linkMaterial.SetFloat(RevealId, _reveal);
            }

            if (_nodeMaterial != null)
            {
                _nodeMaterial.SetFloat(LitUpToId, _lit);
                _nodeMaterial.SetFloat(IgniteId, ignite);
                _nodeMaterial.SetFloat(FlowId, _flow);
                _nodeMaterial.SetFloat(FocusId, _focus * _nodeEmphasis * flicker);
                _nodeMaterial.SetFloat(RevealId, _reveal);
            }
        }
    }
}
