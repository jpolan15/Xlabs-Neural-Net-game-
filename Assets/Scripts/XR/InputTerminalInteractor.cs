using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.XR
{
    /// <summary>
    /// Physical interactive toggle terminal for an input variable (X1 or X2).
    /// Can be clicked in Desktop fallback or grabbed/switched in VR to toggle input signal between 0 and 1.
    /// </summary>
    public class InputTerminalInteractor : MonoBehaviour
    {
        public static event Action<int, double> OnTerminalStateChanged;

        [Header("Terminal Identity")]
        [SerializeField] private int terminalIndex = 0; // 0 for X1, 1 for X2
        [SerializeField] private double currentValue = 0.0;

        [Header("Visual References")]
        [SerializeField] private Renderer indicatorLightRenderer;
        [SerializeField] private Light pointLight;
        [SerializeField] private Color offColor = new Color(0.2f, 0.25f, 0.3f);
        [SerializeField] private Color onColor = new Color(0.0f, 0.95f, 1.0f);

        public int TerminalIndex => terminalIndex;
        public double CurrentValue => currentValue;

        private void Awake()
        {
            UpdateVisuals();
        }

        public void SetValue(double val)
        {
            currentValue = val > 0.5 ? 1.0 : 0.0;
            UpdateVisuals();
            OnTerminalStateChanged?.Invoke(terminalIndex, currentValue);
        }

        public void ToggleValue()
        {
            SetValue(currentValue > 0.5 ? 0.0 : 1.0);
        }

        private void UpdateVisuals()
        {
            bool isOn = currentValue > 0.5;
            Color c = isOn ? onColor : offColor;

            if (indicatorLightRenderer != null && indicatorLightRenderer.material != null)
            {
                indicatorLightRenderer.material.color = c;
                indicatorLightRenderer.material.SetColor("_EmissionColor", isOn ? c * 2.5f : Color.black);
            }

            if (pointLight != null)
            {
                pointLight.color = c;
                pointLight.intensity = isOn ? 2.5f : 0.4f;
            }
        }
    }
}
