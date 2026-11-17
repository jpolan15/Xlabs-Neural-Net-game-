using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The "PICK A STOP" board on the game table at the dock: six tiles (the full ride and the five stops). It shows the
    /// board only while the ride waits at the dock, and lights the chosen tile (its own material, a dark label, a small
    /// pop when the choice changes). The choice is the <see cref="ChapterPicker"/>'s; this only displays it. The tiles'
    /// words are the picker's chapter names, written here at runtime, and the builder leaves the board's renderers off
    /// (turned on here): in the saved scene the dock-only board is empty and dark, so the tools' sightline check at the
    /// other stops neither reads it nor counts it as blocking the boards that share the table.
    /// </summary>
    public sealed class ChapterBoardView : MonoBehaviour
    {
        [SerializeField] private ChapterPicker picker;
        [SerializeField] private GameObject board;
        [SerializeField] private TMP_Text header;
        [SerializeField] private string headerText = "PICK A STOP";
        [SerializeField] private TMP_Text hint;
        [SerializeField] private string hintText = "SHOOT ONE, THEN PULL GO";

        [Tooltip("One tile per chapter, in the picker's order.")]
        [SerializeField] private Renderer[] tiles = new Renderer[0];
        [SerializeField] private TMP_Text[] labels = new TMP_Text[0];
        [SerializeField] private Material tileIdle;
        [SerializeField] private Material tileChosen;
        [SerializeField] private Color labelIdle = Color.white;
        [SerializeField] private Color labelChosen = new Color(0.05f, 0.07f, 0.12f);
        [SerializeField] private float popScale = 1.12f;
        [SerializeField] private float popSeconds = 0.25f;

        private Vector3[] _scales;
        private int _shown = -1;
        private float _popAt = -10f;

        private void Awake()
        {
            _scales = new Vector3[tiles.Length];
            for (int i = 0; i < tiles.Length; i++) _scales[i] = tiles[i] != null ? tiles[i].transform.localScale : Vector3.one;
            if (board != null)
            {
                foreach (Renderer r in board.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            }

            if (header != null) header.text = headerText;
            if (hint != null) hint.text = hintText;
            if (picker == null) return;
            for (int i = 0; i < labels.Length && i < picker.Count; i++)
            {
                if (labels[i] != null) labels[i].text = picker.Chapters[i].name;
            }
        }

        private void Start()
        {
            if (board != null) board.SetActive(false);
        }

        private void Update()
        {
            if (board == null || picker == null) return;
            bool show = picker.IsOpen;
            if (board.activeSelf != show) board.SetActive(show);
            if (!show) return;

            if (picker.Selected != _shown)
            {
                if (_shown >= 0) _popAt = Time.time;
                _shown = picker.Selected;
                Paint();
            }

            Pop();
        }

        private void Paint()
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                bool chosen = i == _shown;
                if (tiles[i] != null)
                {
                    tiles[i].transform.localScale = _scales[i];
                    if (tileIdle != null && tileChosen != null) tiles[i].sharedMaterial = chosen ? tileChosen : tileIdle;
                }

                if (i < labels.Length && labels[i] != null) labels[i].color = chosen ? labelChosen : labelIdle;
            }
        }

        private void Pop()
        {
            if (_shown < 0 || _shown >= tiles.Length || tiles[_shown] == null) return;
            float t = Mathf.Clamp01((Time.time - _popAt) / Mathf.Max(0.01f, popSeconds));
            float k = 1f + (popScale - 1f) * Mathf.Sin(t * Mathf.PI);
            tiles[_shown].transform.localScale = _scales[_shown] * k;
        }
    }
}
