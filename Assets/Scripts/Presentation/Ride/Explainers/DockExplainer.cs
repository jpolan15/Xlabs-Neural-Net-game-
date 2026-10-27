using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Convergence.Presentation.Ride.Explainers
{
    /// <summary>
    /// E0, the dock: "What is this?" (3Blue1Brown's opening). A cloud of twenty real handwritten threes from MNIST
    /// drifts around the rider; thin lines run from every one of them to a single "3" (all different, and you read
    /// every one the same); then they step back and one hero 3 glides to the middle. On {go} the hero 3 flies out and
    /// becomes the ink of the Page, the sheet growing behind it. Pixel (WP2) appears at <c>pixelAnchor</c> on {pixel}
    /// and shows its brain on {brain}; those steps only announce themselves here (<see cref="Explainer.StepRan"/>).
    /// Then (v7) the roadmap: on {stops} a track with the five stops appears above, each stop lighting as it is named;
    /// on {blast} "blast every 3" writes under it; it fades with the threes on {go}.
    /// </summary>
    public sealed class DockExplainer : Explainer
    {
        [SerializeField] private Transform cloud;
        [SerializeField] private ShapeBatch threes;
        [SerializeField] private ShapeBatch rays;
        [SerializeField] private TMP_Text glyph;
        [SerializeField] private Transform hero;
        [SerializeField] private Renderer heroRenderer;
        [SerializeField] private Transform heroCentre;
        [SerializeField] private Transform pixelAnchor;
        [SerializeField] private float driftDegrees = 3f;

        [Header("Roadmap (v7)")]
        [Tooltip("Shape 0 is the track; then one chip per stop.")]
        [SerializeField] private ShapeBatch roadmap;
        [SerializeField] private TMP_Text[] stopLabels = new TMP_Text[0];
        [SerializeField] private TMP_Text blast;

        private Vector3 _heroHome;
        private Quaternion _heroHomeRotation;
        private Vector3 _heroHomeScale;
        private bool _heroKnown;
        private float _clock;

        /// <summary>Where Pixel floats at the dock (WP2 puts Pixel here).</summary>
        public Transform PixelAnchor => pixelAnchor;

        protected override void Awake()
        {
            if (hero != null && !_heroKnown)
            {
                _heroKnown = true;
                _heroHome = hero.localPosition;
                _heroHomeRotation = hero.localRotation;
                _heroHomeScale = hero.localScale;
            }

            base.Awake();
        }

        protected override void ResetPieces()
        {
            _clock = 0f;
            threes.ResetToHome();
            for (int i = 0; i < threes.Count; i++) threes[i].alpha = 0f;
            threes.MarkDirty();
            rays.ResetToHome();
            for (int i = 0; i < rays.Count; i++)
            {
                rays[i].grow = 0f;
                rays[i].alpha = 0f;
            }

            rays.MarkDirty();
            glyph.gameObject.SetActive(false);
            if (_heroKnown)
            {
                hero.localPosition = _heroHome;
                hero.localRotation = _heroHomeRotation;
                hero.localScale = _heroHomeScale;
            }

            hero.gameObject.SetActive(true);
            SetHeroColor(new Color(1f, 1f, 1f, 0f));
            if (cloud != null) cloud.localRotation = Quaternion.identity;
            if (roadmap != null)
            {
                roadmap.transform.parent.gameObject.SetActive(true);
                roadmap.ResetToHome();
                for (int i = 0; i < roadmap.Count; i++) roadmap[i].alpha = 0f;
                roadmap.MarkDirty();
            }

            foreach (TMP_Text t in stopLabels) Hide(t);
            Hide(blast);
        }

        private static void Hide(TMP_Text t)
        {
            if (t == null) return;
            t.gameObject.SetActive(false);
            t.alpha = 1f;
        }

        private void SetHeroColor(Color c)
        {
            var block = new MaterialPropertyBlock();
            heroRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", c);
            heroRenderer.SetPropertyBlock(block);
        }

        protected override void Declare(Steps s)
        {
            Color white = theme.explainWhite, blue = theme.explainBlue;

            // "Look! Lots of threes." They drift in from farther away, one after another.
            s.At("threes", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < threes.Count; i++)
                {
                    Vector3 away = threes.Home(i).position.normalized * 0.8f;
                    list.Add(Anim.FadeIn(threes, i, -away, 1.2f).After(i * 0.06f));
                }

                list.Add(HeroFade(white, 1.2f).After(threes.Count * 0.03f));
                return list;
            });

            // "And you know every one." Every three points at the same 3.
            s.AtClause(Gameplay.Ride.RideLine.Threes, 1, () =>
            {
                var list = Anim.Each(Anim.Range(0, rays.Count), 0.035f, i => Anim.Create(rays, i, 0.9f));
                list.Add(Anim.Write(glyph, 0.8f).After(0.6f));
                return list;
            });

            // "This is Pixel." The crowd steps back and dims; the hero 3 glides to the middle.
            s.At("pixel", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < rays.Count; i++) list.Add(Anim.FadeOut(rays, i, Vector3.zero, 0.7f));
                list.Add(Anim.FadeText(glyph, 0f, 0.7f));
                for (int i = 0; i < threes.Count; i++)
                {
                    int k = i;
                    list.Add(Anim.TransformShape(threes, k, () =>
                    {
                        Shape t = threes[k].Clone();
                        t.position = threes.Home(k).position * 1.25f;
                        t.alpha = 0.22f;
                        return t;
                    }, 1.4f).After(0.02f * i));
                }

                list.Add(Anim.MoveTo(hero, () => (heroCentre.position, heroCentre.rotation, Anim.LocalScaleFor(hero.parent, heroCentre.lossyScale)), 1.5f).After(0.3f));
                return list;
            });

            // "Let's go inside Pixel's brain." (Pixel's own reaction, WP2.)
            s.At("brain", () => null);

            // "Five stops:" The track and its stops fade in, dim; each lights up as the line names it.
            s.At("stops", () =>
            {
                var list = new List<Tween> { Anim.FadeIn(roadmap, 0, Vector3.zero, 0.6f) };
                for (int k = 0; k < stopLabels.Length; k++)
                {
                    int chip = k + 1;
                    list.Add(Anim.FadeIn(roadmap, chip, Vector3.down * 0.15f, 0.6f).After(0.1f + 0.08f * k));
                    list.Add(Anim.FadeTo(roadmap, chip, 0.35f, 0.3f).After(0.75f + 0.08f * k));
                    list.Add(Anim.Write(stopLabels[k], 0.5f).After(0.1f + 0.08f * k));
                    list.Add(Anim.FadeText(stopLabels[k], 0.45f, 0.3f).After(0.75f + 0.08f * k));
                }

                return list;
            });
            for (int k = 0; k < stopLabels.Length; k++)
            {
                int chip = k + 1, label = k;
                s.AtClause(Gameplay.Ride.RideLine.IntroStops, k + 1, () => new[]
                {
                    Anim.FadeTo(roadmap, chip, 1f, 0.3f),
                    Anim.FadeText(stopLabels[label], 1f, 0.3f),
                    Anim.Indicate(roadmap, chip, theme.explainYellow, 0.8f, 1.12f),
                });
            }

            // "In between, blast every three you see."
            s.At("blast", () => new[] { Anim.Write(blast, 0.7f) });

            // "Pull the orange lever." The hero 3 becomes the Page: it flies out, darkens to ink, and the sheet grows behind it.
            s.Unfold("go", () =>
            {
                var list = new List<Tween>();
                for (int i = 0; i < threes.Count; i++) list.Add(Anim.FadeOut(threes, i, threes.Home(i).position.normalized * 0.6f, 0.9f).After(0.015f * i));
                for (int i = 0; i < roadmap.Count; i++) list.Add(Anim.FadeOut(roadmap, i, Vector3.up * 0.3f, 0.8f));
                foreach (TMP_Text t in stopLabels) list.Add(Anim.FadeText(t, 0f, 0.6f, Vector3.up * 0.3f));
                list.Add(Anim.FadeText(blast, 0f, 0.6f, Vector3.up * 0.3f));
                list.Add(new Tween(0.01f, _ => roadmap.transform.parent.gameObject.SetActive(false)).After(0.9f));
                list.Add(Anim.Tint(heroRenderer, theme.ink, 1.1f).After(0.45f));
                return list;
            });
        }

        private Tween HeroFade(Color to, float duration)
        {
            return new Tween(duration, a => SetHeroColor(new Color(to.r, to.g, to.b, a)));
        }

        protected override void Animate(float deltaTime)
        {
            if (cloud == null) return;
            _clock += deltaTime;
            cloud.localRotation = Quaternion.Euler(Mathf.Sin(_clock * 0.21f) * driftDegrees * 0.4f, Mathf.Sin(_clock * 0.13f) * driftDegrees, 0f);
        }
    }
}
