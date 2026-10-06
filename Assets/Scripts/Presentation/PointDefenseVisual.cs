using UnityEngine;
using UnityEngine.XR;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Laser, explosion, scan-gate flash, hull-hit spark, and the ROCK / ICE / FIRE lamps.
    /// Reads scan results. It does not decide them.
    /// </summary>
    public class PointDefenseVisual : MonoBehaviour
    {
        [SerializeField] private AsteroidDefenseDirector director;
        [SerializeField] private ChamberController chamber;
        [SerializeField] private Transform muzzle;
        [SerializeField] private LineRenderer beam;
        [SerializeField] private ParticleSystem explosion;
        [SerializeField] private ParticleSystem hullSparks;
        [SerializeField] private Renderer[] scanGate;
        [SerializeField] private Light rockLamp;
        [SerializeField] private Light iceLamp;
        [SerializeField] private Light fireLamp;
        [SerializeField] private Light[] flashLights;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip laserClip;
        [SerializeField] private AudioClip explosionClip;
        [SerializeField] private AudioClip impactClip;
        [SerializeField] private AudioClip shieldClip;
        [SerializeField] private float hapticAmplitude = 0.55f;
        [SerializeField] private float hapticDuration = 0.08f;

        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        float _beamUntil;
        float _gateUntil;
        float _flashUntil;
        Vector3 _beamEnd;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly Color GateColor = new Color(0.4f, 0.9f, 1f);
        TextMesh _rockTag;
        TextMesh _iceTag;
        TextMesh _fireTag;
        Camera _tagCamera;

        void Awake()
        {
            if (director == null) director = FindAnyObjectByType<AsteroidDefenseDirector>();
            if (chamber == null) chamber = FindAnyObjectByType<ChamberController>();
            if (beam != null)
            {
                beam.positionCount = 2;
                beam.enabled = false;
            }
        }

        void Start()
        {
            if (chamber == null) chamber = FindAnyObjectByType<ChamberController>();
            string rock = "In1";
            string ice = "In2";
            string fire = "Out";
            if (chamber != null && chamber.Curriculum != null)
            {
                rock = chamber.Curriculum.InputNames[0];
                ice = chamber.Curriculum.InputNames[1];
                fire = chamber.Curriculum.OutputName;
            }

            _rockTag = MakeTag(rockLamp, rock);
            _iceTag = MakeTag(iceLamp, ice);
            _fireTag = MakeTag(fireLamp, fire);
        }

        void OnEnable()
        {
            if (director != null)
            {
                director.OnScan += HandleScan;
                director.OnOutcome += HandleOutcome;
            }
            if (chamber != null) chamber.OnShieldDamaged += HandleDamaged;
        }

        void OnDisable()
        {
            if (director != null)
            {
                director.OnScan -= HandleScan;
                director.OnOutcome -= HandleOutcome;
            }
            if (chamber != null) chamber.OnShieldDamaged -= HandleDamaged;
        }

        void Update()
        {
            float now = Time.time;
            if (beam != null)
            {
                bool on = now < _beamUntil;
                beam.enabled = on;
                if (on && muzzle != null)
                {
                    beam.SetPosition(0, muzzle.position);
                    beam.SetPosition(1, _beamEnd);
                }
            }

            if (scanGate != null)
            {
                float glow = now < _gateUntil ? 3.2f : 0.35f;
                for (int i = 0; i < scanGate.Length; i++)
                {
                    Renderer renderer = scanGate[i];
                    if (renderer == null) continue;
                    renderer.GetPropertyBlock(_block);
                    _block.SetColor(EmissionId, GateColor * glow);
                    renderer.SetPropertyBlock(_block);
                }
            }

            FaceTag(_rockTag);
            FaceTag(_iceTag);
            FaceTag(_fireTag);

            if (flashLights != null && now < _flashUntil)
            {
                float k = (_flashUntil - now) / 0.18f;
                for (int i = 0; i < flashLights.Length; i++)
                {
                    if (flashLights[i] != null) flashLights[i].intensity = 0.4f + (2.6f * k);
                }
            }
        }

        void HandleScan(DataTargetReceptor receptor, CaseDiagnostic diag, bool fired)
        {
            _gateUntil = Time.time + 0.2f;
            if (diag == null) return;
            double[] inputs = diag.Inputs;
            if (rockLamp != null) rockLamp.intensity = inputs.Length > 0 && inputs[0] >= 0.5 ? 2.4f : 0.15f;
            if (iceLamp != null) iceLamp.intensity = inputs.Length > 1 && inputs[1] >= 0.5 ? 2.4f : 0.15f;
            if (fireLamp != null) fireLamp.intensity = fired ? 3f : 0.12f;
        }

        void HandleOutcome(DataTargetReceptor receptor, DefenseOutcome outcome)
        {
            if (receptor == null) return;
            if (outcome != DefenseOutcome.Vaporized && outcome != DefenseOutcome.FriendlyFire) return;

            _beamEnd = receptor.transform.position;
            _beamUntil = Time.time + 0.16f;
            if (muzzle != null && beam != null)
            {
                beam.enabled = true;
                beam.SetPosition(0, muzzle.position);
                beam.SetPosition(1, _beamEnd);
            }
            if (explosion != null)
            {
                explosion.transform.position = _beamEnd;
                explosion.Play();
            }
            Play(laserClip, 0.8f);
            Play(explosionClip, 0.7f);
        }

        void HandleDamaged(float amount)
        {
            _flashUntil = Time.time + 0.18f;
            if (hullSparks != null) hullSparks.Play();
            Play(impactClip, 0.75f);
            Play(shieldClip, 0.4f);
            PulseHands();
        }

        void Play(AudioClip clip, float volume)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, volume);
        }

        static TextMesh MakeTag(Light lamp, string word)
        {
            if (lamp == null) return null;
            var go = new GameObject(word + "Tag");
            go.transform.SetParent(lamp.transform.parent, false);
            go.transform.position = lamp.transform.position + (Vector3.up * 0.16f);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = word;
            mesh.characterSize = 0.045f;
            mesh.fontSize = 0;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontStyle = FontStyle.Bold;
            mesh.color = new Color(0.90f, 0.91f, 0.88f, 1f);
            return mesh;
        }

        void FaceTag(TextMesh mesh)
        {
            if (mesh == null) return;
            if (_tagCamera == null) _tagCamera = Camera.main;
            if (_tagCamera == null) return;
            Vector3 toHead = _tagCamera.transform.position - mesh.transform.position;
            if (toHead.sqrMagnitude < 0.01f) return;
            // Plan section 4.3: LookRotation(textPos - eye), not LookAt(camera).
            mesh.transform.rotation = Quaternion.LookRotation(-toHead, Vector3.up);
            float distance = toHead.magnitude;
            mesh.characterSize = distance * Mathf.Tan(3f * Mathf.Deg2Rad);
            mesh.color = new Color(0.90f, 0.91f, 0.88f, 1f);
        }

        void PulseHands()
        {
            InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (right.isValid) right.SendHapticImpulse(0u, hapticAmplitude, hapticDuration);
            InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (left.isValid) left.SendHapticImpulse(0u, hapticAmplitude, hapticDuration);
        }
    }
}
