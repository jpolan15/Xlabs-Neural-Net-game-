using UnityEngine;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Every color and display number the ride views use, in one asset. Orange means "you can touch this"
    /// and nothing else in the ride is orange.
    /// </summary>
    [CreateAssetMenu(menuName = "Convergence/Ride Theme", fileName = "RideTheme")]
    public sealed class RideTheme : ScriptableObject
    {
        [Header("Palette")]
        public Color voidNavy = new Color(0.015f, 0.025f, 0.06f);
        public Color cyan = new Color(0.10f, 0.88f, 1.00f);
        public Color amber = new Color(1.00f, 0.80f, 0.12f);
        public Color orange = new Color(1.00f, 0.40f, 0.04f);
        public Color offWhite = new Color(0.92f, 0.95f, 1.00f);
        public Color locked = new Color(0.36f, 0.40f, 0.48f);

        [Header("Data stream")]
        public float streamSpeed = 1.2f;
        public float spawnX = 3.8f;
        public float scanX = 0f;
        public float despawnX = -3.8f;
        public float pulseTravelSeconds = 1.4f;

        [Header("Pipes")]
        public float pipeHairline = 0.008f;
        public float pipeWidthPerWeight = 0.05f;

        [Header("Core tank")]
        public float tankUnitsPerSum = 0.27f;
        public float tankMaxSum = 3f;
        public float tankFollow = 6f;

        [Header("Loss landscape")]
        public float landscapeSize = 4.4f;
        public float heightPerLoss = 0.12f;
        public float maxHeight = 1.1f;
        public float marbleRadius = 0.09f;
        public float contourEvery = 0.5f;

        [Header("Text")]
        public float instructionSeconds = 3.5f;
    }
}
