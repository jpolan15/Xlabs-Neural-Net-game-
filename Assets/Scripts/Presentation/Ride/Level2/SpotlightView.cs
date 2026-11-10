using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 3, played: a sentence hangs in front of the rider with one word lit gold, the word whose meaning is being worked out. Every
    /// earlier word is a pick; the rider's laser held on one pours attention into it, and a ribbon of light runs from the lit word to each
    /// word, as thick as that word's share (the shares always add up to a hundred percent). Shares are written on the words. When
    /// the round ends the real attention head of GPT-2 is drawn in gold beside the rider's, with its numbers. Display and the colliders
    /// the laser aims at; the rules are the moment's.
    /// </summary>
    public sealed class SpotlightView : MonoBehaviour
    {
        private const int MaxWords = 11;

        /// <summary>How far above (the rider's share) and below (GPT-2's, once revealed) a word its number sits.</summary>
        private const float PercentHeight = 0.62f;

        /// <summary>Half the gap between the two rows of a long sentence: room for the numbers of both rows between them.</summary>
        private const float RowHalfGap = 0.95f;

        private sealed class WordView
        {
            public Transform Root;
            public TextMeshPro Label;
            public TextMeshPro Percent;
            public TextMeshPro Real;
            public BoxCollider Box;
            public float Width;
            public Vector3 At;
            public float FlashAt = -10f;
        }

        [SerializeField] private SpotlightMoment moment;
        [SerializeField] private Transform stage;

        private Level2Kit _kit;
        private Transform _props;
        private WordView[] _words;
        private RibbonField _arcs, _real, _beams;
        private GlowField _glows;
        private TextMeshPro _banner, _result, _legend;
        private GameObject _theatreSign;
        private bool _signLooked;
        private bool _shown;
        private float _roundAt;
        private string _lastLayout = "";
        private static readonly Vector3 Centre = new Vector3(0f, 3.3f, 7.6f); // high enough that the legend clears the road (ADR-023)
        private static readonly Vector3 Eye = new Vector3(0f, 1.3f, 0f);

        private void Awake()
        {
            _kit = Level2Kit.Current;
            if (_kit == null || moment == null) return;
            _props = new GameObject("SpotlightProps").transform;
            _props.SetParent(stage != null ? stage : transform, false);

            _words = new WordView[MaxWords];
            for (int i = 0; i < MaxWords; i++)
            {
                var root = new GameObject("Word" + i).transform;
                root.SetParent(_props, false);
                var view = new WordView { Root = root };
                view.Label = _kit.Text(root, "Label", "", 0.5f, Color.white, Vector3.zero, 5f);
                view.Percent = _kit.Text(root, "Percent", "", 0.3f, new Color(1f, 0.9f, 0.55f), new Vector3(0f, PercentHeight, 0f), 3f);
                view.Real = _kit.Text(root, "Real", "", 0.3f, new Color(1f, 0.82f, 0.4f), new Vector3(0f, -PercentHeight, 0f), 3f);
                view.Real.gameObject.SetActive(false);
                view.Box = root.gameObject.AddComponent<BoxCollider>();
                view.Box.isTrigger = true;
                var pick = root.gameObject.AddComponent<PickTarget>();
                pick.Owner = moment;
                pick.Index = i;
                _words[i] = view;
                root.gameObject.SetActive(false);
            }

            _arcs = RibbonField.Create(_props, "Arcs", MaxWords, _kit.Beam, 2);
            _real = RibbonField.Create(_props, "RealHead", MaxWords, _kit.Beam, 2);
            _beams = RibbonField.Create(_props, "Spotlights", 2, _kit.Beam, 1);
            _glows = GlowField.Create(_props, "Glows", MaxWords * 2 + 6, _kit.Glow, 3);
            _banner = _kit.Text(_props, "Banner", "", 0.3f, new Color(0.7f, 0.95f, 1f), Centre + new Vector3(0f, 2.5f, 0f), 12f);
            _result = _kit.Text(_props, "Result", "", 0.46f, new Color(1f, 0.85f, 0.45f), Centre + new Vector3(0f, -1.7f, 0f), 12f);
            _legend = _kit.Text(_props, "Legend", "", 0.26f, new Color(0.85f, 0.92f, 1f), Centre + new Vector3(0f, -2.3f, 0f), 12f);

            moment.RoundStarted += OnRoundStarted;
            moment.RoundEnded += OnRoundEnded;
            moment.WrongWord += OnWrong;
            _props.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (moment == null) return;
            moment.RoundStarted -= OnRoundStarted;
            moment.RoundEnded -= OnRoundEnded;
            moment.WrongWord -= OnWrong;
        }

        private void OnRoundStarted(int round)
        {
            L2Attention a = moment.Current;
            _roundAt = Time.time;
            _result.text = "";
            _legend.text = "";
            int n = a.words.Length;
            float total = 0f;
            for (int i = 0; i < MaxWords; i++)
            {
                WordView v = _words[i];
                v.Root.gameObject.SetActive(i < n);
                if (i >= n) continue;
                v.Label.text = a.words[i];
                v.Label.ForceMeshUpdate();
                v.Width = v.Label.preferredWidth;
                total += v.Width;
            }

            // One row when it fits, two when it does not.
            const float gap = 0.42f, maxRow = 10.2f;
            bool two = total + gap * (n - 1) > maxRow;
            int split = two ? Mathf.CeilToInt(n / 2f) : n;
            for (int row = 0; row < (two ? 2 : 1); row++)
            {
                int from = row == 0 ? 0 : split, to = row == 0 ? split : n;
                float w = 0f;
                for (int i = from; i < to; i++) w += _words[i].Width;
                w += gap * (to - from - 1);
                float x = -w * 0.5f;
                for (int i = from; i < to; i++)
                {
                    _words[i].At = Centre + new Vector3(x + _words[i].Width * 0.5f, two ? (row == 0 ? RowHalfGap : -RowHalfGap) : 0f, 0f);
                    x += _words[i].Width + gap;
                    _words[i].Root.localPosition = _words[i].At;
                    _words[i].Box.size = new Vector3(_words[i].Width + 0.5f, 0.95f, 0.8f);
                    bool can = moment.CanLookAt(i);
                    _words[i].Box.enabled = can;
                    _words[i].Percent.text = "";
                    _words[i].Percent.gameObject.SetActive(can);
                    _words[i].Real.text = "";
                    _words[i].Real.gameObject.SetActive(false);
                    bool query = i == a.query;
                    _words[i].Label.color = query ? new Color(1f, 0.85f, 0.4f) : i > a.query ? new Color(1f, 1f, 1f, 0.28f) : Color.white;
                    _words[i].Label.fontStyle = query ? FontStyles.Bold | FontStyles.Underline : FontStyles.Bold;
                }
            }

            // The verdict and its key sit under the sentence, clear of the numbers under the lower row.
            _result.transform.localPosition = Centre + new Vector3(0f, two ? -2.2f : -1.7f, 0f);
            _legend.transform.localPosition = Centre + new Vector3(0f, two ? -2.65f : -2.2f, 0f);
            _banner.text = "HOLD YOUR LASER ON THE WORDS THAT EXPLAIN  \"" + a.words[a.query].ToUpperInvariant() + "\"";
            _lastLayout = a.sentence;
            _real.HideAll();
        }

        private void OnRoundEnded(int round, bool won)
        {
            L2Attention a = moment.Current;
            _legend.text = "ABOVE: YOUR ATTENTION     BELOW, IN GOLD: THE REAL GPT-2 ATTENTION HEAD  (LAYER " + (a.layer + 1) + ", HEAD " + (a.head + 1) + ")";
            int best = 0;
            for (int i = 1; i < a.weights.Length; i++) if (a.weights[i] > a.weights[best]) best = i;
            _result.text = (won ? "YOU FOUND IT.   " : "NOT THIS TIME.   ") + "GPT-2 LOOKS AT  \"" + a.words[best] + "\"  " + Mathf.RoundToInt(a.weights[best] * 100f) + "%";
            _result.color = won ? new Color(0.5f, 1f, 0.7f) : new Color(1f, 0.7f, 0.5f);
            RideSounds.Play(won ? "winShimmer" : "tick", 1f);
        }

        private void OnWrong(int word) => _words[word].FlashAt = Time.time;

        private void Update()
        {
            if (_props == null) return;
            bool running = moment.IsRunning;
            if (_props.gameObject.activeSelf != running) _props.gameObject.SetActive(running);
            if (!_signLooked)
            {
                // The theatre's name (HallWorld, built in its own Awake) hangs where the game's instruction reads from the bike: it
                // steps aside while the game runs.
                _signLooked = true;
                Transform sign = stage != null ? stage.Find("Theatre") : null;
                if (sign != null) _theatreSign = sign.gameObject;
            }

            if (_theatreSign != null && _theatreSign.activeSelf == running) _theatreSign.SetActive(!running);
            if (!running || moment.Current == null || _lastLayout != moment.Current.sentence) return;

            L2Attention a = moment.Current;
            bool reveal = moment.Stage == SpotlightMoment.Phase.Reveal;
            Vector3 eyeWorld = _props.TransformPoint(Eye);
            _arcs.HideAll();
            _real.HideAll();
            _beams.HideAll();
            _glows.HideAll();
            int g = 0;
            Vector3 queryTop = _words[a.query].At + new Vector3(0f, 0.55f, 0f);

            for (int i = 0; i < a.words.Length; i++)
            {
                WordView v = _words[i];
                Level2Scenery.FaceEye(v.Root, eyeWorld, true);
                float flash = Time.time - v.FlashAt;
                float share = moment.Share(i);
                bool can = moment.CanLookAt(i);
                bool answer = System.Array.IndexOf(a.answers, i) >= 0;
                Vector3 top = v.At + new Vector3(0f, 0.55f, 0f);

                if (i == a.query)
                {
                    _glows.Set(g++, v.At + new Vector3(0f, 0f, 0.12f), 2.4f + 0.3f * Mathf.Sin(Time.time * 3f), new Color(1f, 0.8f, 0.3f, 0.4f));
                    continue;
                }

                if (!can) continue;
                bool focused = moment.FocusOf(RideHand.Left) == i || moment.FocusOf(RideHand.Right) == i;
                float arch = 0.8f + 0.28f * Mathf.Abs(a.query - i);
                Color c = Color.Lerp(new Color(0.25f, 0.65f, 1f), Color.white, Mathf.Clamp01(share * 1.5f));
                c.a = 0.22f + 0.7f * share;
                _arcs.Set(i, queryTop, top, arch, 0.05f + 0.6f * share, c);
                v.Percent.text = Mathf.RoundToInt(share * 100f) + "%";
                v.Percent.transform.localPosition = new Vector3(0f, PercentHeight, 0f);
                v.Percent.color = Color.Lerp(new Color(0.7f, 0.85f, 1f), new Color(1f, 0.9f, 0.5f), share * 1.4f);
                v.Percent.fontSize = Mathf.Lerp(2.6f, 4.4f, Mathf.Clamp01(share * 1.6f));
                v.Real.gameObject.SetActive(reveal);
                v.Label.color = flash < 0.5f ? new Color(1f, 0.4f, 0.35f) : focused ? new Color(0.6f, 1f, 1f) : Color.Lerp(Color.white, new Color(0.7f, 1f, 1f), share);
                _glows.Set(g++, v.At + new Vector3(0f, 0f, 0.1f), 1.4f + 2.2f * share, new Color(0.4f, 0.85f, 1f, 0.12f + 0.55f * share));

                // A coach: after a while with no share on the words that matter, they pulse.
                if (!reveal && answer && Time.time - _roundAt > 7f && moment.AnswerShare < 0.3f)
                {
                    _glows.Set(g++, v.At + new Vector3(0f, 0f, 0.15f), 2.2f, new Color(1f, 0.95f, 0.6f, 0.5f * (0.5f + 0.5f * Mathf.Sin(Time.time * 6f))));
                }

                if (reveal)
                {
                    float real = a.weights[i];
                    _real.Set(i, queryTop, top, arch * 0.6f, 0.04f + 0.5f * real, new Color(1f, 0.82f, 0.4f, 0.35f + 0.6f * real));
                    v.Percent.color = Color.white;
                    v.Real.text = Mathf.RoundToInt(real * 100f) + "%";
                    v.Real.fontSize = Mathf.Lerp(2.6f, 4.6f, Mathf.Clamp01(real * 1.6f));
                }
            }

            // Where the lasers rest: a beam of light down onto the word.
            for (int hand = 0; hand < 2; hand++)
            {
                int f = moment.FocusOf(hand == 0 ? RideHand.Left : RideHand.Right);
                if (f < 0 || reveal) continue;
                Vector3 top = _words[f].At + new Vector3(0f, 0.3f, 0f);
                _beams.Set(hand, top + new Vector3(0f, 4.5f, 0.5f), top, 0f, 0.55f, new Color(0.5f, 0.95f, 1f, 0.35f));
            }

            _banner.gameObject.SetActive(!reveal);
            _result.gameObject.SetActive(reveal);
            _legend.gameObject.SetActive(reveal);
            Level2Scenery.FaceEye(_banner.transform, eyeWorld, false);
        }
    }
}
