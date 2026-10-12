using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Region 5: every wire of the untrained network burns by its real blame |∂loss/∂w| (one mesh, baked by the
    /// builder from the same Core backward pass the moment runs), with the light flowing backward from the answers to
    /// the pixels as the Guide says "backwards". The few brightest wires pulse on their own mesh until the rider nudges
    /// them; a nudged wire cools. The dash shows the real step: the weight before and after, and the loss of the hero 3.
    /// </summary>
    public sealed class BlameView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private DashScreenView dash;
        [SerializeField] private BlameMoment moment;
        [SerializeField] private Renderer blameLinks;
        [SerializeField] private MeshFilter topWires;
        [SerializeField] private int vertsPerWire = 16;
        [SerializeField] private float flowSeconds = 4f;

        private static readonly int LitUpTo = Shader.PropertyToID("_LitUpTo");
        private MaterialPropertyBlock _block;
        private Mesh _mesh;
        private Color[] _colors;
        private float _flowStarted = -1f;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _mesh = Instantiate(topWires.sharedMesh);
            topWires.sharedMesh = _mesh;
            _colors = _mesh.colors;
            SetLit(-0.1f);
        }

        private void OnEnable()
        {
            if (dash != null) dash.SegmentShown += OnCue;
            if (moment != null) moment.Nudged += OnNudged;
        }

        private void OnDisable()
        {
            if (dash != null) dash.SegmentShown -= OnCue;
            if (moment != null) moment.Nudged -= OnNudged;
        }

        private void OnCue(RideLine line, int index, NarrationLibrary.Segment segment)
        {
            if (NarrationLibrary.HasCue(segment, "backwards") && _flowStarted < 0f) _flowStarted = Time.time;
        }

        private void Update()
        {
            if (_flowStarted < 0f) return;
            float t = Mathf.Clamp01((Time.time - _flowStarted) / flowSeconds);
            SetLit(-0.05f + t * 1.25f);
            if (t >= 1f) _flowStarted = -2f;
        }

        private void SetLit(float value)
        {
            if (blameLinks == null) return;
            blameLinks.GetPropertyBlock(_block);
            _block.SetFloat(LitUpTo, value);
            blameLinks.SetPropertyBlock(_block);
        }

        private void OnNudged(int k, double before, double after)
        {
            int at = k * vertsPerWire;
            for (int i = at; i < at + vertsPerWire && i < _colors.Length; i++) _colors[i].r = 0.05f;
            _mesh.colors = _colors;
            if (dash != null)
            {
                dash.ShowReadout("WIRE NUDGED  " + before.ToString("0.00") + " > " + after.ToString("0.00")
                    + "   LOSS FOR THIS 3: " + moment.StartLoss.ToString("0.00") + " > " + moment.HeroLoss.ToString("0.00"));
            }
        }
    }
}
