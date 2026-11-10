using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 1, played: four slots of flying words, each a bold word whose letters are measured as it spawns, with a glowing seam wherever
    /// GPT-2 would cut. A swipe through a seam splits the word's tokens apart with a flash; when the last seam goes the tokens fly into the
    /// bank (a glass tube that fills as the rider chops). A word the machine keeps whole shows "ONE TOKEN" and flies by; chopping it
    /// flinches. Until the first cut a ghost swipe shows how. Display and the colliders the laser aims at; the rules are the moment's.
    /// </summary>
    public sealed class SliceView : MonoBehaviour
    {
        private static readonly Color[] Palette =
        {
            new Color(0.35f, 0.85f, 1f), new Color(1f, 0.78f, 0.35f), new Color(0.62f, 1f, 0.62f),
            new Color(1f, 0.5f, 0.72f), new Color(0.72f, 0.6f, 1f), new Color(1f, 0.62f, 0.4f),
        };

        private sealed class SlotView
        {
            public Transform Root;
            public BoxCollider Box;
            public TextMeshPro[] Tokens;
            public TextMeshPro Tag;
            public float[] TokenX = new float[12];
            public float[] Gap = new float[12];
            public float ExitAt = -10f;
            public float FlinchAt = -10f;
            public bool Shown;
            public Vector3 Last;
            public float Size;
        }

        private sealed class Chip
        {
            public bool Alive;
            public Vector3 From, Mid, To;
            public float Born, Life;
            public int Colour;
            public TextMeshPro Text;
        }

        [SerializeField] private SliceMoment moment;
        [SerializeField] private Transform stage;

        private Level2Kit _kit;
        private Transform _props;
        private SlotView[] _slots;
        private GlowField _seams, _sparks, _coach;
        private ShapeBatch _chipBack, _bank, _cuts;
        private Chip[] _chips;
        private TextMeshPro _bankCount, _bankLabel, _banner, _measure;
        private readonly List<(Vector3 pos, Vector3 vel, float born, Color color)> _spark = new List<(Vector3, Vector3, float, Color)>();
        private readonly List<(Vector3 pos, float height, float born)> _flashes = new List<(Vector3, float, float)>();
        private int _bankTokens;
        private const int PerSlot = 14;
        private const int SeamQuads = SliceMoment.SlotCount * PerSlot;
        private const int ChipCount = 16;
        private const int SparkCount = 110;
        private static readonly Vector3 BankAt = new Vector3(-5.4f, 2.6f, 6.8f);

        private void Awake()
        {
            _kit = Level2Kit.Current;
            if (_kit == null || moment == null) return;
            _props = new GameObject("SliceProps").transform;
            _props.SetParent(stage != null ? stage : transform, false);

            _measure = _kit.Text(_props, "Measure", "", 0.5f, Color.white, Vector3.zero, 60f, TextAlignmentOptions.Left);
            _measure.GetComponent<Renderer>().enabled = false;

            _slots = new SlotView[SliceMoment.SlotCount];
            for (int i = 0; i < _slots.Length; i++)
            {
                var root = new GameObject("Word" + i).transform;
                root.SetParent(_props, false);
                var box = root.gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                var pick = root.gameObject.AddComponent<PickTarget>();
                pick.Owner = moment;
                pick.Index = i;
                var view = new SlotView { Root = root, Box = box, Tokens = new TextMeshPro[i == 0 ? 12 : 5] };
                for (int t = 0; t < view.Tokens.Length; t++)
                {
                    view.Tokens[t] = _kit.Text(root, "Token" + t, "", 0.5f, Color.white, Vector3.zero, 6f, TextAlignmentOptions.Center, true);
                    view.Tokens[t].gameObject.SetActive(false);
                }

                view.Tag = _kit.Text(root, "Tag", "", 0.26f, Color.white, new Vector3(0f, -0.7f, 0f), 6f);
                view.Tag.gameObject.SetActive(false);
                _slots[i] = view;
                root.gameObject.SetActive(false);
            }

            _seams = GlowField.Create(_props, "Seams", SeamQuads, _kit.Glow, 2);

            // The cut lines themselves: solid, hot pink with a dark edge, so a seam reads against the bright sky (owner playtest
            // 2026-11-10: the soft glow alone was "like an invisible line"). Two lines per seam: the dark edge, then the colour.
            var cuts = new List<Shape>();
            for (int i = 0; i < SeamQuads * 2; i++) cuts.Add(Level2Kit.Line(Vector3.zero, Vector3.up, 0.05f, Color.white));
            foreach (Shape s in cuts) s.alpha = 0f;
            _cuts = _kit.Batch(_props, "Cuts", cuts, 9);
            _sparks = GlowField.Create(_props, "Sparks", SparkCount, _kit.Glow, 4);
            _coach = GlowField.Create(_props, "Coach", 8, _kit.Glow, 5);

            // The chips: a rounded back and a word each, flying to the bank.
            var backs = new List<Shape>();
            for (int i = 0; i < ChipCount; i++) backs.Add(Level2Kit.Square(Vector3.zero, new Vector2(0.9f, 0.4f), new Color(0.05f, 0.06f, 0.14f, 0.85f), 0.4f, Color.white, 0.03f));
            foreach (Shape s in backs) s.alpha = 0f;
            _chipBack = _kit.Batch(_props, "ChipBacks", backs, 6);
            _chips = new Chip[ChipCount];
            for (int i = 0; i < ChipCount; i++)
            {
                _chips[i] = new Chip { Text = _kit.Text(_props, "Chip" + i, "", 0.3f, Color.white, Vector3.zero, 3f) };
                _chips[i].Text.gameObject.SetActive(false);
            }

            // The bank: a glass tube that fills.
            var tube = new List<Shape>
            {
                Level2Kit.Square(BankAt, new Vector2(1.0f, 2.8f), new Color(0.05f, 0.07f, 0.16f, 0.98f), 0.3f, new Color(1f, 0.82f, 0.45f, 0.9f), 0.04f),
                Level2Kit.Square(BankAt + new Vector3(0f, -1.3f, -0.02f), new Vector2(0.78f, 0.05f), new Color(1f, 0.78f, 0.3f, 0.9f), 0.5f),
            };
            _bank = _kit.Batch(_props, "Bank", tube, 3);
            _bankLabel = _kit.Text(_props, "BankLabel", "TOKENS", 0.3f, new Color(1f, 0.82f, 0.45f), BankAt + new Vector3(0f, 1.75f, 0f), 3f);
            _bankCount = _kit.Text(_props, "BankCount", "0", 0.55f, Color.white, BankAt + new Vector3(0f, -1.95f, 0f), 3f);
            _banner = _kit.Text(_props, "Banner", "SWIPE THE LASER THROUGH THE GLOWING SEAMS", 0.3f, new Color(1f, 0.9f, 0.55f), new Vector3(0f, 4.55f, 7.2f), 9f);

            moment.SeamWasCut += OnCut;
            moment.WordDone += OnDone;
            moment.KeptWhole += OnKept;
            moment.Broken += OnBroken;
            moment.WordMissed += OnMissed;
            moment.Spawned += OnSpawned;
            _props.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (moment == null) return;
            moment.SeamWasCut -= OnCut;
            moment.WordDone -= OnDone;
            moment.KeptWhole -= OnKept;
            moment.Broken -= OnBroken;
            moment.WordMissed -= OnMissed;
            moment.Spawned -= OnSpawned;
        }

        private static readonly Vector3 Eye = new Vector3(0f, 1.3f, 0f);

        private void OnSpawned(int i)
        {
            SliceMoment.Slot s = moment.SlotAt(i);
            SlotView v = _slots[i];
            v.ExitAt = -10f;
            v.FlinchAt = -10f;
            v.Shown = true;
            v.Size = s.Size;
            for (int g = 0; g < v.Gap.Length; g++) v.Gap[g] = 0f;
            v.Root.gameObject.SetActive(true);
            v.Tag.gameObject.SetActive(false);

            // Set the whole word once to find where each letter lands: tokens are placed and seams placed from those, so the glowing seam
            // is exactly between the pieces the machine would cut.
            _measure.fontSize = s.Size * 10f;
            _measure.text = s.Word;
            _measure.ForceMeshUpdate();
            TMP_TextInfo info = _measure.textInfo;
            int letters = Mathf.Min(s.Word.Length, info.characterCount);
            float left = info.characterInfo[0].bottomLeft.x, right = info.characterInfo[letters - 1].topRight.x;
            float centre = (left + right) * 0.5f;
            int n = s.Tokens.Length;
            var seamAt = new float[Mathf.Max(0, n - 1)];
            int cursor = 0;
            for (int t = 0; t < v.Tokens.Length; t++)
            {
                TextMeshPro tmp = v.Tokens[t];
                if (t >= n)
                {
                    tmp.gameObject.SetActive(false);
                    continue;
                }

                string text = s.Tokens[t].Trim();
                int first = Mathf.Min(cursor, letters - 1), last = Mathf.Min(cursor + text.Length - 1, letters - 1);
                float a = info.characterInfo[first].bottomLeft.x, b = info.characterInfo[last].topRight.x;
                v.TokenX[t] = (a + b) * 0.5f - centre;
                if (t < n - 1)
                {
                    int next = Mathf.Min(cursor + text.Length, letters - 1);
                    seamAt[t] = (b + info.characterInfo[next].bottomLeft.x) * 0.5f - centre;
                }

                cursor += text.Length;
                tmp.gameObject.SetActive(true);
                tmp.fontSize = s.Size * 10f;
                tmp.text = text;
                tmp.rectTransform.sizeDelta = new Vector2((b - a) + 0.6f, s.Size * 2f);
                tmp.color = Color.white;
                tmp.alpha = 1f;
                tmp.transform.localPosition = new Vector3(v.TokenX[t], 0f, 0f);
            }

            float width = right - left;
            moment.SetLayout(i, width, seamAt);
            v.Box.size = new Vector3(width + 0.8f, s.Size * 2.0f, 0.8f);
            v.Box.center = Vector3.zero;
            Place(i);
        }

        private void Place(int i)
        {
            SliceMoment.Slot s = moment.SlotAt(i);
            SlotView v = _slots[i];
            if (s.Active) v.Last = s.Position;
            v.Root.localPosition = v.Last;
            Level2Scenery.FaceEye(v.Root, _props.TransformPoint(Eye));
        }

        private void Update()
        {
            if (_props == null) return;
            bool running = moment.IsRunning || moment.IsLive;
            if (_props.gameObject.activeSelf != running) _props.gameObject.SetActive(running);
            if (!running)
            {
                _bankTokens = 0;
                return;
            }

            bool teach = moment.Resolved < 2;
            if (_banner.gameObject.activeSelf != teach) _banner.gameObject.SetActive(teach);
            UpdateSlots();
            UpdateSpark();
            UpdateChips();
            UpdateBank();
            UpdateCoach();
        }

        private void UpdateSlots()
        {
            _seams.HideAll();
            for (int c = 0; c < _cuts.Count; c++) _cuts[c].alpha = 0f;
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 7f);
            for (int i = 0; i < _slots.Length; i++)
            {
                SlotView v = _slots[i];
                SliceMoment.Slot s = moment.SlotAt(i);
                if (!v.Shown) continue;
                bool exiting = !s.Active;
                if (exiting && v.ExitAt < 0f) v.ExitAt = Time.time;
                if (exiting && Time.time - v.ExitAt > 0.9f)
                {
                    v.Shown = false;
                    v.Root.gameObject.SetActive(false);
                    continue;
                }

                if (!exiting) v.ExitAt = -10f;
                Place(i);
                float exit = exiting ? (Time.time - v.ExitAt) / 0.9f : 0f;
                Vector3 drift = exiting ? new Vector3(0f, exit * 1.2f, -exit * 2.2f) : Vector3.zero;
                v.Root.localPosition = v.Last + drift;

                float flinch = Time.time - v.FlinchAt;
                Vector3 shake = flinch < 0.5f ? new Vector3(Mathf.Sin(flinch * 70f) * 0.07f, 0f, 0f) : Vector3.zero;
                int n = s.Tokens != null ? s.Tokens.Length : 0;
                Quaternion face = v.Root.localRotation;

                for (int t = 0; t < n && t < v.Tokens.Length; t++)
                {
                    TextMeshPro tmp = v.Tokens[t];
                    // Tokens right of a cut slide right, tokens left of it slide left.
                    float offset = 0f;
                    for (int k = 0; k < n - 1; k++)
                    {
                        if (s.Cut == null || !s.Cut[k]) continue;
                        v.Gap[k] = Mathf.MoveTowards(v.Gap[k], 0.12f, Time.deltaTime * 0.9f);
                        offset += (t > k ? 1f : -1f) * v.Gap[k];
                    }

                    tmp.transform.localPosition = new Vector3(v.TokenX[t] + offset, 0f, 0f) + shake;
                    bool cutAny = s.Cut != null && ((t > 0 && s.Cut[t - 1]) || (t < n - 1 && s.Cut[t]));
                    Color c = s.Broken && flinch < 0.7f ? new Color(1f, 0.35f, 0.3f) : cutAny ? Palette[t % Palette.Length] : Color.white;
                    c.a = exiting ? 1f - exit : 1f;
                    tmp.color = c;
                }

                // The glowing underline and the seams.
                float width = s.Width;
                Vector3 centre = v.Root.localPosition;
                Color under = s.Seams > 0 ? new Color(1f, 0.8f, 0.4f, 0.55f) : new Color(0.5f, 1f, 0.75f, 0.35f);
                under.a *= exiting ? 1f - exit : 1f;
                _seams.Set(i * PerSlot + 13, centre + face * new Vector3(0f, -s.Size * 0.62f, 0.02f), new Vector2(width + 0.4f, 0.09f), under);
                if (!exiting && s.Seams > 0)
                {
                    for (int k = 0; k < s.Seams; k++)
                    {
                        if (s.Cut[k]) continue;
                        Vector3 at = centre + face * new Vector3(moment.SeamOffset(i, k), 0f, -0.05f);
                        _seams.Set(i * PerSlot + k, at, new Vector2(0.25f + 0.06f * pulse, s.Size * 2.3f), new Color(1f, 0.4f, 0.7f, 0.6f * pulse));
                        _seams.Set(i * PerSlot + 12 - (k % 2), at, new Vector2(0.7f, s.Size * 2.6f), new Color(1f, 0.25f, 0.6f, 0.35f * pulse));
                        Vector3 top = at + face * new Vector3(0f, s.Size * 1.05f, -0.02f), bottom = at - face * new Vector3(0f, s.Size * 1.05f, 0.02f);
                        Shape edge = _cuts[(i * PerSlot + k) * 2], line = _cuts[(i * PerSlot + k) * 2 + 1];
                        edge.position = top; edge.end = bottom; edge.size = new Vector2(0.14f, 0.14f); edge.color = new Color(0.08f, 0.02f, 0.1f); edge.alpha = 0.85f;
                        line.position = top; line.end = bottom; line.size = new Vector2(0.075f + 0.02f * pulse, 0.075f + 0.02f * pulse); line.color = new Color(1f, 0.2f, 0.62f); line.alpha = 1f;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (_cuts != null) _cuts.MarkDirty();
        }

        // ---------- events ----------

        private void OnCut(int slot, int seam, Vector3 at, int points)
        {
            SlotView v = _slots[slot];
            Vector3 world = v.Root.localPosition + v.Root.localRotation * new Vector3(moment.SeamOffset(slot, seam), 0f, -0.1f);
            _flashes.Add((world, moment.SlotAt(slot).Size * 2.4f, Time.time));
            for (int k = 0; k < 9; k++)
            {
                float ang = Fx.Rand01(k + slot * 13 + seam * 5 + (int)(Time.time * 10f)) * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang) * 1.4f, -0.2f);
                _spark.Add((world, dir * (1.2f + 1.6f * Fx.Rand01(k + 3)), Time.time, k % 2 == 0 ? new Color(1f, 0.9f, 0.5f) : Color.white));
            }
        }

        private void OnDone(SliceMoment.SlicedWord w)
        {
            SlotView v = _slots[w.Slot];
            for (int t = 0; t < w.Tokens.Length; t++)
            {
                int free = FreeChip();
                if (free < 0) break;
                Chip c = _chips[free];
                c.Alive = true;
                c.Born = Time.time + t * 0.06f;
                c.Life = 0.95f;
                c.Colour = t;
                c.From = v.Root.localPosition + v.Root.localRotation * new Vector3(v.TokenX[t], 0f, -0.1f);
                c.To = BankAt + new Vector3(0f, -0.9f, 0f);
                c.Mid = (c.From + c.To) * 0.5f + new Vector3(0f, 1.6f, -0.8f);
                c.Text.text = w.Tokens[t].Trim();
                c.Text.fontSize = Mathf.Clamp(w.Size * 6f, 2.2f, 3.4f);
                c.Text.color = Palette[t % Palette.Length];
                c.Text.gameObject.SetActive(true);
            }

            // The word's own tokens vanish at once: they are flying.
            v.Shown = false;
            v.Root.gameObject.SetActive(false);
            for (int k = 0; k < 14; k++)
            {
                float ang = k / 14f * Mathf.PI * 2f;
                _spark.Add((v.Last, new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), -0.2f) * 2.2f, Time.time, Palette[k % Palette.Length]));
            }
        }

        private int FreeChip()
        {
            for (int i = 0; i < _chips.Length; i++) if (!_chips[i].Alive) return i;
            return -1;
        }

        private void OnKept(int slot) => Tag(slot, "ONE TOKEN", new Color(0.5f, 1f, 0.7f));

        private void OnMissed(int slot) => Tag(slot, "MISSED IT", new Color(1f, 0.5f, 0.45f));

        private void OnBroken(int slot)
        {
            _slots[slot].FlinchAt = Time.time;
            Tag(slot, "THAT'S ONE TOKEN!", new Color(1f, 0.5f, 0.45f));
        }

        private void Tag(int slot, string text, Color color)
        {
            SlotView v = _slots[slot];
            v.Tag.text = text;
            v.Tag.color = color;
            v.Tag.fontSize = Mathf.Max(2.2f, v.Size * 4.6f);
            v.Tag.gameObject.SetActive(true);
        }

        // ---------- the motion of sparks, chips and the bank ----------

        private void UpdateSpark()
        {
            _sparks.HideAll();
            int n = 0;
            for (int i = _flashes.Count - 1; i >= 0; i--)
            {
                (Vector3 pos, float height, float born) f = _flashes[i];
                float age = Time.time - f.born;
                if (age > 0.35f)
                {
                    _flashes.RemoveAt(i);
                    continue;
                }

                float fade = 1f - age / 0.35f;
                if (n < SparkCount) _sparks.Set(n++, f.pos, new Vector2(0.18f + 0.5f * age, f.height * (1f + age)), new Color(1f, 0.97f, 0.8f, fade));
            }

            for (int i = _spark.Count - 1; i >= 0; i--)
            {
                (Vector3 pos, Vector3 vel, float born, Color color) p = _spark[i];
                float age = Time.time - p.born;
                if (age > 0.7f)
                {
                    _spark.RemoveAt(i);
                    continue;
                }

                if (n >= SparkCount) continue;
                float fade = 1f - age / 0.7f;
                _sparks.Set(n++, p.pos + p.vel * age + Vector3.down * (2f * age * age), 0.13f * fade + 0.02f, Fx.A(p.color, fade));
            }
        }

        private void UpdateChips()
        {
            for (int i = 0; i < _chips.Length; i++)
            {
                Chip c = _chips[i];
                Shape back = _chipBack[i];
                if (!c.Alive)
                {
                    if (back.alpha > 0f)
                    {
                        back.alpha = 0f;
                        _chipBack.MarkDirty();
                    }

                    continue;
                }

                float t = (Time.time - c.Born) / c.Life;
                if (t < 0f)
                {
                    back.alpha = 0f;
                    c.Text.gameObject.SetActive(false);
                    _chipBack.MarkDirty();
                    continue;
                }

                if (t >= 1f)
                {
                    c.Alive = false;
                    c.Text.gameObject.SetActive(false);
                    back.alpha = 0f;
                    _chipBack.MarkDirty();
                    _bankTokens++;
                    for (int k = 0; k < 5; k++) _spark.Add((c.To, new Vector3(Mathf.Cos(k * 1.3f), Mathf.Sin(k * 1.3f), 0f) * 1.2f, Time.time, new Color(1f, 0.85f, 0.4f)));
                    continue;
                }

                c.Text.gameObject.SetActive(true);
                float e = Fx.Smooth(t);
                Vector3 a = Vector3.Lerp(c.From, c.Mid, e), b = Vector3.Lerp(c.Mid, c.To, e);
                Vector3 p = Vector3.Lerp(a, b, e);
                c.Text.transform.localPosition = p + new Vector3(0f, 0f, -0.05f);
                c.Text.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, e);
                c.Text.transform.rotation = Quaternion.LookRotation(c.Text.transform.position - _props.TransformPoint(Eye), Vector3.up);
                back.position = p;
                back.size = new Vector2(Mathf.Max(0.7f, c.Text.text.Length * 0.2f + 0.45f), 0.42f);
                back.scale = Mathf.Lerp(1f, 0.6f, e);
                back.stroke = Fx.A(Palette[c.Colour % Palette.Length], 1f);
                back.alpha = 0.95f;
                back.rotation = c.Text.transform.localRotation;
                _chipBack.MarkDirty();
            }
        }

        private void UpdateBank()
        {
            float fill = Mathf.Clamp01(_bankTokens / 30f);
            Shape level = _bank[1];
            level.position = BankAt + new Vector3(0f, -1.3f + 2.5f * fill * 0.5f, -0.02f);
            level.size = new Vector2(0.78f, Mathf.Max(0.05f, 2.5f * fill));
            _bank.MarkDirty();
            _bankCount.text = _bankTokens.ToString();
        }

        private void UpdateCoach()
        {
            _coach.HideAll();
            if (moment.SeamsCut > 0) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                SliceMoment.Slot s = moment.SlotAt(i);
                if (!s.Active || s.Seams == 0) continue;
                // A ghost swipe, left to right through the first word that has seams, until the rider cuts one.
                float t = Mathf.Repeat(Time.time * 0.55f, 1f);
                float half = s.Width * 0.5f + 0.5f;
                Vector3 centre = _slots[i].Root.localPosition;
                Quaternion q = _slots[i].Root.localRotation;
                for (int k = 0; k < 8; k++)
                {
                    float back = Mathf.Clamp01(t - k * 0.03f);
                    Vector3 p = centre + q * new Vector3(Mathf.Lerp(-half, half, back), Mathf.Sin(back * Mathf.PI * 2f) * 0.18f, -0.2f);
                    _coach.Set(k, p, k == 0 ? 0.42f : 0.3f - k * 0.025f, new Color(1f, 1f, 1f, (k == 0 ? 0.95f : 0.5f - k * 0.05f) * Mathf.Sin(t * Mathf.PI)));
                }

                return;
            }
        }
    }
}
