using System;
using UnityEngine;

namespace Convergence.Presentation
{
    /// <summary>
    /// Cosmic environment component that manages floating, tumbling asteroids in the deep space void.
    /// Positions asteroids around the station perimeter to frame the orbital observatory.
    /// </summary>
    public class FloatingAsteroidField : MonoBehaviour
    {
        [Header("Asteroid Field Configuration")]
        [SerializeField] private int asteroidCount = 20;
        [SerializeField] private float minRadius = 35.0f;
        [SerializeField] private float maxRadius = 110.0f;
        [SerializeField] private float minY = -25.0f;
        [SerializeField] private float maxY = 35.0f;
        [SerializeField] private Material asteroidMaterial;

        private struct AsteroidData
        {
            public Transform transform;
            public Vector3 tumbleAxis;
            public float tumbleSpeed;
            public float bobSpeed;
            public float bobAmplitude;
            public float initialY;
            public float phaseOffset;
        }

        private AsteroidData[] _asteroids;

        private void Start()
        {
            InitializeField();
        }

        public void InitializeField()
        {
            // If already initialized, don't duplicate
            if (_asteroids != null && _asteroids.Length > 0) return;

            // Check if existing child asteroids exist
            int existingChildren = transform.childCount;
            if (existingChildren > 0)
            {
                _asteroids = new AsteroidData[existingChildren];
                for (int i = 0; i < existingChildren; i++)
                {
                    Transform child = transform.GetChild(i);
                    _asteroids[i] = CreateDataForTransform(child, i);
                }
                return;
            }

            // Otherwise, procedurally generate low-poly asteroid shapes
            _asteroids = new AsteroidData[asteroidCount];
            var rng = new System.Random(42); // Deterministic seed

            for (int i = 0; i < asteroidCount; i++)
            {
                float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                float radius = Mathf.Lerp(minRadius, maxRadius, (float)rng.NextDouble());
                float y = Mathf.Lerp(minY, maxY, (float)rng.NextDouble());

                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    y,
                    Mathf.Sin(angle) * radius
                );

                var asteroidGo = GameObject.CreatePrimitive(rng.Next(2) == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube);
                asteroidGo.name = $"Asteroid_{i:D2}";
                asteroidGo.transform.SetParent(transform);
                asteroidGo.transform.position = pos;

                // Random non-uniform scale for craggy asteroid silhouette
                float baseScale = Mathf.Lerp(2.5f, 9.0f, (float)rng.NextDouble());
                float sx = baseScale * Mathf.Lerp(0.7f, 1.3f, (float)rng.NextDouble());
                float sy = baseScale * Mathf.Lerp(0.6f, 1.2f, (float)rng.NextDouble());
                float sz = baseScale * Mathf.Lerp(0.7f, 1.4f, (float)rng.NextDouble());
                asteroidGo.transform.localScale = new Vector3(sx, sy, sz);

                // Remove collider to save physics overhead
                var col = asteroidGo.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }

                if (asteroidMaterial != null)
                {
                    var rend = asteroidGo.GetComponent<Renderer>();
                    if (rend != null) rend.sharedMaterial = asteroidMaterial;
                }

                _asteroids[i] = CreateDataForTransform(asteroidGo.transform, i);
            }
        }

        private AsteroidData CreateDataForTransform(Transform t, int index)
        {
            var data = new AsteroidData();
            data.transform = t;
            data.initialY = t.position.y;
            data.phaseOffset = index * 1.37f;

            // Tumble axis and speeds
            float hash = Mathf.Sin(index * 99.17f);
            data.tumbleAxis = new Vector3(
                Mathf.Sin(index * 1.5f),
                Mathf.Cos(index * 2.1f),
                Mathf.Sin(index * 3.7f)
            ).normalized;

            data.tumbleSpeed = Mathf.Lerp(5.0f, 22.0f, Mathf.Abs(hash));
            data.bobSpeed = Mathf.Lerp(0.2f, 0.6f, Mathf.Abs(Mathf.Cos(index * 45.3f)));
            data.bobAmplitude = Mathf.Lerp(0.5f, 2.0f, Mathf.Abs(Mathf.Sin(index * 23.4f)));

            return data;
        }

        private void Update()
        {
            if (_asteroids == null) return;

            float dt = Time.deltaTime;
            float time = Time.time;

            for (int i = 0; i < _asteroids.Length; i++)
            {
                Transform t = _asteroids[i].transform;
                if (t == null) continue;

                // Tumble rotation
                t.Rotate(_asteroids[i].tumbleAxis, _asteroids[i].tumbleSpeed * dt, Space.World);

                // Subtle orbital vertical bob
                Vector3 currentPos = t.position;
                float newY = _asteroids[i].initialY + Mathf.Sin((time * _asteroids[i].bobSpeed) + _asteroids[i].phaseOffset) * _asteroids[i].bobAmplitude;
                t.position = new Vector3(currentPos.x, newY, currentPos.z);
            }
        }
    }
}
