using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Starts the ride behind a black fade, so the rider never watches the scene stall. From the first frame the eyes are
    /// covered and the sound is off while the ride waits (the director holds its start). Behind the black it pays the
    /// costs that would otherwise land mid-ride: every hidden board, explainer and region is switched on and drawn
    /// from a tiny probe camera for a couple of frames (shaders and pipeline states compile, meshes and font glyphs
    /// reach the GPU), then switched back; every voice and effect clip is loaded; then memory is collected and
    /// the frame time is given a moment to settle. Only then does the black fade out and the ride begin. A long boot
    /// gives up after <see cref="maxBootSeconds"/> and starts anyway, so the rider is never stuck in the dark. Display
    /// only; it decides nothing about the ride.
    /// </summary>
    public sealed class RideBoot : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private NarrationLibrary library;
        [SerializeField] private RideSfxView sfx;
        [Tooltip("An unlit black material (transparent, drawn last), for the cover in front of the eyes.")]
        [SerializeField] private Material coverMaterial;
        [Tooltip("Everything that is hidden at the start and drawn later: regions, the game table, the stamp, the end card.")]
        [SerializeField] private GameObject[] warmRoots = new GameObject[0];
        [Tooltip("How many frames each hidden hierarchy is drawn for.")]
        [SerializeField] private int warmFrames = 2;
        [Tooltip("Pause after the memory collection, so frame time settles before the fade starts.")]
        [SerializeField] private float settleSeconds = 0.25f;
        [SerializeField] private float fadeSeconds = 0.5f;
        [Tooltip("Longest the clips get to load before the ride goes on without them.")]
        [SerializeField] private float audioSeconds = 8f;
        [Tooltip("The whole boot gives up after this long and starts the ride anyway.")]
        [SerializeField] private float maxBootSeconds = 25f;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _block;
        private Renderer _cover;
        private float _volume = 1f;
        private bool _muted;

        /// <summary>True from the first frame until the black has faded out and the ride has been released.</summary>
        public bool IsBooting { get; private set; }

        /// <summary>How long the boot took (real seconds), for the log and the tests; 0 until it finishes.</summary>
        public float BootSeconds { get; private set; }

        private void Awake()
        {
            IsBooting = true;
            _block = new MaterialPropertyBlock();
            // The director's Start runs after every Awake, so it finds the hold in place.
            if (director != null) director.HoldStart();
            _volume = AudioListener.volume;
            AudioListener.volume = 0f;
            _muted = true;
            EnsureCover();
        }

        private void Start() => StartCoroutine(Boot());

        private void OnDestroy()
        {
            if (_muted) AudioListener.volume = _volume;
            if (_cover != null) Destroy(_cover.gameObject);
        }

        private IEnumerator Boot()
        {
            float started = Time.realtimeSinceStartup;
            float deadline = started + maxBootSeconds;
            try
            {
                // 1. Black first: a frame with the eyes covered before anything is switched on.
                while (!EnsureCover() && Time.realtimeSinceStartup < started + 2f) yield return null;
                SetCover(1f);
                yield return null;

                // 2. Shaders, pipeline states, meshes, glyphs: draw everything that is hidden.
                yield return WarmRenderers(deadline);

                // 3. Voice and effect clips decoded and ready.
                yield return WarmAudio(Mathf.Min(deadline, Time.realtimeSinceStartup + audioSeconds));

                // 4. Let go of the garbage this made, and let the frame time settle at the headset's refresh rate.
                GC.Collect();
                yield return new WaitForSecondsRealtime(settleSeconds);

                // 5. Unblank: sound on, the black fades out, and only then does the ride begin.
                RestoreSound();
                for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
                {
                    SetCover(1f - Mathf.SmoothStep(0f, 1f, t / fadeSeconds));
                    yield return null;
                }
            }
            finally
            {
                // However the boot ends, the sound comes back, the black goes, and the ride starts.
                RestoreSound();
                if (_cover != null) Destroy(_cover.gameObject);
                _cover = null;
                IsBooting = false;
                BootSeconds = Time.realtimeSinceStartup - started;
                if (director != null) director.ReleaseStart();
                Debug.Log("[RideBoot] Ready after " + BootSeconds.ToString("0.00") + " s.");
            }
        }

        private void RestoreSound()
        {
            if (!_muted) return;
            AudioListener.volume = _volume;
            _muted = false;
        }

        // ---------- the black ----------

        private bool EnsureCover()
        {
            if (_cover != null) return true;
            Camera head = Camera.main;
            if (head == null || coverMaterial == null) return false;
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "BootCover";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(head.transform, false);
            quad.transform.localPosition = new Vector3(0f, 0f, 0.12f); // past the near plane, in front of everything else
            quad.transform.localScale = new Vector3(2f, 2f, 1f);
            _cover = quad.GetComponent<Renderer>();
            _cover.sharedMaterial = coverMaterial;
            _cover.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _cover.receiveShadows = false;
            SetCover(1f);
            return true;
        }

        private void SetCover(float alpha)
        {
            if (_cover == null) return;
            _cover.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, new Color(0f, 0f, 0f, Mathf.Clamp01(alpha)));
            _cover.SetPropertyBlock(_block);
            _cover.enabled = alpha > 0.001f;
        }

        // ---------- renderers ----------

        private IEnumerator WarmRenderers(float deadline)
        {
            // Switch on every hidden object under the warm roots (parents first, so a child's own switch counts), remembering
            // what was off so it goes back exactly as it was.
            var wasOff = new List<GameObject>();
            var roots = new List<GameObject>();
            foreach (GameObject root in warmRoots)
            {
                if (root == null) continue;
                roots.Add(root);
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.gameObject.activeSelf) continue;
                    wasOff.Add(t.gameObject);
                    t.gameObject.SetActive(true);
                }
            }

            Camera probe = null;
            RenderTexture target = null;
            try
            {
                probe = MakeProbe(out target);
                for (int frame = 0; frame < Mathf.Max(1, warmFrames) && Time.realtimeSinceStartup < deadline; frame++)
                {
                    foreach (GameObject root in roots) Draw(probe, root);
                    yield return null;
                }
            }
            finally
            {
                // Back to how it was: everything that was off goes off again, deepest first.
                for (int i = wasOff.Count - 1; i >= 0; i--) if (wasOff[i] != null) wasOff[i].SetActive(false);
                if (probe != null) Destroy(probe.gameObject);
                if (target != null)
                {
                    target.Release();
                    Destroy(target);
                }
            }
        }

        /// <summary>A small camera that renders into a texture shaped like the headset's eye, so the pipeline states match.</summary>
        private static Camera MakeProbe(out RenderTexture target)
        {
            Camera main = Camera.main;
            var probe = new GameObject("BootProbe").AddComponent<Camera>();
            probe.enabled = false; // rendered by hand
            probe.stereoTargetEye = StereoTargetEyeMask.None;
            probe.clearFlags = CameraClearFlags.SolidColor;
            probe.backgroundColor = Color.black;
            probe.allowHDR = false;
            probe.allowMSAA = main != null && main.allowMSAA;
            probe.fieldOfView = 60f;
            RenderTextureDescriptor desc = XRSettings.isDeviceActive
                ? XRSettings.eyeTextureDesc
                : new RenderTextureDescriptor(128, 128, RenderTextureFormat.Default, 24);
            desc.width = 128;
            desc.height = 128;
            desc.vrUsage = VRTextureUsage.None;
            desc.volumeDepth = 1;
            desc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            desc.msaaSamples = probe.allowMSAA ? Mathf.Max(1, QualitySettings.antiAliasing) : 1;
            if (desc.depthBufferBits == 0) desc.depthBufferBits = 24;
            target = new RenderTexture(desc);
            probe.targetTexture = target;
            return probe;
        }

        /// <summary>Draws a hierarchy once from outside, framed so all of it is in view.</summary>
        private static void Draw(Camera probe, GameObject root)
        {
            Bounds bounds = default;
            bool any = false;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(false))
            {
                if (!any) bounds = r.bounds;
                else bounds.Encapsulate(r.bounds);
                any = true;
            }

            if (!any) return;
            float radius = Mathf.Max(0.1f, bounds.extents.magnitude);
            float distance = radius / Mathf.Sin(probe.fieldOfView * 0.5f * Mathf.Deg2Rad);
            probe.transform.position = bounds.center - Vector3.forward * distance;
            probe.transform.rotation = Quaternion.identity;
            probe.nearClipPlane = Mathf.Max(0.05f, distance - radius);
            probe.farClipPlane = distance + radius + 1f;
            probe.Render();
        }

        // ---------- audio ----------

        private IEnumerator WarmAudio(float deadline)
        {
            var clips = new List<AudioClip>();
            if (library != null)
            {
                foreach (NarrationLibrary.Entry entry in library.entries) if (entry.clip != null) clips.Add(entry.clip);
            }

            if (sfx != null) sfx.CollectClips(clips);

            var seen = new HashSet<AudioClip>();
            var loading = new List<AudioClip>();
            foreach (AudioClip clip in clips)
            {
                // A streaming clip reads as it plays; the others decode from memory, and are loaded now instead of at their first play.
                if (clip == null || !seen.Add(clip) || clip.loadType == AudioClipLoadType.Streaming) continue;
                if (clip.loadState == AudioDataLoadState.Loaded) continue;
                if (clip.LoadAudioData()) loading.Add(clip);
            }

            foreach (AudioClip clip in loading)
            {
                while (clip != null && clip.loadState == AudioDataLoadState.Loading && Time.realtimeSinceStartup < deadline) yield return null;
            }
        }
    }
}
