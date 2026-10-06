using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Convergence.Core.Puzzles;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Runs the curriculum reference checks and writes JSON for V-08.
    /// </summary>
    public static class Chamber01CurriculumAudit
    {
        public static void Write()
        {
            string error = CurriculumCatalog.RunReferenceChecks();
            bool pass = string.IsNullOrEmpty(error);
            var builder = new StringBuilder();
            builder.Append("{\n  \"passed\": ").Append(pass ? "true" : "false");
            builder.Append(",\n  \"error\": \"").Append(error ?? "").Append("\"\n}\n");
            string root = Directory.GetParent(Application.dataPath).FullName;
            string path = Path.Combine(root, "Documentation", "Design", "captures", "puzzle_audit.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, builder.ToString());
            Debug.Log("[Chamber01CurriculumAudit] " + (pass ? "passed" : error));
        }
    }
}
