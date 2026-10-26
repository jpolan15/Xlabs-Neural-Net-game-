using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;
using Convergence.Presentation.Ride;
using Convergence.XR.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// Level 1 v5's games (TASK_LEVEL1_V5_GAMEPLAY): the game table every mini-game is played on (it rises out of the
    /// dash to tablet height), its boards (the 7×7 grid, the plug board, the descent hill), the HUD (title, goal,
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
        private static readonly string[] StepLabels = { "TINY STEP", "MEDIUM STEP", "GIANT LEAP" };

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
            TextMeshPro title = Text("Title", table, new Vector3(-0.09f, top - 0.088f, -0.006f), "DRAW THE 3", 0.039f, _theme.gold, 0.46f, TextAlignmentOptions.Left, true);
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
            BuildPlugBoard(table);
            BuildDescentBoard(table);

            var shoot = Make("HuntBoard", table, BoardCentre).transform;
            Text("Shoot", shoot, new Vector3(0, 0.03f, -0.006f), "DRAG HOT WIRES\nTO THE 3!", 0.05f, _theme.rewindMagenta, 0.6f, bold: true).rectTransform.sizeDelta = new Vector2(0.6f, 0.16f);
            Text("Up", shoot, new Vector3(0, -0.1f, -0.006f), "look up: grab a glowing wire end, drag it to the 3", 0.02f, Color.white, 0.6f);
            shoot.gameObject.SetActive(false);
            return table;
        }

        /// <summary>Glow tiles of the plug board: six string heads, then ten sockets, then the gold goal ring (four bars).</summary>
        public const int PlugHeadTiles = 6, PlugSocketTiles = 10;

        /// <summary>
        /// The plug board, cheap to draw: one mesh of glow tiles (string heads, sockets, the goal ring), the digits in two
        /// text columns, and one line per string. Each socket's tile and collider sit on its digit, measured from the text.
        /// </summary>
        private static void BuildPlugBoard(Transform table)
        {
            var board = Make("PlugBoard", table, BoardCentre).transform;
            var tiles = new TileMesh();
            for (int s = 0; s < PlugHeadTiles; s++)
            {
                Vector3 at = new Vector3(-0.24f, 0.11f - s * 0.046f, -0.008f);
                tiles.Face(at, 0.034f, 0.034f, new Color(0.4f, 0.9f, 1f, 0.5f));
                var head = Make("Head" + s, board, at);
                var c = head.AddComponent<SphereCollider>();
                c.radius = 0.026f;
                c.isTrigger = true;
                var line = Make("String", head.transform, Vector3.zero).AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 12;
                line.widthMultiplier = 0.005f;
                line.numCapVertices = 2;
                line.sharedMaterial = Mat("Laser");
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
            }

            var socketAt = new Vector3[10];
            for (int col = 0; col < 2; col++)
            {
                var digits = Text("Digits" + col, board, new Vector3(0.13f + col * 0.13f, -0.005f, -0.01f), "", 0.03f, Color.white, 0.06f, bold: true);
                digits.rectTransform.sizeDelta = new Vector2(0.06f, 0.3f);
                digits.text = string.Join("\n", new[] { 0, 1, 2, 3, 4 }.Select(r => (col * 5 + r).ToString()));
                digits.lineSpacing = 22f;
                digits.ForceMeshUpdate();
                for (int r = 0; r < 5; r++)
                {
                    TMP_CharacterInfo ch = digits.textInfo.characterInfo[digits.textInfo.lineInfo[r].firstCharacterIndex];
                    Vector3 local = (ch.bottomLeft + ch.topRight) * 0.5f;
                    socketAt[col * 5 + r] = board.InverseTransformPoint(digits.transform.TransformPoint(local));
                }
            }

            for (int k = 0; k < PlugSocketTiles; k++)
            {
                Vector3 at = new Vector3(socketAt[k].x, socketAt[k].y, -0.007f);
                tiles.Face(at, 0.075f, 0.044f, new Color(0.4f, 0.85f, 1f, 0.2f));
                var socket = Make("Socket" + k, board, at);
                var c = socket.AddComponent<BoxCollider>();
                c.size = new Vector3(0.11f, 0.05f, 0.03f);
                c.isTrigger = true;
            }

            Vector3 g = new Vector3(socketAt[Level1Data.HeroDigit].x, socketAt[Level1Data.HeroDigit].y, -0.009f);
            Color gold = new Color(_theme.gold.r, _theme.gold.g, _theme.gold.b, 0.9f);
            tiles.Face(g + new Vector3(0, 0.026f, 0), 0.09f, 0.004f, gold);
            tiles.Face(g + new Vector3(0, -0.026f, 0), 0.09f, 0.004f, gold);
            tiles.Face(g + new Vector3(-0.045f, 0, 0), 0.004f, 0.052f, gold);
            tiles.Face(g + new Vector3(0.045f, 0, 0), 0.004f, 0.052f, gold);
            TileObject("Tiles", board, tiles, "Laser");
            Text("Goal3", board, g + new Vector3(-0.075f, 0, -0.002f), "GOAL", 0.016f, _theme.gold, 0.06f, bold: true);
            board.gameObject.SetActive(false);
        }

        /// <summary>Tiles of the descent board: the three step buttons, then the green bottom mark.</summary>
        public const int DescentButtonTiles = 3;

        /// <summary>The descent board, cheap to draw: the hill and the trail of steps as lines, the ball, and one mesh for the buttons and the bottom mark.</summary>
        private static void BuildDescentBoard(Transform table)
        {
            var board = Make("DescentBoard", table, BoardCentre).transform;
            LineRenderer Line(string name, int points, float width, Color color)
            {
                var line = Make(name, board, Vector3.zero).AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = points;
                line.widthMultiplier = width;
                line.sharedMaterial = Mat("Laser");
                line.startColor = line.endColor = color;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                return line;
            }

            Line("Hill", 48, 0.004f, new Color(_theme.valleyPink.r, _theme.valleyPink.g, _theme.valleyPink.b, 1f));
            Line("Trail", 0, 0.0025f, new Color(1f, 0.85f, 0.4f, 0.8f));
            Prim(PrimitiveType.Sphere, "Ball", board, new Vector3(0, 0, -0.01f), Vector3.one * 0.026f, "SumFires");
            var tiles = new TileMesh();
            for (int b = 0; b < DescentButtonTiles; b++)
            {
                Vector3 at = new Vector3(0.225f, 0.085f - b * 0.075f, -0.006f);
                tiles.Face(at, 0.17f, 0.06f, new Color(0.15f, 0.35f, 0.6f, 0.55f));
                var button = Make("StepButton" + b, board, at);
                Text("Label", button.transform, new Vector3(0, 0, -0.004f), StepLabels[b], 0.022f, b == 2 ? _theme.orange : Color.white, 0.17f, bold: true);
                var c = button.AddComponent<BoxCollider>();
                c.size = new Vector3(0.17f, 0.065f, 0.03f);
                c.isTrigger = true;
            }

            tiles.Face(new Vector3(0, -0.09f, -0.005f), 0.006f, 0.02f, new Color(0.45f, 1f, 0.5f, 0.9f));
            TileObject("Tiles", board, tiles, "Laser");
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

        /// <summary>The template noise speck (hidden; ZapGame copies it): a spinning magenta cube in a pink glow, a generous trigger to hit.</summary>
        private static ZapSpeck BuildSpeckTemplate(Transform parent)
        {
            var speck = Make("NoiseSpeck", parent, Vector3.zero);
            var body = Prim(PrimitiveType.Cube, "Body", speck.transform, Vector3.zero, Vector3.one * 0.13f, "Noise");
            body.transform.localRotation = Quaternion.Euler(30f, 45f, 20f);
            var glow = Quad("Glow", speck.transform, Vector3.zero, new Vector2(0.5f, 0.5f), "GlowPink");
            glow.AddComponent<FaceCamera>();
            var c = speck.AddComponent<SphereCollider>();
            c.radius = 0.3f;
            c.isTrigger = true;
            var s = speck.AddComponent<ZapSpeck>();
            speck.SetActive(false);
            return s;
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

            // The grid: both grid games through one router.
            var gridInput = m.Paint.gameObject.AddComponent<DashGridInput>();
            Ref(gridInput, "draw", m.Draw);
            Ref(gridInput, "paint", m.Paint);
            // The cells are part of the Pod prefab, so the router goes in through the serialized field (which records the scene
            // override); assigning it in code on the instance would be lost when the scene is saved.
            foreach (WeightCellInteractable cell in table.GetComponentsInChildren<WeightCellInteractable>(true)) Ref(cell, "grid", gridInput);

            // The plug board.
            Transform plug = table.Find("PlugBoard");
            var plugView = pod.AddComponent<PlugBoardView>();
            Ref(plugView, "director", rd);
            Ref(plugView, "game", m.Plug);
            Ref(plugView, "board", plug.gameObject);
            Int(plugView, "showSegment", SegWrong);
            Ref(plugView, "tiles", plug.Find("Tiles").GetComponent<MeshFilter>());
            Refs(plugView, "digitColumns", new List<Object> { plug.Find("Digits0").GetComponent<TextMeshPro>(), plug.Find("Digits1").GetComponent<TextMeshPro>() });
            var heads = new List<Object>();
            var strings = new List<Object>();
            for (int s = 0; s < PlugHeadTiles; s++)
            {
                Transform head = plug.Find("Head" + s);
                heads.Add(head);
                strings.Add(head.Find("String").GetComponent<LineRenderer>());
                head.gameObject.AddComponent<PlugTarget>().Set(m.Plug, false, s);
            }

            var sockets = new List<Object>();
            for (int k = 0; k < PlugSocketTiles; k++)
            {
                Transform socket = plug.Find("Socket" + k);
                sockets.Add(socket);
                socket.gameObject.AddComponent<PlugTarget>().Set(m.Plug, true, k);
            }

            Refs(plugView, "heads", heads);
            Refs(plugView, "strings", strings);
            Refs(plugView, "sockets", sockets);

            // The descent board.
            Transform descent = table.Find("DescentBoard");
            var descentView = pod.AddComponent<DescentBoardView>();
            Ref(descentView, "director", rd);
            Ref(descentView, "game", m.Descent);
            Ref(descentView, "board", descent.gameObject);
            Int(descentView, "showSegment", SegRim);
            Ref(descentView, "curve", descent.Find("Hill").GetComponent<LineRenderer>());
            Ref(descentView, "trail", descent.Find("Trail").GetComponent<LineRenderer>());
            Ref(descentView, "ball", descent.Find("Ball"));
            Ref(descentView, "tiles", descent.Find("Tiles").GetComponent<MeshFilter>());
            for (int b = 0; b < DescentButtonTiles; b++) descent.Find("StepButton" + b).gameObject.AddComponent<DescentTarget>().Set(m.Descent, b);

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
            Ref(sfx, "plug", m.Plug);
            Ref(sfx, "descent", m.Descent);

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

            Game(m.Draw, "DRAW THE 3", "Right hand draws, left hand rubs out", RideLine.DrawDone, 90f);
            Game(m.Paint, "TEACH ONE NEURON", "Light up for 3s only. Right = green, left = red", RideLine.NeuronSolved, 120f);
            Game(m.Plug, "PLUG THE STRINGS", "Make the 3 the brightest answer", RideLine.PlugDone, 90f);
            Game(m.Blame, "HOT-WIRE HUNT", "Drag each hot wire to the 3 before it cools", RideLine.BackDone, 60f);
            Game(m.Descent, "ROLL IT DOWN", "Get the ball to the bottom in few steps", RideLine.DescentRealHill, 75f);
            Set(m.Valley, "hopSeconds", p => p.floatValue = 1.3f);
            Set(m.Valley, "pauseSeconds", p => p.floatValue = 0.25f);
        }
    }
}
