using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Convergence.Presentation;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Places Kenney modules at scale (1,1,1) and rejects stretched textured meshes.
    /// </summary>
    public static class KenneyKit
    {
        const float Eps = 0.001f;
        const string AtlasName = "variation-a";

        public static void Mark(GameObject instance)
        {
            if (instance == null) return;
            if (instance.GetComponent<KenneyModuleMark>() == null)
            {
                instance.AddComponent<KenneyModuleMark>();
            }
        }

        public static void Place(GameObject module, Vector3 position, Quaternion rotation, Material material)
        {
            if (module == null) return;
            module.transform.SetPositionAndRotation(position, rotation);
            module.transform.localScale = Vector3.one;
            Mark(module);
            if (material != null)
            {
                foreach (var renderer in module.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = material;
                }
            }

            Vector3 scale = module.transform.localScale;
            if (scale != Vector3.one)
            {
                throw new System.Exception("KenneyKit.Place scale is not (1,1,1) on " + module.name);
            }
        }

        public static void ForceScaleOne()
        {
            foreach (var mark in Object.FindObjectsByType<KenneyModuleMark>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                mark.transform.localScale = Vector3.one;
            }
        }

        public static void Run(SceneAssetRoot roots)
        {
            var problems = new List<string>();
            foreach (var root in roots.Roots)
            {
                if (root == null) continue;
                foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!mr.enabled) continue;
                    Vector3 s = mr.transform.lossyScale;
                    bool uniform = Mathf.Abs(s.x - s.y) < Eps && Mathf.Abs(s.y - s.z) < Eps;
                    bool textured = false;
                    foreach (var m in mr.sharedMaterials)
                    {
                        if (m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null)
                        {
                            textured = true;
                        }
                    }

                    if (textured && !uniform && !UnderUniformKenney(mr))
                    {
                        // Kenney FBX children keep the vendor's own proportions.
                        // A uniform instance root is the scene rule. Stretching that root is the failure.
                        problems.Add("Textured mesh non-uniform: " + PathOf(mr.transform) + " scale=" + s);
                    }
                }

                foreach (var mark in root.GetComponentsInChildren<KenneyModuleMark>(true))
                {
                    Vector3 local = mark.transform.localScale;
                    if (local != Vector3.one)
                    {
                        problems.Add("Kenney root scale is not (1,1,1): " + PathOf(mark.transform) + " scale=" + local);
                    }

                    foreach (var renderer in mark.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer.sharedMaterial != null && renderer.sharedMaterial.name == "Mat_Spaceship_Hull")
                        {
                            problems.Add("Kenney renderer uses Mat_Spaceship_Hull: " + PathOf(renderer.transform));
                        }
                    }
                }
            }

            var station = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Kenney_SpaceStation.mat");
            Texture atlas = station != null && station.HasProperty("_BaseMap") ? station.GetTexture("_BaseMap") : null;
            if (atlas == null || atlas.name != AtlasName)
            {
                problems.Add("Mat_Kenney_SpaceStation _BaseMap is not variation-a.");
            }

            string project = Directory.GetParent(Application.dataPath).FullName;
            string report = Path.Combine(project, "Temp", "chamber01_scale_guard.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(report));
            if (problems.Count > 0)
            {
                File.WriteAllText(report, "FAIL\n" + string.Join("\n", problems));
                throw new System.Exception("SceneScaleGuard failed:\n" + string.Join("\n", problems));
            }

            File.WriteAllText(report, "PASS\n" + AtlasName);
        }

        static bool UnderUniformKenney(Renderer renderer)
        {
            var mark = renderer.GetComponentInParent<KenneyModuleMark>();
            return mark != null && mark.transform.localScale == Vector3.one;
        }

        static string PathOf(Transform t)
        {
            var p = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                p = t.name + "/" + p;
            }

            return p;
        }

        public readonly struct SceneAssetRoot
        {
            public SceneAssetRoot(GameObject[] roots)
            {
                Roots = roots;
            }

            public GameObject[] Roots { get; }
        }
    }
}
