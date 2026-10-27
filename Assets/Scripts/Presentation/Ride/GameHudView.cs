using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The game side of the dash (Level 1 v5): while a mini-game runs, its title, goal, a progress bar and a countdown;
    /// the ride's score all the time. When a game is won a big "STOP CLEAR!" stamp pops up in front of the rider with
    /// one to three stars, confetti bursts and both hands buzz (through the sfx view); then "PULL GO" flashes until the
    /// rider moves on. Display only: it reads the running <see cref="RideGame"/> and the director.
    /// </summary>
    public sealed class GameHudView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshPro title;
        [SerializeField] private TextMeshPro goal;
        [SerializeField] private TextMeshPro timer;
        [SerializeField] private TextMeshPro score;
        [SerializeField] private Transform progressFill;
        [SerializeField] private GameObject stamp;
        [SerializeField] private TextMeshPro stampText;
        [SerializeField] private Renderer[] stars = new Renderer[0];
        [SerializeField] private Material starLit;
        [SerializeField] private Material starDim;
        [SerializeField] private ParticleSystem confetti;
        [SerializeField] private TextMeshPro popup;
        [Tooltip("Where a +N pops up when the points have no place of their own (a whole-game bonus).")]
        [SerializeField] private Transform popupAnchor;
        [SerializeField] private ZapGame zap;
        [SerializeField] private float stampSeconds = 4.5f;
        [Tooltip("How long \"BLAST THE 3s!\" stays up when digits start flying.")]
        [SerializeField] private float blastSeconds = 2.4f;

        private const float PopupSeconds = 0.9f;
        private RideGame _game;
        private float _popupLife = PopupSeconds;
        private int _blastSaidAt = int.MinValue;
        private bool _popupInPod;
        private float _shownFill;
        private float _stampAt = -100f;
        private int _starsShown;
        private float _popupAt = -100f;
        private Vector3 _popupFrom;
        private int _shownScore = -1;
        private Color _popupColor = Color.white;
        private static readonly Color WrongColor = new Color(1f, 0.38f, 0.33f);

        private void Start()
        {
            if (panel != null) panel.SetActive(false);
            if (stamp != null) stamp.SetActive(false);
            if (popup != null)
            {
                _popupColor = popup.color;
                popup.gameObject.SetActive(false);
            }

            if (score != null) score.text = "";
        }

        private void OnEnable()
        {
            if (zap != null)
            {
                zap.Shot += OnShot;
                zap.SpeckSpawned += OnSpeckSpawned;
            }
        }

        private void OnDisable()
        {
            Watch(null);
            if (zap != null)
            {
                zap.Shot -= OnShot;
                zap.SpeckSpawned -= OnSpeckSpawned;
            }
        }

        // The first digit of every travel leg: "BLAST THE 3s!" straight ahead, so the game between stops says what it is.
        private void OnSpeckSpawned(Vector3 where)
        {
            if (popup == null || director == null || director.SegmentIndex == _blastSaidAt) return;
            _blastSaidAt = director.SegmentIndex;
            Camera eye = Camera.main;
            if (eye == null) return;
            Vector3 ahead = Vector3.ProjectOnPlane(eye.transform.forward, Vector3.up).normalized;
            if (ahead.sqrMagnitude < 0.01f) ahead = director.Pod.forward;
            popup.text = "BLAST THE " + zap.TargetLabel + "s!";
            popup.color = _popupColor;
            _popupAt = Time.time;
            _popupLife = blastSeconds;
            // Held in the pod's frame: the pod travels at several metres a second and would fly through a world-space sign.
            _popupFrom = director.Pod.InverseTransformPoint(eye.transform.position + ahead * 3f + Vector3.up * 0.15f);
            _popupInPod = true;
            popup.gameObject.SetActive(true);
        }

        // Blast the 3s: "+10", "+20 x2" on a streak, "NOT A 3  -10" in red for a mistake.
        private void OnShot(Vector3 where, bool right, int points)
        {
            if (popup == null) return;
            int multiplier = zap.LastMultiplier;
            popup.text = right ? (multiplier > 1 ? "+" + points + "  x" + multiplier : "+" + points) : "NOT A " + zap.TargetLabel + (points < 0 ? "  " + points : "");
            popup.color = right ? _popupColor : WrongColor;
            _popupAt = Time.time;
            _popupLife = PopupSeconds;
            _popupInPod = false;
            _popupFrom = where;
            popup.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (director == null) return;
            Watch(director.ActiveMoment as RideGame);

            bool show = _game != null || director.WaitingForGo;
            if (panel != null && panel.activeSelf != show) panel.SetActive(show);
            if (show) ShowGame();

            if (score != null && director.Score != _shownScore && director.SegmentIndex >= 0)
            {
                _shownScore = director.Score;
                score.text = "SCORE  " + _shownScore;
            }

            AnimateStamp();
            AnimatePopup();
        }

        private void ShowGame()
        {
            if (_game != null)
            {
                if (title != null) title.text = _game.Title;
                bool waiting = director.WaitingForGo;
                if (goal != null)
                {
                    goal.text = waiting ? (Mathf.Repeat(Time.time, 0.8f) < 0.5f ? "PULL GO  or press A" : "") : _game.Goal;
                    goal.color = waiting ? new Color(1f, 0.55f, 0.15f) : Color.white;
                }

                float left = _game.SecondsLeft;
                if (timer != null)
                {
                    timer.text = left >= 0f ? Mathf.CeilToInt(left).ToString() : "";
                    timer.color = left >= 0f && left < 6f ? new Color(1f, 0.3f, 0.3f) : Color.white;
                }

                _shownFill = Mathf.MoveTowards(_shownFill, _game.Progress, Time.deltaTime * 1.5f);
            }

            if (progressFill != null) progressFill.localScale = new Vector3(Mathf.Max(0.001f, _shownFill), 1f, 1f);
        }

        private void Watch(RideGame game)
        {
            if (game == _game) return;
            if (_game != null)
            {
                _game.Won -= OnWon;
                _game.Scored -= OnScored;
            }

            _game = game;
            _shownFill = 0f;
            if (_game == null) return;
            _game.Won += OnWon;
            _game.Scored += OnScored;
        }

        private void OnWon(int starCount, bool byRider)
        {
            _stampAt = Time.time;
            _starsShown = starCount;
            if (stampText != null) stampText.text = byRider ? "STOP CLEAR!" : "STOP CLEAR";
            if (stamp != null) stamp.SetActive(true);
            for (int i = 0; i < stars.Length; i++) stars[i].sharedMaterial = starDim;
            if (confetti != null && byRider) confetti.Play();
        }

        private void OnScored(int points, Vector3 where)
        {
            if (popup == null || points <= 0) return;
            popup.text = "+" + points;
            popup.color = _popupColor;
            _popupAt = Time.time;
            _popupLife = PopupSeconds;
            _popupInPod = false;
            _popupFrom = where == Vector3.zero && popupAnchor != null ? popupAnchor.position : where;
            popup.gameObject.SetActive(true);
        }

        private void AnimateStamp()
        {
            if (stamp == null || !stamp.activeSelf) return;
            float t = Time.time - _stampAt;
            if (t > stampSeconds && !director.WaitingForGo)
            {
                stamp.SetActive(false);
                return;
            }

            // Pop in with an overshoot, then breathe.
            float pop = t < 0.35f ? Mathf.Sin(t / 0.35f * Mathf.PI * 0.5f) * 1.18f : 1f + 0.18f * Mathf.Exp(-(t - 0.35f) * 7f) * Mathf.Cos((t - 0.35f) * 20f);
            stamp.transform.localScale = Vector3.one * Mathf.Max(0.01f, pop * (1f + 0.03f * Mathf.Sin(t * 4f)));
            for (int i = 0; i < stars.Length; i++)
            {
                bool lit = i < _starsShown && t > 0.45f + i * 0.3f;
                if (stars[i].sharedMaterial != (lit ? starLit : starDim)) stars[i].sharedMaterial = lit ? starLit : starDim;
                float s = lit ? 1f + 0.4f * Mathf.Exp(-(t - 0.45f - i * 0.3f) * 8f) : 0.8f;
                stars[i].transform.localScale = Vector3.one * s;
            }
        }

        private void AnimatePopup()
        {
            if (popup == null || !popup.gameObject.activeSelf) return;
            float t = Time.time - _popupAt;
            if (t > _popupLife)
            {
                popup.gameObject.SetActive(false);
                return;
            }

            Vector3 from = _popupInPod && director != null ? director.Pod.TransformPoint(_popupFrom) : _popupFrom;
            popup.transform.position = from + Vector3.up * (0.05f + t * 0.25f * PopupSeconds / _popupLife);

            // Same angular size wherever it pops: a blasted digit 6 m out gets text as readable as one on the table.
            Camera eye = Camera.main;
            float distance = eye != null ? Vector3.Distance(eye.transform.position, popup.transform.position) : 0.7f;
            popup.transform.localScale = Vector3.one * Mathf.Max(1f, distance / 0.7f);
            Color c = popup.color;
            c.a = Mathf.Clamp01((_popupLife - t) / 0.9f);
            popup.color = c;
        }
    }
}
