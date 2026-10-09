using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The big chapter card in front of the pod while a briefing plays: the year, a title, and one plain sentence of
    /// the story of AI. It pops in when the briefing starts and out when it ends, so the lesson has something to look
    /// at during the ride. The outro gets the last card. Observes the director only.
    /// </summary>
    public sealed class ChapterCardView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private DashScreenView dash;
        [SerializeField] private NarrationLibrary library;

        [Tooltip("In the outro the last card waits for the clause with this cue (the 1969 line), so it never shows during the recap.")]
        [SerializeField] private string outroCue = "xor";
        [SerializeField] private GameObject card;
        [SerializeField] private TMP_Text year;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text blurb;
        [SerializeField] private float popSeconds = 0.45f;

        [Tooltip("The station that just ended lingers for a moment; the card waits so the two never overlap.")]
        [SerializeField] private float showDelaySeconds = 2.8f;

        private float _scale;
        private float _target;
        private int _pending = -1;
        private float _showAt;
        private bool _awaitOutroCue;

        /// <summary>The card started to pop in (after its delay), so a sound can land with it.</summary>
        public event System.Action Shown;

        private void OnEnable()
        {
            _scale = 0f;
            _target = 0f;
            _pending = -1;
            if (card != null)
            {
                card.transform.localScale = Vector3.zero;
                card.SetActive(false);
            }

            _awaitOutroCue = false;
            if (dash != null) dash.SegmentShown += OnSegment;
            if (director == null) return;
            director.BriefingStarted += OnBriefingStarted;
            director.BriefingEnded += OnBriefingEnded;
            director.OutroStarted += OnOutroStarted;
            director.RideCompleted += Hide;
            director.NarrationSkipped += Hide;
        }

        private void OnDisable()
        {
            if (dash != null) dash.SegmentShown -= OnSegment;
            if (director == null) return;
            director.BriefingStarted -= OnBriefingStarted;
            director.BriefingEnded -= OnBriefingEnded;
            director.OutroStarted -= OnOutroStarted;
            director.RideCompleted -= Hide;
            director.NarrationSkipped -= Hide;
        }

        private void OnBriefingStarted(int stopIndex) => ShowSoon(stopIndex);

        private void OnBriefingEnded(int stopIndex) => Hide();

        private void OnOutroStarted()
        {
            if (library == null) return;
            if (dash != null) _awaitOutroCue = true; // shown with the 1969 line, not over the recap
            else ShowSoon(library.chapters.Length - 1);
        }

        private void OnSegment(RideLine line, int index, NarrationLibrary.Segment segment)
        {
            if (!_awaitOutroCue || !NarrationLibrary.HasCue(segment, outroCue)) return;
            _awaitOutroCue = false;
            _pending = -1;
            Show(library.chapters.Length - 1);
        }

        private void ShowSoon(int chapterIndex)
        {
            _pending = chapterIndex;
            _showAt = Time.time + showDelaySeconds;
        }

        private void Show(int chapterIndex)
        {
            if (library == null || card == null || chapterIndex < 0 || chapterIndex >= library.chapters.Length) return;
            NarrationLibrary.Chapter chapter = library.chapters[chapterIndex];
            if (year != null) year.text = chapter.year;
            if (title != null) title.text = chapter.title;
            if (blurb != null) blurb.text = chapter.blurb;
            card.SetActive(true);
            _target = 1f;
            Shown?.Invoke();
        }

        private void Hide()
        {
            _pending = -1;
            _target = 0f;
        }

        private void Update()
        {
            if (_pending >= 0 && Time.time >= _showAt)
            {
                int chapter = _pending;
                _pending = -1;
                Show(chapter);
            }

            if (card == null || !card.activeSelf) return;
            _scale = Mathf.MoveTowards(_scale, _target, Time.deltaTime / Mathf.Max(0.01f, popSeconds));
            float eased = _scale * _scale * (3f - 2f * _scale);
            card.transform.localScale = Vector3.one * eased;
            if (_scale <= 0f && _target <= 0f) card.SetActive(false);
        }
    }
}
