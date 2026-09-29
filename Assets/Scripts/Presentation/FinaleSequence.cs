using System.Text;
using TMPro;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Plays the departure only after attention predicts JUMP_HOME.
    /// The ship burns out of the asteroid field and settles into orbit beside Earth.
    /// </summary>
    public class FinaleSequence : MonoBehaviour
    {
        [SerializeField] private VoyageDirector voyage;
        [SerializeField] private Transform earth;
        [SerializeField] private Transform ship;
        [SerializeField] private Transform starfield;
        [SerializeField] private Transform asteroidField;
        [SerializeField] private Transform gasGiant;
        [SerializeField] private Light[] engines;
        [SerializeField] private TMP_Text credits;
        [SerializeField] private TextMesh creditsMesh;
        [SerializeField] private float flightSeconds = 16f;

        [SerializeField] private Vector3 earthEndPosition = new Vector3(2.4f, 2.2f, 15.5f);
        [SerializeField] private Vector3 earthEndScale = new Vector3(5.4f, 5.4f, 5.4f);
        [SerializeField] private Vector3 shipBurnPosition = new Vector3(0f, 1.85f, 13.2f);
        [SerializeField] private Vector3 shipParkPosition = new Vector3(-2.5f, 1.65f, 11.4f);
        [SerializeField] private Vector3 shipParkEuler = new Vector3(8f, 28f, -6f);

        private bool _playing;
        private bool _revealed;
        private bool _creditsShown;
        private float _time;

        private Vector3 _earthStartPosition;
        private Vector3 _earthStartScale;
        private Vector3 _shipStartPosition;
        private Quaternion _shipStartRotation;
        private Quaternion _shipParkRotation;
        private Vector3 _starStart;
        private Vector3 _asteroidStart;
        private Vector3 _giantStart;
        private float[] _engineBase;

        private void Awake()
        {
            if (voyage == null) voyage = FindAnyObjectByType<VoyageDirector>();
            CacheStarts();
            if (ship != null) ship.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (voyage == null) return;
            voyage.OnLegChanged += HandleLeg;
            voyage.OnJumpHome += Begin;
        }

        private void OnDisable()
        {
            if (voyage == null) return;
            voyage.OnLegChanged -= HandleLeg;
            voyage.OnJumpHome -= Begin;
        }

        private void HandleLeg(VoyageDirector.Leg leg)
        {
            if (leg == VoyageDirector.Leg.SpectrumXor) RevealShip();
        }

        private void Update()
        {
            if (_revealed && !_playing) PulseEngines(0.7f + 0.25f * Mathf.Sin(Time.time * 2.4f));
            if (!_playing) return;

            _time += Time.deltaTime;
            float duration = flightSeconds < 0.1f ? 0.1f : flightSeconds;
            float t = Mathf.Clamp01(_time / duration);
            float burn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.7f));
            float park = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.58f) / 0.42f));

            if (ship != null)
            {
                Vector3 surged = Vector3.Lerp(_shipStartPosition, shipBurnPosition, burn);
                ship.position = Vector3.Lerp(surged, shipParkPosition, park);
                ship.rotation = Quaternion.Slerp(_shipStartRotation, _shipParkRotation, park);
            }

            if (earth != null)
            {
                earth.position = Vector3.Lerp(_earthStartPosition, earthEndPosition, burn);
                earth.localScale = Vector3.Lerp(_earthStartScale, earthEndScale, burn);
            }

            if (starfield != null)
            {
                starfield.localPosition = _starStart + new Vector3(0f, 0f, -22f * burn);
            }

            if (asteroidField != null)
            {
                asteroidField.position = _asteroidStart + new Vector3(0f, -3.5f, 16f) * burn;
            }

            if (gasGiant != null)
            {
                gasGiant.position = _giantStart + new Vector3(18f, 3f, 10f) * burn;
            }

            PulseEngines(Mathf.Lerp(1.2f, 7f, burn) * Mathf.Lerp(1f, 0.4f, park));

            if (t >= 1f && !_creditsShown)
            {
                _creditsShown = true;
                string card = BuildCredits();
                if (credits != null) credits.text = card;
                if (creditsMesh != null) creditsMesh.text = card;
            }
        }

        private void RevealShip()
        {
            CacheStarts();
            if (ship != null) ship.gameObject.SetActive(true);
            _revealed = true;
        }

        private void Begin()
        {
            RevealShip();
            _playing = true;
            _time = 0f;
            _creditsShown = false;
        }

        private void PulseEngines(float flare)
        {
            if (engines == null || _engineBase == null) return;
            for (int i = 0; i < engines.Length; i++)
            {
                if (engines[i] == null) continue;
                engines[i].intensity = _engineBase[i] * flare;
            }
        }

        private void CacheStarts()
        {
            if (earth != null)
            {
                _earthStartPosition = earth.position;
                _earthStartScale = earth.localScale;
            }

            if (ship != null)
            {
                _shipStartPosition = ship.position;
                _shipStartRotation = ship.rotation;
                _shipParkRotation = Quaternion.Euler(shipParkEuler);
            }

            if (starfield != null) _starStart = starfield.localPosition;
            if (asteroidField != null) _asteroidStart = asteroidField.position;
            if (gasGiant != null) _giantStart = gasGiant.position;

            if (engines == null) return;
            if (_engineBase != null && _engineBase.Length == engines.Length) return;
            _engineBase = new float[engines.Length];
            for (int i = 0; i < engines.Length; i++)
            {
                _engineBase[i] = engines[i] != null ? Mathf.Max(0.2f, engines[i].intensity) : 1f;
            }
        }

        private string BuildCredits()
        {
            var builder = new StringBuilder(256);
            builder.AppendLine("CONVERGENCE");
            builder.AppendLine("Orbit. Earth is holding off the bow.");
            builder.AppendLine();
            if (voyage != null)
            {
                var journal = voyage.Journal;
                for (int i = 0; i < journal.Count; i++)
                {
                    builder.Append("- ").AppendLine(journal[i]);
                }
            }

            return builder.ToString();
        }
    }
}
