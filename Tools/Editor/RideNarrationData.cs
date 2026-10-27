using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Convergence.Gameplay.Ride;
using Convergence.Presentation.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// The narration script of Level 1, the single source for subtitles, pauses, pacing, and what the rider sees while
    /// each clause is spoken. It is the v6 script of TASK_LEVEL1_V6_TRAIN.md (2026-10-26, 3Blue1Brown-style: real terms, brisk),
    /// with the bake's numbers written in (the batch, the test digits, the weights, the target accuracy); the tests
    /// fail if they ever disagree with Assets/_Project/Data/Level1/level1_digits.json. One narrator, the Guide.
    /// [p0.8] marks a pause of 0.8 s inside a line: subtitles change clause by clause at the marks, and the reading
    /// time includes them. {cue} at the start of a clause names what to show while that clause is spoken (several:
    /// {a,b}); the views that know a cue react to it. A voice file only plays when its sidecar text matches this text,
    /// so an old recording can never be heard under new words. ArtSource/voice/render_voices.py reads this file to
    /// render the voice. Development tooling only.
    /// </summary>
    internal static class RideNarrationData
    {
        public const float WordsPerSecond = 2.7f;
        public const float DefaultPauseAfter = 0.3f;

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
            // E0, the dock: every one of these is a three, and Pixel can't read any of them (intro and dock line).
            new Line(RideLine.Threes, "{threes}Every one of these is a three. [p0.15] You read them all, [p0.1] without thinking.", 0.2f),
            new Line(RideLine.MeetPixel, "{pixel}This is Pixel. [p0.15] Pixel can't read a single one.", 0.2f),
            new Line(RideLine.PixelBrain, "{brain}In the next few minutes, [p0.1] you'll build Pixel a real neural network, [p0.1] the kind behind face unlock and chatbots, [p0.15] and train it to read your handwriting.", 0.2f),
            new Line(RideLine.IntroStops, "{stops}Five stops: [p0.15] pixels, [p0.15] neurons, [p0.15] the network, [p0.15] backpropagation, [p0.15] and training. [p0.15] {blast}In between, [p0.1] blast every three you see.", 0.2f),
            new Line(RideLine.PullGo, "{go}Pull the orange lever, [p0.1] or press A.", 0.2f),

            // E1, pixels, then draw your 3 (the drawing is the input for the rest of the ride).
            new Line(RideLine.PixelsSquares, "{grid}To a computer, [p0.1] a picture is a grid of tiny squares, [p0.1] called pixels.", 0.2f),
            new Line(RideLine.PixelsNumbers, "{numbers}And every pixel is just a number. [p0.15] {zero}Zero for blank. [p0.1] {one}One for full ink.", 0.2f),
            new Line(RideLine.PixelsFortyNine, "{shrink}We'll use seven by seven. [p0.15] {column}Forty-nine numbers. [p0.1] That's all the network ever sees.", 0.2f),
            new Line(RideLine.DrawYourTurn, "{unfold}Now draw your own three. [p0.15] Hold the trigger, [p0.1] and write on the board. [p0.1] Shoot DONE when you're happy.", 0.1f),
            new Line(RideLine.DrawBigger, "Bigger! [p0.1] Fill the board.", 0.1f),
            new Line(RideLine.DrawDoneNudge, "Looks like a three to me. [p0.1] Shoot DONE.", 0.1f),
            new Line(RideLine.DrawDone, "That's your three, [p0.1] as forty-nine numbers. [p0.15] Hang on to it.", 0.1f),
            new Line(RideLine.ZapIntro, "Digits incoming! [p0.1] Blast every three, [p0.1] and spare the rest. [p0.15] For now, [p0.1] you're the classifier.", 0.1f),
            new Line(RideLine.ZapWrong, "Careful! [p0.1] That one wasn't a three.", 0.1f),

            // E2, one neuron, then spray its weights.
            new Line(RideLine.NeuronHolds, "{neuron}This is a neuron. [p0.1] It holds one number, [p0.1] its activation. [p0.15] {glow}The bigger the number, [p0.1] the brighter it glows.", 0.2f),
            new Line(RideLine.NeuronWires, "{wires}It's connected to all forty-nine pixels, [p0.1] and every connection has a weight.", 0.2f),
            new Line(RideLine.NeuronGreenRed, "{green}A positive weight says: [p0.1] ink in this spot means three. [p0.15] {red}A negative weight says: [p0.1] ink in this spot means not a three.", 0.2f),
            new Line(RideLine.NeuronAdds, "{add}Multiply each pixel by its weight, [p0.1] add them up, [p0.1] and add a bias. [p0.15] {line}A sigmoid squishes the total between zero and one. [p0.1] High means the neuron fires.", 0.2f),
            new Line(RideLine.NeuronYourTurn, "{paint}Your turn: [p0.1] build a three detector. [p0.15] Right hand paints green, [p0.1] left hand red. [p0.1] Hold the trigger and sweep. [p0.15] {digits}Light up every three beside the grid, [p0.1] and keep the other digits dark.", 0.1f),
            new Line(RideLine.NeuronLitForOther, "It fired for the wrong digit. [p0.1] Paint negative where that one has ink."),
            new Line(RideLine.NeuronMissedThree, "A three slipped past. [p0.1] Paint positive where the threes have ink."),
            new Line(RideLine.NeuronSolved, "You just built a three detector, [p0.1] by hand.", 0.1f),

            // E3, the network: send your 3 through it.
            new Line(RideLine.NetworkTeam, "{copy}One neuron spots one pattern. [p0.15] {layer}Put sixteen side by side, [p0.1] and you have a layer. [p0.1] Each one will learn to look for something different.", 0.2f),
            new Line(RideLine.NetworkAnswers, "{answers}Then ten output neurons, [p0.1] one for each digit.", 0.2f),
            new Line(RideLine.NetworkWire, "{connect}Connect every neuron to the next layer, [p0.1] and that's a neural network. [p0.15] Nine hundred and forty-four weights.", 0.2f),
            new Line(RideLine.NetworkSend, "{race}Shoot your three, [p0.1] and send it in.", 0.1f),

            // E4, the guess and the loss (the guess itself is on the network: it depends on the rider's drawing).
            new Line(RideLine.WrongSays, "{guess}Pixel's guess is in. [p0.15] {wrong}But look how unsure it is. [p0.1] Every weight is still random, [p0.1] so it's only guessing.", 0.2f),
            new Line(RideLine.WrongWanted, "{want}What we want is the three's neuron at one, [p0.1] and every other at zero.", 0.2f),
            new Line(RideLine.WrongLoss, "{oops}Measure how far off it is, [p0.1] and you get one number: [p0.1] the loss. [p0.15] Training means just one thing. [p0.1] Make the loss small.", 0.3f),

            // E5, backpropagation, then the hot-wire hunt on the rider's own 3.
            new Line(RideLine.BackWish, "{nudges}So which weights should change, [p0.1] and which way? [p0.15] {wish}The three's output wants to go up. [p0.1] {dim}Every other output wants to go down.", 0.2f),
            new Line(RideLine.BackFlows, "{back}Those wishes flow backwards, [p0.1] layer by layer, [p0.1] with the chain rule. [p0.15] That's backpropagation. [p0.1] It gives every weight a gradient: [p0.1] which way to move, [p0.1] and how much.", 0.3f),
            new Line(RideLine.BackGrab, "{grab}The hottest wires matter most. [p0.1] Grab each one, [p0.1] and drag it to the three, [p0.1] before it cools.", 0.1f),
            new Line(RideLine.BackHurry, "Quick, [p0.1] they're cooling!", 0.1f),
            new Line(RideLine.BackDone, "Each wire you moved was one real gradient step. [p0.15] Now imagine every weight, [p0.1] over thousands of digits.", 0.1f),
            new Line(RideLine.StopClear, "Stage clear. [p0.1] Pull GO.", 0.1f),

            // E6, gradient descent, then the training game: real training, live.
            new Line(RideLine.DescentHill, "{hill}Picture the loss as a landscape. [p0.15] {ball}Where you stand is a choice of weights. [p0.1] The height is how wrong they are.", 0.2f),
            new Line(RideLine.DescentStep, "{slope}The gradient points uphill, [p0.1] so take a step the other way. [p0.15] That's gradient descent.", 0.2f),
            new Line(RideLine.DescentTooBig, "{bigstep}The size of the step is the learning rate. [p0.15] Too big, [p0.1] and you leap clean over the valley. [p0.15] {smallsteps}Too small, [p0.1] and you'll be here all day.", 0.2f),
            new Line(RideLine.TrainYourTurn, "{landscape}This valley is Pixel's real loss. [p0.15] Pick a step size, [p0.1] and hold its button to train. [p0.1] Every step learns from thirty-two real digits.", 0.1f),
            new Line(RideLine.TrainTooBig, "Way too big! [p0.1] The loss exploded. [p0.15] Let's undo that. [p0.1] Try a smaller step.", 0.1f),
            new Line(RideLine.TrainTooSmall, "It's learning, [p0.1] but painfully slowly. [p0.1] Try a bigger step.", 0.1f),
            new Line(RideLine.TrainGood, "That's the one. [p0.1] Watch the loss go down.", 0.1f),
            new Line(RideLine.TrainDone, "Pixel is trained. [p0.15] You just ran gradient descent on a real neural network.", 0.1f),

            // E7, what training learned (v7), then Pixel reads the rider's own 3 with the weights the rider trained.
            new Line(RideLine.LearnedPainted, "{learned}Remember the neuron you painted by hand? [p0.25] {sixteen}Training just painted all sixteen hidden neurons, [p0.1] from real digits.", 0.2f),
            new Line(RideLine.LearnedHonest, "{blobs}Nobody told them what to look for. [p0.15] They're not neat strokes, [p0.1] but together, [p0.1] they read handwriting.", 0.2f),
            new Line(RideLine.RightYours, "{right}Now, [p0.1] the real test: [p0.15] the three you drew. [p0.2] Pixel says...", 0.2f),
            new Line(RideLine.RightYes, "Three! [p0.15] It can read your handwriting.", 0.3f),
            new Line(RideLine.RightStudy, "Not quite. [p0.1] Your three is unusual. [p0.15] So Pixel studies it, [p0.1] a few more steps...", 0.1f),
            new Line(RideLine.RightNowYes, "And now it's a three.", 0.3f),
            new Line(RideLine.RightAccuracy, "{accuracy}On five hundred digits it has never seen, [p0.1] Pixel gets almost nine out of ten right.", 0.3f),
            new Line(RideLine.RightLoop, "{practice}That's how every neural network learns. [p0.15] Guess. [p0.1] Measure the loss. [p0.1] Backpropagate. [p0.1] Step. [p0.15] Repeat.", 0.3f),

            // Bonus: stump Pixel.
            new Line(RideLine.StumpYourTurn, "Bonus round. [p0.1] Draw the digit on the screen, [p0.1] and shoot DONE. [p0.1] Can Pixel read it?", 0.1f),
            new Line(RideLine.StumpRight, "Got it!", 0.1f),
            new Line(RideLine.StumpWrong, "Fooled it! [p0.1] Even trained networks make mistakes.", 0.1f),
            new Line(RideLine.StumpDone, "Not bad, [p0.1] for nine hundred and seventy numbers.", 0.1f),

            // The recap (owner, 2026-10-27): the five stops again, numbered, the way the intro promised them, before the teaser.
            new Line(RideLine.RecapIntro, "{recap}Quick recap. [p0.1] Five stops.", 0.1f),
            new Line(RideLine.RecapPixels, "{r1}One, [p0.1] pixels: [p0.1] your three became forty-nine numbers.", 0.1f),
            new Line(RideLine.RecapNeuron, "{r2}Two, [p0.1] a neuron: [p0.1] weigh the pixels, [p0.1] add them up, [p0.1] squish.", 0.1f),
            new Line(RideLine.RecapNetwork, "{r3}Three, [p0.1] the network: [p0.1] layers of neurons, [p0.1] all wired together.", 0.1f),
            new Line(RideLine.RecapBackprop, "{r4}Four, [p0.1] backpropagation: [p0.1] measure the loss, [p0.1] send the blame backwards.", 0.1f),
            new Line(RideLine.RecapTraining, "{r5}And five, [p0.15] training: [p0.1] step downhill, [p0.1] again and again.", 0.3f),

            // E8, tokens: the same idea, for words.
            new Line(RideLine.TokensWords, "{sentence}Language works the same way. [p0.15] {tokens}Text gets chopped into pieces called tokens.", 0.2f),
            new Line(RideLine.TokensNumbers, "{vectors}Each token becomes a list of numbers, [p0.1] just like your pixels.", 0.2f),
            new Line(RideLine.TokensPredict, "{predict}A far bigger network, [p0.1] trained the same way, [p0.1] predicts the next token. [p0.15] {mat}That's how a chatbot writes.", 0.3f),
            new Line(RideLine.TokensNext, "{next}Next time, [p0.1] Pixel learns to read words.", 0.5f),
        };

        /// <summary>The intro at the dock (E0's explainer plays with it): GO holds (two seconds of holding skips it). Then the dock says DockLine and GO goes live.</summary>
        public static readonly RideLine[] IntroLines = { RideLine.Threes, RideLine.MeetPixel, RideLine.PixelBrain, RideLine.IntroStops };

        public const RideLine DockLine = RideLine.PullGo;

        /// <summary>
        /// The sequences the segments play (NeuralRideBuilder.Flow refers to them by index, in this order). An explain
        /// sequence plays at a stop while its explainer runs on the cues; its play sequence follows once it unfolded.
        /// </summary>
        public static readonly (string Name, RideLine[] Lines)[] Sequences =
        {
            ("E1 pixels: explain, then draw your 3", new[] { RideLine.PixelsSquares, RideLine.PixelsNumbers, RideLine.PixelsFortyNine, RideLine.DrawYourTurn }),   // 0
            ("E2 neuron: explain", new[] { RideLine.NeuronHolds, RideLine.NeuronWires, RideLine.NeuronGreenRed, RideLine.NeuronAdds }),                      // 1
            ("E2 neuron: your turn", new[] { RideLine.NeuronYourTurn }),                                                                                     // 2
            ("E3 network: explain", new[] { RideLine.NetworkTeam, RideLine.NetworkAnswers, RideLine.NetworkWire }),                                         // 3
            ("E3 network: send your 3", new[] { RideLine.NetworkSend }),                                                                                     // 4
            ("E4 the guess and the loss", new[] { RideLine.WrongSays, RideLine.WrongWanted, RideLine.WrongLoss }),                                          // 5
            ("E5 backprop: explain", new[] { RideLine.BackWish, RideLine.BackFlows }),                                                                      // 6
            ("E5 backprop: grab", new[] { RideLine.BackGrab }),                                                                                              // 7
            ("E6 descent: explain, then train", new[] { RideLine.DescentHill, RideLine.DescentStep, RideLine.DescentTooBig, RideLine.TrainYourTurn }),     // 8
            ("E8 recap, then tokens", new[] { RideLine.RecapIntro, RideLine.RecapPixels, RideLine.RecapNeuron, RideLine.RecapNetwork, RideLine.RecapBackprop, RideLine.RecapTraining, RideLine.TokensWords, RideLine.TokensNumbers, RideLine.TokensPredict, RideLine.TokensNext }),                     // 9
        };

        /// <summary>Lines the moments say (not in a sequence): reactions, win lines, the training game's calls, the reading of the rider's 3.</summary>
        public static readonly RideLine[] MomentLines =
        {
            RideLine.DrawBigger, RideLine.DrawDoneNudge, RideLine.DrawDone, RideLine.ZapIntro, RideLine.ZapWrong,
            RideLine.NeuronLitForOther, RideLine.NeuronMissedThree, RideLine.NeuronSolved,
            RideLine.BackHurry, RideLine.BackDone, RideLine.StopClear,
            RideLine.TrainTooBig, RideLine.TrainTooSmall, RideLine.TrainGood, RideLine.TrainDone,
            RideLine.LearnedPainted, RideLine.LearnedHonest, RideLine.RightYours, RideLine.RightYes, RideLine.RightStudy, RideLine.RightNowYes, RideLine.RightAccuracy, RideLine.RightLoop,
            RideLine.StumpYourTurn, RideLine.StumpRight, RideLine.StumpWrong, RideLine.StumpDone
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
