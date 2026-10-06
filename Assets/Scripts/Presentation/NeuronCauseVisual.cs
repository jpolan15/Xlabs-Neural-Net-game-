using UnityEngine;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Shows the wire, the four cards, and the boundary from live state.
    /// Correctness comes only from the latest PuzzleEvaluation. This script does not evaluate.
    /// </summary>
    public sealed class NeuronCauseVisual : MonoBehaviour
    {
        static readonly int W1Id = Shader.PropertyToID("_W1");
        static readonly int W2Id = Shader.PropertyToID("_W2");
        static readonly int BId = Shader.PropertyToID("_B");
        static readonly int H1Id = Shader.PropertyToID("_H1");
        static readonly int H2Id = Shader.PropertyToID("_H2");
        static readonly int OId = Shader.PropertyToID("_O");
        static readonly int UseHiddenId = Shader.PropertyToID("_UseHidden");

        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private Material cyanWire;
        [SerializeField] private Material slateWire;
        [SerializeField] private Material dashWire;
        [SerializeField] private float wireMin = 0.004f;
        [SerializeField] private float wireMax = 0.03f;
        [SerializeField] private float weightMax = 2f;
        [SerializeField] private float cycleSeconds = 3f;

        LineRenderer _rockWire;
        LineRenderer _iceWire;
        LineRenderer _biasWire;
        GameObject _rockMinus;
        GameObject _iceMinus;
        GameObject _biasMinus;
        Renderer _boundary;
        Material _boundaryMat;
        TextMesh _sum;
        readonly Transform[] _cards = new Transform[4];
        readonly GameObject[] _checks = new GameObject[4];
        readonly GameObject[] _crosses = new GameObject[4];
        readonly GameObject[] _outlines = new GameObject[4];
        int _active;
        int _hovered = -1;
        float _cycle;
        bool _ready;

        public int ActiveCase => _hovered >= 0 ? _hovered : _active;

        /// <summary>WP4 hover points at one card. Pass -1 to resume the cycle.</summary>
        public void SetHoveredCase(int caseIndex)
        {
            _hovered = caseIndex;
            Refresh();
        }

        void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            _rockWire = Wire("RockWire");
            _iceWire = Wire("IceWire");
            _biasWire = Wire("BiasWire");
            _rockMinus = Child("RockMinus");
            _iceMinus = Child("IceMinus");
            _biasMinus = Child("BiasMinus");
            Transform boundary = transform.Find("BoundaryQuad");
            if (boundary != null) _boundary = boundary.GetComponent<Renderer>();
            if (_boundary != null) _boundaryMat = _boundary.material;
            Transform sum = transform.Find("FireNode/SumReadout");
            if (sum != null) _sum = sum.GetComponent<TextMesh>();
            for (int i = 0; i < 4; i++)
            {
                _cards[i] = transform.Find("Card" + i);
                if (_cards[i] == null) continue;
                _checks[i] = ChildOf(_cards[i], "MarkCheck");
                _crosses[i] = ChildOf(_cards[i], "MarkCross");
                _outlines[i] = ChildOf(_cards[i], "Outline");
            }

            _ready = _rockWire != null && _iceWire != null && _biasWire != null && _boundaryMat != null;
        }

        void OnEnable()
        {
            if (neuralState != null) neuralState.OnStateMutated += HandleMutated;
            if (chamberController != null) chamberController.OnEvaluationComplete += HandleEvaluation;
            Refresh();
        }

        void OnDisable()
        {
            if (neuralState != null) neuralState.OnStateMutated -= HandleMutated;
            if (chamberController != null) chamberController.OnEvaluationComplete -= HandleEvaluation;
        }

        void OnDestroy()
        {
            if (_boundaryMat != null) Destroy(_boundaryMat);
        }

        void Update()
        {
            if (_hovered >= 0 || cycleSeconds <= 0f) return;
            _cycle += Time.deltaTime;
            if (_cycle < cycleSeconds) return;
            _cycle = 0f;
            _active = (_active + 1) % 4;
            Refresh();
        }

        void HandleMutated()
        {
            Refresh();
        }

        void HandleEvaluation(PuzzleEvaluation evaluation)
        {
            Refresh();
        }

        void Refresh()
        {
            if (!_ready || neuralState == null) return;
            double rock = neuralState.Cable1Connected ? neuralState.Weight1 : 0.0;
            double ice = neuralState.Cable2Connected ? neuralState.Weight2 : 0.0;
            double bias = neuralState.Bias;
            ApplyWire(_rockWire, _rockMinus, rock);
            ApplyWire(_iceWire, _iceMinus, ice);
            ApplyWire(_biasWire, _biasMinus, bias);

            _boundaryMat.SetFloat(W1Id, (float)rock);
            _boundaryMat.SetFloat(W2Id, (float)ice);
            _boundaryMat.SetFloat(BId, (float)bias);

            PuzzleEvaluation eval = chamberController != null ? chamberController.LastEvaluation : null;
            int active = ActiveCase;
            if (_sum != null)
            {
                _sum.text = SumFor(eval, active);
            }

            var puzzle = chamberController != null ? chamberController.Puzzle : null;
            for (int i = 0; i < 4; i++)
            {
                if (_cards[i] == null) continue;
                bool on = i == active;
                _cards[i].localScale = Vector3.one * (on ? 1.12f : 1f);
                bool known = eval != null && eval.Diagnostics != null && i < eval.Diagnostics.Count;
                bool correct = known && eval.Diagnostics[i].IsCorrect && eval.ActivationMatches;
                if (_checks[i] != null) _checks[i].SetActive(known && correct);
                if (_crosses[i] != null) _crosses[i].SetActive(known && !correct);
                if (_outlines[i] != null) _outlines[i].SetActive(known && correct);
                if (puzzle != null && i < puzzle.TestCases.Count)
                {
                    Transform badge = _cards[i].Find("Badge");
                    if (badge != null) badge.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>Hidden-layer drawing for chamber 04. Zeros leave the single line in place.</summary>
        public void SetHiddenBoundary(Vector4 hidden1, Vector4 hidden2, Vector4 output, bool enabled)
        {
            if (_boundaryMat == null) return;
            _boundaryMat.SetVector(H1Id, hidden1);
            _boundaryMat.SetVector(H2Id, hidden2);
            _boundaryMat.SetVector(OId, output);
            _boundaryMat.SetFloat(UseHiddenId, enabled ? 1f : 0f);
        }

        static string SumFor(PuzzleEvaluation eval, int active)
        {
            if (eval == null || eval.Diagnostics == null || active < 0 || active >= eval.Diagnostics.Count)
            {
                return "—";
            }

            double z = eval.Diagnostics[active].CalculatedZ;
            return z.ToString("+0.00;-0.00;0.00");
        }

        void ApplyWire(LineRenderer line, GameObject minus, double weight)
        {
            float magnitude = (float)System.Math.Abs(weight);
            float span = magnitude <= 0.0001f ? 0f : Mathf.Clamp01(magnitude / Mathf.Max(0.0001f, weightMax));
            float width = magnitude <= 0.0001f ? wireMin : Mathf.Lerp(wireMin, wireMax, span);
            line.startWidth = width;
            line.endWidth = width;
            if (magnitude <= 0.0001f) line.sharedMaterial = slateWire;
            else if (weight < 0.0) line.sharedMaterial = dashWire;
            else line.sharedMaterial = cyanWire;
            if (minus != null) minus.SetActive(weight < -0.0001);
        }

        LineRenderer Wire(string name)
        {
            Transform child = transform.Find(name);
            return child != null ? child.GetComponent<LineRenderer>() : null;
        }

        GameObject Child(string name)
        {
            Transform child = transform.Find(name);
            return child != null ? child.gameObject : null;
        }

        static GameObject ChildOf(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            return child != null ? child.gameObject : null;
        }
    }
}
