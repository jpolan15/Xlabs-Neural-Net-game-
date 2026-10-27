using System.Collections.Generic;
using UnityEngine;
using Convergence.Core.Training;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Keeps one of the giant network diagrams true to the rider's run (v6, ADR-020): its pixel picture and its neurons
    /// show the rider's own digit going through the rider's own network (Core's forward pass), and, for the trained
    /// network, its wires show the weights the rider trained. The builder bakes the meshes from the hero 3 and the
    /// bake's weights; this rewrites the per-vertex data in place, in the builder's order (wires: hidden neuron j's 49
    /// then answer k's 16, 16 vertices each; neurons: 49 pixels, 16 hidden, 10 answers, 4 vertices each; the picture: 49
    /// tiles, 4 vertices each). Display only.
    /// </summary>
    public sealed class NetworkLiveView : MonoBehaviour
    {
        private const int VertsPerLink = 16;
        private const int VertsPerNode = 4;

        [SerializeField] private Level1Session session;
        [SerializeField] private MeshFilter links;
        [SerializeField] private MeshFilter nodes;
        [SerializeField] private MeshFilter plate;
        [Tooltip("Redraw the wires from the rider's weights (the trained network). Off: the wires keep the baked weights.")]
        [SerializeField] private bool followWeights;
        [SerializeField] private Color ink = Color.white;
        [SerializeField] private float refreshEvery = 0.1f;

        private Mesh _links, _nodes, _plate;
        private Color[] _linkData, _nodeData, _plateData;
        private readonly List<Vector2> _radius = new List<Vector2>();
        private bool _dirty = true;
        private float _next;

        private void Awake()
        {
            _links = Copy(links, out _linkData);
            _nodes = Copy(nodes, out _nodeData);
            _plate = Copy(plate, out _plateData);
            if (_nodes != null) _nodes.GetUVs(1, _radius);
        }

        private static Mesh Copy(MeshFilter filter, out Color[] data)
        {
            data = null;
            if (filter == null || filter.sharedMesh == null) return null;
            Mesh mesh = Instantiate(filter.sharedMesh);
            filter.sharedMesh = mesh;
            data = mesh.colors;
            return mesh;
        }

        private void OnEnable()
        {
            if (session == null) return;
            session.DigitChanged += MarkDirty;
            session.WeightsChanged += MarkDirty;
            _dirty = true;
        }

        private void OnDisable()
        {
            if (session == null) return;
            session.DigitChanged -= MarkDirty;
            session.WeightsChanged -= MarkDirty;
        }

        private void MarkDirty() => _dirty = true;

        private void Update()
        {
            if (!_dirty || Time.time < _next || session == null) return;
            _dirty = false;
            _next = Time.time + refreshEvery;
            Refresh();
        }

        /// <summary>Rewrites the diagram from the session now.</summary>
        public void Refresh()
        {
            double[] x = session.Digit;
            session.Read(out double[] p, out double[] h);
            DigitNetwork net = session.Network;

            if (_nodes != null && _nodeData != null)
            {
                for (int i = 0; i < DigitNetwork.Inputs; i++) Node(i, (float)x[i], 0.4f, 0.62f);
                for (int j = 0; j < DigitNetwork.Hidden; j++) Node(DigitNetwork.Inputs + j, (float)h[j], 0.6f, 0.6f);
                for (int k = 0; k < DigitNetwork.Outputs; k++) Node(DigitNetwork.Inputs + DigitNetwork.Hidden + k, (float)p[k], 0.35f, 0.7f);
                _nodes.colors = _nodeData;
                _nodes.SetUVs(1, _radius);
            }

            if (_plate != null && _plateData != null)
            {
                for (int i = 0; i < DigitNetwork.Inputs && i * 4 + 3 < _plateData.Length; i++)
                {
                    float v = Mathf.Clamp01((float)x[i]);
                    Color c = Color.Lerp(new Color(0.03f, 0.05f, 0.10f), ink, v);
                    c.a = v;
                    _plateData[i * 4] = _plateData[i * 4 + 1] = _plateData[i * 4 + 2] = _plateData[i * 4 + 3] = c;
                }

                _plate.colors = _plateData;
            }

            if (followWeights && _links != null && _linkData != null)
            {
                float max1 = MaxAbs(net.W1), max2 = MaxAbs(net.W2);
                int link = 0;
                for (int j = 0; j < DigitNetwork.Hidden; j++)
                {
                    for (int i = 0; i < DigitNetwork.Inputs; i++) Link(link++, (float)net.W1[j * DigitNetwork.Inputs + i], max1);
                }

                for (int k = 0; k < DigitNetwork.Outputs; k++)
                {
                    for (int j = 0; j < DigitNetwork.Hidden; j++) Link(link++, (float)net.W2[k * DigitNetwork.Hidden + j], max2);
                }

                _links.colors = _linkData;
            }
        }

        private void Node(int n, float activation, float baseRadius, float growth)
        {
            int v = n * VertsPerNode;
            if (v + 3 >= _nodeData.Length) return;
            for (int c = 0; c < VertsPerNode; c++)
            {
                Color d = _nodeData[v + c];
                d.g = Mathf.Clamp01(activation);
                _nodeData[v + c] = d;
                if (v + c < _radius.Count) _radius[v + c] = new Vector2(baseRadius + growth * Mathf.Clamp01(activation), 0f);
            }
        }

        private void Link(int link, float weight, float max)
        {
            int v = link * VertsPerLink;
            if (v + VertsPerLink > _linkData.Length) return;
            float size = Mathf.Abs(weight) / max;
            float sign = weight >= 0f ? 1f : 0f;
            for (int c = 0; c < VertsPerLink; c++)
            {
                Color d = _linkData[v + c];
                d.r = size;
                d.g = sign;
                _linkData[v + c] = d;
            }
        }

        private static float MaxAbs(double[] values)
        {
            double max = 1e-9;
            foreach (double v in values) max = System.Math.Max(max, System.Math.Abs(v));
            return (float)max;
        }
    }
}
