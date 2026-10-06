using System;
using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// One value on the pod dash. The XR lever writes it, a station reads it, and the trainer can drive it.
    /// Holds no neural math: it is a clamped, snapped number with a lock.
    /// </summary>
    public sealed class RideControlChannel : MonoBehaviour
    {
        [SerializeField] private string id = "";
        [SerializeField] private float min = -2f;
        [SerializeField] private float max = 2f;
        [SerializeField] private float step = 0.5f;
        [SerializeField] private float value;
        [SerializeField] private bool locked;
        [SerializeField] private bool visible = true;

        /// <summary>Stable id, see <see cref="RideControlIds"/>.</summary>
        public string Id => id;
        public float Min => min;
        public float Max => max;
        public float Step => step;
        public float Value => value;

        /// <summary>A locked control ignores the player. It can still be driven by a station.</summary>
        public bool Locked => locked;

        /// <summary>A hidden control is not part of this stop.</summary>
        public bool Visible => visible;

        /// <summary>Raised on every value change. The bool is true when the player moved it.</summary>
        public event Action<RideControlChannel, bool> Changed;

        /// <summary>Raised when the lock or visibility changes.</summary>
        public event Action<RideControlChannel> StateChanged;

        /// <summary>Sets the id. Used by the scene builder.</summary>
        public void SetId(string newId) => id = newId;

        /// <summary>The value as a 0..1 fraction of the range.</summary>
        public float Fraction => Mathf.Approximately(max, min) ? 0f : Mathf.InverseLerp(min, max, value);

        /// <summary>The player moved the lever. Snaps to the detent grid. Ignored while locked.</summary>
        public void SetFromUser(float requested)
        {
            if (locked) return;
            float snapped = Snap(requested);
            if (Mathf.Approximately(snapped, value)) return;
            value = snapped;
            Changed?.Invoke(this, true);
        }

        /// <summary>A station moved the lever by itself. No snapping, so a trainer can land between detents.</summary>
        public void Drive(float requested)
        {
            float clamped = Mathf.Clamp(requested, min, max);
            if (Mathf.Approximately(clamped, value)) return;
            value = clamped;
            Changed?.Invoke(this, false);
        }

        /// <summary>Sets range, value, lock, and visibility in one go when a stop begins.</summary>
        public void Configure(float newMin, float newMax, float newStep, float newValue, bool isLocked, bool isVisible)
        {
            min = newMin;
            max = newMax;
            step = Mathf.Max(0.0001f, newStep);
            value = Mathf.Clamp(newValue, min, max);
            locked = isLocked;
            visible = isVisible;
            StateChanged?.Invoke(this);
            Changed?.Invoke(this, false);
        }

        private float Snap(float requested)
        {
            float clamped = Mathf.Clamp(requested, min, max);
            float steps = Mathf.Round((clamped - min) / step);
            return Mathf.Clamp(min + steps * step, min, max);
        }
    }
}
