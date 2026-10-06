using System.Collections;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// When a stop is solved the equation assembles itself from the numbers the player just set, one piece at a
    /// time. Touched numbers are orange, locked ones cyan. The text is plain ASCII plus the dot and times signs,
    /// because the project font has no Greek letters or arrows.
    /// </summary>
    public sealed class EquationRevealView : MonoBehaviour
    {
        [SerializeField] private StationController station;
        [SerializeField] private RideTheme theme;
        [SerializeField] private TMP_Text text;
        [SerializeField] private GameObject plate;
        [SerializeField] private float pieceSeconds = 0.8f;

        private Coroutine _routine;

        private void OnEnable()
        {
            if (station != null) station.Solved += OnSolved;
            Hide();
        }

        private void OnDisable()
        {
            if (station != null) station.Solved -= OnSolved;
        }

        private void OnSolved(StationSummary s)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Reveal(Pieces(s)));
        }

        private void Hide()
        {
            if (text != null) text.text = string.Empty;
            if (plate != null) plate.SetActive(false);
        }

        private IEnumerator Reveal(string[] pieces)
        {
            plate.SetActive(true);
            var sb = new StringBuilder();
            for (int i = 0; i < pieces.Length; i++)
            {
                sb.Append(pieces[i]);
                text.text = sb.ToString();
                yield return new WaitForSeconds(pieceSeconds);
            }
        }

        private string[] Pieces(StationSummary s)
        {
            string white = Hex(theme.offWhite);
            string touched = Hex(theme.orange);
            string fixedColor = Hex(theme.cyan);

            switch (s.Kind)
            {
                case StationKind.OneSignal:
                    return new[]
                    {
                        $"<color=#{white}>fire if</color>  ",
                        $"<color=#{fixedColor}>{Num(s.RockWeight)}</color> <color=#{white}>× rock</color>  ",
                        $"<color=#{white}>>=</color>  ",
                        $"<color=#{touched}>{Num(s.Trigger)}</color>"
                    };
                case StationKind.TwoSignals:
                    return new[]
                    {
                        $"<color=#{white}>fire if</color>  ",
                        $"<color=#{touched}>{Num(s.RockWeight)}</color> <color=#{white}>× rock</color>  ",
                        $"<color=#{white}>+</color>  <color=#{touched}>{Num(s.IceWeight)}</color> <color=#{white}>× ice</color>  ",
                        $"<color=#{white}>>=</color>  <color=#{touched}>{Num(s.Trigger)}</color>\n",
                        $"<size=70%><color=#{white}>fire if  w1·x1 + w2·x2 + b  >=  0</color></size>"
                    };
                default:
                    return new[]
                    {
                        $"<color=#{white}>w  <-  w  -  </color>",
                        $"<color=#{touched}>{Num(s.LearningRate)}</color> ",
                        $"<color=#{white}>× slope</color>\n",
                        $"<size=70%><color=#{white}>new weight = old weight - learning rate × slope of the error</color></size>"
                    };
            }
        }

        private static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        private static string Num(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
