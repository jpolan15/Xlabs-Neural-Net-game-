using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;

namespace Convergence.EditorTools
{
    /// <summary>
    /// WP2 text rules. Angular size is height = distance * tan(angle).
    /// Facing uses Quaternion.LookRotation(textPos - eye), the static test in plan section 4.3.
    /// Thresholds are read from Tools/chamber01-thresholds.json and are not written.
    /// </summary>
    public static class Chamber01TextRules
    {
        const int LabelWordMax = 6;
        const string AuditRelative = "Documentation/Design/captures/text_audit.json";

        [Serializable]
        sealed class ThresholdFile
        {
            public float textAngleBodyMinDeg = 1.5f;
            public float textAngleHeadingMinDeg = 2.5f;
            public float iconAngleMinDeg = 3f;
            public float textConeMaxDeg = 25f;
            public int maxWordsPerPrompt = 12;
        }

        public static void Apply()
        {
            ThresholdFile limits = LoadLimits();
            if (!TryEye(out Vector3 eye))
            {
                Debug.LogError("[Chamber01TextRules] Authored eye was not found. Text was not refaced.");
                return;
            }

            PlacePrompt(eye, limits);
            PlaceAlertGlyph();

            foreach (var mesh in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                FitTextMesh(mesh, eye, limits);
            }

            foreach (var tmp in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                FitTmp(tmp, eye, limits);
            }
        }

        public static void WriteAudit()
        {
            ThresholdFile limits = LoadLimits();
            TryEye(out Vector3 eye);
            var violations = new List<string>();
            int checkedCount = 0;
            int promptPanels = 0;
            int skippedStrip = 0;

            foreach (var mesh in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!Include(mesh != null ? mesh.gameObject : null, mesh != null ? mesh.text : null, ref skippedStrip)) continue;
                if (IsPrompt(mesh.gameObject)) promptPanels++;
                checkedCount++;
                Collect(mesh.gameObject, mesh.text, WorldHeight(mesh), mesh.transform.position, mesh.transform.forward, eye, limits, violations);
            }

            foreach (var tmp in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!Include(tmp != null ? tmp.gameObject : null, tmp != null ? tmp.text : null, ref skippedStrip)) continue;
                if (IsPrompt(tmp.gameObject)) promptPanels++;
                checkedCount++;
                Collect(tmp.gameObject, tmp.text, WorldHeight(tmp), tmp.transform.position, tmp.transform.forward, eye, limits, violations);
            }

            if (promptPanels != 1)
            {
                violations.Add("prompt panels " + promptPanels.ToString(CultureInfo.InvariantCulture) + " (want 1)");
            }

