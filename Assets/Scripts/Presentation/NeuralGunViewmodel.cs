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
        [SerializeField] private Color beamStartColor = new Color(0.2f, 0.95f, 1.0f, 1.0f);
        [SerializeField] private Color beamEndColor = new Color(0.0f, 0.6f, 1.0f, 0.8f);

        private Vector3 _defaultChassisPos;
        private Quaternion _defaultChassisRot;

        private Vector3 _recoilOffset;
        private float _recoilPitch;

        private float _beamTimer = 0.0f;
        private const float BeamDuration = 0.22f;

        private void Awake()
        {
            if (chamberController == null)
            {
                chamberController = FindAnyObjectByType<ChamberController>();
            }

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
            }
        }

        private void OnDisable()
        {
            if (chamberController != null)
            {
                chamberController.OnForwardPassTriggered -= HandleForwardPass;
            }
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

                if (muzzleLight != null)
                {
                    muzzleLight.intensity = progress * 4.0f;
                }

                if (laserBeam != null && laserBeam.enabled)
                {
                    Vector3 start = (muzzlePoint != null) ? muzzlePoint.position : transform.position;
                    Vector3 end = (targetCore != null) ? targetCore.position : (start + transform.forward * 15.0f);

                    laserBeam.SetPosition(0, start);
                    laserBeam.SetPosition(1, end);

                    Color cStart = new Color(beamStartColor.r, beamStartColor.g, beamStartColor.b, progress);
                    Color cEnd = new Color(beamEndColor.r, beamEndColor.g, beamEndColor.b, progress * 0.8f);
                    laserBeam.startColor = cStart;
                    laserBeam.endColor = cEnd;
                }

                if (_beamTimer <= 0.0f)
                {
                    if (laserBeam != null) laserBeam.enabled = false;
                    if (muzzleLight != null) muzzleLight.intensity = 0.0f;
                }
            }
        }

        private void HandleForwardPass()
        {
            // Apply recoil kick
            _recoilOffset = new Vector3(0, 0.015f, recoilKickZ);
            _recoilPitch = recoilPitchDeg;

            // Trigger beam
            _beamTimer = BeamDuration;
            if (laserBeam != null)
            {
                laserBeam.enabled = true;
            }
            if (muzzleLight != null)
            {
                muzzleLight.intensity = 4.0f;
            }
        }

        /// <summary>
        /// Explicit trigger for custom aim targets if desired.
        /// </summary>
        public void FireAtTarget(Vector3 worldTarget)
        {
            HandleForwardPass();
            if (laserBeam != null && muzzlePoint != null)
            {
                laserBeam.SetPosition(0, muzzlePoint.position);
                laserBeam.SetPosition(1, worldTarget);
            }
        }
    }
}
