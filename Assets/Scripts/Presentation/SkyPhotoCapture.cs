using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Photographs a sky body by rendering it and counting pixels.
    /// Blue ratio, white ratio, and brightness are the only features passed to training.
    /// </summary>
    public class SkyPhotoCapture : MonoBehaviour
    {
        [SerializeField] private VoyageDirector voyage;
        [SerializeField] private int resolution = 16;
        private PhotoRequest[] _requests;

        private void Awake()
        {
            if (voyage == null) voyage = FindAnyObjectByType<VoyageDirector>();
            _requests = FindObjectsByType<PhotoRequest>(FindObjectsSortMode.None);
        }

        private void OnEnable()
        {
            if (_requests == null) return;
            for (int i = 0; i < _requests.Length; i++)
            {
                if (_requests[i] != null) _requests[i].OnPhotoRequested += CaptureAndFile;
            }
        }

        private void OnDisable()
        {
            if (_requests == null) return;
            for (int i = 0; i < _requests.Length; i++)
            {
                if (_requests[i] != null) _requests[i].OnPhotoRequested -= CaptureAndFile;
            }
        }

        /// <summary>
        /// Raycasts from the camera, renders the hit body, and files the measured features.
        /// </summary>
        public void CaptureAndFile(bool labeledEarth)
        {
            Camera cam = Camera.main;
            if (cam == null || voyage == null) return;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, 400f)) return;

            Renderer renderer = hit.collider.GetComponentInParent<Renderer>();
            if (renderer == null) return;

            Measure(renderer, out double blue, out double white, out double brightness);
            voyage.FilePhoto(blue, white, brightness, labeledEarth);
        }

        private void Measure(Renderer renderer, out double blue, out double white, out double brightness)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            var rt = RenderTexture.GetTemporary(resolution, resolution, 16);
            var photoCamGo = new GameObject("PhotoCamera");
            var photoCam = photoCamGo.AddComponent<Camera>();
            photoCam.enabled = false;
            photoCam.clearFlags = CameraClearFlags.SolidColor;
            photoCam.backgroundColor = Color.black;
            photoCam.cullingMask = 1 << renderer.gameObject.layer;

            Bounds bounds = renderer.bounds;
            photoCam.transform.position = bounds.center + Vector3.back * (bounds.extents.magnitude + 0.5f);
            photoCam.transform.LookAt(bounds.center);
            photoCam.nearClipPlane = 0.01f;
            photoCam.farClipPlane = 50f;
            photoCam.targetTexture = rt;
            int previous = renderer.gameObject.layer;
            SetLayer(renderer.transform, 31);
            photoCam.cullingMask = 1 << 31;
            photoCam.Render();
            SetLayer(renderer.transform, previous);

            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            photoCam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            Destroy(photoCamGo);

            Color32[] pixels = texture.GetPixels32();
            Destroy(texture);
            int blueCount = 0;
            int whiteCount = 0;
            double luma = 0.0;
            for (int i = 0; i < pixels.Length; i++)
            {
                float r = pixels[i].r / 255f;
                float g = pixels[i].g / 255f;
                float b = pixels[i].b / 255f;
                luma += 0.2126 * r + 0.7152 * g + 0.0722 * b;
                if (b > r + 0.05f && b > g) blueCount++;
                if (r > 0.75f && g > 0.75f && b > 0.75f) whiteCount++;
            }

            double count = pixels.Length;
            blue = blueCount / count;
            white = whiteCount / count;
            brightness = luma / count;
        }

        private static void SetLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++) SetLayer(root.GetChild(i), layer);
        }
    }
}
