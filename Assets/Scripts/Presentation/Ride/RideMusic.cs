using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// Background music for the ride. Fades in when the pod launches and ducks under the narration.
    /// The clip is assigned by the scene builder from Assets/_Project/Audio/Music/telstar; with no clip the ride is silent.
    /// </summary>
    public sealed class RideMusic : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private RideDirector director;
        [SerializeField] private DashScreenView dash;
        [SerializeField] private float volume = 0.5f;
        [SerializeField] private float duckedVolume = 0.2f;
        [SerializeField] private float fadeSeconds = 3f;

        private bool _started;

        private void OnEnable()
        {
            if (director != null) director.StateChanged += OnState;
        }

        private void OnDisable()
        {
            if (director != null) director.StateChanged -= OnState;
        }

        private void Start()
        {
            if (source == null) return;
            source.loop = true;
            source.volume = 0f;
            source.playOnAwake = false;
        }

        private void Update()
        {
            if (!_started || source == null) return;
            float target = dash != null && dash.IsSpeaking ? duckedVolume : volume;
            source.volume = Mathf.MoveTowards(source.volume, target, Time.deltaTime * volume / Mathf.Max(0.1f, fadeSeconds));
        }

        private void OnState(RideState state)
        {
            if (_started || state != RideState.Traveling || source == null || source.clip == null) return;
            _started = true;
            source.Play();
        }
    }
}
