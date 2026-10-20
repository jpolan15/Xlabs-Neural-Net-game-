using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>
    /// E3, the network (the classic: 3Blue1Brown's layered diagram). The 49 pixels of the 3 wait on the left; one
    /// neuron appears and copies itself into the 16 of the hidden layer (LAYER); the ten answers 0 to 9 line up on the
    /// right; every wire draws itself in a cascade, coloured by the sign of its real untrained weight and as strong as
    /// its size (NEURAL NETWORK, "Pixel's brain"). The diagram is the giant network seen from close up: it is laid out
    /// in the network's own coordinates at a fifth of its size on the line from the rider's eye, so it covers exactly
    /// the giant network, and on {race} it grows along that line into it (the builder's Morph unfold).
    /// </summary>
    public sealed class NetworkExplainer : Explainer
    {
        [SerializeField] private ShapeBatch inputs;
        [SerializeField] private ShapeBatch layer;
        [SerializeField] private ShapeBatch outputs;
        [SerializeField] private ShapeBatch edges;
        [SerializeField] private TMP_Text[] labels = new TMP_Text[0];
        [SerializeField] private TMP_Text layerTitle;
        [SerializeField] private TMP_Text title;
        [SerializeField] private int firstLayerEdges = 784;

        protected override void ResetPieces()
        {
            Hide(inputs, false);
            Hide(layer, false);
            Hide(outputs, false);
            Hide(edges, true);
            var texts = new List<TMP_Text>(labels) { layerTitle, title };
            foreach (TMP_Text t in texts)
            {
                t.gameObject.SetActive(false);
                t.alpha = 1f;
            }
        }

        private static void Hide(ShapeBatch b, bool lines)
        {
            b.ResetToHome();
            for (int i = 0; i < b.Count; i++)
            {
                b[i].alpha = 0f;
                if (lines) b[i].grow = 0f;
            }

            b.MarkDirty();
        }

        protected override void Declare(Steps s)
        {
            Color yellow = theme.explainYellow;

            // "One neuron finds one shape." The pixels wait on the left; one neuron appears in the middle.
            s.At("copy", () =>
            {
                var list = Anim.Each(Anim.Range(0, inputs.Count), 0.012f, i => Anim.FadeIn(inputs, i, Vector3.zero, 0.7f));
                int first = FirstNeuron();
                list.Add(Anim.FadeIn(layer, first, Vector3.zero, 0.8f).After(0.4f));
                list.Add(Anim.Indicate(layer, first, yellow, 0.9f, 1.25f).After(1.2f));
                return list;
            });

            // "So line up lots of them." It copies itself into sixteen.
            s.At("layer", () =>
            {
                int first = FirstNeuron();
                var list = new List<Tween>();
                int n = 0;
                for (int j = 0; j < layer.Count; j++)
                {
                    if (j == first) continue;
                    int k = j;
                    float delay = 0.07f * n++;
                    list.Add(new Tween(0.01f, _ =>
                    {
                        Shape t = layer[k];
                        t.position = layer.Home(first).position;
                        t.alpha = 1f;
                        layer.MarkDirty();
                    }).After(delay));
                    list.Add(Anim.TransformShape(layer, k, () => layer.Home(k).Clone(), 0.9f).After(delay + 0.01f));
                }

                return list;
            });

            // "That's a layer."
            s.AtClause(RideLine.NetworkTeam, 2, () => new[] { Anim.Write(layerTitle, 1f) });

            // "At the end, ten answer neurons, zero to nine."
            s.At("answers", () =>
            {
                var list = Anim.Each(Anim.Range(0, outputs.Count), 0.08f, i => Anim.FadeIn(outputs, i, Vector3.left * 1.5f, 0.8f));
                for (int k = 0; k < labels.Length; k++) list.Add(Anim.Write(labels[k], 0.5f).After(0.4f + 0.08f * k));
                return list;
            });

            // "Wire them all together": every wire draws itself, the first layer's then the answers'.
            s.At("connect", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < edges.Count; i++)
                {
                    float delay = i < firstLayerEdges ? i * 0.0016f : 1.4f + (i - firstLayerEdges) * 0.004f;
                    list.Add(Anim.Create(edges, i, 0.6f).After(delay));
                }

                return list;
            });

            // "and it's a neural network."
            s.AtClause(RideLine.NetworkWire, 1, () => new[] { Anim.Write(title, 1.2f) });

            // "This is Pixel's brain!" A ripple runs left to right through every neuron.
            s.AtClause(RideLine.NetworkWire, 2, () =>
            {
                var list = Anim.Each(Anim.Range(0, inputs.Count), 0.008f, i => Anim.Indicate(inputs, i, yellow, 0.6f, 1.3f));
                list.AddRange(Anim.Each(Anim.Range(0, layer.Count), 0.03f, i => Anim.Indicate(layer, i, yellow, 0.6f, 1.25f).After(0.4f)));
                list.AddRange(Anim.Each(Anim.Range(0, outputs.Count), 0.04f, i => Anim.Indicate(outputs, i, yellow, 0.6f, 1.25f).After(0.8f)));
                return list;
            });

            // "Let's send our three through." The diagram grows along the line of sight into the giant network.
            s.Unfold("race", () =>
            {
                var list = new List<Tween> { Anim.FadeText(layerTitle, 0f, 0.5f), Anim.FadeText(title, 0f, 0.5f) };
                // The giant network brings its own 0 to 9; the diagram's fade so they never show twice.
                foreach (TMP_Text l in labels) list.Add(Anim.FadeText(l, 0f, 0.4f));
                return list;
            });
        }

        // The neuron nearest the middle of the layer: the one that copies itself.
        private int FirstNeuron()
        {
            Vector3 centre = Vector3.zero;
            for (int j = 0; j < layer.Count; j++) centre += layer.Home(j).position;
            centre /= Mathf.Max(1, layer.Count);
            int best = 0;
            for (int j = 1; j < layer.Count; j++)
            {
                if ((layer.Home(j).position - centre).sqrMagnitude < (layer.Home(best).position - centre).sqrMagnitude) best = j;
            }

            return best;
        }
    }
}
