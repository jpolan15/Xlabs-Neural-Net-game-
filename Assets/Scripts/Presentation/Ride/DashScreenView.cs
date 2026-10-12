using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The pod's screen: who is speaking, the subtitle for what is being said one clause at a time, and a one-line
    /// readout for live numbers (the Valley's step and loss, a nudged wire). Scripted lines play at once and replace
    /// any chatter; reactions queue behind each other and are dropped when the ride moves to the next segment. A line
    /// with no clip shows its subtitle for its reading time, so recordings drop in by filename (see
    /// <see cref="NarrationLibrary"/>).
    /// </summary>
    public sealed class DashScreenView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private NarrationLibrary library;
        [SerializeField] private RideTheme theme;
        [SerializeField] private TMP_Text instruction;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private TMP_Text readout;
        [SerializeField] private AudioSource voice;

        private struct Queued
        {
            public RideLine line;
            public float expireAt;
        }

        private const float QueueLife = 8f;
        private const float LineCooldown = 0.5f;

        private readonly List<Queued> _queue = new List<Queued>();
        private NarrationLibrary.Entry _entry;
        private RideLine _current;
        private bool _hasCurrent;
        private bool _playing;
        private float _startedAt;
        private float _speakSeconds;
        private float _subtitleUntil;
        private int _shownIndex = -1;

        /// <summary>True while a narration clip is playing. The music ducks under it.</summary>
        public bool IsSpeaking => _playing && voice != null && voice.isPlaying;

        /// <summary>True while any line, voiced or silent, is on screen.</summary>
        public bool IsShowingLine => _playing;

        /// <summary>A clause started: the line, its clause index, and the clause with its cues. Views follow this.</summary>
        public event System.Action<RideLine, int, NarrationLibrary.Segment> SegmentShown;

        /// <summary>The screen went quiet: the line finished or was cut off.</summary>
        public event System.Action LineCleared;

        /// <summary>Shows a live number under the subtitle (empty clears it).</summary>
        public void ShowReadout(string text)
        {
            if (readout != null) readout.text = text ?? string.Empty;
        }

        private void OnEnable()
        {
            if (subtitle != null) subtitle.text = string.Empty;
            if (instruction != null) instruction.text = string.Empty;
            if (readout != null) readout.text = string.Empty;

            if (director == null) return;
            director.Said += Enqueue;
            director.Narrated += PlayScripted;
            director.NarrationSkipped += OnSkipped;
            director.SegmentStarted += OnSegment;
        }

        private void OnDisable()
        {
            if (director == null) return;
            director.Said -= Enqueue;
            director.Narrated -= PlayScripted;
            director.NarrationSkipped -= OnSkipped;
            director.SegmentStarted -= OnSegment;
        }

        private void Update()
        {
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                if (Time.time > _queue[i].expireAt) _queue.RemoveAt(i);
            }

            if (_playing) ShowSegment();

            bool busy = IsSpeaking || Time.time < _subtitleUntil + LineCooldown;
            if (!busy && _playing) Clear();

            if (!busy && _queue.Count > 0)
            {
                RideLine next = _queue[0].line;
                _queue.RemoveAt(0);
                Play(next);
            }
        }

        private void Enqueue(RideLine line)
        {
            if (_hasCurrent && _current == line) return;
            for (int i = 0; i < _queue.Count; i++)
            {
                if (_queue[i].line == line) return;
            }

            _queue.Add(new Queued { line = line, expireAt = Time.time + QueueLife });
        }

        private void PlayScripted(RideLine line)
        {
            _queue.Clear();
            InterruptCurrent();
            Play(line);
        }

        private void OnSkipped()
        {
            _queue.Clear();
            InterruptCurrent();
        }

        private void OnSegment(int index)
        {
            _queue.Clear();
            ShowReadout(string.Empty);
        }

        private void InterruptCurrent()
        {
            if (voice != null) voice.Stop();
            Clear();
        }

        private void Clear()
        {
            bool wasShowing = _playing;
            _subtitleUntil = 0f;
            _playing = false;
            _hasCurrent = false;
            _shownIndex = -1;
            if (subtitle != null) subtitle.text = string.Empty;
            if (instruction != null) instruction.text = string.Empty;
            if (wasShowing) LineCleared?.Invoke();
        }

        private void Play(RideLine line)
        {
            if (library == null || !library.TryGet(line, out NarrationLibrary.Entry entry)) return;

            _entry = entry;
            _current = line;
            _hasCurrent = true;
            _playing = true;
            _startedAt = Time.time;
            _speakSeconds = Mathf.Max(0.5f, entry.SpeakSeconds);
            _subtitleUntil = Time.time + _speakSeconds;
            _shownIndex = -1;

            if (instruction != null && theme != null)
            {
                instruction.text = "<color=#" + ColorUtility.ToHtmlStringRGB(theme.cyan) + ">" + entry.tag + "</color>";
            }

            if (entry.clip != null && entry.clip.length > 0.2f && voice != null)
            {
                voice.Stop();
                voice.clip = entry.clip;
                voice.Play();
            }

            ShowSegment();
        }

        private void ShowSegment()
        {
            float fraction = Mathf.Clamp01((Time.time - _startedAt) / _speakSeconds);
            int index = NarrationLibrary.SegmentIndexAt(_entry, fraction);
            if (index == _shownIndex || index < 0) return;
            _shownIndex = index;
            NarrationLibrary.Segment segment = _entry.segments[index];
            if (subtitle != null) subtitle.text = segment.text;
            SegmentShown?.Invoke(_current, index, segment);
        }
    }
}
