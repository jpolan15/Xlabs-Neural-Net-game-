using UnityEngine;
using UnityEditor;

namespace Convergence.EditorTools
{
    public class SceneVisualFixer : EditorWindow
    {
        [MenuItem("Tools/Fix Scene Visuals")]
        public static void FixVisuals()
        {
            int textFixed = 0;
            int cubesFixed = 0;

            // 1. Fix giant TextMesh components
            var textMeshes = FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
            foreach (var tm in textMeshes)
            {
                if (tm.characterSize > 0.01f || tm.transform.localScale.magnitude > 2.0f)
                {
                    Undo.RecordObject(tm, "Fix Text Size");
                    // We reduce character size massively if it's currently giant
                    if (tm.characterSize > 0.01f)
                    {
                        tm.characterSize = 0.01f;
                    }
                    
                    Undo.RecordObject(tm.transform, "Fix Text Scale");
                    // If the text has a massive transform scale, tone it down
                    if (tm.transform.localScale.x > 1f)
                    {
                        tm.transform.localScale = Vector3.one;
                    }
                    textFixed++;
                }
            }

            // 2. Fix the giant magenta cube (ICE box)
            var renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var rend in renderers)
            {
                if (rend.sharedMaterial == null || rend.sharedMaterial.name.Contains("Hidden/InternalErrorShader"))
                {
                    // Magenta means broken or null material. Let's try assigning a default material.
                    Undo.RecordObject(rend, "Fix Missing Material");
                    rend.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
                    
                    if (rend.transform.localScale.magnitude > 5.0f)
                    {
                        Undo.RecordObject(rend.transform, "Fix Cube Scale");
                        rend.transform.localScale = Vector3.one * 0.2f;
                    }
                    cubesFixed++;
                }
            }

            Debug.Log($"[SceneVisualFixer] Applied visual fixes. Scaled down {textFixed} giant text labels and fixed {cubesFixed} broken cubes.");
        }
    }
}
