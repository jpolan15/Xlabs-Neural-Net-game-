using System;
using System.IO;
using UnityEngine;
using Convergence.Core.Puzzles;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Dev-only playtest lines. Compiled out of release player builds.
    /// </summary>
    public sealed class PlaytestEventLog : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private NeuralState neuralState;
        [SerializeField] private ChamberController chamberController;
        [SerializeField] private LiveEvaluationRelay live;
        string _path;
        bool _passed;

        void Awake()
        {
            if (neuralState == null) neuralState = FindAnyObjectByType<NeuralState>();
            if (chamberController == null) chamberController = FindAnyObjectByType<ChamberController>();
            if (live == null) live = FindAnyObjectByType<LiveEvaluationRelay>();
            _path = Path.Combine(Application.persistentDataPath, "playtest_log.txt");
            Write("session start");
        }

        void OnEnable()
        {
            if (neuralState != null) neuralState.OnStateMutated += HandleMutated;
            if (chamberController != null) chamberController.OnEvaluationComplete += HandleEvaluation;
            if (live != null) live.OnHintControl += HandleHint;
        }

        void OnDisable()
        {
            if (neuralState != null) neuralState.OnStateMutated -= HandleMutated;
            if (chamberController != null) chamberController.OnEvaluationComplete -= HandleEvaluation;
            if (live != null) live.OnHintControl -= HandleHint;
        }

        void HandleMutated()
        {
            if (neuralState == null) return;
            Write("control w1=" + neuralState.Weight1.ToString("0.0") + " w2=" + neuralState.Weight2.ToString("0.0") + " b=" + neuralState.Bias.ToString("0.0"));
        }

        void HandleEvaluation(PuzzleEvaluation eval)
        {
            if (eval == null) return;
            if (eval.Diagnostics != null)
            {
                for (int i = 0; i < eval.Diagnostics.Count; i++)
                {
                    Write("card " + i + " correct=" + (eval.Diagnostics[i].IsCorrect && eval.ActivationMatches));
                }
            }

            if (eval.Passed != _passed)
            {
                _passed = eval.Passed;
                Write(_passed ? "solved" : "unsolved");
            }

            var board = ChamberPromptBoard.Instance;
            if (board != null) Write("prompt " + board.Current);
        }

        void HandleHint(string id)
        {
            Write("hint " + id);
        }

        void Write(string line)
        {
            string row = DateTime.UtcNow.ToString("o") + " " + line + Environment.NewLine;
            File.AppendAllText(_path, row);
        }
#else
        void Awake() { }
#endif
    }
}
