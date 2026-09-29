using TMPro;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Short objective that sits by the left controller, or beside the camera when no controller is present.
    /// </summary>
    public class WristObjective : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private ChamberOnboardingController onboarding;
        [SerializeField] private Vector3 cameraLocalOffset = new Vector3(-0.18f, -0.12f, 0.45f);

        private Transform _anchor;
        private float _nextSearch;

        private void Awake()
        {
            if (onboarding == null) onboarding = FindAnyObjectByType<ChamberOnboardingController>();
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextSearch)
            {
                _nextSearch = Time.unscaledTime + 1f;
                FindAnchor();
            }

            if (_anchor != null)
            {
                transform.SetPositionAndRotation(_anchor.position, _anchor.rotation);
            }

            if (label != null && onboarding != null)
            {
                label.text = onboarding.GetCurrentStepPrompt();
            }
        }

        private void FindAnchor()
        {
            if (_anchor != null) return;
            var all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                string name = all[i].name;
                if (name.Contains("Left") && name.Contains("Controller"))
                {
                    _anchor = all[i];
                    return;
                }
            }

            Camera cam = Camera.main;
            if (cam == null) return;
            var anchor = new GameObject("WristAnchor");
            anchor.transform.SetParent(cam.transform, false);
            anchor.transform.localPosition = cameraLocalOffset;
            anchor.transform.localRotation = Quaternion.identity;
            _anchor = anchor.transform;
        }
    }
}
