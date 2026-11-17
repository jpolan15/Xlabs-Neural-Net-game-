using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Region 5: every wire of the untrained network burns by its real blame |∂loss/∂w| (one mesh, baked by the
    /// builder from the same Core backward pass the moment runs), with the light flowing backward from the answers to
    /// the pixels as the Guide says "backwards". "Cool the hot wires" (2026-11-17): the few hottest wires glow hot orange,
    /// each wearing a big pulsing orb (orange to white) that the laser can hit from the stop. When the moment says a wire
    /// was shot, its orb flashes white and settles as a small calm blue dot, the wire itself cools from orange to blue,
    /// sparks fly and the hand buzzes. When nobody has cooled a wire for a few seconds, a gold ring pulses on the hottest
    /// orb with a stream of gold lights falling onto it. The hunt board on the table shows, big and near, how sure Pixel
    /// is that the rider's digit is a 3 (the moment's real softmax output, counting up after each shot) and a loss bar
    /// going down. Every glow is a camera-facing quad in one mesh, so the hunt costs one draw call plus the ring.
    /// Display only: the moment decides what a shot does.
    /// </summary>
    public sealed class BlameView : MonoBehaviour
    {
        private const int CoachDots = 3;

        [SerializeField] private RideDirector director;
        [SerializeField] private DashScreenView dash;
        [SerializeField] private BlameMoment moment;
        [SerializeField] private Renderer blameLinks;
        [SerializeField] private MeshFilter topWires;
        [SerializeField] private int vertsPerWire = 16;
        [SerializeField] private float flowSeconds = 4f;

        [Header("The hot wires")]
        [Tooltip("Each top wire's laser target: a capsule along the wire; its first child is the orb's sphere.")]
        [SerializeField] private Transform[] targets = new Transform[0];
        [Tooltip("Where each orb sits along its wire, 0 at the fixed (pixel or hidden) end: off the middle, so the orbs do not overlap where the wires crowd toward the 3.")]
        [SerializeField] private float orbAlong = 0.3f;
        [Tooltip("Holds the one mesh of glows (an additive glow material with vertex colour); the view fills it each frame.")]
        [SerializeField] private MeshFilter glows;
        [SerializeField] private ParticleSystem sparks;
        [Tooltip("Metres across of a hot wire's orb glow: seen from the stop 30 m away it is a big, easy target.")]
        [SerializeField] private float orbSize = 5f;
        [Tooltip("A bouncing \"SHOOT!\" over each hot ball (2026-11-17: so even a small child knows what to aim at).")]
        [SerializeField] private TMPro.TextMeshPro[] shootLabels = new TMPro.TextMeshPro[0];
        [SerializeField] private Color hotColor = new Color(1f, 0.5f, 0.12f, 1f);
        [SerializeField] private Color whiteHot = new Color(1f, 0.95f, 0.85f, 1f);
        [SerializeField] private Color coolColor = new Color(0.35f, 0.65f, 1f, 0.6f);
        [Tooltip("Seconds a shot wire takes to cool from hot to calm blue.")]
        [SerializeField] private float coolSeconds = 0.9f;
        [SerializeField] private float hitBuzz = 0.75f;
        [SerializeField] private float hitBuzzSeconds = 0.12f;

        [Header("Showing how")]
        [Tooltip("The coach's ring (a closed line renderer of unit radius in its XY plane) that pulses on the hottest orb when the rider stalls. Named for the old drop ring: NeuralRideBuilder.Network sets it by this name.")]
        [SerializeField] private Transform dropRing;
        [Tooltip("Seconds without a cooled wire before the ring shows the hottest one.")]
        [SerializeField] private float showAfterIdle = 1.5f;
        [SerializeField] private Color coachColor = new Color(1f, 0.82f, 0.3f, 0.9f);

        [Header("How sure Pixel is (the hunt board)")]
        [SerializeField] private TextMeshPro sureText;
        [SerializeField] private TextMeshPro startText;
        [Tooltip("A pivot at the bar's left end; its x scale is the probability of a 3.")]
        [SerializeField] private Transform sureFill;
        [Tooltip("A pivot at the loss bar's left end; its x scale is the loss now over the loss before any shot.")]
        [SerializeField] private Transform lossFill;

        [Header("Re-aiming at the rider's digit (v6)")]
        [Tooltip("Where the 49 pixels, 16 hidden neurons and 10 answers sit, in the blame meshes' frame.")]
        [SerializeField] private Vector3[] inputPositions = new Vector3[0];
        [SerializeField] private Vector3[] hiddenPositions = new Vector3[0];
        [SerializeField] private Vector3[] outputPositions = new Vector3[0];

        private static readonly int LitUpTo = Shader.PropertyToID("_LitUpTo");
        private static string[] _percent;
        private MaterialPropertyBlock _block;
        private Mesh _mesh;
        private Color[] _colors;
        private Color[] _heat;
        private float _flowStarted = -1f;
        private Camera _camera;

        // Per top wire: when it was cooled (-1: still hot).
        private float[] _cooledAt = new float[0];
        private float _lastActivity = -100f;
        private float _animateUntil = -1f;

        // The meter.
        private float _shownSure;
        private float _shownLoss = 1f;
        private int _shownPercent = -1;

        // The glows: per wire its orb and its cooling burst, then the coach's falling lights. Four vertices each.
        private Mesh _glowMesh;
        private Vector3[] _glowVerts;
        private Color[] _glowColors;
        private int _glowCount;

        private Mesh _all;
        private Color[] _allColors;

        private void Awake()
        {
            if (_percent == null)
            {
                _percent = new string[101];
                for (int i = 0; i <= 100; i++) _percent[i] = i + "%";
            }

            _block = new MaterialPropertyBlock();
            _mesh = Instantiate(topWires.sharedMesh);
            topWires.sharedMesh = _mesh;
            _colors = _mesh.colors;
            _heat = (Color[])_colors.Clone();
            if (blameLinks != null && blameLinks.TryGetComponent(out MeshFilter all))
            {
                _all = Instantiate(all.sharedMesh);
                all.sharedMesh = _all;
                _allColors = _all.colors;
            }

            _cooledAt = new float[targets.Length];
            for (int k = 0; k < _cooledAt.Length; k++) _cooledAt[k] = -1f;
            SetLit(-0.1f);
            BuildGlowMesh();
        }

        private void BuildGlowMesh()
        {
            if (glows == null) return;
            _glowCount = targets.Length * 2 + CoachDots;
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
            moment.BlameChanged += Reaim;
            moment.Nudged += OnNudged;
            moment.Connected += OnConnected;
            moment.LiveChanged += OnLive;
        }

        private void OnDisable()
        {
            if (dash != null) dash.SegmentShown -= OnCue;
            if (moment == null) return;
            moment.BlameChanged -= Reaim;
            moment.Nudged -= OnNudged;
            moment.Connected -= OnConnected;
            moment.LiveChanged -= OnLive;
        }

        private void Start()
        {
            if (glows != null) glows.gameObject.SetActive(false);
            if (dropRing != null) dropRing.gameObject.SetActive(false);
            Reaim();
        }

        /// <summary>
        /// The blame is the rider's own digit's (v6): every wire burns by its new |∂loss/∂w|, and the hunt's hot wires,
        /// their orbs and laser targets move onto the new hottest wires, all hot again. The builder lays the full blame mesh
        /// in the network's own order (hidden neuron j's 49 wires, then answer k's 16), 16 vertices per wire. The board's
        /// meter starts from this digit's real "how sure".
        /// </summary>
        private void Reaim()
        {
            for (int k = 0; k < _cooledAt.Length; k++) _cooledAt[k] = -1f;
            ResetMeter();
            if (moment == null || moment.Wires == null || moment.Wires.Count == 0 || inputPositions.Length < 49) return;
            var wires = moment.Wires;
            double biggest = System.Math.Max(1e-12, System.Math.Abs(wires[0].Gradient));
            if (_allColors != null)
            {
                foreach (BlameMoment.Wire w in wires)
                {
                    int index = w.Layer == 0 ? w.Neuron * 49 + w.Input : 16 * 49 + w.Neuron * 16 + w.Input;
                    float size = Mathf.Sqrt((float)(System.Math.Abs(w.Gradient) / biggest));
                    for (int v = index * vertsPerWire; v < (index + 1) * vertsPerWire && v < _allColors.Length; v++) _allColors[v].r = size;
                }

                _all.colors = _allColors;
            }

            int top = Mathf.Min(moment.TopCount, targets.Length);
            Vector3[] vertices = _mesh.vertices;
            Vector3[] normals = _mesh.normals;
            var uv = new System.Collections.Generic.List<Vector2>();
            _mesh.GetUVs(0, uv);
            for (int k = 0; k < top && k < wires.Count; k++)
            {
                BlameMoment.Wire w = wires[k];
                Vector3 a = w.Layer == 0 ? inputPositions[w.Input] : hiddenPositions[w.Input];
                Vector3 b = w.Layer == 0 ? hiddenPositions[w.Neuron] : outputPositions[w.Neuron];
                Vector3 tangent = (b - a).normalized;
                int points = vertsPerWire / 2;
                for (int i = 0; i < points; i++)
                {
                    float t = i / (float)(points - 1);
                    Vector3 p = Vector3.Lerp(a, b, t);
                    float run = Vector3.Distance(a, b) * t;
                    for (int side = 0; side < 2; side++)
                    {
                        int v = k * vertsPerWire + i * 2 + side;
                        if (v >= vertices.Length) continue;
                        vertices[v] = p;
                        normals[v] = tangent;
                        if (v < uv.Count) uv[v] = new Vector2(run, side == 0 ? -1f : 1f);
                    }
                }

                if (targets[k] != null)
                {
                    Transform target = targets[k];
                    target.localPosition = (a + b) * 0.5f;
                    target.localRotation = Quaternion.LookRotation(tangent, Vector3.up);
                    if (target.TryGetComponent(out CapsuleCollider capsule)) capsule.height = Vector3.Distance(a, b);
                    if (target.childCount > 0) target.GetChild(0).position = target.parent.TransformPoint(Vector3.Lerp(a, b, orbAlong));
                }
            }

            _mesh.vertices = vertices;
            _mesh.normals = normals;
            _mesh.SetUVs(0, uv);
            _mesh.RecalculateBounds();
            Bounds bounds = _mesh.bounds;
            bounds.Expand(4f);
            _mesh.bounds = bounds;
            PaintWires();
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

            if (moment == null) return;
            bool live = moment.IsLive;
            if (live || Time.time < _animateUntil) PaintWires();
            UpdateGlows(live);
            UpdateMeter();
        }

        private void SetLit(float value)
        {
            if (blameLinks == null) return;
            blameLinks.GetPropertyBlock(_block);
            _block.SetFloat(LitUpTo, value);
            blameLinks.SetPropertyBlock(_block);
        }

        // ---------- the hot wires ----------

        private int WireCount => moment != null ? Mathf.Min(targets.Length, moment.TopCount) : 0;

        private Vector3 OrbPosition(int k)
        {
            Transform target = targets[k];
            if (target == null) return Vector3.zero;
            return target.childCount > 0 ? target.GetChild(0).position : target.position;
        }

        /// <summary>
        /// The wires' colour in the top-wires mesh: red is the width and brightness, green the hue (1: the material's hot
        /// colour, 0: its calm blue). A hot wire pulses while the hunt is live; a shot one cools from hot to blue.
        /// </summary>
        private void PaintWires()
        {
            if (_colors == null) return;
            bool live = moment != null && moment.IsLive;
            int wires = WireCount;
            for (int k = 0; k < wires; k++)
            {
                float r, g;
                if (_cooledAt[k] >= 0f)
                {
                    float u = Mathf.Clamp01((Time.time - _cooledAt[k]) / coolSeconds);
                    r = Mathf.Lerp(1f, 0.4f, u);
                    g = 1f - u;
                }
                else
                {
                    r = live ? 0.8f + 0.2f * Mathf.Sin(Time.time * 5f + k * 1.3f) : -1f;
                    g = 1f;
                }

                int at = k * vertsPerWire;
                for (int i = at; i < at + vertsPerWire && i < _colors.Length; i++)
                {
                    _colors[i].r = r < 0f ? _heat[i].r : r;
                    _colors[i].g = g;
                }
            }

            _mesh.colors = _colors;
        }

        private void UpdateGlows(bool live)
        {
            if (_glowMesh == null) return;
            if (_camera == null) _camera = Camera.main;
            bool any = live;
            for (int i = 0; i < _glowCount; i++) Glow(i, Vector3.zero, 0f, Color.clear);

            int wires = WireCount;
            for (int k = 0; k < shootLabels.Length; k++)
            {
                TMPro.TextMeshPro label = shootLabels[k];
                if (label == null) continue;
                // One target at a time: only the hottest ball still glowing wears "SHOOT!" (the balls cluster near the answers).
                bool hot = live && k < wires && _cooledAt[k] < 0f && k == moment.HottestLeft;
                if (label.gameObject.activeSelf != hot) label.gameObject.SetActive(hot);
                if (hot) label.transform.position = OrbPosition(k) + Vector3.up * (orbSize * 0.75f + 0.5f + 0.35f * Mathf.Abs(Mathf.Sin(Time.time * 3f + k)));
            }

            for (int k = 0; k < wires; k++)
            {
                Vector3 at = OrbPosition(k);
                if (_cooledAt[k] < 0f)
                {
                    if (!live) continue;
                    // Hot: a big orb pulsing from orange to white.
                    float beat = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f + k * 1.3f);
                    Glow(k * 2, at, orbSize * (1f + 0.15f * beat), Color.Lerp(hotColor, whiteHot, beat));
                    // A wide warm halo, so a hot wire reads as a target from the stop 30 m away (2026-11-17 live check: the
                    // orb alone was a speck).
                    Glow(k * 2 + 1, at, orbSize * (2.4f + 0.3f * beat), new Color(hotColor.r, hotColor.g, hotColor.b, 0.5f));
                    continue;
                }

                // Cooled: the orb flashes white and settles as a small calm blue dot; a burst rings out from it.
                float u = Mathf.Clamp01((Time.time - _cooledAt[k]) / coolSeconds);
                if (!live && u >= 1f) continue;
                Glow(k * 2, at, orbSize * Mathf.Lerp(1.6f, 0.45f, u), Color.Lerp(whiteHot, coolColor, u));
                if (u < 1f)
                {
                    any = true;
                    float fade = (1f - u) * (1f - u);
                    Glow(k * 2 + 1, at, orbSize * (1.2f + 3.5f * u), new Color(1f, 1f, 1f, fade));
                }
            }

            UpdateCoach(live);
            glows.gameObject.SetActive(any);
            if (!any) return;
            _glowMesh.vertices = _glowVerts;
            _glowMesh.colors = _glowColors;
        }

        // ---------- showing how ----------

        private void OnLive(bool live)
        {
            // The first hint comes soon after the hunt opens: a rider who has never seen it should not wait long to be shown.
            _lastActivity = live ? Time.time - showAfterIdle + 2f : -100f;
            _animateUntil = Time.time + coolSeconds + 0.1f;
            if (!live) PaintWires();
        }

        /// <summary>
        /// When nobody has cooled a wire for a few seconds, a gold ring pulses on the hottest orb still hot, facing the rider,
        /// with a stream of gold lights falling onto it from above: "this one".
        /// </summary>
        private void UpdateCoach(bool live)
        {
            int k = live ? moment.HottestLeft : -1;
            bool show = k >= 0 && k < WireCount && Time.time - _lastActivity >= showAfterIdle;
            if (dropRing != null && dropRing.gameObject.activeSelf != show) dropRing.gameObject.SetActive(show);
            if (!show) return;

            Vector3 at = OrbPosition(k);
            Vector3 eye = _camera != null ? _camera.transform.position : at - Vector3.forward * 30f;
            Vector3 up = _camera != null ? _camera.transform.up : Vector3.up;
            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 6f);
            float radius = orbSize * 0.75f;
            if (dropRing != null)
            {
                dropRing.position = at;
                Vector3 look = at - eye;
                if (look.sqrMagnitude > 1e-6f) dropRing.rotation = Quaternion.LookRotation(look, up);
                dropRing.localScale = Vector3.one * radius * pulse;
            }

            int first = targets.Length * 2;
            for (int i = 0; i < CoachDots; i++)
            {
                float u = Mathf.Repeat(Time.time * 0.8f + i / (float)CoachDots, 1f);
                Vector3 p = at + up * (radius * 1.2f + 0.4f + (1f - u) * 2.6f);
                Color c = coachColor;
                c.a *= Mathf.Sin(u * Mathf.PI);
                Glow(first + i, p, orbSize * (0.55f - 0.15f * u), c);
            }
        }

        // ---------- the meter on the hunt board ----------

        private void ResetMeter()
        {
            if (moment == null) return;
            _shownSure = (float)moment.StartProbability;
            _shownLoss = 1f;
            _shownPercent = -1;
            if (startText != null) startText.text = "STARTED AT " + Percent(moment.StartProbability);
        }

        private static string Percent(double p) => _percent[Mathf.Clamp(Mathf.RoundToInt((float)p * 100f), 0, 100)];

        /// <summary>The real "how sure it's a 3" counts up toward the moment's value after each shot; the loss bar shrinks.</summary>
        private void UpdateMeter()
        {
            float sure = (float)moment.HeroProbability;
            float gap = Mathf.Abs(sure - _shownSure);
            _shownSure = Mathf.MoveTowards(_shownSure, sure, Mathf.Max(0.03f, gap * 2.5f) * Time.deltaTime);
            int percent = Mathf.Clamp(Mathf.RoundToInt(_shownSure * 100f), 0, 100);
            if (sureText != null && percent != _shownPercent)
            {
                _shownPercent = percent;
                sureText.text = _percent[percent];
            }

            if (sureFill != null) sureFill.localScale = new Vector3(Mathf.Max(0.001f, _shownSure), 1f, 1f);

            double start = moment.StartLoss;
            float loss = start > 1e-9 ? Mathf.Clamp01((float)(moment.HeroLoss / start)) : 1f;
            _shownLoss = Mathf.MoveTowards(_shownLoss, loss, Mathf.Max(0.05f, Mathf.Abs(loss - _shownLoss) * 2.5f) * Time.deltaTime);
            if (lossFill != null) lossFill.localScale = new Vector3(Mathf.Max(0.001f, _shownLoss), 1f, 1f);
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

        // ---------- what the moment says ----------

        private void OnNudged(int k, double before, double after)
        {
            if (k >= 0 && k < _cooledAt.Length && _cooledAt[k] < 0f) _cooledAt[k] = Time.time;
            _animateUntil = Time.time + coolSeconds + 0.1f;
            if (dash != null)
            {
                dash.ShowReadout("WIRE COOLED  " + before.ToString("0.00") + " > " + after.ToString("0.00")
                    + "   SURE IT'S A 3: " + Percent(moment.StartProbability) + " > " + Percent(moment.HeroProbability));
            }
        }

        private void OnConnected(int k, int hand, int order)
        {
            _lastActivity = Time.time;
            if (sparks != null && k >= 0 && k < targets.Length)
            {
                sparks.transform.position = OrbPosition(k);
                sparks.Emit(32);
            }

            RideAim.Buzz(hand, hitBuzz, hitBuzzSeconds);
        }
    }
}
