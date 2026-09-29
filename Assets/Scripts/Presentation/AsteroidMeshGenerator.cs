using UnityEngine;

namespace Convergence.Presentation
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class AsteroidMeshGenerator : MonoBehaviour
    {
        [Header("Asteroid Settings")]
        public float radius = 1.0f;
        public int subdivisions = 2;
        public float noiseScale = 2.5f;
        public float noiseAmount = 0.3f;

        void Start()
        {
            GenerateAsteroid();
        }

        public void GenerateAsteroid()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            Mesh mesh = CreateIcoSphere(subdivisions, radius);

            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                float noise = Mathf.PerlinNoise(v.x * noiseScale, v.y * noiseScale);
                vertices[i] += v.normalized * noise * noiseAmount;
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.mesh = mesh;
        }

        // Basic IcoSphere generation
        private Mesh CreateIcoSphere(int subdivisions, float radius)
        {
            Mesh mesh = new Mesh();
            // A simple placeholder sphere. For a real asteroid, 
            // you'd triangulate an icosahedron here.
            // Using a built-in sphere as base to keep it simple, 
            // and applying noise in GenerateAsteroid.
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh baseMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            
            mesh.vertices = baseMesh.vertices;
            mesh.triangles = baseMesh.triangles;
            mesh.normals = baseMesh.normals;
            mesh.uv = baseMesh.uv;
            
            Destroy(primitive);
            
            for(int i = 0; i < mesh.vertices.Length; i++)
            {
                mesh.vertices[i] = mesh.vertices[i].normalized * radius;
            }

            return mesh;
        }
    }
}
