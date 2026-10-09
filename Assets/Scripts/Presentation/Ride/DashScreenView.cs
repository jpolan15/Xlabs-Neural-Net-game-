using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The pod's one screen: who is speaking, and the subtitle for what they are saying, one clause at a time.
    /// Scripted lines (intro, briefings, outro) play at once and replace any chatter; station hints and reactions
    /// queue behind each other. A line with no clip shows its subtitle for its reading time, so recordings can be
    /// dropped in by filename later without touching the scene logic (see <see cref="NarrationLibrary"/>).
    /// </summary>
    public sealed class DashScreenView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private NarrationLibrary library;
        [SerializeField] private RideTheme theme;
        [SerializeField] private TMP_Text instruction;
        [SerializeField] private TMP_Text subtitle;
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

        /// <summary>
        /// A clause started: the line, its clause index (0 = the line just began), and the clause with its cues. The
        /// explainer panel, the network and the hint arrows follow this, so the rider sees what is being said.
        /// </summary>
        public event System.Action<RideLine, int, NarrationLibrary.Segment> SegmentShown;

        /// <summary>The screen went quiet: the line finished or was cut off.</summary>
        public event System.Action LineCleared;

        /// <summary>How long the clause on screen now is spoken, so a view can time a reaction to its words (0 when none).</summary>
        public float CurrentSegmentSeconds => _hasCurrent && _shownIndex >= 0 ? NarrationLibrary.SegmentSeconds(_entry, _shownIndex) : 0f;

        private void OnEnable()
        {
            // Clear here, not in Start: the director starts the intro in its own Start, and a Start-time clear here would
            // wipe the first line (and its speaker tag) the moment it appeared.
            if (subtitle != null) subtitle.text = string.Empty;
            if (instruction != null) instruction.text = string.Empty;

            if (director == null) return;
            director.Said += Enqueue;
            director.Narrated += PlayScripted;
            director.NarrationSkipped += OnSkipped;
            director.StateChanged += OnState;
        }

        private void OnDisable()
        {
            if (director == null) return;
            director.Said -= Enqueue;
            director.Narrated -= PlayScripted;
            director.NarrationSkipped -= OnSkipped;
            director.StateChanged -= OnState;
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

        /// <summary>Which part of the ride a hint line belongs to: 0 dock, 1 to 3 the stops, -1 for lines that are not tied to one.</summary>
        private static int SectionOf(RideLine line)
        {
            switch (line)
            {
                case RideLine.Launch: return 0;
                case RideLine.NeuronIntro:
                case RideLine.LowerTrigger:
                case RideLine.DroneOops:
                case RideLine.WeightTimesInput: return 1;
                case RideLine.TwoSensors:
                case RideLine.TurnPipes:
                case RideLine.OrBuilt: return 2;
                case RideLine.PullLearn:
                case RideLine.HillIsError:
                case RideLine.GradientDescent:
                case RideLine.LrTooHigh:
                case RideLine.Converged: return 3;
                default: return -1;
            }
        }

        /// <summary>The line that closes a stop. It cuts off any hint or oops chatter still waiting.</summary>
        private static bool IsClosingLine(RideLine line)
        {
            return line == RideLine.WeightTimesInput || line == RideLine.OrBuilt || line == RideLine.GradientDescent;
        }

        private int CurrentSection
        {
            get
            {
                if (director == null) return -1;
                if (director.State == RideState.Dock) return 0;
                return director.State == RideState.AtStop ? director.StopIndex + 1 : -1;
            }
        }

        private void Enqueue(RideLine line)
        {
            int section = SectionOf(line);
            if (section >= 0 && section != CurrentSection) return;
            if (_hasCurrent && _current == line) return;

            if (IsClosingLine(line))
            {
                _queue.Clear();
                if (_hasCurrent) InterruptCurrent();
            }

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

            if (instruction != null)
            {
                instruction.text = "<color=#" + ColorUtility.ToHtmlStringRGB(TagColor(entry.speaker)) + ">" + entry.tag + "</color>";
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

        private Color TagColor(NarrationLibrary.Speaker speaker)
        {
            if (theme == null) return Color.white;
            switch (speaker)
            {
                case NarrationLibrary.Speaker.Aura: return theme.aura;
                case NarrationLibrary.Speaker.AuraClean: return theme.offWhite;
                case NarrationLibrary.Speaker.Archival: return theme.amber;
                default: return theme.cyan;
            }
        }

        // Instructions are shown as arrows on the levers (HintArrowView), not as sentences.
        // When the ride moves to a new section, hint chatter left over from the old one is dropped or cut off.
        private void OnState(RideState state)
        {
            int section = CurrentSection;
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                int queued = SectionOf(_queue[i].line);
                if (queued >= 0 && queued != section) _queue.RemoveAt(i);
            }

            if (_hasCurrent)
            {
                int current = SectionOf(_current);
                if (current >= 0 && current != section) InterruptCurrent();
            }
        }
    }
}
