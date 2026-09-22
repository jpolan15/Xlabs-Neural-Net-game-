using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Visual observer for the central alien neuron machine.
    /// Drives physical dial rotations, conduit emissive glow, and core energy pulsing.
    /// </summary>
    [ExecuteAlways]
    public class NeuronMachineVisual : MonoBehaviour
    {
        [Header("State Source")]
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;

        [Header("Regulator Dials")]
        [SerializeField] private Transform w1RegulatorDial;
        [SerializeField] private Transform w2RegulatorDial;
        [SerializeField] private Transform biasDial;

        [Header("Conduits")]
        [SerializeField] private Renderer cable1Renderer;
        [SerializeField] private Renderer cable2Renderer;

        [Header("Core Energy")]
        [SerializeField] private Light corePulseLight;
        [SerializeField] private Color activeColor = new Color(0.20f, 0.75f, 0.98f);        // Soft Cyan
        [SerializeField] private Color awakenedColor = new Color(0.10f, 0.90f, 0.50f);       // Mint Emerald
        [SerializeField] private Color disconnectedColor = new Color(0.95f, 0.60f, 0.10f);  // Warm Amber

        private void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
        }

        private void Update()
        {
            UpdateDials();
            UpdateConduits();
            UpdateCorePulse();
        }

        private void UpdateDials()
        {
            if (neuralState == null) return;

            // Map weight range [-2.0, 2.0] to rotation angles [-120 deg, 120 deg]
            if (w1RegulatorDial != null)
            {
                float angle1 = Mathf.Lerp(-120f, 120f, Mathf.InverseLerp(-2f, 2f, (float)neuralState.Weight1));
                w1RegulatorDial.localRotation = Quaternion.Euler(0, angle1, 0);
            }

            if (w2RegulatorDial != null)
            {
                float angle2 = Mathf.Lerp(-120f, 120f, Mathf.InverseLerp(-2f, 2f, (float)neuralState.Weight2));
                w2RegulatorDial.localRotation = Quaternion.Euler(0, angle2, 0);
            }

            if (biasDial != null)
            {
                float angleB = Mathf.Lerp(-120f, 120f, Mathf.InverseLerp(-2f, 2f, (float)neuralState.Bias));
                biasDial.localRotation = Quaternion.Euler(0, angleB, 0);
            }
        }

        private void UpdateConduits()
        {
            if (neuralState == null) return;

            if (cable1Renderer != null && cable1Renderer.material != null)
            {
                Color c1 = neuralState.Cable1Connected ? activeColor : disconnectedColor;
                cable1Renderer.material.color = c1;
                cable1Renderer.material.SetColor("_EmissionColor", c1 * 2.0f);
            }
            if (cable2Renderer != null && cable2Renderer.material != null)
            {
                Color c2 = neuralState.Cable2Connected ? activeColor : disconnectedColor;
                cable2Renderer.material.color = c2;
                cable2Renderer.material.SetColor("_EmissionColor", c2 * 2.0f);
            }
        }


        private void UpdateCorePulse()
        {
            if (corePulseLight == null) return;

            bool isAwakened = chamberController != null && chamberController.Phase == ChamberPhase.Awakening;
            Color baseColor = isAwakened ? awakenedColor : activeColor;
            float pulse = Mathf.PingPong(Time.time * (isAwakened ? 3.0f : 1.2f), 1.0f);

            corePulseLight.color = baseColor;
            corePulseLight.intensity = 1.0f + (pulse * (isAwakened ? 4.0f : 1.5f));
        }
    }
}
