using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Gameplay.Ride;
using Convergence.Gameplay.Ride.Level1;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// The win beat of Level 1's games (2026-11-17 playtest, WinCelebrationView): fireworks over the town, Pixel's five brain
    /// lanterns (one lights per topic learned), the big topic card in front of the rider and the card rail on the tub's left
    /// rim where earned cards stay (shoot one to read it again). The cards' words are here, written for someone who has
    /// never heard of AI, checked by the ML-teacher review.
    /// </summary>
    public static partial class NeuralRideBuilder
    {
        private static readonly string[] CardTitles = { "1  PIXELS", "2  NEURON", "3  NETWORK", "4  BACKPROP", "5  TRAINING" };

        private static readonly string[] CardIdeas =
        {
            "A picture is just numbers.",
            "A neuron is a tiny judge.",
            "Many judges, working together.",
            "Send the blame backwards.",
            "Walk downhill on the loss.",
        };

        private static readonly string[] CardFacts =
        {
            "- A pixel is one tiny square of the picture.\n- Blank is 0, full ink is 1.\n- Your 3 became 49 numbers (7 x 7).\n- Those numbers are all the network ever sees.",
            "- Every pixel gets a weight: how much it matters.\n- Green: ink here says 3.  Red: ink here says not a 3.\n- Multiply, add it all up, squish it to 0..1.\n- Close to 1 means \"Yes, I see it!\"",
            "- 16 hidden neurons each look for a different piece.\n- 10 answer neurons, one for each digit.\n- 944 weights, all random at the start.\n- The brightest answer is the network's guess.",
            "- We know the right answer: the digit is labelled 3.\n- The loss scores how wrong the guess was.\n- Backprop works out how much each wire is to blame.\n- That is its gradient: which way to nudge, and how much.",
            "- Step against the gradient: gradient descent.\n- Step size = learning rate. Too big jumps, too small crawls.\n- Guess, loss, backprop, step... thousands of times.\n- Tested on 500 digits it never saw: 85 in 100 right.",
        };

        private static readonly int[] CardRegions = { 1, 2, 3, 5, 7 };

        private static void BuildCelebration(GameObject pod, RideDirector rd, Moments m, PixelBuddyView pixel)
        {
            var view = rd.gameObject.AddComponent<WinCelebrationView>();
            Ref(view, "director", rd);
            Ref(view, "pixel", pixel);
            // Card 3 (the network) comes from "send your 3", a moment that is not a scored game: its Finished earns it.
            Refs(view, "cardGames", new List<Object> { m.Draw, m.Paint, null, m.Blame, m.Train });
            Ref(view, "sendMoment", m.Forward);
            Set(view, "titles", p => Strings(p, CardTitles));
            Set(view, "ideas", p => Strings(p, CardIdeas));
            Set(view, "facts", p => Strings(p, CardFacts));
            Set(view, "colors", p =>
            {
                p.arraySize = CardRegions.Length;
                for (int i = 0; i < CardRegions.Length; i++) p.GetArrayElementAtIndex(i).colorValue = TopicLantern(CardRegions[i]);
            });

            // ----- the big card, up and to the left of the table, facing the eye
            // Low on the console's left wing, beside the table (2026-11-17 ride: the card blocked the view): a console screen.
            Vector3 home = new Vector3(-0.6f, 1.06f, 0.62f);
            var card = Make("TopicCard", pod.transform, home).transform;
            card.localRotation = Quaternion.LookRotation(home - EyePoint, Vector3.up);
            card.localScale = Vector3.one * 0.8f;
            var tiles = new TileMesh();
            tiles.Face(new Vector3(0f, 0f, 0.004f), 0.58f, 0.46f, Color.white);
            GameObject frame = TileObject("Frame", card, tiles, "PlateSolid");
            var plate = new TileMesh();
            plate.Face(new Vector3(0f, 0f, 0.002f), 0.56f, 0.44f, Color.white);
            TileObject("Plate", card, plate, "PlateSolid").GetComponent<Renderer>().sharedMaterial = Mat("PlateSolid");
            frame.GetComponent<Renderer>().sharedMaterial = M("CardFrame", Color.white);
            TextMeshPro header = Text("Header", card, new Vector3(0f, 0.19f, 0f), "NEW CARD!", 0.026f, _theme.gold, 0.52f, TextAlignmentOptions.Center, true);
            TextMeshPro title = Text("Title", card, new Vector3(0f, 0.14f, 0f), "1  PIXELS", 0.052f, Color.white, 0.52f, TextAlignmentOptions.Center, true);
            TextMeshPro idea = Text("Idea", card, new Vector3(0f, 0.085f, 0f), "", 0.03f, Color.white, 0.52f, TextAlignmentOptions.Center, true);
            TextMeshPro body = Text("Body", card, new Vector3(0f, -0.065f, 0f), "", 0.022f, new Color(0.88f, 0.9f, 0.95f), 0.52f, TextAlignmentOptions.TopLeft);
            body.rectTransform.sizeDelta = new Vector2(0.52f, 0.24f);
            body.lineSpacing = 4f;
            body.enableAutoSizing = true;
            body.fontSizeMin = 0.16f;
            body.fontSizeMax = 0.22f;
            foreach (TextMeshPro t in new[] { header, title, idea }) t.textWrappingMode = TextWrappingModes.NoWrap;
            Ref(view, "bigCard", card);
            Ref(view, "header", header);
            Ref(view, "title", title);
            Ref(view, "idea", idea);
            Ref(view, "body", body);
            Ref(view, "border", frame.GetComponent<Renderer>());
            // Shoot the card to close it (it also closes on GO).
            card.gameObject.AddComponent<BoxCollider>().size = new Vector3(0.58f, 0.46f, 0.03f);
            var closer = card.gameObject.AddComponent<TopicCardSlot>();
            Int(closer, "index", -1);
            Ref(view, "closer", closer);
            TextMeshPro x = Text("Close", card, new Vector3(0.235f, 0.19f, -0.002f), "X", 0.04f, new Color(1f, 0.5f, 0.45f), 0.06f, TextAlignmentOptions.Center, true);
            x.textWrappingMode = TextWrappingModes.NoWrap;
            card.gameObject.SetActive(false); // shown only on a win (and the sightline check must not see it)

            // ----- the card rail: five slots along the tub's left rim, low and to the side, out of the view
            var rail = Make("CardRail", pod.transform, Vector3.zero).transform;
            var slots = new List<Object>();
            var faces = new List<Object>();
            for (int i = 0; i < 5; i++)
            {
                // One neat row of card slots on the console's left wing, like buttons on the dash.
                Vector3 at = new Vector3(-0.84f + i * 0.1f, 0.86f, 0.5f);
                var slot = Make("Slot" + i, rail, at).transform;
                slot.localRotation = Quaternion.LookRotation(at - EyePoint, Vector3.up);
                var empty = new TileMesh();
                empty.Face(new Vector3(0f, 0f, 0.003f), 0.09f, 0.07f, new Color(0.18f, 0.2f, 0.26f, 0f));
                TileObject("Empty", slot, empty, "Tiles");
                var face = Make("Face", slot, Vector3.zero);
                var faceTiles = new TileMesh();
                Color c = TopicLantern(CardRegions[i]);
                faceTiles.Face(new Vector3(0f, 0f, 0.002f), 0.09f, 0.07f, new Color(c.r, c.g, c.b, 1f));
                faceTiles.Face(new Vector3(0f, 0f, 0.001f), 0.08f, 0.06f, new Color(0.02f, 0.03f, 0.06f, 0f));
                TileObject("Card" + i, face.transform, faceTiles, "Tiles");
                TextMeshPro label = Text("Label", face.transform, new Vector3(0f, 0f, 0f), CardTitles[i].Replace("  ", "\n"), 0.024f, c, 0.14f, TextAlignmentOptions.Center, true);
                label.rectTransform.sizeDelta = new Vector2(0.084f, 0.064f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.enableAutoSizing = true;
                label.fontSizeMin = 0.12f;
                label.fontSizeMax = 0.24f;
                slot.gameObject.AddComponent<BoxCollider>().size = new Vector3(0.1f, 0.08f, 0.03f);
                var s = slot.gameObject.AddComponent<TopicCardSlot>();
                Int(s, "index", i);
                slots.Add(s);
                faces.Add(face);
                face.SetActive(false);
            }

            Refs(view, "slots", slots);
            Refs(view, "slotFaces", faces);

            // ----- Pixel's brain: five small paper lanterns over its head, dark until a topic is learned
            Transform pixelBody = pod.transform.Find("Pixel/Body");
            var brain = Make("Brain", pixelBody, new Vector3(0f, 0.37f, -0.02f)).transform;
            var lanterns = new FestivalMesh();
            var halos = new GlowMesh();
            for (int i = 0; i < 5; i++)
            {
                Vector3 at = new Vector3((i - 2) * 0.085f, -Mathf.Abs(i - 2) * 0.018f, 0f);
                lanterns.Lantern(at, 0.032f, 0.064f, new Color(0.22f, 0.2f, 0.24f, 0f));
                halos.Halo(at, 0.09f, Color.black, 0f);
            }

            GameObject paperObject = AddMeshObject("Lanterns", brain, SaveMesh(lanterns.ToMesh("PixelBrain"), "L1_PixelBrain"), "Festival");
            GameObject haloObject = AddMeshObject("Halos", brain, SaveMesh(halos.ToMesh("PixelBrainHalos"), "L1_PixelBrainHalos"), "FestivalGlow");
            Ref(view, "brainLanterns", paperObject.GetComponent<MeshFilter>());
            Ref(view, "brainHalos", haloObject.GetComponent<MeshFilter>());
            TextMeshPro learned = Text("Learned", pod.transform.Find("Pixel"), pixelBody.localPosition + new Vector3(-0.05f, 0.62f, 0f), "PIXEL LEARNED", 0.045f, _theme.gold, 0.7f, TextAlignmentOptions.Center, true);
            learned.textWrappingMode = TextWrappingModes.NoWrap;
            learned.gameObject.AddComponent<FaceCamera>();
            Ref(view, "learned", learned);
            learned.gameObject.SetActive(false);

            // ----- fireworks: one particle system on the Track (world space), emitted from code; a soft boom
            var fx = Make("Fireworks", GameObject.Find("Track").transform, Vector3.zero);
            var ps = fx.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = 1.6f;
            main.startSpeed = 0f;
            main.startSize = 0.35f;
            main.gravityModifier = 0.32f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2000;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            col.color = fade;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = 1.6f;
            var trails = ps.trails;
            trails.enabled = true;
            trails.ratio = 0.7f;
            trails.lifetime = 0.16f;
            trails.dieWithParticles = true;
            trails.inheritParticleColor = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.6f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            Material spark = FestivalMaterial("FireworkSpark", "Convergence/Spark");
            var psr = fx.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = spark;
            psr.trailMaterial = spark;
            psr.shadowCastingMode = ShadowCastingMode.Off;
            psr.receiveShadows = false;
            Ref(view, "fireworks", ps);
            var boom = rd.gameObject.AddComponent<AudioSource>();
            boom.playOnAwake = false;
            boom.spatialBlend = 0f;
            boom.volume = 0.5f;
            Ref(view, "boomSource", boom);
        }

        private static void Strings(SerializedProperty p, string[] values)
        {
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }
}
