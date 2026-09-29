using System.Text;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// World-space bridge placard for Level 1.
    /// Shows the lost-in-space alert, the Earth-image upload step, live weights,
    /// and the heading-home beat after the existing evaluator actually passes.
    /// It does not decide puzzle correctness.
    /// </summary>
    public class LostInSpaceMissionBoard : MonoBehaviour
    {
        [SerializeField] private Vector3 boardPosition = new Vector3(-1.15f, 1.48f, 0.2f);
        [SerializeField] private Color alertColor = new Color(1f, 0.28f, 0.22f, 1f);
        [SerializeField] private Color homeColor = new Color(0.25f, 1f, 0.55f, 1f);
        [SerializeField] private Color panelColor = new Color(0.02f, 0.05f, 0.09f, 0.92f);

        private ChamberOnboardingController _onboarding;
        private ChamberController _chamber;
        private NeuralState _neural;
        private TextMesh _text;
        private Renderer _panelRenderer;
        private Material _panelMaterial;
        private Renderer[] _strobes;
        private Material[] _strobeMaterials;
        private readonly StringBuilder _builder = new StringBuilder(512);
        private bool _dirty = true;
        private bool _solved;
        private double _shownW1 = double.NaN;
        private double _shownW2 = double.NaN;
        private Camera _camera;

        private void Awake()
        {
            if (transform.position.sqrMagnitude < 0.01f)
                transform.position = boardPosition;

            _onboarding = FindAnyObjectByType<ChamberOnboardingController>();
            _chamber = FindAnyObjectByType<ChamberController>();
            _neural = FindAnyObjectByType<NeuralState>();
            _camera = Camera.main;
            BuildVisuals();
            CacheStrobes();
        }

        private void OnEnable()
        {
            if (_onboarding != null)
                _onboarding.OnStepChanged += HandleStepChanged;
            if (_chamber != null)
                _chamber.OnPuzzleSolved += HandleSolved;
            if (_neural != null)
                _neural.OnStateMutated += HandleStateMutated;
        }

        private void OnDisable()
        {
            if (_onboarding != null)
                _onboarding.OnStepChanged -= HandleStepChanged;
            if (_chamber != null)
                _chamber.OnPuzzleSolved -= HandleSolved;
            if (_neural != null)
                _neural.OnStateMutated -= HandleStateMutated;
        }

        private void HandleStepChanged(OnboardingStep _)
        {
            _dirty = true;
        }

        private void HandleSolved()
        {
            _solved = true;
            _dirty = true;
        }

        private void HandleStateMutated()
        {
            _dirty = true;
        }

        private void LateUpdate()
        {
            FacePlayerYaw();
            if (_chamber != null && _chamber.HasSolved)
                _solved = true;

            bool weightsChanged = false;
            if (_neural != null)
            {
                weightsChanged = System.Math.Abs(_neural.Weight1 - _shownW1) > 0.01d
                    || System.Math.Abs(_neural.Weight2 - _shownW2) > 0.01d;
            }

            if (_dirty || weightsChanged)
                RefreshText();

            PulseStrobes();
        }

        private void FacePlayerYaw()
        {
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return;

            Vector3 toPlayer = _camera.transform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.001f)
                return;
            transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        }

        private void BuildVisuals()
        {
            var panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panel.name = "MissionBoardPanel";
            panel.transform.SetParent(transform, false);
            panel.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            panel.transform.localRotation = Quaternion.identity;
            panel.transform.localScale = new Vector3(1.55f, 0.78f, 1f);
            var panelCollider = panel.GetComponent<Collider>();
            if (panelCollider != null)
                Destroy(panelCollider);

            _panelRenderer = panel.GetComponent<Renderer>();
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            _panelMaterial = new Material(shader);
            SetColor(_panelMaterial, panelColor);
            _panelRenderer.material = _panelMaterial;

            var textGo = new GameObject("MissionBoardText");
            textGo.transform.SetParent(transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            textGo.transform.localRotation = Quaternion.identity;
            _text = textGo.AddComponent<TextMesh>();
            _text.fontSize = 32;
            _text.characterSize = 0.018f;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = alertColor;
            _text.fontStyle = FontStyle.Bold;
        }

        private void CacheStrobes()
        {
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].name.StartsWith("AlertStrobe"))
                    count++;
            }

            _strobes = new Renderer[count];
            _strobeMaterials = new Material[count];
            int write = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].name.StartsWith("AlertStrobe"))
                    continue;
                _strobes[write] = renderers[i];
                _strobeMaterials[write] = renderers[i].material;
                write++;
            }
        }

        private void RefreshText()
        {
            _dirty = false;
            if (_text == null)
                return;

            double w1 = _neural != null ? _neural.Weight1 : 0d;
            double w2 = _neural != null ? _neural.Weight2 : 0d;
            _shownW1 = w1;
            _shownW2 = w2;

            string step = _onboarding != null
                ? _onboarding.GetCurrentStepPrompt()
                : "Upload Earth images, tune the weights, and lock a course home.";

            _builder.Length = 0;
            if (_solved)
            {
                _builder.AppendLine("EARTH LOCKED");
                _builder.AppendLine("Navigation restored. Heading home.");
                _builder.AppendLine();
                _builder.Append(step);
                _text.color = homeColor;
                SetColor(_panelMaterial, new Color(0.02f, 0.08f, 0.05f, 0.92f));
            }
            else
            {
                _builder.AppendLine("LOST IN SPACE");
                _builder.AppendLine("USS CONVERGENCE — navigation dead");
                _builder.AppendLine();
                _builder.AppendLine("1  Upload Earth images (plug both cables)");
                _builder.AppendLine("2  Tune weights until Earth locks");
                _builder.AppendLine("3  Pull the lever to head home");
                _builder.AppendLine();
                _builder.AppendLine(step);
                _builder.Append("Land ").Append(w1.ToString("+0.0;-0.0;0.0"));
                _builder.Append("    Air ").Append(w2.ToString("+0.0;-0.0;0.0"));
                _text.color = alertColor;
                SetColor(_panelMaterial, panelColor);
            }

            _text.text = _builder.ToString();
        }

        private void PulseStrobes()
        {
            if (_strobeMaterials == null)
                return;

            Color color = _solved
                ? homeColor
                : Color.Lerp(new Color(0.35f, 0.05f, 0.04f, 1f), alertColor, 0.35f + (0.65f * Mathf.PingPong(Time.time * 1.6f, 1f)));

            for (int i = 0; i < _strobeMaterials.Length; i++)
                SetColor(_strobeMaterials[i], color);
        }

        private static void SetColor(Material material, Color color)
        {
            if (material == null)
                return;
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.4f);
            }
        }
    }
}
