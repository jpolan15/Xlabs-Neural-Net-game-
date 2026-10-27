using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The sign over the trained network (v6): what the rider's own training reached, the test accuracy Core measured
    /// on digits the network never trained on, and how many steps and digits it took. Display only.
    /// </summary>
    public sealed class TrainedSignView : MonoBehaviour
    {
        [SerializeField] private Level1Session session;
        [SerializeField] private TextMeshPro text;

        private bool _shown;

        private void OnEnable() => _shown = false;

        private void Update()
        {
            if (_shown || session == null || text == null || !session.IsTrained) return;
            _shown = true;
            int percent = Mathf.RoundToInt((float)(session.TrainedAccuracy * 100.0));
            var invariant = System.Globalization.CultureInfo.InvariantCulture;
            text.text = percent + "% RIGHT ON " + session.Trainer.TestCount.ToString("N0", invariant) + " NEW DIGITS\nYOU TRAINED IT: "
                + session.Trainer.DigitsSeen.ToString("N0", invariant) + " DIGITS";
        }
    }
}
