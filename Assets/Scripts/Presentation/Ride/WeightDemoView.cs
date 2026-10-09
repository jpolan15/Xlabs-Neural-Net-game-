using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// One connection, up close: an input neuron, a wire whose thickness and colour are its weight, and the neuron it
    /// feeds, which glows as much as it is told to. While the Guide says "every connection has a number called a
    /// weight" it holds a weight of +1; on "change the weights, and you change what the AI thinks" the weight sweeps
    /// through negative and back so the glow follows it. At the dock the rider takes over: the ROCK lever is the weight
    /// (the director unlocks it there), so "a weight is how much one neuron listens to another" is something they do.
    /// In the intro the rider gets a goal first: while the director waits for the ROCK lever to reach a value, the
    /// neuron stays small until the weight reaches that line, then fires (it pops and stays big): a neuron fires or it
    /// doesn't. Observes Gameplay; decides nothing (the director owns the goal's value and says when it is met).
    /// </summary>
    public sealed class WeightDemoView : MonoBehaviour
    {
        private enum Mode
        {
            Hold,
            Sweep,
            Lever
        }

        [SerializeField] private RideDirector director;
        [SerializeField] private DashScreenView dash;
        [SerializeField] private RideControlSet controls;
        [SerializeField] private RideTheme theme;

        [SerializeField] private Transform wire;
        [SerializeField] private Renderer wireRenderer;
        [SerializeField] private Material positiveMaterial;
        [SerializeField] private Material negativeMaterial;
        [SerializeField] private Transform outputGlow;
        [SerializeField] private Transform pulse;
        [SerializeField] private TMP_Text weightLabel;
        [SerializeField] private TMP_Text caption;

        [Tooltip("Local x where the wire starts and ends (the two neurons).")]
        [SerializeField] private float wireStart = -1.0f;
        [SerializeField] private float wireEnd = 1.0f;
        [SerializeField] private float minThickness = 0.02f;
        [SerializeField] private float maxThickness = 0.17f;
        [SerializeField] private float maxWeight = 2f;

        [SerializeField] private string captionHold = "A WEIGHT: HOW MUCH ONE NEURON LISTENS TO ANOTHER";
        [SerializeField] private string captionSweep = "CHANGE THE WEIGHT, CHANGE THE ANSWER";
        [SerializeField] private string captionLever = "TRY IT: MOVE THE ROCK LEVER";
        [Tooltip("{0} is the weight the director asks for, so the rider has a number to aim at.")]
        [SerializeField] private string captionGoal = "MAKE IT FIRE: ROCK LEVER TO {0}";
        [SerializeField] private string captionFired = "IT FIRED!";

        [Tooltip("How much bigger the neuron's glow pops for a moment when it fires.")]
        [SerializeField] private float firePop = 1.7f;

        private Mode _mode;
        private float _weight = 1f;
        private float _t;
        private float _pulseAt;
        private int _shownTenths = int.MinValue;
        private bool _shownPositive = true;
        private Vector3 _glowScale;
        private string[] _labels;
        private float _fireAt = float.PositiveInfinity; // the goal's weight while the intro asks the rider to make it fire
        private float _pop;
        private Color _captionColor;
        private FontStyles _captionStyle;

        private void Awake()
        {
            if (outputGlow != null) _glowScale = outputGlow.localScale;
            if (caption != null)
            {
                _captionColor = caption.color;
                _captionStyle = caption.fontStyle;
            }

            // Every label the weight can show, made once, so the label never allocates while it moves.
            int steps = Mathf.RoundToInt(maxWeight * 10f);
            _labels = new string[steps * 2 + 1];
            for (int i = -steps; i <= steps; i++) _labels[i + steps] = "WEIGHT  " + (i / 10f).ToString("+0.0;-0.0;0.0");
        }

        private void OnEnable()
        {
            _t = 0f;
            SetMode(director != null && director.State == RideState.Dock ? Mode.Lever : Mode.Hold);
            if (dash != null) dash.SegmentShown += OnSegment;
            if (director == null) return;
            director.StateChanged += OnState;
            director.RiderTaskStarted += OnTaskStarted;
            director.RiderTaskDone += OnTaskDone;
        }

        private void OnDisable()
        {
            if (dash != null) dash.SegmentShown -= OnSegment;
            if (director == null) return;
            director.StateChanged -= OnState;
            director.RiderTaskStarted -= OnTaskStarted;
            director.RiderTaskDone -= OnTaskDone;
        }

        private void OnTaskStarted(string control, float atLeast)
        {
            if (control != RideControlIds.Rock) return;
            SetMode(Mode.Lever);
            _fireAt = atLeast;
            if (caption != null) caption.text = string.Format(captionGoal, atLeast.ToString("+0.0;-0.0"));
        }

        private void OnTaskDone(string control, bool byRider)
        {
            if (control != RideControlIds.Rock) return;
            _pop = 1f;
            if (caption == null) return;
            caption.text = captionFired;
            caption.fontStyle = FontStyles.Bold;
            if (theme != null) caption.color = theme.cyan;
        }

        private void OnSegment(RideLine line, int index, NarrationLibrary.Segment segment)
        {
            if (NarrationLibrary.HasCue(segment, "weights")) SetMode(Mode.Hold);
            else if (NarrationLibrary.HasCue(segment, "weights_change")) SetMode(Mode.Sweep);
        }

        private void OnState(RideState state)
        {
            if (state == RideState.Dock) SetMode(Mode.Lever);
        }

        private void SetMode(Mode mode)
        {
            _mode = mode;
            _t = 0f;
            _fireAt = float.PositiveInfinity;
            if (caption == null) return;
            caption.color = _captionColor;
            caption.fontStyle = _captionStyle;
            caption.text = mode == Mode.Lever ? captionLever : mode == Mode.Sweep ? captionSweep : captionHold;
        }

        private float TargetWeight()
        {
            switch (_mode)
            {
                case Mode.Sweep:
                    return maxWeight * Mathf.Sin(_t * 1.3f); // through negative and back, about five seconds a cycle
                case Mode.Lever:
                    RideControlChannel rock = controls != null ? controls.Get(RideControlIds.Rock) : null;
                    return rock != null ? rock.Value : 1f;
                default:
                    return 1f;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            _weight = Mathf.MoveTowards(_weight, Mathf.Clamp(TargetWeight(), -maxWeight, maxWeight), dt * 4f);
            float strength = Mathf.Abs(_weight) / maxWeight;

            if (wire != null)
            {
                Vector3 s = wire.localScale;
                s.y = Mathf.Lerp(minThickness, maxThickness, strength);
                wire.localScale = s;
            }

            bool positive = _weight >= 0f;
            if (positive != _shownPositive)
            {
                _shownPositive = positive;
                if (wireRenderer != null) wireRenderer.sharedMaterial = positive ? positiveMaterial : negativeMaterial;
                if (weightLabel != null && theme != null) weightLabel.color = positive ? theme.cyan : theme.amber;
            }

            int tenths = Mathf.RoundToInt(_weight * 10f);
            if (tenths != _shownTenths && weightLabel != null)
            {
                _shownTenths = tenths;
                int steps = (_labels.Length - 1) / 2;
                weightLabel.text = _labels[Mathf.Clamp(tenths + steps, 0, _labels.Length - 1)];
            }

            // The input fires a steady signal of 1, so the neuron hears signal x weight: bright for positive, dark for negative.
            // With a goal, it stays small below the goal's line and fires (big) once the weight reaches it.
            float heard = Mathf.Clamp01(_weight / maxWeight);
            float size = 0.35f + 0.95f * heard;
            if (!float.IsPositiveInfinity(_fireAt)) size = _weight >= _fireAt - 0.01f ? 1.3f : 0.3f + 0.35f * heard;
            _pop = Mathf.MoveTowards(_pop, 0f, dt * 1.6f);
            if (outputGlow != null) outputGlow.localScale = _glowScale * (size * (1f + (firePop - 1f) * _pop));

            if (pulse != null)
            {
                bool flowing = _weight > 0.05f;
                if (pulse.gameObject.activeSelf != flowing) pulse.gameObject.SetActive(flowing);
                if (flowing)
                {
                    _pulseAt = Mathf.Repeat(_pulseAt + dt * (0.25f + 0.6f * strength), 1f);
                    Vector3 p = pulse.localPosition;
                    p.x = Mathf.Lerp(wireStart, wireEnd, _pulseAt);
                    pulse.localPosition = p;
                }
            }
        }
    }
}
