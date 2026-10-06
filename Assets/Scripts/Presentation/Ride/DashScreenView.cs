using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The pod's one screen: a single instruction line, and the subtitle for whatever the ship AI is saying.
    /// Lines play one at a time from the dash speaker. A line with no clip shows its subtitle only, so recordings
    /// can be dropped in by filename later without touching the scene logic.
    /// </summary>
    public sealed class DashScreenView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private TMP_Text instruction;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private AudioSource voice;
        [SerializeField] private AudioClip[] clips = new AudioClip[15];
        [SerializeField] private float silentSubtitleSeconds = 4f;

        private static readonly string[] Subtitles =
        {
            "Neural, launching the probe. Pull the orange lever.",
            "Rocks are slipping through. Lower the trigger line.",
            "Rocks are slipping through. Lower the trigger line.",
            "Whoa, you zapped the drone! Trigger's too low.",
            "That's it. Weight times input, compared to a threshold. That's all a neuron is: math.",
            "Two sensors now. The ice sensor is wired backwards. It is draining the core.",
            "Turn each pipe up or down. That number is called a weight.",
            "Congratulations! Every case is right. You just built an OR gate out of multiplication and addition.",
            "Hand-tuning is slow. Real AI tunes itself. Pull LEARN.",
            "This hill is the error. Lower is better. Every step, the weights roll downhill.",
            "That's gradient descent: measure the slope, step down it, repeat.",
            "Learning rate too high. Watch it overshoot.",
            "Converged. The machine found its own weights.",
            "",
            "Course locked. Taking us home for now."
        };

        private struct Queued
        {
            public RideLine line;
            public float expireAt;
        }

        private const float QueueLife = 8f;
        private readonly List<Queued> _queue = new List<Queued>();
        private RideLine _current;
        private bool _hasCurrent;
        private float _subtitleUntil;
        private bool _playing;

        /// <summary>True while a narration clip is playing. The music ducks under it.</summary>
        public bool IsSpeaking => _playing && voice != null && voice.isPlaying;

        private void OnEnable()
        {
            if (director == null) return;
            director.Said += Enqueue;
            director.PhaseChanged += OnPhase;
            director.StateChanged += OnState;
        }

        private void OnDisable()
        {
            if (director == null) return;
            director.Said -= Enqueue;
            director.PhaseChanged -= OnPhase;
            director.StateChanged -= OnState;
        }

        private void Start()
        {
            if (subtitle != null) subtitle.text = string.Empty;
            if (instruction != null) instruction.text = string.Empty;
        }

        private void Update()
        {
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                if (Time.time > _queue[i].expireAt) _queue.RemoveAt(i);
            }

            const float lineCooldown = 0.5f;
            bool busy = IsSpeaking || Time.time < _subtitleUntil + lineCooldown;
            if (!busy && _playing)
            {
                _playing = false;
                _hasCurrent = false;
                if (subtitle != null) subtitle.text = string.Empty;
            }

            if (!busy && _queue.Count > 0)
            {
                RideLine next = _queue[0].line;
                _queue.RemoveAt(0);
                Play(next);
            }
        }

        /// <summary>Which part of the ride a line belongs to: 0 dock, 1 to 3 the stops, -1 for the travel and finish lines.</summary>
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

        private void InterruptCurrent()
        {
            if (voice != null) voice.Stop();
            _subtitleUntil = 0f;
            _playing = false;
            _hasCurrent = false;
            if (subtitle != null) subtitle.text = string.Empty;
        }

        private void Play(RideLine line)
        {
            int index = (int)line - 1;
            if (index < 0 || index >= Subtitles.Length) return;

            AudioClip clip = index < clips.Length ? clips[index] : null;
            if (string.IsNullOrEmpty(Subtitles[index]) && (clip == null || clip.length <= 0.2f)) return;

            _current = line;
            _hasCurrent = true;
            if (subtitle != null) subtitle.text = Subtitles[index];
            _playing = true;
            if (clip != null && voice != null)
            {
                voice.Stop();
                voice.clip = clip;
                voice.Play();
                _subtitleUntil = Time.time + clip.length;
            }
            else
            {
                _subtitleUntil = Time.time + silentSubtitleSeconds;
            }
        }

        // Instructions are shown as arrows on the levers (HintArrowView), not as sentences.
        // When the ride moves to a new section, anything left over from the old one is dropped or cut off.
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

        private void OnPhase(StationKind kind, StationPhase phase)
        {
        }
    }
}
