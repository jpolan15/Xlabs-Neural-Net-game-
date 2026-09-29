using UnityEngine;

namespace Convergence.XR
{
    /// <summary>
    /// Slides the player rig through the Level 1 bridge without tunneling the body
    /// through walls, the deck consoles, or the canopy.
    /// Bounds match the backing colliders built by Level01SceneBuilder.
    /// </summary>
    public static class PlayspaceMotor
    {
        public const float BodyRadius = 0.22f;
        public const float BodyHeight = 1.55f;
        public const float Skin = 0.04f;

        // Inner faces of BackingCollider_Port/Starboard/Aft and Viewport_BackingCollider, minus the body radius.
        public const float MinX = -3.43f;
        public const float MaxX = 3.43f;
        public const float MinZ = -3.53f;
        public const float MaxZ = 3.43f;

        private static readonly Collider[] OverlapBuffer = new Collider[12];

        public static Vector3 Move(Vector3 origin, Vector3 delta)
        {
            delta.y = 0f;
            Vector3 resolved = ResolveOverlaps(origin);
            if (delta.sqrMagnitude > 1e-10f)
                resolved = Slide(resolved, delta);

            resolved.y = origin.y;
            resolved.x = Mathf.Clamp(resolved.x, MinX, MaxX);
            resolved.z = Mathf.Clamp(resolved.z, MinZ, MaxZ);
            return resolved;
        }

        private static Vector3 Slide(Vector3 origin, Vector3 delta)
        {
            Vector3 first = Cast(origin, delta);
            Vector3 consumed = first - origin;
            Vector3 leftover = delta - consumed;
            leftover.y = 0f;
            if (leftover.sqrMagnitude < 1e-8f)
                return first;

            return Cast(first, leftover);
        }

        private static Vector3 Cast(Vector3 origin, Vector3 delta)
        {
            float distance = delta.magnitude;
            if (distance < 1e-6f)
                return origin;

            Vector3 direction = delta / distance;
            Capsule(origin, out Vector3 bottom, out Vector3 top);
            if (Physics.CapsuleCast(
                    bottom,
                    top,
                    BodyRadius,
                    direction,
                    out RaycastHit hit,
                    distance + Skin,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                float allowed = Mathf.Max(0f, hit.distance - Skin);
                return origin + (direction * allowed);
            }

            return origin + delta;
        }

        private static Vector3 ResolveOverlaps(Vector3 origin)
        {
            Capsule(origin, out Vector3 bottom, out Vector3 top);
            int count = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                BodyRadius,
                OverlapBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            Vector3 push = Vector3.zero;
            Vector3 bodyCenter = origin + (Vector3.up * (BodyHeight * 0.5f));
            for (int i = 0; i < count; i++)
            {
                Collider collider = OverlapBuffer[i];
                OverlapBuffer[i] = null;
                if (collider == null || collider.isTrigger)
                    continue;
                // Deck plates sit under the feet. They should not shove the rig sideways.
                if (collider.bounds.max.y < 0.25f)
                    continue;

                Vector3 closest = collider.ClosestPoint(bodyCenter);
                Vector3 away = bodyCenter - closest;
                away.y = 0f;
                if (away.sqrMagnitude < 1e-4f)
                {
                    away = origin - collider.bounds.center;
                    away.y = 0f;
                }
                if (away.sqrMagnitude < 1e-4f)
                    away = Vector3.back;

                push += away.normalized * (Skin + 0.01f);
            }

            origin += push;
            return origin;
        }

        private static void Capsule(Vector3 origin, out Vector3 bottom, out Vector3 top)
        {
            bottom = origin + (Vector3.up * (BodyRadius + 0.02f));
            top = origin + (Vector3.up * (BodyHeight - BodyRadius));
            if (top.y < bottom.y)
                top = bottom;
        }
    }
}
