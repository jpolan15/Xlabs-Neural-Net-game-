using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Draws the live <see cref="NetworkModel"/>: one sphere per input, a bias node, each neuron, and the outputs.
    /// The rest pose of the pivot is captured on the first Update, after the builder has assigned it.
    /// </summary>
    [ExecuteAlways]
    public class NeuronTopologyVisualizer : MonoBehaviour
    {
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private Transform networkPivot;
        [SerializeField] private bool autoRotate = true;
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private float levitationAmplitude = 0.035f;
        [SerializeField] private float levitationFrequency = 1.2f;
        [SerializeField] private Material inputMaterial;
        [SerializeField] private Material biasMaterial;
        [SerializeField] private Material neuronMaterial;
        [SerializeField] private Material outputMaterial;
        [SerializeField] private Material synapseMaterial;
        [SerializeField] private Material packetMaterial;

        private struct SynapseLink
        {
            public LineRenderer Line;
            public Transform From;
            public Transform To;
            public float Weight;
        }

        private readonly List<Transform> _inputs = new List<Transform>(4);
        private readonly List<Transform> _biases = new List<Transform>(4);
        private readonly List<Transform> _neurons = new List<Transform>(8);
        private readonly List<SynapseLink> _links = new List<SynapseLink>(16);
        private readonly List<Transform> _packets = new List<Transform>(8);
        private Vector3 _restLocal;
        private Vector3 _restScale;
        private float _yaw;
        private bool _restCaptured;
        private string _signature = string.Empty;
        private MaterialPropertyBlock _block;
        private Transform _graphRoot;

        private void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            _block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (neuralState != null) neuralState.OnStateMutated += HandleMutated;
        }

        private void OnDisable()
        {
            if (neuralState != null) neuralState.OnStateMutated -= HandleMutated;
        }

        private void HandleMutated()
        {
            if (neuralState != null && neuralState.Network != null)
            {
                RebuildFromNetwork(neuralState.Network);
            }
        }

        private void Update()
        {
            if (networkPivot != null)
            {
                if (!_restCaptured)
                {
                    _restLocal = networkPivot.localPosition;
                    _restScale = networkPivot.localScale;
                    _yaw = networkPivot.localEulerAngles.y;
                    _restCaptured = true;
                }

                if (autoRotate && Application.isPlaying)
                {
                    _yaw += rotationSpeed * Time.deltaTime;
                    if (_yaw >= 360f) _yaw -= 360f;
                }

                float bob = Application.isPlaying ? Mathf.Sin(Time.time * levitationFrequency) * levitationAmplitude : 0f;
                networkPivot.localRotation = Quaternion.Euler(0f, _yaw, 0f);
                networkPivot.localPosition = _restLocal + new Vector3(0f, bob, 0f);
                networkPivot.localScale = _restScale;
            }

            if (_signature.Length == 0 && neuralState != null && neuralState.Network != null)
            {
                RebuildFromNetwork(neuralState.Network);
            }

            RefreshWeights();
            PaintSynapses();
            MovePackets();
        }

        /// <summary>
        /// Rebuilds spheres and synapses from the network the simulation is actually running.
        /// </summary>
        public void RebuildFromNetwork(NetworkModel network)
        {
            if (network == null || networkPivot == null) return;
            string next = Signature(network);
            if (next == _signature && _neurons.Count > 0) return;

            ClearBakedGraphs();

            _inputs.Clear();
            _biases.Clear();
            _neurons.Clear();
            _links.Clear();
            _packets.Clear();

            var root = new GameObject("Topo_Graph");
            root.transform.SetParent(networkPivot, false);
            _graphRoot = root.transform;

            int inputCount = network.InputCount;
            for (int i = 0; i < inputCount; i++)
            {
                float y = inputCount == 1 ? 0.15f : Mathf.Lerp(0.28f, -0.05f, i / (float)(inputCount - 1));
                _inputs.Add(MakeSphere(root.transform, "Topo_Input_" + i, new Vector3(-0.42f, y, 0f), 0.11f, inputMaterial));
            }

            int layers = network.Layers.Count;
            for (int layer = 0; layer < layers; layer++)
            {
                var neurons = network.Layers[layer].Neurons;
                float x = Mathf.Lerp(-0.05f, 0.42f, layers == 1 ? 1f : layer / (float)(layers - 1));
                for (int n = 0; n < neurons.Count; n++)
                {
                    float y = neurons.Count == 1 ? 0.12f : Mathf.Lerp(0.32f, -0.28f, n / (float)(neurons.Count - 1));
                    Material mat = layer == layers - 1 ? outputMaterial : neuronMaterial;
                    Transform node = MakeSphere(root.transform, "Topo_Neuron_" + layer + "_" + n, new Vector3(x, y, 0f), 0.13f, mat);
                    _neurons.Add(node);

                    Transform bias = MakeSphere(root.transform, "Topo_Bias_" + layer + "_" + n, new Vector3(x - 0.16f, y - 0.22f, 0f), 0.07f, biasMaterial);
                    _biases.Add(bias);
                    AddSynapse(root.transform, bias, node, (float)neurons[n].Bias);

                    if (layer == 0)
                    {
                        int weights = neurons[n].WeightCount;
                        for (int w = 0; w < weights && w < _inputs.Count; w++)
                        {
                            AddSynapse(root.transform, _inputs[w], node, (float)neurons[n].GetWeight(w));
                        }
                    }
                }

                if (layer > 0)
                {
                    var previous = network.Layers[layer - 1].Neurons;
                    int previousStart = _neurons.Count - neurons.Count - previous.Count;
                    for (int n = 0; n < neurons.Count; n++)
                    {
                        Transform to = _neurons[_neurons.Count - neurons.Count + n];
                        for (int p = 0; p < previous.Count; p++)
                        {
                            Transform from = _neurons[previousStart + p];
                            AddSynapse(root.transform, from, to, (float)neurons[n].GetWeight(p));
                        }
                    }
                }
            }

            if (packetMaterial != null)
            {
                int packetCount = Mathf.Min(6, _links.Count);
                for (int i = 0; i < packetCount; i++)
                {
                    var packet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    packet.name = "Topo_Packet";
                    packet.transform.SetParent(root.transform, false);
                    packet.transform.localScale = Vector3.one * 0.035f;
                    var col = packet.GetComponent<Collider>();
                    if (col != null)
                    {
                        if (Application.isPlaying) Destroy(col);
                        else DestroyImmediate(col);
                    }
                    packet.GetComponent<Renderer>().sharedMaterial = packetMaterial;
                    _packets.Add(packet.transform);
                }
            }

            _signature = next;
        }

        private void ClearBakedGraphs()
        {
            if (_graphRoot != null)
            {
                if (Application.isPlaying) Destroy(_graphRoot.gameObject);
                else DestroyImmediate(_graphRoot.gameObject);
                _graphRoot = null;
            }

            if (networkPivot == null) return;
            for (int i = networkPivot.childCount - 1; i >= 0; i--)
            {
                Transform child = networkPivot.GetChild(i);
                if (child == null || child.name != "Topo_Graph") continue;
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        private void AddSynapse(Transform parent, Transform from, Transform to, float weight)
        {
            var go = new GameObject("Topo_Synapse");
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = synapseMaterial;
            line.useWorldSpace = false;
            line.alignment = LineAlignment.View;
            line.numCapVertices = 2;
            line.positionCount = 2;
            line.startWidth = 0.012f;
            line.endWidth = 0.012f;
            if (from != null && to != null)
            {
                line.SetPosition(0, from.localPosition);
                line.SetPosition(1, to.localPosition);
            }
            _links.Add(new SynapseLink { Line = line, From = from, To = to, Weight = weight });
        }

        private void RefreshWeights()
        {
            if (neuralState == null || neuralState.Network == null || _links.Count == 0) return;
            NetworkModel network = neuralState.Network;
            int cursor = 0;
            for (int layer = 0; layer < network.Layers.Count; layer++)
            {
                var neurons = network.Layers[layer].Neurons;
                for (int n = 0; n < neurons.Count; n++)
                {
                    if (cursor < _links.Count) WriteWeight(cursor++, (float)neurons[n].Bias);
                    if (layer == 0)
                    {
                        int weights = neurons[n].WeightCount;
                        for (int w = 0; w < weights && w < _inputs.Count; w++)
                        {
                            if (cursor < _links.Count) WriteWeight(cursor++, (float)neurons[n].GetWeight(w));
                        }
                    }
                }

                if (layer > 0)
                {
                    int previous = network.Layers[layer - 1].NeuronCount;
                    for (int n = 0; n < neurons.Count; n++)
                    {
                        for (int p = 0; p < previous; p++)
                        {
                            if (cursor < _links.Count) WriteWeight(cursor++, (float)neurons[n].GetWeight(p));
                        }
                    }
                }
            }
        }

        private void WriteWeight(int index, float weight)
        {
            SynapseLink link = _links[index];
            link.Weight = weight;
            _links[index] = link;
        }

        private void PaintSynapses()
        {
            for (int i = 0; i < _links.Count; i++)
            {
                SynapseLink link = _links[i];
                if (link.Line == null || link.From == null || link.To == null) continue;
                link.Line.SetPosition(0, link.From.localPosition);
                link.Line.SetPosition(1, link.To.localPosition);
                float width = 0.008f + 0.01f * Mathf.Clamp01(Mathf.Abs(link.Weight));
                link.Line.startWidth = width;
                link.Line.endWidth = width;
            }
        }

        private void MovePackets()
        {
            if (_packets.Count == 0 || _links.Count == 0) return;
            for (int i = 0; i < _packets.Count; i++)
            {
                LineRenderer line = _links[i % _links.Count].Line;
                if (line == null || _packets[i] == null) continue;
                float t = Mathf.Repeat(Time.time * 0.45f + i * 0.17f, 1f);
                _packets[i].localPosition = Vector3.Lerp(line.GetPosition(0), line.GetPosition(1), t);
            }
        }

        private static Transform MakeSphere(Transform parent, string name, Vector3 local, float scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = Vector3.one * scale;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            return go.transform;
        }

        private static string Signature(NetworkModel network)
        {
            var builder = new StringBuilder(32);
            builder.Append(network.InputCount);
            for (int i = 0; i < network.Layers.Count; i++)
            {
                builder.Append(':').Append(network.Layers[i].NeuronCount);
            }
            return builder.ToString();
        }
    }
}
