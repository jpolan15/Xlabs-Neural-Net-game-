using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The plug game's board on the table: six string heads on the left (real hidden neurons, glowing with their real
    /// brightness for the hero 3) and ten answer sockets on the right, 0 to 9, each glowing with the answer's real
    /// softmax share; the answer Pixel would say now is gold. A string is drawn from its head to the socket it is
    /// plugged into, to the laser's dot while a hand carries it, or hanging loose. The 3 socket has a gold ring: the
    /// goal. One glow mesh (heads, then sockets, then the ring) is recoloured; display only, Core decides.
    /// </summary>
    public sealed class PlugBoardView : MonoBehaviour
    {
        private const int Heads = 6, Sockets = 10;

        [SerializeField] private RideDirector director;
        [SerializeField] private PlugMoment game;
        [SerializeField] private GameObject board;
        [SerializeField] private int showSegment = -1;
        [SerializeField] private MeshFilter tiles;
        [SerializeField] private TextMeshPro[] digitColumns = new TextMeshPro[0];
        [SerializeField] private Transform[] heads = new Transform[0];
        [SerializeField] private Transform[] sockets = new Transform[0];
        [SerializeField] private LineRenderer[] strings = new LineRenderer[0];
        [SerializeField] private Color stringColor = new Color(0.4f, 0.9f, 1f, 1f);
        [SerializeField] private Color carriedColor = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color answerColor = new Color(0.4f, 0.85f, 1f, 1f);
        [SerializeField] private Color winningColor = new Color(1f, 0.82f, 0.4f, 1f);

        private Mesh _mesh;
        private Color[] _colors;
        private int _shownGuess = -1;

        private void Awake()
        {
            if (tiles == null) return;
            _mesh = Instantiate(tiles.sharedMesh);
            tiles.sharedMesh = _mesh;
            _colors = _mesh.colors;
        }

        private void OnEnable()
        {
            if (director == null) return;
            director.SegmentStarted += OnSegment;
            director.SegmentEnded += OnSegmentEnded;
        }

        private void OnDisable()
        {
            if (director == null) return;
            director.SegmentStarted -= OnSegment;
            director.SegmentEnded -= OnSegmentEnded;
        }

        private void Start()
        {
            if (board != null) board.SetActive(false);
        }

        private void OnSegment(int index)
        {
            if (index != showSegment || board == null) return;
            game.Prepare();
            _shownGuess = -1;
            board.SetActive(true);
        }

        private void OnSegmentEnded(int index)
        {
            if (index == showSegment && board != null) board.SetActive(false);
        }

        private void Update()
        {
            if (board == null || !board.activeSelf || game == null || game.Puzzle == null || _colors == null) return;
            var puzzle = game.Puzzle;
            double[] p = puzzle.Probabilities();
            int guess = puzzle.Guess;
            for (int k = 0; k < Sockets && k < p.Length; k++)
            {
                Color c = k == guess ? winningColor : answerColor;
                SetTile(Heads + k, new Color(c.r, c.g, c.b, Mathf.Lerp(0.1f, 1f, Mathf.Sqrt((float)p[k]))));
            }

            int left = game.Carrying(RideHand.Left), right = game.Carrying(RideHand.Right);
            for (int s = 0; s < strings.Length && s < puzzle.StringCount; s++)
            {
                float glow = (float)puzzle.Brightness(s);
                bool carried = s == left || s == right;
                SetTile(s, carried ? carriedColor : new Color(stringColor.r, stringColor.g, stringColor.b, Mathf.Lerp(0.25f, 1f, glow)));
                Vector3 from = heads[s].position;
                Vector3 to;
                int hand = s == right ? RideHand.Right : RideHand.Left;
                if (carried && RideAim.TryGet(hand, out _, out Vector3 aim)) to = aim;
                else if (puzzle.PluggedInto(s) >= 0) to = sockets[puzzle.PluggedInto(s)].position;
                else to = from + board.transform.TransformVector(new Vector3(0.03f, -0.025f, 0f));
                DrawString(strings[s], from, to, carried ? carriedColor : stringColor, carried ? 1f : glow);
            }

            _mesh.colors = _colors;
            if (guess != _shownGuess) ShowGuess(guess);
        }

        /// <summary>The digit Pixel would say now is gold in its column.</summary>
        private void ShowGuess(int guess)
        {
            _shownGuess = guess;
            for (int col = 0; col < digitColumns.Length; col++)
            {
                var text = new System.Text.StringBuilder();
                for (int r = 0; r < 5; r++)
                {
                    int k = col * 5 + r;
                    if (r > 0) text.Append('\n');
                    text.Append(k == guess ? "<color=#FFD166>" + k + "</color>" : k.ToString());
                }

                digitColumns[col].text = text.ToString();
            }
        }

        private void SetTile(int tile, Color c)
        {
            int at = tile * 4;
            for (int i = at; i < at + 4 && i < _colors.Length; i++) _colors[i] = c;
        }

        private void DrawString(LineRenderer line, Vector3 from, Vector3 to, Color color, float strength)
        {
            int n = line.positionCount;
            Vector3 sag = -board.transform.up * Mathf.Min(0.04f, Vector3.Distance(from, to) * 0.15f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                line.SetPosition(i, Vector3.Lerp(from, to, t) + sag * (4f * t * (1f - t)));
            }

            Color c = new Color(color.r, color.g, color.b, Mathf.Lerp(0.35f, 1f, strength));
            line.startColor = c;
            line.endColor = c;
        }
    }
}
