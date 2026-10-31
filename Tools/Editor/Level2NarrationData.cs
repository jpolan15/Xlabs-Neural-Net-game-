using System.Collections.Generic;
using Convergence.Gameplay.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// The narration script of Level 2, "Inside the Word Machine" (TASK_LEVEL2_WORD_MACHINE): the same Line format as Level 1's
    /// (RideNarrationData: [p0.8] is a pause, {cue} opens a clause and names what the views show while it is spoken), one narrator, the
    /// Guide. Every number it says is the real GPT-2's: 768 numbers a word, 50,257 words, 124 million weights, the odds the
    /// bake printed. ArtSource/voice/render_voices.py reads this file next to RideNarrationData.cs. Development tooling only.
    /// </summary>
    internal static class Level2NarrationData
    {
        public static readonly RideNarrationData.Line[] Lines =
        {
            // The dock: last time, this time, the bike, the five stops. v2 (ADR-023): pauses about twice as long, so a first-timer keeps up.
            new RideNarrationData.Line(RideLine.L2Hello, "{chat}Welcome back! [p0.4] Last time, [p0.2] you taught Pixel to read handwritten numbers. [p0.5] Now Pixel wants to talk.", 0.4f),
            new RideNarrationData.Line(RideLine.L2Chatbot, "{type}You type a question into a chatbot, [p0.3] and an answer comes back, [p0.2] one word at a time. [p0.5] {inside}Today, [p0.2] we ride a bike right inside the machine, [p0.2] and watch how it works.", 0.4f),
            new RideNarrationData.Line(RideLine.L2Bike, "{bike}If you're on a real bike, [p0.2] pedal along! [p0.4] If you're in a chair, [p0.2] just enjoy the ride.", 0.4f),
            new RideNarrationData.Line(RideLine.L2Stops, "{stops}Five stops on the way: [p0.3] tokens, [p0.3] meaning, [p0.3] attention, [p0.3] prediction, [p0.3] and writing. [p0.5] {real}Everything you'll see comes from a real language model, [p0.2] called GPT-2.", 0.4f),

            // Stop 1, tokens: chop the words. Why pieces: no dictionary holds every word, and a letter alone means too little.
            new RideNarrationData.Line(RideLine.TokChop, "{sentence}First stop: [p0.2] the chopper. [p0.5] The machine can't learn every word there is, [p0.3] and single letters mean too little. [p0.5] {chop}So it chops your words into pieces in between. [p0.3] The pieces are called tokens.", 0.5f),
            new RideNarrationData.Line(RideLine.TokNumbers, "{ids}GPT-2 knows about fifty thousand tokens, [p0.3] and every token has a number, [p0.2] like a name tag. [p0.4] These are its real numbers.", 0.5f),
            new RideNarrationData.Line(RideLine.TokRare, "{rare}Common words stay whole. [p0.3] Rare words get chopped into smaller pieces. [p0.5] {boss}This long one becomes eleven tokens!", 0.5f),
            new RideNarrationData.Line(RideLine.TokSliceGo, "{slice}Your turn: [p0.2] be the chopper. [p0.4] Swipe your laser through the glowing seams, [p0.2] or shoot them. [p0.4] Leave whole words alone.", 0.3f),
            new RideNarrationData.Line(RideLine.TokWhole, "That one is a single token. [p0.2] Leave it whole.", 0.1f),
            new RideNarrationData.Line(RideLine.TokBoss, "The big one! [p0.2] Swipe all the way through.", 0.1f),
            new RideNarrationData.Line(RideLine.TokDone, "Chopped! [p0.3] From now on, [p0.2] the machine never sees letters, [p0.2] only these numbered pieces.", 0.3f),

            // Stop 2, meaning: the number is a name tag; a learned table gives each token 768 numbers, plus its place in the sentence; squashed to 3, similar words sit close.
            new RideNarrationData.Line(RideLine.EmbName, "{name}But a token's number is just a name tag. [p0.4] The number for cat tells you nothing about cats.", 0.5f),
            new RideNarrationData.Line(RideLine.EmbTable, "{table}So the machine looks each token up in a giant table, [p0.3] and gets seven hundred and sixty-eight numbers that describe it. [p0.5] {learned}Nobody typed those numbers in. [p0.3] The machine learned them in training, [p0.2] the same way Pixel learned its weights.", 0.5f),
            new RideNarrationData.Line(RideLine.EmbPlace, "{place}It also adds where each token sits in the sentence, [p0.3] so dog bites man [p0.2] and man bites dog [p0.2] don't mean the same thing.", 0.5f),
            new RideNarrationData.Line(RideLine.EmbSquash, "{squash}Seven hundred and sixty-eight numbers is too many to picture. [p0.4] So we squashed them down to three. [p0.5] {space}Now every word is a point in space.", 0.5f),
            new RideNarrationData.Line(RideLine.EmbSpace, "{close}And look: [p0.2] words with similar meanings ended up close together. [p0.4] Nobody told it to do that.", 0.5f),
            new RideNarrationData.Line(RideLine.EmbMath, "{math}Even directions mean something. [p0.4] Start at king, [p0.3] take away man, [p0.3] add woman. [p0.5] {queen}You land right next to queen.", 0.5f),
            new RideNarrationData.Line(RideLine.EmbSortGo, "{sort}Your turn: [p0.2] sort the loose words into their neighbourhoods. [p0.4] Point at a word, [p0.2] hold the trigger, [p0.2] and carry it home.", 0.3f),
            new RideNarrationData.Line(RideLine.EmbWrong, "Not quite. [p0.2] That word belongs somewhere else.", 0.1f),
            new RideNarrationData.Line(RideLine.EmbDone, "That's what the machine learned: [p0.2] words that mean similar things live close together.", 0.3f),

            // Stop 3, attention: look back, share out, then blend in the meaning (the step v1 left out); one head of 144, picked by us.
            new RideNarrationData.Line(RideLine.AttMeaning, "{sentence}Words get their meaning from the words around them. [p0.5] A fluffy blue creature [p0.2] isn't just any creature.", 0.5f),
            new RideNarrationData.Line(RideLine.AttLookBack, "{look}So each word looks back at the words before it, [p0.3] and decides which ones matter most. [p0.4] It can't look ahead: [p0.2] those words aren't written yet.", 0.5f),
            new RideNarrationData.Line(RideLine.AttShares, "{shares}That's called attention. [p0.4] Each earlier word gets a share, [p0.3] and the shares add up to one hundred percent.", 0.5f),
            new RideNarrationData.Line(RideLine.AttBlend, "{blend}Then the word mixes in a little meaning from each one, [p0.2] by its share. [p0.5] Now creature means: [p0.3] a fluffy, [p0.1] blue creature.", 0.5f),
            new RideNarrationData.Line(RideLine.AttReal, "{real}These are real numbers from GPT-2. [p0.4] It has a hundred and forty-four attention heads, [p0.2] and each one learned its own job. [p0.4] We picked the one that links words the way you would.", 0.5f),
            new RideNarrationData.Line(RideLine.AttGo, "{spot}Your turn: [p0.2] you are the attention. [p0.4] Point your laser at the words that matter, [p0.2] and hold it there. [p0.4] The longer you look, [p0.2] the bigger the share.", 0.3f),
            new RideNarrationData.Line(RideLine.AttSpread, "Look at both words. [p0.2] Share your attention.", 0.1f),
            new RideNarrationData.Line(RideLine.AttWrong, "Not that one. [p0.2] That word doesn't change the meaning.", 0.1f),
            new RideNarrationData.Line(RideLine.AttDone, "That's attention: [p0.2] every word, [p0.2] read in context.", 0.3f),

            // Stop 4, prediction: scores to odds over every token; a low odd is not a wrong word; surprise is the loss it learned from.
            new RideNarrationData.Line(RideLine.PredJob, "{prompt}After twelve rounds of looking and mixing, [p0.3] the machine has one job: [p0.3] guess the next word.", 0.5f),
            new RideNarrationData.Line(RideLine.PredOdds, "{bars}It gives every token it knows a score, [p0.3] all fifty thousand of them, [p0.3] and turns the scores into odds. [p0.5] These are the real odds GPT-2 gave.", 0.5f),
            new RideNarrationData.Line(RideLine.PredUnsure, "{unsure}It's rarely sure. [p0.5] {hungry}Our word, [p0.2] hungry, [p0.2] only got about one percent. [p0.4] But too, [p0.1] so, [p0.1] and scared fit as well. [p0.3] Lots of words can come next.", 0.5f),
            new RideNarrationData.Line(RideLine.PredLearn, "{learn}That's how it learned. [p0.4] GPT-2 read millions of web pages, [p0.2] guessing every next word. [p0.5] When the real word got low odds, [p0.2] it was surprised, [p0.2] and that surprise was its loss. [p0.5] Then backpropagation and gradient descent nudged its weights, [p0.2] just like Pixel's.", 0.5f),
            new RideNarrationData.Line(RideLine.PredGo, "{bet}Your turn: [p0.2] place your chips. [p0.4] Shoot a word to add a chip. [p0.2] Right hand adds, [p0.2] left hand takes one back. [p0.4] Ten chips, [p0.2] then see what the machine thinks.", 0.3f),
            new RideNarrationData.Line(RideLine.PredBeat, "You beat the machine on that one!", 0.1f),
            new RideNarrationData.Line(RideLine.PredMiss, "That was a long shot. [p0.2] That's what odds are.", 0.1f),
            new RideNarrationData.Line(RideLine.PredDone, "You just did the machine's one trick: [p0.2] weighing the odds.", 0.3f),

            // Stop 5, writing: the next word is drawn from the odds (loaded dice), then the loop; temperature reshapes the dice.
            new RideNarrationData.Line(RideLine.GenLoop, "{pick}To write, [p0.2] the machine picks the next word like rolling loaded dice. [p0.4] Likely words win most often, [p0.2] but not every time. [p0.4] That's why a chatbot can answer the same question two different ways. [p0.5] {again}It adds the word to the sentence, [p0.2] and runs the whole machine again, [p0.2] one word at a time.", 0.5f),
            new RideNarrationData.Line(RideLine.GenTemp, "{temp}One knob changes the dice: [p0.2] temperature. [p0.4] {cold}Turn it cold, [p0.2] and it nearly always picks the safest word. [p0.4] {hot}Turn it hot, [p0.2] and wild words get a real chance.", 0.5f),
            new RideNarrationData.Line(RideLine.GenGo, "{road}Your turn: [p0.2] steer the story. [p0.4] At every fork, [p0.2] point at the word you want, [p0.2] and pull the trigger to ride that way. [p0.4] Safe words are easy. [p0.2] Wild words are rare.", 0.3f),
            new RideNarrationData.Line(RideLine.GenWild, "Wow, [p0.2] that was a long shot!", 0.1f),
            new RideNarrationData.Line(RideLine.GenSafe, "Safe and sensible.", 0.1f),
            new RideNarrationData.Line(RideLine.GenDone, "That's your story. [p0.3] You did the steering, [p0.2] and the machine did the guessing.", 0.3f),

            // The finale: the whole loop once more, the scale, and the warning every student should leave with.
            new RideNarrationData.Line(RideLine.OutTalk, "{talk}And that's how Pixel learns to talk. [p0.4] Not from grammar rules, [p0.3] but from guessing the next word, [p0.2] billions of times.", 0.5f),
            new RideNarrationData.Line(RideLine.OutRecap, "{recap}Let's ride the whole machine once more. [p0.4] Chop the words into tokens. [p0.3] Look up their meaning. [p0.3] Let them pay attention to each other. [p0.3] Score every next word. [p0.3] Roll the dice. [p0.3] And go around again.", 0.5f),
            new RideNarrationData.Line(RideLine.OutScale, "{scale}Pixel's number brain had nine hundred and seventy numbers. [p0.4] GPT-2 has one hundred and twenty-four million. [p0.4] Today's biggest chatbots have hundreds of billions, [p0.2] plus extra training to be helpful. [p0.4] But inside, [p0.2] it's the same trick.", 0.5f),
            new RideNarrationData.Line(RideLine.OutCareful, "{careful}One last thing. [p0.4] The machine picks words that sound likely, [p0.2] not words that are true. [p0.5] So a chatbot can be wrong, [p0.2] and still sound sure. [p0.4] Always check.", 0.5f),
            new RideNarrationData.Line(RideLine.OutEnd, "{end}That's the machine inside the chatbot. [p0.5] Nothing magic. [p0.3] Just numbers, [p0.2] and a very good guess.", 0.6f),
        };

        /// <summary>The intro at the dock: GO holds until it ends. Then the dock says Level 1's "Pull the orange lever" line and GO goes live.</summary>
        public static readonly RideLine[] IntroLines = { RideLine.L2Hello, RideLine.L2Chatbot, RideLine.L2Bike, RideLine.L2Stops };

        public const RideLine DockLine = RideLine.PullGo;

        /// <summary>Level 1's lines Level 2 says as they are: the dock's "pull the lever" and the win line a game ends with when it has none of its own.</summary>
        public static readonly RideLine[] SharedLines = { RideLine.PullGo, RideLine.StopClear };

        /// <summary>The sequences the segments play, in the order NeuralRideBuilder.Level2 refers to them by index.</summary>
        public static readonly (string Name, RideLine[] Lines)[] Sequences =
        {
            ("T1 tokens: explain", new[] { RideLine.TokChop, RideLine.TokNumbers, RideLine.TokRare }),              // 0
            ("T1 tokens: your turn", new[] { RideLine.TokSliceGo }),                                                  // 1
            ("T2 meaning: explain", new[] { RideLine.EmbName, RideLine.EmbTable, RideLine.EmbPlace, RideLine.EmbSquash, RideLine.EmbSpace, RideLine.EmbMath }), // 2
            ("T2 meaning: your turn", new[] { RideLine.EmbSortGo }),                                                  // 3
            ("T3 attention: explain", new[] { RideLine.AttMeaning, RideLine.AttLookBack, RideLine.AttShares, RideLine.AttBlend, RideLine.AttReal }), // 4
            ("T3 attention: your turn", new[] { RideLine.AttGo }),                                                    // 5
            ("T4 prediction: explain", new[] { RideLine.PredJob, RideLine.PredOdds, RideLine.PredUnsure, RideLine.PredLearn }), // 6
            ("T4 prediction: your turn", new[] { RideLine.PredGo }),                                                  // 7
            ("T5 writing: explain", new[] { RideLine.GenLoop, RideLine.GenTemp }),                                    // 8
            ("T5 writing: your turn", new[] { RideLine.GenGo }),                                                      // 9
            ("Finale", new[] { RideLine.OutTalk, RideLine.OutRecap, RideLine.OutScale, RideLine.OutCareful, RideLine.OutEnd }), // 10
        };

        /// <summary>Lines the moments say (not in a sequence): reactions and win lines.</summary>
        public static readonly RideLine[] MomentLines =
        {
            RideLine.TokWhole, RideLine.TokBoss, RideLine.TokDone, RideLine.EmbWrong, RideLine.EmbDone,
            RideLine.AttSpread, RideLine.AttWrong, RideLine.AttDone, RideLine.PredBeat, RideLine.PredMiss, RideLine.PredDone,
            RideLine.GenWild, RideLine.GenSafe, RideLine.GenDone,
        };

        /// <summary>The end card's credits (ADR-022).</summary>
        public const string Credits =
            "MODEL  GPT-2, OpenAI (MIT license): tokens, odds, attention\n"
            + "VOICE  Kokoro-82M text-to-speech (synthetic), Apache 2.0\n"
            + "MUSIC  \"Telstar\"    SOUND EFFECTS  Kenney, CC0\n"
            + "TYPE  Computer Modern Unicode (CMU) fonts, SIL OFL 1.1";

        /// <summary>The words of every line of this level and the shared ones, for the builder.</summary>
        public static IEnumerable<RideNarrationData.Line> All()
        {
            foreach (RideNarrationData.Line line in Lines) yield return line;
            foreach (RideLine id in SharedLines) yield return RideNarrationData.Get(id);
        }
    }
}
