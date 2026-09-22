using System;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Animated dotted activation lines etched into the floor connecting the workstation
    /// console, neural nodes, and security blast doors (inspired by Portal's dotted floor indicator lines).
    /// Dynamically shifts from subtle amber (unpowered) to vibrant electric cyan/emerald (powered).
    /// </summary>
    public class DottedFloorTraceVisual : MonoBehaviour
    {
        [Header("State Sources")]
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;

        [Header("Line Renderers / Materials")]
        [SerializeField] private LineRenderer[] floorTraceLines;
        [SerializeField] private float scrollSpeed = 0.8f;

        [Header("Colors")]
        [SerializeField] private Color unpoweredAmber = new Color(1.0f, 0.5f, 0.0f, 0.4f);
        [SerializeField] private Color poweredCyan = new Color(0.0f, 0.9f, 1.0f, 0.95f);
        [SerializeField] private Color allHarmonizedEmerald = new Color(0.0f, 1.0f, 0.45f, 1.0f);

        private float _textureOffset = 0f;

        private void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
        }

        private void Update()
        {
            _textureOffset -= Time.deltaTime * scrollSpeed;

            bool c1 = (neuralState != null && neuralState.Cable1Connected);
            bool c2 = (neuralState != null && neuralState.Cable2Connected);
            bool hasPower = c1 || c2;

            Color targetColor = hasPower ? poweredCyan : unpoweredAmber;

            if (floorTraceLines != null)
            {
                for (int i = 0; i < floorTraceLines.Length; i++)
                {
                    if (floorTraceLines[i] != null)
                    {
                        floorTraceLines[i].startColor = targetColor;
                        floorTraceLines[i].endColor = targetColor;
                        if (floorTraceLines[i].material != null)
                        {
                            floorTraceLines[i].material.mainTextureOffset = new Vector2(_textureOffset, 0);
                        }
                    }
                }
            }
        }
    }
}
