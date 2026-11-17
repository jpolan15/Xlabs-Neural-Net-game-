using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 5, played: the story road. At each of three forks the road splits into four lanes, each ending in a gate wearing the word it
    /// stands for and GPT-2's real chance of writing it; a lane is as wide as its chance. The rider points at a gate and pulls the trigger;
    /// the bike steers into the chosen lane (the scenery slides sideways while the bike carries on and its handlebars turn, ADR-023), the
    /// gate flares and the word joins the sentence
    /// over the road with its surprise score. At the end the finished story is shown with how often the machine would have written
    /// exactly it. Display and the colliders the laser aims at; the rules are the moment's. v3 (owner playtest 2026-11-10): every fork says
    /// what the gates are (the machine's next-word choices), each pick is compared with the machine's favourite and the lanes not taken
    /// show where they would have led, and the end shows the machine's own favourite story and two others it could have written.
    /// </summary>
    public sealed class StoryView : MonoBehaviour
    {
        private const int Lanes = 4;
        private static readonly string[] Tags = { "FAVOURITE", "LIKELY", "SPICY", "WILD" };
        private static readonly Color[] Cols = { new Color(0.45f, 1f, 0.72f), new Color(0.3f, 0.85f, 1f), new Color(1f, 0.82f, 0.4f), new Color(1f, 0.42f, 0.72f) };
        private static readonly float[] LaneX = { -5.2f, -1.7f, 1.7f, 5.2f };

        private sealed class ForkView
        {
            public Transform Root;
            public TextMeshPro[] Word = new TextMeshPro[Lanes];
            public TextMeshPro[] Percent = new TextMeshPro[Lanes];
            public TextMeshPro[] Tag = new TextMeshPro[Lanes];
            public BoxCollider[] Box = new BoxCollider[Lanes];
            public float[] P = new float[Lanes];
            public string[] W = new string[Lanes];
            public string Path = "";
            public float[] X = new float[Lanes]; // each lane's gate position: shuffled, so the favourite is not always on the left
            public bool Open;
            public int Chosen = -1;
            public float ChosenAt = -10f;
            public float OpenedAt = -10f;
        }

        [SerializeField] private StoryMoment moment;
        [SerializeField] private Transform stage;

        private Level2Kit _kit;
        private Transform _props;
        private ForkView[] _forks;
        private RibbonField _lanes;
        private GlowField _glows, _sparks;
        private TextMeshPro _story, _banner, _popup, _final, _odds, _favourite, _favStory, _others;
        private float _favouriteAt = -10f;
        private float _popupAt = -10f;
        private bool _told;
        private float _toldAt;
        private string _toldText = "";
        private double _toldProbability;
        private readonly System.Collections.Generic.List<(Vector3 pos, Vector3 vel, float born, Color color)> _spark = new System.Collections.Generic.List<(Vector3, Vector3, float, Color)>();
        private const int SparkCount = 80;
        private Camera _camera;

        private float ForkZ(int f) => f * moment.ForkSpacing + 15f;

        private void Awake()
        {
            _kit = Level2Kit.Current;
            if (_kit == null || moment == null) return;
            _props = new GameObject("StoryProps").transform;
            _props.SetParent(stage != null ? stage : transform, false);
            int forks = 3;
            _forks = new ForkView[forks];
            for (int f = 0; f < forks; f++)
            {
                var root = new GameObject("Fork" + f).transform;
                root.SetParent(_props, false);
                root.localPosition = new Vector3(0f, 0f, ForkZ(f));
                var v = new ForkView { Root = root };
                for (int l = 0; l < Lanes; l++)
                {
                    var gate = new GameObject("Gate" + l).transform;
                    gate.SetParent(root, false);
                    gate.localPosition = new Vector3(LaneX[l], 1.7f, 0f);
                    v.Box[l] = gate.gameObject.AddComponent<BoxCollider>();
                    v.Box[l].isTrigger = true;
                    v.Box[l].size = new Vector3(3.0f, 3.4f, 1.2f);
                    var pick = gate.gameObject.AddComponent<PickTarget>();
                    pick.Owner = moment;
                    pick.Index = l;
                    v.Word[l] = _kit.Text(gate, "Word", "", 0.62f, Color.white, new Vector3(0f, 0.25f, -0.2f), 3.4f);
                    v.Percent[l] = _kit.Text(gate, "Percent", "", 0.34f, Cols[l], new Vector3(0f, -0.5f, -0.2f), 3f);
                    v.Tag[l] = _kit.Text(gate, "Tag", Tags[l], 0.24f, Cols[l], new Vector3(0f, 1.35f, -0.2f), 3f);
                }

                _forks[f] = v;
                root.gameObject.SetActive(false);
            }

            _lanes = RibbonField.Create(_props, "Lanes", forks * (Lanes + 1), _kit.Beam, -1);
            _glows = GlowField.Create(_props, "Gates", forks * Lanes * 2, _kit.Ring, 1);
            _sparks = GlowField.Create(_props, "Sparks", SparkCount, _kit.Glow, 4);
            _story = _kit.Text(_props, "Story", "", 0.62f, Color.white, new Vector3(0f, 5.6f, 14f), 14f);
            _banner = _kit.Text(_props, "Banner", "POINT AT A WORD AND PULL THE TRIGGER TO RIDE THAT WAY  -  SAFE IS EASY, WILD SCORES BIG", 0.28f, new Color(1f, 0.92f, 0.65f), new Vector3(0f, 4.55f, 13f), 14f);
            _popup = _kit.Text(_props, "Popup", "", 0.5f, new Color(1f, 0.9f, 0.5f), new Vector3(0f, 3.2f, 6f), 8f);
            _final = _kit.Text(_props, "Final", "", 0.56f, Color.white, new Vector3(0f, 4.0f, 10f), 13f);
            _odds = _kit.Text(_props, "Odds", "", 0.3f, new Color(1f, 0.85f, 0.5f), new Vector3(0f, 2.9f, 10f), 13f);
            _favourite = _kit.Text(_props, "Favourite", "", 0.36f, new Color(0.45f, 1f, 0.72f), new Vector3(0f, 2.4f, 8f), 12f);
            _favStory = _kit.Text(_props, "FavStory", "", 0.36f, new Color(0.45f, 1f, 0.72f), new Vector3(0f, 3.4f, 9f), 13f);
            _others = _kit.Text(_props, "Others", "", 0.3f, new Color(0.9f, 0.93f, 1f), new Vector3(0f, 2.3f, 9f), 13f);

            moment.ForkOpened += OnForkOpened;
            moment.Chosen += OnChosen;
            moment.Told += OnTold;
            _props.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (moment == null) return;
            moment.ForkOpened -= OnForkOpened;
            moment.Chosen -= OnChosen;
            moment.Told -= OnTold;
        }

        private void OnForkOpened(int fork, L2StoryNode node)
        {
            ForkView v = _forks[fork];
            v.Open = true;
            v.Chosen = -1;
            v.OpenedAt = Time.time;
            v.Root.gameObject.SetActive(true);
            v.Root.localPosition = new Vector3(0f, 0f, ForkZ(fork));
            v.Path = moment.Path;
            for (int l = 0; l < Lanes; l++)
            {
                v.W[l] = Level2Data.Clean(node.lanes[l].word);
                v.Word[l].text = v.W[l];
                v.Word[l].color = Color.white;
                v.P[l] = node.lanes[l].p;
                v.Percent[l].text = FormatPercent(node.lanes[l].p);
                v.Box[l].enabled = true;
            }

            // Guess first, odds after (owner playtest 2026-11-10): shuffle the gates; their odds and tags show only once the rider picks.
            var order = new System.Collections.Generic.List<int> { 0, 1, 2, 3 };
            var rng = new System.Random(17 + fork * 31 + v.Path.Length * 7 + (v.Path.Length > 0 ? v.Path[v.Path.Length - 1] : 0));
            for (int k = order.Count - 1; k > 0; k--) { int j = rng.Next(k + 1); (order[k], order[j]) = (order[j], order[k]); }
            for (int l = 0; l < Lanes; l++)
            {
                v.X[l] = LaneX[order[l]];
                v.Root.GetChild(l).localPosition = new Vector3(v.X[l], 1.7f, 0f);
            }

            _popup.text = "";
        }

        private static string FormatPercent(float p)
        {
            float pct = p * 100f;
            return pct >= 10f ? Mathf.RoundToInt(pct) + "%" : pct.ToString("0.0") + "%";
        }

        private void OnChosen(int fork, int lane, string word, float p, int points)
        {
            ForkView v = _forks[fork];
            v.Chosen = lane;
            v.ChosenAt = Time.time;
            for (int l = 0; l < Lanes; l++) v.Box[l].enabled = false;
            _popup.text = "+" + points + "   " + (p < 0.03f ? "LONG SHOT!" : p < 0.1f ? "SPICY" : p < 0.25f ? "NICE" : "SAFE");
            _popup.color = Cols[Mathf.Clamp(lane, 0, Lanes - 1)];
            _popupAt = Time.time;

            // Your word against the machine's favourite (lane 0), and where each lane not taken would have gone next.
            _favourite.text = lane == 0
                ? "YOU GUESSED THE MACHINE'S FAVOURITE:  \"" + v.W[0] + "\"  " + FormatPercent(v.P[0])
                : "YOU:  \"" + word + "\"  " + FormatPercent(p) + "      THE MACHINE'S FAVOURITE:  \"" + v.W[0] + "\"  " + FormatPercent(v.P[0]);
            _favouriteAt = Time.time;
            for (int l = 0; l < Lanes; l++)
            {
                if (l == lane) continue;
                L2StoryNode next = moment.NodeAt(v.Path + l);
                if (next != null) v.Percent[l].text = FormatPercent(v.P[l]) + "\nthen: " + Level2Data.Clean(next.lanes[0].word) + " ...";
            }
            Vector3 at = v.Root.localPosition + new Vector3(v.X[lane], 1.7f, 0f);
            for (int k = 0; k < 26; k++)
            {
                float a = k / 26f * Mathf.PI * 2f;
                _spark.Add((at, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.2f) * (2f + 3f * Fx.Rand01(k)), Time.time, Cols[lane]));
            }
        }

        private void OnTold(string text, double bits, double probability)
        {
            _told = true;
            _toldAt = Time.time;
            _toldText = text;
            _toldProbability = probability;
        }

        private Vector3 EyeLocal()
        {
            if (_camera == null) _camera = Camera.main;
            return _camera != null ? _props.InverseTransformPoint(_camera.transform.position) : new Vector3(0f, 1.3f, 0f);
        }

        private void Update()
        {
            if (_props == null) return;
            bool running = moment.IsRunning;
            if (_props.gameObject.activeSelf != running) _props.gameObject.SetActive(running);
            if (!running)
            {
                _told = false;
                return;
            }

            Vector3 eye = EyeLocal();
            Vector3 eyeWorld = _props.TransformPoint(eye);
            _lanes.HideAll();
            _glows.HideAll();
            int g = 0;

            for (int f = 0; f < _forks.Length; f++)
            {
                ForkView v = _forks[f];
                if (!v.Open) continue;
                float since = Time.time - v.OpenedAt;
                float appear = Mathf.Clamp01(since / 0.7f);

                // The chosen lane slides under the pod: the whole fork drifts sideways until the gate is dead ahead.
                float slide = 0f;
                if (v.Chosen >= 0)
                {
                    float turn = Mathf.Clamp01((Time.time - v.ChosenAt) / 2.4f);
                    slide = -v.X[v.Chosen] * Fx.Smooth(turn);
                    // The handlebars turn toward the lane while the bike swings into it, and straighten as it lines up.
                    if (turn < 1f && BikeView.Current != null) BikeView.Current.Steer(v.X[v.Chosen] / LaneX[Lanes - 1] * Mathf.Sin(turn * Mathf.PI));
                }
                v.Root.localPosition = new Vector3(slide, 0f, ForkZ(f));

                // The road behind the gates splits into four lanes as wide as their chances.
                _lanes.Set(f * (Lanes + 1), new Vector3(0f, 0.03f, -18f), new Vector3(0f, 0.03f, -9f), 0f, 2.6f, new Color(1f, 0.85f, 0.45f, 0.5f * appear));
                for (int l = 0; l < Lanes; l++)
                {
                    // Before the pick every lane looks the same; after it, each widens to its real chance.
                    float reveal = v.Chosen >= 0 ? Fx.Smooth(Mathf.Clamp01((Time.time - v.ChosenAt) / 0.6f)) : 0f;
                    float w = Mathf.Lerp(1.4f, 0.22f + 3.1f * Mathf.Sqrt(v.P[l]), reveal);
                    int slot = System.Array.IndexOf(LaneX, v.X[l]);
                    bool chosen = v.Chosen == l;
                    bool faded = v.Chosen >= 0 && !chosen;
                    float fade = faded ? Mathf.Clamp01(1f - (Time.time - v.ChosenAt - 1.6f) / 1.2f) : 1f;
                    // The chosen word has joined the sentence overhead: the gate lets go of it before the pod drives through, so it never fills the view.
                    float gone = chosen ? Fx.Smooth(Mathf.Clamp01((Time.time - v.ChosenAt - 0.9f) / 0.7f)) : 0f;
                    Color c = Cols[reveal > 0.5f ? l : slot];
                    c.a = (chosen ? 0.9f : 0.55f) * appear * fade;
                    _lanes.Set(f * (Lanes + 1) + 1 + l, new Vector3(0f, 0.04f, ForkZ(f) - 9f), new Vector3(slide + v.X[l], 0.04f, ForkZ(f)), 0f, w, c);

                    Transform gate = v.Root.GetChild(l);
                    Level2Scenery.FaceEye(gate, eyeWorld, true);
                    gate.localScale = Vector3.one * (chosen ? (1f + 0.35f * Fx.Smooth(Mathf.Clamp01((Time.time - v.ChosenAt) / 0.5f))) * (1f - 0.6f * gone) : Fx.Pop(appear));
                    float vis = fade * appear * (1f - gone);
                    SetAlpha(v.Word[l], vis);
                    SetAlpha(v.Percent[l], vis * reveal);
                    SetAlpha(v.Tag[l], vis * reveal);
                    v.Word[l].gameObject.SetActive(vis > 0.02f);
                    v.Percent[l].gameObject.SetActive(vis * reveal > 0.02f);
                    v.Tag[l].gameObject.SetActive(vis * reveal > 0.02f);
                    float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 4f + l);
                    Vector3 gp = gate.localPosition + v.Root.localPosition;
                    _glows.Set(g++, gp + new Vector3(0f, 0f, 0.15f), new Vector2(3.3f, 3.3f) * (chosen ? 1.4f : 1f), Fx.A(Cols[reveal > 0.5f ? l : slot], (chosen ? 0.95f : 0.6f * pulse) * vis), Time.time * 0.15f * (l % 2 == 0 ? 1f : -1f));
                    _glows.Set(g++, gp + new Vector3(0f, 0f, 0.2f), new Vector2(2.4f, 2.4f), Fx.A(Cols[reveal > 0.5f ? l : slot], 0.12f * vis));
                }

                // Gates already passed are put away.
                if (v.Chosen >= 0 && eye.z > ForkZ(f) + 2.5f) v.Root.gameObject.SetActive(false);
            }

            // The sentence over the road, and the surprise of the last word.
            bool going = moment.IsLive || moment.Stage == StoryMoment.Phase.Compare || moment.Stage == StoryMoment.Phase.Travelling;
            _story.text = going ? moment.SoFar + "  ..." : moment.Stage == StoryMoment.Phase.Reveal ? moment.FinishedText : _story.text;
            float favAge = Time.time - _favouriteAt;
            _favourite.gameObject.SetActive(favAge < 4.2f && !_told);
            _favourite.transform.localPosition = new Vector3(0f, 2.5f, eye.z + 8.5f);
            SetAlpha(_favourite, Mathf.Clamp01(Mathf.Min(favAge / 0.3f, (4.2f - favAge) / 0.6f)));
            Level2Scenery.FaceEye(_favourite.transform, eyeWorld, false);
            _story.transform.localPosition = new Vector3(0f, 5.5f, eye.z + 12f);
            Level2Scenery.FaceEye(_story.transform, eyeWorld, false);
            _banner.gameObject.SetActive(moment.IsLive);
            _banner.text = moment.Fork == 0
                ? "EACH GATE IS A WORD GPT-2 COULD WRITE NEXT  -  GUESS WHICH ONE IT LIKES BEST, THEN SEE ITS REAL ODDS"
                : "FORK " + (moment.Fork + 1) + " OF " + moment.ForkCount + ":  GUESS THE MACHINE'S FAVOURITE NEXT WORD";
            _banner.transform.localPosition = new Vector3(0f, 4.55f, eye.z + 11.5f);
            float popAge = Time.time - _popupAt;
            _popup.gameObject.SetActive(popAge < 1.6f);
            _popup.transform.localPosition = new Vector3(0f, 3.3f + popAge * 0.5f, eye.z + 8f);
            SetAlpha(_popup, Mathf.Clamp01(1.6f - popAge));
            Level2Scenery.FaceEye(_popup.transform, eyeWorld, false);

            if (_told)
            {
                float k = Mathf.Clamp01((Time.time - _toldAt) / 0.8f);
                _final.gameObject.SetActive(true);
                _odds.gameObject.SetActive(true);
                _final.text = "\"" + _toldText + "\"";
                _final.transform.localPosition = new Vector3(0f, 5.4f, eye.z + 9f);
                _final.transform.localScale = Vector3.one * Fx.Pop(k);
                _odds.text = "THE MACHINE WOULD WRITE EXACTLY THIS ABOUT 1 IN " + FormatBig(1.0 / System.Math.Max(1e-12, _toldProbability)) + " TIMES";
                _odds.transform.localPosition = new Vector3(0f, 4.5f, eye.z + 9f);
                SetAlpha(_odds, Mathf.Clamp01((Time.time - _toldAt - 0.6f) / 0.8f));
                Level2Scenery.FaceEye(_final.transform, eyeWorld, false);
                Level2Scenery.FaceEye(_odds.transform, eyeWorld, false);

                // The machine's own favourite story, and two others it could have written: the roads not taken.
                _favStory.gameObject.SetActive(true);
                _others.gameObject.SetActive(true);
                _favStory.text = moment.Path == moment.FavouritePath
                    ? "THAT IS THE MACHINE'S OWN FAVOURITE STORY!"
                    : "THE MACHINE'S FAVOURITE:  \"" + moment.Tell(moment.FavouritePath) + "\"";
                _others.text = "OTHER STORIES IT COULD HAVE WRITTEN:\n\"" + moment.Tell(OtherPath(1)) + "\"\n\"" + moment.Tell(OtherPath(2)) + "\"";
                _favStory.transform.localPosition = new Vector3(0f, 3.7f, eye.z + 9f);
                _others.transform.localPosition = new Vector3(0f, 2.55f, eye.z + 9f);
                SetAlpha(_favStory, Mathf.Clamp01((Time.time - _toldAt - 2.0f) / 0.8f));
                SetAlpha(_others, Mathf.Clamp01((Time.time - _toldAt - 3.5f) / 0.8f));
                Level2Scenery.FaceEye(_favStory.transform, eyeWorld, false);
                Level2Scenery.FaceEye(_others.transform, eyeWorld, false);
                _story.gameObject.SetActive(false);
            }
            else
            {
                _final.gameObject.SetActive(false);
                _odds.gameObject.SetActive(false);
                _favStory.gameObject.SetActive(false);
                _others.gameObject.SetActive(false);
                _story.gameObject.SetActive(true);
            }

            UpdateSparks();
        }

        /// <summary>Another road through the forks, not the rider's and not the favourite: the k-th lane at the first fork, then the machine's favourites.</summary>
        private string OtherPath(int k)
        {
            string taken = moment.Path;
            int first = taken.Length > 0 ? taken[0] - '0' : 0;
            int lane = (first + k) % Lanes;
            if (lane == 0) lane = (first + k + 1) % Lanes;
            return lane + new string(k == 1 ? '1' : '0', Mathf.Max(0, moment.ForkCount - 1));
        }

        private static string FormatBig(double n)
        {
            if (n < 1000.0) return Mathf.RoundToInt((float)n).ToString();
            return ((long)n).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void SetAlpha(TMP_Text t, float a)
        {
            Color c = t.color;
            c.a = a;
            t.color = c;
        }

        private void UpdateSparks()
        {
            _sparks.HideAll();
            int n = 0;
            for (int i = _spark.Count - 1; i >= 0; i--)
            {
                (Vector3 pos, Vector3 vel, float born, Color color) p = _spark[i];
                float age = Time.time - p.born;
                if (age > 1.0f)
                {
                    _spark.RemoveAt(i);
                    continue;
                }

                if (n >= SparkCount) continue;
                float fade = 1f - age;
                _sparks.Set(n++, p.pos + p.vel * age * (1f - 0.35f * age), 0.24f * fade + 0.03f, Fx.A(p.color, fade));
            }
        }
    }
}
