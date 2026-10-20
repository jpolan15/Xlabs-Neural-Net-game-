using System;
using System.Collections.Generic;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>
    /// One beat's explainer (E0 to E8, TASK_LEVEL1_V4_EXPLAINERS): Explain, then Unfold, then the ride's Play. A
    /// subclass declares its steps in <see cref="Declare"/>, each keyed to a narration cue (the {cue} tags of its
    /// lines, heard through <see cref="DashScreenView.SegmentShown"/>, the same clock as the subtitles: no second
    /// clock), to a clause of a line, or to the ride arriving at its stop. A step returns the tweens it plays.
    ///
    /// The stage: while the beat explains, its region's pieces listed in <c>hidden</c> are off and the sky is the
    /// blackboard (<see cref="RegionAtmosphere"/>). The unfold step flies the named explainer pieces into the region's
    /// real objects (<c>unfolds</c>: piece X becomes region object Y, or Y grows in) with a smooth cascade, then gives
    /// the region back its own sky. The builder writes the lines, the segments, the hidden pieces and the unfolds.
    ///
    /// Skip (Space, a long GO hold: the director's NarrationSkipped) and leaving the beat's segments jump to the end
    /// state: every remaining step runs and completes at once, the unfold included. A restart (R) reloads the scene,
    /// and <see cref="ResetToStart"/> puts everything back. Display only: it never decides anything about the ride.
    /// </summary>
    public abstract class Explainer : MonoBehaviour
    {
        public enum UnfoldStyle
        {
            /// <summary>The piece flies into the target's pose and scale, then the target takes its place.</summary>
            Morph,

            /// <summary>The target grows from nothing (out of the piece, if there is one).</summary>
            Grow,

            /// <summary>The target rises out of its own floor (its y scale grows), like pillars extruding.</summary>
            GrowUp,

            /// <summary>The target just appears (after the delay); use when a step already showed the change.</summary>
            Show
        }

        [Serializable]
        public struct Unfold
        {
            [Tooltip("The explainer's own piece (optional for Grow, GrowUp and Show).")]
            public Transform piece;

            [Tooltip("The region's real object it becomes.")]
            public Transform target;

            public UnfoldStyle style;

            [Tooltip("Seconds after the unfold starts.")]
            public float delay;

            public float duration;
        }

        [SerializeField] protected RideDirector director;
        [SerializeField] protected DashScreenView dash;
        [SerializeField] protected RegionAtmosphere atmosphere;
        [SerializeField] protected RideTheme theme;

        [Tooltip("The lines whose cues drive this explainer.")]
        [SerializeField] protected RideLine[] lines = new RideLine[0];

        [Tooltip("The segment that explains (-1: the intro and the dock).")]
        [SerializeField] protected int explainSegment = -1;

        [Tooltip("By the end of this segment the explainer has unfolded (it jumps to its end if not).")]
        [SerializeField] protected int lastSegment = -1;

        [Tooltip("The sky is the blackboard while this explains.")]
        [SerializeField] protected bool blackboard = true;

        [Tooltip("The explainer's own pieces (shown while it explains; saved off in the scene).")]
        [SerializeField] protected GameObject content;

        [Tooltip("Keep the content after the unfold (it belongs to the place afterwards).")]
        [SerializeField] protected bool keepContent;

        [Tooltip("Region pieces kept off until the unfold.")]
        [SerializeField] protected GameObject[] hidden = new GameObject[0];

        [SerializeField] protected Unfold[] unfolds = new Unfold[0];

        /// <summary>The key of a step that runs when the pod stops at the explain segment's end.</summary>
        public const string Arrive = "@arrive";

        /// <summary>The key of a step that runs when the explain segment starts (or the intro, for E0).</summary>
        public const string Begin = "@begin";

        internal sealed class Step
        {
            public string Key;
            public Func<IEnumerable<Tween>> Action;
            public bool IsUnfold;
        }

        /// <summary>What a subclass uses to declare its steps, in the order they play.</summary>
        protected sealed class Steps
        {
            private readonly List<Step> _list;

            internal Steps(List<Step> list) => _list = list;

            /// <summary>Runs when a clause carrying this {cue} starts (or for Arrive / Begin, at that moment).</summary>
            public void At(string cue, Func<IEnumerable<Tween>> action) => _list.Add(new Step { Key = cue, Action = action });

            /// <summary>Runs when this clause (0-based, split at the [pX] marks) of a line starts: for clauses without a cue.</summary>
            public void AtClause(RideLine line, int clause, Func<IEnumerable<Tween>> action) => _list.Add(new Step { Key = ClauseKey(line, clause), Action = action });

            /// <summary>The unfold, on this cue: the declared unfolds plus the subclass's own extra tweens.</summary>
            public void Unfold(string cue, Func<IEnumerable<Tween>> extra = null) => _list.Add(new Step { Key = cue, Action = extra, IsUnfold = true });
        }

        private readonly List<Step> _steps = new List<Step>();
        private readonly Timeline _timeline = new Timeline();
        private int _next;
        private bool _built;
        private bool _holdingSky;
        private Pose[] _homePose = new Pose[0];
        private Vector3[] _homeScale = new Vector3[0];
        private Transform[] _homed = new Transform[0];

        /// <summary>The explainer is on screen (its segment started).</summary>
        public bool IsShowing { get; private set; }

        /// <summary>The region is back: the unfold played (or there is none and every step ran).</summary>
        public bool HasUnfolded { get; private set; }

        /// <summary>Every step ran and every tween finished: the end state.</summary>
        public bool IsFinished { get; private set; }

        /// <summary>How many steps have run.</summary>
        public int StepsRun => _next;
        public int StepCount => _steps.Count;
        public int ExplainSegment => explainSegment;
        public int LastSegment => lastSegment;
        public GameObject Content => content;

        /// <summary>A cue this explainer reacted to (for Pixel and sounds: WP2).</summary>
        public event Action<string> StepRan;

        /// <summary>The step key for a clause of a line.</summary>
        public static string ClauseKey(RideLine line, int clause) => "@" + line + "/" + clause;

        /// <summary>Declare the steps (called once).</summary>
        protected abstract void Declare(Steps steps);

        /// <summary>Put the explainer's own pieces in their start state (called at load and on every reset).</summary>
        protected virtual void ResetPieces()
        {
        }

        /// <summary>Per-frame idle motion (drift, bob), after the timeline has stepped.</summary>
        protected virtual void Animate(float deltaTime)
        {
        }

        protected virtual void Awake()
        {
            Build();
            ResetToStart();
        }

        private void Build()
        {
            if (_built) return;
            _built = true;
            Declare(new Steps(_steps));

            // Remember every unfold target's home pose, so a reset (and the unfold) knows where the region object lives.
            var homed = new List<Transform>();
            foreach (Unfold u in unfolds)
            {
                if (u.target != null && !homed.Contains(u.target)) homed.Add(u.target);
                if (u.piece != null && !homed.Contains(u.piece)) homed.Add(u.piece);
            }

            _homed = homed.ToArray();
            _homePose = new Pose[_homed.Length];
            _homeScale = new Vector3[_homed.Length];
            for (int i = 0; i < _homed.Length; i++)
            {
                _homePose[i] = new Pose(_homed[i].localPosition, _homed[i].localRotation);
                _homeScale[i] = _homed[i].localScale;
            }
        }

        private int HomeIndex(Transform t) => Array.IndexOf(_homed, t);

        /// <summary>The region object's home local scale (as the builder made it).</summary>
        protected Vector3 HomeScale(Transform t)
        {
            int i = HomeIndex(t);
            return i < 0 ? t.localScale : _homeScale[i];
        }

        /// <summary>Back to the start: pieces reset, region pieces hidden, sky released, nothing played.</summary>
        public void ResetToStart()
        {
            Build();
            _timeline.Clear();
            _next = 0;
            IsShowing = false;
            HasUnfolded = false;
            IsFinished = false;
            for (int i = 0; i < _homed.Length; i++)
            {
                _homed[i].localPosition = _homePose[i].position;
                _homed[i].localRotation = _homePose[i].rotation;
                _homed[i].localScale = _homeScale[i];
            }

            foreach (Unfold u in unfolds)
            {
                if (u.piece != null) u.piece.gameObject.SetActive(true);
                if (u.target != null) u.target.gameObject.SetActive(true);
            }

            foreach (GameObject h in hidden)
            {
                if (h != null) h.SetActive(false);
            }

            if (content != null) content.SetActive(false);
            ResetPieces();
            ReleaseSky();
        }

        protected virtual void OnEnable()
        {
            if (dash != null) dash.SegmentShown += HearClause;
            if (director != null)
            {
                director.SegmentStarted += OnSegmentStarted;
                director.SegmentArrived += OnSegmentArrived;
                director.SegmentEnded += OnSegmentEnded;
                director.NarrationSkipped += OnSkipped;
            }
        }

        protected virtual void OnDisable()
        {
            if (dash != null) dash.SegmentShown -= HearClause;
            if (director != null)
            {
                director.SegmentStarted -= OnSegmentStarted;
                director.SegmentArrived -= OnSegmentArrived;
                director.SegmentEnded -= OnSegmentEnded;
                director.NarrationSkipped -= OnSkipped;
            }
        }

        protected virtual void Start()
        {
            // E0 explains from the first frame (the intro).
            if (explainSegment < 0 && director != null && (director.State == RideState.Intro || director.State == RideState.Dock)) Show();
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>Advances the explainer's tweens by this much game time (Update calls it; tests call it directly).</summary>
        public void Tick(float deltaTime)
        {
            _timeline.Update(deltaTime);
            if (IsShowing) Animate(deltaTime);
            if (IsShowing && !IsFinished && _next >= _steps.Count && _timeline.IsIdle) Finish();
        }

        /// <summary>True while the director is in this explainer's segments (the intro and dock for -1).</summary>
        public bool IsCurrent
        {
            get
            {
                if (director == null) return false;
                int s = director.SegmentIndex;
                if (explainSegment < 0 && s < 0) return true;
                return s >= Mathf.Max(0, explainSegment) && s <= lastSegment;
            }
        }

        // ---------- what drives it ----------

        private void OnSegmentStarted(int index)
        {
            if (index == explainSegment) Show();
            if (index > lastSegment && !IsFinished && (IsShowing || explainSegment <= index)) JumpToEnd();
            Fire(Begin, index == explainSegment);
        }

        private void OnSegmentArrived(int index) => Fire(Arrive, index == explainSegment);

        private void OnSegmentEnded(int index)
        {
            if (index == lastSegment && !IsFinished) JumpToEnd();
        }

        private void OnSkipped()
        {
            if (IsShowing && !IsFinished && IsCurrent) JumpToEnd();
        }

        /// <summary>A clause of a line started (DashScreenView.SegmentShown): runs the step its cue or clause names.</summary>
        public void HearClause(RideLine line, int clause, NarrationLibrary.Segment segment)
        {
            if (Array.IndexOf(lines, line) < 0 || IsFinished) return;
            Show();
            int target = -1;
            for (int i = _next; i < _steps.Count && target < 0; i++)
            {
                string key = _steps[i].Key;
                if (key == ClauseKey(line, clause) || (!key.StartsWith("@", StringComparison.Ordinal) && NarrationLibrary.HasCue(segment, key))) target = i;
            }

            if (target >= 0) RunUpTo(target);
        }

        private void Fire(string key, bool mine)
        {
            if (!mine || IsFinished) return;
            for (int i = _next; i < _steps.Count; i++)
            {
                if (_steps[i].Key != key) continue;
                RunUpTo(i);
                return;
            }
        }

        /// <summary>Runs the steps before this one at once (their cues were missed), then plays this one.</summary>
        private void RunUpTo(int index)
        {
            while (_next < index) RunStep(_steps[_next++], true);
            RunStep(_steps[_next++], false);
        }

        private void RunStep(Step step, bool instant)
        {
            if (instant) _timeline.CompleteAll();
            var tweens = new List<Tween>();
            if (step.Action != null)
            {
                IEnumerable<Tween> made = step.Action();
                if (made != null) tweens.AddRange(made);
            }

            if (step.IsUnfold) tweens.AddRange(UnfoldTweens());
            _timeline.Play(tweens);
            if (instant) _timeline.CompleteAll();
            StepRan?.Invoke(step.Key);
        }

        // ---------- the stage ----------

        /// <summary>Puts the explainer on screen and the sky on the blackboard.</summary>
        public void Show()
        {
            if (IsShowing || IsFinished) return;
            IsShowing = true;
            if (content != null) content.SetActive(true);
            if (blackboard && atmosphere != null && !_holdingSky)
            {
                _holdingSky = true;
                atmosphere.HoldBlackboard(this, director == null || director.SegmentIndex < 0 && Time.timeSinceLevelLoad < 0.5f);
            }
        }

        private void ReleaseSky()
        {
            if (!_holdingSky) return;
            _holdingSky = false;
            if (atmosphere != null) atmosphere.ReleaseBlackboard(this);
        }

        /// <summary>The declared unfolds as tweens: region objects take their real places with a cascade, then the sky comes back.</summary>
        private List<Tween> UnfoldTweens()
        {
            var list = new List<Tween>();
            float last = 0f;
            foreach (Unfold u in unfolds)
            {
                if (u.target == null) continue;
                Transform target = u.target;
                Vector3 scale = HomeScale(target);
                float duration = u.duration > 0f ? u.duration : 1.2f;
                last = Mathf.Max(last, u.delay + duration);
                switch (u.style)
                {
                    case UnfoldStyle.Morph:
                        if (u.piece == null)
                        {
                            list.Add(Tween.Call(u.delay, () => target.gameObject.SetActive(true)));
                            break;
                        }

                        Transform piece = u.piece;
                        list.Add(Anim.MoveTo(piece, () => (target.position, target.rotation, Anim.LocalScaleFor(piece.parent, WorldScale(target, scale))), duration)
                            .After(u.delay).Then(() =>
                            {
                                target.gameObject.SetActive(true);
                                piece.gameObject.SetActive(false);
                            }));
                        break;
                    case UnfoldStyle.Grow:
                        Transform from = u.piece;
                        list.Add(Anim.GrowFromCenter(target, scale, from != null ? from.position : (Vector3?)null, duration).After(u.delay));
                        if (from != null) list.Add(Tween.Call(u.delay, () => from.gameObject.SetActive(false)));
                        break;
                    case UnfoldStyle.GrowUp:
                        list.Add(Anim.GrowUp(target, scale, duration).After(u.delay));
                        break;
                    default:
                        list.Add(Tween.Call(u.delay, () => target.gameObject.SetActive(true)));
                        break;
                }
            }

            // First: the region's top pieces come on with every unfold target still waiting (off, or at nothing).
            list.Insert(0, Tween.Call(0f, () =>
            {
                foreach (Unfold u in unfolds)
                {
                    if (u.target == null) continue;
                    if (u.style == UnfoldStyle.Morph || u.style == UnfoldStyle.Show) u.target.gameObject.SetActive(false);
                    else u.target.localScale = HomeScale(u.target) * 0.001f;
                }

                foreach (GameObject h in hidden)
                {
                    if (h != null) h.SetActive(true);
                }
            }));

            list.Add(Tween.Call(last, () =>
            {
                HasUnfolded = true;
                ReleaseSky();
            }));
            return list;
        }

        private static Vector3 WorldScale(Transform target, Vector3 homeLocal)
        {
            Vector3 p = target.parent != null ? target.parent.lossyScale : Vector3.one;
            return Vector3.Scale(p, homeLocal);
        }

        // ---------- the end ----------

        /// <summary>Jumps to the end state now: every remaining step runs and completes, the unfold included.</summary>
        public void JumpToEnd()
        {
            if (IsFinished) return;
            if (!IsShowing)
            {
                IsShowing = true;
                if (content != null) content.SetActive(true);
            }

            _timeline.CompleteAll();
            while (_next < _steps.Count) RunStep(_steps[_next++], true);
            _timeline.CompleteAll();
            Finish();
        }

        private void Finish()
        {
            IsFinished = true;
            if (!HasUnfolded)
            {
                // No unfold step (or it was never reached): the region still comes back whole.
                foreach (GameObject h in hidden)
                {
                    if (h != null) h.SetActive(true);
                }

                for (int i = 0; i < _homed.Length; i++)
                {
                    if (_homed[i] == null) continue;
                    _homed[i].localScale = _homeScale[i];
                }

                HasUnfolded = true;
            }

            ReleaseSky();
            if (content != null && !keepContent) content.SetActive(false);
        }
    }
}
