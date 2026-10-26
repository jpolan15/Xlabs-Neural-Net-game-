using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Region 5: every wire of the untrained network burns by its real blame |∂loss/∂w| (one mesh, baked by the
    /// builder from the same Core backward pass the moment runs), with the light flowing backward from the answers to
    /// the pixels as the Guide says "backwards". The few brightest wires each wear a pulsing handle on their loose end.
    /// A wire a hand has grabbed dims in place and a bright cable runs from its fixed end to wherever the laser points,
    /// with a pulse of blame flowing back along it; a gold glow marks the 3 it is to be dragged to, and swells as the
    /// end comes near. When the wire snaps on, the cable locks onto the 3, the 3 flashes, sparks fly, the hand buzzes, and
    /// the wire cools. The dash shows the real step: the weight before and after, and the loss of the hero 3.
    /// Every glow (the handles, the pulses, the 3's glow and flash) is a camera-facing quad in one mesh, so the whole
    /// hunt costs one draw call plus the cables. Display only: the moment decides when a wire has reached the 3.
    /// </summary>
    public sealed class BlameView : MonoBehaviour
    {
        private const int CablePoints = 24;

        [SerializeField] private RideDirector director;
        [SerializeField] private DashScreenView dash;
        [SerializeField] private BlameMoment moment;
        [SerializeField] private Renderer blameLinks;
        [SerializeField] private MeshFilter topWires;
        [SerializeField] private int vertsPerWire = 16;
        [SerializeField] private float flowSeconds = 4f;

        [Header("Dragging wires to the 3")]
        [Tooltip("Where each top wire is fixed (the pixel or hidden neuron end).")]
        [SerializeField] private Transform[] origins = new Transform[0];
        [Tooltip("Where each top wire's loose end hangs until a hand takes it; its handle waits there.")]
        [SerializeField] private Transform[] ends = new Transform[0];
        [Tooltip("The cable drawn from each wire's fixed end to its handle (or to the 3 once connected).")]
        [SerializeField] private LineRenderer[] cables = new LineRenderer[0];
        [Tooltip("Holds the one mesh of glows (an additive glow material with vertex colour); the view fills it each frame.")]
        [SerializeField] private MeshFilter glows;
        [SerializeField] private ParticleSystem sparks;
        [Tooltip("Metres across: the network is big, so its handles and pulses are too.")]
        [SerializeField] private float handleSize = 2.2f;
        [SerializeField] private float pulseSize = 1.4f;
        [SerializeField] private Color heldColor = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color connectedColor = new Color(0.4f, 1f, 0.7f, 1f);
        [SerializeField] private Color socketColor = new Color(1f, 0.82f, 0.3f, 0.55f);
        [SerializeField] private float snapBuzz = 0.75f;
        [SerializeField] private float snapBuzzSeconds = 0.12f;

        private static readonly int LitUpTo = Shader.PropertyToID("_LitUpTo");
        private readonly Vector3[] _points = new Vector3[CablePoints];
        private MaterialPropertyBlock _block;
        private Mesh _mesh;
        private Color[] _colors;
        private Color[] _heat;
        private float _flowStarted = -1f;
        private float _flash;
        private Camera _camera;

        // The glows: per wire a handle and a pulse, then the 3's glow, then its flash. Four vertices each.
        private Mesh _glowMesh;
        private Vector3[] _glowVerts;
        private Color[] _glowColors;
        private int _glowCount;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _mesh = Instantiate(topWires.sharedMesh);
            topWires.sharedMesh = _mesh;
            _colors = _mesh.colors;
            _heat = (Color[])_colors.Clone();
            SetLit(-0.1f);
            BuildGlowMesh();
        }

        private void BuildGlowMesh()
        {
            if (glows == null) return;
            _glowCount = origins.Length * 2 + 2;
            _glowVerts = new Vector3[_glowCount * 4];
            _glowColors = new Color[_glowCount * 4];
            var uv = new Vector2[_glowCount * 4];
            var triangles = new int[_glowCount * 6];
            for (int i = 0; i < _glowCount; i++)
            {
                int v = i * 4, t = i * 6;
                uv[v] = new Vector2(0f, 0f);
                uv[v + 1] = new Vector2(1f, 0f);
                uv[v + 2] = new Vector2(1f, 1f);
                uv[v + 3] = new Vector2(0f, 1f);
                triangles[t] = v;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v;
                triangles[t + 4] = v + 3;
                triangles[t + 5] = v + 2;
            }

            _glowMesh = new Mesh { name = "BlameGlows" };
            _glowMesh.MarkDynamic();
            _glowMesh.vertices = _glowVerts;
            _glowMesh.uv = uv;
            _glowMesh.triangles = triangles;
            _glowMesh.colors = _glowColors;
            glows.sharedMesh = _glowMesh;
            // The vertices are in world space (the quads face the rider wherever they are), so the mesh sits at the origin.
            glows.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _glowMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
        }

        private void OnEnable()
        {
            if (dash != null) dash.SegmentShown += OnCue;
            if (moment == null) return;
            moment.Nudged += OnNudged;
            moment.Grabbed += OnGrabbed;
            moment.Dropped += OnDropped;
            moment.Connected += OnConnected;
        }

        private void OnDisable()
        {
            if (dash != null) dash.SegmentShown -= OnCue;
            if (moment == null) return;
            moment.Nudged -= OnNudged;
            moment.Grabbed -= OnGrabbed;
            moment.Dropped -= OnDropped;
            moment.Connected -= OnConnected;
        }

        private void Start()
        {
            foreach (LineRenderer cable in cables) if (cable != null) cable.enabled = false;
            if (glows != null) glows.gameObject.SetActive(false);
        }

        private void OnCue(RideLine line, int index, NarrationLibrary.Segment segment)
        {
            if (NarrationLibrary.HasCue(segment, "back") && _flowStarted < 0f) _flowStarted = Time.time;
        }

        private void Update()
        {
            if (_flowStarted >= 0f)
            {
                float t = Mathf.Clamp01((Time.time - _flowStarted) / flowSeconds);
                SetLit(-0.05f + t * 1.25f);
                if (t >= 1f) _flowStarted = -2f;
            }

            UpdateWires();
        }

        private void SetLit(float value)
        {
            if (blameLinks == null) return;
            blameLinks.GetPropertyBlock(_block);
            _block.SetFloat(LitUpTo, value);
            blameLinks.SetPropertyBlock(_block);
        }

        // ---------- the dragged wires ----------

        private int HandCarrying(int wire)
        {
            if (moment.Carrying(RideHand.Left) == wire) return RideHand.Left;
            return moment.Carrying(RideHand.Right) == wire ? RideHand.Right : -1;
        }

        private void UpdateWires()
        {
            if (moment == null || moment.Socket == null || _glowMesh == null) return;
            bool live = moment.IsLive;
            if (_camera == null) _camera = Camera.main;
            Vector3 eye = _camera != null ? _camera.transform.position : moment.Socket.position - Vector3.forward * 30f;
            Vector3 socket = moment.Socket.position;
            float radius = moment.SnapRadiusFrom(eye);
            bool near = false;
            bool any = live;

            for (int i = 0; i < _glowCount; i++) Glow(i, Vector3.zero, 0f, Color.clear);

            int wires = Mathf.Min(origins.Length, Mathf.Min(ends.Length, cables.Length));
            for (int k = 0; k < wires; k++)
            {
                bool connected = moment.IsNudged(k);
                int hand = HandCarrying(k);
                Vector3 from = origins[k].position;
                Vector3 end = default;
                bool held = hand >= 0 && moment.TryGetCursor(hand, out end);
                if (held) near |= Vector3.Distance(end, socket) <= radius * 1.8f;
                else if (connected) end = socket;

                LineRenderer line = cables[k];
                line.enabled = held || connected;
                any |= line.enabled;

                // The handle: pulsing at the loose end, bigger and following the laser once grabbed.
                if (live && !connected)
                {
                    float beat = 1f + 0.18f * Mathf.Sin(Time.time * 6f + k);
                    Glow(k * 2, held ? end : ends[k].position, handleSize * (held ? 1.35f : 1f) * beat, heldColor);
                }

                if (!line.enabled) continue;
                DrawCable(line, from, end, connected ? connectedColor : heldColor);
                // Blame flows back: a pulse runs from the 3 (or the hand) toward the wire's fixed end.
                float u = Mathf.Repeat(Time.time * 0.9f + k * 0.23f, 1f);
                Glow(k * 2 + 1, Cable(from, end, 1f - u), pulseSize * (0.7f + 0.5f * Mathf.Sin(u * Mathf.PI)), Color.white);
            }

            // The 3: a gold glow the size of the snap zone, swelling as a wire's end comes near, and a flash when one snaps on.
            if (live)
            {
                float swell = near ? 1.25f : 1f;
                float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 5f);
                Glow(_glowCount - 2, socket, radius * 3.2f * swell * pulse, socketColor);
            }

            _flash = Mathf.MoveTowards(_flash, 0f, Time.deltaTime / 0.45f);
            if (_flash > 0f) Glow(_glowCount - 1, socket, radius * (1.5f + 3f * (1f - _flash)), new Color(1f, 1f, 1f, _flash));

            glows.gameObject.SetActive(any || _flash > 0f);
            if (!glows.gameObject.activeSelf) return;
            _glowMesh.vertices = _glowVerts;
            _glowMesh.colors = _glowColors;
        }

        /// <summary>Writes glow i as a camera-facing square (size 0 hides it).</summary>
        private void Glow(int i, Vector3 centre, float size, Color color)
        {
            Vector3 right = Vector3.right, up = Vector3.up;
            if (_camera != null)
            {
                right = _camera.transform.right;
                up = _camera.transform.up;
            }

            float h = size * 0.5f;
            int v = i * 4;
            _glowVerts[v] = centre - right * h - up * h;
            _glowVerts[v + 1] = centre + right * h - up * h;
            _glowVerts[v + 2] = centre + right * h + up * h;
            _glowVerts[v + 3] = centre - right * h + up * h;
            for (int c = 0; c < 4; c++) _glowColors[v + c] = color;
        }

        /// <summary>A cable hanging a little between two points, drawn with the line renderer. No allocation.</summary>
        private void DrawCable(LineRenderer line, Vector3 from, Vector3 to, Color tint)
        {
            if (line.positionCount != CablePoints) line.positionCount = CablePoints;
            for (int i = 0; i < CablePoints; i++) _points[i] = Cable(from, to, i / (float)(CablePoints - 1));
            line.SetPositions(_points);
            line.startColor = new Color(tint.r, tint.g, tint.b, 0.35f);
            line.endColor = tint;
        }

        private static Vector3 Cable(Vector3 from, Vector3 to, float t)
        {
            Vector3 control = (from + to) * 0.5f + Vector3.down * (Vector3.Distance(from, to) * 0.12f);
            float u = 1f - t;
            return u * u * from + 2f * u * t * control + t * t * to;
        }

        private void SetWireHeat(int wire, float scale, bool cool)
        {
            int at = wire * vertsPerWire;
            for (int i = at; i < at + vertsPerWire && i < _colors.Length; i++)
            {
                _colors[i].r = cool ? 0.05f : _heat[i].r * scale;
            }

            _mesh.colors = _colors;
        }

        // ---------- what the moment says ----------

        private void OnGrabbed(int wire, int hand) => SetWireHeat(wire, 0.35f, false); // the fixed wire dims while its cable is dragged

        private void OnDropped(int wire, int hand) => SetWireHeat(wire, 1f, false);

        private void OnConnected(int wire, int hand, int order)
        {
            _flash = 1f;
            if (sparks != null)
            {
                sparks.transform.position = moment.Socket.position;
                sparks.Emit(32);
            }

            RideAim.Buzz(hand, snapBuzz, snapBuzzSeconds);
        }

        private void OnNudged(int k, double before, double after)
        {
            SetWireHeat(k, 1f, true);
            if (dash != null)
            {
                dash.ShowReadout("WIRE NUDGED  " + before.ToString("0.00") + " > " + after.ToString("0.00")
                    + "   LOSS FOR THIS 3: " + moment.StartLoss.ToString("0.00") + " > " + moment.HeroLoss.ToString("0.00"));
            }
        }
    }
}
