using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// The chapter picker (Level 1): while the ride waits at the dock the game table is up with a "PICK A STOP" board, six
    /// laser tiles in a 3×2 grid (the full ride, then pixels, neuron, network, backprop, training). The chosen tile is where
    /// GO starts the ride (RideDirector.SetStartSegment); the ride runs on from there to the end. The board is built on the
    /// pod instance's GameTable in the scene (a dock-only board, wired with the director it needs). Its renderers are left
    /// off and its words empty here: ChapterBoardView turns them on and writes them at runtime, so the sightline check at
    /// the other stops never sees the board on the table.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        /// <summary>The chapters: the tile's words and the segment GO starts at (the Seg* constants, Explainers).</summary>
        private static readonly (string label, int segment)[] ChapterList =
        {
            ("FULL\nRIDE", SegPixels),
            ("1\nPIXELS", SegPixels),
            ("2\nNEURON", SegNeuronExplain),
            ("3\nNETWORK", SegNetworkExplain),
            // Backprop needs the guess and the loss it fixes, so it starts at the wrong-guess stop.
            ("4\nBACKPROP", SegWrong),
            ("5\nTRAINING", SegRim),
        };

        private static readonly Vector2 ChapterTileSize = new Vector2(0.195f, 0.125f);
        private const float ChapterColumnPitch = 0.21f;
        private static readonly float[] ChapterRows = { 0.07f, -0.075f };

        /// <summary>
        /// Builds the chapter board on the pod's game table and wires it: the picker (Gameplay, on the director's object) with
        /// the chapters and the tiles, the view (on the pod), and the table, which now also comes up at the dock. Level 1 only;
        /// call it after WireGames has added the GameTableView.
        /// </summary>
        private static void WireChapters(GameObject pod, RideDirector rd, Moments m)
        {
            Transform table = pod.transform.Find("GameTable");
            Transform board = BuildChapterBoard(table, out List<Object> hits, out List<Object> faces, out List<Object> labels);

            var picker = rd.gameObject.AddComponent<ChapterPicker>();
            Ref(picker, "director", rd);
            Set(picker, "chapters", p =>
            {
                p.arraySize = ChapterList.Length;
                for (int i = 0; i < ChapterList.Length; i++)
                {
                    SerializedProperty e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("name").stringValue = ChapterList[i].label;
                    e.FindPropertyRelative("segment").intValue = ChapterList[i].segment;
                }
            });
            Refs(picker, "tiles", hits);
            Int(picker, "selected", 0);
            // A ride started at the wrong-guess stop or later skipped the network stop's forward pass: its guess is computed then.
            Ref(picker, "primeGuess", m.Forward);
            Int(picker, "primeFromSegment", SegWrong);

            var view = pod.AddComponent<ChapterBoardView>();
            Ref(view, "picker", picker);
            Ref(view, "board", board.gameObject);
            Ref(view, "header", board.Find("Header").GetComponent<TextMeshPro>());
            Ref(view, "hint", board.Find("Hint").GetComponent<TextMeshPro>());
            Refs(view, "tiles", faces);
            Refs(view, "labels", labels);
            Ref(view, "tileIdle", Mat("Button"));
            Ref(view, "tileChosen", Mat("Amber"));
            Set(view, "labelIdle", p => p.colorValue = Color.white);
            Set(view, "labelChosen", p => p.colorValue = new Color(0.05f, 0.07f, 0.12f));

            // The GameTableView is on the Pod prefab's instance: the field goes in through the serialized property.
            Set(pod.GetComponent<GameTableView>(), "upAtDock", p => p.boolValue = true);
        }

        /// <summary>
        /// The board, in the frame the other boards use (centred under the table's HUD strip): a solid plate that covers the HUD
        /// rows (a game's title and score before any game), the header and the hint above, and the six tiles, each a trigger
        /// collider with its face and its label. Labels are 0.04 (0.034 m on the 0.85-scaled table, auto-sized no smaller than
        /// 0.03 m) at about 0.75 m from the eye. Renderers off, words empty (see the class summary); the board starts hidden.
        /// </summary>
        private static Transform BuildChapterBoard(Transform table, out List<Object> hits, out List<Object> faces, out List<Object> labels)
        {
            var board = Make("ChapterBoard", table, BoardCentre + BoardLift).transform;
            GameObject plate = Quad("Plate", board, new Vector3(0f, 0.045f, -0.008f), new Vector2(0.66f, 0.47f), "PlateSolid");
            plate.GetComponent<Renderer>().enabled = false;

            TextMeshPro header = Text("Header", board, new Vector3(0f, 0.235f, -0.012f), "", 0.045f, _theme.gold, 0.62f, bold: true);
            header.rectTransform.sizeDelta = new Vector2(0.62f, 0.055f);
            header.textWrappingMode = TextWrappingModes.NoWrap;
            TextMeshPro hint = Text("Hint", board, new Vector3(0f, 0.185f, -0.012f), "", 0.026f, _theme.cyan, 0.62f, bold: true);
            hint.rectTransform.sizeDelta = new Vector2(0.62f, 0.034f);
            hint.textWrappingMode = TextWrappingModes.NoWrap;

            hits = new List<Object>();
            faces = new List<Object>();
            labels = new List<Object>();
            for (int i = 0; i < ChapterList.Length; i++)
            {
                var at = new Vector3((i % 3 - 1) * ChapterColumnPitch, ChapterRows[i / 3], -0.012f);
                GameObject tile = Make("Tile" + i, board, at);
                Renderer face = Quad("Face", tile.transform, Vector3.zero, ChapterTileSize, "Button").GetComponent<Renderer>();
                face.enabled = false;
                TextMeshPro label = Text("Label", tile.transform, new Vector3(0f, 0f, -0.004f), "", 0.04f, Color.white, ChapterTileSize.x - 0.01f, bold: true);
                label.rectTransform.sizeDelta = new Vector2(ChapterTileSize.x - 0.01f, ChapterTileSize.y - 0.005f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.enableAutoSizing = true;
                label.fontSizeMin = 0.27f; // the builder's Text() units: small enough that BACKPROP fits its tile (2026-11-17)
                label.fontSizeMax = 0.40f;
                var box = tile.AddComponent<BoxCollider>();
                box.size = new Vector3(ChapterTileSize.x, ChapterTileSize.y + 0.006f, 0.03f);
                box.isTrigger = true;
                hits.Add(box);
                faces.Add(face);
                labels.Add(label);
            }

            board.gameObject.SetActive(false);
            return board;
        }
    }
}
