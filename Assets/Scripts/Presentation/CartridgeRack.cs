using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Three circuit slots. They open after the XOR wall unlocks. Plugging rebuilds through gameplay.
    /// </summary>
    public sealed class CartridgeRack : MonoBehaviour
    {
        [SerializeField] private XorWallDirector wall;
        [SerializeField] private ChamberController chamber;
        readonly string[] _ids = { "chamber01", "chamber02", "chamber03" };
        readonly int[] _choice = new int[3];
        bool _built;

        void Awake()
        {
            if (wall == null) wall = FindAnyObjectByType<XorWallDirector>();
            if (chamber == null) chamber = FindAnyObjectByType<ChamberController>();
        }

        void OnEnable()
        {
            if (wall != null) wall.OnWallUnlocked += Show;
            if (wall != null) wall.OnNetworkRebuilt += PaintBoundary;
        }

        void OnDisable()
        {
            if (wall != null) wall.OnWallUnlocked -= Show;
            if (wall != null) wall.OnNetworkRebuilt -= PaintBoundary;
        }

        void Show()
        {
            if (_built) return;
            _built = true;
            var board = ChamberPromptBoard.Instance;
            if (board != null) board.Set("One line can't split these. Add a neuron.");
            var anchor = GameObject.Find("NeuronDisplayAnchor");
            if (anchor == null || wall == null) return;
            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "CircuitSlot" + i;
                go.transform.SetParent(anchor.transform, false);
                go.transform.localPosition = new Vector3(-0.12f + (i * 0.12f), -0.32f, 0f);
                go.transform.localScale = new Vector3(0.08f, 0.04f, 0.02f);
                var button = go.AddComponent<SlotClick>();
                button.Rack = this;
                button.Slot = slot;
            }
        }

        public void Cycle(int slot)
        {
            if (wall == null) return;
            _choice[slot] = (_choice[slot] + 1) % _ids.Length;
            wall.Slot(slot, _ids[_choice[slot]]);
        }

        void PaintBoundary(NetworkModel network)
        {
            var anchor = GameObject.Find("NeuronDisplayAnchor");
            if (anchor == null || network == null || network.Layers.Count < 2) return;
            var visual = anchor.GetComponent<NeuronCauseVisual>();
            if (visual == null) return;
            var h1 = network.Layers[0].Neurons[0];
            var h2 = network.Layers[0].Neurons[1];
            var output = network.Layers[1].Neurons[0];
            visual.SetHiddenBoundary(
                new Vector4((float)h1.Weights[0], (float)h1.Weights[1], (float)h1.Bias, 0f),
                new Vector4((float)h2.Weights[0], (float)h2.Weights[1], (float)h2.Bias, 0f),
                new Vector4((float)output.Weights[0], (float)output.Weights[1], (float)output.Bias, 0f),
                true);
        }

        sealed class SlotClick : MonoBehaviour
        {
            public CartridgeRack Rack;
            public int Slot;

            void OnMouseDown()
            {
                if (Rack != null) Rack.Cycle(Slot);
            }
        }
    }
}
