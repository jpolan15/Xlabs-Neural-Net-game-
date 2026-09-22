using System;
using UnityEngine;
using Convergence.Core.Puzzles;

namespace Convergence.Gameplay
{
    /// <summary>
    /// Tracks player performance metrics during Level 1:
    /// time elapsed, test pulses fired, failed attempts, and rank (S / A / B / C).
    /// </summary>
    public class PerformanceTracker : MonoBehaviour
    {
        private float _startTime;
        private bool _isTimerRunning;
        private float _finalTime;
        private float _lastProgressTime;
        private float _lastMetricsUpdateTime;

        public float ElapsedTime => _isTimerRunning ? Time.time - _startTime : _finalTime;
        public float TimeSinceLastProgress => _isTimerRunning ? (Time.time - _lastProgressTime) : 0f;
        public int PulsesFired { get; private set; }
        public int IncorrectAttempts { get; private set; }
        public double BestAccuracy { get; private set; }
        public char Rank { get; private set; } = 'C';

        public event Action OnMetricsUpdated;
        public event Action OnHintTriggered;

        private void Start()
        {
            ResetTracker();
        }

        private void Update()
        {
            if (_isTimerRunning && Time.time - _lastMetricsUpdateTime >= 0.25f)
            {
                _lastMetricsUpdateTime = Time.time;
                OnMetricsUpdated?.Invoke();
            }
        }

        public void StartTimer()
        {
            _startTime = Time.time;
            _lastProgressTime = Time.time;
            _isTimerRunning = true;
        }

        public void StopTimer()
        {
            if (_isTimerRunning)
            {
                _finalTime = Time.time - _startTime;
                _isTimerRunning = false;
                CalculateRank();
            }
        }

        public void RecordPulseFired()
        {
            PulsesFired++;
            if (!_isTimerRunning)
            {
                StartTimer();
            }
            OnMetricsUpdated?.Invoke();
        }

        public void RecordEvaluation(PuzzleEvaluation evaluation)
        {
            if (evaluation == null) return;

            if (evaluation.Accuracy > BestAccuracy)
            {
                BestAccuracy = evaluation.Accuracy;
                _lastProgressTime = Time.time;
            }

            if (!evaluation.Passed)
            {
                IncorrectAttempts++;
            }
            else
            {
                StopTimer();
            }

            CalculateRank();
            OnMetricsUpdated?.Invoke();
        }

        public void ResetTracker()
        {
            _startTime = Time.time;
            _finalTime = 0.0f;
            _isTimerRunning = false;
            PulsesFired = 0;
            IncorrectAttempts = 0;
            BestAccuracy = 0.0;
            Rank = 'C';
            OnMetricsUpdated?.Invoke();
        }

        private void CalculateRank()
        {
            if (BestAccuracy < 1.0)
            {
                Rank = 'C';
                return;
            }

            // S Rank: Solved with <= 6 pulses and in < 90 seconds
            if (PulsesFired <= 6 && _finalTime < 90.0f)
            {
                Rank = 'S';
            }
            // A Rank: Solved with <= 12 pulses
            else if (PulsesFired <= 12)
            {
                Rank = 'A';
            }
            // B Rank: Solved
            else
            {
                Rank = 'B';
            }
        }
    }
}
