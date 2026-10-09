using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// The narration script for the Neural Ride, the single source for subtitles, pauses, pacing, and what the rider
    /// sees while each clause is spoken. It is the approved draft from TASK_IGNITE_RIDE_V3_MASTER_PLAN.md Appendix A.
    /// [p0.8] marks a pause of 0.8 s inside a line: subtitles change clause by clause at the marks, and the reading
    /// time includes them. {cue} at the start of a clause names what to show while that clause is spoken (several:
    /// {a,b}); the views that know a cue react to it (ExplainerView, NeuralCoreView, HintArrowView). A voice file only
    /// plays when its sidecar text matches this text, so an old recording can never be heard under new words.
    /// ArtSource/voice/render_voices.py reads this file to render the voice. Development tooling only.
    /// </summary>
    internal static class RideNarrationData
    {
        public const float WordsPerSecond = 2.4f;
        public const float DefaultPauseAfter = 0.5f;

        public struct Line
        {
            public RideLine Id;
            public NarrationLibrary.Speaker Speaker;
            public string Tag;
            public string Text;
            public float PauseAfter;
            public float FixedSpeakSeconds; // archival clips with no spoken text we can count

            public Line(RideLine id, NarrationLibrary.Speaker speaker, string tag, string text, float pauseAfter = DefaultPauseAfter, float fixedSeconds = 0f)
            {
                Id = id;
                Speaker = speaker;
                Tag = tag;
                Text = text;
                PauseAfter = pauseAfter;
                FixedSpeakSeconds = fixedSeconds;
            }
        }

        private const NarrationLibrary.Speaker G = NarrationLibrary.Speaker.Guide;
        private const NarrationLibrary.Speaker A = NarrationLibrary.Speaker.Aura;
        private const NarrationLibrary.Speaker C = NarrationLibrary.Speaker.AuraClean;
        private const NarrationLibrary.Speaker X = NarrationLibrary.Speaker.Archival;

        public static readonly Line[] Lines =
        {
            // Intro. The rider's hands are on a lever within about 15 s: JFK in the dark, AURA offline, then the rider wakes
            // her (GuideWakeAura waits for GO) and makes one neuron fire (GuideMakeItFire waits for the ROCK weight). See
            // IntroTasks below; GuideOrange and GuideHardProblem are no longer in the intro (the wake line teaches orange).
            new Line(RideLine.ArchivalMoon, X, "J.F.K., 1962",
                "{jfk_build}We choose to go to the Moon in this decade and do the other things, [p0] {jfk_hard}not because they are easy, but because they are hard.", 0.8f),
            new Line(RideLine.GuideHardProblem, G, "GUIDE", "Every great journey starts with a hard problem. [p0.8] {net_wake}Ours is inside this ship."),
            new Line(RideLine.AuraOffline, A, "AURA", "{offline,net_broken}Targeting... offline. [p0.5] {rock_friend}I can't tell a rock... from a friend."),
            new Line(RideLine.GuideWakeAura, G, "GUIDE", "{orange}Anything orange, you can grab. [p0.3] {go}Pull the orange lever, [p0.2] and wake her up.", 0.3f),
            new Line(RideLine.GuideAuraBrain, G, "GUIDE",
                "There she is: AURA, the ship's mind. [p0.4] Her brain is a neural network: [p0.2] {neurons}thousands of tiny decision-makers called neurons, [p0.2] {links}all wired together. [p0.5] {net_broken}But something in it has gone wrong.", 0.6f),
            new Line(RideLine.GuideMakeItFire, G, "GUIDE", "{fire_goal}Here's one connection, up close. [p0.3] Push the rock lever up, [p0.2] until this neuron fires.", 0.3f),
            new Line(RideLine.GuideWeights, G, "GUIDE",
                "{try_weight}That lever is a weight: [p0.2] how much one neuron listens to another. [p0.5] {weights_change}Change the weights, [p0.2] and you change what the AI thinks.", 0.7f),
            new Line(RideLine.GuidePlan, G, "GUIDE",
                "{plan}We'll make three stops. [p0.4] {plan1}One neuron with one signal. [p0.4] {plan2}One neuron that weighs two signals. [p0.4] {plan3}And last, we'll watch the machine teach itself.", 0.7f),
            new Line(RideLine.GuideOrange, G, "GUIDE", "{orange}Anything orange, you can grab. [p0.3] Everything else is just for looking."),
            new Line(RideLine.Launch, G, "GUIDE", "When you're ready, [p0.4] {go}pull the orange GO lever.", 0.3f),

            // Briefing 1 (opens with the Apollo 11 liftoff when GO is pulled)
            // Jack King, NASA launch commentary. He says "all engine running" (singular), as NASA's own transcript has it.
            new Line(RideLine.ApolloLiftoff, X, "NASA, APOLLO 11",
                "Three, two, one, zero, [p0] all engine running. [p0] {liftoff}Liftoff! We have a liftoff.", 0.4f, 8.8f),
            new Line(RideLine.BriefingFirstNeuron, G, "GUIDE",
                "Chapter one. Nineteen forty-three. [p0.6] Two scientists, Warren McCulloch and Walter Pitts, imagined the first artificial neuron. [p0.4] {sum}It adds up its signals, [p0.3] {trigger}and if the total crosses a line, the trigger, [p0.3] {fire}it fires. [p0.8] {rock_drone}Up ahead, rocks are mixed in with our friendly repair drones. Set the trigger so the neuron zaps rocks, and only rocks.", 0.8f),

            // Stop 1
            new Line(RideLine.NeuronIntro, G, "GUIDE", "Rocks are slipping through. Lower the trigger."),
            new Line(RideLine.LowerTrigger, G, "GUIDE", "Lower. [p0.3] The rock's signal needs to reach the line."),
            new Line(RideLine.DroneOops, A, "AURA", "Whoa, you zapped a drone! [p0.4] Trigger's too low."),
            new Line(RideLine.WeightTimesInput, G, "GUIDE", "That's it. [p0.5] Signal times weight, compared to a trigger. [p0.6] That's all a neuron is: math.", 0.6f),

            // Briefing 2
            new Line(RideLine.BriefingPerceptron, G, "GUIDE",
                "Chapter two. Nineteen fifty-eight. [p0.6] {knob}Frank Rosenblatt's perceptron gave every input its own weight, like a volume knob. [p0.6] {sensors}Now there are two sensors: rock, and ice. [p0.4] {negative}But the ice wire is plugged in backwards. A negative weight. [p0.6] {fix}Fix the weights so anything rocky or icy gets zapped. [p0.4] {you_learn}Today, you are the one doing the learning.", 0.8f),

            // Stop 2
            new Line(RideLine.TwoSensors, A, "AURA", "The ice sensor is wired backwards. [p0.3] It's draining my core."),
            new Line(RideLine.TurnPipes, G, "GUIDE", "Turn each pipe up or down. [p0.3] That number is the weight."),
            new Line(RideLine.OrBuilt, G, "GUIDE", "Every case is right. [p0.5] You just built an OR gate out of multiplication and addition.", 0.6f),

            // Briefing 3
            new Line(RideLine.BriefingLearning, G, "GUIDE",
                "Chapter three. Nineteen sixty. [p0.6] {self_tune}Bernard Widrow and Ted Hoff asked: what if the machine tuned its own weights? [p0.6] {error}Measure how wrong it is, the error, [p0.3] {downhill}then nudge every weight a little bit downhill. [p0.3] {repeat}Repeat. [p0.8] {today}A version of that idea still trains today's AI.", 0.8f),

            // Stop 3
            new Line(RideLine.PullLearn, G, "GUIDE", "Hand-tuning is slow. [p0.4] Pick a learning rate, [p0.3] how big each step is, [p0.3] then pull LEARN."),
            new Line(RideLine.HillIsError, G, "GUIDE", "This hill is the error. Lower is better. [p0.4] Watch the weights roll downhill."),
            new Line(RideLine.GradientDescent, G, "GUIDE", "That's gradient descent. [p0.4] Measure the slope. Step down it. Repeat.", 0.6f),
            new Line(RideLine.LrTooHigh, A, "AURA", "Too big a step! [p0.3] I'm overshooting!"),
            new Line(RideLine.Converged, A, "AURA", "Converged. [p0.4] I found my own weights."),

            // Outro
            new Line(RideLine.TargetingRestored, C, "AURA", "{online}Targeting restored. [p0.5] I learned it from you.", 0.7f),
            new Line(RideLine.GuideHeartOfNetworks, G, "GUIDE",
                "{recap}Weights, sums, triggers, and learning by rolling downhill. [p0.6] {heart}That's the heart of every neural network. [p0.4] {billions}Real ones just do it with billions of weights at once.", 0.8f),
            new Line(RideLine.GuideTheProblem, G, "GUIDE",
                "{xor}But in nineteen sixty-nine, researchers found a problem one neuron can never solve. [p1.0] {next_ride}That's our next ride.", 0.8f),
            new Line(RideLine.CourseLocked, C, "AURA", "Course locked. Taking us home, for now.", 0.5f)
        };

        public static readonly RideLine[] IntroLines =
        {
            RideLine.ArchivalMoon, RideLine.AuraOffline, RideLine.GuideWakeAura, RideLine.GuideAuraBrain,
            RideLine.GuideMakeItFire, RideLine.GuideWeights, RideLine.GuidePlan, RideLine.Launch
        };

        /// <summary>
        /// A hands-on beat: after its line starts, the intro waits for the rider to set a lever to at least a value,
        /// or for the timeout (counted from the end of the line), so nobody gets stuck. Numbers only (RideScript.Beat).
        /// </summary>
        public struct RiderTask
        {
            public RideLine Line;
            public string Control;
            public float From;      // where the lever is set when the beat opens (ignored for GO, which the rider may be holding)
            public float AtLeast;
            public float TimeoutSeconds;

            public RiderTask(RideLine line, string control, float from, float atLeast, float timeoutSeconds)
            {
                Line = line;
                Control = control;
                From = from;
                AtLeast = atLeast;
                TimeoutSeconds = timeoutSeconds;
            }
        }

        public static readonly RiderTask[] IntroTasks =
        {
            // Pull GO: AURA wakes, the network lights from the pod outward.
            new RiderTask(RideLine.GuideWakeAura, RideControlIds.Action, 0f, 0.5f, 8f),
            // Push the ROCK weight from 0 to +1.5 (three detents): the neuron on the explainer fires.
            new RiderTask(RideLine.GuideMakeItFire, RideControlIds.Rock, 0f, 1.5f, 12f)
        };

        public static readonly RideLine[][] BriefingLines =
        {
            new[] { RideLine.ApolloLiftoff, RideLine.BriefingFirstNeuron },
            new[] { RideLine.BriefingPerceptron },
            new[] { RideLine.BriefingLearning }
        };

        public static readonly RideLine[] OutroLines =
        {
            RideLine.TargetingRestored, RideLine.GuideHeartOfNetworks, RideLine.GuideTheProblem, RideLine.CourseLocked
        };

        public static readonly NarrationLibrary.Chapter[] Chapters =
        {
            new NarrationLibrary.Chapter { year = "1943", title = "THE FIRST NEURON", blurb = "McCulloch and Pitts: add up the signals. Past the line? FIRE." },
            new NarrationLibrary.Chapter { year = "1958", title = "THE PERCEPTRON", blurb = "Rosenblatt: every input gets its own weight, like a volume knob." },
            new NarrationLibrary.Chapter { year = "1960", title = "LEARNING DOWNHILL", blurb = "Widrow and Hoff: measure the error, nudge every weight downhill." },
            new NarrationLibrary.Chapter { year = "1969", title = "THE NEXT PROBLEM", blurb = "One neuron can never solve XOR. Next ride: hidden layers." }
        };

        /// <summary>
        /// The finale's credits: every voice, music and sound source with its license (master plan WP4, ADR-012). Four
        /// short lines, so the type can be large enough to read (1.5 degrees) and still fit the finale's width.
        /// </summary>
        public const string Credits =
            "VOICES  Guide and AURA: Kokoro-82M text-to-speech, Apache 2.0\n"
            + "(synthetic; no one's voice was cloned)  /  MUSIC  \"Telstar\"\n"
            + "J.F.K., Rice University 1962: JFK Library, public domain\n"
            + "Apollo 11 launch: NASA, Jack King  /  SOUND EFFECTS  Kenney, CC0";

        private static readonly Regex Pause = new Regex(@"\[p(\d+(?:\.\d+)?)\]", RegexOptions.Compiled);
        private static readonly Regex Cue = new Regex(@"\{([a-z0-9_, ]+)\}", RegexOptions.Compiled);
        private static readonly Regex LeadingCue = new Regex(@"^\s*\{([a-z0-9_, ]+)\}\s*", RegexOptions.Compiled);

        /// <summary>The subtitle with the pause marks and cue tags removed, one clause per segment, each with its cues. A segment's weight is its share of the line's time.</summary>
        public static NarrationLibrary.Segment[] Segments(string text, out float innerPauseSeconds)
        {
            var segments = new List<NarrationLibrary.Segment>();
            innerPauseSeconds = 0f;
            int at = 0;
            foreach (Match m in Pause.Matches(text))
            {
                AddSegment(segments, text.Substring(at, m.Index - at), ParsePause(m));
                innerPauseSeconds += ParsePause(m);
                at = m.Index + m.Length;
            }

            AddSegment(segments, text.Substring(at), 0f);
            return segments.ToArray();
        }

        private static void AddSegment(List<NarrationLibrary.Segment> segments, string clause, float pauseAfter)
        {
            string[] cues = new string[0];
            Match tag = LeadingCue.Match(clause);
            if (tag.Success)
            {
                cues = tag.Groups[1].Value.Split(new[] { ',', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                clause = clause.Substring(tag.Length);
            }

            if (Cue.IsMatch(clause)) throw new System.InvalidOperationException("A {cue} tag must open its clause (right after a [pX] mark or at the start of the line): " + clause);
            clause = clause.Trim();
            if (clause.Length == 0) return;

            // Weight is spoken words plus the pause, in words, so captions track the voice.
            float words = clause.Split(' ').Length;
            segments.Add(new NarrationLibrary.Segment { text = clause, weight = words + pauseAfter * WordsPerSecond, cues = cues });
        }

        private static float ParsePause(Match m) => float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);

        /// <summary>The words as spoken, with the pause marks and cue tags removed.</summary>
        public static string PlainText(string text) => Regex.Replace(Cue.Replace(Pause.Replace(text, " "), " "), @"\s+", " ").Trim();

        /// <summary>Every cue named anywhere in the script, so the builder can check each one has something to show.</summary>
        public static HashSet<string> AllCues()
        {
            var all = new HashSet<string>();
            foreach (Line line in Lines)
            {
                foreach (NarrationLibrary.Segment s in Segments(line.Text, out _))
                {
                    foreach (string c in s.cues) all.Add(c);
                }
            }

            return all;
        }

        /// <summary>Speaking time with no clip: words at a calm pace plus the pauses inside the line.</summary>
        public static float ReadSeconds(Line line)
        {
            if (line.FixedSpeakSeconds > 0f) return line.FixedSpeakSeconds;
            Segments(line.Text, out float inner);
            int words = PlainText(line.Text).Split(' ').Length;
            return System.Math.Max(1.2f, words / WordsPerSecond + inner);
        }
    }
}
