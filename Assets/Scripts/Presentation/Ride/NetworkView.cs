using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// One of the two real 49 → 16 → 10 networks (Region 3 untrained, Region 7 trained). Its meshes are baked by the
    /// builder from the same weights and the same Core forward pass the moment runs: a wire's width is its weight, a
    /// node's size and brightness its activation. This only moves the light: the layers light one by one as the Guide
    /// names them, the hero digit's light races through with the moment's clock, the answer glows, and the untrained
    /// network flickers red on "wrong". During the rewind it steps back so the blame burns brighter.
    /// </summary>
    public sealed class NetworkView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private DashScreenView dash;
        [SerializeField] private ForwardPassMoment moment;
        [SerializeField] private RideTheme theme;
        [SerializeField] private Renderer links;
        [SerializeField] private Renderer nodes;
        [SerializeField] private Transform[] outputNodes = new Transform[0];
        [SerializeField] private TMP_Text[] outputLabels = new TMP_Text[0];
        [SerializeField] private Transform guessGlow;
        [SerializeField] private Renderer guessGlowRenderer;
        [SerializeField] private Color answerColor = Color.white;
        [Tooltip("Cue that makes this network flicker red (the wrong guess), or empty.")]
        [SerializeField] private string flashCue = "";
        [Tooltip("While the ride is in this region, the network steps back (-1: never).")]
        [SerializeField] private int dimInRegion = -1;
        [SerializeField] private float flashSeconds = 2.6f;

        private static readonly int LitUpTo = Shader.PropertyToID("_LitUpTo");
        private static readonly int Focus = Shader.PropertyToID("_Focus");
        private static readonly int PosColor = Shader.PropertyToID("_PosColor");
        private static readonly int NegColor = Shader.PropertyToID("_NegColor");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock _links, _nodes, _glow;
        private Color _pos, _neg;
        private float _lit = -0.1f;
        private float _focus = 1f;
        private float _flashUntil = -1f;
        private int _guess = -1;
        private bool _racing;
        private Vector3 _glowScale;

        private void Awake()
        {
            _links = new MaterialPropertyBlock();
            _nodes = new MaterialPropertyBlock();
            _glow = new MaterialPropertyBlock();
            _pos = links.sharedMaterial.GetColor(PosColor);
            _neg = links.sharedMaterial.GetColor(NegColor);
            if (guessGlow != null)
            {
                _glowScale = guessGlow.localScale;
                guessGlow.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (moment != null)
            {
                moment.RaceStarted += OnRace;
                moment.Revealed += OnRevealed;
            }

            if (dash != null) dash.SegmentShown += OnCue;
            if (director != null) director.RegionChanged += OnRegion;
            Push();
        }

        private void OnDisable()
        {
            if (moment != null)
            {
                moment.RaceStarted -= OnRace;
                moment.Revealed -= OnRevealed;
            }

            if (dash != null) dash.SegmentShown -= OnCue;
            if (director != null) director.RegionChanged -= OnRegion;
        }

        private void OnRace()
        {
            _racing = true;
            _lit = -0.05f;
        }

        private void OnRevealed(int guess)
        {
            _racing = false;
            _lit = 1.2f;
            _guess = guess;
            ShowGuess(answerColor);
            Push();
        }

        private void OnCue(RideLine line, int index, NarrationLibrary.Segment segment)
        {
            if (moment != null && !moment.HasFinished && !_racing)
            {
                if (NarrationLibrary.HasCue(segment, "inputs")) _lit = Mathf.Max(_lit, 0.02f);
                if (NarrationLibrary.HasCue(segment, "hidden")) _lit = Mathf.Max(_lit, 0.52f);
                if (NarrationLibrary.HasCue(segment, "outputs")) _lit = Mathf.Max(_lit, 1.2f);
            }

            if (!string.IsNullOrEmpty(flashCue) && NarrationLibrary.HasCue(segment, flashCue))
            {
                _flashUntil = Time.time + flashSeconds;
                if (_guess < 0 && moment != null && moment.Guess >= 0) _guess = moment.Guess;
                ShowGuess(theme.wrongRed);
            }

            Push();
        }

        private void OnRegion(int region)
        {
            _focus = region == dimInRegion ? 0.3f : 1f;
            Push();
        }

        private void ShowGuess(Color color)
        {
            if (_guess < 0 || _guess >= outputNodes.Length) return;
            if (guessGlow != null)
            {
                guessGlow.position = outputNodes[_guess].position;
                guessGlow.gameObject.SetActive(true);
                guessGlowRenderer.GetPropertyBlock(_glow);
                Color c = color;
                c.a = 0.8f;
                _glow.SetColor(BaseColor, c);
                guessGlowRenderer.SetPropertyBlock(_glow);
            }

            for (int i = 0; i < outputLabels.Length; i++)
            {
                if (outputLabels[i] == null) continue;
                bool on = i == _guess;
                outputLabels[i].color = on ? color : theme.offWhite;
                outputLabels[i].transform.localScale = Vector3.one * (on ? 1.5f : 1f);
            }
        }

        private void Update()
        {
            bool flashing = Time.time < _flashUntil;
            if (_racing && moment != null)
            {
                _lit = -0.05f + moment.Progress * 1.25f;
                Push();
            }
            else if (flashing || _flashWasOn)
            {
                Push();
            }

            _flashWasOn = flashing;
            if (guessGlow != null && guessGlow.gameObject.activeSelf)
            {
                guessGlow.localScale = _glowScale * (1f + 0.15f * Mathf.Sin(Time.time * 5f));
            }
        }

        private bool _flashWasOn;

        private void Push()
        {
            if (links == null || nodes == null || _links == null) return;
            bool redNow = Time.time < _flashUntil && Mathf.Repeat(Time.time * 6f, 1f) < 0.55f;
            links.GetPropertyBlock(_links);
            _links.SetFloat(LitUpTo, _lit);
            _links.SetFloat(Focus, _focus);
            _links.SetColor(PosColor, redNow ? theme.wrongRed : _pos);
            _links.SetColor(NegColor, redNow ? theme.wrongRed * 0.7f : _neg);
            links.SetPropertyBlock(_links);
            nodes.GetPropertyBlock(_nodes);
            _nodes.SetFloat(LitUpTo, _lit);
            _nodes.SetFloat(Focus, _focus);
            nodes.SetPropertyBlock(_nodes);
        }
    }
}
