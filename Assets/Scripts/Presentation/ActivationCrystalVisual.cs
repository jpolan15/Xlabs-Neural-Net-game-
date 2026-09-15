using System;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Visual observer for the socketed activation crystal in the central neuron machine.
    /// Displays distinct 3D geometric shapes and vibrant emissive colors for Linear, ReLU, Step, and Sigmoid.
    /// </summary>
    public class ActivationCrystalVisual : MonoBehaviour
    {
        [Header("State Listener")]
        [SerializeField] private NeuralState neuralState;

        [Header("Crystal Models")]
        [SerializeField] private GameObject linearCrystalModel;
        [SerializeField] private GameObject reluCrystalModel;
        [SerializeField] private GameObject stepCrystalModel;
        [SerializeField] private GameObject sigmoidCrystalModel;

        [Header("Lighting")]
        [SerializeField] private Light crystalGlowLight;

        [Header("Emissive Colors")]
        [SerializeField] private Color linearColor = new Color(0.0f, 0.95f, 1.0f);   // Cyan
        [SerializeField] private Color reluColor = new Color(1.0f, 0.7f, 0.0f);      // Amber
        [SerializeField] private Color stepColor = new Color(0.0f, 0.9f, 0.45f);     // Emerald
        [SerializeField] private Color sigmoidColor = new Color(0.85f, 0.0f, 1.0f);  // Violet

        private Vector3 _initialLocalPos;
        private ActivationType _lastType = (ActivationType)(-1);

        private void Awake()
        {
            if (neuralState == null)
            {
                neuralState = FindAnyObjectByType<NeuralState>();
            }
            _initialLocalPos = transform.localPosition;
        }

        private void OnEnable()
        {
            if (neuralState != null)
            {
                neuralState.OnActivationChanged += HandleActivationChanged;
            }
        }

        private void OnDisable()
        {
            if (neuralState != null)
            {
                neuralState.OnActivationChanged -= HandleActivationChanged;
            }
        }

        private void Start()
        {
            if (neuralState != null)
            {
                HandleActivationChanged(neuralState.Activation);
            }
        }

        private void Update()
        {
            // Gentle hovering levitation
            float bob = Mathf.Sin(Time.time * 2.5f) * 0.025f;
            transform.localPosition = _initialLocalPos + new Vector3(0, bob, 0);
            transform.Rotate(Vector3.up, 30.0f * Time.deltaTime, Space.Self);
        }

        private void HandleActivationChanged(ActivationType type)
        {
            if (_lastType == type) return;
            _lastType = type;

            if (linearCrystalModel != null) linearCrystalModel.SetActive(type == ActivationType.Linear);
            if (reluCrystalModel != null) reluCrystalModel.SetActive(type == ActivationType.ReLU);
            if (stepCrystalModel != null) stepCrystalModel.SetActive(type == ActivationType.Step);
            if (sigmoidCrystalModel != null) sigmoidCrystalModel.SetActive(type == ActivationType.Sigmoid);

            Color activeColor = GetColorForType(type);

            if (crystalGlowLight != null)
            {
                crystalGlowLight.color = activeColor;
            }
        }

        public Color GetColorForType(ActivationType type)
        {
            switch (type)
            {
                case ActivationType.Linear: return linearColor;
                case ActivationType.ReLU: return reluColor;
                case ActivationType.Step: return stepColor;
                case ActivationType.Sigmoid: return sigmoidColor;
                default: return Color.white;
            }
        }
    }
}
