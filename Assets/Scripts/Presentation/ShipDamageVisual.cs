using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Sparks, flickering lights, and a hull-breach patch. They stop as repairs complete.
    /// </summary>
    public class ShipDamageVisual : MonoBehaviour
    {
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private ParticleSystem sparks;
        [SerializeField] private Light[] flickeringLights;
        [SerializeField] private GameObject breachPatch;
        [SerializeField] private int repairsToClear = 1;

        private int _repairs;
        private float _flicker;

        private void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
        }

        private void OnEnable()
        {
            if (chamberController != null) chamberController.OnPuzzleSolved += HandleRepaired;
        }

        private void OnDisable()
        {
            if (chamberController != null) chamberController.OnPuzzleSolved -= HandleRepaired;
        }

        private void Update()
        {
            if (_repairs >= repairsToClear || flickeringLights == null) return;
            _flicker += Time.deltaTime * 9f;
            float intensity = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(_flicker));
            for (int i = 0; i < flickeringLights.Length; i++)
            {
                if (flickeringLights[i] != null) flickeringLights[i].intensity = intensity;
            }
        }

        private void HandleRepaired()
        {
            _repairs++;
            if (_repairs < repairsToClear) return;
            if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (breachPatch != null) breachPatch.SetActive(false);
            if (flickeringLights == null) return;
            for (int i = 0; i < flickeringLights.Length; i++)
            {
                if (flickeringLights[i] != null) flickeringLights[i].intensity = 1.2f;
            }
        }
    }
}
