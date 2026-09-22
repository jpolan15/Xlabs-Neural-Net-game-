using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// First-person viewmodel firearm representation of the Neural Pulse Tool.
    /// Attached to the player camera, featuring breathing sway, snappy weapon recoil,
    /// intense muzzle flash, and a high-voltage laser beam connecting to the Convergence Core.
    /// Strictly presentation/visual — observes Gameplay events and never initiates evaluations.
    /// </summary>
    public class NeuralGunViewmodel : MonoBehaviour
    {
        [Header("State Listener")]
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private NeuralState neuralState;

        [Header("Gun Rig Transforms")]
        [SerializeField] private Transform gunChassis;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private Transform targetCore;

        [Header("VFX Components")]
        [SerializeField] private Light muzzleLight;
        [SerializeField] private LineRenderer laserBeam;

        [Header("Sway & Breathing")]
        [SerializeField] private float swayAmplitude = 0.008f;
        [SerializeField] private float swayFrequency = 1.4f;

        [Header("Recoil Settings")]
        [SerializeField] private float recoilKickZ = -0.07f;
        [SerializeField] private float recoilPitchDeg = 4.5f;
        [SerializeField] private float recoilRecoverySpeed = 12.0f;

        [Header("Beam Colors")]
        [SerializeField] private Color beamLinearColor = new Color(0.2f, 0.95f, 1.0f, 1.0f);   // Cyan
        [SerializeField] private Color beamReluColor = new Color(1.0f, 0.7f, 0.0f, 1.0f);      // Amber
        [SerializeField] private Color beamStepColor = new Color(0.0f, 1.0f, 0.45f, 1.0f);     // Emerald
        [SerializeField] private Color beamSigmoidColor = new Color(0.85f, 0.0f, 1.0f, 1.0f);  // Violet

        private Vector3 _defaultChassisPos;
        private Quaternion _defaultChassisRot;

        private Vector3 _recoilOffset;
        private float _recoilPitch;

        private float _beamTimer = 0.0f;
        private const float BeamDuration = 0.22f;
        private Vector3 _activeBeamEndPos;
        private bool _hasCustomBeamEnd;

        private void Awake()
        {
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();

            if (gunChassis != null)
            {
                _defaultChassisPos = gunChassis.localPosition;
                _defaultChassisRot = gunChassis.localRotation;
            }

            if (laserBeam != null)
            {
                laserBeam.positionCount = 2;
                laserBeam.enabled = false;
            }

            if (muzzleLight != null)
            {
                muzzleLight.intensity = 0.0f;
            }
        }

        private void OnEnable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered += HandleForwardPass;
                chamberController.OnSingleCaseEvaluated += HandleSingleCaseEvaluated;
            }
        }

        private void OnDisable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered -= HandleForwardPass;
                chamberController.OnSingleCaseEvaluated -= HandleSingleCaseEvaluated;
            }
        }

        private void HandleSingleCaseEvaluated(int caseIndex, Convergence.Core.Puzzles.CaseDiagnostic diag, bool activationMatches)
        {
            if (chamberController != null && chamberController.TargetReceptors != null && caseIndex < chamberController.TargetReceptors.Count)
            {
                var target = chamberController.TargetReceptors[caseIndex];
                if (target != null)
                {
                    FireAtTarget(target.transform.position);
                    return;
                }
            }
            HandleForwardPass();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float time = Time.time;

            // 1. Recover Recoil
            _recoilOffset = Vector3.Lerp(_recoilOffset, Vector3.zero, dt * recoilRecoverySpeed);
            _recoilPitch = Mathf.Lerp(_recoilPitch, 0.0f, dt * recoilRecoverySpeed);

            // 2. Idle breathing sway
            float swayX = Mathf.Sin(time * swayFrequency * 0.5f) * swayAmplitude;
            float swayY = Mathf.Cos(time * swayFrequency) * (swayAmplitude * 0.7f);
            Vector3 swayVec = new Vector3(swayX, swayY, 0.0f);

            // 3. Apply combined transform to gun chassis
            if (gunChassis != null)
            {
                gunChassis.localPosition = _defaultChassisPos + swayVec + _recoilOffset;
                gunChassis.localRotation = _defaultChassisRot * Quaternion.Euler(-_recoilPitch, 0, 0);
            }

            // 4. Update Laser Beam & Muzzle Light
            if (_beamTimer > 0.0f)
            {
                _beamTimer -= dt;
                float progress = Mathf.Clamp01(_beamTimer / BeamDuration);

                Color activeColor = GetActiveBeamColor();

                if (muzzleLight != null)
                {
                    muzzleLight.color = activeColor;
                    muzzleLight.intensity = progress * 4.0f;
                }

                if (laserBeam != null && laserBeam.enabled)
                {
                    Vector3 start = (muzzlePoint != null) ? muzzlePoint.position : transform.position;
                    Vector3 end;

                    if (_hasCustomBeamEnd)
                    {
                        end = _activeBeamEndPos;
                    }
                    else
                    {
                        end = (targetCore != null) ? targetCore.position : (start + transform.forward * 15.0f);
                    }

                    laserBeam.SetPosition(0, start);
                    laserBeam.SetPosition(1, end);

                    Color cStart = new Color(activeColor.r, activeColor.g, activeColor.b, progress);
                    Color cEnd = new Color(activeColor.r, activeColor.g, activeColor.b, progress * 0.8f);
                    laserBeam.startColor = cStart;
                    laserBeam.endColor = cEnd;
                }

                if (_beamTimer <= 0.0f)
                {
                    _hasCustomBeamEnd = false;
                    if (laserBeam != null) laserBeam.enabled = false;
                    if (muzzleLight != null) muzzleLight.intensity = 0.0f;
                }
            }
        }

        private Color GetActiveBeamColor()
        {
            if (neuralState == null) return beamLinearColor;

            return neuralState.Activation switch
            {
                Convergence.Core.Neural.ActivationType.ReLU => beamReluColor,
                Convergence.Core.Neural.ActivationType.Step => beamStepColor,
                Convergence.Core.Neural.ActivationType.Sigmoid => beamSigmoidColor,
                _ => beamLinearColor
            };
        }

        private void HandleForwardPass()
        {
            _hasCustomBeamEnd = false;
            TriggerRecoilAndBeam();
        }

        private void TriggerRecoilAndBeam()
        {
            _recoilOffset = new Vector3(0, 0.015f, recoilKickZ);
            _recoilPitch = recoilPitchDeg;
            _beamTimer = BeamDuration;

            if (laserBeam != null) laserBeam.enabled = true;
            if (muzzleLight != null) muzzleLight.intensity = 4.0f;
        }

        /// <summary>
        /// Fires laser beam directly at a custom world target position.
        /// </summary>
        public void FireAtTarget(Vector3 worldTarget)
        {
            _hasCustomBeamEnd = true;
            _activeBeamEndPos = worldTarget;
            TriggerRecoilAndBeam();
        }
    }
}
