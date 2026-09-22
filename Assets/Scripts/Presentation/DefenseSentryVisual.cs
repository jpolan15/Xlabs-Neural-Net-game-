using System;
using System.Collections;
using UnityEngine;
using Convergence.Core.Neural;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Visual observer and kinetic animator for the Automated Defense Sentry in Level 1.
    /// Manages turret rotation, barrel pitch, targeting lasers, plasma projectile discharges,
    /// friendly scan cones, muzzle flashes, and alarm states.
    /// Strictly Presentation layer: observes ChamberController and NeuralState events, never decides puzzle correctness.
    /// </summary>
    public class DefenseSentryVisual : MonoBehaviour
    {
        [Header("State Sources")]
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private NeuralState neuralState;

        [Header("Turret Mechanical Rig")]
        [SerializeField] private Transform turretBase;
        [SerializeField] private Transform turretYawHead;
        [SerializeField] private Transform turretPitchGimbal;
        [SerializeField] private Transform leftMuzzle;
        [SerializeField] private Transform rightMuzzle;
        [SerializeField] private Renderer sentryCorePlasma;
        [SerializeField] private Renderer statusStrobeRenderer;

        [Header("Weapon & Targeting FX")]
        [SerializeField] private LineRenderer targetingLaser;
        [SerializeField] private LineRenderer scanConeBeam;
        [SerializeField] private Light sentrySpotlight;
        [SerializeField] private Light muzzleFlashLight;
        [SerializeField] private ParticleSystem plasmaDischargeParticles;
        [SerializeField] private ParticleSystem alertSteamParticles;

        [Header("Color Palette")]
        [SerializeField] private Color alertRed = new Color(1.0f, 0.15f, 0.15f, 1.0f);
        [SerializeField] private Color scanningCyan = new Color(0.1f, 0.75f, 1.0f, 1.0f);
        [SerializeField] private Color friendlyEmerald = new Color(0.05f, 1.0f, 0.45f, 1.0f);
        [SerializeField] private Color plasmaAmber = new Color(1.0f, 0.6f, 0.1f, 1.0f);

        [Header("Animation Settings")]
        [SerializeField] private float aimTrackingSpeed = 12.0f;
        [SerializeField] private float idleScanSpeed = 0.5f;

        private Quaternion _targetYaw = Quaternion.identity;
        private Quaternion _targetPitch = Quaternion.identity;
        private bool _isEngaging = false;
        private Coroutine _engageRoutine;
        private float _idleTimer;

        private void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
        }

        private void OnEnable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered += HandleForwardPassTriggered;
                chamberController.OnSingleCaseEvaluated += HandleSingleCaseEvaluated;
                chamberController.OnEvaluationComplete += HandleEvaluationComplete;
                chamberController.OnPuzzleSolved += HandlePuzzleSolved;
                chamberController.OnChamberReset += HandleChamberReset;
                chamberController.OnShieldDamaged += HandleShieldDamaged;
                chamberController.OnEmergencyPurge += HandleEmergencyPurge;
            }
        }

        private void OnDisable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered -= HandleForwardPassTriggered;
                chamberController.OnSingleCaseEvaluated -= HandleSingleCaseEvaluated;
                chamberController.OnEvaluationComplete -= HandleEvaluationComplete;
                chamberController.OnPuzzleSolved -= HandlePuzzleSolved;
                chamberController.OnChamberReset -= HandleChamberReset;
                chamberController.OnShieldDamaged -= HandleShieldDamaged;
                chamberController.OnEmergencyPurge -= HandleEmergencyPurge;
            }
        }

        private void HandleShieldDamaged(float damage)
        {
            if (alertSteamParticles != null) alertSteamParticles.Play();
            if (sentrySpotlight != null)
            {
                sentrySpotlight.color = alertRed;
                sentrySpotlight.intensity = 4.0f;
            }
        }

        private void HandleEmergencyPurge()
        {
            if (alertSteamParticles != null) alertSteamParticles.Play();
            SetCoreStatus(false, true);
        }

        private void Start()
        {
            if (targetingLaser != null) targetingLaser.enabled = false;
            if (scanConeBeam != null) scanConeBeam.enabled = false;
            if (muzzleFlashLight != null) muzzleFlashLight.enabled = false;
            SetCoreStatus(false, false);
        }

        private void Update()
        {
            // Aim rotation interpolation
            if (turretYawHead != null)
            {
                if (_isEngaging)
                {
                    turretYawHead.localRotation = Quaternion.Slerp(turretYawHead.localRotation, _targetYaw, Time.deltaTime * aimTrackingSpeed);
                }
                else
                {
                    // Gentle idle sweep across the room
                    _idleTimer += Time.deltaTime * idleScanSpeed;
                    float idleAngle = Mathf.Sin(_idleTimer) * 28.0f;
                    turretYawHead.localRotation = Quaternion.Euler(0, idleAngle, 0);
                }
            }

            if (turretPitchGimbal != null && _isEngaging)
            {
                turretPitchGimbal.localRotation = Quaternion.Slerp(turretPitchGimbal.localRotation, _targetPitch, Time.deltaTime * aimTrackingSpeed);
            }

            // Core plasma pulse
            if (sentryCorePlasma != null && sentryCorePlasma.material != null)
            {
                float pulse = 1.0f + 0.3f * Mathf.Sin(Time.time * 4.0f);
                Color baseCol = (chamberController != null && chamberController.HasSolved) ? friendlyEmerald : alertRed;
                sentryCorePlasma.material.SetColor("_EmissionColor", baseCol * pulse);
            }
        }

        private void HandleForwardPassTriggered()
        {
            if (!gameObject.activeInHierarchy) return;
            if (_engageRoutine != null) StopCoroutine(_engageRoutine);
            _engageRoutine = StartCoroutine(RoutineRunEngagementSequence());
        }

        private void HandleSingleCaseEvaluated(int caseIndex, CaseDiagnostic diag, bool activationMatches)
        {
            if (!gameObject.activeInHierarchy) return;
            if (_engageRoutine != null) StopCoroutine(_engageRoutine);
            _engageRoutine = StartCoroutine(RoutineEngageSingleCase(caseIndex, diag, activationMatches));
        }

        private void HandleEvaluationComplete(PuzzleEvaluation eval)
        {
            if (eval == null) return;
            SetCoreStatus(eval.Passed, true);
        }

        private void HandlePuzzleSolved()
        {
            _isEngaging = false;
            if (targetingLaser != null) targetingLaser.enabled = false;
            if (scanConeBeam != null) scanConeBeam.enabled = false;
            if (sentrySpotlight != null)
            {
                sentrySpotlight.color = friendlyEmerald;
                sentrySpotlight.intensity = 3.5f;
            }
        }

        private void HandleChamberReset()
        {
            if (_engageRoutine != null) StopCoroutine(_engageRoutine);
            _isEngaging = false;
            if (targetingLaser != null) targetingLaser.enabled = false;
            if (scanConeBeam != null) scanConeBeam.enabled = false;
            SetCoreStatus(false, false);
        }

        private IEnumerator RoutineRunEngagementSequence()
        {
            _isEngaging = true;
            var receptors = chamberController != null ? chamberController.TargetReceptors : null;

            if (receptors != null)
            {
                for (int i = 0; i < receptors.Count; i++)
                {
                    var receptor = receptors[i];
                    if (receptor == null) continue;

                    Vector3 targetPos = receptor.transform.position;
                    AimAt(targetPos);
                    yield return new WaitForSeconds(0.18f);

                    // Read live diagnostic for this case
                    var diag = receptor.LastDiagnostic;
                    bool outputEngage = (diag != null && diag.ActualOutput >= 0.5);
                    bool isHarmonized = receptor.IsHarmonized;

                    yield return StartCoroutine(RoutineFireAtTarget(targetPos, outputEngage, isHarmonized, receptor.CaseIndex));
                    yield return new WaitForSeconds(0.12f);
                }
            }

            _isEngaging = false;
            if (targetingLaser != null) targetingLaser.enabled = false;
            if (scanConeBeam != null) scanConeBeam.enabled = false;
        }

        private IEnumerator RoutineEngageSingleCase(int caseIndex, CaseDiagnostic diag, bool activationMatches)
        {
            _isEngaging = true;
            var receptors = chamberController != null ? chamberController.TargetReceptors : null;
            if (receptors != null && caseIndex >= 0 && caseIndex < receptors.Count && receptors[caseIndex] != null)
            {
                Vector3 targetPos = receptors[caseIndex].transform.position;
                AimAt(targetPos);
                yield return new WaitForSeconds(0.15f);

                bool outputEngage = (diag != null && diag.ActualOutput >= 0.5);
                bool isHarmonized = (diag != null && diag.IsCorrect && activationMatches);

                yield return StartCoroutine(RoutineFireAtTarget(targetPos, outputEngage, isHarmonized, caseIndex));
            }

            _isEngaging = false;
            if (targetingLaser != null) targetingLaser.enabled = false;
            if (scanConeBeam != null) scanConeBeam.enabled = false;
        }

        private IEnumerator RoutineFireAtTarget(Vector3 targetPos, bool outputEngage, bool isHarmonized, int caseIndex)
        {
            Vector3 fireOrigin = leftMuzzle != null ? leftMuzzle.position : transform.position + Vector3.up * 1.5f;

            if (outputEngage)
            {
                // Threat Intercept: High-Energy Plasma Laser Blast
                if (targetingLaser != null)
                {
                    targetingLaser.enabled = true;
                    targetingLaser.startColor = plasmaAmber;
                    targetingLaser.endColor = alertRed;
                    targetingLaser.startWidth = 0.08f;
                    targetingLaser.endWidth = 0.12f;
                    targetingLaser.SetPosition(0, fireOrigin);
                    targetingLaser.SetPosition(1, targetPos);
                }

                if (muzzleFlashLight != null)
                {
                    muzzleFlashLight.enabled = true;
                    muzzleFlashLight.color = plasmaAmber;
                    muzzleFlashLight.intensity = 5.0f;
                }

                if (plasmaDischargeParticles != null)
                {
                    plasmaDischargeParticles.transform.position = fireOrigin;
                    plasmaDischargeParticles.Play();
                }

                yield return new WaitForSeconds(0.14f);

                if (muzzleFlashLight != null) muzzleFlashLight.enabled = false;
                if (targetingLaser != null) targetingLaser.enabled = false;
            }
            else
            {
                // Friendly Scan: Soft Emerald/Cyan Identification Cone
                if (scanConeBeam != null)
                {
                    scanConeBeam.enabled = true;
                    scanConeBeam.startColor = scanningCyan;
                    scanConeBeam.endColor = isHarmonized ? friendlyEmerald : alertRed;
                    scanConeBeam.startWidth = 0.04f;
                    scanConeBeam.endWidth = 0.45f;
                    scanConeBeam.SetPosition(0, fireOrigin);
                    scanConeBeam.SetPosition(1, targetPos);
                }

                yield return new WaitForSeconds(0.16f);
                if (scanConeBeam != null) scanConeBeam.enabled = false;
            }

            if (!isHarmonized && alertSteamParticles != null)
            {
                alertSteamParticles.Play();
            }
        }

        public void AimAt(Vector3 worldTargetPos)
        {
            if (turretYawHead == null) return;

            Vector3 dir = worldTargetPos - turretYawHead.position;
            Vector3 horizontalDir = new Vector3(dir.x, 0, dir.z);

            if (horizontalDir.sqrMagnitude > 0.001f)
            {
                Quaternion worldYaw = Quaternion.LookRotation(horizontalDir.normalized, Vector3.up);
                _targetYaw = turretBase != null ? Quaternion.Inverse(turretBase.rotation) * worldYaw : worldYaw;
            }

            if (turretPitchGimbal != null)
            {
                float elevationAngle = -Mathf.Atan2(dir.y, horizontalDir.magnitude) * Mathf.Rad2Deg;
                _targetPitch = Quaternion.Euler(Mathf.Clamp(elevationAngle, -35f, 35f), 0, 0);
            }
        }

        private void SetCoreStatus(bool isSolved, bool hasEvaluated)
        {
            Color col = isSolved ? friendlyEmerald : (hasEvaluated ? alertRed : alertRed * 0.7f);
            if (statusStrobeRenderer != null && statusStrobeRenderer.material != null)
            {
                statusStrobeRenderer.material.SetColor("_EmissionColor", col * 2.0f);
            }
            if (sentrySpotlight != null)
            {
                sentrySpotlight.color = col;
            }
        }
    }
}
