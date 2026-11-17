using System;
using System.Collections.Generic;
using UnityEngine;
using Convergence.Gameplay.Ride.Level1;
using Convergence.Gameplay.Ride.Level2;

namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// The chapter picker at the dock: a teacher (or the operator at the desk, keys 1 to 6) chooses where GO starts the
    /// ride: the full ride, or straight to one stop (pixels, neuron, network, backprop, training). It holds the chapters
    /// and the choice and tells the director; the ride then runs on from there to the end. The board's tiles are laser
    /// targets (a <see cref="PickTarget"/> on each collider, added here); shooting one is the command. It only listens
    /// while the ride waits at the dock. A presentation view shows the board and lights the chosen tile.
    /// </summary>
    public sealed class ChapterPicker : MonoBehaviour, IPickOwner
    {
        [Serializable]
        public struct Chapter
        {
            public string name;

            [Tooltip("The director's segment the chapter starts at (0: the whole ride).")]
            public int segment;
        }

        [SerializeField] private RideDirector director;
        [SerializeField] private Chapter[] chapters = new Chapter[0];

        [Tooltip("One collider per chapter, in the same order: the board's tiles.")]
        [SerializeField] private Collider[] tiles = new Collider[0];

        [SerializeField] private int selected;

        [Tooltip("A ride started at or past this segment never ran this forward pass (the network stop), so its guess is " +
                 "computed when the ride starts there (the wrong-guess stop shows it). Optional.")]
        [SerializeField] private ForwardPassMoment primeGuess;
        [SerializeField] private int primeFromSegment = -1;

        public IReadOnlyList<Chapter> Chapters => chapters;
        public int Count => chapters.Length;

        /// <summary>The chosen chapter's index.</summary>
        public int Selected => selected;

        /// <summary>True while the ride waits at the dock and the start can still be chosen.</summary>
        public bool IsOpen => director != null && director.State == RideState.Dock && director.CanChooseStart;

        /// <summary>The chosen chapter changed (its index).</summary>
        public event Action<int> SelectionChanged;

        private void Awake()
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null) continue;
                PickTarget pick = tiles[i].GetComponent<PickTarget>();
                if (pick == null) pick = tiles[i].gameObject.AddComponent<PickTarget>();
                pick.Owner = this;
                pick.Index = i;
            }
        }

        private void OnEnable()
        {
            if (director != null) director.SegmentStarted += OnSegmentStarted;
        }

        private void OnDisable()
        {
            if (director != null) director.SegmentStarted -= OnSegmentStarted;
        }

        private void Start()
        {
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, chapters.Length - 1));
            if (director != null && chapters.Length > 0) director.SetStartSegment(chapters[selected].segment);
        }

        /// <summary>Chooses a chapter. Only at the dock before GO; returns false otherwise.</summary>
        public bool Select(int index)
        {
            if (!IsOpen || index < 0 || index >= chapters.Length) return false;
            if (!director.SetStartSegment(chapters[index].segment)) return false;
            if (index == selected) return true;
            selected = index;
            SelectionChanged?.Invoke(index);
            return true;
        }

        private void OnSegmentStarted(int index)
        {
            // Only the first segment of the ride matters: a chapter start skipped the network stop's forward pass.
            if (index != director.StartSegment || primeGuess == null || primeFromSegment < 0) return;
            if (index >= primeFromSegment && primeGuess.Guess < 0 && !primeGuess.HasFinished) primeGuess.Compute();
        }

        // ---------- laser (IPickOwner) ----------

        public bool PickHit(int index, int hand) => Select(index);

        public bool PickRelease(int index, int hand) => false;

        public bool PickStrokeBegin(int index, int hand, Vector3 point, bool pressed) => false;

        public void PickStrokeMove(int index, int hand, Vector3 point)
        {
        }

        public void PickStrokeEnd(int index, int hand)
        {
        }
    }
}
