using System;
using System.Collections;
using UnityEngine;

namespace Convergence.Gameplay.Ride
{
    /// <summary>
    /// A moment that is a mini-game (Level 1 v5): it has a title, a goal, progress toward it and a score, and ends with
    /// a win beat: "STOP CLEAR!", one to three stars, the win line, then GO lights up and the rider pulls it (or presses
    /// A) to move on. The rider is never stuck: an operator hurry, or <see cref="giveUpAfter"/> seconds, plays the game
    /// out honestly from Core's own hints and still ends with the win beat (one star).
    /// </summary>
    public abstract class RideGame : RideMoment
    {
        [SerializeField] private string title = "GAME";
        [SerializeField] private string goal = "";
        [Tooltip("Seconds before the game starts helping itself (Core's hints, one move at a time).")]
        [SerializeField] protected float giveUpAfter = 120f;
        [Tooltip("Seconds GO waits for the rider after a win before the ride moves on by itself.")]
        [SerializeField] private float goWait = 12f;
        [SerializeField] private RideLine winLine = RideLine.StopClear;

        private string _goalNow;

        /// <summary>Big title on the dash ("DRAW THE 3").</summary>
        public string Title => title;

        /// <summary>The one-line goal on the dash. Games may change it as they go.</summary>
        public string Goal => string.IsNullOrEmpty(_goalNow) ? goal : _goalNow;

        /// <summary>0 to 1 toward the win.</summary>
        public float Progress { get; protected set; }

        /// <summary>Points scored in this game.</summary>
        public int Score { get; private set; }

        /// <summary>Stars earned (0 until won).</summary>
        public int Stars { get; private set; }

        /// <summary>True while the rider can play.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Seconds left before the game helps itself, for a countdown on the dash; negative when none shows.</summary>
        public virtual float SecondsLeft => -1f;

        /// <summary>Anything on the dash changed (progress, goal, score).</summary>
        public event Action GameChanged;

        /// <summary>The game is won: stars, and whether the rider won it (false when it played itself out).</summary>
        public event Action<int, bool> Won;

        /// <summary>The rider scored points at a place in the world (a burst of +N there).</summary>
        public event Action<int, Vector3> Scored;

        protected void SetGoal(string text)
        {
            _goalNow = text;
            GameChanged?.Invoke();
        }

        protected void Refresh() => GameChanged?.Invoke();

        protected void AddScore(int points, Vector3 where)
        {
            Score += points;
            if (Director != null) Director.AddScore(points);
            Scored?.Invoke(points, where);
            GameChanged?.Invoke();
        }

        protected override IEnumerator Run()
        {
            Stars = 0;
            Score = 0;
            Progress = 0f;
            _goalNow = null;
            IsPlaying = true;
            GameChanged?.Invoke();
            yield return Play();
            IsPlaying = false;
            bool byRider = !PlayedItself;
            Stars = byRider ? Mathf.Clamp(StarsEarned(), 1, 3) : 1;
            Progress = 1f;
            AddScore(100 * Stars, Vector3.zero);
            Won?.Invoke(Stars, byRider);
            GameChanged?.Invoke();
            Director.Narrate(winLine);
            yield return Director.WaitForSpeech();
            yield return Director.WaitForGo(goWait);
        }

        /// <summary>The game itself. Returns when it is won.</summary>
        protected abstract IEnumerator Play();

        /// <summary>1 to 3 stars for a game the rider won.</summary>
        protected abstract int StarsEarned();

        /// <summary>True when the win came from hurry or give-up help rather than the rider.</summary>
        protected abstract bool PlayedItself { get; }
    }
}
