using UnityEngine;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// The bike under the rider in Level 2 (ADR-023): its wheels roll and its pedals turn exactly as fast as the ride moves (and stop when it
    /// stops, so a rider on a real exercise bike can pedal along), and its handlebars turn when the story road changes lane. The rider's
    /// view never tilts or bobs: only the bike moves. Display only; it reads where the pod went.
    /// </summary>
    public sealed class BikeView : MonoBehaviour
    {
        [SerializeField] private Transform frontWheel;
        [SerializeField] private Transform rearWheel;
        [SerializeField] private Transform crank;
        [SerializeField] private Transform[] pedals = new Transform[0];
        [SerializeField] private Transform bars;
        [SerializeField] private float frontRadius = 0.25f;
        [SerializeField] private float rearRadius = 0.33f;
        [Tooltip("Crank turns per rear wheel turn (a low gear, so the legs move at a calm cadence).")]
        [SerializeField] private float gear = 0.45f;
        [SerializeField] private float maxSteerDegrees = 24f;

        /// <summary>The bike in the scene, for the views that steer it.</summary>
        public static BikeView Current { get; private set; }

        private Vector3 _last;
        private float _front, _rear, _steer, _steerTarget;

        private void OnEnable()
        {
            Current = this;
            _last = transform.position;
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        /// <summary>Turns the handlebars toward <paramref name="amount"/> (-1 left, 1 right); they drift back to straight by themselves.</summary>
        public void Steer(float amount) => _steerTarget = Mathf.Clamp(amount, -1f, 1f);

        private void LateUpdate()
        {
            Vector3 p = transform.position;
            float rolled = Vector3.Dot(p - _last, transform.forward);
            _last = p;
            if (Mathf.Abs(rolled) > 2f) rolled = 0f; // a jump (a restart, a skip) is not a roll

            _front += rolled / frontRadius * Mathf.Rad2Deg;
            _rear += rolled / rearRadius * Mathf.Rad2Deg;
            float crankAngle = _rear * gear;
            if (frontWheel != null) frontWheel.localRotation = Quaternion.Euler(_front, 0f, 0f);
            if (rearWheel != null) rearWheel.localRotation = Quaternion.Euler(_rear, 0f, 0f);
            if (crank != null) crank.localRotation = Quaternion.Euler(crankAngle, 0f, 0f);
            foreach (Transform pedal in pedals)
            {
                if (pedal != null) pedal.localRotation = Quaternion.Euler(-crankAngle, 0f, 0f); // pedals stay level as the crank turns
            }

            float dt = Time.deltaTime;
            _steerTarget = Mathf.MoveTowards(_steerTarget, 0f, dt * 0.7f);
            _steer = Mathf.MoveTowards(_steer, _steerTarget, dt * 2.5f);
            if (bars != null) bars.localRotation = Quaternion.Euler(0f, _steer * maxSteerDegrees, 0f);
        }
    }
}
