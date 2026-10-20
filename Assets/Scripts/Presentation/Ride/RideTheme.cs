using System;
using UnityEngine;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Every colour and look number of the ride, in one asset (Assets/ScriptableObjects/RideTheme.asset), written by
    /// the builder. Orange means "you can grab or press this" and nothing else may be orange.
    /// </summary>
    [CreateAssetMenu(menuName = "Convergence/Ride Theme", fileName = "RideTheme")]
    public sealed class RideTheme : ScriptableObject
    {
        /// <summary>How one region looks and is named: its own sky, fog and tag, so a rider knows they crossed into it.</summary>
        [Serializable]
        public struct RegionLook
        {
            public string tag;
            public Color background;
            public Color fog;
            public float fogDensity;
        }

        [Header("Shared")]
        public Color voidNavy = new Color(0.015f, 0.025f, 0.06f);
        public Color cyan = new Color(0.10f, 0.88f, 1.00f);
        public Color amber = new Color(1.00f, 0.71f, 0.28f);
        public Color orange = new Color(1.00f, 0.40f, 0.04f);
        public Color offWhite = new Color(0.92f, 0.95f, 1.00f);
        public Color locked = new Color(0.36f, 0.40f, 0.48f);
        public Color violet = new Color(0.55f, 0.40f, 0.95f);

        [Header("Level 1")]
        public Color paper = new Color(0.984f, 0.965f, 0.925f);
        public Color ink = new Color(0.106f, 0.106f, 0.184f);
        public Color pillar = new Color(0.96f, 0.97f, 1.00f);
        public Color networkBlue = new Color(0.31f, 0.76f, 1.00f);
        public Color negativeWeight = new Color(0.70f, 0.53f, 1.00f);
        public Color wrongRed = new Color(1.00f, 0.23f, 0.23f);
        public Color rewindMagenta = new Color(1.00f, 0.17f, 0.84f);
        public Color valleyPink = new Color(1.00f, 0.44f, 0.57f);
        public Color valleyViolet = new Color(0.42f, 0.18f, 0.55f);
        public Color gold = new Color(1.00f, 0.82f, 0.40f);
        public Color right = new Color(0.40f, 1.00f, 0.62f);

        [Header("Explainers (3Blue1Brown-inspired; orange stays for grabbable things)")]
        [Tooltip("The sky while an explainer explains: near-black navy, no colour cast, faint distant grid.")]
        public RegionLook blackboard = new RegionLook { tag = "", background = new Color(0.043f, 0.055f, 0.078f), fog = new Color(0.043f, 0.055f, 0.078f), fogDensity = 0.002f };
        public Color explainBlue = new Color(0.345f, 0.769f, 0.867f);
        public Color explainYellow = new Color(0.957f, 0.827f, 0.271f);
        public Color explainGreen = new Color(0.514f, 0.757f, 0.404f);
        public Color explainRed = new Color(0.988f, 0.384f, 0.333f);
        public Color explainWhite = new Color(0.925f, 0.925f, 0.925f);
        public Color explainGrey = new Color(0.533f, 0.533f, 0.533f);

        [Tooltip("Regions 0 (The Page) to 7 (Right).")]
        public RegionLook[] regions = new RegionLook[0];

        [Header("Network")]
        [Range(0f, 1f)] public float networkDim = 0.3f;
        public float pulseSpacing = 5f;
        public float pulseSpeed = 7f;

        /// <summary>Region n's look, or the first one for an index out of range.</summary>
        public RegionLook Region(int index)
        {
            if (regions == null || regions.Length == 0) return new RegionLook { tag = "", background = voidNavy, fog = voidNavy, fogDensity = 0.02f };
            return regions[Mathf.Clamp(index, 0, regions.Length - 1)];
        }
    }
}
