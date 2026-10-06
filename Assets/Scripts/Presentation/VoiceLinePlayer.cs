using System;
using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// One voice at a time. Interrupt lines clear the queue. Hit lines are dropped when the channel is busy.
    /// </summary>
    public class VoiceLinePlayer : MonoBehaviour
    {
        public const string Wave1 = "vo_wave1_incoming";
        public const string FirstHit = "vo_first_hit";
        public const string Cables = "vo_cables_in";
        public const string FirstKill = "vo_first_kill";
        public const string ShotDrone = "vo_shot_drone";
        public const string HitA = "vo_hit_a";
        public const string HitB = "vo_hit_b";
        public const string HitC = "vo_hit_c";
        public const string HullHalf = "vo_hull_half";
        public const string Reroute = "vo_reroute";
        public const string Stuck = "vo_stuck";
        public const string Solved = "vo_solved";
        public const string Swarm = "vo_swarm";
        public const string Lesson = "vo_lesson";
        public const string Restored = "vo_restored";

        [Serializable]
        public class Slot
        {
            public string id;
            public AudioClip clip;
            public string text;
            public float start;
            public float end;
        }

        struct Queued
        {
            public int slot;
            public float expireAt;
        }

        [SerializeField] private Slot[] slots = Array.Empty<Slot>();
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private TextMesh banner;
        [SerializeField] private OpeningTeleprompter teleprompter;
        [SerializeField] private AsteroidDefenseDirector director;
        [SerializeField] private ChamberController chamber;
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private float hitCooldown = 6f;
        [SerializeField] private float queueExpiry = 12f;
        [SerializeField] private int queueCap = 4;
        [SerializeField] private float stuckAfter = 45f;
        [SerializeField] private float stuckRepeat = 60f;

        readonly Queued[] _queue = new Queued[8];
        int _count;
        int _playing = -1;
        float _clearAt;
        float _hitReadyAt;
        float _quietFor;
        float _stuckReadyAt;
        bool _firstHitPlayed;
        bool _cablesPlayed;
        bool _firstKillPlayed;
        bool _hullHalfPlayed;
        bool _hadPositive;
        int _hitCursor;

        static readonly string[] HitIds = { HitA, HitB, HitC };
        static readonly string[] InterruptIds = { Solved, Reroute, Restored };

        public bool IsBusy => _playing >= 0 || (teleprompter != null && teleprompter.IsPlaying);

        void Awake()
        {
            if (teleprompter == null) teleprompter = FindAnyObjectByType<OpeningTeleprompter>();
            if (director == null) director = FindAnyObjectByType<AsteroidDefenseDirector>();
            if (chamber == null) chamber = FindAnyObjectByType<ChamberController>();
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (voiceSource == null) voiceSource = GetComponent<AudioSource>();
        }

        void OnEnable()
        {
            if (director != null)
            {
                director.OnWaveStarted += HandleWave;
                director.OnOutcome += HandleOutcome;
                director.OnVictoryStarted += HandleVictory;
                director.OnVictoryComplete += HandleVictoryComplete;
                director.OnRestored += HandleRestored;
            }
            if (chamber != null)
            {
                chamber.OnShieldDamaged += HandleDamaged;
                chamber.OnShieldUpdated += HandleShield;
                chamber.OnHullRerouted += HandleReroute;
                chamber.OnPuzzleSolved += HandleSolved;
            }
            if (neuralState != null) neuralState.OnStateMutated += HandleNeural;
        }

        void OnDisable()
        {
            if (director != null)
            {
                director.OnWaveStarted -= HandleWave;
                director.OnOutcome -= HandleOutcome;
                director.OnVictoryStarted -= HandleVictory;
                director.OnVictoryComplete -= HandleVictoryComplete;
                director.OnRestored -= HandleRestored;
            }
            if (chamber != null)
            {
                chamber.OnShieldDamaged -= HandleDamaged;
                chamber.OnShieldUpdated -= HandleShield;
                chamber.OnHullRerouted -= HandleReroute;
                chamber.OnPuzzleSolved -= HandleSolved;
            }
            if (neuralState != null) neuralState.OnStateMutated -= HandleNeural;
        }

        void Update()
        {
            float now = Time.time;
            if (_playing >= 0 && now >= _clearAt)
            {
                _playing = -1;
                if (banner != null) banner.text = string.Empty;
                PlayNext(now);
            }

            bool solved = director != null && director.Solved;
            if (!solved && !_hadPositive)
            {
                _quietFor += Time.deltaTime;
                if (_quietFor >= stuckAfter && now >= _stuckReadyAt && !IsBusy)
                {
                    _stuckReadyAt = now + stuckRepeat;
                    _quietFor = 0f;
                    Request(Stuck, false, now);
                }
            }
        }

        void HandleWave(int index, bool victory)
        {
            if (index == 0 && !victory) Request(Wave1, false, Time.time);
        }

        void HandleDamaged(float amount)
        {
            if (!_firstHitPlayed)
            {
                _firstHitPlayed = true;
                _hitReadyAt = Time.time + hitCooldown;
                Request(FirstHit, false, Time.time);
                return;
            }
            if (Time.time < _hitReadyAt) return;
            if (IsBusy) return;
            _hitReadyAt = Time.time + hitCooldown;
            string id = HitIds[_hitCursor % HitIds.Length];
            _hitCursor++;
            Request(id, false, Time.time);
        }

        void HandleOutcome(DataTargetReceptor receptor, DefenseOutcome outcome)
        {
            if (outcome == DefenseOutcome.Vaporized || outcome == DefenseOutcome.Docked)
            {
                _hadPositive = true;
                _quietFor = 0f;
            }
            if (outcome == DefenseOutcome.Vaporized && !_firstKillPlayed)
            {
                _firstKillPlayed = true;
                Request(FirstKill, false, Time.time);
            }
            else if (outcome == DefenseOutcome.FriendlyFire)
            {
                Request(ShotDrone, false, Time.time);
            }
        }

        void HandleShield(float current, float max)
        {
            if (_hullHalfPlayed || max <= 0f) return;
            if (current / max < 0.5f)
            {
                _hullHalfPlayed = true;
                Request(HullHalf, false, Time.time);
            }
        }

        void HandleNeural()
        {
            if (_cablesPlayed || neuralState == null) return;
            if (neuralState.Cable1Connected && neuralState.Cable2Connected)
            {
                _cablesPlayed = true;
                Request(Cables, false, Time.time);
            }
        }

        void HandleReroute() => Request(Reroute, true, Time.time);
        void HandleSolved() => Request(Solved, true, Time.time);
        void HandleVictory() => Request(Swarm, false, Time.time);
        void HandleVictoryComplete() => Request(Lesson, false, Time.time);
        void HandleRestored() => Request(Restored, true, Time.time);

        void Request(string id, bool interrupt, float now)
        {
            int slot = FindSlot(id);
            if (slot < 0) return;

            if (interrupt || IsInterrupt(id))
            {
                _count = 0;
                _playing = -1;
                if (voiceSource != null) voiceSource.Stop();
                BeginSlot(slot, now);
                return;
            }

            if (IsHit(id) && (IsBusy || now < _hitReadyAt)) return;

            if (IsBusy || _count > 0)
            {
                if (_count >= queueCap) return;
                _queue[_count].slot = slot;
                _queue[_count].expireAt = now + queueExpiry;
                _count++;
                return;
            }

            BeginSlot(slot, now);
        }

        void PlayNext(float now)
        {
            while (_count > 0)
            {
                Queued next = _queue[0];
                for (int i = 1; i < _count; i++) _queue[i - 1] = _queue[i];
                _count--;
                if (now > next.expireAt) continue;
                BeginSlot(next.slot, now);
                return;
            }
        }

        void BeginSlot(int slot, float now)
        {
            Slot data = slots[slot];
            _playing = slot;
            float span = data.end - data.start;
            if (span < 0.4f) span = 2f;
            _clearAt = now + span;
            if (banner != null) banner.text = data.text ?? string.Empty;
            if (voiceSource != null && data.clip != null)
            {
                voiceSource.spatialBlend = 1f;
                voiceSource.loop = false;
                voiceSource.PlayOneShot(data.clip, 1f);
            }
        }

        int FindSlot(string id)
        {
            if (slots == null || id == null) return -1;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].id == id) return i;
            }
            return -1;
        }

        static bool IsInterrupt(string id)
        {
            for (int i = 0; i < InterruptIds.Length; i++)
            {
                if (InterruptIds[i] == id) return true;
            }
            return false;
        }

        static bool IsHit(string id)
        {
            for (int i = 0; i < HitIds.Length; i++)
            {
                if (HitIds[i] == id) return true;
            }
            return false;
        }
    }
}