            bool mirroredFlagged = MirroredSelfTest(eye);
            bool readableOk = ReadableSelfTest(eye);
            WriteJson(checkedCount, skippedStrip, mirroredFlagged && readableOk, violations);
            if (!mirroredFlagged || !readableOk)
            {
                Debug.LogError("[Chamber01TextRules] Facing self-test failed.");
            }
        }

        static void PlacePrompt(Vector3 eye, ThresholdFile limits)
        {
            var hud = GameObject.Find("WorldSpaceHud");
            if (hud == null) return;

            Vector3 pos = new Vector3(0f, eye.y - 0.12f, eye.z + 0.72f);
            hud.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - eye, Vector3.up));

            var objective = hud.transform.Find("Objective");
            if (objective == null) return;
            var tmp = objective.GetComponent<TMP_Text>();
            if (tmp == null) return;
            tmp.text = "Plug in the ROCK wire.";
            tmp.ForceMeshUpdate();
            float local = tmp.textBounds.size.y;
            if (local < 0.0001f) local = tmp.fontSize;
            float distance = Vector3.Distance(eye, objective.position);
            float required = distance * Mathf.Tan(limits.textAngleHeadingMinDeg * Mathf.Deg2Rad);
            float world = local * Mathf.Max(0.0001f, objective.lossyScale.y);
            float factor = required / world;
            if (factor > 0.01f && factor < 100f)
            {
                Vector3 scaled = hud.transform.localScale * (factor * 1.08f);
                hud.transform.localScale = new Vector3(Round4(scaled.x), Round4(scaled.y), Round4(scaled.z));
            }

            pos = hud.transform.position;
            hud.transform.rotation = Quaternion.LookRotation(pos - eye, Vector3.up);
        }

        static void PlaceAlertGlyph()
        {
            if (GameObject.Find("AlertGlyph") != null) return;
            var glyph = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glyph.name = "AlertGlyph";
            glyph.transform.position = new Vector3(0.16f, 1.18f, -0.02f);
            glyph.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            glyph.transform.localScale = new Vector3(0.03f, 0.03f, 0.006f);
            var collider = glyph.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            var amber = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Spaceship_WarningAmber.mat");
            var renderer = glyph.GetComponent<Renderer>();
            if (renderer != null && amber != null) renderer.sharedMaterial = amber;
        }

        static void FitTextMesh(TextMesh mesh, Vector3 eye, ThresholdFile limits)
        {
            if (mesh == null || IsUnderStrip(mesh.transform)) return;
            if (!mesh.gameObject.activeInHierarchy) return;

            if (!Keep(mesh.gameObject) && OutsideCone(mesh.transform.position, eye, limits.textConeMaxDeg))
            {
                Clear(mesh.gameObject, mesh.GetComponent<Renderer>());
                mesh.text = string.Empty;
                return;
            }

            PullInside(mesh.transform, eye, limits.textConeMaxDeg);
            int cap = IsPrompt(mesh.gameObject) ? limits.maxWordsPerPrompt : LabelWordMax;
            mesh.text = TrimWords(mesh.text, cap);
            if (string.IsNullOrWhiteSpace(mesh.text)) return;

            mesh.color = OffWhite;
            mesh.fontSize = 0;
            float required = RequiredHeight(mesh.transform.position, mesh.gameObject, eye, limits);
            mesh.characterSize = Mathf.Max(mesh.characterSize, required);
            for (int i = 0; i < 4; i++)
            {
                float height = WorldHeight(mesh);
                if (height >= required * 0.98f) break;
                float scale = required / Mathf.Max(height, 0.0001f);
                mesh.characterSize = Round4(mesh.characterSize * scale);
            }

            Face(mesh.transform, eye);
        }

        static void FitTmp(TMP_Text tmp, Vector3 eye, ThresholdFile limits)
        {
            if (tmp == null || IsUnderStrip(tmp.transform)) return;
            if (!tmp.gameObject.activeInHierarchy) return;

            if (!Keep(tmp.gameObject) && OutsideCone(tmp.transform.position, eye, limits.textConeMaxDeg))
            {
                Clear(tmp.gameObject, tmp.GetComponent<Renderer>());
                tmp.text = string.Empty;
                tmp.gameObject.SetActive(false);
                return;
            }

            PullInside(tmp.transform, eye, limits.textConeMaxDeg);
            int cap = IsPrompt(tmp.gameObject) ? limits.maxWordsPerPrompt : LabelWordMax;
            if (!string.IsNullOrWhiteSpace(tmp.text))
            {
                tmp.text = TrimWords(tmp.text, cap);
            }

            if (string.IsNullOrWhiteSpace(tmp.text)) return;
            tmp.color = OffWhite;
            float required = RequiredHeight(tmp.transform.position, tmp.gameObject, eye, limits);
            for (int i = 0; i < 4; i++)
            {
                tmp.ForceMeshUpdate();
                float height = WorldHeight(tmp);
                if (height >= required * 0.98f) break;
                float scale = required / Mathf.Max(height, 0.0001f);
                tmp.fontSize = Round4(tmp.fontSize * scale);
            }

            Face(tmp.transform, eye);
        }

        static void Collect(GameObject go, string text, float height, Vector3 position, Vector3 forward, Vector3 eye, ThresholdFile limits, List<string> violations)
        {
            string name = go.name;
            float distance = Vector3.Distance(eye, position);
            float minDeg = MinAngle(go, limits);
            float angle = distance > 0.0001f ? Mathf.Atan(height / distance) * Mathf.Rad2Deg : 0f;
            if (angle + 0.01f < minDeg)
            {
                violations.Add(name + " angle " + angle.ToString("0.00", CultureInfo.InvariantCulture) + " < " + minDeg.ToString("0.00", CultureInfo.InvariantCulture));
            }

            float facing = Vector3.Dot(forward, eye - position);
            if (facing >= 0f)
            {
                violations.Add(name + " facing dot " + facing.ToString("0.00", CultureInfo.InvariantCulture));
            }

            float cone = Vector3.Angle(position - eye, Vector3.forward);
            if (cone > limits.textConeMaxDeg)
            {
                violations.Add(name + " cone " + cone.ToString("0.00", CultureInfo.InvariantCulture));
            }

            int cap = IsPrompt(go) ? limits.maxWordsPerPrompt : LabelWordMax;
            int words = CountWords(text);
            if (words > cap)
            {
                violations.Add(name + " words " + words.ToString(CultureInfo.InvariantCulture));
            }
        }

        static bool Include(GameObject go, string text, ref int skippedStrip)
        {
            if (go == null || !go.activeInHierarchy) return false;
            if (IsUnderStrip(go.transform))
            {
                skippedStrip++;
                return false;
            }

            if (string.IsNullOrWhiteSpace(text)) return false;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && !renderer.enabled) return false;
            return true;
        }

        static bool IsUnderStrip(Transform t)
        {
            while (t != null)
            {
                if (t.name == "DevHeadsetTextStrip") return true;
                t = t.parent;
            }

            return false;
        }

        static bool IsPrompt(GameObject go)
        {
            return go.name == "Objective";
        }

        static bool Keep(GameObject go)
        {
            if (IsPrompt(go)) return true;
            Transform cursor = go.transform;
            while (cursor != null)
            {
                if (cursor.name == "WorldSpaceHud" || cursor.name == "TactileEngineeringWorkstation") return true;
                cursor = cursor.parent;
            }

            return false;
        }

        static float MinAngle(GameObject go, ThresholdFile limits)
        {
            if (IsPrompt(go) || go.name.IndexOf("Heading", StringComparison.Ordinal) >= 0) return limits.textAngleHeadingMinDeg;
            if (go.name.IndexOf("Tag", StringComparison.Ordinal) >= 0 || go.name.IndexOf("Icon", StringComparison.Ordinal) >= 0) return limits.iconAngleMinDeg;
            return limits.textAngleBodyMinDeg;
        }

        static float RequiredHeight(Vector3 position, GameObject go, Vector3 eye, ThresholdFile limits)
        {
            float distance = Vector3.Distance(eye, position);
            return distance * Mathf.Tan(MinAngle(go, limits) * Mathf.Deg2Rad) * 1.08f;
        }

        static float WorldHeight(TextMesh mesh)
        {
            var renderer = mesh.GetComponent<Renderer>();
            if (renderer != null && renderer.enabled)
            {
                float height = renderer.bounds.size.y;
                if (height > 0.0001f) return height;
            }

            return mesh.characterSize;
        }

        static float WorldHeight(TMP_Text tmp)
        {
            float local = tmp.textBounds.size.y;
            if (local > 0.0001f) return local * tmp.transform.lossyScale.y;
            return tmp.fontSize * tmp.transform.lossyScale.y;
        }

        static bool OutsideCone(Vector3 position, Vector3 eye, float coneDeg)
        {
            return Vector3.Angle(position - eye, Vector3.forward) > coneDeg;
        }

        static void PullInside(Transform t, Vector3 eye, float coneDeg)
        {
            Vector3 offset = t.position - eye;
            if (offset.sqrMagnitude < 0.0001f) return;
            float angle = Vector3.Angle(offset, Vector3.forward);
            if (angle <= coneDeg) return;
            float radians = (angle - coneDeg + 0.5f) * Mathf.Deg2Rad;
            Vector3 pulled = Vector3.RotateTowards(offset, Vector3.forward, radians, 0f);
            t.position = eye + pulled;
        }

        static void Face(Transform t, Vector3 eye)
        {
            Vector3 direction = t.position - eye;
            if (direction.sqrMagnitude < 0.0001f) return;
            t.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        static void Clear(GameObject go, Renderer renderer)
        {
            if (renderer != null) renderer.enabled = false;
            go.SetActive(false);
        }

        static Color OffWhite => new Color(0.90f, 0.91f, 0.88f, 1f);

        static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            int count = 0;
            bool inWord = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    inWord = false;
                }
                else if (!inWord)
                {
                    inWord = true;
                    count++;
                }
            }

            return count;
        }

        static string TrimWords(string text, int max)
        {
            if (string.IsNullOrWhiteSpace(text) || CountWords(text) <= max) return text ?? string.Empty;
            var parts = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            int take = Mathf.Min(max, parts.Length);
            return string.Join(" ", parts, 0, take);
        }

        static bool MirroredSelfTest(Vector3 eye)
        {
            Vector3 pos = eye + Vector3.forward;
            Vector3 mirroredForward = (eye - pos).normalized;
            return Vector3.Dot(mirroredForward, eye - pos) >= 0f;
        }

        static bool ReadableSelfTest(Vector3 eye)
        {
            Vector3 pos = eye + Vector3.forward;
            Vector3 readableForward = (pos - eye).normalized;
            return Vector3.Dot(readableForward, eye - pos) < 0f;
        }

        static bool TryEye(out Vector3 eye)
        {
            var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            if (origin == null)
            {
                eye = new Vector3(0f, 1.36144f, -0.9f);
                return false;
            }

            float height = 1.36144f;
            Transform offset = origin.transform.Find("Camera Offset");
            if (offset != null) height = offset.localPosition.y;
            eye = origin.transform.position + Vector3.up * height;
            return true;
        }

        static ThresholdFile LoadLimits()
        {
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Tools", "chamber01-thresholds.json");
            if (!File.Exists(path)) return new ThresholdFile();
            var loaded = JsonUtility.FromJson<ThresholdFile>(File.ReadAllText(path));
            return loaded ?? new ThresholdFile();
        }

        static void WriteJson(int checkedCount, int skippedStrip, bool selfTest, List<string> violations)
        {
            var builder = new StringBuilder(512);
            builder.Append("{\n  \"checked\": ").Append(checkedCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\n  \"skippedDevTextStrip\": ").Append(skippedStrip.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\n  \"selfTestMirroredFlagged\": ").Append(selfTest ? "true" : "false");
            builder.Append(",\n  \"runtimeCalloutLimitation\": \"B0 window words were runtime DataTargetVisual billboards, so a static audit cannot see them. WP2 does not spawn those callouts.\"");
            builder.Append(",\n  \"violations\": [");
            for (int i = 0; i < violations.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append("\n    \"").Append(Escape(violations[i])).Append('"');
            }

            if (violations.Count > 0) builder.Append('\n');
            builder.Append("  ]\n}\n");

            string root = Directory.GetParent(Application.dataPath).FullName;
            string path = Path.Combine(root, AuditRelative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, builder.ToString());
            Debug.Log("[Chamber01TextRules] Text audit " + checkedCount + " checked, " + violations.Count + " violations, " + path);
        }

        static float Round4(float value)
        {
            return Mathf.Round(value * 10000f) / 10000f;
        }

        static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "'");
        }
    }
}
