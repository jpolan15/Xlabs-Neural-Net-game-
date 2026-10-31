using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level2;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// Stop 2, played: the real galaxy of GPT-2's words around the rider. Four neighbourhoods glow in their own colours with three of
    /// their words already in place; the loose words hang in front, each a pick the laser can take. A carried word rides the laser (with
    /// a thread of light to the hand), the neighbourhood under the laser lights up as a place to let go, a word dropped on the right one
    /// flies to its own spot in the cluster and becomes a star, a neighbourhood that is complete names itself, and a word dropped on the
    /// wrong one bounces home. Until the first pick a ghost thread shows where the first word belongs. Display and the colliders the laser
    /// aims at; the rules are the moment's.
    /// </summary>
    public sealed class GalaxyView : MonoBehaviour
    {
        private sealed class WordView
        {
            public Transform Root;
            public TextMeshPro Label;
            public BoxCollider Box;
            public Vector3 Shown;
            public float RejectAt = -10f;
            public float PlacedAt = -10f;
            public bool Initialised;
        }

        [SerializeField] private GalaxyMoment moment;
        [SerializeField] private Transform stage;

        private Level2Kit _kit;
        private Transform _props;
        private WordView[] _words;
        private GlowField _glows, _sparks;
        private RibbonField _threads;
        private TextMeshPro[] _names;
        private TextMeshPro _banner;
        private float _nameShown;
        private readonly float[] _complete = new float[8];
        private readonly float[] _lit = new float[8];
        private bool _everPicked;
        private readonly List<(Vector3 pos, Vector3 vel, float born, Color color)> _spark = new List<(Vector3, Vector3, float, Color)>();
        private static readonly Vector3 Eye = new Vector3(0f, 1.3f, 0f);
        private const int SparkCount = 90;

        private void Awake()
        {
            _kit = Level2Kit.Current;
            if (_kit == null || moment == null) return;
            moment.Prepare();
            _props = new GameObject("GalaxyProps").transform;
            _props.SetParent(stage != null ? stage : transform, false);

            int count = moment.Items.Count;
            _words = new WordView[count];
            for (int i = 0; i < count; i++)
            {
                GalaxyMoment.Item item = moment.Items[i];
                var root = new GameObject("Word_" + item.Word).transform;
                root.SetParent(_props, false);
                var view = new WordView { Root = root };
                view.Label = _kit.Text(root, "Label", item.Word, item.Seed ? 0.34f : 0.46f, item.Seed ? Fx.Mul(Level2Kit.CategoryColor(item.Category), 1.1f) : Color.white, Vector3.zero, 4f);
                if (!item.Seed)
                {
                    view.Box = root.gameObject.AddComponent<BoxCollider>();
                    view.Box.isTrigger = true;
                    view.Box.size = new Vector3(1.9f, 0.9f, 0.9f);
                    var pick = root.gameObject.AddComponent<PickTarget>();
                    pick.Owner = moment;
                    pick.Index = i;
                }

                view.Shown = item.Position;
                _words[i] = view;
            }

            _glows = GlowField.Create(_props, "Glows", count * 2 + moment.Clusters.Count * 4 + 6, _kit.Glow, 1);
            _sparks = GlowField.Create(_props, "Sparks", SparkCount, _kit.Glow, 4);
            _threads = RibbonField.Create(_props, "Threads", 6, _kit.Beam, 3);
            _names = new TextMeshPro[moment.Clusters.Count];
            for (int c = 0; c < _names.Length; c++)
            {
                _names[c] = _kit.Text(_props, "Name" + c, moment.Clusters[c].Category.ToUpperInvariant(), 0.5f, Level2Kit.CategoryColor(moment.Clusters[c].Category), moment.Clusters[c].Centre + new Vector3(0f, 1.9f, -0.5f), 6f);
                _names[c].characterSpacing = 6f;
                _names[c].gameObject.SetActive(false);
            }

            _banner = _kit.Text(_props, "Banner", "POINT AT A LOOSE WORD  -  HOLD THE TRIGGER  -  CARRY IT HOME", 0.28f, new Color(0.88f, 0.82f, 1f), new Vector3(0f, 5.2f, 8.5f), 12f);

            moment.Picked += OnPicked;
            moment.Released += OnReleased;
            moment.Placed += OnPlaced;
            moment.RejectedBy += OnRejected;
            moment.ClusterCompleted += OnCompleted;
            _props.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (moment == null) return;
            moment.Picked -= OnPicked;
            moment.Released -= OnReleased;
            moment.Placed -= OnPlaced;
            moment.RejectedBy -= OnRejected;
            moment.ClusterCompleted -= OnCompleted;
        }

        private void OnPicked(int index, int side)
        {
            _everPicked = true;
            Burst(_words[index].Shown, Color.white, 6, 1.6f);
        }

        private void OnReleased(int index) => _words[index].RejectAt = -10f;

        private void OnPlaced(int index, int cluster)
        {
            _words[index].PlacedAt = Time.time;
            Color c = Level2Kit.CategoryColor(moment.Clusters[cluster].Category);
            Burst(moment.Items[index].Home, c, 16, 2.6f);
        }

        private void OnRejected(int index, int cluster)
        {
            _words[index].RejectAt = Time.time;
            Burst(_words[index].Shown, new Color(1f, 0.4f, 0.35f), 10, 2f);
        }

        private void OnCompleted(int cluster)
        {
            _complete[cluster] = Time.time;
            _names[cluster].gameObject.SetActive(true);
            Color c = Level2Kit.CategoryColor(moment.Clusters[cluster].Category);
            Burst(moment.Clusters[cluster].Centre, c, 30, 4.2f);
        }

        private void Burst(Vector3 at, Color color, int count, float speed)
        {
            for (int k = 0; k < count; k++)
            {
                float a = k / (float)count * Mathf.PI * 2f + Fx.Rand01(k + (int)(Time.time * 7f));
                float b = (Fx.Rand01(k * 3 + 1) - 0.5f) * 1.6f;
                _spark.Add((at, new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.8f + b * 0.3f, Mathf.Sin(a + 1f) * 0.6f) * speed * (0.5f + Fx.Rand01(k * 5 + 2)), Time.time, color));
            }
        }

        private void Update()
        {
            if (_props == null) return;
            bool running = moment.IsRunning || moment.IsLive;
            if (_props.gameObject.activeSelf != running) _props.gameObject.SetActive(running);
            if (!running) return;

            _glows.HideAll();
            _threads.HideAll();
            Vector3 eyeWorld = _props.TransformPoint(Eye);
            int next = 0;
            int hint = moment.HintItem;
            if (!_everPicked && hint < 0) hint = FirstLoose();

            // The neighbourhood a carried word is held over lights up.
            int over = -1;
            for (int hand = 0; hand < 2; hand++)
            {
                int h = hand == 0 ? RideHand.Left : RideHand.Right;
                if (!RideAim.TryGet(h, out Vector3 origin, out Vector3 point)) continue;
                bool carrying = false;
                for (int i = 0; i < moment.Items.Count; i++) if (moment.Items[i].Carrier == hand) carrying = true;
                if (carrying) over = moment.ClusterUnder(new Ray(origin, (point - origin).normalized));
            }

            for (int c = 0; c < moment.Clusters.Count; c++)
            {
                GalaxyMoment.Cluster cl = moment.Clusters[c];
                Color col = Level2Kit.CategoryColor(cl.Category);
                float done = cl.Complete ? Mathf.Clamp01(1f - (Time.time - _complete[c]) / 2.5f) : 0f;
                _lit[c] = Mathf.MoveTowards(_lit[c], over == c ? 1f : 0f, Time.deltaTime * 5f);
                float hintBoost = hint >= 0 && moment.Items[hint].Category == cl.Category ? 0.5f + 0.5f * Mathf.Sin(Time.time * 5f) : 0f;
                float strength = 0.16f + 0.5f * _lit[c] + 0.35f * done + 0.2f * hintBoost;
                float pulse = 1f + 0.05f * Mathf.Sin(Time.time * 1.3f + c);
                _glows.Set(next++, cl.Centre, 5.2f * pulse + 1.6f * _lit[c], Fx.A(col, strength));
                _glows.Set(next++, cl.Centre, 9.5f * pulse, Fx.A(col, 0.07f + 0.1f * _lit[c]));
                // A ring around the place to let go.
                if (_lit[c] > 0.01f || hintBoost > 0f) _glows.Set(next++, cl.Centre, new Vector2(4.4f, 4.4f), Fx.A(col, 0.5f * Mathf.Max(_lit[c], hintBoost * 0.7f)), Time.time * 0.3f);
                else next++;
                next++;
            }

            for (int i = 0; i < _words.Length; i++)
            {
                GalaxyMoment.Item item = moment.Items[i];
                WordView v = _words[i];
                Color col = Level2Kit.CategoryColor(item.Category);
                Vector3 target = item.Placed ? item.Home : item.Position;
                if (!v.Initialised)
                {
                    v.Shown = target;
                    v.Initialised = true;
                }

                float follow = item.Carrier >= 0 ? 30f : item.Placed ? 7f : 9f;
                v.Shown = Vector3.Lerp(v.Shown, target, 1f - Mathf.Exp(-follow * Time.deltaTime));
                float bob = item.Carrier >= 0 || item.Placed ? 0f : 0.12f * Mathf.Sin(Time.time * 1.3f + i * 1.9f);
                float reject = Time.time - v.RejectAt;
                float shake = reject < 0.4f ? Mathf.Sin(reject * 60f) * 0.1f : 0f;
                v.Root.localPosition = v.Shown + new Vector3(shake, bob, 0f);
                Level2Scenery.FaceEye(v.Root, eyeWorld, false);

                bool hinted = i == hint;
                float pop = Time.time - v.PlacedAt < 0.6f ? 1f + 0.5f * Mathf.Sin((Time.time - v.PlacedAt) / 0.6f * Mathf.PI) : 1f;
                v.Root.localScale = Vector3.one * pop * (hinted ? 1f + 0.08f * Mathf.Sin(Time.time * 6f) : 1f);
                v.Label.color = item.Placed ? Fx.Mul(col, 1.15f) : reject < 0.5f ? new Color(1f, 0.5f, 0.45f) : Color.white;
                if (!item.Seed && v.Box != null) v.Box.enabled = !item.Placed;

                // A soft light behind every word; brighter for a word in the hand or hinted.
                float glow = item.Placed ? 0.55f : item.Carrier >= 0 ? 0.95f : hinted ? 0.5f + 0.4f * Mathf.Sin(Time.time * 6f) : 0.3f;
                float size = item.Seed || item.Placed ? 1.1f : 1.9f;
                _glows.Set(next++, v.Root.localPosition + new Vector3(0f, 0f, 0.1f), size * pop, Fx.A(item.Placed || item.Seed ? col : Color.Lerp(col, Color.white, 0.5f), glow * (item.Placed ? 0.7f : 1f)));
                if (item.Carrier >= 0 && RideAim.TryGet(item.Carrier == 0 ? RideHand.Left : RideHand.Right, out Vector3 origin, out Vector3 _))
                {
                    _threads.Set(item.Carrier, _props.InverseTransformPoint(origin), v.Root.localPosition, 0.4f, 0.09f, Fx.A(Color.white, 0.8f));
                }
            }

            // Before the first pick: a thread of light from the first loose word to the neighbourhood it belongs to, and a moving spark along it.
            if (!_everPicked && hint >= 0)
            {
                GalaxyMoment.Item item = moment.Items[hint];
                int cl = 0;
                for (int c = 0; c < moment.Clusters.Count; c++) if (moment.Clusters[c].Category == item.Category) cl = c;
                Color col = Level2Kit.CategoryColor(item.Category);
                _threads.Set(5, _words[hint].Root.localPosition, moment.Clusters[cl].Centre, 1.1f, 0.14f, Fx.A(col, 0.65f));
                float t = Mathf.Repeat(Time.time * 0.5f, 1f);
                Vector3 mid = (_words[hint].Root.localPosition + moment.Clusters[cl].Centre) * 0.5f + Vector3.up * 0.55f;
                Vector3 p = Vector3.Lerp(Vector3.Lerp(_words[hint].Root.localPosition, mid, t), Vector3.Lerp(mid, moment.Clusters[cl].Centre, t), t);
                if (next < _glows.Count) _glows.Set(next++, p, 0.7f, Fx.A(Color.white, 0.9f * Mathf.Sin(t * Mathf.PI)));
            }

            if (_banner.gameObject.activeSelf == _everPicked) _banner.gameObject.SetActive(!_everPicked);
            for (int c = 0; c < _names.Length; c++)
            {
                if (!_names[c].gameObject.activeSelf) continue;
                float k = Mathf.Clamp01((Time.time - _complete[c]) / 0.6f);
                _names[c].alpha = k;
                _names[c].transform.localScale = Vector3.one * Fx.Pop(k);
                Level2Scenery.FaceEye(_names[c].transform, eyeWorld, false);
            }

            UpdateSparks();
        }

        private int FirstLoose()
        {
            for (int i = 0; i < moment.Items.Count; i++) if (!moment.Items[i].Seed && !moment.Items[i].Placed) return i;
            return -1;
        }

        private void UpdateSparks()
        {
            _sparks.HideAll();
            int n = 0;
            for (int i = _spark.Count - 1; i >= 0; i--)
            {
                (Vector3 pos, Vector3 vel, float born, Color color) p = _spark[i];
                float age = Time.time - p.born;
                if (age > 0.9f)
                {
                    _spark.RemoveAt(i);
                    continue;
                }

                if (n >= SparkCount) continue;
                float fade = 1f - age / 0.9f;
                _sparks.Set(n++, p.pos + p.vel * age * (1f - 0.4f * age), 0.2f * fade + 0.03f, Fx.A(p.color, fade));
            }
        }
    }
}
