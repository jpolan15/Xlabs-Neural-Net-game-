using System;
using UnityEngine;

namespace Convergence.Gameplay
{
    /// <summary>
    /// Controls the physical/visual state of the Awakening Gate.
    /// Strictly adheres to one-way authority: only responds to idempotent events
    /// from ChamberController and never evaluates neural weights or puzzles directly.
    /// </summary>
    public class GatewayController : MonoBehaviour
    {
        [SerializeField] private ChamberController chamberController;

        public bool IsOpen { get; private set; }

        public event Action OnGatewayOpened;
        public event Action OnGatewayClosed;

        private void Awake()
        {
            if (chamberController == null)
            {
                chamberController = FindAnyObjectByType<ChamberController>();
            }
        }

        private void OnEnable()
        {
            if (chamberController != null)
            {
                chamberController.OnPuzzleSolved += HandlePuzzleSolved;
                chamberController.OnChamberReset += HandleChamberReset;
            }
        }

        private void OnDisable()
        {
            if (chamberController != null)
            {
                chamberController.OnPuzzleSolved -= HandlePuzzleSolved;
                chamberController.OnChamberReset -= HandleChamberReset;
            }
        }

        /// <summary>
        /// Idempotent unlock triggered strictly by ChamberController.OnPuzzleSolved.
        /// </summary>
        private void HandlePuzzleSolved()
        {
            Open();
        }

        private void HandleChamberReset()
        {
            Close();
        }

        public void Open()
        {
            if (IsOpen) return;

            IsOpen = true;
            OnGatewayOpened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen) return;

            IsOpen = false;
            OnGatewayClosed?.Invoke();
        }
    }
}
