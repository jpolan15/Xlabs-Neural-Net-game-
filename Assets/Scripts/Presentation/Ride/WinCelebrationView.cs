using System.Collections;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;
using TMPro;
using UnityEngine;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The win beat of every Level 1 mini-game (2026-11-17 playtest): fireworks burst over the festival town, Pixel
    /// "learns" the topic (one of the five paper lanterns over its head lights up in the topic's colour, a happy hop, and
    /// "PIXEL LEARNED ...!"), and a topic card pops up in front of the rider with a little more to read, then flies onto the
    /// card rail on the cart's left side. The rider can shoot a card on the rail later to read it again. It only shows:
    /// the games decide the win, the director decides when the ride moves on.
    /// </summary>
    public sealed class WinCelebrationView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private PixelBuddyView pixel;

        [Header("Which game earns which card (0 Pixels .. 4 Training)")]
        [SerializeField] private RideGame[] cardGames = new RideGame[0];
        [SerializeField] private string[] titles = new string[0];
        [SerializeField] private string[] ideas = new string[0];
        [SerializeField, TextArea] private string[] facts = new string[0];
        [SerializeField] private Color[] colors = new Color[0];

        [Header("The big card in front of the rider")]
        [SerializeField] private Transform bigCard;
        [SerializeField] private TextMeshPro header;
        [SerializeField] private TextMeshPro title;
        [SerializeField] private TextMeshPro idea;
        [SerializeField] private TextMeshPro body;
        [SerializeField] private Renderer border;

        [Header("The card rail and Pixel's brain lanterns")]
        [SerializeField] private TopicCardSlot[] slots = new TopicCardSlot[0];
        [SerializeField] private GameObject[] slotFaces = new GameObject[0];
        [Tooltip("Pixel's five brain lanterns in one mesh (each lantern the same number of vertices), dark until learned.")]
        [SerializeField] private MeshFilter brainLanterns;
        [Tooltip("Their five halos in one glow mesh (four vertices each), strength 0 until learned.")]
        [SerializeField] private MeshFilter brainHalos;
        [SerializeField] private TextMeshPro learned;

        [Header("Fireworks")]
        [SerializeField] private ParticleSystem fireworks;
        [SerializeField] private AudioSource boomSource;
        [SerializeField] private int sparksPerBurst = 110;

        [Header("More (2026-11-17 ride)")]
        [Tooltip("\"Send your 3\" is not a scored game; its end earns card 3.")]
        [SerializeField] private RideMoment sendMoment;
        [SerializeField] private int sendCard = 2;
        [Tooltip("The big card's own target: shooting it closes it.")]
        [SerializeField] private TopicCardSlot closer;
        [SerializeField] private float cardHoldSeconds = 16f;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private RideGame _game;
        private Vector3 _cardHome;
        private Quaternion _cardHomeRotation;
        private Vector3 _cardHomeScale;
        private Coroutine _showing;
        private MaterialPropertyBlock _block;
        private AudioClip _boom;
        private float _learnedAt = -100f;
        private Color[] _paper;
        private Color[] _halo;

        private static readonly Color Gold = new Color(1f, 0.82f, 0.4f);

        private void Awake()
        {
            if (bigCard != null)
            {
                _cardHome = bigCard.localPosition;
                _cardHomeRotation = bigCard.localRotation;
                _cardHomeScale = bigCard.localScale;
                bigCard.gameObject.SetActive(false);
            }

            foreach (GameObject g in slotFaces) if (g != null) g.SetActive(false);
            if (brainLanterns != null) _paper = brainLanterns.mesh.colors;
            if (brainHalos != null) _halo = brainHalos.mesh.colors;
            if (learned != null) learned.gameObject.SetActive(false);
            _boom = MakeBoom();
        }

        private void OnEnable()
        {
            foreach (TopicCardSlot s in slots) if (s != null) s.Opened += OnOpened;
            if (director != null) director.SegmentStarted += OnSegmentStarted;
            if (director != null) director.Narrated += OnNarrated;
            if (sendMoment != null) sendMoment.Finished += OnSent;
            if (closer != null)
            {
                closer.Earned = true;
                closer.Opened += OnClose;
            }
        }

        private void OnDisable()
        {
            foreach (TopicCardSlot s in slots) if (s != null) s.Opened -= OnOpened;
            if (director != null) director.SegmentStarted -= OnSegmentStarted;
            if (director != null) director.Narrated -= OnNarrated;
            if (sendMoment != null) sendMoment.Finished -= OnSent;
            if (closer != null) closer.Opened -= OnClose;
            Watch(null);
        }

        private void Update()
        {
            if (director == null) return;
            Watch(director.ActiveMoment as RideGame);
            if (learned != null && learned.gameObject.activeSelf)
            {
                float t = Time.time - _learnedAt;
                learned.transform.localScale = Vector3.one * Mathf.Min(1f, t * 5f);
                if (t > 3.5f) learned.gameObject.SetActive(false);
            }
        }

        private void Watch(RideGame game)
        {
            if (game == _game) return;
            if (_game != null) _game.Won -= OnWon;
            _game = game;
            if (_game != null) _game.Won += OnWon;
        }

        private void OnWon(int stars, bool byRider)
        {
            int index = System.Array.IndexOf(cardGames, _game);
            StartCoroutine(Fireworks(index >= 0 && index < colors.Length ? colors[index] : Gold, byRider ? 4 : 2));
            if (index < 0 || index >= slots.Length || slots[index] == null || slots[index].Earned) return;
            if (_showing != null) StopCoroutine(_showing);
            _showing = StartCoroutine(Earn(index));
        }

        private void OnSent()
        {
            StartCoroutine(Fireworks(sendCard < colors.Length ? colors[sendCard] : Gold, 3));
            if (sendCard >= slots.Length || slots[sendCard] == null || slots[sendCard].Earned) return;
            if (_showing != null) StopCoroutine(_showing);
            _showing = StartCoroutine(Earn(sendCard));
        }

        private bool _closeRequested;

        private void OnClose(int _) => _closeRequested = true;

        // ---------- the big reveal: Pixel reads the rider's own 3 ----------

        private AudioClip _drum, _crash;

        private void OnNarrated(RideLine line)
        {
            if (boomSource == null) return;
            if (line == RideLine.RightYours)
            {
                if (_drum == null) _drum = MakeDrumRoll();
                boomSource.Stop();
                boomSource.clip = _drum;
                boomSource.volume = 0.6f;
                boomSource.PlayDelayed(1.2f);
            }
            else if (line == RideLine.RightYes || line == RideLine.RightNowYes)
            {
                boomSource.Stop();
                if (_crash == null) _crash = MakeCrash();
                boomSource.PlayOneShot(_crash, 0.8f);
                StartCoroutine(Fireworks(Gold, 6));
                if (pixel != null) pixel.Learn();
            }
            else if (line == RideLine.RightStudy) boomSource.Stop();
        }

        /// <summary>A snare roll that swells for about three seconds (made once, no asset).</summary>
        private static AudioClip MakeDrumRoll()
        {
            const int rate = 44100;
            int n = (int)(rate * 3.4f);
            var data = new float[n];
            var rnd = new System.Random(3);
            float hit = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float period = Mathf.Lerp(0.075f, 0.04f, t / 3.4f);
                if (Mathf.Repeat(t, period) < 1f / rate * 1.5f) hit = 1f;
                hit *= 0.9993f;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float swell = Mathf.Lerp(0.25f, 1f, t / 3.4f);
                data[i] = noise * hit * swell * 0.55f + Mathf.Sin(2f * Mathf.PI * 180f * t) * hit * swell * 0.15f;
            }

            AudioClip clip = AudioClip.Create("DrumRoll", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>A cymbal crash for the reveal.</summary>
        private static AudioClip MakeCrash()
        {
            const int rate = 44100;
            int n = (int)(rate * 1.8f);
            var data = new float[n];
            var rnd = new System.Random(11);
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float bright = noise - prev * 0.6f;
                prev = noise;
                data[i] = bright * Mathf.Exp(-t * 2.2f) * 0.45f;
            }

            AudioClip clip = AudioClip.Create("Crash", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnOpened(int index)
        {
            if (_showing != null) StopCoroutine(_showing);
            _showing = StartCoroutine(Reread(index));
        }

        private void OnSegmentStarted(int segment)
        {
            // A card opened from the rail closes when the ride moves on (an earned card still flies home on its own).
            if (_showing == null && bigCard != null) bigCard.gameObject.SetActive(false);
        }

        // ---------- the card ----------

        private IEnumerator Earn(int index)
        {
            yield return new WaitForSeconds(1.0f);
            LightBrain(index);
            _closeRequested = false;
            ShowCard(index, "NEW CARD!  " + (index + 1) + " OF 5   (shoot to close)");
            yield return Pop(bigCard, _cardHomeScale);

            // The "read me" moment: the card stays up until the rider pulls GO (or the ride moves on by itself), at least a few
            // seconds, at most the hold.
            float held = 0f;
            bool sawGo = false;
            int segment = director.SegmentIndex;
            while (held < cardHoldSeconds)
            {
                if (director.WaitingForGo) sawGo = true;
                if (_closeRequested || (held > 3.5f && ((sawGo && !director.WaitingForGo) || director.SegmentIndex != segment))) break;
                held += Time.deltaTime;
                yield return null;
            }

            // Fly onto the rail.
            Transform slot = slots[index].transform;
            Vector3 from = bigCard.position;
            Quaternion fromRotation = bigCard.rotation;
            Vector3 fromScale = bigCard.localScale;
            Vector3 toScale = Vector3.Scale(_cardHomeScale, new Vector3(0.3f, 0.3f, 1f));
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.7f)
            {
                float e = t * t * (3f - 2f * t);
                bigCard.position = Vector3.Lerp(from, slot.position, e) + Vector3.up * (Mathf.Sin(e * Mathf.PI) * 0.12f);
                bigCard.rotation = Quaternion.Slerp(fromRotation, slot.rotation, e);
                bigCard.localScale = Vector3.Lerp(fromScale, toScale, e);
                yield return null;
            }

            HideCard();
            slots[index].Earned = true;
            if (index < slotFaces.Length && slotFaces[index] != null)
            {
                slotFaces[index].SetActive(true);
                yield return Pop(slotFaces[index].transform, slotFaces[index].transform.localScale);
            }

            _showing = null;
        }

        private IEnumerator Reread(int index)
        {
            ShowCard(index, "CARD " + (index + 1) + " OF 5");
            yield return Pop(bigCard, _cardHomeScale);
            _closeRequested = false;
            for (float t = 0f; t < 12f && !_closeRequested; t += Time.deltaTime) yield return null;
            HideCard();
            _showing = null;
        }

        private void ShowCard(int index, string top)
        {
            if (bigCard == null) return;
            bigCard.localPosition = _cardHome;
            bigCard.localRotation = _cardHomeRotation;
            bigCard.localScale = _cardHomeScale;
            Color c = index < colors.Length ? colors[index] : Gold;
            if (header != null) header.text = top;
            if (title != null)
            {
                title.text = index < titles.Length ? titles[index] : "";
                title.color = c;
            }

            if (idea != null) idea.text = index < ideas.Length ? ideas[index] : "";
            if (body != null) body.text = index < facts.Length ? facts[index] : "";
            if (border != null)
            {
                if (_block == null) _block = new MaterialPropertyBlock();
                border.GetPropertyBlock(_block);
                _block.SetColor(BaseColor, c);
                border.SetPropertyBlock(_block);
            }

            bigCard.gameObject.SetActive(true);
        }

        private void HideCard()
        {
            if (bigCard == null) return;
            bigCard.gameObject.SetActive(false);
            bigCard.localPosition = _cardHome;
            bigCard.localRotation = _cardHomeRotation;
            bigCard.localScale = _cardHomeScale;
        }

        private static IEnumerator Pop(Transform t, Vector3 size)
        {
            if (t == null) yield break;
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.35f)
            {
                // Overshoot a little, then settle.
                float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.18f;
                t.localScale = size * Mathf.Min(1f, k * 2.2f) * s;
                yield return null;
            }

            t.localScale = size;
        }

        // ---------- Pixel learns ----------

        private void LightBrain(int index)
        {
            if (pixel != null) pixel.Learn();
            Color c = index < colors.Length ? colors[index] : Gold;
            if (_paper != null && _paper.Length > 0)
            {
                int per = _paper.Length / 5;
                for (int v = index * per; v < (index + 1) * per; v++) _paper[v] = new Color(c.r, c.g, c.b, 1f);
                brainLanterns.mesh.colors = _paper;
            }

            if (_halo != null && _halo.Length >= (index + 1) * 4)
            {
                for (int v = index * 4; v < index * 4 + 4; v++) _halo[v] = new Color(c.r, c.g, c.b, 0.85f);
                brainHalos.mesh.colors = _halo;
            }

            if (learned != null)
            {
                learned.text = "PIXEL LEARNED\n" + (index < titles.Length ? titles[index] : "") + "!";
                learned.color = index < colors.Length ? colors[index] : Gold;
                learned.gameObject.SetActive(true);
                _learnedAt = Time.time;
            }
        }

        // ---------- fireworks ----------

        private IEnumerator Fireworks(Color topic, int bursts)
        {
            if (fireworks == null || director == null || director.Pod == null) yield break;
            Transform pod = director.Pod;
            for (int b = 0; b < bursts; b++)
            {
                Vector3 at = pod.position + pod.forward * Random.Range(24f, 34f) + pod.right * Random.Range(-15f, 15f) + Vector3.up * Random.Range(11f, 17f);
                Color c = b % 2 == 0 ? topic : Color.Lerp(Gold, Color.white, Random.Range(0f, 0.4f));
                Burst(at, c);
                if (boomSource != null && _boom != null) boomSource.PlayOneShot(_boom, Random.Range(0.55f, 0.8f));
                yield return new WaitForSeconds(Random.Range(0.35f, 0.7f));
            }
        }

        private void Burst(Vector3 at, Color color)
        {
            var p = new ParticleSystem.EmitParams { position = at, applyShapeToPosition = false };
            for (int i = 0; i < sparksPerBurst; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                p.velocity = dir * Random.Range(6.5f, 8.5f);
                p.startColor = i % 7 == 0 ? Color.white : color;
                p.startLifetime = Random.Range(1.3f, 2.0f);
                p.startSize = Random.Range(0.28f, 0.42f);
                fireworks.Emit(p, 1);
            }
        }

        /// <summary>A soft distant firework: a low thump and a crackle tail, made once (no asset).</summary>
        private static AudioClip MakeBoom()
        {
            const int rate = 44100;
            int n = (int)(rate * 1.4f);
            var data = new float[n];
            var rnd = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float thump = Mathf.Sin(2f * Mathf.PI * (55f + 30f * Mathf.Exp(-t * 18f)) * t) * Mathf.Exp(-t * 7f) * 0.8f;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float crackle = t > 0.18f && rnd.NextDouble() < 0.012 ? noise * Mathf.Exp(-(t - 0.18f) * 2.6f) * 0.9f : 0f;
                float hiss = noise * Mathf.Exp(-t * 14f) * 0.15f;
                data[i] = Mathf.Clamp(thump + crackle + hiss, -1f, 1f) * 0.7f;
            }

            AudioClip clip = AudioClip.Create("FireworkBoom", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
