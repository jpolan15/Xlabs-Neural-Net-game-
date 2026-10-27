using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;
using Convergence.Presentation.Ride;
using Convergence.Presentation.Ride.Explainers;
using Convergence.XR.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Level 1's games (v5, TASK_LEVEL1_V5_GAMEPLAY; boards v6, TASK_LEVEL1_V6_TRAIN): the game table every mini-game is
    /// played on (it rises out of the dash to tablet height), its boards (the drawing board, the 7×7 weight grid, the send
    /// card, the hunt card, the training board), the HUD (title, goal,
    /// progress, countdown, score), the "STOP CLEAR!" stamp with stars and confetti, the noise specks the rider zaps
    /// between stops, and a laser on each hand. Geometry is built into the pod prefab; the scene wiring (moments,
    /// director) is added to the pod instance in <see cref="WireGames"/>.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        /// <summary>
        /// The table's raised pose in the pod, for the 1.30 m eye PodSeat normalises to: 0.72 m ahead, its top edge 15.7
        /// degrees under the horizon (the forward view stays clear above the rim), the games' board centred 34 degrees
        /// down and its lowest edge 46 degrees down (it was 56, a neck-bending look), tilted 45 degrees so the face is within
        /// 12 degrees of square to the eye. <see cref="AssertTableErgonomics"/> fails the build if a change breaks that.
        /// </summary>
        private static readonly Vector3 TablePosition = new Vector3(0f, 0.89f, 0.72f);
        private const float TableTilt = 45f;
        private const float TableScale = 0.85f;
        private static readonly Vector2 TableSize = new Vector2(0.68f, 0.54f);
        private static readonly Vector3 BoardCentre = new Vector3(0f, -0.075f, 0f);

        private static void MakeGameMaterials()
        {
            Texture2D glow = GlowTexture();
            Particles("Laser", Color.white, true, null);
            Particles("Confetti", Color.white, false, null);
            Particles("Spark", Color.white, true, glow);
            M("BootCover", Color.black, Kind.Alpha).renderQueue = (int)RenderQueue.Overlay; // the black in front of the eyes while the ride boots
            M("GlowGreen", new Color(0.45f, 1f, 0.5f, 0.75f), Kind.Additive, glow);
            M("GlowStarDim", new Color(0.5f, 0.55f, 0.7f, 0.25f), Kind.Additive, glow);
            M("StarLit", new Color(_theme.gold.r, _theme.gold.g, _theme.gold.b, 0.95f), Kind.Additive);
            M("StarDim", new Color(0.4f, 0.45f, 0.6f, 0.35f), Kind.Additive);
            M("StringLine", Color.white, Kind.Additive);
            H("Noise", _theme.rewindMagenta, 0.3f, 0.9f, Color.white, _theme.rewindMagenta * 0.6f);
            H("TableFrame", _theme.cyan * 0.35f, 0.3f, 0.3f);
            H("Button", new Color(0.12f, 0.22f, 0.36f), 0.6f, 0.8f, _theme.cyan, _theme.cyan * 0.12f);
        }

        /// <summary>
        /// The starburst the 3 throws when a wire snaps on: soft gold and white sparks, played by BlameView (Emit), sized for
        /// the network's metres.
        /// </summary>
        private static ParticleSystem BuildSparks(Transform parent, Vector3 at)
        {
            var sparks = Make("Sparks", parent, at).AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = sparks.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 96;
            main.startColor = new ParticleSystem.MinMaxGradient(_theme.gold, Color.white);
            ParticleSystem.EmissionModule emission = sparks.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;
            ParticleSystem.ColorOverLifetimeModule fade = sparks.colorOverLifetime;
            fade.enabled = true;
            var ramp = new Gradient();
            ramp.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = ramp;
            var renderer = sparks.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Mat("Spark");
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return sparks;
        }

        /// <summary>URP Particles/Unlit (it takes vertex colours, which line renderers and particles need).</summary>
        private static Material Particles(string name, Color c, bool additive, Texture tex)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }

            m.shader = shader;
            m.SetColor("_BaseColor", c);
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", additive ? 2 : 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0);
            m.SetFloat("_Cull", 0);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            Mats[name] = m;
            return m;
        }

        // ---------- geometry (pod prefab) ----------

        /// <summary>
        /// Where the raised table's edges sit in the rider's view, seen from the nominal eye. Its top rim must stay at least
        /// 15 degrees under the horizon so the track and the world-space explainers show above it, the games' board
        /// (the 7×7 grid's centre) must sit within 38 degrees of the horizon, and no edge may reach past 50 degrees down
        /// (past that the rider is bending the neck to read it). It throws with the measured numbers when one is off.
        /// </summary>
        private static void AssertTableErgonomics()
        {
            const float minTop = 15f, maxCentre = 38f, maxBottom = 50f;
            Quaternion tilt = Quaternion.Euler(TableTilt, 0f, 0f);
            float Down(float localY)
            {
                Vector3 d = TablePosition + tilt * new Vector3(0f, localY * TableScale, 0f) - EyePoint;
                return Mathf.Atan2(-d.y, d.z) * Mathf.Rad2Deg;
            }

            float top = Down(TableSize.y * 0.5f), centre = Down(BoardCentre.y), bottom = Down(-TableSize.y * 0.5f);
            var bad = new List<string>();
            if (top < minTop) bad.Add("its top rim is " + top.ToString("0.0") + " degrees under the horizon (needs " + minTop + " or more, so the view ahead stays clear)");
            if (centre > maxCentre) bad.Add("its board centre is " + centre.ToString("0.0") + " degrees down (needs " + maxCentre + " or less)");
            if (bottom > maxBottom) bad.Add("its lowest edge is " + bottom.ToString("0.0") + " degrees down (needs " + maxBottom + " or less)");
            if (bad.Count > 0) throw new System.InvalidOperationException("The game table is uncomfortable to look at: " + string.Join("; ", bad) + ". Adjust TablePosition, TableTilt or TableScale in NeuralRideBuilder.Games.cs.");
        }

        /// <summary>The game table, raised: frame, back, the HUD strip and the three boards (grid, plug, descent).</summary>
        private static Transform BuildGameTable(Transform pod)
        {
            AssertTableErgonomics();
            var table = Make("GameTable", pod, TablePosition).transform;
            table.localRotation = Quaternion.Euler(TableTilt, 0f, 0f);
            table.localScale = Vector3.one * TableScale; // GameTableView grows it from here, so the raised size is this one
            Board("Back", table, new Vector3(0, 0, 0.004f), TableSize, "PlateSolid", 0.02f);
            Quad("Frame", table, new Vector3(0, 0, 0.011f), TableSize + new Vector2(0.014f, 0.014f), "TableFrame");

            float top = TableSize.y * 0.5f;
            // The subtitles ride on the table while it is up (it covers the dash screen): SubtitleMirror copies them.
            TextMeshPro subtitle = Text("Subtitle", table, new Vector3(0, top - 0.03f, -0.006f), "", 0.026f, _theme.offWhite, 0.64f);
            subtitle.rectTransform.sizeDelta = new Vector2(0.64f, 0.045f);
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 0.2f;
            subtitle.fontSizeMax = 0.26f;
            // The table sits farther away and smaller than it did (TableScale), so the title and score are bigger to stay readable.
            TextMeshPro title = Text("Title", table, new Vector3(-0.09f, top - 0.088f, -0.006f), "DRAW YOUR 3", 0.039f, _theme.gold, 0.46f, TextAlignmentOptions.Left, true);
            title.rectTransform.sizeDelta = new Vector2(0.46f, 0.045f);
            TextMeshPro score = Text("Score", table, new Vector3(0.175f, top - 0.088f, -0.006f), "SCORE 0", 0.042f, _theme.cyan, 0.3f, TextAlignmentOptions.Right, true);
            score.rectTransform.sizeDelta = new Vector2(0.3f, 0.048f);
            TextMeshPro goal = Text("Goal", table, new Vector3(-0.06f, top - 0.124f, -0.006f), "", 0.026f, Color.white, 0.52f, TextAlignmentOptions.Left);
            goal.rectTransform.sizeDelta = new Vector2(0.52f, 0.036f);
            goal.enableAutoSizing = true;
            goal.fontSizeMin = 0.2f;
            goal.fontSizeMax = 0.26f;
            TextMeshPro timer = Text("Timer", table, new Vector3(0.24f, top - 0.124f, -0.006f), "", 0.03f, Color.white, 0.14f, TextAlignmentOptions.Right, true);
            timer.rectTransform.sizeDelta = new Vector2(0.14f, 0.04f);
            Quad("ProgressBack", table, new Vector3(0, top - 0.152f, -0.004f), new Vector2(TableSize.x - 0.06f, 0.012f), "PodMid");
            var fillPivot = Make("ProgressFill", table, new Vector3(-(TableSize.x - 0.06f) * 0.5f, top - 0.152f, -0.005f)).transform;
            Quad("Bar", fillPivot, new Vector3((TableSize.x - 0.06f) * 0.5f, 0, 0), new Vector2(TableSize.x - 0.06f, 0.012f), "GlowGreen");
            Make("PopupAnchor", table, new Vector3(0.2f, top - 0.18f, -0.03f));

            Transform grid = BuildWeightGrid(table);
            grid.localPosition = BoardCentre + new Vector3(0, 0, -0.006f);
            BuildDrawBoard(table);
            BuildSendBoard(table);
            BuildTrainBoard(table);

            var shoot = Make("HuntBoard", table, BoardCentre).transform;
            Text("Shoot", shoot, new Vector3(0, 0.03f, -0.006f), "DRAG HOT WIRES\nTO THE 3!", 0.05f, _theme.rewindMagenta, 0.6f, bold: true).rectTransform.sizeDelta = new Vector2(0.6f, 0.16f);
            Text("Up", shoot, new Vector3(0, -0.1f, -0.006f), "look up: grab a glowing wire end, drag it to the 3", 0.02f, Color.white, 0.6f);
            shoot.gameObject.SetActive(false);
            return table;
        }

        /// <summary>A line renderer in a board's own frame (the boards move with the table).</summary>
        private static LineRenderer BoardLine(string name, Transform parent, int points, float width, Color color)
        {
            var line = Make(name, parent, Vector3.zero).AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = points;
            line.widthMultiplier = width;
            line.numCapVertices = 3;
            line.numCornerVertices = 2;
            line.sharedMaterial = Mat("Laser");
            line.startColor = line.endColor = color;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        /// <summary>
        /// A button on a board: its tile goes into the board's tile mesh; a trigger collider and a white label sit on it
        /// (white stays readable on the dark, the pulsing and the lit button colours).
        /// </summary>
        private static Transform BoardButton(TileMesh tiles, Transform board, string name, Vector3 at, Vector2 size, string label, float labelSize = 0.026f)
        {
            tiles.Face(at, size.x, size.y, new Color(0.15f, 0.35f, 0.6f, 0.6f));
            var button = Make(name, board, at);
            Text("Label", button.transform, new Vector3(0, 0, -0.004f), label, labelSize, Color.white, size.x, bold: true).rectTransform.sizeDelta = new Vector2(size.x, size.y);
            var c = button.AddComponent<BoxCollider>();
            c.size = new Vector3(size.x, size.y + 0.006f, 0.03f);
            c.isTrigger = true;
            return button.transform;
        }

        /// <summary>A digit as 3×5 tiles facing the rider (-z), centred at c.</summary>
        private static void FaceGlyph(TileMesh tiles, int n, Vector3 c, float cell, Color color)
        {
            string[] rows = DigitGlyphs[n].Split('|');
            for (int r = 0; r < 5; r++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (rows[r][col] == '#') tiles.Face(c + new Vector3((col - 1) * cell, (2 - r) * cell, 0f), cell * 0.9f, cell * 0.9f, color);
                }
            }
        }

        /// <summary>
        /// The v6 boards stand this far in front of the table's back plate (its front face is at z = -0.006), so their plates,
        /// lines and tiles are never coplanar with it (no z-fighting, and the sightline check sees them).
        /// </summary>
        private static readonly Vector3 BoardLift = new Vector3(0f, 0f, -0.008f);

        /// <summary>
        /// The drawing canvas's side, metres, and its centre on the drawing board; the readout (what Pixel sees, the bonus
        /// round's bars) is centred at ReadoutX, 15 mm clear of the canvas's glowing edge.
        /// </summary>
        private const float CanvasSide = 0.27f;
        private static readonly Vector3 CanvasCentre = new Vector3(-0.142f, 0f, -0.006f);
        private const float ReadoutX = 0.106f;

        /// <summary>
        /// The drawing board (v6), for "draw your 3" and the bonus round: the canvas (a dark square with a trigger collider
        /// the held laser draws on, and the ink mesh DrawBoardView fills), and one tile mesh holding, in order, the 7×7 of
        /// what Pixel sees, the ten probability bars of the bonus round, DONE, CLEAR, the canvas's four edges, and the bars'
        /// digits. Two draw calls plus the labels.
        /// </summary>
        private static void BuildDrawBoard(Transform table)
        {
            var board = Make("DrawBoard", table, BoardCentre + BoardLift).transform;
            Quad("CanvasPlate", board, CanvasCentre, new Vector2(CanvasSide, CanvasSide), "PlateSolid");
            var canvas = Make("Canvas", board, CanvasCentre + new Vector3(0, 0, -0.002f));
            canvas.transform.localScale = new Vector3(CanvasSide, CanvasSide, 1f);
            var box = canvas.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 0.06f);
            box.isTrigger = true;
            var ink = Make("Ink", board, CanvasCentre + new Vector3(0, 0, -0.004f));
            ink.AddComponent<MeshFilter>();
            var inkRenderer = ink.AddComponent<MeshRenderer>();
            inkRenderer.sharedMaterial = Mat("Laser");
            inkRenderer.shadowCastingMode = ShadowCastingMode.Off;
            inkRenderer.receiveShadows = false;

            var tiles = new TileMesh();
            // What Pixel sees: the 49 numbers as a 7×7 (the prompt text doubles as its label).
            const float pitch = 0.025f;
            Vector3 middle = new Vector3(ReadoutX, 0.015f, -0.006f);
            for (int i = 0; i < 49; i++)
            {
                Vector3 at = middle + new Vector3((i % 7 - 3f) * pitch, (3f - i / 7) * pitch, 0f);
                tiles.Face(at, pitch * 0.9f, pitch * 0.9f, new Color(0.04f, 0.06f, 0.12f, 0.9f));
            }

            // The bonus round's ten bars, each over its digit (in the same place as the 7×7; the view shows one or the other).
            const float barPitch = 0.019f, barBottom = -0.06f, barHeight = 0.12f;
            for (int k = 0; k < 10; k++)
            {
                float x = ReadoutX + (k - 4.5f) * barPitch;
                tiles.Face(new Vector3(x, barBottom + barHeight * 0.5f, -0.006f), barPitch * 0.7f, barHeight, new Color(0.3f, 0.6f, 0.95f, 0.85f));
            }

            BoardButton(tiles, board, "Done", new Vector3(0.262f, 0.045f, -0.006f), new Vector2(0.095f, 0.07f), "DONE", 0.025f);
            BoardButton(tiles, board, "Clear", new Vector3(0.262f, -0.045f, -0.006f), new Vector2(0.095f, 0.07f), "CLEAR", 0.025f);
            // The canvas's glowing edge, so the rider sees where to draw (four bars; DrawBoardView leaves them lit).
            Color edge = new Color(_theme.cyan.r, _theme.cyan.g, _theme.cyan.b, 0.75f);
            float side = CanvasSide + 0.008f, bar = 0.004f;
            Vector3 c0 = CanvasCentre + new Vector3(0f, 0f, -0.003f);
            tiles.Face(c0 + new Vector3(0f, side * 0.5f, 0f), side + bar, bar, edge);
            tiles.Face(c0 + new Vector3(0f, -side * 0.5f, 0f), side + bar, bar, edge);
            tiles.Face(c0 + new Vector3(-side * 0.5f, 0f, 0f), bar, side, edge);
            tiles.Face(c0 + new Vector3(side * 0.5f, 0f, 0f), bar, side, edge);
            // The bars' digits, 15 × 25 mm (inside the bar pitch), big enough that 3, 5, 6, 8 and 9 stay apart.
            for (int k = 0; k < 10; k++) FaceGlyph(tiles, k, new Vector3(ReadoutX + (k - 4.5f) * barPitch, barBottom - 0.016f, -0.006f), 0.005f, Color.white);
            TileObject("Tiles", board, tiles, "Laser");

            // "Count" is the hint in the empty canvas (DrawBoardView hides it once there is ink).
            TextMeshPro hint = Text("Count", board, CanvasCentre + new Vector3(0f, 0f, -0.003f), "HOLD TRIGGER\nAND DRAW", 0.032f, new Color(0.55f, 0.95f, 1f, 0.8f), 0.26f, bold: true);
            hint.rectTransform.sizeDelta = new Vector2(0.26f, 0.09f);
            TextMeshPro prompt = Text("Prompt", board, new Vector3(ReadoutX, 0.128f, -0.006f), "PIXEL SEES", 0.026f, _theme.gold, 0.2f, bold: true);
            prompt.rectTransform.sizeDelta = new Vector2(0.2f, 0.034f);
            // The bonus round's verdict sits under the bar digits and right of the canvas (clear of its edge).
            TextMeshPro answer = Text("Answer", board, new Vector3(0.16f, -0.115f, -0.006f), "", 0.028f, Color.white, 0.3f, bold: true);
            answer.rectTransform.sizeDelta = new Vector2(0.3f, 0.036f);
            board.gameObject.SetActive(false);
        }

        /// <summary>The card with the rider's digit (v6), shot to send it into the network: a 7×7 of tiles on a frame.</summary>
        private static void BuildSendBoard(Transform table)
        {
            var board = Make("SendBoard", table, BoardCentre + BoardLift).transform;
            var card = Make("Card", board, new Vector3(0f, 0.02f, -0.006f)).transform;
            Quad("Frame", card, new Vector3(0, 0, 0.002f), new Vector2(0.26f, 0.26f), "TableFrame");
            var tiles = new TileMesh();
            const float pitch = 0.034f;
            for (int i = 0; i < 49; i++) tiles.Face(new Vector3((i % 7 - 3f) * pitch, (3f - i / 7) * pitch, -0.002f), pitch * 0.92f, pitch * 0.92f, new Color(0.04f, 0.06f, 0.12f, 0.9f));
            TileObject("Tiles", card, tiles, "Laser");
            var c = card.gameObject.AddComponent<BoxCollider>();
            c.size = new Vector3(0.26f, 0.26f, 0.04f);
            c.isTrigger = true;
            Text("Shoot", board, new Vector3(0, -0.135f, -0.006f), "SHOOT YOUR 3 TO SEND IT IN", 0.022f, _theme.gold, 0.6f, bold: true).rectTransform.sizeDelta = new Vector2(0.6f, 0.03f);
            board.gameObject.SetActive(false);
        }

        /// <summary>The step-size buttons' labels (the learning rates the bake checked: tiny, good, huge).</summary>
        private static readonly string[] StepLabels = { "SMALL\n0.15", "MEDIUM\n4", "HUGE\n40" };
        private static readonly Vector2 PlotSize = new Vector2(0.36f, 0.2f);
        private static readonly Vector2 PlotCentre = new Vector2(-0.1f, 0.0f);

        /// <summary>
        /// The training board (v6): a plot (the loss curve, the accuracy curve, the target line, a ball at the head of the
        /// loss) and the three step sizes (the learning rate) as hold buttons. One tile mesh: the three buttons first, then
        /// the target line and the plot's axes (TrainBoardView colours the buttons by index).
        /// </summary>
        private static void BuildTrainBoard(Transform table)
        {
            var board = Make("TrainBoard", table, BoardCentre + BoardLift).transform;
            Quad("PlotPlate", board, new Vector3(PlotCentre.x, PlotCentre.y, -0.004f), PlotSize + new Vector2(0.03f, 0.03f), "PlateSolid");
            BoardLine("LossCurve", board, 0, 0.005f, _theme.orange);
            BoardLine("AccuracyCurve", board, 0, 0.0035f, _theme.cyan);
            Prim(PrimitiveType.Sphere, "Ball", board, new Vector3(PlotCentre.x - PlotSize.x * 0.5f, 0, -0.012f), Vector3.one * 0.022f, "SumFires");

            var tiles = new TileMesh();
            for (int b = 0; b < StepLabels.Length; b++)
            {
                BoardButton(tiles, board, "StepButton" + b, new Vector3(0.225f, 0.082f - b * 0.082f, -0.006f), new Vector2(0.15f, 0.07f), StepLabels[b]);
            }

            float target = (float)_data.File.live.targetAccuracy;
            float left = PlotCentre.x - PlotSize.x * 0.5f, bottom = PlotCentre.y - PlotSize.y * 0.5f;
            tiles.Face(new Vector3(PlotCentre.x, bottom + PlotSize.y * target, -0.006f), PlotSize.x, 0.0025f, new Color(0.45f, 1f, 0.5f, 0.9f));
            tiles.Face(new Vector3(PlotCentre.x, bottom, -0.006f), PlotSize.x, 0.002f, new Color(1f, 1f, 1f, 0.5f));
            tiles.Face(new Vector3(left, PlotCentre.y, -0.006f), 0.002f, PlotSize.y, new Color(1f, 1f, 1f, 0.5f));
            TileObject("Tiles", board, tiles, "Laser");

            // The labels sit above the plot's plate, so the loss spiking off the top never runs through them.
            float labels = PlotCentre.y + PlotSize.y * 0.5f + 0.025f;
            Text("LossLabel", board, new Vector3(left + 0.04f, labels, -0.006f), "LOSS", 0.024f, _theme.orange, 0.1f, TextAlignmentOptions.Left, true);
            Text("AccuracyLabel", board, new Vector3(left + 0.17f, labels, -0.006f), "ACCURACY", 0.024f, _theme.cyan, 0.17f, TextAlignmentOptions.Left, true);
            Text("TargetLabel", board, new Vector3(left + PlotSize.x - 0.03f, bottom + PlotSize.y * target + 0.012f, -0.006f), Mathf.RoundToInt(target * 100f) + "%", 0.022f, new Color(0.45f, 1f, 0.5f), 0.07f, bold: true);
            Text("RateLabel", board, new Vector3(0.225f, 0.14f, -0.006f), "LEARNING RATE", 0.024f, Color.white, 0.26f, bold: true);
            // The empty plot says what to do; TrainBoardView hides it at the first step.
            TextMeshPro hint = Text("Hint", board, new Vector3(PlotCentre.x, PlotCentre.y, -0.008f), "HOLD A STEP SIZE\nTO START TRAINING", 0.03f, Color.white, PlotSize.x, bold: true);
            hint.rectTransform.sizeDelta = new Vector2(PlotSize.x, 0.09f);
            board.gameObject.SetActive(false);
        }

        /// <summary>"STOP CLEAR!" with three stars and confetti, 1.25 m ahead a little above the eye. Additive only: it never blocks a sight line.</summary>
        private static void BuildStamp(Transform pod)
        {
            var stamp = Make("Stamp", pod, new Vector3(0f, 1.52f, 1.25f));
            Quad("Burst", stamp.transform, new Vector3(0, -0.02f, 0.02f), new Vector2(1.1f, 0.55f), "GlowGold");
            TextMeshPro text = Text("Text", stamp.transform, new Vector3(0, 0.05f, 0), "STOP CLEAR!", 0.12f, _theme.gold, 1.2f, bold: true);
            text.rectTransform.sizeDelta = new Vector2(1.2f, 0.16f);
            Mesh star = SaveMesh(StarMesh(0.05f, 0.022f), "StampStar");
            for (int i = 0; i < 3; i++)
            {
                var s = AddMeshObject("Star" + i, stamp.transform, star, "StarDim");
                s.transform.localPosition = new Vector3((i - 1) * 0.13f, -0.1f, -0.005f);
            }

            var confetti = Make("Confetti", stamp.transform, new Vector3(0, 0.05f, 0.05f)).AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = confetti.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            main.startColor = new ParticleSystem.MinMaxGradient(new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(_theme.gold, 0f), new GradientColorKey(_theme.right, 0.33f),
                    new GradientColorKey(_theme.cyan, 0.66f), new GradientColorKey(_theme.valleyPink, 1f),
                },
                alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) },
                mode = GradientMode.Fixed,
            }) { mode = ParticleSystemGradientMode.RandomColor };
            ParticleSystem.EmissionModule emission = confetti.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 90) });
            ParticleSystem.ShapeModule shape = confetti.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            shape.radius = 0.2f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            ParticleSystem.RotationOverLifetimeModule spin = confetti.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            var pr = confetti.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = Mat("Confetti");
            pr.shadowCastingMode = ShadowCastingMode.Off;
            pr.receiveShadows = false;

            TextMeshPro popup = Text("Popup", pod, new Vector3(0, 1.2f, 0.7f), "+10", 0.035f, _theme.gold, 0.3f, bold: true);
            popup.gameObject.AddComponent<FaceCamera>();
            popup.gameObject.SetActive(false);
            stamp.SetActive(false);
        }

        /// <summary>A flat five-pointed star facing -Z (the rider).</summary>
        private static Mesh StarMesh(float outer, float inner)
        {
            var vertices = new List<Vector3> { Vector3.zero };
            var triangles = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? outer : inner;
                vertices.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
            }

            for (int i = 0; i < 10; i++) triangles.AddRange(new[] { 0, 1 + (i + 1) % 10, 1 + i });
            var mesh = new Mesh { name = "StampStar" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// The template target (hidden; ZapGame copies it): only a generous trigger to hit. BlasterView draws every target's
        /// handwritten digit in one batch (v7, blast the 3s).
        /// </summary>
        private static ZapSpeck BuildSpeckTemplate(Transform parent)
        {
            var speck = Make("Target", parent, Vector3.zero);
            var c = speck.AddComponent<SphereCollider>();
            c.radius = 0.34f;
            c.isTrigger = true;
            var s = speck.AddComponent<ZapSpeck>();
            speck.SetActive(false);
            return s;
        }

        /// <summary>Blast the 3s, seen (v7): one batch of digit pictures for the whole target pool, a bolt per hand, a pixel burst.</summary>
        private static void BuildBlaster(GameObject pod, ZapGame zap, int pictureCount)
        {
            int pool = new SerializedObject(zap).FindProperty("poolSize").intValue;
            Transform root = Make("Blaster", pod.transform, Vector3.zero).transform;
            var shapes = new List<Shape>();
            for (int i = 0; i < pool; i++) shapes.Add(new Shape { kind = Shape.Kind.Quad, size = Vector2.one * 0.6f, color = Color.white, alpha = 0f });
            ShapeBatch pictures = Batch("Targets", root, shapes, "ExplainerTargets", 8);
            pictures.GetComponent<Renderer>().enabled = false; // BlasterView turns it on while digits fly

            var bolts = new List<Object>();
            foreach (string hand in new[] { "Left", "Right" })
            {
                var line = Make("Bolt" + hand, root, Vector3.zero).AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.sharedMaterial = Mat("Laser");
                line.startWidth = 0.035f;
                line.endWidth = 0.012f;
                line.startColor = Color.white;
                line.endColor = new Color(1f, 0.85f, 0.45f, 1f);
                line.numCapVertices = 3;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.enabled = false;
                bolts.Add(line);
            }

            var burst = Make("Burst", root, Vector3.zero).AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = burst.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            main.gravityModifier = 0.25f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 240;
            ParticleSystem.EmissionModule emission = burst.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = burst.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.22f;
            ParticleSystem.RotationOverLifetimeModule spin = burst.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
            ParticleSystem.ColorOverLifetimeModule fade = burst.colorOverLifetime;
            fade.enabled = true;
            var ramp = new Gradient();
            ramp.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            fade.color = ramp;
            var pr = burst.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = Mat("Confetti");
            pr.shadowCastingMode = ShadowCastingMode.Off;
            pr.receiveShadows = false;
            pr.enabled = false; // on while a burst flies

            var view = pod.AddComponent<BlasterView>();
            Ref(view, "zap", zap);
            Ref(view, "pictures", pictures);
            Set(view, "cells", p =>
            {
                p.arraySize = pictureCount;
                for (int i = 0; i < pictureCount; i++) p.GetArrayElementAtIndex(i).rectValue = TargetRect(i);
            });
            Refs(view, "bolts", bolts);
            Ref(view, "burst", burst);
            Color gold = _theme.gold, red = _theme.explainRed;
            Set(view, "right", p => p.colorValue = gold);
            Set(view, "wrong", p => p.colorValue = red);
        }

        // ---------- scene wiring (pod instance) ----------

        private static void WireGames(GameObject pod, RideDirector rd, Moments m, GameObject rig)
        {
            Transform table = pod.transform.Find("GameTable");

            var mirror = pod.AddComponent<SubtitleMirror>();
            Ref(mirror, "source", pod.transform.Find("Dash/Subtitle").GetComponent<TextMeshPro>());
            Ref(mirror, "target", table.Find("Subtitle").GetComponent<TextMeshPro>());

            var tableView = pod.AddComponent<GameTableView>();
            Ref(tableView, "director", rd);
            Ref(tableView, "table", table);
            Ref(tableView, "frame", table.Find("Frame").GetComponent<Renderer>());
            Ref(tableView, "frameLive", Mat("Orange"));
            Ref(tableView, "frameIdle", Mat("TableFrame"));

            // The weight grid: the paint game through its router (sprayed cell by cell by a held laser, v6).
            var gridInput = m.Paint.gameObject.AddComponent<DashGridInput>();
            Ref(gridInput, "paint", m.Paint);
            // The cells are part of the Pod prefab, so the router goes in through the serialized field (which records the scene
            // override); assigning it in code on the instance would be lost when the scene is saved.
            foreach (WeightCellInteractable cell in table.GetComponentsInChildren<WeightCellInteractable>(true)) Ref(cell, "grid", gridInput);

            // The drawing board: draw your 3, and the bonus round. The canvas routes a held laser to whichever game is live.
            Transform draw = table.Find("DrawBoard");
            var canvasInput = draw.Find("Canvas").gameObject.AddComponent<CanvasInput>();
            Refs(canvasInput, "games", new List<Object> { m.Draw, m.Stump });
            foreach (var (name, index) in new[] { ("Done", DrawMoment.ButtonDone), ("Clear", DrawMoment.ButtonClear) })
            {
                var button = draw.Find(name).gameObject.AddComponent<GameButton>();
                Refs(button, "games", new List<Object> { m.Draw, m.Stump });
                Int(button, "button", index);
            }
            var drawView = pod.AddComponent<DrawBoardView>();
            Ref(drawView, "director", rd);
            Ref(drawView, "draw", m.Draw);
            Ref(drawView, "stump", m.Stump);
            Ref(drawView, "board", draw.gameObject);
            Int(drawView, "drawSegment", SegPixels);
            Int(drawView, "stumpSegment", SegStump);
            Ref(drawView, "ink", draw.Find("Ink").GetComponent<MeshFilter>());
            Float(drawView, "canvasSize", CanvasSide);
            Ref(drawView, "tiles", draw.Find("Tiles").GetComponent<MeshFilter>());
            Ref(drawView, "prompt", draw.Find("Prompt").GetComponent<TextMeshPro>());
            Ref(drawView, "count", draw.Find("Count").GetComponent<TextMeshPro>());
            Ref(drawView, "answer", draw.Find("Answer").GetComponent<TextMeshPro>());

            // The card the rider shoots to send their 3 into the network.
            Transform send = table.Find("SendBoard");
            var sendButton = send.Find("Card").gameObject.AddComponent<GameButton>();
            Refs(sendButton, "games", new List<Object> { m.Forward });
            var sendView = pod.AddComponent<SendCardView>();
            Ref(sendView, "director", rd);
            Ref(sendView, "session", _session);
            Ref(sendView, "moment", m.Forward);
            Ref(sendView, "board", send.gameObject);
            Int(sendView, "showSegment", SegNetworkSend);
            Ref(sendView, "card", send.Find("Card"));
            Ref(sendView, "tiles", send.Find("Card/Tiles").GetComponent<MeshFilter>());
            Set(tableView, "alsoUpAt", p =>
            {
                p.arraySize = 1;
                p.GetArrayElementAtIndex(0).intValue = SegNetworkSend;
            });

            // The training board.
            Transform train = table.Find("TrainBoard");
            var trainView = pod.AddComponent<TrainBoardView>();
            Ref(trainView, "director", rd);
            Ref(trainView, "game", m.Train);
            Ref(trainView, "board", train.gameObject);
            Int(trainView, "showSegment", SegRim);
            Ref(trainView, "lossCurve", train.Find("LossCurve").GetComponent<LineRenderer>());
            Ref(trainView, "accuracyCurve", train.Find("AccuracyCurve").GetComponent<LineRenderer>());
            Ref(trainView, "ball", train.Find("Ball"));
            Ref(trainView, "tiles", train.Find("Tiles").GetComponent<MeshFilter>());
            Ref(trainView, "hint", train.Find("Hint").gameObject);
            Set(trainView, "plotSize", p => p.vector2Value = PlotSize);
            Set(trainView, "plotCentre", p => p.vector2Value = PlotCentre);
            for (int b = 0; b < TrainMoment.ButtonCount; b++)
            {
                var step = train.Find("StepButton" + b).gameObject.AddComponent<GameButton>();
                Refs(step, "games", new List<Object> { m.Train });
                Int(step, "button", b);
            }

            // The hunt's card on the table while the wires are live.
            var hunt = table.Find("HuntBoard").gameObject;
            var huntShow = pod.AddComponent<CueShowView>();
            Ref(huntShow, "director", rd);
            Ref(huntShow, "target", hunt);
            Int(huntShow, "segment", SegRewind);

            // The zapper between the stops.
            var zap = rd.gameObject.AddComponent<ZapGame>();
            Ref(zap, "director", rd);
            Ref(zap, "template", BuildSpeckTemplate(pod.transform));
            Level1Picture[] targets = _data.File.targets;
            Set(zap, "labels", p =>
            {
                p.arraySize = targets.Length;
                for (int i = 0; i < targets.Length; i++) p.GetArrayElementAtIndex(i).intValue = targets[i].label;
            });
            Int(zap, "targetLabel", Level1Data.HeroDigit);
            BuildBlaster(pod, zap, targets.Length);

            // HUD and stamp.
            var hud = pod.AddComponent<GameHudView>();
            Transform stamp = pod.transform.Find("Stamp");
            Ref(hud, "director", rd);
            Ref(hud, "title", table.Find("Title").GetComponent<TextMeshPro>());
            Ref(hud, "goal", table.Find("Goal").GetComponent<TextMeshPro>());
            Ref(hud, "timer", table.Find("Timer").GetComponent<TextMeshPro>());
            Ref(hud, "score", table.Find("Score").GetComponent<TextMeshPro>());
            Ref(hud, "progressFill", table.Find("ProgressFill"));
            Ref(hud, "stamp", stamp.gameObject);
            Ref(hud, "stampText", stamp.Find("Text").GetComponent<TextMeshPro>());
            var stars = new List<Object>();
            for (int i = 0; i < 3; i++) stars.Add(stamp.Find("Star" + i).GetComponent<Renderer>());
            Refs(hud, "stars", stars);
            Ref(hud, "starLit", Mat("StarLit"));
            Ref(hud, "starDim", Mat("StarDim"));
            Ref(hud, "confetti", stamp.Find("Confetti").GetComponent<ParticleSystem>());
            Ref(hud, "popup", pod.transform.Find("Popup").GetComponent<TextMeshPro>());
            Ref(hud, "popupAnchor", table.Find("PopupAnchor"));
            Ref(hud, "zap", zap);

            var sfx = pod.GetComponent<RideSfxView>();
            Ref(sfx, "zap", zap);
            Ref(sfx, "train", m.Train);
            Ref(sfx, "stump", m.Stump);
            Ref(sfx, "send", m.Forward);

            // A laser on each hand. The rig's own near-far ray visuals are turned off so there is one beam per hand.
            Transform offset = rig.transform.Find("Camera Offset");
            var lasers = rig.AddComponent<RideHandLasers>();
            Ref(lasers, "leftHand", offset.Find("Left Controller"));
            Ref(lasers, "rightHand", offset.Find("Right Controller"));
            Ref(lasers, "laserMaterial", Mat("Laser"));

            // Each hand wears its polarity (right = + green, left = − red) on a fingertip orb, and its fingertip touches cells.
            foreach (var (side, hand) in new[] { ("Left Controller", RideHand.Left), ("Right Controller", RideHand.Right) })
            {
                var polarity = offset.Find(side).gameObject.AddComponent<HandPolarityController>();
                Int(polarity, "hand", hand);
                Ref(polarity, "orbMaterial", Mat("Laser"));
            }
            foreach (string side in new[] { "Left Controller", "Right Controller" })
            {
                Transform nearFar = offset.Find(side + "/Near-Far Interactor");
                if (nearFar == null) continue;
                foreach (LineRenderer line in nearFar.GetComponentsInChildren<LineRenderer>(true)) line.enabled = false;
                foreach (MonoBehaviour b in nearFar.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (b.GetType().Name == "CurveVisualController") b.enabled = false;
                }
            }
        }

        /// <summary>The game titles, goals, win lines and how long each waits before it helps itself.</summary>
        private static void ConfigureGames(Moments m)
        {
            void Game(RideGame g, string title, string goal, RideLine win, float giveUp)
            {
                Set(g, "title", p => p.stringValue = title);
                Set(g, "goal", p => p.stringValue = goal);
                Set(g, "winLine", p => p.intValue = (int)win);
                Set(g, "giveUpAfter", p => p.floatValue = giveUp);
            }

            Game(m.Draw, "DRAW YOUR 3", "Hold the trigger and draw a 3", RideLine.DrawDone, 90f);
            Game(m.Paint, "TEACH ONE NEURON", "Fire for 3s only. Right hand +, left hand -", RideLine.NeuronSolved, 120f);
            Game(m.Blame, "HOT-WIRE HUNT", "Drag each hot wire to the 3 before it cools", RideLine.BackDone, 60f);
            Game(m.Train, "TRAIN PIXEL", "Hold a step size to train", RideLine.TrainDone, 90f);
            Game(m.Stump, "STUMP PIXEL", "Draw the digit Pixel asks for", RideLine.StumpDone, 75f);
            // The plunge after the training game is a quick victory ride.
            Set(m.Valley, "hopSeconds", p => p.floatValue = 0.75f);
            Set(m.Valley, "pauseSeconds", p => p.floatValue = 0.1f);
        }
    }
}
