using System;
using System.Collections.Generic;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>
    /// One timed change: after its delay it runs for its duration, handing the eased progress (0 to 1, through its
    /// rate function) to its apply action. Begin runs once, just before the first apply, so a tween reads where its
    /// object is when it starts (a move starts from wherever the last move left it). Complete jumps to the end state:
    /// the operator's skip and a missed cue use it, so every tween must be correct when completed at once.
    /// </summary>
    public sealed class Tween
    {
        public float Delay;
        public float Duration;
        public Func<float, float> Rate;

        private readonly Action<float> _apply;
        private readonly Action _begin;
        private Action _end;

        public bool Started { get; private set; }
        public bool Done { get; private set; }

        public Tween(float duration, Action<float> apply, Func<float, float> rate = null, Action begin = null, Action end = null)
        {
            Duration = Math.Max(0f, duration);
            Rate = rate ?? Explainers.Rate.Smooth;
            _apply = apply;
            _begin = begin;
            _end = end;
        }

        /// <summary>Waits this much longer before starting. Returns itself, so it chains.</summary>
        public Tween After(float seconds)
        {
            Delay += seconds;
            return this;
        }

        /// <summary>Runs this when the tween ends (completed or played out).</summary>
        public Tween Then(Action end)
        {
            _end += end;
            return this;
        }

        /// <summary>Where the tween is when its own clock reads this many seconds since it was played.</summary>
        public void StepTo(float seconds)
        {
            if (Done || seconds < Delay) return;
            if (!Started) Begin();
            float t = Duration <= 0f ? 1f : (seconds - Delay) / Duration;
            if (t >= 1f)
            {
                Finish();
                return;
            }

            _apply?.Invoke(Rate(t));
        }

        /// <summary>Jumps to the end state now.</summary>
        public void Complete()
        {
            if (Done) return;
            if (!Started) Begin();
            Finish();
        }

        private void Begin()
        {
            Started = true;
            _begin?.Invoke();
        }

        private void Finish()
        {
            Done = true;
            _apply?.Invoke(Rate(1f));
            _end?.Invoke();
        }

        /// <summary>A tween that only waits, then runs an action (a show, a hide, a swap).</summary>
        public static Tween Call(float delay, Action action) => new Tween(0f, null, null, null, action) { Delay = delay };
    }

    /// <summary>
    /// The tweens one explainer is playing, on the game clock (Time.deltaTime, so a sped-up test runs them in step
    /// with the narration). Played tweens run on their own clocks from the moment they are played.
    /// </summary>
    public sealed class Timeline
    {
        private sealed class Running
        {
            public Tween Tween;
            public float Clock;
        }

        private readonly List<Running> _running = new List<Running>();

        public bool IsIdle => _running.Count == 0;
        public int Count => _running.Count;

        public void Play(Tween tween)
        {
            if (tween == null) return;
            var r = new Running { Tween = tween };
            _running.Add(r);
            tween.StepTo(0f);
            if (tween.Done) _running.Remove(r);
        }

        public void Play(IEnumerable<Tween> tweens)
        {
            if (tweens == null) return;
            foreach (Tween t in tweens) Play(t);
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < _running.Count; i++)
            {
                Running r = _running[i];
                r.Clock += deltaTime;
                r.Tween.StepTo(r.Clock);
            }

            _running.RemoveAll(r => r.Tween.Done);
        }

        /// <summary>Every playing tween jumps to its end, in the order they were played.</summary>
        public void CompleteAll()
        {
            // Completing can play nothing new, but copy anyway so an end action may touch the list safely.
            var copy = new List<Running>(_running);
            _running.Clear();
            foreach (Running r in copy) r.Tween.Complete();
        }

        /// <summary>Drops every playing tween where it is (a reset puts the pieces back itself).</summary>
        public void Clear() => _running.Clear();
    }
}
