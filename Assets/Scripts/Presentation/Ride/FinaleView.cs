using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Convergence.Gameplay.Hub;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The ending: when the ride completes, a panel with one check mark per stop pops up in front of the pod, then
    /// "to be continued", then the credits (every voice, music and sound source with its license). When a next scene is named, GO lights
    /// once the credits are up and loads it (Level 1 leads to Level 2 and Level 2 back to Level 1), so a booth never needs a menu.
    /// </summary>
    public sealed class FinaleView : MonoBehaviour
    {
        [SerializeField] private RideDirector director;
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject toBeContinued;
        [SerializeField] private GameObject credits;
        [SerializeField] private float popSeconds = 0.6f;
        [SerializeField] private float continuedDelay = 2.5f;
        [SerializeField] private float creditsDelay = 5f;
        [Tooltip("The scene GO loads once the end card is complete (empty: the ride just ends).")]
        [SerializeField] private string nextScene = "";
        [SerializeField] private GameObject goHint;
        [SerializeField] private float goWaitSeconds = 900f;
        [Tooltip("Signs behind the end card that would peek out around it (the accuracy sign); off when it pops up.")]
        [SerializeField] private GameObject[] hideWhenDone = new GameObject[0];
        [Tooltip("Optional: a banner over the card with this ride's points and stars, and NEW RECORD when it beat the hub's scoreboard (ADR-024).")]
        [SerializeField] private TextMeshPro scoreBanner;

        private float _age = -1f;

        private void OnEnable()
        {
            if (scoreBanner != null) scoreBanner.gameObject.SetActive(false);
            if (panel != null) panel.SetActive(false);
            if (toBeContinued != null) toBeContinued.SetActive(false);
            if (credits != null) credits.SetActive(false);
            if (goHint != null) goHint.SetActive(false);
            if (director != null) director.RideCompleted += OnCompleted;
        }

        private void OnDisable()
        {
            if (director != null) director.RideCompleted -= OnCompleted;
        }

        private void OnCompleted()
        {
            _age = 0f;
            panel.transform.localScale = Vector3.zero;
            panel.SetActive(true);
            foreach (GameObject sign in hideWhenDone)
            {
                if (sign != null) sign.SetActive(false);
            }

            // A next scene missing from the build (a level built on its own) ends the ride on the card instead of failing to load.
            if (!string.IsNullOrEmpty(nextScene) && Application.CanStreamedLevelBeLoaded(nextScene)) StartCoroutine(GoToNext());
        }

        /// <summary>The ride's result, from the scoreboard the ride just wrote (it is written when the ride completes, before this shows).</summary>
        private void ShowScore()
        {
            if (scoreBanner == null || LevelProgressRecorder.LastLevel != gameObject.scene.name) return;
            string stars = LevelProgressRecorder.LastMaxStars > 0 ? LevelProgressRecorder.LastStars + " / " + LevelProgressRecorder.LastMaxStars : LevelProgressRecorder.LastStars.ToString();
            scoreBanner.text = LevelProgressRecorder.LastScore + " POINTS   " + stars + " STARS" + (LevelProgressRecorder.LastWasBest ? "   NEW RECORD!" : "");
            scoreBanner.gameObject.SetActive(true);
        }

        private IEnumerator GoToNext()
        {
            yield return new WaitForSeconds(creditsDelay + 1f);
            if (goHint != null) goHint.SetActive(true);
            yield return director.WaitForGo(goWaitSeconds);
            SceneManager.LoadScene(nextScene);
        }

        private void Update()
        {
            if (_age < 0f) return;
            _age += Time.deltaTime;
            if (toBeContinued != null && !toBeContinued.activeSelf && _age >= continuedDelay)
            {
                toBeContinued.SetActive(true);
                ShowScore();
            }

            if (credits != null && !credits.activeSelf && _age >= creditsDelay) credits.SetActive(true);
            if (_age > popSeconds) return;
            float t = Mathf.Clamp01(_age / popSeconds);
            float overshoot = 1f + 0.15f * Mathf.Sin(t * Mathf.PI);
            panel.transform.localScale = Vector3.one * (t * overshoot);
        }
    }
}
