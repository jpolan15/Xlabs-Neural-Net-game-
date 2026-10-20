using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// The narration script of Level 1, the single source for subtitles, pauses, pacing, and what the rider sees while
    /// each clause is spoken. It is the locked v4 script of TASK_LEVEL1_V4_EXPLAINERS.md section 3 (2026-10-20),
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
            // E0, the dock: what is this? (intro and dock line)
            new Line(RideLine.Threes, "{threes}Look! [p0.3] Lots of threes. [p0.3] All different. [p0.3] And you know every one.", 0.6f),
            new Line(RideLine.MeetPixel, "{pixel}This is Pixel. [p0.3] Pixel is a little robot, [p0.2] and Pixel can't read yet.", 0.5f),
            new Line(RideLine.PixelBrain, "{brain}Let's go inside Pixel's brain, [p0.2] and teach it to read a three.", 0.5f),
            new Line(RideLine.PullGo, "{go}Pull the orange lever. [p0.2] Let's go!", 0.3f),

            // E1, pixels.
            new Line(RideLine.PixelsSquares, "{grid}To a computer, [p0.2] a picture is tiny squares. [p0.3] They're called pixels.", 0.6f),
            new Line(RideLine.PixelsNumbers, "{numbers}Every pixel is just a number. [p0.3] {zero}Dark is zero. [p0.3] {one}Bright is one.", 0.6f),
            new Line(RideLine.PixelsFortyNine, "{shrink}We'll use seven by seven. [p0.3] {column}That's forty-nine numbers. [p0.3] It's all Pixel ever sees.", 0.6f),
            new Line(RideLine.PixelsFly, "{unfold}Let's fly over them!", 0.4f),

            // E2, one neuron.
            new Line(RideLine.NeuronHolds, "{neuron}This circle is a neuron. [p0.3] It holds one number. [p0.3] {glow}The bigger the number, [p0.2] the brighter it glows."),
            new Line(RideLine.NeuronWires, "{wires}It's wired to every pixel. [p0.3] Each wire has a weight."),
            new Line(RideLine.NeuronGreenRed, "{green}Green weights say: [p0.2] I like ink here. [p0.3] {red}Red weights say: [p0.2] no ink here, please."),
            new Line(RideLine.NeuronAdds, "{add}The neuron adds it all up. [p0.3] {line}If the total is big enough, [p0.2] it lights up!", 0.6f),
            new Line(RideLine.NeuronYourTurn, "{paint}Your turn! [p0.2] Make it light up for ones, [p0.2] but not for zeros. [p0.3] Tap a square to paint it."),
            new Line(RideLine.NeuronLitForZero, "Oops, it lit up for a zero. [p0.3] Zeros are round. [p0.2] Paint the round part red."),
            new Line(RideLine.NeuronMissedOne, "A one stayed dark. [p0.3] Ones are a line down the middle. [p0.2] Paint it green."),
            new Line(RideLine.NeuronSolved, "You did it! [p0.3] You taught a neuron a shape.", 0.6f),

            // E3, the network.
            new Line(RideLine.NetworkTeam, "{copy}One neuron finds one shape. [p0.3] {layer}So line up lots of them. [p0.3] That's a layer."),
            new Line(RideLine.NetworkAnswers, "{answers}At the end, [p0.2] ten answer neurons, [p0.2] zero to nine."),
            new Line(RideLine.NetworkWire, "{connect}Wire them all together, [p0.2] and it's a neural network. [p0.3] This is Pixel's brain!", 0.6f),
            new Line(RideLine.NetworkSend, "{race}Let's send our three through. [p0.3] Watch the lights!", 0.3f),

            // E4, wrong, and the loss. The guess is the bake's untrainedGuess.
            new Line(RideLine.WrongSays, "{guess}Pixel says... [p0.5] one?", 0.4f),
            new Line(RideLine.WrongRandom, "{wrong}Wrong! [p0.3] Pixel never learned. [p0.3] Its weights are random.", 0.6f),
            new Line(RideLine.WrongWanted, "{want}We wanted the three bright, [p0.2] and all the others dark."),
            new Line(RideLine.WrongLoss, "{oops}Add up how far off each answer is. [p0.3] That's the loss. [p0.3] A big loss means a big mistake.", 0.6f),

            // E5, backpropagation.
            new Line(RideLine.BackWish, "{wish}The three wants to be brighter. [p0.3] {dim}The one wants to be dimmer."),
            new Line(RideLine.BackFlows, "{back}That wish flows backwards, [p0.2] through every wire. [p0.3] That's backpropagation.", 0.6f),
            new Line(RideLine.BackHot, "{hot}The wires that caused the mistake glow hot.", 0.6f),
            new Line(RideLine.BackGrab, "{grab}Grab the hot wires, [p0.2] and nudge them!"),
            new Line(RideLine.BackDone, "Nice! [p0.3] Now every wire knows which way to change.", 0.6f),

            // E6, gradient descent.
            new Line(RideLine.DescentHill, "{hill}Think of the loss as a hill. [p0.3] {ball}High up means lots of mistakes. [p0.3] Down low means hardly any."),
            new Line(RideLine.DescentStep, "{slope}Feel which way is down, [p0.2] and take a step. [p0.3] That's gradient descent.", 0.6f),
            new Line(RideLine.DescentTooBig, "{bigstep}Take too big a step, [p0.2] and you fly past the bottom. [p0.3] {smallsteps}Small steps settle in.", 0.6f),
            new Line(RideLine.DescentRealHill, "{landscape}Now, the real hill. [p0.3] Hold on tight!", 0.4f),
            new Line(RideLine.DescentOvershoot, "Whoa! [p0.3] Too far!", 0.4f),
            new Line(RideLine.DescentSettle, "Small steps now. [p0.3] Down... [p0.3] and down... [p0.3] and down.", 0.6f),

            // E7, right, and training. The count and the accuracy are the bake's trainCount and testAccuracy (rounded down).
            new Line(RideLine.RightSays, "{right}Same three. [p0.3] New weights. [p0.3] Pixel says... [p0.5] three! [p0.3] Yes!", 0.6f),
            new Line(RideLine.RightTraining, "{practice}Pixel practised on sixty thousand digits. [p0.3] That's called training.", 0.6f),
            new Line(RideLine.RightAccuracy, "{accuracy}Now Pixel reads ninety-two out of a hundred, [p0.2] even ones it has never seen.", 0.8f),

            // E8, tokens: the bridge to Level 2.
            new Line(RideLine.TokensWords, "{sentence}Words work the same way. [p0.3] {tokens}We chop a sentence into pieces called tokens."),
            new Line(RideLine.TokensNumbers, "{vectors}Each token becomes a list of numbers, [p0.2] just like pixels."),
            new Line(RideLine.TokensPredict, "{predict}A giant network guesses the next word. [p0.3] {mat}That's how a chatbot talks.", 0.6f),
            new Line(RideLine.TokensNext, "{next}Next time, [p0.2] Pixel learns words! [p0.3] See you there.", 0.8f),
        };

        /// <summary>The intro at the dock (E0's explainer plays with it): GO holds (two seconds of holding skips it). Then the dock says DockLine and GO goes live.</summary>
        public static readonly RideLine[] IntroLines = { RideLine.Threes, RideLine.MeetPixel, RideLine.PixelBrain };

        public const RideLine DockLine = RideLine.PullGo;

        /// <summary>
        /// The sequences the segments play (NeuralRideBuilder.Flow refers to them by index, in this order). An explain
        /// sequence plays at a stop while its explainer runs on the cues; its play sequence follows once it unfolded.
        /// </summary>
        public static readonly (string Name, RideLine[] Lines)[] Sequences =
        {
            ("E1 pixels: explain", new[] { RideLine.PixelsSquares, RideLine.PixelsNumbers, RideLine.PixelsFortyNine, RideLine.PixelsFly }),   // 0
            ("E2 neuron: explain", new[] { RideLine.NeuronHolds, RideLine.NeuronWires, RideLine.NeuronGreenRed, RideLine.NeuronAdds }),       // 1
            ("E2 neuron: your turn", new[] { RideLine.NeuronYourTurn }),                                                                      // 2
            ("E3 network: explain", new[] { RideLine.NetworkTeam, RideLine.NetworkAnswers, RideLine.NetworkWire }),                          // 3
            ("E3 network: send it through", new[] { RideLine.NetworkSend }),                                                                  // 4
            ("E4 wrong: explain", new[] { RideLine.WrongSays, RideLine.WrongRandom, RideLine.WrongWanted, RideLine.WrongLoss }),              // 5
            ("E5 backprop: explain", new[] { RideLine.BackWish, RideLine.BackFlows, RideLine.BackHot }),                                      // 6
            ("E5 backprop: grab", new[] { RideLine.BackGrab }),                                                                               // 7
            ("E6 descent: explain", new[] { RideLine.DescentHill, RideLine.DescentStep, RideLine.DescentTooBig, RideLine.DescentRealHill }),  // 8
            ("E8 tokens: explain", new[] { RideLine.TokensWords, RideLine.TokensNumbers, RideLine.TokensPredict, RideLine.TokensNext }),      // 9
        };

        /// <summary>Lines the moments say (not in a sequence): the puzzle's feedback, the blame's end, the drop, the trained pass.</summary>
        public static readonly RideLine[] MomentLines =
        {
            RideLine.NeuronLitForZero, RideLine.NeuronMissedOne, RideLine.NeuronSolved, RideLine.BackDone,
            RideLine.DescentOvershoot, RideLine.DescentSettle, RideLine.RightSays, RideLine.RightTraining, RideLine.RightAccuracy
        };

        /// <summary>
        /// The end card's credits: every voice, music, data and sound source with its licence (ADR-012). Short lines, so
        /// the type can be large enough to read.
        /// </summary>
        public const string Credits =
            "VOICE  Kokoro-82M text-to-speech (synthetic), Apache 2.0\n"
            + "DIGITS  MNIST, Y. LeCun and C. Cortes, CC BY-SA 3.0\n"
            + "MUSIC  \"Telstar\"    SOUND EFFECTS  Kenney, CC0\n"
            + "TYPE  Computer Modern Unicode (CMU) fonts, SIL OFL 1.1";

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
