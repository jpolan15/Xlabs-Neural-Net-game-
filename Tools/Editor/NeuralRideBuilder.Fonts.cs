using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Convergence.EditorTools
{
    /// <summary>
    /// The explainers' typeface: Computer Modern Unicode (CMU Serif, SIL OFL 1.1, Assets/_Project/Fonts/ComputerModern),
    /// the 3Blue1Brown look. Made once into small static SDF font assets (Quest 2: one 512 atlas each) with only the
    /// characters the ride writes: basic Latin, digits and the math signs CMU has (+ − × … ·); subscripts are written
    /// with TMP's &lt;sub&gt; (w&lt;sub&gt;1&lt;/sub&gt;a&lt;sub&gt;1&lt;/sub&gt;), and ≈ or ₁ fall back to LiberationSans.
    /// The italic is the regular face's italic, so &lt;i&gt; in a label sets math variables in italic as in TeX.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private const string CmuDir = "Assets/_Project/Fonts/ComputerModern";
        private const string CmuSerifPath = CmuDir + "/CMU Serif SDF.asset";
        private const string CmuItalicPath = CmuDir + "/CMU Serif Italic SDF.asset";

        private static TMP_FontAsset _serif;

        private static string FontCharacters()
        {
            var chars = new System.Text.StringBuilder();
            for (char c = (char)32; c < (char)127; c++) chars.Append(c);
            for (char c = '₀'; c <= '₉'; c++) chars.Append(c); // subscript digits
            chars.Append("−×…≈·’‘“”–—²³→←↑↓Σσ");
            return chars.ToString();
        }

        /// <summary>The CMU Serif SDF asset (regular, with its italic), made from the .otf files if missing.</summary>
        private static TMP_FontAsset ExplainerFont()
        {
            if (_serif != null) return _serif;
            TMP_FontAsset italic = MakeFontAsset(CmuDir + "/cmunti.otf", CmuItalicPath);
            _serif = MakeFontAsset(CmuDir + "/cmunrm.otf", CmuSerifPath);
            if (italic != null && _serif != null)
            {
                // The weight table is not settable as a whole; set the regular weight's italic through the serialized asset.
                var so = new SerializedObject(_serif);
                SerializedProperty table = so.FindProperty("m_FontWeightTable");
                if (table != null)
                {
                    if (table.arraySize < 10) table.arraySize = 10;
                    SerializedProperty slot = table.GetArrayElementAtIndex(4).FindPropertyRelative("italicTypeface");
                    if (slot != null && slot.objectReferenceValue != italic)
                    {
                        slot.objectReferenceValue = italic;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(_serif);
                        AssetDatabase.SaveAssets();
                    }
                }
            }

            // CMU has no subscript digits or approx sign: those come from LiberationSans (write w<sub>1</sub> for a subscript).
            foreach (TMP_FontAsset f in new[] { _serif, italic })
            {
                if (f == null || _font == null) continue;
                if (f.fallbackFontAssetTable == null) f.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                if (f.fallbackFontAssetTable.Contains(_font)) continue;
                f.fallbackFontAssetTable.Add(_font);
                EditorUtility.SetDirty(f);
                AssetDatabase.SaveAssets();
            }

            return _serif ?? _font;
        }

        private static TMP_FontAsset MakeFontAsset(string fontPath, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                Debug.LogWarning("[NeuralRideBuilder] " + fontPath + " is missing; explainer text falls back to LiberationSans.");
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 56, 6, GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic, false);
            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            if (!asset.TryAddCharacters(FontCharacters(), out string missing) && !string.IsNullOrEmpty(missing))
            {
                Debug.Log("[NeuralRideBuilder] " + asset.name + " has no glyph for: " + missing);
            }

            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(asset, assetPath);
            Texture2D atlas = asset.atlasTexture;
            atlas.name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, asset);
            Material material = asset.material;
            material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        }
    }
}
