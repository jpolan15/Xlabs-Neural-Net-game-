using System;
using System.Collections.Generic;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// A tile per kind of object (drone, comet, rock, chunk) showing, at the current settings, whether it gets
    /// zapped or passes and whether that is right. Icons only. Fed by Gameplay's evaluated board.
    /// </summary>
    public sealed class LiveBoardView : MonoBehaviour
    {
        [Serializable]
        public struct Tile
        {
            public GameObject root;
            public Renderer frame;
            public Renderer action;
            public Renderer status;
        }

        [SerializeField] private StationController station;
        [SerializeField] private Tile[] tiles = new Tile[4];
        [SerializeField] private Material frameRight;
        [SerializeField] private Material frameWrong;
        [SerializeField] private Material zapIcon;
        [SerializeField] private Material passIcon;
        [SerializeField] private Material rightIcon;
        [SerializeField] private Material wrongIcon;

        private void OnEnable()
        {
            if (station == null) return;
            station.BoardChanged += Refresh;
            if (station.Board.Count > 0) Refresh(station.Board);
        }

        private void OnDisable()
        {
            if (station != null) station.BoardChanged -= Refresh;
        }

        private void Refresh(IReadOnlyList<CaseOutcome> board)
        {
            for (int i = 0; i < tiles.Length; i++) tiles[i].root.SetActive(false);
            for (int i = 0; i < board.Count; i++)
            {
                CaseOutcome o = board[i];
                Tile tile = tiles[(int)o.Kind];
                tile.root.SetActive(true);
                tile.frame.sharedMaterial = o.Correct ? frameRight : frameWrong;
                tile.action.sharedMaterial = o.Fired ? zapIcon : passIcon;
                tile.status.sharedMaterial = o.Correct ? rightIcon : wrongIcon;
            }
        }
    }
}
