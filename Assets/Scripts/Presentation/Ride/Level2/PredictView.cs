using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 4, played: a sentence one word short, and a row of towers in the city, one for each word it might be (plus one for every other
    /// word the machine knows). The rider's laser adds a gold chip to the tower it shoots (left hand takes one back); ten chips in all.
    /// When the rider locks in, GPT-2's real odds rise as glowing bars behind the chips, the real word lights up with a beam, and the
    /// result says how many chips the rider had on it and what the machine gave it. Display and the colliders the laser aims at; the rules
    /// are the moment's.
    /// </summary>
    public sealed class PredictView : MonoBehaviour
    {
        private const int Towers = 7;
        private const int Chips = 10;

        private static readonly Color[] Cols =
        {
            new Color(1f, 0.42f, 0.7f), new Color(0.65f, 0.5f, 1f), new Color(0.3f, 0.85f, 1f), new Color(1f, 0.6f, 0.4f), new Color(0.5f, 1f, 0.7f), new Color(1f, 0.85f, 0.4f), new Color(0.7f, 0.7f, 0.85f),
        };

        [SerializeField] private PredictMoment moment;
        [SerializeField] private Transform stage;

        private Level2Kit _kit;
        private Transform _props;
        private ShapeBatch _batch;
        private GlowField _glows;
        private RibbonField _beam;
        private TextMeshPro[] _names, _percent, _counts;
        private TextMeshPro _prompt, _banner, _left, _result, _lockLabel, _sub;
        private BoxCollider[] _boxes;
        private BoxCollider _lockBox;
        private int _slot, _bar, _chip, _coin, _lock;
        private readonly float[] _popAt = new float[Towers];
        private readonly int[] _shownChips = new int[Towers];
        private float _revealAt = -10f;
        private bool _revealed;
        private int _round = -1;
        private PredictMoment.Offer _offer;
        private static readonly Vector3 Centre = new Vector3(0f, 2.4f, 8f);
        private static readonly Vector3 Eye = new Vector3(0f, 1.3f, 0f);
        private const float Pitch = 1.42f, Base = 1.35f, Full = 2.7f, ChipStep = 0.255f;

        private Vector3 ColumnBase(int i) => new Vector3((i - 3) * Pitch, Base, 8f);

        private void Awake()
        {
            _kit = Level2Kit.Current;
            if (_kit == null || moment == null) return;
            moment.Prepare();
            _props = new GameObject("PredictProps").transform;
            _props.SetParent(stage != null ? stage : transform, false);

            var shapes = new List<Shape>();
            _slot = shapes.Count;
            for (int i = 0; i < Towers; i++) shapes.Add(Level2Kit.Square(ColumnBase(i) + new Vector3(0f, Full * 0.5f, 0.1f), new Vector2(1.12f, Full), new Color(0.1f, 0.04f, 0.16f, 0.5f), 0.12f, Fx.A(Cols[i], 0.4f), 0.03f));
            _bar = shapes.Count;
            for (int i = 0; i < Towers; i++) shapes.Add(Level2Kit.Square(ColumnBase(i), new Vector2(1.0f, 0.05f), Fx.A(Cols[i], 0.5f), 0.1f, Cols[i], 0.05f));
            _chip = shapes.Count;
            for (int i = 0; i < Towers; i++)
            {
                for (int k = 0; k < Chips; k++) shapes.Add(Level2Kit.Square(ColumnBase(i) + new Vector3(0f, 0.2f + ChipStep * k, -0.2f), new Vector2(0.62f, 0.22f), new Color(1f, 0.82f, 0.3f, 1f), 1f, new Color(1f, 0.97f, 0.7f, 1f), 0.035f));
            }

            _coin = shapes.Count;
            for (int k = 0; k < Chips; k++) shapes.Add(Level2Kit.Square(new Vector3(-6.2f, 4.2f - k * 0.36f, 8f), new Vector2(0.3f, 0.3f), new Color(1f, 0.82f, 0.3f, 1f), 1f, new Color(1f, 0.97f, 0.7f, 1f), 0.03f));
            _lock = shapes.Count;
            shapes.Add(Level2Kit.Square(new Vector3(0f, 0.2f, 7.6f), new Vector2(3.2f, 0.62f), new Color(0.3f, 0.85f, 1f, 0.22f), 0.45f, new Color(0.3f, 0.85f, 1f, 0.95f), 0.04f));
            foreach (Shape s in shapes) s.alpha = 0f;
            _batch = _kit.Batch(_props, "Towers", shapes, 2);

            _names = new TextMeshPro[Towers];
            _percent = new TextMeshPro[Towers];
            _counts = new TextMeshPro[Towers];
            _boxes = new BoxCollider[Towers];
            for (int i = 0; i < Towers; i++)
            {
                _names[i] = _kit.Text(_props, "Name" + i, "", 0.34f, Color.white, ColumnBase(i) + new Vector3(0f, -0.38f, -0.1f), Pitch - 0.04f);
                _names[i].enableAutoSizing = true;
                _names[i].fontSizeMin = 1.6f;
                _names[i].fontSizeMax = 3.4f;
                _names[i].textWrappingMode = TextWrappingModes.NoWrap;
                _percent[i] = _kit.Text(_props, "Percent" + i, "", 0.36f, Cols[i], ColumnBase(i) + new Vector3(0f, Full + 0.3f, -0.1f), 1.6f);
                _counts[i] = _kit.Text(_props, "Count" + i, "", 0.44f, new Color(1f, 0.92f, 0.5f), ColumnBase(i) + new Vector3(0f, Full + 0.3f, -0.1f), 1.6f);
                var go = new GameObject("Pick" + i);
                go.transform.SetParent(_props, false);
                go.transform.localPosition = ColumnBase(i) + new Vector3(0f, Full * 0.5f, 0f);
                _boxes[i] = go.AddComponent<BoxCollider>();
                _boxes[i].isTrigger = true;
                _boxes[i].size = new Vector3(Pitch * 0.96f, Full + 0.7f, 0.8f);
                var pick = go.AddComponent<PickTarget>();
                pick.Owner = moment;
                pick.Index = i;
            }

            var lockGo = new GameObject("Lock");
            lockGo.transform.SetParent(_props, false);
            lockGo.transform.localPosition = new Vector3(0f, 0.2f, 7.6f);
            _lockBox = lockGo.AddComponent<BoxCollider>();
            _lockBox.isTrigger = true;
            _lockBox.size = new Vector3(3.4f, 0.8f, 0.8f);
            var lockPick = lockGo.AddComponent<PickTarget>();
            lockPick.Owner = moment;
            lockPick.Index = PredictMoment.LockIndex;

            _prompt = _kit.Text(_props, "Prompt", "", 0.5f, Color.white, new Vector3(0f, 5.25f, 8f), 12f);
            _banner = _kit.Text(_props, "Banner", "SHOOT A WORD TO ADD A CHIP  -  LEFT HAND TAKES ONE BACK", 0.26f, new Color(1f, 0.85f, 0.95f), new Vector3(0f, 4.7f, 8f), 12f);
            _left = _kit.Text(_props, "Left", "", 0.34f, new Color(1f, 0.92f, 0.5f), new Vector3(-6.2f, 4.6f, 8f), 3f);
            _result = _kit.Text(_props, "Result", "", 0.42f, new Color(0.5f, 1f, 0.7f), new Vector3(0f, 0.45f, 7.6f), 12f);
            _sub = _kit.Text(_props, "Sub", "", 0.28f, new Color(0.9f, 0.9f, 1f), new Vector3(0f, -0.05f, 7.6f), 12f);
            _lockLabel = _kit.Text(_props, "LockLabel", "LOCK IT IN", 0.3f, Color.white, new Vector3(0f, 0.2f, 7.5f), 3f);
            _glows = GlowField.Create(_props, "Glows", 40, _kit.Glow, 3);
            _beam = RibbonField.Create(_props, "Beam", 1, _kit.Beam, 1);

            moment.RoundStarted += OnRoundStarted;
            moment.Locked += OnLocked;
            moment.Revealed += OnRevealed;
            moment.ChipsChanged += OnChips;
            _props.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (moment == null) return;
            moment.RoundStarted -= OnRoundStarted;
            moment.Locked -= OnLocked;
            moment.Revealed -= OnRevealed;
            moment.ChipsChanged -= OnChips;
        }

        private void OnRoundStarted(int round)
        {
            _round = round;
            _offer = moment.Current;
            _revealed = false;
            _revealAt = -10f;
            _prompt.text = _offer.Prompt + "  ___";
            _prompt.color = Color.white;
            _result.text = "";
            _sub.text = "";
            for (int i = 0; i < Towers; i++)
            {
                _shownChips[i] = 0;
                _popAt[i] = -10f;
                bool real = i <= _offer.OtherIndex;
                _names[i].text = i < _offer.Words.Length ? _offer.Words[i] : i == _offer.OtherIndex ? "50,000" + System.Environment.NewLine + "OTHERS" : "";
                _names[i].textWrappingMode = i == _offer.OtherIndex ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                _names[i].color = Color.white;
                _percent[i].text = "";
                _counts[i].text = "";
                _boxes[i].enabled = real;
            }

            _banner.gameObject.SetActive(true);
        }

        private void OnChips(int tower, int count)
        {
            if (tower >= 0 && tower < Towers && count > _shownChips[tower]) _popAt[tower] = Time.time;
        }

        private void OnLocked(int round)
        {
            _banner.gameObject.SetActive(false);
        }

        private void OnRevealed(int round, int onTruth, float modelOdds, bool beat)
        {
            _revealed = true;
            _revealAt = Time.time;
            string truth = Level2Data.Clean(_offer.Truth);
            _prompt.text = _offer.Prompt + "  " + truth.ToUpperInvariant();
            _prompt.color = new Color(1f, 0.88f, 0.45f);
            _result.text = onTruth == 0 ? "NO CHIPS ON " + truth.ToUpperInvariant() : onTruth + " OF 10 CHIPS ON " + truth.ToUpperInvariant();
            _result.color = beat ? new Color(0.5f, 1f, 0.7f) : onTruth > 0 ? new Color(1f, 0.88f, 0.45f) : new Color(1f, 0.6f, 0.5f);
            _sub.text = "THE MACHINE GAVE IT " + (modelOdds * 100f).ToString("0") + "%" + (beat ? "   -   YOU BEAT THE MACHINE!" : "");
        }

        private void Update()
        {
            if (_props == null) return;
            bool running = moment.IsRunning;
            if (_props.gameObject.activeSelf != running) _props.gameObject.SetActive(running);
            if (!running || _offer == null || _round != moment.Round) return;

            Vector3 eyeWorld = _props.TransformPoint(Eye);
            _glows.HideAll();
            _beam.HideAll();
            bool betting = moment.Stage == PredictMoment.Phase.Betting;
            float reveal = _revealed ? Mathf.Clamp01((Time.time - _revealAt) / 1.1f) : 0f;
            reveal = Fx.Smooth(reveal);
            int g = 0;

            for (int i = 0; i < Towers; i++)
            {
                bool real = i <= _offer.OtherIndex;
                _names[i].gameObject.SetActive(real);
                _shownChips[i] = moment.ChipsOn(i);
                int chips = _shownChips[i];
                bool truth = i == _offer.TruthIndex;

                // The slot, the rider's chips, the model's bar.
                Shape slot = _batch[_slot + i];
                slot.alpha = real ? 0.9f : 0f;
                if (truth && _revealed) slot.stroke = Fx.A(new Color(1f, 0.88f, 0.4f), 0.95f);
                else slot.stroke = Fx.A(Cols[i], 0.4f);
                for (int k = 0; k < Chips; k++)
                {
                    Shape c = _batch[_chip + i * Chips + k];
                    bool on = real && k < chips;
                    c.alpha = on ? 1f : 0f;
                    float pop = k == chips - 1 && Time.time - _popAt[i] < 0.25f ? 1f + 0.6f * (1f - (Time.time - _popAt[i]) / 0.25f) : 1f;
                    c.scale = pop;
                }

                Shape bar = _batch[_bar + i];
                float p = real ? _offer.Odds[i] : 0f;
                float h = Mathf.Max(0.04f, Full * Mathf.Clamp01(p) * reveal);
                bar.alpha = real && reveal > 0.01f ? 0.55f : 0f;
                bar.size = new Vector2(1.0f, h);
                bar.position = ColumnBase(i) + new Vector3(0f, h * 0.5f, 0.05f);
                _percent[i].text = reveal > 0.2f && real ? Mathf.RoundToInt(p * 100f) + "%" : "";
                // The model's numbers line up along the top of the columns; the rider's chip counts show while betting only, so the two never collide.
                _percent[i].transform.localPosition = ColumnBase(i) + new Vector3(0f, Full + 0.3f, -0.1f);
                _percent[i].gameObject.SetActive(reveal > 0.2f && real);
                _counts[i].text = chips > 0 && real && !_revealed ? chips.ToString() : "";
                _counts[i].transform.localPosition = ColumnBase(i) + new Vector3(0f, 0.2f + ChipStep * chips + 0.3f, -0.25f);

                if (real) _glows.Set(g++, ColumnBase(i) + new Vector3(0f, 0.3f, 0.2f), new Vector2(1.9f, 0.7f), Fx.A(Cols[i], 0.25f + (betting ? 0f : 0.1f)));
                if (truth && _revealed)
                {
                    Vector3 top = ColumnBase(i) + new Vector3(0f, Full + 0.6f, 0f);
                    _glows.Set(g++, ColumnBase(i) + new Vector3(0f, Full * 0.5f, 0.3f), new Vector2(2.4f, Full + 1.4f), new Color(1f, 0.85f, 0.35f, 0.22f * reveal));
                    _beam.Set(0, top + new Vector3(0f, 4f, 0f), ColumnBase(i) + new Vector3(0f, 0.2f, 0f), 0f, 0.9f, new Color(1f, 0.9f, 0.5f, 0.35f * reveal));
                }
            }

            // Chips left, as a column of coins.
            for (int k = 0; k < Chips; k++) _batch[_coin + k].alpha = betting && k < moment.ChipsLeft ? 1f : 0f;
            _left.text = betting ? "CHIPS LEFT  " + moment.ChipsLeft : "";
            _batch[_lock].alpha = betting ? 0.85f : 0f;
            _lockLabel.gameObject.SetActive(betting);
            _lockBox.enabled = betting;
            _result.gameObject.SetActive(_revealed);
            _sub.gameObject.SetActive(_revealed);
            _batch.MarkDirty();
            Level2Scenery.FaceEye(_prompt.transform, eyeWorld, false);
        }
    }
}
