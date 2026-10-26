using TMPro;
using UnityEngine;

namespace Convergence.Presentation.Ride
{
    /// <summary>Copies the dash's subtitle onto the game table while the table is up (it sits in front of the dash screen). Display only.</summary>
    public sealed class SubtitleMirror : MonoBehaviour
    {
        [SerializeField] private TextMeshPro source;
        [SerializeField] private TextMeshPro target;

        private void LateUpdate()
        {
            if (source == null || target == null || !target.gameObject.activeInHierarchy) return;
            if (target.text != source.text) target.text = source.text;
            target.color = source.color;
        }
    }
}
