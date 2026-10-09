using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The classic neural-network picture: input circles, lines whose thickness and color follow the weights,
    /// one neuron circle, and an output circle that lights when the neuron fires. It is the same neuron the rest of
    /// the stop shows as a machine, drawn the way a textbook draws it. It displays Gameplay's results and decides nothing.
    /// </summary>
    public sealed class NetworkDiagramView : MonoBehaviour
    {
        [SerializeField] private StationController station;
        [SerializeField] private RideTheme theme;
        [SerializeField] private LineRenderer rockLine;
        [SerializeField] private LineRenderer iceLine;
        [SerializeField] private TMP_Text rockLabel;
        [SerializeField] private TMP_Text iceLabel;
        [SerializeField] private GameObject iceParts;
        [SerializeField] private Renderer rockNode;
        [SerializeField] private Renderer iceNode;
        [SerializeField] private Renderer outputNode;
        [SerializeField] private Material positiveLine;
        [SerializeField] private Material negativeLine;
        [SerializeField] private Material zeroLine;
        [SerializeField] private Material nodeOff;
        [SerializeField] private Material nodeOn;
        [SerializeField] private float holdSeconds = 0.9f;

        private RideControlChannel _rock;
        private RideControlChannel _ice;

        private void OnEnable()
        {
            if (station == null || station.Controls == null) return;
            _rock = station.Controls.Get(RideControlIds.Rock);
            _ice = station.Controls.Get(RideControlIds.Ice);
            _rock.Changed += OnChanged;
            _ice.Changed += OnChanged;
            _ice.StateChanged += OnState;
            station.CaseFed += OnCase;
            ClearNodes();
            Refresh();
        }

        private void OnDisable()
        {
            if (_rock != null) _rock.Changed -= OnChanged;
            if (_ice != null)
            {
                _ice.Changed -= OnChanged;
                _ice.StateChanged -= OnState;
            }

            if (station != null) station.CaseFed -= OnCase;
            StopAllCoroutines();
        }

        private void OnChanged(RideControlChannel changed, bool byUser) => Refresh();

        private void OnState(RideControlChannel changed) => Refresh();

        private void Refresh()
        {
            Style(rockLine, rockLabel, _rock);
            Style(iceLine, iceLabel, _ice);
            if (iceParts != null) iceParts.SetActive(_ice.Visible);
        }

        private void Style(LineRenderer line, TMP_Text label, RideControlChannel channel)
        {
            float w = channel.Value;
            float width = 0.012f + 0.05f * Mathf.Abs(w);
            line.startWidth = width;
            line.endWidth = width;
            line.sharedMaterial = w > 0.001f ? positiveLine : w < -0.001f ? negativeLine : zeroLine;
            label.text = "×" + w.ToString("0.0", CultureInfo.InvariantCulture); // one line, written like the pipes' labels at stop 1
            label.color = w < 0f ? theme.amber : theme.cyan;
        }

        private void OnCase(CaseOutcome outcome)
        {
            StartCoroutine(Flash(outcome));
        }

        private IEnumerator Flash(CaseOutcome outcome)
        {
            yield return new WaitForSeconds((theme.spawnX - theme.scanX) / theme.streamSpeed);
            if (outcome.RockSignal >= 0.5) rockNode.sharedMaterial = nodeOn;
            if (outcome.IceSignal >= 0.5 && _ice.Visible) iceNode.sharedMaterial = nodeOn;
            yield return new WaitForSeconds(theme.pulseTravelSeconds);
            outputNode.sharedMaterial = outcome.Fired ? nodeOn : nodeOff;
            yield return new WaitForSeconds(holdSeconds);
            ClearNodes();
        }

        private void ClearNodes()
        {
            rockNode.sharedMaterial = nodeOff;
            iceNode.sharedMaterial = nodeOff;
            outputNode.sharedMaterial = nodeOff;
        }
    }
}
