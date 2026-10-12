using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// The narration script of Level 1, the single source for subtitles, pauses, pacing, and what the rider sees while
    /// each clause is spoken. It is the owner-approved draft of HANDOFF_LEVEL1_FULL_BUILD.md section 9 (2026-10-12),
    /// with the bake's numbers written in (the untrained guess, the training count, the test accuracy); the builder
    /// fails if they ever disagree with Assets/_Project/Data/Level1/level1_digits.json. One narrator, the Guide.
    /// [p0.8] marks a pause of 0.8 s inside a line: subtitles change clause by clause at the marks, and the reading
    /// time includes them. {cue} at the start of a clause names what to show while that clause is spoken (several:
    /// {a,b}); the views that know a cue react to it. A voice file only plays when its sidecar text matches this text,
    /// so an old recording can never be heard under new words. ArtSource/voice/render_voices.py reads this file to
    /// render the voice. Development tooling only.
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
            public float FixedSpeakSeconds;

            public Line(RideLine id, string text, float pauseAfter = DefaultPauseAfter)
            {
                Id = id;
                Speaker = NarrationLibrary.Speaker.Guide;
                Tag = "GUIDE";
                Text = text;
                PauseAfter = pauseAfter;
                FixedSpeakSeconds = 0f;
            }
        }

        public static readonly Line[] Lines =
        {
            // Region 0, the Page (dock).
            new Line(RideLine.PageLook, "{page}Look at this number. [p0.4] You read it instantly. [p0.3] It's a three.", 0.6f),
            new Line(RideLine.PageOnlyNumbers, "A computer can't see it. [p0.4] All it ever gets is numbers.", 0.6f),
            new Line(RideLine.PageFromInside, "Today you'll watch a machine learn to read it. [p0.3] From the inside.", 0.6f),
            new Line(RideLine.PagePullToDive, "{go}Pull the orange lever, [p0.2] and we'll dive into the picture.", 0.3f),

            // Region 1, the Grid.
            new Line(RideLine.GridInsidePicture, "We're inside the picture now. [p0.3] {pixels}Every pixel became one number."),
            new Line(RideLine.GridZeroAndOne, "{zero}Blank paper is zero. [p0.3] {one}Full ink is one. [p0.3] {tall}The taller the pillar, the more ink."),
            new Line(RideLine.GridSevenBySeven, "{seven}To keep it simple, we shrink it to seven by seven. [p0.3] Forty-nine numbers. [p0.3] That's all the machine gets.", 0.6f),

            // Region 2, One Neuron.
            new Line(RideLine.NeuronThisIsOne, "{neuron}This is a neuron. [p0.3] It looks at all forty-nine numbers at once."),
            new Line(RideLine.NeuronWeights, "{weights}Every pixel has a weight. [p0.3] {plus}Plus means: I want ink here. [p0.3] {minus}Minus means: I don't."),
            new Line(RideLine.NeuronMultiplyAdd, "{multiply}It multiplies each pixel by its weight and adds it all up. [p0.3] {line}If the total passes the line, it fires.", 0.6f),
            new Line(RideLine.NeuronYourTurn, "{paint}Your turn. [p0.2] Paint the weights so it fires for ones, and not for zeros. [p0.3] Tap a square to change it."),
            new Line(RideLine.NeuronFiredForZero, "It fired for a zero. [p0.3] Zeros have ink around the edges. [p0.2] Try minus there."),
            new Line(RideLine.NeuronMissedOne, "That one didn't fire. [p0.3] Ones have ink down the middle."),
            new Line(RideLine.NeuronSolved, "You found a pattern in plain numbers. [p0.4] That's all a neuron does. [p0.3] Math.", 0.6f),

            // Region 3, the Network.
            new Line(RideLine.NetworkManyNeurons, "One neuron spots one pattern. [p0.3] Reading digits takes many, wired together in layers."),
            new Line(RideLine.NetworkLayers, "{inputs}Forty-nine pixels in. [p0.3] {hidden}Sixteen hidden neurons in the middle. [p0.3] {outputs}Ten answers out, zero to nine.", 0.6f),
            new Line(RideLine.NetworkSendItThrough, "Let's send our three through. [p0.3] {race}Watch the light.", 0.3f),

            // Region 4, Wrong. The guess is the bake's untrainedGuess.
            new Line(RideLine.WrongItSays, "{guess}It says... [p0.5] one.", 0.4f),
            new Line(RideLine.WrongNeverTaught, "{wrong}Wrong. [p0.3] It has never been taught. [p0.3] Every weight is random.", 0.6f),

            // Region 5, Rewind.
            new Line(RideLine.RewindBackwards, "{backwards}So we go backwards. [p0.3] The mistake flows back through every wire."),
            new Line(RideLine.RewindBackprop, "{burn}The more a wire caused the mistake, the brighter it burns. [p0.4] That's called backpropagation.", 0.6f),
            new Line(RideLine.RewindPointAtWires, "{point}Point at the brightest wires to nudge them."),
            new Line(RideLine.RewindWiresKnow, "Now every wire knows which way to move.", 0.6f),

            // Region 6, the Valley.
            new Line(RideLine.ValleyDownhill, "All that blame points one way: [p0.2] downhill. [p0.3] {low}Down here, the network is less wrong.", 0.6f),
            new Line(RideLine.ValleyGradientDescent, "Each step moves every weight a little bit downhill. [p0.3] That's gradient descent. [p0.4] Hold on.", 0.4f),
            new Line(RideLine.ValleyOvershoot, "Too big a step. [p0.3] We flew right past the bottom.", 0.4f),
            new Line(RideLine.ValleySmallerSteps, "Smaller steps now. [p0.3] Down... [p0.3] and down... [p0.3] settling in.", 0.6f),

            // Region 7, Right. The count and the accuracy are the bake's trainCount and testAccuracy (rounded down).
            new Line(RideLine.RightBackInNetwork, "Back in the network. [p0.3] Same three. [p0.3] New weights."),
            new Line(RideLine.RightItSays, "{right}It says... [p0.5] three. [p0.3] Right.", 0.6f),
            new Line(RideLine.RightAccuracy, "{count}We showed it sixty thousand handwritten digits. [p0.4] {accuracy}Now it reads ninety-two percent of digits it has never seen.", 0.8f),
            new Line(RideLine.RightWords, "{words}Do the same with words instead of pixels, [p0.2] with billions of weights, [p0.3] and you get the AI you can talk to.", 0.6f),
            new Line(RideLine.RightNextLevel, "{next}That's where we're going next.", 0.8f),
        };

        /// <summary>The intro at the dock: GO holds (two seconds of holding skips it). Then the dock says DockLine and GO goes live.</summary>
        public static readonly RideLine[] IntroLines = { RideLine.PageLook, RideLine.PageOnlyNumbers, RideLine.PageFromInside };

        public const RideLine DockLine = RideLine.PagePullToDive;

        /// <summary>The sequences the segments play (NeuralRideBuilder.Flow refers to them by index, in this order).</summary>
        public static readonly (string Name, RideLine[] Lines)[] Sequences =
        {
            ("Into the picture", new[] { RideLine.GridInsidePicture, RideLine.GridZeroAndOne, RideLine.GridSevenBySeven }),   // 0
            ("One neuron", new[] { RideLine.NeuronThisIsOne, RideLine.NeuronWeights, RideLine.NeuronMultiplyAdd }),             // 1
            ("Your turn", new[] { RideLine.NeuronYourTurn }),                                                                   // 2
            ("To the network", new[] { RideLine.NetworkManyNeurons, RideLine.NetworkLayers }),                                  // 3
            ("Send it through", new[] { RideLine.NetworkSendItThrough }),                                                       // 4
            ("Wrong", new[] { RideLine.WrongItSays, RideLine.WrongNeverTaught }),                                               // 5
            ("Rewind", new[] { RideLine.RewindBackwards, RideLine.RewindBackprop }),                                            // 6
            ("Point at the blame", new[] { RideLine.RewindPointAtWires }),                                                      // 7
            ("Downhill", new[] { RideLine.ValleyDownhill }),                                                                    // 8
            ("Gradient descent", new[] { RideLine.ValleyGradientDescent }),                                                     // 9
            ("Back in the network", new[] { RideLine.RightBackInNetwork }),                                                     // 10
            ("Words", new[] { RideLine.RightWords, RideLine.RightNextLevel }),                                                  // 11
        };

        /// <summary>
        /// The end card's credits: every voice, music, data and sound source with its licence (ADR-012). Short lines, so
        /// the type can be large enough to read.
        /// </summary>
        public const string Credits =
            "VOICE  Kokoro-82M text-to-speech (synthetic), Apache 2.0\n"
            + "DIGITS  MNIST, Y. LeCun and C. Cortes, CC BY-SA 3.0\n"
            + "MUSIC  \"Telstar\"    SOUND EFFECTS  Kenney, CC0";

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

            float words = clause.Split(' ').Length;
            segments.Add(new NarrationLibrary.Segment { text = clause, weight = words + pauseAfter * WordsPerSecond, cues = cues });
        }

        private static float ParsePause(Match m) => float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);

        /// <summary>The words as spoken, with the pause marks and cue tags removed.</summary>
        public static string PlainText(string text) => Regex.Replace(Cue.Replace(Pause.Replace(text, " "), " "), @"\s+", " ").Trim();

        /// <summary>Speaking time with no clip: words at a calm pace plus the pauses inside the line.</summary>
        public static float ReadSeconds(Line line)
        {
            if (line.FixedSpeakSeconds > 0f) return line.FixedSpeakSeconds;
            Segments(line.Text, out float inner);
            int words = PlainText(line.Text).Split(' ').Length;
            return System.Math.Max(1.2f, words / WordsPerSecond + inner);
        }

        public static Line Get(RideLine id)
        {
            foreach (Line line in Lines)
            {
                if (line.Id == id) return line;
            }

            throw new KeyNotFoundException("RideNarrationData has no line " + id);
        }

        private static readonly string[] Ones =
        {
            "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve",
            "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"
        };

        private static readonly string[] Tens = { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

        /// <summary>A whole number in words as the narrator says it ("ninety-two", "sixty thousand"), 0 to 999,999.</summary>
        public static string Words(int n)
        {
            if (n < 0 || n > 999999) throw new System.ArgumentOutOfRangeException(nameof(n));
            if (n >= 1000)
            {
                string head = Words(n / 1000) + " thousand";
                return n % 1000 == 0 ? head : head + " " + Words(n % 1000);
            }

            if (n >= 100)
            {
                string head = Ones[n / 100] + " hundred";
                return n % 100 == 0 ? head : head + " and " + Words(n % 100);
            }

            if (n < 20) return Ones[n];
            return n % 10 == 0 ? Tens[n / 10] : Tens[n / 10] + "-" + Ones[n % 10];
        }
    }
}
