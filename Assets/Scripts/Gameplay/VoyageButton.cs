using UnityEngine;

namespace Convergence.Gameplay
{
    /// <summary>
    /// A physical control for a voyage action. It does not grade the puzzle itself.
    /// </summary>
    public class VoyageButton : MonoBehaviour
    {
        public enum Kind
        {
            InstallHiddenLayer,
            CycleNeuron,
            TrainEarth,
            MaskNoise
        }

        [SerializeField] private Kind kind;
        [SerializeField] private VoyageDirector voyage;
        [SerializeField] private double learningRate = 0.2;
        [SerializeField] private int epochs = 40;

        private void Awake()
        {
            if (voyage == null) voyage = FindAnyObjectByType<VoyageDirector>();
        }

        /// <summary>Called by the XRI bridge on select.</summary>
        public void Activate()
        {
            if (voyage == null) return;
            switch (kind)
            {
                case Kind.InstallHiddenLayer:
                    voyage.InstallHiddenLayer();
                    break;
                case Kind.CycleNeuron:
                    voyage.CycleEditableNeuron();
                    break;
                case Kind.TrainEarth:
                    voyage.TrainAndGrade(learningRate, epochs);
                    break;
                case Kind.MaskNoise:
                    voyage.SetNoiseMasked(!voyage.NoiseMasked);
                    break;
            }
        }
    }
}
